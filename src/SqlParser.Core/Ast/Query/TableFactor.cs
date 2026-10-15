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

/// <summary>A table name or a parenthesized subquery with an optional alias</summary>
[VisitWith("visit_table_factor")]
public abstract partial record TableFactor : IDisplay
{
    /// <summary>A named table or relation, possibly with arguments, hints, or sampling.</summary>
    /// <param name="Name">Table or relation name.</param>
    /// <param name="Alias">Optional alias for the table (e.g. <c>table AS t</c>).</param>
    /// <param name="Args">
    /// Arguments of a table-valued function, as supported by Postgres
    /// and MSSQL. Note that deprecated MSSQL <c>FROM foo (NOLOCK)</c> syntax
    /// will also be parsed as <c>args</c>.
    /// <para>
    /// This field's value is <c>Some(v)</c>, where <c>v</c> is a (possibly empty)
    /// vector of arguments, in the case of a table-valued function call,
    /// whereas it's <c>None</c> in the case of a regular table name.
    /// </para>
    /// </param>
    /// <param name="WithHints">MSSQL-specific <c>WITH (...)</c> hints such as NOLOCK.</param>
    /// <param name="Version">
    /// Optional version qualifier to facilitate table time-travel, as
    /// supported by BigQuery and MSSQL.
    /// </param>
    /// <param name="WithOrdinality">
    /// For example, <c>SELECT * FROM generate_series(1, 10) WITH ORDINALITY AS t(a, b);</c>
    /// <see href="https://www.postgresql.org/docs/current/functions-srf.html">WITH ORDINALITY</see>, supported by Postgres.
    /// </param>
    /// <param name="Partitions"><see href="https://dev.mysql.com/doc/refman/8.0/en/partitioning-selection.html">Partition selection</see>, supported by MySQL.</param>
    /// <param name="JsonPath">Optional PartiQL JsonPath: <see href="https://partiql.org/dql/from.html"/></param>
    /// <param name="Sample">
    /// Optional table sample modifier
    /// See: <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#sample-clause"/>
    /// </param>
    /// <param name="IndexHints">
    /// Optional index hints(mysql)
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/index-hints.html"/>
    /// </param>
    public sealed record Table(
        [property: VisitWith("visit_relation")] ObjectName Name,
        TableAlias? Alias,
        TableFunctionArgs? Args,
        Sequence<Expr> WithHints,
        TableVersion? Version,
        //  Optional table function modifier to generate the ordinality for column.
        bool WithOrdinality,
        Sequence<Ident> Partitions,
        JsonPath? JsonPath,
        TableSampleKind? Sample,
        Sequence<TableIndexHints> IndexHints) : TableFactor;

    /// <summary>A derived table (a parenthesized subquery), optionally <c>LATERAL</c>.</summary>
    /// <param name="Lateral">Whether the derived table is LATERAL.</param>
    /// <param name="Subquery">The subquery producing the derived table.</param>
    /// <param name="Alias">Optional alias for the derived table.</param>
    /// <param name="Sample">Optional table sample modifier</param>
    public sealed record Derived(bool Lateral, Query Subquery, TableAlias? Alias, TableSampleKind? Sample) : TableFactor;

    /// <summary><c>TABLE(&lt;expr&gt;)[ AS &lt;alias&gt; ]</c></summary>
    /// <param name="Expr">Expression representing the table function call.</param>
    /// <param name="Alias">Optional alias for the table function result.</param>
    public sealed record TableFunction(Expr Expr, TableAlias? Alias) : TableFactor;

    /// <summary><c>e.g. LATERAL FLATTEN(&lt;args&gt;)[ AS &lt;alias&gt; ]</c></summary>
    /// <param name="Lateral">Whether the function is LATERAL.</param>
    /// <param name="Name">Name of the table function.</param>
    /// <param name="Args">Arguments passed to the function.</param>
    /// <param name="WithOrdinality">Whether <c>WITH ORDINALITY</c> was specified to include ordinality.</param>
    /// <param name="Alias">Optional alias for the result of the function.</param>
    public sealed record Function(
        bool Lateral,
        ObjectName Name,
        Sequence<FunctionArg> Args,
        bool WithOrdinality,
        TableAlias? Alias) : TableFactor;

