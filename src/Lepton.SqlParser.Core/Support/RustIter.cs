// Código próprio do port (sem origem direta no sqlparser-rs): equivalentes mínimos de construções de
// iteradores da biblioteca padrão do Rust (`core::iter::once` e `Option` usado como iterador), para
// traduzir as cadeias `.chain(..)` de src/ast/spans.rs com LINQ (convencoes.md §9; onda-b/B7.md).

namespace Lepton.SqlParser.Core;

/// <summary>
/// Equivalentes de construções de iteradores da biblioteca padrão do Rust. Os nomes seguem os do Rust,
/// para que a tradução das cadeias de iteradores continue comparável linha a linha.
/// </summary>
/// <remarks>
/// Mapeamento usado pelo port: <c>core::iter::once(x)</c> → <see cref="Once{T}(T)"/>;
/// <c>a.chain(b)</c> → <c>a.Concat(b)</c>; <c>v.iter().map(f)</c> → <c>v.Select(f)</c>;
/// <c>.flat_map(f)</c> → <c>.SelectMany(f)</c>; <c>opt.iter()</c> / <c>opt.as_ref()</c> num
/// <c>chain</c> (um <c>Option</c> percorrido como iterador de 0 ou 1 elemento) →
/// <see cref="OptionIter{T}(T)"/>.
/// </remarks>
internal static class RustIter
{
    /// <summary>Rust: <c>core::iter::once(value)</c>: um iterador com um único elemento.</summary>
    public static IEnumerable<T> Once<T>(T value) => new[] { value };

    /// <summary>
    /// Rust: <c>Option&lt;T&gt;</c> como iterador (<c>opt.iter()</c>, <c>opt.into_iter()</c>,
    /// <c>opt.as_ref()</c> seguido de <c>.map(..)</c> numa cadeia): nenhum elemento para <c>None</c>,
    /// um elemento para <c>Some(x)</c>. Sobrecarga para tipos-referência.
    /// </summary>
    public static IEnumerable<T> OptionIter<T>(T? value)
        where T : class =>
        value is null ? System.Array.Empty<T>() : new[] { value };

    /// <summary>
    /// Rust: <c>Option&lt;T&gt;</c> como iterador. Sobrecarga para tipos-valor
    /// (<c>Option&lt;Span&gt;</c>, <c>Option&lt;Vec&lt;T&gt;&gt;</c> → <c>Sequence&lt;T&gt;?</c>).
    /// </summary>
    public static IEnumerable<T> OptionIter<T>(T? value)
        where T : struct =>
        value is { } v ? new[] { v } : System.Array.Empty<T>();
}
