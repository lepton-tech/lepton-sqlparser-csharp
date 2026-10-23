# Glossário de nomes (memória de tradução)

Este glossário lista **só o que a regra mecânica de [`convencoes.md`](convencoes.md) §4 e §6 não resolve sozinha**:
- nomes de campos de variantes-tupla com 2+ campos;
- conflitos do compilador C#;
- exceções de tipo.

**Regra de ouro:** antes de inventar um nome, procure aqui. Depois de inventar, registre aqui **antes** de usar em outro lugar. Isso evita que duas sessões escolham nomes diferentes para a mesma coisa.

Formato: `Rust` → `C#` — motivo — arquivo onde foi definido.

## Core (`LeptonTech.SqlParser.Core`)

### Variantes-tupla com 2+ campos

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `Token::Number(String, bool)` | `Number(string Value, bool Long)` | `Long`: sufixo `L` do literal (Display escreve `L` quando verdadeiro) | `Tokenizer/Token.cs` |

### Tuplas em retornos e parâmetros

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `Dialect::peek_nested_delimited_identifier_quotes -> Option<(char, Option<char>)>` | `(char QuoteStart, char? NestedQuoteStart)?` | Nomes das variáveis que o tokenizer usa ao desestruturar (`let Some((quote_start, nested_quote_start))`) | `Dialects/Dialect.cs` |
| `Iterator::size_hint -> (usize, Option<usize>)` | `(int Lower, int? Upper)` | Nomes da documentação da std ("lower bound", "upper bound") | `Support/IDoubleEndedIterator.cs` |

