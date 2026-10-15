// Código próprio da camada pública (sem origem no sqlparser-rs nem no SqlParser-cs, que não tem utilitários de
// análise). Construído sobre o visitor (SqlVisitor e os atalhos de SqlVisitorExtensions), sem percorrer a AST
// por conta própria. Ver docs/adr/0009-api-publica-fase-2.md, decisões 5 e 6.

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
    /// Tabelas (e visões etc.) referenciadas em <paramref name="node"/>: as relações da AST, sem as referências a CTEs.
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
    /// sqlparser-rs v0.63.0 e não aparecem aqui. Uma função de tabela no <c>FROM</c> com nome
    /// (<c>FROM f(1)</c>) também é relação, e o pai de <c>SHOW ... FROM x</c> também, mesmo quando é um banco
    /// ou esquema (<c>SHOW TABLES FROM db</c> devolve <c>db</c>).
    /// </para>
    /// <para>
    /// <b>CTEs não são tabelas:</b> uma relação de uma parte só cujo nome é o de uma CTE visível naquele ponto é
    /// descartada. São visíveis: no corpo da consulta (e em <c>ORDER BY</c>, <c>LIMIT</c> etc.), todas as CTEs do
    /// seu <c>WITH</c> e dos <c>WITH</c> das consultas que a contêm; dentro da definição de uma CTE, as CTEs
    /// anteriores do mesmo <c>WITH</c> e ela própria (CTE recursiva, com ou sem <c>RECURSIVE</c>, como no SQL Server).
    /// Fora do escopo, o mesmo nome volta a ser tabela. Nomes sem aspas são comparados sem diferenciar maiúsculas;
    /// se algum dos dois tem aspas, a comparação diferencia. Nomes qualificados (<c>s.cte</c>) nunca são CTE.
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
    /// <c>CREATE TABLE</c> e os aliases (<c>AS x</c>).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> é <see langword="null"/>.</exception>
    public static IReadOnlyList<ObjectName> GetReferencedColumns<T>(this T node)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        var columns = new NameList();
        node.VisitExpressions(expr =>
        {
            switch (expr)
            {
                case Expr.Identifier identifier:
                    columns.Add(ObjectName.From(identifier.Value));
                    break;
                case Expr.CompoundIdentifier compound:
                    columns.Add(ObjectName.From(compound.Value));
                    break;
            }
        });
        return columns.ToReadOnlyList();
    }

    /// <summary>
    /// Funções chamadas em <paramref name="node"/>: o nome de cada <see cref="Expr.Function"/> (funções escalares, de
    /// agregação e de janela, inclusive as definidas pelo usuário).
    /// </summary>
    /// <typeparam name="T">Tipo do nó.</typeparam>
    /// <param name="node">Nó analisado.</param>
    /// <returns>Os nomes como escritos (<c>COUNT</c> e <c>count</c> são entradas diferentes), sem repetição, na ordem da primeira ocorrência.</returns>
    /// <remarks>
    /// Não entram as construções que o parser representa com nós próprios, mesmo parecendo funções
    /// (<c>CAST</c>, <c>EXTRACT</c>, <c>SUBSTRING</c>, <c>TRIM</c>, <c>POSITION</c>...), as funções de tabela no
    /// <c>FROM</c> (são relações; ver <see cref="GetReferencedTables{T}(T)"/>) nem os procedimentos de <c>CALL</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> é <see langword="null"/>.</exception>
    public static IReadOnlyList<ObjectName> GetReferencedFunctions<T>(this T node)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        var functions = new NameList();
        node.VisitExpressions(expr =>
        {
            if (expr is Expr.Function function)
            {
                functions.Add(function.Value.Name);
            }
        });
        return functions.ToReadOnlyList();
    }

    /// <summary>Classifica <paramref name="statement"/> pelo comando que a inicia (ver <see cref="SqlStatementKind"/>).</summary>
    /// <param name="statement">A instrução.</param>
    /// <returns>A classificação; <see cref="SqlStatementKind.Other"/> para os comandos sem membro próprio.</returns>
    /// <remarks>
    /// Uma consulta cujo corpo é <c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c> ou <c>MERGE</c> (a forma
    /// <c>WITH ... INSERT</c> do PostgreSQL, que o Rust guarda como <c>Statement.Query</c>) é classificada pela
    /// instrução interna.
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

    /// <summary>Junta as relações que não são referências a CTEs visíveis (ver <see cref="GetReferencedTables{T}(T)"/>).</summary>
    private sealed class TableCollector : SqlVisitor
    {
        // Um escopo por consulta com WITH aberta no caminho da raiz até o nó atual; o último é o mais interno.
        private readonly List<CteScope> _scopes = new List<CteScope>();

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
                _scopes.Add(new CteScope(with.CteTables));
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

    /// <summary>As CTEs de um <c>WITH</c> e qual delas está sendo definida no ponto atual da visita.</summary>
    private sealed class CteScope
    {
        private readonly Sequence<Cte> _ctes;
        private int _defining = -1; // índice da CTE cuja consulta está sendo visitada; -1 fora das definições

        public CteScope(Sequence<Cte> ctes) => _ctes = ctes;

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
        /// <paramref name="name"/> é uma CTE visível: dentro da definição da CTE <c>i</c>, as CTEs <c>0..i</c>; fora
        /// das definições, todas.
        /// </summary>
        public bool IsVisible(Ident name)
        {
            var last = _defining >= 0 ? _defining : _ctes.Count - 1;
            for (var i = 0; i <= last; i++)
            {
                if (SameName(_ctes[i].Alias.Name, name))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SameName(Ident cteName, Ident reference) =>
            string.Equals(
                cteName.Value,
                reference.Value,
                cteName.QuoteStyle is null && reference.QuoteStyle is null ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }
}
