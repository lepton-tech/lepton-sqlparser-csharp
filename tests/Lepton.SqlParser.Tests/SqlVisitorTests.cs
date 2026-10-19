// Testes próprios da API pública, fase 2: SqlVisitor, Visit e os atalhos de leitura com lambdas.
// A ordem de visita é comparada com a do visitor do Core (que é a do derive Visit do Rust).

using Lepton.SqlParser.Core;
using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Tests;

public class SqlVisitorTests
{
    private static readonly SqlQueryParser Parser = new SqlQueryParser();

    /// <summary>Consultas e instruções complexas, em vários dialetos (nome aceito por <c>Dialect.DialectFromStr</c>).</summary>
    public static TheoryData<string, string> ComplexStatements => new TheoryData<string, string>
    {
        {
            "generic",
            "WITH cte AS (SELECT id, name FROM users WHERE active = 1 AND created_at > '2024-01-01') "
            + "SELECT c.id, COUNT(o.id) AS n, SUM(o.total) OVER (PARTITION BY c.id ORDER BY o.created_at) "
            + "FROM cte AS c LEFT JOIN orders AS o ON c.id = o.user_id "
            + "WHERE o.total > (SELECT AVG(total) FROM orders) AND c.name LIKE 'A%' "
            + "GROUP BY c.id HAVING COUNT(*) > 1 ORDER BY n DESC LIMIT 10"
        },
        {
            "generic",
            "SELECT CASE WHEN x > 0 THEN 'pos' ELSE 'neg' END, CAST(y AS INT), EXISTS (SELECT 1 FROM z) "
            + "FROM (SELECT x, y FROM w) AS d UNION ALL SELECT 1, 2, true"
        },
        {
            "generic",
            "MERGE INTO target AS t USING source AS s ON t.id = s.id "
            + "WHEN MATCHED THEN UPDATE SET t.v = s.v WHEN NOT MATCHED THEN INSERT (id, v) VALUES (s.id, s.v)"
        },
        {
            "postgres",
            "INSERT INTO archive (id, total) SELECT id, total FROM orders WHERE created_at < now() - INTERVAL '1 year' "
            + "ON CONFLICT (id) DO UPDATE SET total = EXCLUDED.total RETURNING id"
        },
        {
            "mssql",
            "UPDATE t SET t.price = s.price * 1.1 FROM dbo.products AS t INNER JOIN staging.prices AS s ON t.id = s.id "
            + "WHERE s.price IS NOT NULL"
        },
        {
            "mysql",
            "DELETE FROM orders WHERE customer_id IN (SELECT id FROM customers WHERE country = 'BR') ORDER BY id LIMIT 100"
        },
        {
            "snowflake",
            "CREATE TABLE t2 AS SELECT a, b FROM db.s.t1 UNION ALL SELECT a, b FROM t3"
        },
        {
            "clickhouse",
            "CREATE TABLE t (a INT, b INT) ENGINE = MergeTree ORDER BY (a, b)"
        },
    };

    [Theory]
    [MemberData(nameof(ComplexStatements))]
    public void Ordem_dos_ganchos_e_a_mesma_do_visitor_do_Core(string dialect, string sql)
    {
        var statements = Parser.Parse(sql, dialect);
        var core = new RecordingCoreVisitor();
        var visitor = new RecordingSqlVisitor();

        Assert.Equal(ControlFlow<ValueTuple>.Continue, statements.Visit(core));
        Assert.Equal(ControlFlow.Continue, statements.Visit(visitor));

        Assert.NotEmpty(core.Visited);
        Assert.Equal(core.Visited, visitor.Visited);
    }

