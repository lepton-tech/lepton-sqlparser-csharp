// Testes próprios da API pública, fase 2: SqlRewriter, Rewrite e os atalhos de reescrita com lambdas.
// A ordem dos ganchos é comparada com a do visitor mutável do Core (que é a do derive VisitMut do Rust), e
// toda reescrita é conferida pelo SQL gerado e por um novo parse desse SQL (ida e volta).

using SqlParser.Core;
using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Tests;

public class SqlRewriterTests
{
    private static readonly SqlQueryParser Parser = new SqlQueryParser();

    private static Statement ParseOne(string sql, Dialect? dialect = null) => Assert.Single(Parser.Parse(sql, dialect));

    /// <summary>O SQL gerado interpreta-se de novo na mesma AST (ida e volta).</summary>
    private static void AssertRoundTrip(Statement statement, string expectedSql, Dialect? dialect = null)
    {
        Assert.Equal(expectedSql, statement.ToSql());
        Assert.Equal(statement, ParseOne(expectedSql, dialect));
    }

    [Theory]
    [MemberData(nameof(SqlVisitorTests.ComplexStatements), MemberType = typeof(SqlVisitorTests))]
    public void Ordem_dos_ganchos_e_as_trocas_sao_as_do_visitor_mutavel_do_Core(string dialect, string sql)
    {
        var statements = Parser.Parse(sql, dialect);
        var core = new RecordingCoreMutator();
        var rewriter = new RecordingRewriter();

        Assert.Equal(ControlFlow<ValueTuple>.Continue, statements.Visit(core, out var coreResult));
        var result = statements.Rewrite(rewriter);

        Assert.NotEmpty(core.Visited);
        Assert.Equal(core.Visited, rewriter.Visited);
        Assert.Equal(coreResult.ToSql(), result.ToSql());
        Assert.NotEqual(statements.ToSql(), result.ToSql());
    }

    private sealed class RenameTable : SqlRewriter
    {
        private readonly string _from;
        private readonly ObjectName _to;

        public RenameTable(string from, ObjectName to)
        {
            _from = from;
            _to = to;
        }

        public override ObjectName PostVisitRelation(ObjectName relation) =>
            string.Equals(relation.ToSql(), _from, StringComparison.OrdinalIgnoreCase) ? _to : relation;
    }

    [Fact]
    public void Renomear_tabela_em_todos_os_pontos_faz_ida_e_volta()
    {
        var statement = ParseOne(
            "SELECT c.nome, (SELECT COUNT(*) FROM pedidos WHERE pedidos.cliente_id = c.id) "
            + "FROM clientes AS c WHERE c.id IN (SELECT cliente_id FROM pedidos)");
        var rewriter = new RenameTable("pedidos", ObjectName.From(Sequence.Of(new Ident("vendas"), new Ident("pedidos"))));

        var rewritten = statement.Rewrite(rewriter);

        AssertRoundTrip(
            rewritten,
            "SELECT c.nome, (SELECT COUNT(*) FROM vendas.pedidos WHERE pedidos.cliente_id = c.id) "
            + "FROM clientes AS c WHERE c.id IN (SELECT cliente_id FROM vendas.pedidos)");
        // A árvore original não muda.
        Assert.Equal(
            "SELECT c.nome, (SELECT COUNT(*) FROM pedidos WHERE pedidos.cliente_id = c.id) "
            + "FROM clientes AS c WHERE c.id IN (SELECT cliente_id FROM pedidos)",
            statement.ToSql());
    }

