// Testes próprios do port (não existem no sqlparser-rs): medem, em cada alvo (net48, net8.0, net10.0), o que
// `ToLowerInvariant`/`ToUpperInvariant` fazem nos casos especiais do Unicode e documentam a diferença com o
// `to_lowercase()`/`to_uppercase()` do Rust (divergencias.md, DIV-001; docs/port/onda-d/D5.md).
//
// As asserções fixam o comportamento ATUAL do .NET. O resultado do Rust fica no comentário de cada caso; ele
// vem da documentação da std (`char::to_lowercase`: U+0130 vira "i\u{307}"; `char::to_uppercase`: U+00DF
// vira "SS"; `str::to_lowercase`: U+03A3 vira U+03C2 no fim de palavra) e das tabelas do Unicode que a std
// aplica (UnicodeData.txt e as regras incondicionais de SpecialCasing.txt). O .NET aplica só o mapeamento
// simples, um `char` por `char`, e cada runtime usa a sua tabela: NLS no .NET Framework 4.8, a do próprio
// runtime no .NET 8/10.
//
// Os caracteres são montados pelo código numérico (`(char)0x0130`), sem escapes nem caracteres literais no
// código-fonte (ver a armadilha das ferramentas de edição na skill `port-rust-to-csharp`).

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

public class Div001Tests
{
    private const char LatinCapitalLetterIWithDotAbove = (char)0x0130;
    private const char CombiningDotAbove = (char)0x0307;
    private const char LatinSmallLetterDotlessI = (char)0x0131;
    private const char LatinSmallLetterSharpS = (char)0x00DF;
    private const char LatinCapitalLetterSharpS = (char)0x1E9E;
    private const char GreekCapitalLetterSigma = (char)0x03A3;
    private const char GreekSmallLetterFinalSigma = (char)0x03C2;
    private const char GreekSmallLetterSigma = (char)0x03C3;
    private const char LatinSmallLetterLongS = (char)0x017F;
    private const char KelvinSign = (char)0x212A;
    private const char LatinSmallLigatureFi = (char)0xFB01;

    private static string S(params char[] chars) => new string(chars);

    [Fact]
    public void U0130_minusculo_fica_igual_em_todos_os_alvos()
    {
        // Rust: U+0130 em `to_lowercase()` vira "i\u{307}" (dois caracteres, mapeamento completo).
        // .NET (net48, net8.0, net10.0): U+0130 fica como está; nem o mapeamento simples (`i`) é aplicado.
        var rust = S('i', CombiningDotAbove);
        Assert.Equal(S(LatinCapitalLetterIWithDotAbove), S(LatinCapitalLetterIWithDotAbove).ToLowerInvariant());
        Assert.NotEqual(rust, S(LatinCapitalLetterIWithDotAbove).ToLowerInvariant());

        // Nos dois lados o resultado não é ASCII, então a comparação com um nome ASCII dá o mesmo resultado:
        // no Rust, "i\u{307}DENTIFIER" minúsculo != "identifier"; no .NET, U+0130 + "dentifier" != "identifier".
        var identifier = S(LatinCapitalLetterIWithDotAbove) + "DENTIFIER";
        Assert.NotEqual("identifier", identifier.ToLowerInvariant());
        Assert.False(new SnowflakeDialect().IsIdentifierGeneratingFunctionName(
            new Ident(identifier), System.Array.Empty<ObjectNamePart>()));
        Assert.True(new SnowflakeDialect().IsIdentifierGeneratingFunctionName(
            new Ident("IDENTIFIER"), System.Array.Empty<ObjectNamePart>()));
    }

    [Fact]
    public void U00DF_maiusculo_fica_igual_em_todos_os_alvos()
    {
        // Rust: U+00DF em `to_uppercase()` vira "SS"; "pa" + U+00DF + "word" vira "PASSWORD", que é keyword.
        // .NET (net48, net8.0, net10.0): U+00DF fica como está, e o resultado não é keyword.
        Assert.Equal(S(LatinSmallLetterSharpS), S(LatinSmallLetterSharpS).ToUpperInvariant());
        Assert.NotEqual("SS", S(LatinSmallLetterSharpS).ToUpperInvariant());

        var password = "pa" + S(LatinSmallLetterSharpS) + "word";
        Assert.Equal("PA" + S(LatinSmallLetterSharpS) + "WORD", password.ToUpperInvariant());
        Assert.Equal(Keyword.NoKeyword, Keywords.Lookup(password.ToUpperInvariant()));
        Assert.Equal(Keyword.PASSWORD, Keywords.Lookup("PASSWORD"));
    }

