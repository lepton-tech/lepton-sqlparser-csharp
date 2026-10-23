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

namespace LeptonTech.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.postgresql.org/">PostgreSQL</see>
/// </summary>
public class PostgreSqlDialect : Dialect
{
    // Constantes de módulo no Rust (antes e depois do struct). RESERVED_EXCLUSIONS_FOR_TABLE_ALIAS,
    // BRACKET_PREC e COLLATE_PREC são usadas só por is_table_alias/get_next_precedence.

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

    private static readonly Keyword[] ReservedTypeNameKeywords =
    {
        Keyword.ALL,
        Keyword.ANALYZE,
        Keyword.AND,
        Keyword.ANY,
        Keyword.ARRAY,
        Keyword.AS,
        Keyword.ASC,
        Keyword.ASYMMETRIC,
        Keyword.BOTH,
        Keyword.CASE,
        Keyword.CAST,
        Keyword.CHECK,
        Keyword.COLLATE,
        Keyword.COLUMN,
        Keyword.CONSTRAINT,
        Keyword.CREATE,
        Keyword.CURRENT_CATALOG,
        Keyword.CURRENT_DATE,
        Keyword.CURRENT_ROLE,
        Keyword.CURRENT_TIME,
        Keyword.CURRENT_TIMESTAMP,
        Keyword.CURRENT_USER,
        Keyword.DEFAULT,
        Keyword.DEFERRABLE,
        Keyword.DESC,
        Keyword.DISTINCT,
        Keyword.DO,
        Keyword.ELSE,
        Keyword.END,
        Keyword.EXCEPT,
        Keyword.FALSE,
        Keyword.FETCH,
        Keyword.FOR,
        Keyword.FOREIGN,
        Keyword.FROM,
        Keyword.GRANT,
        Keyword.GROUP,
        Keyword.HAVING,
        Keyword.IN,
        Keyword.INITIALLY,
        Keyword.INTERSECT,
        Keyword.INTO,
        Keyword.LATERAL,
        Keyword.LEADING,
        Keyword.LIMIT,
        Keyword.LOCALTIME,
        Keyword.LOCALTIMESTAMP,
        Keyword.NOT,
        Keyword.NULL,
        Keyword.OFFSET,
        Keyword.ON,
        Keyword.ONLY,
        Keyword.OR,
        Keyword.ORDER,
        Keyword.PLACING,
        Keyword.PRIMARY,
        Keyword.REFERENCES,
        Keyword.RETURNING,
        Keyword.SELECT,
        Keyword.SESSION_USER,
        Keyword.SOME,
        Keyword.SYMMETRIC,
        Keyword.SYSTEM_USER,
        Keyword.TABLE,
        Keyword.THEN,
        Keyword.TO,
        Keyword.TRAILING,
        Keyword.TRUE,
        Keyword.UNION,
        Keyword.UNIQUE,
        Keyword.USER,
        Keyword.USING,
        Keyword.VARIADIC,
        Keyword.WHEN,
        Keyword.WHERE,
        Keyword.WINDOW,
        Keyword.WITH,
    };

    private static readonly Keyword[] ColNameKeywords =
    {
        Keyword.BETWEEN,
        Keyword.BIGINT,
        Keyword.BIT,
        Keyword.BOOLEAN,
        Keyword.CHAR,
        Keyword.CHARACTER,
        Keyword.COALESCE,
        Keyword.DEC,
        Keyword.DECIMAL,
        Keyword.EXISTS,
        Keyword.EXTRACT,
        Keyword.FLOAT,
        Keyword.GROUPING,
        Keyword.INOUT,
        Keyword.INT,
        Keyword.INTEGER,
        Keyword.INTERVAL,
        Keyword.JSON,
        Keyword.JSON_TABLE,
        Keyword.LEAST,
        Keyword.NATIONAL,
        Keyword.NCHAR,
        Keyword.NONE,
        Keyword.NORMALIZE,
        Keyword.NULLIF,
        Keyword.NUMERIC,
        Keyword.OUT,
        Keyword.OVERLAY,
        Keyword.POSITION,
        Keyword.PRECISION,
        Keyword.REAL,
        Keyword.ROW,
        Keyword.SETOF,
        Keyword.SMALLINT,
        Keyword.SUBSTRING,
        Keyword.TIME,
        Keyword.TIMESTAMP,
        Keyword.TREAT,
        Keyword.TRIM,
        Keyword.VALUES,
        Keyword.VARCHAR,
        Keyword.XMLNAMESPACES,
        Keyword.XMLTABLE,
    };

