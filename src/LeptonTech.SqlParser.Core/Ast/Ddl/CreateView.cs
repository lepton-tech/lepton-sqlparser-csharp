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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>CREATE VIEW statement.</summary>
/// <param name="OrAlter">
/// True if this is a <c>CREATE OR ALTER VIEW</c> statement
/// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-view-transact-sql">MsSql</see></para>
/// </param>
/// <param name="OrReplace">The <c>OR REPLACE</c> clause is used to re-create the view if it already exists.</param>
/// <param name="Materialized">if true, has MATERIALIZED view modifier</param>
/// <param name="Secure">
/// Snowflake: SECURE view modifier
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-view#syntax"/>
/// </param>
/// <param name="Name">View name</param>
/// <param name="NameBeforeNotExists">
/// If <c>if_not_exists</c> is true, this flag is set to true if the view name comes before the <c>IF NOT EXISTS</c> clause.
/// Example:
/// <code>
/// CREATE VIEW myview IF NOT EXISTS AS SELECT 1`
/// </code>
/// Otherwise, the flag is set to false if the view name comes after the clause
/// Example:
/// <code>
/// CREATE VIEW IF NOT EXISTS myview AS SELECT 1`
/// </code>
/// </param>
/// <param name="Columns">Optional column definitions</param>
/// <param name="Query">The query that defines the view.</param>
/// <param name="Options">Table options (e.g., WITH (..), OPTIONS (...))</param>
/// <param name="ClusterBy">BigQuery: CLUSTER BY columns</param>
/// <param name="Comment">
/// Snowflake: Views can have comments in Snowflake.
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-view#syntax"/>
/// </param>
/// <param name="WithNoSchemaBinding">if true, has RedShift <c>WITH NO SCHEMA BINDING</c> clause <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CREATE_VIEW.html"/></param>
/// <param name="IfNotExists">if true, has SQLite <c>IF NOT EXISTS</c> clause <see href="https://www.sqlite.org/lang_createview.html"/></param>
/// <param name="Temporary">if true, has SQLite <c>TEMP</c> or <c>TEMPORARY</c> clause <see href="https://www.sqlite.org/lang_createview.html"/></param>
/// <param name="CopyGrants">
/// Snowflake: <c>COPY GRANTS</c> clause
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-view"/>
/// </param>
/// <param name="To">
/// if not None, has Clickhouse <c>TO</c> clause, specify the table into which to insert results
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/create/view#materialized-view"/>
/// </param>
/// <param name="Params">MySQL: Optional parameters for the view algorithm, definer, and security context</param>
public sealed partial record CreateView(
    bool OrAlter,
    bool OrReplace,
    bool Materialized,
    bool Secure,
    [property: VisitWith("visit_relation")] ObjectName Name,
    bool NameBeforeNotExists,
    Sequence<ViewColumnDef> Columns,
    Query Query,
    CreateTableOptions Options,
    Sequence<Ident> ClusterBy,
    string? Comment,
    bool WithNoSchemaBinding,
    bool IfNotExists,
    bool Temporary,
    bool CopyGrants,
    ObjectName? To,
    CreateViewParams? Params) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var orAlter = OrAlter ? "OR ALTER " : "";
        var orReplace = OrReplace ? "OR REPLACE " : "";
        f.Write($"CREATE {orAlter}{orReplace}");
        if (Params is { } @params)
        {
            @params.Fmt(f);
        }
        var secure = Secure ? "SECURE " : "";
        var materialized = Materialized ? "MATERIALIZED " : "";
        var temporary = Temporary ? "TEMPORARY " : "";
        // Rust: write!(f, "{secure}{materialized}{temporary}VIEW {if_not_and_name}{to}"), com
        // `if_not_and_name` e `to` montados por `format!`. Escrita direta na mesma posição: a lacuna usa
        // flags novos, como o `format!` (docs/port/onda-a/A2.md, decisão 3; caso 30).
        f.Write($"{secure}{materialized}{temporary}VIEW ");
        if (IfNotExists)
        {
            if (NameBeforeNotExists)
            {
                f.Write($"{Name} IF NOT EXISTS");
            }
            else
            {
                f.Write($"IF NOT EXISTS {Name}");
            }
        }
        else
        {
            f.Write($"{Name}");
        }
        if (To is { } to)
        {
            f.Write($" TO {to}");
        }
        if (CopyGrants)
        {
            f.Write($" COPY GRANTS");
        }
        if (!Columns.IsEmpty)
        {
            f.Write($" ({DisplayCommaSeparated(Columns)})");
        }
        if (Options is CreateTableOptions.With)
        {
            f.Write($" {Options}");
        }
        if (Comment is { } comment)
        {
            f.Write($" COMMENT = '{Ast.Value.EscapeSingleQuoteString(comment)}'");
        }
        if (!ClusterBy.IsEmpty)
        {
            f.Write($" CLUSTER BY ({DisplayCommaSeparated(ClusterBy)})");
        }
        if (Options is CreateTableOptions.Options)
        {
            f.Write($" {Options}");
        }
        f.WriteStr(" AS");
        SpaceOrNewline.Instance.Fmt(f);
        Query.Fmt(f);
        if (WithNoSchemaBinding)
        {
            f.Write($" WITH NO SCHEMA BINDING");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