    /// <summary>
    /// <code>
    /// SELECT * FROM UNNEST ([10,20,30]) as numbers WITH OFFSET;
    /// +---------+--------+
    /// | numbers | offset |
    /// +---------+--------+
    /// | 10      | 0      |
    /// | 20      | 1      |
    /// | 30      | 2      |
    /// +---------+--------+
    /// </code>
    /// </summary>
    /// <param name="Alias">Optional alias for the UNNEST table (e.g. <c>UNNEST(...) AS t</c>).</param>
    /// <param name="ArrayExprs">Expressions producing the arrays to be unnested.</param>
    /// <param name="WithOffset">Whether <c>WITH OFFSET</c> was specified to include element offsets.</param>
    /// <param name="WithOffsetAlias">Optional alias for the offset column when <c>WITH OFFSET</c> is used.</param>
    /// <param name="WithOrdinality">Whether <c>WITH ORDINALITY</c> was specified to include ordinality.</param>
    public sealed record UNNEST(
        TableAlias? Alias,
        Sequence<Expr> ArrayExprs,
        bool WithOffset,
        Ident? WithOffsetAlias,
        bool WithOrdinality) : TableFactor;

    /// <summary>
    /// The <c>JSON_TABLE</c> table-valued function.
    /// Part of the SQL standard, but implemented only by MySQL, Oracle, and DB2.
    /// <para><see href="https://modern-sql.com/blog/2017-06/whats-new-in-sql-2016#json_table"/></para>
    /// <para><see href="https://dev.mysql.com/doc/refman/8.0/en/json-table-functions.html#function_json-table"/></para>
    /// <code>
    /// SELECT * FROM JSON_TABLE(
    ///    '[{"a": 1, "b": 2}, {"a": 3, "b": 4}]',
    ///    '$[*]' COLUMNS(
    ///        a INT PATH '$.a' DEFAULT '0' ON EMPTY,
    ///        b INT PATH '$.b' NULL ON ERROR
    ///     )
    /// ) AS jt;
    /// </code>
    /// </summary>
    /// <param name="JsonExpr">The JSON expression to be evaluated. It must evaluate to a json string</param>
    /// <param name="JsonPath">
    /// The path to the array or object to be iterated over.
    /// It must evaluate to a json array or object.
    /// </param>
    /// <param name="Columns">
    /// The columns to be extracted from each element of the array or object.
    /// Each column must have a name and a type.
    /// </param>
    /// <param name="Alias">The alias for the table.</param>
    public sealed record JsonTable(
        Expr JsonExpr,
        ValueWithSpan JsonPath,
        Sequence<JsonTableColumn> Columns,
        TableAlias? Alias) : TableFactor;

    /// <summary>
    /// The MSSQL's <c>OPENJSON</c> table-valued function.
    /// <code>
    /// OPENJSON( jsonExpression [ , path ] )  [ &lt;with_clause&gt; ]
    ///
    /// &lt;with_clause&gt; ::= WITH ( { colName type [ column_path ] [ AS JSON ] } [ ,...n ] )
    /// </code>
    /// Reference: <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/openjson-transact-sql?view=sql-server-ver16#syntax"/>
    /// </summary>
    /// <param name="JsonExpr">The JSON expression to be evaluated. It must evaluate to a json string</param>
    /// <param name="JsonPath">
    /// The path to the array or object to be iterated over.
    /// It must evaluate to a json array or object.
    /// </param>
    /// <param name="Columns">
    /// The columns to be extracted from each element of the array or object.
    /// Each column must have a name and a type.
    /// </param>
    /// <param name="Alias">The alias for the table.</param>
    public sealed record OpenJsonTable(
        Expr JsonExpr,
        ValueWithSpan? JsonPath,
        Sequence<OpenJsonTableColumn> Columns,
        TableAlias? Alias) : TableFactor;

    /// <summary>
    /// Represents a parenthesized table factor. The SQL spec only allows a
    /// join expression (<c>(foo &lt;JOIN&gt; bar [ &lt;JOIN&gt; baz ... ])</c>) to be nested,
    /// possibly several times.
    /// <para>
    /// The parser may also accept non-standard nesting of bare tables for some
    /// dialects, but the information about such nesting is stripped from AST.
    /// </para>
    /// </summary>
    /// <param name="TableWithJoins">The nested join expression contained in parentheses.</param>
    /// <param name="Alias">Optional alias for the nested join.</param>
    public sealed record NestedJoin(TableWithJoins TableWithJoins, TableAlias? Alias) : TableFactor;

