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

/// <summary>Options for an <c>ORDER BY</c> expression.</summary>
/// <param name="Sort">Optional sort order: <c>ASC</c>, <c>DESC</c>, or <c>USING &lt;operator&gt;</c>.</param>
/// <param name="NullsFirst">Optional <c>NULLS FIRST</c> (<c>Some(true)</c>) or <c>NULLS LAST</c> (<c>Some(false)</c>).</param>
public sealed partial record OrderByOptions(OrderBySort? Sort, bool? NullsFirst) : IDisplay
{
    /// <summary>Rust: <c>#[derive(Default)]</c> (convencoes.md §13, caso 42).</summary>
    public static OrderByOptions Default() => new OrderByOptions(Sort: null, NullsFirst: null);

    public void Fmt(Formatter f)
    {
        switch (Sort)
        {
            case OrderBySort.Asc:
                f.Write($" ASC");
                break;
            case OrderBySort.Desc:
                f.Write($" DESC");
                break;
            case OrderBySort.Using { Value: var op }:
                if (op.Value.Count > 1)
                {
                    f.Write($" USING OPERATOR({op})");
                }
                else
                {
                    f.Write($" USING {op}");
                }
                break;
            case null:
                break;
            default:
                throw new InvalidOperationException($"Variante de OrderBySort não tratada: {Sort.GetType().Name}");
        }
        switch (NullsFirst)
        {
            case true:
                f.Write($" NULLS FIRST");
                break;
            case false:
                f.Write($" NULLS LAST");
                break;
            case null:
                break;
        }
    }

    public override string ToString() => Formatter.Format(this);
}
