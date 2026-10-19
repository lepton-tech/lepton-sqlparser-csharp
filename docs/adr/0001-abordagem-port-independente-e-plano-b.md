# ADR-0001: Port independente do Rust, com Plano B documentado

- **Status:** Aceita
- **Data:** 2026-10-14
- **Contexto completo:** [`../analise-tecnica-e-plano.md`](../analise-tecnica-e-plano.md)

## Contexto

Precisamos de um parser SQL em C# (`netstandard2.0`, sem dependências nativas) com paridade funcional com o `apache/datafusion-sqlparser-rs`, capaz de acompanhar a evolução do projeto Rust. Três caminhos foram avaliados:

| Opção | Descrição | Resultado da avaliação |
|---|---|---|
| **A: fork do SqlParser-cs** | Adaptar o port C# existente para `netstandard2.0` e evoluir | Adaptação barata (patch de ~54 linhas, já validado). Porém só 54% de paridade de comportamento com o Rust v0.63, estrutura divergente e manutenção parada |
| **B/C: port independente** | Reescrever em C# a partir do Rust, com estrutura 1:1 e reaproveitamento seletivo do SqlParser-cs | Paridade por construção; sincronização barata com o upstream |
| **D: wrapper nativo** | Compilar o Rust como DLL nativa e chamar via P/Invoke | **Rejeitada:** viola o requisito "sem DLLs nativas em runtime"; exige DLL por plataforma e serialização da AST na fronteira |

## Decisão

Seguir com o **port independente** (opção C): `LeptonTech.SqlParser.Core` fiel ao Rust, mais uma camada pública própria ([ADR-0002](0002-arquitetura-em-camadas-e-api.md)).

## Plano B: quando e como mudar de rota

Se o port independente se mostrar trabalhoso demais, a alternativa é a **opção A**: o fork do SqlParser-cs adaptado para `netstandard2.0`. O trabalho de adaptação já foi feito e validado, e está guardado em [`../plano-b/`](../plano-b/README.md).

**Gatilhos objetivos.** Qualquer um deles leva a reavaliar a rota, com decisão do responsável pelo projeto:

1. **Velocidade:** ao fim da Fase 2, a velocidade medida (linhas Rust portadas com testes verdes por semana) projeta a paridade com a v0.63 para **mais que o dobro** da estimativa do plano (mais de ~12 meses).
2. **Qualidade:** ao fim da Fase 2, menos de **80%** dos testes de SELECT/expressões de `sqlparser_common.rs` portados passam.
3. **Prazo de negócio:** surge necessidade de um parser `netstandard2.0` utilizável antes do fim da Fase 2. Nesse caso o Plano B entra **como solução provisória, em paralelo**, sem abandonar o port.

**O que se perde ao acionar o Plano B:**
- paridade de comportamento (54% medidos no corpus do Rust);
- spans;
- o visitor mutável;
- a facilidade de acompanhar o upstream.

Essas perdas devem constar da decisão.

## Consequências

- Mais trabalho inicial (estimativa de 5–7 meses até a paridade completa com a v0.63) em troca de paridade verificável e sincronização sustentável.
- O progresso precisa ser medido (`docs/port/progresso.md`) para que os gatilhos acima possam ser avaliados com dados.
