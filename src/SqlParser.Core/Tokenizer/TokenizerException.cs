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
// Modificado: tradução de Rust para C#. `Result<T, TokenizerError>` vira retorno `T` + esta
// exceção (convencoes.md §8); corresponde a `impl core::error::Error for TokenizerError {}`.

namespace SqlParser.Core;

/// <summary>
/// Lançada onde o Rust retorna <c>Err(TokenizerError)</c>. A mensagem é o <c>Display</c> do erro,
/// idêntico ao Rust.
/// </summary>
public sealed class TokenizerException : Exception
{
    /// <summary>Cria a exceção a partir do erro do tokenizer.</summary>
    public TokenizerException(TokenizerError error)
        : base(error.ToString())
    {
        Error = error;
    }

    /// <summary>O erro original (Rust: o valor de <c>Err(..)</c>).</summary>
    public TokenizerError Error { get; }
}
