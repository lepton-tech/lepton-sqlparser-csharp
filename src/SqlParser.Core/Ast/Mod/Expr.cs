// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

// Notas do port (ver docs/port/onda-a/A1.md):
// - Variantes com o nome de um tipo de topo (`Value`, `TypedString`, `Function`, `Map`, `Array`,
//   `Interval`, `MemberOf`) qualificam o tipo do campo como `Ast.X` (convencoes.md §13, caso 38).
// - A variante aninhada `Value` (e `Subquery`) é herdada por todas as outras variantes. Um parâmetro
//   posicional com o mesmo nome (`Value`, `Subquery`) não pode gerar a propriedade sozinho (CS8866), então
//   a propriedade é declarada explicitamente com `new`, mantendo o nome mecânico.

/// <summary>An SQL expression of any type.</summary>
/// <remarks>
/// <para><b>Semantics / Type Checking</b></para>
/// <para>
/// The parser does not distinguish between expressions of different types
/// (e.g. boolean vs string). The caller is responsible for detecting and
/// validating types as necessary (for example  <c>WHERE 1</c> vs <c>SELECT 1=1</c>)
/// See the <see href="https://github.com/apache/datafusion-sqlparser-rs/blob/main/README.md#syntax-vs-semantics">README.md</see> for more details.
/// </para>
/// <para><b>Equality and Hashing Does not Include Source Locations</b></para>
/// <para>
/// The <c>Expr</c> type implements <c>PartialEq</c> and <c>Eq</c> based on the semantic value
/// of the expression (not bitwise comparison). This means that <c>Expr</c> instances
/// that are semantically equivalent but have different spans (locations in the
/// source tree) will compare as equal.
/// </para>
/// </remarks>
[VisitWith("visit_expr")]
public abstract partial record Expr : IDisplay
{
    /// <summary>Identifier e.g. table name or column name</summary>
    public sealed record Identifier(Ident Value) : Expr
    {
        // Rust: campo `.0`.
        public new Ident Value { get; init; } = Value;
    }

    /// <summary>Multi-part identifier, e.g. <c>table_alias.column</c> or <c>schema.table.col</c></summary>
    public sealed record CompoundIdentifier(Sequence<Ident> Value) : Expr
    {
        // Rust: campo `.0`.
        public new Sequence<Ident> Value { get; init; } = Value;
    }

    /// <summary>
    /// Multi-part expression access.
    /// <para>
    /// This structure represents an access chain in structured / nested types
    /// such as maps, arrays, and lists:
    /// </para>
    /// <list type="bullet">
    /// <item>Array
    ///   <list type="bullet">
    ///   <item>A 1-dim array <c>a[1]</c> will be represented like:
    ///     <c>CompoundFieldAccess(Ident('a'), vec![Subscript(1)]</c></item>
    ///   <item>A 2-dim array <c>a[1][2]</c> will be represented like:
    ///     <c>CompoundFieldAccess(Ident('a'), vec![Subscript(1), Subscript(2)]</c></item>
    ///   </list>
    /// </item>
    /// <item>Map or Struct (Bracket-style)
    ///   <list type="bullet">
    ///   <item>A map <c>a['field1']</c> will be represented like:
    ///     <c>CompoundFieldAccess(Ident('a'), vec![Subscript('field')]</c></item>
    ///   <item>A 2-dim map <c>a['field1']['field2']</c> will be represented like:
    ///     <c>CompoundFieldAccess(Ident('a'), vec![Subscript('field1'), Subscript('field2')]</c></item>
    ///   </list>
    /// </item>
    /// <item>Struct (Dot-style) (only effect when the chain contains both subscript and expr)
    ///   <list type="bullet">
    ///   <item>A struct access <c>a[field1].field2</c> will be represented like:
    ///     <c>CompoundFieldAccess(Ident('a'), vec![Subscript('field1'), Ident('field2')]</c></item>
    ///   </list>
    /// </item>
    /// <item>If a struct access likes <c>a.field1.field2</c>, it will be represented by CompoundIdentifier([a, field1, field2])</item>
    /// </list>
    /// </summary>
    /// <param name="Root">The base expression being accessed.</param>
    /// <param name="AccessChain">Sequence of access operations (subscript or identifier accesses).</param>
    public sealed record CompoundFieldAccess(Expr Root, Sequence<AccessExpr> AccessChain) : Expr;

