// Código próprio do port: equivalente de `core::ops::ControlFlow<B, C = ()>` da biblioteca padrão do Rust,
// usado pelo visitor (ast/visitor.rs). Só a forma `C = ()` existe no sqlparser-rs.
// Ver docs/adr/0007-visitor.md e convencoes.md §13 (caso 125).

namespace SqlParser.Core;

/// <summary>
/// Rust: <c>core::ops::ControlFlow&lt;B&gt;</c> (com <c>C = ()</c>). Diz se uma operação deve sair
/// antes (<see cref="Break"/>, com um valor) ou seguir normalmente (<see cref="Continue"/>).
/// </summary>
/// <typeparam name="TBreak">Tipo do valor de <c>Break</c> (Rust: <c>B</c>). O <c>()</c> do Rust é <see cref="ValueTuple"/>.</typeparam>
/// <remarks>
/// <c>ControlFlow::Continue(())</c> → <see cref="Continue"/> (é o valor <c>default</c>);
/// <c>ControlFlow::Break(b)</c> → <see cref="Break"/>; o operador <c>?</c> sobre um <c>ControlFlow</c> →
/// <c>if (flow.IsBreak) return flow;</c>.
/// </remarks>
public readonly struct ControlFlow<TBreak> : IEquatable<ControlFlow<TBreak>>
{
    private readonly TBreak _value;

    private ControlFlow(TBreak value)
    {
        IsBreak = true;
        _value = value;
    }

    /// <summary>Rust: <c>ControlFlow::Continue(())</c>. Move on to the next phase of the operation as normal.</summary>
    public static ControlFlow<TBreak> Continue => default;

    /// <summary>Rust: <c>ControlFlow::Break(value)</c>. Exit the operation without running subsequent phases.</summary>
    public static ControlFlow<TBreak> Break(TBreak value) => new(value);

    /// <summary>Rust: <c>is_break()</c>. Returns <c>true</c> if this is a <c>Break</c> variant.</summary>
    public bool IsBreak { get; }

    /// <summary>Rust: <c>is_continue()</c>. Returns <c>true</c> if this is a <c>Continue</c> variant.</summary>
    public bool IsContinue => !IsBreak;

    /// <summary>
    /// Rust: <c>break_value()</c>. Converts the <c>ControlFlow</c> into an <c>Option</c> which is <c>Some</c> if the
    /// <c>ControlFlow</c> was <c>Break</c> and <c>None</c> otherwise.
    /// </summary>
    public Option<TBreak> BreakValue() => IsBreak ? Option<TBreak>.Some(_value) : Option<TBreak>.None;

    /// <summary>Igualdade do <c>#[derive(PartialEq)]</c>: mesma variante e mesmo valor.</summary>
    public bool Equals(ControlFlow<TBreak> other) =>
        IsBreak == other.IsBreak && (!IsBreak || EqualityComparer<TBreak>.Default.Equals(_value, other._value));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ControlFlow<TBreak> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsBreak ? (_value is null ? 1 : EqualityComparer<TBreak>.Default.GetHashCode(_value) * 31 + 1) : 0;

    /// <summary>Igualdade.</summary>
    public static bool operator ==(ControlFlow<TBreak> left, ControlFlow<TBreak> right) => left.Equals(right);

    /// <summary>Desigualdade.</summary>
    public static bool operator !=(ControlFlow<TBreak> left, ControlFlow<TBreak> right) => !left.Equals(right);

    /// <summary>Formato do <c>Debug</c> do Rust: <c>Continue(())</c> ou <c>Break(valor)</c>.</summary>
    public override string ToString() =>
        IsBreak ? (_value is ValueTuple ? "Break(())" : $"Break({_value})") : "Continue(())";
}
