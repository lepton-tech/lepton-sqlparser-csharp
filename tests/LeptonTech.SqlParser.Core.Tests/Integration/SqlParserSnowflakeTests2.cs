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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_snowflake.rs (linhas 2576–4914).
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C12.md):
// - Test SQL syntax specific to Snowflake. The parser based on the
//   generic dialect is also tested (on the inputs it can handle).
// - Segunda fatia do arquivo Rust; as funções-atalho do arquivo (`snowflake()`, `snowflake_and_generic()`...)
//   foram copiadas como métodos `private static` (nomes mecânicos).
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests/SpansTests).
// - `assert!(v.contains(&x))` → `Assert.Contains(x, v)` (xUnit2017); `assert!(s.contains("x"))` →
//   `Assert.Contains("x", s)` (xUnit2009); `assert_eq!(x, true)` → `Assert.True(x)` (xUnit2004).
// - `assert_eq!(1, v.len())` usa `Assert.Equal(1, v.Count)`, como no Rust; o xUnit2013 (que sugere
//   Assert.Single) fica suprimido neste arquivo, como em AstModTests/SpansTests.
// - `res.unwrap_err().to_string()` → `Assert.Throws<ParserException>(..).Error.ToString()`.
// - `unreachable!()` → `throw new InvalidOperationException("unreachable")`; `panic!(msg)` → `Assert.Fail(msg)`.
#pragma warning disable xUnit2013
public class SqlParserSnowflakeTests2
{
    // fn snowflake() -> TestedDialects
    private static TestedDialects Snowflake() =>
        new TestedDialects(new List<Dialect> { new SnowflakeDialect() });

    // fn snowflake_with_recursion_limit(recursion_limit: usize) -> TestedDialects
    private static TestedDialects SnowflakeWithRecursionLimit(int recursionLimit) =>
        new TestedDialects(new List<Dialect> { new SnowflakeDialect() }).WithRecursionLimit(recursionLimit);

    // fn snowflake_without_unescape() -> TestedDialects
    private static TestedDialects SnowflakeWithoutUnescape() =>
        TestedDialects.NewWithOptions(
            new List<Dialect> { new SnowflakeDialect() },
            new ParserOptions().WithUnescape(false));

    // fn snowflake_and_generic() -> TestedDialects
    private static TestedDialects SnowflakeAndGeneric() =>
        new TestedDialects(new List<Dialect>
        {
            new SnowflakeDialect(),
            new GenericDialect(),
        });

    [Fact]
    public void test_copy_into_with_cast_transformation()
    {
        var variants = new[]
        {
            "COPY INTO my_company.emp_basic (a) FROM " +
            "(SELECT $1:\"A\"::NUMBER(38, 0) FROM @stg)",
            "COPY INTO my_company.emp_basic (a) FROM " +
            "(SELECT $1::NUMBER(38, 0) FROM @stg)",
            "COPY INTO my_company.emp_basic (a) FROM " +
            "(SELECT $1:SEQUENCE::NUMBER(38, 0) FROM @stg)",
            "COPY INTO my_company.emp_basic (a, b) FROM " +
            "(SELECT $1:\"A\"::VARIANT, $1:\"B\"::TEXT FROM @stg)",
            "COPY INTO my_company.emp_basic (a, b) FROM " +
            "(SELECT t.$1:plain AS plain, $1:\"B\"::TEXT FROM @stg AS t)",
            // https://docs.snowflake.com/en/user-guide/tutorials/script-data-load-transform-parquet
            "COPY INTO my_company.emp_basic (a, b) FROM " +
            "(SELECT $1:continent::VARCHAR, $1:country:name::VARCHAR FROM @stg)",
            "COPY INTO my_company.emp_basic (a) FROM " +
            "(SELECT $1:country.name::VARCHAR FROM @stg)",
            "COPY INTO my_company.emp_basic (a) FROM " +
            "(SELECT $1['country']['name']::VARCHAR FROM @stg)",
            "COPY INTO my_company.emp_basic (a) FROM " +
            "(SELECT t.$1:country.name::VARCHAR AS country FROM @stg AS t)",
        };
        foreach (var sql in variants)
        {
            Snowflake().VerifiedStmt(sql);
        }
    }

