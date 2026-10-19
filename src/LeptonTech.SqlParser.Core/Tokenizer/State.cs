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

// Rust: `struct State { peekable: Peekable<Chars>, line, col }`, o cursor do tokenizer.
// Col avança uma vez por scalar value Unicode, como no Rust: a segunda metade
// de um par substituto não incrementa a coluna (convencoes.md §10 e §13).
// Equivalências: `chars.next()` → Next; `chars.peek()` → Peek;
// `chars.peekable.clone()` → Peekable (cópia do `struct`), e
// `chars.peekable.clone().nth(n)` → `chars.Peekable.Nth(n)`.
// Privado do módulo `tokenizer` no Rust → `internal`.
internal sealed class State
{
    private PeekableChars _peekable;

    // Último caractere devolvido por Next(): identifica a segunda metade de um par substituto.
    private char? _last;

    // Rust: `State { peekable: source.chars().peekable(), line, col }`.
    public State(string source, long line, long col)
    {
        _peekable = new PeekableChars(source);
        Line = line;
        Col = col;
    }

    // Rust: campo `peekable`. Devolve uma cópia do cursor (o `.clone()` do Rust):
    // avançar a cópia não avança este `State`.
    public PeekableChars Peekable => _peekable;

    // Rust: campo `line`.
    public long Line { get; private set; }

    // Rust: campo `col`.
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
