# ADR-0012: Renome para LeptonTech.SqlParser

- **Status:** Aceita
- **Data:** 2026-10-19
- **Decidido por:** responsável pelo projeto ("vamos trocar para LeptonTech em todos os lugares")
- **Substitui:** os nomes do [ADR-0011](0011-renome-lepton-sqlparser.md)

## Contexto

- A `0.63.1-preview.3` ([ADR-0011](0011-renome-lepton-sqlparser.md)) chamava o pacote, os assemblies e os namespaces de `Lepton.SqlParser`.
- A primeira publicação pelo `nuget-publish.yml` ocorreu com a tag `v0.63.1-preview.3`, na execução 39272014516 do GitHub Actions. Passaram:
  - o build;
  - os testes nos três alvos;
  - o pacote e o teste dele em apps net48 e net8.0;
  - o login por Trusted Publishing.
- O `dotnet nuget push`, porém, recebeu:

  ```
  409 (The package ID is reserved. You can upload your package with a different package ID. Reach out to support@nuget.org if you have questions.)
  ```

- O pacote estava correto: `<id>Lepton.SqlParser</id>` no `.nuspec` e um `.nupkg` só, com as duas DLLs. Um ID malformado daria 400. O 409 com essa mensagem é a resposta do nuget.org quando o ID cai num prefixo reservado (*ID prefix reservation*) de outra conta.
- Não há no nuget.org nenhum pacote cujo ID comece com `Lepton.`. Isso foi conferido de três formas:
  - na busca;
  - na lista de IDs por prefixo (`/api/v2/package-ids?partialId=Lepton`);
  - nos endereços diretos de vários IDs, inclusive de pacotes não listados.

  O nuget.org não mostra quem é o dono de uma reserva.

## Decisão

Trocar `Lepton.SqlParser` por `LeptonTech.SqlParser` em tudo o que o ADR-0011 renomeou: pacote, assemblies, namespaces, projetos, pastas, solução e benchmark. A versão passa a ser `0.63.1-preview.4`.

| Antes (ADR-0011, `0.63.1-preview.3`) | Depois |
|---|---|
| Pacote `Lepton.SqlParser` | `LeptonTech.SqlParser` |
| `src/Lepton.SqlParser/Lepton.SqlParser.csproj`: `Lepton.SqlParser.dll`, namespace `Lepton.SqlParser` | `src/LeptonTech.SqlParser/LeptonTech.SqlParser.csproj`: `LeptonTech.SqlParser.dll`, `LeptonTech.SqlParser` |
| `src/Lepton.SqlParser.Core/Lepton.SqlParser.Core.csproj`: `Lepton.SqlParser.Core.dll`; namespaces `Lepton.SqlParser.Core`, `Lepton.SqlParser.Core.Ast`, `Lepton.SqlParser.Core.Dialects` | `src/LeptonTech.SqlParser.Core/LeptonTech.SqlParser.Core.csproj`: `LeptonTech.SqlParser.Core.dll`; `LeptonTech.SqlParser.Core`, `LeptonTech.SqlParser.Core.Ast`, `LeptonTech.SqlParser.Core.Dialects` |
| `tests/Lepton.SqlParser.Core.Tests/` e `tests/Lepton.SqlParser.Tests/` | `tests/LeptonTech.SqlParser.Core.Tests/` e `tests/LeptonTech.SqlParser.Tests/` |
| `InternalsVisibleTo` para `Lepton.SqlParser.Tests` e `Lepton.SqlParser.Core.Tests` | `LeptonTech.SqlParser.Tests` e `LeptonTech.SqlParser.Core.Tests` |
| `Lepton.SqlParser.sln` | `LeptonTech.SqlParser.sln` |
| `tools/bench/Lepton.SqlParser.Bench/` (cópias do SqlParser-cs em `%TEMP%/Lepton.SqlParser.Bench/`) | `tools/bench/LeptonTech.SqlParser.Bench/` (`%TEMP%/LeptonTech.SqlParser.Bench/`) |
| `tests/PackageSmoke/nuget.config`: fonte `lepton-local`, padrão `Lepton.*`; propriedade `LeptonSqlParserVersion` | Fonte `leptontech-local`, padrão `LeptonTech.*`; propriedade `LeptonTechSqlParserVersion` |
| Versão `0.63.1-preview.3` | `0.63.1-preview.4`. A tag `v0.63.1-preview.3` já existe no GitHub, no merge `7fee505`; uma versão nova evita mover a tag |

**O que não muda:**
- O que o ADR-0011 já mantinha:
  - os nomes dos tipos;
  - as classes de teste com o nome do arquivo Rust;
  - as propriedades e os metadados do MSBuild com prefixo `SqlParser`;
  - `Authors` e `Company` ("Lepton TI");
  - tudo o que se refere ao SqlParser-cs.
- O repositório: `lepton-tech/lepton-sqlparser-csharp`.
- A política de Trusted Publishing no nuget.org: o padrão `Lepton*` cobre `LeptonTech.SqlParser`, e o nome do workflow e o ambiente continuam os mesmos.

**Como foi feito:**
- A troca de texto foi `Lepton.SqlParser` → `LeptonTech.SqlParser` e `LeptonSqlParserVersion` → `LeptonTechSqlParserVersion`, em todos os arquivos versionados. Ficaram de fora o `upstream/` e o histórico (veja em Consequências).
- As pastas e os arquivos de projeto foram renomeados.
- Os geradores (`Generate-Keywords.ps1`, `Generate-CaseMapping.ps1` e `Generate-Visitor.ps1 -Check`) rodaram de novo e confirmaram os arquivos gerados com o namespace novo.

## Consequências

- **Quebra para consumidores.** Nenhuma versão chegou ao nuget.org, então só é afetado quem usou o pacote local `Lepton.SqlParser`. Basta trocar o `PackageReference` e os `using` (`Lepton.SqlParser*` → `LeptonTech.SqlParser*`) e recompilar. Os tipos, a AST, o comportamento e o SQL gerado não mudam.
- **Risco que continua.** Não dá para saber antes do envio se `LeptonTech.SqlParser` está livre, porque o nuget.org não expõe as reservas. Se a reserva for do prefixo `Lepton` sem ponto, que cobre todo ID começado por `Lepton`, o 409 se repete. Nesse caso, há dois caminhos:
  - descobrir o dono da reserva: o responsável confere logado no nuget.org, ou se pergunta ao support@nuget.org;
  - adotar um nome fora desse prefixo, o que exige também mudar o padrão `Lepton*` da política.
- **Clones e branches.** As pastas mudaram: apague os `bin/` e `obj/` antigos.
- **Documentação.** Foi atualizada onde descreve o estado atual. Ficam com os nomes da época as notas históricas, as entradas antigas do diário (`docs/port/progresso.md`), o `CHANGELOG.md` e o ADR-0011.

## Alternativas rejeitadas

- **Trocar só o ID do pacote** e manter os namespaces `Lepton.SqlParser`: o nome do pacote ficaria diferente do nome do código, e o responsável pediu a troca em todos os lugares.
- **Esperar a liberação do prefixo `Lepton`:** depende do dono desconhecido da reserva ou do suporte do nuget.org, sem prazo.
- **Manter a `0.63.1-preview.3`**, apagando e recriando a tag: obrigaria a mover uma tag que já está no GitHub. A `0.63.1-preview.4` evita isso.
