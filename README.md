# SqlParser (port C# do sqlparser-rs)

Parser SQL para .NET (`netstandard2.0`, compatível com .NET Framework 4.8 e .NET moderno), portado do projeto Rust [apache/datafusion-sqlparser-rs](https://github.com/apache/datafusion-sqlparser-rs). Mantém paridade funcional com o original e acompanha sua evolução. Não depende de Rust nem de DLLs nativas em runtime.

> **Status:** em desenvolvimento, Fase 1 (léxico e infraestrutura). Repositório local. Ver [`docs/port/progresso.md`](docs/port/progresso.md).

## Estrutura

```
src/
  SqlParser.Core/      Port fiel (1:1) do sqlparser-rs: tokenizer, keywords, dialetos, parser, AST
  SqlParser/           API pública (código próprio): ponto de entrada, ToSql, visitors, customizações
tests/
  SqlParser.Core.Tests/  Testes portados do Rust (mesmos nomes e casos)
  SqlParser.Tests/       Testes da API pública
tools/
  upstream/            Baixa o código de referência (Rust e SqlParser-cs) para upstream/
  codegen/             Geradores (ex.: keywords a partir do keywords.rs)
docs/                  Análise, ADRs, convenções de port, progresso, Plano B
.claude/skills/        Skills do Claude Code para o fluxo de port
```

Versão do Rust portada: [`UPSTREAM.json`](UPSTREAM.json) (baseline `v0.63.0`).

## Como compilar e testar

Requisitos: .NET SDK 10 (ver `global.json`), PowerShell 7 e Git (para os scripts de `tools/`).

```bash
pwsh tools/upstream/Get-Upstream.ps1      # opcional: baixa o código Rust de referência
dotnet build SqlParser.sln
dotnet test SqlParser.sln                 # roda em net48, net8.0 e net10.0
```

## Documentação

- [Análise técnica e plano](docs/analise-tecnica-e-plano.md)
- [Decisões de arquitetura (ADRs)](docs/adr/)
- [Convenções de tradução Rust → C#](docs/port/convencoes.md)
- [Referências e boas práticas](docs/port/referencias-e-boas-praticas.md)
- [Plano B](docs/plano-b/README.md)

## Licença e atribuição

O `SqlParser.Core` contém código traduzido do sqlparser-rs (Apache License 2.0, The Apache Software Foundation). Ver [`NOTICE`](NOTICE), [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) e [`licenses/`](licenses/). A licença do projeto em si ainda será definida ([ADR-0003](docs/adr/0003-licenciamento-e-atribuicao.md)).
