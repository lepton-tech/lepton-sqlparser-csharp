---
name: publish-release
description: Publicar uma versão do pacote LeptonTech.SqlParser no nuget.org. Cobre a escolha da versão; a atualização do Directory.Build.props, do CHANGELOG e das demais referências; a validação com Pack.ps1 e Test-Package.ps1; os PRs feature → develop → main; a criação da tag v<versão>, que dispara o nuget-publish.yml; e o Release do GitHub com as notas da versão. Use quando o usuário pedir para criar ou enviar uma tag, publicar ou lançar uma versão, subir a versão do pacote, fazer um release ou publicar no NuGet.
---

# Publicar uma versão (tag `v<versão>`)

**Objetivo:** levar uma versão nova do pacote `LeptonTech.SqlParser` até o nuget.org.

**Por que a tag é o último passo:**
- o push da tag `v<versão>` dispara o `.github/workflows/nuget-publish.yml`;
- o workflow só aceita a tag se ela for igual a `v<PackageVersion>`, a versão do `Directory.Build.props` no commit da tag;
- uma versão publicada no nuget.org **não pode ser apagada**, só ocultada.

Referências: [ADR-0010](../../../docs/adr/0010-empacotamento.md) ("Versão", "Publicação") e [ADR-0012](../../../docs/adr/0012-renome-leptontech-sqlparser.md).

## Regras

- **O Git e o GitHub do usuário.** Push, aprovação e merge de PR e push da tag são feitos pelo usuário:
  - o Git deste ambiente não autentica no GitHub, e não há `gh` instalado;
  - peça o push com `! git push ...` no prompt;
  - abra os PRs pelo Chrome ("Criar PR sem `gh`").
- **Commits e branches:** o pedido de versão ou de tag autoriza o fluxo desta skill. Siga o padrão develop: `feature/versao-<versão>` → `develop` → `main`, nunca push na `develop` ou na `main`. Mescle sempre com "Create a merge commit".
- **Tags já enviadas não se movem nem se apagam.** Se uma tag saiu errada, publique uma versão nova; foi assim no ADR-0012.
- **Formato da tag:** leve, no merge da `main` (`git tag v<versão> origin/main`), como as anteriores.

## 0. Ver o estado

```bash
git fetch origin --tags
git tag -l --sort=-creatordate | head -3                    # últimas tags
grep -n "Version" Directory.Build.props                     # versão atual
git ls-remote --tags origin "v*"                            # tags no GitHub
git log --oneline origin/main..origin/develop               # o que ainda não chegou à main
sed -n 1,15p CHANGELOG.md                                   # o que está em "Não lançado"
```

- Se a versão do `Directory.Build.props` já tem tag, é preciso uma versão nova (passo 1).
- Se a versão ainda não tem tag e já está na `main`, vá direto ao passo 4.

## 1. Escolher a versão

As regras de versão estão no ADR-0010:

- **Tag nova do Rust portada** (ex.: `v0.64.0`): `VersionPrefix` da linha nova (`0.64.x`).
- **Sem tag nova do Rust:** avance o `VersionSuffix` (`preview.N` → `preview.N+1`) enquanto for pré-lançamento, ou o último número do `VersionPrefix`.
- **Sair do `preview`** (versão estável) é decisão do usuário: pergunte.

## 2. Atualizar a versão

Trabalhe numa branch nova, criada a partir de `origin/develop`:

```bash
git switch -c feature/versao-<versão> origin/develop
git grep -n "<versão antiga>"      # ocorrências a atualizar; as entradas históricas ficam
git grep -n "Não lançado\|próxima versão"
```

| Arquivo | O que muda |
|---|---|
| `Directory.Build.props` | `VersionPrefix` / `VersionSuffix` |
| `CHANGELOG.md` | O conteúdo de "Não lançado" vira `## [<versão>] - <AAAA-MM-DD>`, e `## [Não lançado]` fica vazio no topo. |
| `README.md` | "Versão atual": a versão e o commit do Rust (`portedThroughCommit` do `UPSTREAM.json`, 7 caracteres). Também o `--version` do `dotnet add package`. |
| `src/LeptonTech.SqlParser/PACKAGE-README.md` | A versão e o commit do Rust. |
| `docs/adr/0010-empacotamento.md` | Na tabela, o `VersionPrefix`/`VersionSuffix` "atual" e a `InformationalVersion` (com o commit do Rust). Em "Publicar uma versão", o exemplo de tag. |
| `tools/pack/Test-Package.ps1` | O `.EXAMPLE` com `-Version`. |
| `.github/workflows/nuget-publish.yml` | O exemplo de tag no comentário do topo. |
| `docs/port/progresso.md` | A linha "Pacote" do estado e uma entrada nova no diário. |
| Docs que citam "Não lançado" ou "a próxima versão" (ADR-0009, `sync/rodada*.md`) | Passam a citar a versão. |

Os arquivos são UTF-8 sem BOM, com CRLF. Não use `sed -i`, que troca CRLF por LF; use a ferramenta de edição ou um script que leia e grave o texto inteiro.

## 3. Validar e levar à `main`

