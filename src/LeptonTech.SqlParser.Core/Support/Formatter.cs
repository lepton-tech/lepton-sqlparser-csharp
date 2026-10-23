// Código próprio do port (sem origem direta no sqlparser-rs): equivalente de
// core::fmt::{Display, Formatter, Write} do Rust, para que os `impl Display` sejam
// traduzidos de forma mecânica, inclusive o pretty-print (`{:#}`) com indentação.
// Ver docs/port/convencoes.md, seção 7, e o caso 30 da seção 13.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace LeptonTech.SqlParser.Core;

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
/// Equivalente ao trait <c>core::fmt::Write</c>: um destino de texto. O <see cref="Formatter"/>
/// escreve sempre num <see cref="IFmtWrite"/>, o que permite encadear destinos, como o
/// <c>Indent</c> de <c>display_utils.rs</c>, que acrescenta indentação após cada <c>"\n"</c>.
/// </summary>
public interface IFmtWrite
{
    /// <summary>Rust: <c>Write::write_str</c>.</summary>
    bool WriteStr(string value);
}

/// <summary>
/// Equivalente a <c>core::fmt::Formatter</c>: um destino de saída (<see cref="IFmtWrite"/>) mais os
/// flags de formatação (só <c>#</c>, "alternate", é usado pelo sqlparser-rs).
/// </summary>
/// <remarks>
/// Correspondências com o Rust:
/// <list type="bullet">
/// <item><c>f.write_str(s)</c> → <see cref="WriteStr"/>; <c>f.write_char(c)</c> → <see cref="WriteChar"/></item>
/// <item><c>write!(f, "a {} b {:#}", x, y)</c> → <c>f.Write($"a {x} b {y:#}")</c> (cada lacuna é escrita no
/// mesmo destino, com flags novos; <c>:#</c> liga o "alternate" daquela lacuna, como no Rust)</item>
/// <item><c>write!(f, "{}", x)</c> → <see cref="Write(IDisplay)"/>; <c>write!(f, "{:#}", x)</c> → <see cref="WriteAlternate"/></item>
/// <item><c>x.fmt(f)</c> (repassa o formatador e seus flags) → <c>x.Fmt(f)</c>, ou <see cref="FmtInPlace{T}"/>
/// quando <c>x</c> é de tipo genérico</item>
/// <item><c>f.alternate()</c> → <see cref="Alternate"/></item>
/// <item><c>x.to_string()</c> / <c>format!("{}", x)</c> → <see cref="Format"/>; <c>format!("{:#}", x)</c> → <see cref="FormatAlternate"/></item>
/// </list>
/// </remarks>
public sealed class Formatter : IFmtWrite
{
    private readonly IFmtWrite _sink;
    private readonly Formatter _root;
    private bool _error;

    /// <summary>Cria um formatador que escreve em <paramref name="buffer"/>.</summary>
    public Formatter(StringBuilder buffer, bool alternate = false)
        : this(new StringBuilderWrite(buffer ?? throw new ArgumentNullException(nameof(buffer))), alternate)
    {
    }

