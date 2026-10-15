# LeptonTI.SqlParser

Parser SQL para .NET: port em C# do [sqlparser-rs](https://github.com/apache/datafusion-sqlparser-rs) (Rust), com o mesmo comportamento do original. Lê SQL de 16 dialetos, gera a AST, devolve o SQL como texto (inclusive formatado) e oferece visitors, reescrita e análise sintática.

- `netstandard2.0`: roda no .NET Framework 4.8 e no .NET 8/10, sem dependências nativas nem pacotes de runtime.
- Duas DLLs no mesmo pacote: `SqlParser.Core.dll` (port fiel do Rust: tokenizer, dialetos, parser, AST) e `SqlParser.dll` (API pública: `SqlQueryParser`, `ToSql`, visitors, análise).
- A versão acompanha o sqlparser-rs: `0.63.x` corresponde ao Rust `v0.63`. A `0.63.1-preview.1` inclui o `main` do Rust até o commit `dacdbc9`.

## Exemplo

```csharp
using SqlParser;                 // SqlQueryParser, SqlParseException, ToSql, visitors, análise
using SqlParser.Core.Ast;        // Statement, SetExpr, Expr, ObjectName...
using SqlParser.Core.Dialects;   // GenericDialect, PostgreSqlDialect, MsSqlDialect...

var parser = new SqlQueryParser();

// Dialeto padrão (GenericDialect), por instância ou por nome
var statements = parser.Parse("select a, b from t where x = 1");
var pg = parser.Parse("SELECT a::INT FROM t WHERE b ILIKE '%x%'", new PostgreSqlDialect());
var ms = parser.Parse("SELECT TOP 5 [nome] FROM [dbo].[clientes]", "mssql");

Console.WriteLine(statements.ToSql());              // SELECT a, b FROM t WHERE x = 1
Console.WriteLine(statements[0].ToSql(pretty: true)); // uma cláusula por linha, indentação de 2 espaços

// Erros com linha e coluna
if (!parser.TryParse("SELECT * FROM t WHERE x = = 1", out _, out var error))
{
    Console.WriteLine($"{error.Line}:{error.Column} {error.Message}");
    // 1:27 sql parser error: Expected: an expression, found: = at Line: 1, Column: 27
}

// Análise sintática
var tabelas = parser.Parse("SELECT * FROM a JOIN b ON a.id = b.id").GetReferencedTables(); // a, b
```

Também há `SqlVisitor` (ganchos `PreVisit*`/`PostVisit*` e `ControlFlow`), `SqlRewriter` (devolve uma árvore nova; a AST é imutável), atalhos com lambdas (`VisitRelations`, `VisitExpressions`, `RewriteRelations`...), `GetReferencedColumns`, `GetReferencedFunctions`, `GetStatementKind` e `ParseWithComments`.

## Dialetos

`generic`, `ansi`, `postgresql` (`postgres`), `mysql`, `mssql`, `sqlite`, `snowflake`, `bigquery`, `clickhouse`, `duckdb`, `databricks`, `hive`, `redshift`, `spark` (`sparksql`), `oracle` e `teradata`, com as classes `GenericDialect`, `AnsiDialect`, `PostgreSqlDialect`, `MySqlDialect`, `MsSqlDialect`, `SQLiteDialect`, `SnowflakeDialect`, `BigQueryDialect`, `ClickHouseDialect`, `DuckDbDialect`, `DatabricksDialect`, `HiveDialect`, `RedshiftSqlDialect`, `SparkSqlDialect`, `OracleDialect` e `TeradataDialect`.

## Compatibilidade

- Consumidores com C# 7.3 (padrão em projetos antigos do .NET Framework) usam a biblioteca normalmente; `with` e `init` nos records exigem `LangVersion` 9 ou superior.
- As diferenças conhecidas em relação ao Rust são poucas e de baixo impacto (classificação Unicode de identificadores sem aspas fora do ASCII comum, proteção de pilha em outra thread, ausência da feature `bigdecimal`). A lista completa (`DIV-NNN`) acompanha o código-fonte.
- Árvores muito profundas (milhares de `OR` ou de `UNION ALL`): o parse, o `ToSql()` e os visitors funcionam, mas os ganchos dos visitors podem rodar em outra thread (não use controles de UI dentro deles), e `==`/`Equals`/`GetHashCode` dos nós não têm proteção de pilha e podem derrubar o processo. Para comparar ASTs de origem desconhecida, compare `ToSql()`.
- Os símbolos de depuração (PDB portátil) vão embutidos nas DLLs.

## Licença e atribuição

Apache License 2.0. Este pacote contém código traduzido do sqlparser-rs (Apache-2.0, The Apache Software Foundation) e não é afiliado nem endossado pela Apache Software Foundation. Os arquivos `NOTICE`, `THIRD-PARTY-NOTICES.md` e `licenses/` (sqlparser-rs, SqlParser-cs, Unicode) vão dentro do pacote.
