// Testes próprios do port (não existem no Rust): conferem o visitor gerado (tools/codegen/Generate-Visitor.ps1)
// contra a ordem que o derive Visit/VisitMut do Rust produz, deduzida da ordem dos campos dos tipos em
// upstream/sqlparser-rs/src/ast (o derive visita os campos na ordem da declaração e chama pre_/post_ dos
// tipos e campos com visit(with = "...")). Também portam os exemplos da documentação de ast/visitor.rs
// (doctests do Rust) e cobrem decisões do port: VisitMut sobre AST imutável, Break e pilha (docs/adr/0007-visitor.md).

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

public class VisitorPortTests
{
    /// <summary>Registra pre_/post_ de todos os ganchos (menos <c>ident</c>, opcional) com o <c>Display</c> do nó.</summary>
    private sealed class RecordingVisitor : Visitor<ValueTuple>
    {
        private readonly bool _idents;

        public RecordingVisitor(bool idents = false) => _idents = idents;

        public List<string> Visited { get; } = new List<string>();

        private ControlFlow<ValueTuple> Add(string text)
        {
            Visited.Add(text);
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitQuery(Query query) => Add($"PRE: QUERY: {query}");
        public override ControlFlow<ValueTuple> PostVisitQuery(Query query) => Add($"POST: QUERY: {query}");
        public override ControlFlow<ValueTuple> PreVisitSelect(Select select) => Add($"PRE: SELECT: {select}");
        public override ControlFlow<ValueTuple> PostVisitSelect(Select select) => Add($"POST: SELECT: {select}");
        public override ControlFlow<ValueTuple> PreVisitRelation(ObjectName relation) => Add($"PRE: RELATION: {relation}");
        public override ControlFlow<ValueTuple> PostVisitRelation(ObjectName relation) => Add($"POST: RELATION: {relation}");
        public override ControlFlow<ValueTuple> PreVisitTableFactor(TableFactor tableFactor) => Add($"PRE: TABLE FACTOR: {tableFactor}");
        public override ControlFlow<ValueTuple> PostVisitTableFactor(TableFactor tableFactor) => Add($"POST: TABLE FACTOR: {tableFactor}");
        public override ControlFlow<ValueTuple> PreVisitExpr(Expr expr) => Add($"PRE: EXPR: {expr}");
        public override ControlFlow<ValueTuple> PostVisitExpr(Expr expr) => Add($"POST: EXPR: {expr}");
        public override ControlFlow<ValueTuple> PreVisitStatement(Statement statement) => Add($"PRE: STATEMENT: {statement}");
        public override ControlFlow<ValueTuple> PostVisitStatement(Statement statement) => Add($"POST: STATEMENT: {statement}");
        public override ControlFlow<ValueTuple> PreVisitValue(ValueWithSpan value) => Add($"PRE: VALUE: {value}");
        public override ControlFlow<ValueTuple> PostVisitValue(ValueWithSpan value) => Add($"POST: VALUE: {value}");
        public override ControlFlow<ValueTuple> PreVisitOrderBy(OrderBy orderBy) => Add($"PRE: ORDER BY: {orderBy}");
        public override ControlFlow<ValueTuple> PostVisitOrderBy(OrderBy orderBy) => Add($"POST: ORDER BY: {orderBy}");
        public override ControlFlow<ValueTuple> PreVisitOrderByExpr(OrderByExpr orderByExpr) => Add($"PRE: ORDER BY EXPR: {orderByExpr}");
        public override ControlFlow<ValueTuple> PostVisitOrderByExpr(OrderByExpr orderByExpr) => Add($"POST: ORDER BY EXPR: {orderByExpr}");
        public override ControlFlow<ValueTuple> PreVisitGroupBy(GroupByExpr groupBy) => Add($"PRE: GROUP BY: {groupBy}");
        public override ControlFlow<ValueTuple> PostVisitGroupBy(GroupByExpr groupBy) => Add($"POST: GROUP BY: {groupBy}");

        public override ControlFlow<ValueTuple> PreVisitIdent(Ident ident) =>
            _idents ? Add($"PRE: IDENT: {ident}") : ControlFlow<ValueTuple>.Continue;

        public override ControlFlow<ValueTuple> PostVisitIdent(Ident ident) =>
            _idents ? Add($"POST: IDENT: {ident}") : ControlFlow<ValueTuple>.Continue;
    }