    /// <summary>
    /// Cria um formatador sobre outro destino. Rust: o <c>Formatter</c> criado por <c>write!(w, ...)</c>
    /// sobre um <c>w: impl Write</c> (por exemplo, <c>write!(Indent(f), "{:#}", x)</c>).
    /// </summary>
    public Formatter(IFmtWrite sink, bool alternate = false)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        Alternate = alternate;
        _root = this;
    }

    private Formatter(IFmtWrite sink, bool alternate, Formatter root)
    {
        _sink = sink;
        Alternate = alternate;
        _root = root;
    }

    /// <summary>Rust: <c>f.alternate()</c>, verdadeiro quando formatado com <c>{:#}</c>.</summary>
    public bool Alternate { get; }

    /// <summary>Rust: <c>f.write_str(s)</c>.</summary>
    public bool WriteStr(string value)
    {
        if (_root._error)
        {
            return false;
        }
        if (_sink.WriteStr(value))
        {
            return true;
        }
        _root._error = true;
        return false;
    }

    /// <summary>
    /// Rust: <c>f.write_char(c)</c>. Vai ao destino como uma string de um caractere, como no Rust
    /// (<c>write_char</c> chama <c>write_str</c>), o que importa para o <c>Indent</c>, que reage a <c>"\n"</c>.
    /// </summary>
    public bool WriteChar(char value) => WriteStr(value.ToString());

    /// <summary>Rust: <c>write!(f, "{}", value)</c>. O valor é formatado no mesmo destino, com flags novos (sem <c>#</c>).</summary>
    public bool Write(IDisplay value) => WriteDisplay(value, alternate: false);

    /// <summary>Rust: <c>write!(f, "{:#}", value)</c>.</summary>
    public bool WriteAlternate(IDisplay value) => WriteDisplay(value, alternate: true);

    /// <summary>Rust: <c>write!(f, "{}", n)</c> para inteiros (sempre cultura invariante).</summary>
    public bool Write(long value) => WriteStr(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Rust: <c>write!(f, "{}", n)</c> para inteiros sem sinal (sempre cultura invariante).</summary>
    public bool Write(ulong value) => WriteStr(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Rust: <c>write!(f, "texto {} texto {:#}", a, b)</c> → <c>f.Write($"texto {a} texto {b:#}")</c>.
    /// O texto já foi escrito pelo <see cref="FormatterInterpolatedStringHandler"/> durante a chamada.
    /// </summary>
    public bool Write([InterpolatedStringHandlerArgument("")] ref FormatterInterpolatedStringHandler handler) =>
        !_root._error;

    private bool WriteDisplay(IDisplay value, bool alternate)
    {
        if (_root._error)
        {
            return false;
        }
        value.Fmt(new Formatter(_sink, alternate, _root));
        return !_root._error;
    }

    /// <summary>
    /// Escreve um valor de qualquer tipo como uma lacuna <c>{}</c> (ou <c>{:#}</c>) de <c>write!</c>:
    /// flags novos, mesmo destino. Usado pelo handler de interpolação e por tipos genéricos.
    /// </summary>
    /// <remarks>
    /// Tipos aceitos: <see cref="IDisplay"/>, <see cref="string"/>, <see cref="char"/>, <see cref="bool"/>
    /// (<c>true</c>/<c>false</c> minúsculos, como no Rust), <see cref="Keyword"/> (texto SQL) e inteiros.
    /// Qualquer outro tipo, inclusive <c>null</c>, <c>enum</c> sem <c>Display</c> e ponto flutuante, lança
    /// <see cref="InvalidOperationException"/>: são casos que exigem tradução explícita, e falhar alto
    /// evita uma divergência silenciosa (por exemplo, um <c>enum</c> C# imprimiria o nome do membro).
    /// </remarks>
    public bool WriteValue<T>(T value, bool alternate = false) =>
        value is IDisplay display ? WriteDisplay(display, alternate) : WritePrimitive(value);

    /// <summary>
    /// Rust: <c>value.fmt(f)</c> para um valor de tipo genérico: repassa ESTE formatador (e seus flags)
    /// quando o valor é <see cref="IDisplay"/>. Para os demais tipos aceitos por <see cref="WriteValue{T}"/>,
    /// o resultado é o mesmo de uma lacuna <c>{}</c>.
    /// </summary>
    public bool FmtInPlace<T>(T value)
    {
        if (value is not IDisplay display)
        {
            return WritePrimitive(value);
        }
        if (_root._error)
        {
            return false;
        }
        display.Fmt(this);
        return !_root._error;
    }

    private bool WritePrimitive<T>(T value)
    {
        switch (value)
        {
            case null:
                throw new InvalidOperationException(
                    "Valor nulo formatado como Display. No Rust não existe null: trate o Option explicitamente.");
            case string s:
                return WriteStr(s);
            case char c:
                return WriteChar(c);
            case bool b:
                return WriteStr(b ? "true" : "false");
            case Keyword keyword:
                return WriteStr(keyword.ToSql());
            case Enum e:
                throw new InvalidOperationException(
                    $"O enum C# '{e.GetType().Name}' não tem Display. Enums Rust com Display são traduzidos como records (convencoes.md §6).");
            case float or double or decimal:
                throw new InvalidOperationException(
                    "Ponto flutuante formatado como Display: o formato do Rust difere do .NET; trate explicitamente.");
            case sbyte or byte or short or ushort or int or uint or long or ulong:
                return WriteStr(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
            default:
                throw new InvalidOperationException(
                    $"O tipo '{value.GetType().FullName}' não implementa IDisplay e não tem formatação definida.");
        }
    }

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

    private sealed class StringBuilderWrite : IFmtWrite
    {
        private readonly StringBuilder _buffer;

        public StringBuilderWrite(StringBuilder buffer) => _buffer = buffer;

        public bool WriteStr(string value)
        {
            _buffer.Append(value);
            return true;
        }
    }
}

/// <summary>
/// Handler de interpolação de <see cref="Formatter.Write(ref FormatterInterpolatedStringHandler)"/>:
/// traduz <c>write!(f, "...", args)</c> escrevendo cada pedaço literal e cada lacuna no destino do
/// formatador, na ordem, sem montar uma string intermediária (como o <c>format_args!</c> do Rust).
/// A lacuna <c>{x:#}</c> corresponde a <c>{:#}</c> no Rust; nenhum outro especificador é aceito.
/// </summary>
[InterpolatedStringHandler]
public ref struct FormatterInterpolatedStringHandler
{
    private readonly Formatter _formatter;

    /// <summary>Construído pelo compilador para <c>f.Write($"...")</c>.</summary>
    public FormatterInterpolatedStringHandler(int literalLength, int formattedCount, Formatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>Trecho literal do formato.</summary>
    public bool AppendLiteral(string value) => _formatter.WriteStr(value);

    /// <summary>Lacuna <c>{x}</c>.</summary>
    public bool AppendFormatted<T>(T value) => _formatter.WriteValue(value);

    /// <summary>Lacuna <c>{x:#}</c> (Rust <c>{:#}</c>).</summary>
    public bool AppendFormatted<T>(T value, string? format)
    {
        if (format != "#")
        {
            throw new InvalidOperationException($"Especificador de formato não suportado: '{format}'. Use só '#'.");
        }
        return _formatter.WriteValue(value, alternate: true);
    }
}