    [Fact]
    public void U03A3_minusculo_ignora_o_sigma_final()
    {
        // Rust: U+03A3 sozinho vira U+03C3; no fim de palavra, `str::to_lowercase` dá U+03C2 (sigma final):
        // "A" + U+03A3 vira "a" + U+03C2.
        // .NET (net48, net8.0, net10.0): sempre U+03C3, sem olhar o contexto.
        Assert.Equal(S(GreekSmallLetterSigma), S(GreekCapitalLetterSigma).ToLowerInvariant());
        Assert.Equal(S('a', GreekSmallLetterSigma), S('A', GreekCapitalLetterSigma).ToLowerInvariant());
        Assert.NotEqual(S('a', GreekSmallLetterFinalSigma), S('A', GreekCapitalLetterSigma).ToLowerInvariant());
    }

    [Fact]
    public void Casos_que_dependem_do_alvo()
    {
        // Mapeamentos simples que o Rust aplica e o .NET Framework 4.8 (NLS) não aplica; o .NET 8/10 aplica.
        // Rust: U+017F → `S`; U+212A (sinal de Kelvin) → `k`; U+03C2 → U+03A3; U+1E9E → U+00DF.
#if NETFRAMEWORK
        Assert.Equal(S(LatinSmallLetterLongS), S(LatinSmallLetterLongS).ToUpperInvariant());
        Assert.Equal(S(KelvinSign), S(KelvinSign).ToLowerInvariant());
        Assert.Equal(S(GreekSmallLetterFinalSigma), S(GreekSmallLetterFinalSigma).ToUpperInvariant());
        Assert.Equal(S(LatinCapitalLetterSharpS), S(LatinCapitalLetterSharpS).ToLowerInvariant());
#else
        Assert.Equal("S", S(LatinSmallLetterLongS).ToUpperInvariant());
        Assert.Equal("k", S(KelvinSign).ToLowerInvariant());
        Assert.Equal(S(GreekCapitalLetterSigma), S(GreekSmallLetterFinalSigma).ToUpperInvariant());
        Assert.Equal(S(LatinSmallLetterSharpS), S(LatinCapitalLetterSharpS).ToLowerInvariant());
#endif

        // Mapeamentos que nenhum alvo .NET aplica.
        // Rust: U+0131 → `I` (o .NET exclui de propósito o par turco do mapeamento invariante); U+FB01 → "FI"
        // (mapeamento completo, SpecialCasing.txt).
        Assert.Equal(S(LatinSmallLetterDotlessI), S(LatinSmallLetterDotlessI).ToUpperInvariant());
        Assert.Equal(S(LatinSmallLigatureFi), S(LatinSmallLigatureFi).ToUpperInvariant());
    }

    [Fact]
    public void Efeito_em_DialectFromStr()
    {
        // Rust: "SNOWFLA" + U+212A + "E" minúsculo dá "snowflake" → Some(SnowflakeDialect).
        // .NET 8/10: igual. .NET Framework 4.8: o U+212A fica, e o resultado é None.
        var name = "SNOWFLA" + S(KelvinSign) + "E";
#if NETFRAMEWORK
        Assert.Null(Dialect.DialectFromStr(name));
#else
        Assert.IsType<SnowflakeDialect>(Dialect.DialectFromStr(name));
#endif
    }

    [Fact]
    public void Efeito_no_STORAGE_do_MySql()
    {
        // `TABLESPACE .. STORAGE <palavra>` compara `w.value.to_uppercase()` com "DISK"/"MEMORY".
        const string prefix = "CREATE TABLE foo (bar INT) TABLESPACE my_tablespace STORAGE ";
        Assert.Contains(
            "STORAGE DISK",
            Parser.ParseSql(new MySqlDialect(), prefix + "disk")[0].ToString());

        // Rust: "d" + U+0131 + "sk" maiúsculo dá "DISK" → StorageType::Disk. .NET (todos os alvos): erro.
        var dotless = "d" + S(LatinSmallLetterDotlessI) + "sk";
        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(new MySqlDialect(), prefix + dotless));
        Assert.Contains("Expected: Storage type (DISK or MEMORY), found: " + dotless, ex.Message);

        // Rust: "di" + U+017F + "k" maiúsculo dá "DISK". .NET 8/10: igual. .NET Framework 4.8: erro.
        var longS = "di" + S(LatinSmallLetterLongS) + "k";
#if NETFRAMEWORK
        Assert.Throws<ParserException>(() => Parser.ParseSql(new MySqlDialect(), prefix + longS));
#else
        Assert.Contains(
            "STORAGE DISK",
            Parser.ParseSql(new MySqlDialect(), prefix + longS)[0].ToString());
#endif
    }
}
