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

using System.Globalization;

namespace SqlParser.Core.Ast;

/// <summary>Rust: <c>pub struct EscapeUnicodeStringLiteral&lt;'a&gt;(&amp;'a str)</c>, criado por <c>Value.EscapeUnicodeString</c>.</summary>
/// <remarks>O campo <c>.0</c> do Rust é privado: campo <c>_value</c> e construtor <c>internal</c> (convencoes.md §13, casos 20, 25 e 35).</remarks>
public sealed class EscapeUnicodeStringLiteral : IDisplay
{
    private readonly string _value;

    internal EscapeUnicodeStringLiteral(string value) => _value = value;

    public void Fmt(Formatter f)
    {
        // `self.0.chars()` percorre scalar values: um par substituto vira um único code point
        // (necessário para o formato \+XXXXXX).
        var s = _value;
        var i = 0;
        while (i < s.Length)
        {
            var unit = s[i];
            int codepoint;
            if (char.IsHighSurrogate(unit) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                codepoint = char.ConvertToUtf32(unit, s[i + 1]);
                i += 2;
            }
            else
            {
                codepoint = unit;
                i += 1;
            }

            switch (codepoint)
            {
                case '\'':
                    f.Write($"''");
                    break;
                case '\\':
                    f.WriteStr(@"\\");
                    break;
                case <= 0x7F: // x if x.is_ascii()
                    f.Write($"{(char)codepoint}");
                    break;
                default:
                    // if the character fits in 32 bits, we can use the \XXXX format
                    // otherwise, we need to use the \+XXXXXX format
                    if (codepoint <= 0xFFFF)
                    {
                        f.WriteStr(@"\" + codepoint.ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        f.WriteStr(@"\+" + codepoint.ToString("X6", CultureInfo.InvariantCulture));
                    }
                    break;
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
