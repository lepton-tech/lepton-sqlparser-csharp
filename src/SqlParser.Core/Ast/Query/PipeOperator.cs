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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// Pipe syntax, first introduced in Google BigQuery.
/// Example:
/// <code>
/// FROM Produce
/// |&gt; WHERE sales &gt; 0
/// |&gt; AGGREGATE SUM(sales) AS total_sales, COUNT(*) AS num_sales
///    GROUP BY item;
/// </code>
/// See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#pipe_syntax"/>
/// </summary>
/// <remarks>
/// As variantes <c>TableSample</c> e <c>Join</c> escondem os tipos de topo homônimos dentro deste
/// record; por isso eles aparecem qualificados como <c>Ast.X</c> (convencoes.md §6 e §13, caso 38).
/// </remarks>
public abstract partial record PipeOperator : IDisplay
{
    /// <summary>
    /// Limits the number of rows to return in a query, with an optional OFFSET clause to skip over rows.
    /// <para>Syntax: <c>|&gt; LIMIT &lt;n&gt; [OFFSET &lt;m&gt;]</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#limit_pipe_operator"/></para>
    /// </summary>
    /// <param name="Expr">The expression specifying the number of rows to return.</param>
    /// <param name="Offset">Optional offset expression provided inline with <c>LIMIT</c>.</param>
    public sealed record Limit(Expr Expr, Expr? Offset) : PipeOperator;

    /// <summary>
    /// Filters the results of the input table.
    /// <para>Syntax: <c>|&gt; WHERE &lt;condition&gt;</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#where_pipe_operator"/></para>
    /// </summary>
    /// <param name="Expr">The filter expression.</param>
    public sealed record Where(Expr Expr) : PipeOperator;

    /// <summary><c>ORDER BY &lt;expr&gt; [ASC|DESC], ...</c></summary>
    /// <param name="Exprs">The ordering expressions.</param>
    public sealed record OrderBy(Sequence<OrderByExpr> Exprs) : PipeOperator;

    /// <summary>
    /// Produces a new table with the listed columns, similar to the outermost SELECT clause in a table subquery in standard syntax.
    /// <para>Syntax <c>|&gt; SELECT &lt;expr&gt; [[AS] alias], ...</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#select_pipe_operator"/></para>
    /// </summary>
    /// <param name="Exprs">The select items to produce.</param>
    public sealed record Select(Sequence<SelectItem> Exprs) : PipeOperator;

    /// <summary>
    /// Propagates the existing table and adds computed columns, similar to SELECT *, new_column in standard syntax.
    /// <para>Syntax: <c>|&gt; EXTEND &lt;expr&gt; [[AS] alias], ...</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#extend_pipe_operator"/></para>
    /// </summary>
    /// <param name="Exprs">Expressions defining added columns.</param>
    public sealed record Extend(Sequence<SelectItem> Exprs) : PipeOperator;

    /// <summary>
    /// Replaces the value of a column in the current table, similar to SELECT * REPLACE (expression AS column) in standard syntax.
    /// <para>Syntax: <c>|&gt; SET &lt;column&gt; = &lt;expression&gt;, ...</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#set_pipe_operator"/></para>
    /// </summary>
    /// <param name="Assignments">Assignments to apply (<c>column = expr</c>).</param>
    public sealed record Set(Sequence<Assignment> Assignments) : PipeOperator;

    /// <summary>
    /// Removes listed columns from the current table, similar to SELECT * EXCEPT (column) in standard syntax.
    /// <para>Syntax: <c>|&gt; DROP &lt;column&gt;, ...</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#drop_pipe_operator"/></para>
    /// </summary>
    /// <param name="Columns">Columns to drop.</param>
    public sealed record Drop(Sequence<Ident> Columns) : PipeOperator;

    /// <summary>
    /// Introduces a table alias for the input table, similar to applying the AS alias clause on a table subquery in standard syntax.
    /// <para>Syntax: <c>|&gt; AS &lt;alias&gt;</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#as_pipe_operator"/></para>
    /// </summary>
    /// <param name="Alias">Alias to assign to the input table.</param>
    public sealed record As(Ident Alias) : PipeOperator;

    /// <summary>
    /// Performs aggregation on data across grouped rows or an entire table.
    /// <para>Syntax: <c>|&gt; AGGREGATE &lt;agg_expr&gt; [[AS] alias], ...</c></para>
    /// <para>Syntax:</para>
    /// <code>
    /// |&gt; AGGREGATE [&lt;agg_expr&gt; [[AS] alias], ...]
    /// GROUP BY &lt;grouping_expr&gt; [AS alias], ...
    /// </code>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#aggregate_pipe_operator"/></para>
    /// </summary>
    /// <param name="FullTableExprs">Expressions computed for each row prior to grouping.</param>
    /// <param name="GroupByExpr">Grouping expressions for aggregation.</param>
    public sealed record Aggregate(Sequence<ExprWithAliasAndOrderBy> FullTableExprs, Sequence<ExprWithAliasAndOrderBy> GroupByExpr) : PipeOperator;

