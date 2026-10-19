// Código próprio da camada pública (sem origem no sqlparser-rs nem no SqlParser-cs, que não tem utilitários de
// análise). Construído sobre o visitor (SqlVisitor), sem percorrer a AST por conta própria.
// Ver docs/adr/0009-api-publica-fase-2.md, decisões 5 e 6.

using System.Runtime.CompilerServices;
using SqlParser.Core;
using SqlParser.Core.Ast;

namespace SqlParser;

/// <summary>
/// Utilitários de análise sintática: tabelas, colunas e funções referenciadas e o tipo de uma instrução.
/// </summary>
/// <remarks>
/// <para>
/// A análise é <b>sintática</b>: olha só a AST, sem catálogo e sem dialeto. Os nomes são devolvidos como aparecem
/// no texto (com aspas e maiúsculas originais), na ordem da primeira ocorrência na travessia do visitor, sem
/// repetição. Dois nomes são o mesmo quando têm as mesmas partes, com o mesmo texto e o mesmo estilo de aspas
/// (igualdade de <see cref="ObjectName"/>): <c>t</c>, <c>T</c>, <c>"t"</c> e <c>dbo.t</c> são quatro nomes.
/// </para>
/// <para>
/// Valem para qualquer nó visitável: uma instrução, a sequência devolvida pelo parser, uma <see cref="Query"/>, uma
/// <see cref="Expr"/> etc.
/// </para>
/// </remarks>
public static class SqlAnalysisExtensions
{
    /// <summary>
    /// Tabelas (e visões etc.) referenciadas em <paramref name="node"/>: as relações da AST e os alvos de
    /// <c>INTO</c>, sem as referências a CTEs.
    /// </summary>
    /// <typeparam name="T">Tipo do nó.</typeparam>
    /// <param name="node">Nó analisado.</param>
    /// <returns>Os nomes, sem repetição, na ordem da primeira ocorrência.</returns>
    /// <remarks>
    /// <para>
    /// <b>Relações:</b> são os nomes que o visitor entrega em <see cref="SqlVisitor.PreVisitRelation(ObjectName)"/>,
    /// ou seja, os campos marcados como relação no sqlparser-rs: tabelas de <c>FROM</c>/<c>JOIN</c>, alvos de
    /// <c>INSERT</c>/<c>UPDATE</c>/<c>DELETE</c>/<c>MERGE</c>, nomes criados ou alterados por
    /// <c>CREATE TABLE</c>/<c>VIEW</c> e <c>ALTER TABLE</c>/<c>VIEW</c>, a tabela de <c>CREATE INDEX</c>,
    /// <c>TRUNCATE</c> etc. (lista completa no ADR-0009). Os nomes de <c>DROP TABLE</c> <b>não</b> são relações no
    /// sqlparser-rs v0.63.0 e não aparecem aqui. Uma função de tabela chamada direto no <c>FROM</c>
    /// (<c>FROM generate_series(1, 3)</c>, <c>CROSS APPLY dbo.fn(x)</c>) também é relação e aparece aqui e em
    /// <see cref="GetReferencedFunctions{T}(T)"/>; já <c>LATERAL fn(x)</c> e <c>TABLE(fn(x))</c> só aparecem nas
    /// funções. O pai de <c>SHOW ... FROM x</c> também é relação, mesmo quando é um banco ou esquema
    /// (<c>SHOW TABLES FROM db</c> devolve <c>db</c>).
    /// </para>
    /// <para>
    /// <b>Alvos de <c>INTO</c>:</b> o nome que recebe as linhas em <c>SELECT ... INTO t</c> (SQL Server,
    /// PostgreSQL) e em <c>OUTPUT ... INTO t</c> (SQL Server) é tabela, mesmo que a AST o guarde como expressão.
    /// Como a análise é sintática, uma variável usada como alvo (<c>SELECT a INTO @x</c> do MySQL, o
    /// <c>INTO</c> do PL/pgSQL) também aparece aqui.
    /// </para>
    /// <para>
    /// <b>CTEs não são tabelas:</b> uma relação de uma parte só cujo nome é o de uma CTE visível naquele ponto é
    /// descartada. São visíveis: no corpo da consulta (e em <c>ORDER BY</c>, <c>LIMIT</c> etc.), todas as CTEs do
    /// seu <c>WITH</c> e dos <c>WITH</c> das consultas que a contêm; dentro da definição de uma CTE, as CTEs
    /// anteriores do mesmo <c>WITH</c> e, só quando ela pode ser recursiva (o <c>WITH</c> tem <c>RECURSIVE</c>
    /// ou a definição é uma operação de conjunto, como <c>UNION ALL</c>), ela própria. Fora disso, o próprio
    /// nome é a tabela de verdade: em <c>WITH pedidos AS (SELECT * FROM pedidos WHERE ...)</c>, o
    /// <c>pedidos</c> de dentro é tabela. Fora do escopo, o mesmo nome volta a ser tabela. Nomes qualificados
    /// (<c>s.cte</c>) nunca são CTE.
    /// </para>
    /// <para>
    /// <b>Comparação com o nome da CTE:</b> se nenhum dos dois lados tem aspas, sem diferenciar maiúsculas; se algum
    /// tem, diferenciando (o texto sem as aspas). Os bancos diferem nisso, e a regra não segue nenhum deles
    /// exatamente (detalhes e exemplos no ADR-0009).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> é <see langword="null"/>.</exception>
    public static IReadOnlyList<ObjectName> GetReferencedTables<T>(this T node)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        var collector = new TableCollector();
        _ = node.Visit(collector);
        return collector.Tables.ToReadOnlyList();
    }

    /// <summary>
    /// Colunas referenciadas em <paramref name="node"/>: as expressões que são identificadores
    /// (<see cref="Expr.Identifier"/>, como <c>a</c>) ou identificadores compostos
    /// (<see cref="Expr.CompoundIdentifier"/>, como <c>t.a</c> ou <c>s.t.a</c>).
    /// </summary>
    /// <typeparam name="T">Tipo do nó.</typeparam>
    /// <param name="node">Nó analisado.</param>
    /// <returns>
    /// Os nomes (as partes do identificador, como em <c>t.a</c>), sem repetição, na ordem da primeira ocorrência.
    /// <c>a</c> e <c>t.a</c> são entradas diferentes: a análise não resolve a tabela de cada coluna.
    /// </returns>
    /// <remarks>
    /// <para>
    /// É sintático: todo identificador usado como expressão entra, mesmo quando não é coluna (um alias usado no
    /// <c>ORDER BY</c>, um parâmetro de lambda, uma variável <c>@x</c> do SQL Server, a unidade de
    /// <c>DATEADD(day, ...)</c>...).
    /// </para>
    /// <para>
    /// Não entram nomes que a AST não guarda como expressão: <c>*</c> e <c>t.*</c>, a lista de colunas de
    /// <c>INSERT INTO t (a, b)</c>, o alvo de <c>UPDATE ... SET a = 1</c> (o lado direito entra), as colunas de
    /// <c>CREATE TABLE</c> e os aliases (<c>AS x</c>). Também não entram os alvos de <c>SELECT ... INTO t</c> e
    /// <c>OUTPUT ... INTO t</c>, que são tabelas (ver <see cref="GetReferencedTables{T}(T)"/>).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> é <see langword="null"/>.</exception>
    public static IReadOnlyList<ObjectName> GetReferencedColumns<T>(this T node)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        var collector = new ColumnCollector();
        _ = node.Visit(collector);
        return collector.Columns.ToReadOnlyList();
    }

    /// <summary>
    /// Funções chamadas em <paramref name="node"/>: o nome de cada <see cref="Expr.Function"/> (funções escalares, de
    /// agregação e de janela, inclusive as definidas pelo usuário) e de cada função de tabela no <c>FROM</c>.
    /// </summary>
    /// <typeparam name="T">Tipo do nó.</typeparam>
    /// <param name="node">Nó analisado.</param>
    /// <returns>Os nomes como escritos (<c>COUNT</c> e <c>count</c> são entradas diferentes), sem repetição, na ordem da primeira ocorrência.</returns>
    /// <remarks>
    /// <para>
    /// <b>Funções de tabela</b> entram nas três formas da AST: <c>LATERAL fn(x)</c> e <c>LATERAL FLATTEN(...)</c>
    /// (<see cref="TableFactor.Function"/>), <c>TABLE(fn(x))</c> (<see cref="TableFactor.TableFunction"/>, cuja
    /// expressão é uma <see cref="Expr.Function"/>) e a chamada direta no <c>FROM</c>, como
    /// <c>generate_series(1, 3)</c> ou <c>CROSS APPLY dbo.fn(x)</c> (<see cref="TableFactor.Table"/> com
    /// argumentos). Esta última também é relação no Rust e aparece em <see cref="GetReferencedTables{T}(T)"/>.
    /// No SQL Server, a forma antiga de dica <c>FROM t (NOLOCK)</c> é guardada pelo Rust como argumentos de função de
    /// tabela, e por isso <c>t</c> também aparece aqui.
    /// </para>
    /// <para>
    /// Não entram as construções que o parser representa com nós próprios, mesmo parecendo funções
    /// (<c>CAST</c>, <c>EXTRACT</c>, <c>SUBSTRING</c>, <c>TRIM</c>, <c>POSITION</c>, <c>UNNEST</c>,
    /// <c>JSON_TABLE</c>, <c>OPENJSON</c>...), nem os procedimentos de <c>CALL</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> é <see langword="null"/>.</exception>
    public static IReadOnlyList<ObjectName> GetReferencedFunctions<T>(this T node)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        var collector = new FunctionCollector();
        _ = node.Visit(collector);
        return collector.Functions.ToReadOnlyList();
    }

    /// <summary>Classifica <paramref name="statement"/> pelo comando que a inicia (ver <see cref="SqlStatementKind"/>).</summary>
    /// <param name="statement">A instrução.</param>
    /// <returns>A classificação; <see cref="SqlStatementKind.Other"/> para os comandos sem membro próprio.</returns>
    /// <remarks>
    /// <para>
    /// Uma consulta cujo corpo é <c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c> ou <c>MERGE</c> (a forma
    /// <c>WITH ... INSERT</c> do PostgreSQL, que o Rust guarda como <c>Statement.Query</c>) é classificada pela
    /// instrução interna.
    /// </para>
    /// <para>
    /// Um bloco <c>BEGIN ... END</c> (SQL Server, BigQuery, Snowflake) é <see cref="SqlStatementKind.Block"/>,
    /// embora o Rust o guarde como <c>Statement.StartTransaction</c> (com <c>HasEndKeyword</c> verdadeiro); só o
    /// <c>BEGIN</c>/<c>START TRANSACTION</c> sem bloco é <see cref="SqlStatementKind.Transaction"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="statement"/> é <see langword="null"/>.</exception>
    public static SqlStatementKind GetStatementKind(this Statement statement)
    {
        ThrowIfNull(statement, nameof(statement));
        switch (statement)
        {
            case Statement.Query query:
                return query.Value.Body switch
                {
                    SetExpr.Insert insert => GetStatementKind(insert.Value),
                    SetExpr.Update update => GetStatementKind(update.Value),
                    SetExpr.Delete delete => GetStatementKind(delete.Value),
                    SetExpr.Merge merge => GetStatementKind(merge.Value),
                    _ => SqlStatementKind.Query,
                };
            case Statement.Insert:
                return SqlStatementKind.Insert;
            case Statement.Update:
                return SqlStatementKind.Update;
            case Statement.Delete:
                return SqlStatementKind.Delete;
            case Statement.Merge:
                return SqlStatementKind.Merge;
            case Statement.Truncate:
                return SqlStatementKind.Truncate;
            case Statement.Grant:
                return SqlStatementKind.Grant;
            case Statement.Revoke:
                return SqlStatementKind.Revoke;
            case Statement.Deny:
                return SqlStatementKind.Deny;
            case Statement.StartTransaction { HasEndKeyword: true }:
                // BEGIN ... END: o Rust reaproveita a variante de BEGIN [TRANSACTION] (Parser.ParseBeginExceptionEnd).
                return SqlStatementKind.Block;
            case Statement.StartTransaction:
            case Statement.Commit:
            case Statement.Rollback:
            case Statement.Savepoint:
            case Statement.ReleaseSavepoint:
                return SqlStatementKind.Transaction;
        }

        // Os nomes das variantes são os do Rust (convencoes.md §6): o prefixo cobre também as variantes novas.
        var name = statement.GetType().Name;
        if (name.StartsWith("Create", StringComparison.Ordinal))
        {
            return SqlStatementKind.Create;
        }
        if (name.StartsWith("Alter", StringComparison.Ordinal))
        {
            return SqlStatementKind.Alter;
        }
        if (name.StartsWith("Drop", StringComparison.Ordinal))
        {
            return SqlStatementKind.Drop;
        }
        return SqlStatementKind.Other;
    }

    private static void ThrowIfNull<T>(T value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>O nome de uma expressão que é identificador (<c>a</c>) ou identificador composto (<c>t.a</c>); senão, <see langword="null"/>.</summary>
    private static ObjectName? NameOf(Expr expr) => expr switch
    {
        Expr.Identifier identifier => ObjectName.From(identifier.Value),
        Expr.CompoundIdentifier compound => ObjectName.From(compound.Value),
        _ => null,
    };

    /// <summary>Lista de nomes sem repetição, na ordem da primeira ocorrência (igualdade de <see cref="ObjectName"/>).</summary>
    private sealed class NameList
    {
        private readonly List<ObjectName> _items = new List<ObjectName>();
        private readonly HashSet<ObjectName> _seen = new HashSet<ObjectName>();

        public void Add(ObjectName name)
        {
            if (_seen.Add(name))
            {
                _items.Add(name);
            }
        }

        public IReadOnlyList<ObjectName> ToReadOnlyList() => _items.AsReadOnly();
    }

    /// <summary>
    /// Os alvos de <c>INTO</c> (<c>SELECT ... INTO t</c> e <c>OUTPUT ... INTO t</c>) que são nomes. A AST os guarda
    /// como expressões (<see cref="SelectInto.Targets"/>), visitadas pelo gancho de expressão; o dono
    /// (<see cref="Select"/> ou a instrução de DML) é visitado antes delas, e é nesse momento que são registradas.
    /// </summary>
    private sealed class IntoTargets
    {
        // Por referência: o mesmo texto pode aparecer como coluna em outro ponto da instrução.
        private readonly HashSet<Expr> _targets = new HashSet<Expr>(ReferenceComparer.Instance);

        /// <summary>Registra os alvos de <c>SELECT ... INTO</c> de <paramref name="select"/>.</summary>
        public void Register(Select select) => Register(select.Into);

        /// <summary>Registra os alvos de <c>OUTPUT ... INTO</c> de <paramref name="statement"/> (SQL Server).</summary>
        public void Register(Statement statement)
        {
            var output = statement switch
            {
                Statement.Insert insert => insert.Value.Output,
                Statement.Update update => update.Value.Output,
                Statement.Delete delete => delete.Value.Output,
                Statement.Merge merge => merge.Value.Output,
                _ => null,
            };
            if (output is OutputClause.Output { IntoTable: { } into })
            {
                Register(into);
            }
        }

        /// <summary>O nome da tabela, se <paramref name="expr"/> é um alvo registrado; senão, <see langword="null"/>.</summary>
        public ObjectName? TableName(Expr expr) => _targets.Count > 0 && _targets.Contains(expr) ? NameOf(expr) : null;

        private void Register(SelectInto? into)
        {
            if (into is null)
            {
                return;
            }
            foreach (var target in into.Targets)
            {
                if (NameOf(target) is not null)
                {
                    _ = _targets.Add(target);
                }
            }
        }
    }

    /// <summary>Igualdade por referência (o <c>ReferenceEqualityComparer</c> não existe no <c>netstandard2.0</c>).</summary>
    private sealed class ReferenceComparer : IEqualityComparer<Expr>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();

        public bool Equals(Expr? x, Expr? y) => ReferenceEquals(x, y);

        public int GetHashCode(Expr obj) => RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>
    /// Junta as relações que não são referências a CTEs visíveis e os alvos de <c>INTO</c>
    /// (ver <see cref="GetReferencedTables{T}(T)"/>).
    /// </summary>
    private sealed class TableCollector : SqlVisitor
    {
        // Um escopo por consulta com WITH aberta no caminho da raiz até o nó atual; o último é o mais interno.
        private readonly List<CteScope> _scopes = new List<CteScope>();
        private readonly IntoTargets _into = new IntoTargets();

        public NameList Tables { get; } = new NameList();

        public override ControlFlow PreVisitQuery(Query query)
        {
            // A consulta pode ser a definição de uma CTE do escopo mais interno: o WITH é o primeiro campo
            // visitado de Query, e Cte.Query é o único campo de Cte que contém consultas.
            if (_scopes.Count > 0)
            {
                _scopes[_scopes.Count - 1].Enter(query);
            }
            if (query.With is { } with)
            {
                _scopes.Add(new CteScope(with));
            }
            return ControlFlow.Continue;
        }

        public override ControlFlow PostVisitQuery(Query query)
        {
            if (query.With is not null)
            {
                _scopes.RemoveAt(_scopes.Count - 1);
            }
            if (_scopes.Count > 0)
            {
                _scopes[_scopes.Count - 1].Leave(query);
            }
            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitSelect(Select select)
        {
            _into.Register(select);
            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitStatement(Statement statement)
        {
            _into.Register(statement);
            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitExpression(Expr expression)
        {
            // O alvo de INTO cria ou recebe linhas: é sempre tabela, mesmo com o nome de uma CTE.
            if (_into.TableName(expression) is { } table)
            {
                Tables.Add(table);
            }
            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitRelation(ObjectName relation)
        {
            if (!IsCteReference(relation))
            {
                Tables.Add(relation);
            }
            return ControlFlow.Continue;
        }

        private bool IsCteReference(ObjectName relation)
        {
            if (relation.Value.Count != 1 || relation.Value[0] is not ObjectNamePart.Identifier { Value: var name })
            {
                return false;
            }
            foreach (var scope in _scopes)
            {
                if (scope.IsVisible(name))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>Junta os identificadores usados como expressão, menos os alvos de <c>INTO</c> (ver <see cref="GetReferencedColumns{T}(T)"/>).</summary>
    private sealed class ColumnCollector : SqlVisitor
    {
        private readonly IntoTargets _into = new IntoTargets();

        public NameList Columns { get; } = new NameList();

        public override ControlFlow PreVisitSelect(Select select)
        {
            _into.Register(select);
            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitStatement(Statement statement)
        {
            _into.Register(statement);
            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitExpression(Expr expression)
        {
            if (NameOf(expression) is { } column && _into.TableName(expression) is null)
            {
                Columns.Add(column);
            }
            return ControlFlow.Continue;
        }
    }

    /// <summary>Junta os nomes das funções escalares e das funções de tabela (ver <see cref="GetReferencedFunctions{T}(T)"/>).</summary>
    private sealed class FunctionCollector : SqlVisitor
    {
        public NameList Functions { get; } = new NameList();

        public override ControlFlow PreVisitExpression(Expr expression)
        {
            if (expression is Expr.Function function)
            {
                Functions.Add(function.Value.Name);
            }
            return ControlFlow.Continue;
        }

        public override ControlFlow PreVisitTableFactor(TableFactor tableFactor)
        {
            switch (tableFactor)
            {
                // LATERAL fn(x), LATERAL FLATTEN(...).
                case TableFactor.Function function:
                    Functions.Add(function.Name);
                    break;
                // FROM fn(x), CROSS APPLY fn(x): uma relação com argumentos (TableFunctionArgs).
                case TableFactor.Table { Args: not null } table:
                    Functions.Add(table.Name);
                    break;
                // TABLE(fn(x)) não precisa de nada aqui: a Expr.Function de dentro passa por PreVisitExpression.
            }
            return ControlFlow.Continue;
        }
    }

    /// <summary>
    /// As CTEs de um <c>WITH</c> e qual delas está sendo definida no ponto atual da visita.
    /// </summary>
    private sealed class CteScope
    {
        private readonly Sequence<Cte> _ctes;
        private readonly bool _recursive;
        private int _defining = -1; // índice da CTE cuja consulta está sendo visitada; -1 fora das definições

        public CteScope(With with)
        {
            _ctes = with.CteTables;
            _recursive = with.Recursive;
        }

        /// <summary>Marca a entrada na definição da CTE cuja consulta é <paramref name="query"/> (se for de uma CTE deste escopo).</summary>
        public void Enter(Query query)
        {
            for (var i = 0; i < _ctes.Count; i++)
            {
                if (ReferenceEquals(_ctes[i].Query, query))
                {
                    _defining = i;
                    return;
                }
            }
        }

        /// <summary>Marca a saída da definição da CTE cuja consulta é <paramref name="query"/>.</summary>
        public void Leave(Query query)
        {
            if (_defining >= 0 && ReferenceEquals(_ctes[_defining].Query, query))
            {
                _defining = -1;
            }
        }

        /// <summary>
        /// <paramref name="name"/> é uma CTE visível: fora das definições, todas; dentro da definição da CTE
        /// <c>i</c>, as CTEs <c>0..i-1</c> e, se ela pode ser recursiva, a própria CTE <c>i</c>.
        /// </summary>
        public bool IsVisible(Ident name)
        {
            var previous = _defining >= 0 ? _defining : _ctes.Count;
            for (var i = 0; i < previous; i++)
            {
                if (SameName(_ctes[i].Alias.Name, name))
                {
                    return true;
                }
            }
            return _defining >= 0 && CanReferenceItself(_ctes[_defining]) && SameName(_ctes[_defining].Alias.Name, name);
        }

        /// <summary>
        /// A CTE pode citar o próprio nome: o <c>WITH</c> tem <c>RECURSIVE</c> (PostgreSQL, MySQL, SQLite...) ou a
        /// definição é uma operação de conjunto (a forma de CTE recursiva sem a palavra, do SQL Server e do Oracle).
        /// Senão, o nome se refere a uma tabela de verdade.
        /// </summary>
        private bool CanReferenceItself(Cte cte)
        {
            if (_recursive)
            {
                return true;
            }
            var body = cte.Query.Body;
            while (body is SetExpr.Query { Value: var inner })
            {
                body = inner.Body; // parênteses em volta da definição
            }
            return body is SetExpr.SetOperation;
        }

        private static bool SameName(Ident cteName, Ident reference) =>
            string.Equals(
                cteName.Value,
                reference.Value,
                cteName.QuoteStyle is null && reference.QuoteStyle is null ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
