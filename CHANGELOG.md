# Changelog

Mudanças relevantes do pacote `LeptonTech.SqlParser` (até a `0.63.1-preview.2`, `LeptonTI.SqlParser`; na `0.63.1-preview.3`, `Lepton.SqlParser`; [ADR-0011](docs/adr/0011-renome-lepton-sqlparser.md) e [ADR-0012](docs/adr/0012-renome-leptontech-sqlparser.md)).

O formato segue o [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/). A versão acompanha o sqlparser-rs ([ADR-0010](docs/adr/0010-empacotamento.md)): `0.63.x` corresponde ao Rust `v0.63`, e mudanças só nossas avançam o último número ou o sufixo de pré-lançamento.

## [Não lançado]

## [0.63.1-preview.4] - 2026-10-19

Renome do pacote, dos assemblies e dos namespaces para `LeptonTech.SqlParser` ([ADR-0012](docs/adr/0012-renome-leptontech-sqlparser.md)). O nuget.org recusou o ID `Lepton.SqlParser` (`409`, prefixo reservado), e a `0.63.1-preview.3` não foi publicada. O Rust portado é o mesmo (commit `dacdbc9`): a AST, o comportamento e o SQL gerado não mudam.

### Alterado

- **Quebra de compatibilidade (nomes):**
  - pacote `Lepton.SqlParser` → `LeptonTech.SqlParser`;
  - assemblies `Lepton.SqlParser.dll` → `LeptonTech.SqlParser.dll` e `Lepton.SqlParser.Core.dll` → `LeptonTech.SqlParser.Core.dll`;
  - namespaces `Lepton.SqlParser` → `LeptonTech.SqlParser` e `Lepton.SqlParser.Core`, `Lepton.SqlParser.Core.Ast`, `Lepton.SqlParser.Core.Dialects` → `LeptonTech.SqlParser.Core`, `LeptonTech.SqlParser.Core.Ast`, `LeptonTech.SqlParser.Core.Dialects`.

  Os nomes dos tipos não mudam. Quem usou o pacote local troca o `PackageReference` e os `using` e recompila.
- `InternalsVisibleTo` passa a valer para `LeptonTech.SqlParser.Tests` e `LeptonTech.SqlParser.Core.Tests`.
- Repositório: pastas, projetos e solução renomeados (`src/LeptonTech.SqlParser`, `src/LeptonTech.SqlParser.Core`, `tests/LeptonTech.SqlParser.Tests`, `tests/LeptonTech.SqlParser.Core.Tests`, `LeptonTech.SqlParser.sln`, `tools/bench/LeptonTech.SqlParser.Bench`); arquivos gerados com o namespace novo.
- `nuget-publish.yml`: o `user` do `NuGet/login` passa a vir do secret `NUGET_USER` (valor `lepton.tech`), como no artigo da Microsoft sobre Trusted Publishing.

## [0.63.1-preview.3] - 2026-10-19

> **Não publicada no nuget.org:** o envio recebeu `409` (ID reservado). Substituída pela `0.63.1-preview.4`.

Renome do pacote, dos assemblies e dos namespaces para `Lepton.SqlParser` ([ADR-0011](docs/adr/0011-renome-lepton-sqlparser.md)). O Rust portado é o mesmo da `0.63.1-preview.2` (commit `dacdbc9`): a AST, o comportamento e o SQL gerado não mudam.

### Adicionado

- **GitHub Actions** (`.github/workflows/`): `ci.yml` (build e testes em net48, net8.0 e net10.0 a cada push e pull request na `develop` e na `main`, mais a conferência do visitor gerado) e `nuget-publish.yml` (com uma tag `v<versão>`: conferência da tag com a versão, testes, pacote, instalação em net48 e net8.0 e publicação no nuget.org por Trusted Publishing, no ambiente `production`).

### Alterado

- **Quebra de compatibilidade (nomes):**
  - pacote `LeptonTI.SqlParser` → `Lepton.SqlParser`;
  - assemblies `SqlParser.dll` → `Lepton.SqlParser.dll` e `SqlParser.Core.dll` → `Lepton.SqlParser.Core.dll`;
  - namespaces `SqlParser` → `Lepton.SqlParser` e `SqlParser.Core`, `SqlParser.Core.Ast`, `SqlParser.Core.Dialects` → `Lepton.SqlParser.Core`, `Lepton.SqlParser.Core.Ast`, `Lepton.SqlParser.Core.Dialects`.

  Os nomes dos tipos não mudam. O ID antigo nunca foi publicado em feed; quem usou o pacote local troca o `PackageReference` e os `using` e recompila.
- Os namespaces e os assemblies deixam de colidir com os do SqlParser-cs (`SqlParser`, `SqlParser.dll`): as duas bibliotecas podem ser referenciadas no mesmo projeto.
- `InternalsVisibleTo` passa a valer para `Lepton.SqlParser.Tests` e `Lepton.SqlParser.Core.Tests`.
- Repositório: pastas, projetos e solução renomeados (`src/Lepton.SqlParser`, `src/Lepton.SqlParser.Core`, `tests/Lepton.SqlParser.Tests`, `tests/Lepton.SqlParser.Core.Tests`, `Lepton.SqlParser.sln`, `tools/bench/Lepton.SqlParser.Bench`); geradores de código atualizados e arquivos gerados regenerados (só o namespace e o caminho do cabeçalho mudam).

### Adicionado

