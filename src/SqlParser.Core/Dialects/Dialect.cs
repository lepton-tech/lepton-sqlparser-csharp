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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/mod.rs.
// Modificado: tradução de Rust para C#.
//
// Itens de mod.rs sem tradução como código (ver docs/port/convencoes.md §13):
// - macros dialect_of! e dialect_is!: viram teste de tipo do C# (`dialect is A or B`);
// - derive_dialect! (feature "derive-dialect"): vira herança (`class X : GenericDialect`);
// - fn dialect() -> TypeId e `impl dyn Dialect { fn is<T> }`: a identidade do dialeto é o tipo C#.
// O enum Precedence está em Precedence.cs. Os hooks que recebem Parser ficam aqui mesmo, na posição
// do Rust (convencoes.md §13, caso 53).

using SqlParser.Core.Ast;

namespace SqlParser.Core.Dialects;

/// <summary>
/// Encapsulates the differences between SQL implementations.
/// <para>
/// # SQL Dialects
/// </para>
/// <para>
/// SQL implementations deviate from one another, either due to
/// custom extensions or various historical reasons. This trait
/// encapsulates the parsing differences between dialects.
/// </para>
/// <para>
/// <see cref="GenericDialect"/> is the most permissive dialect, and parses the union of
/// all the other dialects, when there is no ambiguity. However, it does not
/// currently allow <c>CREATE TABLE</c> statements without types specified for all
/// columns; use <see cref="SQLiteDialect"/> if you require that.
/// </para>
/// <para>
/// # Examples
/// Most users create a <see cref="Dialect"/> directly, as shown on the [module
/// level documentation]:
/// </para>
/// <code>
/// # use sqlparser::dialect::AnsiDialect;
/// let dialect = AnsiDialect {};
/// </code>
/// <para>
/// It is also possible to dynamically create a <see cref="Dialect"/> from its
/// name. For example:
/// </para>
/// <code>
/// # use sqlparser::dialect::{AnsiDialect, dialect_from_str};
/// let dialect = dialect_from_str("ansi").unwrap();
///
/// // Parsed dialect is an instance of `AnsiDialect`:
/// assert!(dialect.is::&lt;AnsiDialect&gt;());
/// </code>
/// <para>
/// [module level documentation]: crate
/// </para>
/// </summary>
public abstract partial class Dialect
{
    // fn dialect(&self) -> TypeId (mod.rs:209): não portado. Em C# a identidade do dialeto é o
    // próprio tipo (`dialect is GenericDialect`), e um dialeto que "se passa" por outro herda dele.
    // Ver convencoes.md §13 e divergencias.md (DIV-002).

    /// <summary>
    /// Determine if a character starts a quoted identifier. The default
    /// implementation, accepting "double quoted" ids is both ANSI-compliant
    /// and appropriate for most dialects (with the notable exception of
    /// MySQL, MS SQL, and sqlite). You can accept one of characters listed
    /// in <c>Word::matching_end_quote</c> here
    /// </summary>
    public virtual bool IsDelimitedIdentifierStart(char ch) => ch == '"' || ch == '`';

    /// <summary>
    /// Determine if a character starts a potential nested quoted identifier.
    /// Example: RedShift supports the following quote styles to all mean the same thing:
    /// <code>
    /// SELECT 1 AS foo;
    /// SELECT 1 AS "foo";
    /// SELECT 1 AS [foo];
    /// SELECT 1 AS ["foo"];
    /// </code>
    /// </summary>
    public virtual bool IsNestedDelimitedIdentifierStart(char ch) => false;

    /// <summary>
    /// Only applicable whenever <see cref="IsNestedDelimitedIdentifierStart"/> returns true
    /// If the next sequence of tokens potentially represent a nested identifier, then this method
    /// returns a tuple containing the outer quote style, and if present, the inner (nested) quote style.
    /// <para>
    /// Example (Redshift):
    /// </para>
    /// <code>
    /// `["foo"]` => Some(`[`, Some(`"`))
    /// `[foo]` => Some(`[`, None)
    /// `[0]` => None
    /// `"foo"` => None
    /// </code>
    /// </summary>
    // Rust: `mut _chars: Peekable<Chars<'_>>` (um clone do iterador do tokenizer) → PeekableChars,
    // struct copiado por valor. Retorno `Option<(char, Option<char>)>`: nomes da tupla em
    // docs/port/nomes.md.
    public virtual (char QuoteStart, char? NestedQuoteStart)? PeekNestedDelimitedIdentifierQuotes(PeekableChars chars) =>
        null;

    /// <summary>
    /// Return the character used to quote identifiers.
    /// </summary>
    public virtual char? IdentifierQuoteStyle(string identifier) => null;

    /// <summary>
    /// Determine if a character is a valid start character for an unquoted identifier
    /// </summary>
    public abstract bool IsIdentifierStart(char ch);

    /// <summary>
    /// Determine if a character is a valid unquoted identifier character
    /// </summary>
    public abstract bool IsIdentifierPart(char ch);

    /// <summary>
    /// Most dialects do not have custom operators. Override this method to provide custom operators.
    /// </summary>
    public virtual bool IsCustomOperatorPart(char ch) => false;

    /// <summary>
    /// Determine if the dialect supports escaping characters via '\' in string literals.
    /// <para>
    /// Some dialects like BigQuery and Snowflake support this while others like
    /// Postgres do not. Such that the following is accepted by the former but
    /// rejected by the latter.
    /// </para>
    /// <code>
    /// SELECT 'ab\'cd';
    /// </code>
    /// <para>
    /// Conversely, such dialects reject the following statement which
    /// otherwise would be valid in the other dialects.
    /// </para>
    /// <code>
    /// SELECT '\';
    /// </code>
    /// </summary>
    public virtual bool SupportsStringLiteralBackslashEscape => false;

    /// <summary>
    /// Does the dialect tokenize <c>N'...'</c> as a national string literal?
    /// <para>
    /// Dialects such as SQLite treat <c>N</c> as a plain identifier, so <c>N'foo'</c> is
    /// the identifier <c>N</c> followed by a string literal, not a national string.
    /// </para>
    /// </summary>
    public virtual bool SupportsNationalStringLiteral => true;

    /// <summary>
    /// Determine whether the dialect strips the backslash when escaping LIKE wildcards (%, _).
    /// <para>
    /// [MySQL] has a special case when escaping single quoted strings which leaves these unescaped
    /// so they can be used in LIKE patterns without double-escaping (as is necessary in other
    /// escaping dialects, such as [Snowflake]). Generally, special characters have escaping rules
    /// causing them to be replaced with a different byte sequences (e.g. <c>'\0'</c> becoming the zero
    /// byte), and the default if an escaped character does not have a specific escaping rule is to
    /// strip the backslash (e.g. there is no rule for <c>h</c>, so <c>'\h' = 'h'</c>). MySQL's special case
    /// for ignoring LIKE wildcard escapes is to *not* strip the backslash, so that <c>'\%' = '\\%'</c>.
    /// This applies to all string literals though, not just those used in LIKE patterns.
    /// </para>
    /// <code>
    /// mysql&gt; select '\_', hex('\\'), hex('_'), hex('\_');
    /// +----+-----------+----------+-----------+
    /// | \_ | hex('\\') | hex('_') | hex('\_') |
    /// +----+-----------+----------+-----------+
    /// | \_ | 5C        | 5F       | 5C5F      |
    /// +----+-----------+----------+-----------+
    /// 1 row in set (0.00 sec)
    /// </code>
    /// <para>
    /// [MySQL]: https://dev.mysql.com/doc/refman/8.4/en/string-literals.html
    /// [Snowflake]: https://docs.snowflake.com/en/sql-reference/functions/like#usage-notes
    /// </para>
    /// </summary>
    public virtual bool IgnoresWildcardEscapes => false;

    /// <summary>
    /// Determine if the dialect supports string literals with <c>U&amp;</c> prefix.
    /// This is used to specify Unicode code points in string literals.
    /// For example, in PostgreSQL, the following is a valid string literal:
    /// <code>
    /// SELECT U&amp;'\0061\0062\0063';
    /// </code>
    /// <para>
    /// This is equivalent to the string literal <c>'abc'</c>.
    /// See
    ///  - <see href="https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-STRINGS-UESCAPE">Postgres docs</see>
    ///  - <see href="http://www.h2database.com/html/grammar.html#string">H2 docs</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsUnicodeStringLiteral => false;

    /// <summary>
    /// Does the dialect support <c>FILTER (WHERE expr)</c> for aggregate queries?
    /// </summary>
    public virtual bool SupportsFilterDuringAggregation => false;

    /// <summary>
    /// Returns true if the dialect supports referencing another named window
    /// within a window clause declaration.
    /// <para>
    /// Example
    /// </para>
    /// <code>
    /// SELECT * FROM mytable
    /// WINDOW mynamed_window AS another_named_window
    /// </code>
    /// </summary>
    public virtual bool SupportsWindowClauseNamedWindowReference => false;

