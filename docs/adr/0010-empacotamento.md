# ADR-0010: Empacotamento e versão

- **Status:** Aceita (ID do pacote, assemblies e namespaces alterados pelos [ADR-0011](0011-renome-lepton-sqlparser.md) e [ADR-0012](0012-renome-leptontech-sqlparser.md))
- **Data:** 2026-10-15
- **Decidido por:** responsável pelo projeto

## Contexto

O objetivo é deixar a biblioteca "publicável" (pacote pronto para ser distribuído). O destino final ainda não foi definido: feed interno, Bitbucket ou nuget.org. O nome `SqlParser` sozinho colide com o SqlParser-cs no NuGet, cujo pacote é `SqlParserCS` e o namespace é `SqlParser`.

## Decisão

| Tema | Decisão |
|---|---|
| Destino desta versão | **Pacote `.nupkg` gerado localmente.** Nada é publicado fora da máquina; a escolha do feed fica para depois |
| Estrutura | **Um pacote só**, com as duas DLLs (`LeptonTech.SqlParser.Core.dll`, o port fiel, e `LeptonTech.SqlParser.dll`, a API pública), sempre na mesma versão |
| ID do pacote | **`LeptonTech.SqlParser`**, com assemblies e namespaces `LeptonTech.SqlParser` e `LeptonTech.SqlParser.Core` ([ADR-0012](0012-renome-leptontech-sqlparser.md)). Até a `0.63.1-preview.2`, o ID era `LeptonTI.SqlParser` e os namespaces continuavam `SqlParser` e `SqlParser.Core`; na `0.63.1-preview.3`, tudo era `Lepton.SqlParser` ([ADR-0011](0011-renome-lepton-sqlparser.md)), ID que o nuget.org recusou por cair num prefixo reservado |
| Versão | **Alinhada ao sqlparser-rs:** `0.63.x`. A primeira é `0.63.1-preview.1`, porque o port já inclui o `main` do Rust depois da v0.63.0 (commit `dacdbc9`, ver `UPSTREAM.json`). Quando o port chegar a uma tag nova do Rust (ex.: v0.64.0), a versão passa a `0.64.x` |
| Licença | Apache-2.0 (obra derivada do sqlparser-rs), com `NOTICE` e `THIRD-PARTY-NOTICES.md` dentro do pacote ([ADR-0003](0003-licenciamento-e-atribuicao.md)) |

## Consequências

- Quem instala o pacote recebe o Core e a API juntos. Quem só quer a AST fiel ao Rust usa os namespaces `LeptonTech.SqlParser.Core*` do mesmo pacote.
- **Assinatura forte (strong naming):** ainda não decidida. Aplicações .NET Framework com assinatura forte exigem dependências também assinadas. Decidir antes da primeira publicação num feed.
- O número da versão comunica qual Rust está por trás. Mudanças só nossas (API pública, correções) avançam o último número ou o sufixo de pré-lançamento.

## Notas de implementação (2026-10-15)

**Como o pacote único é montado.** Só o `src/LeptonTech.SqlParser/LeptonTech.SqlParser.csproj` gera pacote; o `LeptonTech.SqlParser.Core` tem `IsPackable=false`.
- A referência ao Core usa `PrivateAssets="all"`, para que ele não vire dependência do pacote. Efeito colateral: o Core não chega por transitividade a quem referencia o `LeptonTech.SqlParser` dentro da solução, e por isso o `LeptonTech.SqlParser.Tests` referencia o Core diretamente.
- O alvo `_IncludeSqlParserCoreInPackage` do `LeptonTech.SqlParser.csproj` (`TargetsForTfmSpecificBuildOutput`) leva `LeptonTech.SqlParser.Core.dll` e `.xml` para o pacote, lendo a saída já compilada; com `--no-build`, `BuildProjectReferences=false` impede uma nova compilação do Core. (Até a F1 havia um segundo alvo, `_IncludeSqlParserCoreSymbolsInPackage`, que levava `SqlParser.Core.pdb` para o `.snupkg`; com os PDBs embutidos, saiu.)
- Dependências de runtime: **nenhuma**. O grupo `.NETStandard2.0` do `.nuspec` sai vazio; o PolySharp é só de compilação (`PrivateAssets=all`), e nada do código precisa de `System.Memory`, `System.ValueTuple` ou afins em `netstandard2.0`.

**O que vai dentro.**

