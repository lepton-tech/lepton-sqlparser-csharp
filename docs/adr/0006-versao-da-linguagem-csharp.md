# ADR-0006: Versão da linguagem C# sobre `netstandard2.0`

- **Status:** Aceita, com risco conhecido
- **Data:** 2026-10-14

## Contexto

- A versão padrão de C# para `netstandard2.0` é a **7.3**.
- A documentação da Microsoft diz: "Using a C# language version newer than the version associated with your target TFM is unsupported" (https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-versioning).
- Ainda assim, records, `init`, pattern matching e `switch` expressions são essenciais para traduzir os enums e os `match` do Rust de forma legível e fiel.
- Esses recursos dependem só de atributos, que o PolySharp gera na compilação. Não dependem de suporte do runtime.

## Decisão

- `LangVersion` **14.0**, fixado em `Directory.Build.props`. Nunca `latest`, que mudaria conforme o SDK instalado.
- SDK 10 fixado em `global.json`.
- **PolySharp** (`PrivateAssets=all`) para os atributos de polyfill. Não é dependência de runtime.
- **Proibidos**, porque exigem suporte do runtime e quebrariam no .NET Framework:
  - default interface members;
  - membros `static abstract`/`virtual` em interfaces;
  - `ref` fields;
  - `InlineArray`;
  - tipos `union` (C# 15).
- **Evitados na API pública:**
  - `required`, porque consumidores em C# 7.3 não conseguem construir o tipo;
  - dependência de `init` para construir objetos.

  Todo tipo público oferece construtor posicional.
- O projeto de testes **não** referencia o PolySharp: usa os polyfills internos do Core via `InternalsVisibleTo`. Uma segunda cópia causaria o erro CS0433.

## Consequências

- **Risco aceito:** combinação "não suportada" oficialmente, mas amplamente usada pela comunidade. É mitigada pela execução dos testes em net48 a cada build de teste.
- Consumidores com C# 7.3 (padrão em projetos .NET Framework antigos) usam a biblioteca normalmente, mas não conseguem usar `with`/`init` nos records. A recomendação para eles é `LangVersion` ≥ 9.
