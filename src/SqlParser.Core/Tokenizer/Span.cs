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
/// A span represents a linear portion of the input string (start, end)
/// </summary>
/// <remarks>See <c>Spanned</c> (crate::ast::Spanned) for more information.</remarks>
// #[derive(Copy, Eq, Hash, Ord)] → readonly record struct + IComparable (convencoes.md §13 #6).
public readonly partial record struct Span(Location Start, Location End) : IComparable<Span>
{
    /// <summary>Start <c>Location</c> (inclusive).</summary>
    public Location Start { get; init; } = Start;

    /// <summary>End <c>Location</c> (inclusive).</summary>
    public Location End { get; init; } = End;

    /// <summary>Rust: <c>impl fmt::Debug for Span</c> (<c>Span(Location(l,c)..Location(l,c))</c>).</summary>
    public string ToDebugString() => $"Span({Start.ToDebugString()}..{End.ToDebugString()})";

    /// <summary>
    /// <c>Span</c> não tem <c>Display</c> no Rust: o texto é o do <c>Debug</c> (convencoes.md §13).
    /// </summary>
    public override string ToString() => ToDebugString();

    // An empty span (0, 0) -> (0, 0)
    // We need a const instance for pattern matching
    // Rust: `const EMPTY: Span`. Não portado como campo (colidiria com `Empty()`, CS0102):
    // as comparações usam `Empty()` diretamente. Ver docs/port/nomes.md.

    // fn new(start, end) -> Span → o próprio construtor primário (convencoes.md §13 #7).

    /// <summary>
    /// Returns an empty span <c>(0, 0) -&gt; (0, 0)</c>
    /// Empty spans represent no knowledge of source location
    /// See <c>Spanned</c> (crate::ast::Spanned) for more information.
    /// </summary>
    public static Span Empty() => new Span(new Location(0, 0), new Location(0, 0));

    /// <summary>
    /// Returns the smallest Span that contains both <c>self</c> and <c>other</c>
    /// If either span is <see cref="Empty"/>, the other span is returned
    /// </summary>
    /// <example>
    /// <code>
    /// // line 1, column1 -> line 2, column 5
    /// var span1 = new Span(new Location(1, 1), new Location(2, 5));
    /// // line 2, column 3 -> line 3, column 7
    /// var span2 = new Span(new Location(2, 3), new Location(3, 7));
    /// // Union of the two is the min/max of the two spans
    /// // line 1, column 1 -> line 3, column 7
    /// var union = span1.Union(span2);
    /// // union == new Span(new Location(1, 1), new Location(3, 7))
    /// </code>
    /// </example>
    public Span Union(Span other)
    {
        // If either span is empty, return the other
        // this prevents propagating (0, 0) through the tree
        if (this == Empty())
        {
            return other;
        }
        if (other == Empty())
        {
            return this;
        }
        return new Span(
            Start.CompareTo(other.Start) <= 0 ? Start : other.Start, // cmp::min
            End.CompareTo(other.End) >= 0 ? End : other.End);        // cmp::max
    }

    /// <summary>
    /// Same as <see cref="Union"/> for <c>Option&lt;Span&gt;</c>
    /// If <c>other</c> is <c>None</c>, <c>self</c> is returned
    /// </summary>
    public Span UnionOpt(Span? other)
    {
        if (other is { } o)
        {
            return Union(o);
        }
        return this;
    }

    /// <summary>
    /// Return the <see cref="Union"/> of all spans in the iterator
    /// If the iterator is empty, an empty span is returned
    /// </summary>
    /// <example>
    /// <code>
    /// var spans = new List&lt;Span&gt;
    /// {
    ///     new Span(new Location(1, 1), new Location(2, 5)),
    ///     new Span(new Location(2, 3), new Location(3, 7)),
    ///     new Span(new Location(3, 1), new Location(4, 2)),
    /// };
    /// // line 1, column 1 -> line 4, column 2
    /// // Span.UnionIter(spans) == new Span(new Location(1, 1), new Location(4, 2))
    /// </code>
    /// </example>
    public static Span UnionIter(IEnumerable<Span> iter)
    {
        // iter.into_iter().reduce(|acc, item| acc.union(&item)).unwrap_or(Span::empty())
        Span? acc = null;
        foreach (var item in iter)
        {
            acc = acc is { } a ? a.Union(item) : item;
        }
        return acc ?? Empty();
    }

    /// <summary>Ordem do <c>#[derive(Ord)]</c>: <c>start</c>, depois <c>end</c>.</summary>
    public int CompareTo(Span other)
    {
        var c = Start.CompareTo(other.Start);
        return c != 0 ? c : End.CompareTo(other.End);
    }
}