| Pacote | Conteúdo |
|---|---|
| `LeptonTech.SqlParser.<versão>.nupkg` | `lib/netstandard2.0/`: `LeptonTech.SqlParser.dll`, `LeptonTech.SqlParser.xml`, `LeptonTech.SqlParser.Core.dll`, `LeptonTech.SqlParser.Core.xml` (cada DLL com o PDB portátil embutido); na raiz: `README.md` (de `src/LeptonTech.SqlParser/PACKAGE-README.md`), `NOTICE`, `THIRD-PARTY-NOTICES.md`; `licenses/` (sqlparser-rs, SqlParser-cs, Unicode) |

Não há mais `.snupkg` (até a F1, ele levava `SqlParser.pdb` e `SqlParser.Core.pdb`; ver "Revisão R4" abaixo).

Metadados: `PackageId` `LeptonTech.SqlParser`, `Authors`/`Company` "Lepton TI", `PackageProjectUrl` e `RepositoryUrl` `https://github.com/lepton-tech/lepton-sqlparser-csharp` (ADR-0011), `PackageLicenseExpression` `Apache-2.0`, descrição e tags em inglês (a descrição diz que o pacote não é afiliado nem endossado pela ASF, ADR-0003 §3), `PackageReleaseNotes` com o commit do Rust.

**Versões** (`Directory.Build.props` e `Directory.Build.targets`):

| Propriedade | Valor | Regra |
|---|---|---|
| `VersionPrefix` / `VersionSuffix` | `0.63.1` / `preview.4` (atual; a primeira foi `preview.1`) | Versão do pacote; a única coisa a mudar a cada versão (com o `CHANGELOG.md`) |
| `AssemblyVersion` e `FileVersion` | `0.63.1.0` | `<VersionPrefix>.0`, sem o sufixo. Sem assinatura forte, o .NET Framework não usa a `AssemblyVersion` na vinculação. Se a assinatura forte for adotada, rever (ex.: fixar `0.<minor>.0.0` para reduzir *binding redirects*) |
| `InformationalVersion` | `0.63.1-preview.4+sqlparser-rs.dacdbc9.<commit deste repositório>` | `$(Version)+sqlparser-rs.<commit curto>`, com o commit lido do `UPSTREAM.json` (`portedThroughCommit`); o SDK acrescenta o commit do Git deste repositório (`SourceRevisionId`). Com alterações não commitadas, esse commit não identificaria o conteúdo: o `Pack.ps1` recusa empacotar, e com `-AllowDirty` a versão termina em `.dirty` (`SqlParserDirtyBuild=true`, alvo `_SqlParserMarkDirtyBuild` em `Directory.Build.targets`) |
| `AssemblyMetadata` | `SqlParserRsBaselineTag`, `SqlParserRsCommit` | Tag-base e commit completo do Rust, também lidos do `UPSTREAM.json` |

**Documentação XML.** Gerada nas duas DLLs. A API pública (`LeptonTech.SqlParser`) não suprime nada: todo membro público tem `<summary>`. O Core suprime só o CS1591 (membro público sem comentário XML; cerca de 1.500 membros), porque a documentação do Core é a do Rust, verbatim (`convencoes.md` §12), e itens sem doc no Rust não ganham texto inventado. Os demais avisos de XML doc continuam valendo.

**Build reproduzível e símbolos.** `Deterministic` sempre; `ContinuousIntegrationBuild` (caminhos `/_/` nos PDBs) quando há variável de CI (`CI`, `TF_BUILD`, `BITBUCKET_BUILD_NUMBER`) ou quando passado explicitamente, como faz o `Pack.ps1`. `EmbedUntrackedSources` embute nos PDBs os fontes que o Git não rastreia (os gerados em `obj/`). PDBs portáteis, **embutidos** nas duas DLLs do pacote (`DebugType=embedded` para `LeptonTech.SqlParser` e `LeptonTech.SqlParser.Core` em `Directory.Build.props`; os projetos de teste continuam com o PDB em arquivo). Sem `.snupkg` (`IncludeSymbols=false`).

**Scripts.** `tools/pack/Pack.ps1`: confere que a árvore de trabalho do Git está limpa (`git status --porcelain`, só leitura), restaura, compila em Release, roda a suíte inteira (net48, net8.0 e net10.0) e só então empacota em `artifacts/` (fora do Git), listando o conteúdo do pacote e a `InformationalVersion` das DLLs. Com alterações não commitadas (ou arquivos novos fora do `.gitignore`), ou fora de um repositório Git, o script para antes de compilar; `-AllowDirty` empacota assim mesmo, com `.dirty` no fim da `InformationalVersion`. `tools/pack/Test-Package.ps1`: apaga a entrada do pacote no cache global do NuGet e roda `tests/PackageSmoke/` (fora da solução): dois apps de console, net48 e net8.0, em C# 7.3, que instalam o pacote do feed local (`tests/PackageSmoke/nuget.config`, com mapeamento de fontes: `LeptonTech.*` só de `artifacts/`), analisam SQL em três dialetos e conferem `ToSql`, `GetReferencedTables`, a posição de um erro e uma reescrita.

