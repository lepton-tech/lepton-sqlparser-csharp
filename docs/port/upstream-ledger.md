# Ledger de sincronização com o upstream

Uma linha por PR do sqlparser-rs depois do baseline (`UPSTREAM.json` → `baselineTag`). **Nenhum PR fica sem registro.** "Não-aplicável" exige motivo. Procedimento: skill `sync-upstream`.

- **Baseline:** `v0.63.0` (`85b1a6f`).
- **Portado até:** `dacdbc9` (#2598, `dacdbc99cd16c335fccd353ba41f6c18a7cd73b8`): a Rodada 1 está concluída, com os 63 PRs em `portado` (41) ou `não-aplicável` (22). Não há tag nova: o baseline continua `v0.63.0`.
- **Último levantamento:** 2026-10-14, `origin/main` em `dacdbc9` (2026-10-12): 63 PRs, todos fechados depois do lote 6 (2026-10-15), ver "Rodada 1".

As linhas ficam nas seções abaixo, uma por rodada de sincronização (alvo: tag ou commit), sempre no formato `PR | Commit | Título | Área | Status | Observação`.

## Rodada 1: `v0.63.0` → `origin/main` (`dacdbc9`)

Plano levantado em 2026-10-14 (`git fetch --tags` no clone de referência). Nesta rodada todas as linhas começaram como `pendente`, e as 22 de infraestrutura traziam na observação a **proposta** de `não-aplicável` e o motivo.

**Execução dos lotes 1–3** (2026-10-14; notas por PR em [`sync/rodada1-lotes1-3.md`](sync/rodada1-lotes1-3.md)): os 22 PRs de infraestrutura viraram `não-aplicável` (o motivo ficou na observação); #2595, #2480, #2448 e os 16 PRs do lote 3 viraram `portado`. Nenhum desses PRs muda a forma da AST: o visitor gerado continua consistente (`Generate-Visitor.ps1 -Check` contra o Rust `v0.63.0`). Testes do Core: 1.630 → 1.652 aprovados (+23 testes novos, −1 removido), 9 ignorados, em net48, net8.0 e net10.0.

**Execução dos lotes 4–5** (2026-10-15; notas por PR em [`sync/rodada1-lotes4-5.md`](sync/rodada1-lotes4-5.md)): os 10 PRs de `Display` (lote 4, em ordem cronológica estrita) e os 6 de AST aditiva (lote 5, em ordem cronológica: #2488, #2569, #2553, #2515, #2489, #2574) viraram `portado`. A cada PR que muda a AST, o visitor foi regenerado e conferido contra uma árvore Rust sintética: o `src/` de `dacdbc9` com os hunks de `src/ast/` dos PRs ainda não portados revertidos (`patch -R`). Contra `dacdbc9` puro, o `Generate-Visitor.ps1 -Check` acusa agora só os 4 erros do lote 6 (`Insert` com 27 campos, `Expr.InList`/`Expr.InSubquery` com 4, `RepetitionQuantifier::Reluctant`). Testes do Core: 1.652 → 1.682 aprovados e 9 → 10 ignorados (+31 testes novos: 25 de `tests/*.rs` e 6 internos; o novo `Skip` é `parse_tuple_element_access`, DIV-006), em net48, net8.0 e net10.0. `portedThroughCommit` continua `b595062`.

**Execução do lote 6** (2026-10-15; notas por PR em [`sync/rodada1-lote6.md`](sync/rodada1-lote6.md)): os 6 PRs de quebra de API e da cadeia do `IN` viraram `portado`, em ordem cronológica (#2403 → #2540 → #2510 → #2586 → #2619 → #2598). O visitor foi regenerado a cada PR que muda a AST, contra a árvore sintética sem os PRs ainda não portados; depois do #2598, `Generate-Visitor.ps1 -Check` contra `dacdbc9` puro diz "Código gerado do visitor está atualizado". `Keywords.g.cs` regenerado com `-Source` de `dacdbc9`: idêntico. Testes do Core: 1.682 → 1.695 aprovados (+13 de `tests/*.rs`), 10 ignorados, em net48, net8.0 e net10.0; `SqlParser.Tests` 220. **Fechamento:** todos os 63 PRs têm status final (41 `portado`, 22 `não-aplicável`); `portedThroughCommit` = `dacdbc9`; `baselineTag`, `baselineCommit` e os cabeçalhos dos arquivos continuam `v0.63.0`. As assinaturas públicas alteradas pela rodada estão em [ADR-0009](../adr/0009-api-publica-fase-2.md), "Impacto da Rodada 1 de sincronização".

### Resumo

- **Alvo:** `origin/main` em `dacdbc9` (2026-10-12). **Não há tag nova:** a última release continua `v0.63.0`, o `Cargo.toml` do `main` ainda diz `0.63.0` e não existe `changelog/0.64.0.md`.
- **63 PRs** (63 commits squash, #2403 a #2619), de 2026-09-10 a 2026-10-12.
- **Linhas Rust** (`git diff v0.63.0..dacdbc9`):
  - `src/`: +1.169/−240 em 20 arquivos, incluindo os testes internos `#[cfg(test)]`;
  - `tests/`: +1.721/−42 em 13 arquivos;
  - total a portar: **+2.890/−282**;
  - `fuzz/` e `derive/` (`.rs` de infraestrutura, fora do port): +609/−28.
- **Testes:** 67 `#[test]` novos e 1 removido (#2595).
  - Internos (6): `tokenizer.rs` 2, `ast/value.rs` 2, `ast/spans.rs` 2.
  - Em `tests/` (61):

    | Arquivo | Novos |
    |---|---|
    | `sqlparser_common.rs` | 15 |
    | `sqlparser_postgres.rs` | 14 (e 1 removido) |
    | `sqlparser_clickhouse.rs` | 9 |
    | `sqlparser_sqlite.rs` | 5 |
    | `sqlparser_snowflake.rs` | 4 |
    | `sqlparser_custom_dialect.rs` | 4 |
    | `sqlparser_databricks.rs`, `sqlparser_mssql.rs`, `sqlparser_mysql.rs` | 2 cada |
    | `sqlparser_bigquery.rs`, `sqlparser_duckdb.rs`, `sqlparser_redshift.rs`, `sqlparser_derive_dialect.rs` | 1 cada |

  - **Testes existentes alterados pelo upstream** (re-portar a versão nova; não é "editar para passar"): #2403, #2531, #2533, #2458, #2540, #2545, #2493, #2510, #2518, #2561, #2546 e #2598.
- **Por área** (classificação principal de cada PR):

  | Área | PRs | Esforço |
  |---|---|---|
  | Infra (CI, fuzz, Cargo, ASF) | 22 | proposta: não-aplicável |
  | Só testes | 1 | P |
  | Display/`ToString` | 10 | 10 P |
  | Dialetos (flags) e correções pontuais do parser | 19 | 18 P, 1 M |
  | Tokenizer | 2 | 1 P, 1 M |
  | AST (tipo, variante ou campo novo) | 9 | 3 P, 5 M, 1 G |
  | **Total** | **63** | **33 P, 7 M, 1 G**, mais 22 de infra |

  Um PR pode tocar várias camadas:

  | Camada | PRs |
  |---|---|
  | `parser/mod.rs` | 25 |
  | `ast/**` | 21 (9 deles mudam a forma de um tipo) |
  | `dialect/**` | 20 |
  | `ast/spans.rs` | 4 |
  | `tokenizer.rs` | 3 |
  | `keywords.rs` | 2 |
  | visitor | 1 explícito (#2515), mas todo tipo ou variante nova precisa de cobertura no visitor |

  Esforço: **P** até ~30 linhas de código e nenhuma mudança estrutural; **M** de 30 a 100 linhas, ou mudança de AST ou de algoritmo; **G** acima de 100 linhas ou statement novo.
- **Keywords:** `src/keywords.rs` mudou: +`APPROXIMATE` (#2480) e +`PROC` (#2448), sem mudança nas listas `RESERVED_*`. **É preciso rodar `pwsh tools/codegen/Generate-Keywords.ps1`.**
  - O clone de referência fica no baseline e é lido por outros agentes, então não faça checkout nele.
  - Gere a partir do arquivo extraído: `git -C upstream/sqlparser-rs show dacdbc9:src/keywords.rs > <scratch>/keywords.rs`, depois `pwsh tools/codegen/Generate-Keywords.ps1 -Source <scratch>/keywords.rs`.
  - O cabeçalho gerado usa `baselineTag` e continua `v0.63.0` até o fechamento da release.
  - Feito no lote 2 e conferido no fim do lote 6 (regenerado com `-Source` de `dacdbc9`: sem diferença). Com `portedThroughCommit` = `dacdbc9`, o `Get-Upstream.ps1` passa a posicionar o clone em `dacdbc9`, e a fonte padrão volta a servir; enquanto o clone não for reposicionado (ele continua em `v0.63.0`), use `-Source`.
  - Efeito colateral: o enum C# `Keyword` (valores implícitos) é renumerado a partir de `APPROXIMATE`, então quem compilou contra o Core precisa recompilar.
- **Próximo baseline recomendado:** ainda não existe. Recomendações:
  - Portar até `dacdbc9` avançando só `portedThroughCommit` e manter `baselineTag = v0.63.0` e os cabeçalhos dos arquivos.
  - Quando a `v0.64.0` for publicada, portar o delta até a tag e fechar a release (skill `sync-upstream`, §4). A `v0.63.0` saiu em 2026-09-08 e o ritmo é de 1 a 4 meses, então a `v0.64.0` deve sair entre outubro e dezembro de 2026.
  - Pré-condição: C18 (visitor) concluído e árvore verde, porque vários PRs criam tipos que o visitor precisa percorrer.
- **Ordem sugerida em lotes.** Cada lote tem PRs inteiros, cada PR vai com seus testes e build/testes ficam verdes a cada PR; dentro do lote, ordem cronológica:
  1. **Fechamento administrativo (23):**
     - 22 PRs de infra → `não-aplicável`;
     - #2595 → remover o teste portado `parse_create_table_with_inherit` (`Integration/SqlParserPostgresTests1.cs`).
  2. **Keywords (2):** regenerar `Keywords.g.cs` e depois portar #2480 e #2448.
  3. **Dialetos e parser pontual, sem mudar a forma da AST (16):** #2490, #2458, #2536, #2535, #2493, #2502, #2518, #2561, #2539, #2526, #2584, #2546, #2558, #2560, #2552, #2555.
  4. **Display/`ToString` (10, ordem estrita):** #2531 → #2533 → #2532 → #2538 → #2544 → #2545 → #2542 → #2537 → #2541 → #2543. Todos mexem no mesmo `match` de `Expr::UnaryOp` ou no `Display` de `Value`, e o #2541 reescreve o auxiliar criado no #2532.
  5. **AST aditiva (6):** #2488 → #2553 → #2489 (mesma função `parse_data_type`), depois #2569, #2515 e #2574.
  6. **Quebras de API e cadeia do `IN` (6):** #2403, #2540, #2510, depois #2586 → #2619 → #2598 (mesma função `parse_in`). Concentrar as quebras num lote facilita a nota de versão do pacote.

  Cuidados com essa ordem:
  - Os lotes saem da ordem cronológica, então `portedThroughCommit` só avança até o maior prefixo cronológico já concluído (na prática, no fim do lote 6).
  - Teste novo entra **na posição que ocupa no arquivo Rust em `dacdbc9`**, não no fim do arquivo, para manter a ordem 1:1.
  - Para ler o Rust novo sem checkout, use `git -C upstream/sqlparser-rs show <hash>` ou `show dacdbc9:<arquivo>`.
- **Principais riscos:**
  1. **Quebra da API pública do Core.**
     - Campo novo em record posicional: `Insert.ByName` (#2403) e `Expr.InList`/`Expr.InSubquery.Global` (#2598).
     - Tipo de campo trocado: `Table.TableName`/`SchemaName` de `string?` para `Ident?` (#2540).
     - Assinatura pública nova: `Parser.ParseIn(expr, negated, global)` (#2598).
     - Membros novos no meio de enums C#: `Keyword` (#2480 e #2448) e `MapBracketKind.ParenthesesNotNull` (#2489).
     - Testes já portados que constroem esses nós deixam de compilar e precisam ser atualizados no mesmo passo.
  2. **AST diferente para a mesma entrada.** `S*?` no `MATCH_RECOGNIZE` (#2510), `OBJECT(...)` em todos os dialetos (#2489), `TRIM` com `BOTH` na forma com vírgula (#2493), `IN ((SELECT 1), 2)` (#2586) e a precedência de `IN expr` sem parênteses no ClickHouse (#2598).
  3. **Saída de `ToString` alterada** em 12 PRs: os 10 de Display, mais #2535 e #2558. Afeta quem compara SQL regenerado.
  4. **Tokens diferentes** para `N'...'` no SQLite (#2561) e `t.1` no ClickHouse (#2574). Pontos de Unicode e divergências:
     - #2555: SQLite com caracteres acima de U+FFFF. Conferir contra a DIV-003. As metades de um par substituto são ≥ U+0080 e, por isso, devem passar.
     - #2546: nova comparação `to_lowercase() == "from"`, a acrescentar à DIV-001.
  5. **Testes de desempenho e de visitor.**
     - #2586 e #2619 acrescentam memorizações contra tempo 2^N.
     - Um teste do #2619 roda numa thread com pilha de 8 MB e timeout de 5 s: atenção ao net48 e à DIV-005.
     - Outro teste do #2619 exige `#[cfg(feature = "visitor")]` e depende do C18.

### Ledger

| PR | Commit | Título | Área | Status | Observação |
|---|---|---|---|---|---|
| #2480 | `25565b2` | Redshift: Support APPROXIMATE PERCENTILE_DISC | keywords, dialeto (trait, Redshift), parser | portado | `APPROXIMATE PERCENTILE_DISC(...)` vira `Expr.Prefixed`. Traz a flag `supports_approximate_percentile_disc` e a keyword `APPROXIMATE`. P · risco baixo **Portado (lote 2):** `Keywords.g.cs` (regenerado), `Dialect.cs`, `RedshiftSqlDialect.cs`, `Parser.Part1b.cs` (`ParseExprPrefixByReservedWord`); teste `parse_approximate_percentile_disc` (`SqlParserRedshiftTests`). |
| #2490 | `86fb33d` | Postgres: Fix parsing of COLLATE after :: cast | parser | portado | Braço `COLLATE` em `parse_infix`: `expr::tipo COLLATE c`, que antes dava "No infix parser". P · baixo **Portado (lote 3):** `Parser.Part1d.cs` (`ParseInfix`); teste `parse_collate_after_cast` (`SqlParserPostgresTests1`). |
| #2496 | `b595062` | Add NOTICE file | infra (ASF) | não-aplicável | O nosso `NOTICE` já reproduz o `NOTICE.TXT` (ADR-0003) |
| #2403 | `9296011` | Databricks: support INSERT BY NAME | AST (`Insert`), Display, spans, parser, dialeto (Snowflake) | portado | Campo `by_name` em `Insert`: `BY NAME` depois das colunas/partição, sem gate de dialeto. Altera testes postgres que constroem `Insert`. P · **alto (API)**: parâmetro novo no record posicional **Portado (lote 6):** `Insert.cs` (`ByName` depois de `Columns`, e `Display`), `Spans.Part2.cs` (comentário do `let`), `SnowflakeDialect.cs` (`ParseMultiTableInsert`: `ByName: false`), `Parser.Part5b.cs` (`ParseInsert`: `!PeekKeywords(BY, NAME)` no alias e `byName`); visitor regenerado; testes `parse_insert_by_name` (`SqlParserCommonTests5`), `test_databricks_insert_by_name` (`SqlParserDatabricksTests`) e `parse_insert_by_name_keywords_as_table_and_alias` (`SqlParserPostgresTests3`); `ByName: false` em `test_simple_postgres_insert_with_alias` e `test_simple_insert_with_quoted_alias` (`SqlParserPostgresTests2`), como no PR (a variante `bigdecimal` continua não portada, DIV-006). |
| #2520 | `1195239` | Fuzz: Replace honggfuzz harness with cargo-fuzz | infra (fuzz) | não-aplicável | Harness de fuzzing do Rust |
| #2531 | `3fd342f` | PostgreSQL: Render prefix @ operator with a space | Display | portado | `@a` passa a sair `@ a` (`PGAbs`). Altera teste existente do postgres. P · médio (`ToString`) **Portado (lote 4):** `Expr.Display.cs` (`PGAbs` na lista que sai com espaço); `parse_pg_unary_ops` alterado (`SqlParserPostgresTests1`) e teste `parse_pg_abs_space_before_negative_operand` (`SqlParserPostgresTests3`). |
| #2533 | `0718147` | Render prefix ~ operator with a space | Display | portado | `match` exaustivo em `Expr::UnaryOp`: `~a` vira `~ a`. Altera teste existente do common. P · médio **Portado (lote 4):** `Expr.Display.cs` (o braço `UnaryOp` virou um `switch` exaustivo, com `default: throw`); `parse_generic_unary_ops` alterado (`SqlParserCommonTests5`; o `let select` repetido fora do laço virou `bitwiseNotSelect`, caso 72) e testes `parse_bitwise_not_renders_apart_from_operand` (`SqlParserCommonTests5`) e `parse_bitwise_not_before_pg_prefix_operators` (`SqlParserPostgresTests3`). |
| #2532 | `7449e27` | Fix unary minus rendering into -- line comments | Display | portado | `-(-a)` deixa de sair `--a`, que viraria comentário. Cria o auxiliar `starts_with_operator_char`, reescrito no #2541. P · médio **Portado (lote 4):** `Expr.Display.cs` (braço `Minus`); `AstModule.A3.cs` (`StartsWithOperatorChar`, com a classe privada `FirstChar : IFmtWrite`, removida no #2541); testes `parse_unary_minus_never_renders_line_comment` (`SqlParserCommonTests5`) e `parse_unary_minus_before_pg_prefix_operators` (`SqlParserPostgresTests3`). |
| #2527 | `f8c94d2` | Fuzz: Wire up ClusterFuzzLite CI fuzzing | infra (CI, fuzz) | não-aplicável | CI do upstream |
| #2528 | `d6afa01` | Fuzz: Display roundtrip | infra (fuzz) | não-aplicável | Alvo de fuzz em Rust; a ideia do round-trip pode servir ao ADR-0005 |
| #2458 | `cd5ca0b` | Separate the LAMBDA keyword syntax from the -> lambda syntax | dialeto (trait), parser | portado | Novo `supports_lambda_keyword_syntax`, que por padrão devolve `supports_lambda_functions`. O teste novo de `derive_dialect` deriva de PostgreSQL (conferir a DIV-002). Altera teste do duckdb. P · baixo **Portado (lote 3):** `Dialect.cs`, `Parser.Part1b.cs`; 3 testes novos em `SqlParserCustomDialectTests`, `test_lambda_keyword_syntax_with_json_arrow_operator` (`SqlParserDeriveDialectTests`, sem `Skip`: a DIV-002 não se manifesta nessa entrada) e `test_duckdb_lambda_function` alterado (`SqlParserDuckDbTests`). |
| #2530 | `4486a79` | Fuzz: Add curated seed corpus | infra (fuzz) | não-aplicável | Corpus de fuzz (~190 `.sql`) |
| #2540 | `64fa70b` | Fix `TABLE` command identifier quoting and token consumption | AST (`Table`), parser | portado | `Table.table_name` e `schema_name` passam de `Option<String>` a `Option<Ident>`. `parse_as_table` foi reescrito: preserva as aspas e não consome token a mais. Altera teste existente. P · **alto (API)** **Portado (lote 6):** `Table.cs` (`Ident? TableName`, `Ident? SchemaName`; sai a segunda linha da doc, como no Rust), `Parser.Part4c.cs` (`ParseAsTable` com `ParseIdentifier`/`ConsumeToken(Period)`); visitor regenerado (o `Table` passa a alcançar o gancho de `Ident`); `parse_create_table_as_table` re-portado (`SqlParserCommonTests2`) e teste `parse_table_preserves_quotes_and_trailing_tokens` (`SqlParserCommonTests5`). |
| #2538 | `27da1cc` | PostgreSQL: Fix spacing between adjacent postfix factorial operators | Display | portado | Espaço entre fatoriais pós-fixos seguidos (`a! !`). P · médio **Portado (lote 4):** `Expr.Display.cs` (braço `PGPostfixFactorial`); teste `parse_postfix_factorial_spacing` (`SqlParserPostgresTests3`). |
| #2544 | `efafbc2` | Escape quotes when displaying `Word` tokens | tokenizer (`Word`), AST (`Ident`), Display | portado | Função compartilhada `fmt_ident`. `Word` passa a escapar aspas, e `Ident` com aspas incomuns deixa de dar pânico. Teste novo em `tokenizer.rs`. P · médio **Portado (lote 4):** `AstModule.cs` (`FmtIdent`, `internal`; caso 135), `Ident.cs` e `Word.cs` (os dois `Fmt` delegam a `FmtIdent` e não lançam mais exceção com aspas incomuns); testes `test_word_display_quote_escaping` (`TokenizerTests`) e `test_snowflake_stage_name_with_escaped_quotes` (`SqlParserSnowflakeTests2`). |
| #2545 | `d398304` | PostgreSQL: Render prefix `\|/` and `\|\|/` operators with a space | Display | portado | `PGSquareRoot` e `PGCubeRoot` passam a ter espaço. Altera teste existente do postgres. P · médio **Portado (lote 4):** `Expr.Display.cs`; `parse_pg_unary_ops` alterado de novo (`SqlParserPostgresTests1`) e teste `parse_pg_roots_render_apart_from_operand` (`SqlParserPostgresTests3`). |
| #2542 | `80751ed` | Generic: Fix quote escaping when displaying hex string literals | Display (`Value`) | portado | `X'...'` passa a escapar `'`. Teste novo em `value.rs`. P · médio **Portado (lote 4):** `Value.cs` (`HexStringLiteral`); testes `test_hex_string_literal_display_escaping` (`Ast/ValueTests`) e `parse_hex_string_literal_display_escaping` (`SqlParserCommonTests5`). |
| #2536 | `f354ee2` | Snowflake: Fix stage delimiter handling and gate stage table factor on dialect | dialeto (trait, Snowflake), parser | portado | Flag `supports_stages`: `FROM @stage` só no Snowflake (antes, em todos). O nome de stage termina em `,`/`;` e não pode ser vazio. P · médio **Portado (lote 3):** `Dialect.cs`, `SnowflakeDialect.cs` (`SupportsStages`, `ParseStageNameIdentifier`), `Parser.Part4d.cs` (`ParseTableFactor`); testes `parse_stage_table_factor` (`SqlParserCommonTests5`), `parse_stage_table_factor_rejected` (`SqlParserPostgresTests3`) e `test_stage_name_delimiters` (`SqlParserSnowflakeTests2`). |
| #2537 | `2f029eb` | Generic: Fix quote escaping when displaying byte and raw string literals | Display (`Value`) | portado | `B'..'`, `B".."`, `R'..'` e `R".."` passam a escapar aspas. Teste novo em `value.rs`. P · médio **Portado (lote 4):** `Value.cs` (4 variantes); testes `test_byte_and_raw_string_literal_display_escaping` (`Ast/ValueTests`), `test_byte_and_raw_string_quote_escaping` (`SqlParserBigQueryTests`) e `parse_bitstring_literal_escaping` (`SqlParserMySqlTests2` e `SqlParserPostgresTests3`). |
| #2517 | `0cb62a1` | Add dialect labeler | infra (CI) | não-aplicável | GitHub Actions |
| #2548 | `c03018d` | CI: Run push workflows only on main and release branches | infra (CI) | não-aplicável | GitHub Actions |
| #2547 | `3255202` | CI: Cancel superseded pull request runs | infra (CI) | não-aplicável | GitHub Actions |
| #2535 | `9de1ad1` | ClickHouse: Support FIRST \| AFTER column position in ALTER TABLE | dialeto (trait, ClickHouse, Generic, MySQL), parser, Display | portado | Flag `supports_alter_column_position` no lugar de `dialect_of!`. `AFTER` passa a exibir o `Ident` com as aspas. P · médio **Portado (lote 3):** `MySQLColumnPosition.cs` (`Display`), `Dialect.cs`, `ClickHouseDialect.cs`, `GenericDialect.cs`, `MySqlDialect.cs`, `Parser.Part5d.cs` (`ParseColumnPosition`); testes `parse_alter_table_column_position` em `SqlParserClickHouseTests`, `SqlParserCommonTests5` e `SqlParserMySqlTests2`. |
| #2565 | `2d57723` | CI: Add Codecov coverage job with cargo-llvm-cov | infra (CI) | não-aplicável | CI e cobertura do upstream |
| #2562 | `9c5e22f` | Differential Fuzzing (1/3): Check `SQLiteDialect` against SQLite's own parser | infra (fuzz) | não-aplicável | Fuzz diferencial em Rust |
| #2563 | `4a609e7` | Differential Fuzzing (2/3): Check `PostgreSqlDialect` against PostgreSQL's own grammar | infra (fuzz) | não-aplicável | Fuzz diferencial em Rust |
| #2564 | `9ae00e7` | Differential Fuzzing (3/3): Check `DuckDbDialect` against DuckDB's own parser | infra (fuzz) | não-aplicável | Fuzz diferencial em Rust |
| #2488 | `667e81d` | Snowflake: Parse structured ARRAY types | AST (`ArrayElemTypeDef`), dialeto (trait, ClickHouse, Snowflake), parser | portado | Variante `ParenthesisNotNull` e flags `supports_array_typedef_with_parentheses`/`supports_array_element_not_null`. O ClickHouse deixa de usar `dialect_of!`. M · baixo **Portado (lote 5):** `ArrayElemTypeDef.cs` (`ParenthesisNotNull`), `DataType.cs` (`Display`), `Dialect.cs` (2 flags), `ClickHouseDialect.cs`, `SnowflakeDialect.cs`, `Parser.Part3c.cs` (`ParseDataTypeHelper`, braço `ARRAY`; sai o `_dialect is ClickHouseDialect`); visitor regenerado; testes `reject_angle_bracket_array_type` (`SqlParserClickHouseTests`) e `test_structured_array_type` (`SqlParserSnowflakeTests2`). |
| #2493 | `ae67547` | Postgres: Support TRIM FROM syntax | parser | portado | Aceita `TRIM([BOTH] FROM expr[, chars])`, e a forma com vírgula passa a guardar `trim_where`. Altera `parse_trim` (common). P · médio **Portado (lote 3):** `Parser.Part1c.cs` (`ParseTrimExpr`); `parse_trim` alterado (`SqlParserCommonTests3`) e teste `parse_trim_from_without_characters` (`SqlParserPostgresTests3`). |
| #2497 | `6862e71` | Replace benchmark flamegraph.svg with png | infra (bench) | não-aplicável | Imagem do benchmark |
| #2550 | `2b39e3b` | Run the dialect labeler on pull request pushes and reopens | infra (CI) | não-aplicável | GitHub Actions |
| #2502 | `f45d254` | Snowflake: parse ORDER BY ALL | dialeto (Snowflake) | portado | Só `supports_order_by_all = true`. Os testes já portados que usam essa flag passam a cobrir o Snowflake. P · baixo **Portado (lote 3):** `SnowflakeDialect.cs`; sem teste novo. |
| #2510 | `0fd4fa8` | Snowflake: preserve reluctant MATCH_RECOGNIZE quantifiers | AST (`RepetitionQuantifier`), parser | portado | Variante `Reluctant`. `S*?` vira `Repetition(S, Reluctant(ZeroOrMore))` (antes, repetição aninhada), e quantificadores empilhados (`S**`) passam a dar erro. Altera teste existente. M · **alto** (outra AST para a mesma entrada) **Portado (lote 6):** `RepetitionQuantifier.cs` (`Reluctant(RepetitionQuantifier Value)` e `Display`), `Parser.Part5a.cs` (`ParseRepetitionPattern` sem o laço; o `let quantifier` sombreado virou reatribuição, caso 72); visitor regenerado; `test_match_recognize_patterns` re-portado (`SqlParserCommonTests4`). |
| #2518 | `220e50a` | Snowflake: parse nested COPY transformation paths | dialeto (Snowflake, função auxiliar) | portado | `parse_select_item_for_data_load` devolve ao parser de expressões os caminhos com `:`, `.` e `[`. Estende teste existente. P · baixo **Portado (lote 3):** `SnowflakeDialect.cs` (`ParseSelectItemForDataLoad`); `test_copy_into_with_cast_transformation` estendido (`SqlParserSnowflakeTests2`). |
| #2561 | `f724399` | SQLite: Do not tokenize `N'...'` as a national string literal | tokenizer, dialeto (trait, SQLite) | portado | Flag `supports_national_string_literal`: padrão `true`, `false` no SQLite. Altera teste do tokenizer e do common. P · médio (tokens do SQLite) **Portado (lote 3):** `Dialect.cs`, `SQLiteDialect.cs`, `Tokenizer.cs`; alterados `test_national_strings_backslash_escape_not_supported` (`TokenizerTests`), `parse_listagg` (`SqlParserCommonTests1`) e `parse_literal_string` (`SqlParserCommonTests2`); teste `parse_n_prefix_not_national_string` (`SqlParserSQLiteTests`). |
| #2539 | `1608bf6` | Generic: Disallow quoted identifiers in colon and at-sign placeholders | parser | portado | `:"x"` e `@"x"` deixam de ser placeholder e passam a dar erro. P · médio **Portado (lote 3):** `Parser.Part3c.cs` (`ParseValue`); teste `parse_placeholder_disallows_quoted_ident` (`SqlParserCommonTests5`). |
| #2526 | `712de6e` | Refactor: replace `dialect_of!` table partition check with `Dialect` trait method | dialeto (trait, Generic, MySQL), parser | portado | Flag `supports_table_partitions`, sem mudança de comportamento. P · baixo **Portado (lote 3):** `Dialect.cs`, `GenericDialect.cs`, `MySqlDialect.cs`, `Parser.Part4d.cs` (`ParseTableFactor`); teste `custom_dialect_supports_table_partitions` (`SqlParserCustomDialectTests`). |
| #2569 | `88ffee6` | ClickHouse: Support ALTER TABLE ... MODIFY ORDER BY | AST (`AlterTableOperation`), Display, spans, parser | portado | Variante `ModifyOrderBy`; `parse_sorting_key` foi extraído. Teste novo em `spans.rs`. P · baixo **Portado (lote 5):** `AlterTableOperation.cs` (`ModifyOrderBy` e `Display`), `Spans.Part2.cs`, `Parser.Part2c.cs` (`ParseCreateTable` usa `ParseSortingKey`), `Parser.Part3a.cs` (`ParseSortingKey`, no fim), `Parser.Part3b.cs` (`ParseAlterTableOperation`); visitor regenerado; testes `test_alter_table_modify_order_by_span` (`Ast/SpansTests`) e `parse_alter_table_modify_order_by` (`SqlParserClickHouseTests`). |
| #2482 | `393c2c3` | Check in Cargo.lock | infra (Cargo, CI) | não-aplicável | `Cargo.lock` e CI; a troca para `sort_by_key` em `derive/src/dialect.rs` é interna da macro `derive_dialect!` e não tem efeito |
| #2553 | `ab23305` | Databricks: support collated data types | AST (`DataType`), dialeto (trait, Databricks), parser | portado | Variante `DataType.Collate` e `parse_data_type_with_optional_collation`, usada em STRUCT, ARRAY e MAP. A chave de `MAP<...>` seguida de `>>` passa a dar erro explícito. M · baixo **Portado (lote 5):** `DataType.cs` (`Collate(DataType DataType, ObjectName Collation)`, nomes em `nomes.md`), `Dialect.cs` (`SupportsDataTypeCollation`), `DatabricksDialect.cs`, `Parser.Part1d.cs` (`ParseStructFieldDef`), `Parser.Part3c.cs` (`ARRAY<..>`, `MAP<..>` e `ParseDataTypeWithOptionalCollation`); visitor regenerado; teste `parse_databricks_collated_data_types` (`SqlParserDatabricksTests`). |
| #2581 | `9352883` | Enable GitHub Discussions | infra (ASF) | não-aplicável | `.asf.yaml` |
| #2583 | `ddb9b92` | Ignore libFuzzer worker logs in fuzz/ | infra (fuzz) | não-aplicável | `.gitignore` do fuzz |
| #2582 | `37109d4` | Remove homepage metadata duplicating repository | infra (Cargo) | não-aplicável | Metadados do `Cargo.toml` |
| #2515 | `c23dc05` | PostgreSQL: Support CREATE FOREIGN TABLE | AST (novo `CreateForeignTable`, `Statement`), Display, spans, visitor, parser | portado | Tipo e variante novos; `parse_pg_options_clause` foi extraído de `CREATE SERVER`. `parse_identifier` passa a dar span ao `Ident` que vem de string. O visitor percorre `name` com `visit_relation` (C18). Teste novo em `spans.rs`. G · médio **Portado (lote 5):** `Ast/Ddl/CreateForeignTable.cs` (novo, com `Display` e `Span()`), `Statement.cs`, `Statement.Display.cs`, `Statement.From.Ddl.cs`, `Spans.Part1.cs`, `CreateServerOption.cs` (doc), `Parser.Part2a.cs` (`ParseCreate`), `Parser.Part4a.cs` (`ParseIdentifier`: span no `Ident` que vem de string), `Parser.Part5d.cs` (`ParsePgOptionsClause` e `ParseCreateForeignTable`); visitor regenerado (`Name` com `visit_relation`); testes `test_create_foreign_table_option_spans` (`Ast/SpansTests`) e os 4 de `CREATE FOREIGN TABLE` (`SqlParserPostgresTests3`, depois de `parse_lock_table`). |
| #2489 | `37df75e` | Snowflake: Parse structured OBJECT types | AST (`DataType`, `MapBracketKind`), dialeto (trait, ClickHouse, Generic, Snowflake), parser | portado | Variante `DataType.Object` e membro `MapBracketKind.ParenthesesNotNull` (no meio do enum C#). `OBJECT(...)` não tem gate de dialeto. `parse_click_house_map_def` passa a se chamar `parse_parenthesized_map_type_def`. M · médio **Portado (lote 5):** `DataType.cs` (`Object` e `Display`), `MapBracketKind.cs` (`ParenthesesNotNull` no meio do enum: `AngleBrackets` passa de 1 a 2), `Dialect.cs` (2 flags), `ClickHouseDialect.cs`, `GenericDialect.cs`, `SnowflakeDialect.cs`, `Parser.Part1d.cs` (`ParseParenthesizedMapTypeDef`, antes `ParseClickHouseMapDef`; nomes em `nomes.md`), `Parser.Part3c.cs` (braços `OBJECT` e `MAP`), `Parser.Part4a.cs` (`ParseStructuredObjectTypeDef`); visitor regenerado; testes `parse_object_type_parameter` (`SqlParserClickHouseTests`) e `test_structured_object_type` (`SqlParserSnowflakeTests2`). |
| #2574 | `91f7ce2` | ClickHouse: Support tuple element access by index | tokenizer, dialeto (trait, ClickHouse), parser | portado | Flag `supports_tuple_element_access`: no ClickHouse, `t.1` vira `Word`, `.` e `Number` (antes, `Number(".1")`). Teste novo em `tokenizer.rs`. M · médio (tokens do ClickHouse) **Portado (lote 5):** `Dialect.cs` (`SupportsTupleElementAccess`), `ClickHouseDialect.cs`, `Tokenizer.cs` (braço de números e ponto), `Parser.Part1a.cs` (`ParseWildcardExpr`: o `break` do laço dentro do `match` virou a flag `stop`, caso 100); testes `tokenize_period_before_digits_after_identifier_or_bracket` (`TokenizerTests`) e `parse_tuple_element_access` (`SqlParserClickHouseTests`), este com `Skip = "DIV-006: ..."`: a asserção `SELECT .5, 1e3` → `SELECT 0.5, 1000` depende da feature `bigdecimal`, ligada no CI do upstream (`--all-features`); as demais asserções passam. |
| #2448 | `560a13c` | MSSQL: Support PROC as CREATE PROCEDURE abbreviation | keywords, dialeto (trait, MsSql), parser | portado | Keyword `PROC` e flag `supports_create_proc_syntax`. P · baixo; o enum `Keyword` é renumerado **Portado (lote 2):** `Keywords.g.cs` (regenerado), `Dialect.cs`, `MsSqlDialect.cs`, `Parser.Part2a.cs` (`ParseCreate`); teste `parse_create_proc` (`SqlParserMsSqlTests`). |
| #2595 | `5cad906` | PostgreSQL: Remove test `parse_create_table_with_inherit` | testes | portado | Remover o teste portado (`SqlParserPostgresTests1.cs`); o postgres passa de 201 para 200 testes. P · nenhum **Portado (lote 1):** teste removido de `SqlParserPostgresTests1.cs`. |
| #2602 | `287aaf6` | Bump bigdecimal from 0.4.10 to 0.4.11 | infra (Cargo) | não-aplicável | Dependência Rust; o Core é compilado sem `bigdecimal` (DIV-006) |
| #2601 | `811ef29` | Update SECURITY policy to conform to ASF guidelines | infra (docs) | não-aplicável | Política de segurança do projeto upstream |
| #2585 | `d405abe` | Gate fuzz oracle targets behind features | infra (fuzz) | não-aplicável | Fuzz em Rust |
| #2584 | `c2e085d` | DuckDB: Support nested block comments | dialeto (DuckDB) | portado | `supports_nested_comments = true`: muda os tokens de `/* /* */ */` no DuckDB. P · baixo **Portado (lote 3):** `DuckDbDialect.cs`; teste `test_duckdb_nested_block_comments` (`SqlParserDuckDbTests`). |
| #2541 | `bfc7d38` | Generic: Render prefix `!` operator with space before operator characters | Display | portado | `starts_with_operator_char` passa a ser estrutural, sem renderizar a expressão, e vale para `-` e `!`. P · médio **Portado (lote 4):** `Expr.Display.cs` (`Minus | BangNot`), `AstModule.A3.cs` (`StartsWithOperatorChar` estrutural; `FirstChar` removida); teste `parse_bang_not_renders_apart_from_operand` (`SqlParserCommonTests5`, no fim: os 2 testes do #2586 entram antes dele). |
| #2546 | `8f4da49` | MsSql: Disallow unquoted `FROM` keyword as alias assignment expression | parser | portado | `SELECT a = FROM t` passa a dar erro. Traz nova comparação `to_lowercase() == "from"` (acrescentar à DIV-001). Estende teste existente. P · médio **Portado (lote 3):** `Parser.Part5b.cs` (`ParseSelectItem`, com `PORT-DEVIATION DIV-001`; ponto acrescentado à DIV-001); `test_alias_equal_expr` estendido (`SqlParserCommonTests4`) e teste `parse_bracket_quoted_eq_alias_assignment` (`SqlParserMsSqlTests`). |
| #2558 | `aa1b5d6` | SQLite: Support `CAST(expr AS)` without a type name | dialeto (trait, SQLite), parser, Display | portado | Usa `DataType.Unspecified` no `CAST`, exibido como `CAST(x AS)`. P · baixo **Portado (lote 3):** `Expr.Display.cs`, `Dialect.cs`, `SQLiteDialect.cs`, `Parser.Part1c.cs` (`ParseCastExpr`); teste `test_cast_empty_type` (`SqlParserSQLiteTests`). |
| #2543 | `b1d1486` | Generic: Add spacing around dot in compound field access adjacent to numbers | Display | portado | Põe espaço em volta do `.` de `CompoundFieldAccess`/`AccessExpr` quando há número ao lado. P · médio **Portado (lote 4):** `AstModule.cs` (`IsNumberExpr`; caso 135), `AccessExpr.cs` e `Expr.Display.cs` (`CompoundFieldAccess`); teste `parse_compound_field_access_numeric_display` (`SqlParserCommonTests5`). |
| #2560 | `fc18670` | SQLite: Support string literals as column names in `CREATE TABLE` | dialeto (trait, SQLite), parser | portado | Flag `supports_string_literal_column_names`. P · baixo **Portado (lote 3):** `Dialect.cs`, `SQLiteDialect.cs`, `Parser.Part3a.cs` (`ParseColumns`); teste `parse_create_table_string_column_names` (`SqlParserSQLiteTests`). |
| #2552 | `7939d4b` | SQLite: support == as assignment operator in UPDATE SET | dialeto (trait, SQLite), parser | portado | Flag `supports_double_eq_assignment`. P · baixo **Portado (lote 3):** `Dialect.cs`, `SQLiteDialect.cs`, `Parser.Part5b.cs` (`ParseAssignment`); teste `parse_update_set_double_eq` (`SqlParserSQLiteTests`). |
| #2586 | `1ba145b` | Generic: Parse IN lists that start with a parenthesized subquery | parser | portado | `parse_in` foi reescrito: `IN ((SELECT 1), 2)` vira `InList`. Novo campo de memorização `failed_in_subquery_positions`, contra tempo 2^N. As mensagens de erro mudam. M · médio **Portado (lote 6):** `Parser.cs` (`_failedInSubqueryPositions`, `SortedDictionary<int, int?>`, e a limpeza em `WithTokensWithLocations`), `Parser.Part1d.cs` (`ParseIn`; o `match` com `and_then` virou o caso 138); testes `parse_in_list_leading_subquery` e `parse_in_list_nested_leading_subquery_no_exponential_blowup` (`SqlParserCommonTests5`, antes de `parse_bang_not_renders_apart_from_operand`, que ganhou os auxiliares `VerifiedQuery` e `PgAndGeneric`). |
| #2580 | `c70b5b1` | Add a fuzz target that flags superlinear parsing and printing | infra (fuzz) | não-aplicável | Fuzz em Rust |
| #2555 | `14cbf75` | SQLite: Support identifier characters above U+FFFF | dialeto (SQLite) | portado | `is_identifier_start` passa a aceitar `ch >= U+0080`; U+007F deixa de ser aceito. Conferir contra a DIV-003. O teste tem caracteres fora do BMP: usar escapes e rodar `Find-SuspiciousChars.ps1`. P · baixo **Portado (lote 3):** `SQLiteDialect.cs` (`IsIdentifierStart`: o teste `>= U+0080` aceita as duas metades de um par substituto, o que dá o resultado do Rust; caso 14 e DIV-003 atualizados); teste `test_non_bmp_identifiers` (`SqlParserSQLiteTests`, com os caracteres literais do `.rs`). |
| #2619 | `e790703` | Parser: fix exponential parse time on nested POSITION calls | parser | portado | Memorização `failed_position_syntax_positions` com chave `(índice, ParserState)`. Um teste roda em thread de 8 MB com timeout de 5 s (DIV-005); outro exige `visitor` (C18). P · baixo **Portado (lote 6):** `ParserSupportTypes.cs` (o derive novo de `ParserState` não tem tradução, caso 137), `Parser.cs` (`_failedPositionSyntaxPositions`, `SortedSet<(int, ParserState)>`, antes de `_failedInSubqueryPositions`, como no Rust), `Parser.Part1c.cs` (`ParsePositionExpr`); testes `parse_position_chain_no_exponential_blowup` (o auxiliar `SpawnAndRecvTimeout` ganhou `stackSize`; passa nos três alvos, sem `Skip`), `parse_position_forms_with_nested_fallback` e `parse_position_in_connect_by_list_after_state_change` (`#[cfg(feature = "visitor")]`; roda, porque o visitor existe), no fim de `SqlParserCommonTests5`. |
| #2598 | `dacdbc9` | ClickHouse: Support GLOBAL IN / GLOBAL NOT IN | AST (`Expr`), Display, spans, dialeto (trait, ClickHouse, precedência), parser | portado | Campo `global` em `Expr.InList` e `Expr.InSubquery`; nova assinatura `Parser.ParseIn(expr, negated, global)`. `IN expr` sem parênteses passa a usar `parse_subexpr(prec Eq)`. Altera testes do common. M · **alto (API)** **Portado (lote 6):** `Expr.cs` (`Global` no fim de `InList` e `InSubquery`), `Expr.Display.cs`, `Spans.Part2.cs` (comentários), `Dialect.cs` (`SupportsGlobalIn`, depois de `SupportsInUnparenthesizedExpr`; braço `GLOBAL` em `GetNextPrecedenceDefault`, antes de `IN`), `ClickHouseDialect.cs`, `Parser.Part1d.cs` (`ParseInfix`: braço `GLOBAL`; `ParseIn(expr, negated, global)`); visitor regenerado; testes `parse_global_in`, `parse_in_unparenthesized_expr_precedence`, `global_in_bare_table_binds_before_and` e `global_in_bare_table_binds_before_equality` (`SqlParserClickHouseTests`, antes das funções auxiliares); `Global: false` em `parse_not_precedence`, `parse_in_list`, `parse_in_subquery`, `parse_in_union` (`SqlParserCommonTests1`) e `parse_in_list_leading_subquery` (`SqlParserCommonTests5`), como no PR. O `fuzz_seeds/*.sql` do PR é infraestrutura. |

### Detalhamento: arquivos tocados e testes

Linhas por commit (`git show --numstat`). Os caminhos de código são relativos a `src/`, e os de teste a `tests/`. Nos PRs de infra, os arquivos são de fora de `src/` e `tests/`.

| PR | Arquivos tocados (+/−) | Testes (+/−; `#[test]` novos) |
|---|---|---|
| #2480 | `dialect/mod.rs` +6/−0, `dialect/redshift.rs` +5/−0, `keywords.rs` +1/−0, `parser/mod.rs` +12/−0 | `sqlparser_redshift.rs` +16/−0 (1 novo) |
| #2490 | `parser/mod.rs` +7/−0 | `sqlparser_postgres.rs` +13/−0 (1 novo) |
| #2496 | (infra) `NOTICE.TXT` +5, `Cargo.toml` +1 | — |
| #2403 | `ast/dml.rs` +19/−0, `ast/spans.rs` +1/−0, `dialect/snowflake.rs` +1/−0, `parser/mod.rs` +5/−0 | `sqlparser_common.rs` +45/−0 (1 novo), `sqlparser_databricks.rs` +8/−0 (1 novo), `sqlparser_postgres.rs` +22/−0 (1 novo; acrescenta `by_name` em `test_simple_postgres_insert_with_alias`, inclusive na variante `bigdecimal` não portada, e em `test_simple_insert_with_quoted_alias`) |
| #2520 | (infra) `fuzz/**` +34/−31, `docs/fuzzing.md` +4/−31 | — |
| #2531 | `ast/mod.rs` +1/−0 | `sqlparser_postgres.rs` +21/−6 (1 novo; altera `parse_pg_unary_ops`) |
| #2533 | `ast/mod.rs` +17/−18 | `sqlparser_common.rs` +17/−5 (1 novo; altera `parse_generic_unary_ops`), `sqlparser_postgres.rs` +7/−0 (1 novo) |
| #2532 | `ast/mod.rs` +27/−1 | `sqlparser_common.rs` +8/−0 (1 novo), `sqlparser_postgres.rs` +9/−0 (1 novo) |
| #2527 | (infra) `.clusterfuzzlite/*` +75, `.github/workflows/cflite_*.yml` +181, `docs/fuzzing.md` +4 | — |
| #2528 | (infra) `fuzz/fuzz_targets/fuzz_parse_roundtrip.rs` +61, `fuzz/**` +11/−2, `docs/fuzzing.md` +4 | — |
| #2458 | `dialect/mod.rs` +24/−0, `parser/mod.rs` +1/−1 | `sqlparser_custom_dialect.rs` +117/−0 (3 novos), `sqlparser_derive_dialect.rs` +69/−0 (1 novo), `sqlparser_duckdb.rs` +15/−0 (altera `test_duckdb_lambda_function`) |
| #2530 | (infra) `fuzz/fuzz_seeds/*.sql` (~190 arquivos), `.gitattributes`, `dev/release/*`, `.github/workflows/license.yml` (+242/−2) | — |
| #2540 | `ast/query.rs` +3/−4, `parser/mod.rs` +6/−34 | `sqlparser_common.rs` +44/−3 (1 novo; altera `parse_create_table_as_table`) |
| #2538 | `ast/mod.rs` +13/−1 | `sqlparser_postgres.rs` +18/−0 (1 novo) |
| #2544 | `ast/mod.rs` +16/−9, `tokenizer.rs` +32/−7 (com 1 teste interno novo) | `sqlparser_snowflake.rs` +7/−0 (1 novo) |
| #2545 | `ast/mod.rs` +6/−6 | `sqlparser_postgres.rs` +22/−2 (1 novo; altera `parse_pg_unary_ops`) |
| #2542 | `ast/value.rs` +13/−1 (com 1 teste interno novo) | `sqlparser_common.rs` +7/−0 (1 novo) |
| #2536 | `dialect/mod.rs` +7/−0, `dialect/snowflake.rs` +9/−6, `parser/mod.rs` +1/−1 | `sqlparser_common.rs` +15/−0 (1 novo), `sqlparser_postgres.rs` +9/−0 (1 novo), `sqlparser_snowflake.rs` +40/−0 (1 novo) |
| #2537 | `ast/value.rs` +49/−4 (com 1 teste interno novo) | `sqlparser_bigquery.rs` +27/−0 (1 novo), `sqlparser_mysql.rs` +6/−0 (1 novo), `sqlparser_postgres.rs` +6/−0 (1 novo) |
| #2517 | (infra) `.github/workflows/labeler*` +186, `.github/PULL_REQUEST_TEMPLATE.md` +2, `dev/release/rat_exclude_files.txt` +2/−1 | — |
| #2548 | (infra) `.github/workflows/{audit,rust}.yml` +8/−7 | — |
| #2547 | (infra) `.github/workflows/{audit,license,rust}.yml` +15 | — |
| #2535 | `ast/mod.rs` +1/−4, `dialect/clickhouse.rs` +5/−0, `dialect/generic.rs` +4/−0, `dialect/mod.rs` +10/−0, `dialect/mysql.rs` +5/−0, `parser/mod.rs` +1/−1 | `sqlparser_clickhouse.rs` +38/−0 (1 novo), `sqlparser_common.rs` +40/−0 (1 novo), `sqlparser_mysql.rs` +65/−0 (1 novo) |
| #2565 | (infra) `.github/workflows/rust.yml` +28/−2, `codecov.yml` +19, `README.md` +1/−1, `.asf.yaml` +1 | — |
| #2562 | (infra) `fuzz/fuzz_targets/fuzz_sqlite_accepts.rs` +149, `fuzz/Cargo.toml` +8, `docs/fuzzing.md` +2 | — |
| #2563 | (infra) `fuzz/fuzz_targets/fuzz_postgres_accepts.rs` +38, `fuzz/Cargo.toml` +8, `.clusterfuzzlite/Dockerfile` +2, `docs/fuzzing.md` +2 | — |
| #2564 | (infra) `fuzz/fuzz_targets/fuzz_duckdb_accepts.rs` +210, `fuzz/Cargo.toml` +9, `.clusterfuzzlite/build.sh` +2, `docs/fuzzing.md` +2 | — |
| #2488 | `ast/data_type.rs` +5/−0, `dialect/clickhouse.rs` +4/−0, `dialect/mod.rs` +16/−0, `dialect/snowflake.rs` +9/−0, `parser/mod.rs` +23/−5 | `sqlparser_clickhouse.rs` +10/−0 (1 novo), `sqlparser_snowflake.rs` +24/−0 (1 novo) |
| #2493 | `parser/mod.rs` +16/−1 | `sqlparser_common.rs` +11/−0 (altera `parse_trim`), `sqlparser_postgres.rs` +5/−0 (1 novo) |
| #2497 | (infra) `sqlparser_bench/img/flamegraph.svg` −491, `flamegraph.png` (binário), `sqlparser_bench/README.md` +1/−1 | — |
| #2550 | (infra) `.github/workflows/labeler.yml` +1/−1 | — |
| #2502 | `dialect/snowflake.rs` +5/−0 | — (testes existentes passam a incluir o Snowflake) |
| #2510 | `ast/query.rs` +3/−0, `parser/mod.rs` +51/−48 | `sqlparser_common.rs` +35/−11 (altera `test_match_recognize_patterns`) |
| #2518 | `dialect/snowflake.rs` +8/−4 | `sqlparser_snowflake.rs` +17/−0 (estende `test_copy_into_with_cast_transformation`) |
| #2561 | `dialect/mod.rs` +8/−0, `dialect/sqlite.rs` +4/−0, `tokenizer.rs` +13/−10 (altera o teste interno `test_national_strings_backslash_escape_not_supported`) | `sqlparser_common.rs` +8/−5 (altera `parse_listagg` e `parse_literal_string`), `sqlparser_sqlite.rs` +12/−0 (1 novo) |
| #2539 | `parser/mod.rs` +1/−1 | `sqlparser_common.rs` +42/−0 (1 novo) |
| #2526 | `dialect/generic.rs` +4/−0, `dialect/mod.rs` +13/−0, `dialect/mysql.rs` +5/−0, `parser/mod.rs` +1/−1 | `sqlparser_custom_dialect.rs` +41/−0 (1 novo) |
| #2569 | `ast/ddl.rs` +11/−0, `ast/spans.rs` +18/−0 (com 1 teste interno novo), `parser/mod.rs` +21/−11 | `sqlparser_clickhouse.rs` +39/−0 (1 novo) |
| #2482 | (infra) `Cargo.lock` +893, `Cargo.toml` +3, `.github/workflows/rust.yml` +9/−9, `.gitignore` −4, `.tool-versions` +1/−1, `derive/src/dialect.rs` +1/−1, `fuzz/.gitignore` +1 | — |
| #2553 | `ast/data_type.rs` +5/−0, `dialect/databricks.rs` +4/−0, `dialect/mod.rs` +6/−0, `parser/mod.rs` +26/−4 | `sqlparser_databricks.rs` +12/−0 (1 novo) |
| #2581 | (infra) `.asf.yaml` +2 | — |
| #2583 | (infra) `fuzz/.gitignore` +1 | — |
| #2582 | (infra) `Cargo.toml` −1, `derive/Cargo.toml` −1 | — |
| #2515 | `ast/ddl.rs` +81/−9, `ast/mod.rs` +8/−2, `ast/spans.rs` +15/−0 (com 1 teste interno novo), `parser/mod.rs` +72/−12 | `sqlparser_postgres.rs` +96/−0 (4 novos) |
| #2489 | `ast/data_type.rs` +12/−0, `dialect/clickhouse.rs` +4/−0, `dialect/generic.rs` +4/−0, `dialect/mod.rs` +11/−0, `dialect/snowflake.rs` +8/−0, `parser/mod.rs` +57/−6 | `sqlparser_clickhouse.rs` +66/−0 (1 novo), `sqlparser_snowflake.rs` +71/−0 (1 novo) |
| #2574 | `dialect/clickhouse.rs` +5/−0, `dialect/mod.rs` +6/−0, `parser/mod.rs` +2/−0, `tokenizer.rs` +96/−0 (com 1 teste interno novo, ~70 linhas); (infra) `fuzz/fuzz_seeds/…` +1 | `sqlparser_clickhouse.rs` +62/−0 (1 novo) |
| #2448 | `dialect/mod.rs` +6/−0, `dialect/mssql.rs` +4/−0, `keywords.rs` +1/−0, `parser/mod.rs` +3/−1 | `sqlparser_mssql.rs` +17/−0 (1 novo) |
| #2595 | — | `sqlparser_postgres.rs` +0/−12 (remove `parse_create_table_with_inherit`) |
| #2602 | (infra) `Cargo.lock` +2/−2 | — |
| #2601 | (infra) `SECURITY.md` +36/−1 | — |
| #2585 | (infra) `fuzz/Cargo.toml` +13/−4, `.clusterfuzzlite/build.sh` +1/−1, `docs/fuzzing.md` +2 | — |
| #2584 | `dialect/duckdb.rs` +4/−0 | `sqlparser_duckdb.rs` +17/−0 (1 novo) |
| #2541 | `ast/mod.rs` +11/−20 | `sqlparser_common.rs` +17/−0 (1 novo) |
| #2546 | `parser/mod.rs` +7/−0 | `sqlparser_common.rs` +18/−0 (estende `test_alias_equal_expr`), `sqlparser_mssql.rs` +5/−0 (1 novo) |
| #2558 | `ast/mod.rs` +5/−1, `dialect/mod.rs` +12/−0, `dialect/sqlite.rs` +4/−0, `parser/mod.rs` +7/−1 | `sqlparser_sqlite.rs` +12/−0 (1 novo) |
| #2543 | `ast/mod.rs` +11/−4 | `sqlparser_common.rs` +19/−0 (1 novo) |
| #2560 | `dialect/mod.rs` +6/−0, `dialect/sqlite.rs` +4/−0, `parser/mod.rs` +4/−0 | `sqlparser_sqlite.rs` +13/−0 (1 novo) |
| #2552 | `dialect/mod.rs` +7/−0, `dialect/sqlite.rs` +4/−0, `parser/mod.rs` +7/−1 | `sqlparser_sqlite.rs` +17/−0 (1 novo) |
| #2586 | `parser/mod.rs` +49/−15 | `sqlparser_common.rs` +116/−0 (2 novos, um deles contra explosão exponencial) |
| #2580 | (infra) `fuzz/fuzz_targets/fuzz_stage_cost.rs` +124, `fuzz/Cargo.toml` +7, `docs/fuzzing.md` +3/−1 | — |
| #2555 | `dialect/sqlite.rs` +1/−4 | `sqlparser_sqlite.rs` +13/−0 (1 novo) |
| #2619 | `parser/mod.rs` +17/−2 | `sqlparser_common.rs` +70/−0 (3 novos: thread de 8 MB com timeout de 5 s; formas aninhadas; um com `#[cfg(feature = "visitor")]`) |
| #2598 | `ast/mod.rs` +12/−4, `ast/spans.rs` +2/−0, `dialect/clickhouse.rs` +5/−0, `dialect/mod.rs` +19/−0, `parser/mod.rs` +18/−5; (infra) `fuzz/fuzz_seeds/clickhouse_global_in.sql` +63 | `sqlparser_clickhouse.rs` +119/−0 (4 novos), `sqlparser_common.rs` +8/−0 (altera `parse_in_list`, `parse_in_subquery`, `parse_in_union`, `parse_not_precedence` e `parse_in_list_leading_subquery`) |
