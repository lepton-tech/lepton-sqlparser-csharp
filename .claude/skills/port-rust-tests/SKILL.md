---
name: port-rust-tests
description: "Como portar testes do sqlparser-rs (Rust: atributos de teste, verified_stmt, one_statement_parses_to, TestedDialects, assert_eq!) para testes xUnit no SqlParser.Core.Tests, mantendo nomes e casos idênticos. Use sempre que portar código do parser/tokenizer/dialetos (todo código portado precisa dos testes correspondentes), ao portar arquivos tests/sqlparser_*.rs ou blocos de teste cfg(test) mod tests, ou quando um teste portado falhar e for preciso decidir entre corrigir o código ou registrar divergência."
---

# Portar testes Rust → xUnit

Os testes portados do Rust são a **especificação executável** do Core. A regra central: **o caso de teste é o do Rust, sem edição**. Se o teste C# falha, quem está errado é o código C#. A única outra possibilidade é uma divergência deliberada, registrada.

## Onde colocar

| Rust | C# (`tests/SqlParser.Core.Tests/`) |
|---|---|
| `#[cfg(test)] mod tests` em `src/tokenizer.rs` | `Tokenizer/TokenizerTests.cs` (classe `TokenizerTests`) |
| `#[cfg(test)] mod tests` em `src/<x>.rs` | `<Area>/<X>Tests.cs` |
| `tests/sqlparser_common.rs` | `Integration/SqlParserCommonTests1.cs` … `5.cs`: uma **classe própria por fatia** (não `partial`), na ordem do original, com cópia dos auxiliares do arquivo (decisão da onda C: fatias portadas em paralelo nunca conflitam) |
| `tests/sqlparser_<dialeto>.rs` | `Integration/SqlParser<Dialeto>Tests.cs` (arquivo grande: `SqlParser<Dialeto>Tests1.cs`, `2.cs`…, uma classe por fatia, como acima) |
| `tests/queries/**` (lidos por `include_str!`) | `Queries/**`, copiados sem alteração e marcados como `Content` no `.csproj` |
| `src/test_utils.rs` (`TestedDialects`, `all_dialects()`…) | `TestUtils/TestedDialects.cs` etc. (portar sob demanda, com as mesmas regras do código) |

Mantenha a **ordem** dos testes igual à do arquivo Rust. O upstream acrescenta testes novos no fim dos arquivos; mantendo a ordem, o diff de sincronização fica trivial.

## Tradução

