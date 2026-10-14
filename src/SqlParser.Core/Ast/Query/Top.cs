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

/// <summary>MSSQL <c>TOP</c> clause options.</summary>
/// <param name="WithTies">
/// SQL semantic equivalent of LIMIT but with same structure as FETCH.
/// MSSQL only.
/// </param>
/// <param name="Percent">Apply <c>PERCENT</c> extension.</param>
/// <param name="Quantity">The optional quantity (expression or constant) following <c>TOP</c>.</param>
public sealed partial record Top(bool WithTies, bool Percent, TopQuantity? Quantity) : IDisplay
{
    // Rust: o `impl fmt::Display for Top` vem depois de `TopQuantity` no arquivo.
    public void Fmt(Formatter f)
    {
        var extension = WithTies ? " WITH TIES" : "";
        if (Quantity is { } quantity)
        {
            var percent = Percent ? " PERCENT" : "";
            switch (quantity)
            {
                case TopQuantity.Expr { Value: var expr }:
                    f.Write($"TOP ({expr}){percent}{extension}");
                    break;
                case TopQuantity.Constant { Value: var constant }:
                    f.Write($"TOP {constant}{percent}{extension}");
                    break;
                default:
                    throw new InvalidOperationException($"Variante de TopQuantity não tratada: {quantity.GetType().Name}");
            }
        }
        else
        {
            f.Write($"TOP{extension}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
