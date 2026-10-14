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

/// <summary>An identifier, decomposed into its value or character data and the quote style.</summary>
/// <param name="Value">The value of the identifier without quotes.</param>
/// <param name="QuoteStyle">
/// The starting quote if any. Valid quote characters are the single quote,
/// double quote, backtick, and opening square bracket.
/// </param>
/// <param name="Span">The span of the identifier in the original SQL string.</param>
public sealed record Ident(string Value, char? QuoteStyle, Span Span) : IDisplay
{
    /// <summary>Create a new identifier with the given value and no quotes and an empty span.</summary>
    public Ident(string value)
        : this(value, null, Span.Empty())
    {
    }

    /// <summary>
    /// Create a new quoted identifier with the given quote and value. This function
    /// panics if the given quote is not a valid quote character.
    /// </summary>
    public static Ident WithQuote(char quote, string value)
    {
        if (!(quote == '\'' || quote == '"' || quote == '`' || quote == '['))
        {
            throw new InvalidOperationException("assertion failed: quote == '\\'' || quote == '\"' || quote == '`' || quote == '['");
        }
        return new Ident(value, quote, Span.Empty());
    }

    /// <summary>Create an <c>Ident</c> with the given <c>span</c> and <c>value</c> (unquoted).</summary>
    public static Ident WithSpan(Span span, string value) => new Ident(value, null, span);

    /// <summary>Create a quoted <c>Ident</c> with the given <c>quote</c> and <c>span</c>.</summary>
    public static Ident WithQuoteAndSpan(char quote, Span span, string value)
    {
        if (!(quote == '\'' || quote == '"' || quote == '`' || quote == '['))
        {
            throw new InvalidOperationException("assertion failed: quote == '\\'' || quote == '\"' || quote == '`' || quote == '['");
        }
        return new Ident(value, quote, span);
    }

    /// <summary>Rust: <c>impl From&lt;&amp;str&gt; for Ident</c>.</summary>
    public static Ident From(string value) => new Ident(value, null, Span.Empty());

    /// <summary>Rust: <c>impl PartialEq for Ident</c>: we ignore spans in comparisons.</summary>
    public bool Equals(Ident? other) =>
        other is not null && Value == other.Value && QuoteStyle == other.QuoteStyle;

    /// <summary>Rust: <c>impl Hash for Ident</c>: we ignore spans in hashes.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            return (Value.GetHashCode() * 397) ^ QuoteStyle.GetHashCode();
        }
    }

    public void Fmt(Formatter f)
    {
        switch (QuoteStyle)
        {
            case { } q when q == '"' || q == '\'' || q == '`':
            {
                var escaped = Ast.Value.EscapeQuotedString(Value, q);
                f.Write($"{q}{escaped}{q}");
                break;
            }
            case '[':
                f.Write($"[{Value}]");
                break;
            case null:
                f.WriteStr(Value);
                break;
            default:
                throw new InvalidOperationException("unexpected quote style");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // PORT-TODO: `impl Visit/VisitMut for Ident` (pre/post_visit_ident) entra com o visitor (ast/visitor.rs).
}