1. Rode `pwsh tools/pack/Pack.ps1`, com `-AllowDirty` se o commit ainda não foi feito. Ele faz o build em Release, roda os testes em net48, net8.0 e net10.0 e gera o pacote em `artifacts/`. Confira se a `InformationalVersion` é `<versão>+sqlparser-rs.<commit do Rust>.<commit>`.
2. Rode `pwsh tools/pack/Test-Package.ps1 -Version <versão>`: net48 e net8.0 têm que dar OK.
3. Faça o commit "Versão <versão>", no estilo do `adb7413`: o que a versão traz, os arquivos atualizados e a verificação. Feche com o trailer de coautoria.
4. O usuário envia a branch: `! git push -u origin feature/versao-<versão>`.
5. Abra o PR `feature/versao-<versão>` → `develop`, com o título "Versão <versão>". O usuário aprova e mescla.
6. Abra o PR `develop` → `main`, com o título "Versão <versão> na main". A `main` exige 1 aprovação, e o `ci.yml` roda no PR. O usuário aprova e mescla com "Create a merge commit".

## 4. Criar e enviar a tag

Só depois do merge na `main`:

```bash
git fetch origin --tags
git log -1 --format="%h %s" origin/main                             # tem de ser o merge do PR develop → main
git show origin/main:Directory.Build.props | grep -n "Version"      # versão na main = versão da tag
git ls-remote --tags origin "v<versão>"                             # vazio: a tag ainda não existe
git tag v<versão> origin/main
```

Antes de pedir o push, lembre o usuário de que ele publica no nuget.org e não tem volta. O usuário envia a tag com `! git push origin v<versão>`. Em seguida, crie o release com as notas (passo 5).

## 5. Notas da versão (Release do GitHub)

A tag sozinha não mostra o que mudou. Logo depois do push da tag, crie um **Release do GitHub** para ela, com as notas da versão. O release não dispara nenhum workflow, porque o `nuget-publish.yml` só reage ao push da tag.

1. Monte as notas a partir da seção `## [<versão>]` do `CHANGELOG.md` da `main` (`git show origin/main:CHANGELOG.md`). O texto abre com:
   - uma frase sobre a versão;
   - o `dotnet add package LeptonTech.SqlParser --version <versão>`;
   - o link `https://www.nuget.org/packages/LeptonTech.SqlParser/<versão>`;
   - o aviso de quebra de compatibilidade, quando houver.

   Em seguida vêm "Adicionado" e "Alterado". No fim, o link para o `CHANGELOG.md` completo.
2. **Links:**
   - **Caminhos relativos** do repositório (`docs/...`) não funcionam no release. Use `https://github.com/lepton-tech/lepton-sqlparser-csharp/blob/v<versão>/<caminho>`.
   - **Número solto** (`#2628`) vira link para um PR **deste** repositório. Para PRs do Rust, escreva `apache/datafusion-sqlparser-rs#2628`.
3. Grave as notas num arquivo no scratchpad. Monte a URL do formulário já preenchido com `[Uri]::EscapeDataString`:

   `https://github.com/lepton-tech/lepton-sqlparser-csharp/releases/new?tag=v<versão>&title=v<versão>&prerelease=1&body=<notas>`

   - o `prerelease=1` vale só para versões `preview`;
   - em versão estável, tire o parâmetro e deixe "Set as the latest release".
4. Abra a URL no Chrome. Confira, sem publicar ainda:
   - "Existing tag" com a tag certa;
   - o título;
   - o fim das notas;
   - o "Release label" (Pre-release ou latest).
5. Clique em "Publish release" numa chamada separada e confira a página `releases/tag/v<versão>`.
6. Release errado se corrige pelo lápis da página do release. **Nunca apague nem recrie a tag.**
## 6. Acompanhar a publicação

1. No GitHub, abra **Actions → "Publicação no nuget.org"** e a execução da tag:
   - o job **build** roda no Windows e confere tag × versão, `Pack.ps1` e `Test-Package.ps1`;
   - o job **publish** roda no ambiente `production`, que pode pedir aprovação.
2. Confira o pacote em `https://www.nuget.org/packages/LeptonTech.SqlParser/<versão>`. A versão leva alguns minutos para aparecer (cerca de 7 na `0.63.1-preview.4`).
3. Se o passo **"Tag × versão do pacote"** falhar, a tag não bate com o `Directory.Build.props` do commit marcado. Não mova a tag sem decisão do usuário; o normal é publicar uma versão nova.
4. Registre no diário (`docs/port/progresso.md`) a versão publicada e o número da execução.

## Criar PR sem `gh`

Use a skill `claude-in-chrome`.

1. Abra `https://github.com/lepton-tech/lepton-sqlparser-csharp/compare/<base>...<head>?expand=1`.
2. Confira na página `base:` e `compare:`, "Able to merge", o número de commits e de arquivos. Confira também se já existe PR aberto para o mesmo par de branches.
3. **Título:** `form_input` no campo "Title" (`input[name="pull_request[title]"]`).
4. **Descrição:**
   - clique na caixa visível (`#pull_request_body`) e digite com `computer` → `type`;
   - o `form_input` cai num campo oculto, então não use;
   - o editor do GitHub continua listas sozinho a cada Enter, então escreva a descrição em parágrafos, sem itens `- `;
   - termine com "🤖 Generated with [Claude Code](https://claude.com/claude-code)".
5. **Confira com um screenshot antes de criar.** Clique em "Create pull request" numa chamada separada, nunca na mesma leva do preenchimento: se o preenchimento falhar, o PR sai sem título e sem descrição.
   - A página tem dois botões "Create pull request", um oculto; clique no visível, por coordenadas de um screenshot recente.
   - A página rola sozinha ao focar a descrição. Se o clique não abrir o PR, confira antes que nenhum PR foi criado, para não duplicar.
6. **Se o PR saiu errado:** corrija o título pelo lápis ao lado dele ("Edit title") e a descrição pelo menu `...` do primeiro comentário ("Edit comment" → "Update comment").
