// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_common.rs (linhas 16423–20017).
// Modificado: tradução de Rust para C#.

using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C5.md):
// - Cada fatia de tests/sqlparser_common.rs é uma classe própria; as funções auxiliares do arquivo
//   Rust usadas aqui (`verified_stmt`, `verified_only_select`, `parse_sql_statements`,
//   `one_statement_parses_to`, `non_left_associative_dialects`) estão copiadas como métodos privados.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000). `assert_eq!(x, true/false)` vira `Assert.True/False` (xUnit2004).
// - `assert_eq!(n, v.len())` com n = 0/1 fica `Assert.Equal`, como no Rust (xUnit2013 suprimido).
// - `unreachable!()` → `throw Unreachable()`; `panic!(msg)` → `Assert.Fail(msg)`;
//   `.unwrap()` de `Option` → `Unwrap(x)`; `Vec::pop().unwrap()` → `Pop(lista)`.
// - `thread::spawn` + `mpsc::channel` + `recv_timeout(..).expect(..)` → `SpawnAndRecvTimeout`.
#pragma warning disable xUnit2013
public class SqlParserCommonTests5
{
    // ---- Funções auxiliares do arquivo Rust (sqlparser_common.rs, linhas 10994–11020) ----

    private static Sequence<Statement> ParseSqlStatements(string sql) =>
        TestUtils.AllDialects().ParseSqlStatements(sql);

    private static Statement OneStatementParsesTo(string sql, string canonical) =>
        TestUtils.AllDialects().OneStatementParsesTo(sql, canonical);

    private static TestedDialects NonLeftAssociativeDialects() =>
        TestUtils.AllDialectsWhere(d => !d.SupportsLeftAssociativeJoinsWithoutParens);

    private static Statement VerifiedStmt(string query) =>
        TestUtils.AllDialects().VerifiedStmt(query);

    // fn verified_query(..) (usada a partir do upstream #2586, Rodada 1 de sincronização)
    private static Query VerifiedQuery(string query) =>
        TestUtils.AllDialects().VerifiedQuery(query);

    private static Select VerifiedOnlySelect(string query) =>
        TestUtils.AllDialects().VerifiedOnlySelect(query);

    // fn pg_and_generic() -> TestedDialects (sqlparser_common.rs, linha 1717; usada a partir do
    // upstream #2586, Rodada 1 de sincronização)
    private static TestedDialects PgAndGeneric() =>
        new TestedDialects(new List<Dialect>
        {
            new PostgreSqlDialect(),
            new GenericDialect(),
        });

    // ---- Tradução de construções da std do Rust (sem equivalente direto em C#) ----

    // Rust: `unreachable!()` / `unreachable!(msg)`.
    private static InvalidOperationException Unreachable() =>
        new InvalidOperationException("internal error: entered unreachable code");

    private static InvalidOperationException Unreachable(string message) =>
        new InvalidOperationException($"internal error: entered unreachable code: {message}");

    // Rust: `Option::unwrap()` (falha em `None`).
    private static T Unwrap<T>(T? value)
        where T : class =>
        value ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");

