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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/bigquery.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://cloud.google.com/bigquery/">Google Bigquery</see>
/// </summary>
public class BigQueryDialect : Dialect
{
    /// <summary>
    /// These keywords are disallowed as column identifiers. Such that
    /// <c>SELECT 5 AS &lt;col&gt; FROM T</c> is rejected by BigQuery.
    /// </summary>
    // Constante de módulo no Rust (antes do struct); usada só por is_column_alias.
    private static readonly Keyword[] ReservedForColumnAlias =
    {
        Keyword.WITH,
        Keyword.SELECT,
        Keyword.WHERE,
        Keyword.GROUP,
        Keyword.HAVING,
        Keyword.ORDER,
        Keyword.LATERAL,
        Keyword.LIMIT,
        Keyword.FETCH,
        Keyword.UNION,
        Keyword.EXCEPT,
        Keyword.INTERSECT,
        Keyword.FROM,
        Keyword.INTO,
        Keyword.END,
    };

    public override Statement? ParseStatement(Parser parser)
    {
        if (parser.ParseKeyword(Keyword.BEGIN))
        {
            if (parser.PeekKeyword(Keyword.TRANSACTION)
                || parser.PeekTokenRef().Token is Token.SemiColon
                || parser.PeekTokenRef().Token is Token.EOF)
            {
                parser.PrevToken();
                return null;
            }
            return parser.ParseBeginExceptionEnd();
        }

        return null;
    }

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#identifiers"/>
    /// </summary>
    public override bool IsDelimitedIdentifierStart(char ch) => ch == '`';

    /// <summary>
    /// See <see href="https://docs.cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_identifiers"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '`';

    public override bool SupportsProjectionTrailingCommas => true;

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_table_statement"/>
    /// </summary>
    public override bool SupportsColumnDefinitionTrailingCommas => true;

    public override bool IsIdentifierStart(char ch) =>
        (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_'
            // BigQuery supports `@@foo.bar` variable syntax in its procedural language.
            // https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#beginexceptionend
            || ch == '@';

    public override bool IsIdentifierPart(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_';

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">doc</see>
    /// </summary>
    public override bool SupportsTripleQuotedString => true;

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/navigation_functions#first_value">doc</see>
    /// </summary>
    public override bool SupportsWindowFunctionNullTreatmentArg => true;

    // See https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#escape_sequences
    public override bool SupportsStringLiteralBackslashEscape => true;

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/window-function-calls#ref_named_window">doc</see>
    /// </summary>
    public override bool SupportsWindowClauseNamedWindowReference => true;

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#set">doc</see>
    /// </summary>
    public override bool SupportsParenthesizedSetVariables => true;

    // See https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#select_except
    public override bool SupportsSelectWildcardExcept => true;

    public override bool RequireIntervalQualifier => true;

    // See https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#constructing_a_struct
    public override bool SupportsStructLiteral => true;

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#select_expression_star"/>
    /// </summary>
    public override bool SupportsSelectExprStar => true;

    /// <summary>
    /// BigQuery allows a query to start with <c>FROM</c> (e.g. <c>FROM t</c>, and the
    /// entry form for pipe syntax, <c>FROM t |&gt; ...</c>).
    /// <para>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#from_queries"/>
    /// </para>
    /// </summary>
    public override bool SupportsFromFirstSelect => true;

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#execute_immediate"/>
    /// </summary>
    public override bool SupportsExecuteImmediate => true;

    // See <https://cloud.google.com/bigquery/docs/access-historical-data>
    public override bool SupportsTableVersioning => true;

    // See <https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#group_by_clause>
    public override bool SupportsGroupByExpr => true;

    public override bool IsColumnAlias(Keyword kw, Parser parser) => !ReservedForColumnAlias.Contains(kw);

    public override bool SupportsPipeOperator => true;

    public override bool SupportsCreateTableMultiSchemaInfoSources => true;

    /// <summary>
    /// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#select_replace"/>
    /// </summary>
    public override bool SupportsSelectWildcardReplace => true;

    public override bool SupportsCommaSeparatedTrim => true;
}
