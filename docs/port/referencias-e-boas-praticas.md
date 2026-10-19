# Referências e boas práticas para o port

Este documento reúne **fontes externas verificadas** sobre ports que preservam estrutura, tradução de código assistida por IA, consistência de agentes entre sessões e testes diferenciais. Para cada prática, diz **como ela é aplicada neste projeto**. É a fundamentação das regras de [`convencoes.md`](convencoes.md) e das skills em `.claude/skills/`.

- **Pesquisa:** 2026-10-14. Todas as URLs foram abertas e conferidas nessa data.
- **Citações:** trechos entre aspas são literais; o resto é paráfrase.
- **Ressalvas:** a seção 7 lista o que não pôde ser confirmado.

---

## 1. Precedentes: ports fiéis que acompanham um projeto original

### P1. TypeScript 7: port do compilador TypeScript para Go (Microsoft)

**Fontes:**
- "Why Go?", Ryan Cavanaugh, 2025-03-05: https://github.com/microsoft/typescript-go/discussions/411
- "A 10x Faster TypeScript", Anders Hejlsberg, 2025-03-11: https://devblogs.microsoft.com/typescript/typescript-native-port/
- "Progress on TypeScript 7 – December 2025": https://devblogs.microsoft.com/typescript/?p=5013
- "Announcing TypeScript 7.0 Beta", 2026-04-21: https://devblogs.microsoft.com/typescript/announcing-typescript-7-0-beta

**O que diz:**
- Foi um **port, não uma reescrita**: "we're undertaking this more as a port that maintains the existing behavior and critical optimizations".
- A equipe exigiu manter "the new codebase as compatible as possible, both in terms of semantics and in terms of code structure", porque as duas bases conviveriam por muito tempo e mudanças precisariam passar facilmente de uma para outra.
- O verificador de tipos é "structurally identical" ao original.
- A paridade foi medida pela suíte de testes original: de ~6.000 testes com erro no 6.0, "In all but 74 cases, TypeScript 7 also produces at least one error".
- A linha antiga foi congelada para não complicar a passagem de mudanças entre as duas bases.

**Como aplicamos:**
- Estrutura 1:1 é **requisito**, não estilo (`convencoes.md` §1).
- A paridade é medida pelos testes do Rust portados ([skill `port-rust-tests`](../../.claude/skills/port-rust-tests/SKILL.md)).
- O baseline fica fixado em uma tag (`UPSTREAM.json`).

### P2. Práticas do repositório typescript-go

**Fonte:** https://github.com/microsoft/typescript-go (`.github/copilot-instructions.md`, `CONTRIBUTING.md`, `CHANGES.md`)

**O que diz:**
- Há um submódulo Git apontando para "the commit being ported". As instruções para agentes dizem que o código em `internal` "is ported from the code in `_submodules/TypeScript`".
- As diferenças conhecidas ficam como arquivos `.diff` de baseline, e "A reduction/removal of `.diff` file baselines is **ideal**".
- Desvios intencionais são listados em `CHANGES.md`.
- Regras de trabalho:
  - um teste mínimo antes da mudança;
  - build, testes, lint e formatação verdes ao fim de cada sessão.

**Como aplicamos:**
- `upstream/` + `UPSTREAM.json` fazem o papel do submódulo. Ver ADR-0004; quando o repositório Git for criado, avaliar o submódulo.
- `CLAUDE.md` + skills fazem o papel das instruções para agentes.
- `divergencias.md` corresponde ao `CHANGES.md` e aos `.diff`.
- Sessão só termina com build e testes verdes (skill `port-rust-to-csharp` §5).

### P3. Apache Lucene.NET: port linha a linha de Java para C#

**Fontes:**
- https://lucenenet.apache.org/
- https://github.com/apache/lucenenet/blob/master/CONTRIBUTING.md
- Exemplo real: `src/Lucene.Net.Queries/CommonTermsQuery.cs`

**O que diz:**
- Meta declarada: "Maintain the existing line-by-line port from Java to C#".
- Cada arquivo tem um cabeçalho de versão (`// Lucene version compatibility level 4.8.1`).
- Desvios ficam marcados no código com uma tag buscável (`// LUCENENET specific - ...`), e pendências com `LUCENENET TODO`.
- A revisão é feita "line by line, comparing the code against the original ... Many of the bugs have been found this way, as the tests are not showing them".
- **Alerta:** o projeto continua "Working toward Lucene.Net 4.8.0 (currently in BETA)". Um port manual sem automação de sincronização fica para trás.

