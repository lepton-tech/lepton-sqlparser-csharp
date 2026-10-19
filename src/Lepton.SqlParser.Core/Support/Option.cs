// Código próprio do port: equivalente mínimo de `core::option::Option<T>`, usado SÓ onde o C# não
// consegue representar a distinção com `T?`: Option aninhado (`Option<Option<T>>`,
// `Option<Result<Option<T>>>`) e o retorno de `next()`/`next_back()` dos iteradores portados, com `T`
// genérico sem restrição (caso 65). Nos demais casos, `Option<T>` vira `T?` (convencoes.md §5; caso 55).

namespace Lepton.SqlParser.Core;

/// <summary>
/// <c>Option&lt;T&gt;</c> do Rust para os casos aninhados. <c>None</c> = <see cref="None"/>;
/// <c>Some(x)</c> = <see cref="Some(T)"/>.
/// </summary>
public readonly struct Option<T>
{
    private readonly T _value;

    private Option(T value)
    {
        _value = value;
        IsSome = true;
    }

    /// <summary>Rust: <c>None</c>.</summary>
    public static Option<T> None => default;

    /// <summary>Rust: <c>Some(value)</c>.</summary>
    public static Option<T> Some(T value) => new(value);

    /// <summary>Rust: <c>is_some()</c>.</summary>
    public bool IsSome { get; }

    /// <summary>Rust: <c>is_none()</c>.</summary>
    public bool IsNone => !IsSome;

    /// <summary>Rust: <c>unwrap()</c>: o valor de <c>Some</c>; lança se for <c>None</c>.</summary>
    public T Value => IsSome ? _value : throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");
}
