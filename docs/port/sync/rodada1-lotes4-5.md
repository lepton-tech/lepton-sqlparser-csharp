# Rodada 1 de sincronização: lotes 4 e 5 (unidade U2)

Notas da execução dos lotes 4 e 5 da "Rodada 1" de [`../upstream-ledger.md`](../upstream-ledger.md) (`v0.63.0` → `origin/main` em `dacdbc9`). Procedimento: skills `sync-upstream`, `port-rust-to-csharp` e `port-rust-tests`. Unidade anterior: [`rodada1-lotes1-3.md`](rodada1-lotes1-3.md).

- **Data:** 2026-10-15.
- **Base:** worktree isolado, sincronizado com a cópia de trabalho do repositório principal (estado da unidade U1: 1.652 aprovados e 9 ignorados no Core; 220 na API pública).
- **Leitura do Rust:**
  - O `src/`, `tests/`, `derive/` e o `HEADER` de `dacdbc9` foram extraídos uma vez com `git archive` para uma pasta temporária.
  - O ambiente do worktree isolado bloqueia `git -C <clone>` no shell. Os diffs de cada PR vieram então de `https://github.com/apache/datafusion-sqlparser-rs/commit/<hash>.diff` (`curl`), com o mesmo conteúdo do `git show <hash>`.
  - O clone de referência não foi alterado (nada de checkout/reset).
