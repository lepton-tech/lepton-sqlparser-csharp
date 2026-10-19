// Testes próprios do port (não existem no sqlparser-rs): conferem que RustString/RustChar/RustCaseMapping
// reproduzem `str::to_lowercase`/`str::to_uppercase` e `char::to_lowercase`/`char::to_uppercase` do Rust
// (mapeamento completo do Unicode, regra do sigma final), com o mesmo resultado em net48, net8.0 e net10.0
// (divergencias.md, DIV-001, resolvida na D6; docs/port/onda-d/D6.md).
//
// Os resultados esperados são os do Rust: documentação da std (`char::to_lowercase`: U+0130 vira
// "i\u{307}"; `char::to_uppercase`: U+00DF vira "SS"; `str::to_lowercase`: U+03A3 vira U+03C2 no fim de
// palavra) e as tabelas que a std gera do Unicode (UnicodeData.txt, as regras incondicionais de
// SpecialCasing.txt e DerivedCoreProperties.txt). Na D6, as tabelas também foram comparadas, code point a
// code point, com os dados de teste do próprio Rust 1.99.0 (library/coretests/tests/unicode/test_data.rs).
//
// Os caracteres são montados pelo código numérico (`(char)0x0130`, `char.ConvertFromUtf32`), sem escapes
// nem caracteres literais no código-fonte (ver a armadilha das ferramentas de edição na skill
// `port-rust-to-csharp`).

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

public class Div001Tests
{
    private const char LatinCapitalLetterIWithDotAbove = (char)0x0130;
    private const char CombiningDotAbove = (char)0x0307;
    private const char CombiningAcuteAccent = (char)0x0301;
    private const char CombiningDiaeresis = (char)0x0308;
    private const char LatinSmallLetterDotlessI = (char)0x0131;
    private const char LatinSmallLetterSharpS = (char)0x00DF;
    private const char LatinCapitalLetterSharpS = (char)0x1E9E;
    private const char LatinSmallLetterNPrecededByApostrophe = (char)0x0149;
    private const char ModifierLetterApostrophe = (char)0x02BC;
    private const char GreekCapitalLetterSigma = (char)0x03A3;
    private const char GreekSmallLetterFinalSigma = (char)0x03C2;
    private const char GreekSmallLetterSigma = (char)0x03C3;
    private const char GreekCapitalLetterAlpha = (char)0x0391;
    private const char GreekSmallLetterAlpha = (char)0x03B1;
    private const char GreekCapitalLetterIota = (char)0x0399;
    private const char GreekSmallLetterIotaWithDialytikaAndTonos = (char)0x0390;
    private const char GreekSmallLetterAlphaWithPsiliAndYpogegrammeni = (char)0x1F80;
    private const char GreekCapitalLetterAlphaWithPsili = (char)0x1F08;
    private const char LatinSmallLetterLongS = (char)0x017F;
    private const char KelvinSign = (char)0x212A;
    private const char LatinSmallLigatureFi = (char)0xFB01;
    private const char LatinSmallLigatureFfi = (char)0xFB03;
    private const int DeseretCapitalLetterLongI = 0x10400;
    private const int DeseretSmallLetterLongI = 0x10428;

    private static string S(params char[] chars) => new string(chars);

    private static string Scalar(int c) => char.ConvertFromUtf32(c);

    [Fact]
    public void Unicode_da_toolchain_de_referencia()
    {
        // Rust 1.99.0 (estável em 2026-10): `char::UNICODE_VERSION` = (17, 0, 0).
        Assert.Equal("17.0.0", RustCaseMapping.UnicodeVersion);
    }

    [Fact]
    public void U0130_minusculo_vira_i_com_ponto_combinante()
    {
        // Rust: U+0130 em `to_lowercase()` vira "i\u{307}" (dois caracteres, SpecialCasing.txt).
        var rust = S('i', CombiningDotAbove);
        Assert.Equal(rust, RustString.ToLowercase(S(LatinCapitalLetterIWithDotAbove)));
        Assert.Equal(rust, RustChar.ToLowercase(LatinCapitalLetterIWithDotAbove));
        Assert.Equal(S(LatinCapitalLetterIWithDotAbove), RustString.ToUppercase(S(LatinCapitalLetterIWithDotAbove)));

        // "i\u{307}dentifier" != "identifier": como no Rust, não é a função IDENTIFIER do Snowflake.
        var identifier = S(LatinCapitalLetterIWithDotAbove) + "DENTIFIER";
        Assert.Equal(rust + "dentifier", RustString.ToLowercase(identifier));
        Assert.False(new SnowflakeDialect().IsIdentifierGeneratingFunctionName(
            new Ident(identifier), System.Array.Empty<ObjectNamePart>()));
        Assert.True(new SnowflakeDialect().IsIdentifierGeneratingFunctionName(
            new Ident("IDENTIFIER"), System.Array.Empty<ObjectNamePart>()));
    }

