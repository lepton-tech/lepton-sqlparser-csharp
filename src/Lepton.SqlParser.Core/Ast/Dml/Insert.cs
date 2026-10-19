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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dml.rs.
// Modificado: tradução de Rust para C#.

using System.Text;
using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>INSERT statement.</summary>
/// <param name="InsertToken">Token for the <c>INSERT</c> keyword (or its substitutes)</param>
/// <param name="OptimizerHints">
/// Query optimizer hints
/// <para>
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/optimizer-hints.html">MySQL</see>
/// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Comments.html#GUID-D316D545-89E2-4D54-977F-FC97815CD62E">Oracle</see>
/// </para>
/// </param>
/// <param name="Or">Only for Sqlite</param>
/// <param name="Ignore">Only for mysql</param>
/// <param name="Into">INTO - optional keyword</param>
/// <param name="Table">TABLE</param>
/// <param name="TableAlias"><c>table_name as foo</c> (for PostgreSQL) <c>table_name foo</c> (for Oracle)</param>
/// <param name="Columns">COLUMNS</param>
/// <param name="ByName">
/// <c>BY NAME</c> clause used by Databricks SQL.
/// <para>
/// When present, columns from the source query are matched to columns in
/// the target table by name instead of by position. The syntax is:
/// </para>
/// <code>
/// INSERT INTO [TABLE] table_name
///     [PARTITION (...)]
///     [(column_name [, ...]) | BY NAME]
///     query
/// </code>
/// <para>See <see href="https://docs.databricks.com/gcp/en/sql/language-manual/sql-ref-syntax-dml-insert-into"/>.</para>
/// </param>
/// <param name="Overwrite">Overwrite (Hive)</param>
/// <param name="Source">A SQL query that specifies what to insert</param>
/// <param name="Assignments">
/// MySQL <c>INSERT INTO ... SET</c>
/// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/insert.html"/>
/// </param>
/// <param name="Partitioned">partitioned insert (Hive)</param>
/// <param name="AfterColumns">Columns defined after PARTITION</param>
/// <param name="HasTableKeyword">whether the insert has the table keyword (Hive)</param>
/// <param name="On">ON INSERT</param>
/// <param name="Returning">RETURNING</param>
/// <param name="Output">
/// OUTPUT (MSSQL)
/// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql"/>
/// </param>
/// <param name="ReplaceInto">Only for mysql</param>
/// <param name="Priority">Only for mysql</param>
/// <param name="InsertAlias">Only for mysql</param>
/// <param name="Settings">
/// Settings used for ClickHouse.
/// <para>ClickHouse syntax: <c>INSERT INTO tbl SETTINGS format_template_resultset = '/some/path/resultset.format'</c></para>
/// <para><see href="https://clickhouse.com/docs/en/sql-reference/statements/insert-into">ClickHouse <c>INSERT INTO</c></see></para>
/// </param>
/// <param name="FormatClause">
/// Format for <c>INSERT</c> statement when not using standard SQL format. Can be e.g. <c>CSV</c>,
/// <c>JSON</c>, <c>JSONAsString</c>, <c>LineAsString</c> and more.
/// <para>ClickHouse syntax: <c>INSERT INTO tbl FORMAT JSONEachRow {"foo": 1, "bar": 2}, {"foo": 3}</c></para>
/// <para><see href="https://clickhouse.com/docs/en/interfaces/formats#json-inserting-data">ClickHouse formats JSON insert</see></para>
/// </param>
/// <param name="MultiTableInsertType">
/// For Snowflake multi-table insert: specifies the type (<c>ALL</c> or <c>FIRST</c>)
/// <para>
/// - <c>None</c> means this is a regular single-table INSERT
/// - <c>Some(All)</c> means <c>INSERT ALL</c> (all matching WHEN clauses are executed)
/// - <c>Some(First)</c> means <c>INSERT FIRST</c> (only the first matching WHEN clause is executed)
/// </para>
/// <para>See: <see href="https://docs.snowflake.com/en/sql-reference/sql/insert-multi-table"/></para>
/// </param>
/// <param name="MultiTableIntoClauses">
/// For multi-table insert: additional INTO clauses (unconditional)
/// <para>Used for <c>INSERT ALL INTO t1 INTO t2 ... SELECT ...</c></para>
/// <para>See: <see href="https://docs.snowflake.com/en/sql-reference/sql/insert-multi-table"/></para>
/// </param>
/// <param name="MultiTableWhenClauses">
/// For conditional multi-table insert: WHEN clauses
/// <para>Used for <c>INSERT ALL/FIRST WHEN cond THEN INTO t1 ... SELECT ...</c></para>
/// <para>See: <see href="https://docs.snowflake.com/en/sql-reference/sql/insert-multi-table"/></para>
/// </param>
/// <param name="MultiTableElseClause">
/// For conditional multi-table insert: ELSE clause
/// <para>See: <see href="https://docs.snowflake.com/en/sql-reference/sql/insert-multi-table"/></para>
/// </param>
public sealed partial record Insert(
    AttachedToken InsertToken,
    Sequence<OptimizerHint> OptimizerHints,
    SqliteOnConflict? Or,
    bool Ignore,
    bool Into,
    TableObject Table,
    TableAliasWithoutColumns? TableAlias,
    Sequence<ObjectName> Columns,
    bool ByName,
    bool Overwrite,
    Query? Source,
    Sequence<Assignment> Assignments,
    Sequence<Expr>? Partitioned,
    Sequence<Ident> AfterColumns,
    bool HasTableKeyword,
    OnInsert? On,
    Sequence<SelectItem>? Returning,
    OutputClause? Output,
    bool ReplaceInto,
    MysqlInsertPriority? Priority,
    InsertAliases? InsertAlias,
    Sequence<Setting>? Settings,
    InputFormatClause? FormatClause,
    MultiTableInsertType? MultiTableInsertType,
    Sequence<MultiTableInsertIntoClause> MultiTableIntoClauses,
    Sequence<MultiTableInsertWhenClause> MultiTableWhenClauses,
    Sequence<MultiTableInsertIntoClause>? MultiTableElseClause) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // SQLite OR conflict has a special format: INSERT OR ... INTO table_name
        string tableName;
        if (TableAlias is { } tableAlias)
        {
            // format!("{table} {as_keyword}{alias}", ...): formatador novo, sem o flag alternate.
            var buffer = new StringBuilder();
            new Formatter(buffer).Write($"{Table} {(tableAlias.Explicit ? "AS " : "")}{tableAlias.Alias}");
            tableName = buffer.ToString();
        }
        else
        {
            tableName = Formatter.Format(Table);
        }

        if (Or is { } onConflict)
        {
            f.WriteStr("INSERT");
            foreach (var hint in OptimizerHints)
            {
                f.Write($" {hint}");
            }
            f.Write($" {onConflict} INTO {tableName} ");
        }
        else
        {
            f.Write($"{(ReplaceInto ? "REPLACE" : "INSERT")}");
            foreach (var hint in OptimizerHints)
            {
                f.Write($" {hint}");
            }
            if (Priority is { } priority)
            {
                f.Write($" {priority}");
            }

            if (Ignore)
            {
                f.Write($" IGNORE");
            }

            if (Overwrite)
            {
                f.Write($" OVERWRITE");
            }

            if (MultiTableInsertType is { } insertType)
            {
                f.Write($" {insertType}");
            }

            if (Into)
            {
                f.Write($" INTO");
            }

            if (HasTableKeyword)
            {
                f.Write($" TABLE");
            }

            if (tableName.Length != 0)
            {
                f.Write($" {tableName} ");
            }
        }

        if (!Columns.IsEmpty)
        {
            f.Write($"({DisplayCommaSeparated(Columns)})");
            SpaceOrNewline.Instance.Fmt(f);
        }

        if (Partitioned is { } parts)
        {
            if (!parts.IsEmpty)
            {
                f.Write($"PARTITION ({DisplayCommaSeparated(parts)})");
                SpaceOrNewline.Instance.Fmt(f);
            }
        }

        if (ByName)
        {
            f.Write($"BY NAME");
            SpaceOrNewline.Instance.Fmt(f);
        }

        if (!AfterColumns.IsEmpty)
        {
            f.Write($"({DisplayCommaSeparated(AfterColumns)})");
            SpaceOrNewline.Instance.Fmt(f);
        }

        if (Output is { } output)
        {
            f.Write($"{output}");
            SpaceOrNewline.Instance.Fmt(f);
        }

        if (Settings is { } settings)
        {
            f.Write($"SETTINGS {DisplayCommaSeparated(settings)}");
            SpaceOrNewline.Instance.Fmt(f);
        }

        foreach (var intoClause in MultiTableIntoClauses)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.Write($"{intoClause}");
        }

        foreach (var whenClause in MultiTableWhenClauses)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.Write($"{whenClause}");
        }

        if (MultiTableElseClause is { } elseClauses)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.Write($"ELSE");
            foreach (var intoClause in elseClauses)
            {
                SpaceOrNewline.Instance.Fmt(f);
                f.Write($"{intoClause}");
            }
        }

        if (Source is { } source)
        {
            if (!MultiTableIntoClauses.IsEmpty
                || !MultiTableWhenClauses.IsEmpty)
            {
                SpaceOrNewline.Instance.Fmt(f);
            }
            source.Fmt(f);
        }
        else if (!Assignments.IsEmpty)
        {
            f.Write($"SET");
            DisplayUtils.IndentedList(f, Assignments);
        }
        else if (FormatClause is { } formatClause)
        {
            formatClause.Fmt(f);
        }
        else if (Columns.IsEmpty)
        {
            f.Write($"DEFAULT VALUES");
        }

        if (InsertAlias is { } insertAlias)
        {
            f.Write($" AS {insertAlias.RowAlias}");

            if (insertAlias.ColAliases is { } colAliases)
            {
                if (!colAliases.IsEmpty)
                {
                    f.Write($" ({DisplayCommaSeparated(colAliases)})");
                }
            }
        }

        if (On is { } on)
        {
            f.Write($"{on}");
        }

        if (Returning is { } returning)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("RETURNING");
            DisplayUtils.IndentedList(f, returning);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
