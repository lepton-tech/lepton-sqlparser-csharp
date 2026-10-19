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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A function call</summary>
/// <param name="Name">The function name (may be qualified).</param>
/// <param name="UsesOdbcSyntax">
/// Flags whether this function call uses the
/// <see href="https://learn.microsoft.com/en-us/sql/odbc/reference/develop-app/scalar-function-calls?view=sql-server-2017">ODBC syntax</see>.
/// <para>Example:</para>
/// <code>
/// SELECT {fn CONCAT('foo', 'bar')}
/// </code>
/// </param>
/// <param name="Parameters">
/// The parameters to the function, including any options specified within the
/// delimiting parentheses.
/// <para>Example:</para>
/// <code>
/// HISTOGRAM(0.5, 0.6)(x, y)
/// </code>
/// <see href="https://clickhouse.com/docs/en/sql-reference/aggregate-functions/parametric-functions">ClickHouse</see>
/// </param>
/// <param name="Args">
/// The arguments to the function, including any options specified within the
/// delimiting parentheses.
/// </param>
/// <param name="WithinGroup">
/// A clause used with certain aggregate functions to control the ordering
/// within grouped sets before the function is applied.
/// <para>Syntax:</para>
/// <code>
/// &lt;aggregate_function&gt;(expression) WITHIN GROUP (ORDER BY key [ASC | DESC], ...)
/// </code>
/// </param>
/// <param name="Filter">e.g. <c>x &gt; 5</c> in <c>COUNT(x) FILTER (WHERE x &gt; 5)</c></param>
/// <param name="NullTreatment">
/// Indicates how <c>NULL</c>s should be handled in the calculation.
/// <para>Example:</para>
/// <code>
/// FIRST_VALUE( &lt;expr&gt; ) [ { IGNORE | RESPECT } NULLS ] OVER ...
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/functions/first_value">Snowflake</see>
/// </param>
/// <param name="Over">The <c>OVER</c> clause, indicating a window function call.</param>
public sealed partial record Function(
    ObjectName Name,
    bool UsesOdbcSyntax,
    FunctionArguments Parameters,
    FunctionArguments Args,
    Sequence<OrderByExpr> WithinGroup,
    Expr? Filter,
    NullTreatment? NullTreatment,
    WindowType? Over) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (UsesOdbcSyntax)
        {
            f.Write($"{{fn ");
        }

        f.Write($"{Name}{Parameters}{Args}");

        if (!WithinGroup.IsEmpty)
        {
            f.Write($" WITHIN GROUP (ORDER BY {DisplayCommaSeparated(WithinGroup)})");
        }

        if (Filter is { } filterCond)
        {
            f.Write($" FILTER (WHERE {filterCond})");
        }

        if (NullTreatment is { } nullTreatment)
        {
            f.Write($" {nullTreatment}");
        }

        if (Over is { } o)
        {
            f.WriteStr(" OVER ");
            o.Fmt(f);
        }

        if (UsesOdbcSyntax)
        {
            f.Write($"}}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
