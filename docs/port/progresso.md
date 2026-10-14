# Progresso do port

**Leia este arquivo no início de cada sessão** e **atualize-o ao final**. É o "estado salvo" do projeto: diz o que já foi portado, de qual versão do upstream e o que vem em seguida. Assim nenhuma sessão precisa reconstruir o contexto do zero.

- **Baseline do upstream:** `v0.63.0` (`85b1a6f`). Ver `UPSTREAM.json`.
- **Fase atual:** 2, AST e parser portados (ondas A e B); testes de integração portados (onda C, todos passando); visitor (C18) e onda D (correções, lista em "Pendências para a onda D") em andamento; API pública fase 1 pronta (E1). Plano em [`../analise-tecnica-e-plano.md`](../analise-tecnica-e-plano.md) §8.

## Estado por arquivo Rust

Legenda: ⬜ não iniciado · 🟨 em andamento · ✅ portado (testes portados passando) · ⏸️ adiado (motivo na nota)

| Arquivo Rust (v0.63.0) | Linhas | Destino C# | Status | Testes Rust → C# | Notas |
|---|---|---|---|---|---|
| `src/keywords.rs` | 1.321 | `Keywords.g.cs` + `Keywords.cs` | ✅ | — (+2 testes próprios) | Gerado por `tools/codegen/Generate-Keywords.ps1` |
| `src/tokenizer.rs` | 4.555 | `Tokenizer/` (+ `Ast/Value/DollarQuotedString.cs` e `QuoteDelimitedString.cs` de `ast/value.rs`; `Support/PeekableChars.cs`, `Support/RustChar.cs`) | ✅ | 67/67, passando em net48/net8.0/net10.0 (+ 4 testes próprios em `TokenizerPortTests`) | Linhas 1–2636 completas. Divergências: DIV-003 e DIV-004. Decisões: convencoes §13, casos 17 a 28 |
| `src/dialect/mod.rs` | 2.150 | `Dialects/Dialect.cs` + `Dialects/Precedence.cs` | ✅ | 5/5, passando em net48/net8.0/net10.0 (`parse_with_wrapped_dialect` ativado na B7) | 167 de 168 membros do trait, `Precedence` e `dialect_from_str`; `fn dialect()` não portado por decisão (convencoes §13, caso 8). Lista abaixo |
| `src/dialect/*.rs` (16 dialetos) | 4.828 | `Dialects/<Struct>.cs` (16 arquivos) | ✅ | — (sem testes no Rust) | 441 de 441 overrides: 418 na Fase 1 e 23 na Fase 2 (B6), com as funções auxiliares de MsSql, MySQL e Snowflake. Lista abaixo |
| `src/ast/**` (exceto `spans.rs` e `visitor.rs`) | ~28.500 | `Ast/` (`Mod/`, `Query/`, `Ddl/`, `Dml/`, `Dcl/`, `DataType/`, `Value/`, `Operator/`, `Trigger/`, `TableConstraints/`, `Helpers/`, `Comments/`) | ✅ | `mod.rs` 10/10 (8 passando, 2 `Skip` PENDENTE: `OneOrManyWithParensIntoIter` e `impl Ord for Ident`, ver onda D); `comments.rs` 1/1; `value.rs` 1/1 (`Skip` DIV-003); builders 4 (próprios) | Onda A (`docs/port/onda-a/`) |
| `src/ast/spans.rs` | 3.165 | `Ast/Spans/` + `impl` nos arquivos dos tipos | ✅ | 18/18, passando em net48/net8.0/net10.0 | B7 (`docs/port/onda-b/B7.md`): 145 `impl Spanned` |
| `src/ast/visitor.rs` | 1.376 | `Ast/Visitor/` (+ `Ast/Visitor/Generated/*.g.cs`, gerados por `tools/codegen/Generate-Visitor.ps1`) | ✅ | 7/7, passando em net48/net8.0/net10.0 (+ 18 testes próprios em `VisitorPortTests`) | C18 (`onda-c/C18.md`, ADR-0007): o derive `Visit`/`VisitMut` virou gerador; casos 125–134; DIV-008 e DIV-009 |
| `src/parser/**` | ~22.800 (`mod.rs` 22.043, `alter.rs` 512, `merge.rs` 286) | `Parser/` | ✅ | 19/19 (`mod tests` de `mod.rs`, inclusive `test_parse_data_type`), passando em net48/net8.0/net10.0 (C17) | Onda B (B0–B6; `docs/port/onda-b/`). Revisões R1–R3 de contexto limpo: nenhum achado Alto ou Médio |
| `src/test_utils.rs` | 505 | `tests/.../TestUtils/` (`TestedDialects.cs`, `TestUtils.cs`) | ✅ | — (sem testes no Rust) | Completo desde o commit `88c8499` (inclusive `options`, `recursion_limit` e as funções que usam o parser) |
| `tests/*.rs` (21 arquivos) | ~55.000 | `tests/.../Integration/` (+ `Queries/tpch/`) | ✅ | 1.429/1.430 portados (tabela abaixo; o que falta é a variante `bigdecimal`, que não se aplica) | Onda C (C1–C17), integrada ao `main` |

### Testes de integração (`tests/*.rs`) por arquivo

Contagem de `#[test]` no Rust v0.63.0 × testes portados. "Passando" vale para os três alvos (net48, net8.0, net10.0). Unidades e diagnósticos em `docs/port/onda-c/`.

