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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_clickhouse.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

// Notas da tradução (onda-c/C15.md):
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal/constante: o literal
//   vai na posição "expected" (regra xUnit2000, como em AstModTests e SpansTests).
// - `assert_eq!(x.len(), n)` → `Assert.Equal(n, x.Count)`; o xUnit2013 fica suprimido neste arquivo.
// - `assert_eq!(x, None)` → `Assert.Null(x)` (xUnit2003); `assert_eq!(x, true)` → `Assert.True(x)` (xUnit2004);
//   `assert!(v.contains(&x))` → `Assert.Contains(x, v)` (xUnit2017).
// - `unreachable!()` / `panic!(msg)` → `Assert.Fail(msg)` (como em SpansTests); `.unwrap()` → `!`;
//   `.expect(msg)` → `?? throw new InvalidOperationException(msg)`.
// - `.expect_err(msg)` → `Assert.Throws<ParserException>(..)`.
// - `fn` aninhada dentro de um teste → função local `static` com o nome mecânico (onda-b/B1.md, item 9).
// - Strings Rust com `\` no fim da linha (continuação que remove a quebra e a indentação seguinte) →
//   concatenação dos pedaços sem a indentação (onda-b/B1.md, item 13). `concat!(..)` → `+`.
// - `r#"..."#` com aspas → string verbatim `@"..."` com aspas dobradas.

/// <summary>Test SQL syntax specific to ClickHouse.</summary>
#pragma warning disable xUnit2013
public class SqlParserClickHouseTests
{
    private const string Unreachable = "internal error: entered unreachable code";