    [Fact]
    public void U00DF_maiusculo_vira_SS()
    {
        // Rust: U+00DF em `to_uppercase()` vira "SS"; "pa" + U+00DF + "word" vira "PASSWORD", que é keyword.
        Assert.Equal("SS", RustString.ToUppercase(S(LatinSmallLetterSharpS)));
        Assert.Equal("SS", RustChar.ToUppercase(LatinSmallLetterSharpS));
        Assert.Equal(S(LatinSmallLetterSharpS), RustString.ToLowercase(S(LatinSmallLetterSharpS)));

        var password = "pa" + S(LatinSmallLetterSharpS) + "word";
        Assert.Equal("PASSWORD", RustString.ToUppercase(password));
        Assert.Equal(Keyword.PASSWORD, Keywords.Lookup(RustString.ToUppercase(password)));
    }

    [Fact]
    public void Sigma_final()
    {
        var sigma = S(GreekCapitalLetterSigma);
        var small = S(GreekSmallLetterSigma);
        var final = S(GreekSmallLetterFinalSigma);

        // Sem letra antes: U+03C3.
        Assert.Equal(small, RustString.ToLowercase(sigma));
        // Fim de palavra (letra com caixa antes, nenhuma depois): U+03C2.
        Assert.Equal("a" + final, RustString.ToLowercase("A" + sigma));
        Assert.Equal(S(GreekSmallLetterAlpha, GreekSmallLetterFinalSigma),
            RustString.ToLowercase(S(GreekCapitalLetterAlpha, GreekCapitalLetterSigma)));
        // Meio de palavra: U+03C3.
        Assert.Equal("a" + small + "b", RustString.ToLowercase("A" + sigma + "B"));
        // Caracteres "case-ignorable" (apóstrofo, ponto, acento combinante) são pulados nos dois sentidos.
        Assert.Equal("a'" + final, RustString.ToLowercase("A'" + sigma));
        Assert.Equal("a" + final + ".", RustString.ToLowercase("A" + sigma + "."));
        Assert.Equal("a" + small + "'b", RustString.ToLowercase("A" + sigma + "'B"));
        Assert.Equal(S('a', CombiningAcuteAccent, GreekSmallLetterFinalSigma),
            RustString.ToLowercase(S('A', CombiningAcuteAccent, GreekCapitalLetterSigma)));
        // Um espaço não é "case-ignorable" nem tem caixa: a palavra anterior não conta.
        Assert.Equal("a " + small, RustString.ToLowercase("A " + sigma));
        // O primeiro sigma não tem letra antes; o segundo tem e é o último.
        Assert.Equal(small + final, RustString.ToLowercase(sigma + sigma));
        // Letra com caixa fora do BMP antes do sigma.
        Assert.Equal(Scalar(DeseretSmallLetterLongI) + final,
            RustString.ToLowercase(Scalar(DeseretCapitalLetterLongI) + sigma));
        // Prefixo ASCII longo (o `convert_while_ascii` do Rust processa blocos de 16 bytes).
        Assert.Equal("abcdefghijklmnopqrstuvwxyz" + final, RustString.ToLowercase("ABCDEFGHIJKLMNOPQRSTUVWXYZ" + sigma));
        // `char::to_lowercase` não olha o contexto.
        Assert.Equal(small, RustChar.ToLowercase(GreekCapitalLetterSigma));
    }

    [Fact]
    public void Mapeamentos_simples_iguais_em_todos_os_alvos()
    {
        // Na D5, o .NET Framework 4.8 (NLS) não aplicava os quatro primeiros, e nenhum alvo aplicava os
        // dois últimos. O resultado agora é o do Rust nos três alvos.
        Assert.Equal("S", RustString.ToUppercase(S(LatinSmallLetterLongS)));
        Assert.Equal("k", RustString.ToLowercase(S(KelvinSign)));
        Assert.Equal(S(GreekCapitalLetterSigma), RustString.ToUppercase(S(GreekSmallLetterFinalSigma)));
        Assert.Equal(S(LatinSmallLetterSharpS), RustString.ToLowercase(S(LatinCapitalLetterSharpS)));
        Assert.Equal("I", RustString.ToUppercase(S(LatinSmallLetterDotlessI)));
        Assert.Equal("FI", RustString.ToUppercase(S(LatinSmallLigatureFi)));
    }