| Arquivo Rust | `#[test]` | Portados | Resultado | Unidade |
|---|---|---|---|---|
| `sqlparser_common.rs` | 497 | 497/497 | 497 passando (os 2 testes de estouro de pilha passaram com a D1, e `test_visit_order` com o visitor C18) | C1–C5 |
| `sqlparser_postgres.rs` | 201 | 200/201 | 200 passando; a variante `#[cfg(feature = "bigdecimal")]` de `test_simple_postgres_insert_with_alias` não se aplica (DIV-006) | C6–C8 |
| `sqlparser_mysql.rs` | 140 | 140/140 | 140 passando | C9, C10 |
| `sqlparser_snowflake.rs` | 164 | 164/164 | 164 passando | C11, C12 |
| `sqlparser_mssql.rs` | 71 | 71/71 | 71 passando | C13 |
| `sqlparser_bigquery.rs` | 56 | 56/56 | 56 passando | C14 |
| `sqlparser_clickhouse.rs` | 53 | 53/53 | 53 passando | C15 |
| `sqlparser_sqlite.rs` | 35 | 35/35 | 35 passando | C15 |
| `sqlparser_duckdb.rs` | 26 | 26/26 | 26 passando | C16 |
| `sqlparser_databricks.rs` | 17 | 17/17 | 17 passando | C16 |
| `sqlparser_hive.rs` | 45 | 45/45 | 45 passando | C16 |
| `sqlparser_redshift.rs` | 26 | 26/26 | 26 passando | C16 |
| `sqlparser_oracle.rs` | 13 | 13/13 | 13 passando | C16 |
| `sqlparser_spark.rs` | 38 | 38/38 | 38 passando | C17 |
| `sqlparser_teradata.rs` | 8 | 8/8 | 8 passando | C17 |
| `sqlparser_custom_dialect.rs` | 4 | 4/4 | 4 passando | C17 |
| `sqlparser_derive_dialect.rs` | 4 | 4/4 | 3 passando; 1 `Skip` DIV-002 (`test_different_base_dialects`) | C17 |
| `no_std_recursion.rs` | 8 | 8/8 | 8 passando | C17 |
| `sqlparser_comments.rs` | 1 | 1/1 | 1 passando | C17 |
| `sqlparser_regression.rs` | 1 | 1/1 | a macro `tpch_tests!` gera 22 testes C#: 22 passando | C17 |
| `pretty_print.rs` | 22 | 22/22 | 15 passando; 7 `Skip` `#[ignore]` do próprio Rust (issue 1850 do upstream) | C17 |
| **Total** | **1.430** | **1.429** | O que falta não se aplica (variante `bigdecimal`, DIV-006) | |

### Membros adiados para a Fase 2 (dependem de Parser/AST)

Cada item tem, no arquivo C#, um comentário `// PENDENTE (...)` na posição exata do Rust (convencoes.md §13, caso 16). Ao portar, troque o comentário pelo membro. Os hooks de `Dialect` que recebem `Parser` ficam no próprio `Dialects/Dialect.cs`, na posição do Rust (convencoes.md §2 e §13, caso 53; o `Dialect.Parser.cs` previsto antes não é usado).

**Contagem por arquivo** (membros do trait ou overrides; portados/total):

| Arquivo Rust | Destino C# | Portados | Fase 2 | Tokenizer | Outros |
|---|---|---|---|---|---|
| `dialect/mod.rs` | `Dialect.cs` | 167/168 | 0 | — | `dialect` (não portado, caso 8) |
| `dialect/ansi.rs` | `AnsiDialect.cs` | 5/5 | — | — | |
| `dialect/bigquery.rs` | `BigQueryDialect.cs` | 25/25 | — (B6) | — | |
| `dialect/clickhouse.rs` | `ClickHouseDialect.cs` | 30/30 | — | — | |
| `dialect/databricks.rs` | `DatabricksDialect.cs` | 24/24 | — | — | |
| `dialect/duckdb.rs` | `DuckDbDialect.cs` | 25/25 | — | — | |
| `dialect/generic.rs` | `GenericDialect.cs` | 73/73 | — | — | |
| `dialect/hive.rs` | `HiveDialect.cs` | 13/13 | — | — | |
| `dialect/mssql.rs` | `MsSqlDialect.cs` | 25/25 | — (B6) | — | + 3 funções de `impl MsSqlDialect` (B6) |
| `dialect/mysql.rs` | `MySqlDialect.cs` | 34/34 | — (B6) | — | + 4 funções livres (B6) |
| `dialect/oracle.rs` | `OracleDialect.cs` | 20/20 | — (B6) | — | |
| `dialect/postgresql.rs` | `PostgreSqlDialect.cs` | 44/44 | — (B6) | — | |
| `dialect/redshift.rs` | `RedshiftSqlDialect.cs` | 24/24 | — | — | |
| `dialect/snowflake.rs` | `SnowflakeDialect.cs` | 49/49 | — (B6) | — | + 27 funções livres (B6) |
| `dialect/spark.rs` | `SparkSqlDialect.cs` | 21/21 | — (B6) | — | |
| `dialect/sqlite.rs` | `SQLiteDialect.cs` | 15/15 | — (B6) | — | |
| `dialect/teradata.rs` | `TeradataDialect.cs` | 14/14 | — | — | |

