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

/// <summary>Helper to indicate if a collection should be wrapped by a symbol in the display form</summary>
/// <remarks>
/// <c>Display</c> is implemented for every <c>Vec&lt;T&gt;</c> where <c>T: Display</c>.
/// The string output is a comma separated list for the vec items
/// <para>Examples:</para>
/// <code>
/// let items = WrappedCollection::Parentheses(vec!["one", "two", "three"]);
/// assert_eq!("(one, two, three)", items.to_string());
///
/// let items = WrappedCollection::NoWrapping(vec!["one", "two", "three"]);
/// assert_eq!("one, two, three", items.to_string());
/// </code>
/// <para>
/// No Rust, o <c>Display</c> existe só para <c>WrappedCollection&lt;Vec&lt;T&gt;&gt;</c>. C# não permite
/// implementar uma interface para uma única instanciação do genérico; por isso o tipo inteiro implementa
/// <see cref="IDisplay"/> e o <see cref="Fmt"/> exige, em tempo de execução, que <c>T</c> seja uma coleção
/// (na AST, <c>Sequence&lt;U&gt;</c>). Com outro <c>T</c>, lança <see cref="InvalidOperationException"/>
/// (no Rust seria erro de compilação).
/// </para>
/// </remarks>
public abstract partial record WrappedCollection<T> : IDisplay
{
    /// <summary>Print the collection without wrapping symbols, as <c>item, item, item</c></summary>
    public sealed record NoWrapping(T Value) : WrappedCollection<T>;

    /// <summary>Wraps the collection in Parentheses, as <c>(item, item, item)</c></summary>
    public sealed record Parentheses(T Value) : WrappedCollection<T>;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case NoWrapping inner:
                f.Write($"{DisplayCommaSeparated(AsSlice(inner.Value))}");
                break;
            case Parentheses inner:
                f.Write($"({DisplayCommaSeparated(AsSlice(inner.Value))})");
                break;
            default:
                throw new InvalidOperationException($"Variante de WrappedCollection não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);

    // Rust: `inner.as_slice()` de um `Vec<T>`. Ver a observação no <remarks> do tipo.
    private static IReadOnlyList<object?> AsSlice(T inner)
    {
        if (inner is IEnumerable items and not string)
        {
            var list = new List<object?>();
            foreach (var item in items)
            {
                list.Add(item);
            }
            return list;
        }
        throw new InvalidOperationException(
            $"WrappedCollection<{typeof(T).Name}> não tem Display: no Rust ele só existe para WrappedCollection<Vec<T>>.");
    }
}
