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

namespace SqlParser.Core;

/// <summary>
/// Location in input string
/// </summary>
/// <remarks>
/// Create an "empty" (unknown) <c>Location</c>: <c>Location.Empty()</c>.
/// Create a <c>Location</c> from a line and column: <c>new Location(1, 1)</c>.
/// Create a <c>Location</c> from a pair: <c>Location.From((1, 1))</c>.
/// </remarks>
// #[derive(Copy, Eq, Hash, Ord)] → readonly record struct + IComparable (ordem lexicográfica
// dos campos, como o derive do Rust). Ver convencoes.md §13 #6.
public readonly record struct Location(long Line, long Column) : IDisplay, IComparable<Location>
{
    /// <summary>
    /// Line number, starting from 1.
    /// Note: Line 0 is used for empty spans
    /// </summary>
    public long Line { get; init; } = Line;

    /// <summary>
    /// Line column, starting from 1.
    /// Note: Column 0 is used for empty spans
    /// </summary>
    public long Column { get; init; } = Column;

    /// <inheritdoc/>
    public void Fmt(Formatter f)
    {
        if (Line == 0)
        {
            return;
        }
        f.WriteStr(FormattableString.Invariant($" at Line: {Line}, Column: {Column}"));
    }

    /// <summary>Rust: <c>impl fmt::Display for Location</c>.</summary>
    public override string ToString() => Formatter.Format(this);

    /// <summary>Rust: <c>impl fmt::Debug for Location</c> (<c>Location(line,column)</c>).</summary>
    public string ToDebugString() => FormattableString.Invariant($"Location({Line},{Column})");

    /// <summary>Return an "empty" / unknown location</summary>
    public static Location Empty() => new Location(0, 0);

    // fn new(line, column) -> Self → o próprio construtor primário (convencoes.md §13 #7).

    /// <summary>
    /// Create a new location for a given line and column
    /// Alias for <c>new Location(line, column)</c>
    /// </summary>
    // TODO: remove / deprecate in favor of` `new` for consistency?
    public static Location Of(long line, long column) => new Location(line, column);

    /// <summary>Combine self and <c>end</c> into a new <see cref="Span"/></summary>
    public Span SpanTo(Location end) => new Span(this, end);

    /// <summary>Rust: <c>impl From&lt;(u64, u64)&gt; for Location</c>.</summary>
    public static Location From((long Line, long Column) value) => new Location(value.Line, value.Column);

    /// <summary>Ordem do <c>#[derive(Ord)]</c>: <c>line</c>, depois <c>column</c>.</summary>
    public int CompareTo(Location other)
    {
        var c = Line.CompareTo(other.Line);
        return c != 0 ? c : Column.CompareTo(other.Column);
    }
}
