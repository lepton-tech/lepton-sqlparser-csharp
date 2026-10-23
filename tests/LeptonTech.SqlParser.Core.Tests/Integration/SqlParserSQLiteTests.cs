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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_sqlite.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

// Notas da tradução (onda-c/C15.md):
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal/constante: o literal
//   vai na posição "expected" (regra xUnit2000, como em AstModTests e SpansTests).
// - `assert_eq!(x.len(), 0)` → `Assert.Equal(0, x.Count)`; o xUnit2013 fica suprimido neste arquivo.
// - `unreachable!()` / `panic!(msg)` → `Assert.Fail(msg)` (como em SpansTests); `.unwrap()` → `!`.
// - `fn` aninhada dentro de um teste → função local `static` com o nome mecânico (onda-b/B1.md, item 9).

/// <summary>
/// Test SQL syntax specific to SQLite. The parser based on the
/// generic dialect is also tested (on the inputs it can handle).
/// </summary>
#pragma warning disable xUnit2013
public class SqlParserSQLiteTests
{
    private const string Unreachable = "internal error: entered unreachable code";

    [Fact]
    public void pragma_no_value()
    {
        var sql = "PRAGMA cache_size";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.Pragma { Value: null, IsEq: false } pragma:
                Assert.Equal("cache_size", pragma.Name.ToString());
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void pragma_eq_style()
    {
        var sql = "PRAGMA cache_size = 10";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.Pragma { Value: { } val, IsEq: true } pragma:
                Assert.Equal("cache_size", pragma.Name.ToString());
                Assert.Equal("10", val.ToString());
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void pragma_function_style()
    {
        var sql = "PRAGMA cache_size(10)";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.Pragma { Value: { } val, IsEq: false } pragma:
                Assert.Equal("cache_size", pragma.Name.ToString());
                Assert.Equal("10", val.ToString());
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void pragma_eq_string_style()
    {
        var sql = "PRAGMA table_info = 'sqlite_master'";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.Pragma { Value: { } val, IsEq: true } pragma:
                Assert.Equal("table_info", pragma.Name.ToString());
                Assert.Equal("'sqlite_master'", val.ToString());
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void pragma_function_string_style()
    {
        var sql = "PRAGMA table_info(\"sqlite_master\")";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.Pragma { Value: { } val, IsEq: false } pragma:
                Assert.Equal("table_info", pragma.Name.ToString());
                Assert.Equal("\"sqlite_master\"", val.ToString());
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void pragma_eq_placeholder_style()
    {
        var sql = "PRAGMA table_info = ?";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.Pragma { Value: { } val, IsEq: true } pragma:
                Assert.Equal("table_info", pragma.Name.ToString());
                Assert.Equal("?", val.ToString());
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_create_table_without_rowid()
    {
        var sql = "CREATE TABLE t (a INT) WITHOUT ROWID";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(
                    createTable.SqliteTableOptions.Options,
                    Sequence.Of<SqliteTableOption>(new SqliteTableOption.WithoutRowid()));
                Assert.Equal("t", createTable.Name.ToString());
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_create_virtual_table()
    {
        var sql = "CREATE VIRTUAL TABLE IF NOT EXISTS t USING module_name (arg1, arg2)";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateVirtualTable { IfNotExists: true } virtualTable:
            {
                var args = Sequence.Of(new Ident("arg1"), new Ident("arg2"));
                Assert.Equal("t", virtualTable.Name.ToString());
                Assert.Equal("module_name", virtualTable.ModuleName.ToString());
                Assert.Equal(args, virtualTable.ModuleArgs);
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }

        sql = "CREATE VIRTUAL TABLE t USING module_name";
        SqliteAndGeneric().VerifiedStmt(sql);
    }

    [Fact]
    public void parse_create_view_temporary_if_not_exists()
    {
        var sql = "CREATE TEMPORARY VIEW IF NOT EXISTS myschema.myview AS SELECT foo FROM bar";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateView { Value: var createView }:
            {
                var lateBinding = createView.WithNoSchemaBinding;
                Assert.Equal("myschema.myview", createView.Name.ToString());
                Assert.Equal(Sequence<ViewColumnDef>.Empty, createView.Columns);
                Assert.Equal("SELECT foo FROM bar", createView.Query.ToString());
                Assert.True(!createView.Materialized);
                Assert.True(!createView.OrReplace);
                Assert.Equal(createView.Options, new CreateTableOptions.None());
                Assert.Equal(createView.ClusterBy, Sequence<Ident>.Empty);
                Assert.True(createView.Comment is null);
                Assert.True(!lateBinding);
                Assert.True(createView.IfNotExists);
                Assert.True(createView.Temporary);
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void double_equality_operator()
    {
        // Sqlite supports this operator: https://www.sqlite.org/lang_expr.html#binaryops
        var input = "SELECT a==b FROM t";
        var expected = "SELECT a = b FROM t";
        _ = SqliteAndGeneric().OneStatementParsesTo(input, expected);
    }

    [Fact]
    public void parse_create_table_auto_increment()
    {
        var sql = "CREATE TABLE foo (bar INT PRIMARY KEY AUTOINCREMENT)";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("foo", createTable.Name.ToString());
                Assert.Equal(
                    Sequence.Of(new ColumnDef(
                        Name: Ident.From("bar"),
                        DataType: new DataType.Int(null),
                        Options: Sequence.Of(
                            new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.PrimaryKey(new PrimaryKeyConstraint(
                                    Name: null,
                                    IndexName: null,
                                    IndexType: null,
                                    Columns: Sequence<IndexColumn>.Empty,
                                    Include: Sequence<Ident>.Empty,
                                    IndexOptions: Sequence<IndexOption>.Empty,
                                    Characteristics: null))),
                            new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.DialectSpecific(Sequence.Of(Token.MakeKeyword(
                                    "AUTOINCREMENT"))))))),
                    createTable.Columns);
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_create_table_primary_key_asc_desc()
    {
        static ColumnDef ExpectedColumnDef(string kind) => new ColumnDef(
            Name: Ident.From("bar"),
            DataType: new DataType.Int(null),
            Options: Sequence.Of(
                new ColumnOptionDef(
                    Name: null,
                    Option: new ColumnOption.PrimaryKey(new PrimaryKeyConstraint(
                        Name: null,
                        IndexName: null,
                        IndexType: null,
                        Columns: Sequence<IndexColumn>.Empty,
                        Include: Sequence<Ident>.Empty,
                        IndexOptions: Sequence<IndexOption>.Empty,
                        Characteristics: null))),
                new ColumnOptionDef(
                    Name: null,
                    Option: new ColumnOption.DialectSpecific(Sequence.Of(Token.MakeKeyword(kind))))));

        var sql = "CREATE TABLE foo (bar INT PRIMARY KEY ASC)";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(Sequence.Of(ExpectedColumnDef("ASC")), createTable.Columns);
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
        sql = "CREATE TABLE foo (bar INT PRIMARY KEY DESC)";
        switch (SqliteAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(Sequence.Of(ExpectedColumnDef("DESC")), createTable.Columns);
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_create_sqlite_quote()
    {
        var sql = "CREATE TABLE `PRIMARY` (\"KEY\" INT, [INDEX] INT)";
        switch (Sqlite().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("`PRIMARY`", createTable.Name.ToString());
                Assert.Equal(
                    Sequence.Of(
                        new ColumnDef(
                            Name: Ident.WithQuote('"', "KEY"),
                            DataType: new DataType.Int(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: Ident.WithQuote('[', "INDEX"),
                            DataType: new DataType.Int(null),
                            Options: Sequence<ColumnOptionDef>.Empty)),
                    createTable.Columns);
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_create_table_gencol()
    {
        var sqlDefault = "CREATE TABLE t1 (a INT, b INT GENERATED ALWAYS AS (a * 2))";
        SqliteAndGeneric().VerifiedStmt(sqlDefault);

        var sqlVirt = "CREATE TABLE t1 (a INT, b INT GENERATED ALWAYS AS (a * 2) VIRTUAL)";
        SqliteAndGeneric().VerifiedStmt(sqlVirt);

        var sqlStored = "CREATE TABLE t1 (a INT, b INT GENERATED ALWAYS AS (a * 2) STORED)";
        SqliteAndGeneric().VerifiedStmt(sqlStored);

        SqliteAndGeneric().VerifiedStmt("CREATE TABLE t1 (a INT, b INT AS (a * 2))");
        SqliteAndGeneric().VerifiedStmt("CREATE TABLE t1 (a INT, b INT AS (a * 2) VIRTUAL)");
        SqliteAndGeneric().VerifiedStmt("CREATE TABLE t1 (a INT, b INT AS (a * 2) STORED)");
    }

    [Fact]
    public void parse_create_table_on_conflict_col()
    {
        foreach (var keyword in new[]
        {
            Keyword.ROLLBACK,
            Keyword.ABORT,
            Keyword.FAIL,
            Keyword.IGNORE,
            Keyword.REPLACE,
        })
        {
            // `{keyword:?}` → nome da variante (convencoes.md §13, caso 46)
            var sql = $"CREATE TABLE t1 (a INT, b INT ON CONFLICT {keyword.ToString()})";
            switch (SqliteAndGeneric().VerifiedStmt(sql))
            {
                case Statement.CreateTable { Value: var createTable }:
                    Assert.Equal(
                        Sequence.Of(new ColumnOptionDef(
                            Name: null,
                            Option: new ColumnOption.OnConflict(keyword))),
                        createTable.Columns[1].Options);
                    break;
                default:
                    Assert.Fail(Unreachable);
                    break;
            }
        }
    }

    [Fact]
    public void test_parse_create_table_on_conflict_col_err()
    {
        var sqlErr = "CREATE TABLE t1 (a INT, b INT ON CONFLICT BOH)";
        var err = Assert.Throws<ParserException>(() => SqliteAndGeneric()
            .ParseSqlStatements(sqlErr)).Error;
        Assert.Equal(
            err,
            new ParserError.ParserErrorValue(
                "Expected: one of ROLLBACK or ABORT or FAIL or IGNORE or REPLACE, found: BOH"));
    }

    [Fact]
    public void parse_create_table_untyped()
    {
        Sqlite().VerifiedStmt("CREATE TABLE t1 (a, b AS (a * 2), c NOT NULL)");
    }

    [Fact]
    public void test_placeholder()
    {
        // In postgres, this would be the absolute value operator '@' applied to the column 'xxx'
        // But in sqlite, this is a named parameter.
        // see https://www.sqlite.org/lang_expr.html#varparam
        var sql = "SELECT @xxx";
        var ast = Sqlite().VerifiedOnlySelect(sql);
        Assert.Equal(
            ast.Projection[0],
            new SelectItem.UnnamedExpr(new Expr.Value(
                new Value.Placeholder("@xxx").WithEmptySpan())));
    }

    [Fact]
    public void parse_create_table_with_strict()
    {
        var sql = "CREATE TABLE Fruits (id TEXT NOT NULL PRIMARY KEY) STRICT";
        if (Sqlite().VerifiedStmt(sql) is Statement.CreateTable { Value: var createTable })
        {
            Assert.Equal("Fruits", createTable.Name.ToString());
            Assert.Equal(
                createTable.SqliteTableOptions.Options,
                Sequence.Of<SqliteTableOption>(new SqliteTableOption.Strict()));
        }
    }

    [Fact]
    public void parse_single_quoted_identified()
    {
        Sqlite().VerifiedOnlySelect("SELECT 't'.*, t.'x' FROM 't'");
        // TODO: add support for select 't'.x
    }

    [Fact]
    public void parse_substring()
    {
        // SQLite supports the SUBSTRING function since v3.34, but does not support the SQL standard
        // SUBSTRING(expr FROM start FOR length) syntax.
        // https://www.sqlite.org/lang_corefunc.html#substr
        Sqlite().VerifiedOnlySelect("SELECT SUBSTRING('SQLITE', 3, 4)");
        Sqlite().VerifiedOnlySelect("SELECT SUBSTR('SQLITE', 3, 4)");
        Sqlite().VerifiedOnlySelect("SELECT SUBSTRING('SQLITE', 3)");
        Sqlite().VerifiedOnlySelect("SELECT SUBSTR('SQLITE', 3)");
    }

    [Fact]
    public void parse_window_function_with_filter()
    {
        foreach (var funcName in new[]
        {
            "row_number",
            "rank",
            "max",
            "count",
            "user_defined_function",
        })
        {
            var sql = $"SELECT {funcName}(x) FILTER (WHERE y) OVER () FROM t";
            var select = Sqlite().VerifiedOnlySelect(sql);
            Assert.Equal(select.ToString(), sql);
            Assert.Equal(
                select.Projection,
                Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(new Ident(funcName))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.List(new FunctionArgumentList(
                        DuplicateTreatment: null,
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                            new Expr.Identifier(new Ident("x"))))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                    NullTreatment: null,
                    Over: new WindowType.WindowSpec(new WindowSpec(
                        WindowName: null,
                        PartitionBy: Sequence<Expr>.Empty,
                        OrderBy: Sequence<OrderByExpr>.Empty,
                        WindowFrame: null)),
                    Filter: new Expr.Identifier(new Ident("y")),
                    WithinGroup: Sequence<OrderByExpr>.Empty)))));
        }
    }

    [Fact]
    public void parse_attach_database()
    {
        var sql = "ATTACH DATABASE 'test.db' AS test";
        var verifiedStmt = Sqlite().VerifiedStmt(sql);
        Assert.Equal(sql, $"{verifiedStmt}");
        switch (verifiedStmt)
        {
            case Statement.AttachDatabase
            {
                DatabaseFileName: Expr.Value { Inner: { Value: Value.SingleQuotedString { Value: var literalName } } },
                Database: true,
            } attachDatabase:
                Assert.Equal("test", attachDatabase.SchemaName.Value);
                Assert.Equal("test.db", literalName);
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_update_tuple_row_values()
    {
        // See https://github.com/sqlparser-rs/sqlparser-rs/issues/1311
        Assert.Equal(
            Sqlite().VerifiedStmt("UPDATE x SET (a, b) = (1, 2)"),
            new Statement.Update(new Update(
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Or: null,
                Assignments: Sequence.Of(new Assignment(
                    Target: new AssignmentTarget.Tuple(Sequence.Of(
                        ObjectName.From(Sequence.Of(new Ident("a"))),
                        ObjectName.From(Sequence.Of(new Ident("b"))))),
                    Value: new Expr.Tuple(Sequence.Of<Expr>(
                        new Expr.Value(new Value.Number("1", false).WithEmptySpan()),
                        new Expr.Value(new Value.Number("2", false).WithEmptySpan()))))),
                Selection: null,
                Table: new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("x")))),
                    Joins: Sequence<Join>.Empty),
                OnCluster: null,
                From: null,
                Returning: null,
                Settings: null,
                Output: null,
                OrderBy: Sequence<OrderByExpr>.Empty,
                Limit: null,
                UpdateToken: AttachedToken.Empty())));
    }

    [Fact]
    public void parse_where_in_empty_list()
    {
        var sql = "SELECT * FROM t1 WHERE a IN ()";
        var select = Sqlite().VerifiedOnlySelect(sql);
        if (select.Selection! is Expr.In { Target: InTarget.List { Value: var list } })
        {
            Assert.Equal(0, list.Count);
        }
        else
        {
            Assert.Fail(Unreachable);
        }

        SqliteWithOptions(new ParserOptions().WithTrailingCommas(true)).OneStatementParsesTo(
            "SELECT * FROM t1 WHERE a IN (,)",
            "SELECT * FROM t1 WHERE a IN ()");
    }

    [Fact]
    public void invalid_empty_list()
    {
        var sql = "SELECT * FROM t1 WHERE a IN (,,)";
        var sqlite = SqliteWithOptions(new ParserOptions().WithTrailingCommas(true));
        Assert.Equal(
            "sql parser error: Expected: an expression, found: ,",
            Assert.Throws<ParserException>(() => sqlite.ParseSqlStatements(sql)).Error.ToString());
    }

    [Fact]
    public void parse_start_transaction_with_modifier()
    {
        SqliteAndGeneric().VerifiedStmt("BEGIN DEFERRED TRANSACTION");
        SqliteAndGeneric().VerifiedStmt("BEGIN IMMEDIATE TRANSACTION");
        SqliteAndGeneric().VerifiedStmt("BEGIN EXCLUSIVE TRANSACTION");
        SqliteAndGeneric().VerifiedStmt("BEGIN DEFERRED");
        SqliteAndGeneric().VerifiedStmt("BEGIN IMMEDIATE");
        SqliteAndGeneric().VerifiedStmt("BEGIN EXCLUSIVE");
    }

    [Fact]
    public void test_dollar_identifier_as_placeholder()
    {
        // This relates to the discussion in issue #291. The `$id` should be treated as a placeholder,
        // not as an identifier in SQLite dialect.
        //
        // Reference: https://www.sqlite.org/lang_expr.html#varparam
        switch (Sqlite().VerifiedExpr("id = $id"))
        {
            case Expr.BinaryOp binaryOp:
                Assert.Equal(binaryOp.Op, new BinaryOperator.Eq());
                Assert.Equal(binaryOp.Left, new Expr.Identifier(new Ident("id")));
                Assert.Equal(
                    binaryOp.Right,
                    new Expr.Value(
                        new Value.Placeholder("$id").WithEmptySpan()));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }

        // $$ is a valid placeholder in SQLite
        switch (Sqlite().VerifiedExpr("id = $$"))
        {
            case Expr.BinaryOp binaryOp:
                Assert.Equal(binaryOp.Op, new BinaryOperator.Eq());
                Assert.Equal(binaryOp.Left, new Expr.Identifier(new Ident("id")));
                Assert.Equal(
                    binaryOp.Right,
                    new Expr.Value(
                        new Value.Placeholder("$$").WithEmptySpan()));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void test_match_operator()
    {
        Assert.Equal(
            Sqlite().VerifiedExpr("col MATCH 'pattern'"),
            new Expr.BinaryOp(
                Op: new BinaryOperator.Match(),
                Left: new Expr.Identifier(new Ident("col")),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("pattern").WithEmptySpan())));
        Sqlite().VerifiedOnlySelect("SELECT * FROM email WHERE email MATCH 'fts5'");
    }

    [Fact]
    public void test_regexp_operator()
    {
        Assert.Equal(
            Sqlite().VerifiedExpr("col REGEXP 'pattern'"),
            new Expr.BinaryOp(
                Op: new BinaryOperator.Regexp(),
                Left: new Expr.Identifier(new Ident("col")),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("pattern").WithEmptySpan())));
        Sqlite().VerifiedOnlySelect(@"SELECT count(*) FROM messages WHERE msg_text REGEXP '\d+'");

        // Should return an error, not panic
        foreach (var sql in new[] { "SELECT 1 REGEXP", "SELECT 1 MATCH" })
        {
            Assert.Equal(
                Assert.Throws<ParserException>(() => Sqlite().ParseSqlStatements(sql)).Error,
                new ParserError.ParserErrorValue(
                    "Expected: an expression, found: EOF"));
        }
    }

    [Fact]
    public void test_glob_operator()
    {
        Assert.Equal(
            Sqlite().VerifiedExpr("col GLOB 'pattern'"),
            new Expr.BinaryOp(
                Op: new BinaryOperator.Glob(),
                Left: new Expr.Identifier(new Ident("col")),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("pattern").WithEmptySpan())));
        Sqlite().VerifiedOnlySelect(@"SELECT count(*) FROM files WHERE name GLOB '*.txt'");

        // Should return an error, not panic
        Assert.Equal(
            Assert.Throws<ParserException>(() => Sqlite().ParseSqlStatements("SELECT 1 GLOB")).Error,
            new ParserError.ParserErrorValue(
                "Expected: an expression, found: EOF"));
    }

    [Fact]
    public void test_update_delete_limit()
    {
        switch (Sqlite().VerifiedStmt("UPDATE foo SET bar = 1 LIMIT 99"))
        {
            case Statement.Update { Value: var update }:
                Assert.Equal(update.Limit, Expr.MakeValue(TestUtils.Number("99")));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }

        switch (Sqlite().VerifiedStmt("DELETE FROM foo LIMIT 99"))
        {
            case Statement.Delete { Value: var delete }:
                Assert.Equal(delete.Limit, Expr.MakeValue(TestUtils.Number("99")));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void test_create_trigger()
    {
        var statement1 = "CREATE TRIGGER trg_inherit_asset_models AFTER INSERT ON assets FOR EACH ROW BEGIN INSERT INTO users (name) SELECT pam.name FROM users AS pam; END";

        switch (Sqlite().VerifiedStmt(statement1))
        {
            case Statement.CreateTrigger { Value: var trigger }:
                Assert.True(!trigger.OrAlter);
                Assert.True(!trigger.Temporary);
                Assert.True(!trigger.OrReplace);
                Assert.True(!trigger.IsConstraint);
                Assert.Equal("trg_inherit_asset_models", trigger.Name.ToString());
                Assert.Equal(trigger.Period, new TriggerPeriod.After());
                Assert.True(trigger.PeriodBeforeTable);
                Assert.Equal(trigger.Events, Sequence.Of<TriggerEvent>(new TriggerEvent.Insert()));
                Assert.Equal("assets", trigger.TableName.ToString());
                Assert.True(trigger.ReferencedTableName is null);
                Assert.True(trigger.Referencing.IsEmpty);
                Assert.Equal(
                    trigger.TriggerObject,
                    new TriggerObjectKind.ForEach(new TriggerObject.Row()));
                Assert.True(trigger.Condition is null);
                Assert.True(!trigger.StatementsAs);
                Assert.True(trigger.Characteristics is null);
                break;
            default:
                Assert.Fail($"{Unreachable}: Expected CREATE TRIGGER statement");
                break;
        }

        // Here we check that the variant of CREATE TRIGGER that omits the `FOR EACH ROW` clause,
        // which in SQLite may be implicitly assumed, is parsed correctly.
        var statement2 = "CREATE TRIGGER log_new_user AFTER INSERT ON users BEGIN INSERT INTO user_log (user_id, action, timestamp) VALUES (NEW.id, 'created', datetime('now')); END";

        switch (Sqlite().VerifiedStmt(statement2))
        {
            case Statement.CreateTrigger { Value: var trigger }:
                Assert.True(!trigger.OrAlter);
                Assert.True(!trigger.Temporary);
                Assert.True(!trigger.OrReplace);
                Assert.True(!trigger.IsConstraint);
                Assert.Equal("log_new_user", trigger.Name.ToString());
                Assert.Equal(trigger.Period, new TriggerPeriod.After());
                Assert.True(trigger.PeriodBeforeTable);
                Assert.Equal(trigger.Events, Sequence.Of<TriggerEvent>(new TriggerEvent.Insert()));
                Assert.Equal("users", trigger.TableName.ToString());
                Assert.True(trigger.ReferencedTableName is null);
                Assert.True(trigger.Referencing.IsEmpty);
                Assert.True(trigger.TriggerObject is null);
                Assert.True(trigger.Condition is null);
                Assert.True(!trigger.StatementsAs);
                Assert.True(trigger.Characteristics is null);
                break;
            default:
                Assert.Fail($"{Unreachable}: Expected CREATE TRIGGER statement");
                break;
        }

        var statement3 = "CREATE TRIGGER cleanup_orders AFTER DELETE ON customers BEGIN DELETE FROM orders WHERE customer_id = OLD.id; DELETE FROM invoices WHERE customer_id = OLD.id; END";
        switch (Sqlite().VerifiedStmt(statement3))
        {
            case Statement.CreateTrigger { Value: var trigger }:
                Assert.True(!trigger.OrAlter);
                Assert.True(!trigger.Temporary);
                Assert.True(!trigger.OrReplace);
                Assert.True(!trigger.IsConstraint);
                Assert.Equal("cleanup_orders", trigger.Name.ToString());
                Assert.Equal(trigger.Period, new TriggerPeriod.After());
                Assert.True(trigger.PeriodBeforeTable);
                Assert.Equal(trigger.Events, Sequence.Of<TriggerEvent>(new TriggerEvent.Delete()));
                Assert.Equal("customers", trigger.TableName.ToString());
                Assert.True(trigger.ReferencedTableName is null);
                Assert.True(trigger.Referencing.IsEmpty);
                Assert.True(trigger.TriggerObject is null);
                Assert.True(trigger.Condition is null);
                Assert.True(!trigger.StatementsAs);
                Assert.True(trigger.Characteristics is null);
                break;
            default:
                Assert.Fail($"{Unreachable}: Expected CREATE TRIGGER statement");
                break;
        }

        var statement4 = "CREATE TRIGGER trg_before_update BEFORE UPDATE ON products FOR EACH ROW WHEN NEW.price < 0 BEGIN SELECT RAISE(ABORT, 'Price cannot be negative'); END";
        switch (Sqlite().VerifiedStmt(statement4))
        {
            case Statement.CreateTrigger { Value: var trigger }:
                Assert.True(!trigger.OrAlter);
                Assert.True(!trigger.Temporary);
                Assert.True(!trigger.OrReplace);
                Assert.True(!trigger.IsConstraint);
                Assert.Equal("trg_before_update", trigger.Name.ToString());
                Assert.Equal(trigger.Period, new TriggerPeriod.Before());
                Assert.True(trigger.PeriodBeforeTable);
                Assert.Equal(
                    trigger.Events,
                    Sequence.Of<TriggerEvent>(new TriggerEvent.Update(Sequence<Ident>.Empty)));
                Assert.Equal("products", trigger.TableName.ToString());
                Assert.True(trigger.ReferencedTableName is null);
                Assert.True(trigger.Referencing.IsEmpty);
                Assert.Equal(
                    trigger.TriggerObject,
                    new TriggerObjectKind.ForEach(new TriggerObject.Row()));
                Assert.True(trigger.Condition is not null);
                Assert.True(!trigger.StatementsAs);
                Assert.True(trigger.Characteristics is null);
                break;
            default:
                Assert.Fail($"{Unreachable}: Expected CREATE TRIGGER statement");
                break;
        }

        // We test a INSTEAD OF trigger on a view
        var statement5 = "CREATE TRIGGER trg_instead_of_insert INSTEAD OF INSERT ON my_view BEGIN INSERT INTO my_table (col1, col2) VALUES (NEW.col1, NEW.col2); END";
        switch (Sqlite().VerifiedStmt(statement5))
        {
            case Statement.CreateTrigger { Value: var trigger }:
                Assert.True(!trigger.OrAlter);
                Assert.True(!trigger.Temporary);
                Assert.True(!trigger.OrReplace);
                Assert.True(!trigger.IsConstraint);
                Assert.Equal("trg_instead_of_insert", trigger.Name.ToString());
                Assert.Equal(trigger.Period, new TriggerPeriod.InsteadOf());
                Assert.True(trigger.PeriodBeforeTable);
                Assert.Equal(trigger.Events, Sequence.Of<TriggerEvent>(new TriggerEvent.Insert()));
                Assert.Equal("my_view", trigger.TableName.ToString());
                Assert.True(trigger.ReferencedTableName is null);
                Assert.True(trigger.Referencing.IsEmpty);
                Assert.True(trigger.TriggerObject is null);
                Assert.True(trigger.Condition is null);
                Assert.True(!trigger.StatementsAs);
                Assert.True(trigger.Characteristics is null);
                break;
            default:
                Assert.Fail($"{Unreachable}: Expected CREATE TRIGGER statement");
                break;
        }

        // We test a temporary trigger
        var statement6 = "CREATE TEMPORARY TRIGGER temp_trigger AFTER INSERT ON temp_table BEGIN UPDATE log_table SET count = count + 1; END";
        switch (Sqlite().VerifiedStmt(statement6))
        {
            case Statement.CreateTrigger { Value: var trigger }:
                Assert.True(!trigger.OrAlter);
                Assert.True(trigger.Temporary);
                Assert.True(!trigger.OrReplace);
                Assert.True(!trigger.IsConstraint);
                Assert.Equal("temp_trigger", trigger.Name.ToString());
                Assert.Equal(trigger.Period, new TriggerPeriod.After());
                Assert.True(trigger.PeriodBeforeTable);
                Assert.Equal(trigger.Events, Sequence.Of<TriggerEvent>(new TriggerEvent.Insert()));
                Assert.Equal("temp_table", trigger.TableName.ToString());
                Assert.True(trigger.ReferencedTableName is null);
                Assert.True(trigger.Referencing.IsEmpty);
                Assert.True(trigger.TriggerObject is null);
                Assert.True(trigger.Condition is null);
                Assert.True(!trigger.StatementsAs);
                Assert.True(trigger.Characteristics is null);
                break;
            default:
                Assert.Fail($"{Unreachable}: Expected CREATE TRIGGER statement");
                break;
        }

        // We test a trigger defined without a period (BEFORE/AFTER/INSTEAD OF)
        var statement7 = "CREATE TRIGGER trg_inherit_asset_models INSERT ON assets FOR EACH ROW BEGIN INSERT INTO users (name) SELECT pam.name FROM users AS pam; END";
        Sqlite().VerifiedStmt(statement7);
    }

    [Fact]
    public void test_drop_trigger()
    {
        var statement = "DROP TRIGGER IF EXISTS trg_inherit_asset_models";

        switch (Sqlite().VerifiedStmt(statement))
        {
            case Statement.DropTrigger { Value: var dropTrigger }:
                Assert.True(dropTrigger.IfExists);
                Assert.Equal("trg_inherit_asset_models", dropTrigger.TriggerName.ToString());
                Assert.True(dropTrigger.TableName is null);
                Assert.True(dropTrigger.Option is null);
                break;
            default:
                Assert.Fail($"{Unreachable}: Expected DROP TRIGGER statement");
                break;
        }
    }

    [Fact]
    public void parse_pattern_operators_bind_at_like_precedence()
    {
        static BinaryOperator WhereOperator(string sql)
        {
            // `let P = x else { panic!(..) };` numa função que devolve valor → `throw` com a mesma mensagem
            if (Sqlite().VerifiedStmt(sql) is not Statement.Query { Value: var query })
            {
                throw new InvalidOperationException("expected a query");
            }
            if (query.Body is not SetExpr.Select { Value: var select })
            {
                throw new InvalidOperationException("expected a select");
            }
            if (select.Selection is not Expr.BinaryOp { Op: var op })
            {
                throw new InvalidOperationException("expected a WHERE binary operator");
            }
            return op;
        }

        // Above AND, so the pattern does not swallow the rest of the expression.
        foreach (var @operator in new[] { "REGEXP", "MATCH", "GLOB", "LIKE" })
        {
            var sql = $"SELECT 1 FROM t WHERE a {@operator} 'p' AND b = 1";
            // assert_eq!(.., "{operator}"): a mensagem extra não tem equivalente no Assert.Equal do xUnit
            Assert.Equal(WhereOperator(sql), new BinaryOperator.And());
        }
        // Below string concatenation, so the pattern is not cut short either.
        foreach (var (@operator, expected) in new (string, BinaryOperator)[]
        {
            ("REGEXP", new BinaryOperator.Regexp()),
            ("MATCH", new BinaryOperator.Match()),
            ("GLOB", new BinaryOperator.Glob()),
        })
        {
            var sql = $"SELECT 1 FROM t WHERE a {@operator} 'p' || 'q'";
            Assert.Equal(WhereOperator(sql), expected);
        }
    }

    [Fact]
    public void parse_update_set_double_eq()
    {
        // SQLite treats `==` as `=` in all positions, including SET assignments.
        Sqlite().OneStatementParsesTo("UPDATE t SET a == 1", "UPDATE t SET a = 1");
        Sqlite().OneStatementParsesTo("UPDATE t SET a == 1, b == 2", "UPDATE t SET a = 1, b = 2");
        // `=` still works
        Sqlite().VerifiedStmt("UPDATE t SET a = 1");
        // Other dialects reject `==` in SET
        var res = new ParserError.ParserErrorValue("Expected: =, found: ==");
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => TestUtils.AllDialectsExcept(d => d.SupportsDoubleEqAssignment)
                .ParseSqlStatements("UPDATE t SET a == 1")).Error,
            res);
    }

    [Fact]
    public void test_non_bmp_identifiers()
    {
        // SQLite tokenizer treats every byte >= 0x80 as an identifier character,
        // so any Unicode code point above U+007F is a valid identifier start/part.
        // Caracteres literais, como no .rs: U+DFFFE (invisível), U+12000 e U+12001.
        Sqlite().VerifiedStmt("SELECT 󟿾");
        Sqlite().VerifiedStmt("SELECT 𒀀");
        Sqlite().VerifiedStmt("SELECT 𒀀𒀁");
        // U+DFFFE is not alphabetic, so GenericDialect rejects it as an identifier.
        Assert.Equal(
            Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), "SELECT \U000DFFFE")).Error,
            new ParserError.ParserErrorValue(
                "Expected: an expression, found: \U000DFFFE at Line: 1, Column: 8"));
        // SQLite rejects U+007F as an unrecognized token.
        Assert.Equal(
            Assert.Throws<ParserException>(() => Sqlite().ParseSqlStatements("SELECT \u007f")).Error,
            new ParserError.ParserErrorValue(
                "Expected: an expression, found: \u007f"));
    }

    [Fact]
    public void parse_create_table_string_column_names()
    {
        Sqlite().VerifiedStmt("CREATE TABLE t ('a')");
        Sqlite().VerifiedStmt("""CREATE TABLE '""' ('id' INT UNSIGNED NOT NULL)""");
        Sqlite().VerifiedStmt(
            """CREATE TABLE '""' ('id' INT UNSIGNED NOT NULL, 'name' TEXT NOT NULL, 'zip' INT UNSIGNED NULL)""");
        // Generic dialect does not support this
        Assert.Equal(
            Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), "CREATE TABLE t ('a')")).Error,
            new ParserError.ParserErrorValue(
                "Expected: column name or constraint definition, found: 'a' at Line: 1, Column: 17"));
    }

