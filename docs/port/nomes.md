# Glossário de nomes (memória de tradução)

Este glossário lista **só o que a regra mecânica de [`convencoes.md`](convencoes.md) §4 e §6 não resolve sozinha**:
- nomes de campos de variantes-tupla com 2+ campos;
- conflitos do compilador C#;
- exceções de tipo.

**Regra de ouro:** antes de inventar um nome, procure aqui. Depois de inventar, registre aqui **antes** de usar em outro lugar. Isso evita que duas sessões escolham nomes diferentes para a mesma coisa.

Formato: `Rust` → `C#` — motivo — arquivo onde foi definido.

## Core (`SqlParser.Core`)

### Variantes-tupla com 2+ campos

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `Token::Number(String, bool)` | `Number(string Value, bool Long)` | `Long`: sufixo `L` do literal (Display escreve `L` quando verdadeiro) | `Tokenizer/Token.cs` |

### Tuplas em retornos e parâmetros

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `Dialect::peek_nested_delimited_identifier_quotes -> Option<(char, Option<char>)>` | `(char QuoteStart, char? NestedQuoteStart)?` | Nomes das variáveis que o tokenizer usa ao desestruturar (`let Some((quote_start, nested_quote_start))`) | `Dialects/Dialect.cs` |

### Conflitos de compilação (CS0542 etc.)

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| Variante tupla `Value(T)` | parâmetro `Inner` em vez de `Value` | CS0542: o membro teria o nome do tipo que o contém | regra geral, `convencoes.md` §6 |
| `Unescape::unescape(self)` | `Unescape.DoUnescape()` | CS0542: método com o nome do tipo que o contém | `Tokenizer/Unescape.cs` |
| `Tokenizer::tokenizer_error<R>(..)` | `Tokenizer.TokenizerErrorAt<T>(..)` | Dentro de `Tokenizer`, um método `TokenizerError` esconderia o tipo `TokenizerError` em expressões | `Tokenizer/Tokenizer.cs` |
| `Span::EMPTY` (const privado, "for pattern matching") | não portado; as comparações usam `Span.Empty()` | CS0102: o campo `Empty` colidiria com `fn empty()` → `Empty()` | `Tokenizer/Span.cs` |

### Exceções de tipo/valor

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `Location { line: u64, column: u64 }` | `long Line`, `long Column` | `ulong` é incômodo em API pública. Os valores nunca são negativos | `convencoes.md` §5 |
| `keywords::Keyword::NoKeyword` | `Keyword.NoKeyword` | Mantido igual ao Rust. O SqlParser-cs usava `undefined` | `Keywords.g.cs` |
| `Token::Char(char)` | `Token.Char(string Value)` | Um `char` do Rust (scalar value) fora do BMP não cabe num `char` C#. `Value` contém sempre **um** scalar value (1 ou 2 unidades UTF-16); o tokenizer junta o par substituto, como o Rust (testes `tokenize_invalid_string*`) | `Tokenizer/Token.cs` |
| `NumStringQuoteChars::Many(NonZeroU8)` | `Many(byte Value)`, construtor rejeita 0 | `NonZeroU8` → tipo base + validação (`convencoes.md` §5) | `Tokenizer/NumStringQuoteChars.cs` |
| `Peekable<Chars<'_>>` (campo de `State` e parâmetro de `Dialect::peek_nested_delimited_identifier_quotes`) | `PeekableChars` (`struct` público mutável) | O parâmetro de um membro `public virtual` precisa de tipo público; copiar o `struct` equivale ao `.clone()` do iterador (`convencoes.md` §13) | `Support/PeekableChars.cs` |
| Escapes que produzem `char` fora do BMP (`Option<char>` de `Unescape`, `take_char_from_hex_digits`) | `int`/`int?` com o scalar value | `\U0010FFFF` não cabe num `char` C# (`convencoes.md` §13) | `Tokenizer/Unescape.cs`, `Tokenizer/Tokenizer.cs` |
| `#[deprecated] pub type TokenWithLocation = TokenWithSpan` | não portado | C# não exporta alias de tipo; obsoleto desde a 0.53.0 | `Tokenizer/TokenWithSpan.cs` |

## Camada pública (`SqlParser`)

Nomes "amigáveis" herdados do SqlParser-cs, aplicáveis **somente** à API pública ([ADR-0002](../adr/0002-arquitetura-em-camadas-e-api.md)). Serão definidos quando a camada pública for desenhada.

| Conceito (Core) | API pública | Observação |
|---|---|---|
| _(a definir)_ | | |
