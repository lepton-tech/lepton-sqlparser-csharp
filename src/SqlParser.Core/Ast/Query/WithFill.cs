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

namespace SqlParser.Core.Ast;

/// <summary>
/// ClickHouse <c>WITH FILL</c> modifier for <c>ORDER BY</c> clause.
/// Supported by <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/order-by#order-by-expr-with-fill-modifier">ClickHouse syntax</see>
/// <c>WITH FILL</c> options for ClickHouse <c>ORDER BY</c> expressions.
/// </summary>
/// <param name="From">Optional lower bound expression for the fill range (<c>FROM &lt;expr&gt;</c>).</param>
/// <param name="To">Optional upper bound expression for the fill range (<c>TO &lt;expr&gt;</c>).</param>
/// <param name="Step">Optional step expression specifying interpolation step (<c>STEP &lt;expr&gt;</c>).</param>
public sealed partial record WithFill(Expr? From, Expr? To, Expr? Step) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"WITH FILL");
        if (From is { } from)
        {
            f.Write($" FROM {from}");
        }
        if (To is { } to)
        {
            f.Write($" TO {to}");
        }
        if (Step is { } step)
        {
            f.Write($" STEP {step}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