**Pendências para o responsável:**
- **Assinatura forte:** continua em aberto (ver Consequências). As DLLs saem sem assinatura.
- ~~**SourceLink e endereços do projeto:** o repositório não tem remoto. Quando for para o Bitbucket, descomentar e preencher os marcadores do `src/SqlParser/SqlParser.csproj` (`PackageProjectUrl`, `RepositoryUrl`, `RepositoryType`, `PublishRepositoryUrl`); o SourceLink embutido no SDK 8 ou superior passa a gerar os links (para Bitbucket Server, declarar também o item `SourceLinkBitbucketGitHost`). Até lá, o `.nuspec` sai sem `projectUrl`.~~ **Resolvido no [ADR-0011](0011-renome-lepton-sqlparser.md):** o repositório está no GitHub (`lepton-tech/lepton-sqlparser-csharp`); os endereços estão preenchidos em `src/LeptonTech.SqlParser/LeptonTech.SqlParser.csproj`, e o SourceLink do SDK gera os links (host GitHub, sem item extra).
- **Feed:** a escolha (interno, Bitbucket ou nuget.org) define a publicação e a validação de pacote (`EnablePackageValidation` com versão de referência).

## Revisão R4 (F1, 2026-10-15)

A revisão R4 (`docs/port/onda-d/R4.md`, itens 11 e 12) apontou três problemas no pacote `0.63.1-preview.1`. Decisões da F1:

**1. Árvore suja (item 11).** O pacote tinha sido gerado com alterações não commitadas, então o commit gravado na `InformationalVersion` (e no `repository commit` do `.nuspec`) não identificava o conteúdo.
- O `Pack.ps1` recusa empacotar quando `git status --porcelain` lista qualquer coisa (inclusive arquivos novos fora do `.gitignore`; `artifacts/`, `bin/` e `obj/` são ignorados) ou quando não está num repositório Git. O comando é só de leitura.
- `-AllowDirty` empacota assim mesmo, para testes locais, e passa `-p:SqlParserDirtyBuild=true` à compilação: a `InformationalVersion` termina em `.dirty` (ex.: `0.63.1-preview.1+sqlparser-rs.dacdbc9.<commit>.dirty`). A versão do pacote não muda; o `.dirty` é metadado de compilação (SemVer 2.0, depois do `+`) e não entra na resolução do NuGet. O `repository commit` do `.nuspec` continua sendo o commit do `HEAD` (o campo só aceita um commit); a marca fica nas DLLs, e o `Pack.ps1` e o `Test-Package.ps1` a mostram.
- Alternativa rejeitada: só um aviso. O pacote sai igual e o aviso some no meio da saída do `dotnet`.

**2. Símbolos (item 12).** O `.snupkg` só é aceito pelo nuget.org e por servidores de símbolos; num feed local (pasta) ou interno, o depurador não o encontra.
- **Decisão:** PDB portátil **embutido** nas duas DLLs (`DebugType=embedded`) e nenhum `.snupkg`. O depurador do Visual Studio/Rider e os rastros de pilha (`StackTrace` com arquivo e linha, no .NET Framework 4.7.2+ e no .NET Core) acham os símbolos na própria DLL, em qualquer feed. O nuget.org também aceita esse formato, caso seja o destino.
- **Custo:** as DLLs crescem o tamanho do PDB, que vai comprimido. Medido na F1 (que também acrescentou um pouco de código), em relação ao pacote anterior: `SqlParser.Core.dll` de 4.175.360 para 4.527.104 bytes (+8%) e `SqlParser.dll` de 36.352 para 71.680 bytes; o `.nupkg` de 1.542.867 para 1.928.038 bytes, sem o `.snupkg` de 386.555 bytes. Para uma biblioteca de análise, sem restrição de tamanho de implantação, compensa.
- Alternativas rejeitadas: manter o `.snupkg` além do PDB embutido (com o PDB dentro da DLL, não há `.pdb` em arquivo para ele levar; gerar os dois duplicaria os símbolos sem ganho); copiar os `.pdb` para dentro do `.nupkg` (funciona em feed local, mas é a forma que o NuGet desaconselha).