    [Fact]
    public void Mapeamentos_completos_de_dois_e_tres_caracteres()
    {
        Assert.Equal("FFI", RustString.ToUppercase(S(LatinSmallLigatureFfi)));
        Assert.Equal(S(ModifierLetterApostrophe, 'N'), RustString.ToUppercase(S(LatinSmallLetterNPrecededByApostrophe)));
        Assert.Equal(
            S(GreekCapitalLetterIota, CombiningDiaeresis, CombiningAcuteAccent),
            RustString.ToUppercase(S(GreekSmallLetterIotaWithDialytikaAndTonos)));
        Assert.Equal(
            S(GreekCapitalLetterAlphaWithPsili, GreekCapitalLetterIota),
            RustChar.ToUppercase(GreekSmallLetterAlphaWithPsiliAndYpogegrammeni));
        Assert.Equal(
            "X" + S(GreekCapitalLetterAlphaWithPsili, GreekCapitalLetterIota) + "Y",
            RustString.ToUppercase("x" + S(GreekSmallLetterAlphaWithPsiliAndYpogegrammeni) + "y"));
        // Os mapeamentos de `RustCaseMapping` seguem o `[char; 3]` do Rust (0 = `'\0'`).
        Assert.Equal((0x0399, 0x0308, 0x0301), RustCaseMapping.ToUpper(GreekSmallLetterIotaWithDialytikaAndTonos));
        Assert.Equal((0x0069, 0x0307, 0), RustCaseMapping.ToLower(LatinCapitalLetterIWithDotAbove));
        Assert.Equal((0x00E0, 0, 0), RustCaseMapping.ToLower(0x00C0));
        Assert.Equal((0x0101, 0, 0), RustCaseMapping.ToLower(0x0100));
        Assert.Equal((0x0101, 0, 0), RustCaseMapping.ToLower(0x0101));
    }

    [Fact]
    public void Fora_do_BMP_e_substitutos_sozinhos()
    {
        Assert.Equal(Scalar(DeseretCapitalLetterLongI), RustString.ToUppercase(Scalar(DeseretSmallLetterLongI)));
        Assert.Equal(Scalar(DeseretSmallLetterLongI), RustString.ToLowercase(Scalar(DeseretCapitalLetterLongI)));
        Assert.Equal(Scalar(DeseretCapitalLetterLongI), RustChar.ToUppercase(DeseretSmallLetterLongI));
        Assert.Equal("A" + Scalar(DeseretCapitalLetterLongI) + "B",
            RustString.ToUppercase("a" + Scalar(DeseretSmallLetterLongI) + "b"));

        // Uma metade de par substituto sozinha (impossível num `&str` do Rust) é copiada sem mudança.
        var high = S((char)0xD801);
        Assert.Equal("A" + high + "B", RustString.ToUppercase("a" + high + "b"));
        Assert.Equal("a" + high, RustString.ToLowercase("A" + high));
    }

    [Fact]
    public void Atalho_ASCII()
    {
        // Sem nada a mudar, a mesma instância volta (o resultado é igual ao do Rust; string é imutável).
        const string lower = "select_1 from t";
        const string upper = "SELECT_1 FROM T";
        Assert.Same(lower, RustString.ToLowercase(lower));
        Assert.Same(upper, RustString.ToUppercase(upper));
        Assert.Same(string.Empty, RustString.ToLowercase(string.Empty));
        Assert.Equal("select", RustString.ToLowercase("SeLeCt"));
        Assert.Equal("SELECT", RustString.ToUppercase("SeLeCt"));
        Assert.Equal("~[]{}@^_`", RustString.ToUppercase("~[]{}@^_`"));
    }

    [Fact]
    public void Efeito_em_DialectFromStr()
    {
        // Rust: "SNOWFLA" + U+212A + "E" minúsculo dá "snowflake" → Some(SnowflakeDialect). Na D5, o .NET
        // Framework 4.8 dava None.
        Assert.IsType<SnowflakeDialect>(Dialect.DialectFromStr("SNOWFLA" + S(KelvinSign) + "E"));
    }

