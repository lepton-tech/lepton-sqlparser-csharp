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

    // Rust: `#[derive(Clone)]`: cópia independente (avançar uma não afeta a outra).
    public OneOrManyWithParensIntoIter<T> Clone() => new OneOrManyWithParensIntoIter<T>(_inner.CloneValue());

    // impl<T> core::iter::FusedIterator for OneOrManyWithParensIntoIter<T>: marcador sem métodos. Depois
    // do fim, Next() e NextBack() continuam devolvendo None, porque os iteradores internos também o fazem.

    // Rust: `impl ExactSizeIterator for OneOrManyWithParensIntoIter<T>` (`len()`, derivado de `size_hint`).
    public int Len() => SizeHint().Lower;

    // impl<T> core::iter::Iterator for OneOrManyWithParensIntoIter<T> { type Item = T; ... }

    // Rust: `Iterator::next`.
    public Option<T> Next() =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.Next(),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.Next(),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    // Rust: `Iterator::size_hint`.
    public (int Lower, int? Upper) SizeHint() =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.SizeHint(),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.SizeHint(),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    // Rust: `Iterator::count(self)` (consome o iterador).
    public int Count() =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.Count(),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.Count(),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    // Rust: `Iterator::fold(self, init, f)` (consome o iterador).
    public B Fold<B>(B init, Func<B, T, B> f) =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.Fold(init, f),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.Fold(init, f),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    // Rust: `impl DoubleEndedIterator for OneOrManyWithParensIntoIter<T>` (`next_back`).
    public Option<T> NextBack() =>
        _inner switch
        {
            OneOrManyWithParensIntoIterInner<T>.One one => one.Value.NextBack(),
            OneOrManyWithParensIntoIterInner<T>.Many many => many.Value.NextBack(),
            _ => throw new InvalidOperationException($"Variante de OneOrManyWithParensIntoIterInner não tratada: {_inner.GetType().Name}"),
        };

    // Rust: `DoubleEndedIterator::rev(self)` (método padrão do trait): os elementos restantes, de trás
    // para a frente, obtidos com NextBack (consome o iterador ao enumerar).
    public IEnumerable<T> Rev()
    {
        while (NextBack() is { IsSome: true } item)
        {
            yield return item.Value;
        }
    }

    // Rust: `impl<I: Iterator> IntoIterator for I`: enumera os elementos restantes com
    // Next (consome o iterador).
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