### Conflitos de compilação (CS0542 etc.)

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| Variante tupla `Value(T)` | parâmetro `Inner` em vez de `Value` | CS0542: o membro teria o nome do tipo que o contém | regra geral, `convencoes.md` §6 |
| `Unescape::unescape(self)` | `Unescape.DoUnescape()` | CS0542: método com o nome do tipo que o contém | `Tokenizer/Unescape.cs` |
| `Tokenizer::tokenizer_error<R>(..)` | `Tokenizer.TokenizerErrorAt<T>(..)` | Dentro de `Tokenizer`, um método `TokenizerError` esconderia o tipo `TokenizerError` em expressões | `Tokenizer/Tokenizer.cs` |
| `Span::EMPTY` (const privado, "for pattern matching") | não portado; as comparações usam `Span.Empty()` | CS0102: o campo `Empty` colidiria com `fn empty()` → `Empty()` | `Tokenizer/Span.cs` |
| `core::iter::Once<T>` (std) | `OnceIter<T>` (`internal`) | `Once` já é o método `RustIter.Once` (`core::iter::once`); um tipo genérico homônimo no mesmo namespace tornaria `Once<T>(x)` ambíguo nos arquivos com `using static LeptonTech.SqlParser.Core.RustIter` | `Support/OnceIter.cs` |
| `alloc::vec::IntoIter<T>` (std) | `VecIntoIter<T>` (`internal`) | `IntoIter` sozinho não diz de qual coleção é e colidiria com os métodos `IntoIter()` (`into_iter`) | `Support/VecIntoIter.cs` |
| `Parser::geometric_type_kind(&self, Keyword) -> Option<GeometricTypeKind>` (upstream #2689, Rodada 2) | `Parser.GeometricTypeKindOf(Keyword)` | Dentro de `Parser`, um método `GeometricTypeKind` esconderia o tipo `GeometricTypeKind` da AST (`new GeometricTypeKind.Circle()` deixaria de compilar) | `Parser/Parser.Part1b.cs` |
| `struct FirstChar(Option<char>)` local de `display_utils::first_char` (upstream #2577, Rodada 2) | classe privada `DisplayUtils.FirstCharWrite` | CS0102: a classe aninhada colidiria com o método `DisplayUtils.FirstChar` (`first_char`) | `DisplayUtils/DisplayUtils.cs` |
| `core::fmt::Error` / `fmt::Result` (std) | `false` / `bool`, devolvidos por `IFmtWrite.WriteStr` e pelos métodos de escrita do `Formatter` | Tipo da std sem equivalente no .NET; o erro é um valor, como no Rust: `convencoes.md` §13, caso 141 | `Support/Formatter.cs` |

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
| `pub struct OneOrManyWithParensIntoIter<T>` + `enum OneOrManyWithParensIntoIterInner<T>` (privado) | `public sealed class OneOrManyWithParensIntoIter<T>` + `internal abstract record OneOrManyWithParensIntoIterInner<T>` (`One(OnceIter<T> Value)`, `Many(VecIntoIter<T> Value)`; `#[derive(Clone)]` → `CloneValue()`, caso 59) | Portados na onda D (D2), no lugar da decisão 2 da A1 (não portar). `impl IntoIterator for OneOrManyWithParens<T>` → `IntoIter()` (`convencoes.md` §13, caso 65) | `Ast/Mod/OneOrManyWithParensIntoIter.cs` |
| `Set::SetNamesDefault {}` (variante-struct sem campos) | `public sealed record SetNamesDefault : Set;` | Equivale a uma variante unitária | `Ast/Mod/Set.cs` |
| `Statement::Comment { comment: Option<String>, .. }` | `Statement.Comment(.., string? CommentValue, ..)` | CS0542: membro com o nome do tipo que o contém (caso 41) | `Ast/Mod/Statement.cs` |
| `Statement::CreateSchema { clone: Option<ObjectName>, .. }` | `CreateSchema(.., ObjectName? CloneValue)` | CS8859: records não aceitam membros chamados `Clone` (o compilador reserva o nome para a cópia do `with`). Sufixo `Value`, por analogia com o caso 41 | `Ast/Mod/Statement.cs` |
| `Statement::CreateDatabase { clone: Option<ObjectName>, .. }` | `CreateDatabase(.., ObjectName? CloneValue, ..)` | CS8859 (idem) | `Ast/Mod/Statement.cs` |
| `Statement::RenameTable(Vec<RenameTable>)` | `RenameTable(Sequence<Ast.RenameTable> Value)` | Variante-tupla de 1 campo; tipo homônimo qualificado (caso 38) | `Ast/Mod/Statement.cs` |
| `Statement::Declare { stmts: Vec<Declare> }` | `Declare(Sequence<Ast.Declare> Stmts)` | Variante-struct cujo campo usa o tipo homônimo da variante: qualificado (caso 38) | `Ast/Mod/Statement.cs` |
| `AlterTableOperation::Algorithm { equals, algorithm }` | `Algorithm(bool EqualsValue, AlterTableAlgorithm AlgorithmValue)` | `Equals`: CS0102, colide com os métodos `Equals` sintetizados pelo record (nova regra, Decisões 3). `Algorithm`: CS0542 (caso 41) | `Ast/Ddl/AlterTableOperation.cs` |
| `AlterTableOperation::Lock { equals, lock }` | `Lock(bool EqualsValue, AlterTableLock LockValue)` | CS0102 / CS0542 (idem) | `Ast/Ddl/AlterTableOperation.cs` |
| Variante unitária `FunctionArgOperator::Equals` (`f(a = 1)`) | `FunctionArgOperator.EqualsVariant` | Um tipo aninhado `Equals` colidiria com o método `Equals` sintetizado pelo record (CS0102). Sufixo `Variant`: o sufixo `Value` do caso 41 é para membros de dados, e esta variante não tem campos | `Ast/Mod/FunctionArgOperator.cs` |
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
| `DataType::Collate(Box<DataType>, ObjectName)` (upstream #2553, Rodada 1) | `Collate(DataType DataType, ObjectName Collation)` | Nomes do `Display` (`DataType::Collate(data_type, collation)`) e do parser (`data_type`) | `Ast/DataType/DataType.cs` |
| `JoinConstraint::UsingAlias(Vec<ObjectName>, Ident)` (upstream #2492, Rodada 2) | `UsingAlias(Sequence<ObjectName> Columns, Ident Alias)` | Nomes do `Display` e de `spans.rs` (`JoinConstraint::UsingAlias(columns, alias)`) | `Ast/Query/JoinConstraint.cs` |
| `NamedWindowDefinition(pub Ident, pub NamedWindowExpr)` | `NamedWindowDefinition(Ident Name, NamedWindowExpr WindowExpr)` | Doc: `<name> AS <window specification>`; o parser constrói `NamedWindowDefinition(ident, window_expr)` | `Ast/Query/NamedWindowDefinition.cs` |
| `SelectItem::QualifiedWildcard(SelectItemQualifiedWildcardKind, WildcardAdditionalOptions)` | `QualifiedWildcard(SelectItemQualifiedWildcardKind Kind, WildcardAdditionalOptions AdditionalOptions)` | Nomes do binding no `Display` do Rust (`QualifiedWildcard(kind, additional_options)`) | `Ast/Query/SelectItem.cs` |
| `MatchRecognizePattern::Repetition(Box<MatchRecognizePattern>, RepetitionQuantifier)` | `Repetition(MatchRecognizePattern Pattern, RepetitionQuantifier Quantifier)` | Nomes do parser (`Repetition(Box::new(pattern), quantifier)`) | `Ast/Query/MatchRecognizePattern.cs` |
| `RepetitionQuantifier::Range(u32, u32)` | `Range(uint Min, uint Max)` | Doc `{n,m}`: limite inferior e superior (o parser fala em "upper bound"); coerente com `AtLeast`/`AtMost` | `Ast/Query/RepetitionQuantifier.cs` |
| `GroupByExpr::Expressions(Vec<Expr>, Vec<GroupByWithModifier>)` | `Expressions(Sequence<Expr> Exprs, Sequence<GroupByWithModifier> Modifiers)` | Nomes do parser (`GroupByExpr::Expressions(exprs, modifiers)`) e do `Select` (`Expressions(exprs, _)`) | `Ast/Query/GroupByExpr.cs` |
| Variante `Distinct::Distinct` | `Distinct.DistinctValue` | CS0542: um tipo aninhado não pode ter o nome do tipo que o contém. Sufixo `Value`, por analogia ao caso 41. **Proposta de regra geral**: variante com o nome do próprio enum → sufixo `Value`. O único outro caso no upstream é `Password::Password` (`mod.rs`, outra unidade): convém usar `Password.PasswordValue` | `Ast/Query/Distinct.cs` |
| Campo `LateralView.lateral_view` | `LateralView.LateralViewValue` | CS0542 (caso 41) | `Ast/Query/LateralView.cs` |
| Campo `TableVersion::Changes { changes, .. }` | `Changes(Expr ChangesValue, Expr At, Expr? End)` | CS0542 (caso 41) | `Ast/Query/TableVersion.cs` |
| Campo `CreateRole.create_role: Option<bool>` | `CreateRole(.., bool? CreateRoleValue, ..)` | CS0542 (caso 41). Unidade A8, sem nota; registrado na consolidação da onda B | `Ast/Dcl/CreateRole.cs` |
| Campo `Action::Role { role }` | `Action.Role(ObjectName RoleValue)` | CS0542 (caso 41). Unidade A3/A4, sem nota; registrado na consolidação da onda B | `Ast/Mod/Action.cs` |

### Parser e spans (onda B): nomes não mecânicos

Consolidado das notas em `onda-b/` (seção "Nomes" de cada unidade, com o motivo completo).

**Tuplas em retornos e parâmetros** (nomes das variáveis que o chamador usa ao desestruturar, salvo indicação):

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `parse_struct_type_def -> Result<(Vec<StructField>, MatchedTrailingBracket)>` e o parâmetro `F: FnMut(&mut Parser) -> Result<(StructField, MatchedTrailingBracket)>` | `(Sequence<StructField> Fields, MatchedTrailingBracket TrailingBracket)`; `Func<Parser, (StructField Field, MatchedTrailingBracket TrailingBracket)>` | `let (fields, trailing_bracket) = ..` | `Parser/Parser.Part1d.cs` (B1) |
| `parse_struct_field_def -> Result<(StructField, MatchedTrailingBracket)>` | `(StructField Field, MatchedTrailingBracket TrailingBracket)` | `let (def, trailing_bracket) = elem_parser(self)?` | `Parser/Parser.Part1d.cs` (B1) |
| `parse_parenthesized_map_type_def -> Result<(DataType, DataType, bool)>` (antes `parse_click_house_map_def -> Result<(DataType, DataType)>`, renomeada no upstream #2489, Rodada 1) | `(DataType KeyDataType, DataType ValueDataType, bool ValueNotNull)` | `let (key_data_type, value_data_type, value_not_null) = ..` | `Parser/Parser.Part1d.cs` (B1; Rodada 1, lote 5) |
| `parse_as_query -> Result<(bool, Box<Query>)>` | `(bool HasAs, Query Query)` | Campos de `Statement::Cache` que recebem os valores | `Parser/Parser.Part2a.cs` (B2) |
| `parse_create_function_name_and_params -> Result<(ObjectName, Vec<OperateFunctionArg>)>` | `(ObjectName Name, Sequence<OperateFunctionArg> Args)` | `let (name, args) = ..` | `Parser/Parser.Part2a.cs` (B2) |
| `parse_columns -> Result<(Vec<ColumnDef>, Vec<TableConstraint>)>` | `(Sequence<ColumnDef> Columns, Sequence<TableConstraint> Constraints)` | `let (columns, constraints) = ..` | `Parser/Parser.Part3a.cs` (B2, B3) |
| `parse_alter_aggregate_signature -> Result<(FunctionDesc, bool, Option<Vec<OperateFunctionArg>>)>` | `(FunctionDesc Function, bool AggregateStar, Sequence<OperateFunctionArg>? AggregateOrderBy)` | `let (function, aggregate_star, aggregate_order_by) = ..` | `Parser/Parser.Part3b.cs` (B3) |
| `parse_alter_function_actions -> Result<(Vec<AlterFunctionAction>, bool)>` | `(Sequence<AlterFunctionAction> Actions, bool Restrict)` | `let (actions, restrict) = ..` | `Parser/Parser.Part3b.cs` (B3) |
| `parse_data_type_helper -> Result<(DataType, MatchedTrailingBracket)>` | `(DataType DataType, MatchedTrailingBracket TrailingBracket)` | Tipo do primeiro elemento (o chamador usa `ty`); mesmo nome da B1 | `Parser/Parser.Part3c.cs` (B3) |
| `parse_datetime_64 -> Result<(u64, Option<String>)>` | `(ulong Precision, string? TimeZone)` | `let (precision, time_zone) = ..` | `Parser/Parser.Part4a.cs` (B3, B4) |
| `parse_unquoted_hyphenated_identifier -> Result<(Ident, bool)>` | `(Ident Ident, bool EndWithPeriod)` | `let (ident, end_with_period) = ..` | `Parser/Parser.Part4a.cs` (B4) |
| `parse_optional_precision_scale -> Result<(Option<u64>, Option<u64>)>` | `(ulong? Precision, ulong? Scale)` | Doc Rust: "return `(Option<precision>, Option<scale>)`" | `Parser/Parser.Part4a.cs` (B4) |
| `parse_select_modifiers -> Result<(Option<SelectModifiers>, Option<Distinct>)>` | `(SelectModifiers? SelectModifiers, Distinct? Distinct)` | `return Ok((select_modifiers, distinct))` | `Parser/Parser.Part4b.cs` (B4) |
| `extract_hint_prefix_and_text -> Option<(String, String)>` | `(string Prefix, string Text)?` | Campos de `OptimizerHint` que recebem os valores | `Parser/Parser.Part4b.cs` (B4) |
| Tupla da closure de `parse_cte` (`Ok((query, closing_paren_token))`) | `(Query Query, TokenWithSpan ClosingParenToken)` | Nomes Rust | `Parser/Parser.Part4b.cs` (B4) |
| `parse_grant_deny_revoke_privileges_objects -> Result<(Privileges, Option<GrantObjects>)>` | `(Privileges Privileges, GrantObjects? Objects)` | `let (privileges, objects) = ..`; campos de `Grant`/`Revoke`/`DenyStatement` | `Parser/Parser.Part5a.cs` (B5) |
| `parse_order_by_expr_inner -> Result<(OrderByExpr, Option<ObjectName>)>` | `(OrderByExpr OrderByExpr, ObjectName? OperatorClass)` | Tipo do primeiro elemento; campo `operator_class` de `IndexColumn` | `Parser/Parser.Part5c.cs` (B5) |
| `snowflake.rs`: `parse_multi_table_insert_when_clauses -> Result<(Vec<MultiTableInsertWhenClause>, Option<Vec<MultiTableInsertIntoClause>>)>` | `(Sequence<MultiTableInsertWhenClause> WhenClauses, Sequence<MultiTableInsertIntoClause>? ElseClause)` | `let (when_clauses, else_clause) = ..` | `Dialects/SnowflakeDialect.cs` (B6) |

**Tipos, funções aninhadas e auxiliares do port:**

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `struct Body` local de `parse_postgres_create_function` | `private sealed class Parser.Body` | `convencoes.md` §13, caso 105 | `Parser/Parser.Part2a.cs` (B2) |
| `fn validator` aninhada (`parse_expr_with_alias_and_order_by`, `maybe_parse_select_item_alias`, `maybe_parse_table_alias`, `parse_optional_alias`, `parse_pivot_aggregate_function`) | função local `static Validator` | Caso 93 | `Parser/Parser.Part1a.cs`, `Part4a.cs`, `Part5a.cs` (B1, B4, B5) |
| `fn ensure_not_set<T>`, `fn parse_data_type_no_default`, `fn parse_data_type_for_aggregate_arg` (aninhadas) | funções locais `EnsureNotSet<T>`, `ParseDataTypeNoDefault`, `ParseDataTypeForAggregateArg` | Caso 93 | `Parser/Parser.Part2*.cs` (B2) |
| Closures `ok_value` (`parse_value`) e `parse_string_expr` (`parse_create_function_body_string`) | funções locais `OkValue`, `ParseStringExpr` | Caso 93 | `Parser/Parser.Part3c.cs` (B3) |
| `fn parse_columns` aninhada em `parse_grant_permission` | função local `static Sequence<Ident>? ParseColumns(Parser parser)` | Caso 93; esconde o membro `ParseColumns()`, que o método não usa | `Parser/Parser.Part5a.cs` (B5) |
| Corpo do braço `alias.replace(outer_alias)` em `parse_table_factor` | função local `static ReplaceAlias` | Caso 104 | `Parser/Parser.Part4d.cs` (B4) |
| Auxiliares do port para `Result` adiado | `ParseOptionalPrecisionResult` + `Unwrap` (`parse_data_type_helper`); `objectsResult`, `objectsError`, `UnwrapObjects()` (`parse_grant_deny_revoke_privileges_objects`) | Caso 83 | `Parser/Parser.Part3c.cs` (B3), `Parser/Parser.Part5a.cs` (B5) |
| Auxiliares do port em dialetos | `maybeBlock` (MsSql); `parsed`, `result`, `Map` (`parse_column_option` do Snowflake); `parseError`, `magnitude`, `negative` (`parse_select_item_for_data_load`) | Casos 84, 85 e 89 | `Dialects/MsSqlDialect.cs`, `Dialects/SnowflakeDialect.cs` (B6) |
| `format!("{:?}", token)` / `{s:?}` de `str` | `token.ToDebugString()` / `TokenDebugExtensions.DebugStr(s)` (`internal`) | Mesmo nome dos `Debug` de `Location`/`Span` (caso 22); caso 90 | `Parser/TokenDebugExtensions.cs` (B1, B3) |
| `str::eq_ignore_ascii_case` | `RustChar.EqIgnoreAsciiCase(string, string)` | Caso 19 | `Support/RustChar.cs` (B1) |
| `str::to_uppercase`/`str::to_lowercase`; `char::to_uppercase`/`char::to_lowercase`; `core::unicode::conversions::to_upper`/`to_lower`, `char::is_cased`, `char::is_case_ignorable` | `RustString.ToUppercase`/`ToLowercase(string)`; `RustChar.ToUppercase`/`ToLowercase(int)` (→ `string`); `RustCaseMapping.ToUpper`/`ToLower(int)` (→ `(int, int, int)`), `IsCased`, `IsCaseIgnorable` (todos `internal`) | Nome do tipo Rust (`str` → `RustString`, como `RustChar`) + nome mecânico do método; `RustCaseMapping` reúne o módulo `conversions` e as tabelas geradas (caso 139) | `Support/RustString.cs`, `Support/RustCaseMapping.cs`, `Support/RustCaseMapping.g.cs` (D6) |
| `mssql.rs`: `&[GranteesType::Public]` | `private static readonly GranteesType[] ReservedGranteesTypes` | Caso 113 | `Dialects/MsSqlDialect.cs` (B6) |
| Funções livres de `mysql.rs`/`snowflake.rs` | `MySqlDialect.ParseLockTables` … `SnowflakeDialect.ParseMultiTableInsertWhenClauses` (nome mecânico, `static`) | Caso 112 | `Dialects/MySqlDialect.cs`, `Dialects/SnowflakeDialect.cs` (B6) |
| `pub trait Spanned` / `fn span(&self)` | `ISpanned` / `Span Span()` | Prefixo `I`, como `IDisplay` (caso 117) | `Ast/Spans/ISpanned.cs` (B7) |
| `fn union_spans` (módulo `spans`) | `SpansModule.UnionSpans` (`internal`) | Casos 20 e 29 | `Ast/Spans/SpansModule.cs` (B7) |
| `core::iter::once`, `Option` como iterador | `RustIter.Once`, `RustIter.OptionIter` (`internal`) | Nomes do Rust, como `RustChar` (caso 119) | `Support/RustIter.cs` (B7) |

**Variáveis locais:** as renomeações por sombreamento (caso 72 e caso 94) e os nomes com `@` (casos 56 e 95) de cada unidade estão listados nas notas `onda-b/B1.md`–`B7.md` (seção "Nomes"). Não são API; não precisam ser repetidos em outros arquivos.

## Testes (`LeptonTech.SqlParser.Core.Tests`)

Auxiliares **inventados** nos testes portados (não existem no Rust). Antes de criar outro com o mesmo papel, reutilize o nome daqui. Os auxiliares que vêm do Rust seguem o nome mecânico (`pg()` → `Pg()`, `mysql_and_generic()` → `MysqlAndGeneric()`) e não estão listados.

| Papel | C# | Origem |
|---|---|---|
| Mensagem do `panic` de `unreachable!()` (`internal error: entered unreachable code`) | constante `private const string Unreachable` (C2, C10, C15); método `Unreachable()`/`Unreachable(msg)` que devolve a `InvalidOperationException` (C5) | `SqlParserCommonTests2.cs`, `SqlParserMySqlTests2.cs`, `SqlParserClickHouseTests.cs`, `SqlParserCommonTests5.cs` |
| `Option::unwrap()` com a mensagem do Rust | `Unwrap<T>(T?)` (C5, C7); constante `UnwrapNone` usada com `?? throw` (C11) | `SqlParserCommonTests5.cs`, `SqlParserPostgresTests2.cs`, `SqlParserSnowflakeTests1.cs` |
| `Vec::pop().unwrap()` sobre uma cópia da `Sequence` | `Pop(lista)` (C5) | `SqlParserCommonTests5.cs` |
| `thread::spawn` + `mpsc::channel` + `recv_timeout(..).expect(msg)` | `SpawnAndRecvTimeout(body, timeout, msg)` (thread com 2 MiB de pilha); com `thread::Builder::new().stack_size(n)`, o parâmetro opcional `stackSize: n` (Rodada 1, #2619). O valor que a thread envia (`tx.send(result.is_ok())`) vira um local capturado pelo corpo | `SqlParserCommonTests5.cs` (C5) |
| `ObjectName::from(vec![Ident::new(a), ..])` abreviado | `Name(params string[])` | `SqlParserPostgresTests3.cs` (C8) |
| `parsed.err()` | função local `ParseErr` que devolve `ParserError?` | `SqlParserSnowflakeTests2.cs` (C12) |
| `run_parser_method` com closure que devolve `()` | sobrecarga privada `RunParserMethod(TestedDialects, string, Action<Parser>)` | `Parser/ParserTests.cs` (C17; ver caso 85) |
| `macro_rules!` locais | `TestParseDataType`, `TestParseSchemaName`, `TestParseTableConstraint`, `TestParseMultipartIdentifierError`, `TpchTest(n)` | `Parser/ParserTests.cs`, `SqlParserRegressionTests.cs` (C17) |
| `s.repeat(n)` | `Repeat(string, int)` | `NoStdRecursionTests.cs` (C17) |
| `struct MyDialect` declarado dentro de vários testes | classes de escopo `CustomPrefixParser`, `CustomInfixParser`, `CustomStatementParser`, `TestMapSyntaxNotSupportDefault`, cada uma com `MyDialect` aninhado; na Rodada 1 de sincronização, `CustomDialectSupportsTablePartitions` (com `MyDialect` e `WithoutPartitions`), `CustomDialectLambdaKeywordSyntaxWithoutArrow`, `CustomDialectLambdaKeywordDefaultsToArrowSupport` e `CustomDialectLambdaArrowSyntaxWithoutKeyword` | `SqlParserCustomDialectTests.cs` (C17; `sync/rodada1-lotes1-3.md`) |
| `struct SpanTest<'a>(Parser<'a>, &'a str)` | `SpanTest.Parser`, `SpanTest.Sql` | `Ast/SpansTests.cs` (B7) |
| `struct` que implementa `fmt::Write` ou `Display` dentro de um teste (Rodada 2: `RejectingWriter(char)`, `Chunks(Cell<usize>)`, `IgnoresErrors`) | classes de escopo `DisplayCreateMacroPropagatesArgumentListErrors.RejectingWriter` (campo `.0` → `_value`), `TestFirstCharStopsAtFirstChar.Chunks` (`Cell<usize>` → campo `int Value`) e `TestFirstCharKeepsFirstCharWhenErrorsAreIgnored.IgnoresErrors`, como os dialetos de teste | `SqlParserDuckDbTests.cs`, `DisplayUtils/DisplayUtilsTests.cs` |
| `struct WrappedDialect(MySqlDialect)` | campo privado `_inner` | `Dialects/DialectTests.cs` (B7) |

### Visitor (`ast/visitor.rs` e derive `Visit`/`VisitMut`; onda C, C18)

Regras em `convencoes.md` §13, casos 125–134, e `docs/adr/0007-visitor.md`.

| Rust | C# | Motivo | Definido em |
|---|---|---|---|
| `pub trait Visit` / `fn visit(&self, visitor)` | `IVisit` / `ControlFlow<TBreak> Visit<TBreak>(Visitor<TBreak> visitor)` | Prefixo `I`, como `ISpanned` e `IDisplay` | `Ast/Visitor/IVisit.cs` |
| `pub trait VisitMut` / `fn visit(&mut self, visitor)` | `IVisitMut<TSelf>` / `Visit<TBreak>(VisitorMut<TBreak> visitor, out TSelf self)` | O nome `visit` é o mesmo nos dois traits; o C# distingue pela sobrecarga. `self` devolve o nó (caso 129) | `Ast/Visitor/IVisitMut.cs` |
| Corpo do `visit` de `VisitMut` | `internal static VisitMut<TBreak>(ref T node, VisitorMut<TBreak> visitor)` em cada tipo | Chamado pelos pais no código gerado, sobre a variável do campo | `Ast/Visitor/Generated/` |
| Corpo de cada braço do `match self` do derive | `private static VisitVariant(V v, ..)` / `VisitMutVariant(ref E node, V v, ..)` | Um método por variante (quadro de pilha pequeno; ADR-0007, decisão 6) | `Ast/Visitor/Generated/` |
| `pub trait Visitor { type Break; .. }` / `VisitorMut` | `Visitor<TBreak>` / `VisitorMut<TBreak>` (classes abstratas) | O tipo associado vira o parâmetro genérico `TBreak` (caso 127) | `Ast/Visitor/Visitor.cs`, `VisitorMut.cs` |
| Ganchos `pre_visit_x(&mut self, _x: &X)` | `PreVisitX(X x)` / `PreVisitX(ref X x)` (`VisitorMut`) | Regra mecânica; o `_` inicial do parâmetro sai | idem |
| Funções livres `visit_relations`, `visit_relations_mut`, `visit_expressions`, `visit_expressions_mut`, `visit_statements`, `visit_statements_mut` | `Visitor.VisitRelations<V, E>` … `Visitor.VisitStatementsMut<V, E>` (classe estática `Visitor`) | Caso 132 | `Ast/Visitor/Visitor.Functions.cs` |
| `struct RelationVisitor<F>(F)` (com `impl Visitor` e `impl VisitorMut`); idem `ExprVisitor<F>`, `StatementVisitor<F>` | `RelationVisitor<E>` e `RelationVisitorMut<E>` (privadas); idem `ExprVisitor<E>`/`ExprVisitorMut<E>`, `StatementVisitor<E>`/`StatementVisitorMut<E>`; o campo `.0` → `_f` | Caso 129 | `Ast/Visitor/Visitor.Functions.cs` |
| `core::ops::ControlFlow<B>` | `ControlFlow<TBreak>` (`Continue`, `Break(b)`, `IsBreak`, `IsContinue`, `BreakValue()`) | Caso 123 | `Support/ControlFlow.cs` |
| `()` (tipo unitário) como argumento de tipo | `ValueTuple` | Caso 85 (e 123) | — |
| `FnMut(&mut T) -> R` | `RefFunc<T, R>` | Caso 124 | `Support/RefFunc.cs` |
| Crate `recursive` (`recursive::recursive`, `stacker::maybe_grow`/`stacker::grow`) | `StackGuard.IsStackLow()` / `StackGuard.Grow<TResult>(Func<TResult>)` (`internal`; mesma API da unidade D1); no visitor, `VisitSupport.StackIsLow`, `VisitOnNewStack`, `VisitMutOnNewStack` | Caso 130 | `Support/StackGuard.cs`, `Ast/Visitor/VisitSupport.cs` |
| Variante `FunctionArgOperator::Equals` | `FunctionArgOperator.EqualsVariant` | CS0102/CS0542: um tipo aninhado `Equals` colidiria com o método `Equals` do record. Registro tardio (a onda A citava `nomes.md` sem registrar); o gerador do visitor aceita os sufixos `Value` e `Variant` ao casar variantes com o Rust | `Ast/Mod/FunctionArgOperator.cs` |
| Testes (`mod tests`/`mod visit_mut_tests`): campos `visited`, `idents`, `relations` (privados) | propriedades `Visited`, `Idents`, `Relations` | Lidos pelo teste fora da classe aninhada | `tests/.../Ast/VisitorTests.cs` |
| Testes: `do_visit`, `do_visit_mut` | `DoVisit`, `DoVisitMut` (`private static`) | Regra mecânica (skill `port-rust-tests`) | idem |

## Camada pública (`LeptonTech.SqlParser`)

Nomes "amigáveis" herdados do SqlParser-cs, aplicáveis **somente** à API pública ([ADR-0002](../adr/0002-arquitetura-em-camadas-e-api.md)). Decisões da fase 1 em [ADR-0008](../adr/0008-api-publica-fase-1.md) e da fase 2 em [ADR-0009](../adr/0009-api-publica-fase-2.md).

| Conceito (Core) | API pública | Observação |
|---|---|---|
| `Parser::parse_sql`, `Parser::new(d).with_options(o).try_with_sql(sql).parse_statements()` | `SqlQueryParser.Parse(sql, dialect, options)` | Nome do SqlParser-cs. Dialeto padrão `GenericDialect` |
| `dialect_from_str(name)` + `parse_sql` | `SqlQueryParser.Parse(sql, dialectName, options)` | `Parse(sql, null, options)` é ambíguo: use `Parse(sql, options: options)` |
| `Result<Vec<Statement>, ParserError>` sem exceção | `SqlQueryParser.TryParse(..., out statements, out error)` | Padrão Try do .NET |
| `Parser::parse_sql_with_comments` | `SqlQueryParser.ParseWithComments(sql, dialect, options)` → `SqlParseResult` | Tupla do Core vira tipo nomeado com `Deconstruct` |
| `Parser::with_recursion_limit` | `new SqlQueryParser(recursionLimit)` / `SqlQueryParser.RecursionLimit` | `null` = padrão do Core |
| `ParserException` (Core) / `ParserError` | `SqlParseException` (`Error`, `Location`, `Line`, `Column`) | Não pode se chamar `ParserException` (CS0104 com o Core) |
| `format!("{}", x)` / `format!("{:#}", x)` | `x.ToSql()` / `x.ToSql(pretty: true)` (`SqlWritingExtensions`) | Sobre `IDisplay`; também sobre `IEnumerable<Statement>` (separadores `"; "` e `";\n"`) |
| `trait Visitor` (Core: `Visitor<TBreak>`) | `SqlVisitor` (abstrata) | Adaptador sobre o `Visitor<ValueTuple>` do Core. `Visitor` colidiria com `Visitor`/`Visitor<TBreak>` do Core (CS0104) |
| `core::ops::ControlFlow<B>` (Core: `ControlFlow<TBreak>`) | `ControlFlow` (enum: `Continue`, `Break`) | Como no SqlParser-cs, sem valor no `Break`. Aridade diferente da do Core: sem conflito |
| `pre_visit_expr`/`post_visit_expr` (Core: `PreVisitExpr`/`PostVisitExpr`) | `PreVisitExpression`/`PostVisitExpression` | Nome do SqlParser-cs; parâmetro `Expr` |
| `pre_visit_ident`/`post_visit_ident` (Core: `PreVisitIdent`/`PostVisitIdent`) | `PreVisitIdentifier`/`PostVisitIdentifier` | Por extenso (diretrizes do .NET) |
| `pre_visit_order_by_expr`/`post_visit_order_by_expr` (Core: `PreVisitOrderByExpr`/`PostVisitOrderByExpr`) | `PreVisitOrderByExpression`/`PostVisitOrderByExpression` | Por extenso |
| Demais ganchos (`pre_visit_query`, `_select`, `_relation`, `_table_factor`, `_statement`, `_value`, `_order_by`, `_group_by`) | Mesmo nome do Core (`PreVisitQuery`...) | Em `SqlVisitor` e em `SqlRewriter` |
| `trait VisitorMut` (Core: `VisitorMut<TBreak>`, ganchos `ref`) | `SqlRewriter` (abstrata): ganchos devolvem o nó; `protected void Break()` | Sem `ref`, como o `ExpressionVisitor` do .NET |
| `Visit::visit` (Core: `IVisit.Visit(Visitor<TBreak>)`) | `node.Visit(SqlVisitor)` → `ControlFlow` (`SqlVisitorExtensions`) | Extensão genérica `where T : IVisit` (sem boxing de `Sequence<T>`) |
| `VisitMut::visit` (Core: `IVisitMut<T>.Visit(VisitorMut<TBreak>, out T)`) | `node.Rewrite(SqlRewriter)` → `T` | `where T : IVisitMut<T>`: o nó tipado pelo tipo-base |
| `visit_relations`, `visit_expressions`, `visit_statements` | `node.VisitRelations`, `VisitExpressions`, `VisitStatements` (`Action<T>` ou `Func<T, ControlFlow>`) | Gancho `pre`, como no Rust |
| `visit_relations_mut`, `visit_expressions_mut`, `visit_statements_mut` | `node.RewriteRelations`, `RewriteExpressions`, `RewriteStatements` (`Func<T, T>`) → árvore nova | Gancho `post`, como no Rust |
| (sem equivalente no Rust nem no SqlParser-cs) | `GetReferencedTables`, `GetReferencedColumns`, `GetReferencedFunctions`, `GetStatementKind` (`SqlAnalysisExtensions`); enum `SqlStatementKind` | Semântica no ADR-0009, decisões 5 e 6 |
