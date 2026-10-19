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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/tokenizer.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core;

// Rust: `#[deprecated] pub type TokenWithLocation = TokenWithSpan;` não é portado: C# não exporta
// aliases de tipo e o alias está obsoleto desde a 0.53.0 (docs/port/nomes.md).

/// <summary>
/// A <see cref="Core.Token"/> with <see cref="Core.Span"/> attached to it
/// </summary>
/// <remarks>
/// This is used to track the location of a token in the input string.
/// A <c>Token</c> together with its <c>Span</c> (location in the source).
/// </remarks>
/// <example>
/// <code>
/// // commas @ line 1, column 10
/// var tok1 = new TokenWithSpan(
///   new Token.Comma(),
///   new Span(new Location(1, 10), new Location(1, 11)));
/// // tok1.Equals(new Token.Comma()) == true: can compare the token
///
/// // commas @ line 2, column 20
/// var tok2 = new TokenWithSpan(
///   new Token.Comma(),
///   new Span(new Location(2, 20), new Location(2, 21)));
/// // same token but different locations are not equal
/// // tok1 != tok2
/// </code>
/// </example>
/// <param name="Token">The token value.</param>
/// <param name="Span">The span covering the token in the input.</param>
public sealed partial record TokenWithSpan(Token Token, Span Span) : IDisplay
{
    // fn new(token, span) -> Self → o próprio construtor primário (convencoes.md §13 #7).

    /// <summary>Wrap a token with an empty span</summary>
    public static TokenWithSpan Wrap(Token token) => new TokenWithSpan(token, Core.Span.Empty());

    /// <summary>Wrap a token with a location from <c>start</c> to <c>end</c></summary>
    public static TokenWithSpan At(Token token, Location start, Location end) =>
        new TokenWithSpan(token, new Span(start, end));

    /// <summary>Return an EOF token with no location</summary>
    public static TokenWithSpan NewEof() => Wrap(new Token.EOF());

    // Rust: `impl PartialEq<Token> for TokenWithSpan` (compara só o token).
    public bool Equals(Token? other) => other is not null && Token == other;

    /// <inheritdoc/>
    public void Fmt(Formatter f) => Token.Fmt(f);

    // Rust: `impl fmt::Display for TokenWithSpan`.
    public override string ToString() => Formatter.Format(this);
}
