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
| `dialect/mod.rs` | `Dialects/Dialect.cs` (inclusive os hooks que recebem `Parser`, na posição do Rust) | `SqlParser.Core.Dialects` |
| `dialect/<nome>.rs` | `Dialects/<Nome>Dialect.cs` (mesmo nome do struct Rust) | `SqlParser.Core.Dialects` |
| `parser/mod.rs`, `alter.rs`, `merge.rs` | `Parser/Parser*.cs` (`partial class Parser`; a divisão por seção fica registrada em `progresso.md`) | `SqlParser.Core` |
| `ast/<modulo>.rs` | `Ast/<Modulo>/<Tipo>.cs` (pasta em PascalCase: `Ast/Mod/`, `Ast/Query/`, `Ast/Ddl/`, `Ast/DataType/`…; um tipo de topo por arquivo; tipos pequenos e muito ligados, como um enum usado só por um struct, podem ficar no arquivo desse struct) | `SqlParser.Core.Ast` |
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

1. **Enum sem dados e sem nenhum `impl`** (nem `Display`) → `enum` C# com os mesmos nomes. Se tiver `impl Display` ou métodos, use a regra 2 (§13, caso 37).
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
  - `write!(f, "a {} b {:#}", x, y)` → `f.Write($"a {x} b {y:#}")` (handler de interpolação; §13, caso 30). É a forma **preferida** para todo `write!` com texto e lacunas.
  - O código do tokenizer (Fase 1) usa `f.WriteStr(FormattableString.Invariant(...))` em alguns pontos. Ali o resultado é o mesmo, porque tokens não têm pretty-print. **Na AST, use sempre `f.Write($"...")`.**

