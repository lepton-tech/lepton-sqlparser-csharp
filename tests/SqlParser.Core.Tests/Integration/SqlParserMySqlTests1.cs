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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_mysql.rs (linhas 1–2650).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Test SQL syntax specific to MySQL. The parser based on the generic dialect
// is also tested (on the inputs it can handle).
//
// Notas da tradução (docs/port/onda-c/C9.md):
// - `match stmt { Statement::X(..) => .., _ => unreachable!() }` → `Assert.IsType<Statement.X>(stmt).Value`.
// - `assert_eq!(x.to_string(), "lit")`: o literal vai na posição "expected" (regra xUnit2000).
// - `assert!(v.contains(&x))` → `Assert.Contains(x, v)` (a regra xUnit2017 proíbe `Assert.True(v.Contains(x))`).
// - `assert!(matches!(res, Err(ParserError::ParserError(_))))` → `Assert.Throws<ParserException>` + `Assert.IsType<ParserError.ParserErrorValue>`.
// - `std::panic::catch_unwind(..)` + `assert!(result.is_err())` → `Assert.ThrowsAny<Exception>`.
// - `.unwrap()` de `Option` → `!` (um `null` derruba o teste, como o `panic!` do Rust).
// - Strings `r"..."`/`r#"..."#` com aspas ou barras → literais verbatim `@"..."` (aspas dobradas).
public class SqlParserMySqlTests1
{
    private static TestedDialects Mysql() =>
        new TestedDialects(new List<Dialect> { new MySqlDialect() });

    private static TestedDialects MysqlAndGeneric() =>
        new TestedDialects(new List<Dialect> { new MySqlDialect(), new GenericDialect() });

    [Fact]
    public void parse_identifiers()
    {
        Mysql().VerifiedStmt("SELECT $a$, àà");
    }

    [Fact]
    public void parse_literal_string()
    {
        var sql = @"SELECT 'single', ""double""";
        var select = Mysql().VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            new Expr.Value(new Value.SingleQuotedString("single").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Value(new Value.DoubleQuotedString("double").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[1]));
    }

