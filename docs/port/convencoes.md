# Convenções de tradução Rust → C#

> **Documento normativo.** Todo código portado para o `SqlParser.Core` segue estas regras. Elas existem para que sessões e pessoas diferentes traduzam o mesmo construto Rust **sempre do mesmo jeito**.
>
> Quando você encontrar um caso não coberto aqui:
> 1. decida;
> 2. registre a decisão na seção [13. Casos decididos](#13-casos-decididos-memória-de-tradução), ou em [`nomes.md`](nomes.md) se for um nome;
> 3. só depois siga.
>
> Nunca decida "no improviso" sem registrar.

Documentos irmãos:

| Documento | Conteúdo |
|---|---|
| [`nomes.md`](nomes.md) | Nomes que a regra mecânica não resolve sozinha |
| [`divergencias.md`](divergencias.md) | Incompatibilidades conhecidas com o Rust |
| [`progresso.md`](progresso.md) | Estado do port por arquivo Rust |
| [`referencias-e-boas-praticas.md`](referencias-e-boas-praticas.md) | Fundamentação externa |
| Skills em `.claude/skills/` | Fluxos de trabalho para o Claude Code |

---

## 1. Princípios

1. **Duas camadas** ([ADR-0002](../adr/0002-arquitetura-em-camadas-e-api.md)):
   - **`SqlParser.Core`** é o port **fiel** do Rust. Tem nomes, estrutura e comportamento do Rust, e só isso. Nenhuma adaptação, conveniência ou customização.
   - **`SqlParser`** é a **API pública** do produto: código nosso, no estilo do SqlParser-cs (`SqlQueryParser`, `ToSql()`, visitors, utilitários, customizações). Tudo que não é tradução 1:1 vai para cá.
   - O Core nunca referencia a camada pública.
2. **O Rust é a fonte da verdade de comportamento.** Na dúvida, o Core faz o que o Rust faz, inclusive mensagens de erro e o SQL gerado.
3. **Correspondência 1:1.**
   - Cada arquivo `.rs` tem destino conhecido (seção 2).
   - Cada tipo Rust vira um tipo C# de mesmo nome; cada `fn` vira um método com o nome derivado pela regra mecânica (seção 4).
   - A ordem dos membros segue a ordem do arquivo Rust. Isso facilita comparar com o upstream lado a lado.
   - Um `match` do Rust vira **um** `switch` no mesmo lugar. Não espalhe a lógica em overrides por variante.
4. **Tradução fiel, não reescrita.** Não "melhore", não reorganize e não funda funções enquanto porta. Se o Rust tem um bug, porte o bug. Uma correção só entra como decisão registrada em `divergencias.md`.
5. **Sem dependências de runtime.** `netstandard2.0` puro. Respeite a seção 11.

## 2. Mapeamento de arquivos e namespaces (Core)

| Rust (`upstream/sqlparser-rs/src/...`) | C# (`src/SqlParser.Core/...`) | Namespace |
|---|---|---|
| `keywords.rs` | `Keywords.g.cs` (**gerado**, não edite) + `Keywords.cs` | `SqlParser.Core` |
| `tokenizer.rs` | `Tokenizer/*.cs` (um tipo de topo por arquivo: `Token.cs`, `Word.cs`, `Whitespace.cs`, `Location.cs`, `Span.cs`, `TokenWithSpan.cs`, `TokenizerError.cs`, `Tokenizer.cs`…) | `SqlParser.Core` |
| `dialect/mod.rs` | `Dialects/Dialect.cs` (+ `Dialects/Dialect.Parser.cs` para hooks que recebem `Parser`) | `SqlParser.Core.Dialects` |
| `dialect/<nome>.rs` | `Dialects/<Nome>Dialect.cs` (mesmo nome do struct Rust) | `SqlParser.Core.Dialects` |
| `parser/mod.rs`, `alter.rs`, `merge.rs` | `Parser/Parser*.cs` (`partial class Parser`; a divisão por seção fica registrada em `progresso.md`) | `SqlParser.Core` |
| `ast/<modulo>.rs` | `Ast/<Modulo>/<Tipo>.cs` (um tipo de topo por arquivo) | `SqlParser.Core.Ast` |
| `ast/helpers/*.rs` | `Ast/Helpers/*.cs` | `SqlParser.Core.Ast` |
| `ast/spans.rs`, `ast/visitor.rs` | `Ast/Spans/*.cs`, `Ast/Visitor/*.cs` | `SqlParser.Core.Ast` |
| Infraestrutura própria (equivalentes de `core::fmt` etc.) | `Support/*.cs` | `SqlParser.Core` |
| `test_utils.rs` | `tests/SqlParser.Core.Tests/TestUtils/*.cs` | `SqlParser.Core.Tests` |
| `#[cfg(test)] mod tests` de `x.rs` | `tests/SqlParser.Core.Tests/<Area>/<X>Tests.cs` | `SqlParser.Core.Tests` |
| `tests/sqlparser_<d>.rs` | `tests/SqlParser.Core.Tests/Integration/SqlParser<D>Tests*.cs` (partial se grande) | `SqlParser.Core.Tests` |

Módulos Rust `tokenizer`, `parser` e `keywords` ficam no namespace raiz `SqlParser.Core`, para evitar o conflito tipo/namespace `Tokenizer.Tokenizer`. `dialect` vira `Dialects` pelo mesmo motivo.

## 3. Cabeçalho obrigatório em arquivos portados

Copie o cabeçalho ASF verbatim de `upstream/sqlparser-rs/HEADER` e acrescente a origem:

```csharp
// Licensed to the Apache Software Foundation (ASF) under one
// ... (16 linhas de upstream/sqlparser-rs/HEADER, sem alterar) ...
// under the License.
//
// Portado para C# a partir de sqlparser-rs v0.63.0, src/tokenizer.rs.
// Modificado: tradução de Rust para C#.
```

- Arquivos sem origem no Rust (`Support/`, camada pública) **não** levam o cabeçalho ASF. Levam só um comentário dizendo que são próprios.
- Ao atualizar um arquivo para uma versão nova do upstream, atualize a versão no cabeçalho.

## 4. Nomes (Core: regra mecânica, sem exceções de gosto)

| Rust | C# | Exemplo |
|---|---|---|
| Tipo (`struct`/`enum`/`trait`) | Mesmo nome | `SetExpr` → `SetExpr`; `Expr` → `Expr` |
| `fn snake_case` | `PascalCase` | `parse_select_item` → `ParseSelectItem` |
| Campo `snake_case` | Propriedade `PascalCase` | `quote_style` → `QuoteStyle` |
| Variante de enum | Mesmo nome | `JoinOperator::LeftOuter` → `JoinOperator.LeftOuter` |
| `const SCREAMING_CASE` | `PascalCase` | `RESERVED_FOR_TABLE_ALIAS` → `ReservedForTableAlias` |
| Funções livres de módulo | Método estático em classe com o nome do módulo | `keywords::...` → `Keywords....` |
| Keywords | Idênticas, MAIÚSCULAS | `Keyword.SELECT`, `Keyword.NoKeyword` |
| Identificador bruto `r#type` | `Type` | — |
| Nome de teste `fn tokenize_select_1` | **Mantido em snake_case** | `public void tokenize_select_1()` |

**Siglas** seguem a grafia do Rust: `MySqlDialect`, `SQLiteDialect`, `ParserError` (não "renomeie para o padrão .NET").

**Conflitos que o C# não aceita** (por exemplo, CS0542: membro com o nome do tipo que o contém):
- aplique a regra da seção 6;
- registre o caso em [`nomes.md`](nomes.md).

**Nomes "amigáveis"** no estilo SqlParser-cs (`Expression`, `Statement.Select` etc.) **não entram no Core**. Se desejados, pertencem à camada pública.

## 5. Tipos

| Rust | C# | Observação |
|---|---|---|
| `bool` | `bool` | |
| `u8`, `u16`, `u32`, `u64` | `byte`, `ushort`, `uint`, `ulong` | Exceção registrada: `Location.line/column` → `long` |
| `i8`…`i64` | `sbyte`, `short`, `int`, `long` | |
| `usize`/`isize` | `int` | Índices e contagens |
| `char` | `char` | Ver Unicode na seção 10 |
| `String`, `&str`, `Cow<str>` | `string` | |
| `Option<T>` | `T?` | Para tipos-valor, `Nullable<T>`. Nunca use "valor mágico" no lugar de `null` |
| `Box<T>`, `Rc<T>`, `Arc<T>` | `T` | |
| `Vec<T>` | `Sequence<T>` na AST (definido na Fase 2); `List<T>` em código interno | |
| `&[T]` retornado | `IReadOnlyList<T>` | |
| `HashMap`/`BTreeMap`/`HashSet`/`BTreeSet` | `Dictionary`/`SortedDictionary`/`HashSet`/`SortedSet` | Se a ordem afetar a saída, use a versão ordenada |
| `Cell`/`RefCell`/`Rc<Cell<T>>` | Campo mutável em classe | |
| `Peekable<Chars>` | Cursor próprio (ver o `State` do tokenizer) | |
| `NonZeroU8` etc. | Tipo base + validação | |
| `()` | `void` | |

## 6. Enums

1. **Enum sem dados** (todas as variantes unitárias) → `enum` C# com os mesmos nomes.
2. **Enum com dados** → `public abstract record E` com cada variante como `public sealed record V(...) : E` **aninhado**. Inclui `Token`.
   - Variante unitária → `public sealed record V : E;`.
   - Variante struct `V { a: A, b: B }` → `public sealed record V(A A, B B) : E;`. Os nomes dos parâmetros são os campos em PascalCase.
   - Variante tupla de 1 campo `V(T)` → `public sealed record V(T Value) : E;`.
     - Se a variante se chama `Value`, o parâmetro se chama `Inner` (CS0542).
     - Se `T` tem o **mesmo nome** da variante (`Token::Word(Word)`, `Statement::Insert(Insert)`), qualifique o tipo: `public sealed record Word(SqlParser.Core.Word Value) : Token;`.
   - Variante tupla de 2+ campos `V(A, B)` → nomes descritivos tirados da doc Rust, **registrados em [`nomes.md`](nomes.md)** antes de usar, para nunca variarem. Exemplo: `Token::Number(String, bool)` → `Number(string Value, bool Long)`.
   - Uso: `if (tok is Token.Word { Value: var w })`, `case Token.Comma:`.
3. `match` exaustivo → `switch` com `default: throw new InvalidOperationException(...)`. Nunca deixe cair em silêncio.
4. Métodos de `impl E { fn x(&self) { match self { … } } }` → método na **base** `E` com `switch (this)`, mantendo o `match` num lugar só.

## 7. Structs, traits, `impl` e `Display`

| Rust | C# |
|---|---|
| `struct` de dados (`#[derive(PartialEq)]`) | `public sealed record` |
| `struct` com estado mutável (`Tokenizer`, `Parser`) | `public class` |
| `#[derive(Default)]` / `impl Default` | Construtor sem parâmetros ou `static X Default` |
| `impl PartialEq` customizado (ex.: ignora `Span`) | Sobrescrever `Equals`/`GetHashCode` com o **mesmo critério** |
| `impl X { fn ... }` | Métodos na própria classe/record |
| `trait T` com implementações padrão | `abstract class T` com membros `virtual`. **Proibido usar default interface members** (não rodam no .NET Framework) |
| `trait` sem corpo (marcação/capacidade) | `interface` |
| `impl Display for X` | `IDisplay.Fmt(Formatter f)` + `ToString()` delegando (ver abaixo) |
| `impl Debug` | Só se o texto aparecer em mensagens de erro; então replique o formato `Debug` |

**`Display`** usa `SqlParser.Core.IDisplay` e `Formatter` (`Support/Formatter.cs`), o equivalente direto de `core::fmt`:

```csharp
// Rust: impl fmt::Display for Token { fn fmt(&self, f) -> fmt::Result { match self { ... } } }
public abstract record Token : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case EOF: f.WriteStr("EOF"); break;                 // f.write_str("EOF")
            case Word w: f.Write(w.Value); break;               // write!(f, "{w}")
            // ...
            default: throw new InvalidOperationException($"Variante não tratada: {GetType().Name}");
        }
    }

    // Base de hierarquia de records: "sealed" impede que cada variante gere o
    // ToString() sintetizado ("Word { Value = ... }"), que seria uma divergência silenciosa.
    public sealed override string ToString() => Formatter.Format(this);
}
```

- Em struct/record isolado: `public override string ToString() => Formatter.Format(this);`.
- Mapeamentos:
  - `write!(f, "{}", x)` → `f.Write(x)`;
  - `write!(f, "{:#}", x)` → `f.WriteAlternate(x)`;
  - `x.fmt(f)` → `x.Fmt(f)` (repassa os flags);
  - `f.alternate()` → `f.Alternate`;
  - `write!(f, "a {} b", x)` → `f.WriteStr(FormattableString.Invariant($"a {x} b"))`.

**Dialect:**
- `fn x(&self) -> T` **sem parâmetros** → propriedade `public virtual T X => <padrão Rust>;`.
- Com parâmetros → `public virtual T X(...)`.
- Hooks que recebem `&Parser`/`&mut Parser` → métodos `virtual` em `Dialect.Parser.cs` (Fase 2).
- `dialect_of!(self is A | B)` → `this is A or B` (ou `dialect is A or B`).

## 8. Erros e controle de fluxo

| Rust | C# |
|---|---|
| `Result<T, TokenizerError>` | Retorna `T` e lança `TokenizerException` (carrega o `TokenizerError`) |
| `Result<T, ParserError>` | Retorna `T` e lança `ParserException` (Fase 2) |
| `?` | Nada: a exceção propaga |
| `Err(...)` | `throw` com **mensagem idêntica** ao Rust |
| `.is_err()` / `.ok()` / `match` sobre `Result` | `try/catch` local **só** da exceção específica |
| `panic!`, `unreachable!`, `unwrap()` "impossível" | `throw new InvalidOperationException(...)` (bug) |
| `if let Some(x) = opt` | `if (opt is { } x)` |
| `while let Some(x) = it.next()` | `while` com atribuição explícita |
| `loop { … }` | `while (true)` |
| Labels (`'outer:`) | Flag ou método extraído; registre na seção 13 |

**Mensagens de erro:** o texto é contrato, porque testes do Rust comparam mensagens exatas. Copie o `format!` original. `{:?}` exige replicar o formato `Debug` do Rust; crie helper e registre aqui.

## 9. Coleções, iteradores e closures

- Em tokenizer e parser (caminhos quentes), prefira laços explícitos a LINQ.
- Iteradores encadeados em código frio (AST, `Display`) podem virar LINQ.
- Closures de `maybe_parse(|p| …)` e similares → `Func<Parser, T>`.
- `clone()` → normalmente nada, porque records são imutáveis. Para estado mutável, faça cópia explícita.

## 10. Strings e Unicode

- Rust `char` = *Unicode scalar value*. C# `char` = unidade UTF-16. Fora do BMP, **um `char` Rust = dois `char` C#**.
  - Preserve a semântica do Rust quando for barato. Por exemplo, `Location.Column` avança 1 por *scalar value*, ou seja, uma vez por par substituto.
  - Quando não for barato, registre em `divergencias.md` e marque o teste.
- Índices de byte do Rust (`&s[a..b]`, `len()`) **não** são índices de `char`. Traduza a intenção.
- `to_uppercase()`/`to_lowercase()` → `ToUpperInvariant()`/`ToLowerInvariant()`. A diferença em casos especiais (`ß`) está registrada em `divergencias.md`.
- Classificação de caracteres:
  - `is_alphabetic()` → `char.IsLetter`;
  - `is_alphanumeric()` → `char.IsLetterOrDigit`;
  - `is_whitespace()` → `char.IsWhiteSpace`;
  - `is_ascii_*()` → comparação explícita de intervalo;
  - `is_digit(10)` / `is_ascii_digit()` → `c >= '0' && c <= '9'`. **Nunca** `char.IsDigit`, que aceita dígitos Unicode.
  - **Decisão do responsável (2026-10-14):** a classificação Unicode é a do .NET. As diferenças com o Rust estão aceitas e registradas (DIV-003, DIV-004). Não crie tabelas Unicode próprias para imitar o Rust sem nova decisão.
- Números para texto: sempre `CultureInfo.InvariantCulture`.

## 11. Restrições de `netstandard2.0`

**Indisponíveis:**
- `string.Contains/StartsWith/EndsWith(char)` e `Split(char, opções)` → use sobrecargas com `string`;
- `HashCode`;
- `Enum.GetValues<T>()`/`GetNames<T>()`;
- `Span<T>`/`ReadOnlySpan<T>` (exigiriam pacote externo);
- `char.IsAscii*`;
- `Index`/`Range` sobre `string` (`s[1..^1]`);
- `ArgumentNullException.ThrowIfNull`;
- `Dictionary.TryAdd`/`GetValueOrDefault` (este último existe só como extensão de `IReadOnlyDictionary` em versões novas: evite).

**Permitidos** (C# 14 + PolySharp): records, `init`, pattern matching, `switch` expression, `is not null`.

**Proibidos:** default interface members, membros `static abstract`, `required` na API pública.

## 12. Documentação e comentários

- **Doc comments `///` do Rust: em inglês, verbatim**, convertidos para XML doc (`<summary>`, `<c>`, `<see cref>`). Isso dá rastreabilidade e facilita o diff com o upstream.
- **Comentários novos** (explicação do port, decisões): **pt-BR**.
- Comentários `//` do Rust dentro de funções: mantidos em inglês quando explicam lógica.
- **Tags buscáveis** (prática do Lucene.NET; ver `referencias-e-boas-praticas.md` P3):
  - `// PORT-DEVIATION DIV-NNN: <motivo>` em toda linha/bloco que diverge deliberadamente do Rust. O ID aponta para `divergencias.md`.
  - `// PORT-TODO: <o quê>` para pendências conhecidas, por exemplo um trecho que depende de algo ainda não portado.
  - Para localizar: `grep -rn "PORT-DEVIATION\|PORT-TODO" src/`.

## 13. Casos decididos (memória de tradução)

Registre aqui toda decisão de tradução não óbvia: construto Rust, forma C#, motivo e primeiro uso. **Consulte antes de traduzir algo parecido.**

| # | Construto Rust | Tradução C# | Motivo | Primeiro uso |
|---|---|---|---|---|
| 1 | `define_keywords!` (macro declarativa) | Gerado por `tools/codegen/Generate-Keywords.ps1` | Não esquecer keywords novas do upstream | `Keywords.g.cs` |
| 2 | `ALL_KEYWORDS.binary_search(&s)` + `ALL_KEYWORDS_INDEX[i]` | `Keywords.Lookup(s)` (busca binária ordinal) | Mesma ordenação por bytes do Rust para ASCII | `Keywords.cs` |
| 3 | `impl Display for Keyword` | Extensão `Keyword.ToSql()` | `enum` C# não sobrescreve `ToString()` | `Keywords.cs` |
| 4 | `core::fmt::{Display, Formatter}` | `IDisplay` + `Formatter` | Tradução mecânica dos 455 `impl Display` | `Support/Formatter.cs` |
| 5 | Funções livres de um módulo que tem um tipo homônimo (`tokenizer::keyword_lookup`, funções soltas de `parser`) | Membros `static` (com a mesma visibilidade) do tipo homônimo (`Tokenizer`, `Parser`) | Evita classes `TokenizerModule` artificiais; a busca pelo nome Rust continua direta | `Tokenizer/Tokenizer.cs` |
| 6 | `#[derive(Copy, Eq, Hash, Ord)]` em struct pequeno | `readonly record struct` + `IComparable<T>` com ordem lexicográfica dos campos | Mesmo comportamento de cópia/igualdade/ordenação do derive | `Tokenizer/Location.cs` |
| 7 | `fn new(..) -> Self` / outras fábricas `fn x() -> Self` / `impl From<A> for B` | Construtor / método estático `X()` / método estático `B.From(A)` (sem conversão implícita) | Mantém nomes buscáveis; conversões implícitas escondem chamadas | `Tokenizer/Location.cs` |
| 8 | Identidade de dialeto: `fn dialect(&self) -> TypeId`, `impl dyn Dialect { fn is::<T>() }`, macros `dialect_of!`/`dialect_is!` e `derive_dialect!` | Sem código: teste de tipo do C# (`dialect is A or B`, §7). Não existem `Dialect()` nem `Is<T>()`. Os dialetos embutidos **não** são `sealed`: um wrapper ou derivado que deve ser reconhecido como o dialeto base herda dele (equivale a `derive_dialect!(.., preserve_type_id = true)`) | A §7 já fixou `is`; manter um `TypeId` sobrescrevível criaria dois mecanismos de identidade. Consequência registrada em DIV-002 | `Dialects/Dialect.cs` |
| 9 | `pub struct XDialect;` / `XDialect {}` com `#[derive(Debug, Default, Clone, Copy, PartialEq, Eq, Hash, PartialOrd, Ord)]` + `impl Dialect for XDialect` | `public class XDialect : Dialect` (não selada), construtor padrão, sem igualdade estrutural nem `ToString()` próprio. Outros tipos de topo do módulo (`enum Precedence`) vão para arquivo próprio (`Dialects/Precedence.cs`) | O parser só usa `&dyn Dialect`; nada compara dialetos. O `Debug` só aparece em mensagens de `test_utils.rs` (use `GetType().Name` lá) | `Dialects/*Dialect.cs` |
| 10 | Membro do trait `Dialect` sem parâmetros com prefixo `get_` e retorno `&[T]` (`get_reserved_keywords_for_select_item_operator`) | Propriedade com o nome mecânico, **mantendo** o `Get` (`GetReservedKeywordsForSelectItemOperator`), tipo `IReadOnlyList<T>`; padrão `Array.Empty<T>()`; overrides devolvem um `private static readonly T[]` | Regra mecânica da §4 e §7 sem exceções de gosto | `Dialects/Dialect.cs`, `OracleDialect.cs` |
| 11 | `const X: &[Keyword] = &[..]` / `const X: [Keyword; N]` / `const X: u8 = n` de módulo (fora do `impl`) | `private static readonly Keyword[] X` / `private const byte X` (PascalCase) no topo da classe do arquivo, na ordem do Rust. São portadas mesmo quando o único uso ainda está adiado (são só dados) | Mantém o arquivo comparável ao Rust; `static readonly` sem uso não gera aviso | `PostgreSqlDialect.cs`, `SnowflakeDialect.cs` |
| 12 | `slice.contains(&x)` (inclusive `keywords::RESERVED_FOR_*`) | `lista.Contains(x)` (LINQ `Enumerable.Contains`, que delega a `ICollection<T>.Contains` do array) | Tradução direta; custo de busca linear igual ao Rust | `Dialect.IsReservedForIdentifier` |
| 13 | `matches!(x, A \| B \| 'a'..='z')` | Padrão C#: `x is A or B or (>= 'a' and <= 'z')` | Correspondência 1:1 com o padrão do Rust, inclusive intervalos | `DatabricksDialect.cs`, `PostgreSqlDialect.IsCustomOperatorPart` |
| 14 | Classificação ASCII e intervalos de `char`: `ch.is_ascii()`, `!ch.is_ascii()`, `('\u{X}'..='\u{ffff}').contains(&ch)` | `ch <= '\u007f'`, `ch > '\u007f'`, `ch >= '\uX' && ch <= '\uffff' && !char.IsSurrogate(ch)` | Um `char` do Rust nunca é substituto (U+D800–U+DFFF) e o intervalo exclui o que está fora do BMP; sem `!char.IsSurrogate`, as duas metades de um par seriam aceitas. Já `!is_ascii()` aceita o não-BMP no Rust, e `ch > '\u007f'` aceita as duas metades: mesmo resultado | `SQLiteDialect.IsIdentifierStart`, `MySqlDialect.IsIdentifierStart` |
| 15 | Construir um dialeto sem campos só para chamar um método (`PostgreSqlDialect {}.is_identifier_start(ch)`) | Instância `private static readonly` em cache, chamada `<Tipo>Instance` (`PostgreSqlDialectInstance`), usada no lugar do literal | Custo zero no Rust; em C# evitaria uma alocação por caractere no tokenizer. O dialeto não tem estado, então o comportamento é o mesmo | `RedshiftSqlDialect.cs` |
| 16 | Membro ou função que ainda não pode ser portado (depende de `Parser`, da AST ou de tipo de outra área) | Comentário na posição exata do Rust: `// PENDENTE (<motivo>): <nome_rust> (<arquivo>.rs:<linha>).` e entrada na lista de pendências de `progresso.md` | Mantém a ordem dos membros igual à do Rust e deixa a pendência localizável por busca | `Dialects/*.cs` |
| 17 | `Peekable<Chars<'_>>`: o `State` do tokenizer e o parâmetro de `Dialect::peek_nested_delimited_identifier_quotes` | `struct` público mutável `PeekableChars` (`Peek`/`Next`/`Nth`/`SkipWhile`). `State` (interno) guarda um `PeekableChars` + `Line`/`Col`. `chars.peekable.clone()` → `chars.Peekable` (a propriedade devolve uma **cópia**); `chars.peekable.clone().nth(n)` → `chars.Peekable.Nth(n)`; `it.skip_while(p).peekable()` → `it.SkipWhile(p)` | Copiar o `struct` equivale ao `.clone()` do iterador, então avançar a cópia não afeta o original, como no Rust. Percorre unidades UTF-16; `State.Next()` não incrementa `Col` na 2ª metade de um par substituto (§10) | `Support/PeekableChars.cs`, `Tokenizer/State.cs` |
| 18 | `char` do Rust gerado de um número ou que pode estar fora do BMP: `Option<char>` de `char::from_u32`, escapes `\U0010FFFF`; `Token::Char(char)` | `int` / `int?` com o scalar value, anexado com `RustChar.PushScalar`; `Token.Char` guarda `string` com **um** scalar value (nomes.md). Pares substitutos no fallback `other =>` do tokenizer viram **um** `Token.Char` | Preserva a semântica do Rust onde é barato (§10); o resto está em DIV-003 | `Tokenizer/Unescape.cs`, `Tokenizer/Tokenizer.cs` |
| 19 | Métodos da std do Rust sem equivalente exato no .NET: `is_ascii_digit`, `is_ascii_hexdigit`, `is_ascii_lowercase/uppercase`, `to_ascii_uppercase`, `to_digit(radix)`, `char::from_u32`, `u32::from_str_radix`, `String::push(char)` | `RustChar.IsAsciiDigit`, `IsAsciiHexdigit`, `IsAsciiLowercase/Uppercase`, `ToAsciiUppercase`, `ToDigit`, `FromU32`, `U32FromStrRadix` (aceita `+` inicial, `null` em erro/estouro), `PushScalar` (`internal`, em `Support/RustChar.cs`). `is_numeric()` → `char.IsNumber` (N*: Nd, Nl, No, igual ao Rust); `is_alphanumeric()` → `char.IsLetterOrDigit` (§10; diferenças em DIV-004) | Nome mecânico do método Rust, fácil de buscar; evita reimplementar o mesmo teste em cada arquivo | `Support/RustChar.cs` |
| 20 | Itens privados de módulo (sem `pub`) usados por outro tipo do mesmo arquivo `.rs` ou pelos testes (`keyword_lookup`, `peeking_take_while`, `Word::matching_end_quote`, `Token::make_word_owned`, `struct State`, `struct Unescape`...) | `internal` (funções livres como membros `internal static` do tipo homônimo, caso 5; tipos `internal`, um por arquivo) | A visibilidade de módulo do Rust é mais larga que `private`; os testes os acessam via `InternalsVisibleTo` | `Tokenizer/*.cs` |
| 21 | `impl PartialEq<B> for A` entre tipos diferentes (`TokenWithSpan` × `Token`) | Método `public bool Equals(B? other)` em `A` (e o simétrico em `B`), **sem** operadores `==`/`!=` | Operadores `==(A, B)` tornariam `x == null` ambíguo (CS0121) | `Tokenizer/TokenWithSpan.cs`, `Tokenizer/Token.cs` |
| 22 | Tipo com `impl Debug` manual e **sem** `Display` (`Span`) | `ToDebugString()` com o formato do `Debug`, e `ToString()` delegando a ele | Sem `Display` no Rust, nada formata o tipo com `{}`; o texto do `Debug` evita o `ToString()` sintetizado do record | `Tokenizer/Span.cs` |
| 23 | `#[derive(PartialOrd, Ord)]` em enums/structs de dados que nenhum código portado ordena (`Token`, `Word`, `Whitespace`, `TokenWithSpan`, `DollarQuotedString`, `QuoteDelimitedString`) | Não traduzido (sem `IComparable`) até surgir um uso; `Location` e `Span` têm (caso 6), porque `Span::union` usa `min`/`max` | Evita implementar a ordem de variantes de enum sem necessidade | `Tokenizer/*.cs` |
| 24 | `fn with_x(mut self, ..) -> Self` (builder que consome `self`) em `struct` com estado mutável | Método que altera a instância e devolve `this` (`Tokenizer.WithUnescape`) | Mesmo uso encadeado do Rust (`new Tokenizer(d, sql).WithUnescape(false).Tokenize()`) | `Tokenizer/Tokenizer.cs` |
| 25 | Campos privados de `struct` com estado mutável (`Tokenizer { dialect, query, unescape }`, `Unescape { chars }`) | Campos `private` em `_camelCase` (`_dialect`, `_query`, `_unescape`) | Não fazem parte da API; a §4 (propriedade PascalCase) vale para campos `pub` | `Tokenizer/Tokenizer.cs` |
| 26 | `match ch { x @ 'a' \| x @ 'b' if cond => .., c if pred(c) => .., _ => .. }` com guardas | **Um** `switch` com `case 'a' or 'b' when cond:` / `case var c when pred(c):` na mesma ordem (guarda falsa cai no caso seguinte, como no Rust); cada seção entre chaves; o binding `x @` vira `var x = ch;`. `match chars.peek() { Some('x') => .. }` → `switch (chars.Peek()) { case 'x': .. }` | Correspondência 1:1 com a ordem e as guardas do Rust; as chaves evitam colisão de nomes entre seções | `Tokenizer.NextToken` |
| 27 | Parâmetros `impl Fn(char) -> bool`, `impl FnMut(T) -> T`, genéricos `F: Fn(String) -> Token`; construtor de variante passado como função (`Token::SingleQuotedString`) | `Func<char, bool>`, `Func<T, T>`, `Func<string, Token>`; lambda `v => new Token.SingleQuotedString(v)` | Tradução direta de closures (§9) | `Tokenizer/Tokenizer.cs` |
| 28 | Parâmetros genéricos constantes (`fn f<const N: usize>()`, `byte_to_char::<16>`) | Parâmetro comum (`UnescapeUnicode(int num)`, `ByteToChar(string s, uint radix)`) | C# não tem genéricos de valor | `Tokenizer/Unescape.cs` |

## 14. Checklist de cada unidade portada

1. Li o trecho Rust inteiro, incluindo docs e testes, na versão de `UPSTREAM.json` (`upstream/sqlparser-rs`).
2. Consultei `nomes.md`, `divergencias.md` e a seção 13.
3. Tipos, membros, ordem e nomes seguem o Rust e as seções 4–7.
4. O arquivo tem o cabeçalho da seção 3.
5. Portei os testes Rust correspondentes (skill `port-rust-tests`).
6. `dotnet build` sem warnings (`TreatWarningsAsErrors`).
7. `dotnet test` verde em **net48, net8.0 e net10.0**.
8. Atualizei `progresso.md`, e também `nomes.md`, `divergencias.md` e a seção 13 se aplicável.
