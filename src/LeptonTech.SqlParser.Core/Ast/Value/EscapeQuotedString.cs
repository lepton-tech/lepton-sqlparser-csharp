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

namespace LeptonTech.SqlParser.Core.Ast;

// Rust: `pub struct EscapeQuotedString<'a>`, criado por `Value.EscapeQuotedString`.
// Os campos do Rust são privados (`string: &'a str`, `quote: char`): campos privados e
// construtor `internal` (convencoes.md §13, casos 20 e 25). O `quote` é o scalar value (`int`),
// porque o `char` do Rust pode estar fora do BMP (caso 18).
public sealed class EscapeQuotedString : IDisplay
{
    private readonly string _string;
    private readonly int _quote;

    internal EscapeQuotedString(string @string, int quote)
    {
        _string = @string;
        _quote = quote;
    }

    public void Fmt(Formatter f)
    {
        // EscapeQuotedString doesn't know which mode of escape was
        // chosen by the user. So this code must to correctly display
        // strings without knowing if the strings are already escaped
        // or not.
        //
        // If the quote symbol in the string is repeated twice, OR, if
        // the quote symbol is after backslash, display all the chars
        // without any escape. However, if the quote symbol is used
        // just between usual chars, `fmt()` should display it twice."
        //
        // The following table has examples
        //
        // | original query | mode      | AST Node                                           | serialized   |
        // | -------------  | --------- | -------------------------------------------------- | ------------ |
        // | `"A""B""A"`    | no-escape | `DoubleQuotedString(String::from("A\"\"B\"\"A"))`  | `"A""B""A"`  |
        // | `"A""B""A"`    | default   | `DoubleQuotedString(String::from("A\"B\"A"))`      | `"A""B""A"`  |
        // | `"A\"B\"A"`    | no-escape | `DoubleQuotedString(String::from("A\\\"B\\\"A"))`  | `"A\"B\"A"`  |
        // | `"A\"B\"A"`    | default   | `DoubleQuotedString(String::from("A\"B\"A"))`      | `"A""B""A"`  |
        //
        // Port: `char_indices().peekable()` percorre scalar values; aqui `pos` é o índice (em unidades
        // UTF-16) do próximo scalar e `ScalarAt` junta pares substitutos, como o `char` do Rust.
        var s = _string;
        var quote = _quote;
        var previousChar = 0; // char::default()
        var startIdx = 0;
        var pos = 0;
        while (pos < s.Length)
        {
            var idx = pos;
            var ch = ScalarAt(s, idx, out var chLen);
            if (ch == quote)
            {
                if (previousChar == '\\')
                {
                    // the quote is already escaped with a backslash, skip
                    pos += chLen;
                    continue;
                }
                pos += chLen;
                if (pos < s.Length && ScalarAt(s, pos, out var nextLen) == quote)
                {
                    // the quote is already escaped with another quote, skip
                    pos += nextLen;
                }
                else
                {
                    // The quote is not escaped.
                    // Including idx in the range, so the quote at idx will be printed twice:
                    // in this call to write_str() and in the next one.
                    var endIdx = idx + chLen;
                    f.WriteStr(s.Substring(startIdx, endIdx - startIdx));
                    startIdx = idx;
                }
            }
            else
            {
                pos += chLen;
            }
            previousChar = ch;
        }
        f.WriteStr(s.Substring(startIdx));
    }

    public override string ToString() => Formatter.Format(this);

    // Scalar value em `index` (par substituto combinado) e quantas unidades UTF-16 ele ocupa.
    private static int ScalarAt(string s, int index, out int length)
    {
        var c = s[index];
        if (char.IsHighSurrogate(c) && index + 1 < s.Length && char.IsLowSurrogate(s[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(c, s[index + 1]);
        }
        length = 1;
        return c;
    }
}
