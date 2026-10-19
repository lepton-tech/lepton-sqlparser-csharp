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

/// <summary>Represents an <c>ORDER BY</c> clause with its kind and optional <c>INTERPOLATE</c>.</summary>
/// <param name="Kind">The kind of ordering (expressions or <c>ALL</c>).</param>
/// <param name="Interpolate">Optional <c>INTERPOLATE</c> clause (ClickHouse extension).</param>
[VisitWith("visit_order_by")]
public sealed partial record OrderBy(OrderByKind Kind, Interpolate? Interpolate) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"ORDER BY");
        switch (Kind)
        {
            case OrderByKind.Expressions { Value: var exprs }:
                f.Write($" {DisplayCommaSeparated(exprs)}");
                break;
            case OrderByKind.All { Value: var all }:
                f.Write($" ALL{all}");
                break;
            default:
                throw new InvalidOperationException($"Variante de OrderByKind não tratada: {Kind.GetType().Name}");
        }

        if (Interpolate is { } interpolate)
        {
            switch (interpolate.Exprs)
            {
                case { } exprs:
                    f.Write($" INTERPOLATE ({DisplayCommaSeparated(exprs)})");
                    break;
                case null:
                    f.Write($" INTERPOLATE");
                    break;
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
