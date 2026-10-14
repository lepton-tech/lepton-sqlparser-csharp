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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/visitor.rs (linhas 48–66: pub trait VisitMut;
// linhas 92–114: impl VisitMut for Option<T>/Vec<T>/Box<T>).
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// A type that can be visited by a <see cref="VisitorMut{TBreak}"/>. See <see cref="VisitorMut{TBreak}"/> for
/// recursively visiting parsed SQL statements.
/// </summary>
/// <typeparam name="TSelf">O próprio tipo (o <c>Self</c> do Rust): é o tipo do nó devolvido em <c>self</c>.</typeparam>
/// <remarks>
/// <para><b>Note</b></para>
/// <para>
/// This trait should be automatically derived for sqlparser AST nodes
/// using the VisitMut proc macro (<c>#[cfg_attr(feature = "visitor", derive(Visit, VisitMut))]</c>).
/// </para>
/// <para>
/// No Rust, <c>fn visit(&amp;mut self, ..)</c> altera o nó no lugar. A AST C# é imutável (records, ADR-0002), então
/// a visita devolve o nó resultante no parâmetro <c>self</c> (<c>var flow = s.Visit(mutador, out s);</c>):
/// é a mesma instância quando nenhum gancho mudou nada; senão, cada pai do nó alterado é reconstruído com
/// <c>with</c>, inclusive quando a visita para no meio com <c>Break</c> (o Rust também mantém as mudanças feitas
/// até ali). Os ganchos de <see cref="VisitorMut{TBreak}"/> recebem o nó por <c>ref</c> (<c>&amp;mut</c>).
/// </para>
/// </remarks>
public interface IVisitMut<TSelf>
{
    /// <summary>
    /// Mutably visit this node with the provided <see cref="VisitorMut{TBreak}"/>.
    /// </summary>
    /// <remarks>
    /// Implementations should call the appropriate mutable visitor hooks to
    /// traverse and allow in-place mutation of child nodes. Returning a
    /// <c>ControlFlow</c> value permits early termination of the traversal.
    /// </remarks>
    /// <param name="visitor">O visitor.</param>
    /// <param name="self">O nó depois da visita (o <c>&amp;mut self</c> do Rust).</param>
    ControlFlow<TBreak> Visit<TBreak>(VisitorMut<TBreak> visitor, out TSelf self);
}
