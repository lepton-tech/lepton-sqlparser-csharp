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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/snowflake.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.snowflake.com/">Snowflake</see>
/// </summary>
public class SnowflakeDialect : Dialect
{
    // Constantes de módulo no Rust (antes do struct). RESERVED_KEYWORDS_FOR_TABLE_FACTOR é usada só
    // por is_table_factor (Fase 2).
    private static readonly Keyword[] ReservedKeywordsForSelectItemOperator = { Keyword.CONNECT_BY_ROOT };

    // See: <https://docs.snowflake.com/en/sql-reference/reserved-keywords>
    private static readonly Keyword[] ReservedKeywordsForTableFactor =
    {
        Keyword.ALL,
        Keyword.ALTER,
        Keyword.AND,
        Keyword.ANY,
        Keyword.AS,
        Keyword.BETWEEN,
        Keyword.BY,
        Keyword.CHECK,
        Keyword.COLUMN,
        Keyword.CONNECT,
        Keyword.CREATE,
        Keyword.CROSS,
        Keyword.CURRENT,
        Keyword.DELETE,
        Keyword.DISTINCT,
        Keyword.DROP,
        Keyword.ELSE,
        Keyword.EXISTS,
        Keyword.FOLLOWING,
        Keyword.FOR,
        Keyword.FROM,
        Keyword.FULL,
        Keyword.GRANT,
        Keyword.GROUP,
        Keyword.HAVING,
        Keyword.ILIKE,
        Keyword.IN,
        Keyword.INCREMENT,
        Keyword.INNER,
        Keyword.INSERT,
        Keyword.INTERSECT,
        Keyword.INTO,
        Keyword.IS,
        Keyword.JOIN,
        Keyword.LEFT,
        Keyword.LIKE,
        Keyword.MINUS,
        Keyword.NATURAL,
        Keyword.NOT,
        Keyword.NULL,
        Keyword.OF,
        Keyword.ON,
        Keyword.OR,
        Keyword.ORDER,
        Keyword.QUALIFY,
        Keyword.REGEXP,
        Keyword.REVOKE,
        Keyword.RIGHT,
        Keyword.RLIKE,
        Keyword.ROW,
        Keyword.ROWS,
        Keyword.SAMPLE,
        Keyword.SELECT,
        Keyword.SET,
        Keyword.SOME,
        Keyword.START,
        Keyword.TABLE,
        Keyword.TABLESAMPLE,
        Keyword.THEN,
        Keyword.TO,
        Keyword.TRIGGER,
        Keyword.UNION,
        Keyword.UNIQUE,
        Keyword.UPDATE,
        Keyword.USING,
        Keyword.VALUES,
        Keyword.WHEN,
        Keyword.WHENEVER,
        Keyword.WHERE,
        Keyword.WINDOW,
        Keyword.WITH,
    };

