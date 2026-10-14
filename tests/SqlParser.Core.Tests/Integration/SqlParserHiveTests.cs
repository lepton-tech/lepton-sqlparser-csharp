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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_hive.rs.
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

/// <summary>
/// Test SQL syntax specific to Hive. The parser based on the generic dialect
/// is also tested (on the inputs it can handle).
/// </summary>
/// <remarks>
/// Notas da tradução (onda-c/C16.md): <c>assert_eq!(a, b)</c> mantém a ordem do Rust, exceto quando
/// um dos lados é literal (regra xUnit2000); <c>panic!</c>/<c>unreachable!</c> num <c>match</c> →
/// <c>default: throw new InvalidOperationException(..)</c>; <c>println!("{}", x)</c> →
/// <c>Console.WriteLine(x)</c>. As funções <c>hive()</c> e <c>hive_and_generic()</c> ficam no fim da
/// classe, como no Rust.
/// </remarks>
public class SqlParserHiveTests
{
    [Fact]
    public void parse_table_create()
    {
        var sql = "CREATE TABLE IF NOT EXISTS db.table (a BIGINT, b STRING, c TIMESTAMP) PARTITIONED BY (d STRING, e TIMESTAMP) STORED AS ORC LOCATION 's3://...' TBLPROPERTIES (\"prop\" = \"2\", \"asdf\" = '1234', 'asdf' = \"1234\", \"asdf\" = 2)";
        var iof = "CREATE TABLE IF NOT EXISTS db.table (a BIGINT, b STRING, c TIMESTAMP) PARTITIONED BY (d STRING, e TIMESTAMP) STORED AS INPUTFORMAT 'org.apache.hadoop.hive.ql.io.orc.OrcInputFormat' OUTPUTFORMAT 'org.apache.hadoop.hive.ql.io.orc.OrcOutputFormat' LOCATION 's3://...'";
        var serdeproperties = "CREATE EXTERNAL TABLE IF NOT EXISTS db.table (a STRING, b STRING, c STRING) PARTITIONED BY (d STRING, e STRING) ROW FORMAT SERDE 'org.apache.hadoop.hive.serde.config' WITH SERDEPROPERTIES ('prop_a' = 'a', 'prop_b' = 'b') STORED AS TEXTFILE LOCATION 's3://...' TBLPROPERTIES ('prop_c' = 'c')";
        var externaltable = "CREATE EXTERNAL TABLE t (c INT)";

        Hive().VerifiedStmt(sql);
        Hive().VerifiedStmt(iof);
        Hive().VerifiedStmt(serdeproperties);
        Hive().VerifiedStmt(externaltable);
    }

    [Fact]
    public void parse_describe()
    {
        HiveAndGeneric().VerifiedStmt("DESCRIBE namespace.`table`");
        HiveAndGeneric().VerifiedStmt("DESCRIBE namespace.table");
        HiveAndGeneric().VerifiedStmt("DESCRIBE table");
    }

    [Fact]
    public void explain_describe_formatted()
    {
        Hive().VerifiedStmt("DESCRIBE FORMATTED test.table");
    }

    [Fact]
    public void explain_describe_extended()
    {
        Hive().VerifiedStmt("DESCRIBE EXTENDED test.table");
    }

    [Fact]
    public void parse_insert_overwrite()
    {
        var insertPartitions = "INSERT OVERWRITE TABLE db.new_table PARTITION (a = '1', b) SELECT a, b, c FROM db.table";
        Hive().VerifiedStmt(insertPartitions);
    }

    [Fact]
    public void test_truncate()
    {
        var truncate = "TRUNCATE TABLE db.table";
        Hive().VerifiedStmt(truncate);
    }

    [Fact]
    public void parse_analyze()
    {
        var analyze = "ANALYZE TABLE db.table_name PARTITION (a = '1234', b) COMPUTE STATISTICS NOSCAN CACHE METADATA";
        Hive().VerifiedStmt(analyze);
    }

    [Fact]
    public void parse_analyze_for_columns()
    {
        var analyze =
            "ANALYZE TABLE db.table_name PARTITION (a = '1234', b) COMPUTE STATISTICS FOR COLUMNS";
        Hive().VerifiedStmt(analyze);
    }

