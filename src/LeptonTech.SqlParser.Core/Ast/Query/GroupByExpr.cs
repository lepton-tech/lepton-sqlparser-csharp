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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Represents the two syntactic forms that <c>GROUP BY</c> can take, including
/// <c>GROUP BY ALL</c> with optional modifiers and ordinary <c>GROUP BY &lt;exprs&gt;</c>.
/// </summary>
[VisitWith("visit_group_by")]
public abstract partial record GroupByExpr : IDisplay
{
    /// <summary>
    /// ALL syntax of [Snowflake], [DuckDB] and [ClickHouse].
    /// <para>
    /// [Snowflake]: <see href="https://docs.snowflake.com/en/sql-reference/constructs/group-by#label-group-by-all-columns"/>
    /// [DuckDB]:  <see href="https://duckdb.org/docs/sql/query_syntax/groupby.html"/>
    /// [ClickHouse]: <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/group-by#group-by-all"/>
    /// </para>
    /// <para>ClickHouse also supports WITH modifiers after GROUP BY ALL and expressions.</para>
    /// <para>[ClickHouse]: <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/group-by#rollup-modifier"/></para>
    /// </summary>
    public sealed record All(Sequence<GroupByWithModifier> Value) : GroupByExpr;

    /// <summary><c>GROUP BY &lt;expressions&gt;</c> with optional modifiers.</summary>
    // Exprs: Rust: campo `.0` (nomes.md).
    // Modifiers: Rust: campo `.1` (nomes.md).
    public sealed record Expressions(Sequence<Expr> Exprs, Sequence<GroupByWithModifier> Modifiers) : GroupByExpr;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case All { Value: var modifiers }:
                f.Write($"GROUP BY ALL");
                if (!modifiers.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(modifiers, " ")}");
                }
                break;
            case Expressions { Exprs: var colNames, Modifiers: var modifiers }:
                f.WriteStr("GROUP BY");
                SpaceOrNewline.Instance.Fmt(f);
                new Indent<DisplaySeparated<Expr>>(DisplayCommaSeparated(colNames)).Fmt(f);
                if (!modifiers.IsEmpty)
                {
                    f.Write($" {DisplaySeparated(modifiers, " ")}");
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de GroupByExpr não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
