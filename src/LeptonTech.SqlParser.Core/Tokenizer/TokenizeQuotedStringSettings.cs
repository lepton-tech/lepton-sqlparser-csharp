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

namespace LeptonTech.SqlParser.Core;

/// <summary>Settings for tokenizing a quoted string literal.</summary>
/// <param name="QuoteStyle">The character used to quote the string.</param>
/// <param name="NumQuoteChars">Represents how many quotes characters enclose the string literal.</param>
/// <param name="NumOpeningQuotesToConsume">
/// The number of opening quotes left to consume, before parsing
/// the remaining string literal.
/// For example: given initial string <c>"""abc"""</c>. If the caller has
/// already parsed the first quote for some reason, then this value
/// is set to 1, flagging to look to consume only 2 leading quotes.
/// </param>
/// <param name="BackslashEscape">
/// True if the string uses backslash escaping of special characters
/// e.g <c>'abc\ndef\'ghi'</c>
/// </param>
// Privado do módulo `tokenizer` no Rust → internal (convencoes.md §13).
internal sealed record TokenizeQuotedStringSettings(
    char QuoteStyle,
    NumStringQuoteChars NumQuoteChars,
    byte NumOpeningQuotesToConsume,
    bool BackslashEscape);
