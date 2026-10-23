# Rodada 2 de sincronização: `dacdbc9` → `9119ec6`

Notas da execução da "Rodada 2" de [`../upstream-ledger.md`](../upstream-ledger.md): os 46 commits do `main` do sqlparser-rs depois de `dacdbc9`, até `9119ec6` (2026-10-23). Procedimento: skills `sync-upstream`, `port-rust-to-csharp` e `port-rust-tests`. Rodada anterior: [`rodada1-lotes1-3.md`](rodada1-lotes1-3.md), [`rodada1-lotes4-5.md`](rodada1-lotes4-5.md) e [`rodada1-lote6.md`](rodada1-lote6.md).

- **Data:** 2026-10-23.
- **Base:** cópia de trabalho do repositório (branch `feature/renomeia-leptontech`, árvore limpa): `LeptonTech.SqlParser.Core.Tests` 1.715 aprovados e 10 ignorados, `LeptonTech.SqlParser.Tests` 279, nos três alvos.
- **Leitura do Rust:** `git -C upstream/sqlparser-rs show <hash>` para cada diff; a árvore de cada commit que muda a AST ou as keywords foi extraída com `git archive <hash> src derive HEADER Cargo.toml` para uma pasta temporária e passada a `Generate-Visitor.ps1 -Upstream` e `Generate-Keywords.ps1 -Source`. O clone de referência só recebeu `fetch`.
- **Ordem:** cronológica estrita, um PR por vez, com build e testes (net8.0) depois de cada um; os três alvos no fim e em pontos de risco (#2623 e #2577 também em net48).
- **Resultado:** 37 PRs `portado` e 9 `não-aplicável`; `portedThroughCommit` = `9119ec6`. `baselineTag`, `baselineCommit` e os cabeçalhos continuam `v0.63.0` (não há tag nova).

## Por PR

A observação de cada PR (arquivos, decisões, testes) está no ledger. Aqui ficam só o que não cabe numa linha.

| PR | Nota |
|---|---|
| #2628, #2629, #2630, #2631, #2644 | Asserções exatas. Forma usada: `Assert.Equal(Assert.Throws<ParserException>(() => ..).Error, new ParserError.ParserErrorValue(".."))`, na ordem dos operandos do Rust (o `new` não é constante, então o xUnit2000 não se aplica); com mensagem (`assert_eq!(.., "{sql}")`), `TestUtils.AssertEqualWithMessage<ParserError>(..)`, com o tipo explícito porque o esperado é a variante. Laços `for (sql, expected) in [..]` viram `foreach (var (..) in new (string, string)[] { .. })`; quando `sql` já é local do método, o nome da tupla muda (`invalidSql`, caso 72). Alguns testes mudaram de forma no upstream e foram re-portados como estão agora: `parse_global_in` e `parse_tuple_element_access` (ClickHouse) passaram a exigir resultado idêntico em todos os dialetos; `parse_unterminated_escape` (MySQL) deixou o `catch_unwind`; `test_non_bmp_identifiers` (SQLite) passou a escrever U+DFFFE como escape, gerado por script (`\U000DFFFE`, nunca digitado). Nenhuma mensagem divergiu. |
| #2636 | `ZEROFILL` é lido depois do `match` do primeiro token de `ParseDataTypeHelper`; `ParseDataTypeNoMatch` (caso 140) não muda. |
| #2626 | Variante `InsertOverriding.System`: um record aninhado chamado `System` compila e não atrapalha as referências a `System.*` do arquivo. |
| #2575 | Na primeira tentativa, o utilitário de substituição tratou mal os blocos de remoção e corrompeu `CreateTable.cs` e `CreateTableBuilder.cs`; os dois foram restaurados do `HEAD` (`git show`, só leitura) e a mudança foi refeita. Ver "Ferramentas". |
| #2623 | O comentário `PORT-DEVIATION DIV-005` do `SetExpr.Fmt` virou o comentário padrão do caso 123. O teste novo roda numa `Thread` de 256 KB com `ExceptionDispatchInfo` para repassar falhas (o `spawn().join().unwrap()` do Rust); passa em net48. O `drop_chain_iteratively` do Rust foi portado como função local, embora o C# não tenha *drop glue*. |
| #2492 | Variante-tupla de 2 campos: `UsingAlias(Sequence<ObjectName> Columns, Ident Alias)`, nomes em `nomes.md`. |
| #2656 | `display_create_macro_propagates_argument_list_errors` usa um `IFmtWrite` cujo `WriteStr` devolve `false` ao receber `(` ou `)`; o `assert_eq!(write!(writer, "{statement}"), Err(fmt::Error))` vira `Assert.False(new Formatter(writer).Write($"{statement}"))`, com o mecanismo que entrou no #2577 (caso 141). |
| #2674 | `chars.peekable.clone().nth(1).is_some_and(..)` → `chars.Peekable.Nth(1) is ' ' or '!'` (caso 17). |
| #2577 | `first_char` precisa interromper a renderização (é o que deixa `display_nested_unary_minus_in_linear_time` linear; sem a interrupção, a renderização dobraria a cada nível). Como o `Write` do C# não devolvia valor, a primeira versão lançava uma exceção (`FmtError`) no destino: uma exceção por `-`/`!` prefixo no `ToString`. Decisão desta rodada, o mecanismo mais rápido e mais próximo do Rust: `IFmtWrite.WriteStr` e os métodos de escrita do `Formatter` devolvem `bool` (o `fmt::Result`); o erro fica guardado no `Formatter` raiz e descarta as escritas e lacunas seguintes, o que equivale ao `?` de todos os `Fmt`; `FirstCharWrite` devolve `false` depois do primeiro caractere. O braço `Minus`/`BangNot` traduz o `?` de `write!(f, "{op}")`, porque depois dele vem o `starts_with_operator_char`, que renderiza `expr` noutro destino: sem isso, o `FirstChar` de cada nível percorreria a cadeia inteira (tempo quadrático). Nenhuma exceção no `ToString`. `OPERATOR_CHARS` (constante de módulo de `tokenizer.rs`) → `Tokenizer.OperatorChars` (`internal const`). |
| #2689 | As três listas do PostgreSQL foram geradas do `.rs` por script e conferidas pela contagem (77, 43 e 22). `first_keyword` é calculado antes do `MaybeParseNoMatch`; o braço `DataType.Custom` ganhou a alternativa `firstKeyword is { } kw && parser.PeekCustomTypedString(kw, name)`. |
| #2679 | O tipo aninhado `Expr.In` passou a esconder o parâmetro posicional `In` de `Expr.Position` (CS8866); a propriedade foi declarada com `new`, como manda o caso 58. |
| #2671 | `DeclareSeparator` não tem `impl` no Rust (só `derive(Default)`), então é `enum` C# (§6, regra 1); o `#[default]` é o primeiro membro (`Semicolon`), que é o `default` do `enum`. |
| #2669 | O guarda `match kind { .. }` virou expressão `switch` com `_ => throw` (caso 98). |

## Visitor e keywords

- Visitor regenerado nos PRs que mudam a AST (#2636, #2626, #2617, #2575, #2492, #2691, #2679, #2671), sempre contra a árvore do próprio commit. Tipos novos visitáveis: `InsertOverriding`, `SqliteTableOption`, `SqliteTableOptions`, `InTarget`; `DeclareSeparator` é folha (`enum` C#, DIV-008). No fim, `Generate-Visitor.ps1 -Check -Upstream <9119ec6>`: atualizado (518 tipos visitáveis, 19 `enum` folha, 368 nós que alcançam ganchos).
- `Keywords.g.cs` regenerado no #2636 (`ZEROFILL`) e no #2626 (`OVERRIDING`) com o `keywords.rs` do commit; regenerado de novo com o de `9119ec6`: idêntico (1.106 keywords).

## Decisões

- **convencoes.md §13, caso 141:** `fmt::Result` → `bool` em `IFmtWrite.WriteStr` e nos métodos de escrita do `Formatter`, com o erro guardado no `Formatter` raiz no lugar do `?` dos `Fmt`; sem exceção. A §7 aponta para o caso.
- **nomes.md:** `JoinConstraint::UsingAlias`, `Parser.GeometricTypeKindOf` (o nome mecânico colidiria com o tipo `GeometricTypeKind`), `DisplayUtils.FirstCharWrite` (colidiria com o método `FirstChar`), `core::fmt::Error` → `false` e as classes de escopo dos testes novos.
- **divergencias.md:** DIV-005 (o `Display` de `SetExpr` é protegido no Rust desde o #2623) e DIV-008 (`DeclareSeparator`). Nenhuma divergência nova, nenhum `Skip` novo.
- **API pública:** sem mudança de código na camada `LeptonTech.SqlParser`; quebras e mudanças de comportamento no ADR-0009, "Impacto da Rodada 2", e no `CHANGELOG.md`, `0.63.1-preview.5` (versão publicada depois da rodada, em commit próprio).

## Verificações

- `dotnet build LeptonTech.SqlParser.sln -nologo -v q`: 0 avisos, 0 erros.
- `dotnet test LeptonTech.SqlParser.sln -nologo -v q`: `LeptonTech.SqlParser.Core.Tests` 1.757 aprovados e 10 ignorados; `LeptonTech.SqlParser.Tests` 279; em net48, net8.0 e net10.0.
- Contagem de `#[test]` entre `dacdbc9` e `9119ec6`: +42 (`comments.rs` +1, `spans.rs` +3, `display_utils.rs` +2, `sqlparser_clickhouse.rs` +4, `sqlparser_comments.rs` +3, `sqlparser_common.rs` +13, `sqlparser_duckdb.rs` +2, `sqlparser_mssql.rs` +1, `sqlparser_mysql.rs` +2, `sqlparser_postgres.rs` +6, `sqlparser_sqlite.rs` +5), igual ao aumento do C#.
- `pwsh tools/check/Find-SuspiciousChars.ps1`: as mesmas 14 linhas de antes em `src/` e `tests/`.

## Ferramentas (para a próxima rodada)

- As edições em bloco usaram um script descartável que troca trechos exatos (blocos "antigo"/"novo" por arquivo) preservando UTF-8 sem BOM e CRLF. Cuidados que apareceram:
  - um bloco de remoção precisa terminar com a quebra de linha da última linha removida; a primeira versão do script casava errado blocos de substituição vazia e corrompeu dois arquivos (#2575);
  - o *heredoc* do Bash deste ambiente colapsou `\\` em `\`; textos com barra invertida foram escritos pela ferramenta de escrita de arquivos ou montados com `[char]92`;
  - no PowerShell, `$out` e `$Out` são a mesma variável (nomes não diferenciam maiúsculas), o que quebrou um script que tinha o parâmetro `-Out`;
  - `sed -i` do Git Bash troca CRLF por LF: não usar nos `.cs`.
