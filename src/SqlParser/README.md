# SqlParser: API pública

Camada **própria** do produto, construída sobre o `SqlParser.Core` (port fiel do sqlparser-rs). Ver [ADR-0002](../../docs/adr/0002-arquitetura-em-camadas-e-api.md).

O que entra aqui (no estilo do SqlParser-cs):
- ponto de entrada `SqlQueryParser` (`Parse(sql, dialect, options)`, `TryParse`);
- geração de SQL: `ToSql()` e `ToSql(pretty: true)`;
- visitors com `PreVisit*`/`PostVisit*` e `ControlFlow`;
- utilitários de análise (tabelas/colunas referenciadas etc.);
- customizações e extensões nossas.

O que **não** entra: tradução do Rust. Isso fica no `SqlParser.Core`, que nunca referencia este projeto.

**Status:** vazio até o parser do Core existir (Fase 2).
