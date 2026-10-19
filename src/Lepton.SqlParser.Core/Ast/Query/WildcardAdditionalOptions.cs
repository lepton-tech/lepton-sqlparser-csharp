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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Additional options for wildcards, e.g. Snowflake <c>EXCLUDE</c>/<c>RENAME</c> and Bigquery <c>EXCEPT</c>.</summary>
/// <param name="WildcardToken">The wildcard token <c>*</c></param>
/// <param name="OptIlike">
/// <c>[ILIKE...]</c>.
/// Snowflake syntax: <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters"/>
/// </param>
/// <param name="OptExclude"><c>[EXCLUDE...]</c>.</param>
/// <param name="OptExcept">
/// <c>[EXCEPT...]</c>.
/// Clickhouse syntax: <see href="https://clickhouse.com/docs/en/sql-reference/statements/select#except"/>
/// </param>
/// <param name="OptReplace">
/// <c>[REPLACE]</c>
/// BigQuery syntax: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#select_replace"/>
/// Clickhouse syntax: <see href="https://clickhouse.com/docs/en/sql-reference/statements/select#replace"/>
/// Snowflake syntax: <see href="https://docs.snowflake.com/en/sql-reference/sql/select#parameters"/>
/// </param>
/// <param name="OptRename"><c>[RENAME ...]</c>.</param>
/// <param name="OptAlias">
/// <c>[AS &lt;alias&gt;]</c>.
/// Redshift syntax: <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_SELECT_list.html"/>
/// </param>
public sealed partial record WildcardAdditionalOptions(
    AttachedToken WildcardToken,
    IlikeSelectItem? OptIlike,
    ExcludeSelectItem? OptExclude,
    ExceptSelectItem? OptExcept,
    ReplaceSelectItem? OptReplace,
    RenameSelectItem? OptRename,
    Ident? OptAlias) : IDisplay
{
    // Rust: `impl Default for WildcardAdditionalOptions` (convencoes.md §7 e §13, caso 42).
    public static WildcardAdditionalOptions Default() =>
        new WildcardAdditionalOptions(
            WildcardToken: AttachedToken.From(TokenWithSpan.Wrap(new Token.Mul())),
            OptIlike: null,
            OptExclude: null,
            OptExcept: null,
            OptReplace: null,
            OptRename: null,
            OptAlias: null);

    public void Fmt(Formatter f)
    {
        if (OptIlike is { } ilike)
        {
            f.Write($" {ilike}");
        }
        if (OptExclude is { } exclude)
        {
            f.Write($" {exclude}");
        }
        if (OptExcept is { } except)
        {
            f.Write($" {except}");
        }
        if (OptReplace is { } replace)
        {
            f.Write($" {replace}");
        }
        if (OptRename is { } rename)
        {
            f.Write($" {rename}");
        }
        if (OptAlias is { } alias)
        {
            f.Write($" AS {alias}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