    /// <summary>
    /// Represents PIVOT operation on a table.
    /// For example <c>FROM monthly_sales PIVOT(sum(amount) FOR MONTH IN ('JAN', 'FEB'))</c>
    /// <para>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#pivot_operator">BigQuery</see>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constructs/pivot">Snowflake</see>
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/SELECT.html#GUID-CFA006CA-6FF1-4972-821E-6996142A51C6__GUID-68257B27-1C4C-4C47-8140-5C60E0E65D35">Oracle</see>
    /// </para>
    /// </summary>
    /// <param name="Table">The input table to pivot.</param>
    /// <param name="AggregateFunctions">Aggregate expressions used as pivot values (optionally aliased).</param>
    /// <param name="ValueColumn">Columns producing the values to be pivoted.</param>
    /// <param name="ValueSource">Source of pivot values (e.g. list of literals or columns).</param>
    /// <param name="DefaultOnNull">Optional expression providing a default when a pivot produces NULL.</param>
    /// <param name="Alias">Optional alias for the pivoted table.</param>
    public sealed record Pivot(
        TableFactor Table,
        Sequence<ExprWithAlias> AggregateFunctions, // Function expression
        Sequence<Expr> ValueColumn,
        PivotValueSource ValueSource,
        Expr? DefaultOnNull,
        TableAlias? Alias) : TableFactor
    {
        // CS8866: o campo `table` tem o nome da variante irmã `Table`; a propriedade é declarada
        // explicitamente com `new` (decisão do integrador; A7.md, "Decisões").
        /// <summary>The input table to pivot.</summary>
        public new TableFactor Table { get; init; } = Table;
    }

    /// <summary>
    /// An UNPIVOT operation on a table.
    /// <para>Syntax:</para>
    /// <code>
    /// table UNPIVOT [ { INCLUDE | EXCLUDE } NULLS ] (value FOR name IN (column1, [ column2, ... ])) [ alias ]
    /// </code>
    /// <para>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constructs/unpivot">Snowflake</see>
    /// <see href="https://docs.databricks.com/aws/en/sql/language-manual/sql-ref-syntax-qry-select-unpivot">Databricks</see>
    /// <see href="https://docs.cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#unpivot_operator">BigQuery</see>
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/SELECT.html#GUID-CFA006CA-6FF1-4972-821E-6996142A51C6__GUID-9B4E0389-413C-4014-94A1-0A0571BDF7E1">Oracle</see>
    /// </para>
    /// </summary>
    /// <param name="Table">The input table to unpivot.</param>
    /// <param name="Value">Expression producing the unpivoted value.</param>
    /// <param name="Name">Identifier used for the generated column name.</param>
    /// <param name="Columns">Columns or expressions to unpivot, optionally aliased.</param>
    /// <param name="NullInclusion">Whether to include or exclude NULLs during unpivot.</param>
    /// <param name="Alias">Optional alias for the resulting table.</param>
    public sealed record Unpivot(
        TableFactor Table,
        Expr Value,
        Ident Name,
        Sequence<ExprWithAlias> Columns,
        NullInclusion? NullInclusion,
        TableAlias? Alias) : TableFactor
    {
        // CS8866: o campo `table` tem o nome da variante irmã `Table` (ver Pivot).
        /// <summary>The input table to unpivot.</summary>
        public new TableFactor Table { get; init; } = Table;
    }

    /// <summary>
    /// Object unpivoting on a SUPER expression in the FROM clause.
    /// <para>Syntax:</para>
    /// <code>
    /// UNPIVOT expression AS value_alias [AT attribute_alias]
    /// </code>
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/query-super.html#unpivoting">Redshift</see>
    /// </summary>
    /// <param name="Expression">SUPER expression to unpivot.</param>
    /// <param name="ValueAlias">Alias for the generated unpivoted value.</param>
    /// <param name="AttributeAlias">Optional alias for the generated attribute key/index.</param>
    public sealed record UnpivotExpr(Expr Expression, Ident ValueAlias, Ident? AttributeAlias) : TableFactor;

