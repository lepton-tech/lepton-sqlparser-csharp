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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/value.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// Wraps a primitive SQL <c>Value</c> with its <c>Span</c> location
/// </summary>
/// <remarks>
/// <para>Example: create a <c>ValueWithSpan</c> from a <c>Value</c></para>
/// <code>
/// let value = Value::SingleQuotedString(String::from("endpoint"));
/// // from line 1, column 1 to line 1, column 7
/// let span = Span::new(Location::new(1, 1), Location::new(1, 7));
/// let value_with_span = value.with_span(span);
/// </code>
/// <para>Example: create a <c>ValueWithSpan</c> from a <c>Value</c> with an empty span</para>
/// <para>You can call <c>Value::with_empty_span</c> to create a <c>ValueWithSpan</c> with an empty span</para>
/// <code>
/// let value = Value::SingleQuotedString(String::from("endpoint"));
/// let value_with_span = value.with_empty_span();
/// assert_eq!(value_with_span.span, Span::empty());
/// </code>
/// <para>You can also use the <c>From</c> trait to convert <c>ValueWithSpan</c> to/from <c>Value</c>s</para>
/// <code>
/// let value = Value::SingleQuotedString(String::from("endpoint"));
/// // converting `Value` to `ValueWithSpan` results in an empty span
/// let value_with_span: ValueWithSpan = value.into();
/// assert_eq!(value_with_span.span, Span::empty());
/// // convert back to `Value`
/// let value: Value = value_with_span.into();
/// </code>
/// <para>A <c>Value</c> paired with its source <c>Span</c> location.</para>
/// </remarks>
/// <param name="Value">The wrapped <c>Value</c>.</param>
/// <param name="Span">The source <c>Span</c> covering the token(s) that produced the value.</param>
[VisitWith("visit_value")]
public sealed partial record ValueWithSpan(Value Value, Span Span) : IDisplay
{
    /// <summary>Rust: <c>impl PartialEq for ValueWithSpan</c> (compara só o valor, ignora o span).</summary>
    public bool Equals(ValueWithSpan? other) => other is not null && Value == other.Value;

    // `impl Ord`/`impl PartialOrd for ValueWithSpan` (delegam à ordem derivada de `Value`): não
    // portados, como os `#[derive(PartialOrd, Ord)]` (convencoes.md §13, caso 23).

    /// <summary>Rust: <c>impl Hash for ValueWithSpan</c> (só o valor contribui para o hash).</summary>
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>Rust: <c>impl From&lt;Value&gt; for ValueWithSpan</c>.</summary>
    public static ValueWithSpan From(Value value) => value.WithEmptySpan();

    // `impl From<ValueWithSpan> for Value` → `Value.From(ValueWithSpan)` (Value.cs).
    // `impl Deref/DerefMut for ValueWithSpan` → não traduzido: use `.Value` (convencoes.md §13, caso 34).

    /// <summary>If the underlying literal is a string, regardless of quote style, returns the associated string value</summary>
    public string? IntoString() => Value.IntoString();

    public void Fmt(Formatter f)
    {
        f.Write($"{Value}");
    }

    public override string ToString() => Formatter.Format(this);
}