    /// <summary>
    /// Selects a random sample of rows from the input table.
    /// Syntax: `|&gt; TABLESAMPLE SYSTEM (10 PERCENT)
    /// See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#tablesample_pipe_operator"/>
    /// </summary>
    /// <param name="Sample">Sampling clause describing the sample.</param>
    public sealed record TableSample(Ast.TableSample Sample) : PipeOperator;

    /// <summary>
    /// Renames columns in the input table.
    /// <para>Syntax: <c>|&gt; RENAME old_name AS new_name, ...</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#rename_pipe_operator"/></para>
    /// </summary>
    /// <param name="Mappings">Mappings of old to new identifiers.</param>
    public sealed record Rename(Sequence<IdentWithAlias> Mappings) : PipeOperator;

    /// <summary>
    /// Combines the input table with one or more tables using UNION.
    /// <para>Syntax: <c>|&gt; UNION [ALL|DISTINCT] (&lt;query&gt;), (&lt;query&gt;), ...</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#union_pipe_operator"/></para>
    /// </summary>
    /// <param name="SetQuantifier">Set quantifier (<c>ALL</c> or <c>DISTINCT</c>).</param>
    /// <param name="Queries">The queries to combine with <c>UNION</c>.</param>
    public sealed record Union(SetQuantifier SetQuantifier, Sequence<Query> Queries) : PipeOperator;

    /// <summary>
    /// Returns only the rows that are present in both the input table and the specified tables.
    /// <para>Syntax: <c>|&gt; INTERSECT [DISTINCT] (&lt;query&gt;), (&lt;query&gt;), ...</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#intersect_pipe_operator"/></para>
    /// </summary>
    /// <param name="SetQuantifier">Set quantifier for the <c>INTERSECT</c> operator.</param>
    /// <param name="Queries">The queries to intersect.</param>
    public sealed record Intersect(SetQuantifier SetQuantifier, Sequence<Query> Queries) : PipeOperator;

    /// <summary>
    /// Returns only the rows that are present in the input table but not in the specified tables.
    /// <para>Syntax: <c>|&gt; EXCEPT DISTINCT (&lt;query&gt;), (&lt;query&gt;), ...</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#except_pipe_operator"/></para>
    /// </summary>
    /// <param name="SetQuantifier">Set quantifier for the <c>EXCEPT</c> operator.</param>
    /// <param name="Queries">The queries to exclude from the input set.</param>
    public sealed record Except(SetQuantifier SetQuantifier, Sequence<Query> Queries) : PipeOperator;

    /// <summary>
    /// Calls a table function or procedure that returns a table.
    /// <para>Syntax: <c>|&gt; CALL function_name(args) [AS alias]</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#call_pipe_operator"/></para>
    /// </summary>
    /// <param name="Function">The function or procedure to call which returns a table.</param>
    /// <param name="Alias">Optional alias for the result table.</param>
    public sealed record Call(Function Function, Ident? Alias) : PipeOperator;

    /// <summary>
    /// Pivots data from rows to columns.
    /// <para>Syntax: <c>|&gt; PIVOT(aggregate_function(column) FOR pivot_column IN (value1, value2, ...)) [AS alias]</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#pivot_pipe_operator"/></para>
    /// </summary>
    /// <param name="AggregateFunctions">Aggregate functions to compute during pivot.</param>
    /// <param name="ValueColumn">Column(s) that provide the pivot values.</param>
    /// <param name="ValueSource">The source of pivot values (literal list or subquery).</param>
    /// <param name="Alias">Optional alias for the output.</param>
    public sealed record Pivot(
        Sequence<ExprWithAlias> AggregateFunctions,
        Sequence<Ident> ValueColumn,
        PivotValueSource ValueSource,
        Ident? Alias) : PipeOperator;

    /// <summary>
    /// The <c>UNPIVOT</c> pipe operator transforms columns into rows.
    /// <para>Syntax:</para>
    /// <code>
    /// |&gt; UNPIVOT(value_column FOR name_column IN (column1, column2, ...)) [alias]
    /// </code>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#unpivot_pipe_operator"/></para>
    /// </summary>
    /// <param name="ValueColumn">Output column that will receive the unpivoted value.</param>
    /// <param name="NameColumn">Column name holding the unpivoted column name.</param>
    /// <param name="UnpivotColumns">Columns to unpivot.</param>
    /// <param name="Alias">Optional alias for the unpivot result.</param>
    public sealed record Unpivot(
        Ident ValueColumn,
        Ident NameColumn,
        Sequence<Ident> UnpivotColumns,
        Ident? Alias) : PipeOperator;

