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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_common.rs (linhas 4320–8209).
// Modificado: tradução de Rust para C#.

using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C2.md):
// - Cada fatia de tests/sqlparser_common.rs é uma classe própria (não partial); as funções-atalho do
//   arquivo Rust (`verified_stmt`, `parse_sql_statements`...) e a macro `nest!` (tests/test_utils/mod.rs)
//   estão copiadas aqui como métodos `private static`.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests/SpansTests).
// - `unreachable!()` → `throw new InvalidOperationException(Unreachable)` (mensagem do Rust);
//   `panic!(msg)` → `Assert.Fail(msg)` (como em SpansTests).
// - `fn` aninhada e closures dentro de um teste → função local `static` com o nome mecânico.
// - Strings Rust com `\` no fim da linha (continuação: remove a quebra e a indentação seguinte) viram
//   concatenações sem a quebra; quebras de linha reais viram `\n` explícito (o .editorconfig usa CRLF,
//   então raw string literals com várias linhas mudariam o conteúdo).
public class SqlParserCommonTests2
{
    // Mensagem do `unreachable!()` do Rust.
    private const string Unreachable = "internal error: entered unreachable code";

    [Fact]
    public void parse_create_table_with_constraint_characteristics()
    {
        var sql = "CREATE TABLE uk_cities ("
            + "name VARCHAR(100) NOT NULL,"
            + "lat DOUBLE NULL,"
            + "lng DOUBLE,\n"
            + "               constraint fkey foreign key (lat) references othertable3 (lat) on delete restrict deferrable initially deferred,"
            + "constraint fkey2 foreign key (lat) references othertable4(lat) on delete no action on update restrict deferrable initially immediate, "
            + "foreign key (lat) references othertable4(lat) on update set default on delete cascade not deferrable initially deferred not enforced, "
            + "FOREIGN KEY (lng) REFERENCES othertable4 (longitude) ON UPDATE SET NULL enforced not deferrable initially immediate\n"
            + "               )";
        var ast = OneStatementParsesTo(
            sql,
            "CREATE TABLE uk_cities ("
            + "name VARCHAR(100) NOT NULL, "
            + "lat DOUBLE NULL, "
            + "lng DOUBLE, "
            + "CONSTRAINT fkey FOREIGN KEY (lat) REFERENCES othertable3(lat) ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED, "
            + "CONSTRAINT fkey2 FOREIGN KEY (lat) REFERENCES othertable4(lat) ON DELETE NO ACTION ON UPDATE RESTRICT DEFERRABLE INITIALLY IMMEDIATE, "
            + "FOREIGN KEY (lat) REFERENCES othertable4(lat) ON DELETE CASCADE ON UPDATE SET DEFAULT NOT DEFERRABLE INITIALLY DEFERRED NOT ENFORCED, "
            + "FOREIGN KEY (lng) REFERENCES othertable4(longitude) ON UPDATE SET NULL NOT DEFERRABLE INITIALLY IMMEDIATE ENFORCED)");
        switch (ast)
        {
            case Statement.CreateTable { Value: { IfNotExists: false, External: false, FileFormat: null, Location: null } createTable }:
                Assert.Equal("uk_cities", createTable.Name.ToString());
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("name"),
                            DataType: new DataType.Varchar(new CharacterLength.IntegerLength(Length: 100, Unit: null)),
                            Options: Sequence.Of(new ColumnOptionDef(Name: null, Option: new ColumnOption.NotNull()))),
                        new ColumnDef(
                            Name: new Ident("lat"),
                            DataType: new DataType.Double(new ExactNumberInfo.None()),
                            Options: Sequence.Of(new ColumnOptionDef(Name: null, Option: new ColumnOption.Null()))),
                        new ColumnDef(
                            Name: new Ident("lng"),
                            DataType: new DataType.Double(new ExactNumberInfo.None()),
                            Options: Sequence<ColumnOptionDef>.Empty)));
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
                            Characteristics: new ConstraintCharacteristics(
                                Deferrable: true,
                                Initially: DeferrableInitial.Deferred,
                                Enforced: null))),
                        TableConstraint.From(new ForeignKeyConstraint(
                            Name: new Ident("fkey2"),
                            IndexName: null,
                            Columns: Sequence.Of(new Ident("lat")),
                            ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable4"))),
                            ReferredColumns: Sequence.Of(new Ident("lat")),
                            OnDelete: new ReferentialAction.NoAction(),
                            OnUpdate: new ReferentialAction.Restrict(),
                            MatchKind: null,
                            Characteristics: new ConstraintCharacteristics(
                                Deferrable: true,
                                Initially: DeferrableInitial.Immediate,
                                Enforced: null))),
                        TableConstraint.From(new ForeignKeyConstraint(
                            Name: null,
                            IndexName: null,
                            Columns: Sequence.Of(new Ident("lat")),
                            ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable4"))),
                            ReferredColumns: Sequence.Of(new Ident("lat")),
                            OnDelete: new ReferentialAction.Cascade(),
                            OnUpdate: new ReferentialAction.SetDefault(),
                            MatchKind: null,
                            Characteristics: new ConstraintCharacteristics(
                                Deferrable: false,
                                Initially: DeferrableInitial.Deferred,
                                Enforced: false))),
                        TableConstraint.From(new ForeignKeyConstraint(
                            Name: null,
                            IndexName: null,
                            Columns: Sequence.Of(new Ident("lng")),
                            ForeignTable: ObjectName.From(Sequence.Of(new Ident("othertable4"))),
                            ReferredColumns: Sequence.Of(new Ident("longitude")),
                            OnDelete: null,
                            OnUpdate: new ReferentialAction.SetNull(),
                            MatchKind: null,
                            Characteristics: new ConstraintCharacteristics(
                                Deferrable: false,
                                Initially: DeferrableInitial.Immediate,
                                Enforced: true)))));
                Assert.Equal(createTable.TableOptions, new CreateTableOptions.None());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        var res = Assert.Throws<ParserException>(() => ParseSqlStatements("CREATE TABLE t (\n"
            + "        a int NOT NULL,\n"
            + "         FOREIGN KEY (a) REFERENCES othertable4(a) ON DELETE CASCADE ON UPDATE SET DEFAULT DEFERRABLE INITIALLY IMMEDIATE NOT DEFERRABLE, "
            + ")"));
        Assert.Contains("Expected: ',' or ')' after column definition, found: NOT", res.Error.ToString());

        res = Assert.Throws<ParserException>(() => ParseSqlStatements("CREATE TABLE t (\n"
            + "        a int NOT NULL,\n"
            + "         FOREIGN KEY (a) REFERENCES othertable4(a) ON DELETE CASCADE ON UPDATE SET DEFAULT NOT ENFORCED INITIALLY DEFERRED ENFORCED, "
            + ")"));
        Assert.Contains("Expected: ',' or ')' after column definition, found: ENFORCED", res.Error.ToString());

        res = Assert.Throws<ParserException>(() => ParseSqlStatements("CREATE TABLE t (\n"
            + "        a int NOT NULL,\n"
            + "         FOREIGN KEY (lat) REFERENCES othertable4(lat) ON DELETE CASCADE ON UPDATE SET DEFAULT INITIALLY DEFERRED INITIALLY IMMEDIATE, "
            + ")"));
        Assert.Contains("Expected: ',' or ')' after column definition, found: INITIALLY", res.Error.ToString());
    }

    [Fact]
    public void parse_create_table_column_constraint_characteristics()
    {
        static void TestCombo(string syntax, bool? deferrable, DeferrableInitial? initially, bool? enforced)
        {
            var message = syntax.Length == 0 ? "No clause" : syntax;

            var sql = $"CREATE TABLE t (a int UNIQUE {syntax})";
            var expectedClause = syntax.Length == 0 ? "" : $" {syntax}";
            var expected = $"CREATE TABLE t (a INT UNIQUE{expectedClause})";
            var ast = OneStatementParsesTo(sql, expected);

            ConstraintCharacteristics? expectedValue =
                deferrable is not null || initially is not null || enforced is not null
                    ? new ConstraintCharacteristics(Deferrable: deferrable, Initially: initially, Enforced: enforced)
                    : null;

            switch (ast)
            {
                case Statement.CreateTable { Value: var createTable }:
                    // assert_eq!(columns, vec![..], "{message}")
                    try
                    {
                        Assert.Equal(
                            createTable.Columns,
                            Sequence.Of(new ColumnDef(
                                Name: new Ident("a"),
                                DataType: new DataType.Int(null),
                                Options: Sequence.Of(new ColumnOptionDef(
                                    Name: null,
                                    Option: new ColumnOption.Unique(new UniqueConstraint(
                                        Name: null,
                                        IndexName: null,
                                        IndexTypeDisplay: new KeyOrIndexDisplay.None(),
                                        IndexType: null,
                                        Columns: Sequence<IndexColumn>.Empty,
                                        Include: Sequence<Ident>.Empty,
                                        IndexOptions: Sequence<IndexOption>.Empty,
                                        Characteristics: expectedValue,
                                        NullsDistinct: new NullsDistinctOption.None())))))));
                    }
                    catch (Xunit.Sdk.XunitException e)
                    {
                        Assert.Fail($"{message}{Environment.NewLine}{e.Message}");
                    }
                    break;
                default:
                    throw new InvalidOperationException(Unreachable);
            }
        }

        foreach (var deferrable in new bool?[] { null, true, false })
        {
            foreach (var initially in new DeferrableInitial?[] { null, DeferrableInitial.Immediate, DeferrableInitial.Deferred })
            {
                foreach (var enforced in new bool?[] { null, true, false })
                {
                    var deferrableText = deferrable is { } d ? (d ? "DEFERRABLE" : "NOT DEFERRABLE") : null;
                    var initiallyText = initially is { } i
                        ? i switch
                        {
                            DeferrableInitial.Immediate => "INITIALLY IMMEDIATE",
                            DeferrableInitial.Deferred => "INITIALLY DEFERRED",
                            _ => throw new InvalidOperationException(Unreachable),
                        }
                        : null;
                    var enforcedText = enforced is { } en ? (en ? "ENFORCED" : "NOT ENFORCED") : null;

                    var syntax = string.Join(" ", new[] { deferrableText, initiallyText, enforcedText }.OfType<string>());

                    TestCombo(syntax, deferrable, initially, enforced);
                }
            }
        }

        var res = Assert.Throws<ParserException>(() => ParseSqlStatements(
            "CREATE TABLE t (a int NOT NULL UNIQUE DEFERRABLE INITIALLY BADVALUE)"));
        Assert.Contains("Expected: one of DEFERRED or IMMEDIATE, found: BADVALUE", res.Error.ToString());

        // res.expect_err("INITIALLY {IMMEDIATE|DEFERRED} setting should only be allowed once")
        Assert.Throws<ParserException>(() => ParseSqlStatements(
            "CREATE TABLE t (a int NOT NULL UNIQUE INITIALLY IMMEDIATE DEFERRABLE INITIALLY DEFERRED)"));

        // res.expect_err("[NOT] DEFERRABLE setting should only be allowed once")
        Assert.Throws<ParserException>(() => ParseSqlStatements(
            "CREATE TABLE t (a int NOT NULL UNIQUE DEFERRABLE INITIALLY DEFERRED NOT DEFERRABLE)"));

        // res.expect_err("[NOT] ENFORCED setting should only be allowed once")
        Assert.Throws<ParserException>(() => ParseSqlStatements(
            "CREATE TABLE t (a int NOT NULL UNIQUE DEFERRABLE INITIALLY DEFERRED ENFORCED NOT ENFORCED)"));
    }

    [Fact]
    public void parse_create_table_hive_array()
    {
        // Parsing [] type arrays does not work in MsSql since [ is used in is_delimited_identifier_start
        foreach (var (dialectList, angleBracketSyntax) in new (List<Dialect>, bool)[]
        {
            (new List<Dialect> { new PostgreSqlDialect() }, false),
            (new List<Dialect> { new HiveDialect(), new BigQueryDialect() }, true),
        })
        {
            var dialects = new TestedDialects(dialectList);

            var sql = $"CREATE TABLE IF NOT EXISTS something (name INT, val {(angleBracketSyntax ? "ARRAY<INT>" : "INT[]")})";

            DataType expectedElem = new DataType.Int(null);
            ArrayElemTypeDef expected = angleBracketSyntax
                ? new ArrayElemTypeDef.AngleBracket(expectedElem)
                : new ArrayElemTypeDef.SquareBracket(expectedElem, null);

            switch (dialects.OneStatementParsesTo(sql, sql))
            {
                case Statement.CreateTable { Value: var createTable }:
                    Assert.True(createTable.IfNotExists);
                    Assert.Equal(createTable.Name, ObjectName.From(Sequence.Of(new Ident("something"))));
                    Assert.Equal(
                        createTable.Columns,
                        Sequence.Of(
                            new ColumnDef(
                                Name: new Ident("name"),
                                DataType: new DataType.Int(null),
                                Options: Sequence<ColumnOptionDef>.Empty),
                            new ColumnDef(
                                Name: new Ident("val"),
                                DataType: new DataType.Array(expected),
                                Options: Sequence<ColumnOptionDef>.Empty)));
                    break;
                default:
                    throw new InvalidOperationException(Unreachable);
            }
        }

        // SnowflakeDialect using array different
        var snowflakeLikeDialects = new TestedDialects(new List<Dialect>
        {
            new PostgreSqlDialect(),
            new HiveDialect(),
            new MySqlDialect(),
        });
        var invalidSql = "CREATE TABLE IF NOT EXISTS something (name int, val array<int)";

        Assert.Equal(
            Assert.Throws<ParserException>(() => snowflakeLikeDialects.ParseSqlStatements(invalidSql)).Error,
            new ParserError.ParserErrorValue("Expected: >, found: )"));
    }

    [Fact]
    public void parse_create_table_with_multiple_on_delete_in_constraint_fails()
    {
        // .expect_err("should have failed")
        Assert.Throws<ParserException>(() => ParseSqlStatements(
            ""
            + "create table X ("
            + "y_id int, "
            + "foreign key (y_id) references Y (id) on delete cascade on update cascade on delete no action"
            + ")"));
    }

    [Fact]
    public void parse_create_table_with_multiple_on_delete_fails()
    {
        // .expect_err("should have failed")
        Assert.Throws<ParserException>(() => ParseSqlStatements(
            ""
            + "create table X ("
            + "y_id int references Y (id) "
            + "on delete cascade on update cascade on delete no action"
            + ")"));
    }

    [Fact]
    public void parse_assert()
    {
        var sql = "ASSERT (SELECT COUNT(*) FROM my_table) > 0";
        var ast = OneStatementParsesTo(sql, "ASSERT (SELECT COUNT(*) FROM my_table) > 0");
        switch (ast)
        {
            case Statement.Assert { Message: var message }:
                Assert.Null(message);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_assert_message()
    {
        var sql = "ASSERT (SELECT COUNT(*) FROM my_table) > 0 AS 'No rows in my_table'";
        var ast = OneStatementParsesTo(
            sql,
            "ASSERT (SELECT COUNT(*) FROM my_table) > 0 AS 'No rows in my_table'");
        switch (ast)
        {
            case Statement.Assert { Message: { } message }:
                switch (message)
                {
                    case Expr.Value { Inner: { Value: Value.SingleQuotedString { Value: var s } } }:
                        Assert.Equal("No rows in my_table", s);
                        break;
                    default:
                        throw new InvalidOperationException(Unreachable);
                }
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_schema()
    {
        var sql = "CREATE SCHEMA X";

        switch (VerifiedStmt(sql))
        {
            case Statement.CreateSchema { SchemaName: var schemaName }:
                Assert.Equal("X", schemaName.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        VerifiedStmt("CREATE SCHEMA a.b.c OPTIONS(key1 = 'value1', key2 = 'value2')");
        VerifiedStmt("CREATE SCHEMA IF NOT EXISTS a OPTIONS(key1 = 'value1')");
        VerifiedStmt("CREATE SCHEMA IF NOT EXISTS a OPTIONS()");
        VerifiedStmt("CREATE SCHEMA IF NOT EXISTS a DEFAULT COLLATE 'und:ci' OPTIONS()");
        VerifiedStmt("CREATE SCHEMA a.b.c WITH (key1 = 'value1', key2 = 'value2')");
        VerifiedStmt("CREATE SCHEMA IF NOT EXISTS a WITH (key1 = 'value1')");
        VerifiedStmt("CREATE SCHEMA IF NOT EXISTS a WITH ()");
        VerifiedStmt("CREATE SCHEMA a CLONE b");
    }

    [Fact]
    public void parse_create_or_replace_schema()
    {
        switch (VerifiedStmt("CREATE OR REPLACE SCHEMA X"))
        {
            case Statement.CreateSchema createSchema:
                Assert.Equal("X", createSchema.SchemaName.ToString());
                Assert.True(createSchema.OrReplace);
                Assert.False(createSchema.IfNotExists);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        VerifiedStmt("CREATE OR REPLACE SCHEMA IF NOT EXISTS X");
    }

    [Fact]
    public void parse_create_schema_with_authorization()
    {
        var sql = "CREATE SCHEMA AUTHORIZATION Y";

        switch (VerifiedStmt(sql))
        {
            case Statement.CreateSchema { SchemaName: var schemaName }:
                Assert.Equal("AUTHORIZATION Y", schemaName.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_schema_with_name_and_authorization()
    {
        var sql = "CREATE SCHEMA X AUTHORIZATION Y";

        switch (VerifiedStmt(sql))
        {
            case Statement.CreateSchema { SchemaName: var schemaName }:
                Assert.Equal("X AUTHORIZATION Y", schemaName.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_drop_schema()
    {
        var sql = "DROP SCHEMA X";

        switch (VerifiedStmt(sql))
        {
            case Statement.Drop { ObjectType: var objectType }:
                Assert.Equal(objectType, new ObjectType.Schema());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_table_as()
    {
        var sql = "CREATE TABLE t AS SELECT * FROM a";

        switch (VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("t", createTable.Name.ToString());
                Assert.Equal(createTable.Query, VerifiedQuery("SELECT * FROM a"));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        // BigQuery allows specifying table schema in CTAS
        // ANSI SQL and PostgreSQL let you only specify the list of columns
        // (without data types) in a CTAS, but we have yet to support that.
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsCreateTableMultiSchemaInfoSources);
        var sqlWithSchema = "CREATE TABLE t (a INT, b INT) AS SELECT 1 AS b, 2 AS a";
        switch (dialects.VerifiedStmt(sqlWithSchema))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(2, createTable.Columns.Count);
                Assert.Equal("a INT", createTable.Columns[0].ToString());
                Assert.Equal("b INT", createTable.Columns[1].ToString());
                Assert.Equal(
                    createTable.Query,
                    VerifiedQuery("SELECT 1 AS b, 2 AS a"));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_table_as_table()
    {
        var sql1 = "CREATE TABLE new_table AS TABLE old_table";

        var expectedQuery1 = new Query(
            With: null,
            Body: new SetExpr.Table(new Table(TableName: new Ident("old_table"), SchemaName: null)),
            OrderBy: null,
            LimitClause: null,
            Fetch: null,
            Locks: Sequence<LockClause>.Empty,
            ForClause: null,
            Settings: null,
            FormatClause: null,
            PipeOperators: Sequence<PipeOperator>.Empty);

        switch (VerifiedStmt(sql1))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(createTable.Name, ObjectName.From(Sequence.Of(new Ident("new_table"))));
                Assert.Equal(createTable.Query, expectedQuery1);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        var sql2 = "CREATE TABLE new_table AS TABLE schema_name.old_table";

        var expectedQuery2 = new Query(
            With: null,
            Body: new SetExpr.Table(new Table(TableName: new Ident("old_table"), SchemaName: new Ident("schema_name"))),
            OrderBy: null,
            LimitClause: null,
            Fetch: null,
            Locks: Sequence<LockClause>.Empty,
            ForClause: null,
            Settings: null,
            FormatClause: null,
            PipeOperators: Sequence<PipeOperator>.Empty);

        switch (VerifiedStmt(sql2))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(createTable.Name, ObjectName.From(Sequence.Of(new Ident("new_table"))));
                Assert.Equal(createTable.Query, expectedQuery2);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_table_on_cluster()
    {
        var generic = new TestedDialects(new List<Dialect> { new GenericDialect() });

        // Using single-quote literal to define current cluster
        var sql = "CREATE TABLE t ON CLUSTER '{cluster}' (a INT, b INT)";
        switch (generic.VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("'{cluster}'", createTable.OnCluster!.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        // Using explicitly declared cluster name
        sql = "CREATE TABLE t ON CLUSTER my_cluster (a INT, b INT)";
        switch (generic.VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("my_cluster", createTable.OnCluster!.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_or_replace_table()
    {
        var sql = "CREATE OR REPLACE TABLE t (a INT)";

        switch (VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("t", createTable.Name.ToString());
                Assert.True(createTable.OrReplace);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_table_with_on_delete_on_update_2in_any_order()
    {
        // -> Result<(), ParserError>: o `?` vira a própria exceção, que falha o teste.
        static string Sql(string options) => $"create table X (y_id int references Y (id) {options})";

        ParseSqlStatements(Sql("on update cascade on delete no action"));
        ParseSqlStatements(Sql("on delete cascade on update cascade"));
        ParseSqlStatements(Sql("on update no action"));
        ParseSqlStatements(Sql("on delete restrict"));
    }

    [Fact]
    public void parse_create_table_with_options()
    {
        var generic = new TestedDialects(new List<Dialect> { new GenericDialect() });

        var sql = "CREATE TABLE t (c INT) WITH (foo = 'bar', a = 123)";
        switch (generic.VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                var withOptions = createTable.TableOptions switch
                {
                    CreateTableOptions.With options => options.Value,
                    _ => throw new InvalidOperationException(Unreachable),
                };
                Assert.Equal(
                    Sequence.Of<SqlOption>(
                        new SqlOption.KeyValue(
                            Key: new Ident("foo"),
                            Value: new Expr.Value(new Value.SingleQuotedString("bar").WithEmptySpan())),
                        new SqlOption.KeyValue(
                            Key: new Ident("a"),
                            Value: Expr.MakeValue(TestUtils.Number("123")))),
                    withOptions);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_table_clone()
    {
        var sql = "CREATE OR REPLACE TABLE a CLONE a_tmp";
        switch (VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal(ObjectName.From(Sequence.Of(new Ident("a"))), createTable.Name);
                Assert.Equal(ObjectName.From(Sequence.Of(new Ident("a_tmp"))), createTable.CloneValue);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_table_trailing_comma()
    {
        var dialect = new TestedDialects(new List<Dialect> { new DuckDbDialect() });

        var sql = "CREATE TABLE foo (bar int,);";
        dialect.OneStatementParsesTo(sql, "CREATE TABLE foo (bar INT)");
    }

    [Fact]
    public void parse_create_external_table()
    {
        var sql = "CREATE EXTERNAL TABLE uk_cities ("
            + "name VARCHAR(100) NOT NULL,"
            + "lat DOUBLE NULL,"
            + "lng DOUBLE)"
            + "STORED AS TEXTFILE LOCATION '/tmp/example.csv'";
        var ast = OneStatementParsesTo(
            sql,
            "CREATE EXTERNAL TABLE uk_cities ("
            + "name VARCHAR(100) NOT NULL, "
            + "lat DOUBLE NULL, "
            + "lng DOUBLE) "
            + "STORED AS TEXTFILE LOCATION '/tmp/example.csv'");
        switch (ast)
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("uk_cities", createTable.Name.ToString());
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("name"),
                            DataType: new DataType.Varchar(new CharacterLength.IntegerLength(Length: 100, Unit: null)),
                            Options: Sequence.Of(new ColumnOptionDef(Name: null, Option: new ColumnOption.NotNull()))),
                        new ColumnDef(
                            Name: new Ident("lat"),
                            DataType: new DataType.Double(new ExactNumberInfo.None()),
                            Options: Sequence.Of(new ColumnOptionDef(Name: null, Option: new ColumnOption.Null()))),
                        new ColumnDef(
                            Name: new Ident("lng"),
                            DataType: new DataType.Double(new ExactNumberInfo.None()),
                            Options: Sequence<ColumnOptionDef>.Empty)));
                Assert.True(createTable.Constraints.IsEmpty);

                Assert.True(createTable.External);
                Assert.Equal(new FileFormat.TEXTFILE(), createTable.FileFormat);
                Assert.Equal("/tmp/example.csv", createTable.Location);

                Assert.Equal(createTable.TableOptions, new CreateTableOptions.None());
                Assert.False(createTable.IfNotExists);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_or_replace_external_table()
    {
        // Supported by at least Snowflake
        // https://docs.snowflake.com/en/sql-reference/sql/create-external-table.html
        var sql = "CREATE OR REPLACE EXTERNAL TABLE uk_cities ("
            + "name VARCHAR(100) NOT NULL)"
            + "STORED AS TEXTFILE LOCATION '/tmp/example.csv'";
        var ast = OneStatementParsesTo(
            sql,
            "CREATE OR REPLACE EXTERNAL TABLE uk_cities ("
            + "name VARCHAR(100) NOT NULL) "
            + "STORED AS TEXTFILE LOCATION '/tmp/example.csv'");
        switch (ast)
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("uk_cities", createTable.Name.ToString());
                Assert.Equal(
                    createTable.Columns,
                    Sequence.Of(new ColumnDef(
                        Name: new Ident("name"),
                        DataType: new DataType.Varchar(new CharacterLength.IntegerLength(Length: 100, Unit: null)),
                        Options: Sequence.Of(new ColumnOptionDef(Name: null, Option: new ColumnOption.NotNull())))));
                Assert.True(createTable.Constraints.IsEmpty);

                Assert.True(createTable.External);
                Assert.Equal(new FileFormat.TEXTFILE(), createTable.FileFormat);
                Assert.Equal("/tmp/example.csv", createTable.Location);

                Assert.Equal(createTable.TableOptions, new CreateTableOptions.None());
                Assert.False(createTable.IfNotExists);
                Assert.True(createTable.OrReplace);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_create_external_table_lowercase()
    {
        var sql = "create external table uk_cities ("
            + "name varchar(100) not null,"
            + "lat double null,"
            + "lng double)"
            + "stored as parquet location '/tmp/example.csv'";
        var ast = OneStatementParsesTo(
            sql,
            "CREATE EXTERNAL TABLE uk_cities ("
            + "name VARCHAR(100) NOT NULL, "
            + "lat DOUBLE NULL, "
            + "lng DOUBLE) "
            + "STORED AS PARQUET LOCATION '/tmp/example.csv'");
        Assert.IsType<Statement.CreateTable>(ast);
    }

    [Fact]
    public void parse_create_table_hive_formats_none_when_no_options()
    {
        var sql = "CREATE TABLE simple_table (id INT, name VARCHAR(100))";
        switch (VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Null(createTable.HiveFormats);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_alter_table()
    {
        var addColumn = "ALTER TABLE tab ADD COLUMN foo TEXT;";
        switch (TestUtils.AlterTableOp(OneStatementParsesTo(
            addColumn,
            "ALTER TABLE tab ADD COLUMN foo TEXT")))
        {
            case AlterTableOperation.AddColumn addColumnOp:
                Assert.True(addColumnOp.ColumnKeyword);
                Assert.False(addColumnOp.IfNotExists);
                Assert.Equal("foo", addColumnOp.ColumnDef.Name.ToString());
                Assert.Equal("TEXT", addColumnOp.ColumnDef.DataType.ToString());
                Assert.Null(addColumnOp.ColumnPosition);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        var renameTable = "ALTER TABLE tab RENAME TO new_tab";
        switch (TestUtils.AlterTableOp(VerifiedStmt(renameTable)))
        {
            case AlterTableOperation.RenameTable { TableName: var tableName }:
                Assert.Equal(
                    new RenameTableNameKind.To(ObjectName.From(Sequence.Of(new Ident("new_tab")))),
                    tableName);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        var renameTableAs = "ALTER TABLE tab RENAME AS new_tab";
        switch (TestUtils.AlterTableOp(VerifiedStmt(renameTableAs)))
        {
            case AlterTableOperation.RenameTable { TableName: var tableName }:
                Assert.Equal(
                    new RenameTableNameKind.As(ObjectName.From(Sequence.Of(new Ident("new_tab")))),
                    tableName);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        var renameColumn = "ALTER TABLE tab RENAME COLUMN foo TO new_foo";
        switch (TestUtils.AlterTableOp(VerifiedStmt(renameColumn)))
        {
            case AlterTableOperation.RenameColumn renameColumnOp:
                Assert.Equal("foo", renameColumnOp.OldColumnName.ToString());
                Assert.Equal("new_foo", renameColumnOp.NewColumnName.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        var setTableProperties = "ALTER TABLE tab SET TBLPROPERTIES('classification' = 'parquet')";
        switch (TestUtils.AlterTableOp(VerifiedStmt(setTableProperties)))
        {
            case AlterTableOperation.SetTblProperties { TableProperties: var tableProperties }:
                Assert.Equal(
                    tableProperties,
                    Sequence.Of<SqlOption>(new SqlOption.KeyValue(
                        Key: new Ident("classification", '\'', Span.Empty()),
                        Value: new Expr.Value(new Value.SingleQuotedString("parquet").WithEmptySpan()))));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        var setStorageParameters = "ALTER TABLE tab SET (autovacuum_vacuum_scale_factor = 0.01, autovacuum_vacuum_threshold = 500)";
        switch (TestUtils.AlterTableOp(VerifiedStmt(setStorageParameters)))
        {
            case AlterTableOperation.SetOptionsParens { Options: var options }:
                Assert.Equal(
                    options,
                    Sequence.Of<SqlOption>(
                        new SqlOption.KeyValue(
                            Key: new Ident("autovacuum_vacuum_scale_factor", null, Span.Empty()),
                            Value: new Expr.Value(TestUtils.Number("0.01").WithEmptySpan())),
                        new SqlOption.KeyValue(
                            Key: new Ident("autovacuum_vacuum_threshold", null, Span.Empty()),
                            Value: new Expr.Value(TestUtils.Number("500").WithEmptySpan()))));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_rename_table()
    {
        switch (VerifiedStmt("RENAME TABLE test.test1 TO test_db.test2"))
        {
            case Statement.RenameTable { Value: var renameTables }:
                Assert.Equal(
                    Sequence.Of(new RenameTable(
                        OldName: ObjectName.From(Sequence.Of(
                            new Ident("test"),
                            new Ident("test1"))),
                        NewName: ObjectName.From(Sequence.Of(
                            new Ident("test_db"),
                            new Ident("test2"))))),
                    renameTables);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        switch (VerifiedStmt(
            "RENAME TABLE old_table1 TO new_table1, old_table2 TO new_table2, old_table3 TO new_table3"))
        {
            case Statement.RenameTable { Value: var renameTables }:
                Assert.Equal(
                    Sequence.Of(
                        new RenameTable(
                            OldName: ObjectName.From(Sequence.Of(new Ident("old_table1"))),
                            NewName: ObjectName.From(Sequence.Of(new Ident("new_table1")))),
                        new RenameTable(
                            OldName: ObjectName.From(Sequence.Of(new Ident("old_table2"))),
                            NewName: ObjectName.From(Sequence.Of(new Ident("new_table2")))),
                        new RenameTable(
                            OldName: ObjectName.From(Sequence.Of(new Ident("old_table3"))),
                            NewName: ObjectName.From(Sequence.Of(new Ident("new_table3"))))),
                    renameTables);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        Assert.Equal(
            Assert.Throws<ParserException>(() => ParseSqlStatements("RENAME TABLE old_table TO new_table a")).Error,
            new ParserError.ParserErrorValue("Expected: end of statement, found: a"));

        Assert.Equal(
            Assert.Throws<ParserException>(() => ParseSqlStatements("RENAME TABLE1 old_table TO new_table a")).Error,
            new ParserError.ParserErrorValue(
                "Expected: KEYWORD `TABLE` after RENAME, found: TABLE1"));
    }

    [Fact]
    public void test_alter_table_with_on_cluster()
    {
        switch (TestUtils.AllDialects()
            .VerifiedStmt("ALTER TABLE t ON CLUSTER 'cluster' ADD CONSTRAINT bar PRIMARY KEY (baz)"))
        {
            case Statement.AlterTable { Value: var alterTable }:
                Assert.Equal("t", alterTable.Name.ToString());
                Assert.Equal(alterTable.OnCluster, Ident.WithQuote('\'', "cluster"));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        switch (TestUtils.AllDialects()
            .VerifiedStmt("ALTER TABLE t ON CLUSTER cluster_name ADD CONSTRAINT bar PRIMARY KEY (baz)"))
        {
            case Statement.AlterTable { Value: var alterTable }:
                Assert.Equal("t", alterTable.Name.ToString());
                Assert.Equal(alterTable.OnCluster, new Ident("cluster_name"));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        var res = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("ALTER TABLE t ON CLUSTER 123 ADD CONSTRAINT bar PRIMARY KEY (baz)"));
        Assert.Equal(
            res.Error,
            new ParserError.ParserErrorValue("Expected: identifier, found: 123"));
    }

    [Fact]
    public void parse_alter_index()
    {
        var renameIndex = "ALTER INDEX idx RENAME TO new_idx";
        switch (VerifiedStmt(renameIndex))
        {
            case Statement.AlterIndex { Name: var name, Operation: AlterIndexOperation.RenameIndex { IndexName: var indexName } }:
                Assert.Equal("idx", name.ToString());
                Assert.Equal("new_idx", indexName.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_alter_view()
    {
        var sql = "ALTER VIEW myschema.myview AS SELECT foo FROM bar";
        switch (VerifiedStmt(sql))
        {
            case Statement.AlterView alterView:
                Assert.Equal("myschema.myview", alterView.Name.ToString());
                Assert.Equal(Sequence<Ident>.Empty, alterView.Columns);
                Assert.Equal("SELECT foo FROM bar", alterView.Query.ToString());
                Assert.Equal(alterView.WithOptions, Sequence<SqlOption>.Empty);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_alter_view_with_options()
    {
        var sql = "ALTER VIEW v WITH (foo = 'bar', a = 123) AS SELECT 1";
        switch (VerifiedStmt(sql))
        {
            case Statement.AlterView { WithOptions: var withOptions }:
                Assert.Equal(
                    Sequence.Of<SqlOption>(
                        new SqlOption.KeyValue(
                            Key: new Ident("foo"),
                            Value: new Expr.Value(new Value.SingleQuotedString("bar").WithEmptySpan())),
                        new SqlOption.KeyValue(
                            Key: new Ident("a"),
                            Value: Expr.MakeValue(TestUtils.Number("123")))),
                    withOptions);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_alter_view_with_columns()
    {
        var sql = "ALTER VIEW v (has, cols) AS SELECT 1, 2";
        switch (VerifiedStmt(sql))
        {
            case Statement.AlterView alterView:
                Assert.Equal("v", alterView.Name.ToString());
                Assert.Equal(alterView.Columns, Sequence.Of(new Ident("has"), new Ident("cols")));
                Assert.Equal("SELECT 1, 2", alterView.Query.ToString());
                Assert.Equal(alterView.WithOptions, Sequence<SqlOption>.Empty);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_alter_table_add_column()
    {
        switch (TestUtils.AlterTableOp(VerifiedStmt("ALTER TABLE tab ADD foo TEXT")))
        {
            case AlterTableOperation.AddColumn { ColumnKeyword: var columnKeyword }:
                Assert.False(columnKeyword);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        switch (TestUtils.AlterTableOp(VerifiedStmt("ALTER TABLE tab ADD COLUMN foo TEXT")))
        {
            case AlterTableOperation.AddColumn { ColumnKeyword: var columnKeyword }:
                Assert.True(columnKeyword);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_alter_table_add_column_if_not_exists()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new PostgreSqlDialect(),
            new BigQueryDialect(),
            new GenericDialect(),
            new DuckDbDialect(),
        });

        switch (TestUtils.AlterTableOp(dialects.VerifiedStmt("ALTER TABLE tab ADD IF NOT EXISTS foo TEXT")))
        {
            case AlterTableOperation.AddColumn { IfNotExists: var ifNotExists }:
                Assert.True(ifNotExists);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        switch (TestUtils.AlterTableOp(
            dialects.VerifiedStmt("ALTER TABLE tab ADD COLUMN IF NOT EXISTS foo TEXT")))
        {
            case AlterTableOperation.AddColumn { ColumnKeyword: var columnKeyword, IfNotExists: var ifNotExists }:
                Assert.True(columnKeyword);
                Assert.True(ifNotExists);
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_alter_table_constraints()
    {
        CheckOne("CONSTRAINT address_pkey PRIMARY KEY (address_id)");
        CheckOne("CONSTRAINT uk_task UNIQUE (report_date, task_id)");
        CheckOne(
            "CONSTRAINT customer_address_id_fkey FOREIGN KEY (address_id) "
            + "REFERENCES public.address(address_id)");
        CheckOne("CONSTRAINT ck CHECK (rtrim(ltrim(REF_CODE)) <> '')");

        CheckOne("PRIMARY KEY (foo, bar)");
        CheckOne("UNIQUE (id)");
        CheckOne("FOREIGN KEY (foo, bar) REFERENCES AnotherTable(foo, bar)");
        CheckOne("CHECK (end_date > start_date OR end_date IS NULL)");
        CheckOne("CONSTRAINT fk FOREIGN KEY (lng) REFERENCES othertable4");

        static void CheckOne(string constraintText)
        {
            switch (TestUtils.AlterTableOp(VerifiedStmt(
                $"ALTER TABLE tab ADD {constraintText}")))
            {
                case AlterTableOperation.AddConstraint { Constraint: var constraint }:
                    Assert.Equal(constraintText, constraint.ToString());
                    break;
                default:
                    throw new InvalidOperationException(Unreachable);
            }
            VerifiedStmt($"CREATE TABLE foo (id INT, {constraintText})");
        }
    }

    [Fact]
    public void parse_alter_table_drop_column()
    {
        CheckOne("DROP COLUMN IF EXISTS is_active");
        CheckOne("DROP COLUMN IF EXISTS is_active CASCADE");
        CheckOne("DROP COLUMN IF EXISTS is_active RESTRICT");
        OneStatementParsesTo(
            "ALTER TABLE tab DROP COLUMN IF EXISTS is_active CASCADE",
            "ALTER TABLE tab DROP COLUMN IF EXISTS is_active CASCADE");
        OneStatementParsesTo(
            "ALTER TABLE tab DROP is_active CASCADE",
            "ALTER TABLE tab DROP is_active CASCADE");

        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsCommaSeparatedDropColumnList);
        dialects.VerifiedStmt("ALTER TABLE tbl DROP COLUMN c1, c2, c3");

        static void CheckOne(string constraintText)
        {
            switch (TestUtils.AlterTableOp(VerifiedStmt($"ALTER TABLE tab {constraintText}")))
            {
                case AlterTableOperation.DropColumn { HasColumnKeyword: true } dropColumn:
                    Assert.Equal("is_active", dropColumn.ColumnNames.First!.ToString());
                    Assert.True(dropColumn.IfExists);
                    switch (dropColumn.DropBehavior)
                    {
                        case null:
                            Assert.EndsWith(" is_active", constraintText, StringComparison.Ordinal);
                            break;
                        case DropBehavior.Restrict:
                            Assert.EndsWith(" RESTRICT", constraintText, StringComparison.Ordinal);
                            break;
                        case DropBehavior.Cascade:
                            Assert.EndsWith(" CASCADE", constraintText, StringComparison.Ordinal);
                            break;
                        default:
                            throw new InvalidOperationException($"Variante não tratada: {dropColumn.DropBehavior}");
                    }
                    break;
                default:
                    throw new InvalidOperationException(Unreachable);
            }
        }
    }

    [Fact]
    public void parse_alter_table_alter_column()
    {
        var alterStmt = "ALTER TABLE tab";
        switch (TestUtils.AlterTableOp(VerifiedStmt(
            $"{alterStmt} ALTER COLUMN is_active SET NOT NULL")))
        {
            case AlterTableOperation.AlterColumn { ColumnName: var columnName, Op: var op }:
                Assert.Equal("is_active", columnName.ToString());
                Assert.Equal(op, new AlterColumnOperation.SetNotNull());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        OneStatementParsesTo(
            "ALTER TABLE tab ALTER is_active DROP NOT NULL",
            "ALTER TABLE tab ALTER COLUMN is_active DROP NOT NULL");

        switch (TestUtils.AlterTableOp(VerifiedStmt(
            $"{alterStmt} ALTER COLUMN is_active SET DEFAULT 0")))
        {
            case AlterTableOperation.AlterColumn { ColumnName: var columnName, Op: var op }:
                Assert.Equal("is_active", columnName.ToString());
                Assert.Equal(
                    op,
                    new AlterColumnOperation.SetDefault(
                        Value: new Expr.Value(TestUtils.Number("0").WithEmptySpan())));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        switch (TestUtils.AlterTableOp(VerifiedStmt(
            $"{alterStmt} ALTER COLUMN is_active DROP DEFAULT")))
        {
            case AlterTableOperation.AlterColumn { ColumnName: var columnName, Op: var op }:
                Assert.Equal("is_active", columnName.ToString());
                Assert.Equal(op, new AlterColumnOperation.DropDefault());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
    }

    [Fact]
    public void parse_alter_table_alter_column_type()
    {
        var alterStmt = "ALTER TABLE tab";
        switch (TestUtils.AlterTableOp(VerifiedStmt(
            "ALTER TABLE tab ALTER COLUMN is_active SET DATA TYPE TEXT")))
        {
            case AlterTableOperation.AlterColumn { ColumnName: var columnName, Op: var op }:
                Assert.Equal("is_active", columnName.ToString());
                Assert.Equal(
                    op,
                    new AlterColumnOperation.SetDataType(
                        DataType: new DataType.Text(),
                        Using: null,
                        HadSet: true));
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }
        VerifiedStmt($"{alterStmt} ALTER COLUMN is_active TYPE TEXT");

        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsAlterColumnTypeUsing);
        dialects.VerifiedStmt(
            $"{alterStmt} ALTER COLUMN is_active SET DATA TYPE TEXT USING 'text'");

        var dialectsWithoutUsing = TestUtils.AllDialectsExcept(d => d.SupportsAlterColumnTypeUsing);
        var res = Assert.Throws<ParserException>(() => dialectsWithoutUsing.ParseSqlStatements(
            $"{alterStmt} ALTER COLUMN is_active SET DATA TYPE TEXT USING 'text'"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: USING"),
            res.Error);
    }

    [Fact]
    public void parse_alter_table_drop_constraint()
    {
        CheckOne("DROP CONSTRAINT IF EXISTS constraint_name");
        CheckOne("DROP CONSTRAINT IF EXISTS constraint_name RESTRICT");
        CheckOne("DROP CONSTRAINT IF EXISTS constraint_name CASCADE");
        static void CheckOne(string constraintText)
        {
            switch (TestUtils.AlterTableOp(VerifiedStmt($"ALTER TABLE tab {constraintText}")))
            {
                case AlterTableOperation.DropConstraint { Name: var constrName, IfExists: var ifExists, DropBehavior: var dropBehavior }:
                    Assert.Equal("constraint_name", constrName.ToString());
                    Assert.True(ifExists);
                    switch (dropBehavior)
                    {
                        case null:
                            Assert.EndsWith(" constraint_name", constraintText, StringComparison.Ordinal);
                            break;
                        case DropBehavior.Restrict:
                            Assert.EndsWith(" RESTRICT", constraintText, StringComparison.Ordinal);
                            break;
                        case DropBehavior.Cascade:
                            Assert.EndsWith(" CASCADE", constraintText, StringComparison.Ordinal);
                            break;
                        default:
                            throw new InvalidOperationException($"Variante não tratada: {dropBehavior}");
                    }
                    break;
                default:
                    throw new InvalidOperationException(Unreachable);
            }
        }

        var res = Assert.Throws<ParserException>(() => ParseSqlStatements("ALTER TABLE tab DROP CONSTRAINT is_active TEXT"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: TEXT"),
            res.Error);
    }

    [Fact]
    public void parse_bad_constraint()
    {
        var res = Assert.Throws<ParserException>(() => ParseSqlStatements("ALTER TABLE tab ADD"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: identifier, found: EOF"),
            res.Error);

        res = Assert.Throws<ParserException>(() => ParseSqlStatements("CREATE TABLE tab (foo int,"));
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected: column name or constraint definition, found: EOF"),
            res.Error);
    }

    [Fact]
    public void parse_scalar_function_in_projection()
    {
        var names = new List<string> { "sqrt", "foo" };

        foreach (var functionName in names)
        {
            // like SELECT sqrt(id) FROM foo
            var sql = $"SELECT {functionName}(id) FROM foo";
            var select = VerifiedOnlySelect(sql);
            Assert.Equal(
                TestUtils.Call(functionName, new Expr[] { new Expr.Identifier(new Ident("id")) }),
                TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        }
    }

    private static void RunExplainAnalyze(
        TestedDialects dialect,
        string query,
        bool expectedVerbose,
        bool expectedAnalyze,
        AnalyzeFormatKind? expectedFormat,
        Sequence<UtilityOption>? expectedOptions)
    {
        switch (dialect.VerifiedStmt(query))
        {
            case Statement.Explain explain:
                Assert.Equal(explain.Verbose, expectedVerbose);
                Assert.Equal(explain.Analyze, expectedAnalyze);
                Assert.Equal(explain.Format, expectedFormat);
                Assert.Equal(explain.Options, expectedOptions);
                Assert.False(explain.QueryPlan);
                Assert.False(explain.Estimate);
                Assert.Equal("SELECT sqrt(id) FROM foo", explain.Statement.ToString());
                break;
            default:
                Assert.Fail("Unexpected Statement, must be Explain");
                break;
        }
    }

    [Fact]
    public void parse_explain_table()
    {
        static void ValidateExplain(string query, DescribeAlias expectedDescribeAlias, bool expectedTableKeyword)
        {
            switch (VerifiedStmt(query))
            {
                case Statement.ExplainTable explainTable:
                    Assert.Equal(explainTable.DescribeAlias, expectedDescribeAlias);
                    Assert.Null(explainTable.HiveFormat);
                    Assert.Equal(explainTable.HasTableKeyword, expectedTableKeyword);
                    Assert.Equal("test_identifier", explainTable.TableName.ToString());
                    break;
                default:
                    Assert.Fail("Unexpected Statement, must be ExplainTable");
                    break;
            }
        }

        ValidateExplain("EXPLAIN test_identifier", new DescribeAlias.Explain(), false);
        ValidateExplain("DESCRIBE test_identifier", new DescribeAlias.Describe(), false);
        ValidateExplain("DESC test_identifier", new DescribeAlias.Desc(), false);
    }

    [Fact]
    public void explain_describe()
    {
        VerifiedStmt("DESCRIBE test.table");
    }

    [Fact]
    public void explain_desc()
    {
        VerifiedStmt("DESC test.table");
    }

    [Fact]
    public void parse_explain_analyze_with_simple_select()
    {
        // Describe is an alias for EXPLAIN
        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "DESCRIBE SELECT sqrt(id) FROM foo",
            false,
            false,
            null,
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN SELECT sqrt(id) FROM foo",
            false,
            false,
            null,
            null);
        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN VERBOSE SELECT sqrt(id) FROM foo",
            true,
            false,
            null,
            null);
        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN ANALYZE SELECT sqrt(id) FROM foo",
            false,
            true,
            null,
            null);
        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN ANALYZE VERBOSE SELECT sqrt(id) FROM foo",
            true,
            true,
            null,
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN ANALYZE FORMAT GRAPHVIZ SELECT sqrt(id) FROM foo",
            false,
            true,
            new AnalyzeFormatKind.Keyword(new AnalyzeFormat.GRAPHVIZ()),
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN ANALYZE VERBOSE FORMAT JSON SELECT sqrt(id) FROM foo",
            true,
            true,
            new AnalyzeFormatKind.Keyword(new AnalyzeFormat.JSON()),
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN ANALYZE VERBOSE FORMAT=JSON SELECT sqrt(id) FROM foo",
            true,
            true,
            new AnalyzeFormatKind.Assignment(new AnalyzeFormat.JSON()),
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN VERBOSE FORMAT TEXT SELECT sqrt(id) FROM foo",
            true,
            false,
            new AnalyzeFormatKind.Keyword(new AnalyzeFormat.TEXT()),
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN FORMAT=TEXT SELECT sqrt(id) FROM foo",
            false,
            false,
            new AnalyzeFormatKind.Assignment(new AnalyzeFormat.TEXT()),
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN FORMAT=GRAPHVIZ SELECT sqrt(id) FROM foo",
            false,
            false,
            new AnalyzeFormatKind.Assignment(new AnalyzeFormat.GRAPHVIZ()),
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN FORMAT=JSON SELECT sqrt(id) FROM foo",
            false,
            false,
            new AnalyzeFormatKind.Assignment(new AnalyzeFormat.JSON()),
            null);

        RunExplainAnalyze(
            TestUtils.AllDialects(),
            "EXPLAIN FORMAT=TREE SELECT sqrt(id) FROM foo",
            false,
            false,
            new AnalyzeFormatKind.Assignment(new AnalyzeFormat.TREE()),
            null);
    }

    [Fact]
    public void parse_explain_query_plan()
    {
        switch (TestUtils.AllDialects().VerifiedStmt("EXPLAIN QUERY PLAN SELECT sqrt(id) FROM foo"))
        {
            case Statement.Explain explain:
                Assert.True(explain.QueryPlan);
                Assert.False(explain.Analyze);
                Assert.False(explain.Verbose);
                Assert.Equal("SELECT sqrt(id) FROM foo", explain.Statement.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        // omit QUERY PLAN should be good
        TestUtils.AllDialects().VerifiedStmt("EXPLAIN SELECT sqrt(id) FROM foo");

        // missing PLAN keyword should return error
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: SELECT"),
            Assert.Throws<ParserException>(() => TestUtils.AllDialects()
                .ParseSqlStatements("EXPLAIN QUERY SELECT sqrt(id) FROM foo")).Error);
    }

    [Fact]
    public void parse_explain_estimate()
    {
        var statement = TestUtils.AllDialects().VerifiedStmt("EXPLAIN ESTIMATE SELECT sqrt(id) FROM foo");

        switch (statement)
        {
            case Statement.Explain explain:
                Assert.True(explain.Estimate);
                Assert.False(explain.QueryPlan);
                Assert.False(explain.Analyze);
                Assert.False(explain.Verbose);
                Assert.Equal("SELECT sqrt(id) FROM foo", explain.Statement.ToString());
                break;
            default:
                throw new InvalidOperationException(Unreachable);
        }

        Assert.Equal(
            "EXPLAIN ESTIMATE SELECT sqrt(id) FROM foo",
            statement.ToString());
    }

    [Fact]
    public void parse_named_argument_function()
    {
        var dialects = TestUtils.AllDialectsWhere(d =>
            d.SupportsNamedFnArgsWithRarrowOperator
            && !d.SupportsNamedFnArgsWithExprName);
        var sql = "SELECT FUN(a => '1', b => '2') FROM foo";
        var select = dialects.VerifiedOnlySelect(sql);

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
                            Operator: new FunctionArgOperator.RightArrow()),
                        new FunctionArg.Named(
                            Name: new Ident("b"),
                            Arg: new FunctionArgExpr.Expr(new Expr.Value(
                                new Value.SingleQuotedString("2").WithEmptySpan())),
                            Operator: new FunctionArgOperator.RightArrow())),
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_named_argument_function_with_eq_operator()
    {
        var sql = "SELECT FUN(a = '1', b = '2') FROM foo";

        var select = TestUtils.AllDialectsWhere(d => d.SupportsNamedFnArgsWithEqOperator)
            .VerifiedOnlySelect(sql);
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
                            Operator: new FunctionArgOperator.EqualsVariant()),
                        new FunctionArg.Named(
                            Name: new Ident("b"),
                            Arg: new FunctionArgExpr.Expr(new Expr.Value(
                                new Value.SingleQuotedString("2").WithEmptySpan())),
                            Operator: new FunctionArgOperator.EqualsVariant())),
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        // Ensure that bar = 42 in a function argument parses as an equality binop
        // rather than a named function argument.
        Assert.Equal(
            TestUtils.AllDialectsExcept(d => d.SupportsNamedFnArgsWithEqOperator)
                .VerifiedExpr("foo(bar = 42)"),
            TestUtils.Call(
                "foo",
                new Expr[]
                {
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("bar")),
                        Op: new BinaryOperator.Eq(),
                        Right: Expr.MakeValue(TestUtils.Number("42"))),
                }));

        // TODO: should this parse for all dialects?
        TestUtils.AllDialectsExcept(d => d.SupportsNamedFnArgsWithEqOperator)
            .VerifiedExpr("iff(1 = 1, 1, 0)");
    }

    [Fact]
    public void parse_window_functions()
    {
        var sql = "SELECT row_number() OVER (ORDER BY dt DESC), "
            + "sum(foo) OVER (PARTITION BY a, b ORDER BY c, d "
            + "ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW), "
            + "avg(bar) OVER (ORDER BY a "
            + "RANGE BETWEEN 1 PRECEDING AND 1 FOLLOWING), "
            + "sum(bar) OVER (ORDER BY a "
            + "RANGE BETWEEN INTERVAL '1' DAY PRECEDING AND INTERVAL '1 MONTH' FOLLOWING), "
            + "COUNT(*) OVER (ORDER BY a "
            + "RANGE BETWEEN INTERVAL '1 DAY' PRECEDING AND INTERVAL '1 DAY' FOLLOWING), "
            + "max(baz) OVER (ORDER BY a "
            + "ROWS UNBOUNDED PRECEDING), "
            + "sum(qux) OVER (ORDER BY a "
            + "GROUPS BETWEEN 1 PRECEDING AND 1 FOLLOWING) "
            + "FROM foo";
        var dialects = TestUtils.AllDialectsExcept(d => d.RequireIntervalQualifier);
        var select = dialects.VerifiedOnlySelect(sql);

        const int ExpectedProjQty = 7;
        Assert.Equal(ExpectedProjQty, select.Projection.Count);

        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("row_number"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: null,
                    Args: Sequence<FunctionArg>.Empty,
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                NullTreatment: null,
                Filter: null,
                Over: new WindowType.WindowSpec(new WindowSpec(
                    WindowName: null,
                    PartitionBy: Sequence<Expr>.Empty,
                    OrderBy: Sequence.Of(new OrderByExpr(
                        Expr: new Expr.Identifier(new Ident("dt")),
                        Options: new OrderByOptions(
                            Sort: new OrderBySort.Desc(),
                            NullsFirst: null),
                        WithFill: null)),
                    WindowFrame: null)),
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(select.Projection[0]));

        for (var i = 0; i < ExpectedProjQty; i++)
        {
            Assert.True(
                TestUtils.ExprFromProjection(select.Projection[i]) is Expr.Function
                {
                    Value.Over: WindowType.WindowSpec { Value.WindowName: null },
                });
        }
    }

    [Fact]
    public void parse_named_window_functions()
    {
        var supportedDialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new PostgreSqlDialect(),
            new MySqlDialect(),
            new BigQueryDialect(),
        });

        var sql = "SELECT row_number() OVER (w ORDER BY dt DESC), "
            + "sum(foo) OVER (win PARTITION BY a, b ORDER BY c, d "
            + "ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) "
            + "FROM foo "
            + "WINDOW w AS (PARTITION BY x), win AS (ORDER BY y)";
        supportedDialects.VerifiedStmt(sql);

        var select = TestUtils.AllDialectsExcept(d => d.IsTableAlias(Keyword.WINDOW, new Parser(d)))
            .VerifiedOnlySelect(sql);

        const int ExpectedProjQty = 2;
        Assert.Equal(ExpectedProjQty, select.Projection.Count);

        var expectedWinNames = new[] { "w", "win" };
        for (var i = 0; i < expectedWinNames.Length; i++)
        {
            var winName = expectedWinNames[i];
            Assert.True(
                TestUtils.ExprFromProjection(select.Projection[i]) is Expr.Function
                {
                    Value.Over: WindowType.WindowSpec { Value.WindowName: Ident { Value: var value } },
                }
                && value == winName);
        }

        var sqlWithAliases = "SELECT "
            + "FIRST_VALUE(x) OVER (w ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS first, "
            + "FIRST_VALUE(x) OVER (ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS last, "
            + "SUM(y) OVER (win PARTITION BY x) AS last "
            + "FROM EMPLOYEE "
            + "WINDOW w AS (PARTITION BY x), win AS (w ORDER BY y)";
        supportedDialects.VerifiedStmt(sqlWithAliases);
    }

    [Fact]
    public void parse_window_clause()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d.IsTableAlias(Keyword.WINDOW, new Parser(d)));
        var sql = "SELECT * "
            + "FROM mytable "
            + "WINDOW "
            + "window1 AS (ORDER BY 1 ASC, 2 DESC, 3 NULLS FIRST), "
            + "window2 AS (window1), "
            + "window3 AS (PARTITION BY a, b, c), "
            + "window4 AS (ROWS UNBOUNDED PRECEDING), "
            + "window5 AS (window1 PARTITION BY a), "
            + "window6 AS (window1 ORDER BY a), "
            + "window7 AS (window1 ROWS UNBOUNDED PRECEDING), "
            + "window8 AS (window1 PARTITION BY a ORDER BY b ROWS UNBOUNDED PRECEDING) "
            + "ORDER BY C3";
        dialects.VerifiedOnlySelect(sql);

        var sqlNamedWindow = "SELECT * from mytable WINDOW window1 AS window2";
        var dialectsWithoutNamedWindow = TestUtils.AllDialectsExcept(d =>
            d is BigQueryDialect
            || d is GenericDialect
            || d.IsTableAlias(Keyword.WINDOW, new Parser(d)));
        var res = Assert.Throws<ParserException>(() => dialectsWithoutNamedWindow.ParseSqlStatements(sqlNamedWindow));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: (, found: window2"),
            res.Error);
    }

    [Fact]
    public void test_parse_named_window()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d.IsTableAlias(Keyword.WINDOW, new Parser(d)));
        var sql = "SELECT "
            + "MIN(c12) OVER window1 AS min1, "
            + "MAX(c12) OVER window2 AS max1 "
            + "FROM aggregate_test_100 "
            + "WINDOW window1 AS (ORDER BY C12), "
            + "window2 AS (PARTITION BY C11) "
            + "ORDER BY C3";
        var actualSelectOnly = dialects.VerifiedOnlySelect(sql);
        var expected = new Select(
            SelectToken: AttachedToken.Empty(),
            OptimizerHints: Sequence<OptimizerHint>.Empty,
            Distinct: null,
            SelectModifiers: null,
            Top: null,
            TopBeforeDistinct: false,
            Projection: Sequence.Of<SelectItem>(
                new SelectItem.ExprWithAlias(
                    Expr: new Expr.Function(new Function(
                        Name: ObjectName.From(Sequence.Of(new Ident("MIN", null, Span.Empty()))),
                        UsesOdbcSyntax: false,
                        Parameters: new FunctionArguments.None(),
                        Args: new FunctionArguments.List(new FunctionArgumentList(
                            DuplicateTreatment: null,
                            Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                                new Expr.Identifier(new Ident("c12", null, Span.Empty()))))),
                            Clauses: Sequence<FunctionArgumentClause>.Empty)),
                        NullTreatment: null,
                        Filter: null,
                        Over: new WindowType.NamedWindow(new Ident("window1", null, Span.Empty())),
                        WithinGroup: Sequence<OrderByExpr>.Empty)),
                    Alias: new Ident("min1", null, Span.Empty())),
                new SelectItem.ExprWithAlias(
                    Expr: new Expr.Function(new Function(
                        Name: ObjectName.From(Sequence.Of(new Ident("MAX", null, Span.Empty()))),
                        UsesOdbcSyntax: false,
                        Parameters: new FunctionArguments.None(),
                        Args: new FunctionArguments.List(new FunctionArgumentList(
                            DuplicateTreatment: null,
                            Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                                new Expr.Identifier(new Ident("c12", null, Span.Empty()))))),
                            Clauses: Sequence<FunctionArgumentClause>.Empty)),
                        NullTreatment: null,
                        Filter: null,
                        Over: new WindowType.NamedWindow(new Ident("window2", null, Span.Empty())),
                        WithinGroup: Sequence<OrderByExpr>.Empty)),
                    Alias: new Ident("max1", null, Span.Empty()))),
            Exclude: null,
            Into: null,
            From: Sequence.Of(new TableWithJoins(
                Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("aggregate_test_100", null, Span.Empty())))),
                Joins: Sequence<Join>.Empty)),
            LateralViews: Sequence<LateralView>.Empty,
            Prewhere: null,
            Selection: null,
            GroupBy: new GroupByExpr.Expressions(Sequence<Expr>.Empty, Sequence<GroupByWithModifier>.Empty),
            ClusterBy: Sequence<Expr>.Empty,
            DistributeBy: Sequence<Expr>.Empty,
            SortBy: Sequence<OrderByExpr>.Empty,
            Having: null,
            NamedWindow: Sequence.Of(
                new NamedWindowDefinition(
                    new Ident("window1", null, Span.Empty()),
                    new NamedWindowExpr.WindowSpec(new WindowSpec(
                        WindowName: null,
                        PartitionBy: Sequence<Expr>.Empty,
                        OrderBy: Sequence.Of(new OrderByExpr(
                            Expr: new Expr.Identifier(new Ident("C12", null, Span.Empty())),
                            Options: new OrderByOptions(
                                Sort: null,
                                NullsFirst: null),
                            WithFill: null)),
                        WindowFrame: null))),
                new NamedWindowDefinition(
                    new Ident("window2", null, Span.Empty()),
                    new NamedWindowExpr.WindowSpec(new WindowSpec(
                        WindowName: null,
                        PartitionBy: Sequence.Of<Expr>(new Expr.Identifier(new Ident("C11", null, Span.Empty()))),
                        OrderBy: Sequence<OrderByExpr>.Empty,
                        WindowFrame: null)))),
            Qualify: null,
            WindowBeforeQualify: true,
            ValueTableMode: null,
            ConnectBy: Sequence<ConnectByKind>.Empty,
            Flavor: SelectFlavor.Standard);
        Assert.Equal(actualSelectOnly, expected);
    }

    [Fact]
    public void parse_window_and_qualify_clause()
    {
        var dialects = TestUtils.AllDialectsExcept(d =>
            d.IsTableAlias(Keyword.WINDOW, new Parser(d))
            || d.IsTableAlias(Keyword.QUALIFY, new Parser(d)));
        var sql = "SELECT "
            + "MIN(c12) OVER window1 AS min1 "
            + "FROM aggregate_test_100 "
            + "QUALIFY ROW_NUMBER() OVER my_window "
            + "WINDOW window1 AS (ORDER BY C12), "
            + "window2 AS (PARTITION BY C11) "
            + "ORDER BY C3";
        dialects.VerifiedOnlySelect(sql);

        sql = "SELECT "
            + "MIN(c12) OVER window1 AS min1 "
            + "FROM aggregate_test_100 "
            + "WINDOW window1 AS (ORDER BY C12), "
            + "window2 AS (PARTITION BY C11) "
            + "QUALIFY ROW_NUMBER() OVER my_window "
            + "ORDER BY C3";
        dialects.VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_window_clause_named_window()
    {
        var sql = "SELECT * FROM mytable WINDOW window1 AS window2";
        var namedWindow =
            TestUtils.AllDialectsWhere(d => d.SupportsWindowClauseNamedWindowReference)
                .VerifiedOnlySelect(sql).NamedWindow;
        Assert.Equal(
            Sequence.Of(new NamedWindowDefinition(
                new Ident("window1"),
                new NamedWindowExpr.NamedWindow(new Ident("window2")))),
            namedWindow);
    }

    [Fact]
    public void parse_aggregate_with_group_by()
    {
        var sql = "SELECT a, COUNT(1), MIN(b), MAX(b) FROM foo GROUP BY a";
        _ = VerifiedOnlySelect(sql);
        //TODO: assertions
    }

    [Fact]
    public void parse_aggregate_with_where_filter()
    {
        // The inline `WHERE` filter inside an aggregate call, e.g. `COUNT(* WHERE cond)` /
        // `SUM(x WHERE cond)`, is the in-argument spelling of the standard
        // `AGG(x) FILTER (WHERE cond)`. Popularized by GoogleSQL, it is accepted for all
        // dialects (`verified_stmt` round-trips through every dialect).
        VerifiedStmt("SELECT COUNT(* WHERE x > 1) FROM t");
        VerifiedStmt("SELECT SUM(x WHERE y > 0) FROM t");
        // Co-occurs with (and precedes) an in-argument ORDER BY.
        VerifiedStmt("SELECT ARRAY_AGG(x WHERE x > 100 ORDER BY x DESC) FROM t");
        // A compound predicate referencing multiple columns round-trips intact.
        VerifiedStmt("SELECT ARRAY_AGG(a WHERE b > 0 AND c < 10) FROM t");

        // The filter is captured as a FunctionArgumentClause::Where holding the predicate.
        var select = VerifiedOnlySelect("SELECT SUM(salary WHERE dept = 1) FROM emp");
        if (TestUtils.ExprFromProjection(select.Projection[0]) is not Expr.Function { Value: var func })
        {
            Assert.Fail("expected a function projection");
            return;
        }
        if (func.Args is not FunctionArguments.List { Value: var list })
        {
            Assert.Fail("expected a function argument list");
            return;
        }
        Assert.Contains(
            list.Clauses,
            c => c is FunctionArgumentClause.Where { Value: Expr.BinaryOp });
    }

    [Fact]
    public void parse_literal_integer()
    {
        var sql = "SELECT 1, -10, +20";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            Expr.MakeValue(TestUtils.Number("1")),
            TestUtils.ExprFromProjection(select.Projection[0]));
        // negative literal is parsed as a - and expr
        Assert.Equal(
            new Expr.UnaryOp(
                Op: new UnaryOperator.Minus(),
                Expr: Expr.MakeValue(TestUtils.Number("10"))),
            TestUtils.ExprFromProjection(select.Projection[1]));
        // positive literal is parsed as a + and expr
        Assert.Equal(
            new Expr.UnaryOp(
                Op: new UnaryOperator.Plus(),
                Expr: Expr.MakeValue(TestUtils.Number("20"))),
            TestUtils.ExprFromProjection(select.Projection[2]));
    }

    [Fact]
    public void parse_literal_decimal()
    {
        // These numbers were explicitly chosen to not roundtrip if represented as
        // f64s (i.e., as 64-bit binary floating point numbers).
        var sql = "SELECT 0.300000000000000004, 9007199254740993.0";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(2, select.Projection.Count);
        Assert.Equal(
            Expr.MakeValue(TestUtils.Number("0.300000000000000004")),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            Expr.MakeValue(TestUtils.Number("9007199254740993.0")),
            TestUtils.ExprFromProjection(select.Projection[1]));
    }

    [Fact]
    public void parse_literal_string()
    {
        var nationalStringDialects = TestUtils.AllDialectsWhere(d => d.SupportsNationalStringLiteral);
        var sql = "SELECT 'one', N'national string', X'deadBEEF'";
        var select = nationalStringDialects.VerifiedOnlySelect(sql);
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            new Expr.Value(new Value.SingleQuotedString("one").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Value(
                new Value.NationalStringLiteral("national string").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Value(new Value.HexStringLiteral("deadBEEF").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[2]));

        TestUtils.AllDialects().OneStatementParsesTo("SELECT x'deadBEEF'", "SELECT X'deadBEEF'");
        nationalStringDialects
            .OneStatementParsesTo("SELECT n'national string'", "SELECT N'national string'");
        nationalStringDialects.OneStatementParsesTo(
            """SELECT n'Tu geres '';'' et ''"'' ?'""",
            """SELECT N'Tu geres '';'' et ''"'' ?'""");
    }

    [Fact]
    public void parse_literal_date()
    {
        var sql = "SELECT DATE '1999-01-01'";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.Date(),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("1999-01-01"),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_literal_time()
    {
        var sql = "SELECT TIME '01:23:34'";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.Time(null, new TimezoneInfo.None()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("01:23:34"),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_literal_datetime()
    {
        var sql = "SELECT DATETIME '1999-01-01 01:23:34.45'";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.Datetime(null),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("1999-01-01 01:23:34.45"),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_literal_timestamp_without_time_zone()
    {
        var sql = "SELECT TIMESTAMP '1999-01-01 01:23:34'";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("1999-01-01 01:23:34"),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        OneStatementParsesTo("SELECT TIMESTAMP '1999-01-01 01:23:34'", sql);
    }

    [Fact]
    public void parse_literal_timestamp_with_time_zone()
    {
        var sql = "SELECT TIMESTAMPTZ '1999-01-01 01:23:34Z'";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.Timestamp(null, new TimezoneInfo.Tz()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("1999-01-01 01:23:34Z"),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        OneStatementParsesTo("SELECT TIMESTAMPTZ '1999-01-01 01:23:34Z'", sql);
    }

    [Fact]
    public void parse_interval_all()
    {
        // these intervals expressions all work with both variants of INTERVAL

        var sql = "SELECT INTERVAL '1-1' YEAR TO MONTH";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Interval(new Interval(
                Value: new Expr.Value(
                    new Value.SingleQuotedString("1-1").WithEmptySpan()),
                LeadingField: new DateTimeField.Year(),
                LeadingPrecision: null,
                LastField: new DateTimeField.Month(),
                FractionalSecondsPrecision: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT INTERVAL '01:01.01' MINUTE (5) TO SECOND (5)";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Interval(new Interval(
                Value: new Expr.Value(
                    new Value.SingleQuotedString("01:01.01").WithEmptySpan()),
                LeadingField: new DateTimeField.Minute(),
                LeadingPrecision: 5,
                LastField: new DateTimeField.Second(),
                FractionalSecondsPrecision: 5)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT INTERVAL '1' SECOND (5, 4)";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Interval(new Interval(
                Value: new Expr.Value(
                    new Value.SingleQuotedString("1").WithEmptySpan()),
                LeadingField: new DateTimeField.Second(),
                LeadingPrecision: 5,
                LastField: null,
                FractionalSecondsPrecision: 4)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT INTERVAL '10' HOUR";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Interval(new Interval(
                Value: new Expr.Value(
                    new Value.SingleQuotedString("10").WithEmptySpan()),
                LeadingField: new DateTimeField.Hour(),
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT INTERVAL 5 DAY";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Interval(new Interval(
                Value: Expr.MakeValue(TestUtils.Number("5")),
                LeadingField: new DateTimeField.Day(),
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT INTERVAL 5 DAYS";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Interval(new Interval(
                Value: Expr.MakeValue(TestUtils.Number("5")),
                LeadingField: new DateTimeField.Days(),
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = "SELECT INTERVAL '10' HOUR (1)";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Interval(new Interval(
                Value: new Expr.Value(
                    new Value.SingleQuotedString("10").WithEmptySpan()),
                LeadingField: new DateTimeField.Hour(),
                LeadingPrecision: 1,
                LastField: null,
                FractionalSecondsPrecision: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        var result = Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT INTERVAL '1' SECOND TO SECOND"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: SECOND"),
            result.Error);

        result = Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT INTERVAL '10' HOUR (1) TO HOUR (2)"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: ("),
            result.Error);

        VerifiedOnlySelect("SELECT INTERVAL '1' YEAR");
        VerifiedOnlySelect("SELECT INTERVAL '1' MONTH");
        VerifiedOnlySelect("SELECT INTERVAL '1' WEEK");
        VerifiedOnlySelect("SELECT INTERVAL '1' DAY");
        VerifiedOnlySelect("SELECT INTERVAL '1' HOUR");
        VerifiedOnlySelect("SELECT INTERVAL '1' MINUTE");
        VerifiedOnlySelect("SELECT INTERVAL '1' SECOND");
        VerifiedOnlySelect("SELECT INTERVAL '1' YEARS");
        VerifiedOnlySelect("SELECT INTERVAL '1' MONTHS");
        VerifiedOnlySelect("SELECT INTERVAL '1' WEEKS");
        VerifiedOnlySelect("SELECT INTERVAL '1' DAYS");
        VerifiedOnlySelect("SELECT INTERVAL '1' HOURS");
        VerifiedOnlySelect("SELECT INTERVAL '1' MINUTES");
        VerifiedOnlySelect("SELECT INTERVAL '1' SECONDS");
        VerifiedOnlySelect("SELECT INTERVAL '1' YEAR TO MONTH");
        VerifiedOnlySelect("SELECT INTERVAL '1' DAY TO HOUR");
        VerifiedOnlySelect("SELECT INTERVAL '1' DAY TO MINUTE");
        VerifiedOnlySelect("SELECT INTERVAL '1' DAY TO SECOND");
        VerifiedOnlySelect("SELECT INTERVAL '1' HOUR TO MINUTE");
        VerifiedOnlySelect("SELECT INTERVAL '1' HOUR TO SECOND");
        VerifiedOnlySelect("SELECT INTERVAL '1' MINUTE TO SECOND");
        VerifiedOnlySelect("SELECT INTERVAL 1 YEAR");
        VerifiedOnlySelect("SELECT INTERVAL 1 MONTH");
        VerifiedOnlySelect("SELECT INTERVAL 1 WEEK");
        VerifiedOnlySelect("SELECT INTERVAL 1 DAY");
        VerifiedOnlySelect("SELECT INTERVAL 1 HOUR");
        VerifiedOnlySelect("SELECT INTERVAL 1 MINUTE");
        VerifiedOnlySelect("SELECT INTERVAL 1 SECOND");
        VerifiedOnlySelect("SELECT INTERVAL 1 YEARS");
        VerifiedOnlySelect("SELECT INTERVAL 1 MONTHS");
        VerifiedOnlySelect("SELECT INTERVAL 1 WEEKS");
        VerifiedOnlySelect("SELECT INTERVAL 1 DAYS");
        VerifiedOnlySelect("SELECT INTERVAL 1 HOURS");
        VerifiedOnlySelect("SELECT INTERVAL 1 MINUTES");
        VerifiedOnlySelect("SELECT INTERVAL 1 SECONDS");
        VerifiedOnlySelect(
            "SELECT '2 years 15 months 100 weeks 99 hours 123456789 milliseconds'::INTERVAL");
    }

    [Fact]
    public void parse_interval_dont_require_unit()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d.RequireIntervalQualifier);

        var sql = "SELECT INTERVAL '1 DAY'";
        var select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Interval(new Interval(
                Value: new Expr.Value(
                    new Value.SingleQuotedString("1 DAY").WithEmptySpan()),
                LeadingField: null,
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        dialects.VerifiedOnlySelect("SELECT INTERVAL '1 YEAR'");
        dialects.VerifiedOnlySelect("SELECT INTERVAL '1 MONTH'");
        dialects.VerifiedOnlySelect("SELECT INTERVAL '1 DAY'");
        dialects.VerifiedOnlySelect("SELECT INTERVAL '1 HOUR'");
        dialects.VerifiedOnlySelect("SELECT INTERVAL '1 MINUTE'");
        dialects.VerifiedOnlySelect("SELECT INTERVAL '1 SECOND'");
    }

    [Fact]
    public void parse_interval_require_unit()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.RequireIntervalQualifier);
        var sql = "SELECT INTERVAL '1 DAY'";
        var err = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql)).Error;
        Assert.Equal(
            "sql parser error: INTERVAL requires a unit after the literal value",
            err.ToString());
    }

    [Fact]
    public void parse_interval_require_qualifier()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.RequireIntervalQualifier);

        var sql = "SELECT INTERVAL 1 + 1 DAY";
        var select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)),
            new Expr.Interval(new Interval(
                Value: new Expr.BinaryOp(
                    Left: Expr.MakeValue(TestUtils.Number("1")),
                    Op: new BinaryOperator.Plus(),
                    Right: Expr.MakeValue(TestUtils.Number("1"))),
                LeadingField: new DateTimeField.Day(),
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)));

        sql = "SELECT INTERVAL '1' + '1' DAY";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)),
            new Expr.Interval(new Interval(
                Value: new Expr.BinaryOp(
                    Left: new Expr.Value(
                        new Value.SingleQuotedString("1").WithEmptySpan()),
                    Op: new BinaryOperator.Plus(),
                    Right: new Expr.Value(
                        new Value.SingleQuotedString("1").WithEmptySpan())),
                LeadingField: new DateTimeField.Day(),
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)));

        sql = "SELECT INTERVAL '1' + '2' - '3' DAY";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)),
            new Expr.Interval(new Interval(
                Value: new Expr.BinaryOp(
                    Left: new Expr.BinaryOp(
                        Left: new Expr.Value(
                            new Value.SingleQuotedString("1").WithEmptySpan()),
                        Op: new BinaryOperator.Plus(),
                        Right: new Expr.Value(
                            new Value.SingleQuotedString("2").WithEmptySpan())),
                    Op: new BinaryOperator.Minus(),
                    Right: new Expr.Value(
                        new Value.SingleQuotedString("3").WithEmptySpan())),
                LeadingField: new DateTimeField.Day(),
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)));
    }

    [Fact]
    public void parse_interval_disallow_interval_expr()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d.RequireIntervalQualifier);

        var sql = "SELECT INTERVAL '1 DAY'";
        var select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)),
            new Expr.Interval(new Interval(
                Value: new Expr.Value(
                    new Value.SingleQuotedString("1 DAY").WithEmptySpan()),
                LeadingField: null,
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)));

        dialects.VerifiedOnlySelect("SELECT INTERVAL '1 YEAR'");
        dialects.VerifiedOnlySelect("SELECT INTERVAL '1 YEAR' AS one_year");
        dialects.OneStatementParsesTo(
            "SELECT INTERVAL '1 YEAR' one_year",
            "SELECT INTERVAL '1 YEAR' AS one_year");

        sql = "SELECT INTERVAL '1 DAY' > INTERVAL '1 SECOND'";
        select = dialects.VerifiedOnlySelect(sql);
        Assert.Equal(
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)),
            new Expr.BinaryOp(
                Left: new Expr.Interval(new Interval(
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("1 DAY").WithEmptySpan()),
                    LeadingField: null,
                    LeadingPrecision: null,
                    LastField: null,
                    FractionalSecondsPrecision: null)),
                Op: new BinaryOperator.Gt(),
                Right: new Expr.Interval(new Interval(
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("1 SECOND").WithEmptySpan()),
                    LeadingField: null,
                    LeadingPrecision: null,
                    LastField: null,
                    FractionalSecondsPrecision: null))));
    }

    [Fact]
    public void interval_disallow_interval_expr_gt()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d.RequireIntervalQualifier);
        var expr = dialects.VerifiedExpr("INTERVAL '1 second' > x");
        Assert.Equal(
            expr,
            new Expr.BinaryOp(
                Left: new Expr.Interval(new Interval(
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("1 second").WithEmptySpan()),
                    LeadingField: null,
                    LeadingPrecision: null,
                    LastField: null,
                    FractionalSecondsPrecision: null)),
                Op: new BinaryOperator.Gt(),
                Right: new Expr.Identifier(new Ident("x", null, Span.Empty()))));
    }

    [Fact]
    public void interval_disallow_interval_expr_double_colon()
    {
        var dialects = TestUtils.AllDialectsExcept(d => d.RequireIntervalQualifier);
        var expr = dialects.VerifiedExpr("INTERVAL '1 second'::TEXT");
        Assert.Equal(
            expr,
            new Expr.Cast(
                Kind: CastKind.DoubleColon,
                Expr: new Expr.Interval(new Interval(
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("1 second").WithEmptySpan()),
                    LeadingField: null,
                    LeadingPrecision: null,
                    LastField: null,
                    FractionalSecondsPrecision: null)),
                DataType: new DataType.Text(),
                Format: null));
    }

    [Fact]
    public void parse_text_type_modifier_double_colon_cast()
    {
        var expr = VerifiedExpr("ID::TEXT(16777216)");
        Assert.Equal(
            expr,
            new Expr.Cast(
                Kind: CastKind.DoubleColon,
                Expr: new Expr.Identifier(new Ident("ID")),
                DataType: new DataType.Custom(
                    ObjectName.From(Sequence.Of(new Ident("TEXT"))),
                    Sequence.Of("16777216")),
                Format: null));
    }

    [Fact]
    public void parse_interval_and_or_xor()
    {
        var sql = "SELECT col FROM test "
            + "WHERE d3_date > d1_date + INTERVAL '5 days' "
            + "AND d2_date > d1_date + INTERVAL '3 days'";

        var dialects = TestUtils.AllDialectsExcept(d => d.RequireIntervalQualifier);
        var actualAst = dialects.ParseSqlStatements(sql);

        var expectedAst = Sequence.Of<Statement>(new Statement.Query(new Query(
            With: null,
            Body: new SetExpr.Select(new Select(
                SelectToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Distinct: null,
                SelectModifiers: null,
                Top: null,
                TopBeforeDistinct: false,
                Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("col", null, Span.Empty())))),
                Exclude: null,
                Into: null,
                From: Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("test", null, Span.Empty())))),
                    Joins: Sequence<Join>.Empty)),
                LateralViews: Sequence<LateralView>.Empty,
                Prewhere: null,
                Selection: new Expr.BinaryOp(
                    Left: new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("d3_date", null, Span.Empty())),
                        Op: new BinaryOperator.Gt(),
                        Right: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("d1_date", null, Span.Empty())),
                            Op: new BinaryOperator.Plus(),
                            Right: new Expr.Interval(new Interval(
                                Value: new Expr.Value(
                                    new Value.SingleQuotedString("5 days").WithEmptySpan()),
                                LeadingField: null,
                                LeadingPrecision: null,
                                LastField: null,
                                FractionalSecondsPrecision: null)))),
                    Op: new BinaryOperator.And(),
                    Right: new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("d2_date", null, Span.Empty())),
                        Op: new BinaryOperator.Gt(),
                        Right: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("d1_date", null, Span.Empty())),
                            Op: new BinaryOperator.Plus(),
                            Right: new Expr.Interval(new Interval(
                                Value: new Expr.Value(
                                    new Value.SingleQuotedString("3 days").WithEmptySpan()),
                                LeadingField: null,
                                LeadingPrecision: null,
                                LastField: null,
                                FractionalSecondsPrecision: null))))),
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

        Assert.Equal(actualAst, expectedAst);

        dialects.VerifiedStmt(
            "SELECT col FROM test "
            + "WHERE d3_date > d1_date + INTERVAL '5 days' "
            + "AND d2_date > d1_date + INTERVAL '3 days'");

        dialects.VerifiedStmt(
            "SELECT col FROM test "
            + "WHERE d3_date > d1_date + INTERVAL '5 days' "
            + "OR d2_date > d1_date + INTERVAL '3 days'");

        dialects.VerifiedStmt(
            "SELECT col FROM test "
            + "WHERE d3_date > d1_date + INTERVAL '5 days' "
            + "XOR d2_date > d1_date + INTERVAL '3 days'");
    }

    [Fact]
    public void parse_at_timezone()
    {
        var zero = Expr.MakeValue(TestUtils.Number("0"));
        var sql = "SELECT FROM_UNIXTIME(0) AT TIME ZONE 'UTC-06:00' FROM t";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.AtTimeZone(
                Timestamp: TestUtils.Call("FROM_UNIXTIME", new[] { zero }),
                TimeZone: new Expr.Value(
                    new Value.SingleQuotedString("UTC-06:00").WithEmptySpan())),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        sql = """SELECT DATE_FORMAT(FROM_UNIXTIME(0) AT TIME ZONE 'UTC-06:00', '%Y-%m-%dT%H') AS "hour" FROM t""";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.ExprWithAlias(
                Expr: TestUtils.Call(
                    "DATE_FORMAT",
                    new Expr[]
                    {
                        new Expr.AtTimeZone(
                            Timestamp: TestUtils.Call("FROM_UNIXTIME", new[] { zero }),
                            TimeZone: new Expr.Value(
                                new Value.SingleQuotedString("UTC-06:00").WithEmptySpan())),
                        new Expr.Value(
                            new Value.SingleQuotedString("%Y-%m-%dT%H").WithEmptySpan()),
                    }),
                Alias: new Ident("hour", '"', Span.Empty())),
            TestUtils.Only(select.Projection));
    }

    [Fact]
    public void parse_json_keyword()
    {
        // r#"..."# com várias linhas: quebras de linha `\n` explícitas.
        var sql = "SELECT JSON '{\n"
            + "  \"id\": 10,\n"
            + "  \"type\": \"fruit\",\n"
            + "  \"name\": \"apple\",\n"
            + "  \"on_menu\": true,\n"
            + "  \"recipes\":\n"
            + "    {\n"
            + "      \"salads\":\n"
            + "      [\n"
            + "        { \"id\": 2001, \"type\": \"Walnut Apple Salad\" },\n"
            + "        { \"id\": 2002, \"type\": \"Apple Spinach Salad\" }\n"
            + "      ],\n"
            + "      \"desserts\":\n"
            + "      [\n"
            + "        { \"id\": 3001, \"type\": \"Apple Pie\" },\n"
            + "        { \"id\": 3002, \"type\": \"Apple Scones\" },\n"
            + "        { \"id\": 3003, \"type\": \"Apple Crumble\" }\n"
            + "      ]\n"
            + "    }\n"
            + "}'";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.JSON(),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString(
                        "{\n"
                        + "  \"id\": 10,\n"
                        + "  \"type\": \"fruit\",\n"
                        + "  \"name\": \"apple\",\n"
                        + "  \"on_menu\": true,\n"
                        + "  \"recipes\":\n"
                        + "    {\n"
                        + "      \"salads\":\n"
                        + "      [\n"
                        + "        { \"id\": 2001, \"type\": \"Walnut Apple Salad\" },\n"
                        + "        { \"id\": 2002, \"type\": \"Apple Spinach Salad\" }\n"
                        + "      ],\n"
                        + "      \"desserts\":\n"
                        + "      [\n"
                        + "        { \"id\": 3001, \"type\": \"Apple Pie\" },\n"
                        + "        { \"id\": 3002, \"type\": \"Apple Scones\" },\n"
                        + "        { \"id\": 3003, \"type\": \"Apple Crumble\" }\n"
                        + "      ]\n"
                        + "    }\n"
                        + "}"),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_typed_strings()
    {
        var expr = VerifiedExpr("""JSON '{"foo":"bar"}'""");
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.JSON(),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("""{"foo":"bar"}"""),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            expr);

        if (expr is Expr.TypedString { Value: { UsesOdbcSyntax: false } typedString })
        {
            Assert.Equal(new DataType.JSON(), typedString.DataType);
            Assert.Equal("""{"foo":"bar"}""", typedString.Value.IntoString());
        }
    }

    [Fact]
    public void parse_bignumeric_keyword()
    {
        var sql = """SELECT BIGNUMERIC '0'""";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("""0"""),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        VerifiedStmt("SELECT BIGNUMERIC '0'");

        sql = """SELECT BIGNUMERIC '123456'""";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("""123456"""),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        VerifiedStmt("SELECT BIGNUMERIC '123456'");

        sql = """SELECT BIGNUMERIC '-3.14'""";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("""-3.14"""),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        VerifiedStmt("SELECT BIGNUMERIC '-3.14'");

        sql = """SELECT BIGNUMERIC '-0.54321'""";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("""-0.54321"""),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        VerifiedStmt("SELECT BIGNUMERIC '-0.54321'");

        sql = """SELECT BIGNUMERIC '1.23456e05'""";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("""1.23456e05"""),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        VerifiedStmt("SELECT BIGNUMERIC '1.23456e05'");

        sql = """SELECT BIGNUMERIC '-9.876e-3'""";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.TypedString(new TypedString(
                DataType: new DataType.BigNumeric(new ExactNumberInfo.None()),
                Value: new ValueWithSpan(
                    new Value.SingleQuotedString("""-9.876e-3"""),
                    Span.Empty()),
                UsesOdbcSyntax: false)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
        VerifiedStmt("SELECT BIGNUMERIC '-9.876e-3'");
    }

    [Fact]
    public void parse_simple_math_expr_plus()
    {
        var sql = "SELECT a + b, 2 + a, 2.5 + a, a_f + b_f, 2 + a_f, 2.5 + a_f FROM c";
        VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_simple_math_expr_minus()
    {
        var sql = "SELECT a - b, 2 - a, 2.5 - a, a_f - b_f, 2 - a_f, 2.5 - a_f FROM c";
        VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_table_function()
    {
        var select = VerifiedOnlySelect("SELECT * FROM TABLE(FUN('1')) AS a");

        switch (TestUtils.Only(select.From).Relation)
        {
            case TableFactor.TableFunction tableFunction:
                Assert.Equal(
                    TestUtils.Call(
                        "FUN",
                        new Expr[]
                        {
                            new Expr.Value(
                                new Value.SingleQuotedString("1").WithEmptySpan()),
                        }),
                    tableFunction.Expr);
                Assert.Equal(tableFunction.Alias, TestUtils.TableAlias(true, "a"));
                break;
            default:
                Assert.Fail("Expecting TableFactor::TableFunction");
                break;
        }

        var res = Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT * FROM TABLE '1' AS a"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: (, found: '1'"),
            res.Error);

        res = Assert.Throws<ParserException>(() => ParseSqlStatements("SELECT * FROM TABLE (FUN(a) AS a"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: ), found: AS"),
            res.Error);
    }

    [Fact]
    public void parse_select_with_alias_and_column_defs()
    {
        var sql = """SELECT * FROM jsonb_to_record('{"a": "x", "b": 2}'::JSONB) AS x (a TEXT, b INT)""";
        var select = VerifiedOnlySelect(sql);

        switch (TestUtils.Only(select.From))
        {
            case { Relation: TableFactor.Table { Alias: { } alias } }:
                Assert.Equal("x", alias.Name.Value);
                Assert.Equal(
                    alias.Columns,
                    Sequence.Of(
                        new TableAliasColumnDef(
                            Name: new Ident("a"),
                            DataType: new DataType.Text()),
                        new TableAliasColumnDef(
                            Name: new Ident("b"),
                            DataType: new DataType.Int(null))));
                break;
            default:
                throw new InvalidOperationException(
                    $"{Unreachable}: Expecting only TableWithJoins with TableFactor::Table, got {string.Join(", ", select.From)}");
        }
    }

    [Fact]
    public void parse_unnest()
    {
        var sql = "SELECT UNNEST(make_array(1, 2, 3))";
        OneStatementParsesTo(sql, sql);
        sql = "SELECT UNNEST(make_array(1, 2, 3), make_array(4, 5))";
        OneStatementParsesTo(sql, sql);
    }

    [Fact]
    public void parse_unnest_in_from_clause()
    {
        static void Chk(
            string arrayExprs,
            bool alias,
            bool withOffset,
            bool withOffsetAlias,
            TestedDialects dialects,
            Sequence<TableWithJoins> want)
        {
            var sql = string.Format(
                "SELECT * FROM UNNEST({0}){1}{2}{3}",
                arrayExprs,
                alias ? " AS numbers" : "",
                withOffset ? " WITH OFFSET" : "",
                withOffsetAlias ? " AS with_offset_alias" : "");
            var select = dialects.VerifiedOnlySelect(sql);
            Assert.Equal(select.From, want);
        }
        var dialects = new TestedDialects(new List<Dialect>
        {
            new BigQueryDialect(),
            new GenericDialect(),
        });
        // 1. both Alias and WITH OFFSET clauses.
        Chk(
            "expr",
            true,
            true,
            false,
            dialects,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.UNNEST(
                    Alias: TestUtils.TableAlias(true, "numbers"),
                    ArrayExprs: Sequence.Of<Expr>(new Expr.Identifier(new Ident("expr"))),
                    WithOffset: true,
                    WithOffsetAlias: null,
                    WithOrdinality: false),
                Joins: Sequence<Join>.Empty)));
        // 2. neither Alias nor WITH OFFSET clause.
        Chk(
            "expr",
            false,
            false,
            false,
            dialects,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.UNNEST(
                    Alias: null,
                    ArrayExprs: Sequence.Of<Expr>(new Expr.Identifier(new Ident("expr"))),
                    WithOffset: false,
                    WithOffsetAlias: null,
                    WithOrdinality: false),
                Joins: Sequence<Join>.Empty)));
        // 3. Alias but no WITH OFFSET clause.
        Chk(
            "expr",
            false,
            true,
            false,
            dialects,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.UNNEST(
                    Alias: null,
                    ArrayExprs: Sequence.Of<Expr>(new Expr.Identifier(new Ident("expr"))),
                    WithOffset: true,
                    WithOffsetAlias: null,
                    WithOrdinality: false),
                Joins: Sequence<Join>.Empty)));
        // 4. WITH OFFSET but no Alias.
        Chk(
            "expr",
            true,
            false,
            false,
            dialects,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.UNNEST(
                    Alias: TestUtils.TableAlias(true, "numbers"),
                    ArrayExprs: Sequence.Of<Expr>(new Expr.Identifier(new Ident("expr"))),
                    WithOffset: false,
                    WithOffsetAlias: null,
                    WithOrdinality: false),
                Joins: Sequence<Join>.Empty)));
        // 5. Simple array
        Chk(
            "make_array(1, 2, 3)",
            false,
            false,
            false,
            dialects,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.UNNEST(
                    Alias: null,
                    ArrayExprs: Sequence.Of(TestUtils.Call(
                        "make_array",
                        new[]
                        {
                            Expr.MakeValue(TestUtils.Number("1")),
                            Expr.MakeValue(TestUtils.Number("2")),
                            Expr.MakeValue(TestUtils.Number("3")),
                        })),
                    WithOffset: false,
                    WithOffsetAlias: null,
                    WithOrdinality: false),
                Joins: Sequence<Join>.Empty)));
        // 6. Multiple arrays
        Chk(
            "make_array(1, 2, 3), make_array(5, 6)",
            false,
            false,
            false,
            dialects,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.UNNEST(
                    Alias: null,
                    ArrayExprs: Sequence.Of(
                        TestUtils.Call(
                            "make_array",
                            new[]
                            {
                                Expr.MakeValue(TestUtils.Number("1")),
                                Expr.MakeValue(TestUtils.Number("2")),
                                Expr.MakeValue(TestUtils.Number("3")),
                            }),
                        TestUtils.Call(
                            "make_array",
                            new[] { Expr.MakeValue(TestUtils.Number("5")), Expr.MakeValue(TestUtils.Number("6")) })),
                    WithOffset: false,
                    WithOffsetAlias: null,
                    WithOrdinality: false),
                Joins: Sequence<Join>.Empty)));
    }

    [Fact]
    public void parse_parens()
    {
        var sql = "(a + b) - (c + d)";
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Nested(new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("a")),
                    Op: new BinaryOperator.Plus(),
                    Right: new Expr.Identifier(new Ident("b")))),
                Op: new BinaryOperator.Minus(),
                Right: new Expr.Nested(new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("c")),
                    Op: new BinaryOperator.Plus(),
                    Right: new Expr.Identifier(new Ident("d"))))),
            VerifiedExpr(sql));
    }

    [Fact]
    public void parse_searched_case_expr()
    {
        var sql = "SELECT CASE WHEN bar IS NULL THEN 'null' WHEN bar = 0 THEN '=0' WHEN bar >= 0 THEN '>=0' ELSE '<0' END FROM foo";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Case(
                CaseToken: AttachedToken.Empty(),
                EndToken: AttachedToken.Empty(),
                Operand: null,
                Conditions: Sequence.Of(
                    new CaseWhen(
                        Condition: new Expr.IsNull(new Expr.Identifier(new Ident("bar"))),
                        Result: Expr.MakeValue(new Value.SingleQuotedString("null"))),
                    new CaseWhen(
                        Condition: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("bar")),
                            Op: new BinaryOperator.Eq(),
                            Right: Expr.MakeValue(TestUtils.Number("0"))),
                        Result: Expr.MakeValue(new Value.SingleQuotedString("=0"))),
                    new CaseWhen(
                        Condition: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("bar")),
                            Op: new BinaryOperator.GtEq(),
                            Right: Expr.MakeValue(TestUtils.Number("0"))),
                        Result: Expr.MakeValue(new Value.SingleQuotedString(">=0")))),
                ElseResult: Expr.MakeValue(new Value.SingleQuotedString(
                    "<0"))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_simple_case_expr()
    {
        // ANSI calls a CASE expression with an operand "<simple case>"
        var sql = "SELECT CASE foo WHEN 1 THEN 'Y' ELSE 'N' END";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Case(
                CaseToken: AttachedToken.Empty(),
                EndToken: AttachedToken.Empty(),
                Operand: new Expr.Identifier(new Ident("foo")),
                Conditions: Sequence.Of(new CaseWhen(
                    Condition: Expr.MakeValue(TestUtils.Number("1")),
                    Result: Expr.MakeValue(new Value.SingleQuotedString("Y")))),
                ElseResult: Expr.MakeValue(new Value.SingleQuotedString(
                    "N"))),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_from_advanced()
    {
        var sql = "SELECT * FROM fn(1, 2) AS foo, schema.bar AS bar WITH (NOLOCK)";
        _ = VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_nullary_table_valued_function()
    {
        var sql = "SELECT * FROM fn()";
        _ = VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_implicit_join()
    {
        var sql = "SELECT * FROM t1, t2";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            Sequence.Of(
                new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t1")))),
                    Joins: Sequence<Join>.Empty),
                new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t2")))),
                    Joins: Sequence<Join>.Empty)),
            select.From);

        sql = "SELECT * FROM t1a NATURAL JOIN t1b, t2a NATURAL JOIN t2b";
        select = VerifiedOnlySelect(sql);
        Assert.Equal(
            Sequence.Of(
                new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t1a")))),
                    Joins: Sequence.Of(new Join(
                        Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t1b")))),
                        Global: false,
                        JoinOperator: new JoinOperator.Join(new JoinConstraint.Natural())))),
                new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t2a")))),
                    Joins: Sequence.Of(new Join(
                        Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t2b")))),
                        Global: false,
                        JoinOperator: new JoinOperator.Join(new JoinConstraint.Natural()))))),
            select.From);
    }

    [Fact]
    public void parse_cross_join()
    {
        var sql = "SELECT * FROM t1 CROSS JOIN t2";
        var select = VerifiedOnlySelect(sql);
        Assert.Equal(
            new Join(
                Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t2")))),
                Global: false,
                JoinOperator: new JoinOperator.CrossJoin(new JoinConstraint.None())),
            TestUtils.Only(TestUtils.Only(select.From).Joins));
    }

    [Fact]
    public void parse_cross_join_constraint()
    {
        static Join JoinWithConstraint(JoinConstraint constraint) =>
            new Join(
                Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t2")))),
                Global: false,
                JoinOperator: new JoinOperator.CrossJoin(constraint));

        static void TestConstraint(string sql, JoinConstraint constraint)
        {
            var dialect = TestUtils.AllDialectsWhere(d => d.SupportsCrossJoinConstraint);
            var select = dialect.VerifiedOnlySelect(sql);
            Assert.Equal(
                JoinWithConstraint(constraint),
                TestUtils.Only(TestUtils.Only(select.From).Joins));
        }

        TestConstraint(
            "SELECT * FROM t1 CROSS JOIN t2 ON a = b",
            new JoinConstraint.On(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("a")),
                Op: new BinaryOperator.Eq(),
                Right: new Expr.Identifier(new Ident("b")))));
        TestConstraint(
            "SELECT * FROM t1 CROSS JOIN t2 USING(a)",
            new JoinConstraint.Using(Sequence.Of(ObjectName.From(Sequence.Of(new Ident("a"))))));
    }

    [Fact]
    public void parse_joins_on()
    {
        static Join JoinWithConstraint(
            string relation,
            TableAlias? alias,
            bool global,
            Func<JoinConstraint, JoinOperator> f) =>
            new Join(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident(relation))),
                    Alias: alias,
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    Partitions: Sequence<Ident>.Empty,
                    WithOrdinality: false,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Global: global,
                JoinOperator: f(new JoinConstraint.On(new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("c1")),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.Identifier(new Ident("c2"))))));
        // Test parsing of aliases
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 JOIN t2 AS foo ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                TestUtils.TableAlias(true, "foo"),
                false,
                c => new JoinOperator.Join(c))));
        OneStatementParsesTo(
            "SELECT * FROM t1 JOIN t2 foo ON c1 = c2",
            "SELECT * FROM t1 JOIN t2 foo ON c1 = c2");
        // Test parsing of different join operators
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, false, c => new JoinOperator.Join(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 INNER JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, false, c => new JoinOperator.Inner(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 LEFT JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, false, c => new JoinOperator.Left(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 LEFT OUTER JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                null,
                false,
                c => new JoinOperator.LeftOuter(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 RIGHT JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, false, c => new JoinOperator.Right(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 RIGHT OUTER JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                null,
                false,
                c => new JoinOperator.RightOuter(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 SEMI JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, false, c => new JoinOperator.Semi(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 LEFT SEMI JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                null,
                false,
                c => new JoinOperator.LeftSemi(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 RIGHT SEMI JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                null,
                false,
                c => new JoinOperator.RightSemi(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 ANTI JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, false, c => new JoinOperator.Anti(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 LEFT ANTI JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                null,
                false,
                c => new JoinOperator.LeftAnti(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 RIGHT ANTI JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                null,
                false,
                c => new JoinOperator.RightAnti(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 FULL JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                null,
                false,
                c => new JoinOperator.FullOuter(c))));

        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 GLOBAL FULL JOIN t2 ON c1 = c2").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                null,
                true,
                c => new JoinOperator.FullOuter(c))));
    }

    [Fact]
    public void parse_joins_using()
    {
        static Join JoinWithConstraint(
            string relation,
            TableAlias? alias,
            Func<JoinConstraint, JoinOperator> f) =>
            new Join(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident(relation))),
                    Alias: alias,
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    Partitions: Sequence<Ident>.Empty,
                    WithOrdinality: false,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Global: false,
                JoinOperator: f(new JoinConstraint.Using(Sequence.Of(ObjectName.From(Sequence.Of(
                    new Ident("c1")))))));
        // Test parsing of aliases
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 JOIN t2 AS foo USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint(
                "t2",
                TestUtils.TableAlias(true, "foo"),
                c => new JoinOperator.Join(c))));
        OneStatementParsesTo(
            "SELECT * FROM t1 JOIN t2 AS foo USING(c1)",
            "SELECT * FROM t1 JOIN t2 AS foo USING(c1)");
        OneStatementParsesTo(
            "SELECT * FROM t1 JOIN t2 foo USING(c1)",
            "SELECT * FROM t1 JOIN t2 foo USING(c1)");
        // Test parsing of different join operators
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.Join(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 INNER JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.Inner(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 LEFT JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.Left(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 LEFT OUTER JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.LeftOuter(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 RIGHT JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.Right(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 RIGHT OUTER JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.RightOuter(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 SEMI JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.Semi(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 LEFT SEMI JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.LeftSemi(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 RIGHT SEMI JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.RightSemi(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 ANTI JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.Anti(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 LEFT ANTI JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.LeftAnti(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 RIGHT ANTI JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.RightAnti(c))));
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 FULL JOIN t2 USING(c1)").From).Joins,
            Sequence.Of(JoinWithConstraint("t2", null, c => new JoinOperator.FullOuter(c))));
        VerifiedStmt("SELECT * FROM tbl1 AS t1 JOIN tbl2 AS t2 USING(t2.col1)");
    }

    [Fact]
    public void parse_natural_join()
    {
        static Join NaturalJoin(Func<JoinConstraint, JoinOperator> f, TableAlias? alias) =>
            new Join(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("t2"))),
                    Alias: alias,
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    Partitions: Sequence<Ident>.Empty,
                    WithOrdinality: false,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Global: false,
                JoinOperator: f(new JoinConstraint.Natural()));

        // unspecified join
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 NATURAL JOIN t2").From).Joins,
            Sequence.Of(NaturalJoin(c => new JoinOperator.Join(c), null)));

        // inner join explicitly
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 NATURAL INNER JOIN t2").From).Joins,
            Sequence.Of(NaturalJoin(c => new JoinOperator.Inner(c), null)));

        // left join explicitly
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 NATURAL LEFT JOIN t2").From).Joins,
            Sequence.Of(NaturalJoin(c => new JoinOperator.Left(c), null)));

        // left outer join explicitly
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 NATURAL LEFT OUTER JOIN t2").From).Joins,
            Sequence.Of(NaturalJoin(c => new JoinOperator.LeftOuter(c), null)));

        // right join explicitly
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 NATURAL RIGHT JOIN t2").From).Joins,
            Sequence.Of(NaturalJoin(c => new JoinOperator.Right(c), null)));

        // right outer join explicitly
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 NATURAL RIGHT OUTER JOIN t2").From).Joins,
            Sequence.Of(NaturalJoin(c => new JoinOperator.RightOuter(c), null)));

        // full join explicitly
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 NATURAL FULL JOIN t2").From).Joins,
            Sequence.Of(NaturalJoin(c => new JoinOperator.FullOuter(c), null)));

        // natural join another table with alias
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect("SELECT * FROM t1 NATURAL JOIN t2 AS t3").From).Joins,
            Sequence.Of(NaturalJoin(c => new JoinOperator.Join(c), TestUtils.TableAlias(true, "t3"))));

        var sql = "SELECT * FROM t1 natural";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: a join type after NATURAL, found: EOF"),
            Assert.Throws<ParserException>(() => ParseSqlStatements(sql)).Error);
    }

    [Fact]
    public void parse_complex_join()
    {
        var sql = "SELECT c1, c2 FROM t1, t4 JOIN t2 ON t2.c = t1.c LEFT JOIN t3 USING(q, c) WHERE t4.c = t1.c";
        VerifiedOnlySelect(sql);
    }

    [Fact]
    public void parse_join_nesting()
    {
        var sql = "SELECT * FROM a NATURAL JOIN (b NATURAL JOIN (c NATURAL JOIN d NATURAL JOIN e)) "
            + "NATURAL JOIN (f NATURAL JOIN (g NATURAL JOIN h))";
        Assert.Equal(
            TestUtils.Only(VerifiedOnlySelect(sql).From).Joins,
            Sequence.Of(
                TestUtils.Join(Nest(TestUtils.Table("b"), Nest(TestUtils.Table("c"), TestUtils.Table("d"), TestUtils.Table("e")))),
                TestUtils.Join(Nest(TestUtils.Table("f"), Nest(TestUtils.Table("g"), TestUtils.Table("h"))))));

        sql = "SELECT * FROM (a NATURAL JOIN b) NATURAL JOIN c";
        var select = VerifiedOnlySelect(sql);
        var from = TestUtils.Only(select.From);
        Assert.Equal(from.Relation, Nest(TestUtils.Table("a"), TestUtils.Table("b")));
        Assert.Equal(from.Joins, Sequence.Of(TestUtils.Join(TestUtils.Table("c"))));

        sql = "SELECT * FROM (((a NATURAL JOIN b)))";
        select = VerifiedOnlySelect(sql);
        from = TestUtils.Only(select.From);
        Assert.Equal(from.Relation, Nest(Nest(Nest(TestUtils.Table("a"), TestUtils.Table("b")))));
        Assert.Equal(from.Joins, Sequence<Join>.Empty);

        sql = "SELECT * FROM a NATURAL JOIN (((b NATURAL JOIN c)))";
        select = VerifiedOnlySelect(sql);
        from = TestUtils.Only(select.From);
        Assert.Equal(from.Relation, TestUtils.Table("a"));
        Assert.Equal(
            from.Joins,
            Sequence.Of(TestUtils.Join(Nest(Nest(Nest(TestUtils.Table("b"), TestUtils.Table("c")))))));

        sql = "SELECT * FROM (a NATURAL JOIN b) AS c";
        select = VerifiedOnlySelect(sql);
        from = TestUtils.Only(select.From);
        Assert.Equal(
            from.Relation,
            new TableFactor.NestedJoin(
                TableWithJoins: new TableWithJoins(
                    Relation: TestUtils.Table("a"),
                    Joins: Sequence.Of(TestUtils.Join(TestUtils.Table("b")))),
                Alias: TestUtils.TableAlias(true, "c")));
        Assert.Equal(from.Joins, Sequence<Join>.Empty);
    }

    [Fact]
    public void parse_join_syntax_variants()
    {
        VerifiedStmt("SELECT c1 FROM t1 JOIN t2 USING(c1)");
        VerifiedStmt("SELECT c1 FROM t1 INNER JOIN t2 USING(c1)");
        VerifiedStmt("SELECT c1 FROM t1 LEFT JOIN t2 USING(c1)");
        VerifiedStmt("SELECT c1 FROM t1 LEFT OUTER JOIN t2 USING(c1)");
        VerifiedStmt("SELECT c1 FROM t1 RIGHT JOIN t2 USING(c1)");
        VerifiedStmt("SELECT c1 FROM t1 RIGHT OUTER JOIN t2 USING(c1)");
        OneStatementParsesTo(
            "SELECT c1 FROM t1 FULL OUTER JOIN t2 USING(c1)",
            "SELECT c1 FROM t1 FULL JOIN t2 USING(c1)");

        var dialects = TestUtils.AllDialectsExcept(d => d.IsTableAlias(Keyword.OUTER, new Parser(d)));
        var res = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("SELECT * FROM a OUTER JOIN b ON 1"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: APPLY, found: JOIN"),
            res.Error);
    }

    [Fact]
    public void parse_ctes()
    {
        var cteSqls = new List<string> { "SELECT 1 AS foo", "SELECT 2 AS bar" };
        var @with = string.Format(
            "WITH a AS ({0}), b AS ({1}) SELECT foo + bar FROM a, b",
            cteSqls[0], cteSqls[1]);

        static void AssertCtesInSelect(IReadOnlyList<string> expected, Query sel)
        {
            for (var i = 0; i < expected.Count; i++)
            {
                var exp = expected[i];
                // let Cte { alias, query, .. } = &sel.with.as_ref().unwrap().cte_tables[i];
                var cte = sel.With!.CteTables[i];
                var alias = cte.Alias;
                var query = cte.Query;
                Assert.Equal(exp, query.ToString());
                Assert.False(alias.Explicit);
                Assert.Equal(
                    i == 0
                        ? new Ident("a")
                        : new Ident("b"),
                    alias.Name);
                Assert.True(alias.Columns.IsEmpty);
            }
        }

        // Top-level CTE
        AssertCtesInSelect(cteSqls, VerifiedQuery(@with));
        // CTE in a subquery
        var sql = $"SELECT ({@with})";
        var select = VerifiedOnlySelect(sql);
        switch (TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)))
        {
            case Expr.Subquery { Value: var subquery }:
                AssertCtesInSelect(cteSqls, subquery);
                break;
            default:
                Assert.Fail("Expected: subquery");
                break;
        }
        // CTE in a derived table
        sql = $"SELECT * FROM ({@with})";
        select = VerifiedOnlySelect(sql);
        switch (TestUtils.Only(select.From).Relation)
        {
            case TableFactor.Derived { Subquery: var subquery }:
                AssertCtesInSelect(cteSqls, subquery);
                break;
            default:
                Assert.Fail("Expected: derived table");
                break;
        }
        // CTE in a view
        sql = $"CREATE VIEW v AS {@with}";
        switch (VerifiedStmt(sql))
        {
            case Statement.CreateView { Value: var createView }:
                AssertCtesInSelect(cteSqls, createView.Query);
                break;
            default:
                Assert.Fail("Expected: CREATE VIEW");
                break;
        }
        // CTE in a CTE...
        sql = $"WITH outer_cte AS ({@with}) SELECT * FROM outer_cte";
        var query = VerifiedQuery(sql);
        AssertCtesInSelect(cteSqls, TestUtils.Only(query.With!.CteTables).Query);
    }

    [Fact]
    public void parse_cte_renamed_columns()
    {
        var sql = "WITH cte (col1, col2) AS (SELECT foo, bar FROM baz) SELECT * FROM cte";
        var query = TestUtils.AllDialects().VerifiedQuery(sql);
        Assert.Equal(
            Sequence.Of(
                TableAliasColumnDef.FromName("col1"),
                TableAliasColumnDef.FromName("col2")),
            query
                .With!
                .CteTables
                .First!
                .Alias
                .Columns);
    }

    // Macro `nest!` de tests/test_utils/mod.rs:
    // `TableFactor::NestedJoin { table_with_joins: Box::new(TableWithJoins { relation: $base, joins: vec![$(join($join)),*] }), alias: None }`.
    private static TableFactor Nest(TableFactor @base, params TableFactor[] joins) =>
        new TableFactor.NestedJoin(
            TableWithJoins: new TableWithJoins(
                Relation: @base,
                Joins: joins.Select(TestUtils.Join).ToSequence()),
            Alias: null);

    // Funções-atalho do arquivo Rust (tests/sqlparser_common.rs, linhas 10994–11020).

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
