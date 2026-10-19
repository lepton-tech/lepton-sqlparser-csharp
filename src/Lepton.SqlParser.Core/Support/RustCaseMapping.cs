// Código próprio do port (sem origem no sqlparser-rs): busca nas tabelas de maiúsculas e minúsculas da
// biblioteca padrão do Rust (`core::unicode::conversions::to_lower`/`to_upper`, `char::is_cased` e
// `char::is_case_ignorable`). As tabelas ficam em RustCaseMapping.g.cs, gerado por
// tools/codegen/Generate-CaseMapping.ps1 a partir do Unicode Character Database. Ver docs/port/convencoes.md
// (§10 e §13, caso 139), docs/port/divergencias.md (DIV-001) e docs/port/onda-d/D6.md.

namespace Lepton.SqlParser.Core;

/// <summary>
/// Mapeamento de maiúsculas e minúsculas do Rust por scalar value: o mapeamento completo do Unicode
/// (UnicodeData.txt mais as regras incondicionais de SpecialCasing.txt), na versão do Unicode da toolchain
/// de referência (<see cref="UnicodeVersion"/>), igual em todos os alvos .NET.
/// </summary>
/// <remarks>
/// Os resultados seguem o <c>[char; 3]</c> do Rust: o primeiro elemento é sempre um scalar value; os demais
/// são 0 (o <c>'\0'</c> do Rust) quando o mapeamento tem menos de três. Um valor que não é scalar value (a
/// metade de um par substituto) volta inalterado.
/// </remarks>
internal static partial class RustCaseMapping
{
    /// <summary>Rust: <c>core::unicode::conversions::to_lower</c>.</summary>
    public static (int, int, int) ToLower(int c)
    {
        // https://util.unicode.org/UnicodeJsps/list-unicodeset.jsp?a=[:Changes_When_Lowercased:]-[:ASCII:]&abb=on
        if (c < 0xC0)
        {
            return (c >= 'A' && c <= 'Z' ? c + ('a' - 'A') : c, 0, 0);
        }

        return Lookup(c, LowerRuns, LowerMulti);
    }

    /// <summary>Rust: <c>core::unicode::conversions::to_upper</c>.</summary>
    public static (int, int, int) ToUpper(int c)
    {
        // https://util.unicode.org/UnicodeJsps/list-unicodeset.jsp?a=[:Changes_When_Uppercased:]-[:ASCII:]&abb=on
        if (c < 0xB5)
        {
            return (c >= 'a' && c <= 'z' ? c - ('a' - 'A') : c, 0, 0);
        }

        return Lookup(c, UpperRuns, UpperMulti);
    }

    /// <summary>Rust: <c>char::is_cased</c> (<c>Lowercase || Uppercase || Lt</c>).</summary>
    public static bool IsCased(int c)
    {
        if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
        {
            return true;
        }
        if (c <= 0xA9)
        {
            return false;
        }
        return InRanges(CasedRanges, c);
    }

    /// <summary>Rust: <c>char::is_case_ignorable</c>.</summary>
    public static bool IsCaseIgnorable(int c)
    {
        if (c <= 0x7F)
        {
            return c is '\'' or '.' or ':' or '^' or '`';
        }
        return InRanges(CaseIgnorableRanges, c);
    }

    // Rust: `lookup(c, &LUT).unwrap_or([c, '\0', '\0'])`. Primeiro a tabela dos mapeamentos múltiplos (chaves
    // ordenadas, quádruplas `c, a, b, c`); depois a das sequências (início, fim, passo, deslocamento), que não
    // se sobrepõem.
    private static (int, int, int) Lookup(int c, int[] runs, int[] multi)
    {
        var lo = 0;
        var hi = multi.Length / 4;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            var key = multi[mid * 4];
            if (key == c)
            {
                return (multi[(mid * 4) + 1], multi[(mid * 4) + 2], multi[(mid * 4) + 3]);
            }
            if (key < c)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        // A última sequência com início <= c.
        lo = 0;
        hi = runs.Length / 4;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (runs[mid * 4] <= c)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }
        if (lo > 0)
        {
            var r = (lo - 1) * 4;
            if (c <= runs[r + 1] && (runs[r + 2] == 1 || ((c - runs[r]) & 1) == 0))
            {
                return (c + runs[r + 3], 0, 0);
            }
        }
        return (c, 0, 0);
    }

    // Busca binária em pares ordenados e disjuntos (início, fim inclusive).
    private static bool InRanges(int[] ranges, int c)
    {
        var lo = 0;
        var hi = ranges.Length / 2;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (ranges[mid * 2] <= c)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }
        return lo > 0 && c <= ranges[((lo - 1) * 2) + 1];
    }
}