    [Fact]
    public void test_cast_empty_type()
    {
        // SQLite allows CAST(expr AS) with an empty type name (typetoken can be empty)
        // See https://www.sqlite.org/lang_expr.html
        Sqlite().VerifiedStmt("SELECT CAST(a AS)");

        // Rejected by dialects without the flag
        Assert.Equal(
            Assert.Throws<ParserException>(() => new TestedDialects(new List<Dialect> { new GenericDialect() })
                .ParseSqlStatements("SELECT CAST(a AS)")).Error,
            new ParserError.ParserErrorValue("Expected: a data type name, found: )"));
    }

    [Fact]
    public void parse_n_prefix_not_national_string()
    {
        // In SQLite, `n'...'` is the identifier `n` followed by a string literal.
        // The string becomes an implicit alias, so `t.n''` round-trips as `t.n AS ''`.
        Sqlite().OneStatementParsesTo("SELECT t.n'' FROM t", "SELECT t.n AS '' FROM t");
        Sqlite().OneStatementParsesTo("SELECT n'' FROM t", "SELECT n AS '' FROM t");
        Sqlite().OneStatementParsesTo("SELECT N'hello'", "SELECT N AS 'hello'");

        // Other dialects still tokenize N'...' as a national string literal.
        TestUtils.AllDialectsWhere(d => d.SupportsNationalStringLiteral).VerifiedStmt("SELECT N'hello'");
    }

