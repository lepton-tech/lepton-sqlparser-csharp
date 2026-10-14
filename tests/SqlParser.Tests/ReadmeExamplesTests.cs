// Testes próprios: cada exemplo de src/SqlParser/README.md, na mesma ordem. Ao mudar um exemplo
// no README, mude o teste correspondente (e vice-versa).

using SqlParser.Core;
using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Tests;

public class ReadmeExamplesTests
{
    [Fact]
    public void Exemplo_1_parse_com_dialeto_padrao()
    {
        var parser = new SqlQueryParser();

        var statements = parser.Parse("select top 10 * from my_table where abc > 99 order by xyz desc");

        Assert.Equal("SELECT TOP 10 * FROM my_table WHERE abc > 99 ORDER BY xyz DESC", statements.ToSql());
    }

    [Fact]
    public void Exemplo_2_navegar_na_AST()
    {
        var statements = new SqlQueryParser().Parse("select top 10 * from my_table where abc > 99 order by xyz desc");

        var query = (Statement.Query)statements[0];
        var select = (SetExpr.Select)query.Value.Body;
        var table = (TableFactor.Table)select.Value.From[0].Relation;

        Assert.Equal("my_table", table.Name.ToSql());
        Assert.Equal("abc > 99", select.Value.Selection!.ToSql());
    }

    [Fact]
    public void Exemplo_3_dialeto_por_instancia_ou_por_nome()
    {
        var parser = new SqlQueryParser();

        var pg = parser.Parse("SELECT a::INT FROM t WHERE b ILIKE '%x%'", new PostgreSqlDialect());
        var ms = parser.Parse("SELECT TOP 5 [nome] FROM [dbo].[clientes]", "mssql");

        Assert.Equal("SELECT a::INT FROM t WHERE b ILIKE '%x%'", pg.ToSql());
        Assert.Equal("SELECT TOP 5 [nome] FROM [dbo].[clientes]", ms.ToSql());
    }

    [Fact]
    public void Exemplo_4_pretty_print()
    {
        var parser = new SqlQueryParser();

        var pretty = parser.Parse("SELECT a, b FROM t WHERE x = 1")[0].ToSql(pretty: true);

        Assert.Equal("SELECT\n  a,\n  b\nFROM\n  t\nWHERE\n  x = 1", pretty);
    }

    [Fact]
    public void Exemplo_5_erros()
    {
        var parser = new SqlQueryParser();

        var e = Assert.Throws<SqlParseException>(() => parser.Parse("SELECT * FROM t WHERE x = = 1"));
        Assert.Equal("sql parser error: Expected: an expression, found: = at Line: 1, Column: 27", e.Message);
        Assert.Equal(1, e.Line);
        Assert.Equal(27, e.Column);

        Assert.False(parser.TryParse("SELEC 1", out _, out var error));
        Assert.Equal("sql parser error: Expected: an SQL statement, found: SELEC at Line: 1, Column: 1", error.Message);
    }

    [Fact]
    public void Exemplo_6_opcoes_e_limite_de_recursao()
    {
        var options = new ParserOptions().WithTrailingCommas(true);
        var statements = new SqlQueryParser().Parse("SELECT a, b, FROM t", new PostgreSqlDialect(), options);
        Assert.Equal("SELECT a, b FROM t", statements.ToSql());

        var deep = new SqlQueryParser(recursionLimit: 200);
        Assert.Single(deep.Parse("SELECT " + new string('(', 100) + "1" + new string(')', 100)));
    }

    [Fact]
    public void Exemplo_7_comentarios()
    {
        var (statements, comments) = new SqlQueryParser().ParseWithComments("SELECT a FROM t -- fim");

        Assert.Equal("SELECT a FROM t", statements.ToSql());
        var comment = Assert.Single(comments.ToVec());
        Assert.Equal(" fim", comment.Comment.AsStr());
    }
}