    [Fact]
    public void Sequencia_de_ganchos_de_uma_consulta_simples()
    {
        var visitor = new RecordingSqlVisitor(leaves: false);

        Parser.Parse("SELECT a FROM t WHERE b = 1")[0].Visit(visitor);

        Assert.Equal(
            new[]
            {
                "PRE: STATEMENT: SELECT a FROM t WHERE b = 1",
                "PRE: QUERY: SELECT a FROM t WHERE b = 1",
                "PRE: SELECT: SELECT a FROM t WHERE b = 1",
                "PRE: EXPR: a",
                "POST: EXPR: a",
                "PRE: TABLE FACTOR: t",
                "PRE: RELATION: t",
                "POST: RELATION: t",
                "POST: TABLE FACTOR: t",
                "PRE: EXPR: b = 1",
                "PRE: EXPR: b",
                "POST: EXPR: b",
                "PRE: EXPR: 1",
                "POST: EXPR: 1",
                "POST: EXPR: b = 1",
                "PRE: GROUP BY: GROUP BY ",
                "POST: GROUP BY: GROUP BY ",
                "POST: SELECT: SELECT a FROM t WHERE b = 1",
                "POST: QUERY: SELECT a FROM t WHERE b = 1",
                "POST: STATEMENT: SELECT a FROM t WHERE b = 1",
            },
            visitor.Visited);
    }

    [Fact]
    public void Ganchos_que_nao_existem_no_SqlParser_cs_tambem_sao_chamados()
    {
        var visitor = new RecordingSqlVisitor();

        Parser.Parse("SELECT a FROM t ORDER BY a")[0].Visit(visitor);

        Assert.Contains("PRE: IDENT: a", visitor.Visited);
        Assert.Contains("PRE: ORDER BY: ORDER BY a", visitor.Visited);
        Assert.Contains("PRE: ORDER BY EXPR: a", visitor.Visited);
        Assert.Contains("POST: ORDER BY EXPR: a", visitor.Visited);
    }

    private sealed class BreakOnRelation : SqlVisitor
    {
        private readonly string _stopAt;

        public BreakOnRelation(string stopAt) => _stopAt = stopAt;

        public List<string> Visited { get; } = new List<string>();

        public override ControlFlow PreVisitRelation(ObjectName relation)
        {
            Visited.Add(relation.ToSql());
            return relation.ToSql() == _stopAt ? ControlFlow.Break : ControlFlow.Continue;
        }

        public override ControlFlow PostVisitStatement(Statement statement)
        {
            Visited.Add("POST: STATEMENT");
            return ControlFlow.Continue;
        }
    }

    [Fact]
    public void Break_interrompe_a_visita()
    {
        var statements = Parser.Parse("SELECT * FROM t1 JOIN t2 ON a = b JOIN t3 ON c = d; SELECT * FROM t4");
        var visitor = new BreakOnRelation("t2");

        var flow = statements.Visit(visitor);

        Assert.Equal(ControlFlow.Break, flow);
        // Nem t3, nem os PostVisit* pendentes, nem a segunda instrução.
        Assert.Equal(new[] { "t1", "t2" }, visitor.Visited);
    }

    [Fact]
    public void Visitor_pode_ser_reutilizado()
    {
        var visitor = new BreakOnRelation("nenhuma");

        Assert.Equal(ControlFlow.Continue, Parser.Parse("SELECT * FROM a")[0].Visit(visitor));
        Assert.Equal(ControlFlow.Continue, Parser.Parse("SELECT * FROM b")[0].Visit(visitor));

        Assert.Equal(new[] { "a", "POST: STATEMENT", "b", "POST: STATEMENT" }, visitor.Visited);
    }

    [Fact]
    public void Visit_aceita_subarvores()
    {
        var query = ((Statement.Query)Parser.Parse("SELECT x + 1 FROM t WHERE y > (SELECT 2)")[0]).Value;
        var select = (SetExpr.Select)query.Body;
        var visitor = new RecordingSqlVisitor(leaves: false);

        select.Value.Selection!.Visit(visitor);

        Assert.Equal(
            new[]
            {
                "PRE: EXPR: y > (SELECT 2)",
                "PRE: EXPR: y",
                "POST: EXPR: y",
                "PRE: EXPR: (SELECT 2)",
                "PRE: QUERY: SELECT 2",
                "PRE: SELECT: SELECT 2",
                "PRE: EXPR: 2",
                "POST: EXPR: 2",
                "PRE: GROUP BY: GROUP BY ",
                "POST: GROUP BY: GROUP BY ",
                "POST: SELECT: SELECT 2",
                "POST: QUERY: SELECT 2",
                "POST: EXPR: (SELECT 2)",
                "POST: EXPR: y > (SELECT 2)",
            },
            visitor.Visited);
    }

