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
/// <typeparam name="TSelf">The implementing type: the type of the node returned in <c>self</c>.</typeparam>
/// <remarks>
/// <para><b>Note</b></para>
/// <para>
/// This trait should be automatically derived for sqlparser AST nodes
/// using the VisitMut proc macro (<c>#[cfg_attr(feature = "visitor", derive(Visit, VisitMut))]</c>).
/// </para>
/// </remarks>
// No Rust, `fn visit(&mut self, ..)` altera o nó no lugar. A AST C# é imutável (records, ADR-0002), então
// a visita devolve o nó resultante no parâmetro `self` (`var flow = s.Visit(mutador, out s);`):
// é a mesma instância quando nenhum gancho mudou nada; senão, cada pai do nó alterado é reconstruído com
// `with`, inclusive quando a visita para no meio com `Break` (o Rust também mantém as mudanças feitas
// até ali). Os ganchos de VisitorMut<TBreak> recebem o nó por `ref` (`&mut`).
// TSelf: o próprio tipo (o `Self` do Rust).
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
    /// <param name="visitor">The visitor.</param>
    /// <param name="self">The node after the visit: the same instance if no hook changed anything.</param>
    // self: o nó depois da visita (o `&mut self` do Rust).
    ControlFlow<TBreak> Visit<TBreak>(VisitorMut<TBreak> visitor, out TSelf self);
}
