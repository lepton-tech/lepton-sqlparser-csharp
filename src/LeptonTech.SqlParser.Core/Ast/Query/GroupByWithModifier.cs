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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// ClickHouse supports GROUP BY WITH modifiers(includes ROLLUP|CUBE|TOTALS).
/// e.g. GROUP BY year WITH ROLLUP WITH TOTALS
/// <para>[ClickHouse]: <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/group-by#rollup-modifier"/></para>
/// Modifiers used with <c>GROUP BY</c> such as <c>WITH ROLLUP</c> or <c>WITH CUBE</c>.
/// </summary>
public abstract partial record GroupByWithModifier : IDisplay
{
    /// <summary><c>WITH ROLLUP</c> modifier.</summary>
    public sealed record Rollup : GroupByWithModifier;

    /// <summary><c>WITH CUBE</c> modifier.</summary>
    public sealed record Cube : GroupByWithModifier;

    /// <summary><c>WITH TOTALS</c> modifier (ClickHouse).</summary>
    public sealed record Totals : GroupByWithModifier;

    /// <summary>
    /// Hive supports GROUPING SETS syntax, e.g. <c>GROUP BY GROUPING SETS(...)</c>.
    /// <para>[Hive]: <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=30151323#EnhancedAggregation,Cube,GroupingandRollup-GROUPINGSETSclause"/></para>
    /// </summary>
    public sealed record GroupingSets(Expr Value) : GroupByWithModifier;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Rollup:
                f.Write($"WITH ROLLUP");
                break;
            case Cube:
                f.Write($"WITH CUBE");
                break;
            case Totals:
                f.Write($"WITH TOTALS");
                break;
            case GroupingSets { Value: var expr }:
                f.Write($"{expr}");
                break;
            default:
                throw new InvalidOperationException($"Variante de GroupByWithModifier não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
