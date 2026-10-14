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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/attached_token.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// A wrapper over <see cref="TokenWithSpan"/>s that ignores the token and source
/// location in comparisons and hashing.
/// </summary>
/// <remarks>
/// This type is used when the token and location is not relevant for semantics,
/// but is still needed for accurate source location tracking, for example, in
/// the nodes in the ast module.
/// <para>Note: <b>All</b> <c>AttachedTokens</c> are equal.</para>
/// <para>
/// Por isso, um record que contém um <see cref="AttachedToken"/> ignora esse campo na igualdade
/// sintetizada, exatamente como o <c>#[derive(PartialEq)]</c> do Rust.
/// </para>
/// </remarks>
public sealed record AttachedToken(TokenWithSpan Value)
{
    /// <summary>Return a new Empty AttachedToken</summary>
    public static AttachedToken Empty() => new AttachedToken(TokenWithSpan.NewEof());

    /// <summary>Rust: <c>impl PartialEq for AttachedToken</c> (sempre igual).</summary>
    public bool Equals(AttachedToken? other) => other is not null;

    /// <summary>Rust: <c>impl Hash for AttachedToken</c> (não contribui para o hash).</summary>
    public override int GetHashCode() => 0;

    /// <summary>Rust: <c>impl From&lt;TokenWithSpan&gt; for AttachedToken</c>.</summary>
    public static AttachedToken From(TokenWithSpan value) => new AttachedToken(value);

    // `impl From<AttachedToken> for TokenWithSpan` → use `.Value` (convencoes.md §13, caso 7).

    /// <summary>Rust: <c>impl Debug for AttachedToken</c> (delega ao token).</summary>
    public override string ToString() => Value.ToString();
}
