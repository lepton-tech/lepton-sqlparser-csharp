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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_duckdb.rs.
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Notas da tradução (onda-c/C16.md):
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000). `assert_eq!(x, None)` → `Assert.Null(x)` (xUnit2003).
// - `panic!`/`unreachable!` num `match` → `default: throw new InvalidOperationException(..)`.
#pragma warning disable xUnit2013
public class SqlParserDuckDbTests
{
    private static TestedDialects DuckDb()
    {
        return new TestedDialects(new List<Dialect> { new DuckDbDialect() });
    }

    private static TestedDialects DuckDbAndGeneric()
    {
        return new TestedDialects(new List<Dialect>
        {
            new DuckDbDialect(),
            new GenericDialect(),
        });
    }

    [Fact]
    public void test_struct()
    {
        // s STRUCT(v VARCHAR, i INTEGER)
        var structType1 = new DataType.Struct(
            Sequence.Of(
                new StructField(
                    FieldName: new Ident("v"),
                    FieldType: new DataType.Varchar(null),
                    Options: null),
                new StructField(
                    FieldName: new Ident("i"),
                    FieldType: new DataType.Integer(null),
                    Options: null)),
            StructBracketKind.Parentheses);

        // basic struct
        var statement = DuckDb().VerifiedStmt("CREATE TABLE t1 (s STRUCT(v VARCHAR, i INTEGER))");
        Assert.Equal(
            ColumnDefs(statement),
            Sequence.Of(new ColumnDef(
                Name: Ident.From("s"),
                DataType: structType1,
                Options: Sequence<ColumnOptionDef>.Empty)));

        // struct array
        statement = DuckDb().VerifiedStmt("CREATE TABLE t1 (s STRUCT(v VARCHAR, i INTEGER)[])");
        Assert.Equal(
            ColumnDefs(statement),
            Sequence.Of(new ColumnDef(
                Name: Ident.From("s"),
                DataType: new DataType.Array(new ArrayElemTypeDef.SquareBracket(
                    structType1,
                    null)),
                Options: Sequence<ColumnOptionDef>.Empty)));

        // s STRUCT(v VARCHAR, s STRUCT(a1 INTEGER, a2 VARCHAR))
        var structType2 = new DataType.Struct(
            Sequence.Of(
                new StructField(
                    FieldName: new Ident("v"),
                    FieldType: new DataType.Varchar(null),
                    Options: null),
                new StructField(
                    FieldName: new Ident("s"),
                    FieldType: new DataType.Struct(
                        Sequence.Of(
                            new StructField(
                                FieldName: new Ident("a1"),
                                FieldType: new DataType.Integer(null),
                                Options: null),
                            new StructField(
                                FieldName: new Ident("a2"),
                                FieldType: new DataType.Varchar(null),
                                Options: null)),
                        StructBracketKind.Parentheses),
                    Options: null)),
            StructBracketKind.Parentheses);

        // nested struct
        statement = DuckDb().VerifiedStmt(
            "CREATE TABLE t1 (s STRUCT(v VARCHAR, s STRUCT(a1 INTEGER, a2 VARCHAR))[])");

        Assert.Equal(
            ColumnDefs(statement),
            Sequence.Of(new ColumnDef(
                Name: Ident.From("s"),
                DataType: new DataType.Array(new ArrayElemTypeDef.SquareBracket(
                    structType2,
                    null)),
                Options: Sequence<ColumnOptionDef>.Empty)));

        // failing test (duckdb does not support bracket syntax)
        var sqlList = new List<string>
        {
            "CREATE TABLE t1 (s STRUCT(v VARCHAR, i INTEGER)))",
            "CREATE TABLE t1 (s STRUCT(v VARCHAR, i INTEGER>)",
            "CREATE TABLE t1 (s STRUCT<v VARCHAR, i INTEGER>)",
            "CREATE TABLE t1 (s STRUCT v VARCHAR, i INTEGER )",
            "CREATE TABLE t1 (s STRUCT VARCHAR, i INTEGER )",
            "CREATE TABLE t1 (s STRUCT (VARCHAR, INTEGER))",
        };

        foreach (var sql in sqlList)
        {
            Assert.Throws<ParserException>(() => DuckDb().ParseSqlStatements(sql));
        }
    }

