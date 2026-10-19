# LeptonTech.SqlParser: API pública

Camada **própria** do produto, construída sobre o `LeptonTech.SqlParser.Core` (port fiel do sqlparser-rs). Ver [ADR-0002](../../docs/adr/0002-arquitetura-em-camadas-e-api.md) (arquitetura), [ADR-0008](../../docs/adr/0008-api-publica-fase-1.md) (fase 1) e [ADR-0009](../../docs/adr/0009-api-publica-fase-2.md) (fase 2).

**Status:** fases 1 e 2 da API pública:
- fase 1: ponto de entrada (`SqlQueryParser`), erros (`SqlParseException`) e geração de SQL (`ToSql`);
- fase 2: visitors no estilo do SqlParser-cs (`SqlVisitor`, `node.Visit(visitor)`, `ControlFlow`), reescrita (`SqlRewriter`, `node.Rewrite(rewriter)`), atalhos com lambdas e utilitários de análise (tabelas, colunas e funções referenciadas; tipo da instrução).

O que ainda falta está em [O que ainda não existe](#o-que-ainda-não-existe).

O que **não** entra aqui: tradução do Rust. Isso fica no `LeptonTech.SqlParser.Core`, que nunca referencia este projeto.

## Superfície

| Tipo | Membros |
|---|---|
| `SqlQueryParser` | `SqlQueryParser()`, `SqlQueryParser(int recursionLimit)`, `int? RecursionLimit` |
| | `Sequence<Statement> Parse(string sql, Dialect? dialect = null, ParserOptions? options = null)` |
| | `Sequence<Statement> Parse(string sql, string dialectName, ParserOptions? options = null)` |
| | `bool TryParse(string sql, out Sequence<Statement> statements, out SqlParseException? error)` |
| | `bool TryParse(string sql, Dialect? dialect, out Sequence<Statement> statements, out SqlParseException? error)` |
| | `bool TryParse(string sql, Dialect? dialect, ParserOptions? options, out Sequence<Statement> statements, out SqlParseException? error)` |
| | `SqlParseResult ParseWithComments(string sql, Dialect? dialect = null, ParserOptions? options = null)` |
| `SqlParseException` | `ParserError Error`, `Location? Location`, `long Line`, `long Column`, `SqlParseException(ParserError error, Exception? innerException = null)` |
| `SqlParseResult` | `Sequence<Statement> Statements`, `Comments Comments`, `Deconstruct(out statements, out comments)` |
| `SqlWritingExtensions` | `string ToSql(this IDisplay node, bool pretty = false)` |
| | `string ToSql(this IEnumerable<Statement> statements, bool pretty = false)` |
| `ControlFlow` (enum) | `Continue`, `Break` |
| `SqlVisitor` (abstrata) | `virtual ControlFlow PreVisitX(X)` e `PostVisitX(X)` para `Query`, `Select`, `Relation` (`ObjectName`), `TableFactor`, `Expression` (`Expr`), `Statement`, `Value` (`ValueWithSpan`), `Identifier` (`Ident`), `OrderBy`, `OrderByExpression` (`OrderByExpr`), `GroupBy` (`GroupByExpr`) |
| `SqlRewriter` (abstrata) | `virtual X PreVisitX(X)` e `PostVisitX(X)` para os mesmos 11 tipos; `protected void Break()` |
| `SqlVisitorExtensions` | `ControlFlow Visit<T>(this T node, SqlVisitor visitor) where T : IVisit` |
| | `T Rewrite<T>(this T node, SqlRewriter rewriter) where T : IVisitMut<T>` |
| | `ControlFlow VisitRelations<T>(this T node, Func<ObjectName, ControlFlow> visit)` e `void VisitRelations<T>(this T node, Action<ObjectName> visit)` |
| | `VisitExpressions` (`Expr`) e `VisitStatements` (`Statement`), com as mesmas duas formas |
| | `T RewriteRelations<T>(this T node, Func<ObjectName, ObjectName> rewrite)`, `RewriteExpressions` (`Func<Expr, Expr>`), `RewriteStatements` (`Func<Statement, Statement>`) |
| `SqlAnalysisExtensions` | `IReadOnlyList<ObjectName> GetReferencedTables<T>(this T node) where T : IVisit` |
| | `IReadOnlyList<ObjectName> GetReferencedColumns<T>(this T node)`, `IReadOnlyList<ObjectName> GetReferencedFunctions<T>(this T node)` |
| | `SqlStatementKind GetStatementKind(this Statement statement)` |
| `SqlStatementKind` (enum) | `Other`, `Query`, `Insert`, `Update`, `Delete`, `Merge`, `Create`, `Alter`, `Drop`, `Truncate`, `Grant`, `Revoke`, `Deny`, `Transaction`, `Block` |

Os tipos da AST, os dialetos, `ParserOptions`, `ParserError`, `Location` e `Comments` são os do Core (namespaces `LeptonTech.SqlParser.Core`, `LeptonTech.SqlParser.Core.Ast` e `LeptonTech.SqlParser.Core.Dialects`). A API pública não duplica a AST.

## Exemplos

Os exemplos abaixo são verificados por `tests/LeptonTech.SqlParser.Tests/ReadmeExamplesTests.cs` (um teste por exemplo, na mesma ordem).

```csharp
using LeptonTech.SqlParser;                 // SqlQueryParser, SqlParseException, ToSql, SqlVisitor, ControlFlow...
using LeptonTech.SqlParser.Core;            // ParserOptions, ParserError, Location, Sequence
using LeptonTech.SqlParser.Core.Ast;        // Statement, SetExpr, TableFactor, Expr, ObjectName, Ident...
using LeptonTech.SqlParser.Core.Dialects;   // GenericDialect, PostgreSqlDialect...
```

### 1. Parse com o dialeto padrão (`GenericDialect`)

```csharp
var parser = new SqlQueryParser();

var statements = parser.Parse("select top 10 * from my_table where abc > 99 order by xyz desc");

Console.WriteLine(statements.ToSql());
// SELECT TOP 10 * FROM my_table WHERE abc > 99 ORDER BY xyz DESC
```

### 2. Navegar na AST

A AST usa os nomes do Rust (`Statement.Query`, `SetExpr.Select`, `Expr`), e não os do SqlParser-cs (`Statement.Select`, `SetExpression.SelectExpression`, `Expression`).

```csharp
var query = (Statement.Query)statements[0];
var select = (SetExpr.Select)query.Value.Body;
var table = (TableFactor.Table)select.Value.From[0].Relation;

Console.WriteLine(table.Name.ToSql());              // my_table
Console.WriteLine(select.Value.Selection!.ToSql()); // abc > 99
```

### 3. Dialeto por instância ou por nome

```csharp
var pg = parser.Parse("SELECT a::INT FROM t WHERE b ILIKE '%x%'", new PostgreSqlDialect());
var ms = parser.Parse("SELECT TOP 5 [nome] FROM [dbo].[clientes]", "mssql");
```

Os nomes aceitos são os do `dialect_from_str` do Rust (`Dialect.DialectFromStr`), sem diferenciar maiúsculas: `generic`, `mysql`, `postgresql`/`postgres`, `hive`, `sqlite`, `snowflake`, `redshift`, `mssql`, `clickhouse`, `bigquery`, `ansi`, `duckdb`, `databricks`, `spark`/`sparksql`, `oracle`, `teradata`. Um nome desconhecido lança `ArgumentException`.

### 4. Pretty print

`ToSql(pretty: true)` é o formato alternativo do Rust (`format!("{:#}", stmt)`), com quebras de linha `"\n"` e indentação de dois espaços.

```csharp
Console.WriteLine(parser.Parse("SELECT a, b FROM t WHERE x = 1")[0].ToSql(pretty: true));
// SELECT
//   a,
//   b
// FROM
//   t
// WHERE
//   x = 1
```

Em uma coleção de instruções, `ToSql()` separa as instruções com `"; "` e `ToSql(pretty: true)` com `";\n"`, sem `;` depois da última.

### 5. Erros

A mensagem é a mesma do Rust. A posição (linha e coluna, a partir de 1) fica em `Line`/`Column`; quando o erro não tem posição (por exemplo, fim inesperado do texto), `Location` é `null` e `Line`/`Column` valem 0.

```csharp
try
{
    parser.Parse("SELECT * FROM t WHERE x = = 1");
}
catch (SqlParseException e)
{
    // e.Message == "sql parser error: Expected: an expression, found: = at Line: 1, Column: 27"
    // e.Line == 1, e.Column == 27, e.Error é ParserError.ParserErrorValue
}

if (!parser.TryParse("SELEC 1", out var stmts, out var error))
{
    Console.WriteLine(error.Message);
    // sql parser error: Expected: an SQL statement, found: SELEC at Line: 1, Column: 1
}
```

`TryParse` não lança exceção para SQL inválido, mas lança `ArgumentNullException` se `sql` for `null`.

### 6. Opções e limite de recursão

```csharp
var options = new ParserOptions().WithTrailingCommas(true);
var statements = new SqlQueryParser().Parse("SELECT a, b, FROM t", new PostgreSqlDialect(), options);

var deep = new SqlQueryParser(recursionLimit: 200); // padrão do Core: 50
```

Opções informadas **substituem** as opções padrão do dialeto por inteiro, como `Parser::with_options` no Rust. Exemplo: o DuckDB aceita vírgula final por padrão, mas `Parse(sql, new DuckDbDialect(), new ParserOptions())` não aceita. Para usar o dialeto padrão com opções, use o argumento nomeado: `Parse(sql, options: options)`. A forma `Parse(sql, null, options)` não compila, porque é ambígua com a sobrecarga que recebe o nome do dialeto.

Um limite de recursão acima do padrão aceita árvores mais profundas, e todo código que percorre a árvore desce o mesmo tanto: veja [Árvores profundas e threads](#árvores-profundas-e-threads).

### 7. Comentários

```csharp
var (statements, comments) = new SqlQueryParser().ParseWithComments("SELECT a FROM t -- fim");

foreach (var c in comments.ToVec())
{
    Console.WriteLine($"{c.Comment.AsStr()} em {c.Span.Start.Line}:{c.Span.Start.Column}");
}
```

### 8. Visitor

Derive de `SqlVisitor`, sobrescreva os ganchos de interesse e chame `Visit` em qualquer nó: a sequência devolvida pelo parser, uma instrução, uma expressão. `PreVisit*` roda antes dos filhos do nó e `PostVisit*`, depois. A ordem é a do visitor do Core, gerado a partir do derive `Visit` do Rust ([ADR-0007](../../docs/adr/0007-visitor.md)): em profundidade, com os campos de cada nó na ordem do Rust.

```csharp
sealed class TableCollector : SqlVisitor
{
    public List<string> Tables { get; } = new List<string>();

    public override ControlFlow PreVisitRelation(ObjectName relation)
    {
        Tables.Add(relation.ToSql());
        return ControlFlow.Continue;   // ControlFlow.Break encerra a visita na hora
    }
}

var collector = new TableCollector();
new SqlQueryParser().Parse("SELECT * FROM a JOIN b ON a.id = b.id WHERE a.x IN (SELECT x FROM c)").Visit(collector);
// collector.Tables: a, b, c
```

Os ganchos existem para os 11 tipos que o Rust visita: `Statement`, `Query`, `Select`, `TableFactor`, `Relation` (o `ObjectName` de uma tabela, visão etc.), `Expression` (`Expr`), `Value`, `Identifier`, `OrderBy`, `OrderByExpression` e `GroupBy`. Um gancho que devolve `ControlFlow.Break` interrompe a visita, sem chamar os `PostVisit*` pendentes, e `Visit` devolve `ControlFlow.Break`.

Em árvores muito profundas, os ganchos podem rodar em **outra thread** (ver [Árvores profundas e threads](#árvores-profundas-e-threads)): não dependa da thread dentro de um gancho.

### 9. Atalhos com lambdas

```csharp
var statements = new SqlQueryParser().Parse("SELECT a + 1 FROM t WHERE b > 2");

var expressions = new List<string>();
statements.VisitExpressions(expr => expressions.Add(expr.ToSql()));
// expressions: a + 1, a, 1, b > 2, b, 2

var temSubconsulta = statements.VisitExpressions(
    expr => expr is Expr.Subquery ? ControlFlow.Break : ControlFlow.Continue) == ControlFlow.Break;
// temSubconsulta: false
```

`VisitRelations`, `VisitExpressions` e `VisitStatements` aceitam uma `Action<T>` ou uma `Func<T, ControlFlow>` (para interromper). Recebem o pai antes dos filhos, como `visit_relations`, `visit_expressions` e `visit_statements` do Rust.

### 10. Reescrita

A AST é imutável: a reescrita devolve uma árvore nova e não muda a original. Derive de `SqlRewriter` e devolva, em cada gancho, o nó que deve ficar no lugar (o mesmo, se nada mudar).

```csharp
sealed class RenameTable : SqlRewriter
{
    public override ObjectName PostVisitRelation(ObjectName relation) =>
        relation.ToSql() == "clientes"
            ? ObjectName.From(Sequence.Of(new Ident("crm"), new Ident("clientes")))
            : relation;
}

var statement = new SqlQueryParser().Parse("SELECT * FROM clientes WHERE id = 1")[0];
var rewritten = statement.Rewrite(new RenameTable());

Console.WriteLine(rewritten.ToSql()); // SELECT * FROM crm.clientes WHERE id = 1
Console.WriteLine(statement.ToSql()); // SELECT * FROM clientes WHERE id = 1

var semLimite = new SqlQueryParser().Parse("SELECT * FROM t LIMIT 10").RewriteStatements(
    s => s is Statement.Query q ? q with { Value = q.Value with { LimitClause = null } } : s);
// semLimite.ToSql(): SELECT * FROM t
```

- O nó devolvido por um `PreVisit*` tem os filhos visitados em seguida; o devolvido por um `PostVisit*` não é visitado de novo.
- `RewriteRelations`, `RewriteExpressions` e `RewriteStatements` usam o gancho `post`, como os `visit_*_mut` do Rust: a função recebe o nó com os filhos já reescritos.
- Sem nenhuma troca, `Rewrite` devolve a mesma instância.
- Um gancho pode chamar `Break()` para encerrar a reescrita; as trocas feitas até ali ficam no resultado.
- `Rewrite` exige o tipo-base do nó (`Statement`, e não `Statement.Query`), porque a reescrita pode trocar a variante.

### 11. Análise

```csharp
var statements = new SqlQueryParser().Parse(
    "WITH ativos AS (SELECT id, nome FROM clientes WHERE ativo = 1) "
    + "SELECT a.nome, COUNT(p.id) FROM ativos AS a JOIN pedidos AS p ON p.cliente_id = a.id GROUP BY a.nome");

var tabelas = statements.GetReferencedTables();    // clientes, pedidos (a CTE "ativos" não é tabela)
var colunas = statements.GetReferencedColumns();   // id, nome, ativo, a.nome, p.id, p.cliente_id, a.id
var funcoes = statements.GetReferencedFunctions(); // COUNT
var tipo = statements[0].GetStatementKind();       // SqlStatementKind.Query
```

A análise é sintática (sem catálogo e sem dialeto). Os nomes vêm como `ObjectName`, como no texto, sem repetição e na ordem da primeira ocorrência. Semântica exata ([ADR-0009](../../docs/adr/0009-api-publica-fase-2.md), decisão 5):

- **Tabelas:** as relações da AST (`FROM`/`JOIN`, alvos de `INSERT`/`UPDATE`/`DELETE`/`MERGE`, nomes de `CREATE TABLE`/`VIEW`, `ALTER TABLE`, `TRUNCATE` etc.) e os alvos de `SELECT ... INTO t` e `OUTPUT ... INTO t`, sem as referências a CTEs visíveis no ponto da referência. Os nomes de `DROP TABLE` não são relações no Rust e não aparecem. Uma função de tabela chamada direto no `FROM` (`generate_series(1, 3)`, `CROSS APPLY dbo.fn(x)`) é relação e aparece aqui e nas funções; `LATERAL fn(x)` e `TABLE(fn(x))` só aparecem nas funções. Um alvo de `INTO` que é variável (`SELECT a INTO @x` do MySQL) também aparece, porque a análise não distingue.
- **CTEs:** no corpo da consulta, todas as CTEs do `WITH` (e dos `WITH` de fora) são visíveis; dentro da definição de uma CTE, as anteriores. O próprio nome, dentro da definição, só é a CTE quando ela pode ser recursiva (`WITH RECURSIVE`, ou a definição é um `UNION`/`UNION ALL`/`EXCEPT`/`INTERSECT`); senão, é a tabela de verdade: `WITH pedidos AS (SELECT * FROM pedidos WHERE ...)` devolve `pedidos`.
- **Comparação com o nome da CTE:** sem aspas dos dois lados, sem diferenciar maiúsculas; com aspas em algum lado, diferenciando. Os bancos têm regras próprias (o PostgreSQL passa nomes sem aspas para minúsculas, o Oracle e o Snowflake para maiúsculas, o SQL Server segue a ordenação do banco), e a análise não conhece o dialeto: `WITH "c" ... FROM C` dá `C` como tabela, embora no PostgreSQL `C` seja a CTE. Na maioria das diferenças, a lista sai com um nome a mais; em poucas (nomes que só diferem por maiúsculas ou aspas, em bancos que dobram para maiúsculas ou diferenciam maiúsculas), uma tabela fica de fora. A tabela completa está no [ADR-0009](../../docs/adr/0009-api-publica-fase-2.md), decisão 5.
- **Colunas:** expressões que são identificadores (`a`, `t.a`). Não entram `*`, `t.*`, a lista de colunas de `INSERT`, o alvo de `UPDATE ... SET a =`, os alvos de `INTO` nem aliases. Um alias usado no `ORDER BY` entra, porque a análise não resolve nomes.
- **Funções:** nomes de `Expr.Function` e das funções de tabela no `FROM`: `LATERAL fn(x)`, `LATERAL FLATTEN(...)`, `TABLE(fn(x))` e a chamada direta (`generate_series(1, 3)`). `CAST`, `EXTRACT`, `TRIM`, `SUBSTRING`, `UNNEST`, `JSON_TABLE` e afins têm nós próprios e não entram. No SQL Server, a dica antiga `FROM t (NOLOCK)` fica guardada como argumentos de função de tabela, e `t` também aparece aqui.
- **Tipo da instrução:** `SqlStatementKind`, uma classificação pelo comando (`Query`, `Insert`, `Create`, `Transaction`, `Block`...). Um bloco `BEGIN ... END` (SQL Server, BigQuery, Snowflake) é `Block`; `BEGIN`/`START TRANSACTION` sem bloco é `Transaction`. Para distinguir qualquer variante, use o tipo da instrução (`statement is Statement.CreateTable`).

## Árvores profundas e threads

O parser recusa aninhamentos acima do limite de recursão (50 por padrão), mas cadeias de operadores (`a OR b OR ...`, `a + b + ...`) e de `UNION ALL` não contam para o limite e podem ter milhares de níveis. Para não derrubar o processo com estouro de pilha, o parse, o `ToSql()`, os visitors e a reescrita continuam numa **thread nova** (pilha de 2 MiB) quando falta pilha, como o `#[recursive::recursive]` do Rust ([DIV-005 e DIV-009](../../docs/port/divergencias.md)). Isso tem duas consequências e um limite:

- **Ganchos em outra thread.** Em árvores profundas, os ganchos de `SqlVisitor`/`SqlRewriter` e as funções passadas a `VisitExpressions`, `RewriteRelations` etc. podem rodar em outra thread. Quem chamou espera o fim, e uma exceção de um gancho chega a ele com o tipo e o rastro de pilha originais. Estado `[ThreadStatic]`/`ThreadLocal` não acompanha (a cultura e o `AsyncLocal` acompanham).
- **Thread de UI (STA).** Não use controles do WinForms/WPF nem objetos COM de apartamento STA dentro de um gancho. Chamado numa thread STA, o `Visit` espera com um `Join` que bombeia mensagens COM, o que pode reentrar no código da interface. Junte os dados no visitor e atualize a interface depois que `Visit` voltar, ou chame `Visit` fora da thread de UI.
- **Igualdade sem proteção ([DIV-010](../../docs/port/divergencias.md)).** `==`, `Equals` e `GetHashCode` dos nós (e de `Sequence<Statement>`) são os gerados para os records, recursivos e sem essa proteção: com milhares de níveis, estouram a pilha e derrubam o processo (8.000 `OR` na thread principal do .NET 8; 1.000 numa thread de 256 KB). Para comparar ASTs de origem desconhecida, compare o SQL gerado (`a.ToSql() == b.ToSql()`); não use nós profundos como chave de `Dictionary`/`HashSet`.

Com `new SqlQueryParser(recursionLimit: N)` acima do padrão, subconsultas, parênteses e junções aninhadas passam a ter até `N` níveis. O `ToSql()` e os visitors aguentam, mas a igualdade e o `Span()` (dos nós do Core) de consultas e junções profundas não têm proteção. Use um limite alto só quando as entradas precisarem e, para SQL de terceiros, limite também o tamanho do texto aceito.

## Correspondência com o SqlParser-cs

| SqlParser-cs | Esta API | Observação |
|---|---|---|
| `using SqlParser; using SqlParser.Ast; using SqlParser.Dialects;` | `using LeptonTech.SqlParser; using LeptonTech.SqlParser.Core; using LeptonTech.SqlParser.Core.Ast; using LeptonTech.SqlParser.Core.Dialects;` | AST e dialetos vêm do Core. Assemblies e namespaces são distintos, então as duas bibliotecas podem ser referenciadas no mesmo projeto; num mesmo arquivo, use alias, porque vários tipos têm o mesmo nome ([ADR-0011](../../docs/adr/0011-renome-lepton-sqlparser.md)) |
| `new SqlQueryParser().Parse(sql)` | `new SqlQueryParser().Parse(sql)` | Igual. Dialeto padrão: `GenericDialect` nos dois |
| `Parse(sql, dialect, options)` | `Parse(sql, dialect, options)` | Igual |
| `Parse(sql, options)` | `Parse(sql, options: options)` | Argumento nomeado |
| `Parse(ReadOnlySpan<char> sql, ...)` | `Parse(string sql, ...)` | `netstandard2.0` sem `Span<T>` |
| (não existe) | `Parse(sql, "postgres")` | Dialeto por nome (`dialect_from_str` do Rust) |
| (não existe) | `TryParse(sql, ..., out statements, out error)` | |
| (não existe) | `ParseWithComments(sql, ...)` | `parse_sql_with_comments` do Rust |
| `Sequence<Statement>` (herda de `List<T>`, mutável) | `Sequence<Statement>` do Core (`readonly struct`, `IReadOnlyList<T>`, igualdade estrutural) | Sem `Add`; `First`/`Last` são propriedades (o LINQ `First()` continua funcionando) |
| `ParserException`, `TokenizeException` (`Line`, `Column`) | `SqlParseException` (`Line`, `Column`, `Location`, `Error`) | Um tipo só: erro do tokenizador chega como `ParserError.TokenizerError` |
| `ParserOptions { TrailingCommas, Unescape, RecursionLimit }` (classe mutável) | `ParserOptions` do Core (record imutável: `WithTrailingCommas`, `WithUnescape`, `RequireSemicolonStmtDelimiter`) + `new SqlQueryParser(recursionLimit)` | O limite de recursão não faz parte das opções no Rust |
| `node.ToSql()` (`IWriteSql`) | `node.ToSql()` (`IDisplay`) | Mesma saída que `ToString()` |
| (não existe) | `node.ToSql(pretty: true)` | `{:#}` do Rust |
| `list.ToSqlDelimited(", ")` | (não existe) | Use `string.Join(", ", list.Select(x => x.ToSql()))` |
| `Statement.Select`, `SetExpression.SelectExpression`, `Expression` | `Statement.Query`, `SetExpr.Select`, `Expr` | Nomes do Rust (`convencoes.md` §4) |
| `select.Query.Body`, `body.Select.From` | `query.Value.Body`, `select.Value.From` | Variante-tupla do Rust: campo `Value` (`convencoes.md` §6) |
| `RedshiftDialect`, `DataBricksDialect` | `RedshiftSqlDialect`, `DatabricksDialect` | Nomes dos structs Rust |
| `abstract class Visitor` | `abstract class SqlVisitor` | `Visitor` colidiria com `Visitor` e `Visitor<TBreak>` do Core (CS0104) |
| `enum ControlFlow { Continue, Break }` | `enum ControlFlow { Continue, Break }` | Igual. Convive com o `ControlFlow<TBreak>` do Core (aridade diferente) |
| `ast.Visit(visitor)` (`IElement.Visit`, `Sequence<T>.Visit`) | `node.Visit(visitor)` | Extensão sobre qualquer `IVisit` do Core |
| `PreVisitStatement`, `PreVisitQuery`, `PreVisitTableFactor`, `PreVisitRelation(ObjectName)`, `PreVisitExpression(Expression)` e os `PostVisit*` | Mesmos nomes; `PreVisitExpression(Expr)` | Tipos da AST do Core |
| (não existe) | `PreVisitSelect`, `PreVisitValue`, `PreVisitIdentifier`, `PreVisitOrderBy`, `PreVisitOrderByExpression`, `PreVisitGroupBy` e os `PostVisit*` | Ganchos do Rust |
| `PreVisitDefault`/`PostVisitDefault(IElement)` | (não existe) | O visitor do Rust só tem ganchos para 11 tipos; não há gancho genérico |
| `PreVisitRelation` só para tabelas do `FROM` | `PreVisitRelation` nos 21 campos de relação do Rust | Inclui alvos de `INSERT`, `CREATE TABLE` etc. (ADR-0009) |
| Ordem por reflexão em tempo de execução e `[Visit(n)]` | Ordem do Rust, em código gerado | [ADR-0007](../../docs/adr/0007-visitor.md) |
| (não existe) | `SqlRewriter` e `node.Rewrite(rewriter)` | Reescrita: devolve a árvore nova |
| (não existe) | `VisitRelations`, `VisitExpressions`, `VisitStatements`, `RewriteRelations`, `RewriteExpressions`, `RewriteStatements` | `visit_*` e `visit_*_mut` do Rust |
| (não existe) | `GetReferencedTables`, `GetReferencedColumns`, `GetReferencedFunctions`, `GetStatementKind` | Análise sintática |
| `statement.AsQuery()`, `body.AsSelect()`, `factor.AsTable()` | (não existe) | Use `is`/`as` com os tipos do Core: `(Statement.Query)statement` |

## O que ainda não existe

- **Gancho genérico** (`PreVisitDefault` do SqlParser-cs) e ganchos para outros tipos da AST: o visitor do Rust só tem os 11 ganchos acima.
- **Análise semântica** (resolver a tabela de cada coluna, aliases, catálogo): os utilitários de análise são sintáticos.
- **AST com nomes do SqlParser-cs** (`Expression`, `Statement.Select`...): fora de escopo por ora ([ADR-0002](../../docs/adr/0002-arquitetura-em-camadas-e-api.md), "opção futura").
- **Spans na API pública**: os nós do Core já implementam `ISpanned`; nenhuma conveniência foi criada aqui.
- **Serialização (JSON)** da AST.
