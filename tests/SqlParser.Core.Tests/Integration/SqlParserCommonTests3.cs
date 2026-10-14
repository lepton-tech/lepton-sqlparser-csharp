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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_common.rs (linhas 8210–12371).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C3.md):
// - Cada fatia de sqlparser_common.rs é uma classe própria (não partial); as funções-atalho do
//   arquivo Rust (`verified_stmt`, `parse_sql_statements`, `make_where_clause`...) são métodos
//   `private static` desta classe, na posição em que aparecem no Rust.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal/constante: o
//   literal vai na posição "expected" (regra xUnit2000, como em AstModTests/SpansTests).
//   `assert_eq!(None, x)` → `Assert.Null(x)`; `assert_eq!(x, false)` → `Assert.False(x)`.
// - `assert_eq!(v.len(), n)` → `Assert.Equal(n, v.Count)` (xUnit2013 suprimido, como em SpansTests).
// - `match stmt { Statement::X { .. } => .., _ => unreachable!() }` → `Assert.IsType<Statement.X>(stmt)`.
// - Funções aninhadas dentro de um teste Rust (`fn test_with`, `fn chk`...) viram funções locais
//   `static` com o nome mecânico (PascalCase).
#pragma warning disable xUnit2013
public class SqlParserCommonTests3
{
    [Fact]
    public void parse_recursive_cte()
    {
        var cteQuery = "SELECT 1 UNION ALL SELECT val + 1 FROM nums WHERE val < 10";
        var sql = $"WITH RECURSIVE nums (val) AS ({cteQuery}) SELECT * FROM nums";

        var cteQueryAst = VerifiedQuery(cteQuery);
        var query = VerifiedQuery(sql);

        var with = query.With!;
        Assert.True(with.Recursive);
        Assert.Equal(1, with.CteTables.Count);
        var expected = new Cte(
            Alias: new TableAlias(
                Explicit: false,
                Name: new Ident("nums", null, Span.Empty()),
                Columns: Sequence.Of(TableAliasColumnDef.FromName("val")),
                At: null),
            Query: cteQueryAst,
            From: null,
            Materialized: null,
            ClosingParenToken: AttachedToken.Empty());
        Assert.Equal(with.CteTables.First, expected);
    }

    [Fact]
    public void parse_cte_in_data_modification_statements()
    {
        switch (VerifiedStmt("WITH x AS (SELECT 1) UPDATE t SET bar = (SELECT * FROM x)"))
        {
            case Statement.Query { Value: var query }:
                Assert.Equal("WITH x AS (SELECT 1)", query.With!.ToString());
                Assert.True(query.Body is SetExpr.Update);
                break;
            case var other:
                throw new InvalidOperationException($"Expected: UPDATE, got: {other}");
        }

        switch (VerifiedStmt("WITH t (x) AS (SELECT 9) DELETE FROM q WHERE id IN (SELECT x FROM t)"))
        {
            case Statement.Query { Value: var query }:
                Assert.Equal("WITH t (x) AS (SELECT 9)", query.With!.ToString());
                Assert.True(query.Body is SetExpr.Delete);
                break;
            case var other:
                throw new InvalidOperationException($"Expected: DELETE, got: {other}");
        }

        switch (VerifiedStmt("WITH x AS (SELECT 42) INSERT INTO t SELECT foo FROM x"))
        {
            case Statement.Query { Value: var query }:
                Assert.Equal("WITH x AS (SELECT 42)", query.With!.ToString());
                Assert.True(query.Body is SetExpr.Insert);
                break;
            case var other:
                throw new InvalidOperationException($"Expected: INSERT, got: {other}");
        }
    }

    [Fact]
    public void parse_derived_tables()
    {
        var sql = "SELECT a.x, b.y FROM (SELECT x FROM foo) AS a CROSS JOIN (SELECT y FROM bar) AS b";
        _ = VerifiedOnlySelect(sql);
        //TODO: add assertions

        sql = "SELECT a.x, b.y " +
              "FROM (SELECT x FROM foo) AS a (x) " +
              "CROSS JOIN (SELECT y FROM bar) AS b (y)";
        _ = VerifiedOnlySelect(sql);
        //TODO: add assertions

        sql = "SELECT * FROM (((SELECT 1)))";
        _ = VerifiedOnlySelect(sql);
        // TODO: add assertions

        sql = "SELECT * FROM t NATURAL JOIN (((SELECT 1)))";
        _ = VerifiedOnlySelect(sql);
        // TODO: add assertions

        sql = "SELECT * FROM (((SELECT 1) UNION (SELECT 2)) AS t1 NATURAL JOIN t2)";
        var select = VerifiedOnlySelect(sql);
        var from = TestUtils.Only(select.From);
        Assert.Equal(
            from.Relation,
            new TableFactor.NestedJoin(
                TableWithJoins: new TableWithJoins(
                    Relation: new TableFactor.Derived(
                        Lateral: false,
                        Subquery: VerifiedQuery("(SELECT 1) UNION (SELECT 2)"),
                        Alias: TestUtils.TableAlias(true, "t1"),
                        Sample: null),
                    Joins: Sequence.Of(new Join(
                        Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t2")))),
                        Global: false,
                        JoinOperator: new JoinOperator.Join(new JoinConstraint.Natural())))),
                Alias: null));
    }

    [Fact]
    public void parse_union_except_intersect_minus()
    {
        // TODO: add assertions
        VerifiedStmt("SELECT 1 UNION SELECT 2");
        VerifiedStmt("SELECT 1 UNION ALL SELECT 2");
        VerifiedStmt("SELECT 1 UNION DISTINCT SELECT 1");
        VerifiedStmt("SELECT 1 EXCEPT SELECT 2");
        VerifiedStmt("SELECT 1 EXCEPT ALL SELECT 2");
        VerifiedStmt("SELECT 1 EXCEPT DISTINCT SELECT 1");
        VerifiedStmt("SELECT 1 INTERSECT SELECT 2");
        VerifiedStmt("SELECT 1 INTERSECT ALL SELECT 2");
        VerifiedStmt("SELECT 1 INTERSECT DISTINCT SELECT 1");
        VerifiedStmt("SELECT 1 UNION SELECT 2 UNION SELECT 3");
        VerifiedStmt("SELECT 1 EXCEPT SELECT 2 UNION SELECT 3"); // Union[Except[1,2], 3]
        VerifiedStmt("SELECT 1 INTERSECT (SELECT 2 EXCEPT SELECT 3)");
        VerifiedStmt("WITH cte AS (SELECT 1 AS foo) (SELECT foo FROM cte ORDER BY 1 LIMIT 1)");
        VerifiedStmt("SELECT 1 UNION (SELECT 2 ORDER BY 1 LIMIT 1)");
        VerifiedStmt("SELECT 1 UNION SELECT 2 INTERSECT SELECT 3"); // Union[1, Intersect[2,3]]
        VerifiedStmt("SELECT foo FROM tab UNION SELECT bar FROM TAB");
        VerifiedStmt("(SELECT * FROM new EXCEPT SELECT * FROM old) UNION ALL (SELECT * FROM old EXCEPT SELECT * FROM new) ORDER BY 1");
        VerifiedStmt("(SELECT * FROM new EXCEPT DISTINCT SELECT * FROM old) UNION DISTINCT (SELECT * FROM old EXCEPT DISTINCT SELECT * FROM new) ORDER BY 1");
        VerifiedStmt("SELECT 1 AS x, 2 AS y EXCEPT BY NAME SELECT 9 AS y, 8 AS x");
        VerifiedStmt("SELECT 1 AS x, 2 AS y EXCEPT ALL BY NAME SELECT 9 AS y, 8 AS x");
        VerifiedStmt("SELECT 1 AS x, 2 AS y EXCEPT DISTINCT BY NAME SELECT 9 AS y, 8 AS x");
        VerifiedStmt("SELECT 1 AS x, 2 AS y INTERSECT BY NAME SELECT 9 AS y, 8 AS x");
        VerifiedStmt("SELECT 1 AS x, 2 AS y INTERSECT ALL BY NAME SELECT 9 AS y, 8 AS x");
        VerifiedStmt("SELECT 1 AS x, 2 AS y INTERSECT DISTINCT BY NAME SELECT 9 AS y, 8 AS x");

        // Dialects that support `MINUS` as column identifier
        // do not support `MINUS` as a set operator.
        var dialects = TestUtils.AllDialectsWhere(d => !d.IsColumnAlias(Keyword.MINUS, new Parser(d)));
        dialects.VerifiedStmt("SELECT 1 MINUS SELECT 2");
        dialects.VerifiedStmt("SELECT 1 MINUS ALL SELECT 2");
        dialects.VerifiedStmt("SELECT 1 MINUS DISTINCT SELECT 1");
    }

    [Fact]
    public void parse_values()
    {
        VerifiedStmt("SELECT * FROM (VALUES (1), (2), (3))");
        VerifiedStmt("SELECT * FROM (VALUES (1), (2), (3)), (VALUES (1, 2, 3))");
        VerifiedStmt("SELECT * FROM (VALUES (1)) UNION VALUES (1)");
        VerifiedStmt("SELECT * FROM (VALUES ROW(1, NULL, 'a'), ROW(2, NULL, 'b')) AS t (a, b, c)");
    }

    [Fact]
    public void parse_multiple_statements()
    {
        static void TestWith(string sql1, string sql2Kw, string sql2Rest)
        {
            // Check that a string consisting of two statements delimited by a semicolon
            // parses the same as both statements individually:
            var res = ParseSqlStatements(sql1 + ";" + sql2Kw + sql2Rest);
            Assert.Equal(
                Sequence.Of(
                    OneStatementParsesTo(sql1, ""),
                    OneStatementParsesTo(sql2Kw + sql2Rest, "")),
                res);
            // Check that extra semicolon at the end is stripped by normalization:
            OneStatementParsesTo(sql1 + ";", sql1);
            // Check that forgetting the semicolon results in an error:
            var ex = Assert.Throws<ParserException>(() => ParseSqlStatements(sql1 + " " + sql2Kw + sql2Rest));
            Assert.Equal(
                new ParserError.ParserErrorValue("Expected: end of statement, found: " + sql2Kw),
                ex.Error);
        }
        TestWith("SELECT foo", "SELECT", " bar");
        // ensure that SELECT/WITH is not parsed as a table or column alias if ';'
        // separating the statements is omitted:
        TestWith("SELECT foo FROM baz", "SELECT", " bar");
        TestWith("SELECT foo", "WITH", " cte AS (SELECT 1 AS s) SELECT bar");
        TestWith(
            "SELECT foo FROM baz",
            "WITH",
            " cte AS (SELECT 1 AS s) SELECT bar");
        TestWith("DELETE FROM foo", "SELECT", " bar");
        TestWith("INSERT INTO foo VALUES (1)", "SELECT", " bar");
        // Since MySQL supports the `CREATE TABLE SELECT` syntax, this needs to be handled separately
        var res = ParseSqlStatements("CREATE TABLE foo (baz INT); SELECT bar");
        Assert.Equal(
            Sequence.Of(
                OneStatementParsesTo("CREATE TABLE foo (baz INT)", ""),
                OneStatementParsesTo("SELECT bar", "")),
            res);
        // Check that extra semicolon at the end is stripped by normalization:
        OneStatementParsesTo("CREATE TABLE foo (baz INT);", "CREATE TABLE foo (baz INT)");
        // Make sure that empty statements do not cause an error:
        var res2 = ParseSqlStatements(";;");
        Assert.Equal(0, res2.Count);
    }

    [Fact]
    public void parse_scalar_subqueries()
    {
        var sql = "(SELECT 1) + (SELECT 2)";
        Assert.True(VerifiedExpr(sql) is Expr.BinaryOp { Op: BinaryOperator.Plus });
    }

    [Fact]
    public void parse_substring()
    {
        VerifiedStmt("SELECT SUBSTRING('1')");
        VerifiedStmt("SELECT SUBSTRING('1' FROM 1)");
        VerifiedStmt("SELECT SUBSTRING('1' FROM 1 FOR 3)");
        VerifiedStmt("SELECT SUBSTRING('1', 1, 3)");
        VerifiedStmt("SELECT SUBSTRING('1', 1)");
        VerifiedStmt("SELECT SUBSTRING('1' FOR 3)");
        VerifiedStmt("SELECT SUBSTRING('foo' FROM 1 FOR 2) FROM t");
        VerifiedStmt("SELECT SUBSTR('foo' FROM 1 FOR 2) FROM t");
        VerifiedStmt("SELECT SUBSTR('foo', 1, 2) FROM t");
    }

