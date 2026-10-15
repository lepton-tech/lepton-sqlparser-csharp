# CLAUDE.md: instruções para agentes neste repositório

Port em C# (`netstandard2.0`) do parser SQL **apache/datafusion-sqlparser-rs** (Rust), com paridade funcional e acompanhamento das versões novas do Rust.

## Comece aqui (toda sessão)

1. Leia `docs/port/progresso.md`: estado atual, fase e diário da última sessão.
2. Rode `dotnet test SqlParser.sln -nologo -v q`. Só comece trabalho novo com a árvore verde.
3. Para portar código, use a skill **`port-rust-to-csharp`**. Para testes, **`port-rust-tests`**. Para atualizar a partir do Rust, **`sync-upstream`**.
4. Ao terminar, atualize o diário em `docs/port/progresso.md`.

## Regras que não podem ser quebradas

- Comunicação e documentação em **pt-BR**, com acentuação correta. Identificadores de código seguem as convenções.
- **Nada de Git** (init, commit, branch, push) sem autorização explícita do usuário.
- **`SqlParser.Core` = port fiel do Rust:** nomes, estrutura, ordem e comportamento iguais aos do Rust. Regras normativas em `docs/port/convencoes.md`. Adaptações e conveniências vão para o projeto **`SqlParser`** (API pública). Ver `docs/adr/0002-arquitetura-em-camadas-e-api.md`.
- O código Rust de referência fica em `upstream/sqlparser-rs`, na versão de `UPSTREAM.json`. Se não existir, rode `pwsh tools/upstream/Get-Upstream.ps1`. **Não edite nada em `upstream/`.**
- `src/SqlParser.Core/Keywords.g.cs` é gerado: rode `pwsh tools/codegen/Generate-Keywords.ps1`, nunca edite à mão.
- `src/SqlParser.Core/Support/RustCaseMapping.g.cs` (tabelas de `to_uppercase`/`to_lowercase` do Rust) é gerado: rode `pwsh tools/codegen/Generate-CaseMapping.ps1`, nunca edite à mão (`docs/port/onda-d/D6.md`).
- `src/SqlParser.Core/Ast/Visitor/Generated/*.g.cs` (o derive `Visit`/`VisitMut`) é gerado: todo tipo da AST é `partial`; depois de mudar a AST, rode `pwsh tools/codegen/Generate-Visitor.ps1` (`docs/adr/0007-visitor.md`).
- Testes portados do Rust **nunca** são editados para passar. Use `Skip = "DIV-NNN: ..."` (divergência registrada em `docs/port/divergencias.md`) ou `Skip = "PENDENTE: ..."`.
- Decisão de tradução nova → registrar em `docs/port/convencoes.md` §13 ou `docs/port/nomes.md` **na mesma mudança**.
- `netstandard2.0`: sem default interface members, sem `Span<T>`, sem APIs exclusivas do .NET Core (`convencoes.md` §11; `docs/adr/0006-versao-da-linguagem-csharp.md`).

## Comandos

```bash
dotnet build SqlParser.sln -nologo -v q                                        # TreatWarningsAsErrors
dotnet test SqlParser.sln -nologo -v q                                         # net48 + net8.0 + net10.0
dotnet test SqlParser.sln --filter "FullyQualifiedName~TokenizerTests" -nologo -v q
pwsh tools/upstream/Get-Upstream.ps1                                           # baixa/posiciona referências
pwsh tools/codegen/Generate-Keywords.ps1                                       # regenera keywords
pwsh tools/codegen/Generate-Visitor.ps1                                        # regenera o visitor (Ast/Visitor/Generated) após mudar a AST
pwsh tools/codegen/Generate-Visitor.ps1 -Check                                 # só confere se o visitor gerado está atualizado
pwsh tools/codegen/Generate-CaseMapping.ps1                                    # regenera as tabelas de maiúsculas/minúsculas do Rust (Unicode do char::UNICODE_VERSION)
pwsh tools/check/Find-SuspiciousChars.ps1                                      # escapes \uXXXX viraram caractere literal?
```

No shell Bash deste ambiente, use caminhos absolutos em vez de `cd`.

## Mapa da documentação

| Documento | Para quê |
|---|---|
| `docs/analise-tecnica-e-plano.md` | Análise comparativa e plano em fases |
| `docs/adr/` | Decisões de arquitetura (abordagem e Plano B, camadas, licença, upstream, testes, versão C#, visitor gerado) |
| `docs/port/convencoes.md` | **Normativo:** como traduzir cada construto Rust |
| `docs/port/nomes.md` | Glossário de nomes não mecânicos |
| `docs/port/divergencias.md` | Incompatibilidades conhecidas (`DIV-NNN`) |
| `docs/port/progresso.md` | Estado do port + diário de sessões |
| `docs/port/upstream-ledger.md` | Um registro por PR do upstream após o baseline |
| `docs/port/referencias-e-boas-praticas.md` | Fontes externas que fundamentam o processo |
| `docs/plano-b/` | Rota alternativa (SqlParser-cs em `netstandard2.0`) |