    [Fact]
    public void parse_create_table_options_list()
    {
        foreach (var (suffix, leadingComma, options) in new (string, bool, Sequence<SqliteTableOption>)[]
        {
            (" WITHOUT ROWID, STRICT", false, Sequence.Of<SqliteTableOption>(new SqliteTableOption.WithoutRowid(), new SqliteTableOption.Strict())),
            (" STRICT, WITHOUT ROWID", false, Sequence.Of<SqliteTableOption>(new SqliteTableOption.Strict(), new SqliteTableOption.WithoutRowid())),
            (" STRICT, STRICT", false, Sequence.Of<SqliteTableOption>(new SqliteTableOption.Strict(), new SqliteTableOption.Strict())),
            (", STRICT", true, Sequence.Of<SqliteTableOption>(new SqliteTableOption.Strict())),
            (", WITHOUT ROWID, STRICT", true, Sequence.Of<SqliteTableOption>(new SqliteTableOption.WithoutRowid(), new SqliteTableOption.Strict())),
        })
        {
            var sql = $"CREATE TABLE t (a INT){suffix}";
            switch (SqliteAndGeneric().VerifiedStmt(sql))
            {
                case Statement.CreateTable { Value: var createTable }:
                    TestUtils.AssertEqualWithMessage(
                        createTable.SqliteTableOptions,
                        new SqliteTableOptions(
                            LeadingComma: leadingComma,
                            Options: options),
                        sql);
                    break;
                default:
                    throw new InvalidOperationException(Unreachable);
            }
        }
        SqliteAndGeneric().OneStatementParsesTo(
            "CREATE TABLE t (a INT) , STRICT",
            "CREATE TABLE t (a INT), STRICT");

        foreach (var (suffix, error) in new (string, string)[]
        {
            (
                " WITHOUT ROWID STRICT",
                "Expected: end of statement, found: STRICT"
            ),
            (
                " STRICT WITHOUT ROWID",
                "Expected: end of statement, found: WITHOUT"
            ),
            (" STRICT,", "Expected: WITHOUT ROWID or STRICT, found: EOF"),
            (" STRICT, WITHOUT", "Expected: ROWID, found: EOF"),
            (",", "Expected: end of statement, found: ,"),
            (", , STRICT", "Expected: end of statement, found: ,"),
        })
        {
            var sql = $"CREATE TABLE t (a INT){suffix}";
            var actual = Assert.Throws<ParserException>(() => SqliteAndGeneric().ParseSqlStatements(sql)).Error;
            // "{sql}: {actual}"
            Assert.Contains(error, actual.ToString());
        }
    }

