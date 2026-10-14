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

/// <summary>Specifies <c>WindowFrame</c>'s <c>start_bound</c> and <c>end_bound</c></summary>
public abstract partial record WindowFrameBound : IDisplay
{
    /// <summary><c>CURRENT ROW</c></summary>
    public sealed record CurrentRow : WindowFrameBound;

    /// <summary><c>&lt;N&gt; PRECEDING</c> or <c>UNBOUNDED PRECEDING</c></summary>
    /// <param name="Value">Rust: campo <c>.0</c> (<c>None</c> = <c>UNBOUNDED</c>).</param>
    public sealed record Preceding(Expr? Value) : WindowFrameBound;

    /// <summary><c>&lt;N&gt; FOLLOWING</c> or <c>UNBOUNDED FOLLOWING</c>.</summary>
    /// <param name="Value">Rust: campo <c>.0</c> (<c>None</c> = <c>UNBOUNDED</c>).</param>
    public sealed record Following(Expr? Value) : WindowFrameBound;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case CurrentRow:
                f.WriteStr("CURRENT ROW");
                break;
            case Preceding { Value: null }:
                f.WriteStr("UNBOUNDED PRECEDING");
                break;
            case Following { Value: null }:
                f.WriteStr("UNBOUNDED FOLLOWING");
                break;
            case Preceding { Value: { } n }:
                f.Write($"{n} PRECEDING");
                break;
            case Following { Value: { } n }:
                f.Write($"{n} FOLLOWING");
                break;
            default:
                throw new InvalidOperationException($"Variante de WindowFrameBound não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
