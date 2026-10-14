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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/comments.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>An opaque container for comments from a parse SQL source code.</summary>
public sealed class Comments
{
    // Rust: campo `.0` do tuple struct (Vec<CommentWithSpan>), privado.
    private readonly List<CommentWithSpan> _value = new();

    /// <summary>
    /// Accepts <c>comment</c> if its the first or is located strictly after the
    /// last accepted comment.  In other words, this method will skip the
    /// comment if its comming out of order (as encountered in the parsed
    /// source code.)
    /// </summary>
    public void Offer(CommentWithSpan comment)
    {
        if (_value.Count == 0 || _value[_value.Count - 1].Span.CompareTo(comment.Span) < 0)
        {
            _value.Add(comment);
        }
    }

    /// <summary>
    /// Finds comments starting within the given location range. The order of
    /// iterator reflects the order of the comments as encountered in the parsed
    /// source code.
    /// </summary>
    /// <remarks>
    /// The Spanned trait allows you to access location information for certain AST nodes.
    /// <para>Faixas do Rust (<c>..x</c>, <c>a..b</c>...) → <see cref="RangeBounds"/> (convencoes.md §13, caso 57).</para>
    /// </remarks>
    public IEnumerable<CommentWithSpan> Find(RangeBounds<Location> range)
    {
        var start = StartIndex(range.StartBound);
        var end = EndIndex(range.EndBound);
        // in case the user specified a reverse range
        if (start <= end)
        {
            for (var i = start; i < end; i++)
            {
                yield return _value[i];
            }
        }
    }

    /// <summary>Find the index of the first comment starting "before" the given location.</summary>
    /// <remarks>The returned index is _inclusive_ and within the range of <c>0..=self.0.len()</c>.</remarks>
    private int StartIndex(Bound<Location> location)
    {
        switch (location)
        {
            case Bound<Location>.Included included:
            {
                var (_, i) = BinarySearchByStart(included.Value);
                return i; // Ok(i) => i, Err(i) => i
            }
            case Bound<Location>.Excluded excluded:
            {
                var (found, i) = BinarySearchByStart(excluded.Value);
                return found ? i + 1 : i; // Ok(i) => i + 1, Err(i) => i
            }
            case Bound<Location>.Unbounded:
                return 0;
            default:
                throw new InvalidOperationException($"Variante de Bound não tratada: {location.GetType().Name}");
        }
    }

    /// <summary>Find the index of the first comment starting "after" the given location.</summary>
    /// <remarks>The returned index is _exclusive_ and within the range of <c>0..=self.0.len()</c>.</remarks>
    private int EndIndex(Bound<Location> location)
    {
        switch (location)
        {
            case Bound<Location>.Included included:
            {
                var (found, i) = BinarySearchByStart(included.Value);
                return found ? i + 1 : i; // Ok(i) => i + 1, Err(i) => i
            }
            case Bound<Location>.Excluded excluded:
            {
                var (_, i) = BinarySearchByStart(excluded.Value);
                return i; // Ok(i) => i, Err(i) => i
            }
            case Bound<Location>.Unbounded:
                return _value.Count;
            default:
                throw new InvalidOperationException($"Variante de Bound não tratada: {location.GetType().Name}");
        }
    }

    // Rust: `self.0.binary_search_by(|c| c.span.start.cmp(location))` → Ok(i) = (true, i); Err(i) = (false, i).
    // Os inícios dos comentários são estritamente crescentes (ver Offer), então não há empates.
    private (bool Found, int Index) BinarySearchByStart(Location location)
    {
        var lo = 0;
        var hi = _value.Count;
        while (lo < hi)
        {
            var mid = lo + (hi - lo) / 2;
            var cmp = _value[mid].Span.Start.CompareTo(location);
            if (cmp == 0)
            {
                return (true, mid);
            }
            if (cmp < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }
        return (false, lo);
    }

    /// <summary>
    /// Rust: <c>impl From&lt;Comments&gt; for Vec&lt;CommentWithSpan&gt;</c>. O destino é um tipo da biblioteca
    /// padrão, então a conversão vira método de instância (convencoes.md §13, caso 57).
    /// </summary>
    public List<CommentWithSpan> ToVec() => new(_value);
}

/// <summary>A source code comment with information of its entire span.</summary>
/// <param name="Comment">The source code comment iself</param>
/// <param name="Span">The span of the comment including its markers</param>
/// <remarks><c>impl Deref for CommentWithSpan</c> (→ <c>Comment</c>) não é traduzido: use <see cref="Comment"/> (caso 34).</remarks>
public sealed partial record CommentWithSpan(Comment Comment, Span Span);

/// <summary>A unified type of the different source code comment formats.</summary>
public abstract record Comment
{
    /// <summary>
    /// A single-line comment, typically introduced with a prefix and spanning
    /// until end-of-line or end-of-file in the source code.
    /// Note: <c>content</c> will include the terminating new-line character, if any.
    /// </summary>
    /// <param name="Content">The content of the comment (including trailing newline, if any).</param>
    /// <param name="Prefix">The prefix introducing the comment (e.g. <c>--</c>, <c>#</c>).</param>
    public sealed record SingleLine(string Content, string Prefix) : Comment;

    /// <summary>
    /// A multi-line comment, typically enclosed in <c>/* .. */</c> markers. The
    /// string represents the content excluding the markers.
    /// </summary>
    public sealed record MultiLine(string Value) : Comment;

    /// <summary>Retrieves the content of the comment as string slice.</summary>
    /// <remarks><c>impl Deref for Comment</c> (→ <c>str</c>) não é traduzido: use este método (caso 34).</remarks>
    public string AsStr() =>
        this switch
        {
            SingleLine s => s.Content,
            MultiLine m => m.Value,
            _ => throw new InvalidOperationException($"Variante de Comment não tratada: {GetType().Name}"),
        };
}
