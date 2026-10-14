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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>The contents inside the <c>[</c> and <c>]</c> in a subscript expression.</summary>
public abstract partial record Subscript : IDisplay
{
    /// <summary>Accesses the element of the array at the given index.</summary>
    /// <param name="IndexValue">
    /// The index expression used to access the array element.
    /// (Rust: campo <c>index</c>; renomeado porque teria o nome da variante, CS0542; convencoes.md §13, caso 41.)
    /// </param>
    public sealed record Index(Expr IndexValue) : Subscript;

    /// <summary>
    /// Accesses a slice of an array on PostgreSQL, e.g.
    /// <code>
    /// =&gt; select (array[1,2,3,4,5,6])[2:5];
    /// -----------
    /// {2,3,4,5}
    /// </code>
    /// The lower and/or upper bound can be omitted to slice from the start or
    /// end of the array respectively.
    /// <para>See <see href="https://www.postgresql.org/docs/current/arrays.html#ARRAYS-ACCESSING"/>.</para>
    /// <para>
    /// Also supports an optional "stride" as the last element (this is not
    /// supported by postgres), e.g.
    /// </para>
    /// <code>
    /// =&gt; select (array[1,2,3,4,5,6])[1:6:2];
    /// -----------
    /// {1,3,5}
    /// </code>
    /// </summary>
    /// <param name="LowerBound">Optional lower bound for the slice (inclusive).</param>
    /// <param name="UpperBound">Optional upper bound for the slice (inclusive).</param>
    /// <param name="Stride">Optional stride for the slice (step size).</param>
    public sealed record Slice(Expr? LowerBound, Expr? UpperBound, Expr? Stride) : Subscript;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Index { IndexValue: var index }:
                f.Write($"{index}");
                break;
            case Slice slice:
            {
                if (slice.LowerBound is { } lower)
                {
                    f.Write($"{lower}");
                }
                f.Write($":");
                if (slice.UpperBound is { } upper)
                {
                    f.Write($"{upper}");
                }
                if (slice.Stride is { } stride)
                {
                    f.Write($":");
                    f.Write($"{stride}");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de Subscript não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
