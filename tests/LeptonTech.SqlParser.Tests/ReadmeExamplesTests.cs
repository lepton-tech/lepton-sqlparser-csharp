// Testes próprios: cada exemplo de src/LeptonTech.SqlParser/README.md, na mesma ordem. Ao mudar um exemplo
// no README, mude o teste correspondente (e vice-versa).

using LeptonTech.SqlParser.Core;
using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Tests;

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

    private sealed class TableCollector : SqlVisitor
    {
        public List<string> Tables { get; } = new List<string>();

        public override ControlFlow PreVisitRelation(ObjectName relation)
        {
            Tables.Add(relation.ToSql());
            return ControlFlow.Continue;
        }
    }

    [Fact]
    public void Exemplo_8_visitor()
    {
        var collector = new TableCollector();
        var flow = new SqlQueryParser().Parse("SELECT * FROM a JOIN b ON a.id = b.id WHERE a.x IN (SELECT x FROM c)").Visit(collector);

        Assert.Equal(ControlFlow.Continue, flow);
        Assert.Equal(new[] { "a", "b", "c" }, collector.Tables);
    }

    [Fact]
    public void Exemplo_9_atalhos_com_lambdas()
    {
        var statements = new SqlQueryParser().Parse("SELECT a + 1 FROM t WHERE b > 2");

        var expressions = new List<string>();
        statements.VisitExpressions(expr => expressions.Add(expr.ToSql()));
        Assert.Equal(new[] { "a + 1", "a", "1", "b > 2", "b", "2" }, expressions);

        var temSubconsulta = statements.VisitExpressions(
            expr => expr is Expr.Subquery ? ControlFlow.Break : ControlFlow.Continue) == ControlFlow.Break;
        Assert.False(temSubconsulta);
    }

    private sealed class RenameTable : SqlRewriter
    {
        public override ObjectName PostVisitRelation(ObjectName relation) =>
            relation.ToSql() == "clientes"
                ? ObjectName.From(Sequence.Of(new Ident("crm"), new Ident("clientes")))
                : relation;
    }

    [Fact]
    public void Exemplo_10_reescrita()
    {
        var statement = new SqlQueryParser().Parse("SELECT * FROM clientes WHERE id = 1")[0];
        var rewritten = statement.Rewrite(new RenameTable());

        Assert.Equal("SELECT * FROM crm.clientes WHERE id = 1", rewritten.ToSql());
        Assert.Equal("SELECT * FROM clientes WHERE id = 1", statement.ToSql());

        var semLimite = new SqlQueryParser().Parse("SELECT * FROM t LIMIT 10").RewriteStatements(
            s => s is Statement.Query q ? q with { Value = q.Value with { LimitClause = null } } : s);
        Assert.Equal("SELECT * FROM t", semLimite.ToSql());
    }

    [Fact]
    public void Exemplo_11_analise()
    {
        var statements = new SqlQueryParser().Parse(
            "WITH ativos AS (SELECT id, nome FROM clientes WHERE ativo = 1) "
            + "SELECT a.nome, COUNT(p.id) FROM ativos AS a JOIN pedidos AS p ON p.cliente_id = a.id GROUP BY a.nome");

        var tabelas = statements.GetReferencedTables();
        var colunas = statements.GetReferencedColumns();
        var funcoes = statements.GetReferencedFunctions();
        var tipo = statements[0].GetStatementKind();

        Assert.Equal(new[] { "clientes", "pedidos" }, tabelas.Select(n => n.ToSql()));
        Assert.Equal(new[] { "id", "nome", "ativo", "a.nome", "p.id", "p.cliente_id", "a.id" }, colunas.Select(n => n.ToSql()));
        Assert.Equal(new[] { "COUNT" }, funcoes.Select(n => n.ToSql()));
        Assert.Equal(SqlStatementKind.Query, tipo);
    }
}
