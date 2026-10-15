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

using System.Text;

namespace SqlParser.Core;

// Rust: `struct Unescape`, o "desescapador" de `E'...'`.
// Os `Option<char>` dos métodos viram `int?` com o scalar value Unicode, porque
// `\U0010FFFF` produz um `char` Rust fora do BMP (convencoes.md §13).
// Privado do módulo `tokenizer` no Rust → `internal`.
internal sealed class Unescape
{
    private readonly State _chars;

    public Unescape(State chars)
    {
        _chars = chars;
    }

    // Rust: `fn unescape(mut self)`. Renomeado: um membro não pode ter o nome do tipo (CS0542).
    // Ver docs/port/nomes.md.
    public string? DoUnescape()
    {
        var unescaped = new StringBuilder();

        _chars.Next();

        while (_chars.Next() is { } c)
        {
            if (c == '\'')
            {
                // case: ''''
                if (_chars.Peek() == '\'')
                {
                    _chars.Next();
                    unescaped.Append('\'');
                    continue;
                }
                return unescaped.ToString();
            }

            if (c != '\\')
            {
                unescaped.Append(c);
                continue;
            }

            if (_chars.Next() is not { } escaped)
            {
                return null;
            }

            int? scalar;
            switch (escaped)
            {
                case 'b':
                    scalar = '\u0008';
                    break;
                case 'f':
                    scalar = '\u000C';
                    break;
                case 'n':
                    scalar = '\n';
                    break;
                case 'r':
                    scalar = '\r';
                    break;
                case 't':
                    scalar = '\t';
                    break;
                case 'u':
                    scalar = UnescapeUnicode16();
                    break;
                case 'U':
                    scalar = UnescapeUnicode32();
                    break;
                case 'x':
                    scalar = UnescapeHex();
                    break;
                case var d when IsOctalDigit(d):
                    scalar = UnescapeOctal(d);
                    break;
                default:
                    scalar = escaped;
                    break;
            }

            // `?` sobre os Option<char> acima
            if (scalar is not { } value)
            {
                return null;
            }

            if (CheckNull(value) is not { } nonNull)
            {
                return null;
            }
            RustChar.PushScalar(unescaped, nonNull);
        }

        return null;
    }

    private static int? CheckNull(int c)
    {
        if (c == '\0')
        {
            return null;
        }
        return c;
    }

    // Rust: `fn byte_to_char<const RADIX: u32>(s: &str)` → o parâmetro genérico constante vira argumento.
    private static int? ByteToChar(string s, uint radix)
    {
        // u32 is used here because Pg has an overflow operation rather than throwing an exception directly.
        if (RustChar.U32FromStrRadix(s, radix) is not { } n)
        {
            return null;
        }
        n &= 0xFF;
        if (n <= 127)
        {
            return RustChar.FromU32(n);
        }
        return null;
    }

    // Hexadecimal byte value. \xh, \xhh (h = 0–9, A–F)
    private int? UnescapeHex()
    {
        var s = new StringBuilder();

        for (var i = 0; i < 2; i++)
        {
            if (NextHexDigit() is { } c)
            {
                s.Append(c);
            }
            else
            {
                break;
            }
        }

        if (s.Length == 0)
        {
            return 'x';
        }

        return ByteToChar(s.ToString(), 16);
    }

    private char? NextHexDigit()
    {
        if (_chars.Peek() is { } c && RustChar.IsAsciiHexdigit(c))
        {
            return _chars.Next();
        }
        return null;
    }

    // Octal byte value. \o, \oo, \ooo (o = 0–7)
    private int? UnescapeOctal(char c)
    {
        var s = new StringBuilder();

        s.Append(c);
        for (var i = 0; i < 2; i++)
        {
            if (NextOctalDigest() is { } d)
            {
                s.Append(d);
            }
            else
            {
                break;
            }
        }

        return ByteToChar(s.ToString(), 8);
    }

    private char? NextOctalDigest()
    {
        if (_chars.Peek() is { } c && IsOctalDigit(c))
        {
            return _chars.Next();
        }
        return null;
    }

    // 16-bit hexadecimal Unicode character value. \uxxxx (x = 0–9, A–F)
    private int? UnescapeUnicode16() => UnescapeUnicode(4);

    // 32-bit hexadecimal Unicode character value. \Uxxxxxxxx (x = 0–9, A–F)
    private int? UnescapeUnicode32() => UnescapeUnicode(8);

    // Rust: `fn unescape_unicode<const NUM: usize>` → o parâmetro genérico constante vira argumento.
    private int? UnescapeUnicode(int num)
    {
        var s = new StringBuilder();
        for (var i = 0; i < num; i++)
        {
            if (_chars.Next() is not { } c)
            {
                return null;
            }
            s.Append(c);
        }
        if (RustChar.U32FromStrRadix(s.ToString(), 16) is not { } n)
        {
            return null;
        }
        return RustChar.FromU32(n);
    }

    // Rust: `c.is_digit(8)`.
    private static bool IsOctalDigit(char c) => c >= '0' && c <= '7';
}