    [Fact]
    public void Renomear_tabela_em_DML_de_varios_dialetos()
    {
        var to = ObjectName.From(Sequence.Of(Ident.WithQuote('[', "dbo"), Ident.WithQuote('[', "Clientes")));
        var rewriter = new RenameTable("clientes", to);
        var ms = new MsSqlDialect();

        AssertRoundTrip(
            ParseOne("UPDATE clientes SET ativo = 0 WHERE id = 1", ms).Rewrite(rewriter),
            "UPDATE [dbo].[Clientes] SET ativo = 0 WHERE id = 1",
            ms);
        AssertRoundTrip(
            ParseOne("DELETE FROM clientes WHERE id = 1", ms).Rewrite(rewriter),
            "DELETE FROM [dbo].[Clientes] WHERE id = 1",
            ms);
        AssertRoundTrip(
            ParseOne("INSERT INTO clientes (id) VALUES (1)", ms).Rewrite(rewriter),
            "INSERT INTO [dbo].[Clientes] (id) VALUES (1)",
            ms);

        var pg = new PostgreSqlDialect();
        var rewriterPg = new RenameTable("clientes", ObjectName.From(Sequence.Of(new Ident("crm"), new Ident("clientes"))));
        AssertRoundTrip(
            ParseOne("WITH ativos AS (SELECT * FROM clientes WHERE ativo) DELETE FROM clientes WHERE id NOT IN (SELECT id FROM ativos)", pg)
                .Rewrite(rewriterPg),
            "WITH ativos AS (SELECT * FROM crm.clientes WHERE ativo) DELETE FROM crm.clientes WHERE id NOT IN (SELECT id FROM ativos)",
            pg);
    }

    [Fact]
    public void Sem_trocas_devolve_a_mesma_instancia()
    {
        var statements = Parser.Parse("SELECT a FROM t WHERE b > 1; SELECT 2");
        var first = statements[0];

        var rewritten = statements.Rewrite(new RenameTable("nenhuma", ObjectName.From(new Ident("x"))));

        Assert.Same(first, rewritten[0]);
        Assert.Same(first, first.Rewrite(new RenameTable("nenhuma", ObjectName.From(new Ident("x")))));
        Assert.Same(first, first.RewriteExpressions(expr => expr));
    }

    private sealed class RedactLiterals : SqlRewriter
    {
        public override ValueWithSpan PreVisitValue(ValueWithSpan value) =>
            value with { Value = new Value.Placeholder("?") };
    }

    [Fact]
    public void Trocar_literais_por_parametros()
    {
        var statement = ParseOne("SELECT * FROM t WHERE nome = 'Ana' AND idade > 30 LIMIT 10");

        AssertRoundTrip(statement.Rewrite(new RedactLiterals()), "SELECT * FROM t WHERE nome = ? AND idade > ? LIMIT ?");
    }

    private sealed class ReplaceXInPre : SqlRewriter
    {
        public List<string> Identifiers { get; } = new List<string>();

        public override Expr PreVisitExpression(Expr expression) =>
            expression is Expr.Identifier { Value.Value: "x" }
                ? new Expr.BinaryOp(new Expr.Identifier(new Ident("y")), new BinaryOperator.Plus(), new Expr.Identifier(new Ident("z")))
                : expression;

        public override Ident PreVisitIdentifier(Ident identifier)
        {
            Identifiers.Add(identifier.Value);
            return identifier;
        }
    }

    [Fact]
    public void Troca_no_pre_faz_a_visita_seguir_pelos_filhos_do_no_novo()
    {
        var rewriter = new ReplaceXInPre();

        var rewritten = ParseOne("SELECT x FROM t").Rewrite(rewriter);

        Assert.Equal(new[] { "y", "z", "t" }, rewriter.Identifiers);
        AssertRoundTrip(rewritten, "SELECT y + z FROM t");
    }

    private sealed class UppercaseUntil : SqlRewriter
    {
        private readonly string _stopAt;

        public UppercaseUntil(string stopAt) => _stopAt = stopAt;

        public int Calls { get; private set; }

        public override Ident PreVisitIdentifier(Ident identifier)
        {
            Calls++;
            if (identifier.Value == _stopAt)
            {
                Break();
            }
            return identifier with { Value = identifier.Value.ToUpperInvariant() };
        }
    }

    [Fact]
    public void Break_mantem_as_trocas_anteriores_e_a_do_proprio_gancho()
    {
        var statement = ParseOne("SELECT a, b, c FROM t");
        var rewriter = new UppercaseUntil("b");

        var rewritten = statement.Rewrite(rewriter);

        Assert.Equal("SELECT A, B, c FROM t", rewritten.ToSql());
        Assert.Equal(2, rewriter.Calls);

        // O Break vale só para a reescrita em que foi pedido.
        var again = ParseOne("SELECT x, y FROM t").Rewrite(rewriter);
        Assert.Equal("SELECT X, Y FROM T", again.ToSql());
    }

