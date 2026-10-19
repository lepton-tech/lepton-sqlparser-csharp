# ADR-0002: Arquitetura em duas camadas (Core fiel + API pública)

- **Status:** Aceita
- **Data:** 2026-10-14

## Contexto

- O responsável pelo projeto gostou da API do SqlParser-cs e quer reaproveitá-la.
- Quer também um lugar para "a nossa tradução" e para customizações futuras, separado da parte que é reescrita do Rust.
- Ao mesmo tempo, a fidelidade ao Rust exige nomes e estrutura idênticos aos do upstream, porque é isso que torna a sincronização barata (ver `docs/port/referencias-e-boas-praticas.md`, P1 e P3).

## Decisão

Dois projetos, com dependência em um só sentido:

```
Lepton.SqlParser (API pública)  ──depende de──►  Lepton.SqlParser.Core (port fiel)
```

| | `Lepton.SqlParser.Core` | `Lepton.SqlParser` |
|---|---|---|
| **Conteúdo** | Tradução 1:1 do sqlparser-rs: tokenizer, keywords, dialetos, parser, AST, `Display`, spans, visitor | Código nosso: ponto de entrada (`SqlQueryParser`), `ToSql()`/`ToSql(pretty)`, `TryParse`, visitors no estilo SqlParser-cs, utilitários de análise, customizações |
| **Nomes** | Os do Rust, por regra mecânica (`Expr`, `SetExpr`, `Statement.Query`). Sem exceções "de gosto" | Livres, no estilo do SqlParser-cs (`SqlQueryParser`, `ToSql`…) |
| **Regras** | `docs/port/convencoes.md` (normativo) | Boas práticas .NET (Framework Design Guidelines) |
| **Cabeçalho ASF** | Sim (código derivado) | Não (código próprio), salvo se algo for derivado |
| **Muda quando** | O upstream muda | Nós decidimos |
| **Testes** | `Lepton.SqlParser.Core.Tests` (portados do Rust) | `Lepton.SqlParser.Tests` (nossos) |
| **Namespaces** | `Lepton.SqlParser.Core`, `Lepton.SqlParser.Core.Ast`, `Lepton.SqlParser.Core.Dialects` | `Lepton.SqlParser`, e outros a definir |

**AST exposta pela API pública:**
- Inicialmente, a camada pública **reexpõe os tipos de AST do Core**, sem duplicar a AST.
- Uma AST pública própria, com nomes do SqlParser-cs e conversão gerada automaticamente, fica como **opção futura**, adotada se consumidores precisarem de uma API de AST imune às mudanças do upstream.
- Custo dessa opção: um segundo conjunto de ~500 tipos mais o mapeamento, mitigável por geração de código. Ela não exige mudar o Core.

## O que se reaproveita do SqlParser-cs na camada pública

- O formato de uso: `new SqlQueryParser().Parse(sql, dialect)`, `statement.ToSql()`, `Visitor` com `PreVisit*`/`PostVisit*` e `ControlFlow`.
- O padrão de AST com `abstract record` + records aninhados, também adotado no Core (`convencoes.md` §6).
- Trechos de código copiados ou adaptados são listados em `THIRD-PARTY-NOTICES.md` (MIT).

## Consequências

- O Core segue regras mecânicas sem exceções. Isso reduz decisões por sessão e, com elas, o risco de traduções inconsistentes.
- Mudanças de API vindas do upstream (frequentes) chegam ao Core. A camada pública é onde se pode amortecê-las.
- **Empacotamento** (um pacote com as duas DLLs ou dois pacotes NuGet): decidido no [ADR-0010](0010-empacotamento.md): um pacote só, com as duas DLLs. O nome (`Lepton.SqlParser`, também dos assemblies e namespaces) está no [ADR-0011](0011-renome-lepton-sqlparser.md); até a `0.63.1-preview.2`, o pacote era `LeptonTI.SqlParser` e os namespaces, `SqlParser` e `SqlParser.Core`.