    /// <summary>
    /// Returns true if the dialect supports <c>ARRAY_AGG() [WITHIN GROUP (ORDER BY)]</c> expressions.
    /// Otherwise, the dialect should expect an <c>ORDER BY</c> without the <c>WITHIN GROUP</c> clause, e.g. <c>ANSI</c>
    /// <para>
    /// <c>ANSI</c>: https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#array-aggregate-function
    /// </para>
    /// </summary>
    public virtual bool SupportsWithinAfterArrayAggregation => false;

    /// <summary>
    /// Returns true if the dialect supports <c>PARTITION BY</c> appearing after <c>ORDER BY</c>
    /// in a <c>CREATE TABLE</c> statement (in addition to the standard placement before <c>ORDER BY</c>).
    /// <para>
    /// ClickHouse DDL uses this ordering:
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/create/table#partition-by"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsPartitionByAfterOrderBy => false;

    /// <summary>
    /// Returns true if the dialect supports ClickHouse-style <c>ARRAY JOIN</c> / <c>LEFT ARRAY JOIN</c> /
    /// <c>INNER ARRAY JOIN</c> syntax for unnesting arrays inline.
    /// <para>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/array-join"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsArrayJoinSyntax => false;

    /// <summary>
    /// Returns true if the dialect treats <c>ALTER USER</c> as a synonym for <c>ALTER ROLE</c>.
    /// <para>
    /// In PostgreSQL, <c>ALTER USER</c> and <c>ALTER ROLE</c> are synonyms that accept the same
    /// option syntax, so <c>ALTER USER</c> is parsed into a <c>Statement::AlterRole</c>.
    /// </para>
    /// <para>
    /// <see href="https://www.postgresql.org/docs/current/sql-alteruser.html"/>
    /// </para>
    /// <para>
    /// <c>Statement::AlterRole</c>: crate::ast::Statement::AlterRole
    /// </para>
    /// </summary>
    public virtual bool SupportsAlterUserAsAlterRole => false;

    /// <summary>
    /// Returns true if the dialects supports <c>group sets, roll up, or cube</c> expressions.
    /// </summary>
    public virtual bool SupportsGroupByExpr => false;

    /// <summary>
    /// Returns true if the dialects supports <c>GROUP BY</c> modifiers prefixed by a <c>WITH</c> keyword.
    /// Example: <c>GROUP BY value WITH ROLLUP</c>.
    /// </summary>
    public virtual bool SupportsGroupByWithModifier => false;

    /// <summary>
    /// Indicates whether the dialect supports left-associative join parsing
    /// by default when parentheses are omitted in nested joins.
    /// <para>
    /// Most dialects (like MySQL or Postgres) assume **left-associative** precedence,
    /// so a query like:
    /// </para>
    /// <code>
    /// SELECT * FROM t1 NATURAL JOIN t5 INNER JOIN t0 ON ...
    /// </code>
    /// <para>
    /// is interpreted as:
    /// </para>
    /// <code>
    /// ((t1 NATURAL JOIN t5) INNER JOIN t0 ON ...)
    /// </code>
    /// <para>
    /// and internally represented as a **flat list** of joins.
    /// </para>
    /// <para>
    /// In contrast, some dialects (e.g. **Snowflake**) assume **right-associative**
    /// precedence and interpret the same query as:
    /// </para>
    /// <code>
    /// (t1 NATURAL JOIN (t5 INNER JOIN t0 ON ...))
    /// </code>
    /// <para>
    /// which results in a **nested join** structure in the AST.
    /// </para>
    /// <para>
    /// If this method returns <c>false</c>, the parser must build nested join trees
    /// even in the absence of parentheses to reflect the correct associativity
    /// </para>
    /// </summary>
    public virtual bool SupportsLeftAssociativeJoinsWithoutParens => true;

    /// <summary>
    /// Returns true if the dialect supports the <c>(+)</c> syntax for OUTER JOIN.
    /// </summary>
    public virtual bool SupportsOuterJoinOperator => false;

    /// <summary>
    /// Returns true if the dialect supports a join specification on CROSS JOIN.
    /// </summary>
    public virtual bool SupportsCrossJoinConstraint => false;

    /// <summary>
    /// Returns true if the dialect supports CONNECT BY.
    /// </summary>
    public virtual bool SupportsConnectBy => false;

    /// <summary>
    /// Returns true if the dialect supports <c>EXECUTE IMMEDIATE</c> statements.
    /// </summary>
    public virtual bool SupportsExecuteImmediate => false;

    /// <summary>
    /// Returns true if the dialect supports the MATCH_RECOGNIZE operation.
    /// </summary>
    public virtual bool SupportsMatchRecognize => false;

    /// <summary>
    /// Returns true if the dialect supports <c>(NOT) IN ()</c> expressions
    /// </summary>
    public virtual bool SupportsInEmptyList => false;

    /// <summary>
    /// Returns true if the dialect supports a bare expression as the right-hand
    /// side of <c>IN</c>, without a parenthesized list — as in <c>x IN 'a'</c>.
    /// </summary>
    public virtual bool SupportsInUnparenthesizedExpr => false;

    /// <summary>Returns true if the dialect supports <c>GLOBAL IN</c> and <c>GLOBAL NOT IN</c>.</summary>
    public virtual bool SupportsGlobalIn => false;

    /// <summary>
    /// Returns true if the dialect supports <c>BEGIN {DEFERRED | IMMEDIATE | EXCLUSIVE | TRY | CATCH} [TRANSACTION]</c> statements
    /// </summary>
    public virtual bool SupportsStartTransactionModifier => false;

    /// <summary>
    /// Returns true if the dialect supports <c>END {TRY | CATCH}</c> statements
    /// </summary>
    public virtual bool SupportsEndTransactionModifier => false;

    /// <summary>
    /// Returns true if the dialect supports named arguments of the form <c>FUN(a = '1', b = '2')</c>.
    /// </summary>
    public virtual bool SupportsNamedFnArgsWithEqOperator => false;

    /// <summary>
    /// Returns true if the dialect supports named arguments of the form <c>FUN(a : '1', b : '2')</c>.
    /// </summary>
    public virtual bool SupportsNamedFnArgsWithColonOperator => false;

    /// <summary>
    /// Returns true if the dialect supports named arguments of the form <c>FUN(a := '1', b := '2')</c>.
    /// </summary>
    public virtual bool SupportsNamedFnArgsWithAssignmentOperator => false;

    /// <summary>
    /// Returns true if the dialect supports named arguments of the form <c>FUN(a =&gt; '1', b =&gt; '2')</c>.
    /// </summary>
    public virtual bool SupportsNamedFnArgsWithRarrowOperator => true;

    /// <summary>
    /// Returns true if dialect supports argument name as arbitrary expression.
    /// e.g. <c>FUN(LOWER('a'):'1',  b:'2')</c>
    /// Such function arguments are represented in the AST by the <c>FunctionArg::ExprNamed</c> variant,
    /// otherwise use the <c>FunctionArg::Named</c> variant (compatible reason).
    /// </summary>
    public virtual bool SupportsNamedFnArgsWithExprName => false;

    /// <summary>
    /// Returns true if the dialect supports identifiers starting with a numeric
    /// prefix such as tables named <c>59901_user_login</c>
    /// </summary>
    public virtual bool SupportsNumericPrefix => false;

    /// <summary>
    /// Returns true if a period directly after an identifier starts a tuple
    /// element access such as <c>t.1</c>, instead of the number <c>.1</c>.
    /// </summary>
    public virtual bool SupportsTupleElementAccess => false;

    /// <summary>
    /// Returns true if the dialect supports numbers containing underscores, e.g. <c>10_000_000</c>
    /// </summary>
    public virtual bool SupportsNumericLiteralUnderscores => false;

    /// <summary>
    /// Returns true if the dialects supports specifying null treatment
    /// as part of a window function's parameter list as opposed
    /// to after the parameter list.
    /// <para>
    /// i.e The following syntax returns true
    /// </para>
    /// <code>
    /// FIRST_VALUE(a IGNORE NULLS) OVER ()
    /// </code>
    /// <para>
    /// while the following syntax returns false
    /// </para>
    /// <code>
    /// FIRST_VALUE(a) IGNORE NULLS OVER ()
    /// </code>
    /// </summary>
    public virtual bool SupportsWindowFunctionNullTreatmentArg => false;

    /// <summary>
    /// Returns true if the dialect supports defining structs or objects using a
    /// syntax like <c>{'x': 1, 'y': 2, 'z': 3}</c>.
    /// </summary>
    public virtual bool SupportsDictionarySyntax => false;

    /// <summary>
    /// Returns true if the dialect supports defining object using the
    /// syntax like <c>Map {1: 10, 2: 20}</c>.
    /// </summary>
    public virtual bool SupportMapLiteralSyntax => false;