    [Fact]
    public void parse_flush()
    {
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH OPTIMIZER_COSTS"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.OptimizerCosts(),
                Channel: null,
                ReadLock: false,
                Export: false,
                Tables: Sequence<ObjectName>.Empty));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH BINARY LOGS"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.BinaryLogs(),
                Channel: null,
                ReadLock: false,
                Export: false,
                Tables: Sequence<ObjectName>.Empty));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH ENGINE LOGS"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.EngineLogs(),
                Channel: null,
                ReadLock: false,
                Export: false,
                Tables: Sequence<ObjectName>.Empty));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH ERROR LOGS"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.ErrorLogs(),
                Channel: null,
                ReadLock: false,
                Export: false,
                Tables: Sequence<ObjectName>.Empty));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH NO_WRITE_TO_BINLOG GENERAL LOGS"),
            new Statement.Flush(
                Location: new FlushLocation.NoWriteToBinlog(),
                ObjectType: new FlushType.GeneralLogs(),
                Channel: null,
                ReadLock: false,
                Export: false,
                Tables: Sequence<ObjectName>.Empty));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH RELAY LOGS FOR CHANNEL test"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.RelayLogs(),
                Channel: "test",
                ReadLock: false,
                Export: false,
                Tables: Sequence<ObjectName>.Empty));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH LOCAL SLOW LOGS"),
            new Statement.Flush(
                Location: new FlushLocation.Local(),
                ObjectType: new FlushType.SlowLogs(),
                Channel: null,
                ReadLock: false,
                Export: false,
                Tables: Sequence<ObjectName>.Empty));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH TABLES `mek`.`table1`, table2"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.Tables(),
                Channel: null,
                ReadLock: false,
                Export: false,
                Tables: Sequence.Of(
                    ObjectName.From(Sequence.Of(
                        new Ident(Value: "mek", QuoteStyle: '`', Span: Span.Empty()),
                        new Ident(Value: "table1", QuoteStyle: '`', Span: Span.Empty()))),
                    ObjectName.From(Sequence.Of(
                        new Ident(Value: "table2", QuoteStyle: null, Span: Span.Empty()))))));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH TABLES WITH READ LOCK"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.Tables(),
                Channel: null,
                ReadLock: true,
                Export: false,
                Tables: Sequence<ObjectName>.Empty));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH TABLES `mek`.`table1`, table2 WITH READ LOCK"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.Tables(),
                Channel: null,
                ReadLock: true,
                Export: false,
                Tables: Sequence.Of(
                    ObjectName.From(Sequence.Of(
                        new Ident(Value: "mek", QuoteStyle: '`', Span: Span.Empty()),
                        new Ident(Value: "table1", QuoteStyle: '`', Span: Span.Empty()))),
                    ObjectName.From(Sequence.Of(
                        new Ident(Value: "table2", QuoteStyle: null, Span: Span.Empty()))))));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("FLUSH TABLES `mek`.`table1`, table2 FOR EXPORT"),
            new Statement.Flush(
                Location: null,
                ObjectType: new FlushType.Tables(),
                Channel: null,
                ReadLock: false,
                Export: true,
                Tables: Sequence.Of(
                    ObjectName.From(Sequence.Of(
                        new Ident(Value: "mek", QuoteStyle: '`', Span: Span.Empty()),
                        new Ident(Value: "table1", QuoteStyle: '`', Span: Span.Empty()))),
                    ObjectName.From(Sequence.Of(
                        new Ident(Value: "table2", QuoteStyle: null, Span: Span.Empty()))))));
    }

    [Fact]
    public void parse_show_columns()
    {
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW COLUMNS FROM mytable"),
            new Statement.ShowColumns(
                Extended: false,
                Full: false,
                ShowOptions: new ShowStatementOptions(
                    ShowIn: new ShowStatementIn(
                        Clause: new ShowStatementInClause.FROM(),
                        ParentType: null,
                        ParentName: ObjectName.From(Sequence.Of(new Ident("mytable")))),
                    FilterPosition: null,
                    LimitFrom: null,
                    Limit: null,
                    StartsWith: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW COLUMNS FROM mydb.mytable"),
            new Statement.ShowColumns(
                Extended: false,
                Full: false,
                ShowOptions: new ShowStatementOptions(
                    ShowIn: new ShowStatementIn(
                        Clause: new ShowStatementInClause.FROM(),
                        ParentType: null,
                        ParentName: ObjectName.From(Sequence.Of(
                            new Ident("mydb"),
                            new Ident("mytable")))),
                    FilterPosition: null,
                    LimitFrom: null,
                    Limit: null,
                    StartsWith: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW EXTENDED COLUMNS FROM mytable"),
            new Statement.ShowColumns(
                Extended: true,
                Full: false,
                ShowOptions: new ShowStatementOptions(
                    ShowIn: new ShowStatementIn(
                        Clause: new ShowStatementInClause.FROM(),
                        ParentType: null,
                        ParentName: ObjectName.From(Sequence.Of(new Ident("mytable")))),
                    FilterPosition: null,
                    LimitFrom: null,
                    Limit: null,
                    StartsWith: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW FULL COLUMNS FROM mytable"),
            new Statement.ShowColumns(
                Extended: false,
                Full: true,
                ShowOptions: new ShowStatementOptions(
                    ShowIn: new ShowStatementIn(
                        Clause: new ShowStatementInClause.FROM(),
                        ParentType: null,
                        ParentName: ObjectName.From(Sequence.Of(new Ident("mytable")))),
                    FilterPosition: null,
                    LimitFrom: null,
                    Limit: null,
                    StartsWith: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW COLUMNS FROM mytable LIKE 'pattern'"),
            new Statement.ShowColumns(
                Extended: false,
                Full: false,
                ShowOptions: new ShowStatementOptions(
                    ShowIn: new ShowStatementIn(
                        Clause: new ShowStatementInClause.FROM(),
                        ParentType: null,
                        ParentName: ObjectName.From(Sequence.Of(new Ident("mytable")))),
                    FilterPosition: new ShowStatementFilterPosition.Suffix(
                        new ShowStatementFilter.Like("pattern")),
                    LimitFrom: null,
                    Limit: null,
                    StartsWith: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW COLUMNS FROM mytable WHERE 1 = 2"),
            new Statement.ShowColumns(
                Extended: false,
                Full: false,
                ShowOptions: new ShowStatementOptions(
                    ShowIn: new ShowStatementIn(
                        Clause: new ShowStatementInClause.FROM(),
                        ParentType: null,
                        ParentName: ObjectName.From(Sequence.Of(new Ident("mytable")))),
                    FilterPosition: new ShowStatementFilterPosition.Suffix(
                        new ShowStatementFilter.Where(MysqlAndGeneric().VerifiedExpr("1 = 2"))),
                    LimitFrom: null,
                    Limit: null,
                    StartsWith: null)));
        MysqlAndGeneric()
            .OneStatementParsesTo("SHOW FIELDS FROM mytable", "SHOW COLUMNS FROM mytable");
        MysqlAndGeneric()
            .OneStatementParsesTo("SHOW COLUMNS IN mytable", "SHOW COLUMNS IN mytable");
        MysqlAndGeneric()
            .OneStatementParsesTo("SHOW FIELDS IN mytable", "SHOW COLUMNS IN mytable");
        MysqlAndGeneric().OneStatementParsesTo(
            "SHOW COLUMNS FROM mytable FROM mydb",
            "SHOW COLUMNS FROM mydb.mytable");
    }

    [Fact]
    public void parse_show_process_list()
    {
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW PROCESSLIST"),
            new Statement.ShowProcessList(Full: false));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW FULL PROCESSLIST"),
            new Statement.ShowProcessList(Full: true));
    }

    [Fact]
    public void parse_show_status()
    {
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW SESSION STATUS LIKE 'ssl_cipher'"),
            new Statement.ShowStatus(
                Filter: new ShowStatementFilter.Like("ssl_cipher"),
                Session: true,
                Global: false));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW GLOBAL STATUS LIKE 'ssl_cipher'"),
            new Statement.ShowStatus(
                Filter: new ShowStatementFilter.Like("ssl_cipher"),
                Session: false,
                Global: true));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW STATUS WHERE value = 2"),
            new Statement.ShowStatus(
                Filter: new ShowStatementFilter.Where(
                    MysqlAndGeneric().VerifiedExpr("value = 2")),
                Session: false,
                Global: false));
    }

    [Fact]
    public void parse_show_tables()
    {
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW TABLES"),
            new Statement.ShowTables(
                Terse: false,
                History: false,
                Extended: false,
                Full: false,
                External: false,
                ShowOptions: new ShowStatementOptions(
                    StartsWith: null,
                    Limit: null,
                    LimitFrom: null,
                    ShowIn: null,
                    FilterPosition: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW TABLES FROM mydb"),
            new Statement.ShowTables(
                Terse: false,
                History: false,
                Extended: false,
                Full: false,
                External: false,
                ShowOptions: new ShowStatementOptions(
                    StartsWith: null,
                    Limit: null,
                    LimitFrom: null,
                    ShowIn: new ShowStatementIn(
                        Clause: new ShowStatementInClause.FROM(),
                        ParentType: null,
                        ParentName: ObjectName.From(Sequence.Of(new Ident("mydb")))),
                    FilterPosition: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW EXTENDED TABLES"),
            new Statement.ShowTables(
                Terse: false,
                History: false,
                Extended: true,
                Full: false,
                External: false,
                ShowOptions: new ShowStatementOptions(
                    StartsWith: null,
                    Limit: null,
                    LimitFrom: null,
                    ShowIn: null,
                    FilterPosition: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW FULL TABLES"),
            new Statement.ShowTables(
                Terse: false,
                History: false,
                Extended: false,
                Full: true,
                External: false,
                ShowOptions: new ShowStatementOptions(
                    StartsWith: null,
                    Limit: null,
                    LimitFrom: null,
                    ShowIn: null,
                    FilterPosition: null)));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW TABLES LIKE 'pattern'"),
            new Statement.ShowTables(
                Terse: false,
                History: false,
                Extended: false,
                Full: false,
                External: false,
                ShowOptions: new ShowStatementOptions(
                    StartsWith: null,
                    Limit: null,
                    LimitFrom: null,
                    ShowIn: null,
                    FilterPosition: new ShowStatementFilterPosition.Suffix(
                        new ShowStatementFilter.Like("pattern")))));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW TABLES WHERE 1 = 2"),
            new Statement.ShowTables(
                Terse: false,
                History: false,
                Extended: false,
                Full: false,
                External: false,
                ShowOptions: new ShowStatementOptions(
                    StartsWith: null,
                    Limit: null,
                    LimitFrom: null,
                    ShowIn: null,
                    FilterPosition: new ShowStatementFilterPosition.Suffix(
                        new ShowStatementFilter.Where(MysqlAndGeneric().VerifiedExpr("1 = 2"))))));
        MysqlAndGeneric().VerifiedStmt("SHOW TABLES IN mydb");
        MysqlAndGeneric().VerifiedStmt("SHOW TABLES FROM mydb");
    }

    [Fact]
    public void parse_show_extended_full()
    {
        // assert!(..is_ok()): se o parse falhar, a ParserException derruba o teste.
        MysqlAndGeneric().ParseSqlStatements("SHOW EXTENDED FULL TABLES");
        MysqlAndGeneric().ParseSqlStatements("SHOW EXTENDED FULL COLUMNS FROM mytable");
        // SHOW EXTENDED/FULL can only be used with COLUMNS and TABLES
        Assert.Throws<ParserException>(() => MysqlAndGeneric()
            .ParseSqlStatements("SHOW EXTENDED FULL CREATE TABLE mytable"));
        Assert.Throws<ParserException>(() => MysqlAndGeneric()
            .ParseSqlStatements("SHOW EXTENDED FULL COLLATION"));
        Assert.Throws<ParserException>(() => MysqlAndGeneric()
            .ParseSqlStatements("SHOW EXTENDED FULL VARIABLES"));
    }

    [Fact]
    public void parse_show_create()
    {
        var objName = ObjectName.From(Sequence.Of(new Ident("myident")));

        foreach (var objType in new ShowCreateObject[]
        {
            new ShowCreateObject.Table(),
            new ShowCreateObject.Trigger(),
            new ShowCreateObject.Event(),
            new ShowCreateObject.Function(),
            new ShowCreateObject.Procedure(),
            new ShowCreateObject.View(),
        })
        {
            Assert.Equal(
                MysqlAndGeneric().VerifiedStmt($"SHOW CREATE {objType} myident"),
                new Statement.ShowCreate(
                    ObjType: objType,
                    ObjName: objName));
        }
    }

    [Fact]
    public void parse_show_collation()
    {
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW COLLATION"),
            new Statement.ShowCollation(Filter: null));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW COLLATION LIKE 'pattern'"),
            new Statement.ShowCollation(
                Filter: new ShowStatementFilter.Like("pattern")));
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SHOW COLLATION WHERE 1 = 2"),
            new Statement.ShowCollation(
                Filter: new ShowStatementFilter.Where(
                    MysqlAndGeneric().VerifiedExpr("1 = 2"))));
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
        var quoteStyles = new[] { '\'', '"', '`' };
        foreach (var objectName in validObjectNames)
        {
            // Test single identifier without quotes
            Assert.Equal(
                MysqlAndGeneric().VerifiedStmt($"USE {objectName}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(new Ident(
                    objectName))))));
            foreach (var quote in quoteStyles)
            {
                // Test single identifier with different type of quotes
                Assert.Equal(
                    MysqlAndGeneric().VerifiedStmt($"USE {quote}{objectName}{quote}"),
                    new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(Ident.WithQuote(
                        quote,
                        objectName))))));
            }
        }
    }

    [Fact]
    public void parse_set_variables()
    {
        MysqlAndGeneric().VerifiedStmt("SET sql_mode = CONCAT(@@sql_mode, ',STRICT_TRANS_TABLES')");
        Assert.Equal(
            MysqlAndGeneric().VerifiedStmt("SET LOCAL autocommit = 1"),
            new Statement.Set(new Set.SingleAssignment(
                Scope: new ContextModifier.Local(),
                Hivevar: false,
                Variable: ObjectName.From(Sequence.Of(Ident.From("autocommit"))),
                Values: Sequence.Of(Expr.MakeValue(TestUtils.Number("1"))))));
    }

    [Fact]
    public void parse_create_table_auto_increment()
    {
        var sql = "CREATE TABLE foo (bar INT PRIMARY KEY AUTO_INCREMENT)";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(new ColumnDef(
                Name: new Ident("bar"),
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
                            "AUTO_INCREMENT"))))))),
            columns);
    }

    // if `unique_index_type_display` is `Some` create `TableConstraint::Unique`
    //  otherwise create `TableConstraint::Primary`
    private static TableConstraint TableConstraintUniquePrimaryCtor(
        Ident? name,
        Ident? indexName,
        IndexType? indexType,
        Sequence<Ident> columns,
        Sequence<IndexOption> indexOptions,
        ConstraintCharacteristics? characteristics,
        KeyOrIndexDisplay? uniqueIndexTypeDisplay)
    {
        var indexColumns = columns
            .Select(ident => new IndexColumn(
                Column: new OrderByExpr(
                    Expr: new Expr.Identifier(ident),
                    Options: new OrderByOptions(
                        Sort: null,
                        NullsFirst: null),
                    WithFill: null),
                OperatorClass: null))
            .ToSequence();
        return uniqueIndexTypeDisplay switch
        {
            { } indexTypeDisplay => TableConstraint.From(new UniqueConstraint(
                Name: name,
                IndexName: indexName,
                IndexTypeDisplay: indexTypeDisplay,
                IndexType: indexType,
                Columns: indexColumns,
                Include: Sequence<Ident>.Empty,
                IndexOptions: indexOptions,
                Characteristics: characteristics,
                NullsDistinct: new NullsDistinctOption.None())),
            null => TableConstraint.From(new PrimaryKeyConstraint(
                Name: name,
                IndexName: indexName,
                IndexType: indexType,
                Columns: indexColumns,
                Include: Sequence<Ident>.Empty,
                IndexOptions: indexOptions,
                Characteristics: characteristics)),
        };
    }

    [Fact]
    public void parse_create_table_primary_and_unique_key()
    {
        var sqls = new[] { "UNIQUE KEY", "PRIMARY KEY" }
            .Select(keyTy => $"CREATE TABLE foo (id INT PRIMARY KEY AUTO_INCREMENT, bar INT NOT NULL, CONSTRAINT bar_key {keyTy} (bar))")
            .ToArray();

        var indexTypeDisplays = new KeyOrIndexDisplay?[] { new KeyOrIndexDisplay.Key(), null };

        // sqls.iter().zip(index_type_display)
        for (var i = 0; i < Math.Min(sqls.Length, indexTypeDisplays.Length); i++)
        {
            var sql = sqls[i];
            var indexTypeDisplay = indexTypeDisplays[i];
            var createTable = Assert.IsType<Statement.CreateTable>(Mysql().OneStatementParsesTo(sql, "")).Value;
            var name = createTable.Name;
            var columns = createTable.Columns;
            var constraints = createTable.Constraints;
            Assert.Equal("foo", name.ToString());

            var expectedConstraint = TableConstraintUniquePrimaryCtor(
                new Ident("bar_key"),
                null,
                null,
                Sequence.Of(new Ident("bar")),
                Sequence<IndexOption>.Empty,
                null,
                indexTypeDisplay);
            Assert.Equal(Sequence.Of(expectedConstraint), constraints);

            Assert.Equal(
                Sequence.Of(
                    new ColumnDef(
                        Name: new Ident("id"),
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
                                Option: new ColumnOption.DialectSpecific(Sequence.Of(
                                    Token.MakeKeyword("AUTO_INCREMENT")))))),
                    new ColumnDef(
                        Name: new Ident("bar"),
                        DataType: new DataType.Int(null),
                        Options: Sequence.Of(new ColumnOptionDef(
                            Name: null,
                            Option: new ColumnOption.NotNull())))),
                columns);
        }
    }

    [Fact]
    public void parse_create_table_primary_and_unique_key_with_index_options()
    {
        var sqls = new[] { "UNIQUE INDEX", "PRIMARY KEY" }
            .Select(keyTy => $"CREATE TABLE foo (bar INT, var INT, CONSTRAINT constr {keyTy} index_name (bar, var) USING HASH COMMENT 'yes, ' USING BTREE COMMENT 'MySQL allows')")
            .ToArray();

        var indexTypeDisplays = new KeyOrIndexDisplay?[] { new KeyOrIndexDisplay.Index(), null };

        // sqls.iter().zip(index_type_display)
        for (var i = 0; i < Math.Min(sqls.Length, indexTypeDisplays.Length); i++)
        {
            var sql = sqls[i];
            var indexTypeDisplay = indexTypeDisplays[i];
            var createTable = Assert.IsType<Statement.CreateTable>(MysqlAndGeneric().OneStatementParsesTo(sql, "")).Value;
            var name = createTable.Name;
            var constraints = createTable.Constraints;
            Assert.Equal("foo", name.ToString());

            var expectedConstraint = TableConstraintUniquePrimaryCtor(
                new Ident("constr"),
                new Ident("index_name"),
                null,
                Sequence.Of(new Ident("bar"), new Ident("var")),
                Sequence.Of<IndexOption>(
                    new IndexOption.Using(new IndexType.Hash()),
                    new IndexOption.Comment("yes, "),
                    new IndexOption.Using(new IndexType.BTree()),
                    new IndexOption.Comment("MySQL allows")),
                null,
                indexTypeDisplay);
            Assert.Equal(Sequence.Of(expectedConstraint), constraints);

            MysqlAndGeneric().VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_prefix_key_part()
    {
        var expected = Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(Expr.MakeValue(
            TestUtils.Number("10")))));
        foreach (var sql in new[]
        {
            "CREATE INDEX idx_index ON t(textcol(10))",
            "ALTER TABLE tab ADD INDEX idx_index (textcol(10))",
            "ALTER TABLE tab ADD PRIMARY KEY (textcol(10))",
            "ALTER TABLE tab ADD UNIQUE KEY (textcol(10))",
            "ALTER TABLE tab ADD UNIQUE KEY (textcol(10))",
            "ALTER TABLE tab ADD FULLTEXT INDEX (textcol(10))",
            "CREATE TABLE t (textcol TEXT, INDEX idx_index (textcol(10)))",
        })
        {
            switch (TestUtils.IndexColumn(MysqlAndGeneric().VerifiedStmt(sql)))
            {
                case Expr.Function
                {
                    Value:
                    {
                        Name: var name,
                        Args: FunctionArguments.List { Value: { Args: var args } },
                    },
                }:
                    Assert.Equal("textcol", name.ToString());
                    Assert.Equal(args, expected);
                    break;
                case var expr:
                    Assert.Fail($"unexpected expression {expr} for {sql}");
                    break;
            }
        }
    }

    [Fact]
    public void test_functional_key_part()
    {
        Assert.Equal(
            TestUtils.IndexColumn(
                MysqlAndGeneric()
                    .VerifiedStmt("CREATE INDEX idx_index ON t((col COLLATE utf8mb4_bin) DESC)")),
            new Expr.Nested(new Expr.Collate(
                Expr: new Expr.Identifier(Ident.From("col")),
                Collation: new ObjectName(Sequence.Of<ObjectNamePart>(new ObjectNamePart.Identifier(
                    new Ident("utf8mb4_bin")))))));
        Assert.Equal(
            TestUtils.IndexColumn(MysqlAndGeneric().VerifiedStmt(
                "CREATE TABLE t (jsoncol JSON, PRIMARY KEY ((CAST(col ->> '$.id' AS UNSIGNED)) ASC))")),
            new Expr.Nested(new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("col")),
                    Op: new BinaryOperator.LongArrow(),
                    Right: new Expr.Value(
                        new Value.SingleQuotedString("$.id").WithEmptySpan())),
                DataType: new DataType.Unsigned(),
                Format: null)));
        Assert.Equal(
            TestUtils.IndexColumn(MysqlAndGeneric().VerifiedStmt(
                "CREATE TABLE t (jsoncol JSON, PRIMARY KEY ((CAST(col ->> '$.fields' AS UNSIGNED ARRAY)) ASC))")),
            new Expr.Nested(new Expr.Cast(
                Kind: CastKind.Cast,
                Expr: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("col")),
                    Op: new BinaryOperator.LongArrow(),
                    Right: new Expr.Value(
                        new Value.SingleQuotedString("$.fields").WithEmptySpan())),
                DataType: new DataType.Array(new ArrayElemTypeDef.Qualified(
                    new DataType.Unsigned(),
                    null)),
                Format: null)));
    }

    [Fact]
    public void parse_create_table_primary_and_unique_key_with_index_type()
    {
        var sqls = new[] { "UNIQUE", "PRIMARY KEY" }
            .Select(keyTy => $"CREATE TABLE foo (bar INT, {keyTy} index_name USING BTREE (bar) USING HASH)")
            .ToArray();

        var indexTypeDisplays = new KeyOrIndexDisplay?[] { new KeyOrIndexDisplay.None(), null };

        // sqls.iter().zip(index_type_display)
        for (var i = 0; i < Math.Min(sqls.Length, indexTypeDisplays.Length); i++)
        {
            var sql = sqls[i];
            var indexTypeDisplay = indexTypeDisplays[i];
            var createTable = Assert.IsType<Statement.CreateTable>(MysqlAndGeneric().OneStatementParsesTo(sql, "")).Value;
            var name = createTable.Name;
            var constraints = createTable.Constraints;
            Assert.Equal("foo", name.ToString());

            var expectedConstraint = TableConstraintUniquePrimaryCtor(
                null,
                new Ident("index_name"),
                new IndexType.BTree(),
                Sequence.Of(new Ident("bar")),
                Sequence.Of<IndexOption>(new IndexOption.Using(new IndexType.Hash())),
                null,
                indexTypeDisplay);
            Assert.Equal(Sequence.Of(expectedConstraint), constraints);
            MysqlAndGeneric().VerifiedStmt(sql);
        }

        {
            var sql = "CREATE TABLE foo (bar INT, UNIQUE INDEX index_name USING BTREE (bar) USING HASH)";
            MysqlAndGeneric().VerifiedStmt(sql);
            sql = "CREATE TABLE foo (bar INT, PRIMARY KEY index_name USING BTREE (bar) USING HASH)";
            MysqlAndGeneric().VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_create_table_primary_and_unique_key_characteristic_test()
    {
        var sqls = new[] { "UNIQUE INDEX", "PRIMARY KEY" }
            .Select(keyTy => $"CREATE TABLE x (y INT, CONSTRAINT constr {keyTy} (y) NOT DEFERRABLE INITIALLY IMMEDIATE)")
            .ToArray();
        foreach (var sql in sqls)
        {
            MysqlAndGeneric().VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_create_table_column_key_options()
    {
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE foo (x INT UNIQUE KEY)");
        MysqlAndGeneric().OneStatementParsesTo(
            "CREATE TABLE foo (x INT KEY)",
            "CREATE TABLE foo (x INT PRIMARY KEY)");
    }

    [Fact]
    public void parse_create_table_comment()
    {
        var withoutEqual = "CREATE TABLE foo (bar INT) COMMENT 'baz'";
        var withEqual = "CREATE TABLE foo (bar INT) COMMENT = 'baz'";

        foreach (var sql in new[] { withoutEqual, withEqual })
        {
            var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
            var name = createTable.Name;
            var tableOptions = createTable.TableOptions;
            Assert.Equal("foo", name.ToString());

            var plainOptions = tableOptions switch
            {
                CreateTableOptions.Plain { Value: var options } => options,
                _ => throw new InvalidOperationException("internal error: entered unreachable code"),
            };
            var comment = plainOptions.First! switch
            {
                SqlOption.Comment { Value: CommentDef.WithEq { Value: var c } } => c,
                SqlOption.Comment { Value: CommentDef.WithoutEq { Value: var c } } => c,
                _ => throw new InvalidOperationException("internal error: entered unreachable code"),
            };
            Assert.Equal("baz", comment);
        }
    }

    [Fact]
    public void parse_create_table_auto_increment_offset()
    {
        var sql =
            "CREATE TABLE foo (bar INT NOT NULL AUTO_INCREMENT) ENGINE = InnoDB AUTO_INCREMENT = 123";

        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var tableOptions = createTable.TableOptions;
        Assert.Equal("foo", name.ToString());

        var plainOptions = tableOptions switch
        {
            CreateTableOptions.Plain { Value: var options } => options,
            _ => throw new InvalidOperationException("internal error: entered unreachable code"),
        };

        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("AUTO_INCREMENT"),
            Value: new Expr.Value(TestUtils.Number("123").WithEmptySpan())), plainOptions);
    }

    [Fact]
    public void parse_create_table_multiple_options_order_independent()
    {
        var sql1 = "CREATE TABLE mytable (id INT) ENGINE=InnoDB ROW_FORMAT=DYNAMIC KEY_BLOCK_SIZE=8 COMMENT='abc'";
        var sql2 = "CREATE TABLE mytable (id INT) KEY_BLOCK_SIZE=8 COMMENT='abc' ENGINE=InnoDB ROW_FORMAT=DYNAMIC";
        var sql3 = "CREATE TABLE mytable (id INT) ROW_FORMAT=DYNAMIC KEY_BLOCK_SIZE=8 COMMENT='abc' ENGINE=InnoDB";

        foreach (var sql in new[] { sql1, sql2, sql3 })
        {
            // parse_sql_statements(sql).unwrap().pop().unwrap()
            var createTable = Assert.IsType<Statement.CreateTable>(Mysql().ParseSqlStatements(sql).Last!).Value;
            var name = createTable.Name;
            var tableOptions = createTable.TableOptions;
            Assert.Equal("mytable", name.ToString());

            var plainOptions = tableOptions switch
            {
                CreateTableOptions.Plain { Value: var options } => options,
                _ => throw new InvalidOperationException("internal error: entered unreachable code"),
            };

            Assert.Contains(new SqlOption.NamedParenthesizedList(
                new NamedParenthesizedList(
                    Key: new Ident("ENGINE"),
                    Name: new Ident("InnoDB"),
                    Values: Sequence<Ident>.Empty)), plainOptions);

            Assert.Contains(new SqlOption.KeyValue(
                Key: new Ident("KEY_BLOCK_SIZE"),
                Value: new Expr.Value(TestUtils.Number("8").WithEmptySpan())), plainOptions);

            Assert.Contains(
                new SqlOption.Comment(new CommentDef.WithEq("abc")), plainOptions);

            Assert.Contains(new SqlOption.KeyValue(
                Key: new Ident("ROW_FORMAT"),
                Value: new Expr.Identifier(new Ident("DYNAMIC"))), plainOptions);
        }
    }

    [Fact]
    public void parse_create_table_with_all_table_options()
    {
        var sql =
            "CREATE TABLE foo (bar INT NOT NULL AUTO_INCREMENT) ENGINE = InnoDB AUTO_INCREMENT = 123 DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci INSERT_METHOD = FIRST KEY_BLOCK_SIZE = 8 ROW_FORMAT = DYNAMIC DATA DIRECTORY = '/var/lib/mysql/data' INDEX DIRECTORY = '/var/lib/mysql/index' PACK_KEYS = 1 STATS_AUTO_RECALC = 1 STATS_PERSISTENT = 0 STATS_SAMPLE_PAGES = 128 DELAY_KEY_WRITE = 1 COMPRESSION = 'ZLIB' ENCRYPTION = 'Y' MAX_ROWS = 10000 MIN_ROWS = 10 AUTOEXTEND_SIZE = 64 AVG_ROW_LENGTH = 128 CHECKSUM = 1 CONNECTION = 'mysql://localhost' ENGINE_ATTRIBUTE = 'primary' PASSWORD = 'secure_password' SECONDARY_ENGINE_ATTRIBUTE = 'secondary_attr' START TRANSACTION TABLESPACE my_tablespace STORAGE DISK UNION = (table1, table2, table3)";

        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var tableOptions = createTable.TableOptions;
        Assert.Equal(name, ObjectName.From(Sequence.Of(new Ident("foo"))));

        var plainOptions = tableOptions switch
        {
            CreateTableOptions.Plain { Value: var options } => options,
            _ => throw new InvalidOperationException("internal error: entered unreachable code"),
        };

        Assert.Contains(new SqlOption.NamedParenthesizedList(
            new NamedParenthesizedList(
                Key: new Ident("ENGINE"),
                Name: new Ident("InnoDB"),
                Values: Sequence<Ident>.Empty)), plainOptions);

        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("COLLATE"),
            Value: new Expr.Identifier(new Ident("utf8mb4_0900_ai_ci"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("DEFAULT CHARSET"),
            Value: new Expr.Identifier(new Ident("utf8mb4"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("AUTO_INCREMENT"),
            Value: Expr.MakeValue(TestUtils.Number("123"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("KEY_BLOCK_SIZE"),
            Value: Expr.MakeValue(TestUtils.Number("8"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("ROW_FORMAT"),
            Value: new Expr.Identifier(new Ident("DYNAMIC"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("PACK_KEYS"),
            Value: Expr.MakeValue(TestUtils.Number("1"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("STATS_AUTO_RECALC"),
            Value: Expr.MakeValue(TestUtils.Number("1"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("STATS_PERSISTENT"),
            Value: Expr.MakeValue(TestUtils.Number("0"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("STATS_SAMPLE_PAGES"),
            Value: Expr.MakeValue(TestUtils.Number("128"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("STATS_SAMPLE_PAGES"),
            Value: Expr.MakeValue(TestUtils.Number("128"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("INSERT_METHOD"),
            Value: new Expr.Identifier(new Ident("FIRST"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("COMPRESSION"),
            Value: Expr.MakeValue(new Value.SingleQuotedString("ZLIB"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("ENCRYPTION"),
            Value: Expr.MakeValue(new Value.SingleQuotedString("Y"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("MAX_ROWS"),
            Value: Expr.MakeValue(TestUtils.Number("10000"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("MIN_ROWS"),
            Value: Expr.MakeValue(TestUtils.Number("10"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("AUTOEXTEND_SIZE"),
            Value: Expr.MakeValue(TestUtils.Number("64"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("AVG_ROW_LENGTH"),
            Value: Expr.MakeValue(TestUtils.Number("128"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("CHECKSUM"),
            Value: Expr.MakeValue(TestUtils.Number("1"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("CONNECTION"),
            Value: Expr.MakeValue(new Value.SingleQuotedString("mysql://localhost"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("ENGINE_ATTRIBUTE"),
            Value: Expr.MakeValue(new Value.SingleQuotedString("primary"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("PASSWORD"),
            Value: Expr.MakeValue(new Value.SingleQuotedString("secure_password"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("SECONDARY_ENGINE_ATTRIBUTE"),
            Value: Expr.MakeValue(new Value.SingleQuotedString("secondary_attr"))), plainOptions);
        Assert.Contains(new SqlOption.Ident(new Ident(
            "START TRANSACTION")), plainOptions);
        Assert.Contains(
            new SqlOption.TableSpace(new TablespaceOption(
                Name: "my_tablespace",
                Storage: StorageType.Disk)), plainOptions);

        Assert.Contains(new SqlOption.NamedParenthesizedList(
            new NamedParenthesizedList(
                Key: new Ident("UNION"),
                Name: null,
                Values: Sequence.Of(
                    new Ident("table1"),
                    new Ident("table2"),
                    new Ident("table3")))), plainOptions);

        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("DATA DIRECTORY"),
            Value: Expr.MakeValue(new Value.SingleQuotedString("/var/lib/mysql/data"))), plainOptions);
        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("INDEX DIRECTORY"),
            Value: Expr.MakeValue(new Value.SingleQuotedString("/var/lib/mysql/index"))), plainOptions);
    }

    [Fact]
    public void parse_create_table_set_enum()
    {
        var sql = "CREATE TABLE foo (bar SET('a', 'b'), baz ENUM('a', 'b'))";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(
                new ColumnDef(
                    Name: new Ident("bar"),
                    DataType: new DataType.Set(Sequence.Of("a", "b")),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("baz"),
                    DataType: new DataType.Enum(
                        Sequence.Of<EnumMember>(
                            new EnumMember.Name("a"),
                            new EnumMember.Name("b")),
                        null),
                    Options: Sequence<ColumnOptionDef>.Empty)),
            columns);
    }

    [Fact]
    public void parse_create_table_engine_default_charset()
    {
        var sql = "CREATE TABLE foo (id INT(11)) ENGINE = InnoDB DEFAULT CHARSET = utf8mb3";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        var tableOptions = createTable.TableOptions;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(new ColumnDef(
                Name: new Ident("id"),
                DataType: new DataType.Int(11),
                Options: Sequence<ColumnOptionDef>.Empty)),
            columns);

        var plainOptions = tableOptions switch
        {
            CreateTableOptions.Plain { Value: var options } => options,
            _ => throw new InvalidOperationException("internal error: entered unreachable code"),
        };

        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("DEFAULT CHARSET"),
            Value: new Expr.Identifier(new Ident("utf8mb3"))), plainOptions);

        Assert.Contains(new SqlOption.NamedParenthesizedList(
            new NamedParenthesizedList(
                Key: new Ident("ENGINE"),
                Name: new Ident("InnoDB"),
                Values: Sequence<Ident>.Empty)), plainOptions);
    }

    [Fact]
    public void parse_create_table_collate()
    {
        var sql = "CREATE TABLE foo (id INT(11)) COLLATE = utf8mb4_0900_ai_ci";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        var tableOptions = createTable.TableOptions;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(new ColumnDef(
                Name: new Ident("id"),
                DataType: new DataType.Int(11),
                Options: Sequence<ColumnOptionDef>.Empty)),
            columns);

        var plainOptions = tableOptions switch
        {
            CreateTableOptions.Plain { Value: var options } => options,
            _ => throw new InvalidOperationException("internal error: entered unreachable code"),
        };

        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("COLLATE"),
            Value: new Expr.Identifier(new Ident("utf8mb4_0900_ai_ci"))), plainOptions);
    }

    [Fact]
    public void parse_create_table_both_options_and_as_query()
    {
        var sql = "CREATE TABLE foo (id INT(11)) ENGINE = InnoDB DEFAULT CHARSET = utf8mb3 COLLATE = utf8mb4_0900_ai_ci AS SELECT 1";
        var createTable = Assert.IsType<Statement.CreateTable>(MysqlAndGeneric().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var query = createTable.Query;
        var tableOptions = createTable.TableOptions;
        Assert.Equal("foo", name.ToString());

        var plainOptions = tableOptions switch
        {
            CreateTableOptions.Plain { Value: var options } => options,
            _ => throw new InvalidOperationException("internal error: entered unreachable code"),
        };

        Assert.Contains(new SqlOption.KeyValue(
            Key: new Ident("COLLATE"),
            Value: new Expr.Identifier(new Ident("utf8mb4_0900_ai_ci"))), plainOptions);

        Assert.Equal(
            query!.Body.AsSelect()!.Projection,
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Value(
                TestUtils.Number("1").WithEmptySpan()))));

        sql =
            @"CREATE TABLE foo (id INT(11)) ENGINE = InnoDB AS SELECT 1 DEFAULT CHARSET = utf8mb3";
        var ex = Assert.Throws<ParserException>(() => MysqlAndGeneric().ParseSqlStatements(sql));
        Assert.IsType<ParserError.ParserErrorValue>(ex.Error);
    }

    [Fact]
    public void parse_create_table_comment_character_set()
    {
        var sql = "CREATE TABLE foo (s TEXT CHARACTER SET utf8mb4 COMMENT 'comment')";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(new ColumnDef(
                Name: new Ident("s"),
                DataType: new DataType.Text(),
                Options: Sequence.Of(
                    new ColumnOptionDef(
                        Name: null,
                        Option: new ColumnOption.CharacterSet(ObjectName.From(Sequence.Of(new Ident(
                            "utf8mb4"))))),
                    new ColumnOptionDef(
                        Name: null,
                        Option: new ColumnOption.Comment("comment"))))),
            columns);
    }

    [Fact]
    public void parse_create_table_gencol()
    {
        var sqlDefault = "CREATE TABLE t1 (a INT, b INT GENERATED ALWAYS AS (a * 2))";
        MysqlAndGeneric().VerifiedStmt(sqlDefault);

        var sqlVirt = "CREATE TABLE t1 (a INT, b INT GENERATED ALWAYS AS (a * 2) VIRTUAL)";
        MysqlAndGeneric().VerifiedStmt(sqlVirt);

        var sqlStored = "CREATE TABLE t1 (a INT, b INT GENERATED ALWAYS AS (a * 2) STORED)";
        MysqlAndGeneric().VerifiedStmt(sqlStored);

        MysqlAndGeneric().VerifiedStmt("CREATE TABLE t1 (a INT, b INT AS (a * 2))");
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE t1 (a INT, b INT AS (a * 2) VIRTUAL)");
        MysqlAndGeneric().VerifiedStmt("CREATE TABLE t1 (a INT, b INT AS (a * 2) STORED)");
    }

    [Fact]
    public void parse_create_table_options_comma_separated()
    {
        var sql = "CREATE TABLE t (x INT) DEFAULT CHARSET = utf8mb4, ENGINE = InnoDB , AUTO_INCREMENT 1 DATA DIRECTORY '/var/lib/mysql/data'";
        var canonical = "CREATE TABLE t (x INT) DEFAULT CHARSET = utf8mb4 ENGINE = InnoDB AUTO_INCREMENT = 1 DATA DIRECTORY = '/var/lib/mysql/data'";
        MysqlAndGeneric().OneStatementParsesTo(sql, canonical);
    }

    [Fact]
    public void parse_quote_identifiers()
    {
        var sql = "CREATE TABLE `PRIMARY` (`BEGIN` INT PRIMARY KEY)";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        Assert.Equal("`PRIMARY`", name.ToString());
        Assert.Equal(
            Sequence.Of(new ColumnDef(
                Name: Ident.WithQuote('`', "BEGIN"),
                DataType: new DataType.Int(null),
                Options: Sequence.Of(new ColumnOptionDef(
                    Name: null,
                    Option: new ColumnOption.PrimaryKey(new PrimaryKeyConstraint(
                        Name: null,
                        IndexName: null,
                        IndexType: null,
                        Columns: Sequence<IndexColumn>.Empty,
                        Include: Sequence<Ident>.Empty,
                        IndexOptions: Sequence<IndexOption>.Empty,
                        Characteristics: null)))))),
            columns);
    }

    [Fact]
    public void parse_escaped_quote_identifiers_with_escape()
    {
        var sql = "SELECT `quoted `` identifier`";
        Assert.Equal(
            new TestedDialects(new List<Dialect> { new MySqlDialect() }).VerifiedStmt(sql),
            new Statement.Query(new Query(
                With: null,
                Body: new SetExpr.Select(new Select(
                    SelectToken: AttachedToken.Empty(),
                    OptimizerHints: Sequence<OptimizerHint>.Empty,
                    Distinct: null,
                    SelectModifiers: null,
                    Top: null,
                    TopBeforeDistinct: false,
                    Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident(
                        Value: "quoted ` identifier",
                        QuoteStyle: '`',
                        Span: Span.Empty())))),
                    Exclude: null,
                    Into: null,
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
                PipeOperators: Sequence<PipeOperator>.Empty)));
    }

    [Fact]
    public void parse_escaped_quote_identifiers_with_no_escape()
    {
        var sql = "SELECT `quoted `` identifier`";
        Assert.Equal(
            TestedDialects.NewWithOptions(
                new List<Dialect> { new MySqlDialect() },
                new ParserOptions(
                    TrailingCommas: false,
                    Unescape: false,
                    RequireSemicolonStmtDelimiter: true))
            .VerifiedStmt(sql),
            new Statement.Query(new Query(
                With: null,
                Body: new SetExpr.Select(new Select(
                    SelectToken: AttachedToken.Empty(),
                    OptimizerHints: Sequence<OptimizerHint>.Empty,
                    Distinct: null,
                    SelectModifiers: null,
                    Top: null,
                    TopBeforeDistinct: false,
                    Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident(
                        Value: "quoted `` identifier",
                        QuoteStyle: '`',
                        Span: Span.Empty())))),
                    Exclude: null,
                    Into: null,
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
                PipeOperators: Sequence<PipeOperator>.Empty)));
    }

    [Fact]
    public void parse_escaped_backticks_with_escape()
    {
        var sql = "SELECT ```quoted identifier```";
        Assert.Equal(
            new TestedDialects(new List<Dialect> { new MySqlDialect() }).VerifiedStmt(sql),
            new Statement.Query(new Query(
                With: null,
                Body: new SetExpr.Select(new Select(
                    SelectToken: AttachedToken.Empty(),
                    OptimizerHints: Sequence<OptimizerHint>.Empty,
                    Distinct: null,
                    SelectModifiers: null,
                    Top: null,
                    TopBeforeDistinct: false,
                    Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident(
                        Value: "`quoted identifier`",
                        QuoteStyle: '`',
                        Span: Span.Empty())))),
                    Exclude: null,
                    Into: null,
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
                PipeOperators: Sequence<PipeOperator>.Empty)));
    }

    [Fact]
    public void parse_escaped_backticks_with_no_escape()
    {
        var sql = "SELECT ```quoted identifier```";
        Assert.Equal(
            TestedDialects.NewWithOptions(
                new List<Dialect> { new MySqlDialect() },
                new ParserOptions().WithUnescape(false))
            .VerifiedStmt(sql),
            new Statement.Query(new Query(
                With: null,
                Body: new SetExpr.Select(new Select(
                    SelectToken: AttachedToken.Empty(),
                    OptimizerHints: Sequence<OptimizerHint>.Empty,
                    Distinct: null,
                    SelectModifiers: null,
                    Top: null,
                    TopBeforeDistinct: false,
                    Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident(
                        Value: "``quoted identifier``",
                        QuoteStyle: '`',
                        Span: Span.Empty())))),
                    Exclude: null,
                    Into: null,
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
                PipeOperators: Sequence<PipeOperator>.Empty)));
    }

    [Fact]
    public void parse_unterminated_escape()
    {
        var sql = @"SELECT 'I\'m not fine\'";
        Assert.ThrowsAny<Exception>(() => Mysql().OneStatementParsesTo(sql, ""));

        sql = @"SELECT 'I\\'m not fine'";
        Assert.ThrowsAny<Exception>(() => Mysql().OneStatementParsesTo(sql, ""));
    }

    [Fact]
    public void check_roundtrip_of_escaped_string()
    {
        var options = new ParserOptions().WithUnescape(false);

        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt(@"SELECT 'I\'m fine'");
        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt("SELECT 'I''m fine'");
        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt(@"SELECT 'I\\\'m fine'");
        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt(@"SELECT 'I\\\'m fine'");
        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt(@"SELECT ""I\""m fine""");
        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt(@"SELECT ""I""""m fine""");
        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt(@"SELECT ""I\\\""m fine""");
        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt(@"SELECT ""I\\\""m fine""");
        TestedDialects.NewWithOptions(new List<Dialect> { new MySqlDialect() }, options)
            .VerifiedStmt(@"SELECT ""I'm ''fine''""");
    }

    [Fact]
    public void parse_create_table_with_minimum_display_width()
    {
        var sql = "CREATE TABLE foo (bar_tinyint TINYINT(3), bar_smallint SMALLINT(5), bar_mediumint MEDIUMINT(6), bar_int INT(11), bar_bigint BIGINT(20))";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(
                new ColumnDef(
                    Name: new Ident("bar_tinyint"),
                    DataType: new DataType.TinyInt(3),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_smallint"),
                    DataType: new DataType.SmallInt(5),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_mediumint"),
                    DataType: new DataType.MediumInt(6),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_int"),
                    DataType: new DataType.Int(11),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_bigint"),
                    DataType: new DataType.BigInt(20),
                    Options: Sequence<ColumnOptionDef>.Empty)),
            columns);
    }

    [Fact]
    public void parse_create_table_unsigned()
    {
        var sql = "CREATE TABLE foo (bar_tinyint TINYINT(3) UNSIGNED, bar_smallint SMALLINT(5) UNSIGNED, bar_mediumint MEDIUMINT(13) UNSIGNED, bar_int INT(11) UNSIGNED, bar_bigint BIGINT(20) UNSIGNED)";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(
                new ColumnDef(
                    Name: new Ident("bar_tinyint"),
                    DataType: new DataType.TinyIntUnsigned(3),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_smallint"),
                    DataType: new DataType.SmallIntUnsigned(5),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_mediumint"),
                    DataType: new DataType.MediumIntUnsigned(13),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_int"),
                    DataType: new DataType.IntUnsigned(11),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_bigint"),
                    DataType: new DataType.BigIntUnsigned(20),
                    Options: Sequence<ColumnOptionDef>.Empty)),
            columns);
    }

    [Fact]
    public void parse_signed_data_types()
    {
        var sql = "CREATE TABLE foo (bar_tinyint TINYINT(3) SIGNED, bar_smallint SMALLINT(5) SIGNED, bar_mediumint MEDIUMINT(13) SIGNED, bar_int INT(11) SIGNED, bar_bigint BIGINT(20) SIGNED)";
        var canonical = "CREATE TABLE foo (bar_tinyint TINYINT(3), bar_smallint SMALLINT(5), bar_mediumint MEDIUMINT(13), bar_int INT(11), bar_bigint BIGINT(20))";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().OneStatementParsesTo(sql, canonical)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(
                new ColumnDef(
                    Name: new Ident("bar_tinyint"),
                    DataType: new DataType.TinyInt(3),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_smallint"),
                    DataType: new DataType.SmallInt(5),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_mediumint"),
                    DataType: new DataType.MediumInt(13),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_int"),
                    DataType: new DataType.Int(11),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_bigint"),
                    DataType: new DataType.BigInt(20),
                    Options: Sequence<ColumnOptionDef>.Empty)),
            columns);
        // .expect_err("SIGNED suffix should not be allowed")
        Assert.Throws<ParserException>(() => TestUtils.AllDialectsExcept(d => d.SupportsDataTypeSignedSuffix)
            .RunParserMethod(sql, p => p.ParseStatement()));
    }

    [Fact]
    public void parse_deprecated_mysql_unsigned_data_types()
    {
        var sql = "CREATE TABLE foo (bar_decimal DECIMAL UNSIGNED, bar_decimal_prec DECIMAL(10) UNSIGNED, bar_decimal_scale DECIMAL(10,2) UNSIGNED, bar_dec DEC UNSIGNED, bar_dec_prec DEC(10) UNSIGNED, bar_dec_scale DEC(10,2) UNSIGNED, bar_float FLOAT UNSIGNED, bar_float_prec FLOAT(10) UNSIGNED, bar_float_scale FLOAT(10,2) UNSIGNED, bar_double DOUBLE UNSIGNED, bar_double_prec DOUBLE(10) UNSIGNED, bar_double_scale DOUBLE(10,2) UNSIGNED, bar_real REAL UNSIGNED, bar_double_precision DOUBLE PRECISION UNSIGNED)";
        var createTable = Assert.IsType<Statement.CreateTable>(Mysql().VerifiedStmt(sql)).Value;
        var name = createTable.Name;
        var columns = createTable.Columns;
        Assert.Equal("foo", name.ToString());
        Assert.Equal(
            Sequence.Of(
                new ColumnDef(
                    Name: new Ident("bar_decimal"),
                    DataType: new DataType.DecimalUnsigned(new ExactNumberInfo.None()),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_decimal_prec"),
                    DataType: new DataType.DecimalUnsigned(new ExactNumberInfo.Precision(10)),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_decimal_scale"),
                    DataType: new DataType.DecimalUnsigned(new ExactNumberInfo.PrecisionAndScale(
                        10, 2)),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_dec"),
                    DataType: new DataType.DecUnsigned(new ExactNumberInfo.None()),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_dec_prec"),
                    DataType: new DataType.DecUnsigned(new ExactNumberInfo.Precision(10)),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_dec_scale"),
                    DataType: new DataType.DecUnsigned(new ExactNumberInfo.PrecisionAndScale(10, 2)),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_float"),
                    DataType: new DataType.FloatUnsigned(new ExactNumberInfo.None()),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_float_prec"),
                    DataType: new DataType.FloatUnsigned(new ExactNumberInfo.Precision(10)),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_float_scale"),
                    DataType: new DataType.FloatUnsigned(new ExactNumberInfo.PrecisionAndScale(
                        10, 2)),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_double"),
                    DataType: new DataType.DoubleUnsigned(new ExactNumberInfo.None()),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_double_prec"),
                    DataType: new DataType.DoubleUnsigned(new ExactNumberInfo.Precision(10)),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_double_scale"),
                    DataType: new DataType.DoubleUnsigned(new ExactNumberInfo.PrecisionAndScale(
                        10, 2)),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_real"),
                    DataType: new DataType.RealUnsigned(),
                    Options: Sequence<ColumnOptionDef>.Empty),
                new ColumnDef(
                    Name: new Ident("bar_double_precision"),
                    DataType: new DataType.DoublePrecisionUnsigned(),
                    Options: Sequence<ColumnOptionDef>.Empty)),
            columns);
    }

    [Fact]
    public void parse_simple_insert()
    {
        var sql = @"INSERT INTO tasks (title, priority) VALUES ('Test Some Inserts', 1), ('Test Entry 2', 2), ('Test Entry 3', 3)";

        var insert = Assert.IsType<Statement.Insert>(Mysql().VerifiedStmt(sql)).Value;
        var tableName = insert.Table;
        var columns = insert.Columns;
        var source = insert.Source;
        var on = insert.On;
        Assert.Equal(
            new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("tasks")))),
            tableName);
        Assert.Equal(
            Sequence.Of(
                ObjectName.From(new Ident("title")),
                ObjectName.From(new Ident("priority"))),
            columns);
        Assert.Null(on);
        Assert.Equal(
            new Query(
                With: null,
                Body: new SetExpr.Values(new Values(
                    ValueKeyword: false,
                    ExplicitRow: false,
                    Rows: Sequence.Of(
                        Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                            new Expr.Value(
                                new Value.SingleQuotedString("Test Some Inserts")
                                    .WithEmptySpan()),
                            Expr.MakeValue(TestUtils.Number("1")))),
                        Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                            new Expr.Value(
                                new Value.SingleQuotedString("Test Entry 2")
                                    .WithEmptySpan()),
                            Expr.MakeValue(TestUtils.Number("2")))),
                        Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                            new Expr.Value(
                                new Value.SingleQuotedString("Test Entry 3")
                                    .WithEmptySpan()),
                            Expr.MakeValue(TestUtils.Number("3"))))))),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty),
            source);
    }

    [Fact]
    public void parse_ignore_insert()
    {
        var sql = @"INSERT IGNORE INTO tasks (title, priority) VALUES ('Test Some Inserts', 1)";

        var insert = Assert.IsType<Statement.Insert>(MysqlAndGeneric().VerifiedStmt(sql)).Value;
        var tableName = insert.Table;
        var columns = insert.Columns;
        var source = insert.Source;
        var on = insert.On;
        var ignore = insert.Ignore;
        Assert.Equal(
            new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("tasks")))),
            tableName);
        Assert.Equal(
            Sequence.Of(
                ObjectName.From(new Ident("title")),
                ObjectName.From(new Ident("priority"))),
            columns);
        Assert.Null(on);
        Assert.True(ignore);
        Assert.Equal(
            new Query(
                With: null,
                Body: new SetExpr.Values(new Values(
                    ValueKeyword: false,
                    ExplicitRow: false,
                    Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                        new Expr.Value(
                            new Value.SingleQuotedString("Test Some Inserts")
                                .WithEmptySpan()),
                        Expr.MakeValue(TestUtils.Number("1"))))))),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty),
            source);
    }

    [Fact]
    public void parse_priority_insert()
    {
        var sql = @"INSERT HIGH_PRIORITY INTO tasks (title, priority) VALUES ('Test Some Inserts', 1)";

        {
            var insert = Assert.IsType<Statement.Insert>(MysqlAndGeneric().VerifiedStmt(sql)).Value;
            var tableName = insert.Table;
            var columns = insert.Columns;
            var source = insert.Source;
            var on = insert.On;
            var priority = insert.Priority;
            Assert.Equal(
                new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("tasks")))),
                tableName);
            Assert.Equal(
                Sequence.Of(
                    ObjectName.From(new Ident("title")),
                    ObjectName.From(new Ident("priority"))),
                columns);
            Assert.Null(on);
            Assert.Equal(priority, new MysqlInsertPriority.HighPriority());
            Assert.Equal(
                new Query(
                    With: null,
                    Body: new SetExpr.Values(new Values(
                        ValueKeyword: false,
                        ExplicitRow: false,
                        Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                            new Expr.Value(
                                new Value.SingleQuotedString("Test Some Inserts")
                                    .WithEmptySpan()),
                            Expr.MakeValue(TestUtils.Number("1"))))))),
                    OrderBy: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty),
                source);
        }

        var sql2 = @"INSERT LOW_PRIORITY INTO tasks (title, priority) VALUES ('Test Some Inserts', 1)";

        {
            var insert = Assert.IsType<Statement.Insert>(Mysql().VerifiedStmt(sql2)).Value;
            var tableName = insert.Table;
            var columns = insert.Columns;
            var source = insert.Source;
            var on = insert.On;
            var priority = insert.Priority;
            Assert.Equal(
                new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("tasks")))),
                tableName);
            Assert.Equal(
                Sequence.Of(
                    ObjectName.From(new Ident("title")),
                    ObjectName.From(new Ident("priority"))),
                columns);
            Assert.Null(on);
            Assert.Equal(priority, new MysqlInsertPriority.LowPriority());
            Assert.Equal(
                new Query(
                    With: null,
                    Body: new SetExpr.Values(new Values(
                        ValueKeyword: false,
                        ExplicitRow: false,
                        Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                            new Expr.Value(
                                new Value.SingleQuotedString("Test Some Inserts")
                                    .WithEmptySpan()),
                            Expr.MakeValue(TestUtils.Number("1"))))))),
                    OrderBy: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty),
                source);
        }
    }

    [Fact]
    public void parse_insert_as()
    {
        var sql = @"INSERT INTO `table` (`date`) VALUES ('2024-01-01') AS `alias`";
        {
            var insert = Assert.IsType<Statement.Insert>(MysqlAndGeneric().VerifiedStmt(sql)).Value;
            var tableName = insert.Table;
            var columns = insert.Columns;
            var source = insert.Source;
            Assert.Equal(
                new TableObject.TableName(ObjectName.From(Sequence.Of(Ident.WithQuote('`', "table")))),
                tableName);
            Assert.Equal(
                Sequence.Of(ObjectName.From(Ident.WithQuote('`', "date"))),
                columns);
            var insertAlias = insert.InsertAlias!;

            Assert.Equal(
                ObjectName.From(Sequence.Of(Ident.WithQuote('`', "alias"))),
                insertAlias.RowAlias);
            Assert.Equal((Sequence<Ident>?)Sequence<Ident>.Empty, insertAlias.ColAliases);
            Assert.Equal(
                new Query(
                    With: null,
                    Body: new SetExpr.Values(new Values(
                        ValueKeyword: false,
                        ExplicitRow: false,
                        Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(new Expr.Value(
                            new Value.SingleQuotedString("2024-01-01").WithEmptySpan())))))),
                    OrderBy: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty),
                source);
        }

        sql = @"INSERT INTO `table` (`date`) VALUES ('2024-01-01') AS `alias` ()";
        var ex = Assert.Throws<ParserException>(() => MysqlAndGeneric().ParseSqlStatements(sql));
        Assert.IsType<ParserError.ParserErrorValue>(ex.Error);

        sql = @"INSERT INTO `table` (`id`, `date`) VALUES (1, '2024-01-01') AS `alias` (`mek_id`, `mek_date`)";
        {
            var insert = Assert.IsType<Statement.Insert>(MysqlAndGeneric().VerifiedStmt(sql)).Value;
            var tableName = insert.Table;
            var columns = insert.Columns;
            var source = insert.Source;
            Assert.Equal(
                new TableObject.TableName(ObjectName.From(Sequence.Of(Ident.WithQuote('`', "table")))),
                tableName);
            Assert.Equal(
                Sequence.Of(
                    ObjectName.From(Ident.WithQuote('`', "id")),
                    ObjectName.From(Ident.WithQuote('`', "date"))),
                columns);
            var insertAlias = insert.InsertAlias!;
            Assert.Equal(
                ObjectName.From(Sequence.Of(Ident.WithQuote('`', "alias"))),
                insertAlias.RowAlias);
            Assert.Equal(
                (Sequence<Ident>?)Sequence.Of(
                    Ident.WithQuote('`', "mek_id"),
                    Ident.WithQuote('`', "mek_date")),
                insertAlias.ColAliases);
            Assert.Equal(
                new Query(
                    With: null,
                    Body: new SetExpr.Values(new Values(
                        ValueKeyword: false,
                        ExplicitRow: false,
                        Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                            Expr.MakeValue(TestUtils.Number("1")),
                            new Expr.Value(
                                new Value.SingleQuotedString("2024-01-01")
                                    .WithEmptySpan())))))),
                    OrderBy: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty),
                source);
        }
    }

    [Fact]
    public void parse_replace_insert()
    {
        var sql = @"REPLACE DELAYED INTO tasks (title, priority) VALUES ('Test Some Inserts', 1)";
        var insert = Assert.IsType<Statement.Insert>(Mysql().VerifiedStmt(sql)).Value;
        var tableName = insert.Table;
        var columns = insert.Columns;
        var source = insert.Source;
        var on = insert.On;
        var replaceInto = insert.ReplaceInto;
        var priority = insert.Priority;
        Assert.Equal(
            new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("tasks")))),
            tableName);
        Assert.Equal(
            Sequence.Of(
                ObjectName.From(new Ident("title")),
                ObjectName.From(new Ident("priority"))),
            columns);
        Assert.Null(on);
        Assert.True(replaceInto);
        Assert.Equal(priority, new MysqlInsertPriority.Delayed());
        Assert.Equal(
            new Query(
                With: null,
                Body: new SetExpr.Values(new Values(
                    ValueKeyword: false,
                    ExplicitRow: false,
                    Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                        new Expr.Value(
                            new Value.SingleQuotedString("Test Some Inserts")
                                .WithEmptySpan()),
                        Expr.MakeValue(TestUtils.Number("1"))))))),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty),
            source);
    }

    [Fact]
    public void parse_empty_row_insert()
    {
        var sql = "INSERT INTO tb () VALUES (), ()";

        var insert = Assert.IsType<Statement.Insert>(Mysql().OneStatementParsesTo(sql, "INSERT INTO tb VALUES (), ()")).Value;
        var tableName = insert.Table;
        var columns = insert.Columns;
        var source = insert.Source;
        var on = insert.On;
        Assert.Equal(
            new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("tb")))),
            tableName);
        Assert.True(columns.IsEmpty);
        Assert.Null(on);
        Assert.Equal(
            new Query(
                With: null,
                Body: new SetExpr.Values(new Values(
                    ValueKeyword: false,
                    ExplicitRow: false,
                    Rows: Sequence.Of(
                        Parens<Sequence<Expr>>.WithEmptySpan(Sequence<Expr>.Empty),
                        Parens<Sequence<Expr>>.WithEmptySpan(Sequence<Expr>.Empty)))),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty),
            source);
    }

    [Fact]
    public void parse_insert_with_on_duplicate_update()
    {
        var sql = "INSERT INTO permission_groups (name, description, perm_create, perm_read, perm_update, perm_delete) VALUES ('accounting_manager', 'Some description about the group', true, true, true, true) ON DUPLICATE KEY UPDATE description = VALUES(description), perm_create = VALUES(perm_create), perm_read = VALUES(perm_read), perm_update = VALUES(perm_update), perm_delete = VALUES(perm_delete)";

        var insert = Assert.IsType<Statement.Insert>(Mysql().VerifiedStmt(sql)).Value;
        var tableName = insert.Table;
        var columns = insert.Columns;
        var source = insert.Source;
        var on = insert.On;
        Assert.Equal(
            new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("permission_groups")))),
            tableName);
        Assert.Equal(
            Sequence.Of(
                ObjectName.From(new Ident("name")),
                ObjectName.From(new Ident("description")),
                ObjectName.From(new Ident("perm_create")),
                ObjectName.From(new Ident("perm_read")),
                ObjectName.From(new Ident("perm_update")),
                ObjectName.From(new Ident("perm_delete"))),
            columns);
        Assert.Equal(
            new Query(
                With: null,
                Body: new SetExpr.Values(new Values(
                    ValueKeyword: false,
                    ExplicitRow: false,
                    Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                        new Expr.Value(
                            new Value.SingleQuotedString("accounting_manager")
                                .WithEmptySpan()),
                        new Expr.Value(
                            new Value.SingleQuotedString(
                                "Some description about the group")
                            .WithEmptySpan()),
                        new Expr.Value(new Value.Boolean(true).WithEmptySpan()),
                        new Expr.Value(new Value.Boolean(true).WithEmptySpan()),
                        new Expr.Value(new Value.Boolean(true).WithEmptySpan()),
                        new Expr.Value(new Value.Boolean(true).WithEmptySpan())))))),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty),
            source);
        Assert.Equal(
            new OnInsert.DuplicateKeyUpdate(Sequence.Of(
                new Assignment(
                    Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident(
                        "description")))),
                    Value: TestUtils.Call("VALUES", new Expr[] { new Expr.Identifier(new Ident("description")) })),
                new Assignment(
                    Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident(
                        "perm_create")))),
                    Value: TestUtils.Call("VALUES", new Expr[] { new Expr.Identifier(new Ident("perm_create")) })),
                new Assignment(
                    Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident(
                        "perm_read")))),
                    Value: TestUtils.Call("VALUES", new Expr[] { new Expr.Identifier(new Ident("perm_read")) })),
                new Assignment(
                    Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident(
                        "perm_update")))),
                    Value: TestUtils.Call("VALUES", new Expr[] { new Expr.Identifier(new Ident("perm_update")) })),
                new Assignment(
                    Target: new AssignmentTarget.ColumnName(ObjectName.From(Sequence.Of(new Ident(
                        "perm_delete")))),
                    Value: TestUtils.Call("VALUES", new Expr[] { new Expr.Identifier(new Ident("perm_delete")) })))),
            on);
    }

    [Fact]
    public void parse_select_with_numeric_prefix_column_name()
    {
        var sql = "SELECT 123col_$@123abc FROM \"table\"";
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
                Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident(
                    "123col_$@123abc")))),
                Exclude: null,
                Into: null,
                From: Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(Ident.WithQuote(
                        '"', "table")))),
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
    public void parse_qualified_identifiers_with_numeric_prefix()
    {
        // Case 1: Qualified column name that starts with digits.
        switch (Mysql().VerifiedStmt("SELECT t.15to29 FROM my_table AS t"))
        {
            case Statement.Query { Value: var q }:
                switch (q.Body)
                {
                    case SetExpr.Select { Value: var s }:
                        switch (s.Projection.Last)
                        {
                            case SelectItem.UnnamedExpr { Value: Expr.CompoundIdentifier { Value: var parts } }:
                                Assert.Equal(Sequence.Of(new Ident("t"), new Ident("15to29")), parts);
                                break;
                            case var proj:
                                Assert.Fail($"Unexpected projection: {proj}");
                                break;
                        }
                        break;
                    case var body:
                        Assert.Fail($"Unexpected statement body: {body}");
                        break;
                }
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }

        // Case 2: Qualified column name that starts with digits and on its own represents a number.
        switch (Mysql().VerifiedStmt("SELECT t.15e29 FROM my_table AS t"))
        {
            case Statement.Query { Value: var q }:
                switch (q.Body)
                {
                    case SetExpr.Select { Value: var s }:
                        switch (s.Projection.Last)
                        {
                            case SelectItem.UnnamedExpr { Value: Expr.CompoundIdentifier { Value: var parts } }:
                                Assert.Equal(Sequence.Of(new Ident("t"), new Ident("15e29")), parts);
                                break;
                            case var proj:
                                Assert.Fail($"Unexpected projection: {proj}");
                                break;
                        }
                        break;
                    case var body:
                        Assert.Fail($"Unexpected statement body: {body}");
                        break;
                }
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }

        // Case 3: Unqualified, the same token is parsed as a number.
        switch (Mysql()
            .ParseSqlStatements("SELECT 15e29 FROM my_table")
            .Last)
        {
            case Statement.Query { Value: var q }:
                switch (q.Body)
                {
                    case SetExpr.Select { Value: var s }:
                        switch (s.Projection.Last)
                        {
                            case SelectItem.UnnamedExpr { Value: Expr.Value { Inner: { Value: var value } } }:
                                Assert.Equal(TestUtils.Number("15e29"), value);
                                break;
                            case var proj:
                                Assert.Fail($"Unexpected projection: {proj}");
                                break;
                        }
                        break;
                    case var body:
                        Assert.Fail($"Unexpected statement body: {body}");
                        break;
                }
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }

        // Case 4: Quoted simple identifier.
        switch (Mysql().VerifiedStmt("SELECT `15e29` FROM my_table"))
        {
            case Statement.Query { Value: var q }:
                switch (q.Body)
                {
                    case SetExpr.Select { Value: var s }:
                        switch (s.Projection.Last)
                        {
                            case SelectItem.UnnamedExpr { Value: Expr.Identifier { Value: var name } }:
                                Assert.Equal(Ident.WithQuote('`', "15e29"), name);
                                break;
                            case var proj:
                                Assert.Fail($"Unexpected projection: {proj}");
                                break;
                        }
                        break;
                    case var body:
                        Assert.Fail($"Unexpected statement body: {body}");
                        break;
                }
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }

        // Case 5: Quoted compound identifier.
        switch (Mysql().VerifiedStmt("SELECT t.`15e29` FROM my_table AS t"))
        {
            case Statement.Query { Value: var q }:
                switch (q.Body)
                {
                    case SetExpr.Select { Value: var s }:
                        switch (s.Projection.Last)
                        {
                            case SelectItem.UnnamedExpr { Value: Expr.CompoundIdentifier { Value: var parts } }:
                                Assert.Equal(
                                    Sequence.Of(new Ident("t"), Ident.WithQuote('`', "15e29")),
                                    parts);
                                break;
                            case var proj:
                                Assert.Fail($"Unexpected projection: {proj}");
                                break;
                        }
                        break;
                    case var body:
                        Assert.Fail($"Unexpected statement body: {body}");
                        break;
                }
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }

        // Case 6: Multi-level compound identifiers.
        switch (Mysql().VerifiedStmt("SELECT 1db.1table.1column"))
        {
            case Statement.Query { Value: var q }:
                switch (q.Body)
                {
                    case SetExpr.Select { Value: var s }:
                        switch (s.Projection.Last)
                        {
                            case SelectItem.UnnamedExpr { Value: Expr.CompoundIdentifier { Value: var parts } }:
                                Assert.Equal(
                                    Sequence.Of(
                                        new Ident("1db"),
                                        new Ident("1table"),
                                        new Ident("1column")),
                                    parts);
                                break;
                            case var proj:
                                Assert.Fail($"Unexpected projection: {proj}");
                                break;
                        }
                        break;
                    case var body:
                        Assert.Fail($"Unexpected statement body: {body}");
                        break;
                }
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }

        // Case 7: Multi-level compound quoted identifiers.
        switch (Mysql().VerifiedStmt("SELECT `1`.`2`.`3`"))
        {
            case Statement.Query { Value: var q }:
                switch (q.Body)
                {
                    case SetExpr.Select { Value: var s }:
                        switch (s.Projection.Last)
                        {
                            case SelectItem.UnnamedExpr { Value: Expr.CompoundIdentifier { Value: var parts } }:
                                Assert.Equal(
                                    Sequence.Of(
                                        Ident.WithQuote('`', "1"),
                                        Ident.WithQuote('`', "2"),
                                        Ident.WithQuote('`', "3")),
                                    parts);
                                break;
                            case var proj:
                                Assert.Fail($"Unexpected projection: {proj}");
                                break;
                        }
                        break;
                    case var body:
                        Assert.Fail($"Unexpected statement body: {body}");
                        break;
                }
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }
    }
}