**Como aplicamos:**
- Cabeçalho com a versão do upstream em todo arquivo portado (`convencoes.md` §3).
- Tags buscáveis `PORT-DEVIATION` e `PORT-TODO` (`convencoes.md` §12).
- Revisão linha a linha por um subagente com contexto limpo (skill `port-rust-to-csharp` §5).
- Automação de sincronização (skill `sync-upstream`).

### P4. iText para .NET

**Fonte:** https://github.com/itext/itext-dotnet

**O que diz:**
- "We develop in Java first, and then port to .NET".
- Um arquivo na raiz (`port-hash`) guarda o commit de origem já portado.
- Os commits citam a origem: "Autoported commit. Original commit hash: [...]".

**Como aplicamos:**
- `UPSTREAM.json` → `portedThroughCommit`.
- Toda mudança de sincronização cita o PR e o hash do upstream (skill `sync-upstream`).

### P5. Outros ports do sqlparser-rs

- **SqlParser-cs:** https://github.com/TylerBrinks/SqlParser-cs
  - É um port para .NET 8+. Mapeia enums para "abstract base records with enclosed sub-class implementations".
  - **Não registra qual versão do upstream acompanha.** É referência útil de API, sem mecanismo de sincronização (ver a análise técnica).
- **xsqlparser (Go):** https://github.com/akito0107/xsqlparser. "This repo is ported of sqlparser-rs in Go". Abandonado desde 2022.
- **Como aplicamos:** nenhum port anterior resolveu o acompanhamento do upstream. Esse mecanismo é parte central deste projeto (ADR-0004).

### P6. O próprio sqlparser-rs

**Fonte:** https://github.com/apache/datafusion-sqlparser-rs (`AGENTS.md`, `docs/fuzzing.md`, `changelog/`)

**O que diz:**
- O `AGENTS.md` do upstream pede "Make targeted code changes and refrain from refactoring". Os testes seguem o modelo de round-trip `verified_stmt` / `one_statement_parses_to`.
- Há fuzzing de round-trip (`fuzz_parse_roundtrip`) e diferencial contra PostgreSQL, SQLite e DuckDB, com um corpus `fuzz_seeds/` versionado.
- A feature `serde` serializa a AST em JSON.
- Há um changelog por release (`changelog/0.NN.0.md`).

**Como aplicamos:**
- Os changelogs servem de fila de sincronização.
- `serde` + `fuzz_seeds` formam a base do oráculo diferencial planejado (seção 4).

## 2. Tradução e migração de código com IA

### L1. Google: "How is Google using AI for internal code migrations?"

**Fonte:** Nikolov et al., 2025-01-12, https://arxiv.org/abs/2501.06972

**O que diz:**
- Combinar LLM com ferramentas determinísticas.
- Usar diff de AST para pegar edições indesejadas.
- Prompts pequenos, por subetapa.
- Separar arquivos interdependentes em prompts diferentes gera inconsistência.
- Toda mudança passa por build, testes e revisão humana.

**Como aplicamos:** uma unidade de port coesa por vez (um tipo + suas funções), com validação por build/teste.

### L2. Google: "Migrating Code At Scale With LLMs At Google"

**Fonte:** Ziftci et al., 2025-04-13, https://arxiv.org/abs/2504.09691

**O que diz:**
- Validações em cascata, da mais barata à mais cara: resposta válida, parse, build, teste. Falhou em qualquer etapa, a mudança é descartada.
- Até 3 tentativas por arquivo, porque LLMs não são determinísticos.
- Falhas típicas: **edições não pedidas** (reformatação, comentários extras).

**Como aplicamos:**
- Build com `TreatWarningsAsErrors` + testes em 3 runtimes.
- Edições fora do escopo da unidade são proibidas (skill `port-rust-to-csharp` §3).

### L3. Airbnb: "Accelerating Large-Scale Test Migration with LLMs"

**Fonte:** Charles Covey-Brandt, 2025, https://airbnb.tech/?p=366

**O que diz:**
- Cada arquivo passa por uma máquina de estados de etapas, cada uma com validação automática e nova tentativa alimentada pelos erros.
- **Escolher o contexto certo** (arquivos relacionados, exemplos irmãos) importou mais que a redação do prompt.
- O ciclo "sample, tune, sweep" levou o resultado de 75% a 97%.

