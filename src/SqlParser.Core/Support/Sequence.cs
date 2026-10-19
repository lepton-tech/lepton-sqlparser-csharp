// Código próprio do port: coleção imutável com igualdade estrutural, usada no lugar de `Vec<T>` na AST.
// Ver docs/port/convencoes.md §5 e §13 (caso 31).

using System.Collections;

namespace SqlParser.Core;

/// <summary>
/// Equivalente a <c>Vec&lt;T&gt;</c> nos tipos da AST: lista imutável com igualdade e hash estruturais
/// (elemento a elemento), como o <c>#[derive(PartialEq, Eq, Hash)]</c> do Rust sobre um <c>Vec</c>.
/// </summary>
/// <remarks>
/// É um <c>struct</c>: o valor padrão (<c>default</c>) é a lista vazia, então nunca é nulo (como no Rust).
/// Correspondências: <c>vec![]</c>/<c>Vec::new()</c> → <see cref="Empty"/>; <c>vec![a, b]</c> → <c>Sequence.Of(a, b)</c>;
/// um <c>Vec</c> montado com <c>push</c> → <c>List&lt;T&gt;</c> + <c>.ToSequence()</c>; <c>v.len()</c> → <see cref="Count"/>;
/// <c>v.is_empty()</c> → <see cref="IsEmpty"/>; <c>v.first()</c>/<c>v.last()</c> → <see cref="First"/>/<see cref="Last"/>
/// (retornam <c>default</c>/<c>null</c> se vazia, como o <c>Option</c>); <c>v[i]</c> → indexador.
/// </remarks>
public readonly partial struct Sequence<T> : IReadOnlyList<T>, IEquatable<Sequence<T>>
{
    private readonly T[]? _items;

    /// <summary>Cria uma sequência com uma cópia dos itens.</summary>
    public Sequence(IEnumerable<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }
        var array = items.ToArray();
        _items = array.Length == 0 ? null : array;
    }

    private Sequence(T[] items, bool _)
    {
        _items = items.Length == 0 ? null : items;
    }

    /// <summary>Sequência vazia (Rust: <c>vec![]</c>).</summary>
    public static Sequence<T> Empty => default;

    /// <summary>Número de itens (Rust: <c>len()</c>).</summary>
    public int Count => _items?.Length ?? 0;

    /// <summary>Rust: <c>is_empty()</c>.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>Rust: <c>v[i]</c>.</summary>
    public T this[int index] => _items is null ? throw new ArgumentOutOfRangeException(nameof(index)) : _items[index];

    /// <summary>Rust: <c>first()</c>. <c>default</c> se vazia.</summary>
    public T? First => Count == 0 ? default : _items![0];

    /// <summary>Rust: <c>last()</c>. <c>default</c> se vazia.</summary>
    public T? Last => Count == 0 ? default : _items![_items.Length - 1];

    internal static Sequence<T> FromArrayNoCopy(T[] items) => new(items, false);

    /// <summary>Enumerador sem alocação.</summary>
    public Enumerator GetEnumerator() => new(_items);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)(_items ?? Array.Empty<T>())).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)this).GetEnumerator();

    /// <summary>Igualdade elemento a elemento (<see cref="EqualityComparer{T}.Default"/>).</summary>
    public bool Equals(Sequence<T> other)
    {
        var count = Count;
        if (count != other.Count)
        {
            return false;
        }
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < count; i++)
        {
            if (!comparer.Equals(_items![i], other._items![i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Sequence<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            var comparer = EqualityComparer<T>.Default;
            foreach (var item in this)
            {
                hash = hash * 31 + (item is null ? 0 : comparer.GetHashCode(item));
            }
            return hash;
        }
    }

    /// <summary>Igualdade estrutural.</summary>
    public static bool operator ==(Sequence<T> left, Sequence<T> right) => left.Equals(right);

    /// <summary>Desigualdade estrutural.</summary>
    public static bool operator !=(Sequence<T> left, Sequence<T> right) => !left.Equals(right);

    /// <summary>Representação de depuração (o Rust não tem <c>Display</c> para <c>Vec</c>).</summary>
    public override string ToString() => $"Sequence<{typeof(T).Name}>[{Count}]";

    /// <summary>Enumerador por valor.</summary>
    public struct Enumerator
    {
        private readonly T[]? _items;
        private int _index;

        internal Enumerator(T[]? items)
        {
            _items = items;
            _index = -1;
        }

        /// <summary>Item atual.</summary>
        public T Current => _items![_index];

        /// <summary>Avança.</summary>
        public bool MoveNext() => _items is not null && ++_index < _items.Length;
    }
}

/// <summary>Fábricas de <see cref="Sequence{T}"/>.</summary>
public static class Sequence
{
    /// <summary>Rust: <c>vec![a, b, c]</c>.</summary>
    public static Sequence<T> Of<T>(params T[] items) => Sequence<T>.FromArrayNoCopy((T[])items.Clone());

    /// <summary>Rust: <c>iter.collect::&lt;Vec&lt;_&gt;&gt;()</c> / um <c>Vec</c> montado com <c>push</c>.</summary>
    public static Sequence<T> ToSequence<T>(this IEnumerable<T> items) => new(items);
}
