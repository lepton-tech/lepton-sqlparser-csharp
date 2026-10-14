# ADR-0003: Licenciamento e atribuição

- **Status:** Aceita, com a licença do projeto **pendente**
- **Data:** 2026-10-14
- *Não é parecer jurídico. Para distribuição comercial, valide com o jurídico.*

## Contexto

- O `SqlParser.Core` é uma **tradução** do sqlparser-rs, licenciado sob Apache-2.0. Pela própria licença (§1, "Derivative Works"), é obra derivada.
- O SqlParser-cs (MIT) pode fornecer trechos à camada pública.
- O projeto ainda não decidiu se será aberto ou fechado, nem onde ficará hospedado (Bitbucket em avaliação).

## Decisão

1. **Obrigações Apache-2.0** (§4), que valem para código aberto **ou fechado**:
   - incluir o texto da licença: `licenses/sqlparser-rs-LICENSE.txt`;
   - manter os avisos de atribuição: cabeçalho ASF verbatim em todo arquivo portado (`docs/port/convencoes.md` §3);
   - indicar de forma visível que os arquivos foram modificados: linha "Portado para C# a partir de sqlparser-rs vX, arquivo Y. Modificado: tradução de Rust para C#";
   - reproduzir o NOTICE do upstream no nosso `NOTICE`.
2. **MIT (SqlParser-cs):** trechos copiados levam o aviso de copyright e a permissão. Ficam listados em `THIRD-PARTY-NOTICES.md`.
3. **Marcas** (Apache §6): o nome do produto e do pacote não deve usar "Apache" nem "DataFusion", nem sugerir endosso. O ID `SqlParserCS` no NuGet já pertence a outro projeto.
4. **Licença do nosso projeto:** **pendente**, a ser decidida com o dono da empresa. A Apache-2.0 permite distribuição fechada, desde que as obrigações acima sejam cumpridas. Se o projeto for aberto, a recomendação é Apache-2.0, a mesma do upstream.

## Consequências

- Os arquivos `NOTICE`, `THIRD-PARTY-NOTICES.md` e `licenses/` precisam acompanhar qualquer distribuição (pacote NuGet, instalador).
- O corpus de testes extraído dos testes do Rust também é derivado e segue as mesmas regras.