**Como aplicamos:**
- Status por unidade em `progresso.md`.
- Exemplos canônicos na skill (`references/exemplos.md`).
- Quando uma tradução erra, **corrige-se a regra ou o exemplo** (o "prompt compartilhado"), não só aquele resultado.

### L4. "Lost in Translation: A Study of Bugs Introduced by LLMs while Translating Code"

**Fonte:** Pan et al., ICSE 2024, https://arxiv.org/abs/2308.03109

**O que diz:**
- Taxonomia de 15 categorias de bugs de tradução por LLM. Frequentes:
  - violar requisitos da linguagem-alvo;
  - parsing de entrada incorreto;
  - tipos de dados errados (largura de inteiros);
  - diferença de semântica de API;
  - lógica adicionada ou removida;
  - "duplicating source syntax".
- O reparo funciona melhor quando o prompt inclui o erro e o par entrada/saída esperada.

**Como aplicamos:** checklist de revisão ([`.claude/skills/port-rust-to-csharp/references/checklist-revisao.md`](../../.claude/skills/port-rust-to-csharp/references/checklist-revisao.md)).

### L5. AlphaTrans (FSE 2025)

**Fonte:** Ibrahimzada et al., https://arxiv.org/abs/2410.24117

**O que diz:**
- Primeiro um esqueleto com assinaturas, depois a tradução dos fragmentos "in the reverse call order" (folhas antes dos chamadores).
- Um mapeamento universal de tipos reutilizável.
- Código e testes traduzidos juntos.

**Como aplicamos:** Fase 2 na ordem tipos da AST → funções folha do parser → chamadores. Tabela de tipos em `convencoes.md` §5.

### L6. "Scalable, Validated Code Translation of Entire Projects using LLMs" (PLDI 2025)

**Fonte:** Zhang et al., https://arxiv.org/abs/2412.08035

**O que diz:**
- *Feature mapping*: regras de tradução predefinidas + verificação estática de que cada tradução as segue. Isso "allows developers to select their preferred mapping and ensures its consistent application".
- É a evidência mais direta contra "reescritas inconsistentes".

**Como aplicamos:**
- `convencoes.md` é o mapeamento normativo; §13 é a memória de decisões.
- **Planejado:** verificações automáticas (analisador ou script) para as regras mecânicas mais importantes, como proibir `ToString()` sintetizado em records portados ou exigir cabeçalho de origem.

### L7. FLOURINE: tradução real para Rust

**Fonte:** Eniser et al., https://arxiv.org/abs/2405.11514

**O que diz:**
- Fuzzing diferencial valida a tradução sem precisar de testes prévios.
- "The most effective feedback strategy is to re-generate a new translation from scratch".
- A taxa de sucesso cai por volta de ~100 linhas por unidade.

**Como aplicamos:**
- Unidades pequenas.
- Depois de duas tentativas de correção sem sucesso, **retraduzir do zero** a unidade em vez de remendar (skill `port-rust-to-csharp` §5).

### L8. Syzygy e DARPA TRACTOR (C → Rust)

**Fontes:**
- Syzygy: https://arxiv.org/abs/2412.14234
- TRACTOR: https://www.darpa.mil/research/programs/translating-all-c-to-rust

**O que diz:** traduzir código e testes juntos, "in dependency order", verificando equivalência contra o original.

**Como aplicamos:** testes portados junto com cada unidade (skill `port-rust-tests`).

## 3. Consistência de agentes entre sessões (engenharia de contexto)

### A1. Claude Code: boas práticas e memória

**Fontes:**
- https://code.claude.com/docs/en/best-practices
- https://code.claude.com/docs/en/memory

**O que diz:**
- Dar ao agente uma verificação executável (testes, build).
- `CLAUDE.md` curto: "under 200 lines". Regras contraditórias fazem o agente "pick one arbitrarily".
- Hooks são determinísticos, ao contrário das instruções do `CLAUDE.md`, que são "advisory".
- Usar subagentes para investigação e revisão com contexto limpo.

**Como aplicamos:**
- `CLAUDE.md` enxuto, que aponta para os documentos normativos.
- Procedimentos ficam em skills.
- **Planejado:** hook de build/teste quando o fluxo estabilizar (exige a aprovação do usuário para a configuração).

