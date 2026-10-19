---
name: port-rust-to-csharp
description: Fluxo obrigatório para portar (traduzir/transcrever) código do sqlparser-rs (Rust) para o Lepton.SqlParser.Core em C#. Use SEMPRE que a tarefa envolver portar, traduzir, transcrever, reescrever ou implementar em C# qualquer parte do sqlparser-rs (tokenizer, keywords, dialects, parser, AST, Display/ToString, spans, visitor), corrigir uma divergência em relação ao Rust, ou continuar o port de onde parou, mesmo que o pedido diga só "continua o port", "porta o arquivo X" ou "implementa a função parse_xxx".
---

# Portar Rust → C# (Lepton.SqlParser.Core)

Esta skill garante que cada sessão porte código **do mesmo jeito** que as anteriores. A consistência vem de três fontes, nesta ordem:
1. as regras escritas;
2. a memória de tradução (decisões já tomadas);
3. o código já portado.

Nunca dependa de "lembrar" como foi feito antes: leia.

## 1. Carregar o contexto (sempre, antes de escrever código)

Leia, nesta ordem:

1. `docs/port/progresso.md`: o que já foi feito, a fase atual e o último diário de sessão.
2. `docs/port/convencoes.md`: regras normativas. Atenção às §4–§8 e à §13 (casos decididos).
3. `docs/port/nomes.md` e `docs/port/divergencias.md`.
4. `UPSTREAM.json`: qual versão do Rust portar. O código Rust fica em `upstream/sqlparser-rs/`. Se a pasta não existir, rode `pwsh tools/upstream/Get-Upstream.ps1`.

Depois rode o *smoke test*: `dotnet test Lepton.SqlParser.sln -nologo -v q`. **Comece só com a árvore verde.** Se algo já estiver quebrado, conserte ou registre isso antes de trabalho novo, para não misturar a falha herdada com a sua.

Antes de traduzir um construto, procure no código C# já portado (`src/Lepton.SqlParser.Core/`) como ele foi traduzido antes. Exemplos: um `match` sobre `Token`, um `Option<Box<Expr>>`, um `impl Display`. **Copie o padrão existente.** Se o padrão existente contradiz `convencoes.md`, a convenção vence: corrija o código antigo na mesma mudança, ou registre a pendência no diário.

## 2. Delimitar a unidade de trabalho

Porte em unidades pequenas e verificáveis: um tipo, um grupo de funções relacionadas ou uma seção de arquivo. Para cada unidade:
- identifique no Rust o trecho exato (arquivo + itens) e os **testes Rust** que o exercitam: `#[cfg(test)] mod tests` do próprio arquivo e `tests/sqlparser_*.rs`;
- identifique as dependências (tipos ainda não portados). Se faltar dependência, porte-a antes ou deixe a unidade para depois. **Não crie stubs "provisórios" com comportamento inventado.**

## 3. Traduzir

Siga `docs/port/convencoes.md`. O essencial:

- **Destino:** arquivo/namespace da §2. **Cabeçalho ASF + linha de origem** (§3) em todo arquivo portado.
- **Nomes:** regra mecânica (§4). Snake → Pascal. Tipos, variantes e keywords idênticos ao Rust. No Core **não** há nomes "amigáveis".
- **Enums com dados:** `abstract record` + `sealed record` aninhados (§6). Um `match` vira **um** `switch` no mesmo lugar.
- **`impl Display`:** `IDisplay.Fmt(Formatter f)` + `ToString()` delegando, e `sealed override` na base de hierarquias (§7).
- **Erros:** exceções com **mensagem idêntica** ao Rust (§8).
- **Ordem dos membros:** a mesma do arquivo Rust.
- **Fidelidade:** não melhore, não reorganize, não corrija bugs do Rust em silêncio. Divergência necessária → `docs/port/divergencias.md` + teste com `Skip = "DIV-NNN"`.
- **`netstandard2.0`:** respeite a §11. Nada de default interface members, `Span<T>` nem APIs .NET Core-only.

**Caso não coberto pelas convenções:** escolha a forma mais próxima do espírito das regras e **registre** em `convencoes.md` §13 (ou em `nomes.md`, se for nome) **na mesma mudança**. Uma decisão não registrada é a principal causa de traduções inconsistentes entre sessões.

Exemplos canônicos de tradução: [references/exemplos.md](references/exemplos.md).

## 4. Testes

Porte os testes Rust correspondentes junto com o código, usando a skill `port-rust-tests`. Uma unidade só está pronta com seus testes portados passando.

## 5. Verificar

Rode na raiz do repositório:

```bash
dotnet build Lepton.SqlParser.sln -nologo -v q
dotnet test Lepton.SqlParser.sln -nologo -v q
```

- **Armadilha das ferramentas de edição:** Write/Edit podem converter sequências de escape (a barra invertida seguida de `u` e 4 dígitos hex, como no escape de U+FFFF) no **caractere literal**. Isso já aconteceu duas vezes neste projeto: um teste de unescape continuou verde testando outra entrada. Depois de escrever literais de string/char com escapes, rode `pwsh tools/check/Find-SuspiciousChars.ps1` e confira cada linha listada. Para restaurar, gere a barra por script (por exemplo, `[char]92` no PowerShell) em vez de digitá-la.
- O build tem `TreatWarningsAsErrors`: warning é erro. Corrija; não suprima sem registrar o motivo.
- Os testes rodam em **net48, net8.0 e net10.0**. As três precisam passar, porque o .NET Framework pega diferenças de BCL que o .NET moderno esconde.
- Um teste portado que falha **não** pode ser apagado nem "ajustado" para passar. Ou o código está errado (corrija), ou é divergência registrada (`Skip = "DIV-NNN: ..."`).
- **Limite de tentativas.** Se duas correções seguidas da mesma unidade não resolverem, pare de remendar. Releia o Rust e **retraduza a unidade do zero**: estudos de tradução por LLM mostram que regenerar funciona melhor que remendar. Se ainda falhar, registre no diário e siga para outra unidade.
- **Revisão linha a linha.** Ao fim de cada unidade relevante (tipo grande, função de parse, arquivo de dialeto), peça a um **subagente com contexto limpo** que compare o C# com o Rust usando [references/checklist-revisao.md](references/checklist-revisao.md). Passe a ele só o trecho Rust, o arquivo C# e o checklist. Corrija o que ele apontar.
- **Desvios no código.** Toda divergência deliberada leva um comentário buscável na linha: `// PORT-DEVIATION DIV-NNN: <motivo>`. Pendências levam `// PORT-TODO: <o quê>`. Pontos em que só o mecanismo de falha muda, por desempenho, levam `// PORT-PERF (caso N): <motivo>`; ao portar uma mudança do Rust num deles, siga a receita do caso citado (hoje, o 140: variantes `...NoMatch`). Ver `convencoes.md` §12.

## 6. Fechar a sessão (handoff)

Antes de encerrar, atualize:

1. `docs/port/progresso.md`: status do arquivo Rust, contagem de testes e **entrada no diário** (feito, decisões, próximo passo).
2. `docs/port/nomes.md`, `docs/port/divergencias.md` e `convencoes.md` §13, se algo novo foi decidido.
3. `THIRD-PARTY-NOTICES.md`, se algum trecho do SqlParser-cs foi copiado ou adaptado.

**Não faça operações Git** (commit, branch, push) sem autorização explícita do usuário.
