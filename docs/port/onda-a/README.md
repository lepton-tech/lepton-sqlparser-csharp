# Onda A da Fase 2: port da AST (notas dos agentes)

Cada unidade de trabalho da onda A registra aqui, em `<unidade>.md`, as decisões que tomou:
- casos novos para `convencoes.md` §13;
- nomes para `nomes.md`;
- divergências;
- pendências.

Escrevem em arquivos separados para evitar edições concorrentes nos documentos compartilhados. **Consolidado em 2026-10-14**: as decisões viraram os casos 58–78 de `convencoes.md` §13; os nomes estão em `nomes.md` ("AST (onda A)"); a única divergência nova virou DIV-005. Esta pasta fica como **histórico** (motivos detalhados de cada decisão). Unidades A3, A4 e A8 foram interrompidas antes de escrever suas notas; o código delas está completo e compila.

| Unidade | Trecho Rust (v0.63.0) | Destino C# |
|---|---|---|
| A1 | `src/ast/mod.rs` 491–3575 (de `Array` até antes de `Statement`, inclui `Expr`) | `Ast/Mod/` |
| A2 | `src/ast/mod.rs` 3576–6521 (`Statement` + `impl Display for Statement`) | `Ast/Mod/` |
| A3 | `src/ast/mod.rs` 6522–9461 | `Ast/Mod/` |
| A4 | `src/ast/mod.rs` 9462–12949 (inclui os 10 testes) | `Ast/Mod/` + `tests/.../Ast/` |
| A5 | `src/ast/ddl.rs` 1–2909 | `Ast/Ddl/` |
| A6 | `src/ast/ddl.rs` 2910–5997 | `Ast/Ddl/` |
| A7 | `src/ast/query.rs` (inteiro) | `Ast/Query/` |
| A8 | `data_type.rs`, `dml.rs`, `table_constraints.rs`, `value.rs` (+1 teste), `dcl.rs`, `operator.rs`, `trigger.rs`, `helpers/{stmt_create_table,stmt_create_database,stmt_data_loading,key_value_options}.rs` | `Ast/DataType/`, `Ast/Dml/`, `Ast/TableConstraints/`, `Ast/Value/`, `Ast/Dcl/`, `Ast/Operator/`, `Ast/Trigger/`, `Ast/Helpers/` |

Já portados pelo integrador (não refaça): `mod.rs` 1–490 (`DisplaySeparated`, `display_separated`, `display_comma_separated`, `format_statement_list`, `Parens`, `Ident`, `ObjectName`, `ObjectNamePart`, `ObjectNamePartFunction`) e `helpers/attached_token.rs`; também `display_utils.rs` (`src/SqlParser.Core/DisplayUtils/`).
