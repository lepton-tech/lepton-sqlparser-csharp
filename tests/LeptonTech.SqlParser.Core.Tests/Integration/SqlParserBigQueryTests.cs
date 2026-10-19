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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_bigquery.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C14.md):
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests e SpansTests).
// - `assert_eq!(1, v.len())` usa `Assert.Equal(1, v.Count)`, como no Rust; o xUnit2013 (que sugere
//   Assert.Single) fica suprimido neste arquivo, como em SpansTests.
// - `match`/`let .. else` com `unreachable!()`/`panic!(..)` → `switch`/`is not` com
//   `throw new InvalidOperationException(..)` (convencoes.md §8).
// - Strings `r#"..."#` viram raw string literals `"""..."""` quando o conteúdo não começa nem termina
//   com aspas; nos demais casos, strings comuns com `\"` e `\\`.
#pragma warning disable xUnit2013
/// <summary>Rust: <c>tests/sqlparser_bigquery.rs</c>.</summary>
public class SqlParserBigQueryTests
{
    [Fact]
    public void parse_literal_string()
    {
        var sql = string.Concat(
            "SELECT ",                                   // line 1, column 1
            "'single', ",                                // line 1, column 7
            "\"double\", ",                              // line 1, column 14
            "'''triple-single''', ",                     // line 1, column 22
            "\"\"\"triple-double\"\"\", ",               // line 1, column 33
            "'single\\'escaped', ",                      // line 1, column 43
            "'''triple-single\\'escaped''', ",           // line 1, column 55
            "'''triple-single'unescaped''', ",           // line 1, column 68
            "\"double\\\"escaped\", ",                   // line 1, column 83
            "\"\"\"triple-double\\\"escaped\"\"\", ",    // line 1, column 92
            "\"\"\"triple-double\"unescaped\"\"\", ",    // line 1, column 105
            "\"\"\"triple-double'unescaped\"\"\", ",     // line 1, column 118
            "'''triple-single\"unescaped'''"             // line 1, column 131
        );
        var dialect = TestedDialects.NewWithOptions(
            new List<Dialect> { new BigQueryDialect() },
            new ParserOptions().WithUnescape(false));
        var select = dialect.VerifiedOnlySelect(sql);
        Assert.Equal(12, select.Projection.Count);
        Assert.Equal(
            new Expr.Value(new Value.SingleQuotedString("single").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Value(new Value.DoubleQuotedString("double").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Value(new Value.TripleSingleQuotedString("triple-single").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[2]));
        Assert.Equal(
            new Expr.Value(new Value.TripleDoubleQuotedString("triple-double").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[3]));
        Assert.Equal(
            new Expr.Value(new Value.SingleQuotedString(@"single\'escaped").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[4]));
        Assert.Equal(
            new Expr.Value(
                new Value.TripleSingleQuotedString(@"triple-single\'escaped").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[5]));
        Assert.Equal(
            new Expr.Value(
                new Value.TripleSingleQuotedString("triple-single'unescaped").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[6]));
        Assert.Equal(
            new Expr.Value(new Value.DoubleQuotedString("double\\\"escaped").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[7]));
        Assert.Equal(
            new Expr.Value(
                new Value.TripleDoubleQuotedString("triple-double\\\"escaped")
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[8]));
        Assert.Equal(
            new Expr.Value(
                new Value.TripleDoubleQuotedString("triple-double\"unescaped")
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[9]));
        Assert.Equal(
            new Expr.Value(
                new Value.TripleDoubleQuotedString("triple-double'unescaped")
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[10]));
        Assert.Equal(
            new Expr.Value(
                new Value.TripleSingleQuotedString("triple-single\"unescaped")
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[11]));
    }

    [Fact]
    public void parse_byte_literal()
    {
        var sql = string.Concat(
            "SELECT ",                          // line 1, column 1
            "B'abc', ",                         // line 1, column 8
            "B\"abc\", ",                       // line 1, column 15
            "B'f\\(abc,(.*),def\\)', ",         // line 1, column 22
            "B\"f\\(abc,(.*),def\\)\", ",       // line 1, column 42
            "B'''abc''', ",                     // line 1, column 62
            "B\"\"\"abc\"\"\""                  // line 1, column 74
        );
        var stmt = Bigquery().VerifiedStmt(sql);
        if (stmt is Statement.Query query)
        {
            if (query.Value.Body is SetExpr.Select { Value: var select })
            {
                Assert.Equal(6, select.Projection.Count);
                Assert.Equal(
                    new Expr.Value(
                        new Value.SingleQuotedByteStringLiteral("abc").WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[0]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.DoubleQuotedByteStringLiteral("abc").WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[1]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.SingleQuotedByteStringLiteral(@"f\(abc,(.*),def\)")
                            .WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[2]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.DoubleQuotedByteStringLiteral(@"f\(abc,(.*),def\)")
                            .WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[3]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.TripleSingleQuotedByteStringLiteral(@"abc")
                            .WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[4]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.TripleDoubleQuotedByteStringLiteral(@"abc")
                            .WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[5]));
            }
        }
        else
        {
            throw new InvalidOperationException("invalid query");
        }

        Bigquery().OneStatementParsesTo(
            "SELECT b'123', b\"123\", b'''123''', b\"\"\"123\"\"\"",
            "SELECT B'123', B\"123\", B'''123''', B\"\"\"123\"\"\"");
    }

    [Fact]
    public void parse_raw_literal()
    {
        var sql = string.Concat(
            "SELECT ",                          // line 1, column 1
            "R'abc', ",                         // line 1, column 8
            "R\"abc\", ",                       // line 1, column 15
            "R'f\\(abc,(.*),def\\)', ",         // line 1, column 22
            "R\"f\\(abc,(.*),def\\)\", ",       // line 1, column 42
            "R'''abc''', ",                     // line 1, column 62
            "R\"\"\"abc\"\"\""                  // line 1, column 74
        );
        var stmt = Bigquery().VerifiedStmt(sql);
        if (stmt is Statement.Query query)
        {
            if (query.Value.Body is SetExpr.Select { Value: var select })
            {
                Assert.Equal(6, select.Projection.Count);
                Assert.Equal(
                    new Expr.Value(
                        new Value.SingleQuotedRawStringLiteral("abc").WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[0]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.DoubleQuotedRawStringLiteral("abc").WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[1]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.SingleQuotedRawStringLiteral(@"f\(abc,(.*),def\)")
                            .WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[2]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.DoubleQuotedRawStringLiteral(@"f\(abc,(.*),def\)")
                            .WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[3]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.TripleSingleQuotedRawStringLiteral(@"abc").WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[4]));
                Assert.Equal(
                    new Expr.Value(
                        new Value.TripleDoubleQuotedRawStringLiteral(@"abc").WithEmptySpan()),
                    TestUtils.ExprFromProjection(select.Projection[5]));
            }
        }
        else
        {
            throw new InvalidOperationException("invalid query");
        }

        Bigquery().OneStatementParsesTo(
            "SELECT r'123', r\"123\", r'''123''', r\"\"\"123\"\"\"",
            "SELECT R'123', R\"123\", R'''123''', R\"\"\"123\"\"\"");
    }

    [Fact]
    public void parse_big_query_non_reserved_column_alias()
    {
        var sql = @"SELECT OFFSET, EXPLAIN, ANALYZE, SORT, TOP, VIEW FROM T";
        Bigquery().VerifiedStmt(sql);

        sql = @"SELECT 1 AS OFFSET, 2 AS EXPLAIN, 3 AS ANALYZE FROM T";
        Bigquery().VerifiedStmt(sql);
    }

    [Fact]
    public void parse_at_at_identifier()
    {
        Bigquery().VerifiedStmt("SELECT @@error.stack_trace, @@error.message");
    }

    [Fact]
    public void parse_begin()
    {
        var sql = @"BEGIN SELECT 1; EXCEPTION WHEN ERROR THEN SELECT 2; RAISE USING MESSAGE = FORMAT('ERR: %s', 'Bad'); END";
        if (Bigquery().VerifiedStmt(sql) is not Statement.StartTransaction startTransaction)
        {
            throw new InvalidOperationException("unreachable");
        }
        var statements = startTransaction.Statements;
        var exceptionOpt = startTransaction.Exception;
        var hasEndKeyword = startTransaction.HasEndKeyword;
        Assert.Equal(1, statements.Count);
        Assert.True(exceptionOpt is not null);

        var exception = exceptionOpt!.Value;
        Assert.Equal(1, exception.Count);
        Assert.True(hasEndKeyword);

        Bigquery().VerifiedStmt(
            "BEGIN SELECT 1; SELECT 2; EXCEPTION WHEN ERROR THEN SELECT 2; SELECT 4; END");
        Bigquery()
            .VerifiedStmt("BEGIN SELECT 1; EXCEPTION WHEN ERROR THEN SELECT @@error.stack_trace; END");
        Bigquery().VerifiedStmt("BEGIN EXCEPTION WHEN ERROR THEN SELECT 2; END");
        Bigquery().VerifiedStmt("BEGIN SELECT 1; SELECT 2; EXCEPTION WHEN ERROR THEN END");
        Bigquery().VerifiedStmt("BEGIN EXCEPTION WHEN ERROR THEN END");
        Bigquery().VerifiedStmt("BEGIN SELECT 1; SELECT 2; END");
        Bigquery().VerifiedStmt("BEGIN END");

        Assert.Equal(
            Assert.Throws<ParserException>(() => Bigquery()
                .ParseSqlStatements("BEGIN SELECT 1; SELECT 2 END")).Error,
            new ParserError.ParserErrorValue("Expected: ;, found: END"));
        Assert.Equal(
            Assert.Throws<ParserException>(() => Bigquery()
                .ParseSqlStatements("BEGIN SELECT 1; EXCEPTION WHEN ERROR THEN SELECT 2 END")).Error,
            new ParserError.ParserErrorValue("Expected: ;, found: END"));
    }

    [Fact]
    public void parse_delete_statement()
    {
        var sql = "DELETE \"table\" WHERE 1";
        switch (BigqueryAndGeneric().VerifiedStmt(sql))
        {
            case Statement.Delete { Value.From: FromTable.WithoutKeyword { Value: var from } }:
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(Ident.WithQuote('"', "table")))),
                    from[0].Relation);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_create_view_with_options()
    {
        var sql = string.Concat(
            "CREATE VIEW myproject.mydataset.newview ",
            """(name, age OPTIONS(description = "field age")) """,
            @"OPTIONS(expiration_timestamp = TIMESTAMP_ADD(CURRENT_TIMESTAMP(), INTERVAL 48 HOUR), ",
            """friendly_name = "newview", description = "a view that expires in 2 days", labels = [("org_unit", "development")]) """,
            "AS SELECT column_1, column_2, column_3 FROM myproject.mydataset.mytable"
        );
        switch (Bigquery().VerifiedStmt(sql))
        {
            case Statement.CreateView { Value: var createView }:
            {
                var name = createView.Name;
                var query = createView.Query;
                var options = createView.Options;
                var columns = createView.Columns;
                Assert.Equal(
                    name,
                    ObjectName.From(Sequence.Of(
                        Ident.From("myproject"),
                        Ident.From("mydataset"),
                        Ident.From("newview"))));
                Assert.Equal(
                    Sequence.Of(
                        new ViewColumnDef(
                            Name: new Ident("name"),
                            DataType: null,
                            Options: null),
                        new ViewColumnDef(
                            Name: new Ident("age"),
                            DataType: null,
                            Options: new ColumnOptions.CommaSeparated(Sequence.Of<ColumnOption>(new ColumnOption.Options(
                                Sequence.Of<SqlOption>(new SqlOption.KeyValue(
                                    Key: new Ident("description"),
                                    Value: new Expr.Value(
                                        new Value.DoubleQuotedString("field age").WithSpan(
                                            new Span(new Location(1, 42), new Location(1, 52))))))))))),
                    columns);
                Assert.Equal(
                    "SELECT column_1, column_2, column_3 FROM myproject.mydataset.mytable",
                    query.ToString());
                Assert.Equal(
                    """OPTIONS(expiration_timestamp = TIMESTAMP_ADD(CURRENT_TIMESTAMP(), INTERVAL 48 HOUR), friendly_name = "newview", description = "a view that expires in 2 days", labels = [("org_unit", "development")])""",
                    options.ToString());
                if (options is not CreateTableOptions.Options { Value: var optionsList })
                {
                    throw new InvalidOperationException("unreachable");
                }
                Assert.Equal<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: new Ident("description"),
                        Value: new Expr.Value(
                            new Value.DoubleQuotedString("a view that expires in 2 days")
                                .WithEmptySpan())),
                    optionsList[2]);
                break;
            }
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_create_view_if_not_exists()
    {
        var sql = "CREATE VIEW IF NOT EXISTS mydataset.newview AS SELECT foo FROM bar";
        switch (Bigquery().VerifiedStmt(sql))
        {
            case Statement.CreateView { Value: var createView }:
            {
                var lateBinding = createView.WithNoSchemaBinding;
                Assert.Equal("mydataset.newview", createView.Name.ToString());
                Assert.Equal(Sequence<ViewColumnDef>.Empty, createView.Columns);
                Assert.Equal("SELECT foo FROM bar", createView.Query.ToString());
                Assert.False(createView.Materialized);
                Assert.False(createView.OrReplace);
                Assert.Equal(createView.Options, new CreateTableOptions.None());
                Assert.Equal(createView.ClusterBy, Sequence<Ident>.Empty);
                Assert.True(createView.Comment is null);
                Assert.False(lateBinding);
                Assert.True(createView.IfNotExists);
                Assert.False(createView.Temporary);
                break;
            }
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_create_view_with_unquoted_hyphen()
    {
        var sql = "CREATE VIEW IF NOT EXISTS my-pro-ject.mydataset.myview AS SELECT 1";
        switch (Bigquery().VerifiedStmt(sql))
        {
            case Statement.CreateView { Value: var createView }:
                Assert.Equal("my-pro-ject.mydataset.myview", createView.Name.ToString());
                Assert.Equal("SELECT 1", createView.Query.ToString());
                Assert.True(createView.IfNotExists);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_create_table_with_unquoted_hyphen()
    {
        var sql = "CREATE TABLE my-pro-ject.mydataset.mytable (x INT64)";
        switch (Bigquery().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(
                    createTable.Name,
                    ObjectName.From(Sequence.Of(
                        Ident.From("my-pro-ject"),
                        Ident.From("mydataset"),
                        Ident.From("mytable"))));
                Assert.Equal(
                    Sequence.Of(new ColumnDef(
                        Name: new Ident("x"),
                        DataType: new DataType.Int64(),
                        Options: Sequence<ColumnOptionDef>.Empty)),
                    createTable.Columns);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_create_table_with_options()
    {
        var sql = string.Concat(
            "CREATE TABLE mydataset.newtable ",
            """(x INT64 NOT NULL OPTIONS(description = "field x"), """,
            """y BOOL OPTIONS(description = "field y")) """,
            "PARTITION BY _PARTITIONDATE ",
            "CLUSTER BY userid, age ",
            """OPTIONS(partition_expiration_days = 1, description = "table option description")"""
        );
        switch (Bigquery().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(
                    createTable.Name,
                    ObjectName.From(Sequence.Of(Ident.From("mydataset"), Ident.From("newtable"))));
                Assert.Equal(
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("x"),
                            DataType: new DataType.Int64(),
                            Options: Sequence.Of(
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.NotNull()),
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Options(Sequence.Of<SqlOption>(new SqlOption.KeyValue(
                                        Key: new Ident("description"),
                                        Value: new Expr.Value(
                                            new Value.DoubleQuotedString("field x").WithSpan(
                                                new Span(new Location(1, 42), new Location(1, 52))))))))
                            )),
                        new ColumnDef(
                            Name: new Ident("y"),
                            DataType: new DataType.Bool(),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Options(Sequence.Of<SqlOption>(new SqlOption.KeyValue(
                                    Key: new Ident("description"),
                                    Value: new Expr.Value(
                                        new Value.DoubleQuotedString("field y").WithSpan(
                                            new Span(new Location(1, 42), new Location(1, 52))))))))))
                    ),
                    createTable.Columns);
                Assert.Equal<(Expr?, WrappedCollection<Sequence<Expr>>?, CreateTableOptions)>(
                    (
                        new Expr.Identifier(new Ident("_PARTITIONDATE")),
                        new WrappedCollection<Sequence<Expr>>.NoWrapping(Sequence.Of<Expr>(
                            new Expr.Identifier(new Ident("userid")),
                            new Expr.Identifier(new Ident("age")))),
                        new CreateTableOptions.Options(Sequence.Of<SqlOption>(
                            new SqlOption.KeyValue(
                                Key: new Ident("partition_expiration_days"),
                                Value: new Expr.Value(
                                    TestUtils.Number("1").WithSpan(new Span(
                                        new Location(1, 42),
                                        new Location(1, 43))))),
                            new SqlOption.KeyValue(
                                Key: new Ident("description"),
                                Value: new Expr.Value(
                                    new Value.DoubleQuotedString("table option description")
                                        .WithSpan(new Span(
                                            new Location(1, 42),
                                            new Location(1, 52)))))))
                    ),
                    (createTable.PartitionBy, createTable.ClusterBy, createTable.TableOptions));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        sql = string.Concat(
            "CREATE TABLE mydataset.newtable ",
            """(x INT64 NOT NULL OPTIONS(description = "field x"), """,
            """y BOOL OPTIONS(description = "field y")) """,
            "CLUSTER BY userid ",
            "OPTIONS(partition_expiration_days = 1, ",
            """description = "table option description")"""
        );
        Bigquery().VerifiedStmt(sql);

        sql = "CREATE TABLE foo (x INT64) OPTIONS()";
        Bigquery().VerifiedStmt(sql);

        sql = "CREATE TABLE db.schema.test (x INT64 OPTIONS(description = 'An optional INTEGER field')) OPTIONS()";
        Bigquery().VerifiedStmt(sql);
    }

    [Fact]
    public void parse_create_external_table_with_options()
    {
        Bigquery().VerifiedStmt(
            "CREATE EXTERNAL TABLE dataset_id.table1 (hvr_tx_seq STRING) OPTIONS(format = 'CSV')");
        Bigquery().VerifiedStmt(
            "CREATE EXTERNAL TABLE dataset_id.table1 (hvr_tx_seq STRING) OPTIONS(format = 'CSV', allow_quoted_newlines = true, encoding = 'UTF8')");
    }

    [Fact]
    public void parse_nested_data_types()
    {
        var sql = "CREATE TABLE table (x STRUCT<a ARRAY<INT64>, b BYTES(42)>, y ARRAY<STRUCT<INT64>>)";
        switch (BigqueryAndGeneric().OneStatementParsesTo(sql, sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(createTable.Name, ObjectName.From(Sequence.Of(Ident.From("table"))));
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("x"),
                            DataType: new DataType.Struct(
                                Sequence.Of(
                                    new StructField(
                                        FieldName: Ident.From("a"),
                                        FieldType: new DataType.Array(new ArrayElemTypeDef.AngleBracket(
                                            new DataType.Int64())),
                                        Options: null),
                                    new StructField(
                                        FieldName: Ident.From("b"),
                                        FieldType: new DataType.Bytes(42),
                                        Options: null)),
                                StructBracketKind.AngleBrackets),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident("y"),
                            DataType: new DataType.Array(new ArrayElemTypeDef.AngleBracket(
                                new DataType.Struct(
                                    Sequence.Of(new StructField(
                                        FieldName: null,
                                        FieldType: new DataType.Int64(),
                                        Options: null)),
                                    StructBracketKind.AngleBrackets))),
                            Options: Sequence<ColumnOptionDef>.Empty)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_tuple_struct_literal()
    {
        // tuple syntax: https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#tuple_syntax
        // syntax: (expr1, expr2 [, ... ])
        var sql = "SELECT (1, 2, 3), (1, 1.0, '123', true)"; // line 1, column 1
        var select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Tuple(Sequence.Of(
                Expr.MakeValue(TestUtils.Number("1")),
                Expr.MakeValue(TestUtils.Number("2")),
                Expr.MakeValue(TestUtils.Number("3")))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Tuple(Sequence.Of(
                Expr.MakeValue(TestUtils.Number("1")),
                Expr.MakeValue(TestUtils.Number("1.0")),
                new Expr.Value(new Value.SingleQuotedString("123").WithEmptySpan()),
                new Expr.Value(new Value.Boolean(true).WithEmptySpan()))),
            TestUtils.ExprFromProjection(select.Projection[1]));
    }

    [Fact]
    public void parse_typeless_struct_syntax()
    {
        // typeless struct syntax https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#typeless_struct_syntax
        // syntax: STRUCT( expr1 [AS field_name] [, ... ])
        var sql = "SELECT STRUCT(1, 2, 3), STRUCT('abc'), STRUCT(1, t.str_col), STRUCT(1 AS a, 'abc' AS b), STRUCT(str_col AS abc)";
        var select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(5, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(
                    Expr.MakeValue(TestUtils.Number("1")),
                    Expr.MakeValue(TestUtils.Number("2")),
                    Expr.MakeValue(TestUtils.Number("3"))),
                Fields: Sequence<StructField>.Empty),
            TestUtils.ExprFromProjection(select.Projection[0]));

        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.SingleQuotedString("abc").WithEmptySpan())),
                Fields: Sequence<StructField>.Empty),
            TestUtils.ExprFromProjection(select.Projection[1]));

        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(
                    Expr.MakeValue(TestUtils.Number("1")),
                    new Expr.CompoundIdentifier(Sequence.Of(Ident.From("t"), Ident.From("str_col")))),
                Fields: Sequence<StructField>.Empty),
            TestUtils.ExprFromProjection(select.Projection[2]));

        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(
                    new Expr.Named(
                        Expr: Expr.MakeValue(TestUtils.Number("1")),
                        Name: Ident.From("a")),
                    new Expr.Named(
                        Expr: new Expr.Value(new Value.SingleQuotedString("abc").WithEmptySpan()),
                        Name: Ident.From("b"))),
                Fields: Sequence<StructField>.Empty),
            TestUtils.ExprFromProjection(select.Projection[3]));

        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Named(
                    Expr: new Expr.Identifier(Ident.From("str_col")),
                    Name: Ident.From("abc"))),
                Fields: Sequence<StructField>.Empty),
            TestUtils.ExprFromProjection(select.Projection[4]));
    }

    [Fact]
    public void parse_typed_struct_syntax_bigquery()
    {
        // typed struct syntax https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#typed_struct_syntax
        // syntax: STRUCT<[field_name] field_type, ...>( expr1 [, ... ])

        var sql = @"SELECT STRUCT<INT64>(5), STRUCT<x INT64, y STRING>(1, t.str_col), STRUCT<arr ARRAY<FLOAT64>, str STRUCT<BOOL>>(nested_col)";
        var select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("5"))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Int64(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(
                    Expr.MakeValue(TestUtils.Number("1")),
                    new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident(
                            Value: "t",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        new Ident(
                            Value: "str_col",
                            QuoteStyle: null,
                            Span: Span.Empty())))),
                Fields: Sequence.Of(
                    new StructField(
                        FieldName: new Ident(
                            Value: "x",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        FieldType: new DataType.Int64(),
                        Options: null),
                    new StructField(
                        FieldName: new Ident(
                            Value: "y",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        FieldType: new DataType.String(null),
                        Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Identifier(new Ident(
                    Value: "nested_col",
                    QuoteStyle: null,
                    Span: Span.Empty()))),
                Fields: Sequence.Of(
                    new StructField(
                        FieldName: Ident.From("arr"),
                        FieldType: new DataType.Array(new ArrayElemTypeDef.AngleBracket(
                            new DataType.Float64())),
                        Options: null),
                    new StructField(
                        FieldName: Ident.From("str"),
                        FieldType: new DataType.Struct(
                            Sequence.Of(new StructField(
                                FieldName: null,
                                FieldType: new DataType.Bool(),
                                Options: null)),
                            StructBracketKind.AngleBrackets),
                        Options: null))),
            TestUtils.ExprFromProjection(select.Projection[2]));

        sql = @"SELECT STRUCT<x STRUCT, y ARRAY<STRUCT>>(nested_col)";
        select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Identifier(new Ident(
                    Value: "nested_col",
                    QuoteStyle: null,
                    Span: Span.Empty()))),
                Fields: Sequence.Of(
                    new StructField(
                        FieldName: Ident.From("x"),
                        FieldType: new DataType.Struct(
                            Sequence<StructField>.Empty,
                            StructBracketKind.AngleBrackets),
                        Options: null),
                    new StructField(
                        FieldName: Ident.From("y"),
                        FieldType: new DataType.Array(new ArrayElemTypeDef.AngleBracket(
                            new DataType.Struct(Sequence<StructField>.Empty, StructBracketKind.AngleBrackets))),
                        Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));

        sql = @"SELECT STRUCT<BOOL>(true), STRUCT<BYTES(42)>(B'abc')";
        select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(new Value.Boolean(true).WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Bool(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.SingleQuotedByteStringLiteral("abc").WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Bytes(42),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        sql = """SELECT STRUCT<DATE>("2011-05-05"), STRUCT<DATETIME>(DATETIME '1999-01-01 01:23:34.45'), STRUCT<FLOAT64>(5.0), STRUCT<INT64>(1)""";
        select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(4, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.DoubleQuotedString("2011-05-05").WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Date(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.Datetime(null),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("1999-01-01 01:23:34.45"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Datetime(null),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("5.0"))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Float64(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[2]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("1"))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Int64(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[3]));

        sql = """SELECT STRUCT<INTERVAL>(INTERVAL '2' HOUR), STRUCT<JSON>(JSON '{"class" : {"students" : [{"name" : "Jane"}]}}')""";
        select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Interval(new Interval(
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("2").WithEmptySpan()),
                    LeadingField: new DateTimeField.Hour(),
                    LeadingPrecision: null,
                    LastField: null,
                    FractionalSecondsPrecision: null))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Interval(
                        Fields: null,
                        Precision: null),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.JSON(),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString(
                            """{"class" : {"students" : [{"name" : "Jane"}]}}"""),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.JSON(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        sql = """SELECT STRUCT<STRING(42)>("foo"), STRUCT<TIMESTAMP>(TIMESTAMP '2008-12-25 15:30:00 America/Los_Angeles'), STRUCT<TIME>(TIME '15:30:00')""";
        select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.DoubleQuotedString("foo").WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.String(42),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString(
                            "2008-12-25 15:30:00 America/Los_Angeles"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.Time(null, new TimezoneInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("15:30:00"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Time(null, new TimezoneInfo.None()),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[2]));

        sql = @"SELECT STRUCT<NUMERIC>(NUMERIC '1'), STRUCT<BIGNUMERIC>(BIGNUMERIC '1')";
        select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.Numeric(new ExactNumberInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("1"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Numeric(new ExactNumberInfo.None()),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("1"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        // Keywords in the parser may be used as field names.
        sql = @"SELECT STRUCT<key INT64, value INT64>(1, 2)";
        select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("1")), Expr.MakeValue(TestUtils.Number("2"))),
                Fields: Sequence.Of(
                    new StructField(
                        FieldName: Ident.From("key"),
                        FieldType: new DataType.Int64(),
                        Options: null),
                    new StructField(
                        FieldName: Ident.From("value"),
                        FieldType: new DataType.Int64(),
                        Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
    }

    [Fact]
    public void parse_typed_struct_syntax_bigquery_and_generic()
    {
        // typed struct syntax https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#typed_struct_syntax
        // syntax: STRUCT<[field_name] field_type, ...>( expr1 [, ... ])

        var sql = @"SELECT STRUCT<INT64>(5), STRUCT<x INT64, y STRING>(1, t.str_col), STRUCT<arr ARRAY<FLOAT64>, str STRUCT<BOOL>>(nested_col)";
        var select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("5"))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Int64(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(
                    Expr.MakeValue(TestUtils.Number("1")),
                    new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident(
                            Value: "t",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        new Ident(
                            Value: "str_col",
                            QuoteStyle: null,
                            Span: Span.Empty())))),
                Fields: Sequence.Of(
                    new StructField(
                        FieldName: new Ident(
                            Value: "x",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        FieldType: new DataType.Int64(),
                        Options: null),
                    new StructField(
                        FieldName: new Ident(
                            Value: "y",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        FieldType: new DataType.String(null),
                        Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        sql = @"SELECT STRUCT<x STRUCT, y ARRAY<STRUCT>>(nested_col)";
        select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Identifier(new Ident(
                    Value: "nested_col",
                    QuoteStyle: null,
                    Span: Span.Empty()))),
                Fields: Sequence.Of(
                    new StructField(
                        FieldName: Ident.From("x"),
                        FieldType: new DataType.Struct(
                            Sequence<StructField>.Empty,
                            StructBracketKind.AngleBrackets),
                        Options: null),
                    new StructField(
                        FieldName: Ident.From("y"),
                        FieldType: new DataType.Array(new ArrayElemTypeDef.AngleBracket(
                            new DataType.Struct(Sequence<StructField>.Empty, StructBracketKind.AngleBrackets))),
                        Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));

        sql = @"SELECT STRUCT<BOOL>(true), STRUCT<BYTES(42)>(B'abc')";
        select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(new Value.Boolean(true).WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Bool(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.SingleQuotedByteStringLiteral("abc").WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Bytes(42),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        sql = @"SELECT STRUCT<DATE>('2011-05-05'), STRUCT<DATETIME>(DATETIME '1999-01-01 01:23:34.45'), STRUCT<FLOAT64>(5.0), STRUCT<INT64>(1)";
        select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(4, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.SingleQuotedString("2011-05-05").WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Date(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.Datetime(null),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("1999-01-01 01:23:34.45"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Datetime(null),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("5.0"))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Float64(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[2]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("1"))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Int64(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[3]));

        sql = """SELECT STRUCT<INTERVAL>(INTERVAL '1' MONTH), STRUCT<JSON>(JSON '{"class" : {"students" : [{"name" : "Jane"}]}}')""";
        select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Interval(new Interval(
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("1").WithEmptySpan()),
                    LeadingField: new DateTimeField.Month(),
                    LeadingPrecision: null,
                    LastField: null,
                    FractionalSecondsPrecision: null))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Interval(
                        Fields: null,
                        Precision: null),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.JSON(),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString(
                            """{"class" : {"students" : [{"name" : "Jane"}]}}"""),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.JSON(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        sql = @"SELECT STRUCT<STRING(42)>('foo'), STRUCT<TIMESTAMP>(TIMESTAMP '2008-12-25 15:30:00 America/Los_Angeles'), STRUCT<TIME>(TIME '15:30:00')";
        select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.SingleQuotedString("foo").WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.String(42),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString(
                            "2008-12-25 15:30:00 America/Los_Angeles"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.Time(null, new TimezoneInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("15:30:00"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Time(null, new TimezoneInfo.None()),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[2]));

        sql = @"SELECT STRUCT<NUMERIC>(NUMERIC '1'), STRUCT<BIGNUMERIC>(BIGNUMERIC '1')";
        select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.Numeric(new ExactNumberInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("1"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.Numeric(new ExactNumberInfo.None()),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.TypedString(new TypedString(
                    DataType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("1"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false))),
                Fields: Sequence.Of(new StructField(
                    FieldName: null,
                    FieldType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));
    }

    [Fact]
    public void parse_typed_struct_with_field_name_bigquery()
    {
        var sql = """SELECT STRUCT<x INT64>(5), STRUCT<y STRING>("foo")"""; // line 1, column 1
        var select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("5"))),
                Fields: Sequence.Of(new StructField(
                    FieldName: Ident.From("x"),
                    FieldType: new DataType.Int64(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.DoubleQuotedString("foo").WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: Ident.From("y"),
                    FieldType: new DataType.String(null),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        sql = @"SELECT STRUCT<x INT64, y INT64>(5, 5)"; // line 1, column 1
        select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("5")), Expr.MakeValue(TestUtils.Number("5"))),
                Fields: Sequence.Of(
                    new StructField(
                        FieldName: Ident.From("x"),
                        FieldType: new DataType.Int64(),
                        Options: null),
                    new StructField(
                        FieldName: Ident.From("y"),
                        FieldType: new DataType.Int64(),
                        Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
    }

    [Fact]
    public void parse_typed_struct_with_field_name_bigquery_and_generic()
    {
        var sql = @"SELECT STRUCT<x INT64>(5), STRUCT<y STRING>('foo')"; // line 1, column 1
        var select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("5"))),
                Fields: Sequence.Of(new StructField(
                    FieldName: Ident.From("x"),
                    FieldType: new DataType.Int64(),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of<Expr>(new Expr.Value(
                    new Value.SingleQuotedString("foo").WithEmptySpan())),
                Fields: Sequence.Of(new StructField(
                    FieldName: Ident.From("y"),
                    FieldType: new DataType.String(null),
                    Options: null))),
            TestUtils.ExprFromProjection(select.Projection[1]));

        sql = @"SELECT STRUCT<x INT64, y INT64>(5, 5)"; // line 1, column 1
        select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            new Expr.Struct(
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("5")), Expr.MakeValue(TestUtils.Number("5"))),
                Fields: Sequence.Of(
                    new StructField(
                        FieldName: Ident.From("x"),
                        FieldType: new DataType.Int64(),
                        Options: null),
                    new StructField(
                        FieldName: Ident.From("y"),
                        FieldType: new DataType.Int64(),
                        Options: null))),
            TestUtils.ExprFromProjection(select.Projection[0]));
    }

    [Fact]
    public void parse_table_identifiers()
    {
        // Parses a table identifier ident and verifies that re-serializing the
        // parsed identifier produces the original ident string.
        //
        // In some cases, re-serializing the result of the parsed ident is not
        // expected to produce the original ident string. canonical is provided
        // instead as the canonical representation of the identifier for comparison.
        // For example, re-serializing the result of ident `foo.bar` produces
        // the equivalent canonical representation `foo`.`bar`
        static void TestTableIdent(string ident, string? canonical, Sequence<Ident> expected)
        {
            var sql = $"SELECT 1 FROM {ident}";
            var canonicalSql = canonical is { } c ? $"SELECT 1 FROM {c}" : null;

            var select = canonicalSql is { } canonicalValue
                ? Bigquery().VerifiedOnlySelectWithCanonical(sql, canonicalValue)
                : Bigquery().VerifiedOnlySelect(sql);

            Assert.Equal(
                select.From,
                Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(expected)),
                    Joins: Sequence<Join>.Empty)));
        }

        static void TestTableIdentErr(string ident)
        {
            var sql = $"SELECT 1 FROM {ident}";
            Assert.Throws<ParserException>(() => Bigquery().ParseSqlStatements(sql));
        }

        TestTableIdent("`spa ce`", null, Sequence.Of(Ident.WithQuote('`', "spa ce")));

        TestTableIdent(
            "`!@#$%^&*()-=_+`",
            null,
            Sequence.Of(Ident.WithQuote('`', "!@#$%^&*()-=_+")));

        TestTableIdent(
            "_5abc.dataField",
            null,
            Sequence.Of(new Ident("_5abc"), new Ident("dataField")));
        TestTableIdent(
            "`5abc`.dataField",
            null,
            Sequence.Of(Ident.WithQuote('`', "5abc"), new Ident("dataField")));

        TestTableIdentErr("5abc.dataField");

        TestTableIdent(
            "abc5.dataField",
            null,
            Sequence.Of(new Ident("abc5"), new Ident("dataField")));

        TestTableIdentErr("abc5!.dataField");

        TestTableIdent(
            "`GROUP`.dataField",
            null,
            Sequence.Of(Ident.WithQuote('`', "GROUP"), new Ident("dataField")));

        // TODO: this should be error
        // test_table_ident_err("GROUP.dataField");

        TestTableIdent(
            "abc5.GROUP",
            null,
            Sequence.Of(new Ident("abc5"), new Ident("GROUP")));

        TestTableIdent(
            "`foo.bar.baz`",
            "`foo`.`bar`.`baz`",
            Sequence.Of(
                Ident.WithQuote('`', "foo"),
                Ident.WithQuote('`', "bar"),
                Ident.WithQuote('`', "baz")));

        TestTableIdent(
            "`foo.bar`.`baz`",
            "`foo`.`bar`.`baz`",
            Sequence.Of(
                Ident.WithQuote('`', "foo"),
                Ident.WithQuote('`', "bar"),
                Ident.WithQuote('`', "baz")));

        TestTableIdent(
            "`foo`.`bar.baz`",
            "`foo`.`bar`.`baz`",
            Sequence.Of(
                Ident.WithQuote('`', "foo"),
                Ident.WithQuote('`', "bar"),
                Ident.WithQuote('`', "baz")));

        TestTableIdent(
            "`foo`.`bar`.`baz`",
            "`foo`.`bar`.`baz`",
            Sequence.Of(
                Ident.WithQuote('`', "foo"),
                Ident.WithQuote('`', "bar"),
                Ident.WithQuote('`', "baz")));

        TestTableIdent(
            "`5abc.dataField`",
            "`5abc`.`dataField`",
            Sequence.Of(
                Ident.WithQuote('`', "5abc"),
                Ident.WithQuote('`', "dataField")));

        TestTableIdent(
            "`_5abc.da-sh-es`",
            "`_5abc`.`da-sh-es`",
            Sequence.Of(
                Ident.WithQuote('`', "_5abc"),
                Ident.WithQuote('`', "da-sh-es")));

        TestTableIdent(
            "foo-bar.baz-123",
            "foo-bar.baz-123",
            Sequence.Of(new Ident("foo-bar"), new Ident("baz-123")));

        TestTableIdentErr("foo-`bar`");
        TestTableIdentErr("`foo`-bar");
        TestTableIdentErr("foo-123a");
        TestTableIdentErr("foo - bar");
        TestTableIdentErr("123-bar");
        TestTableIdentErr("bar-");
    }

    [Fact]
    public void parse_hyphenated_table_identifiers()
    {
        Bigquery().OneStatementParsesTo(
            "select * from foo-bar f join baz-qux b on f.id = b.id",
            "SELECT * FROM foo-bar f JOIN baz-qux b ON f.id = b.id");

        Assert.Equal(
            Bigquery()
                .VerifiedOnlySelectWithCanonical(
                    "select * from foo-123.bar",
                    "SELECT * FROM foo-123.bar")
                .From[0]
                .Relation,
            TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                new Ident("foo-123"),
                new Ident("bar")))));

        Assert.Equal<SelectItem>(
            Bigquery()
                .VerifiedOnlySelectWithCanonical(
                    "SELECT foo-bar.x FROM t",
                    "SELECT foo - bar.x FROM t")
                .Projection[0],
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("foo")),
                Op: new BinaryOperator.Minus(),
                Right: new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("bar"),
                    new Ident("x"))))));
    }

    [Fact]
    public void parse_table_time_travel()
    {
        var version = "2023-08-18 23:08:18";
        var sql = $"SELECT 1 FROM t1 FOR SYSTEM_TIME AS OF '{version}'"; // line 1, column 1
        var select = Bigquery().VerifiedOnlySelect(sql);
        Assert.Equal(
            select.From,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("t1"))),
                    Alias: null,
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: new TableVersion.ForSystemTimeAsOf(new Expr.Value(
                        new Value.SingleQuotedString(version).WithEmptySpan())),
                    Partitions: Sequence<Ident>.Empty,
                    WithOrdinality: false,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Joins: Sequence<Join>.Empty)));

        var sql2 = "SELECT 1 FROM t1 FOR SYSTEM TIME AS OF 'some_timestamp'";
        Assert.Throws<ParserException>(() => Bigquery().ParseSqlStatements(sql2));
    }

    [Fact]
    public void parse_join_constraint_unnest_alias()
    {
        Assert.Equal(
            TestUtils.Only(
                Bigquery()
                    .VerifiedOnlySelect("SELECT * FROM t1 JOIN UNNEST(t1.a) AS f ON c1 = c2")
                    .From)
                .Joins,
            Sequence.Of(new Join(
                Relation: new TableFactor.UNNEST(
                    Alias: TestUtils.TableAlias(true, "f"),
                    ArrayExprs: Sequence.Of<Expr>(new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("t1"),
                        new Ident("a")))),
                    WithOffset: false,
                    WithOffsetAlias: null,
                    WithOrdinality: false),
                Global: false,
                JoinOperator: new JoinOperator.Join(new JoinConstraint.On(new Expr.BinaryOp(
                    Left: new Expr.Identifier(Ident.From("c1")),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.Identifier(Ident.From("c2"))))))));
    }

    [Fact]
    public void parse_merge()
    {
        var sql = string.Concat(
            "MERGE inventory AS T USING newArrivals AS S ON false ",
            "WHEN NOT MATCHED AND 1 THEN INSERT (product, quantity) VALUES (1, 2) ",
            "WHEN NOT MATCHED BY TARGET AND 1 THEN INSERT (product, quantity) VALUES (1, 2) ",
            "WHEN NOT MATCHED BY TARGET THEN INSERT (product, quantity) VALUES (1, 2) ",
            "WHEN NOT MATCHED BY SOURCE AND 2 THEN DELETE ",
            "WHEN NOT MATCHED BY SOURCE THEN DELETE ",
            "WHEN NOT MATCHED BY SOURCE AND 1 THEN UPDATE SET a = 1, b = 2 ",
            "WHEN NOT MATCHED AND 1 THEN INSERT (product, quantity) ROW ",
            "WHEN NOT MATCHED THEN INSERT (product, quantity) ROW ",
            "WHEN NOT MATCHED AND 1 THEN INSERT ROW ",
            "WHEN NOT MATCHED THEN INSERT ROW ",
            "WHEN MATCHED AND 1 THEN DELETE ",
            "WHEN MATCHED THEN UPDATE SET a = 1, b = 2 ",
            "WHEN NOT MATCHED THEN INSERT (a, b) VALUES (1, DEFAULT) ",
            "WHEN NOT MATCHED THEN INSERT VALUES (1, DEFAULT)"
        );
        var insertAction = new MergeAction.Insert(new MergeInsertExpr(
            InsertToken: AttachedToken.Empty(),
            Columns: Sequence.Of(ObjectName.From(new Ident("product")), ObjectName.From(new Ident("quantity"))),
            KindToken: AttachedToken.Empty(),
            Kind: new MergeInsertKind.Values(new Values(
                ValueKeyword: false,
                ExplicitRow: false,
                Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of(
                    Expr.MakeValue(TestUtils.Number("1")),
                    Expr.MakeValue(TestUtils.Number("2"))))))),
            InsertPredicate: null));
        var updateAction = new MergeAction.Update(new MergeUpdateExpr(
            UpdateToken: AttachedToken.Empty(),
            Kind: new MergeUpdateKind.Set(Sequence.Of(
                new Assignment(
                    Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident("a")))),
                    Value: Expr.MakeValue(TestUtils.Number("1"))),
                new Assignment(
                    Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident("b")))),
                    Value: Expr.MakeValue(TestUtils.Number("2"))))),
            UpdatePredicate: null,
            DeletePredicate: null));

        switch (BigqueryAndGeneric().VerifiedStmt(sql))
        {
            case Statement.Merge { Value: var merge }:
                Assert.False(merge.Into);
                Assert.Equal<TableFactor>(
                    new TableFactor.Table(
                        Name: ObjectName.From(Sequence.Of(new Ident("inventory"))),
                        Alias: TestUtils.TableAlias(true, "T"),
                        Args: null,
                        WithHints: Sequence<Expr>.Empty,
                        Version: null,
                        Partitions: Sequence<Ident>.Empty,
                        WithOrdinality: false,
                        JsonPath: null,
                        Sample: null,
                        IndexHints: Sequence<TableIndexHints>.Empty),
                    merge.Table);
                Assert.Equal<TableFactor>(
                    new TableFactor.Table(
                        Name: ObjectName.From(Sequence.Of(new Ident("newArrivals"))),
                        Alias: TestUtils.TableAlias(true, "S"),
                        Args: null,
                        WithHints: Sequence<Expr>.Empty,
                        Version: null,
                        Partitions: Sequence<Ident>.Empty,
                        WithOrdinality: false,
                        JsonPath: null,
                        Sample: null,
                        IndexHints: Sequence<TableIndexHints>.Empty),
                    merge.Source);
                Assert.Equal<Expr>(new Expr.Value(new Value.Boolean(false).WithEmptySpan()), merge.On);
                Assert.Equal(
                    Sequence.Of(
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatched(),
                            Predicate: Expr.MakeValue(TestUtils.Number("1")),
                            Action: insertAction),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatchedByTarget(),
                            Predicate: Expr.MakeValue(TestUtils.Number("1")),
                            Action: insertAction),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatchedByTarget(),
                            Predicate: null,
                            Action: insertAction),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatchedBySource(),
                            Predicate: Expr.MakeValue(TestUtils.Number("2")),
                            Action: new MergeAction.Delete(
                                DeleteToken: AttachedToken.Empty())),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatchedBySource(),
                            Predicate: null,
                            Action: new MergeAction.Delete(
                                DeleteToken: AttachedToken.Empty())),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatchedBySource(),
                            Predicate: Expr.MakeValue(TestUtils.Number("1")),
                            Action: updateAction),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatched(),
                            Predicate: Expr.MakeValue(TestUtils.Number("1")),
                            Action: new MergeAction.Insert(new MergeInsertExpr(
                                InsertToken: AttachedToken.Empty(),
                                Columns: Sequence.Of(
                                    ObjectName.From(new Ident("product")),
                                    ObjectName.From(new Ident("quantity"))),
                                KindToken: AttachedToken.Empty(),
                                Kind: new MergeInsertKind.Row(),
                                InsertPredicate: null))),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatched(),
                            Predicate: null,
                            Action: new MergeAction.Insert(new MergeInsertExpr(
                                InsertToken: AttachedToken.Empty(),
                                Columns: Sequence.Of(
                                    ObjectName.From(new Ident("product")),
                                    ObjectName.From(new Ident("quantity"))),
                                KindToken: AttachedToken.Empty(),
                                Kind: new MergeInsertKind.Row(),
                                InsertPredicate: null))),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatched(),
                            Predicate: Expr.MakeValue(TestUtils.Number("1")),
                            Action: new MergeAction.Insert(new MergeInsertExpr(
                                InsertToken: AttachedToken.Empty(),
                                Columns: Sequence<ObjectName>.Empty,
                                KindToken: AttachedToken.Empty(),
                                Kind: new MergeInsertKind.Row(),
                                InsertPredicate: null))),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatched(),
                            Predicate: null,
                            Action: new MergeAction.Insert(new MergeInsertExpr(
                                InsertToken: AttachedToken.Empty(),
                                Columns: Sequence<ObjectName>.Empty,
                                KindToken: AttachedToken.Empty(),
                                Kind: new MergeInsertKind.Row(),
                                InsertPredicate: null))),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.Matched(),
                            Predicate: Expr.MakeValue(TestUtils.Number("1")),
                            Action: new MergeAction.Delete(
                                DeleteToken: AttachedToken.Empty())),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.Matched(),
                            Predicate: null,
                            Action: updateAction),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatched(),
                            Predicate: null,
                            Action: new MergeAction.Insert(new MergeInsertExpr(
                                InsertToken: AttachedToken.Empty(),
                                Columns: Sequence.Of(ObjectName.From(new Ident("a")), ObjectName.From(new Ident("b"))),
                                KindToken: AttachedToken.Empty(),
                                Kind: new MergeInsertKind.Values(new Values(
                                    ValueKeyword: false,
                                    ExplicitRow: false,
                                    Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of(
                                        Expr.MakeValue(TestUtils.Number("1")),
                                        new Expr.Identifier(new Ident("DEFAULT"))))))),
                                InsertPredicate: null))),
                        new MergeClause(
                            WhenToken: AttachedToken.Empty(),
                            ClauseKind: new MergeClauseKind.NotMatched(),
                            Predicate: null,
                            Action: new MergeAction.Insert(new MergeInsertExpr(
                                InsertToken: AttachedToken.Empty(),
                                Columns: Sequence<ObjectName>.Empty,
                                KindToken: AttachedToken.Empty(),
                                Kind: new MergeInsertKind.Values(new Values(
                                    ValueKeyword: false,
                                    ExplicitRow: false,
                                    Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of(
                                        Expr.MakeValue(TestUtils.Number("1")),
                                        new Expr.Identifier(new Ident("DEFAULT"))))))),
                                InsertPredicate: null)))),
                    merge.Clauses);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_merge_invalid_statements()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d is BigQueryDialect || d is GenericDialect);
        foreach (var (sql, errMsg) in new[]
        {
            (
                "MERGE T USING U ON TRUE WHEN MATCHED BY TARGET AND 1 THEN DELETE",
                "Expected: THEN, found: BY"
            ),
            (
                "MERGE T USING U ON TRUE WHEN MATCHED BY SOURCE AND 1 THEN DELETE",
                "Expected: THEN, found: BY"
            ),
            (
                "MERGE T USING U ON TRUE WHEN NOT MATCHED BY SOURCE THEN INSERT(a) VALUES (b)",
                "INSERT is not allowed in a NOT MATCHED BY SOURCE merge clause"
            ),
            (
                "MERGE INTO T USING U ON TRUE WHEN NOT MATCHED BY TARGET THEN DELETE",
                "DELETE is not allowed in a NOT MATCHED BY TARGET merge clause"
            ),
            (
                "MERGE INTO T USING U ON TRUE WHEN NOT MATCHED BY TARGET THEN UPDATE SET a = b",
                "UPDATE is not allowed in a NOT MATCHED BY TARGET merge clause"
            ),
        })
        {
            var res = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
            Assert.Equal(
                new ParserError.ParserErrorValue(errMsg),
                res.Error);
        }
    }

    [Fact]
    public void parse_trailing_comma()
    {
        foreach (var (sql, canonical) in new[]
        {
            ("SELECT a,", "SELECT a"),
            ("SELECT 1,", "SELECT 1"),
            ("SELECT 1,2,", "SELECT 1, 2"),
            ("SELECT a, b,", "SELECT a, b"),
            ("SELECT a, b AS c,", "SELECT a, b AS c"),
            ("SELECT a, b AS c, FROM t", "SELECT a, b AS c FROM t"),
            ("SELECT a, b, FROM t", "SELECT a, b FROM t"),
            ("SELECT a, b, LIMIT 1", "SELECT a, b LIMIT 1"),
            ("SELECT a, (SELECT 1, )", "SELECT a, (SELECT 1)"),
        })
        {
            Bigquery().OneStatementParsesTo(sql, canonical);
        }
    }

    [Fact]
    public void parse_cast_type()
    {
        var sql = @"SELECT SAFE_CAST(1 AS INT64)";
        BigqueryAndGeneric().VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_cast_date_format()
    {
        var sql =
            @"SELECT CAST(date_valid_from AS DATE FORMAT 'YYYY-MM-DD') AS date_valid_from FROM foo";
        BigqueryAndGeneric().VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_cast_time_format()
    {
        var sql = @"SELECT CAST(TIME '21:30:00' AS STRING FORMAT 'PM') AS date_time_to_string";
        BigqueryAndGeneric().VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_cast_timestamp_format_tz()
    {
        var sql = @"SELECT CAST(TIMESTAMP '2008-12-25 00:00:00+00:00' AS STRING FORMAT 'TZH' AT TIME ZONE 'Asia/Kolkata') AS date_time_to_string";
        BigqueryAndGeneric().VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_cast_string_to_bytes_format()
    {
        var sql = @"SELECT CAST('Hello' AS BYTES FORMAT 'ASCII') AS string_to_bytes";
        BigqueryAndGeneric().VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_cast_bytes_to_string_format()
    {
        var sql = @"SELECT CAST(B'\x48\x65\x6c\x6c\x6f' AS STRING FORMAT 'ASCII') AS bytes_to_string";
        BigqueryAndGeneric().VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_array_agg_func()
    {
        foreach (var sql in new[]
        {
            "SELECT ARRAY_AGG(x ORDER BY x) AS a FROM T",
            "SELECT ARRAY_AGG(x ORDER BY x LIMIT 2) FROM tbl",
            "SELECT ARRAY_AGG(DISTINCT x ORDER BY x LIMIT 2) FROM tbl",
        })
        {
            Bigquery().VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_big_query_declare()
    {
        foreach (var (sql, expectedNames, expectedDataType, expectedAssignedExpr) in new (string, Sequence<Ident>, DataType?, DeclareAssignment?)[]
        {
            (
                "DECLARE x INT64", // line 1, column 1
                Sequence.Of(new Ident("x")),
                new DataType.Int64(),
                null
            ),
            (
                "DECLARE x INT64 DEFAULT 42",
                Sequence.Of(new Ident("x")),
                new DataType.Int64(),
                new DeclareAssignment.Default(new Expr.Value(
                    TestUtils.Number("42").WithEmptySpan()))
            ),
            (
                "DECLARE x, y, z INT64 DEFAULT 42",
                Sequence.Of(new Ident("x"), new Ident("y"), new Ident("z")),
                new DataType.Int64(),
                new DeclareAssignment.Default(new Expr.Value(
                    TestUtils.Number("42").WithEmptySpan()))
            ),
            (
                "DECLARE x DEFAULT 42",
                Sequence.Of(new Ident("x")),
                null,
                new DeclareAssignment.Default(new Expr.Value(
                    TestUtils.Number("42").WithEmptySpan()))
            ),
        })
        {
            switch (Bigquery().VerifiedStmt(sql))
            {
                case Statement.Declare { Stmts: var stmts }:
                {
                    Assert.Equal(1, stmts.Count);
                    // let Declare { names, data_type, assignment: assigned_expr, .. } = stmts.swap_remove(0);
                    var declare = stmts[0];
                    var names = declare.Names;
                    var dataType = declare.DataType;
                    var assignedExpr = declare.Assignment;
                    Assert.Equal(expectedNames, names);
                    Assert.Equal(expectedDataType, dataType);
                    Assert.Equal(expectedAssignedExpr, assignedExpr);
                    break;
                }
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }

        var errorSql = "DECLARE x";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: a data type name, found: EOF"),
            Assert.Throws<ParserException>(() => Bigquery().ParseSqlStatements(errorSql)).Error);

        var errorSql2 = "DECLARE x 42";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: a data type name, found: 42"),
            Assert.Throws<ParserException>(() => Bigquery().ParseSqlStatements(errorSql2)).Error);
    }

    [Fact]
    public void parse_bigquery_create_external_table_with_connection()
    {
        Bigquery().OneStatementParsesTo(
            string.Concat(
                "CREATE OR REPLACE EXTERNAL TABLE `proj.ds.tbl` ",
                "WITH CONNECTION `projects/proj/locations/us/connections/c` ",
                """OPTIONS(format = "ICEBERG", uris = ["gs://b/m.json"])"""
            ),
            string.Concat(
                "CREATE OR REPLACE EXTERNAL TABLE `proj`.`ds`.`tbl` () ",
                "WITH CONNECTION `projects/proj/locations/us/connections/c` ",
                """OPTIONS(format = "ICEBERG", uris = ["gs://b/m.json"])"""
            ));
        Bigquery().OneStatementParsesTo(
            "CREATE EXTERNAL TABLE t WITH CONNECTION c",
            "CREATE EXTERNAL TABLE t () WITH CONNECTION c");
        Bigquery().VerifiedStmt(string.Concat(
            "CREATE EXTERNAL TABLE t (a INT64, b STRING) ",
            """WITH CONNECTION c OPTIONS(uris = ["gs://x"])"""
        ));
        Bigquery().VerifiedStmt("""CREATE EXTERNAL TABLE t (a INT64) OPTIONS(uris = ["gs://x"])""");
    }

    private static TestedDialects Bigquery()
    {
        return new TestedDialects(new List<Dialect> { new BigQueryDialect() });
    }

    private static TestedDialects BigqueryAndGeneric()
    {
        return new TestedDialects(new List<Dialect>
        {
            new BigQueryDialect(),
            new GenericDialect(),
        });
    }

    [Fact]
    public void parse_map_access_expr()
    {
        var sql = "users[-1][safe_offset(2)].a.b";
        var expr = Bigquery().VerifiedExpr(sql);

        var expected = new Expr.CompoundFieldAccess(
            Root: new Expr.Identifier(Ident.WithSpan(
                new Span(Location.Of(1, 1), Location.Of(1, 6)),
                "users")),
            AccessChain: Sequence.Of<AccessExpr>(
                new AccessExpr.Subscript(new Subscript.Index(
                    IndexValue: new Expr.UnaryOp(
                        Op: new UnaryOperator.Minus(),
                        Expr: Expr.MakeValue(TestUtils.Number("1"))))),
                new AccessExpr.Subscript(new Subscript.Index(
                    IndexValue: new Expr.Function(new Function(
                        Name: ObjectName.From(Sequence.Of(Ident.WithSpan(
                            new Span(Location.Of(1, 11), Location.Of(1, 22)),
                            "safe_offset"))),
                        Parameters: new FunctionArguments.None(),
                        Args: new FunctionArguments.List(new FunctionArgumentList(
                            DuplicateTreatment: null,
                            Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                                TestUtils.Number("2").WithEmptySpan())))),
                            Clauses: Sequence<FunctionArgumentClause>.Empty)),
                        Filter: null,
                        NullTreatment: null,
                        Over: null,
                        WithinGroup: Sequence<OrderByExpr>.Empty,
                        UsesOdbcSyntax: false)))),
                new AccessExpr.Dot(new Expr.Identifier(Ident.WithSpan(
                    new Span(Location.Of(1, 24), Location.Of(1, 25)),
                    "a"))),
                new AccessExpr.Dot(new Expr.Identifier(Ident.WithSpan(
                    new Span(Location.Of(1, 26), Location.Of(1, 27)),
                    "b")))));
        Assert.Equal<Expr>(expr, expected);

        sql = "SELECT myfunc()[-1].a[SAFE_OFFSET(2)].b";
        Bigquery().VerifiedOnlySelect(sql);
    }

    [Fact]
    public void test_bigquery_create_function()
    {
        var sql = string.Concat(
            "CREATE OR REPLACE TEMPORARY FUNCTION ",
            "project1.mydataset.myfunction(x FLOAT64) ",
            "RETURNS FLOAT64 ",
            "OPTIONS(x = 'y') ",
            "AS 42"
        );

        var stmt = Bigquery().VerifiedStmt(sql);
        Assert.Equal<Statement>(
            stmt,
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: true,
                Temporary: true,
                IfNotExists: false,
                Name: ObjectName.From(Sequence.Of(
                    new Ident("project1"),
                    new Ident("mydataset"),
                    new Ident("myfunction"))),
                Args: Sequence.Of(OperateFunctionArg.WithName("x", new DataType.Float64())),
                ReturnType: new FunctionReturnType.DataType(new DataType.Float64()),
                FunctionBody: new CreateFunctionBody.AsAfterOptions(new Expr.Value(
                    TestUtils.Number("42").WithEmptySpan())),
                Options: Sequence.Of<SqlOption>(new SqlOption.KeyValue(
                    Key: new Ident("x"),
                    Value: new Expr.Value(new Value.SingleQuotedString("y").WithEmptySpan()))),
                Behavior: null,
                Using: null,
                Language: null,
                DeterminismSpecifier: null,
                RemoteConnection: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty)));

        var sqls = new[]
        {
            // Arbitrary Options expressions.
            string.Concat(
                "CREATE OR REPLACE TEMPORARY FUNCTION ",
                "myfunction(a FLOAT64, b INT64, c STRING) ",
                "RETURNS ARRAY<FLOAT64> ",
                "OPTIONS(a = [1, 2], b = 'two', c = [('k1', 'v1'), ('k2', 'v2')]) ",
                "AS ((SELECT 1 FROM mytable))"
            ),
            // Options after body.
            string.Concat(
                "CREATE OR REPLACE TEMPORARY FUNCTION ",
                "myfunction(a FLOAT64, b INT64, c STRING) ",
                "RETURNS ARRAY<FLOAT64> ",
                "AS ((SELECT 1 FROM mytable)) ",
                "OPTIONS(a = [1, 2], b = 'two', c = [('k1', 'v1'), ('k2', 'v2')])"
            ),
            // IF NOT EXISTS
            string.Concat(
                "CREATE OR REPLACE TEMPORARY FUNCTION IF NOT EXISTS ",
                "myfunction(a FLOAT64, b INT64, c STRING) ",
                "RETURNS ARRAY<FLOAT64> ",
                "OPTIONS(a = [1, 2]) ",
                "AS ((SELECT 1 FROM mytable))"
            ),
            // No return type.
            string.Concat(
                "CREATE OR REPLACE TEMPORARY FUNCTION ",
                "myfunction(a FLOAT64, b INT64, c STRING) ",
                "OPTIONS(a = [1, 2]) ",
                "AS ((SELECT 1 FROM mytable))"
            ),
            // With language - body after options
            string.Concat(
                "CREATE OR REPLACE TEMPORARY FUNCTION ",
                "myfunction(a FLOAT64, b INT64, c STRING) ",
                "DETERMINISTIC ",
                "LANGUAGE js ",
                "OPTIONS(a = [1, 2]) ",
                "AS \"console.log('hello');\""
            ),
            // With language - body before options
            string.Concat(
                "CREATE OR REPLACE TEMPORARY FUNCTION ",
                "myfunction(a FLOAT64, b INT64, c STRING) ",
                "NOT DETERMINISTIC ",
                "LANGUAGE js ",
                "AS \"console.log('hello');\" ",
                "OPTIONS(a = [1, 2])"
            ),
            // Remote
            string.Concat(
                "CREATE OR REPLACE TEMPORARY FUNCTION ",
                "myfunction(a FLOAT64, b INT64, c STRING) ",
                "RETURNS INT64 ",
                "REMOTE WITH CONNECTION us.myconnection ",
                "OPTIONS(a = [1, 2])"
            ),
        };
        foreach (var s in sqls)
        {
            Bigquery().VerifiedStmt(s);
        }

        var errorSqls = new[]
        {
            (
                string.Concat(
                    "CREATE TEMPORARY FUNCTION myfunction() ",
                    "OPTIONS(a = [1, 2]) ",
                    "AS ((SELECT 1 FROM mytable)) ",
                    "OPTIONS(a = [1, 2])"
                ),
                "Expected: end of statement, found: OPTIONS"
            ),
            (
                string.Concat(
                    "CREATE TEMPORARY FUNCTION myfunction() ",
                    "IMMUTABLE ",
                    "AS ((SELECT 1 FROM mytable)) "
                ),
                "Expected: AS, found: IMMUTABLE"
            ),
            (
                string.Concat(
                    "CREATE TEMPORARY FUNCTION myfunction() ",
                    "AS \"console.log('hello');\" ",
                    "LANGUAGE js "
                ),
                "Expected: end of statement, found: LANGUAGE"
            ),
        };
        foreach (var (errorSql, error) in errorSqls)
        {
            Assert.Equal(
                new ParserError.ParserErrorValue(error),
                Assert.Throws<ParserException>(() => Bigquery().ParseSqlStatements(errorSql)).Error);
        }
    }

    [Fact]
    public void test_bigquery_trim()
    {
        var realSql = """SELECT customer_id, TRIM(item_price_id, '"', "a") AS item_price_id FROM models_staging.subscriptions""";
        Assert.Equal(Bigquery().VerifiedStmt(realSql).ToString(), realSql);

        var sqlOnlySelect = "SELECT TRIM('xyz', 'a')";
        var select = Bigquery().VerifiedOnlySelect(sqlOnlySelect);
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
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: 'a'"),
            Assert.Throws<ParserException>(() => Bigquery().ParseSqlStatements(errorSql)).Error);
    }

    [Fact]
    public void parse_extract_weekday()
    {
        var sql = "SELECT EXTRACT(WEEK(MONDAY) FROM d)";
        var select = BigqueryAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Extract(
                Field: new DateTimeField.Week(new Ident("MONDAY")),
                Syntax: ExtractSyntax.From,
                Expr: new Expr.Identifier(new Ident("d"))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void bigquery_select_expr_star()
    {
        Bigquery()
            .VerifiedOnlySelect("SELECT STRUCT<STRING>((SELECT foo FROM T WHERE true)).* FROM T");
        Bigquery().VerifiedOnlySelect("SELECT [STRUCT<STRING>('foo')][0].* EXCEPT (foo) FROM T");
        Bigquery().VerifiedOnlySelect("SELECT myfunc()[0].* FROM T");
    }

    [Fact]
    public void test_select_as_struct()
    {
        foreach (var (sql, parseTo) in new[]
        {
            (
                "SELECT * FROM (SELECT AS STRUCT STRUCT(123 AS a, false AS b))",
                "SELECT * FROM (SELECT AS STRUCT STRUCT(123 AS a, false AS b))"
            ),
            (
                "SELECT * FROM (SELECT DISTINCT AS STRUCT STRUCT(123 AS a, false AS b))",
                "SELECT * FROM (SELECT DISTINCT AS STRUCT STRUCT(123 AS a, false AS b))"
            ),
            (
                "SELECT * FROM (SELECT ALL AS STRUCT STRUCT(123 AS a, false AS b))",
                "SELECT * FROM (SELECT AS STRUCT STRUCT(123 AS a, false AS b))"
            ),
        })
        {
            Bigquery().OneStatementParsesTo(sql, parseTo);
        }

        var select = Bigquery().VerifiedOnlySelect("SELECT AS STRUCT 1 AS a, 2 AS b");
        Assert.Equal<ValueTableMode?>(new ValueTableMode.AsStruct(), select.ValueTableMode);
    }

    [Fact]
    public void test_select_as_value()
    {
        foreach (var (sql, parseTo) in new[]
        {
            (
                "SELECT * FROM (SELECT AS VALUE STRUCT(5 AS star_rating, false AS up_down_rating))",
                "SELECT * FROM (SELECT AS VALUE STRUCT(5 AS star_rating, false AS up_down_rating))"
            ),
            (
                "SELECT * FROM (SELECT DISTINCT AS VALUE STRUCT(5 AS star_rating, false AS up_down_rating))",
                "SELECT * FROM (SELECT DISTINCT AS VALUE STRUCT(5 AS star_rating, false AS up_down_rating))"
            ),
            (
                "SELECT * FROM (SELECT ALL AS VALUE STRUCT(5 AS star_rating, false AS up_down_rating))",
                "SELECT * FROM (SELECT AS VALUE STRUCT(5 AS star_rating, false AS up_down_rating))"
            ),
        })
        {
            Bigquery().OneStatementParsesTo(sql, parseTo);
        }

        var select = Bigquery().VerifiedOnlySelect("SELECT AS VALUE STRUCT(1 AS a, 2 AS b) AS xyz");
        Assert.Equal<ValueTableMode?>(new ValueTableMode.AsValue(), select.ValueTableMode);
    }

    [Fact]
    public void test_triple_quote_typed_strings()
    {
        Bigquery().VerifiedExpr("""JSON '''{"foo":"bar's"}'''""");

        var expr = Bigquery().VerifiedExpr("JSON \"\"\"{\"foo\":\"bar's\"}\"\"\"");
        Assert.Equal<Expr>(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.JSON(),
                Value: new ValueWithSpan(
                    Value: new Value.TripleDoubleQuotedString("""{"foo":"bar's"}"""),
                    Span: Span.Empty()),
                UsesOdbcSyntax: false)),
            expr);
    }

    [Fact]
    public void test_array_agg()
    {
        BigqueryAndGeneric().VerifiedExpr("ARRAY_AGG(state)");
        BigqueryAndGeneric().VerifiedExpr("ARRAY_CONCAT_AGG(x LIMIT 2)");
        BigqueryAndGeneric().VerifiedExpr("ARRAY_AGG(state IGNORE NULLS LIMIT 10)");
        BigqueryAndGeneric().VerifiedExpr("ARRAY_AGG(state RESPECT NULLS ORDER BY population)");
        BigqueryAndGeneric()
            .VerifiedExpr("ARRAY_AGG(DISTINCT state IGNORE NULLS ORDER BY population DESC LIMIT 10)");
        BigqueryAndGeneric().VerifiedExpr("ARRAY_CONCAT_AGG(x ORDER BY ARRAY_LENGTH(x))");
    }

    [Fact]
    public void test_any_value()
    {
        BigqueryAndGeneric().VerifiedExpr("ANY_VALUE(fruit)");
        BigqueryAndGeneric().VerifiedExpr(
            "ANY_VALUE(fruit) OVER (ORDER BY LENGTH(fruit) ROWS BETWEEN 1 PRECEDING AND CURRENT ROW)");
        BigqueryAndGeneric().VerifiedExpr("ANY_VALUE(fruit HAVING MAX sold)");
        BigqueryAndGeneric().VerifiedExpr("ANY_VALUE(fruit HAVING MIN sold)");
    }

    [Fact]
    public void test_any_type()
    {
        Bigquery().VerifiedStmt(string.Concat(
            "CREATE OR REPLACE TEMPORARY FUNCTION ",
            "my_function(param1 ANY TYPE) ",
            "AS (",
            "(SELECT 1)",
            ")"
        ));
    }

    [Fact]
    public void test_any_type_dont_break_custom_type()
    {
        BigqueryAndGeneric().VerifiedStmt("CREATE TABLE foo (x ANY)");
    }

    [Fact]
    public void test_struct_field_options()
    {
        Bigquery().VerifiedStmt(string.Concat(
            "CREATE TABLE my_table (",
            "f0 STRUCT<a STRING, b INT64>, ",
            "f1 STRUCT<",
            "a STRING OPTIONS(description = 'This is a string', type = 'string'), ",
            "b INT64",
            "> OPTIONS(description = 'This is a struct field')",
            ")"
        ));
    }

    [Fact]
    public void test_struct_trailing_and_nested_bracket()
    {
        Bigquery().VerifiedStmt(string.Concat(
            "CREATE TABLE my_table (",
            "f0 STRING, ",
            "f1 STRUCT<a STRING, b STRUCT<c INT64, d STRING>>, ",
            "f2 STRING",
            ")"
        ));

        // More complex nested structs
        Bigquery().VerifiedStmt(string.Concat(
            "CREATE TABLE my_table (",
            "f0 STRING, ",
            "f1 STRUCT<a STRING, b STRUCT<c INT64, d STRUCT<e STRING>>>, ",
            "f2 STRUCT<h STRING, i STRUCT<j INT64, k STRUCT<l STRUCT<m STRING>>>>, ",
            "f3 STRUCT<e STRING, f STRUCT<c INT64>>",
            ")"
        ));

        // Bad case with missing closing bracket
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: >, found: )"),
            Assert.Throws<ParserException>(() => Bigquery()
                .ParseSqlStatements("CREATE TABLE my_table(f1 STRUCT<a STRING, b INT64)")).Error);

        // Bad case with redundant closing bracket
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "unmatched > after parsing data type STRUCT<a STRING, b INT64>)"),
            Assert.Throws<ParserException>(() => Bigquery()
                .ParseSqlStatements("CREATE TABLE my_table(f1 STRUCT<a STRING, b INT64>>)")).Error);

        // Base case with redundant closing bracket in nested struct
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: ',' or ')' after column definition, found: >"),
            Assert.Throws<ParserException>(() => Bigquery()
                .ParseSqlStatements("CREATE TABLE my_table(f1 STRUCT<a STRUCT<b INT>>>, c INT64)")).Error);

        var sql = "SELECT STRUCT<INT64>>(NULL)";
        Assert.Equal(
            Assert.Throws<ParserException>(() => BigqueryAndGeneric()
                .ParseSqlStatements(sql)).Error,
            new ParserError.ParserErrorValue("unmatched > in STRUCT literal"));

        var sql2 = "SELECT STRUCT<STRUCT<INT64>>>(NULL)";
        Assert.Equal(
            Assert.Throws<ParserException>(() => BigqueryAndGeneric()
                .ParseSqlStatements(sql2)).Error,
            new ParserError.ParserErrorValue("Expected: (, found: >"));

        var sql3 = "CREATE TABLE table (x STRUCT<STRUCT<INT64>>>)";
        Assert.Equal(
            Assert.Throws<ParserException>(() => BigqueryAndGeneric()
                .ParseSqlStatements(sql3)).Error,
            new ParserError.ParserErrorValue(
                "Expected: ',' or ')' after column definition, found: >"));
    }

    [Fact]
    public void test_export_data()
    {
        var stmt = Bigquery().VerifiedStmt(string.Concat(
            "EXPORT DATA OPTIONS(",
            "uri = 'gs://bucket/folder/*', ",
            "format = 'PARQUET', ",
            "overwrite = true",
            ") AS ",
            "SELECT field1, field2 FROM mydataset.table1 ORDER BY field1 LIMIT 10"
        ));
        Assert.Equal<Statement>(
            stmt,
            new Statement.ExportData(new ExportData(
                Options: Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: new Ident("uri"),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("gs://bucket/folder/*")
                                .WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: new Ident("format"),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("PARQUET").WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: new Ident("overwrite"),
                        Value: new Expr.Value(new Value.Boolean(true).WithEmptySpan()))),
                Connection: null,
                Query: new Query(
                    With: null,
                    Body: new SetExpr.Select(new Select(
                        SelectToken: new AttachedToken(new TokenWithSpan(
                            new Token.Word(new Word(
                                Value: "SELECT",
                                QuoteStyle: null,
                                Keyword: Keyword.SELECT)),
                            Span.Empty())),
                        OptimizerHints: Sequence<OptimizerHint>.Empty,
                        Distinct: null,
                        SelectModifiers: null,
                        Top: null,
                        TopBeforeDistinct: false,
                        Projection: Sequence.Of<SelectItem>(
                            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("field1"))),
                            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("field2")))),
                        Exclude: null,
                        Into: null,
                        From: Sequence.Of(new TableWithJoins(
                            Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                                new Ident("mydataset"),
                                new Ident("table1")))),
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
                        Qualify: null,
                        WindowBeforeQualify: false,
                        ValueTableMode: null,
                        ConnectBy: Sequence<ConnectByKind>.Empty,
                        Flavor: SelectFlavor.Standard)),
                    OrderBy: new OrderBy(
                        Kind: new OrderByKind.Expressions(Sequence.Of(new OrderByExpr(
                            Expr: new Expr.Identifier(new Ident("field1")),
                            Options: new OrderByOptions(
                                Sort: null,
                                NullsFirst: null),
                            WithFill: null))),
                        Interpolate: null),
                    LimitClause: new LimitClause.LimitOffset(
                        Limit: new Expr.Value(TestUtils.Number("10").WithEmptySpan()),
                        Offset: null,
                        LimitBy: Sequence<Expr>.Empty),
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty))));

        stmt = Bigquery().VerifiedStmt(string.Concat(
            "EXPORT DATA WITH CONNECTION myconnection.myproject.us OPTIONS(",
            "uri = 'gs://bucket/folder/*', ",
            "format = 'PARQUET', ",
            "overwrite = true",
            ") AS ",
            "SELECT field1, field2 FROM mydataset.table1 ORDER BY field1 LIMIT 10"
        ));

        Assert.Equal<Statement>(
            stmt,
            new Statement.ExportData(new ExportData(
                Options: Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: new Ident("uri"),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("gs://bucket/folder/*")
                                .WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: new Ident("format"),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("PARQUET").WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: new Ident("overwrite"),
                        Value: new Expr.Value(new Value.Boolean(true).WithEmptySpan()))),
                Connection: ObjectName.From(Sequence.Of(
                    new Ident("myconnection"),
                    new Ident("myproject"),
                    new Ident("us"))),
                Query: new Query(
                    With: null,
                    Body: new SetExpr.Select(new Select(
                        SelectToken: new AttachedToken(new TokenWithSpan(
                            new Token.Word(new Word(
                                Value: "SELECT",
                                QuoteStyle: null,
                                Keyword: Keyword.SELECT)),
                            Span.Empty())),
                        OptimizerHints: Sequence<OptimizerHint>.Empty,
                        Distinct: null,
                        SelectModifiers: null,
                        Top: null,
                        TopBeforeDistinct: false,
                        Projection: Sequence.Of<SelectItem>(
                            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("field1"))),
                            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("field2")))),
                        Exclude: null,
                        Into: null,
                        From: Sequence.Of(new TableWithJoins(
                            Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                                new Ident("mydataset"),
                                new Ident("table1")))),
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
                        Qualify: null,
                        WindowBeforeQualify: false,
                        ValueTableMode: null,
                        ConnectBy: Sequence<ConnectByKind>.Empty,
                        Flavor: SelectFlavor.Standard)),
                    OrderBy: new OrderBy(
                        Kind: new OrderByKind.Expressions(Sequence.Of(new OrderByExpr(
                            Expr: new Expr.Identifier(new Ident("field1")),
                            Options: new OrderByOptions(
                                Sort: null,
                                NullsFirst: null),
                            WithFill: null))),
                        Interpolate: null),
                    LimitClause: new LimitClause.LimitOffset(
                        Limit: new Expr.Value(TestUtils.Number("10").WithEmptySpan()),
                        Offset: null,
                        LimitBy: Sequence<Expr>.Empty),
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty))));

        // at least one option (uri) is required
        var err = Assert.Throws<ParserException>(() => Bigquery()
            .ParseSqlStatements(string.Concat(
                "EXPORT DATA OPTIONS() AS ",
                "SELECT field1, field2 FROM mydataset.table1 ORDER BY field1 LIMIT 10"
            ))).Error;
        Assert.Equal(
            "sql parser error: Expected: identifier, found: )",
            err.ToString());

        err = Assert.Throws<ParserException>(() => Bigquery()
            .ParseSqlStatements(
                "EXPORT DATA AS SELECT field1, field2 FROM mydataset.table1 ORDER BY field1 LIMIT 10"
            )).Error;
        Assert.Equal(
            "sql parser error: Expected: OPTIONS, found: AS",
            err.ToString());
    }

    [Fact]
    public void test_begin_transaction()
    {
        Bigquery().VerifiedStmt("BEGIN TRANSACTION");
    }

    [Fact]
    public void test_begin_statement()
    {
        Bigquery().VerifiedStmt("BEGIN");
    }

    [Fact]
    public void test_alter_schema()
    {
        BigqueryAndGeneric().VerifiedStmt("ALTER SCHEMA mydataset SET DEFAULT COLLATE 'und:ci'");
        BigqueryAndGeneric().VerifiedStmt("ALTER SCHEMA mydataset ADD REPLICA 'us'");
        BigqueryAndGeneric()
            .VerifiedStmt("ALTER SCHEMA mydataset ADD REPLICA 'us' OPTIONS (location = 'us')");
        BigqueryAndGeneric().VerifiedStmt("ALTER SCHEMA mydataset DROP REPLICA 'us'");
        BigqueryAndGeneric().VerifiedStmt("ALTER SCHEMA mydataset SET OPTIONS (location = 'us')");
        BigqueryAndGeneric()
            .VerifiedStmt("ALTER SCHEMA IF EXISTS mydataset SET OPTIONS (location = 'us')");
    }

    [Fact]
    public void test_create_snapshot_table()
    {
        BigqueryAndGeneric()
            .VerifiedStmt("CREATE SNAPSHOT TABLE dataset_id.table1 CLONE dataset_id.table2");

        Bigquery().VerifiedStmt(
            "CREATE SNAPSHOT TABLE IF NOT EXISTS dataset_id.table1 CLONE dataset_id.table2");

        Bigquery().VerifiedStmt(
            "CREATE SNAPSHOT TABLE dataset_id.table1 CLONE dataset_id.table2 FOR SYSTEM_TIME AS OF TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 1 HOUR)");

        Bigquery().VerifiedStmt(
            "CREATE SNAPSHOT TABLE dataset_id.table1 CLONE dataset_id.table2 OPTIONS(expiration_timestamp = TIMESTAMP '2025-01-01 00:00:00 UTC', friendly_name = 'my_table')");

        Bigquery().VerifiedStmt(
            "CREATE SNAPSHOT TABLE IF NOT EXISTS dataset_id.table1 CLONE dataset_id.table2 FOR SYSTEM_TIME AS OF TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 1 HOUR) OPTIONS(expiration_timestamp = TIMESTAMP '2025-01-01 00:00:00 UTC')");
    }

    [Fact]
    public void parse_from_first_select()
    {
        Bigquery().VerifiedStmt("FROM t");
        Bigquery().VerifiedStmt("FROM t SELECT a, b");
        Bigquery().VerifiedStmt("FROM t |> WHERE a > 1 |> SELECT a");
    }

    [Fact]
    public void test_byte_and_raw_string_quote_escaping()
    {
        var generic = new TestedDialects(new List<Dialect> { new GenericDialect() });
        generic.VerifiedStmt("SELECT B''''");
        generic.VerifiedStmt("SELECT B'it''s'");
        generic.VerifiedStmt("SELECT B\"\"\"\"");
        generic.VerifiedStmt("SELECT B\"it\"\"s\"");
        generic.VerifiedStmt("SELECT R'it''s'");
        generic.VerifiedStmt("SELECT R\"it\"\"s\"");
        generic.VerifiedStmt("SELECT COUNSELECT AS name, B''''");

        BigqueryAndGeneric().VerifiedStmt("SELECT B'it''s'");
        BigqueryAndGeneric().VerifiedStmt("SELECT B\"it\"\"s\"");
        BigqueryAndGeneric().VerifiedStmt("SELECT R'it''s'");
        BigqueryAndGeneric().VerifiedStmt("SELECT R\"it\"\"s\"");

        var err = Assert.Throws<ParserException>(() => generic
            .ParseSqlStatements("SELECT B'unterminated")).Error;
        Assert.Equal<ParserError>(
            new ParserError.TokenizerError(
                "Unterminated string literal at Line: 1, Column: 9"),
            err);
    }
}
