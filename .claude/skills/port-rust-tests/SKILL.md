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