**Pendentes por dependência do tokenizer:** nenhum. `peek_nested_delimited_identifier_quotes` (`dialect/mod.rs:245` e `dialect/redshift.rs:61`) foi portado com o tokenizer, recebendo o `struct` público `PeekableChars` (convencoes.md §13, caso 17).

**Pendentes da Fase 2:**
- `dialect/mod.rs` (trait `Dialect`): **concluído na Fase 2** (os 13 membros estão em `Dialect.cs`, na posição do Rust; `Option` aninhado de `parse_column_option` → `Option<ColumnOption?>`, convencoes §13, casos 53 e 55).
- **Dialetos: concluído na Fase 2 (unidade B6, `onda-b/B6.md`)** — os itens abaixo estão portados, na posição do Rust (funções livres como membros `static` do dialeto, no fim da classe):
- ✅ `dialect/bigquery.rs`: `parse_statement` (50), `is_column_alias` (162; a constante `RESERVED_FOR_COLUMN_ALIAS` já está portada).
- ✅ `dialect/mssql.rs`: `get_reserved_grantees_types` (132), `is_select_item_alias` (136), `is_table_factor_alias` (166), `parse_statement` (200), `get_next_precedence` (243); bloco `impl MsSqlDialect` (253): `parse_if_stmt` (260), `parse_create_trigger` (341), `parse_statement_list` (380).
- ✅ `dialect/mysql.rs`: `parse_infix` (97), `parse_statement` (120), `is_table_factor_alias` (152; a constante `RESERVED_FOR_TABLE_ALIAS_MYSQL` já está portada); funções livres `parse_lock_tables` (232), `parse_lock_table` (238), `parse_lock_tables_type` (252), `parse_unlock_tables` (272).
- ✅ `dialect/oracle.rs`: `get_next_precedence` (88).
- ✅ `dialect/postgresql.rs`: `is_table_alias` (108; a constante `RESERVED_EXCLUSIONS_FOR_TABLE_ALIAS` já está portada), `get_next_precedence` (136; as constantes `COLLATE_PREC`/`BRACKET_PREC` já estão portadas).
- ✅ `dialect/snowflake.rs`: `parse_statement` (253), `parse_column_option` (413), `get_next_precedence` (443), `is_column_alias` (488), `is_table_alias` (534), `is_table_factor` (618; a constante `RESERVED_KEYWORDS_FOR_TABLE_FACTOR` já está portada), `is_identifier_generating_function_name` (650); funções livres `peek_for_limit_options` (702), `parse_put` (716), `parse_file_staging_command` (727), `parse_alter_dynamic_table` (749), `parse_alter_external_table` (787), `parse_alter_session` (829), `pub parse_create_table` (844), `pub parse_create_database` (1101), `pub parse_storage_serialization_policy` (1208), `pub parse_create_stage` (1222), `pub parse_create_file_format` (1286), `pub parse_stage_name_identifier` (1313), `pub parse_snowflake_stage_name` (1344, reexportada por `dialect/mod.rs`), `pub parse_copy_into` (1366), `parse_select_items_for_data_load` (1530), `parse_select_item_for_data_load` (1552), `parse_stage_params` (1627), `parse_session_options` (1693), `parse_identity_property` (1744), `parse_column_policy_property` (1779), `parse_column_tags` (1806), `parse_show_objects` (1816), `parse_multi_table_insert` (1842), `parse_multi_table_insert_into_clauses` (1896), `parse_multi_table_insert_into_clause` (1912), `parse_multi_table_insert_value` (1940), `parse_multi_table_insert_when_clauses` (1951).
- ✅ `dialect/spark.rs`: `parse_infix` (136).
- ✅ `dialect/sqlite.rs`: `parse_statement` (69), `parse_infix` (78).
- ✅ Teste `parse_with_wrapped_dialect` (`Dialects/DialectTests.cs`): ativado na B7 (sem `Skip`), passando.

**Não portados por decisão** (convencoes.md §13, caso 8; DIV-002): `fn dialect() -> TypeId` (`mod.rs:209`), `impl dyn Dialect { fn is::<T>() }` (`mod.rs:1899`), macros `dialect_of!`/`dialect_is!` (`mod.rs:155`/`164`) e o reexport de `derive_dialect!` (`mod.rs:133`). No C#, use `dialect is A or B`.

### Pendências de `test_utils.rs` (dependem do Parser/AST, Fase 2)

**Concluídas** no commit `88c8499` (não há mais `// PENDENTE` em `tests/SqlParser.Core.Tests/TestUtils/`). Lista original, mantida como histórico:
- `TestedDialects`: campos `options: Option<ParserOptions>` e `recursion_limit: Option<usize>`; `new_with_options`, `with_recursion_limit`, `new_parser`, `one_of_identical_results`, `run_parser_method`, `parse_sql_statements`, `one_statement_parses_to`, `statements_parse_to`, `expr_parses_to`, `verified_stmt`, `verified_query`, `verified_query_with_canonical`, `verified_only_select`, `verified_only_select_with_canonical`, `verified_expr`.
- `TestedDialects.TokenizesTo`: o ramo `if let Some(options) = &self.options { tokenizer = tokenizer.with_unescape(options.unescape) }` (hoje `options` é sempre `None`, então o comportamento é o mesmo).
- Funções livres: `all_dialects_with_options`, `expr_from_projection`, `alter_table_op_with_name`, `alter_table_op`, `number`, `single_quoted_string`, `table_alias`, `table`, `table_from_name`, `table_with_alias`, `join`, `call`, `index_column`.

