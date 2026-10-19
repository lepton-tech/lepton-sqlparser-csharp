// Código próprio do port (sem origem direta no sqlparser-rs): a parte dos traits `Iterator`,
// `DoubleEndedIterator` e `ExactSizeIterator` (mais o `Clone`) que os iteradores por valor portados
// implementam. Permite escrever código genérico sobre eles, como o `test_steps<I>` de ast/mod.rs.
// Ver docs/port/convencoes.md §13 (caso 65).

namespace LeptonTech.SqlParser.Core;

/// <summary>
/// Rust: <c>Iterator + DoubleEndedIterator + ExactSizeIterator + Clone</c>, só com os métodos que os
/// iteradores portados sobrescrevem ou que os testes usam. Os demais métodos de <c>Iterator</c>
/// (<c>map</c>, <c>chain</c>, <c>collect</c>...) correspondem ao LINQ sobre o <see cref="IEnumerable{T}"/>,
/// cuja enumeração consome o iterador (<c>impl&lt;I: Iterator&gt; IntoIterator for I</c>).
/// </summary>
/// <typeparam name="T">Rust: <c>type Item</c>.</typeparam>
/// <typeparam name="TSelf">O próprio tipo do iterador, devolvido por <see cref="Clone"/>.</typeparam>
internal interface IDoubleEndedIterator<T, TSelf> : IEnumerable<T>
    where TSelf : IDoubleEndedIterator<T, TSelf>
{
    /// <summary>Rust: <c>Iterator::next</c>: o próximo elemento, ou <c>None</c> no fim.</summary>
    Option<T> Next();

    /// <summary>Rust: <c>Iterator::size_hint</c>: os limites inferior e superior do que falta.</summary>
    (int Lower, int? Upper) SizeHint();

    /// <summary>Rust: <c>Iterator::count(self)</c>: quantos elementos faltam (consome o iterador).</summary>
    int Count();

    /// <summary>Rust: <c>Iterator::fold(self, init, f)</c> (consome o iterador).</summary>
    B Fold<B>(B init, Func<B, T, B> f);

    /// <summary>Rust: <c>DoubleEndedIterator::next_back</c>: o último elemento, ou <c>None</c> no fim.</summary>
    Option<T> NextBack();

    /// <summary>
    /// Rust: <c>DoubleEndedIterator::rev(self)</c>: os elementos restantes, de trás para a frente, obtidos
    /// com <see cref="NextBack"/> (consome o iterador ao enumerar).
    /// </summary>
    IEnumerable<T> Rev();

    /// <summary>Rust: <c>ExactSizeIterator::len</c>: quantos elementos faltam, sem consumir.</summary>
    int Len();

    /// <summary>Rust: <c>Clone::clone</c>: cópia independente (avançar uma não afeta a outra).</summary>
    TSelf Clone();
}
