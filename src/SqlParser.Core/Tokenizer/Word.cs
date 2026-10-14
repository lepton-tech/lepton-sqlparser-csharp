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

/// <summary>A keyword (like SELECT) or an optionally quoted SQL identifier</summary>
/// <param name="Value">
/// The value of the token, without the enclosing quotes, and with the
/// escape sequences (if any) processed (TODO: escapes are not handled)
/// </param>
/// <param name="QuoteStyle">
/// An identifier can be "quoted" (&lt;delimited identifier&gt; in ANSI parlance).
/// The standard and most implementations allow using double quotes for this,
/// but some implementations support other quoting styles as well (e.g. [MS SQL])
/// </param>
/// <param name="Keyword">
/// If the word was not quoted and it matched one of the known keywords,
/// this will have one of the values from dialect::keywords, otherwise empty
/// </param>
public sealed record Word(string Value, char? QuoteStyle, Keyword Keyword) : IDisplay
{
    /// <inheritdoc/>
    public void Fmt(Formatter f)
    {
        switch (QuoteStyle)
        {
            case char s when s == '"' || s == '[' || s == '`':
                f.WriteChar(s);
                f.WriteStr(Value);
                f.WriteChar(MatchingEndQuote(s));
                break;
            case null:
                f.WriteStr(Value);
                break;
            default:
                throw new InvalidOperationException("Unexpected quote_style!");
        }
    }

    /// <summary>Rust: <c>impl fmt::Display for Word</c>.</summary>
    public override string ToString() => Formatter.Format(this);

    // Rust: `fn matching_end_quote` é privado do módulo `tokenizer` → internal (convencoes.md §13).
    internal static char MatchingEndQuote(char ch)
    {
        switch (ch)
        {
            case '"': return '"'; // ANSI and most dialects
            case '[': return ']'; // MS SQL
            case '`': return '`'; // MySQL
            default: throw new InvalidOperationException("unexpected quoting style!");
        }
    }
}
