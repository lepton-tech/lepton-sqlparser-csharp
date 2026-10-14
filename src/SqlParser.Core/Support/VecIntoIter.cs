// Código próprio do port (sem origem direta no sqlparser-rs): equivalente de `alloc::vec::IntoIter<T>`,
// o iterador por valor de um `Vec<T>` (`vec.into_iter()`), usado como campo de iteradores portados
// (`OneOrManyWithParensIntoIter`). Ver docs/port/convencoes.md §13 (caso 65) e docs/port/nomes.md.

using System.Collections;

namespace SqlParser.Core;

/// <summary>
/// Rust: <c>alloc::vec::IntoIter&lt;T&gt;</c>: percorre os elementos de um <c>Vec&lt;T&gt;</c> pelas duas
/// pontas.
/// </summary>
/// <remarks>
/// O <c>Vec</c> da AST é imutável no C# (<see cref="Sequence{T}"/>), então <see cref="Clone"/> compartilha os
/// elementos e copia só as duas posições. O Rust copia os elementos restantes para um <c>Vec</c> novo; o
/// resultado observável é o mesmo.
/// </remarks>
internal sealed class VecIntoIter<T> : IDoubleEndedIterator<T, VecIntoIter<T>>
{
    private readonly IReadOnlyList<T> _buf;

    // Posição do próximo elemento da frente.
    private int _ptr;

    // Uma posição depois do próximo elemento de trás.
    private int _end;

    /// <summary>Rust: <c>vec.into_iter()</c>.</summary>
    public VecIntoIter(IReadOnlyList<T> vec)
        : this(vec, 0, vec.Count)
    {
    }

    private VecIntoIter(IReadOnlyList<T> buf, int ptr, int end)
    {
        _buf = buf;
        _ptr = ptr;
        _end = end;
    }

    public Option<T> Next()
    {
        if (_ptr == _end)
        {
            return Option<T>.None;
        }
        return Option<T>.Some(_buf[_ptr++]);
    }

    public (int Lower, int? Upper) SizeHint()
    {
        var exact = _end - _ptr;
        return (exact, exact);
    }

    public int Count()
    {
        var count = _end - _ptr;
        _ptr = _end;
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

    public Option<T> NextBack()
    {
        if (_end == _ptr)
        {
            return Option<T>.None;
        }
        return Option<T>.Some(_buf[--_end]);
    }

    public IEnumerable<T> Rev()
    {
        while (NextBack() is { IsSome: true } item)
        {
            yield return item.Value;
        }
    }

    public int Len() => _end - _ptr;

    public VecIntoIter<T> Clone() => new VecIntoIter<T>(_buf, _ptr, _end);

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