**3. `InternalsVisibleTo` (item 12).** As DLLs do pacote dão acesso aos membros `internal` para `LeptonTech.SqlParser.Tests` e `LeptonTech.SqlParser.Core.Tests` (até a `0.63.1-preview.2`, `SqlParser.Tests` e `SqlParser.Core.Tests`; na `0.63.1-preview.3`, `Lepton.SqlParser.Tests` e `Lepton.SqlParser.Core.Tests`).
- **Decisão: manter.** O `Pack.ps1` roda os testes em Release contra as **mesmas** DLLs que empacota (`dotnet pack --no-build`): tirar o atributo só no pacote exigiria uma segunda compilação, e o pacote deixaria de conter os binários testados. Os testes do Core usam membros `internal` (ex.: `StackGuard`, `RustString`); os da API hoje não usam nenhum, mas o atributo fica, pelo mesmo motivo, para quando precisarem.
- **Risco:** sem assinatura forte, qualquer assembly chamado `LeptonTech.SqlParser.Tests` ou `LeptonTech.SqlParser.Core.Tests` enxerga os membros `internal`. Isso não é fronteira de segurança (reflexão também chega a eles), e um consumidor não tem motivo para usar esses nomes. Se a assinatura forte for adotada, o atributo passa a exigir a chave pública dos testes, e o risco desaparece.
- Comentário no `.csproj` dos dois projetos aponta para esta seção.
## Publicação (GitHub Actions, 2026-10-19)

O repositório está em `https://github.com/lepton-tech/lepton-sqlparser-csharp`, com o padrão `develop`: o trabalho entra na `develop` por pull request, e a `main` recebe da `develop` nas versões. O destino do pacote é o **nuget.org**, por **Trusted Publishing**: uma credencial OIDC de curta duração emitida pelo GitHub Actions, sem chave de API guardada. Referência: <https://learn.microsoft.com/pt-br/nuget/nuget-org/trusted-publishing>.

| Workflow | Quando roda | O que faz |
|---|---|---|
| `.github/workflows/ci.yml` | Push e pull request na `develop` e na `main` (e manual) | Windows: restore, build em Release (0 avisos), testes em net48, net8.0 e net10.0 (resultados como artefato) e `Generate-Visitor.ps1 -Check` |
| `.github/workflows/nuget-publish.yml` | Tag `v*` | **build** (Windows, por causa do net48): tag igual a `v<PackageVersion>` do projeto (`VersionPrefix`/`VersionSuffix` do `Directory.Build.props`), `Pack.ps1` (build em Release, testes nos três alvos e pacote) e `Test-Package.ps1`; o `.nupkg` vira artefato. **publish** (ambiente `production`, `id-token: write`): `NuGet/login@v1` troca o token OIDC do GitHub por uma chave de API válida por 1 hora, pedida logo antes do `dotnet nuget push`. Sem chave de API guardada: a confiança é a política no nuget.org, e o único secret é o `NUGET_USER` (o nome do usuário) |

**Política de Trusted Publishing no nuget.org** (já cadastrada; o nome do arquivo e o ambiente do workflow têm de bater com ela):
- dono do pacote: a organização `lepton-tech` do nuget.org; escopo: publicar pacotes novos e versões novas; padrão de pacotes `Lepton*`;
- Repository Owner `lepton-tech`; Repository `lepton-sqlparser-csharp`; Workflow File `nuget-publish.yml` (só o nome do arquivo, sem `.github/workflows/`); Environment `production`.

Renomear o `nuget-publish.yml` ou tirar o `environment: production` do job **publish** faz o nuget.org recusar a troca do token. O ambiente `production` do GitHub é criado na primeira execução, se ainda não existir; em *Settings → Environments* dá para exigir aprovação antes de publicar e, se houver regra de branches e tags de implantação, ela precisa aceitar as tags `v*`.

O `user` do `NuGet/login` é o **nome de perfil** no nuget.org (não o e-mail) do **usuário que criou a política**, e não o da organização dona dela (o próprio `NuGet/login` avisa, ao falhar: "use the username of the policy creator, not the policy owner"). É obrigatório na action, mas não é chave nem dá acesso sozinho. Como no artigo da Microsoft, ele fica no secret `NUGET_USER`, com o valor `lepton.tech`, em *Settings → Secrets and variables → Actions* (do repositório ou do ambiente `production`). Sem o secret, o `user` vai vazio e a publicação falha no login.

**Publicar uma versão:**
1. Na `develop`, ajuste `VersionPrefix`/`VersionSuffix` e o `CHANGELOG.md`.
2. Abra o pull request `develop` → `main` e faça o merge com "Create a merge commit".
3. Crie a tag na `main` e envie-a:

   ```powershell
   git tag v0.63.1-preview.4 origin/main
   git push origin v0.63.1-preview.4
   ```
