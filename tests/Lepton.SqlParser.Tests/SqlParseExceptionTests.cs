// Testes próprios da API pública (SqlParseException). Ver docs/adr/0008-api-publica-fase-1.md.

using Lepton.SqlParser.Core;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Tests;

public class SqlParseExceptionTests
{
    private readonly SqlQueryParser _parser = new SqlQueryParser();

    [Theory]
    [InlineData("SELEC 1")]
    [InlineData("SELECT * FROM t WHERE")]
    [InlineData("SELECT 'abc FROM t")]
    [InlineData("SELECT a,\n  b FROM t WHERE x = = 1")]
    [InlineData("SELECT 1 SELECT 2")]
    public void Mensagem_e_identica_a_do_Core(string sql)
    {
        var core = Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), sql));

        var e = Assert.Throws<SqlParseException>(() => _parser.Parse(sql));

        Assert.Equal(core.Message, e.Message);
        Assert.Equal(core.Error, e.Error);
        Assert.IsType<ParserException>(e.InnerException);
    }

    [Fact]
    public void Erro_do_parser_com_posicao()
    {
        var e = Assert.Throws<SqlParseException>(() => _parser.Parse("SELECT a,\n  b FROM t WHERE x = = 1"));

        Assert.Equal("sql parser error: Expected: an expression, found: = at Line: 2, Column: 22", e.Message);
        Assert.IsType<ParserError.ParserErrorValue>(e.Error);
        Assert.Equal(new Location(2, 22), e.Location);
        Assert.Equal(2, e.Line);
        Assert.Equal(22, e.Column);
    }

    [Fact]
    public void Erro_do_tokenizador_com_posicao()
    {
        var e = Assert.Throws<SqlParseException>(() => _parser.Parse("SELECT 'abc FROM t"));

        Assert.Equal("sql parser error: Unterminated string literal at Line: 1, Column: 8", e.Message);
        Assert.IsType<ParserError.TokenizerError>(e.Error);
        Assert.Equal(1, e.Line);
        Assert.Equal(8, e.Column);
    }

    [Fact]
    public void Erro_sem_posicao_tem_Location_nulo_e_linha_coluna_zero()
    {
        // O token EOF não tem posição (Location 0,0), então a mensagem não tem o sufixo " at Line: ...".
        var e = Assert.Throws<SqlParseException>(() => _parser.Parse("SELECT * FROM t WHERE"));

        Assert.Equal("sql parser error: Expected: an expression, found: EOF", e.Message);
        Assert.Null(e.Location);
        Assert.Equal(0, e.Line);
        Assert.Equal(0, e.Column);
    }

    [Fact]
    public void Posicao_e_lida_so_do_sufixo_da_mensagem()
    {
        var e = new SqlParseException(new ParserError.ParserErrorValue("texto ' at Line: 9, Column: 9' fim"));

        Assert.Null(e.Location);
        Assert.Null(e.InnerException);
    }

    [Fact]
    public void Construtor_publico_aceita_qualquer_ParserError()
    {
        var e = new SqlParseException(new ParserError.ParserErrorValue("Expected: x, found: y at Line: 3, Column: 14"));

        Assert.Equal("sql parser error: Expected: x, found: y at Line: 3, Column: 14", e.Message);
        Assert.Equal(new Location(3, 14), e.Location);
        Assert.Throws<ArgumentNullException>(() => new SqlParseException(null!));
    }
}
