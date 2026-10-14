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
/// Rust: <c>struct State { peekable: Peekable&lt;Chars&gt;, line, col }</c>, o cursor do tokenizer.
/// </summary>
/// <remarks>
/// <see cref="Col"/> avança <b>uma vez por scalar value</b> Unicode, como no Rust: a segunda metade
/// de um par substituto não incrementa a coluna (convencoes.md §10 e §13).
/// Equivalências: <c>chars.next()</c> → <see cref="Next"/>; <c>chars.peek()</c> → <see cref="Peek"/>;
/// <c>chars.peekable.clone()</c> → <see cref="Peekable"/> (cópia do <c>struct</c>), e
/// <c>chars.peekable.clone().nth(n)</c> → <c>chars.Peekable.Nth(n)</c>.
/// Privado do módulo <c>tokenizer</c> no Rust → <c>internal</c>.
/// </remarks>
internal sealed class State
{
    private PeekableChars _peekable;

    // Último caractere devolvido por Next(): identifica a segunda metade de um par substituto.
    private char? _last;

    /// <summary>
    /// Rust: <c>State { peekable: source.chars().peekable(), line, col }</c>.
    /// </summary>
    public State(string source, long line, long col)
    {
        _peekable = new PeekableChars(source);
        Line = line;
        Col = col;
    }

    /// <summary>
    /// Rust: campo <c>peekable</c>. Devolve uma <b>cópia</b> do cursor (o <c>.clone()</c> do Rust):
    /// avançar a cópia não avança este <c>State</c>.
    /// </summary>
    public PeekableChars Peekable => _peekable;

    /// <summary>Rust: campo <c>line</c>.</summary>
    public long Line { get; private set; }

    /// <summary>Rust: campo <c>col</c>.</summary>
    public long Col { get; private set; }

    /// <summary>return the next character and advance the stream</summary>
    public char? Next()
    {
        // match self.peekable.next() { None => None, Some(s) => { ... } }
        if (_peekable.Next() is not { } s)
        {
            return null;
        }

        if (s == '\n')
        {
            Line += 1;
            Col = 1;
        }
        else if (!(char.IsLowSurrogate(s) && _last is { } prev && char.IsHighSurrogate(prev)))
        {
            // Um scalar value fora do BMP (par substituto) conta como UMA coluna, como o
            // `char` do Rust.
            Col += 1;
        }
        _last = s;
        return s;
    }

    /// <summary>return the next character but do not advance the stream</summary>
    public char? Peek() => _peekable.Peek();

    /// <summary>Return the current <c>Location</c> (line and column)</summary>
    public Location Location() => new Location(Line, Col);
}