    /// <summary>
    /// Returns true if the dialect supports lambda functions, for example:
    /// <code>
    /// SELECT transform(array(1, 2, 3), x -&gt; x + 1); -- returns [2,3,4]
    /// </code>
    /// <para>
    /// This enables both the <c>-&gt;</c> spelling above and the <c>LAMBDA</c> keyword
    /// spelling gated by <see cref="SupportsLambdaKeywordSyntax"/>. A dialect
    /// that uses <c>-&gt;</c> as a binary operator should override only the latter.
    /// </para>
    /// </summary>
    public virtual bool SupportsLambdaFunctions => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>LAMBDA</c> keyword spelling of
    /// lambda functions, for example:
    /// <code>
    /// SELECT list_transform([1, 2, 3], lambda x : x + 1); -- returns [2, 3, 4]
    /// </code>
    /// <para>
    /// This spelling does not claim the <c>-&gt;</c> token, so it can be enabled by
    /// dialects that already give <c>-&gt;</c> a different meaning, such as JSON
    /// member access.
    /// </para>
    /// <para>
    /// Defaults to <see cref="SupportsLambdaFunctions"/>, so dialects supporting
    /// the <c>-&gt;</c> spelling accept the <c>LAMBDA</c> spelling too unless they say
    /// otherwise.
    /// </para>
    /// <para>
    /// See <see href="https://duckdb.org/docs/stable/sql/functions/lambda"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsLambdaKeywordSyntax => SupportsLambdaFunctions;

    /// <summary>
    /// Returns true if the dialect supports multiple variable assignment
    /// using parentheses in a <c>SET</c> variable declaration.
    /// <code>
    /// SET (variable[, ...]) = (expression[, ...]);
    /// </code>
    /// </summary>
    public virtual bool SupportsParenthesizedSetVariables => false;

    /// <summary>
    /// Returns true if the dialect supports multiple <c>SET</c> statements
    /// in a single statement.
    /// <code>
    /// SET variable = expression [, variable = expression];
    /// </code>
    /// </summary>
    public virtual bool SupportsCommaSeparatedSetAssignments => false;

    /// <summary>
    /// Returns true if the dialect supports <c>ORDER BY</c> in <c>UPDATE</c> statements.
    /// <code>
    /// UPDATE foo SET bar = false WHERE foo = true ORDER BY foo ASC;
    /// </code>
    /// <para>
    /// See <see href="https://dev.mysql.com/doc/refman/8.4/en/update.html"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsUpdateOrderBy => false;

    /// <summary>
    /// Returns true if the dialect supports an <c>EXCEPT</c> clause following a
    /// wildcard in a select list.
    /// <para>
    /// For example
    /// </para>
    /// <code>
    /// SELECT * EXCEPT order_id FROM orders;
    /// </code>
    /// </summary>
    public virtual bool SupportsSelectWildcardExcept => false;

    /// <summary>
    /// Returns true if the dialect has a CONVERT function which accepts a type first
    /// and an expression second, e.g. <c>CONVERT(varchar, 1)</c>
    /// </summary>
    public virtual bool ConvertTypeBeforeValue => false;

    /// <summary>
    /// Returns true if the dialect supports triple quoted string
    /// e.g. <c>"""abc"""</c>
    /// </summary>
    public virtual bool SupportsTripleQuotedString => false;

    /// <summary>Dialect-specific prefix parser override</summary>
    // Rust: `Option<Result<Expr, ParserError>>` → `null` para "use o comportamento padrão";
    // erro → ParserException (convencoes.md §13, caso 53).
    public virtual Expr? ParsePrefix(Parser parser)
    {
        // return None to fall back to the default behavior
        return null;
    }

    /// <summary>
    /// Does the dialect support the <c>APPROXIMATE PERCENTILE_DISC</c> function syntax?
    /// See <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_APPROXIMATE_PERCENTILE_DISC.html"/>
    /// </summary>
    public virtual bool SupportsApproximatePercentileDisc => false;

    /// <summary>
    /// Does the dialect support trailing commas around the query?
    /// </summary>
    public virtual bool SupportsTrailingCommas => false;

    /// <summary>
    /// Does the dialect support parsing <c>LIMIT 1, 2</c> as <c>LIMIT 2 OFFSET 1</c>?
    /// </summary>
    public virtual bool SupportsLimitComma => false;

    /// <summary>
    /// Returns true if the dialect supports concatenating of string literal
    /// Example: <c>SELECT 'Hello ' "world" =&gt; SELECT 'Hello world'</c>
    /// </summary>
    public virtual bool SupportsStringLiteralConcatenation => false;

    /// <summary>
    /// Returns true if the dialect supports concatenating string literals with a newline.
    /// For example, the following statement would return <c>true</c>:
    /// <code>
    /// SELECT 'abc' in (
    ///   'a'
    ///   'b'
    ///   'c'
    /// );
    /// </code>
    /// </summary>
    public virtual bool SupportsStringLiteralConcatenationWithNewline => false;

    /// <summary>
    /// Does the dialect support trailing commas in the projection list?
    /// </summary>
    public virtual bool SupportsProjectionTrailingCommas => SupportsTrailingCommas;

    /// <summary>
    /// Returns true if the dialect supports trailing commas in the <c>FROM</c> clause of a <c>SELECT</c> statement.
    /// Example: <c>SELECT 1 FROM T, U, LIMIT 1</c>
    /// </summary>
    public virtual bool SupportsFromTrailingCommas => false;

    /// <summary>
    /// Returns true if the dialect supports trailing commas in the
    /// column definitions list of a <c>CREATE</c> statement.
    /// Example: <c>CREATE TABLE T (x INT, y TEXT,)</c>
    /// </summary>
    public virtual bool SupportsColumnDefinitionTrailingCommas => false;

    /// <summary>
    /// Returns true if the dialect supports double dot notation for object names
    /// <para>
    /// Example
    /// </para>
    /// <code>
    /// SELECT * FROM db_name..table_name
    /// </code>
    /// </summary>
    public virtual bool SupportsObjectNameDoubleDotNotation => false;

    /// <summary>
    /// Return true if the dialect supports the STRUCT literal
    /// <para>
    /// Example
    /// </para>
    /// <code>
    /// SELECT STRUCT(1 as one, 'foo' as foo, false)
    /// </code>
    /// </summary>
    public virtual bool SupportsStructLiteral => false;

    /// <summary>
    /// Return true if the dialect supports empty projections in SELECT statements
    /// <para>
    /// Example
    /// </para>
    /// <code>
    /// SELECT from table_name
    /// </code>
    /// </summary>
    public virtual bool SupportsEmptyProjections => false;

    /// <summary>
    /// Return true if the dialect supports wildcard expansion on
    /// arbitrary expressions in projections.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT STRUCT&lt;STRING&gt;('foo').* FROM T
    /// </code>
    /// </summary>
    public virtual bool SupportsSelectExprStar => false;

    /// <summary>
    /// Return true if the dialect supports "FROM-first" selects.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// FROM table
    /// SELECT *
    /// </code>
    /// </summary>
    public virtual bool SupportsFromFirstSelect => false;

    /// <summary>
    /// Return true if the dialect supports "FROM-first" inserts.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// WITH cte AS (SELECT key FROM src)
    /// FROM cte
    /// INSERT OVERWRITE table my_table
    /// SELECT *
    ///
    /// See &lt;https://hive.apache.org/docs/latest/language/common-table-expression/&gt;
    /// </code>
    /// </summary>
    public virtual bool SupportsFromFirstInsert => false;

    /// <summary>
    /// Return true if the dialect supports pipe operator.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT *
    /// FROM table
    /// |&gt; limit 1
    /// </code>
    /// <para>
    /// See <see href="https://cloud.google.com/bigquery/docs/pipe-syntax-guide#basic_syntax"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsPipeOperator => false;

    /// <summary>
    /// Does the dialect support MySQL-style <c>'user'@'host'</c> grantee syntax?
    /// </summary>
    public virtual bool SupportsUserHostGrantee => false;

    /// <summary>
    /// Does the dialect support the <c>MATCH() AGAINST()</c> syntax?
    /// </summary>
    public virtual bool SupportsMatchAgainst => false;

    /// <summary>
    /// Returns true if the dialect supports an exclude option
    /// following a wildcard in the projection section. For example:
    /// <c>SELECT * EXCLUDE col1 FROM tbl</c>.
    /// <para>
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_EXCLUDE_list.html">Redshift</see>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/select">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSelectWildcardExclude => false;

    /// <summary>
    /// Returns true if the dialect supports an exclude option
    /// as the last item in the projection section, not necessarily
    /// after a wildcard. For example:
    /// <c>SELECT *, c1, c2 EXCLUDE c3 FROM tbl</c>
    /// <para>
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_EXCLUDE_list.html">Redshift</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSelectExclude => false;

    /// <summary>
    /// Returns true if the dialect supports specifying multiple options
    /// in a <c>CREATE TABLE</c> statement for the structure of the new table. For example:
    /// <c>CREATE TABLE t (a INT, b INT) AS SELECT 1 AS b, 2 AS a</c>
    /// </summary>
    public virtual bool SupportsCreateTableMultiSchemaInfoSources => false;

    /// <summary>
    /// Returns true if the dialect supports MySQL-specific SELECT modifiers
    /// like <c>HIGH_PRIORITY</c>, <c>STRAIGHT_JOIN</c>, <c>SQL_SMALL_RESULT</c>, etc.
    /// <para>
    /// For example:
    /// </para>
    /// <code>
    /// SELECT HIGH_PRIORITY STRAIGHT_JOIN SQL_SMALL_RESULT * FROM t1 JOIN t2 ON ...
    /// </code>
    /// <para>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html">MySQL</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSelectModifiers => false;