    /// <summary>
    /// Joins the input table with another table.
    /// <para>Syntax: <c>|&gt; [JOIN_TYPE] JOIN &lt;table&gt; [alias] ON &lt;condition&gt;</c> or <c>|&gt; [JOIN_TYPE] JOIN &lt;table&gt; [alias] USING (&lt;columns&gt;)</c></para>
    /// <para>See more at <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/pipe-syntax#join_pipe_operator"/></para>
    /// </summary>
    public sealed record Join(Ast.Join Value) : PipeOperator;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Select { Exprs: var exprs }:
                f.Write($"SELECT {DisplayCommaSeparated(exprs)}");
                break;
            case Extend { Exprs: var exprs }:
                f.Write($"EXTEND {DisplayCommaSeparated(exprs)}");
                break;
            case Set { Assignments: var assignments }:
                f.Write($"SET {DisplayCommaSeparated(assignments)}");
                break;
            case Drop { Columns: var columns }:
                f.Write($"DROP {DisplayCommaSeparated(columns)}");
                break;
            case As { Alias: var alias }:
                f.Write($"AS {alias}");
                break;
            case Limit { Expr: var expr, Offset: var offset }:
                f.Write($"LIMIT {expr}");
                if (offset is { } offsetExpr)
                {
                    f.Write($" OFFSET {offsetExpr}");
                }
                break;
            case Aggregate { FullTableExprs: var fullTableExprs, GroupByExpr: var groupByExpr }:
                f.Write($"AGGREGATE");
                if (!fullTableExprs.IsEmpty)
                {
                    f.Write($" {DisplayCommaSeparated(fullTableExprs)}");
                }
                if (!groupByExpr.IsEmpty)
                {
                    f.Write($" GROUP BY {DisplayCommaSeparated(groupByExpr)}");
                }
                break;

            case Where { Expr: var expr }:
                f.Write($"WHERE {expr}");
                break;
            case OrderBy { Exprs: var exprs }:
                f.Write($"ORDER BY {DisplayCommaSeparated(exprs)}");
                break;

            case TableSample { Sample: var sample }:
                f.Write($"{sample}");
                break;
            case Rename { Mappings: var mappings }:
                f.Write($"RENAME {DisplayCommaSeparated(mappings)}");
                break;
            case Union { SetQuantifier: var setQuantifier, Queries: var queries }:
                FmtSetOperation(f, "UNION", setQuantifier, queries);
                break;
            case Intersect { SetQuantifier: var setQuantifier, Queries: var queries }:
                FmtSetOperation(f, "INTERSECT", setQuantifier, queries);
                break;
            case Except { SetQuantifier: var setQuantifier, Queries: var queries }:
                FmtSetOperation(f, "EXCEPT", setQuantifier, queries);
                break;
            case Call { Function: var function, Alias: var alias }:
                f.Write($"CALL {function}");
                FmtOptionalAlias(f, alias);
                break;
            case Pivot { AggregateFunctions: var aggregateFunctions, ValueColumn: var valueColumn, ValueSource: var valueSource, Alias: var alias }:
                f.Write($"PIVOT({DisplayCommaSeparated(aggregateFunctions)} FOR {new Expr.CompoundIdentifier(valueColumn)} IN ({valueSource}))");
                FmtOptionalAlias(f, alias);
                break;
            case Unpivot { ValueColumn: var valueColumn, NameColumn: var nameColumn, UnpivotColumns: var unpivotColumns, Alias: var alias }:
                f.Write($"UNPIVOT({valueColumn} FOR {nameColumn} IN ({DisplayCommaSeparated(unpivotColumns)}))");
                FmtOptionalAlias(f, alias);
                break;
            case Join { Value: var join }:
                f.Write($"{join}");
                break;
            default:
                throw new InvalidOperationException($"Variante de PipeOperator não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);

    /// <summary>Helper function to format optional alias for pipe operators</summary>
    private static void FmtOptionalAlias(Formatter f, Ident? alias)
    {
        if (alias is { } a)
        {
            f.Write($" AS {a}");
        }
    }

    /// <summary>Helper function to format set operations (UNION, INTERSECT, EXCEPT) with queries</summary>
    private static void FmtSetOperation(
        Formatter f,
        string operation,
        SetQuantifier setQuantifier,
        IReadOnlyList<Query> queries)
    {
        f.Write($"{operation}");
        switch (setQuantifier)
        {
            case SetQuantifier.None:
                break;
            default:
                f.Write($" {setQuantifier}");
                break;
        }
        f.Write($" ");
        // format!("({query})") → Formatter.Format: como no Rust, cada consulta vira String sem o flag {:#}.
        var parenthesizedQueries = new List<string>();
        foreach (var query in queries)
        {
            parenthesizedQueries.Add("(" + Formatter.Format(query) + ")");
        }
        f.Write($"{DisplayCommaSeparated(parenthesizedQueries)}");
    }
}
