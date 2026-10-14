# Progresso do port

**Leia este arquivo no início de cada sessão** e **atualize-o ao final**. É o "estado salvo" do projeto: diz o que já foi portado, de qual versão do upstream e o que vem em seguida. Assim nenhuma sessão precisa reconstruir o contexto do zero.

- **Baseline do upstream:** `v0.63.0` (`85b1a6f`). Ver `UPSTREAM.json`.
- **Fase atual:** 1, léxico e infraestrutura (ver o plano em [`../analise-tecnica-e-plano.md`](../analise-tecnica-e-plano.md) §8).

## Estado por arquivo Rust

Legenda: ⬜ não iniciado · 🟨 em andamento · ✅ portado (testes portados passando) · ⏸️ adiado (motivo na nota)

| Arquivo Rust (v0.63.0) | Linhas | Destino C# | Status | Testes Rust → C# | Notas |
|---|---|---|---|---|---|
| `src/keywords.rs` | 1.321 | `Keywords.g.cs` + `Keywords.cs` | ✅ | — (+2 testes próprios) | Gerado por `tools/codegen/Generate-Keywords.ps1` |
| `src/tokenizer.rs` | 4.555 | `Tokenizer/` (+ `Ast/Value/DollarQuotedString.cs` e `QuoteDelimitedString.cs` de `ast/value.rs`; `Support/PeekableChars.cs`, `Support/RustChar.cs`) | ✅ | 67/67, passando em net48/net8.0/net10.0 (+ 4 testes próprios em `TokenizerPortTests`) | Linhas 1–2636 completas. Divergências: DIV-003 e DIV-004. Decisões: convencoes §13, casos 17 a 28 |
| `src/dialect/mod.rs` | 2.150 | `Dialects/Dialect.cs` + `Dialects/Precedence.cs` | 🟨 | 5/5 (4 passando, 1 `Skip` PENDENTE: `parse_with_wrapped_dialect`) | Parte da Fase 1 concluída: 154 de 168 membros do trait, `Precedence` e `dialect_from_str`. Pendentes: 13 (Fase 2); `fn dialect()` não portado (convencoes §13, caso 8). Lista abaixo |
| `src/dialect/*.rs` (16 dialetos) | 4.828 | `Dialects/<Struct>.cs` (16 arquivos) | 🟨 | — (sem testes no Rust) | Parte da Fase 1 concluída: 418 de 441 overrides. Os 23 restantes e as funções auxiliares de MsSql, MySQL e Snowflake foram portados na Fase 2 (B6). Lista abaixo |
| `src/ast/**` (exceto `spans.rs` e `visitor.rs`) | ~28.500 | `Ast/` (`Mod/`, `Query/`, `Ddl/`, `Dml/`, `Dcl/`, `DataType/`, `Value/`, `Operator/`, `Trigger/`, `TableConstraints/`, `Helpers/`, `Comments/`) | ✅ (compila) | testes escritos: `mod.rs` (10), `comments.rs` (1), `value.rs` (1, `Skip` DIV-003), builders (4); só rodam quando o parser compilar | Onda A (`docs/port/onda-a/`). `impl Spanned` espalhados viraram `PORT-TODO` |
| `src/ast/spans.rs`, `src/ast/visitor.rs` | ~4.500 | `Ast/Spans/`, `Ast/Visitor/` | ⬜ | 0/25 | Depois do parser |
| `src/parser/**` | ~23.100 | `Parser/` | 🟨 | 0/19 | B0 pronto (infraestrutura e navegação de tokens). Onda B sequencial: `docs/port/onda-b/README.md` |
| `src/test_utils.rs` | 505 | `tests/.../TestUtils/` (`TestedDialects.cs`, `TestUtils.cs`) | 🟨 | — (sem testes no Rust) | Portadas as partes sem parser: `TestedDialects` (`new`, `dialects`, `tokenizes_to`), `all_dialects`, `all_dialects_where`, `all_dialects_except`, `assert_eq_vec`, `only`. Pendentes da Fase 2 listados abaixo |
| `tests/sqlparser_*.rs` | ~55.000 | `tests/.../Integration/` | ⬜ | 0/1.490 | Fases 2–4 |

### Membros adiados para a Fase 2 (dependem de Parser/AST)

Cada item tem, no arquivo C#, um comentário `// PENDENTE (...)` na posição exata do Rust (convencoes.md §13, caso 16). Ao portar, troque o comentário pelo membro. Os hooks de `Dialect` que recebem `Parser` vão para `Dialects/Dialect.Parser.cs` (`partial class Dialect`, convencoes.md §2).

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
- Teste `parse_with_wrapped_dialect` (`Dialects/DialectTests.cs`): `Skip = "PENDENTE: depende do Parser (Fase 2)"`; o esboço da tradução está comentado no corpo.

**Não portados por decisão** (convencoes.md §13, caso 8; DIV-002): `fn dialect() -> TypeId` (`mod.rs:209`), `impl dyn Dialect { fn is::<T>() }` (`mod.rs:1899`), macros `dialect_of!`/`dialect_is!` (`mod.rs:155`/`164`) e o reexport de `derive_dialect!` (`mod.rs:133`). No C#, use `dialect is A or B`.

### Pendências de `test_utils.rs` (dependem do Parser/AST, Fase 2)

Marcadas com `// PENDENTE (Fase 2)` em `tests/SqlParser.Core.Tests/TestUtils/`:
- `TestedDialects`: campos `options: Option<ParserOptions>` e `recursion_limit: Option<usize>`; `new_with_options`, `with_recursion_limit`, `new_parser`, `one_of_identical_results`, `run_parser_method`, `parse_sql_statements`, `one_statement_parses_to`, `statements_parse_to`, `expr_parses_to`, `verified_stmt`, `verified_query`, `verified_query_with_canonical`, `verified_only_select`, `verified_only_select_with_canonical`, `verified_expr`.
- `TestedDialects.TokenizesTo`: o ramo `if let Some(options) = &self.options { tokenizer = tokenizer.with_unescape(options.unescape) }` (hoje `options` é sempre `None`, então o comportamento é o mesmo).
- Funções livres: `all_dialects_with_options`, `expr_from_projection`, `alter_table_op_with_name`, `alter_table_op`, `number`, `single_quoted_string`, `table_alias`, `table`, `table_from_name`, `table_with_alias`, `join`, `call`, `index_column`.

## Diário de sessões

Registre ao fim de cada sessão: data, o que foi feito, decisões tomadas e próximo passo. Entradas mais recentes no topo.

### 2026-10-14 (Fase 2: marco 1, parser compilando)
- **Onda B** (parser), em sequência, um agente por vez: B1–B5 (`parser/mod.rs` completo), B6 (`alter.rs`, `merge.rs`, overrides de dialeto, `impl Word`) e B7 (`ast/spans.rs` + os `impl Spanned` espalhados). Notas e decisões em `docs/port/onda-b/B*.md`, **ainda não consolidadas** em `convencoes.md`/`nomes.md`.
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
