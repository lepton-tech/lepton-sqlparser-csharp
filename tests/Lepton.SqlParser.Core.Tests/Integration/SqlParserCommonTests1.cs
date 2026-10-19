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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_common.rs (linhas 1–4319).
// Modificado: tradução de Rust para C#.

using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

// Notas da tradução (onda-c/C1.md):
// - Cada fatia de sqlparser_common.rs é uma classe própria (não partial). As funções de arquivo usadas
//   aqui (`verified_stmt`, `parse_sql_statements`, `pg_and_generic`...) foram copiadas como métodos
//   `private static`, mesmo as definidas fora das linhas 1–4319.
// - `cfg!(feature = "bigdecimal")` é falso no port (DIV-006): `Value::Number` guarda o texto
//   (TestUtils.Number); só a variante sem a feature foi portada.
// - `match x { P => .., _ => unreachable!() }` vira `switch` com `default: throw`.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal/constante: o literal
//   vai na posição "expected" (xUnit2000). `assert_eq!(x, None)` → `Assert.Null(x)`.
#pragma warning disable xUnit2013
public class SqlParserCommonTests1
{
    [Fact]
    public void parse_numeric_literal_underscore()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsNumericLiteralUnderscores);

        // cfg!(feature = "bigdecimal") é falso no port.
        var canonical = "SELECT 10_000";

        var select = dialects.VerifiedOnlySelectWithCanonical("SELECT 10_000", canonical);

        Assert.Equal(
            select.Projection,
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Value(TestUtils.Number("10_000").WithEmptySpan()))));

        foreach (var (sql, column) in new[]
                 {
                     ("SELECT 10__00", 10),
                     ("SELECT 10_00_", 13),
                     ("SELECT 1._000", 10),
                     ("SELECT 1_a", 9),
                     ("SELECT 1e_1", 10),
                     ("SELECT 1e1_", 11),
                 })
        {
            var err = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql)).Error;
            Assert.Equal(
                $"sql parser error: Unexpected character '_' at Line: 1, Column: {column}",
                err.ToString());
        }
    }

    [Fact]
    public void parse_function_object_name()
    {
        var select = VerifiedOnlySelect("SELECT a.b.c.d(1, 2, 3) FROM T");
        if (TestUtils.ExprFromProjection(select.Projection[0]) is not Expr.Function { Value: var func })
        {
            throw new InvalidOperationException("unreachable!()");
        }
        Assert.Equal(
            ObjectName.From(
                new[] { "a", "b", "c", "d" }
                    .Select(s => new Ident(s))
                    .ToSequence()),
            func.Name);
    }

    [Fact]
    public void parse_insert_values()
    {
        var row = Sequence.Of(
            Expr.MakeValue(TestUtils.Number("1")),
            Expr.MakeValue(TestUtils.Number("2")),
            Expr.MakeValue(TestUtils.Number("3")));
        var rows1 = new[] { Parens<Sequence<Expr>>.WithEmptySpan(row) };
        var rows2 = new[]
        {
            Parens<Sequence<Expr>>.WithEmptySpan(row),
            Parens<Sequence<Expr>>.WithEmptySpan(row),
        };

        var sql = "INSERT customer VALUES (1, 2, 3)";
        CheckOne(sql, "customer", new string[] { }, rows1, false);

        sql = "INSERT INTO customer VALUES (1, 2, 3)";
        CheckOne(sql, "customer", new string[] { }, rows1, false);

        sql = "INSERT INTO customer VALUES (1, 2, 3), (1, 2, 3)";
        CheckOne(sql, "customer", new string[] { }, rows2, false);

        sql = "INSERT INTO public.customer VALUES (1, 2, 3)";
        CheckOne(sql, "public.customer", new string[] { }, rows1, false);

        sql = "INSERT INTO db.public.customer VALUES (1, 2, 3)";
        CheckOne(sql, "db.public.customer", new string[] { }, rows1, false);

        sql = "INSERT INTO public.customer (id, name, active) VALUES (1, 2, 3)";
        CheckOne(
            sql,
            "public.customer",
            new[] { "id", "name", "active" },
            rows1,
            false);

        sql = "INSERT INTO t (id, name, active) VALUE (1, 2, 3)";
        CheckOne(
            sql,
            "t",
            new[] { "id", "name", "active" },
            rows1,
            true);

        static void CheckOne(
            string sql,
            string expectedTableName,
            string[] expectedColumns,
            Parens<Sequence<Expr>>[] expectedRows,
            bool expectedValueKeyword)
        {
            switch (VerifiedStmt(sql))
            {
                case Statement.Insert { Value: { Source: { } source } insert }:
                {
                    var tableName = insert.Table;
                    var columns = insert.Columns;
                    Assert.Equal(tableName.ToString(), expectedTableName);
                    Assert.Equal(columns.Count, expectedColumns.Length);
                    for (var index = 0; index < columns.Count; index++)
                    {
                        var column = columns[index];
                        Assert.Equal(
                            column,
                            ObjectName.From(new Ident(expectedColumns[index])));
                    }
                    switch (source.Body)
                    {
                        case SetExpr.Values { Value: var values }:
                            Assert.Equal(values.Rows.ToArray(), expectedRows);
                            Assert.True(values.ValueKeyword == expectedValueKeyword);
                            break;
                        default:
                            throw new InvalidOperationException("unreachable!()");
                    }
                    break;
                }
                default:
                    throw new InvalidOperationException("unreachable!()");
            }
        }

        VerifiedStmt("INSERT INTO customer WITH foo AS (SELECT 1) SELECT * FROM foo UNION VALUES (1)");
    }

    [Fact]
    public void parse_insert_set()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsInsertSet);
        dialects.VerifiedStmt("INSERT INTO tbl1 SET col1 = 1, col2 = 'abc', col3 = current_date()");
    }

    [Fact]
    public void parse_replace_into()
    {
        var dialect = new PostgreSqlDialect();
        var sql = "REPLACE INTO public.customer (id, name, active) VALUES (1, 2, 3)";

        Assert.Equal(
            new ParserError.ParserErrorValue("Unsupported statement REPLACE at Line: 1, Column: 9"),
            Assert.Throws<ParserException>(() => Parser.ParseSql(dialect, sql)).Error);
    }

    [Fact]
    public void parse_insert_default_values()
    {
        var insertWithDefaultValues = VerifiedStmt("INSERT INTO test_table DEFAULT VALUES");

        switch (insertWithDefaultValues)
        {
            case Statement.Insert { Value: var insert }:
                Assert.Equal(insert.Columns, Sequence<ObjectName>.Empty);
                Assert.Equal(insert.AfterColumns, Sequence<Ident>.Empty);
                Assert.Null(insert.On);
                Assert.Null(insert.Partitioned);
                Assert.Null(insert.Returning);
                Assert.Null(insert.Source);
                Assert.Equal(
                    insert.Table,
                    new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("test_table")))));
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }

        var insertWithDefaultValuesAndReturning =
            VerifiedStmt("INSERT INTO test_table DEFAULT VALUES RETURNING test_column");

        switch (insertWithDefaultValuesAndReturning)
        {
            case Statement.Insert { Value: var insert }:
                Assert.Equal(insert.AfterColumns, Sequence<Ident>.Empty);
                Assert.Equal(insert.Columns, Sequence<ObjectName>.Empty);
                Assert.Null(insert.On);
                Assert.Null(insert.Partitioned);
                Assert.True(insert.Returning is not null);
                Assert.Null(insert.Source);
                Assert.Equal(
                    insert.Table,
                    new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("test_table")))));
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }

        var insertWithDefaultValuesAndOnConflict =
            VerifiedStmt("INSERT INTO test_table DEFAULT VALUES ON CONFLICT DO NOTHING");

        switch (insertWithDefaultValuesAndOnConflict)
        {
            case Statement.Insert { Value: var insert }:
                Assert.Equal(insert.AfterColumns, Sequence<Ident>.Empty);
                Assert.Equal(insert.Columns, Sequence<ObjectName>.Empty);
                Assert.True(insert.On is not null);
                Assert.Null(insert.Partitioned);
                Assert.Null(insert.Returning);
                Assert.Null(insert.Source);
                Assert.Equal(
                    insert.Table,
                    new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("test_table")))));
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }

        var insertWithColumnsAndDefaultValues = "INSERT INTO test_table (test_col) DEFAULT VALUES";
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: DEFAULT"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(insertWithColumnsAndDefaultValues)).Error);

        var insertWithDefaultValuesAndHiveAfterColumns =
            "INSERT INTO test_table DEFAULT VALUES (some_column)";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: ("),
            Assert.Throws<ParserException>(() => ParseSqlStatements(insertWithDefaultValuesAndHiveAfterColumns)).Error);

        var insertWithDefaultValuesAndHivePartition =
            "INSERT INTO test_table DEFAULT VALUES PARTITION (some_column)";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: PARTITION"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(insertWithDefaultValuesAndHivePartition)).Error);

        var insertWithDefaultValuesAndValuesList = "INSERT INTO test_table DEFAULT VALUES (1)";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: ("),
            Assert.Throws<ParserException>(() => ParseSqlStatements(insertWithDefaultValuesAndValuesList)).Error);
    }

    [Fact]
    public void parse_insert_select_returning()
    {
        // Dialects that support `RETURNING` as a column identifier do
        // not support this syntax.
        var dialects =
            TestUtils.AllDialectsWhere(d => !d.IsColumnAlias(Keyword.RETURNING, new Parser(d)));

        dialects.VerifiedStmt("INSERT INTO t SELECT 1 RETURNING 2");
        var stmt = dialects.VerifiedStmt("INSERT INTO t SELECT x RETURNING x AS y");
        switch (stmt)
        {
            case Statement.Insert { Value: { Returning: { } ret, Source: not null } }:
                Assert.Equal(1, ret.Count);
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_insert_select_from_returning()
    {
        var sql = "INSERT INTO table1 SELECT * FROM table2 RETURNING id";
        switch (VerifiedStmt(sql))
        {
            case Statement.Insert
            {
                Value:
                {
                    Table: TableObject.TableName { Value: var tableName },
                    Source: { } source,
                    Returning: { } returning,
                }
            }:
                Assert.Equal("table1", tableName.ToString());
                Assert.True(source.Body is SetExpr.Select);
                Assert.Equal(
                    returning,
                    Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("id")))));
                break;
            case var badStmt:
                throw new InvalidOperationException($"Expected valid insert, got {badStmt}");
        }
    }

    [Fact]
    public void parse_insert_nested_parenthesized_subquery()
    {
        // The source query may be wrapped in one or more layers of parentheses;
        // the leading parens must not be mistaken for a column list.
        VerifiedStmt("INSERT INTO t ((SELECT 1))");
        VerifiedStmt("INSERT INTO t (((SELECT 1)))");
    }

    [Fact]
    public void parse_returning_as_column_alias()
    {
        VerifiedStmt("SELECT 1 AS RETURNING");
    }

    [Fact]
    public void parse_insert_sqlite()
    {
        var dialect = new SQLiteDialect();

        void Check(string sql, SqliteOnConflict? expectedAction)
        {
            var statements = Parser.ParseSql(dialect, sql);
            switch (statements.Last)
            {
                case Statement.Insert { Value: var insert }:
                    Assert.Equal(insert.Or, expectedAction);
                    break;
                default:
                    Assert.Fail(sql); // panic!("{}", sql)
                    break;
            }
        }

        var sql = "INSERT INTO test_table(id) VALUES(1)";
        Check(sql, null);

        sql = "REPLACE INTO test_table(id) VALUES(1)";
        Check(sql, new SqliteOnConflict.Replace());

        sql = "INSERT OR REPLACE INTO test_table(id) VALUES(1)";
        Check(sql, new SqliteOnConflict.Replace());

        sql = "INSERT OR ROLLBACK INTO test_table(id) VALUES(1)";
        Check(sql, new SqliteOnConflict.Rollback());

        sql = "INSERT OR ABORT INTO test_table(id) VALUES(1)";
        Check(sql, new SqliteOnConflict.Abort());

        sql = "INSERT OR FAIL INTO test_table(id) VALUES(1)";
        Check(sql, new SqliteOnConflict.Fail());

        sql = "INSERT OR IGNORE INTO test_table(id) VALUES(1)";
        Check(sql, new SqliteOnConflict.Ignore());
    }

    [Fact]
    public void parse_update()
    {
        var sql = "UPDATE t SET a = 1, b = 2, c = 3 WHERE d";
        switch (VerifiedStmt(sql))
        {
            case Statement.Update { Value: var update }:
                Assert.Equal("t", update.Table.ToString());
                Assert.Equal(
                    update.Assignments,
                    Sequence.Of(
                        new Assignment(
                            Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident("a")))),
                            Value: Expr.MakeValue(TestUtils.Number("1"))),
                        new Assignment(
                            Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident("b")))),
                            Value: Expr.MakeValue(TestUtils.Number("2"))),
                        new Assignment(
                            Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident("c")))),
                            Value: Expr.MakeValue(TestUtils.Number("3")))));
                Assert.Equal(update.Selection!, new Expr.Identifier(new Ident("d")));
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }

        VerifiedStmt("UPDATE t SET a = 1, a = 2, a = 3");

        sql = "UPDATE t WHERE 1";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: SET, found: WHERE"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);

        sql = "UPDATE t SET a = 1 extrabadstuff";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: extrabadstuff"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_update_set_from()
    {
        var sql = "UPDATE t1 SET name = t2.name FROM (SELECT name, id FROM t1 GROUP BY id) AS t2 WHERE t1.id = t2.id";
        var dialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new DuckDbDialect(),
            new PostgreSqlDialect(),
            new BigQueryDialect(),
            new SnowflakeDialect(),
            new RedshiftSqlDialect(),
            new MsSqlDialect(),
            new SQLiteDialect(),
        });
        var stmt = dialects.VerifiedStmt(sql);
        Assert.Equal(
            stmt,
            new Statement.Update(new Update(
                UpdateToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Table: new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t1")))),
                    Joins: Sequence<Join>.Empty),
                Assignments: Sequence.Of(new Assignment(
                    Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident("name")))),
                    Value: new Expr.CompoundIdentifier(Sequence.Of(new Ident("t2"), new Ident("name"))))),
                From: new UpdateTableFromKind.AfterSet(Sequence.Of(new TableWithJoins(
                    Relation: new TableFactor.Derived(
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
                                Projection: Sequence.Of<SelectItem>(
                                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("name"))),
                                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("id")))),
                                Exclude: null,
                                Into: null,
                                From: Sequence.Of(new TableWithJoins(
                                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t1")))),
                                    Joins: Sequence<Join>.Empty)),
                                LateralViews: Sequence<LateralView>.Empty,
                                Prewhere: null,
                                Selection: null,
                                GroupBy: new GroupByExpr.Expressions(
                                    Sequence.Of<Expr>(new Expr.Identifier(new Ident("id"))),
                                    Sequence<GroupByWithModifier>.Empty),
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
                            OrderBy: null,
                            LimitClause: null,
                            Fetch: null,
                            Locks: Sequence<LockClause>.Empty,
                            ForClause: null,
                            Settings: null,
                            FormatClause: null,
                            PipeOperators: Sequence<PipeOperator>.Empty),
                        Alias: TestUtils.TableAlias(true, "t2"),
                        Sample: null),
                    Joins: Sequence<Join>.Empty))),
                Selection: new Expr.BinaryOp(
                    Left: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("t1"),
                        new Ident("id"))),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("t2"),
                        new Ident("id")))),
                Returning: null,
                Output: null,
                Or: null,
                OrderBy: Sequence<OrderByExpr>.Empty,
                Limit: null)));

        sql = "UPDATE T SET a = b FROM U, (SELECT foo FROM V) AS W WHERE 1 = 1";
        dialects.VerifiedStmt(sql);
    }

    [Fact]
    public void parse_update_with_table_alias()
    {
        var sql = "UPDATE users AS u SET u.username = 'new_user' WHERE u.username = 'old_user'";
        switch (VerifiedStmt(sql))
        {
            case Statement.Update { Value: { Or: null, Limit: null } update } when update.OptimizerHints.IsEmpty:
                Assert.Equal(
                    new TableWithJoins(
                        Relation: new TableFactor.Table(
                            Name: ObjectName.From(Sequence.Of(new Ident("users"))),
                            Alias: TestUtils.TableAlias(true, "u"),
                            Args: null,
                            WithHints: Sequence<Expr>.Empty,
                            Version: null,
                            Partitions: Sequence<Ident>.Empty,
                            WithOrdinality: false,
                            JsonPath: null,
                            Sample: null,
                            IndexHints: Sequence<TableIndexHints>.Empty),
                        Joins: Sequence<Join>.Empty),
                    update.Table);
                Assert.Equal(
                    Sequence.Of(new Assignment(
                        Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(
                            new Ident("u"),
                            new Ident("username")))),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("new_user").WithEmptySpan()))),
                    update.Assignments);
                Assert.Equal(
                    new Expr.BinaryOp(
                        Left: new Expr.CompoundIdentifier(Sequence.Of(
                            new Ident("u"),
                            new Ident("username"))),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.SingleQuotedString("old_user").WithEmptySpan())),
                    update.Selection);
                Assert.Null(update.Returning);
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_update_or()
    {
        void ExpectOrClause(string sql, SqliteOnConflict expectedAction)
        {
            switch (VerifiedStmt(sql))
            {
                case Statement.Update { Value: var update }:
                    Assert.Equal(update.Or, expectedAction);
                    break;
                case var other:
                    throw new InvalidOperationException($"Expected update with or, got {other}");
            }
        }
        ExpectOrClause(
            "UPDATE OR REPLACE t SET n = n + 1",
            new SqliteOnConflict.Replace());
        ExpectOrClause(
            "UPDATE OR ROLLBACK t SET n = n + 1",
            new SqliteOnConflict.Rollback());
        ExpectOrClause("UPDATE OR ABORT t SET n = n + 1", new SqliteOnConflict.Abort());
        ExpectOrClause("UPDATE OR FAIL t SET n = n + 1", new SqliteOnConflict.Fail());
        ExpectOrClause("UPDATE OR IGNORE t SET n = n + 1", new SqliteOnConflict.Ignore());
    }

    [Fact]
    public void parse_select_with_table_alias_as()
    {
        OneStatementParsesTo(
            "SELECT a, b, c FROM lineitem AS l (A, B, C)",
            "SELECT a, b, c FROM lineitem AS l (A, B, C)");
        // AS is optional
        OneStatementParsesTo(
            "SELECT a, b, c FROM lineitem l (A, B, C)",
            "SELECT a, b, c FROM lineitem l (A, B, C)");
    }

    [Fact]
    public void parse_select_with_table_alias()
    {
        var select = VerifiedOnlySelect("SELECT a, b, c FROM lineitem AS l (A, B, C)");
        Assert.Equal(
            select.Projection,
            Sequence.Of<SelectItem>(
                new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("a"))),
                new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("b"))),
                new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("c")))));
        Assert.Equal(
            select.From,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("lineitem"))),
                    Alias: new TableAlias(
                        Explicit: true,
                        Name: new Ident("l"),
                        Columns: Sequence.Of(
                            TableAliasColumnDef.FromName("A"),
                            TableAliasColumnDef.FromName("B"),
                            TableAliasColumnDef.FromName("C")),
                        At: null),
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
    public void parse_analyze()
    {
        VerifiedStmt("ANALYZE TABLE test_table");
        VerifiedStmt("ANALYZE test_table");
    }

    [Fact]
    public void parse_invalid_table_name()
    {
        Assert.Throws<ParserException>(() =>
            TestUtils.AllDialects().RunParserMethod("db.public..customer", parser =>
                parser.ParseObjectName(false)));
    }

    [Fact]
    public void parse_no_table_name()
    {
        Assert.Throws<ParserException>(() =>
            TestUtils.AllDialects().RunParserMethod("", parser => parser.ParseObjectName(false)));
    }

    [Fact]
    public void parse_delete_statement()
    {
        var sql = "DELETE FROM \"table\"";
        switch (VerifiedStmt(sql))
        {
            case Statement.Delete { Value.From: FromTable.WithFromKeyword { Value: var from } }:
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(Ident.WithQuote('"', "table")))),
                    from[0].Relation);
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_delete_without_from_error()
    {
        var sql = "DELETE \"table\" WHERE 1";

        var dialects = TestUtils.AllDialectsExcept(d =>
            d is BigQueryDialect || d is OracleDialect || d is GenericDialect);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: FROM, found: WHERE"),
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_delete_statement_for_multi_tables()
    {
        var sql = "DELETE schema1.table1, schema2.table2 FROM schema1.table1 JOIN schema2.table2 ON schema2.table2.col1 = schema1.table1.col1 WHERE schema2.table2.col2 = 1";
        var dialects = TestUtils.AllDialectsExcept(d =>
            d is BigQueryDialect || d is OracleDialect || d is GenericDialect);
        switch (dialects.VerifiedStmt(sql))
        {
            case Statement.Delete { Value: { From: FromTable.WithFromKeyword { Value: var from } } delete }:
                var tables = delete.Tables;
                Assert.Equal(
                    ObjectName.From(Sequence.Of(new Ident("schema1"), new Ident("table1"))),
                    tables[0]);
                Assert.Equal(
                    ObjectName.From(Sequence.Of(new Ident("schema2"), new Ident("table2"))),
                    tables[1]);
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                        new Ident("schema1"),
                        new Ident("table1")))),
                    from[0].Relation);
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                        new Ident("schema2"),
                        new Ident("table2")))),
                    from[0].Joins[0].Relation);
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_delete_statement_for_multi_tables_with_using()
    {
        var sql = "DELETE FROM schema1.table1, schema2.table2 USING schema1.table1 JOIN schema2.table2 ON schema2.table2.pk = schema1.table1.col1 WHERE schema2.table2.col2 = 1";
        switch (VerifiedStmt(sql))
        {
            case Statement.Delete { Value: { From: FromTable.WithFromKeyword { Value: var from }, Using: { } @using } }:
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                        new Ident("schema1"),
                        new Ident("table1")))),
                    from[0].Relation);
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                        new Ident("schema2"),
                        new Ident("table2")))),
                    from[1].Relation);
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                        new Ident("schema1"),
                        new Ident("table1")))),
                    @using[0].Relation);
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                        new Ident("schema2"),
                        new Ident("table2")))),
                    @using[0].Joins[0].Relation);
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_where_delete_statement()
    {
        var sql = "DELETE FROM foo WHERE name = 5";
        switch (VerifiedStmt(sql))
        {
            case Statement.Delete { Value: { From: FromTable.WithFromKeyword { Value: var from } } delete }:
                Assert.Equal(
                    TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("foo")))),
                    from[0].Relation);

                Assert.Null(delete.Using);
                Assert.Equal(
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("name")),
                        Op: new BinaryOperator.Eq(),
                        Right: Expr.MakeValue(TestUtils.Number("5"))),
                    delete.Selection!);
                Assert.Null(delete.Returning);
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_where_delete_with_alias_statement()
    {
        var sql = "DELETE FROM basket AS a USING basket AS b WHERE a.id < b.id";
        switch (VerifiedStmt(sql))
        {
            case Statement.Delete { Value: { From: FromTable.WithFromKeyword { Value: var from } } delete }:
                Assert.Equal(
                    new TableFactor.Table(
                        Name: ObjectName.From(Sequence.Of(new Ident("basket"))),
                        Alias: TestUtils.TableAlias(true, "a"),
                        Args: null,
                        WithHints: Sequence<Expr>.Empty,
                        Version: null,
                        Partitions: Sequence<Ident>.Empty,
                        WithOrdinality: false,
                        JsonPath: null,
                        Sample: null,
                        IndexHints: Sequence<TableIndexHints>.Empty),
                    from[0].Relation);
                Assert.Equal(
                    Sequence.Of(new TableWithJoins(
                        Relation: new TableFactor.Table(
                            Name: ObjectName.From(Sequence.Of(new Ident("basket"))),
                            Alias: TestUtils.TableAlias(true, "b"),
                            Args: null,
                            WithHints: Sequence<Expr>.Empty,
                            Version: null,
                            Partitions: Sequence<Ident>.Empty,
                            WithOrdinality: false,
                            JsonPath: null,
                            Sample: null,
                            IndexHints: Sequence<TableIndexHints>.Empty),
                        Joins: Sequence<Join>.Empty)),
                    delete.Using);
                Assert.Equal(
                    new Expr.BinaryOp(
                        Left: new Expr.CompoundIdentifier(Sequence.Of(
                            new Ident("a"),
                            new Ident("id"))),
                        Op: new BinaryOperator.Lt(),
                        Right: new Expr.CompoundIdentifier(Sequence.Of(
                            new Ident("b"),
                            new Ident("id")))),
                    delete.Selection!);
                Assert.Null(delete.Returning);
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_top_level()
    {
        VerifiedStmt("SELECT 1");
        VerifiedStmt("(SELECT 1)");
        VerifiedStmt("((SELECT 1))");
        VerifiedStmt("VALUES (1)");
        VerifiedStmt("VALUES ROW(1, NULL, 'a'), ROW(2, NULL, 'b')");
    }

    [Fact]
    public void parse_simple_select()
    {
        var sql = "SELECT id, fname, lname FROM customer WHERE id = 1 LIMIT 5";
        var select = VerifiedOnlySelect(sql);
        Assert.True(select.Distinct is null);
        Assert.Equal(3, select.Projection.Count);
        var query = VerifiedQuery(sql);
        var expectedLimitClause = new LimitClause.LimitOffset(
            Limit: Expr.MakeValue(TestUtils.Number("5")),
            Offset: null,
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal(expectedLimitClause, query.LimitClause);
    }

    [Fact]
    public void parse_limit()
    {
        VerifiedStmt("SELECT * FROM user LIMIT 1");
    }

    [Fact]
    public void parse_invalid_limit_by()
    {
        // .expect_err("BY without LIMIT")
        Assert.Throws<ParserException>(() =>
            TestUtils.AllDialects()
                .ParseSqlStatements("SELECT * FROM user BY name"));
    }

    [Fact]
    public void parse_limit_is_not_an_alias()
    {
        // In dialects supporting LIMIT it shouldn't be parsed as a table alias
        var ast = VerifiedQuery("SELECT id FROM customer LIMIT 1");
        var expectedLimitClause = new LimitClause.LimitOffset(
            Limit: Expr.MakeValue(TestUtils.Number("1")),
            Offset: null,
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal(expectedLimitClause, ast.LimitClause);

        ast = VerifiedQuery("SELECT 1 LIMIT 5");
        expectedLimitClause = new LimitClause.LimitOffset(
            Limit: Expr.MakeValue(TestUtils.Number("5")),
            Offset: null,
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal(expectedLimitClause, ast.LimitClause);
    }

    [Fact]
    public void parse_select_distinct()
    {
        var sql = "SELECT DISTINCT name FROM customer";
        var select = VerifiedOnlySelect(sql);
        Assert.True(select.Distinct is not null);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("name"))),
            TestUtils.Only(select.Projection));
    }

    [Fact]
    public void parse_select_distinct_two_fields()
    {
        var sql = "SELECT DISTINCT name, id FROM customer";
        var select = VerifiedOnlySelect(sql);
        Assert.True(select.Distinct is not null);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("name"))),
            select.Projection[0]);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("id"))),
            select.Projection[1]);
    }

    [Fact]
    public void parse_select_distinct_tuple()
    {
        var sql = "SELECT DISTINCT (name, id) FROM customer";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Tuple(Sequence.Of<Expr>(
                new Expr.Identifier(new Ident("name")),
                new Expr.Identifier(new Ident("id")))))),
            select.Projection);
    }

    [Fact]
    public void parse_outer_join_operator()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsOuterJoinOperator);

        var select = dialects.VerifiedOnlySelect("SELECT 1 FROM T WHERE a = b (+)");
        Assert.Equal(
            select.Selection,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("a")),
                Op: new BinaryOperator.Eq(),
                Right: new Expr.OuterJoin(new Expr.Identifier(new Ident("b")))));

        select = dialects.VerifiedOnlySelect("SELECT 1 FROM T WHERE t1.c1 = t2.c2.d3 (+)");
        Assert.Equal(
            select.Selection,
            new Expr.BinaryOp(
                Left: new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("t1"),
                    new Ident("c1"))),
                Op: new BinaryOperator.Eq(),
                Right: new Expr.OuterJoin(new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("t2"),
                    new Ident("c2"),
                    new Ident("d3"))))));

        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: column identifier before (+), found: 2"),
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT 1 FROM T WHERE 1 = 2 (+)")).Error);
    }

    [Fact]
    public void parse_select_distinct_on()
    {
        var sql = "SELECT DISTINCT ON (album_id) name FROM track ORDER BY album_id, milliseconds";
        var select = TestUtils.AllDialectsExcept(d => d is MySqlDialect).VerifiedOnlySelect(sql);
        Assert.Equal(
            new Distinct.On(Sequence.Of<Expr>(new Expr.Identifier(new Ident("album_id")))),
            select.Distinct);

        sql = "SELECT DISTINCT ON () name FROM track ORDER BY milliseconds";
        select = TestUtils.AllDialectsExcept(d => d is MySqlDialect).VerifiedOnlySelect(sql);
        Assert.Equal(new Distinct.On(Sequence<Expr>.Empty), select.Distinct);

        sql = "SELECT DISTINCT ON (album_id, milliseconds) name FROM track";
        select = TestUtils.AllDialectsExcept(d => d is MySqlDialect).VerifiedOnlySelect(sql);
        Assert.Equal(
            new Distinct.On(Sequence.Of<Expr>(
                new Expr.Identifier(new Ident("album_id")),
                new Expr.Identifier(new Ident("milliseconds")))),
            select.Distinct);
    }

    [Fact]
    public void parse_select_distinct_missing_paren()
    {
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: FROM"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT DISTINCT (name, id FROM customer")).Error);
    }

    [Fact]
    public void parse_select_all()
    {
        VerifiedStmt("SELECT ALL name FROM customer");
    }

    [Fact]
    public void parse_select_all_distinct()
    {
        Assert.Equal(
            new ParserError.ParserErrorValue("Cannot specify ALL then DISTINCT"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT ALL DISTINCT name FROM customer")).Error);
        Assert.Equal(
            new ParserError.ParserErrorValue("Cannot specify DISTINCT then ALL"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT DISTINCT ALL name FROM customer")).Error);
        Assert.Equal(
            new ParserError.ParserErrorValue("Cannot specify ALL then DISTINCT"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT ALL DISTINCT ON(name) name FROM customer")).Error);
    }

    [Fact]
    public void parse_select_into()
    {
        var sql = "SELECT * INTO table0 FROM table1";
        OneStatementParsesTo(sql, "SELECT * INTO table0 FROM table1");
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectInto(
                Temporary: false,
                Unlogged: false,
                Table: false,
                Targets: Sequence.Of<Expr>(new Expr.Identifier(new Ident("table0")))),
            // only(&select.into): Option iterado como coleção de 0 ou 1 item.
            TestUtils.Only(select.Into is { } into ? new[] { into } : System.Array.Empty<SelectInto>()));

        sql = "SELECT * INTO TEMPORARY UNLOGGED TABLE table0 FROM table1";
        OneStatementParsesTo(
            sql,
            "SELECT * INTO TEMPORARY UNLOGGED TABLE table0 FROM table1");

        VerifiedOnlySelect("SELECT a, b INTO foo.bar, bar.baz FROM t");
        VerifiedStmt("SELECT a, b, c INTO p, q, r FROM t");
        VerifiedStmt("SELECT a, b INTO :h1, :h2 FROM t");

        // Do not allow aliases here
        sql = "SELECT * INTO table0 asdf FROM table1";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: asdf"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_select_wildcard()
    {
        var sql = "SELECT * FROM foo";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.Wildcard(WildcardAdditionalOptions.Default()),
            TestUtils.Only(select.Projection));

        sql = "SELECT foo.* FROM foo";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.QualifiedWildcard(
                new SelectItemQualifiedWildcardKind.ObjectName(ObjectName.From(Sequence.Of(new Ident("foo")))),
                WildcardAdditionalOptions.Default()),
            TestUtils.Only(select.Projection));

        sql = "SELECT myschema.mytable.* FROM myschema.mytable";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.QualifiedWildcard(
                new SelectItemQualifiedWildcardKind.ObjectName(ObjectName.From(Sequence.Of(
                    new Ident("myschema"),
                    new Ident("mytable")))),
                WildcardAdditionalOptions.Default()),
            TestUtils.Only(select.Projection));

        sql = "SELECT * + * FROM foo;";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: +"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_count_wildcard()
    {
        VerifiedOnlySelect("SELECT COUNT(*) FROM Order WHERE id = 10");

        VerifiedOnlySelect(
            "SELECT COUNT(Employee.*) FROM Order JOIN Employee ON Order.employee = Employee.id");
    }

    [Fact]
    public void parse_column_aliases()
    {
        var sql = "SELECT a.col + 1 AS newname FROM foo AS a";
        var select = VerifiedOnlySelect(sql);
        if (TestUtils.Only(select.Projection) is SelectItem.ExprWithAlias
            {
                Expr: Expr.BinaryOp { Op: var op, Right: var right },
                Alias: var alias,
            })
        {
            Assert.Equal(new BinaryOperator.Plus(), op);
            Assert.Equal(Expr.MakeValue(TestUtils.Number("1")), right);
            Assert.Equal(new Ident("newname"), alias);
        }
        else
        {
            Assert.Fail("Expected: ExprWithAlias");
        }

        // alias without AS is parsed correctly:
        OneStatementParsesTo("SELECT a.col + 1 newname FROM foo AS a", sql);
    }

    [Fact]
    public void parse_select_expr_star()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsSelectExprStar);

        // Identifier wildcard expansion.
        var select = dialects.VerifiedOnlySelect("SELECT foo.bar.* FROM T");
        if (TestUtils.Only(select.Projection) is not SelectItem.QualifiedWildcard
            {
                Kind: SelectItemQualifiedWildcardKind.ObjectName { Value: var objectName },
            })
        {
            throw new InvalidOperationException($"expected wildcard select item: got {select.Projection[0]}");
        }
        Assert.Equal(
            ObjectName.From(
                new[] { "foo", "bar" }
                    .Select(s => new Ident(s))
                    .ToSequence()),
            objectName);

        // Arbitrary compound expression with wildcard expansion.
        select = dialects.VerifiedOnlySelect("SELECT foo - bar.* FROM T");
        if (TestUtils.Only(select.Projection) is not SelectItem.QualifiedWildcard
            {
                Kind: SelectItemQualifiedWildcardKind.Expr { Value: Expr.BinaryOp { Left: var left, Op: var op, Right: var right } },
            })
        {
            throw new InvalidOperationException($"expected wildcard select item: got {select.Projection[0]}");
        }
        if ((left, op, right) is not (Expr.Identifier { Value: var leftIdent }, BinaryOperator.Minus, Expr.Identifier { Value: var rightIdent }))
        {
            throw new InvalidOperationException($"expected binary op expr: got {select.Projection[0]}");
        }
        Assert.Equal(new Ident("foo"), leftIdent);
        Assert.Equal(new Ident("bar"), rightIdent);

        // Arbitrary expression wildcard expansion.
        select = dialects.VerifiedOnlySelect("SELECT myfunc().foo.* FROM T");
        if (TestUtils.Only(select.Projection) is not SelectItem.QualifiedWildcard
            {
                Kind: SelectItemQualifiedWildcardKind.Expr { Value: Expr.CompoundFieldAccess { Root: var root, AccessChain: var accessChain } },
            })
        {
            throw new InvalidOperationException($"expected wildcard expr: got {select.Projection[0]}");
        }
        Assert.True(root is Expr.Function);
        Assert.Equal(1, accessChain.Count);
        Assert.True(accessChain[0] is AccessExpr.Dot { Value: Expr.Identifier });

        dialects.OneStatementParsesTo(
            "SELECT 2. * 3 FROM T",
            // #[cfg(not(feature = "bigdecimal"))]
            "SELECT 2. * 3 FROM T");
        dialects.VerifiedOnlySelect("SELECT myfunc().* FROM T");

        // Invalid
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: ."),
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT foo.*.* FROM T")).Error);

        dialects = TestUtils.AllDialectsWhere(d =>
            d.SupportsSelectExprStar && d.SupportsSelectWildcardExcept);
        dialects.VerifiedOnlySelect("SELECT myfunc().* EXCEPT (foo) FROM T");
    }

    [Fact]
    public void parse_select_wildcard_with_alias()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsSelectWildcardWithAlias);

        // qualified wildcard with alias
        dialects
            .ParseSqlStatements("SELECT t.* AS all_cols FROM t");
        dialects.OneStatementParsesTo(
            "SELECT t.* all_cols FROM t",
            "SELECT t.* AS all_cols FROM t");
        dialects.OneStatementParsesTo(
            "SELECT t.* all_cols, other_col FROM t",
            "SELECT t.* AS all_cols, other_col FROM t");

        // unqualified wildcard with alias
        dialects
            .ParseSqlStatements("SELECT * AS all_cols FROM t");
        dialects.OneStatementParsesTo("SELECT * all_cols FROM t", "SELECT * AS all_cols FROM t");

        // mixed: regular column + qualified wildcard with alias
        dialects
            .ParseSqlStatements("SELECT a.id, b.* AS b_cols FROM a JOIN b ON (a.id = b.a_id)");
        dialects.OneStatementParsesTo(
            "SELECT a.id, b.* b_cols FROM a JOIN b ON (a.id = b.a_id)",
            "SELECT a.id, b.* AS b_cols FROM a JOIN b ON (a.id = b.a_id)");
    }

    [Fact]
    public void test_eof_after_as()
    {
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: an identifier after AS, found: EOF"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT foo AS")).Error);

        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: an identifier after AS, found: EOF"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT 1 FROM foo AS")).Error);
    }

    [Fact]
    public void test_no_infix_error()
    {
        var dialects = new TestedDialects(new List<Dialect> { new ClickHouseDialect() });

        Assert.Equal(
            new ParserError.ParserErrorValue("No infix parser for token ShiftLeft"),
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("ASSERT-URA<<")).Error);
    }

    [Fact]
    public void parse_select_count_wildcard()
    {
        var sql = "SELECT COUNT(*) FROM customer";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("COUNT"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: null,
                    Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Wildcard())),
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                WithinGroup: Sequence<OrderByExpr>.Empty,
                Filter: null,
                NullTreatment: null,
                Over: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_select_count_distinct()
    {
        var sql = "SELECT COUNT(DISTINCT +x) FROM customer";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("COUNT"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: new DuplicateTreatment.Distinct(),
                    Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.UnaryOp(
                        Op: new UnaryOperator.Plus(),
                        Expr: new Expr.Identifier(new Ident("x")))))),
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                WithinGroup: Sequence<OrderByExpr>.Empty,
                Filter: null,
                NullTreatment: null,
                Over: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        VerifiedStmt("SELECT COUNT(ALL +x) FROM customer");
        VerifiedStmt("SELECT COUNT(+x) FROM customer");

        sql = "SELECT COUNT(ALL DISTINCT + x) FROM customer";
        Assert.Equal(
            new ParserError.ParserErrorValue("Cannot specify both ALL and DISTINCT"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_not()
    {
        var sql = "SELECT id FROM customer WHERE NOT salary = ''";
        _ = VerifiedOnlySelect(sql);
        //TODO: add assertions
    }

    [Fact]
    public void parse_invalid_infix_not()
    {
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: NOT"),
            Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT c FROM t WHERE c NOT (")).Error);
    }

    [Fact]
    public void parse_collate()
    {
        var sql = "SELECT name COLLATE \"de_DE\" FROM customer";
        Assert.True(
            TestUtils.Only(TestUtils.AllDialects().VerifiedOnlySelect(sql).Projection)
                is SelectItem.UnnamedExpr { Value: Expr.Collate });
    }

    [Fact]
    public void parse_collate_after_parens()
    {
        var sql = "SELECT (name) COLLATE \"de_DE\" FROM customer";
        Assert.True(
            TestUtils.Only(TestUtils.AllDialects().VerifiedOnlySelect(sql).Projection)
                is SelectItem.UnnamedExpr { Value: Expr.Collate });
    }

    [Fact]
    public void parse_select_string_predicate()
    {
        var sql = "SELECT id, fname, lname FROM customer " +
                  "WHERE salary <> 'Not Provided' AND salary <> ''";
        _ = VerifiedOnlySelect(sql);
        //TODO: add assertions
    }

    [Fact]
    public void parse_projection_nested_type()
    {
        var sql = "SELECT customer.address.state FROM foo";
        _ = VerifiedOnlySelect(sql);
        //TODO: add assertions
    }

    [Fact]
    public void parse_null_in_select()
    {
        var sql = "SELECT NULL";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Value(new Value.Null().WithEmptySpan()),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_exponent_in_select()
    {
        // all except Hive, as it allows numbers to start an identifier
        var dialects = new TestedDialects(new List<Dialect>
        {
            new AnsiDialect(),
            new BigQueryDialect(),
            new ClickHouseDialect(),
            new DuckDbDialect(),
            new GenericDialect(),
            // new HiveDialect(),
            new MsSqlDialect(),
            new MySqlDialect(),
            new PostgreSqlDialect(),
            new RedshiftSqlDialect(),
            new SnowflakeDialect(),
            new SQLiteDialect(),
        });
        var sql = "SELECT 10e-20, 1e3, 1e+3, 1e3a, 1e, 0.5e2";
        var statements = dialects.ParseSqlStatements(sql);

        var query = statements.Last switch
        {
            Statement.Query inner => inner.Value,
            _ => throw new InvalidOperationException("Expected: Query"),
        };
        var select = query.Body switch
        {
            SetExpr.Select inner => inner.Value,
            _ => throw new InvalidOperationException("Expected: SetExpr::Select"),
        };

        Assert.Equal(
            Sequence.Of<SelectItem>(
                new SelectItem.UnnamedExpr(new Expr.Value(TestUtils.Number("10e-20").WithEmptySpan())),
                new SelectItem.UnnamedExpr(Expr.MakeValue(TestUtils.Number("1e3"))),
                new SelectItem.UnnamedExpr(new Expr.Value(TestUtils.Number("1e+3").WithEmptySpan())),
                new SelectItem.ExprWithAlias(
                    Expr: Expr.MakeValue(TestUtils.Number("1e3")),
                    Alias: new Ident("a")),
                new SelectItem.ExprWithAlias(
                    Expr: Expr.MakeValue(TestUtils.Number("1")),
                    Alias: new Ident("e")),
                new SelectItem.UnnamedExpr(Expr.MakeValue(TestUtils.Number("0.5e2")))),
            select.Projection);
    }

    [Fact]
    public void parse_select_with_date_column_name()
    {
        var sql = "SELECT date";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Identifier(new Ident(
                Value: "date",
                QuoteStyle: null,
                Span: Span.Empty())),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_escaped_single_quote_string_predicate_with_escape()
    {
        var sql = "SELECT id, fname, lname FROM customer " +
                  "WHERE salary <> 'Jim''s salary'";

        var ast = VerifiedOnlySelect(sql);

        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("salary")),
                Op: new BinaryOperator.NotEq(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("Jim's salary").WithEmptySpan())),
            ast.Selection);
    }

    [Fact]
    public void parse_escaped_single_quote_string_predicate_with_no_escape()
    {
        var sql = "SELECT id, fname, lname FROM customer " +
                  "WHERE salary <> 'Jim''s salary'";

        var ast = TestedDialects.NewWithOptions(
                new List<Dialect> { new MySqlDialect() },
                new ParserOptions()
                    .WithTrailingCommas(true)
                    .WithUnescape(false))
            .VerifiedOnlySelect(sql);

        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("salary")),
                Op: new BinaryOperator.NotEq(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("Jim''s salary").WithEmptySpan())),
            ast.Selection);
    }

    [Fact]
    public void parse_number()
    {
        var expr = VerifiedExpr("1.0");

        // #[cfg(not(feature = "bigdecimal"))]
        Assert.Equal(
            expr,
            new Expr.Value(new Value.Number("1.0", false).WithEmptySpan()));
    }

    [Fact]
    public void parse_compound_expr_1()
    {
        var sql = "a + b * c";
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("a")),
                Op: new BinaryOperator.Plus(),
                Right: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("b")),
                    Op: new BinaryOperator.Multiply(),
                    Right: new Expr.Identifier(new Ident("c")))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_compound_expr_2()
    {
        var sql = "a * b + c";
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("a")),
                    Op: new BinaryOperator.Multiply(),
                    Right: new Expr.Identifier(new Ident("b"))),
                Op: new BinaryOperator.Plus(),
                Right: new Expr.Identifier(new Ident("c"))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_unary_math_with_plus()
    {
        var sql = "-a + -b";
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.UnaryOp(
                    Op: new UnaryOperator.Minus(),
                    Expr: new Expr.Identifier(new Ident("a"))),
                Op: new BinaryOperator.Plus(),
                Right: new Expr.UnaryOp(
                    Op: new UnaryOperator.Minus(),
                    Expr: new Expr.Identifier(new Ident("b")))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_unary_math_with_multiply()
    {
        var sql = "-a * -b";
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.UnaryOp(
                    Op: new UnaryOperator.Minus(),
                    Expr: new Expr.Identifier(new Ident("a"))),
                Op: new BinaryOperator.Multiply(),
                Right: new Expr.UnaryOp(
                    Op: new UnaryOperator.Minus(),
                    Expr: new Expr.Identifier(new Ident("b")))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_mod()
    {
        var sql = "a % b";
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("a")),
                Op: new BinaryOperator.Modulo(),
                Right: new Expr.Identifier(new Ident("b"))),
            VerifiedExpr(sql));
    }

    private static TestedDialects PgAndGeneric() =>
        new TestedDialects(new List<Dialect>
        {
            new PostgreSqlDialect(),
            new GenericDialect(),
        });

    private static TestedDialects MsAndGeneric() =>
        new TestedDialects(new List<Dialect> { new MsSqlDialect(), new GenericDialect() });

    [Fact]
    public void parse_json_ops_without_colon()
    {
        var binaryOps = new (string, BinaryOperator, TestedDialects)[]
        {
            (
                "->",
                new BinaryOperator.Arrow(),
                TestUtils.AllDialectsExcept(d => d.SupportsLambdaFunctions)
            ),
            ("->", new BinaryOperator.Arrow(), PgAndGeneric()),
            ("->>", new BinaryOperator.LongArrow(), TestUtils.AllDialects()),
            ("#>", new BinaryOperator.HashArrow(), PgAndGeneric()),
            ("#>>", new BinaryOperator.HashLongArrow(), PgAndGeneric()),
            ("@>", new BinaryOperator.AtArrow(), TestUtils.AllDialects()),
            ("<@", new BinaryOperator.ArrowAt(), TestUtils.AllDialects()),
            ("#-", new BinaryOperator.HashMinus(), PgAndGeneric()),
            ("@?", new BinaryOperator.AtQuestion(), TestUtils.AllDialects()),
            ("@@", new BinaryOperator.AtAt(), TestUtils.AllDialects()),
        };

        foreach (var (strOp, op, dialects) in binaryOps)
        {
            var select = dialects.VerifiedOnlySelect($"SELECT a {strOp} b");
            Assert.Equal(
                new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("a")),
                    Op: op,
                    Right: new Expr.Identifier(new Ident("b")))),
                select.Projection[0]);
        }
    }

    [Fact]
    public void parse_json_object()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new MsSqlDialect(),
            new PostgreSqlDialect(),
        });
        var select = dialects.VerifiedOnlySelect("SELECT JSON_OBJECT('name' : 'value', 'type' : 1)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: var list } }:
                Assert.Equal(
                    new FunctionArg[]
                    {
                        new FunctionArg.ExprNamed(
                            Name: new Expr.Value(new Value.SingleQuotedString("name").WithEmptySpan()),
                            Arg: new FunctionArgExpr.Expr(new Expr.Value(
                                new Value.SingleQuotedString("value").WithEmptySpan())),
                            Operator: new FunctionArgOperator.Colon()),
                        new FunctionArg.ExprNamed(
                            Name: new Expr.Value(new Value.SingleQuotedString("type").WithEmptySpan()),
                            Arg: new FunctionArgExpr.Expr(Expr.MakeValue(TestUtils.Number("1"))),
                            Operator: new FunctionArgOperator.Colon()),
                    },
                    list.Args.ToArray());
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
        select = dialects
            .VerifiedOnlySelect("SELECT JSON_OBJECT('name' : 'value', 'type' : NULL ABSENT ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: var list } }:
                Assert.Equal(
                    new FunctionArg[]
                    {
                        new FunctionArg.ExprNamed(
                            Name: new Expr.Value(
                                new Value.SingleQuotedString("name").WithEmptySpan()),
                            Arg: new FunctionArgExpr.Expr(new Expr.Value(
                                new Value.SingleQuotedString("value").WithEmptySpan())),
                            Operator: new FunctionArgOperator.Colon()),
                        new FunctionArg.ExprNamed(
                            Name: new Expr.Value(
                                new Value.SingleQuotedString("type").WithEmptySpan()),
                            Arg: new FunctionArgExpr.Expr(new Expr.Value(new Value.Null().WithEmptySpan())),
                            Operator: new FunctionArgOperator.Colon()),
                    },
                    list.Args.ToArray());
                Assert.Equal(
                    new FunctionArgumentClause[]
                    {
                        new FunctionArgumentClause.JsonNullClause(
                            new JsonNullClause.AbsentOnNull()),
                    },
                    list.Clauses.ToArray());
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
        select = dialects.VerifiedOnlySelect("SELECT JSON_OBJECT(NULL ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: var list } }:
                Assert.True(list.Args.IsEmpty);
                Assert.Equal(
                    new FunctionArgumentClause[]
                    {
                        new FunctionArgumentClause.JsonNullClause(
                            new JsonNullClause.NullOnNull()),
                    },
                    list.Clauses.ToArray());
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
        select = dialects.VerifiedOnlySelect("SELECT JSON_OBJECT(ABSENT ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: var list } }:
                Assert.True(list.Args.IsEmpty);
                Assert.Equal(
                    new FunctionArgumentClause[]
                    {
                        new FunctionArgumentClause.JsonNullClause(
                            new JsonNullClause.AbsentOnNull()),
                    },
                    list.Clauses.ToArray());
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
        select = dialects.VerifiedOnlySelect(
            "SELECT JSON_OBJECT('name' : 'value', 'type' : JSON_ARRAY(1, 2) ABSENT ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: var list } }:
                Assert.Equal(
                    new FunctionArg.ExprNamed(
                        Name: new Expr.Value(new Value.SingleQuotedString("name").WithEmptySpan()),
                        Arg: new FunctionArgExpr.Expr(new Expr.Value(
                            new Value.SingleQuotedString("value").WithEmptySpan())),
                        Operator: new FunctionArgOperator.Colon()),
                    list.Args[0]);
                Assert.True(
                    list.Args[1] is FunctionArg.ExprNamed
                    {
                        Name: Expr.Value { Inner.Value: Value.SingleQuotedString },
                        Arg: FunctionArgExpr.Expr { Value: Expr.Function },
                        Operator: FunctionArgOperator.Colon,
                    });
                Assert.Equal(
                    new FunctionArgumentClause[]
                    {
                        new FunctionArgumentClause.JsonNullClause(
                            new JsonNullClause.AbsentOnNull()),
                    },
                    list.Clauses.ToArray());
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
        select = dialects.VerifiedOnlySelect(
            "SELECT JSON_OBJECT('name' : 'value', 'type' : JSON_OBJECT('type_id' : 1, 'name' : 'a') NULL ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: var list } }:
                Assert.Equal(
                    new FunctionArg.ExprNamed(
                        Name: new Expr.Value(new Value.SingleQuotedString("name").WithEmptySpan()),
                        Arg: new FunctionArgExpr.Expr(new Expr.Value(
                            new Value.SingleQuotedString("value").WithEmptySpan())),
                        Operator: new FunctionArgOperator.Colon()),
                    list.Args[0]);
                Assert.True(
                    list.Args[1] is FunctionArg.ExprNamed
                    {
                        Name: Expr.Value { Inner.Value: Value.SingleQuotedString },
                        Arg: FunctionArgExpr.Expr { Value: Expr.Function },
                        Operator: FunctionArgOperator.Colon,
                    });
                Assert.Equal(
                    new FunctionArgumentClause[]
                    {
                        new FunctionArgumentClause.JsonNullClause(
                            new JsonNullClause.NullOnNull()),
                    },
                    list.Clauses.ToArray());
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_mod_no_spaces()
    {
        var canonical = "a1 % b1";
        var sqls = new[] { "a1 % b1", "a1% b1", "a1 %b1", "a1%b1" };
        foreach (var sql in sqls)
        {
            // println!("Parsing {sql}");
            Assert.Equal(
                new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("a1")),
                    Op: new BinaryOperator.Modulo(),
                    Right: new Expr.Identifier(new Ident("b1"))),
                PgAndGeneric().ExprParsesTo(sql, canonical));
        }
    }

    [Fact]
    public void parse_is_null()
    {
        var sql = "a IS NULL";
        Assert.Equal(
            new Expr.IsNull(new Expr.Identifier(new Ident("a"))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_is_not_null()
    {
        var sql = "a IS NOT NULL";
        Assert.Equal(
            new Expr.IsNotNull(new Expr.Identifier(new Ident("a"))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_is_distinct_from()
    {
        var sql = "a IS DISTINCT FROM b";
        Assert.Equal(
            new Expr.IsDistinctFrom(
                new Expr.Identifier(new Ident("a")),
                new Expr.Identifier(new Ident("b"))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_is_not_distinct_from()
    {
        var sql = "a IS NOT DISTINCT FROM b";
        Assert.Equal(
            new Expr.IsNotDistinctFrom(
                new Expr.Identifier(new Ident("a")),
                new Expr.Identifier(new Ident("b"))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_is_distinct_from_precedence()
    {
        // The right operand of `IS [NOT] DISTINCT FROM` binds tighter than `AND`/`OR`,
        // so the boolean operator must end up at the root of the tree.
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.IsDistinctFrom(
                    new Expr.Identifier(new Ident("a")),
                    Expr.MakeValue(TestUtils.Number("1"))),
                Op: new BinaryOperator.And(),
                Right: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("b")),
                    Op: new BinaryOperator.Eq(),
                    Right: Expr.MakeValue(TestUtils.Number("2")))),
            VerifiedExpr("a IS DISTINCT FROM 1 AND b = 2"));

        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.IsNotDistinctFrom(
                    new Expr.Identifier(new Ident("a")),
                    Expr.MakeValue(TestUtils.Number("1"))),
                Op: new BinaryOperator.Or(),
                Right: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("b")),
                    Op: new BinaryOperator.Eq(),
                    Right: Expr.MakeValue(TestUtils.Number("2")))),
            VerifiedExpr("a IS NOT DISTINCT FROM 1 OR b = 2"));

        // `AND` binds tighter than `OR` within the surrounding expression.
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.BinaryOp(
                    Left: new Expr.IsDistinctFrom(
                        new Expr.Identifier(new Ident("a")),
                        Expr.MakeValue(TestUtils.Number("1"))),
                    Op: new BinaryOperator.And(),
                    Right: new Expr.Identifier(new Ident("b"))),
                Op: new BinaryOperator.Or(),
                Right: new Expr.Identifier(new Ident("c"))),
            VerifiedExpr("a IS DISTINCT FROM 1 AND b OR c"));
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.IsDistinctFrom(
                    new Expr.Identifier(new Ident("a")),
                    Expr.MakeValue(TestUtils.Number("1"))),
                Op: new BinaryOperator.Or(),
                Right: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("b")),
                    Op: new BinaryOperator.And(),
                    Right: new Expr.Identifier(new Ident("c")))),
            VerifiedExpr("a IS DISTINCT FROM 1 OR b AND c"));

        // Explicit parentheses still push the boolean expression into the right operand.
        Assert.Equal(
            new Expr.IsDistinctFrom(
                new Expr.Identifier(new Ident("a")),
                new Expr.Nested(new Expr.BinaryOp(
                    Left: Expr.MakeValue(TestUtils.Number("1")),
                    Op: new BinaryOperator.And(),
                    Right: new Expr.Identifier(new Ident("b"))))),
            VerifiedExpr("a IS DISTINCT FROM (1 AND b)"));

        // sqlparser resolves the IS family left-associatively, consistent with how
        // `a IS NULL IS NULL` already parses. Deliberately more permissive than
        // PostgreSQL, which declares IS as %nonassoc and rejects the chain.
        Assert.Equal(
            new Expr.IsNull(new Expr.IsDistinctFrom(
                new Expr.Identifier(new Ident("a")),
                new Expr.Identifier(new Ident("b")))),
            VerifiedExpr("a IS DISTINCT FROM b IS NULL"));

        // Operators that bind tighter than `IS` are still part of the right operand.
        Assert.Equal(
            new Expr.IsDistinctFrom(
                new Expr.Identifier(new Ident("a")),
                new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("b")),
                    Op: new BinaryOperator.Plus(),
                    Right: Expr.MakeValue(TestUtils.Number("1")))),
            VerifiedExpr("a IS DISTINCT FROM b + 1"));

        Assert.Equal(
            new Expr.IsDistinctFrom(
                new Expr.Identifier(new Ident("a")),
                new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("b")),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.Identifier(new Ident("c")))),
            VerifiedExpr("a IS DISTINCT FROM b = c"));

        // `NOT` binds more loosely than `IS`, so it applies to the whole comparison.
        Assert.Equal(
            new Expr.UnaryOp(
                Op: new UnaryOperator.Not(),
                Expr: new Expr.IsDistinctFrom(
                    new Expr.Identifier(new Ident("a")),
                    new Expr.Identifier(new Ident("b")))),
            VerifiedExpr("NOT a IS DISTINCT FROM b"));

        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.IsNotDistinctFrom(
                    new Expr.Identifier(new Ident("a")),
                    new Expr.Identifier(new Ident("b"))),
                Op: new BinaryOperator.And(),
                Right: new Expr.IsNotDistinctFrom(
                    new Expr.Identifier(new Ident("c")),
                    new Expr.Identifier(new Ident("d")))),
            VerifiedExpr("a IS NOT DISTINCT FROM b AND c IS NOT DISTINCT FROM d"));
    }

    [Fact]
    public void parse_pg_other_operator_precedence()
    {
        Expr ArrowK(Expr left) => new Expr.BinaryOp(
            Left: left,
            Op: new BinaryOperator.Arrow(),
            Right: new Expr.Value(
                new Value.SingleQuotedString("k").WithEmptySpan()));
        Expr TA() => new Expr.CompoundIdentifier(Sequence.Of(new Ident("t"), new Ident("a")));

        // `->` binds tighter than comparison operators, so the arrow expression is
        // the left operand rather than the comparison being the arrow's key.
        Expr ExpectedEq(Expr left) => new Expr.BinaryOp(
            Left: ArrowK(left),
            Op: new BinaryOperator.Eq(),
            Right: new Expr.Identifier(new Ident("b")));

        // Dialects with lambda functions read a bare `a ->` as the start of a lambda.
        Assert.Equal(
            ExpectedEq(new Expr.Identifier(new Ident("a"))),
            TestUtils.AllDialectsWhere(d => !d.SupportsLambdaFunctions).VerifiedExpr("a -> 'k' = b"));
        Assert.Equal(
            ExpectedEq(TA()),
            TestUtils.AllDialects().VerifiedExpr("t.a -> 'k' = b"));

        // `LIKE` sits below `=` in the precedence table, so cover that boundary too.
        Assert.Equal(
            new Expr.Like(
                Negated: false,
                Any: false,
                Expr: ArrowK(TA()),
                Pattern: new Expr.Value(
                    new Value.SingleQuotedString("x").WithEmptySpan()),
                EscapeChar: null),
            TestUtils.AllDialects().VerifiedExpr("t.a -> 'k' LIKE 'x'"));
    }

    [Fact]
    public void parse_not_precedence()
    {
        // NOT has higher precedence than OR/AND, so the following must parse as (NOT true) OR true
        var sql = "NOT 1 OR 1";
        Assert.True(VerifiedExpr(sql) is Expr.BinaryOp { Op: BinaryOperator.Or });

        // But NOT has lower precedence than comparison operators, so the following parses as NOT (a IS NULL)
        sql = "NOT a IS NULL";
        Assert.True(VerifiedExpr(sql) is Expr.UnaryOp { Op: UnaryOperator.Not });

        // NOT has lower precedence than BETWEEN, so the following parses as NOT (1 NOT BETWEEN 1 AND 2)
        sql = "NOT 1 NOT BETWEEN 1 AND 2";
        Assert.Equal(
            VerifiedExpr(sql),
            new Expr.UnaryOp(
                Op: new UnaryOperator.Not(),
                Expr: new Expr.Between(
                    Expr: Expr.MakeValue(TestUtils.Number("1")),
                    Low: Expr.MakeValue(TestUtils.Number("1")),
                    High: Expr.MakeValue(TestUtils.Number("2")),
                    Negated: true)));

        // NOT has lower precedence than LIKE, so the following parses as NOT ('a' NOT LIKE 'b')
        sql = "NOT 'a' NOT LIKE 'b'";
        Assert.Equal(
            VerifiedExpr(sql),
            new Expr.UnaryOp(
                Op: new UnaryOperator.Not(),
                Expr: new Expr.Like(
                    Expr: new Expr.Value(
                        new Value.SingleQuotedString("a").WithEmptySpan()),
                    Negated: true,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("b").WithEmptySpan()),
                    EscapeChar: null,
                    Any: false)));

        // NOT has lower precedence than IN, so the following parses as NOT (a NOT IN 'a')
        sql = "NOT a NOT IN ('a')";
        Assert.Equal(
            VerifiedExpr(sql),
            new Expr.UnaryOp(
                Op: new UnaryOperator.Not(),
                Expr: new Expr.InList(
                    Expr: new Expr.Identifier(new Ident("a")),
                    List: Sequence.Of<Expr>(new Expr.Value(
                        new Value.SingleQuotedString("a").WithEmptySpan())),
                    Negated: true,
                    Global: false)));
    }

    [Fact]
    public void parse_null_like()
    {
        var sql = "SELECT " +
                  "column1 LIKE NULL AS col_null, " +
                  "NULL LIKE column1 AS null_col " +
                  "FROM customers";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.ExprWithAlias(
                Expr: new Expr.Like(
                    Expr: new Expr.Identifier(new Ident("column1")),
                    Any: false,
                    Negated: false,
                    Pattern: new Expr.Value(new Value.Null().WithEmptySpan()),
                    EscapeChar: null),
                Alias: new Ident(
                    Value: "col_null",
                    QuoteStyle: null,
                    Span: Span.Empty())),
            select.Projection[0]);
        Assert.Equal(
            new SelectItem.ExprWithAlias(
                Expr: new Expr.Like(
                    Expr: new Expr.Value(new Value.Null().WithEmptySpan()),
                    Any: false,
                    Negated: false,
                    Pattern: new Expr.Identifier(new Ident("column1")),
                    EscapeChar: null),
                Alias: new Ident(
                    Value: "null_col",
                    QuoteStyle: null,
                    Span: Span.Empty())),
            select.Projection[1]);
    }

    [Fact]
    public void parse_ilike()
    {
        static void Chk(bool negated)
        {
            var sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}ILIKE '%a'";
            var select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.ILike(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: null,
                    Any: false),
                select.Selection!);

            // Test with escape char
            sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}ILIKE '%a' ESCAPE '^'";
            select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.ILike(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: Expr.MakeValue(
                        new Value.SingleQuotedString("^").WithEmptySpan()),
                    Any: false),
                select.Selection!);

            // This statement tests that ILIKE and NOT ILIKE have the same precedence.
            // This was previously mishandled (#81).
            sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}ILIKE '%a' IS NULL";
            select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.IsNull(new Expr.ILike(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: null,
                    Any: false)),
                select.Selection!);
        }
        Chk(false);
        Chk(true);
    }

    [Fact]
    public void parse_like()
    {
        static void Chk(bool negated)
        {
            var sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}LIKE '%a'";
            var select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.Like(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: null,
                    Any: false),
                select.Selection!);

            // Test with escape char
            sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}LIKE '%a' ESCAPE '^'";
            select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.Like(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: Expr.MakeValue(
                        new Value.SingleQuotedString("^").WithEmptySpan()),
                    Any: false),
                select.Selection!);

            // This statement tests that LIKE and NOT LIKE have the same precedence.
            // This was previously mishandled (#81).
            sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}LIKE '%a' IS NULL";
            select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.IsNull(new Expr.Like(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: null,
                    Any: false)),
                select.Selection!);
        }
        Chk(false);
        Chk(true);
    }

    [Fact]
    public void parse_like_escape_expression()
    {
        VerifiedExpr("'a%' LIKE 'a#%' ESCAPE ('' || '#')");
    }

    [Fact]
    public void parse_similar_to()
    {
        static void Chk(bool negated)
        {
            var sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}SIMILAR TO '%a'";
            var select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.SimilarTo(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: null),
                select.Selection!);

            // Test with escape char
            sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}SIMILAR TO '%a' ESCAPE '^'";
            select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.SimilarTo(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: Expr.MakeValue(
                        new Value.SingleQuotedString("^").WithEmptySpan())),
                select.Selection!);

            sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}SIMILAR TO '%a' ESCAPE NULL";
            select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.SimilarTo(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: Expr.MakeValue(new Value.Null().WithEmptySpan())),
                select.Selection!);

            // This statement tests that SIMILAR TO and NOT SIMILAR TO have the same precedence.
            sql = $"SELECT * FROM customers WHERE name {(negated ? "NOT " : "")}SIMILAR TO '%a' ESCAPE '^' IS NULL";
            select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.IsNull(new Expr.SimilarTo(
                    Expr: new Expr.Identifier(new Ident("name")),
                    Negated: negated,
                    Pattern: new Expr.Value(
                        new Value.SingleQuotedString("%a").WithEmptySpan()),
                    EscapeChar: Expr.MakeValue(
                        new Value.SingleQuotedString("^").WithEmptySpan()))),
                select.Selection!);
        }
        Chk(false);
        Chk(true);
    }

    [Fact]
    public void parse_in_list()
    {
        static void Chk(bool negated)
        {
            var sql = $"SELECT * FROM customers WHERE segment {(negated ? "NOT " : "")}IN ('HIGH', 'MED')";
            var select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.InList(
                    Expr: new Expr.Identifier(new Ident("segment")),
                    List: Sequence.Of<Expr>(
                        new Expr.Value(new Value.SingleQuotedString("HIGH").WithEmptySpan()),
                        new Expr.Value(new Value.SingleQuotedString("MED").WithEmptySpan())),
                    Negated: negated,
                    Global: false),
                select.Selection!);
        }
        Chk(false);
        Chk(true);
    }

    [Fact]
    public void parse_in_subquery()
    {
        var sql = "SELECT * FROM customers WHERE segment IN (SELECT segm FROM bar)";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.InSubquery(
                Expr: new Expr.Identifier(new Ident("segment")),
                Subquery: VerifiedQuery("SELECT segm FROM bar"),
                Negated: false,
                Global: false),
            select.Selection!);
    }

    [Fact]
    public void parse_in_union()
    {
        var sql = "SELECT * FROM customers WHERE segment IN ((SELECT segm FROM bar) UNION (SELECT segm FROM bar2))";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.InSubquery(
                Expr: new Expr.Identifier(new Ident("segment")),
                Subquery: VerifiedQuery(
                    "(SELECT segm FROM bar) UNION (SELECT segm FROM bar2)"),
                Negated: false,
                Global: false),
            select.Selection!);
    }

    [Fact]
    public void parse_in_unnest()
    {
        static void Chk(bool negated)
        {
            var sql = $"SELECT * FROM customers WHERE segment {(negated ? "NOT " : "")}IN UNNEST(expr)";
            var select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.InUnnest(
                    Expr: new Expr.Identifier(new Ident("segment")),
                    ArrayExpr: VerifiedExpr("expr"),
                    Negated: negated),
                select.Selection!);
        }
        Chk(false);
        Chk(true);
    }

    [Fact]
    public void parse_in_error()
    {
        // <expr> IN <expr> is no valid, except in dialects that accept an
        // unparenthesized expression as the IN right-hand side (e.g. ClickHouse).
        var sql = "SELECT * FROM customers WHERE segment in segment";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: (, found: segment"),
            Assert.Throws<ParserException>(() =>
                TestUtils.AllDialectsExcept(d => d.SupportsInUnparenthesizedExpr).ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_in_unparenthesized_expr()
    {
        // Dialects supporting an unparenthesized IN right-hand side wrap a bare expression
        // into a single-element list (e.g. `x IN 'a'` -> `x IN ('a')`).
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsInUnparenthesizedExpr);
        dialects.ExprParsesTo("x IN 'a'", "x IN ('a')");

        // The branch must not fire when the next token is `(` (regressions).
        dialects.VerifiedExpr("x IN (1, 2, 3)");
        dialects.VerifiedStmt("SELECT * FROM t WHERE x IN (SELECT y FROM u)");
    }

    [Fact]
    public void parse_string_agg()
    {
        var sql = "SELECT a || b";

        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("a")),
                Op: new BinaryOperator.StringConcat(),
                Right: new Expr.Identifier(new Ident("b")))),
            select.Projection[0]);
    }

    /// <summary>selects all dialects but PostgreSQL</summary>
    private static TestedDialects AllDialectsButPg() =>
        new TestedDialects(
            TestUtils.AllDialects()
                .Dialects
                .Where(x => x is not PostgreSqlDialect)
                .ToList());

    [Fact]
    public void parse_bitwise_ops()
    {
        var bitwiseOps = new (string, BinaryOperator, TestedDialects)[]
        {
            ("^", new BinaryOperator.BitwiseXor(), AllDialectsButPg()),
            ("|", new BinaryOperator.BitwiseOr(), TestUtils.AllDialects()),
            ("&", new BinaryOperator.BitwiseAnd(), TestUtils.AllDialects()),
        };

        foreach (var (strOp, op, dialects) in bitwiseOps)
        {
            var select = dialects.VerifiedOnlySelect($"SELECT a {strOp} b");
            Assert.Equal(
                new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("a")),
                    Op: op,
                    Right: new Expr.Identifier(new Ident("b")))),
                select.Projection[0]);
        }
    }

    [Fact]
    public void parse_bitwise_shift_ops()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsBitwiseShiftOperators);
        var sql = "SELECT 1 << 2, 3 >> 4";
        var select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Value(TestUtils.Number("1").WithEmptySpan()),
                Op: new BinaryOperator.PGBitwiseShiftLeft(),
                Right: new Expr.Value(TestUtils.Number("2").WithEmptySpan()))),
            select.Projection[0]);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Value(TestUtils.Number("3").WithEmptySpan()),
                Op: new BinaryOperator.PGBitwiseShiftRight(),
                Right: new Expr.Value(TestUtils.Number("4").WithEmptySpan()))),
            select.Projection[1]);
    }

    [Fact]
    public void parse_binary_any()
    {
        var select = VerifiedOnlySelect("SELECT a = ANY(b)");
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.AnyOp(
                Left: new Expr.Identifier(new Ident("a")),
                CompareOp: new BinaryOperator.Eq(),
                Right: new Expr.Identifier(new Ident("b")),
                IsSome: false)),
            select.Projection[0]);
    }

    [Fact]
    public void parse_binary_all()
    {
        var select = VerifiedOnlySelect("SELECT a = ALL(b)");
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.AllOp(
                Left: new Expr.Identifier(new Ident("a")),
                CompareOp: new BinaryOperator.Eq(),
                Right: new Expr.Identifier(new Ident("b")))),
            select.Projection[0]);
    }

    [Fact]
    public void parse_between()
    {
        static void Chk(bool negated)
        {
            var sql = $"SELECT * FROM customers WHERE age {(negated ? "NOT " : "")}BETWEEN 25 AND 32";
            var select = VerifiedOnlySelect(sql);
            Assert.Equal(
                new Expr.Between(
                    Expr: new Expr.Identifier(new Ident("age")),
                    Low: Expr.MakeValue(TestUtils.Number("25")),
                    High: Expr.MakeValue(TestUtils.Number("32")),
                    Negated: negated),
                select.Selection!);
        }
        Chk(false);
        Chk(true);
    }

    [Fact]
    public void parse_between_with_expr()
    {
        var sql = "SELECT * FROM t WHERE 1 BETWEEN 1 + 2 AND 3 + 4 IS NULL";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.IsNull(new Expr.Between(
                Expr: Expr.MakeValue(TestUtils.Number("1")),
                Low: new Expr.BinaryOp(
                    Left: Expr.MakeValue(TestUtils.Number("1")),
                    Op: new BinaryOperator.Plus(),
                    Right: Expr.MakeValue(TestUtils.Number("2"))),
                High: new Expr.BinaryOp(
                    Left: Expr.MakeValue(TestUtils.Number("3")),
                    Op: new BinaryOperator.Plus(),
                    Right: Expr.MakeValue(TestUtils.Number("4"))),
                Negated: false)),
            select.Selection!);

        sql = "SELECT * FROM t WHERE 1 = 1 AND 1 + x BETWEEN 1 AND 2";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.BinaryOp(
                    Left: Expr.MakeValue(TestUtils.Number("1")),
                    Op: new BinaryOperator.Eq(),
                    Right: Expr.MakeValue(TestUtils.Number("1"))),
                Op: new BinaryOperator.And(),
                Right: new Expr.Between(
                    Expr: new Expr.BinaryOp(
                        Left: Expr.MakeValue(TestUtils.Number("1")),
                        Op: new BinaryOperator.Plus(),
                        Right: new Expr.Identifier(new Ident("x"))),
                    Low: Expr.MakeValue(TestUtils.Number("1")),
                    High: Expr.MakeValue(TestUtils.Number("2")),
                    Negated: false)),
            select.Selection!);
    }

    [Fact]
    public void parse_tuples()
    {
        var sql = "SELECT (1, 2), (1), ('foo', 3, baz)";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            Sequence.Of<SelectItem>(
                new SelectItem.UnnamedExpr(new Expr.Tuple(Sequence.Of(
                    Expr.MakeValue(TestUtils.Number("1")),
                    Expr.MakeValue(TestUtils.Number("2"))))),
                new SelectItem.UnnamedExpr(new Expr.Nested(new Expr.Value(
                    TestUtils.Number("1").WithEmptySpan()))),
                new SelectItem.UnnamedExpr(new Expr.Tuple(Sequence.Of<Expr>(
                    new Expr.Value(new Value.SingleQuotedString("foo").WithEmptySpan()),
                    Expr.MakeValue(TestUtils.Number("3")),
                    new Expr.Identifier(new Ident("baz")))))),
            select.Projection);
    }

    [Fact]
    public void parse_tuple_invalid()
    {
        var sql = "select (1";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: EOF"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);

        sql = "select (), 2";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: an expression, found: )"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_select_order_by()
    {
        static void Chk(string sql)
        {
            var select = VerifiedQuery(sql);
            Assert.Equal(
                new OrderByKind.Expressions(Sequence.Of(
                    new OrderByExpr(
                        Expr: new Expr.Identifier(new Ident("lname")),
                        Options: new OrderByOptions(
                            Sort: new OrderBySort.Asc(),
                            NullsFirst: null),
                        WithFill: null),
                    new OrderByExpr(
                        Expr: new Expr.Identifier(new Ident("fname")),
                        Options: new OrderByOptions(
                            Sort: new OrderBySort.Desc(),
                            NullsFirst: null),
                        WithFill: null),
                    new OrderByExpr(
                        Expr: new Expr.Identifier(new Ident("id")),
                        Options: new OrderByOptions(
                            Sort: null,
                            NullsFirst: null),
                        WithFill: null))),
                (select.OrderBy ?? throw new InvalidOperationException("ORDER BY expected")).Kind);
        }
        Chk("SELECT id, fname, lname FROM customer WHERE id < 5 ORDER BY lname ASC, fname DESC, id");
        // make sure ORDER is not treated as an alias
        Chk("SELECT id, fname, lname FROM customer ORDER BY lname ASC, fname DESC, id");
        Chk("SELECT 1 AS lname, 2 AS fname, 3 AS id, 4 ORDER BY lname ASC, fname DESC, id");
    }

    [Fact]
    public void parse_select_order_by_limit()
    {
        var sql = "SELECT id, fname, lname FROM customer WHERE id < 5 " +
                  "ORDER BY lname ASC, fname DESC LIMIT 2";
        var select = VerifiedQuery(sql);
        Assert.Equal(
            new OrderByKind.Expressions(Sequence.Of(
                new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("lname")),
                    Options: new OrderByOptions(
                        Sort: new OrderBySort.Asc(),
                        NullsFirst: null),
                    WithFill: null),
                new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("fname")),
                    Options: new OrderByOptions(
                        Sort: new OrderBySort.Desc(),
                        NullsFirst: null),
                    WithFill: null))),
            (select.OrderBy ?? throw new InvalidOperationException("ORDER BY expected")).Kind);
        var expectedLimitClause = new LimitClause.LimitOffset(
            Limit: Expr.MakeValue(TestUtils.Number("2")),
            Offset: null,
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal(expectedLimitClause, select.LimitClause);
    }

    [Fact]
    public void parse_select_order_by_all()
    {
        static void Chk(string sql, OrderByKind exceptOrderBy)
        {
            var dialects = TestUtils.AllDialectsWhere(d => d.SupportsOrderByAll);
            var select = dialects.VerifiedQuery(sql);
            Assert.Equal(
                exceptOrderBy,
                (select.OrderBy ?? throw new InvalidOperationException("ORDER BY expected")).Kind);
        }
        var testCases = new (string, OrderByKind)[]
        {
            (
                "SELECT id, fname, lname FROM customer WHERE id < 5 ORDER BY ALL",
                new OrderByKind.All(new OrderByOptions(
                    Sort: null,
                    NullsFirst: null))
            ),
            (
                "SELECT id, fname, lname FROM customer WHERE id < 5 ORDER BY ALL NULLS FIRST",
                new OrderByKind.All(new OrderByOptions(
                    Sort: null,
                    NullsFirst: true))
            ),
            (
                "SELECT id, fname, lname FROM customer WHERE id < 5 ORDER BY ALL NULLS LAST",
                new OrderByKind.All(new OrderByOptions(
                    Sort: null,
                    NullsFirst: false))
            ),
            (
                "SELECT id, fname, lname FROM customer ORDER BY ALL ASC",
                new OrderByKind.All(new OrderByOptions(
                    Sort: new OrderBySort.Asc(),
                    NullsFirst: null))
            ),
            (
                "SELECT id, fname, lname FROM customer ORDER BY ALL ASC NULLS FIRST",
                new OrderByKind.All(new OrderByOptions(
                    Sort: new OrderBySort.Asc(),
                    NullsFirst: true))
            ),
            (
                "SELECT id, fname, lname FROM customer ORDER BY ALL ASC NULLS LAST",
                new OrderByKind.All(new OrderByOptions(
                    Sort: new OrderBySort.Asc(),
                    NullsFirst: false))
            ),
            (
                "SELECT id, fname, lname FROM customer WHERE id < 5 ORDER BY ALL DESC",
                new OrderByKind.All(new OrderByOptions(
                    Sort: new OrderBySort.Desc(),
                    NullsFirst: null))
            ),
            (
                "SELECT id, fname, lname FROM customer WHERE id < 5 ORDER BY ALL DESC NULLS FIRST",
                new OrderByKind.All(new OrderByOptions(
                    Sort: new OrderBySort.Desc(),
                    NullsFirst: true))
            ),
            (
                "SELECT id, fname, lname FROM customer WHERE id < 5 ORDER BY ALL DESC NULLS LAST",
                new OrderByKind.All(new OrderByOptions(
                    Sort: new OrderBySort.Desc(),
                    NullsFirst: false))
            ),
        };

        foreach (var (sql, expectedOrderBy) in testCases)
        {
            Chk(sql, expectedOrderBy);
        }
    }

    [Fact]
    public void parse_select_order_by_not_support_all()
    {
        static void Chk(string sql, OrderByKind exceptOrderBy)
        {
            var dialects = TestUtils.AllDialectsWhere(d => !d.SupportsOrderByAll);
            var select = dialects.VerifiedQuery(sql);
            Assert.Equal(
                exceptOrderBy,
                (select.OrderBy ?? throw new InvalidOperationException("ORDER BY expected")).Kind);
        }
        var testCases = new (string, OrderByKind)[]
        {
            (
                "SELECT id, ALL FROM customer WHERE id < 5 ORDER BY ALL",
                new OrderByKind.Expressions(Sequence.Of(new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("ALL")),
                    Options: new OrderByOptions(
                        Sort: null,
                        NullsFirst: null),
                    WithFill: null)))
            ),
            (
                "SELECT id, ALL FROM customer ORDER BY ALL ASC NULLS FIRST",
                new OrderByKind.Expressions(Sequence.Of(new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("ALL")),
                    Options: new OrderByOptions(
                        Sort: new OrderBySort.Asc(),
                        NullsFirst: true),
                    WithFill: null)))
            ),
            (
                "SELECT id, ALL FROM customer ORDER BY ALL DESC NULLS LAST",
                new OrderByKind.Expressions(Sequence.Of(new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("ALL")),
                    Options: new OrderByOptions(
                        Sort: new OrderBySort.Desc(),
                        NullsFirst: false),
                    WithFill: null)))
            ),
        };

        foreach (var (sql, expectedOrderBy) in testCases)
        {
            Chk(sql, expectedOrderBy);
        }
    }

    [Fact]
    public void parse_select_order_by_nulls_order()
    {
        var sql = "SELECT id, fname, lname FROM customer WHERE id < 5 " +
                  "ORDER BY lname ASC NULLS FIRST, fname DESC NULLS LAST LIMIT 2";
        var select = VerifiedQuery(sql);
        Assert.Equal(
            new OrderByKind.Expressions(Sequence.Of(
                new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("lname")),
                    Options: new OrderByOptions(
                        Sort: new OrderBySort.Asc(),
                        NullsFirst: true),
                    WithFill: null),
                new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("fname")),
                    Options: new OrderByOptions(
                        Sort: new OrderBySort.Desc(),
                        NullsFirst: false),
                    WithFill: null))),
            (select.OrderBy ?? throw new InvalidOperationException("ORDER BY expeccted")).Kind);
        var expectedLimitClause = new LimitClause.LimitOffset(
            Limit: Expr.MakeValue(TestUtils.Number("2")),
            Offset: null,
            LimitBy: Sequence<Expr>.Empty);
        Assert.Equal(expectedLimitClause, select.LimitClause);
    }

    [Fact]
    public void parse_aggregate_order_by_using_operator()
    {
        var sql = "SELECT aggfns(DISTINCT a, a, c ORDER BY c USING ~<~, a) FROM t";
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsOrderByUsingOperator);
        var select = dialects.VerifiedOnlySelect(sql);
        if (select.Projection[0] is not SelectItem.UnnamedExpr
            {
                Value: Expr.Function { Value.Args: FunctionArguments.List { Value.Clauses: var clauses } },
            })
        {
            throw new InvalidOperationException("expected aggregate function in projection");
        }

        if (clauses
                .FirstOrDefault(clause => clause is FunctionArgumentClause.OrderBy)
            is not FunctionArgumentClause.OrderBy { Value: var orderByExprs })
        {
            throw new InvalidOperationException("expected ORDER BY clause in aggregate function argument list");
        }

        Assert.Equal(
            orderByExprs[0].Options.Sort,
            new OrderBySort.Using(ObjectName.From(Sequence.Of(new Ident("~<~")))));
        Assert.Null(orderByExprs[1].Options.Sort);
    }

    [Fact]
    public void parse_order_by_using_operator_syntax()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsOrderByUsingOperator);
        dialects.OneStatementParsesTo(
            "SELECT a FROM t ORDER BY a USING OPERATOR(<)",
            "SELECT a FROM t ORDER BY a USING <");

        var query = dialects
            .VerifiedQuery("SELECT a FROM t ORDER BY a USING OPERATOR(pg_catalog.<) NULLS LAST");
        var orderBy = query.OrderBy ?? throw new InvalidOperationException("expected ORDER BY clause");
        if (orderBy.Kind is not OrderByKind.Expressions { Value: var exprs })
        {
            throw new InvalidOperationException("expected ORDER BY expressions");
        }

        Assert.Equal(
            exprs[0].Options.Sort,
            new OrderBySort.Using(ObjectName.From(Sequence.Of(
                new Ident("pg_catalog"),
                new Ident("<")))));
        // assert_eq!(x, Some(false))
        Assert.False(exprs[0].Options.NullsFirst);
    }

    [Fact]
    public void parse_order_by_using_operator_invalid_cases()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsOrderByUsingOperator);
        var err = Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements("SELECT a FROM t ORDER BY a USING OPERATOR();")).Error;
        Assert.True(err is ParserError.ParserErrorValue);
    }

    [Fact]
    public void parse_select_group_by()
    {
        var sql = "SELECT id, fname, lname FROM customer GROUP BY lname, fname";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new GroupByExpr.Expressions(
                Sequence.Of<Expr>(
                    new Expr.Identifier(new Ident("lname")),
                    new Expr.Identifier(new Ident("fname"))),
                Sequence<GroupByWithModifier>.Empty),
            select.GroupBy);

        // Tuples can also be in the set
        OneStatementParsesTo(
            "SELECT id, fname, lname FROM customer GROUP BY (lname, fname)",
            "SELECT id, fname, lname FROM customer GROUP BY (lname, fname)");
    }

    [Fact]
    public void parse_select_group_by_all()
    {
        var sql = "SELECT id, fname, lname, SUM(order) FROM customer GROUP BY ALL";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(new GroupByExpr.All(Sequence<GroupByWithModifier>.Empty), select.GroupBy);

        OneStatementParsesTo(
            "SELECT id, fname, lname, SUM(order) FROM customer GROUP BY ALL",
            "SELECT id, fname, lname, SUM(order) FROM customer GROUP BY ALL");
    }

    [Fact]
    public void parse_group_by_with_modifier()
    {
        var clauses = new[] { "x", "a, b", "ALL" };
        var modifiers = new[]
        {
            "WITH ROLLUP",
            "WITH CUBE",
            "WITH TOTALS",
            "WITH ROLLUP WITH CUBE",
        };
        var expectedModifiers = new[]
        {
            Sequence.Of<GroupByWithModifier>(new GroupByWithModifier.Rollup()),
            Sequence.Of<GroupByWithModifier>(new GroupByWithModifier.Cube()),
            Sequence.Of<GroupByWithModifier>(new GroupByWithModifier.Totals()),
            Sequence.Of<GroupByWithModifier>(new GroupByWithModifier.Rollup(), new GroupByWithModifier.Cube()),
        };
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsGroupByWithModifier);

        foreach (var clause in clauses)
        {
            foreach (var (modifier, expectedModifier) in modifiers.Zip(expectedModifiers, (m, e) => (m, e)))
            {
                var sql = $"SELECT * FROM t GROUP BY {clause} {modifier}";
                switch (dialects.VerifiedStmt(sql))
                {
                    case Statement.Query { Value: var query }:
                        var groupBy = query.Body.AsSelect()!.GroupBy;
                        if (clause == "ALL")
                        {
                            Assert.Equal(groupBy, new GroupByExpr.All(expectedModifier));
                        }
                        else
                        {
                            Assert.Equal(
                                groupBy,
                                new GroupByExpr.Expressions(
                                    clause
                                        .Split(new[] { ", " }, StringSplitOptions.None)
                                        .Select(c => (Expr)new Expr.Identifier(new Ident(c)))
                                        .ToSequence(),
                                    expectedModifier));
                        }
                        break;
                    default:
                        throw new InvalidOperationException("unreachable!()");
                }
            }
        }

        // invalid cases
        var invalidCases = new[]
        {
            "SELECT * FROM t GROUP BY x WITH",
            "SELECT * FROM t GROUP BY x WITH ROLLUP CUBE",
            "SELECT * FROM t GROUP BY x WITH WITH ROLLUP",
            "SELECT * FROM t GROUP BY WITH ROLLUP",
        };
        foreach (var sql in invalidCases)
        {
            // .expect_err("Expected: one of ROLLUP or CUBE or TOTALS, found: WITH")
            Assert.Throws<ParserException>(() => dialects
                .ParseSqlStatements(sql));
        }
    }

    [Fact]
    public void parse_group_by_special_grouping_sets()
    {
        var sql = "SELECT a, b, SUM(c) FROM tab1 GROUP BY a, b GROUPING SETS ((a, b), (a), (b), ())";
        switch (TestUtils.AllDialects().VerifiedStmt(sql))
        {
            case Statement.Query { Value: var query }:
                var groupBy = query.Body.AsSelect()!.GroupBy;
                Assert.Equal(
                    groupBy,
                    new GroupByExpr.Expressions(
                        Sequence.Of<Expr>(
                            new Expr.Identifier(new Ident("a")),
                            new Expr.Identifier(new Ident("b"))),
                        Sequence.Of<GroupByWithModifier>(new GroupByWithModifier.GroupingSets(new Expr.GroupingSets(Sequence.Of(
                            Sequence.Of<Expr>(
                                new Expr.Identifier(new Ident("a")),
                                new Expr.Identifier(new Ident("b"))),
                            Sequence.Of<Expr>(new Expr.Identifier(new Ident("a"))),
                            Sequence.Of<Expr>(new Expr.Identifier(new Ident("b"))),
                            Sequence<Expr>.Empty))))));
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_group_by_grouping_sets_single_values()
    {
        var sql = "SELECT a, b, SUM(c) FROM tab1 GROUP BY a, b GROUPING SETS ((a, b), a, (b), c, ())";
        var canonical =
            "SELECT a, b, SUM(c) FROM tab1 GROUP BY a, b GROUPING SETS ((a, b), (a), (b), (c), ())";
        switch (TestUtils.AllDialects().OneStatementParsesTo(sql, canonical))
        {
            case Statement.Query { Value: var query }:
                var groupBy = query.Body.AsSelect()!.GroupBy;
                Assert.Equal(
                    groupBy,
                    new GroupByExpr.Expressions(
                        Sequence.Of<Expr>(
                            new Expr.Identifier(new Ident("a")),
                            new Expr.Identifier(new Ident("b"))),
                        Sequence.Of<GroupByWithModifier>(new GroupByWithModifier.GroupingSets(new Expr.GroupingSets(Sequence.Of(
                            Sequence.Of<Expr>(
                                new Expr.Identifier(new Ident("a")),
                                new Expr.Identifier(new Ident("b"))),
                            Sequence.Of<Expr>(new Expr.Identifier(new Ident("a"))),
                            Sequence.Of<Expr>(new Expr.Identifier(new Ident("b"))),
                            Sequence.Of<Expr>(new Expr.Identifier(new Ident("c"))),
                            Sequence<Expr>.Empty))))));
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }
    }

    [Fact]
    public void parse_select_having()
    {
        var sql = "SELECT foo FROM bar GROUP BY foo HAVING COUNT(*) > 1";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(new Ident("COUNT"))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.List(new FunctionArgumentList(
                        DuplicateTreatment: null,
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Wildcard())),
                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                    WithinGroup: Sequence<OrderByExpr>.Empty,
                    Filter: null,
                    NullTreatment: null,
                    Over: null)),
                Op: new BinaryOperator.Gt(),
                Right: Expr.MakeValue(TestUtils.Number("1"))),
            select.Having);

        sql = "SELECT 'foo' HAVING 1 = 1";
        select = VerifiedOnlySelect(sql);
        Assert.True(select.Having is not null);
    }

    [Fact]
    public void parse_select_qualify()
    {
        var sql = "SELECT i, p, o FROM qt QUALIFY ROW_NUMBER() OVER (PARTITION BY p ORDER BY o) = 1";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(new Ident("ROW_NUMBER"))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.List(new FunctionArgumentList(
                        DuplicateTreatment: null,
                        Args: Sequence<FunctionArg>.Empty,
                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                    WithinGroup: Sequence<OrderByExpr>.Empty,
                    Filter: null,
                    NullTreatment: null,
                    Over: new WindowType.WindowSpec(new WindowSpec(
                        WindowName: null,
                        PartitionBy: Sequence.Of<Expr>(new Expr.Identifier(new Ident("p"))),
                        OrderBy: Sequence.Of(new OrderByExpr(
                            Expr: new Expr.Identifier(new Ident("o")),
                            Options: new OrderByOptions(
                                Sort: null,
                                NullsFirst: null),
                            WithFill: null)),
                        WindowFrame: null)))),
                Op: new BinaryOperator.Eq(),
                Right: Expr.MakeValue(TestUtils.Number("1"))),
            select.Qualify);

        sql = "SELECT i, p, o, ROW_NUMBER() OVER (PARTITION BY p ORDER BY o) AS row_num FROM qt QUALIFY row_num = 1";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("row_num")),
                Op: new BinaryOperator.Eq(),
                Right: Expr.MakeValue(TestUtils.Number("1"))),
            select.Qualify);
    }

    [Fact]
    public void parse_limit_accepts_all()
    {
        OneStatementParsesTo(
            "SELECT id, fname, lname FROM customer WHERE id = 1 LIMIT ALL",
            "SELECT id, fname, lname FROM customer WHERE id = 1");
        OneStatementParsesTo(
            "SELECT id, fname, lname FROM customer WHERE id = 1 LIMIT ALL OFFSET 1",
            "SELECT id, fname, lname FROM customer WHERE id = 1 OFFSET 1");
        OneStatementParsesTo(
            "SELECT id, fname, lname FROM customer WHERE id = 1 OFFSET 1 LIMIT ALL",
            "SELECT id, fname, lname FROM customer WHERE id = 1 OFFSET 1");
    }

    [Fact]
    public void parse_cast()
    {
        var sql = "SELECT CAST(id AS BIGINT) FROM customer";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.BigInt(null),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT CAST(id AS TINYINT) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.TinyInt(null),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        OneStatementParsesTo(
            "SELECT CAST(id AS MEDIUMINT) FROM customer",
            "SELECT CAST(id AS MEDIUMINT) FROM customer");

        OneStatementParsesTo(
            "SELECT CAST(id AS BIGINT) FROM customer",
            "SELECT CAST(id AS BIGINT) FROM customer");

        VerifiedStmt("SELECT CAST(id AS NUMERIC) FROM customer");

        VerifiedStmt("SELECT CAST(id AS DEC) FROM customer");

        VerifiedStmt("SELECT CAST(id AS DECIMAL) FROM customer");

        sql = "SELECT CAST(id AS NVARCHAR(50)) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.Nvarchar(new CharacterLength.IntegerLength(
                    Length: 50,
                    Unit: null)),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT CAST(id AS CLOB) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.Clob(null),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT CAST(id AS CLOB(50)) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.Clob(50),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT CAST(id AS BINARY(50)) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.Binary(50),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT CAST(id AS VARBINARY(50)) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.Varbinary(new BinaryLength.IntegerLength(Length: 50)),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT CAST(id AS BLOB) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.Blob(null),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT CAST(id AS BLOB(50)) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.Blob(50),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT CAST(details AS JSONB) FROM customer";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.Identifier(new Ident("details")),
                DataType: new DataType.JSONB(),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_try_cast()
    {
        var sql = "SELECT TRY_CAST(id AS BIGINT) FROM customer";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Cast(
                Kind: CastKind.TryCast,
                Expr: new Expr.Identifier(new Ident("id")),
                DataType: new DataType.BigInt(null),
                Format: null),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        VerifiedStmt("SELECT TRY_CAST(id AS BIGINT) FROM customer");

        VerifiedStmt("SELECT TRY_CAST(id AS NUMERIC) FROM customer");

        VerifiedStmt("SELECT TRY_CAST(id AS DEC) FROM customer");

        VerifiedStmt("SELECT TRY_CAST(id AS DECIMAL) FROM customer");
    }

    [Fact]
    public void parse_extract()
    {
        var sql = "SELECT EXTRACT(YEAR FROM d)";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Extract(
                Field: new DateTimeField.Year(),
                Syntax: ExtractSyntax.From,
                Expr: new Expr.Identifier(new Ident("d"))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        OneStatementParsesTo("SELECT EXTRACT(year from d)", "SELECT EXTRACT(YEAR FROM d)");

        VerifiedStmt("SELECT EXTRACT(MONTH FROM d)");
        VerifiedStmt("SELECT EXTRACT(WEEK FROM d)");
        VerifiedStmt("SELECT EXTRACT(DAY FROM d)");
        VerifiedStmt("SELECT EXTRACT(DAYOFWEEK FROM d)");
        VerifiedStmt("SELECT EXTRACT(DAYOFYEAR FROM d)");
        VerifiedStmt("SELECT EXTRACT(DATE FROM d)");
        VerifiedStmt("SELECT EXTRACT(DATETIME FROM d)");
        VerifiedStmt("SELECT EXTRACT(HOUR FROM d)");
        VerifiedStmt("SELECT EXTRACT(MINUTE FROM d)");
        VerifiedStmt("SELECT EXTRACT(SECOND FROM d)");
        VerifiedStmt("SELECT EXTRACT(MILLISECOND FROM d)");
        VerifiedStmt("SELECT EXTRACT(MICROSECOND FROM d)");
        VerifiedStmt("SELECT EXTRACT(NANOSECOND FROM d)");
        VerifiedStmt("SELECT EXTRACT(CENTURY FROM d)");
        VerifiedStmt("SELECT EXTRACT(DECADE FROM d)");
        VerifiedStmt("SELECT EXTRACT(DOW FROM d)");
        VerifiedStmt("SELECT EXTRACT(DOY FROM d)");
        VerifiedStmt("SELECT EXTRACT(EPOCH FROM d)");
        VerifiedStmt("SELECT EXTRACT(ISODOW FROM d)");
        VerifiedStmt("SELECT EXTRACT(ISOWEEK FROM d)");
        VerifiedStmt("SELECT EXTRACT(ISOYEAR FROM d)");
        VerifiedStmt("SELECT EXTRACT(JULIAN FROM d)");
        VerifiedStmt("SELECT EXTRACT(MICROSECOND FROM d)");
        VerifiedStmt("SELECT EXTRACT(MICROSECONDS FROM d)");
        VerifiedStmt("SELECT EXTRACT(MILLENIUM FROM d)");
        VerifiedStmt("SELECT EXTRACT(MILLENNIUM FROM d)");
        VerifiedStmt("SELECT EXTRACT(MILLISECOND FROM d)");
        VerifiedStmt("SELECT EXTRACT(MILLISECONDS FROM d)");
        VerifiedStmt("SELECT EXTRACT(QUARTER FROM d)");
        VerifiedStmt("SELECT EXTRACT(TIMEZONE FROM d)");
        VerifiedStmt("SELECT EXTRACT(TIMEZONE_ABBR FROM d)");
        VerifiedStmt("SELECT EXTRACT(TIMEZONE_HOUR FROM d)");
        VerifiedStmt("SELECT EXTRACT(TIMEZONE_MINUTE FROM d)");
        VerifiedStmt("SELECT EXTRACT(TIMEZONE_REGION FROM d)");
        VerifiedStmt("SELECT EXTRACT(TIME FROM d)");

        var dialects = TestUtils.AllDialectsExcept(d => d.AllowExtractCustom);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: date/time field, found: JIFFY"),
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT EXTRACT(JIFFY FROM d)")).Error);
    }

    [Fact]
    public void parse_ceil_number()
    {
        VerifiedStmt("SELECT CEIL(1.5)");
        VerifiedStmt("SELECT CEIL(float_column) FROM my_table");
    }

    [Fact]
    public void parse_floor_number()
    {
        VerifiedStmt("SELECT FLOOR(1.5)");
        VerifiedStmt("SELECT FLOOR(float_column) FROM my_table");
    }

    [Fact]
    public void parse_ceil_number_scale()
    {
        VerifiedStmt("SELECT CEIL(1.5, 1)");
        VerifiedStmt("SELECT CEIL(float_column, 3) FROM my_table");
    }

    [Fact]
    public void parse_floor_number_scale()
    {
        VerifiedStmt("SELECT FLOOR(1.5, 1)");
        VerifiedStmt("SELECT FLOOR(float_column, 3) FROM my_table");
    }

    [Fact]
    public void parse_ceil_scale()
    {
        var sql = "SELECT CEIL(d, 2)";
        var select = VerifiedOnlySelect(sql);

        // #[cfg(not(feature = "bigdecimal"))]
        Assert.Equal(
            new Expr.Ceil(
                Expr: new Expr.Identifier(new Ident("d")),
                Field: new CeilFloorKind.Scale(new Value.Number(2.ToString(System.Globalization.CultureInfo.InvariantCulture), false).WithEmptySpan())),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_floor_scale()
    {
        var sql = "SELECT FLOOR(d, 2)";
        var select = VerifiedOnlySelect(sql);

        // #[cfg(not(feature = "bigdecimal"))]
        Assert.Equal(
            new Expr.Floor(
                Expr: new Expr.Identifier(new Ident("d")),
                Field: new CeilFloorKind.Scale(new Value.Number(2.ToString(System.Globalization.CultureInfo.InvariantCulture), false).WithEmptySpan())),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_ceil_datetime()
    {
        var sql = "SELECT CEIL(d TO DAY)";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Ceil(
                Expr: new Expr.Identifier(new Ident("d")),
                Field: new CeilFloorKind.DateTimeField(new DateTimeField.Day())),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        OneStatementParsesTo("SELECT CEIL(d to day)", "SELECT CEIL(d TO DAY)");

        VerifiedStmt("SELECT CEIL(d TO HOUR) FROM df");
        VerifiedStmt("SELECT CEIL(d TO MINUTE) FROM df");
        VerifiedStmt("SELECT CEIL(d TO SECOND) FROM df");
        VerifiedStmt("SELECT CEIL(d TO MILLISECOND) FROM df");

        var dialects = TestUtils.AllDialectsExcept(d => d.AllowExtractCustom);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: date/time field, found: JIFFY"),
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT CEIL(d TO JIFFY) FROM df")).Error);
    }

    [Fact]
    public void parse_floor_datetime()
    {
        var sql = "SELECT FLOOR(d TO DAY)";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Floor(
                Expr: new Expr.Identifier(new Ident("d")),
                Field: new CeilFloorKind.DateTimeField(new DateTimeField.Day())),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        OneStatementParsesTo("SELECT FLOOR(d to day)", "SELECT FLOOR(d TO DAY)");

        VerifiedStmt("SELECT FLOOR(d TO HOUR) FROM df");
        VerifiedStmt("SELECT FLOOR(d TO MINUTE) FROM df");
        VerifiedStmt("SELECT FLOOR(d TO SECOND) FROM df");
        VerifiedStmt("SELECT FLOOR(d TO MILLISECOND) FROM df");

        var dialects = TestUtils.AllDialectsExcept(d => d.AllowExtractCustom);
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: date/time field, found: JIFFY"),
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT FLOOR(d TO JIFFY) FROM df")).Error);
    }

    [Fact]
    public void parse_listagg()
    {
        var select = VerifiedOnlySelect(
            "SELECT LISTAGG(DISTINCT dateid, ', ' ON OVERFLOW TRUNCATE '%' WITHOUT COUNT) " +
            "WITHIN GROUP (ORDER BY id, username)");

        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("LISTAGG"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: new DuplicateTreatment.Distinct(),
                    Args: Sequence.Of<FunctionArg>(
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Identifier(new Ident(
                            "dateid")))),
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            new Value.SingleQuotedString(", ").WithEmptySpan())))),
                    Clauses: Sequence.Of<FunctionArgumentClause>(new FunctionArgumentClause.OnOverflow(
                        new ListAggOnOverflow.Truncate(
                            Filler: new Expr.Value(
                                new Value.SingleQuotedString("%").WithEmptySpan()),
                            WithCount: false))))),
                WithinGroup: Sequence.Of(
                    new OrderByExpr(
                        Expr: new Expr.Identifier(new Ident(
                            Value: "id",
                            QuoteStyle: null,
                            Span: Span.Empty())),
                        Options: new OrderByOptions(
                            Sort: null,
                            NullsFirst: null),
                        WithFill: null),
                    new OrderByExpr(
                        Expr: new Expr.Identifier(new Ident(
                            Value: "username",
                            QuoteStyle: null,
                            Span: Span.Empty())),
                        Options: new OrderByOptions(
                            Sort: null,
                            NullsFirst: null),
                        WithFill: null)),
                Filter: null,
                NullTreatment: null,
                Over: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        VerifiedStmt("SELECT LISTAGG(sellerid) WITHIN GROUP (ORDER BY dateid)");
        VerifiedStmt("SELECT LISTAGG(dateid)");
        VerifiedStmt("SELECT LISTAGG(DISTINCT dateid)");
        VerifiedStmt("SELECT LISTAGG(dateid ON OVERFLOW ERROR)");
        TestUtils.AllDialectsWhere(d => d.SupportsNationalStringLiteral)
            .VerifiedStmt("SELECT LISTAGG(dateid ON OVERFLOW TRUNCATE N'...' WITH COUNT)");
        VerifiedStmt("SELECT LISTAGG(dateid ON OVERFLOW TRUNCATE X'deadbeef' WITH COUNT)");
    }

    [Fact]
    public void parse_array_agg_func()
    {
        var supportedDialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new DuckDbDialect(),
            new PostgreSqlDialect(),
            new MsSqlDialect(),
            new AnsiDialect(),
            new HiveDialect(),
        });

        foreach (var sql in new[]
                 {
                     "SELECT ARRAY_AGG(x ORDER BY x) AS a FROM T",
                     "SELECT ARRAY_AGG(x ORDER BY x LIMIT 2) FROM tbl",
                     "SELECT ARRAY_AGG(DISTINCT x ORDER BY x LIMIT 2) FROM tbl",
                     "SELECT ARRAY_AGG(x ORDER BY x, y) AS a FROM T",
                     "SELECT ARRAY_AGG(x ORDER BY x ASC, y DESC) AS a FROM T",
                 })
        {
            supportedDialects.VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_agg_with_order_by()
    {
        var supportedDialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new PostgreSqlDialect(),
            new MsSqlDialect(),
            new AnsiDialect(),
            new HiveDialect(),
        });

        foreach (var sql in new[]
                 {
                     "SELECT FIRST_VALUE(x ORDER BY x) AS a FROM T",
                     "SELECT FIRST_VALUE(x ORDER BY x) FROM tbl",
                     "SELECT LAST_VALUE(x ORDER BY x, y) AS a FROM T",
                     "SELECT LAST_VALUE(x ORDER BY x ASC, y DESC) AS a FROM T",
                 })
        {
            supportedDialects.VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_window_rank_function()
    {
        var supportedDialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new PostgreSqlDialect(),
            new MsSqlDialect(),
            new AnsiDialect(),
            new HiveDialect(),
            new SnowflakeDialect(),
        });

        foreach (var sql in new[]
                 {
                     "SELECT column1, column2, FIRST_VALUE(column2) OVER (PARTITION BY column1 ORDER BY column2 NULLS LAST) AS column2_first FROM t1",
                     "SELECT column1, column2, FIRST_VALUE(column2) OVER (ORDER BY column2 NULLS LAST) AS column2_first FROM t1",
                     "SELECT col_1, col_2, LAG(col_2) OVER (ORDER BY col_1) FROM t1",
                     "SELECT LAG(col_2, 1, 0) OVER (ORDER BY col_1) FROM t1",
                     "SELECT LAG(col_2, 1, 0) OVER (PARTITION BY col_3 ORDER BY col_1)",
                 })
        {
            supportedDialects.VerifiedStmt(sql);
        }

        var supportedDialectsNulls = new TestedDialects(new List<Dialect>
        {
            new MsSqlDialect(),
            new SnowflakeDialect(),
        });

        foreach (var sql in new[]
                 {
                     "SELECT column1, column2, FIRST_VALUE(column2) IGNORE NULLS OVER (PARTITION BY column1 ORDER BY column2 NULLS LAST) AS column2_first FROM t1",
                     "SELECT column1, column2, FIRST_VALUE(column2) RESPECT NULLS OVER (PARTITION BY column1 ORDER BY column2 NULLS LAST) AS column2_first FROM t1",
                     "SELECT LAG(col_2, 1, 0) IGNORE NULLS OVER (ORDER BY col_1) FROM t1",
                     "SELECT LAG(col_2, 1, 0) RESPECT NULLS OVER (ORDER BY col_1) FROM t1",
                 })
        {
            supportedDialectsNulls.VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_window_function_null_treatment_arg()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsWindowFunctionNullTreatmentArg);
        var sql = "SELECT " +
                  "FIRST_VALUE(a IGNORE NULLS) OVER (), " +
                  "FIRST_VALUE(b RESPECT NULLS) OVER () " +
                  "FROM mytable";
        var projection = dialects.VerifiedOnlySelect(sql).Projection;
        var cases = new (string, NullTreatment)[]
        {
            ("a", new NullTreatment.IgnoreNulls()),
            ("b", new NullTreatment.RespectNulls()),
        };
        for (var i = 0; i < cases.Length; i++)
        {
            var (expectedExpr, expectedNullTreatment) = cases[i];
            if (projection[i] is not SelectItem.UnnamedExpr { Value: Expr.Function { Value: var actual } })
            {
                throw new InvalidOperationException("unreachable!()");
            }
            Assert.Equal(
                ObjectName.From(Sequence.Of(new Ident("FIRST_VALUE"))),
                actual.Name);
            if (actual.Args is not FunctionArguments.List { Value: var argList })
            {
                throw new InvalidOperationException("expected argument list");
            }
            Assert.True(
                argList
                    .Clauses
                    .All(clause => clause is not FunctionArgumentClause.OrderBy));
            Assert.Equal(1, argList.Args.Count);
            if (argList.Args[0] is not FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.Identifier { Value: var actualExpr } } })
            {
                throw new InvalidOperationException("unreachable!()");
            }
            Assert.Equal(new Ident(expectedExpr), actualExpr);
            Assert.Equal(
                expectedNullTreatment,
                argList.Clauses
                    .Select(clause => clause switch
                    {
                        FunctionArgumentClause.IgnoreOrRespectNulls { Value: var nt } => nt,
                        _ => null,
                    })
                    .FirstOrDefault(nt => nt is not null));
        }

        sql = "SELECT LAG(1 IGNORE NULLS) IGNORE NULLS OVER () FROM t1";
        Assert.Equal(
            Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql)).Error,
            new ParserError.ParserErrorValue("Expected: end of statement, found: NULLS"));

        sql = "SELECT LAG(1 IGNORE NULLS) IGNORE NULLS OVER () FROM t1";
        Assert.Equal(
            Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => !d.SupportsWindowFunctionNullTreatmentArg)
                .ParseSqlStatements(sql)).Error,
            new ParserError.ParserErrorValue("Expected: ), found: IGNORE"));
    }

    [Fact]
    public void test_compound_expr()
    {
        var supportedDialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new DuckDbDialect(),
            new BigQueryDialect(),
        });
        var sqls = new[]
        {
            "SELECT abc[1].f1 FROM t",
            "SELECT abc[1].f1.f2 FROM t",
            "SELECT f1.abc[1] FROM t",
            "SELECT f1.f2.abc[1] FROM t",
            "SELECT f1.abc[1].f2 FROM t",
            "SELECT named_struct('a', 1, 'b', 2).a",
            "SELECT named_struct('a', 1, 'b', 2).a",
            "SELECT make_array(1, 2, 3)[1]",
            "SELECT make_array(named_struct('a', 1))[1].a",
            "SELECT abc[1][-1].a.b FROM t",
            "SELECT abc[1][-1].a.b[1] FROM t",
        };
        foreach (var sql in sqls)
        {
            supportedDialects.VerifiedStmt(sql);
        }
    }

    [Fact]
    public void test_double_value()
    {
        var dialects = TestUtils.AllDialects();
        var testCases = new List<(List<string>, List<SelectItem>)>
        {
            GenNumberCaseWithSign("0."),
            GenNumberCaseWithSign("0.0"),
            GenNumberCaseWithSign("0000."),
            GenNumberCaseWithSign("0000.00"),
            GenNumberCaseWithSign(".0"),
            GenNumberCaseWithSign(".00"),
            GenNumberCaseWithSign("0e0"),
            GenNumberCaseWithSign("0e+0"),
            GenNumberCaseWithSign("0e-0"),
            GenNumberCaseWithSign("0.e-0"),
            GenNumberCaseWithSign("0.e+0"),
            GenNumberCaseWithSign(".0e-0"),
            GenNumberCaseWithSign(".0e+0"),
            GenNumberCaseWithSign("00.0e+0"),
            GenNumberCaseWithSign("00.0e-0"),
        };

        foreach (var (input, expected) in testCases)
        {
            for (var i = 0; i < input.Count; i++)
            {
                var expr = input[i];
                if (dialects.OneStatementParsesTo($"SELECT {expr}", "") is Statement.Query { Value: var query })
                {
                    if (query.Body is SetExpr.Select { Value: var select })
                    {
                        Assert.Equal(expected[i], select.Projection[0]);
                    }
                    else
                    {
                        Assert.Fail("Expected a SELECT statement");
                    }
                }
                else
                {
                    Assert.Fail("Expected a SELECT statement");
                }
            }
        }
    }

    private static (List<string>, List<SelectItem>) GenNumberCase(string value)
    {
        var input = new List<string>
        {
            value,
            $"{value} col_alias",
            $"{value} AS col_alias",
        };
        var expected = new List<SelectItem>
        {
            new SelectItem.UnnamedExpr(Expr.MakeValue(TestUtils.Number(value))),
            new SelectItem.ExprWithAlias(
                Expr: Expr.MakeValue(TestUtils.Number(value)),
                Alias: new Ident("col_alias")),
            new SelectItem.ExprWithAlias(
                Expr: Expr.MakeValue(TestUtils.Number(value)),
                Alias: new Ident("col_alias")),
        };
        return (input, expected);
    }

    private static (List<string>, List<SelectItem>) GenSignNumberCase(string value, UnaryOperator op)
    {
        switch (op)
        {
            case UnaryOperator.Plus or UnaryOperator.Minus:
                break;
            default:
                throw new InvalidOperationException("Invalid sign");
        }

        var input = new List<string>
        {
            $"{op}{value}",
            $"{op}{value} col_alias",
            $"{op}{value} AS col_alias",
        };
        var expected = new List<SelectItem>
        {
            new SelectItem.UnnamedExpr(new Expr.UnaryOp(
                Op: op,
                Expr: Expr.MakeValue(TestUtils.Number(value)))),
            new SelectItem.ExprWithAlias(
                Expr: new Expr.UnaryOp(
                    Op: op,
                    Expr: Expr.MakeValue(TestUtils.Number(value))),
                Alias: new Ident("col_alias")),
            new SelectItem.ExprWithAlias(
                Expr: new Expr.UnaryOp(
                    Op: op,
                    Expr: Expr.MakeValue(TestUtils.Number(value))),
                Alias: new Ident("col_alias")),
        };
        return (input, expected);
    }

    /// <summary>
    /// generate the test cases for signed and unsigned numbers
    /// For example, given "0.0", the test cases will be:
    /// - "0.0"
    /// - "+0.0"
    /// - "-0.0"
    /// </summary>
    private static (List<string>, List<SelectItem>) GenNumberCaseWithSign(string number)
    {
        var (input, expected) = GenNumberCase(number);
        foreach (var op in new UnaryOperator[] { new UnaryOperator.Plus(), new UnaryOperator.Minus() })
        {
            var (inputSign, expectedSign) = GenSignNumberCase(number, op);
            input.AddRange(inputSign);
            expected.AddRange(expectedSign);
        }
        return (input, expected);
    }

    [Fact]
    public void parse_negative_value()
    {
        var sql1 = "SELECT -1";
        OneStatementParsesTo(sql1, "SELECT -1");

        var sql2 = "CREATE SEQUENCE name INCREMENT -10 MINVALUE -1000 MAXVALUE 15 START -100;";
        OneStatementParsesTo(
            sql2,
            "CREATE SEQUENCE name INCREMENT -10 MINVALUE -1000 MAXVALUE 15 START -100");
    }

    [Fact]
    public void parse_create_table()
    {
        // String Rust com `\` no fim da linha (remove a quebra e a indentação seguinte); as linhas sem
        // `\` mantêm a quebra e os 15 espaços de indentação.
        var sql = "CREATE TABLE uk_cities (" +
                  "name VARCHAR(100) NOT NULL," +
                  "lat DOUBLE NULL," +
                  "lng DOUBLE,\n" +
                  "               constrained INT NULL CONSTRAINT pkey PRIMARY KEY NOT NULL UNIQUE CHECK (constrained > 0),\n" +
                  "               ref INT REFERENCES othertable (a, b)," +
                  "ref2 INT references othertable2 on delete cascade on update no action," +
                  "constraint fkey foreign key (lat) references othertable3 (lat) on delete restrict," +
                  "constraint fkey2 foreign key (lat) references othertable4(lat) on delete no action on update restrict, " +
                  "foreign key (lat) references othertable4(lat) on update set default on delete cascade, " +
                  "FOREIGN KEY (lng) REFERENCES othertable4 (longitude) ON UPDATE SET NULL\n" +
                  "               )";
        var ast = OneStatementParsesTo(
            sql,
            "CREATE TABLE uk_cities (" +
            "name VARCHAR(100) NOT NULL, " +
            "lat DOUBLE NULL, " +
            "lng DOUBLE, " +
            "constrained INT NULL CONSTRAINT pkey PRIMARY KEY NOT NULL UNIQUE CHECK (constrained > 0), " +
            "ref INT REFERENCES othertable (a, b), " +
            "ref2 INT REFERENCES othertable2 ON DELETE CASCADE ON UPDATE NO ACTION, " +
            "CONSTRAINT fkey FOREIGN KEY (lat) REFERENCES othertable3(lat) ON DELETE RESTRICT, " +
            "CONSTRAINT fkey2 FOREIGN KEY (lat) REFERENCES othertable4(lat) ON DELETE NO ACTION ON UPDATE RESTRICT, " +
            "FOREIGN KEY (lat) REFERENCES othertable4(lat) ON DELETE CASCADE ON UPDATE SET DEFAULT, " +
            "FOREIGN KEY (lng) REFERENCES othertable4(longitude) ON UPDATE SET NULL)");
        switch (ast)
        {
            case Statement.CreateTable
            {
                Value:
                {
                    IfNotExists: false,
                    External: false,
                    FileFormat: null,
                    Location: null,
                } createTable
            }:
                Assert.Equal("uk_cities", createTable.Name.ToString());
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("name"),
                            DataType: new DataType.Varchar(new CharacterLength.IntegerLength(
                                Length: 100,
                                Unit: null)),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.NotNull()))),
                        new ColumnDef(
                            Name: new Ident("lat"),
                            DataType: new DataType.Double(new ExactNumberInfo.None()),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Null()))),
                        new ColumnDef(
                            Name: new Ident("lng"),
                            DataType: new DataType.Double(new ExactNumberInfo.None()),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident("constrained"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Null()),
                                new ColumnOptionDef(
                                    Name: new Ident("pkey"),
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
                                    Option: new ColumnOption.NotNull()),
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Unique(new UniqueConstraint(
                                        Name: null,
                                        IndexName: null,
                                        IndexTypeDisplay: new KeyOrIndexDisplay.None(),
                                        IndexType: null,
                                        Columns: Sequence<IndexColumn>.Empty,
                                        Include: Sequence<Ident>.Empty,
                                        IndexOptions: Sequence<IndexOption>.Empty,
                                        Characteristics: null,
                                        NullsDistinct: new NullsDistinctOption.None()))),
                                new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Check(new CheckConstraint(
                                        Name: null,
                                        Expr: VerifiedExpr("constrained > 0"),
                                        NoInherit: false,
                                        Enforced: null))))),
                        new ColumnDef(
                            Name: new Ident("ref"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.ForeignKey(new ForeignKeyConstraint(
                                    Name: null,
                                    IndexName: null,
                                    Columns: Sequence<Ident>.Empty,
                                    ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable"))),
                                    ReferredColumns: Sequence.Of(new Ident("a"), new Ident("b")),
                                    OnDelete: null,
                                    OnUpdate: null,
                                    MatchKind: null,
                                    Characteristics: null))))),
                        new ColumnDef(
                            Name: new Ident("ref2"),
                            DataType: new DataType.Int(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.ForeignKey(new ForeignKeyConstraint(
                                    Name: null,
                                    IndexName: null,
                                    Columns: Sequence<Ident>.Empty,
                                    ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable2"))),
                                    ReferredColumns: Sequence<Ident>.Empty,
                                    OnDelete: new ReferentialAction.Cascade(),
                                    OnUpdate: new ReferentialAction.NoAction(),
                                    MatchKind: null,
                                    Characteristics: null)))))));
                Assert.Equal(
                    createTable.Constraints,
                    Sequence.Of(
                        TableConstraint.From(new ForeignKeyConstraint(
                            Name: new Ident("fkey"),
                            IndexName: null,
                            Columns: Sequence.Of(new Ident("lat")),
                            ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable3"))),
                            ReferredColumns: Sequence.Of(new Ident("lat")),
                            OnDelete: new ReferentialAction.Restrict(),
                            OnUpdate: null,
                            MatchKind: null,
                            Characteristics: null)),
                        TableConstraint.From(new ForeignKeyConstraint(
                            Name: new Ident("fkey2"),
                            IndexName: null,
                            Columns: Sequence.Of(new Ident("lat")),
                            ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable4"))),
                            ReferredColumns: Sequence.Of(new Ident("lat")),
                            OnDelete: new ReferentialAction.NoAction(),
                            OnUpdate: new ReferentialAction.Restrict(),
                            MatchKind: null,
                            Characteristics: null)),
                        TableConstraint.From(new ForeignKeyConstraint(
                            Name: null,
                            IndexName: null,
                            Columns: Sequence.Of(new Ident("lat")),
                            ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable4"))),
                            ReferredColumns: Sequence.Of(new Ident("lat")),
                            OnDelete: new ReferentialAction.Cascade(),
                            OnUpdate: new ReferentialAction.SetDefault(),
                            MatchKind: null,
                            Characteristics: null)),
                        TableConstraint.From(new ForeignKeyConstraint(
                            Name: null,
                            IndexName: null,
                            Columns: Sequence.Of(new Ident("lng")),
                            ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable4"))),
                            ReferredColumns: Sequence.Of(new Ident("longitude")),
                            OnDelete: null,
                            OnUpdate: new ReferentialAction.SetNull(),
                            MatchKind: null,
                            Characteristics: null))));
                Assert.Equal(createTable.TableOptions, new CreateTableOptions.None());
                break;
            default:
                throw new InvalidOperationException("unreachable!()");
        }

        var err = Assert.Throws<ParserException>(() => ParseSqlStatements("CREATE TABLE t (a int NOT NULL GARBAGE)")).Error;
        Assert.Contains(
            "Expected: ',' or ')' after column definition, found: GARBAGE",
            err.ToString());

        err = Assert.Throws<ParserException>(() => ParseSqlStatements("CREATE TABLE t (a int NOT NULL CONSTRAINT foo)")).Error;
        Assert.Contains(
            "Expected: constraint details after CONSTRAINT <name>",
            err.ToString());
    }

    // ---------------------------------------------------------------------------------------------
    // Funções de arquivo de sqlparser_common.rs definidas fora das linhas 1–4319 (linhas 10994–11020).
    // ---------------------------------------------------------------------------------------------

    private static Sequence<Statement> ParseSqlStatements(string sql) =>
        TestUtils.AllDialects().ParseSqlStatements(sql);

    private static Statement OneStatementParsesTo(string sql, string canonical) =>
        TestUtils.AllDialects().OneStatementParsesTo(sql, canonical);

    private static Statement VerifiedStmt(string query) =>
        TestUtils.AllDialects().VerifiedStmt(query);

    private static Query VerifiedQuery(string query) =>
        TestUtils.AllDialects().VerifiedQuery(query);

    private static Select VerifiedOnlySelect(string query) =>
        TestUtils.AllDialects().VerifiedOnlySelect(query);

    private static Expr VerifiedExpr(string query) =>
        TestUtils.AllDialects().VerifiedExpr(query);
}
