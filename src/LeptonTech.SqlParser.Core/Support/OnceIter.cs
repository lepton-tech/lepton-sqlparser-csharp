// Código próprio do port (sem origem direta no sqlparser-rs): equivalente de `core::iter::Once<T>`, o
// iterador criado por `core::iter::once(value)`, usado como campo de iteradores portados
// (`OneOrManyWithParensIntoIter`). Ver docs/port/convencoes.md §13 (caso 65) e docs/port/nomes.md.

using System.Collections;

namespace LeptonTech.SqlParser.Core;

/// <summary>
/// Rust: <c>core::iter::Once&lt;T&gt;</c>: um iterador que produz exatamente um elemento.
/// </summary>
/// <remarks>
/// O nome tem o sufixo <c>Iter</c> porque <c>Once</c> já é o método <see cref="RustIter.Once{T}(T)"/>
/// (equivalente de <c>core::iter::once</c> nas cadeias de <c>spans.rs</c>, que só precisam de um
/// <see cref="IEnumerable{T}"/>).
/// </remarks>
internal sealed class OnceIter<T> : IDoubleEndedIterator<T, OnceIter<T>>
{
    // Rust: `inner: option::IntoIter<T>`, um `Option<T>` que `next` e `next_back` esvaziam (`take()`).
    private Option<T> _inner;

    /// <summary>Rust: <c>core::iter::once(value)</c>.</summary>
    public OnceIter(T value)
        : this(Option<T>.Some(value))
    {
    }

    private OnceIter(Option<T> inner)
    {
        _inner = inner;
    }

    public Option<T> Next()
    {
        var item = _inner;
        _inner = Option<T>.None;
        return item;
    }

    public (int Lower, int? Upper) SizeHint()
    {
        var len = _inner.IsSome ? 1 : 0;
        return (len, len);
    }

    public int Count()
    {
        var count = 0;
        while (Next().IsSome)
        {
            count++;
        }
        return count;
    }

    public B Fold<B>(B init, Func<B, T, B> f)
    {
        var accum = init;
        while (Next() is { IsSome: true } item)
        {
            accum = f(accum, item.Value);
        }
        return accum;
    }

    // Rust: `option::IntoIter::next_back` também é `self.inner.take()`.
    public Option<T> NextBack() => Next();

    public IEnumerable<T> Rev()
    {
        while (NextBack() is { IsSome: true } item)
        {
            yield return item.Value;
        }
    }

    public int Len() => _inner.IsSome ? 1 : 0;

    public OnceIter<T> Clone() => new OnceIter<T>(_inner);

    /// <summary>Enumera os elementos restantes com <see cref="Next"/> (consome o iterador).</summary>
    public IEnumerator<T> GetEnumerator()
    {
        while (Next() is { IsSome: true } item)
        {
            yield return item.Value;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