    private static readonly Keyword[] TypeFuncNameKeywords =
    {
        Keyword.AUTHORIZATION,
        Keyword.BINARY,
        Keyword.COLLATION,
        Keyword.CONCURRENTLY,
        Keyword.CROSS,
        Keyword.CURRENT_SCHEMA,
        Keyword.FREEZE,
        Keyword.FULL,
        Keyword.ILIKE,
        Keyword.INNER,
        Keyword.IS,
        Keyword.JOIN,
        Keyword.LEFT,
        Keyword.LIKE,
        Keyword.NATURAL,
        Keyword.NOTNULL,
        Keyword.OUTER,
        Keyword.OVERLAPS,
        Keyword.RIGHT,
        Keyword.SIMILAR,
        Keyword.TABLESAMPLE,
        Keyword.VERBOSE,
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

    public override bool SupportsCteMaterialization => true;

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

    public override bool IsTableAlias(Keyword kw, Parser parser) =>
        !Keywords.ReservedForTableAlias.Contains(kw)
            || ReservedExclusionsForTableAlias.Contains(kw);

    /// <summary>
    /// Aliases are identifiers, see <c>output_name</c> and <c>alias</c> in
    /// <see href="https://www.postgresql.org/docs/current/sql-select.html"/>
    /// </summary>
    public override bool SupportsStringLiteralColumnAlias(bool @explicit) => false;

    /// <summary>
    /// Aliases are identifiers, see <c>alias</c> in
    /// <see href="https://www.postgresql.org/docs/current/sql-select.html"/>
    /// </summary>
    public override bool SupportsStringLiteralTableAlias(bool @explicit) => false;

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

    public override byte? GetNextPrecedence(Parser parser)
    {
        var token = parser.PeekTokenRef();
        // debug!("get_next_precedence() {token:?}"); (log não traduzido, convencoes.md §13, caso 44)

        // we only return some custom value here when the behaviour (not merely the numeric value) differs
        // from the default implementation
        switch (token.Token)
        {
            case Token.Word { Value: var w }
                when w.Keyword == Keyword.COLLATE && !parser.InColumnDefinitionState():
                return CollatePrec;
            case Token.LBracket:
                return BracketPrec;
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
                or Token.QuestionPipe
                or Token.ExclamationMark
                or Token.Overlap
                or Token.CaretAt
                or Token.StringConcat
                or Token.Sharp
                or Token.ShiftRight
                or Token.ShiftLeft
                or Token.CustomBinaryOperator:
                return PgOtherPrec;
            // lowest prec to prevent it from turning into a binary op
            case Token.Colon:
                return PrecUnknown;
            default:
                return null;
        }
    }

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
    /// <para>
    /// The target list is optional in <see href="https://www.postgresql.org/docs/current/sql-select.html"/>
    /// </para>
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

    /// <summary>
    /// See <see href="https://www.postgresql.org/docs/current/sql-keywords-appendix.html"/>.
    /// </summary>
    public override bool IsCustomTypeTypedString(Keyword firstKeyword, bool qualified)
    {
        var notATypeName = qualified
            ? TypeFuncNameKeywords
            : ColNameKeywords;
        return !ReservedTypeNameKeywords.Contains(firstKeyword)
            && !notATypeName.Contains(firstKeyword);
    }

    public override bool SupportsAliasedFunctionArgs => true;

    /// <summary>
    /// Postgres supports query optimizer hints via the <c>pg_hint_plan</c> extension,
    /// using the same comment-prefixed-with-<c>+</c> syntax as MySQL and Oracle.
    /// <para>
    /// See <see href="https://github.com/ossc-db/pg_hint_plan"/>
    /// </para>
    /// </summary>
    public override bool SupportsCommentOptimizerHint => true;

    /// <summary>
    /// See <see href="https://www.postgresql.org/docs/current/sql-altertable.html"/>
    /// </summary>
    public override bool SupportsAddColumnIfNotExists => true;
}