## Pendências para a onda D (correções)

Levantadas nas notas das ondas B e C e nas revisões R1–R3 (consolidação de 2026-10-14). Cada item cita a origem. Marque `[x]` ao concluir e registre em `convencoes.md` §13 toda regra que mudar.

### Parser e `Display`: pilha (severidade Alta)
- [x] (D1, `onda-d/D1.md`; `Support/StackGuard.cs`, casos 123 e 124, DIV-005 reescrita) **Estouro de pilha**: `overflow` (o `ToString()` de `SELECT 1 + 1 + ... + 1` com 1000 termos estoura em `Expr.Fmt`, `Ast/Mod/Expr.Display.cs`) e `parse_deeply_nested_boolean_expr_does_not_stackoverflow` (o parse de 200 níveis estoura antes de `RecursionCounter.TryDecrease` agir; `Parser/RecursionCounter.cs` e `ParseSubexpr`/`ParsePrefixInner`/`ParseInfix` em `Parser/Parser.Part1a.cs`, `Part1b.cs`, `Part1d.cs`) derrubam o host com `StackOverflowException`. Os dois estão em `SqlParserCommonTests4` com `Skip = "PENDENTE (onda D, estouro de pilha): ..."`; diagnóstico em `onda-c/C4.md`. A D1 porta `#[recursive::recursive]`/stacker e reescreve a DIV-005 (só ela edita a DIV-005). Ao concluir, remover os dois `Skip`.

### Parser: documentação (§12)
- [x] (D2, `onda-d/D2.md`) Doc comments não verbatim (R1, achado 2): `Parser/Parser.Part1c.cs:1037-1041` (`# Errors` virou prosa; Rust `mod.rs:3316-3320`), `Parser/Parser.Part1d.cs:920-924` (a lista de `parse_subscript_inner` virou uma linha só; Rust 4236-4244), `Parser/Parser.Part1b.cs:29-32` (o erro de digitação "`None if" do Rust foi corrigido; Rust 1528-1529). Restaurar o texto original com `<list>`/`<para>`.
- [x] (D2, `onda-d/D2.md`) Doc de `parse_multipart_identifier` (R2, achado 1): `Parser/Parser.Part4a.cs:683-705` reescreveu o exemplo Rust em C#, com trecho em pt-BR. Manter o exemplo original em `<code>`.
- [x] (D2, `onda-d/D2.md`) Notas do port em pt-BR dentro do XML doc público (R2, achado 2): `<remarks>` de `ParseSubType` (`Parser/Parser.Part4a.cs:1360-1364`) e de `WithState` (`Parser/Parser.Part4b.cs:1367-1374`). Mover para comentários `//` no corpo.
- [ ] **Decidir a regra geral** para o mesmo problema, que aparece em escala: cerca de 470 linhas `///` com notas do port em pt-BR ("Rust: ...", referências a `convencoes.md`/`nomes.md`) em cerca de 180 arquivos, a maioria de `Ast/Mod`, `Ast/Query` e `Ast/Ddl` (ex.: `<param name="RoleValue">` em `Ast/Mod/Action.cs:146-149`). A §12 pede doc Rust verbatim e notas do port em `//`. Decidir se notas curtas em `<remarks>`/`<param>` são aceitas ou se tudo vai para `//`, e aplicar.
- [ ] Opcional: `Tokenizer/Span.cs` (linhas 26 e 54) cita `Spanned` (`crate::ast::Spanned`) em texto; depois da B7, pode virar `<see cref="ISpanned"/>` (caso 77).

### Parser: marcações e divergências
- [x] (D2, `onda-d/D2.md`) `// PORT-DEVIATION DIV-007: ...` em `Parser/TokenDebugExtensions.cs:142-145`, na aproximação de `escape_debug` (R1, achado 1).
- [ ] `to_lowercase()` → `ToLowerInvariant()`, agora coberto pela DIV-001 (B6; R3, achado 2): conferir no net48 que `ToLowerInvariant` mantém U+0130 (no .NET 10 mantém, e o resultado é igual ao do Rust) e marcar com `// PORT-DEVIATION DIV-001` os dois pontos, `Dialects/SnowflakeDialect.cs:690` (`IsIdentifierGeneratingFunctionName`) e `Parser/Parser.Part5b.cs:1000` (`ParseSelectItem`). Conferir também os `ToUpperInvariant` de `Parser/Parser.Part2b.cs:1704` e `Parser/Parser.Part2c.cs:1294`.
- [ ] Opcional (B3): expor `TokenDebugExtensions.DebugStr` como extensão `string.ToDebugString()`, para um nome só (caso 90).

