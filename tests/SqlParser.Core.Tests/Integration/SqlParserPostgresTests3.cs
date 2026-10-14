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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_postgres.rs (linhas 6828–9920).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C8.md):
// - Classe própria (não partial) para este trecho do arquivo Rust; os helpers do arquivo usados aqui
//   (`pg()`, `pg_and_generic()`, `check_arrow_precedence`) foram copiados como métodos privados.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests/SpansTests).
// - `assert_eq!(n, v.len())` usa `Assert.Equal(n, v.Count)`, como no Rust; o xUnit2013 (que sugere
//   Assert.Single/Assert.Empty) fica suprimido neste arquivo.
// - `assert!(s.contains(x))` sobre string vira `Assert.Contains(x, s)` (regra xUnit2009).
// - `panic!(..)`/`unreachable!(..)` num braço de `match` → `default: throw new InvalidOperationException(..)`.
// - Strings `r#"..."#` com várias linhas viram concatenações com `\n` explícito, para não depender do
//   fim de linha do arquivo nem da remoção de indentação dos raw string literals do C#.
#pragma warning disable xUnit2013
public class SqlParserPostgresTests3
{
    // fn pg() -> TestedDialects
    private static TestedDialects Pg() =>
        new TestedDialects(new List<Dialect> { new PostgreSqlDialect() });

    // fn pg_and_generic() -> TestedDialects
    private static TestedDialects PgAndGeneric() =>
        new TestedDialects(new List<Dialect>
        {
            new PostgreSqlDialect(),
            new GenericDialect(),
        });

    // `ObjectName::from(vec![Ident::new(a), ..])`
    private static ObjectName Name(params string[] parts) =>
        ObjectName.From(parts.Select(p => new Ident(p)).ToSequence());