- **Resultado:** 16 PRs `portado` (10 do lote 4 e 6 do lote 5). `portedThroughCommit` continua `b5950628` (#2496): o PR seguinte na ordem cronológica, #2403, é do lote 6. `baselineTag` continua `v0.63.0`, e os cabeçalhos dos arquivos também.

## Lote 4: `Display`/`ToString` (ordem cronológica estrita)

Todos mexem no braço `Expr::UnaryOp` de `impl Display for Expr` (`Ast/Mod/Expr.Display.cs`), no `Display` de `Value` (`Ast/Value/Value.cs`) ou em funções livres novas de `mod.rs`. Cada PR foi portado sobre o resultado do anterior, como no Rust, e a árvore ficou verde a cada passo.

| PR | Status | Arquivos C# | Testes | Observações |
|---|---|---|---|---|
| #2531 `@` com espaço | portado | `Expr.Display.cs` (`PGAbs` na lista que sai com espaço) | `parse_pg_unary_ops` re-portado (`SqlParserPostgresTests1`); `parse_pg_abs_space_before_negative_operand` (`SqlParserPostgresTests3`) | O `format!` do laço virou a lista de SQL completos, como no Rust |
| #2533 `~` com espaço | portado | `Expr.Display.cs`: o `if`/`else if`/`else` virou um `switch` exaustivo sobre `UnaryOperator`, um `case` por braço do `match`, com `default: throw` (§6, regra 3) | `parse_generic_unary_ops` re-portado e `parse_bitwise_not_renders_apart_from_operand` (`SqlParserCommonTests5`); `parse_bitwise_not_before_pg_prefix_operators` (`SqlParserPostgresTests3`) | O segundo `let select` do teste fica fora do laço que também declara `select`: virou `bitwiseNotSelect` (CS0136, caso 72) |
| #2532 `-` sem `--` | portado | `Expr.Display.cs` (braço `Minus`); `AstModule.A3.cs` (`StartsWithOperatorChar`, antes de `FmtNamedFunctionArg`, como no Rust) | `parse_unary_minus_never_renders_line_comment` (`SqlParserCommonTests5`); `parse_unary_minus_before_pg_prefix_operators` (`SqlParserPostgresTests3`) | O `struct FirstChar(Option<char>)` local com `impl fmt::Write` virou a classe privada `FirstChar : IFmtWrite`, e `write!(first, "{expr}")` virou `new Formatter(first).Write($"{expr}")` (caso 136). Os caracteres de `OPERATOR_CHARS` são ASCII, então testar a primeira unidade UTF-16 dá o resultado do primeiro scalar value |
| #2538 fatoriais seguidos | portado | `Expr.Display.cs` (braço `PGPostfixFactorial`) | `parse_postfix_factorial_spacing` (`SqlParserPostgresTests3`) | |
| #2544 aspas em `Word` | portado | `AstModule.cs` (`FmtIdent`, `internal`); `Ident.cs` e `Tokenizer/Word.cs` (os dois `Fmt` delegam a `FmtIdent`) | `test_word_display_quote_escaping` (`TokenizerTests`, no fim); `test_snowflake_stage_name_with_escaped_quotes` (`SqlParserSnowflakeTests2`) | `fmt_ident` fica no Rust entre `impl From<&str> for Ident` e `impl Display for Ident`; no C#, em `AstModule.cs`, com comentário no `Ident.cs` (caso 135). O tokenizer chama `Ast.AstModule.FmtIdent(..)` qualificado. As exceções `unexpected quote style` (`Ident`) e `Unexpected quote_style!` (`Word`) saíram, como os `panic!` no Rust. `Word.MatchingEndQuote` continua (o tokenizer usa) |
| #2545 `\|/` e `\|\|/` com espaço | portado | `Expr.Display.cs` | `parse_pg_unary_ops` re-portado de novo (`SqlParserPostgresTests1`); `parse_pg_roots_render_apart_from_operand` (`SqlParserPostgresTests3`) | |
| #2542 aspas em `X'..'` | portado | `Value.cs` (`HexStringLiteral` com `EscapeSingleQuoteString`) | `test_hex_string_literal_display_escaping` (`Ast/ValueTests`, que ganhou `using SqlParser.Core.Ast;`); `parse_hex_string_literal_display_escaping` (`SqlParserCommonTests5`) | |
| #2537 aspas em `B`/`R` | portado | `Value.cs` (4 variantes) | `test_byte_and_raw_string_literal_display_escaping` (`Ast/ValueTests`); `test_byte_and_raw_string_quote_escaping` (`SqlParserBigQueryTests`, no fim); `parse_bitstring_literal_escaping` (`SqlParserMySqlTests2`, antes de `parse_alter_table_column_position`, e `SqlParserPostgresTests3`, no fim) | `r#"SELECT B"it""s""#` termina com aspas: string comum com `\"` (skill `port-rust-tests`) |
| #2541 `!` com espaço | portado | `Expr.Display.cs` (`Minus or BangNot`); `AstModule.A3.cs` (`StartsWithOperatorChar` estrutural, laço sobre o `left` de `BinaryOp`; a classe `FirstChar` saiu junto com o código do #2532) | `parse_bang_not_renders_apart_from_operand` (`SqlParserCommonTests5`, no fim) | `fn starts_with_operator_char(mut expr: &Expr)`: o parâmetro é reatribuído no laço, como no Rust; `!matches!(op, Not)` → `op is not UnaryOperator.Not` (caso 106) |
| #2543 `.` perto de números | portado | `AstModule.cs` (`IsNumberExpr`); `AccessExpr.cs` (ganhou `using static ..AstModule`); `Expr.Display.cs` (`CompoundFieldAccess`) | `parse_compound_field_access_numeric_display` (`SqlParserCommonTests5`, entre `parse_alter_table_column_position` e `parse_placeholder_disallows_quoted_ident`) | `const fn is_number_expr` fica no Rust entre `enum AccessExpr` e o seu `Display`: caso 135, como o `fmt_ident` |

## Lote 5: AST aditiva

**Ordem:** cronológica (#2488 → #2569 → #2553 → #2515 → #2489 → #2574), a da skill `sync-upstream`. A ordem sugerida no ledger (#2488 → #2553 → #2489 e depois os demais) só agrupava os PRs de `parse_data_type`; a cronológica não teve conflito.

| PR | Status | Arquivos C# | Testes | Observações |
|---|---|---|---|---|
| #2488 `ARRAY(T [NOT NULL])` | portado | `ArrayElemTypeDef.cs` (`ParenthesisNotNull`); `DataType.cs` (`Display`); `Dialect.cs` (`SupportsArrayTypedefWithParentheses`, `SupportsArrayElementNotNull`, depois de `SupportsArrayTypedefWithoutElementType`); `ClickHouseDialect.cs`; `SnowflakeDialect.cs`; `Parser.Part3c.cs` (`ParseDataTypeHelper`, braço `ARRAY`) | `reject_angle_bracket_array_type` (`SqlParserClickHouseTests`, depois de `parse_create_table_with_nested_data_types`); `test_structured_array_type` (`SqlParserSnowflakeTests2`) | Sai o `_dialect is ClickHouseDialect` com `ParseSubType` (que continua em uso por `Nullable`/`LowCardinality`). `self.expected("(", self.peek_token())` → `Expected<DataType>("(", PeekToken())` |
| #2569 `MODIFY ORDER BY` | portado | `AlterTableOperation.cs` (`ModifyOrderBy(OneOrManyWithParens<Expr> OrderBy)` e `Display`); `Spans.Part2.cs`; `Parser.Part2c.cs` (`ParseCreateTable` usa `ParseSortingKey`); `Parser.Part3a.cs` (`ParseSortingKey`, `internal`, no fim, antes de `parse_alter_table_operation`); `Parser.Part3b.cs` (`ParseAlterTableOperation`) | `test_alter_table_modify_order_by_span` (`Ast/SpansTests`, no fim); `parse_alter_table_modify_order_by` (`SqlParserClickHouseTests`) | O local `orderByColumns` de `ParseCreateTable` (caso 72) saiu com o trecho extraído; em `ParseSortingKey` o nome volta a ser `columns` |
| #2553 `COLLATE` em tipos | portado | `DataType.cs` (`Collate(DataType DataType, ObjectName Collation)` depois de `String`, e `Display`); `Dialect.cs` (`SupportsDataTypeCollation`, depois de `SupportsCreateViewCommentSyntax`); `DatabricksDialect.cs`; `Parser.Part1d.cs` (`ParseStructFieldDef`); `Parser.Part3c.cs` (`ARRAY<..>`, `MAP<..>` e `ParseDataTypeWithOptionalCollation`, no fim) | `parse_databricks_collated_data_types` (`SqlParserDatabricksTests`, no fim) | Nomes da variante-tupla em `nomes.md`. O `parser_err!(.., self.peek_token_ref())` da chave do `MAP<..>` segue o caso 88 |
| #2515 `CREATE FOREIGN TABLE` | portado | `Ast/Ddl/CreateForeignTable.cs` (novo: record `partial`, `[VisitWith("visit_relation")]` em `Name`, `Display`, `Span()` no próprio arquivo, caso 114); `Statement.cs` (variante depois de `CreateServer`); `Statement.Display.cs`; `Statement.From.Ddl.cs`; `Spans.Part1.cs`; `CreateServerOption.cs` (doc nova do Rust); `Parser.Part2a.cs` (`ParseCreate`: `modifierLoc` e o braço `FOREIGN TABLE`); `Parser.Part4a.cs` (`ParseIdentifier`: `WithQuoteAndSpan`); `Parser.Part5d.cs` (`ParsePgOptionsClause`, `internal`, e `ParseCreateForeignTable`, `public`, depois de `ParsePgCreateServer`) | `test_create_foreign_table_option_spans` (`Ast/SpansTests`, antes de `test_alter_table_modify_order_by_span`, como em `dacdbc9`); `parse_create_foreign_table`, `parse_create_foreign_table_requires_column_list`, `parse_create_foreign_table_rejects_modifiers` e `parse_create_foreign_table_with_check_constraint` (`SqlParserPostgresTests3`, depois de `parse_lock_table`) | `global.is_some()` usa o `global` sombreado (`Option<bool>`), que no C# é `globalOption` (caso 72 da U1). `assert!(err.to_string().contains(..), "..")` → `Assert.Contains(..)` com a mensagem em comentário, como no arquivo. Locais renomeados nos testes (CS0128/CS0136, caso 72): `optionsSql`, `constraintOnlyStmt`. A camada pública classifica a variante nova como `Create` pelo prefixo do nome (`SqlAnalysisExtensions`), sem mudança |
| #2489 `OBJECT(..)` e `MAP(.., T NOT NULL)` | portado | `DataType.cs` (`Object(Sequence<ColumnDef> Value)` depois de `Nested`, e `Display` de `Object` e de `ParenthesesNotNull`); `MapBracketKind.cs` (`ParenthesesNotNull` entre `Parentheses` e `AngleBrackets`); `Dialect.cs` (`SupportsMapTypedefWithParentheses`, `SupportsMapValueNotNull`); `ClickHouseDialect.cs`; `GenericDialect.cs` (override no topo da classe, como no Rust); `SnowflakeDialect.cs`; `Parser.Part1d.cs` (`ParseParenthesizedMapTypeDef`, ex-`ParseClickHouseMapDef`); `Parser.Part3c.cs` (braço `OBJECT` antes de `STRUCT`; braço `MAP` com a flag no lugar de `dialect is ClickHouseDialect or GenericDialect`); `Parser.Part4a.cs` (`ParseStructuredObjectTypeDef`, entre `ParseOptionalTypeModifiers` e `ParseSubType`) | `parse_object_type_parameter` (`SqlParserClickHouseTests`); `test_structured_object_type` (`SqlParserSnowflakeTests2`) | `self.maybe_parse(..)?` sobre `Vec<ColumnDef>` → `MaybeParseValue` (`Sequence<T>` é `struct`, caso 48). `let [P] = statements.as_slice() else { .. }` → padrão de lista (caso 102). **Quebra binária:** `MapBracketKind` é um `enum` C# (§6, regra 1) e `AngleBrackets` passou de 1 a 2 |
| #2574 `t.1` no ClickHouse | portado | `Dialect.cs` (`SupportsTupleElementAccess`, depois de `SupportsNumericPrefix`); `ClickHouseDialect.cs` (no fim); `Tokenizer.cs` (braço de números e ponto: duas verificações novas, antes e logo depois da primeira chamada a `TokenizeNumberPart`); `Parser.Part1a.cs` (`ParseWildcardExpr`) | `tokenize_period_before_digits_after_identifier_or_bracket` (`TokenizerTests`, no fim); `parse_tuple_element_access` (`SqlParserClickHouseTests`), com `Skip = "DIV-006: ..."` | O `break` do `while` dentro do `match` virou a flag `stop` (caso 100). O `fuzz_seeds/*.sql` do PR é infraestrutura. **Skip:** `one_statement_parses_to("SELECT .5, 1e3", "SELECT 0.5, 1000")` depende da normalização do `BigDecimal`; o CI do upstream roda `cargo test --all-features`, e sem a feature o Rust também devolve `SELECT .5, 1e3`. As demais asserções do teste passam (conferido comentando só essa linha numa execução local, desfeita em seguida). DIV-006 ampliada |

### Visitor: árvore Rust sintética

O `Generate-Visitor.ps1` confere a AST C# contra **uma** árvore Rust (`-Upstream`), mas, com os lotes fora de ordem, nenhum commit do upstream tem exatamente a AST portada. Solução usada a cada PR que muda a AST:

1. Copiar `src/` e `HEADER` de `dacdbc9` (extraídos com `git archive`) para uma pasta temporária e normalizar os `.rs` para LF (o `git archive` gravou CRLF; os diffs do GitHub estão em LF).
2. De cada PR ainda não portado que mexe em `src/ast/`, do mais novo para o mais antigo, separar só as seções `diff --git a/src/ast/...` e aplicar com `patch -R -p1` (o `patch.exe` do Git for Windows). Todos os hunks reverteram sem conflito.
3. Rodar `pwsh tools/codegen/Generate-Visitor.ps1 -Upstream <pasta>`; no fim, `-Check`.

Estado final: com `dacdbc9` menos os hunks de AST de #2598, #2510, #2540 e #2403, o `-Check` diz "Código gerado do visitor está atualizado". Contra `dacdbc9` puro, restam 4 erros, todos do lote 6: `Insert` (27 campos no Rust, 26 no C#), `Expr.InList` e `Expr.InSubquery` (4 × 3) e `RepetitionQuantifier::Reluctant`. Os 6 erros dos lotes 5 listados na U1 sumiram. Os arquivos gerados que mudaram: `Ast.DataType.g.cs`, `Ast.Ddl.g.cs` e `Ast.Mod.g.cs`.

## Decisões

- **convencoes.md §13, caso 135:** função livre de `mod.rs` declarada entre um tipo e o seu `impl` (`fmt_ident`, `is_number_expr`) → `internal static` em `Ast/Mod/AstModule.cs`, na ordem de `mod.rs`, com comentário no arquivo do tipo; chamada do tokenizer qualificada (`Ast.AstModule.FmtIdent`).
- **convencoes.md §13, caso 136:** `struct` local com `impl fmt::Write` → classe privada `: IFmtWrite` + `new Formatter(destino).Write(..)`.
- **nomes.md:** `DataType::Collate(Box<DataType>, ObjectName)` → `Collate(DataType DataType, ObjectName Collation)`; a tupla de `parse_parenthesized_map_type_def` → `(DataType KeyDataType, DataType ValueDataType, bool ValueNotNull)` (a linha de `parse_click_house_map_def` foi atualizada).
- **divergencias.md, DIV-006:** ampliada para testes sem `cfg` que dependem do `BigDecimal` (o CI do upstream usa `--all-features`); `parse_tuple_element_access` com `Skip`.
- Locais renomeados nos testes (caso 72; não são API): `bitwiseNotSelect` (`parse_generic_unary_ops`), `optionsSql` e `constraintOnlyStmt` (testes de `CREATE FOREIGN TABLE`), `castDataType` e `castFields` (`test_structured_object_type`), `genericTokens` (`tokenize_period_before_digits_after_identifier_or_bracket`).
- Nenhum teste portado foi editado para passar; os testes que o upstream alterou (`parse_generic_unary_ops`, `parse_pg_unary_ops` duas vezes) foram re-portados na versão nova.

## Verificações

- `dotnet build SqlParser.sln -nologo -v q`: 0 avisos, 0 erros.
- `dotnet test SqlParser.sln -nologo -v q`: `SqlParser.Core.Tests` 1.682 aprovados e 10 ignorados em net48, net8.0 e net10.0 (antes, 1.652 e 9: +31 testes novos, 1 deles ignorado); `SqlParser.Tests` 220 nos três alvos.
  - Por PR: o Core foi testado em net8.0 depois de cada PR; build e testes completos nos três alvos no fim do lote 4 e no fim do lote 5.
- `pwsh tools/check/Find-SuspiciousChars.ps1 -Path src,tests`: as mesmas 14 linhas de antes (todas anteriores a esta unidade). Nenhum escape `\uXXXX` foi escrito nesta unidade.
- `Generate-Visitor.ps1 -Check`: atualizado contra a árvore sintética; 4 erros esperados (lote 6) contra `dacdbc9`.

## Pendências e cuidados para o lote 6

1. **Visitor:** ao portar cada PR do lote 6, refazer a árvore sintética sem reverter esse PR. Depois do #2598, a árvore é o próprio `dacdbc9` e o `-Check` deve passar direto.
2. **Keywords:** continua valendo a pendência 1 da U1 (`Generate-Keywords.ps1 -Source` a partir de `dacdbc9`).
3. **`portedThroughCommit`:** com o lote 6, todos os 63 PRs ficam fechados, e o valor pode ir direto a `dacdbc9`.
4. **API:** o lote 6 traz as quebras já listadas no ledger (`Insert.ByName`, `Table.TableName`/`SchemaName` como `Ident?`, `Expr.InList`/`InSubquery.Global`, `Parser.ParseIn(expr, negated, global)`). Somam-se às desta unidade: `MapBracketKind.AngleBrackets` renumerado e os records novos/variantes novas (aditivos).

### Posição dos testes do lote 6

Os testes novos entram na posição do Rust em `dacdbc9`. Estado atual dos arquivos C#:

- `SqlParserCommonTests5` (fim):
  - `parse_insert_by_name` (#2403): entre `parse_function_arg_call_chain_no_exponential_blowup` e `parse_bitwise_not_renders_apart_from_operand`;
  - `parse_table_preserves_quotes_and_trailing_tokens` (#2540): entre `parse_unary_minus_never_renders_line_comment` e `parse_hex_string_literal_display_escaping`;
  - `parse_in_list_leading_subquery` e `parse_in_list_nested_leading_subquery_no_exponential_blowup` (#2586): entre `parse_placeholder_disallows_quoted_ident` e `parse_bang_not_renders_apart_from_operand`;
  - os três `parse_position_*` (#2619): depois de `parse_bang_not_renders_apart_from_operand`, no fim (`parse_position_in_connect_by_list_after_state_change` tem `#[cfg(feature = "visitor")]`; o visitor já existe, então roda).
- `SqlParserPostgresTests3`: `parse_insert_by_name_keywords_as_table_and_alias` (#2403) entre `parse_trim_from_without_characters` e `parse_pg_abs_space_before_negative_operand`.
- `SqlParserDatabricksTests`: `test_databricks_insert_by_name` (#2403) entre `parse_cte_without_as` e `parse_databricks_query_entry_points`.
- `SqlParserClickHouseTests`: `parse_global_in`, `parse_in_unparenthesized_expr_precedence`, `global_in_bare_table_binds_before_and` e `global_in_bare_table_binds_before_equality` (#2598) depois de `parse_tuple_element_access` e antes das funções auxiliares (`Clickhouse()` …).
- Testes existentes que o lote 6 altera: `test_simple_postgres_insert_with_alias` e `test_simple_insert_with_quoted_alias` (`SqlParserPostgresTests2`, campo `by_name`, #2403); `parse_create_table_as_table` (#2540), `test_match_recognize_patterns` (#2510), `parse_in_list`, `parse_in_subquery`, `parse_in_union` e `parse_not_precedence` (#2598) nas fatias `SqlParserCommonTests1`, `2` e `4`; `parse_in_list_leading_subquery` (#2598, depois de entrar com o #2586).