| Rust | C# |
|---|---|
| `#[test] fn tokenize_select_1()` | `[Fact] public void tokenize_select_1()` (**nome idêntico, em snake_case**) |
| `#[should_panic(expected = "msg")]` | `var ex = Assert.ThrowsAny<Exception>(...); Assert.Contains("msg", ex.Message);` |
| `assert_eq!(a, b)` | `Assert.Equal(a, b)` (mesma ordem do Rust) |
| `assert_ne!(a, b)` | `Assert.NotEqual(a, b)` |
| `assert!(cond)` | `Assert.True(cond)` |
| `assert!(matches!(x, Pat))` | `Assert.IsType<...>(x)` ou `Assert.True(x is ...)` |
| `vec![Token::Comma, ...]` | `new List<Token> { new Token.Comma(), ... }` |
| `.unwrap()` num resultado esperado OK | Chamada direta (se lançar, o teste falha) |
| `.unwrap_err()` / `assert_eq!(Err(e), res)` | `var ex = Assert.Throws<TokenizerException>(() => ...); Assert.Equal(e, ex.Error);` |
| `r#"..."#` (raw string) | `"""..."""` (raw string literal C# 11) ou `@"..."` com aspas dobradas |
| Helper local `fn compare(...)` dentro do `mod tests` | Método `private static` na mesma classe, mesmo nome em PascalCase |
| Helpers de `test_utils.rs` (`verified_stmt`, `all_dialects()`, …) | Métodos em `TestUtils/`, com nome PascalCase (`VerifiedStmt`, `AllDialects()`) e comportamento idêntico ao Rust |

### Padrões dos testes de integração (`tests/sqlparser_*.rs`)

A API de teste já portada fica em `tests/SqlParser.Core.Tests/TestUtils/` (`TestedDialects`, `TestUtils`). Use-a sempre.

As tabelas abaixo consolidam as decisões de tradução de testes das ondas B e C (notas `docs/port/onda-b/B7.md` e `docs/port/onda-c/C*.md`). Linhas marcadas **CONFLITO — decidir** registram práticas divergentes entre unidades e aguardam decisão do responsável; até lá, siga a forma já usada no arquivo em que estiver trabalhando. Auxiliares inventados nos testes (`Unreachable`, `Unwrap`, `SpawnAndRecvTimeout`...) estão em `docs/port/nomes.md`, seção "Testes": reutilize os nomes.

**Estrutura e auxiliares**

| Rust | C# |
|---|---|
| Arquivo de teste | Cabeçalho ASF + linha de origem, como no código (`convencoes.md` §3). Os `.cs` ficam em CRLF (`.editorconfig`) |
| Fatia de um arquivo Rust grande | Classe própria por fatia, **não** `partial` (ver "Onde colocar") |
| Funções-atalho do arquivo (`fn verified_stmt(sql) -> Statement { all_dialects().verified_stmt(sql) }`, `fn pg() -> TestedDialects { .. }`, `fn column_def(..)`) | Métodos `private static` da classe de teste, com nome mecânico (`VerifiedStmt`, `Pg()`, `ColumnDef(..)`), copiados em cada classe que os usa: na posição do Rust quando a função está no trecho; senão no topo ou no fim da classe, com a linha de origem em comentário. Um método com o nome de um tipo da AST compila (`ColumnDef(..)` × `new ColumnDef(..)`); se o tipo ficar ambíguo, qualifique-o (`Ast.QuoteDelimitedString`) |
| Funções de `test_utils` com nome igual a um tipo da AST (`index_column` × `Ast.IndexColumn`, `join` × `Ast.Join`, `table_alias`) | Chame sempre qualificado, `TestUtils.IndexColumn(..)`, sem `using static` |
| `all_dialects().verified_stmt(sql)` | `TestUtils.AllDialects().VerifiedStmt(sql)` |
| `all_dialects_where(\|d\| d.supports_x())` | `TestUtils.AllDialectsWhere(d => d.SupportsX)` |
| `TestedDialects::new(vec![Box::new(GenericDialect {}), ..])` | `new TestedDialects(new List<Dialect> { new GenericDialect(), .. })` |
| `TestedDialects::new_with_options(.., ParserOptions::new().with_trailing_commas(true))` | `TestedDialects.NewWithOptions(.., new ParserOptions().WithTrailingCommas(true))` |
| `one_statement_parses_to(sql, canonical)` / `verified_query` / `verified_only_select` / `verified_expr` / `expr_parses_to` | `OneStatementParsesTo` / `VerifiedQuery` / `VerifiedOnlySelect` / `VerifiedExpr` / `ExprParsesTo` |
| `fn` aninhada ou closure dentro do teste | Função local `static` com nome mecânico (`check_one` → `CheckOne`); closure guardada em variável → `Func<..>` local com o nome Rust (`let div = \|l, r\| ..` → `Func<Expr, Expr, Expr> div`). O `///` de uma função local vira `//` (função local não aceita XML doc) |
| `macro_rules!` local (`test_parse_data_type!`) e macros de teste (`nest!`, `tpch_tests!`) | Método `private static` PascalCase logo antes do teste que o usa (`TestParseDataType(..)`, `Nest(..)`). Macro que gera testes → um `[Fact]` por teste gerado, com os nomes que a macro gera (`tpch_1`..`tpch_22`) |
| Submódulo `mod test_x { .. }` dentro de `mod tests` | Achatado na mesma classe e posição, delimitado por comentários; nomes dos testes inalterados |
| `use Enum::*;` dentro do teste | Nomes qualificados (`new MatchRecognizePattern.Repetition(..)`), sem `using static` nem aliases |
| `struct` só de dados declarado no teste (`struct TestCase { sql, expected_owner }`) | Array ou `List` de tuplas nomeadas com os nomes dos campos (`(string Sql, Owner ExpectedOwner)[]`) |
| `struct` que implementa `Dialect` dentro de funções de teste (`struct MyDialect`) | `private static class <NomeDoTestePascal> { internal sealed class MyDialect : Dialect { .. } }`, mantendo o nome e o escopo. Funções livres do arquivo chamadas dentro do dialeto vão **qualificadas** (`SqlParserCustomDialectTests.IsIdentifierStart(ch)`); sem isso o nome resolve para o próprio override (recursão infinita) |
| `struct WrappedDialect(MySqlDialect)` / `derive_dialect!(Novo, Base, overrides = { .. })` | Classe privada aninhada que **herda** da base e sobrescreve ou repassa os membros (`convencoes.md` §13, caso 8; DIV-002), com a macro original em comentário. `metodo = true` → `public override bool Metodo => true;` |
| `#[ignore = "motivo"]` | `[Fact(Skip = "#[ignore] no Rust: motivo")]`, com o corpo portado por inteiro. Não é `DIV`: o upstream também não executa o teste |
| `#[cfg(feature = "bigdecimal")]` | Não portado (comentário na posição; DIV-006). A variante `#[cfg(not(feature = "bigdecimal"))]` é portada e roda sempre. `#![cfg(not(feature = "std"))]` não é traduzido (os testes rodam sempre) |
| `#[cfg(feature = "visitor")]` (enquanto o visitor não existir) | `Skip = "PENDENTE: depende do visitor ..."`, com o corpo traduzido em comentário |
| `include_str!("queries/tpch/N.sql")` | `File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Queries", "tpch", "N.sql"), Encoding.UTF8)`, com os arquivos copiados sem alteração e `<Content Include=".." CopyToOutputDirectory="PreserveNewest" />` no `.csproj` |
| `thread::spawn` + `mpsc::channel` + `rx.recv_timeout(d).expect(msg)` (testes de explosão exponencial) | Auxiliar `SpawnAndRecvTimeout(body, timeout, msg)`: thread com **2 MiB de pilha** (o padrão de `std::thread::spawn`); exceção no corpo → falha com `msg: Disconnected`; prazo vencido → `msg: Timeout`, como o `expect` do Rust. `thread::Builder::new().stack_size(n)` → `stackSize: n`. Valor enviado pela thread (`tx.send(result.is_ok())`) → local capturado pelo corpo e verificado depois |
| `println!("{}", x)` / `dbg!(..)` | `Console.WriteLine(x)` / não traduzido |
| `///` sobre um teste; comentários `//` do Rust | XML doc (`<summary>`, `<remarks>`) em inglês verbatim; comentários mantidos em inglês, na mesma posição |

**Asserções** (os analisadores do xUnit viram erro com `TreatWarningsAsErrors`)

| Rust | C# |
|---|---|
| `assert_eq!(a, b)` | `Assert.Equal(a, b)`, na ordem do Rust |
| `assert_eq!(valor, literal)` | `Assert.Equal(literal, valor)`: literal ou constante (inclusive membro de `enum` C#) vai na posição "expected" (xUnit2000). É a única exceção à regra "mesma ordem do Rust" |
| `assert_eq!(x, true)` / `assert_eq!(x, false)` / `assert!(!x)` | `Assert.True(x)` / `Assert.False(x)` (xUnit2004) / `Assert.False(x)` |
| `assert_eq!(x, Some(true))` com `x: Option<bool>` | `Assert.True(x)` (sobrecarga `bool?`, que falha com `null`) |
| `assert_eq!(x, None)` / `assert!(x.is_none())` / `assert!(x.is_some())` | `Assert.Null(x)` (xUnit2003) / `Assert.Null(x)` / `Assert.NotNull(x)` |
| `assert_eq!(x, Some(y))` | `Assert.Equal(y, x)` (o `null` do C# já representa `None`); com tipo-valor anulável, tipo explícito (`Assert.Equal<ulong?>(5, x)`); `x.map(\|p\| p.to_string())` comparado a `Some("..")` → `Assert.Equal("..", x?.ToString())` |
| `assert!(v.is_empty())` | `Assert.True(v.IsEmpty)` |
| `assert_eq!(1, v.len())` | `Assert.Single(v)` (xUnit2013), ou `#pragma warning disable xUnit2013` no arquivo, como em `AstModTests` |
| `assert!(v.contains(&x))` | `Assert.Contains(x, v)` (a forma `Assert.True(v.Contains(x))` dispara o xUnit2017) |
| `assert!(v.iter().any(\|c\| matches!(c, P)))` | `Assert.Contains(v, c => c is P)` (xUnit2012) |
| `assert!(s.contains(m))` / `assert!(s.ends_with(m))` sobre texto | `Assert.Contains(m, s)` / `Assert.EndsWith(m, s, StringComparison.Ordinal)` (xUnit2009; `Ordinal` porque o padrão do xUnit depende da cultura) |
| `assert!(matches!(x, P))` / `assert_matches!(x, P)` | `Assert.True(x is P)` com padrões de propriedade estendidos (`Expr.UnaryOp { Op: UnaryOperator.Not }`); com guarda, `Assert.True(x is P && guarda)`; quando o padrão é só a variante, `Assert.IsType<P>(x)` |
| `matches!(..)` solto, sem `assert!` (o Rust descarta o resultado) | `_ = <expressão>;` com comentário: não verifica nada, como no Rust |
| Esperado é uma variante e o obtido é a base (ou `Sequence<T>?`) | Tipo explícito: `Assert.Equal<Statement>(..)`, `Assert.Equal<Sequence<SelectItem>?>(..)`; `Sequence.Of<TBase>(..)` quando os elementos são variantes; ternário entre variantes com cast para a base |
| `Some(vec![..])` comparado a `Option<Vec<T>>` | Cast explícito para `Sequence<T>?`, para distinguir `None` de `Some(vec![])` |
| `assert_eq!(&[..], &args[..])` (fatias) | Arrays C# comparados elemento a elemento (`new FunctionArg[] { .. }` × `list.Args.ToArray()`) |
| `assert_eq!(a, b, "{msg}")` (mensagem extra) | `TestUtils.AssertEqualWithMessage(a, b, msg)`: mostra a mensagem do Rust **e** a diferença do `Assert.Equal` (decidido pelo integrador em 2026-10-14). As formas antigas (omitir a mensagem, nas C15/C16; `Assert.True(a == b, msg)`, nas C5/C6/C13; `try`/`catch` + `Assert.Fail`, na C2) são equivalentes e ficam como estão |
| `pretty_assertions::assert_eq!` | `Assert.Equal` |

**Erros, pânicos e `unwrap`**

| Rust | C# |
|---|---|
| `let res = parse_sql_statements(sql); assert_eq!(ParserError::ParserError("msg".to_string()), res.unwrap_err());` | `var ex = Assert.Throws<ParserException>(() => ParseSqlStatements(sql)); Assert.Equal(new ParserError.ParserErrorValue("msg"), ex.Error);` (ordem dos operandos do Rust) |
| `assert!(parse_sql_statements(sql).is_err())` / `.expect_err(msg)` / `.unwrap_err()` sem comparação | `Assert.Throws<ParserException>(() => ParseSqlStatements(sql));` (a mensagem do `expect_err` fica em comentário) |
| `assert!(matches!(res, Err(ParserError::ParserError(_))))` | `Assert.IsType<ParserError.ParserErrorValue>(Assert.Throws<ParserException>(..).Error)` |
| `res.unwrap_err().to_string()` (e `.contains(m)`) | `Assert.Throws<ParserException>(..).Error.ToString()` (o `Display`, com o prefixo `sql parser error: `); `Assert.Contains(m, ..)` |
| `Parser::parse_sql(&d, sql).unwrap_err()` | `Assert.Throws<ParserException>(() => Parser.ParseSql(d, sql)).Error` |
| `assert_eq!(result, Err(e))` com `try_with_sql(..).unwrap()` antes | Crie o parser **fora** do `Assert.Throws`, para um erro do tokenizer não se confundir com o esperado |
| `let ParserError::ParserError(msg) = .. else { panic!(..) }` | `Assert.IsType<ParserError.ParserErrorValue>(ex.Error).Value` |
| `parsed.err()` | Função local que devolve `ParserError?` (`null` = `None`) |
| `assert!(x.is_ok())` / `.unwrap()` / `Ok(_) => {}` num resultado esperado OK; `.expect(msg)` sobre `Result` | Chamada direta (se lançar, o teste falha); o `expect` fica em comentário |
| `std::panic::catch_unwind(\|\| ..)` + `assert!(result.is_err())` | `Assert.ThrowsAny<Exception>(..)` |
| `#[should_panic]` / `#[should_panic(expected = "msg")]` | `var ex = Assert.ThrowsAny<Exception>(..); Assert.Contains("msg", ex.Message);`, com o atributo Rust em comentário |
| `let _ = Parser::parse_sql(..)` (resultado descartado) | Chamada dentro de `try { .. } catch (ParserException) { }` |
| `unreachable!()` / `unreachable!(msg)` | `throw new InvalidOperationException("internal error: entered unreachable code")` (com `: msg` no fim, quando houver), como no código (`convencoes.md` §8). Pode usar a constante `Unreachable` |
| `panic!(msg)` no corpo de um teste | `throw new InvalidOperationException(msg)` (**preferido**, pela coerência com §8) ou `Assert.Fail(msg)`: os dois reprovam o teste. Em contexto de expressão (`switch` expression, `?? throw`) só cabe o `throw`. `panic!()` sem mensagem → `"explicit panic"` |
| `match stmt { Statement::Query(q) => .., _ => unreachable!() }` | `var q = Assert.IsType<Statement.Query>(stmt).Value;` quando o padrão só seleciona a variante; quando também filtra campos (`name: Some(n)`, `if_not_exists: false`, tuplas), `switch` com padrões de propriedade ou posicionais e `default: throw new InvalidOperationException(..)` |
| `let P = x else { unreachable!() };` / `let P = x else { panic!(..) };` | `if (x is not P) throw new InvalidOperationException(..);` (com `Assert.Fail`, acrescente `return;`, para a atribuição definida) |
| `if let P = x { .. }` sem `else` | Sem `else` (passa em silêncio, como no Rust) |
| `other => panic!("msg {other:?}")` / `{:#?}` | `case var other: throw new InvalidOperationException($"msg {other}")`: o `Display` (`ToString()`) no lugar do `Debug`, o que só muda o texto da falha |
| `.unwrap_or_else(\|e\| panic!(..))` | `try`/`catch (ParserException e)` que lança `InvalidOperationException` com a mesma mensagem |
| `Option::unwrap()` | Valor só comparado → comparação direta com o anulável. Valor desreferenciado → operador `!` (o `NullReferenceException` faz o papel do `panic`) ou `Assert.NotNull(x)` antes do uso. Com a mensagem do Rust → ``x ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value")`` |
| `Option::expect(msg)` | `x ?? throw new InvalidOperationException(msg)` |

**Padrões, coleções e mutação**

| Rust | C# |
|---|---|
| `match` com padrão de struct (`Statement::CreateIndex(CreateIndex { name: Some(n), if_not_exists: false, .. })`) | `switch`/`is` com padrões de propriedade: `if_not_exists: false` → `IfNotExists: false`; `predicate: None` → `Predicate: null`; `Some(_)` → `not null`; `name: Some(ObjectName(name))` → `Name: { Value: var name }`; padrões aninhados → padrões de propriedade estendidos (`Value.HiveDistribution: HiveDistributionStyle.PARTITIONED { Columns: var cols }`). O `..` do Rust não tem tradução |
| Campos ligados sem restrição (`Statement::X { a, b, .. } => ..`) | `var x = Assert.IsType<Statement.X>(stmt).Value;` + locais com os nomes Rust em camelCase, ou leitura direta das propriedades (`createIndex.Unique`); o padrão Rust completo fica em comentário quando há campos ignorados |
| `match v[..] { [a, b] => .. }`, `matches!(x.as_slice(), [A { .. }, B { .. }])` | Padrão de lista (`v is [A, B]`, `v is [A, ..]`), como no Core (`convencoes.md` §13, caso 102): compila e passa em net48. Proibido só o `..` **com** ligação (`[first, .. var rest]`), que exige `Range`/`Slice`. O teste explícito de `Count` e de tipo (`v.Count == 2 && v[0] is A && v[1] is B`), usado nas C4, C6, C8, C10 e C12, é equivalente e fica como está |
| `vec.pop().unwrap()` / `remove(0)` / `swap_remove(0)` sobre `Sequence<T>` | Se só lê: indexação (`v[v.Count - 1]`, `v.Last`, `v[0]`). Se a lista continua em uso: `ToList()` + leitura + `RemoveAt`, na mesma ordem |
| Alteração de campo de um record (`select.from[0].alias.explicit = true`, `revoke.grant_option_for = false`) | Expressão `with`, que reconstrói o valor |
| `a.iter().zip(b)` | `Enumerable.Zip` **com** seletor (`a.Zip(b, (x, y) => (x, y))`) ou laço com índice até `Math.Min` dos tamanhos: a sobrecarga sem seletor não existe no net48 |
| `find_map` / `find` / `all` / `.iter().map(ToString::to_string).collect()` / `(0..=10).map(..)` | LINQ (`Select(..).FirstOrDefault(..)`, `FirstOrDefault`, `All`, `Select(n => n.ToString()).ToList()`, `Enumerable.Range(0, 11).Select(..)`) |
| `for (a, b) in &[(..), ..]` / `for &q in &['"', '\'']` | `foreach (var (a, b) in new (A, B)[] { .. })` / `foreach (var q in new[] { '"', '\'' })` |
| `only(&opt)` sobre `Option` | Coleção de 0 ou 1 item antes de `TestUtils.Only` (`x is { } v ? new[] { v } : System.Array.Empty<T>()`) |
| `Vec::from(ALL_KEYWORDS)` + `sort_unstable()` | `new List<string>(Keywords.AllKeywords)` + `Sort(string.CompareOrdinal)` (ordem de bytes, igual à do Rust para ASCII) |
| `.into_iter().for_each(\|d\| assert!(..))` | `.ForEach(d => Assert.True(..))` |
| `sql.len()` (bytes) em testes de span | `sql.Length`, desde que o SQL seja ASCII (confira) |

**Valores, conversões e nomes**

| Rust | C# |
|---|---|
| AST esperada: `Expr::BinaryOp { left: Box::new(Expr::Identifier(Ident::new("a"))), op: BinaryOperator::Eq, right: .. }` | `new Expr.BinaryOp(Left: new Expr.Identifier(new Ident("a")), Op: new BinaryOperator.Eq(), Right: ..)`. Argumentos nomeados quando o record tem mais de 3 campos ou quando a ordem do C# difere da do literal Rust (ex.: `Whitespace::SingleLineComment { prefix, comment }` → `new Whitespace.SingleLineComment(Prefix: .., Comment: ..)`: o record declara `Comment` antes, e sem os nomes os valores trocariam de lugar, C11). Abra o arquivo C# do record para ver os nomes exatos; exceções em `docs/port/nomes.md` |
| `Expr::value(number("1"))`, `Value::SingleQuotedString(..)` | `Expr.MakeValue(TestUtils.Number("1"))`, `new Value.SingleQuotedString(..)` |
| `Expr::Value(number("1").into())` / `Value::X(..).into()` (para `ValueWithSpan`) / `.with_empty_span()` | `new Expr.Value(ValueWithSpan.From(TestUtils.Number("1")))` / `ValueWithSpan.From(..)` / `.WithEmptySpan()` |
| `Value::Number("1".parse().unwrap(), false)` | `new Value.Number("1", false)` (sem a feature `bigdecimal`, DIV-006) |
| `vec![a, b]` / `vec![]` / `Box::new(x)` | `Sequence.Of(a, b)` / `Sequence<T>.Empty` / `x` |
| `"x".into()` ou `Ident::from("x")` (para `Ident`) / `Ident::new("x")` | `Ident.From("x")` / `new Ident("x")` |
| `Ident { value, quote_style, span: Span::empty() }` | `new Ident(Value: .., QuoteStyle: .., Span: Span.Empty())` |
| `Ident::new("x").into()` (para `ObjectName`) / `X { .. }.into()` (para `TableConstraint`) / `(3, 3).into()` (para `Location`) | `ObjectName.From(new Ident("x"))` / `TableConstraint.From(new X(..))` / `Location.From((3, 3))` (caso 7) |
| `X { campo: v, ..Default::default() }` / `Default::default()` campo a campo | `X.Default() with { Campo = v }` (caso 42) / o valor padrão explícito (`false`, `null`, `Sequence<T>.Empty`) |
| `ParserOptions { trailing_commas, unescape, require_semicolon_stmt_delimiter }` / `options.clone()` | `new ParserOptions(TrailingCommas: .., Unescape: .., RequireSemicolonStmtDelimiter: ..)` / o próprio `options` (record imutável) |
| `Box<Query>` com `.clone().into()`; `Expr::..(..).into()` para `Box<Expr>`; `"lit".into()`/`"lit".to_string()` para `String` | A própria `Query`/`Expr`; o próprio literal |
| `Tag::new(key, value)` e outros `fn new` com os campos na ordem | O construtor posicional do record (`new Tag(key, value)`) |
| `dialect.is::<X>()` / `d.supports_x()` / `&mut Parser::new(d)` / `let d: &dyn Dialect = &TeradataDialect;` | `d is X` (caso 8) / propriedade `d.SupportsX` (§7) / `new Parser(d)` / `Dialect d = new TeradataDialect();` |
| `format!("{dialect:?}")` / `{token:?}` / `{keyword:?}` | `dialect.GetType().Name` (caso 9) / `token.ToDebugString()` (`internal`, visível via `InternalsVisibleTo`) / `keyword.ToString()` (caso 46) |
| `assert_eq!(parser.peek_token(), Token::X)` (`TokenWithSpan` × `Token`) | `Assert.Equal(parser.PeekToken().Token, X)`: mesmo critério do `PartialEq` (caso 21), com mensagem de falha legível |
| `run_parser_method(sql, \|p\| ..)` com closure que devolve `()` | Sobrecarga privada com `Action<Parser>` que devolve `ValueTuple.Create()` (C17). `ValueTuple` é a forma canônica de `()` também no código (`convencoes.md` §13, caso 85) |
| Tipos da AST homônimos de tipos de `System` (`Array`, `Action`) | `Ast.Array`, `Ast.Action`; no sentido de `System`, qualificado: `System.Array.Empty<T>()`, `System.Action` (CS0104, caso 49) |
| Variável Rust com nome de palavra reservada do C# (`using`, `operator`, `lock`, `params`, `case`) | `@using`, `@operator`, `@lock`, `@params` (caso 56); em laço que colidiria com um local externo, nome distinto (`for case in ..` → `testCase`) |
| Variável Rust com nome de palavra contextual do C# (`from`, `select`, `where`, `with`…) | Prefixo `@` (`@with`, `@from`), como no código (`convencoes.md` §13, caso 95), que mantém o nome Rust buscável; renomeie só quando o `@` não resolver. Locais criados pela tradução, sem equivalente no Rust, recebem nome livre (ex.: `firstFrom`, C12) |
| Sombreamento (`let sql = ..` repetido) | Mesmo tipo e escopo → reatribuição; tipo diferente ou colisão de escopo (CS0136) → nome distinto (`invalidSql`, `alterStatements`), caso 72 |

**Teste que falha depois de portado:**
- Não altere o `src/` (vários agentes trabalham em paralelo).
- Registre a falha nas suas notas, com o teste, a asserção, a saída obtida × esperada e o arquivo/função suspeita no Core.
- Deixe o teste **falhando** (sem `Skip`), para que a falha continue visível até ser corrigida.
- **Única exceção:** um teste que **derruba o processo** (`StackOverflowException`, que o .NET não deixa capturar: o host de teste morre e a execução inteira é anulada, escondendo todos os outros resultados) recebe `[Fact(Skip = "PENDENTE (onda D, <motivo>): ...")]`, com o diagnóstico nas notas. Ao corrigir o `src/`, remova o `Skip`. Exemplos: `overflow` e `parse_deeply_nested_boolean_expr_does_not_stackoverflow` (`SqlParserCommonTests4`, C4).
- Um `Skip` usa só os prefixos `DIV-NNN:` (divergência registrada) ou `PENDENTE` (dependência ainda não portada ou correção da onda D); nunca `FALHA:` nem texto livre. A outra forma admitida é `#[ignore] no Rust: <motivo>`, para os testes que o próprio upstream ignora (C17).

**Strings Rust com escapes** (`"\u{1F600}"`, `"\x41"`, `"\'"`): converta com cuidado para o equivalente C#. `"\u{1F600}"` (fora do BMP) vira `"\U0001F600"`. Um erro aqui produz um teste "verde" que testa outra coisa.

**Strings: regras consolidadas da onda C** (os `.cs` ficam em CRLF):
- `\` no fim da linha (continuação) → concatenação dos pedaços **sem** a quebra e **sem** a indentação da linha seguinte; os espaços antes da `\` ficam (`convencoes.md` §13, caso 109). Uma linha **sem** `\` dentro do literal → `"\n"` explícito + a indentação real (confira com `cat -A` no `.rs`).
- String de várias linhas (comum ou `r#"..."#`) → concatenação linha a linha com `"\n"` explícito. **Nunca** raw string literal C# de várias linhas: ele carregaria `"\r\n"` (e a remoção de indentação do C#) e mudaria o teste.
- `r#"..."#` de uma linha → `"""..."""` quando o conteúdo não começa nem termina com aspas e não tem três aspas seguidas; senão, string comum com `\"`/`\\`. `r"..."`/`r#"..."#` com barras invertidas → string verbatim `@"..."` (aspas dobradas). Confira caractere a caractere.
- `concat!(..)` → `+` ou `string.Concat(..)`; `format!("..{x}..")` → interpolação; `format!` com argumentos posicionais → `string.Format` ou interpolação; `"x".repeat(n)` → `new string(c, n)`, `string.Concat(Enumerable.Repeat(..))` ou o auxiliar `Repeat`.
- Escapes no valor esperado: `\u{8}` → `\b`; `\u{7}` → `\a`; `\u{1a}` → `\x001A` (sempre quatro dígitos, para o `\x` não engolir os caracteres seguintes); um scalar qualquer → `char.ConvertFromUtf32(0x10FFFF)`. Tabulação literal no SQL → `\t`. `'\"'` e `'\\'` significam o mesmo em C#.
- SQL de entrada que contém a barra invertida seguida de `u`/`U` e dígitos (`E'..'`, `U&'..'`): escreva com o marcador `{BS}` e `.Replace("{BS}", Backslash)` (C7), para nenhuma ferramenta de edição converter a sequência no caractere.
- Caracteres não ASCII do original (símbolos, emojis, cirílico, CJK) ficam literais, conferidos byte a byte contra o `.rs`. O `Find-SuspiciousChars.ps1` lista essas linhas, e isso é esperado: anote nas notas que são intencionais.

## Quando o teste portado falha

1. Releia o código Rust da funcionalidade e compare com o C#. Em 95% dos casos o port está errado: corrija o código, **nunca o teste**.
2. Se a diferença for deliberada e aprovada (limitação de plataforma, decisão registrada):
   - registre em `docs/port/divergencias.md` com um ID `DIV-NNN`;
   - marque o teste com `[Fact(Skip = "DIV-NNN: descrição curta")]`.

   **Não apague** o teste.
3. Se o teste depender de algo ainda não portado (por exemplo, um teste de tokenizer que usa o `Parser`):
   - marque `[Fact(Skip = "PENDENTE: depende de <item> (Fase N)")]`;
   - registre em `docs/port/progresso.md`.

## Contagem

Atualize a coluna "Testes Rust → C#" de `docs/port/progresso.md` com `portados/total` do arquivo Rust. Conte os `#[test]` do Rust com:

```bash
grep -c "#\[test\]" upstream/sqlparser-rs/src/tokenizer.rs
```

Para rodar só uma classe de teste:

```bash
dotnet test SqlParser.sln --filter "FullyQualifiedName~TokenizerTests" -nologo -v q
```
