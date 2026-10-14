# Ledger de sincronização com o upstream

Uma linha por PR do sqlparser-rs depois do baseline (`UPSTREAM.json` → `baselineTag`). **Nenhum PR fica sem registro.** "Não-aplicável" exige motivo. Procedimento: skill `sync-upstream`.

- **Baseline:** `v0.63.0` (`85b1a6f`).
- **Portado até:** `85b1a6f` (o próprio baseline: a sincronização começa depois que o port do baseline estiver concluído).
- **Último levantamento:** 2026-10-14, `origin/main` em `dacdbc9` (2026-10-12): 63 PRs pendentes, ver "Rodada 1".

As linhas ficam nas seções abaixo, uma por rodada de sincronização (alvo: tag ou commit), sempre no formato `PR | Commit | Título | Área | Status | Observação`.

## Rodada 1: `v0.63.0` → `origin/main` (`dacdbc9`)

Plano levantado em 2026-10-14 (`git fetch --tags` no clone de referência; nada portado ainda). Nesta rodada todas as linhas começam como `pendente`. As 22 de infraestrutura trazem na observação a **proposta** de `não-aplicável` e o motivo; viram `não-aplicável` quando o lote 1 for executado.

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
| #2480 | `25565b2` | Redshift: Support APPROXIMATE PERCENTILE_DISC | keywords, dialeto (trait, Redshift), parser | pendente | `APPROXIMATE PERCENTILE_DISC(...)` vira `Expr.Prefixed`. Traz a flag `supports_approximate_percentile_disc` e a keyword `APPROXIMATE`. P · risco baixo |
| #2490 | `86fb33d` | Postgres: Fix parsing of COLLATE after :: cast | parser | pendente | Braço `COLLATE` em `parse_infix`: `expr::tipo COLLATE c`, que antes dava "No infix parser". P · baixo |
| #2496 | `b595062` | Add NOTICE file | infra (ASF) | pendente | Proposta: não-aplicável. O nosso `NOTICE` já reproduz o `NOTICE.TXT` (ADR-0003) |
| #2403 | `9296011` | Databricks: support INSERT BY NAME | AST (`Insert`), Display, spans, parser, dialeto (Snowflake) | pendente | Campo `by_name` em `Insert`: `BY NAME` depois das colunas/partição, sem gate de dialeto. Altera testes postgres que constroem `Insert`. P · **alto (API)**: parâmetro novo no record posicional |
| #2520 | `1195239` | Fuzz: Replace honggfuzz harness with cargo-fuzz | infra (fuzz) | pendente | Proposta: não-aplicável. Harness de fuzzing do Rust |
| #2531 | `3fd342f` | PostgreSQL: Render prefix @ operator with a space | Display | pendente | `@a` passa a sair `@ a` (`PGAbs`). Altera teste existente do postgres. P · médio (`ToString`) |
| #2533 | `0718147` | Render prefix ~ operator with a space | Display | pendente | `match` exaustivo em `Expr::UnaryOp`: `~a` vira `~ a`. Altera teste existente do common. P · médio |
| #2532 | `7449e27` | Fix unary minus rendering into -- line comments | Display | pendente | `-(-a)` deixa de sair `--a`, que viraria comentário. Cria o auxiliar `starts_with_operator_char`, reescrito no #2541. P · médio |
| #2527 | `f8c94d2` | Fuzz: Wire up ClusterFuzzLite CI fuzzing | infra (CI, fuzz) | pendente | Proposta: não-aplicável. CI do upstream |
| #2528 | `d6afa01` | Fuzz: Display roundtrip | infra (fuzz) | pendente | Proposta: não-aplicável. Alvo de fuzz em Rust; a ideia do round-trip pode servir ao ADR-0005 |
| #2458 | `cd5ca0b` | Separate the LAMBDA keyword syntax from the -> lambda syntax | dialeto (trait), parser | pendente | Novo `supports_lambda_keyword_syntax`, que por padrão devolve `supports_lambda_functions`. O teste novo de `derive_dialect` deriva de PostgreSQL (conferir a DIV-002). Altera teste do duckdb. P · baixo |
| #2530 | `4486a79` | Fuzz: Add curated seed corpus | infra (fuzz) | pendente | Proposta: não-aplicável. Corpus de fuzz (~190 `.sql`) |
| #2540 | `64fa70b` | Fix `TABLE` command identifier quoting and token consumption | AST (`Table`), parser | pendente | `Table.table_name` e `schema_name` passam de `Option<String>` a `Option<Ident>`. `parse_as_table` foi reescrito: preserva as aspas e não consome token a mais. Altera teste existente. P · **alto (API)** |
| #2538 | `27da1cc` | PostgreSQL: Fix spacing between adjacent postfix factorial operators | Display | pendente | Espaço entre fatoriais pós-fixos seguidos (`a! !`). P · médio |
| #2544 | `efafbc2` | Escape quotes when displaying `Word` tokens | tokenizer (`Word`), AST (`Ident`), Display | pendente | Função compartilhada `fmt_ident`. `Word` passa a escapar aspas, e `Ident` com aspas incomuns deixa de dar pânico. Teste novo em `tokenizer.rs`. P · médio |
| #2545 | `d398304` | PostgreSQL: Render prefix `\|/` and `\|\|/` operators with a space | Display | pendente | `PGSquareRoot` e `PGCubeRoot` passam a ter espaço. Altera teste existente do postgres. P · médio |
| #2542 | `80751ed` | Generic: Fix quote escaping when displaying hex string literals | Display (`Value`) | pendente | `X'...'` passa a escapar `'`. Teste novo em `value.rs`. P · médio |
| #2536 | `f354ee2` | Snowflake: Fix stage delimiter handling and gate stage table factor on dialect | dialeto (trait, Snowflake), parser | pendente | Flag `supports_stages`: `FROM @stage` só no Snowflake (antes, em todos). O nome de stage termina em `,`/`;` e não pode ser vazio. P · médio |
| #2537 | `2f029eb` | Generic: Fix quote escaping when displaying byte and raw string literals | Display (`Value`) | pendente | `B'..'`, `B".."`, `R'..'` e `R".."` passam a escapar aspas. Teste novo em `value.rs`. P · médio |
| #2517 | `0cb62a1` | Add dialect labeler | infra (CI) | pendente | Proposta: não-aplicável. GitHub Actions |
| #2548 | `c03018d` | CI: Run push workflows only on main and release branches | infra (CI) | pendente | Proposta: não-aplicável. GitHub Actions |
| #2547 | `3255202` | CI: Cancel superseded pull request runs | infra (CI) | pendente | Proposta: não-aplicável. GitHub Actions |
| #2535 | `9de1ad1` | ClickHouse: Support FIRST \| AFTER column position in ALTER TABLE | dialeto (trait, ClickHouse, Generic, MySQL), parser, Display | pendente | Flag `supports_alter_column_position` no lugar de `dialect_of!`. `AFTER` passa a exibir o `Ident` com as aspas. P · médio |
| #2565 | `2d57723` | CI: Add Codecov coverage job with cargo-llvm-cov | infra (CI) | pendente | Proposta: não-aplicável. CI e cobertura do upstream |
| #2562 | `9c5e22f` | Differential Fuzzing (1/3): Check `SQLiteDialect` against SQLite's own parser | infra (fuzz) | pendente | Proposta: não-aplicável. Fuzz diferencial em Rust |
| #2563 | `4a609e7` | Differential Fuzzing (2/3): Check `PostgreSqlDialect` against PostgreSQL's own grammar | infra (fuzz) | pendente | Proposta: não-aplicável. Fuzz diferencial em Rust |
| #2564 | `9ae00e7` | Differential Fuzzing (3/3): Check `DuckDbDialect` against DuckDB's own parser | infra (fuzz) | pendente | Proposta: não-aplicável. Fuzz diferencial em Rust |
| #2488 | `667e81d` | Snowflake: Parse structured ARRAY types | AST (`ArrayElemTypeDef`), dialeto (trait, ClickHouse, Snowflake), parser | pendente | Variante `ParenthesisNotNull` e flags `supports_array_typedef_with_parentheses`/`supports_array_element_not_null`. O ClickHouse deixa de usar `dialect_of!`. M · baixo |
| #2493 | `ae67547` | Postgres: Support TRIM FROM syntax | parser | pendente | Aceita `TRIM([BOTH] FROM expr[, chars])`, e a forma com vírgula passa a guardar `trim_where`. Altera `parse_trim` (common). P · médio |
| #2497 | `6862e71` | Replace benchmark flamegraph.svg with png | infra (bench) | pendente | Proposta: não-aplicável. Imagem do benchmark |
| #2550 | `2b39e3b` | Run the dialect labeler on pull request pushes and reopens | infra (CI) | pendente | Proposta: não-aplicável. GitHub Actions |
| #2502 | `f45d254` | Snowflake: parse ORDER BY ALL | dialeto (Snowflake) | pendente | Só `supports_order_by_all = true`. Os testes já portados que usam essa flag passam a cobrir o Snowflake. P · baixo |
| #2510 | `0fd4fa8` | Snowflake: preserve reluctant MATCH_RECOGNIZE quantifiers | AST (`RepetitionQuantifier`), parser | pendente | Variante `Reluctant`. `S*?` vira `Repetition(S, Reluctant(ZeroOrMore))` (antes, repetição aninhada), e quantificadores empilhados (`S**`) passam a dar erro. Altera teste existente. M · **alto** (outra AST para a mesma entrada) |
| #2518 | `220e50a` | Snowflake: parse nested COPY transformation paths | dialeto (Snowflake, função auxiliar) | pendente | `parse_select_item_for_data_load` devolve ao parser de expressões os caminhos com `:`, `.` e `[`. Estende teste existente. P · baixo |
| #2561 | `f724399` | SQLite: Do not tokenize `N'...'` as a national string literal | tokenizer, dialeto (trait, SQLite) | pendente | Flag `supports_national_string_literal`: padrão `true`, `false` no SQLite. Altera teste do tokenizer e do common. P · médio (tokens do SQLite) |
| #2539 | `1608bf6` | Generic: Disallow quoted identifiers in colon and at-sign placeholders | parser | pendente | `:"x"` e `@"x"` deixam de ser placeholder e passam a dar erro. P · médio |
| #2526 | `712de6e` | Refactor: replace `dialect_of!` table partition check with `Dialect` trait method | dialeto (trait, Generic, MySQL), parser | pendente | Flag `supports_table_partitions`, sem mudança de comportamento. P · baixo |
| #2569 | `88ffee6` | ClickHouse: Support ALTER TABLE ... MODIFY ORDER BY | AST (`AlterTableOperation`), Display, spans, parser | pendente | Variante `ModifyOrderBy`; `parse_sorting_key` foi extraído. Teste novo em `spans.rs`. P · baixo |
| #2482 | `393c2c3` | Check in Cargo.lock | infra (Cargo, CI) | pendente | Proposta: não-aplicável. `Cargo.lock` e CI; a troca para `sort_by_key` em `derive/src/dialect.rs` é interna da macro `derive_dialect!` e não tem efeito |
| #2553 | `ab23305` | Databricks: support collated data types | AST (`DataType`), dialeto (trait, Databricks), parser | pendente | Variante `DataType.Collate` e `parse_data_type_with_optional_collation`, usada em STRUCT, ARRAY e MAP. A chave de `MAP<...>` seguida de `>>` passa a dar erro explícito. M · baixo |
| #2581 | `9352883` | Enable GitHub Discussions | infra (ASF) | pendente | Proposta: não-aplicável. `.asf.yaml` |
| #2583 | `ddb9b92` | Ignore libFuzzer worker logs in fuzz/ | infra (fuzz) | pendente | Proposta: não-aplicável. `.gitignore` do fuzz |
| #2582 | `37109d4` | Remove homepage metadata duplicating repository | infra (Cargo) | pendente | Proposta: não-aplicável. Metadados do `Cargo.toml` |
| #2515 | `c23dc05` | PostgreSQL: Support CREATE FOREIGN TABLE | AST (novo `CreateForeignTable`, `Statement`), Display, spans, visitor, parser | pendente | Tipo e variante novos; `parse_pg_options_clause` foi extraído de `CREATE SERVER`. `parse_identifier` passa a dar span ao `Ident` que vem de string. O visitor percorre `name` com `visit_relation` (C18). Teste novo em `spans.rs`. G · médio |
| #2489 | `37df75e` | Snowflake: Parse structured OBJECT types | AST (`DataType`, `MapBracketKind`), dialeto (trait, ClickHouse, Generic, Snowflake), parser | pendente | Variante `DataType.Object` e membro `MapBracketKind.ParenthesesNotNull` (no meio do enum C#). `OBJECT(...)` não tem gate de dialeto. `parse_click_house_map_def` passa a se chamar `parse_parenthesized_map_type_def`. M · médio |
| #2574 | `91f7ce2` | ClickHouse: Support tuple element access by index | tokenizer, dialeto (trait, ClickHouse), parser | pendente | Flag `supports_tuple_element_access`: no ClickHouse, `t.1` vira `Word`, `.` e `Number` (antes, `Number(".1")`). Teste novo em `tokenizer.rs`. M · médio (tokens do ClickHouse) |
| #2448 | `560a13c` | MSSQL: Support PROC as CREATE PROCEDURE abbreviation | keywords, dialeto (trait, MsSql), parser | pendente | Keyword `PROC` e flag `supports_create_proc_syntax`. P · baixo; o enum `Keyword` é renumerado |
| #2595 | `5cad906` | PostgreSQL: Remove test `parse_create_table_with_inherit` | testes | pendente | Remover o teste portado (`SqlParserPostgresTests1.cs`); o postgres passa de 201 para 200 testes. P · nenhum |
| #2602 | `287aaf6` | Bump bigdecimal from 0.4.10 to 0.4.11 | infra (Cargo) | pendente | Proposta: não-aplicável. Dependência Rust; o Core é compilado sem `bigdecimal` (DIV-006) |
| #2601 | `811ef29` | Update SECURITY policy to conform to ASF guidelines | infra (docs) | pendente | Proposta: não-aplicável. Política de segurança do projeto upstream |
| #2585 | `d405abe` | Gate fuzz oracle targets behind features | infra (fuzz) | pendente | Proposta: não-aplicável. Fuzz em Rust |
| #2584 | `c2e085d` | DuckDB: Support nested block comments | dialeto (DuckDB) | pendente | `supports_nested_comments = true`: muda os tokens de `/* /* */ */` no DuckDB. P · baixo |
| #2541 | `bfc7d38` | Generic: Render prefix `!` operator with space before operator characters | Display | pendente | `starts_with_operator_char` passa a ser estrutural, sem renderizar a expressão, e vale para `-` e `!`. P · médio |
| #2546 | `8f4da49` | MsSql: Disallow unquoted `FROM` keyword as alias assignment expression | parser | pendente | `SELECT a = FROM t` passa a dar erro. Traz nova comparação `to_lowercase() == "from"` (acrescentar à DIV-001). Estende teste existente. P · médio |
| #2558 | `aa1b5d6` | SQLite: Support `CAST(expr AS)` without a type name | dialeto (trait, SQLite), parser, Display | pendente | Usa `DataType.Unspecified` no `CAST`, exibido como `CAST(x AS)`. P · baixo |
| #2543 | `b1d1486` | Generic: Add spacing around dot in compound field access adjacent to numbers | Display | pendente | Põe espaço em volta do `.` de `CompoundFieldAccess`/`AccessExpr` quando há número ao lado. P · médio |
| #2560 | `fc18670` | SQLite: Support string literals as column names in `CREATE TABLE` | dialeto (trait, SQLite), parser | pendente | Flag `supports_string_literal_column_names`. P · baixo |
| #2552 | `7939d4b` | SQLite: support == as assignment operator in UPDATE SET | dialeto (trait, SQLite), parser | pendente | Flag `supports_double_eq_assignment`. P · baixo |
| #2586 | `1ba145b` | Generic: Parse IN lists that start with a parenthesized subquery | parser | pendente | `parse_in` foi reescrito: `IN ((SELECT 1), 2)` vira `InList`. Novo campo de memorização `failed_in_subquery_positions`, contra tempo 2^N. As mensagens de erro mudam. M · médio |
| #2580 | `c70b5b1` | Add a fuzz target that flags superlinear parsing and printing | infra (fuzz) | pendente | Proposta: não-aplicável. Fuzz em Rust |
| #2555 | `14cbf75` | SQLite: Support identifier characters above U+FFFF | dialeto (SQLite) | pendente | `is_identifier_start` passa a aceitar `ch >= U+0080`; U+007F deixa de ser aceito. Conferir contra a DIV-003. O teste tem caracteres fora do BMP: usar escapes e rodar `Find-SuspiciousChars.ps1`. P · baixo |
| #2619 | `e790703` | Parser: fix exponential parse time on nested POSITION calls | parser | pendente | Memorização `failed_position_syntax_positions` com chave `(índice, ParserState)`. Um teste roda em thread de 8 MB com timeout de 5 s (DIV-005); outro exige `visitor` (C18). P · baixo |
| #2598 | `dacdbc9` | ClickHouse: Support GLOBAL IN / GLOBAL NOT IN | AST (`Expr`), Display, spans, dialeto (trait, ClickHouse, precedência), parser | pendente | Campo `global` em `Expr.InList` e `Expr.InSubquery`; nova assinatura `Parser.ParseIn(expr, negated, global)`. `IN expr` sem parênteses passa a usar `parse_subexpr(prec Eq)`. Altera testes do common. M · **alto (API)** |

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