    /// <summary>
    /// Access data nested in a value containing semi-structured data, such as
    /// the <c>VARIANT</c> type on Snowflake. for example <c>src:customer[0].name</c>.
    /// <para>See <see href="https://docs.snowflake.com/en/user-guide/querying-semistructured"/>.</para>
    /// <para>See <see href="https://docs.databricks.com/en/sql/language-manual/functions/colonsign.html"/>.</para>
    /// </summary>
    /// <param name="Value">The value being queried.</param>
    /// <param name="Path">The path to the data to extract.</param>
    public sealed record JsonAccess(Expr Value, JsonPath Path) : Expr
    {
        /// <summary>The value being queried.</summary>
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS FALSE</c> operator</summary>
    public sealed record IsFalse(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS NOT FALSE</c> operator</summary>
    public sealed record IsNotFalse(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS TRUE</c> operator</summary>
    public sealed record IsTrue(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS NOT TRUE</c> operator</summary>
    public sealed record IsNotTrue(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS NULL</c> operator</summary>
    public sealed record IsNull(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS NOT NULL</c> operator</summary>
    public sealed record IsNotNull(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS UNKNOWN</c> operator</summary>
    public sealed record IsUnknown(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS NOT UNKNOWN</c> operator</summary>
    public sealed record IsNotUnknown(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary><c>IS DISTINCT FROM</c> operator</summary>
    // Left: Rust: campo `.0` (operando à esquerda).
    // Right: Rust: campo `.1` (operando à direita).
    public sealed record IsDistinctFrom(Expr Left, Expr Right) : Expr;

    /// <summary><c>IS NOT DISTINCT FROM</c> operator</summary>
    // Left: Rust: campo `.0` (operando à esquerda).
    // Right: Rust: campo `.1` (operando à direita).
    public sealed record IsNotDistinctFrom(Expr Left, Expr Right) : Expr;

    /// <summary><c>&lt;expr&gt; IS [NOT] JSON [VALUE|SCALAR|ARRAY|OBJECT] [WITH|WITHOUT UNIQUE [KEYS]]</c></summary>
    /// <param name="Expr">Expression being tested.</param>
    /// <param name="Kind">Optional JSON shape constraint.</param>
    /// <param name="UniqueKeys">Optional duplicate-key handling constraint for JSON objects.</param>
    /// <param name="Negated"><c>true</c> when <c>NOT</c> is present.</param>
    public sealed record IsJson(Expr Expr, JsonPredicateType? Kind, JsonKeyUniqueness? UniqueKeys, bool Negated) : Expr;

    /// <summary><c>&lt;expr&gt; IS [ NOT ] [ form ] NORMALIZED</c></summary>
    /// <param name="Expr">Expression being tested.</param>
    /// <param name="Form">Optional normalization <c>form</c> (e.g., NFC, NFD).</param>
    /// <param name="Negated"><c>true</c> when <c>NOT</c> is present.</param>
    public sealed record IsNormalized(Expr Expr, NormalizationForm? Form, bool Negated) : Expr;

    /// <summary><c>[ GLOBAL ] [ NOT ] IN (val1, val2, ...)</c></summary>
    /// <param name="Expr">Left-hand expression to test for membership.</param>
    /// <param name="List">Literal list of expressions to check against.</param>
    /// <param name="Negated"><c>true</c> when the <c>NOT</c> modifier is present.</param>
    /// <param name="Global"><c>true</c> when the ClickHouse <c>GLOBAL</c> modifier is present.</param>
    public sealed record InList(Expr Expr, Sequence<Expr> List, bool Negated, bool Global) : Expr;

    /// <summary><c>[ GLOBAL ] [ NOT ] IN (SELECT ...)</c></summary>
    /// <param name="Expr">Left-hand expression to test for membership.</param>
    /// <param name="Subquery">The subquery providing the candidate values.</param>
    /// <param name="Negated"><c>true</c> when the <c>NOT</c> modifier is present.</param>
    /// <param name="Global"><c>true</c> when the ClickHouse <c>GLOBAL</c> modifier is present.</param>
    public sealed record InSubquery(Expr Expr, Query Subquery, bool Negated, bool Global) : Expr
    {
        /// <summary>The subquery providing the candidate values.</summary>
        public new Query Subquery { get; init; } = Subquery;
    }

    /// <summary><c>[ NOT ] IN UNNEST(array_expression)</c></summary>
    /// <param name="Expr">Left-hand expression to test for membership.</param>
    /// <param name="ArrayExpr">Array expression being unnested.</param>
    /// <param name="Negated"><c>true</c> when the <c>NOT</c> modifier is present.</param>
    public sealed record InUnnest(Expr Expr, Expr ArrayExpr, bool Negated) : Expr;

    /// <summary><c>&lt;expr&gt; [ NOT ] BETWEEN &lt;low&gt; AND &lt;high&gt;</c></summary>
    /// <param name="Expr">Expression being compared.</param>
    /// <param name="Negated"><c>true</c> when the <c>NOT</c> modifier is present.</param>
    /// <param name="Low">Lower bound.</param>
    /// <param name="High">Upper bound.</param>
    public sealed record Between(Expr Expr, bool Negated, Expr Low, Expr High) : Expr;

    /// <summary>Binary operation e.g. <c>1 + 1</c> or <c>foo &gt; bar</c></summary>
    /// <param name="Left">Left operand.</param>
    /// <param name="Op">Operator between operands.</param>
    /// <param name="Right">Right operand.</param>
    public sealed record BinaryOp(Expr Left, BinaryOperator Op, Expr Right) : Expr;

    /// <summary><c>[NOT] LIKE &lt;pattern&gt; [ESCAPE &lt;escape_character&gt;]</c></summary>
    /// <param name="Negated"><c>true</c> when <c>NOT</c> is present.</param>
    /// <param name="Any">
    /// Snowflake supports the ANY keyword to match against a list of patterns
    /// <see href="https://docs.snowflake.com/en/sql-reference/functions/like_any"/>
    /// </param>
    /// <param name="Expr">Expression to match.</param>
    /// <param name="Pattern">Pattern expression.</param>
    /// <param name="EscapeChar">Optional escape character.</param>
    public sealed record Like(bool Negated, bool Any, Expr Expr, Expr Pattern, Expr? EscapeChar) : Expr;

    /// <summary><c>ILIKE</c> (case-insensitive <c>LIKE</c>)</summary>
    /// <param name="Negated"><c>true</c> when <c>NOT</c> is present.</param>
    /// <param name="Any">
    /// Snowflake supports the ANY keyword to match against a list of patterns
    /// <see href="https://docs.snowflake.com/en/sql-reference/functions/like_any"/>
    /// </param>
    /// <param name="Expr">Expression to match.</param>
    /// <param name="Pattern">Pattern expression.</param>
    /// <param name="EscapeChar">Optional escape character.</param>
    public sealed record ILike(bool Negated, bool Any, Expr Expr, Expr Pattern, Expr? EscapeChar) : Expr;

    /// <summary><c>SIMILAR TO</c> regex</summary>
    /// <param name="Negated"><c>true</c> when <c>NOT</c> is present.</param>
    /// <param name="Expr">Expression to test.</param>
    /// <param name="Pattern">Pattern expression.</param>
    /// <param name="EscapeChar">Optional escape character.</param>
    public sealed record SimilarTo(bool Negated, Expr Expr, Expr Pattern, Expr? EscapeChar) : Expr;

    /// <summary>MySQL: <c>RLIKE</c> regex or <c>REGEXP</c> regex</summary>
    /// <param name="Negated"><c>true</c> when <c>NOT</c> is present.</param>
    /// <param name="Expr">Expression to test.</param>
    /// <param name="Pattern">Pattern expression.</param>
    /// <param name="Regexp">true for REGEXP, false for RLIKE (no difference in semantics)</param>
    public sealed record RLike(bool Negated, Expr Expr, Expr Pattern, bool Regexp) : Expr;

    /// <summary>
    /// <c>ANY</c> operation e.g. <c>foo &gt; ANY(bar)</c>, comparison operator is one of <c>[=, &gt;, &lt;, =&gt;, =&lt;, !=]</c>
    /// <see href="https://docs.snowflake.com/en/sql-reference/operators-subquery#all-any"/>
    /// </summary>
    /// <param name="Left">Left operand.</param>
    /// <param name="CompareOp">Comparison operator.</param>
    /// <param name="Right">Right-hand subquery expression.</param>
    /// <param name="IsSome">ANY and SOME are synonymous: <see href="https://docs.cloudera.com/cdw-runtime/cloud/using-hiveql/topics/hive_comparison_predicates.html"/></param>
    public sealed record AnyOp(Expr Left, BinaryOperator CompareOp, Expr Right, bool IsSome) : Expr;

    /// <summary>
    /// <c>ALL</c> operation e.g. <c>foo &gt; ALL(bar)</c>, comparison operator is one of <c>[=, &gt;, &lt;, =&gt;, =&lt;, !=]</c>
    /// <see href="https://docs.snowflake.com/en/sql-reference/operators-subquery#all-any"/>
    /// </summary>
    /// <param name="Left">Left operand.</param>
    /// <param name="CompareOp">Comparison operator.</param>
    /// <param name="Right">Right-hand subquery expression.</param>
    public sealed record AllOp(Expr Left, BinaryOperator CompareOp, Expr Right) : Expr;

    /// <summary>Unary operation e.g. <c>NOT foo</c></summary>
    /// <param name="Op">The unary operator (e.g., <c>NOT</c>, <c>-</c>).</param>
    /// <param name="Expr">Operand expression.</param>
    public sealed record UnaryOp(UnaryOperator Op, Expr Expr) : Expr;

    /// <summary>CONVERT a value to a different data type or character encoding. e.g. <c>CONVERT(foo USING utf8mb4)</c></summary>
    /// <param name="IsTry">
    /// CONVERT (false) or TRY_CONVERT (true)
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/try-convert-transact-sql?view=sql-server-ver16"/>
    /// </param>
    /// <param name="Expr">The expression to convert.</param>
    /// <param name="DataType">The target data type, if provided.</param>
    /// <param name="Charset">Optional target character encoding (e.g., <c>utf8mb4</c>).</param>
    /// <param name="TargetBeforeValue"><c>true</c> when target precedes the value (MSSQL syntax).</param>
    /// <param name="Styles">
    /// How to translate the expression.
    /// <para>[MSSQL]: https://learn.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver16#style</para>
    /// </param>
    public sealed record Convert(
        bool IsTry,
        Expr Expr,
        DataType? DataType,
        ObjectName? Charset,
        bool TargetBeforeValue,
        Sequence<Expr> Styles) : Expr;

    /// <summary><c>CAST</c> an expression to a different data type e.g. <c>CAST(foo AS VARCHAR(123))</c></summary>
    /// <param name="Kind">The cast kind (e.g., <c>CAST</c>, <c>TRY_CAST</c>).</param>
    /// <param name="Expr">Expression being cast.</param>
    /// <param name="DataType">Target data type.</param>
    /// <param name="Format">
    /// Optional CAST(string_expression AS type FORMAT format_string_expression) as used by [BigQuery]
    /// <para>[BigQuery]: https://cloud.google.com/bigquery/docs/reference/standard-sql/format-elements#formatting_syntax</para>
    /// </param>
    public sealed record Cast(CastKind Kind, Expr Expr, DataType DataType, CastFormat? Format) : Expr;

    /// <summary>AT a timestamp to a different timezone e.g. <c>FROM_UNIXTIME(0) AT TIME ZONE 'UTC-06:00'</c></summary>
    /// <param name="Timestamp">Timestamp expression to shift.</param>
    /// <param name="TimeZone">Time zone expression to apply.</param>
    public sealed record AtTimeZone(Expr Timestamp, Expr TimeZone) : Expr;

    /// <summary>
    /// Extract a field from a timestamp e.g. <c>EXTRACT(MONTH FROM foo)</c>
    /// Or <c>EXTRACT(MONTH, foo)</c>
    /// <para>Syntax:</para>
    /// <code>
    /// EXTRACT(DateTimeField FROM &lt;expr&gt;) | EXTRACT(DateTimeField, &lt;expr&gt;)
    /// </code>
    /// </summary>
    /// <param name="Field">Which datetime field is being extracted.</param>
    /// <param name="Syntax">Syntax variant used (<c>From</c> or <c>Comma</c>).</param>
    /// <param name="Expr">Expression to extract from.</param>
    public sealed record Extract(DateTimeField Field, ExtractSyntax Syntax, Expr Expr) : Expr;

    /// <summary>
    /// <code>
    /// CEIL(&lt;expr&gt; [TO DateTimeField])
    /// </code>
    /// <code>
    /// CEIL( &lt;input_expr&gt; [, &lt;scale_expr&gt; ] )
    /// </code>
    /// </summary>
    /// <param name="Expr">Expression to ceil.</param>
    /// <param name="Field">The CEIL/FLOOR kind (datetime field or scale).</param>
    public sealed record Ceil(Expr Expr, CeilFloorKind Field) : Expr;

    /// <summary>
    /// <code>
    /// FLOOR(&lt;expr&gt; [TO DateTimeField])
    /// </code>
    /// <code>
    /// FLOOR( &lt;input_expr&gt; [, &lt;scale_expr&gt; ] )
    /// </code>
    /// </summary>
    /// <param name="Expr">Expression to floor.</param>
    /// <param name="Field">The CEIL/FLOOR kind (datetime field or scale).</param>
    public sealed record Floor(Expr Expr, CeilFloorKind Field) : Expr;

    /// <summary>
    /// <code>
    /// POSITION(&lt;expr&gt; in &lt;expr&gt;)
    /// </code>
    /// </summary>
    /// <param name="Expr">Expression to search for.</param>
    /// <param name="In">Expression to search in.</param>
    // In: Rust: `r#in`.
    public sealed record Position(Expr Expr, Expr In) : Expr;

    /// <summary>
    /// <code>
    /// SUBSTRING(&lt;expr&gt; [FROM &lt;expr&gt;] [FOR &lt;expr&gt;])
    /// </code>
    /// or
    /// <code>
    /// SUBSTRING(&lt;expr&gt;, &lt;expr&gt;, &lt;expr&gt;)
    /// </code>
    /// </summary>
    /// <param name="Expr">Source expression.</param>
    /// <param name="SubstringFrom">Optional <c>FROM</c> expression.</param>
    /// <param name="SubstringFor">Optional <c>FOR</c> expression.</param>
    /// <param name="Special">
    /// false if the expression is represented using the <c>SUBSTRING(expr [FROM start] [FOR len])</c> syntax
    /// true if the expression is represented using the <c>SUBSTRING(expr, start, len)</c> syntax
    /// This flag is used for formatting.
    /// </param>
    /// <param name="Shorthand">
    /// true if the expression is represented using the <c>SUBSTR</c> shorthand
    /// This flag is used for formatting.
    /// </param>
    public sealed record Substring(Expr Expr, Expr? SubstringFrom, Expr? SubstringFor, bool Special, bool Shorthand) : Expr;

    /// <summary>
    /// <code>
    /// TRIM([BOTH | LEADING | TRAILING] [&lt;expr&gt; FROM] &lt;expr&gt;)
    /// TRIM(&lt;expr&gt;)
    /// TRIM(&lt;expr&gt;, [, characters]) -- PostgreSQL, DuckDB, Snowflake, BigQuery, Generic
    /// </code>
    /// </summary>
    /// <param name="TrimWhere">Which side to trim: <c>BOTH</c>, <c>LEADING</c>, or <c>TRAILING</c>.</param>
    /// <param name="TrimWhat">Optional expression specifying what to trim from the value <c>expr</c>.</param>
    /// <param name="Expr">The expression to trim from.</param>
    /// <param name="TrimCharacters">Optional list of characters to trim (dialect-specific).</param>
    public sealed record Trim(TrimWhereField? TrimWhere, Expr? TrimWhat, Expr Expr, Sequence<Expr>? TrimCharacters) : Expr;

    /// <summary>
    /// <code>
    /// OVERLAY(&lt;expr&gt; PLACING &lt;expr&gt; FROM &lt;expr&gt;[ FOR &lt;expr&gt; ]
    /// </code>
    /// </summary>
    /// <param name="Expr">The target expression being overlayed.</param>
    /// <param name="OverlayWhat">The expression to place into the target.</param>
    /// <param name="OverlayFrom">The <c>FROM</c> position expression indicating where to start overlay.</param>
    /// <param name="OverlayFor">Optional <c>FOR</c> length expression limiting the overlay span.</param>
    public sealed record Overlay(Expr Expr, Expr OverlayWhat, Expr OverlayFrom, Expr? OverlayFor) : Expr;

    /// <summary><c>expr COLLATE collation</c></summary>
    /// <param name="Expr">The expression being collated.</param>
    /// <param name="Collation">The collation name to apply to the expression.</param>
    public sealed record Collate(Expr Expr, ObjectName Collation) : Expr;

    /// <summary>Nested expression e.g. <c>(foo &gt; bar)</c> or <c>(1)</c></summary>
    public sealed record Nested(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary>A literal value, such as string, number, date or NULL</summary>
    // Inner: Rust: campo `.0` (variante chamada `Value`: parâmetro `Inner`, convencoes.md §6).
    public sealed record Value(ValueWithSpan Inner) : Expr;

    /// <summary>
    /// Prefixed expression, e.g. introducer strings, projection prefix
    /// <see href="https://dev.mysql.com/doc/refman/8.0/en/charset-introducer.html"/>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constructs/connect-by"/>
    /// </summary>
    /// <param name="Prefix">The prefix identifier (introducer or projection prefix).</param>
    /// <param name="Value">
    /// The value expression being prefixed.
    /// Hint: you can unwrap the string value using <c>value.into_string()</c>.
    /// </param>
    public sealed record Prefixed(Ident Prefix, Expr Value) : Expr
    {
        /// <summary>The value expression being prefixed.</summary>
        public new Expr Value { get; init; } = Value;
    }

    /// <summary>
    /// A constant of form <c>&lt;data_type&gt; 'value'</c>.
    /// This can represent ANSI SQL <c>DATE</c>, <c>TIME</c>, and <c>TIMESTAMP</c> literals (such as <c>DATE '2020-01-01'</c>),
    /// as well as constants of other types (a non-standard PostgreSQL extension).
    /// </summary>
    public sealed record TypedString(Ast.TypedString Value) : Expr
    {
        // Rust: campo `.0`.
        public new Ast.TypedString Value { get; init; } = Value;
    }

    /// <summary>Scalar function call e.g. <c>LEFT(foo, 5)</c></summary>
    public sealed record Function(Ast.Function Value) : Expr
    {
        // Rust: campo `.0`.
        public new Ast.Function Value { get; init; } = Value;
    }

    /// <summary>
    /// <c>CASE [&lt;operand&gt;] WHEN &lt;condition&gt; THEN &lt;result&gt; ... [ELSE &lt;result&gt;] END</c>
    /// <para>
    /// Note we only recognize a complete single expression as <c>&lt;condition&gt;</c>,
    /// not <c>&lt; 0</c> nor <c>1, 2, 3</c> as allowed in a <c>&lt;simple when clause&gt;</c> per
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2011-foundation-grammar.html#simple-when-clause"/>
    /// </para>
    /// </summary>
    /// <param name="CaseToken">The attached <c>CASE</c> token (keeps original spacing/comments).</param>
    /// <param name="EndToken">The attached <c>END</c> token (keeps original spacing/comments).</param>
    /// <param name="Operand">Optional operand expression after <c>CASE</c> (for simple CASE).</param>
    /// <param name="Conditions">The <c>WHEN ... THEN</c> conditions and results.</param>
    /// <param name="ElseResult">Optional <c>ELSE</c> result expression.</param>
    public sealed record Case(
        AttachedToken CaseToken,
        AttachedToken EndToken,
        Expr? Operand,
        Sequence<CaseWhen> Conditions,
        Expr? ElseResult) : Expr;

    /// <summary>
    /// An exists expression <c>[ NOT ] EXISTS(SELECT ...)</c>, used in expressions like
    /// <c>WHERE [ NOT ] EXISTS (SELECT ...)</c>.
    /// </summary>
    /// <param name="Subquery">The subquery checked by <c>EXISTS</c>.</param>
    /// <param name="Negated">Whether the <c>EXISTS</c> is negated (<c>NOT EXISTS</c>).</param>
    public sealed record Exists(Query Subquery, bool Negated) : Expr
    {
        /// <summary>The subquery checked by <c>EXISTS</c>.</summary>
        public new Query Subquery { get; init; } = Subquery;
    }

    /// <summary>
    /// A parenthesized subquery <c>(SELECT ...)</c>, used in expression like
    /// <c>SELECT (subquery) AS x</c> or <c>WHERE (subquery) = x</c>
    /// </summary>
    public sealed record Subquery(Query Value) : Expr
    {
        // Rust: campo `.0`.
        public new Query Value { get; init; } = Value;
    }

    /// <summary>The <c>GROUPING SETS</c> expr.</summary>
    public sealed record GroupingSets(Sequence<Sequence<Expr>> Value) : Expr
    {
        // Rust: campo `.0`.
        public new Sequence<Sequence<Expr>> Value { get; init; } = Value;
    }

    /// <summary>The <c>CUBE</c> expr.</summary>
    public sealed record Cube(Sequence<Sequence<Expr>> Value) : Expr
    {
        // Rust: campo `.0`.
        public new Sequence<Sequence<Expr>> Value { get; init; } = Value;
    }

    /// <summary>The <c>ROLLUP</c> expr.</summary>
    public sealed record Rollup(Sequence<Sequence<Expr>> Value) : Expr
    {
        // Rust: campo `.0`.
        public new Sequence<Sequence<Expr>> Value { get; init; } = Value;
    }

    /// <summary>ROW / TUPLE a single value, such as <c>SELECT (1, 2)</c></summary>
    public sealed record Tuple(Sequence<Expr> Value) : Expr
    {
        // Rust: campo `.0`.
        public new Sequence<Expr> Value { get; init; } = Value;
    }

    /// <summary>
    /// <c>Struct</c> literal expression
    /// Syntax:
    /// <code>
    /// STRUCT&lt;[field_name] field_type, ...&gt;( expr1 [, ... ])
    ///
    /// [BigQuery](https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#struct_type)
    /// [Databricks](https://docs.databricks.com/en/sql/language-manual/functions/struct.html)
    /// </code>
    /// </summary>
    /// <param name="Values">Struct values.</param>
    /// <param name="Fields">Struct field definitions.</param>
    public sealed record Struct(Sequence<Expr> Values, Sequence<StructField> Fields) : Expr;

    /// <summary>
    /// A named expression: <c>1 AS A</c>. Used in <c>BigQuery</c> typeless structs [1]
    /// and in aliased function arguments, e.g. <c>XMLFOREST(a AS x)</c> in
    /// PostgreSQL [2].
    /// <para>[1]: https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#struct_type</para>
    /// <para>[2]: https://www.postgresql.org/docs/current/functions-xml.html#FUNCTIONS-PRODUCING-XML-XMLFOREST</para>
    /// </summary>
    /// <param name="Expr">The expression being named.</param>
    /// <param name="Name">The assigned identifier name for the expression.</param>
    public sealed record Named(Expr Expr, Ident Name) : Expr;

    /// <summary>
    /// <c>DuckDB</c> specific <c>Struct</c> literal expression [1]
    /// <para>Syntax:</para>
    /// <code>
    /// syntax: {'field_name': expr1[, ... ]}
    /// </code>
    /// <para>[1]: https://duckdb.org/docs/sql/data_types/struct#creating-structs</para>
    /// </summary>
    public sealed record Dictionary(Sequence<DictionaryField> Value) : Expr
    {
        // Rust: campo `.0`.
        public new Sequence<DictionaryField> Value { get; init; } = Value;
    }

    /// <summary>
    /// <c>DuckDB</c> specific <c>Map</c> literal expression [1]
    /// <para>Syntax:</para>
    /// <code>
    /// syntax: Map {key1: value1[, ... ]}
    /// </code>
    /// <para>[1]: https://duckdb.org/docs/sql/data_types/map#creating-maps</para>
    /// </summary>
    public sealed record Map(Ast.Map Value) : Expr
    {
        // Rust: campo `.0`.
        public new Ast.Map Value { get; init; } = Value;
    }

    /// <summary>An array expression e.g. <c>ARRAY[1, 2]</c></summary>
    public sealed record Array(Ast.Array Value) : Expr
    {
        // Rust: campo `.0`.
        public new Ast.Array Value { get; init; } = Value;
    }

    /// <summary>An interval expression e.g. <c>INTERVAL '1' YEAR</c></summary>
    public sealed record Interval(Ast.Interval Value) : Expr
    {
        // Rust: campo `.0`.
        public new Ast.Interval Value { get; init; } = Value;
    }

    /// <summary>
    /// <c>MySQL</c> specific text search function [(1)].
    /// <para>Syntax:</para>
    /// <code>
    /// MATCH (&lt;col&gt;, &lt;col&gt;, ...) AGAINST (&lt;expr&gt; [&lt;search modifier&gt;])
    ///
    /// &lt;col&gt; = CompoundIdentifier
    /// &lt;expr&gt; = String literal
    /// </code>
    /// <para>[(1)]: https://dev.mysql.com/doc/refman/8.0/en/fulltext-search.html#function_match</para>
    /// </summary>
    /// <param name="Columns"><c>(&lt;col&gt;, &lt;col&gt;, ...)</c>.</param>
    /// <param name="MatchValue"><c>&lt;expr&gt;</c>.</param>
    /// <param name="OptSearchModifier"><c>&lt;search modifier&gt;</c></param>
    public sealed record MatchAgainst(Sequence<ObjectName> Columns, ValueWithSpan MatchValue, SearchModifier? OptSearchModifier) : Expr;

    /// <summary>An unqualified <c>*</c> wildcard token (e.g. <c>*</c>).</summary>
    public sealed record Wildcard(AttachedToken Value) : Expr
    {
        // Rust: campo `.0`.
        public new AttachedToken Value { get; init; } = Value;
    }

    /// <summary>
    /// Qualified wildcard, e.g. <c>alias.*</c> or <c>schema.table.*</c>.
    /// (Same caveats apply to <c>QualifiedWildcard</c> as to <c>Wildcard</c>.)
    /// </summary>
    // Prefix: Rust: campo `.0` (o prefixo antes de `.*`).
    // Token: Rust: campo `.1` (o token `*`).
    public sealed record QualifiedWildcard(ObjectName Prefix, AttachedToken Token) : Expr;

    /// <summary>
    /// Some dialects support an older syntax for outer joins where columns are
    /// marked with the <c>(+)</c> operator in the WHERE clause, for example:
    /// <code>
    /// SELECT t1.c1, t2.c2 FROM t1, t2 WHERE t1.c1 = t2.c2 (+)
    /// </code>
    /// which is equivalent to
    /// <code>
    /// SELECT t1.c1, t2.c2 FROM t1 LEFT OUTER JOIN t2 ON t1.c1 = t2.c2
    /// </code>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/where#joins-in-the-where-clause"/>.
    /// </summary>
    public sealed record OuterJoin(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary>A reference to the prior level in a CONNECT BY clause.</summary>
    public sealed record Prior(Expr Value) : Expr
    {
        // Rust: campo `.0`.
        public new Expr Value { get; init; } = Value;
    }

    /// <summary>
    /// A lambda function.
    /// <para>Syntax:</para>
    /// <code>
    /// param -&gt; expr | (param1, ...) -&gt; expr
    /// </code>
    /// <para><see href="https://clickhouse.com/docs/en/sql-reference/functions#higher-order-functions---operator-and-lambdaparams-expr-function">ClickHouse</see></para>
    /// <para><see href="https://docs.databricks.com/en/sql/language-manual/sql-ref-lambda-functions.html">Databricks</see></para>
    /// <para><see href="https://duckdb.org/docs/stable/sql/functions/lambda">DuckDB</see></para>
    /// </summary>
    public sealed record Lambda(LambdaFunction Value) : Expr
    {
        // Rust: campo `.0`.
        public new LambdaFunction Value { get; init; } = Value;
    }

    /// <summary>Checks membership of a value in a JSON array</summary>
    public sealed record MemberOf(Ast.MemberOf Value) : Expr
    {
        // Rust: campo `.0`.
        public new Ast.MemberOf Value { get; init; } = Value;
    }

    // impl Expr { pub fn value(value: impl Into<ValueWithSpan>) -> Self }
    // CS0102: o nome `Value` já é a variante aninhada; ver "Nomes" em docs/port/onda-a/A1.md.
    // `impl Into<ValueWithSpan>` aceita `ValueWithSpan` e `Value` (via `impl From<Value> for ValueWithSpan`).

    /// <summary>Creates a new <c>Expr::Value</c></summary>
    public static Expr MakeValue(ValueWithSpan value) => new Value(value);

    /// <summary>Creates a new <c>Expr::Value</c></summary>
    public static Expr MakeValue(Ast.Value value) => new Value(ValueWithSpan.From(value));
}
