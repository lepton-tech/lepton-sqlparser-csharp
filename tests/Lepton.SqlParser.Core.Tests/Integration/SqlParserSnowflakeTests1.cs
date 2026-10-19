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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_snowflake.rs (linhas 1–2575).
// Modificado: tradução de Rust para C#.

using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C11.md):
// - Test SQL syntax specific to Snowflake. The parser based on the
//   generic dialect is also tested (on the inputs it can handle).
// - Primeira fatia do arquivo Rust; as funções-atalho do arquivo (`snowflake()`, `snowflake_and_generic()`...)
//   foram copiadas como métodos `private static` (nomes mecânicos), como em SqlParserSnowflakeTests2.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000).
// - `assert_eq!(Some(true), x)` / `assert_eq!(Some(false), x)` com `x: Option<bool>` → `Assert.True(x)` /
//   `Assert.False(x)` (sobrecargas de `bool?`, que falham com `null`, como a comparação do Rust; xUnit2004).
// - `x.unwrap()` → `x ?? throw new InvalidOperationException(..)`; `unreachable!()` →
//   `throw new InvalidOperationException("unreachable")`.
// - `assert_eq!(1, v.len())` usa `Assert.Equal(1, v.Count)`, como no Rust; o xUnit2013 fica suprimido.
// - Linhas Rust terminadas em `\` (continuação de string) e `concat!(..)` → concatenação dos pedaços.
#pragma warning disable xUnit2013
public class SqlParserSnowflakeTests1
{
    private const string UnwrapNone = "called `Option::unwrap()` on a `None` value";

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
    public void test_snowflake_create_table()
    {
        var sql = "CREATE TABLE _my_$table (am00unt number)";
        switch (SnowflakeAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name } }:
                Assert.Equal("_my_$table", name.ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_sf_create_secure_view_and_materialized_view()
    {
        foreach (var sql in new[]
        {
            "CREATE SECURE VIEW v AS SELECT 1",
            "CREATE SECURE MATERIALIZED VIEW v AS SELECT 1",
            "CREATE OR REPLACE SECURE VIEW v AS SELECT 1",
            "CREATE OR REPLACE SECURE MATERIALIZED VIEW v AS SELECT 1",
        })
        {
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.CreateView { Value: { Secure: var secure, Materialized: var materialized } }:
                    Assert.True(secure);
                    if (sql.Contains("MATERIALIZED"))
                    {
                        Assert.True(materialized);
                    }
                    else
                    {
                        Assert.False(materialized);
                    }
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
            Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
        }
    }

    [Fact]
    public void test_snowflake_create_or_replace_table()
    {
        var sql = "CREATE OR REPLACE TABLE my_table (a number)";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, OrReplace: var orReplace } }:
                Assert.Equal("my_table", name.ToString());
                Assert.True(orReplace);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_or_replace_table_copy_grants()
    {
        var sql = "CREATE OR REPLACE TABLE my_table (a number) COPY GRANTS";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, OrReplace: var orReplace, CopyGrants: var copyGrants } }:
                Assert.Equal("my_table", name.ToString());
                Assert.True(orReplace);
                Assert.True(copyGrants);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_or_replace_table_copy_grants_at_end()
    {
        var sql = "CREATE OR REPLACE TABLE my_table COPY GRANTS (a number) ";
        var parsed = "CREATE OR REPLACE TABLE my_table (a number) COPY GRANTS";
        switch (Snowflake().OneStatementParsesTo(sql, parsed))
        {
            case Statement.CreateTable { Value: { Name: var name, OrReplace: var orReplace, CopyGrants: var copyGrants } }:
                Assert.Equal("my_table", name.ToString());
                Assert.True(orReplace);
                Assert.True(copyGrants);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_or_replace_table_copy_grants_cta()
    {
        var sql = "CREATE OR REPLACE TABLE my_table COPY GRANTS AS SELECT 1 AS a";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, OrReplace: var orReplace, CopyGrants: var copyGrants } }:
                Assert.Equal("my_table", name.ToString());
                Assert.True(orReplace);
                Assert.True(copyGrants);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_enable_schema_evolution()
    {
        var sql = "CREATE TABLE my_table (a number) ENABLE_SCHEMA_EVOLUTION=TRUE";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, EnableSchemaEvolution: var enableSchemaEvolution } }:
                Assert.Equal("my_table", name.ToString());
                Assert.True(enableSchemaEvolution); // assert_eq!(Some(true), enable_schema_evolution)
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_change_tracking()
    {
        var sql = "CREATE TABLE my_table (a number) CHANGE_TRACKING=TRUE";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, ChangeTracking: var changeTracking } }:
                Assert.Equal("my_table", name.ToString());
                Assert.True(changeTracking); // assert_eq!(Some(true), change_tracking)
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_data_retention_time_in_days()
    {
        var sql = "CREATE TABLE my_table (a number) DATA_RETENTION_TIME_IN_DAYS=5";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, DataRetentionTimeInDays: var dataRetentionTimeInDays } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal<ulong?>(5, dataRetentionTimeInDays);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_max_data_extension_time_in_days()
    {
        var sql = "CREATE TABLE my_table (a number) MAX_DATA_EXTENSION_TIME_IN_DAYS=5";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, MaxDataExtensionTimeInDays: var maxDataExtensionTimeInDays } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal<ulong?>(5, maxDataExtensionTimeInDays);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_with_aggregation_policy()
    {
        switch (Snowflake()
            .VerifiedStmt("CREATE TABLE my_table (a number) WITH AGGREGATION POLICY policy_name"))
        {
            case Statement.CreateTable { Value: { Name: var name, WithAggregationPolicy: var withAggregationPolicy } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal(
                    "policy_name",
                    withAggregationPolicy?.ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        switch (Snowflake()
            .ParseSqlStatements("CREATE TABLE my_table (a number)  AGGREGATION POLICY policy_name")
            .Last
            ?? throw new InvalidOperationException(UnwrapNone))
        {
            case Statement.CreateTable { Value: { Name: var name, WithAggregationPolicy: var withAggregationPolicy } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal(
                    "policy_name",
                    withAggregationPolicy?.ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_with_row_access_policy()
    {
        switch (Snowflake().VerifiedStmt(
            "CREATE TABLE my_table (a number, b number) WITH ROW ACCESS POLICY policy_name ON (a)"))
        {
            case Statement.CreateTable { Value: { Name: var name, WithRowAccessPolicy: var withRowAccessPolicy } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal(
                    "WITH ROW ACCESS POLICY policy_name ON (a)",
                    withRowAccessPolicy?.ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        switch (Snowflake()
            .ParseSqlStatements(
                "CREATE TABLE my_table (a number, b number) ROW ACCESS POLICY policy_name ON (a)")
            .Last
            ?? throw new InvalidOperationException(UnwrapNone))
        {
            case Statement.CreateTable { Value: { Name: var name, WithRowAccessPolicy: var withRowAccessPolicy } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal(
                    "WITH ROW ACCESS POLICY policy_name ON (a)",
                    withRowAccessPolicy?.ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_with_storage_lifecycle_policy()
    {
        // WITH keyword
        switch (Snowflake().VerifiedStmt(
            "CREATE TABLE IF NOT EXISTS my_table (a NUMBER(38, 0), b VARIANT) WITH STORAGE LIFECYCLE POLICY dba.global_settings.my_policy ON (a)"))
        {
            case Statement.CreateTable { Value: { Name: var name, WithStorageLifecyclePolicy: var withStorageLifecyclePolicy } }:
                Assert.Equal("my_table", name.ToString());
                var policy = withStorageLifecyclePolicy ?? throw new InvalidOperationException(UnwrapNone);
                Assert.Equal("dba.global_settings.my_policy", policy.Policy.ToString());
                Assert.Equal(Sequence.Of(new Ident("a")), policy.On);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Without WITH keyword — canonicalizes to WITH form
        Snowflake().OneStatementParsesTo(
            "CREATE TABLE my_table (a NUMBER(38, 0)) STORAGE LIFECYCLE POLICY my_policy ON (a, b)",
            "CREATE TABLE my_table (a NUMBER(38, 0)) WITH STORAGE LIFECYCLE POLICY my_policy ON (a, b)");
    }

    [Fact]
    public void test_snowflake_create_table_with_tag()
    {
        switch (Snowflake()
            .VerifiedStmt("CREATE TABLE my_table (a number) WITH TAG (A='TAG A', B='TAG B')"))
        {
            case Statement.CreateTable { Value: { Name: var name, WithTags: var withTags } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal(
                    (Sequence<Tag>?)Sequence.Of(
                        new Tag(ObjectName.From(Sequence.Of(new Ident("A"))), "TAG A"),
                        new Tag(ObjectName.From(Sequence.Of(new Ident("B"))), "TAG B")),
                    withTags);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        switch (Snowflake()
            .ParseSqlStatements("CREATE TABLE my_table (a number) TAG (A='TAG A', B='TAG B')")
            .Last
            ?? throw new InvalidOperationException(UnwrapNone))
        {
            case Statement.CreateTable { Value: { Name: var name, WithTags: var withTags } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal(
                    (Sequence<Tag>?)Sequence.Of(
                        new Tag(ObjectName.From(Sequence.Of(new Ident("A"))), "TAG A"),
                        new Tag(ObjectName.From(Sequence.Of(new Ident("B"))), "TAG B")),
                    withTags);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_default_ddl_collation()
    {
        var sql = "CREATE TABLE my_table (a number) DEFAULT_DDL_COLLATION='de'";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, DefaultDdlCollation: var defaultDdlCollation } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal("de", defaultDdlCollation);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_transient_table()
    {
        var sql = "CREATE TRANSIENT TABLE CUSTOMER (id INT, name VARCHAR(255))";
        switch (SnowflakeAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, Transient: var transient } }:
                Assert.Equal("CUSTOMER", name.ToString());
                Assert.True(transient);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_column_comment()
    {
        var sql = "CREATE TABLE my_table (a STRING COMMENT 'some comment')";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, Columns: var columns } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal(
                    Sequence.Of(new ColumnDef(
                        Name: Ident.From("a"),
                        DataType: new DataType.String(null),
                        Options: Sequence.Of(new ColumnOptionDef(
                            Name: null,
                            Option: new ColumnOption.Comment("some comment"))))),
                    columns);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_on_commit()
    {
        Snowflake().VerifiedStmt(
            "CREATE LOCAL TEMPORARY TABLE \"AAA\".\"foo\" (\"bar\" INTEGER) ON COMMIT PRESERVE ROWS");
        Snowflake().VerifiedStmt("CREATE TABLE \"AAA\".\"foo\" (\"bar\" INTEGER) ON COMMIT DELETE ROWS");
        Snowflake().VerifiedStmt("CREATE TABLE \"AAA\".\"foo\" (\"bar\" INTEGER) ON COMMIT DROP");
    }

    [Fact]
    public void test_snowflake_create_local_table()
    {
        switch (Snowflake().VerifiedStmt("CREATE TABLE my_table (a INT)"))
        {
            case Statement.CreateTable { Value: { Name: var name, Global: var global } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Null(global);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        switch (Snowflake().VerifiedStmt("CREATE LOCAL TABLE my_table (a INT)"))
        {
            case Statement.CreateTable { Value: { Name: var name, Global: var global } }:
                Assert.Equal("my_table", name.ToString());
                Assert.False(global); // assert_eq!(Some(false), global)
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_global_table()
    {
        switch (Snowflake().VerifiedStmt("CREATE GLOBAL TABLE my_table (a INT)"))
        {
            case Statement.CreateTable { Value: { Name: var name, Global: var global } }:
                Assert.Equal("my_table", name.ToString());
                Assert.True(global); // assert_eq!(Some(true), global)
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_invalid_local_global_table()
    {
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("CREATE LOCAL GLOBAL TABLE my_table (a INT)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: an SQL statement, found: LOCAL"));

        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("CREATE GLOBAL LOCAL TABLE my_table (a INT)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: an SQL statement, found: GLOBAL"));
    }

    [Fact]
    public void test_snowflake_create_invalid_temporal_table()
    {
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("CREATE TEMP TEMPORARY TABLE my_table (a INT)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: an object type after CREATE, found: TEMPORARY"));

        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("CREATE TEMP TRANSIENT TABLE my_table (a INT)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: an object type after CREATE, found: TRANSIENT"));
    }

    [Fact]
    public void test_snowflake_create_invalid_temporal_file_format()
    {
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("CREATE TEMPORARY VOLATILE FILE FORMAT my_fmt")).Error,
            new ParserError.ParserErrorValue(
                "Expected: an object type after CREATE, found: FILE"));
    }

    [Fact]
    public void test_snowflake_create_table_if_not_exists()
    {
        switch (Snowflake().VerifiedStmt("CREATE TABLE IF NOT EXISTS my_table (a INT)"))
        {
            case Statement.CreateTable { Value: { Name: var name, IfNotExists: var ifNotExists } }:
                Assert.Equal("my_table", name.ToString());
                Assert.True(ifNotExists);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        foreach (var (sql, parseTo) in new[]
        {
            (
                "CREATE TABLE IF NOT EXISTS \"A\".\"B\".\"C\" (v VARIANT)",
                "CREATE TABLE IF NOT EXISTS \"A\".\"B\".\"C\" (v VARIANT)"
            ),
            (
                "CREATE TABLE \"A\".\"B\".\"C\" IF NOT EXISTS (v VARIANT)",
                "CREATE TABLE IF NOT EXISTS \"A\".\"B\".\"C\" (v VARIANT)"
            ),
            (
                "CREATE TRANSIENT TABLE IF NOT EXISTS \"A\".\"B\".\"C\" (v VARIANT)",
                "CREATE TRANSIENT TABLE IF NOT EXISTS \"A\".\"B\".\"C\" (v VARIANT)"
            ),
            (
                "CREATE TRANSIENT TABLE \"A\".\"B\".\"C\" IF NOT EXISTS (v VARIANT)",
                "CREATE TRANSIENT TABLE IF NOT EXISTS \"A\".\"B\".\"C\" (v VARIANT)"
            ),
        })
        {
            Snowflake().OneStatementParsesTo(sql, parseTo);
        }
    }

    [Fact]
    public void test_snowflake_create_table_cluster_by()
    {
        switch (Snowflake().VerifiedStmt("CREATE TABLE my_table (a INT) CLUSTER BY (a, b, my_func(c))"))
        {
            case Statement.CreateTable { Value: { Name: var name, ClusterBy: var clusterBy } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal<WrappedCollection<Sequence<Expr>>?>(
                    new WrappedCollection<Sequence<Expr>>.Parentheses(Sequence.Of<Expr>(
                        new Expr.Identifier(new Ident("a")),
                        new Expr.Identifier(new Ident("b")),
                        new Expr.Function(new Function(
                            Name: ObjectName.From(Sequence.Of(new Ident("my_func"))),
                            UsesOdbcSyntax: false,
                            Parameters: new FunctionArguments.None(),
                            Args: new FunctionArguments.List(new FunctionArgumentList(
                                Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                                    new Expr.Identifier(new Ident("c"))))),
                                DuplicateTreatment: null,
                                Clauses: Sequence<FunctionArgumentClause>.Empty)),
                            Filter: null,
                            NullTreatment: null,
                            Over: null,
                            WithinGroup: Sequence<OrderByExpr>.Empty)))),
                    clusterBy);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_comment()
    {
        switch (Snowflake().VerifiedStmt("CREATE TABLE my_table (a INT) COMMENT = 'some comment'"))
        {
            case Statement.CreateTable { Value: { Name: var name, TableOptions: var tableOptions } }:
                Assert.Equal("my_table", name.ToString());
                var plainOptions = tableOptions switch
                {
                    CreateTableOptions.Plain { Value: var options } => options,
                    _ => throw new InvalidOperationException("unreachable"),
                };
                var comment = (plainOptions.First ?? throw new InvalidOperationException(UnwrapNone)) switch
                {
                    SqlOption.Comment { Value: CommentDef.WithEq { Value: var c } } => c,
                    SqlOption.Comment { Value: CommentDef.WithoutEq { Value: var c } } => c,
                    _ => throw new InvalidOperationException("unreachable"),
                };
                Assert.Equal("some comment", comment);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_single_line_tokenize()
    {
        var sql = "CREATE TABLE# this is a comment \ntable_1";
        var dialect = new SnowflakeDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("CREATE"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("TABLE"),
            new Token.Whitespace(new Whitespace.SingleLineComment(
                Prefix: "#",
                Comment: " this is a comment ")),
            new Token.Whitespace(new Whitespace.Newline()),
            Token.MakeWord("table_1", null),
        };

        Assert.Equal(expected, tokens);

        sql = "CREATE TABLE // this is a comment \ntable_1";
        tokens = new Tokenizer(dialect, sql).Tokenize();

        expected = new List<Token>
        {
            Token.MakeKeyword("CREATE"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("TABLE"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Whitespace(new Whitespace.SingleLineComment(
                Prefix: "//",
                Comment: " this is a comment ")),
            new Token.Whitespace(new Whitespace.Newline()),
            Token.MakeWord("table_1", null),
        };

        Assert.Equal(expected, tokens);
    }

    [Fact]
    public void test_snowflake_create_table_with_autoincrement_columns()
    {
        var sql =
            "CREATE TABLE my_table (" +
            "a INT AUTOINCREMENT ORDER, " +
            "b INT AUTOINCREMENT(100, 1) NOORDER, " +
            "c INT IDENTITY, " +
            "d INT IDENTITY START 100 INCREMENT 1 ORDER" +
            ")";
        // it is a snowflake specific options (AUTOINCREMENT/IDENTITY)
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Columns: var columns } }:
                Assert.Equal(
                    columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: Ident.From("a"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Identity(new IdentityPropertyKind.Autoincrement(
                                    new IdentityProperty(
                                        Parameters: null,
                                        Order: new IdentityPropertyOrder.Order())))))),
                        new ColumnDef(
                            Name: Ident.From("b"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Identity(new IdentityPropertyKind.Autoincrement(
                                    new IdentityProperty(
                                        Parameters: new IdentityPropertyFormatKind.FunctionCall(
                                            new IdentityParameters(
                                                Seed: Expr.MakeValue(TestUtils.Number("100")),
                                                Increment: Expr.MakeValue(TestUtils.Number("1")))),
                                        Order: new IdentityPropertyOrder.NoOrder())))))),
                        new ColumnDef(
                            Name: Ident.From("c"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Identity(new IdentityPropertyKind.Identity(
                                    new IdentityProperty(
                                        Parameters: null,
                                        Order: null)))))),
                        new ColumnDef(
                            Name: Ident.From("d"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Identity(new IdentityPropertyKind.Identity(
                                    new IdentityProperty(
                                        Parameters:
                                            new IdentityPropertyFormatKind.StartAndIncrement(
                                                new IdentityParameters(
                                                    Seed: new Expr.Value(
                                                        TestUtils.Number("100").WithEmptySpan()),
                                                    Increment: new Expr.Value(
                                                        TestUtils.Number("1").WithEmptySpan()))),
                                        Order: new IdentityPropertyOrder.Order()))))))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_with_collated_column()
    {
        switch (SnowflakeAndGeneric().VerifiedStmt("CREATE TABLE my_table (a TEXT COLLATE 'de_DE')"))
        {
            case Statement.CreateTable { Value: { Columns: var columns } }:
                Assert.Equal(
                    columns,
                    Sequence.Of(new ColumnDef(
                        Name: Ident.From("a"),
                        DataType: new DataType.Text(),
                        Options: Sequence.Of(new ColumnOptionDef(
                            Name: null,
                            Option: new ColumnOption.Collation(ObjectName.From(Sequence.Of(Ident.WithQuote(
                                '\'', "de_DE")))))))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_table_with_columns_masking_policy()
    {
        foreach (var (sql, @with, usingColumns) in new (string, bool, Sequence<Ident>?)[]
        {
            (
                "CREATE TABLE my_table (a INT WITH MASKING POLICY p)",
                true,
                null
            ),
            (
                "CREATE TABLE my_table (a INT MASKING POLICY p)",
                false,
                null
            ),
            (
                "CREATE TABLE my_table (a INT WITH MASKING POLICY p USING (a, b))",
                true,
                Sequence.Of(Ident.From("a"), Ident.From("b"))
            ),
            (
                "CREATE TABLE my_table (a INT MASKING POLICY p USING (a, b))",
                false,
                Sequence.Of(Ident.From("a"), Ident.From("b"))
            ),
        })
        {
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.CreateTable { Value: { Columns: var columns } }:
                    Assert.Equal(
                        columns,
                        Sequence.Of(new ColumnDef(
                            Name: Ident.From("a"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Policy(new ColumnPolicy.MaskingPolicy(
                                    new ColumnPolicyProperty(
                                        With: @with,
                                        PolicyName: ObjectName.From(Sequence.Of(new Ident("p"))),
                                        UsingColumns: usingColumns))))))));
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
    }

    [Fact]
    public void test_snowflake_create_table_with_columns_projection_policy()
    {
        foreach (var (sql, @with) in new[]
        {
            (
                "CREATE TABLE my_table (a INT WITH PROJECTION POLICY p)",
                true
            ),
            ("CREATE TABLE my_table (a INT PROJECTION POLICY p)", false),
        })
        {
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.CreateTable { Value: { Columns: var columns } }:
                    Assert.Equal(
                        columns,
                        Sequence.Of(new ColumnDef(
                            Name: Ident.From("a"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Policy(new ColumnPolicy.ProjectionPolicy(
                                    new ColumnPolicyProperty(
                                        With: @with,
                                        PolicyName: ObjectName.From(Sequence.Of(new Ident("p"))),
                                        UsingColumns: null))))))));
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
    }

    [Fact]
    public void test_snowflake_create_table_with_columns_tags()
    {
        foreach (var (sql, @with) in new[]
        {
            (
                "CREATE TABLE my_table (a INT WITH TAG (A='TAG A', B='TAG B'))",
                true
            ),
            (
                "CREATE TABLE my_table (a INT TAG (A='TAG A', B='TAG B'))",
                false
            ),
        })
        {
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.CreateTable { Value: { Columns: var columns } }:
                    Assert.Equal(
                        columns,
                        Sequence.Of(new ColumnDef(
                            Name: Ident.From("a"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Tags(new TagsColumnOption(
                                    With: @with,
                                    Tags: Sequence.Of(
                                        new Tag(
                                            ObjectName.From(Sequence.Of(new Ident("A"))),
                                            "TAG A"),
                                        new Tag(
                                            ObjectName.From(Sequence.Of(new Ident("B"))),
                                            "TAG B")))))))));
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
    }

    [Fact]
    public void test_snowflake_create_table_with_several_column_options()
    {
        var sql =
            "CREATE TABLE my_table (" +
            "a INT IDENTITY WITH MASKING POLICY p1 USING (a, b) WITH TAG (A='TAG A', B='TAG B'), " +
            "b TEXT COLLATE 'de_DE' PROJECTION POLICY p2 TAG (C='TAG C', D='TAG D')" +
            ")";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Columns: var columns } }:
                Assert.Equal(
                    columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: Ident.From("a"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Identity(new IdentityPropertyKind.Identity(
                                        new IdentityProperty(
                                            Parameters: null,
                                            Order: null)))),
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Policy(new ColumnPolicy.MaskingPolicy(
                                        new ColumnPolicyProperty(
                                            With: true,
                                            PolicyName: ObjectName.From(Sequence.Of(new Ident("p1"))),
                                            UsingColumns: Sequence.Of(Ident.From("a"), Ident.From("b")))))),
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Tags(new TagsColumnOption(
                                        With: true,
                                        Tags: Sequence.Of(
                                            new Tag(
                                                ObjectName.From(Sequence.Of(new Ident("A"))),
                                                "TAG A"),
                                            new Tag(
                                                ObjectName.From(Sequence.Of(new Ident("B"))),
                                                "TAG B"))))))),
                        new ColumnDef(
                            Name: Ident.From("b"),
                            DataType: new DataType.Text(),
                            Options: Sequence.Of(
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Collation(ObjectName.From(Sequence.Of(
                                        Ident.WithQuote('\'', "de_DE"))))),
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Policy(new ColumnPolicy.ProjectionPolicy(
                                        new ColumnPolicyProperty(
                                            With: false,
                                            PolicyName: ObjectName.From(Sequence.Of(new Ident("p2"))),
                                            UsingColumns: null)))),
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Tags(new TagsColumnOption(
                                        With: false,
                                        Tags: Sequence.Of(
                                            new Tag(
                                                ObjectName.From(Sequence.Of(new Ident("C"))),
                                                "TAG C"),
                                            new Tag(
                                                ObjectName.From(Sequence.Of(new Ident("D"))),
                                                "TAG D")))))))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_iceberg_table_all_options()
    {
        switch (Snowflake().VerifiedStmt("CREATE ICEBERG TABLE my_table (a INT, b INT) " +
            "CLUSTER BY (a, b) EXTERNAL_VOLUME='volume' CATALOG='SNOWFLAKE' BASE_LOCATION='relative/path' CATALOG_SYNC='OPEN_CATALOG' " +
            "STORAGE_SERIALIZATION_POLICY=COMPATIBLE COPY GRANTS CHANGE_TRACKING=TRUE DATA_RETENTION_TIME_IN_DAYS=5 MAX_DATA_EXTENSION_TIME_IN_DAYS=10 " +
            "WITH AGGREGATION POLICY policy_name WITH ROW ACCESS POLICY policy_name ON (a) WITH TAG (A='TAG A', B='TAG B')"))
        {
            case Statement.CreateTable { Value: var createTable }:
            {
                var name = createTable.Name;
                var clusterBy = createTable.ClusterBy;
                var baseLocation = createTable.BaseLocation;
                var externalVolume = createTable.ExternalVolume;
                var catalog = createTable.Catalog;
                var catalogSync = createTable.CatalogSync;
                var storageSerializationPolicy = createTable.StorageSerializationPolicy;
                var changeTracking = createTable.ChangeTracking;
                var copyGrants = createTable.CopyGrants;
                var dataRetentionTimeInDays = createTable.DataRetentionTimeInDays;
                var maxDataExtensionTimeInDays = createTable.MaxDataExtensionTimeInDays;
                var withAggregationPolicy = createTable.WithAggregationPolicy;
                var withRowAccessPolicy = createTable.WithRowAccessPolicy;
                var withTags = createTable.WithTags;

                Assert.Equal("my_table", name.ToString());
                Assert.Equal<WrappedCollection<Sequence<Expr>>?>(
                    new WrappedCollection<Sequence<Expr>>.Parentheses(Sequence.Of<Expr>(
                        new Expr.Identifier(new Ident("a")),
                        new Expr.Identifier(new Ident("b")))),
                    clusterBy);
                Assert.Equal("relative/path", baseLocation ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal("volume", externalVolume ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal("SNOWFLAKE", catalog ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal("OPEN_CATALOG", catalogSync ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal<StorageSerializationPolicy>(
                    new StorageSerializationPolicy.Compatible(),
                    storageSerializationPolicy ?? throw new InvalidOperationException(UnwrapNone));
                Assert.True(changeTracking ?? throw new InvalidOperationException(UnwrapNone));
                Assert.True(copyGrants);
                Assert.Equal<ulong?>(5, dataRetentionTimeInDays);
                Assert.Equal<ulong?>(10, maxDataExtensionTimeInDays);
                Assert.Equal(
                    "WITH ROW ACCESS POLICY policy_name ON (a)",
                    withRowAccessPolicy?.ToString());
                Assert.Equal(
                    "policy_name",
                    withAggregationPolicy?.ToString());
                Assert.Equal(
                    (Sequence<Tag>?)Sequence.Of(
                        new Tag(ObjectName.From(Sequence.Of(new Ident("A"))), "TAG A"),
                        new Tag(ObjectName.From(Sequence.Of(new Ident("B"))), "TAG B")),
                    withTags);
                break;
            }
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_iceberg_table()
    {
        switch (Snowflake()
            .VerifiedStmt("CREATE ICEBERG TABLE my_table (a INT) BASE_LOCATION='relative_path'"))
        {
            case Statement.CreateTable { Value: { Name: var name, BaseLocation: var baseLocation } }:
                Assert.Equal("my_table", name.ToString());
                Assert.Equal("relative_path", baseLocation ?? throw new InvalidOperationException(UnwrapNone));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_snowflake_create_iceberg_table_without_location()
    {
        var res = Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("CREATE ICEBERG TABLE my_table (a INT)"));
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("BASE_LOCATION is required for ICEBERG tables"),
            res.Error);
    }

    [Fact]
    public void test_snowflake_create_table_trailing_options()
    {
        // Serialization to SQL assume that in `CREATE TABLE AS` the options come before the `AS (<query>)`
        // but Snowflake supports also the other way around
        Snowflake()
            .VerifiedStmt("CREATE TEMPORARY TABLE dst ON COMMIT PRESERVE ROWS AS (SELECT * FROM src)");
        _ = Snowflake()
            .ParseSqlStatements(
                "CREATE TEMPORARY TABLE dst AS (SELECT * FROM src) ON COMMIT PRESERVE ROWS");

        // Same for `CREATE TABLE LIKE|CLONE`:
        Snowflake().VerifiedStmt("CREATE TEMPORARY TABLE dst LIKE src ON COMMIT PRESERVE ROWS");
        _ = Snowflake()
            .ParseSqlStatements("CREATE TEMPORARY TABLE dst ON COMMIT PRESERVE ROWS LIKE src");

        Snowflake().VerifiedStmt("CREATE TEMPORARY TABLE dst CLONE src ON COMMIT PRESERVE ROWS");
        _ = Snowflake()
            .ParseSqlStatements("CREATE TEMPORARY TABLE dst ON COMMIT PRESERVE ROWS CLONE src");
    }

    [Fact]
    public void parse_sf_create_or_replace_view_with_comment_missing_equal()
    {
        // assert!(..is_ok()): se lançar, o teste falha
        _ = SnowflakeAndGeneric()
            .ParseSqlStatements("CREATE OR REPLACE VIEW v COMMENT = 'hello, world' AS SELECT 1");

        Assert.Throws<ParserException>(() => SnowflakeAndGeneric()
            .ParseSqlStatements("CREATE OR REPLACE VIEW v COMMENT 'hello, world' AS SELECT 1"));
    }

    [Fact]
    public void parse_sf_create_or_replace_with_comment_for_snowflake()
    {
        var sql = "CREATE OR REPLACE VIEW v COMMENT = 'hello, world' AS SELECT 1";
        var dialect =
            new TestedDialects(new List<Dialect> { new SnowflakeDialect() });

        switch (dialect.VerifiedStmt(sql))
        {
            case Statement.CreateView { Value: var createView }:
            {
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

                Assert.Equal("v", name.ToString());
                Assert.Equal(columns, Sequence<ViewColumnDef>.Empty);
                Assert.Equal<CreateTableOptions>(options, new CreateTableOptions.None());
                Assert.Equal("SELECT 1", query.ToString());
                Assert.False(materialized);
                Assert.True(orReplace);
                Assert.Equal(clusterBy, Sequence<Ident>.Empty);
                Assert.NotNull(comment);
                Assert.Equal("hello, world", comment ?? throw new InvalidOperationException("expected comment"));
                Assert.False(lateBinding);
                Assert.False(ifNotExists);
                Assert.False(temporary);
                break;
            }
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_sf_create_table_or_view_with_dollar_quoted_comment()
    {
        // Snowflake transforms dollar quoted comments into a common comment in DDL representation of creation
        Snowflake()
            .OneStatementParsesTo(
                "CREATE OR REPLACE TEMPORARY VIEW foo.bar.baz (\"COL_1\" COMMENT $$comment 1$$) COMMENT = $$view comment$$ AS (SELECT 1)",
                "CREATE OR REPLACE TEMPORARY VIEW foo.bar.baz (\"COL_1\" COMMENT 'comment 1') COMMENT = 'view comment' AS (SELECT 1)");

        Snowflake().OneStatementParsesTo(
            "CREATE TABLE my_table (a STRING COMMENT $$comment 1$$) COMMENT = $$table comment$$",
            "CREATE TABLE my_table (a STRING COMMENT 'comment 1') COMMENT = 'table comment'");
    }

    [Fact]
    public void parse_create_dynamic_table()
    {
        Snowflake().VerifiedStmt("CREATE OR REPLACE DYNAMIC TABLE my_dynamic_table TARGET_LAG='20 minutes' WAREHOUSE=mywh AS SELECT product_id, product_name FROM staging_table");
        Snowflake().VerifiedStmt(
            "CREATE DYNAMIC ICEBERG TABLE my_dynamic_table (date TIMESTAMP_NTZ, id NUMBER, content STRING)" +
            " EXTERNAL_VOLUME='my_external_volume'" +
            " CATALOG='SNOWFLAKE'" +
            " BASE_LOCATION='my_iceberg_table'" +
            " TARGET_LAG='20 minutes'" +
            " WAREHOUSE=mywh" +
            " AS SELECT product_id, product_name FROM staging_table");

        Snowflake().VerifiedStmt(
            "CREATE DYNAMIC TABLE my_dynamic_table (date TIMESTAMP_NTZ, id NUMBER, content VARIANT)" +
            " CLUSTER BY (date, id)" +
            " TARGET_LAG='20 minutes'" +
            " WAREHOUSE=mywh" +
            " AS SELECT product_id, product_name FROM staging_table");

        Snowflake().VerifiedStmt(
            "CREATE DYNAMIC TABLE my_cloned_dynamic_table" +
            " CLONE my_dynamic_table" +
            " AT(TIMESTAMP => TO_TIMESTAMP_TZ('04/05/2013 01:02:03', 'mm/dd/yyyy hh24:mi:ss'))");

        Snowflake().VerifiedStmt(
            "CREATE DYNAMIC TABLE my_cloned_dynamic_table" +
            " CLONE my_dynamic_table" +
            " BEFORE(OFFSET => TO_TIMESTAMP_TZ('04/05/2013 01:02:03', 'mm/dd/yyyy hh24:mi:ss'))");

        Snowflake().VerifiedStmt(
            "CREATE DYNAMIC TABLE my_dynamic_table" +
            " TARGET_LAG='DOWNSTREAM'" +
            " WAREHOUSE=mywh" +
            " INITIALIZE=ON_SCHEDULE" +
            " REQUIRE USER" +
            " AS SELECT product_id, product_name FROM staging_table");

        Snowflake().VerifiedStmt(
            "CREATE DYNAMIC TABLE my_dynamic_table" +
            " TARGET_LAG='DOWNSTREAM'" +
            " WAREHOUSE=mywh" +
            " REFRESH_MODE=AUTO" +
            " INITIALIZE=ON_SCHEDULE" +
            " REQUIRE USER" +
            " AS SELECT product_id, product_name FROM staging_table");

        Snowflake().VerifiedStmt(
            "CREATE DYNAMIC TABLE my_dynamic_table" +
            " TARGET_LAG='DOWNSTREAM'" +
            " WAREHOUSE=mywh" +
            " REFRESH_MODE=FULL" +
            " INITIALIZE=ON_SCHEDULE" +
            " REQUIRE USER" +
            " AS SELECT product_id, product_name FROM staging_table");

        Snowflake().VerifiedStmt(
            "CREATE DYNAMIC TABLE my_dynamic_table" +
            " TARGET_LAG='DOWNSTREAM'" +
            " WAREHOUSE=mywh" +
            " REFRESH_MODE=INCREMENTAL" +
            " INITIALIZE=ON_SCHEDULE" +
            " REQUIRE USER" +
            " AS SELECT product_id, product_name FROM staging_table");
    }

    [Fact]
    public void test_sf_derived_table_in_parenthesis()
    {
        // Nesting a subquery in an extra set of parentheses is non-standard,
        // but supported in Snowflake SQL
        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM ((SELECT 1) AS t)",
            "SELECT * FROM (SELECT 1) AS t");
        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (((SELECT 1) AS t))",
            "SELECT * FROM (SELECT 1) AS t");
    }

    [Fact]
    public void test_single_table_in_parenthesis()
    {
        // Parenthesized table names are non-standard, but supported in Snowflake SQL
        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a NATURAL JOIN (b))",
            "SELECT * FROM (a NATURAL JOIN b)");
        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a NATURAL JOIN ((b)))",
            "SELECT * FROM (a NATURAL JOIN b)");
    }

    [Fact]
    public void test_single_table_in_parenthesis_with_alias()
    {
        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a NATURAL JOIN (b) c )",
            "SELECT * FROM (a NATURAL JOIN b c)");

        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a NATURAL JOIN ((b)) c )",
            "SELECT * FROM (a NATURAL JOIN b c)");

        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a NATURAL JOIN ( (b) c ) )",
            "SELECT * FROM (a NATURAL JOIN b c)");

        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a NATURAL JOIN ( (b) as c ) )",
            "SELECT * FROM (a NATURAL JOIN b AS c)");

        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a as alias1 NATURAL JOIN ( (b) c ) )",
            "SELECT * FROM (a AS alias1 NATURAL JOIN b c)");
        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a alias1 NATURAL JOIN ( (b) c ) )",
            "SELECT * FROM (a alias1 NATURAL JOIN b c)");

        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a as alias1 NATURAL JOIN ( (b) as c ) )",
            "SELECT * FROM (a AS alias1 NATURAL JOIN b AS c)");

        SnowflakeAndGeneric().OneStatementParsesTo(
            "SELECT * FROM (a NATURAL JOIN b) AS c",
            "SELECT * FROM (a NATURAL JOIN b) AS c");

        var res = Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements("SELECT * FROM (a b) c"));
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("duplicate alias b"),
            res.Error);
    }

    [Fact]
    public void parse_array()
    {
        var sql = "SELECT CAST(a AS ARRAY) FROM customer";
        var select = Snowflake().VerifiedOnlySelect(sql);
        Assert.Equal<Expr>(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("a")),
                DataType: new DataType.Array(new ArrayElemTypeDef.None()),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_lateral_flatten()
    {
        Snowflake().VerifiedOnlySelect("SELECT * FROM TABLE(FLATTEN(input => parse_json('{\"a\":1, \"b\":[77,88]}'), outer => true)) AS f");
        Snowflake().VerifiedOnlySelect("SELECT emp.employee_ID, emp.last_name, index, value AS project_name FROM employees AS emp, LATERAL FLATTEN(INPUT => emp.project_names) AS proj_names");
    }

    // https://docs.snowflake.com/en/user-guide/querying-semistructured
    [Fact]
    public void parse_semi_structured_data_traversal()
    {
        // see `tests/sqlparser_common.rs` -> `parse_semi_structured_data_traversal` for more test
        // cases. This test only has Snowflake-specific syntax like array access.

        // expressions are allowed in bracket notation
        var sql = "SELECT a[2 + 2] FROM t";
        var select = Snowflake().VerifiedOnlySelect(sql);
        Assert.Equal<SelectItem>(
            new SelectItem.UnnamedExpr(new Expr.JsonAccess(
                Value: new Expr.Identifier(new Ident("a")),
                Path: new JsonPath(Sequence.Of<JsonPathElem>(new JsonPathElem.Bracket(
                    Key: new Expr.BinaryOp(
                        Left: Expr.MakeValue(TestUtils.Number("2")),
                        Op: new BinaryOperator.Plus(),
                        Right: Expr.MakeValue(TestUtils.Number("2")))))))),
            select.Projection[0]);

        // dot and bracket notation can be mixed (starting with bracket case)
        // https://docs.snowflake.com/en/user-guide/querying-semistructured#dot-notation
        sql = "SELECT a[0].foo.bar";
        select = Snowflake().VerifiedOnlySelect(sql);
        Assert.Equal(
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.JsonAccess(
                Value: new Expr.Identifier(new Ident("a")),
                Path: new JsonPath(Sequence.Of<JsonPathElem>(
                    new JsonPathElem.Bracket(
                        Key: Expr.MakeValue(TestUtils.Number("0"))),
                    new JsonPathElem.Dot(
                        Key: "foo",
                        Quoted: false),
                    new JsonPathElem.Dot(
                        Key: "bar",
                        Quoted: false)))))),
            select.Projection);

        // a json access used as a key to another json access
        Assert.Equal<Expr>(
            Snowflake().VerifiedExpr("a[b:c]"),
            new Expr.JsonAccess(
                Value: new Expr.Identifier(new Ident("a")),
                Path: new JsonPath(Sequence.Of<JsonPathElem>(new JsonPathElem.Bracket(
                    Key: new Expr.JsonAccess(
                        Value: new Expr.Identifier(new Ident("b")),
                        Path: new JsonPath(Sequence.Of<JsonPathElem>(new JsonPathElem.Dot(
                            Key: "c",
                            Quoted: false)))))))));

        // unquoted object keys cannot start with a digit
        Assert.Equal(
            "sql parser error: Expected: variant object key name, found: 42",
            Assert.Throws<ParserException>(() => Snowflake()
                .ParseSqlStatements("SELECT a:42"))
                .Error
                .ToString());

        // casting a json access and accessing an array element
        Assert.Equal<Expr>(
            Snowflake().VerifiedExpr("a:b::ARRAY[1]"),
            new Expr.JsonAccess(
                Value: new Expr.Cast(
                    Kind: CastKind.DoubleColon,
                    Expr: new Expr.JsonAccess(
                        Value: new Expr.Identifier(new Ident("a")),
                        Path: new JsonPath(Sequence.Of<JsonPathElem>(new JsonPathElem.Dot(
                            Key: "b",
                            Quoted: false)))),
                    DataType: new DataType.Array(new ArrayElemTypeDef.None()),
                    Format: null),
                Path: new JsonPath(Sequence.Of<JsonPathElem>(new JsonPathElem.Bracket(
                    Key: Expr.MakeValue(TestUtils.Number("1")))))));
    }

    [Fact]
    public void parse_delimited_identifiers()
    {
        // check that quoted identifiers in any position remain quoted after serialization
        var select = Snowflake().VerifiedOnlySelect(
            "SELECT \"alias\".\"bar baz\", \"myfun\"(), \"simple id\" AS \"column alias\" FROM \"a table\" AS \"alias\"");
        // check FROM
        switch (TestUtils.Only(select.From).Relation)
        {
            case TableFactor.Table
            {
                Name: var name,
                Alias: var alias,
                Args: var args,
                WithHints: var withHints,
                Version: var version,
            }:
                Assert.Equal(
                    ObjectName.From(Sequence.Of(Ident.WithQuote('"', "a table"))),
                    name);
                Assert.Equal(Ident.WithQuote('"', "alias"), (alias ?? throw new InvalidOperationException(UnwrapNone)).Name);
                Assert.Null(args);
                Assert.True(withHints.IsEmpty);
                Assert.Null(version);
                break;
            default:
                Assert.Fail("Expecting TableFactor::Table");
                break;
        }
        // check SELECT
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal<Expr>(
            new Expr.CompoundIdentifier(Sequence.Of(
                Ident.WithQuote('"', "alias"),
                Ident.WithQuote('"', "bar baz"))),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal<Expr>(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(Ident.WithQuote('"', "myfun"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: null,
                    Args: Sequence<FunctionArg>.Empty,
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                Filter: null,
                NullTreatment: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(select.Projection[1]));
        switch (select.Projection[2])
        {
            case SelectItem.ExprWithAlias { Expr: var expr, Alias: var alias }:
                Assert.Equal<Expr>(new Expr.Identifier(Ident.WithQuote('"', "simple id")), expr);
                Assert.Equal(Ident.WithQuote('"', "column alias"), alias);
                break;
            default:
                Assert.Fail("Expected: ExprWithAlias");
                break;
        }

        Snowflake().VerifiedStmt("CREATE TABLE \"foo\" (\"bar\" \"int\")");
        Snowflake().VerifiedStmt("ALTER TABLE foo ADD CONSTRAINT \"bar\" PRIMARY KEY (baz)");
        //TODO verified_stmt(r#"UPDATE foo SET "bar" = 5"#);
    }

    [Fact]
    public void test_array_agg_func()
    {
        foreach (var sql in new[]
        {
            "SELECT ARRAY_AGG(x) WITHIN GROUP (ORDER BY x) AS a FROM T",
            "SELECT ARRAY_AGG(DISTINCT x) WITHIN GROUP (ORDER BY x ASC) FROM tbl",
        })
        {
            Snowflake().VerifiedStmt(sql);
        }
    }

    // As funções-atalho `snowflake()`, `snowflake_with_recursion_limit()`, `snowflake_without_unescape()` e
    // `snowflake_and_generic()` (linhas 1480–1500 do Rust) estão no topo da classe.

    [Fact]
    public void test_select_wildcard_with_exclude()
    {
        var select = SnowflakeAndGeneric().VerifiedOnlySelect("SELECT * EXCLUDE (col_a) FROM data");
        SelectItem expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptExclude = new ExcludeSelectItem.Multiple(Sequence.Of(ObjectName.From(
                new Ident("col_a")))),
        });
        Assert.Equal(expected, select.Projection[0]);

        select = SnowflakeAndGeneric()
            .VerifiedOnlySelect("SELECT name.* EXCLUDE department_id FROM employee_table");
        expected = new SelectItem.QualifiedWildcard(
            new SelectItemQualifiedWildcardKind.ObjectName(ObjectName.From(Sequence.Of(new Ident("name")))),
            WildcardAdditionalOptions.Default() with
            {
                OptExclude = new ExcludeSelectItem.Single(ObjectName.From(new Ident(
                    "department_id"))),
            });
        Assert.Equal(expected, select.Projection[0]);

        select = SnowflakeAndGeneric()
            .VerifiedOnlySelect("SELECT * EXCLUDE (department_id, employee_id) FROM employee_table");
        expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptExclude = new ExcludeSelectItem.Multiple(Sequence.Of(
                ObjectName.From(new Ident("department_id")),
                ObjectName.From(new Ident("employee_id")))),
        });
        Assert.Equal(expected, select.Projection[0]);
    }

    [Fact]
    public void test_select_wildcard_with_rename()
    {
        var select =
            SnowflakeAndGeneric().VerifiedOnlySelect("SELECT * RENAME col_a AS col_b FROM data");
        SelectItem expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptRename = new RenameSelectItem.Single(new IdentWithAlias(
                Ident: new Ident("col_a"),
                Alias: new Ident("col_b"))),
        });
        Assert.Equal(expected, select.Projection[0]);

        select = SnowflakeAndGeneric().VerifiedOnlySelect(
            "SELECT name.* RENAME (department_id AS new_dep, employee_id AS new_emp) FROM employee_table");
        expected = new SelectItem.QualifiedWildcard(
            new SelectItemQualifiedWildcardKind.ObjectName(ObjectName.From(Sequence.Of(new Ident("name")))),
            WildcardAdditionalOptions.Default() with
            {
                OptRename = new RenameSelectItem.Multiple(Sequence.Of(
                    new IdentWithAlias(
                        Ident: new Ident("department_id"),
                        Alias: new Ident("new_dep")),
                    new IdentWithAlias(
                        Ident: new Ident("employee_id"),
                        Alias: new Ident("new_emp")))),
            });
        Assert.Equal(expected, select.Projection[0]);
    }

    [Fact]
    public void test_select_wildcard_with_replace_and_rename()
    {
        var select = SnowflakeAndGeneric().VerifiedOnlySelect(
            "SELECT * REPLACE (col_z || col_z AS col_z) RENAME (col_z AS col_zz) FROM data");
        SelectItem expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptReplace = new ReplaceSelectItem(
                Items: Sequence.Of(new ReplaceSelectElement(
                    Expr: new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("col_z")),
                        Op: new BinaryOperator.StringConcat(),
                        Right: new Expr.Identifier(new Ident("col_z"))),
                    ColumnName: new Ident("col_z"),
                    AsKeyword: true))),
            OptRename = new RenameSelectItem.Multiple(Sequence.Of(new IdentWithAlias(
                Ident: new Ident("col_z"),
                Alias: new Ident("col_zz")))),
        });
        Assert.Equal(expected, select.Projection[0]);

        // rename cannot precede replace
        // https://docs.snowflake.com/en/sql-reference/sql/select#parameters
        Assert.Equal(
            "sql parser error: Expected: end of statement, found: REPLACE",
            Assert.Throws<ParserException>(() => SnowflakeAndGeneric()
                .ParseSqlStatements(
                    "SELECT * RENAME (col_z AS col_zz) REPLACE (col_z || col_z AS col_z) FROM data"))
                .Error
                .ToString());
    }

    [Fact]
    public void test_select_wildcard_with_exclude_and_rename()
    {
        var select = SnowflakeAndGeneric()
            .VerifiedOnlySelect("SELECT * EXCLUDE col_z RENAME col_a AS col_b FROM data");
        SelectItem expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptExclude = new ExcludeSelectItem.Single(ObjectName.From(new Ident(
                "col_z"))),
            OptRename = new RenameSelectItem.Single(new IdentWithAlias(
                Ident: new Ident("col_a"),
                Alias: new Ident("col_b"))),
        });
        Assert.Equal(expected, select.Projection[0]);

        // rename cannot precede exclude
        // https://docs.snowflake.com/en/sql-reference/sql/select#parameters
        Assert.Equal(
            "sql parser error: Expected: end of statement, found: EXCLUDE",
            Assert.Throws<ParserException>(() => SnowflakeAndGeneric()
                .ParseSqlStatements("SELECT * RENAME col_a AS col_b EXCLUDE col_z FROM data"))
                .Error
                .ToString());
    }

    [Fact]
    public void test_alter_table_swap_with()
    {
        var sql = "ALTER TABLE tab1 SWAP WITH tab2";
        switch (TestUtils.AlterTableOpWithName(SnowflakeAndGeneric().VerifiedStmt(sql), "tab1"))
        {
            case AlterTableOperation.SwapWith { TableName: var tableName }:
                Assert.Equal("tab2", tableName.ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_alter_table_clustering()
    {
        var sql = "ALTER TABLE tab CLUSTER BY (c1, \"c2\", TO_DATE(c3))";
        switch (TestUtils.AlterTableOp(SnowflakeAndGeneric().VerifiedStmt(sql)))
        {
            case AlterTableOperation.ClusterBy { Exprs: var exprs }:
                Assert.Equal(
                    exprs,
                    Sequence.Of<Expr>(
                        new Expr.Identifier(new Ident("c1")),
                        new Expr.Identifier(Ident.WithQuote('"', "c2")),
                        new Expr.Function(new Function(
                            Name: ObjectName.From(Sequence.Of(new Ident("TO_DATE"))),
                            UsesOdbcSyntax: false,
                            Parameters: new FunctionArguments.None(),
                            Args: new FunctionArguments.List(new FunctionArgumentList(
                                Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                                    new Expr.Identifier(new Ident("c3"))))),
                                DuplicateTreatment: null,
                                Clauses: Sequence<FunctionArgumentClause>.Empty)),
                            Filter: null,
                            NullTreatment: null,
                            Over: null,
                            WithinGroup: Sequence<OrderByExpr>.Empty))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        SnowflakeAndGeneric().VerifiedStmt("ALTER TABLE tbl DROP CLUSTERING KEY");
        SnowflakeAndGeneric().VerifiedStmt("ALTER TABLE tbl SUSPEND RECLUSTER");
        SnowflakeAndGeneric().VerifiedStmt("ALTER TABLE tbl RESUME RECLUSTER");
    }

    [Fact]
    public void test_alter_iceberg_table()
    {
        SnowflakeAndGeneric().VerifiedStmt("ALTER ICEBERG TABLE tbl DROP CLUSTERING KEY");
        SnowflakeAndGeneric().VerifiedStmt("ALTER ICEBERG TABLE tbl SUSPEND RECLUSTER");
        SnowflakeAndGeneric().VerifiedStmt("ALTER ICEBERG TABLE tbl RESUME RECLUSTER");
    }

    [Fact]
    public void test_drop_stage()
    {
        switch (SnowflakeAndGeneric().VerifiedStmt("DROP STAGE s1"))
        {
            case Statement.Drop { Names: var names, IfExists: var ifExists }:
                Assert.False(ifExists);
                Assert.Equal("s1", names[0].ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        switch (SnowflakeAndGeneric().VerifiedStmt("DROP STAGE IF EXISTS s1"))
        {
            case Statement.Drop { Names: var names, IfExists: var ifExists }:
                Assert.True(ifExists);
                Assert.Equal("s1", names[0].ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        SnowflakeAndGeneric().OneStatementParsesTo("DROP STAGE s1", "DROP STAGE s1");

        SnowflakeAndGeneric()
            .OneStatementParsesTo("DROP STAGE IF EXISTS s1", "DROP STAGE IF EXISTS s1");
    }

    [Fact]
    public void parse_snowflake_declare_cursor()
    {
        foreach (var (sql, expectedName, expectedAssignedExpr, expectedQueryProjections) in new (string, string, DeclareAssignment?, List<string>?)[]
        {
            (
                "DECLARE c1 CURSOR FOR SELECT id, price FROM invoices",
                "c1",
                null,
                new List<string> { "id", "price" }
            ),
            (
                "DECLARE c1 CURSOR FOR res",
                "c1",
                new DeclareAssignment.For(
                    new Expr.Identifier(new Ident("res"))),
                null
            ),
        })
        {
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.Declare { Stmts: var stmts }:
                {
                    Assert.Equal(1, stmts.Count);
                    // let Declare { names, data_type, declare_type, assignment: assigned_expr, for_query, .. } = stmts.swap_remove(0);
                    var declare = stmts[0];
                    var names = declare.Names;
                    var dataType = declare.DataType;
                    var declareType = declare.DeclareType;
                    var assignedExpr = declare.Assignment;
                    var forQuery = declare.ForQuery;
                    Assert.Equal(Sequence.Of(new Ident(expectedName)), names);
                    Assert.Null(dataType);
                    Assert.Equal<DeclareType?>(new DeclareType.Cursor(), declareType);
                    Assert.Equal(expectedAssignedExpr, assignedExpr);
                    Assert.Equal(
                        expectedQueryProjections,
                        forQuery is null
                            ? null
                            : forQuery.Body switch
                            {
                                SetExpr.Select { Value: var q } => q
                                    .Projection
                                    .Select(item => item switch
                                    {
                                        SelectItem.UnnamedExpr { Value: Expr.Identifier { Value: var ident } } =>
                                            ident.Value,
                                        _ => throw new InvalidOperationException("unreachable"),
                                    })
                                    .ToList(),
                                _ => throw new InvalidOperationException("unreachable"),
                            });
                    break;
                }
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }

        var errorSql = "DECLARE c1 CURSOR SELECT id FROM invoices";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: FOR, found: SELECT"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);

        errorSql = "DECLARE c1 CURSOR res";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: FOR, found: res"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);
    }

    [Fact]
    public void parse_snowflake_declare_result_set()
    {
        foreach (var (sql, expectedName, expectedAssignedExpr) in new (string, string, DeclareAssignment?)[]
        {
            (
                "DECLARE res RESULTSET DEFAULT 42",
                "res",
                new DeclareAssignment.Default(Expr.MakeValue(TestUtils.Number("42")))
            ),
            (
                "DECLARE res RESULTSET := 42",
                "res",
                new DeclareAssignment.DuckAssignment(
                    Expr.MakeValue(TestUtils.Number("42")))
            ),
            ("DECLARE res RESULTSET", "res", null),
        })
        {
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.Declare { Stmts: var stmts }:
                {
                    Assert.Equal(1, stmts.Count);
                    var declare = stmts[0]; // stmts.swap_remove(0)
                    var names = declare.Names;
                    var dataType = declare.DataType;
                    var declareType = declare.DeclareType;
                    var assignedExpr = declare.Assignment;
                    var forQuery = declare.ForQuery;
                    Assert.Equal(Sequence.Of(new Ident(expectedName)), names);
                    Assert.Null(dataType);
                    Assert.Null(forQuery);
                    Assert.Equal<DeclareType?>(new DeclareType.ResultSet(), declareType);
                    Assert.Equal(expectedAssignedExpr, assignedExpr);
                    break;
                }
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }

        var sql2 = "DECLARE res RESULTSET DEFAULT (SELECT price FROM invoices)";
        Assert.Equal(Snowflake().VerifiedStmt(sql2).ToString(), sql2);

        var errorSql = "DECLARE res RESULTSET DEFAULT";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: an expression, found: EOF"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);

        errorSql = "DECLARE res RESULTSET :=";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: an expression, found: EOF"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);
    }

    [Fact]
    public void parse_snowflake_declare_exception()
    {
        foreach (var (sql, expectedName, expectedAssignedExpr) in new (string, string, DeclareAssignment?)[]
        {
            (
                "DECLARE ex EXCEPTION (42, 'ERROR')",
                "ex",
                new DeclareAssignment.Expr(
                    new Expr.Tuple(Sequence.Of(
                        Expr.MakeValue(TestUtils.Number("42")),
                        new Expr.Value(new Value.SingleQuotedString("ERROR").WithEmptySpan()))))
            ),
            ("DECLARE ex EXCEPTION", "ex", null),
        })
        {
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.Declare { Stmts: var stmts }:
                {
                    Assert.Equal(1, stmts.Count);
                    var declare = stmts[0]; // stmts.swap_remove(0)
                    var names = declare.Names;
                    var dataType = declare.DataType;
                    var declareType = declare.DeclareType;
                    var assignedExpr = declare.Assignment;
                    var forQuery = declare.ForQuery;
                    Assert.Equal(Sequence.Of(new Ident(expectedName)), names);
                    Assert.Null(dataType);
                    Assert.Null(forQuery);
                    Assert.Equal<DeclareType?>(new DeclareType.Exception(), declareType);
                    Assert.Equal(expectedAssignedExpr, assignedExpr);
                    break;
                }
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
    }

    [Fact]
    public void parse_snowflake_declare_variable()
    {
        foreach (var (sql, expectedName, expectedDataType, expectedAssignedExpr) in new (string, string, DataType?, DeclareAssignment?)[]
        {
            (
                "DECLARE profit TEXT DEFAULT 42",
                "profit",
                new DataType.Text(),
                new DeclareAssignment.Default(Expr.MakeValue(TestUtils.Number("42")))
            ),
            (
                "DECLARE profit DEFAULT 42",
                "profit",
                null,
                new DeclareAssignment.Default(Expr.MakeValue(TestUtils.Number("42")))
            ),
            ("DECLARE profit TEXT", "profit", new DataType.Text(), null),
            ("DECLARE profit", "profit", null, null),
        })
        {
            switch (Snowflake().VerifiedStmt(sql))
            {
                case Statement.Declare { Stmts: var stmts }:
                {
                    Assert.Equal(1, stmts.Count);
                    var declare = stmts[0]; // stmts.swap_remove(0)
                    var names = declare.Names;
                    var dataType = declare.DataType;
                    var declareType = declare.DeclareType;
                    var assignedExpr = declare.Assignment;
                    var forQuery = declare.ForQuery;
                    Assert.Equal(Sequence.Of(new Ident(expectedName)), names);
                    Assert.Null(forQuery);
                    Assert.Equal(expectedDataType, dataType);
                    Assert.Null(declareType);
                    Assert.Equal(expectedAssignedExpr, assignedExpr);
                    break;
                }
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }

        Snowflake().OneStatementParsesTo("DECLARE profit;", "DECLARE profit");

        var errorSql = "DECLARE profit INT 2";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: end of statement, found: 2"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);

        errorSql = "DECLARE profit INT DEFAULT";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: an expression, found: EOF"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);

        errorSql = "DECLARE profit DEFAULT";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: an expression, found: EOF"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);
    }

    [Fact]
    public void parse_snowflake_declare_multi_statements()
    {
        var sql =
            "DECLARE profit DEFAULT 42; " +
            "res RESULTSET DEFAULT (SELECT price FROM invoices); " +
            "c1 CURSOR FOR res; " +
            "ex EXCEPTION (-20003, 'ERROR: Could not create table.')";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.Declare { Stmts: var stmts }:
            {
                var actual = stmts
                    .Select(stmt => (stmt.Names[0].Value, stmt.DeclareType))
                    .ToList();

                Assert.Equal(
                    new List<(string, DeclareType?)>
                    {
                        ("profit", null),
                        ("res", new DeclareType.ResultSet()),
                        ("c1", new DeclareType.Cursor()),
                        ("ex", new DeclareType.Exception()),
                    },
                    actual);
                break;
            }
            default:
                throw new InvalidOperationException("unreachable");
        }

        var errorSql = "DECLARE profit DEFAULT 42 c1 CURSOR FOR res;";
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue("Expected: end of statement, found: c1"),
            Assert.Throws<ParserException>(() => Snowflake().ParseSqlStatements(errorSql)).Error);
    }

    [Fact]
    public void test_create_stage()
    {
        var sql = "CREATE STAGE s1.s2";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateStage
            {
                OrReplace: var orReplace,
                Temporary: var temporary,
                IfNotExists: var ifNotExists,
                Name: var name,
                Comment: var comment,
            }:
                Assert.False(orReplace);
                Assert.False(temporary);
                Assert.False(ifNotExists);
                Assert.Equal("s1.s2", name.ToString());
                Assert.Null(comment);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);

        var extendedSql =
            "CREATE OR REPLACE TEMPORARY STAGE IF NOT EXISTS s1.s2 " +
            "COMMENT='some-comment'";
        switch (Snowflake().VerifiedStmt(extendedSql))
        {
            case Statement.CreateStage
            {
                OrReplace: var orReplace,
                Temporary: var temporary,
                IfNotExists: var ifNotExists,
                Name: var name,
                StageParams: var stageParams,
                Comment: var comment,
            }:
                Assert.True(orReplace);
                Assert.True(temporary);
                Assert.True(ifNotExists);
                Assert.Null(stageParams.Url);
                Assert.Null(stageParams.Endpoint);
                Assert.Equal("s1.s2", name.ToString());
                Assert.Equal("some-comment", comment ?? throw new InvalidOperationException(UnwrapNone));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(
            Snowflake().VerifiedStmt(extendedSql).ToString(),
            extendedSql);
    }

    [Fact]
    public void test_create_stage_with_stage_params()
    {
        var sql =
            "CREATE OR REPLACE STAGE my_ext_stage " +
            "URL='s3://load/files/' " +
            "STORAGE_INTEGRATION=myint " +
            "ENDPOINT='<s3_api_compatible_endpoint>' " +
            "CREDENTIALS=(AWS_KEY_ID='1a2b3c' AWS_SECRET_KEY='4x5y6z') " +
            "ENCRYPTION=(MASTER_KEY='key' TYPE='AWS_SSE_KMS')";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateStage { StageParams: var stageParams }:
                Assert.Equal("s3://load/files/", stageParams.Url ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal("myint", stageParams.StorageIntegration ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal(
                    "<s3_api_compatible_endpoint>",
                    stageParams.Endpoint ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "AWS_KEY_ID",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("1a2b3c").WithEmptySpan())),
                    stageParams.Credentials.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "AWS_SECRET_KEY",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("4x5y6z").WithEmptySpan())),
                    stageParams.Credentials.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "MASTER_KEY",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("key").WithEmptySpan())),
                    stageParams.Encryption.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "TYPE",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("AWS_SSE_KMS").WithEmptySpan())),
                    stageParams.Encryption.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_create_stage_with_directory_table_params()
    {
        var sql =
            "CREATE OR REPLACE STAGE my_ext_stage " +
            "URL='s3://load/files/' " +
            "DIRECTORY=(ENABLE=true REFRESH_ON_CREATE=false NOTIFICATION_INTEGRATION='some-string')";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateStage { DirectoryTableParams: var directoryTableParams }:
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "ENABLE",
                        OptionValue: new KeyValueOptionKind.Single(new Value.Boolean(true).WithEmptySpan())),
                    directoryTableParams.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "REFRESH_ON_CREATE",
                        OptionValue: new KeyValueOptionKind.Single(new Value.Boolean(false).WithEmptySpan())),
                    directoryTableParams.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "NOTIFICATION_INTEGRATION",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("some-string").WithEmptySpan())),
                    directoryTableParams.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_create_stage_with_file_format()
    {
        var sql =
            "CREATE OR REPLACE STAGE my_ext_stage " +
            "URL='s3://load/files/' " +
            @"FILE_FORMAT=(COMPRESSION=AUTO BINARY_FORMAT=HEX ESCAPE='\\')";

        switch (SnowflakeWithoutUnescape().VerifiedStmt(sql))
        {
            case Statement.CreateStage { FileFormat: var fileFormat }:
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
    }

    [Fact]
    public void test_create_stage_with_copy_options()
    {
        var sql =
            "CREATE OR REPLACE STAGE my_ext_stage " +
            "URL='s3://load/files/' " +
            "COPY_OPTIONS=(ON_ERROR=CONTINUE FORCE=true)";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateStage { CopyOptions: var copyOptions }:
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
    public void test_create_file_format()
    {
        var sql = "CREATE FILE FORMAT my_fmt";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateFileFormat
            {
                OrReplace: var orReplace,
                Temporary: var temporary,
                Volatile: var @volatile,
                IfNotExists: var ifNotExists,
                Name: var name,
                Options: var options,
                Comment: var comment,
            }:
                Assert.False(orReplace);
                Assert.False(temporary);
                Assert.False(@volatile);
                Assert.False(ifNotExists);
                Assert.Equal("my_fmt", name.ToString());
                Assert.True(options.Options.IsEmpty);
                Assert.Null(comment);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);

        var extendedSql =
            "CREATE OR REPLACE TEMPORARY FILE FORMAT IF NOT EXISTS my_fmt " +
            "COMMENT='some-comment'";
        switch (Snowflake().VerifiedStmt(extendedSql))
        {
            case Statement.CreateFileFormat
            {
                OrReplace: var orReplace,
                Temporary: var temporary,
                IfNotExists: var ifNotExists,
                Name: var name,
                Comment: var comment,
            }:
                Assert.True(orReplace);
                Assert.True(temporary);
                Assert.True(ifNotExists);
                Assert.Equal("my_fmt", name.ToString());
                Assert.Equal("some-comment", comment ?? throw new InvalidOperationException(UnwrapNone));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(
            Snowflake().VerifiedStmt(extendedSql).ToString(),
            extendedSql);
    }

    [Fact]
    public void test_create_file_format_with_options()
    {
        var sql =
            "CREATE FILE FORMAT my_fmt " +
            "TYPE=CSV FIELD_DELIMITER='|' SKIP_HEADER=1 COMPRESSION=GZIP";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateFileFormat { Options: var options }:
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "TYPE",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Placeholder("CSV").WithEmptySpan())),
                    options.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "FIELD_DELIMITER",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("|").WithEmptySpan())),
                    options.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "SKIP_HEADER",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Number("1", false).WithEmptySpan())),
                    options.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "COMPRESSION",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.Placeholder("GZIP").WithEmptySpan())),
                    options.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_create_file_format_volatile()
    {
        var sql = "CREATE VOLATILE FILE FORMAT my_fmt TYPE=JSON STRIP_OUTER_ARRAY=true";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CreateFileFormat
            {
                Temporary: var temporary,
                Volatile: var @volatile,
                Options: var options,
            }:
                Assert.False(temporary);
                Assert.True(@volatile);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "STRIP_OUTER_ARRAY",
                        OptionValue: new KeyValueOptionKind.Single(new Value.Boolean(true).WithEmptySpan())),
                    options.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_create_file_format_with_identifier_function()
    {
        // The Snowflake driver emits `CREATE TEMP FILE FORMAT identifier(?) ...` when
        // uploading pandas DataFrames. `TEMP` is an alias of `TEMPORARY` and the name
        // is a call to the `IDENTIFIER` function with a bind parameter.
        Snowflake().OneStatementParsesTo(
            "CREATE TEMP FILE FORMAT identifier(?) TYPE=PARQUET COMPRESSION=auto",
            "CREATE TEMPORARY FILE FORMAT identifier(?) TYPE=PARQUET COMPRESSION=auto");
    }

    [Fact]
    public void test_copy_into()
    {
        var sql =
            "COPY INTO my_company.emp_basic " +
            "FROM 'gcs://mybucket/./../a.csv'";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake
            {
                Kind: var kind,
                Into: var into,
                FromObj: var fromObj,
                Files: var files,
                Pattern: var pattern,
                ValidationMode: var validationMode,
            }:
                Assert.Equal(CopyIntoSnowflakeKind.Table, kind);
                Assert.Equal(
                    into,
                    ObjectName.From(Sequence.Of(new Ident("my_company"), new Ident("emp_basic"))));
                Assert.Equal(
                    fromObj,
                    ObjectName.From(Sequence.Of(Ident.WithQuote(
                        '\'',
                        "gcs://mybucket/./../a.csv"))));
                Assert.Null(files);
                Assert.Null(pattern);
                Assert.Null(validationMode);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);

        sql = "COPY INTO 's3://a/b/c/data.parquet' " + "FROM db.sc.tbl " + "PARTITION BY ('date=' || to_varchar(dt, 'YYYY-MM-DD') || '/hour=' || to_varchar(date_part(hour, ts)))";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake
            {
                Kind: var kind,
                Into: var into,
                FromObj: var fromObj,
                FromQuery: var fromQuery,
                Partition: var partition,
            }:
                Assert.Equal(CopyIntoSnowflakeKind.Location, kind);
                Assert.Equal(
                    into,
                    ObjectName.From(Sequence.Of(Ident.WithQuote('\'', "s3://a/b/c/data.parquet"))));
                Assert.Equal(
                    fromObj,
                    ObjectName.From(Sequence.Of(
                        new Ident("db"),
                        new Ident("sc"),
                        new Ident("tbl"))));
                Assert.Null(fromQuery);
                Assert.NotNull(partition);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);

        sql =
            "COPY INTO 's3://a/b/c/data.parquet' " +
            "FROM (SELECT * FROM tbl)";
        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake
            {
                Kind: var kind,
                Into: var into,
                FromObj: var fromObj,
                FromQuery: var fromQuery,
            }:
                Assert.Equal(CopyIntoSnowflakeKind.Location, kind);
                Assert.Equal(
                    into,
                    ObjectName.From(Sequence.Of(Ident.WithQuote('\'', "s3://a/b/c/data.parquet"))));
                Assert.NotNull(fromQuery);
                Assert.Null(fromObj);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_copy_into_with_stage_params()
    {
        var sql =
            "COPY INTO my_company.emp_basic " +
            "FROM 's3://load/files/' " +
            "STORAGE_INTEGRATION=myint " +
            "ENDPOINT='<s3_api_compatible_endpoint>' " +
            "CREDENTIALS=(AWS_KEY_ID='1a2b3c' AWS_SECRET_KEY='4x5y6z') " +
            "ENCRYPTION=(MASTER_KEY='key' TYPE='AWS_SSE_KMS')";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake { FromObj: var fromObj, StageParams: var stageParams }:
                //assert_eq!("s3://load/files/", stage_params.url.unwrap());
                Assert.Equal(
                    fromObj,
                    ObjectName.From(Sequence.Of(Ident.WithQuote(
                        '\'',
                        "s3://load/files/"))));
                Assert.Equal("myint", stageParams.StorageIntegration ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal(
                    "<s3_api_compatible_endpoint>",
                    stageParams.Endpoint ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "AWS_KEY_ID",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("1a2b3c").WithEmptySpan())),
                    stageParams.Credentials.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "AWS_SECRET_KEY",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("4x5y6z").WithEmptySpan())),
                    stageParams.Credentials.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "MASTER_KEY",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("key").WithEmptySpan())),
                    stageParams.Encryption.Options);
                Assert.Contains(
                    new KeyValueOption(
                        OptionName: "TYPE",
                        OptionValue: new KeyValueOptionKind.Single(
                            new Value.SingleQuotedString("AWS_SSE_KMS").WithEmptySpan())),
                    stageParams.Encryption.Options);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);

        // stage params within copy into with transformations
        sql =
            "COPY INTO my_company.emp_basic FROM " +
            "(SELECT t1.$1 FROM 's3://load/files/' STORAGE_INTEGRATION=myint)";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake { FromObj: var fromObj, StageParams: var stageParams }:
                Assert.Equal(
                    fromObj,
                    ObjectName.From(Sequence.Of(Ident.WithQuote(
                        '\'',
                        "s3://load/files/"))));
                Assert.Equal("myint", stageParams.StorageIntegration ?? throw new InvalidOperationException(UnwrapNone));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void test_copy_into_with_files_and_pattern_and_verification()
    {
        var sql =
            "COPY INTO my_company.emp_basic " +
            "FROM 'gcs://mybucket/./../a.csv' AS some_alias " +
            "FILES = ('file1.json', 'file2.json') " +
            "PATTERN = '.*employees0[1-5].csv.gz' " +
            "VALIDATION_MODE = RETURN_7_ROWS";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake
            {
                Files: var files,
                Pattern: var pattern,
                ValidationMode: var validationMode,
                FromObjAlias: var fromObjAlias,
            }:
                Assert.Equal(files ?? throw new InvalidOperationException(UnwrapNone), Sequence.Of("file1.json", "file2.json"));
                Assert.Equal(".*employees0[1-5].csv.gz", pattern ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal("RETURN_7_ROWS", validationMode ?? throw new InvalidOperationException(UnwrapNone));
                Assert.Equal(fromObjAlias ?? throw new InvalidOperationException(UnwrapNone), new Ident("some_alias"));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);
    }

    [Fact]
    public void test_copy_into_with_transformations()
    {
        var sql =
            "COPY INTO my_company.emp_basic FROM " +
            "(SELECT t1.$1:st AS st, $1:index, t2.$1, 4, '5' AS const_str FROM @schema.general_finished AS T) " +
            "FILES = ('file1.json', 'file2.json') " +
            "PATTERN = '.*employees0[1-5].csv.gz' " +
            "VALIDATION_MODE = RETURN_7_ROWS";

        switch (Snowflake().VerifiedStmt(sql))
        {
            case Statement.CopyIntoSnowflake { FromObj: var fromObj, FromTransformations: var fromTransformations }:
                Assert.Equal(
                    fromObj,
                    ObjectName.From(Sequence.Of(
                        new Ident("@schema"),
                        new Ident("general_finished"))));
                Assert.Equal<StageLoadSelectItemKind>(
                    (fromTransformations ?? throw new InvalidOperationException(UnwrapNone))[0],
                    new StageLoadSelectItemKind.StageLoadSelectItem(new StageLoadSelectItem(
                        Alias: new Ident("t1"),
                        FileColNum: 1,
                        Element: new Ident("st"),
                        ItemAs: new Ident("st"))));
                Assert.Equal<StageLoadSelectItemKind>(
                    (fromTransformations ?? throw new InvalidOperationException(UnwrapNone))[1],
                    new StageLoadSelectItemKind.StageLoadSelectItem(new StageLoadSelectItem(
                        Alias: null,
                        FileColNum: 1,
                        Element: new Ident("index"),
                        ItemAs: null)));
                Assert.Equal<StageLoadSelectItemKind>(
                    (fromTransformations ?? throw new InvalidOperationException(UnwrapNone))[2],
                    new StageLoadSelectItemKind.StageLoadSelectItem(new StageLoadSelectItem(
                        Alias: new Ident("t2"),
                        FileColNum: 1,
                        Element: null,
                        ItemAs: null)));
                Assert.Equal<StageLoadSelectItemKind>(
                    (fromTransformations ?? throw new InvalidOperationException(UnwrapNone))[3],
                    new StageLoadSelectItemKind.SelectItem(new SelectItem.UnnamedExpr(new Expr.Value(
                        ValueWithSpan.From(new Value.Number("4", false))))));
                Assert.Equal<StageLoadSelectItemKind>(
                    (fromTransformations ?? throw new InvalidOperationException(UnwrapNone))[4],
                    new StageLoadSelectItemKind.SelectItem(new SelectItem.ExprWithAlias(
                        Expr: new Expr.Value(ValueWithSpan.From(new Value.SingleQuotedString("5"))),
                        Alias: new Ident("const_str"))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        Assert.Equal(Snowflake().VerifiedStmt(sql).ToString(), sql);

        // Test optional AS keyword to denote an alias for the stage
        var sql1 =
            "COPY INTO my_company.emp_basic FROM " +
            "(SELECT t1.$1:st AS st, $1:index, t2.$1, 4, '5' AS const_str FROM @schema.general_finished T) ";
        _ = Snowflake().ParseSqlStatements(sql1);
    }
}