    /// <summary>Dialect-specific infix parser override</summary>
    /// <remarks>
    /// This method is called to parse the next infix expression.
    /// If <c>None</c> is returned, falls back to the default behavior.
    /// </remarks>
    public virtual Expr? ParseInfix(Parser parser, Expr expr, byte precedence)
    {
        // return None to fall back to the default behavior
        return null;
    }

    /// <summary>Dialect-specific precedence override</summary>
    /// <remarks>
    /// This method is called to get the precedence of the next token.
    /// If <c>None</c> is returned, falls back to the default behavior.
    /// </remarks>
    public virtual byte? GetNextPrecedence(Parser parser)
    {
        // return None to fall back to the default behavior
        return null;
    }

    /// <summary>Get the precedence of the next token, looking at the full token stream.</summary>
    /// <remarks>
    /// A higher number =&gt; higher precedence.
    /// See <see cref="GetNextPrecedence"/> to override the behavior for just the
    /// next token.
    /// The default implementation is used for many dialects, but can be
    /// overridden to provide dialect-specific behavior.
    /// </remarks>
    public virtual byte GetNextPrecedenceDefault(Parser parser)
    {
        if (GetNextPrecedence(parser) is { } precedence)
        {
            return precedence;
        }

        // macro_rules! p → PrecValue(Precedence.X)
        var token = parser.PeekTokenRef();
        // Rust: debug!("get_next_precedence_full() {token:?}"): log não traduzido (caso 44).
        switch (token.Token)
        {
            case Token.Word { Value: var w } when w.Keyword == Keyword.OR:
                return PrecValue(Precedence.Or);
            case Token.Word { Value: var w } when w.Keyword == Keyword.AND:
                return PrecValue(Precedence.And);
            case Token.Word { Value: var w } when w.Keyword == Keyword.XOR:
                return PrecValue(Precedence.Xor);

            case Token.Word { Value: var w } when w.Keyword == Keyword.AT:
                switch ((parser.PeekNthTokenRef(1).Token, parser.PeekNthTokenRef(2).Token))
                {
                    case (Token.Word { Value: var w1 }, Token.Word { Value: var w2 })
                        when w1.Keyword == Keyword.TIME && w2.Keyword == Keyword.ZONE:
                        return PrecValue(Precedence.AtTz);
                    default:
                        return PrecUnknown;
                }

            case Token.Word { Value: var w } when w.Keyword == Keyword.NOT:
                switch (parser.PeekNthTokenRef(1).Token)
                {
                    // The precedence of NOT varies depending on keyword that
                    // follows it. If it is followed by IN, BETWEEN, or LIKE,
                    // it takes on the precedence of those tokens. Otherwise, it
                    // is not an infix operator, and therefore has zero
                    // precedence.
                    case Token.Word { Value: var n } when n.Keyword == Keyword.IN:
                        return PrecValue(Precedence.Between);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.BETWEEN:
                        return PrecValue(Precedence.Between);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.LIKE:
                        return PrecValue(Precedence.Like);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.ILIKE:
                        return PrecValue(Precedence.Like);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.RLIKE:
                        return PrecValue(Precedence.Like);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.REGEXP:
                        return PrecValue(Precedence.Like);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.MATCH:
                        return PrecValue(Precedence.Like);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.GLOB:
                        return PrecValue(Precedence.Like);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.SIMILAR:
                        return PrecValue(Precedence.Like);
                    case Token.Word { Value: var n } when n.Keyword == Keyword.MEMBER:
                        return PrecValue(Precedence.Like);
                    case Token.Word { Value: var n }
                        when n.Keyword == Keyword.NULL && !parser.InColumnDefinitionState():
                        return PrecValue(Precedence.Is);
                    default:
                        return PrecUnknown;
                }
            case Token.Word { Value: var w } when w.Keyword == Keyword.NOTNULL && SupportsNotnullOperator:
                return PrecValue(Precedence.Is);
            case Token.Word { Value: var w } when w.Keyword == Keyword.IS:
                return PrecValue(Precedence.Is);
            case Token.Word { Value: var w } when w.Keyword == Keyword.GLOBAL && SupportsGlobalIn:
                // Rust: o `w` do padrão interno sombreia o externo: `w1` (caso 72), como no braço `AT`.
                switch ((parser.PeekNthTokenRef(1).Token, parser.PeekNthTokenRef(2).Token))
                {
                    case (Token.Word { Value: var w1 }, _) when w1.Keyword == Keyword.IN:
                        return PrecValue(Precedence.Between);
                    case (Token.Word { Value: var w1 }, Token.Word { Value: var w2 })
                        when w1.Keyword == Keyword.NOT && w2.Keyword == Keyword.IN:
                        return PrecValue(Precedence.Between);
                    default:
                        return PrecUnknown;
                }
            case Token.Word { Value: var w } when w.Keyword == Keyword.IN:
                return PrecValue(Precedence.Between);
            case Token.Word { Value: var w } when w.Keyword == Keyword.BETWEEN:
                return PrecValue(Precedence.Between);
            case Token.Word { Value: var w } when w.Keyword == Keyword.OVERLAPS:
                return PrecValue(Precedence.Between);
            case Token.Word { Value: var w } when w.Keyword == Keyword.LIKE:
                return PrecValue(Precedence.Like);
            case Token.Word { Value: var w } when w.Keyword == Keyword.ILIKE:
                return PrecValue(Precedence.Like);
            case Token.Word { Value: var w } when w.Keyword == Keyword.RLIKE:
                return PrecValue(Precedence.Like);
            case Token.Word { Value: var w } when w.Keyword == Keyword.REGEXP:
                return PrecValue(Precedence.Like);
            case Token.Word { Value: var w } when w.Keyword == Keyword.MATCH:
                return PrecValue(Precedence.Like);
            case Token.Word { Value: var w } when w.Keyword == Keyword.GLOB:
                return PrecValue(Precedence.Like);
            case Token.Word { Value: var w } when w.Keyword == Keyword.SIMILAR:
                return PrecValue(Precedence.Like);
            case Token.Word { Value: var w } when w.Keyword == Keyword.MEMBER:
                return PrecValue(Precedence.Like);
            case Token.Word { Value: var w } when w.Keyword == Keyword.OPERATOR:
                return PrecValue(Precedence.Between);
            case Token.Word { Value: var w } when w.Keyword == Keyword.DIV:
                return PrecValue(Precedence.MulDivModOp);
            case Token.Period:
                return PrecValue(Precedence.Period);
            case Token.Assignment
                or Token.Eq
                or Token.Lt
                or Token.LtEq
                or Token.Neq
                or Token.Gt
                or Token.GtEq
                or Token.DoubleEq
                or Token.Tilde
                or Token.TildeAsterisk
                or Token.ExclamationMarkTilde
                or Token.ExclamationMarkTildeAsterisk
                or Token.DoubleTilde
                or Token.DoubleTildeAsterisk
                or Token.ExclamationMarkDoubleTilde
                or Token.ExclamationMarkDoubleTildeAsterisk
                or Token.Spaceship:
                return PrecValue(Precedence.Eq);
            case Token.Pipe
                or Token.QuestionMarkDash
                or Token.DoubleSharp
                or Token.Overlap
                or Token.AmpersandLeftAngleBracket
                or Token.AmpersandRightAngleBracket
                or Token.QuestionMarkDashVerticalBar
                or Token.AmpersandLeftAngleBracketVerticalBar
                or Token.VerticalBarAmpersandRightAngleBracket
                or Token.TwoWayArrow
                or Token.LeftAngleBracketCaret
                or Token.RightAngleBracketCaret
                or Token.QuestionMarkSharp
                or Token.QuestionMarkDoubleVerticalBar
                or Token.QuestionPipe
                or Token.TildeEqual
                or Token.AtSign
                or Token.ShiftLeftVerticalBar
                or Token.VerticalBarShiftRight:
                return PrecValue(Precedence.Pipe);
            case Token.Caret or Token.Sharp or Token.ShiftRight or Token.ShiftLeft:
                return PrecValue(Precedence.Caret);
            case Token.Ampersand:
                return PrecValue(Precedence.Ampersand);
            case Token.Plus or Token.Minus:
                return PrecValue(Precedence.PlusMinus);
            case Token.Mul or Token.Div or Token.DuckIntDiv or Token.Mod or Token.StringConcat:
                return PrecValue(Precedence.MulDivModOp);
            case Token.DoubleColon or Token.ExclamationMark or Token.LBracket or Token.CaretAt:
                return PrecValue(Precedence.DoubleColon);
            case Token.Colon:
                switch (parser.PeekNthTokenRef(1).Token)
                {
                    // When colon is followed by a string or a number, it's usually in MAP syntax.
                    case Token.SingleQuotedString or Token.Number:
                        return PrecUnknown;
                    // In other cases, it's used in semi-structured data traversal like in variant or JSON
                    // string columns. See `JsonAccess`.
                    default:
                        return PrecValue(Precedence.Colon);
                }
            case Token.Arrow
                or Token.LongArrow
                or Token.HashArrow
                or Token.HashLongArrow
                or Token.AtArrow
                or Token.ArrowAt
                or Token.HashMinus
                or Token.AtQuestion
                or Token.AtAt
                or Token.Question
                or Token.QuestionAnd
                or Token.CustomBinaryOperator:
                return PrecValue(Precedence.PgOther);
            default:
                return PrecUnknown;
        }
    }