    [Fact]
    public void Visit_valida_argumentos()
    {
        var statement = Parser.Parse("SELECT 1")[0];

        Assert.Throws<ArgumentNullException>(() => statement.Visit((SqlVisitor)null!));
        Assert.Throws<ArgumentNullException>(() => ((Statement)null!).Visit(new RecordingSqlVisitor()));
        Assert.Throws<ArgumentNullException>(() => statement.VisitExpressions((Action<Expr>)null!));
        Assert.Throws<ArgumentNullException>(() => statement.VisitRelations((Func<ObjectName, ControlFlow>)null!));
        Assert.Throws<ArgumentNullException>(() => ((Statement)null!).VisitStatements(_ => { }));
    }

    // ---------------------------------------------------------------------------------------------
    // Atalhos com lambdas: mesma sequência das funções do Core (visit_relations, visit_expressions,
    // visit_statements do Rust).

    [Theory]
    [MemberData(nameof(ComplexStatements))]
    public void VisitExpressions_entrega_as_expressoes_na_ordem_do_Core(string dialect, string sql)
    {
        var statements = Parser.Parse(sql, dialect);
        var core = new List<string>();
        Visitor.VisitExpressions(statements, expr =>
        {
            core.Add(expr.ToString());
            return ControlFlow<ValueTuple>.Continue;
        });

        var visited = new List<string>();
        statements.VisitExpressions(expr => visited.Add(expr.ToSql()));

        Assert.Equal(core, visited);
    }

    [Theory]
    [MemberData(nameof(ComplexStatements))]
    public void VisitRelations_e_VisitStatements_entregam_os_nos_na_ordem_do_Core(string dialect, string sql)
    {
        var statements = Parser.Parse(sql, dialect);

        var coreRelations = new List<string>();
        Visitor.VisitRelations(statements, relation =>
        {
            coreRelations.Add(relation.ToString());
            return ControlFlow<ValueTuple>.Continue;
        });
        var coreStatements = new List<string>();
        Visitor.VisitStatements(statements, statement =>
        {
            coreStatements.Add(statement.ToString());
            return ControlFlow<ValueTuple>.Continue;
        });

        var relations = new List<string>();
        statements.VisitRelations(relation => relations.Add(relation.ToSql()));
        var visitedStatements = new List<string>();
        statements.VisitStatements(statement => visitedStatements.Add(statement.ToSql()));

        Assert.Equal(coreRelations, relations);
        Assert.Equal(coreStatements, visitedStatements);
    }

    [Fact]
    public void Atalhos_de_leitura_com_Break()
    {
        var statements = Parser.Parse("SELECT a + b FROM t1 JOIN t2 ON t1.id = t2.id; SELECT c FROM t3");

        var expressions = new List<string>();
        var flow = statements.VisitExpressions(expr =>
        {
            expressions.Add(expr.ToSql());
            return expr.ToSql() == "a" ? ControlFlow.Break : ControlFlow.Continue;
        });
        Assert.Equal(ControlFlow.Break, flow);
        Assert.Equal(new[] { "a + b", "a" }, expressions);

        var relations = new List<string>();
        flow = statements.VisitRelations(relation =>
        {
            relations.Add(relation.ToSql());
            return relations.Count == 2 ? ControlFlow.Break : ControlFlow.Continue;
        });
        Assert.Equal(ControlFlow.Break, flow);
        Assert.Equal(new[] { "t1", "t2" }, relations);

        var count = 0;
        flow = statements.VisitStatements(_ => ++count == 1 ? ControlFlow.Break : ControlFlow.Continue);
        Assert.Equal(ControlFlow.Break, flow);
        Assert.Equal(1, count);

        Assert.Equal(ControlFlow.Continue, statements.VisitStatements(_ => ControlFlow.Continue));
    }

    [Fact]
    public void VisitStatements_inclui_instrucoes_aninhadas()
    {
        var statements = Parser.Parse("EXPLAIN SELECT 1; WITH x AS (SELECT 1) INSERT INTO t SELECT * FROM x", new PostgreSqlDialect());

        var visited = new List<string>();
        statements.VisitStatements(statement => visited.Add(statement.GetType().Name));

        Assert.Equal(new[] { "Explain", "Query", "Query", "Insert" }, visited);
    }
}
