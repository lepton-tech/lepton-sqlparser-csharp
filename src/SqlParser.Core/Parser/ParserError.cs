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
    /// <remarks>
    /// Rust: <c>ParserError::ParserError(String)</c>. Uma variante com o nome do próprio enum gera CS0542
    /// em C#; recebe o sufixo <c>Value</c> (convencoes.md §13, caso 43).
    /// </remarks>
    public sealed record ParserErrorValue(string Value) : ParserError;

    /// <summary>Raised when a recursion depth limit is exceeded.</summary>
    public sealed record RecursionLimitExceeded : ParserError;

    /// <summary>Rust: <c>impl From&lt;TokenizerError&gt; for ParserError</c>.</summary>
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
/// Lançada onde o Rust devolve <c>Err(ParserError)</c> (convencoes.md §8). A mensagem é o
/// <c>Display</c> do erro (<c>"sql parser error: ..."</c>) e o valor fica em <see cref="Error"/>.
/// </summary>
public sealed class ParserException : Exception
{
    /// <summary>Cria a exceção que carrega <paramref name="error"/>.</summary>
    public ParserException(ParserError error)
        : base(error.ToString())
    {
        Error = error;
    }

    /// <summary>O erro do parser (equivalente ao valor do <c>Err</c> no Rust).</summary>
    public ParserError Error { get; }
}
