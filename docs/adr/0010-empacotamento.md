# ADR-0010: Empacotamento e versão

- **Status:** Aceita
- **Data:** 2026-10-15
- **Decidido por:** responsável pelo projeto

## Contexto

O objetivo é deixar a biblioteca "publicável" (pacote pronto para ser distribuído). O destino final ainda não foi definido: feed interno, Bitbucket ou nuget.org. O nome `SqlParser` sozinho colide com o SqlParser-cs no NuGet, cujo pacote é `SqlParserCS` e o namespace é `SqlParser`.

## Decisão

| Tema | Decisão |
|---|---|
| Destino desta versão | **Pacote `.nupkg` gerado localmente.** Nada é publicado fora da máquina; a escolha do feed fica para depois |
| Estrutura | **Um pacote só**, com as duas DLLs (`SqlParser.Core.dll`, o port fiel, e `SqlParser.dll`, a API pública), sempre na mesma versão |
| ID do pacote | **`LeptonTI.SqlParser`**. Os namespaces do código continuam `SqlParser` e `SqlParser.Core` |
| Versão | **Alinhada ao sqlparser-rs:** `0.63.x`. A primeira é `0.63.1-preview.1`, porque o port já inclui o `main` do Rust depois da v0.63.0 (commit `dacdbc9`, ver `UPSTREAM.json`). Quando o port chegar a uma tag nova do Rust (ex.: v0.64.0), a versão passa a `0.64.x` |
| Licença | Apache-2.0 (obra derivada do sqlparser-rs), com `NOTICE` e `THIRD-PARTY-NOTICES.md` dentro do pacote ([ADR-0003](0003-licenciamento-e-atribuicao.md)) |

## Consequências

- Quem instala o pacote recebe o Core e a API juntos. Quem só quer a AST fiel ao Rust usa os namespaces `SqlParser.Core*` do mesmo pacote.
- **Assinatura forte (strong naming):** ainda não decidida. Aplicações .NET Framework com assinatura forte exigem dependências também assinadas. Decidir antes da primeira publicação num feed.
- O número da versão comunica qual Rust está por trás. Mudanças só nossas (API pública, correções) avançam o último número ou o sufixo de pré-lançamento.

## Notas de implementação (2026-10-15)

**Como o pacote único é montado.** Só o `src/SqlParser/SqlParser.csproj` gera pacote; o `SqlParser.Core` tem `IsPackable=false`.
- A referência ao Core usa `PrivateAssets="all"`, para que ele não vire dependência do pacote. Efeito colateral: o Core não chega por transitividade a quem referencia o `SqlParser` dentro da solução, e por isso o `SqlParser.Tests` referencia o Core diretamente.
- Dois alvos do `SqlParser.csproj` levam os arquivos do Core para o pacote: `_IncludeSqlParserCoreInPackage` (`TargetsForTfmSpecificBuildOutput`: `SqlParser.Core.dll` e `.xml`) e `_IncludeSqlParserCoreSymbolsInPackage` (`TargetsForTfmSpecificDebugSymbolsInPackage`: `SqlParser.Core.pdb` no `.snupkg`). Os dois leem a saída já compilada; com `--no-build`, `BuildProjectReferences=false` impede uma nova compilação do Core.
- Dependências de runtime: **nenhuma**. O grupo `.NETStandard2.0` do `.nuspec` sai vazio; o PolySharp é só de compilação (`PrivateAssets=all`), e nada do código precisa de `System.Memory`, `System.ValueTuple` ou afins em `netstandard2.0`.

**O que vai dentro.**

| Pacote | Conteúdo |
|---|---|
| `LeptonTI.SqlParser.<versão>.nupkg` | `lib/netstandard2.0/`: `SqlParser.dll`, `SqlParser.xml`, `SqlParser.Core.dll`, `SqlParser.Core.xml`; na raiz: `README.md` (de `src/SqlParser/PACKAGE-README.md`), `NOTICE`, `THIRD-PARTY-NOTICES.md`; `licenses/` (sqlparser-rs, SqlParser-cs, Unicode) |
| `LeptonTI.SqlParser.<versão>.snupkg` | `lib/netstandard2.0/`: `SqlParser.pdb` e `SqlParser.Core.pdb` (PDBs portáteis) |

