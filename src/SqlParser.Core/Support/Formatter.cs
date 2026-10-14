// Código próprio do port (sem origem direta no sqlparser-rs): equivalente mínimo de
// core::fmt::Display / core::fmt::Formatter do Rust, para que os `impl Display` sejam
// traduzidos de forma mecânica. Ver docs/port/convencoes.md, seção 7.

using System.Globalization;
using System.Text;

namespace SqlParser.Core;

/// <summary>
/// Equivalente ao trait <c>core::fmt::Display</c>. <c>impl Display for X { fn fmt(&amp;self, f) }</c>
/// vira <c>public void Fmt(Formatter f)</c>.
/// </summary>
public interface IDisplay
{
    /// <summary>Escreve a representação do valor no formatador (Rust: <c>fmt</c>).</summary>
    void Fmt(Formatter f);
}

/// <summary>
/// Equivalente a <c>core::fmt::Formatter</c>: um buffer de saída + flags de formatação.
/// </summary>
/// <remarks>
/// Correspondências com o Rust:
/// <list type="bullet">
/// <item><c>f.write_str(s)</c> → <see cref="WriteStr"/></item>
/// <item><c>f.write_char(c)</c> → <see cref="WriteChar"/></item>
/// <item><c>write!(f, "{}", x)</c> → <see cref="Write(IDisplay)"/> (flags novos, sem <c>#</c>)</item>
/// <item><c>write!(f, "{:#}", x)</c> → <see cref="WriteAlternate"/></item>
/// <item><c>x.fmt(f)</c> (repassa o formatador e seus flags) → <c>x.Fmt(f)</c></item>
/// <item><c>f.alternate()</c> → <see cref="Alternate"/></item>
/// <item><c>write!(f, "a {} b {}", x, y)</c> → <c>f.WriteStr(FormattableString.Invariant($"a {x} b {y}"))</c>
/// (interpolar um <see cref="IDisplay"/> chama <c>ToString()</c>, que equivale a <c>{}</c>)</item>
/// </list>
/// </remarks>
public sealed class Formatter
{
    private readonly StringBuilder _buffer;

    /// <summary>Cria um formatador que escreve em <paramref name="buffer"/>.</summary>
    public Formatter(StringBuilder buffer, bool alternate = false)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        Alternate = alternate;
    }

    /// <summary>Rust: <c>f.alternate()</c>, verdadeiro quando formatado com <c>{:#}</c>.</summary>
    public bool Alternate { get; }

    /// <summary>Rust: <c>f.write_str(s)</c>.</summary>
    public void WriteStr(string value) => _buffer.Append(value);

    /// <summary>Rust: <c>f.write_char(c)</c>.</summary>
    public void WriteChar(char value) => _buffer.Append(value);

    /// <summary>Rust: <c>write!(f, "{}", value)</c>. O valor é formatado com flags novos (sem <c>#</c>).</summary>
    public void Write(IDisplay value) => value.Fmt(new Formatter(_buffer));

    /// <summary>Rust: <c>write!(f, "{:#}", value)</c>.</summary>
    public void WriteAlternate(IDisplay value) => value.Fmt(new Formatter(_buffer, alternate: true));

    /// <summary>Rust: <c>write!(f, "{}", n)</c> para inteiros (sempre cultura invariante).</summary>
    public void Write(long value) => _buffer.Append(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Rust: <c>write!(f, "{}", n)</c> para inteiros sem sinal (sempre cultura invariante).</summary>
    public void Write(ulong value) => _buffer.Append(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Rust: <c>value.to_string()</c> / <c>format!("{}", value)</c>.</summary>
    public static string Format(IDisplay value)
    {
        var sb = new StringBuilder();
        value.Fmt(new Formatter(sb));
        return sb.ToString();
    }

    /// <summary>Rust: <c>format!("{:#}", value)</c>.</summary>
    public static string FormatAlternate(IDisplay value)
    {
        var sb = new StringBuilder();
        value.Fmt(new Formatter(sb, alternate: true));
        return sb.ToString();
    }
}