    /// <summary>Returns the ColumnDefinitions from a CreateTable statement</summary>
    private static Sequence<ColumnDef> ColumnDefs(Statement statement)
    {
        switch (statement)
        {
            case Statement.CreateTable { Value: var createTable }:
                return createTable.Columns;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void test_select_wildcard_with_exclude()
    {
        var select = DuckDb().VerifiedOnlySelect("SELECT * EXCLUDE (col_a) FROM data");
        SelectItem expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptExclude = new ExcludeSelectItem.Multiple(Sequence.Of(ObjectName.From(
                new Ident("col_a")))),
        });
        Assert.Equal(expected, select.Projection[0]);

        select =
            DuckDb().VerifiedOnlySelect("SELECT name.* EXCLUDE department_id FROM employee_table");
        expected = new SelectItem.QualifiedWildcard(
            new SelectItemQualifiedWildcardKind.ObjectName(ObjectName.From(Sequence.Of(new Ident("name")))),
            WildcardAdditionalOptions.Default() with
            {
                OptExclude = new ExcludeSelectItem.Single(ObjectName.From(new Ident(
                    "department_id"))),
            });
        Assert.Equal(expected, select.Projection[0]);

        select = DuckDb()
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
    public void parse_div_infix()
    {
        DuckDbAndGeneric().VerifiedStmt("SELECT 5 // 2");
    }

    [Fact]
    public void test_create_macro()
    {
        var macro = DuckDb().VerifiedStmt("CREATE MACRO schema.add(a, b) AS a + b");
        Statement expected = new Statement.CreateMacro(
            OrReplace: false,
            Temporary: false,
            Name: ObjectName.From(Sequence.Of(new Ident("schema"), new Ident("add"))),
            Args: Sequence.Of(new MacroArg("a"), new MacroArg("b")),
            Definition: new MacroDefinition.Expr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("a")),
                Op: new BinaryOperator.Plus(),
                Right: new Expr.Identifier(new Ident("b")))));
        Assert.Equal(expected, macro);
    }

    [Fact]
    public void test_create_macro_default_args()
    {
        var macro = DuckDb().VerifiedStmt("CREATE MACRO add_default(a, b := 5) AS a + b");
        Statement expected = new Statement.CreateMacro(
            OrReplace: false,
            Temporary: false,
            Name: ObjectName.From(Sequence.Of(new Ident("add_default"))),
            Args: Sequence.Of(
                new MacroArg("a"),
                new MacroArg(
                    Name: new Ident("b"),
                    DefaultExpr: Expr.MakeValue(TestUtils.Number("5")))),
            Definition: new MacroDefinition.Expr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("a")),
                Op: new BinaryOperator.Plus(),
                Right: new Expr.Identifier(new Ident("b")))));
        Assert.Equal(expected, macro);
    }

    [Fact]
    public void test_create_table_macro()
    {
        var query = "SELECT col1_value AS column1, col2_value AS column2 UNION ALL SELECT 'Hello' AS col1_value, 456 AS col2_value";
        var macro = DuckDb().VerifiedStmt(
            "CREATE OR REPLACE TEMPORARY MACRO dynamic_table(col1_value, col2_value) AS TABLE "
            + query);
        Statement expected = new Statement.CreateMacro(
            OrReplace: true,
            Temporary: true,
            Name: ObjectName.From(Sequence.Of(new Ident("dynamic_table"))),
            Args: Sequence.Of(
                new MacroArg("col1_value"),
                new MacroArg("col2_value")),
            Definition: new MacroDefinition.Table(DuckDb().VerifiedQuery(query)));
        Assert.Equal(expected, macro);
    }

    [Fact]
    public void test_select_union_by_name()
    {
        var q1 = "SELECT * FROM capitals UNION BY NAME SELECT * FROM weather";
        var q2 = "SELECT * FROM capitals UNION ALL BY NAME SELECT * FROM weather";
        var q3 = "SELECT * FROM capitals UNION DISTINCT BY NAME SELECT * FROM weather";

        foreach (var (ast, expectedQuantifier) in new (Query, SetQuantifier)[]
        {
            (DuckDb().VerifiedQuery(q1), new SetQuantifier.ByName()),
            (DuckDb().VerifiedQuery(q2), new SetQuantifier.AllByName()),
            (DuckDb().VerifiedQuery(q3), new SetQuantifier.DistinctByName()),
        })
        {
            SetExpr expected = new SetExpr.SetOperation(
                Op: new SetOperator.Union(),
                SetQuantifier: expectedQuantifier,
                Left: new SetExpr.Select(new Select(
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
                        Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident(
                            Value: "capitals",
                            QuoteStyle: null,
                            Span: Span.Empty())))),
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
                Right: new SetExpr.Select(new Select(
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
                        Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident(
                            Value: "weather",
                            QuoteStyle: null,
                            Span: Span.Empty())))),
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
                    Flavor: SelectFlavor.Standard)));
            Assert.Equal(ast.Body, expected);
        }
    }

    [Fact]
    public void test_duckdb_install()
    {
        var stmt = DuckDb().VerifiedStmt("INSTALL tpch");
        Assert.Equal(
            stmt,
            new Statement.Install(
                ExtensionName: new Ident(
                    Value: "tpch",
                    QuoteStyle: null,
                    Span: Span.Empty())));
    }

    [Fact]
    public void test_duckdb_load_extension()
    {
        var stmt = DuckDb().VerifiedStmt("LOAD my_extension");
        Assert.Equal(
            new Statement.Load(
                ExtensionName: new Ident(
                    Value: "my_extension",
                    QuoteStyle: null,
                    Span: Span.Empty())),
            stmt);
    }

    [Fact]
    public void test_duckdb_specific_int_types()
    {
        var duckdbDtypes = new List<(string, DataType)>
        {
            ("UTINYINT", new DataType.UTinyInt()),
            ("USMALLINT", new DataType.USmallInt()),
            ("UBIGINT", new DataType.UBigInt()),
            ("UHUGEINT", new DataType.UHugeInt()),
            ("HUGEINT", new DataType.HugeInt()),
        };
        foreach (var (dtypeString, dataType) in duckdbDtypes)
        {
            var sql = $"SELECT 123::{dtypeString}";
            var select = DuckDb().VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.Cast(
                    Kind: CastKind.DoubleColon,
                    Expr: new Expr.Value(
                        new Value.Number("123", false).WithEmptySpan()),
                    DataType: dataType,
                    Format: null),
                TestUtils.ExprFromProjection(select.Projection[0]));
        }
    }

    [Fact]
    public void test_duckdb_struct_literal()
    {
        //struct literal syntax https://duckdb.org/docs/sql/data_types/struct#creating-structs
        //syntax: {'field_name': expr1[, ... ]}
        var sql = "SELECT {'a': 1, 'b': 2, 'c': 3}, [{'a': 'abc'}], {'a': 1, 'b': [t.str_col]}, {'a': 1, 'b': 'abc'}, {'abc': str_col}, {'a': {'aa': 1}}";
        var select = DuckDbAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(6, select.Projection.Count);
        Assert.Equal(
            new Expr.Dictionary(Sequence.Of(
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "a"),
                    Value: Expr.MakeValue(TestUtils.Number("1"))),
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "b"),
                    Value: Expr.MakeValue(TestUtils.Number("2"))),
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "c"),
                    Value: Expr.MakeValue(TestUtils.Number("3"))))),
            TestUtils.ExprFromProjection(select.Projection[0]));

        Assert.Equal(
            new Expr.Array(new Ast.Array(
                Elem: Sequence.Of<Expr>(new Expr.Dictionary(Sequence.Of(new DictionaryField(
                    Key: Ident.WithQuote('\'', "a"),
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("abc").WithEmptySpan()))))),
                Named: false)),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Dictionary(Sequence.Of(
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "a"),
                    Value: Expr.MakeValue(TestUtils.Number("1"))),
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "b"),
                    Value: new Expr.Array(new Ast.Array(
                        Elem: Sequence.Of<Expr>(new Expr.CompoundIdentifier(Sequence.Of(
                            Ident.From("t"),
                            Ident.From("str_col")))),
                        Named: false))))),
            TestUtils.ExprFromProjection(select.Projection[2]));
        Assert.Equal(
            new Expr.Dictionary(Sequence.Of(
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "a"),
                    Value: Expr.MakeValue(TestUtils.Number("1"))),
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "b"),
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("abc").WithEmptySpan())))),
            TestUtils.ExprFromProjection(select.Projection[3]));
        Assert.Equal(
            new Expr.Dictionary(Sequence.Of(new DictionaryField(
                Key: Ident.WithQuote('\'', "abc"),
                Value: new Expr.Identifier(Ident.From("str_col"))))),
            TestUtils.ExprFromProjection(select.Projection[4]));
        Assert.Equal(
            new Expr.Dictionary(Sequence.Of(new DictionaryField(
                Key: Ident.WithQuote('\'', "a"),
                Value: new Expr.Dictionary(Sequence.Of(new DictionaryField(
                    Key: Ident.WithQuote('\'', "aa"),
                    Value: Expr.MakeValue(TestUtils.Number("1")))))))),
            TestUtils.ExprFromProjection(select.Projection[5]));
    }

    [Fact]
    public void test_create_secret()
    {
        var sql = "CREATE OR REPLACE PERSISTENT SECRET IF NOT EXISTS name IN storage ( TYPE type, key1 value1, key2 value2 )";
        var stmt = DuckDb().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.CreateSecret(
                OrReplace: true,
                Temporary: false,
                IfNotExists: true,
                Name: new Ident("name"),
                StorageSpecifier: new Ident("storage"),
                SecretType: new Ident("type"),
                Options: Sequence.Of(
                    new SecretOption(
                        Key: new Ident("key1"),
                        Value: new Ident("value1")),
                    new SecretOption(
                        Key: new Ident("key2"),
                        Value: new Ident("value2")))),
            stmt);
    }

    [Fact]
    public void test_create_secret_simple()
    {
        var sql = "CREATE SECRET ( TYPE type )";
        var stmt = DuckDb().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.CreateSecret(
                OrReplace: false,
                Temporary: null,
                IfNotExists: false,
                Name: null,
                StorageSpecifier: null,
                SecretType: new Ident("type"),
                Options: Sequence<SecretOption>.Empty),
            stmt);
    }

    [Fact]
    public void test_drop_secret()
    {
        var sql = "DROP PERSISTENT SECRET IF EXISTS secret FROM storage";
        var stmt = DuckDb().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.DropSecret(
                IfExists: true,
                Temporary: false,
                Name: new Ident("secret"),
                StorageSpecifier: new Ident("storage")),
            stmt);
    }

    [Fact]
    public void test_drop_secret_simple()
    {
        var sql = "DROP SECRET secret";
        var stmt = DuckDb().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.DropSecret(
                IfExists: false,
                Temporary: null,
                Name: new Ident("secret"),
                StorageSpecifier: null),
            stmt);
    }

    [Fact]
    public void test_attach_database()
    {
        var sql = "ATTACH DATABASE IF NOT EXISTS 'sqlite_file.db' AS sqlite_db (READ_ONLY false, TYPE SQLITE)";
        var stmt = DuckDb().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.AttachDuckDBDatabase(
                IfNotExists: true,
                Database: true,
                DatabasePath: Ident.WithQuote('\'', "sqlite_file.db"),
                DatabaseAlias: new Ident("sqlite_db"),
                AttachOptions: Sequence.Of<AttachDuckDBDatabaseOption>(
                    new AttachDuckDBDatabaseOption.ReadOnly(false),
                    new AttachDuckDBDatabaseOption.Type(new Ident("SQLITE")))),
            stmt);
    }

    [Fact]
    public void test_attach_database_simple()
    {
        var sql = "ATTACH 'postgres://user.name:pass-word@some.url.com:5432/postgres'";
        var stmt = DuckDb().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.AttachDuckDBDatabase(
                IfNotExists: false,
                Database: false,
                DatabasePath: Ident.WithQuote(
                    '\'',
                    "postgres://user.name:pass-word@some.url.com:5432/postgres"),
                DatabaseAlias: null,
                AttachOptions: Sequence<AttachDuckDBDatabaseOption>.Empty),
            stmt);
    }

    [Fact]
    public void test_detach_database()
    {
        var sql = "DETACH DATABASE IF EXISTS db_name";
        var stmt = DuckDb().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.DetachDuckDBDatabase(
                IfExists: true,
                Database: true,
                DatabaseAlias: new Ident("db_name")),
            stmt);
    }

    [Fact]
    public void test_detach_database_simple()
    {
        var sql = "DETACH db_name";
        var stmt = DuckDb().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.DetachDuckDBDatabase(
                IfExists: false,
                Database: false,
                DatabaseAlias: new Ident("db_name")),
            stmt);
    }

    [Fact]
    public void test_duckdb_named_argument_function_with_assignment_operator()
    {
        var sql = "SELECT FUN(a := '1', b := '2') FROM foo";
        var select = DuckDbAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("FUN"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: null,
                    Args: Sequence.Of<FunctionArg>(
                        new FunctionArg.Named(
                            Name: new Ident("a"),
                            Arg: new FunctionArgExpr.Expr(new Expr.Value(
                                new Value.SingleQuotedString("1").WithEmptySpan())),
                            Operator: new FunctionArgOperator.Assignment()),
                        new FunctionArg.Named(
                            Name: new Ident("b"),
                            Arg: new FunctionArgExpr.Expr(new Expr.Value(
                                new Value.SingleQuotedString("2").WithEmptySpan())),
                            Operator: new FunctionArgOperator.Assignment())),
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void test_array_index()
    {
        var sql = "SELECT ['a', 'b', 'c'][3] AS three";
        var select = DuckDb().VerifiedOnlySelect(sql);
        var projection = select.Projection;
        Assert.Equal(1, projection.Count);
        var expr = projection[0] switch
        {
            SelectItem.ExprWithAlias exprWithAlias => exprWithAlias.Expr,
            _ => throw new InvalidOperationException("Expected an expression with alias"),
        };
        Assert.Equal(
            new Expr.CompoundFieldAccess(
                Root: new Expr.Array(new Ast.Array(
                    Elem: Sequence.Of<Expr>(
                        new Expr.Value(new Value.SingleQuotedString("a").WithEmptySpan()),
                        new Expr.Value(new Value.SingleQuotedString("b").WithEmptySpan()),
                        new Expr.Value(new Value.SingleQuotedString("c").WithEmptySpan())),
                    Named: false)),
                AccessChain: Sequence.Of<AccessExpr>(new AccessExpr.Subscript(new Subscript.Index(
                    IndexValue: Expr.MakeValue(TestUtils.Number("3")))))),
            expr);
    }

    [Fact]
    public void test_duckdb_union_datatype()
    {
        var sql = "CREATE TABLE tbl1 (one UNION(a INT), two UNION(a INT, b INT), nested UNION(a UNION(b INT)))";
        var stmt = DuckDbAndGeneric().VerifiedStmt(sql);
        Assert.Equal(
            new Statement.CreateTable(new CreateTable(
                OrReplace: false,
                Temporary: false,
                Unlogged: false,
                External: false,
                Global: null,
                IfNotExists: false,
                Transient: false,
                Volatile: false,
                Iceberg: false,
                Snapshot: false,
                Dynamic: false,
                Name: ObjectName.From(Sequence.Of(Ident.From("tbl1"))),
                Columns: Sequence.Of(
                    new ColumnDef(
                        Name: Ident.From("one"),
                        DataType: new DataType.Union(Sequence.Of(new UnionField(
                            FieldName: Ident.From("a"),
                            FieldType: new DataType.Int(null)))),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    new ColumnDef(
                        Name: Ident.From("two"),
                        DataType: new DataType.Union(Sequence.Of(
                            new UnionField(
                                FieldName: Ident.From("a"),
                                FieldType: new DataType.Int(null)),
                            new UnionField(
                                FieldName: Ident.From("b"),
                                FieldType: new DataType.Int(null)))),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    new ColumnDef(
                        Name: Ident.From("nested"),
                        DataType: new DataType.Union(Sequence.Of(new UnionField(
                            FieldName: Ident.From("a"),
                            FieldType: new DataType.Union(Sequence.Of(new UnionField(
                                FieldName: Ident.From("b"),
                                FieldType: new DataType.Int(null))))))),
                        Options: Sequence<ColumnOptionDef>.Empty)),
                Constraints: Sequence<TableConstraint>.Empty,
                HiveDistribution: new HiveDistributionStyle.NONE(),
                HiveFormats: null,
                FileFormat: null,
                Location: null,
                Query: null,
                WithoutRowid: false,
                Like: null,
                CloneValue: null,
                Comment: null,
                OnCommit: null,
                OnCluster: null,
                PrimaryKey: null,
                OrderBy: null,
                PartitionBy: null,
                ClusterBy: null,
                ClusteredBy: null,
                Inherits: null,
                PartitionOf: null,
                ForValues: null,
                Strict: false,
                CopyGrants: false,
                EnableSchemaEvolution: null,
                ChangeTracking: null,
                DataRetentionTimeInDays: null,
                MaxDataExtensionTimeInDays: null,
                DefaultDdlCollation: null,
                WithAggregationPolicy: null,
                WithRowAccessPolicy: null,
                WithStorageLifecyclePolicy: null,
                WithTags: null,
                BaseLocation: null,
                ExternalVolume: null,
                WithConnection: null,
                Catalog: null,
                CatalogSync: null,
                StorageSerializationPolicy: null,
                TableOptions: new CreateTableOptions.None(),
                TargetLag: null,
                Warehouse: null,
                Version: null,
                RefreshMode: null,
                Initialize: null,
                RequireUser: false,
                Diststyle: null,
                Distkey: null,
                Sortkey: null,
                Backup: null,
                Multiset: null,
                Fallback: null,
                WithData: null)),
            stmt);
    }

    [Fact]
    public void parse_use()
    {
        var validObjectNames = new[]
        {
            "mydb",
            "SCHEMA",
            "DATABASE",
            "CATALOG",
            "WAREHOUSE",
            "DEFAULT",
        };
        var quoteStyles = new[] { '"', '\'' };

        foreach (var objectName in validObjectNames)
        {
            // Test single identifier without quotes
            Assert.Equal(
                DuckDb().VerifiedStmt($"USE {objectName}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(new Ident(
                    objectName))))));
            foreach (var quote in quoteStyles)
            {
                // Test single identifier with different type of quotes
                Assert.Equal(
                    DuckDb().VerifiedStmt($"USE {quote}{objectName}{quote}"),
                    new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(Ident.WithQuote(
                        quote,
                        objectName))))));
            }
        }

        foreach (var quote in quoteStyles)
        {
            // Test double identifier with different type of quotes
            Assert.Equal(
                DuckDb().VerifiedStmt(
                    $"USE {quote}CATALOG{quote}.{quote}my_schema{quote}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(
                    Ident.WithQuote(quote, "CATALOG"),
                    Ident.WithQuote(quote, "my_schema"))))));
        }
        // Test double identifier without quotes
        Assert.Equal(
            DuckDb().VerifiedStmt("USE mydb.my_schema"),
            new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(
                new Ident("mydb"),
                new Ident("my_schema"))))));
    }

    [Fact]
    public void test_duckdb_trim()
    {
        var realSql = "SELECT customer_id, TRIM(item_price_id, '\"', \"a\") AS item_price_id FROM models_staging.subscriptions";
        Assert.Equal(DuckDb().VerifiedStmt(realSql).ToString(), realSql);

        var sqlOnlySelect = "SELECT TRIM('xyz', 'a')";
        var select = DuckDb().VerifiedOnlySelect(sqlOnlySelect);
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
        var ex = Assert.Throws<ParserException>(() => DuckDb().ParseSqlStatements(errorSql));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: 'a'"),
            ex.Error);
    }

    [Fact]
    public void parse_extract_single_quotes()
    {
        var sql = "SELECT EXTRACT('month' FROM my_timestamp) FROM my_table";
        DuckDb().VerifiedStmt(sql);
    }

    [Fact]
    public void test_duckdb_lambda_function()
    {
        // Test basic lambda with list_filter
        var sql = "SELECT [3, 4, 5, 6].list_filter(lambda x : x > 4)";
        DuckDb().VerifiedStmt(sql);

        // Test lambda with arrow syntax (also supported by DuckDB)
        var sqlArrow = "SELECT list_filter([1, 2, 3], x -> x > 1)";
        DuckDb().VerifiedStmt(sqlArrow);

        // Both readings of `->` print identically, so round-tripping cannot tell
        // a lambda from JSON member access. Assert the shape instead.
        var select = DuckDb().VerifiedOnlySelect(sqlArrow);
        if (TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)) is not Expr.Function { Value: var func })
        {
            throw new InvalidOperationException("expected a function call");
        }
        if (func.Args is not FunctionArguments.List { Value: var args })
        {
            throw new InvalidOperationException("expected an argument list");
        }
        if (args.Args is not [_, FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.Lambda { Value: var lambda } } }])
        {
            throw new InvalidOperationException("expected the second argument to be a lambda");
        }
        Assert.Equal(LambdaSyntax.Arrow, lambda.Syntax);

        // Test lambda with multiple parameters (with index)
        var sqlMulti = "SELECT list_filter([1, 3, 1, 5], lambda x, i : x > i)";
        DuckDb().VerifiedStmt(sqlMulti);

        // Test lambda in list_transform
        var sqlTransform = "SELECT list_transform([1, 2, 3], lambda x : x * 2)";
        DuckDb().VerifiedStmt(sqlTransform);
    }

    [Fact]
    public void test_duckdb_nested_block_comments()
    {
        DuckDb().OneStatementParsesTo(
            "SELECT a /* outer /* inner */ still comment */ FROM t",
            "SELECT a FROM t");

        var err = Assert.Throws<ParserException>(() => DuckDb()
            .ParseSqlStatements("SELECT a /* outer /* inner */ FROM t")).Error;
        // assert!(err.to_string().contains(..), "{err}")
        Assert.Contains(
            "Unexpected EOF while in a multi-line comment",
            err.ToString());
    }
}
