// Código próprio do port: equivalente de `core::ops::{Bound, RangeBounds}` e das sintaxes de faixa
// do Rust (`a..b`, `a..=b`, `a..`, `..b`, `..=b`, `..`), usado por `Comments::find`.
// Ver convencoes.md §13, caso 57.

namespace LeptonTech.SqlParser.Core;

/// <summary>Rust: <c>core::ops::Bound&lt;T&gt;</c>.</summary>
public abstract record Bound<T>
{
    /// <summary>An inclusive bound.</summary>
    public sealed record Included(T Value) : Bound<T>;

    /// <summary>An exclusive bound.</summary>
    public sealed record Excluded(T Value) : Bound<T>;

    /// <summary>An infinite endpoint. Indicates that there is no bound in this direction.</summary>
    public sealed record Unbounded : Bound<T>;
}

/// <summary>Rust: <c>impl RangeBounds&lt;T&gt;</c>: um par de limites (início, fim).</summary>
public readonly record struct RangeBounds<T>(Bound<T> StartBound, Bound<T> EndBound);

/// <summary>Fábricas com os nomes dos tipos de faixa do Rust.</summary>
public static class RangeBounds
{
    /// <summary>Rust: <c>start..end</c> (<c>Range</c>).</summary>
    public static RangeBounds<T> Range<T>(T start, T end) =>
        new(new Bound<T>.Included(start), new Bound<T>.Excluded(end));

    /// <summary>Rust: <c>start..=end</c> (<c>RangeInclusive</c>).</summary>
    public static RangeBounds<T> RangeInclusive<T>(T start, T end) =>
        new(new Bound<T>.Included(start), new Bound<T>.Included(end));

    /// <summary>Rust: <c>start..</c> (<c>RangeFrom</c>).</summary>
    public static RangeBounds<T> RangeFrom<T>(T start) =>
        new(new Bound<T>.Included(start), new Bound<T>.Unbounded());

    /// <summary>Rust: <c>..end</c> (<c>RangeTo</c>).</summary>
    public static RangeBounds<T> RangeTo<T>(T end) =>
        new(new Bound<T>.Unbounded(), new Bound<T>.Excluded(end));

    /// <summary>Rust: <c>..=end</c> (<c>RangeToInclusive</c>).</summary>
    public static RangeBounds<T> RangeToInclusive<T>(T end) =>
        new(new Bound<T>.Unbounded(), new Bound<T>.Included(end));

    /// <summary>Rust: <c>..</c> (<c>RangeFull</c>).</summary>
    public static RangeBounds<T> RangeFull<T>() =>
        new(new Bound<T>.Unbounded(), new Bound<T>.Unbounded());
}
