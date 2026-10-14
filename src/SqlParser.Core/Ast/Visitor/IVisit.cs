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

namespace SqlParser.Core.Ast;

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
/// <para>
/// No port, o derive é o gerador <c>tools/codegen/Generate-Visitor.ps1</c>: cada tipo da AST ganha um
/// <c>partial</c> em <c>Ast/Visitor/Generated/*.g.cs</c> que implementa esta interface (docs/adr/0007-visitor.md).
/// Os <c>impl</c> do Rust para tipos da biblioteca padrão viram:
/// </para>
/// <list type="bullet">
/// <item><c>impl&lt;T: Visit&gt; Visit for Option&lt;T&gt;</c> (visitor.rs:68): um teste de <c>null</c> no código gerado
/// (<c>if let Some(s) = self { s.visit(visitor)?; }</c>);</item>
/// <item><c>impl&lt;T: Visit&gt; Visit for Vec&lt;T&gt;</c> (visitor.rs:77): <see cref="Sequence{T}"/> implementa esta interface
/// (<c>Ast/Visitor/Sequence.Visit.cs</c>), e o código gerado percorre as sequências em laço;</item>
/// <item><c>impl&lt;T: Visit&gt; Visit for Box&lt;T&gt;</c> (visitor.rs:86): nada, porque <c>Box&lt;T&gt;</c> → <c>T</c>;</item>
/// <item><c>visit_noop!(u8, u16, u32, u64, i8, i16, i32, i64, char, bool, String)</c> (visitor.rs:131): folhas sem
/// ganchos; o código gerado não as visita. Os enums Rust sem dados que viraram <c>enum</c> C# (convencoes.md §6,
/// regra 1) também são folhas e não implementam a interface (DIV-008).</item>
/// </list>
/// </remarks>
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
