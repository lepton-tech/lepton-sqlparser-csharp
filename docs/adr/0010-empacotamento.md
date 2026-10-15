# ADR-0010: Empacotamento e versão

- **Status:** Aceita
- **Data:** 2026-10-15
- **Decidido por:** responsável pelo projeto

## Contexto

O objetivo é deixar a biblioteca "publicável" (pacote pronto para ser distribuído). O destino final ainda não foi definido: feed interno, Bitbucket ou nuget.org. O nome `SqlParser` sozinho colide com o SqlParser-cs no NuGet, cujo pacote é `SqlParserCS` e o namespace é `SqlParser`.

## Decisão

| Tema | Decisão |
|---|---|
| Destino desta versão | **Pacote `.nupkg` gerado localmente.** Nada é publicado fora da máquina; a escolha do feed fica para depois |
| Estrutura | **Um pacote só**, com as duas DLLs (`SqlParser.Core.dll`, o port fiel, e `SqlParser.dll`, a API pública), sempre na mesma versão |
| ID do pacote | **`LeptonTI.SqlParser`**. Os namespaces do código continuam `SqlParser` e `SqlParser.Core` |
| Versão | **Alinhada ao sqlparser-rs:** `0.63.x`. A primeira é `0.63.1-preview.1`, porque o port já inclui o `main` do Rust depois da v0.63.0 (commit `dacdbc9`, ver `UPSTREAM.json`). Quando o port chegar a uma tag nova do Rust (ex.: v0.64.0), a versão passa a `0.64.x` |
| Licença | Apache-2.0 (obra derivada do sqlparser-rs), com `NOTICE` e `THIRD-PARTY-NOTICES.md` dentro do pacote ([ADR-0003](0003-licenciamento-e-atribuicao.md)) |

## Consequências

- Quem instala o pacote recebe o Core e a API juntos. Quem só quer a AST fiel ao Rust usa os namespaces `SqlParser.Core*` do mesmo pacote.
- **Assinatura forte (strong naming):** ainda não decidida. Aplicações .NET Framework com assinatura forte exigem dependências também assinadas. Decidir antes da primeira publicação num feed.
- O número da versão comunica qual Rust está por trás. Mudanças só nossas (API pública, correções) avançam o último número ou o sufixo de pré-lançamento.
