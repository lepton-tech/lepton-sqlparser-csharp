# SqlParser: API pública

Camada **própria** do produto, construída sobre o `SqlParser.Core` (port fiel do sqlparser-rs). Ver [ADR-0002](../../docs/adr/0002-arquitetura-em-camadas-e-api.md) (arquitetura) e [ADR-0008](../../docs/adr/0008-api-publica-fase-1.md) (decisões desta fase).

**Status:** fase 1 da API pública: ponto de entrada (`SqlQueryParser`), erros (`SqlParseException`) e geração de SQL (`ToSql`). Visitors e utilitários de análise ainda não existem (ver [O que ainda não existe](#o-que-ainda-não-existe)).

O que **não** entra aqui: tradução do Rust. Isso fica no `SqlParser.Core`, que nunca referencia este projeto.

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

Os tipos da AST, os dialetos, `ParserOptions`, `ParserError`, `Location` e `Comments` são os do Core (namespaces `SqlParser.Core`, `SqlParser.Core.Ast` e `SqlParser.Core.Dialects`). A API pública não duplica a AST.

## Exemplos

Os exemplos abaixo são verificados por `tests/SqlParser.Tests/ReadmeExamplesTests.cs` (um teste por exemplo, na mesma ordem).

```csharp
using SqlParser;                 // SqlQueryParser, SqlParseException, ToSql
using SqlParser.Core;            // ParserOptions, ParserError, Location
using SqlParser.Core.Ast;        // Statement, SetExpr, TableFactor...
using SqlParser.Core.Dialects;   // GenericDialect, PostgreSqlDialect...
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

### 7. Comentários

```csharp
var (statements, comments) = new SqlQueryParser().ParseWithComments("SELECT a FROM t -- fim");

foreach (var c in comments.ToVec())
{
    Console.WriteLine($"{c.Comment.AsStr()} em {c.Span.Start.Line}:{c.Span.Start.Column}");
}
```

## Correspondência com o SqlParser-cs

| SqlParser-cs | Esta API | Observação |
|---|---|---|
| `using SqlParser; using SqlParser.Ast; using SqlParser.Dialects;` | `using SqlParser; using SqlParser.Core; using SqlParser.Core.Ast; using SqlParser.Core.Dialects;` | AST e dialetos vêm do Core |
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
| `ast.Visit(visitor)`, `Visitor`, `ControlFlow` | (ainda não existe) | Fase 2 da API pública |

## O que ainda não existe

- **Visitors** (`Visit`, `PreVisit*`/`PostVisit*`, `ControlFlow`): dependem do port de `ast/visitor.rs` no Core, em andamento.
- **Utilitários de análise** (tabelas e colunas referenciadas etc.): dependem dos visitors.
- **AST com nomes do SqlParser-cs** (`Expression`, `Statement.Select`...): fora de escopo por ora ([ADR-0002](../../docs/adr/0002-arquitetura-em-camadas-e-api.md), "opção futura").
- **Spans na API pública**: os nós do Core já implementam `ISpanned`; nenhuma conveniência foi criada aqui.
- **Serialização (JSON)** da AST.
