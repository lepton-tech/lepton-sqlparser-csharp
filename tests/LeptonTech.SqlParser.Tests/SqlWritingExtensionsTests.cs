// Testes próprios da API pública (ToSql). Ver docs/adr/0008-api-publica-fase-1.md.

using LeptonTech.SqlParser.Core;
using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Tests;

public class SqlWritingExtensionsTests
{
    private readonly SqlQueryParser _parser = new SqlQueryParser();

    [Theory]
    [InlineData("generic", "SELECT a, b FROM t WHERE c = 1 ORDER BY a DESC LIMIT 10")]
    [InlineData("generic", "WITH cte AS (SELECT a FROM t) SELECT * FROM cte")]
    [InlineData("generic", "INSERT INTO t (a, b) VALUES (1, 'x')")]
    [InlineData("generic", "UPDATE t SET a = 1 WHERE b = 2")]
    [InlineData("generic", "DELETE FROM t WHERE a = 1")]
    [InlineData("generic", "CREATE TABLE t (id INT PRIMARY KEY, nome VARCHAR(100) NOT NULL)")]
    [InlineData("generic", "SELECT a FROM t1 LEFT JOIN t2 ON t1.id = t2.id")]
    [InlineData("postgresql", "SELECT a::INT FROM t WHERE b ILIKE '%x%'")]
    [InlineData("mysql", "SELECT `a` FROM `t` LIMIT 5")]
    [InlineData("mssql", "SELECT TOP 5 [nome] FROM [dbo].[clientes]")]
    [InlineData("bigquery", "SELECT * FROM `proj`.`ds`.`t`")]
    [InlineData("snowflake", "SELECT a FROM t QUALIFY ROW_NUMBER() OVER (PARTITION BY b ORDER BY c) = 1")]
    [InlineData("duckdb", "SELECT * EXCLUDE (a) FROM t")]
    [InlineData("sqlite", "INSERT OR REPLACE INTO t (a) VALUES (1)")]
    public void ToSql_regera_o_SQL_canonico(string dialectName, string sql)
    {
        var statement = Assert.Single(_parser.Parse(sql, dialectName));

        Assert.Equal(sql, statement.ToSql());
        Assert.Equal(statement.ToString(), statement.ToSql());
    }

    [Fact]
    public void ToSql_normaliza_o_texto()
    {
        var statements = _parser.Parse("select   top 10 *\n  from my_table where abc > 99 order by xyz desc");

        Assert.Equal("SELECT TOP 10 * FROM my_table WHERE abc > 99 ORDER BY xyz DESC", statements.ToSql());
    }

    [Fact]
    public void ToSql_funciona_em_qualquer_no_da_AST()
    {
        var query = (Statement.Query)Assert.Single(_parser.Parse("SELECT a + 1 AS b FROM t WHERE c = 1"));
        var select = (SetExpr.Select)query.Value.Body;

        Assert.Equal("SELECT a + 1 AS b FROM t WHERE c = 1", query.Value.ToSql());
        Assert.Equal("c = 1", select.Value.Selection!.ToSql());
        Assert.Equal("a + 1 AS b", select.Value.Projection[0].ToSql());
        Assert.Equal("t", select.Value.From[0].ToSql());
    }

    [Fact]
    public void ToSql_pretty_equivale_ao_formato_alternativo_do_Core()
    {
        var statement = Assert.Single(_parser.Parse("SELECT a, b FROM t WHERE x = 1"));

        Assert.Equal(Formatter.FormatAlternate(statement), statement.ToSql(pretty: true));
        Assert.Equal("SELECT\n  a,\n  b\nFROM\n  t\nWHERE\n  x = 1", statement.ToSql(pretty: true));
        Assert.Equal(statement.ToSql(), statement.ToSql(pretty: false));
    }

    [Fact]
    public void ToSql_de_varias_instrucoes_usa_ponto_e_virgula()
    {
        var statements = _parser.Parse("SELECT a FROM t; SELECT b FROM u");

        Assert.Equal("SELECT a FROM t; SELECT b FROM u", statements.ToSql());
        Assert.Equal("SELECT\n  a\nFROM\n  t;\nSELECT\n  b\nFROM\n  u", statements.ToSql(pretty: true));
        Assert.Equal("", Sequence<Statement>.Empty.ToSql());
    }

    [Fact]
    public void ToSql_aceita_qualquer_colecao_de_instrucoes()
    {
        var list = _parser.Parse("SELECT 1; SELECT 2").ToList();

        Assert.Equal("SELECT 1; SELECT 2", list.ToSql());
    }

    [Fact]
    public void ToSql_com_argumento_nulo_lanca_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((IDisplay)null!).ToSql());
        Assert.Equal("statements", Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Statement>)null!).ToSql()).ParamName);
    }

    [Fact]
    public void ToSql_com_item_nulo_aponta_o_parametro_statements()
    {
        // R4, item 9: antes, a exceção vinha do ToSql do item, com ParamName "node".
        var statement = Assert.Single(_parser.Parse("SELECT 1"));

        var error = Assert.Throws<ArgumentNullException>(() => new[] { statement, null! }.ToSql());

        Assert.Equal("statements", error.ParamName);
        Assert.Contains("item 1", error.Message);
    }
}
