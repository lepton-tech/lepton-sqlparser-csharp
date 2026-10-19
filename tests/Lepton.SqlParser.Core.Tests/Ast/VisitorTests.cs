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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/visitor.rs (mod tests, linhas 802–1292;
// mod visit_mut_tests, linhas 1294–1376).
// Modificado: tradução de Rust para C#.

using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

// mod tests
public class VisitorTests
{
    // #[derive(Default)] struct TestVisitor { visited: Vec<String> }
    private sealed class TestVisitor : Visitor<ValueTuple>
    {
        public List<string> Visited { get; } = new List<string>();

        /// <summary>Invoked for any queries that appear in the AST before visiting children</summary>
        public override ControlFlow<ValueTuple> PreVisitQuery(Query query)
        {
            Visited.Add($"PRE: QUERY: {query}");
            return ControlFlow<ValueTuple>.Continue;
        }

        /// <summary>Invoked for any queries that appear in the AST after visiting children</summary>
        public override ControlFlow<ValueTuple> PostVisitQuery(Query query)
        {
            Visited.Add($"POST: QUERY: {query}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitSelect(Select select)
        {
            Visited.Add($"PRE: SELECT: {select}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitSelect(Select select)
        {
            Visited.Add($"POST: SELECT: {select}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitRelation(ObjectName relation)
        {
            Visited.Add($"PRE: RELATION: {relation}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitRelation(ObjectName relation)
        {
            Visited.Add($"POST: RELATION: {relation}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitTableFactor(TableFactor tableFactor)
        {
            Visited.Add($"PRE: TABLE FACTOR: {tableFactor}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitTableFactor(TableFactor tableFactor)
        {
            Visited.Add($"POST: TABLE FACTOR: {tableFactor}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitExpr(Expr expr)
        {
            Visited.Add($"PRE: EXPR: {expr}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitExpr(Expr expr)
        {
            Visited.Add($"POST: EXPR: {expr}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitStatement(Statement statement)
        {
            Visited.Add($"PRE: STATEMENT: {statement}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitStatement(Statement statement)
        {
            Visited.Add($"POST: STATEMENT: {statement}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitOrderBy(OrderBy orderBy)
        {
            Visited.Add($"PRE: ORDER BY: {orderBy}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitOrderBy(OrderBy orderBy)
        {
            Visited.Add($"POST: ORDER BY: {orderBy}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitOrderByExpr(OrderByExpr orderByExpr)
        {
            Visited.Add($"PRE: ORDER BY EXPR: {orderByExpr}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitOrderByExpr(OrderByExpr orderByExpr)
        {
            Visited.Add($"POST: ORDER BY EXPR: {orderByExpr}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitGroupBy(GroupByExpr groupBy)
        {
            Visited.Add($"PRE: GROUP BY: {groupBy}");
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitGroupBy(GroupByExpr groupBy)
        {
            Visited.Add($"POST: GROUP BY: {groupBy}");
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    private static Statement DoVisit(string sql, Visitor<ValueTuple> visitor)
    {
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var s = new Parser(dialect)
            .WithTokens(tokens)
            .ParseStatement();

        var flow = s.Visit(visitor);
        Assert.Equal(flow, ControlFlow<ValueTuple>.Continue);
        return s;
    }

    [Fact]
    public void test_sql()
    {
        var tests = new (string Sql, string[] Expected)[]
        {
            (
                "SELECT * from table_name as my_table",
                new[]
                {
                    "PRE: STATEMENT: SELECT * FROM table_name AS my_table",
                    "PRE: QUERY: SELECT * FROM table_name AS my_table",
                    "PRE: SELECT: SELECT * FROM table_name AS my_table",
                    "PRE: TABLE FACTOR: table_name AS my_table",
                    "PRE: RELATION: table_name",
                    "POST: RELATION: table_name",
                    "POST: TABLE FACTOR: table_name AS my_table",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT * FROM table_name AS my_table",
                    "POST: QUERY: SELECT * FROM table_name AS my_table",
                    "POST: STATEMENT: SELECT * FROM table_name AS my_table",
                }
            ),
            (
                "SELECT * from t1 join t2 on t1.id = t2.t1_id",
                new[]
                {
                    "PRE: STATEMENT: SELECT * FROM t1 JOIN t2 ON t1.id = t2.t1_id",
                    "PRE: QUERY: SELECT * FROM t1 JOIN t2 ON t1.id = t2.t1_id",
                    "PRE: SELECT: SELECT * FROM t1 JOIN t2 ON t1.id = t2.t1_id",
                    "PRE: TABLE FACTOR: t1",
                    "PRE: RELATION: t1",
                    "POST: RELATION: t1",
                    "POST: TABLE FACTOR: t1",
                    "PRE: TABLE FACTOR: t2",
                    "PRE: RELATION: t2",
                    "POST: RELATION: t2",
                    "POST: TABLE FACTOR: t2",
                    "PRE: EXPR: t1.id = t2.t1_id",
                    "PRE: EXPR: t1.id",
                    "POST: EXPR: t1.id",
                    "PRE: EXPR: t2.t1_id",
                    "POST: EXPR: t2.t1_id",
                    "POST: EXPR: t1.id = t2.t1_id",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT * FROM t1 JOIN t2 ON t1.id = t2.t1_id",
                    "POST: QUERY: SELECT * FROM t1 JOIN t2 ON t1.id = t2.t1_id",
                    "POST: STATEMENT: SELECT * FROM t1 JOIN t2 ON t1.id = t2.t1_id",
                }
            ),
            (
                "SELECT * from t1 where EXISTS(SELECT column from t2)",
                new[]
                {
                    "PRE: STATEMENT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "PRE: QUERY: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "PRE: SELECT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "PRE: TABLE FACTOR: t1",
                    "PRE: RELATION: t1",
                    "POST: RELATION: t1",
                    "POST: TABLE FACTOR: t1",
                    "PRE: EXPR: EXISTS (SELECT column FROM t2)",
                    "PRE: QUERY: SELECT column FROM t2",
                    "PRE: SELECT: SELECT column FROM t2",
                    "PRE: EXPR: column",
                    "POST: EXPR: column",
                    "PRE: TABLE FACTOR: t2",
                    "PRE: RELATION: t2",
                    "POST: RELATION: t2",
                    "POST: TABLE FACTOR: t2",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT column FROM t2",
                    "POST: QUERY: SELECT column FROM t2",
                    "POST: EXPR: EXISTS (SELECT column FROM t2)",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "POST: QUERY: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "POST: STATEMENT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                }
            ),
            (
                "SELECT * from t1 where EXISTS(SELECT column from t2)",
                new[]
                {
                    "PRE: STATEMENT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "PRE: QUERY: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "PRE: SELECT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "PRE: TABLE FACTOR: t1",
                    "PRE: RELATION: t1",
                    "POST: RELATION: t1",
                    "POST: TABLE FACTOR: t1",
                    "PRE: EXPR: EXISTS (SELECT column FROM t2)",
                    "PRE: QUERY: SELECT column FROM t2",
                    "PRE: SELECT: SELECT column FROM t2",
                    "PRE: EXPR: column",
                    "POST: EXPR: column",
                    "PRE: TABLE FACTOR: t2",
                    "PRE: RELATION: t2",
                    "POST: RELATION: t2",
                    "POST: TABLE FACTOR: t2",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT column FROM t2",
                    "POST: QUERY: SELECT column FROM t2",
                    "POST: EXPR: EXISTS (SELECT column FROM t2)",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "POST: QUERY: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "POST: STATEMENT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                }
            ),
            (
                "SELECT * from t1 where EXISTS(SELECT column from t2) UNION SELECT * from t3",
                new[]
                {
                    "PRE: STATEMENT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2) UNION SELECT * FROM t3",
                    "PRE: QUERY: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2) UNION SELECT * FROM t3",
                    "PRE: SELECT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "PRE: TABLE FACTOR: t1",
                    "PRE: RELATION: t1",
                    "POST: RELATION: t1",
                    "POST: TABLE FACTOR: t1",
                    "PRE: EXPR: EXISTS (SELECT column FROM t2)",
                    "PRE: QUERY: SELECT column FROM t2",
                    "PRE: SELECT: SELECT column FROM t2",
                    "PRE: EXPR: column",
                    "POST: EXPR: column",
                    "PRE: TABLE FACTOR: t2",
                    "PRE: RELATION: t2",
                    "POST: RELATION: t2",
                    "POST: TABLE FACTOR: t2",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT column FROM t2",
                    "POST: QUERY: SELECT column FROM t2",
                    "POST: EXPR: EXISTS (SELECT column FROM t2)",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2)",
                    "PRE: SELECT: SELECT * FROM t3",
                    "PRE: TABLE FACTOR: t3",
                    "PRE: RELATION: t3",
                    "POST: RELATION: t3",
                    "POST: TABLE FACTOR: t3",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT * FROM t3",
                    "POST: QUERY: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2) UNION SELECT * FROM t3",
                    "POST: STATEMENT: SELECT * FROM t1 WHERE EXISTS (SELECT column FROM t2) UNION SELECT * FROM t3",
                }
            ),
            (
                "SELECT * FROM monthly_sales "
                    + "PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d) "
                    + "ORDER BY EMPID",
                new[]
                {
                    "PRE: STATEMENT: SELECT * FROM monthly_sales PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d) ORDER BY EMPID",
                    "PRE: QUERY: SELECT * FROM monthly_sales PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d) ORDER BY EMPID",
                    "PRE: SELECT: SELECT * FROM monthly_sales PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d)",
                    "PRE: TABLE FACTOR: monthly_sales PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d)",
                    "PRE: TABLE FACTOR: monthly_sales",
                    "PRE: RELATION: monthly_sales",
                    "POST: RELATION: monthly_sales",
                    "POST: TABLE FACTOR: monthly_sales",
                    "PRE: EXPR: SUM(a.amount)",
                    "PRE: EXPR: a.amount",
                    "POST: EXPR: a.amount",
                    "POST: EXPR: SUM(a.amount)",
                    "PRE: EXPR: a.MONTH",
                    "POST: EXPR: a.MONTH",
                    "PRE: EXPR: 'JAN'",
                    "POST: EXPR: 'JAN'",
                    "PRE: EXPR: 'FEB'",
                    "POST: EXPR: 'FEB'",
                    "PRE: EXPR: 'MAR'",
                    "POST: EXPR: 'MAR'",
                    "PRE: EXPR: 'APR'",
                    "POST: EXPR: 'APR'",
                    "POST: TABLE FACTOR: monthly_sales PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d)",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT * FROM monthly_sales PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d)",
                    "PRE: ORDER BY: ORDER BY EMPID",
                    "PRE: ORDER BY EXPR: EMPID",
                    "PRE: EXPR: EMPID",
                    "POST: EXPR: EMPID",
                    "POST: ORDER BY EXPR: EMPID",
                    "POST: ORDER BY: ORDER BY EMPID",
                    "POST: QUERY: SELECT * FROM monthly_sales PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d) ORDER BY EMPID",
                    "POST: STATEMENT: SELECT * FROM monthly_sales PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d) ORDER BY EMPID",
                }
            ),
            (
                "SHOW COLUMNS FROM t1",
                new[]
                {
                    "PRE: STATEMENT: SHOW COLUMNS FROM t1",
                    "PRE: RELATION: t1",
                    "POST: RELATION: t1",
                    "POST: STATEMENT: SHOW COLUMNS FROM t1",
                }
            ),
            (
                "SELECT * FROM t1 ORDER BY a DESC, b",
                new[]
                {
                    "PRE: STATEMENT: SELECT * FROM t1 ORDER BY a DESC, b",
                    "PRE: QUERY: SELECT * FROM t1 ORDER BY a DESC, b",
                    "PRE: SELECT: SELECT * FROM t1",
                    "PRE: TABLE FACTOR: t1",
                    "PRE: RELATION: t1",
                    "POST: RELATION: t1",
                    "POST: TABLE FACTOR: t1",
                    "PRE: GROUP BY: GROUP BY ",
                    "POST: GROUP BY: GROUP BY ",
                    "POST: SELECT: SELECT * FROM t1",
                    "PRE: ORDER BY: ORDER BY a DESC, b",
                    "PRE: ORDER BY EXPR: a DESC",
                    "PRE: EXPR: a",
                    "POST: EXPR: a",
                    "POST: ORDER BY EXPR: a DESC",
                    "PRE: ORDER BY EXPR: b",
                    "PRE: EXPR: b",
                    "POST: EXPR: b",
                    "POST: ORDER BY EXPR: b",
                    "POST: ORDER BY: ORDER BY a DESC, b",
                    "POST: QUERY: SELECT * FROM t1 ORDER BY a DESC, b",
                    "POST: STATEMENT: SELECT * FROM t1 ORDER BY a DESC, b",
                }
            ),
            (
                "SELECT a FROM t GROUP BY a, b",
                new[]
                {
                    "PRE: STATEMENT: SELECT a FROM t GROUP BY a, b",
                    "PRE: QUERY: SELECT a FROM t GROUP BY a, b",
                    "PRE: SELECT: SELECT a FROM t GROUP BY a, b",
                    "PRE: EXPR: a",
                    "POST: EXPR: a",
                    "PRE: TABLE FACTOR: t",
                    "PRE: RELATION: t",
                    "POST: RELATION: t",
                    "POST: TABLE FACTOR: t",
                    "PRE: GROUP BY: GROUP BY a, b",
                    "PRE: EXPR: a",
                    "POST: EXPR: a",
                    "PRE: EXPR: b",
                    "POST: EXPR: b",
                    "POST: GROUP BY: GROUP BY a, b",
                    "POST: SELECT: SELECT a FROM t GROUP BY a, b",
                    "POST: QUERY: SELECT a FROM t GROUP BY a, b",
                    "POST: STATEMENT: SELECT a FROM t GROUP BY a, b",
                }
            ),
        };
        foreach (var (sql, expected) in tests)
        {
            var visitor = new TestVisitor();
            _ = DoVisit(sql, visitor);
            var actual = visitor.Visited.ToList();
            Assert.Equal(actual, expected);
        }
    }

    // [`TestVisitor`] is too slow to iterate over thousands of nodes
    private sealed class QuickVisitor : Visitor<ValueTuple>
    {
    }

    [Fact]
    public void overflow()
    {
        var cond = string.Join(" OR ", Enumerable.Range(0, 1000).Select(n => FormattableString.Invariant($"X = {n}")));
        var sql = $"SELECT x where {cond}";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var s = new Parser(dialect)
            .WithTokens(tokens)
            .ParseStatement();

        var visitor = new QuickVisitor();
        var flow = s.Visit(visitor);
        Assert.Equal(flow, ControlFlow<ValueTuple>.Continue);
    }

    // #[derive(Default)] struct IdentVisitor { idents: Vec<String> }
    private sealed class IdentVisitor : Visitor<ValueTuple>
    {
        public List<string> Idents { get; } = new List<string>();

        public override ControlFlow<ValueTuple> PreVisitIdent(Ident ident)
        {
            Idents.Add(ident.Value);
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void test_pre_visit_ident()
    {
        var visitor = new IdentVisitor();
        DoVisit("SELECT a, b FROM t", visitor);
        Assert.Equal(visitor.Idents, new[] { "a", "b", "t" });
    }

    // struct local de visits_function_clauses_in_source_order:
    // #[derive(Default)] struct ExprVisitor { idents: Vec<String> }
    private sealed class ExprVisitor : Visitor<ValueTuple>
    {
        public List<string> Idents { get; } = new List<string>();

        public override ControlFlow<ValueTuple> PreVisitExpr(Expr expr)
        {
            if (expr is Expr.Identifier { Value: var ident })
            {
                Idents.Add(ident.Value);
            }
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void visits_function_clauses_in_source_order()
    {
        var visitor = new ExprVisitor();
        DoVisit(
            "SELECT LISTAGG(value) WITHIN GROUP (ORDER BY order_key) FILTER (WHERE filter_key)",
            visitor);
        Assert.Equal(visitor.Idents, new[] { "value", "order_key", "filter_key" });
    }

    // #[derive(Default)] struct RelationVisitor { relations: Vec<String> }
    private sealed class RelationVisitor : Visitor<ValueTuple>
    {
        public List<string> Relations { get; } = new List<string>();

        public override ControlFlow<ValueTuple> PreVisitRelation(ObjectName relation)
        {
            Relations.Add(relation.ToString());
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void test_visit_create_view_name_as_relation()
    {
        var visitor = new RelationVisitor();
        DoVisit("CREATE VIEW db1.v AS SELECT * FROM t", visitor);
        Assert.Equal(visitor.Relations, new[] { "db1.v", "t" });
    }
}

// mod visit_mut_tests
public class VisitMutTests
{
    // #[derive(Default)] struct MutatorVisitor { index: u64 }
    private sealed class MutatorVisitor : VisitorMut<ValueTuple>
    {
        private ulong _index;

        public override ControlFlow<ValueTuple> PreVisitValue(ref ValueWithSpan value)
        {
            _index += 1;
            value = value with { Value = new Value.SingleQuotedString(FormattableString.Invariant($"REDACTED_{_index}")) };
            return ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PostVisitValue(ref ValueWithSpan value) => ControlFlow<ValueTuple>.Continue;
    }

    private static Statement DoVisitMut(string sql, VisitorMut<ValueTuple> visitor)
    {
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var s = new Parser(dialect)
            .WithTokens(tokens)
            .ParseStatement();

        var flow = s.Visit(visitor, out s);
        Assert.Equal(flow, ControlFlow<ValueTuple>.Continue);
        return s;
    }

    [Fact]
    public void test_value_redact()
    {
        var tests = new (string Sql, string Expected)[]
        {
            (
                "SELECT * FROM monthly_sales "
                    + "PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d) "
                    + "ORDER BY EMPID",
                "SELECT * FROM monthly_sales "
                    + "PIVOT(SUM(a.amount) FOR a.MONTH IN ('REDACTED_1', 'REDACTED_2', 'REDACTED_3', 'REDACTED_4')) AS p (c, d) "
                    + "ORDER BY EMPID"
            ),
        };

        foreach (var (sql, expected) in tests)
        {
            var visitor = new MutatorVisitor();
            var mutated = DoVisitMut(sql, visitor);
            Assert.Equal(mutated.ToString(), expected);
        }
    }

    // #[derive(Default)] struct IdentMutator;
    private sealed class IdentMutator : VisitorMut<ValueTuple>
    {
        public override ControlFlow<ValueTuple> PreVisitIdent(ref Ident ident)
        {
            // Rust: `ident.value = ident.value.to_uppercase();` (convencoes.md §10).
            ident = ident with { Value = RustString.ToUppercase(ident.Value) };
            return ControlFlow<ValueTuple>.Continue;
        }
    }

    [Fact]
    public void test_pre_visit_ident_mut()
    {
        var visitor = new IdentMutator();
        var mutated = DoVisitMut("SELECT a, b FROM t", visitor);
        Assert.Equal("SELECT A, B FROM T", mutated.ToString());
    }
}