    /// <summary>Dialect-specific statement parser override</summary>
    /// <remarks>
    /// This method is called to parse the next statement.
    /// If <c>None</c> is returned, falls back to the default behavior.
    /// </remarks>
    public virtual Statement? ParseStatement(Parser parser)
    {
        // return None to fall back to the default behavior
        return null;
    }

    /// <summary>Dialect-specific column option parser override</summary>
    /// <remarks>
    /// This method is called to parse the next column option.
    /// If <c>None</c> is returned, falls back to the default behavior.
    /// </remarks>
    // Rust: `Result<Option<Result<Option<ColumnOption>, ParserError>>, ParserError>`.
    // Os dois `Result` viram exceção; o `Option` externo vira Option<T> (casos 53 e 55):
    // `Option.None` = "use o comportamento padrão"; `Option.Some(null)` = "tratado, sem opção".
    public virtual Option<ColumnOption?> ParseColumnOption(Parser parser)
    {
        // return None to fall back to the default behavior
        return Option<ColumnOption?>.None;
    }

    /// <summary>
    /// Decide the lexical Precedence of operators.
    /// <para>
    /// Uses (APPROXIMATELY) <see href="https://www.postgresql.org/docs/7.0/operators.htm#AEN2026"/> as a reference
    /// </para>
    /// </summary>
    public virtual byte PrecValue(Precedence prec) => prec switch
    {
        Precedence.Period => 100,
        Precedence.DoubleColon => 50,
        Precedence.AtTz => 41,
        Precedence.MulDivModOp => 40,
        Precedence.PlusMinus => 30,
        Precedence.Xor => 24,
        Precedence.Ampersand => 23,
        Precedence.Caret => 22,
        Precedence.Pipe => 21,
        Precedence.Colon => 21,
        // "any other operator" -- `->`, `@>`, custom operators. PostgreSQL
        // places this row above `BETWEEN` / `LIKE` and below `+` / `-`
        // (`%left Op OPERATOR RIGHT_ARROW '|'` in gram.y), so it must bind
        // more tightly than `IS`, whose right operand would otherwise stop
        // short of it.
        Precedence.PgOther => 21,
        Precedence.Between => 20,
        Precedence.Eq => 20,
        Precedence.Like => 19,
        Precedence.Is => 17,
        Precedence.UnaryNot => 15,
        Precedence.And => 10,
        Precedence.Or => 5,
        _ => throw new InvalidOperationException($"Variante de Precedence não tratada: {prec}"),
    };

    /// <summary>
    /// Returns the precedence when the precedence is otherwise unknown
    /// </summary>
    public virtual byte PrecUnknown => 0;

    /// <summary>
    /// Returns true if this dialect requires the <c>TABLE</c> keyword after <c>DESCRIBE</c>
    /// <para>
    /// Defaults to false.
    /// </para>
    /// <para>
    /// If true, the following statement is valid: <c>DESCRIBE TABLE my_table</c>
    /// If false, the following statements are valid: <c>DESCRIBE my_table</c> and <c>DESCRIBE table</c>
    /// </para>
    /// </summary>
    public virtual bool DescribeRequiresTableKeyword => false;

    /// <summary>
    /// Returns true if this dialect allows the <c>EXTRACT</c> function to words other than <see cref="Keyword"/>.
    /// </summary>
    public virtual bool AllowExtractCustom => false;

    /// <summary>
    /// Returns true if this dialect allows the <c>EXTRACT</c> function to use single quotes in the part being extracted.
    /// </summary>
    public virtual bool AllowExtractSingleQuotes => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>EXTRACT</c> function
    /// with a comma separator instead of <c>FROM</c>.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT EXTRACT(YEAR, date_column) FROM table;
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/functions/extract">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsExtractCommaSyntax => false;

    /// <summary>
    /// Returns true if this dialect supports a subquery passed to a function
    /// as the only argument without enclosing parentheses.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT FLATTEN(SELECT * FROM tbl);
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/functions/flatten">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSubqueryAsFunctionArg => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>COMMENT</c> clause in
    /// <c>CREATE VIEW</c> statements using the <c>COMMENT = 'comment'</c> syntax.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// CREATE VIEW v COMMENT = 'my comment' AS SELECT 1;
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-view#optional-parameters">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsCreateViewCommentSyntax => false;

    /// <summary>
    /// Returns true if a data type can carry a collation, including inside a
    /// nested type such as <c>MAP&lt;STRING COLLATE UTF8_BINARY, STRING&gt;</c>.
    /// </summary>
    public virtual bool SupportsDataTypeCollation => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>ARRAY</c> type without
    /// specifying an element type.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// CREATE TABLE t (a ARRAY);
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/data-types-semistructured#array">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsArrayTypedefWithoutElementType => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>ARRAY(element_type)</c> syntax.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// CREATE TABLE t (a ARRAY(VARCHAR));
    /// </code>
    /// </summary>
    public virtual bool SupportsArrayTypedefWithParentheses => false;

    /// <summary>
    /// Returns true if this dialect supports <c>NOT NULL</c> on an element type in
    /// an <c>ARRAY(element_type)</c> definition.
    /// </summary>
    public virtual bool SupportsArrayElementNotNull => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>MAP(key_type, value_type)</c> syntax.
    /// </summary>
    public virtual bool SupportsMapTypedefWithParentheses => false;

    /// <summary>
    /// Returns true if this dialect supports <c>NOT NULL</c> on the value type in
    /// a <c>MAP(key_type, value_type)</c> definition.
    /// </summary>
    public virtual bool SupportsMapValueNotNull => false;

    /// <summary>
    /// Returns true if this dialect supports extra parentheses around
    /// lone table names or derived tables in the <c>FROM</c> clause.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM (mytable);
    /// SELECT * FROM ((SELECT 1));
    /// SELECT * FROM (mytable) AS alias;
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constructs/from">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsParensAroundTableFactor => false;

    /// <summary>
    /// Returns true if this dialect supports <c>VALUES</c> as a table factor
    /// without requiring parentheses around the entire clause.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM VALUES (1, 'a'), (2, 'b') AS t (col1, col2);
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constructs/values">Snowflake</see>
    /// <see href="https://docs.databricks.com/en/sql/language-manual/sql-ref-syntax-qry-select-values.html">Databricks</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsValuesAsTableFactor => false;

    /// <summary>
    /// Returns true if this dialect allows dollar placeholders
    /// e.g. <c>SELECT $var</c> (SQLite)
    /// </summary>
    public virtual bool SupportsDollarPlaceholder => false;

    /// <summary>
    /// Returns true if this dialect supports <c>$</c> as a prefix for money literals
    /// e.g. <c>SELECT $123.45</c> (SQL Server)
    /// </summary>
    public virtual bool SupportsDollarAsMoneyPrefix => false;

    /// <summary>
    /// Does the dialect support with clause in create index statement?
    /// e.g. <c>CREATE INDEX idx ON t WITH (key = value, key2)</c>
    /// </summary>
    public virtual bool SupportsCreateIndexWithClause => false;

    /// <summary>
    /// Whether <c>INTERVAL</c> expressions require units (called "qualifiers" in the ANSI SQL spec) to be specified,
    /// e.g. <c>INTERVAL 1 DAY</c> vs <c>INTERVAL 1</c>.
    /// <para>
    /// Expressions within intervals (e.g. <c>INTERVAL '1' + '1' DAY</c>) are only allowed when units are required.
    /// </para>
    /// <para>
    /// See <see href="https://github.com/sqlparser-rs/sqlparser-rs/pull/1398"/> for more information.
    /// </para>
    /// <para>
    /// When <c>true</c>:
    /// * <c>INTERVAL '1' DAY</c> is VALID
    /// * <c>INTERVAL 1 + 1 DAY</c> is VALID
    /// * <c>INTERVAL '1' + '1' DAY</c> is VALID
    /// * <c>INTERVAL '1'</c> is INVALID
    /// </para>
    /// <para>
    /// When <c>false</c>:
    /// * <c>INTERVAL '1'</c> is VALID
    /// * <c>INTERVAL '1' DAY</c> is VALID — unit is not required, but still allowed
    /// * <c>INTERVAL 1 + 1 DAY</c> is INVALID
    /// </para>
    /// </summary>
    public virtual bool RequireIntervalQualifier => false;

    /// <summary>
    /// Returns true if the dialect supports <c>EXPLAIN</c> statements with utility options
    /// e.g. <c>EXPLAIN (ANALYZE TRUE, BUFFERS TRUE) SELECT * FROM tbl;</c>
    /// </summary>
    public virtual bool SupportsExplainWithUtilityOptions => false;

    /// <summary>
    /// Returns true if the dialect supports <c>ASC</c> and <c>DESC</c> in column definitions
    /// e.g. <c>CREATE TABLE t (a INT ASC, b INT DESC);</c>
    /// </summary>
    public virtual bool SupportsAscDescInColumnDefinition => false;

    /// <summary>
    /// Returns true if the dialect allows string literals as column names in <c>CREATE TABLE</c>.
    /// SQLite's grammar rule <c>nm ::= id | STRING | JOIN_KW</c> permits this.
    /// </summary>
    public virtual bool SupportsStringLiteralColumnNames => false;

