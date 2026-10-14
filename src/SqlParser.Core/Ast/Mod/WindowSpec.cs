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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>A window specification (i.e. <c>OVER ([window_name] PARTITION BY .. ORDER BY .. etc.)</c>)</summary>
/// <param name="WindowName">
/// Optional window name.
/// <para>You can find it at least in [MySQL][1], [BigQuery][2], [PostgreSQL][3]</para>
/// <para>[1]: https://dev.mysql.com/doc/refman/8.0/en/window-functions-named-windows.html</para>
/// <para>[2]: https://cloud.google.com/bigquery/docs/reference/standard-sql/window-function-calls</para>
/// <para>[3]: https://www.postgresql.org/docs/current/sql-expressions.html#SYNTAX-WINDOW-FUNCTIONS</para>
/// </param>
/// <param name="PartitionBy"><c>OVER (PARTITION BY ...)</c></param>
/// <param name="OrderBy"><c>OVER (ORDER BY ...)</c></param>
/// <param name="WindowFrame"><c>OVER (window frame)</c></param>
public sealed partial record WindowSpec(
    Ident? WindowName,
    Sequence<Expr> PartitionBy,
    Sequence<OrderByExpr> OrderBy,
    WindowFrame? WindowFrame) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var isFirst = true;
        if (WindowName is { } windowName)
        {
            if (!isFirst)
            {
                SpaceOrNewline.Instance.Fmt(f);
            }
            isFirst = false;
            f.Write($"{windowName}");
        }
        if (!PartitionBy.IsEmpty)
        {
            if (!isFirst)
            {
                SpaceOrNewline.Instance.Fmt(f);
            }
            isFirst = false;
            f.Write($"PARTITION BY {DisplayCommaSeparated(PartitionBy)}");
        }
        if (!OrderBy.IsEmpty)
        {
            if (!isFirst)
            {
                SpaceOrNewline.Instance.Fmt(f);
            }
            isFirst = false;
            f.Write($"ORDER BY {DisplayCommaSeparated(OrderBy)}");
        }
        if (WindowFrame is { } windowFrame)
        {
            if (!isFirst)
            {
                SpaceOrNewline.Instance.Fmt(f);
            }
            if (windowFrame.EndBound is { } endBound)
            {
                f.Write($"{windowFrame.Units} BETWEEN {windowFrame.StartBound} AND {endBound}");
            }
            else
            {
                f.Write($"{windowFrame.Units} {windowFrame.StartBound}");
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