    // see https://docs.snowflake.com/en/sql-reference/identifiers-syntax.html
    public override bool IsIdentifierStart(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/identifiers-syntax"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '"';

    public override bool SupportsProjectionTrailingCommas => true;

    public override bool SupportsFromTrailingCommas => true;

    // Snowflake supports double-dot notation when the schema name is not specified
    // In this case the default PUBLIC schema is used
    //
    // see https://docs.snowflake.com/en/sql-reference/name-resolution#resolution-when-schema-omitted-double-dot-notation
    public override bool SupportsObjectNameDoubleDotNotation => true;

    public override bool IsIdentifierPart(char ch) =>
        (ch >= 'a' && ch <= 'z')
            || (ch >= 'A' && ch <= 'Z')
            || (ch >= '0' && ch <= '9')
            || ch == '$'
            || ch == '_';

    // See https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#escape_sequences
    public override bool SupportsStringLiteralBackslashEscape => true;

    public override bool SupportsWithinAfterArrayAggregation => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/where#joins-in-the-where-clause"/>
    /// </summary>
    public override bool SupportsOuterJoinOperator => true;

    public override bool SupportsConnectBy => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/execute-immediate"/>
    /// </summary>
    public override bool SupportsExecuteImmediate => true;

    public override bool SupportsMatchRecognize => true;

    // Snowflake uses this syntax for "object constants" (the values of which
    // are not actually required to be constants).
    //
    // https://docs.snowflake.com/en/sql-reference/data-types-semistructured#label-object-constant
    public override bool SupportsDictionarySyntax => true;

    // Snowflake doesn't document this but `FIRST_VALUE(arg, { IGNORE | RESPECT } NULLS)`
    // works (i.e. inside the argument list instead of after).
    public override bool SupportsWindowFunctionNullTreatmentArg => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/set#syntax">doc</see>
    /// </summary>
    public override bool SupportsParenthesizedSetVariables => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/comment">doc</see>
    /// </summary>
    public override bool SupportsCommentOn => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/functions/extract">doc</see>
    /// </summary>
    public override bool SupportsExtractCommaSyntax => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/functions/flatten">doc</see>
    /// </summary>
    public override bool SupportsSubqueryAsFunctionArg => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/create-view#optional-parameters">doc</see>
    /// </summary>
    public override bool SupportsCreateViewCommentSyntax => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/data-types-semistructured#array">doc</see>
    /// </summary>
    public override bool SupportsArrayTypedefWithoutElementType => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/from">doc</see>
    /// </summary>
    public override bool SupportsParensAroundTableFactor => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/values">doc</see>
    /// </summary>
    public override bool SupportsValuesAsTableFactor => true;

    // PENDENTE (Fase 2, depende de Parser/AST): parse_statement (snowflake.rs:253).

    // PENDENTE (Fase 2, depende de Parser/AST): parse_column_option (snowflake.rs:413).

    // PENDENTE (Fase 2, depende de Parser/AST): get_next_precedence (snowflake.rs:443).

    public override bool DescribeRequiresTableKeyword => true;

    public override bool AllowExtractCustom => true;

    public override bool AllowExtractSingleQuotes => true;

    /// <summary>
    /// Snowflake expects the <c>LIKE</c> option before the <c>IN</c> option,
    /// for example: <see href="https://docs.snowflake.com/en/sql-reference/sql/show-views#syntax"/>
    /// </summary>
    public override bool SupportsShowLikeBeforeIn => true;

    public override bool SupportsLeftAssociativeJoinsWithoutParens => false;

    public override bool IsReservedForIdentifier(Keyword kw)
    {
        // Unreserve some keywords that Snowflake accepts as identifiers
        // See: https://docs.snowflake.com/en/sql-reference/reserved-keywords
        if (kw is Keyword.INTERVAL)
        {
            return false;
        }
        else
        {
            return Keywords.ReservedForIdentifier.Contains(kw);
        }
    }

    public override bool SupportsPartiql => true;

    // PENDENTE (Fase 2, depende de Parser/AST): is_column_alias (snowflake.rs:488).

    // PENDENTE (Fase 2, depende de Parser/AST): is_table_alias (snowflake.rs:534).

    // PENDENTE (Fase 2, depende de Parser/AST): is_table_factor (snowflake.rs:618).

    /// <summary>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/constructs/at-before"/>
    /// </summary>
    public override bool SupportsTableVersioning => true;

    /// <summary>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/constructs/group-by"/>
    /// </summary>
    public override bool SupportsGroupByExpr => true;

    /// <summary>
    /// See: <see href="https://docs.snowflake.com/en/sql-reference/constructs/connect-by"/>
    /// </summary>
    public override IReadOnlyList<Keyword> GetReservedKeywordsForSelectItemOperator => ReservedKeywordsForSelectItemOperator;

    public override bool SupportsSpaceSeparatedColumnOptions => true;

    public override bool SupportsCommaSeparatedDropColumnList => true;

    // PENDENTE (Fase 2, depende da AST): is_identifier_generating_function_name (snowflake.rs:650).

    // For example: `SELECT IDENTIFIER('alias1').* FROM tbl AS alias1`
    public override bool SupportsSelectExprStar => true;

    public override bool SupportsSelectWildcardExclude => true;

    public override bool SupportsSemanticViewTableFactor => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters"/>
    /// </summary>
    public override bool SupportsSelectWildcardReplace => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters"/>
    /// </summary>
    public override bool SupportsSelectWildcardIlike => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters"/>
    /// </summary>
    public override bool SupportsSelectWildcardRename => true;

    /// <summary>
    /// See <see href="https://docs.snowflake.com/en/user-guide/querying-semistructured#label-higher-order-functions"/>
    /// </summary>
    public override bool SupportsLambdaFunctions => true;

    public override bool SupportsCommaSeparatedTrim => true;

    // PENDENTE (Fase 2, depende de Parser/AST): funções livres de snowflake.rs (linhas 700–1990), de
    // peek_for_limit_options até parse_multi_table_insert_when_clauses, inclusive as públicas
    // parse_create_table, parse_create_database, parse_storage_serialization_policy, parse_create_stage,
    // parse_create_file_format, parse_stage_name_identifier, parse_snowflake_stage_name (reexportada em
    // dialect/mod.rs) e parse_copy_into. Lista completa em docs/port/progresso.md.
}
