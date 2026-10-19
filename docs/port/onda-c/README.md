# Onda C da Fase 2: testes de integração, visitor e revisões

**Execução em fila, com no máximo 5 agentes simultâneos** (decisão do usuário em 2026-10-14, depois que o lançamento de 20 agentes de uma vez esgotou o limite de sessão e interrompeu todos). Agentes interrompidos são retomados com o contexto preservado; o trabalho parcial fica no worktree. Cada agente trabalha num **git worktree isolado** (`.claude/worktrees/agent-*`), criado a partir do commit `1776531`, para que as compilações não disputem arquivos. O integrador junta os resultados no `main`.

Regras comuns aos agentes de teste (C1–C17):
- Só criam arquivos de teste e notas; **não alteram o `src/`**.
- Cada fatia de um arquivo Rust vira uma **classe própria** (não `partial`), com cópia dos helpers do arquivo como métodos `private static`. Assim as fatias nunca conflitam.
- Teste que falha fica **falhando** (sem `Skip`), com diagnóstico nas notas `Cn.md`. A correção vem na onda de correções (D), com um responsável por área do Core.
- O código Rust é lido de `D:/git/rcoliveira/SQLSelectParserV2/upstream/sqlparser-rs/` (a pasta `upstream/` não existe nos worktrees, porque está no `.gitignore`).

| Unidade | Trecho Rust (v0.63.0) | Destino C# (`tests/SqlParser.Core.Tests/`) | Status |
|---|---|---|---|
| C1 | `tests/sqlparser_common.rs` 1–4319 | `Integration/SqlParserCommonTests1.cs` | ✅ 132/132 passando (net48, net8.0, net10.0); integrado ao `main` |
| C2 | `tests/sqlparser_common.rs` 4320–8209 | `Integration/SqlParserCommonTests2.cs` | ✅ 99/99 passando (net48, net8.0, net10.0); integrado ao `main` |
| C3 | `tests/sqlparser_common.rs` 8210–12371 | `Integration/SqlParserCommonTests3.cs` | ✅ 95/95 passando (net48, net8.0, net10.0); integrado ao `main` |
| C4 | `tests/sqlparser_common.rs` 12372–16422 | `Integration/SqlParserCommonTests4.cs` | ✅ 78/78 portados: 75 passando, 3 com Skip (`test_visit_order` aguarda o visitor; `overflow` e `parse_deeply_nested_boolean_expr_does_not_stackoverflow` derrubam o host com StackOverflowException → onda D); integrado ao `main` |
| C5 | `tests/sqlparser_common.rs` 16423–fim | `Integration/SqlParserCommonTests5.cs` | ✅ 93/93 passando (net48, net8.0, net10.0); integrado ao `main` |
| C6 | `tests/sqlparser_postgres.rs` 1–3408 | `Integration/SqlParserPostgresTests1.cs` | ✅ 72/72 passando (net48, net8.0, net10.0); integrado ao `main` |
| C7 | `tests/sqlparser_postgres.rs` 3409–6827 | `Integration/SqlParserPostgresTests2.cs` | ✅ 74/74 passando (net48, net8.0, net10.0); a variante `#[cfg(feature = "bigdecimal")]` de 1 teste não se aplica; integrado ao `main` |
| C8 | `tests/sqlparser_postgres.rs` 6828–fim | `Integration/SqlParserPostgresTests3.cs` | ✅ 54/54 passando (net48, net8.0, net10.0); integrado ao `main` |
| C9 | `tests/sqlparser_mysql.rs` 1–2650 | `Integration/SqlParserMySqlTests1.cs` | ✅ 51/51 passando (net48, net8.0, net10.0); integrado ao `main` |
| C10 | `tests/sqlparser_mysql.rs` 2651–fim | `Integration/SqlParserMySqlTests2.cs` | ✅ 89/89 passando (net48, net8.0, net10.0); integrado ao `main` |
| C11 | `tests/sqlparser_snowflake.rs` 1–2575 | `Integration/SqlParserSnowflakeTests1.cs` | ✅ 75/75 passando (net48, net8.0, net10.0); integrado ao `main` |
| C12 | `tests/sqlparser_snowflake.rs` 2576–fim | `Integration/SqlParserSnowflakeTests2.cs` | ✅ 89/89 passando (net48, net8.0, net10.0); integrado ao `main` |
| C13 | `tests/sqlparser_mssql.rs` | `Integration/SqlParserMsSqlTests.cs` | ✅ 71/71 passando (net48, net8.0, net10.0); integrado ao `main` |
| C14 | `tests/sqlparser_bigquery.rs` | `Integration/SqlParserBigQueryTests.cs` | ✅ 56/56 passando (net48, net8.0, net10.0); integrado ao `main` |
| C15 | `tests/sqlparser_clickhouse.rs`, `tests/sqlparser_sqlite.rs` | `Integration/SqlParserClickHouseTests.cs`, `Integration/SqlParserSQLiteTests.cs` | ✅ 88/88 passando (ClickHouse 53, SQLite 35; net48, net8.0, net10.0); integrado ao `main` |
| C16 | `tests/sqlparser_{duckdb,databricks,hive,redshift,oracle}.rs` | `Integration/SqlParser{DuckDb,Databricks,Hive,Redshift,Oracle}Tests.cs` | ✅ 127/127 passando (DuckDB 26, Databricks 17, Hive 45, Redshift 26, Oracle 13; net48, net8.0, net10.0); integrado ao `main` |
| C17 | `src/parser/mod.rs` (testes, 21270–fim), `tests/{pretty_print,sqlparser_spark,sqlparser_custom_dialect,sqlparser_teradata,sqlparser_derive_dialect,no_std_recursion,sqlparser_comments,sqlparser_regression}.rs` | `Parser/ParserTests.cs`, `Integration/*.cs`, `Queries/tpch/` | ✅ 126 testes: 118 passando, 8 ignorados (7 `#[ignore]` do próprio Rust + 1 DIV-002), 0 falhas; integrado ao `main` |
| C18 | `src/ast/visitor.rs` (+ testes) | `src/SqlParser.Core/Ast/Visitor*.cs`, `Ast/VisitorTests.cs`, ADR 0007 | ✅ 7/7 passando (+ 18 próprios); integrado ao `main` (casos renumerados para 125–134) |
| R1 | Revisão (somente leitura) de `Parser.Part1*`/`Part2*` | relatório do integrador | ✅ nenhum achado Alto ou Médio; 5 Baixos (ver `R1.md`; o 1º virou DIV-007) |
| R2 | Revisão (somente leitura) de `Parser.Part3*`/`Part4*` | relatório do integrador | ✅ nenhum achado Alto ou Médio; 4 Baixos (ver `R2.md`) |
| R3 | Revisão (somente leitura) de `Parser.Part5*`, `Alter`, `Merge`, dialetos | relatório do integrador | ✅ nenhum achado Alto ou Médio; 2 Baixos (ver `R3.md`) |

Notas de cada unidade: `onda-c/<unidade>.md` (resumo, falhas com diagnóstico, decisões, arquivos).

**Consolidação** (agente de documentação, concluída em 2026-10-14 para C1–C17 e R1–R3): decisões das notas das ondas B e C → `convencoes.md` §13 (casos 79–122), `nomes.md`, `divergencias.md` (DIV-001 ampliada), skill `port-rust-tests`; pendências → `progresso.md` ("Pendências para a onda D"). Cada nota consolidada tem uma linha **Consolidado** no topo. O caso de §13 do visitor (C18) deve usar o próximo número livre (123).
