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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/postgresql.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.postgresql.org/">PostgreSQL</see>
/// </summary>
public class PostgreSqlDialect : Dialect
{
    // Constantes de módulo no Rust (antes e depois do struct). RESERVED_EXCLUSIONS_FOR_TABLE_ALIAS,
    // BRACKET_PREC e COLLATE_PREC são usadas só por is_table_alias/get_next_precedence (Fase 2).

    /// <summary>
    /// Keywords in <c>keywords::RESERVED_FOR_TABLE_ALIAS</c> because of other dialects, yet are safe for aliasing in PostgreSQL.
    /// See <see href="https://www.postgresql.org/docs/current/sql-keywords-appendix.html"/>.
    /// </summary>
    private static readonly Keyword[] ReservedExclusionsForTableAlias =
    {
        Keyword.CLUSTER,
        Keyword.DISTRIBUTE,
        Keyword.EXPLAIN,
        Keyword.MINUS,
        Keyword.SAMPLE,
        Keyword.SORT,
        Keyword.START,
        Keyword.TOP,
        Keyword.VIEW,
    };

    private const byte PeriodPrec = 200;
    private const byte DoubleColonPrec = 140;
    private const byte BracketPrec = 130;
    private const byte CollatePrec = 120;
    private const byte AtTzPrec = 110;
    private const byte CaretPrec = 100;
    private const byte MulDivModOpPrec = 90;
    private const byte PlusMinusPrec = 80;
    // there's no XOR operator in PostgreSQL, but support it here to avoid breaking tests
    private const byte XorPrec = 75;
    private const byte PgOtherPrec = 70;
    private const byte BetweenLikePrec = 60;
    private const byte EqPrec = 50;
    private const byte IsPrec = 40;
    private const byte NotPrec = 30;
    private const byte AndPrec = 20;
    private const byte OrPrec = 10;

    public override char? IdentifierQuoteStyle(string identifier) => '"';

    public override bool IsDelimitedIdentifierStart(char ch) => ch == '"'; // Postgres does not support backticks to quote identifiers

    public override bool IsIdentifierStart(char ch) =>
        char.IsLetter(ch) || ch == '_' ||
        // PostgreSQL implements Unicode characters in identifiers.
        ch > '\u007f';

    public override bool IsIdentifierPart(char ch) =>
        char.IsLetter(ch) || (ch >= '0' && ch <= '9') || ch == '$' || ch == '_' ||
        // PostgreSQL implements Unicode characters in identifiers.
        ch > '\u007f';

    public override bool SupportsUnicodeStringLiteral => true;

    public override bool IsReservedForIdentifier(Keyword kw)
    {
        if (kw is Keyword.INTERVAL)
        {
            return false;
        }
        else
        {
            return Keywords.ReservedForIdentifier.Contains(kw);
        }
    }

    // PENDENTE (Fase 2, depende de Parser/AST): is_table_alias (postgresql.rs:108).

    /// <summary>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createoperator.html"/>
    /// </summary>
    public override bool IsCustomOperatorPart(char ch) =>
        ch is '+' or '-'
            or '*'
            or '/'
            or '<'
            or '>'
            or '='
            or '~'
            or '!'
            or '@'
            or '#'
            or '%'
            or '^'
            or '&'
            or '|'
            or '`'
            or '?';

    // PENDENTE (Fase 2, depende de Parser/AST): get_next_precedence (postgresql.rs:136).

    public override bool SupportsFilterDuringAggregation => true;

    public override bool SupportsGroupByExpr => true;

    public override bool SupportsAlterUserAsAlterRole => true;

    public override byte PrecValue(Precedence prec) => prec switch
    {
        Precedence.Period => PeriodPrec,
        Precedence.DoubleColon => DoubleColonPrec,
        Precedence.AtTz => AtTzPrec,
        Precedence.MulDivModOp => MulDivModOpPrec,
        Precedence.PlusMinus => PlusMinusPrec,
        Precedence.Xor => XorPrec,
        Precedence.Ampersand => PgOtherPrec,
        Precedence.Caret => CaretPrec,
        Precedence.Pipe => PgOtherPrec,
        Precedence.Colon => PgOtherPrec,
        Precedence.Between => BetweenLikePrec,
        Precedence.Eq => EqPrec,
        Precedence.Like => BetweenLikePrec,
        Precedence.Is => IsPrec,
        Precedence.PgOther => PgOtherPrec,
        Precedence.UnaryNot => NotPrec,
        Precedence.And => AndPrec,
        Precedence.Or => OrPrec,
        _ => throw new InvalidOperationException($"Variante de Precedence não tratada: {prec}"),
    };

    public override bool AllowExtractCustom => true;