    private sealed class NullRewriter : SqlRewriter
    {
        public override Expr PostVisitExpression(Expr expression) => null!;
    }

    [Fact]
    public void Gancho_que_devolve_null_lanca_InvalidOperationException()
    {
        var statement = ParseOne("SELECT a FROM t");

        var e = Assert.Throws<InvalidOperationException>(() => statement.Rewrite(new NullRewriter()));
        Assert.Contains("PostVisitExpression", e.Message);
        Assert.Throws<InvalidOperationException>(() => statement.RewriteExpressions(_ => null!));
        Assert.Throws<InvalidOperationException>(() => statement.RewriteRelations(_ => null!));
        Assert.Throws<InvalidOperationException>(() => statement.RewriteStatements(_ => null!));
    }

    [Fact]
    public void Rewrite_valida_argumentos()
    {
        var statement = ParseOne("SELECT 1");

        Assert.Throws<ArgumentNullException>(() => statement.Rewrite(null!));
        Assert.Throws<ArgumentNullException>(() => ((Statement)null!).Rewrite(new RedactLiterals()));
        Assert.Throws<ArgumentNullException>(() => statement.RewriteExpressions(null!));
        Assert.Throws<ArgumentNullException>(() => ((Statement)null!).RewriteRelations(r => r));
    }

    private sealed class ReplaceStatement : SqlRewriter
    {
        public override Statement PreVisitStatement(Statement statement) =>
            statement is Statement.Query ? Assert.Single(Parser.Parse("SELECT 42")) : statement;
    }

    [Fact]
    public void A_raiz_tambem_pode_ser_trocada()
    {
        var rewritten = ParseOne("SELECT 1 FROM t").Rewrite(new ReplaceStatement());

        AssertRoundTrip(rewritten, "SELECT 42");
    }

    // ---------------------------------------------------------------------------------------------
    // Atalhos com lambdas (visit_*_mut do Rust: gancho post).

    [Fact]
    public void RewriteRelations_troca_todas_as_relacoes()
    {
        var statements = Parser.Parse("SELECT * FROM a JOIN b ON a.id = b.id; INSERT INTO a SELECT * FROM c");

        var rewritten = statements.RewriteRelations(relation =>
            ObjectName.From(Sequence.Of(new Ident("s"), new Ident(relation.ToSql()))));

        Assert.Equal("SELECT * FROM s.a JOIN s.b ON a.id = b.id; INSERT INTO s.a SELECT * FROM s.c", rewritten.ToSql());
        Assert.Equal(rewritten, Parser.Parse(rewritten.ToSql()));
    }

    [Fact]
    public void RewriteExpressions_recebe_os_filhos_ja_reescritos()
    {
        var statement = ParseOne("SELECT 1 + 1 FROM t WHERE x = 1");
        var seen = new List<string>();

        var rewritten = statement.RewriteExpressions(expr =>
        {
            seen.Add(expr.ToSql());
            return expr is Expr.Value { Inner.Value: Value.Number { Value: "1" } }
                ? new Expr.Value(new ValueWithSpan(new Value.Number("2", false), Span.Empty()))
                : expr;
        });

        // Gancho post: o pai chega com os filhos já trocados, e o nó devolvido não é visitado de novo.
        Assert.Equal(new[] { "1", "1", "2 + 2", "x", "1", "x = 2" }, seen);
        AssertRoundTrip(rewritten, "SELECT 2 + 2 FROM t WHERE x = 2");
    }

    [Fact]
    public void RewriteStatements_troca_instrucoes_externas_e_aninhadas()
    {
        var statements = Parser.Parse("SELECT x FROM foo LIMIT 9; EXPLAIN SELECT * FROM t LIMIT 5");

        var rewritten = statements.RewriteStatements(statement =>
            statement is Statement.Query q ? q with { Value = q.Value with { LimitClause = null } } : statement);

        Assert.Equal("SELECT x FROM foo; EXPLAIN SELECT * FROM t", rewritten.ToSql());
        Assert.Equal(rewritten, Parser.Parse(rewritten.ToSql()));
    }
}
