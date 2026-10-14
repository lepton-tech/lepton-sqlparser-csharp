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
/// A restricted variant of <c>SELECT</c> (without CTEs/<c>ORDER BY</c>), which may
/// appear either as the only body item of a <c>Query</c>, or as an operand
/// to a set operation like <c>UNION</c>.
/// </summary>
/// <param name="SelectToken">Token for the <c>SELECT</c> keyword</param>
/// <param name="OptimizerHints">
/// Query optimizer hints
/// <para>
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/optimizer-hints.html">MySQL</see>
/// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Comments.html#GUID-D316D545-89E2-4D54-977F-FC97815CD62E">Oracle</see>
/// </para>
/// </param>
/// <param name="Distinct"><c>SELECT [DISTINCT] ...</c></param>
/// <param name="SelectModifiers">
/// MySQL-specific SELECT modifiers.
/// See <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html">MySQL SELECT</see>.
/// </param>
/// <param name="Top">MSSQL syntax: <c>TOP (&lt;N&gt;) [ PERCENT ] [ WITH TIES ]</c></param>
/// <param name="TopBeforeDistinct">Whether the top was located before <c>ALL</c>/<c>DISTINCT</c></param>
/// <param name="Projection">projection expressions</param>
/// <param name="Exclude">
/// Excluded columns from the projection expression which are not specified
/// directly after a wildcard.
/// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_EXCLUDE_list.html">Redshift</see>
/// </param>
/// <param name="Into">INTO</param>
/// <param name="From">FROM</param>
/// <param name="LateralViews">LATERAL VIEWs</param>
/// <param name="Prewhere">
/// ClickHouse syntax: <c>PREWHERE a = 1 WHERE b = 2</c>,
/// and it can be used together with WHERE selection.
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/prewhere">ClickHouse</see>
/// </param>
/// <param name="Selection">WHERE</param>
/// <param name="ConnectBy">[START WITH ..] CONNECT BY ..</param>
/// <param name="GroupBy">GROUP BY</param>
/// <param name="ClusterBy">CLUSTER BY (Hive)</param>
/// <param name="DistributeBy">DISTRIBUTE BY (Hive)</param>
/// <param name="SortBy">SORT BY (Hive)</param>
/// <param name="Having">HAVING</param>
/// <param name="NamedWindow">WINDOW AS</param>
/// <param name="Qualify">QUALIFY (Snowflake)</param>
/// <param name="WindowBeforeQualify">
/// The positioning of QUALIFY and WINDOW clauses differ between dialects.
/// e.g. BigQuery requires that WINDOW comes after QUALIFY, while DUCKDB accepts
/// WINDOW before QUALIFY.
/// We accept either positioning and flag the accepted variant.
/// </param>
/// <param name="ValueTableMode">BigQuery syntax: <c>SELECT AS VALUE | SELECT AS STRUCT</c></param>
/// <param name="Flavor">Was this a FROM-first query?</param>
[VisitWith("visit_select")]
public sealed partial record Select(
    AttachedToken SelectToken,
    Sequence<OptimizerHint> OptimizerHints,
    Distinct? Distinct,
    SelectModifiers? SelectModifiers,
    Top? Top,
    bool TopBeforeDistinct,
    Sequence<SelectItem> Projection,
    ExcludeSelectItem? Exclude,
    SelectInto? Into,
    Sequence<TableWithJoins> From,
    Sequence<LateralView> LateralViews,
    Expr? Prewhere,
    Expr? Selection,
    Sequence<ConnectByKind> ConnectBy,
    GroupByExpr GroupBy,
    Sequence<Expr> ClusterBy,
    Sequence<Expr> DistributeBy,
    Sequence<OrderByExpr> SortBy,
    Expr? Having,
    Sequence<NamedWindowDefinition> NamedWindow,
    Expr? Qualify,
    bool WindowBeforeQualify,
    ValueTableMode? ValueTableMode,
    SelectFlavor Flavor) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (Flavor)
        {
            case SelectFlavor.Standard:
                f.Write($"SELECT");
                break;
            case SelectFlavor.FromFirst:
                f.Write($"FROM {DisplayCommaSeparated(From)} SELECT");
                break;
            case SelectFlavor.FromFirstNoSelect:
                f.Write($"FROM {DisplayCommaSeparated(From)}");
                break;
            default:
                throw new InvalidOperationException($"Variante de SelectFlavor não tratada: {Flavor}");
        }

        foreach (var hint in OptimizerHints)
        {
            f.WriteStr(" ");
            hint.Fmt(f);
        }

        if (ValueTableMode is { } valueTableMode)
        {
            f.WriteStr(" ");
            valueTableMode.Fmt(f);
        }

        if (Top is { } top)
        {
            if (TopBeforeDistinct)
            {
                f.WriteStr(" ");
                top.Fmt(f);
            }
        }
        if (Distinct is { } distinct)
        {
            f.WriteStr(" ");
            distinct.Fmt(f);
        }
        if (Top is { } top2)
        {
            if (!TopBeforeDistinct)
            {
                f.WriteStr(" ");
                top2.Fmt(f);
            }
        }

        if (SelectModifiers is { } selectModifiers)
        {
            selectModifiers.Fmt(f);
        }

        if (!Projection.IsEmpty)
        {
            DisplayUtils.IndentedList(f, Projection);
        }

        if (Exclude is { } exclude)
        {
            f.Write($" {exclude}");
        }

        if (Into is { } into)
        {
            f.WriteStr(" ");
            into.Fmt(f);
        }

        if (Flavor == SelectFlavor.Standard && !From.IsEmpty)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("FROM");
            DisplayUtils.IndentedList(f, From);
        }
        if (!LateralViews.IsEmpty)
        {
            foreach (var lv in LateralViews)
            {
                lv.Fmt(f);
            }
        }
        if (Prewhere is { } prewhere)
        {
            f.WriteStr(" PREWHERE ");
            prewhere.Fmt(f);
        }
        if (Selection is { } selection)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("WHERE");
            SpaceOrNewline.Instance.Fmt(f);
            new Indent<Expr>(selection).Fmt(f);
        }
        foreach (var clause in ConnectBy)
        {
            SpaceOrNewline.Instance.Fmt(f);
            clause.Fmt(f);
        }
        switch (GroupBy)
        {
            case GroupByExpr.All:
                SpaceOrNewline.Instance.Fmt(f);
                GroupBy.Fmt(f);
                break;
            case GroupByExpr.Expressions { Exprs: var exprs }:
                if (!exprs.IsEmpty)
                {
                    SpaceOrNewline.Instance.Fmt(f);
                    GroupBy.Fmt(f);
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de GroupByExpr não tratada: {GroupBy.GetType().Name}");
        }
        if (!ClusterBy.IsEmpty)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("CLUSTER BY");
            SpaceOrNewline.Instance.Fmt(f);
            new Indent<DisplaySeparated<Expr>>(DisplayCommaSeparated(ClusterBy)).Fmt(f);
        }
        if (!DistributeBy.IsEmpty)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("DISTRIBUTE BY");
            SpaceOrNewline.Instance.Fmt(f);
            DisplayCommaSeparated(DistributeBy).Fmt(f);
        }
        if (!SortBy.IsEmpty)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("SORT BY");
            SpaceOrNewline.Instance.Fmt(f);
            new Indent<DisplaySeparated<OrderByExpr>>(DisplayCommaSeparated(SortBy)).Fmt(f);
        }
        if (Having is { } having)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("HAVING");
            SpaceOrNewline.Instance.Fmt(f);
            new Indent<Expr>(having).Fmt(f);
        }
        if (WindowBeforeQualify)
        {
            if (!NamedWindow.IsEmpty)
            {
                SpaceOrNewline.Instance.Fmt(f);
                f.WriteStr("WINDOW");
                SpaceOrNewline.Instance.Fmt(f);
                DisplayCommaSeparated(NamedWindow).Fmt(f);
            }
            if (Qualify is { } qualify)
            {
                SpaceOrNewline.Instance.Fmt(f);
                f.WriteStr("QUALIFY");
                SpaceOrNewline.Instance.Fmt(f);
                qualify.Fmt(f);
            }
        }
        else
        {
            if (Qualify is { } qualify)
            {
                SpaceOrNewline.Instance.Fmt(f);
                f.WriteStr("QUALIFY");
                SpaceOrNewline.Instance.Fmt(f);
                qualify.Fmt(f);
            }
            if (!NamedWindow.IsEmpty)
            {
                SpaceOrNewline.Instance.Fmt(f);
                f.WriteStr("WINDOW");
                SpaceOrNewline.Instance.Fmt(f);
                DisplayCommaSeparated(NamedWindow).Fmt(f);
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
