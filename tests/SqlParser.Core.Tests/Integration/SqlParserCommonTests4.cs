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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_common.rs (linhas 12372–16422).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C4.md):
// - Cada fatia de tests/sqlparser_common.rs é uma classe própria; as funções auxiliares do arquivo Rust
//   usadas aqui (`verified_stmt`, `pg_and_generic`, `run_explain_analyze`...) foram copiadas como
//   métodos `private static`, mesmo quando definidas fora do trecho.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests/SpansTests). `assert_eq!(x, None)` vira
//   `Assert.Null(x)` (xUnit2003) e `assert_eq!(x, true/false)` vira `Assert.True/False(x)` (xUnit2004).
// - `assert_eq!(n, v.len())` usa `Assert.Equal(n, v.Count)`; o xUnit2013 fica suprimido neste arquivo.
// - `match x { P => .., _ => unreachable!() }` → `Assert.IsType<P>(x)`; `_ => panic!(msg)` → `Assert.Fail(msg)`.
// - `parse_sql_statements(sql).unwrap_err()` → `Assert.Throws<ParserException>(..)` + `ex.Error`;
//   `.unwrap_err().to_string()` → `ex.Error.ToString()`.
// - Strings Rust com `\` no fim da linha (continuação) viram concatenações sem a quebra e sem a
//   indentação da linha seguinte; `concat!(..)` vira concatenação com `+`; `r#"..."#` vira string
//   comum com `\"` (e `\n` explícito quando tem várias linhas).
#pragma warning disable xUnit2013
public class SqlParserCommonTests4
{
    // ---------------------------------------------------------------------------------------------
    // Funções auxiliares do arquivo Rust (definidas fora do trecho 12372–16422, copiadas aqui).
    // ---------------------------------------------------------------------------------------------

    // fn pg_and_generic() -> TestedDialects (linha 1717)
    private static TestedDialects PgAndGeneric() =>
        new TestedDialects(new List<Dialect>
        {
            new PostgreSqlDialect(),
            new GenericDialect(),
        });

    // fn ms_and_generic() -> TestedDialects (linha 1724)
    private static TestedDialects MsAndGeneric() =>
        new TestedDialects(new List<Dialect> { new MsSqlDialect(), new GenericDialect() });

    // fn run_explain_analyze(..) (linha 5690)
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

    // fn parse_sql_statements(sql: &str) -> Result<Vec<Statement>, ParserError> (linha 10994)
    private static Sequence<Statement> ParseSqlStatements(string sql) =>
        TestUtils.AllDialects().ParseSqlStatements(sql);

    // fn one_statement_parses_to(sql: &str, canonical: &str) -> Statement (linha 10998)
    private static Statement OneStatementParsesTo(string sql, string canonical) =>
        TestUtils.AllDialects().OneStatementParsesTo(sql, canonical);

    // fn verified_stmt(query: &str) -> Statement (linha 11006)
    private static Statement VerifiedStmt(string query) =>
        TestUtils.AllDialects().VerifiedStmt(query);

    // fn verified_only_select(query: &str) -> Select (linha 11014)
    private static Select VerifiedOnlySelect(string query) =>
        TestUtils.AllDialects().VerifiedOnlySelect(query);

    // fn verified_expr(query: &str) -> Expr (linha 11018)
    private static Expr VerifiedExpr(string query) =>
        TestUtils.AllDialects().VerifiedExpr(query);

