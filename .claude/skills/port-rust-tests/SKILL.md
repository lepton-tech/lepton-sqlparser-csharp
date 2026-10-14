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
| `tests/sqlparser_common.rs` | `Integration/SqlParserCommonTests*.cs` (`partial class`; um arquivo por ~1.500 linhas Rust, na ordem do original) |
| `tests/sqlparser_<dialeto>.rs` | `Integration/SqlParser<Dialeto>Tests.cs` |
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

| Rust | C# |
|---|---|
| `all_dialects().verified_stmt(sql)` | `TestUtils.AllDialects().VerifiedStmt(sql)` |
| `all_dialects_where(\|d\| d.supports_x())` | `TestUtils.AllDialectsWhere(d => d.SupportsX)` |
| `TestedDialects::new(vec![Box::new(GenericDialect {}), ..])` | `new TestedDialects(new List<Dialect> { new GenericDialect(), .. })` |
| `TestedDialects::new_with_options(.., ParserOptions::new().with_trailing_commas(true))` | `TestedDialects.NewWithOptions(.., new ParserOptions().WithTrailingCommas(true))` |
| Funções-atalho do arquivo (`fn verified_stmt(sql) -> Statement { all_dialects().verified_stmt(sql) }`, `fn pg() -> TestedDialects { .. }`) | Métodos `private static` da classe de teste, com nome mecânico (`VerifiedStmt`, `Pg()`) |
| `one_statement_parses_to(sql, canonical)` / `verified_query` / `verified_only_select` / `verified_expr` / `expr_parses_to` | `OneStatementParsesTo` / `VerifiedQuery` / `VerifiedOnlySelect` / `VerifiedExpr` / `ExprParsesTo` |
| `let res = parse_sql_statements(sql); assert_eq!(ParserError::ParserError("msg".to_string()), res.unwrap_err());` | `var ex = Assert.Throws<ParserException>(() => ParseSqlStatements(sql)); Assert.Equal(new ParserError.ParserErrorValue("msg"), ex.Error);` |
| `assert!(parse_sql_statements(sql).is_err())` | `Assert.Throws<ParserException>(() => ParseSqlStatements(sql));` |
| `match stmt { Statement::Query(q) => .., _ => unreachable!() }` | `var q = Assert.IsType<Statement.Query>(stmt).Value;` ou `switch` com `default: throw ..` |
| AST esperada: `Expr::BinaryOp { left: Box::new(Expr::Identifier(Ident::new("a"))), op: BinaryOperator::Eq, right: .. }` | `new Expr.BinaryOp(Left: new Expr.Identifier(new Ident("a")), Op: new BinaryOperator.Eq(), Right: ..)`. Abra o arquivo C# do record para ver os nomes exatos; exceções em `docs/port/nomes.md` |
| `Expr::value(number("1"))`, `Value::SingleQuotedString(..)` | `Expr.MakeValue(TestUtils.Number("1"))`, `new Value.SingleQuotedString(..)` |
| `vec![a, b]` / `vec![]` / `Box::new(x)` | `Sequence.Of(a, b)` / `Sequence<T>.Empty` / `x` |
| `#[should_panic]` | `Assert.ThrowsAny<Exception>(..)` |

**Teste que falha depois de portado:**
- Não altere o `src/` (vários agentes trabalham em paralelo).
- Registre a falha nas suas notas, com o teste, a asserção, a saída obtida × esperada e o arquivo/função suspeita no Core.
- Deixe o teste **falhando** (sem `Skip`), para que a falha continue visível até ser corrigida.

**Strings Rust com escapes** (`"\u{1F600}"`, `"\x41"`, `"\'"`): converta com cuidado para o equivalente C#. `"\u{1F600}"` (fora do BMP) vira `"\U0001F600"`. Um erro aqui produz um teste "verde" que testa outra coisa.

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