- Endereços do projeto no pacote: `projectUrl` e `repository` (`https://github.com/lepton-tech/lepton-sqlparser-csharp`). O SourceLink do SDK gera os links dos fontes nos PDBs embutidos a partir desse repositório.

### Documentação

- ADR-0011 (renome, com a tabela de nomes antigos e novos). READMEs, `CLAUDE.md`, ADRs, convenções, glossário, divergências e skills usam os nomes novos; as notas históricas mantêm os antigos.

## [0.63.1-preview.2] - 2026-10-15

Correções da revisão R4 da API pública e do pacote (unidade F1; [`docs/port/onda-d/R4.md`](docs/port/onda-d/R4.md)) e desempenho do parser (unidade P2; [`docs/port/onda-d/P2.md`](docs/port/onda-d/P2.md)).

### Adicionado

- `SqlStatementKind.Block`, para os blocos `BEGIN ... END` do SQL Server, do BigQuery e do Snowflake (no fim do enum: os valores dos membros existentes não mudam).

### Alterado

- **Desempenho do parser (`SqlParser.Core`):** as tentativas especulativas mais frequentes (o literal tipado, como `DATE '...'`, testado em quase toda expressão, e o argumento nomeado, em quase todo argumento de função) deixaram de lançar e capturar exceções quando falham, e as demais lançam uma vez, não duas. O resultado é o mesmo (mesma AST, mesmo SQL gerado, mesmos erros e posições). Nas cargas do `tools/bench`, as exceções por parse caíram de 4–10.002 para 0–40, e o parse ficou de 4,9 a 18 vezes mais rápido no .NET Framework 4.8 e de 7,5 a 49 vezes no .NET 8 (por exemplo, um `INSERT` de 1.000 linhas: de 100 ms para 5,4 ms no net48). O depurador também deixa de parar em milhares de exceções de primeira chance.
- **`GetStatementKind`:** um bloco `BEGIN ... END` passa a ser `Block`, e não `Transaction`. `BEGIN`/`START TRANSACTION` sem bloco continuam `Transaction`.
- **`GetReferencedTables` e `GetReferencedColumns`:** o alvo de `SELECT ... INTO t` e de `OUTPUT ... INTO t` (SQL Server) passa a ser tabela e sai das colunas (antes, `SELECT * INTO #tmp FROM t` dava as tabelas `[t]` e as colunas `[#tmp]`).
- **`GetReferencedTables`:** dentro da definição de uma CTE, o próprio nome só conta como a CTE quando ela pode ser recursiva (`WITH RECURSIVE`, ou a definição é uma operação de conjunto como `UNION ALL`). Antes contava sempre, e `WITH orders AS (SELECT * FROM orders WHERE x = 1) SELECT * FROM orders` dava uma lista vazia; agora dá `orders`.
- **`GetReferencedFunctions`:** passa a incluir as funções de tabela: `LATERAL fn(x)`, `LATERAL FLATTEN(...)` e a chamada direta no `FROM` (`generate_series(1, 3)`, `CROSS APPLY dbo.fn(x)`; esta continua também nas tabelas). `TABLE(fn(x))` já entrava.
- **`ToSql(IEnumerable<Statement>)`:** um item `null` lança `ArgumentNullException` com `ParamName = "statements"` e a posição do item na mensagem (antes, `ParamName = "node"`).
- **Pacote:** os PDBs portáteis vão embutidos nas duas DLLs, e não há mais `.snupkg` (a depuração funciona a partir de feed local ou interno). As DLLs crescem cerca de 8% (`SqlParser.Core.dll`).
- **`tools/pack/Pack.ps1`:** recusa empacotar com alterações não commitadas (ou fora de um repositório Git); `-AllowDirty` empacota assim mesmo e marca a `InformationalVersion` com `.dirty`.

### Corrigido

- `ToSql()`/`ToString()` de subconsultas aninhadas e de junções entre parênteses (possíveis com limite de recursão elevado) e de cadeias longas de `UNION ALL` (5.000 termos, com o limite padrão) estouravam a pilha e derrubavam o processo; o `Display` de `Query`, `SetExpr` e `TableFactor` ganhou a mesma proteção do de `Expr` (desvio registrado na DIV-005).
- `Span()` dos mesmos casos (subconsultas e junções aninhadas, cadeias longas de `UNION ALL`) também estourava a pilha; o `Span()` de `Query`, `SetExpr` e `TableFactor` ganhou a mesma proteção (P2, DIV-005).

### Documentação

- DIV-010 nova: a igualdade (`==`, `Equals`, `GetHashCode`) dos nós da AST é recursiva e sem proteção de pilha; com milhares de níveis, estoura. Uso seguro: comparar `ToSql()`.
- `SqlVisitor`, `SqlRewriter` e `SqlVisitorExtensions` avisam que, em árvores profundas, os ganchos podem rodar em outra thread (DIV-009), com o cuidado para threads de UI (STA); o construtor `SqlQueryParser(int recursionLimit)` avisa o que um limite alto implica.
- A regra de comparação do nome de uma CTE com nomes entre aspas ficou documentada, com as diferenças por banco (ADR-0009).
- ADR-0010: decisões sobre árvore suja, símbolos embutidos e `InternalsVisibleTo` (mantido para os testes); marcadores comentados de `PackageProjectUrl`/`RepositoryUrl` no `.csproj`, para quando houver remoto.

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