### Parser: desempenho (opcional; o comportamento já é igual ao do Rust)
- [x] (D2, `onda-d/D2.md`) `new[] { .. }.Contains(..)` aloca um array por chamada (R1, achado 4): `Parser/Parser.Part1a.cs:1017` (`Validator` de `ParseExprWithAliasAndOrderBy`, a cada alias) e `Parser/Parser.Part1c.cs:726` (`ParseTrimExpr`, a cada `TRIM(`). Trocar por `private static readonly Keyword[]` (caso 11) e procurar outros usos no `Parser/`.
- [ ] `try/catch` no caminho comum (R2, achado 3; caso 81): `Parser/Parser.Part4d.cs:293-302` e `811-823` (`UNNEST(..)` sem `WITH OFFSET`; `PASSING` sem `BY VALUE`) e `Parser/Parser.Part3a.cs:590-652` (`GENERATED .. AS IDENTITY` sem `(`) lançam e capturam uma `ParserException` a cada uso. Há equivalentes exatos sem exceção: `ParseKeyword(WITH) && ParseKeyword(OFFSET)` (consome `WITH` mesmo quando falha, como o Rust) e `ConsumeToken(LParen)` no lugar de `ExpectToken(..)` + `is_ok`. Se adotar, registrar no caso 81.
- [ ] `throw structBodyError;` reinicia o stack trace (R1, achado 5): `Parser/Parser.Part1d.cs:118-142` e os demais pontos do caso 83. Opcional: `ExceptionDispatchInfo.Capture(e).Throw()`.

### Dialetos
- [x] (D2, `onda-d/D2.md`) **Arrays estáticos expostos como `IReadOnlyList<T>`** (R3, achado 1): um `cast` para `T[]` altera o estado global de todas as instâncias. Confirmado: depois de `((GranteesType[])ms.GetReservedGranteesTypes)[0] = GranteesType.User`, um `new MsSqlDialect()` lê `GRANT SELECT ON t TO USER` como grantee de nome `USER`. Pontos: `Dialects/MsSqlDialect.cs:105-107`, `Dialects/OracleDialect.cs:12` e `74`, `Dialects/SnowflakeDialect.cs:30` e `680`; conferir também `Keywords.ReservedFor*` (`Keywords.cs:44`) e os demais dialetos. Rever os casos 10 e 113 da §13 (ex.: `ReadOnlyCollection<T>` em cache, com `Array.AsReadOnly`) e aplicar em todos os pontos.

### AST
- [x] (D2, `onda-d/D2.md`) `AstModTests` tem 2 `Skip` PENDENTE que são lacunas do Core: `OneOrManyWithParensIntoIter` (iterador por valor com `next_back`/`size_hint`/`clone`; `Ast/Mod/OneOrManyWithParens.cs`) e `impl PartialOrd/Ord for Ident` (`mod.rs` 289–310; `Ast/Mod/Ident.cs`). `nomes.md` registra `OneOrManyWithParensIntoIter` como "não portado" por decisão da onda A (o enumerador de `IReadOnlyList<T>` cumpriria o papel), mas o teste do Rust mostra que não cumpre: rever a decisão ao portar.

### Visitor e testes
- [x] (visitor integrado) `test_visit_order` (`SqlParserCommonTests4`, `Skip` PENDENTE) aguarda o visitor (C18). Remover o `Skip` quando o visitor entrar.
- [ ] Opcional (C17, decisão 1): levar a sobrecarga `RunParserMethod(.., Action<Parser>)` para `TestUtils/TestedDialects.cs`, se outras classes precisarem; depende do conflito do caso 85.

### Conflitos de convenção (decididos pelo integrador em 2026-10-14)
- [x] (decidido pelo integrador: `ValueTuple`; falta trocar o `bool` de `MsSqlDialect.ParseStatement`) `convencoes.md` §13, caso 85: representação de `()` como valor genérico (`bool` no código × `ValueTuple` nos testes).
- [x] (decidido: `@`, como no código) Caso 95 e skill `port-rust-tests`: palavras contextuais do C# nos testes (`@from` × renomear para `firstFrom`).
- [x] (decidido: permitidos sem `..` ligado) Caso 102 e skill: padrões de lista (`is [a, b]`) nos testes (evitar sempre × permitir sem `..` ligado, como o Core e a C17 já fazem).
- [x] (decidido: forma da B7 para código novo) Caso 121: desestruturação inicial `let X { .. } = self;` (locais para todos os campos, caso 78 × locais só para os renomeados, B7).
- [x] (decidido: `TestUtils.AssertEqualWithMessage`) Skill `port-rust-tests`: `assert_eq!(a, b, "msg")` (omitir a mensagem × `Assert.True(a == b, msg)` × `try`/`catch` + `Assert.Fail`).

### Informativo (não corrigir)
- Bugs e comportamentos estranhos do upstream, reproduzidos fielmente (§1, princípio 4; caso 101): no PostgreSQL, `ALTER OPERATOR FAMILY f USING btree ADD FUNCTION 1 () f()` → `Expected: identifier, found: )` (R2, achado 4); no Snowflake, `CREATE GLOBAL TEMPORARY VIEW` cai em "an SQL statement, found: GLOBAL", `parse_stage_name_identifier` consome o `;` (`PUT 'f' @s; SELECT 1` falha) e `{- d -}` logo depois de `c?` é lido como quantificador (R3).

## Diário de sessões

Registre ao fim de cada sessão: data, o que foi feito, decisões tomadas e próximo passo. Entradas mais recentes no topo.

