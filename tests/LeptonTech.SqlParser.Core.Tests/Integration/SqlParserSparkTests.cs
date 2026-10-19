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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_spark.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

/// <summary>Test SQL syntax specific to Apache Spark SQL.</summary>
public class SqlParserSparkTests
{
    private static TestedDialects Spark() =>
        new TestedDialects(new List<Dialect> { new SparkSqlDialect() });

    // --------------------------------
    // CREATE TABLE USING
    // --------------------------------

    [Fact]
    public void test_create_table_using()
    {
        var stmt = Spark().VerifiedStmt("CREATE TABLE t (i INT, s STRING) USING parquet");
        switch (stmt)
        {
            case Statement.CreateTable { Value: var ct }:
                // Literal como `expected` (o analisador xUnit2000 é erro com TreatWarningsAsErrors).
                Assert.Equal("t", ct.Name.ToString());
                Assert.Equal(2, ct.Columns.Count);
                Assert.Equal(
                    (ct.HiveFormats ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value")).Storage,
                    new HiveIOFormat.Using(
                        Format: new Ident("parquet")));
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void test_create_table_using_if_not_exists()
    {
        Spark().VerifiedStmt("CREATE TABLE IF NOT EXISTS t (i INT) USING delta");
    }

    [Fact]
    public void test_create_table_using_with_location()
    {
        Spark().VerifiedStmt("CREATE TABLE t (i INT) USING parquet LOCATION '/data/t'");
    }

    [Fact]
    public void test_create_table_multi_column()
    {
        Spark().VerifiedStmt(
            "CREATE TABLE t (i INT, l BIGINT, f FLOAT, d DOUBLE, s STRING, b BOOLEAN) USING parquet");
    }

    [Fact]
    public void test_create_table_long_type()
    {
        // LONG is an alias for BIGINT; round-trips as BIGINT
        Spark().OneStatementParsesTo(
            "CREATE TABLE t (id LONG, val LONG) USING parquet",
            "CREATE TABLE t (id BIGINT, val BIGINT) USING parquet");
    }

    [Fact]
    public void test_create_table_array_type()
    {
        Spark().VerifiedStmt("CREATE TABLE t (arr ARRAY<INT>) USING parquet");
    }

    [Fact]
    public void test_create_table_map_type()
    {
        // MAP<K, V> parses and stores as DataType::Map (which displays as Map(K, V))
        Spark()
            .ParseSqlStatements("CREATE TABLE t (m MAP<STRING, INT>) USING parquet");
    }

    [Fact]
    public void test_create_table_struct_type()
    {
        // STRUCT field definitions drop the colon separator on round-trip
        Spark().OneStatementParsesTo(
            "CREATE TABLE t (s STRUCT<name: STRING, age: INT, score: DOUBLE>) USING parquet",
            "CREATE TABLE t (s STRUCT<name STRING, age INT, score DOUBLE>) USING parquet");
    }

    [Fact]
    public void test_create_table_nested_types()
    {
        // Nested types parse successfully
        Spark()
            .ParseSqlStatements(
                "CREATE TABLE t (arr ARRAY<STRUCT<name: STRING, value: INT>>) USING parquet");
        Spark()
            .ParseSqlStatements("CREATE TABLE t (m MAP<STRING, INT>, arr ARRAY<INT>) USING parquet");
    }

    [Fact]
    public void test_create_table_decimal_type()
    {
        Spark()
            .VerifiedStmt("CREATE TABLE t (grp STRING, d DECIMAL(10,2), flag BOOLEAN) USING parquet");
    }

    // --------------------------------
    // INSERT INTO
    // --------------------------------

    [Fact]
    public void test_insert_values()
    {
        Spark().VerifiedStmt(
            "INSERT INTO t VALUES (1, 'a'), (2, 'b'), (3, 'c'), (NULL, 'd'), (1, NULL), (NULL, NULL)");
    }

    [Fact]
    public void test_insert_values_multiline()
    {
        // Multi-line whitespace is normalized to single-line on round-trip
        Spark().OneStatementParsesTo(
            "INSERT INTO t VALUES\n  (1, 10, 'a'),\n  (2, 20, 'a'),\n  (3, 30, 'b')",
            "INSERT INTO t VALUES (1, 10, 'a'), (2, 20, 'a'), (3, 30, 'b')");
    }

    // --------------------------------
    // Lambda expressions
    // --------------------------------

    [Fact]
    public void test_lambda_single_param()
    {
        Spark().VerifiedStmt("SELECT filter(arr, x -> x > 2) FROM t");
    }

    [Fact]
    public void test_lambda_two_params()
    {
        Spark().VerifiedStmt("SELECT filter(arr, (x, i) -> i > 0) FROM t");
    }

    [Fact]
    public void test_lambda_transform()
    {
        Spark().VerifiedStmt("SELECT transform(arr, x -> x * 2) FROM t");
    }

    // --------------------------------
    // DIV integer division
    // --------------------------------

    [Fact]
    public void test_div_operator()
    {
        Spark().OneStatementParsesTo("SELECT c1 div c2 FROM t", "SELECT c1 DIV c2 FROM t");
    }

    [Fact]
    public void test_div_literal()
    {
        Spark().OneStatementParsesTo("SELECT 10 div 3", "SELECT 10 DIV 3");
    }

    [Fact]
    public void test_div_precedence()
    {
        Func<Expr, Expr, Expr> div = (left, right) => new Expr.BinaryOp(
            Left: left,
            Op: new BinaryOperator.MyIntegerDivide(),
            Right: right);
        Func<string, Expr> num = n => Expr.MakeValue(TestUtils.Number(n));

        // `DIV` shares the precedence of `*` and `/`, so `+` must end up at the root.
        Assert.Equal(
            new Expr.BinaryOp(
                Left: div(num("7"), num("2")),
                Op: new BinaryOperator.Plus(),
                Right: num("1")),
            Spark().VerifiedExpr("7 DIV 2 + 1"));

        // Equal precedence resolves left-associatively.
        Assert.Equal(
            new Expr.BinaryOp(
                Left: div(num("9"), num("3")),
                Op: new BinaryOperator.Multiply(),
                Right: num("3")),
            Spark().VerifiedExpr("9 DIV 3 * 3"));
    }

    // --------------------------------
    // Struct support
    // --------------------------------

    [Fact]
    public void test_named_struct()
    {
        Spark().VerifiedStmt("SELECT named_struct('x', a, 'y', b, 'z', c) FROM t");
    }

    [Fact]
    public void test_struct_function()
    {
        // Parses as a STRUCT literal; round-trips with uppercase STRUCT keyword
        Spark().OneStatementParsesTo(
            "SELECT struct(a, b, c) FROM t",
            "SELECT STRUCT(a, b, c) FROM t");
    }

    // --------------------------------
    // Aggregate FILTER
    // --------------------------------

    [Fact]
    public void test_aggregate_filter()
    {
        Spark().VerifiedStmt(
            "SELECT COUNT(*) FILTER (WHERE i > 0), SUM(val) FILTER (WHERE val IS NOT NULL) FROM t");
    }

    [Fact]
    public void test_aggregate_filter_with_group_by()
    {
        Spark().VerifiedStmt(
            "SELECT grp, SUM(i) FILTER (WHERE flag = true) FROM t GROUP BY grp ORDER BY grp");
    }

    // --------------------------------
    // Window functions with IGNORE NULLS
    // --------------------------------

    [Fact]
    public void test_lag_ignore_nulls()
    {
        Spark().VerifiedStmt("SELECT LAG(val) IGNORE NULLS OVER (ORDER BY id) AS lag_val FROM t");
    }

    [Fact]
    public void test_lead_ignore_nulls()
    {
        Spark().VerifiedStmt(
            "SELECT LEAD(val) IGNORE NULLS OVER (PARTITION BY grp ORDER BY id) AS lead_val FROM t");
    }

    [Fact]
    public void test_lag_with_offset_and_default()
    {
        Spark().VerifiedStmt("SELECT LAG(val, 2, -1) OVER (ORDER BY id) AS lag_val FROM t");
    }

    // --------------------------------
    // CASE WHEN
    // --------------------------------

    [Fact]
    public void test_case_when()
    {
        Spark().VerifiedStmt(
            "SELECT CASE WHEN i = 1 THEN 'one' WHEN i = 2 THEN 'two' ELSE 'other' END FROM t");
    }

    [Fact]
    public void test_case_value()
    {
        Spark().VerifiedStmt("SELECT CASE i WHEN 1 THEN 'one' WHEN 2 THEN 'two' END FROM t");
    }

    // --------------------------------
    // CAST expressions
    // --------------------------------

    [Fact]
    public void test_cast_basic_types()
    {
        // cast() lower-case round-trips as CAST() upper-case
        Spark().OneStatementParsesTo(
            "SELECT cast(i AS BIGINT), cast(i AS DOUBLE), cast(i AS STRING) FROM t",
            "SELECT CAST(i AS BIGINT), CAST(i AS DOUBLE), CAST(i AS STRING) FROM t");
    }

    [Fact]
    public void test_cast_to_timestamp()
    {
        Spark().OneStatementParsesTo(
            "SELECT cast('2020-01-01' AS TIMESTAMP)",
            "SELECT CAST('2020-01-01' AS TIMESTAMP)");
        Spark().OneStatementParsesTo(
            "SELECT cast('2020-01-01T12:34:56' AS TIMESTAMP)",
            "SELECT CAST('2020-01-01T12:34:56' AS TIMESTAMP)");
    }

    [Fact]
    public void test_cast_special_float_values()
    {
        Spark().OneStatementParsesTo(
            "SELECT cast('NaN' AS FLOAT), cast('Infinity' AS DOUBLE)",
            "SELECT CAST('NaN' AS FLOAT), CAST('Infinity' AS DOUBLE)");
    }

    // --------------------------------
    // Aggregate functions
    // --------------------------------

    [Fact]
    public void test_count_aggregate()
    {
        Spark().VerifiedStmt("SELECT count(*), count(i), count(s) FROM t");
        Spark().VerifiedStmt("SELECT grp, count(*), count(i) FROM t GROUP BY grp ORDER BY grp");
    }

    [Fact]
    public void test_sum_avg()
    {
        Spark().VerifiedStmt("SELECT avg(i), avg(l), avg(f), avg(d) FROM t");
    }

    [Fact]
    public void test_bit_aggregates()
    {
        Spark().VerifiedStmt("SELECT bit_and(i), bit_or(i), bit_xor(i) FROM t");
    }

    // --------------------------------
    // Arithmetic
    // --------------------------------

    [Fact]
    public void test_arithmetic_operators()
    {
        Spark().VerifiedStmt("SELECT a + b, a - b, a * b, a / b, a % b FROM t");
    }

    [Fact]
    public void test_unary_negative()
    {
        Spark().VerifiedStmt("SELECT negative(col1), -(col1) FROM t");
    }

    // --------------------------------
    // String operations
    // --------------------------------

    [Fact]
    public void test_like_pattern()
    {
        Spark().VerifiedStmt("SELECT s FROM t WHERE s LIKE 'foo%'");
    }

    [Fact]
    public void test_substring()
    {
        Spark().OneStatementParsesTo(
            "SELECT substring(s, 1, 3) FROM t",
            "SELECT SUBSTRING(s, 1, 3) FROM t");
    }

    [Fact]
    public void test_pipe_operator()
    {
        Spark().VerifiedStmt("SELECT * FROM t |> WHERE x > 1 |> SELECT x AS y |> ORDER BY y");
    }
}
