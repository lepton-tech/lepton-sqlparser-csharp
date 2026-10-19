# Onda B da Fase 2: port do parser (`src/parser/`)

**Consolidado** em `convencoes.md` §13 (casos 79–122) / `nomes.md` (seção "Parser e spans (onda B)") / `divergencias.md` (DIV-007; DIV-001 ampliada) em 2026-10-14. As decisões de teste foram para a skill `port-rust-tests`; as pendências, para `progresso.md` ("Pendências para a onda D"). Esta pasta fica como **histórico** (motivos detalhados de cada decisão).

**Execução sequencial, um agente por vez** (decisão do usuário em 2026-10-14: paralelismo alto esgotou os créditos). Cada unidade registra suas decisões em `onda-b/<unidade>.md`; o integrador consolida em `convencoes.md`/`nomes.md` ao fim da onda.

Já feito (B0, integrador): `Parser/Parser.cs` (linhas 1–610), `Parser/Parser.TokenHelpers.cs` (4466–5201), `ParserError.cs`, `RecursionCounter.cs`, `ParserSupportTypes.cs`, hooks do parser em `Dialects/Dialect.cs`.

| Unidade | Trecho Rust (v0.63.0) | Destino C# | Status |
|---|---|---|---|
| B1 | `parser/mod.rs` 611–4465 (`parse_statement` … expressões, até antes de `get_next_precedence`) | `Parser/Parser.Part1*.cs` | ✅ (108 fn; notas em `B1.md`) |
| B2 | `parser/mod.rs` 5202–9342 (`parse_all_or_distinct` … até antes de `parse_plain_options`) | `Parser/Parser.Part2*.cs` | ✅ (91 fn; notas em `B2.md`) |
| B3 | `parser/mod.rs` 9343–13249 (`parse_plain_options` … `parse_data_type_helper`) | `Parser/Parser.Part3*.cs` | ✅ (91 fn; notas em `B3.md`) |
| B4 | `parser/mod.rs` 13250–17265 (`parse_returns_table_column` … `parse_match_recognize`) | `Parser/Parser.Part4*.cs` | ✅ (104 fn; notas em `B4.md`) |
| B5 | `parser/mod.rs` 17266–21230 (`parse_base_pattern` … `parse_reset`) | `Parser/Parser.Part5*.cs` | ✅ (138 fn; notas em `B5.md`) |
| B6 | `parser/mod.rs` 21231–21269 (`maybe_prefixed_expr`, `impl Word`) + `parser/alter.rs` + `parser/merge.rs` + overrides de dialeto pendentes (lista em `progresso.md`); os testes passaram para a B8 (ajuste abaixo) | `Parser/Parser.Alter.cs`, `Parser/Parser.Merge.cs`, `Parser/Word.Parser.cs`, `Parser/Parser.Part5d.cs` (fim), `Dialects/` | ✅ (8 fn de `alter.rs`, 6 de `merge.rs`, 3 de `mod.rs`; 23 overrides + 34 funções de dialeto; notas em `B6.md`) |
| B7 | `ast/spans.rs` (trait `Spanned`, `union_spans`, 120 `impl` e os 18 testes) + os 25 `impl Spanned` de `ddl.rs`, `dcl.rs` e `table_constraints.rs`; ativa `parse_with_wrapped_dialect` | `Ast/Spans/*.cs`, os arquivos dos 25 tipos, `Support/RustIter.cs`, `tests/.../Ast/SpansTests.cs` | ✅ (145 `impl`; Core compila; 115/118 testes passam e 3 ignorados preexistentes em net48, net8.0 e net10.0; notas em `B7.md`) |

Durante a onda, o build do Core só fica verde quando todas as unidades terminarem: chamadas a funções `Parse*` de unidades posteriores são esperadas (CS1061/CS0103).

**Ajustes de plano após a B1:**
- **B7 (nova):** `ast/spans.rs`, o trait `Spanned`, mais os `impl Spanned` espalhados (`PORT-TODO`). O parser usa `Expr.Span()`/`AccessExpr.Span()` em caminhos de erro, então o Core só compila depois dela.
- `impl Word` de `parser/mod.rs` (21242, `to_ident`/`into_ident`) é usado pela B1. A B6 deve tornar `Tokenizer/Word.cs` um `partial record` e pôr os métodos em `Parser/Word.Parser.cs`.
- Cada unidade deve ler as **assinaturas supostas** pelas anteriores (`onda-b/B*.md`, seção "Pendências e suposições") e implementar as funções do seu trecho com exatamente essas assinaturas. Se a regra exigir outra, a unidade ajusta os chamadores e registra.

**Ajuste após a B5:** a B6 fica só com **código** (`alter.rs`, `merge.rs`, `maybe_prefixed_expr`, `impl Word`, overrides de dialeto pendentes); depois vem a B7 (spans) e então a **B8**: testes do `parser/mod.rs` (21270–fim), partes de `test_utils.rs` que usam o parser e testes de `spans.rs`. Objetivo: o Core compilar logo após B6 + B7.
