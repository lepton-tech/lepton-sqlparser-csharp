// Código próprio do port (sem origem no sqlparser-rs): `str::to_lowercase` e `str::to_uppercase` da
// biblioteca padrão do Rust (library/alloc/src/str.rs), traduzidos sobre as tabelas de RustCaseMapping
// (Unicode da toolchain de referência). Substituem `ToLowerInvariant`/`ToUpperInvariant`, que aplicam só o
// mapeamento simples, com a tabela de cada runtime. Ver docs/port/convencoes.md (§10 e §13, caso 139),
// docs/port/divergencias.md (DIV-001, resolvida) e docs/port/onda-d/D6.md.

using System.Text;

namespace Lepton.SqlParser.Core;

/// <summary>
/// Equivalentes de métodos de <c>str</c>/<c>String</c> da biblioteca padrão do Rust. Os nomes seguem os do
/// Rust (regra mecânica), para que a tradução continue buscável.
/// </summary>
/// <remarks>
/// As funções percorrem scalar values (um par substituto é um caractere, como o <c>char</c> do Rust). Uma
/// metade de par substituto sozinha, que não existe num <c>&amp;str</c> do Rust, é copiada sem mudança.
/// </remarks>
internal static class RustString
{
    private const int GreekCapitalLetterSigma = 0x03A3;
    private const char GreekSmallLetterSigma = (char)0x03C3;
    private const char GreekSmallLetterFinalSigma = (char)0x03C2;

    /// <summary>
    /// Rust: <c>str::to_lowercase</c>: mapeamento completo para minúsculas, com a regra contextual do sigma
    /// final (<c>Final_Sigma</c>: U+03A3 vira U+03C2 no fim de palavra e U+03C3 nos demais casos).
    /// </summary>
    public static string ToLowercase(string s)
    {
        // Rust: `convert_while_ascii(self, u8::to_ascii_lowercase)`: o prefixo ASCII é convertido byte a byte.
        // Atalho do port: uma string toda ASCII sem maiúsculas volta como está (o resultado é igual ao do Rust,
        // e `string` é imutável).
        var i = 0;
        while (i < s.Length && s[i] <= 0x7F && !RustChar.IsAsciiUppercase(s[i]))
        {
            i++;
        }
        if (i == s.Length)
        {
            return s;
        }

        // O prefixo ASCII vai para um `char[]` (mais barato que `StringBuilder.Append` por caractere); só o
        // resto, se houver, passa pelo `StringBuilder`, porque o mapeamento completo pode mudar o tamanho.
        var prefix = new char[s.Length];
        s.CopyTo(0, prefix, 0, i);
        while (i < s.Length && s[i] <= 0x7F)
        {
            var ch = s[i];
            prefix[i] = RustChar.IsAsciiUppercase(ch) ? (char)(ch + ('a' - 'A')) : ch;
            i++;
        }
        if (i == s.Length)
        {
            return new string(prefix);
        }

        var result = new StringBuilder(s.Length + 16);
        result.Append(prefix, 0, i);

        // Rust: `for (i, c) in rest.char_indices()`; `i` aqui já é o índice na string inteira (o
        // `prefix_len + i` do Rust).
        while (i < s.Length)
        {
            var c = ScalarAt(s, i, out var width);
            if (c == GreekCapitalLetterSigma)
            {
                // (Comentário do Rust, com as letras gregas escritas como code points.)
                // U+03A3 maps to U+03C3, except at the end of a word where it maps to U+03C2.
                // This is the only conditional (contextual) but language-independent mapping
                // in `SpecialCasing.txt`,
                // so hard-code it rather than have a generic "condition" mechanism.
                // See https://github.com/rust-lang/rust/issues/26035
                result.Append(MapUppercaseSigma(s, i));
            }
            else
            {
                Push(result, RustCaseMapping.ToLower(c));
            }
            i += width;
        }
        return result.ToString();
    }

