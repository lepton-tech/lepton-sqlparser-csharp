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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

using System.Collections;
using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// Encapsulates the common pattern in SQL where either one unparenthesized item
/// such as an identifier or expression is permitted, or multiple of the same
/// item in a parenthesized list. For accessing items regardless of the form,
/// <c>OneOrManyWithParens</c> implements <c>Deref&lt;Target = [T]&gt;</c> and <c>IntoIterator</c>,
/// so you can call slice methods on it and iterate over items
/// </summary>
/// <remarks>
/// <para><b>Examples</b></para>
/// <para>Accessing as a slice:</para>
/// <code>
/// # use sqlparser::ast::OneOrManyWithParens;
/// let one = OneOrManyWithParens::One("a");
///
/// assert_eq!(one[0], "a");
/// assert_eq!(one.len(), 1);
/// </code>
/// <para>Iterating:</para>
/// <code>
/// # use sqlparser::ast::OneOrManyWithParens;
/// let one = OneOrManyWithParens::One("a");
/// let many = OneOrManyWithParens::Many(vec!["a", "b"]);
///
/// assert_eq!(one.into_iter().chain(many).collect::&lt;Vec&lt;_&gt;&gt;(), vec!["a", "a", "b"] );
/// </code>
/// <para>
/// No C#, <c>impl Deref&lt;Target = [T]&gt;</c>, <c>impl AsRef&lt;[T]&gt;</c> e os dois <c>impl IntoIterator</c>
/// (por referência e por valor) viram a implementação de <see cref="IReadOnlyList{T}"/> na base:
/// <c>one.len()</c> → <see cref="Count"/>, <c>one[0]</c> → indexador, <c>for x in &amp;one</c> → <c>foreach</c>.
/// O iterador por valor <c>OneOrManyWithParensIntoIter&lt;T&gt;</c> (com o enum privado
/// <c>OneOrManyWithParensIntoIterInner&lt;T&gt;</c> e os <c>impl Iterator</c>, <c>FusedIterator</c>,
/// <c>ExactSizeIterator</c> e <c>DoubleEndedIterator</c>) não é portado: o enumerador de
/// <see cref="GetEnumerator"/> cumpre o papel, e <c>next_back</c>/<c>rev</c>, <c>count</c> e <c>fold</c>
/// correspondem a <c>Reverse()</c>, <c>Count()</c> e <c>Aggregate()</c> do LINQ. Ver docs/port/onda-a/A1.md.
/// </para>
/// </remarks>
public abstract record OneOrManyWithParens<T> : IDisplay, IReadOnlyList<T>
{
    /// <summary>A single <c>T</c>, unparenthesized.</summary>
    /// <param name="Value">Rust: campo <c>.0</c>.</param>
    public sealed record One(T Value) : OneOrManyWithParens<T>;

    /// <summary>One or more <c>T</c>s, parenthesized.</summary>
    /// <param name="Value">Rust: campo <c>.0</c>.</param>
    public sealed record Many(Sequence<T> Value) : OneOrManyWithParens<T>;

    // Rust: impl<T> Deref for OneOrManyWithParens<T> { type Target = [T]; fn deref(&self) -> &[T] }
    // (também cobre `impl AsRef<[T]>`, que devolve `self`). O `match` do `deref` fica aqui, num lugar só.
    private IReadOnlyList<T> AsSlice() =>
        this switch
        {
            One one => new[] { one.Value }, // core::slice::from_ref(one)
            Many many => many.Value,
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParens não tratada: {GetType().Name}"),
        };

    /// <summary>Rust: <c>len()</c> do slice obtido por <c>Deref</c>.</summary>
    public int Count => AsSlice().Count;

    /// <summary>Rust: <c>self[index]</c> sobre o slice obtido por <c>Deref</c>.</summary>
    public T this[int index] => AsSlice()[index];

    /// <summary>Rust: <c>impl IntoIterator for &amp;OneOrManyWithParens&lt;T&gt;</c> e <c>impl IntoIterator for OneOrManyWithParens&lt;T&gt;</c>.</summary>
    public IEnumerator<T> GetEnumerator() => AsSlice().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case One one:
                f.Write($"{one.Value}");
                break;
            case Many many:
                f.Write($"({DisplayCommaSeparated(many.Value)})");
                break;
            default:
                throw new InvalidOperationException($"Variante de OneOrManyWithParens não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