    public override bool AllowExtractSingleQuotes => true;

    public override bool SupportsCreateIndexWithClause => true;

    /// <summary>
    /// see <see href="https://www.postgresql.org/docs/current/sql-explain.html"/>
    /// </summary>
    public override bool SupportsExplainWithUtilityOptions => true;

    /// <summary>
    /// see <see href="https://www.postgresql.org/docs/current/sql-listen.html"/>
    /// see <see href="https://www.postgresql.org/docs/current/sql-unlisten.html"/>
    /// see <see href="https://www.postgresql.org/docs/current/sql-notify.html"/>
    /// </summary>
    public override bool SupportsListenNotify => true;

    /// <summary>
    /// see <see href="https://www.postgresql.org/docs/current/sql-createtable.html#SQL-CREATETABLE-EXCLUDE"/>
    /// </summary>
    public override bool SupportsExcludeConstraint => true;

    /// <summary>
    /// see <see href="https://www.postgresql.org/docs/13/functions-math.html"/>
    /// </summary>
    public override bool SupportsFactorialOperator => true;

    public override bool SupportsBitwiseShiftOperators => true;

    /// <summary>
    /// see <see href="https://www.postgresql.org/docs/current/sql-comment.html"/>
    /// </summary>
    public override bool SupportsCommentOn => true;

    /// <summary>
    /// See <see href="https://www.postgresql.org/docs/current/sql-load.html"/>
    /// </summary>
    public override bool SupportsLoadExtension => true;

    /// <summary>
    /// See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>
    /// <para>
    /// Required to support the colon in:
    /// </para>
    /// <code>
    /// SELECT json_object('a': 'b')
    /// </code>
    /// </summary>
    public override bool SupportsNamedFnArgsWithColonOperator => true;

    /// <summary>
    /// See <see href="https://www.postgresql.org/docs/current/functions-json.html"/>
    /// <para>
    /// Required to support the label in:
    /// </para>
    /// <code>
    /// SELECT json_object('label': 'value')
    /// </code>
    /// </summary>
    public override bool SupportsNamedFnArgsWithExprName => true;

    /// <summary>
    /// Return true if the dialect supports empty projections in SELECT statements
    /// <para>
    /// Example
    /// </para>
    /// <code>
    /// SELECT from table_name
    /// </code>
    /// </summary>
    public override bool SupportsEmptyProjections => true;

    public override bool SupportsNestedComments => true;

    public override bool SupportsStringEscapeConstant => true;

    public override bool SupportsNumericLiteralUnderscores => true;

    /// <summary>
    /// See: <see href="https://www.postgresql.org/docs/current/arrays.html#ARRAYS-DECLARATION"/>
    /// </summary>
    public override bool SupportsArrayTypedefWithBrackets => true;

    public override bool SupportsGeometricTypes => true;

    public override bool SupportsOrderByUsingOperator => true;

    public override bool SupportsSetNames => true;

    public override bool SupportsAlterColumnTypeUsing => true;

    /// <summary>
    /// PostgreSQL supports right-deep join chains: <c>t0 JOIN t1 JOIN t2 ON c1 ON c2</c>
    /// See: <see href="https://www.postgresql.org/docs/current/queries-table-expressions.html#QUERIES-JOIN"/>
    /// </summary>
    public override bool SupportsLeftAssociativeJoinsWithoutParens => false;

    /// <summary>
    /// Postgres supports <c>NOTNULL</c> as an alias for <c>IS NOT NULL</c>
    /// See: <see href="https://www.postgresql.org/docs/17/functions-comparison.html"/>
    /// </summary>
    public override bool SupportsNotnullOperator => true;

    /// <summary>
    /// [Postgres] supports optional field and precision options for <c>INTERVAL</c> data type.
    /// <para>
    /// [Postgres]: https://www.postgresql.org/docs/17/datatype-datetime.html
    /// </para>
    /// </summary>
    public override bool SupportsIntervalOptions => true;

    public override bool SupportsInsertTableAlias => true;

    public override bool SupportsCreateTableLikeParenthesized => true;

    public override bool SupportsSelectWildcardWithAlias => true;

    public override bool SupportsCommaSeparatedTrim => true;

    public override bool SupportsXmlExpressions => true;

    public override bool SupportsAliasedFunctionArgs => true;

    /// <summary>
    /// Postgres supports query optimizer hints via the <c>pg_hint_plan</c> extension,
    /// using the same comment-prefixed-with-<c>+</c> syntax as MySQL and Oracle.
    /// <para>
    /// See <see href="https://github.com/ossc-db/pg_hint_plan"/>
    /// </para>
    /// </summary>
    public override bool SupportsCommentOptimizerHint => true;
}