    /// <summary>
    /// A <c>MATCH_RECOGNIZE</c> operation on a table.
    /// <para>See <see href="https://docs.snowflake.com/en/sql-reference/constructs/match_recognize"/>.</para>
    /// </summary>
    /// <param name="Table">The input table to apply <c>MATCH_RECOGNIZE</c> on.</param>
    /// <param name="PartitionBy"><c>PARTITION BY &lt;expr&gt; [, ... ]</c></param>
    /// <param name="OrderBy"><c>ORDER BY &lt;expr&gt; [, ... ]</c></param>
    /// <param name="Measures"><c>MEASURES &lt;expr&gt; [AS] &lt;alias&gt; [, ... ]</c></param>
    /// <param name="RowsPerMatch"><c>ONE ROW PER MATCH | ALL ROWS PER MATCH [ &lt;option&gt; ]</c></param>
    /// <param name="AfterMatchSkip"><c>AFTER MATCH SKIP &lt;option&gt;</c></param>
    /// <param name="Pattern"><c>PATTERN ( &lt;pattern&gt; )</c></param>
    /// <param name="Symbols"><c>DEFINE &lt;symbol&gt; AS &lt;expr&gt; [, ... ]</c></param>
    /// <param name="Alias">The alias for the table.</param>
    public sealed record MatchRecognize(
        TableFactor Table,
        Sequence<Expr> PartitionBy,
        Sequence<OrderByExpr> OrderBy,
        Sequence<Measure> Measures,
        RowsPerMatch? RowsPerMatch,
        AfterMatchSkip? AfterMatchSkip,
        MatchRecognizePattern Pattern,
        Sequence<SymbolDefinition> Symbols,
        TableAlias? Alias) : TableFactor
    {
        // CS8866: o campo `table` tem o nome da variante irmã `Table` (ver Pivot).
        /// <summary>The input table to apply <c>MATCH_RECOGNIZE</c> on.</summary>
        public new TableFactor Table { get; init; } = Table;
    }

    /// <summary>
    /// The <c>XMLTABLE</c> table-valued function.
    /// Part of the SQL standard, supported by PostgreSQL, Oracle, and DB2.
    /// <para><see href="https://www.postgresql.org/docs/15/functions-xml.html#FUNCTIONS-XML-PROCESSING"/></para>
    /// <code>
    /// SELECT xmltable.*
    /// FROM xmldata,
    /// XMLTABLE('//ROWS/ROW'
    ///     PASSING data
    ///     COLUMNS id int PATH '@id',
    ///     ordinality FOR ORDINALITY,
    ///     "COUNTRY_NAME" text,
    ///     country_id text PATH 'COUNTRY_ID',
    ///     size_sq_km float PATH 'SIZE[@unit = "sq_km"]',
    ///     size_other text PATH 'concat(SIZE[@unit!="sq_km"], " ", SIZE[@unit!="sq_km"]/@unit)',
    ///     premier_name text PATH 'PREMIER_NAME' DEFAULT 'not specified'
    /// );
    /// </code>
    /// </summary>
    /// <param name="Namespaces">Optional XMLNAMESPACES clause (empty if not present)</param>
    /// <param name="RowExpression">The row-generating XPath expression.</param>
    /// <param name="Passing">The PASSING clause specifying the document expression.</param>
    /// <param name="Columns">The columns to be extracted from each generated row.</param>
    /// <param name="Alias">The alias for the table.</param>
    public sealed record XmlTable(
        Sequence<XmlNamespaceDefinition> Namespaces,
        Expr RowExpression,
        XmlPassingClause Passing,
        Sequence<XmlTableColumn> Columns,
        TableAlias? Alias) : TableFactor;