    [Fact]
    public void parse_msck()
    {
        var msck = "MSCK REPAIR TABLE db.table_name ADD PARTITIONS";
        var msck2 = "MSCK REPAIR TABLE db.table_name";
        Hive().VerifiedStmt(msck);
        Hive().VerifiedStmt(msck2);
    }

    [Fact]
    public void parse_set_hivevar()
    {
        var set = "SET HIVEVAR:name = a, b, c_d";
        Hive().VerifiedStmt(set);
    }

    [Fact]
    public void test_spaceship()
    {
        var spaceship = "SELECT * FROM db.table WHERE a <=> b";
        Hive().VerifiedStmt(spaceship);
    }

    [Fact]
    public void parse_with_cte()
    {
        var with = "WITH a AS (SELECT * FROM b) INSERT INTO TABLE db.table_table PARTITION (a) SELECT * FROM b";
        Hive().VerifiedStmt(with);
    }

    [Fact]
    public void drop_table_purge()
    {
        var purge = "DROP TABLE db.table_name PURGE";
        Hive().VerifiedStmt(purge);
    }

    [Fact]
    public void create_table_like()
    {
        var like = "CREATE TABLE db.table_name LIKE db.other_table";
        Hive().VerifiedStmt(like);
    }

    [Fact]
    public void create_table_with_comment()
    {
        var sql = "CREATE TABLE db.table_name (a INT, b STRING)"
            + " COMMENT 'table comment'"
            + " PARTITIONED BY (a INT, b STRING)"
            + " CLUSTERED BY (a, b) SORTED BY (a ASC, b DESC)"
            + " INTO 4 BUCKETS";
        switch (Hive().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(
                    createTable.Comment,
                    new CommentDef.WithoutEq("table comment"));
                break;
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }

        // negative test case
        var invalidSql = "CREATE TABLE db.table_name (a INT, b STRING)"
            + " PARTITIONED BY (a INT, b STRING)"
            + " COMMENT 'table comment'";
        var ex = Assert.Throws<ParserException>(() => Hive().ParseSqlStatements(invalidSql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: end of statement, found: COMMENT"));
    }

    [Fact]
    public void create_table_with_clustered_by()
    {
        var sql = "CREATE TABLE db.table_name (a INT, b STRING)"
            + " PARTITIONED BY (a INT, b STRING)"
            + " CLUSTERED BY (a, b) SORTED BY (a ASC, b DESC)"
            + " INTO 4 BUCKETS";
        switch (HiveAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(
                    createTable.ClusteredBy!,
                    new ClusteredBy(
                        Columns: Sequence.Of(new Ident("a"), new Ident("b")),
                        SortedBy: Sequence.Of(
                            new OrderByExpr(
                                Expr: new Expr.Identifier(new Ident("a")),
                                Options: new OrderByOptions(
                                    Sort: new OrderBySort.Asc(),
                                    NullsFirst: null),
                                WithFill: null),
                            new OrderByExpr(
                                Expr: new Expr.Identifier(new Ident("b")),
                                Options: new OrderByOptions(
                                    Sort: new OrderBySort.Desc(),
                                    NullsFirst: null),
                                WithFill: null)),
                        NumBuckets: new Value.Number("4", false)));
                break;
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }

        // SORTED BY is optional
        HiveAndGeneric().VerifiedStmt("CREATE TABLE db.table_name (a INT, b STRING) PARTITIONED BY (a INT, b STRING) CLUSTERED BY (a, b) INTO 4 BUCKETS");

        // missing INTO BUCKETS
        var ex = Assert.Throws<ParserException>(() => HiveAndGeneric().ParseSqlStatements(
            "CREATE TABLE db.table_name (a INT, b STRING) PARTITIONED BY (a INT, b STRING) CLUSTERED BY (a, b)"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: INTO, found: EOF"));
        // missing CLUSTER BY columns
        ex = Assert.Throws<ParserException>(() => HiveAndGeneric().ParseSqlStatements(
            "CREATE TABLE db.table_name (a INT, b STRING) PARTITIONED BY (a INT, b STRING) CLUSTERED BY () INTO 4 BUCKETS"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: identifier, found: )"));
        // missing SORT BY columns
        ex = Assert.Throws<ParserException>(() => HiveAndGeneric().ParseSqlStatements(
            "CREATE TABLE db.table_name (a INT, b STRING) PARTITIONED BY (a INT, b STRING) CLUSTERED BY (a, b) SORTED BY INTO 4 BUCKETS"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: (, found: INTO"));
        // missing number BUCKETS
        ex = Assert.Throws<ParserException>(() => HiveAndGeneric().ParseSqlStatements(
            "CREATE TABLE db.table_name (a INT, b STRING) PARTITIONED BY (a INT, b STRING) CLUSTERED BY (a, b) SORTED BY (a ASC, b DESC) INTO"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: a value, found: EOF"));
    }

    // Turning off this test until we can parse identifiers starting with numbers :(
    [Fact]
    public void test_identifier()
    {
        var between = "SELECT a AS 3_barrr_asdf FROM db.table_name";
        Hive().VerifiedStmt(between);
    }

    [Fact]
    public void test_alter_partition()
    {
        var alter = "ALTER TABLE db.table PARTITION (a = 2) RENAME TO PARTITION (a = 1)";
        Hive().VerifiedStmt(alter);
    }

    [Fact]
    public void test_alter_with_location()
    {
        var alter =
            "ALTER TABLE db.table PARTITION (a = 2) RENAME TO PARTITION (a = 1) LOCATION 's3://...'";
        Hive().VerifiedStmt(alter);
    }

    [Fact]
    public void test_alter_with_set_location()
    {
        var alter = "ALTER TABLE db.table PARTITION (a = 2) RENAME TO PARTITION (a = 1) SET LOCATION 's3://...'";
        Hive().VerifiedStmt(alter);
    }

    [Fact]
    public void test_add_partition()
    {
        var add = "ALTER TABLE db.table ADD IF NOT EXISTS PARTITION (a = 'asdf', b = 2)";
        Hive().VerifiedStmt(add);
    }

    [Fact]
    public void test_add_multiple_partitions()
    {
        var add = "ALTER TABLE db.table ADD IF NOT EXISTS PARTITION (`a` = 'asdf', `b` = 2) PARTITION (`a` = 'asdh', `b` = 3)";
        Hive().VerifiedStmt(add);
    }

    [Fact]
    public void test_drop_partition()
    {
        var drop = "ALTER TABLE db.table DROP PARTITION (a = 1)";
        Hive().VerifiedStmt(drop);
    }

    [Fact]
    public void test_drop_if_exists()
    {
        var drop = "ALTER TABLE db.table DROP IF EXISTS PARTITION (a = 'b', c = 'd')";
        Hive().VerifiedStmt(drop);
    }

    [Fact]
    public void test_cluster_by()
    {
        var cluster = "SELECT a FROM db.table CLUSTER BY a, b";
        Hive().VerifiedStmt(cluster);
    }

    [Fact]
    public void test_distribute_by()
    {
        var cluster = "SELECT a FROM db.table DISTRIBUTE BY a, b";
        Hive().VerifiedStmt(cluster);
    }

    [Fact]
    public void no_join_condition()
    {
        var join = "SELECT a, b FROM db.table_name JOIN a";
        Hive().VerifiedStmt(join);
    }

    [Fact]
    public void columns_after_partition()
    {
        var query = "INSERT INTO db.table_name PARTITION (a, b) (c, d) SELECT a, b, c, d FROM db.table";
        Hive().VerifiedStmt(query);
    }

    [Fact]
    public void long_numerics()
    {
        var query = "SELECT MIN(MIN(10, 5), 1L) AS a";
        Hive().VerifiedStmt(query);
    }

    [Fact]
    public void decimal_precision()
    {
        var query = "SELECT CAST(a AS DECIMAL(18,2)) FROM db.table";
        Hive().VerifiedStmt(query);
    }

    [Fact]
    public void create_temp_table()
    {
        var query = "CREATE TEMPORARY TABLE db.table (a INT NOT NULL)";
        var query2 = "CREATE TEMP TABLE db.table (a INT NOT NULL)";

        Hive().VerifiedStmt(query);
        Hive().OneStatementParsesTo(query2, query);
    }

    [Fact]
    public void create_delimited_table()
    {
        // Mesmos escapes do Rust: `\t` é um TAB literal no SQL e `\"` é uma aspa dupla.
        var query = "CREATE TABLE tab (cola STRING, colb BIGINT) ROW FORMAT DELIMITED FIELDS TERMINATED BY '\t' ESCAPED BY '\"' MAP KEYS TERMINATED BY '\"'";
        Hive().VerifiedStmt(query);
    }

    [Fact]
    public void create_local_directory()
    {
        var query =
            "INSERT OVERWRITE LOCAL DIRECTORY '/home/blah' STORED AS TEXTFILE SELECT * FROM db.table";
        Hive().VerifiedStmt(query);
    }

    [Fact]
    public void lateral_view()
    {
        var view = "SELECT a FROM db.table LATERAL VIEW explode(a) t AS j, P LATERAL VIEW OUTER explode(a) t AS a, b WHERE a = 1";
        Hive().VerifiedStmt(view);
    }

    [Fact]
    public void sort_by()
    {
        var sortBy = "SELECT * FROM db.table SORT BY a";
        Hive().VerifiedStmt(sortBy);

        var sortByWithDirection = "SELECT * FROM db.table SORT BY a, b DESC";
        Hive().VerifiedStmt(sortByWithDirection);
    }

    [Fact]
    public void rename_table()
    {
        var rename = "ALTER TABLE db.table_name RENAME TO db.table_2";
        Hive().VerifiedStmt(rename);
    }

    [Fact]
    public void map_access()
    {
        var rename = "SELECT a.b[\"asdf\"] FROM db.table WHERE a = 2";
        Hive().VerifiedStmt(rename);
    }

    [Fact]
    public void from_cte()
    {
        var rename =
            "WITH cte AS (SELECT * FROM a.b) FROM cte INSERT INTO TABLE a.b PARTITION (a) SELECT *";
        Console.WriteLine(Hive().VerifiedStmt(rename));
    }

    [Fact]
    public void set_statement_with_minus()
    {
        Assert.Equal(
            Hive().VerifiedStmt("SET hive.tez.java.opts = -Xmx4g"),
            new Statement.Set(new Set.SingleAssignment(
                Scope: null,
                Hivevar: false,
                Variable: ObjectName.From(Sequence.Of(
                    new Ident("hive"),
                    new Ident("tez"),
                    new Ident("java"),
                    new Ident("opts"))),
                Values: Sequence.Of<Expr>(new Expr.UnaryOp(
                    Op: new UnaryOperator.Minus(),
                    Expr: new Expr.Identifier(new Ident("Xmx4g")))))));

        var ex = Assert.Throws<ParserException>(() => Hive().ParseSqlStatements("SET hive.tez.java.opts = -"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: variable value, found: EOF"));
    }

    [Fact]
    public void parse_create_function()
    {
        var sql = "CREATE TEMPORARY FUNCTION mydb.myfunc AS 'org.random.class.Name' USING JAR 'hdfs://somewhere.com:8020/very/far'";
        switch (Hive().VerifiedStmt(sql))
        {
            case Statement.CreateFunction { Value: var createFunction }:
            {
                var temporary = createFunction.Temporary;
                var name = createFunction.Name;
                var functionBody = createFunction.FunctionBody;
                var @using = createFunction.Using;
                Assert.True(temporary);
                Assert.Equal("mydb.myfunc", name.ToString());
                Assert.Equal(
                    functionBody,
                    new CreateFunctionBody.AsBeforeOptions(
                        Body: new Expr.Value(
                            new Value.SingleQuotedString("org.random.class.Name")
                                .WithEmptySpan()),
                        LinkSymbol: null));
                Assert.Equal(
                    @using,
                    new CreateFunctionUsing.Jar(
                        "hdfs://somewhere.com:8020/very/far"));
                break;
            }
            default:
                throw new InvalidOperationException("internal error: entered unreachable code");
        }

        // Test error in dialect that doesn't support parsing CREATE FUNCTION
        var unsupportedDialects = new TestedDialects(new List<Dialect> { new AnsiDialect() });

        var ex = Assert.Throws<ParserException>(() => unsupportedDialects.ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: an object type after CREATE, found: FUNCTION"));

        sql = "CREATE TEMPORARY FUNCTION mydb.myfunc AS 'org.random.class.Name' USING JAR";
        ex = Assert.Throws<ParserException>(() => Hive().ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: literal string, found: EOF"));
    }

    [Fact]
    public void filter_as_alias()
    {
        var sql = "SELECT name filter FROM region";
        var expected = "SELECT name AS filter FROM region";
        Console.WriteLine(Hive().OneStatementParsesTo(sql, expected));
    }

    [Fact]
    public void parse_delimited_identifiers()
    {
        // check that quoted identifiers in any position remain quoted after serialization
        var select = Hive().VerifiedOnlySelect(
            "SELECT \"alias\".\"bar baz\", \"myfun\"(), \"simple id\" AS \"column alias\" FROM \"a table\" AS \"alias\"");
        // check FROM
        switch (TestUtils.Only(select.From).Relation)
        {
            case TableFactor.Table table:
            {
                // TableFactor::Table { name, alias, args, with_hints, version, with_ordinality: _,
                // partitions: _, json_path: _, sample: _, index_hints: _ }
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
                throw new InvalidOperationException("Expected: ExprWithAlias");
        }

        Hive().VerifiedStmt("CREATE TABLE \"foo\" (\"bar\" \"int\")");
        Hive().VerifiedStmt("ALTER TABLE foo ADD CONSTRAINT \"bar\" PRIMARY KEY (baz)");
        //TODO verified_stmt(r#"UPDATE foo SET "bar" = 5"#);
    }

    [Fact]
    public void parse_use()
    {
        var validObjectNames = new[] { "mydb", "SCHEMA", "DATABASE", "CATALOG", "WAREHOUSE" };
        var quoteStyles = new[] { '\'', '"', '`' };
        foreach (var objectName in validObjectNames)
        {
            // Test single identifier without quotes
            Assert.Equal(
                Hive().VerifiedStmt($"USE {objectName}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(new Ident(
                    objectName))))));
            foreach (var quote in quoteStyles)
            {
                // Test single identifier with different type of quotes
                Assert.Equal(
                    Hive().VerifiedStmt($"USE {quote}{objectName}{quote}"),
                    new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(Ident.WithQuote(
                        quote,
                        objectName))))));
            }
        }
        // Test DEFAULT keyword that is special case in Hive
        Assert.Equal(
            Hive().VerifiedStmt("USE DEFAULT"),
            new Statement.Use(new Use.Default()));
    }

    [Fact]
    public void test_tample_sample()
    {
        Hive().VerifiedStmt("SELECT * FROM source TABLESAMPLE (BUCKET 3 OUT OF 32 ON rand()) AS s");
        Hive().VerifiedStmt("SELECT * FROM source TABLESAMPLE (BUCKET 3 OUT OF 16 ON id)");
        Hive().VerifiedStmt("SELECT * FROM source TABLESAMPLE (100M) AS s");
        Hive().VerifiedStmt("SELECT * FROM source TABLESAMPLE (0.1 PERCENT) AS s");
        Hive().VerifiedStmt("SELECT * FROM source TABLESAMPLE (10 ROWS)");
    }

    [Fact]
    public void parse_create_table_with_map_column_comment()
    {
        Hive().VerifiedStmt(
            "CREATE TABLE target (kv_map MAP<STRING, STRING> COMMENT 'kv col comment') COMMENT 'this is table comment'");
    }

    private static TestedDialects Hive()
    {
        return new TestedDialects(new List<Dialect> { new HiveDialect() });
    }

    private static TestedDialects HiveAndGeneric()
    {
        return new TestedDialects(new List<Dialect> { new HiveDialect(), new GenericDialect() });
    }
}