    // ---------------------------------------------------------------------------------------------
    // Testes (na ordem do Rust).
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void parse_non_latin_identifiers()
    {
        var supportedDialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new DuckDbDialect(),
            new PostgreSqlDialect(),
            new MsSqlDialect(),
            new RedshiftSqlDialect(),
            new MySqlDialect(),
        });
        supportedDialects.VerifiedStmt("SELECT a.説明 FROM test.public.inter01 AS a");
        supportedDialects.VerifiedStmt("SELECT a.説明 FROM inter01 AS a, inter01_transactions AS b WHERE a.説明 = b.取引 GROUP BY a.説明");
        supportedDialects.VerifiedStmt("SELECT 説明, hühnervögel, garçon, Москва, 東京 FROM inter01");

        supportedDialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new DuckDbDialect(),
            new MsSqlDialect(),
        });
        Assert.Throws<ParserException>(() => supportedDialects
            .ParseSqlStatements("SELECT 💝 FROM table1"));
    }

    [Fact]
    public void parse_trailing_comma()
    {
        // At the moment, DuckDB is the only dialect that allows
        // trailing commas anywhere in the query
        var trailingCommas = new TestedDialects(new List<Dialect> { new DuckDbDialect() });

        trailingCommas.OneStatementParsesTo(
            "SELECT album_id, name, FROM track",
            "SELECT album_id, name FROM track");

        trailingCommas.OneStatementParsesTo(
            "SELECT * FROM track ORDER BY milliseconds,",
            "SELECT * FROM track ORDER BY milliseconds");

        trailingCommas.OneStatementParsesTo(
            "SELECT DISTINCT ON (album_id,) name FROM track",
            "SELECT DISTINCT ON (album_id) name FROM track");

        trailingCommas.OneStatementParsesTo(
            "CREATE TABLE employees (name text, age int,)",
            "CREATE TABLE employees (name TEXT, age INT)");

        trailingCommas.OneStatementParsesTo(
            "GRANT USAGE, SELECT, INSERT, ON p TO u",
            "GRANT USAGE, SELECT, INSERT ON p TO u");

        trailingCommas.VerifiedStmt("SELECT album_id, name FROM track");
        trailingCommas.VerifiedStmt("SELECT * FROM track ORDER BY milliseconds");
        trailingCommas.VerifiedStmt("SELECT DISTINCT ON (album_id) name FROM track");

        // check quoted "from" identifier edge-case
        trailingCommas.OneStatementParsesTo(
            "SELECT \"from\", FROM \"from\"",
            "SELECT \"from\" FROM \"from\"");
        trailingCommas.VerifiedStmt("SELECT \"from\" FROM \"from\"");

        // doesn't allow any trailing commas
        trailingCommas = new TestedDialects(new List<Dialect> { new PostgreSqlDialect() });

        var ex = Assert.Throws<ParserException>(() => trailingCommas
            .ParseSqlStatements("SELECT name, age, from employees;"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected an expression, found: from"));

        ex = Assert.Throws<ParserException>(() => trailingCommas
            .ParseSqlStatements("REVOKE USAGE, SELECT, ON p TO u"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: a privilege keyword, found: ON"));

        ex = Assert.Throws<ParserException>(() => trailingCommas
            .ParseSqlStatements("CREATE TABLE employees (name text, age int,)"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: column name or constraint definition, found: )"));

        var unsupportedDialects = TestUtils.AllDialectsWhere(d => !d.SupportsTrailingCommas);
        ex = Assert.Throws<ParserException>(() => unsupportedDialects
            .ParseSqlStatements("SELECT * FROM track ORDER BY milliseconds,"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: an expression, found: EOF"));
    }

    [Fact]
    public void parse_projection_trailing_comma()
    {
        var trailingCommas = TestUtils.AllDialectsWhere(d => d.SupportsProjectionTrailingCommas);

        trailingCommas.OneStatementParsesTo(
            "SELECT album_id, name, FROM track",
            "SELECT album_id, name FROM track");

        trailingCommas.VerifiedStmt("SELECT album_id, name FROM track");

        trailingCommas.VerifiedStmt("SELECT * FROM track ORDER BY milliseconds");

        trailingCommas.VerifiedStmt("SELECT DISTINCT ON (album_id) name FROM track");

        var unsupportedDialects = TestUtils.AllDialectsWhere(d =>
            !d.SupportsProjectionTrailingCommas && !d.SupportsTrailingCommas);
        var ex = Assert.Throws<ParserException>(() => unsupportedDialects
            .ParseSqlStatements("SELECT album_id, name, FROM track"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected an expression, found: FROM"));
    }

    [Fact]
    public void parse_create_type()
    {
        var createType = Assert.IsType<Statement.CreateType>(VerifiedStmt("CREATE TYPE mytype"));
        Assert.Equal("mytype", createType.Name.ToString());
        Assert.Null(createType.Representation);

        createType = Assert.IsType<Statement.CreateType>(
            VerifiedStmt("CREATE TYPE address AS (street VARCHAR(100), city TEXT COLLATE \"en_US\")"));
        {
            Assert.Equal("address", createType.Name.ToString());
            var attributes = Assert.IsType<UserDefinedTypeRepresentation.Composite>(createType.Representation).Attributes;
            Assert.Equal(2, attributes.Count);
            Assert.Equal(attributes[0].Name, new Ident("street"));
            Assert.Equal(
                attributes[0].DataType,
                new DataType.Varchar(new CharacterLength.IntegerLength(
                    Length: 100,
                    Unit: null)));
            Assert.Null(attributes[0].Collation);

            Assert.Equal(attributes[1].Name, new Ident("city"));
            Assert.Equal(attributes[1].DataType, new DataType.Text());
            Assert.Equal(
                "\"en_US\"",
                attributes[1].Collation?.ToString());
        }

        VerifiedStmt("CREATE TYPE empty AS ()");

        createType = Assert.IsType<Statement.CreateType>(VerifiedStmt("CREATE TYPE mood AS ENUM ('happy', 'sad')"));
        {
            Assert.Equal("mood", createType.Name.ToString());
            var labels = Assert.IsType<UserDefinedTypeRepresentation.Enum>(createType.Representation).Labels;
            Assert.Equal(2, labels.Count);
            Assert.Equal(labels[0], Ident.WithQuote('\'', "happy"));
            Assert.Equal(labels[1], Ident.WithQuote('\'', "sad"));
        }

        createType = Assert.IsType<Statement.CreateType>(
            VerifiedStmt("CREATE TYPE int4range AS RANGE (SUBTYPE = INTEGER, CANONICAL = fn1)"));
        {
            Assert.Equal("int4range", createType.Name.ToString());
            var options = Assert.IsType<UserDefinedTypeRepresentation.Range>(createType.Representation).Options;
            Assert.Equal(2, options.Count);
            Assert.True(options[0] is UserDefinedTypeRangeOption.Subtype { Value: DataType.Integer });
            Assert.True(options[1] is UserDefinedTypeRangeOption.Canonical);
        }

        VerifiedStmt("CREATE TYPE textrange AS RANGE (SUBTYPE = TEXT, COLLATION = \"en_US\", MULTIRANGE_TYPE_NAME = textmultirange)");

        createType = Assert.IsType<Statement.CreateType>(VerifiedStmt(
            "CREATE TYPE int4range AS RANGE (SUBTYPE = INTEGER, SUBTYPE_OPCLASS = int4_ops)"));
        {
            Assert.Equal("int4range", createType.Name.ToString());
            var options = Assert.IsType<UserDefinedTypeRepresentation.Range>(createType.Representation).Options;
            Assert.Equal(2, options.Count);
            Assert.True(options[0] is UserDefinedTypeRangeOption.Subtype { Value: DataType.Integer });
            switch (options[1])
            {
                case UserDefinedTypeRangeOption.SubtypeOpClass { Value: var name }:
                    Assert.Equal("int4_ops", name.ToString());
                    break;
                default:
                    Assert.Fail("Expected SubtypeOpClass");
                    break;
            }
        }

        createType = Assert.IsType<Statement.CreateType>(VerifiedStmt(
            "CREATE TYPE int4range AS RANGE (SUBTYPE = INTEGER, SUBTYPE_DIFF = int4range_subdiff)"));
        {
            Assert.Equal("int4range", createType.Name.ToString());
            var options = Assert.IsType<UserDefinedTypeRepresentation.Range>(createType.Representation).Options;
            Assert.Equal(2, options.Count);
            Assert.True(options[0] is UserDefinedTypeRangeOption.Subtype { Value: DataType.Integer });
            switch (options[1])
            {
                case UserDefinedTypeRangeOption.SubtypeDiff { Value: var name }:
                    Assert.Equal("int4range_subdiff", name.ToString());
                    break;
                default:
                    Assert.Fail("Expected SubtypeDiff");
                    break;
            }
        }

        createType = Assert.IsType<Statement.CreateType>(VerifiedStmt(
            "CREATE TYPE int4range AS RANGE (SUBTYPE = INTEGER, SUBTYPE_OPCLASS = int4_ops, CANONICAL = int4range_canonical, SUBTYPE_DIFF = int4range_subdiff, MULTIRANGE_TYPE_NAME = int4multirange)"));
        {
            Assert.Equal("int4range", createType.Name.ToString());
            var options = Assert.IsType<UserDefinedTypeRepresentation.Range>(createType.Representation).Options;
            Assert.Equal(5, options.Count);
            Assert.True(options[0] is UserDefinedTypeRangeOption.Subtype { Value: DataType.Integer });
            Assert.True(options[1] is UserDefinedTypeRangeOption.SubtypeOpClass);
            Assert.True(options[2] is UserDefinedTypeRangeOption.Canonical);
            Assert.True(options[3] is UserDefinedTypeRangeOption.SubtypeDiff);
            Assert.True(options[4] is UserDefinedTypeRangeOption.MultirangeTypeName);
        }

        createType = Assert.IsType<Statement.CreateType>(VerifiedStmt(
            "CREATE TYPE mytype (INPUT = in_fn, OUTPUT = out_fn, INTERNALLENGTH = 16, PASSEDBYVALUE)"));
        {
            Assert.Equal("mytype", createType.Name.ToString());
            var options = Assert.IsType<UserDefinedTypeRepresentation.SqlDefinition>(createType.Representation).Options;
            Assert.Equal(4, options.Count);
            Assert.True(options[0] is UserDefinedTypeSqlDefinitionOption.Input);
            Assert.True(options[1] is UserDefinedTypeSqlDefinitionOption.Output);
            Assert.True(options[2] is UserDefinedTypeSqlDefinitionOption.InternalLength
            {
                Value: UserDefinedTypeInternalLength.Fixed { Value: 16 },
            });
            Assert.True(options[3] is UserDefinedTypeSqlDefinitionOption.PassedByValue);
        }

        VerifiedStmt("CREATE TYPE mytype (INPUT = in_fn, OUTPUT = out_fn, INTERNALLENGTH = VARIABLE, STORAGE = extended)");

        // Test all storage variants
        foreach (var storage in new[] { "plain", "external", "extended", "main" })
        {
            VerifiedStmt(
                $"CREATE TYPE t (INPUT = f_in, OUTPUT = f_out, STORAGE = {storage})");
        }

        // Test all alignment variants
        foreach (var align in new[] { "char", "int2", "int4", "double" })
        {
            VerifiedStmt(
                $"CREATE TYPE t (INPUT = f_in, OUTPUT = f_out, ALIGNMENT = {align})");
        }

        // Test additional function options (PostgreSQL-specific due to ANALYZE keyword)
        PgAndGeneric().VerifiedStmt("CREATE TYPE t (INPUT = f_in, OUTPUT = f_out, RECEIVE = f_recv, SEND = f_send, TYPMOD_IN = f_tmin, TYPMOD_OUT = f_tmout, ANALYZE = f_analyze, SUBSCRIPT = f_sub)");

        // Test advanced options
        VerifiedStmt("CREATE TYPE t (INPUT = f_in, OUTPUT = f_out, LIKE = INT, CATEGORY = 'N', PREFERRED = true, DEFAULT = 0, ELEMENT = INTEGER, DELIMITER = ',', COLLATABLE = false)");
    }

    [Fact]
    public void parse_drop_type()
    {
        var sql = "DROP TYPE abc";
        {
            var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
            TestUtils.AssertEqVec(new[] { "abc" }, drop.Names);
            Assert.Equal<ObjectType>(new ObjectType.Type(), drop.ObjectType);
            Assert.False(drop.IfExists);
            Assert.False(drop.Cascade);
        }

        sql = "DROP TYPE IF EXISTS def, magician, quaternion";
        {
            var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
            TestUtils.AssertEqVec(new[] { "def", "magician", "quaternion" }, drop.Names);
            Assert.Equal<ObjectType>(new ObjectType.Type(), drop.ObjectType);
            Assert.True(drop.IfExists);
            Assert.False(drop.Cascade);
        }

        sql = "DROP TYPE IF EXISTS my_type CASCADE";
        {
            var drop = Assert.IsType<Statement.Drop>(VerifiedStmt(sql));
            TestUtils.AssertEqVec(new[] { "my_type" }, drop.Names);
            Assert.Equal<ObjectType>(new ObjectType.Type(), drop.ObjectType);
            Assert.True(drop.IfExists);
            Assert.True(drop.Cascade);
        }
    }

    [Fact]
    public void parse_call()
    {
        TestUtils.AllDialects().VerifiedStmt("CALL my_procedure()");
        TestUtils.AllDialects().VerifiedStmt("CALL my_procedure(1, 'a')");
        PgAndGeneric().VerifiedStmt("CALL my_procedure(1, 'a', $1)");
        TestUtils.AllDialects().VerifiedStmt("CALL my_procedure");
        Assert.Equal(
            VerifiedStmt("CALL my_procedure('a')"),
            new Statement.Call(new Function(
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: null,
                    Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                        new Value.SingleQuotedString("a").WithEmptySpan())))),
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                Name: ObjectName.From(Sequence.Of(new Ident("my_procedure"))),
                Filter: null,
                NullTreatment: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)));
    }

    [Fact]
    public void parse_execute_stored_procedure()
    {
        Statement expected = new Statement.Execute(
            Name: ObjectName.From(Sequence.Of(
                new Ident("my_schema", null, Span.Empty()),
                new Ident("my_stored_procedure", null, Span.Empty()))),
            Parameters: Sequence.Of<Expr>(
                new Expr.Value(new Value.NationalStringLiteral("param1").WithEmptySpan()),
                new Expr.Value(new Value.NationalStringLiteral("param2").WithEmptySpan())),
            HasParentheses: false,
            Immediate: false,
            Using: Sequence<ExprWithAlias>.Empty,
            Into: Sequence<Ident>.Empty,
            Output: false,
            Default: false);
        Assert.Equal(
            // Microsoft SQL Server does not use parentheses around arguments for EXECUTE
            MsAndGeneric()
                .VerifiedStmt("EXECUTE my_schema.my_stored_procedure N'param1', N'param2'"),
            expected);
        Assert.Equal(
            MsAndGeneric().OneStatementParsesTo(
                "EXEC my_schema.my_stored_procedure N'param1', N'param2';",
                "EXECUTE my_schema.my_stored_procedure N'param1', N'param2'"),
            expected);
        {
            var execute = Assert.IsType<Statement.Execute>(MsAndGeneric().VerifiedStmt("EXECUTE dbo.proc1 @ReturnVal = @X OUTPUT"));
            Assert.True(execute.Output);
        }
        {
            var execute = Assert.IsType<Statement.Execute>(MsAndGeneric().VerifiedStmt("EXECUTE dbo.proc1 DEFAULT"));
            Assert.True(execute.Default);
        }
        // Parenthesised form is dynamic SQL; the expression ends up in parameters.
        MsAndGeneric().OneStatementParsesTo("EXEC ('name')", "EXECUTE ('name')");
    }

    [Fact]
    public void parse_execute_immediate()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsExecuteImmediate);

        Statement expected = new Statement.Execute(
            Parameters: Sequence.Of<Expr>(new Expr.Value(
                new Value.SingleQuotedString("SELECT 1").WithEmptySpan())),
            Immediate: true,
            Using: Sequence.Of(new ExprWithAlias(
                Expr: Expr.MakeValue(TestUtils.Number("1")),
                Alias: new Ident("b"))),
            Into: Sequence.Of(new Ident("a")),
            Name: null,
            HasParentheses: false,
            Output: false,
            Default: false);

        var stmt = dialects.VerifiedStmt("EXECUTE IMMEDIATE 'SELECT 1' INTO a USING 1 AS b");
        Assert.Equal(expected, stmt);

        dialects.VerifiedStmt("EXECUTE IMMEDIATE 'SELECT 1' INTO a, b USING 1 AS x, y");
        dialects.VerifiedStmt("EXECUTE IMMEDIATE 'SELECT 1' USING 1 AS x, y");
        dialects.VerifiedStmt("EXECUTE IMMEDIATE 'SELECT 1' INTO a, b");
        dialects.VerifiedStmt("EXECUTE IMMEDIATE 'SELECT 1'");
        dialects.VerifiedStmt("EXECUTE 'SELECT 1'");

        var ex = Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements("EXECUTE IMMEDIATE 'SELECT 1' USING 1 AS, y"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: identifier, found: ,"),
            ex.Error);
    }

    [Fact]
    public void parse_create_table_collate()
    {
        TestUtils.AllDialects().VerifiedStmt("CREATE TABLE tbl (foo INT, bar TEXT COLLATE \"de_DE\")");
        // check ordering is preserved
        TestUtils.AllDialects().VerifiedStmt(
            "CREATE TABLE tbl (foo INT, bar TEXT CHARACTER SET utf8mb4 COLLATE \"de_DE\")");
        TestUtils.AllDialects().VerifiedStmt(
            "CREATE TABLE tbl (foo INT, bar TEXT COLLATE \"de_DE\" CHARACTER SET utf8mb4)");
    }

    [Fact]
    public void parse_binary_operators_without_whitespace()
    {
        // x + y
        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT field+1000 FROM tbl1",
            "SELECT field + 1000 FROM tbl1");

        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT tbl1.field+tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id",
            "SELECT tbl1.field + tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id");

        // x - y
        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT field-1000 FROM tbl1",
            "SELECT field - 1000 FROM tbl1");

        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT tbl1.field-tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id",
            "SELECT tbl1.field - tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id");

        // x * y
        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT field*1000 FROM tbl1",
            "SELECT field * 1000 FROM tbl1");

        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT tbl1.field*tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id",
            "SELECT tbl1.field * tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id");

        // x / y
        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT field/1000 FROM tbl1",
            "SELECT field / 1000 FROM tbl1");

        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT tbl1.field/tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id",
            "SELECT tbl1.field / tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id");

        // x % y
        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT field%1000 FROM tbl1",
            "SELECT field % 1000 FROM tbl1");

        TestUtils.AllDialects().OneStatementParsesTo(
            "SELECT tbl1.field%tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id",
            "SELECT tbl1.field % tbl2.field FROM tbl1 JOIN tbl2 ON tbl1.id = tbl2.entity_id");
    }

    [Fact]
    public void parse_unload()
    {
        var unload = VerifiedStmt("UNLOAD(SELECT cola FROM tab) TO 's3://...' WITH (format = 'AVRO')");
        Assert.Equal(
            unload,
            new Statement.Unload(
                Query: new Query(
                    Body: new SetExpr.Select(new Select(
                        SelectToken: AttachedToken.Empty(),
                        OptimizerHints: Sequence<OptimizerHint>.Empty,
                        Distinct: null,
                        SelectModifiers: null,
                        Top: null,
                        TopBeforeDistinct: false,
                        Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("cola")))),
                        Exclude: null,
                        Into: null,
                        From: Sequence.Of(new TableWithJoins(
                            Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("tab")))),
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
                    With: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    OrderBy: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty),
                To: new Ident("s3://...", '\'', Span.Empty()),
                With: Sequence.Of<SqlOption>(new SqlOption.KeyValue(
                    Key: new Ident("format", null, Span.Empty()),
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("AVRO").WithEmptySpan()))),
                QueryText: null,
                Auth: null,
                Options: Sequence<CopyLegacyOption>.Empty));

        OneStatementParsesTo(
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "FORMAT AS CSV " +
            "FORMAT AS PARQUET " +
            "FORMAT AS JSON " +
            "MAXFILESIZE AS 10 MB " +
            "ROWGROUPSIZE AS 10 MB " +
            "PARALLEL ON " +
            "PARALLEL OFF " +
            "REGION AS 'us-east-1'",
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "CSV " +
            "PARQUET " +
            "JSON " +
            "MAXFILESIZE 10 MB " +
            "ROWGROUPSIZE 10 MB " +
            "PARALLEL TRUE " +
            "PARALLEL FALSE " +
            "REGION 'us-east-1'");

        VerifiedStmt(
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "PARTITION BY (c1, c2, c3)");
        VerifiedStmt(
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "PARTITION BY (c1, c2, c3) INCLUDE");

        VerifiedStmt(
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "PARTITION BY (c1, c2, c3) INCLUDE " +
            "MANIFEST");
        VerifiedStmt(
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "PARTITION BY (c1, c2, c3) INCLUDE " +
            "MANIFEST VERBOSE");

        VerifiedStmt(
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "PARTITION BY (c1, c2, c3) INCLUDE " +
            "MANIFEST VERBOSE " +
            "HEADER " +
            "FIXEDWIDTH 'col1:1,col2:2' " +
            "ENCRYPTED");
        VerifiedStmt(
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "PARTITION BY (c1, c2, c3) INCLUDE " +
            "MANIFEST VERBOSE " +
            "HEADER " +
            "FIXEDWIDTH 'col1:1,col2:2' " +
            "ENCRYPTED AUTO");

        VerifiedStmt(
            "UNLOAD('SELECT 1') " +
            "TO 's3://...' " +
            "IAM_ROLE 'arn:aws:iam::123456789:role/role1' " +
            "PARTITION BY (c1, c2, c3) INCLUDE " +
            "MANIFEST VERBOSE " +
            "HEADER " +
            "FIXEDWIDTH 'col1:1,col2:2' " +
            "ENCRYPTED AUTO " +
            "BZIP2 " +
            "GZIP " +
            "ZSTD " +
            "ADDQUOTES " +
            "NULL 'nil' " +
            "ESCAPE " +
            "ALLOWOVERWRITE " +
            "CLEANPATH " +
            "PARALLEL " +
            "PARALLEL TRUE " +
            "PARALLEL FALSE " +
            "MAXFILESIZE 10 " +
            "MAXFILESIZE 10 MB " +
            "MAXFILESIZE 10 GB " +
            "ROWGROUPSIZE 10 " +
            "ROWGROUPSIZE 10 MB " +
            "ROWGROUPSIZE 10 GB " +
            "REGION 'us-east-1' " +
            "EXTENSION 'ext1'");
    }

    [Fact]
    public void test_savepoint()
    {
        var savepoint = Assert.IsType<Statement.Savepoint>(VerifiedStmt("SAVEPOINT test1"));
        Assert.Equal(new Ident("test1"), savepoint.Name);
    }

    [Fact]
    public void test_release_savepoint()
    {
        var releaseSavepoint = Assert.IsType<Statement.ReleaseSavepoint>(VerifiedStmt("RELEASE SAVEPOINT test1"));
        Assert.Equal(new Ident("test1"), releaseSavepoint.Name);

        OneStatementParsesTo("RELEASE test1", "RELEASE SAVEPOINT test1");
    }

    [Fact]
    public void test_comment_hash_syntax()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new BigQueryDialect(),
            new SnowflakeDialect(),
            new MySqlDialect(),
            new HiveDialect(),
        });
        // r#"..."# com várias linhas: quebras e indentação explícitas.
        var sql =
            "\n" +
            "    # comment\n" +
            "    SELECT a, b, c # , d, e\n" +
            "    FROM T\n" +
            "    ####### comment #################\n" +
            "    WHERE true\n" +
            "    # comment\n" +
            "    ";
        var canonical = "SELECT a, b, c FROM T WHERE true";
        dialects.VerifiedOnlySelectWithCanonical(sql, canonical);
    }

    [Fact]
    public void test_parse_inline_comment()
    {
        var sql =
            "CREATE TABLE t0 (id INT COMMENT 'comment without equal') COMMENT = 'comment with equal'";
        // Hive dialect doesn't support `=` in table comment, please refer:
        // [Hive](https://cwiki.apache.org/confluence/display/Hive/LanguageManual+DDL#LanguageManualDDL-CreateTable)
        var createTable = Assert.IsType<Statement.CreateTable>(
            TestUtils.AllDialectsExcept(d => d is HiveDialect).VerifiedStmt(sql)).Value;
        Assert.Equal(
            createTable.Columns,
            Sequence.Of(new ColumnDef(
                Name: new Ident("id"),
                DataType: new DataType.Int(null),
                Options: Sequence.Of(new ColumnOptionDef(
                    Name: null,
                    Option: new ColumnOption.Comment("comment without equal"))))));
        Assert.Equal(
            createTable.TableOptions,
            new CreateTableOptions.Plain(Sequence.Of<SqlOption>(new SqlOption.Comment(new CommentDef.WithEq(
                "comment with equal")))));
    }

    [Fact]
    public void test_buffer_reuse()
    {
        var d = new GenericDialect();
        var q = "INSERT INTO customer WITH foo AS (SELECT 1) SELECT * FROM foo UNION VALUES (1)";
        var buf = new List<TokenWithSpan>();
        new Tokenizer(d, q)
            .TokenizeWithLocationIntoBuf(buf);
        var p = new Parser(d).WithTokensWithLocations(buf);
        p.ParseStatements();
        _ = p.IntoTokens();
    }

    [Fact]
    public void parse_map_access_expr()
    {
        var sql = "users[-1][safe_offset(2)]";
        var dialects = new TestedDialects(new List<Dialect>
        {
            new BigQueryDialect(),
            new ClickHouseDialect(),
        });
        var expr = dialects.VerifiedExpr(sql);
        Expr expected = new Expr.CompoundFieldAccess(
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
                        UsesOdbcSyntax: false))))));
        Assert.Equal(expr, expected);

        foreach (var s in new[] { "users[1]", "a[array_length(b) - 1 + 2][c + 3][d * 4]" })
        {
            _ = dialects.VerifiedExpr(s);
        }
    }

    [Fact]
    public void parse_connect_by()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsConnectBy);

        var connectBy1 =
            "SELECT employee_id, manager_id, title FROM employees " +
            "START WITH title = 'president' " +
            "CONNECT BY manager_id = PRIOR employee_id " +
            "ORDER BY employee_id";

        Assert.Equal(
            dialects.VerifiedOnlySelect(connectBy1),
            new Select(
                SelectToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Distinct: null,
                SelectModifiers: null,
                Top: null,
                TopBeforeDistinct: false,
                Projection: Sequence.Of<SelectItem>(
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("employee_id"))),
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("manager_id"))),
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("title")))),
                Exclude: null,
                From: Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("employees")))),
                    Joins: Sequence<Join>.Empty)),
                Into: null,
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
                ConnectBy: Sequence.Of<ConnectByKind>(
                    new ConnectByKind.StartWith(
                        StartToken: AttachedToken.Empty(),
                        Condition: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("title")),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.Value(
                                new Value.SingleQuotedString("president").WithEmptySpan()))),
                    new ConnectByKind.ConnectBy(
                        ConnectToken: AttachedToken.Empty(),
                        Nocycle: false,
                        Relationships: Sequence.Of<Expr>(new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("manager_id")),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.Prior(new Expr.Identifier(new Ident(
                                "employee_id"))))))),
                Flavor: SelectFlavor.Standard));

        // CONNECT BY can come before START WITH
        var connectBy2 =
            "SELECT employee_id, manager_id, title FROM employees " +
            "CONNECT BY manager_id = PRIOR employee_id " +
            "START WITH title = 'president' " +
            "ORDER BY employee_id";
        Assert.Equal(
            dialects.VerifiedOnlySelect(connectBy2),
            new Select(
                SelectToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Distinct: null,
                SelectModifiers: null,
                Top: null,
                TopBeforeDistinct: false,
                Projection: Sequence.Of<SelectItem>(
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("employee_id"))),
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("manager_id"))),
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("title")))),
                Exclude: null,
                From: Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("employees")))),
                    Joins: Sequence<Join>.Empty)),
                Into: null,
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
                ConnectBy: Sequence.Of<ConnectByKind>(
                    new ConnectByKind.ConnectBy(
                        ConnectToken: AttachedToken.Empty(),
                        Nocycle: false,
                        Relationships: Sequence.Of<Expr>(new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("manager_id")),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.Prior(new Expr.Identifier(new Ident(
                                "employee_id")))))),
                    new ConnectByKind.StartWith(
                        StartToken: AttachedToken.Empty(),
                        Condition: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("title")),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.Value(
                                new Value.SingleQuotedString("president").WithEmptySpan())))),
                Flavor: SelectFlavor.Standard));

        // WHERE must come before CONNECT BY
        var connectBy3 =
            "SELECT employee_id, manager_id, title FROM employees " +
            "WHERE employee_id <> 42 " +
            "START WITH title = 'president' " +
            "CONNECT BY manager_id = PRIOR employee_id " +
            "ORDER BY employee_id";
        Assert.Equal(
            dialects.VerifiedOnlySelect(connectBy3),
            new Select(
                SelectToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Distinct: null,
                SelectModifiers: null,
                Top: null,
                TopBeforeDistinct: false,
                Projection: Sequence.Of<SelectItem>(
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("employee_id"))),
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("manager_id"))),
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("title")))),
                Exclude: null,
                From: Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("employees")))),
                    Joins: Sequence<Join>.Empty)),
                Into: null,
                LateralViews: Sequence<LateralView>.Empty,
                Prewhere: null,
                Selection: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("employee_id")),
                    Op: new BinaryOperator.NotEq(),
                    Right: Expr.MakeValue(TestUtils.Number("42"))),
                GroupBy: new GroupByExpr.Expressions(Sequence<Expr>.Empty, Sequence<GroupByWithModifier>.Empty),
                ClusterBy: Sequence<Expr>.Empty,
                DistributeBy: Sequence<Expr>.Empty,
                SortBy: Sequence<OrderByExpr>.Empty,
                Having: null,
                NamedWindow: Sequence<NamedWindowDefinition>.Empty,
                Qualify: null,
                WindowBeforeQualify: false,
                ValueTableMode: null,
                ConnectBy: Sequence.Of<ConnectByKind>(
                    new ConnectByKind.StartWith(
                        StartToken: AttachedToken.Empty(),
                        Condition: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("title")),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.Value(
                                new Value.SingleQuotedString("president").WithEmptySpan()))),
                    new ConnectByKind.ConnectBy(
                        ConnectToken: AttachedToken.Empty(),
                        Nocycle: false,
                        Relationships: Sequence.Of<Expr>(new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("manager_id")),
                            Op: new BinaryOperator.Eq(),
                            Right: new Expr.Prior(new Expr.Identifier(new Ident(
                                "employee_id"))))))),
                Flavor: SelectFlavor.Standard));

        var connectBy4 =
            "SELECT employee_id, manager_id, title FROM employees " +
            "START WITH title = 'president' " +
            "CONNECT BY manager_id = PRIOR employee_id " +
            "WHERE employee_id <> 42 " +
            "ORDER BY employee_id";
        // .expect_err("should have failed")
        Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements(connectBy4));

        // PRIOR expressions are only valid within a CONNECT BY, and the the token
        // `prior` is valid as an identifier anywhere else.
        Assert.Equal(
            TestUtils.AllDialects()
                .VerifiedOnlySelect("SELECT prior FROM some_table")
                .Projection,
            Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident(
                "prior")))));

        // no START WITH and NOCYCLE
        var connectBy5 = "SELECT child, parent FROM t CONNECT BY NOCYCLE parent = PRIOR child";
        Assert.Equal(
            dialects.VerifiedOnlySelect(connectBy5),
            new Select(
                SelectToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Distinct: null,
                SelectModifiers: null,
                Top: null,
                TopBeforeDistinct: false,
                Projection: Sequence.Of<SelectItem>(
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("child"))),
                    new SelectItem.UnnamedExpr(new Expr.Identifier(new Ident("parent")))),
                Exclude: null,
                From: Sequence.Of(new TableWithJoins(
                    Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("t")))),
                    Joins: Sequence<Join>.Empty)),
                Into: null,
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
                ConnectBy: Sequence.Of<ConnectByKind>(new ConnectByKind.ConnectBy(
                    ConnectToken: AttachedToken.Empty(),
                    Nocycle: true,
                    Relationships: Sequence.Of<Expr>(new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("parent")),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Prior(new Expr.Identifier(new Ident("child"))))))),
                Flavor: SelectFlavor.Standard));

        // CONNECT BY after WHERE and before GROUP BY
        dialects.VerifiedOnlySelect("SELECT 0 FROM t WHERE 1 = 1 CONNECT BY 2 = 2 GROUP BY 3");
        dialects.VerifiedOnlySelect(
            "SELECT 0 FROM t WHERE 1 = 1 START WITH 'a' = 'a' CONNECT BY 2 = 2 GROUP BY 3");
        dialects.VerifiedOnlySelect(
            "SELECT 0 FROM t WHERE 1 = 1 CONNECT BY 2 = 2 START WITH 'a' = 'a' GROUP BY 3");
    }

    [Fact]
    public void test_selective_aggregation()
    {
        var testingDialects = TestUtils.AllDialectsWhere(d => d.SupportsFilterDuringAggregation);
        var expectedDialects = new List<Dialect>
        {
            new PostgreSqlDialect(),
            new DatabricksDialect(),
            new HiveDialect(),
            new SQLiteDialect(),
            new DuckDbDialect(),
            new GenericDialect(),
        };
        Assert.Equal(testingDialects.Dialects.Count, expectedDialects.Count);
        expectedDialects
            .ForEach(d => Assert.True(d.SupportsFilterDuringAggregation));

        var sql =
            "SELECT " +
            "ARRAY_AGG(name) FILTER (WHERE name IS NOT NULL), " +
            "ARRAY_AGG(name) FILTER (WHERE name LIKE 'a%') AS agg2 " +
            "FROM region";
        Assert.Equal(
            testingDialects.VerifiedOnlySelect(sql).Projection,
            Sequence.Of<SelectItem>(
                new SelectItem.UnnamedExpr(new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(new Ident("ARRAY_AGG"))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.List(new FunctionArgumentList(
                        DuplicateTreatment: null,
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                            new Expr.Identifier(new Ident("name"))))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                    Filter: new Expr.IsNotNull(new Expr.Identifier(
                        new Ident("name"))),
                    Over: null,
                    WithinGroup: Sequence<OrderByExpr>.Empty,
                    NullTreatment: null))),
                new SelectItem.ExprWithAlias(
                    Expr: new Expr.Function(new Function(
                        Name: ObjectName.From(Sequence.Of(new Ident("ARRAY_AGG"))),
                        UsesOdbcSyntax: false,
                        Parameters: new FunctionArguments.None(),
                        Args: new FunctionArguments.List(new FunctionArgumentList(
                            DuplicateTreatment: null,
                            Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                                new Expr.Identifier(new Ident("name"))))),
                            Clauses: Sequence<FunctionArgumentClause>.Empty)),
                        Filter: new Expr.Like(
                            Negated: false,
                            Expr: new Expr.Identifier(new Ident("name")),
                            Pattern: new Expr.Value(
                                new Value.SingleQuotedString("a%").WithEmptySpan()),
                            EscapeChar: null,
                            Any: false),
                        NullTreatment: null,
                        Over: null,
                        WithinGroup: Sequence<OrderByExpr>.Empty)),
                    Alias: new Ident("agg2"))));
    }

    [Fact]
    public void test_group_by_grouping_sets()
    {
        var sql =
            "SELECT city, car_model, sum(quantity) AS sum " +
            "FROM dealer " +
            "GROUP BY GROUPING SETS ((city, car_model), (city), (car_model), ()) " +
            "ORDER BY city";
        Assert.Equal(
            TestUtils.AllDialectsWhere(d => d.SupportsGroupByExpr)
                .VerifiedOnlySelect(sql)
                .GroupBy,
            new GroupByExpr.Expressions(
                Sequence.Of<Expr>(new Expr.GroupingSets(Sequence.Of(
                    Sequence.Of<Expr>(
                        new Expr.Identifier(new Ident("city")),
                        new Expr.Identifier(new Ident("car_model"))),
                    Sequence.Of<Expr>(new Expr.Identifier(new Ident("city"))),
                    Sequence.Of<Expr>(new Expr.Identifier(new Ident("car_model"))),
                    Sequence<Expr>.Empty))),
                Sequence<GroupByWithModifier>.Empty));
    }

    [Fact]
    public void test_group_by_grouping_sets_bare_columns()
    {
        TestUtils.AllDialectsWhere(d => d.SupportsGroupByExpr).OneStatementParsesTo(
            "SELECT a, b FROM t GROUP BY GROUPING SETS (a, b, c)",
            "SELECT a, b FROM t GROUP BY GROUPING SETS ((a), (b), (c))");

        TestUtils.AllDialectsWhere(d => d.SupportsGroupByExpr).OneStatementParsesTo(
            "SELECT a, b FROM t GROUP BY GROUPING SETS ((a, b), c)",
            "SELECT a, b FROM t GROUP BY GROUPING SETS ((a, b), (c))");
    }

    [Fact]
    public void test_xmltable()
    {
        TestUtils.AllDialects()
            .VerifiedOnlySelect("SELECT * FROM XMLTABLE('/root' PASSING data COLUMNS element TEXT)");

        // Minimal meaningful working example: returns a single row with a single column named y containing the value z
        TestUtils.AllDialects().VerifiedOnlySelect(
            "SELECT y FROM XMLTABLE('/X' PASSING '<X><y>z</y></X>' COLUMNS y TEXT)");

        // Test using subqueries
        TestUtils.AllDialects().VerifiedOnlySelect("SELECT y FROM XMLTABLE((SELECT '/X') PASSING (SELECT CAST('<X><y>z</y></X>' AS xml)) COLUMNS y TEXT PATH (SELECT 'y'))");

        // NOT NULL
        TestUtils.AllDialects().VerifiedOnlySelect(
            "SELECT y FROM XMLTABLE('/X' PASSING '<X></X>' COLUMNS y TEXT NOT NULL)");

        TestUtils.AllDialects().VerifiedOnlySelect("SELECT * FROM XMLTABLE('/root/row' PASSING xmldata COLUMNS id INT PATH '@id', name TEXT PATH 'name/text()', value FLOAT PATH 'value')");

        TestUtils.AllDialects().VerifiedOnlySelect("SELECT * FROM XMLTABLE('//ROWS/ROW' PASSING data COLUMNS row_num FOR ORDINALITY, id INT PATH '@id', name TEXT PATH 'NAME' DEFAULT 'unnamed')");

        // Example from https://www.postgresql.org/docs/15/functions-xml.html#FUNCTIONS-XML-PROCESSING
        TestUtils.AllDialects().VerifiedOnlySelect(
            "SELECT xmltable.* FROM xmldata, XMLTABLE('//ROWS/ROW' PASSING data COLUMNS id INT PATH '@id', ordinality FOR ORDINALITY, \"COUNTRY_NAME\" TEXT, country_id TEXT PATH 'COUNTRY_ID', size_sq_km FLOAT PATH 'SIZE[@unit = \"sq_km\"]', size_other TEXT PATH 'concat(SIZE[@unit!=\"sq_km\"], \" \", SIZE[@unit!=\"sq_km\"]/@unit)', premier_name TEXT PATH 'PREMIER_NAME' DEFAULT 'not specified')");

        // Example from DB2 docs without explicit PASSING clause: https://www.ibm.com/docs/en/db2/12.1.0?topic=xquery-simple-column-name-passing-xmlexists-xmlquery-xmltable
        TestUtils.AllDialects().VerifiedOnlySelect(
            "SELECT X.* FROM T1, XMLTABLE('$CUSTLIST/customers/customerinfo' COLUMNS \"Cid\" BIGINT PATH '@Cid', \"Info\" XML PATH 'document{.}', \"History\" XML PATH 'NULL') AS X");

        // Example from PostgreSQL with XMLNAMESPACES
        TestUtils.AllDialects().VerifiedOnlySelect(
            "SELECT xmltable.* FROM XMLTABLE(XMLNAMESPACES('http://example.com/myns' AS x, 'http://example.com/b' AS \"B\"), '/x:example/x:item' PASSING (SELECT data FROM xmldata) COLUMNS foo INT PATH '@foo', bar INT PATH '@B:bar')");
    }

    [Fact]
    public void test_match_recognize()
    {
        // use MatchRecognizePattern::*; use MatchRecognizeSymbol::*; use RepetitionQuantifier::*;
        // → nomes qualificados.

        var table = TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident("my_table"))));

        static void Check(string options, TableFactor expect)
        {
            var select = TestUtils.AllDialectsWhere(d => d.SupportsMatchRecognize).VerifiedOnlySelect(
                $"SELECT * FROM my_table MATCH_RECOGNIZE({options})");
            Assert.Equal(select.From[0].Relation, expect);
        }

        Check(
            "PARTITION BY company " +
            "ORDER BY price_date " +
            "MEASURES " +
            "MATCH_NUMBER() AS match_number, " +
            "FIRST(price_date) AS start_date, " +
            "LAST(price_date) AS end_date " +
            "ONE ROW PER MATCH " +
            "AFTER MATCH SKIP TO LAST row_with_price_increase " +
            "PATTERN (row_before_decrease row_with_price_decrease+ row_with_price_increase+) " +
            "DEFINE " +
            "row_with_price_decrease AS price < LAG(price), " +
            "row_with_price_increase AS price > LAG(price)",
            new TableFactor.MatchRecognize(
                Table: table,
                PartitionBy: Sequence.Of<Expr>(new Expr.Identifier(new Ident("company"))),
                OrderBy: Sequence.Of(new OrderByExpr(
                    Expr: new Expr.Identifier(new Ident("price_date")),
                    Options: new OrderByOptions(
                        Sort: null,
                        NullsFirst: null),
                    WithFill: null)),
                Measures: Sequence.Of(
                    new Measure(
                        Expr: TestUtils.Call("MATCH_NUMBER", Sequence<Expr>.Empty),
                        Alias: new Ident("match_number")),
                    new Measure(
                        Expr: TestUtils.Call("FIRST", new Expr[] { new Expr.Identifier(new Ident("price_date")) }),
                        Alias: new Ident("start_date")),
                    new Measure(
                        Expr: TestUtils.Call("LAST", new Expr[] { new Expr.Identifier(new Ident("price_date")) }),
                        Alias: new Ident("end_date"))),
                RowsPerMatch: new RowsPerMatch.OneRow(),
                AfterMatchSkip: new AfterMatchSkip.ToLast(new Ident(
                    "row_with_price_increase")),
                Pattern: new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                    new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("row_before_decrease"))),
                    new MatchRecognizePattern.Repetition(
                        new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("row_with_price_decrease"))),
                        new RepetitionQuantifier.OneOrMore()),
                    new MatchRecognizePattern.Repetition(
                        new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("row_with_price_increase"))),
                        new RepetitionQuantifier.OneOrMore()))),
                Symbols: Sequence.Of(
                    new SymbolDefinition(
                        Symbol: new Ident("row_with_price_decrease"),
                        Definition: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("price")),
                            Op: new BinaryOperator.Lt(),
                            Right: TestUtils.Call("LAG", new Expr[] { new Expr.Identifier(new Ident("price")) }))),
                    new SymbolDefinition(
                        Symbol: new Ident("row_with_price_increase"),
                        Definition: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("price")),
                            Op: new BinaryOperator.Gt(),
                            Right: TestUtils.Call("LAG", new Expr[] { new Expr.Identifier(new Ident("price")) })))),
                Alias: null));

        var examples = new[]
        {
            "SELECT * " +
            "FROM login_attempts " +
            "MATCH_RECOGNIZE(" +
                "PARTITION BY user_id " +
                "ORDER BY timestamp " +
                "PATTERN (failed_attempt{3,}) " +
                "DEFINE " +
                    "failed_attempt AS status = 'failure'" +
            ")",
            "SELECT * " +
            "FROM stock_transactions " +
            "MATCH_RECOGNIZE(" +
                "PARTITION BY symbol " +
                "ORDER BY timestamp " +
                "MEASURES " +
                    "FIRST(price) AS start_price, " +
                    "LAST(price) AS end_price, " +
                    "MATCH_NUMBER() AS match_num " +
                "ALL ROWS PER MATCH " +
                "PATTERN (STRT UP+) " +
                "DEFINE " +
                    "UP AS price > PREV(price)" +
            ")",
            "SELECT * " +
            "FROM event_log " +
            "MATCH_RECOGNIZE(" +
                "MEASURES " +
                    "FIRST(event_type) AS start_event, " +
                    "LAST(event_type) AS end_event, " +
                    "COUNT(*) AS error_count " +
                "ALL ROWS PER MATCH " +
                "PATTERN (STRT ERROR+ END) " +
                "DEFINE " +
                    "STRT AS event_type = 'START', " +
                    "ERROR AS event_type = 'ERROR', " +
                    "END AS event_type = 'END'" +
            ")",
        };

        foreach (var sql in examples)
        {
            TestUtils.AllDialectsWhere(d => d.SupportsMatchRecognize).VerifiedQuery(sql);
        }
    }

    [Fact]
    public void test_match_recognize_patterns()
    {
        // use MatchRecognizePattern::*; use MatchRecognizeSymbol::*; use RepetitionQuantifier::*;
        // → nomes qualificados.

        static void Check(string pattern, MatchRecognizePattern expect)
        {
            var select =
                TestUtils.AllDialectsWhere(d => d.SupportsMatchRecognize).VerifiedOnlySelect(
                    $"SELECT * FROM my_table MATCH_RECOGNIZE(PATTERN ({pattern}) DEFINE DUMMY AS 1 = 1)" // "select * from my_table match_recognize ("
                );
            if (select.From[0].Relation is not TableFactor.MatchRecognize { Pattern: var actual })
            {
                Assert.Fail("expected match_recognize table factor");
                return;
            }
            Assert.Equal(actual, expect);
        }

        // just a symbol
        Check("FOO", new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("FOO"))));

        // just a symbol
        Check(
            "^ FOO $",
            new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Start()),
                new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("FOO"))),
                new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.End()))));

        // exclusion
        Check("{- FOO -}", new MatchRecognizePattern.Exclude(new MatchRecognizeSymbol.Named(new Ident("FOO"))));

        Check(
            "PERMUTE(A, B, C)",
            new MatchRecognizePattern.Permute(Sequence.Of<MatchRecognizeSymbol>(
                new MatchRecognizeSymbol.Named(new Ident("A")),
                new MatchRecognizeSymbol.Named(new Ident("B")),
                new MatchRecognizeSymbol.Named(new Ident("C")))));

        // various identifiers
        Check(
            "FOO | \"BAR\" | baz42",
            new MatchRecognizePattern.Alternation(Sequence.Of<MatchRecognizePattern>(
                new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("FOO"))),
                new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(Ident.WithQuote('"', "BAR"))),
                new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("baz42"))))));

        // concatenated basic quantifiers
        Check(
            "S1* S2+ S3?",
            new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                new MatchRecognizePattern.Repetition(new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S1"))), new RepetitionQuantifier.ZeroOrMore()),
                new MatchRecognizePattern.Repetition(new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S2"))), new RepetitionQuantifier.OneOrMore()),
                new MatchRecognizePattern.Repetition(new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S3"))), new RepetitionQuantifier.AtMostOne()))));

        // reluctant repetition
        Check(
            "S2*?",
            new MatchRecognizePattern.Repetition(
                new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S2"))),
                new RepetitionQuantifier.Reluctant(new RepetitionQuantifier.ZeroOrMore())));

        Check(
            "S1+? S2?? S3{2,4}?",
            new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                new MatchRecognizePattern.Repetition(
                    new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S1"))),
                    new RepetitionQuantifier.Reluctant(new RepetitionQuantifier.OneOrMore())),
                new MatchRecognizePattern.Repetition(
                    new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S2"))),
                    new RepetitionQuantifier.Reluctant(new RepetitionQuantifier.AtMostOne())),
                new MatchRecognizePattern.Repetition(
                    new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S3"))),
                    new RepetitionQuantifier.Reluctant(new RepetitionQuantifier.Range(2, 4))))));

        foreach (var pattern in new[] { "S1**", "S1+++", "S1???", "S1{2,4}+" })
        {
            var sql =
                $"SELECT * FROM my_table MATCH_RECOGNIZE(PATTERN ({pattern}) DEFINE DUMMY AS 1 = 1)";
            // "stacked quantifier should fail: {pattern}"
            Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => d.SupportsMatchRecognize)
                .ParseSqlStatements(sql));
        }

        // range quantifiers in an alternation
        Check(
            "S1{1} | S2{2,3} | S3{4,} | S4{,5}",
            new MatchRecognizePattern.Alternation(Sequence.Of<MatchRecognizePattern>(
                new MatchRecognizePattern.Repetition(new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S1"))), new RepetitionQuantifier.Exactly(1)),
                new MatchRecognizePattern.Repetition(new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S2"))), new RepetitionQuantifier.Range(2, 3)),
                new MatchRecognizePattern.Repetition(new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S3"))), new RepetitionQuantifier.AtLeast(4)),
                new MatchRecognizePattern.Repetition(new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S4"))), new RepetitionQuantifier.AtMost(5)))));

        // grouping case 1
        Check(
            "S1 ( S2 )",
            new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S1"))),
                new MatchRecognizePattern.Group(new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S2")))))));

        // grouping case 2
        Check(
            "( {- S3 -} S4 )+",
            new MatchRecognizePattern.Repetition(
                new MatchRecognizePattern.Group(new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                    new MatchRecognizePattern.Exclude(new MatchRecognizeSymbol.Named(new Ident("S3"))),
                    new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S4")))))),
                new RepetitionQuantifier.OneOrMore()));

        // the grand finale (example taken from snowflake docs)
        Check(
            "^ S1 S2*? ( {- S3 -} S4 )+ | PERMUTE(S1, S2){1,2} $",
            new MatchRecognizePattern.Alternation(Sequence.Of<MatchRecognizePattern>(
                new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                    new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Start()),
                    new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S1"))),
                    new MatchRecognizePattern.Repetition(
                        new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S2"))),
                        new RepetitionQuantifier.Reluctant(new RepetitionQuantifier.ZeroOrMore())),
                    new MatchRecognizePattern.Repetition(
                        new MatchRecognizePattern.Group(new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                            new MatchRecognizePattern.Exclude(new MatchRecognizeSymbol.Named(new Ident("S3"))),
                            new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(new Ident("S4")))))),
                        new RepetitionQuantifier.OneOrMore()))),
                new MatchRecognizePattern.Concat(Sequence.Of<MatchRecognizePattern>(
                    new MatchRecognizePattern.Repetition(
                        new MatchRecognizePattern.Permute(Sequence.Of<MatchRecognizeSymbol>(
                            new MatchRecognizeSymbol.Named(new Ident("S1")),
                            new MatchRecognizeSymbol.Named(new Ident("S2")))),
                        new RepetitionQuantifier.Range(1, 2)),
                    new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.End()))))));
    }

    [Fact]
    public void test_select_wildcard_with_replace()
    {
        var sql = "SELECT * REPLACE (lower(city) AS city) FROM addresses";
        var dialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new BigQueryDialect(),
            new ClickHouseDialect(),
            new SnowflakeDialect(),
            new DuckDbDialect(),
        });
        var select = dialects.VerifiedOnlySelect(sql);
        SelectItem expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptReplace = new ReplaceSelectItem(
                Items: Sequence.Of(new ReplaceSelectElement(
                    Expr: TestUtils.Call("lower", new Expr[] { new Expr.Identifier(new Ident("city")) }),
                    ColumnName: new Ident("city"),
                    AsKeyword: true))),
        });
        Assert.Equal(expected, select.Projection[0]);

        select =
            dialects.VerifiedOnlySelect("SELECT * REPLACE ('widget' AS item_name) FROM orders");
        expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptReplace = new ReplaceSelectItem(
                Items: Sequence.Of(new ReplaceSelectElement(
                    Expr: new Expr.Value(
                        new Value.SingleQuotedString("widget").WithEmptySpan()),
                    ColumnName: new Ident("item_name"),
                    AsKeyword: true))),
        });
        Assert.Equal(expected, select.Projection[0]);

        select = dialects.VerifiedOnlySelect(
            "SELECT * REPLACE (quantity / 2 AS quantity, 3 AS order_id) FROM orders");
        expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptReplace = new ReplaceSelectItem(
                Items: Sequence.Of(
                    new ReplaceSelectElement(
                        Expr: new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("quantity")),
                            Op: new BinaryOperator.Divide(),
                            Right: Expr.MakeValue(TestUtils.Number("2"))),
                        ColumnName: new Ident("quantity"),
                        AsKeyword: true),
                    new ReplaceSelectElement(
                        Expr: Expr.MakeValue(TestUtils.Number("3")),
                        ColumnName: new Ident("order_id"),
                        AsKeyword: true))),
        });
        Assert.Equal(expected, select.Projection[0]);
    }

    [Fact]
    public void parse_sized_list()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new PostgreSqlDialect(),
            new DuckDbDialect(),
        });
        var sql = "CREATE TABLE embeddings (data FLOAT[1536])";
        dialects.VerifiedStmt(sql);
        sql = "CREATE TABLE embeddings (data FLOAT[1536][3])";
        dialects.VerifiedStmt(sql);
        sql = "SELECT data::FLOAT[1536] FROM embeddings";
        dialects.VerifiedStmt(sql);
    }

    [Fact]
    public void insert_into_with_parentheses()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new SnowflakeDialect(),
            new RedshiftSqlDialect(),
            new GenericDialect(),
        });
        dialects.VerifiedStmt("INSERT INTO t1 (id, name) (SELECT t2.id, t2.name FROM t2)");
        dialects.VerifiedStmt("INSERT INTO t1 (SELECT t2.id, t2.name FROM t2)");
        dialects.VerifiedStmt("INSERT INTO t1 (\"select\", name) (SELECT t2.name FROM t2)");
    }

    [Fact]
    public void test_insert_with_query_table()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsInsertTableQuery);

        // a simple query (block); i.e. SELECT ...
        var sql = "INSERT INTO (SELECT employee_id, last_name FROM employees) VALUES (207, 'Gregory')";
        dialects.VerifiedStmt(sql);

        // a full blown query; i.e. `WITH ... SELECT .. ORDER BY ...`
        sql = "INSERT INTO " +
              "(WITH cte AS (SELECT 1 AS id, 2 AS val FROM dual) SELECT foo_t.id, foo_t.val FROM foo_t " +
              "WHERE EXISTS (SELECT 1 FROM cte WHERE cte.id = foo_t.id) ORDER BY 1, 2) " +
              "(id, val) " +
              "VALUES (1000, 10101)";
        dialects.VerifiedStmt(sql);

        // an alias to the insert target query table
        sql = "INSERT INTO " +
              "(WITH cte AS (SELECT 1 AS id, 2 AS val FROM dual) SELECT foo_t.id, foo_t.val FROM foo_t " +
              "WHERE EXISTS (SELECT 1 FROM cte WHERE cte.id = foo_t.id)) abc " +
              "(id, val) " +
              "VALUES (1000, 10101)";
        dialects.VerifiedStmt(sql);

        // a query table target and a query source
        sql = "INSERT INTO (SELECT foo_t.id, foo_t.val FROM foo_t) SELECT 10, 20 FROM dual";
        dialects.VerifiedStmt(sql);

        // a query table target and a query source, with explicit columns
        sql =
            "INSERT INTO (SELECT foo_t.id, foo_t.val FROM foo_t) (id, val) SELECT 10, 20 FROM dual";
        dialects.VerifiedStmt(sql);
    }

    [Fact]
    public void parse_odbc_scalar_function()
    {
        var select = VerifiedOnlySelect("SELECT {fn my_func(1, 2)}");
        if (TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)) is not Expr.Function { Value: var function })
        {
            Assert.Fail("expected function");
            return;
        }
        Assert.Equal(function.Name, ObjectName.From(Sequence.Of(new Ident("my_func"))));
        Assert.True(function.UsesOdbcSyntax);
        // `matches!(..)` sem `assert!` no Rust: o resultado é descartado (não verifica nada).
        _ = function.Args is FunctionArguments.List { Value: var l } && l.Args.Count == 2;

        VerifiedStmt("SELECT {fn fna()} AS foo, fnb(1)");

        // Testing invalid SQL with any-one dialect is intentional.
        // Depending on dialect flags the error message may be different.
        var pg = new TestedDialects(new List<Dialect> { new PostgreSqlDialect() });
        var ex = Assert.Throws<ParserException>(() => pg.ParseSqlStatements("SELECT {fn2 my_func()}"));
        Assert.Equal(
            "sql parser error: Expected: an expression, found: {",
            ex.Error.ToString());
    }

    [Fact]
    public void test_dictionary_syntax()
    {
        static void Check(string sql, Expr expect)
        {
            Assert.Equal(
                TestUtils.AllDialectsWhere(d => d.SupportsDictionarySyntax).VerifiedExpr(sql),
                expect);
        }

        Check("{}", new Expr.Dictionary(Sequence<DictionaryField>.Empty));

        Check(
            "{'Alberta': 'Edmonton', 'Manitoba': 'Winnipeg'}",
            new Expr.Dictionary(Sequence.Of(
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "Alberta"),
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("Edmonton").WithEmptySpan())),
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "Manitoba"),
                    Value: new Expr.Value(
                        new Value.SingleQuotedString("Winnipeg").WithEmptySpan())))));

        Check(
            "{'start': CAST('2023-04-01' AS TIMESTAMP), 'end': CAST('2023-04-05' AS TIMESTAMP)}",
            new Expr.Dictionary(Sequence.Of(
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "start"),
                    Value: new Expr.Cast(
                        Kind: CastKind.Cast,
                        Expr: new Expr.Value(
                            new Value.SingleQuotedString("2023-04-01").WithEmptySpan()),
                        DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                        Format: null)),
                new DictionaryField(
                    Key: Ident.WithQuote('\'', "end"),
                    Value: new Expr.Cast(
                        Kind: CastKind.Cast,
                        Expr: new Expr.Value(
                            new Value.SingleQuotedString("2023-04-05").WithEmptySpan()),
                        DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                        Format: null)))));
    }

    [Fact]
    public void test_map_syntax()
    {
        static void Check(string sql, Expr expect)
        {
            Assert.Equal(
                TestUtils.AllDialectsWhere(d => d.SupportMapLiteralSyntax).VerifiedExpr(sql),
                expect);
        }

        Check(
            "MAP {'Alberta': 'Edmonton', 'Manitoba': 'Winnipeg'}",
            new Expr.Map(new Map(
                Entries: Sequence.Of(
                    new MapEntry(
                        Key: new Expr.Value(
                            new Value.SingleQuotedString("Alberta").WithEmptySpan()),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("Edmonton").WithEmptySpan())),
                    new MapEntry(
                        Key: new Expr.Value(
                            new Value.SingleQuotedString("Manitoba").WithEmptySpan()),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("Winnipeg").WithEmptySpan()))))));

        static Expr NumberExpr(string s) => Expr.MakeValue(TestUtils.Number(s));

        static Expr NullExpr() => new Expr.Value(new Value.Null().WithEmptySpan());

        Check(
            "MAP {1: 10.0, 2: 20.0}",
            new Expr.Map(new Map(
                Entries: Sequence.Of(
                    new MapEntry(
                        Key: NumberExpr("1"),
                        Value: NumberExpr("10.0")),
                    new MapEntry(
                        Key: NumberExpr("2"),
                        Value: NumberExpr("20.0"))))));

        Check(
            "MAP {[1, 2, 3]: 10.0, [4, 5, 6]: 20.0}",
            new Expr.Map(new Map(
                Entries: Sequence.Of(
                    new MapEntry(
                        Key: new Expr.Array(new Ast.Array(
                            Elem: Sequence.Of(NumberExpr("1"), NumberExpr("2"), NumberExpr("3")),
                            Named: false)),
                        Value: Expr.MakeValue(TestUtils.Number("10.0"))),
                    new MapEntry(
                        Key: new Expr.Array(new Ast.Array(
                            Elem: Sequence.Of(NumberExpr("4"), NumberExpr("5"), NumberExpr("6")),
                            Named: false)),
                        Value: Expr.MakeValue(TestUtils.Number("20.0")))))));

        Check(
            "MAP {'a': 10, 'b': 20}['a']",
            new Expr.CompoundFieldAccess(
                Root: new Expr.Map(new Map(
                    Entries: Sequence.Of(
                        new MapEntry(
                            Key: new Expr.Value(
                                new Value.SingleQuotedString("a").WithEmptySpan()),
                            Value: NumberExpr("10")),
                        new MapEntry(
                            Key: new Expr.Value(
                                new Value.SingleQuotedString("b").WithEmptySpan()),
                            Value: NumberExpr("20"))))),
                AccessChain: Sequence.Of<AccessExpr>(new AccessExpr.Subscript(new Subscript.Index(
                    IndexValue: new Expr.Value(new Value.SingleQuotedString("a").WithEmptySpan()))))));

        Check("MAP {}", new Expr.Map(new Map(Entries: Sequence<MapEntry>.Empty)));

        Check(
            "MAP {'a': 1, 'b': NULL}",
            new Expr.Map(new Map(
                Entries: Sequence.Of(
                    new MapEntry(
                        Key: new Expr.Value(
                            new Value.SingleQuotedString("a").WithEmptySpan()),
                        Value: NumberExpr("1")),
                    new MapEntry(
                        Key: new Expr.Value(
                            new Value.SingleQuotedString("b").WithEmptySpan()),
                        Value: NullExpr())))));

        Check(
            "MAP {1: [1, NULL, 3], 2: [4, NULL, 6], 3: [7, 8, 9]}",
            new Expr.Map(new Map(
                Entries: Sequence.Of(
                    new MapEntry(
                        Key: NumberExpr("1"),
                        Value: new Expr.Array(new Ast.Array(
                            Elem: Sequence.Of(NumberExpr("1"), NullExpr(), NumberExpr("3")),
                            Named: false))),
                    new MapEntry(
                        Key: NumberExpr("2"),
                        Value: new Expr.Array(new Ast.Array(
                            Elem: Sequence.Of(NumberExpr("4"), NullExpr(), NumberExpr("6")),
                            Named: false))),
                    new MapEntry(
                        Key: NumberExpr("3"),
                        Value: new Expr.Array(new Ast.Array(
                            Elem: Sequence.Of(NumberExpr("7"), NumberExpr("8"), NumberExpr("9")),
                            Named: false)))))));
    }

    [Fact]
    public void parse_within_group()
    {
        VerifiedExpr("PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY sales_amount)");
        VerifiedExpr(
            "PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY sales_amount) " +
            "OVER (PARTITION BY department)");
    }

    [Fact]
    public void tests_select_values_without_parens()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new SnowflakeDialect(),
            new DatabricksDialect(),
        });
        var sql = "SELECT * FROM VALUES (1, 2), (2,3) AS tbl (id, val)";
        var canonical = "SELECT * FROM (VALUES (1, 2), (2, 3)) AS tbl (id, val)";
        dialects.VerifiedOnlySelectWithCanonical(sql, canonical);
    }

    [Fact]
    public void tests_select_values_without_parens_and_set_op()
    {
        var dialects = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new SnowflakeDialect(),
            new DatabricksDialect(),
        });

        var sql = "SELECT id + 1, name FROM VALUES (1, 'Apple'), (2, 'Banana'), (3, 'Orange') AS fruits (id, name) UNION ALL SELECT 5, 'Strawberry'";
        var canonical = "SELECT id + 1, name FROM (VALUES (1, 'Apple'), (2, 'Banana'), (3, 'Orange')) AS fruits (id, name) UNION ALL SELECT 5, 'Strawberry'";
        var query = dialects.VerifiedQueryWithCanonical(sql, canonical);
        switch (query.Body)
        {
            case SetExpr.SetOperation setOperation:
                Assert.Equal<SetOperator>(new SetOperator.Union(), setOperation.Op);
                switch (setOperation.Left)
                {
                    case SetExpr.Select:
                        break;
                    default:
                        Assert.Fail("Expected: a SELECT statement");
                        break;
                }
                switch (setOperation.Right)
                {
                    case SetExpr.Select:
                        break;
                    default:
                        Assert.Fail("Expected: a SELECT statement");
                        break;
                }
                break;
            default:
                Assert.Fail("Expected: a SET OPERATION");
                break;
        }
    }

    [Fact]
    public void parse_select_wildcard_with_except()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsSelectWildcardExcept);

        var select = dialects.VerifiedOnlySelect("SELECT * EXCEPT (col_a) FROM data");
        SelectItem expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptExcept = new ExceptSelectItem(
                FirstElement: new Ident("col_a"),
                AdditionalElements: Sequence<Ident>.Empty),
        });
        Assert.Equal(expected, select.Projection[0]);

        select = dialects
            .VerifiedOnlySelect("SELECT * EXCEPT (department_id, employee_id) FROM employee_table");
        expected = new SelectItem.Wildcard(WildcardAdditionalOptions.Default() with
        {
            OptExcept = new ExceptSelectItem(
                FirstElement: new Ident("department_id"),
                AdditionalElements: Sequence.Of(new Ident("employee_id"))),
        });
        Assert.Equal(expected, select.Projection[0]);

        var ex = Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements("SELECT * EXCEPT () FROM employee_table"));
        Assert.Equal(
            "sql parser error: Expected: identifier, found: )",
            ex.Error.ToString());
    }

    [Fact]
    public void test_group_by_nothing()
    {
        var groupBy = TestUtils.AllDialectsWhere(d => d.SupportsGroupByExpr)
            .VerifiedOnlySelect("SELECT count(1) FROM t GROUP BY ()").GroupBy;
        {
            Assert.Equal<GroupByExpr>(
                new GroupByExpr.Expressions(Sequence.Of<Expr>(new Expr.Tuple(Sequence<Expr>.Empty)), Sequence<GroupByWithModifier>.Empty),
                groupBy);
        }

        groupBy = TestUtils.AllDialectsWhere(d => d.SupportsGroupByExpr)
            .VerifiedOnlySelect("SELECT name, count(1) FROM t GROUP BY name, ()").GroupBy;
        {
            Assert.Equal<GroupByExpr>(
                new GroupByExpr.Expressions(
                    Sequence.Of<Expr>(
                        new Expr.Identifier(new Ident("name")),
                        new Expr.Tuple(Sequence<Expr>.Empty)),
                    Sequence<GroupByWithModifier>.Empty),
                groupBy);
        }
    }

    [Fact]
    public void test_extract_seconds_ok()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.AllowExtractCustom);
        var stmt = dialects.VerifiedExpr("EXTRACT(SECONDS FROM '2 seconds'::INTERVAL)");

        Assert.Equal<Expr>(
            stmt,
            new Expr.Extract(
                Field: new DateTimeField.Seconds(),
                Syntax: ExtractSyntax.From,
                Expr: new Expr.Cast(
                    Kind: CastKind.DoubleColon,
                    Expr: new Expr.Value(
                        new Value.SingleQuotedString("2 seconds").WithEmptySpan()),
                    DataType: new DataType.Interval(
                        Fields: null,
                        Precision: null),
                    Format: null)));

        var actualAst = dialects
            .ParseSqlStatements("SELECT EXTRACT(seconds FROM '2 seconds'::INTERVAL)");

        var expectedAst = Sequence.Of<Statement>(new Statement.Query(new Query(
            With: null,
            Body: new SetExpr.Select(new Select(
                SelectToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Distinct: null,
                SelectModifiers: null,
                Top: null,
                TopBeforeDistinct: false,
                Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Extract(
                    Field: new DateTimeField.Seconds(),
                    Syntax: ExtractSyntax.From,
                    Expr: new Expr.Cast(
                        Kind: CastKind.DoubleColon,
                        Expr: new Expr.Value(
                            new Value.SingleQuotedString("2 seconds").WithEmptySpan()),
                        DataType: new DataType.Interval(
                            Fields: null,
                            Precision: null),
                        Format: null)))),
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

        Assert.Equal(actualAst, expectedAst);
    }

    [Fact]
    public void test_extract_seconds_single_quote_ok()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.AllowExtractCustom);
        var stmt = dialects.VerifiedExpr("EXTRACT('seconds' FROM '2 seconds'::INTERVAL)");

        Assert.Equal<Expr>(
            stmt,
            new Expr.Extract(
                Field: new DateTimeField.Custom(new Ident("seconds", '\'', Span.Empty())),
                Syntax: ExtractSyntax.From,
                Expr: new Expr.Cast(
                    Kind: CastKind.DoubleColon,
                    Expr: new Expr.Value(
                        new Value.SingleQuotedString("2 seconds").WithEmptySpan()),
                    DataType: new DataType.Interval(
                        Fields: null,
                        Precision: null),
                    Format: null)));
    }

    [Fact]
    public void test_extract_seconds_single_quote_err()
    {
        var sql = "SELECT EXTRACT('seconds' FROM '2 seconds'::INTERVAL)";
        var dialects = TestUtils.AllDialectsExcept(d => d.AllowExtractSingleQuotes);
        var err = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql)).Error;
        Assert.Equal(
            "sql parser error: Expected: date/time field, found: 'seconds'",
            err.ToString());
    }

    [Fact]
    public void test_truncate_table_with_on_cluster()
    {
        var sql = "TRUNCATE TABLE t ON CLUSTER cluster_name";
        switch (TestUtils.AllDialects().VerifiedStmt(sql))
        {
            case Statement.Truncate { Value: var truncate }:
                Assert.Equal(truncate.OnCluster, new Ident("cluster_name"));
                break;
            default:
                Assert.Fail("Expected: TRUNCATE TABLE statement");
                break;
        }

        // Omit ON CLUSTER is allowed
        TestUtils.AllDialects().VerifiedStmt("TRUNCATE TABLE t");

        var ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("TRUNCATE TABLE t ON CLUSTER"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: identifier, found: EOF"),
            ex.Error);
    }

    [Fact]
    public void parse_explain_with_option_list()
    {
        RunExplainAnalyze(
            TestUtils.AllDialectsWhere(d => d.SupportsExplainWithUtilityOptions),
            "EXPLAIN (ANALYZE false, VERBOSE true) SELECT sqrt(id) FROM foo",
            false,
            false,
            null,
            Sequence.Of(
                new UtilityOption(
                    Name: new Ident("ANALYZE"),
                    Arg: new Expr.Value(new Value.Boolean(false).WithEmptySpan())),
                new UtilityOption(
                    Name: new Ident("VERBOSE"),
                    Arg: new Expr.Value(new Value.Boolean(true).WithEmptySpan()))));

        RunExplainAnalyze(
            TestUtils.AllDialectsWhere(d => d.SupportsExplainWithUtilityOptions),
            "EXPLAIN (ANALYZE ON, VERBOSE OFF) SELECT sqrt(id) FROM foo",
            false,
            false,
            null,
            Sequence.Of(
                new UtilityOption(
                    Name: new Ident("ANALYZE"),
                    Arg: new Expr.Identifier(new Ident("ON"))),
                new UtilityOption(
                    Name: new Ident("VERBOSE"),
                    Arg: new Expr.Identifier(new Ident("OFF")))));

        RunExplainAnalyze(
            TestUtils.AllDialectsWhere(d => d.SupportsExplainWithUtilityOptions),
            "EXPLAIN (FORMAT1 TEXT, FORMAT2 'JSON', FORMAT3 \"XML\", FORMAT4 YAML) SELECT sqrt(id) FROM foo",
            false,
            false,
            null,
            Sequence.Of(
                new UtilityOption(
                    Name: new Ident("FORMAT1"),
                    Arg: new Expr.Identifier(new Ident("TEXT"))),
                new UtilityOption(
                    Name: new Ident("FORMAT2"),
                    Arg: new Expr.Value(
                        new Value.SingleQuotedString("JSON").WithEmptySpan())),
                new UtilityOption(
                    Name: new Ident("FORMAT3"),
                    Arg: new Expr.Identifier(Ident.WithQuote('"', "XML"))),
                new UtilityOption(
                    Name: new Ident("FORMAT4"),
                    Arg: new Expr.Identifier(new Ident("YAML")))));

        RunExplainAnalyze(
            TestUtils.AllDialectsWhere(d => d.SupportsExplainWithUtilityOptions),
            "EXPLAIN (NUM1 10, NUM2 +10.1, NUM3 -10.2) SELECT sqrt(id) FROM foo",
            false,
            false,
            null,
            Sequence.Of(
                new UtilityOption(
                    Name: new Ident("NUM1"),
                    Arg: new Expr.Value(
                        new Value.Number("10", false).WithEmptySpan())),
                new UtilityOption(
                    Name: new Ident("NUM2"),
                    Arg: new Expr.UnaryOp(
                        Op: new UnaryOperator.Plus(),
                        Expr: new Expr.Value(
                            new Value.Number("10.1", false).WithEmptySpan()))),
                new UtilityOption(
                    Name: new Ident("NUM3"),
                    Arg: new Expr.UnaryOp(
                        Op: new UnaryOperator.Minus(),
                        Expr: new Expr.Value(
                            new Value.Number("10.2", false).WithEmptySpan())))));

        var utilityOptions = Sequence.Of(
            new UtilityOption(
                Name: new Ident("ANALYZE"),
                Arg: null),
            new UtilityOption(
                Name: new Ident("VERBOSE"),
                Arg: new Expr.Value(new Value.Boolean(true).WithEmptySpan())),
            new UtilityOption(
                Name: new Ident("WAL"),
                Arg: new Expr.Identifier(new Ident("OFF"))),
            new UtilityOption(
                Name: new Ident("FORMAT"),
                Arg: new Expr.Identifier(new Ident("YAML"))),
            new UtilityOption(
                Name: new Ident("USER_DEF_NUM"),
                Arg: new Expr.UnaryOp(
                    Op: new UnaryOperator.Minus(),
                    Expr: new Expr.Value(
                        new Value.Number("100.1", false).WithEmptySpan()))));
        RunExplainAnalyze(
            TestUtils.AllDialectsWhere(d => d.SupportsExplainWithUtilityOptions),
            "EXPLAIN (ANALYZE, VERBOSE true, WAL OFF, FORMAT YAML, USER_DEF_NUM -100.1) SELECT sqrt(id) FROM foo",
            false,
            false,
            null,
            utilityOptions);
    }

    [Fact]
    public void test_create_policy()
    {
        var sql = "CREATE POLICY my_policy ON my_table " +
                  "AS PERMISSIVE FOR SELECT " +
                  "TO my_role, CURRENT_USER " +
                  "USING (c0 = 1) " +
                  "WITH CHECK (1 = 1)";

        {
            var createPolicy = Assert.IsType<Statement.CreatePolicy>(TestUtils.AllDialects().VerifiedStmt(sql)).Value;
            Assert.Equal("my_policy", createPolicy.Name.ToString());
            Assert.Equal("my_table", createPolicy.TableName.ToString());
            Assert.Equal<Sequence<Owner>?>(
                createPolicy.To,
                Sequence.Of<Owner>(
                    new Owner.Ident(new Ident("my_role")),
                    new Owner.CurrentUser()));
            Assert.Equal<Expr?>(
                createPolicy.Using,
                new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("c0")),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.Value(
                        new Value.Number("1", false).WithEmptySpan())));
            Assert.Equal<Expr?>(
                createPolicy.WithCheck,
                new Expr.BinaryOp(
                    Left: new Expr.Value(
                        new Value.Number("1", false).WithEmptySpan()),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.Value(
                        new Value.Number("1", false).WithEmptySpan())));
        }

        // USING with SELECT query
        TestUtils.AllDialects().VerifiedStmt(
            "CREATE POLICY my_policy ON my_table " +
            "AS PERMISSIVE FOR SELECT " +
            "TO my_role, CURRENT_USER " +
            "USING (c0 IN (SELECT column FROM t0)) " +
            "WITH CHECK (1 = 1)");
        // omit AS / FOR / TO / USING / WITH CHECK clauses is allowed
        TestUtils.AllDialects().VerifiedStmt("CREATE POLICY my_policy ON my_table");

        // missing table name
        var ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("CREATE POLICY my_policy"));
        Assert.Equal(
            "sql parser error: Expected: ON, found: EOF",
            ex.Error.ToString());
        // missing policy type
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("CREATE POLICY my_policy ON my_table AS"));
        Assert.Equal(
            "sql parser error: Expected: one of PERMISSIVE or RESTRICTIVE, found: EOF",
            ex.Error.ToString());
        // missing FOR command
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("CREATE POLICY my_policy ON my_table FOR"));
        Assert.Equal(
            "sql parser error: Expected: one of ALL or SELECT or INSERT or UPDATE or DELETE, found: EOF",
            ex.Error.ToString());
        // missing TO owners
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("CREATE POLICY my_policy ON my_table TO"));
        Assert.Equal(
            "sql parser error: Expected: CURRENT_USER, CURRENT_ROLE, SESSION_USER or identifier after OWNER TO. sql parser error: Expected: identifier, found: EOF",
            ex.Error.ToString());
        // missing USING expression
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("CREATE POLICY my_policy ON my_table USING"));
        Assert.Equal(
            "sql parser error: Expected: (, found: EOF",
            ex.Error.ToString());
        // missing WITH CHECK expression
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("CREATE POLICY my_policy ON my_table WITH CHECK"));
        Assert.Equal(
            "sql parser error: Expected: (, found: EOF",
            ex.Error.ToString());
    }

    [Fact]
    public void test_drop_policy()
    {
        var sql = "DROP POLICY IF EXISTS my_policy ON my_table RESTRICT";
        {
            var dropPolicy = Assert.IsType<Statement.DropPolicy>(TestUtils.AllDialects().VerifiedStmt(sql)).Value;
            Assert.True(dropPolicy.IfExists);
            Assert.Equal("my_policy", dropPolicy.Name.ToString());
            Assert.Equal("my_table", dropPolicy.TableName.ToString());
            Assert.Equal<DropBehavior?>(dropPolicy.DropBehavior, new DropBehavior.Restrict());
        }

        // omit IF EXISTS is allowed
        TestUtils.AllDialects().VerifiedStmt("DROP POLICY my_policy ON my_table CASCADE");
        // omit option is allowed
        TestUtils.AllDialects().VerifiedStmt("DROP POLICY my_policy ON my_table");

        // missing table name
        var ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("DROP POLICY my_policy"));
        Assert.Equal(
            "sql parser error: Expected: ON, found: EOF",
            ex.Error.ToString());
        // Wrong option name
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("DROP POLICY my_policy ON my_table WRONG"));
        Assert.Equal(
            "sql parser error: Expected: end of statement, found: WRONG",
            ex.Error.ToString());
    }

    [Fact]
    public void test_alter_policy()
    {
        {
            var alterPolicy = Assert.IsType<Statement.AlterPolicy>(
                VerifiedStmt("ALTER POLICY old_policy ON my_table RENAME TO new_policy")).Value;
            Assert.Equal("old_policy", alterPolicy.Name.ToString());
            Assert.Equal("my_table", alterPolicy.TableName.ToString());
            Assert.Equal<AlterPolicyOperation>(
                alterPolicy.Operation,
                new AlterPolicyOperation.Rename(
                    NewName: new Ident("new_policy")));
        }

        {
            var alterPolicy = Assert.IsType<Statement.AlterPolicy>(VerifiedStmt(
                "ALTER POLICY my_policy ON my_table TO CURRENT_USER " +
                "USING ((SELECT c0)) WITH CHECK (c0 > 0)")).Value;
            Assert.Equal("my_policy", alterPolicy.Name.ToString());
            Assert.Equal("my_table", alterPolicy.TableName.ToString());
        }

        // omit TO / USING / WITH CHECK clauses is allowed
        VerifiedStmt("ALTER POLICY my_policy ON my_table");

        // mixing RENAME and APPLY expressions
        var ex = Assert.Throws<ParserException>(() =>
            ParseSqlStatements("ALTER POLICY old_policy ON my_table TO public RENAME TO new_policy"));
        Assert.Equal(
            "sql parser error: Expected: end of statement, found: RENAME",
            ex.Error.ToString());
        ex = Assert.Throws<ParserException>(() =>
            ParseSqlStatements("ALTER POLICY old_policy ON my_table RENAME TO new_policy TO public"));
        Assert.Equal(
            "sql parser error: Expected: end of statement, found: TO",
            ex.Error.ToString());
        // missing TO in RENAME TO
        ex = Assert.Throws<ParserException>(() =>
            ParseSqlStatements("ALTER POLICY old_policy ON my_table RENAME"));
        Assert.Equal(
            "sql parser error: Expected: TO, found: EOF",
            ex.Error.ToString());
        // missing new name in RENAME TO
        ex = Assert.Throws<ParserException>(() =>
            ParseSqlStatements("ALTER POLICY old_policy ON my_table RENAME TO"));
        Assert.Equal(
            "sql parser error: Expected: identifier, found: EOF",
            ex.Error.ToString());

        // missing the expression in USING
        ex = Assert.Throws<ParserException>(() =>
            ParseSqlStatements("ALTER POLICY my_policy ON my_table USING"));
        Assert.Equal(
            "sql parser error: Expected: (, found: EOF",
            ex.Error.ToString());
        // missing the expression in WITH CHECK
        ex = Assert.Throws<ParserException>(() =>
            ParseSqlStatements("ALTER POLICY my_policy ON my_table WITH CHECK"));
        Assert.Equal(
            "sql parser error: Expected: (, found: EOF",
            ex.Error.ToString());
    }

    [Fact]
    public void test_create_connector()
    {
        var sql = "CREATE CONNECTOR my_connector " +
                  "TYPE 'jdbc' " +
                  "URL 'jdbc:mysql://localhost:3306/mydb' " +
                  "WITH DCPROPERTIES('user' = 'root', 'password' = 'password')";
        var dialects = TestUtils.AllDialects();
        {
            var createConnector = Assert.IsType<Statement.CreateConnector>(dialects.VerifiedStmt(sql)).Value;
            Assert.Equal("my_connector", createConnector.Name.ToString());
            Assert.Equal("jdbc", createConnector.ConnectorType);
            Assert.Equal("jdbc:mysql://localhost:3306/mydb", createConnector.Url);
            Assert.Equal<Sequence<SqlOption>?>(
                createConnector.WithDcproperties,
                Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "user"),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("root").WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "password"),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("password").WithEmptySpan()))));
        }

        // omit IF NOT EXISTS/TYPE/URL/COMMENT/WITH DCPROPERTIES clauses is allowed
        dialects.VerifiedStmt("CREATE CONNECTOR my_connector");

        // missing connector name
        var ex = Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements("CREATE CONNECTOR"));
        Assert.Equal(
            "sql parser error: Expected: identifier, found: EOF",
            ex.Error.ToString());
    }

    [Fact]
    public void test_drop_connector()
    {
        var dialects = TestUtils.AllDialects();
        {
            var dropConnector = Assert.IsType<Statement.DropConnector>(dialects.VerifiedStmt("DROP CONNECTOR IF EXISTS my_connector"));
            Assert.True(dropConnector.IfExists);
            Assert.Equal("my_connector", dropConnector.Name.ToString());
        }

        // omit IF EXISTS is allowed
        dialects.VerifiedStmt("DROP CONNECTOR my_connector");

        // missing connector name
        var ex = Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements("DROP CONNECTOR"));
        Assert.Equal(
            "sql parser error: Expected: identifier, found: EOF",
            ex.Error.ToString());
    }

    [Fact]
    public void test_alter_connector()
    {
        var dialects = TestUtils.AllDialects();
        {
            var alterConnector = Assert.IsType<Statement.AlterConnector>(dialects.VerifiedStmt(
                "ALTER CONNECTOR my_connector SET DCPROPERTIES('user' = 'root', 'password' = 'password')"));
            Assert.Equal("my_connector", alterConnector.Name.ToString());
            Assert.Equal<Sequence<SqlOption>?>(
                alterConnector.Properties,
                Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "user"),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("root").WithEmptySpan())),
                    new SqlOption.KeyValue(
                        Key: Ident.WithQuote('\'', "password"),
                        Value: new Expr.Value(
                            new Value.SingleQuotedString("password").WithEmptySpan()))));
            Assert.Null(alterConnector.Url);
            Assert.Null(alterConnector.Owner);
        }

        {
            var alterConnector = Assert.IsType<Statement.AlterConnector>(dialects
                .VerifiedStmt("ALTER CONNECTOR my_connector SET URL 'jdbc:mysql://localhost:3306/mydb'"));
            Assert.Equal("my_connector", alterConnector.Name.ToString());
            Assert.Null(alterConnector.Properties);
            Assert.Equal("jdbc:mysql://localhost:3306/mydb", alterConnector.Url);
            Assert.Null(alterConnector.Owner);
        }

        {
            var alterConnector = Assert.IsType<Statement.AlterConnector>(dialects.VerifiedStmt("ALTER CONNECTOR my_connector SET OWNER USER 'root'"));
            Assert.Equal("my_connector", alterConnector.Name.ToString());
            Assert.Null(alterConnector.Properties);
            Assert.Null(alterConnector.Url);
            Assert.Equal<AlterConnectorOwner?>(
                alterConnector.Owner,
                new AlterConnectorOwner.User(Ident.WithQuote('\'', "root")));
        }

        {
            var alterConnector = Assert.IsType<Statement.AlterConnector>(dialects.VerifiedStmt("ALTER CONNECTOR my_connector SET OWNER ROLE 'admin'"));
            Assert.Equal("my_connector", alterConnector.Name.ToString());
            Assert.Null(alterConnector.Properties);
            Assert.Null(alterConnector.Url);
            Assert.Equal<AlterConnectorOwner?>(
                alterConnector.Owner,
                new AlterConnectorOwner.Role(Ident.WithQuote('\'', "admin")));
        }

        // Wrong option name
        var ex = Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements(
                "ALTER CONNECTOR my_connector SET WRONG 'jdbc:mysql://localhost:3306/mydb'"));
        Assert.Equal(
            "sql parser error: Expected: end of statement, found: WRONG",
            ex.Error.ToString());
    }

    [Fact]
    public void test_select_where_with_like_or_ilike_any()
    {
        VerifiedStmt("SELECT * FROM x WHERE a ILIKE ANY '%abc%'");
        VerifiedStmt("SELECT * FROM x WHERE a LIKE ANY '%abc%'");
        VerifiedStmt("SELECT * FROM x WHERE a ILIKE ANY ('%Jo%oe%', 'T%e')");
        VerifiedStmt("SELECT * FROM x WHERE a LIKE ANY ('%Jo%oe%', 'T%e')");
    }

    [Fact]
    public void test_any_some_all_comparison()
    {
        VerifiedStmt("SELECT c1 FROM tbl WHERE c1 = ANY(SELECT c2 FROM tbl)");
        VerifiedStmt("SELECT c1 FROM tbl WHERE c1 >= ALL(SELECT c2 FROM tbl)");
        VerifiedStmt("SELECT c1 FROM tbl WHERE c1 <> SOME(SELECT c2 FROM tbl)");
        VerifiedStmt("SELECT 1 = ANY(WITH x AS (SELECT 1) SELECT * FROM x)");
    }

    [Fact]
    public void test_alias_equal_expr()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsEqAliasAssignment);
        var sql = "SELECT some_alias = some_column FROM some_table";
        var expected = "SELECT some_column AS some_alias FROM some_table";
        _ = dialects.OneStatementParsesTo(sql, expected);

        sql = "SELECT some_alias = (a*b) FROM some_table";
        expected = "SELECT (a * b) AS some_alias FROM some_table";
        _ = dialects.OneStatementParsesTo(sql, expected);

        sql = """SELECT some_alias = "from" FROM some_table""";
        expected = """SELECT "from" AS some_alias FROM some_table""";
        _ = dialects.OneStatementParsesTo(sql, expected);

        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => dialects
                .ParseSqlStatements("SELECT a = FROM")).Error,
            new ParserError.ParserErrorValue("Expected an expression, found: FROM"));

        Assert.Equal<ParserError>(
            Assert.Throws<ParserException>(() => dialects
                .ParseSqlStatements("SELECT a = FROM some_table")).Error,
            new ParserError.ParserErrorValue("Expected an expression, found: FROM"));

        dialects = TestUtils.AllDialectsWhere(d => !d.SupportsEqAliasAssignment);
        sql = "SELECT x = (a * b) FROM some_table";
        expected = "SELECT x = (a * b) FROM some_table";
        _ = dialects.OneStatementParsesTo(sql, expected);
    }

    [Fact]
    public void test_try_convert()
    {
        var dialects =
            TestUtils.AllDialectsWhere(d => d.SupportsTryConvert && d.ConvertTypeBeforeValue);
        dialects.VerifiedExpr("TRY_CONVERT(VARCHAR(MAX), 'foo')");

        dialects =
            TestUtils.AllDialectsWhere(d => d.SupportsTryConvert && !d.ConvertTypeBeforeValue);
        dialects.VerifiedExpr("TRY_CONVERT('foo', VARCHAR(MAX))");
    }

    [Fact]
    public void parse_method_select()
    {
        _ = VerifiedOnlySelect(
            "SELECT LEFT('abc', 1).value('.', 'NVARCHAR(MAX)').value('.', 'NVARCHAR(MAX)') AS T");
        _ = VerifiedOnlySelect("SELECT STUFF((SELECT ',' + name FROM sys.objects FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'), 1, 1, '') AS T");
        _ = VerifiedOnlySelect("SELECT CAST(column AS XML).value('.', 'NVARCHAR(MAX)') AS T");

        // `CONVERT` support
        var dialects =
            TestUtils.AllDialectsWhere(d => d.SupportsTryConvert && d.ConvertTypeBeforeValue);
        _ = dialects.VerifiedOnlySelect("SELECT CONVERT(XML, '<Book>abc</Book>').value('.', 'NVARCHAR(MAX)').value('.', 'NVARCHAR(MAX)') AS T");
    }

    [Fact]
    public void parse_method_expr()
    {
        // `matches!(access_chain[..], [P1, P2])` → `Count == 2` + teste de cada elemento (sem padrões
        // de lista, que exigem polyfills ausentes no net48).
        var expr =
            VerifiedExpr("LEFT('abc', 1).value('.', 'NVARCHAR(MAX)').value('.', 'NVARCHAR(MAX)')");
        {
            var compoundFieldAccess = Assert.IsType<Expr.CompoundFieldAccess>(expr);
            var accessChain = compoundFieldAccess.AccessChain;
            Assert.True(compoundFieldAccess.Root is Expr.Function);
            Assert.True(
                accessChain.Count == 2
                && accessChain[0] is AccessExpr.Dot { Value: Expr.Function }
                && accessChain[1] is AccessExpr.Dot { Value: Expr.Function });
        }

        expr = VerifiedExpr(
            "(SELECT ',' + name FROM sys.objects FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)')");
        {
            var compoundFieldAccess = Assert.IsType<Expr.CompoundFieldAccess>(expr);
            var accessChain = compoundFieldAccess.AccessChain;
            Assert.True(compoundFieldAccess.Root is Expr.Subquery);
            Assert.True(
                accessChain.Count == 1
                && accessChain[0] is AccessExpr.Dot { Value: Expr.Function });
        }
        expr = VerifiedExpr("CAST(column AS XML).value('.', 'NVARCHAR(MAX)')");
        {
            var compoundFieldAccess = Assert.IsType<Expr.CompoundFieldAccess>(expr);
            var accessChain = compoundFieldAccess.AccessChain;
            Assert.True(compoundFieldAccess.Root is Expr.Cast);
            Assert.True(
                accessChain.Count == 1
                && accessChain[0] is AccessExpr.Dot { Value: Expr.Function });
        }

        // `CONVERT` support
        var dialects =
            TestUtils.AllDialectsWhere(d => d.SupportsTryConvert && d.ConvertTypeBeforeValue);
        expr = dialects.VerifiedExpr(
            "CONVERT(XML, '<Book>abc</Book>').value('.', 'NVARCHAR(MAX)').value('.', 'NVARCHAR(MAX)')");
        {
            var compoundFieldAccess = Assert.IsType<Expr.CompoundFieldAccess>(expr);
            var accessChain = compoundFieldAccess.AccessChain;
            Assert.True(compoundFieldAccess.Root is Expr.Convert);
            Assert.True(
                accessChain.Count == 2
                && accessChain[0] is AccessExpr.Dot { Value: Expr.Function }
                && accessChain[1] is AccessExpr.Dot { Value: Expr.Function });
        }
    }

    [Fact]
    public void test_show_dbs_schemas_tables_views()
    {
        // These statements are parsed the same by all dialects
        var stmts = new List<string>
        {
            "SHOW CATALOGS",
            "SHOW DATABASES",
            "SHOW SCHEMAS",
            "SHOW TABLES",
            "SHOW VIEWS",
            "SHOW TABLES IN db1",
            "SHOW VIEWS FROM db1",
            "SHOW MATERIALIZED VIEWS",
            "SHOW MATERIALIZED VIEWS IN db1",
            "SHOW MATERIALIZED VIEWS FROM db1",
        };
        foreach (var stmt in stmts)
        {
            VerifiedStmt(stmt);
        }

        // These statements are parsed the same by all dialects
        // except for how the parser interprets the location of
        // LIKE option (infix/suffix)
        stmts = new List<string>
        {
            "SHOW CATALOGS LIKE '%abc'",
            "SHOW DATABASES LIKE '%abc'",
            "SHOW SCHEMAS LIKE '%abc'",
        };
        foreach (var stmt in stmts)
        {
            TestUtils.AllDialectsWhere(d => d.SupportsShowLikeBeforeIn).VerifiedStmt(stmt);
            TestUtils.AllDialectsWhere(d => !d.SupportsShowLikeBeforeIn).VerifiedStmt(stmt);
        }

        // These statements are only parsed by dialects that
        // support the LIKE option in the suffix
        stmts = new List<string>
        {
            "SHOW TABLES IN db1 'abc'",
            "SHOW VIEWS IN db1 'abc'",
            "SHOW VIEWS FROM db1 'abc'",
            "SHOW MATERIALIZED VIEWS IN db1 'abc'",
            "SHOW MATERIALIZED VIEWS FROM db1 'abc'",
        };
        foreach (var stmt in stmts)
        {
            TestUtils.AllDialectsWhere(d => !d.SupportsShowLikeBeforeIn).VerifiedStmt(stmt);
        }
    }

    [Fact]
    public void parse_listen_channel()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsListenNotify);

        {
            var listen = Assert.IsType<Statement.LISTEN>(dialects.VerifiedStmt("LISTEN test1"));
            Assert.Equal(new Ident("test1"), listen.Channel);
        }

        var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("LISTEN *"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: identifier, found: *"));

        dialects = TestUtils.AllDialectsWhere(d => !d.SupportsListenNotify);

        ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("LISTEN test1"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: an SQL statement, found: LISTEN"));
    }

    [Fact]
    public void parse_unlisten_channel()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsListenNotify);

        {
            var unlisten = Assert.IsType<Statement.UNLISTEN>(dialects.VerifiedStmt("UNLISTEN test1"));
            Assert.Equal(new Ident("test1"), unlisten.Channel);
        }

        {
            var unlisten = Assert.IsType<Statement.UNLISTEN>(dialects.VerifiedStmt("UNLISTEN *"));
            Assert.Equal(new Ident("*"), unlisten.Channel);
        }

        var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("UNLISTEN +"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: wildcard or identifier, found: +"));

        dialects = TestUtils.AllDialectsWhere(d => !d.SupportsListenNotify);

        ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("UNLISTEN test1"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: an SQL statement, found: UNLISTEN"));
    }

    [Fact]
    public void parse_notify_channel()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsListenNotify);

        {
            var notify = Assert.IsType<Statement.NOTIFY>(dialects.VerifiedStmt("NOTIFY test1"));
            Assert.Equal(new Ident("test1"), notify.Channel);
            Assert.Null(notify.Payload);
        }

        switch (dialects.VerifiedStmt("NOTIFY test1, 'this is a test notification'"))
        {
            case Statement.NOTIFY { Payload: { } payload } notify:
                Assert.Equal(new Ident("test1"), notify.Channel);
                Assert.Equal("this is a test notification", payload);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }

        var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements("NOTIFY *"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: identifier, found: *"));
        ex = Assert.Throws<ParserException>(() => dialects
            .ParseSqlStatements("NOTIFY test1, *"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: literal string, found: *"));

        var sqlStatements = new[]
        {
            "NOTIFY test1",
            "NOTIFY test1, 'this is a test notification'",
        };
        dialects = TestUtils.AllDialectsWhere(d => !d.SupportsListenNotify);

        foreach (var sql in sqlStatements)
        {
            ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
            Assert.Equal(
                ex.Error,
                new ParserError.ParserErrorValue("Expected: an SQL statement, found: NOTIFY"));
        }
    }

    [Fact]
    public void parse_load_data()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsLoadData);
        var onlySupportsLoadExtensionDialects =
            TestUtils.AllDialectsWhere(d => !d.SupportsLoadData && d.SupportsLoadExtension);
        var notSupportsLoadDialects =
            TestUtils.AllDialectsWhere(d => !d.SupportsLoadData && !d.SupportsLoadExtension);

        var sql = "LOAD DATA INPATH '/local/path/to/data.txt' INTO TABLE test.my_table";
        {
            var loadData = Assert.IsType<Statement.LoadData>(dialects.VerifiedStmt(sql));
            Assert.False(loadData.Local);
            Assert.Equal("/local/path/to/data.txt", loadData.Inpath);
            Assert.False(loadData.Overwrite);
            Assert.Equal(
                ObjectName.From(Sequence.Of(new Ident("test"), new Ident("my_table"))),
                loadData.TableName);
            Assert.Null(loadData.Partitioned);
            Assert.Null(loadData.TableFormat);
        }

        // with OVERWRITE keyword
        sql = "LOAD DATA INPATH '/local/path/to/data.txt' OVERWRITE INTO TABLE my_table";
        {
            var loadData = Assert.IsType<Statement.LoadData>(dialects.VerifiedStmt(sql));
            Assert.False(loadData.Local);
            Assert.Equal("/local/path/to/data.txt", loadData.Inpath);
            Assert.True(loadData.Overwrite);
            Assert.Equal(ObjectName.From(Sequence.Of(new Ident("my_table"))), loadData.TableName);
            Assert.Null(loadData.Partitioned);
            Assert.Null(loadData.TableFormat);
        }

        var ex = Assert.Throws<ParserException>(() => onlySupportsLoadExtensionDialects
            .ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: end of statement, found: INPATH"));
        ex = Assert.Throws<ParserException>(() => notSupportsLoadDialects
            .ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: `DATA` or an extension name after `LOAD`, found: INPATH"));

        // with LOCAL keyword
        sql = "LOAD DATA LOCAL INPATH '/local/path/to/data.txt' INTO TABLE test.my_table";
        {
            var loadData = Assert.IsType<Statement.LoadData>(dialects.VerifiedStmt(sql));
            Assert.True(loadData.Local);
            Assert.Equal("/local/path/to/data.txt", loadData.Inpath);
            Assert.False(loadData.Overwrite);
            Assert.Equal(
                ObjectName.From(Sequence.Of(new Ident("test"), new Ident("my_table"))),
                loadData.TableName);
            Assert.Null(loadData.Partitioned);
            Assert.Null(loadData.TableFormat);
        }

        ex = Assert.Throws<ParserException>(() => onlySupportsLoadExtensionDialects
            .ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: end of statement, found: LOCAL"));
        ex = Assert.Throws<ParserException>(() => notSupportsLoadDialects
            .ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: `DATA` or an extension name after `LOAD`, found: LOCAL"));

        // with PARTITION  clause
        sql = "LOAD DATA LOCAL INPATH '/local/path/to/data.txt' INTO TABLE my_table PARTITION (year = 2024, month = 11)";
        {
            var loadData = Assert.IsType<Statement.LoadData>(dialects.VerifiedStmt(sql));
            Assert.True(loadData.Local);
            Assert.Equal("/local/path/to/data.txt", loadData.Inpath);
            Assert.False(loadData.Overwrite);
            Assert.Equal(ObjectName.From(Sequence.Of(new Ident("my_table"))), loadData.TableName);
            Assert.Equal<Sequence<Expr>?>(
                Sequence.Of<Expr>(
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("year")),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.Number("2024", false).WithEmptySpan())),
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("month")),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.Number("11", false).WithEmptySpan()))),
                loadData.Partitioned);
            Assert.Null(loadData.TableFormat);
        }

        // with PARTITION  clause
        sql = "LOAD DATA LOCAL INPATH '/local/path/to/data.txt' OVERWRITE INTO TABLE good.my_table PARTITION (year = 2024, month = 11) INPUTFORMAT 'org.apache.hadoop.mapred.TextInputFormat' SERDE 'org.apache.hadoop.hive.serde2.OpenCSVSerde'";
        {
            var loadData = Assert.IsType<Statement.LoadData>(dialects.VerifiedStmt(sql));
            Assert.True(loadData.Local);
            Assert.Equal("/local/path/to/data.txt", loadData.Inpath);
            Assert.True(loadData.Overwrite);
            Assert.Equal(
                ObjectName.From(Sequence.Of(new Ident("good"), new Ident("my_table"))),
                loadData.TableName);
            Assert.Equal<Sequence<Expr>?>(
                Sequence.Of<Expr>(
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("year")),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.Number("2024", false).WithEmptySpan())),
                    new Expr.BinaryOp(
                        Left: new Expr.Identifier(new Ident("month")),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.Number("11", false).WithEmptySpan()))),
                loadData.Partitioned);
            Assert.Equal(
                new HiveLoadDataFormat(
                    Serde: new Expr.Value(
                        new Value.SingleQuotedString(
                            "org.apache.hadoop.hive.serde2.OpenCSVSerde")
                        .WithEmptySpan()),
                    InputFormat: new Expr.Value(
                        new Value.SingleQuotedString(
                            "org.apache.hadoop.mapred.TextInputFormat")
                        .WithEmptySpan())),
                loadData.TableFormat);
        }

        // negative test case
        sql = "LOAD DATA2 LOCAL INPATH '/local/path/to/data.txt' INTO TABLE test.my_table";
        ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: `DATA` or an extension name after `LOAD`, found: DATA2"));
    }

    [Fact]
    public void test_load_extension()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsLoadExtension);
        var notSupportsLoadExtensionDialects = TestUtils.AllDialectsWhere(d => !d.SupportsLoadExtension);
        var sql = "LOAD my_extension";

        {
            var load = Assert.IsType<Statement.Load>(dialects.VerifiedStmt(sql));
            Assert.Equal(new Ident("my_extension"), load.ExtensionName);
        }

        var ex = Assert.Throws<ParserException>(() => notSupportsLoadExtensionDialects
            .ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: `DATA` or an extension name after `LOAD`, found: my_extension"));

        sql = "LOAD 'filename'";

        {
            var load = Assert.IsType<Statement.Load>(dialects.VerifiedStmt(sql));
            Assert.Equal(
                new Ident("filename", '\'', Span.Empty()),
                load.ExtensionName);
        }
    }

    [Fact]
    public void test_select_top()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsTopBeforeDistinct);
        dialects.VerifiedStmt("SELECT ALL * FROM tbl");
        dialects.VerifiedStmt("SELECT TOP 3 * FROM tbl");
        dialects.VerifiedStmt("SELECT TOP 3 ALL * FROM tbl");
        dialects.VerifiedStmt("SELECT TOP 3 DISTINCT * FROM tbl");
        dialects.VerifiedStmt("SELECT TOP 3 DISTINCT a, b, c FROM tbl");
    }

    [Fact]
    public void parse_bang_not()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsBangNotOperator);
        var sql = "SELECT !a, !(b > 3)";
        var projection = dialects.VerifiedOnlySelect(sql).Projection;

        var exprs = new Expr[]
        {
            new Expr.Identifier(new Ident("a")),
            new Expr.Nested(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("b")),
                Op: new BinaryOperator.Gt(),
                Right: new Expr.Value(
                    new Value.Number("3", false).WithEmptySpan()))),
        };
        for (var i = 0; i < exprs.Length; i++)
        {
            var expr = exprs[i];
            Assert.Equal<SelectItem>(
                new SelectItem.UnnamedExpr(new Expr.UnaryOp(
                    Op: new UnaryOperator.BangNot(),
                    Expr: expr)),
                projection[i]);
        }

        var sqlStatements = new[] { "SELECT a!", "SELECT a ! b", "SELECT a ! as b" };

        foreach (var s in sqlStatements)
        {
            var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(s));
            Assert.Equal(
                ex.Error,
                new ParserError.ParserErrorValue("No infix parser for token ExclamationMark"));
        }

        sqlStatements = new[] { "SELECT !a", "SELECT !a b", "SELECT !a as b" };
        dialects = TestUtils.AllDialectsWhere(d => !d.SupportsBangNotOperator);

        foreach (var s in sqlStatements)
        {
            var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(s));
            Assert.Equal(
                ex.Error,
                new ParserError.ParserErrorValue("Expected: an expression, found: !"));
        }
    }

    [Fact]
    public void parse_factorial_operator()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsFactorialOperator);
        var sql = "SELECT a!, (b + c)!";
        var projection = dialects.VerifiedOnlySelect(sql).Projection;

        var exprs = new Expr[]
        {
            new Expr.Identifier(new Ident("a")),
            new Expr.Nested(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("b")),
                Op: new BinaryOperator.Plus(),
                Right: new Expr.Identifier(new Ident("c")))),
        };
        for (var i = 0; i < exprs.Length; i++)
        {
            var expr = exprs[i];
            Assert.Equal<SelectItem>(
                new SelectItem.UnnamedExpr(new Expr.UnaryOp(
                    Op: new UnaryOperator.PGPostfixFactorial(),
                    Expr: expr)),
                projection[i]);
        }

        var sqlStatements = new[] { "SELECT !a", "SELECT !a b", "SELECT !a as b" };

        foreach (var s in sqlStatements)
        {
            var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(s));
            Assert.Equal(
                ex.Error,
                new ParserError.ParserErrorValue("Expected: an expression, found: !"));
        }

        sqlStatements = new[] { "SELECT a!", "SELECT a ! b", "SELECT a ! as b" };

        // Due to the exclamation mark, which is both part of the `bang not` operator
        // and the `factorial` operator,  additional filtering not supports
        // `bang not` operator is required here.
        dialects =
            TestUtils.AllDialectsWhere(d => !d.SupportsFactorialOperator && !d.SupportsBangNotOperator);

        foreach (var s in sqlStatements)
        {
            var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(s));
            Assert.Equal(
                ex.Error,
                new ParserError.ParserErrorValue("No infix parser for token ExclamationMark"));
        }

        // Due to the exclamation mark, which is both part of the `bang not` operator
        // and the `factorial` operator,  additional filtering supports
        // `bang not` operator is required here.
        dialects =
            TestUtils.AllDialectsWhere(d => !d.SupportsFactorialOperator && d.SupportsBangNotOperator);

        foreach (var s in sqlStatements)
        {
            var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(s));
            Assert.Equal(
                ex.Error,
                new ParserError.ParserErrorValue("No infix parser for token ExclamationMark"));
        }
    }

    [Fact]
    public void parse_comments()
    {
        switch (TestUtils.AllDialectsWhere(d => d.SupportsCommentOn)
                    .VerifiedStmt("COMMENT ON COLUMN tab.name IS 'comment'"))
        {
            case Statement.Comment { CommentValue: { } comment } c:
                Assert.Equal("comment", comment);
                Assert.Equal("tab.name", c.ObjectName.ToString());
                Assert.Equal<CommentObject>(new CommentObject.Column(), c.ObjectType);
                Assert.False(c.IfExists);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }

        // https://www.postgresql.org/docs/current/sql-comment.html
        var objectTypes = new (string Keyword, CommentObject ExpectedObjectType)[]
        {
            ("COLLATION", new CommentObject.Collation()),
            ("COLUMN", new CommentObject.Column()),
            ("DATABASE", new CommentObject.Database()),
            ("DOMAIN", new CommentObject.Domain()),
            ("EXTENSION", new CommentObject.Extension()),
            ("FUNCTION", new CommentObject.Function()),
            ("INDEX", new CommentObject.Index()),
            ("MATERIALIZED VIEW", new CommentObject.MaterializedView()),
            ("PROCEDURE", new CommentObject.Procedure()),
            ("ROLE", new CommentObject.Role()),
            ("SCHEMA", new CommentObject.Schema()),
            ("SEQUENCE", new CommentObject.Sequence()),
            ("TABLE", new CommentObject.Table()),
            ("TYPE", new CommentObject.Type()),
            ("USER", new CommentObject.User()),
            ("VIEW", new CommentObject.View()),
        };
        foreach (var (keyword, expectedObjectType) in objectTypes)
        {
            switch (TestUtils.AllDialectsWhere(d => d.SupportsCommentOn)
                        .VerifiedStmt($"COMMENT IF EXISTS ON {keyword} db.t0 IS 'comment'"))
            {
                case Statement.Comment { CommentValue: { } comment } c:
                    Assert.Equal("comment", comment);
                    Assert.Equal("db.t0", c.ObjectName.ToString());
                    Assert.Equal(expectedObjectType, c.ObjectType);
                    Assert.True(c.IfExists);
                    break;
                default:
                    Assert.Fail("unreachable");
                    break;
            }
        }

        switch (TestUtils.AllDialectsWhere(d => d.SupportsCommentOn)
                    .VerifiedStmt("COMMENT IF EXISTS ON TABLE public.tab IS NULL"))
        {
            case Statement.Comment { CommentValue: null } c:
                Assert.Equal("public.tab", c.ObjectName.ToString());
                Assert.Equal<CommentObject>(new CommentObject.Table(), c.ObjectType);
                Assert.True(c.IfExists);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }

        // missing IS statement
        var ex = Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => d.SupportsCommentOn)
            .ParseSqlStatements("COMMENT ON TABLE t0"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: IS, found: EOF"));

        // missing comment literal
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => d.SupportsCommentOn)
            .ParseSqlStatements("COMMENT ON TABLE t0 IS"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: literal string, found: EOF"));

        // unknown object type
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialectsWhere(d => d.SupportsCommentOn)
            .ParseSqlStatements("COMMENT ON UNKNOWN t0 IS 'comment'"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: comment object_type, found: UNKNOWN"));
    }

    [Fact]
    public void parse_create_table_select()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsCreateTableSelect);
        var sql1 = "CREATE TABLE foo (baz INT) SELECT bar";
        var expected = "CREATE TABLE foo (baz INT) AS SELECT bar";
        _ = dialects.OneStatementParsesTo(sql1, expected);

        var sql2 = "CREATE TABLE foo (baz INT, name STRING) SELECT bar, oth_name FROM test.table_a";
        expected =
            "CREATE TABLE foo (baz INT, name STRING) AS SELECT bar, oth_name FROM test.table_a";
        _ = dialects.OneStatementParsesTo(sql2, expected);

        dialects = TestUtils.AllDialectsWhere(d => !d.SupportsCreateTableSelect);
        foreach (var sql in new[] { sql1, sql2 })
        {
            var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
            Assert.Equal(
                ex.Error,
                new ParserError.ParserErrorValue("Expected: end of statement, found: SELECT"));
        }
    }

    [Fact]
    public void test_reserved_keywords_for_identifiers()
    {
        // Dialects that reserve INTERVAL will not allow it as an unquoted
        // identifier, and report the failure consistently at the token that fails
        // to start an expression (`)`), independent of named-argument support.
        var dialects = TestUtils.AllDialectsWhere(d => d.IsReservedForIdentifier(Keyword.INTERVAL));
        var sql = "SELECT MAX(interval) FROM tbl";
        var ex = Assert.Throws<ParserException>(() => dialects.ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: an expression, found: )"));

        // Dialects that do not reserve the word INTERVAL will allow it
        dialects = TestUtils.AllDialectsWhere(d => !d.IsReservedForIdentifier(Keyword.INTERVAL));
        sql = "SELECT MAX(interval) FROM tbl";
        dialects.ParseSqlStatements(sql);
    }

    [Fact]
    public void test_keywords_as_column_names_after_dot()
    {
        // Test various keywords that have special meaning when standalone
        // but should be treated as identifiers after a dot.
        var keywords = new[]
        {
            "interval",  // INTERVAL '1' DAY
            "case",      // CASE WHEN ... END
            "cast",      // CAST(x AS y)
            "extract",   // EXTRACT(DAY FROM ...)
            "trim",      // TRIM(...)
            "substring", // SUBSTRING(...)
            "left",      // LEFT(str, n)
            "right",     // RIGHT(str, n)
        };

        foreach (var kw in keywords)
        {
            var sql = $"SELECT T.{kw} FROM T";
            VerifiedStmt(sql);

            sql = $"SELECT SUM(x) OVER (PARTITION BY T.{kw} ORDER BY T.id) FROM T";
            VerifiedStmt(sql);

            sql = $"SELECT T.{kw}, S.{kw} FROM T, S WHERE T.{kw} = S.{kw}";
            VerifiedStmt(sql);
        }

        var select = VerifiedOnlySelect("SELECT T.interval, T.case FROM T");
        switch (select.Projection[0])
        {
            case SelectItem.UnnamedExpr { Value: Expr.CompoundIdentifier { Value: var idents } }:
                Assert.Equal(2, idents.Count);
                Assert.Equal("T", idents[0].Value);
                Assert.Equal("interval", idents[1].Value);
                break;
            default:
                Assert.Fail("Expected CompoundIdentifier for T.interval");
                break;
        }
        switch (select.Projection[1])
        {
            case SelectItem.UnnamedExpr { Value: Expr.CompoundIdentifier { Value: var idents } }:
                Assert.Equal(2, idents.Count);
                Assert.Equal("T", idents[0].Value);
                Assert.Equal("case", idents[1].Value);
                break;
            default:
                Assert.Fail("Expected CompoundIdentifier for T.case");
                break;
        }
    }

    [Fact]
    public void parse_create_table_with_bit_types()
    {
        var sql = "CREATE TABLE t (a BIT, b BIT VARYING, c BIT(42), d BIT VARYING(43))";
        {
            var columns = Assert.IsType<Statement.CreateTable>(VerifiedStmt(sql)).Value.Columns;
            Assert.Equal(4, columns.Count);
            Assert.Equal<DataType>(columns[0].DataType, new DataType.Bit(null));
            Assert.Equal("a BIT", columns[0].ToString());
            Assert.Equal<DataType>(columns[1].DataType, new DataType.BitVarying(null));
            Assert.Equal("b BIT VARYING", columns[1].ToString());
            Assert.Equal<DataType>(columns[2].DataType, new DataType.Bit(42));
            Assert.Equal("c BIT(42)", columns[2].ToString());
            Assert.Equal<DataType>(columns[3].DataType, new DataType.BitVarying(43));
            Assert.Equal("d BIT VARYING(43)", columns[3].ToString());
        }
    }

    [Fact]
    public void parse_composite_access_expr()
    {
        Assert.Equal<Expr>(
            VerifiedExpr("f(a).b"),
            new Expr.CompoundFieldAccess(
                Root: new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(new Ident("f"))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.List(new FunctionArgumentList(
                        DuplicateTreatment: null,
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                            new Expr.Identifier(new Ident("a"))))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                    NullTreatment: null,
                    Filter: null,
                    Over: null,
                    WithinGroup: Sequence<OrderByExpr>.Empty)),
                AccessChain: Sequence.Of<AccessExpr>(new AccessExpr.Dot(new Expr.Identifier(new Ident("b"))))));

        // Nested Composite Access
        Assert.Equal<Expr>(
            VerifiedExpr("f(a).b.c"),
            new Expr.CompoundFieldAccess(
                Root: new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(new Ident("f"))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.List(new FunctionArgumentList(
                        DuplicateTreatment: null,
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                            new Expr.Identifier(new Ident("a"))))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                    NullTreatment: null,
                    Filter: null,
                    Over: null,
                    WithinGroup: Sequence<OrderByExpr>.Empty)),
                AccessChain: Sequence.Of<AccessExpr>(
                    new AccessExpr.Dot(new Expr.Identifier(new Ident("b"))),
                    new AccessExpr.Dot(new Expr.Identifier(new Ident("c"))))));

        // Composite Access in Select and Where Clauses
        var stmt = VerifiedOnlySelect("SELECT f(a).b FROM t WHERE f(a).b IS NOT NULL");
        Expr expr = new Expr.CompoundFieldAccess(
            Root: new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("f"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.List(new FunctionArgumentList(
                    DuplicateTreatment: null,
                    Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                        new Expr.Identifier(new Ident("a"))))),
                    Clauses: Sequence<FunctionArgumentClause>.Empty)),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            AccessChain: Sequence.Of<AccessExpr>(new AccessExpr.Dot(new Expr.Identifier(new Ident("b")))));

        Assert.Equal<SelectItem>(stmt.Projection[0], new SelectItem.UnnamedExpr(expr));
        // stmt.selection.unwrap()
        Assert.NotNull(stmt.Selection);
        Assert.Equal<Expr>(stmt.Selection, new Expr.IsNotNull(expr));

        // Compound access with quoted identifier.
        TestUtils.AllDialectsWhere(d => d.IsDelimitedIdentifierStart('"'))
            .VerifiedOnlySelect("SELECT f(a).\"an id\"");

        // Composite Access in struct literal
        TestUtils.AllDialectsWhere(d => d.SupportsStructLiteral).VerifiedStmt(
            "SELECT * FROM t WHERE STRUCT(STRUCT(1 AS a, NULL AS b) AS c, NULL AS d).c.a IS NOT NULL");
        var supportStruct = TestUtils.AllDialectsWhere(d => d.SupportsStructLiteral);
        stmt = supportStruct
            .VerifiedOnlySelect("SELECT STRUCT(STRUCT(1 AS a, NULL AS b) AS c, NULL AS d).c.a");
        SelectItem expected = new SelectItem.UnnamedExpr(new Expr.CompoundFieldAccess(
            Root: new Expr.Struct(
                Values: Sequence.Of<Expr>(
                    new Expr.Named(
                        Name: new Ident("c"),
                        Expr: new Expr.Struct(
                            Values: Sequence.Of<Expr>(
                                new Expr.Named(
                                    Name: new Ident("a"),
                                    Expr: new Expr.Value(
                                        new Value.Number("1", false).WithEmptySpan())),
                                new Expr.Named(
                                    Name: new Ident("b"),
                                    Expr: new Expr.Value(new Value.Null().WithEmptySpan()))),
                            Fields: Sequence<StructField>.Empty)),
                    new Expr.Named(
                        Name: new Ident("d"),
                        Expr: new Expr.Value(new Value.Null().WithEmptySpan()))),
                Fields: Sequence<StructField>.Empty),
            AccessChain: Sequence.Of<AccessExpr>(
                new AccessExpr.Dot(new Expr.Identifier(new Ident("c"))),
                new AccessExpr.Dot(new Expr.Identifier(new Ident("a"))))));
        Assert.Equal(stmt.Projection[0], expected);
    }

    [Fact]
    public void parse_create_table_with_enum_types()
    {
        var sql = "CREATE TABLE t0 (foo ENUM8('a' = 1, 'b' = 2), bar ENUM16('a' = 1, 'b' = 2), baz ENUM('a', 'b'))";
        {
            var createTable = Assert.IsType<Statement.CreateTable>(TestUtils.AllDialects().VerifiedStmt(sql)).Value;
            Assert.Equal("t0", createTable.Name.ToString());
            Assert.Equal(
                Sequence.Of(
                    new ColumnDef(
                        Name: new Ident("foo"),
                        DataType: new DataType.Enum(
                            Sequence.Of<EnumMember>(
                                new EnumMember.NamedValue(
                                    "a",
                                    new Expr.Value(
                                        new Value.Number("1", false).WithEmptySpan())),
                                new EnumMember.NamedValue(
                                    "b",
                                    new Expr.Value(
                                        new Value.Number("2", false).WithEmptySpan()))),
                            8),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    new ColumnDef(
                        Name: new Ident("bar"),
                        DataType: new DataType.Enum(
                            Sequence.Of<EnumMember>(
                                new EnumMember.NamedValue(
                                    "a",
                                    new Expr.Value(
                                        new Value.Number("1", false).WithEmptySpan())),
                                new EnumMember.NamedValue(
                                    "b",
                                    new Expr.Value(
                                        new Value.Number("2", false).WithEmptySpan()))),
                            16),
                        Options: Sequence<ColumnOptionDef>.Empty),
                    new ColumnDef(
                        Name: new Ident("baz"),
                        DataType: new DataType.Enum(
                            Sequence.Of<EnumMember>(
                                new EnumMember.Name("a"),
                                new EnumMember.Name("b")),
                            null),
                        Options: Sequence<ColumnOptionDef>.Empty)),
                createTable.Columns);
        }

        // invalid case missing value for enum pair
        var ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("CREATE TABLE t0 (foo ENUM8('a' = 1, 'b' = ))"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: a value, found: )"));

        // invalid case that name is not a string
        ex = Assert.Throws<ParserException>(() => TestUtils.AllDialects()
            .ParseSqlStatements("CREATE TABLE t0 (foo ENUM8('a' = 1, 2))"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue("Expected: literal string, found: 2"));
    }

    [Fact]
    public void test_table_sample()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsTableSampleBeforeAlias);
        dialects.VerifiedStmt("SELECT * FROM tbl TABLESAMPLE (50) AS t");
        dialects.VerifiedStmt("SELECT * FROM tbl TABLESAMPLE (50 ROWS) AS t");
        dialects.VerifiedStmt("SELECT * FROM tbl TABLESAMPLE (50 PERCENT) AS t");

        dialects = TestUtils.AllDialectsWhere(d => !d.SupportsTableSampleBeforeAlias);
        dialects.VerifiedStmt("SELECT * FROM tbl AS t TABLESAMPLE BERNOULLI (50)");
        dialects.VerifiedStmt("SELECT * FROM tbl AS t TABLESAMPLE SYSTEM (50)");
        dialects.VerifiedStmt("SELECT * FROM tbl AS t TABLESAMPLE SYSTEM (50) REPEATABLE (10)");
    }

    // FALHA (não é divergência): o parse funciona, mas `statement.ToString()` estoura a pilha
    // (StackOverflowException em Expr.Fmt, ~186 níveis da árvore de BinaryOp) e derruba o processo de
    // teste, anulando a execução inteira. Só por isso o teste fica com Skip até a correção no src/.
    // Detalhes em docs/port/onda-c/C4.md.
    [Fact]
    public void overflow()
    {
        var expr = string.Join(" + ", Enumerable.Repeat("1", 1000));
        var sql = $"SELECT {expr}";

        var statements = Parser.ParseSql(new GenericDialect(), sql);
        var statement = statements[statements.Count - 1]; // statements.pop().unwrap()
        Assert.Equal(statement.ToString(), sql);
    }

    // FALHA (não é divergência): no C#, o parse estoura a pilha (StackOverflowException, que não pode
    // ser capturada) por volta do 116º nível de aninhamento, apesar da proteção de DIV-005
    // (RecursionCounter.TryDecrease + EnsureSufficientExecutionStack). O estouro derruba o processo de
    // teste e anula a execução inteira (nenhum outro resultado é reportado); por isso, e só por isso, o
    // teste fica com Skip até a correção no src/. Detalhes em docs/port/onda-c/C4.md.
    [Fact]
    public void parse_deeply_nested_boolean_expr_does_not_stackoverflow()
    {
        static string BuildNestedExpr(int depth)
        {
            if (depth == 0)
            {
                return "x = 1";
            }
            return $"({BuildNestedExpr(0)} OR {BuildNestedExpr(0)} AND ({BuildNestedExpr(depth - 1)}))";
        }

        var depth = 200;
        var whereClause = BuildNestedExpr(depth);
        var sql = $"SELECT pk FROM tab0 WHERE {whereClause}";

        // .try_with_sql(&sql).expect("tokenize to work"): a TokenizerException derruba o teste.
        var statements = new Parser(new GenericDialect())
            .TryWithSql(sql)
            .WithRecursionLimit(depth * 10)
            .ParseStatements();
        var statement = statements[statements.Count - 1]; // statements.pop().unwrap()
        Assert.Equal(statement.ToString(), sql);
    }

    [Fact]
    public void parse_select_without_projection()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsEmptyProjections);
        dialects.VerifiedStmt("SELECT FROM users");
    }

    [Fact]
    public void parse_update_from_before_select()
    {
        VerifiedStmt("UPDATE t1 FROM (SELECT name, id FROM t1 GROUP BY id) AS t2 SET name = t2.name WHERE t1.id = t2.id");
        VerifiedStmt("UPDATE t1 FROM U, (SELECT id FROM V) AS W SET a = b WHERE 1 = 1");

        var query =
            "UPDATE t1 FROM (SELECT name, id FROM t1 GROUP BY id) AS t2 SET name = t2.name FROM (SELECT name from t2) AS t2";
        var ex = Assert.Throws<ParserException>(() => ParseSqlStatements(query));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: FROM"),
            ex.Error);
    }

    [Fact]
    public void parse_overlaps()
    {
        VerifiedStmt("SELECT (DATE '2016-01-10', DATE '2016-02-01') OVERLAPS (DATE '2016-01-20', DATE '2016-02-10')");
    }

    [Fact]
    public void parse_column_definition_trailing_commas()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsColumnDefinitionTrailingCommas);

        dialects.OneStatementParsesTo("CREATE TABLE T (x INT64,)", "CREATE TABLE T (x INT64)");
        dialects.OneStatementParsesTo(
            "CREATE TABLE T (x INT64, y INT64, )",
            "CREATE TABLE T (x INT64, y INT64)");
        dialects.OneStatementParsesTo(
            "CREATE VIEW T (x, y, ) AS SELECT 1",
            "CREATE VIEW T (x, y) AS SELECT 1");

        var unsupportedDialects = TestUtils.AllDialectsWhere(d =>
            !d.SupportsProjectionTrailingCommas && !d.SupportsTrailingCommas);
        var ex = Assert.Throws<ParserException>(() => unsupportedDialects
            .ParseSqlStatements("CREATE TABLE employees (name text, age int,)"));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: column name or constraint definition, found: )"));
    }

    [Fact]
    public void test_trailing_commas_in_from()
    {
        var dialects = TestUtils.AllDialectsWhere(d => d.SupportsFromTrailingCommas);
        dialects.VerifiedOnlySelectWithCanonical("SELECT 1, 2 FROM t,", "SELECT 1, 2 FROM t");

        dialects
            .VerifiedOnlySelectWithCanonical("SELECT 1, 2 FROM t1, t2,", "SELECT 1, 2 FROM t1, t2");

        var sql = "SELECT a, FROM b, LIMIT 1";
        _ = dialects.ParseSqlStatements(sql);

        sql = "INSERT INTO a SELECT b FROM c,";
        _ = dialects.ParseSqlStatements(sql);

        sql = "SELECT a FROM b, HAVING COUNT(*) > 1";
        _ = dialects.ParseSqlStatements(sql);

        sql = "SELECT a FROM b, WHERE c = 1";
        _ = dialects.ParseSqlStatements(sql);

        // nested
        sql = "SELECT 1, 2 FROM (SELECT * FROM t,),";
        _ = dialects.ParseSqlStatements(sql);

        // multiple_subqueries
        dialects.VerifiedOnlySelectWithCanonical(
            "SELECT 1, 2 FROM (SELECT * FROM t1), (SELECT * FROM t2),",
            "SELECT 1, 2 FROM (SELECT * FROM t1), (SELECT * FROM t2)");
    }

    // #[cfg(feature = "visitor")]: o visitor está portado (src/ast/visitor.rs → Ast/Visitor/, ADR-0007).
    [Fact]
    public void test_visit_order()
    {
        var sql = "SELECT CASE a WHEN 1 THEN 2 WHEN 3 THEN 4 ELSE 5 END";
        var stmt = VerifiedStmt(sql);
        var visited = new List<string>();
        _ = Visitor.VisitExpressions(stmt, expr =>
        {
            visited.Add(expr.ToString());
            return ControlFlow<ValueTuple>.Continue; // core::ops::ControlFlow::<()>::Continue(())
        });

        // assert_eq!(visited, [..]): o literal vai na posição "expected" (xUnit2000).
        Assert.Equal(
            new[] { "CASE a WHEN 1 THEN 2 WHEN 3 THEN 4 ELSE 5 END", "a", "1", "2", "3", "4", "5" },
            visited);
    }
}
