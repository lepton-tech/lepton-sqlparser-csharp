# ADR-0011: Renome para Lepton.SqlParser

- **Status:** Aceita
- **Data:** 2026-10-19
- **Decidido por:** responsável pelo projeto ("renomear para Lepton.SqlParser")

> **Notas antigas.** Os documentos e registros anteriores a 2026-10-19 (notas das ondas A a D, da sincronização e do Plano B, a análise técnica, as entradas antigas do diário em `docs/port/progresso.md` e do `CHANGELOG.md`) usam os nomes antigos. Leia `SqlParser` (projeto, assembly ou namespace) como `Lepton.SqlParser`, `SqlParser.Core` como `Lepton.SqlParser.Core` e `LeptonTI.SqlParser` como `Lepton.SqlParser`.

## Contexto

- Até a `0.63.1-preview.2`, o pacote se chamava `LeptonTI.SqlParser`, mas os projetos, os assemblies e os namespaces continuavam `SqlParser` e `SqlParser.Core` ([ADR-0010](0010-empacotamento.md)).
- O SqlParser-cs também usa o assembly `SqlParser.dll` e o namespace `SqlParser`. As duas bibliotecas não conviviam no mesmo projeto ([ADR-0008](0008-api-publica-fase-1.md), "A confirmar", item 2), e o benchmark precisava de cópias renomeadas do SqlParser-cs e de `extern alias`.
- O repositório passou a ter remoto no GitHub (`lepton-tech/lepton-sqlparser-csharp`). Isso resolve a pendência do ADR-0010 sobre os endereços do pacote e o SourceLink.
- O pacote `LeptonTI.SqlParser` nunca foi publicado em feed: só existiu em `artifacts/`, gerado localmente.

## Decisão

Um nome só, **`Lepton.SqlParser`**, para o pacote, os projetos, os assemblies, os namespaces e a solução.

| Antes | Depois |
|---|---|
| Pacote `LeptonTI.SqlParser` (`Product` no `Directory.Build.props`) | `Lepton.SqlParser` |
| `src/SqlParser/SqlParser.csproj`: assembly `SqlParser.dll`, namespace `SqlParser` | `src/Lepton.SqlParser/Lepton.SqlParser.csproj`: `Lepton.SqlParser.dll`, `Lepton.SqlParser` |
| `src/SqlParser.Core/SqlParser.Core.csproj`: assembly `SqlParser.Core.dll`; namespaces `SqlParser.Core`, `SqlParser.Core.Ast`, `SqlParser.Core.Dialects` | `src/Lepton.SqlParser.Core/Lepton.SqlParser.Core.csproj`: `Lepton.SqlParser.Core.dll`; `Lepton.SqlParser.Core`, `Lepton.SqlParser.Core.Ast`, `Lepton.SqlParser.Core.Dialects` |
| `tests/SqlParser.Core.Tests/` (projeto, assembly e namespace `SqlParser.Core.Tests`) | `tests/Lepton.SqlParser.Core.Tests/` (`Lepton.SqlParser.Core.Tests`) |
| `tests/SqlParser.Tests/` (`SqlParser.Tests`) | `tests/Lepton.SqlParser.Tests/` (`Lepton.SqlParser.Tests`) |
| `InternalsVisibleTo` para `SqlParser.Tests` e `SqlParser.Core.Tests` | `Lepton.SqlParser.Tests` e `Lepton.SqlParser.Core.Tests` |
| `SqlParser.sln` | `Lepton.SqlParser.sln` |
| `tools/bench/SqlParser.Bench/` (namespace e assembly `SqlParser.Bench`; cópias do SqlParser-cs em `%TEMP%/SqlParser.Bench/`) | `tools/bench/Lepton.SqlParser.Bench/` (`Lepton.SqlParser.Bench`; `%TEMP%/Lepton.SqlParser.Bench/`) |
| `tests/PackageSmoke/nuget.config`: fonte `leptonti-local`, padrão `LeptonTI.*`; propriedade `LeptonTISqlParserVersion` | Fonte `lepton-local`, padrão `Lepton.*`; propriedade `LeptonSqlParserVersion` |
| `PackageProjectUrl`/`RepositoryUrl` comentados (sem remoto) | `https://github.com/lepton-tech/lepton-sqlparser-csharp`, `RepositoryType` `git`, `PublishRepositoryUrl` `true` |
| Versão `0.63.1-preview.2` | `0.63.1-preview.3` (o Rust portado continua o mesmo, `dacdbc9`; só o sufixo avança, como pede o ADR-0010) |