    /// <summary>Rust: <c>str::to_uppercase</c>: mapeamento completo para maiúsculas.</summary>
    public static string ToUppercase(string s)
    {
        // Rust: `convert_while_ascii(self, u8::to_ascii_uppercase)`; o atalho é o mesmo de ToLowercase.
        var i = 0;
        while (i < s.Length && s[i] <= 0x7F && !RustChar.IsAsciiLowercase(s[i]))
        {
            i++;
        }
        if (i == s.Length)
        {
            return s;
        }

        var prefix = new char[s.Length];
        s.CopyTo(0, prefix, 0, i);
        while (i < s.Length && s[i] <= 0x7F)
        {
            prefix[i] = RustChar.ToAsciiUppercase(s[i]);
            i++;
        }
        if (i == s.Length)
        {
            return new string(prefix);
        }

        var result = new StringBuilder(s.Length + 16);
        result.Append(prefix, 0, i);

        // Rust: `for c in rest.chars()`.
        while (i < s.Length)
        {
            var c = ScalarAt(s, i, out var width);
            Push(result, RustCaseMapping.ToUpper(c));
            i += width;
        }
        return result.ToString();
    }

    // Rust: `match conversions::to_lower(c) { [a, '\0', _] => .., [a, b, '\0'] => .., [a, b, c] => .. }`.
    internal static void Push(StringBuilder s, (int, int, int) mapping)
    {
        var (a, b, c) = mapping;
        RustChar.PushScalar(s, a);
        if (b == 0)
        {
            return;
        }
        RustChar.PushScalar(s, b);
        if (c != 0)
        {
            RustChar.PushScalar(s, c);
        }
    }

    // Rust: `fn map_uppercase_sigma(from: &str, i: usize) -> char` (`i` é o índice do U+03A3).
    private static char MapUppercaseSigma(string from, int i)
    {
        // See https://www.unicode.org/versions/latest/core-spec/chapter-3/#G54277
        // for the definition of `Final_Sigma`.
        // Rust: `case_ignorable_then_cased(from[..i].chars().rev())
        //     && !case_ignorable_then_cased(from[i + const { SIGMA.len_utf8() }..].chars())`, com SIGMA no
        // lugar do literal U+03A3, que tem 2 bytes em UTF-8 e 1 unidade em UTF-16 (o `i + 1` daqui).
        var isWordFinal = CaseIgnorableThenCasedBackward(from, i) && !CaseIgnorableThenCasedForward(from, i + 1);
        return isWordFinal ? GreekSmallLetterFinalSigma : GreekSmallLetterSigma;
    }

    // Rust: `case_ignorable_then_cased(iter)` = `match iter.skip_while(|&c| c.is_case_ignorable()).next() {
    // Some(c) => c.is_cased(), None => false }`, com `iter` = os scalar values de `s[start..]`.
    private static bool CaseIgnorableThenCasedForward(string s, int start)
    {
        var i = start;
        while (i < s.Length)
        {
            var c = ScalarAt(s, i, out var width);
            if (!RustCaseMapping.IsCaseIgnorable(c))
            {
                return RustCaseMapping.IsCased(c);
            }
            i += width;
        }
        return false;
    }

    // O mesmo, com `iter` = os scalar values de `s[..end]` de trás para frente (`.chars().rev()`).
    private static bool CaseIgnorableThenCasedBackward(string s, int end)
    {
        var i = end;
        while (i > 0)
        {
            int c = s[i - 1];
            var width = 1;
            if (char.IsLowSurrogate(s[i - 1]) && i >= 2 && char.IsHighSurrogate(s[i - 2]))
            {
                c = char.ConvertToUtf32(s[i - 2], s[i - 1]);
                width = 2;
            }
            if (!RustCaseMapping.IsCaseIgnorable(c))
            {
                return RustCaseMapping.IsCased(c);
            }
            i -= width;
        }
        return false;
    }

    // O scalar value que começa em `s[i]` e quantas unidades UTF-16 ele ocupa (2 para um par substituto).
    private static int ScalarAt(string s, int i, out int width)
    {
        var ch = s[i];
        if (char.IsHighSurrogate(ch) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
        {
            width = 2;
            return char.ConvertToUtf32(ch, s[i + 1]);
        }
        width = 1;
        return ch;
    }
}