### 2026-10-14 (visitor e onda D: D1, D2; plano de sincronização S1)
- **C18 (visitor):** `src/ast/visitor.rs` portado. O derive `Visit`/`VisitMut` virou o gerador `tools/codegen/Generate-Visitor.ps1` (console `tools/codegen/VisitorGen`, fora da solução): compila o Core sem o código gerado, lê a AST por reflexão **só na geração**, confere 530 tipos, nomes, ordem dos campos e as 30 anotações `visit(with)` contra o Rust e emite `Ast/Visitor/Generated/*.g.cs`. `VisitMut` reconstrói os pais com `with` só quando um filho muda. Decisões em ADR-0007, casos 125–134 (renumerados na integração; a C18 usava 123–132), DIV-008 e DIV-009. `test_visit_order` ativado.
- **D1 (estouro de pilha, severidade Alta):** `Support/StackGuard.cs` equivale ao `stacker::maybe_grow` (zona vermelha de 128 KiB; com pouca pilha, a chamada continua numa thread nova de 2 MiB), aplicado nos 6 pontos `#[recursive::recursive]` do Rust e em `Expr.Span()` (desvio registrado). `TryDecrease` voltou a só contar a profundidade, como no Rust. `throw;` dentro de `catch` no caminho da recursão foi proibido (caso 124; o caso 82 foi emendado). DIV-005 reescrita. Os testes `overflow` e `parse_deeply_nested_boolean_expr_does_not_stackoverflow` passam.
- **D2 (lacunas e achados baixos):** `impl Ord for Ident` e `OneOrManyWithParensIntoIter` portados (os 2 `Skip` PENDENTE de `AstModTests` saíram); `PORT-TODO` de `f.align()` resolvidos; listas estáticas dos dialetos e de `Keywords` expostas como somente leitura (R3, achado 1); arrays estáticos no lugar de `new[] {..}.Contains` (R1, achado 4); docs verbatim (R1, achado 2; R2, achados 1 e 2); `PORT-DEVIATION DIV-007`.
- **Conflitos de convenção** (casos 85, 95, 102, 121 e `assert_eq!` com mensagem): decididos pelo integrador; ver os casos e a skill `port-rust-tests`.
- **S1:** plano de sincronização com o upstream na seção "Rodada 1" de `upstream-ledger.md` (63 PRs depois da v0.63.0, sem tag nova; ordem dos lotes recomendada).
- **Resultado:** `SqlParser.Core.Tests` 1.615 aprovados e 9 ignorados (7 `#[ignore]` do próprio Rust, DIV-002, DIV-003); `SqlParser.Tests` 75 aprovados; 0 falhas em net48, net8.0 e net10.0. Build com 0 avisos.
- **Próximo:** D3 (fuzzing, em andamento); notas do port em pt-BR dentro de `<remarks>` públicos (decidir e aplicar); `ValueTuple` em `MsSqlDialect.ParseStatement`; marcações `PORT-DEVIATION DIV-001`; sincronização com o upstream (Rodada 1); API pública fase 2 (visitors no estilo SqlParser-cs).

### 2026-10-14 (API pública, fase 1: unidade E1)
- **Feito** (só `src/SqlParser`, `tests/SqlParser.Tests` e documentação; o Core não foi alterado):
  - `SqlQueryParser`: `Parse` (dialeto por instância ou por nome, opções), `TryParse`, `ParseWithComments`, limite de recursão no construtor;
  - `SqlParseException`: mensagem idêntica à do Core, `Error`, `Location`/`Line`/`Column` extraídos do sufixo `" at Line: L, Column: C"`;
  - `SqlParseResult` (instruções + comentários);
  - `SqlWritingExtensions.ToSql(pretty)` sobre `IDisplay` e sobre `IEnumerable<Statement>`.
- **Testes:** 75 em `tests/SqlParser.Tests` (parse e ida e volta em 9 dialetos, `TryParse`, erros com e sem posição, pretty-print com casos de `tests/pretty_print.rs`, exemplos do README), passando em net48/net8.0/net10.0.
- **Decisões:** [ADR-0008](../adr/0008-api-publica-fase-1.md) (inclui a lista "A confirmar" para o responsável); nomes em `nomes.md`, seção "Camada pública"; nota em `THIRD-PARTY-NOTICES.md` (nomes imitados do SqlParser-cs, sem cópia de código).
- **Próximo:** fase 2 da API pública (visitors no estilo SqlParser-cs e utilitários de análise), depois do port de `ast/visitor.rs`.

### 2026-10-14 (Fase 2: onda C, testes de integração, revisões e consolidação)
- **Execução:** o lançamento de 20 agentes de uma vez esgotou o limite de sessão e interrompeu todos. A onda seguiu numa **fila de no máximo 5 agentes simultâneos**, cada um num git worktree isolado (criado a partir de `88c8499`), com retomada dos interrompidos. Divisão e status em `docs/port/onda-c/README.md`.
- **Resultados até agora:**
  - C1–C17 concluídas e integradas ao `main`: 1.429 dos 1.430 `#[test]` de `tests/*.rs` portados (o que falta é a variante `bigdecimal`, que não se aplica; tabela "Testes de integração por arquivo"), mais os 19 de `parser/mod.rs`.
  - **Nenhum teste portado falha em net48, net8.0 ou net10.0.** Os únicos `Skip` são os previstos: 2 PENDENTE de estouro de pilha (C4, onda D), 1 PENDENTE do visitor, 1 DIV-002 e 7 `#[ignore]` do próprio Rust.
  - Nenhuma asserção falhou por erro de tradução do Core. O único defeito real é o estouro de pilha (C4), que derruba o processo; está em correção pela D1.
  - Revisões de contexto limpo R1 (B1+B2), R2 (B3+B4) e R3 (B5+B6 e dialetos): nenhum achado Alto ou Médio; 11 Baixos (R1: 5, R2: 4, R3: 2). O 1º da R1 virou a DIV-007.
  - Em andamento: C18 (visitor) e D1 (estouro de pilha).