    private static Statement Parse(string sql, Dialect? dialect = null)
    {
        var statements = Parser.ParseSql(dialect ?? new GenericDialect(), sql);
        Assert.Single(statements);
        return statements[0];
    }

    private const string ComplexSql =
        "WITH cte AS (SELECT id FROM users WHERE active = 1) "
        + "SELECT c.id, COUNT(o.id) FROM cte AS c JOIN orders AS o ON c.id = o.user_id "
        + "WHERE o.total > (SELECT AVG(total) FROM orders) GROUP BY c.id ORDER BY c.id";

    // Ordem deduzida dos campos Rust: Statement::Query → Query { with, body, order_by, .. };
    // With { cte_tables } → Cte { alias, query, .. }; Select { .., projection, .., from, .., selection, .., group_by, .. };
    // TableWithJoins { relation, joins }; Join { relation, global, join_operator }; TableFactor::Table { name [relation], alias, .. };
    // Function { name, uses_odbc_syntax, parameters, args, within_group, filter, .. }; Expr::Value(ValueWithSpan [value]).
    [Fact]
    public void Ordem_de_visita_em_consulta_com_cte_join_subconsulta_e_funcao()
    {
        const string outerSelect =
            "SELECT c.id, COUNT(o.id) FROM cte AS c JOIN orders AS o ON c.id = o.user_id "
            + "WHERE o.total > (SELECT AVG(total) FROM orders) GROUP BY c.id";
        var expected = new[]
        {
            $"PRE: STATEMENT: {ComplexSql}",
            $"PRE: QUERY: {ComplexSql}",
            // Query.with → Cte.query
            "PRE: QUERY: SELECT id FROM users WHERE active = 1",
            "PRE: SELECT: SELECT id FROM users WHERE active = 1",
            "PRE: EXPR: id",
            "POST: EXPR: id",
            "PRE: TABLE FACTOR: users",
            "PRE: RELATION: users",
            "POST: RELATION: users",
            "POST: TABLE FACTOR: users",
            "PRE: EXPR: active = 1",
            "PRE: EXPR: active",
            "POST: EXPR: active",
            "PRE: EXPR: 1",
            "PRE: VALUE: 1",
            "POST: VALUE: 1",
            "POST: EXPR: 1",
            "POST: EXPR: active = 1",
            "PRE: GROUP BY: GROUP BY ",
            "POST: GROUP BY: GROUP BY ",
            "POST: SELECT: SELECT id FROM users WHERE active = 1",
            "POST: QUERY: SELECT id FROM users WHERE active = 1",
            // Query.body
            $"PRE: SELECT: {outerSelect}",
            "PRE: EXPR: c.id",
            "POST: EXPR: c.id",
            "PRE: EXPR: COUNT(o.id)",
            "PRE: EXPR: o.id",
            "POST: EXPR: o.id",
            "POST: EXPR: COUNT(o.id)",
            "PRE: TABLE FACTOR: cte AS c",
            "PRE: RELATION: cte",
            "POST: RELATION: cte",
            "POST: TABLE FACTOR: cte AS c",
            "PRE: TABLE FACTOR: orders AS o",
            "PRE: RELATION: orders",
            "POST: RELATION: orders",
            "POST: TABLE FACTOR: orders AS o",
            "PRE: EXPR: c.id = o.user_id",
            "PRE: EXPR: c.id",
            "POST: EXPR: c.id",
            "PRE: EXPR: o.user_id",
            "POST: EXPR: o.user_id",
            "POST: EXPR: c.id = o.user_id",
            "PRE: EXPR: o.total > (SELECT AVG(total) FROM orders)",
            "PRE: EXPR: o.total",
            "POST: EXPR: o.total",
            "PRE: EXPR: (SELECT AVG(total) FROM orders)",
            "PRE: QUERY: SELECT AVG(total) FROM orders",
            "PRE: SELECT: SELECT AVG(total) FROM orders",
            "PRE: EXPR: AVG(total)",
            "PRE: EXPR: total",
            "POST: EXPR: total",
            "POST: EXPR: AVG(total)",
            "PRE: TABLE FACTOR: orders",
            "PRE: RELATION: orders",
            "POST: RELATION: orders",
            "POST: TABLE FACTOR: orders",
            "PRE: GROUP BY: GROUP BY ",
            "POST: GROUP BY: GROUP BY ",
            "POST: SELECT: SELECT AVG(total) FROM orders",
            "POST: QUERY: SELECT AVG(total) FROM orders",
            "POST: EXPR: (SELECT AVG(total) FROM orders)",
            "POST: EXPR: o.total > (SELECT AVG(total) FROM orders)",
            "PRE: GROUP BY: GROUP BY c.id",
            "PRE: EXPR: c.id",
            "POST: EXPR: c.id",
            "POST: GROUP BY: GROUP BY c.id",
            $"POST: SELECT: {outerSelect}",
            // Query.order_by
            "PRE: ORDER BY: ORDER BY c.id",
            "PRE: ORDER BY EXPR: c.id",
            "PRE: EXPR: c.id",
            "POST: EXPR: c.id",
            "POST: ORDER BY EXPR: c.id",
            "POST: ORDER BY: ORDER BY c.id",
            $"POST: QUERY: {ComplexSql}",
            $"POST: STATEMENT: {ComplexSql}",
        };

        var visitor = new RecordingVisitor();
        var flow = Parse(ComplexSql).Visit(visitor);

        Assert.Equal(ControlFlow<ValueTuple>.Continue, flow);
        Assert.Equal(expected, visitor.Visited);
    }

