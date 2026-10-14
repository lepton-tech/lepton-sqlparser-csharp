# ADR-0005: Testes e validação de paridade

- **Status:** Aceita
- **Data:** 2026-10-14

## Contexto

- O requisito "evitar divergências silenciosas" exige que a paridade seja **medida**, e não suposta.
- Na análise, 319 casos SQL do Rust eram aceitos pelo SqlParser-cs com saída diferente, sem erro.
- Precedentes:
  - o TypeScript 7 mediu a paridade pela suíte de testes original;
  - o compilador C da Anthropic usou o GCC como oráculo;
  - McKeeman (1998) definiu o teste diferencial.

  Ver `docs/port/referencias-e-boas-praticas.md`.

## Decisão

1. **Testes do Rust portados são a especificação** (skill `port-rust-tests`):
   - mesmo nome, mesmos dados, mesma ordem;
   - um teste portado **nunca** é editado para passar.
2. **Falhas conhecidas ficam visíveis:**
   - `Skip = "DIV-NNN: ..."` para divergência registrada em `docs/port/divergencias.md`;
   - `Skip = "PENDENTE: ..."` para dependência ainda não portada.
3. **Três runtimes em todo `dotnet test`:** net48, net8.0 e net10.0. A biblioteca é `netstandard2.0`, e o .NET Framework revela diferenças de BCL que o .NET moderno esconde.
4. **Oráculo Rust (planejado para a Fase 2):**
   - ferramenta só de desenvolvimento, em `tools/oracle`, com o sqlparser-rs na mesma versão de `UPSTREAM.json` e a feature `serde`;
   - para cada SQL do corpus extraído dos testes do Rust, gera SQL canônico, AST em JSON e mensagem de erro;
   - o C# compara, de preferência como snapshots versionados.
   - **Pré-requisito:** toolchain Rust (`cargo`) na máquina de desenvolvimento. Hoje não está instalado. Nunca é dependência de runtime.
5. **Revisão linha a linha** de cada unidade portada, por subagente com contexto limpo, usando `.claude/skills/port-rust-to-csharp/references/checklist-revisao.md`.

## Consequências

- O placar de paridade (testes portados passando / total) fica em `docs/port/progresso.md` e alimenta os gatilhos do [ADR-0001](0001-abordagem-port-independente-e-plano-b.md).
- Testes de comportamento do .NET Framework rodam só no Windows.