**O que não muda:**
- Os nomes dos tipos: os públicos da API (`SqlQueryParser`, `SqlParseException`, `SqlParseResult`, `SqlVisitor`, `SqlRewriter`, `ControlFlow`, `SqlStatementKind`...) e todos os do Core (os do Rust). Só o namespace muda.
- As classes de teste com o nome do arquivo Rust (`SqlParserCommonTests1`, `SqlParserPostgresTests2`...).
- As propriedades e os alvos internos do MSBuild com prefixo `SqlParser` (`SqlParserRsCommit`, `SqlParserRsBaselineTag`, `SqlParserDirtyBuild`, `SqlParserVisitorCodegen`, `_IncludeSqlParserCoreInPackage`...) e os `AssemblyMetadata` `SqlParserRsCommit` e `SqlParserRsBaselineTag`. São nomes de configuração, não de namespace; trocá-los só quebraria scripts e quem lê os metadados.
- `Authors` e `Company` ("Lepton TI").
- Tudo o que se refere ao SqlParser-cs (`upstream/SqlParser-cs`, o `extern alias SqlParserCs` e as cópias `SqlParserCs.*` do benchmark).

**SourceLink:** o SourceLink embutido no SDK 8 ou superior gera os links dos fontes a partir do remoto do Git; o GitHub é suportado sem pacote nem item extra. `RepositoryUrl` e `PublishRepositoryUrl` põem o endereço e o commit no `<repository>` do `.nuspec`, e `PackageProjectUrl` vira o `projectUrl`.

**Arquivos gerados:** os geradores (`Generate-Keywords.ps1`, `Generate-Visitor.ps1` com o `VisitorGen` e `Generate-CaseMapping.ps1`) passaram a emitir o namespace novo e a apontar para `src/Lepton.SqlParser.Core/`. Os três foram rodados de novo: o conteúdo gerado difere do anterior só no namespace e no caminho citado no cabeçalho.

## Consequências

- **Quebra para consumidores locais** (não há outros, porque o pacote antigo nunca foi publicado): trocar o `PackageReference` (`LeptonTI.SqlParser` → `Lepton.SqlParser`) e os `using` (`SqlParser` → `Lepton.SqlParser`; `SqlParser.Core*` → `Lepton.SqlParser.Core*`). Nenhum tipo muda de nome, e a AST, o comportamento e o SQL gerado são os mesmos. Não há pacote de transição nem redirecionamento.
- **Código compilado contra a versão anterior** precisa ser recompilado: os assemblies e os nomes completos dos tipos mudaram.
- **Fim do conflito com o SqlParser-cs** (ADR-0008, "A confirmar", item 2): com assemblies e namespaces distintos, as duas bibliotecas podem ser referenciadas no mesmo projeto, o que permite uma migração lado a lado. Num mesmo arquivo, os tipos de nome igual (`SqlQueryParser`, `Parser`, `Dialect`, `Statement`...) pedem alias. O benchmark mantém o `extern alias` e as cópias renomeadas por esse motivo e pelos do `tools/bench/README.md`.
- **Clones e branches existentes:** as pastas mudaram (`src/SqlParser.Core` → `src/Lepton.SqlParser.Core` etc.). Apague os `bin/` e `obj/` antigos. Uma branch aberta antes do renome precisa de rebase ou merge com detecção de renomes, que o Git faz para arquivos movidos.
- **Documentação:** foi atualizada onde descreve o estado atual (READMEs, `CLAUDE.md`, ADRs, convenções, glossário, divergências, skills, READMEs das ferramentas). As notas históricas ficam como estão (aviso no início deste ADR e do `docs/port/progresso.md`).

## Alternativas rejeitadas

- **Trocar só o ID do pacote** e manter os namespaces `SqlParser` e `SqlParser.Core`: manteria o conflito com o SqlParser-cs e a diferença entre o nome do pacote e o do código.
- **`LeptonTI.SqlParser` em tudo:** o nome escolhido pelo responsável é `Lepton.SqlParser`, e o ID antigo nunca foi publicado, então não há compatibilidade a preservar.
- **Renomear também as propriedades do MSBuild e os metadados com prefixo `SqlParser`:** sem ganho para o consumidor e com risco de quebrar scripts; ficam como estão.
