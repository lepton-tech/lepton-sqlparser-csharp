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

/// <summary>A quote delimited string literal, e.g. <c>Q'_abc_'</c>.</summary>
/// <remarks>
/// See <c>Value::QuoteDelimitedStringLiteral</c> and/or
/// <c>Value::NationalQuoteDelimitedStringLiteral</c>.
/// </remarks>
/// <param name="StartQuote">the quote start character; i.e. the character _after_ the opening <c>Q'</c></param>
/// <param name="Value">the string literal value itself</param>
/// <param name="EndQuote">the quote end character; i.e. the character _before_ the closing <c>'</c></param>
public sealed partial record QuoteDelimitedString(char StartQuote, string Value, char EndQuote) : IDisplay
{
    /// <inheritdoc/>
    public void Fmt(Formatter f)
    {
        // write!(f, "Q'{}{}{}'", self.start_quote, self.value, self.end_quote)
        f.WriteStr("Q'");
        f.WriteChar(StartQuote);
        f.WriteStr(Value);
        f.WriteChar(EndQuote);
        f.WriteStr("'");
    }

    /// <summary>Rust: <c>impl fmt::Display for QuoteDelimitedString</c>.</summary>
    public override string ToString() => Formatter.Format(this);
}
