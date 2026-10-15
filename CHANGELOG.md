# Changelog

Mudanças relevantes do pacote `LeptonTI.SqlParser`.

O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/). A versão acompanha o sqlparser-rs ([ADR-0010](docs/adr/0010-empacotamento.md)): `0.63.x` corresponde ao Rust `v0.63`, e mudanças só nossas avançam o último número ou o sufixo de pré-lançamento.

## [Não lançado]

## [0.63.1-preview.1] - 2026-10-15

Primeira versão empacotada. Pré-lançamento, gerada só como pacote local (nada publicado).

### Adicionado

- **`SqlParser.Core`, port fiel do sqlparser-rs** `v0.63.0` mais o `main` do Rust até o commit `dacdbc9` (Rodada 1 de sincronização: 63 PRs, 41 portados e 22 não aplicáveis, como CI e Cargo):
  - tokenizer, keywords (geradas do `keywords.rs`), os 16 dialetos com todos os overrides, parser, AST, `Display` (`ToString()` e o formato alternativo `{:#}`) e spans (`ISpanned`);
  - visitor (`Visit`/`VisitMut`) gerado a partir do derive do Rust, com a mesma ordem de travessia ([ADR-0007](docs/adr/0007-visitor.md));
  - testes do Rust portados com os mesmos nomes e casos: 1.701 passando e 10 ignorados (7 com `#[ignore]` no próprio Rust e 3 por divergência registrada), em net48, net8.0 e net10.0.
- **Proteção contra estouro de pilha** (`StackGuard`), equivalente ao `#[recursive::recursive]` do Rust, no parser, no `Display`, nos spans e no visitor.
- **Maiúsculas e minúsculas como no Rust** (`RustString`, `RustChar`): mapeamento completo do Unicode 17.0.0, com tabelas geradas do UCD e a regra do sigma final; o resultado é o mesmo nos três runtimes (DIV-001 resolvida).
- **API pública (`SqlParser`), fase 1** ([ADR-0008](docs/adr/0008-api-publica-fase-1.md)): `SqlQueryParser` (`Parse` com dialeto por instância ou por nome, `TryParse`, `ParseWithComments`, limite de recursão), `SqlParseException` com `Line` e `Column`, `ToSql()` e `ToSql(pretty: true)`.
- **API pública, fase 2** ([ADR-0009](docs/adr/0009-api-publica-fase-2.md)): `SqlVisitor` e `ControlFlow` no estilo do SqlParser-cs, `SqlRewriter`, atalhos com lambdas (`VisitRelations`, `VisitExpressions`, `VisitStatements`, `RewriteRelations`, `RewriteExpressions`, `RewriteStatements`) e análise sintática (`GetReferencedTables`, `GetReferencedColumns`, `GetReferencedFunctions`, `GetStatementKind`). 220 testes próprios.
- **Pacote** `LeptonTI.SqlParser` (`netstandard2.0`, sem dependências): as duas DLLs com documentação XML, símbolos (`.snupkg`), README, `NOTICE`, `THIRD-PARTY-NOTICES.md` e `licenses/`. Scripts `tools/pack/Pack.ps1` (Release, testes e pacote) e `tools/pack/Test-Package.ps1` (consumidores net48 e net8.0).

### Limitações conhecidas

- Divergências aceitas em relação ao Rust, todas de impacto baixo ou nenhum: DIV-002 a DIV-009 ([`docs/port/divergencias.md`](docs/port/divergencias.md)). Sem as features `bigdecimal` e `serde` do Rust (DIV-006).
- As DLLs não têm assinatura forte (strong naming), e os PDBs ainda não têm SourceLink (o repositório não tem remoto).
- O Core ainda segue a `0.x` do Rust: versões novas podem mudar tipos da AST e renumerar o enum `Keyword` (quem compila contra o pacote precisa recompilar).