    // impl Visit for Ident (manual): pre_visit_ident/post_visit_ident, sem visitar o span. Cte.alias vem antes de
    // Cte.query; TableFactor::Table visita name antes de alias; Function visita name antes de args.
    [Fact]
    public void Ordem_dos_identificadores_em_consulta_complexa()
    {
        var idents = new List<string>();
        var visitor = new IdentCollector(idents);
        Parse(ComplexSql).Visit(visitor);

        Assert.Equal(
            new[]
            {
                "cte", "id", "users", "active",
                "c", "id", "COUNT", "o", "id",
                "cte", "c", "orders", "o", "c", "id", "o", "user_id",
                "o", "total", "AVG", "total", "orders",
                "c", "id",
                "c", "id",
            },
            idents);
    }

    private sealed class IdentCollector : Visitor<ValueTuple>
    {
        private readonly List<string> _idents;

        public IdentCollector(List<string> idents) => _idents = idents;

        public override ControlFlow<ValueTuple> PreVisitIdent(Ident ident)
        {
            _idents.Add(ident.Value);
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Exemplos da documentação de ast/visitor.rs (doctests do Rust), na ordem do arquivo.

    // trait Visitor (visitor.rs:149)
    private sealed class V : Visitor<ValueTuple>
    {
        public List<string> Visited { get; } = new List<string>();

        public override ControlFlow<ValueTuple> PreVisitRelation(ObjectName relation)
        {
            Visited.Add($"PRE: RELATION: {relation}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitExpr(Expr expr)
        {
            Visited.Add($"PRE: EXPR: {expr}");
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void Doc_Visitor()
    {
        var sql = "SELECT a FROM foo where x IN (SELECT y FROM bar)";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        var visitor = new V();
        statements.Visit(visitor);

        var expected = new[]
        {
            "PRE: EXPR: a",
            "PRE: RELATION: foo",
            "PRE: EXPR: x IN (SELECT y FROM bar)",
            "PRE: EXPR: x",
            "PRE: EXPR: y",
            "PRE: RELATION: bar",
        };
        Assert.Equal(visitor.Visited, expected);
    }

    // trait VisitorMut (visitor.rs:336)
    private sealed class Replacer : VisitorMut<ValueTuple>
    {
        public override ControlFlow<ValueTuple> PostVisitExpr(ref Expr expr)
        {
            if (expr is Expr.Identifier id)
            {
                expr = id with { Value = id.Value with { Value = id.Value.Value.Replace("to_replace", "replaced") } };
            }
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void Doc_VisitorMut()
    {
        var sql = "SELECT to_replace FROM foo where to_replace IN (SELECT to_replace FROM bar)";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        statements.Visit(new Replacer(), out statements);

        Assert.Equal("SELECT replaced FROM foo WHERE replaced IN (SELECT replaced FROM bar)", statements[0].ToString());
    }

    // visit_relations (visitor.rs:517)
    [Fact]
    public void Doc_visit_relations()
    {
        var sql = "SELECT a FROM foo where x IN (SELECT y FROM bar)";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        var visited = new List<string>();
        Visitor.VisitRelations(statements, relation =>
        {
            visited.Add($"RELATION: {relation}");
            return ControlFlow<ValueTuple>.Continue;
        });

        Assert.Equal(visited, new[] { "RELATION: foo", "RELATION: bar" });
    }

    // visit_relations_mut (visitor.rs:557)
    [Fact]
    public void Doc_visit_relations_mut()
    {
        var sql = "SELECT a FROM foo";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        // table.0[0] = ObjectNamePart::Identifier(Ident::new("bar"))
        Visitor.VisitRelationsMut(ref statements, (ref ObjectName table) =>
        {
            var parts = table.Value.ToList();
            parts[0] = new ObjectNamePart.Identifier(new Ident("bar"));
            table = new ObjectName(parts.ToSequence());
            return ControlFlow<ValueTuple>.Continue;
        });

        Assert.Equal("SELECT a FROM bar", statements[0].ToString());
    }

    // visit_expressions (visitor.rs:605)
    [Fact]
    public void Doc_visit_expressions()
    {
        var sql = "SELECT a FROM foo where x IN (SELECT y FROM bar)";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        var visited = new List<string>();
        Visitor.VisitExpressions(statements, expr =>
        {
            visited.Add($"EXPR: {expr}");
            return ControlFlow<ValueTuple>.Continue;
        });

        Assert.Equal(visited, new[] { "EXPR: a", "EXPR: x IN (SELECT y FROM bar)", "EXPR: x", "EXPR: y" });
    }

    // visit_expressions_mut, "Remove all select limits in sub-queries" (visitor.rs:649)
    [Fact]
    public void Doc_visit_expressions_mut_remove_limits()
    {
        var sql = "SELECT (SELECT y FROM z LIMIT 9) FROM t LIMIT 3";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        Visitor.VisitExpressionsMut(ref statements, (ref Expr expr) =>
        {
            if (expr is Expr.Subquery q)
            {
                expr = q with { Value = q.Value with { LimitClause = null } };
            }
            return ControlFlow<ValueTuple>.Continue;
        });

        Assert.Equal("SELECT (SELECT y FROM z) FROM t LIMIT 3", statements[0].ToString());
    }

    // visit_expressions_mut, "Wrap column name in function call" (visitor.rs:669)
    [Fact]
    public void Doc_visit_expressions_mut_wrap_in_function()
    {
        var sql = "SELECT x, y FROM t";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        Visitor.VisitExpressionsMut(ref statements, (ref Expr expr) =>
        {
            if (expr is Expr.Identifier { Value: var colName } && colName.Value == "x")
            {
                var oldExpr = expr;
                expr = new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(new Ident("f"))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.List(new FunctionArgumentList(
                        DuplicateTreatment: null,
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(oldExpr))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                    WithinGroup: Sequence<OrderByExpr>.Empty,
                    Filter: null,
                    NullTreatment: null,
                    Over: null));
            }
            return ControlFlow<ValueTuple>.Continue;
        });

        Assert.Equal("SELECT f(x), y FROM t", statements[0].ToString());
    }

    // visit_statements (visitor.rs:736)
    [Fact]
    public void Doc_visit_statements()
    {
        var sql = "SELECT a FROM foo where x IN (SELECT y FROM bar); CREATE TABLE baz(q int)";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        var visited = new List<string>();
        Visitor.VisitStatements(statements, stmt =>
        {
            visited.Add($"STATEMENT: {stmt}");
            return ControlFlow<ValueTuple>.Continue;
        });

        Assert.Equal(
            visited,
            new[]
            {
                "STATEMENT: SELECT a FROM foo WHERE x IN (SELECT y FROM bar)",
                "STATEMENT: CREATE TABLE baz (q INT)",
            });
    }

    // visit_statements_mut (visitor.rs:773)
    [Fact]
    public void Doc_visit_statements_mut()
    {
        var sql = "SELECT x FROM foo LIMIT 9+$limit; SELECT * FROM t LIMIT f()";
        var statements = Parser.ParseSql(new GenericDialect(), sql);

        // Remove all select limits in outer statements (not in sub-queries)
        Visitor.VisitStatementsMut(ref statements, (ref Statement stmt) =>
        {
            if (stmt is Statement.Query q)
            {
                stmt = q with { Value = q.Value with { LimitClause = null } };
            }
            return ControlFlow<ValueTuple>.Continue;
        });

        Assert.Equal("SELECT x FROM foo", statements[0].ToString());
        Assert.Equal("SELECT * FROM t", statements[1].ToString());
    }

    // Corpo de `test_visit_order` (tests/sqlparser_common.rs:16400, #[cfg(feature = "visitor")]), que no `main`
    // está com Skip PENDENTE em SqlParserCommonTests4 à espera do visitor. Aqui com Parser.ParseSql no lugar
    // de verified_stmt; a tradução para lá está em docs/port/onda-c/C18.md.
    [Fact]
    public void Corpo_de_test_visit_order_de_sqlparser_common()
    {
        var sql = "SELECT CASE a WHEN 1 THEN 2 WHEN 3 THEN 4 ELSE 5 END";
        var stmt = Parse(sql);
        var visited = new List<string>();
        _ = Visitor.VisitExpressions(stmt, expr =>
        {
            visited.Add(expr.ToString());
            return ControlFlow<ValueTuple>.Continue;
        });

        Assert.Equal(
            visited,
            new[] { "CASE a WHEN 1 THEN 2 WHEN 3 THEN 4 ELSE 5 END", "a", "1", "2", "3", "4", "5" });
    }

    // ---------------------------------------------------------------------------------------------
    // ControlFlow::Break, VisitMut sobre AST imutável, ganchos que trocam o nó, tipos genéricos e pilha.

    private sealed class BreakOnRelation : Visitor<string>
    {
        public List<string> Visited { get; } = new List<string>();

        public override ControlFlow<string> PreVisitRelation(ObjectName relation)
        {
            Visited.Add(relation.ToString());
            return relation.ToString() == "t2" ? ControlFlow<string>.Break("parou em t2") : ControlFlow<string>.Continue;
        }

        public override ControlFlow<string> PostVisitStatement(Statement statement)
        {
            Visited.Add("POST: STATEMENT");
            return ControlFlow<string>.Continue;
        }
    }

    [Fact]
    public void Break_interrompe_a_visita_e_devolve_o_valor()
    {
        var visitor = new BreakOnRelation();
        var flow = Parse("SELECT * FROM t1 JOIN t2 ON a = b JOIN t3 ON c = d").Visit(visitor);

        Assert.True(flow.IsBreak);
        Assert.Equal("parou em t2", flow.BreakValue().Value);
        Assert.Equal(ControlFlow<string>.Break("parou em t2"), flow);
        // Nem t3 nem o post_visit_statement chegam a ser chamados.
        Assert.Equal(new[] { "t1", "t2" }, visitor.Visited);
    }

    private sealed class NoOpMutator : VisitorMut<ValueTuple>
    {
        public int Count { get; private set; }

        public override ControlFlow<ValueTuple> PreVisitExpr(ref Expr expr)
        {
            Count++;
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void VisitMut_sem_mudancas_devolve_a_mesma_instancia()
    {
        var statements = Parser.ParseSql(new GenericDialect(), ComplexSql + "; SELECT 1");
        var original = statements;
        var first = statements[0];
        var mutator = new NoOpMutator();

        var flow = statements.Visit(mutator, out statements);

        Assert.Equal(ControlFlow<ValueTuple>.Continue, flow);
        Assert.True(mutator.Count > 0);
        Assert.True(statements.IsSameStorage(original));
        Assert.Same(first, statements[0]);
    }

    private sealed class UppercaseUntil : VisitorMut<ValueTuple>
    {
        private readonly string _stopAt;

        public UppercaseUntil(string stopAt) => _stopAt = stopAt;

        public override ControlFlow<ValueTuple> PreVisitIdent(ref Ident ident)
        {
            var original = ident.Value;
            ident = ident with { Value = ident.Value.ToUpperInvariant() };
            return original == _stopAt ? ControlFlow<ValueTuple>.Break(default) : ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void VisitMut_com_break_mantem_as_mudancas_anteriores()
    {
        var statement = Parse("SELECT a, b, c FROM t");
        var original = statement;

        var flow = statement.Visit(new UppercaseUntil("b"), out statement);

        // Como no Rust (&mut no lugar): o que foi alterado antes do Break fica alterado.
        Assert.True(flow.IsBreak);
        Assert.Equal("SELECT A, B, c FROM t", statement.ToString());
        Assert.Equal("SELECT a, b, c FROM t", original.ToString());
    }

    private sealed class ReplaceXAndRecordIdents : VisitorMut<ValueTuple>
    {
        public List<string> Idents { get; } = new List<string>();

        public override ControlFlow<ValueTuple> PreVisitExpr(ref Expr expr)
        {
            if (expr is Expr.Identifier { Value.Value: "x" })
            {
                expr = new Expr.BinaryOp(
                    new Expr.Identifier(new Ident("y")),
                    new BinaryOperator.Plus(),
                    new Expr.Identifier(new Ident("z")));
            }
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitIdent(ref Ident ident)
        {
            Idents.Add(ident.Value);
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void Gancho_pre_que_troca_o_no_faz_a_visita_seguir_pelos_filhos_do_no_novo()
    {
        var statement = Parse("SELECT x FROM t");
        var visitor = new ReplaceXAndRecordIdents();

        statement.Visit(visitor, out statement);

        // Rust: depois de `*expr = ..` em pre_visit_expr, o `match self` do derive já vê o nó novo.
        Assert.Equal(new[] { "y", "z", "t" }, visitor.Idents);
        Assert.Equal("SELECT y + z FROM t", statement.ToString());
    }

    // CreateTable.order_by: Option<OneOrManyWithParens<Expr>> (ClickHouse): tipo genérico, visitado com
    // despacho em tempo de execução (o `T: Visit` do Rust).
    [Fact]
    public void Tipo_generico_OneOrManyWithParens_e_visitado_e_reescrito()
    {
        var statement = Parse("CREATE TABLE t (a INT, b INT) ENGINE = MergeTree ORDER BY (a, b)", new ClickHouseDialect());
        var visitor = new RecordingVisitor();
        statement.Visit(visitor);

        Assert.Equal(
            new[]
            {
                "PRE: STATEMENT: CREATE TABLE t (a INT, b INT) ENGINE = MergeTree ORDER BY (a, b)",
                "PRE: RELATION: t",
                "POST: RELATION: t",
                "PRE: EXPR: a",
                "POST: EXPR: a",
                "PRE: EXPR: b",
                "POST: EXPR: b",
                "POST: STATEMENT: CREATE TABLE t (a INT, b INT) ENGINE = MergeTree ORDER BY (a, b)",
            },
            visitor.Visited);

        Visitor.VisitExpressionsMut(ref statement, (ref Expr expr) =>
        {
            if (expr is Expr.Identifier { Value.Value: "b" } id)
            {
                expr = id with { Value = new Ident("c") };
            }
            return ControlFlow<ValueTuple>.Continue;
        });
        Assert.Equal("CREATE TABLE t (a INT, b INT) ENGINE = MergeTree ORDER BY (a, c)", statement.ToString());
    }

    private sealed class CountingVisitor : Visitor<ValueTuple>
    {
        public int Exprs { get; private set; }

        public List<string> Values { get; } = new List<string>();

        public override ControlFlow<ValueTuple> PreVisitExpr(Expr expr)
        {
            Exprs++;
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitValue(ValueWithSpan value)
        {
            Values.Add(value.Value.ToString());
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    private sealed class RedactValues : VisitorMut<ValueTuple>
    {
        public override ControlFlow<ValueTuple> PreVisitValue(ref ValueWithSpan value)
        {
            value = value with { Value = new Value.SingleQuotedString("R") };
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    // `recursive::recursive` no `visit` gerado (DIV-009): uma cadeia de 5000 OR (Expr com 5000 níveis à esquerda)
    // é visitada sem estourar a pilha, nos dois visitors.
    [Fact]
    public void Visita_muito_profunda_nao_estoura_a_pilha()
    {
        const int n = 5000;
        var cond = string.Join(" OR ", Enumerable.Range(0, n).Select(i => FormattableString.Invariant($"X = {i}")));
        var statement = Parse($"SELECT x WHERE {cond}");

        var counter = new CountingVisitor();
        Assert.Equal(ControlFlow<ValueTuple>.Continue, statement.Visit(counter));
        Assert.Equal(1 + n * 3 + (n - 1), counter.Exprs);
        Assert.Equal(n, counter.Values.Count);

        Assert.Equal(ControlFlow<ValueTuple>.Continue, statement.Visit(new RedactValues(), out var redacted));
        var after = new CountingVisitor();
        redacted.Visit(after);
        Assert.Equal(n, after.Values.Count);
        Assert.All(after.Values, v => Assert.Equal("'R'", v));
    }
}
