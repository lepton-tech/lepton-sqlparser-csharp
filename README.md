# LeptonTI.SqlParser

Parser SQL para .NET: port em C# do [sqlparser-rs](https://github.com/apache/datafusion-sqlparser-rs) (Rust, projeto Apache DataFusion). O port é **fiel**: mesmos tipos, mesma AST, mesmas mensagens de erro e o mesmo SQL de saída do original, conferidos pelos testes do próprio Rust, portados um a um. Acompanha as versões novas do Rust.

- **`netstandard2.0`**: roda no .NET Framework 4.8 e no .NET 8/10, sem Rust, sem DLLs nativas e sem pacotes de runtime.
- **16 dialetos**, AST completa, SQL de volta como texto (inclusive formatado), visitors, reescrita e análise sintática.
- **Versão atual:** `0.63.1-preview.1` (pré-lançamento), com o Rust até o commit `dacdbc9`, depois da `v0.63.0` ([`CHANGELOG.md`](CHANGELOG.md)).

O pacote `LeptonTI.SqlParser` traz duas DLLs: `SqlParser.Core.dll`, o port fiel (namespaces `SqlParser.Core*`), e `SqlParser.dll`, a API pública (namespace `SqlParser`), no estilo do [SqlParser-cs](https://github.com/TylerBrinks/SqlParser-cs).

## Instalação (pacote local)

Por enquanto o pacote só existe localmente: nada é publicado em feed ([ADR-0010](docs/adr/0010-empacotamento.md)).

```bash
pwsh tools/pack/Pack.ps1        # compila, roda os testes e gera artifacts/LeptonTI.SqlParser.<versão>.nupkg
dotnet nuget add source "<caminho do repositório>/artifacts" --name leptonti-local
dotnet add package LeptonTI.SqlParser --version 0.63.1-preview.1
```

Em vez do `dotnet nuget add source`, um `nuget.config` ao lado da solução consumidora também serve (exemplo em [`tests/PackageSmoke/nuget.config`](tests/PackageSmoke/nuget.config)). Projetos .NET Framework com C# 7.3 usam a biblioteca normalmente; `with`/`init` nos records pedem `LangVersion` 9 ou superior ([ADR-0006](docs/adr/0006-versao-da-linguagem-csharp.md)).

## Início rápido

```csharp
using SqlParser;                 // SqlQueryParser, SqlParseException, ToSql, SqlVisitor, SqlRewriter, análise
using SqlParser.Core;            // ParserOptions, Sequence
using SqlParser.Core.Ast;        // Statement, SetExpr, Expr, ObjectName, Ident...
using SqlParser.Core.Dialects;   // GenericDialect, PostgreSqlDialect, MsSqlDialect...

var parser = new SqlQueryParser();

// Parse: dialeto padrão (GenericDialect), por instância ou por nome
var statements = parser.Parse("select top 10 * from my_table where abc > 99 order by xyz desc");
var pg = parser.Parse("SELECT a::INT FROM t WHERE b ILIKE '%x%'", new PostgreSqlDialect());
var ms = parser.Parse("SELECT TOP 5 [nome] FROM [dbo].[clientes]", "mssql");

// SQL de volta, em uma linha ou formatado
Console.WriteLine(statements.ToSql());
// SELECT TOP 10 * FROM my_table WHERE abc > 99 ORDER BY xyz DESC
Console.WriteLine(parser.Parse("SELECT a, b FROM t WHERE x = 1")[0].ToSql(pretty: true));
// SELECT
//   a,
//   b
// FROM
//   t
// WHERE
//   x = 1

// Erros: mensagem igual à do Rust, com linha e coluna
try { parser.Parse("SELECT * FROM t WHERE x = = 1"); }
catch (SqlParseException e)
{
    Console.WriteLine($"{e.Line}:{e.Column} {e.Message}");
    // 1:27 sql parser error: Expected: an expression, found: = at Line: 1, Column: 27
}
if (!parser.TryParse("SELEC 1", out var stmts, out var error))
    Console.WriteLine(error.Message); // sql parser error: Expected: an SQL statement, found: SELEC at Line: 1, Column: 1

// Visitor (classe abaixo): ganchos PreVisit*/PostVisit*, como no SqlParser-cs
var collector = new TableCollector();
parser.Parse("SELECT * FROM a JOIN b ON a.id = b.id WHERE a.x IN (SELECT x FROM c)").Visit(collector);
// collector.Tables: a, b, c

// Reescrita: a AST é imutável, e a reescrita devolve uma árvore nova
var renamed = parser.Parse("SELECT * FROM clientes").RewriteRelations(
    name => name.ToSql() == "clientes" ? ObjectName.From(Sequence.Of(new Ident("crm"), new Ident("clientes"))) : name);
// renamed.ToSql(): SELECT * FROM crm.clientes

// Análise sintática (sem catálogo): tabelas, colunas, funções e tipo da instrução
var analisada = parser.Parse("SELECT a.nome, COUNT(p.id) FROM clientes AS a JOIN pedidos AS p ON p.cliente_id = a.id GROUP BY a.nome");
var tabelas = analisada.GetReferencedTables();     // clientes, pedidos
var colunas = analisada.GetReferencedColumns();    // a.nome, p.id, p.cliente_id, a.id
var funcoes = analisada.GetReferencedFunctions();  // COUNT
var tipo = analisada[0].GetStatementKind();        // SqlStatementKind.Query

sealed class TableCollector : SqlVisitor
{
    public List<string> Tables { get; } = new List<string>();

    public override ControlFlow PreVisitRelation(ObjectName relation)
    {
        Tables.Add(relation.ToSql());
        return ControlFlow.Continue;   // ControlFlow.Break encerra a visita
    }
}
```

A referência da API pública, com todos os membros, a semântica da análise e a correspondência com o SqlParser-cs, está em [`src/SqlParser/README.md`](src/SqlParser/README.md). A AST usa os nomes do Rust (`Statement.Query`, `SetExpr.Select`, `Expr`).

## Dialetos

| Dialeto | Classe (`SqlParser.Core.Dialects`) | Nome em `Parse(sql, "nome")` |
|---|---|---|
| Genérico (padrão) | `GenericDialect` | `generic` |
| ANSI SQL | `AnsiDialect` | `ansi` |
| PostgreSQL | `PostgreSqlDialect` | `postgresql`, `postgres` |
| MySQL | `MySqlDialect` | `mysql` |
| Microsoft SQL Server | `MsSqlDialect` | `mssql` |
| SQLite | `SQLiteDialect` | `sqlite` |
| Snowflake | `SnowflakeDialect` | `snowflake` |
| BigQuery | `BigQueryDialect` | `bigquery` |
| ClickHouse | `ClickHouseDialect` | `clickhouse` |
| DuckDB | `DuckDbDialect` | `duckdb` |
| Databricks | `DatabricksDialect` | `databricks` |
| Hive | `HiveDialect` | `hive` |
| Redshift | `RedshiftSqlDialect` | `redshift` |
| Spark SQL | `SparkSqlDialect` | `spark`, `sparksql` |
| Oracle | `OracleDialect` | `oracle` |
| Teradata | `TeradataDialect` | `teradata` |

Os nomes não diferenciam maiúsculas; um nome desconhecido lança `ArgumentException`. Dialetos próprios derivam de `Dialect`, como no Rust.

## Compatibilidade e limitações

O comportamento é o do sqlparser-rs. As diferenças conhecidas estão em [`docs/port/divergencias.md`](docs/port/divergencias.md); todas têm impacto baixo ou nenhum:

- **DIV-001** (resolvida): maiúsculas/minúsculas seguem o Unicode do Rust, com tabelas próprias, iguais nos três runtimes.
- **DIV-002**: a identidade de um dialeto é o tipo C# (`dialect is T`); só afeta dialetos do usuário derivados de um embutido.
- **DIV-003**: caracteres fora do BMP (emojis, letras matemáticas) não formam identificadores sem aspas em alguns dialetos.
- **DIV-004**: a classificação Unicode dentro do BMP é a do .NET (`char.IsLetter`), não a `is_alphabetic` do Rust (afeta sinais combinantes raros).
- **DIV-005**: a proteção contra estouro de pilha continua em outra thread quando falta pilha (estado `[ThreadStatic]` não acompanha).
- **DIV-006**: sem as features `bigdecimal` e `serde` do Rust: números ficam como texto e não há serialização.
- **DIV-007**: o escape de caracteres em algumas mensagens de erro é uma aproximação do `escape_debug` do Rust.
- **DIV-008**: enums simples da AST não implementam `IVisit` (sem efeito na visita).
- **DIV-009**: em ASTs muito profundas, o visitor pode continuar em outra thread.

## Versões

A versão acompanha o sqlparser-rs ([ADR-0010](docs/adr/0010-empacotamento.md)): `0.63.x` corresponde ao Rust `v0.63`, e uma tag nova do Rust (ex.: `v0.64.0`) leva o pacote a `0.64.x`. Mudanças só nossas (API pública, correções) avançam o último número ou o sufixo de pré-lançamento. Como o Rust ainda está em `0.x`, uma versão nova pode quebrar a compatibilidade da AST; o [`CHANGELOG.md`](CHANGELOG.md) registra o que muda. O commit do Rust portado fica em [`UPSTREAM.json`](UPSTREAM.json) e na `InformationalVersion` das DLLs.

## Licença e atribuição

Apache License 2.0. O `SqlParser.Core` é obra derivada do sqlparser-rs (Apache-2.0, The Apache Software Foundation); este projeto não é afiliado nem endossado pela Apache Software Foundation. A API pública imita o estilo do SqlParser-cs (MIT), sem cópia de código. As tabelas de maiúsculas/minúsculas vêm do Unicode Character Database (Unicode License v3), e a lógica de `to_lowercase`/`to_uppercase`, da biblioteca padrão do Rust (MIT ou Apache-2.0). Detalhes em [`NOTICE`](NOTICE), [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) e [`licenses/`](licenses/), que também vão dentro do pacote ([ADR-0003](docs/adr/0003-licenciamento-e-atribuicao.md)).

## Para quem mantém o port

Comece por [`CLAUDE.md`](CLAUDE.md) (regras e comandos) e [`docs/port/progresso.md`](docs/port/progresso.md) (estado e diário).

```
src/SqlParser.Core/      Port fiel do sqlparser-rs: tokenizer, keywords, dialetos, parser, AST, spans, visitor
src/SqlParser/           API pública (código próprio): SqlQueryParser, ToSql, visitors, análise
tests/SqlParser.Core.Tests/  Testes portados do Rust (mesmos nomes e casos)
tests/SqlParser.Tests/       Testes da API pública
tests/PackageSmoke/      Consumidores do pacote (net48 e net8.0), fora da solução
tools/                   upstream/ (código de referência), codegen/ (geradores), pack/ (pacote), check/
docs/                    Análise e plano, ADRs, convenções de port, divergências, progresso
```

Requisitos: .NET SDK 10 (`global.json`), PowerShell 7 e Git.

```bash
dotnet build SqlParser.sln            # sem avisos (TreatWarningsAsErrors)
dotnet test SqlParser.sln             # net48, net8.0 e net10.0
pwsh tools/pack/Pack.ps1              # Release + testes + pacote em artifacts/
pwsh tools/pack/Test-Package.ps1      # instala o pacote em apps net48 e net8.0 e confere
```

### Como atualizar quando sair uma versão nova do sqlparser-rs

1. **Portar as mudanças do Rust.** No Claude Code, aberto na pasta do repositório, peça em português, por exemplo "tem versão nova do sqlparser-rs? Atualiza o port" ou "porta o PR #NNNN do sqlparser-rs". A skill [`sync-upstream`](.claude/skills/sync-upstream/SKILL.md) é acionada sozinha (ou digite `/sync-upstream`). Ela:
   - lista e classifica os PRs novos em [`docs/port/upstream-ledger.md`](docs/port/upstream-ledger.md);
   - porta PR a PR, com os testes do Rust;
   - regenera os arquivos gerados (keywords, visitor, tabelas de maiúsculas e minúsculas);
   - roda a suíte nos três alvos e atualiza o [`UPSTREAM.json`](UPSTREAM.json).

   Sem o Claude Code, o mesmo roteiro está escrito na própria skill.
2. **Subir a versão.** Altere `VersionPrefix`/`VersionSuffix` em [`Directory.Build.props`](Directory.Build.props) (tag nova do Rust, por exemplo v0.64.0 → `0.64.x`) e registre a mudança no [`CHANGELOG.md`](CHANGELOG.md).
3. **Gerar e conferir o pacote:** `pwsh tools/pack/Pack.ps1` e depois `pwsh tools/pack/Test-Package.ps1`.
4. **Atualizar os consumidores:** nos projetos que usam a biblioteca, troque a versão de `LeptonTI.SqlParser`. As mudanças que quebram a API estão no `CHANGELOG.md`.

- [Análise técnica e plano](docs/analise-tecnica-e-plano.md) e [decisões de arquitetura (ADRs)](docs/adr/)
- [Convenções de tradução Rust → C#](docs/port/convencoes.md), [glossário de nomes](docs/port/nomes.md) e [divergências](docs/port/divergencias.md)
- [Sincronização com o upstream](docs/port/upstream-ledger.md) e [referências e boas práticas](docs/port/referencias-e-boas-praticas.md)