    [Fact]
    public void Efeito_no_STORAGE_do_MySql()
    {
        // `TABLESPACE .. STORAGE <palavra>` compara `w.value.to_uppercase()` com "DISK"/"MEMORY".
        const string prefix = "CREATE TABLE foo (bar INT) TABLESPACE my_tablespace STORAGE ";
        Assert.Contains(
            "STORAGE DISK",
            Parser.ParseSql(new MySqlDialect(), prefix + "disk")[0].ToString());

        // Rust: "d" + U+0131 + "sk" e "di" + U+017F + "k" maiúsculos dão "DISK" → StorageType::Disk.
        Assert.Contains(
            "STORAGE DISK",
            Parser.ParseSql(new MySqlDialect(), prefix + "d" + S(LatinSmallLetterDotlessI) + "sk")[0].ToString());
        Assert.Contains(
            "STORAGE DISK",
            Parser.ParseSql(new MySqlDialect(), prefix + "di" + S(LatinSmallLetterLongS) + "k")[0].ToString());

        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(new MySqlDialect(), prefix + "dusk"));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: Storage type (DISK or MEMORY), found: dusk at Line: 1, Column: 61"),
            ex.Error);
    }

    [Fact]
    public void Efeito_no_DECLARE_do_Snowflake()
    {
        // Achado do fuzzing da D3: `DECLARE x INT; "<U+FB01>rst" INT`. O Rust testa se a palavra depois do `;`
        // é keyword com `ALL_KEYWORDS.binary_search(&w.value.to_uppercase())`: "<U+FB01>rst" vira "FIRST", que
        // é keyword, então o `;` é devolvido e o DECLARE termina em `x INT`. A instrução seguinte começa por um
        // identificador entre aspas (keyword NoKeyword), e `parse_statement` falha em
        // `self.expected("an SQL statement", next_token)`, com a posição do token (`Parser::parse_sql` usa
        // `try_with_sql`, que guarda as posições).
        foreach (var word in new[] { S(LatinSmallLigatureFi) + "rst", "pa" + S(LatinSmallLetterSharpS) + "word", "first" })
        {
            var ex = Assert.Throws<ParserException>(
                () => Parser.ParseSql(new SnowflakeDialect(), "DECLARE x INT; \"" + word + "\" INT"));
            Assert.Equal(
                new ParserError.ParserErrorValue(
                    "Expected: an SQL statement, found: \"" + word + "\" at Line: 1, Column: 16"),
                ex.Error);
        }

        // "<U+FB01>rstname" vira "FIRSTNAME", que não é keyword: a segunda declaração continua no mesmo DECLARE.
        var name = S(LatinSmallLigatureFi) + "rstname";
        var statements = Parser.ParseSql(new SnowflakeDialect(), "DECLARE x INT; \"" + name + "\" INT");
        var declare = Assert.IsType<Statement.Declare>(Assert.Single(statements));
        Assert.Equal(2, declare.Stmts.Count);
        Assert.Equal(name, declare.Stmts[1].Names[0].Value);
        Assert.Equal('"', declare.Stmts[1].Names[0].QuoteStyle);
    }

    [Fact]
    public void Busca_de_keywords_do_tokenizer_so_usa_ASCII()
    {
        // Rust: `keyword_lookup` compara com `to_ascii_uppercase` byte a byte; U+00DF e as ligaduras não
        // mudam, e a palavra sem aspas não é keyword (só `Keywords.Lookup` sobre `to_uppercase()` a vê).
        var password = "pa" + S(LatinSmallLetterSharpS) + "word";
        var first = S(LatinSmallLigatureFi) + "rst";
        Assert.Equal(Keyword.NoKeyword, Tokenizer.KeywordLookup(password, null));
        Assert.Equal(Keyword.NoKeyword, Tokenizer.KeywordLookup(first, null));
        Assert.Equal(Keyword.PASSWORD, Tokenizer.KeywordLookup("password", null));
        Assert.Equal(Keyword.FIRST, Keywords.Lookup(RustString.ToUppercase(first)));

        var select = Parser.ParseSql(new GenericDialect(), "SELECT " + password + " FROM t");
        Assert.Equal("SELECT " + password + " FROM t", Assert.Single(select).ToString());
    }
}
