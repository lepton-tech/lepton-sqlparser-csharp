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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core;

/// <summary>Errors produced by the SQL parser.</summary>
public abstract record ParserError : IDisplay
{
    /// <summary>Error originating from the tokenizer with a message.</summary>
    public sealed record TokenizerError(string Value) : ParserError;

    /// <summary>Generic parser error with a message.</summary>
    // Rust: `ParserError::ParserError(String)`. Uma variante com o nome do próprio enum gera CS0542
    // em C#; recebe o sufixo `Value` (convencoes.md §13, caso 43).
    public sealed record ParserErrorValue(string Value) : ParserError;

    /// <summary>Raised when a recursion depth limit is exceeded.</summary>
    public sealed record RecursionLimitExceeded : ParserError;

    // Rust: `impl From<TokenizerError> for ParserError`.
    public static ParserError From(SqlParser.Core.TokenizerError e) => new TokenizerError(e.ToString());

    public void Fmt(Formatter f)
    {
        var message = this switch
        {
            TokenizerError s => s.Value,
            ParserErrorValue s => s.Value,
            RecursionLimitExceeded => "recursion limit exceeded",
            _ => throw new InvalidOperationException($"Variante de ParserError não tratada: {GetType().Name}"),
        };
        f.Write($"sql parser error: {message}");
    }

    public sealed override string ToString() => Formatter.Format(this);
}

/// <summary>
/// The exception thrown when parsing fails. The message is the <c>Display</c> text of the error
/// (<c>"sql parser error: ..."</c>) and the error itself is in <see cref="Error"/>.
/// </summary>
// Lançada onde o Rust devolve `Err(ParserError)` (convencoes.md §8).
public sealed class ParserException : Exception
{
    /// <summary>Creates the exception that carries <paramref name="error"/>.</summary>
    public ParserException(ParserError error)
        : base(error.ToString())
    {
        Error = error;
    }

    /// <summary>The parser error.</summary>
    // Equivale ao valor do `Err` no Rust.
    public ParserError Error { get; }
}
