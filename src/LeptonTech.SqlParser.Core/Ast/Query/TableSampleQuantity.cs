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

/// <summary>Quantity for a <c>TABLESAMPLE</c> clause (e.g. <c>10 PERCENT</c> or <c>(10)</c>).</summary>
/// <param name="Parenthesized">Whether the quantity was wrapped in parentheses.</param>
/// <param name="Value">The numeric expression specifying the quantity.</param>
/// <param name="Unit">Optional unit (e.g. <c>PERCENT</c>, <c>ROWS</c>).</param>
public sealed partial record TableSampleQuantity(bool Parenthesized, Expr Value, TableSampleUnit? Unit) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (Parenthesized)
        {
            f.Write($"(");
        }
        f.Write($"{Value}");
        if (Unit is { } unit)
        {
            f.Write($" {unit}");
        }
        if (Parenthesized)
        {
            f.Write($")");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