- **Consolidação das notas das ondas B e C:**
  - `convencoes.md` §13: casos 79–122 (e correção do "Primeiro uso" do caso 53);
  - `nomes.md`: seção "Parser e spans (onda B)", `CreateRoleValue` e `RoleValue` (unidades sem nota da onda A) e a nova seção "Testes";
  - `divergencias.md`: a DIV-001 passou a cobrir `to_lowercase()`; nenhuma DIV nova;
  - skill `port-rust-tests`: padrões de integração reorganizados por tema, regra de `unreachable!`/`panic!` (preferir `InvalidOperationException`), regras de strings e a exceção do `Skip` para teste que derruba o processo;
  - nova seção "Pendências para a onda D (correções)", com 21 itens, 5 deles decisões do responsável (**CONFLITO — decidir**).
- **Próximo:**
  1. Concluir C18 e D1 e integrá-las ao `main`. O caso de §13 do visitor (C18) entra com o próximo número livre (123).
  2. Decidir os 5 conflitos.
  3. Onda D: a lista de pendências, com um responsável por área do Core.

### 2026-10-14 (Fase 2: marco 1, parser compilando)
- **Onda B** (parser), em sequência, um agente por vez: B1–B5 (`parser/mod.rs` completo), B6 (`alter.rs`, `merge.rs`, overrides de dialeto, `impl Word`) e B7 (`ast/spans.rs` + os `impl Spanned` espalhados). Notas e decisões em `docs/port/onda-b/B*.md`, **ainda não consolidadas** em `convencoes.md`/`nomes.md` (consolidadas depois; ver a entrada da onda C).
- **Resultado:**
  - o Core compila com 0 erros e 0 avisos;
  - **115 testes passando + 3 ignorados** em net48, net8.0 e net10.0;
  - demonstração: SQL de Generic, MsSql, PostgreSQL e Oracle (inclusive `ANTI JOIN`, CTE, `CONNECT BY`) interpretado e regerado, com o pretty-print igual ao do Rust.
- **Medições:** cada unidade do parser levou de 21 a 30 min e de 430 a 480 mil tokens, para cerca de 4 mil linhas do Rust.
- **Próximo:**
  1. B8: testes de `parser/mod.rs` e `test_utils` com parser.
  2. Revisões com contexto limpo de B1–B7.
  3. Visitor (`ast/visitor.rs`).
  4. Port dos testes de integração `tests/sqlparser_*.rs`.
  5. Consolidar as notas da onda B.

### 2026-10-14 (Fase 2 em andamento: onda A, a AST)
- **Feito pelo integrador:**
  - `Support/`: `Formatter` (`IFmtWrite`, handler `f.Write($"...")`), `Sequence<T>`, `VisitWithAttribute`, `Option<T>` e `RangeBounds`.
  - `DisplayUtils/` (pretty-print).
  - Exemplos canônicos da AST: `Ast/Mod/{AstModule,Parens,Ident,ObjectName}.cs` e `Ast/Helpers/AttachedToken.cs`.
  - `Ast/Comments/Comments.cs`, com teste.
  - Parser B0: `Parser/{Parser,Parser.TokenHelpers,ParserError,RecursionCounter,ParserSupportTypes}.cs`.
  - Os 13 hooks de `Dialect.cs` que dependem do parser.
  - Casos 29–57 em `convencoes.md` §13.
- **Onda A** (8 agentes; divisão em `docs/port/onda-a/README.md`, notas de cada unidade em `docs/port/onda-a/<A?>.md`):
  - concluídas com relatório: **A1**, **A2** e **A6**;
  - **interrompidas por decisão do usuário** (economia de créditos), já perto do fim: A3, A4, A5, A7 e A8. O código está no disco.
  - Pendências da parada:
    - A3, A4 e A8 sem arquivo de notas;
    - A8 parou nos builders de `helpers/` (`CreateTableBuilder.cs` existe; conferir `stmt_create_database.rs`);
    - a revisão linha a linha de `query.rs` (A7) não terminou;
    - A4: testes de `mod.rs` em `tests/.../Ast/AstModTests.cs`, a conferir.
  - **Build do Core no momento da parada:** a AST compila, exceto 1 erro em `Ast/DataType/DataType.cs:1034` (CS0136, nome `fields` repetido). Os demais erros são esperados: métodos do parser da onda B (`ParseStatement`, `ParseSelectItem`, `ParseTableAndJoins`, `ParseGrantPermission`, `InColumnDefinitionState`) e o aviso de `_state` sem uso.
- **Para retomar:**
  1. Ver quais arquivos de cada unidade existem em `src/SqlParser.Core/Ast/` e as notas em `docs/port/onda-a/`.
  2. Terminar as unidades que faltarem (mesmos prompts/regras: README da onda + skill).
  3. Consolidar as notas em `convencoes.md`/`nomes.md`/`divergencias.md`.
  4. Compilar o Core e corrigir.
  5. Onda B: o parser. Plano: B1 `parser/mod.rs` 611–4466; B2–B5 de 5202 a 21269 em 4 partes; B6 `alter.rs`, `merge.rs`, overrides de dialeto pendentes e testes do parser.
