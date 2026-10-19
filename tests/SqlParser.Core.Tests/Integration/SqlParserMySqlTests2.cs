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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_mysql.rs (linhas 2651–5054).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C10.md):
// - Esta classe é a segunda fatia de tests/sqlparser_mysql.rs; as funções-atalho do arquivo
//   (`mysql()`, `mysql_and_generic()`) são copiadas como métodos `private static`.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests/SpansTests).
// - `assert_eq!(x.len(), n)` usa `Assert.Equal(n, x.Count)`; o xUnit2013 (que sugere Assert.Single)
//   fica suprimido neste arquivo, como em SpansTests.
// - `match stmt { Variante(x) => .., _ => unreachable!() }` → `Assert.IsType<Statement.Variante>(stmt)`;
//   padrões com guarda ou campos fixos → `switch`/`is` com o mesmo padrão; `unreachable!()` →
//   `throw new InvalidOperationException("internal error: entered unreachable code")`;
//   `panic!(msg)` → `Assert.Fail(msg)`.
// - `assert_matches!(x, P)` → `Assert.True(x is P)` (ou `Assert.IsType` quando o padrão é só o tipo).
// - Strings Rust com `\` no fim da linha (continuação que remove a quebra e a indentação seguinte)
//   viram concatenações dos pedaços, sem o espaço removido.
#pragma warning disable xUnit2013
public class SqlParserMySqlTests2
{
    private const string Unreachable = "internal error: entered unreachable code";

    private static TestedDialects Mysql() =>
        new TestedDialects(new List<Dialect> { new MySqlDialect() });

    private static TestedDialects MysqlAndGeneric() =>
        new TestedDialects(new List<Dialect> { new MySqlDialect(), new GenericDialect() });

