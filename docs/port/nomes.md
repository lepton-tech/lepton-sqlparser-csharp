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


### AST (onda A da Fase 2): nomes não mecânicos

Consolidado das notas em `onda-a/` (o detalhe e o motivo completo de cada linha estão lá).

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `Expr::IsDistinctFrom(Box<Expr>, Box<Expr>)` | `IsDistinctFrom(Expr Left, Expr Right)` | Operandos à esquerda/direita (`spans.rs` usa `lhs`/`rhs`; mesmo par de nomes de `BinaryOp`) | `Ast/Mod/Expr.cs` |
| `Expr::IsNotDistinctFrom(Box<Expr>, Box<Expr>)` | `IsNotDistinctFrom(Expr Left, Expr Right)` | Idem | `Ast/Mod/Expr.cs` |
| `Expr::QualifiedWildcard(ObjectName, AttachedToken)` | `QualifiedWildcard(ObjectName Prefix, AttachedToken Token)` | Nomes usados pelo parser (`Expr::QualifiedWildcard(prefix, token)`) e pelo `Display` (`prefix`) | `Ast/Mod/Expr.cs` |
| `CastFormat::ValueAtTimeZone(ValueWithSpan, ValueWithSpan)` | `ValueAtTimeZone(ValueWithSpan Format, ValueWithSpan TimeZone)` | Doc Rust: "`(format, timezone)`". `Value` não serve: colidiria com a variante `CastFormat.Value` (CS8866) | `Ast/Mod/CastFormat.cs` |
| `impl Expr { pub fn value(value: impl Into<ValueWithSpan>) -> Self }` | `Expr.MakeValue(ValueWithSpan)` e `Expr.MakeValue(Value)` | CS0102: `Value` já é a variante aninhada `Expr.Value`. Duas sobrecargas cobrem o `impl Into<ValueWithSpan>` (o próprio `ValueWithSpan` e `Value`, via `ValueWithSpan.From`) | `Ast/Mod/Expr.cs` |
| Variante `Password::Password(Expr)` | `Password.PasswordValue(Expr Value)` | CS0542: tipo aninhado com o nome do tipo que o contém. Sufixo `Value`, por analogia com o caso 41 | `Ast/Mod/Password.cs` |
| Campo `Subscript::Index { index }` | `Subscript.Index(Expr IndexValue)` | CS0542/CS8866: campo com o nome da variante que o contém. Caso 41 (sufixo `Value`) | `Ast/Mod/Subscript.cs` |
| `pub struct OneOrManyWithParensIntoIter<T>` + `enum OneOrManyWithParensIntoIterInner<T>` (privado) | não portados | O enumerador de `IReadOnlyList<T>` cumpre o papel (Decisões, item 2) | `Ast/Mod/OneOrManyWithParens.cs` |
| `Set::SetNamesDefault {}` (variante-struct sem campos) | `public sealed record SetNamesDefault : Set;` | Equivale a uma variante unitária | `Ast/Mod/Set.cs` |
| `Statement::Comment { comment: Option<String>, .. }` | `Statement.Comment(.., string? CommentValue, ..)` | CS0542: membro com o nome do tipo que o contém (caso 41) | `Ast/Mod/Statement.cs` |
| `Statement::CreateSchema { clone: Option<ObjectName>, .. }` | `CreateSchema(.., ObjectName? CloneValue)` | CS8859: records não aceitam membros chamados `Clone` (o compilador reserva o nome para a cópia do `with`). Sufixo `Value`, por analogia com o caso 41 | `Ast/Mod/Statement.cs` |
| `Statement::CreateDatabase { clone: Option<ObjectName>, .. }` | `CreateDatabase(.., ObjectName? CloneValue, ..)` | CS8859 (idem) | `Ast/Mod/Statement.cs` |
| `Statement::RenameTable(Vec<RenameTable>)` | `RenameTable(Sequence<Ast.RenameTable> Value)` | Variante-tupla de 1 campo; tipo homônimo qualificado (caso 38) | `Ast/Mod/Statement.cs` |
| `Statement::Declare { stmts: Vec<Declare> }` | `Declare(Sequence<Ast.Declare> Stmts)` | Variante-struct cujo campo usa o tipo homônimo da variante: qualificado (caso 38) | `Ast/Mod/Statement.cs` |
| `AlterTableOperation::Algorithm { equals, algorithm }` | `Algorithm(bool EqualsValue, AlterTableAlgorithm AlgorithmValue)` | `Equals`: CS0102, colide com os métodos `Equals` sintetizados pelo record (nova regra, Decisões 3). `Algorithm`: CS0542 (caso 41) | `Ast/Ddl/AlterTableOperation.cs` |
| `AlterTableOperation::Lock { equals, lock }` | `Lock(bool EqualsValue, AlterTableLock LockValue)` | CS0102 / CS0542 (idem) | `Ast/Ddl/AlterTableOperation.cs` |
| `AlterTableOperation::AutoIncrement { equals, value }` | `AutoIncrement(bool EqualsValue, ValueWithSpan Value)` | CS0102 (idem) | `Ast/Ddl/AlterTableOperation.cs` |
| `UserDefinedTypeSqlDefinitionOption::Category(char)` | `Category(string Value)` | Exceção de tipo, como `Token::Char`: o parser preenche com `category_str.chars().next()`, que pode estar fora do BMP; `Value` tem sempre **um** scalar value (1 ou 2 unidades UTF-16). Saída do `Display` idêntica | `Ast/Ddl/UserDefinedTypeSqlDefinitionOption.cs` |
| `display_option` → struct local `OptionDisplay<'a, T>(&'a str, &'a str, &'a Option<T>)` | classe privada `OptionDisplay<T>` com `Prefix`, `Postfix`, `Option` | Tuple struct de 3 campos (caso 35); nomes das variáveis que o próprio `fmt` usa (`let (prefix, postfix) = (self.0, self.1)`) | `Ast/Ddl/DdlModule.A5.cs` |
| `display_constraint_name` → struct local `ConstraintName<'a>(&'a Option<Ident>)` | classe privada `ConstraintName` com `Value` | Tuple struct de 1 campo (caso 35, mecânico) | `Ast/Ddl/DdlModule.A5.cs` |
| `CreateIndex.r#async` | `Async` | Identificador bruto (§4, mecânico) | `Ast/Ddl/CreateIndex.cs` |
| `CreateTable { clone: Option<ObjectName>, .. }` | `CreateTable(.., ObjectName? CloneValue, ..)` | CS8859: records não aceitam membro chamado `Clone`. Sufixo `Value`, como na decisão 2 do integrador (A2.md) e no caso 41 | `Ast/Ddl/CreateTable.cs` |
| `ForValues::From { from, to }` | `From(Sequence<PartitionBoundValue> FromValue, Sequence<PartitionBoundValue> To)` | CS0542: o campo `from` teria o nome da variante que o contém (caso 41) | `Ast/Ddl/ForValues.cs` |
| `AlterFunctionOperation::Actions { actions, restrict }` | `Actions(Sequence<AlterFunctionAction> ActionsValue, bool Restrict)` | CS0542 (caso 41) | `Ast/Ddl/AlterFunctionOperation.cs` |
| `AlterFunctionAction::Security { external, security }` | `Security(bool External, FunctionSecurity SecurityValue)` | CS0542 (caso 41) | `Ast/Ddl/AlterFunctionAction.cs` |
| `PartitionBoundValue::Expr(Expr)` | `Expr(Ast.Expr Value)` | Variante com o nome do tipo do campo: tipo qualificado (§6 e caso 38). Não é nome novo, só registro do uso | `Ast/Ddl/PartitionBoundValue.cs` |
| `FunctionReturnType::DataType(DataType)` / `SetOf(DataType)` | `DataType(Ast.DataType Value)` / `SetOf(Ast.DataType Value)` | Idem (caso 38): dentro do record, `DataType` é a variante | `Ast/Ddl/FunctionReturnType.cs` |
| `NamedWindowDefinition(pub Ident, pub NamedWindowExpr)` | `NamedWindowDefinition(Ident Name, NamedWindowExpr WindowExpr)` | Doc: `<name> AS <window specification>`; o parser constrói `NamedWindowDefinition(ident, window_expr)` | `Ast/Query/NamedWindowDefinition.cs` |
| `SelectItem::QualifiedWildcard(SelectItemQualifiedWildcardKind, WildcardAdditionalOptions)` | `QualifiedWildcard(SelectItemQualifiedWildcardKind Kind, WildcardAdditionalOptions AdditionalOptions)` | Nomes do binding no `Display` do Rust (`QualifiedWildcard(kind, additional_options)`) | `Ast/Query/SelectItem.cs` |
| `MatchRecognizePattern::Repetition(Box<MatchRecognizePattern>, RepetitionQuantifier)` | `Repetition(MatchRecognizePattern Pattern, RepetitionQuantifier Quantifier)` | Nomes do parser (`Repetition(Box::new(pattern), quantifier)`) | `Ast/Query/MatchRecognizePattern.cs` |
| `RepetitionQuantifier::Range(u32, u32)` | `Range(uint Min, uint Max)` | Doc `{n,m}`: limite inferior e superior (o parser fala em "upper bound"); coerente com `AtLeast`/`AtMost` | `Ast/Query/RepetitionQuantifier.cs` |
| `GroupByExpr::Expressions(Vec<Expr>, Vec<GroupByWithModifier>)` | `Expressions(Sequence<Expr> Exprs, Sequence<GroupByWithModifier> Modifiers)` | Nomes do parser (`GroupByExpr::Expressions(exprs, modifiers)`) e do `Select` (`Expressions(exprs, _)`) | `Ast/Query/GroupByExpr.cs` |
| Variante `Distinct::Distinct` | `Distinct.DistinctValue` | CS0542: um tipo aninhado não pode ter o nome do tipo que o contém. Sufixo `Value`, por analogia ao caso 41. **Proposta de regra geral**: variante com o nome do próprio enum → sufixo `Value`. O único outro caso no upstream é `Password::Password` (`mod.rs`, outra unidade): convém usar `Password.PasswordValue` | `Ast/Query/Distinct.cs` |
| Campo `LateralView.lateral_view` | `LateralView.LateralViewValue` | CS0542 (caso 41) | `Ast/Query/LateralView.cs` |
| Campo `TableVersion::Changes { changes, .. }` | `Changes(Expr ChangesValue, Expr At, Expr? End)` | CS0542 (caso 41) | `Ast/Query/TableVersion.cs` |

## Camada pública (`SqlParser`)

Nomes "amigáveis" herdados do SqlParser-cs, aplicáveis **somente** à API pública ([ADR-0002](../adr/0002-arquitetura-em-camadas-e-api.md)). Serão definidos quando a camada pública for desenhada.

| Conceito (Core) | API pública | Observação |
|---|---|---|
| _(a definir)_ | | |