    [Fact]
    public void parse_parenthesized_table_factor()
    {
        foreach (var (sql, canonical) in new (string, string)[]
        {
            ("SELECT * FROM (t)", "SELECT * FROM t"),
            ("SELECT * FROM ((t))", "SELECT * FROM t"),
            ("SELECT * FROM (t) AS a", "SELECT * FROM t AS a"),
            ("SELECT * FROM (t AS a)", "SELECT * FROM t AS a"),
            (
                "SELECT * FROM ((SELECT 1) AS s)",
                "SELECT * FROM (SELECT 1) AS s"
            ),
            ("SELECT * FROM f(), (t)", "SELECT * FROM f(), t"),
        })
        {
            SqliteAndGeneric().OneStatementParsesTo(sql, canonical);
        }

        Assert.Equal(
            Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => !d.SupportsParensAroundTableFactor)
                .ParseSqlStatements("SELECT * FROM (t)")).Error,
            new ParserError.ParserErrorValue(
                "Expected: joined table, found: )"));
    }

    [Fact]
    public void parse_unterminated_block_comment()
    {
        Sqlite().OneStatementParsesTo("SELECT 1 /* x", "SELECT 1");
        Sqlite().OneStatementParsesTo("SELECT 1 /* x;", "SELECT 1");
        Assert.Empty(Sqlite().ParseSqlStatements("/* ;"));

        Assert.Equal(
            Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => !d.SupportsUnterminatedBlockComments)
                .ParseSqlStatements("SELECT 1 /* x")).Error,
            new ParserError.TokenizerError(
                "Unexpected EOF while in a multi-line comment at Line: 1, Column: 14"));
    }

    private static TestedDialects Sqlite() =>
        new TestedDialects(new List<Dialect> { new SQLiteDialect() });

    private static TestedDialects SqliteWithOptions(ParserOptions options) =>
        TestedDialects.NewWithOptions(new List<Dialect> { new SQLiteDialect() }, options);

    private static TestedDialects SqliteAndGeneric() =>
        new TestedDialects(new List<Dialect>
        {
            new SQLiteDialect(),
            new GenericDialect(),
        });

    [Fact]
    public void test_dollar_sign_in_identifier()
    {
        // SQLite allows $ anywhere after the first character of an identifier
        Sqlite().VerifiedStmt("SELECT a$ FROM t");
        Sqlite().VerifiedStmt("SELECT 1 AS a$");
        Sqlite().OneStatementParsesTo("CREATE TABLE t(a$ INT)", "CREATE TABLE t (a$ INT)");
        Sqlite().VerifiedStmt("INSERT INTO t$ VALUES (1)");
        // AS$ is parsed as the alias name
        Sqlite().OneStatementParsesTo("SELECT a AS$", "SELECT a AS AS$");
        // Dialects without $ as an identifier character still reject these identifiers
        Assert.Throws<ParserException>(() => new TestedDialects(new List<Dialect> { new AnsiDialect() })
            .ParseSqlStatements("SELECT a$ FROM t"));
    }

    [Fact]
    public void parse_bitwise_and_or_share_precedence()
    {
        static Expr Num(string n) => Expr.MakeValue(TestUtils.Number(n));
        static Expr Bin(Expr left, BinaryOperator op, Expr right) =>
            new Expr.BinaryOp(
                Left: left,
                Op: op,
                Right: right);

        foreach (var (sql, expected) in new (string, Expr)[]
        {
            (
                "6 | 1 & 3",
                Bin(Bin(Num("6"), new BinaryOperator.BitwiseOr(), Num("1")), new BinaryOperator.BitwiseAnd(), Num("3"))
            ),
            (
                "6 & 3 | 8",
                Bin(Bin(Num("6"), new BinaryOperator.BitwiseAnd(), Num("3")), new BinaryOperator.BitwiseOr(), Num("8"))
            ),
            (
                "1 & 2 & 3",
                Bin(Bin(Num("1"), new BinaryOperator.BitwiseAnd(), Num("2")), new BinaryOperator.BitwiseAnd(), Num("3"))
            ),
            (
                "1 | 2 | 3",
                Bin(Bin(Num("1"), new BinaryOperator.BitwiseOr(), Num("2")), new BinaryOperator.BitwiseOr(), Num("3"))
            ),
            (
                "1 | 2 + 3 & 4 * 5",
                Bin(
                    Bin(Num("1"), new BinaryOperator.BitwiseOr(), Bin(Num("2"), new BinaryOperator.Plus(), Num("3"))),
                    new BinaryOperator.BitwiseAnd(),
                    Bin(Num("4"), new BinaryOperator.Multiply(), Num("5")))
            ),
            (
                "6 | (1 & 3)",
                Bin(
                    Num("6"),
                    new BinaryOperator.BitwiseOr(),
                    new Expr.Nested(Bin(Num("1"), new BinaryOperator.BitwiseAnd(), Num("3"))))
            ),
            (
                "1 | 2 = 3 & 4",
                Bin(
                    Bin(Num("1"), new BinaryOperator.BitwiseOr(), Num("2")),
                    new BinaryOperator.Eq(),
                    Bin(Num("3"), new BinaryOperator.BitwiseAnd(), Num("4")))
            ),
        })
        {
            TestUtils.AssertEqualWithMessage(Sqlite().VerifiedExpr(sql), expected, sql);
        }
    }
}
