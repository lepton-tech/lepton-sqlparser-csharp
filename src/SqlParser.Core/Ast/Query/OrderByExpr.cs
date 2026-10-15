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

/// <summary>An <c>ORDER BY</c> expression</summary>
/// <param name="Expr">The expression to order by.</param>
/// <param name="Options">Ordering options such as <c>ASC</c>/<c>DESC</c>/<c>USING &lt;operator&gt;</c> and <c>NULLS</c> behavior.</param>
/// <param name="WithFill">Optional <c>WITH FILL</c> clause (ClickHouse extension) which specifies how to fill gaps.</param>
[VisitWith("visit_order_by_expr")]
public sealed partial record OrderByExpr(Expr Expr, OrderByOptions Options, WithFill? WithFill) : IDisplay
{
    // Rust: `impl From<Ident> for OrderByExpr`.
    public static OrderByExpr From(Ident ident) =>
        new OrderByExpr(new Expr.Identifier(ident), OrderByOptions.Default(), null);

    public void Fmt(Formatter f)
    {
        f.Write($"{Expr}");
        f.Write($"{Options}");
        if (WithFill is { } withFill)
        {
            f.Write($" {withFill}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