Metadados: `PackageId` `LeptonTI.SqlParser`, `Authors`/`Company` "Lepton TI", `PackageLicenseExpression` `Apache-2.0`, descrição e tags em inglês (a descrição diz que o pacote não é afiliado nem endossado pela ASF, ADR-0003 §3), `PackageReleaseNotes` com o commit do Rust.

**Versões** (`Directory.Build.props` e `Directory.Build.targets`):

| Propriedade | Valor | Regra |
|---|---|---|
| `VersionPrefix` / `VersionSuffix` | `0.63.1` / `preview.1` | Versão do pacote; a única coisa a mudar a cada versão (com o `CHANGELOG.md`) |
| `AssemblyVersion` e `FileVersion` | `0.63.1.0` | `<VersionPrefix>.0`, sem o sufixo. Sem assinatura forte, o .NET Framework não usa a `AssemblyVersion` na vinculação. Se a assinatura forte for adotada, rever (ex.: fixar `0.<minor>.0.0` para reduzir *binding redirects*) |
| `InformationalVersion` | `0.63.1-preview.1+sqlparser-rs.dacdbc9.<commit deste repositório>` | `$(Version)+sqlparser-rs.<commit curto>`, com o commit lido do `UPSTREAM.json` (`portedThroughCommit`); o SDK acrescenta o commit do Git deste repositório (`SourceRevisionId`). Com alterações não commitadas, esse último commit não identifica o conteúdo exato |
| `AssemblyMetadata` | `SqlParserRsBaselineTag`, `SqlParserRsCommit` | Tag-base e commit completo do Rust, também lidos do `UPSTREAM.json` |

**Documentação XML.** Gerada nas duas DLLs. A API pública (`SqlParser`) não suprime nada: todo membro público tem `<summary>`. O Core suprime só o CS1591 (membro público sem comentário XML; cerca de 1.500 membros), porque a documentação do Core é a do Rust, verbatim (`convencoes.md` §12), e itens sem doc no Rust não ganham texto inventado. Os demais avisos de XML doc continuam valendo.

**Build reproduzível e símbolos.** `Deterministic` sempre; `ContinuousIntegrationBuild` (caminhos `/_/` nos PDBs) quando há variável de CI (`CI`, `TF_BUILD`, `BITBUCKET_BUILD_NUMBER`) ou quando passado explicitamente, como faz o `Pack.ps1`. `EmbedUntrackedSources` embute nos PDBs os fontes que o Git não rastreia (os gerados em `obj/`). `.snupkg` com PDBs portáteis.

**Scripts.** `tools/pack/Pack.ps1`: restaura, compila em Release, roda a suíte inteira (net48, net8.0 e net10.0) e só então empacota em `artifacts/` (fora do Git), listando o conteúdo dos pacotes. `tools/pack/Test-Package.ps1`: apaga a entrada do pacote no cache global do NuGet e roda `tests/PackageSmoke/` (fora da solução): dois apps de console, net48 e net8.0, em C# 7.3, que instalam o pacote do feed local (`tests/PackageSmoke/nuget.config`, com mapeamento de fontes: `LeptonTI.*` só de `artifacts/`), analisam SQL em três dialetos e conferem `ToSql`, `GetReferencedTables`, a posição de um erro e uma reescrita.

**Pendências para o responsável:**
- **Assinatura forte:** continua em aberto (ver Consequências). As DLLs saem sem assinatura.
- **SourceLink:** o repositório não tem remoto. Quando for para o Bitbucket, definir `RepositoryUrl` e `PublishRepositoryUrl`; o SourceLink embutido no SDK 8 ou superior passa a gerar os links (para Bitbucket Server, declarar também o item `SourceLinkBitbucketGitHost`).
- **Feed:** a escolha (interno, Bitbucket ou nuget.org) define a publicação e a validação de pacote (`EnablePackageValidation` com versão de referência).