### A2. Anthropic: "Effective context engineering for AI agents"

**Fonte:** 2025-09-29, https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents

**O que diz:**
- "the smallest possible set of high-signal tokens".
- Contexto carregado sob demanda.
- **Poucos exemplos canônicos e diversos** em vez de regras exaustivas.
- Notas persistidas fora da janela de contexto.

**Como aplicamos:**
- `references/exemplos.md` (exemplos canônicos).
- `progresso.md` (notas persistentes e diário).
- Skills carregam detalhes só quando acionadas.

### A3. Anthropic: "Effective harnesses for long-running agents"

**Fonte:** 2025-11-26, https://www.anthropic.com/engineering/effective-harnesses-for-long-running-agents

**O que diz:**
- Arquivo de progresso + lista de funcionalidades em JSON, em que o agente só muda o status ("It is unacceptable to remove or edit tests").
- Uma funcionalidade por sessão.
- Ao iniciar: ler o progresso e rodar um *smoke test* antes de trabalho novo.

**Como aplicamos:**
- `progresso.md` com diário.
- Rotina de início de sessão (skill `port-rust-to-csharp` §1: rodar build + testes antes).
- Proibição de apagar ou alterar testes portados.
- **Planejado:** manifesto `port-status.json` por função na Fase 2, quando a granularidade passar a ser por função do parser.

### A4. Agent Skills

**Fontes:**
- https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills (2025-10-16)
- https://code.claude.com/docs/en/skills

**O que diz:**
- Divulgação progressiva: metadados, depois `SKILL.md`, depois arquivos ligados.
- Scripts embutidos são "consistent and repeatable".
- "Keep `SKILL.md` under 500 lines".

**Como aplicamos:**
- Skills `port-rust-to-csharp`, `port-rust-tests` e `sync-upstream`, cada uma com referências.
- Partes mecânicas viram scripts (`tools/codegen/`, `tools/upstream/`).

### A5. Anthropic: "Building a C compiler with a team of parallel Claudes"

**Fonte:** 2026-02-05, https://www.anthropic.com/engineering/building-c-compiler

**O que diz:**
- O GCC foi usado como oráculo ("known-good compiler oracle").
- "the task verifier is nearly perfect, otherwise Claude will solve the wrong problem".
- Saída de teste concisa; READMEs e arquivos de progresso mantidos pelos agentes.

**Como aplicamos:** o sqlparser-rs será o oráculo (seção 4). Investir no verificador antes de acelerar o port.

## 4. Testes diferenciais e oráculos

### D1. McKeeman, "Differential Testing for Software" (1998)

**Fonte:** Digital Technical Journal 10(1), https://web.archive.org/web/2006id_/http://www.hpl.hp.com/hpjournal/dtj/vol10num1/vol10num1art9.pdf

**O que diz:**
- "Differential testing requires that two or more comparable systems be available to the tester."
- Toda diferença é investigada; casos que falham são reduzidos automaticamente.

**Como aplicamos (planejado):**
- O sqlparser-rs (com `serde`) gera a AST em JSON para cada SQL do corpus.
- O C# compara.
- Casos divergentes são reduzidos antes da análise.

### D2. Verify (snapshot testing para .NET)

**Fonte:** https://github.com/VerifyTests/Verify

**O que diz:** arquivos `*.verified.*` versionados. Suporta net462–net48x e net6–net10.

**Como aplicamos (planejado):** as saídas do oráculo Rust ficam versionadas como snapshots aprovados, e a divergência aparece como diff.

## 5. Referências oficiais de C# para o mapeamento

- **Versão da linguagem:** https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning
  - O padrão para `netstandard2.0` é C# 7.3.
  - "Using a C# language version newer than the version associated with your target TFM is unsupported".
  - **Decisão consciente registrada no [ADR-0006](../adr/0006-versao-da-linguagem-csharp.md).**
- **PolySharp:** https://github.com/Sergio0694/PolySharp. Polyfills de atributos (`IsExternalInit` etc.). Não resolve recursos que exigem suporte do runtime.
- **Records:** https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record
  - Igualdade sintetizada a partir dos membros.
  - **Inferência:** coleções comparam por referência. Por isso a AST precisa de coleção com igualdade estrutural (`Sequence<T>`, Fase 2).
