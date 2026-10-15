# Onda D da Fase 2: correções

Correções levantadas pelas ondas B e C e pelas revisões R1–R3. A lista completa, com caixas de seleção, está em `docs/port/progresso.md`, seção "Pendências para a onda D". Execução em fila, com no máximo 5 agentes simultâneos, cada unidade de código num git worktree isolado. O integrador junta o resultado no `main` por cópia ou mesclagem de três vias.

| Unidade | Escopo | Status |
|---|---|---|
| D1 | Estouro de pilha (severidade Alta): `StackGuard` (= `stacker::maybe_grow`) nos pontos `#[recursive::recursive]`, `TryDecrease` fiel, sem `throw;` dentro de `catch` no caminho da recursão; DIV-005 reescrita | ✅ integrada (`D1.md`) |
| D2 | Lacunas do Core (`impl Ord for Ident`, `OneOrManyWithParensIntoIter`, `f.align()`) e achados baixos das revisões (listas estáticas somente leitura, arrays estáticos, docs verbatim, `PORT-DEVIATION DIV-007`) | ✅ integrada (`D2.md`) |
| D3 | Fuzzing do parser (somente leitura): SQL mutado do corpus dos testes contra os 16 dialetos; procura exceções que não sejam `ParserException`, travamentos e quebras de ida e volta | ✅ concluída (`D3.md`): 38 milhões de parses, nenhum crash; 1 divergência DIV-001 (keywords com ligaduras e ß), corrigível com mapeamento completo de maiúsculas |
| D4 | Notas do port em pt-BR fora da documentação XML pública (§12): movidas de `///` para comentários `//` | ✅ integrada (`D4.md`): 430 blocos em 192 arquivos escritos à mão + 158 gerados (geradores ajustados) |
| D5 | Correções pequenas: `()` como `ValueTuple` em `MsSqlDialect.ParseStatement` (caso 85); DIV-001 medida nos três alvos e marcada com `PORT-DEVIATION`; `try/catch` do caminho comum trocado pelos equivalentes exatos sem exceção (caso 81); `throw StackGuard.Rethrow(e)` nos relançamentos de erro guardado (caso 83); `cref` de `ISpanned` em `Span.cs` | ✅ integrada (`D5.md`) |
| D6 | Mapeamento completo de maiúsculas/minúsculas (achado da D3): `RustString.ToUppercase/ToLowercase` e `RustChar.ToUppercase/ToLowercase` sobre tabelas do Unicode 17.0.0 (o `char::UNICODE_VERSION` do Rust 1.99.0) geradas por `tools/codegen/Generate-CaseMapping.ps1`, com o sigma final; os 6 pontos `to_*case` do Core trocados; DIV-001 **resolvida** | ✅ concluída (`D6.md`): tabelas iguais às do Rust em todos os 1.112.064 scalar values; `DECLARE x INT; "<U+FB01>rst" INT` falha como no Rust |

Unidades paralelas da mesma leva, fora do Core: E2 (API pública, fase 2: visitors no estilo SqlParser-cs e utilitários de análise).
