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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_databricks.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

// Notas da tradução (onda-c/C16.md):
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000). `assert_eq!(x, None)` → `Assert.Null(x)` (xUnit2003).
// - `panic!`/`unreachable!` num `match` → `default: throw new InvalidOperationException(..)`.
// - `concat!(..)` e strings com `\` no fim da linha viram concatenações dos mesmos pedaços.
#pragma warning disable xUnit2013
public class SqlParserDatabricksTests
{
    private static TestedDialects Databricks()
    {
        return new TestedDialects(new List<Dialect> { new DatabricksDialect() });
    }

    private static TestedDialects DatabricksAndGeneric()
    {
        return new TestedDialects(new List<Dialect>
        {
            new DatabricksDialect(),
            new GenericDialect(),
        });
    }

    [Fact]
    public void test_databricks_identifiers()
    {
        // databricks uses backtick for delimited identifiers
        Assert.Equal(
            Databricks().VerifiedOnlySelect("SELECT `Ä`").Projection[0],
            new SelectItem.UnnamedExpr(new Expr.Identifier(Ident.WithQuote('`', "Ä"))));

        // double quotes produce string literals, not delimited identifiers
        Assert.Equal(
            Databricks()
                .VerifiedOnlySelect("SELECT \"Ä\"")
                .Projection[0],
            new SelectItem.UnnamedExpr(new Expr.Value(
                new Value.DoubleQuotedString("Ä").WithEmptySpan())));
    }

    [Fact]
    public void test_databricks_exists()
    {
        // exists is a function in databricks
        Assert.Equal(
            Databricks().VerifiedExpr("exists(array(1, 2, 3), x -> x IS NULL)"),
            TestUtils.Call(
                "exists",
                new Expr[]
                {
                    TestUtils.Call(
                        "array",
                        new[]
                        {
                            Expr.MakeValue(TestUtils.Number("1")),
                            Expr.MakeValue(TestUtils.Number("2")),
                            Expr.MakeValue(TestUtils.Number("3")),
                        }),
                    new Expr.Lambda(new LambdaFunction(
                        Params: new OneOrManyWithParens<LambdaFunctionParameter>.One(new LambdaFunctionParameter(
                            Name: new Ident("x"),
                            DataType: null)),
                        Body: new Expr.IsNull(new Expr.Identifier(new Ident("x"))),
                        Syntax: LambdaSyntax.Arrow)),
                }));

        var ex = Assert.Throws<ParserException>(() => Databricks().ParseSqlStatements("SELECT EXISTS ("));
        Assert.Equal(
            // TODO: improve this error message...
            new ParserError.ParserErrorValue("Expected: an expression, found: EOF"),
            ex.Error);
    }

    [Fact]
    public void test_databricks_lambdas()
    {
        var sql = "SELECT array_sort(array('Hello', 'World'), "
            + "(p1, p2) -> CASE WHEN p1 = p2 THEN 0 "
            + "WHEN reverse(p1) < reverse(p2) THEN -1 "
            + "ELSE 1 END)";
        Assert.Equal(
            new SelectItem.UnnamedExpr(TestUtils.Call(
                "array_sort",
                new Expr[]
                {
                    TestUtils.Call(
                        "array",
                        new[]
                        {
                            Expr.MakeValue(new Value.SingleQuotedString("Hello")),
                            Expr.MakeValue(new Value.SingleQuotedString("World")),
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
            Databricks().VerifiedOnlySelect(sql).Projection[0]);

        Databricks().VerifiedExpr(
            "map_zip_with(map(1, 'a', 2, 'b'), map(1, 'x', 2, 'y'), (k, v1, v2) -> concat(v1, v2))");
        Databricks().VerifiedExpr("transform(array(1, 2, 3), x -> x + 1)");
    }

    [Fact]
    public void test_values_clause()
    {
        var values = new Values(
            ValueKeyword: false,
            ExplicitRow: false,
            Rows: Sequence.Of(
                Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of(
                    new Expr.Value(new Value.DoubleQuotedString("one").WithEmptySpan()),
                    Expr.MakeValue(TestUtils.Number("1")))),
                Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of(
                    new Expr.Value(new Value.SingleQuotedString("two").WithEmptySpan()),
                    Expr.MakeValue(TestUtils.Number("2"))))));

        var query = Databricks().VerifiedQuery("VALUES (\"one\", 1), ('two', 2)");
        Assert.Equal(new SetExpr.Values(values), query.Body);

        // VALUES is permitted in a FROM clause without a subquery
        query = Databricks().VerifiedQueryWithCanonical(
            "SELECT * FROM VALUES (\"one\", 1), ('two', 2)",
            "SELECT * FROM (VALUES (\"one\", 1), ('two', 2))");
        if (query
            .Body
            .AsSelect()?
            .From[0].Relation is not TableFactor.Derived { Subquery: var subquery })
        {
            throw new InvalidOperationException("expected subquery");
        }
        Assert.Equal(new SetExpr.Values(values), subquery.Body);

        // values is also a valid table name
        query = DatabricksAndGeneric().VerifiedQuery(
            "WITH values AS (SELECT 42) "
            + "SELECT * FROM values");
        Assert.Equal(
            TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident(
                "values")))),
            query
                .Body
                .AsSelect()?
                .From[0].Relation);

        // TODO: support this example from https://docs.databricks.com/en/sql/language-manual/sql-ref-syntax-qry-select-values.html#examples
        // databricks().verified_query("VALUES 1, 2, 3");
    }

