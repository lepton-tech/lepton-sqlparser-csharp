# Avisos de terceiros

## sqlparser-rs (Apache-2.0)

- Origem: https://github.com/apache/datafusion-sqlparser-rs
- Uso: código-fonte traduzido de Rust para C# (lexer, parser, AST, dialetos) e casos de teste derivados dos testes do projeto.
- Licença: Apache License 2.0, em [`licenses/sqlparser-rs-LICENSE.txt`](licenses/sqlparser-rs-LICENSE.txt). Aviso de atribuição em [`NOTICE`](NOTICE).

## SqlParser-cs (MIT)

- Origem: https://github.com/TylerBrinks/SqlParser-cs
- Uso: referência de desenho da API pública (padrão de AST com records aninhados, `SqlQueryParser`, `ToSql`, `Visitor`). Trechos de código copiados ou adaptados devem ser listados abaixo, com o arquivo de destino.
- Licença: MIT, em [`licenses/SqlParser-cs-LICENSE.txt`](licenses/SqlParser-cs-LICENSE.txt).

```
MIT License

Copyright (c) 2023 Tyler Brinks

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

### Trechos copiados ou adaptados do SqlParser-cs

| Arquivo de destino | Origem no SqlParser-cs | Observação |
|---|---|---|
| _(nenhum até o momento)_ | | |

Os arquivos de `src/LeptonTech.SqlParser/` (API pública, fase 1: `SqlQueryParser.cs`, `SqlParseException.cs`, `SqlParseResult.cs`, `SqlWritingExtensions.cs`) **imitam nomes e formato de uso** do SqlParser-cs (`SqlQueryParser`, `Parse(sql, dialect, options)`, `ToSql`, `SqlWritingExtensions`, `Line`/`Column` na exceção) como referência de desenho, **sem cópia de código** ([ADR-0008](docs/adr/0008-api-publica-fase-1.md)).

Na fase 2, `ControlFlow.cs`, `SqlVisitor.cs` e `SqlVisitorExtensions.cs` **imitam nomes e formato de uso** do visitor do SqlParser-cs (o enum `ControlFlow` com `Continue` e `Break`, os ganchos `PreVisit*`/`PostVisit*`, o nome `PreVisitExpression` e a chamada `node.Visit(visitor)`), também **sem cópia de código**: a travessia é a do visitor do Core, gerado a partir do sqlparser-rs ([ADR-0009](docs/adr/0009-api-publica-fase-2.md)). `SqlRewriter.cs`, `SqlAnalysisExtensions.cs` e `SqlStatementKind.cs` não têm equivalente no SqlParser-cs.

## Unicode Character Database (Unicode License v3)

- Origem: https://www.unicode.org/Public/17.0.0/ucd/ (`UnicodeData.txt`, `SpecialCasing.txt`, `DerivedCoreProperties.txt`)
- Uso: tabelas de maiúsculas e minúsculas e das propriedades `Lowercase`, `Uppercase`, `Lt` e `Case_Ignorable`, geradas a partir desses arquivos por `tools/codegen/Generate-CaseMapping.ps1` em `src/LeptonTech.SqlParser.Core/Support/RustCaseMapping.g.cs` (DIV-001; `docs/port/onda-d/D6.md`). Os arquivos do UCD não ficam no repositório.
- Licença: Unicode License v3, em [`licenses/Unicode-LICENSE.txt`](licenses/Unicode-LICENSE.txt) (Copyright (c) 1991-2026 Unicode, Inc.).

## Biblioteca padrão do Rust (MIT OR Apache-2.0)

- Origem: https://github.com/rust-lang/rust (`library/alloc/src/str.rs`, `library/core/src/char/methods.rs`, `src/tools/unicode-table-generator`, versão 1.99.0)
- Uso: a lógica de `str::to_lowercase` (regra do sigma final: `map_uppercase_sigma`, `case_ignorable_then_cased`), de `str::to_uppercase`, de `char::is_cased`/`is_case_ignorable` e a forma como o gerador do Rust monta as tabelas foram traduzidas para `src/LeptonTech.SqlParser.Core/Support/RustString.cs`, `RustCaseMapping.cs` e `tools/codegen/Generate-CaseMapping.ps1`. Usada sob a Apache License 2.0, cujo texto está em [`licenses/sqlparser-rs-LICENSE.txt`](licenses/sqlparser-rs-LICENSE.txt); os direitos autorais pertencem aos contribuidores do projeto Rust (arquivo `COPYRIGHT` do repositório).

## Ferramentas de build (não distribuídas)

| Pacote | Licença | Uso |
|---|---|---|
| PolySharp | MIT | Polyfills de linguagem gerados em compilação (`PrivateAssets=all`) |
| xunit, Microsoft.NET.Test.Sdk | Apache-2.0 / MIT | Somente no projeto de testes |