    // Don't run with bigdecimal as it fails like this on rust beta:
    //
    // 'parse_select_with_concatenation_of_exp_number_and_numeric_prefix_column'
    // panicked at 'assertion failed: `(left == right)`
    //
    //  left: `"SELECT 123e4, 123col_$@123abc FROM \"table\""`,
    //  right: `"SELECT 1230000, 123col_$@123abc FROM \"table\""`', src/test_utils.rs:114:13
    // #[cfg(not(feature = "bigdecimal"))]: o Core não tem a feature `bigdecimal` (números são `string`),
    // então o teste roda sempre.
    [Fact]
    public void parse_select_with_concatenation_of_exp_number_and_numeric_prefix_column()
    {
        var sql = "SELECT 123e4, 123col_$@123abc FROM \"table\"";
        var q = Assert.IsType<Statement.Query>(Mysql().VerifiedStmt(sql)).Value;
        Assert.Equal(
            q.Body,
            new SetExpr.Select(new Select(
                SelectToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Distinct: null,
                SelectModifiers: null,
                Top: null,
                TopBeforeDistinct: false,
                Projection: Sequence.Of<SelectItem>(
                    new SelectItem.UnnamedExpr(Expr.MakeValue(TestUtils.Number("123e4"))),
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("123col_$@123abc")))),
                Exclude: null,
                Into: null,
                From: Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(Ident.WithQuote('"', "table")))),
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
                Flavor: SelectFlavor.Standard)));
    }

    [Fact]
    public void parse_insert_with_numeric_prefix_column_name()
    {
        var sql = "INSERT INTO s1.t1 (123col_$@length123) VALUES (67.654)";
        var insert = Assert.IsType<Statement.Insert>(Mysql().VerifiedStmt(sql)).Value;
        var tableName = insert.Table;
        var columns = insert.Columns;
        Assert.Equal(
            new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("s1"), new Ident("t1")))),
            tableName);
        Assert.Equal(
            Sequence.Of(ObjectName.From(new Ident("123col_$@length123"))),
            columns);
    }

    [Fact]
    public void parse_update_with_joins()
    {
        var sql = "UPDATE orders AS o JOIN customers AS c ON o.customer_id = c.id SET o.completed = true WHERE c.firstname = 'Peter'";
        switch (Mysql().VerifiedStmt(sql))
        {
            // Statement::Update(Update { table, assignments, from: _from, selection, returning, or: None,
            //     order_by: _, limit: None, optimizer_hints, update_token: _, output: _ }) if optimizer_hints.is_empty()
            case Statement.Update { Value: { Or: null, Limit: null } update } when update.OptimizerHints.IsEmpty:
            {
                Assert.Equal(
                    new TableWithJoins(
                        Relation: new TableFactor.Table(
                            Name: ObjectName.From(Sequence.Of(new Ident("orders"))),
                            Alias: TestUtils.TableAlias(true, "o"),
                            Args: null,
                            WithHints: Sequence<Expr>.Empty,
                            Version: null,
                            Partitions: Sequence<Ident>.Empty,
                            WithOrdinality: false,
                            JsonPath: null,
                            Sample: null,
                            IndexHints: Sequence<TableIndexHints>.Empty),
                        Joins: Sequence.Of(new Join(
                            Relation: new TableFactor.Table(
                                Name: ObjectName.From(Sequence.Of(new Ident("customers"))),
                                Alias: TestUtils.TableAlias(true, "c"),
                                Args: null,
                                WithHints: Sequence<Expr>.Empty,
                                Version: null,
                                Partitions: Sequence<Ident>.Empty,
                                WithOrdinality: false,
                                JsonPath: null,
                                Sample: null,
                                IndexHints: Sequence<TableIndexHints>.Empty),
                            Global: false,
                            JoinOperator: new JoinOperator.Join(new JoinConstraint.On(new Expr.BinaryOp(
                                Left: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("o"),
                                    new Ident("customer_id"))),
                                Op: new BinaryOperator.Eq(),
                                Right: new Expr.CompoundIdentifier(Sequence.Of(
                                    new Ident("c"),
                                    new Ident("id"))))))))),
                    update.Table);
                Assert.Equal(
                    Sequence.Of(new Assignment(
                        Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(
                            new Ident("o"),
                            new Ident("completed")))),
                        Value: new Expr.Value(new Value.Boolean(true).WithEmptySpan()))),
                    update.Assignments);
                Assert.Equal(
                    new Expr.BinaryOp(
                        Left: new Expr.CompoundIdentifier(Sequence.Of(
                            new Ident("c"),
                            new Ident("firstname"))),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.SingleQuotedString("Peter").WithEmptySpan())),
                    update.Selection);
                Assert.Null(update.Returning);
                break;
            }
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_update_with_order_by()
    {
        var sql = "UPDATE foo SET bar = false WHERE foo = true ORDER BY foo ASC";
        var update = Assert.IsType<Statement.Update>(MysqlAndGeneric().VerifiedStmt(sql)).Value;
        Assert.Equal(
            Sequence.Of(new OrderByExpr(
                Expr: new Expr.Identifier(new Ident("foo", null, Span.Empty())),
                Options: new OrderByOptions(
                    Sort: new OrderBySort.Asc(),
                    NullsFirst: null),
                WithFill: null)),
            update.OrderBy);
    }

    [Fact]
    public void parse_update_with_order_by_and_limit()
    {
        var sql = "UPDATE foo SET bar = false WHERE foo = true ORDER BY foo ASC LIMIT 10";
        var update = Assert.IsType<Statement.Update>(MysqlAndGeneric().VerifiedStmt(sql)).Value;
        Assert.Equal(
            Sequence.Of(new OrderByExpr(
                Expr: new Expr.Identifier(new Ident("foo", null, Span.Empty())),
                Options: new OrderByOptions(
                    Sort: new OrderBySort.Asc(),
                    NullsFirst: null),
                WithFill: null)),
            update.OrderBy);
        Assert.Equal(Expr.MakeValue(TestUtils.Number("10")), update.Limit);
    }

    [Fact]
    public void parse_delete_with_order_by()
    {
        var sql = "DELETE FROM customers ORDER BY id DESC";
        var delete = Assert.IsType<Statement.Delete>(Mysql().VerifiedStmt(sql)).Value;
        Assert.Equal(
            Sequence.Of(new OrderByExpr(
                Expr: new Expr.Identifier(new Ident("id", null, Span.Empty())),
                Options: new OrderByOptions(
                    Sort: new OrderBySort.Desc(),
                    NullsFirst: null),
                WithFill: null)),
            delete.OrderBy);
    }

    [Fact]
    public void parse_delete_with_limit()
    {
        var sql = "DELETE FROM customers LIMIT 100";
        var delete = Assert.IsType<Statement.Delete>(Mysql().VerifiedStmt(sql)).Value;
        Assert.Equal(Expr.MakeValue(TestUtils.Number("100")), delete.Limit);
    }

    [Fact]
    public void parse_alter_table_add_column()
    {
        {
            // Statement::AlterTable(AlterTable { name, if_exists, only, operations, table_type,
            //     location: _, on_cluster: _, end_token: _ })
            var alterTable = Assert.IsType<Statement.AlterTable>(Mysql().VerifiedStmt("ALTER TABLE tab ADD COLUMN b INT FIRST")).Value;
            Assert.Equal("tab", alterTable.Name.ToString());
            Assert.False(alterTable.IfExists);
            Assert.Null(alterTable.TableType);
            Assert.False(alterTable.Only);
            Assert.Equal(
                alterTable.Operations,
                Sequence.Of<AlterTableOperation>(new AlterTableOperation.AddColumn(
                    ColumnKeyword: true,
                    IfNotExists: false,
                    ColumnDef: new ColumnDef(
                        Name: Ident.From("b"),
                        DataType: new DataType.Int(null),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    ColumnPosition: new MySQLColumnPosition.First())));
        }

        {
            var alterTable = Assert.IsType<Statement.AlterTable>(Mysql().VerifiedStmt("ALTER TABLE tab ADD COLUMN b INT AFTER foo")).Value;
            Assert.Equal("tab", alterTable.Name.ToString());
            Assert.False(alterTable.IfExists);
            Assert.False(alterTable.Only);
            Assert.Equal(
                alterTable.Operations,
                Sequence.Of<AlterTableOperation>(new AlterTableOperation.AddColumn(
                    ColumnKeyword: true,
                    IfNotExists: false,
                    ColumnDef: new ColumnDef(
                        Name: Ident.From("b"),
                        DataType: new DataType.Int(null),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    ColumnPosition: new MySQLColumnPosition.After(new Ident("foo", null, Span.Empty())))));
        }
    }

    [Fact]
    public void parse_alter_table_add_columns()
    {
        var alterTable = Assert.IsType<Statement.AlterTable>(Mysql()
            .VerifiedStmt("ALTER TABLE tab ADD COLUMN a TEXT FIRST, ADD COLUMN b INT AFTER foo")).Value;
        Assert.Equal("tab", alterTable.Name.ToString());
        Assert.False(alterTable.IfExists);
        Assert.False(alterTable.Only);
        Assert.Equal(
            alterTable.Operations,
            Sequence.Of<AlterTableOperation>(
                new AlterTableOperation.AddColumn(
                    ColumnKeyword: true,
                    IfNotExists: false,
                    ColumnDef: new ColumnDef(
                        Name: Ident.From("a"),
                        DataType: new DataType.Text(),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    ColumnPosition: new MySQLColumnPosition.First()),
                new AlterTableOperation.AddColumn(
                    ColumnKeyword: true,
                    IfNotExists: false,
                    ColumnDef: new ColumnDef(
                        Name: Ident.From("b"),
                        DataType: new DataType.Int(null),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    ColumnPosition: new MySQLColumnPosition.After(new Ident("foo", null, Span.Empty())))));
    }

    [Fact]
    public void parse_alter_table_drop_primary_key()
    {
        Assert.True(
            TestUtils.AlterTableOp(MysqlAndGeneric().VerifiedStmt("ALTER TABLE tab DROP PRIMARY KEY"))
                is AlterTableOperation.DropPrimaryKey { DropBehavior: null });
    }

    [Fact]
    public void parse_alter_table_drop_foreign_key()
    {
        Assert.True(
            TestUtils.AlterTableOp(
                MysqlAndGeneric().VerifiedStmt("ALTER TABLE tab DROP FOREIGN KEY foo_ibfk_1"))
                is AlterTableOperation.DropForeignKey { Name.Value: "foo_ibfk_1" });
    }

    [Fact]
    public void parse_alter_table_change_column()
    {
        var expectedName = ObjectName.From(Sequence.Of(new Ident("orders")));
        var expectedOperation = new AlterTableOperation.ChangeColumn(
            OldName: new Ident("description"),
            NewName: new Ident("desc"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: null);

        var sql1 = "ALTER TABLE orders CHANGE COLUMN description desc TEXT NOT NULL";
        var operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql1), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        var sql2 = "ALTER TABLE orders CHANGE description desc TEXT NOT NULL";
        operation = TestUtils.AlterTableOpWithName(
            Mysql().OneStatementParsesTo(sql2, sql1),
            expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        expectedOperation = new AlterTableOperation.ChangeColumn(
            OldName: new Ident("description"),
            NewName: new Ident("desc"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: new MySQLColumnPosition.First());
        var sql3 = "ALTER TABLE orders CHANGE COLUMN description desc TEXT NOT NULL FIRST";
        operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql3), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        expectedOperation = new AlterTableOperation.ChangeColumn(
            OldName: new Ident("description"),
            NewName: new Ident("desc"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: new MySQLColumnPosition.After(new Ident("foo", null, Span.Empty())));
        var sql4 = "ALTER TABLE orders CHANGE COLUMN description desc TEXT NOT NULL AFTER foo";
        operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql4), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);
    }

    [Fact]
    public void parse_alter_table_change_column_with_column_position()
    {
        var expectedName = ObjectName.From(Sequence.Of(new Ident("orders")));
        var expectedOperationFirst = new AlterTableOperation.ChangeColumn(
            OldName: new Ident("description"),
            NewName: new Ident("desc"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: new MySQLColumnPosition.First());

        var sql1 = "ALTER TABLE orders CHANGE COLUMN description desc TEXT NOT NULL FIRST";
        var operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql1), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperationFirst, operation);

        var sql2 = "ALTER TABLE orders CHANGE description desc TEXT NOT NULL FIRST";
        operation = TestUtils.AlterTableOpWithName(
            Mysql().OneStatementParsesTo(sql2, sql1),
            expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperationFirst, operation);

        var expectedOperationAfter = new AlterTableOperation.ChangeColumn(
            OldName: new Ident("description"),
            NewName: new Ident("desc"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: new MySQLColumnPosition.After(new Ident("total_count", null, Span.Empty())));

        sql1 = "ALTER TABLE orders CHANGE COLUMN description desc TEXT NOT NULL AFTER total_count";
        operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql1), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperationAfter, operation);

        sql2 = "ALTER TABLE orders CHANGE description desc TEXT NOT NULL AFTER total_count";
        operation = TestUtils.AlterTableOpWithName(
            Mysql().OneStatementParsesTo(sql2, sql1),
            expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperationAfter, operation);
    }

    [Fact]
    public void parse_alter_table_modify_column()
    {
        var expectedName = ObjectName.From(Sequence.Of(new Ident("orders")));
        var expectedOperation = new AlterTableOperation.ModifyColumn(
            ColName: new Ident("description"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: null);

        var sql1 = "ALTER TABLE orders MODIFY COLUMN description TEXT NOT NULL";
        var operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql1), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        var sql2 = "ALTER TABLE orders MODIFY description TEXT NOT NULL";
        operation = TestUtils.AlterTableOpWithName(
            Mysql().OneStatementParsesTo(sql2, sql1),
            expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        expectedOperation = new AlterTableOperation.ModifyColumn(
            ColName: new Ident("description"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: new MySQLColumnPosition.First());
        var sql3 = "ALTER TABLE orders MODIFY COLUMN description TEXT NOT NULL FIRST";
        operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql3), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        expectedOperation = new AlterTableOperation.ModifyColumn(
            ColName: new Ident("description"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: new MySQLColumnPosition.After(new Ident("foo", null, Span.Empty())));
        var sql4 = "ALTER TABLE orders MODIFY COLUMN description TEXT NOT NULL AFTER foo";
        operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql4), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);
    }

    [Fact]
    public void parse_alter_table_with_algorithm()
    {
        var sql = "ALTER TABLE tab ALGORITHM = COPY";
        var expectedOperation = new AlterTableOperation.Algorithm(
            EqualsValue: true,
            AlgorithmValue: new AlterTableAlgorithm.Copy());
        var operation = TestUtils.AlterTableOp(MysqlAndGeneric().VerifiedStmt(sql));
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        //  Check order doesn't matter
        sql =
            "ALTER TABLE users DROP COLUMN password_digest, ALGORITHM = COPY, RENAME COLUMN name TO username";
        var stmt = MysqlAndGeneric().VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(
                        new AlterTableOperation.DropColumn(
                            HasColumnKeyword: true,
                            ColumnNames: Sequence.Of(new Ident("password_digest")),
                            IfExists: false,
                            DropBehavior: null),
                        new AlterTableOperation.Algorithm(
                            EqualsValue: true,
                            AlgorithmValue: new AlterTableAlgorithm.Copy()),
                        new AlterTableOperation.RenameColumn(
                            OldColumnName: new Ident("name"),
                            NewColumnName: new Ident("username"))));
                break;
            default:
                Assert.Fail($"Unexpected statement {stmt}");
                break;
        }

        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` ALGORITHM DEFAULT");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` ALGORITHM INSTANT");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` ALGORITHM INPLACE");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` ALGORITHM COPY");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` ALGORITHM = DEFAULT");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` ALGORITHM = INSTANT");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` ALGORITHM = INPLACE");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` ALGORITHM = COPY");
    }

    [Fact]
    public void parse_alter_table_with_lock()
    {
        var sql = "ALTER TABLE tab LOCK = SHARED";
        var expectedOperation = new AlterTableOperation.Lock(
            EqualsValue: true,
            LockValue: new AlterTableLock.Shared());
        var operation = TestUtils.AlterTableOp(MysqlAndGeneric().VerifiedStmt(sql));
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        sql =
            "ALTER TABLE users DROP COLUMN password_digest, LOCK = EXCLUSIVE, RENAME COLUMN name TO username";
        var stmt = MysqlAndGeneric().VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(
                        new AlterTableOperation.DropColumn(
                            HasColumnKeyword: true,
                            ColumnNames: Sequence.Of(new Ident("password_digest")),
                            IfExists: false,
                            DropBehavior: null),
                        new AlterTableOperation.Lock(
                            EqualsValue: true,
                            LockValue: new AlterTableLock.Exclusive()),
                        new AlterTableOperation.RenameColumn(
                            OldColumnName: new Ident("name"),
                            NewColumnName: new Ident("username"))));
                break;
            default:
                Assert.Fail($"Unexpected statement {stmt}");
                break;
        }
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` LOCK DEFAULT");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` LOCK SHARED");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` LOCK NONE");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` LOCK EXCLUSIVE");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` LOCK = DEFAULT");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` LOCK = SHARED");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` LOCK = NONE");
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` LOCK = EXCLUSIVE");
    }

    [Fact]
    public void parse_alter_table_auto_increment()
    {
        var sql = "ALTER TABLE tab AUTO_INCREMENT = 42";
        var expectedOperation = new AlterTableOperation.AutoIncrement(
            EqualsValue: true,
            Value: TestUtils.Number("42").WithEmptySpan());
        var operation = TestUtils.AlterTableOp(Mysql().VerifiedStmt(sql));
        Assert.Equal<AlterTableOperation>(expectedOperation, operation);

        MysqlAndGeneric().VerifiedStmt("ALTER TABLE `users` AUTO_INCREMENT 42");
    }

    [Fact]
    public void parse_alter_table_modify_column_with_column_position()
    {
        var expectedName = ObjectName.From(Sequence.Of(new Ident("orders")));
        var expectedOperationFirst = new AlterTableOperation.ModifyColumn(
            ColName: new Ident("description"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: new MySQLColumnPosition.First());

        var sql1 = "ALTER TABLE orders MODIFY COLUMN description TEXT NOT NULL FIRST";
        var operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql1), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperationFirst, operation);

        var sql2 = "ALTER TABLE orders MODIFY description TEXT NOT NULL FIRST";
        operation = TestUtils.AlterTableOpWithName(
            Mysql().OneStatementParsesTo(sql2, sql1),
            expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperationFirst, operation);

        var expectedOperationAfter = new AlterTableOperation.ModifyColumn(
            ColName: new Ident("description"),
            DataType: new DataType.Text(),
            Options: Sequence.Of<ColumnOption>(new ColumnOption.NotNull()),
            ColumnPosition: new MySQLColumnPosition.After(new Ident("total_count", null, Span.Empty())));

        sql1 = "ALTER TABLE orders MODIFY COLUMN description TEXT NOT NULL AFTER total_count";
        operation =
            TestUtils.AlterTableOpWithName(Mysql().VerifiedStmt(sql1), expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperationAfter, operation);

        sql2 = "ALTER TABLE orders MODIFY description TEXT NOT NULL AFTER total_count";
        operation = TestUtils.AlterTableOpWithName(
            Mysql().OneStatementParsesTo(sql2, sql1),
            expectedName.ToString());
        Assert.Equal<AlterTableOperation>(expectedOperationAfter, operation);
    }

    [Fact]
    public void parse_substring_in_select()
    {
        var sql = "SELECT DISTINCT SUBSTRING(description, 0, 1) FROM test";
        var query = Assert.IsType<Statement.Query>(Mysql().OneStatementParsesTo(
            sql,
            "SELECT DISTINCT SUBSTRING(description, 0, 1) FROM test")).Value;
        Assert.Equal(
            new Query(
                With: null,
                Body: new SetExpr.Select(new Select(
                    SelectToken: AttachedToken.Empty(),
                    OptimizerHints: Sequence<OptimizerHint>.Empty,
                    Distinct: new Distinct.DistinctValue(),
                    SelectModifiers: null,
                    Top: null,
                    TopBeforeDistinct: false,
                    Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Substring(
                        Expr: new Expr.Identifier(new Ident("description", null, Span.Empty())),
                        SubstringFrom: new Expr.Value(
                            TestUtils.Number("0").WithEmptySpan()),
                        SubstringFor: new Expr.Value(
                            TestUtils.Number("1").WithEmptySpan()),
                        Special: true,
                        Shorthand: false))),
                    Exclude: null,
                    Into: null,
                    From: Sequence.Of(new TableWithJoins(
                        Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(
                            new Ident("test", null, Span.Empty())))),
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
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty),
            query);
    }

    [Fact]
    public void parse_show_variables()
    {
        MysqlAndGeneric().VerifiedStmt("SHOW VARIABLES");
        MysqlAndGeneric().VerifiedStmt("SHOW VARIABLES LIKE 'admin%'");
        MysqlAndGeneric().VerifiedStmt("SHOW VARIABLES WHERE value = '3306'");
        MysqlAndGeneric().VerifiedStmt("SHOW GLOBAL VARIABLES");
        MysqlAndGeneric().VerifiedStmt("SHOW GLOBAL VARIABLES LIKE 'admin%'");
        MysqlAndGeneric().VerifiedStmt("SHOW GLOBAL VARIABLES WHERE value = '3306'");
        MysqlAndGeneric().VerifiedStmt("SHOW SESSION VARIABLES");
        MysqlAndGeneric().VerifiedStmt("SHOW SESSION VARIABLES LIKE 'admin%'");
        MysqlAndGeneric().VerifiedStmt("SHOW GLOBAL VARIABLES WHERE value = '3306'");
    }

    [Fact]
    public void parse_rlike_and_regexp()
    {
        foreach (var s in new[]
        {
            "SELECT 1 WHERE 'a' RLIKE '^a$'",
            "SELECT 1 WHERE 'a' REGEXP '^a$'",
            "SELECT 1 WHERE 'a' NOT RLIKE '^a$'",
            "SELECT 1 WHERE 'a' NOT REGEXP '^a$'",
        })
        {
            MysqlAndGeneric().VerifiedOnlySelect(s);
        }
    }

    [Fact]
    public void parse_like_with_escape()
    {
        // verify backslash is not stripped for escaped wildcards
        Mysql().VerifiedOnlySelect(@"SELECT 'a\%c' LIKE 'a\%c'");
        Mysql().VerifiedOnlySelect(@"SELECT 'a\_c' LIKE 'a\_c'");
        Mysql().VerifiedOnlySelect(@"SELECT '%\_\%' LIKE '%\_\%'");
        Mysql().VerifiedOnlySelect(@"SELECT '\_\%' LIKE CONCAT('\_', '\%')");
        Mysql().VerifiedOnlySelect(@"SELECT 'a%c' LIKE 'a$%c' ESCAPE '$'");
        Mysql().VerifiedOnlySelect(@"SELECT 'a_c' LIKE 'a#_c' ESCAPE '#'");
    }

    [Fact]
    public void parse_kill()
    {
        var stmt = MysqlAndGeneric().VerifiedStmt("KILL CONNECTION 5");
        Assert.Equal(
            stmt,
            new Statement.Kill(
                Modifier: new KillType.Connection(),
                Id: 5));

        stmt = MysqlAndGeneric().VerifiedStmt("KILL QUERY 5");
        Assert.Equal(
            stmt,
            new Statement.Kill(
                Modifier: new KillType.Query(),
                Id: 5));

        stmt = MysqlAndGeneric().VerifiedStmt("KILL 5");
        Assert.Equal(
            stmt,
            new Statement.Kill(
                Modifier: null,
                Id: 5));
    }

    [Fact]
    public void parse_table_column_option_on_update()
    {
        var sql1 = "CREATE TABLE foo (`modification_time` DATETIME ON UPDATE CURRENT_TIMESTAMP())";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql1)).Value;
        Assert.Equal("foo", createTable.Name.ToString());
        Assert.Equal(
            Sequence.Of(new ColumnDef(
                Name: Ident.WithQuote('`', "modification_time"),
                DataType: new DataType.Datetime(null),
                Options: Sequence.Of(new ColumnOptionDef(
                    Name: null,
                    Option: new ColumnOption.OnUpdate(TestUtils.Call("CURRENT_TIMESTAMP", System.Array.Empty<Expr>())))))),
            createTable.Columns);
    }

    [Fact]
    public void parse_set_names()
    {
        var stmt = MysqlAndGeneric().VerifiedStmt("SET NAMES utf8mb4");
        Assert.Equal(
            stmt,
            new Statement.Set(new Set.SetNames(
                CharsetName: Ident.From("utf8mb4"),
                CollationName: null)));

        stmt = MysqlAndGeneric().VerifiedStmt("SET NAMES utf8mb4 COLLATE bogus");
        Assert.Equal(
            stmt,
            new Statement.Set(new Set.SetNames(
                CharsetName: Ident.From("utf8mb4"),
                CollationName: "bogus")));

        // `let stmt = ..parse_sql_statements(..).unwrap()` sombreia `stmt` com um `Vec<Statement>`.
        var stmts = MysqlAndGeneric()
            .ParseSqlStatements("set names utf8mb4 collate bogus");
        Assert.Equal(
            stmts,
            Sequence.Of<Statement>(new Statement.Set(new Set.SetNames(
                CharsetName: Ident.From("utf8mb4"),
                CollationName: "bogus"))));

        stmt = MysqlAndGeneric().VerifiedStmt("SET NAMES DEFAULT");
        Assert.Equal(stmt, new Statement.Set(new Set.SetNamesDefault()));
    }

    [Fact]
    public void parse_limit_my_sql_syntax()
    {
        MysqlAndGeneric().VerifiedStmt("SELECT id, fname, lname FROM customer LIMIT 10 OFFSET 5");
        MysqlAndGeneric().VerifiedStmt("SELECT id, fname, lname FROM customer LIMIT 5, 10");
        MysqlAndGeneric().VerifiedStmt("SELECT * FROM user LIMIT ? OFFSET ?");
    }

    [Fact]
    public void parse_create_table_with_index_definition()
    {
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE tb (id INT, INDEX (id))",
            "CREATE TABLE tb (id INT, INDEX (id))");

        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE tb (id INT, index USING BTREE (id))",
            "CREATE TABLE tb (id INT, INDEX USING BTREE (id))");

        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE tb (id INT, KEY USING HASH (id))",
            "CREATE TABLE tb (id INT, KEY USING HASH (id))");

        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE tb (id INT, key index (id))",
            "CREATE TABLE tb (id INT, KEY index (id))");

        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE tb (id INT, INDEX 'index' (id))",
            "CREATE TABLE tb (id INT, INDEX 'index' (id))");

        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE tb (id INT, INDEX index USING BTREE (id))",
            "CREATE TABLE tb (id INT, INDEX index USING BTREE (id))");

        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE tb (id INT, INDEX index USING HASH (id))",
            "CREATE TABLE tb (id INT, INDEX index USING HASH (id))");

        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE tb (id INT, INDEX (c1, c2, c3, c4,c5))",
            "CREATE TABLE tb (id INT, INDEX (c1, c2, c3, c4, c5))");
    }

    [Fact]
    public void parse_create_table_unallow_constraint_then_index()
    {
        var sql = "CREATE TABLE foo (bar INT, CONSTRAINT constr INDEX index (bar))";
        Assert.Throws<ParserException>(() => MysqlAndGeneric().ParseSqlStatements(sql));

        sql = "CREATE TABLE foo (bar INT, INDEX index (bar))";
        // assert!(..is_ok()): se lançar, o teste falha.
        MysqlAndGeneric().ParseSqlStatements(sql);
    }

    [Fact]
    public void parse_create_table_constraint_check_without_name()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsConstraintKeywordWithoutName);
        dialects.OneStatementParsesTo(
            "CREATE TABLE t (x INT, CONSTRAINT PRIMARY KEY (x))",
            "CREATE TABLE t (x INT, PRIMARY KEY (x))");
        dialects.OneStatementParsesTo(
            "CREATE TABLE t (x INT, CONSTRAINT UNIQUE (x))",
            "CREATE TABLE t (x INT, UNIQUE (x))");
        dialects.OneStatementParsesTo(
            "CREATE TABLE t (x INT, CONSTRAINT FOREIGN KEY (x) REFERENCES t2(id))",
            "CREATE TABLE t (x INT, FOREIGN KEY (x) REFERENCES t2(id))");
        dialects.OneStatementParsesTo(
            "CREATE TABLE t (x INT, CONSTRAINT CHECK (x > 1))",
            "CREATE TABLE t (x INT, CHECK (x > 1))");
    }

    [Fact]
    public void parse_create_table_with_fulltext_definition()
    {
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, FULLTEXT (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, FULLTEXT INDEX (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, FULLTEXT KEY (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, FULLTEXT potato (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, FULLTEXT INDEX potato (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, FULLTEXT KEY potato (id))");

        MysqlAndGeneric()
            .VerifiedStmt("CREATE TABLE tb (c1 INT, c2 INT, FULLTEXT KEY potato (c1, c2))");
    }

    [Fact]
    public void parse_create_table_with_spatial_definition()
    {
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, SPATIAL (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, SPATIAL INDEX (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, SPATIAL KEY (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, SPATIAL potato (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, SPATIAL INDEX potato (id))");

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (id INT, SPATIAL KEY potato (id))");

        MysqlAndGeneric()
            .VerifiedStmt("CREATE TABLE tb (c1 INT, c2 INT, SPATIAL KEY potato (c1, c2))");
    }

    [Fact]
    public void parse_fulltext_expression()
    {
        MysqlAndGeneric().VerifiedStmt("SELECT * FROM tb WHERE MATCH (c1) AGAINST ('string')");

        MysqlAndGeneric().VerifiedStmt(
            "SELECT * FROM tb WHERE MATCH (c1) AGAINST ('string' IN NATURAL LANGUAGE MODE)");

        MysqlAndGeneric().VerifiedStmt("SELECT * FROM tb WHERE MATCH (c1) AGAINST ('string' IN NATURAL LANGUAGE MODE WITH QUERY EXPANSION)");

        MysqlAndGeneric()
            .VerifiedStmt("SELECT * FROM tb WHERE MATCH (c1) AGAINST ('string' IN BOOLEAN MODE)");

        MysqlAndGeneric()
            .VerifiedStmt("SELECT * FROM tb WHERE MATCH (c1) AGAINST ('string' WITH QUERY EXPANSION)");

        MysqlAndGeneric()
            .VerifiedStmt("SELECT * FROM tb WHERE MATCH (c1, c2, c3) AGAINST ('string')");

        MysqlAndGeneric().VerifiedStmt("SELECT * FROM tb WHERE MATCH (c1) AGAINST (123)");

        MysqlAndGeneric().VerifiedStmt("SELECT * FROM tb WHERE MATCH (c1) AGAINST (NULL)");

        MysqlAndGeneric().VerifiedStmt("SELECT COUNT(IF(MATCH (title, body) AGAINST ('database' IN NATURAL LANGUAGE MODE), 1, NULL)) AS count FROM articles");
    }

    // #[should_panic = "Expected: FULLTEXT or SPATIAL option without constraint name, found: cons"]
    [Fact]
    public void parse_create_table_with_fulltext_definition_should_not_accept_constraint_name()
    {
        var ex = Assert.ThrowsAny<Exception>(() =>
            MysqlAndGeneric().VerifiedStmt("CREATE TABLE tb (c1 INT, CONSTRAINT cons FULLTEXT (c1))"));
        Assert.Contains("Expected: FULLTEXT or SPATIAL option without constraint name, found: cons", ex.Message);
    }

    [Fact]
    public void parse_values()
    {
        Mysql().VerifiedStmt("VALUES ROW(1, true, 'a')");
        Mysql().VerifiedStmt("SELECT a, c FROM (VALUES ROW(1, true, 'a'), ROW(2, false, 'b'), ROW(3, false, 'c')) AS t (a, b, c)");
    }

    [Fact]
    public void parse_hex_string_introducer()
    {
        Assert.Equal(
            Mysql().VerifiedStmt("SELECT _latin1 X'4D7953514C'"),
            new Statement.Query(new Query(
                With: null,
                Body: new SetExpr.Select(new Select(
                    SelectToken: AttachedToken.Empty(),
                    OptimizerHints: Sequence<OptimizerHint>.Empty,
                    Distinct: null,
                    SelectModifiers: null,
                    Top: null,
                    TopBeforeDistinct: false,
                    Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Prefixed(
                        Prefix: Ident.From("_latin1"),
                        Value: new Expr.Value(
                            new Value.HexStringLiteral("4D7953514C").WithEmptySpan())))),
                    Exclude: null,
                    From: Sequence<TableWithJoins>.Empty,
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
                    Into: null,
                    ConnectBy: Sequence<ConnectByKind>.Empty,
                    Flavor: SelectFlavor.Standard)),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty)));
    }

    [Fact]
    public void parse_string_introducers()
    {
        Mysql().VerifiedStmt("SELECT _binary 'abc'");
        Mysql().OneStatementParsesTo("SELECT _utf8'abc'", "SELECT _utf8 'abc'");
        Mysql().OneStatementParsesTo("SELECT _utf8mb4'abc'", "SELECT _utf8mb4 'abc'");
        Mysql().VerifiedStmt("SELECT _binary 'abc', _utf8mb4 'abc'");
    }

    [Fact]
    public void parse_div_infix()
    {
        Mysql().VerifiedStmt("SELECT 5 DIV 2");
    }

    [Fact]
    public void parse_div_infix_propagates_parse_error()
    {
        // .expect_err("expected an error")
        var err = Assert.Throws<ParserException>(() => Mysql()
            .ParseSqlStatements("SELECT 5 DIV")).Error;
        Assert.IsType<ParserError.ParserErrorValue>(err);
    }

    [Fact]
    public void parse_div_precedence()
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
            Mysql().VerifiedExpr("7 DIV 2 + 1"));

        // Equal precedence resolves left-associatively, both against `*` and against itself.
        Assert.Equal(
            new Expr.BinaryOp(
                Left: div(num("9"), num("3")),
                Op: new BinaryOperator.Multiply(),
                Right: num("3")),
            Mysql().VerifiedExpr("9 DIV 3 * 3"));
        Assert.Equal(
            div(div(num("10"), num("5")), num("2")),
            Mysql().VerifiedExpr("10 DIV 5 DIV 2"));

        Assert.Equal(
            new Expr.BinaryOp(
                Left: div(new Expr.Identifier(new Ident("a")), num("2")),
                Op: new BinaryOperator.Eq(),
                Right: num("1")),
            Mysql().VerifiedExpr("a DIV 2 = 1"));

        // Explicit parentheses still push the whole expression into the right operand.
        Assert.Equal(
            div(
                num("7"),
                new Expr.Nested(new Expr.BinaryOp(
                    Left: num("2"),
                    Op: new BinaryOperator.Plus(),
                    Right: num("1")))),
            Mysql().VerifiedExpr("7 DIV (2 + 1)"));
    }

    [Fact]
    public void parse_drop_temporary_table()
    {
        var sql = "DROP TEMPORARY TABLE foo";
        // Statement::Drop { object_type, if_exists, names, cascade, purge: _, temporary, .. }
        var drop = Assert.IsType<Statement.Drop>(Mysql().VerifiedStmt(sql));
        Assert.False(drop.IfExists);
        Assert.Equal(new ObjectType.Table(), drop.ObjectType);
        Assert.Equal(
            new[] { "foo" },
            drop.Names.Select(n => n.ToString()).ToList());
        Assert.False(drop.Cascade);
        Assert.True(drop.Temporary);
    }

    [Fact]
    public void parse_convert_using()
    {
        // https://dev.mysql.com/doc/refman/8.0/en/cast-functions.html#function_convert

        // CONVERT(expr USING transcoding_name)
        Mysql().VerifiedOnlySelect("SELECT CONVERT('x' USING latin1)");
        Mysql().VerifiedOnlySelect("SELECT CONVERT(my_column USING utf8mb4) FROM my_table");

        // CONVERT(expr, type)
        Mysql().VerifiedOnlySelect("SELECT CONVERT('abc', CHAR(60))");
        Mysql().VerifiedOnlySelect("SELECT CONVERT(123.456, DECIMAL(5,2))");
        // with a type + a charset
        Mysql().VerifiedOnlySelect("SELECT CONVERT('test', CHAR CHARACTER SET utf8mb4)");
    }

    [Fact]
    public void parse_create_table_with_column_collate()
    {
        var sql = "CREATE TABLE tb (id TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci)";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        Assert.Equal("tb", createTable.Name.ToString());
        Assert.Equal(
            Sequence.Of(new ColumnDef(
                Name: new Ident("id"),
                DataType: new DataType.Text(),
                Options: Sequence.Of(
                    new ColumnOptionDef(
                        Name: null,
                        Option: new ColumnOption.CharacterSet(ObjectName.From(Sequence.Of(new Ident(
                            "utf8mb4"))))),
                    new ColumnOptionDef(
                        Name: null,
                        Option: new ColumnOption.Collation(ObjectName.From(Sequence.Of(new Ident(
                            "utf8mb4_0900_ai_ci")))))))),
            createTable.Columns);
    }

    [Fact]
    public void parse_lock_tables()
    {
        Mysql().OneStatementParsesTo(
            "LOCK TABLES trans t READ, customer WRITE",
            "LOCK TABLES trans AS t READ, customer WRITE");
        Mysql().VerifiedStmt("LOCK TABLES trans AS t READ, customer WRITE");
        Mysql().VerifiedStmt("LOCK TABLES trans AS t READ LOCAL, customer WRITE");
        Mysql().VerifiedStmt("LOCK TABLES trans AS t READ, customer LOW_PRIORITY WRITE");
        Mysql().VerifiedStmt("UNLOCK TABLES");
    }

    [Fact]
    public void parse_json_table()
    {
        Mysql().VerifiedOnlySelect("SELECT * FROM JSON_TABLE('[[1, 2], [3, 4]]', '$[*]' COLUMNS(a INT PATH '$[0]', b INT PATH '$[1]')) AS t");
        Mysql().VerifiedOnlySelect(
            """SELECT * FROM JSON_TABLE('["x", "y"]', '$[*]' COLUMNS(a VARCHAR(20) PATH '$')) AS t""");
        // with a bound parameter
        Mysql().VerifiedOnlySelect(
            """SELECT * FROM JSON_TABLE(?, '$[*]' COLUMNS(a VARCHAR(20) PATH '$')) AS t""");
        // quote escaping
        Mysql().VerifiedOnlySelect("""SELECT * FROM JSON_TABLE('{"''": [1,2,3]}', '$."''"[*]' COLUMNS(a VARCHAR(20) PATH '$')) AS t""");
        // double quotes
        Mysql().VerifiedOnlySelect(
            """SELECT * FROM JSON_TABLE("[]", "$[*]" COLUMNS(a VARCHAR(20) PATH "$")) AS t""");
        // exists
        Mysql().VerifiedOnlySelect("""SELECT * FROM JSON_TABLE('[{}, {"x":1}]', '$[*]' COLUMNS(x INT EXISTS PATH '$.x')) AS t""");
        // error handling
        Mysql().VerifiedOnlySelect(
            """SELECT * FROM JSON_TABLE('[1,2]', '$[*]' COLUMNS(x INT PATH '$' ERROR ON ERROR)) AS t""");
        Mysql().VerifiedOnlySelect(
            """SELECT * FROM JSON_TABLE('[1,2]', '$[*]' COLUMNS(x INT PATH '$' ERROR ON EMPTY)) AS t""");
        Mysql().VerifiedOnlySelect("""SELECT * FROM JSON_TABLE('[1,2]', '$[*]' COLUMNS(x INT PATH '$' ERROR ON EMPTY DEFAULT '0' ON ERROR)) AS t""");
        Mysql().VerifiedOnlySelect(
            """SELECT jt.* FROM JSON_TABLE('["Alice", "Bob", "Charlie"]', '$[*]' COLUMNS(row_num FOR ORDINALITY, name VARCHAR(50) PATH '$')) AS jt""");
        Mysql().VerifiedOnlySelect(
            """SELECT * FROM JSON_TABLE('[ {"a": 1, "b": [11,111]}, {"a": 2, "b": [22,222]}, {"a":3}]', '$[*]' COLUMNS(a INT PATH '$.a', NESTED PATH '$.b[*]' COLUMNS (b INT PATH '$'))) AS jt""");
        Assert.Equal(
            Mysql()
                .VerifiedOnlySelect(
                    """SELECT * FROM JSON_TABLE('[1,2]', '$[*]' COLUMNS(x INT PATH '$' DEFAULT '0' ON EMPTY NULL ON ERROR)) AS t""")
                .From[0]
                .Relation,
            new TableFactor.JsonTable(
                JsonExpr: new Expr.Value(new Value.SingleQuotedString("[1,2]").WithEmptySpan()),
                JsonPath: new Value.SingleQuotedString("$[*]").WithEmptySpan(),
                Columns: Sequence.Of<JsonTableColumn>(
                    new JsonTableColumn.Named(new JsonTableNamedColumn(
                        Name: new Ident("x"),
                        Type: new DataType.Int(null),
                        Path: new Value.SingleQuotedString("$").WithEmptySpan(),
                        Exists: false,
                        OnEmpty: new JsonTableColumnErrorHandling.Default(new Value.SingleQuotedString("0").WithEmptySpan()),
                        OnError: new JsonTableColumnErrorHandling.Null()))),
                Alias: TestUtils.TableAlias(true, "t")));
    }

    [Fact]
    public void test_group_concat()
    {
        // examples taken from mysql docs
        // https://dev.mysql.com/doc/refman/8.0/en/aggregate-functions.html#function_group-concat
        MysqlAndGeneric().VerifiedExpr("GROUP_CONCAT(DISTINCT test_score)");
        MysqlAndGeneric().VerifiedExpr("GROUP_CONCAT(test_score ORDER BY test_score)");
        MysqlAndGeneric().VerifiedExpr("GROUP_CONCAT(test_score SEPARATOR ' ')");
        MysqlAndGeneric()
            .VerifiedExpr("GROUP_CONCAT(DISTINCT test_score ORDER BY test_score DESC SEPARATOR ' ')");
    }

    /// <summary>The XOR binary operator is only supported in MySQL</summary>
    [Fact]
    public void parse_logical_xor()
    {
        var sql = "SELECT true XOR true, false XOR false, true XOR false, false XOR true";
        var select = MysqlAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Value(new Value.Boolean(true).WithEmptySpan()),
                Op: new BinaryOperator.Xor(),
                Right: new Expr.Value(new Value.Boolean(true).WithEmptySpan()))),
            select.Projection[0]);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Value(new Value.Boolean(false).WithEmptySpan()),
                Op: new BinaryOperator.Xor(),
                Right: new Expr.Value(new Value.Boolean(false).WithEmptySpan()))),
            select.Projection[1]);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Value(new Value.Boolean(true).WithEmptySpan()),
                Op: new BinaryOperator.Xor(),
                Right: new Expr.Value(new Value.Boolean(false).WithEmptySpan()))),
            select.Projection[2]);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Value(new Value.Boolean(false).WithEmptySpan()),
                Op: new BinaryOperator.Xor(),
                Right: new Expr.Value(new Value.Boolean(true).WithEmptySpan()))),
            select.Projection[3]);
    }

    [Fact]
    public void parse_bitstring_literal()
    {
        var select = MysqlAndGeneric().VerifiedOnlySelect("SELECT B'111'");
        Assert.Equal(
            select.Projection,
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Value(
                new Value.SingleQuotedByteStringLiteral("111").WithEmptySpan()))));
    }

    [Fact]
    public void parse_grant()
    {
        var sql = "GRANT ALL ON *.* TO 'jeffrey'@'%'";
        var stmt = Mysql().VerifiedStmt(sql);
        // if let Statement::Grant(Grant { privileges, objects, grantees, with_grant_option,
        //     as_grantor: _, granted_by, current_grants: _ }) = stmt
        if (stmt is Statement.Grant { Value: var grant })
        {
            Assert.Equal(
                grant.Privileges,
                new Privileges.All(WithPrivilegesKeyword: false));
            Assert.Equal(
                grant.Objects,
                new GrantObjects.Tables(Sequence.Of(ObjectName.From(Sequence.Of(
                    Ident.From("*"),
                    Ident.From("*"))))));
            Assert.False(grant.WithGrantOption);
            Assert.Null(grant.GrantedBy);
            // if let [Grantee { grantee_type: GranteesType::None, name: Some(GranteeName::UserHost { user, host }) }] = grantees.as_slice()
            if (grant.Grantees.Count == 1
                && grant.Grantees[0] is { GranteeType: GranteesType.None, Name: GranteeName.UserHost { User: var user, Host: var host } })
            {
                Assert.Equal("jeffrey", user.Value);
                Assert.Equal('\'', user.QuoteStyle);
                Assert.Equal("%", host.Value);
                Assert.Equal('\'', host.QuoteStyle);
            }
            else
            {
                throw new InvalidOperationException(Unreachable);
            }
        }
        else
        {
            throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_revoke()
    {
        var sql = "REVOKE ALL ON db1.* FROM 'jeffrey'@'%'";
        var stmt = MysqlAndGeneric().VerifiedStmt(sql);
        // if let Statement::Revoke(Revoke { grant_option_for: false, privileges, objects, grantees,
        //     granted_by, cascade }) = stmt
        if (stmt is Statement.Revoke { Value: { GrantOptionFor: false } revoke })
        {
            Assert.Equal(
                revoke.Privileges,
                new Privileges.All(WithPrivilegesKeyword: false));
            Assert.Equal(
                revoke.Objects,
                new GrantObjects.Tables(Sequence.Of(ObjectName.From(Sequence.Of(
                    Ident.From("db1"),
                    Ident.From("*"))))));
            if (revoke.Grantees.Count == 1
                && revoke.Grantees[0] is { GranteeType: GranteesType.None, Name: GranteeName.UserHost { User: var user, Host: var host } })
            {
                Assert.Equal("jeffrey", user.Value);
                Assert.Equal('\'', user.QuoteStyle);
                Assert.Equal("%", host.Value);
                Assert.Equal('\'', host.QuoteStyle);
            }
            else
            {
                throw new InvalidOperationException(Unreachable);
            }
            Assert.Null(revoke.GrantedBy);
            Assert.Null(revoke.Cascade);
        }
        else
        {
            throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_view_algorithm_param()
    {
        var sql = "CREATE ALGORITHM = MERGE VIEW foo AS SELECT 1";
        var stmt = Mysql().VerifiedStmt(sql);
        if (stmt is Statement.CreateView { Value.Params: { Algorithm: var algorithm, Definer: var definer, Security: var security } })
        {
            Assert.Equal(algorithm, new CreateViewAlgorithm.Merge());
            Assert.Null(definer);
            Assert.Null(security);
        }
        else
        {
            throw new InvalidOperationException(Unreachable);
        }
        Mysql().VerifiedStmt("CREATE ALGORITHM = UNDEFINED VIEW foo AS SELECT 1");
        Mysql().VerifiedStmt("CREATE ALGORITHM = TEMPTABLE VIEW foo AS SELECT 1");
    }

    [Fact]
    public void parse_create_view_definer_param()
    {
        var sql = "CREATE DEFINER = 'jeffrey'@'localhost' VIEW foo AS SELECT 1";
        var stmt = Mysql().VerifiedStmt(sql);
        if (stmt is Statement.CreateView { Value.Params: { Algorithm: var algorithm, Definer: var definer, Security: var security } })
        {
            Assert.Null(algorithm);
            if (definer is GranteeName.UserHost { User: var user, Host: var host })
            {
                Assert.Equal("jeffrey", user.Value);
                Assert.Equal('\'', user.QuoteStyle);
                Assert.Equal("localhost", host.Value);
                Assert.Equal('\'', host.QuoteStyle);
            }
            else
            {
                throw new InvalidOperationException(Unreachable);
            }
            Assert.Null(security);
        }
        else
        {
            throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_view_security_param()
    {
        var sql = "CREATE SQL SECURITY DEFINER VIEW foo AS SELECT 1";
        var stmt = Mysql().VerifiedStmt(sql);
        if (stmt is Statement.CreateView { Value.Params: { Algorithm: var algorithm, Definer: var definer, Security: var security } })
        {
            Assert.Null(algorithm);
            Assert.Null(definer);
            Assert.Equal(security, new CreateViewSecurity.Definer());
        }
        else
        {
            throw new InvalidOperationException(Unreachable);
        }
        Mysql().VerifiedStmt("CREATE SQL SECURITY INVOKER VIEW foo AS SELECT 1");
    }

    [Fact]
    public void parse_create_view_multiple_params()
    {
        var sql = "CREATE ALGORITHM = UNDEFINED DEFINER = `root`@`%` SQL SECURITY INVOKER VIEW foo AS SELECT 1";
        var stmt = Mysql().VerifiedStmt(sql);
        if (stmt is Statement.CreateView { Value.Params: { Algorithm: var algorithm, Definer: var definer, Security: var security } })
        {
            Assert.Equal(algorithm, new CreateViewAlgorithm.Undefined());
            if (definer is GranteeName.UserHost { User: var user, Host: var host })
            {
                Assert.Equal("root", user.Value);
                Assert.Equal('`', user.QuoteStyle);
                Assert.Equal("%", host.Value);
                Assert.Equal('`', host.QuoteStyle);
            }
            else
            {
                throw new InvalidOperationException(Unreachable);
            }
            Assert.Equal(security, new CreateViewSecurity.Invoker());
        }
        else
        {
            throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_longblob_type()
    {
        var sql = "CREATE TABLE foo (bar LONGBLOB)";
        var stmt = MysqlAndGeneric().VerifiedStmt(sql);
        if (stmt is Statement.CreateTable { Value.Columns: var columns })
        {
            Assert.Equal(1, columns.Count);
            Assert.Equal(columns[0].DataType, new DataType.LongBlob());
        }
        else
        {
            throw new InvalidOperationException(Unreachable);
        }
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE foo (bar TINYBLOB)");
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE foo (bar MEDIUMBLOB)");
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE foo (bar TINYTEXT)");
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE foo (bar MEDIUMTEXT)");
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE foo (bar LONGTEXT)");
    }

    [Fact]
    public void parse_begin_without_transaction()
    {
        Mysql().VerifiedStmt("BEGIN");
    }

    [Fact]
    public void parse_geometric_types_srid_option()
    {
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE t (a geometry SRID 4326)");
    }

    [Fact]
    public void parse_double_precision()
    {
        Mysql().VerifiedStmt("CREATE TABLE foo (bar DOUBLE)");
        Mysql().VerifiedStmt("CREATE TABLE foo (bar DOUBLE(11,0))");
        Mysql().OneStatementParsesTo(
            "CREATE TABLE foo (bar DOUBLE(11, 0))",
            "CREATE TABLE foo (bar DOUBLE(11,0))");
    }

    [Fact]
    public void parse_looks_like_single_line_comment()
    {
        Mysql().OneStatementParsesTo(
            "UPDATE account SET balance=balance--1 WHERE account_id=5752",
            "UPDATE account SET balance = balance - -1 WHERE account_id = 5752");
        // r#"..."# de várias linhas: quebras `\n` explícitas e indentação idêntica à do Rust.
        Mysql().OneStatementParsesTo(
            "\n"
            + "            UPDATE account SET balance=balance-- 1\n"
            + "            WHERE account_id=5752\n"
            + "        ",
            "UPDATE account SET balance = balance WHERE account_id = 5752");
    }

    [Fact]
    public void parse_create_trigger()
    {
        var sqlCreateTrigger = "CREATE TRIGGER emp_stamp BEFORE INSERT ON emp FOR EACH ROW EXECUTE FUNCTION emp_stamp()";
        var createStmt = Mysql().VerifiedStmt(sqlCreateTrigger);
        Assert.Equal(
            createStmt,
            new Statement.CreateTrigger(new CreateTrigger(
                OrAlter: false,
                Temporary: false,
                OrReplace: false,
                IsConstraint: false,
                Name: ObjectName.From(Sequence.Of(new Ident("emp_stamp"))),
                Period: new TriggerPeriod.Before(),
                PeriodBeforeTable: true,
                Events: Sequence.Of<TriggerEvent>(new TriggerEvent.Insert()),
                TableName: ObjectName.From(Sequence.Of(new Ident("emp"))),
                ReferencedTableName: null,
                Referencing: Sequence<TriggerReferencing>.Empty,
                TriggerObject: new TriggerObjectKind.ForEach(new TriggerObject.Row()),
                Condition: null,
                ExecBody: new TriggerExecBody(
                    ExecType: new TriggerExecBodyType.Function(),
                    FuncDesc: new FunctionDesc(
                        Name: ObjectName.From(Sequence.Of(new Ident("emp_stamp"))),
                        Args: Sequence<OperateFunctionArg>.Empty)),
                StatementsAs: false,
                Statements: null,
                Characteristics: null)));
    }

    [Fact]
    public void parse_create_trigger_compound_statement()
    {
        MysqlAndGeneric().VerifiedStmt("CREATE TRIGGER mytrigger BEFORE INSERT ON mytable FOR EACH ROW BEGIN SET NEW.a = 1; SET NEW.b = 2; END");
        MysqlAndGeneric().VerifiedStmt("CREATE TRIGGER tr AFTER INSERT ON t1 FOR EACH ROW BEGIN INSERT INTO t2 VALUES (NEW.id); END");
    }

    [Fact]
    public void parse_drop_trigger()
    {
        var sqlDropTrigger = "DROP TRIGGER emp_stamp;";
        var dropStmt = Mysql().OneStatementParsesTo(sqlDropTrigger, "");
        Assert.Equal(
            dropStmt,
            new Statement.DropTrigger(new DropTrigger(
                IfExists: false,
                TriggerName: ObjectName.From(Sequence.Of(new Ident("emp_stamp"))),
                TableName: null,
                Option: null)));
    }

    [Fact]
    public void parse_cast_integers()
    {
        Mysql().VerifiedExpr("CAST(foo AS UNSIGNED)");
        Mysql().VerifiedExpr("CAST(foo AS SIGNED)");
        Mysql().VerifiedExpr("CAST(foo AS UNSIGNED INTEGER)");
        Mysql().VerifiedExpr("CAST(foo AS SIGNED INTEGER)");

        // .expect_err("CAST doesn't allow display width")
        Assert.Throws<ParserException>(() => Mysql()
            .RunParserMethod("CAST(foo AS UNSIGNED(3))", p => p.ParseExpr()));
        Assert.Throws<ParserException>(() => Mysql()
            .RunParserMethod("CAST(foo AS UNSIGNED(3) INTEGER)", p => p.ParseExpr()));
        Assert.Throws<ParserException>(() => Mysql()
            .RunParserMethod("CAST(foo AS UNSIGNED INTEGER(3))", p => p.ParseExpr()));
    }

    [Fact]
    public void parse_cast_array()
    {
        // The element type may be any type accepted by CAST().
        foreach (var ty in new[]
        {
            "SIGNED",
            "UNSIGNED",
            "CHAR",
            "CHAR(10)",
            "BINARY",
            "BINARY(5)",
            "DATE",
            "TIME",
            "DATETIME",
            "DECIMAL",
            "DECIMAL(10,2)",
            "DOUBLE",
            "FLOAT",
            "YEAR",
        })
        {
            Mysql().VerifiedExpr($"CAST(foo AS {ty} ARRAY)");
        }

        // `ARRAY` on its own is not a valid CAST target type.
        // .expect_err("ARRAY alone is not a type")
        Assert.Throws<ParserException>(() => Mysql()
            .RunParserMethod("CAST(foo AS ARRAY)", p => p.ParseExpr()));
    }

    [Fact]
    public void parse_multi_valued_index()
    {
        // `CAST(... AS <type> ARRAY)` key part in CREATE TABLE, CREATE INDEX, and
        // ALTER TABLE. See https://dev.mysql.com/doc/refman/8.0/en/create-index.html
        MysqlAndGeneric().VerifiedStmt(
            "CREATE TABLE customers (id BIGINT, custinfo JSON, INDEX zips ((CAST(custinfo -> '$.zipcode' AS UNSIGNED ARRAY))))");
        MysqlAndGeneric().VerifiedStmt(
            "CREATE INDEX zips ON customers((CAST(custinfo -> '$.zipcode' AS UNSIGNED ARRAY)))");
        MysqlAndGeneric().VerifiedStmt(
            "ALTER TABLE customers ADD INDEX zips ((CAST(custinfo -> '$.zipcode' AS UNSIGNED ARRAY)))");
    }

    [Fact]
    public void parse_match_against_with_alias()
    {
        var sql = "SELECT tbl.ProjectID FROM surveys.tbl1 AS tbl WHERE MATCH (tbl.ReferenceID) AGAINST ('AAA' IN BOOLEAN MODE)";
        var query = Assert.IsType<Statement.Query>(Mysql().VerifiedStmt(sql)).Value;
        var select = Assert.IsType<SetExpr.Select>(query.Body).Value;
        // Some(Expr::MatchAgainst { columns, match_value, opt_search_modifier })
        var matchAgainst = Assert.IsType<Expr.MatchAgainst>(select.Selection);
        Assert.Equal(
            matchAgainst.Columns,
            Sequence.Of(ObjectName.From(Sequence.Of(
                new Ident("tbl"),
                new Ident("ReferenceID")))));
        Assert.Equal(
            matchAgainst.MatchValue,
            new Value.SingleQuotedString("AAA").WithEmptySpan());
        Assert.Equal(matchAgainst.OptSearchModifier, new SearchModifier.InBooleanMode());
    }

    [Fact]
    public void test_variable_assignment_using_colon_equal()
    {
        var sqlSelect = "SELECT @price := price, @tax := price * 0.1 FROM products WHERE id = 1";
        var stmt = Mysql().VerifiedStmt(sqlSelect);
        switch (stmt)
        {
            case Statement.Query { Value: var query }:
            {
                var select = query.Body.AsSelect();
                Assert.NotNull(select); // .unwrap()

                Assert.Equal(
                    select.Projection,
                    Sequence.Of<SelectItem>(
                        new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("@price", null, Span.Empty())),
                            Op: new BinaryOperator.Assignment(),
                            Right: new Expr.Identifier(new Ident("price", null, Span.Empty())))),
                        new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("@tax", null, Span.Empty())),
                            Op: new BinaryOperator.Assignment(),
                            Right: new Expr.BinaryOp(
                                Left: new Expr.Identifier(new Ident("price", null, Span.Empty())),
                                Op: new BinaryOperator.Multiply(),
                                Right: new Expr.Value(
                                    TestUtils.Number("0.1").WithEmptySpan()))))));

                Assert.Equal(
                    select.Selection,
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("id", null, Span.Empty())),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(TestUtils.Number("1").WithEmptySpan())));
                break;
            }
            default:
                Assert.Fail($"Unexpected statement {stmt}");
                break;
        }

        var sqlUpdate =
            "UPDATE products SET price = @new_price := price * 1.1 WHERE category = 'Books'";
        stmt = Mysql().VerifiedStmt(sqlUpdate);

        switch (stmt)
        {
            case Statement.Update { Value.Assignments: var assignments }:
                Assert.Equal(
                    assignments,
                    Sequence.Of(new Assignment(
                        Target: new AssignmentTarget.ColumnName(new ObjectName(Sequence.Of<ObjectNamePart>(
                            new ObjectNamePart.Identifier(new Ident("price", null, Span.Empty()))))),
                        Value: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("@new_price", null, Span.Empty())),
                            Op: new BinaryOperator.Assignment(),
                            Right: new Expr.BinaryOp(
                                Left: new Expr.Identifier(new Ident("price", null, Span.Empty())),
                                Op: new BinaryOperator.Multiply(),
                                Right: new Expr.Value(
                                    TestUtils.Number("1.1").WithEmptySpan()))))));
                break;
            default:
                Assert.Fail($"Unexpected statement {stmt}");
                break;
        }
    }

    [Fact]
    public void parse_straight_join()
    {
        Mysql().VerifiedStmt(
            "SELECT a.*, b.* FROM table_a AS a STRAIGHT_JOIN table_b AS b ON a.b_id = b.id");
        // Without table alias
        Mysql()
            .VerifiedStmt("SELECT a.*, b.* FROM table_a STRAIGHT_JOIN table_b AS b ON a.b_id = b.id");
    }

    [Fact]
    public void parse_distinctrow_to_distinct()
    {
        Mysql().OneStatementParsesTo(
            "SELECT DISTINCTROW * FROM employees",
            "SELECT DISTINCT * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY DISTINCTROW * FROM employees",
            "SELECT DISTINCT HIGH_PRIORITY * FROM employees");
    }

    [Fact]
    public void parse_select_straight_join()
    {
        var select = Mysql().VerifiedOnlySelect(
            "SELECT STRAIGHT_JOIN * FROM employees e JOIN dept_emp d ON e.emp_no = d.emp_no WHERE d.emp_no = 10001");
        Assert.True(select.SelectModifiers!.StraightJoin);

        Mysql().VerifiedStmt(
            "SELECT STRAIGHT_JOIN e.emp_no, d.dept_no FROM employees e JOIN dept_emp d ON e.emp_no = d.emp_no");
        Mysql().VerifiedStmt("SELECT DISTINCT STRAIGHT_JOIN emp_no FROM employees");

        select = Mysql().VerifiedOnlySelect("SELECT * FROM employees");
        Assert.Null(select.SelectModifiers);
    }

    [Fact]
    public void parse_select_modifiers()
    {
        var select = Mysql().VerifiedOnlySelect("SELECT HIGH_PRIORITY * FROM employees");
        Assert.True(select.SelectModifiers!.HighPriority);
        Assert.False(select.SelectModifiers!.StraightJoin);

        select = Mysql().VerifiedOnlySelect("SELECT SQL_SMALL_RESULT * FROM employees");
        Assert.True(select.SelectModifiers!.SqlSmallResult);

        select = Mysql().VerifiedOnlySelect("SELECT SQL_BIG_RESULT * FROM employees");
        Assert.True(select.SelectModifiers!.SqlBigResult);

        select = Mysql().VerifiedOnlySelect("SELECT SQL_BUFFER_RESULT * FROM employees");
        Assert.True(select.SelectModifiers!.SqlBufferResult);

        select = Mysql().VerifiedOnlySelect("SELECT SQL_NO_CACHE * FROM employees");
        Assert.True(select.SelectModifiers!.SqlNoCache);

        select = Mysql().VerifiedOnlySelect("SELECT SQL_CALC_FOUND_ROWS * FROM employees");
        Assert.True(select.SelectModifiers!.SqlCalcFoundRows);

        select = Mysql().VerifiedOnlySelect(
            "SELECT HIGH_PRIORITY STRAIGHT_JOIN SQL_SMALL_RESULT SQL_BIG_RESULT SQL_BUFFER_RESULT SQL_NO_CACHE SQL_CALC_FOUND_ROWS * FROM employees");
        Assert.True(select.SelectModifiers!.HighPriority);
        Assert.True(select.SelectModifiers!.StraightJoin);
        Assert.True(select.SelectModifiers!.SqlSmallResult);
        Assert.True(select.SelectModifiers!.SqlBigResult);
        Assert.True(select.SelectModifiers!.SqlBufferResult);
        Assert.True(select.SelectModifiers!.SqlNoCache);
        Assert.True(select.SelectModifiers!.SqlCalcFoundRows);

        Mysql().VerifiedStmt("SELECT DISTINCT HIGH_PRIORITY emp_no FROM employees");
        Mysql().VerifiedStmt("SELECT DISTINCT SQL_CALC_FOUND_ROWS emp_no FROM employees");
        Mysql().VerifiedStmt("SELECT HIGH_PRIORITY STRAIGHT_JOIN e.emp_no, d.dept_no FROM employees e JOIN dept_emp d ON e.emp_no = d.emp_no");
    }

    [Fact]
    public void parse_select_modifiers_any_order()
    {
        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY DISTINCT * FROM employees",
            "SELECT DISTINCT HIGH_PRIORITY * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT SQL_CALC_FOUND_ROWS DISTINCT HIGH_PRIORITY * FROM employees",
            "SELECT DISTINCT HIGH_PRIORITY SQL_CALC_FOUND_ROWS * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY DISTINCT SQL_SMALL_RESULT * FROM employees",
            "SELECT DISTINCT HIGH_PRIORITY SQL_SMALL_RESULT * FROM employees");

        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY DISTINCTROW * FROM employees",
            "SELECT DISTINCT HIGH_PRIORITY * FROM employees");

        Mysql().VerifiedStmt("SELECT ALL * FROM employees");
        Mysql().VerifiedStmt("SELECT ALL HIGH_PRIORITY * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY ALL * FROM employees",
            "SELECT ALL HIGH_PRIORITY * FROM employees");

        var select = Mysql().VerifiedOnlySelectWithCanonical(
            "SELECT HIGH_PRIORITY DISTINCT * FROM employees",
            "SELECT DISTINCT HIGH_PRIORITY * FROM employees");
        Assert.True(select.SelectModifiers!.HighPriority);
        Assert.True(select.Distinct is Distinct.DistinctValue);

        select = Mysql().VerifiedOnlySelectWithCanonical(
            "SELECT SQL_CALC_FOUND_ROWS ALL HIGH_PRIORITY * FROM employees",
            "SELECT ALL HIGH_PRIORITY SQL_CALC_FOUND_ROWS * FROM employees");
        Assert.True(select.SelectModifiers!.HighPriority);
        Assert.True(select.SelectModifiers!.SqlCalcFoundRows);
        Assert.Equal(select.Distinct, new Distinct.All());
    }

    [Fact]
    public void parse_select_modifiers_can_be_repeated()
    {
        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY HIGH_PRIORITY * FROM employees",
            "SELECT HIGH_PRIORITY * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT SQL_CALC_FOUND_ROWS SQL_CALC_FOUND_ROWS * FROM employees",
            "SELECT SQL_CALC_FOUND_ROWS * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT STRAIGHT_JOIN STRAIGHT_JOIN * FROM employees",
            "SELECT STRAIGHT_JOIN * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT SQL_NO_CACHE SQL_NO_CACHE * FROM employees",
            "SELECT SQL_NO_CACHE * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY DISTINCT HIGH_PRIORITY * FROM employees",
            "SELECT DISTINCT HIGH_PRIORITY * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT SQL_CALC_FOUND_ROWS DISTINCT SQL_CALC_FOUND_ROWS * FROM employees",
            "SELECT DISTINCT SQL_CALC_FOUND_ROWS * FROM employees");
    }

    [Fact]
    public void parse_select_modifiers_canonical_ordering()
    {
        Mysql().OneStatementParsesTo(
            "SELECT SQL_CALC_FOUND_ROWS SQL_NO_CACHE SQL_BUFFER_RESULT SQL_BIG_RESULT SQL_SMALL_RESULT STRAIGHT_JOIN HIGH_PRIORITY * FROM employees",
            "SELECT HIGH_PRIORITY STRAIGHT_JOIN SQL_SMALL_RESULT SQL_BIG_RESULT SQL_BUFFER_RESULT SQL_NO_CACHE SQL_CALC_FOUND_ROWS * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT SQL_NO_CACHE DISTINCT SQL_CALC_FOUND_ROWS * FROM employees",
            "SELECT DISTINCT SQL_NO_CACHE SQL_CALC_FOUND_ROWS * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY STRAIGHT_JOIN DISTINCT SQL_SMALL_RESULT * FROM employees",
            "SELECT DISTINCT HIGH_PRIORITY STRAIGHT_JOIN SQL_SMALL_RESULT * FROM employees");
        Mysql().OneStatementParsesTo(
            "SELECT HIGH_PRIORITY ALL STRAIGHT_JOIN * FROM employees",
            "SELECT ALL HIGH_PRIORITY STRAIGHT_JOIN * FROM employees");
    }

    [Fact]
    public void parse_select_modifiers_errors()
    {
        Assert.Throws<ParserException>(() => Mysql()
            .ParseSqlStatements("SELECT DISTINCT DISTINCT * FROM t"));
        Assert.Throws<ParserException>(() => Mysql()
            .ParseSqlStatements("SELECT DISTINCTROW DISTINCTROW * FROM t"));
        Assert.Throws<ParserException>(() => Mysql()
            .ParseSqlStatements("SELECT DISTINCT DISTINCTROW * FROM t"));
        Assert.Throws<ParserException>(() => Mysql()
            .ParseSqlStatements("SELECT ALL DISTINCT * FROM t"));
        Assert.Throws<ParserException>(() => Mysql()
            .ParseSqlStatements("SELECT DISTINCT ALL * FROM t"));
        Assert.Throws<ParserException>(() => Mysql()
            .ParseSqlStatements("SELECT ALL DISTINCTROW * FROM t"));
        Assert.Throws<ParserException>(() => Mysql()
            .ParseSqlStatements("SELECT ALL ALL * FROM t"));
    }

    [Fact]
    public void mysql_foreign_key_with_index_name()
    {
        Mysql().VerifiedStmt(
            "CREATE TABLE orders (customer_id INT, INDEX idx_customer (customer_id), CONSTRAINT fk_customer FOREIGN KEY idx_customer (customer_id) REFERENCES customers(id))");
    }

    [Fact]
    public void parse_drop_index()
    {
        var sql = "DROP INDEX idx_name ON table_name";
        // Statement::Drop { object_type, if_exists, names, cascade, restrict, purge, temporary, table }
        var drop = Assert.IsType<Statement.Drop>(Mysql().VerifiedStmt(sql));
        Assert.False(drop.IfExists);
        Assert.Equal(new ObjectType.Index(), drop.ObjectType);
        Assert.Equal(
            new[] { "idx_name" },
            drop.Names.Select(n => n.ToString()).ToList());
        Assert.False(drop.Cascade);
        Assert.False(drop.Restrict);
        Assert.False(drop.Purge);
        Assert.False(drop.Temporary);
        Assert.NotNull(drop.Table);
        Assert.Equal("table_name", drop.Table!.ToString());
    }

    [Fact]
    public void parse_alter_table_drop_index()
    {
        Assert.True(
            TestUtils.AlterTableOp(
                MysqlAndGeneric().VerifiedStmt("ALTER TABLE tab DROP INDEX idx_index"))
                is AlterTableOperation.DropIndex { Name.Value: "idx_index" });
    }

    [Fact]
    public void parse_json_member_of()
    {
        Mysql().VerifiedStmt("""SELECT 17 MEMBER OF('[23, "abc", 17, "ab", 10]')""");
        var sql = """SELECT 'ab' MEMBER OF('[23, "abc", 17, "ab", 10]')""";
        var stmt = Mysql().VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.Query { Value: var query }:
            {
                var select = query.Body.AsSelect();
                Assert.NotNull(select); // .unwrap()
                Assert.Equal(
                    select.Projection,
                    Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.MemberOf(new MemberOf(
                        Value: new Expr.Value(
                            ValueWithSpan.From(new Value.SingleQuotedString("ab"))),
                        Array: new Expr.Value(
                            ValueWithSpan.From(new Value.SingleQuotedString("""[23, "abc", 17, "ab", 10]"""))))))));
                break;
            }
            default:
                Assert.Fail($"Unexpected statement {stmt}");
                break;
        }
    }

    [Fact]
    public void parse_show_charset()
    {
        var res = Mysql().VerifiedStmt("SHOW CHARACTER SET");
        Assert.Equal(
            res,
            new Statement.ShowCharset(new ShowCharset(
                IsShorthand: false,
                Filter: null)));
        Mysql().VerifiedStmt("SHOW CHARACTER SET LIKE 'utf8mb4%'");
        Mysql().VerifiedStmt("SHOW CHARSET WHERE charset = 'utf8mb4%'");
        Mysql().VerifiedStmt("SHOW CHARSET LIKE 'utf8mb4%'");
    }

    [Fact]
    public void test_ddl_with_index_using()
    {
        var columns = "(name, age DESC)";
        var @using = "USING BTREE";

        foreach (var sql in new[]
        {
            $"CREATE INDEX idx_name ON test {@using} {columns}",
            $"CREATE TABLE foo (name VARCHAR(255), age INT, KEY idx_name {@using} {columns})",
            $"ALTER TABLE foo ADD KEY idx_name {@using} {columns}",
            $"CREATE INDEX idx_name ON test{columns} {@using}",
            $"CREATE TABLE foo (name VARCHAR(255), age INT, KEY idx_name {columns} {@using})",
            $"ALTER TABLE foo ADD KEY idx_name {columns} {@using}",
        })
        {
            MysqlAndGeneric().VerifiedStmt(sql);
        }
    }

    [Fact]
    public void test_create_index_options()
    {
        MysqlAndGeneric()
            .VerifiedStmt("CREATE INDEX idx_name ON t(c1, c2) USING HASH LOCK = SHARED");
        MysqlAndGeneric()
            .VerifiedStmt("CREATE INDEX idx_name ON t(c1, c2) USING BTREE ALGORITHM = INPLACE");
        MysqlAndGeneric().VerifiedStmt(
            "CREATE INDEX idx_name ON t(c1, c2) USING BTREE LOCK = EXCLUSIVE ALGORITHM = DEFAULT");
    }

    [Fact]
    public void test_optimizer_hints()
    {
        var mysqlDialect = MysqlAndGeneric();

        // ~ selects
        mysqlDialect.VerifiedStmt(
            "SELECT /*+ SET_VAR(optimizer_switch = 'mrr_cost_based=off') "
            + "SET_VAR(max_heap_table_size = 1G) */ 1");

        mysqlDialect.VerifiedStmt(
            "SELECT /*+ SET_VAR(target_partitions=1) */ * FROM "
            + "(SELECT /*+ SET_VAR(target_partitions=8) */ * FROM t1 LIMIT 1) AS dt");

        // ~ inserts / replace
        mysqlDialect.VerifiedStmt(
            "INSERT /*+ RESOURCE_GROUP(Batch) */ "
            + "INTO t2 VALUES (2)");

        mysqlDialect.VerifiedStmt(
            "REPLACE /*+ foobar */ INTO test "
            + "VALUES (1, 'Old', '2014-08-20 18:47:00')");

        // ~ updates
        mysqlDialect.VerifiedStmt(
            "UPDATE /*+ quux */ table_name "
            + "SET column1 = 1 "
            + "WHERE 1 = 1");

        // ~ deletes
        mysqlDialect.VerifiedStmt(
            "DELETE /*+ foobar */ FROM table_name");

        // prefixed hints: any alphanumeric prefix before `+` is captured
        var select = mysqlDialect.VerifiedOnlySelect("SELECT /*abc+ text */ 1");
        Assert.Equal(1, select.OptimizerHints.Count);
        Assert.Equal("abc", select.OptimizerHints[0].Prefix);
        Assert.Equal(" text ", select.OptimizerHints[0].Text);

        // multiple hints with different prefixes
        select = mysqlDialect.VerifiedOnlySelect("SELECT /*+ A */ /*x2+ B */ 1");
        Assert.Equal(2, select.OptimizerHints.Count);
        Assert.Equal("", select.OptimizerHints[0].Prefix);
        Assert.Equal(" A ", select.OptimizerHints[0].Text);
        Assert.Equal("x2", select.OptimizerHints[1].Prefix);
        Assert.Equal(" B ", select.OptimizerHints[1].Text);

        // hints mixed with regular comments: regular comments are skipped
        select = mysqlDialect.VerifiedOnlySelectWithCanonical(
            "SELECT /*+ A */ /* Regular comment */ /*x2+ B */ 1",
            "SELECT /*+ A */ /*x2+ B */ 1");
        Assert.Equal(2, select.OptimizerHints.Count);
        Assert.Equal("", select.OptimizerHints[0].Prefix);
        Assert.Equal(" A ", select.OptimizerHints[0].Text);
        Assert.Equal("x2", select.OptimizerHints[1].Prefix);
        Assert.Equal(" B ", select.OptimizerHints[1].Text);

        // prefixed hints in INSERT/UPDATE/DELETE
        mysqlDialect.VerifiedStmt("INSERT /*abc+ append */ INTO t2 VALUES (2)");
        mysqlDialect.VerifiedStmt("UPDATE /*abc+ PARALLEL */ table_name SET column1 = 1");
        mysqlDialect.VerifiedStmt("DELETE /*abc+ ENABLE_DML */ FROM table_name");
    }

    [Fact]
    public void parse_create_database_with_charset()
    {
        // Test DEFAULT CHARACTER SET with = sign
        MysqlAndGeneric().VerifiedStmt("CREATE DATABASE mydb DEFAULT CHARACTER SET utf8mb4");

        // Test DEFAULT CHARACTER SET without = sign (normalized form)
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE DATABASE mydb DEFAULT CHARACTER SET = utf8mb4",
            "CREATE DATABASE mydb DEFAULT CHARACTER SET utf8mb4");

        // Test CHARACTER SET without DEFAULT
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE DATABASE mydb CHARACTER SET utf8mb4",
            "CREATE DATABASE mydb DEFAULT CHARACTER SET utf8mb4");

        // Test CHARSET shorthand
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE DATABASE mydb CHARSET utf8mb4",
            "CREATE DATABASE mydb DEFAULT CHARACTER SET utf8mb4");

        // Test DEFAULT CHARSET shorthand
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE DATABASE mydb DEFAULT CHARSET utf8mb4",
            "CREATE DATABASE mydb DEFAULT CHARACTER SET utf8mb4");

        // Test DEFAULT COLLATE
        MysqlAndGeneric().VerifiedStmt("CREATE DATABASE mydb DEFAULT COLLATE utf8mb4_unicode_ci");

        // Test COLLATE without DEFAULT
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE DATABASE mydb COLLATE utf8mb4_unicode_ci",
            "CREATE DATABASE mydb DEFAULT COLLATE utf8mb4_unicode_ci");

        // Test both CHARACTER SET and COLLATE together
        MysqlAndGeneric().VerifiedStmt(
            "CREATE DATABASE mydb DEFAULT CHARACTER SET utf8mb4 DEFAULT COLLATE utf8mb4_unicode_ci");

        // Test IF NOT EXISTS with CHARACTER SET
        MysqlAndGeneric()
            .VerifiedStmt("CREATE DATABASE IF NOT EXISTS mydb DEFAULT CHARACTER SET utf16");

        // Test the exact syntax from the issue
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE DATABASE IF NOT EXISTS noria DEFAULT CHARACTER SET = utf16",
            "CREATE DATABASE IF NOT EXISTS noria DEFAULT CHARACTER SET utf16");
    }

    [Fact]
    public void parse_create_database_with_charset_errors()
    {
        // Missing charset name after CHARACTER SET
        Assert.Throws<ParserException>(() => MysqlAndGeneric()
            .ParseSqlStatements("CREATE DATABASE mydb DEFAULT CHARACTER SET"));

        // Missing charset name after CHARSET
        Assert.Throws<ParserException>(() => MysqlAndGeneric()
            .ParseSqlStatements("CREATE DATABASE mydb CHARSET"));

        // Missing collation name after COLLATE
        Assert.Throws<ParserException>(() => MysqlAndGeneric()
            .ParseSqlStatements("CREATE DATABASE mydb DEFAULT COLLATE"));

        // Equals sign but no value
        Assert.Throws<ParserException>(() => MysqlAndGeneric()
            .ParseSqlStatements("CREATE DATABASE mydb CHARACTER SET ="));
    }

    [Fact]
    public void parse_create_database_with_charset_option_ordering()
    {
        // MySQL allows COLLATE before CHARACTER SET - output is normalized to CHARACTER SET first
        // (matches MySQL's own SHOW CREATE DATABASE output order)
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE DATABASE mydb DEFAULT COLLATE utf8mb4_unicode_ci DEFAULT CHARACTER SET utf8mb4",
            "CREATE DATABASE mydb DEFAULT CHARACTER SET utf8mb4 DEFAULT COLLATE utf8mb4_unicode_ci");

        // COLLATE first without DEFAULT keywords
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE DATABASE mydb COLLATE utf8mb4_unicode_ci CHARACTER SET utf8mb4",
            "CREATE DATABASE mydb DEFAULT CHARACTER SET utf8mb4 DEFAULT COLLATE utf8mb4_unicode_ci");
    }

    [Fact]
    public void parse_adjacent_string_literal_concatenation()
    {
        var sql = """SELECT 'M' "y" 'S' "q" 'l'""";
        Mysql().OneStatementParsesTo(sql, "SELECT 'MySql'");
    }

    [Fact]
    public void parse_group_by_with_rollup()
    {
        Mysql().VerifiedStmt("SELECT * FROM tbl GROUP BY col1, col2 WITH ROLLUP");
    }

    [Fact]
    public void parse_table_partition_selection()
    {
        MysqlAndGeneric().VerifiedStmt("SELECT * FROM employees PARTITION (p0, p2)");
        MysqlAndGeneric().VerifiedStmt("SELECT * FROM employees PARTITION (p0) AS e");
        MysqlAndGeneric().VerifiedStmt(
            "SELECT * FROM employees PARTITION (p0) JOIN departments PARTITION (p1) ON employees.dept_id = departments.id");
        MysqlAndGeneric().VerifiedStmt("UPDATE employees PARTITION (p0) SET salary = 1");
        MysqlAndGeneric().VerifiedStmt("DELETE FROM employees PARTITION (p0) WHERE id = 1");

        // .expect_err("expected an error")
        var err = Assert.Throws<ParserException>(() => MysqlAndGeneric()
            .ParseSqlStatements("SELECT * FROM employees PARTITION")).Error;
        Assert.IsType<ParserError.ParserErrorValue>(err);
    }

    [Fact]
    public void parse_is_distinct_from_json_arrow_precedence()
    {
        // MySQL's `->` binds tighter than `IS [NOT] DISTINCT FROM`, so the JSON
        // extraction must stay inside the right operand.
        Assert.Equal(
            new Expr.IsDistinctFrom(
                new Expr.Identifier(new Ident("a")),
                new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("b")),
                    Op: new BinaryOperator.Arrow(),
                    Right: new Expr.Value(
                        new Value.SingleQuotedString("k").WithEmptySpan()))),
            MysqlAndGeneric().VerifiedExpr("a IS DISTINCT FROM b -> 'k'"));

        Assert.Equal(
            new Expr.IsNotDistinctFrom(
                new Expr.Identifier(new Ident("a")),
                new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("b")),
                    Op: new BinaryOperator.LongArrow(),
                    Right: new Expr.Value(
                        new Value.SingleQuotedString("k").WithEmptySpan()))),
            MysqlAndGeneric().VerifiedExpr("a IS NOT DISTINCT FROM b ->> 'k'"));
    }

    [Fact]
    public void parse_bitstring_literal_escaping()
    {
        MysqlAndGeneric().VerifiedStmt("SELECT B''''");
        MysqlAndGeneric().VerifiedStmt("SELECT B'it''s'");
    }

    [Fact]
    public void parse_alter_table_column_position()
    {
        // MySQL makes the COLUMN keyword optional for ADD, CHANGE and MODIFY.
        switch (TestUtils.AlterTableOp(MysqlAndGeneric().VerifiedStmt("ALTER TABLE tab ADD c INT AFTER b")))
        {
            case AlterTableOperation.AddColumn { ColumnKeyword: var columnKeyword, ColumnPosition: var columnPosition }:
                Assert.False(columnKeyword);
                Assert.Equal<MySQLColumnPosition?>(
                    columnPosition,
                    new MySQLColumnPosition.After(new Ident("b")));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
        MysqlAndGeneric().VerifiedStmt("ALTER TABLE tab ADD c INT FIRST");
        MysqlAndGeneric().OneStatementParsesTo(
            "ALTER TABLE tab CHANGE a b INT FIRST",
            "ALTER TABLE tab CHANGE COLUMN a b INT FIRST");
        MysqlAndGeneric().OneStatementParsesTo(
            "ALTER TABLE tab MODIFY c INT AFTER b",
            "ALTER TABLE tab MODIFY COLUMN c INT AFTER b");

        // The position follows the full column definition, options included.
        MysqlAndGeneric().VerifiedStmt(
            "ALTER TABLE tab ADD COLUMN c VARCHAR(255) NOT NULL DEFAULT 'x' COMMENT 'c' AFTER b");
        MysqlAndGeneric().VerifiedStmt(
            "ALTER TABLE tab MODIFY COLUMN c INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY FIRST");

        // The AFTER target is an identifier, so it may be quoted.
        switch (TestUtils.AlterTableOp(
            MysqlAndGeneric().VerifiedStmt("ALTER TABLE tab ADD COLUMN c INT AFTER `order`")))
        {
            case AlterTableOperation.AddColumn { ColumnPosition: var columnPosition }:
                Assert.Equal<MySQLColumnPosition?>(
                    columnPosition,
                    new MySQLColumnPosition.After(Ident.WithQuote('`', "order")));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        MysqlAndGeneric().VerifiedStmt("ALTER TABLE tab ADD COLUMN a INT FIRST, CHANGE COLUMN b c INT AFTER a, MODIFY COLUMN d INT AFTER c");

        // FIRST and AFTER are mutually exclusive, and AFTER needs a target.
        foreach (var sql in new[]
        {
            "ALTER TABLE tab ADD COLUMN c INT FIRST AFTER b",
            "ALTER TABLE tab ADD COLUMN c INT AFTER b FIRST",
            "ALTER TABLE tab ADD COLUMN c INT AFTER",
            "ALTER TABLE tab CHANGE COLUMN a b INT AFTER",
            "ALTER TABLE tab MODIFY COLUMN c INT AFTER",
        })
        {
            // assert!(mysql_and_generic().parse_sql_statements(sql).is_err(), "{sql}")
            Assert.Throws<ParserException>(() => MysqlAndGeneric().ParseSqlStatements(sql));
        }
    }
}