    [Fact]
    public void parse_use()
    {
        var validObjectNames = new[] { "mydb", "WAREHOUSE", "DEFAULT" };
        var quoteStyles = new[] { '"', '`' };

        foreach (var objectName in validObjectNames)
        {
            // Test single identifier without quotes
            Assert.Equal(
                Databricks().VerifiedStmt($"USE {objectName}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(new Ident(
                    objectName))))));
            foreach (var quote in quoteStyles)
            {
                // Test single identifier with different type of quotes
                Assert.Equal(
                    Databricks().VerifiedStmt($"USE {quote}{objectName}{quote}"),
                    new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(Ident.WithQuote(
                        quote,
                        objectName))))));
            }
        }

        foreach (var quote in quoteStyles)
        {
            // Test single identifier with keyword and different type of quotes
            Assert.Equal(
                Databricks().VerifiedStmt($"USE CATALOG {quote}my_catalog{quote}"),
                new Statement.Use(new Use.Catalog(ObjectName.From(Sequence.Of(Ident.WithQuote(
                    quote,
                    "my_catalog"))))));
            Assert.Equal(
                Databricks().VerifiedStmt($"USE DATABASE {quote}my_database{quote}"),
                new Statement.Use(new Use.Database(ObjectName.From(Sequence.Of(Ident.WithQuote(
                    quote,
                    "my_database"))))));
            Assert.Equal(
                Databricks().VerifiedStmt($"USE SCHEMA {quote}my_schema{quote}"),
                new Statement.Use(new Use.Schema(ObjectName.From(Sequence.Of(Ident.WithQuote(
                    quote,
                    "my_schema"))))));
        }

        // Test single identifier with keyword and no quotes
        Assert.Equal(
            Databricks().VerifiedStmt("USE CATALOG my_catalog"),
            new Statement.Use(new Use.Catalog(ObjectName.From(Sequence.Of(new Ident(
                "my_catalog"))))));
        Assert.Equal(
            Databricks().VerifiedStmt("USE DATABASE my_schema"),
            new Statement.Use(new Use.Database(ObjectName.From(Sequence.Of(new Ident(
                "my_schema"))))));
        Assert.Equal(
            Databricks().VerifiedStmt("USE SCHEMA my_schema"),
            new Statement.Use(new Use.Schema(ObjectName.From(Sequence.Of(new Ident("my_schema"))))));

        // Test invalid syntax - missing identifier
        var invalidCases = new[] { "USE SCHEMA", "USE DATABASE", "USE CATALOG" };
        foreach (var sql in invalidCases)
        {
            var ex = Assert.Throws<ParserException>(() => Databricks().ParseSqlStatements(sql));
            Assert.Equal(
                ex.Error,
                new ParserError.ParserErrorValue("Expected: identifier, found: EOF"));
        }
    }

    [Fact]
    public void parse_show_catalogs()
    {
        Databricks().VerifiedStmt("SHOW CATALOGS");
        Databricks().VerifiedStmt("SHOW TERSE CATALOGS");
        Databricks().VerifiedStmt("SHOW CATALOGS HISTORY");
        Databricks().VerifiedStmt("SHOW CATALOGS LIKE 'pay*'");
        Databricks().VerifiedStmt("SHOW CATALOGS 'pay*'");
        Databricks().VerifiedStmt("SHOW CATALOGS STARTS WITH 'pay'");
        Databricks().VerifiedStmt("SHOW CATALOGS LIMIT 10");
        Databricks().VerifiedStmt("SHOW CATALOGS HISTORY STARTS WITH 'pay'");

        switch (Databricks().VerifiedStmt("SHOW CATALOGS LIKE 'pay*'"))
        {
            case Statement.ShowCatalogs showCatalogs:
            {
                var terse = showCatalogs.Terse;
                var history = showCatalogs.History;
                var showOptions = showCatalogs.ShowOptions;
                Assert.False(terse);
                Assert.False(history);
                Assert.Null(showOptions.ShowIn);
                Assert.Null(showOptions.StartsWith);
                Assert.Null(showOptions.Limit);
                Assert.Null(showOptions.LimitFrom);
                Assert.Equal(
                    showOptions.FilterPosition,
                    new ShowStatementFilterPosition.Suffix(
                        new ShowStatementFilter.Like("pay*")));
                break;
            }
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }
    }

    [Fact]
    public void parse_show_catalogs_with_show_options()
    {
        Databricks().VerifiedStmt("SHOW TERSE CATALOGS HISTORY IN ACCOUNT");

        switch (Databricks().VerifiedStmt("SHOW TERSE CATALOGS HISTORY IN ACCOUNT"))
        {
            case Statement.ShowCatalogs showCatalogs:
            {
                var terse = showCatalogs.Terse;
                var history = showCatalogs.History;
                var showOptions = showCatalogs.ShowOptions;
                Assert.True(terse);
                Assert.True(history);
                Assert.Null(showOptions.FilterPosition);
                Assert.True(
                    showOptions.ShowIn is ShowStatementIn
                    {
                        ParentType: ShowStatementInParentType.Account,
                        ParentName: null,
                    });
                break;
            }
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }
    }

    [Fact]
    public void parse_databricks_struct_function()
    {
        Assert.Equal(
            DatabricksAndGeneric()
                .VerifiedOnlySelect("SELECT STRUCT(1, 'foo')")
                .Projection[0],
            new SelectItem.UnnamedExpr(new Expr.Struct(
                Values: Sequence.Of<Expr>(
                    Expr.MakeValue(TestUtils.Number("1")),
                    new Expr.Value(new Value.SingleQuotedString("foo").WithEmptySpan())),
                Fields: Sequence<StructField>.Empty)));
        Assert.Equal(
            DatabricksAndGeneric()
                .VerifiedOnlySelect("SELECT STRUCT(1 AS one, 'foo' AS foo, false)")
                .Projection[0],
            new SelectItem.UnnamedExpr(new Expr.Struct(
                Values: Sequence.Of<Expr>(
                    new Expr.Named(
                        Expr: Expr.MakeValue(TestUtils.Number("1")),
                        Name: new Ident("one")),
                    new Expr.Named(
                        Expr: new Expr.Value(
                            new Value.SingleQuotedString("foo").WithEmptySpan()),
                        Name: new Ident("foo")),
                    new Expr.Value(new Value.Boolean(false).WithEmptySpan())),
                Fields: Sequence<StructField>.Empty)));
    }

    [Fact]
    public void data_type_timestamp_ntz()
    {
        // Literal
        Assert.Equal(
            Databricks().VerifiedExpr("TIMESTAMP_NTZ '2025-03-29T18:52:00'"),
            new Expr.TypedString(new TypedString(
                DataType: new DataType.TimestampNtz(null),
                Value: new ValueWithSpan(
                    Value: new Value.SingleQuotedString("2025-03-29T18:52:00"),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)));

        // Cast
        Assert.Equal(
            Databricks().VerifiedExpr("(created_at)::TIMESTAMP_NTZ"),
            new Expr.Cast(
                Kind: CastKind.DoubleColon,
                Expr: new Expr.Nested(new Expr.Identifier(
                    Ident.From("created_at"))),
                DataType: new DataType.TimestampNtz(null),
                Format: null));

        // Column definition
        switch (Databricks().VerifiedStmt("CREATE TABLE foo (x TIMESTAMP_NTZ)"))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(new ColumnDef(
                        Name: Ident.From("x"),
                        DataType: new DataType.TimestampNtz(null),
                        Options: Sequence<ColumnOptionDef>.Empty)));
                break;
            case var s:
                throw new InvalidOperationException($"Unexpected statement: {s}");
        }
    }

    [Fact]
    public void parse_table_time_travel()
    {
        TestUtils.AllDialectsWhere(d => d.SupportsTableVersioning)
            .VerifiedOnlySelect("SELECT 1 FROM t1 TIMESTAMP AS OF '2018-10-18T22:15:12.013Z'");

        TestUtils.AllDialectsWhere(d => d.SupportsTableVersioning).VerifiedOnlySelect(
            "SELECT 1 FROM t1 TIMESTAMP AS OF CURRENT_TIMESTAMP() - INTERVAL 12 HOURS");

        TestUtils.AllDialectsWhere(d => d.SupportsTableVersioning)
            .VerifiedOnlySelect("SELECT 1 FROM t1 VERSION AS OF 1");

        Assert.Equal(
            Assert.Throws<ParserException>(() => Databricks()
                .ParseSqlStatements("SELECT 1 FROM t1 FOR TIMESTAMP AS OF 'some_timestamp'")).Error,
            new ParserError.ParserErrorValue(
                "Expected: one of UPDATE or SHARE, found: TIMESTAMP"));

        Assert.Equal(
            Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => d.SupportsTableVersioning)
                .ParseSqlStatements("SELECT 1 FROM t1 VERSION AS OF 1 - 2")).Error,
            new ParserError.ParserErrorValue(
                "Expected: end of statement, found: -"));
    }

    [Fact]
    public void parse_optimize_table()
    {
        // Basic OPTIMIZE (Databricks style - no TABLE keyword)
        Databricks().VerifiedStmt("OPTIMIZE my_table");
        Databricks().VerifiedStmt("OPTIMIZE db.my_table");
        Databricks().VerifiedStmt("OPTIMIZE catalog.db.my_table");

        // With WHERE clause
        Databricks().VerifiedStmt("OPTIMIZE my_table WHERE date = '2023-01-01'");
        Databricks()
            .VerifiedStmt("OPTIMIZE my_table WHERE date >= '2023-01-01' AND date < '2023-02-01'");

        // With ZORDER BY clause
        Databricks().VerifiedStmt("OPTIMIZE my_table ZORDER BY (col1)");
        Databricks().VerifiedStmt("OPTIMIZE my_table ZORDER BY (col1, col2)");
        Databricks().VerifiedStmt("OPTIMIZE my_table ZORDER BY (col1, col2, col3)");

        // Combined WHERE and ZORDER BY
        Databricks().VerifiedStmt("OPTIMIZE my_table WHERE date = '2023-01-01' ZORDER BY (col1)");
        Databricks()
            .VerifiedStmt("OPTIMIZE my_table WHERE date >= '2023-01-01' ZORDER BY (col1, col2)");

        // Verify AST structure
        switch (Databricks()
            .VerifiedStmt("OPTIMIZE my_table WHERE date = '2023-01-01' ZORDER BY (col1, col2)"))
        {
            case Statement.OptimizeTable optimizeTable:
            {
                var name = optimizeTable.Name;
                var hasTableKeyword = optimizeTable.HasTableKeyword;
                var onCluster = optimizeTable.OnCluster;
                var partition = optimizeTable.Partition;
                var includeFinal = optimizeTable.IncludeFinal;
                var deduplicate = optimizeTable.Deduplicate;
                var predicate = optimizeTable.Predicate;
                var zorder = optimizeTable.Zorder;
                Assert.Equal("my_table", name.ToString());
                Assert.False(hasTableKeyword);
                Assert.Null(onCluster);
                Assert.Null(partition);
                Assert.False(includeFinal);
                Assert.Null(deduplicate);
                Assert.NotNull(predicate);
                Assert.Equal(
                    zorder,
                    Sequence.Of<Expr>(
                        new Expr.Identifier(new Ident("col1")),
                        new Expr.Identifier(new Ident("col2"))));
                break;
            }
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }

        // Negative cases
        var ex = Assert.Throws<ParserException>(() => Databricks()
            .ParseSqlStatements("OPTIMIZE my_table ZORDER BY"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: (, found: EOF"));
        ex = Assert.Throws<ParserException>(() => Databricks()
            .ParseSqlStatements("OPTIMIZE my_table ZORDER BY ()"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: an expression, found: )"));
    }

    [Fact]
    public void parse_create_table_partitioned_by()
    {
        // Databricks allows PARTITIONED BY with just column names (referencing existing columns)
        // https://docs.databricks.com/en/sql/language-manual/sql-ref-partition.html

        // Single partition column without type
        Databricks().VerifiedStmt("CREATE TABLE t (col1 STRING, col2 INT) PARTITIONED BY (col1)");

        // Multiple partition columns without types
        Databricks().VerifiedStmt(
            "CREATE TABLE t (col1 STRING, col2 INT, col3 DATE) PARTITIONED BY (col1, col2)");

        // Partition columns with types (new columns not in table spec)
        Databricks().VerifiedStmt("CREATE TABLE t (name STRING) PARTITIONED BY (year INT, month INT)");

        // Mixed: some with types, some without
        Databricks()
            .VerifiedStmt("CREATE TABLE t (id INT, name STRING) PARTITIONED BY (region, year INT)");

        // Verify AST structure for column without type
        switch (Databricks().VerifiedStmt("CREATE TABLE t (col1 STRING) PARTITIONED BY (col1)"))
        {
            case Statement.CreateTable { Value: var createTable }:
            {
                var name = createTable.Name;
                var columns = createTable.Columns;
                var hiveDistribution = createTable.HiveDistribution;
                Assert.Equal("t", name.ToString());
                Assert.Equal(1, columns.Count);
                Assert.Equal("col1", columns[0].Name.ToString());
                switch (hiveDistribution)
                {
                    case HiveDistributionStyle.PARTITIONED { Columns: var partitionCols }:
                        Assert.Equal(1, partitionCols.Count);
                        Assert.Equal("col1", partitionCols[0].Name.ToString());
                        Assert.Equal(partitionCols[0].DataType, new DataType.Unspecified());
                        break;
                    default:
                        throw new InvalidOperationException("internal error: entered unreachable code");
                }
                break;
            }
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }

        // Verify AST structure for column with type
        switch (Databricks().VerifiedStmt("CREATE TABLE t (name STRING) PARTITIONED BY (year INT)"))
        {
            case Statement.CreateTable
            {
                Value.HiveDistribution: HiveDistributionStyle.PARTITIONED
                {
                    Columns: var partitionCols,
                },
            }:
                Assert.Equal(1, partitionCols.Count);
                Assert.Equal("year", partitionCols[0].Name.ToString());
                Assert.Equal(partitionCols[0].DataType, new DataType.Int(null));
                break;
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }
    }

    [Fact]
    public void parse_databricks_struct_type()
    {
        // Databricks uses colon-separated struct field syntax (colon is optional)
        // https://docs.databricks.com/en/sql/language-manual/data-types/struct-type.html

        // Basic struct with colon syntax - parses to canonical form without colons
        Databricks().OneStatementParsesTo(
            "CREATE TABLE t (col1 STRUCT<field1: STRING, field2: INT>)",
            "CREATE TABLE t (col1 STRUCT<field1 STRING, field2 INT>)");

        // Nested array of struct (the original issue case)
        Databricks().OneStatementParsesTo(
            "CREATE TABLE t (col1 ARRAY<STRUCT<finish_flag: STRING, survive_flag: STRING, score: INT>>)",
            "CREATE TABLE t (col1 ARRAY<STRUCT<finish_flag STRING, survive_flag STRING, score INT>>)");

        // Multiple struct columns
        Databricks().OneStatementParsesTo(
            "CREATE TABLE t (col1 STRUCT<a: INT, b: STRING>, col2 STRUCT<x: DOUBLE>)",
            "CREATE TABLE t (col1 STRUCT<a INT, b STRING>, col2 STRUCT<x DOUBLE>)");

        // Deeply nested structs
        Databricks().OneStatementParsesTo(
            "CREATE TABLE t (col1 STRUCT<outer: STRUCT<inner: STRING>>)",
            "CREATE TABLE t (col1 STRUCT<outer STRUCT<inner STRING>>)");

        // Struct with array field
        Databricks().OneStatementParsesTo(
            "CREATE TABLE t (col1 STRUCT<items: ARRAY<INT>, name: STRING>)",
            "CREATE TABLE t (col1 STRUCT<items ARRAY<INT>, name STRING>)");

        // Syntax without colons should also work (BigQuery compatible)
        Databricks().VerifiedStmt("CREATE TABLE t (col1 STRUCT<field1 STRING, field2 INT>)");

        // Verify AST structure
        switch (Databricks().OneStatementParsesTo(
            "CREATE TABLE t (col1 STRUCT<field1: STRING, field2: INT>)",
            "CREATE TABLE t (col1 STRUCT<field1 STRING, field2 INT>)"))
        {
            case Statement.CreateTable { Value: var createTable }:
            {
                var columns = createTable.Columns;
                Assert.Equal(1, columns.Count);
                Assert.Equal("col1", columns[0].Name.ToString());
                switch (columns[0].DataType)
                {
                    case DataType.Struct { Fields: var fields, Bracket: StructBracketKind.AngleBrackets }:
                        Assert.Equal(2, fields.Count);
                        Assert.Equal(
                            "field1",
                            fields[0].FieldName?.ToString());
                        Assert.Equal(fields[0].FieldType, new DataType.String(null));
                        Assert.Equal(
                            "field2",
                            fields[1].FieldName?.ToString());
                        Assert.Equal(fields[1].FieldType, new DataType.Int(null));
                        break;
                    default:
                        throw new InvalidOperationException("internal error: entered unreachable code");
                }
                break;
            }
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }
    }

    [Fact]
    public void parse_databricks_json_accessor()
    {
        // Basic colon accessor — unquoted field names are case-insensitive
        Databricks().VerifiedOnlySelect("SELECT raw:owner, RAW:owner FROM store_data");

        // Unquoted field access is case-insensitive; bracket notation is case-sensitive.
        Databricks().VerifiedOnlySelect(
            "SELECT raw:OWNER AS case_insensitive, raw:['OWNER'] AS case_sensitive FROM store_data");

        // Backtick-quoted keys (Databricks delimited identifiers) normalise to double-quoted output.
        Databricks().OneStatementParsesTo(
            "SELECT raw:`zip code`, raw:`Zip Code`, raw:['fb:testid'] FROM store_data",
            "SELECT raw:\"zip code\", raw:\"Zip Code\", raw:['fb:testid'] FROM store_data");

        // Dot notation
        Databricks().VerifiedOnlySelect("SELECT raw:store.bicycle FROM store_data");

        // String-key bracket notation after a dot segment
        Databricks()
            .VerifiedOnlySelect("SELECT raw:store['bicycle'], raw:store['BICYCLE'] FROM store_data");

        // Integer-index bracket notation
        Databricks()
            .VerifiedOnlySelect("SELECT raw:store.fruit[0], raw:store.fruit[1] FROM store_data");

        // Wildcard [*] — including chained and mixed positions
        Databricks().VerifiedOnlySelect(
            "SELECT raw:store.basket[*], raw:store.basket[*][0] AS first_of_baskets, "
            + "raw:store.basket[0][*] AS first_basket, raw:store.basket[*][*] AS all_elements_flattened, "
            + "raw:store.basket[0][2].b AS subfield FROM store_data");

        // Dot access following a wildcard bracket
        Databricks().VerifiedOnlySelect("SELECT raw:store.book[*].isbn FROM store_data");

        // Double-colon cast — type keyword normalises to upper case
        Databricks().OneStatementParsesTo(
            "SELECT raw:store.bicycle.price::double FROM store_data",
            "SELECT raw:store.bicycle.price::DOUBLE FROM store_data");
    }

    [Fact]
    public void parse_numeric_prefix_identifier()
    {
        Databricks().VerifiedStmt("SELECT * FROM catalog.schema.1st_table");

        Databricks().VerifiedStmt("SELECT * FROM a.b.1c");
    }

    [Fact]
    public void parse_cte_without_as()
    {
        DatabricksAndGeneric().OneStatementParsesTo(
            "WITH cte (SELECT 1) SELECT * FROM cte",
            "WITH cte AS (SELECT 1) SELECT * FROM cte");

        DatabricksAndGeneric().OneStatementParsesTo(
            "WITH a AS (SELECT 1), b (SELECT 2) SELECT * FROM a, b",
            "WITH a AS (SELECT 1), b AS (SELECT 2) SELECT * FROM a, b");

        DatabricksAndGeneric().OneStatementParsesTo(
            "WITH cte (col1, col2) (SELECT 1, 2) SELECT * FROM cte",
            "WITH cte (col1, col2) AS (SELECT 1, 2) SELECT * FROM cte");

        DatabricksAndGeneric().VerifiedQuery("WITH cte AS (SELECT 1) SELECT * FROM cte");

        DatabricksAndGeneric()
            .VerifiedQuery("WITH cte (col1, col2) AS (SELECT 1, 2) SELECT * FROM cte");

        Assert.Equal(
            Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => !d.SupportsCteWithoutAs)
                .ParseSqlStatements("WITH cte (SELECT 1) SELECT * FROM cte")).Error,
            new ParserError.ParserErrorValue(
                "Expected: ), found: 1"));
    }

    [Fact]
    public void test_databricks_insert_by_name()
    {
        DatabricksAndGeneric().VerifiedStmt("INSERT INTO target BY NAME SELECT 1 AS a");
        DatabricksAndGeneric().VerifiedStmt(
            "INSERT INTO TABLE lakehouse.dwd.dwd_event_quality_sla_metric_di BY NAME WITH day AS (SELECT 1 AS event_data_id) SELECT event_data_id FROM day");
    }

    [Fact]
    public void parse_databricks_query_entry_points()
    {
        Databricks().VerifiedStmt("CREATE TABLE t (attrs MAP<STRING, ARRAY<INT>>)");
        Databricks().OneStatementParsesTo(@"SELECT 'it\'s'", "SELECT 'it''s'");
        Databricks().VerifiedStmt("SELECT * REPLACE (upper(name) AS name) FROM source");
        Databricks().VerifiedStmt(
            "CREATE VIEW cross_product AS FROM main.raw.left_table, main.raw.right_table");
        Databricks()
            .VerifiedStmt("CREATE VIEW filtered AS FROM main.raw.source |> WHERE id > 0 |> SELECT id");
    }

    [Fact]
    public void parse_databricks_collated_data_types()
    {
        Databricks().VerifiedStmt(
            "CREATE TABLE t (name STRING COLLATE UTF8_BINARY, values ARRAY<STRING COLLATE UTF8_LCASE>, attrs MAP<STRING COLLATE UTF8_BINARY, STRING COLLATE UTF8_LCASE>)");
        Databricks().VerifiedStmt("CREATE TABLE t (s STRUCT<a STRING COLLATE UTF8_LCASE>)");
        Databricks().VerifiedStmt("CREATE TABLE t (c ARRAY<ARRAY<STRING>> COLLATE UTF8_LCASE)");
        Assert.Equal(
            Assert.Throws<ParserException>(() => new TestedDialects(new List<Dialect> { new GenericDialect() })
                .ParseSqlStatements("CREATE TABLE t (c ARRAY<STRING COLLATE UTF8_LCASE>)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: >, found: COLLATE"));
    }
}