- **Estado do build:** a Fase 2 não compila até a onda A terminar (tipos cruzados entre unidades). Não há commits desde `22c326d`.

### 2026-10-14 (revisão linha a linha do tokenizer)
- Revisão por subagente com contexto limpo, usando `checklist-revisao.md`. Nenhum achado de severidade alta.
- Corrigidos:
  1. **Média:** par substituto dividido entre dois tokens após `@`/`@@`/`#`/`%` (ex.: `SELECT @𝔘x`). Corrigido em `TokenizeIdentifierOrKeyword`, com teste próprio `Par_substituto_nao_e_dividido_apos_prefixo_de_identificador`.
  2. **Baixa:** ramo inalcançável `"invalid number of opening quotes"` agora transcrito, para manter o 1:1.
- Estado: 78 testes passando + 1 `Skip` (Parser), em net48/net8.0/net10.0.
- Próximo passo: Fase 2. Começar pelos tipos da AST (folhas) e pelo `Sequence<T>`, seguindo a ordem "esqueleto → folhas → chamadores" (`referencias-e-boas-praticas.md`, L5).

### 2026-10-14 (decisão do responsável)
- Classificação Unicode: manter a do .NET. DIV-003 e DIV-004 aceitas e registradas em `divergencias.md` ("Decisões do responsável") e em `convencoes.md` §10. Não criar tabelas Unicode próprias sem nova decisão.

### 2026-10-14 (tokenizer, parte da Fase 1)
- Feito:
  - `tokenizer.rs` (linhas 1–2636) → `Tokenizer/`: `Token`, `Word`, `Whitespace`, `Location`, `Span`, `TokenWithSpan`, `TokenizerError` (+ `TokenizerException`), `Tokenizer`, `State`, `NumStringQuoteChars`, `TokenizeQuotedStringSettings`, `Unescape`. `DollarQuotedString` e `QuoteDelimitedString` (de `ast/value.rs`) em `Ast/Value/`.
  - `Support/PeekableChars.cs` (equivalente público de `Peekable<Chars>`) e `Support/RustChar.cs` (métodos da std do Rust); `peek_nested_delimited_identifier_quotes` em `Dialect.cs` e `RedshiftSqlDialect.cs`.
  - Testes: 67/67 de `tokenizer.rs` em `Tokenizer/TokenizerTests.cs` (mesmos nomes e ordem), passando em net48/net8.0/net10.0; 4 testes próprios em `Tokenizer/TokenizerPortTests.cs`. Partes de `test_utils.rs` sem parser em `TestUtils/`.
- Decisões: convencoes.md §13, casos 17 a 28; nomes.md (`Token.Char(string)`, `Number(Value, Long)`, tupla `(QuoteStart, NestedQuoteStart)`, `DoUnescape`, `TokenizerErrorAt`, `Span::EMPTY`, `PeekableChars`, `TokenWithLocation`); DIV-003 e DIV-004.
- Armadilha de ferramenta: sequências `\u` + 4 dígitos hexadecimais escritas pelas ferramentas Write/Edit chegam decodificadas (ex.: o texto de teste `@"\u4c91"` virou o caractere literal). Depois de gravar, confira com `grep` e, se preciso, gere a barra invertida por script (`chr(92)`).
- Próximo passo: Fase 2 (AST/Parser); ao portar o parser, completar `TestedDialects` (lista acima).

### 2026-10-14 (dialetos, parte da Fase 1)
- Feito:
  - `dialect/mod.rs` → `Dialects/Dialect.cs` (`abstract partial class`) + `Dialects/Precedence.cs`; `dialect_from_str` → `Dialect.DialectFromStr`.
  - Os 16 dialetos → `Dialects/<Struct>.cs` (classes não seladas), com todos os overrides que não dependem de `Parser`/AST, na ordem do Rust.
  - Testes de `mod.rs`: `Dialects/DialectTests.cs`, 5/5 portados (4 passando em net48/net8.0/net10.0; `parse_with_wrapped_dialect` com `Skip` PENDENTE).
  - Verificação: como os arquivos do tokenizer ainda estavam em andamento (erros só em `Tokenizer/`), o build e os testes rodaram numa cópia isolada (Core sem `Tokenizer/`/`Ast/`): 0 avisos, 0 erros.
- Decisões: convencoes.md §13, casos 8 a 16; DIV-002; nota sobre `char.IsLetter` e pares substitutos em "Itens a confirmar" de `divergencias.md`.
- Próximo passo: o tokenizer precisa expor um tipo público para `peek_nested_delimited_identifier_quotes` (Dialect e Redshift). Na Fase 2, portar a lista "Membros adiados para a Fase 2".

### 2026-10-14
- Feito:
  - Análise técnica comparativa e plano (`docs/analise-tecnica-e-plano.md`).
  - Estrutura da solução em duas camadas (`SqlParser.Core`, port fiel; `SqlParser`, API pública).
  - Convenções, ADRs e skills.
  - Gerador de keywords, com 2 testes passando em net48/net8.0/net10.0.
- Decisões: ADR-0001 a ADR-0006.
- Próximo passo: portar `tokenizer.rs` e `dialect/` (partes que não dependem do parser), com os testes.