    /// <summary>
    /// While in the parse_create_trigger test we test the full syntax of the CREATE TRIGGER statement,
    /// here we test the invalid cases of the CREATE TRIGGER statement which should cause an appropriate
    /// error to be returned.
    /// </summary>
    [Fact]
    public void parse_create_trigger_invalid_cases()
    {
        // Test invalid cases for the CREATE TRIGGER statement
        var invalidCases = new (string Sql, string ExpectedError)[]
        {
            (
                "CREATE TRIGGER check_update BEFORE UPDATE ON accounts FUNCTION check_account_update",
                "Expected: an SQL statement, found: FUNCTION"
            ),
            (
                "CREATE TRIGGER check_update TOMORROW UPDATE ON accounts EXECUTE FUNCTION check_account_update",
                "Expected: one of INSERT or UPDATE or DELETE or TRUNCATE, found: TOMORROW"
            ),
            (
                "CREATE TRIGGER check_update BEFORE SAVE ON accounts EXECUTE FUNCTION check_account_update",
                "Expected: one of INSERT or UPDATE or DELETE or TRUNCATE, found: SAVE"
            ),
        };

        foreach (var (sql, expectedError) in invalidCases)
        {
            var ex = Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(sql));
            Assert.Equal(
                $"sql parser error: {expectedError}",
                ex.Error.ToString());
        }
    }

    [Fact]
    public void parse_drop_trigger()
    {
        foreach (var ifExists in new[] { true, false })
        {
            foreach (var option in new ReferentialAction?[]
            {
                null,
                new ReferentialAction.Cascade(),
                new ReferentialAction.Restrict(),
            })
            {
                var sql =
                    $"DROP TRIGGER{(ifExists ? " IF EXISTS" : "")} check_update ON table_name{(option is { } o ? $" {o}" : "")}";
                Assert.Equal(
                    Pg().VerifiedStmt(sql),
                    new Statement.DropTrigger(new DropTrigger(
                        IfExists: ifExists,
                        TriggerName: Name("check_update"),
                        TableName: Name("table_name"),
                        Option: option)));
            }
        }
    }

    [Fact]
    public void parse_drop_trigger_invalid_cases()
    {
        // Test invalid cases for the DROP TRIGGER statement
        var invalidCases = new (string Sql, string ExpectedError)[]
        {
            (
                "DROP TRIGGER check_update ON table_name CASCADE RESTRICT",
                "Expected: end of statement, found: RESTRICT"
            ),
            (
                "DROP TRIGGER check_update ON table_name CASCADE CASCADE",
                "Expected: end of statement, found: CASCADE"
            ),
            (
                "DROP TRIGGER check_update ON table_name CASCADE CASCADE CASCADE",
                "Expected: end of statement, found: CASCADE"
            ),
        };

        foreach (var (sql, expectedError) in invalidCases)
        {
            var ex = Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(sql));
            Assert.Equal(
                $"sql parser error: {expectedError}",
                ex.Error.ToString());
        }
    }

    [Fact]
    public void parse_trigger_related_functions()
    {
        // First we define all parts of the trigger definition,
        // including the table creation, the function creation, the trigger creation and the trigger drop.
        // The following example is taken from the PostgreSQL documentation <https://www.postgresql.org/docs/current/plpgsql-trigger.html>

        var sqlTableCreation =
            "\n" +
            "    CREATE TABLE emp (\n" +
            "        empname           text,\n" +
            "        salary            integer,\n" +
            "        last_date         timestamp,\n" +
            "        last_user         text\n" +
            "    );\n" +
            "    ";

        var sqlCreateFunction =
            "\n" +
            "    CREATE FUNCTION emp_stamp() RETURNS trigger AS $emp_stamp$\n" +
            "        BEGIN\n" +
            "            -- Check that empname and salary are given\n" +
            "            IF NEW.empname IS NULL THEN\n" +
            "                RAISE EXCEPTION 'empname cannot be null';\n" +
            "            END IF;\n" +
            "            IF NEW.salary IS NULL THEN\n" +
            "                RAISE EXCEPTION '% cannot have null salary', NEW.empname;\n" +
            "            END IF;\n" +
            "\n" +
            "            -- Who works for us when they must pay for it?\n" +
            "            IF NEW.salary < 0 THEN\n" +
            "                RAISE EXCEPTION '% cannot have a negative salary', NEW.empname;\n" +
            "            END IF;\n" +
            "\n" +
            "            -- Remember who changed the payroll when\n" +
            "            NEW.last_date := current_timestamp;\n" +
            "            NEW.last_user := current_user;\n" +
            "            RETURN NEW;\n" +
            "        END;\n" +
            "    $emp_stamp$ LANGUAGE plpgsql;\n" +
            "    ";

        var sqlCreateTrigger =
            "\n" +
            "    CREATE TRIGGER emp_stamp BEFORE INSERT OR UPDATE ON emp\n" +
            "        FOR EACH ROW EXECUTE FUNCTION emp_stamp();\n" +
            "    ";

        var sqlDropTrigger =
            "\n" +
            "    DROP TRIGGER emp_stamp ON emp;\n" +
            "    ";

        // Now we parse the statements and check if they are parsed correctly.
        var statements = Pg()
            .ParseSqlStatements(
                $"{sqlTableCreation}{sqlCreateFunction}{sqlCreateTrigger}{sqlDropTrigger}");

        Assert.Equal(4, statements.Count);
        // statements.pop() x4
        var dropTrigger = statements[3];
        var createTrigger = statements[2];
        var createFunction = statements[1];
        var createTableStmt = statements[0];

        // Check the first statement
        var createTable = createTableStmt switch
        {
            Statement.CreateTable c => c.Value,
            _ => throw new InvalidOperationException("Expected CreateTable statement"),
        };

        Assert.Equal(
            createTable,
            new CreateTable(
                OrReplace: false,
                Temporary: false,
                Unlogged: false,
                External: false,
                Global: null,
                Dynamic: false,
                IfNotExists: false,
                Transient: false,
                Volatile: false,
                Iceberg: false,
                Snapshot: false,
                Name: Name("emp"),
                Columns: Sequence.Of(
                    new ColumnDef(
                        Name: new Ident("empname"),
                        DataType: new DataType.Text(),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    new ColumnDef(
                        Name: new Ident("salary"),
                        DataType: new DataType.Integer(null),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    new ColumnDef(
                        Name: new Ident("last_date"),
                        DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    new ColumnDef(
                        Name: new Ident("last_user"),
                        DataType: new DataType.Text(),
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
                WithData: null));

        // Check the second statement

        Assert.Equal(
            createFunction,
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: false,
                Temporary: false,
                IfNotExists: false,
                Name: Name("emp_stamp"),
                Args: Sequence<OperateFunctionArg>.Empty,
                ReturnType: new FunctionReturnType.DataType(new DataType.Trigger()),
                FunctionBody: new CreateFunctionBody.AsBeforeOptions(
                    Body: Expr.MakeValue(
                        new Value.DollarQuotedString(
                            new DollarQuotedString(
                                Value: "\n        BEGIN\n            -- Check that empname and salary are given\n            IF NEW.empname IS NULL THEN\n                RAISE EXCEPTION 'empname cannot be null';\n            END IF;\n            IF NEW.salary IS NULL THEN\n                RAISE EXCEPTION '% cannot have null salary', NEW.empname;\n            END IF;\n\n            -- Who works for us when they must pay for it?\n            IF NEW.salary < 0 THEN\n                RAISE EXCEPTION '% cannot have a negative salary', NEW.empname;\n            END IF;\n\n            -- Remember who changed the payroll when\n            NEW.last_date := current_timestamp;\n            NEW.last_user := current_user;\n            RETURN NEW;\n        END;\n    ",
                                Tag: "emp_stamp"))),
                    LinkSymbol: null),
                Behavior: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                Using: null,
                Language: new Ident("plpgsql"),
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));

        // Check the third statement

        Assert.Equal(
            createTrigger,
            new Statement.CreateTrigger(new CreateTrigger(
                OrAlter: false,
                Temporary: false,
                OrReplace: false,
                IsConstraint: false,
                Name: Name("emp_stamp"),
                Period: new TriggerPeriod.Before(),
                PeriodBeforeTable: true,
                Events: Sequence.Of<TriggerEvent>(new TriggerEvent.Insert(), new TriggerEvent.Update(Sequence<Ident>.Empty)),
                TableName: Name("emp"),
                ReferencedTableName: null,
                Referencing: Sequence<TriggerReferencing>.Empty,
                TriggerObject: new TriggerObjectKind.ForEach(new TriggerObject.Row()),
                Condition: null,
                ExecBody: new TriggerExecBody(
                    ExecType: new TriggerExecBodyType.Function(),
                    FuncDesc: new FunctionDesc(
                        Name: Name("emp_stamp"),
                        Args: Sequence<OperateFunctionArg>.Empty)),
                StatementsAs: false,
                Statements: null,
                Characteristics: null)));

        // Check the fourth statement
        Assert.Equal(
            dropTrigger,
            new Statement.DropTrigger(new DropTrigger(
                IfExists: false,
                TriggerName: Name("emp_stamp"),
                TableName: Name("emp"),
                Option: null)));
    }

    [Fact]
    public void test_unicode_string_literal()
    {
        var pairs = new (string Input, string Expected)[]
        {
            // Example from the postgres docs
            (@"U&'\0441\043B\043E\043D'", "слон"),
            // High unicode code point (> 0xFFFF)
            (@"U&'\+01F418'", "🐘"),
            // Escaped backslash
            (@"U&'\\'", @"\"),
            // Escaped single quote
            ("U&''''", "'"),
        };
        foreach (var (input, expected) in pairs)
        {
            switch (PgAndGeneric().VerifiedExpr(input))
            {
                case Expr.Value { Inner: { Value: Value.UnicodeStringLiteral { Value: var s } } }:
                    Assert.Equal(expected, s);
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
    }

    // fn check_arrow_precedence(sql: &str, arrow_operator: BinaryOperator)
    private static void CheckArrowPrecedence(string sql, BinaryOperator arrowOperator)
    {
        Assert.Equal(
            Pg().VerifiedExpr(sql),
            new Expr.BinaryOp(
                Left: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident(
                        Value: "foo",
                        QuoteStyle: null,
                        Span: Span.Empty())),
                    Op: arrowOperator,
                    Right: Expr.MakeValue(
                        new Value.SingleQuotedString("bar"))),
                Op: new BinaryOperator.Eq(),
                Right: Expr.MakeValue(
                    new Value.SingleQuotedString("spam"))));
    }

    [Fact]
    public void arrow_precedence()
    {
        CheckArrowPrecedence("foo -> 'bar' = 'spam'", new BinaryOperator.Arrow());
    }

    [Fact]
    public void long_arrow_precedence()
    {
        CheckArrowPrecedence("foo ->> 'bar' = 'spam'", new BinaryOperator.LongArrow());
    }

    [Fact]
    public void arrow_cast_precedence()
    {
        // check this matches postgres where you would need `(foo -> 'bar')::TEXT`
        var stmt = Pg().VerifiedExpr("foo -> 'bar'::TEXT");
        Assert.Equal(
            stmt,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident(
                    Value: "foo",
                    QuoteStyle: null,
                    Span: Span.Empty())),
                Op: new BinaryOperator.Arrow(),
                Right: new Expr.Cast(
                    Kind: CastKind.DoubleColon,
                    Expr: Expr.MakeValue(
                        new Value.SingleQuotedString("bar")),
                    DataType: new DataType.Text(),
                    Format: null)));
    }

    [Fact]
    public void parse_create_type_as_enum()
    {
        var sql = "CREATE TYPE public.my_type AS ENUM ('label1', 'label2', 'label3', 'label4')";
        var statement = PgAndGeneric().VerifiedStmt(sql);
        switch (statement)
        {
            case Statement.CreateType
            {
                Name: var name,
                Representation: UserDefinedTypeRepresentation.Enum { Labels: var labels },
            }:
                Assert.Equal("public.my_type", name.ToString());
                Assert.Equal(
                    new[] { "label1", "label2", "label3", "label4" }
                        .Select(l => Ident.WithQuote('\'', l))
                        .ToSequence(),
                    labels);
                break;
            default:
                throw new InvalidOperationException($"\"{sql}\" should parse to Statement::CreateType");
        }
    }

    [Fact]
    public void parse_alter_type()
    {
        // struct TestCase { sql, name, operation } → tupla nomeada
        var testCases = new (string Sql, string Name, AlterTypeOperation Operation)[]
        {
            (
                Sql: "ALTER TYPE public.my_type RENAME TO my_new_type",
                Name: "public.my_type",
                Operation: new AlterTypeOperation.Rename(new AlterTypeRename(
                    NewName: new Ident("my_new_type")))
            ),
            (
                Sql: "ALTER TYPE public.my_type ADD VALUE IF NOT EXISTS 'label3.5' BEFORE 'label4'",
                Name: "public.my_type",
                Operation: new AlterTypeOperation.AddValue(new AlterTypeAddValue(
                    IfNotExists: true,
                    Value: Ident.WithQuote('\'', "label3.5"),
                    Position: new AlterTypeAddValuePosition.Before(Ident.WithQuote(
                        '\'', "label4"))))
            ),
            (
                Sql: "ALTER TYPE public.my_type ADD VALUE 'label3.5' BEFORE 'label4'",
                Name: "public.my_type",
                Operation: new AlterTypeOperation.AddValue(new AlterTypeAddValue(
                    IfNotExists: false,
                    Value: Ident.WithQuote('\'', "label3.5"),
                    Position: new AlterTypeAddValuePosition.Before(Ident.WithQuote(
                        '\'', "label4"))))
            ),
            (
                Sql: "ALTER TYPE public.my_type ADD VALUE IF NOT EXISTS 'label3.5' AFTER 'label3'",
                Name: "public.my_type",
                Operation: new AlterTypeOperation.AddValue(new AlterTypeAddValue(
                    IfNotExists: true,
                    Value: Ident.WithQuote('\'', "label3.5"),
                    Position: new AlterTypeAddValuePosition.After(Ident.WithQuote(
                        '\'', "label3"))))
            ),
            (
                Sql: "ALTER TYPE public.my_type ADD VALUE 'label3.5' AFTER 'label3'",
                Name: "public.my_type",
                Operation: new AlterTypeOperation.AddValue(new AlterTypeAddValue(
                    IfNotExists: false,
                    Value: Ident.WithQuote('\'', "label3.5"),
                    Position: new AlterTypeAddValuePosition.After(Ident.WithQuote(
                        '\'', "label3"))))
            ),
            (
                Sql: "ALTER TYPE public.my_type ADD VALUE IF NOT EXISTS 'label5'",
                Name: "public.my_type",
                Operation: new AlterTypeOperation.AddValue(new AlterTypeAddValue(
                    IfNotExists: true,
                    Value: Ident.WithQuote('\'', "label5"),
                    Position: null))
            ),
            (
                Sql: "ALTER TYPE public.my_type ADD VALUE 'label5'",
                Name: "public.my_type",
                Operation: new AlterTypeOperation.AddValue(new AlterTypeAddValue(
                    IfNotExists: false,
                    Value: Ident.WithQuote('\'', "label5"),
                    Position: null))
            ),
        };

        foreach (var tc in testCases)
        {
            var statement = PgAndGeneric().VerifiedStmt(tc.Sql);
            if (statement is Statement.AlterType { Value: { Name: var name, Operation: var operation } })
            {
                Assert.Equal(tc.Name, name.ToString()); // "TestCase[{index}].name"
                Assert.Equal(tc.Operation, operation); // "TestCase[{index}].operation"
            }
            else
            {
                throw new InvalidOperationException($"\"{tc.Sql}\" should parse to Statement::AlterType");
            }
        }
    }

    [Fact]
    public void parse_bitstring_literal()
    {
        var select = PgAndGeneric().VerifiedOnlySelect("SELECT B'111'");
        Assert.Equal(
            select.Projection,
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(Expr.MakeValue(
                new Value.SingleQuotedByteStringLiteral("111")))));
    }

    [Fact]
    public void parse_varbit_datatype()
    {
        switch (PgAndGeneric().VerifiedStmt("CREATE TABLE foo (x VARBIT, y VARBIT(42))"))
        {
            case Statement.CreateTable { Value.Columns: var columns }:
                Assert.Equal(
                    columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: new Ident("x"),
                            DataType: new DataType.VarBit(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident("y"),
                            DataType: new DataType.VarBit(42),
                            Options: Sequence<ColumnOptionDef>.Empty)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_alter_table_replica_identity()
    {
        switch (PgAndGeneric().VerifiedStmt("ALTER TABLE foo REPLICA IDENTITY FULL"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.ReplicaIdentity(
                        Identity: new ReplicaIdentity.Full())));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        switch (PgAndGeneric().VerifiedStmt("ALTER TABLE foo REPLICA IDENTITY USING INDEX foo_idx"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.ReplicaIdentity(
                        Identity: new ReplicaIdentity.Index(new Ident("foo_idx")))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        switch (PgAndGeneric().VerifiedStmt("ALTER TABLE foo REPLICA IDENTITY NOTHING"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.ReplicaIdentity(
                        Identity: new ReplicaIdentity.Nothing())));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        switch (PgAndGeneric().VerifiedStmt("ALTER TABLE foo REPLICA IDENTITY DEFAULT"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.ReplicaIdentity(
                        Identity: new ReplicaIdentity.Default())));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_ts_datatypes()
    {
        switch (PgAndGeneric().VerifiedStmt("CREATE TABLE foo (x TSVECTOR)"))
        {
            case Statement.CreateTable { Value.Columns: var columns }:
                Assert.Equal(
                    columns,
                    Sequence.Of(new ColumnDef(
                        Name: new Ident("x"),
                        DataType: new DataType.TsVector(),
                        Options: Sequence<ColumnOptionDef>.Empty)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        switch (PgAndGeneric().VerifiedStmt("CREATE TABLE foo (x TSQUERY)"))
        {
            case Statement.CreateTable { Value.Columns: var columns }:
                Assert.Equal(
                    columns,
                    Sequence.Of(new ColumnDef(
                        Name: new Ident("x"),
                        DataType: new DataType.TsQuery(),
                        Options: Sequence<ColumnOptionDef>.Empty)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_alter_table_constraint_not_valid()
    {
        switch (PgAndGeneric().VerifiedStmt(
            "ALTER TABLE foo ADD CONSTRAINT bar FOREIGN KEY (baz) REFERENCES other(ref) NOT VALID"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.AddConstraint(
                        Constraint: TableConstraint.From(new ForeignKeyConstraint(
                            Name: new Ident("bar"),
                            IndexName: null,
                            Columns: Sequence.Of(new Ident("baz")),
                            ForeignTable: ObjectName.From(Sequence.Of(new Ident("other"))),
                            ReferredColumns: Sequence.Of(new Ident("ref")),
                            OnDelete: null,
                            OnUpdate: null,
                            MatchKind: null,
                            Characteristics: null)),
                        NotValid: true)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_alter_table_validate_constraint()
    {
        switch (PgAndGeneric().VerifiedStmt("ALTER TABLE foo VALIDATE CONSTRAINT bar"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.ValidateConstraint(Name: new Ident("bar"))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_create_server()
    {
        var testCases = new (string Sql, CreateServerStatement Expected)[]
        {
            (
                "CREATE SERVER myserver FOREIGN DATA WRAPPER postgres_fdw",
                new CreateServerStatement(
                    Name: ObjectName.From(Sequence.Of(new Ident("myserver"))),
                    IfNotExists: false,
                    ServerType: null,
                    Version: null,
                    ForeignDataWrapper: ObjectName.From(Sequence.Of(new Ident("postgres_fdw"))),
                    Options: null)
            ),
            (
                "CREATE SERVER IF NOT EXISTS myserver TYPE 'server_type' VERSION 'server_version' FOREIGN DATA WRAPPER postgres_fdw",
                new CreateServerStatement(
                    Name: ObjectName.From(Sequence.Of(new Ident("myserver"))),
                    IfNotExists: true,
                    ServerType: new Ident(
                        Value: "server_type",
                        QuoteStyle: '\'',
                        Span: Span.Empty()),
                    Version: new Ident(
                        Value: "server_version",
                        QuoteStyle: '\'',
                        Span: Span.Empty()),
                    ForeignDataWrapper: ObjectName.From(Sequence.Of(new Ident("postgres_fdw"))),
                    Options: null)
            ),
            (
                "CREATE SERVER myserver2 FOREIGN DATA WRAPPER postgres_fdw OPTIONS (host 'foo', dbname 'foodb', port '5432')",
                new CreateServerStatement(
                    Name: ObjectName.From(Sequence.Of(new Ident("myserver2"))),
                    IfNotExists: false,
                    ServerType: null,
                    Version: null,
                    ForeignDataWrapper: ObjectName.From(Sequence.Of(new Ident("postgres_fdw"))),
                    Options: Sequence.Of(
                        new CreateServerOption(
                            Key: new Ident("host"),
                            Value: new Ident(
                                Value: "foo",
                                QuoteStyle: '\'',
                                Span: Span.Empty())),
                        new CreateServerOption(
                            Key: new Ident("dbname"),
                            Value: new Ident(
                                Value: "foodb",
                                QuoteStyle: '\'',
                                Span: Span.Empty())),
                        new CreateServerOption(
                            Key: new Ident("port"),
                            Value: new Ident(
                                Value: "5432",
                                QuoteStyle: '\'',
                                Span: Span.Empty()))))
            ),
        };

        foreach (var (sql, expected) in testCases)
        {
            if (PgAndGeneric().VerifiedStmt(sql) is not Statement.CreateServer { Value: var stmt })
            {
                throw new InvalidOperationException("unreachable");
            }
            Assert.Equal(stmt, expected);
        }
    }

    [Fact]
    public void parse_alter_schema()
    {
        // Test RENAME operation
        var stmt = PgAndGeneric().VerifiedStmt("ALTER SCHEMA foo RENAME TO bar");
        switch (stmt)
        {
            case Statement.AlterSchema { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterSchemaOperation>(new AlterSchemaOperation.Rename(
                        Name: ObjectName.From(Sequence.Of(new Ident("bar"))))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        // Test OWNER TO operations with different owner types
        foreach (var (ownerClause, expectedOwner) in new (string, Owner)[]
        {
            ("bar", new Owner.Ident(new Ident("bar"))),
            ("CURRENT_ROLE", new Owner.CurrentRole()),
            ("CURRENT_USER", new Owner.CurrentUser()),
            ("SESSION_USER", new Owner.SessionUser()),
        })
        {
            var sql = $"ALTER SCHEMA foo OWNER TO {ownerClause}";
            var ownerStmt = PgAndGeneric().VerifiedStmt(sql);
            switch (ownerStmt)
            {
                case Statement.AlterSchema { Value.Operations: var operations }:
                    Assert.Equal(
                        operations,
                        Sequence.Of<AlterSchemaOperation>(new AlterSchemaOperation.OwnerTo(
                            Owner: expectedOwner)));
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }
    }

    [Fact]
    public void parse_foreign_key_match()
    {
        var testCases = new (string, ConstraintReferenceMatchKind)[]
        {
            ("MATCH FULL", new ConstraintReferenceMatchKind.Full()),
            ("MATCH SIMPLE", new ConstraintReferenceMatchKind.Simple()),
            ("MATCH PARTIAL", new ConstraintReferenceMatchKind.Partial()),
        };

        foreach (var (matchClause, expectedKind) in testCases)
        {
            // Test column-level foreign key
            var sql = $"CREATE TABLE t (id INT REFERENCES other_table (id) {matchClause})";
            var statement = PgAndGeneric().VerifiedStmt(sql);
            switch (statement)
            {
                case Statement.CreateTable { Value.Columns: var columns }:
                    switch (columns[0].Options[0].Option)
                    {
                        case ColumnOption.ForeignKey { Value: var constraint }:
                            Assert.Equal(constraint.MatchKind, expectedKind);
                            break;
                        default:
                            throw new InvalidOperationException("Expected ColumnOption::ForeignKey");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"\"{sql}\" should parse to Statement::CreateTable");
            }

            // Test table-level foreign key constraint
            var sql2 =
                $"CREATE TABLE t (id INT, FOREIGN KEY (id) REFERENCES other_table(id) {matchClause})";
            var statement2 = PgAndGeneric().VerifiedStmt(sql2);
            switch (statement2)
            {
                case Statement.CreateTable { Value.Constraints: var constraints }:
                    switch (constraints[0])
                    {
                        case TableConstraint.ForeignKey { Value: var constraint }:
                            Assert.Equal(constraint.MatchKind, expectedKind);
                            break;
                        default:
                            throw new InvalidOperationException("Expected TableConstraint::ForeignKey");
                    }
                    break;
                default:
                    throw new InvalidOperationException($"\"{sql2}\" should parse to Statement::CreateTable");
            }
        }
    }

    [Fact]
    public void parse_foreign_key_match_with_actions()
    {
        var sql = "CREATE TABLE orders (order_id INT REFERENCES another_table (id) MATCH FULL ON DELETE CASCADE ON UPDATE RESTRICT, customer_id INT, CONSTRAINT fk_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id) MATCH SIMPLE ON DELETE SET NULL ON UPDATE CASCADE)";

        PgAndGeneric().VerifiedStmt(sql);
    }

    [Fact]
    public void parse_create_operator()
    {
        var sql = "CREATE OPERATOR myschema.@@ (PROCEDURE = myschema.my_proc, LEFTARG = TIMESTAMP WITH TIME ZONE, RIGHTARG = VARCHAR(255), COMMUTATOR = schema.>, NEGATOR = schema.<=, RESTRICT = myschema.sel_func, JOIN = myschema.join_func, HASHES, MERGES)";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.CreateOperator(new CreateOperator(
                Name: Name("myschema", "@@"),
                Function: Name("myschema", "my_proc"),
                IsProcedure: true,
                LeftArg: new DataType.Timestamp(null, new TimezoneInfo.WithTimeZone()),
                RightArg: new DataType.Varchar(new CharacterLength.IntegerLength(
                    Length: 255,
                    Unit: null)),
                Options: Sequence.Of<OperatorOption>(
                    new OperatorOption.Commutator(Name(
                        "schema",
                        ">")),
                    new OperatorOption.Negator(Name(
                        "schema",
                        "<=")),
                    new OperatorOption.Restrict(Name(
                        "myschema",
                        "sel_func")),
                    new OperatorOption.Join(Name(
                        "myschema",
                        "join_func")),
                    new OperatorOption.Hashes(),
                    new OperatorOption.Merges()))));

        foreach (var opSymbol in new[]
        {
            "-", "*", "/", "<", ">", "=", "<=", ">=", "<>", "~", "!", "@", "#", "%", "^", "&", "|",
            "<<", ">>", "&&",
        })
        {
            Assert.Equal(
                Pg().VerifiedStmt($"CREATE OPERATOR {opSymbol} (FUNCTION = f)"),
                new Statement.CreateOperator(new CreateOperator(
                    Name: Name(opSymbol),
                    Function: Name("f"),
                    IsProcedure: false,
                    LeftArg: null,
                    RightArg: null,
                    Options: Sequence<OperatorOption>.Empty)));
        }

        Pg().OneStatementParsesTo(
            "CREATE OPERATOR != (FUNCTION = func)",
            "CREATE OPERATOR <> (FUNCTION = func)");

        foreach (var (name, expectedName) in new (string, ObjectName)[]
        {
            (
                "s1.+",
                Name("s1", "+")
            ),
            (
                "s2.-",
                Name("s2", "-")
            ),
            (
                "s1.s3.*",
                Name("s1", "s3", "*")
            ),
        })
        {
            switch (Pg().VerifiedStmt($"CREATE OPERATOR {name} (FUNCTION = f)"))
            {
                case Statement.CreateOperator { Value: { Name: var parsedName, Options: var options } }:
                    Assert.Equal(parsedName, expectedName);
                    Assert.True(options.IsEmpty);
                    break;
                default:
                    throw new InvalidOperationException("unreachable");
            }
        }

        Pg().OneStatementParsesTo(
            "CREATE OPERATOR + (FUNCTION = f, COMMUTATOR = OPERATOR(>), NEGATOR = OPERATOR(>=))",
            "CREATE OPERATOR + (FUNCTION = f, COMMUTATOR = >, NEGATOR = >=)");

        // Test all duplicate clause errors
        foreach (var field in new[]
        {
            "FUNCTION = f2",
            "PROCEDURE = p",
            "LEFTARG = INT4, LEFTARG = INT4",
            "RIGHTARG = INT4, RIGHTARG = INT4",
            "COMMUTATOR = -, COMMUTATOR = *",
            "NEGATOR = -, NEGATOR = *",
            "RESTRICT = f1, RESTRICT = f2",
            "JOIN = f1, JOIN = f2",
            "HASHES, HASHES",
            "MERGES, MERGES",
        })
        {
            Assert.Throws<ParserException>(() => Pg()
                .ParseSqlStatements($"CREATE OPERATOR + (FUNCTION = f, {field})"));
        }

        // Test missing FUNCTION/PROCEDURE error
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("CREATE OPERATOR + (LEFTARG = INT4)"));

        // Test empty parameter list error
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements("CREATE OPERATOR + ()"));

        // Test nested empty parentheses error
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements("CREATE OPERATOR > (()"));
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements("CREATE OPERATOR > ())"));
    }

    [Fact]
    public void parse_drop_operator()
    {
        // Test DROP OPERATOR with NONE for prefix operator
        var sql = "DROP OPERATOR ~ (NONE, BIT)";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.DropOperator(new DropOperator(
                IfExists: false,
                Operators: Sequence.Of(new DropOperatorSignature(
                    Name: Name("~"),
                    LeftType: null,
                    RightType: new DataType.Bit(null))),
                DropBehavior: null)));

        foreach (var ifExist in new[] { true, false })
        {
            foreach (var cascading in new DropBehavior?[]
            {
                null,
                new DropBehavior.Cascade(),
                new DropBehavior.Restrict(),
            })
            {
                foreach (var op in new[] { "<", ">", "<=", ">=", "<>", "||", "&&", "<<", ">>" })
                {
                    var loopSql =
                        $"DROP OPERATOR{(ifExist ? " IF EXISTS" : "")} {op} (INTEGER, INTEGER){(cascading is { } c ? $" {c}" : "")}";
                    Assert.Equal(
                        PgAndGeneric().VerifiedStmt(loopSql),
                        new Statement.DropOperator(new DropOperator(
                            IfExists: ifExist,
                            Operators: Sequence.Of(new DropOperatorSignature(
                                Name: Name(op),
                                LeftType: new DataType.Integer(null),
                                RightType: new DataType.Integer(null))),
                            DropBehavior: cascading)));
                }
            }
        }

        // Test DROP OPERATOR with schema-qualified operator name
        sql = "DROP OPERATOR myschema.@@ (TEXT, TEXT)";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.DropOperator(new DropOperator(
                IfExists: false,
                Operators: Sequence.Of(new DropOperatorSignature(
                    Name: Name("myschema", "@@"),
                    LeftType: new DataType.Text(),
                    RightType: new DataType.Text())),
                DropBehavior: null)));

        // Test DROP OPERATOR with multiple operators, IF EXISTS and CASCADE
        sql = "DROP OPERATOR IF EXISTS + (INTEGER, INTEGER), - (INTEGER, INTEGER) CASCADE";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.DropOperator(new DropOperator(
                IfExists: true,
                Operators: Sequence.Of(
                    new DropOperatorSignature(
                        Name: Name("+"),
                        LeftType: new DataType.Integer(null),
                        RightType: new DataType.Integer(null)),
                    new DropOperatorSignature(
                        Name: Name("-"),
                        LeftType: new DataType.Integer(null),
                        RightType: new DataType.Integer(null))),
                DropBehavior: new DropBehavior.Cascade())));

        // Test error: DROP OPERATOR with no operators
        sql = "DROP OPERATOR (INTEGER, INTEGER)";
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(sql));

        // Test error: DROP OPERATOR IF EXISTS with no operators
        var sql2 = "DROP OPERATOR IF EXISTS (INTEGER, INTEGER)";
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(sql2));
    }

    [Fact]
    public void parse_alter_operator()
    {
        // Test ALTER OPERATOR ... OWNER TO with different owner types
        foreach (var (ownerSql, ownerAst) in new (string, Owner)[]
        {
            ("joe", new Owner.Ident(new Ident("joe"))),
            ("CURRENT_USER", new Owner.CurrentUser()),
            ("CURRENT_ROLE", new Owner.CurrentRole()),
            ("SESSION_USER", new Owner.SessionUser()),
        })
        {
            foreach (var (opName, opNameAst, leftTypeSql, leftTypeAst, rightTypeSql, rightTypeAst) in
                new (string, ObjectName, string, DataType?, string, DataType)[]
                {
                    (
                        "+",
                        Name("+"),
                        "INTEGER",
                        new DataType.Integer(null),
                        "INTEGER",
                        new DataType.Integer(null)
                    ),
                    (
                        "~",
                        Name("~"),
                        "NONE",
                        null,
                        "BIT",
                        new DataType.Bit(null)
                    ),
                    (
                        "@@",
                        Name("@@"),
                        "TEXT",
                        new DataType.Text(),
                        "TEXT",
                        new DataType.Text()
                    ),
                })
            {
                var sql = $"ALTER OPERATOR {opName} ({leftTypeSql}, {rightTypeSql}) OWNER TO {ownerSql}";
                Assert.Equal(
                    PgAndGeneric().VerifiedStmt(sql),
                    new Statement.AlterOperator(new AlterOperator(
                        Name: opNameAst,
                        LeftType: leftTypeAst,
                        RightType: rightTypeAst,
                        Operation: new AlterOperatorOperation.OwnerTo(ownerAst))));
            }
        }

        // Test ALTER OPERATOR ... SET SCHEMA
        foreach (var (opName, opNameAst, schemaName, schemaNameAst) in new (string, ObjectName, string, ObjectName)[]
        {
            (
                "+",
                Name("+"),
                "new_schema",
                Name("new_schema")
            ),
            (
                "myschema.@@",
                Name("myschema", "@@"),
                "other_schema",
                Name("other_schema")
            ),
        })
        {
            var sql = $"ALTER OPERATOR {opName} (TEXT, TEXT) SET SCHEMA {schemaName}";
            Assert.Equal(
                PgAndGeneric().VerifiedStmt(sql),
                new Statement.AlterOperator(new AlterOperator(
                    Name: opNameAst,
                    LeftType: new DataType.Text(),
                    RightType: new DataType.Text(),
                    Operation: new AlterOperatorOperation.SetSchema(
                        SchemaName: schemaNameAst))));
        }

        // Test ALTER OPERATOR ... SET with RESTRICT and JOIN
        foreach (var (restrictVal, restrictAst, joinVal, joinAst) in new (string, ObjectName?, string, ObjectName?)[]
        {
            (
                "_int_contsel",
                Name("_int_contsel"),
                "_int_contjoinsel",
                Name("_int_contjoinsel")
            ),
            (
                "NONE",
                null,
                "my_joinsel",
                Name("my_joinsel")
            ),
            (
                "my_sel",
                Name("my_sel"),
                "NONE",
                null
            ),
        })
        {
            var sql = $"ALTER OPERATOR && (TEXT, TEXT) SET (RESTRICT = {restrictVal}, JOIN = {joinVal})";
            Assert.Equal(
                PgAndGeneric().VerifiedStmt(sql),
                new Statement.AlterOperator(new AlterOperator(
                    Name: Name("&&"),
                    LeftType: new DataType.Text(),
                    RightType: new DataType.Text(),
                    Operation: new AlterOperatorOperation.Set(
                        Options: Sequence.Of<OperatorOption>(
                            new OperatorOption.Restrict(restrictAst),
                            new OperatorOption.Join(joinAst))))));
        }

        // Test ALTER OPERATOR ... SET with COMMUTATOR and NEGATOR
        foreach (var (op, commutator, negator) in new[] { ("&&", "&&", ">"), ("+", "+", "-"), ("<", "<", ">=") })
        {
            var sql = $"ALTER OPERATOR {op} (INTEGER, INTEGER) SET (COMMUTATOR = {commutator}, NEGATOR = {negator})";
            Assert.Equal(
                PgAndGeneric().VerifiedStmt(sql),
                new Statement.AlterOperator(new AlterOperator(
                    Name: Name(op),
                    LeftType: new DataType.Integer(null),
                    RightType: new DataType.Integer(null),
                    Operation: new AlterOperatorOperation.Set(
                        Options: Sequence.Of<OperatorOption>(
                            new OperatorOption.Commutator(Name(commutator)),
                            new OperatorOption.Negator(Name(negator)))))));
        }

        // Test ALTER OPERATOR ... SET with HASHES and MERGES (individually and combined)
        foreach (var (op, optionsSql, optionsAst) in new (string, string, Sequence<OperatorOption>)[]
        {
            ("=", "HASHES", Sequence.Of<OperatorOption>(new OperatorOption.Hashes())),
            ("<", "MERGES", Sequence.Of<OperatorOption>(new OperatorOption.Merges())),
            (
                "<=",
                "HASHES, MERGES",
                Sequence.Of<OperatorOption>(new OperatorOption.Hashes(), new OperatorOption.Merges())
            ),
        })
        {
            var sql = $"ALTER OPERATOR {op} (INTEGER, INTEGER) SET ({optionsSql})";
            Assert.Equal(
                PgAndGeneric().VerifiedStmt(sql),
                new Statement.AlterOperator(new AlterOperator(
                    Name: Name(op),
                    LeftType: new DataType.Integer(null),
                    RightType: new DataType.Integer(null),
                    Operation: new AlterOperatorOperation.Set(
                        Options: optionsAst))));
        }

        // Test ALTER OPERATOR ... SET with multiple options combined
        var combinedSql =
            "ALTER OPERATOR + (INTEGER, INTEGER) SET (COMMUTATOR = +, NEGATOR = -, HASHES, MERGES)";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(combinedSql),
            new Statement.AlterOperator(new AlterOperator(
                Name: Name("+"),
                LeftType: new DataType.Integer(null),
                RightType: new DataType.Integer(null),
                Operation: new AlterOperatorOperation.Set(
                    Options: Sequence.Of<OperatorOption>(
                        new OperatorOption.Commutator(Name("+")),
                        new OperatorOption.Negator(Name("-")),
                        new OperatorOption.Hashes(),
                        new OperatorOption.Merges())))));
    }

    [Fact]
    public void parse_alter_operator_family()
    {
        // Test ALTER OPERATOR FAMILY ... ADD OPERATOR
        var sql = "ALTER OPERATOR FAMILY integer_ops USING btree ADD OPERATOR 1 < (INT4, INT2)";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorFamily(new AlterOperatorFamily(
                Name: Name("integer_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorFamilyOperation.Add(
                    Items: Sequence.Of<OperatorFamilyItem>(new OperatorFamilyItem.Operator(
                        StrategyNumber: 1,
                        OperatorName: Name("<"),
                        OpTypes: Sequence.Of<DataType>(new DataType.Int4(null), new DataType.Int2(null)),
                        Purpose: null))))));

        // Test ALTER OPERATOR FAMILY ... ADD OPERATOR with FOR SEARCH
        sql =
            "ALTER OPERATOR FAMILY text_ops USING btree ADD OPERATOR 1 @@ (TEXT, TEXT) FOR SEARCH";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorFamily(new AlterOperatorFamily(
                Name: Name("text_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorFamilyOperation.Add(
                    Items: Sequence.Of<OperatorFamilyItem>(new OperatorFamilyItem.Operator(
                        StrategyNumber: 1,
                        OperatorName: Name("@@"),
                        OpTypes: Sequence.Of<DataType>(new DataType.Text(), new DataType.Text()),
                        Purpose: new OperatorPurpose.ForSearch()))))));

        // Test ALTER OPERATOR FAMILY ... ADD FUNCTION
        sql = "ALTER OPERATOR FAMILY integer_ops USING btree ADD FUNCTION 1 btint42cmp(INT4, INT2)";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorFamily(new AlterOperatorFamily(
                Name: Name("integer_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorFamilyOperation.Add(
                    Items: Sequence.Of<OperatorFamilyItem>(new OperatorFamilyItem.Function(
                        SupportNumber: 1,
                        OpTypes: null,
                        FunctionName: Name("btint42cmp"),
                        ArgumentTypes: Sequence.Of<DataType>(new DataType.Int4(null), new DataType.Int2(null))))))));

        // Test ALTER OPERATOR FAMILY ... DROP OPERATOR
        sql = "ALTER OPERATOR FAMILY integer_ops USING btree DROP OPERATOR 1 (INT4, INT2)";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorFamily(new AlterOperatorFamily(
                Name: Name("integer_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorFamilyOperation.Drop(
                    Items: Sequence.Of<OperatorFamilyDropItem>(new OperatorFamilyDropItem.Operator(
                        StrategyNumber: 1,
                        OpTypes: Sequence.Of<DataType>(new DataType.Int4(null), new DataType.Int2(null))))))));

        // Test ALTER OPERATOR FAMILY ... DROP FUNCTION
        sql = "ALTER OPERATOR FAMILY integer_ops USING btree DROP FUNCTION 1 (INT4, INT2)";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorFamily(new AlterOperatorFamily(
                Name: Name("integer_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorFamilyOperation.Drop(
                    Items: Sequence.Of<OperatorFamilyDropItem>(new OperatorFamilyDropItem.Function(
                        SupportNumber: 1,
                        OpTypes: Sequence.Of<DataType>(new DataType.Int4(null), new DataType.Int2(null))))))));

        // Test ALTER OPERATOR FAMILY ... RENAME TO
        sql = "ALTER OPERATOR FAMILY old_ops USING btree RENAME TO new_ops";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorFamily(new AlterOperatorFamily(
                Name: Name("old_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorFamilyOperation.RenameTo(
                    NewName: Name("new_ops")))));

        // Test ALTER OPERATOR FAMILY ... OWNER TO
        sql = "ALTER OPERATOR FAMILY my_ops USING btree OWNER TO joe";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorFamily(new AlterOperatorFamily(
                Name: Name("my_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorFamilyOperation.OwnerTo(new Owner.Ident(new Ident("joe"))))));

        // Test ALTER OPERATOR FAMILY ... SET SCHEMA
        sql = "ALTER OPERATOR FAMILY my_ops USING btree SET SCHEMA new_schema";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorFamily(new AlterOperatorFamily(
                Name: Name("my_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorFamilyOperation.SetSchema(
                    SchemaName: Name("new_schema")))));

        // Test error cases
        // Missing USING clause
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops ADD OPERATOR 1 < (INT4, INT2)"));

        // Invalid operation
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree INVALID_OPERATION"));

        // Missing operator name in ADD OPERATOR
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 (INT4, INT2)"));

        // Missing function name in ADD FUNCTION
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1 (INT4, INT2)"));

        // Missing parentheses in DROP OPERATOR
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree DROP OPERATOR 1 INT4, INT2"));

        // Invalid operator name (empty)
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1  (INT4, INT2)"));

        // Invalid operator name (special characters)
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 @#$ (INT4, INT2)"));

        // Negative strategy number
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR -1 < (INT4, INT2)"));

        // Non-integer strategy number
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1.5 < (INT4, INT2)"));

        // Missing closing parenthesis in operator types
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2"));

        // Missing opening parenthesis in operator types
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < INT4, INT2)"));

        // Empty operator types
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < ()"));

        // Invalid data type (using punctuation)
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (@#$%, INT2)"));

        // Incomplete FOR clause
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2) FOR"));

        // Invalid FOR clause keyword
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2) FOR INVALID"));

        // FOR ORDER BY without sort family
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2) FOR ORDER BY"));

        // Missing function name in ADD FUNCTION
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1 (INT4, INT2)"));

        // Invalid function name
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1 123invalid(INT4, INT2)"));

        // Negative support number
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION -1 func(INT4, INT2)"));

        // Non-integer support number
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1.5 func(INT4, INT2)"));

        // Missing closing parenthesis in function operator types
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1 (INT4, INT2 func()"));

        // Missing closing parenthesis in function arguments
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1 func(INT4, INT2"));

        // Invalid data type in function arguments
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1 func(@#$%, INT2)"));

        // DROP OPERATOR with FOR clause (not allowed)
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree DROP OPERATOR 1 (INT4, INT2) FOR SEARCH"));

        // DROP FUNCTION with function arguments (not allowed)
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree DROP FUNCTION 1 (INT4, INT2) func(INT4)"));

        // Multiple ADD items with error in middle
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2), INVALID_ITEM"));

        // Multiple DROP items with error in middle
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree DROP OPERATOR 1 (INT4, INT2), INVALID_ITEM"));

        // RENAME TO with invalid new name
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree RENAME TO 123invalid"));

        // OWNER TO with invalid owner
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree OWNER TO 123invalid"));

        // SET SCHEMA with invalid schema name
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree SET SCHEMA 123invalid"));

        // Schema-qualified operator family name with invalid schema
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY 123invalid.my_ops USING btree ADD OPERATOR 1 < (INT4, INT2)"));

        // Missing operator family name
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY USING btree ADD OPERATOR 1 < (INT4, INT2)"));

        // Extra tokens at end
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2) EXTRA"));

        // Incomplete statement
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree ADD"));

        // Very long numbers
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 999999999999999999999 < (INT4, INT2)"));

        // Multiple FOR clauses
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2) FOR SEARCH FOR ORDER BY sort_family"));

        // FOR SEARCH with extra tokens
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2) FOR SEARCH EXTRA"));

        // FOR ORDER BY with invalid sort family
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree ADD OPERATOR 1 < (INT4, INT2) FOR ORDER BY 123invalid"));

        // Function with empty operator types but missing function args parens
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1 () func"));

        // Function with mismatched parentheses
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR FAMILY my_ops USING btree ADD FUNCTION 1 (INT4 func(INT2"));

        // DROP with empty types
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree DROP OPERATOR 1 ()"));

        // DROP FUNCTION with empty types
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR FAMILY my_ops USING btree DROP FUNCTION 1 ()"));
    }

    [Fact]
    public void parse_alter_operator_class()
    {
        // Test ALTER OPERATOR CLASS ... RENAME TO
        var sql = "ALTER OPERATOR CLASS int_ops USING btree RENAME TO integer_ops";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorClass(new AlterOperatorClass(
                Name: Name("int_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorClassOperation.RenameTo(
                    NewName: Name("integer_ops")))));

        // Test ALTER OPERATOR CLASS ... OWNER TO
        sql = "ALTER OPERATOR CLASS int_ops USING btree OWNER TO joe";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorClass(new AlterOperatorClass(
                Name: Name("int_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorClassOperation.OwnerTo(new Owner.Ident(new Ident("joe"))))));

        // Test ALTER OPERATOR CLASS ... OWNER TO CURRENT_USER
        sql = "ALTER OPERATOR CLASS int_ops USING btree OWNER TO CURRENT_USER";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorClass(new AlterOperatorClass(
                Name: Name("int_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorClassOperation.OwnerTo(new Owner.CurrentUser()))));

        // Test ALTER OPERATOR CLASS ... SET SCHEMA
        sql = "ALTER OPERATOR CLASS int_ops USING btree SET SCHEMA new_schema";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorClass(new AlterOperatorClass(
                Name: Name("int_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorClassOperation.SetSchema(
                    SchemaName: Name("new_schema")))));

        // Test with schema-qualified operator class name
        sql = "ALTER OPERATOR CLASS myschema.int_ops USING btree RENAME TO integer_ops";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.AlterOperatorClass(new AlterOperatorClass(
                Name: Name("myschema", "int_ops"),
                Using: new Ident("btree"),
                Operation: new AlterOperatorClassOperation.RenameTo(
                    NewName: Name("integer_ops")))));

        // Test with different index methods
        foreach (var indexMethod in new[] { "hash", "gist", "gin", "spgist", "brin" })
        {
            var methodSql = $"ALTER OPERATOR CLASS int_ops USING {indexMethod} RENAME TO integer_ops";
            PgAndGeneric().VerifiedStmt(methodSql);
        }

        // Test error cases
        // Missing USING clause
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops RENAME TO integer_ops"));

        // Invalid operation
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops USING btree INVALID_OPERATION"));

        // Missing new name for RENAME TO
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops USING btree RENAME TO"));

        // Missing owner for OWNER TO
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops USING btree OWNER TO"));

        // Missing schema for SET SCHEMA
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops USING btree SET SCHEMA"));

        // Invalid new name
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops USING btree RENAME TO 123invalid"));

        // Invalid owner
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops USING btree OWNER TO 123invalid"));

        // Invalid schema name
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops USING btree SET SCHEMA 123invalid"));

        // Missing operator class name
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS USING btree RENAME TO integer_ops"));

        // Extra tokens at end
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR CLASS int_ops USING btree RENAME TO integer_ops EXTRA"));

        // Missing index method
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops RENAME TO integer_ops"));

        // Invalid index method
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER OPERATOR CLASS int_ops USING 123invalid RENAME TO integer_ops"));

        // Trying to use ADD operation (only valid for OPERATOR FAMILY)
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR CLASS int_ops USING btree ADD OPERATOR 1 < (INT4, INT2)"));

        // Trying to use DROP operation (only valid for OPERATOR FAMILY)
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "ALTER OPERATOR CLASS int_ops USING btree DROP OPERATOR 1 (INT4, INT2)"));
    }

    [Fact]
    public void parse_alter_function_and_aggregate()
    {
        foreach (var (sql, expected) in new[]
        {
            (
                "ALTER AGGREGATE alt_func1(int) RENAME TO alt_func3",
                "ALTER AGGREGATE alt_func1(INT) RENAME TO alt_func3"
            ),
            (
                "ALTER AGGREGATE alt_func1(int) OWNER TO regress_alter_generic_user3",
                "ALTER AGGREGATE alt_func1(INT) OWNER TO regress_alter_generic_user3"
            ),
            (
                "ALTER AGGREGATE alt_func1(int) SET SCHEMA alt_nsp2",
                "ALTER AGGREGATE alt_func1(INT) SET SCHEMA alt_nsp2"
            ),
            (
                "ALTER AGGREGATE alt_agg1(int) RENAME TO alt_agg2",
                "ALTER AGGREGATE alt_agg1(INT) RENAME TO alt_agg2"
            ),
            (
                "ALTER AGGREGATE alt_agg1(int) RENAME TO alt_agg3",
                "ALTER AGGREGATE alt_agg1(INT) RENAME TO alt_agg3"
            ),
            (
                "ALTER AGGREGATE alt_agg2(int) OWNER TO regress_alter_generic_user2",
                "ALTER AGGREGATE alt_agg2(INT) OWNER TO regress_alter_generic_user2"
            ),
            (
                "ALTER AGGREGATE alt_agg2(int) OWNER TO regress_alter_generic_user3",
                "ALTER AGGREGATE alt_agg2(INT) OWNER TO regress_alter_generic_user3"
            ),
            (
                "ALTER AGGREGATE alt_agg2(int) SET SCHEMA alt_nsp2",
                "ALTER AGGREGATE alt_agg2(INT) SET SCHEMA alt_nsp2"
            ),
            (
                "ALTER AGGREGATE alt_order(int ORDER BY text) RENAME TO alt_order2",
                "ALTER AGGREGATE alt_order(INT ORDER BY TEXT) RENAME TO alt_order2"
            ),
            (
                "ALTER AGGREGATE alt_order_only(ORDER BY int) SET SCHEMA alt_nsp2",
                "ALTER AGGREGATE alt_order_only(ORDER BY INT) SET SCHEMA alt_nsp2"
            ),
            (
                "ALTER AGGREGATE alt_star(*) OWNER TO regress_alter_generic_user2",
                "ALTER AGGREGATE alt_star(*) OWNER TO regress_alter_generic_user2"
            ),
        })
        {
            var statement = PgAndGeneric().OneStatementParsesTo(sql, expected);
            Assert.True(statement is Statement.AlterFunction
            {
                Value.Kind: AlterFunctionKind.Aggregate,
            });
        }

        foreach (var (sql, expected) in new[]
        {
            (
                "ALTER FUNCTION alt_func1(int) RENAME TO alt_func2",
                "ALTER FUNCTION alt_func1(INT) RENAME TO alt_func2"
            ),
            (
                "ALTER FUNCTION alt_func1(int) RENAME TO alt_func3",
                "ALTER FUNCTION alt_func1(INT) RENAME TO alt_func3"
            ),
            (
                "ALTER FUNCTION alt_func2(int) OWNER TO regress_alter_generic_user2",
                "ALTER FUNCTION alt_func2(INT) OWNER TO regress_alter_generic_user2"
            ),
            (
                "ALTER FUNCTION alt_func2(int) OWNER TO regress_alter_generic_user3",
                "ALTER FUNCTION alt_func2(INT) OWNER TO regress_alter_generic_user3"
            ),
            (
                "ALTER FUNCTION alt_func2(int) SET SCHEMA alt_nsp1",
                "ALTER FUNCTION alt_func2(INT) SET SCHEMA alt_nsp1"
            ),
            (
                "ALTER FUNCTION alt_func2(int) SET SCHEMA alt_nsp2",
                "ALTER FUNCTION alt_func2(INT) SET SCHEMA alt_nsp2"
            ),
            (
                "ALTER FUNCTION alt_func2(int) DEPENDS ON EXTENSION ext1",
                "ALTER FUNCTION alt_func2(INT) DEPENDS ON EXTENSION ext1"
            ),
            (
                "ALTER FUNCTION alt_func2(int) NO DEPENDS ON EXTENSION ext1",
                "ALTER FUNCTION alt_func2(INT) NO DEPENDS ON EXTENSION ext1"
            ),
            (
                "ALTER FUNCTION alt_func2 IMMUTABLE",
                "ALTER FUNCTION alt_func2 IMMUTABLE"
            ),
            (
                "ALTER FUNCTION alt_func2(int) IMMUTABLE",
                "ALTER FUNCTION alt_func2(INT) IMMUTABLE"
            ),
            (
                "ALTER FUNCTION alt_func2(int) STABLE",
                "ALTER FUNCTION alt_func2(INT) STABLE"
            ),
            (
                "ALTER FUNCTION alt_func2(int) VOLATILE",
                "ALTER FUNCTION alt_func2(INT) VOLATILE"
            ),
            (
                "ALTER FUNCTION alt_func2(int) CALLED ON NULL INPUT",
                "ALTER FUNCTION alt_func2(INT) CALLED ON NULL INPUT"
            ),
            (
                "ALTER FUNCTION alt_func2(int) RETURNS NULL ON NULL INPUT",
                "ALTER FUNCTION alt_func2(INT) RETURNS NULL ON NULL INPUT"
            ),
            (
                "ALTER FUNCTION alt_func2(int) STRICT",
                "ALTER FUNCTION alt_func2(INT) STRICT"
            ),
            (
                "ALTER FUNCTION alt_func2(int) LEAKPROOF",
                "ALTER FUNCTION alt_func2(INT) LEAKPROOF"
            ),
            (
                "ALTER FUNCTION alt_func2(int) NOT LEAKPROOF",
                "ALTER FUNCTION alt_func2(INT) NOT LEAKPROOF"
            ),
            (
                "ALTER FUNCTION alt_func2(int) SECURITY DEFINER",
                "ALTER FUNCTION alt_func2(INT) SECURITY DEFINER"
            ),
            (
                "ALTER FUNCTION alt_func2(int) EXTERNAL SECURITY INVOKER",
                "ALTER FUNCTION alt_func2(INT) EXTERNAL SECURITY INVOKER"
            ),
            (
                "ALTER FUNCTION alt_func2(int) PARALLEL SAFE",
                "ALTER FUNCTION alt_func2(INT) PARALLEL SAFE"
            ),
            (
                "ALTER FUNCTION alt_func2(int) PARALLEL RESTRICTED",
                "ALTER FUNCTION alt_func2(INT) PARALLEL RESTRICTED"
            ),
            (
                "ALTER FUNCTION alt_func2(int) PARALLEL UNSAFE",
                "ALTER FUNCTION alt_func2(INT) PARALLEL UNSAFE"
            ),
            (
                "ALTER FUNCTION alt_func2(int) COST 3.5",
                "ALTER FUNCTION alt_func2(INT) COST 3.5"
            ),
            (
                "ALTER FUNCTION alt_func2(int) ROWS 42",
                "ALTER FUNCTION alt_func2(INT) ROWS 42"
            ),
            (
                "ALTER FUNCTION alt_func2(int) SUPPORT pg_catalog.alt_support",
                "ALTER FUNCTION alt_func2(INT) SUPPORT pg_catalog.alt_support"
            ),
            (
                "ALTER FUNCTION alt_func2(int) SET work_mem TO DEFAULT",
                "ALTER FUNCTION alt_func2(INT) SET work_mem = DEFAULT"
            ),
            (
                "ALTER FUNCTION alt_func2(int) SET work_mem FROM CURRENT",
                "ALTER FUNCTION alt_func2(INT) SET work_mem FROM CURRENT"
            ),
            (
                "ALTER FUNCTION alt_func2(int) SET search_path = pg_catalog, public",
                "ALTER FUNCTION alt_func2(INT) SET search_path = pg_catalog, public"
            ),
            (
                "ALTER FUNCTION alt_func2(int) RESET work_mem",
                "ALTER FUNCTION alt_func2(INT) RESET work_mem"
            ),
            (
                "ALTER FUNCTION alt_func2(int) RESET ALL",
                "ALTER FUNCTION alt_func2(INT) RESET ALL"
            ),
            (
                "ALTER FUNCTION alt_func2(int) IMMUTABLE STRICT PARALLEL SAFE RESTRICT",
                "ALTER FUNCTION alt_func2(INT) IMMUTABLE STRICT PARALLEL SAFE RESTRICT"
            ),
            (
                "ALTER FUNCTION alt_variadic(VARIADIC int[]) STABLE",
                "ALTER FUNCTION alt_variadic(VARIADIC INT[]) STABLE"
            ),
        })
        {
            var statement = PgAndGeneric().OneStatementParsesTo(sql, expected);
            Assert.True(statement is Statement.AlterFunction
            {
                Value.Kind: AlterFunctionKind.Function,
            });
        }

        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER AGGREGATE alt_func1(INT) DEPENDS ON EXTENSION ext1"));
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER AGGREGATE alt_func1(INT) NO DEPENDS ON EXTENSION ext1"));
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER AGGREGATE alt_func1(OUT INT) OWNER TO joe"));
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER AGGREGATE alt_func1(INOUT INT) OWNER TO joe"));
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER AGGREGATE alt_func1(INT = 1) OWNER TO joe"));

        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER AGGREGATE alt_func1(INT) IMMUTABLE"));
    }

    [Fact]
    public void parse_create_text_search()
    {
        // CREATE: one per object type
        var stmt =
            PgAndGeneric().VerifiedStmt("CREATE TEXT SEARCH DICTIONARY d (template = simple)");
        Assert.Equal(Span.Empty(), stmt.Span());
        PgAndGeneric().VerifiedStmt("CREATE TEXT SEARCH CONFIGURATION c (copy = english)");
        PgAndGeneric().VerifiedStmt("CREATE TEXT SEARCH TEMPLATE t (lexize = dsimple_lexize)");
        PgAndGeneric().VerifiedStmt(
            "CREATE TEXT SEARCH PARSER p (start = prsd_start, gettoken = prsd_nexttoken, end = prsd_end, lextypes = prsd_lextype)");

        // CREATE with quoted option key
        PgAndGeneric().VerifiedStmt("""CREATE TEXT SEARCH TEMPLATE t ("Init" = init_function)""");

        // Object type must be an unquoted keyword-like token in this position.
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("""CREATE TEXT SEARCH "DICTIONARY" d (template = simple)"""));

        // CREATE options are key-value pairs in PostgreSQL syntax.
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("CREATE TEXT SEARCH DICTIONARY d (template)"));
    }

    [Fact]
    public void parse_alter_text_search()
    {
        // One test per operation kind.
        var stmt = PgAndGeneric().VerifiedStmt("ALTER TEXT SEARCH DICTIONARY d (opt = val)");
        Assert.Equal(Span.Empty(), stmt.Span());
        if (stmt is Statement.AlterTextSearch { Value: var alterTextSearch })
        {
            Assert.Equal(Span.Empty(), alterTextSearch.Span());
        }
        else
        {
            throw new InvalidOperationException("expected ALTER TEXT SEARCH statement");
        }
        PgAndGeneric().VerifiedStmt("ALTER TEXT SEARCH DICTIONARY d (opt)");
        PgAndGeneric().VerifiedStmt("ALTER TEXT SEARCH CONFIGURATION c OWNER TO some_user");
        PgAndGeneric().VerifiedStmt("ALTER TEXT SEARCH TEMPLATE t SET SCHEMA s");
        PgAndGeneric().VerifiedStmt("ALTER TEXT SEARCH PARSER p RENAME TO p2");

        // The parser accepts text search operations permissively across object types.
        PgAndGeneric().VerifiedStmt("ALTER TEXT SEARCH TEMPLATE t OWNER TO some_user");
        PgAndGeneric().VerifiedStmt("ALTER TEXT SEARCH PARSER p (opt = val)");

        var err = Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements("ALTER TEXT SEARCH DICTIONARY d RESET foo")).Error;
        Assert.Contains(
            "RENAME TO, OWNER TO, SET SCHEMA, or (...) after ALTER TEXT SEARCH",
            err.ToString());
    }

    [Fact]
    public void parse_drop_operator_family()
    {
        foreach (var ifExists in new[] { true, false })
        {
            foreach (var dropBehavior in new DropBehavior?[]
            {
                null,
                new DropBehavior.Cascade(),
                new DropBehavior.Restrict(),
            })
            {
                foreach (var indexMethod in new[] { "btree", "hash", "gist", "gin", "spgist", "brin" })
                {
                    foreach (var (namesStr, namesVec) in new (string, Sequence<ObjectName>)[]
                    {
                        (
                            "float_ops",
                            Sequence.Of(Name("float_ops"))
                        ),
                        (
                            "myschema.custom_ops",
                            Sequence.Of(Name(
                                "myschema",
                                "custom_ops"))
                        ),
                        (
                            "ops1, ops2, schema.ops3",
                            Sequence.Of(
                                Name("ops1"),
                                Name("ops2"),
                                Name("schema", "ops3"))
                        ),
                    })
                    {
                        var sql =
                            $"DROP OPERATOR FAMILY{(ifExists ? " IF EXISTS" : "")} {namesStr} USING {indexMethod}{(dropBehavior is { } behavior ? $" {behavior}" : "")}";
                        Assert.Equal(
                            PgAndGeneric().VerifiedStmt(sql),
                            new Statement.DropOperatorFamily(new DropOperatorFamily(
                                IfExists: ifExists,
                                Names: namesVec,
                                Using: new Ident(indexMethod),
                                DropBehavior: dropBehavior)));
                    }
                }
            }
        }

        // Test error: DROP OPERATOR FAMILY with no names
        var sql1 = "DROP OPERATOR FAMILY USING btree";
        Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql1));

        // Test error: DROP OPERATOR FAMILY IF EXISTS with no names
        var sql2 = "DROP OPERATOR FAMILY IF EXISTS USING btree";
        Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql2));
    }

    [Fact]
    public void parse_drop_operator_class()
    {
        foreach (var ifExists in new[] { true, false })
        {
            foreach (var dropBehavior in new DropBehavior?[]
            {
                null,
                new DropBehavior.Cascade(),
                new DropBehavior.Restrict(),
            })
            {
                foreach (var indexMethod in new[] { "btree", "hash", "gist", "gin", "spgist", "brin" })
                {
                    foreach (var (namesStr, namesVec) in new (string, Sequence<ObjectName>)[]
                    {
                        (
                            "widget_ops",
                            Sequence.Of(Name("widget_ops"))
                        ),
                        (
                            "myschema.int4_ops",
                            Sequence.Of(Name(
                                "myschema",
                                "int4_ops"))
                        ),
                        (
                            "ops1, ops2, schema.ops3",
                            Sequence.Of(
                                Name("ops1"),
                                Name("ops2"),
                                Name("schema", "ops3"))
                        ),
                    })
                    {
                        var sql =
                            $"DROP OPERATOR CLASS{(ifExists ? " IF EXISTS" : "")} {namesStr} USING {indexMethod}{(dropBehavior is { } behavior ? $" {behavior}" : "")}";
                        Assert.Equal(
                            PgAndGeneric().VerifiedStmt(sql),
                            new Statement.DropOperatorClass(new DropOperatorClass(
                                IfExists: ifExists,
                                Names: namesVec,
                                Using: new Ident(indexMethod),
                                DropBehavior: dropBehavior)));
                    }
                }
            }
        }

        // Test error: DROP OPERATOR CLASS with no names
        var sql1 = "DROP OPERATOR CLASS USING btree";
        Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql1));

        // Test error: DROP OPERATOR CLASS IF EXISTS with no names
        var sql2 = "DROP OPERATOR CLASS IF EXISTS USING btree";
        Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql2));
    }

    [Fact]
    public void parse_create_operator_family()
    {
        foreach (var indexMethod in new[] { "btree", "hash", "gist", "gin", "spgist", "brin" })
        {
            Assert.Equal(
                PgAndGeneric().VerifiedStmt(
                    $"CREATE OPERATOR FAMILY my_family USING {indexMethod}"),
                new Statement.CreateOperatorFamily(new CreateOperatorFamily(
                    Name: Name("my_family"),
                    Using: new Ident(indexMethod))));
            Assert.Equal(
                PgAndGeneric().VerifiedStmt(
                    $"CREATE OPERATOR FAMILY myschema.test_family USING {indexMethod}"),
                new Statement.CreateOperatorFamily(new CreateOperatorFamily(
                    Name: Name("myschema", "test_family"),
                    Using: new Ident(indexMethod))));
        }
    }

    [Fact]
    public void parse_create_operator_class()
    {
        // Test all combinations of DEFAULT flag and FAMILY clause with different name qualifications
        foreach (var (isDefault, defaultClause) in new[] { (false, ""), (true, "DEFAULT ") })
        {
            foreach (var (hasFamily, familyClause) in new[] { (false, ""), (true, " FAMILY int4_family") })
            {
                foreach (var (className, expectedName) in new (string, ObjectName)[]
                {
                    ("int4_ops", Name("int4_ops")),
                    (
                        "myschema.test_ops",
                        Name("myschema", "test_ops")
                    ),
                })
                {
                    var sql =
                        $"CREATE OPERATOR CLASS {className} {defaultClause}FOR TYPE INT4 USING btree{familyClause} AS OPERATOR 1 <";
                    switch (PgAndGeneric().VerifiedStmt(sql))
                    {
                        case Statement.CreateOperatorClass
                        {
                            Value:
                            {
                                Name: var name,
                                Default: var @default,
                                ForType: var forType,
                                Using: var @using,
                                Family: var family,
                                Items: var items,
                            },
                        }:
                            Assert.Equal(name, expectedName);
                            Assert.Equal(@default, isDefault);
                            Assert.Equal(forType, new DataType.Int4(null));
                            Assert.Equal(@using, new Ident("btree"));
                            Assert.Equal(
                                family,
                                hasFamily
                                    ? Name("int4_family")
                                    : null);
                            Assert.Equal(1, items.Count);
                            break;
                        default:
                            throw new InvalidOperationException("Expected CreateOperatorClass statement");
                    }
                }
            }
        }

        // Test comprehensive operator class with all fields
        switch (PgAndGeneric().VerifiedStmt("CREATE OPERATOR CLASS CAS_btree_ops DEFAULT FOR TYPE CAS USING btree FAMILY CAS_btree_ops AS OPERATOR 1 <, OPERATOR 2 <=, OPERATOR 3 =, OPERATOR 4 >=, OPERATOR 5 >, FUNCTION 1 cas_cmp(CAS, CAS)"))
        {
            case Statement.CreateOperatorClass
            {
                Value:
                {
                    Name: var name,
                    Default: true,
                    ForType: var forType,
                    Using: var @using,
                    Family: var family,
                    Items: var items,
                },
            }:
                Assert.Equal(name, Name("CAS_btree_ops"));
                Assert.Equal(forType, new DataType.Custom(Name("CAS"), Sequence<string>.Empty));
                Assert.Equal(@using, new Ident("btree"));
                Assert.Equal(family, Name("CAS_btree_ops"));
                Assert.Equal(6, items.Count);
                break;
            default:
                throw new InvalidOperationException("Expected CreateOperatorClass statement");
        }

        // Test operator with argument types
        switch (PgAndGeneric().VerifiedStmt(
            "CREATE OPERATOR CLASS test_ops FOR TYPE INT4 USING gist AS OPERATOR 1 < (INT4, INT4)"))
        {
            case Statement.CreateOperatorClass { Value.Items: var items }:
                Assert.Equal(1, items.Count);
                switch (items[0])
                {
                    case OperatorClassItem.Operator
                    {
                        StrategyNumber: 1,
                        OperatorName: var operatorName,
                        OpTypes:
                        {
                            Left: DataType.Int4 { Value: null },
                            Right: DataType.Int4 { Value: null },
                        },
                        Purpose: null,
                    }:
                        Assert.Equal(operatorName, Name("<"));
                        break;
                    default:
                        throw new InvalidOperationException("Expected Operator item with arg types");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateOperatorClass statement");
        }

        // Test operator FOR SEARCH
        switch (PgAndGeneric().VerifiedStmt(
            "CREATE OPERATOR CLASS test_ops FOR TYPE INT4 USING gist AS OPERATOR 1 < FOR SEARCH"))
        {
            case Statement.CreateOperatorClass { Value.Items: var items }:
                Assert.Equal(1, items.Count);
                switch (items[0])
                {
                    case OperatorClassItem.Operator
                    {
                        StrategyNumber: 1,
                        OperatorName: var operatorName,
                        OpTypes: null,
                        Purpose: OperatorPurpose.ForSearch,
                    }:
                        Assert.Equal(operatorName, Name("<"));
                        break;
                    default:
                        throw new InvalidOperationException("Expected Operator item FOR SEARCH");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateOperatorClass statement");
        }

        // Test operator FOR ORDER BY
        switch (Pg().VerifiedStmt("CREATE OPERATOR CLASS test_ops FOR TYPE INT4 USING gist AS OPERATOR 2 <<-> FOR ORDER BY float_ops"))
        {
            case Statement.CreateOperatorClass
            {
                Value.Items: var items,
            }:
                Assert.Equal(1, items.Count);
                switch (items[0])
                {
                    case OperatorClassItem.Operator
                    {
                        StrategyNumber: 2,
                        OperatorName: var operatorName,
                        OpTypes: null,
                        Purpose: OperatorPurpose.ForOrderBy { SortFamily: var sortFamily },
                    }:
                        Assert.Equal(operatorName, Name("<<->"));
                        Assert.Equal(sortFamily, Name("float_ops"));
                        break;
                    default:
                        throw new InvalidOperationException("Expected Operator item FOR ORDER BY");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateOperatorClass statement");
        }

        // Test function with operator class arg types
        switch (PgAndGeneric().VerifiedStmt("CREATE OPERATOR CLASS test_ops FOR TYPE INT4 USING btree AS FUNCTION 1 (INT4, INT4) btcmp(INT4, INT4)"))
        {
            case Statement.CreateOperatorClass
            {
                Value.Items: var items,
            }:
                Assert.Equal(1, items.Count);
                switch (items[0])
                {
                    case OperatorClassItem.Function
                    {
                        SupportNumber: 1,
                        OpTypes: not null,
                        FunctionName: var functionName,
                        ArgumentTypes: var argumentTypes,
                    }:
                        Assert.Equal(functionName, Name("btcmp"));
                        Assert.Equal(2, argumentTypes.Count);
                        break;
                    default:
                        throw new InvalidOperationException("Expected Function item with op_types");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateOperatorClass statement");
        }

        // Test function with no arguments (empty parentheses normalizes to no parentheses)
        PgAndGeneric().OneStatementParsesTo(
            "CREATE OPERATOR CLASS test_ops FOR TYPE INT4 USING btree AS FUNCTION 1 my_func()",
            "CREATE OPERATOR CLASS test_ops FOR TYPE INT4 USING btree AS FUNCTION 1 my_func");
        switch (PgAndGeneric().VerifiedStmt(
            "CREATE OPERATOR CLASS test_ops FOR TYPE INT4 USING btree AS FUNCTION 1 my_func"))
        {
            case Statement.CreateOperatorClass { Value.Items: var items }:
                Assert.Equal(1, items.Count);
                switch (items[0])
                {
                    case OperatorClassItem.Function
                    {
                        SupportNumber: 1,
                        OpTypes: null,
                        FunctionName: var functionName,
                        ArgumentTypes: var argumentTypes,
                    }:
                        Assert.Equal(
                            functionName,
                            Name("my_func"));
                        Assert.Equal(0, argumentTypes.Count);
                        break;
                    default:
                        throw new InvalidOperationException("Expected Function item without op_types and no arguments");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateOperatorClass statement");
        }

        // Test multiple items including STORAGE
        switch (PgAndGeneric().VerifiedStmt("CREATE OPERATOR CLASS gist_ops FOR TYPE geometry USING gist AS OPERATOR 1 <<, FUNCTION 1 gist_consistent(internal, geometry, INT4), STORAGE box"))
        {
            case Statement.CreateOperatorClass
            {
                Value.Items: var items,
            }:
                Assert.Equal(3, items.Count);
                // Check operator item
                switch (items[0])
                {
                    case OperatorClassItem.Operator
                    {
                        StrategyNumber: 1,
                        OperatorName: var operatorName,
                    }:
                        Assert.Equal(operatorName, Name("<<"));
                        break;
                    default:
                        throw new InvalidOperationException("Expected Operator item");
                }
                // Check function item
                switch (items[1])
                {
                    case OperatorClassItem.Function
                    {
                        SupportNumber: 1,
                        FunctionName: var functionName,
                        ArgumentTypes: var argumentTypes,
                    }:
                        Assert.Equal(functionName, Name("gist_consistent"));
                        Assert.Equal(3, argumentTypes.Count);
                        break;
                    default:
                        throw new InvalidOperationException("Expected Function item");
                }
                // Check storage item
                switch (items[2])
                {
                    case OperatorClassItem.Storage { StorageType: var storageType }:
                        Assert.Equal(storageType, new DataType.Custom(Name("box"), Sequence<string>.Empty));
                        break;
                    default:
                        throw new InvalidOperationException("Expected Storage item");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateOperatorClass statement");
        }

        // Test nested empty parentheses error in function arguments
        Assert.Throws<ParserException>(() => Pg()
            .ParseSqlStatements(
                "CREATE OPERATOR CLASS test_ops FOR TYPE INT4 USING btree AS FUNCTION 1 cas_cmp(()"));
    }

    [Fact]
    public void parse_identifiers_semicolon_handling()
    {
        var statement = "SHOW search_path; SELECT 1";
        PgAndGeneric().StatementsParseTo(statement, statement);
        statement = "SHOW search_path; SHOW ALL; SHOW ALL";
        PgAndGeneric().StatementsParseTo(statement, statement);
    }

    [Fact]
    public void parse_create_table_partition_of_range()
    {
        // RANGE partition with FROM ... TO
        var sql = "CREATE TABLE measurement_y2006m02 PARTITION OF measurement FOR VALUES FROM ('2006-02-01') TO ('2006-03-01')";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("measurement_y2006m02", createTable.Name.ToString());
                Assert.Equal(
                    Name("measurement"),
                    createTable.PartitionOf);
                switch (createTable.ForValues)
                {
                    case ForValues.From { FromValue: var from, To: var to }:
                        Assert.Equal(1, from.Count);
                        Assert.Equal(1, to.Count);
                        switch (from[0])
                        {
                            case PartitionBoundValue.Expr { Value: Expr.Value { Inner: var v } }:
                                Assert.Equal("'2006-02-01'", v.ToString());
                                break;
                            default:
                                throw new InvalidOperationException("Expected Expr value in from");
                        }
                        switch (to[0])
                        {
                            case PartitionBoundValue.Expr { Value: Expr.Value { Inner: var v } }:
                                Assert.Equal("'2006-03-01'", v.ToString());
                                break;
                            default:
                                throw new InvalidOperationException("Expected Expr value in to");
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Expected ForValues::From");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void parse_create_table_partition_of_range_with_minvalue_maxvalue()
    {
        // RANGE partition with MINVALUE/MAXVALUE
        var sql =
            "CREATE TABLE orders_old PARTITION OF orders FOR VALUES FROM (MINVALUE) TO ('2020-01-01')";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("orders_old", createTable.Name.ToString());
                Assert.Equal(
                    Name("orders"),
                    createTable.PartitionOf);
                switch (createTable.ForValues)
                {
                    case ForValues.From { FromValue: var from, To: var to }:
                        Assert.Equal<PartitionBoundValue>(new PartitionBoundValue.MinValue(), from[0]);
                        switch (to[0])
                        {
                            case PartitionBoundValue.Expr { Value: Expr.Value { Inner: var v } }:
                                Assert.Equal("'2020-01-01'", v.ToString());
                                break;
                            default:
                                throw new InvalidOperationException("Expected Expr value in to");
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Expected ForValues::From");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }

        // With MAXVALUE
        sql =
            "CREATE TABLE orders_new PARTITION OF orders FOR VALUES FROM ('2024-01-01') TO (MAXVALUE)";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                switch (createTable.ForValues)
                {
                    case ForValues.From { FromValue: var from, To: var to }:
                        switch (from[0])
                        {
                            case PartitionBoundValue.Expr { Value: Expr.Value { Inner: var v } }:
                                Assert.Equal("'2024-01-01'", v.ToString());
                                break;
                            default:
                                throw new InvalidOperationException("Expected Expr value in from");
                        }
                        Assert.Equal<PartitionBoundValue>(new PartitionBoundValue.MaxValue(), to[0]);
                        break;
                    default:
                        throw new InvalidOperationException("Expected ForValues::From");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void parse_create_table_partition_of_list()
    {
        // LIST partition
        var sql = "CREATE TABLE orders_us PARTITION OF orders FOR VALUES IN ('US', 'CA', 'MX')";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("orders_us", createTable.Name.ToString());
                Assert.Equal(
                    Name("orders"),
                    createTable.PartitionOf);
                switch (createTable.ForValues)
                {
                    case ForValues.In { Value: var values }:
                        Assert.Equal(3, values.Count);
                        break;
                    default:
                        throw new InvalidOperationException("Expected ForValues::In");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void parse_create_table_partition_of_hash()
    {
        // HASH partition
        var sql = "CREATE TABLE orders_p0 PARTITION OF orders FOR VALUES WITH (MODULUS 4, REMAINDER 0)";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("orders_p0", createTable.Name.ToString());
                Assert.Equal(
                    Name("orders"),
                    createTable.PartitionOf);
                switch (createTable.ForValues)
                {
                    case ForValues.With { Modulus: var modulus, Remainder: var remainder }:
                        Assert.Equal(4UL, modulus);
                        Assert.Equal(0UL, remainder);
                        break;
                    default:
                        throw new InvalidOperationException("Expected ForValues::With");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void parse_create_table_partition_of_default()
    {
        // DEFAULT partition
        var sql = "CREATE TABLE orders_default PARTITION OF orders DEFAULT";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("orders_default", createTable.Name.ToString());
                Assert.Equal(
                    Name("orders"),
                    createTable.PartitionOf);
                Assert.Equal<ForValues?>(new ForValues.Default(), createTable.ForValues);
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void parse_create_table_partition_of_multicolumn_range()
    {
        // Multi-column RANGE partition
        var sql = "CREATE TABLE sales_2023_q1 PARTITION OF sales FOR VALUES FROM ('2023-01-01', 1) TO ('2023-04-01', 1)";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("sales_2023_q1", createTable.Name.ToString());
                switch (createTable.ForValues)
                {
                    case ForValues.From { FromValue: var from, To: var to }:
                        Assert.Equal(2, from.Count);
                        Assert.Equal(2, to.Count);
                        break;
                    default:
                        throw new InvalidOperationException("Expected ForValues::From");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void parse_create_table_partition_of_with_constraint()
    {
        // With table constraint (not column constraint which has different syntax in PARTITION OF)
        var sql = "CREATE TABLE orders_2023 PARTITION OF orders (" +
            "CONSTRAINT check_date CHECK (order_date >= '2023-01-01')" +
            ") FOR VALUES FROM ('2023-01-01') TO ('2024-01-01')";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("orders_2023", createTable.Name.ToString());
                Assert.Equal(
                    Name("orders"),
                    createTable.PartitionOf);
                // Check that table constraint was parsed
                Assert.Equal(1, createTable.Constraints.Count);
                switch (createTable.ForValues)
                {
                    case ForValues.From:
                        break;
                    default:
                        throw new InvalidOperationException("Expected ForValues::From");
                }
                break;
            default:
                throw new InvalidOperationException("Expected CreateTable");
        }
    }

    [Fact]
    public void parse_create_table_partition_of_errors()
    {
        var sql = "CREATE TABLE p PARTITION OF parent";
        var err = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql)).Error.ToString();
        // "Expected error about FOR VALUES, got: {err}"
        Assert.Contains("FOR VALUES or DEFAULT", err);

        sql = "CREATE TABLE p PARTITION OF parent WITH (fillfactor = 70)";
        err = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql)).Error.ToString();
        // "Expected error about FOR VALUES, got: {err}"
        Assert.Contains("FOR VALUES or DEFAULT", err);

        sql = "CREATE TABLE p PARTITION OF parent FOR VALUES RANGE (1, 10)";
        err = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql)).Error.ToString();
        // "Expected error about invalid keyword after FOR VALUES, got: {err}"
        Assert.Contains("IN, FROM, or WITH", err);

        sql = "CREATE TABLE p PARTITION OF parent FOR VALUES FROM (1)";
        err = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql)).Error.ToString();
        // "Expected error about missing TO clause, got: {err}"
        Assert.Contains("TO", err);

        sql = "CREATE TABLE p PARTITION OF parent FOR VALUES IN ()";
        err = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql)).Error.ToString();
        // "Expected error about empty value list in IN clause, got: {err}"
        Assert.Contains("at least one value", err);

        sql = "CREATE TABLE p PARTITION OF parent FOR VALUES FROM () TO (10)";
        err = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql)).Error.ToString();
        // "Expected error about empty FROM list, got: {err}"
        Assert.Contains("at least one value", err);

        sql = "CREATE TABLE p PARTITION OF parent FOR VALUES FROM (1) TO ()";
        err = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql)).Error.ToString();
        // "Expected error about empty TO list, got: {err}"
        Assert.Contains("at least one value", err);
    }

    [Fact]
    public void parse_pg_analyze()
    {
        // Bare ANALYZE
        PgAndGeneric().VerifiedStmt("ANALYZE");

        // ANALYZE with table name
        PgAndGeneric().VerifiedStmt("ANALYZE t");

        // ANALYZE with column specification
        PgAndGeneric().VerifiedStmt("ANALYZE t (col1, col2)");

        // Verify AST for column specification
        var stmt = Pg().VerifiedStmt("ANALYZE t (col1, col2)");
        switch (stmt)
        {
            case Statement.Analyze { Value: var analyze }:
                Assert.Equal("t", analyze.TableName!.ToString());
                Assert.Equal(2, analyze.Columns.Count);
                Assert.Equal("col1", analyze.Columns[0].ToString());
                Assert.Equal("col2", analyze.Columns[1].ToString());
                Assert.False(analyze.ForColumns);
                break;
            default:
                throw new InvalidOperationException($"Expected Analyze, got: {stmt}");
        }
    }

    [Fact]
    public void parse_lock_table()
    {
        PgAndGeneric().OneStatementParsesTo(
            "LOCK public.widgets IN EXCLUSIVE MODE",
            "LOCK TABLE public.widgets IN EXCLUSIVE MODE");
        PgAndGeneric().OneStatementParsesTo(
            "LOCK TABLE public.widgets NOWAIT",
            "LOCK TABLE public.widgets NOWAIT");

        var stmt = PgAndGeneric().VerifiedStmt(
            "LOCK TABLE ONLY public.widgets, analytics.events * IN SHARE ROW EXCLUSIVE MODE NOWAIT");
        switch (stmt)
        {
            case Statement.Lock { Value: var @lock }:
                Assert.Equal(2, @lock.Tables.Count);
                Assert.Equal("public.widgets", @lock.Tables[0].Name.ToString());
                Assert.True(@lock.Tables[0].Only);
                Assert.False(@lock.Tables[0].HasAsterisk);
                Assert.Equal("analytics.events", @lock.Tables[1].Name.ToString());
                Assert.False(@lock.Tables[1].Only);
                Assert.True(@lock.Tables[1].HasAsterisk);
                Assert.Equal(@lock.LockMode, new LockTableMode.ShareRowExclusive());
                Assert.True(@lock.Nowait);
                break;
            default:
                throw new InvalidOperationException($"Expected Lock, got: {stmt}");
        }

        var lockModes = new (string, LockTableMode)[]
        {
            ("ACCESS SHARE", new LockTableMode.AccessShare()),
            ("ROW SHARE", new LockTableMode.RowShare()),
            ("ROW EXCLUSIVE", new LockTableMode.RowExclusive()),
            (
                "SHARE UPDATE EXCLUSIVE",
                new LockTableMode.ShareUpdateExclusive()
            ),
            ("SHARE", new LockTableMode.Share()),
            ("SHARE ROW EXCLUSIVE", new LockTableMode.ShareRowExclusive()),
            ("EXCLUSIVE", new LockTableMode.Exclusive()),
            ("ACCESS EXCLUSIVE", new LockTableMode.AccessExclusive()),
        };

        foreach (var (modeSql, expectedMode) in lockModes)
        {
            var modeStmt = PgAndGeneric()
                .VerifiedStmt($"LOCK TABLE public.widgets IN {modeSql} MODE");
            switch (modeStmt)
            {
                case Statement.Lock { Value: var @lock }:
                    Assert.Equal(1, @lock.Tables.Count);
                    Assert.Equal("public.widgets", @lock.Tables[0].Name.ToString());
                    Assert.False(@lock.Tables[0].Only);
                    Assert.False(@lock.Tables[0].HasAsterisk);
                    Assert.Equal(@lock.LockMode, expectedMode);
                    Assert.False(@lock.Nowait);
                    break;
                default:
                    throw new InvalidOperationException($"Expected Lock, got: {modeStmt}");
            }
        }
    }

    [Fact]
    public void exclude_as_column_name()
    {
        // `EXCLUDE` is a non-reserved keyword, so it stays usable as a column name
        // even on dialects that parse `EXCLUDE` constraints: a bare `exclude` not
        // followed by `USING` or `(` must not be mistaken for a constraint.
        var sql = "CREATE TABLE t (exclude INT)";
        foreach (var dialect in new Dialect[]
        {
            new MySqlDialect(),
            new SQLiteDialect(),
            new PostgreSqlDialect(),
            new GenericDialect(),
        })
        {
            // format!("{dialect:?}") → nome do tipo (convencoes.md §13, caso 9)
            var typeName = dialect.GetType().Name;
            var parser = new TestedDialects(new List<Dialect> { dialect });
            Sequence<Statement> stmts;
            try
            {
                stmts = parser.ParseSqlStatements(sql);
            }
            catch (ParserException e)
            {
                throw new InvalidOperationException($"{typeName} failed to parse {sql}: {e.Error}");
            }
            switch (stmts[0])
            {
                case Statement.CreateTable { Value: var createTable }:
                    Assert.Equal(1, createTable.Columns.Count);
                    Assert.Equal("exclude", createTable.Columns[0].Name.Value);
                    break;
                case var other:
                    throw new InvalidOperationException($"{typeName}: expected CreateTable, got {other}");
            }
        }
    }

    [Fact]
    public void parse_limit_after_locking_clause()
    {
        // PostgreSQL accepts `LIMIT`/`OFFSET` after the row-locking clause as well
        // as before it; both orderings are semantically identical. The AST renders
        // the limit in its canonical position (before the locking clause).
        Pg().OneStatementParsesTo(
            "SELECT * FROM t ORDER BY id FOR UPDATE SKIP LOCKED LIMIT 5",
            "SELECT * FROM t ORDER BY id LIMIT 5 FOR UPDATE SKIP LOCKED");
        Pg().OneStatementParsesTo(
            "SELECT * FROM t FOR UPDATE LIMIT 5",
            "SELECT * FROM t LIMIT 5 FOR UPDATE");
        // The pre-existing ordering keeps round-tripping unchanged.
        Pg().VerifiedStmt("SELECT * FROM t ORDER BY id LIMIT 5 FOR UPDATE SKIP LOCKED");
    }

    [Fact]
    public void parse_right_deep_join_chain()
    {
        // PostgreSQL supports right-deep join syntax where ON clauses follow all JOIN keywords:
        //   t0 JOIN t1 JOIN t2 ON c1 ON c2
        // which is equivalent to (and serialized as) t0 JOIN (t1 JOIN t2 ON c1) ON c2.
        Pg().OneStatementParsesTo(
            "SELECT * FROM t0 INNER JOIN t1 INNER JOIN t2 ON true ON true",
            "SELECT * FROM t0 INNER JOIN (t1 INNER JOIN t2 ON true) ON true");
        Pg().OneStatementParsesTo(
            "SELECT * FROM t0 INNER JOIN t1 INNER JOIN t2 INNER JOIN t3 ON true ON true ON true",
            "SELECT * FROM t0 INNER JOIN (t1 INNER JOIN (t2 INNER JOIN t3 ON true) ON true) ON true");
        // NATURAL JOIN followed by a constrained join must stay left-associative.
        Pg().VerifiedStmt("SELECT * FROM t0 NATURAL JOIN t1 INNER JOIN t2 ON true");
    }

    [Fact]
    public void parse_quoted_function_argument_name()
    {
        var statement = PgAndGeneric().VerifiedStmt(
            """CREATE FUNCTION is_member("Role" TEXT) RETURNS BOOLEAN LANGUAGE SQL AS 'SELECT true'""");
        if (statement is not Statement.CreateFunction { Value: var function })
        {
            throw new InvalidOperationException("expected a CREATE FUNCTION statement");
        }
        Assert.Equal(
            function.Args,
            Sequence.Of(new OperateFunctionArg(
                Mode: null,
                Name: Ident.WithQuote('"', "Role"),
                DataType: new DataType.Text(),
                DefaultExpr: null)));

        // An embedded quote is doubled in the input and belongs to the name once.
        statement = PgAndGeneric().VerifiedStmt(
            """CREATE FUNCTION f("we""ird" INT) RETURNS BOOLEAN LANGUAGE SQL RETURN true""");
        if (statement is not Statement.CreateFunction { Value: var function2 })
        {
            throw new InvalidOperationException("expected a CREATE FUNCTION statement");
        }
        Assert.Equal(
            function2.Args,
            Sequence.Of(new OperateFunctionArg(
                Mode: null,
                Name: Ident.WithQuote('"', """we"ird"""),
                DataType: new DataType.Int(null),
                DefaultExpr: null)));

        // A name outside ASCII is quoted for the same reason and survives the same way.
        PgAndGeneric().VerifiedStmt(
            """CREATE FUNCTION f("Rôle" TEXT) RETURNS BOOLEAN LANGUAGE SQL RETURN true""");
    }

    [Fact]
    public void parse_quoted_function_argument_name_span()
    {
        var sql =
            """CREATE FUNCTION is_member("Role" TEXT) RETURNS BOOLEAN LANGUAGE SQL AS 'SELECT true'""";
        // Parsed directly rather than through the test helpers, which tokenize
        // without locations.
        var statements = Parser.ParseSql(new PostgreSqlDialect(), sql);
        // statements.pop()
        if (statements.Count == 0 || statements[statements.Count - 1] is not Statement.CreateFunction { Value: var function })
        {
            throw new InvalidOperationException("expected a CREATE FUNCTION statement");
        }
        var name = function.Args!.Value[0].Name!;
        // "the span covers the quoted name in the input"
        Assert.Equal(
            name.Span,
            new Span(new Location(1, 27), new Location(1, 33)));
    }

    [Fact]
    public void parse_function_argument_modes_and_defaults_keep_quoted_names()
    {
        var sql = """CREATE FUNCTION f(IN "A" INT = 1, OUT "B" TEXT, INOUT "C" BOOLEAN, VARIADIC "D" INT[]) RETURNS INT LANGUAGE SQL AS 'x'""";
        if (PgAndGeneric().VerifiedStmt(sql) is not Statement.CreateFunction { Value: var function })
        {
            throw new InvalidOperationException("expected a CREATE FUNCTION statement");
        }
        Assert.Equal(
            function.Args,
            Sequence.Of(
                new OperateFunctionArg(
                    Mode: new ArgMode.In(),
                    Name: Ident.WithQuote('"', "A"),
                    DataType: new DataType.Int(null),
                    DefaultExpr: Expr.MakeValue(
                        new Value.Number("1", false))),
                new OperateFunctionArg(
                    Mode: new ArgMode.Out(),
                    Name: Ident.WithQuote('"', "B"),
                    DataType: new DataType.Text(),
                    DefaultExpr: null),
                new OperateFunctionArg(
                    Mode: new ArgMode.InOut(),
                    Name: Ident.WithQuote('"', "C"),
                    DataType: new DataType.Boolean(),
                    DefaultExpr: null),
                new OperateFunctionArg(
                    Mode: new ArgMode.Variadic(),
                    Name: Ident.WithQuote('"', "D"),
                    DataType: new DataType.Array(new ArrayElemTypeDef.SquareBracket(
                        new DataType.Int(null),
                        null)),
                    DefaultExpr: null)));

        // The `DEFAULT` spelling of the same argument list renders as `=`.
        PgAndGeneric().OneStatementParsesTo(
            """CREATE FUNCTION f("A" INT DEFAULT 1) RETURNS INT LANGUAGE SQL AS 'x'""",
            """CREATE FUNCTION f("A" INT = 1) RETURNS INT LANGUAGE SQL AS 'x'""");
    }

    [Fact]
    public void parse_quoted_argument_names_in_function_signatures()
    {
        PgAndGeneric().VerifiedStmt("""DROP FUNCTION f("Role" TEXT)""");
        PgAndGeneric().VerifiedStmt("""DROP PROCEDURE p("Role" TEXT)""");
        PgAndGeneric().VerifiedStmt("""ALTER FUNCTION f("Role" TEXT) RENAME TO g""");
        PgAndGeneric().VerifiedStmt("""ALTER AGGREGATE my_agg("Role" TEXT) RENAME TO other_agg""");

        // An aggregate's `ORDER BY` arguments are parsed by the same reader as its
        // direct ones.
        var sql = """ALTER AGGREGATE my_agg("A" INT ORDER BY "B" INT) OWNER TO some_role""";
        if (PgAndGeneric().VerifiedStmt(sql) is not Statement.AlterFunction { Value: var alter })
        {
            throw new InvalidOperationException("expected an ALTER AGGREGATE statement");
        }
        Assert.Equal(
            alter.Function.Args,
            Sequence.Of(new OperateFunctionArg(
                Mode: null,
                Name: Ident.WithQuote('"', "A"),
                DataType: new DataType.Int(null),
                DefaultExpr: null)));
        Assert.Equal(
            alter.AggregateOrderBy,
            Sequence.Of(new OperateFunctionArg(
                Mode: null,
                Name: Ident.WithQuote('"', "B"),
                DataType: new DataType.Int(null),
                DefaultExpr: null)));
    }

    [Fact]
    public void parse_alter_table_constraint_check_no_inherit()
    {
        switch (PgAndGeneric()
            .VerifiedStmt("ALTER TABLE docs ADD CONSTRAINT c CHECK (id > 0) NO INHERIT NOT VALID"))
        {
            case Statement.AlterTable { Value.Operations: var operations }:
                Assert.Equal(
                    operations,
                    Sequence.Of<AlterTableOperation>(new AlterTableOperation.AddConstraint(
                        Constraint: TableConstraint.From(new CheckConstraint(
                            Name: new Ident("c"),
                            Expr: new Expr.BinaryOp(
                                Left: new Expr.Identifier(new Ident("id")),
                                Op: new BinaryOperator.Gt(),
                                Right: Expr.MakeValue(TestUtils.Number("0"))),
                            NoInherit: true,
                            Enforced: null)),
                        NotValid: true)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        PgAndGeneric().VerifiedStmt("ALTER TABLE docs ADD CONSTRAINT c CHECK (id > 0) NO INHERIT");
    }

    [Fact]
    public void parse_merge_do_nothing()
    {
        if (PgAndGeneric().VerifiedStmt(
            "MERGE INTO target USING source ON target.id = source.id WHEN MATCHED THEN DO NOTHING WHEN NOT MATCHED THEN DO NOTHING")
            is not Statement.Merge { Value: var merge })
        {
            throw new InvalidOperationException("expected MERGE statement");
        }
        // matches!(merge.clauses.as_slice(), [a, b]): sem padrão de lista (indisponível no net48)
        Assert.True(
            merge.Clauses.Count == 2
            && merge.Clauses[0] is
            {
                ClauseKind: MergeClauseKind.Matched,
                Action: MergeAction.DoNothing,
            }
            && merge.Clauses[1] is
            {
                ClauseKind: MergeClauseKind.NotMatched,
                Action: MergeAction.DoNothing,
            });
        var ex = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(
            "MERGE INTO target USING source ON target.id = source.id WHEN MATCHED THEN DO UPDATE"));
        Assert.Equal<ParserError>(
            new ParserError.ParserErrorValue(
                "Expected: NOTHING, found: UPDATE"),
            ex.Error);
    }

    [Fact]
    public void parse_compound_field_access_numeric_display()
    {
        var sql = "SELECT * FROM t WHERE CASE WHEN a = 1 THEN b ELSE c END . 2";
        var statements = Pg().ParseSqlStatements(sql);
        Assert.Equal(1, statements.Count);
        var statement = statements[statements.Count - 1]; // statements.pop()
        var displayed = statement.ToString();
        var reparsed = Pg().ParseSqlStatements(displayed);
        Assert.Equal(Sequence.Of(statement), reparsed);
    }

    [Fact]
    public void parse_non_reserved_keywords_as_table_alias()
    {
        // PostgreSQL allows these keywords as explicit table aliases.
        foreach (var kw in new[]
        {
            "cluster",
            "distribute",
            "explain",
            "minus",
            "sample",
            "sort",
            "start",
            "top",
            "view",
        })
        {
            Pg().VerifiedStmt(
                $"SELECT * FROM tbl_name AS {kw} JOIN tbl_name_2 ON {kw}.id = tbl_name_2.id");
            Pg().VerifiedStmt(
                $"SELECT * FROM tbl_name {kw} JOIN tbl_name_2 ON {kw}.id = tbl_name_2.id");
        }
    }
}
#pragma warning restore xUnit2013
