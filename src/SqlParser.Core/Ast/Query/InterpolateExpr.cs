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
/// ClickHouse <c>INTERPOLATE</c> clause for use in <c>ORDER BY</c> clause when using <c>WITH FILL</c> modifier.
/// Supported by <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/order-by#order-by-expr-with-fill-modifier">ClickHouse syntax</see>
/// An expression used by <c>WITH FILL</c>/<c>INTERPOLATE</c> to specify interpolation for a column.
/// </summary>
/// <param name="Column">The column to interpolate.</param>
/// <param name="Expr">Optional <c>AS &lt;expr&gt;</c> expression specifying how to compute interpolated values.</param>
public sealed partial record InterpolateExpr(Ident Column, Expr? Expr) : IDisplay
{
    // Rust: o `impl fmt::Display for InterpolateExpr` vem depois de `Interpolate` no arquivo.
    public void Fmt(Formatter f)
    {
        f.Write($"{Column}");
        if (Expr is { } expr)
        {
            f.Write($" AS {expr}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
