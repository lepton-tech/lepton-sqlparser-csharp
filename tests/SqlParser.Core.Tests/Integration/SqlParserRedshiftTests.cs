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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_redshift.rs.
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Notas da tradução (onda-c/C16.md):
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000). `assert_eq!(x, None)` → `Assert.Null(x)` (xUnit2003).
// - `panic!`/`unreachable!` num `match` → `default: throw new InvalidOperationException(..)`.
// - As funções `redshift()` e `redshift_and_generic()` ficam na mesma posição do Rust (depois de
//   `parse_delimited_identifiers`).
#pragma warning disable xUnit2013
public class SqlParserRedshiftTests
{
    [Fact]
    public void test_square_brackets_over_db_schema_table_name()
    {
        var select = Redshift().VerifiedOnlySelect("SELECT [col1] FROM [test_schema].[test_table]");
        Assert.Equal(
            select.Projection[0],
            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident(
                Value: "col1",
                QuoteStyle: '[',
                Span: Span.Empty()))));
        Assert.Equal(
            select.From[0],
            new TableWithJoins(
                Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                    new Ident(
                        Value: "test_schema",
                        QuoteStyle: '[',
                        Span: Span.Empty()),
                    new Ident(
                        Value: "test_table",
                        QuoteStyle: '[',
                        Span: Span.Empty())))),
                Joins: Sequence<Join>.Empty));
    }

    [Fact]
    public void brackets_over_db_schema_table_name_with_whites_paces()
    {
        // match .. { Ok(statements) => .., _ => unreachable!() }: um erro lança ParserException e o teste falha.
        var statements = Redshift().ParseSqlStatements("SELECT [   col1  ] FROM [  test_schema].[ test_table]");
        Assert.Equal(1, statements.Count);
    }

    [Fact]
    public void test_double_quotes_over_db_schema_table_name()
    {
        var select =
            Redshift().VerifiedOnlySelect("SELECT \"col1\" FROM \"test_schema\".\"test_table\"");
        Assert.Equal(
            select.Projection[0],
            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident(
                Value: "col1",
                QuoteStyle: '"',
                Span: Span.Empty()))));
        Assert.Equal(
            select.From[0],
            new TableWithJoins(
                Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                    new Ident(
                        Value: "test_schema",
                        QuoteStyle: '"',
                        Span: Span.Empty()),
                    new Ident(
                        Value: "test_table",
                        QuoteStyle: '"',
                        Span: Span.Empty())))),
                Joins: Sequence<Join>.Empty));
    }

    [Fact]
    public void parse_delimited_identifiers()
    {
        // check that quoted identifiers in any position remain quoted after serialization
        var select = Redshift().VerifiedOnlySelect(
            "SELECT \"alias\".\"bar baz\", \"myfun\"(), \"simple id\" AS \"column alias\" FROM \"a table\" AS \"alias\"");
        // check FROM
        switch (TestUtils.Only(select.From).Relation)
        {
            case TableFactor.Table table:
            {
                // TableFactor::Table { name, alias, args, with_hints, version, .. }
                var name = table.Name;
                var alias = table.Alias;
                var args = table.Args;
                var withHints = table.WithHints;
                var version = table.Version;
                Assert.Equal(
                    ObjectName.From(Sequence.Of(Ident.WithQuote('"', "a table"))),
                    name);
                Assert.Equal(Ident.WithQuote('"', "alias"), alias!.Name);
                Assert.Null(args);
                Assert.True(withHints.IsEmpty);
                Assert.Null(version);
                break;
            }
            default:
                throw new InvalidOperationException("Expecting TableFactor::Table");
        }
        // check SELECT
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            new Expr.CompoundIdentifier(Sequence.Of(
                Ident.WithQuote('"', "alias"),
                Ident.WithQuote('"', "bar baz"))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(Ident.WithQuote('"', "myfun"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: null,
                    Args: Sequence<FunctionArg>.Empty,
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(select.Projection[1]));
        switch (select.Projection[2])
        {
            case SelectItem.ExprWithAlias exprWithAlias:
                Assert.Equal(new Expr.Identifier(Ident.WithQuote('"', "simple id")), exprWithAlias.Expr);
                Assert.Equal(Ident.WithQuote('"', "column alias"), exprWithAlias.Alias);
                break;
            default:
                throw new InvalidOperationException("Expected ExprWithAlias");
        }

        Redshift().VerifiedStmt("CREATE TABLE \"foo\" (\"bar\" \"int\")");
        // An alias starting with a number
        Redshift().VerifiedStmt("CREATE TABLE \"foo\" (\"1\" INT)");
        Redshift().VerifiedStmt("ALTER TABLE foo ADD CONSTRAINT \"bar\" PRIMARY KEY (baz)");
        //TODO verified_stmt(r#"UPDATE foo SET "bar" = 5"#);
    }

    private static TestedDialects Redshift()
    {
        return new TestedDialects(new List<Dialect> { new RedshiftSqlDialect() });
    }

    private static TestedDialects RedshiftAndGeneric()
    {
        return new TestedDialects(new List<Dialect>
        {
            new RedshiftSqlDialect(),
            new GenericDialect(),
        });
    }

    [Fact]
    public void test_sharp()
    {
        var sql = "SELECT #_of_values";
        var select = Redshift().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("#_of_values"))),
            select.Projection[0]);
    }

    [Fact]
    public void test_create_view_with_no_schema_binding()
    {
        RedshiftAndGeneric()
            .VerifiedStmt("CREATE VIEW myevent AS SELECT eventname FROM event WITH NO SCHEMA BINDING");
    }

    [Fact]
    public void test_redshift_json_path()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPartiql);
        var sql = "SELECT cust.c_orders[0].o_orderkey FROM customer_orders_lineitem";
        var select = dialects.VerifiedOnlySelect(sql);

        Assert.Equal(
            new Expr.JsonAccess(
                Value: new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("cust"),
                    new Ident("c_orders"))),
                Path: new JsonPath(
                    Path: Sequence.Of<JsonPathElem>(
                        new JsonPathElem.Bracket(
                            Key: Expr.MakeValue(TestUtils.Number("0"))),
                        new JsonPathElem.Dot(
                            Key: "o_orderkey",
                            Quoted: false)))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT cust.c_orders[0]['id'] FROM customer_orders_lineitem";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.JsonAccess(
                Value: new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("cust"),
                    new Ident("c_orders"))),
                Path: new JsonPath(
                    Path: Sequence.Of<JsonPathElem>(
                        new JsonPathElem.Bracket(
                            Key: Expr.MakeValue(TestUtils.Number("0"))),
                        new JsonPathElem.Bracket(
                            Key: new Expr.Value(
                                new Value.SingleQuotedString("id").WithEmptySpan()))))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT db1.sc1.tbl1.col1[0]['id'] FROM customer_orders_lineitem";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.JsonAccess(
                Value: new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("db1"),
                    new Ident("sc1"),
                    new Ident("tbl1"),
                    new Ident("col1"))),
                Path: new JsonPath(
                    Path: Sequence.Of<JsonPathElem>(
                        new JsonPathElem.Bracket(
                            Key: Expr.MakeValue(TestUtils.Number("0"))),
                        new JsonPathElem.Bracket(
                            Key: new Expr.Value(
                                new Value.SingleQuotedString("id").WithEmptySpan()))))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT db1.sc1.tbl1.col1[0].\"id\" FROM customer_orders_lineitem";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.JsonAccess(
                Value: new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("db1"),
                    new Ident("sc1"),
                    new Ident("tbl1"),
                    new Ident("col1"))),
                Path: new JsonPath(
                    Path: Sequence.Of<JsonPathElem>(
                        new JsonPathElem.Bracket(
                            Key: Expr.MakeValue(TestUtils.Number("0"))),
                        new JsonPathElem.Dot(
                            Key: "id",
                            Quoted: true)))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void test_parse_json_path_from()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPartiql);
        var select = dialects.VerifiedOnlySelect("SELECT * FROM src[0].a AS a");
        switch (select.From[0].Relation)
        {
            case TableFactor.Table { Name: var name, JsonPath: var jsonPath }:
                Assert.Equal(name, ObjectName.From(Sequence.Of(new Ident("src"))));
                Assert.Equal(
                    jsonPath,
                    new JsonPath(
                        Path: Sequence.Of<JsonPathElem>(
                            new JsonPathElem.Bracket(
                                Key: Expr.MakeValue(TestUtils.Number("0"))),
                            new JsonPathElem.Dot(
                                Key: "a",
                                Quoted: false))));
                break;
            default:
                throw new InvalidOperationException("explicit panic");
        }

        select = dialects.VerifiedOnlySelect("SELECT * FROM src[0].a[1].b AS a");
        switch (select.From[0].Relation)
        {
            case TableFactor.Table { Name: var name, JsonPath: var jsonPath }:
                Assert.Equal(name, ObjectName.From(Sequence.Of(new Ident("src"))));
                Assert.Equal(
                    jsonPath,
                    new JsonPath(
                        Path: Sequence.Of<JsonPathElem>(
                            new JsonPathElem.Bracket(
                                Key: Expr.MakeValue(TestUtils.Number("0"))),
                            new JsonPathElem.Dot(
                                Key: "a",
                                Quoted: false),
                            new JsonPathElem.Bracket(
                                Key: new Expr.Value(
                                    new Value.Number("1", false).WithEmptySpan())),
                            new JsonPathElem.Dot(
                                Key: "b",
                                Quoted: false))));
                break;
            default:
                throw new InvalidOperationException("explicit panic");
        }

        select = dialects.VerifiedOnlySelect("SELECT * FROM src.a.b");
        switch (select.From[0].Relation)
        {
            case TableFactor.Table { Name: var name, JsonPath: var jsonPath }:
                Assert.Equal(
                    name,
                    ObjectName.From(Sequence.Of(new Ident("src"), new Ident("a"), new Ident("b"))));
                Assert.Null(jsonPath);
                break;
            default:
                throw new InvalidOperationException("explicit panic");
        }
    }

    [Fact]
    public void test_parse_select_numbered_columns()
    {
        // An alias starting with a number
        RedshiftAndGeneric().VerifiedStmt("SELECT 1 AS \"1\" FROM a");
        RedshiftAndGeneric().VerifiedStmt("SELECT 1 AS \"1abc\" FROM a");
    }

    [Fact]
    public void test_parse_nested_quoted_identifier()
    {
        Redshift().VerifiedStmt("SELECT 1 AS [\"1\"] FROM a");
        Redshift().VerifiedStmt("SELECT 1 AS [\"[=\"] FROM a");
        Redshift().VerifiedStmt("SELECT 1 AS [\"=]\"] FROM a");
        Redshift().VerifiedStmt("SELECT 1 AS [\"a[b]\"] FROM a");
        // trim spaces
        Redshift().OneStatementParsesTo("SELECT 1 AS [ \" 1 \" ]", "SELECT 1 AS [\" 1 \"]");
        // invalid query
        Assert.Throws<ParserException>(() => Redshift()
            .ParseSqlStatements("SELECT 1 AS [\"1]"));
    }

    [Fact]
    public void parse_extract_single_quotes()
    {
        var sql = "SELECT EXTRACT('month' FROM my_timestamp) FROM my_table";
        Redshift().VerifiedStmt(sql);
    }

    [Fact]
    public void parse_string_literal_backslash_escape()
    {
        Redshift().OneStatementParsesTo(@"SELECT 'l\'auto'", "SELECT 'l''auto'");
    }

    [Fact]
    public void parse_utf8_multibyte_idents()
    {
        Redshift().VerifiedStmt("SELECT 🚀.city AS 🎸 FROM customers AS 🚀");
    }

    [Fact]
    public void parse_vacuum()
    {
        var stmt = Redshift().VerifiedStmt("VACUUM FULL");
        switch (stmt)
        {
            case Statement.Vacuum { Value: var v }:
                Assert.True(v.Full);
                Assert.Null(v.TableName);
                break;
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }
        stmt = Redshift().VerifiedStmt("VACUUM tbl");
        switch (stmt)
        {
            case Statement.Vacuum { Value: var v }:
                Assert.Equal(
                    v.TableName,
                    ObjectName.From(Sequence.Of(new Ident("tbl"))));
                break;
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }
        stmt = Redshift().VerifiedStmt(
            "VACUUM FULL SORT ONLY DELETE ONLY REINDEX RECLUSTER db1.sc1.tbl1 TO 20 PERCENT BOOST");
        switch (stmt)
        {
            case Statement.Vacuum { Value: var v }:
                Assert.True(v.Full);
                Assert.True(v.SortOnly);
                Assert.True(v.DeleteOnly);
                Assert.True(v.Reindex);
                Assert.True(v.Recluster);
                Assert.Equal(
                    v.TableName,
                    ObjectName.From(Sequence.Of(
                        new Ident("db1"),
                        new Ident("sc1"),
                        new Ident("tbl1"))));
                Assert.Equal(v.Threshold, TestUtils.Number("20").WithEmptySpan());
                Assert.True(v.Boost);
                break;
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }
    }

    [Fact]
    public void test_create_table_diststyle_distkey()
    {
        Redshift().VerifiedStmt(
            "CREATE TEMPORARY TABLE tmp_sbk_summary_pp DISTSTYLE KEY DISTKEY(bet_id) AS SELECT 1 AS bet_id");
    }

    [Fact]
    public void test_create_table_diststyle()
    {
        Redshift().VerifiedStmt("CREATE TABLE t1 (c1 INT) DISTSTYLE AUTO");
        Redshift().VerifiedStmt("CREATE TABLE t1 (c1 INT) DISTSTYLE EVEN");
        Redshift().VerifiedStmt("CREATE TABLE t1 (c1 INT) DISTSTYLE KEY DISTKEY(c1)");
        Redshift().VerifiedStmt("CREATE TABLE t1 (c1 INT) DISTSTYLE ALL");
    }

    [Fact]
    public void test_copy_credentials()
    {
        Redshift().VerifiedStmt(
            "COPY t1 FROM 's3://bucket/file.csv' CREDENTIALS 'aws_access_key_id=AK;aws_secret_access_key=SK' CSV");
    }

    [Fact]
    public void test_create_table_sortkey()
    {
        Redshift().VerifiedStmt("CREATE TABLE t1 (c1 INT, c2 INT, c3 TIMESTAMP) SORTKEY(c3)");
        Redshift().VerifiedStmt("CREATE TABLE t1 (c1 INT, c2 INT) SORTKEY(c1, c2)");
    }

    [Fact]
    public void test_create_table_distkey_sortkey_with_ctas()
    {
        Redshift().VerifiedStmt(
            "CREATE TABLE t1 DISTKEY(1) SORTKEY(1, 3) AS SELECT eventid, venueid, dateid, eventname FROM event");
    }

    [Fact]
    public void test_create_table_diststyle_distkey_sortkey()
    {
        Redshift().VerifiedStmt(
            "CREATE TABLE t1 (c1 INT, c2 INT) DISTSTYLE KEY DISTKEY(c1) SORTKEY(c1, c2)");
    }

    [Fact]
    public void test_alter_table_alter_sortkey()
    {
        Redshift().VerifiedStmt("ALTER TABLE users ALTER SORTKEY(created_at)");
        Redshift().VerifiedStmt("ALTER TABLE users ALTER SORTKEY(c1, c2)");
    }

    [Fact]
    public void test_create_table_backup()
    {
        Redshift().VerifiedStmt("CREATE TABLE public.users (id INT, name VARCHAR(255)) BACKUP YES");

        Redshift().VerifiedStmt("CREATE TABLE staging.events (event_id INT) BACKUP NO");

        Redshift().VerifiedStmt(
            "CREATE TABLE public.users_backup_test BACKUP YES DISTSTYLE AUTO AS SELECT id, name, email FROM public.users");
    }

    [Fact]
    public void test_null_treatment_inside_and_outside_window_function()
    {
        Redshift().VerifiedStmt("SELECT FIRST_VALUE(1 IGNORE NULLS) OVER () FROM (SELECT 1) t");
        Redshift().VerifiedStmt("SELECT FIRST_VALUE(1) IGNORE NULLS OVER () FROM (SELECT 1) t");
    }

    [Fact]
    public void test_partiql_from_alias_with_at_index()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsPartiql);
        dialects.VerifiedStmt("SELECT * FROM lineitem AS l (a, b, c) AT idx");

        var sql =
            "SELECT index, val FROM (SELECT array('AAA', 'BBB') AS val) AS b, b.val AS val AT index";
        var select = dialects.VerifiedOnlySelect(sql);

        switch (select.From[1].Relation)
        {
            case TableFactor.Table { Name: var name, Alias: var alias }:
                Assert.Equal(
                    name,
                    ObjectName.From(Sequence.Of(new Ident("b"), new Ident("val"))));
                Assert.Equal(alias?.Name, new Ident("val"));
                Assert.Equal(
                    alias?.At,
                    new Ident("index"));
                break;
            default:
                throw new InvalidOperationException("expected table factor");
        }
    }

    [Fact]
    public void parse_unpivot_expression()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsUnpivotExpr);

        dialects.VerifiedStmt(
            "SELECT t.id, k, v FROM test_colors AS t, UNPIVOT t.count_by_color AS v AT k");
        dialects.VerifiedStmt("SELECT t.id, k, v FROM test_colors AS t, UNPIVOT t AS v AT k");
    }

    [Fact]
    public void test_interval_as_column_name()
    {
        Redshift().VerifiedStmt("SELECT * FROM table_name WHERE interval = 78");
    }
}
