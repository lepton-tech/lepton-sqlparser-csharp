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

/// <summary>The source of values in a <c>PIVOT</c> operation.</summary>
public abstract partial record PivotValueSource : IDisplay
{
    /// <summary>
    /// Pivot on a static list of values.
    /// <para>See <see href="https://docs.snowflake.com/en/sql-reference/constructs/pivot#pivot-on-a-specified-list-of-column-values-for-the-pivot-column"/>.</para>
    /// </summary>
    public sealed record List(Sequence<ExprWithAlias> Value) : PivotValueSource;

    /// <summary>
    /// Pivot on all distinct values of the pivot column.
    /// <para>See <see href="https://docs.snowflake.com/en/sql-reference/constructs/pivot#pivot-on-all-distinct-column-values-automatically-with-dynamic-pivot"/>.</para>
    /// </summary>
    public sealed record Any(Sequence<OrderByExpr> Value) : PivotValueSource;

    /// <summary>
    /// Pivot on all values returned by a subquery.
    /// <para>See <see href="https://docs.snowflake.com/en/sql-reference/constructs/pivot#pivot-on-column-values-using-a-subquery-with-dynamic-pivot"/>.</para>
    /// </summary>
    public sealed record Subquery(Query Value) : PivotValueSource;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case List { Value: var values }:
                f.Write($"{DisplayCommaSeparated(values)}");
                break;
            case Any { Value: var orderBy }:
                f.Write($"ANY");
                if (!orderBy.IsEmpty)
                {
                    f.Write($" ORDER BY {DisplayCommaSeparated(orderBy)}");
                }
                break;
            case Subquery { Value: var query }:
                f.Write($"{query}");
                break;
            default:
                throw new InvalidOperationException($"Variante de PivotValueSource não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
