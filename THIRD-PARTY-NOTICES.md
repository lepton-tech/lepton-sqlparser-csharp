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

## Ferramentas de build (não distribuídas)

| Pacote | Licença | Uso |
|---|---|---|
| PolySharp | MIT | Polyfills de linguagem gerados em compilação (`PrivateAssets=all`) |
| xunit, Microsoft.NET.Test.Sdk | Apache-2.0 / MIT | Somente no projeto de testes |
