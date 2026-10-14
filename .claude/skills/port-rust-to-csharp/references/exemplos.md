# Exemplos canônicos de tradução Rust → C#

Estes exemplos são **o padrão a copiar**. Vêm do `tokenizer.rs` e do `dialect/mod.rs` do sqlparser-rs v0.63.0. Se o código já portado em `src/SqlParser.Core/` divergir daqui, quem está certo é `docs/port/convencoes.md`. Quando um exemplo melhor surgir no código real, atualize este arquivo apontando para ele.

Sumário:
1. [Struct `Copy` com `Display`, `Debug`, `From` e construtores](#1-struct-copy-com-display-debug-from-e-construtores)
2. [Enum com dados + `impl` com `match`](#2-enum-com-dados--impl-com-match)
3. [`Result`, `?` e erro com mensagem idêntica](#3-result--e-erro-com-mensagem-idêntica)
4. [`Option`, `if let`, `while let`](#4-option-if-let-while-let)
5. [Trait `Dialect`: métodos abstratos, padrões e propriedades](#5-trait-dialect-métodos-abstratos-padrões-e-propriedades)
6. [`dialect_of!`](#6-dialect_of)

---

## 1. Struct `Copy` com `Display`, `Debug`, `From` e construtores

```rust
#[derive(Eq, PartialEq, Hash, Clone, Copy, Ord, PartialOrd)]
pub struct Location {
    /// Line number, starting from 1.
    ///
    /// Note: Line 0 is used for empty spans
    pub line: u64,
    /// Line column, starting from 1.
    ///
    /// Note: Column 0 is used for empty spans
    pub column: u64,
}

impl fmt::Display for Location {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        if self.line == 0 {
            return Ok(());
        }
        write!(f, " at Line: {}, Column: {}", self.line, self.column)
    }
}

impl Location {
    /// Return an "empty" / unknown location
    pub fn empty() -> Self { Self { line: 0, column: 0 } }
    /// Create a new `Location` for a given line and column
    pub fn new(line: u64, column: u64) -> Self { Self { line, column } }
    /// Combine self and `end` into a new `Span`
    pub fn span_to(self, end: Self) -> Span { Span { start: self, end } }
}

impl From<(u64, u64)> for Location {
    fn from((line, column): (u64, u64)) -> Self { Self { line, column } }
}
```

```csharp
/// <summary>...</summary>
// #[derive(Copy, Eq, Hash, Ord)] → readonly record struct + IComparable (ordem lexicográfica dos campos, como o derive do Rust)
public readonly record struct Location(long Line, long Column) : IDisplay, IComparable<Location>
{
    /// <summary>Line number, starting from 1. Note: Line 0 is used for empty spans</summary>
    public long Line { get; init; } = Line;
    /// <summary>Line column, starting from 1. Note: Column 0 is used for empty spans</summary>
    public long Column { get; init; } = Column;

    /// <summary>Return an "empty" / unknown location</summary>
    public static Location Empty() => new Location(0, 0);          // fn empty() -> Self   → método estático
    // fn new(..) -> Self → o próprio construtor primário (new Location(l, c))

    /// <summary>Combine self and <c>end</c> into a new <see cref="Span"/></summary>
    public Span SpanTo(Location end) => new Span(this, end);

    // impl From<(u64, u64)> for Location → método estático From (conversões implícitas não são usadas)
    public static Location From((long Line, long Column) value) => new Location(value.Line, value.Column);

    public void Fmt(Formatter f)
    {
        if (Line == 0)
        {
            return;
        }
        f.WriteStr(FormattableString.Invariant($" at Line: {Line}, Column: {Column}"));
    }

    public override string ToString() => Formatter.Format(this);

    // impl fmt::Debug → só porque o texto aparece em mensagens/testes
    public string ToDebugString() => FormattableString.Invariant($"Location({Line},{Column})");

    public int CompareTo(Location other)
    {
        var c = Line.CompareTo(other.Line);
        return c != 0 ? c : Column.CompareTo(other.Column);
    }
}
```

Pontos a observar:
- O doc comment inglês é mantido.
- `fn new` vira o construtor. As outras fábricas (`empty`, `of`) viram métodos estáticos.
- `From` vira o método estático `From`.
- `Debug` só é traduzido quando necessário, como `ToDebugString()`.

## 2. Enum com dados + `impl` com `match`

```rust
pub enum Token {
    /// An end-of-file marker, not a real token
    EOF,
    /// A keyword (like SELECT) or an optionally quoted SQL identifier
    Word(Word),
    /// An unsigned numeric literal
    Number(String, bool),
    /// Comma
    Comma,
    // ...
}

impl fmt::Display for Token {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        match self {
            Token::EOF => f.write_str("EOF"),
            Token::Word(ref w) => write!(f, "{w}"),
            Token::Number(ref n, l) => write!(f, "{}{long}", n, long = if *l { "L" } else { "" }),
            Token::Comma => f.write_str(","),
            // ...
        }
    }
}

impl Token {
    pub fn make_word(word: &str, quote_style: Option<char>) -> Self {
        Token::Word(Word { keyword: keyword_lookup(word, quote_style), value: word.to_string(), quote_style })
    }
}
```

```csharp
public abstract record Token : IDisplay
{
    /// <summary>An end-of-file marker, not a real token</summary>
    public sealed record EOF : Token;
    /// <summary>A keyword (like SELECT) or an optionally quoted SQL identifier</summary>
    // Variante tupla cujo tipo tem o mesmo nome: tipo qualificado (convencoes.md §6)
    public sealed record Word(SqlParser.Core.Word Value) : Token;
    /// <summary>An unsigned numeric literal</summary>
    // 2+ campos: nomes registrados em docs/port/nomes.md
    public sealed record Number(string Value, bool Long) : Token;
    /// <summary>Comma</summary>
    public sealed record Comma : Token;

    // impl Token { pub fn make_word(..) }
    public static Token MakeWord(string word, char? quoteStyle) =>
        new Word(new SqlParser.Core.Word(word, quoteStyle, KeywordLookup(word, quoteStyle)));

    // impl fmt::Display for Token: o match inteiro vira UM switch na base
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case EOF:
                f.WriteStr("EOF");
                break;
            case Word w:
                f.Write(w.Value);
                break;
            case Number n:
                f.WriteStr(n.Value);
                f.WriteStr(n.Long ? "L" : "");
                break;
            case Comma:
                f.WriteStr(",");
                break;
            default:
                throw new InvalidOperationException($"Variante de Token não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
```

Uso no tokenizer/parser: `if (token is Token.Word { Value: var w } && w.Keyword == Keyword.SELECT)`, `case Token.Comma:`.

## 3. `Result`, `?` e erro com mensagem idêntica

```rust
#[derive(Debug, PartialEq, Eq)]
pub struct TokenizerError {
    pub message: String,
    pub location: Location,
}

impl fmt::Display for TokenizerError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{}{}", self.message, self.location,)
    }
}

fn tokenizer_error<R>(&self, loc: Location, message: impl Into<String>) -> Result<R, TokenizerError> {
    Err(TokenizerError { message: message.into(), location: loc })
}

// uso:
let s = self.tokenize_single_quoted_string(chars, '\'', true)?;
```

```csharp
public sealed record TokenizerError(string Message, Location Location) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.WriteStr(Message);
        f.Write(Location);   // "{}" de Location: string vazia se line == 0
    }
    public override string ToString() => Formatter.Format(this);
}

/// <summary>Lançada onde o Rust retorna <c>Err(TokenizerError)</c>.</summary>
public sealed class TokenizerException : Exception
{
    public TokenizerException(TokenizerError error) : base(error.ToString()) => Error = error;
    public TokenizerError Error { get; }
}

// fn tokenizer_error<R>(..) -> Result<R, _> → método que LANÇA; o tipo de retorno genérico permite
// usar em expressões como no Rust:  return TokenizerErrorAt<Token>(loc, "...");
private T TokenizerErrorAt<T>(Location loc, string message) =>
    throw new TokenizerException(new TokenizerError(message, loc));

// `?` → chamada simples: a exceção propaga sozinha
var s = TokenizeSingleQuotedString(chars, '\'', true);
```

## 4. `Option`, `if let`, `while let`

```rust
if let Some(ch) = chars.peek() { ... }
let quote_end = Word::matching_end_quote(quote_start);   // Option<char>
while let Some(&ch) = chars.peek() {
    match ch {
        ' ' => { chars.next(); }
        _ => break,
    }
}
```

```csharp
if (chars.Peek() is { } ch) { ... }                       // Peek() retorna char? (null = fim)
char? quoteEnd = SqlParser.Core.Word.MatchingEndQuote(quoteStart);
while (chars.Peek() is { } ch2)
{
    if (ch2 == ' ') { chars.Next(); continue; }
    break;
}
```

## 5. Trait `Dialect`: métodos abstratos, padrões e propriedades

```rust
pub trait Dialect: Debug + Any {
    /// Determine if a character starts an unquoted identifier
    fn is_identifier_start(&self, ch: char) -> bool;            // sem padrão
    fn is_delimited_identifier_start(&self, ch: char) -> bool { // com padrão
        ch == '"' || ch == '`'
    }
    fn supports_string_literal_backslash_escape(&self) -> bool { // sem parâmetros
        false
    }
}
```

```csharp
public abstract class Dialect
{
    /// <summary>Determine if a character starts an unquoted identifier</summary>
    public abstract bool IsIdentifierStart(char ch);                       // sem padrão → abstract
    public virtual bool IsDelimitedIdentifierStart(char ch) => ch == '"' || ch == '`';  // com padrão → virtual
    public virtual bool SupportsStringLiteralBackslashEscape => false;    // &self sem parâmetros → propriedade
}
```

## 6. `dialect_of!`

```rust
if dialect_of!(self is PostgreSqlDialect | GenericDialect) { ... }
```

```csharp
if (_dialect is PostgreSqlDialect or GenericDialect) { ... }
```
