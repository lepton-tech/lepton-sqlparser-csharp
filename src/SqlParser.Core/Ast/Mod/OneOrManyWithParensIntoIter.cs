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

namespace SqlParser.Core.Ast;

/// <summary>Owned iterator implementation of <c>OneOrManyWithParens</c></summary>
// Iterador do Rust portado como tipo (convencoes.md §13, caso 65): `next`/`next_back` devolvem
// `Option<T>` (Support/Option.cs), `#[derive(Clone)]` → Clone(), e a enumeração (IEnumerable<T>)
// consome o iterador, como o `IntoIterator` de todo `Iterator` no Rust.
public sealed class OneOrManyWithParensIntoIter<T> : IEnumerable<T>, IDoubleEndedIterator<T, OneOrManyWithParensIntoIter<T>>
{
    // Rust: `inner: OneOrManyWithParensIntoIterInner<T>` (campo privado; caso 25).
    private readonly OneOrManyWithParensIntoIterInner<T> _inner;

    // Rust: o literal `OneOrManyWithParensIntoIter { inner }` só existe dentro do módulo (campo privado);
    // quem cria é OneOrManyWithParens<T>.IntoIter().
    internal OneOrManyWithParensIntoIter(OneOrManyWithParensIntoIterInner<T> inner)
    {
        _inner = inner;
    }

    /// <summary>Rust: <c>#[derive(Clone)]</c>: cópia independente (avançar uma não afeta a outra).</summary>
    public OneOrManyWithParensIntoIter<T> Clone() => new OneOrManyWithParensIntoIter<T>(_inner.CloneValue());

    // impl<T> core::iter::FusedIterator for OneOrManyWithParensIntoIter<T>: marcador sem métodos. Depois
    // do fim, Next() e NextBack() continuam devolvendo None, porque os iteradores internos também o fazem.

    /// <summary>Rust: <c>impl ExactSizeIterator for OneOrManyWithParensIntoIter&lt;T&gt;</c> (<c>len()</c>, derivado de <c>size_hint</c>).</summary>
    public int Len() => SizeHint().Lower;

    // impl<T> core::iter::Iterator for OneOrManyWithParensIntoIter<T> { type Item = T; ... }

    /// <summary>Rust: <c>Iterator::next</c>.</summary>
    public Option<T> Next() =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.Next(),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.Next(),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    /// <summary>Rust: <c>Iterator::size_hint</c>.</summary>
    public (int Lower, int? Upper) SizeHint() =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.SizeHint(),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.SizeHint(),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    /// <summary>Rust: <c>Iterator::count(self)</c> (consome o iterador).</summary>
    public int Count() =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.Count(),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.Count(),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    /// <summary>Rust: <c>Iterator::fold(self, init, f)</c> (consome o iterador).</summary>
    public B Fold<B>(B init, Func<B, T, B> f) =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.Fold(init, f),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.Fold(init, f),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    /// <summary>Rust: <c>impl DoubleEndedIterator for OneOrManyWithParensIntoIter&lt;T&gt;</c> (<c>next_back</c>).</summary>
    public Option<T> NextBack() =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.NextBack(),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.NextBack(),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    /// <summary>
    /// Rust: <c>DoubleEndedIterator::rev(self)</c> (método padrão do trait): os elementos restantes, de trás
    /// para a frente, obtidos com <see cref="NextBack"/> (consome o iterador ao enumerar).
    /// </summary>
    public IEnumerable<T> Rev()
    {
        while (NextBack() is { IsSome: true } item)
        {
            yield return item.Value;
        }
    }

    /// <summary>
    /// Rust: <c>impl&lt;I: Iterator&gt; IntoIterator for I</c>: enumera os elementos restantes com
    /// <see cref="Next"/> (consome o iterador).
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        while (Next() is { IsSome: true } item)
        {
            yield return item.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// Rust: `enum OneOrManyWithParensIntoIterInner<T>` (privado ao módulo → internal, caso 20), com
// `#[derive(Debug, Clone)]`. Fica no arquivo do iterador que o usa (§2). Enum com dados e sem Display
// (caso 74).
internal abstract record OneOrManyWithParensIntoIterInner<T>
{
    public sealed record One(OnceIter<T> Value) : OneOrManyWithParensIntoIterInner<T>;

    // Rust: `Many(<Vec<T> as IntoIterator>::IntoIter)`.
    public sealed record Many(VecIntoIter<T> Value) : OneOrManyWithParensIntoIterInner<T>;

    // Rust: #[derive(Clone)]. Records não aceitam um membro chamado `Clone` (CS8859): sufixo `Value`
    // (caso 59). A cópia é profunda, porque o iterador de cada variante tem estado mutável.
    public OneOrManyWithParensIntoIterInner<T> CloneValue() =>
        this switch
        {
            One one => new One(one.Value.Clone()),
            Many many => new Many(many.Value.Clone()),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {GetType().Name}"),
        };
}