    [Fact]
    public void test_copy_into_file_format()
    {
        var sql =
            "COPY INTO my_company.emp_basic " +
            "FROM 'gcs://mybucket/./../a.csv' " +
            "FILES = ('file1.json', 'file2.json') " +
            "PATTERN = '.*employees0[1-5].csv.gz' " +
            @"FILE_FORMAT=(COMPRESSION=AUTO BINARY_FORMAT=HEX ESCAPE='\\')";

        switch (SnowflakeWithoutUnescape().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake { FileFormat: var fileFormat }:
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "COMPRESSION",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Placeholder("AUTO").WithEmptySpan())),
                    fileFormat.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "BINARY_FORMAT",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Placeholder("HEX").WithEmptySpan())),
                    fileFormat.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "ESCAPE",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString(@"\\").WithEmptySpan())),
                    fileFormat.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(
            SnowflakeWithoutUnescape().VerifiedStmt(sql).ToString(),
            sql);

        // Test commas in file format
        sql =
            "COPY INTO my_company.emp_basic " +
            "FROM 'gcs://mybucket/./../a.csv' " +
            "FILES = ('file1.json', 'file2.json') " +
            "PATTERN = '.*employees0[1-5].csv.gz' " +
            @"FILE_FORMAT=(COMPRESSION=AUTO, BINARY_FORMAT=HEX, ESCAPE='\\')";

        var first = SnowflakeWithoutUnescape()
            .ParseSqlStatements(sql)
            .First
            ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");
        switch (first)
        {
            case Statement.CopyIntoSnowflake { FileFormat: var fileFormat }:
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "COMPRESSION",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Placeholder("AUTO").WithEmptySpan())),
                    fileFormat.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "BINARY_FORMAT",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Placeholder("HEX").WithEmptySpan())),
                    fileFormat.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "ESCAPE",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString(@"\\").WithEmptySpan())),
                    fileFormat.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_copy_into_copy_options()
    {
        var sql =
            "COPY INTO my_company.emp_basic " +
            "FROM 'gcs://mybucket/./../a.csv' " +
            "FILES = ('file1.json', 'file2.json') " +
            "PATTERN = '.*employees0[1-5].csv.gz' " +
            "COPY_OPTIONS=(ON_ERROR=CONTINUE FORCE=true)";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake { CopyOptions: var copyOptions }:
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "ON_ERROR",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Placeholder("CONTINUE").WithEmptySpan())),
                    copyOptions.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "FORCE",
                        OptionValue: new KeyValueOptionKind.Single(new Value.Boolean(true).WithEmptySpan())),
                    copyOptions.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_snowflake_stage_object_names_into_location()
    {
        var allowedObjectNames = new[]
        {
            ObjectName.From(Sequence.Of(new Ident("@namespace"), new Ident("%table_name"))),
            ObjectName.From(Sequence.Of(
                new Ident("@namespace"),
                new Ident("%table_name/path"))),
            ObjectName.From(Sequence.Of(
                new Ident("@namespace"),
                new Ident("stage_name/path"))),
            ObjectName.From(Sequence.Of(new Ident("@~/path"))),
        };

        var allowedNamesIntoLocation = new[]
        {
            "@namespace.%table_name",
            "@namespace.%table_name/path",
            "@namespace.stage_name/path",
            "@~/path",
        };
        foreach (var (formattedName, objectName) in allowedNamesIntoLocation.Zip(allowedObjectNames, (a, b) => (a, b)))
        {
            var sql = $"COPY INTO {formattedName} FROM 'gcs://mybucket/./../a.csv'";
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.CopyIntoSnowflake { Into: var into }:
                    Assert.Equal(into.Value, objectName.Value);
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
    }

    [Fact]
    public void test_snowflake_stage_object_names_into_table()
    {
        var allowedObjectNames = new[]
        {
            ObjectName.From(Sequence.Of(new Ident("my_company"), new Ident("emp_basic"))),
            ObjectName.From(Sequence.Of(new Ident("emp_basic"))),
        };

        var allowedNamesIntoTable = new[] { "my_company.emp_basic", "emp_basic" };
        foreach (var (formattedName, objectName) in allowedNamesIntoTable.Zip(allowedObjectNames, (a, b) => (a, b)))
        {
            var sql = $"COPY INTO {formattedName} FROM 'gcs://mybucket/./../a.csv'";
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.CopyIntoSnowflake { Into: var into }:
                    Assert.Equal(into.Value, objectName.Value);
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
    }

    [Fact]
    public void test_snowflake_copy_into()
    {
        var sql = "COPY INTO a.b FROM @namespace.stage_name";
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake { Into: var into, FromObj: var fromObj }:
                Assert.Equal(
                    into,
                    ObjectName.From(Sequence.Of(new Ident("a"), new Ident("b"))));
                Assert.Equal(
                    fromObj,
                    ObjectName.From(Sequence.Of(
                        new Ident("@namespace"),
                        new Ident("stage_name"))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Test for non-ident characters in stage names
        sql = "COPY INTO a.b FROM @namespace.stage_name/x@x~x%x+/20250723_data-x";
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake { Into: var into, FromObj: var fromObj }:
                Assert.Equal(
                    into,
                    ObjectName.From(Sequence.Of(new Ident("a"), new Ident("b"))));
                Assert.Equal(
                    fromObj,
                    ObjectName.From(Sequence.Of(
                        new Ident("@namespace"),
                        new Ident("stage_name/x@x~x%x+/20250723_data-x"))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_copy_into_stage_name_ends_with_parens()
    {
        var sql = "COPY INTO SCHEMA.SOME_MONITORING_SYSTEM FROM (SELECT t.$1:st AS st FROM @schema.general_finished)";
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake { Into: var into, FromObj: var fromObj }:
                Assert.Equal(
                    into,
                    ObjectName.From(Sequence.Of(
                        new Ident("SCHEMA"),
                        new Ident("SOME_MONITORING_SYSTEM"))));
                Assert.Equal(
                    fromObj,
                    ObjectName.From(Sequence.Of(
                        new Ident("@schema"),
                        new Ident("general_finished"))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_stage_name_with_special_chars()
    {
        // Stage path with '=' (Hive-style partitioning)
        Snowflake().VerifiedStmt("SELECT * FROM @stage/day=18/23.parquet");

        // Stage path with ':' (time-based partitioning)
        Snowflake().VerifiedStmt("SELECT * FROM @stage/0:18:23/23.parquet");

        // COPY INTO with '=' in stage path
        Snowflake().VerifiedStmt("COPY INTO my_table FROM @stage/day=18/file.parquet");

        // COPY INTO with ':' in stage path
        Snowflake().VerifiedStmt("COPY INTO my_table FROM @stage/0:18:23/file.parquet");
    }

    [Fact]
    public void test_snowflake_trim()
    {
        var realSql = "SELECT customer_id, TRIM(sub_items.value:item_price_id, '\"', \"a\") AS item_price_id FROM models_staging.subscriptions";
        Assert.Equal(Snowflake().VerifiedStmt(realSql).ToString(), realSql);

        var sqlOnlySelect = "SELECT TRIM('xyz', 'a')";
        var select = Snowflake().VerifiedOnlySelect(sqlOnlySelect);
        Assert.Equal(
            new Expr.Trim(
                Expr: new Expr.Value(
                    new Value.SingleQuotedString("xyz").WithEmptySpan()),
                TrimWhere: null,
                TrimWhat: null,
                TrimCharacters: Sequence.Of<Expr>(new Expr.Value(
                    new Value.SingleQuotedString("a").WithEmptySpan()))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        // missing comma separation
        var errorSql = "SELECT TRIM('xyz' 'a')";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: ), found: 'a'"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);
    }

    [Fact]
    public void test_number_placeholder()
    {
        var sqlOnlySelect = "SELECT :1";
        var select = Snowflake().VerifiedOnlySelect(sqlOnlySelect);
        Assert.Equal(
            new Expr.Value(new Value.Placeholder(":1").WithEmptySpan()),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        // .expect_err("should have failed")
        Assert.Throws<ParserException>(() =>
            Snowflake().ParseSqlStatements("alter role 1 with name = 'foo'"));
    }

    [Fact]
    public void parse_position_not_function_columns()
    {
        SnowflakeAndGeneric()
            .VerifiedStmt("SELECT position FROM tbl1 WHERE position NOT IN ('first', 'last')");
    }

    [Fact]
    public void parse_subquery_function_argument()
    {
        // Snowflake allows passing an unparenthesized subquery as the single
        // argument to a function.
        Snowflake().VerifiedStmt("SELECT parse_json(SELECT '{}')");

        // Subqueries that begin with WITH work too.
        Snowflake()
            .VerifiedStmt("SELECT parse_json(WITH q AS (SELECT '{}' AS foo) SELECT foo FROM q)");

        // Commas are parsed as part of the subquery, not additional arguments to
        // the function.
        Snowflake().VerifiedStmt("SELECT func(SELECT 1, 2)");
    }

    [Fact]
    public void parse_division_correctly()
    {
        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT field/1000 FROM tbl1",
            "SELECT field / 1000 FROM tbl1");

        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT tbl1.field/tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id",
            "SELECT tbl1.field / tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id");
    }

    [Fact]
    public void parse_pivot_of_table_factor_derived()
    {
        Snowflake().VerifiedStmt(
            "SELECT * FROM (SELECT place_id, weekday, open FROM times AS p) PIVOT(max(open) FOR weekday IN (0, 1, 2, 3, 4, 5, 6)) AS p (place_id, open_sun, open_mon, open_tue, open_wed, open_thu, open_fri, open_sat)");
    }

    [Fact]
    public void parse_top()
    {
        Snowflake().OneStatementParsesTo(
            "SELECT TOP 4 c1 FROM testtable",
            "SELECT TOP 4 c1 FROM testtable");
    }

    [Fact]
    public void parse_extract_custom_part()
    {
        var sql = "SELECT EXTRACT(eod FROM d)";
        var select = SnowflakeAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Extract(
                Field: new DateTimeField.Custom(new Ident("eod")),
                Syntax: ExtractSyntax.From,
                Expr: new Expr.Identifier(new Ident("d"))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_extract_comma()
    {
        var sql = "SELECT EXTRACT(HOUR, d)";
        var select = SnowflakeAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Extract(
                Field: new DateTimeField.Hour(),
                Syntax: ExtractSyntax.Comma,
                Expr: new Expr.Identifier(new Ident("d"))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_extract_comma_quoted()
    {
        var sql = "SELECT EXTRACT('hour', d)";
        var select = SnowflakeAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Extract(
                Field: new DateTimeField.Custom(Ident.WithQuote('\'', "hour")),
                Syntax: ExtractSyntax.Comma,
                Expr: new Expr.Identifier(new Ident("d"))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_comma_outer_join()
    {
        // compound identifiers
        var case1 =
            Snowflake().VerifiedOnlySelect("SELECT t1.c1, t2.c2 FROM t1, t2 WHERE t1.c1 = t2.c2 (+)");
        Assert.Equal(
            case1.Selection,
            new Expr.BinaryOp(
                Left: new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("t1"),
                    new Ident("c1"))),
                Op: new BinaryOperator.Eq(),
                Right: new Expr.OuterJoin(new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("t2"),
                    new Ident("c2"))))));

        // regular identifiers
        var case2 =
            Snowflake().VerifiedOnlySelect("SELECT t1.c1, t2.c2 FROM t1, t2 WHERE c1 = c2 (+)");
        Assert.Equal(
            case2.Selection,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("c1")),
                Op: new BinaryOperator.Eq(),
                Right: new Expr.OuterJoin(new Expr.Identifier(new Ident(
                    "c2")))));

        // ensure we can still parse function calls with a unary plus arg
        var case3 =
            Snowflake().VerifiedOnlySelect("SELECT t1.c1, t2.c2 FROM t1, t2 WHERE c1 = myudf(+42)");
        Assert.Equal(
            case3.Selection,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("c1")),
                Op: new BinaryOperator.Eq(),
                Right: TestUtils.Call(
                    "myudf",
                    new Expr[]
                    {
                        new Expr.UnaryOp(
                            Op: new UnaryOperator.Plus(),
                            Expr: Expr.MakeValue(TestUtils.Number("42"))),
                    })));

        // permissive with whitespace
        Snowflake().VerifiedOnlySelectWithCanonical(
            "SELECT t1.c1, t2.c2 FROM t1, t2 WHERE t1.c1 = t2.c2(   +     )",
            "SELECT t1.c1, t2.c2 FROM t1, t2 WHERE t1.c1 = t2.c2 (+)");
    }

    [Fact]
    public void test_sf_trailing_commas()
    {
        Snowflake().VerifiedOnlySelectWithCanonical("SELECT 1, 2, FROM t", "SELECT 1, 2 FROM t");
    }

    [Fact]
    public void test_select_wildcard_with_ilike()
    {
        var select = SnowflakeAndGeneric().VerifiedOnlySelect("SELECT * ILIKE '%id%' FROM tbl");
        var expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptIlike = new IlikeSelectItem(Pattern: "%id%"),
        });
        Assert.Equal<SelectItem>(expected, select.Projection[0]);
    }

    [Fact]
    public void test_select_wildcard_with_ilike_double_quote()
    {
        var res = Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("SELECT * ILIKE \"%id\" FROM tbl"));
        Assert.Equal(
            "sql parser error: Expected: ilike pattern, found: \"%id\"",
            res.Error.ToString());
    }

    [Fact]
    public void test_select_wildcard_with_ilike_number()
    {
        var res = Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("SELECT * ILIKE 42 FROM tbl"));
        Assert.Equal(
            "sql parser error: Expected: ilike pattern, found: 42",
            res.Error.ToString());
    }

    [Fact]
    public void test_select_wildcard_with_ilike_replace()
    {
        var res = Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("SELECT * ILIKE '%id%' EXCLUDE col FROM tbl"));
        Assert.Equal(
            "sql parser error: Expected: end of statement, found: EXCLUDE",
            res.Error.ToString());
    }

    [Fact]
    public void first_value_ignore_nulls()
    {
        Snowflake().VerifiedOnlySelect(
            "SELECT FIRST_VALUE(column2 IGNORE NULLS) " +
            "OVER (PARTITION BY column1 ORDER BY column2) " +
            "FROM some_table");
    }

    [Fact]
    public void test_pivot()
    {
        // pivot on static list of values with default
        Snowflake().VerifiedOnlySelect(
            "SELECT * " +
            "FROM quarterly_sales " +
              "PIVOT(SUM(amount) " +
                "FOR quarter IN (" +
                  "'2023_Q1', " +
                  "'2023_Q2', " +
                  "'2023_Q3', " +
                  "'2023_Q4', " +
                  "'2024_Q1') " +
                "DEFAULT ON NULL (0)" +
              ") " +
            "ORDER BY empid");

        // dynamic pivot from subquery
        Snowflake().VerifiedOnlySelect(
            "SELECT * " +
            "FROM quarterly_sales " +
              "PIVOT(SUM(amount) FOR quarter IN (" +
                "SELECT DISTINCT quarter " +
                  "FROM ad_campaign_types_by_quarter " +
                  "WHERE television = true " +
                  "ORDER BY quarter)" +
              ") " +
            "ORDER BY empid");

        // dynamic pivot on any value (with order by)
        Snowflake().VerifiedOnlySelect(
            "SELECT * " +
            "FROM quarterly_sales " +
              "PIVOT(SUM(amount) FOR quarter IN (ANY ORDER BY quarter)) " +
            "ORDER BY empid");

        // dynamic pivot on any value (without order by)
        Snowflake().VerifiedOnlySelect(
            "SELECT * " +
            "FROM sales_data " +
              "PIVOT(SUM(total_sales) FOR fis_quarter IN (ANY)) " +
            "WHERE fis_year IN (2023) " +
            "ORDER BY region");
    }

    [Fact]
    public void asof_joins()
    {
        var query = SnowflakeAndGeneric().VerifiedOnlySelect(
            "SELECT * " +
              "FROM trades_unixtime AS tu " +
                "ASOF JOIN quotes_unixtime AS qu " +
                "MATCH_CONDITION (tu.trade_time >= qu.quote_time)");

        Assert.Equal(
            query.From[0],
            new TableWithJoins(
                Relation: TestUtils.TableWithAlias("trades_unixtime", true, "tu"),
                Joins: Sequence.Of(new Join(
                    Relation: TestUtils.TableWithAlias("quotes_unixtime", true, "qu"),
                    Global: false,
                    JoinOperator: new JoinOperator.AsOf(
                        MatchCondition: new Expr.BinaryOp(
                            Left: new Expr.CompoundIdentifier(Sequence.Of(
                                new Ident("tu"),
                                new Ident("trade_time"))),
                            Op: new BinaryOperator.GtEq(),
                            Right: new Expr.CompoundIdentifier(Sequence.Of(
                                new Ident("qu"),
                                new Ident("quote_time")))),
                        Constraint: new JoinConstraint.None())))));

        SnowflakeAndGeneric().VerifiedQuery(
            "SELECT t.stock_symbol, t.trade_time, t.quantity, q.quote_time, q.price " +
            "FROM trades AS t ASOF JOIN quotes AS q " +
              "MATCH_CONDITION (t.trade_time >= quote_time) " +
              "ON t.stock_symbol = q.stock_symbol " +
            "ORDER BY t.stock_symbol");

        SnowflakeAndGeneric().VerifiedQuery(
            "SELECT t.stock_symbol, c.company_name, t.trade_time, t.quantity, q.quote_time, q.price " +
              "FROM trades AS t ASOF JOIN quotes AS q " +
                "MATCH_CONDITION (t.trade_time <= quote_time) " +
                "USING(stock_symbol) " +
                "JOIN companies AS c ON c.stock_symbol = t.stock_symbol " +
              "ORDER BY t.stock_symbol");

        SnowflakeAndGeneric().VerifiedQuery(
            "SELECT * " +
              "FROM snowtime AS s " +
                "ASOF JOIN raintime AS r " +
                  "MATCH_CONDITION (s.observed >= r.observed) " +
                  "ON s.state = r.state " +
                "ASOF JOIN preciptime AS p " +
                  "MATCH_CONDITION (s.observed >= p.observed) " +
                  "ON s.state = p.state " +
              "ORDER BY s.observed");

        // Test without explicit aliases
        SnowflakeAndGeneric().VerifiedQuery(
            "SELECT * " +
              "FROM snowtime " +
                "ASOF JOIN raintime " +
                  "MATCH_CONDITION (snowtime.observed >= raintime.observed) " +
                  "ON snowtime.state = raintime.state " +
                "ASOF JOIN preciptime " +
                  "MATCH_CONDITION (showtime.observed >= preciptime.observed) " +
                  "ON showtime.state = preciptime.state " +
              "ORDER BY showtime.observed");
    }

    [Fact]
    public void test_parse_position()
    {
        Snowflake().VerifiedQuery("SELECT position('an', 'banana', 1)");
        Snowflake().VerifiedQuery("SELECT n, h, POSITION(n IN h) FROM pos");
    }

    [Fact]
    public void explain_describe()
    {
        Snowflake().VerifiedStmt("DESCRIBE test.table");
        Snowflake().VerifiedStmt("DESCRIBE TABLE test.table");
    }

    [Fact]
    public void explain_desc()
    {
        Snowflake().VerifiedStmt("DESC test.table");
        Snowflake().VerifiedStmt("DESC TABLE test.table");
    }

    [Fact]
    public void parse_explain_table()
    {
        switch (Snowflake().VerifiedStmt("EXPLAIN TABLE test_identifier"))
        {
            case Statement.ExplainTable
            {
                DescribeAlias: var describeAlias,
                HiveFormat: var hiveFormat,
                HasTableKeyword: var hasTableKeyword,
                TableName: var tableName,
            }:
                Assert.Equal(describeAlias, new DescribeAlias.Explain());
                Assert.Null(hiveFormat);
                Assert.True(hasTableKeyword);
                Assert.Equal("test_identifier", tableName.ToString());
                break;
            default:
                Assert.Fail("Unexpected Statement, must be ExplainTable");
                break;
        }
    }

    [Fact]
    public void parse_use()
    {
        var validObjectNames = new[] { "mydb", "CATALOG", "DEFAULT" };
        var quoteStyles = new[] { '\'', '"', '`' };
        foreach (var objectName in validObjectNames)
        {
            // Test single identifier without quotes
            Assert.Equal(
                Snowflake().VerifiedStmt($"USE {objectName}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(new Ident(
                    objectName))))));
            foreach (var quote in quoteStyles)
            {
                // Test single identifier with different type of quotes
                Assert.Equal(
                    Snowflake().VerifiedStmt($"USE {quote}{objectName}{quote}"),
                    new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(Ident.WithQuote(
                        quote,
                        objectName))))));
            }
        }

        foreach (var quote in quoteStyles)
        {
            // Test double identifier with different type of quotes
            Assert.Equal(
                Snowflake().VerifiedStmt(
                    $"USE {quote}CATALOG{quote}.{quote}my_schema{quote}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(
                    Ident.WithQuote(quote, "CATALOG"),
                    Ident.WithQuote(quote, "my_schema"))))));
        }
        // Test double identifier without quotes
        Assert.Equal(
            Snowflake().VerifiedStmt("USE mydb.my_schema"),
            new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(
                new Ident("mydb"),
                new Ident("my_schema"))))));

        foreach (var quote in quoteStyles)
        {
            // Test single and double identifier with keyword and different type of quotes
            Assert.Equal(
                Snowflake().VerifiedStmt($"USE DATABASE {quote}my_database{quote}"),
                new Statement.Use(new Use.Database(ObjectName.From(Sequence.Of(Ident.WithQuote(
                    quote,
                    "my_database"))))));
            Assert.Equal(
                Snowflake().VerifiedStmt($"USE SCHEMA {quote}my_schema{quote}"),
                new Statement.Use(new Use.Schema(ObjectName.From(Sequence.Of(Ident.WithQuote(
                    quote,
                    "my_schema"))))));
            Assert.Equal(
                Snowflake().VerifiedStmt(
                    $"USE SCHEMA {quote}CATALOG{quote}.{quote}my_schema{quote}"),
                new Statement.Use(new Use.Schema(ObjectName.From(Sequence.Of(
                    Ident.WithQuote(quote, "CATALOG"),
                    Ident.WithQuote(quote, "my_schema"))))));
            Assert.Equal(
                Snowflake().VerifiedStmt($"USE ROLE {quote}my_role{quote}"),
                new Statement.Use(new Use.Role(ObjectName.From(Sequence.Of(Ident.WithQuote(
                    quote,
                    "my_role"))))));
            Assert.Equal(
                Snowflake().VerifiedStmt($"USE WAREHOUSE {quote}my_wh{quote}"),
                new Statement.Use(new Use.Warehouse(ObjectName.From(Sequence.Of(Ident.WithQuote(
                    quote,
                    "my_wh"))))));
        }

        // Test invalid syntax - missing identifier
        var invalidCases = new[] { "USE SCHEMA", "USE DATABASE", "USE WAREHOUSE" };
        foreach (var sql in invalidCases)
        {
            Assert.Equal<ParserError>(
                Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(sql)).Error,
                new ParserError.ParserErrorValue("Expected: identifier, found: EOF"));
        }

        Snowflake().VerifiedStmt("USE SECONDARY ROLES ALL");
        Snowflake().VerifiedStmt("USE SECONDARY ROLES NONE");
        Snowflake().VerifiedStmt("USE SECONDARY ROLES r1, r2, r3");

        // The following is not documented by Snowflake but still works:
        Snowflake().OneStatementParsesTo("USE SECONDARY ROLE ALL", "USE SECONDARY ROLES ALL");
        Snowflake().OneStatementParsesTo("USE SECONDARY ROLE NONE", "USE SECONDARY ROLES NONE");
        Snowflake().OneStatementParsesTo(
            "USE SECONDARY ROLE r1, r2, r3",
            "USE SECONDARY ROLES r1, r2, r3");
    }

    [Fact]
    public void view_comment_option_should_be_after_column_list()
    {
        foreach (var sql in new[]
        {
            "CREATE OR REPLACE VIEW v (a) COMMENT = 'Comment' AS SELECT a FROM t",
            "CREATE OR REPLACE VIEW v (a COMMENT 'a comment', b, c COMMENT 'c comment') COMMENT = 'Comment' AS SELECT a FROM t",
            "CREATE OR REPLACE VIEW v (a COMMENT 'a comment', b, c COMMENT 'c comment') WITH (foo = bar) COMMENT = 'Comment' AS SELECT a FROM t",
        })
        {
            Snowflake()
                .VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_view_column_descriptions()
    {
        var sql = "CREATE OR REPLACE VIEW v (a COMMENT 'Comment', b) AS SELECT a, b FROM table1";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateView { Value: { Name: var name, Columns: var columns } }:
                Assert.Equal("v", name.ToString());
                Assert.Equal(
                    columns,
                    Sequence.Of(
                        new ViewColumnDef(
                            Name: new Ident("a"),
                            DataType: null,
                            Options: new ColumnOptions.SpaceSeparated(Sequence.Of<ColumnOption>(new ColumnOption.Comment(
                                "Comment")))),
                        new ViewColumnDef(
                            Name: new Ident("b"),
                            DataType: null,
                            Options: null)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_parentheses_overflow()
    {
        // Use a modest nesting level to avoid actual stack overflow on
        // CI runners with small thread stacks (debug builds use large frames
        // and each nesting level adds extra depth via maybe_parse).
        var maxNestingLevel = 20;

        // Verify the recursion check is not too wasteful (num of parentheses within budget)
        var slack = 3;
        var lParens = new string('(', maxNestingLevel - slack);
        var rParens = new string(')', maxNestingLevel - slack);
        var sql = $"SELECT * FROM {lParens}a.b.c{rParens}";
        var parsedErr = ParseErr(SnowflakeWithRecursionLimit(maxNestingLevel), sql);
        Assert.Null(parsedErr);

        // Verify the recursion check triggers (one more paren exceeds the budget)
        slack = 2;
        lParens = new string('(', maxNestingLevel - slack);
        rParens = new string(')', maxNestingLevel - slack);
        sql = $"SELECT * FROM {lParens}a.b.c{rParens}";
        parsedErr = ParseErr(SnowflakeWithRecursionLimit(maxNestingLevel), sql);
        Assert.Equal<ParserError?>(parsedErr, new ParserError.RecursionLimitExceeded());

        // `parse_sql_statements(..).err()`: o erro, se houver; null se o parse der certo.
        static ParserError? ParseErr(TestedDialects dialects, string s)
        {
            try
            {
                dialects.ParseSqlStatements(s);
                return null;
            }
            catch (ParserException e)
            {
                return e.Error;
            }
        }
    }

    [Fact]
    public void test_show_databases()
    {
        Snowflake().VerifiedStmt("SHOW DATABASES");
        Snowflake().VerifiedStmt("SHOW TERSE DATABASES");
        Snowflake().VerifiedStmt("SHOW DATABASES HISTORY");
        Snowflake().VerifiedStmt("SHOW DATABASES LIKE '%abc%'");
        Snowflake().VerifiedStmt("SHOW DATABASES STARTS WITH 'demo_db'");
        Snowflake().VerifiedStmt("SHOW DATABASES LIMIT 12");
        Snowflake()
            .VerifiedStmt("SHOW DATABASES HISTORY LIKE '%aa' STARTS WITH 'demo' LIMIT 20 FROM 'abc'");
        Snowflake().VerifiedStmt("SHOW DATABASES IN ACCOUNT abc");
    }

    [Fact]
    public void test_parse_show_schemas()
    {
        Snowflake().VerifiedStmt("SHOW SCHEMAS");
        Snowflake().VerifiedStmt("SHOW TERSE SCHEMAS");
        Snowflake().VerifiedStmt("SHOW SCHEMAS IN ACCOUNT");
        Snowflake().VerifiedStmt("SHOW SCHEMAS IN ACCOUNT abc");
        Snowflake().VerifiedStmt("SHOW SCHEMAS IN DATABASE");
        Snowflake().VerifiedStmt("SHOW SCHEMAS IN DATABASE xyz");
        Snowflake().VerifiedStmt("SHOW SCHEMAS HISTORY LIKE '%xa%'");
        Snowflake().VerifiedStmt("SHOW SCHEMAS STARTS WITH 'abc' LIMIT 20");
        Snowflake().VerifiedStmt("SHOW SCHEMAS IN DATABASE STARTS WITH 'abc' LIMIT 20 FROM 'xyz'");
    }

    [Fact]
    public void test_parse_show_objects()
    {
        Snowflake().VerifiedStmt("SHOW OBJECTS");
        Snowflake().VerifiedStmt("SHOW OBJECTS IN abc");
        Snowflake().VerifiedStmt("SHOW OBJECTS LIKE '%test%' IN abc");
        Snowflake().VerifiedStmt("SHOW OBJECTS IN ACCOUNT");
        Snowflake().VerifiedStmt("SHOW OBJECTS IN DATABASE");
        Snowflake().VerifiedStmt("SHOW OBJECTS IN DATABASE abc");
        Snowflake().VerifiedStmt("SHOW OBJECTS IN SCHEMA");
        Snowflake().VerifiedStmt("SHOW OBJECTS IN SCHEMA abc");
        Snowflake().VerifiedStmt("SHOW TERSE OBJECTS");
        Snowflake().VerifiedStmt("SHOW TERSE OBJECTS IN abc");
        Snowflake().VerifiedStmt("SHOW TERSE OBJECTS LIKE '%test%' IN abc");
        Snowflake().VerifiedStmt("SHOW TERSE OBJECTS LIKE '%test%' IN abc STARTS WITH 'b'");
        Snowflake().VerifiedStmt("SHOW TERSE OBJECTS LIKE '%test%' IN abc STARTS WITH 'b' LIMIT 10");
        Snowflake()
            .VerifiedStmt("SHOW TERSE OBJECTS LIKE '%test%' IN abc STARTS WITH 'b' LIMIT 10 FROM 'x'");
        switch (Snowflake().VerifiedStmt("SHOW TERSE OBJECTS LIKE '%test%' IN abc"))
        {
            case Statement.ShowObjects { Value: { Terse: var terse, ShowOptions: var showOptions } }:
                Assert.True(terse);
                var name = showOptions.ShowIn switch
                {
                    { ParentName: { } val } => val.ToString(),
                    _ => throw new InvalidOperationException("unreachable"),
                };
                Assert.Equal("abc", name);
                var like = showOptions.FilterPosition switch
                {
                    ShowStatementFilterPosition.Infix { Value: ShowStatementFilter.Like { Value: var val } } => val,
                    _ => throw new InvalidOperationException("unreachable"),
                };
                Assert.Equal("%test%", like);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_parse_show_tables()
    {
        Snowflake().VerifiedStmt("SHOW TABLES");
        Snowflake().VerifiedStmt("SHOW TERSE TABLES");
        Snowflake().VerifiedStmt("SHOW TABLES IN ACCOUNT");
        Snowflake().VerifiedStmt("SHOW TABLES IN DATABASE");
        Snowflake().VerifiedStmt("SHOW TABLES IN DATABASE xyz");
        Snowflake().VerifiedStmt("SHOW TABLES IN SCHEMA");
        Snowflake().VerifiedStmt("SHOW TABLES IN SCHEMA xyz");
        Snowflake().VerifiedStmt("SHOW TABLES HISTORY LIKE '%xa%'");
        Snowflake().VerifiedStmt("SHOW TABLES STARTS WITH 'abc' LIMIT 20");
        Snowflake().VerifiedStmt("SHOW TABLES IN SCHEMA STARTS WITH 'abc' LIMIT 20 FROM 'xyz'");
        Snowflake().VerifiedStmt("SHOW EXTERNAL TABLES");
        Snowflake().VerifiedStmt("SHOW EXTERNAL TABLES IN ACCOUNT");
        Snowflake().VerifiedStmt("SHOW EXTERNAL TABLES IN DATABASE");
        Snowflake().VerifiedStmt("SHOW EXTERNAL TABLES IN DATABASE xyz");
        Snowflake().VerifiedStmt("SHOW EXTERNAL TABLES IN SCHEMA");
        Snowflake().VerifiedStmt("SHOW EXTERNAL TABLES IN SCHEMA xyz");
        Snowflake().VerifiedStmt("SHOW EXTERNAL TABLES STARTS WITH 'abc' LIMIT 20");
        Snowflake()
            .VerifiedStmt("SHOW EXTERNAL TABLES IN SCHEMA STARTS WITH 'abc' LIMIT 20 FROM 'xyz'");
    }

    [Fact]
    public void test_show_views()
    {
        Snowflake().VerifiedStmt("SHOW VIEWS");
        Snowflake().VerifiedStmt("SHOW TERSE VIEWS");
        Snowflake().VerifiedStmt("SHOW VIEWS IN ACCOUNT");
        Snowflake().VerifiedStmt("SHOW VIEWS IN DATABASE");
        Snowflake().VerifiedStmt("SHOW VIEWS IN DATABASE xyz");
        Snowflake().VerifiedStmt("SHOW VIEWS IN SCHEMA");
        Snowflake().VerifiedStmt("SHOW VIEWS IN SCHEMA xyz");
        Snowflake().VerifiedStmt("SHOW VIEWS STARTS WITH 'abc' LIMIT 20");
        Snowflake().VerifiedStmt("SHOW VIEWS IN SCHEMA STARTS WITH 'abc' LIMIT 20 FROM 'xyz'");
    }

    [Fact]
    public void test_parse_show_columns_sql()
    {
        Snowflake().VerifiedStmt("SHOW COLUMNS IN TABLE");
        Snowflake().VerifiedStmt("SHOW COLUMNS IN TABLE abc");
        Snowflake().VerifiedStmt("SHOW COLUMNS LIKE '%xyz%' IN TABLE abc");
    }

    [Fact]
    public void test_projection_with_nested_trailing_commas()
    {
        var sql = "SELECT a, FROM b, LATERAL FLATTEN(input => events)";
        _ = Snowflake().ParseSqlStatements(sql);

        //Single nesting
        sql = "SELECT (SELECT a, FROM b, LATERAL FLATTEN(input => events))";
        _ = Snowflake().ParseSqlStatements(sql);

        //Double nesting
        sql = "SELECT (SELECT (SELECT a, FROM b, LATERAL FLATTEN(input => events)))";
        _ = Snowflake().ParseSqlStatements(sql);

        sql = "SELECT a, b, FROM c, (SELECT d, e, FROM f, LATERAL FLATTEN(input => events))";
        _ = Snowflake().ParseSqlStatements(sql);
    }

    [Fact]
    public void test_sf_double_dot_notation()
    {
        Snowflake().VerifiedStmt("SELECT * FROM db_name..table_name");
        Snowflake().VerifiedStmt("SELECT * FROM x, y..z JOIN a..b AS b ON x.id = b.id");

        Assert.Equal(
            "sql parser error: Expected: identifier, found: .",
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("SELECT * FROM X.Y.."))
                .Error
                .ToString());
        Assert.Equal(
            "sql parser error: Expected: identifier, found: .",
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("SELECT * FROM X..Y..Z"))
                .Error
                .ToString());
        Assert.Equal(
            "sql parser error: Expected: identifier, found: .",
            // Ensure we don't parse leading token
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("SELECT * FROM .X.Y"))
                .Error
                .ToString());
    }

    [Fact]
    public void test_parse_double_dot_notation_wrong_position()
    {
    }

    [Fact]
    public void parse_insert_overwrite()
    {
        var insertOverwriteInto = "INSERT OVERWRITE INTO schema.table SELECT a FROM b";
        Snowflake().VerifiedStmt(insertOverwriteInto);
    }

    [Fact]
    public void test_table_sample()
    {
        SnowflakeAndGeneric().VerifiedStmt("SELECT * FROM testtable SAMPLE (10)");
        SnowflakeAndGeneric().VerifiedStmt("SELECT * FROM testtable TABLESAMPLE (10)");
        SnowflakeAndGeneric()
            .VerifiedStmt("SELECT * FROM testtable AS t TABLESAMPLE BERNOULLI (10)");
        SnowflakeAndGeneric().VerifiedStmt("SELECT * FROM testtable AS t TABLESAMPLE ROW (10)");
        SnowflakeAndGeneric().VerifiedStmt("SELECT * FROM testtable AS t TABLESAMPLE ROW (10 ROWS)");
        SnowflakeAndGeneric()
            .VerifiedStmt("SELECT * FROM testtable TABLESAMPLE BLOCK (3) SEED (82)");
        SnowflakeAndGeneric()
            .VerifiedStmt("SELECT * FROM testtable TABLESAMPLE SYSTEM (3) REPEATABLE (82)");
        SnowflakeAndGeneric().VerifiedStmt("SELECT id FROM mytable TABLESAMPLE (10) REPEATABLE (1)");
        SnowflakeAndGeneric().VerifiedStmt("SELECT id FROM mytable TABLESAMPLE (10) SEED (1)");
    }

    [Fact]
    public void test_subquery_sample()
    {
        // Test SAMPLE clause on subqueries (derived tables)
        SnowflakeAndGeneric().VerifiedStmt("SELECT * FROM (SELECT * FROM mytable) SAMPLE (10)");
        SnowflakeAndGeneric()
            .VerifiedStmt("SELECT * FROM (SELECT * FROM mytable) SAMPLE (10000 ROWS)");
        SnowflakeAndGeneric()
            .VerifiedStmt("SELECT * FROM (SELECT * FROM mytable) AS t SAMPLE (50 PERCENT)");
        // Nested subquery with SAMPLE
        SnowflakeAndGeneric().VerifiedStmt(
            "SELECT * FROM (SELECT * FROM (SELECT report_from FROM mytable) SAMPLE (10000 ROWS)) AS anon_1");
        // SAMPLE with SEED on subquery
        SnowflakeAndGeneric()
            .VerifiedStmt("SELECT * FROM (SELECT * FROM mytable) SAMPLE (10) SEED (42)");
    }

    [Fact]
    public void test_multi_table_insert_unconditional()
    {
        // Basic unconditional multi-table insert
        // See: https://docs.snowflake.com/en/sql-reference/sql/insert-multi-table
        Snowflake().VerifiedStmt("INSERT ALL INTO t1 SELECT n1, n2, n3 FROM src");

        // Multiple INTO clauses
        Snowflake().VerifiedStmt("INSERT ALL INTO t1 INTO t2 SELECT n1, n2, n3 FROM src");

        // With column list
        Snowflake().VerifiedStmt("INSERT ALL INTO t1 (c1, c2, c3) SELECT n1, n2, n3 FROM src");

        // With VALUES clause
        Snowflake().VerifiedStmt(
            "INSERT ALL INTO t1 (c1, c2, c3) VALUES (n2, n1, DEFAULT) SELECT n1, n2, n3 FROM src");

        // Complex example from Snowflake docs
        Snowflake().VerifiedStmt(
            "INSERT ALL INTO t1 INTO t1 (c1, c2, c3) VALUES (n2, n1, DEFAULT) INTO t2 (c1, c2, c3) INTO t2 VALUES (n3, n2, n1) SELECT n1, n2, n3 FROM src");

        // With OVERWRITE
        Snowflake().VerifiedStmt("INSERT OVERWRITE ALL INTO t1 INTO t2 SELECT n1, n2, n3 FROM src");
    }

    [Fact]
    public void test_multi_table_insert_conditional()
    {
        // Basic conditional multi-table insert with WHEN clause
        // See: https://docs.snowflake.com/en/sql-reference/sql/insert-multi-table
        Snowflake().VerifiedStmt("INSERT ALL WHEN n1 > 100 THEN INTO t1 SELECT n1 FROM src");

        // Multiple WHEN clauses
        Snowflake().VerifiedStmt(
            "INSERT ALL WHEN n1 > 100 THEN INTO t1 WHEN n1 > 10 THEN INTO t2 SELECT n1 FROM src");

        // WHEN with multiple INTO clauses
        Snowflake().VerifiedStmt("INSERT ALL WHEN n1 > 10 THEN INTO t1 INTO t2 SELECT n1 FROM src");

        // With ELSE clause
        Snowflake()
            .VerifiedStmt("INSERT ALL WHEN n1 > 100 THEN INTO t1 ELSE INTO t2 SELECT n1 FROM src");

        // Complex conditional insert from Snowflake docs
        Snowflake().VerifiedStmt(
            "INSERT ALL WHEN n1 > 100 THEN INTO t1 WHEN n1 > 10 THEN INTO t1 INTO t2 ELSE INTO t2 SELECT n1 FROM src");

        // INSERT FIRST - only first matching WHEN clause executes
        Snowflake().VerifiedStmt(
            "INSERT FIRST WHEN n1 > 100 THEN INTO t1 WHEN n1 > 10 THEN INTO t1 INTO t2 ELSE INTO t2 SELECT n1 FROM src");

        // With OVERWRITE
        Snowflake().VerifiedStmt(
            "INSERT OVERWRITE ALL WHEN n1 > 100 THEN INTO t1 ELSE INTO t2 SELECT n1 FROM src");

        // WHEN with always-true condition
        Snowflake().VerifiedStmt("INSERT ALL WHEN 1 = 1 THEN INTO t1 SELECT n1 FROM src");
    }

    [Fact]
    public void test_multi_table_insert_with_values()
    {
        // INTO clause with VALUES using column references
        Snowflake().VerifiedStmt("INSERT ALL INTO t1 VALUES (n1, n2) SELECT n1, n2 FROM src");

        // INTO clause with VALUES using DEFAULT
        Snowflake().VerifiedStmt(
            "INSERT ALL INTO t1 (c1, c2, c3) VALUES (n1, n2, DEFAULT) SELECT n1, n2 FROM src");

        // INTO clause with VALUES using NULL
        Snowflake().VerifiedStmt(
            "INSERT ALL INTO t1 (c1, c2, c3) VALUES (n1, NULL, n2) SELECT n1, n2 FROM src");

        // Positional alias in VALUES
        Snowflake().VerifiedStmt("INSERT ALL INTO t1 VALUES ($1, $2) SELECT 1, 50 AS an_alias");
    }

    /// <summary>Unit tests for multi-table INSERT AST structure validation</summary>
    [Fact]
    public void test_multi_table_insert_ast_unconditional()
    {
        // Test basic unconditional multi-table insert AST
        var sql = "INSERT ALL INTO t1 INTO t2 (c1, c2) SELECT n1, n2 FROM src";
        var stmt = Snowflake().VerifiedStmt(sql);

        switch (stmt)
        {
            case Statement.Insert { Value: var insert }:
            {
                var multiTableInsertType = insert.MultiTableInsertType;
                var overwrite = insert.Overwrite;
                var multiTableIntoClauses = insert.MultiTableIntoClauses;
                var multiTableWhenClauses = insert.MultiTableWhenClauses;
                var multiTableElseClause = insert.MultiTableElseClause;
                var source = insert.Source;

                // Should be INSERT ALL (not FIRST)
                Assert.Equal(multiTableInsertType, new MultiTableInsertType.All());
                Assert.False(overwrite);

                // Should have 2 INTO clauses
                Assert.Equal(2, multiTableIntoClauses.Count);

                // First INTO clause: INTO t1
                Assert.Equal("t1", multiTableIntoClauses[0].TableName.ToString());
                Assert.True(multiTableIntoClauses[0].Columns.IsEmpty);
                Assert.Null(multiTableIntoClauses[0].Values);

                // Second INTO clause: INTO t2 (c1, c2)
                Assert.Equal("t2", multiTableIntoClauses[1].TableName.ToString());
                Assert.Equal(2, multiTableIntoClauses[1].Columns.Count);
                Assert.Equal("c1", multiTableIntoClauses[1].Columns[0].ToString());
                Assert.Equal("c2", multiTableIntoClauses[1].Columns[1].ToString());
                Assert.Null(multiTableIntoClauses[1].Values);

                // No WHEN clauses for unconditional insert
                Assert.True(multiTableWhenClauses.IsEmpty);
                Assert.Null(multiTableElseClause);

                // Should have source query
                Assert.NotNull(source);
                break;
            }
            default:
                Assert.Fail("Expected INSERT statement");
                break;
        }
    }

    [Fact]
    public void test_multi_table_insert_ast_with_values()
    {
        // Test INTO clause with VALUES
        var sql = "INSERT ALL INTO t1 (c1, c2, c3) VALUES (n1, n2, DEFAULT) SELECT n1, n2 FROM src";
        var stmt = Snowflake().VerifiedStmt(sql);

        switch (stmt)
        {
            case Statement.Insert { Value: { MultiTableIntoClauses: var multiTableIntoClauses } }:
            {
                Assert.Equal(1, multiTableIntoClauses.Count);

                var intoClause = multiTableIntoClauses[0];
                Assert.Equal("t1", intoClause.TableName.ToString());
                Assert.Equal(3, intoClause.Columns.Count);

                // Check VALUES clause
                var values = intoClause.Values ?? throw new InvalidOperationException("Expected VALUES clause");
                Assert.Equal(3, values.Values.Count);

                // First value: n1 (expression)
                switch (values.Values[0])
                {
                    case MultiTableInsertValue.Expr { Value: var expr }:
                        Assert.Equal("n1", expr.ToString());
                        break;
                    default:
                        Assert.Fail("Expected Expr");
                        break;
                }

                // Second value: n2 (expression)
                switch (values.Values[1])
                {
                    case MultiTableInsertValue.Expr { Value: var expr }:
                        Assert.Equal("n2", expr.ToString());
                        break;
                    default:
                        Assert.Fail("Expected Expr");
                        break;
                }

                // Third value: DEFAULT
                switch (values.Values[2])
                {
                    case MultiTableInsertValue.Default:
                        break;
                    default:
                        Assert.Fail("Expected DEFAULT");
                        break;
                }
                break;
            }
            default:
                Assert.Fail("Expected INSERT statement");
                break;
        }
    }

    [Fact]
    public void test_multi_table_insert_ast_conditional()
    {
        // Test conditional multi-table insert with WHEN clauses
        var sql = "INSERT ALL WHEN n1 > 100 THEN INTO t1 WHEN n1 > 10 THEN INTO t2 INTO t3 ELSE INTO t4 SELECT n1 FROM src";
        var stmt = Snowflake().VerifiedStmt(sql);

        switch (stmt)
        {
            case Statement.Insert { Value: var insert }:
            {
                var multiTableInsertType = insert.MultiTableInsertType;
                var multiTableIntoClauses = insert.MultiTableIntoClauses;
                var multiTableWhenClauses = insert.MultiTableWhenClauses;
                var multiTableElseClause = insert.MultiTableElseClause;

                // Should be INSERT ALL (not FIRST)
                Assert.Equal(multiTableInsertType, new MultiTableInsertType.All());

                // Unconditional INTO clauses should be empty for conditional insert
                Assert.True(multiTableIntoClauses.IsEmpty);

                // Should have 2 WHEN clauses
                Assert.Equal(2, multiTableWhenClauses.Count);

                // First WHEN clause: WHEN n1 > 100 THEN INTO t1
                Assert.Equal(
                    "n1 > 100",
                    multiTableWhenClauses[0].Condition.ToString());
                Assert.Equal(1, multiTableWhenClauses[0].IntoClauses.Count);
                Assert.Equal(
                    "t1",
                    multiTableWhenClauses[0].IntoClauses[0]
                        .TableName
                        .ToString());

                // Second WHEN clause: WHEN n1 > 10 THEN INTO t2 INTO t3
                Assert.Equal("n1 > 10", multiTableWhenClauses[1].Condition.ToString());
                Assert.Equal(2, multiTableWhenClauses[1].IntoClauses.Count);
                Assert.Equal(
                    "t2",
                    multiTableWhenClauses[1].IntoClauses[0]
                        .TableName
                        .ToString());
                Assert.Equal(
                    "t3",
                    multiTableWhenClauses[1].IntoClauses[1]
                        .TableName
                        .ToString());

                // ELSE clause: ELSE INTO t4
                var elseClause = multiTableElseClause ?? throw new InvalidOperationException("Expected ELSE clause");
                Assert.Equal(1, elseClause.Count);
                Assert.Equal("t4", elseClause[0].TableName.ToString());
                break;
            }
            default:
                Assert.Fail("Expected INSERT statement");
                break;
        }
    }

    [Fact]
    public void test_multi_table_insert_ast_first()
    {
        // Test INSERT FIRST vs INSERT ALL
        var sql =
            "INSERT FIRST WHEN n1 > 100 THEN INTO t1 WHEN n1 > 10 THEN INTO t2 SELECT n1 FROM src";
        var stmt = Snowflake().VerifiedStmt(sql);

        switch (stmt)
        {
            case Statement.Insert
            {
                Value: { MultiTableInsertType: var multiTableInsertType, MultiTableWhenClauses: var multiTableWhenClauses },
            }:
                // Should be INSERT FIRST
                Assert.Equal(multiTableInsertType, new MultiTableInsertType.First());
                Assert.Equal(2, multiTableWhenClauses.Count);
                break;
            default:
                Assert.Fail("Expected INSERT statement");
                break;
        }
    }

    [Fact]
    public void test_multi_table_insert_ast_overwrite()
    {
        // Test INSERT OVERWRITE ALL
        var sql = "INSERT OVERWRITE ALL INTO t1 INTO t2 SELECT n1 FROM src";
        var stmt = Snowflake().VerifiedStmt(sql);

        switch (stmt)
        {
            case Statement.Insert
            {
                Value:
                {
                    Overwrite: var overwrite,
                    MultiTableInsertType: var multiTableInsertType,
                    MultiTableIntoClauses: var multiTableIntoClauses,
                },
            }:
                Assert.True(overwrite);
                Assert.Equal(multiTableInsertType, new MultiTableInsertType.All());
                Assert.Equal(2, multiTableIntoClauses.Count);
                break;
            default:
                Assert.Fail("Expected INSERT statement");
                break;
        }
    }

    [Fact]
    public void test_multi_table_insert_ast_complex_values()
    {
        // Test complex VALUES with expressions
        var sql = "INSERT ALL INTO t1 VALUES (n1 + n2, n3 * 2, DEFAULT) SELECT n1, n2, n3 FROM src";
        var stmt = Snowflake().VerifiedStmt(sql);

        switch (stmt)
        {
            case Statement.Insert { Value: { MultiTableIntoClauses: var multiTableIntoClauses } }:
            {
                Assert.Equal(1, multiTableIntoClauses.Count);

                var values = multiTableIntoClauses[0]
                    .Values
                    ?? throw new InvalidOperationException("Expected VALUES");
                Assert.Equal(3, values.Values.Count);

                // First value: n1 + n2 (binary expression)
                switch (values.Values[0])
                {
                    case MultiTableInsertValue.Expr { Value: Expr.BinaryOp { Op: var op } }:
                        Assert.Equal(op, new BinaryOperator.Plus());
                        break;
                    default:
                        Assert.Fail("Expected BinaryOp expression");
                        break;
                }

                // Second value: n3 * 2 (binary expression)
                switch (values.Values[1])
                {
                    case MultiTableInsertValue.Expr { Value: Expr.BinaryOp { Op: var op } }:
                        Assert.Equal(op, new BinaryOperator.Multiply());
                        break;
                    default:
                        Assert.Fail("Expected BinaryOp expression");
                        break;
                }

                // Third value: DEFAULT
                Assert.IsType<MultiTableInsertValue.Default>(values.Values[2]);
                break;
            }
            default:
                Assert.Fail("Expected INSERT statement");
                break;
        }
    }

    [Fact]
    public void parse_ls_and_rm()
    {
        Snowflake().OneStatementParsesTo("LS @~", "LIST @~");
        Snowflake().OneStatementParsesTo("RM @~", "REMOVE @~");

        var statement = Snowflake()
            .VerifiedStmt("LIST @SNOWFLAKE_KAFKA_CONNECTOR_externalDataLakeSnowflakeConnector_STAGE_call_tracker_stream/");
        switch (statement)
        {
            case Statement.List { Value: var command }:
                Assert.Equal(command.Stage, ObjectName.From(Sequence.Of(Ident.From("@SNOWFLAKE_KAFKA_CONNECTOR_externalDataLakeSnowflakeConnector_STAGE_call_tracker_stream/"))));
                Assert.Null(command.Pattern);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        statement =
            Snowflake().VerifiedStmt("REMOVE @my_csv_stage/analysis/ PATTERN='.*data_0.*'");
        switch (statement)
        {
            case Statement.Remove { Value: var command }:
                Assert.Equal(
                    command.Stage,
                    ObjectName.From(Sequence.Of(Ident.From("@my_csv_stage/analysis/"))));
                Assert.Equal(".*data_0.*", command.Pattern);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        Snowflake().VerifiedStmt("LIST @\"STAGE_WITH_QUOTES\"");
        // Semi-colon after stage name - should terminate the stage name
        Snowflake()
            .ParseSqlStatements("LIST @db1.schema1.stage1/dir1/;");
    }

    [Fact]
    public void test_put()
    {
        var sql = "PUT 'file:///tmp/data.csv' @my_stage";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.Put
            {
                Source: var source,
                Stage: var stage,
                Options: var options,
            }:
                Assert.Equal("file:///tmp/data.csv", source);
                Assert.Equal(ObjectName.From(Sequence.Of(Ident.From("@my_stage"))), stage);
                Assert.True(options.Options.IsEmpty);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_put_with_quoted_stage()
    {
        // Stage names can be quoted (e.g. Snowflake driver `write_pandas`)
        var sql = "PUT 'file:///tmp/data.csv' @\"my stage\" PARALLEL=4";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.Put { Stage: var stage }:
                Assert.Equal(ObjectName.From(Sequence.Of(Ident.From("@\"my stage\""))), stage);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_put_with_options()
    {
        var sql =
            "PUT 'file:///tmp/data.csv' @my_stage " +
            "PARALLEL=8 AUTO_COMPRESS=true SOURCE_COMPRESSION=GZIP OVERWRITE=false";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.Put { Options: var options }:
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "PARALLEL",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Number("8", false).WithEmptySpan())),
                    options.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "AUTO_COMPRESS",
                        OptionValue: new KeyValueOptionKind.Single(new Value.Boolean(true).WithEmptySpan())),
                    options.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "SOURCE_COMPRESSION",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Placeholder("GZIP").WithEmptySpan())),
                    options.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "OVERWRITE",
                        OptionValue: new KeyValueOptionKind.Single(new Value.Boolean(false).WithEmptySpan())),
                    options.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_sql_keywords_as_select_item_ident()
    {
        // Some keywords that should be parsed as an alias
        var unreservedKws = new List<string> { "CLUSTER", "FETCH", "RETURNING", "LIMIT", "EXCEPT", "SORT" };
        foreach (var kw in unreservedKws)
        {
            Snowflake().VerifiedStmt($"SELECT 1, {kw}");
        }

        // Some keywords that should not be parsed as an alias
        foreach (var (kw, err) in new (string, string)[]
        {
            ("FROM", "Expected: identifier, found: EOF"),
            ("GROUP", "Expected: end of statement, found: GROUP"),
            ("HAVING", "Expected: an expression, found: EOF"),
            (
                "INTERSECT",
                "Expected: SELECT, VALUES, or a subquery in the query body, found: EOF"
            ),
            ("INTO", "Expected: an expression, found: EOF"),
            ("ORDER", "Expected: end of statement, found: ORDER"),
            ("SELECT", "Expected: end of statement, found: SELECT"),
            (
                "UNION",
                "Expected: SELECT, VALUES, or a subquery in the query body, found: EOF"
            ),
            ("WHERE", "Expected: an expression, found: EOF"),
            ("WITH", "Expected: end of statement, found: WITH"),
        })
        {
            Assert.Equal(
                Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements($"SELECT 1, {kw}")).Error,
                new ParserError.ParserErrorValue(err));
        }
    }

    [Fact]
    public void test_sql_keywords_as_select_item_aliases()
    {
        // Some keywords that should be parsed as an alias
        var unreservedKws = new List<string> { "CLUSTER", "FETCH", "RETURNING", "LIMIT", "EXCEPT", "SORT" };
        foreach (var kw in unreservedKws)
        {
            Snowflake()
                .OneStatementParsesTo($"SELECT 1 {kw}", $"SELECT 1 AS {kw}");
        }

        // Some keywords that should not be parsed as an alias
        foreach (var (kw, err) in new (string, string)[]
        {
            ("FROM", "Expected: identifier, found: EOF"),
            ("GROUP", "Expected: end of statement, found: GROUP"),
            ("HAVING", "Expected: an expression, found: EOF"),
            (
                "INTERSECT",
                "Expected: SELECT, VALUES, or a subquery in the query body, found: EOF"
            ),
            ("INTO", "Expected: an expression, found: EOF"),
            ("ORDER", "Expected: end of statement, found: ORDER"),
            ("SELECT", "Expected: end of statement, found: SELECT"),
            (
                "UNION",
                "Expected: SELECT, VALUES, or a subquery in the query body, found: EOF"
            ),
            ("WHERE", "Expected: an expression, found: EOF"),
            ("WITH", "Expected: end of statement, found: WITH"),
        })
        {
            Assert.Equal(
                Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements($"SELECT 1 {kw}")).Error,
                new ParserError.ParserErrorValue(err));
        }

        // LIMIT is alias
        Snowflake().OneStatementParsesTo("SELECT 1 LIMIT", "SELECT 1 AS LIMIT");
        // LIMIT is not an alias
        Snowflake().VerifiedStmt("SELECT 1 LIMIT 1");
        Snowflake().VerifiedStmt("SELECT 1 LIMIT $1");
        Snowflake().VerifiedStmt("SELECT 1 LIMIT ''");
        Snowflake().VerifiedStmt("SELECT 1 LIMIT NULL");
        Snowflake().VerifiedStmt("SELECT 1 LIMIT $$$$");
    }

    [Fact]
    public void test_sql_keywords_as_table_aliases()
    {
        // Some keywords that should be parsed as an alias implicitly
        var unreservedKws = new List<string>
        {
            "VIEW",
            "EXPLAIN",
            "ANALYZE",
            "SORT",
            "PIVOT",
            "UNPIVOT",
            "TOP",
            "LIMIT",
            "OFFSET",
            "FETCH",
            "EXCEPT",
            "CLUSTER",
            "DISTRIBUTE",
            "GLOBAL",
            "ANTI",
            "SEMI",
            "RETURNING",
            "OUTER",
            "WINDOW",
            "END",
            "PARTITION",
            "PREWHERE",
            "SETTINGS",
            "FORMAT",
            "MATCH_RECOGNIZE",
            "OPEN",
        };

        // fn assert_implicit_alias(mut select: Select, canonical_with_explicit_alias: &str)
        // O Rust altera `alias.explicit` no próprio `select`; aqui os records são imutáveis, então o
        // `select` é reconstruído com `with` (mesmo efeito observável).
        static void AssertImplicitAlias(Select select, string canonicalWithExplicitAlias)
        {
            var firstFrom = select
                .From
                .First
                ?? throw new InvalidOperationException("missing FROM");
            if (firstFrom.Relation is TableFactor.Table table)
            {
                var alias = table.Alias ?? throw new InvalidOperationException("missing ALIAS");
                Assert.False(alias.Explicit);
                alias = alias with { Explicit = true };
                var fromList = select.From.ToList();
                fromList[0] = firstFrom with { Relation = table with { Alias = alias } };
                select = select with { From = fromList.ToSequence() };
                Assert.Equal($"{select}", canonicalWithExplicitAlias);
            }
            else
            {
                Assert.Fail("unexpected FROM <table-factor>");
            }
        }

        // fn assert_no_alias(select: Select)
        static void AssertNoAlias(Select select)
        {
            if ((select.From.First ?? throw new InvalidOperationException("missing FROM")).Relation is TableFactor.Table { Alias: var alias })
            {
                Assert.Null(alias);
            }
            else
            {
                Assert.Fail("unexpected FROM <table-factor>");
            }
        }

        foreach (var kw in unreservedKws)
        {
            Snowflake().VerifiedStmt($"SELECT * FROM tbl AS {kw}");
            AssertImplicitAlias(
                Snowflake().VerifiedOnlySelect($"SELECT * FROM tbl {kw}"),
                $"SELECT * FROM tbl AS {kw}");
        }

        // Some keywords that should not be parsed as an alias implicitly
        foreach (var (kw, err) in new (string, string)[]
        {
            ("FROM", "Expected: end of statement, found: FROM"),
            ("GROUP", "Expected: end of statement, found: GROUP"),
            ("HAVING", "Expected: an expression, found: EOF"),
            ("ORDER", "Expected: end of statement, found: ORDER"),
            ("SELECT", "Expected: end of statement, found: SELECT"),
            (
                "UNION",
                "Expected: SELECT, VALUES, or a subquery in the query body, found: EOF"
            ),
            ("WHERE", "Expected: an expression, found: EOF"),
            ("WITH", "Expected: end of statement, found: WITH"),
        })
        {
            Assert.Equal(
                Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements($"SELECT * FROM tbl {kw}")).Error,
                new ParserError.ParserErrorValue(err));
        }

        // LIMIT is alias
        AssertImplicitAlias(
            Snowflake().VerifiedOnlySelect("SELECT * FROM tbl LIMIT"),
            "SELECT * FROM tbl AS LIMIT");

        // LIMIT is not an alias
        AssertNoAlias(Snowflake().VerifiedOnlySelect("SELECT * FROM tbl LIMIT 1"));
        AssertNoAlias(Snowflake().VerifiedOnlySelect("SELECT * FROM tbl LIMIT $1"));
        AssertNoAlias(Snowflake().VerifiedOnlySelect("SELECT * FROM tbl LIMIT ''"));
        AssertNoAlias(Snowflake().VerifiedOnlySelect("SELECT * FROM tbl LIMIT NULL"));
        AssertNoAlias(Snowflake().VerifiedOnlySelect("SELECT * FROM tbl LIMIT $$$$"));
    }

    [Fact]
    public void test_sql_keywords_as_table_factor()
    {
        // LIMIT is a table factor, Snowflake does not reserve it
        Snowflake().VerifiedStmt("SELECT * FROM tbl, LIMIT");
        // LIMIT is not a table factor
        Snowflake().OneStatementParsesTo("SELECT * FROM tbl, LIMIT 1", "SELECT * FROM tbl LIMIT 1");

        // Table functions are table factors
        Snowflake().VerifiedStmt("SELECT 1 FROM TABLE(GENERATOR(ROWCOUNT => 10)) AS a, TABLE(GENERATOR(ROWCOUNT => 10)) AS b");

        // ORDER is reserved
        Assert.Equal(
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("SELECT * FROM tbl, order")).Error,
            new ParserError.ParserErrorValue(
                "Expected: end of statement, found: order"));
    }

    [Fact]
    public void test_timetravel_at_before()
    {
        Snowflake().VerifiedOnlySelect("SELECT * FROM tbl AT(TIMESTAMP => '2024-12-15 00:00:00')");
        Snowflake()
            .VerifiedOnlySelect("SELECT * FROM tbl BEFORE(TIMESTAMP => '2024-12-15 00:00:00')");
    }

    [Fact]
    public void test_changes_clause()
    {
        // CHANGES with AT and END
        Snowflake().VerifiedStmt(
            "SELECT a FROM \"PCH_ODS_FIDELIO\".\"SRC_VW_SYS_ACC_MASTER\" CHANGES(INFORMATION => DEFAULT) AT(TIMESTAMP => TO_TIMESTAMP_TZ('2026-02-18 11:23:19.660000000')) END(TIMESTAMP => TO_TIMESTAMP_TZ('2026-02-18 11:38:30.211000000'))");

        // CHANGES with AT only (no END)
        Snowflake().VerifiedStmt(
            "SELECT a FROM t CHANGES(INFORMATION => DEFAULT) AT(TIMESTAMP => TO_TIMESTAMP_TZ('2026-02-18 11:23:19.660000000'))");

        // CHANGES with APPEND_ONLY
        Snowflake().VerifiedStmt(
            "SELECT a FROM t CHANGES(INFORMATION => APPEND_ONLY) AT(TIMESTAMP => TO_TIMESTAMP_TZ('2026-01-01 00:00:00'))");

        // CHANGES with OFFSET
        Snowflake().VerifiedStmt("SELECT a FROM t CHANGES(INFORMATION => DEFAULT) AT(OFFSET => -60)");

        // CHANGES with STATEMENT
        Snowflake().VerifiedStmt(
            "SELECT a FROM t CHANGES(INFORMATION => DEFAULT) AT(STATEMENT => '8e5d0ca9-005e-44e6-b858-a8f5b37c5726')");
    }

    [Fact]
    public void test_grant_account_global_privileges()
    {
        var privileges = new List<string>
        {
            "ALL",
            "ALL PRIVILEGES",
            "ATTACH POLICY",
            "AUDIT",
            "BIND SERVICE ENDPOINT",
            "IMPORT SHARE",
            "OVERRIDE SHARE RESTRICTIONS",
            "PURCHASE DATA EXCHANGE LISTING",
            "RESOLVE ALL",
            "READ SESSION",
        };
        var withGrantOptions = new List<string> { "", " WITH GRANT OPTION" };

        foreach (var p in privileges)
        {
            foreach (var wgo in withGrantOptions)
            {
                var sql = $"GRANT {p} ON ACCOUNT TO ROLE role1{wgo}";
                SnowflakeAndGeneric().VerifiedStmt(sql);
            }
        }

        var createObjectTypes = new List<string>
        {
            "ACCOUNT",
            "APPLICATION",
            "APPLICATION PACKAGE",
            "COMPUTE POOL",
            "DATA EXCHANGE LISTING",
            "DATABASE",
            "EXTERNAL VOLUME",
            "FAILOVER GROUP",
            "INTEGRATION",
            "NETWORK POLICY",
            "ORGANIZATION LISTING",
            "REPLICATION GROUP",
            "ROLE",
            "SHARE",
            "USER",
            "WAREHOUSE",
        };
        foreach (var t in createObjectTypes)
        {
            foreach (var wgo in withGrantOptions)
            {
                var sql = $"GRANT CREATE {t} ON ACCOUNT TO ROLE role1{wgo}";
                SnowflakeAndGeneric().VerifiedStmt(sql);
            }
        }

        var applyTypes = new List<string>
        {
            "AGGREGATION POLICY",
            "AUTHENTICATION POLICY",
            "JOIN POLICY",
            "MASKING POLICY",
            "PACKAGES POLICY",
            "PASSWORD POLICY",
            "PROJECTION POLICY",
            "ROW ACCESS POLICY",
            "SESSION POLICY",
            "TAG",
        };
        foreach (var t in applyTypes)
        {
            foreach (var wgo in withGrantOptions)
            {
                var sql = $"GRANT APPLY {t} ON ACCOUNT TO ROLE role1{wgo}";
                SnowflakeAndGeneric().VerifiedStmt(sql);
            }
        }

        var executeTypes = new List<string>
        {
            "ALERT",
            "DATA METRIC FUNCTION",
            "MANAGED ALERT",
            "MANAGED TASK",
            "TASK",
        };
        foreach (var t in executeTypes)
        {
            foreach (var wgo in withGrantOptions)
            {
                var sql = $"GRANT EXECUTE {t} ON ACCOUNT TO ROLE role1{wgo}";
                SnowflakeAndGeneric().VerifiedStmt(sql);
            }
        }

        var manageTypes = new List<string>
        {
            "ACCOUNT SUPPORT CASES",
            "EVENT SHARING",
            "GRANTS",
            "LISTING AUTO FULFILLMENT",
            "ORGANIZATION SUPPORT CASES",
            "USER SUPPORT CASES",
            "WAREHOUSES",
        };
        foreach (var t in manageTypes)
        {
            foreach (var wgo in withGrantOptions)
            {
                var sql = $"GRANT MANAGE {t} ON ACCOUNT TO ROLE role1{wgo}";
                SnowflakeAndGeneric().VerifiedStmt(sql);
            }
        }

        var monitorTypes = new List<string> { "EXECUTION", "SECURITY", "USAGE" };
        foreach (var t in monitorTypes)
        {
            foreach (var wgo in withGrantOptions)
            {
                var sql = $"GRANT MONITOR {t} ON ACCOUNT TO ROLE role1{wgo}";
                SnowflakeAndGeneric().VerifiedStmt(sql);
            }
        }
    }

    [Fact]
    public void test_grant_account_object_privileges()
    {
        var privileges = new List<string>
        {
            "ALL",
            "ALL PRIVILEGES",
            "APPLYBUDGET",
            "MODIFY",
            "MONITOR",
            "USAGE",
            "OPERATE",
        };

        var objectsTypes = new List<string>
        {
            "USER",
            "RESOURCE MONITOR",
            "WAREHOUSE",
            "COMPUTE POOL",
            "DATABASE",
            "INTEGRATION",
            "CONNECTION",
            "FAILOVER GROUP",
            "REPLICATION GROUP",
            "EXTERNAL VOLUME",
        };

        var withGrantOptions = new List<string> { "", " WITH GRANT OPTION" };

        foreach (var t in objectsTypes)
        {
            foreach (var p in privileges)
            {
                foreach (var wgo in withGrantOptions)
                {
                    var sql = $"GRANT {p} ON {t} obj1 TO ROLE role1{wgo}";
                    SnowflakeAndGeneric().VerifiedStmt(sql);
                }
            }
        }
    }

    [Fact]
    public void test_grant_role_to()
    {
        SnowflakeAndGeneric().VerifiedStmt("GRANT ROLE r1 TO ROLE r2");
        SnowflakeAndGeneric().VerifiedStmt("GRANT ROLE r1 TO USER u1");
    }

    [Fact]
    public void test_grant_database_role_to()
    {
        SnowflakeAndGeneric().VerifiedStmt("GRANT DATABASE ROLE r1 TO ROLE r2");
        SnowflakeAndGeneric().VerifiedStmt("GRANT DATABASE ROLE db1.sc1.r1 TO ROLE db1.sc1.r2");
    }

    [Fact]
    public void test_alter_session()
    {
        Assert.Equal(
            "sql parser error: expected at least one option",
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("ALTER SESSION SET"))
                .Error
                .ToString());
        Assert.Equal(
            "sql parser error: expected at least one option",
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("ALTER SESSION UNSET"))
                .Error
                .ToString());

        Snowflake().OneStatementParsesTo(
            "ALTER SESSION SET AUTOCOMMIT=TRUE",
            "ALTER SESSION SET AUTOCOMMIT=true");
        Snowflake().VerifiedStmt("ALTER SESSION SET AUTOCOMMIT=false QUERY_TAG='tag'");
        Snowflake().VerifiedStmt("ALTER SESSION UNSET AUTOCOMMIT");
        Snowflake().VerifiedStmt("ALTER SESSION UNSET AUTOCOMMIT, QUERY_TAG");
        Snowflake().OneStatementParsesTo(
            "ALTER SESSION SET A=false, B='tag';",
            "ALTER SESSION SET A=false B='tag'");
        Snowflake().OneStatementParsesTo(
            "ALTER SESSION SET A=true \nB='tag'",
            "ALTER SESSION SET A=true B='tag'");
        Snowflake().OneStatementParsesTo("ALTER SESSION UNSET a\nB", "ALTER SESSION UNSET a, B");
    }

    [Fact]
    public void test_alter_session_followed_by_statement()
    {
        var stmts = Snowflake()
            .ParseSqlStatements("ALTER SESSION SET QUERY_TAG='hello'; SELECT 42");
        // match stmts[..] { [Statement::AlterSession { .. }, Statement::Query { .. }] => {} .. }
        if (stmts.Count == 2 && stmts[0] is Statement.AlterSession && stmts[1] is Statement.Query)
        {
        }
        else
        {
            Assert.Fail($"Unexpected statements: [{string.Join(", ", stmts.Select(s => s.ToString()))}]");
        }
    }

    [Fact]
    public void parse_connect_by_root_operator()
    {
        var sql = "SELECT CONNECT_BY_ROOT name AS root_name FROM Tbl1";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.Query { Value: var query }:
                Assert.Equal(
                    (query.Body.AsSelect() ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value")).Projection[0],
                    new SelectItem.ExprWithAlias(
                        Expr: new Expr.Prefixed(
                            Prefix: new Ident("CONNECT_BY_ROOT"),
                            Value: new Expr.Identifier(new Ident("name"))),
                        Alias: new Ident("root_name")));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        sql = "SELECT CONNECT_BY_ROOT name FROM Tbl2";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.Query { Value: var query }:
                Assert.Equal(
                    (query.Body.AsSelect() ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value")).Projection[0],
                    new SelectItem.UnnamedExpr(new Expr.Prefixed(
                        Prefix: new Ident("CONNECT_BY_ROOT"),
                        Value: new Expr.Identifier(new Ident("name")))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        sql = "SELECT CONNECT_BY_ROOT FROM Tbl2";
        var res = Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(sql));
        Assert.Equal(
            "sql parser error: Expected an expression, found: FROM",
            res.Error.ToString());
    }

    [Fact]
    public void test_begin_exception_end()
    {
        foreach (var beginSql in new[]
        {
            "BEGIN SELECT 1; EXCEPTION WHEN OTHER THEN SELECT 2; RAISE; END",
            "BEGIN SELECT 1; EXCEPTION WHEN OTHER THEN SELECT 2; RAISE EX_1; END",
            "BEGIN SELECT 1; EXCEPTION WHEN FOO THEN SELECT 2; WHEN OTHER THEN SELECT 3; RAISE; END",
            "BEGIN BEGIN SELECT 1; EXCEPTION WHEN OTHER THEN SELECT 2; RAISE; END; END",
        })
        {
            Snowflake().VerifiedStmt(beginSql);
        }

        var sql =
            "\n" +
            "DECLARE\n" +
            "  EXCEPTION_1 EXCEPTION (-20001, 'I caught the expected exception.');\n" +
            "  EXCEPTION_2 EXCEPTION (-20002, 'Not the expected exception!');\n" +
            "  EXCEPTION_3 EXCEPTION (-20003, 'The worst exception...');\n" +
            "BEGIN\n" +
            "    BEGIN\n" +
            "        SELECT 1;\n" +
            "    EXCEPTION\n" +
            "        WHEN EXCEPTION_1 THEN\n" +
            "            SELECT 1;\n" +
            "        WHEN EXCEPTION_2 OR EXCEPTION_3 THEN\n" +
            "            SELECT 2;\n" +
            "            SELECT 3;\n" +
            "        WHEN OTHER THEN\n" +
            "            SELECT 4;\n" +
            "    RAISE;\n" +
            "    END;\n" +
            "END\n";

        // Outer `BEGIN` of the two nested `BEGIN` statements.
        if (Snowflake()
            .ParseSqlStatements(sql)
            .Last is not Statement.StartTransaction { Statements: var outerStatements })
        {
            throw new InvalidOperationException("unreachable");
        }

        // Inner `BEGIN` of the two nested `BEGIN` statements.
        if (outerStatements.Last is not Statement.StartTransaction
            {
                Statements: var statements,
                Exception: var exception,
                HasEndKeyword: var hasEndKeyword,
            })
        {
            throw new InvalidOperationException("unreachable");
        }

        Assert.Equal(1, statements.Count);
        Assert.True(hasEndKeyword);

        var exceptionValue = exception ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");
        Assert.Equal(3, exceptionValue.Count);
        Assert.Equal(1, exceptionValue[0].Idents.Count);
        Assert.Equal(1, exceptionValue[0].Statements.Count);
        Assert.Equal(2, exceptionValue[1].Idents.Count);
        Assert.Equal(2, exceptionValue[1].Statements.Count);
    }

    [Fact]
    public void test_begin_transaction()
    {
        Snowflake().VerifiedStmt("BEGIN TRANSACTION");
        Snowflake().VerifiedStmt("BEGIN WORK");

        // BEGIN TRANSACTION with statements
        var stmts = Snowflake()
            .ParseSqlStatements("BEGIN TRANSACTION; DROP TABLE IF EXISTS bla; COMMIT");
        Assert.Equal(3, stmts.Count);

        // Bare BEGIN (no TRANSACTION keyword) with statements
        stmts = Snowflake()
            .ParseSqlStatements("BEGIN; DROP TABLE IF EXISTS bla; COMMIT");
        Assert.Equal(3, stmts.Count);

        // Bare BEGIN at EOF (no semicolon, no TRANSACTION keyword)
        Snowflake().VerifiedStmt("BEGIN");
    }

    [Fact]
    public void test_snowflake_fetch_clause_syntax()
    {
        var canonical = "SELECT c1 FROM fetch_test FETCH FIRST 2 ROWS ONLY";
        Snowflake().VerifiedOnlySelectWithCanonical("SELECT c1 FROM fetch_test FETCH 2", canonical);

        Snowflake()
            .VerifiedOnlySelectWithCanonical("SELECT c1 FROM fetch_test FETCH FIRST 2", canonical);
        Snowflake()
            .VerifiedOnlySelectWithCanonical("SELECT c1 FROM fetch_test FETCH NEXT 2", canonical);

        Snowflake()
            .VerifiedOnlySelectWithCanonical("SELECT c1 FROM fetch_test FETCH 2 ROW", canonical);

        Snowflake().VerifiedOnlySelectWithCanonical(
            "SELECT c1 FROM fetch_test FETCH FIRST 2 ROWS",
            canonical);
    }

    [Fact]
    public void test_snowflake_create_view_with_multiple_column_options()
    {
        var createViewWithTag =
            "CREATE VIEW X (COL WITH TAG (pii='email') COMMENT 'foobar') AS SELECT * FROM Y";
        Snowflake().VerifiedStmt(createViewWithTag);
    }

    [Fact]
    public void test_snowflake_create_view_with_composite_tag()
    {
        var createViewWithTag =
            "CREATE VIEW X (COL WITH TAG (foo.bar.baz.pii='email')) AS SELECT * FROM Y";
        Snowflake().VerifiedStmt(createViewWithTag);
    }

    [Fact]
    public void test_snowflake_create_view_with_composite_policy_name()
    {
        var createViewWithTag =
            "CREATE VIEW X (COL WITH MASKING POLICY foo.bar.baz) AS SELECT * FROM Y";
        Snowflake().VerifiedStmt(createViewWithTag);
    }

    [Fact]
    public void test_snowflake_create_view_copy_grants()
    {
        Snowflake().VerifiedStmt("CREATE OR REPLACE VIEW bla COPY GRANTS AS (SELECT * FROM source)");
        Snowflake()
            .VerifiedStmt("CREATE OR REPLACE SECURE VIEW bla COPY GRANTS AS (SELECT * FROM source)");
        // COPY GRANTS with column list
        Snowflake().VerifiedStmt(
            "CREATE OR REPLACE VIEW bla COPY GRANTS (a, b) AS (SELECT a, b FROM source)");
    }

    [Fact]
    public void test_snowflake_create_view_copy_grants_after_columns()
    {
        var cases = new[]
        {
            (
                "CREATE OR REPLACE VIEW v (a, b) COPY GRANTS AS SELECT a, b FROM t",
                "CREATE OR REPLACE VIEW v COPY GRANTS (a, b) AS SELECT a, b FROM t"
            ),
            (
                "CREATE OR REPLACE SECURE VIEW v (a, b) COPY GRANTS AS SELECT a, b FROM t",
                "CREATE OR REPLACE SECURE VIEW v COPY GRANTS (a, b) AS SELECT a, b FROM t"
            ),
            (
                "CREATE MATERIALIZED VIEW v (a) COPY GRANTS AS SELECT a FROM t",
                "CREATE MATERIALIZED VIEW v COPY GRANTS (a) AS SELECT a FROM t"
            ),
        };
        foreach (var (sql, parsed) in cases)
        {
            Snowflake().OneStatementParsesTo(sql, parsed);
        }
        Snowflake().VerifiedStmt("CREATE OR REPLACE VIEW v (a) AS SELECT a FROM t");
    }

    [Fact]
    public void test_snowflake_identifier_function()
    {
        // Using IDENTIFIER to reference a column
        switch (Snowflake()
            .VerifiedOnlySelect("SELECT identifier('email') FROM customers")
            .Projection[0])
        {
            case SelectItem.UnnamedExpr { Value: Expr.Function { Value: { Name: var name, Args: var args } } }:
                Assert.Equal(name, ObjectName.From(Sequence.Of(new Ident("identifier"))));
                Assert.Equal<FunctionArguments>(
                    args,
                    new FunctionArguments.List(new FunctionArgumentList(
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            ValueWithSpan.From(new Value.SingleQuotedString("email")))))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty,
                        DuplicateTreatment: null)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Using IDENTIFIER to reference a case-sensitive column
        switch (Snowflake()
            .VerifiedOnlySelect("SELECT identifier('\"Email\"') FROM customers")
            .Projection[0])
        {
            case SelectItem.UnnamedExpr { Value: Expr.Function { Value: { Name: var name, Args: var args } } }:
                Assert.Equal(name, ObjectName.From(Sequence.Of(new Ident("identifier"))));
                Assert.Equal<FunctionArguments>(
                    args,
                    new FunctionArguments.List(new FunctionArgumentList(
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            ValueWithSpan.From(new Value.SingleQuotedString("\"Email\"")))))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty,
                        DuplicateTreatment: null)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Using IDENTIFIER to reference an alias of a table
        switch (Snowflake()
            .VerifiedOnlySelect("SELECT identifier('alias1').* FROM tbl AS alias1")
            .Projection[0])
        {
            case SelectItem.QualifiedWildcard
            {
                Kind: SelectItemQualifiedWildcardKind.Expr { Value: Expr.Function { Value: { Name: var name, Args: var args } } },
            }:
                Assert.Equal(name, ObjectName.From(Sequence.Of(new Ident("identifier"))));
                Assert.Equal<FunctionArguments>(
                    args,
                    new FunctionArguments.List(new FunctionArgumentList(
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            ValueWithSpan.From(new Value.SingleQuotedString("alias1")))))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty,
                        DuplicateTreatment: null)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Using IDENTIFIER to reference a database
        switch (Snowflake().VerifiedStmt("CREATE DATABASE IDENTIFIER('tbl')"))
        {
            case Statement.CreateDatabase { DbName: var dbName }:
                Assert.Equal(
                    dbName,
                    new ObjectName(Sequence.Of<ObjectNamePart>(new ObjectNamePart.Function(new ObjectNamePartFunction(
                        Name: new Ident("IDENTIFIER"),
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            ValueWithSpan.From(new Value.SingleQuotedString("tbl")))))))))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Using IDENTIFIER to reference a schema
        switch (Snowflake().VerifiedStmt("CREATE SCHEMA IDENTIFIER('db1.sc1')"))
        {
            case Statement.CreateSchema { SchemaName: var schemaName }:
                Assert.Equal<SchemaName>(
                    schemaName,
                    new SchemaName.Simple(new ObjectName(Sequence.Of<ObjectNamePart>(new ObjectNamePart.Function(
                        new ObjectNamePartFunction(
                            Name: new Ident("IDENTIFIER"),
                            Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                                ValueWithSpan.From(new Value.SingleQuotedString("db1.sc1"))))))))))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Using IDENTIFIER to reference a table
        switch (Snowflake().VerifiedStmt("CREATE TABLE IDENTIFIER('tbl') (id INT)"))
        {
            case Statement.CreateTable { Value: { Name: var name } }:
                Assert.Equal(
                    name,
                    new ObjectName(Sequence.Of<ObjectNamePart>(new ObjectNamePart.Function(new ObjectNamePartFunction(
                        Name: new Ident("IDENTIFIER"),
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            ValueWithSpan.From(new Value.SingleQuotedString("tbl")))))))))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Cannot have more than one IDENTIFIER part in an object name
        Assert.Equal(
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(
                "CREATE TABLE IDENTIFIER('db1').IDENTIFIER('sc1').IDENTIFIER('tbl') (id INT)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: column name or constraint definition, found: 'sc1'"));
        Assert.Equal(
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("CREATE TABLE IDENTIFIER('db1')..IDENTIFIER('tbl') (id INT)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: column name or constraint definition, found: 'tbl'"));
    }

    [Fact]
    public void test_create_database()
    {
        Snowflake().VerifiedStmt("CREATE DATABASE my_db");
        Snowflake().VerifiedStmt("CREATE OR REPLACE DATABASE my_db");
        Snowflake().VerifiedStmt("CREATE TRANSIENT DATABASE IF NOT EXISTS my_db");
        Snowflake().VerifiedStmt("CREATE DATABASE my_db CLONE src_db");
        Snowflake().VerifiedStmt(
            "CREATE OR REPLACE DATABASE my_db CLONE src_db DATA_RETENTION_TIME_IN_DAYS = 1");
        Snowflake().OneStatementParsesTo(
            "\n" +
            "        CREATE OR REPLACE TRANSIENT DATABASE IF NOT EXISTS my_db\n" +
            "        CLONE src_db\n" +
            "        DATA_RETENTION_TIME_IN_DAYS = 1\n" +
            "        MAX_DATA_EXTENSION_TIME_IN_DAYS = 5\n" +
            "        EXTERNAL_VOLUME = 'volume1'\n" +
            "        CATALOG = 'my_catalog'\n" +
            "        REPLACE_INVALID_CHARACTERS = TRUE\n" +
            "        DEFAULT_DDL_COLLATION = 'en-ci'\n" +
            "        STORAGE_SERIALIZATION_POLICY = COMPATIBLE\n" +
            "        COMMENT = 'This is my database'\n" +
            "        CATALOG_SYNC = 'sync_integration'\n" +
            "        CATALOG_SYNC_NAMESPACE_MODE = NEST\n" +
            "        CATALOG_SYNC_NAMESPACE_FLATTEN_DELIMITER = '/'\n" +
            "        WITH TAG (env = 'prod', team = 'data')\n" +
            "        WITH CONTACT (owner = 'admin', dpo = 'compliance')\n" +
            "    ",
            "CREATE OR REPLACE TRANSIENT DATABASE IF NOT EXISTS " +
            "my_db CLONE src_db DATA_RETENTION_TIME_IN_DAYS = 1 MAX_DATA_EXTENSION_TIME_IN_DAYS = 5 " +
            "EXTERNAL_VOLUME = 'volume1' CATALOG = 'my_catalog' " +
            "REPLACE_INVALID_CHARACTERS = TRUE DEFAULT_DDL_COLLATION = 'en-ci' " +
            "STORAGE_SERIALIZATION_POLICY = COMPATIBLE COMMENT = 'This is my database' " +
            "CATALOG_SYNC = 'sync_integration' CATALOG_SYNC_NAMESPACE_MODE = NEST " +
            "CATALOG_SYNC_NAMESPACE_FLATTEN_DELIMITER = '/' " +
            "WITH TAG (env='prod', team='data') " +
            "WITH CONTACT (owner = admin, dpo = compliance)");

        foreach (var sql in new[] { "CREATE DATABASE", "CREATE DATABASE my_db CLONE" })
        {
            TestUtils.AssertEqualWithMessage<ParserError>(
                Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(sql)).Error,
                new ParserError.ParserErrorValue(
                    "Expected: identifier, found: EOF"),
                sql);
        }
    }

    [Fact]
    public void test_timestamp_ntz_with_precision()
    {
        Snowflake().VerifiedStmt("SELECT CAST('2024-01-01 01:00:00' AS TIMESTAMP_NTZ(1))");
        Snowflake().VerifiedStmt("SELECT CAST('2024-01-01 01:00:00' AS TIMESTAMP_NTZ(9))");

        var select =
            Snowflake().VerifiedOnlySelect("SELECT CAST('2024-01-01 01:00:00' AS TIMESTAMP_NTZ(9))");
        switch (TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)))
        {
            case Expr.Cast { DataType: var dataType }:
                Assert.Equal<DataType>(dataType, new DataType.TimestampNtz(9));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_drop_constraints()
    {
        Snowflake().VerifiedStmt("ALTER TABLE tbl DROP PRIMARY KEY");
        Snowflake().VerifiedStmt("ALTER TABLE tbl DROP FOREIGN KEY k1");
        Snowflake().VerifiedStmt("ALTER TABLE tbl DROP CONSTRAINT c1");
        Snowflake().VerifiedStmt("ALTER TABLE tbl DROP PRIMARY KEY CASCADE");
        Snowflake().VerifiedStmt("ALTER TABLE tbl DROP FOREIGN KEY k1 RESTRICT");
        Snowflake().VerifiedStmt("ALTER TABLE tbl DROP CONSTRAINT c1 CASCADE");
    }

    [Fact]
    public void test_alter_dynamic_table()
    {
        Snowflake().VerifiedStmt("ALTER DYNAMIC TABLE MY_DYNAMIC_TABLE REFRESH");
        Snowflake().VerifiedStmt("ALTER DYNAMIC TABLE my_database.my_schema.my_dynamic_table REFRESH");
        Snowflake().VerifiedStmt("ALTER DYNAMIC TABLE my_dyn_table SUSPEND");
        Snowflake().VerifiedStmt("ALTER DYNAMIC TABLE my_dyn_table RESUME");
    }

    [Fact]
    public void test_alter_external_table()
    {
        Snowflake().VerifiedStmt("ALTER EXTERNAL TABLE some_table REFRESH");
        Snowflake().VerifiedStmt("ALTER EXTERNAL TABLE some_table REFRESH 'year=2025/month=12/'");
        Snowflake().VerifiedStmt("ALTER EXTERNAL TABLE IF EXISTS some_table REFRESH");
        Snowflake()
            .VerifiedStmt("ALTER EXTERNAL TABLE IF EXISTS some_table REFRESH 'year=2025/month=12/'");
    }

    [Fact]
    public void test_truncate_table_if_exists()
    {
        Snowflake().VerifiedStmt("TRUNCATE TABLE IF EXISTS my_table");
        Snowflake().VerifiedStmt("TRUNCATE TABLE my_table");
        Snowflake().VerifiedStmt("TRUNCATE IF EXISTS my_table");
    }

    [Fact]
    public void test_select_dollar_column_from_stage()
    {
        // With table function args and alias
        Snowflake().VerifiedStmt("SELECT t.$1, t.$2 FROM @mystage1(file_format => 'myformat', pattern => '.*data.*[.]csv.gz') t");
        // Without table function args, with alias
        Snowflake().VerifiedStmt("SELECT t.$1, t.$2 FROM @mystage1 t");
        // Without table function args, without alias
        Snowflake().VerifiedStmt("SELECT $1, $2 FROM @mystage1");
        // With table function args, without alias
        Snowflake().VerifiedStmt("SELECT $1, $2 FROM @mystage1(file_format => 'myformat')");
    }

    [Fact]
    public void test_structured_array_type()
    {
        Snowflake().OneStatementParsesTo(
            "CREATE TABLE t (a ARRAY(VARCHAR))",
            "CREATE TABLE t (a Array(VARCHAR))");
        Snowflake().OneStatementParsesTo(
            "SELECT CAST(a AS ARRAY(NUMBER(10, 2))) FROM t",
            "SELECT CAST(a AS Array(NUMBER(10, 2))) FROM t");
        Snowflake().VerifiedStmt("CREATE TABLE t (a ARRAY(VARCHAR NOT NULL))");
        var select =
            Snowflake().VerifiedOnlySelect("SELECT CAST(a AS ARRAY(VARCHAR NOT NULL)) FROM t");
        if (TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)) is not Expr.Cast { DataType: var dataType })
        {
            throw new InvalidOperationException("internal error: entered unreachable code");
        }
        Assert.Equal<DataType>(
            dataType,
            new DataType.Array(new ArrayElemTypeDef.ParenthesisNotNull(
                new DataType.Varchar(null))));
    }

    [Fact]
    public void test_structured_object_type()
    {
        SnowflakeAndGeneric().VerifiedStmt(
            "SELECT payload::OBJECT(address OBJECT(city VARCHAR NOT NULL), zip NUMBER) FROM t");
        Snowflake().OneStatementParsesTo(
            "SELECT payload::OBJECT(tags ARRAY, labels MAP(VARCHAR, VARCHAR)) FROM t",
            "SELECT payload::OBJECT(tags ARRAY, labels Map(VARCHAR, VARCHAR)) FROM t");
        var select = Snowflake().VerifiedOnlySelect(
            "SELECT payload::OBJECT(items ARRAY(NUMBER NOT NULL), meta MAP(VARCHAR, OBJECT(k NUMBER) NOT NULL)) FROM t");
        if (TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)) is not Expr.Cast { DataType: var dataType })
        {
            throw new InvalidOperationException("internal error: entered unreachable code");
        }
        if (dataType is not DataType.Object { Value: var fields })
        {
            throw new InvalidOperationException("internal error: entered unreachable code");
        }
        Assert.True(fields[0].DataType is DataType.Array { Value: ArrayElemTypeDef.ParenthesisNotNull });
        if (fields[1].DataType is not DataType.Map { ValueDataType: var value, Bracket: MapBracketKind.ParenthesesNotNull })
        {
            throw new InvalidOperationException("internal error: entered unreachable code");
        }
        Assert.IsType<DataType.Object>(value);

        Snowflake().OneStatementParsesTo(
            "SELECT payload::ARRAY(NUMBER) FROM t",
            "SELECT payload::Array(NUMBER) FROM t");
        Snowflake().OneStatementParsesTo(
            "SELECT payload::MAP(VARCHAR, OBJECT(k NUMBER)) FROM t",
            "SELECT payload::Map(VARCHAR, OBJECT(k NUMBER)) FROM t");
        Snowflake().VerifiedStmt("SELECT payload::ARRAY(NUMBER NOT NULL) FROM t");
        Snowflake().VerifiedStmt("SELECT payload::MAP(VARCHAR, NUMBER NOT NULL) FROM t");
        SnowflakeAndGeneric().VerifiedStmt("CREATE TABLE t (o OBJECT())");

        select = Snowflake().VerifiedOnlySelect(
            "SELECT CAST(payload AS OBJECT(city VARCHAR, zip NUMBER NOT NULL)) FROM t");
        // `let Expr::Cast { data_type, .. }` e `let DataType::Object(fields)` repetidos: nomes
        // distintos (CS0128, caso 72).
        if (TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)) is not Expr.Cast { DataType: var castDataType })
        {
            throw new InvalidOperationException("internal error: entered unreachable code");
        }
        if (castDataType is not DataType.Object { Value: var castFields })
        {
            throw new InvalidOperationException("internal error: entered unreachable code");
        }
        Assert.Equal(2, castFields.Count);
        Assert.Equal(castFields[0].Name, new Ident("city"));
        Assert.True(castFields[0].Options.IsEmpty);
        Assert.Equal(castFields[1].Name, new Ident("zip"));
        Assert.Equal(1, castFields[1].Options.Count);
        Assert.Equal<ColumnOption>(castFields[1].Options[0].Option, new ColumnOption.NotNull());

        Assert.Equal(
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("CREATE TABLE t (o OBJECT(city VARCHAR)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: ',' or ')' after column definition, found: EOF"));

        if (SnowflakeAndGeneric().VerifiedStmt("CREATE TABLE t (o OBJECT)")
            is not Statement.CreateTable { Value.Columns: var columns })
        {
            throw new InvalidOperationException("internal error: entered unreachable code");
        }

        Assert.Equal<DataType>(
            columns[0].DataType,
            new DataType.Custom(ObjectName.From(Sequence.Of(new Ident("OBJECT"))), Sequence<string>.Empty));
    }

    [Fact]
    public void test_snowflake_stage_name_with_escaped_quotes()
    {
        Snowflake().VerifiedStmt("REMOVE @````");
        Snowflake().OneStatementParsesTo("RM @````", "REMOVE @````");
        Snowflake().VerifiedStmt("REMOVE @\"stage\"\"name\"");
    }

    [Fact]
    public void test_stage_name_delimiters()
    {
        Snowflake().VerifiedStmt("SELECT * FROM @stage1, @stage2");
        Snowflake().VerifiedStmt("SELECT * FROM @stage, my_table");
        Snowflake().VerifiedStmt("SELECT * FROM my_table, @stage");
        Snowflake().VerifiedStmt("SELECT * FROM @namespace.stage_name, item");
        Snowflake().VerifiedStmt("SELECT * FROM @stage(file_format => 'myformat'), my_table");
        Snowflake().VerifiedStmt("SELECT * FROM @stage AS s, my_table");
        Snowflake().VerifiedStmt("SELECT * FROM @stage s, my_table");
        var stmts = Snowflake()
            .ParseSqlStatements("SELECT * FROM @stage; SELECT 1");
        Assert.Equal(2, stmts.Count);

        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("SELECT * FROM @")).Error,
            new ParserError.ParserErrorValue("Expected: stage name identifier, found: EOF"));
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("SELECT * FROM @;")).Error,
            new ParserError.ParserErrorValue("Expected: stage name identifier, found: ;"));
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("SELECT * FROM @, item")).Error,
            new ParserError.ParserErrorValue("Expected: stage name identifier, found: ,"));
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("SELECT * FROM @.stage")).Error,
            new ParserError.ParserErrorValue("Expected: stage name identifier, found: ."));
    }
}
#pragma warning restore xUnit2013