    [Fact]
    public void parse_overlay()
    {
        OneStatementParsesTo(
            "SELECT OVERLAY('abccccde' PLACING 'abc' FROM 3)",
            "SELECT OVERLAY('abccccde' PLACING 'abc' FROM 3)");
        OneStatementParsesTo(
            "SELECT OVERLAY('abccccde' PLACING 'abc' FROM 3 FOR 12)",
            "SELECT OVERLAY('abccccde' PLACING 'abc' FROM 3 FOR 12)");
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: PLACING, found: FROM"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT OVERLAY('abccccde' FROM 3)")).Error);

        var sql = "SELECT OVERLAY('abcdef' PLACING name FROM 3 FOR id + 1) FROM CUSTOMERS";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Overlay(
                Expr: new Expr.Value(new Value.SingleQuotedString("abcdef").WithEmptySpan()),
                OverlayWhat: new Expr.Identifier(new Ident("name")),
                OverlayFrom: Expr.MakeValue(TestUtils.Number("3")),
                OverlayFor: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("id")),
                    Op: new BinaryOperator.Plus(),
                    Right: Expr.MakeValue(TestUtils.Number("1")))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_trim()
    {
        OneStatementParsesTo(
            "SELECT TRIM(BOTH 'xyz' FROM 'xyzfooxyz')",
            "SELECT TRIM(BOTH 'xyz' FROM 'xyzfooxyz')");

        OneStatementParsesTo(
            "SELECT TRIM(LEADING 'xyz' FROM 'xyzfooxyz')",
            "SELECT TRIM(LEADING 'xyz' FROM 'xyzfooxyz')");

        OneStatementParsesTo(
            "SELECT TRIM(TRAILING 'xyz' FROM 'xyzfooxyz')",
            "SELECT TRIM(TRAILING 'xyz' FROM 'xyzfooxyz')");

        OneStatementParsesTo(
            "SELECT TRIM('xyz' FROM 'xyzfooxyz')",
            "SELECT TRIM('xyz' FROM 'xyzfooxyz')");
        OneStatementParsesTo("SELECT TRIM('   foo   ')", "SELECT TRIM('   foo   ')");
        OneStatementParsesTo(
            "SELECT TRIM(LEADING '   foo   ')",
            "SELECT TRIM(LEADING '   foo   ')");

        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: 'xyz'"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT TRIM(FOO 'xyz' FROM 'xyzfooxyz')")).Error);

        // dialects that support comma-separated TRIM syntax
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsCommaSeparatedTrim);

        var sql = "SELECT TRIM('  xyz  ', ' ')";
        var select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Trim(
                Expr: new Expr.Value(new Value.SingleQuotedString("  xyz  ").WithEmptySpan()),
                TrimWhere: null,
                TrimWhat: null,
                TrimCharacters: Sequence.Of<Expr>(new Expr.Value(new Value.SingleQuotedString(" ").WithEmptySpan()))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT TRIM('xyz', 'a')";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Trim(
                Expr: new Expr.Value(new Value.SingleQuotedString("xyz").WithEmptySpan()),
                TrimWhere: null,
                TrimWhat: null,
                TrimCharacters: Sequence.Of<Expr>(new Expr.Value(new Value.SingleQuotedString("a").WithEmptySpan()))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        // dialects without comma-style TRIM syntax should fail
        var unsupportedDialects = TestUtils.AllDialectsWhere(d => !d.SupportsCommaSeparatedTrim);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: ,"),
            Assert.Throws<ParserException>(() => unsupportedDialects.ParseSqlStatements("SELECT TRIM('xyz', 'a')")).Error);
    }

    [Fact]
    public void parse_exists_subquery()
    {
        var expectedInner = VerifiedQuery("SELECT 1");
        var sql = "SELECT * FROM t WHERE EXISTS (SELECT 1)";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Exists(
                Negated: false,
                Subquery: expectedInner),
            select.Selection!);

        sql = "SELECT * FROM t WHERE NOT EXISTS (SELECT 1)";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Exists(
                Negated: true,
                Subquery: expectedInner),
            select.Selection!);

        VerifiedStmt("SELECT * FROM t WHERE EXISTS (WITH u AS (SELECT 1) SELECT * FROM u)");
        VerifiedStmt("SELECT EXISTS (SELECT 1)");

        var ex = Assert.Throws<ParserException>(() => TestUtils.AllDialectsExcept(d => d is DatabricksDialect)
            .ParseSqlStatements("SELECT EXISTS ("));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: EOF"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialectsExcept(d => d is DatabricksDialect)
            .ParseSqlStatements("SELECT EXISTS (NULL)"));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: NULL"),
            ex.Error);
    }

    [Fact]
    public void parse_create_database()
    {
        var sql = "CREATE DATABASE mydb";
        var createDatabase = Assert.IsType<Statement.CreateDatabase>(VerifiedStmt(sql));
        Assert.Equal("mydb", createDatabase.DbName.ToString());
        Assert.False(createDatabase.IfNotExists);
        Assert.Null(createDatabase.Location);
        Assert.Null(createDatabase.ManagedLocation);
        Assert.Null(createDatabase.CloneValue);

        sql = "CREATE DATABASE mydb CLONE otherdb";
        createDatabase = Assert.IsType<Statement.CreateDatabase>(VerifiedStmt(sql));
        Assert.Equal("mydb", createDatabase.DbName.ToString());
        Assert.False(createDatabase.IfNotExists);
        Assert.Null(createDatabase.Location);
        Assert.Null(createDatabase.ManagedLocation);
        Assert.Equal(
            ObjectName.From(Sequence.Of(new Ident("otherdb"))),
            createDatabase.CloneValue);
    }

    [Fact]
    public void parse_create_database_ine()
    {
        var sql = "CREATE DATABASE IF NOT EXISTS mydb";
        var createDatabase = Assert.IsType<Statement.CreateDatabase>(VerifiedStmt(sql));
        Assert.Equal("mydb", createDatabase.DbName.ToString());
        Assert.True(createDatabase.IfNotExists);
        Assert.Null(createDatabase.Location);
        Assert.Null(createDatabase.ManagedLocation);
        Assert.Null(createDatabase.CloneValue);
    }

    [Fact]
    public void parse_drop_database()
    {
        var sql = "DROP DATABASE mycatalog.mydb";
        var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        Assert.Equal(
            new List<string> { "mycatalog.mydb" },
            drop.Names.Select(n => n.ToString()).ToList());
        Assert.Equal<ObjectType>(new ObjectType.Database(), drop.ObjectType);
        Assert.False(drop.IfExists);
    }

    [Fact]
    public void parse_drop_database_if_exists()
    {
        var sql = "DROP DATABASE IF EXISTS mydb";
        var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        Assert.Equal<ObjectType>(new ObjectType.Database(), drop.ObjectType);
        Assert.True(drop.IfExists);
    }

    [Fact]
    public void parse_create_view()
    {
        var sql = "CREATE VIEW myschema.myview AS SELECT foo FROM bar";
        var createView = Assert.IsType<Statement.CreateView>(VerifiedStmt(sql)).Value;
        Assert.False(createView.OrAlter);
        Assert.Equal("myschema.myview", createView.Name.ToString());
        Assert.Equal(Sequence<ViewColumnDef>.Empty, createView.Columns);
        Assert.Equal("SELECT foo FROM bar", createView.Query.ToString());
        Assert.False(createView.Materialized);
        Assert.False(createView.OrReplace);
        Assert.Equal<CreateTableOptions>(createView.Options, new CreateTableOptions.None());
        Assert.Equal(createView.ClusterBy, Sequence<Ident>.Empty);
        Assert.Null(createView.Comment);
        Assert.False(createView.WithNoSchemaBinding); // late_binding
        Assert.False(createView.IfNotExists);
        Assert.False(createView.Temporary);
        Assert.Null(createView.To);
        Assert.Null(createView.Params);

        _ = VerifiedStmt("CREATE OR ALTER VIEW v AS SELECT 1");
    }

    [Fact]
    public void parse_create_view_with_options()
    {
        var sql = "CREATE VIEW v WITH (foo = 'bar', a = 123) AS SELECT 1";
        var createView = Assert.IsType<Statement.CreateView>(VerifiedStmt(sql)).Value;
        Assert.Equal<CreateTableOptions>(
            new CreateTableOptions.With(Sequence.Of<SqlOption>(
                new SqlOption.KeyValue(
                    Key: new Ident("foo"),
                    Value: new Expr.Value(new Value.SingleQuotedString("bar").WithEmptySpan())),
                new SqlOption.KeyValue(
                    Key: new Ident("a"),
                    Value: Expr.MakeValue(TestUtils.Number("123"))))),
            createView.Options);
    }

    [Fact]
    public void parse_create_view_with_columns()
    {
        var sql = "CREATE VIEW v (has, cols) AS SELECT 1, 2";
        // TODO: why does this fail for ClickHouseDialect? (#1449)
        // match all_dialects().verified_stmt(sql) {
        var createView = Assert.IsType<Statement.CreateView>(
            TestUtils.AllDialectsExcept(d => d is ClickHouseDialect).VerifiedStmt(sql)).Value;
        var orAlter = createView.OrAlter;
        var name = createView.Name;
        var columns = createView.Columns;
        var orReplace = createView.OrReplace;
        var options = createView.Options;
        var query = createView.Query;
        var materialized = createView.Materialized;
        var clusterBy = createView.ClusterBy;
        var comment = createView.Comment;
        var lateBinding = createView.WithNoSchemaBinding;
        var ifNotExists = createView.IfNotExists;
        var temporary = createView.Temporary;
        var to = createView.To;
        var @params = createView.Params;
        Assert.False(orAlter);
        Assert.Equal("v", name.ToString());
        Assert.Equal(
            columns,
            new[] { new Ident("has"), new Ident("cols") }
                .Select(n => new ViewColumnDef(Name: n, DataType: null, Options: null))
                .ToSequence());
        Assert.Equal<CreateTableOptions>(options, new CreateTableOptions.None());
        Assert.Equal("SELECT 1, 2", query.ToString());
        Assert.False(materialized);
        Assert.False(orReplace);
        Assert.Equal(clusterBy, Sequence<Ident>.Empty);
        Assert.Null(comment);
        Assert.False(lateBinding);
        Assert.False(ifNotExists);
        Assert.False(temporary);
        Assert.Null(to);
        Assert.Null(@params);
    }

    [Fact]
    public void parse_create_view_temporary()
    {
        var sql = "CREATE TEMPORARY VIEW myschema.myview AS SELECT foo FROM bar";
        var createView = Assert.IsType<Statement.CreateView>(VerifiedStmt(sql)).Value;
        Assert.False(createView.OrAlter);
        Assert.Equal("myschema.myview", createView.Name.ToString());
        Assert.Equal(Sequence<ViewColumnDef>.Empty, createView.Columns);
        Assert.Equal("SELECT foo FROM bar", createView.Query.ToString());
        Assert.False(createView.Materialized);
        Assert.False(createView.OrReplace);
        Assert.Equal<CreateTableOptions>(createView.Options, new CreateTableOptions.None());
        Assert.Equal(createView.ClusterBy, Sequence<Ident>.Empty);
        Assert.Null(createView.Comment);
        Assert.False(createView.WithNoSchemaBinding); // late_binding
        Assert.False(createView.IfNotExists);
        Assert.True(createView.Temporary);
        Assert.Null(createView.To);
        Assert.Null(createView.Params);
    }

    [Fact]
    public void parse_create_or_replace_view()
    {
        var sql = "CREATE OR REPLACE VIEW v AS SELECT 1";
        var createView = Assert.IsType<Statement.CreateView>(VerifiedStmt(sql)).Value;
        Assert.False(createView.OrAlter);
        Assert.Equal("v", createView.Name.ToString());
        Assert.Equal(createView.Columns, Sequence<ViewColumnDef>.Empty);
        Assert.Equal<CreateTableOptions>(createView.Options, new CreateTableOptions.None());
        Assert.Equal("SELECT 1", createView.Query.ToString());
        Assert.False(createView.Materialized);
        Assert.True(createView.OrReplace);
        Assert.Equal(createView.ClusterBy, Sequence<Ident>.Empty);
        Assert.Null(createView.Comment);
        Assert.False(createView.WithNoSchemaBinding); // late_binding
        Assert.False(createView.IfNotExists);
        Assert.False(createView.Temporary);
        Assert.Null(createView.To);
        Assert.Null(createView.Params);
    }

    [Fact]
    public void parse_create_or_replace_materialized_view()
    {
        // Supported in BigQuery (Beta)
        // https://cloud.google.com/bigquery/docs/materialized-views-intro
        // and Snowflake:
        // https://docs.snowflake.com/en/sql-reference/sql/create-materialized-view.html
        var sql = "CREATE OR REPLACE MATERIALIZED VIEW v AS SELECT 1";
        var createView = Assert.IsType<Statement.CreateView>(VerifiedStmt(sql)).Value;
        Assert.False(createView.OrAlter);
        Assert.Equal("v", createView.Name.ToString());
        Assert.Equal(createView.Columns, Sequence<ViewColumnDef>.Empty);
        Assert.Equal<CreateTableOptions>(createView.Options, new CreateTableOptions.None());
        Assert.Equal("SELECT 1", createView.Query.ToString());
        Assert.True(createView.Materialized);
        Assert.True(createView.OrReplace);
        Assert.Equal(createView.ClusterBy, Sequence<Ident>.Empty);
        Assert.Null(createView.Comment);
        Assert.False(createView.WithNoSchemaBinding); // late_binding
        Assert.False(createView.IfNotExists);
        Assert.False(createView.Temporary);
        Assert.Null(createView.To);
        Assert.Null(createView.Params);
    }

    [Fact]
    public void parse_create_materialized_view()
    {
        var sql = "CREATE MATERIALIZED VIEW myschema.myview AS SELECT foo FROM bar";
        var createView = Assert.IsType<Statement.CreateView>(VerifiedStmt(sql)).Value;
        Assert.False(createView.OrAlter);
        Assert.Equal("myschema.myview", createView.Name.ToString());
        Assert.Equal(Sequence<ViewColumnDef>.Empty, createView.Columns);
        Assert.Equal("SELECT foo FROM bar", createView.Query.ToString());
        Assert.True(createView.Materialized);
        Assert.Equal<CreateTableOptions>(createView.Options, new CreateTableOptions.None());
        Assert.False(createView.OrReplace);
        Assert.Equal(createView.ClusterBy, Sequence<Ident>.Empty);
        Assert.Null(createView.Comment);
        Assert.False(createView.WithNoSchemaBinding); // late_binding
        Assert.False(createView.IfNotExists);
        Assert.False(createView.Temporary);
        Assert.Null(createView.To);
        Assert.Null(createView.Params);
    }

    [Fact]
    public void parse_create_materialized_view_with_cluster_by()
    {
        var sql = "CREATE MATERIALIZED VIEW myschema.myview CLUSTER BY (foo) AS SELECT foo FROM bar";
        var createView = Assert.IsType<Statement.CreateView>(VerifiedStmt(sql)).Value;
        Assert.False(createView.OrAlter);
        Assert.Equal("myschema.myview", createView.Name.ToString());
        Assert.Equal(Sequence<ViewColumnDef>.Empty, createView.Columns);
        Assert.Equal("SELECT foo FROM bar", createView.Query.ToString());
        Assert.True(createView.Materialized);
        Assert.Equal<CreateTableOptions>(createView.Options, new CreateTableOptions.None());
        Assert.False(createView.OrReplace);
        Assert.Equal(createView.ClusterBy, Sequence.Of(new Ident("foo")));
        Assert.Null(createView.Comment);
        Assert.False(createView.WithNoSchemaBinding); // late_binding
        Assert.False(createView.IfNotExists);
        Assert.False(createView.Temporary);
        Assert.Null(createView.To);
        Assert.Null(createView.Params);
    }

    [Fact]
    public void parse_drop_table()
    {
        var sql = "DROP TABLE foo";
        var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        Assert.False(drop.IfExists);
        Assert.Equal<ObjectType>(new ObjectType.Table(), drop.ObjectType);
        Assert.Equal(
            new List<string> { "foo" },
            drop.Names.Select(n => n.ToString()).ToList());
        Assert.False(drop.Cascade);
        Assert.False(drop.Temporary);

        sql = "DROP TABLE IF EXISTS foo, bar CASCADE";
        drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        Assert.True(drop.IfExists);
        Assert.Equal<ObjectType>(new ObjectType.Table(), drop.ObjectType);
        Assert.Equal(
            new List<string> { "foo", "bar" },
            drop.Names.Select(n => n.ToString()).ToList());
        Assert.True(drop.Cascade);
        Assert.False(drop.Temporary);

        sql = "DROP TABLE";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: identifier, found: EOF"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);

        sql = "DROP TABLE IF EXISTS foo, bar CASCADE RESTRICT";
        Assert.Equal(
            new ParserError.ParserErrorValue("Cannot specify both CASCADE and RESTRICT in DROP"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_drop_view()
    {
        var sql = "DROP VIEW myschema.myview";
        var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        Assert.Equal(
            new List<string> { "myschema.myview" },
            drop.Names.Select(n => n.ToString()).ToList());
        Assert.Equal<ObjectType>(new ObjectType.View(), drop.ObjectType);

        VerifiedStmt("DROP MATERIALIZED VIEW a.b.c");
        VerifiedStmt("DROP MATERIALIZED VIEW IF EXISTS a.b.c");
    }

    [Fact]
    public void parse_drop_user()
    {
        var sql = "DROP USER u1";
        var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        Assert.Equal(
            new List<string> { "u1" },
            drop.Names.Select(n => n.ToString()).ToList());
        Assert.Equal<ObjectType>(new ObjectType.User(), drop.ObjectType);
        VerifiedStmt("DROP USER IF EXISTS u1");
    }

    [Fact]
    public void parse_create_warehouse()
    {
        VerifiedStmt("CREATE WAREHOUSE my_wh");
        VerifiedStmt("CREATE OR REPLACE WAREHOUSE IF NOT EXISTS my_wh");
        VerifiedStmt("CREATE WAREHOUSE my_wh WAREHOUSE_SIZE='XSMALL' AUTO_SUSPEND=60");
        OneStatementParsesTo(
            "CREATE WAREHOUSE my_wh WITH WAREHOUSE_SIZE = 'XSMALL' AUTO_SUSPEND = 60",
            "CREATE WAREHOUSE my_wh WAREHOUSE_SIZE='XSMALL' AUTO_SUSPEND=60");
        VerifiedStmt("DROP WAREHOUSE my_wh");
        VerifiedStmt("DROP WAREHOUSE IF EXISTS my_wh");
    }

    [Fact]
    public void parse_invalid_subquery_without_parens()
    {
        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT SELECT 1 FROM bar WHERE 1=1 FROM baz"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: 1"),
            ex.Error);
    }

    [Fact]
    public void parse_offset()
    {
        // Dialects that support `OFFSET` as column identifiers
        // don't support this syntax.
        var dialects =
            TestUtils.AllDialectsWhere(d => !d.IsColumnAlias(Keyword.OFFSET, new Parser(d)));

        LimitClause? expectedLimitClause = new LimitClause.LimitOffset(
            Limit: null,
            Offset: new Offset(
                Value: Expr.MakeValue(TestUtils.Number("2")),
                Rows: new OffsetRows.Rows()),
            LimitBy: Sequence<Expr>.Empty);
        var ast = dialects.VerifiedQuery("SELECT foo FROM bar OFFSET 2 ROWS");
        Assert.Equal(ast.LimitClause, expectedLimitClause);
        ast = dialects.VerifiedQuery("SELECT foo FROM bar WHERE foo = 4 OFFSET 2 ROWS");
        Assert.Equal(ast.LimitClause, expectedLimitClause);
        ast = dialects.VerifiedQuery("SELECT foo FROM bar ORDER BY baz OFFSET 2 ROWS");
        Assert.Equal(ast.LimitClause, expectedLimitClause);
        ast =
            dialects.VerifiedQuery("SELECT foo FROM bar WHERE foo = 4 ORDER BY baz OFFSET 2 ROWS");
        Assert.Equal(ast.LimitClause, expectedLimitClause);
        ast =
            dialects.VerifiedQuery("SELECT foo FROM (SELECT * FROM bar OFFSET 2 ROWS) OFFSET 2 ROWS");
        Assert.Equal(ast.LimitClause, expectedLimitClause);
        switch (ast.Body)
        {
            case SetExpr.Select { Value: var s }:
                switch (TestUtils.Only(s.From).Relation)
                {
                    case TableFactor.Derived { Subquery: var subquery }:
                        Assert.Equal(subquery.LimitClause, expectedLimitClause);
                        break;
                    default:
                        throw new InvalidOperationException("Test broke");
                }
                break;
            default:
                throw new InvalidOperationException("Test broke");
        }
        var expectedLimitClause2 = new LimitClause.LimitOffset(
            Limit: null,
            Offset: new Offset(
                Value: Expr.MakeValue(TestUtils.Number("0")),
                Rows: new OffsetRows.Rows()),
            LimitBy: Sequence<Expr>.Empty);
        ast = dialects.VerifiedQuery("SELECT 'foo' OFFSET 0 ROWS");
        Assert.Equal<LimitClause?>(ast.LimitClause, expectedLimitClause2);
        var expectedLimitClause3 = new LimitClause.LimitOffset(
            Limit: null,
            Offset: new Offset(
                Value: Expr.MakeValue(TestUtils.Number("1")),
                Rows: new OffsetRows.Row()),
            LimitBy: Sequence<Expr>.Empty);
        ast = dialects.VerifiedQuery("SELECT 'foo' OFFSET 1 ROW");
        Assert.Equal<LimitClause?>(ast.LimitClause, expectedLimitClause3);
        var expectedLimitClause4 = new LimitClause.LimitOffset(
            Limit: null,
            Offset: new Offset(
                Value: Expr.MakeValue(TestUtils.Number("2")),
                Rows: new OffsetRows.None()),
            LimitBy: Sequence<Expr>.Empty);
        ast = dialects.VerifiedQuery("SELECT 'foo' OFFSET 2");
        Assert.Equal<LimitClause?>(ast.LimitClause, expectedLimitClause4);
    }

    [Fact]
    public void parse_fetch()
    {
        Fetch? fetchFirstTwoRowsOnly = new Fetch(
            WithTies: false,
            Percent: false,
            Quantity: Expr.MakeValue(TestUtils.Number("2")));
        var ast = VerifiedQuery("SELECT foo FROM bar FETCH FIRST 2 ROWS ONLY");
        Assert.Equal(ast.Fetch, fetchFirstTwoRowsOnly);
        ast = VerifiedQuery("SELECT 'foo' FETCH FIRST 2 ROWS ONLY");
        Assert.Equal(ast.Fetch, fetchFirstTwoRowsOnly);
        ast = VerifiedQuery("SELECT foo FROM bar FETCH FIRST ROWS ONLY");
        Assert.Equal(
            ast.Fetch,
            new Fetch(
                WithTies: false,
                Percent: false,
                Quantity: null));
        ast = VerifiedQuery("SELECT foo FROM bar WHERE foo = 4 FETCH FIRST 2 ROWS ONLY");
        Assert.Equal(ast.Fetch, fetchFirstTwoRowsOnly);
        ast = VerifiedQuery("SELECT foo FROM bar ORDER BY baz FETCH FIRST 2 ROWS ONLY");
        Assert.Equal(ast.Fetch, fetchFirstTwoRowsOnly);
        ast = VerifiedQuery(
            "SELECT foo FROM bar WHERE foo = 4 ORDER BY baz FETCH FIRST 2 ROWS WITH TIES");
        Assert.Equal(
            ast.Fetch,
            new Fetch(
                WithTies: true,
                Percent: false,
                Quantity: Expr.MakeValue(TestUtils.Number("2"))));
        ast = VerifiedQuery("SELECT foo FROM bar FETCH FIRST 50 PERCENT ROWS ONLY");
        Assert.Equal(
            ast.Fetch,
            new Fetch(
                WithTies: false,
                Percent: true,
                Quantity: Expr.MakeValue(TestUtils.Number("50"))));
        ast = VerifiedQuery(
            "SELECT foo FROM bar WHERE foo = 4 ORDER BY baz OFFSET 2 ROWS FETCH FIRST 2 ROWS ONLY");
        LimitClause? expectedLimitClause = new LimitClause.LimitOffset(
            Limit: null,
            Offset: new Offset(
                Value: Expr.MakeValue(TestUtils.Number("2")),
                Rows: new OffsetRows.Rows()),
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal(ast.LimitClause, expectedLimitClause);
        Assert.Equal(ast.Fetch, fetchFirstTwoRowsOnly);
        ast = VerifiedQuery(
            "SELECT foo FROM (SELECT * FROM bar FETCH FIRST 2 ROWS ONLY) FETCH FIRST 2 ROWS ONLY");
        Assert.Equal(ast.Fetch, fetchFirstTwoRowsOnly);
        switch (ast.Body)
        {
            case SetExpr.Select { Value: var s }:
                switch (TestUtils.Only(s.From).Relation)
                {
                    case TableFactor.Derived { Subquery: var subquery }:
                        Assert.Equal(subquery.Fetch, fetchFirstTwoRowsOnly);
                        break;
                    default:
                        throw new InvalidOperationException("Test broke");
                }
                break;
            default:
                throw new InvalidOperationException("Test broke");
        }
        ast = VerifiedQuery("SELECT foo FROM (SELECT * FROM bar OFFSET 2 ROWS FETCH FIRST 2 ROWS ONLY) OFFSET 2 ROWS FETCH FIRST 2 ROWS ONLY");
        expectedLimitClause = new LimitClause.LimitOffset(
            Limit: null,
            Offset: new Offset(
                Value: Expr.MakeValue(TestUtils.Number("2")),
                Rows: new OffsetRows.Rows()),
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal(ast.LimitClause, expectedLimitClause);
        Assert.Equal(ast.Fetch, fetchFirstTwoRowsOnly);
        switch (ast.Body)
        {
            case SetExpr.Select { Value: var s }:
                switch (TestUtils.Only(s.From).Relation)
                {
                    case TableFactor.Derived { Subquery: var subquery }:
                        Assert.Equal(subquery.LimitClause, expectedLimitClause);
                        Assert.Equal(subquery.Fetch, fetchFirstTwoRowsOnly);
                        break;
                    default:
                        throw new InvalidOperationException("Test broke");
                }
                break;
            default:
                throw new InvalidOperationException("Test broke");
        }
    }

    [Fact]
    public void parse_fetch_variations()
    {
        OneStatementParsesTo(
            "SELECT foo FROM bar FETCH FIRST 10 ROW ONLY",
            "SELECT foo FROM bar FETCH FIRST 10 ROWS ONLY");
        OneStatementParsesTo(
            "SELECT foo FROM bar FETCH NEXT 10 ROW ONLY",
            "SELECT foo FROM bar FETCH FIRST 10 ROWS ONLY");
        OneStatementParsesTo(
            "SELECT foo FROM bar FETCH NEXT 10 ROWS WITH TIES",
            "SELECT foo FROM bar FETCH FIRST 10 ROWS WITH TIES");
        OneStatementParsesTo(
            "SELECT foo FROM bar FETCH NEXT ROWS WITH TIES",
            "SELECT foo FROM bar FETCH FIRST ROWS WITH TIES");
        OneStatementParsesTo(
            "SELECT foo FROM bar FETCH FIRST ROWS ONLY",
            "SELECT foo FROM bar FETCH FIRST ROWS ONLY");
    }

    [Fact]
    public void lateral_derived()
    {
        static void Chk(bool lateralIn)
        {
            var lateralStr = lateralIn ? "LATERAL " : "";
            var sql =
                $"SELECT * FROM customer LEFT JOIN {lateralStr}" +
                "(SELECT * FROM orders WHERE orders.customer = customer.id LIMIT 3) AS orders ON 1";
            var select = VerifiedOnlySelect(sql);
            var from = TestUtils.Only(select.From);
            Assert.Equal(1, from.Joins.Count);
            var join = from.Joins[0];
            Assert.Equal(
                join.JoinOperator,
                new JoinOperator.Left(new JoinConstraint.On(new Expr.Value(
                    TestUtils.Number("1").WithEmptySpan()))));
            if (join.Relation is TableFactor.Derived { Alias: { } alias } derived)
            {
                Assert.Equal(lateralIn, derived.Lateral);
                Assert.Equal(new Ident("orders"), alias.Name);
                Assert.Equal(
                    "SELECT * FROM orders WHERE orders.customer = customer.id LIMIT 3",
                    derived.Subquery.ToString());
            }
            else
            {
                throw new InvalidOperationException("unreachable");
            }
        }
        Chk(false);
        Chk(true);

        var sql = "SELECT * FROM LATERAL UNNEST ([10,20,30]) as numbers WITH OFFSET;";
        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements(sql));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: WITH"),
            ex.Error);

        sql = "SELECT * FROM a LEFT JOIN LATERAL (b CROSS JOIN c)";
        ex = Assert.Throws<ParserException>(() => ParseSqlStatements(sql));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: b"),
            ex.Error);
    }

    [Fact]
    public void lateral_function()
    {
        var sql = "SELECT * FROM customer LEFT JOIN LATERAL generate_series(1, customer.id)";
        var actualSelectOnly = VerifiedOnlySelect(sql);
        var expected = new Select(
            SelectToken: AttachedToken.Empty(),
            OptimizerHints: Sequence<OptimizerHint>.Empty,
            Distinct: null,
            SelectModifiers: null,
            Top: null,
            Projection: Sequence.Of<SelectItem>(new SelectItem.Wildcard(WildcardAdditionalOptions.Default())),
            Exclude: null,
            TopBeforeDistinct: false,
            Into: null,
            From: Sequence.Of(new TableWithJoins(
                Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("customer", null, Span.Empty())))),
                Joins: Sequence.Of(new Join(
                    Relation: new TableFactor.Function(
                        Lateral: true,
                        Name: ObjectName.From(Sequence.Of(new Ident("generate_series"))),
                        Args: Sequence.Of<FunctionArg>(
                            new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                                TestUtils.Number("1").WithEmptySpan()))),
                            new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.CompoundIdentifier(
                                Sequence.Of(new Ident("customer"), new Ident("id")))))),
                        WithOrdinality: false,
                        Alias: null),
                    Global: false,
                    JoinOperator: new JoinOperator.Left(new JoinConstraint.None()))))),
            LateralViews: Sequence<LateralView>.Empty,
            Prewhere: null,
            Selection: null,
            GroupBy: new GroupByExpr.Expressions(Sequence<Expr>.Empty, Sequence<GroupByWithModifier>.Empty),
            ClusterBy: Sequence<Expr>.Empty,
            DistributeBy: Sequence<Expr>.Empty,
            SortBy: Sequence<OrderByExpr>.Empty,
            Having: null,
            NamedWindow: Sequence<NamedWindowDefinition>.Empty,
            Qualify: null,
            WindowBeforeQualify: false,
            ValueTableMode: null,
            ConnectBy: Sequence<ConnectByKind>.Empty,
            Flavor: SelectFlavor.Standard);
        Assert.Equal(actualSelectOnly, expected);
    }

    [Fact]
    public void parse_start_transaction()
    {
        var dialects = TestUtils.AllDialectsExcept(d =>
            // BigQuery and Snowflake does not support this syntax
            //
            // BigQuery: <https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#begin_transaction>
            // Snowflake: <https://docs.snowflake.com/en/sql-reference/sql/begin>
            d is BigQueryDialect || d is SnowflakeDialect);
        var startTransaction = Assert.IsType<Statement.StartTransaction>(dialects
            .VerifiedStmt("START TRANSACTION READ ONLY, READ WRITE, ISOLATION LEVEL SERIALIZABLE"));
        Assert.Equal(
            startTransaction.Modes,
            Sequence.Of<TransactionMode>(
                new TransactionMode.AccessMode(new TransactionAccessMode.ReadOnly()),
                new TransactionMode.AccessMode(new TransactionAccessMode.ReadWrite()),
                new TransactionMode.IsolationLevel(new TransactionIsolationLevel.Serializable())));

        // For historical reasons, PostgreSQL allows the commas between the modes to
        // be omitted.
        startTransaction = Assert.IsType<Statement.StartTransaction>(dialects.OneStatementParsesTo(
            "START TRANSACTION READ ONLY READ WRITE ISOLATION LEVEL SERIALIZABLE",
            "START TRANSACTION READ ONLY, READ WRITE, ISOLATION LEVEL SERIALIZABLE"));
        Assert.Equal(
            startTransaction.Modes,
            Sequence.Of<TransactionMode>(
                new TransactionMode.AccessMode(new TransactionAccessMode.ReadOnly()),
                new TransactionMode.AccessMode(new TransactionAccessMode.ReadWrite()),
                new TransactionMode.IsolationLevel(new TransactionIsolationLevel.Serializable())));

        dialects.VerifiedStmt("START TRANSACTION");
        dialects.VerifiedStmt("BEGIN");
        dialects.VerifiedStmt("BEGIN WORK");
        dialects.VerifiedStmt("BEGIN TRANSACTION");

        dialects.VerifiedStmt("START TRANSACTION ISOLATION LEVEL READ UNCOMMITTED");
        dialects.VerifiedStmt("START TRANSACTION ISOLATION LEVEL READ COMMITTED");
        dialects.VerifiedStmt("START TRANSACTION ISOLATION LEVEL REPEATABLE READ");
        dialects.VerifiedStmt("START TRANSACTION ISOLATION LEVEL SERIALIZABLE");

        // Regression test for https://github.com/sqlparser-rs/sqlparser-rs/pull/139,
        // in which START TRANSACTION would fail to parse if followed by a statement
        // terminator.
        Assert.Equal(
            dialects.ParseSqlStatements("START TRANSACTION; SELECT 1"),
            Sequence.Of(
                VerifiedStmt("START TRANSACTION"),
                VerifiedStmt("SELECT 1")));

        var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("START TRANSACTION ISOLATION LEVEL BAD"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: isolation level, found: BAD"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("START TRANSACTION BAD"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: BAD"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("START TRANSACTION READ ONLY,"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: transaction mode, found: EOF"),
            ex.Error);

        // MS-SQL syntax
        dialects = TestUtils.AllDialectsWhere(d => d.SupportsStartTransactionModifier);
        dialects.VerifiedStmt("BEGIN TRY");
        dialects.VerifiedStmt("BEGIN CATCH");

        dialects = TestUtils.AllDialectsWhere(d =>
            d.SupportsStartTransactionModifier && d.SupportsEndTransactionModifier);
        dialects
            .ParseSqlStatements(
                "\n" +
                "        BEGIN TRY;\n" +
                "            SELECT 1/0;\n" +
                "        END TRY;\n" +
                "        BEGIN CATCH;\n" +
                "            EXECUTE foo;\n" +
                "        END CATCH;\n" +
                "    ");
    }

    [Fact]
    public void parse_set_transaction()
    {
        // SET TRANSACTION shares transaction mode parsing code with START
        // TRANSACTION, so no need to duplicate the tests here. We just do a quick
        // sanity check.
        var set = Assert.IsType<Statement.Set>(VerifiedStmt("SET TRANSACTION READ ONLY, READ WRITE, ISOLATION LEVEL SERIALIZABLE"));
        var setTransaction = Assert.IsType<Set.SetTransaction>(set.Value);
        Assert.Equal(
            setTransaction.Modes,
            Sequence.Of<TransactionMode>(
                new TransactionMode.AccessMode(new TransactionAccessMode.ReadOnly()),
                new TransactionMode.AccessMode(new TransactionAccessMode.ReadWrite()),
                new TransactionMode.IsolationLevel(new TransactionIsolationLevel.Serializable())));
        Assert.False(setTransaction.Session);
        Assert.Null(setTransaction.Snapshot);
    }

    [Fact]
    public void parse_set_variable()
    {
        var set = Assert.IsType<Statement.Set>(VerifiedStmt("SET SOMETHING = '1'"));
        var singleAssignment = Assert.IsType<Set.SingleAssignment>(set.Value);
        Assert.Null(singleAssignment.Scope);
        Assert.False(singleAssignment.Hivevar);
        Assert.Equal(singleAssignment.Variable, ObjectName.From(Sequence.Of(new Ident("SOMETHING"))));
        Assert.Equal(
            singleAssignment.Values,
            Sequence.Of<Expr>(new Expr.Value(new Value.SingleQuotedString("1").WithEmptySpan())));

        set = Assert.IsType<Statement.Set>(VerifiedStmt("SET GLOBAL VARIABLE = 'Value'"));
        singleAssignment = Assert.IsType<Set.SingleAssignment>(set.Value);
        Assert.Equal<ContextModifier?>(singleAssignment.Scope, new ContextModifier.Global());
        Assert.False(singleAssignment.Hivevar);
        Assert.Equal(singleAssignment.Variable, ObjectName.From(Sequence.Of(new Ident("VARIABLE"))));
        Assert.Equal(
            singleAssignment.Values,
            Sequence.Of<Expr>(new Expr.Value(new Value.SingleQuotedString("Value").WithEmptySpan())));

        var multiVariableDialects = TestUtils.AllDialectsWhere(d => d.SupportsParenthesizedSetVariables);
        var sql = "SET (a, b, c) = (1, 2, 3)";
        set = Assert.IsType<Statement.Set>(multiVariableDialects.VerifiedStmt(sql));
        var parenthesizedAssignments = Assert.IsType<Set.ParenthesizedAssignments>(set.Value);
        Assert.Equal(
            parenthesizedAssignments.Variables,
            Sequence.Of(
                ObjectName.From(Sequence.Of(new Ident("a"))),
                ObjectName.From(Sequence.Of(new Ident("b"))),
                ObjectName.From(Sequence.Of(new Ident("c")))));
        Assert.Equal(
            parenthesizedAssignments.Values,
            Sequence.Of(
                Expr.MakeValue(TestUtils.Number("1")),
                Expr.MakeValue(TestUtils.Number("2")),
                Expr.MakeValue(TestUtils.Number("3"))));

        // Subquery expression
        foreach (var (sql2, canonical) in new[]
        {
            (
                "SET (a) = (SELECT 22 FROM tbl1)",
                "SET (a) = ((SELECT 22 FROM tbl1))"
            ),
            (
                "SET (a) = (SELECT 22 FROM tbl1, (SELECT 1 FROM tbl2))",
                "SET (a) = ((SELECT 22 FROM tbl1, (SELECT 1 FROM tbl2)))"
            ),
            (
                "SET (a) = ((SELECT 22 FROM tbl1, (SELECT 1 FROM tbl2)))",
                "SET (a) = ((SELECT 22 FROM tbl1, (SELECT 1 FROM tbl2)))"
            ),
            (
                "SET (a, b) = ((SELECT 22 FROM tbl1, (SELECT 1 FROM tbl2)), SELECT 33 FROM tbl3)",
                "SET (a, b) = ((SELECT 22 FROM tbl1, (SELECT 1 FROM tbl2)), (SELECT 33 FROM tbl3))"
            ),
        })
        {
            multiVariableDialects.OneStatementParsesTo(sql2, canonical);
        }

        var errorSqls = new[]
        {
            ("SET (a, b, c) = (1, 2, 3", "Expected: ), found: EOF"),
            ("SET (a, b, c) = 1, 2, 3", "Expected: (, found: 1"),
            (
                "SET (a) = ((SELECT 22 FROM tbl1)",
                "Expected: ), found: EOF"
            ),
            (
                "SET (a) = ((SELECT 22 FROM tbl1) (SELECT 22 FROM tbl1))",
                "Expected: ), found: ("
            ),
        };
        foreach (var (sql2, error) in errorSqls)
        {
            Assert.Equal(
                new ParserError.ParserErrorValue(error),
                Assert.Throws<ParserException>(() => multiVariableDialects
                    .ParseSqlStatements(sql2)).Error);
        }

        OneStatementParsesTo("SET SOMETHING TO '1'", "SET SOMETHING = '1'");
    }

    [Fact]
    public void parse_set_role_as_variable()
    {
        var set = Assert.IsType<Statement.Set>(VerifiedStmt("SET role = 'foobar'"));
        var singleAssignment = Assert.IsType<Set.SingleAssignment>(set.Value);
        Assert.Null(singleAssignment.Scope);
        Assert.False(singleAssignment.Hivevar);
        Assert.Equal(singleAssignment.Variable, ObjectName.From(Sequence.Of(new Ident("role"))));
        Assert.Equal(
            singleAssignment.Values,
            Sequence.Of<Expr>(new Expr.Value(new Value.SingleQuotedString("foobar").WithEmptySpan())));
    }

    [Fact]
    public void parse_double_colon_cast_at_timezone()
    {
        var sql = "SELECT '2001-01-01T00:00:00.000Z'::TIMESTAMP AT TIME ZONE 'Europe/Brussels' FROM t";
        var select = VerifiedOnlySelect(sql);

        Assert.Equal(
            new Expr.AtTimeZone(
                Timestamp: new Expr.Cast(
                    Kind: CastKind.DoubleColon,
                    Expr: new Expr.Value(
                        new Value.SingleQuotedString("2001-01-01T00:00:00.000Z").WithEmptySpan()),
                    DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                    Format: null),
                TimeZone: new Expr.Value(
                    new Value.SingleQuotedString("Europe/Brussels").WithEmptySpan())),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_set_time_zone()
    {
        var set = Assert.IsType<Statement.Set>(VerifiedStmt("SET TIMEZONE = 'UTC'"));
        var singleAssignment = Assert.IsType<Set.SingleAssignment>(set.Value);
        Assert.Null(singleAssignment.Scope);
        Assert.False(singleAssignment.Hivevar);
        Assert.Equal(singleAssignment.Variable, ObjectName.From(Sequence.Of(new Ident("TIMEZONE"))));
        Assert.Equal(
            singleAssignment.Values,
            Sequence.Of<Expr>(new Expr.Value(new Value.SingleQuotedString("UTC").WithEmptySpan())));

        OneStatementParsesTo("SET TIME ZONE TO 'UTC'", "SET TIMEZONE = 'UTC'");
    }

    [Fact]
    public void parse_commit()
    {
        Assert.True(VerifiedStmt("COMMIT") is Statement.Commit { Chain: false });

        Assert.True(VerifiedStmt("COMMIT AND CHAIN") is Statement.Commit { Chain: true });

        OneStatementParsesTo("COMMIT AND NO CHAIN", "COMMIT");
        OneStatementParsesTo("COMMIT WORK AND NO CHAIN", "COMMIT");
        OneStatementParsesTo("COMMIT TRANSACTION AND NO CHAIN", "COMMIT");
        OneStatementParsesTo("COMMIT WORK AND CHAIN", "COMMIT AND CHAIN");
        OneStatementParsesTo("COMMIT TRANSACTION AND CHAIN", "COMMIT AND CHAIN");
        OneStatementParsesTo("COMMIT WORK", "COMMIT");
        OneStatementParsesTo("COMMIT TRANSACTION", "COMMIT");
    }

    [Fact]
    public void parse_end()
    {
        OneStatementParsesTo("END AND NO CHAIN", "END");
        OneStatementParsesTo("END WORK AND NO CHAIN", "END");
        OneStatementParsesTo("END TRANSACTION AND NO CHAIN", "END");
        OneStatementParsesTo("END WORK AND CHAIN", "END AND CHAIN");
        OneStatementParsesTo("END TRANSACTION AND CHAIN", "END AND CHAIN");
        OneStatementParsesTo("END WORK", "END");
        OneStatementParsesTo("END TRANSACTION", "END");
        // MS-SQL syntax
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsEndTransactionModifier);
        dialects.VerifiedStmt("END TRY");
        dialects.VerifiedStmt("END CATCH");
    }

    [Fact]
    public void parse_rollback()
    {
        Assert.True(VerifiedStmt("ROLLBACK") is Statement.Rollback { Chain: false, Savepoint: null });

        Assert.True(VerifiedStmt("ROLLBACK AND CHAIN") is Statement.Rollback { Chain: true, Savepoint: null });

        var rollback = Assert.IsType<Statement.Rollback>(VerifiedStmt("ROLLBACK TO SAVEPOINT test1"));
        Assert.False(rollback.Chain);
        Assert.Equal(rollback.Savepoint, new Ident("test1"));

        rollback = Assert.IsType<Statement.Rollback>(VerifiedStmt("ROLLBACK AND CHAIN TO SAVEPOINT test1"));
        Assert.True(rollback.Chain);
        Assert.Equal(rollback.Savepoint, new Ident("test1"));

        OneStatementParsesTo("ROLLBACK AND NO CHAIN", "ROLLBACK");
        OneStatementParsesTo("ROLLBACK WORK AND NO CHAIN", "ROLLBACK");
        OneStatementParsesTo("ROLLBACK TRANSACTION AND NO CHAIN", "ROLLBACK");
        OneStatementParsesTo("ROLLBACK WORK AND CHAIN", "ROLLBACK AND CHAIN");
        OneStatementParsesTo("ROLLBACK TRANSACTION AND CHAIN", "ROLLBACK AND CHAIN");
        OneStatementParsesTo("ROLLBACK WORK", "ROLLBACK");
        OneStatementParsesTo("ROLLBACK TRANSACTION", "ROLLBACK");
        OneStatementParsesTo("ROLLBACK TO test1", "ROLLBACK TO SAVEPOINT test1");
        OneStatementParsesTo(
            "ROLLBACK AND CHAIN TO test1",
            "ROLLBACK AND CHAIN TO SAVEPOINT test1");
    }

    [Fact]
    public void parse_abort()
    {
        OneStatementParsesTo("ABORT", "ROLLBACK");
        OneStatementParsesTo("ABORT TRANSACTION", "ROLLBACK");
        OneStatementParsesTo("ABORT WORK", "ROLLBACK");
        OneStatementParsesTo("ABORT AND CHAIN", "ROLLBACK AND CHAIN");
        OneStatementParsesTo("ABORT AND NO CHAIN", "ROLLBACK");
        OneStatementParsesTo("ABORT TRANSACTION AND CHAIN", "ROLLBACK AND CHAIN");
        OneStatementParsesTo("ABORT WORK AND NO CHAIN", "ROLLBACK");

        Assert.Equal(
            Assert.Throws<ParserException>(() => ParseSqlStatements("ABORT TO test1")).Error,
            new ParserError.ParserErrorValue("Expected: end of statement, found: TO"));
    }

    // #[should_panic(expected = "Parse results with GenericDialect are different from PostgreSqlDialect")]
    [Fact]
    public void ensure_multiple_dialects_are_tested()
    {
        var ex = Assert.ThrowsAny<Exception>(() =>
        {
            // The SQL here must be parsed differently by different dialects.
            // At the time of writing, `@foo` is accepted as a valid identifier
            // by the Generic and the MSSQL dialect, but not by Postgres and ANSI.
            _ = ParseSqlStatements("SELECT @foo");
        });
        Assert.Contains("Parse results with GenericDialect are different from PostgreSqlDialect", ex.Message);
    }

    [Fact]
    public void parse_create_index()
    {
        var sql = "CREATE UNIQUE INDEX IF NOT EXISTS idx_name ON test(name, age DESC)";
        var indexedColumns = Sequence.Of(
            new IndexColumn(
                OperatorClass: null,
                Column: new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("name")),
                    WithFill: null,
                    Options: new OrderByOptions(
                        Sort: null,
                        NullsFirst: null))),
            new IndexColumn(
                OperatorClass: null,
                Column: new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("age")),
                    WithFill: null,
                    Options: new OrderByOptions(
                        Sort: new OrderBySort.Desc(),
                        NullsFirst: null))));
        switch (VerifiedStmt(sql))
        {
            case Statement.CreateIndex { Value: { Name: { } name } createIndex }:
                Assert.Equal("idx_name", name.ToString());
                Assert.Equal("test", createIndex.TableName.ToString());
                Assert.Equal(indexedColumns, createIndex.Columns);
                Assert.True(createIndex.Unique);
                Assert.True(createIndex.IfNotExists);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_create_index_with_using_function()
    {
        var sql = "CREATE UNIQUE INDEX IF NOT EXISTS idx_name ON test USING BTREE (name, age DESC)";
        var indexedColumns = Sequence.Of(
            new IndexColumn(
                OperatorClass: null,
                Column: new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("name")),
                    WithFill: null,
                    Options: new OrderByOptions(
                        Sort: null,
                        NullsFirst: null))),
            new IndexColumn(
                OperatorClass: null,
                Column: new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("age")),
                    WithFill: null,
                    Options: new OrderByOptions(
                        Sort: new OrderBySort.Desc(),
                        NullsFirst: null))));
        switch (VerifiedStmt(sql))
        {
            case Statement.CreateIndex { Value: { Name: { } name, NullsDistinct: null, Predicate: null } createIndex }:
                Assert.Equal("idx_name", name.ToString());
                Assert.Equal("test", createIndex.TableName.ToString());
                Assert.Equal("BTREE", createIndex.Using!.ToString());
                Assert.Equal(indexedColumns, createIndex.Columns);
                Assert.True(createIndex.Unique);
                Assert.False(createIndex.Concurrently);
                Assert.False(createIndex.Async);
                Assert.True(createIndex.IfNotExists);
                Assert.True(createIndex.Include.IsEmpty);
                Assert.True(createIndex.With.IsEmpty);
                Assert.True(createIndex.IndexOptions.IsEmpty);
                Assert.True(createIndex.AlterOptions.IsEmpty);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_create_index_with_with_clause()
    {
        var sql = "CREATE UNIQUE INDEX title_idx ON films(title) WITH (fillfactor = 70, single_param)";
        var indexedColumns = Sequence.Of(new IndexColumn(
            Column: new OrderByExpr(
                Expr: new Expr.Identifier(new Ident("title")),
                Options: new OrderByOptions(
                    Sort: null,
                    NullsFirst: null),
                WithFill: null),
            OperatorClass: null));
        var withParameters = Sequence.Of<Expr>(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("fillfactor")),
                Op: new BinaryOperator.Eq(),
                Right: Expr.MakeValue(TestUtils.Number("70"))),
            new Expr.Identifier(new Ident("single_param")));
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsCreateIndexWithClause);
        switch (dialects.VerifiedStmt(sql))
        {
            case Statement.CreateIndex { Value: { Name: { } name, Using: null, NullsDistinct: null, Predicate: null } createIndex }:
                Assert.Equal("title_idx", name.ToString());
                Assert.Equal("films", createIndex.TableName.ToString());
                Assert.Equal(indexedColumns, createIndex.Columns);
                Assert.True(createIndex.Unique);
                Assert.False(createIndex.Concurrently);
                Assert.False(createIndex.Async);
                Assert.False(createIndex.IfNotExists);
                Assert.True(createIndex.Include.IsEmpty);
                Assert.Equal(withParameters, createIndex.With);
                Assert.True(createIndex.IndexOptions.IsEmpty);
                Assert.True(createIndex.AlterOptions.IsEmpty);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_create_index_async()
    {
        VerifiedStmt("CREATE INDEX ASYNC my_index ON my_table(col1)");
        VerifiedStmt("CREATE UNIQUE INDEX ASYNC my_index ON my_table(col1)");
    }

    [Fact]
    public void parse_drop_index()
    {
        var sql = "DROP INDEX idx_a";
        var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        Assert.Equal(
            new List<string> { "idx_a" },
            drop.Names.Select(n => n.ToString()).ToList());
        Assert.Equal<ObjectType>(new ObjectType.Index(), drop.ObjectType);
    }

    [Fact]
    public void parse_create_role()
    {
        var sql = "CREATE ROLE consultant";
        var createRole = Assert.IsType<Statement.CreateRole>(VerifiedStmt(sql)).Value;
        TestUtils.AssertEqVec(new[] { "consultant" }, createRole.Names);

        sql = "CREATE ROLE IF NOT EXISTS mysql_a, mysql_b";
        createRole = Assert.IsType<Statement.CreateRole>(VerifiedStmt(sql)).Value;
        TestUtils.AssertEqVec(new[] { "mysql_a", "mysql_b" }, createRole.Names);
        Assert.True(createRole.IfNotExists);
    }

    [Fact]
    public void parse_drop_role()
    {
        var sql = "DROP ROLE abc";
        var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        TestUtils.AssertEqVec(new[] { "abc" }, drop.Names);
        Assert.Equal<ObjectType>(new ObjectType.Role(), drop.ObjectType);
        Assert.False(drop.IfExists);

        sql = "DROP ROLE IF EXISTS def, magician, quaternion";
        drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
        TestUtils.AssertEqVec(new[] { "def", "magician", "quaternion" }, drop.Names);
        Assert.Equal<ObjectType>(new ObjectType.Role(), drop.ObjectType);
        Assert.True(drop.IfExists);
    }

    [Fact]
    public void parse_grant()
    {
        var sql = "GRANT SELECT, INSERT, UPDATE (shape, size), USAGE, DELETE, TRUNCATE, REFERENCES, TRIGGER, CONNECT, CREATE, EXECUTE, TEMPORARY, DROP ON abc, def TO xyz, m WITH GRANT OPTION GRANTED BY jj";
        var grant = Assert.IsType<Statement.Grant>(VerifiedStmt(sql)).Value;
        switch ((grant.Privileges, grant.Objects))
        {
            case (Privileges.Actions { Value: var actions }, GrantObjects.Tables { Value: var objects }):
                Assert.Equal(
                    Sequence.Of<Ast.Action>(
                        new Ast.Action.Select(Columns: null),
                        new Ast.Action.Insert(Columns: null),
                        new Ast.Action.Update(
                            Columns: Sequence.Of(
                                new Ident("shape", null, Span.Empty()),
                                new Ident("size", null, Span.Empty()))),
                        new Ast.Action.Usage(),
                        new Ast.Action.Delete(),
                        new Ast.Action.Truncate(),
                        new Ast.Action.References(Columns: null),
                        new Ast.Action.Trigger(),
                        new Ast.Action.Connect(),
                        new Ast.Action.Create(ObjType: null),
                        new Ast.Action.Execute(ObjType: null),
                        new Ast.Action.Temporary(),
                        new Ast.Action.Drop()),
                    actions);
                TestUtils.AssertEqVec(new[] { "abc", "def" }, objects);
                TestUtils.AssertEqVec(new[] { "xyz", "m" }, grant.Grantees);
                Assert.True(grant.WithGrantOption);
                Assert.Equal("jj", grant.GrantedBy!.ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        var sql2 = "GRANT INSERT ON ALL TABLES IN SCHEMA public TO browser";
        grant = Assert.IsType<Statement.Grant>(VerifiedStmt(sql2)).Value;
        switch ((grant.Privileges, grant.Objects))
        {
            case (Privileges.Actions { Value: var actions }, GrantObjects.AllTablesInSchema { Schemas: var schemas }):
                Assert.Equal(Sequence.Of<Ast.Action>(new Ast.Action.Insert(Columns: null)), actions);
                TestUtils.AssertEqVec(new[] { "public" }, schemas);
                TestUtils.AssertEqVec(new[] { "browser" }, grant.Grantees);
                Assert.False(grant.WithGrantOption);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        var sql3 = "GRANT USAGE, SELECT ON SEQUENCE p TO u";
        grant = Assert.IsType<Statement.Grant>(VerifiedStmt(sql3)).Value;
        switch ((grant.Privileges, grant.Objects, grant.GrantedBy))
        {
            case (Privileges.Actions { Value: var actions }, GrantObjects.Sequences { Value: var objects }, null):
                Assert.Equal(
                    Sequence.Of<Ast.Action>(new Ast.Action.Usage(), new Ast.Action.Select(Columns: null)),
                    actions);
                TestUtils.AssertEqVec(new[] { "p" }, objects);
                TestUtils.AssertEqVec(new[] { "u" }, grant.Grantees);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        var sql4 = "GRANT ALL PRIVILEGES ON aa, b TO z";
        grant = Assert.IsType<Statement.Grant>(VerifiedStmt(sql4)).Value;
        Assert.Equal<Privileges>(
            new Privileges.All(WithPrivilegesKeyword: true),
            grant.Privileges);

        var sql5 = "GRANT ALL ON SCHEMA aa, b TO z";
        grant = Assert.IsType<Statement.Grant>(VerifiedStmt(sql5)).Value;
        switch ((grant.Privileges, grant.Objects))
        {
            case (Privileges.All { WithPrivilegesKeyword: var withPrivilegesKeyword }, GrantObjects.Schemas { Value: var schemas }):
                Assert.False(withPrivilegesKeyword);
                TestUtils.AssertEqVec(new[] { "aa", "b" }, schemas);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        var sql6 = "GRANT USAGE ON ALL SEQUENCES IN SCHEMA bus TO a, beta WITH GRANT OPTION";
        grant = Assert.IsType<Statement.Grant>(VerifiedStmt(sql6)).Value;
        switch ((grant.Privileges, grant.Objects))
        {
            case (Privileges.Actions { Value: var actions }, GrantObjects.AllSequencesInSchema { Schemas: var schemas }):
                Assert.Equal(Sequence.Of<Ast.Action>(new Ast.Action.Usage()), actions);
                TestUtils.AssertEqVec(new[] { "bus" }, schemas);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        VerifiedStmt("GRANT SELECT ON ALL TABLES IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON ALL TABLES IN SCHEMA db1.sc1 TO ROLE role1 WITH GRANT OPTION");
        VerifiedStmt("GRANT SELECT ON ALL TABLES IN SCHEMA db1.sc1 TO DATABASE ROLE role1");
        VerifiedStmt("GRANT SELECT ON ALL TABLES IN SCHEMA db1.sc1 TO APPLICATION role1");
        VerifiedStmt("GRANT SELECT ON ALL TABLES IN SCHEMA db1.sc1 TO APPLICATION ROLE role1");
        VerifiedStmt("GRANT SELECT ON ALL TABLES IN SCHEMA db1.sc1 TO SHARE share1");
        VerifiedStmt("GRANT SELECT ON ALL VIEWS IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON ALL MATERIALIZED VIEWS IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON ALL EXTERNAL TABLES IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT USAGE ON ALL FUNCTIONS IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT USAGE ON SCHEMA sc1 TO a:b");
        VerifiedStmt("GRANT USAGE ON SCHEMA sc1 TO GROUP group1");
        VerifiedStmt("GRANT OWNERSHIP ON ALL TABLES IN SCHEMA DEV_STAS_ROGOZHIN TO ROLE ANALYST");
        VerifiedStmt("GRANT OWNERSHIP ON ALL TABLES IN SCHEMA DEV_STAS_ROGOZHIN TO ROLE ANALYST COPY CURRENT GRANTS");
        VerifiedStmt("GRANT OWNERSHIP ON ALL TABLES IN SCHEMA DEV_STAS_ROGOZHIN TO ROLE ANALYST REVOKE CURRENT GRANTS");
        // Printing these the other way round yields output the parser rejects.
        VerifiedStmt(
            "GRANT OWNERSHIP ON ALL TABLES IN SCHEMA s TO ROLE r WITH GRANT OPTION COPY CURRENT GRANTS");
        VerifiedStmt("GRANT USAGE ON DATABASE db1 TO ROLE role1");
        VerifiedStmt("GRANT USAGE ON WAREHOUSE wh1 TO ROLE role1");
        VerifiedStmt("GRANT OWNERSHIP ON INTEGRATION int1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON VIEW view1 TO ROLE role1");
        VerifiedStmt("GRANT EXEC ON my_sp TO runner");
        VerifiedStmt("GRANT UPDATE ON my_table TO updater_role AS dbo");
        TestUtils.AllDialectsWhere(d => d.IdentifierQuoteStyle("none") == '[')
            .VerifiedStmt("GRANT SELECT ON [my_table] TO [public]");
        VerifiedStmt("GRANT SELECT ON FUTURE SCHEMAS IN DATABASE db1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON FUTURE TABLES IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON FUTURE EXTERNAL TABLES IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON FUTURE VIEWS IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON FUTURE MATERIALIZED VIEWS IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT SELECT ON FUTURE SEQUENCES IN SCHEMA db1.sc1 TO ROLE role1");
        VerifiedStmt("GRANT USAGE ON PROCEDURE db1.sc1.foo(INT) TO ROLE role1");
        VerifiedStmt("GRANT USAGE ON FUNCTION db1.sc1.foo(INT) TO ROLE role1");
        VerifiedStmt("GRANT ROLE role1 TO ROLE role2");
        VerifiedStmt("GRANT ROLE role1 TO USER user");
        VerifiedStmt("GRANT CREATE SCHEMA ON DATABASE db1 TO ROLE role1");
        // PUBLIC takes no name, so it must not trail a space. MsSql reserves it as
        // an ordinary grantee name.
        TestUtils.AllDialectsExcept(d => d is MsSqlDialect).VerifiedStmt("GRANT SELECT ON t TO PUBLIC");
    }

    [Fact]
    public void parse_deny()
    {
        var sql = "DENY INSERT, DELETE ON users TO analyst CASCADE AS admin";
        var deny = Assert.IsType<Statement.Deny>(VerifiedStmt(sql)).Value;
        Assert.Equal<Privileges>(
            new Privileges.Actions(Sequence.Of<Ast.Action>(new Ast.Action.Insert(Columns: null), new Ast.Action.Delete())),
            deny.Privileges);
        Assert.Equal<GrantObjects>(
            new GrantObjects.Tables(Sequence.Of(ObjectName.From(Sequence.Of(new Ident("users"))))),
            deny.Objects);
        TestUtils.AssertEqVec(new[] { "analyst" }, deny.Grantees);
        Assert.Equal<CascadeOption?>(new CascadeOption.Cascade(), deny.Cascade);
        Assert.Equal(Ident.From("admin"), deny.GrantedBy);

        VerifiedStmt("DENY SELECT, INSERT, UPDATE, DELETE ON db1.sc1 TO role1, role2");
        VerifiedStmt("DENY ALL ON db1.sc1 TO role1");
        VerifiedStmt("DENY EXEC ON my_sp TO runner");

        TestUtils.AllDialectsWhere(d => d.IdentifierQuoteStyle("none") == '[')
            .VerifiedStmt("DENY SELECT ON [my_table] TO [public]");
    }

    [Fact]
    public void test_revoke()
    {
        var sql = "REVOKE ALL PRIVILEGES ON users, auth FROM analyst";
        switch (VerifiedStmt(sql))
        {
            case Statement.Revoke { Value: { GrantOptionFor: false, Objects: GrantObjects.Tables { Value: var tables } } revoke }:
                Assert.Equal<Privileges>(
                    new Privileges.All(WithPrivilegesKeyword: true),
                    revoke.Privileges);
                TestUtils.AssertEqVec(new[] { "users", "auth" }, tables);
                TestUtils.AssertEqVec(new[] { "analyst" }, revoke.Grantees);
                Assert.Null(revoke.Cascade);
                Assert.Null(revoke.GrantedBy);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_revoke_with_cascade()
    {
        var sql = "REVOKE ALL PRIVILEGES ON users, auth FROM analyst CASCADE";
        switch (VerifiedStmt(sql))
        {
            case Statement.Revoke { Value: { GrantOptionFor: false, Objects: GrantObjects.Tables { Value: var tables } } revoke }:
                Assert.Equal<Privileges>(
                    new Privileges.All(WithPrivilegesKeyword: true),
                    revoke.Privileges);
                TestUtils.AssertEqVec(new[] { "users", "auth" }, tables);
                TestUtils.AssertEqVec(new[] { "analyst" }, revoke.Grantees);
                Assert.Equal<CascadeOption?>(revoke.Cascade, new CascadeOption.Cascade());
                Assert.Null(revoke.GrantedBy);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_revoke_grant_option_for()
    {
        if (VerifiedStmt("REVOKE GRANT OPTION FOR SELECT ON t FROM r") is not Statement.Revoke { Value: var revoke })
        {
            throw new InvalidOperationException("unreachable");
        }
        Assert.True(revoke.GrantOptionFor);

        // Clearing the flag must yield exactly the plain revoke.
        revoke = revoke with { GrantOptionFor = false };
        Assert.Equal<Statement>(
            new Statement.Revoke(revoke),
            VerifiedStmt("REVOKE SELECT ON t FROM r"));

        VerifiedStmt("REVOKE GRANT OPTION FOR ALL ON t FROM r");
        VerifiedStmt("REVOKE GRANT OPTION FOR SELECT ON t FROM r CASCADE");
        VerifiedStmt("REVOKE GRANT OPTION FOR SELECT ON t FROM r RESTRICT");
    }

    [Fact]
    public void parse_merge()
    {
        var sql = "MERGE INTO s.bar AS dest USING (SELECT * FROM s.foo) AS stg ON dest.D = stg.D AND dest.E = stg.E WHEN NOT MATCHED THEN INSERT (A, B, C) VALUES (stg.A, stg.B, stg.C) WHEN MATCHED AND dest.A = 'a' THEN UPDATE SET dest.F = stg.F, dest.G = stg.G WHEN MATCHED THEN DELETE";
        var sqlNoInto = "MERGE s.bar AS dest USING (SELECT * FROM s.foo) AS stg ON dest.D = stg.D AND dest.E = stg.E WHEN NOT MATCHED THEN INSERT (A, B, C) VALUES (stg.A, stg.B, stg.C) WHEN MATCHED AND dest.A = 'a' THEN UPDATE SET dest.F = stg.F, dest.G = stg.G WHEN MATCHED THEN DELETE";
        switch ((VerifiedStmt(sql), VerifiedStmt(sqlNoInto)))
        {
            case (Statement.Merge { Value: var merge }, Statement.Merge { Value: var mergeNoInto }):
            {
                var into = merge.Into;
                var table = merge.Table;
                var source = merge.Source;
                var on = merge.On;
                var clauses = merge.Clauses;
                var noInto = mergeNoInto.Into;
                var tableNoInto = mergeNoInto.Table;
                var sourceNoInto = mergeNoInto.Source;
                var onNoInto = mergeNoInto.On;
                var clausesNoInto = mergeNoInto.Clauses;

                Assert.True(into);
                Assert.False(noInto);

                Assert.Equal<TableFactor>(
                    table,
                    new TableFactor.Table(
                        Name: ObjectName.From(Sequence.Of(new Ident("s"), new Ident("bar"))),
                        Alias: TestUtils.TableAlias(true, "dest"),
                        Args: null,
                        WithHints: Sequence<Expr>.Empty,
                        Version: null,
                        Partitions: Sequence<Ident>.Empty,
                        WithOrdinality: false,
                        JsonPath: null,
                        Sample: null,
                        IndexHints: Sequence<TableIndexHints>.Empty));
                Assert.Equal(table, tableNoInto);

                Assert.Equal<TableFactor>(
                    source,
                    new TableFactor.Derived(
                        Lateral: false,
                        Subquery: new Query(
                            With: null,
                            Body: new SetExpr.Select(new Select(
                                SelectToken: AttachedToken.Empty(),
                                OptimizerHints: Sequence<OptimizerHint>.Empty,
                                Distinct: null,
                                SelectModifiers: null,
                                Top: null,
                                TopBeforeDistinct: false,
                                Projection: Sequence.Of<SelectItem>(new SelectItem.Wildcard(
                                    WildcardAdditionalOptions.Default())),
                                Exclude: null,
                                Into: null,
                                From: Sequence.Of(new TableWithJoins(
                                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                                        new Ident("s"),
                                        new Ident("foo")))),
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
                                Flavor: SelectFlavor.Standard)),
                            OrderBy: null,
                            LimitClause: null,
                            Fetch: null,
                            Locks: Sequence<LockClause>.Empty,
                            ForClause: null,
                            Settings: null,
                            FormatClause: null,
                            PipeOperators: Sequence<PipeOperator>.Empty),
                        Alias: TestUtils.TableAlias(true, "stg"),
                        Sample: null));
                Assert.Equal(source, sourceNoInto);

                Assert.Equal<Expr>(
                    on,
                    new Expr.BinaryOp(
                        Left: new Expr.BinaryOp(
                            Left: new Expr.CompoundIdentifier(Sequence.Of(
                                new Ident("dest"),
                                new Ident("D"))),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.CompoundIdentifier(Sequence.Of(
                                new Ident("stg"),
                                new Ident("D")))),
                        Op: new BinaryOperator.And(),
                        Right: new Expr.BinaryOp(
                            Left: new Expr.CompoundIdentifier(Sequence.Of(
                                new Ident("dest"),
                                new Ident("E"))),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.CompoundIdentifier(Sequence.Of(
                                new Ident("stg"),
                                new Ident("E"))))));
                Assert.Equal(on, onNoInto);

                Assert.Equal(
                    clauses,
                    Sequence.Of(
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatched(),
                            Predicate: null,
                            Action: new MergeAction.Insert(new MergeInsertExpr(
                                InsertToken: AttachedToken.Empty(),
                                Columns: Sequence.Of(
                                    ObjectName.From(new Ident("A")),
                                    ObjectName.From(new Ident("B")),
                                    ObjectName.From(new Ident("C"))),
                                KindToken: AttachedToken.Empty(),
                                Kind: new MergeInsertKind.Values(new Values(
                                    ValueKeyword: false,
                                    ExplicitRow: false,
                                    Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                                        new Expr.CompoundIdentifier(Sequence.Of(
                                            new Ident("stg"),
                                            new Ident("A"))),
                                        new Expr.CompoundIdentifier(Sequence.Of(
                                            new Ident("stg"),
                                            new Ident("B"))),
                                        new Expr.CompoundIdentifier(Sequence.Of(
                                            new Ident("stg"),
                                            new Ident("C")))))))),
                                InsertPredicate: null))),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.Matched(),
                            Predicate: new Expr.BinaryOp(
                                Left: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("dest"),
                                    new Ident("A"))),
                                Op: new BinaryOperator.Eq(),
                                Right: new Expr.Value(
                                    new Value.SingleQuotedString("a").WithEmptySpan())),
                            Action: new MergeAction.Update(new MergeUpdateExpr(
                                UpdateToken: AttachedToken.Empty(),
                                Kind: new MergeUpdateKind.Set(Sequence.Of(
                                    new Assignment(
                                        Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(
                                            new Ident("dest"),
                                            new Ident("F")))),
                                        Value: new Expr.CompoundIdentifier(Sequence.Of(
                                            new Ident("stg"),
                                            new Ident("F")))),
                                    new Assignment(
                                        Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(
                                            new Ident("dest"),
                                            new Ident("G")))),
                                        Value: new Expr.CompoundIdentifier(Sequence.Of(
                                            new Ident("stg"),
                                            new Ident("G")))))),
                                UpdatePredicate: null,
                                DeletePredicate: null))),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.Matched(),
                            Predicate: null,
                            Action: new MergeAction.Delete(
                                DeleteToken: AttachedToken.Empty()))));
                Assert.Equal(clauses, clausesNoInto);
                break;
            }
            default:
                throw new InvalidOperationException("unreachable");
        }

        sql = "MERGE INTO s.bar AS dest USING newArrivals AS S ON (1 > 1) WHEN NOT MATCHED THEN INSERT VALUES (stg.A, stg.B, stg.C)";
        VerifiedStmt(sql);

        // MERGE with predicates
        sql =
            "MERGE INTO FOO " +
            "USING FOO_IMPORT " +
            "ON (FOO.ID = FOO_IMPORT.ID) " +
            "WHEN MATCHED THEN " +
            "UPDATE SET FOO.NAME = FOO_IMPORT.NAME " +
            "WHERE 1 = 1 " +
            "DELETE WHERE FOO.NAME LIKE '%.DELETE' " +
            "WHEN NOT MATCHED THEN " +
            "INSERT (ID, NAME) " +
            "VALUES (FOO_IMPORT.ID, UPPER(FOO_IMPORT.NAME)) " +
            "WHERE NOT FOO_IMPORT.NAME LIKE '%.DO_NOT_INSERT'";
        TestUtils.AllDialects().VerifiedStmt(sql);

        // MERGE with simple insert columns
        sql =
            "MERGE INTO FOO USING FOO_IMPORT ON (FOO.ID = FOO_IMPORT.ID) " +
            "WHEN NOT MATCHED THEN " +
            "INSERT (ID, NAME) " +
            "VALUES (1, 'abc')";
        TestUtils.AllDialects().VerifiedStmt(sql);

        // MERGE with qualified insert columns
        sql =
            "MERGE INTO FOO USING FOO_IMPORT ON (FOO.ID = FOO_IMPORT.ID) " +
            "WHEN NOT MATCHED THEN " +
            "INSERT (FOO.ID, FOO.NAME) " +
            "VALUES (1, 'abc')";
        TestUtils.AllDialects().VerifiedStmt(sql);

        // MERGE with schema qualified insert columns
        sql =
            "MERGE INTO PLAYGROUND.FOO USING FOO_IMPORT ON (PLAYGROUND.FOO.ID = FOO_IMPORT.ID) " +
            "WHEN NOT MATCHED THEN " +
            "INSERT (PLAYGROUND.FOO.ID, PLAYGROUND.FOO.NAME) " +
            "VALUES (1, 'abc')";
        TestUtils.AllDialects().VerifiedStmt(sql);

        // MERGE with wildcard (UPDATE SET * and INSERT *)
        sql = "MERGE INTO target USING source ON target.id = source.id WHEN MATCHED THEN UPDATE SET * WHEN NOT MATCHED THEN INSERT *";
        switch (VerifiedStmt(sql))
        {
            case Statement.Merge { Value: var merge }:
                Assert.Equal(2, merge.Clauses.Count);

                switch (merge.Clauses[0].Action)
                {
                    case MergeAction.Update { Value: var updateExpr }:
                        Assert.True(updateExpr.Kind is MergeUpdateKind.Wildcard);
                        break;
                    default:
                        throw new InvalidOperationException("Expected UPDATE action");
                }

                switch (merge.Clauses[1].Action)
                {
                    case MergeAction.Insert { Value: var insertExpr }:
                        Assert.True(insertExpr.Kind is MergeInsertKind.Wildcard);
                        Assert.True(insertExpr.Columns.IsEmpty);
                        break;
                    default:
                        throw new InvalidOperationException("Expected INSERT action");
                }
                break;
            default:
                throw new InvalidOperationException("Expected MERGE statement");
        }

        VerifiedStmt("MERGE INTO target USING source ON target.id = source.id WHEN MATCHED AND source.active = 1 THEN UPDATE SET *");

        VerifiedStmt("MERGE INTO target USING source ON target.id = source.id WHEN NOT MATCHED BY TARGET THEN INSERT *");

        VerifiedStmt("MERGE INTO target USING source ON target.id = source.id WHEN MATCHED THEN UPDATE SET * WHEN NOT MATCHED THEN INSERT (a, b) VALUES (source.a, source.b)");

        sql = string.Concat(
            "MERGE INTO t1 AS target ",
            "USING (SELECT * FROM t2) AS source ",
            "ON target.id = source.id ",
            "WHEN MATCHED THEN UPDATE SET * ",
            "WHEN NOT MATCHED THEN INSERT *");
        VerifiedStmt(sql);
    }

    [Fact]
    public void test_merge_in_cte()
    {
        VerifiedOnlySelect(
            "WITH x AS (" +
            "MERGE INTO t USING (VALUES (1)) ON 1 = 1 " +
            "WHEN MATCHED THEN DELETE " +
            "RETURNING *" +
            ") SELECT * FROM x");
    }

    [Fact]
    public void test_merge_with_returning()
    {
        var sql = "MERGE INTO wines AS w " +
                  "USING wine_stock_changes AS s " +
                  "ON s.winename = w.winename " +
                  "WHEN NOT MATCHED AND s.stock_delta > 0 THEN INSERT VALUES (s.winename, s.stock_delta) " +
                  "WHEN MATCHED AND w.stock + s.stock_delta > 0 THEN UPDATE SET stock = w.stock + s.stock_delta " +
                  "WHEN MATCHED THEN DELETE " +
                  "RETURNING merge_action(), w.*";
        VerifiedStmt(sql);
    }

    [Fact]
    public void test_merge_with_output()
    {
        var sql = "MERGE INTO target_table USING source_table " +
                  "ON target_table.id = source_table.oooid " +
                  "WHEN MATCHED THEN " +
                  "UPDATE SET target_table.description = source_table.description " +
                  "WHEN NOT MATCHED THEN " +
                  "INSERT (ID, description) VALUES (source_table.id, source_table.description) " +
                  "OUTPUT inserted.* INTO log_target";

        VerifiedStmt(sql);
    }

    [Fact]
    public void test_merge_with_output_without_into()
    {
        var sql = "MERGE INTO a USING b ON a.id = b.id " +
                  "WHEN MATCHED THEN DELETE " +
                  "OUTPUT inserted.*";
        VerifiedStmt(sql);
    }

    [Fact]
    public void test_merge_into_using_table()
    {
        var sql = "MERGE INTO target_table USING source_table " +
                  "ON target_table.id = source_table.oooid " +
                  "WHEN MATCHED THEN " +
                  "UPDATE SET target_table.description = source_table.description " +
                  "WHEN NOT MATCHED THEN " +
                  "INSERT (ID, description) VALUES (source_table.id, source_table.description)";

        VerifiedStmt(sql);
    }

    [Fact]
    public void test_merge_with_delimiter()
    {
        var sql = "MERGE INTO target_table USING source_table " +
                  "ON target_table.id = source_table.oooid " +
                  "WHEN MATCHED THEN " +
                  "UPDATE SET target_table.description = source_table.description " +
                  "WHEN NOT MATCHED THEN " +
                  "INSERT (ID, description) VALUES (source_table.id, source_table.description);";

        // match parse_sql_statements(sql) { Ok(_) => {} _ => unreachable!() }
        _ = ParseSqlStatements(sql);
    }

    [Fact]
    public void test_merge_invalid_statements()
    {
        var dialects = TestUtils.AllDialects();
        foreach (var (sql, errMsg) in new[]
        {
            (
                "MERGE INTO T USING U ON TRUE WHEN NOT MATCHED THEN UPDATE SET a = b",
                "UPDATE is not allowed in a NOT MATCHED merge clause"
            ),
            (
                "MERGE INTO T USING U ON TRUE WHEN NOT MATCHED THEN DELETE",
                "DELETE is not allowed in a NOT MATCHED merge clause"
            ),
            (
                "MERGE INTO T USING U ON TRUE WHEN MATCHED THEN INSERT(a) VALUES(b)",
                "INSERT is not allowed in a MATCHED merge clause"
            ),
        })
        {
            var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
            Assert.Equal(
                new ParserError.ParserErrorValue(errMsg),
                ex.Error);
        }
    }

    [Fact]
    public void test_lock()
    {
        var sql = "SELECT * FROM student WHERE id = '1' FOR UPDATE";
        var locks = VerifiedQuery(sql).Locks.ToList();
        Assert.Equal(1, locks.Count);
        var @lock = locks[locks.Count - 1]; // ast.locks.pop().unwrap()
        locks.RemoveAt(locks.Count - 1);
        Assert.Equal<LockType>(@lock.LockType, new LockType.Update());
        Assert.Null(@lock.Of);
        Assert.Null(@lock.Nonblock);

        sql = "SELECT * FROM student WHERE id = '1' FOR SHARE";
        locks = VerifiedQuery(sql).Locks.ToList();
        Assert.Equal(1, locks.Count);
        @lock = locks[locks.Count - 1]; // ast.locks.pop().unwrap()
        locks.RemoveAt(locks.Count - 1);
        Assert.Equal<LockType>(@lock.LockType, new LockType.Share());
        Assert.Null(@lock.Of);
        Assert.Null(@lock.Nonblock);
    }

    [Fact]
    public void test_lock_table()
    {
        var sql = "SELECT * FROM student WHERE id = '1' FOR UPDATE OF school";
        var locks = VerifiedQuery(sql).Locks.ToList();
        Assert.Equal(1, locks.Count);
        var @lock = locks[locks.Count - 1]; // ast.locks.pop().unwrap()
        locks.RemoveAt(locks.Count - 1);
        Assert.Equal<LockType>(@lock.LockType, new LockType.Update());
        Assert.Equal(
            @lock.Of!,
            ObjectName.From(Sequence.Of(new Ident("school", null, Span.Empty()))));
        Assert.Null(@lock.Nonblock);

        sql = "SELECT * FROM student WHERE id = '1' FOR SHARE OF school";
        locks = VerifiedQuery(sql).Locks.ToList();
        Assert.Equal(1, locks.Count);
        @lock = locks[locks.Count - 1]; // ast.locks.pop().unwrap()
        locks.RemoveAt(locks.Count - 1);
        Assert.Equal<LockType>(@lock.LockType, new LockType.Share());
        Assert.Equal(
            @lock.Of!,
            ObjectName.From(Sequence.Of(new Ident("school", null, Span.Empty()))));
        Assert.Null(@lock.Nonblock);

        sql = "SELECT * FROM student WHERE id = '1' FOR SHARE OF school FOR UPDATE OF student";
        locks = VerifiedQuery(sql).Locks.ToList();
        Assert.Equal(2, locks.Count);
        @lock = locks[0]; // ast.locks.remove(0)
        locks.RemoveAt(0);
        Assert.Equal<LockType>(@lock.LockType, new LockType.Share());
        Assert.Equal(
            @lock.Of!,
            ObjectName.From(Sequence.Of(new Ident("school", null, Span.Empty()))));
        Assert.Null(@lock.Nonblock);
        @lock = locks[0]; // ast.locks.remove(0)
        locks.RemoveAt(0);
        Assert.Equal<LockType>(@lock.LockType, new LockType.Update());
        Assert.Equal(
            @lock.Of!,
            ObjectName.From(Sequence.Of(new Ident("student", null, Span.Empty()))));
        Assert.Null(@lock.Nonblock);
    }

    [Fact]
    public void test_lock_nonblock()
    {
        var sql = "SELECT * FROM student WHERE id = '1' FOR UPDATE OF school SKIP LOCKED";
        var locks = VerifiedQuery(sql).Locks.ToList();
        Assert.Equal(1, locks.Count);
        var @lock = locks[locks.Count - 1]; // ast.locks.pop().unwrap()
        locks.RemoveAt(locks.Count - 1);
        Assert.Equal<LockType>(@lock.LockType, new LockType.Update());
        Assert.Equal(
            @lock.Of!,
            ObjectName.From(Sequence.Of(new Ident("school", null, Span.Empty()))));
        Assert.Equal<NonBlock>(@lock.Nonblock!, new NonBlock.SkipLocked());

        sql = "SELECT * FROM student WHERE id = '1' FOR SHARE OF school NOWAIT";
        locks = VerifiedQuery(sql).Locks.ToList();
        Assert.Equal(1, locks.Count);
        @lock = locks[locks.Count - 1]; // ast.locks.pop().unwrap()
        locks.RemoveAt(locks.Count - 1);
        Assert.Equal<LockType>(@lock.LockType, new LockType.Share());
        Assert.Equal(
            @lock.Of!,
            ObjectName.From(Sequence.Of(new Ident("school", null, Span.Empty()))));
        Assert.Equal<NonBlock>(@lock.Nonblock!, new NonBlock.Nowait());
    }

    [Fact]
    public void test_placeholder()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new DuckDbDialect(),
            new PostgreSqlDialect(),
            new MsSqlDialect(),
            new AnsiDialect(),
            new BigQueryDialect(),
            new SnowflakeDialect(),
            // Note: `$` is the starting word for the HiveDialect identifier
            // Box::new(sqlparser::dialect::HiveDialect {}),
        });
        var sql = "SELECT * FROM student WHERE id = $Id1";
        var ast = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            ast.Selection,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("id")),
                Op: new BinaryOperator.Eq(),
                Right: new Expr.Value(
                    new Value.Placeholder("$Id1").WithEmptySpan())));

        var ast2 = dialects.VerifiedQuery("SELECT * FROM student LIMIT $1 OFFSET $2");
        var expectedLimitClause = new LimitClause.LimitOffset(
            Limit: new Expr.Value(
                new Value.Placeholder("$1").WithEmptySpan()),
            Offset: new Offset(
                Value: new Expr.Value(new Value.Placeholder("$2").WithEmptySpan()),
                Rows: new OffsetRows.None()),
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal<LimitClause?>(ast2.LimitClause, expectedLimitClause);

        dialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new DuckDbDialect(),
            // Note: `?` is for jsonb operators in PostgreSqlDialect
            // Box::new(PostgreSqlDialect {}),
            new MsSqlDialect(),
            new AnsiDialect(),
            new BigQueryDialect(),
            new SnowflakeDialect(),
            // Note: `$` is the starting word for the HiveDialect identifier
            // Box::new(sqlparser::dialect::HiveDialect {}),
        });
        sql = "SELECT * FROM student WHERE id = ?";
        ast = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            ast.Selection,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("id")),
                Op: new BinaryOperator.Eq(),
                Right: new Expr.Value(
                    new Value.Placeholder("?").WithEmptySpan())));

        sql = "SELECT $fromage_français, :x, ?123";
        ast = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            ast.Projection,
            Sequence.Of<SelectItem>(
                new SelectItem.UnnamedExpr(new Expr.Value(
                    new Value.Placeholder("$fromage_français").WithEmptySpan())),
                new SelectItem.UnnamedExpr(new Expr.Value(
                    new Value.Placeholder(":x").WithEmptySpan())),
                new SelectItem.UnnamedExpr(new Expr.Value(
                    new Value.Placeholder("?123").WithEmptySpan()))));
    }

    [Fact]
    public void all_keywords_sorted()
    {
        // assert!(ALL_KEYWORDS.is_sorted())
        var copy = new List<string>(Keywords.AllKeywords);
        copy.Sort(string.CompareOrdinal); // sort_unstable em &str: ordem de bytes
        Assert.Equal(copy, Keywords.AllKeywords);
    }

    private static Sequence<Statement> ParseSqlStatements(string sql) =>
        TestUtils.AllDialects().ParseSqlStatements(sql);

    private static Statement OneStatementParsesTo(string sql, string canonical) =>
        TestUtils.AllDialects().OneStatementParsesTo(sql, canonical);

    private static TestedDialects NonLeftAssociativeDialects() =>
        TestUtils.AllDialectsWhere(d => !d.SupportsLeftAssociativeJoinsWithoutParens);

    private static Statement VerifiedStmt(string query) =>
        TestUtils.AllDialects().VerifiedStmt(query);

    private static Query VerifiedQuery(string query) =>
        TestUtils.AllDialects().VerifiedQuery(query);

    private static Select VerifiedOnlySelect(string query) =>
        TestUtils.AllDialects().VerifiedOnlySelect(query);

    private static Expr VerifiedExpr(string query) =>
        TestUtils.AllDialects().VerifiedExpr(query);

    [Fact]
    public void parse_offset_and_limit()
    {
        var sql = "SELECT foo FROM bar LIMIT 1 OFFSET 2";
        LimitClause? expectedLimitClause = new LimitClause.LimitOffset(
            Limit: Expr.MakeValue(TestUtils.Number("1")),
            Offset: new Offset(
                Value: Expr.MakeValue(TestUtils.Number("2")),
                Rows: new OffsetRows.None()),
            LimitBy: Sequence<Expr>.Empty);
        var ast = VerifiedQuery(sql);
        Assert.Equal(ast.LimitClause, expectedLimitClause);

        // different order is OK
        OneStatementParsesTo("SELECT foo FROM bar OFFSET 2 LIMIT 1", sql);

        // mysql syntax is ok for some dialects
        TestUtils.AllDialectsWhere(d => d.SupportsLimitComma)
            .VerifiedQuery("SELECT foo FROM bar LIMIT 2, 1");

        // expressions are allowed
        sql = "SELECT foo FROM bar LIMIT 1 + 2 OFFSET 3 * 4";
        ast = VerifiedQuery(sql);
        var expectedLimitClause2 = new LimitClause.LimitOffset(
            Limit: new Expr.BinaryOp(
                Left: Expr.MakeValue(TestUtils.Number("1")),
                Op: new BinaryOperator.Plus(),
                Right: Expr.MakeValue(TestUtils.Number("2"))),
            Offset: new Offset(
                Value: new Expr.BinaryOp(
                    Left: Expr.MakeValue(TestUtils.Number("3")),
                    Op: new BinaryOperator.Multiply(),
                    Right: Expr.MakeValue(TestUtils.Number("4"))),
                Rows: new OffsetRows.None()),
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal<LimitClause?>(ast.LimitClause, expectedLimitClause2);

        // OFFSET without LIMIT
        VerifiedStmt("SELECT foo FROM bar OFFSET 2");

        // Can't repeat OFFSET / LIMIT
        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT foo FROM bar OFFSET 2 OFFSET 2"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: OFFSET"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT foo FROM bar LIMIT 2 LIMIT 2"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: LIMIT"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT foo FROM bar OFFSET 2 LIMIT 2 OFFSET 2"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: OFFSET"),
            ex.Error);
    }

    [Fact]
    public void parse_time_functions()
    {
        static void TestTimeFunction(string funcName)
        {
            var sql = $"SELECT {funcName}()";
            var select = VerifiedOnlySelect(sql);
            var selectLocaltimeFuncCallAst = new Function(
                Name: ObjectName.From(Sequence.Of(new Ident(funcName))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: null,
                    Args: Sequence<FunctionArg>.Empty,
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty);
            Assert.Equal<Expr>(
                new Expr.Function(selectLocaltimeFuncCallAst),
                TestUtils.ExprFromProjection(select.Projection[0]));

            // Validating Parenthesis
            var sqlWithoutParens = $"SELECT {funcName}";
            var astWithoutParens = selectLocaltimeFuncCallAst with { Args = new FunctionArguments.None() };
            Assert.Equal<Expr>(
                new Expr.Function(astWithoutParens),
                TestUtils.ExprFromProjection(VerifiedOnlySelect(sqlWithoutParens).Projection[0]));
        }

        TestTimeFunction("CURRENT_TIMESTAMP");
        TestTimeFunction("CURRENT_TIME");
        TestTimeFunction("CURRENT_DATE");
        TestTimeFunction("LOCALTIME");
        TestTimeFunction("LOCALTIMESTAMP");
    }

    [Fact]
    public void parse_position()
    {
        Assert.Equal<Expr>(
            new Expr.Position(
                Expr: new Expr.Value(
                    new Value.SingleQuotedString("@").WithEmptySpan()),
                In: new Expr.Identifier(new Ident("field"))),
            VerifiedExpr("POSITION('@' IN field)"));

        // some dialects (e.g. snowflake) support position as a function call (i.e. without IN)
        Assert.Equal(
            TestUtils.Call(
                "position",
                new Expr[]
                {
                    new Expr.Value(new Value.SingleQuotedString("an").WithEmptySpan()),
                    new Expr.Value(new Value.SingleQuotedString("banana").WithEmptySpan()),
                    Expr.MakeValue(TestUtils.Number("1")),
                }),
            VerifiedExpr("position('an', 'banana', 1)"));
    }

    [Fact]
    public void parse_position_negative()
    {
        // Dialects that accept an unparenthesized IN right-hand side (e.g. ClickHouse)
        // report a different error here, so exclude them.
        var sql = "SELECT POSITION(foo IN) from bar";
        var ex = Assert.Throws<ParserException>(() =>
            TestUtils.AllDialectsExcept(d => d.SupportsInUnparenthesizedExpr).ParseSqlStatements(sql));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: (, found: )"),
            ex.Error);

        var resultUnparenthesized = Assert.Throws<ParserException>(() =>
            TestUtils.AllDialectsWhere(d => d.SupportsInUnparenthesizedExpr).ParseSqlStatements(sql));

        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: an expression, found: )"),
            resultUnparenthesized.Error);
    }

    [Fact]
    public void parse_is_boolean()
    {
        var sql = "a IS TRUE";
        Assert.Equal<Expr>(
            new Expr.IsTrue(new Expr.Identifier(new Ident("a"))),
            VerifiedExpr(sql));

        sql = "a IS NOT TRUE";
        Assert.Equal<Expr>(
            new Expr.IsNotTrue(new Expr.Identifier(new Ident("a"))),
            VerifiedExpr(sql));

        sql = "a IS FALSE";
        Assert.Equal<Expr>(
            new Expr.IsFalse(new Expr.Identifier(new Ident("a"))),
            VerifiedExpr(sql));

        sql = "a IS NOT FALSE";
        Assert.Equal<Expr>(
            new Expr.IsNotFalse(new Expr.Identifier(new Ident("a"))),
            VerifiedExpr(sql));

        sql = "a IS NORMALIZED";
        Assert.Equal<Expr>(
            new Expr.IsNormalized(
                Expr: new Expr.Identifier(new Ident("a")),
                Form: null,
                Negated: false),
            VerifiedExpr(sql));

        sql = "a IS NOT NORMALIZED";
        Assert.Equal<Expr>(
            new Expr.IsNormalized(
                Expr: new Expr.Identifier(new Ident("a")),
                Form: null,
                Negated: true),
            VerifiedExpr(sql));

        sql = "a IS NFKC NORMALIZED";
        Assert.Equal<Expr>(
            new Expr.IsNormalized(
                Expr: new Expr.Identifier(new Ident("a")),
                Form: new NormalizationForm.NFKC(),
                Negated: false),
            VerifiedExpr(sql));

        sql = "a IS NOT NFKD NORMALIZED";
        Assert.Equal<Expr>(
            new Expr.IsNormalized(
                Expr: new Expr.Identifier(new Ident("a")),
                Form: new NormalizationForm.NFKD(),
                Negated: true),
            VerifiedExpr(sql));

        sql = "a IS UNKNOWN";
        Assert.Equal<Expr>(
            new Expr.IsUnknown(new Expr.Identifier(new Ident("a"))),
            VerifiedExpr(sql));

        sql = "a IS NOT UNKNOWN";
        Assert.Equal<Expr>(
            new Expr.IsNotUnknown(new Expr.Identifier(new Ident("a"))),
            VerifiedExpr(sql));

        VerifiedStmt("SELECT f FROM foo WHERE field IS TRUE");
        VerifiedStmt("SELECT f FROM foo WHERE field IS NOT TRUE");

        VerifiedStmt("SELECT f FROM foo WHERE field IS FALSE");
        VerifiedStmt("SELECT f FROM foo WHERE field IS NOT FALSE");

        VerifiedStmt("SELECT f FROM foo WHERE field IS NORMALIZED");
        VerifiedStmt("SELECT f FROM foo WHERE field IS NFC NORMALIZED");
        VerifiedStmt("SELECT f FROM foo WHERE field IS NFD NORMALIZED");
        VerifiedStmt("SELECT f FROM foo WHERE field IS NOT NORMALIZED");
        VerifiedStmt("SELECT f FROM foo WHERE field IS NOT NFKC NORMALIZED");

        VerifiedStmt("SELECT f FROM foo WHERE field IS UNKNOWN");
        VerifiedStmt("SELECT f FROM foo WHERE field IS NOT UNKNOWN");

        foreach (var sql2 in new[]
        {
            "SELECT f from foo where field is 0",
            "SELECT s, s IS XYZ NORMALIZED FROM foo",
            "SELECT s, s IS NFKC FROM foo",
            "SELECT s, s IS TRIM(' NFKC ') FROM foo",
        })
        {
            // assert!(parse_sql_statements(sql).is_err(), "expected a parse failure for `{sql}`")
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql2));
        }
    }

    [Fact]
    public void parse_is_json_predicate()
    {
        // Assert the full AST once for a case that exercises every field.
        var sql = "a IS NOT JSON OBJECT WITHOUT UNIQUE KEYS";
        Assert.Equal<Expr>(
            new Expr.IsJson(
                Expr: new Expr.Identifier(new Ident("a")),
                Kind: new JsonPredicateType.Object(),
                UniqueKeys: new JsonKeyUniqueness.WithoutUniqueKeys(),
                Negated: true),
            VerifiedExpr(sql));

        // The remaining forms only need to round-trip.
        VerifiedExpr("a IS JSON");
        VerifiedExpr("a IS NOT JSON");
        VerifiedExpr("a IS JSON VALUE");
        VerifiedExpr("a IS JSON SCALAR");
        VerifiedExpr("a IS JSON ARRAY");
        VerifiedExpr("a IS JSON OBJECT");
        VerifiedExpr("a IS JSON WITH UNIQUE KEYS");
        VerifiedExpr("a IS JSON WITHOUT UNIQUE KEYS");

        TestUtils.AllDialects().ExprParsesTo("a IS JSON WITH UNIQUE", "a IS JSON WITH UNIQUE KEYS");
        TestUtils.AllDialects().ExprParsesTo("a IS JSON WITHOUT UNIQUE", "a IS JSON WITHOUT UNIQUE KEYS");

        Assert.True(VerifiedExpr("NOT a IS JSON") is Expr.UnaryOp { Op: UnaryOperator.Not, Expr: Expr.IsJson });
    }

    [Fact]
    public void parse_is_json_predicate_invalid()
    {
        var dialects = TestUtils.AllDialects();

        var invalid = new[]
        {
            "SELECT * FROM t WHERE a IS JSON WITH FROM",
            "SELECT * FROM t WHERE a IS JSON WITH KEYS",
            "SELECT * FROM t WHERE a IS JSON WITHOUT FROM",
            "SELECT * FROM t WHERE a IS JSON WITHOUT KEYS",
            "SELECT * FROM t WHERE a IS NOT JSON WITH FROM",
            "SELECT * FROM t WHERE a IS JSON VALUE ARRAY",
            "SELECT * FROM t WHERE a IS JSON OBJECT VALUE",
            "SELECT * FROM t WHERE a IS JSON WITH UNIQUE EXTRA",
            "SELECT * FROM t WHERE a IS JSON WITH UNIQUE KEYS EXTRA",
            "SELECT * FROM t WHERE a IS JSON WITHOUT UNIQUE EXTRA",
            "SELECT * FROM t WHERE a IS JSON WITHOUT UNIQUE KEYS EXTRA",
            "SELECT * FROM t WHERE a IS JSON WITH UNIQUE KEYS WITH UNIQUE KEYS",
            "SELECT * FROM t WHERE a IS JSON WITHOUT UNIQUE KEYS WITHOUT UNIQUE KEYS",
        };

        foreach (var sql in invalid)
        {
            // assert!(dialects.parse_sql_statements(sql).is_err(), "expected a parse failure for `{sql}`")
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
        }
    }

    [Fact]
    public void parse_discard()
    {
        var sql = "DISCARD ALL";
        var discard = Assert.IsType<Statement.Discard>(VerifiedStmt(sql));
        Assert.Equal<DiscardObject>(discard.ObjectType, new DiscardObject.ALL());

        sql = "DISCARD PLANS";
        discard = Assert.IsType<Statement.Discard>(VerifiedStmt(sql));
        Assert.Equal<DiscardObject>(discard.ObjectType, new DiscardObject.PLANS());

        sql = "DISCARD SEQUENCES";
        discard = Assert.IsType<Statement.Discard>(VerifiedStmt(sql));
        Assert.Equal<DiscardObject>(discard.ObjectType, new DiscardObject.SEQUENCES());

        sql = "DISCARD TEMP";
        discard = Assert.IsType<Statement.Discard>(VerifiedStmt(sql));
        Assert.Equal<DiscardObject>(discard.ObjectType, new DiscardObject.TEMP());
    }

    [Fact]
    public void parse_cursor()
    {
        var sql = "CLOSE my_cursor";
        var close = Assert.IsType<Statement.Close>(VerifiedStmt(sql));
        Assert.Equal<CloseCursor>(
            close.Cursor,
            new CloseCursor.Specific(
                Name: new Ident("my_cursor")));

        sql = "CLOSE ALL";
        close = Assert.IsType<Statement.Close>(VerifiedStmt(sql));
        Assert.Equal<CloseCursor>(close.Cursor, new CloseCursor.All());
    }

    [Fact]
    public void parse_show_functions()
    {
        Assert.Equal<Statement>(
            VerifiedStmt("SHOW FUNCTIONS LIKE 'pattern'"),
            new Statement.ShowFunctions(
                Filter: new ShowStatementFilter.Like("pattern")));
    }

    [Fact]
    public void parse_cache_table()
    {
        var sql = "SELECT a, b, c FROM foo";
        var cacheTableName = "cache_table_name";
        var tableFlag = "flag";
        var query = TestUtils.AllDialects().VerifiedQuery(sql);

        Assert.Equal<Statement>(
            VerifiedStmt($"CACHE TABLE '{cacheTableName}'"),
            new Statement.Cache(
                TableFlag: null,
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', cacheTableName))),
                HasAs: false,
                Options: Sequence<SqlOption>.Empty,
                Query: null));

        Assert.Equal<Statement>(
            VerifiedStmt($"CACHE {tableFlag} TABLE '{cacheTableName}'"),
            new Statement.Cache(
                TableFlag: ObjectName.From(Sequence.Of(new Ident(tableFlag))),
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', cacheTableName))),
                HasAs: false,
                Options: Sequence<SqlOption>.Empty,
                Query: null));

        Assert.Equal<Statement>(
            VerifiedStmt(
                $"CACHE {tableFlag} TABLE '{cacheTableName}' OPTIONS('K1' = 'V1', 'K2' = 0.88)"),
            new Statement.Cache(
                TableFlag: ObjectName.From(Sequence.Of(new Ident(tableFlag))),
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', cacheTableName))),
                HasAs: false,
                Options: Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "K1"),
                        Value: new Expr.Value(new Value.SingleQuotedString("V1").WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "K2"),
                        Value: Expr.MakeValue(TestUtils.Number("0.88")))),
                Query: null));

        Assert.Equal<Statement>(
            VerifiedStmt(
                $"CACHE {tableFlag} TABLE '{cacheTableName}' OPTIONS('K1' = 'V1', 'K2' = 0.88) {sql}"),
            new Statement.Cache(
                TableFlag: ObjectName.From(Sequence.Of(new Ident(tableFlag))),
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', cacheTableName))),
                HasAs: false,
                Options: Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "K1"),
                        Value: new Expr.Value(new Value.SingleQuotedString("V1").WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "K2"),
                        Value: Expr.MakeValue(TestUtils.Number("0.88")))),
                Query: query));

        Assert.Equal<Statement>(
            VerifiedStmt(
                $"CACHE {tableFlag} TABLE '{cacheTableName}' OPTIONS('K1' = 'V1', 'K2' = 0.88) AS {sql}"),
            new Statement.Cache(
                TableFlag: ObjectName.From(Sequence.Of(new Ident(tableFlag))),
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', cacheTableName))),
                HasAs: true,
                Options: Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "K1"),
                        Value: new Expr.Value(new Value.SingleQuotedString("V1").WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "K2"),
                        Value: Expr.MakeValue(TestUtils.Number("0.88")))),
                Query: query));

        Assert.Equal<Statement>(
            VerifiedStmt($"CACHE {tableFlag} TABLE '{cacheTableName}' {sql}"),
            new Statement.Cache(
                TableFlag: ObjectName.From(Sequence.Of(new Ident(tableFlag))),
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', cacheTableName))),
                HasAs: false,
                Options: Sequence<SqlOption>.Empty,
                Query: query));

        Assert.Equal<Statement>(
            VerifiedStmt($"CACHE {tableFlag} TABLE '{cacheTableName}' AS {sql}"),
            new Statement.Cache(
                TableFlag: ObjectName.From(Sequence.Of(new Ident(tableFlag))),
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', cacheTableName))),
                HasAs: true,
                Options: Sequence<SqlOption>.Empty,
                Query: query));

        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements("CACHE TABLE 'table_name' foo"));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: foo"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("CACHE flag TABLE 'table_name' OPTIONS('K1'='V1') foo"));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: foo"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("CACHE TABLE 'table_name' AS foo"));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: foo"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("CACHE flag TABLE 'table_name' OPTIONS('K1'='V1') AS foo"));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: foo"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("CACHE 'table_name'"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: a `TABLE` keyword, found: 'table_name'"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("CACHE 'table_name' OPTIONS('K1'='V1')"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: a `TABLE` keyword, found: OPTIONS"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("CACHE flag 'table_name' OPTIONS('K1'='V1')"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: a `TABLE` keyword, found: 'table_name'"),
            ex.Error);
    }

    [Fact]
    public void parse_uncache_table()
    {
        Assert.Equal<Statement>(
            VerifiedStmt("UNCACHE TABLE 'table_name'"),
            new Statement.UNCache(
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', "table_name"))),
                IfExists: false));

        Assert.Equal<Statement>(
            VerifiedStmt("UNCACHE TABLE IF EXISTS 'table_name'"),
            new Statement.UNCache(
                TableName: ObjectName.From(Sequence.Of(Ident.WithQuote('\'', "table_name"))),
                IfExists: true));

        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements("UNCACHE TABLE 'table_name' foo"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: foo"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("UNCACHE 'table_name' foo"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: TABLE, found: 'table_name'"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => ParseSqlStatements("UNCACHE IF EXISTS 'table_name' foo"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: TABLE, found: IF"),
            ex.Error);
    }

    [Fact]
    public void parse_deeply_nested_parens_hits_recursion_limits()
    {
        var sql = new string('(', 1000);
        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements(sql));
        Assert.Equal(new ParserError.RecursionLimitExceeded(), ex.Error);
    }

    [Fact]
    public void parse_update_deeply_nested_parens_hits_recursion_limits()
    {
        var sql = "\nUPDATE\n\n\n\n\n\n\n\n\n\n" + new string('(', 1000);
        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements(sql));
        Assert.Equal(new ParserError.RecursionLimitExceeded(), ex.Error);
    }

    [Fact]
    public void parse_deeply_nested_unary_op_hits_recursion_limits()
    {
        var sql = $"SELECT {new string('+', 1000)}";
        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements(sql));
        Assert.Equal(new ParserError.RecursionLimitExceeded(), ex.Error);
    }

    [Fact]
    public void parse_deeply_nested_expr_hits_recursion_limits()
    {
        var dialect = new GenericDialect();

        var whereClause = MakeWhereClause(100);
        var sql = $"SELECT id, user_id FROM test WHERE {whereClause}";

        var ex = Assert.Throws<ParserException>(() => new Parser(dialect)
            .TryWithSql(sql) // .expect("tokenize to work")
            .ParseStatements());

        Assert.Equal(new ParserError.RecursionLimitExceeded(), ex.Error);
    }

    [Fact]
    public void parse_deeply_nested_subquery_expr_hits_recursion_limits()
    {
        var dialect = new GenericDialect();

        var whereClause = MakeWhereClause(100);
        var sql = $"SELECT id, user_id where id IN (select id from t WHERE {whereClause})";

        var ex = Assert.Throws<ParserException>(() => new Parser(dialect)
            .TryWithSql(sql) // .expect("tokenize to work")
            .ParseStatements());

        Assert.Equal(new ParserError.RecursionLimitExceeded(), ex.Error);
    }

    [Fact]
    public void parse_deeply_nested_interval_hits_recursion_limits()
    {
        var dialect = new GenericDialect();

        var sql = $"SELECT {string.Concat(Enumerable.Repeat("INTERVAL ", 1000))}1";

        var ex = Assert.Throws<ParserException>(() => new Parser(dialect)
            .TryWithSql(sql) // .expect("tokenize to work")
            .ParseStatements());

        Assert.Equal(new ParserError.RecursionLimitExceeded(), ex.Error);
    }

    [Fact]
    public void parse_deeply_nested_data_type_hits_recursion_limits()
    {
        var dialect = new GenericDialect();

        var sql =
            $"SELECT CAST(x AS {string.Concat(Enumerable.Repeat("ARRAY<", 1000))}INT64{new string('>', 1000)})";

        var ex = Assert.Throws<ParserException>(() => new Parser(dialect)
            .TryWithSql(sql) // .expect("tokenize to work")
            .ParseStatements());

        Assert.Equal(new ParserError.RecursionLimitExceeded(), ex.Error);
    }

    [Fact]
    public void parse_with_recursion_limit()
    {
        var dialect = new GenericDialect();

        var whereClause = MakeWhereClause(20);
        var sql = $"SELECT id, user_id FROM test WHERE {whereClause}";

        // Expect the statement to parse with default limit
        // assert!(res.is_ok(), "{res:?}")
        _ = new Parser(dialect)
            .TryWithSql(sql) // .expect("tokenize to work")
            .ParseStatements();

        // limit recursion to something smaller, expect parsing to fail
        var ex = Assert.Throws<ParserException>(() => new Parser(dialect)
            .TryWithSql(sql) // .expect("tokenize to work")
            .WithRecursionLimit(20)
            .ParseStatements());

        Assert.Equal(new ParserError.RecursionLimitExceeded(), ex.Error);

        // limit recursion to 50, expect it to succeed
        // assert!(res.is_ok(), "{res:?}")
        _ = new Parser(dialect)
            .TryWithSql(sql) // .expect("tokenize to work")
            .WithRecursionLimit(50)
            .ParseStatements();
    }

    [Fact]
    public void parse_escaped_string_with_unescape()
    {
        static void AssertMysqlQueryValue(TestedDialects dialects, string sql, string quoted)
        {
            switch (dialects.OneStatementParsesTo(sql, ""))
            {
                case Statement.Query { Value: var query }:
                    switch (query.Body)
                    {
                        case SetExpr.Select { Value: var value }:
                            var expr = TestUtils.ExprFromProjection(TestUtils.Only(value.Projection));
                            Assert.Equal<Expr>(
                                expr,
                                new Expr.Value(
                                    new Value.SingleQuotedString(quoted).WithEmptySpan()));
                            break;
                        default:
                            throw new InvalidOperationException("unreachable");
                    }
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }

        var escapingDialects =
            TestUtils.AllDialectsWhere(dialect => dialect.SupportsStringLiteralBackslashEscape);
        var noWildcardException = TestUtils.AllDialectsWhere(dialect =>
            dialect.SupportsStringLiteralBackslashEscape && !dialect.IgnoresWildcardEscapes);
        var withWildcardException = TestUtils.AllDialectsWhere(dialect =>
            dialect.SupportsStringLiteralBackslashEscape && dialect.IgnoresWildcardEscapes);

        var sql = @"SELECT 'I\'m fine'";
        AssertMysqlQueryValue(escapingDialects, sql, "I'm fine");

        sql = "SELECT 'I''m fine'";
        AssertMysqlQueryValue(escapingDialects, sql, "I'm fine");

        sql = @"SELECT 'I\""m fine'";
        AssertMysqlQueryValue(escapingDialects, sql, "I\"m fine");

        sql = @"SELECT 'Testing: \0 \\ \% \_ \b \n \r \t \Z \a \h \ '";
        // Rust: "Testing: \0 \\ % _ \u{8} \n \r \t \u{1a} \u{7} h  "
        AssertMysqlQueryValue(
            noWildcardException,
            sql,
            "Testing: \0 \\ % _ \b \n \r \t \x001A \a h  ");

        // check MySQL doesn't remove backslash from escaped LIKE wildcards
        // Rust: "Testing: \0 \\ \\% \\_ \u{8} \n \r \t \u{1a} \u{7} h  "
        AssertMysqlQueryValue(
            withWildcardException,
            sql,
            "Testing: \0 \\ \\% \\_ \b \n \r \t \x001A \a h  ");
    }

    [Fact]
    public void parse_escaped_string_without_unescape()
    {
        static void AssertMysqlQueryValue(string sql, string quoted)
        {
            var stmt = TestedDialects.NewWithOptions(
                    new List<Dialect>
                    {
                        new MySqlDialect(),
                        new BigQueryDialect(),
                        new SnowflakeDialect(),
                    },
                    new ParserOptions().WithUnescape(false))
                .OneStatementParsesTo(sql, "");

            switch (stmt)
            {
                case Statement.Query { Value: var query }:
                    switch (query.Body)
                    {
                        case SetExpr.Select { Value: var value }:
                            var expr = TestUtils.ExprFromProjection(TestUtils.Only(value.Projection));
                            Assert.Equal<Expr>(
                                expr,
                                new Expr.Value(
                                    new Value.SingleQuotedString(quoted).WithEmptySpan()));
                            break;
                        default:
                            throw new InvalidOperationException("unreachable");
                    }
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
        var sql = @"SELECT 'I\'m fine'";
        AssertMysqlQueryValue(sql, @"I\'m fine");

        sql = "SELECT 'I''m fine'";
        AssertMysqlQueryValue(sql, "I''m fine");

        sql = @"SELECT 'I\""m fine'";
        AssertMysqlQueryValue(sql, @"I\""m fine");

        sql = @"SELECT 'Testing: \0 \\ \% \_ \b \n \r \t \Z \a \ '";
        AssertMysqlQueryValue(sql, @"Testing: \0 \\ \% \_ \b \n \r \t \Z \a \ ");
    }

    [Fact]
    public void parse_pivot_table()
    {
        var sql = string.Concat(
            "SELECT * FROM monthly_sales AS a PIVOT(",
            "SUM(a.amount), ",
            "SUM(b.amount) AS t, ",
            "SUM(c.amount) AS u ",
            "FOR a.MONTH IN (1 AS x, 'two', three AS y)) AS p (c, d) ",
            "ORDER BY EMPID");

        static ExprWithAlias ExpectedFunction(string table, string? alias) =>
            new ExprWithAlias(
                Expr: TestUtils.Call(
                    "SUM",
                    new Expr[]
                    {
                        new Expr.CompoundIdentifier(Sequence.Of(
                            new Ident(table),
                            new Ident("amount"))),
                    }),
                Alias: alias is null ? null : new Ident(alias));

        Assert.Equal<TableFactor>(
            VerifiedOnlySelect(sql).From[0].Relation,
            new TableFactor.Pivot(
                Table: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("monthly_sales"))),
                    Alias: TestUtils.TableAlias(true, "a"),
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    Partitions: Sequence<Ident>.Empty,
                    WithOrdinality: false,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                AggregateFunctions: Sequence.Of(
                    ExpectedFunction("a", null),
                    ExpectedFunction("b", "t"),
                    ExpectedFunction("c", "u")),
                ValueColumn: Sequence.Of<Expr>(new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("a"),
                    new Ident("MONTH")))),
                ValueSource: new PivotValueSource.List(Sequence.Of(
                    new ExprWithAlias(
                        Expr: Expr.MakeValue(TestUtils.Number("1")),
                        Alias: new Ident("x")),
                    new ExprWithAlias(
                        Expr: new Expr.Value(
                            new Value.SingleQuotedString("two").WithEmptySpan()),
                        Alias: null),
                    new ExprWithAlias(
                        Expr: new Expr.Identifier(new Ident("three")),
                        Alias: new Ident("y")))),
                DefaultOnNull: null,
                Alias: new TableAlias(
                    Explicit: true,
                    Name: new Ident("p", null, Span.Empty()),
                    Columns: Sequence.Of(
                        TableAliasColumnDef.FromName("c"),
                        TableAliasColumnDef.FromName("d")),
                    At: null)));
        Assert.Equal(VerifiedStmt(sql).ToString(), sql);

        // parsing should succeed with empty alias
        var sqlWithoutTableAlias = string.Concat(
            "SELECT * FROM monthly_sales ",
            "PIVOT(SUM(a.amount) FOR a.MONTH IN ('JAN', 'FEB', 'MAR', 'APR')) AS p (c, d) ",
            "ORDER BY EMPID");
        Assert.True(
            VerifiedOnlySelect(sqlWithoutTableAlias).From[0].Relation
                is TableFactor.Pivot { Table: TableFactor.Table { Alias: null } });
        Assert.Equal(
            VerifiedStmt(sqlWithoutTableAlias).ToString(),
            sqlWithoutTableAlias);

        var multipleValueColumnsSql = string.Concat(
            "SELECT * FROM person ",
            "PIVOT(",
            "SUM(age) AS a, AVG(class) AS c ",
            "FOR (name, age) IN (('John', 30) AS c1, ('Mike', 40) AS c2))");

        Assert.Equal<TableFactor>(
            VerifiedOnlySelect(multipleValueColumnsSql).From[0].Relation,
            new TableFactor.Pivot(
                Table: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("person"))),
                    Alias: null,
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    Partitions: Sequence<Ident>.Empty,
                    WithOrdinality: false,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                AggregateFunctions: Sequence.Of(
                    new ExprWithAlias(
                        Expr: TestUtils.Call("SUM", new Expr[] { new Expr.Identifier(new Ident("age")) }),
                        Alias: new Ident("a")),
                    new ExprWithAlias(
                        Expr: TestUtils.Call("AVG", new Expr[] { new Expr.Identifier(new Ident("class")) }),
                        Alias: new Ident("c"))),
                ValueColumn: Sequence.Of<Expr>(
                    new Expr.Identifier(new Ident("name")),
                    new Expr.Identifier(new Ident("age"))),
                ValueSource: new PivotValueSource.List(Sequence.Of(
                    new ExprWithAlias(
                        Expr: new Expr.Tuple(Sequence.Of<Expr>(
                            new Expr.Value(
                                new Value.SingleQuotedString("John").WithEmptySpan()),
                            new Expr.Value(
                                new Value.Number("30", false).WithEmptySpan()))),
                        Alias: new Ident("c1")),
                    new ExprWithAlias(
                        Expr: new Expr.Tuple(Sequence.Of<Expr>(
                            new Expr.Value(
                                new Value.SingleQuotedString("Mike").WithEmptySpan()),
                            new Expr.Value(
                                new Value.Number("40", false).WithEmptySpan()))),
                        Alias: new Ident("c2")))),
                DefaultOnNull: null,
                Alias: null));
        Assert.Equal(
            VerifiedStmt(multipleValueColumnsSql).ToString(),
            multipleValueColumnsSql);

        // assert optional "AS" keyword for aliases for pivot values
        OneStatementParsesTo(
            "SELECT * FROM t PIVOT(SUM(1) FOR a.abc IN (1 x, 'two' y, three z))",
            "SELECT * FROM t PIVOT(SUM(1) FOR a.abc IN (1 AS x, 'two' AS y, three AS z))");

        // assert optional "AS" keyword for aliases for pivot aggregate function
        OneStatementParsesTo(
            "SELECT * FROM t PIVOT(SUM(1) x, COUNT(42) y FOR a.abc IN (1))",
            "SELECT * FROM t PIVOT(SUM(1) AS x, COUNT(42) AS y FOR a.abc IN (1))");
    }

    [Fact]
    public void parse_unpivot_table()
    {
        var sql = string.Concat(
            "SELECT * FROM sales AS s ",
            "UNPIVOT(quantity FOR quarter IN (Q1, Q2, Q3, Q4)) AS u (product, quarter, quantity)");
        var baseUnpivot = new TableFactor.Unpivot(
            Table: new TableFactor.Table(
                Name: ObjectName.From(Sequence.Of(new Ident("sales"))),
                Alias: TestUtils.TableAlias(true, "s"),
                Args: null,
                WithHints: Sequence<Expr>.Empty,
                Version: null,
                Partitions: Sequence<Ident>.Empty,
                WithOrdinality: false,
                JsonPath: null,
                Sample: null,
                IndexHints: Sequence<TableIndexHints>.Empty),
            NullInclusion: null,
            Value: new Expr.Identifier(new Ident("quantity")),
            Name: new Ident("quarter"),
            Columns: new[] { "Q1", "Q2", "Q3", "Q4" }
                .Select(col => new ExprWithAlias(
                    Expr: new Expr.Identifier(new Ident(col)),
                    Alias: null))
                .ToSequence(),
            Alias: new TableAlias(
                Explicit: true,
                Name: new Ident("u"),
                Columns: new[] { "product", "quarter", "quantity" }
                    .Select(TableAliasColumnDef.FromName)
                    .ToSequence(),
                At: null));
        Assert.Equal<TableFactor>(VerifiedOnlySelect(sql).From[0].Relation, baseUnpivot);
        Assert.Equal(VerifiedStmt(sql).ToString(), sql);

        var sqlWithoutAliases = string.Concat(
            "SELECT * FROM sales ",
            "UNPIVOT(quantity FOR quarter IN (Q1, Q2, Q3, Q4))");

        Assert.True(
            VerifiedOnlySelect(sqlWithoutAliases).From[0].Relation
                is TableFactor.Unpivot { Alias: null, Table: TableFactor.Table { Alias: null } });
        Assert.Equal(
            VerifiedStmt(sqlWithoutAliases).ToString(),
            sqlWithoutAliases);

        var sqlUnpivotExcludeNulls = string.Concat(
            "SELECT * FROM sales AS s ",
            "UNPIVOT EXCLUDE NULLS (quantity FOR quarter IN (Q1, Q2, Q3, Q4)) AS u (product, quarter, quantity)");

        if (VerifiedOnlySelect(sqlUnpivotExcludeNulls).From[0].Relation
            is TableFactor.Unpivot { NullInclusion: var nullInclusion })
        {
            Assert.Equal<NullInclusion?>(nullInclusion, new NullInclusion.ExcludeNulls());
        }

        Assert.Equal(
            VerifiedStmt(sqlUnpivotExcludeNulls).ToString(),
            sqlUnpivotExcludeNulls);

        var sqlUnpivotIncludeNulls = string.Concat(
            "SELECT * FROM sales AS s ",
            "UNPIVOT INCLUDE NULLS (quantity FOR quarter IN (Q1, Q2, Q3, Q4)) AS u (product, quarter, quantity)");

        if (VerifiedOnlySelect(sqlUnpivotIncludeNulls).From[0].Relation
            is TableFactor.Unpivot { NullInclusion: var nullInclusion2 })
        {
            Assert.Equal<NullInclusion?>(nullInclusion2, new NullInclusion.IncludeNulls());
        }

        Assert.Equal(
            VerifiedStmt(sqlUnpivotIncludeNulls).ToString(),
            sqlUnpivotIncludeNulls);

        var sqlUnpivotWithAlias = string.Concat(
            "SELECT * FROM sales AS s ",
            "UNPIVOT INCLUDE NULLS ",
            "(quantity FOR quarter IN ",
            "(Q1 AS Quater1, Q2 AS Quater2, Q3 AS Quater3, Q4 AS Quater4)) ",
            "AS u (product, quarter, quantity)");

        if (VerifiedOnlySelect(sqlUnpivotWithAlias).From[0].Relation
            is TableFactor.Unpivot { Value: var value, Columns: var columns })
        {
            Assert.Equal(
                columns,
                Sequence.Of(
                    new ExprWithAlias(
                        Expr: new Expr.Identifier(new Ident("Q1")),
                        Alias: new Ident("Quater1")),
                    new ExprWithAlias(
                        Expr: new Expr.Identifier(new Ident("Q2")),
                        Alias: new Ident("Quater2")),
                    new ExprWithAlias(
                        Expr: new Expr.Identifier(new Ident("Q3")),
                        Alias: new Ident("Quater3")),
                    new ExprWithAlias(
                        Expr: new Expr.Identifier(new Ident("Q4")),
                        Alias: new Ident("Quater4"))));
            Assert.Equal<Expr>(value, new Expr.Identifier(new Ident("quantity")));
        }

        Assert.Equal(
            VerifiedStmt(sqlUnpivotWithAlias).ToString(),
            sqlUnpivotWithAlias);

        var sqlUnpivotWithAliasAndMultiValue = string.Concat(
            "SELECT * FROM sales AS s ",
            "UNPIVOT INCLUDE NULLS ((first_quarter, second_quarter) ",
            "FOR half_of_the_year IN (",
            "(Q1, Q2) AS H1, ",
            "(Q3, Q4) AS H2",
            "))");

        if (VerifiedOnlySelect(sqlUnpivotWithAliasAndMultiValue).From[0].Relation
            is TableFactor.Unpivot { Value: var value2, Columns: var columns2 })
        {
            Assert.Equal(
                columns2,
                Sequence.Of(
                    new ExprWithAlias(
                        Expr: new Expr.Tuple(Sequence.Of<Expr>(
                            new Expr.Identifier(new Ident("Q1")),
                            new Expr.Identifier(new Ident("Q2")))),
                        Alias: new Ident("H1")),
                    new ExprWithAlias(
                        Expr: new Expr.Tuple(Sequence.Of<Expr>(
                            new Expr.Identifier(new Ident("Q3")),
                            new Expr.Identifier(new Ident("Q4")))),
                        Alias: new Ident("H2"))));
            Assert.Equal<Expr>(
                value2,
                new Expr.Tuple(Sequence.Of<Expr>(
                    new Expr.Identifier(new Ident("first_quarter")),
                    new Expr.Identifier(new Ident("second_quarter")))));
        }

        Assert.Equal(
            VerifiedStmt(sqlUnpivotWithAliasAndMultiValue).ToString(),
            sqlUnpivotWithAliasAndMultiValue);

        var sqlUnpivotWithAliasAndMultiValueAndQualifier = string.Concat(
            "SELECT * FROM sales AS s ",
            "UNPIVOT INCLUDE NULLS ((first_quarter, second_quarter) ",
            "FOR half_of_the_year IN (",
            "(sales.Q1, sales.Q2) AS H1, ",
            "(sales.Q3, sales.Q4) AS H2",
            "))");

        if (VerifiedOnlySelect(sqlUnpivotWithAliasAndMultiValueAndQualifier).From[0].Relation
            is TableFactor.Unpivot { Columns: var columns3 })
        {
            Assert.Equal(
                columns3,
                Sequence.Of(
                    new ExprWithAlias(
                        Expr: new Expr.Tuple(Sequence.Of<Expr>(
                            new Expr.CompoundIdentifier(Sequence.Of(new Ident("sales"), new Ident("Q1"))),
                            new Expr.CompoundIdentifier(Sequence.Of(new Ident("sales"), new Ident("Q2"))))),
                        Alias: new Ident("H1")),
                    new ExprWithAlias(
                        Expr: new Expr.Tuple(Sequence.Of<Expr>(
                            new Expr.CompoundIdentifier(Sequence.Of(new Ident("sales"), new Ident("Q3"))),
                            new Expr.CompoundIdentifier(Sequence.Of(new Ident("sales"), new Ident("Q4"))))),
                        Alias: new Ident("H2"))));
        }

        Assert.Equal(
            VerifiedStmt(sqlUnpivotWithAliasAndMultiValueAndQualifier).ToString(),
            sqlUnpivotWithAliasAndMultiValueAndQualifier);
    }

    [Fact]
    public void parse_select_table_with_index_hints()
    {
        var supportedDialects = TestUtils.AllDialectsWhere(d => d.SupportsTableHints);
        var s = supportedDialects.VerifiedOnlySelect(
            "SELECT * FROM t1 USE INDEX (i1) IGNORE INDEX FOR ORDER BY (i2) ORDER BY a");
        if (s.From[0].Relation is TableFactor.Table { IndexHints: var indexHints })
        {
            Assert.Equal(
                Sequence.Of(
                    new TableIndexHints(
                        HintType: new TableIndexHintType.Use(),
                        IndexNames: Sequence.Of(new Ident("i1")),
                        IndexType: new TableIndexType.Index(),
                        ForClause: null),
                    new TableIndexHints(
                        HintType: new TableIndexHintType.Ignore(),
                        IndexNames: Sequence.Of(new Ident("i2")),
                        IndexType: new TableIndexType.Index(),
                        ForClause: new TableIndexHintForClause.OrderBy())),
                indexHints);
        }
        else
        {
            throw new InvalidOperationException("Expected TableFactor::Table");
        }
        supportedDialects.VerifiedStmt("SELECT * FROM t1 USE INDEX (i1) USE INDEX (i1, i1)");
        supportedDialects.VerifiedStmt(
            "SELECT * FROM t1 USE INDEX () IGNORE INDEX (i2) USE INDEX (i1) USE INDEX (i2)");
        supportedDialects.VerifiedStmt("SELECT * FROM t1 FORCE INDEX FOR JOIN (i2)");
        supportedDialects.VerifiedStmt("SELECT * FROM t1 IGNORE INDEX FOR JOIN (i2)");
        supportedDialects.VerifiedStmt(
            "SELECT * FROM t USE INDEX (index1) IGNORE INDEX FOR ORDER BY (index1) IGNORE INDEX FOR GROUP BY (index1) WHERE A = B");

        // Test that dialects that don't support table hints will keep parsing the USE as table alias
        var sql = "SELECT * FROM T USE LIMIT 1";
        var unsupportedDialects = TestUtils.AllDialectsWhere(d => !d.SupportsTableHints);
        var select = unsupportedDialects
            .VerifiedOnlySelectWithCanonical(sql, "SELECT * FROM T USE LIMIT 1");
        Assert.Equal(
            select.From,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: new ObjectName(Sequence.Of<ObjectNamePart>(new ObjectNamePart.Identifier(
                        new Ident("T")))),
                    Alias: TestUtils.TableAlias(false, "USE"),
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    Partitions: Sequence<Ident>.Empty,
                    WithOrdinality: false,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Joins: Sequence<Join>.Empty)));
    }

    [Fact]
    public void parse_pivot_unpivot_table()
    {
        var sql = string.Concat(
            "SELECT * FROM census AS c ",
            "UNPIVOT(population FOR year IN (population_2000, population_2010)) AS u ",
            "PIVOT(sum(population) FOR year IN ('population_2000', 'population_2010')) AS p");

        Assert.Equal<TableFactor>(
            VerifiedOnlySelect(sql).From[0].Relation,
            new TableFactor.Pivot(
                Table: new TableFactor.Unpivot(
                    Table: new TableFactor.Table(
                        Name: ObjectName.From(Sequence.Of(new Ident("census"))),
                        Alias: TestUtils.TableAlias(true, "c"),
                        Args: null,
                        WithHints: Sequence<Expr>.Empty,
                        Version: null,
                        Partitions: Sequence<Ident>.Empty,
                        WithOrdinality: false,
                        JsonPath: null,
                        Sample: null,
                        IndexHints: Sequence<TableIndexHints>.Empty),
                    NullInclusion: null,
                    Value: new Expr.Identifier(new Ident("population")),
                    Name: new Ident("year"),
                    Columns: new[] { "population_2000", "population_2010" }
                        .Select(col => new ExprWithAlias(
                            Expr: new Expr.Identifier(new Ident(col)),
                            Alias: null))
                        .ToSequence(),
                    Alias: TestUtils.TableAlias(true, "u")),
                AggregateFunctions: Sequence.Of(new ExprWithAlias(
                    Expr: TestUtils.Call("sum", new Expr[] { new Expr.Identifier(new Ident("population")) }),
                    Alias: null)),
                ValueColumn: Sequence.Of<Expr>(new Expr.Identifier(new Ident("year"))),
                ValueSource: new PivotValueSource.List(Sequence.Of(
                    new ExprWithAlias(
                        Expr: new Expr.Value(
                            new Value.SingleQuotedString("population_2000")
                                .WithEmptySpan()),
                        Alias: null),
                    new ExprWithAlias(
                        Expr: new Expr.Value(
                            new Value.SingleQuotedString("population_2010")
                                .WithEmptySpan()),
                        Alias: null))),
                DefaultOnNull: null,
                Alias: TestUtils.TableAlias(true, "p")));
        Assert.Equal(VerifiedStmt(sql).ToString(), sql);
    }

    /// <summary>Makes a predicate that looks like ((user_id = $id) OR user_id = $2...)</summary>
    private static string MakeWhereClause(int num)
    {
        var output = new System.Text.StringBuilder(new string('(', num - 1));

        for (var i = 0; i < num; i++)
        {
            if (i > 0)
            {
                output.Append(" OR ");
            }
            output.Append("user_id = ").Append(i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (i < num - 1)
            {
                output.Append(')');
            }
        }
        return output.ToString();
    }
}
