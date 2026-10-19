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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>The pattern in a <c>MATCH_RECOGNIZE</c> operation.</summary>
/// <remarks>
/// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/match_recognize#pattern-specifying-the-pattern-to-match"/>.
/// </remarks>
public abstract partial record MatchRecognizePattern : IDisplay
{
    /// <summary>A named symbol such as <c>S1</c> or a virtual symbol such as <c>^</c>.</summary>
    public sealed record Symbol(MatchRecognizeSymbol Value) : MatchRecognizePattern;

    /// <summary>{- symbol -}</summary>
    public sealed record Exclude(MatchRecognizeSymbol Value) : MatchRecognizePattern;

    /// <summary>PERMUTE(symbol_1, ..., symbol_n)</summary>
    public sealed record Permute(Sequence<MatchRecognizeSymbol> Value) : MatchRecognizePattern;

    /// <summary>pattern_1 pattern_2 ... pattern_n</summary>
    public sealed record Concat(Sequence<MatchRecognizePattern> Value) : MatchRecognizePattern;

    /// <summary>( pattern )</summary>
    public sealed record Group(MatchRecognizePattern Value) : MatchRecognizePattern;

    /// <summary>pattern_1 | pattern_2 | ... | pattern_n</summary>
    public sealed record Alternation(Sequence<MatchRecognizePattern> Value) : MatchRecognizePattern;

    /// <summary>e.g. pattern*</summary>
    // Pattern: Rust: campo `.0` (nomes.md).
    // Quantifier: Rust: campo `.1` (nomes.md).
    public sealed record Repetition(MatchRecognizePattern Pattern, RepetitionQuantifier Quantifier) : MatchRecognizePattern;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Symbol { Value: var symbol }:
                f.Write($"{symbol}");
                break;
            case Exclude { Value: var symbol }:
                f.Write($"{{- {symbol} -}}");
                break;
            case Permute { Value: var symbols }:
                f.Write($"PERMUTE({DisplayCommaSeparated(symbols)})");
                break;
            case Concat { Value: var patterns }:
                f.Write($"{DisplaySeparated(patterns, " ")}");
                break;
            case Group { Value: var pattern }:
                f.Write($"( {pattern} )");
                break;
            case Alternation { Value: var patterns }:
                f.Write($"{DisplaySeparated(patterns, " | ")}");
                break;
            case Repetition { Pattern: var pattern, Quantifier: var op }:
                f.Write($"{pattern}{op}");
                break;
            default:
                throw new InvalidOperationException($"Variante de MatchRecognizePattern não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
