# ADR-0004: Baseline do upstream e processo de sincronização

- **Status:** Aceita
- **Data:** 2026-10-14

## Contexto

O sqlparser-rs publica uma release a cada 1–4 meses, com 40–80 PRs cada, e muda a AST em quase todas as releases (ver a análise técnica, §3.3). Ports que não registram a versão de origem perdem o controle da defasagem: foi o que aconteceu com o SqlParser-cs. Outros projetos registram explicitamente o commit de origem: o iText (`port-hash`) e o TypeScript 7 (submódulo apontando para o commit portado).

## Decisão

1. **Baseline:** tag `v0.63.0` (commit `85b1a6f`), registrada em `UPSTREAM.json`.
   - O port inicial é feito contra essa tag.
   - Commits posteriores entram depois, pela skill `sync-upstream`.
2. **Fonte de referência local:** `tools/upstream/Get-Upstream.ps1` clona e posiciona `upstream/sqlparser-rs` (e `upstream/SqlParser-cs`) nos commits de `UPSTREAM.json`. A pasta `upstream/` não é versionada.
   - Ainda não há repositório Git do projeto, por isso não há submódulo. Ao criar o repositório, avaliar trocar por submódulo, como faz o typescript-go.
3. **Registro do que foi portado:**
   - `UPSTREAM.json` → `portedThroughCommit`;
   - ledger `docs/port/upstream-ledger.md`, com uma linha por PR, e nenhum PR sem registro;
   - cabeçalho de versão em cada arquivo portado.
4. **Sincronização release por release, PR a PR**, em ordem cronológica, citando PR e hash de origem.
5. **Sem pipeline por enquanto:** o repositório é local. CI (build, testes e um "rastreador" semanal do upstream) será configurada quando houver decisão sobre hospedagem (Bitbucket ou outra). Até lá, a verificação é manual pelos comandos da skill `port-rust-to-csharp`.

## Consequências

- A defasagem fica mensurável a qualquer momento: commits entre `portedThroughCommit` e `origin/main`.
- Os scripts de `tools/` precisam de `git` e `pwsh` na máquina de desenvolvimento.