    /// <summary>
    /// Returns true if the dialect supports <c>a!</c> expressions
    /// </summary>
    public virtual bool SupportsFactorialOperator => false;

    /// <summary>
    /// Returns true if the dialect supports <c>&lt;&lt;</c> and <c>&gt;&gt;</c> shift operators.
    /// </summary>
    public virtual bool SupportsBitwiseShiftOperators => false;

    /// <summary>
    /// Returns true if the dialect supports nested comments
    /// e.g. <c>/* /* nested */ */</c>
    /// </summary>
    public virtual bool SupportsNestedComments => false;

    /// <summary>
    /// Returns true if the dialect supports optimizer hints in multiline comments
    /// e.g. <c>/*!50110 KEY_BLOCK_SIZE = 1024*/</c>
    /// </summary>
    public virtual bool SupportsMultilineCommentHints => false;

    /// <summary>
    /// Returns true if this dialect supports treating the equals operator <c>=</c> within a <c>SelectItem</c>
    /// as an alias assignment operator, rather than a boolean expression.
    /// For example: the following statements are equivalent for such a dialect:
    /// <code>
    ///  SELECT col_alias = col FROM tbl;
    ///  SELECT col_alias AS col FROM tbl;
    /// </code>
    /// </summary>
    public virtual bool SupportsEqAliasAssignment => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>TRY_CONVERT</c> function
    /// </summary>
    public virtual bool SupportsTryConvert => false;

    /// <summary>
    /// Returns true if the dialect supports <c>!a</c> syntax for boolean <c>NOT</c> expressions.
    /// </summary>
    public virtual bool SupportsBangNotOperator => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>LISTEN</c>, <c>UNLISTEN</c> and <c>NOTIFY</c> statements
    /// </summary>
    public virtual bool SupportsListenNotify => false;

    /// <summary>
    /// Returns true if the dialect supports <c>EXCLUDE</c> table constraints, e.g.
    /// <c>EXCLUDE USING gist (c WITH &amp;&amp;)</c> in <c>CREATE TABLE</c>/<c>ALTER TABLE</c>.
    /// See <see href="https://www.postgresql.org/docs/current/sql-createtable.html#SQL-CREATETABLE-EXCLUDE"/>.
    /// </summary>
    public virtual bool SupportsExcludeConstraint => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>LOAD DATA</c> statement
    /// </summary>
    public virtual bool SupportsLoadData => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>LOAD extension</c> statement
    /// </summary>
    public virtual bool SupportsLoadExtension => false;

    /// <summary>
    /// Returns true if this dialect expects the <c>TOP</c> option
    /// before the <c>ALL</c>/<c>DISTINCT</c> options in a <c>SELECT</c> statement.
    /// </summary>
    public virtual bool SupportsTopBeforeDistinct => false;

    /// <summary>
    /// Returns true if the dialect supports boolean literals (<c>true</c> and <c>false</c>).
    /// For example, in MSSQL these are treated as identifiers rather than boolean literals.
    /// </summary>
    public virtual bool SupportsBooleanLiterals => true;

    /// <summary>
    /// Returns true if this dialect supports the <c>LIKE 'pattern'</c> option in
    /// a <c>SHOW</c> statement before the <c>IN</c> option
    /// </summary>
    public virtual bool SupportsShowLikeBeforeIn => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>COMMENT</c> statement
    /// </summary>
    public virtual bool SupportsCommentOn => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>CREATE TABLE SELECT</c> statement
    /// </summary>
    public virtual bool SupportsCreateTableSelect => false;

    /// <summary>
    /// Returns true if this dialect supports <c>PROC</c> as an abbreviation for
    /// <c>PROCEDURE</c> in a <c>CREATE</c> statement.
    /// </summary>
    public virtual bool SupportsCreateProcSyntax => false;

    /// <summary>
    /// Returns true if the dialect accepts a comma-separated list of table-level
    /// options placed between the table name and the column-list parenthesis, e.g.
    /// <code>
    /// CREATE TABLE foo, NO FALLBACK, NO BEFORE JOURNAL (col INTEGER)
    /// </code>
    /// </summary>
    public virtual bool SupportsLeadingCommaBeforeTableOptions => false;

    /// <summary>
    /// Returns true if the dialect supports PartiQL for querying semi-structured data
    /// <see href="https://partiql.org/index.html"/>
    /// </summary>
    public virtual bool SupportsPartiql => false;

    /// <summary>
    /// Returns true if the dialect supports object-unpivot table factors in the FROM clause.
    /// <para>
    /// Syntax:
    /// </para>
    /// <code>
    /// SELECT * FROM T UNPIVOT expression AS value_alias [AT attribute_alias]
    /// </code>
    /// </summary>
    public virtual bool SupportsUnpivotExpr => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>CONSTRAINT</c> keyword without a name
    /// in table constraint definitions.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// CREATE TABLE t (a INT, CONSTRAINT CHECK (a &gt; 0))
    /// </code>
    /// <para>
    /// This is a MySQL extension; the SQL standard requires a name after <c>CONSTRAINT</c>.
    /// When the name is omitted, the output normalizes to just the constraint type
    /// without the <c>CONSTRAINT</c> keyword (e.g., <c>CHECK (a &gt; 0)</c>).
    /// </para>
    /// <para>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table.html"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsConstraintKeywordWithoutName => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>KEY</c> keyword as part of
    /// column-level constraints in a <c>CREATE TABLE</c> statement.
    /// <para>
    /// When enabled, the parser accepts these MySQL-specific column options:
    /// - <c>UNIQUE [KEY]</c> — optional <c>KEY</c> after <c>UNIQUE</c>
    /// - <c>[PRIMARY] KEY</c> — standalone <c>KEY</c> as shorthand for <c>PRIMARY KEY</c>
    /// </para>
    /// <para>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table.html"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsKeyColumnOption => false;

    /// <summary>
    /// Returns true if the specified keyword is reserved and cannot be
    /// used as an identifier without special handling like quoting.
    /// </summary>
    public virtual bool IsReservedForIdentifier(Keyword kw) => Keywords.ReservedForIdentifier.Contains(kw);

    /// <summary>
    /// Returns reserved keywords that may prefix a select item expression
    /// e.g. <c>SELECT CONNECT_BY_ROOT name FROM Tbl2</c> (Snowflake)
    /// </summary>
    public virtual IReadOnlyList<Keyword> GetReservedKeywordsForSelectItemOperator => System.Array.Empty<Keyword>();

    /// <summary>Returns grantee types that should be treated as identifiers</summary>
    public virtual IReadOnlyList<GranteesType> GetReservedGranteesTypes => System.Array.Empty<GranteesType>();

    /// <summary>
    /// Returns true if this dialect supports the <c>TABLESAMPLE</c> option
    /// before the table alias option. For example:
    /// <para>
    /// Table sample before alias: <c>SELECT * FROM tbl AS t TABLESAMPLE (10)</c>
    /// Table sample after alias: <c>SELECT * FROM tbl TABLESAMPLE (10) AS t</c>
    /// </para>
    /// <para>
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#_7_6_table_reference"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsTableSampleBeforeAlias => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>INSERT INTO ... SET col1 = 1, ...</c> syntax.
    /// <para>
    /// MySQL: <see href="https://dev.mysql.com/doc/refman/8.4/en/insert.html"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsInsertSet => false;

    /// <summary>
    /// Does the dialect support table function in insertion?
    /// </summary>
    public virtual bool SupportsInsertTableFunction => false;

    /// <summary>
    /// Does the dialect support table queries in insertion?
    /// <para>
    /// e.g. <c>SELECT INTO (&lt;query&gt;) ...</c>
    /// </para>
    /// </summary>
    public virtual bool SupportsInsertTableQuery => false;

    /// <summary>
    /// Does the dialect support insert formats, e.g. <c>INSERT INTO ... FORMAT &lt;format&gt;</c>
    /// </summary>
    public virtual bool SupportsInsertFormat => false;

    /// <summary>
    /// Returns true if this dialect supports <c>INSERT INTO t [[AS] alias] ...</c>.
    /// </summary>
    public virtual bool SupportsInsertTableAlias => false;

    /// <summary>
    /// Returns true if this dialect supports <c>SET</c> statements without an explicit
    /// assignment operator such as <c>=</c>. For example: <c>SET SHOWPLAN_XML ON</c>.
    /// </summary>
    public virtual bool SupportsSetStmtWithoutOperator => false;

    /// <summary>Returns true if the specified keyword should be parsed as a column identifier.</summary>
    /// <remarks>See <see cref="Keywords.ReservedForColumnAlias"/></remarks>
    public virtual bool IsColumnAlias(Keyword kw, Parser parser) => !Keywords.ReservedForColumnAlias.Contains(kw);

    /// <summary>
    /// Returns true if the specified keyword should be parsed as a select item alias.
    /// When explicit is true, the keyword is preceded by an <c>AS</c> word. Parser is provided
    /// to enable looking ahead if needed.
    /// </summary>
    public virtual bool IsSelectItemAlias(bool @explicit, Keyword kw, Parser parser) => @explicit || IsColumnAlias(kw, parser);

