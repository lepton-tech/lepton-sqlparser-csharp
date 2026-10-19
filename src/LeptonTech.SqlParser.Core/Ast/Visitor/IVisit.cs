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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/visitor.rs (linhas 29–46: pub trait Visit;
// linhas 68–90 e 116–131: impl Visit for Option<T>/Vec<T>/Box<T> e visit_noop!).
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// A type that can be visited by a <see cref="Visitor{TBreak}"/>. See <see cref="Visitor{TBreak}"/> for
/// recursively visiting parsed SQL statements.
/// </summary>
/// <remarks>
/// <para><b>Note</b></para>
/// <para>
/// This trait should be automatically derived for sqlparser AST nodes
/// using the Visit proc macro (<c>#[cfg_attr(feature = "visitor", derive(Visit, VisitMut))]</c>).
/// </para>
/// </remarks>
// No port, o derive é o gerador `tools/codegen/Generate-Visitor.ps1`: cada tipo da AST ganha um
// `partial` em `Ast/Visitor/Generated/*.g.cs` que implementa esta interface (docs/adr/0007-visitor.md).
// Os `impl` do Rust para tipos da biblioteca padrão viram:
// - `impl<T: Visit> Visit for Option<T>` (visitor.rs:68): um teste de `null` no código gerado
//   (`if let Some(s) = self { s.visit(visitor)?; }`);
// - `impl<T: Visit> Visit for Vec<T>` (visitor.rs:77): Sequence<T> implementa esta interface
//   (`Ast/Visitor/Sequence.Visit.cs`), e o código gerado percorre as sequências em laço;
// - `impl<T: Visit> Visit for Box<T>` (visitor.rs:86): nada, porque `Box<T>` → `T`;
// - `visit_noop!(u8, u16, u32, u64, i8, i16, i32, i64, char, bool, String)` (visitor.rs:131): folhas sem
//   ganchos; o código gerado não as visita. Os enums Rust sem dados que viraram `enum` C# (convencoes.md §6,
//   regra 1) também são folhas e não implementam a interface (DIV-008).
public interface IVisit
{
    /// <summary>
    /// Visit this node with the provided <see cref="Visitor{TBreak}"/>.
    /// </summary>
    /// <remarks>
    /// Implementations should call the appropriate visitor hooks to traverse
    /// child nodes and return a <c>ControlFlow</c> value to allow early exit.
    /// </remarks>
    ControlFlow<TBreak> Visit<TBreak>(Visitor<TBreak> visitor);
}