    /// <summary>
    /// Snowflake's SEMANTIC_VIEW function for semantic models.
    /// <para><see href="https://docs.snowflake.com/en/sql-reference/constructs/semantic_view"/></para>
    /// <code>
    /// SELECT * FROM SEMANTIC_VIEW(
    ///     tpch_analysis
    ///     DIMENSIONS customer.customer_market_segment
    ///     METRICS orders.order_average_value
    /// );
    /// </code>
    /// </summary>
    /// <param name="Name">The name of the semantic model</param>
    /// <param name="Dimensions">List of dimensions or expression referring to dimensions (e.g. DATE_PART('year', col))</param>
    /// <param name="Metrics">List of metrics (references to objects like orders.value, value, orders.*)</param>
    /// <param name="Facts">List of facts or expressions referring to facts or dimensions.</param>
    /// <param name="WhereClause">WHERE clause for filtering</param>
    /// <param name="Alias">The alias for the table</param>
    public sealed record SemanticView(
        ObjectName Name,
        Sequence<Expr> Dimensions,
        Sequence<Expr> Metrics,
        Sequence<Expr> Facts,
        Expr? WhereClause,
        TableAlias? Alias) : TableFactor;

    // Rust: o `impl fmt::Display for TableFactor` vem depois de `RepetitionQuantifier` no arquivo.
    public void Fmt(Formatter f)
    {
        // PORT-DEVIATION DIV-005: o Rust não marca este método com #[recursive::recursive], mas aqui cada nível
        // de junções entre parênteses (`FROM ((a JOIN b))`, possível com o limite de recursão elevado) gasta KBs
        // de pilha; recebe a mesma proteção do Display de Expr (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            StackGuard.Grow(this, f, static (tableFactor, formatter) => tableFactor.Fmt(formatter));
            return;
        }

