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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/visitor.rs (linhas 77–84: impl<T: Visit> Visit for Vec<T>;
// linhas 101–108: impl<T: VisitMut> VisitMut for Vec<T>).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;

namespace SqlParser.Core;

// `Vec<T>` → `Sequence<T>` (convencoes.md §13, caso 31). O Rust só implementa Visit quando `T: Visit`;
// o C# não tem implementação condicional de interface, então a sequência é sempre visitável e cada
// elemento é visitado se implementar IVisit/IVisitMut<T> (os demais são folhas, como no `visit_noop!`).
// O código gerado percorre as sequências de tipo conhecido em laço próprio, sem estes testes de tipo.
public readonly partial struct Sequence<T> : IVisit, IVisitMut<Sequence<T>>, ISequenceStorage
{
    /// <summary>Rust: <c>impl&lt;T: Visit&gt; Visit for Vec&lt;T&gt;</c>.</summary>
    public ControlFlow<TBreak> Visit<TBreak>(Visitor<TBreak> visitor)
    {
        // for v in self { v.visit(visitor)?; }
        for (var i = 0; i < Count; i++)
        {
            if (_items![i] is IVisit v)
            {
                var flow = v.Visit(visitor);
                if (flow.IsBreak)
                {
                    return flow;
                }
            }
        }
        return ControlFlow<TBreak>.Continue;
    }

    /// <summary>Rust: <c>impl&lt;T: VisitMut&gt; VisitMut for Vec&lt;T&gt;</c>.</summary>
    /// <remarks>Devolve em <paramref name="self"/> a mesma sequência se nenhum elemento mudou.</remarks>
    public ControlFlow<TBreak> Visit<TBreak>(VisitorMut<TBreak> visitor, out Sequence<T> self)
    {
        // for v in self { v.visit(visitor)?; }
        var flow = ControlFlow<TBreak>.Continue;
        T[]? changed = null;
        for (var i = 0; i < Count; i++)
        {
            var item = _items![i];
            flow = VisitSupport.VisitMutGeneric(ref item, visitor);
            if (!VisitSupport.IsSame(item, _items[i]))
            {
                changed ??= (T[])_items.Clone();
                changed[i] = item;
            }
            if (flow.IsBreak)
            {
                break;
            }
        }
        // `self` só é escrito no fim: numa chamada `s.Visit(v, out s)`, ele é a mesma variável que `this`.
        self = changed is null ? this : FromArrayNoCopy(changed);
        return flow;
    }

    /// <summary>Mesma memória (mesmo array) que <paramref name="other"/>: usado para saber se a visita mutável mudou algo.</summary>
    internal bool IsSameStorage(Sequence<T> other) => ReferenceEquals(_items, other._items);

    object? ISequenceStorage.Storage => _items;
}

/// <summary>Acesso à memória de uma <see cref="Sequence{T}"/> de tipo desconhecido (contextos genéricos do visitor).</summary>
internal interface ISequenceStorage
{
    object? Storage { get; }
}