- **Union types (C# 15):** https://devblogs.microsoft.com/dotnet/csharp-15-union-types/. Exige runtime novo, então **não serve para `netstandard2.0`**. Mantemos `abstract record` + `sealed record` aninhados.
- **Nullable reference types:** https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references. "entirely a compile-time feature". Por isso `Option<T>` mapeia para `T?` em tipos de referência e para `Nullable<T>` em tipos-valor.
- **Exceções** (Framework Design Guidelines):
  - https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exception-throwing ("DO report execution failures by throwing exceptions");
  - https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exceptions-and-performance (padrão Try-Parse).
  - **Como aplicamos:** `Result`/`?` → exceções no Core (o fluxo fica linha a linha igual ao Rust). `TryParse` fica na camada pública.

## 6. Práticas adotadas (resumo)

| # | Prática | Fontes | Onde está no projeto | Status |
|---|---|---|---|---|
| 1 | Mapeamento normativo Rust→C# + memória de decisões | L6, L5, L4 | `convencoes.md` (§13), `nomes.md` | ✅ |
| 2 | Estrutura 1:1 (arquivo, ordem, nomes mecânicos) | P1, P3 | `convencoes.md` §1–§4 | ✅ |
| 3 | Desvios marcados no código (`PORT-DEVIATION`) e listados | P2, P3 | `convencoes.md` §12, `divergencias.md` | ✅ |
| 4 | Esqueleto primeiro, folhas antes de chamadores, unidades pequenas | L5, L7, L8 | Skill `port-rust-to-csharp` §2 | ✅ |
| 5 | Exemplos canônicos dentro da skill | A2, A4, L3 | `references/exemplos.md` | ✅ |
| 6 | Partes mecânicas geradas por script | L1, A4 | `tools/codegen/Generate-Keywords.ps1` | ✅ (keywords) |
| 7 | Revisão linha a linha em contexto limpo | A1, P3, L1 | Skill §5 + `checklist-revisao.md` | ✅ |
| 8 | Proibir edições fora do escopo | L2, P6 | Skill §3 | ✅ |
| 9 | `CLAUDE.md` curto + skills para procedimentos | A1, A4 | `CLAUDE.md`, `.claude/skills/` | ✅ |
| 10 | Estado persistente + diário + rotina de início de sessão | A3, A5 | `progresso.md`, skill §1 | ✅ |
| 11 | Manifesto JSON de status por função | A3, L3 | — | Planejado (Fase 2) |
| 12 | Hooks/CI para build e testes | A1, A5 | — | Planejado (precisa de aprovação) |
| 13 | Testes portados junto com o código; testes nunca editados para passar | P1, L8, A3 | Skill `port-rust-tests` | ✅ |
| 14 | Oráculo Rust (AST JSON) + snapshots | D1, D2, P6, A5 | — | Planejado (Fase 2; exige toolchain Rust local) |
| 15 | Divergências como baseline que só diminui | P2 | `divergencias.md` + `Skip = "DIV-NNN"` | ✅ |
| 16 | Fuzzing diferencial com redução de casos | D1, P6, L7 | — | Planejado (Fase 5) |
| 17 | Retentativas limitadas; retraduzir em vez de remendar | L2, L3, L7 | Skill §5 | ✅ |
| 18 | Checklist de bugs típicos de LLM na revisão | L4 | `checklist-revisao.md` | ✅ |
| 19 | Upstream fixado + commit portado registrado | P2, P4 | `UPSTREAM.json`, `tools/upstream/` | ✅ |
| 20 | Sincronização release a release, PR a PR, citando a origem | P4, P6 | Skill `sync-upstream` | ✅ |
| 21 | Cabeçalho com a versão do upstream em cada arquivo | P3 | `convencoes.md` §3 | ✅ |
| 22 | Não idiomatizar o Core; adaptações só na camada pública | P1, P3, P6 | ADR-0002 | ✅ |

## 7. Não verificado / ressalvas

- O uso efetivo do "ts-to-go" (https://github.com/jakebailey/ts-to-go) no port do TypeScript só é afirmado em fontes secundárias.
- O processo de port do NHibernate e o uso da ferramenta Sharpen pelo iText não foram confirmados.
- Amazon Q Code Transformation não foi analisado.
- A data exata do post do Airbnb não aparece na página (~março de 2025).
- A disponibilidade geral de union types com o .NET 11 não foi confirmada.
- As datas de release do sqlparser-rs citadas são as dos commits das tags.
