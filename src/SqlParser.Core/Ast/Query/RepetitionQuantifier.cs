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
/// Determines the minimum and maximum allowed occurrences of a pattern in a
/// <c>MATCH_RECOGNIZE</c> operation.
/// </summary>
public abstract record RepetitionQuantifier : IDisplay
{
    /// <summary><c>*</c></summary>
    public sealed record ZeroOrMore : RepetitionQuantifier;

    /// <summary><c>+</c></summary>
    public sealed record OneOrMore : RepetitionQuantifier;

    /// <summary><c>?</c></summary>
    public sealed record AtMostOne : RepetitionQuantifier;

    /// <summary><c>{n}</c></summary>
    public sealed record Exactly(uint Value) : RepetitionQuantifier;

    /// <summary><c>{n,}</c></summary>
    public sealed record AtLeast(uint Value) : RepetitionQuantifier;

    /// <summary><c>{,n}</c></summary>
    public sealed record AtMost(uint Value) : RepetitionQuantifier;

    /// <summary>`{n,m}</summary>
    /// <param name="Min">Rust: campo <c>.0</c> (o <c>n</c>; nomes.md).</param>
    /// <param name="Max">Rust: campo <c>.1</c> (o <c>m</c>; nomes.md).</param>
    public sealed record Range(uint Min, uint Max) : RepetitionQuantifier;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case ZeroOrMore:
                f.Write($"*");
                break;
            case OneOrMore:
                f.Write($"+");
                break;
            case AtMostOne:
                f.Write($"?");
                break;
            case Exactly { Value: var n }:
                f.Write($"{{{n}}}");
                break;
            case AtLeast { Value: var n }:
                f.Write($"{{{n},}}");
                break;
            case AtMost { Value: var n }:
                f.Write($"{{,{n}}}");
                break;
            case Range { Min: var n, Max: var m }:
                f.Write($"{{{n},{m}}}");
                break;
            default:
                throw new InvalidOperationException($"Variante de RepetitionQuantifier não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
