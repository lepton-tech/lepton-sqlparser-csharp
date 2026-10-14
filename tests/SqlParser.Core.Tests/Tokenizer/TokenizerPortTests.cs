// Testes próprios do port (não existem no tokenizer.rs): verificam decisões de tradução registradas
// em docs/port/convencoes.md (§10 e §13) que nenhum teste do Rust exercita diretamente.

using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

public class TokenizerPortTests
{
    [Fact]
    public void Coluna_avanca_uma_vez_por_scalar_value_fora_do_BMP()
    {
        // 💝 (U+1F49D) é UM char no Rust e um par substituto em C#: deve ocupar UMA coluna.
        var sql = "'\U0001F49D' x";
        var tokens = new Tokenizer(new GenericDialect(), sql).TokenizeWithLocation();

        Assert.Equal(
            new List<TokenWithSpan>
            {
                TokenWithSpan.At(new Token.SingleQuotedString("\U0001F49D"), new Location(1, 1), new Location(1, 4)),
                TokenWithSpan.At(new Token.Whitespace(new Whitespace.Space()), new Location(1, 4), new Location(1, 5)),
                TokenWithSpan.At(Token.MakeWord("x", null), new Location(1, 5), new Location(1, 6)),
            },
            tokens);
    }

    [Fact]
    public void Char_fora_do_BMP_vira_um_unico_token()
    {
        var tokens = new Tokenizer(new GenericDialect(), "\U0001F49D\U0001F49D").TokenizeWithLocation();

        Assert.Equal(
            new List<TokenWithSpan>
            {
                TokenWithSpan.At(new Token.Char("\U0001F49D"), new Location(1, 1), new Location(1, 2)),
                TokenWithSpan.At(new Token.Char("\U0001F49D"), new Location(1, 2), new Location(1, 3)),
            },
            tokens);
        Assert.Equal("\U0001F49D", tokens[0].ToString());
    }

    [Fact]
    public void Par_substituto_nao_e_dividido_apos_prefixo_de_identificador()
    {
        // Achado da revisão linha a linha (2026-10-14): nos ramos `[ch, *sch]` ('@', '@@', '#', '%'),
        // o Rust guarda o scalar inteiro. 𝔘 (U+1D518) deve ficar num único Word, como no Rust.
        var sql = "SELECT @\U0001D518x";
        var tokens = new Tokenizer(new GenericDialect(), sql).TokenizeWithLocation();

        Assert.Equal(
            new List<TokenWithSpan>
            {
                TokenWithSpan.At(Token.MakeKeyword("SELECT"), new Location(1, 1), new Location(1, 7)),
                TokenWithSpan.At(new Token.Whitespace(new Whitespace.Space()), new Location(1, 7), new Location(1, 8)),
                TokenWithSpan.At(Token.MakeWord("@\U0001D518x", null), new Location(1, 8), new Location(1, 11)),
            },
            tokens);
    }

    [Fact]
    public void Redshift_identificador_com_delimitador_aninhado()
    {
        var dialect = new RedshiftSqlDialect();

        Assert.Equal(
            new List<Token> { Token.MakeWord("\"foo\"", '[') },
            new Tokenizer(dialect, "[\"foo\"]").Tokenize());
        Assert.Equal(
            new List<Token> { Token.MakeWord("foo", '[') },
            new Tokenizer(dialect, "[foo]").Tokenize());
        Assert.Equal(
            new List<Token> { new Token.LBracket(), new Token.Number("0", false), new Token.RBracket() },
            new Tokenizer(dialect, "[0]").Tokenize());
    }

    [Fact]
    public void PeekableChars_e_copiado_por_valor()
    {
        var original = new PeekableChars("ab");
        var copia = original;
        Assert.Equal('a', copia.Next());
        Assert.Equal('a', original.Peek());
        Assert.Equal('b', original.Nth(1));
        Assert.Null(original.Next());
    }
}