**Dialect:**
- `fn x(&self) -> T` **sem parâmetros** → propriedade `public virtual T X => <padrão Rust>;`.
- Com parâmetros → `public virtual T X(...)`.
- Hooks que recebem `&Parser`/`&mut Parser` → métodos `virtual` no próprio `Dialect.cs`, na posição do Rust (§13, caso 53).
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
| 29 | Funções livres de um módulo **sem** tipo homônimo (`ast::display_separated`, funções de `ddl.rs`, `display_utils::indented_list`) | `static (partial) class <Modulo>Module` (`AstModule`, `DdlModule`). Exceção histórica: `DisplayUtils`. Com tipo homônimo, vale o caso 5 (`value::escape_quoted_string` → `Value.EscapeQuotedString`). Chamadas com `using static SqlParser.Core.Ast.AstModule;` no topo do arquivo, para ficar igual ao Rust. `AstModule` é `partial`: cada arquivo C# que porta um trecho de `mod.rs` acrescenta suas funções livres num `partial` próprio | Uma classe chamada `Ast` dentro do namespace `SqlParser.Core.Ast` quebraria as referências `Ast.X` | `Ast/Mod/AstModule.cs`, `DisplayUtils/DisplayUtils.cs` |
| 30 | `write!(f, "a {} b {:#} c", x, y)` e `format!` em `impl Display` | `f.Write($"a {x} b {y:#} c")`, com o handler de interpolação do `Formatter`, que escreve cada pedaço no mesmo destino, com flags novos. `{:#}` vira `{y:#}`. `x.fmt(f)` vira `x.Fmt(f)` (ou `f.FmtInPlace(x)` se `x` for genérico). `f.write_str(s)` vira `f.WriteStr(s)`. **Não use** `FormattableString`/`string.Format`/`ToString()` dentro de `Display`: quebraria o pretty-print (indentação). Lacunas com `null`, ponto flutuante ou `enum` C# lançam exceção: trate explicitamente | Tradução mecânica dos 451 `impl Display` da AST, com o pretty-print (`{:#}`) idêntico | `Support/Formatter.cs`, `Ast/Mod/ObjectName.cs` |
| 31 | `Vec<T>` em tipos da AST | `Sequence<T>` (`struct` imutável, igualdade estrutural, `default` = vazio). `vec![a, b]` → `Sequence.Of(a, b)`; `vec![]` → `Sequence<T>.Empty`; `Vec` montado com `push` → `List<T>` + `.ToSequence()`; `.len()` → `.Count`; `.is_empty()` → `.IsEmpty`; `.first()`/`.last()` → `.First`/`.Last`; `&v` passado a `display_*` → `v` (é um `IReadOnlyList<T>`) | `List<T>` compararia por referência e mudaria a semântica de `PartialEq` dos records | `Support/Sequence.cs` |
| 32 | `#[cfg_attr(feature = "visitor", visit(with = "visit_x"))]` | `[VisitWith("visit_x")]` no tipo; `[property: VisitWith("visit_x")]` no parâmetro posicional do campo | Preserva a informação para o visitor (gerado depois), sem reler o Rust | `Support/VisitWithAttribute.cs` |
| 33 | Struct unitário (`pub struct NewLine;`) usado como valor | `sealed class` com construtor privado e `public static readonly X Instance`; uso: `NewLine.Instance` | Evita alocação e mantém um valor único, como no Rust | `DisplayUtils/NewLine.cs` |
| 34 | `impl Deref`/`DerefMut` (ex.: `Parens<T>` → `T`) | Não traduzido: o código acessa o campo explicitamente (`parens.Content`) | C# não tem desreferência implícita; um operador de conversão esconderia a chamada | `Ast/Mod/Parens.cs` |
| 35 | Tuple struct `pub struct X(pub A);` / `pub struct X(pub A, pub B);` | `sealed record X(A Value)`. Com 2+ campos, nomes descritivos registrados em `nomes.md`, como nas variantes-tupla (§6) | Mesma regra das variantes-tupla | `Ast/Mod/ObjectName.cs` |
| 36 | `struct` de dados da AST (`#[derive(Debug, Clone, PartialEq, Eq, Hash)]`) | `public sealed record X(T1 Campo1, T2 Campo2, ...)` **posicional**, campos na ordem do Rust, sem valores padrão; `Box<T>` → `T`; `Option<Box<T>>` → `T?`. Construção com argumentos nomeados quando houver mais de 3 campos: `new Select(SelectToken: ..., Distinct: ..., ...)`. `impl X { fn metodo(&self) }` → métodos no record. Doc dos campos → `<param name="Campo">` | O construtor posicional obriga a passar todos os campos, como o literal de struct do Rust (que não compila se faltar campo) | `Ast/Mod/Ident.cs`, `ObjectNamePartFunction` |
| 37 | Enum sem dados **com** `impl Display` (ou com métodos em `impl`) | `abstract record` + `sealed record` unitários (`public sealed record Inner : JoinType;`), como os enums com dados. `enum` C# só para enums Rust sem dados e **sem** `impl` algum (ex.: `Precedence`) | Um `enum` C# interpolado imprimiria o nome do membro (divergência silenciosa); o `Formatter` lança exceção se isso acontecer | `Ast/` |
| 38 | Variante aninhada com o nome de um tipo de topo (`Expr::Value(ValueWithSpan)` esconde `Value` dentro de `Expr`; `Statement::Insert(Insert)`) | Dentro do record que contém a variante, o tipo de topo é qualificado como `Ast.X` (`Ast.Value.EscapeQuotedString(..)`, `sealed record Insert(Ast.Insert Value) : Statement`). Em propriedades homônimas (`Ident.Value`), o mesmo vale para o tipo `Value` | Regra de busca de nomes do C#: o membro aninhado vence | `Ast/Mod/Ident.cs` |
| 39 | `assert!(cond)` / `assert_eq!` em código de produção | `if (!(cond)) throw new InvalidOperationException("assertion failed: <cond em Rust>");` | O `assert!` do Rust falha também em release; `Debug.Assert` sumiria no build de produção | `Ast/Mod/Ident.cs` |
| 40 | `impl Visit`/`impl VisitMut` manual | Comentário `// PORT-TODO: impl Visit/VisitMut ... entra com o visitor (ast/visitor.rs)` na posição do Rust | O visitor é gerado/portado depois da AST | `Ast/Mod/Ident.cs` |
| 41 | Campo de struct ou de variante-struct com o **mesmo nome** do tipo que o contém (`Statement::Comment { comment, .. }`) | Sufixo `Value` no parâmetro (`string? CommentValue`), registrado em `nomes.md` | CS0542 | — |
| 42 | `#[derive(Default)]` usado com `X::default()` ou `..Default::default()` | `public static X Default()` no record, com os valores do `Default` do Rust (`None` → `null`, `Vec` → vazio, `bool` → `false`); `..Default::default()` → `X.Default() with { Campo = ... }` | Construção explícita, buscável pelo nome Rust | — |
| 43 | Variante (tipo aninhado) com o **mesmo nome do enum** que a contém (`ParserError::ParserError(String)`) | Sufixo `Value` no nome do tipo aninhado: `ParserError.ParserErrorValue(string Value)` | CS0542; mesmo sufixo do caso 41 | `Parser/ParserError.cs` |
| 44 | Macros de log (`debug!`, `trace!`, crate `log`) | Não traduzidas (comentário opcional) | Sem dependência de logging no Core | `Parser/Parser.cs` |
| 45 | `Result<T, ParserError>` no parser | Retorna `T` e lança `ParserException(ParserError)`. `Ok(x)` → `return x;`. `?` → nada. `return self.expected("x", tok)` → `return Expected<T>("x", tok);` (lança; o tipo genérico é o de retorno do método); em método `void`, `Expected("x", tok); return;`. Idem `ExpectedRef`/`ExpectedAt`. `parser_err!(msg, loc)` → `throw ParserErr(msg, loc);` (ou `return` em contexto de expressão com `throw`). `Err(ParserError::ParserError(s))` → `throw new ParserException(new ParserError.ParserErrorValue(s));`. `let _guard = self.recursion_counter.try_decrease()?;` → `using var _guard = _recursionCounter.TryDecrease();`. `#[cfg_attr(feature = "recursive-protection", recursive::recursive)]` → não traduzido (o `TryDecrease` protege a pilha, DIV-005) | Fluxo linha a linha igual ao Rust; mensagens idênticas | `Parser/Parser.TokenHelpers.cs` |
| 46 | `format!("{:?}", keyword)` / `{x:?}` de `Keyword` | `keyword.ToString()`: o nome da variante, igual ao `Debug` do Rust (o `Display`, `ToSql()`, difere em `END-EXEC`) | Mensagens de erro como `Expected: AND, found: ...` | `Parser.ExpectKeyword` |
| 47 | `Self::parse::<T>(s, loc)` (`T: FromStr`) | `Parse<T>(s, loc)`, que suporta `string`, `ulong` (u64), `uint` (u32) e `long` (i64), com a mensagem do Rust: `Could not parse '{s}' as u64: invalid digit found in string{loc}` | Texto do `ParseIntError` e do `type_name` do Rust | `Parser.Parse<T>` |
| 48 | `maybe_parse(\|p\| ...)` / `try_parse` / `Result<Option<T>, E>` | `MaybeParse(p => ...)` para `T` referência; `MaybeParseValue(p => ...)` para `T` tipo-valor (`Option<u64>` → `ulong?`); `TryParse(p => ...)`. Função que devolve `Result<Option<T>>` → `T?` (`Nullable<T>` em tipo-valor) e lança em erro | `T?` genérico sem restrição não vira `Nullable<T>` em C# | `Parser.MaybeParse` |
| 49 | Tipo da AST com nome igual a um tipo de `System` (`Action`, `Array`...) usado fora do namespace `SqlParser.Core.Ast` | Qualificar: `Ast.Action`, `Ast.Array` | `ImplicitUsings` traz `System`: o nome ficaria ambíguo (CS0104) | `Parser.ParseActionsList` |
| 50 | Assinaturas do parser: `-> Result<Vec<T>>`; parâmetros `&[Keyword]`/`&[Token]`; closures | Retorno `Sequence<T>` (acumule num `List<T>` e devolva `.ToSequence()`); `params Keyword[]`/`params Token[]` (`ParseKeywords(Keyword.ORDER, Keyword.BY)`); `FnMut(&mut Parser) -> Result<T>` → `Func<Parser, T>`; `Fn(&Keyword, &mut Parser) -> bool` → `Func<Keyword, Parser, bool>`; `Parser::parse_x` passado como função → `p => p.ParseX()`; `Self::f` estático → grupo de método `F` | Construir a AST direto com o retorno, sem conversões espalhadas | `Parser/Parser.TokenHelpers.cs` |
| 51 | `match &self.peek_token_ref().token { Token::Word(w) if .. => .., Token::Comma => .. }`; `self.peek_tokens()` com padrão de fatia; comparações de token | `switch (PeekTokenRef().Token) { case Token.Word { Value: var w } when ..: ...; case Token.Comma: ... }`; `var t = PeekTokens(2);` + `t[0] is Token.X`; `tok == &Token::X` → `tok is Token.X` (variante unitária) ou `tok.Equals(new Token.X(..))`; `peek_token_ref() == &Token::X` (TokenWithSpan × Token) → `PeekTokenRef().Equals(token)` (caso 21). `peek_token()` e `peek_token_ref()` devolvem a mesma referência imutável | Correspondência 1:1 com os `match` do Rust (caso 26) | `Parser/Parser.TokenHelpers.cs` |
| 52 | Visibilidade de funções do `Parser`: `pub fn` / `pub(crate) fn` / `fn` | `public` / `internal` / `internal` | Funções privadas do Rust são usadas pelos testes do módulo e por `alter.rs`/`merge.rs` (caso 20) | `Parser/*.cs` |
| 53 | Hooks do `Dialect` que recebem o `Parser`: `fn parse_prefix(&self, parser: &mut Parser) -> Option<Result<Expr, ParserError>>`, `parse_infix(.., expr: &Expr, precedence: u8)`, `parse_statement`, `parse_column_option`, `get_next_precedence -> Option<Result<u8>>`, `get_next_precedence_default -> Result<u8>`, `is_column_alias(&self, kw: &Keyword, parser: &mut Parser) -> bool`... | Métodos `virtual` no próprio `Dialects/Dialect.cs`, na posição do Rust (o arquivo `Dialect.Parser.cs` previsto antes não é usado): `Expr? ParsePrefix(Parser parser)` (`None` → `null`, `Some(Err)` → lança), `Expr? ParseInfix(Parser parser, Expr expr, byte precedence)`, `Statement? ParseStatement(Parser parser)`, `byte? GetNextPrecedence(Parser parser)`, `byte GetNextPrecedenceDefault(Parser parser)`, `bool IsColumnAlias(Keyword kw, Parser parser)`. No parser: `if (_dialect.ParsePrefix(this) is { } prefix) return prefix;` | `Option<Result<T>>` vira `T?` + exceção, como no caso 48 | `Dialects/Dialect.Parser.cs` |
| 54 | Campos privados do `Parser` (`self.index`, `self.tokens`, `self.state`, `self.dialect`, `self.options`, `self.recursion_counter`, caches `failed_*`) | `_index`, `_tokens`, `_state`, `_dialect`, `_options`, `_recursionCounter`, `_failedPrefixPositions`... (`BTreeMap` → `SortedDictionary`, `BTreeSet` → `SortedSet`) | Caso 25 | `Parser/Parser.cs` |
| 55 | `Option` aninhado (`Option<Option<T>>`, `Option<Result<Option<T>>>`) | `Option<T?>` (`Support/Option.cs`: `Option<T>.None`, `Option<T>.Some(x)`, `.IsSome`, `.Value`). Fora desse caso, `Option<T>` continua `T?` | `T??` não existe em C#: só um tipo explícito distingue `None` de `Some(None)` | `Dialect.ParseColumnOption` |
| 56 | Identificador Rust que é palavra reservada do C# (`explicit`, `params`, `object`, `operator`, `event`, `string`, `base`, `default`...) | Prefixo `@` (`bool @explicit`), mantendo o nome | Mantém o nome buscável sem inventar sinônimos | `Dialect.IsSelectItemAlias` |
| 57 | Faixas do Rust (`a..b`, `a..=b`, `a..`, `..b`, `..=b`, `..`) e `RangeBounds`/`Bound`; `impl From<NossoTipo> for TipoDaStd` | `RangeBounds.Range(a, b)`, `RangeInclusive`, `RangeFrom`, `RangeTo`, `RangeToInclusive`, `RangeFull<T>()` (`Support/RangeBounds.cs`), `Bound<T>.Included/Excluded/Unbounded`; a conversão para tipo da std vira método de instância `To<Tipo>()` no nosso tipo (`Comments.ToVec()`) | C# não tem sintaxe de faixa genérica nem permite estender `List<T>` com fábrica estática | `Ast/Comments/Comments.cs` |
| 58 | Propriedade de parâmetro posicional que esbarra num membro **herdado** (CS8866): campo de variante com o nome de outra variante do mesmo enum (`Statement::AlterView { query }` × `Statement::Query`); variante-tupla `Value` num enum que tem a variante `Value` (todas as variantes-tupla de `Expr`); campo com o nome de um método da base (`ConditionalStatements::Sequence { statements }`) | Manter o nome Rust e declarar a propriedade explicitamente: `public sealed record AlterView(.., Ast.Query Query, ..) : Statement { public new Ast.Query Query { get; init; } = Query; }` | Construtor, `with`, `Deconstruct` e igualdade continuam funcionando | `Ast/Mod/Statement.cs`, `Ast/Mod/Expr.cs` (onda A: A1, A2, A7) |
| 59 | Membro com o nome de um membro sintetizado pelo record: `clone` (CS8859); `equals`, `get_hash_code`, `to_string`, `print_members`, `deconstruct`, `equality_contract` (CS0102) | Sufixo `Value`: `CloneValue`, `EqualsValue` | Mesmo sufixo dos casos 41 e 43 | `Statement.CreateDatabase`, `AlterTableOperation.Algorithm` |
| 60 | Métodos de builder com o nome de um campo (`fn location(mut self, ..)` com campo `location`) | Prefixo `Set` (`SetLocation`); o builder é `sealed record` com propriedades `{ get; set; }` e cada método altera e devolve `this` (caso 24) | CS0102 | `Ast/Helpers/CreateTableBuilder.cs`, `CreateDatabaseBuilder.cs` |
| 61 | `format!`/`to_string()` montando pedaços de um `write!` (`let x = if c { format!(" {}", y) } else { String::new() }; write!(f, "..{x}..")`) | Escrita condicional direta, na mesma posição da saída (`if (c) f.Write($" {y}");`). Quando o Rust **precisa** de um `String` (ex.: `Vec<String>` + `join`), use `Formatter.Format(x)`, que equivale a `format!("{}", x)` | O texto é o mesmo e o pretty-print não é afetado | `Statement.Display.cs`, `CreateView.cs`, `PipeOperator.cs` |
| 62 | `writeln!(f, "X")` / `write!(f, "")` | `f.Write($"X\n")` (um único pedaço) / nada (comentário no lugar) | O `Indent` reage só a `"\n"` isolado, como no Rust | `Statement.Display.cs` |
| 63 | `match` sobre tupla; `match` interno sobre `Option`; braço `_ => ()` | `switch` sobre tupla C# com padrões posicionais (`case (true, { } q):`, `case (x, null):`); `switch` aninhado com `case { } v:`/`case null:`; `_ => ()` → `default: break;` (o `default: throw` fica só para `match` exaustivo sem curinga) | Um `match` = um `switch`, na mesma ordem (caso 26) | `Interval.cs`, `Statement.Display.cs`, `CreateTable.cs` |
| 64 | `Option<Vec<T>>` | `Sequence<T>?` (`Nullable<Sequence<T>>`), para distinguir `None` de `Some(vec![])` | Caso 31 + §5 | `StructField.Options`, `Analyze.Partitions` |
| 65 | `impl Deref<Target=[T]>`/`AsRef<[T]>`/`IntoIterator` em enum (`OneOrManyWithParens<T>`) | A base `abstract record` implementa `IReadOnlyList<T>`; os tipos iteradores do Rust não são portados (LINQ cobre `rev`/`count`/`fold`) | Complementa o caso 34 | `Ast/Mod/OneOrManyWithParens.cs` |
| 66 | Alinhamento `{:>}` / `f.align()` | Corpo do `fmt` em `private void FmtAligned(Formatter f, bool alignRight)`; `Fmt(f)` chama com `false`; quem usava `{:>}` chama com `true` | O `Formatter` não tem alinhamento; só `KeyOrIndexDisplay` usa | `Ast/Ddl/KeyOrIndexDisplay.cs` |
| 67 | Valor `impl Display` criado na hora: função que devolve `impl fmt::Display` com struct local; `format_args!(..)` passado a `Indent(..)`; funções e structs aninhados dentro de `fn fmt` | `internal static IDisplay X(..)` + classe privada aninhada; `format_args!` → classe privada `FormatArgs : IDisplay` que embrulha `Action<Formatter>`; funções locais `static` e classes privadas com os nomes mecânicos | Mantém a escrita no mesmo destino (pretty-print correto) | `DdlModule.A5.cs`, `Ast/Query/Values.cs`, `Ast/Query/Join.cs` |
| 68 | `debug_assert!(cond)` | `#if DEBUG` + o `throw` do caso 39 + `#endif` | Só existe em build de depuração no Rust; `Debug.Assert` derrubaria o processo de teste | `Ast/Query/LimitClause.cs` |
| 69 | Igualdade com variante unitária (`x == Enum::Variant`, `matches!(x, A \| B)`) | Teste de tipo: `x is Enum.Variant` / `x is A or B` | Sem alocar; mesmo resultado | `Expr.Display.cs`, `ColumnDef.cs` |
| 70 | Constante de `enum` C# dentro de um record que tem uma propriedade homônima **anulável** (`OnCommit? OnCommit`) | Qualificar: `Ast.OnCommit.Drop` | A regra "Color Color" do C# não vale para `Nullable<T>` | `Ast/Ddl/CreateTable.cs` |
| 71 | `#[derive(Copy)]` em struct de dados da **AST** | `sealed record` (classe), não `readonly record struct`; o caso 6 vale só para os tipos pequenos do tokenizer | `Option<T>` continua sendo referência anulável, como nos demais tipos da AST | `ConstraintCharacteristics`, `WithData` |
| 72 | Nomes de variáveis Rust que o C# não aceita repetir: `if let Some(x)` consecutivos no mesmo bloco; ligações que sombreiam variáveis externas | Nomes distintos e descritivos (`isEnabled`, `changeTrackingIsEnabled`; sufixo `Text` para o texto derivado) | C# não permite sombreamento (CS0128/CS0136) | `CreateTable.cs`, `ConstraintCharacteristics.cs`, `DataType.cs` |
| 73 | `impl From<X> for Statement` fora de `mod.rs` | `partial` de `Statement` em arquivo próprio na pasta do módulo de origem (`Ast/Ddl/Statement.From.Ddl.cs`, `Ast/Dcl/Statement.Dcl.cs`); os de `mod.rs` ficam em `Ast/Mod/Statement.From.cs`. `From<Query>` e `From<Box<Query>>` viram uma sobrecarga só | `Statement` é `partial`; `Box<T>` → `T` | `Ast/Mod/Statement.From.cs` |
| 74 | Enum com dados e **sem** `Display` | `abstract record` com variantes aninhadas, sem `IDisplay` e sem `ToString()` próprio | Quem escreve é o `Display` do tipo que o contém | `CreateCollationDefinition`, `HiveDistributionStyle` |
| 75 | `impl Spanned for X` que está num módulo da AST (fora de `spans.rs`) | Comentário `// PORT-TODO: impl Spanned for X (arquivo.rs:linha) entra com os spans (ast/spans.rs).` na posição do Rust | Quem portar os spans encontra também os `impl` espalhados | `Ast/Ddl/*.cs` |
| 76 | `f.write_str(match self { .. })` / `let x = match self { .. }; write!(f, "{x}")` | Expressão `switch` (`this switch { Union => "UNION", .., _ => throw ... }`) | Um `match` num lugar só | `Ast/Query/SetOperator.cs` |
| 77 | Doc comments com markdown do Rust | ` ```sql ` → `<code>`; `[texto](url)` → `<see href="url">texto</see>`; `<url>` → `<see href="url"/>`; lista numerada → `<list type="number">`; `<`, `>` e `&` escapados | XML doc válido (o build gera a documentação) | `Ast/` |
| 78 | Desestruturação no início de um `fmt` (`let X { a, b: _, c: AttachedToken(end), .. } = self;`) | Locais com os nomes Rust (`var matchExpr = MatchExpr; var end = EndCaseToken.Value;`) + o `let` original em comentário | Mantém os nomes usados no resto da função | `Ast/Mod/CaseStatement.cs` |

## 14. Checklist de cada unidade portada

1. Li o trecho Rust inteiro, incluindo docs e testes, na versão de `UPSTREAM.json` (`upstream/sqlparser-rs`).
2. Consultei `nomes.md`, `divergencias.md` e a seção 13.
3. Tipos, membros, ordem e nomes seguem o Rust e as seções 4–7.
4. O arquivo tem o cabeçalho da seção 3.
5. Portei os testes Rust correspondentes (skill `port-rust-tests`).
6. `dotnet build` sem warnings (`TreatWarningsAsErrors`).
7. `dotnet test` verde em **net48, net8.0 e net10.0**.
8. Atualizei `progresso.md`, e também `nomes.md`, `divergencias.md` e a seção 13 se aplicável.
