# Geradores de código (`tools/codegen/`)

Três partes do `Lepton.SqlParser.Core` não são traduzidas à mão: são **geradas** a partir de fontes oficiais. Assim, uma mudança no upstream nunca é "esquecida" e o resultado não depende de quem portou.

| Script | Gera | A partir de | Quando rodar |
|---|---|---|---|
| [`Generate-Keywords.ps1`](Generate-Keywords.ps1) | `src/Lepton.SqlParser.Core/Keywords.g.cs` (enum `Keyword`, `ALL_KEYWORDS`, listas `RESERVED_*`) | `upstream/sqlparser-rs/src/keywords.rs` | Quando o `keywords.rs` do Rust mudar (keyword nova ou lista reservada alterada) |
| [`Generate-Visitor.ps1`](Generate-Visitor.ps1) + [`VisitorGen/`](VisitorGen/) | `src/Lepton.SqlParser.Core/Ast/Visitor/Generated/*.g.cs` (o equivalente do `derive(Visit, VisitMut)`) | Os tipos da AST do próprio Core, conferidos contra `upstream/sqlparser-rs` | Sempre que um tipo da AST mudar (campo novo, tipo novo, `visit(with = ...)`) |
| [`Generate-CaseMapping.ps1`](Generate-CaseMapping.ps1) | `src/Lepton.SqlParser.Core/Support/RustCaseMapping.g.cs` (tabelas de `to_uppercase`/`to_lowercase` do Rust) | Unicode Character Database, na versão do `char::UNICODE_VERSION` do Rust | Quando a toolchain estável do Rust mudar a versão do Unicode |

Execute os comandos na raiz do repositório:

```powershell
pwsh tools/codegen/Generate-Keywords.ps1
pwsh tools/codegen/Generate-Visitor.ps1
pwsh tools/codegen/Generate-Visitor.ps1 -Check      # só confere; código de saída 1 se o gerado estiver desatualizado
pwsh tools/codegen/Generate-CaseMapping.ps1
```

Requisitos: PowerShell 7 e .NET SDK 10 (o do `global.json`). O `Generate-CaseMapping.ps1` também precisa de acesso à internet para baixar o UCD na primeira vez.

## Regras

- **Nunca edite um `*.g.cs` à mão.** A próxima geração apaga a edição. Se o gerado está errado, corrija o gerador ou a fonte.
- **Depois de gerar:** compile, rode os testes e revise o diff do gerado antes do commit:

  ```powershell
  dotnet build Lepton.SqlParser.sln -nologo -v q
  dotnet test Lepton.SqlParser.sln -nologo -v q
  ```

- **O clone de referência precisa estar na versão certa.** Os geradores de keywords e do visitor leem `upstream/sqlparser-rs`, que o [`Get-Upstream.ps1`](../upstream/Get-Upstream.ps1) posiciona no commit `portedThroughCommit` do [`UPSTREAM.json`](../../UPSTREAM.json). Se o clone estiver em outra versão, aponte o gerador para uma extração do commit certo (`-Source` no de keywords, `-Upstream` no do visitor), sem fazer checkout no clone compartilhado.
- **Na sincronização com o upstream**, a skill [`sync-upstream`](../../.claude/skills/sync-upstream/SKILL.md) roda os geradores quando o PR mexe em `keywords.rs` ou em tipos da AST.

## `Generate-Keywords.ps1`

O `keywords.rs` do Rust é declarativo (macro `define_keywords!` + listas `RESERVED_*`), então é gerado em vez de portado.

| Parâmetro | Padrão | Uso |
|---|---|---|
| `-Source` | `upstream/sqlparser-rs/src/keywords.rs` | Outro `keywords.rs`, por exemplo extraído de um commit específico com `git show <commit>:src/keywords.rs` |
| `-Output` | `src/Lepton.SqlParser.Core/Keywords.g.cs` | Arquivo de saída |

Uma keyword nova renumera o enum `Keyword`. O código-fonte não muda, mas código já compilado contra a versão anterior do pacote precisa ser recompilado; registre isso no `CHANGELOG.md`.

## `Generate-Visitor.ps1` e `VisitorGen/`

No Rust, cada tipo da AST tem `derive(Visit, VisitMut)`, e a macro gera a travessia: campos na ordem da declaração e ganchos `pre_`/`post_` de `visit(with = "...")`. Aqui, essa macro é o gerador. Decisão e formato: [ADR-0007](../../docs/adr/0007-visitor.md).

Como funciona:
1. Compila o `Lepton.SqlParser.Core` **sem** o código gerado (`-p:SqlParserVisitorCodegen=true`), numa pasta temporária. Por isso o gerador funciona mesmo quando o gerado está desatualizado ou não compila.
2. Roda o `VisitorGen` (console .NET fora da solução):
   - lê a AST por reflexão: a ordem dos campos é a do construtor primário e os ganchos vêm do atributo `[VisitWith]`;
   - confere tudo contra o código Rust: tipos com derive, nomes e ordem dos campos, anotações `visit(with)` e o `impl Visit` manual de `Ident`;
   - escreve um arquivo por pasta da AST.

   **Qualquer diferença com o Rust aborta a geração**, com a lista do que não bate.

| Parâmetro | Padrão | Uso |
|---|---|---|
| `-Check` | desligado | Só verifica se o gerado está atualizado (nada é escrito) |
| `-Upstream` | `upstream/sqlparser-rs` | Outra árvore Rust para a conferência, por exemplo uma extração do commit que está sendo portado |

Arquivos do `VisitorGen`:
- `Program.cs`: linha de comando;
- `CsModel.cs`: modelo da AST C# por reflexão;
- `RustModel.cs`: leitura dos tipos e anotações do Rust;
- `Analysis.cs`: conferência C# × Rust;
- `Emitter.cs`: escrita dos `.g.cs`.

Requisito da AST: **todo tipo da AST é `partial`**, para o gerado acrescentar `IVisit`/`IVisitMut`. Tipo novo sem `partial` é apontado pelo gerador.

## `Generate-CaseMapping.ps1`

Reproduz as tabelas de maiúsculas e minúsculas da biblioteca padrão do Rust, para que `RustString.ToUppercase`/`ToLowercase` deem o mesmo resultado do Rust (`ß` → `SS`, `ﬁ` → `FI`, sigma final) em net48, net8.0 e net10.0, sem depender da versão do Unicode de cada runtime. Contexto: DIV-001 e [D6](../../docs/port/onda-d/D6.md).

| Parâmetro | Padrão | Uso |
|---|---|---|
| `-UnicodeVersion` | `17.0.0` | Versão do UCD (a do `char::UNICODE_VERSION` da toolchain estável do Rust) |
| `-RustToolchain` | `1.99.0` | Só para o cabeçalho do gerado: a toolchain correspondente |
| `-UcdDir` | pasta temporária `sqlparser-ucd/<versão>` | Onde os arquivos do UCD ficam (baixados se não existirem) |
| `-Output` | `src/Lepton.SqlParser.Core/Support/RustCaseMapping.g.cs` | Arquivo de saída |

O gerado contém só números (code points em hexadecimal), sem caracteres literais nem sequências de escape. O próprio script confere a consistência das tabelas, e os testes `Div001Tests` confirmam o comportamento. Os dados do Unicode são distribuídos sob a licença em [`licenses/Unicode-LICENSE.txt`](../../licenses/Unicode-LICENSE.txt).
