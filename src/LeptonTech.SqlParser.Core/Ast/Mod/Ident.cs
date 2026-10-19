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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>An identifier, decomposed into its value or character data and the quote style.</summary>
/// <param name="Value">The value of the identifier without quotes.</param>
/// <param name="QuoteStyle">
/// The starting quote if any. Valid quote characters are the single quote,
/// double quote, backtick, and opening square bracket.
/// </param>
/// <param name="Span">The span of the identifier in the original SQL string.</param>
public sealed record Ident(string Value, char? QuoteStyle, Span Span) : IDisplay, IComparable<Ident>, IVisit, IVisitMut<Ident>
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

    // Rust: `impl From<&str> for Ident`.
    public static Ident From(string value) => new Ident(value, null, Span.Empty());

    // Rust: `impl PartialEq for Ident`: we ignore spans in comparisons.
    public bool Equals(Ident? other) =>
        other is not null && Value == other.Value && QuoteStyle == other.QuoteStyle;

    // Rust: `impl Hash for Ident`: we ignore spans in hashes.
    public override int GetHashCode()
    {
        unchecked
        {
            return (Value.GetHashCode() * 397) ^ QuoteStyle.GetHashCode();
        }
    }

    // Rust: `impl PartialOrd for Ident` e `impl Ord for Ident`: we ignore spans in ordering.
    public int CompareTo(Ident? other)
    {
        // Convenção do IComparable<T>: qualquer instância vem depois de null (o Rust não tem null).
        if (other is null)
        {
            return 1;
        }

        // let Ident { value, quote_style, span: _ } = self;
        // let Ident { value: other_value, quote_style: other_quote_style, span: _ } = other;
        // (exhaustiveness check; we ignore spans in ordering)

        // First compare by value, then by quote_style
        // `String::cmp` → RustChar.Cmp (ordem dos scalar values); `Option<char>::cmp` (None antes de
        // Some, depois pelo char) → Nullable.Compare.
        var ordering = RustChar.Cmp(Value, other.Value);
        return ordering != 0 ? ordering : Nullable.Compare(QuoteStyle, other.QuoteStyle);
    }

    // Rust: `pub(crate) fn fmt_ident` fica entre `impl From<&str> for Ident` e este `impl Display`;
    // no C#, está em AstModule.cs (convencoes.md §13, caso 135).
    public void Fmt(Formatter f)
    {
        AstModule.FmtIdent(f, Value, QuoteStyle);
    }

    public override string ToString() => Formatter.Format(this);

    // #[cfg(feature = "visitor")] impl Visit for Ident: escrito à mão no Rust (não usa o derive, e por isso
    // não visita `span`). O gerador do visitor detecta esta implementação e chama Ident.VisitMut nos pais.
    public ControlFlow<TBreak> Visit<TBreak>(Visitor<TBreak> visitor)
    {
        var flow = visitor.PreVisitIdent(this);
        if (flow.IsBreak)
        {
            return flow;
        }
        return visitor.PostVisitIdent(this);
    }

    // #[cfg(feature = "visitor")] impl VisitMut for Ident
    public ControlFlow<TBreak> Visit<TBreak>(VisitorMut<TBreak> visitor, out Ident self)
    {
        var node = this;
        var flow = VisitMut(ref node, visitor);
        self = node;
        return flow;
    }

    // Corpo de `impl VisitMut for Ident` sobre a referência do pai (`&mut self`).
    internal static ControlFlow<TBreak> VisitMut<TBreak>(ref Ident node, VisitorMut<TBreak> visitor)
    {
        var flow = visitor.PreVisitIdent(ref node);
        if (flow.IsBreak)
        {
            return flow;
        }
        return visitor.PostVisitIdent(ref node);
    }
}