    /// <summary>Returns true if the specified keyword should be parsed as a table factor identifier.</summary>
    /// <remarks>See <see cref="Keywords.ReservedForTableFactor"/></remarks>
    public virtual bool IsTableFactor(Keyword kw, Parser parser) => !Keywords.ReservedForTableFactor.Contains(kw);

    /// <summary>Returns true if the specified keyword should be parsed as a table factor alias.</summary>
    /// <remarks>See <see cref="Keywords.ReservedForTableAlias"/></remarks>
    public virtual bool IsTableAlias(Keyword kw, Parser parser) => !Keywords.ReservedForTableAlias.Contains(kw);

    /// <summary>
    /// Returns true if the specified keyword should be parsed as a table factor alias.
    /// When explicit is true, the keyword is preceded by an <c>AS</c> word. Parser is provided
    /// to enable looking ahead if needed.
    /// </summary>
    public virtual bool IsTableFactorAlias(bool @explicit, Keyword kw, Parser parser) => @explicit || IsTableAlias(kw, parser);

    /// <summary>
    /// Returns true if this dialect supports querying historical table data
    /// by specifying which version of the data to query.
    /// </summary>
    public virtual bool SupportsTableVersioning => false;

    /// <summary>
    /// Returns true if this dialect supports the E'...' syntax for string literals
    /// <para>
    /// Postgres: <see href="https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-STRINGS-ESCAPE"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsStringEscapeConstant => false;

    /// <summary>
    /// Returns true if the dialect supports the table hints in the <c>FROM</c> clause.
    /// </summary>
    public virtual bool SupportsTableHints => false;

    /// <summary>
    /// Returns true if this dialect requires a whitespace character after <c>--</c> to start a single line comment.
    /// <para>
    /// MySQL: <see href="https://dev.mysql.com/doc/refman/8.4/en/ansi-diff-comments.html"/>
    /// e.g. UPDATE account SET balance=balance--1
    /// </para>
    /// </summary>
    //       WHERE account_id=5752             ^^^ will be interpreted as two minus signs instead of a comment
    public virtual bool RequiresSingleLineCommentWhitespace => false;

    /// <summary>
    /// Returns true if the dialect supports array type definition with brackets with
    /// an optional size. For example:
    /// <c>CREATE TABLE my_table (arr1 INT[], arr2 INT[3])</c>
    /// <c>SELECT x::INT[]</c>
    /// </summary>
    public virtual bool SupportsArrayTypedefWithBrackets => false;

    /// <summary>
    /// Returns true if the dialect supports geometric types.
    /// <para>
    /// Postgres: <see href="https://www.postgresql.org/docs/9.5/functions-geometry.html"/>
    /// e.g. @@ circle '((0,0),10)'
    /// </para>
    /// </summary>
    public virtual bool SupportsGeometricTypes => false;

    /// <summary>
    /// Returns true if the dialect supports <c>ORDER BY ALL</c>.
    /// <c>ALL</c> which means all columns of the SELECT clause.
    /// <para>
    /// For example: ``<c>SELECT * FROM addresses ORDER BY ALL;</c>``.
    /// </para>
    /// </summary>
    public virtual bool SupportsOrderByAll => false;

    /// <summary>
    /// Returns true if the dialect supports PostgreSQL-style ordering operators:
    /// <c>ORDER BY expr USING &lt;operator&gt;</c>.
    /// <para>
    /// For example: <c>SELECT * FROM t ORDER BY a USING &lt;</c>.
    /// </para>
    /// </summary>
    public virtual bool SupportsOrderByUsingOperator => false;

    /// <summary>
    /// Returns true if the dialect supports <c>SET NAMES &lt;charset_name&gt; [COLLATE &lt;collation_name&gt;]</c>.
    /// <para>
    /// - <see href="https://dev.mysql.com/doc/refman/8.4/en/set-names.html">MySQL</see>
    /// - <see href="https://www.postgresql.org/docs/17/sql-set.html">PostgreSQL</see>
    /// </para>
    /// <para>
    /// Note: Postgres doesn't support the <c>COLLATE</c> clause, but we permissively parse it anyway.
    /// </para>
    /// </summary>
    public virtual bool SupportsSetNames => false;

    /// <summary>
    /// Returns true if the dialect supports space-separated column options
    /// in a <c>CREATE TABLE</c> statement. For example:
    /// <code>
    /// CREATE TABLE tbl (
    ///     col INT NOT NULL DEFAULT 0
    /// );
    /// </code>
    /// </summary>
    public virtual bool SupportsSpaceSeparatedColumnOptions => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>USING</c> clause in an <c>ALTER COLUMN</c> statement.
    /// Example:
    /// <code>
    ///  ALTER TABLE tbl ALTER COLUMN col SET DATA TYPE &lt;type&gt; USING &lt;exp&gt;`
    /// </code>
    /// </summary>
    public virtual bool SupportsAlterColumnTypeUsing => false;

    /// <summary>
    /// Returns true if the dialect supports <c>ALTER TABLE tbl DROP COLUMN c1, ..., cn</c>
    /// </summary>
    public virtual bool SupportsCommaSeparatedDropColumnList => false;

    /// <summary>
    /// Returns true if the dialect supports a <c>FIRST</c> or <c>AFTER col</c> column
    /// position in <c>ALTER TABLE ... ADD | CHANGE | MODIFY COLUMN</c>.
    /// Example:
    /// <code>
    ///  ALTER TABLE tbl ADD COLUMN c INT AFTER b
    /// </code>
    /// </summary>
    public virtual bool SupportsAlterColumnPosition => false;

    /// <summary>
    /// Returns true if the dialect considers the specified ident as a function
    /// that returns an identifier. Typically used to generate identifiers
    /// programmatically.
    /// </summary>
    /// <remarks>- <see href="https://docs.snowflake.com/en/sql-reference/identifier-literal">Snowflake</see></remarks>
    public virtual bool IsIdentifierGeneratingFunctionName(Ident ident, IReadOnlyList<ObjectNamePart> nameParts) => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>x NOTNULL</c>
    /// operator expression.
    /// </summary>
    public virtual bool SupportsNotnullOperator => false;

    /// <summary>
    /// Returns true if this dialect allows an optional <c>SIGNED</c> suffix after integer data types.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// CREATE TABLE t (i INT(20) SIGNED);
    /// </code>
    /// <para>
    /// Note that this is canonicalized to <c>INT(20)</c>.
    /// </para>
    /// </summary>
    public virtual bool SupportsDataTypeSignedSuffix => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>INTERVAL</c> data type with [Postgres]-style options.
    /// <para>
    /// Examples:
    /// </para>
    /// <code>
    /// CREATE TABLE t (i INTERVAL YEAR TO MONTH);
    /// SELECT '1 second'::INTERVAL HOUR TO SECOND(3);
    /// </code>
    /// <para>
    /// See <c>crate::ast::DataType::Interval</c> and <c>crate::ast::IntervalFields</c>.
    /// </para>
    /// <para>
    /// [Postgres]: https://www.postgresql.org/docs/17/datatype-datetime.html
    /// </para>
    /// </summary>
    public virtual bool SupportsIntervalOptions => false;

    /// <summary>
    /// Returns true if the dialect supports specifying which table to copy
    /// the schema from inside parenthesis.
    /// <para>
    /// Not parenthesized:
    /// '''sql
    /// CREATE TABLE new LIKE old ...
    /// '''
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table#label-create-table-like">Snowflake</see>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_table_like">BigQuery</see>
    /// </para>
    /// <para>
    /// Parenthesized:
    /// '''sql
    /// CREATE TABLE new (LIKE old ...)
    /// '''
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CREATE_TABLE_NEW.html">Redshift</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsCreateTableLikeParenthesized => false;

    /// <summary>
    /// Returns true if the dialect supports <c>SEMANTIC_VIEW()</c> table functions.
    /// <code>
    /// SELECT * FROM SEMANTIC_VIEW(
    ///     model_name
    ///     DIMENSIONS customer.name, customer.region
    ///     METRICS orders.revenue, orders.count
    ///     WHERE customer.active = true
    /// )
    /// </code>
    /// </summary>
    public virtual bool SupportsSemanticViewTableFactor => false;

    /// <summary>
    /// Returns true if this dialect supports Snowflake-style stages in table factors (e.g. <c>@stage</c>).
    /// <para>
    /// <see href="https://docs.snowflake.com/en/user-guide/querying-stage">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsStages => false;

    /// <summary>
    /// Support quote delimited string literals, e.g. <c>Q'{...}'</c>
    /// <para>
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Literals.html#GUID-1824CBAA-6E16-4921-B2A6-112FB02248DA">Oracle</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsQuoteDelimitedString => false;

    /// <summary>
    /// Returns <c>true</c> if the dialect supports query optimizer hints in the
    /// format of single and multi line comments immediately following a
    /// <c>SELECT</c>, <c>INSERT</c>, <c>REPLACE</c>, <c>DELETE</c>, or <c>MERGE</c> keyword.
    /// <para>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/optimizer-hints.html">MySQL</see>
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Comments.html#SQLRF-GUID-D316D545-89E2-4D54-977F-FC97815CD62E">Oracle</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsCommentOptimizerHint => false;