        switch (this)
        {
            case Table t:
            {
                t.Name.Fmt(f);
                if (t.JsonPath is { } jsonPath)
                {
                    jsonPath.Fmt(f);
                }
                if (!t.Partitions.IsEmpty)
                {
                    f.Write($" PARTITION ({DisplayCommaSeparated(t.Partitions)})");
                }
                if (t.Args is { } args)
                {
                    f.Write($"(");
                    f.Write($"{DisplayCommaSeparated(args.Args)}");
                    if (args.Settings is { } settings)
                    {
                        if (!args.Args.IsEmpty)
                        {
                            f.Write($", ");
                        }
                        f.Write($"SETTINGS {DisplayCommaSeparated(settings)}");
                    }
                    f.Write($")");
                }
                if (t.WithOrdinality)
                {
                    f.Write($" WITH ORDINALITY");
                }
                if (t.Sample is TableSampleKind.BeforeTableAlias { Value: var sampleBefore })
                {
                    f.Write($" {sampleBefore}");
                }
                if (t.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                if (!t.IndexHints.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(t.IndexHints, " ")}");
                }
                if (!t.WithHints.IsEmpty)
                {
                    f.Write($" WITH ({DisplayCommaSeparated(t.WithHints)})");
                }
                if (t.Version is { } version)
                {
                    f.Write($" {version}");
                }
                if (t.Sample is TableSampleKind.AfterTableAlias { Value: var sampleAfter })
                {
                    f.Write($" {sampleAfter}");
                }
                break;
            }
            case Derived d:
            {
                if (d.Lateral)
                {
                    f.Write($"LATERAL ");
                }
                f.WriteStr("(");
                NewLine.Instance.Fmt(f);
                new Indent<Query>(d.Subquery).Fmt(f);
                NewLine.Instance.Fmt(f);
                f.WriteStr(")");
                if (d.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                if (d.Sample is TableSampleKind.AfterTableAlias { Value: var sample })
                {
                    f.Write($" {sample}");
                }
                break;
            }
            case Function fn:
            {
                if (fn.Lateral)
                {
                    f.Write($"LATERAL ");
                }
                f.Write($"{fn.Name}");
                f.Write($"({DisplayCommaSeparated(fn.Args)})");
                if (fn.WithOrdinality)
                {
                    f.Write($" WITH ORDINALITY");
                }
                if (fn.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case TableFunction tf:
            {
                f.Write($"TABLE({tf.Expr})");
                if (tf.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case UNNEST u:
            {
                f.Write($"UNNEST({DisplayCommaSeparated(u.ArrayExprs)})");

                if (u.WithOrdinality)
                {
                    f.Write($" WITH ORDINALITY");
                }

                if (u.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                if (u.WithOffset)
                {
                    f.Write($" WITH OFFSET");
                }
                if (u.WithOffsetAlias is { } withOffsetAlias)
                {
                    f.Write($" {withOffsetAlias}");
                }
                break;
            }
            case JsonTable jt:
            {
                f.Write($"JSON_TABLE({jt.JsonExpr}, {jt.JsonPath} COLUMNS({DisplayCommaSeparated(jt.Columns)}))");
                if (jt.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case OpenJsonTable ojt:
            {
                f.Write($"OPENJSON({ojt.JsonExpr}");
                if (ojt.JsonPath is { } jsonPath)
                {
                    f.Write($", {jsonPath}");
                }
                f.Write($")");
                if (!ojt.Columns.IsEmpty)
                {
                    f.Write($" WITH ({DisplayCommaSeparated(ojt.Columns)})");
                }
                if (ojt.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case NestedJoin nj:
            {
                f.Write($"({nj.TableWithJoins})");
                if (nj.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case Pivot p:
            {
                f.Write($"{p.Table} PIVOT({DisplayCommaSeparated(p.AggregateFunctions)} FOR ");
                if (p.ValueColumn.Count == 1)
                {
                    f.Write($"{p.ValueColumn[0]}");
                }
                else
                {
                    f.Write($"({DisplayCommaSeparated(p.ValueColumn)})");
                }
                f.Write($" IN ({p.ValueSource})");
                if (p.DefaultOnNull is { } expr)
                {
                    f.Write($" DEFAULT ON NULL ({expr})");
                }
                f.Write($")");
                if (p.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case Unpivot up:
            {
                f.Write($"{up.Table} UNPIVOT");
                if (up.NullInclusion is { } nullInclusion)
                {
                    f.Write($" {nullInclusion} ");
                }
                f.Write($"({up.Value} FOR {up.Name} IN ({DisplayCommaSeparated(up.Columns)}))");
                if (up.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case UnpivotExpr ue:
            {
                f.Write($"UNPIVOT {ue.Expression} AS {ue.ValueAlias}");
                if (ue.AttributeAlias is { } attributeAlias)
                {
                    f.Write($" AT {attributeAlias}");
                }
                break;
            }
            case MatchRecognize mr:
            {
                f.Write($"{mr.Table} MATCH_RECOGNIZE(");
                if (!mr.PartitionBy.IsEmpty)
                {
                    f.Write($"PARTITION BY {DisplayCommaSeparated(mr.PartitionBy)} ");
                }
                if (!mr.OrderBy.IsEmpty)
                {
                    f.Write($"ORDER BY {DisplayCommaSeparated(mr.OrderBy)} ");
                }
                if (!mr.Measures.IsEmpty)
                {
                    f.Write($"MEASURES {DisplayCommaSeparated(mr.Measures)} ");
                }
                if (mr.RowsPerMatch is { } rowsPerMatch)
                {
                    f.Write($"{rowsPerMatch} ");
                }
                if (mr.AfterMatchSkip is { } afterMatchSkip)
                {
                    f.Write($"{afterMatchSkip} ");
                }
                f.Write($"PATTERN ({mr.Pattern}) ");
                f.Write($"DEFINE {DisplayCommaSeparated(mr.Symbols)})");
                if (mr.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case XmlTable xt:
            {
                f.Write($"XMLTABLE(");
                if (!xt.Namespaces.IsEmpty)
                {
                    f.Write($"XMLNAMESPACES({DisplayCommaSeparated(xt.Namespaces)}), ");
                }
                f.Write($"{xt.RowExpression}{xt.Passing} COLUMNS {DisplayCommaSeparated(xt.Columns)})");
                if (xt.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }
                break;
            }
            case SemanticView sv:
            {
                f.Write($"SEMANTIC_VIEW({sv.Name}");

                if (!sv.Dimensions.IsEmpty)
                {
                    f.Write($" DIMENSIONS {DisplayCommaSeparated(sv.Dimensions)}");
                }

                if (!sv.Metrics.IsEmpty)
                {
                    f.Write($" METRICS {DisplayCommaSeparated(sv.Metrics)}");
                }

                if (!sv.Facts.IsEmpty)
                {
                    f.Write($" FACTS {DisplayCommaSeparated(sv.Facts)}");
                }

                if (sv.WhereClause is { } whereClause)
                {
                    f.Write($" WHERE {whereClause}");
                }

                f.Write($")");

                if (sv.Alias is { } alias)
                {
                    f.Write($" {alias}");
                }

                break;
            }
            default:
                throw new InvalidOperationException($"Variante de TableFactor não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
