# Onda D da Fase 2: correções

Correções levantadas pelas ondas B e C e pelas revisões R1–R3. A lista completa, com caixas de seleção, está em `docs/port/progresso.md`, seção "Pendências para a onda D". Execução em fila, com no máximo 5 agentes simultâneos, cada unidade de código num git worktree isolado. O integrador junta o resultado no `main` por cópia ou mesclagem de três vias.

| Unidade | Escopo | Status |
|---|---|---|
| D1 | Estouro de pilha (severidade Alta): `StackGuard` (= `stacker::maybe_grow`) nos pontos `#[recursive::recursive]`, `TryDecrease` fiel, sem `throw;` dentro de `catch` no caminho da recursão; DIV-005 reescrita | ✅ integrada (`D1.md`) |
| D2 | Lacunas do Core (`impl Ord for Ident`, `OneOrManyWithParensIntoIter`, `f.align()`) e achados baixos das revisões (listas estáticas somente leitura, arrays estáticos, docs verbatim, `PORT-DEVIATION DIV-007`) | ✅ integrada (`D2.md`) |
| D3 | Fuzzing do parser (somente leitura): SQL mutado do corpus dos testes contra os 16 dialetos; procura exceções que não sejam `ParserException`, travamentos e quebras de ida e volta | em andamento |
