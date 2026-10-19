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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/spans.rs (linhas 103–107:
// impl Spanned for TokenWithSpan).
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core;

// Rust: o tipo `TokenWithSpan` é do tokenizer (Tokenizer/TokenWithSpan.cs); este `impl` fica em
// ast/spans.rs. Em C#, um `partial` do mesmo record, num arquivo próprio porque o namespace é o do
// tokenizer (os demais `impl` de spans.rs estão em Ast/Spans/Spans.Part*.cs, na ordem do Rust).
// O record já tem a propriedade `Span` (campo `span`): implementação explícita (onda-b/B7.md).
public sealed partial record TokenWithSpan : Ast.ISpanned
{
    // impl Spanned for TokenWithSpan (spans.rs:103)
    Span Ast.ISpanned.Span() => Span;
}
