# Rodada 1 de sincronização: lotes 1 a 3 (unidade U1)

Notas da execução dos lotes 1–3 da "Rodada 1" de [`../upstream-ledger.md`](../upstream-ledger.md) (`v0.63.0` → `origin/main` em `dacdbc9`). Procedimento: skills `sync-upstream`, `port-rust-to-csharp` e `port-rust-tests`.

- **Datas:** 2026-10-14 e 2026-10-15 (a sessão foi interrompida e retomada no mesmo worktree).
- **Base:** cópia do `main` (código idêntico ao do commit `16ecd9f`, que só mudou documentação).
- **Clone de referência:** não foi alterado (nada de checkout/reset). O Rust novo foi lido com `git show <commit>` e `git show dacdbc9:<arquivo>`; para leituras amplas, `git archive dacdbc9 src tests derive HEADER` foi extraído numa pasta temporária.
- **Resultado:** 41 PRs fechados (23 do lote 1, 2 do lote 2, 16 do lote 3). `portedThroughCommit` = `b5950628` (#2496): é o maior prefixo cronológico concluído, porque o PR seguinte, #2403, é do lote 6. `baselineTag` continua `v0.63.0`, e os cabeçalhos dos arquivos também (não há tag nova).

## Lote 1: fechamento administrativo

### PRs de infraestrutura → `não-aplicável` (22)

Conferido com `git show --name-only`: nenhum deles toca `src/` ou `tests/`. O motivo de cada um está na coluna "Observação" do ledger.

| PRs | Motivo |
|---|---|
| #2496 | Só `NOTICE.TXT` e o `include` do `Cargo.toml`; o nosso `NOTICE` já reproduz o texto do `NOTICE.TXT` (ADR-0003) |
| #2520, #2527, #2528, #2530, #2562, #2563, #2564, #2583, #2585, #2580 | Fuzzing do Rust (harness, alvos, corpus, CI do ClusterFuzzLite) |
| #2517, #2548, #2547, #2550, #2565 | GitHub Actions e cobertura do upstream |
| #2482, #2582, #2602 | `Cargo.lock`/`Cargo.toml`. A troca para `sort_by_key` em `derive/src/dialect.rs` (#2482) é interna da macro `derive_dialect!`, sem efeito no port; `bigdecimal` não se aplica (DIV-006) |
| #2497 | Imagem do benchmark |
| #2581, #2601 | `.asf.yaml` e `SECURITY.md` do projeto upstream |

### #2595: remover `parse_create_table_with_inherit`

- **Status:** portado.
- **Arquivo:** `tests/SqlParser.Core.Tests/Integration/SqlParserPostgresTests1.cs` (teste removido, como no Rust).

## Lote 2: keywords

`Keywords.g.cs` foi regenerado uma vez, com as duas keywords do lote: o `keywords.rs` de `dacdbc9` foi extraído com `git show dacdbc9:src/keywords.rs` e passado em `pwsh tools/codegen/Generate-Keywords.ps1 -Source <arquivo>`. O diff de `keywords.rs` entre `v0.63.0` e `dacdbc9` tem só `APPROXIMATE` e `PROC`, sem mudança nas listas `RESERVED_*`. O gerador lê o `HEADER` de `upstream/sqlparser-rs/` relativo ao script; no worktree, que não tinha essa pasta, foi copiado só o `HEADER` (pasta ignorada pelo `.gitignore`).

**Efeito:** o enum C# `Keyword` (valores implícitos) é renumerado: +1 a partir de `APPROXIMATE` e +2 a partir de `PROC`. Quem compilou contra o Core precisa recompilar.

| PR | Status | Arquivos C# | Testes |
|---|---|---|---|
| #2480 Redshift: `APPROXIMATE PERCENTILE_DISC` | portado | `Keywords.g.cs`; `Dialect.cs` (`SupportsApproximatePercentileDisc`, depois de `ParsePrefix`); `RedshiftSqlDialect.cs`; `Parser.Part1b.cs` (`ParseExprPrefixByReservedWord`, braço `APPROXIMATE` com `MaybeParse<Expr>`) | `parse_approximate_percentile_disc` (`SqlParserRedshiftTests`, no fim) |
| #2448 MSSQL: `CREATE PROC` | portado | `Keywords.g.cs`; `Dialect.cs` (`SupportsCreateProcSyntax`, depois de `SupportsCreateTableSelect`); `MsSqlDialect.cs`; `Parser.Part2a.cs` (`ParseCreate`) | `parse_create_proc` (`SqlParserMsSqlTests`, no fim) |

## Lote 3: dialetos e parser pontual

Todos `portado`. Os membros novos do `Dialect` e dos dialetos ficam na posição que ocupam no Rust de `dacdbc9` (alguns PRs posteriores inserem membros **antes** de outros anteriores, como no `sqlite.rs`, e a ordem final é a do Rust).

| PR | Arquivos C# | Testes | Observações |
|---|---|---|---|
| #2490 Postgres: `COLLATE` depois de `::` | `Parser.Part1d.cs` (`ParseInfix`, braço `Keyword.COLLATE` antes do `default`) | `parse_collate_after_cast` (`SqlParserPostgresTests1`, depois de `parse_cast_in_default_expr`) | |
| #2458 `LAMBDA` separado de `->` | `Dialect.cs` (`SupportsLambdaKeywordSyntax => SupportsLambdaFunctions`; doc de `SupportsLambdaFunctions` ampliado); `Parser.Part1b.cs` | 3 testes em `SqlParserCustomDialectTests` (classes de escopo em `nomes.md`); `test_lambda_keyword_syntax_with_json_arrow_operator` (`SqlParserDeriveDialectTests`); `test_duckdb_lambda_function` re-portado | O `derive_dialect!` sem `preserve_type_id` vira subclasse de `PostgreSqlDialect` (DIV-002). O teste passa sem `Skip`: nessa entrada o resultado é o do Rust. Nota na DIV-002 |
| #2536 Snowflake: stages | `Dialect.cs` (`SupportsStages`); `SnowflakeDialect.cs` (override; `ParseStageNameIdentifier`: `,`/`;` devolvem o token, nome vazio ou só `@` dá erro); `Parser.Part4d.cs` (`ParseTableFactor`) | `parse_stage_table_factor` (`SqlParserCommonTests5`), `parse_stage_table_factor_rejected` (`SqlParserPostgresTests3`), `test_stage_name_delimiters` (`SqlParserSnowflakeTests2`) | O item "Informativo" do `progresso.md` sobre `parse_stage_name_identifier` consumir o `;` deixou de valer no upstream |
| #2535 ClickHouse: `FIRST`/`AFTER` | `MySQLColumnPosition.cs` (`AFTER {ident}`, com aspas); `Dialect.cs` (`SupportsAlterColumnPosition`); `ClickHouseDialect.cs`, `GenericDialect.cs`, `MySqlDialect.cs`; `Parser.Part5d.cs` (`ParseColumnPosition`, no lugar de `dialect is MySqlDialect or GenericDialect`) | `parse_alter_table_column_position` em `SqlParserClickHouseTests`, `SqlParserCommonTests5` e `SqlParserMySqlTests2` | Saída de `ToString` alterada para `AFTER` com identificador entre aspas |
| #2493 Postgres: `TRIM FROM` | `Parser.Part1c.cs` (`ParseTrimExpr`) | `parse_trim` re-portado (`SqlParserCommonTests3`); `parse_trim_from_without_characters` (`SqlParserPostgresTests3`) | O `expr` do ramo novo virou `fromExpr`: no C# ele colidiria com o `expr` declarado depois no mesmo método (CS0136, caso 72) |
| #2502 Snowflake: `ORDER BY ALL` | `SnowflakeDialect.cs` | — | Os testes existentes com `SupportsOrderByAll` passam a cobrir o Snowflake |
| #2518 Snowflake: caminhos aninhados no `COPY` | `SnowflakeDialect.cs` (`ParseSelectItemForDataLoad`) | `test_copy_into_with_cast_transformation` re-portado (`SqlParserSnowflakeTests2`) | |
| #2561 SQLite: `N'...'` não é string nacional | `Dialect.cs` (`SupportsNationalStringLiteral => true`); `SQLiteDialect.cs`; `Tokenizer.cs` (guarda no braço `N'`) | `test_national_strings_backslash_escape_not_supported` (`TokenizerTests`), `parse_listagg` (`SqlParserCommonTests1`) e `parse_literal_string` (`SqlParserCommonTests2`) re-portados; `parse_n_prefix_not_national_string` (`SqlParserSQLiteTests`) | Tokens diferentes no SQLite |
| #2539 Placeholders sem identificador entre aspas | `Parser.Part3c.cs` (`ParseValue`: guarda `QuoteStyle is null`) | `parse_placeholder_disallows_quoted_ident` (`SqlParserCommonTests5`) | |
| #2526 `supports_table_partitions` | `Dialect.cs`; `GenericDialect.cs`; `MySqlDialect.cs`; `Parser.Part4d.cs` (`ParseTableFactor`) | `custom_dialect_supports_table_partitions` (`SqlParserCustomDialectTests`, antes das funções `IsIdentifierStart`/`IsIdentifierPart`, como no Rust) | Sem mudança de comportamento nos dialetos embutidos |
| #2584 DuckDB: comentários aninhados | `DuckDbDialect.cs` (`SupportsNestedComments`) | `test_duckdb_nested_block_comments` (`SqlParserDuckDbTests`) | |
| #2546 MsSql: `a = FROM` | `Parser.Part5b.cs` (`ParseSelectItem`) | `test_alias_equal_expr` re-portado (`SqlParserCommonTests4`); `parse_bracket_quoted_eq_alias_assignment` (`SqlParserMsSqlTests`, antes de `parse_create_proc`) | `to_lowercase() == "from"` → `ToLowerInvariant()` com `// PORT-DEVIATION DIV-001` (sem efeito observável, como o ponto vizinho); ponto acrescentado à DIV-001 |
| #2558 SQLite: `CAST(expr AS)` | `Expr.Display.cs` (`CAST(x AS)` com `DataType.Unspecified`); `Dialect.cs` (`SupportsCastEmptyDataTypeToUnspecified`); `SQLiteDialect.cs`; `Parser.Part1c.cs` (`ParseCastExpr`) | `test_cast_empty_type` (`SqlParserSQLiteTests`) | |
| #2560 SQLite: string como nome de coluna | `Dialect.cs` (`SupportsStringLiteralColumnNames`); `SQLiteDialect.cs`; `Parser.Part3a.cs` (`ParseColumns`) | `parse_create_table_string_column_names` (`SqlParserSQLiteTests`) | |
| #2552 SQLite: `==` no `UPDATE SET` | `Dialect.cs` (`SupportsDoubleEqAssignment`); `SQLiteDialect.cs`; `Parser.Part5b.cs` (`ParseAssignment`) | `parse_update_set_double_eq` (`SqlParserSQLiteTests`) | |
| #2555 SQLite: identificadores acima de U+FFFF | `SQLiteDialect.cs` (`IsIdentifierStart`: teste `>= U+0080`) | `test_non_bmp_identifiers` (`SqlParserSQLiteTests`) | Com unidades UTF-16, as duas metades de um par substituto (>= U+D800) passam no teste, o que dá o resultado do Rust (caso 14 estendido; DIV-003 atualizada: o SQLite agora aceita identificadores fora do BMP, no Rust e aqui). O teste usa os caracteres literais do `.rs` (U+DFFFE, U+12000, U+12001), como manda a skill `port-rust-tests`, gravados por script e conferidos por code point; o U+007F do Rust (`\u{007f}`) virou o escape C# equivalente |

### Ordem final dos membros novos no SQLite

`SupportsNumericLiteralUnderscores`, `SupportsDoubleEqAssignment` (#2552), `SupportsStringLiteralColumnNames` (#2560), `SupportsCastEmptyDataTypeToUnspecified` (#2558), `SupportsNationalStringLiteral` (#2561): a ordem do `sqlite.rs` de `dacdbc9`. No fim do `Dialect`, `SupportsDoubleEqAssignment` fica antes de `SupportsCastEmptyDataTypeToUnspecified`, também como no Rust.

## Decisões

- **convencoes.md §13, caso 14:** estendido para `ch >= '\u{0080}'` (o teste `>=` aceita as duas metades de um par substituto, como o scalar no Rust).
- **nomes.md (Testes):** classes de escopo novas de `SqlParserCustomDialectTests`.
- **divergencias.md:** DIV-001 (novo ponto `"from"`), DIV-002 (teste novo do `derive_dialect!` sem `Skip`), DIV-003 (SQLite fora do BMP).
- Nenhuma divergência nova e nenhum `Skip` novo. Nenhum teste portado foi editado para passar; os testes que o upstream alterou foram re-portados na versão nova.

## Verificações

- `dotnet build SqlParser.sln -nologo -v q`: 0 avisos, 0 erros.
- `dotnet test SqlParser.sln -nologo -v q`: `SqlParser.Core.Tests` 1.652 aprovados e 9 ignorados em net48, net8.0 e net10.0 (antes: 1.630 e 9; +23 novos, −1 removido); `SqlParser.Tests` 220 nos três alvos.
- `pwsh tools/check/Find-SuspiciousChars.ps1 -Path src,tests`: as mesmas 14 linhas de antes (todas intencionais e anteriores a esta unidade); os escapes escritos nesta unidade (`'\u0080'` no SQLite, `\u007f` no teste) foram conferidos.
- `pwsh tools/codegen/Generate-Visitor.ps1 -Check -Upstream <clone em v0.63.0>`: "Código gerado do visitor está atualizado". Os lotes 1–3 não mudam a AST. **Limitação:** o gerador confere a AST C# contra **uma** árvore Rust. Contra `dacdbc9` ele acusa hoje 10 erros, todos dos lotes 5 e 6 (`CreateForeignTable`, `ArrayElemTypeDef::ParenthesisNotNull`, `DataType::Collate`, `DataType::Object`, `AlterTableOperation::ModifyOrderBy`, `Insert` com 27 campos, `Expr.InList`/`Expr.InSubquery` com 4, `Statement::CreateForeignTable`, `RepetitionQuantifier::Reluctant`). A troca de tipo de campo do #2540 (`Table.TableName`) não aparece, porque o gerador compara nomes e quantidade de campos, não tipos.

## Pendências e cuidados para os próximos lotes

1. **Clone de referência × `portedThroughCommit`:** o `Get-Upstream.ps1` posiciona o clone em `portedThroughCommit` (`b595062`). Nesse commit o `keywords.rs` ainda não tem `PROC`: regenerar `Keywords.g.cs` com a fonte padrão apagaria `Keyword.PROC` e quebraria o build. Até o fim do lote 6, gere com `-Source` a partir de `dacdbc9`.
2. **D3 (`to_*case` completo):** `ParseSelectItem` tem agora dois pontos `"from"` marcados com `PORT-DEVIATION DIV-001`.
3. **Cabeçalhos:** os arquivos tocados continuam com "Portado para C# a partir de sqlparser-rs v0.63.0"; atualizar quando a release fechar (skill `sync-upstream`, §4).

### Posição dos testes dos próximos lotes

> Os lotes 4 e 5 foram portados na unidade U2; as posições atualizadas do lote 6 estão em [`rodada1-lotes4-5.md`](rodada1-lotes4-5.md). A lista abaixo é o registro da U1.

Os testes novos entram na posição do Rust em `dacdbc9`. Nos arquivos que esta unidade já alterou:

- `SqlParserCommonTests5` (fim do arquivo): `parse_insert_by_name` (#2403), `parse_bitwise_not_renders_apart_from_operand` (#2533), `parse_unary_minus_never_renders_line_comment` (#2532), `parse_table_preserves_quotes_and_trailing_tokens` (#2540) e `parse_hex_string_literal_display_escaping` (#2542) entram **antes** de `parse_stage_table_factor`; `parse_compound_field_access_numeric_display` (#2543) entre `parse_alter_table_column_position` e `parse_placeholder_disallows_quoted_ident`; `parse_in_list_leading_subquery` e `parse_in_list_nested_leading_subquery_no_exponential_blowup` (#2586), `parse_bang_not_renders_apart_from_operand` (#2541) e os três `parse_position_*` (#2619) **depois** de `parse_placeholder_disallows_quoted_ident`.
- `SqlParserPostgresTests3` (fim): entre `parse_trim_from_without_characters` e `parse_stage_table_factor_rejected`, na ordem: `parse_insert_by_name_keywords_as_table_and_alias` (#2403), `parse_pg_abs_space_before_negative_operand` (#2531), `parse_bitwise_not_before_pg_prefix_operators` (#2533), `parse_unary_minus_before_pg_prefix_operators` (#2532), `parse_postfix_factorial_spacing` (#2538), `parse_pg_roots_render_apart_from_operand` (#2545); `parse_bitstring_literal_escaping` (#2537) **depois** de `parse_stage_table_factor_rejected`. Os quatro testes de `CREATE FOREIGN TABLE` (#2515) vão depois de `parse_lock_table`.
- `SqlParserSnowflakeTests2` (fim): `test_structured_array_type` (#2488), `test_structured_object_type` (#2489) e `test_snowflake_stage_name_with_escaped_quotes` (#2544) **antes** de `test_stage_name_delimiters`.
- `SqlParserMySqlTests2` (fim): `parse_bitstring_literal_escaping` (#2537) **antes** de `parse_alter_table_column_position`.
- `SqlParserClickHouseTests`: `reject_angle_bracket_array_type` (#2488) depois de `parse_create_table_with_nested_data_types`; `parse_alter_table_modify_order_by` (#2569), `parse_object_type_parameter` (#2489), `parse_tuple_element_access` (#2574), `parse_global_in`, `parse_in_unparenthesized_expr_precedence`, `global_in_bare_table_binds_before_and` e `global_in_bare_table_binds_before_equality` (#2598) **depois** de `parse_alter_table_column_position` e antes das funções auxiliares (`Clickhouse()` …).
- `SqlParserSQLiteTests`, `SqlParserMsSqlTests`, `SqlParserDuckDbTests`, `SqlParserRedshiftTests`, `SqlParserCustomDialectTests` e `SqlParserDeriveDialectTests`: completos em relação a `dacdbc9`.