    // Rust: `Vec::pop().unwrap()`.
    private static T Pop<T>(List<T> list)
    {
        if (list.Count == 0)
        {
            throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");
        }
        var last = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);
        return last;
    }

    // Pilha padrão de uma thread criada com `std::thread::spawn` (2 MiB).
    private const int RustThreadStackSize = 2 * 1024 * 1024;

    // Rust:
    //     let (tx, rx) = mpsc::channel();
    //     thread::spawn(move || { <body>; let _ = tx.send(()); });
    //     rx.recv_timeout(timeout).expect(expect_message);
    // Se o corpo lançar uma exceção (o `panic!` da thread no Rust), o `tx` é descartado sem enviar e
    // o `recv_timeout` falha com `Disconnected`; se o prazo vencer, com `Timeout`.
    // `stackSize`: `thread::Builder::new().stack_size(n).spawn(..)` (upstream #2619, Rodada 1 de
    // sincronização); sem ele, a pilha padrão do `std::thread::spawn`.
    private static void SpawnAndRecvTimeout(
        System.Action body,
        TimeSpan timeout,
        string expectMessage,
        int stackSize = RustThreadStackSize)
    {
        var sent = false;
        Exception? panic = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    body();
                    sent = true; // let _ = tx.send(());
                }
                catch (Exception e)
                {
                    panic = e;
                }
            },
            stackSize)
        {
            IsBackground = true,
        };
        thread.Start();
        if (!thread.Join(timeout))
        {
            Assert.Fail($"{expectMessage}: Timeout");
        }
        if (!sent)
        {
            Assert.Fail($"{expectMessage}: Disconnected ({panic})");
        }
    }

    [Fact]
    public void parse_case_statement()
    {
        var sql = "CASE 1 WHEN 2 THEN SELECT 1; SELECT 2; ELSE SELECT 3; END CASE";
        if (VerifiedStmt(sql) is not Statement.Case { Value: var stmt })
        {
            throw Unreachable();
        }

        Assert.Equal(Expr.MakeValue(TestUtils.Number("1")), stmt.MatchExpr);
        Assert.Equal(
            Expr.MakeValue(TestUtils.Number("2")),
            stmt.WhenBlocks[0].Condition);
        Assert.Equal(2, stmt.WhenBlocks[0].Statements().Count);
        Assert.Equal(1, Unwrap(stmt.ElseBlock).Statements().Count);

        VerifiedStmt(
            "CASE 1" +
            " WHEN a THEN" +
            " SELECT 1; SELECT 2; SELECT 3;" +
            " WHEN b THEN" +
            " SELECT 4; SELECT 5;" +
            " ELSE" +
            " SELECT 7; SELECT 8;" +
            " END CASE");
        VerifiedStmt(
            "CASE 1" +
            " WHEN a THEN" +
            " SELECT 1; SELECT 2; SELECT 3;" +
            " WHEN b THEN" +
            " SELECT 4; SELECT 5;" +
            " END CASE");
        VerifiedStmt(
            "CASE 1" +
            " WHEN a THEN" +
            " SELECT 1; SELECT 2; SELECT 3;" +
            " END CASE");
        VerifiedStmt(
            "CASE 1" +
            " WHEN a THEN" +
            " SELECT 1; SELECT 2; SELECT 3;" +
            " END");

        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: THEN, found: END"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("CASE 1 WHEN a END")).Error);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: WHEN, found: ELSE"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("CASE 1 ELSE SELECT 1; END")).Error);
    }

    [Fact]
    public void test_case_statement_span()
    {
        var sql = "CASE 1 WHEN 2 THEN SELECT 1; SELECT 2; ELSE SELECT 3; END CASE";
        var parser = new Parser(new GenericDialect()).TryWithSql(sql);
        // `sql.len()` conta bytes; o SQL é ASCII, então equivale a `sql.Length`.
        Assert.Equal(
            parser.ParseStatement().Span(),
            new Span(new Location(1, 1), new Location(1, sql.Length + 1)));
    }

    [Fact]
    public void parse_if_statement()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d is MsSqlDialect);

        var sql = "IF 1 THEN SELECT 1; ELSEIF 2 THEN SELECT 2; ELSE SELECT 3; END IF";
        if (dialects.VerifiedStmt(sql) is not Statement.If
            {
                Value: { IfBlock: var ifBlock, ElseifBlocks: var elseifBlocks, ElseBlock: var elseBlock },
            })
        {
            throw Unreachable();
        }
        Assert.Equal(Expr.MakeValue(TestUtils.Number("1")), ifBlock.Condition);
        Assert.Equal(Expr.MakeValue(TestUtils.Number("2")), elseifBlocks[0].Condition);
        Assert.Equal(1, Unwrap(elseBlock).Statements().Count);

        dialects.VerifiedStmt(
            "IF 1 THEN" +
            " SELECT 1;" +
            " SELECT 2;" +
            " SELECT 3;" +
            " ELSEIF 2 THEN" +
            " SELECT 4;" +
            " SELECT 5;" +
            " ELSEIF 3 THEN" +
            " SELECT 6;" +
            " SELECT 7;" +
            " ELSE" +
            " SELECT 8;" +
            " SELECT 9;" +
            " END IF");
        dialects.VerifiedStmt(
            "IF 1 THEN" +
            " SELECT 1;" +
            " SELECT 2;" +
            " ELSE" +
            " SELECT 3;" +
            " SELECT 4;" +
            " END IF");
        dialects.VerifiedStmt(
            "IF 1 THEN" +
            " SELECT 1;" +
            " SELECT 2;" +
            " SELECT 3;" +
            " ELSEIF 2 THEN" +
            " SELECT 3;" +
            " SELECT 4;" +
            " END IF");
        dialects.VerifiedStmt("IF 1 THEN" + " SELECT 1;" + " SELECT 2;" + " END IF");
        dialects.VerifiedStmt(
            "IF (1) THEN" +
            " SELECT 1;" +
            " SELECT 2;" +
            " END IF");
        dialects.VerifiedStmt("IF 1 THEN END IF");
        dialects.VerifiedStmt("IF 1 THEN SELECT 1; ELSEIF 1 THEN END IF");

        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: IF, found: EOF"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("IF 1 THEN SELECT 1; ELSEIF 1 THEN SELECT 2; END")).Error);
    }

    [Fact]
    public void test_if_statement_span()
    {
        var sql = "IF 1=1 THEN SELECT 1; ELSEIF 1=2 THEN SELECT 2; ELSE SELECT 3; END IF";
        var parser = new Parser(new GenericDialect()).TryWithSql(sql);
        Assert.Equal(
            parser.ParseStatement().Span(),
            new Span(new Location(1, 1), new Location(1, sql.Length + 1)));
    }

    [Fact]
    public void test_if_statement_multiline_span()
    {
        var sqlLine1 = "IF 1 = 1 THEN SELECT 1;";
        var sqlLine2 = "ELSEIF 1 = 2 THEN SELECT 2;";
        var sqlLine3 = "ELSE SELECT 3;";
        var sqlLine4 = "END IF";
        var sql = string.Join("\n", new[] { sqlLine1, sqlLine2, sqlLine3, sqlLine4 });
        var parser = new Parser(new GenericDialect()).TryWithSql(sql);
        Assert.Equal(
            parser.ParseStatement().Span(),
            new Span(
                new Location(1, 1),
                new Location(4, sqlLine4.Length + 1)));
    }

    [Fact]
    public void test_conditional_statement_span()
    {
        var sql = "IF 1=1 THEN SELECT 1; ELSEIF 1=2 THEN SELECT 2; ELSE SELECT 3; END IF";
        var parser = new Parser(new GenericDialect()).TryWithSql(sql);
        switch (parser.ParseStatement())
        {
            case Statement.If { Value: { IfBlock: var ifBlock, ElseifBlocks: var elseifBlocks, ElseBlock: var elseBlock } }:
                Assert.Equal(
                    new Span(new Location(1, 1), new Location(1, 21)),
                    ifBlock.Span());
                Assert.Equal(
                    new Span(new Location(1, 23), new Location(1, 47)),
                    elseifBlocks[0].Span());
                Assert.Equal(
                    new Span(new Location(1, 49), new Location(1, 62)),
                    Unwrap(elseBlock).Span());
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }
    }

    [Fact]
    public void parse_raise_statement()
    {
        var sql = "RAISE USING MESSAGE = 42";
        if (VerifiedStmt(sql) is not Statement.Raise { Value: var stmt })
        {
            throw Unreachable();
        }
        Assert.Equal(
            new RaiseStatementValue.UsingMessage(Expr.MakeValue(TestUtils.Number("42"))),
            stmt.Value);

        VerifiedStmt("RAISE USING MESSAGE = 'error'");
        VerifiedStmt("RAISE myerror");
        VerifiedStmt("RAISE 42");
        VerifiedStmt("RAISE using");
        VerifiedStmt("RAISE");

        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: =, found: error"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("RAISE USING MESSAGE error")).Error);
    }

    [Fact]
    public void test_lambdas()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsLambdaFunctions);

        var sql =
            "SELECT array_sort(array('Hello', 'World'), " +
                "(p1, p2) -> CASE WHEN p1 = p2 THEN 0 " +
                            "WHEN reverse(p1) < reverse(p2) THEN -1 " +
                            "ELSE 1 END)";
        Assert.Equal(
            new SelectItem.UnnamedExpr(TestUtils.Call(
                "array_sort",
                new Expr[]
                {
                    TestUtils.Call(
                        "array",
                        new Expr[]
                        {
                            new Expr.Value(
                                new Value.SingleQuotedString("Hello").WithEmptySpan()),
                            new Expr.Value(
                                new Value.SingleQuotedString("World").WithEmptySpan()),
                        }),
                    new Expr.Lambda(new LambdaFunction(
                        Params: new OneOrManyWithParens<LambdaFunctionParameter>.Many(Sequence.Of(
                            new LambdaFunctionParameter(
                                Name: new Ident("p1"),
                                DataType: null),
                            new LambdaFunctionParameter(
                                Name: new Ident("p2"),
                                DataType: null))),
                        Body: new Expr.Case(
                            CaseToken: AttachedToken.Empty(),
                            EndToken: AttachedToken.Empty(),
                            Operand: null,
                            Conditions: Sequence.Of(
                                new CaseWhen(
                                    Condition: new Expr.BinaryOp(
                                        Left: new Expr.Identifier(new Ident("p1")),
                                        Op: new BinaryOperator.Eq(),
                                        Right: new Expr.Identifier(new Ident("p2"))),
                                    Result: Expr.MakeValue(TestUtils.Number("0"))),
                                new CaseWhen(
                                    Condition: new Expr.BinaryOp(
                                        Left: TestUtils.Call(
                                            "reverse",
                                            new Expr[] { new Expr.Identifier(new Ident("p1")) }),
                                        Op: new BinaryOperator.Lt(),
                                        Right: TestUtils.Call(
                                            "reverse",
                                            new Expr[] { new Expr.Identifier(new Ident("p2")) })),
                                    Result: new Expr.UnaryOp(
                                        Op: new UnaryOperator.Minus(),
                                        Expr: Expr.MakeValue(TestUtils.Number("1"))))),
                            ElseResult: Expr.MakeValue(TestUtils.Number("1"))),
                        Syntax: LambdaSyntax.Arrow)),
                })),
            dialects.VerifiedOnlySelect(sql).Projection[0]);

        dialects.VerifiedExpr(
            "map_zip_with(map(1, 'a', 2, 'b'), map(1, 'x', 2, 'y'), (k, v1, v2) -> concat(v1, v2))");
        dialects.VerifiedExpr("transform(array(1, 2, 3), x -> x + 1)");

        // Ensure all lambda variants are parsed correctly
        dialects.VerifiedExpr("a -> a * 2"); // Single parameter without type
        dialects.VerifiedExpr("a INT -> a * 2"); // Single parameter with type
        dialects.VerifiedExpr("(a, b) -> a * b"); // Multiple parameters without types
        dialects.VerifiedExpr("(a INT, b FLOAT) -> a * b"); // Multiple parameters with types
    }

    [Fact]
    public void test_select_from_first()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsFromFirstSelect);
        var q1 = "FROM capitals";
        var q2 = "FROM capitals SELECT *";

        foreach (var (q, flavor, projection) in new (string, SelectFlavor, Sequence<SelectItem>)[]
        {
            (q1, SelectFlavor.FromFirstNoSelect, Sequence<SelectItem>.Empty),
            (
                q2,
                SelectFlavor.FromFirst,
                Sequence.Of<SelectItem>(new SelectItem.Wildcard(WildcardAdditionalOptions.Default()))
            ),
        })
        {
            var ast = dialects.VerifiedQuery(q);
            var expected = new Query(
                With: null,
                Body: new SetExpr.Select(new Select(
                    SelectToken: AttachedToken.Empty(),
                    OptimizerHints: Sequence<OptimizerHint>.Empty,
                    Distinct: null,
                    SelectModifiers: null,
                    Top: null,
                    Projection: projection,
                    Exclude: null,
                    TopBeforeDistinct: false,
                    Into: null,
                    From: Sequence.Of(new TableWithJoins(
                        Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident(
                            "capitals",
                            null,
                            Span.Empty())))),
                        Joins: Sequence<Join>.Empty)),
                    LateralViews: Sequence<LateralView>.Empty,
                    Prewhere: null,
                    Selection: null,
                    GroupBy: new GroupByExpr.Expressions(Sequence<Expr>.Empty, Sequence<GroupByWithModifier>.Empty),
                    ClusterBy: Sequence<Expr>.Empty,
                    DistributeBy: Sequence<Expr>.Empty,
                    SortBy: Sequence<OrderByExpr>.Empty,
                    Having: null,
                    NamedWindow: Sequence<NamedWindowDefinition>.Empty,
                    WindowBeforeQualify: false,
                    Qualify: null,
                    ValueTableMode: null,
                    ConnectBy: Sequence<ConnectByKind>.Empty,
                    Flavor: flavor)),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty);
            Assert.Equal(expected, ast);
        }
    }

    [Fact]
    public void test_select_from_first_with_cte()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsFromFirstSelect);
        var q = "WITH test AS (FROM t SELECT a) FROM test SELECT 1";

        var ast = dialects.VerifiedQuery(q);

        var astSelect = Unwrap(ast.Body.AsSelect());

        var expectedBodySelectProjection =
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Value(new ValueWithSpan(
                Value: TestUtils.Number("1"),
                Span: Span.Empty()))));

        var expectedBodyFrom = Sequence.Of(new TableWithJoins(
            Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident(
                "test",
                null,
                Span.Empty())))),
            Joins: Sequence<Join>.Empty));

        Assert.Equal(astSelect.Projection, expectedBodySelectProjection);
        Assert.Equal(astSelect.From, expectedBodyFrom);
    }

    [Fact]
    public void test_geometric_unary_operators()
    {
        // Number of points in path or polygon
        var sql = "# path '((1,0),(0,1),(-1,0))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.UnaryOp { Op: UnaryOperator.Hash });

        // Length or circumference
        sql = "@-@ path '((0,0),(1,0))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.UnaryOp { Op: UnaryOperator.AtDashAt });

        // Center
        sql = "@@ circle '((0,0),10)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.UnaryOp { Op: UnaryOperator.DoubleAt });
        // Is horizontal?
        sql = "?- lseg '((-1,0),(1,0))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.UnaryOp { Op: UnaryOperator.QuestionDash });

        // Is vertical?
        sql = "?| lseg '((-1,0),(1,0))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.UnaryOp { Op: UnaryOperator.QuestionPipe });
    }

    [Fact]
    public void test_geometry_type()
    {
        var sql = "point '1,2'";
        Assert.Equal(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql),
            new Expr.TypedString(new TypedString(
                DataType: new DataType.GeometricType(new GeometricTypeKind.Point()),
                Value: new ValueWithSpan(
                    Value: new Value.SingleQuotedString("1,2"),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)));

        sql = "line '1,2,3,4'";
        Assert.Equal(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql),
            new Expr.TypedString(new TypedString(
                DataType: new DataType.GeometricType(new GeometricTypeKind.Line()),
                Value: new ValueWithSpan(
                    Value: new Value.SingleQuotedString("1,2,3,4"),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)));

        sql = "path '1,2,3,4'";
        Assert.Equal(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql),
            new Expr.TypedString(new TypedString(
                DataType: new DataType.GeometricType(new GeometricTypeKind.GeometricPath()),
                Value: new ValueWithSpan(
                    Value: new Value.SingleQuotedString("1,2,3,4"),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)));
        sql = "box '1,2,3,4'";
        Assert.Equal(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql),
            new Expr.TypedString(new TypedString(
                DataType: new DataType.GeometricType(new GeometricTypeKind.GeometricBox()),
                Value: new ValueWithSpan(
                    Value: new Value.SingleQuotedString("1,2,3,4"),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)));

        sql = "circle '1,2,3'";
        Assert.Equal(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql),
            new Expr.TypedString(new TypedString(
                DataType: new DataType.GeometricType(new GeometricTypeKind.Circle()),
                Value: new ValueWithSpan(
                    Value: new Value.SingleQuotedString("1,2,3"),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)));

        sql = "polygon '1,2,3,4'";
        Assert.Equal(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql),
            new Expr.TypedString(new TypedString(
                DataType: new DataType.GeometricType(new GeometricTypeKind.Polygon()),
                Value: new ValueWithSpan(
                    Value: new Value.SingleQuotedString("1,2,3,4"),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)));
        sql = "lseg '1,2,3,4'";
        Assert.Equal(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql),
            new Expr.TypedString(new TypedString(
                DataType: new DataType.GeometricType(new GeometricTypeKind.LineSegment()),
                Value: new ValueWithSpan(
                    Value: new Value.SingleQuotedString("1,2,3,4"),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)));
    }

    [Fact]
    public void test_geometric_binary_operators()
    {
        // Translation plus
        var sql = "box '((0,0),(1,1))' + point '(2.0,0)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.Plus });
        // Translation minus
        sql = "box '((0,0),(1,1))' - point '(2.0,0)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.Minus });

        // Scaling multiply
        sql = "box '((0,0),(1,1))' * point '(2.0,0)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.Multiply });

        // Scaling divide
        sql = "box '((0,0),(1,1))' / point '(2.0,0)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.Divide });

        // Intersection
        sql = "'((1,-1),(-1,1))' # '((1,1),(-1,-1))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.PGBitwiseXor });

        //Point of closest proximity
        sql = "point '(0,0)' ## lseg '((2,0),(0,2))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.DoubleHash });

        // Point of closest proximity
        sql = "box '((0,0),(1,1))' && box '((0,0),(2,2))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.PGOverlap });

        // Overlaps to left?
        sql = "box '((0,0),(1,1))' &< box '((0,0),(2,2))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.AndLt });

        // Overlaps to right?
        sql = "box '((0,0),(3,3))' &> box '((0,0),(2,2))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.AndGt });

        // Distance between
        sql = "circle '((0,0),1)' <-> circle '((5,0),1)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.LtDashGt });

        // Is left of?
        sql = "circle '((0,0),1)' << circle '((5,0),1)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.PGBitwiseShiftLeft });

        // Is right of?
        sql = "circle '((5,0),1)' >> circle '((0,0),1)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.PGBitwiseShiftRight });

        // Is below?
        sql = "circle '((0,0),1)' <^ circle '((0,5),1)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.LtCaret });

        // 	Intersects or overlaps
        sql = "lseg '((-1,0),(1,0))' ?# box '((-2,-2),(2,2))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.QuestionHash });

        // Is horizontal?
        sql = "point '(1,0)' ?- point '(0,0)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.QuestionDash });

        // Is perpendicular?
        sql = "lseg '((0,0),(0,1))' ?-| lseg '((0,0),(1,0))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.QuestionDashPipe });

        // Is vertical?
        sql = "point '(0,1)' ?| point '(0,0)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.QuestionPipe });

        // Are parallel?
        sql = "lseg '((-1,0),(1,0))' ?|| lseg '((-1,2),(1,2))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.QuestionDoublePipe });

        // Contained or on?
        sql = "point '(1,1)' @ circle '((0,0),2)'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.At });

        //
        // Same as?
        sql = "polygon '((0,0),(1,1))' ~= polygon '((1,1),(0,0))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.TildeEq });

        // Is strictly below?
        sql = "box '((0,0),(3,3))' <<| box '((3,4),(5,5))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.LtLtPipe });

        // Is strictly above?
        sql = "box '((3,4),(5,5))' |>> box '((0,0),(3,3))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.PipeGtGt });

        // Does not extend above?
        sql = "box '((0,0),(1,1))' &<| box '((0,0),(2,2))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.AndLtPipe });

        // Does not extend below?
        sql = "box '((0,0),(3,3))' |&> box '((0,0),(2,2))'";
        Assert.True(
            TestUtils.AllDialectsWhere(d => d.SupportsGeometricTypes).VerifiedExpr(sql)
                is Expr.BinaryOp { Op: BinaryOperator.PipeAndGt });
    }

    [Fact]
    public void parse_array_type_def_with_brackets()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsArrayTypedefWithBrackets);
        dialects.VerifiedStmt("SELECT x::INT[]");
        dialects.VerifiedStmt("SELECT STRING_TO_ARRAY('1,2,3', ',')::INT[3]");
    }

    [Fact]
    public void parse_set_names()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsSetNames);
        dialects.VerifiedStmt("SET NAMES 'UTF8'");
        dialects.VerifiedStmt("SET NAMES 'utf8'");
        dialects.VerifiedStmt("SET NAMES UTF8 COLLATE bogus");
    }

    [Fact]
    public void parse_pipe_operator_as()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> AS new_users");
    }

    [Fact]
    public void parse_pipe_operator_select()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> SELECT id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> SELECT id, name");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> SELECT id user_id",
            "SELECT * FROM tbl |> SELECT id AS user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> SELECT id AS user_id");
    }

    [Fact]
    public void parse_pipe_operator_extend()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> EXTEND id + 1 AS new_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> EXTEND id AS new_id, name AS new_name");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> EXTEND id user_id",
            "SELECT * FROM tbl |> EXTEND id AS user_id");
    }

    [Fact]
    public void parse_pipe_operator_set()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> SET id = id + 1");
        dialects.VerifiedStmt("SELECT * FROM tbl |> SET id = id + 1, name = name + ' Doe'");
    }

    [Fact]
    public void parse_pipe_operator_drop()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> DROP id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> DROP id, name");
        dialects.VerifiedStmt("SELECT * FROM tbl |> DROP c |> RENAME a AS x");
        dialects.VerifiedStmt("SELECT * FROM tbl |> DROP a, b |> SELECT c");
    }

    [Fact]
    public void parse_pipe_operator_limit()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> LIMIT 10");
        dialects.VerifiedStmt("SELECT * FROM tbl |> LIMIT 10 OFFSET 5");
        dialects.VerifiedStmt("SELECT * FROM tbl |> LIMIT 10 |> LIMIT 5");
        dialects.VerifiedStmt("SELECT * FROM tbl |> LIMIT 10 |> WHERE true");
    }

    [Fact]
    public void parse_pipe_operator_where()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> WHERE id = 1");
        dialects.VerifiedStmt("SELECT * FROM tbl |> WHERE id = 1 AND name = 'John'");
        dialects.VerifiedStmt("SELECT * FROM tbl |> WHERE id = 1 OR name = 'John'");
    }

    [Fact]
    public void parse_pipe_operator_aggregate()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> AGGREGATE COUNT(*)");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> AGGREGATE COUNT(*) total_users",
            "SELECT * FROM tbl |> AGGREGATE COUNT(*) AS total_users");
        dialects.VerifiedStmt("SELECT * FROM tbl |> AGGREGATE COUNT(*) AS total_users");
        dialects.VerifiedStmt("SELECT * FROM tbl |> AGGREGATE COUNT(*), MIN(id)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> AGGREGATE SUM(o_totalprice) AS price, COUNT(*) AS cnt GROUP BY EXTRACT(YEAR FROM o_orderdate) AS year");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> AGGREGATE GROUP BY EXTRACT(YEAR FROM o_orderdate) AS year");
        dialects
            .VerifiedStmt("SELECT * FROM tbl |> AGGREGATE GROUP BY EXTRACT(YEAR FROM o_orderdate)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> AGGREGATE GROUP BY a, b");
        dialects.VerifiedStmt("SELECT * FROM tbl |> AGGREGATE SUM(c) GROUP BY a, b");
        dialects.VerifiedStmt("SELECT * FROM tbl |> AGGREGATE SUM(c) ASC");
    }

    [Fact]
    public void parse_pipe_operator_order_by()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> ORDER BY id ASC");
        dialects.VerifiedStmt("SELECT * FROM tbl |> ORDER BY id DESC");
        dialects.VerifiedStmt("SELECT * FROM tbl |> ORDER BY id DESC, name ASC");
    }

    [Fact]
    public void parse_pipe_operator_tablesample()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> TABLESAMPLE BERNOULLI (50)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> TABLESAMPLE SYSTEM (50 PERCENT)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> TABLESAMPLE SYSTEM (50) REPEATABLE (10)");
    }

    [Fact]
    public void parse_pipe_operator_rename()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> RENAME old_name AS new_name");
        dialects.VerifiedStmt("SELECT * FROM tbl |> RENAME id AS user_id, name AS user_name");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> RENAME id user_id",
            "SELECT * FROM tbl |> RENAME id AS user_id");
    }

    [Fact]
    public void parse_pipe_operator_union()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNION ALL (SELECT * FROM admins)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNION DISTINCT (SELECT * FROM admins)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNION (SELECT * FROM admins)");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> UNION ALL (SELECT * FROM admins), (SELECT * FROM guests)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNION DISTINCT (SELECT * FROM admins), (SELECT * FROM guests), (SELECT * FROM employees)");
        dialects
            .VerifiedStmt("SELECT * FROM tbl |> UNION (SELECT * FROM admins), (SELECT * FROM guests)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNION BY NAME (SELECT * FROM admins)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNION ALL BY NAME (SELECT * FROM admins)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNION DISTINCT BY NAME (SELECT * FROM admins)");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> UNION BY NAME (SELECT * FROM admins), (SELECT * FROM guests)");
    }

    [Fact]
    public void parse_pipe_operator_intersect()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> INTERSECT DISTINCT (SELECT * FROM admins)");
        dialects
            .VerifiedStmt("SELECT * FROM tbl |> INTERSECT DISTINCT BY NAME (SELECT * FROM admins)");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> INTERSECT DISTINCT (SELECT * FROM admins), (SELECT * FROM guests)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> INTERSECT DISTINCT BY NAME (SELECT * FROM admins), (SELECT * FROM guests)");
    }

    [Fact]
    public void parse_pipe_operator_except()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> EXCEPT DISTINCT (SELECT * FROM admins)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> EXCEPT DISTINCT BY NAME (SELECT * FROM admins)");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> EXCEPT DISTINCT (SELECT * FROM admins), (SELECT * FROM guests)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> EXCEPT DISTINCT BY NAME (SELECT * FROM admins), (SELECT * FROM guests)");
    }

    [Fact]
    public void parse_pipe_operator_call()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> CALL my_function()");
        dialects.VerifiedStmt("SELECT * FROM tbl |> CALL process_data(5, 'test')");
        dialects
            .VerifiedStmt("SELECT * FROM tbl |> CALL namespace.function_name(col1, col2, 'literal')");
        dialects.VerifiedStmt("SELECT * FROM tbl |> CALL transform_data(col1 + col2)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> CALL analyze_data('param1', 100, true)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> CALL tvf1(arg1) AS al");
        dialects.VerifiedStmt("SELECT * FROM tbl |> CALL process_data(5) AS result_table");
        dialects.VerifiedStmt("SELECT * FROM tbl |> CALL namespace.func() AS my_alias");
        dialects.VerifiedStmt("SELECT * FROM tbl |> CALL tvf1(arg1) |> CALL tvf2(arg2, arg3)");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> CALL transform(col1) |> CALL validate() |> CALL process(param)");
        dialects
            .VerifiedStmt("SELECT * FROM tbl |> CALL tvf1(arg1) AS step1 |> CALL tvf2(arg2) AS step2");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> CALL preprocess() AS clean_data |> CALL analyze(mode) AS results");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> CALL transform() |> WHERE status = 'active' |> CALL process(param)");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> CALL preprocess() AS clean |> SELECT col1, col2 |> CALL validate()");
    }

    [Fact]
    public void parse_pipe_operator_pivot()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> PIVOT(SUM(amount) FOR quarter IN ('Q1', 'Q2', 'Q3', 'Q4'))");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> PIVOT(AVG(revenue) FOR region IN ('North', 'South', 'East', 'West'))");
        dialects.VerifiedStmt("SELECT * FROM tbl |> PIVOT(SUM(sales) AS total_sales, COUNT(*) AS num_transactions FOR month IN ('Jan', 'Feb', 'Mar'))");
        dialects.VerifiedStmt("SELECT * FROM tbl |> PIVOT(SUM(amount) FOR product.category IN ('Electronics', 'Clothing'))");
        dialects.VerifiedStmt("SELECT * FROM tbl |> WHERE year = 2023 |> PIVOT(SUM(revenue) FOR quarter IN ('Q1', 'Q2', 'Q3', 'Q4'))");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> PIVOT(SUM(sales) FOR quarter IN ('Q1', 'Q2')) AS quarterly_sales");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> PIVOT(AVG(price) FOR category IN ('A', 'B', 'C')) AS avg_by_category");
        dialects.VerifiedStmt("SELECT * FROM tbl |> PIVOT(COUNT(*) AS transactions, SUM(amount) AS total FOR region IN ('North', 'South')) AS regional_summary");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> PIVOT(SUM(sales) FOR quarter IN ('Q1', 'Q2')) quarterly_sales",
            "SELECT * FROM tbl |> PIVOT(SUM(sales) FOR quarter IN ('Q1', 'Q2')) AS quarterly_sales");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> PIVOT(AVG(price) FOR category IN ('A', 'B', 'C')) avg_by_category",
            "SELECT * FROM tbl |> PIVOT(AVG(price) FOR category IN ('A', 'B', 'C')) AS avg_by_category");
    }

    [Fact]
    public void parse_pipe_operator_unpivot()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNPIVOT(revenue FOR quarter IN (Q1, Q2, Q3, Q4))");
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNPIVOT(value FOR category IN (A, B, C))");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> UNPIVOT(measurement FOR metric_type IN (cpu, memory, disk))");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> UNPIVOT(amount FOR period IN (jan, feb, mar, apr, may, jun))");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> UNPIVOT(score FOR subject IN (math, science, english, history))");
        dialects.VerifiedStmt("SELECT * FROM tbl |> WHERE year = 2023 |> UNPIVOT(revenue FOR quarter IN (Q1, Q2, Q3, Q4))");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> UNPIVOT(amount FOR period IN (Q1, Q2)) AS unpivoted_sales");
        dialects.VerifiedStmt(
            "SELECT * FROM tbl |> UNPIVOT(value FOR category IN (A, B, C)) AS transformed_data");
        dialects.VerifiedStmt("SELECT * FROM tbl |> UNPIVOT(measurement FOR metric_type IN (cpu, memory)) AS metric_measurements");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> UNPIVOT(amount FOR period IN (Q1, Q2)) unpivoted_sales",
            "SELECT * FROM tbl |> UNPIVOT(amount FOR period IN (Q1, Q2)) AS unpivoted_sales");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> UNPIVOT(value FOR category IN (A, B, C)) transformed_data",
            "SELECT * FROM tbl |> UNPIVOT(value FOR category IN (A, B, C)) AS transformed_data");
    }

    [Fact]
    public void parse_pipe_operator_join()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> JOIN orders ON users.id = orders.user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> INNER JOIN orders ON users.id = orders.user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> LEFT JOIN orders ON users.id = orders.user_id");
        dialects
            .VerifiedStmt("SELECT * FROM tbl |> LEFT OUTER JOIN orders ON users.id = orders.user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> RIGHT JOIN orders ON users.id = orders.user_id");
        dialects
            .VerifiedStmt("SELECT * FROM tbl |> RIGHT OUTER JOIN orders ON users.id = orders.user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> FULL JOIN orders ON users.id = orders.user_id");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> FULL OUTER JOIN orders ON users.id = orders.user_id",
            "SELECT * FROM tbl |> FULL JOIN orders ON users.id = orders.user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> CROSS JOIN orders");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> JOIN orders USING (user_id)",
            "SELECT * FROM tbl |> JOIN orders USING(user_id)");
        dialects.VerifiedQueryWithCanonical(
            "SELECT * FROM tbl |> LEFT JOIN orders USING (user_id, order_date)",
            "SELECT * FROM tbl |> LEFT JOIN orders USING(user_id, order_date)");
        dialects.VerifiedStmt("SELECT * FROM tbl |> JOIN orders o ON users.id = o.user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> LEFT JOIN orders AS o ON users.id = o.user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> JOIN orders ON users.id = orders.user_id AND orders.status = 'active'");
        dialects.VerifiedStmt("SELECT * FROM tbl |> LEFT JOIN orders ON users.id = orders.user_id AND orders.amount > 100");
        dialects.VerifiedStmt("SELECT * FROM tbl |> JOIN orders ON users.id = orders.user_id |> JOIN products ON orders.product_id = products.id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> LEFT JOIN orders ON users.id = orders.user_id |> RIGHT JOIN products ON orders.product_id = products.id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> JOIN orders ON users.id = orders.user_id |> WHERE orders.amount > 100");
        dialects.VerifiedStmt("SELECT * FROM tbl |> WHERE users.active = true |> LEFT JOIN orders ON users.id = orders.user_id");
        dialects.VerifiedStmt("SELECT * FROM tbl |> JOIN orders ON users.id = orders.user_id |> SELECT users.name, orders.amount");
    }

    [Fact]
    public void parse_pipe_operator_chained()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);
        dialects.VerifiedStmt("SELECT * FROM tbl |> AGGREGATE SUM(cost) AS total_cost GROUP BY customer_id, state, item_type |> EXTEND COUNT(*) OVER (PARTITION BY customer_id) AS num_orders |> WHERE num_orders > 1 |> AGGREGATE AVG(total_cost) AS average GROUP BY state DESC, item_type ASC");
    }

    [Fact]
    public void parse_pipeline_operator_negative_tests()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPipeOperator);

        // Test that plain EXCEPT without DISTINCT fails
        Assert.Equal(
            new ParserError.ParserErrorValue("EXCEPT pipe operator requires DISTINCT modifier"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("SELECT * FROM users |> EXCEPT (SELECT * FROM admins)")).Error);

        // Test that EXCEPT ALL fails
        Assert.Equal(
            new ParserError.ParserErrorValue("EXCEPT pipe operator requires DISTINCT modifier"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("SELECT * FROM users |> EXCEPT ALL (SELECT * FROM admins)")).Error);

        // Test that EXCEPT BY NAME without DISTINCT fails
        Assert.Equal(
            new ParserError.ParserErrorValue("EXCEPT pipe operator requires DISTINCT modifier"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("SELECT * FROM users |> EXCEPT BY NAME (SELECT * FROM admins)")).Error);

        // Test that EXCEPT ALL BY NAME fails
        Assert.Equal(
            new ParserError.ParserErrorValue("EXCEPT pipe operator requires DISTINCT modifier"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements(
                    "SELECT * FROM users |> EXCEPT ALL BY NAME (SELECT * FROM admins)")).Error);

        // Test that plain INTERSECT without DISTINCT fails
        Assert.Equal(
            new ParserError.ParserErrorValue("INTERSECT pipe operator requires DISTINCT modifier"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("SELECT * FROM users |> INTERSECT (SELECT * FROM admins)")).Error);

        // Test that INTERSECT ALL fails
        Assert.Equal(
            new ParserError.ParserErrorValue("INTERSECT pipe operator requires DISTINCT modifier"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("SELECT * FROM users |> INTERSECT ALL (SELECT * FROM admins)")).Error);

        // Test that INTERSECT BY NAME without DISTINCT fails
        Assert.Equal(
            new ParserError.ParserErrorValue("INTERSECT pipe operator requires DISTINCT modifier"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("SELECT * FROM users |> INTERSECT BY NAME (SELECT * FROM admins)")).Error);

        // Test that INTERSECT ALL BY NAME fails
        Assert.Equal(
            new ParserError.ParserErrorValue("INTERSECT pipe operator requires DISTINCT modifier"),
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements(
                    "SELECT * FROM users |> INTERSECT ALL BY NAME (SELECT * FROM admins)")).Error);

        // Test that CALL without function name fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> CALL"));

        // Test that CALL without parentheses fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> CALL my_function"));

        // Test that CALL with invalid function syntax fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> CALL 123 invalid"));

        // Test that CALL with malformed arguments fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> CALL my_function(,)"));

        // Test that CALL with invalid alias syntax fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> CALL my_function() AS"));

        // Test that PIVOT without parentheses fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> PIVOT SUM(amount) FOR month IN ('Jan')"));

        // Test that PIVOT without FOR keyword fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> PIVOT(SUM(amount) month IN ('Jan'))"));

        // Test that PIVOT without IN keyword fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> PIVOT(SUM(amount) FOR month ('Jan'))"));

        // Test that PIVOT with empty IN list fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> PIVOT(SUM(amount) FOR month IN ())"));

        // Test that PIVOT with invalid alias syntax fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> PIVOT(SUM(amount) FOR month IN ('Jan')) AS"));

        // Test UNPIVOT negative cases

        // Test that UNPIVOT without parentheses fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> UNPIVOT value FOR name IN col1, col2"));

        // Test that UNPIVOT without FOR keyword fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> UNPIVOT(value name IN (col1, col2))"));

        // Test that UNPIVOT without IN keyword fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> UNPIVOT(value FOR name (col1, col2))"));

        // Test that UNPIVOT with missing value column fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> UNPIVOT(FOR name IN (col1, col2))"));

        // Test that UNPIVOT with missing name column fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> UNPIVOT(value FOR IN (col1, col2))"));

        // Test that UNPIVOT with empty IN list fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> UNPIVOT(value FOR name IN ())"));

        // Test that UNPIVOT with invalid alias syntax fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> UNPIVOT(value FOR name IN (col1, col2)) AS"));

        // Test that UNPIVOT with missing closing parenthesis fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> UNPIVOT(value FOR name IN (col1, col2)"));

        // Test that JOIN without table name fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> JOIN ON users.id = orders.user_id"));

        // Test that CROSS JOIN with ON condition fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements(
                "SELECT * FROM users |> CROSS JOIN orders ON users.id = orders.user_id"));

        // Test that CROSS JOIN with USING condition fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> CROSS JOIN orders USING (user_id)"));

        // Test that JOIN with empty USING list fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> JOIN orders USING ()"));

        // Test that JOIN with malformed ON condition fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> JOIN orders ON"));

        // Test that JOIN with invalid USING syntax fails
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT * FROM users |> JOIN orders USING user_id"));
    }

    [Fact]
    public void parse_multiple_set_statements()
    {
        // Rust: `fn ..() -> Result<(), ParserError>`, terminando em `Ok(())`; aqui um erro é exceção.
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsCommaSeparatedSetAssignments);
        var stmt = dialects.VerifiedStmt("SET @a = 1, b = 2");

        switch (stmt)
        {
            case Statement.Set { Value: Set.MultipleAssignments { Assignments: var assignments } }:
                Assert.Equal(
                    assignments,
                    Sequence.Of(
                        new SetAssignment(
                            Scope: null,
                            Name: ObjectName.From(Sequence.Of(new Ident("@a"))),
                            Value: Expr.MakeValue(TestUtils.Number("1"))),
                        new SetAssignment(
                            Scope: null,
                            Name: ObjectName.From(Sequence.Of(new Ident("b"))),
                            Value: Expr.MakeValue(TestUtils.Number("2")))));
                break;
            default:
                Assert.Fail("Expected SetVariable with 2 variables and 2 values");
                break;
        }

        stmt = dialects.VerifiedStmt("SET GLOBAL @a = 1, SESSION b = 2, LOCAL c = 3, d = 4");

        switch (stmt)
        {
            case Statement.Set { Value: Set.MultipleAssignments { Assignments: var assignments } }:
                Assert.Equal(
                    assignments,
                    Sequence.Of(
                        new SetAssignment(
                            Scope: new ContextModifier.Global(),
                            Name: ObjectName.From(Sequence.Of(new Ident("@a"))),
                            Value: Expr.MakeValue(TestUtils.Number("1"))),
                        new SetAssignment(
                            Scope: new ContextModifier.Session(),
                            Name: ObjectName.From(Sequence.Of(new Ident("b"))),
                            Value: Expr.MakeValue(TestUtils.Number("2"))),
                        new SetAssignment(
                            Scope: new ContextModifier.Local(),
                            Name: ObjectName.From(Sequence.Of(new Ident("c"))),
                            Value: Expr.MakeValue(TestUtils.Number("3"))),
                        new SetAssignment(
                            Scope: null,
                            Name: ObjectName.From(Sequence.Of(new Ident("d"))),
                            Value: Expr.MakeValue(TestUtils.Number("4")))));
                break;
            default:
                Assert.Fail("Expected MultipleAssignments with 4 scoped variables and 4 values");
                break;
        }
    }

    [Fact]
    public void parse_set_time_zone_alias()
    {
        switch (TestUtils.AllDialects().VerifiedStmt("SET TIME ZONE 'UTC'"))
        {
            case Statement.Set { Value: Set.SetTimeZone { Local: var local, Value: var value } }:
                Assert.False(local);
                Assert.Equal(
                    value,
                    new Expr.Value(new Value.SingleQuotedString("UTC").WithEmptySpan()));
                break;
            default:
                throw Unreachable();
        }
    }

    [Fact]
    public void parse_return()
    {
        var stmt = TestUtils.AllDialects().VerifiedStmt("RETURN");
        Assert.Equal(stmt, new Statement.Return(new ReturnStatement(Value: null)));

        _ = TestUtils.AllDialects().VerifiedStmt("RETURN 1");
    }

    [Fact]
    public void parse_subquery_limit()
    {
        _ = TestUtils.AllDialects().VerifiedStmt("SELECT t1_id, t1_name FROM t1 WHERE t1_id IN (SELECT t2_id FROM t2 WHERE t1_name = t2_name LIMIT 10)");
    }

    [Fact]
    public void test_open()
    {
        var openCursor = "OPEN Employee_Cursor";
        var stmt = TestUtils.AllDialects().VerifiedStmt(openCursor);
        Assert.Equal(
            stmt,
            new Statement.Open(new OpenStatement(
                CursorName: new Ident("Employee_Cursor"))));
    }

    [Fact]
    public void parse_truncate_only()
    {
        var truncate = TestUtils.AllDialects().VerifiedStmt("TRUNCATE TABLE employee, ONLY dept");

        var tableNames = Sequence.Of(
            new TruncateTableTarget(
                Name: ObjectName.From(Sequence.Of(new Ident("employee"))),
                Only: false,
                HasAsterisk: false),
            new TruncateTableTarget(
                Name: ObjectName.From(Sequence.Of(new Ident("dept"))),
                Only: true,
                HasAsterisk: false));

        Assert.Equal(
            new Statement.Truncate(new Truncate(
                TableNames: tableNames,
                Partitions: null,
                Table: true,
                IfExists: false,
                Identity: null,
                Cascade: null,
                OnCluster: null)),
            truncate);
    }

    [Fact]
    public void check_enforced()
    {
        TestUtils.AllDialects().VerifiedStmt(
            "CREATE TABLE t (a INT, b INT, c INT, CHECK (a > 0) NOT ENFORCED, CHECK (b > 0) ENFORCED, CHECK (c > 0))");
    }

    [Fact]
    public void column_check_enforced()
    {
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (x INT CHECK (x > 1) NOT ENFORCED)");
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (x INT CHECK (x > 1) ENFORCED)");
        TestUtils.AllDialects().VerifiedStmt(
            "CREATE TABLE t (a INT CHECK (a > 0) NOT ENFORCED, b INT CHECK (b > 0) ENFORCED, c INT CHECK (c > 0))");
    }

    [Fact]
    public void table_check_no_inherit()
    {
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (a INT, CONSTRAINT c CHECK (a > 0) NO INHERIT)");
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (a INT, CHECK (a > 0) NO INHERIT)");
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (a INT, CHECK (a > 0) NO INHERIT NOT ENFORCED)");
    }

    [Fact]
    public void column_check_no_inherit()
    {
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (x INT CHECK (x > 1) NO INHERIT)");
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (x INT CHECK (x > 1) NO INHERIT NOT ENFORCED)");
    }

    [Fact]
    public void join_precedence()
    {
        TestUtils.AllDialects().VerifiedQueryWithCanonical(
            "SELECT *\n" +
            "         FROM t1\n" +
            "         NATURAL JOIN t5\n" +
            "         INNER JOIN t0 ON (t0.v1 + t5.v0) > 0\n" +
            "         WHERE t0.v1 = t1.v0",
            // canonical string without parentheses
            "SELECT * FROM t1 NATURAL JOIN t5 INNER JOIN t0 ON (t0.v1 + t5.v0) > 0 WHERE t0.v1 = t1.v0");
    }

    [Fact]
    public void test_nested_join_without_parentheses()
    {
        var query = "SELECT DISTINCT p.product_id FROM orders AS o INNER JOIN customers AS c INNER JOIN products AS p ON p.customer_id = c.customer_id ON c.order_id = o.order_id";
        Assert.Equal(
            TestUtils.Only(
                NonLeftAssociativeDialects()
                    .VerifiedOnlySelectWithCanonical(query, "SELECT DISTINCT p.product_id FROM orders AS o INNER JOIN (customers AS c INNER JOIN products AS p ON p.customer_id = c.customer_id) ON c.order_id = o.order_id")
                    .From)
            .Joins,
            Sequence.Of(new Join(
                Relation: new TableFactor.NestedJoin(
                    TableWithJoins: new TableWithJoins(
                        Relation: new TableFactor.Table(
                            Name: ObjectName.From(Sequence.Of(new Ident("customers"))),
                            Alias: TestUtils.TableAlias(true, "c"),
                            Args: null,
                            WithHints: Sequence<Expr>.Empty,
                            Version: null,
                            Partitions: Sequence<Ident>.Empty,
                            WithOrdinality: false,
                            JsonPath: null,
                            Sample: null,
                            IndexHints: Sequence<TableIndexHints>.Empty),
                        Joins: Sequence.Of(new Join(
                            Relation: new TableFactor.Table(
                                Name: ObjectName.From(Sequence.Of(new Ident("products"))),
                                Alias: TestUtils.TableAlias(true, "p"),
                                Args: null,
                                WithHints: Sequence<Expr>.Empty,
                                Version: null,
                                Partitions: Sequence<Ident>.Empty,
                                WithOrdinality: false,
                                JsonPath: null,
                                Sample: null,
                                IndexHints: Sequence<TableIndexHints>.Empty),
                            Global: false,
                            JoinOperator: new JoinOperator.Inner(new JoinConstraint.On(new Expr.BinaryOp(
                                Left: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("p"),
                                    new Ident("customer_id"))),
                                Op: new BinaryOperator.Eq(),
                                Right: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("c"),
                                    new Ident("customer_id"))))))))),
                    Alias: null),
                Global: false,
                JoinOperator: new JoinOperator.Inner(new JoinConstraint.On(new Expr.BinaryOp(
                    Left: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("c"),
                        new Ident("order_id"))),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("o"),
                        new Ident("order_id")))))))));

        query = "SELECT DISTINCT p.product_id FROM orders AS o JOIN customers AS c JOIN products AS p ON p.customer_id = c.customer_id ON c.order_id = o.order_id";
        Assert.Equal(
            TestUtils.Only(
                NonLeftAssociativeDialects()
                    .VerifiedOnlySelectWithCanonical(query, "SELECT DISTINCT p.product_id FROM orders AS o JOIN (customers AS c JOIN products AS p ON p.customer_id = c.customer_id) ON c.order_id = o.order_id")
                    .From)
            .Joins,
            Sequence.Of(new Join(
                Relation: new TableFactor.NestedJoin(
                    TableWithJoins: new TableWithJoins(
                        Relation: new TableFactor.Table(
                            Name: ObjectName.From(Sequence.Of(new Ident("customers"))),
                            Alias: TestUtils.TableAlias(true, "c"),
                            Args: null,
                            WithHints: Sequence<Expr>.Empty,
                            Version: null,
                            Partitions: Sequence<Ident>.Empty,
                            WithOrdinality: false,
                            JsonPath: null,
                            Sample: null,
                            IndexHints: Sequence<TableIndexHints>.Empty),
                        Joins: Sequence.Of(new Join(
                            Relation: new TableFactor.Table(
                                Name: ObjectName.From(Sequence.Of(new Ident("products"))),
                                Alias: TestUtils.TableAlias(true, "p"),
                                Args: null,
                                WithHints: Sequence<Expr>.Empty,
                                Version: null,
                                Partitions: Sequence<Ident>.Empty,
                                WithOrdinality: false,
                                JsonPath: null,
                                Sample: null,
                                IndexHints: Sequence<TableIndexHints>.Empty),
                            Global: false,
                            JoinOperator: new JoinOperator.Join(new JoinConstraint.On(new Expr.BinaryOp(
                                Left: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("p"),
                                    new Ident("customer_id"))),
                                Op: new BinaryOperator.Eq(),
                                Right: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("c"),
                                    new Ident("customer_id"))))))))),
                    Alias: null),
                Global: false,
                JoinOperator: new JoinOperator.Join(new JoinConstraint.On(new Expr.BinaryOp(
                    Left: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("c"),
                        new Ident("order_id"))),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("o"),
                        new Ident("order_id")))))))));

        query = "SELECT DISTINCT p.product_id FROM orders AS o LEFT JOIN customers AS c LEFT JOIN products AS p ON p.customer_id = c.customer_id ON c.order_id = o.order_id";
        Assert.Equal(
            TestUtils.Only(
                NonLeftAssociativeDialects()
                    .VerifiedOnlySelectWithCanonical(query, "SELECT DISTINCT p.product_id FROM orders AS o LEFT JOIN (customers AS c LEFT JOIN products AS p ON p.customer_id = c.customer_id) ON c.order_id = o.order_id")
                    .From)
            .Joins,
            Sequence.Of(new Join(
                Relation: new TableFactor.NestedJoin(
                    TableWithJoins: new TableWithJoins(
                        Relation: new TableFactor.Table(
                            Name: ObjectName.From(Sequence.Of(new Ident("customers"))),
                            Alias: TestUtils.TableAlias(true, "c"),
                            Args: null,
                            WithHints: Sequence<Expr>.Empty,
                            Version: null,
                            Partitions: Sequence<Ident>.Empty,
                            WithOrdinality: false,
                            JsonPath: null,
                            Sample: null,
                            IndexHints: Sequence<TableIndexHints>.Empty),
                        Joins: Sequence.Of(new Join(
                            Relation: new TableFactor.Table(
                                Name: ObjectName.From(Sequence.Of(new Ident("products"))),
                                Alias: TestUtils.TableAlias(true, "p"),
                                Args: null,
                                WithHints: Sequence<Expr>.Empty,
                                Version: null,
                                Partitions: Sequence<Ident>.Empty,
                                WithOrdinality: false,
                                JsonPath: null,
                                Sample: null,
                                IndexHints: Sequence<TableIndexHints>.Empty),
                            Global: false,
                            JoinOperator: new JoinOperator.Left(new JoinConstraint.On(new Expr.BinaryOp(
                                Left: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("p"),
                                    new Ident("customer_id"))),
                                Op: new BinaryOperator.Eq(),
                                Right: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("c"),
                                    new Ident("customer_id"))))))))),
                    Alias: null),
                Global: false,
                JoinOperator: new JoinOperator.Left(new JoinConstraint.On(new Expr.BinaryOp(
                    Left: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("c"),
                        new Ident("order_id"))),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("o"),
                        new Ident("order_id")))))))));

        query = "SELECT DISTINCT p.product_id FROM orders AS o RIGHT JOIN customers AS c RIGHT JOIN products AS p ON p.customer_id = c.customer_id ON c.order_id = o.order_id";
        Assert.Equal(
            TestUtils.Only(
                NonLeftAssociativeDialects()
                    .VerifiedOnlySelectWithCanonical(query, "SELECT DISTINCT p.product_id FROM orders AS o RIGHT JOIN (customers AS c RIGHT JOIN products AS p ON p.customer_id = c.customer_id) ON c.order_id = o.order_id")
                    .From)
            .Joins,
            Sequence.Of(new Join(
                Relation: new TableFactor.NestedJoin(
                    TableWithJoins: new TableWithJoins(
                        Relation: new TableFactor.Table(
                            Name: ObjectName.From(Sequence.Of(new Ident("customers"))),
                            Alias: TestUtils.TableAlias(true, "c"),
                            Args: null,
                            WithHints: Sequence<Expr>.Empty,
                            Version: null,
                            Partitions: Sequence<Ident>.Empty,
                            WithOrdinality: false,
                            JsonPath: null,
                            Sample: null,
                            IndexHints: Sequence<TableIndexHints>.Empty),
                        Joins: Sequence.Of(new Join(
                            Relation: new TableFactor.Table(
                                Name: ObjectName.From(Sequence.Of(new Ident("products"))),
                                Alias: TestUtils.TableAlias(true, "p"),
                                Args: null,
                                WithHints: Sequence<Expr>.Empty,
                                Version: null,
                                Partitions: Sequence<Ident>.Empty,
                                WithOrdinality: false,
                                JsonPath: null,
                                Sample: null,
                                IndexHints: Sequence<TableIndexHints>.Empty),
                            Global: false,
                            JoinOperator: new JoinOperator.Right(new JoinConstraint.On(new Expr.BinaryOp(
                                Left: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("p"),
                                    new Ident("customer_id"))),
                                Op: new BinaryOperator.Eq(),
                                Right: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("c"),
                                    new Ident("customer_id"))))))))),
                    Alias: null),
                Global: false,
                JoinOperator: new JoinOperator.Right(new JoinConstraint.On(new Expr.BinaryOp(
                    Left: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("c"),
                        new Ident("order_id"))),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("o"),
                        new Ident("order_id")))))))));

        query = "SELECT DISTINCT p.product_id FROM orders AS o FULL JOIN customers AS c FULL JOIN products AS p ON p.customer_id = c.customer_id ON c.order_id = o.order_id";
        Assert.Equal(
            TestUtils.Only(
                NonLeftAssociativeDialects()
                    .VerifiedOnlySelectWithCanonical(query, "SELECT DISTINCT p.product_id FROM orders AS o FULL JOIN (customers AS c FULL JOIN products AS p ON p.customer_id = c.customer_id) ON c.order_id = o.order_id")
                    .From)
            .Joins,
            Sequence.Of(new Join(
                Relation: new TableFactor.NestedJoin(
                    TableWithJoins: new TableWithJoins(
                        Relation: new TableFactor.Table(
                            Name: ObjectName.From(Sequence.Of(new Ident("customers"))),
                            Alias: TestUtils.TableAlias(true, "c"),
                            Args: null,
                            WithHints: Sequence<Expr>.Empty,
                            Version: null,
                            Partitions: Sequence<Ident>.Empty,
                            WithOrdinality: false,
                            JsonPath: null,
                            Sample: null,
                            IndexHints: Sequence<TableIndexHints>.Empty),
                        Joins: Sequence.Of(new Join(
                            Relation: new TableFactor.Table(
                                Name: ObjectName.From(Sequence.Of(new Ident("products"))),
                                Alias: TestUtils.TableAlias(true, "p"),
                                Args: null,
                                WithHints: Sequence<Expr>.Empty,
                                Version: null,
                                Partitions: Sequence<Ident>.Empty,
                                WithOrdinality: false,
                                JsonPath: null,
                                Sample: null,
                                IndexHints: Sequence<TableIndexHints>.Empty),
                            Global: false,
                            JoinOperator: new JoinOperator.FullOuter(new JoinConstraint.On(
                                new Expr.BinaryOp(
                                    Left: new Expr.CompoundIdentifier(Sequence.Of(
                                        new Ident("p"),
                                        new Ident("customer_id"))),
                                    Op: new BinaryOperator.Eq(),
                                    Right: new Expr.CompoundIdentifier(Sequence.Of(
                                        new Ident("c"),
                                        new Ident("customer_id"))))))))),
                    Alias: null),
                Global: false,
                JoinOperator: new JoinOperator.FullOuter(new JoinConstraint.On(new Expr.BinaryOp(
                    Left: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("c"),
                        new Ident("order_id"))),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("o"),
                        new Ident("order_id")))))))));
    }

    [Fact]
    public void parse_create_procedure_with_language()
    {
        var sql = "CREATE PROCEDURE test_proc LANGUAGE sql AS BEGIN SELECT 1; END";
        switch (VerifiedStmt(sql))
        {
            case Statement.CreateProcedure
            {
                OrAlter: var orAlter,
                Name: var name,
                Params: var @params,
                Language: var language,
            }:
                Assert.False(orAlter); // assert_eq!(or_alter, false)
                Assert.Equal("test_proc", name.ToString());
                Assert.Equal<Sequence<ProcedureParam>?>(@params, Sequence<ProcedureParam>.Empty);
                Assert.Equal(
                    language,
                    new Ident(
                        "sql",
                        null,
                        new Span(Location.Empty(), Location.Empty())));
                break;
            default:
                throw Unreachable();
        }
    }

    [Fact]
    public void parse_create_procedure_with_parameter_modes()
    {
        var sql = "CREATE PROCEDURE test_proc (IN a INTEGER, OUT b TEXT, INOUT c TIMESTAMP, d BOOL) AS BEGIN SELECT 1; END";
        switch (VerifiedStmt(sql))
        {
            case Statement.CreateProcedure
            {
                OrAlter: var orAlter,
                Name: var name,
                Params: var @params,
            }:
                Assert.False(orAlter); // assert_eq!(or_alter, false)
                Assert.Equal("test_proc", name.ToString());
                var fakeSpan = new Span(
                    new Location(0, 0),
                    new Location(0, 0));
                Assert.Equal<Sequence<ProcedureParam>?>(
                    @params,
                    Sequence.Of(
                        new ProcedureParam(
                            Name: new Ident("a", null, fakeSpan),
                            DataType: new DataType.Integer(null),
                            Mode: new ArgMode.In(),
                            Default: null),
                        new ProcedureParam(
                            Name: new Ident("b", null, fakeSpan),
                            DataType: new DataType.Text(),
                            Mode: new ArgMode.Out(),
                            Default: null),
                        new ProcedureParam(
                            Name: new Ident("c", null, fakeSpan),
                            DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                            Mode: new ArgMode.InOut(),
                            Default: null),
                        new ProcedureParam(
                            Name: new Ident("d", null, fakeSpan),
                            DataType: new DataType.Bool(),
                            Mode: null,
                            Default: null)));
                break;
            default:
                throw Unreachable();
        }

        // parameters with default values
        sql = "CREATE PROCEDURE test_proc (IN a INTEGER = 1, OUT b TEXT = '2', INOUT c TIMESTAMP = NULL, d BOOL = 0) AS BEGIN SELECT 1; END";
        switch (VerifiedStmt(sql))
        {
            case Statement.CreateProcedure
            {
                OrAlter: var orAlter,
                Name: var name,
                Params: var @params,
            }:
                Assert.False(orAlter); // assert_eq!(or_alter, false)
                Assert.Equal("test_proc", name.ToString());
                Assert.Equal<Sequence<ProcedureParam>?>(
                    @params,
                    Sequence.Of(
                        new ProcedureParam(
                            Name: new Ident("a"),
                            DataType: new DataType.Integer(null),
                            Mode: new ArgMode.In(),
                            Default: new Expr.Value(TestUtils.Number("1").WithEmptySpan())),
                        new ProcedureParam(
                            Name: new Ident("b"),
                            DataType: new DataType.Text(),
                            Mode: new ArgMode.Out(),
                            Default: new Expr.Value(
                                new Value.SingleQuotedString("2").WithEmptySpan())),
                        new ProcedureParam(
                            Name: new Ident("c"),
                            DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                            Mode: new ArgMode.InOut(),
                            Default: new Expr.Value(new Value.Null().WithEmptySpan())),
                        new ProcedureParam(
                            Name: new Ident("d"),
                            DataType: new DataType.Bool(),
                            Mode: null,
                            Default: new Expr.Value(TestUtils.Number("0").WithEmptySpan()))));
                break;
            default:
                throw Unreachable();
        }
    }

    [Fact]
    public void parse_not_null()
    {
        _ = TestUtils.AllDialects().ExprParsesTo("x NOT NULL", "x IS NOT NULL");
        _ = TestUtils.AllDialects().ExprParsesTo("NULL NOT NULL", "NULL IS NOT NULL");

        Assert.True(
            TestUtils.AllDialects().ExprParsesTo("NOT NULL NOT NULL", "NOT NULL IS NOT NULL")
                is Expr.UnaryOp { Op: UnaryOperator.Not });
        Assert.True(
            TestUtils.AllDialects().ExprParsesTo("NOT x NOT NULL", "NOT x IS NOT NULL")
                is Expr.UnaryOp { Op: UnaryOperator.Not });
    }

    [Fact]
    public void test_select_exclude()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsSelectWildcardExclude);
        switch (dialects
            .VerifiedOnlySelect("SELECT * EXCLUDE c1 FROM test")
            .Projection[0])
        {
            case SelectItem.Wildcard { Value.OptExclude: var optExclude }:
                Assert.Equal(
                    optExclude,
                    new ExcludeSelectItem.Single(ObjectName.From(new Ident(
                        "c1"))));
                break;
            default:
                throw Unreachable();
        }
        switch (dialects
            .VerifiedOnlySelect("SELECT * EXCLUDE (c1, c2) FROM test")
            .Projection[0])
        {
            case SelectItem.Wildcard { Value.OptExclude: var optExclude }:
                Assert.Equal(
                    optExclude,
                    new ExcludeSelectItem.Multiple(Sequence.Of(
                        ObjectName.From(new Ident("c1")),
                        ObjectName.From(new Ident("c2")))));
                break;
            default:
                throw Unreachable();
        }
        var select = dialects.VerifiedOnlySelect("SELECT * EXCLUDE c1, c2 FROM test");
        switch (select.Projection[0])
        {
            case SelectItem.Wildcard { Value.OptExclude: var optExclude }:
                Assert.Equal(
                    optExclude,
                    new ExcludeSelectItem.Single(ObjectName.From(new Ident(
                        "c1"))));
                break;
            default:
                throw Unreachable();
        }
        switch (select.Projection[1])
        {
            case SelectItem.UnnamedExpr { Value: Expr.Identifier { Value: var ident } }:
                Assert.Equal(ident, new Ident("c2"));
                break;
            default:
                throw Unreachable();
        }

        dialects = TestUtils.AllDialectsWhere(d => d.SupportsSelectExclude);
        select = dialects.VerifiedOnlySelect("SELECT *, c1 EXCLUDE c1 FROM test");
        switch (select.Projection[0])
        {
            case SelectItem.Wildcard { Value: var additionalOptions }:
                Assert.Equal(additionalOptions, WildcardAdditionalOptions.Default());
                break;
            default:
                throw Unreachable();
        }
        Assert.Equal(
            select.Exclude,
            new ExcludeSelectItem.Single(ObjectName.From(new Ident(
                "c1"))));

        dialects = TestUtils.AllDialectsWhere(d =>
            d.SupportsSelectWildcardExclude && !d.SupportsSelectExclude);
        select = dialects.VerifiedOnlySelect("SELECT * EXCLUDE c1 FROM test");
        switch (select.Projection[0])
        {
            case SelectItem.Wildcard { Value.OptExclude: var optExclude }:
                Assert.Equal(
                    optExclude,
                    new ExcludeSelectItem.Single(ObjectName.From(new Ident(
                        "c1"))));
                break;
            default:
                throw Unreachable();
        }

        // Dialects that only support the wildcard form and do not accept EXCLUDE as an implicity alias
        // will fail when encountered with the `c2` ident
        dialects = TestUtils.AllDialectsWhere(d =>
            d.SupportsSelectWildcardExclude
                && !d.SupportsSelectExclude
                && d.IsColumnAlias(Keyword.EXCLUDE, new Parser(d)));
        Assert.Equal(
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("SELECT *, c1 EXCLUDE c2 FROM test")).Error,
            new ParserError.ParserErrorValue("Expected: end of statement, found: c2"));

        // Dialects that only support the wildcard form and accept EXCLUDE as an implicity alias
        // will fail when encountered with the `EXCLUDE` keyword
        dialects = TestUtils.AllDialectsWhere(d =>
            d.SupportsSelectWildcardExclude
                && !d.SupportsSelectExclude
                && !d.IsColumnAlias(Keyword.EXCLUDE, new Parser(d)));
        Assert.Equal(
            Assert.Throws<ParserException>(
                () => dialects.ParseSqlStatements("SELECT *, c1 EXCLUDE c2 FROM test")).Error,
            new ParserError.ParserErrorValue("Expected: end of statement, found: EXCLUDE"));
    }

    [Fact]
    public void test_select_exclude_qualified_names()
    {
        // EXCLUDE should accept qualified names like `f.col` parsed as ObjectName.
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsSelectWildcardExclude);

        // Qualified name in multi-column EXCLUDE list: f.* EXCLUDE (f.col1, f.col2)
        var select = dialects
            .VerifiedOnlySelect("SELECT f.* EXCLUDE (f.account_canonical_id, f.amount) FROM t AS f");
        switch (select.Projection[0])
        {
            case SelectItem.QualifiedWildcard { AdditionalOptions.OptExclude: var optExclude }:
                Assert.Equal(
                    optExclude,
                    new ExcludeSelectItem.Multiple(Sequence.Of(
                        ObjectName.From(Sequence.Of(new Ident("f"), new Ident("account_canonical_id"))),
                        ObjectName.From(Sequence.Of(new Ident("f"), new Ident("amount"))))));
                break;
            default:
                throw Unreachable();
        }

        // Plain identifiers must still parse successfully.
        dialects.VerifiedOnlySelect("SELECT f.* EXCLUDE (account_canonical_id) FROM t AS f");
        dialects.VerifiedOnlySelect("SELECT f.* EXCLUDE (col1, col2) FROM t AS f");
    }

    [Fact]
    public void test_no_semicolon_required_between_statements()
    {
        var sql = "\n" +
                  "SELECT * FROM tbl1\n" +
                  "SELECT * FROM tbl2\n" +
                  "    ";

        var dialects = TestUtils.AllDialectsWithOptions(new ParserOptions(
            TrailingCommas: false,
            Unescape: true,
            RequireSemicolonStmtDelimiter: false));
        var stmts = dialects.ParseSqlStatements(sql);
        Assert.Equal(2, stmts.Count);
        Assert.True(stmts.All(s => s is Statement.Query));
    }

    [Fact]
    public void test_identifier_unicode_support()
    {
        var sql = "SELECT phoneǤЖשचᎯ⻩☯♜🦄⚛🀄ᚠ⌛🌀 AS tbl FROM customers";
        var dialects = new TestedDialects(new List<Dialect>
        {
            new MySqlDialect(),
            new RedshiftSqlDialect(),
            new PostgreSqlDialect(),
        });
        _ = dialects.VerifiedStmt(sql);
    }

    [Fact]
    public void test_identifier_unicode_start()
    {
        var sql = "SELECT 💝phone AS 💝 FROM customers";
        var dialects = new TestedDialects(new List<Dialect>
        {
            new MySqlDialect(),
            new RedshiftSqlDialect(),
            new PostgreSqlDialect(),
        });
        _ = dialects.VerifiedStmt(sql);
    }

    [Fact]
    public void parse_notnull()
    {
        // Some dialects support `x NOTNULL` as an expression while others consider
        // `x NOTNULL` like `x AS NOTNULL` and thus consider `NOTNULL` an alias for x.
        var notnullUnsupportedDialects = TestUtils.AllDialectsExcept(d => d.SupportsNotnullOperator);
        _ = notnullUnsupportedDialects
            .VerifiedOnlySelectWithCanonical("SELECT NULL NOTNULL", "SELECT NULL AS NOTNULL");

        // Supported dialects consider `x NOTNULL` as an alias for `x IS NOT NULL`
        var notnullSupportedDialects = TestUtils.AllDialectsWhere(d => d.SupportsNotnullOperator);
        _ = notnullSupportedDialects.ExprParsesTo("x NOTNULL", "x IS NOT NULL");

        // For dialects which support it, `NOT NULL NOTNULL` should
        // parse as `(NOT (NULL IS NOT NULL))`
        Assert.True(
            notnullSupportedDialects.ExprParsesTo("NOT NULL NOTNULL", "NOT NULL IS NOT NULL")
                is Expr.UnaryOp { Op: UnaryOperator.Not });

        // for unsupported dialects, parsing should stop at `NOT NULL`
        notnullUnsupportedDialects.ExprParsesTo("NOT NULL NOTNULL", "NOT NULL");
    }

    [Fact]
    public void parse_odbc_time_date_timestamp()
    {
        // Supported statements
        var sqlD = "SELECT {d '2025-07-17'}, category_name FROM categories";
        _ = TestUtils.AllDialects().VerifiedStmt(sqlD);
        var sqlT = "SELECT {t '14:12:01'}, category_name FROM categories";
        _ = TestUtils.AllDialects().VerifiedStmt(sqlT);
        var sqlTs = "SELECT {ts '2025-07-17 14:12:01'}, category_name FROM categories";
        _ = TestUtils.AllDialects().VerifiedStmt(sqlTs);
        // Unsupported statement
        var supportsDictionary = TestUtils.AllDialectsWhere(d => d.SupportsDictionarySyntax);
        var dictionaryUnsupported = TestUtils.AllDialectsWhere(d => !d.SupportsDictionarySyntax);
        var sql = "SELECT {tt '14:12:01'} FROM foo";
        // `parse_sql_statements` devolve `Result`; aqui o erro é capturado logo na chamada.
        var res = Assert.Throws<ParserException>(() => supportsDictionary.ParseSqlStatements(sql));
        var resDict = Assert.Throws<ParserException>(() => dictionaryUnsupported.ParseSqlStatements(sql));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: :, found: '14:12:01'"),
            res.Error);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: an expression, found: {"),
            resDict.Error);
    }

    [Fact]
    public void parse_create_user()
    {
        var create = VerifiedStmt("CREATE USER u1");
        switch (create)
        {
            case Statement.CreateUser { Value: var stmt }:
                Assert.Equal(stmt.Name, new Ident("u1"));
                break;
            default:
                throw Unreachable();
        }
        VerifiedStmt("CREATE OR REPLACE USER u1");
        VerifiedStmt("CREATE OR REPLACE USER IF NOT EXISTS u1");
        VerifiedStmt("CREATE OR REPLACE USER IF NOT EXISTS u1 PASSWORD='secret'");
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsBooleanLiterals);
        dialects.OneStatementParsesTo(
            "CREATE OR REPLACE USER IF NOT EXISTS u1 PASSWORD='secret' MUST_CHANGE_PASSWORD=TRUE",
            "CREATE OR REPLACE USER IF NOT EXISTS u1 PASSWORD='secret' MUST_CHANGE_PASSWORD=true");
        dialects.VerifiedStmt("CREATE OR REPLACE USER IF NOT EXISTS u1 PASSWORD='secret' MUST_CHANGE_PASSWORD=true TYPE=SERVICE TAG (t1='v1')");
        create = dialects.VerifiedStmt("CREATE OR REPLACE USER IF NOT EXISTS u1 PASSWORD='secret' MUST_CHANGE_PASSWORD=false TYPE=SERVICE WITH TAG (t1='v1', t2='v2')");
        switch (create)
        {
            case Statement.CreateUser { Value: var stmt }:
                Assert.Equal(stmt.Name, new Ident("u1"));
                Assert.True(stmt.OrReplace); // assert_eq!(stmt.or_replace, true)
                Assert.True(stmt.IfNotExists); // assert_eq!(stmt.if_not_exists, true)
                Assert.Equal(
                    stmt.Options,
                    new KeyValueOptions(
                        Delimiter: KeyValueOptionsDelimiter.Space,
                        Options: Sequence.Of(
                            new KeyValueOption(
                                OptionName: "PASSWORD",
                                OptionValue: new KeyValueOptionKind.Single(
                                    new Value.SingleQuotedString("secret").WithEmptySpan())),
                            new KeyValueOption(
                                OptionName: "MUST_CHANGE_PASSWORD",
                                OptionValue: new KeyValueOptionKind.Single(
                                    new Value.Boolean(false).WithEmptySpan())),
                            new KeyValueOption(
                                OptionName: "TYPE",
                                OptionValue: new KeyValueOptionKind.Single(
                                    new Value.Placeholder("SERVICE").WithEmptySpan())))));
                Assert.True(stmt.WithTags); // assert_eq!(stmt.with_tags, true)
                Assert.Equal(
                    stmt.Tags,
                    new KeyValueOptions(
                        Delimiter: KeyValueOptionsDelimiter.Comma,
                        Options: Sequence.Of(
                            new KeyValueOption(
                                OptionName: "t1",
                                OptionValue: new KeyValueOptionKind.Single(
                                    new Value.SingleQuotedString("v1").WithEmptySpan())),
                            new KeyValueOption(
                                OptionName: "t2",
                                OptionValue: new KeyValueOptionKind.Single(
                                    new Value.SingleQuotedString("v2").WithEmptySpan())))));
                break;
            default:
                throw Unreachable();
        }
    }

    [Fact]
    public void key_value_option_statements_do_not_swallow_following_statement()
    {
        // An unparenthesized key-value option list must not swallow the statement
        // terminator, otherwise any following statement fails to parse. This covers
        // every unparenthesized caller of `parse_key_value_options`: `CREATE USER`
        // and both `ALTER USER ... SET` forms.

        // `CREATE USER` parses identically across all dialects.
        var statements = TestUtils.AllDialects()
            .ParseSqlStatements("CREATE USER user1; SELECT 1");
        Assert.Equal(2, statements.Count);

        // `ALTER USER ... SET` routes through the same key-value option list, but
        // PostgreSQL parses `ALTER USER` as a synonym for `ALTER ROLE` (a different
        // code path), so scope these to dialects that keep the Snowflake grammar.
        var dialects = TestUtils.AllDialectsExcept(d => d.SupportsAlterUserAsAlterRole);
        foreach (var sql in new[]
        {
            "ALTER USER user1 SET x = 'y'; SELECT 1",
            "ALTER USER user1 SET TAG t = 'v'; SELECT 1",
        })
        {
            // Rust: `let statements = ..` (sombreamento) e `assert_eq!(statements.len(), 2, "{sql}")`.
            var alterStatements = dialects.ParseSqlStatements(sql);
            Assert.True(alterStatements.Count == 2, sql);
        }
    }

    [Fact]
    public void parse_drop_stream()
    {
        var sql = "DROP STREAM s1";
        switch (VerifiedStmt(sql))
        {
            case Statement.Drop { Names: var names, ObjectType: var objectType }:
                Assert.Equal(
                    new List<string> { "s1" },
                    names.Select(n => n.ToString()).ToList());
                Assert.Equal(new ObjectType.Stream(), objectType);
                break;
            default:
                throw Unreachable();
        }
        VerifiedStmt("DROP STREAM IF EXISTS s1");
    }

    [Fact]
    public void parse_create_view_if_not_exists()
    {
        // Name after IF NOT EXISTS
        var sql = "CREATE VIEW IF NOT EXISTS v AS SELECT 1";
        _ = TestUtils.AllDialects().VerifiedStmt(sql);
        // Name before IF NOT EXISTS
        sql = "CREATE VIEW v IF NOT EXISTS AS SELECT 1";
        _ = TestUtils.AllDialects().VerifiedStmt(sql);
        // Name missing from query
        sql = "CREATE VIEW IF NOT EXISTS AS SELECT 1";
        var res = Assert.Throws<ParserException>(() => TestUtils.AllDialects().ParseSqlStatements(sql));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: AS, found: SELECT"),
            res.Error);
    }

    [Fact]
    public void test_parse_not_null_in_column_options()
    {
        var canonical =
            "CREATE TABLE foo (" +
            "abc INT DEFAULT (42 IS NOT NULL) NOT NULL," +
            " def INT," +
            " def_null BOOL GENERATED ALWAYS AS (def IS NOT NULL) STORED," +
            " CHECK (abc IS NOT NULL)" +
            ")";
        TestUtils.AllDialects().VerifiedStmt(canonical);
        TestUtils.AllDialects().OneStatementParsesTo(
            "CREATE TABLE foo (" +
            "abc INT DEFAULT (42 NOT NULL) NOT NULL," +
            " def INT," +
            " def_null BOOL GENERATED ALWAYS AS (def NOT NULL) STORED," +
            " CHECK (abc NOT NULL)" +
            ")",
            canonical);
    }

    [Fact]
    public void test_parse_default_expr_with_operators()
    {
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (c INT DEFAULT (1 + 2) + 3)");
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE t (c INT DEFAULT (1 + 2) + 3 NOT NULL)");
    }

    [Fact]
    public void test_parse_default_with_collate_column_option()
    {
        var sql = "CREATE TABLE foo (abc TEXT DEFAULT 'foo' COLLATE 'en_US')";
        var stmt = TestUtils.AllDialects().VerifiedStmt(sql);
        if (stmt is Statement.CreateTable { Value.Columns: var createTableColumns })
        {
            var columns = createTableColumns.ToList(); // `mut columns`
            var column = Pop(columns);
            Assert.Equal("abc", column.Name.Value);
            Assert.Equal(column.DataType, new DataType.Text());
            var columnOptions = column.Options.ToList(); // `column.options` (mutável)
            var collateOption = Pop(columnOptions);
            if (collateOption is { Name: null, Option: ColumnOption.Collation { Value: var collate } })
            {
                Assert.Equal("'en_US'", collate.ToString());
            }
            else
            {
                Assert.Fail($"Expected collate column option, got {collateOption}");
            }
            var defaultOption = Pop(columnOptions);
            if (defaultOption is { Name: null, Option: ColumnOption.Default { Value: Expr.Value { Inner: var value } } })
            {
                Assert.Equal("'foo'", value.ToString());
            }
            else
            {
                Assert.Fail($"Expected default column option, got {defaultOption}");
            }
        }
        else
        {
            Assert.Fail("Expected create table statement");
        }
    }

    [Fact]
    public void parse_create_table_like()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d.SupportsCreateTableLikeParenthesized);
        var sql = "CREATE TABLE new LIKE old";
        switch (dialects.VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var stmt }:
                Assert.Equal(
                    stmt.Name,
                    ObjectName.From(Sequence.Of(new Ident("new"))));
                Assert.Equal(
                    stmt.Like,
                    new CreateTableLikeKind.Plain(new CreateTableLike(
                        Name: ObjectName.From(Sequence.Of(new Ident("old"))),
                        Defaults: null)));
                break;
            default:
                throw Unreachable();
        }
        dialects = TestUtils.AllDialectsWhere(d => d.SupportsCreateTableLikeParenthesized);
        sql = "CREATE TABLE new (LIKE old)";
        switch (dialects.VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var stmt }:
                Assert.Equal(
                    stmt.Name,
                    ObjectName.From(Sequence.Of(new Ident("new"))));
                Assert.Equal(
                    stmt.Like,
                    new CreateTableLikeKind.Parenthesized(new CreateTableLike(
                        Name: ObjectName.From(Sequence.Of(new Ident("old"))),
                        Defaults: null)));
                break;
            default:
                throw Unreachable();
        }
        sql = "CREATE TABLE new (LIKE old INCLUDING DEFAULTS)";
        switch (dialects.VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var stmt }:
                Assert.Equal(
                    stmt.Name,
                    ObjectName.From(Sequence.Of(new Ident("new"))));
                Assert.Equal(
                    stmt.Like,
                    new CreateTableLikeKind.Parenthesized(new CreateTableLike(
                        Name: ObjectName.From(Sequence.Of(new Ident("old"))),
                        Defaults: new CreateTableLikeDefaults.Including())));
                break;
            default:
                throw Unreachable();
        }
        sql = "CREATE TABLE new (LIKE old EXCLUDING DEFAULTS)";
        switch (dialects.VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var stmt }:
                Assert.Equal(
                    stmt.Name,
                    ObjectName.From(Sequence.Of(new Ident("new"))));
                Assert.Equal(
                    stmt.Like,
                    new CreateTableLikeKind.Parenthesized(new CreateTableLike(
                        Name: ObjectName.From(Sequence.Of(new Ident("old"))),
                        Defaults: new CreateTableLikeDefaults.Excluding())));
                break;
            default:
                throw Unreachable();
        }
    }

    [Fact]
    public void parse_copy_options()
    {
        var copy = VerifiedStmt(
            "COPY dst (c1, c2, c3) FROM 's3://redshift-downloads/tickit/category_pipe.txt' IAM_ROLE 'arn:aws:iam::123456789:role/role1' CSV IGNOREHEADER 1");
        switch (copy)
        {
            case Statement.Copy { LegacyOptions: var legacyOptions }:
                Assert.Equal(
                    legacyOptions,
                    Sequence.Of<CopyLegacyOption>(
                        new CopyLegacyOption.IamRole(new IamRoleKind.Arn(
                            "arn:aws:iam::123456789:role/role1")),
                        new CopyLegacyOption.Csv(Sequence<CopyLegacyCsvOption>.Empty),
                        new CopyLegacyOption.IgnoreHeader(1)));
                break;
            default:
                throw Unreachable();
        }

        copy = OneStatementParsesTo(
            "COPY dst (c1, c2, c3) FROM 's3://redshift-downloads/tickit/category_pipe.txt' IAM_ROLE DEFAULT CSV IGNOREHEADER AS 1",
            "COPY dst (c1, c2, c3) FROM 's3://redshift-downloads/tickit/category_pipe.txt' IAM_ROLE DEFAULT CSV IGNOREHEADER 1");
        switch (copy)
        {
            case Statement.Copy { LegacyOptions: var legacyOptions }:
                Assert.Equal(
                    legacyOptions,
                    Sequence.Of<CopyLegacyOption>(
                        new CopyLegacyOption.IamRole(new IamRoleKind.Default()),
                        new CopyLegacyOption.Csv(Sequence<CopyLegacyCsvOption>.Empty),
                        new CopyLegacyOption.IgnoreHeader(1)));
                break;
            default:
                throw Unreachable();
        }
        OneStatementParsesTo(
            "COPY dst (c1, c2, c3) FROM 's3://redshift-downloads/tickit/category_pipe.txt' " +
            "ACCEPTANYDATE " +
            "ACCEPTINVCHARS AS '*' " +
            "BLANKSASNULL " +
            "CSV " +
            "DATEFORMAT AS 'DD-MM-YYYY' " +
            "EMPTYASNULL " +
            "IAM_ROLE DEFAULT " +
            "IGNOREHEADER AS 1 " +
            "JSON " +
            "JSON 'auto' " +
            "JSON AS 'auto' " +
            "TIMEFORMAT AS 'auto' " +
            "TRUNCATECOLUMNS " +
            "REMOVEQUOTES " +
            "COMPUPDATE " +
            "COMPUPDATE PRESET " +
            "COMPUPDATE ON " +
            "COMPUPDATE OFF " +
            "COMPUPDATE TRUE " +
            "COMPUPDATE FALSE " +
            "STATUPDATE " +
            "STATUPDATE ON " +
            "STATUPDATE OFF " +
            "STATUPDATE TRUE " +
            "STATUPDATE FALSE",
            "COPY dst (c1, c2, c3) FROM 's3://redshift-downloads/tickit/category_pipe.txt' " +
            "ACCEPTANYDATE " +
            "ACCEPTINVCHARS '*' " +
            "BLANKSASNULL " +
            "CSV " +
            "DATEFORMAT 'DD-MM-YYYY' " +
            "EMPTYASNULL " +
            "IAM_ROLE DEFAULT " +
            "IGNOREHEADER 1 " +
            "JSON " +
            "JSON AS 'auto' " +
            "JSON AS 'auto' " +
            "TIMEFORMAT 'auto' " +
            "TRUNCATECOLUMNS " +
            "REMOVEQUOTES " +
            "COMPUPDATE " +
            "COMPUPDATE PRESET " +
            "COMPUPDATE TRUE " +
            "COMPUPDATE FALSE " +
            "COMPUPDATE TRUE " +
            "COMPUPDATE FALSE " +
            "STATUPDATE " +
            "STATUPDATE TRUE " +
            "STATUPDATE FALSE " +
            "STATUPDATE TRUE " +
            "STATUPDATE FALSE");
        OneStatementParsesTo(
            "COPY dst (c1, c2, c3) FROM 's3://redshift-downloads/tickit/category_pipe.txt' FORMAT AS CSV",
            "COPY dst (c1, c2, c3) FROM 's3://redshift-downloads/tickit/category_pipe.txt' CSV");
    }

    [Fact]
    public void test_parse_semantic_view_table_factor()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsSemanticViewTableFactor);

        var validSqls = new (string, string?)[]
        {
            ("SELECT * FROM SEMANTIC_VIEW(model)", null),
            (
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1, dim2)",
                null
            ),
            ("SELECT * FROM SEMANTIC_VIEW(a.b METRICS c.d, c.e)", null),
            (
                "SELECT * FROM SEMANTIC_VIEW(model FACTS fact1, fact2)",
                null
            ),
            (
                "SELECT * FROM SEMANTIC_VIEW(model FACTS DATE_PART('year', col))",
                null
            ),
            (
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1 METRICS met1)",
                null
            ),
            (
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1 WHERE x > 0)",
                null
            ),
            (
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1) AS sv",
                null
            ),
            (
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS DATE_PART('year', col))",
                null
            ),
            (
                "SELECT * FROM SEMANTIC_VIEW(model METRICS orders.col, orders.col2)",
                null
            ),
            ("SELECT * FROM SEMANTIC_VIEW(model METRICS orders.*)", null),
            ("SELECT * FROM SEMANTIC_VIEW(model FACTS fact.*)", null),
            (
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim.* METRICS orders.*)",
                null
            ),
            // We can parse in any order but will always produce a result in a fixed order.
            (
                "SELECT * FROM SEMANTIC_VIEW(model WHERE x > 0 DIMENSIONS dim1)",
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1 WHERE x > 0)"
            ),
            (
                "SELECT * FROM SEMANTIC_VIEW(model METRICS met1 DIMENSIONS dim1)",
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1 METRICS met1)"
            ),
            (
                "SELECT * FROM SEMANTIC_VIEW(model FACTS fact1 DIMENSIONS dim1)",
                "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1 FACTS fact1)"
            ),
        };

        foreach (var (inputSql, expectedSql) in validSqls)
        {
            if (expectedSql is { } expected)
            {
                // Test that non-canonical order gets normalized
                var parsed = dialects.ParseSqlStatements(inputSql);
                var formatted = parsed[0].ToString();
                Assert.Equal(formatted, expected);
            }
            else
            {
                dialects.VerifiedStmt(inputSql);
            }
        }

        var invalidSqls = new[]
        {
            "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1 INVALID inv1)",
            "SELECT * FROM SEMANTIC_VIEW(model DIMENSIONS dim1 DIMENSIONS dim2)",
        };

        foreach (var sql in invalidSqls)
        {
            // Rust: assert!(result.is_err(), "Expected error for invalid SQL: {sql}");
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
        }

        var astSql =
            "SELECT * FROM SEMANTIC_VIEW(\n" +
            "        my_model\n" +
            "        DIMENSIONS DATE_PART('year', date_col), region_name\n" +
            "        METRICS orders.revenue, orders.count\n" +
            "        WHERE active = true\n" +
            "    ) AS model_alias";

        var stmt = dialects.ParseSqlStatements(astSql);
        switch (stmt[0])
        {
            case Statement.Query { Value: var q }:
                if (q.Body is SetExpr.Select { Value: var select })
                {
                    if (select.From.First is { Relation: var relation })
                    {
                        switch (relation)
                        {
                            case TableFactor.SemanticView
                            {
                                Name: var name,
                                Dimensions: var dimensions,
                                Metrics: var metrics,
                                Facts: var facts,
                                WhereClause: var whereClause,
                                Alias: var alias,
                            }:
                                Assert.Equal("my_model", name.ToString());
                                Assert.Equal(2, dimensions.Count);
                                Assert.Equal("DATE_PART('year', date_col)", dimensions[0].ToString());
                                Assert.Equal("region_name", dimensions[1].ToString());
                                Assert.Equal(2, metrics.Count);
                                Assert.Equal("orders.revenue", metrics[0].ToString());
                                Assert.Equal("orders.count", metrics[1].ToString());
                                Assert.True(facts.IsEmpty);
                                Assert.NotNull(whereClause);
                                Assert.Equal("active = true", Unwrap(whereClause).ToString());
                                Assert.NotNull(alias);
                                Assert.Equal("model_alias", Unwrap(alias).Name.Value);
                                break;
                            default:
                                Assert.Fail("Expected SemanticView table factor");
                                break;
                        }
                    }
                    else
                    {
                        Assert.Fail("Expected table in FROM clause");
                    }
                }
                else
                {
                    Assert.Fail("Expected SELECT statement");
                }
                break;
            default:
                Assert.Fail("Expected Query statement");
                break;
        }
    }

    [Fact]
    public void parse_adjacent_string_literal_concatenation()
    {
        var sql = "SELECT 'M' 'y' 'S' 'q' 'l'";
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsStringLiteralConcatenation);
        dialects.OneStatementParsesTo(sql, "SELECT 'MySql'");

        sql = "SELECT * FROM t WHERE col = 'Hello' \n ' ' \t 'World!'";
        dialects.OneStatementParsesTo(sql, "SELECT * FROM t WHERE col = 'Hello World!'");

        dialects = TestUtils.AllDialectsWhere(d => d.SupportsStringLiteralConcatenationWithNewline);
        sql =
            "\n" +
            "    SELECT 'abc' in ('a'\n" +
            "        'b'\n" +
            "        'c',\n" +
            "        'd'\n" +
            "    )";
        dialects.OneStatementParsesTo(sql, "SELECT 'abc' IN ('abc', 'd')");

        sql =
            "\n" +
            "    SELECT 'abc' in ('a'\n" +
            "        'b'\n" +
            "        -- COMMENT\n" +
            "        'c',\n" +
            "        -- COMMENT\n" +
            "        'd'\n" +
            "    )";
        dialects.OneStatementParsesTo(sql, "SELECT 'abc' IN ('abc', 'd')");

        sql =
            "\n" +
            "    SELECT 'abc' in ('a'\n" +
            "        'b' -- COMMENT\n" +
            "        'c',\n" +
            "        'd'\n" +
            "    )";
        dialects.OneStatementParsesTo(sql, "SELECT 'abc' IN ('abc', 'd')");
    }

    [Fact]
    public void parse_invisible_column()
    {
        var sql = "CREATE TABLE t (foo INT, bar INT INVISIBLE)";
        var stmt = VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.CreateTable { Value.Columns: var columns }:
                Assert.Equal(
                    columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("foo"),
                            DataType: new DataType.Int(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident("bar"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Invisible())))));
                break;
            default:
                Assert.Fail($"Unexpected statement {stmt}");
                break;
        }

        sql = "ALTER TABLE t ADD COLUMN bar INT INVISIBLE";
        stmt = VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.AlterTable { Value: var alterTable }:
                Assert.Equal(
                    alterTable.Operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.AddColumn(
                        ColumnKeyword: true,
                        IfNotExists: false,
                        ColumnDef: new ColumnDef(
                            Name: new Ident("bar"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Invisible()))),
                        ColumnPosition: null)));
                break;
            default:
                Assert.Fail($"Unexpected statement {stmt}");
                break;
        }
    }

    [Fact]
    public void parse_create_index_different_using_positions()
    {
        var sql = "CREATE INDEX idx_name USING BTREE ON table_name (col1)";
        var expected = "CREATE INDEX idx_name ON table_name USING BTREE (col1)";
        switch (TestUtils.AllDialects().OneStatementParsesTo(sql, expected))
        {
            case Statement.CreateIndex
            {
                Value:
                {
                    Name: var name,
                    TableName: var tableName,
                    Using: var @using,
                    Columns: var columns,
                    Unique: var unique,
                },
            }:
                Assert.Equal("idx_name", Unwrap(name).ToString());
                Assert.Equal("table_name", tableName.ToString());
                Assert.Equal(@using, new IndexType.BTree());
                Assert.Equal(1, columns.Count);
                Assert.False(unique);
                break;
            default:
                throw Unreachable();
        }

        sql = "CREATE INDEX idx_name USING BTREE ON table_name (col1) USING HASH";
        expected = "CREATE INDEX idx_name ON table_name USING BTREE (col1) USING HASH";
        switch (TestUtils.AllDialects().OneStatementParsesTo(sql, expected))
        {
            case Statement.CreateIndex
            {
                Value:
                {
                    Name: var name,
                    TableName: var tableName,
                    Columns: var columns,
                    IndexOptions: var indexOptions,
                },
            }:
                Assert.Equal("idx_name", Unwrap(name).ToString());
                Assert.Equal("table_name", tableName.ToString());
                Assert.Equal(1, columns.Count);
                // Rust: assert!(index_options.iter().any(|o| o == &IndexOption::Using(IndexType::Hash)));
                // (`Assert.True(..Any(..))` é rejeitado pelo analisador xUnit2012.)
                Assert.Contains(
                    indexOptions,
                    o => o.Equals(new IndexOption.Using(new IndexType.Hash())));
                break;
            default:
                throw Unreachable();
        }
    }

    [Fact]
    public void test_parse_alter_user()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d.SupportsAlterUserAsAlterRole);
        dialects.VerifiedStmt("ALTER USER u1");
        dialects.VerifiedStmt("ALTER USER IF EXISTS u1");
        var stmt = dialects.VerifiedStmt("ALTER USER IF EXISTS u1 RENAME TO u2");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.True(alter.IfExists);
                Assert.Equal(alter.Name, new Ident("u1"));
                Assert.Equal(alter.RenameTo, new Ident("u2"));
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER IF EXISTS u1 RESET PASSWORD");
        dialects.VerifiedStmt("ALTER USER IF EXISTS u1 ABORT ALL QUERIES");
        dialects.VerifiedStmt(
            "ALTER USER IF EXISTS u1 ADD DELEGATED AUTHORIZATION OF ROLE r1 TO SECURITY INTEGRATION i1");
        dialects.VerifiedStmt("ALTER USER IF EXISTS u1 REMOVE DELEGATED AUTHORIZATION OF ROLE r1 FROM SECURITY INTEGRATION i1");
        dialects.VerifiedStmt(
            "ALTER USER IF EXISTS u1 REMOVE DELEGATED AUTHORIZATIONS FROM SECURITY INTEGRATION i1");
        dialects.VerifiedStmt("ALTER USER IF EXISTS u1 ENROLL MFA");
        stmt = dialects.VerifiedStmt("ALTER USER u1 SET DEFAULT_MFA_METHOD PASSKEY");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(alter.SetDefaultMfaMethod, new MfaMethodKind.PassKey());
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 SET DEFAULT_MFA_METHOD TOTP");
        dialects.VerifiedStmt("ALTER USER u1 SET DEFAULT_MFA_METHOD DUO");
        stmt = dialects.VerifiedStmt("ALTER USER u1 REMOVE MFA METHOD PASSKEY");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(alter.RemoveMfaMethod, new MfaMethodKind.PassKey());
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 REMOVE MFA METHOD TOTP");
        dialects.VerifiedStmt("ALTER USER u1 REMOVE MFA METHOD DUO");
        stmt = dialects.VerifiedStmt("ALTER USER u1 MODIFY MFA METHOD PASSKEY SET COMMENT 'abc'");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(
                    alter.ModifyMfaMethod,
                    new AlterUserModifyMfaMethod(
                        Method: new MfaMethodKind.PassKey(),
                        Comment: "abc"));
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 ADD MFA METHOD OTP");
        dialects.VerifiedStmt("ALTER USER u1 ADD MFA METHOD OTP COUNT = 8");

        stmt = dialects.VerifiedStmt("ALTER USER u1 SET AUTHENTICATION POLICY p1");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(
                    alter.SetPolicy,
                    new AlterUserSetPolicy(
                        PolicyKind: new UserPolicyKind.Authentication(),
                        Policy: new Ident("p1")));
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 SET PASSWORD POLICY p1");
        dialects.VerifiedStmt("ALTER USER u1 SET SESSION POLICY p1");
        stmt = dialects.VerifiedStmt("ALTER USER u1 UNSET AUTHENTICATION POLICY");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(alter.UnsetPolicy, new UserPolicyKind.Authentication());
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 UNSET PASSWORD POLICY");
        dialects.VerifiedStmt("ALTER USER u1 UNSET SESSION POLICY");

        stmt = dialects.VerifiedStmt("ALTER USER u1 SET TAG k1='v1'");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(
                    alter.SetTag.Options,
                    Sequence.Of(new KeyValueOption(
                        OptionName: "k1",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("v1").WithEmptySpan()))));
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 SET TAG k1='v1', k2='v2'");
        stmt = dialects.VerifiedStmt("ALTER USER u1 UNSET TAG k1");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(alter.UnsetTag, Sequence.Of("k1"));
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 UNSET TAG k1, k2, k3");

        var boolDialects = TestUtils.AllDialectsWhere(d =>
            d.SupportsBooleanLiterals && !d.SupportsAlterUserAsAlterRole);
        boolDialects.OneStatementParsesTo(
            "ALTER USER u1 SET PASSWORD='secret', MUST_CHANGE_PASSWORD=TRUE, MINS_TO_UNLOCK=10",
            "ALTER USER u1 SET PASSWORD='secret', MUST_CHANGE_PASSWORD=true, MINS_TO_UNLOCK=10");

        stmt = boolDialects.VerifiedStmt(
            "ALTER USER u1 SET PASSWORD='secret', MUST_CHANGE_PASSWORD=true, MINS_TO_UNLOCK=10");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(
                    alter.SetProps,
                    new KeyValueOptions(
                        Delimiter: KeyValueOptionsDelimiter.Comma,
                        Options: Sequence.Of(
                            new KeyValueOption(
                                OptionName: "PASSWORD",
                                OptionValue: new KeyValueOptionKind.Single(
                                    new Value.SingleQuotedString("secret").WithEmptySpan())),
                            new KeyValueOption(
                                OptionName: "MUST_CHANGE_PASSWORD",
                                OptionValue: new KeyValueOptionKind.Single(
                                    new Value.Boolean(true).WithEmptySpan())),
                            new KeyValueOption(
                                OptionName: "MINS_TO_UNLOCK",
                                OptionValue: new KeyValueOptionKind.Single(
                                    TestUtils.Number("10").WithEmptySpan())))));
                break;
            default:
                throw Unreachable();
        }

        stmt = dialects.VerifiedStmt("ALTER USER u1 UNSET PASSWORD");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(alter.UnsetProps, Sequence.Of("PASSWORD"));
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 UNSET PASSWORD, MUST_CHANGE_PASSWORD, MINS_TO_UNLOCK");

        stmt = dialects.VerifiedStmt("ALTER USER u1 SET DEFAULT_SECONDARY_ROLES=('ALL')");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(
                    alter.SetProps.Options,
                    Sequence.Of(new KeyValueOption(
                        OptionName: "DEFAULT_SECONDARY_ROLES",
                        OptionValue: new KeyValueOptionKind.Multi(Sequence.Of(new Value.SingleQuotedString(
                                "ALL")
                            .WithEmptySpan())))));
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 SET DEFAULT_SECONDARY_ROLES=()");
        dialects.VerifiedStmt("ALTER USER u1 SET DEFAULT_SECONDARY_ROLES=('R1', 'R2', 'R3')");
        dialects.VerifiedStmt("ALTER USER u1 SET PASSWORD='secret', DEFAULT_SECONDARY_ROLES=('ALL')");
        dialects.VerifiedStmt("ALTER USER u1 SET DEFAULT_SECONDARY_ROLES=('ALL'), PASSWORD='secret'");
        stmt = dialects.VerifiedStmt(
            "ALTER USER u1 SET WORKLOAD_IDENTITY=(TYPE=AWS, ARN='arn:aws:iam::123456789:r1/')");
        switch (stmt)
        {
            case Statement.AlterUser { Value: var alter }:
                Assert.Equal(
                    alter.SetProps.Options,
                    Sequence.Of(new KeyValueOption(
                        OptionName: "WORKLOAD_IDENTITY",
                        OptionValue: new KeyValueOptionKind.KeyValueOptions(new KeyValueOptions(
                            Delimiter: KeyValueOptionsDelimiter.Comma,
                            Options: Sequence.Of(
                                new KeyValueOption(
                                    OptionName: "TYPE",
                                    OptionValue: new KeyValueOptionKind.Single(
                                        new Value.Placeholder("AWS").WithEmptySpan())),
                                new KeyValueOption(
                                    OptionName: "ARN",
                                    OptionValue: new KeyValueOptionKind.Single(
                                        new Value.SingleQuotedString(
                                                "arn:aws:iam::123456789:r1/")
                                            .WithEmptySpan()))))))));
                break;
            default:
                throw Unreachable();
        }
        dialects.VerifiedStmt("ALTER USER u1 SET DEFAULT_SECONDARY_ROLES=('ALL'), PASSWORD='secret', WORKLOAD_IDENTITY=(TYPE=AWS, ARN='arn:aws:iam::123456789:r1/')");

        dialects.VerifiedStmt("ALTER USER u1 PASSWORD 'AAA'");
        dialects.VerifiedStmt("ALTER USER u1 ENCRYPTED PASSWORD 'AAA'");
        dialects.VerifiedStmt("ALTER USER u1 PASSWORD NULL");

        dialects.OneStatementParsesTo(
            "ALTER USER u1 WITH PASSWORD 'AAA'",
            "ALTER USER u1 PASSWORD 'AAA'");
    }

    [Fact]
    public void parse_generic_unary_ops()
    {
        var unaryOps = new (string, UnaryOperator)[] { ("-", new UnaryOperator.Minus()), ("+", new UnaryOperator.Plus()) };
        foreach (var (strOp, op) in unaryOps)
        {
            var select = VerifiedOnlySelect($"SELECT {strOp}expr");
            Assert.Equal(
                new SelectItem.UnnamedExpr(new Expr.UnaryOp(
                    Op: op,
                    Expr: new Expr.Identifier(new Ident("expr")))),
                select.Projection[0]);
        }

        // `let select` repetido fora do laço: nome distinto (CS0136, caso 72).
        var bitwiseNotSelect = VerifiedOnlySelect("SELECT ~ expr");
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.UnaryOp(
                Op: new UnaryOperator.BitwiseNot(),
                Expr: new Expr.Identifier(new Ident("expr")))),
            bitwiseNotSelect.Projection[0]);
        OneStatementParsesTo("SELECT ~expr", "SELECT ~ expr");
    }

    [Fact]
    public void parse_reset_statement()
    {
        switch (VerifiedStmt("RESET some_parameter"))
        {
            case Statement.Reset { Value.Reset: Reset.ConfigurationParameter { Value: var o } }:
                Assert.Equal(o, ObjectName.From(Sequence.Of(new Ident("some_parameter"))));
                break;
            default:
                throw Unreachable();
        }
        switch (VerifiedStmt("RESET some_extension.some_parameter"))
        {
            case Statement.Reset { Value.Reset: Reset.ConfigurationParameter { Value: var o } }:
                Assert.Equal(
                    o,
                    ObjectName.From(Sequence.Of(new Ident("some_extension"), new Ident("some_parameter"))));
                break;
            default:
                throw Unreachable();
        }
        switch (VerifiedStmt("RESET ALL"))
        {
            case Statement.Reset { Value.Reset: var reset }:
                Assert.Equal(reset, new Reset.ALL());
                break;
            default:
                throw Unreachable();
        }
        switch (VerifiedStmt("RESET SESSION AUTHORIZATION"))
        {
            case Statement.Reset { Value.Reset: var reset }:
                Assert.Equal(reset, new Reset.SessionAuthorization());
                break;
            default:
                throw Unreachable();
        }
    }

    [Fact]
    public void test_parse_set_session_authorization()
    {
        var stmt = VerifiedStmt("SET SESSION AUTHORIZATION DEFAULT");
        Assert.Equal(
            stmt,
            new Statement.Set(new Set.SetSessionAuthorization(new SetSessionAuthorizationParam(
                Scope: new ContextModifier.Session(),
                Kind: new SetSessionAuthorizationParamKind.Default()))));

        stmt = VerifiedStmt("SET SESSION AUTHORIZATION 'username'");
        Assert.Equal(
            stmt,
            new Statement.Set(new Set.SetSessionAuthorization(new SetSessionAuthorizationParam(
                Scope: new ContextModifier.Session(),
                Kind: new SetSessionAuthorizationParamKind.User(new Ident(
                    "username",
                    '\'',
                    Span.Empty()))))));
    }

    [Fact]
    public void test_set_authorization_without_scope_errors()
    {
        // This should return a parser error, not panic.
        // Rust: assert!(res.is_err(), "SET AUTHORIZATION without a scope modifier (e.g. SESSION) should error");
        Assert.Throws<ParserException>(() => ParseSqlStatements("SET AUTHORIZATION TIME TIME"));
    }

    [Fact]
    public void parse_select_parenthesized_wildcard()
    {
        // Test SELECT DISTINCT(*) which uses a parenthesized wildcard
        // The parentheses are syntactic sugar and get normalized to just *
        var sql = "SELECT DISTINCT (*) FROM table1";
        var canonical = "SELECT DISTINCT * FROM table1";
        var select = TestUtils.AllDialects().VerifiedOnlySelectWithCanonical(sql, canonical);
        Assert.Equal(select.Distinct, new Distinct.DistinctValue());
        Assert.Equal(1, select.Projection.Count);
        Assert.True(select.Projection[0] is SelectItem.Wildcard);

        // Also test without spaces: SELECT DISTINCT(*)
        var sqlNoSpaces = "SELECT DISTINCT(*) FROM table1";
        var select2 = TestUtils.AllDialects().VerifiedOnlySelectWithCanonical(sqlNoSpaces, canonical);
        Assert.Equal(select2.Distinct, new Distinct.DistinctValue());
        Assert.Equal(1, select2.Projection.Count);
        Assert.True(select2.Projection[0] is SelectItem.Wildcard);
    }

    [Fact]
    public void parse_overlap_as_bool_and()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsDoubleAmpersandOperator);
        dialects.OneStatementParsesTo("SELECT x && y", "SELECT x AND y");
    }

    [Fact]
    public void test_parse_key_value_options_trailing_semicolon()
    {
        OneStatementParsesTo(
            "CREATE USER u1 option1='value1' option2='value2';",
            "CREATE USER u1 option1='value1' option2='value2'");
    }

    [Fact]
    public void test_binary_kw_as_cast()
    {
        TestUtils.AllDialectsWhere(d => d.SupportsBinaryKwAsCast)
            .OneStatementParsesTo("SELECT BINARY 1+1", "SELECT CAST(1 + 1 AS BINARY)");
    }

    [Fact]
    public void parse_semi_structured_data_traversal()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new SnowflakeDialect(),
            new DatabricksDialect(),
        });

        // most basic case
        var sql = "SELECT a:b FROM t";
        var select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.JsonAccess(
                Value: new Expr.Identifier(new Ident("a")),
                Path: new JsonPath(
                    Path: Sequence.Of<JsonPathElem>(new JsonPathElem.Dot(
                        Key: "b",
                        Quoted: false))))),
            select.Projection[0]);

        // identifier can be quoted
        sql = "SELECT a:\"my long object key name\" FROM t";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.JsonAccess(
                Value: new Expr.Identifier(new Ident("a")),
                Path: new JsonPath(
                    Path: Sequence.Of<JsonPathElem>(new JsonPathElem.Dot(
                        Key: "my long object key name",
                        Quoted: true))))),
            select.Projection[0]);

        dialects.VerifiedStmt("SELECT a:b::INT FROM t");

        // unquoted keywords are permitted in the object key
        sql = "SELECT a:select, a:from FROM t";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            Sequence.Of<SelectItem>(
                new SelectItem.UnnamedExpr(new Expr.JsonAccess(
                    Value: new Expr.Identifier(new Ident("a")),
                    Path: new JsonPath(
                        Path: Sequence.Of<JsonPathElem>(new JsonPathElem.Dot(
                            Key: "select",
                            Quoted: false))))),
                new SelectItem.UnnamedExpr(new Expr.JsonAccess(
                    Value: new Expr.Identifier(new Ident("a")),
                    Path: new JsonPath(
                        Path: Sequence.Of<JsonPathElem>(new JsonPathElem.Dot(
                            Key: "from",
                            Quoted: false)))))),
            select.Projection);

        // multiple levels can be traversed
        // https://docs.snowflake.com/en/user-guide/querying-semistructured#dot-notation
        sql = "SELECT a:foo.\"bar\".baz";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.JsonAccess(
                Value: new Expr.Identifier(new Ident("a")),
                Path: new JsonPath(
                    Path: Sequence.Of<JsonPathElem>(
                        new JsonPathElem.Dot(
                            Key: "foo",
                            Quoted: false),
                        new JsonPathElem.Dot(
                            Key: "bar",
                            Quoted: true),
                        new JsonPathElem.Dot(
                            Key: "baz",
                            Quoted: false)))))),
            select.Projection);

        // dot and bracket notation can be mixed (starting with : case)
        // https://docs.snowflake.com/en/user-guide/querying-semistructured#dot-notation
        sql = "SELECT a:foo[0].bar";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.JsonAccess(
                Value: new Expr.Identifier(new Ident("a")),
                Path: new JsonPath(
                    Path: Sequence.Of<JsonPathElem>(
                        new JsonPathElem.Dot(
                            Key: "foo",
                            Quoted: false),
                        new JsonPathElem.Bracket(
                            Key: Expr.MakeValue(TestUtils.Number("0"))),
                        new JsonPathElem.Dot(
                            Key: "bar",
                            Quoted: false)))))),
            select.Projection);
    }

    [Fact]
    public void parse_array_subscript()
    {
        var dialects = TestUtils.AllDialectsExcept(d =>
            d is MsSqlDialect
                || d is SnowflakeDialect
                || d is SQLiteDialect
                || d is RedshiftSqlDialect);

        dialects.VerifiedStmt("SELECT arr[1]");
        dialects.VerifiedStmt("SELECT arr[:]");
        dialects.VerifiedStmt("SELECT arr[1:2]");
        dialects.VerifiedStmt("SELECT arr[1:2:4]");
        dialects.VerifiedStmt("SELECT arr[1:array_length(arr)]");
        dialects.VerifiedStmt("SELECT arr[array_length(arr) - 1:array_length(arr)]");
        dialects
            .VerifiedStmt("SELECT arr[array_length(arr) - 2:array_length(arr) - 1:array_length(arr)]");

        dialects.VerifiedStmt("SELECT arr[1][2]");
        dialects.VerifiedStmt("SELECT arr[:][:]");
    }

    [Fact]
    public void test_wildcard_func_arg()
    {
        // Wildcard (*) and wildcard with EXCLUDE as a function argument.
        // Documented for Snowflake's HASH function but parsed for any dialect that
        // supports the wildcard-EXCLUDE select syntax.
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsSelectWildcardExclude);

        // Wildcard with EXCLUDE — canonical form has a space before the parenthesised column list.
        dialects.OneStatementParsesTo(
            "SELECT HASH(* EXCLUDE(col1)) FROM t",
            "SELECT HASH(* EXCLUDE (col1)) FROM t");
        dialects.VerifiedExpr("HASH(* EXCLUDE (col1))");
        dialects.VerifiedExpr("HASH(* EXCLUDE (col1, col2))");
    }

    [Fact]
    public void parse_select_item_multi_column_alias()
    {
        TestUtils.AllDialectsWhere(d => d.SupportsSelectItemMultiColumnAlias)
            .VerifiedStmt("SELECT stack(2, 'a', 'b', 'c', 'd') AS (col1, col2)");

        TestUtils.AllDialectsWhere(d => d.SupportsSelectItemMultiColumnAlias)
            .VerifiedStmt("SELECT stack(2, 'a', 'b', 'c', 'd') AS (col1, col2) FROM t");

        Assert.Throws<ParserException>(
            () => TestUtils.AllDialectsWhere(d => !d.SupportsSelectItemMultiColumnAlias)
                .ParseSqlStatements("SELECT stack(2, 'a', 'b') AS (col1, col2)"));
    }

    [Fact]
    public void parse_non_pg_dialects_keep_xml_names_as_regular_identifiers()
    {
        // On dialects that do NOT support XML expressions, bare `xml` should
        // be treated as a regular column identifier, not a typed-string prefix.
        var dialects = TestUtils.AllDialectsExcept(d => d.SupportsXmlExpressions);
        dialects.VerifiedOnlySelect("SELECT xml FROM t");
    }

    [Fact]
    public void parse_aliased_function_args()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsAliasedFunctionArgs);
        dialects.VerifiedOnlySelect("SELECT foo(a AS x, b)");
        dialects.VerifiedOnlySelect("SELECT foo('bar' AS x)");
        dialects.VerifiedOnlySelect("SELECT foo(1 + 2 AS x)");
        dialects.VerifiedOnlySelect("SELECT foo(lower(a) AS x, b AS y)");
        dialects.VerifiedOnlySelect("SELECT foo(a AS \"x y\")");
        dialects.VerifiedOnlySelect("SELECT foo(bar(a AS x) AS y)");
        Assert.Throws<ParserException>(
            () => TestUtils.AllDialectsExcept(d => d.SupportsAliasedFunctionArgs)
                .ParseSqlStatements("SELECT foo(a AS x)"));
    }

    [Fact]
    public void parse_xmlparse()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsXmlExpressions);

        foreach (var (sql, mode) in new[]
        {
            ("SELECT xmlparse(content '<a/>')", "content"),
            ("SELECT xmlparse(document '<a/>')", "document"),
        })
        {
            var select = dialects.VerifiedOnlySelect(sql);
            switch (TestUtils.ExprFromProjection(select.Projection[0]))
            {
                case Expr.Function
                {
                    Value:
                    {
                        Name: var name,
                        Args: FunctionArguments.List { Value: var list },
                    },
                }:
                    Assert.Equal("xmlparse", name.ToString());
                    Assert.Equal(
                        list.Args,
                        Sequence.Of<FunctionArg>(new FunctionArg.Named(
                            Name: new Ident(mode),
                            Arg: new FunctionArgExpr.Expr(new Expr.Value(
                                ValueWithSpan.From(new Value.SingleQuotedString("<a/>")))),
                            Operator: new FunctionArgOperator.Space())));
                    break;
                case var expr:
                    Assert.Fail($"expected an XMLPARSE function call, got {expr}");
                    break;
            }
        }

        // XMLPARSE needs both a mode word and a value.
        Assert.Throws<ParserException>(
            () => dialects.ParseSqlStatements("SELECT xmlparse('<a/>')"));

        // Going through the ordinary function-call path, XMLPARSE accepts the same
        // trailing clauses as any other function.
        dialects.VerifiedStmt("SELECT xmlparse(document x) FILTER (WHERE y > 1)");
        dialects.VerifiedStmt("SELECT xmlparse(document x) OVER (PARTITION BY y)");

        // On dialects without XML support, `xmlparse` stays a regular function
        // and the special `CONTENT <expr>` syntax is rejected.
        var others = TestUtils.AllDialectsExcept(d => d.SupportsXmlExpressions);
        others.VerifiedOnlySelect("SELECT xmlparse(1)");
        Assert.Throws<ParserException>(
            () => others.ParseSqlStatements("SELECT xmlparse(content '<a/>')"));
    }

    /// <summary>
    /// Regression test for the 2^N parse-time blowup in <c>parse_compound_expr</c> on
    /// inputs like <c>IF a0.a1...aN.#</c>. The parse is run on a worker thread and the
    /// main thread asserts that it reports back within a generous timeout. Post-fix
    /// the parser returns <c>Err</c> in well under a millisecond, so the timeout is a
    /// hang guard, not a perf threshold.
    /// </summary>
    [Fact]
    public void parse_compound_chain_no_exponential_blowup()
    {
        var chain = string.Join(".", Enumerable.Range(0, 30).Select(i => $"a{i}"));
        var sql = $"IF {chain}.#";

        SpawnAndRecvTimeout(
            () =>
            {
                // let _ = Parser::parse_sql(&GenericDialect {}, &sql);
                try
                {
                    _ = Parser.ParseSql(new GenericDialect(), sql);
                }
                catch (ParserException)
                {
                }
            },
            TimeSpan.FromSeconds(5),
            "parser should reject this quickly, not loop exponentially");
    }

    /// <summary>
    /// Regression test for the 2^N parse-time blowup in <c>parse_compound_expr</c> on
    /// chains like <c>x.not-b.not-b...</c>. The <c>NOT</c> keyword in field position drives
    /// <c>parse_prefix</c> -&gt; <c>parse_not</c> -&gt; <c>parse_subexpr</c>, which re-walks the
    /// remaining chain at every segment and doubles the work. Post-fix the parser
    /// handles 25 segments in well under a millisecond, so the timeout is a hang
    /// guard, not a perf threshold.
    /// </summary>
    [Fact]
    public void parse_compound_keyword_chain_no_exponential_blowup()
    {
        var body = string.Concat(Enumerable.Repeat(".not-b", 25));
        var sql = $"SELECT x{body}";

        SpawnAndRecvTimeout(
            () =>
            {
                // let _ = Parser::parse_sql(&GenericDialect {}, &sql);
                try
                {
                    _ = Parser.ParseSql(new GenericDialect(), sql);
                }
                catch (ParserException)
                {
                }
            },
            TimeSpan.FromSeconds(5),
            "parser should handle this quickly, not loop exponentially");
    }

    /// <summary>
    /// Regression test for the 2^N parse-time blowup in <c>parse_prefix</c> on inputs
    /// like <c>IF(current_time(current_time(...x</c>. Each nested <c>current_time(</c> used
    /// to be explored twice at every level (once via the speculative reserved-word
    /// arm, once via the unreserved-word fallback), doubling work per level.
    /// Post-fix the failing parse short-circuits via the position-keyed cache.
    /// </summary>
    [Fact]
    public void parse_prefix_keyword_call_chain_no_exponential_blowup()
    {
        var sql = "if(" + string.Concat(Enumerable.Repeat("current_time(", 30)) + "x";

        SpawnAndRecvTimeout(
            () =>
            {
                // let _ = Parser::parse_sql(&PostgreSqlDialect {}, &sql);
                try
                {
                    _ = Parser.ParseSql(new PostgreSqlDialect(), sql);
                }
                catch (ParserException)
                {
                }
            },
            TimeSpan.FromSeconds(5),
            "parser should reject this quickly, not loop exponentially");
    }

    /// <summary>
    /// Regression test for the 2^N parse-time blowup in <c>parse_prefix</c> on inputs
    /// like <c>case-case-case-...c</c>. Each <c>case</c> token triggers a speculative
    /// <c>parse_case_expr</c> that fails, but the unreserved-word fallback returns
    /// <c>Identifier(case)</c>, so the outer failure cache never fires. Post-fix the
    /// per-arm cache short-circuits the speculative descent.
    /// </summary>
    [Fact]
    public void parse_prefix_case_chain_no_exponential_blowup()
    {
        var sql = string.Concat(Enumerable.Repeat("case\t-", 30)) + "c";

        SpawnAndRecvTimeout(
            () =>
            {
                // let _ = Parser::parse_sql(&SQLiteDialect {}, &sql);
                try
                {
                    _ = Parser.ParseSql(new SQLiteDialect(), sql);
                }
                catch (ParserException)
                {
                }
            },
            TimeSpan.FromSeconds(5),
            "parser should reject this quickly, not loop exponentially");
    }

    /// <summary>
    /// Regression test for the 2^N parse-time blowup in <c>parse_table_factor</c> on
    /// inputs like <c>SELECT 1 FROM ((((...</c>. The speculative derived-table arm
    /// and the nested-join fallback both recurse through the remaining paren
    /// chain, doubling work per level. Post-fix the per-position failure cache
    /// short-circuits the second descent.
    /// </summary>
    [Fact]
    public void parse_table_factor_paren_chain_no_exponential_blowup()
    {
        var sql = "SELECT 1 ";
        for (var i = 0; i < 5; i++)
        {
            sql += "FROM ";
            sql += new string('(', 30);
            sql += " ";
        }

        SpawnAndRecvTimeout(
            () =>
            {
                // let _ = Parser::new(&GenericDialect {}).with_recursion_limit(256)
                //     .try_with_sql(&sql).and_then(|mut p| p.parse_statements());
                try
                {
                    _ = new Parser(new GenericDialect())
                        .WithRecursionLimit(256)
                        .TryWithSql(sql)
                        .ParseStatements();
                }
                catch (ParserException)
                {
                }
            },
            TimeSpan.FromSeconds(5),
            "parser should reject this quickly, not loop exponentially");
    }

    [Fact]
    public void parse_unlogged_table_logging_controls_in_all_dialects()
    {
        switch (TestUtils.AllDialects().VerifiedStmt("CREATE UNLOGGED TABLE t (a INT)"))
        {
            case Statement.CreateTable { Value.Unlogged: var unlogged }:
                Assert.True(unlogged);
                break;
            default:
                throw Unreachable("Expected CREATE TABLE");
        }

        switch (TestUtils.AllDialects().VerifiedStmt("ALTER TABLE t SET LOGGED"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(Sequence.Of<AlterTableOperation>(new AlterTableOperation.SetLogged()), operations);
                break;
            default:
                throw Unreachable("Expected ALTER TABLE");
        }

        switch (TestUtils.AllDialects().VerifiedStmt("ALTER TABLE t SET UNLOGGED"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(Sequence.Of<AlterTableOperation>(new AlterTableOperation.SetUnlogged()), operations);
                break;
            default:
                throw Unreachable("Expected ALTER TABLE");
        }
    }

    /// <summary>
    /// Regression test for the 2^N parse-time blowup in <c>parse_function_args</c> on
    /// inputs like <c>CAST(CASE (CAST(CASE (...</c>. On dialects with expression-named
    /// function arguments (e.g. PostgreSQL), the named-arg arm parsed the whole
    /// argument expression and then re-parsed it on the unnamed path, doubling
    /// work per level. Post-fix the leading expression is parsed exactly once.
    /// </summary>
    [Fact]
    public void parse_function_arg_call_chain_no_exponential_blowup()
    {
        var sql = "SELECT " + string.Concat(Enumerable.Repeat("CAST(CASE (", 30)) + new string(')', 30);

        SpawnAndRecvTimeout(
            () =>
            {
                // let _ = Parser::new(&PostgreSqlDialect {}).with_recursion_limit(256)
                //     .try_with_sql(&sql).and_then(|mut p| p.parse_statements());
                try
                {
                    _ = new Parser(new PostgreSqlDialect())
                        .WithRecursionLimit(256)
                        .TryWithSql(sql)
                        .ParseStatements();
                }
                catch (ParserException)
                {
                }
            },
            TimeSpan.FromSeconds(5),
            "parser should reject this quickly, not loop exponentially");
    }

    [Fact]
    public void parse_insert_by_name()
    {
        VerifiedStmt("INSERT INTO target BY NAME SELECT 1 AS a");

        switch (VerifiedStmt("INSERT INTO target (a) BY NAME SELECT 1 AS a"))
        {
            case Statement.Insert { Value: { ByName: var byName, Columns: var columns } }:
                Assert.True(byName);
                Assert.Single(columns);
                break;
            default:
                throw Unreachable();
        }

        var dialects = TestUtils.AllDialectsWhere(d => !d.SupportsInsertTableAlias);
        switch (dialects.VerifiedStmt("INSERT INTO TABLE target PARTITION (p = 1) BY NAME SELECT 1 AS a"))
        {
            case Statement.Insert
            {
                Value:
                {
                    ByName: var byName,
                    HasTableKeyword: var hasTableKeyword,
                    Partitioned: var partitioned,
                }
            }:
                Assert.True(byName);
                Assert.True(hasTableKeyword);
                Assert.Single(partitioned!.Value);
                break;
            default:
                throw Unreachable();
        }

        // `BY NAME` does not shadow a table alias in dialects supporting one.
        dialects = TestUtils.AllDialectsWhere(d => d.SupportsInsertTableAlias);
        switch (dialects.VerifiedStmt("INSERT INTO target AS t BY NAME SELECT 1 AS a"))
        {
            case Statement.Insert { Value: { ByName: var byName, TableAlias: var tableAlias } }:
                Assert.True(byName);
                Assert.Equal("t", tableAlias!.Alias.Value);
                break;
            default:
                throw Unreachable();
        }
    }

    [Fact]
    public void parse_bitwise_not_renders_apart_from_operand()
    {
        TestUtils.AllDialects().VerifiedStmt("SELECT ~ -1");
        TestUtils.AllDialects().VerifiedStmt("SELECT ~ ~ 1");
    }

    [Fact]
    public void parse_unary_minus_never_renders_line_comment()
    {
        TestUtils.AllDialects().VerifiedStmt("SELECT - -1");
        TestUtils.AllDialects().VerifiedStmt("SELECT - - -1");
        TestUtils.AllDialects().VerifiedStmt("SELECT -1");
        TestUtils.AllDialects().VerifiedStmt("SELECT -x");
    }

    [Fact]
    public void parse_table_preserves_quotes_and_trailing_tokens()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new AnsiDialect(),
            new GenericDialect(),
            new PostgreSqlDialect(),
            new DuckDbDialect(),
            new SnowflakeDialect(),
        });
        dialects.VerifiedStmt("CREATE TABLE new_table AS TABLE \"old_table\"");
        dialects.VerifiedStmt("CREATE TABLE new_table AS TABLE \"schema_name\".\"old_table\"");
        dialects.VerifiedStmt("CREATE TABLE new_table AS TABLE old_table ORDER BY x");
        dialects.VerifiedStmt("CREATE TABLE new_table AS TABLE old_table LIMIT 10");
        dialects.VerifiedStmt("SELECT * FROM (TABLE old_table ORDER BY x)");

        var backtickDialects = new TestedDialects(new List<Dialect>
        {
            new AnsiDialect(),
            new GenericDialect(),
            new MySqlDialect(),
        });
        backtickDialects.VerifiedStmt("CREATE TABLE new_table AS TABLE `old_table`");
        backtickDialects.VerifiedStmt("CREATE TABLE new_table AS TABLE `%mpty`");
        backtickDialects.VerifiedStmt("INSERT INTO t TABLE `%mpty`");

        var err = Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements("CREATE TABLE new_table AS TABLE %mpty")).Error;
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: identifier, found: %"),
            err);

        err = Assert.Throws<ParserException>(() => backtickDialects
            .ParseSqlStatements("CREATE TABLE new_table AS TABLE `x` ORE")).Error;
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: ORE"),
            err);
    }

    [Fact]
    public void parse_hex_string_literal_display_escaping()
    {
        TestUtils.AllDialects().VerifiedStmt("SELECT X''''");
        TestUtils.AllDialects().VerifiedStmt("SELECT X'ab''cd'");
        TestUtils.AllDialects().OneStatementParsesTo("SELECT x'''' N", "SELECT X'''' AS N");
    }

    [Fact]
    public void parse_stage_table_factor()
    {
        var supported = TestUtils.AllDialectsWhere(d => d.SupportsStages);
        supported.VerifiedStmt("SELECT * FROM @stage");
        supported.VerifiedStmt("SELECT * FROM @stage, my_table");

        var unsupported = TestUtils.AllDialectsWhere(d => !d.SupportsStages && !d.IsIdentifierStart('@'));
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => unsupported
                .ParseSqlStatements("SELECT * FROM @stage")).Error,
            new ParserError.ParserErrorValue("Expected: identifier, found: @"));
    }

    [Fact]
    public void parse_alter_table_column_position()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsAlterColumnPosition);
        switch (TestUtils.AlterTableOp(dialects.VerifiedStmt("ALTER TABLE tab ADD COLUMN c INT FIRST")))
        {
            case AlterTableOperation.AddColumn { ColumnPosition: var columnPosition }:
                Assert.Equal<MySQLColumnPosition?>(columnPosition, new MySQLColumnPosition.First());
                break;
            default:
                throw Unreachable();
        }
        switch (TestUtils.AlterTableOp(dialects.VerifiedStmt("ALTER TABLE tab ADD COLUMN c INT AFTER b")))
        {
            case AlterTableOperation.AddColumn { ColumnPosition: var columnPosition }:
                Assert.Equal<MySQLColumnPosition?>(
                    columnPosition,
                    new MySQLColumnPosition.After(new Ident("b")));
                break;
            default:
                throw Unreachable();
        }
        switch (TestUtils.AlterTableOp(dialects.VerifiedStmt("ALTER TABLE tab MODIFY COLUMN c INT AFTER b")))
        {
            case AlterTableOperation.ModifyColumn { ColumnPosition: var columnPosition }:
                Assert.Equal<MySQLColumnPosition?>(
                    columnPosition,
                    new MySQLColumnPosition.After(new Ident("b")));
                break;
            default:
                throw Unreachable();
        }
        // assert!(..parse_sql_statements(..).is_err())
        Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements("ALTER TABLE tab ADD COLUMN c INT AFTER"));

        dialects = TestUtils.AllDialectsWhere(d => !d.SupportsAlterColumnPosition);
        foreach (var sql in new[]
        {
            "ALTER TABLE tab ADD COLUMN c INT FIRST",
            "ALTER TABLE tab ADD COLUMN c INT AFTER b",
        })
        {
            // assert!(dialects.parse_sql_statements(sql).is_err(), "{sql}")
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
        }
    }

    [Fact]
    public void parse_compound_field_access_numeric_display()
    {
        TestUtils.AllDialects().VerifiedStmt("SELECT 1 . i");
        TestUtils.AllDialects().VerifiedStmt("SELECT (1 . i)");
        TestUtils.AllDialects().VerifiedStmt("SELECT 1 . 2");
        TestUtils.AllDialects().VerifiedStmt("SELECT (a . 1 . b)");
        TestUtils.AllDialects().VerifiedStmt("SELECT 1 . i.j");
        TestUtils.AllDialects().VerifiedStmt("SELECT 1 . 2 . 3");
        TestUtils.AllDialects().VerifiedStmt("SELECT a.b.c");

        var err = Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => !d.SupportsNumericPrefix)
            .ParseSqlStatements("SELECT (1.i)")).Error;
        Assert.Equal<ParserError>(
            err,
            new ParserError.ParserErrorValue("Expected: ), found: i"));
    }

    [Fact]
    public void parse_placeholder_disallows_quoted_ident()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new AnsiDialect(),
            new GenericDialect(),
            new SnowflakeDialect(),
            new SQLiteDialect(),
        });
        // Valid placeholders roundtrip
        dialects.VerifiedStmt("SELECT :x");

        // Quoted identifiers are not valid placeholders
        var err = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT :`a`")).Error;
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: placeholder, found: `a`"),
            err);
        err = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT :\"a\"")).Error;
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: placeholder, found: \"a\""),
            err);
        err = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT:` a` a")).Error;
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: placeholder, found: ` a`"),
            err);

        var ansi = new TestedDialects(new List<Dialect> { new AnsiDialect() });
        ansi.VerifiedStmt("SELECT @x");
        err = Assert.Throws<ParserException>(() => ansi.ParseSqlStatements("SELECT @`a`")).Error;
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: placeholder, found: `a`"),
            err);
        err = Assert.Throws<ParserException>(() => ansi.ParseSqlStatements("SELECT @\"a\"")).Error;
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: placeholder, found: \"a\""),
            err);
    }

    [Fact]
    public void parse_in_list_leading_subquery()
    {
        Func<Expr> x = () => new Expr.Identifier(new Ident("x"));
        Func<Expr> subquery = () => new Expr.Subquery(VerifiedQuery("SELECT a FROM u"));

        var select = VerifiedOnlySelect("SELECT * FROM t WHERE x IN ((SELECT a FROM u), 1)");
        Assert.Equal(
            new Expr.InList(
                Expr: x(),
                List: Sequence.Of(subquery(), Expr.MakeValue(TestUtils.Number("1"))),
                Negated: false,
                Global: false),
            select.Selection!);

        select = VerifiedOnlySelect("SELECT * FROM t WHERE x NOT IN (((SELECT a FROM u)), 1)");
        Assert.Equal(
            new Expr.InList(
                Expr: x(),
                List: Sequence.Of<Expr>(new Expr.Nested(subquery()), Expr.MakeValue(TestUtils.Number("1"))),
                Negated: true,
                Global: false),
            select.Selection!);

        select = VerifiedOnlySelect("SELECT * FROM t WHERE x IN ((SELECT a FROM u) + 1, 2)");
        Assert.Equal(
            new Expr.InList(
                Expr: x(),
                List: Sequence.Of<Expr>(
                    new Expr.BinaryOp(
                        Left: subquery(),
                        Op: new BinaryOperator.Plus(),
                        Right: Expr.MakeValue(TestUtils.Number("1"))),
                    Expr.MakeValue(TestUtils.Number("2"))),
                Negated: false,
                Global: false),
            select.Selection!);

        select = PgAndGeneric()
            .VerifiedOnlySelect("SELECT * FROM t WHERE x IN ((SELECT a FROM u)[1], 2)");
        Assert.True(
            select.Selection is Expr.InList { List: var list }
            && list[0] is Expr.CompoundFieldAccess { Root: var root }
            && root.Equals(subquery()));

        select = VerifiedOnlySelect("SELECT * FROM t WHERE x IN ((SELECT a FROM u))");
        Assert.Equal(
            new Expr.InSubquery(
                Expr: x(),
                Subquery: VerifiedQuery("(SELECT a FROM u)"),
                Negated: false,
                Global: false),
            select.Selection!);

        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: 1"),
            Assert.Throws<ParserException>(
                () => ParseSqlStatements("SELECT * FROM t WHERE x IN ((SELECT a FROM u) 1)")).Error);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: EOF"),
            Assert.Throws<ParserException>(
                () => ParseSqlStatements("SELECT * FROM t WHERE x IN (SELECT a FROM u")).Error);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: EOF"),
            Assert.Throws<ParserException>(
                () => ParseSqlStatements("SELECT * FROM t WHERE x IN ((SELECT a FROM u), 1")).Error);
        // The inner `IN` is read again for the outer list after its subquery
        // arm was cached as failed.
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: EOF"),
            Assert.Throws<ParserException>(
                () => ParseSqlStatements("SELECT * FROM t WHERE x IN ((SELECT 1 FROM u WHERE y IN (SELECT 2")).Error);
    }

    /// <summary>
    /// Hang guard for <c>IN</c> lists nested in the leading subquery of an enclosing
    /// <c>IN</c> list, where every level is first tried as the whole subquery and then
    /// read again as a list item.
    /// </summary>
    [Fact]
    public void parse_in_list_nested_leading_subquery_no_exponential_blowup()
    {
        foreach (var level in new[]
        {
            "x IN ((SELECT {}), 1)",
            "x IN ((SELECT {}) + 1, 2)",
            "x IN (((SELECT {}) + 1), 2)",
        })
        {
            var sql = "SELECT "
                + Enumerable.Range(0, 30).Aggregate("1", (inner, _) => level.Replace("{}", inner));

            // let _ = tx.send(result.is_ok()); ... let parsed = rx.recv_timeout(..).expect(..);
            var parsed = false;
            SpawnAndRecvTimeout(
                () =>
                {
                    try
                    {
                        _ = new Parser(new GenericDialect())
                            .WithRecursionLimit(1000)
                            .TryWithSql(sql)
                            .ParseStatements();
                        parsed = true;
                    }
                    catch (ParserException)
                    {
                        parsed = false;
                    }
                },
                TimeSpan.FromSeconds(5),
                "parser should handle this quickly, not loop exponentially");
            Assert.True(parsed, $"{level} nested 30 deep should parse");
        }
    }

    [Fact]
    public void parse_bang_not_renders_apart_from_operand()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsBangNotOperator);
        dialects.VerifiedStmt("SELECT ! !a");
        dialects.VerifiedStmt("SELECT ! ! !a");
        dialects.VerifiedStmt("SELECT ! ~ a");
        dialects.VerifiedStmt("SELECT ! -a");
        dialects.VerifiedStmt("SELECT ! +a");
        dialects.VerifiedStmt("SELECT !a");
        dialects.VerifiedStmt("SELECT !(b > 3)");
        dialects.VerifiedStmt("SELECT ! -a + b");
        dialects.VerifiedStmt("SELECT !a + b");
        dialects.VerifiedStmt("SELECT !NOT a");
        dialects.VerifiedStmt("SET eaac_cion = ! !o");
        dialects.OneStatementParsesTo("SET eaac_cion = ! ! o", "SET eaac_cion = ! !o");
    }

    [Fact]
    public void parse_position_chain_no_exponential_blowup()
    {
        var sql = "SELECT " + string.Concat(Enumerable.Repeat("POSITION(", 24)) + "1" + new string(')', 24);

        // let _ = tx.send(Parser::parse_sql(&GenericDialect {}, &sql).map(|_| ()));
        // `null` = `Ok(())`; senão, o erro do `Err`.
        ParserException? result = null;
        // 24 levels overflow the default 2 MB test-thread stack in debug builds.
        SpawnAndRecvTimeout(
            () =>
            {
                try
                {
                    _ = Parser.ParseSql(new GenericDialect(), sql);
                }
                catch (ParserException e)
                {
                    result = e;
                }
            },
            TimeSpan.FromSeconds(5),
            "parser should finish quickly, not loop exponentially",
            stackSize: 8 * 1024 * 1024);
        // assert!(result.is_ok(), "{result:?}")
        Assert.True(result is null, $"{result?.Error}");
    }

    [Fact]
    public void parse_position_forms_with_nested_fallback()
    {
        var dialects = TestUtils.AllDialects();
        Assert.IsType<Expr.Position>(dialects.VerifiedExpr("POSITION('@' IN field)"));
        // The inner call takes the `IN` form and the outer one falls back.
        switch (dialects.VerifiedExpr("POSITION(POSITION('@' IN field))"))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value.Args: var args } }:
                Assert.True(args is [FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.Position } }]);
                break;
            case var other:
                throw new InvalidOperationException($"outer POSITION should be an ordinary call, got {other}");
        }
        // The outer call takes the `IN` form around an ordinary inner call.
        Assert.IsType<Expr.Position>(dialects.VerifiedExpr("POSITION(POSITION(x) IN field)"));
        dialects.VerifiedExpr("POSITION(POSITION(POSITION(x)), POSITION(y))");
    }

    // #[cfg(feature = "visitor")]: o visitor está portado (src/ast/visitor.rs → Ast/Visitor/, ADR-0007).
    [Fact]
    public void parse_position_in_connect_by_list_after_state_change()
    {
        // The list is first tried as a subquery in `CONNECT BY` state, where
        // `PRIOR` is an operator, then reparsed as expressions, where it is a name.
        var sql = "SELECT * FROM t CONNECT BY x IN (((SELECT POSITION(PRIOR IN (z)))), 1)";
        foreach (var dialect in TestUtils.AllDialects().Dialects)
        {
            var stmts = Parser.ParseSql(dialect, sql);
            var positions = 0;
            _ = Visitor.VisitExpressions(stmts, expr =>
            {
                if (expr is Expr.Position)
                {
                    positions += 1;
                }
                return ControlFlow<ValueTuple>.Continue; // core::ops::ControlFlow::<()>::Continue(())
            });
            TestUtils.AssertEqualWithMessage(positions, 1, dialect.GetType().Name);
        }
    }
}
