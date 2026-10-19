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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 21242–21268:
// impl Word { to_ident, into_ident }).
// Modificado: tradução de Rust para C#.

using Lepton.SqlParser.Core.Ast;

namespace Lepton.SqlParser.Core;

// Rust: o tipo `Word` é do tokenizer (Tokenizer/Word.cs); este `impl` fica em parser/mod.rs porque
// depende da AST (`Ident`). Em C#, um `partial` do mesmo record.
public sealed partial record Word
{
    /// <summary>Convert a reference to this word into an <see cref="Ident"/> by cloning the value.</summary>
    /// <remarks>
    /// Use this method when you need to keep the original <c>Word</c> around.
    /// If you can consume the <c>Word</c>, prefer <see cref="IntoIdent"/> instead
    /// to avoid cloning.
    /// </remarks>
    // #[inline]: não traduzido.
    public Ident ToIdent(Span span) => new Ident(Value: Value, QuoteStyle: QuoteStyle, Span: span);

    /// <summary>Convert this word into an <see cref="Ident"/> identifier, consuming the <c>Word</c>.</summary>
    /// <remarks>
    /// This avoids cloning the string value. If you need to keep the original
    /// <c>Word</c>, use <see cref="ToIdent"/> instead.
    /// </remarks>
    // Strings são imutáveis em C#: o corpo é igual ao de ToIdent.
    public Ident IntoIdent(Span span) => new Ident(Value: Value, QuoteStyle: QuoteStyle, Span: span);
}