    /// <summary>
    /// Returns true if the dialect considers the <c>&amp;&amp;</c> operator as a boolean AND operator.
    /// </summary>
    public virtual bool SupportsDoubleAmpersandOperator => false;

    /// <summary>
    /// Returns true if the dialect supports casting an expression to a binary type
    /// using the <c>BINARY &lt;expr&gt;</c> syntax.
    /// </summary>
    public virtual bool SupportsBinaryKwAsCast => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>REPLACE</c> option in a
    /// <c>SELECT *</c> wildcard expression.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * REPLACE (col1 AS col1_alias) FROM table;
    /// </code>
    /// <para>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#select_replace">BigQuery</see>
    /// <see href="https://clickhouse.com/docs/sql-reference/statements/select#replace">ClickHouse</see>
    /// <see href="https://duckdb.org/docs/sql/query_syntax/select#replace-clause">DuckDB</see>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSelectWildcardReplace => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>ILIKE</c> option in a
    /// <c>SELECT *</c> wildcard expression.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * ILIKE '%pattern%' FROM table;
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSelectWildcardIlike => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>RENAME</c> option in a
    /// <c>SELECT *</c> wildcard expression.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * RENAME col1 AS col1_alias FROM table;
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters">Snowflake</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSelectWildcardRename => false;

    /// <summary>
    /// Returns true if this dialect supports aliasing a wildcard select item.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT t.* alias FROM t
    /// SELECT t.* AS alias FROM t
    /// </code>
    /// </summary>
    public virtual bool SupportsSelectWildcardWithAlias => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>OPTIMIZE TABLE</c> statement.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// OPTIMIZE TABLE table_name;
    /// </code>
    /// <para>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize">ClickHouse</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsOptimizeTable => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>INSTALL</c> statement.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// INSTALL extension_name;
    /// </code>
    /// <para>
    /// <see href="https://duckdb.org/docs/extensions/overview">DuckDB</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsInstall => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>DETACH</c> statement.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// DETACH DATABASE db_name;
    /// </code>
    /// <para>
    /// <see href="https://duckdb.org/docs/sql/statements/attach#detach-syntax">DuckDB</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsDetach => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>PREWHERE</c> clause
    /// in <c>SELECT</c> statements.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM table PREWHERE col &gt; 0 WHERE col &lt; 100;
    /// </code>
    /// <para>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/prewhere">ClickHouse</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsPrewhere => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>WITH FILL</c> clause
    /// in <c>ORDER BY</c> expressions.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM table ORDER BY col WITH FILL FROM 1 TO 10 STEP 1;
    /// </code>
    /// <para>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/order-by#order-by-expr-with-fill-modifier">ClickHouse</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsWithFill => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>LIMIT BY</c> clause.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM table LIMIT 10 BY col;
    /// </code>
    /// <para>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/limit-by">ClickHouse</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsLimitBy => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>INTERPOLATE</c> clause
    /// in <c>ORDER BY</c> expressions.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM table ORDER BY col WITH FILL INTERPOLATE (col2 AS col2 + 1);
    /// </code>
    /// <para>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/order-by#order-by-expr-with-fill-modifier">ClickHouse</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsInterpolate => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>SETTINGS</c> clause.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM table SETTINGS max_threads = 4;
    /// </code>
    /// <para>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select#settings-in-select-query">ClickHouse</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSettings => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>PARTITION</c> clause on a table factor,
    /// restricting a query to an explicit list of partitions.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM employees PARTITION (p0, p1)
    /// </code>
    /// <para>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/partitioning-selection.html">MySQL</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsTablePartitions => false;

    /// <summary>
    /// Returns true if this dialect supports the <c>FORMAT</c> clause in <c>SELECT</c> statements.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT * FROM table FORMAT JSON;
    /// </code>
    /// <para>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/format">ClickHouse</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSelectFormat => false;

    /// <summary>
    /// Returns true if the dialect supports the two-argument comma-separated
    /// form of the <c>TRIM</c> function: <c>TRIM(expr, characters)</c>.
    /// </summary>
    public virtual bool SupportsCommaSeparatedTrim => false;

    /// <summary>
    /// Returns true if the dialect supports the <c>AS</c> keyword being
    /// optional in a CTE definition. For example:
    /// <code>
    /// WITH cte_name (SELECT ...)
    /// </code>
    /// <para>
    /// <see href="https://docs.databricks.com/aws/en/sql/language-manual/sql-ref-syntax-qry-select-cte">Databricks</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsCteWithoutAs => false;

    /// <summary>
    /// Returns true if the dialect supports parenthesized multi-column
    /// aliases in SELECT items. For example:
    /// <code>
    /// SELECT stack(2, 'a', 'b') AS (col1, col2)
    /// </code>
    /// <para>
    /// <see href="https://spark.apache.org/docs/latest/sql-ref-syntax-qry-select.html">Spark SQL</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsSelectItemMultiColumnAlias => false;

    /// <summary>
    /// Returns true if the dialect supports XML-related expressions
    /// such as <c>xml '&lt;foo/&gt;'</c> typed strings, XML functions like
    /// <c>XMLCONCAT</c>, <c>XMLELEMENT</c>, etc.
    /// <para>
    /// When this returns false, <c>xml</c> is treated as a regular identifier.
    /// </para>
    /// <para>
    /// <see href="https://www.postgresql.org/docs/current/functions-xml.html">PostgreSQL</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsXmlExpressions => false;

    /// <summary>
    /// Returns true if the dialect supports aliased function arguments,
    /// e.g. <c>XMLFOREST(a AS x)</c> in PostgreSQL.
    /// </summary>
    public virtual bool SupportsAliasedFunctionArgs => false;

    /// <summary>
    /// Returns true if the dialect supports <c>USING &lt;format&gt;</c> in <c>CREATE TABLE</c>.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// CREATE TABLE t (i INT) USING PARQUET
    /// </code>
    /// <para>
    /// <see href="https://spark.apache.org/docs/latest/sql-ref-syntax-ddl-create-table-datasource.html">Spark SQL</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsCreateTableUsing => false;

    /// <summary>
    /// Returns true if the dialect treats <c>LONG</c> as an alias for <c>BIGINT</c>.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// CREATE TABLE t (id LONG)
    /// </code>
    /// <para>
    /// <see href="https://spark.apache.org/docs/latest/sql-ref-datatypes.html">Spark SQL</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsLongTypeAsBigint => false;

    /// <summary>
    /// Returns true if the dialect supports <c>MAP&lt;K, V&gt;</c> angle-bracket syntax for the MAP data type.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// CREATE TABLE t (m MAP&lt;STRING, INT&gt;)
    /// </code>
    /// <para>
    /// <see href="https://spark.apache.org/docs/latest/sql-ref-datatypes.html">Spark SQL</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsMapLiteralWithAngleBrackets => false;

    /// <summary>
    /// Returns true if the dialect accepts <c>==</c> as an alternative to <c>=</c> in <c>UPDATE SET</c> assignments.
    /// <para>
    /// See <see href="https://www.sqlite.org/lang_update.html"/>
    /// </para>
    /// </summary>
    public virtual bool SupportsDoubleEqAssignment => false;

    /// <summary>
    /// Returns true if the dialect supports <c>CAST(expr AS)</c> with an empty type name.
    /// <para>
    /// Example:
    /// </para>
    /// <code>
    /// SELECT CAST(a AS)
    /// </code>
    /// <para>
    /// <see href="https://www.sqlite.org/lang_expr.html">SQLite</see>
    /// </para>
    /// </summary>
    public virtual bool SupportsCastEmptyDataTypeToUnspecified => false;

    // Rust: `pub fn dialect_from_str` é função livre do módulo `dialect`; vira membro estático de
    // Dialect (convencoes.md §13, caso 5). Fica no fim, como no arquivo Rust.

    /// <summary>
    /// Returns the built in <see cref="Dialect"/> corresponding to <c>dialect_name</c>.
    /// <para>
    /// See <see cref="Dialect"/> documentation for an example.
    /// </para>
    /// </summary>
    public static Dialect? DialectFromStr(string dialectName)
    {
        // Rust: `dialect_name.to_lowercase()` → RustString.ToLowercase (convencoes.md §10; DIV-001, resolvida).
        switch (RustString.ToLowercase(dialectName))
        {
            case "generic":
                return new GenericDialect();
            case "mysql":
                return new MySqlDialect();
            case "postgresql":
            case "postgres":
                return new PostgreSqlDialect();
            case "hive":
                return new HiveDialect();
            case "sqlite":
                return new SQLiteDialect();
            case "snowflake":
                return new SnowflakeDialect();
            case "redshift":
                return new RedshiftSqlDialect();
            case "mssql":
                return new MsSqlDialect();
            case "clickhouse":
                return new ClickHouseDialect();
            case "bigquery":
                return new BigQueryDialect();
            case "ansi":
                return new AnsiDialect();
            case "duckdb":
                return new DuckDbDialect();
            case "databricks":
                return new DatabricksDialect();
            case "spark":
            case "sparksql":
                return new SparkSqlDialect();
            case "oracle":
                return new OracleDialect();
            case "teradata":
                return new TeradataDialect();
            default:
                return null;
        }
    }
}
