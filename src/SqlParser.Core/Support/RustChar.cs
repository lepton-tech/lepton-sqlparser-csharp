// Código próprio do port (sem origem direta no sqlparser-rs): equivalentes mínimos de métodos de
// `char`, `u32` e `String` da biblioteca padrão do Rust que não têm correspondente exato no .NET
// (netstandard2.0). Ver docs/port/convencoes.md, seções 10 e 13.

using System.Text;

namespace SqlParser.Core;

/// <summary>
/// Equivalentes de métodos da biblioteca padrão do Rust para caracteres. Os nomes seguem os do Rust
/// (regra mecânica), para que a tradução continue buscável.
/// </summary>
/// <remarks>
/// Onde o Rust trabalha com um <c>char</c> que pode estar fora do BMP, estes métodos usam <c>int</c>
/// com o <b>scalar value</b> Unicode (ver convencoes.md §13).
/// </remarks>
internal static class RustChar
{
    /// <summary>Rust: <c>char::is_ascii_digit</c>.</summary>
    public static bool IsAsciiDigit(char c) => c >= '0' && c <= '9';

    /// <summary>Rust: <c>char::is_ascii_hexdigit</c>.</summary>
    public static bool IsAsciiHexdigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    /// <summary>Rust: <c>char::is_ascii_lowercase</c>.</summary>
    public static bool IsAsciiLowercase(char c) => c >= 'a' && c <= 'z';

    /// <summary>Rust: <c>char::is_ascii_uppercase</c>.</summary>
    public static bool IsAsciiUppercase(char c) => c >= 'A' && c <= 'Z';

    /// <summary>Rust: <c>u8::to_ascii_uppercase</c> / <c>char::to_ascii_uppercase</c> (só <c>a-z</c> muda).</summary>
    public static char ToAsciiUppercase(char c) => IsAsciiLowercase(c) ? (char)(c - ('a' - 'A')) : c;

    /// <summary>
    /// Rust: <c>char::to_lowercase</c> com o iterador (1 a 3 <c>char</c>s) reunido numa <c>string</c>: o
    /// mapeamento completo do scalar value <paramref name="c"/> para minúsculas (RustCaseMapping). Sem regra
    /// de contexto: U+03A3 vira sempre U+03C3 (o sigma final só existe em <c>str::to_lowercase</c>,
    /// <see cref="RustString.ToLowercase"/>).
    /// </summary>
    public static string ToLowercase(int c)
    {
        var s = new StringBuilder(3);
        RustString.Push(s, RustCaseMapping.ToLower(c));
        return s.ToString();
    }

    /// <summary>
    /// Rust: <c>char::to_uppercase</c> com o iterador (1 a 3 <c>char</c>s) reunido numa <c>string</c>: o
    /// mapeamento completo do scalar value <paramref name="c"/> para maiúsculas (RustCaseMapping).
    /// </summary>
    public static string ToUppercase(int c)
    {
        var s = new StringBuilder(3);
        RustString.Push(s, RustCaseMapping.ToUpper(c));
        return s.ToString();
    }

    /// <summary>
    /// Rust: <c>str::eq_ignore_ascii_case</c>: igualdade que ignora maiúsculas/minúsculas só em
    /// <c>a-z</c>/<c>A-Z</c>; os demais caracteres precisam ser idênticos (diferente de
    /// <c>StringComparison.OrdinalIgnoreCase</c>, que também dobra letras fora do ASCII).
    /// </summary>
    public static bool EqIgnoreAsciiCase(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }
        for (var i = 0; i < a.Length; i++)
        {
            if (ToAsciiUppercase(a[i]) != ToAsciiUppercase(b[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Rust: <c>&lt;str as Ord&gt;::cmp</c>: ordem lexicográfica dos bytes UTF-8, que é a ordem dos
    /// scalar values. Devolve -1, 0 ou 1 (<c>Ordering::Less</c>, <c>Equal</c>, <c>Greater</c>).
    /// </summary>
    /// <remarks>
    /// <c>string.CompareOrdinal</c> compara unidades UTF-16 e só difere dessa ordem quando um caractere
    /// fora do BMP (par substituto, U+D800..U+DFFF) encontra um caractere de U+E000..U+FFFF na mesma
    /// posição: o par representa um scalar maior. Nesse caso as duas unidades são deslocadas para que os
    /// substitutos fiquem acima de U+E000..U+FFFF (a correção de "code point order" do UTF-16).
    /// </remarks>
    public static int Cmp(string a, string b)
    {
        var n = Math.Min(a.Length, b.Length);
        for (var i = 0; i < n; i++)
        {
            int ca = a[i];
            int cb = b[i];
            if (ca == cb)
            {
                continue;
            }
            if (ca >= 0xD800 && cb >= 0xD800)
            {
                ca = ca >= 0xE000 ? ca - 0x800 : ca + 0x2000;
                cb = cb >= 0xE000 ? cb - 0x800 : cb + 0x2000;
            }
            return ca < cb ? -1 : 1;
        }
        return a.Length.CompareTo(b.Length);
    }

    /// <summary>
    /// Rust: <c>char::to_digit(radix)</c> (radix 2..=36): o valor do dígito ou <c>null</c>.
    /// Só dígitos ASCII e letras ASCII contam, como no Rust.
    /// </summary>
    public static uint? ToDigit(char c, uint radix)
    {
        uint digit;
        if (c >= '0' && c <= '9')
        {
            digit = (uint)(c - '0');
        }
        else if (c >= 'a' && c <= 'z')
        {
            digit = (uint)(c - 'a') + 10;
        }
        else if (c >= 'A' && c <= 'Z')
        {
            digit = (uint)(c - 'A') + 10;
        }
        else
        {
            return null;
        }
        return digit < radix ? digit : null;
    }

    /// <summary>
    /// Rust: <c>char::from_u32(n)</c>: o scalar value, ou <c>null</c> se <paramref name="n"/> for um
    /// substituto (U+D800..U+DFFF) ou maior que U+10FFFF.
    /// </summary>
    public static int? FromU32(uint n)
    {
        if (n > 0x10FFFF || (n >= 0xD800 && n <= 0xDFFF))
        {
            return null;
        }
        return (int)n;
    }

    /// <summary>
    /// Rust: <c>u32::from_str_radix(s, radix)</c>: aceita um <c>+</c> inicial opcional; vazio, dígito
    /// inválido ou estouro → <c>null</c> (o <c>Err</c> do Rust).
    /// </summary>
    public static uint? U32FromStrRadix(string s, uint radix)
    {
        var start = 0;
        if (s.Length > 1 && s[0] == '+')
        {
            start = 1;
        }
        if (s.Length - start == 0)
        {
            return null;
        }

        ulong result = 0;
        for (var i = start; i < s.Length; i++)
        {
            if (ToDigit(s[i], radix) is not { } digit)
            {
                return null;
            }
            result = result * radix + digit;
            if (result > uint.MaxValue)
            {
                return null;
            }
        }
        return (uint)result;
    }

    /// <summary>
    /// Rust: <c>String::push(c)</c> com um <c>char</c> dado pelo scalar value (1 ou 2 unidades UTF-16).
    /// </summary>
    public static void PushScalar(StringBuilder s, int scalar)
    {
        if (scalar <= 0xFFFF)
        {
            s.Append((char)scalar);
        }
        else
        {
            s.Append(char.ConvertFromUtf32(scalar));
        }
    }
}