    [Fact]
    public void parse_map_access_expr()
    {
        var sql = "SELECT string_values[indexOf(string_names, 'endpoint')] FROM foos WHERE id = 'test' AND string_value[indexOf(string_name, 'app')] <> 'foo'";
        var select = Clickhouse().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Select(
                SelectToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Distinct: null,
                SelectModifiers: null,
                Top: null,
                TopBeforeDistinct: false,
                Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.CompoundFieldAccess(
                    Root: new Expr.Identifier(new Ident(
                        Value: "string_values",
                        QuoteStyle: null,
                        Span: Span.Empty())),
                    AccessChain: Sequence.Of<AccessExpr>(new AccessExpr.Subscript(new Subscript.Index(
                        IndexValue: TestUtils.Call(
                            "indexOf",
                            new[]
                            {
                                new Expr.Identifier(new Ident("string_names")),
                                Expr.MakeValue(new Value.SingleQuotedString("endpoint")),
                            }))))))),
                Exclude: null,
                Into: null,
                From: Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("foos")))),
                    Joins: Sequence<Join>.Empty)),
                LateralViews: Sequence<LateralView>.Empty,
                Prewhere: null,
                Selection: new Expr.BinaryOp(
                    Left: new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("id")),
                        Op: new BinaryOperator.Eq(),
                        Right: Expr.MakeValue(new Value.SingleQuotedString("test"))),
                    Op: new BinaryOperator.And(),
                    Right: new Expr.BinaryOp(
                        Left: new Expr.CompoundFieldAccess(
                            Root: new Expr.Identifier(new Ident("string_value")),
                            AccessChain: Sequence.Of<AccessExpr>(new AccessExpr.Subscript(new Subscript.Index(
                                IndexValue: TestUtils.Call(
                                    "indexOf",
                                    new[]
                                    {
                                        new Expr.Identifier(new Ident("string_name")),
                                        Expr.MakeValue(new Value.SingleQuotedString("app")),
                                    }))))),
                        Op: new BinaryOperator.NotEq(),
                        Right: Expr.MakeValue(new Value.SingleQuotedString("foo")))),
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
                Flavor: SelectFlavor.Standard),
            select);
    }

    [Fact]
    public void parse_array_expr()
    {
        var sql = "SELECT ['1', '2'] FROM test";
        var select = Clickhouse().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Array(new Ast.Array(
                Elem: Sequence.Of(
                    Expr.MakeValue(new Value.SingleQuotedString("1")),
                    Expr.MakeValue(new Value.SingleQuotedString("2"))),
                Named: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_array_fn()
    {
        var sql = "SELECT array(x1, x2) FROM foo";
        var select = Clickhouse().VerifiedOnlySelect(sql);
        Assert.Equal(
            TestUtils.Call(
                "array",
                new Expr[]
                {
                    new Expr.Identifier(new Ident("x1")),
                    new Expr.Identifier(new Ident("x2")),
                }),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_kill()
    {
        var stmt = Clickhouse().VerifiedStmt("KILL MUTATION 5");
        Assert.Equal(
            stmt,
            new Statement.Kill(
                Modifier: new KillType.Mutation(),
                Id: 5));
    }

    [Fact]
    public void parse_delimited_identifiers()
    {
        // check that quoted identifiers in any position remain quoted after serialization
        var select = Clickhouse().VerifiedOnlySelect(
            @"SELECT ""alias"".""bar baz"", ""myfun""(), ""simple id"" AS ""column alias"" FROM ""a table"" AS ""alias""");
        // check FROM
        switch (TestUtils.Only(select.From).Relation)
        {
            case TableFactor.Table table:
                Assert.Equal(
                    ObjectName.From(Sequence.Of(Ident.WithQuote('"', "a table"))),
                    table.Name);
                Assert.Equal(Ident.WithQuote('"', "alias"), table.Alias!.Name);
                Assert.True(table.Args is null);
                Assert.True(table.WithHints.IsEmpty);
                Assert.True(table.Version is null);
                break;
            default:
                Assert.Fail("Expecting TableFactor::Table");
                break;
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
                Assert.Fail("Expected ExprWithAlias");
                break;
        }

        Clickhouse().VerifiedStmt(@"CREATE TABLE ""foo"" (""bar"" ""int"")");
        Clickhouse().VerifiedStmt(@"ALTER TABLE foo ADD CONSTRAINT ""bar"" PRIMARY KEY (baz)");
        //TODO verified_stmt(r#"UPDATE foo SET "bar" = 5"#);
    }

    [Fact]
    public void parse_create_table()
    {
        Clickhouse().VerifiedStmt(@"CREATE TABLE ""x"" (""a"" ""int"") ENGINE = MergeTree ORDER BY (""x"")");
        Clickhouse().VerifiedStmt(@"CREATE TABLE ""x"" (""a"" ""int"") ENGINE = MergeTree ORDER BY ""x""");
        Clickhouse().VerifiedStmt(
            @"CREATE TABLE ""x"" (""a"" ""int"") ENGINE = MergeTree ORDER BY ""x"" AS SELECT * FROM ""t"" WHERE true");
        Clickhouse().OneStatementParsesTo(
            "CREATE TABLE x (a int) ENGINE = MergeTree() ORDER BY a",
            "CREATE TABLE x (a INT) ENGINE = MergeTree ORDER BY a");
    }

    [Fact]
    public void parse_create_table_partition_by_after_order_by()
    {
        // ClickHouse DDL places PARTITION BY after ORDER BY.
        // MergeTree() is canonicalized to MergeTree and type names are uppercased.
        Clickhouse().OneStatementParsesTo(
            "CREATE TABLE IF NOT EXISTS \"MyTable\" (`col1` Int64, `col2` Int32) "
                + "ENGINE = MergeTree() "
                + "PRIMARY KEY (toDate(toDateTime(`col2`)), `col1`, `col2`) "
                + "ORDER BY (toDate(toDateTime(`col2`)), `col1`, `col2`) "
                + "PARTITION BY col1 % 64",
            "CREATE TABLE IF NOT EXISTS \"MyTable\" (`col1` INT64, `col2` Int32) "
                + "ENGINE = MergeTree "
                + "PRIMARY KEY (toDate(toDateTime(`col2`)), `col1`, `col2`) "
                + "ORDER BY (toDate(toDateTime(`col2`)), `col1`, `col2`) "
                + "PARTITION BY col1 % 64");

        // PARTITION BY after ORDER BY works with both ClickHouseDialect and GenericDialect
        ClickhouseAndGeneric()
            .VerifiedStmt("CREATE TABLE t (a INT) ENGINE = MergeTree ORDER BY a PARTITION BY a");

        // Arithmetic expression in PARTITION BY (roundtrip)
        ClickhouseAndGeneric()
            .VerifiedStmt("CREATE TABLE t (a INT) ENGINE = MergeTree ORDER BY a PARTITION BY a % 64");

        // AST: partition_by is populated with the correct expression
        switch (ClickhouseAndGeneric()
            .VerifiedStmt("CREATE TABLE t (a INT) ENGINE = MergeTree ORDER BY a PARTITION BY a % 64"))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(
                    createTable.PartitionBy,
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("a")),
                        Op: new BinaryOperator.Modulo(),
                        Right: new Expr.Value(
                            new Value.Number("64", false).WithEmptySpan())));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }

        // Function call expression in PARTITION BY (ClickHouse-specific function)
        Clickhouse().VerifiedStmt(
            "CREATE TABLE t (d DATE) ENGINE = MergeTree ORDER BY d PARTITION BY toYYYYMM(d)");

        // Negative: PARTITION BY with no expression should fail
        // .expect_err("PARTITION BY with no expression should fail")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements("CREATE TABLE t (a INT) ENGINE = MergeTree ORDER BY a PARTITION BY"));
    }

    [Fact]
    public void parse_insert_into_function()
    {
        Clickhouse().VerifiedStmt("INSERT INTO TABLE FUNCTION remote('localhost', default.simple_table) VALUES (100, 'inserted via remote()')");
        Clickhouse().VerifiedStmt("INSERT INTO FUNCTION remote('localhost', default.simple_table) VALUES (100, 'inserted via remote()')");
    }

    [Fact]
    public void parse_alter_table_attach_and_detach_partition()
    {
        foreach (var operation in new[] { "ATTACH", "DETACH" })
        {
            switch (ClickhouseAndGeneric()
                .VerifiedStmt($"ALTER TABLE t0 {operation} PARTITION part"))
            {
                case Statement.AlterTable { Value: var alterTable }:
                    Assert.Equal("t0", alterTable.Name.ToString());
                    Assert.Equal(
                        alterTable.Operations[0],
                        operation == "ATTACH"
                            ? new AlterTableOperation.AttachPartition(
                                Partition: new Partition.Expr(new Expr.Identifier(new Ident("part"))))
                            : (AlterTableOperation)new AlterTableOperation.DetachPartition(
                                Partition: new Partition.Expr(new Expr.Identifier(new Ident("part")))));
                    break;
                default:
                    Assert.Fail(Unreachable);
                    break;
            }

            switch (ClickhouseAndGeneric()
                .VerifiedStmt($"ALTER TABLE t1 {operation} PART part"))
            {
                case Statement.AlterTable { Value: var alterTable }:
                    Assert.Equal("t1", alterTable.Name.ToString());
                    Assert.Equal(
                        alterTable.Operations[0],
                        operation == "ATTACH"
                            ? new AlterTableOperation.AttachPartition(
                                Partition: new Partition.Part(new Expr.Identifier(new Ident("part"))))
                            : (AlterTableOperation)new AlterTableOperation.DetachPartition(
                                Partition: new Partition.Part(new Expr.Identifier(new Ident("part")))));
                    break;
                default:
                    Assert.Fail(Unreachable);
                    break;
            }

            // negative cases
            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements($"ALTER TABLE t0 {operation} PARTITION")).Error,
                new ParserError.ParserErrorValue("Expected: an expression, found: EOF"));
            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements($"ALTER TABLE t0 {operation} PART")).Error,
                new ParserError.ParserErrorValue("Expected: an expression, found: EOF"));
        }
    }

    [Fact]
    public void parse_alter_table_add_projection()
    {
        switch (ClickhouseAndGeneric().VerifiedStmt(
            "ALTER TABLE t0 ADD PROJECTION IF NOT EXISTS my_name"
                + " (SELECT a, b GROUP BY a ORDER BY b)"))
        {
            case Statement.AlterTable { Value: var alterTable }:
                Assert.Equal(alterTable.Name, ObjectName.From(Sequence.Of(Ident.From("t0"))));
                Assert.Equal(1, alterTable.Operations.Count);
                Assert.Equal(
                    alterTable.Operations[0],
                    new AlterTableOperation.AddProjection(
                        IfNotExists: true,
                        Name: Ident.From("my_name"),
                        Select: new ProjectionSelect(
                            Projection: Sequence.Of<SelectItem>(
                                new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("a"))),
                                new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("b")))),
                            GroupBy: new GroupByExpr.Expressions(
                                Sequence.Of<Expr>(new Expr.Identifier(new Ident("a"))),
                                Sequence<GroupByWithModifier>.Empty),
                            OrderBy: new OrderBy(
                                Kind: new OrderByKind.Expressions(Sequence.Of(new OrderByExpr(
                                    Expr: new Expr.Identifier(new Ident("b")),
                                    Options: new OrderByOptions(
                                        Sort: null,
                                        NullsFirst: null),
                                    WithFill: null))),
                                Interpolate: null))));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }

        // leave out IF NOT EXISTS is allowed
        ClickhouseAndGeneric()
            .VerifiedStmt("ALTER TABLE t0 ADD PROJECTION my_name (SELECT a, b GROUP BY a ORDER BY b)");
        // leave out GROUP BY is allowed
        ClickhouseAndGeneric()
            .VerifiedStmt("ALTER TABLE t0 ADD PROJECTION my_name (SELECT a, b ORDER BY b)");
        // leave out ORDER BY is allowed
        ClickhouseAndGeneric()
            .VerifiedStmt("ALTER TABLE t0 ADD PROJECTION my_name (SELECT a, b GROUP BY a)");

        // missing select query is not allowed
        Assert.Equal(
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements("ALTER TABLE t0 ADD PROJECTION my_name")).Error,
            new ParserError.ParserErrorValue("Expected: (, found: EOF"));
        Assert.Equal(
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements("ALTER TABLE t0 ADD PROJECTION my_name ()")).Error,
            new ParserError.ParserErrorValue("Expected: SELECT, found: )"));
        Assert.Equal(
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements("ALTER TABLE t0 ADD PROJECTION my_name (SELECT)")).Error,
            new ParserError.ParserErrorValue("Expected: an expression, found: )"));
    }

    [Fact]
    public void parse_alter_table_drop_projection()
    {
        switch (ClickhouseAndGeneric().VerifiedStmt("ALTER TABLE t0 DROP PROJECTION IF EXISTS my_name"))
        {
            case Statement.AlterTable { Value: var alterTable }:
                Assert.Equal(alterTable.Name, ObjectName.From(Sequence.Of(Ident.From("t0"))));
                Assert.Equal(1, alterTable.Operations.Count);
                Assert.Equal(
                    alterTable.Operations[0],
                    new AlterTableOperation.DropProjection(
                        IfExists: true,
                        Name: Ident.From("my_name")));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
        // allow to skip `IF EXISTS`
        ClickhouseAndGeneric().VerifiedStmt("ALTER TABLE t0 DROP PROJECTION my_name");

        Assert.Equal(
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements("ALTER TABLE t0 DROP PROJECTION")).Error,
            new ParserError.ParserErrorValue("Expected: identifier, found: EOF"));
    }

    [Fact]
    public void parse_alter_table_clear_and_materialize_projection()
    {
        foreach (var keyword in new[] { "CLEAR", "MATERIALIZE" })
        {
            switch (ClickhouseAndGeneric().VerifiedStmt(
                $"ALTER TABLE t0 {keyword} PROJECTION IF EXISTS my_name IN PARTITION p0"))
            {
                case Statement.AlterTable { Value: var alterTable }:
                    Assert.Equal(alterTable.Name, ObjectName.From(Sequence.Of(Ident.From("t0"))));
                    Assert.Equal(1, alterTable.Operations.Count);
                    Assert.Equal(
                        alterTable.Operations[0],
                        keyword == "CLEAR"
                            ? new AlterTableOperation.ClearProjection(
                                IfExists: true,
                                Name: Ident.From("my_name"),
                                Partition: new Ident("p0"))
                            : (AlterTableOperation)new AlterTableOperation.MaterializeProjection(
                                IfExists: true,
                                Name: Ident.From("my_name"),
                                Partition: new Ident("p0")));
                    break;
                default:
                    Assert.Fail(Unreachable);
                    break;
            }
            // allow to skip `IF EXISTS`
            ClickhouseAndGeneric().VerifiedStmt(
                $"ALTER TABLE t0 {keyword} PROJECTION my_name IN PARTITION p0");
            // allow to skip `IN PARTITION partition_name`
            ClickhouseAndGeneric()
                .VerifiedStmt($"ALTER TABLE t0 {keyword} PROJECTION my_name");

            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements($"ALTER TABLE t0 {keyword} PROJECTION")).Error,
                new ParserError.ParserErrorValue("Expected: identifier, found: EOF"));

            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements(
                        $"ALTER TABLE t0 {keyword} PROJECTION my_name IN PARTITION")).Error,
                new ParserError.ParserErrorValue("Expected: identifier, found: EOF"));

            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements(
                        $"ALTER TABLE t0 {keyword} PROJECTION my_name IN")).Error,
                new ParserError.ParserErrorValue("Expected: end of statement, found: IN"));
        }
    }

    [Fact]
    public void parse_optimize_table()
    {
        ClickhouseAndGeneric().VerifiedStmt("OPTIMIZE TABLE t0");
        ClickhouseAndGeneric().VerifiedStmt("OPTIMIZE TABLE db.t0");
        ClickhouseAndGeneric().VerifiedStmt("OPTIMIZE TABLE t0 ON CLUSTER 'cluster'");
        ClickhouseAndGeneric().VerifiedStmt("OPTIMIZE TABLE t0 ON CLUSTER 'cluster' FINAL");
        ClickhouseAndGeneric().VerifiedStmt("OPTIMIZE TABLE t0 FINAL DEDUPLICATE");
        ClickhouseAndGeneric().VerifiedStmt("OPTIMIZE TABLE t0 DEDUPLICATE");
        ClickhouseAndGeneric().VerifiedStmt("OPTIMIZE TABLE t0 DEDUPLICATE BY id");
        ClickhouseAndGeneric().VerifiedStmt("OPTIMIZE TABLE t0 FINAL DEDUPLICATE BY id");
        ClickhouseAndGeneric()
            .VerifiedStmt("OPTIMIZE TABLE t0 PARTITION tuple('2023-04-22') DEDUPLICATE BY id");
        switch (ClickhouseAndGeneric().VerifiedStmt(
            "OPTIMIZE TABLE t0 ON CLUSTER cluster PARTITION ID '2024-07' FINAL DEDUPLICATE BY id"))
        {
            case Statement.OptimizeTable optimizeTable:
                Assert.Equal("t0", optimizeTable.Name.ToString());
                Assert.Equal(optimizeTable.OnCluster, new Ident("cluster"));
                Assert.Equal(
                    optimizeTable.Partition,
                    new Partition.Identifier(Ident.WithQuote('\'', "2024-07")));
                Assert.True(optimizeTable.IncludeFinal);
                Assert.Equal(
                    optimizeTable.Deduplicate,
                    new Deduplicate.ByExpression(new Expr.Identifier(new Ident("id"))));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }

        // negative cases
        Assert.Equal(
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements("OPTIMIZE TABLE t0 DEDUPLICATE BY")).Error,
            new ParserError.ParserErrorValue("Expected: an expression, found: EOF"));
        Assert.Equal(
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements("OPTIMIZE TABLE t0 PARTITION")).Error,
            new ParserError.ParserErrorValue("Expected: an expression, found: EOF"));
        Assert.Equal(
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements("OPTIMIZE TABLE t0 PARTITION ID")).Error,
            new ParserError.ParserErrorValue("Expected: identifier, found: EOF"));
    }

    private static ColumnDef ColumnDef(Ident name, DataType dataType) =>
        new ColumnDef(
            Name: name,
            DataType: dataType,
            Options: Sequence<ColumnOptionDef>.Empty);

    [Fact]
    public void parse_clickhouse_data_types()
    {
        var sql = "CREATE TABLE table ("
            + "a1 UInt8, a2 UInt16, a3 UInt32, a4 UInt64, a5 UInt128, a6 UInt256,"
            + " b1 Int8, b2 Int16, b3 Int32, b4 Int64, b5 Int128, b6 Int256,"
            + " c1 Float32, c2 Float64,"
            + " d1 Date32, d2 DateTime64(3), d3 DateTime64(3, 'UTC'),"
            + " e1 FixedString(255),"
            + " f1 LowCardinality(Int32)"
            + ") ORDER BY (a1)";
        // ClickHouse has a case-sensitive definition of data type, but canonical representation is not
        var canonicalSql = sql
            .Replace(" Int8", " INT8")
            .Replace(" Int64", " INT64")
            .Replace(" Float64", " FLOAT64");

        switch (ClickhouseAndGeneric().OneStatementParsesTo(sql, canonicalSql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(createTable.Name, ObjectName.From(Sequence.Of(Ident.From("table"))));
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(
                        ColumnDef(Ident.From("a1"), new DataType.UInt8()),
                        ColumnDef(Ident.From("a2"), new DataType.UInt16()),
                        ColumnDef(Ident.From("a3"), new DataType.UInt32()),
                        ColumnDef(Ident.From("a4"), new DataType.UInt64()),
                        ColumnDef(Ident.From("a5"), new DataType.UInt128()),
                        ColumnDef(Ident.From("a6"), new DataType.UInt256()),
                        ColumnDef(Ident.From("b1"), new DataType.Int8(null)),
                        ColumnDef(Ident.From("b2"), new DataType.Int16()),
                        ColumnDef(Ident.From("b3"), new DataType.Int32()),
                        ColumnDef(Ident.From("b4"), new DataType.Int64()),
                        ColumnDef(Ident.From("b5"), new DataType.Int128()),
                        ColumnDef(Ident.From("b6"), new DataType.Int256()),
                        ColumnDef(Ident.From("c1"), new DataType.Float32()),
                        ColumnDef(Ident.From("c2"), new DataType.Float64()),
                        ColumnDef(Ident.From("d1"), new DataType.Date32()),
                        ColumnDef(Ident.From("d2"), new DataType.Datetime64(3, null)),
                        ColumnDef(Ident.From("d3"), new DataType.Datetime64(3, "UTC")),
                        ColumnDef(Ident.From("e1"), new DataType.FixedString(255)),
                        ColumnDef(
                            Ident.From("f1"),
                            new DataType.LowCardinality(new DataType.Int32()))));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_create_table_with_nullable()
    {
        var sql = "CREATE TABLE table (k UInt8, `a` Nullable(String), `b` Nullable(DateTime64(9, 'UTC')), c Nullable(DateTime64(9)), d Date32 NULL) ENGINE = MergeTree ORDER BY (`k`)";
        // ClickHouse has a case-sensitive definition of data type, but canonical representation is not
        var canonicalSql = sql.Replace("String", "STRING");

        switch (ClickhouseAndGeneric().OneStatementParsesTo(sql, canonicalSql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(createTable.Name, ObjectName.From(Sequence.Of(Ident.From("table"))));
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(
                        ColumnDef(Ident.From("k"), new DataType.UInt8()),
                        ColumnDef(
                            Ident.WithQuote('`', "a"),
                            new DataType.Nullable(new DataType.String(null))),
                        ColumnDef(
                            Ident.WithQuote('`', "b"),
                            new DataType.Nullable(new DataType.Datetime64(
                                9,
                                "UTC"))),
                        ColumnDef(
                            Ident.From("c"),
                            new DataType.Nullable(new DataType.Datetime64(9, null))),
                        new ColumnDef(
                            Name: Ident.From("d"),
                            DataType: new DataType.Date32(),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Null())))));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_create_table_with_nested_data_types()
    {
        var sql = "CREATE TABLE table ("
            + " i Nested(a Array(Int16), b LowCardinality(String)),"
            + " k Array(Tuple(FixedString(128), Int128)),"
            + " l Tuple(a DateTime64(9), b Array(UUID)),"
            + " m Map(String, UInt16)"
            + ") ENGINE=MergeTree ORDER BY (k)";

        switch (Clickhouse().OneStatementParsesTo(sql, ""))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(createTable.Name, ObjectName.From(Sequence.Of(Ident.From("table"))));
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("i"),
                            DataType: new DataType.Nested(Sequence.Of(
                                ColumnDef(
                                    Ident.From("a"),
                                    new DataType.Array(new ArrayElemTypeDef.Parenthesis(
                                        new DataType.Int16()))),
                                ColumnDef(
                                    Ident.From("b"),
                                    new DataType.LowCardinality(new DataType.String(null))))),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident("k"),
                            DataType: new DataType.Array(new ArrayElemTypeDef.Parenthesis(
                                new DataType.Tuple(Sequence.Of(
                                    new StructField(
                                        FieldName: null,
                                        FieldType: new DataType.FixedString(128),
                                        Options: null),
                                    new StructField(
                                        FieldName: null,
                                        FieldType: new DataType.Int128(),
                                        Options: null))))),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident("l"),
                            DataType: new DataType.Tuple(Sequence.Of(
                                new StructField(
                                    FieldName: Ident.From("a"),
                                    FieldType: new DataType.Datetime64(9, null),
                                    Options: null),
                                new StructField(
                                    FieldName: Ident.From("b"),
                                    FieldType: new DataType.Array(new ArrayElemTypeDef.Parenthesis(
                                        new DataType.Uuid())),
                                    Options: null))),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident("m"),
                            DataType: new DataType.Map(
                                new DataType.String(null),
                                new DataType.UInt16(),
                                MapBracketKind.Parentheses),
                            Options: Sequence<ColumnOptionDef>.Empty)));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void reject_angle_bracket_array_type()
    {
        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => Clickhouse()
                .ParseSqlStatements("CREATE TABLE t (a ARRAY<INT>)")).Error,
            new ParserError.ParserErrorValue("Expected: (, found: <"));
    }

    [Fact]
    public void parse_create_table_with_primary_key()
    {
        static bool AssertFunction(Function actual, string name, string arg)
        {
            Assert.Equal(actual.Name, ObjectName.From(Sequence.Of(new Ident(name))));
            Assert.Equal(
                actual.Args,
                new FunctionArguments.List(new FunctionArgumentList(
                    Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Identifier(
                        new Ident(arg))))),
                    DuplicateTreatment: null,
                    Clauses: Sequence<FunctionArgumentClause>.Empty)));
            return true;
        }

        switch (ClickhouseAndGeneric().VerifiedStmt(
            "CREATE TABLE db.table (`i` INT, `k` INT)"
                + " ENGINE = SharedMergeTree('/clickhouse/tables/{uuid}/{shard}', '{replica}')"
                + " PRIMARY KEY tuple(i)"
                + " ORDER BY tuple(i)"))
        {
            case Statement.CreateTable { Value: var createTable }:
            {
                Assert.Equal("db.table", createTable.Name.ToString());
                Assert.Equal(
                    Sequence.Of(
                        new ColumnDef(
                            Name: Ident.WithQuote('`', "i"),
                            DataType: new DataType.Int(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: Ident.WithQuote('`', "k"),
                            DataType: new DataType.Int(null),
                            Options: Sequence<ColumnOptionDef>.Empty)),
                    createTable.Columns);

                if (createTable.TableOptions is not CreateTableOptions.Plain { Value: var plainOptions })
                {
                    Assert.Fail(Unreachable);
                    return;
                }

                Assert.Contains(
                    new SqlOption.NamedParenthesizedList(
                        new NamedParenthesizedList(
                            Key: new Ident("ENGINE"),
                            Name: new Ident("SharedMergeTree"),
                            Values: Sequence.Of(
                                Ident.WithQuote('\'', "/clickhouse/tables/{uuid}/{shard}"),
                                Ident.WithQuote('\'', "{replica}")))),
                    plainOptions);

                switch (createTable.PrimaryKey!)
                {
                    case Expr.Function { Value: var primaryKey }:
                        Assert.True(AssertFunction(primaryKey, "tuple", "i"));
                        break;
                    default:
                        Assert.Fail("unexpected primary key type");
                        break;
                }
                switch (createTable.OrderBy)
                {
                    case OneOrManyWithParens<Expr>.One { Value: Expr.Function { Value: var orderBy } }:
                        Assert.True(AssertFunction(orderBy, "tuple", "i"));
                        break;
                    default:
                        Assert.Fail("unexpected order by type");
                        break;
                }
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }

        // .expect_err("ORDER BY supports one expression with tuple")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements(
                "CREATE TABLE db.table (`i` Int, `k` Int)"
                    + " ORDER BY tuple(i), tuple(k)"));
    }

    [Fact]
    public void parse_create_table_with_variant_default_expressions()
    {
        var sql = "CREATE TABLE table ("
            + "a DATETIME MATERIALIZED now(),"
            + " b DATETIME EPHEMERAL now(),"
            + " c DATETIME EPHEMERAL,"
            + " d STRING ALIAS toString(c)"
            + ") ENGINE = MergeTree";
        switch (ClickhouseAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("a"),
                            DataType: new DataType.Datetime(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Materialized(new Expr.Function(new Function(
                                    Name: ObjectName.From(Sequence.Of(new Ident("now"))),
                                    UsesOdbcSyntax: false,
                                    Args: new FunctionArguments.List(new FunctionArgumentList(
                                        Args: Sequence<FunctionArg>.Empty,
                                        DuplicateTreatment: null,
                                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                                    Parameters: new FunctionArguments.None(),
                                    NullTreatment: null,
                                    Filter: null,
                                    Over: null,
                                    WithinGroup: Sequence<OrderByExpr>.Empty)))))),
                        new ColumnDef(
                            Name: new Ident("b"),
                            DataType: new DataType.Datetime(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Ephemeral(new Expr.Function(new Function(
                                    Name: ObjectName.From(Sequence.Of(new Ident("now"))),
                                    UsesOdbcSyntax: false,
                                    Args: new FunctionArguments.List(new FunctionArgumentList(
                                        Args: Sequence<FunctionArg>.Empty,
                                        DuplicateTreatment: null,
                                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                                    Parameters: new FunctionArguments.None(),
                                    NullTreatment: null,
                                    Filter: null,
                                    Over: null,
                                    WithinGroup: Sequence<OrderByExpr>.Empty)))))),
                        new ColumnDef(
                            Name: new Ident("c"),
                            DataType: new DataType.Datetime(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Ephemeral(null)))),
                        new ColumnDef(
                            Name: new Ident("d"),
                            DataType: new DataType.String(null),
                            Options: Sequence.Of(new ColumnOptionDef(
                                Name: null,
                                Option: new ColumnOption.Alias(new Expr.Function(new Function(
                                    Name: ObjectName.From(Sequence.Of(new Ident("toString"))),
                                    UsesOdbcSyntax: false,
                                    Args: new FunctionArguments.List(new FunctionArgumentList(
                                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                                            new Expr.Identifier(new Ident("c"))))),
                                        DuplicateTreatment: null,
                                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                                    Parameters: new FunctionArguments.None(),
                                    NullTreatment: null,
                                    Filter: null,
                                    Over: null,
                                    WithinGroup: Sequence<OrderByExpr>.Empty))))))));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_create_view_with_fields_data_types()
    {
        switch (Clickhouse().VerifiedStmt(@"CREATE VIEW v (i ""int"", f ""String"") AS SELECT * FROM t"))
        {
            case Statement.CreateView { Value: var createView }:
                Assert.Equal(createView.Name, ObjectName.From(Sequence.Of(Ident.From("v"))));
                Assert.Equal(
                    createView.Columns,
                    Sequence.Of(
                        new ViewColumnDef(
                            Name: Ident.From("i"),
                            DataType: new DataType.Custom(
                                ObjectName.From(Sequence.Of(new Ident(
                                    Value: "int",
                                    QuoteStyle: '"',
                                    Span: Span.Empty()))),
                                Sequence<string>.Empty),
                            Options: null),
                        new ViewColumnDef(
                            Name: Ident.From("f"),
                            DataType: new DataType.Custom(
                                ObjectName.From(Sequence.Of(new Ident(
                                    Value: "String",
                                    QuoteStyle: '"',
                                    Span: Span.Empty()))),
                                Sequence<string>.Empty),
                            Options: null)));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }

        // .expect_err("CREATE VIEW with fields and without data types should be invalid")
        Assert.Throws<ParserException>(() => Clickhouse()
            .ParseSqlStatements("CREATE VIEW v (i, f) AS SELECT * FROM t"));
    }

    [Fact]
    public void parse_double_equal()
    {
        Clickhouse().OneStatementParsesTo(
            "SELECT foo FROM bar WHERE buz == 'buz'",
            "SELECT foo FROM bar WHERE buz = 'buz'");
    }

    [Fact]
    public void parse_limit_by()
    {
        ClickhouseAndGeneric().VerifiedStmt(
            "SELECT * FROM default.last_asset_runs_mv ORDER BY created_at DESC LIMIT 1 BY asset");
        ClickhouseAndGeneric().VerifiedStmt(
            "SELECT * FROM default.last_asset_runs_mv ORDER BY created_at DESC LIMIT 1 BY asset, toStartOfDay(created_at)");
        // .expect_err("BY without LIMIT")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric().ParseSqlStatements(
            "SELECT * FROM default.last_asset_runs_mv ORDER BY created_at DESC BY asset, toStartOfDay(created_at)"));
        // .expect_err("BY with OFFSET but without LIMIT")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements("SELECT * FROM T OFFSET 5 BY foo"));
    }

    [Fact]
    public void parse_settings_in_query()
    {
        static void CheckSettings(string sql, Sequence<Setting> expected)
        {
            switch (ClickhouseAndGeneric().VerifiedStmt(sql))
            {
                case Statement.Query { Value: var q }:
                    Assert.Equal(q.Settings, (Sequence<Setting>?)expected);
                    break;
                default:
                    Assert.Fail(Unreachable);
                    break;
            }
        }

        foreach (var (sql, expectedSettings) in new (string, Sequence<Setting>)[]
        {
            (
                "SELECT * FROM t SETTINGS max_threads = 1, max_block_size = 10000",
                Sequence.Of(
                    new Setting(
                        Key: new Ident("max_threads"),
                        Value: Expr.MakeValue(TestUtils.Number("1"))),
                    new Setting(
                        Key: new Ident("max_block_size"),
                        Value: Expr.MakeValue(TestUtils.Number("10000"))))
            ),
            (
                "SELECT * FROM t SETTINGS additional_table_filters = {'table_1': 'x != 2'}",
                Sequence.Of(new Setting(
                    Key: new Ident("additional_table_filters"),
                    Value: new Expr.Dictionary(Sequence.Of(new DictionaryField(
                        Key: Ident.WithQuote('\'', "table_1"),
                        Value: Expr.MakeValue(TestUtils.SingleQuotedString("x != 2")))))))
            ),
            (
                "SELECT * FROM t SETTINGS additional_result_filter = 'x != 2', query_plan_optimize_lazy_materialization = false",
                Sequence.Of(
                    new Setting(
                        Key: new Ident("additional_result_filter"),
                        Value: Expr.MakeValue(TestUtils.SingleQuotedString("x != 2"))),
                    new Setting(
                        Key: new Ident("query_plan_optimize_lazy_materialization"),
                        Value: Expr.MakeValue(new Value.Boolean(false))))
            ),
        })
        {
            CheckSettings(sql, expectedSettings);
        }

        var invalidCases = new List<(string, string)>
        {
            ("SELECT * FROM t SETTINGS a", "Expected: =, found: EOF"),
            (
                "SELECT * FROM t SETTINGS a=",
                "Expected: an expression, found: EOF"
            ),
            ("SELECT * FROM t SETTINGS a=1, b", "Expected: =, found: EOF"),
            (
                "SELECT * FROM t SETTINGS a=1, b=",
                "Expected: an expression, found: EOF"
            ),
            (
                "SELECT * FROM t SETTINGS a = {",
                "Expected: identifier, found: EOF"
            ),
            (
                "SELECT * FROM t SETTINGS a = {'b'",
                "Expected: :, found: EOF"
            ),
            (
                "SELECT * FROM t SETTINGS a = {'b': ",
                "Expected: an expression, found: EOF"
            ),
            (
                "SELECT * FROM t SETTINGS a = {'b': 'c',}",
                "Expected: identifier, found: }"
            ),
            (
                "SELECT * FROM t SETTINGS a = {'b': 'c', 'd'}",
                "Expected: :, found: }"
            ),
            (
                "SELECT * FROM t SETTINGS a = {'b': 'c', 'd': }",
                "Expected: an expression, found: }"
            ),
            (
                "SELECT * FROM t SETTINGS a = {ANY(b)}",
                "Expected: :, found: ("
            ),
        };
        foreach (var (sql, errorMsg) in invalidCases)
        {
            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements(sql)).Error,
                new ParserError.ParserErrorValue(errorMsg));
        }
    }

    [Fact]
    public void parse_select_star_except()
    {
        Clickhouse().VerifiedStmt("SELECT * EXCEPT (prev_status) FROM anomalies");
    }

    [Fact]
    public void parse_select_parametric_function()
    {
        switch (ClickhouseAndGeneric().VerifiedStmt("SELECT HISTOGRAM(0.5, 0.6)(x, y) FROM t"))
        {
            case Statement.Query { Value: var query }:
            {
                IReadOnlyList<SelectItem> projection = query.Body.AsSelect()!.Projection;
                Assert.Equal(1, projection.Count);
                switch (projection[0])
                {
                    case SelectItem.UnnamedExpr { Value: Expr.Function { Value: var f } }:
                    {
                        if (f.Args is not FunctionArguments.List { Value: var args })
                        {
                            Assert.Fail(Unreachable);
                            return;
                        }
                        Assert.Equal(2, args.Args.Count);
                        Assert.Equal(
                            args.Args[0],
                            new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Identifier(Ident.From("x")))));
                        Assert.Equal(
                            args.Args[1],
                            new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Identifier(Ident.From("y")))));

                        if (f.Parameters is not FunctionArguments.List { Value: var parameters })
                        {
                            Assert.Fail(Unreachable);
                            return;
                        }
                        Assert.Equal(2, parameters.Args.Count);
                        Assert.Equal(
                            parameters.Args[0],
                            new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                                new Value.Number("0.5", false).WithEmptySpan()))));
                        Assert.Equal(
                            parameters.Args[1],
                            new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                                new Value.Number("0.6", false).WithEmptySpan()))));
                        break;
                    }
                    default:
                        Assert.Fail(Unreachable);
                        break;
                }
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_select_star_except_no_parens()
    {
        Clickhouse().OneStatementParsesTo(
            "SELECT * EXCEPT prev_status FROM anomalies",
            "SELECT * EXCEPT (prev_status) FROM anomalies");
    }

    [Fact]
    public void parse_create_materialized_view()
    {
        // example sql
        // https://clickhouse.com/docs/en/guides/developer/cascading-materialized-views
        var sql = "CREATE MATERIALIZED VIEW analytics.monthly_aggregated_data_mv "
            + "TO analytics.monthly_aggregated_data "
            + "AS SELECT toDate(toStartOfMonth(event_time)) "
            + "AS month, domain_name, sumState(count_views) "
            + "AS sumCountViews FROM analytics.hourly_data "
            + "GROUP BY domain_name, month";
        ClickhouseAndGeneric().VerifiedStmt(sql);
    }

    [Fact]
    public void parse_select_order_by_with_fill_interpolate()
    {
        var sql = "SELECT id, fname, lname FROM customer WHERE id < 5 "
            + "ORDER BY "
            + "fname ASC NULLS FIRST WITH FILL FROM 10 TO 20 STEP 2, "
            + "lname DESC NULLS LAST WITH FILL FROM 30 TO 40 STEP 3 "
            + "INTERPOLATE (col1 AS col1 + 1) "
            + "LIMIT 2";
        var select = Clickhouse().VerifiedQuery(sql);
        Assert.Equal(
            new OrderBy(
                Kind: new OrderByKind.Expressions(Sequence.Of(
                    new OrderByExpr(
                        Expr: new Expr.Identifier(new Ident("fname")),
                        Options: new OrderByOptions(
                            Sort: new OrderBySort.Asc(),
                            NullsFirst: true),
                        WithFill: new WithFill(
                            From: Expr.MakeValue(TestUtils.Number("10")),
                            To: Expr.MakeValue(TestUtils.Number("20")),
                            Step: Expr.MakeValue(TestUtils.Number("2")))),
                    new OrderByExpr(
                        Expr: new Expr.Identifier(new Ident("lname")),
                        Options: new OrderByOptions(
                            Sort: new OrderBySort.Desc(),
                            NullsFirst: false),
                        WithFill: new WithFill(
                            From: Expr.MakeValue(TestUtils.Number("30")),
                            To: Expr.MakeValue(TestUtils.Number("40")),
                            Step: Expr.MakeValue(TestUtils.Number("3")))))),
                Interpolate: new Interpolate(
                    Exprs: Sequence.Of(new InterpolateExpr(
                        Column: new Ident("col1"),
                        Expr: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("col1")),
                            Op: new BinaryOperator.Plus(),
                            Right: Expr.MakeValue(TestUtils.Number("1"))))))),
            select.OrderBy ?? throw new InvalidOperationException("ORDER BY expected"));
        Assert.Equal(
            select.LimitClause,
            new LimitClause.LimitOffset(
                Limit: Expr.MakeValue(TestUtils.Number("2")),
                Offset: null,
                LimitBy: Sequence<Expr>.Empty));
    }

    [Fact]
    public void parse_select_order_by_with_fill_interpolate_multi_interpolates()
    {
        var sql = "SELECT id, fname, lname FROM customer ORDER BY fname WITH FILL "
            + "INTERPOLATE (col1 AS col1 + 1) INTERPOLATE (col2 AS col2 + 2)";
        // .expect_err("ORDER BY only accepts a single INTERPOLATE clause")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements(sql));
    }

    [Fact]
    public void parse_select_order_by_with_fill_interpolate_multi_with_fill_interpolates()
    {
        var sql = "SELECT id, fname, lname FROM customer "
            + "ORDER BY "
            + "fname WITH FILL INTERPOLATE (col1 AS col1 + 1), "
            + "lname WITH FILL INTERPOLATE (col2 AS col2 + 2)";
        // .expect_err("ORDER BY only accepts a single INTERPOLATE clause")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements(sql));
    }

    [Fact]
    public void parse_select_order_by_interpolate_not_last()
    {
        // A linha de `fname` não termina com `\` no Rust: a quebra e a indentação de `lname` ficam na string.
        var sql = "SELECT id, fname, lname FROM customer "
            + "ORDER BY "
            + "fname INTERPOLATE (col2 AS col2 + 2),\n"
            + "            lname";
        // .expect_err("ORDER BY INTERPOLATE must be in the last position")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements(sql));
    }

    [Fact]
    public void parse_with_fill()
    {
        var sql = "SELECT fname FROM customer ORDER BY fname "
            + "WITH FILL FROM 10 TO 20 STEP 2";
        var select = Clickhouse().VerifiedQuery(sql);
        Assert.Equal(
            new WithFill(
                From: Expr.MakeValue(TestUtils.Number("10")),
                To: Expr.MakeValue(TestUtils.Number("20")),
                Step: Expr.MakeValue(TestUtils.Number("2"))),
            (select.OrderBy ?? throw new InvalidOperationException("ORDER BY expected")).Kind switch
            {
                OrderByKind.Expressions { Value: var exprs } => exprs[0].WithFill,
                _ => null,
            });
    }

    [Fact]
    public void parse_with_fill_missing_single_argument()
    {
        var sql = "SELECT id, fname, lname FROM customer ORDER BY "
            + "fname WITH FILL FROM TO 20";
        // .expect_err("WITH FILL requires expressions for all arguments")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements(sql));
    }

    [Fact]
    public void parse_with_fill_multiple_incomplete_arguments()
    {
        var sql = "SELECT id, fname, lname FROM customer ORDER BY "
            + "fname WITH FILL FROM TO 20, lname WITH FILL FROM TO STEP 1";
        // .expect_err("WITH FILL requires expressions for all arguments")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements(sql));
    }

    [Fact]
    public void parse_interpolate_body_with_columns()
    {
        var sql = "SELECT fname FROM customer ORDER BY fname WITH FILL "
            + "INTERPOLATE (col1 AS col1 + 1, col2 AS col3, col4 AS col4 + 4)";
        var select = Clickhouse().VerifiedQuery(sql);
        Assert.Equal(
            new Interpolate(
                Exprs: Sequence.Of(
                    new InterpolateExpr(
                        Column: new Ident("col1"),
                        Expr: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("col1")),
                            Op: new BinaryOperator.Plus(),
                            Right: Expr.MakeValue(TestUtils.Number("1")))),
                    new InterpolateExpr(
                        Column: new Ident("col2"),
                        Expr: new Expr.Identifier(new Ident("col3"))),
                    new InterpolateExpr(
                        Column: new Ident("col4"),
                        Expr: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("col4")),
                            Op: new BinaryOperator.Plus(),
                            Right: Expr.MakeValue(TestUtils.Number("4")))))),
            (select
                .OrderBy ?? throw new InvalidOperationException("ORDER BY expected"))
                .Interpolate);
    }

    [Fact]
    public void parse_interpolate_without_body()
    {
        var sql = "SELECT fname FROM customer ORDER BY fname WITH FILL INTERPOLATE";
        var select = Clickhouse().VerifiedQuery(sql);
        Assert.Equal(
            new Interpolate(Exprs: null),
            (select
                .OrderBy ?? throw new InvalidOperationException("ORDER BY expected"))
                .Interpolate);
    }

    [Fact]
    public void parse_interpolate_with_empty_body()
    {
        var sql = "SELECT fname FROM customer ORDER BY fname WITH FILL INTERPOLATE ()";
        var select = Clickhouse().VerifiedQuery(sql);
        Assert.Equal(
            new Interpolate(
                Exprs: Sequence<InterpolateExpr>.Empty),
            (select
                .OrderBy ?? throw new InvalidOperationException("ORDER BY expected"))
                .Interpolate);
    }

    [Fact]
    public void test_prewhere()
    {
        switch (ClickhouseAndGeneric().VerifiedStmt("SELECT * FROM t PREWHERE x = 1 WHERE y = 2"))
        {
            case Statement.Query { Value: var query }:
            {
                var prewhere = query.Body.AsSelect()!.Prewhere;
                Assert.Equal(
                    prewhere,
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("x")),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.Number("1", false).WithEmptySpan())));
                var selection = query.Body.AsSelect()!.Selection;
                Assert.Equal(
                    selection,
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("y")),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.Number("2", false).WithEmptySpan())));
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }

        switch (ClickhouseAndGeneric().VerifiedStmt("SELECT * FROM t PREWHERE x = 1 AND y = 2"))
        {
            case Statement.Query { Value: var query }:
            {
                var prewhere = query.Body.AsSelect()!.Prewhere;
                Assert.Equal(
                    prewhere,
                    new Expr.BinaryOp(
                        Left: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("x")),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.Value(
                                new Value.Number("1", false).WithEmptySpan())),
                        Op: new BinaryOperator.And(),
                        Right: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("y")),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.Value(
                                new Value.Number("2", false).WithEmptySpan()))));
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }
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
        var quoteStyles = new[] { '"', '`' };

        foreach (var objectName in validObjectNames)
        {
            // Test single identifier without quotes
            Assert.Equal(
                Clickhouse().VerifiedStmt($"USE {objectName}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(new Ident(
                    objectName))))));
            foreach (var quote in quoteStyles)
            {
                // Test single identifier with different type of quotes
                Assert.Equal(
                    Clickhouse().VerifiedStmt($"USE {quote}{objectName}{quote}"),
                    new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(Ident.WithQuote(
                        quote,
                        objectName))))));
            }
        }
    }

    [Fact]
    public void test_query_with_format_clause()
    {
        var formatOptions = new List<string> { "TabSeparated", "JSONCompact", "NULL" };
        foreach (var format in formatOptions)
        {
            var sql = $"SELECT * FROM t FORMAT {format}";
            switch (ClickhouseAndGeneric().VerifiedStmt(sql))
            {
                case Statement.Query { Value: var query }:
                    if (format == "NULL")
                    {
                        Assert.Equal(query.FormatClause, new FormatClause.Null());
                    }
                    else
                    {
                        Assert.Equal(
                            query.FormatClause,
                            new FormatClause.Identifier(new Ident(format)));
                    }
                    break;
                default:
                    Assert.Fail(Unreachable);
                    break;
            }
        }

        var invalidCases = new[]
        {
            "SELECT * FROM t FORMAT",
            "SELECT * FROM t FORMAT TabSeparated JSONCompact",
            "SELECT * FROM t FORMAT TabSeparated TabSeparated",
        };
        foreach (var sql in invalidCases)
        {
            // .expect_err("Expected: FORMAT {identifier}, found: ")
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements(sql));
        }
    }

    [Fact]
    public void test_insert_query_with_format_clause()
    {
        var cases = new[]
        {
            @"INSERT INTO tbl FORMAT JSONEachRow {""id"": 1, ""value"": ""foo""}, {""id"": 2, ""value"": ""bar""}",
            @"INSERT INTO tbl FORMAT JSONEachRow [""first"", ""second"", ""third""]",
            @"INSERT INTO tbl FORMAT JSONEachRow [{""first"": 1}]",
            @"INSERT INTO tbl (foo) FORMAT JSONAsObject {""foo"": {""bar"": {""x"": ""y""}, ""baz"": 1}}",
            @"INSERT INTO tbl (foo, bar) FORMAT JSON {""foo"": 1, ""bar"": 2}",
            @"INSERT INTO tbl FORMAT CSV col1, col2, col3",
            @"INSERT INTO tbl FORMAT LineAsString ""I love apple"", ""I love banana"", ""I love orange""",
            @"INSERT INTO tbl (foo) SETTINGS input_format_json_read_bools_as_numbers = true FORMAT JSONEachRow {""id"": 1, ""value"": ""foo""}",
            @"INSERT INTO tbl SETTINGS format_template_resultset = '/some/path/resultset.format', format_template_row = '/some/path/row.format' FORMAT Template",
            @"INSERT INTO tbl SETTINGS input_format_json_read_bools_as_numbers = true FORMAT JSONEachRow {""id"": 1, ""value"": ""foo""}",
        };

        foreach (var sql in cases)
        {
            Clickhouse().VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_create_table_on_commit_and_as_query()
    {
        var sql = "CREATE LOCAL TEMPORARY TABLE test ON COMMIT PRESERVE ROWS AS SELECT 1";
        switch (ClickhouseAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("test", createTable.Name.ToString());
                Assert.Equal(OnCommit.PreserveRows, createTable.OnCommit);
                Assert.Equal(
                    createTable.Query!.Body.AsSelect()!.Projection,
                    Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Value(
                        new Value.Number("1", false).WithEmptySpan()))));
                break;
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_freeze_and_unfreeze_partition()
    {
        // test cases without `WITH NAME`
        foreach (var operationName in new[] { "FREEZE", "UNFREEZE" })
        {
            var sql = $"ALTER TABLE t {operationName} PARTITION '2024-08-14'";

            var expectedPartition = new Partition.Expr(new Expr.Value(
                new Value.SingleQuotedString("2024-08-14").WithEmptySpan()));
            switch (ClickhouseAndGeneric().VerifiedStmt(sql))
            {
                case Statement.AlterTable { Value: var alterTable }:
                {
                    Assert.Equal(1, alterTable.Operations.Count);
                    var expectedOperation = operationName == "FREEZE"
                        ? new AlterTableOperation.FreezePartition(
                            Partition: expectedPartition,
                            WithName: null)
                        : (AlterTableOperation)new AlterTableOperation.UnfreezePartition(
                            Partition: expectedPartition,
                            WithName: null);
                    Assert.Equal(alterTable.Operations[0], expectedOperation);
                    break;
                }
                default:
                    Assert.Fail(Unreachable);
                    break;
            }
        }

        // test case with `WITH NAME`
        foreach (var operationName in new[] { "FREEZE", "UNFREEZE" })
        {
            var sql =
                $"ALTER TABLE t {operationName} PARTITION '2024-08-14' WITH NAME 'hello'";
            switch (ClickhouseAndGeneric().VerifiedStmt(sql))
            {
                case Statement.AlterTable { Value: var alterTable }:
                {
                    Assert.Equal(1, alterTable.Operations.Count);
                    var expectedPartition = new Partition.Expr(new Expr.Value(
                        new Value.SingleQuotedString("2024-08-14").WithEmptySpan()));
                    var expectedOperation = operationName == "FREEZE"
                        ? new AlterTableOperation.FreezePartition(
                            Partition: expectedPartition,
                            WithName: Ident.WithQuote('\'', "hello"))
                        : (AlterTableOperation)new AlterTableOperation.UnfreezePartition(
                            Partition: expectedPartition,
                            WithName: Ident.WithQuote('\'', "hello"));
                    Assert.Equal(alterTable.Operations[0], expectedOperation);
                    break;
                }
                default:
                    Assert.Fail(Unreachable);
                    break;
            }
        }

        // negative cases
        foreach (var operationName in new[] { "FREEZE", "UNFREEZE" })
        {
            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements($"ALTER TABLE t0 {operationName} PARTITION")).Error,
                new ParserError.ParserErrorValue("Expected: an expression, found: EOF"));
            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements(
                        $"ALTER TABLE t0 {operationName} PARTITION p0 WITH")).Error,
                new ParserError.ParserErrorValue("Expected: NAME, found: EOF"));
            Assert.Equal(
                Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                    .ParseSqlStatements(
                        $"ALTER TABLE t0 {operationName} PARTITION p0 WITH NAME")).Error,
                new ParserError.ParserErrorValue("Expected: identifier, found: EOF"));
        }
    }

    [Fact]
    public void parse_select_table_function_settings()
    {
        static void CheckSettings(string sql, TableFunctionArgs expected)
        {
            switch (ClickhouseAndGeneric().VerifiedStmt(sql))
            {
                case Statement.Query { Value: var q }:
                {
                    var from = q.Body.AsSelect()!.From;
                    Assert.Equal(1, from.Count);
                    Assert.Equal(from[0].Joins, Sequence<Join>.Empty);
                    switch (from[0].Relation)
                    {
                        case TableFactor.Table table:
                        {
                            var args = table.Args!;
                            Assert.Equal(args, expected);
                            break;
                        }
                        default:
                            Assert.Fail(Unreachable);
                            break;
                    }
                    break;
                }
                default:
                    Assert.Fail(Unreachable);
                    break;
            }
        }
        CheckSettings(
            "SELECT * FROM table_function(arg, SETTINGS s0 = 3, s1 = 's')",
            new TableFunctionArgs(
                Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                    new Expr.Identifier(Ident.From("arg"))))),

                Settings: Sequence.Of(
                    new Setting(
                        Key: Ident.From("s0"),
                        Value: Expr.MakeValue(TestUtils.Number("3"))),
                    new Setting(
                        Key: Ident.From("s1"),
                        Value: Expr.MakeValue(TestUtils.SingleQuotedString("s"))))));
        CheckSettings(
            "SELECT * FROM table_function(arg)",
            new TableFunctionArgs(
                Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                    new Expr.Identifier(Ident.From("arg"))))),
                Settings: null));
        CheckSettings(
            "SELECT * FROM table_function(SETTINGS s0 = 3, s1 = 's')",
            new TableFunctionArgs(
                Args: Sequence<FunctionArg>.Empty,
                Settings: Sequence.Of(
                    new Setting(
                        Key: Ident.From("s0"),
                        Value: Expr.MakeValue(TestUtils.Number("3"))),
                    new Setting(
                        Key: Ident.From("s1"),
                        Value: Expr.MakeValue(TestUtils.SingleQuotedString("s"))))));
        var invalidCases = new List<string>
        {
            "SELECT * FROM t(SETTINGS a)",
            "SELECT * FROM t(SETTINGS a=)",
            "SELECT * FROM t(SETTINGS a=1, b)",
            "SELECT * FROM t(SETTINGS a=1, b=)",
        };
        foreach (var sql in invalidCases)
        {
            // .expect_err("Expected: SETTINGS key = value, found: ")
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
                .ParseSqlStatements(sql));
        }
    }

    [Fact]
    public void explain_describe()
    {
        Clickhouse().VerifiedStmt("DESCRIBE test.table");
        Clickhouse().VerifiedStmt("DESCRIBE TABLE test.table");
    }

    [Fact]
    public void explain_desc()
    {
        Clickhouse().VerifiedStmt("DESC test.table");
        Clickhouse().VerifiedStmt("DESC TABLE test.table");
    }

    [Fact]
    public void parse_explain_table()
    {
        switch (Clickhouse().VerifiedStmt("EXPLAIN TABLE test_identifier"))
        {
            case Statement.ExplainTable explainTable:
                Assert.Equal(explainTable.DescribeAlias, new DescribeAlias.Explain());
                Assert.Null(explainTable.HiveFormat);
                Assert.True(explainTable.HasTableKeyword);
                Assert.Equal("test_identifier", explainTable.TableName.ToString());
                break;
            default:
                Assert.Fail("Unexpected Statement, must be ExplainTable");
                break;
        }
    }

    [Fact]
    public void parse_table_sample()
    {
        Clickhouse().VerifiedStmt("SELECT * FROM tbl SAMPLE 0.1");
        Clickhouse().VerifiedStmt("SELECT * FROM tbl SAMPLE 1000");
        Clickhouse().VerifiedStmt("SELECT * FROM tbl SAMPLE 1 / 10");
        Clickhouse().VerifiedStmt("SELECT * FROM tbl SAMPLE 1 / 10 OFFSET 1 / 2");
    }

    [Fact]
    public void test_parse_not_null_in_column_options()
    {
        // In addition to DEFAULT and CHECK ClickHouse also supports MATERIALIZED, all of which
        // can contain `IS NOT NULL` and thus `NOT NULL` as an alias.
        var canonical = "CREATE TABLE foo ("
            + "abc INT DEFAULT (42 IS NOT NULL) NOT NULL,"
            + " not_null BOOL MATERIALIZED (abc IS NOT NULL),"
            + " CHECK (abc IS NOT NULL)"
            + ")";
        Clickhouse().VerifiedStmt(canonical);
        Clickhouse().OneStatementParsesTo(
            "CREATE TABLE foo ("
                + "abc INT DEFAULT (42 NOT NULL) NOT NULL,"
                + " not_null BOOL MATERIALIZED (abc NOT NULL),"
                + " CHECK (abc NOT NULL)"
                + ")",
            canonical);
    }

    [Fact]
    public void parse_array_join()
    {
        // ARRAY JOIN works with both ClickHouseDialect and GenericDialect (roundtrip)
        ClickhouseAndGeneric().VerifiedStmt("SELECT x FROM t ARRAY JOIN arr AS x");

        // AST: join_operator is the unit variant ArrayJoin (no constraint)
        switch (ClickhouseAndGeneric().VerifiedStmt("SELECT x FROM t ARRAY JOIN arr AS x"))
        {
            case Statement.Query { Value: var query }:
            {
                var select = query.Body.AsSelect()!;
                var join = select.From[0].Joins[0];
                Assert.Equal(join.JoinOperator, new JoinOperator.ArrayJoin());
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }

        // Combined: regular JOIN followed by ARRAY JOIN
        ClickhouseAndGeneric()
            .VerifiedStmt("SELECT x FROM t JOIN u ON t.id = u.id ARRAY JOIN arr AS x");

        // Negative: ARRAY JOIN with no table expression should fail
        // .expect_err("ARRAY JOIN requires a table expression")
        Assert.Throws<ParserException>(() => ClickhouseAndGeneric()
            .ParseSqlStatements("SELECT x FROM t ARRAY JOIN"));
    }

    [Fact]
    public void parse_left_array_join()
    {
        // LEFT ARRAY JOIN preserves rows with empty/null arrays (roundtrip)
        ClickhouseAndGeneric().VerifiedStmt("SELECT x FROM t LEFT ARRAY JOIN arr AS x");

        // AST: join_operator is LeftArrayJoin
        switch (ClickhouseAndGeneric().VerifiedStmt("SELECT x FROM t LEFT ARRAY JOIN arr AS x"))
        {
            case Statement.Query { Value: var query }:
            {
                var select = query.Body.AsSelect()!;
                var join = select.From[0].Joins[0];
                Assert.Equal(join.JoinOperator, new JoinOperator.LeftArrayJoin());
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_inner_array_join()
    {
        // INNER ARRAY JOIN filters rows with empty/null arrays (roundtrip)
        ClickhouseAndGeneric().VerifiedStmt("SELECT x FROM t INNER ARRAY JOIN arr AS x");

        // AST: join_operator is InnerArrayJoin
        switch (ClickhouseAndGeneric().VerifiedStmt("SELECT x FROM t INNER ARRAY JOIN arr AS x"))
        {
            case Statement.Query { Value: var query }:
            {
                var select = query.Body.AsSelect()!;
                var join = select.From[0].Joins[0];
                Assert.Equal(join.JoinOperator, new JoinOperator.InnerArrayJoin());
                break;
            }
            default:
                Assert.Fail(Unreachable);
                break;
        }
    }

    [Fact]
    public void parse_in_unparenthesized_expr()
    {
        // IN [expr] parses to IN ([expr]) and does not cause regressions
        Clickhouse().ExprParsesTo("x IN 'a'", "x IN ('a')");

        // The branch must not fire when the next token is `(` (regressions).
        Clickhouse().VerifiedExpr("x IN (1, 2, 3)");
        Clickhouse().VerifiedStmt("SELECT * FROM t WHERE x IN (SELECT y FROM u)");
    }

    [Fact]
    public void parse_in_unparenthesized_dictionary_placeholder()
    {
        // IN [{placeholder:Type}] parses to IN ({placholder:Type})
        Clickhouse().ExprParsesTo("x IN {ids:Array(UInt64)}", "x IN ({ids: Array(UInt64)})");
        Clickhouse().ExprParsesTo(
            "x NOT IN {ids:Array(UInt64)}",
            "x NOT IN ({ids: Array(UInt64)})");
        Clickhouse().VerifiedExpr("x IN ({ids: Array(UInt64)})");
        // Precedence: the trailing `AND` is not swallowed.
        Clickhouse().VerifiedExpr("x IN ({p: Array(UInt64)}) AND y = 1");
    }

    [Fact]
    public void parse_alter_table_column_position()
    {
        Clickhouse().VerifiedStmt("ALTER TABLE t ADD COLUMN c Nullable(UInt8) FIRST");
        Clickhouse().VerifiedStmt("ALTER TABLE t ADD COLUMN c UInt8 DEFAULT 0 AFTER a");
        Clickhouse().VerifiedStmt("ALTER TABLE t ON CLUSTER cl ADD COLUMN c UInt8 AFTER a");
        Clickhouse().VerifiedStmt("ALTER TABLE t MODIFY COLUMN c UInt16 FIRST");
        Clickhouse().VerifiedStmt("ALTER TABLE t ADD COLUMN c UInt8 AFTER `order`");
        Clickhouse().VerifiedStmt("ALTER TABLE t ADD COLUMN c UInt8 AFTER \"order\"");

        switch (Clickhouse()
            .VerifiedStmt("ALTER TABLE t ADD COLUMN c UInt8 FIRST, ADD COLUMN d UInt8 AFTER c"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
            {
                var positions = operations
                    .Select(op => op switch
                    {
                        AlterTableOperation.AddColumn { ColumnPosition: var columnPosition } => columnPosition,
                        _ => throw new InvalidOperationException(Unreachable),
                    })
                    .ToList();
                Assert.Equal(
                    positions,
                    new List<MySQLColumnPosition?>
                    {
                        new MySQLColumnPosition.First(),
                        new MySQLColumnPosition.After(new Ident("c")),
                    });
                break;
            }
            default:
                throw new InvalidOperationException(Unreachable);
        }

        // assert!(..parse_sql_statements(..).is_err())
        Assert.Throws<ParserException>(() => Clickhouse()
            .ParseSqlStatements("ALTER TABLE t ADD COLUMN c UInt8 AFTER"));
    }

    [Fact]
    public void parse_alter_table_modify_order_by()
    {
        Clickhouse().VerifiedStmt("ALTER TABLE events MODIFY ORDER BY (region, user.id)");
        ClickhouseAndGeneric().VerifiedStmt("ALTER TABLE events MODIFY ORDER BY region");
        ClickhouseAndGeneric().VerifiedStmt("ALTER TABLE events MODIFY ORDER BY ()");
        ClickhouseAndGeneric().VerifiedStmt("ALTER TABLE events MODIFY ORDER BY tuple()");
        ClickhouseAndGeneric()
            .VerifiedStmt("ALTER TABLE events ON CLUSTER c MODIFY ORDER BY (a, toDate(ts))");
        ClickhouseAndGeneric()
            .VerifiedStmt("ALTER TABLE events ADD COLUMN b UInt8, MODIFY ORDER BY (a, b)");
        // A column named `order` is still handled by MODIFY COLUMN.
        ClickhouseAndGeneric().VerifiedStmt("ALTER TABLE events MODIFY COLUMN `order` UInt8");

        switch (ClickhouseAndGeneric().VerifiedStmt("ALTER TABLE events MODIFY ORDER BY (a)"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.ModifyOrderBy(
                        OrderBy: new OneOrManyWithParens<Expr>.Many(
                            Sequence.Of<Expr>(new Expr.Identifier(new Ident("a")))))));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        foreach (var sql in new[]
        {
            "ALTER TABLE events MODIFY ORDER BY",
            "ALTER TABLE events MODIFY ORDER BY (a",
            "ALTER TABLE events MODIFY ORDER BY (,)",
            "ALTER TABLE events MODIFY ORDER BY (a) b",
            "CREATE TABLE events (a UInt8) ENGINE = MergeTree ORDER BY (a",
        })
        {
            // assert!(clickhouse_and_generic().parse_sql_statements(sql).is_err(), "{sql}")
            Assert.Throws<ParserException>(() => ClickhouseAndGeneric().ParseSqlStatements(sql));
        }
    }

    [Fact]
    public void parse_object_type_parameter()
    {
        foreach (var dialects in new[]
        {
            Clickhouse(),
            new TestedDialects(new List<Dialect> { new GenericDialect() }),
        })
        {
            var statements = dialects
                .ParseSqlStatements("CREATE TABLE t (o Object('json'))");
            if (statements is not [Statement.CreateTable { Value.Columns: var columns }])
            {
                throw new InvalidOperationException(Unreachable);
            }

            Assert.Equal<DataType>(
                columns[0].DataType,
                new DataType.Custom(
                    ObjectName.From(Sequence.Of(new Ident("Object"))),
                    Sequence.Of("json")));

            var formatted = statements[0].ToString();
            Assert.Equal(
                dialects.ParseSqlStatements(formatted),
                statements);
        }

        ClickhouseAndGeneric().VerifiedStmt("CREATE TABLE t (o OBJECT())");
        ClickhouseAndGeneric().VerifiedStmt("CREATE TABLE t (o OBJECT(city VARCHAR NOT NULL))");

        foreach (var (sql, modifiers) in new (string, string[])[]
        {
            ("CREATE TABLE t (o OBJECT(10))", new[] { "10" }),
            ("CREATE TABLE t (o OBJECT(foo))", new[] { "foo" }),
            ("CREATE TABLE t (o OBJECT(foo, bar))", new[] { "foo", "bar" }),
        })
        {
            foreach (var dialects in new[]
            {
                Clickhouse(),
                new TestedDialects(new List<Dialect> { new GenericDialect() }),
            })
            {
                var statements = dialects.ParseSqlStatements(sql);
                if (statements is not [Statement.CreateTable { Value.Columns: var columns }])
                {
                    throw new InvalidOperationException(Unreachable);
                }

                Assert.Equal<DataType>(
                    columns[0].DataType,
                    new DataType.Custom(
                        ObjectName.From(Sequence.Of(new Ident("OBJECT"))),
                        modifiers
                            .Select(modifier => modifier)
                            .ToSequence()));

                var formatted = statements[0].ToString();
                Assert.Equal(
                    dialects.ParseSqlStatements(formatted),
                    statements);
            }
        }
    }

    // O upstream roda os testes com `--all-features`: `SELECT .5, 1e3` → `SELECT 0.5, 1000` é a
    // normalização do `BigDecimal`. Sem a feature (no Rust e aqui), sai `SELECT .5, 1e3`. As demais
    // asserções passam (conferido na Rodada 1, lote 5).
    [Fact(Skip = "DIV-006: a asserção `SELECT .5, 1e3` → `SELECT 0.5, 1000` exige a feature `bigdecimal`")]
    public void parse_tuple_element_access()
    {
        foreach (var (sql, canonical) in new[]
        {
            ("SELECT t.1 FROM t", "SELECT t . 1 FROM t"),
            (
                "SELECT t.1 AS a, t.2 AS b FROM (SELECT (1, 'x') AS t)",
                "SELECT t . 1 AS a, t . 2 AS b FROM (SELECT (1, 'x') AS t)"
            ),
            ("SELECT (1, 'a').1", "SELECT (1, 'a') . 1"),
            ("SELECT tuple(1, 'a').2", "SELECT tuple(1, 'a') . 2"),
            ("SELECT arr[1].1 FROM t", "SELECT arr[1] . 1 FROM t"),
            ("SELECT `t`.1 FROM t", "SELECT `t` . 1 FROM t"),
            ("SELECT t.1 + 1 FROM t", "SELECT t . 1 + 1 FROM t"),
            (
                "SELECT * FROM t WHERE t.1 = 1",
                "SELECT * FROM t WHERE t . 1 = 1"
            ),
            ("SELECT t.1.2 FROM t", "SELECT t . 1 . 2 FROM t"),
            (
                "SELECT ((1, 2), 3).1.2, tuple(1, (2, 3)).2.1",
                "SELECT ((1, 2), 3) . 1 . 2, tuple(1, (2, 3)) . 2 . 1"
            ),
        })
        {
            Clickhouse().OneStatementParsesTo(sql, canonical);
        }

        var select = Clickhouse().VerifiedOnlySelect("SELECT t . 1 FROM t");
        Assert.Equal<SelectItem>(
            select.Projection[0],
            new SelectItem.UnnamedExpr(new Expr.CompoundFieldAccess(
                Root: new Expr.Identifier(new Ident("t")),
                AccessChain: Sequence.Of<AccessExpr>(
                    new AccessExpr.Dot(new Expr.Value(TestUtils.Number("1").WithEmptySpan()))))));

        select = Clickhouse().VerifiedOnlySelect("SELECT t . 1 . 2 FROM t");
        Assert.Equal<SelectItem>(
            select.Projection[0],
            new SelectItem.UnnamedExpr(new Expr.CompoundFieldAccess(
                Root: new Expr.Identifier(new Ident("t")),
                AccessChain: Sequence.Of<AccessExpr>(
                    new AccessExpr.Dot(new Expr.Value(TestUtils.Number("1").WithEmptySpan())),
                    new AccessExpr.Dot(new Expr.Value(TestUtils.Number("2").WithEmptySpan()))))));

        Clickhouse().VerifiedStmt("SELECT 1.5, 1 + 0.5");
        Clickhouse().OneStatementParsesTo("SELECT .5, 1e3", "SELECT 0.5, 1000");

        // assert!(..parse_sql_statements(..).is_err())
        Assert.Throws<ParserException>(() => Clickhouse().ParseSqlStatements("SELECT t."));
        Assert.Throws<ParserException>(() => Clickhouse().ParseSqlStatements("SELECT t.1."));

        var unsupported = TestUtils.AllDialectsWhere(d => !d.SupportsTupleElementAccess);
        unsupported.VerifiedStmt("SELECT (1, 'a') . 1");
        foreach (var dialect in unsupported.Dialects)
        {
            // assert!(TestedDialects::new(vec![dialect]).parse_sql_statements(..).is_err())
            Assert.Throws<ParserException>(() => new TestedDialects(new List<Dialect> { dialect })
                .ParseSqlStatements("SELECT t.1 FROM t"));
        }
    }

    [Fact]
    public void parse_global_in()
    {
        var select = Clickhouse().VerifiedOnlySelect("SELECT * FROM t WHERE x GLOBAL IN (1, 2)");
        Assert.Equal<Expr>(
            select.Selection!,
            new Expr.InList(
                Expr: new Expr.Identifier(new Ident("x")),
                List: Sequence.Of<Expr>(
                    new Expr.Value(TestUtils.Number("1").WithEmptySpan()),
                    new Expr.Value(TestUtils.Number("2").WithEmptySpan())),
                Negated: false,
                Global: true));

        select = Clickhouse()
            .VerifiedOnlySelect("SELECT * FROM t WHERE x GLOBAL NOT IN (SELECT y FROM t2)");
        Assert.Equal<Expr>(
            select.Selection!,
            new Expr.InSubquery(
                Expr: new Expr.Identifier(new Ident("x")),
                Subquery: Clickhouse().VerifiedQuery("SELECT y FROM t2"),
                Negated: true,
                Global: true));

        Clickhouse().OneStatementParsesTo(
            "SELECT * FROM t WHERE x GLOBAL IN t2",
            "SELECT * FROM t WHERE x GLOBAL IN (t2)");

        select =
            Clickhouse().VerifiedOnlySelect("SELECT * FROM t WHERE x GLOBAL IN (1) AND y = 2");
        Assert.True(select.Selection! is Expr.BinaryOp { Op: BinaryOperator.And });

        // assert!(..parse_sql_statements(..).is_err())
        Assert.Throws<ParserException>(() => Clickhouse()
            .ParseSqlStatements("SELECT * FROM t WHERE x GLOBAL y"));

        foreach (var dialect in TestUtils.AllDialectsWhere(d => !d.SupportsGlobalIn).Dialects)
        {
            // assert!(TestedDialects::new(vec![dialect]).parse_sql_statements(..).is_err())
            Assert.Throws<ParserException>(() => new TestedDialects(new List<Dialect> { dialect })
                .ParseSqlStatements("SELECT * FROM t WHERE x GLOBAL IN (1)"));
        }
    }

    [Fact]
    public void parse_in_unparenthesized_expr_precedence()
    {
        foreach (var (sql, canonical, op) in new (string, string, BinaryOperator)[]
        {
            (
                "SELECT * FROM t WHERE x IN t2 AND y = 2",
                "SELECT * FROM t WHERE x IN (t2) AND y = 2",
                new BinaryOperator.And()
            ),
            (
                "SELECT * FROM t WHERE x GLOBAL IN t2 AND y = 2",
                "SELECT * FROM t WHERE x GLOBAL IN (t2) AND y = 2",
                new BinaryOperator.And()
            ),
            (
                "SELECT * FROM t WHERE x GLOBAL NOT IN t2 OR y = 2",
                "SELECT * FROM t WHERE x GLOBAL NOT IN (t2) OR y = 2",
                new BinaryOperator.Or()
            ),
            (
                "SELECT * FROM t WHERE x IN t2 = 1",
                "SELECT * FROM t WHERE x IN (t2) = 1",
                new BinaryOperator.Eq()
            ),
        })
        {
            var select = Clickhouse().VerifiedOnlySelectWithCanonical(sql, canonical);
            switch (select.Selection!)
            {
                case Expr.BinaryOp { Op: var got }:
                    TestUtils.AssertEqualWithMessage(got, op, sql);
                    break;
                case var other:
                    Assert.Fail($"{sql}: expected BinaryOp, got {other}");
                    break;
            }
        }

        // Higher-precedence operators still belong to the IN operand.
        Clickhouse().OneStatementParsesTo(
            "SELECT * FROM t WHERE x IN 1 + 2",
            "SELECT * FROM t WHERE x IN (1 + 2)");
    }

    [Fact]
    public void global_in_bare_table_binds_before_and()
    {
        var expr =
            Clickhouse().ExprParsesTo("x GLOBAL IN t2 AND y = 2", "x GLOBAL IN (t2) AND y = 2");
        Assert.True(
            expr is Expr.BinaryOp { Left: var left, Op: BinaryOperator.And }
            && left is Expr.InList { Global: true });
    }

    [Fact]
    public void global_in_bare_table_binds_before_equality()
    {
        var expr = Clickhouse().ExprParsesTo("x GLOBAL IN t2 = 1", "x GLOBAL IN (t2) = 1");
        Assert.True(
            expr is Expr.BinaryOp { Left: var left, Op: BinaryOperator.Eq }
            && left is Expr.InList { Global: true });
    }

    private static TestedDialects Clickhouse() =>
        new TestedDialects(new List<Dialect> { new ClickHouseDialect() });

    private static TestedDialects ClickhouseAndGeneric() =>
        new TestedDialects(new List<Dialect>
        {
            new ClickHouseDialect(),
            new GenericDialect(),
        });
}
