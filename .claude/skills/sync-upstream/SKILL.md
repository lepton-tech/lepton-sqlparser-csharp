---
name: sync-upstream
description: Acompanhar e incorporar atualizações do sqlparser-rs (upstream Rust) no port C#. Ver o que mudou desde a versão portada, classificar commits/PRs, portar PR a PR e avançar o UPSTREAM.json. Use quando o usuário perguntar se há versão nova do sqlparser-rs, pedir para atualizar, sincronizar ou acompanhar o upstream, portar um PR específico (#NNNN) ou uma release (v0.64 etc.), ou quiser saber quão defasado o port está.
---

# Sincronizar com o upstream (sqlparser-rs)

**Objetivo:** levar o `SqlParser.Core` da versão registrada em `UPSTREAM.json` para uma versão mais nova do Rust, **sem pular nenhuma mudança em silêncio**.

**Modelo:** iText ("port-hash" + commits que citam a origem) e TypeScript 7 (port acompanhando o commit de referência). Ver `docs/port/referencias-e-boas-praticas.md` P1–P4 e o [ADR-0004](../../../docs/adr/0004-baseline-e-sincronizacao.md).

## Pré-condições

- Port do baseline concluído para as áreas afetadas. Sincronizar uma área ainda não portada é desnecessário: ela será portada direto da versão nova.
- Árvore verde (`dotnet test SqlParser.sln`).
- **Não use Git no repositório do projeto** (commit, branch) sem autorização do usuário. O Git em `upstream/sqlparser-rs` (só leitura: `fetch`, `log`, `diff`, `show`) pode ser usado à vontade.

## 1. Descobrir o que mudou

```powershell
pwsh tools/upstream/Get-Upstream.ps1                  # garante os clones; posiciona no commit portado
git -C upstream/sqlparser-rs fetch --tags origin
git -C upstream/sqlparser-rs tag --sort=-creatordate | Select-Object -First 5   # releases novas
git -C upstream/sqlparser-rs log --oneline <portedThroughCommit>..<alvo>        # alvo: tag (v0.64.0) ou origin/main
```

- Prefira sincronizar **release por release**: o alvo é a próxima tag `vX.Y.Z`.
- `changelog/X.Y.Z.md` lista os PRs da release. O rótulo "Breaking" quase não é usado, então **não confie nele**: quase toda release muda a AST.
- Todo commit do upstream é squash e termina em `(#NNNN)`, o número do PR.

## 2. Classificar cada commit

Para cada commit, veja os arquivos tocados:

```powershell
git -C upstream/sqlparser-rs show --stat --format="%h %s" <hash>
```

| Arquivos tocados | Área | Ação |
|---|---|---|
| `src/keywords.rs` | keywords | Rodar `pwsh tools/codegen/Generate-Keywords.ps1` (nunca editar `Keywords.g.cs` à mão) |
| `src/tokenizer.rs` | tokenizer | Portar o diff em `src/SqlParser.Core/Tokenizer/` |
| `src/dialect/*.rs` | dialetos | Portar. Atenção a flags `supports_*` novas ou renomeadas |
| `src/ast/**` | AST | Portar a mudança de tipo/campo/variante e ajustar o `Display`. Tipo novo é `partial`. Depois, rodar `pwsh tools/codegen/Generate-Visitor.ps1`: ele confere a AST C# contra o Rust (campos, ordem, `visit(with)`) e regenera o visitor |
| `src/ast/visitor.rs`, `derive/src/visit.rs` | visitor | Portar a mudança em `Ast/Visitor/*.cs` (à mão) ou no gerador `tools/codegen/VisitorGen` (derive) e regenerar (`docs/adr/0007-visitor.md`) |
| `src/parser/**` | parser | Portar o diff da(s) função(ões) |
| só `tests/**` | testes | Portar os testes novos (skill `port-rust-tests`) |
| `docs/`, `.github/`, `Cargo.toml`, `changelog/`, `fuzz/` | infra | Normalmente "não aplicável". Registrar mesmo assim |

## 3. Portar PR a PR

Para cada commit, **em ordem cronológica**:

1. `git -C upstream/sqlparser-rs show <hash>`: leia o diff inteiro, código **e testes**.
2. Porte com a skill `port-rust-to-csharp`, aplicando o diff no arquivo C# correspondente. A estrutura 1:1 é o que torna isso direto.
3. Porte os testes do commit (skill `port-rust-tests`). Eles entram no fim do arquivo de teste, como no Rust.
4. Build e testes verdes em net48/net8.0/net10.0.
5. Registre no ledger, `docs/port/upstream-ledger.md`, com uma linha por PR:

   | PR | Commit | Título | Área | Status | Observação |
   |---|---|---|---|---|---|
   | #2598 | `dacdbc9` | ClickHouse: Support GLOBAL IN / GLOBAL NOT IN | parser, ast | portado / não-aplicável / pendente | … |

   **Nenhum PR fica sem linha.** "Não-aplicável" exige motivo.

## 4. Fechar a release

Quando todos os PRs até a tag-alvo estiverem `portado` ou `não-aplicável`:

1. Atualize `UPSTREAM.json`: `portedThroughCommit` com o hash da tag e, se for o novo baseline, `baselineTag`/`baselineCommit`.
2. Atualize a versão nos cabeçalhos dos arquivos tocados (`Portado para C# a partir de sqlparser-rs vX.Y.Z, ...`).
3. Atualize `docs/port/progresso.md` (diário) e `docs/port/divergencias.md` (algo resolvido ou novo).
4. Rode `pwsh tools/upstream/Get-Upstream.ps1` para confirmar que o clone de referência está no commit registrado.
5. Confira o `char::UNICODE_VERSION` da toolchain estável do Rust (página `std::char::UNICODE_VERSION` da documentação). Se mudou desde a última geração (`RustCaseMapping.UnicodeVersion`), regenere as tabelas de maiúsculas/minúsculas com `pwsh tools/codegen/Generate-CaseMapping.ps1 -UnicodeVersion X.Y.Z -RustToolchain 1.NN.0` e rode os testes (`docs/port/onda-d/D6.md`; convencoes.md §13, caso 139).
6. Versão do pacote ([ADR-0010](../../../docs/adr/0010-empacotamento.md)): ao chegar a uma tag nova do Rust (ex.: `v0.64.0`), passe `VersionPrefix` em `Directory.Build.props` para a linha nova (`0.64.x`); sem tag nova, avance o último número ou o `VersionSuffix`. Registre a versão no `CHANGELOG.md` e gere o pacote com `pwsh tools/pack/Pack.ps1` e `pwsh tools/pack/Test-Package.ps1`.

## Relatório "quão defasado estamos?"

Sem portar nada, responda com:
- o número de commits e as releases entre `portedThroughCommit` e `origin/main`;
- a contagem de commits por área (tabela da seção 2);
- os PRs com mudança em `src/ast/` (os mais caros de portar).
