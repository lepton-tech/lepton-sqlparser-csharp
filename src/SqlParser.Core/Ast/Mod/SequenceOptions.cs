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

/// <summary>
/// Can use to describe options in create sequence or table column type identity
/// <code>
/// [ INCREMENT [ BY ] increment ]
///     [ MINVALUE minvalue | NO MINVALUE ] [ MAXVALUE maxvalue | NO MAXVALUE ]
///     [ START [ WITH ] start ] [ CACHE cache ] [ [ NO ] CYCLE ]
/// </code>
/// </summary>
public abstract record SequenceOptions : IDisplay
{
    /// <summary><c>INCREMENT [BY] &lt;expr&gt;</c> option; second value indicates presence of <c>BY</c> keyword.</summary>
    /// <param name="Increment">Rust: campo <c>.0</c> (nome registrado em nomes.md).</param>
    /// <param name="By">Rust: campo <c>.1</c> (nome registrado em nomes.md).</param>
    public sealed record IncrementBy(Expr Increment, bool By) : SequenceOptions;

    /// <summary><c>MINVALUE &lt;expr&gt;</c> or <c>NO MINVALUE</c>.</summary>
    public sealed record MinValue(Expr? Value) : SequenceOptions;

    /// <summary><c>MAXVALUE &lt;expr&gt;</c> or <c>NO MAXVALUE</c>.</summary>
    public sealed record MaxValue(Expr? Value) : SequenceOptions;

    /// <summary><c>START [WITH] &lt;expr&gt;</c>; second value indicates presence of <c>WITH</c>.</summary>
    /// <param name="Start">Rust: campo <c>.0</c> (nome registrado em nomes.md).</param>
    /// <param name="With">Rust: campo <c>.1</c> (nome registrado em nomes.md).</param>
    public sealed record StartWith(Expr Start, bool With) : SequenceOptions;

    /// <summary><c>CACHE &lt;expr&gt;</c> option.</summary>
    public sealed record Cache(Expr Value) : SequenceOptions;

    /// <summary><c>CYCLE</c> or <c>NO CYCLE</c> option.</summary>
    public sealed record Cycle(bool Value) : SequenceOptions;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case IncrementBy incrementBy:
                f.Write($" INCREMENT{(incrementBy.By ? " BY" : "")} {incrementBy.Increment}");
                break;
            case MinValue { Value: { } expr }:
                f.Write($" MINVALUE {expr}");
                break;
            case MinValue { Value: null }:
                f.Write($" NO MINVALUE");
                break;
            case MaxValue { Value: { } expr }:
                f.Write($" MAXVALUE {expr}");
                break;
            case MaxValue { Value: null }:
                f.Write($" NO MAXVALUE");
                break;
            case StartWith startWith:
                f.Write($" START{(startWith.With ? " WITH" : "")} {startWith.Start}");
                break;
            case Cache cache:
                f.Write($" CACHE {cache.Value}");
                break;
            case Cycle cycle:
                f.Write($" {(cycle.Value ? "NO " : "")}CYCLE");
                break;
            default:
                throw new InvalidOperationException($"Variante de SequenceOptions não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
