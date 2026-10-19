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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_mssql.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

// Notas da tradução (docs/port/onda-c/C13.md):
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests e SpansTests).
// - `assert_eq!(x.len(), 1)` vira `Assert.Equal(1, x.Count)`; o xUnit2013 (que sugere Assert.Single)
//   fica suprimido neste arquivo, como em AstModTests e SpansTests.
// - Strings Rust com `\` no fim da linha (continuação, que remove a quebra e a indentação da linha
//   seguinte) viram concatenações de pedaços; quebras de linha reais viram `\n` explícito.
// - `panic!(msg)` → `Assert.Fail(msg)`; `unreachable!()` → `throw new InvalidOperationException(..)`;
//   `let P = x else { .. };` → `if (x is not P) { ..; return; }`.
#pragma warning disable xUnit2013

/// <summary>
/// Test SQL syntax specific to Microsoft's T-SQL. The parser based on the
/// generic dialect is also tested (on the inputs it can handle).
/// </summary>
public class SqlParserMsSqlTests
{
    [Fact]
    public void parse_mssql_identifiers()
    {
        var sql = "SELECT @@version, _foo$123 FROM ##temp";
        var select = MsAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal<Expr>(
            new Expr.Identifier(new Ident("@@version")),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal<Expr>(
            new Expr.Identifier(new Ident("_foo$123")),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(2, select.Projection.Count);
        switch (TestUtils.Only(select.From).Relation)
        {
            case TableFactor.Table { Name: var name }:
                Assert.Equal("##temp", name.ToString());
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_table_time_travel()
    {
        var version = "2023-08-18 23:08:18";
        var sql = $"SELECT 1 FROM t1 FOR SYSTEM_TIME AS OF '{version}'";
        var select = Ms().VerifiedOnlySelect(sql);
        Assert.Equal(
            select.From,
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("t1"))),
                    Alias: null,
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: new TableVersion.ForSystemTimeAsOf(new Expr.Value(
                        new Value.SingleQuotedString(version).WithEmptySpan())),
                    Partitions: Sequence<Ident>.Empty,
                    WithOrdinality: false,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Joins: Sequence<Join>.Empty)));

        sql = "SELECT 1 FROM t1 FOR SYSTEM TIME AS OF 'some_timestamp'";
        Assert.Throws<ParserException>(() => Ms().ParseSqlStatements(sql));
    }

    [Fact]
    public void parse_mssql_single_quoted_aliases()
    {
        _ = MsAndGeneric().OneStatementParsesTo("SELECT foo 'alias'", "SELECT foo AS 'alias'");
    }

    [Fact]
    public void parse_mssql_delimited_identifiers()
    {
        var s = Ms().OneStatementParsesTo(
            "SELECT [a.b!] [FROM] FROM foo [WHERE]",
            "SELECT [a.b!] AS [FROM] FROM foo [WHERE]");
        if (s is Statement.Query { Value: var q })
        {
            var from = (q.Body.AsSelect() ?? throw new InvalidOperationException("not a SELECT")).From;
            if (from.Count == 1)
            {
                switch (from[0].Relation)
                {
                    case TableFactor.Table { Name: var name, Alias: var alias }:
                        Assert.Equal("foo", name.ToString());
                        Assert.Equal(
                            alias,
                            new TableAlias(
                                Explicit: false,
                                Name: Ident.WithQuote('[', "WHERE"),
                                Columns: Sequence<TableAliasColumnDef>.Empty,
                                At: null));
                        break;
                    default:
                        Assert.Fail("unexpected FROM type");
                        break;
                }
            }
            else
            {
                Assert.Fail("unexpected number of FROMs");
            }
        }
        else
        {
            Assert.Fail("statement not parsed as QUERY");
        }
    }

    [Fact]
    public void parse_create_procedure()
    {
        var sql = "CREATE OR ALTER PROCEDURE test (@foo INT, @bar VARCHAR(256)) AS BEGIN SELECT 1; END";

        Assert.Equal(
            Ms().VerifiedStmt(sql),
            new Statement.CreateProcedure(
                OrAlter: true,
                Body: new ConditionalStatements.BeginEnd(new BeginEndStatements(
                    BeginToken: AttachedToken.Empty(),
                    Statements: Sequence.Of<Statement>(new Statement.Query(new Query(
                        With: null,
                        LimitClause: null,
                        Fetch: null,
                        Locks: Sequence<LockClause>.Empty,
                        ForClause: null,
                        OrderBy: null,
                        Settings: null,
                        FormatClause: null,
                        PipeOperators: Sequence<PipeOperator>.Empty,
                        Body: new SetExpr.Select(new Select(
                            SelectToken: AttachedToken.Empty(),
                            OptimizerHints: Sequence<OptimizerHint>.Empty,
                            Distinct: null,
                            SelectModifiers: null,
                            Top: null,
                            TopBeforeDistinct: false,
                            Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Value(
                                TestUtils.Number("1").WithEmptySpan()))),
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
                            WindowBeforeQualify: false,
                            Qualify: null,
                            ValueTableMode: null,
                            ConnectBy: Sequence<ConnectByKind>.Empty,
                            Flavor: SelectFlavor.Standard))))),
                    EndToken: AttachedToken.Empty())),
                Params: Sequence.Of(
                    new ProcedureParam(
                        Name: new Ident(Value: "@foo", QuoteStyle: null, Span: Span.Empty()),
                        DataType: new DataType.Int(null),
                        Mode: null,
                        Default: null),
                    new ProcedureParam(
                        Name: new Ident(Value: "@bar", QuoteStyle: null, Span: Span.Empty()),
                        DataType: new DataType.Varchar(new CharacterLength.IntegerLength(
                            Length: 256,
                            Unit: null)),
                        Mode: null,
                        Default: null)),
                Name: ObjectName.From(Sequence.Of(new Ident(Value: "test", QuoteStyle: null, Span: Span.Empty()))),
                Language: null));
    }

    [Fact]
    public void parse_mssql_create_procedure()
    {
        _ = MsAndGeneric().VerifiedStmt("CREATE OR ALTER PROCEDURE foo AS SELECT 1;");
        _ = MsAndGeneric().VerifiedStmt("CREATE OR ALTER PROCEDURE foo AS BEGIN SELECT 1; END");
        _ = MsAndGeneric().VerifiedStmt("CREATE PROCEDURE foo AS BEGIN SELECT 1; END");
        _ = Ms().VerifiedStmt(
            "CREATE PROCEDURE foo AS BEGIN SELECT [myColumn] FROM [myschema].[mytable]; END");
        _ = MsAndGeneric().VerifiedStmt(
            "CREATE PROCEDURE foo (@CustomerName NVARCHAR(50)) AS BEGIN SELECT * FROM DEV; END");
        _ = Ms().VerifiedStmt("CREATE PROCEDURE [foo] AS BEGIN UPDATE bar SET col = 'test'; END");
        // Test a statement with END in it
        _ = Ms().VerifiedStmt("CREATE PROCEDURE [foo] AS BEGIN SELECT [foo], CASE WHEN [foo] IS NULL THEN 'empty' ELSE 'notempty' END AS [foo]; END");
        // Multiple statements
        _ = Ms().VerifiedStmt("CREATE PROCEDURE [foo] AS BEGIN UPDATE bar SET col = 'test'; SELECT [foo] FROM BAR WHERE [FOO] > 10; END");

        // parameters with default values
        var sql = "CREATE PROCEDURE foo (IN @a INTEGER = 1, OUT @b TEXT = '2', INOUT @c DATETIME = NULL, @d BOOL = 0) AS BEGIN SELECT 1; END";
        _ = Ms().VerifiedStmt(sql);
    }

    [Fact]
    public void parse_create_function()
    {
        var returnExpressionFunction = "CREATE FUNCTION some_scalar_udf(@foo INT, @bar VARCHAR(256)) RETURNS INT AS BEGIN RETURN 1; END";
        Assert.Equal(
            Ms().VerifiedStmt(returnExpressionFunction),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: false,
                Temporary: false,
                IfNotExists: false,
                Name: ObjectName.From(Sequence.Of(new Ident("some_scalar_udf"))),
                Args: Sequence.Of(
                    new OperateFunctionArg(
                        Mode: null,
                        Name: new Ident("@foo"),
                        DataType: new DataType.Int(null),
                        DefaultExpr: null),
                    new OperateFunctionArg(
                        Mode: null,
                        Name: new Ident("@bar"),
                        DataType: new DataType.Varchar(new CharacterLength.IntegerLength(
                            Length: 256,
                            Unit: null)),
                        DefaultExpr: null)),
                ReturnType: new FunctionReturnType.DataType(new DataType.Int(null)),
                FunctionBody: new CreateFunctionBody.AsBeginEnd(new BeginEndStatements(
                    BeginToken: AttachedToken.Empty(),
                    Statements: Sequence.Of<Statement>(new Statement.Return(new ReturnStatement(
                        new ReturnStatementValue.Expr(new Expr.Value(
                            TestUtils.Number("1").WithEmptySpan()))))),
                    EndToken: AttachedToken.Empty())),
                Behavior: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                Using: null,
                Language: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));

        var multiStatementFunction =
            "CREATE FUNCTION some_scalar_udf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS INT " +
            "AS " +
            "BEGIN " +
            "SET @foo = @foo + 1; " +
            "RETURN @foo; " +
            "END";
        _ = Ms().VerifiedStmt(multiStatementFunction);

        var multiStatementFunctionWithoutAs = multiStatementFunction.Replace(" AS", "");
        _ = Ms().OneStatementParsesTo(
            multiStatementFunctionWithoutAs,
            multiStatementFunction);

        var createFunctionWithConditional =
            "CREATE FUNCTION some_scalar_udf() " +
            "RETURNS INT " +
            "AS " +
            "BEGIN " +
            "IF 1 = 2 " +
            "BEGIN " +
            "RETURN 1; " +
            "END; " +
            "RETURN 0; " +
            "END";
        _ = Ms().VerifiedStmt(createFunctionWithConditional);

        var createOrAlterFunction =
            "CREATE OR ALTER FUNCTION some_scalar_udf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS INT " +
            "AS " +
            "BEGIN " +
            "SET @foo = @foo + 1; " +
            "RETURN @foo; " +
            "END";
        _ = Ms().VerifiedStmt(createOrAlterFunction);

        var createFunctionWithReturnExpression =
            "CREATE FUNCTION some_scalar_udf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS INT " +
            "AS " +
            "BEGIN " +
            "RETURN CONVERT(INT, 1) + 2; " +
            "END";
        _ = Ms().VerifiedStmt(createFunctionWithReturnExpression);

        var createInlineTableValueFunction =
            "CREATE FUNCTION some_inline_tvf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS TABLE " +
            "AS " +
            "RETURN (SELECT 1 AS col_1)";
        _ = Ms().VerifiedStmt(createInlineTableValueFunction);

        var createInlineTableValueFunctionWithoutParentheses =
            "CREATE FUNCTION some_inline_tvf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS TABLE " +
            "AS " +
            "RETURN SELECT 1 AS col_1";
        _ = Ms().VerifiedStmt(createInlineTableValueFunctionWithoutParentheses);

        var createInlineTableValueFunctionWithoutAs =
            createInlineTableValueFunction.Replace(" AS", "");
        _ = Ms().OneStatementParsesTo(
            createInlineTableValueFunctionWithoutAs,
            createInlineTableValueFunction);

        var createMultiStatementTableValueFunction =
            "CREATE FUNCTION some_multi_statement_tvf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS @t TABLE (col_1 INT) " +
            "AS " +
            "BEGIN " +
            "INSERT INTO @t SELECT 1; " +
            "RETURN; " +
            "END";
        _ = Ms().VerifiedStmt(createMultiStatementTableValueFunction);

        var createMultiStatementTableValueFunctionWithoutAs =
            createMultiStatementTableValueFunction.Replace(" AS", "");
        _ = Ms().OneStatementParsesTo(
            createMultiStatementTableValueFunctionWithoutAs,
            createMultiStatementTableValueFunction);

        var createMultiStatementTableValueFunctionWithConstraints =
            "CREATE FUNCTION some_multi_statement_tvf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS @t TABLE (col_1 INT NOT NULL) " +
            "AS " +
            "BEGIN " +
            "INSERT INTO @t SELECT 1; " +
            "RETURN @t; " +
            "END";
        _ = Ms().VerifiedStmt(createMultiStatementTableValueFunctionWithConstraints);

        // No Rust, a linha "RETURNS @t TABLE ()" não termina com `\`: a quebra de linha e a
        // indentação da linha seguinte fazem parte da string.
        var createMultiStatementTvfWithoutTableDefinition =
            "CREATE FUNCTION incorrect_tvf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS @t TABLE ()\n" +
            "        AS " +
            "BEGIN " +
            "INSERT INTO @t SELECT 1; " +
            "RETURN @t; " +
            "END";
        Assert.Equal(
            new ParserError.ParserErrorValue("Unparsable function body"),
            Assert.Throws<ParserException>(() => Ms().ParseSqlStatements(createMultiStatementTvfWithoutTableDefinition))
                .Error);

        // Idem: a linha "RETURNS TABLE" não termina com `\`.
        var createInlineTvfWithoutSubqueryOrBareSelect =
            "CREATE FUNCTION incorrect_tvf(@foo INT, @bar VARCHAR(256)) " +
            "RETURNS TABLE\n" +
            "        AS " +
            "RETURN 'hi'";
        Assert.Equal(
            new ParserError.ParserErrorValue(
                "Expected a subquery (or bare SELECT statement) after RETURN"),
            Assert.Throws<ParserException>(() => Ms().ParseSqlStatements(createInlineTvfWithoutSubqueryOrBareSelect))
                .Error);
    }

    [Fact]
    public void parse_create_function_parameter_default_values()
    {
        var singleDefaultSql =
            "CREATE FUNCTION test_func(@param1 INT = 42) RETURNS INT AS BEGIN RETURN @param1; END";
        Assert.Equal(
            Ms().VerifiedStmt(singleDefaultSql),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: false,
                Temporary: false,
                IfNotExists: false,
                Name: ObjectName.From(Sequence.Of(new Ident("test_func"))),
                Args: Sequence.Of(new OperateFunctionArg(
                    Mode: null,
                    Name: new Ident("@param1"),
                    DataType: new DataType.Int(null),
                    DefaultExpr: new Expr.Value(TestUtils.Number("42").WithEmptySpan()))),
                ReturnType: new FunctionReturnType.DataType(new DataType.Int(null)),
                FunctionBody: new CreateFunctionBody.AsBeginEnd(new BeginEndStatements(
                    BeginToken: AttachedToken.Empty(),
                    Statements: Sequence.Of<Statement>(new Statement.Return(new ReturnStatement(
                        new ReturnStatementValue.Expr(new Expr.Identifier(new Ident(
                            "@param1")))))),
                    EndToken: AttachedToken.Empty())),
                Behavior: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                Using: null,
                Language: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));
    }

    [Fact]
    public void parse_mssql_apply_join()
    {
        _ = MsAndGeneric().VerifiedOnlySelect(
            "SELECT * FROM sys.dm_exec_query_stats AS deqs " +
            "CROSS APPLY sys.dm_exec_query_plan(deqs.plan_handle)");
        _ = MsAndGeneric().VerifiedOnlySelect(
            "SELECT * FROM sys.dm_exec_query_stats AS deqs " +
            "OUTER APPLY sys.dm_exec_query_plan(deqs.plan_handle)");
        _ = MsAndGeneric().VerifiedOnlySelect(
            "SELECT * FROM foo " +
            "OUTER APPLY (SELECT foo.x + 1) AS bar");
    }

    [Fact]
    public void parse_mssql_openjson()
    {
        var select = Ms().VerifiedOnlySelect(
            "SELECT B.kind, B.id_list " +
            "FROM t_test_table AS A " +
            "CROSS APPLY OPENJSON(A.param, '$.config') WITH (kind VARCHAR(20) '$.kind', [id_list] NVARCHAR(MAX) '$.id_list' AS JSON) AS B");
        Assert.Equal(
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("t_test_table"))),
                    Alias: TestUtils.TableAlias(true, "A"),
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    WithOrdinality: false,
                    Partitions: Sequence<Ident>.Empty,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Joins: Sequence.Of(new Join(
                    Relation: new TableFactor.OpenJsonTable(
                        JsonExpr: new Expr.CompoundIdentifier(
                            Sequence.Of(new Ident("A"), new Ident("param"))),
                        JsonPath: new Value.SingleQuotedString("$.config").WithEmptySpan(),
                        Columns: Sequence.Of(
                            new OpenJsonTableColumn(
                                Name: new Ident("kind"),
                                Type: new DataType.Varchar(new CharacterLength.IntegerLength(
                                    Length: 20,
                                    Unit: null)),
                                Path: "$.kind",
                                AsJson: false),
                            new OpenJsonTableColumn(
                                Name: new Ident(Value: "id_list", QuoteStyle: '[', Span: Span.Empty()),
                                Type: new DataType.Nvarchar(new CharacterLength.Max()),
                                Path: "$.id_list",
                                AsJson: true)),
                        Alias: TestUtils.TableAlias(true, "B")),
                    Global: false,
                    JoinOperator: new JoinOperator.CrossApply())))),
            select.From);
        select = Ms().VerifiedOnlySelect(
            "SELECT B.kind, B.id_list " +
            "FROM t_test_table AS A " +
            "CROSS APPLY OPENJSON(A.param) WITH (kind VARCHAR(20) '$.kind', [id_list] NVARCHAR(MAX) '$.id_list' AS JSON) AS B");
        Assert.Equal(
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("t_test_table"))),
                    Alias: TestUtils.TableAlias(true, "A"),
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    WithOrdinality: false,
                    Partitions: Sequence<Ident>.Empty,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Joins: Sequence.Of(new Join(
                    Relation: new TableFactor.OpenJsonTable(
                        JsonExpr: new Expr.CompoundIdentifier(
                            Sequence.Of(new Ident("A"), new Ident("param"))),
                        JsonPath: null,
                        Columns: Sequence.Of(
                            new OpenJsonTableColumn(
                                Name: new Ident("kind"),
                                Type: new DataType.Varchar(new CharacterLength.IntegerLength(
                                    Length: 20,
                                    Unit: null)),
                                Path: "$.kind",
                                AsJson: false),
                            new OpenJsonTableColumn(
                                Name: new Ident(Value: "id_list", QuoteStyle: '[', Span: Span.Empty()),
                                Type: new DataType.Nvarchar(new CharacterLength.Max()),
                                Path: "$.id_list",
                                AsJson: true)),
                        Alias: TestUtils.TableAlias(true, "B")),
                    Global: false,
                    JoinOperator: new JoinOperator.CrossApply())))),
            select.From);
        select = Ms().VerifiedOnlySelect(
            "SELECT B.kind, B.id_list " +
            "FROM t_test_table AS A " +
            "CROSS APPLY OPENJSON(A.param) WITH (kind VARCHAR(20), [id_list] NVARCHAR(MAX)) AS B");
        Assert.Equal(
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("t_test_table"))),
                    Alias: TestUtils.TableAlias(true, "A"),
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    WithOrdinality: false,
                    Partitions: Sequence<Ident>.Empty,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Joins: Sequence.Of(new Join(
                    Relation: new TableFactor.OpenJsonTable(
                        JsonExpr: new Expr.CompoundIdentifier(
                            Sequence.Of(new Ident("A"), new Ident("param"))),
                        JsonPath: null,
                        Columns: Sequence.Of(
                            new OpenJsonTableColumn(
                                Name: new Ident("kind"),
                                Type: new DataType.Varchar(new CharacterLength.IntegerLength(
                                    Length: 20,
                                    Unit: null)),
                                Path: null,
                                AsJson: false),
                            new OpenJsonTableColumn(
                                Name: new Ident(Value: "id_list", QuoteStyle: '[', Span: Span.Empty()),
                                Type: new DataType.Nvarchar(new CharacterLength.Max()),
                                Path: null,
                                AsJson: false)),
                        Alias: TestUtils.TableAlias(true, "B")),
                    Global: false,
                    JoinOperator: new JoinOperator.CrossApply())))),
            select.From);
        select = MsAndGeneric().VerifiedOnlySelect(
            "SELECT B.kind, B.id_list " +
            "FROM t_test_table AS A " +
            "CROSS APPLY OPENJSON(A.param, '$.config') AS B");
        Assert.Equal(
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("t_test_table"))),
                    Alias: TestUtils.TableAlias(true, "A"),
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    WithOrdinality: false,
                    Partitions: Sequence<Ident>.Empty,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Joins: Sequence.Of(new Join(
                    Relation: new TableFactor.OpenJsonTable(
                        JsonExpr: new Expr.CompoundIdentifier(
                            Sequence.Of(new Ident("A"), new Ident("param"))),
                        JsonPath: new Value.SingleQuotedString("$.config").WithEmptySpan(),
                        Columns: Sequence<OpenJsonTableColumn>.Empty,
                        Alias: TestUtils.TableAlias(true, "B")),
                    Global: false,
                    JoinOperator: new JoinOperator.CrossApply())))),
            select.From);
        select = MsAndGeneric().VerifiedOnlySelect(
            "SELECT B.kind, B.id_list " +
            "FROM t_test_table AS A " +
            "CROSS APPLY OPENJSON(A.param) AS B");
        Assert.Equal(
            Sequence.Of(new TableWithJoins(
                Relation: new TableFactor.Table(
                    Name: ObjectName.From(Sequence.Of(new Ident("t_test_table"))),
                    Alias: TestUtils.TableAlias(true, "A"),
                    Args: null,
                    WithHints: Sequence<Expr>.Empty,
                    Version: null,
                    WithOrdinality: false,
                    Partitions: Sequence<Ident>.Empty,
                    JsonPath: null,
                    Sample: null,
                    IndexHints: Sequence<TableIndexHints>.Empty),
                Joins: Sequence.Of(new Join(
                    Relation: new TableFactor.OpenJsonTable(
                        JsonExpr: new Expr.CompoundIdentifier(
                            Sequence.Of(new Ident("A"), new Ident("param"))),
                        JsonPath: null,
                        Columns: Sequence<OpenJsonTableColumn>.Empty,
                        Alias: TestUtils.TableAlias(true, "B")),
                    Global: false,
                    JoinOperator: new JoinOperator.CrossApply())))),
            select.From);
    }

    [Fact]
    public void parse_mssql_top_paren()
    {
        var sql = "SELECT TOP (5) * FROM foo";
        var select = MsAndGeneric().VerifiedOnlySelect(sql);
        var top = select.Top!;
        Assert.Equal<TopQuantity?>(
            new TopQuantity.Expr(new Expr.Value(
                TestUtils.Number("5").WithEmptySpan())),
            top.Quantity);
        Assert.False(top.Percent);
    }

    [Fact]
    public void parse_mssql_top_percent()
    {
        var sql = "SELECT TOP (5) PERCENT * FROM foo";
        var select = MsAndGeneric().VerifiedOnlySelect(sql);
        var top = select.Top!;
        Assert.Equal<TopQuantity?>(
            new TopQuantity.Expr(new Expr.Value(
                TestUtils.Number("5").WithEmptySpan())),
            top.Quantity);
        Assert.True(top.Percent);
    }

    [Fact]
    public void parse_mssql_top_with_ties()
    {
        var sql = "SELECT TOP (5) WITH TIES * FROM foo";
        var select = MsAndGeneric().VerifiedOnlySelect(sql);
        var top = select.Top!;
        Assert.Equal<TopQuantity?>(
            new TopQuantity.Expr(new Expr.Value(
                TestUtils.Number("5").WithEmptySpan())),
            top.Quantity);
        Assert.True(top.WithTies);
    }

    [Fact]
    public void parse_mssql_top_percent_with_ties()
    {
        var sql = "SELECT TOP (10) PERCENT WITH TIES * FROM foo";
        var select = MsAndGeneric().VerifiedOnlySelect(sql);
        var top = select.Top!;
        Assert.Equal<TopQuantity?>(
            new TopQuantity.Expr(new Expr.Value(
                TestUtils.Number("10").WithEmptySpan())),
            top.Quantity);
        Assert.True(top.Percent);
    }

    [Fact]
    public void parse_mssql_top()
    {
        var sql = "SELECT TOP 5 bar, baz FROM foo";
        _ = MsAndGeneric().OneStatementParsesTo(sql, "SELECT TOP 5 bar, baz FROM foo");
    }

    [Fact]
    public void parse_mssql_bin_literal()
    {
        _ = MsAndGeneric().OneStatementParsesTo("SELECT 0xdeadBEEF", "SELECT X'deadBEEF'");
    }

    [Fact]
    public void parse_mssql_create_role()
    {
        var sql = "CREATE ROLE mssql AUTHORIZATION helena";
        switch (Ms().VerifiedStmt(sql))
        {
            case Statement.CreateRole { Value: var createRole }:
                TestUtils.AssertEqVec(new[] { "mssql" }, createRole.Names);
                Assert.Equal(
                    createRole.AuthorizationOwner,
                    ObjectName.From(Sequence.Of(new Ident(Value: "helena", QuoteStyle: null, Span: Span.Empty()))));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_alter_role()
    {
        var sql = "ALTER ROLE old_name WITH NAME = new_name";
        Assert.Equal(
            Ms().ParseSqlStatements(sql),
            Sequence.Of<Statement>(new Statement.AlterRole(
                Name: new Ident(Value: "old_name", QuoteStyle: null, Span: Span.Empty()),
                Operation: new AlterRoleOperation.RenameRole(
                    RoleName: new Ident(Value: "new_name", QuoteStyle: null, Span: Span.Empty())))));

        sql = "ALTER ROLE role_name ADD MEMBER new_member";
        Assert.Equal(
            Ms().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident(Value: "role_name", QuoteStyle: null, Span: Span.Empty()),
                Operation: new AlterRoleOperation.AddMember(
                    MemberName: new Ident(Value: "new_member", QuoteStyle: null, Span: Span.Empty()))));

        sql = "ALTER ROLE role_name DROP MEMBER old_member";
        Assert.Equal(
            Ms().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident(Value: "role_name", QuoteStyle: null, Span: Span.Empty()),
                Operation: new AlterRoleOperation.DropMember(
                    MemberName: new Ident(Value: "old_member", QuoteStyle: null, Span: Span.Empty()))));
    }

    [Fact]
    public void parse_delimited_identifiers()
    {
        // check that quoted identifiers in any position remain quoted after serialization
        var select = MsAndGeneric().VerifiedOnlySelect(
            "SELECT \"alias\".\"bar baz\", \"myfun\"(), \"simple id\" AS \"column alias\" FROM \"a table\" AS \"alias\"");
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
                NullTreatment: null,
                Filter: null,
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
                Assert.Fail("Expected ExprWithAlias");
                break;
        }

        MsAndGeneric().VerifiedStmt("CREATE TABLE \"foo\" (\"bar\" \"int\")");
        MsAndGeneric().VerifiedStmt("ALTER TABLE foo ADD CONSTRAINT \"bar\" PRIMARY KEY (baz)");
        //TODO verified_stmt(r#"UPDATE foo SET "bar" = 5"#);
    }

    [Fact]
    public void parse_table_name_in_square_brackets()
    {
        var select = Ms().VerifiedOnlySelect("SELECT [a column] FROM [a schema].[a table]");
        if (TestUtils.Only(select.From).Relation is TableFactor.Table { Name: var name })
        {
            Assert.Equal(
                ObjectName.From(Sequence.Of(
                    Ident.WithQuote('[', "a schema"),
                    Ident.WithQuote('[', "a table"))),
                name);
        }
        else
        {
            Assert.Fail("Expecting TableFactor::Table");
        }
        Assert.Equal<Expr>(
            new Expr.Identifier(Ident.WithQuote('[', "a column")),
            TestUtils.ExprFromProjection(select.Projection[0]));
    }

    [Fact]
    public void parse_for_clause()
    {
        MsAndGeneric().VerifiedStmt("SELECT a FROM t FOR JSON PATH");
        MsAndGeneric().VerifiedStmt("SELECT b FROM t FOR JSON AUTO");
        MsAndGeneric().VerifiedStmt("SELECT c FROM t FOR JSON AUTO, WITHOUT_ARRAY_WRAPPER");
        MsAndGeneric().VerifiedStmt("SELECT 1 FROM t FOR JSON PATH, ROOT('x'), INCLUDE_NULL_VALUES");
        MsAndGeneric().VerifiedStmt("SELECT 2 FROM t FOR XML AUTO");
        MsAndGeneric().VerifiedStmt("SELECT 3 FROM t FOR XML AUTO, TYPE, ELEMENTS");
        MsAndGeneric().VerifiedStmt("SELECT * FROM t WHERE x FOR XML AUTO, ELEMENTS");
        MsAndGeneric().VerifiedStmt("SELECT x FROM t ORDER BY y FOR XML AUTO, ELEMENTS");
        MsAndGeneric().VerifiedStmt("SELECT y FROM t FOR XML PATH('x'), ROOT('y'), ELEMENTS");
        MsAndGeneric().VerifiedStmt("SELECT z FROM t FOR XML EXPLICIT, BINARY BASE64");
        MsAndGeneric().VerifiedStmt("SELECT * FROM t FOR XML RAW('x')");
        MsAndGeneric().VerifiedStmt("SELECT * FROM t FOR BROWSE");
    }

    [Fact]
    public void dont_parse_trailing_for()
    {
        Assert.Throws<ParserException>(() => Ms()
            .RunParserMethod("SELECT * FROM foo FOR", p => p.ParseQuery()));
    }

    [Fact]
    public void parse_for_json_expect_ast()
    {
        Assert.Equal<ForClause>(
            Ms().VerifiedQuery("SELECT * FROM t FOR JSON PATH, ROOT('root')")
                .ForClause!,
            new ForClause.Json(
                ForJson: new ForJson.Path(),
                Root: "root",
                WithoutArrayWrapper: false,
                IncludeNullValues: false));
    }

    [Fact]
    public void parse_mssql_json_object()
    {
        var select = Ms().VerifiedOnlySelect(
            "SELECT JSON_OBJECT('user_name' : USER_NAME(), LOWER(@id_key) : @id_value, 'sid' : (SELECT @@SPID) ABSENT ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: { Args: var args, Clauses: var clauses } } }:
                Assert.True(
                    args[0] is FunctionArg.ExprNamed
                    {
                        Name: Expr.Value { Inner.Value: Value.SingleQuotedString },
                        Arg: FunctionArgExpr.Expr { Value: Expr.Function },
                        Operator: FunctionArgOperator.Colon,
                    });
                Assert.True(
                    args[1] is FunctionArg.ExprNamed
                    {
                        Name: Expr.Function,
                        Arg: FunctionArgExpr.Expr { Value: Expr.Identifier },
                        Operator: FunctionArgOperator.Colon,
                    });
                Assert.True(
                    args[2] is FunctionArg.ExprNamed
                    {
                        Name: Expr.Value { Inner.Value: Value.SingleQuotedString },
                        Arg: FunctionArgExpr.Expr { Value: Expr.Subquery },
                        Operator: FunctionArgOperator.Colon,
                    });
                Assert.Equal(
                    Sequence.Of<FunctionArgumentClause>(new FunctionArgumentClause.JsonNullClause(
                        new JsonNullClause.AbsentOnNull())),
                    clauses);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        select = Ms().VerifiedOnlySelect(
            "SELECT s.session_id, JSON_OBJECT('security_id' : s.security_id, 'login' : s.login_name, 'status' : s.status) AS info " +
            "FROM sys.dm_exec_sessions AS s " +
            "WHERE s.is_user_process = 1");
        switch (select.Projection[1])
        {
            case SelectItem.ExprWithAlias
            {
                Expr: Expr.Function { Value.Args: FunctionArguments.List { Value.Args: var args } },
            }:
                Assert.True(
                    args[0] is FunctionArg.ExprNamed
                    {
                        Name: Expr.Value { Inner.Value: Value.SingleQuotedString },
                        Arg: FunctionArgExpr.Expr { Value: Expr.CompoundIdentifier },
                        Operator: FunctionArgOperator.Colon,
                    });
                Assert.True(
                    args[1] is FunctionArg.ExprNamed
                    {
                        Name: Expr.Value { Inner.Value: Value.SingleQuotedString },
                        Arg: FunctionArgExpr.Expr { Value: Expr.CompoundIdentifier },
                        Operator: FunctionArgOperator.Colon,
                    });
                Assert.True(
                    args[2] is FunctionArg.ExprNamed
                    {
                        Name: Expr.Value { Inner.Value: Value.SingleQuotedString },
                        Arg: FunctionArgExpr.Expr { Value: Expr.CompoundIdentifier },
                        Operator: FunctionArgOperator.Colon,
                    });
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_mssql_json_array()
    {
        var select = Ms().VerifiedOnlySelect("SELECT JSON_ARRAY('a', 1, NULL, 2 NULL ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: { Args: var args, Clauses: var clauses } } }:
                Assert.Equal(
                    Sequence.Of<FunctionArg>(
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            new Value.SingleQuotedString("a").WithEmptySpan()))),
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            TestUtils.Number("1").WithEmptySpan()))),
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            new Value.Null().WithEmptySpan()))),
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            TestUtils.Number("2").WithEmptySpan())))),
                    args);
                Assert.Equal(
                    Sequence.Of<FunctionArgumentClause>(new FunctionArgumentClause.JsonNullClause(
                        new JsonNullClause.NullOnNull())),
                    clauses);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        select = Ms().VerifiedOnlySelect("SELECT JSON_ARRAY('a', 1, NULL, 2 ABSENT ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: { Args: var args, Clauses: var clauses } } }:
                Assert.Equal(
                    Sequence.Of<FunctionArg>(
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            new Value.SingleQuotedString("a").WithEmptySpan()))),
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            TestUtils.Number("1").WithEmptySpan()))),
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            new Value.Null().WithEmptySpan()))),
                        new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                            TestUtils.Number("2").WithEmptySpan())))),
                    args);
                Assert.Equal(
                    Sequence.Of<FunctionArgumentClause>(new FunctionArgumentClause.JsonNullClause(
                        new JsonNullClause.AbsentOnNull())),
                    clauses);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        select = Ms().VerifiedOnlySelect("SELECT JSON_ARRAY(NULL ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: { Args: var args, Clauses: var clauses } } }:
                Assert.True(args.IsEmpty);
                Assert.Equal(
                    Sequence.Of<FunctionArgumentClause>(new FunctionArgumentClause.JsonNullClause(
                        new JsonNullClause.NullOnNull())),
                    clauses);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        select = Ms().VerifiedOnlySelect("SELECT JSON_ARRAY(ABSENT ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: { Args: var args, Clauses: var clauses } } }:
                Assert.True(args.IsEmpty);
                Assert.Equal(
                    Sequence.Of<FunctionArgumentClause>(new FunctionArgumentClause.JsonNullClause(
                        new JsonNullClause.AbsentOnNull())),
                    clauses);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        select = Ms().VerifiedOnlySelect(
            "SELECT JSON_ARRAY('a', JSON_OBJECT('name' : 'value', 'type' : 1) NULL ON NULL)");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value: { Args: var args, Clauses: var clauses } } }:
                Assert.Equal<FunctionArg>(
                    new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                        new Value.SingleQuotedString("a").WithEmptySpan()))),
                    args[0]);
                Assert.True(
                    args[1] is FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.Function } });
                Assert.Equal(
                    Sequence.Of<FunctionArgumentClause>(new FunctionArgumentClause.JsonNullClause(
                        new JsonNullClause.NullOnNull())),
                    clauses);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        select = Ms().VerifiedOnlySelect(
            "SELECT JSON_ARRAY('a', JSON_OBJECT('name' : 'value', 'type' : 1), JSON_ARRAY(1, NULL, 2 NULL ON NULL))");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value.Args: var args } }:
                Assert.Equal<FunctionArg>(
                    new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                        new Value.SingleQuotedString("a").WithEmptySpan()))),
                    args[0]);
                Assert.True(
                    args[1] is FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.Function } });
                Assert.True(
                    args[2] is FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.Function } });
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        select = Ms().VerifiedOnlySelect("SELECT JSON_ARRAY(1, @id_value, (SELECT @@SPID))");
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value.Args: var args } }:
                Assert.Equal<FunctionArg>(
                    new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Value(
                        TestUtils.Number("1").WithEmptySpan()))),
                    args[0]);
                Assert.True(
                    args[1] is FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.Identifier } });
                Assert.True(
                    args[2] is FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.Subquery } });
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
        select = Ms().VerifiedOnlySelect(
            "SELECT s.session_id, JSON_ARRAY(s.host_name, s.program_name, s.client_interface_name NULL ON NULL) AS info " +
            "FROM sys.dm_exec_sessions AS s " +
            "WHERE s.is_user_process = 1");
        switch (select.Projection[1])
        {
            case SelectItem.ExprWithAlias
            {
                Expr: Expr.Function { Value.Args: FunctionArguments.List { Value: { Args: var args, Clauses: var clauses } } },
            }:
                Assert.True(
                    args[0] is FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.CompoundIdentifier } });
                Assert.True(
                    args[1] is FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.CompoundIdentifier } });
                Assert.True(
                    args[2] is FunctionArg.Unnamed { Value: FunctionArgExpr.Expr { Value: Expr.CompoundIdentifier } });
                Assert.Equal(
                    Sequence.Of<FunctionArgumentClause>(new FunctionArgumentClause.JsonNullClause(
                        new JsonNullClause.NullOnNull())),
                    clauses);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_ampersand_arobase()
    {
        // In SQL Server, a&@b means (a) & (@b), in PostgreSQL it means (a) &@ (b)
        Ms().ExprParsesTo("a&@b", "a & @b");
    }

    [Fact]
    public void parse_cast_varchar_max()
    {
        MsAndGeneric().VerifiedExpr("CAST('foo' AS VARCHAR(MAX))");
        MsAndGeneric().VerifiedExpr("CAST('foo' AS NVARCHAR(MAX))");
    }

    [Fact]
    public void parse_convert()
    {
        var sql = "CONVERT(INT, 1, 2, 3, NULL)";
        if (Ms().VerifiedExpr(sql) is not Expr.Convert
            {
                IsTry: var isTry,
                Expr: var expr,
                DataType: var dataType,
                Charset: var charset,
                TargetBeforeValue: var targetBeforeValue,
                Styles: var styles,
            })
        {
            throw new InvalidOperationException("unreachable");
        }
        Assert.False(isTry);
        Assert.Equal(Expr.MakeValue(TestUtils.Number("1")), expr);
        Assert.Equal<DataType?>(new DataType.Int(null), dataType);
        Assert.True(charset is null);
        Assert.True(targetBeforeValue);
        Assert.Equal(
            Sequence.Of(
                Expr.MakeValue(TestUtils.Number("2")),
                Expr.MakeValue(TestUtils.Number("3")),
                new Expr.Value(new Value.Null().WithEmptySpan())),
            styles);

        Ms().VerifiedExpr("CONVERT(VARCHAR(MAX), 'foo')");
        Ms().VerifiedExpr("CONVERT(VARCHAR(10), 'foo')");
        Ms().VerifiedExpr("CONVERT(DECIMAL(10,5), 12.55)");

        var errorSql = "SELECT CONVERT(INT, 'foo',) FROM T";
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: an expression, found: )"),
            Assert.Throws<ParserException>(() => Ms().ParseSqlStatements(errorSql)).Error);
    }

    [Fact]
    public void parse_substring_in_select()
    {
        var sql = "SELECT DISTINCT SUBSTRING(description, 0, 1) FROM test";
        switch (Ms().OneStatementParsesTo(
            sql,
            "SELECT DISTINCT SUBSTRING(description, 0, 1) FROM test"))
        {
            case Statement.Query { Value: var query }:
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
                                Expr: new Expr.Identifier(new Ident(
                                    Value: "description",
                                    QuoteStyle: null,
                                    Span: Span.Empty())),
                                SubstringFrom: new Expr.Value(
                                    TestUtils.Number("0").WithEmptySpan()),
                                SubstringFor: new Expr.Value(
                                    TestUtils.Number("1").WithEmptySpan()),
                                Special: true,
                                Shorthand: false))),
                            Exclude: null,
                            Into: null,
                            From: Sequence.Of(new TableWithJoins(
                                Relation: TestUtils.TableFromName(ObjectName.From(Sequence.Of(new Ident(
                                    Value: "test",
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
                    query);
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_mssql_declare()
    {
        var sql = "DECLARE @foo CURSOR, @bar INT, @baz AS TEXT = 'foobar';";
        var ast = Ms().ParseSqlStatements(sql);

        Assert.Equal(
            Sequence.Of<Statement>(new Statement.Declare(
                Stmts: Sequence.Of(
                    new Declare(
                        Names: Sequence.Of(new Ident(
                            Value: "@foo",
                            QuoteStyle: null,
                            Span: Span.Empty())),
                        DataType: null,
                        Assignment: null,
                        DeclareType: new DeclareType.Cursor(),
                        Binary: null,
                        Sensitive: null,
                        Scroll: null,
                        Hold: null,
                        ForQuery: null),
                    new Declare(
                        Names: Sequence.Of(new Ident(
                            Value: "@bar",
                            QuoteStyle: null,
                            Span: Span.Empty())),
                        DataType: new DataType.Int(null),
                        Assignment: null,
                        DeclareType: null,
                        Binary: null,
                        Sensitive: null,
                        Scroll: null,
                        Hold: null,
                        ForQuery: null),
                    new Declare(
                        Names: Sequence.Of(new Ident(
                            Value: "@baz",
                            QuoteStyle: null,
                            Span: Span.Empty())),
                        DataType: new DataType.Text(),
                        Assignment: new DeclareAssignment.MsSqlAssignment(new Expr.Value(
                            new Value.SingleQuotedString("foobar").WithEmptySpan())),
                        DeclareType: null,
                        Binary: null,
                        Sensitive: null,
                        Scroll: null,
                        Hold: null,
                        ForQuery: null)))),
            ast);

        sql = "DECLARE @bar INT;SET @bar = 2;SELECT @bar * 4";
        ast = Ms().ParseSqlStatements(sql);
        Assert.Equal(
            Sequence.Of<Statement>(
                new Statement.Declare(
                    Stmts: Sequence.Of(new Declare(
                        Names: Sequence.Of(new Ident("@bar")),
                        DataType: new DataType.Int(null),
                        Assignment: null,
                        DeclareType: null,
                        Binary: null,
                        Sensitive: null,
                        Scroll: null,
                        Hold: null,
                        ForQuery: null))),
                new Statement.Set(new Set.SingleAssignment(
                    Scope: null,
                    Hivevar: false,
                    Variable: ObjectName.From(Sequence.Of(new Ident("@bar"))),
                    Values: Sequence.Of<Expr>(new Expr.Value(
                        new Value.Number("2", false).WithEmptySpan())))),
                new Statement.Query(new Query(
                    With: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    OrderBy: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty,

                    Body: new SetExpr.Select(new Select(
                        SelectToken: AttachedToken.Empty(),
                        OptimizerHints: Sequence<OptimizerHint>.Empty,
                        Distinct: null,
                        SelectModifiers: null,
                        Top: null,
                        TopBeforeDistinct: false,
                        Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                            Left: new Expr.Identifier(new Ident("@bar")),
                            Op: new BinaryOperator.Multiply(),
                            Right: new Expr.Value(
                                new Value.Number("4", false).WithEmptySpan())))),
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
                        WindowBeforeQualify: false,
                        Qualify: null,
                        ValueTableMode: null,
                        ConnectBy: Sequence<ConnectByKind>.Empty,
                        Flavor: SelectFlavor.Standard))))),
            ast);

        var declareCursorForSelect =
            "DECLARE vend_cursor CURSOR FOR SELECT * FROM Purchasing.Vendor";
        _ = Ms().VerifiedStmt(declareCursorForSelect);
    }

    [Fact]
    public void test_mssql_cursor()
    {
        var fullCursorUsage =
            "DECLARE Employee_Cursor CURSOR FOR " +
            "SELECT LastName, FirstName " +
            "FROM AdventureWorks2022.HumanResources.vEmployee " +
            "WHERE LastName LIKE 'B%'; " +
            "" +
            "OPEN Employee_Cursor; " +
            "" +
            "FETCH NEXT FROM Employee_Cursor; " +
            "" +
            "WHILE @@FETCH_STATUS = 0 " +
            "BEGIN " +
            "FETCH NEXT FROM Employee_Cursor; " +
            "END; " +
            "" +
            "CLOSE Employee_Cursor; " +
            "DEALLOCATE Employee_Cursor";
        _ = Ms().StatementsParseTo(fullCursorUsage, "");
    }

    [Fact]
    public void test_mssql_while_statement()
    {
        var whileSingleStatement = "WHILE 1 = 0 PRINT 'Hello World';";
        var stmt = Ms().VerifiedStmt(whileSingleStatement);
        Assert.Equal(
            stmt,
            new Statement.While(new WhileStatement(
                WhileBlock: new ConditionalStatementBlock(
                    StartToken: new AttachedToken(new TokenWithSpan(
                        Token: new Token.Word(new Word(
                            Value: "WHILE",
                            QuoteStyle: null,
                            Keyword: Keyword.WHILE)),
                        Span: Span.Empty())),
                    Condition: new Expr.BinaryOp(
                        Left: new Expr.Value(
                            new Value.Number("1", false).WithEmptySpan()),
                        Op: new BinaryOperator.Eq(),
                        Right: new Expr.Value(
                            new Value.Number("0", false).WithEmptySpan())),
                    ThenToken: null,
                    ConditionalStatements: new ConditionalStatements.Sequence(
                        Statements: Sequence.Of<Statement>(new Statement.Print(new PrintStatement(
                            Message: new Expr.Value(
                                new Value.SingleQuotedString("Hello World")
                                    .WithEmptySpan())))))))));

        var whileBeginEnd =
            "WHILE @@FETCH_STATUS = 0 " +
            "BEGIN " +
            "FETCH NEXT FROM Employee_Cursor; " +
            "END";
        _ = Ms().VerifiedStmt(whileBeginEnd);

        var whileBeginEndMultipleStatements =
            "WHILE @@FETCH_STATUS = 0 " +
            "BEGIN " +
            "FETCH NEXT FROM Employee_Cursor; " +
            "PRINT 'Hello World'; " +
            "END";
        _ = Ms().VerifiedStmt(whileBeginEndMultipleStatements);
    }

    [Fact]
    public void test_parse_raiserror()
    {
        var sql = "RAISERROR('This is a test', 16, 1)";
        var s = Ms().VerifiedStmt(sql);
        Assert.Equal(
            s,
            new Statement.RaisError(
                Message: new Expr.Value(
                    new Value.SingleQuotedString("This is a test").WithEmptySpan()),
                Severity: new Expr.Value(
                    new Value.Number("16", false).WithEmptySpan()),
                State: new Expr.Value(
                    new Value.Number("1", false).WithEmptySpan()),
                Arguments: Sequence<Expr>.Empty,
                Options: Sequence<RaisErrorOption>.Empty));

        sql = "RAISERROR('This is a test', 16, 1) WITH NOWAIT";
        _ = Ms().VerifiedStmt(sql);

        sql = "RAISERROR('This is a test', 16, 1, 'ARG') WITH SETERROR, LOG";
        _ = Ms().VerifiedStmt(sql);

        sql = "RAISERROR(N'This is message %s %d.', 10, 1, N'number', 5)";
        _ = Ms().VerifiedStmt(sql);

        sql = "RAISERROR(N'<<%*.*s>>', 10, 1, 7, 3, N'abcde')";
        _ = Ms().VerifiedStmt(sql);

        sql = "RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState)";
        _ = Ms().VerifiedStmt(sql);
    }

    [Fact]
    public void test_parse_throw()
    {
        // THROW with arguments
        var sql = "THROW 51000, 'Record does not exist.', 1";
        var s = Ms().VerifiedStmt(sql);
        Assert.Equal(
            s,
            new Statement.Throw(new ThrowStatement(
                ErrorNumber: new Expr.Value(
                    new Value.Number("51000", false).WithEmptySpan()),
                Message: new Expr.Value(
                    new Value.SingleQuotedString("Record does not exist.").WithEmptySpan()),
                State: new Expr.Value(
                    new Value.Number("1", false).WithEmptySpan()))));

        // THROW with variable references
        sql = "THROW @ErrorNumber, @ErrorMessage, @ErrorState";
        _ = Ms().VerifiedStmt(sql);

        // Re-throw (no arguments)
        sql = "THROW";
        s = Ms().VerifiedStmt(sql);
        Assert.Equal(
            s,
            new Statement.Throw(new ThrowStatement(
                ErrorNumber: null,
                Message: null,
                State: null)));
    }

    [Fact]
    public void test_parse_waitfor()
    {
        // WAITFOR DELAY
        var sql = "WAITFOR DELAY '00:00:05'";
        var stmt = MsAndGeneric().VerifiedStmt(sql);
        Assert.Equal(
            stmt,
            new Statement.WaitFor(new WaitForStatement(
                WaitType: new WaitForType.Delay(),
                Expr: new Expr.Value(
                    new Value.SingleQuotedString("00:00:05").WithEmptySpan()))));

        // WAITFOR TIME
        sql = "WAITFOR TIME '14:30:00'";
        stmt = MsAndGeneric().VerifiedStmt(sql);
        Assert.Equal(
            stmt,
            new Statement.WaitFor(new WaitForStatement(
                WaitType: new WaitForType.Time(),
                Expr: new Expr.Value(
                    new Value.SingleQuotedString("14:30:00").WithEmptySpan()))));

        // WAITFOR DELAY with variable
        sql = "WAITFOR DELAY @WaitTime";
        _ = MsAndGeneric().VerifiedStmt(sql);

        // Error: WAITFOR without DELAY or TIME
        Assert.Throws<ParserException>(() => MsAndGeneric().ParseSqlStatements("WAITFOR '00:00:05'"));
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
        var quoteStyles = new[] { '\'', '"' };
        foreach (var objectName in validObjectNames)
        {
            // Test single identifier without quotes
            Assert.Equal(
                Ms().VerifiedStmt($"USE {objectName}"),
                new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(new Ident(
                    objectName))))));
            foreach (var quote in quoteStyles)
            {
                // Test single identifier with different type of quotes
                Assert.Equal(
                    Ms().VerifiedStmt($"USE {quote}{objectName}{quote}"),
                    new Statement.Use(new Use.Object(ObjectName.From(Sequence.Of(Ident.WithQuote(
                        quote,
                        objectName))))));
            }
        }
    }

    [Fact]
    public void parse_create_table_with_valid_options()
    {
        var options = new (string Sql, Sequence<SqlOption> WithOptions)[]
        {
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (DISTRIBUTION = ROUND_ROBIN, PARTITION (column_a RANGE FOR VALUES (10, 11)))",
                Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: new Ident(
                            Value: "DISTRIBUTION",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        Value: new Expr.Identifier(new Ident(
                            Value: "ROUND_ROBIN",
                            QuoteStyle: null,
                            Span: Span.Empty()))),
                    new SqlOption.Partition(
                        ColumnName: new Ident("column_a"),
                        RangeDirection: null,
                        ForValues: Sequence.Of<Expr>(new Expr.Value(TestUtils.Number("10").WithEmptySpan()), new Expr.Value(TestUtils.Number("11").WithEmptySpan()))))
            ),
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (PARTITION (column_a RANGE LEFT FOR VALUES (10, 11)))",
                Sequence.Of<SqlOption>(
                    new SqlOption.Partition(
                        ColumnName: new Ident("column_a"),
                        RangeDirection: PartitionRangeDirection.Left,
                        ForValues: Sequence.Of<Expr>(
                            new Expr.Value(TestUtils.Number("10").WithEmptySpan()),
                            new Expr.Value(TestUtils.Number("11").WithEmptySpan()))))
            ),
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (CLUSTERED COLUMNSTORE INDEX)",
                Sequence.Of<SqlOption>(new SqlOption.Clustered(new TableOptionsClustered.ColumnstoreIndex()))
            ),
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (CLUSTERED COLUMNSTORE INDEX ORDER (column_a, column_b))",
                Sequence.Of<SqlOption>(
                    new SqlOption.Clustered(new TableOptionsClustered.ColumnstoreIndexOrder(Sequence.Of(
                        new Ident("column_a"),
                        new Ident("column_b")))))
            ),
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (CLUSTERED INDEX (column_a ASC, column_b DESC, column_c))",
                Sequence.Of<SqlOption>(
                    new SqlOption.Clustered(new TableOptionsClustered.Index(Sequence.Of(
                        new ClusteredIndex(
                            Name: new Ident(
                                Value: "column_a",
                                QuoteStyle: null,
                                Span: Span.Empty()),
                            Asc: true),
                        new ClusteredIndex(
                            Name: new Ident(
                                Value: "column_b",
                                QuoteStyle: null,
                                Span: Span.Empty()),
                            Asc: false),
                        new ClusteredIndex(
                            Name: new Ident(
                                Value: "column_c",
                                QuoteStyle: null,
                                Span: Span.Empty()),
                            Asc: null)))))
            ),
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (DISTRIBUTION = HASH(column_a, column_b), HEAP)",
                Sequence.Of<SqlOption>(
                    new SqlOption.KeyValue(
                        Key: new Ident(
                            Value: "DISTRIBUTION",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        Value: new Expr.Function(
                            new Function(
                                Name: ObjectName.From(
                                    Sequence.Of(
                                        new Ident(
                                            Value: "HASH",
                                            QuoteStyle: null,
                                            Span: Span.Empty()))),
                                UsesOdbcSyntax: false,
                                Parameters: new FunctionArguments.None(),
                                Args: new FunctionArguments.List(
                                    new FunctionArgumentList(
                                        DuplicateTreatment: null,
                                        Args: Sequence.Of<FunctionArg>(
                                            new FunctionArg.Unnamed(
                                                new FunctionArgExpr.Expr(
                                                    new Expr.Identifier(
                                                        new Ident(
                                                            Value: "column_a",
                                                            QuoteStyle: null,
                                                            Span: Span.Empty())))),
                                            new FunctionArg.Unnamed(
                                                new FunctionArgExpr.Expr(
                                                    new Expr.Identifier(
                                                        new Ident(
                                                            Value: "column_b",
                                                            QuoteStyle: null,
                                                            Span: Span.Empty()))))),
                                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                                Filter: null,
                                NullTreatment: null,
                                Over: null,
                                WithinGroup: Sequence<OrderByExpr>.Empty))),
                    new SqlOption.Ident(new Ident("HEAP")))
            ),
        };

        foreach (var (sql, withOptions) in options)
        {
            Assert.Equal(
                MsAndGeneric().VerifiedStmt(sql),
                new Statement.CreateTable(new CreateTable(
                    OrReplace: false,
                    Temporary: false,
                    Unlogged: false,
                    External: false,
                    Global: null,
                    Dynamic: false,
                    IfNotExists: false,
                    Transient: false,
                    Volatile: false,
                    Name: ObjectName.From(Sequence.Of(new Ident(
                        Value: "mytable",
                        QuoteStyle: null,
                        Span: Span.Empty()))),
                    Columns: Sequence.Of(
                        new ColumnDef(
                            Name: new Ident(
                                Value: "column_a",
                                QuoteStyle: null,
                                Span: Span.Empty()),
                            DataType: new DataType.Int(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident(
                                Value: "column_b",
                                QuoteStyle: null,
                                Span: Span.Empty()),
                            DataType: new DataType.Int(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: new Ident(
                                Value: "column_c",
                                QuoteStyle: null,
                                Span: Span.Empty()),
                            DataType: new DataType.Int(null),
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
                    Iceberg: false,
                    Snapshot: false,
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
                    TableOptions: new CreateTableOptions.With(withOptions),
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
                    WithData: null)));
        }
    }

    [Fact]
    public void parse_nested_slash_star_comment()
    {
        var sql =
            "\n" +
            "    select\n" +
            "    /*\n" +
            "       comment level 1\n" +
            "       /*\n" +
            "          comment level 2\n" +
            "       */\n" +
            "    */\n" +
            "    1;\n" +
            "    ";
        var canonical = "SELECT 1";
        Ms().OneStatementParsesTo(sql, canonical);
    }

    [Fact]
    public void parse_create_table_with_invalid_options()
    {
        var invalidCases = new List<(string, string)>
        {
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (CLUSTERED COLUMNSTORE INDEX ORDER ())",
                "Expected: identifier, found: )"
            ),
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (CLUSTERED COLUMNSTORE)",
                "invalid CLUSTERED sequence"
            ),
            (
                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (HEAP INDEX)",
                "Expected: ), found: INDEX"
            ),
            (

                "CREATE TABLE mytable (column_a INT, column_b INT, column_c INT) WITH (PARTITION (RANGE LEFT FOR VALUES (10, 11)))",
                "Expected: RANGE, found: LEFT"
            ),
        };

        foreach (var (sql, expectedError) in invalidCases)
        {
            var res = Assert.Throws<ParserException>(() => MsAndGeneric().ParseSqlStatements(sql));
            Assert.Equal(
                $"sql parser error: {expectedError}",
                res.Error.ToString());
        }
    }

    [Fact]
    public void parse_create_table_with_identity_column()
    {
        var withColumnOptions = new (string Sql, Sequence<ColumnOptionDef> ColumnOptions)[]
        {
            (
                "CREATE TABLE mytable (columnA INT IDENTITY NOT NULL)",
                Sequence.Of(
                    new ColumnOptionDef(
                        Name: null,
                        Option: new ColumnOption.Identity(new IdentityPropertyKind.Identity(
                            new IdentityProperty(
                                Parameters: null,
                                Order: null)))),
                    new ColumnOptionDef(
                        Name: null,
                        Option: new ColumnOption.NotNull()))
            ),
            (
                "CREATE TABLE mytable (columnA INT IDENTITY(1, 1) NOT NULL)",
                Sequence.Of(
                    new ColumnOptionDef(
                        Name: null,
                        Option: new ColumnOption.Identity(new IdentityPropertyKind.Identity(
                            new IdentityProperty(
                                Parameters: new IdentityPropertyFormatKind.FunctionCall(
                                    new IdentityParameters(
                                        Seed: Expr.MakeValue(TestUtils.Number("1")),
                                        Increment: Expr.MakeValue(TestUtils.Number("1")))),
                                Order: null)))),
                    new ColumnOptionDef(
                        Name: null,
                        Option: new ColumnOption.NotNull()))
            ),
        };

        foreach (var (sql, columnOptions) in withColumnOptions)
        {
            Assert.Equal(
                MsAndGeneric().VerifiedStmt(sql),
                new Statement.CreateTable(new CreateTable(
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
                    Name: ObjectName.From(Sequence.Of(new Ident(
                        Value: "mytable",
                        QuoteStyle: null,
                        Span: Span.Empty()))),
                    Columns: Sequence.Of(new ColumnDef(
                        Name: new Ident(
                            Value: "columnA",
                            QuoteStyle: null,
                            Span: Span.Empty()),
                        DataType: new DataType.Int(null),

                        Options: columnOptions)),
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
                    WithData: null)));
        }
    }

    [Fact]
    public void parse_true_false_as_identifiers()
    {
        Assert.Equal<Expr>(
            Ms().VerifiedExpr("true"),
            new Expr.Identifier(new Ident("true")));
        Assert.Equal<Expr>(
            Ms().VerifiedExpr("false"),
            new Expr.Identifier(new Ident("false")));
    }

    [Fact]
    public void parse_mssql_set_session_value()
    {
        Ms().VerifiedStmt(
            "SET OFFSETS SELECT, FROM, ORDER, TABLE, PROCEDURE, STATEMENT, PARAM, EXECUTE ON");
        Ms().VerifiedStmt("SET IDENTITY_INSERT dbo.Tool ON");
        Ms().VerifiedStmt("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED");
        Ms().VerifiedStmt("SET TRANSACTION ISOLATION LEVEL READ COMMITTED");
        Ms().VerifiedStmt("SET TRANSACTION ISOLATION LEVEL REPEATABLE READ");
        Ms().VerifiedStmt("SET TRANSACTION ISOLATION LEVEL SNAPSHOT");
        Ms().VerifiedStmt("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE");
        Ms().VerifiedStmt("SET STATISTICS IO ON");
        Ms().VerifiedStmt("SET STATISTICS XML ON");
        Ms().VerifiedStmt("SET STATISTICS PROFILE ON");
        Ms().VerifiedStmt("SET STATISTICS TIME ON");
        Ms().VerifiedStmt("SET DATEFIRST 7");
        Ms().VerifiedStmt("SET DATEFIRST @xxx");
        Ms().VerifiedStmt("SET DATEFIRST @@xxx");
        Ms().VerifiedStmt("SET DATEFORMAT dmy");
        Ms().VerifiedStmt("SET DATEFORMAT @datevar");
        Ms().VerifiedStmt("SET DATEFORMAT @@datevar");
        Ms().VerifiedStmt("SET DEADLOCK_PRIORITY 'LOW'");
        Ms().VerifiedStmt("SET DEADLOCK_PRIORITY LOW");
        Ms().VerifiedStmt("SET DEADLOCK_PRIORITY 8");
        Ms().VerifiedStmt("SET DEADLOCK_PRIORITY -8");
        Ms().VerifiedStmt("SET DEADLOCK_PRIORITY @xxx");
        Ms().VerifiedStmt("SET DEADLOCK_PRIORITY @@xxx");
        Ms().VerifiedStmt("SET LOCK_TIMEOUT 1800");
        Ms().VerifiedStmt("SET CONCAT_NULL_YIELDS_NULL ON");
        Ms().VerifiedStmt("SET CURSOR_CLOSE_ON_COMMIT ON");
        Ms().VerifiedStmt("SET FIPS_FLAGGER 'level'");
        Ms().VerifiedStmt("SET FIPS_FLAGGER OFF");
        Ms().VerifiedStmt("SET LANGUAGE Italian");
        Ms().VerifiedStmt("SET QUOTED_IDENTIFIER ON");
        Ms().VerifiedStmt("SET ARITHABORT ON");
        Ms().VerifiedStmt("SET ARITHIGNORE OFF");
        Ms().VerifiedStmt("SET FMTONLY ON");
        Ms().VerifiedStmt("SET NOCOUNT OFF");
        Ms().VerifiedStmt("SET NOEXEC ON");
        Ms().VerifiedStmt("SET NUMERIC_ROUNDABORT ON");
        Ms().VerifiedStmt("SET QUERY_GOVERNOR_COST_LIMIT 11");
        Ms().VerifiedStmt("SET ROWCOUNT 4");
        Ms().VerifiedStmt("SET ROWCOUNT @xxx");
        Ms().VerifiedStmt("SET ROWCOUNT @@xxx");
        Ms().VerifiedStmt("SET TEXTSIZE 11");
        Ms().VerifiedStmt("SET ANSI_DEFAULTS ON");
        Ms().VerifiedStmt("SET ANSI_NULL_DFLT_OFF ON");
        Ms().VerifiedStmt("SET ANSI_NULL_DFLT_ON ON");
        Ms().VerifiedStmt("SET ANSI_NULLS ON");
        Ms().VerifiedStmt("SET ANSI_PADDING ON");
        Ms().VerifiedStmt("SET ANSI_WARNINGS ON");
        Ms().VerifiedStmt("SET FORCEPLAN ON");
        Ms().VerifiedStmt("SET SHOWPLAN_ALL ON");
        Ms().VerifiedStmt("SET SHOWPLAN_TEXT ON");
        Ms().VerifiedStmt("SET SHOWPLAN_XML ON");
        Ms().VerifiedStmt("SET IMPLICIT_TRANSACTIONS ON");
        Ms().VerifiedStmt("SET REMOTE_PROC_TRANSACTIONS ON");
        Ms().VerifiedStmt("SET XACT_ABORT ON");
        Ms().VerifiedStmt("SET ANSI_NULLS, ANSI_PADDING ON");
    }

    [Fact]
    public void parse_mssql_if_else()
    {
        // Simple statements and blocks
        Ms().VerifiedStmt("IF 1 = 1 SELECT '1'; ELSE SELECT '2';");
        Ms().VerifiedStmt("IF 1 = 1 BEGIN SET @A = 1; END ELSE SET @A = 2;");
        Ms().VerifiedStmt(
            "IF DATENAME(weekday, GETDATE()) IN (N'Saturday', N'Sunday') SELECT 'Weekend'; ELSE SELECT 'Weekday';");
        Ms().VerifiedStmt(
            "IF (SELECT COUNT(*) FROM a.b WHERE c LIKE 'x%') > 1 SELECT 'yes'; ELSE SELECT 'No';");

        // Multiple statements
        var stmts = Ms()
            .ParseSqlStatements("DECLARE @A INT; IF 1=1 BEGIN SET @A = 1 END ELSE SET @A = 2");
        if (stmts.Count == 2 && stmts[0] is Statement.Declare && stmts[1] is Statement.If { Value: var stmt })
        {
            Assert.Equal(
                "IF 1 = 1 BEGIN SET @A = 1; END ELSE SET @A = 2;",
                stmt.ToString());
        }
        else
        {
            Assert.Fail($"Unexpected statements: {string.Join(", ", stmts)}");
        }
    }

    [Fact]
    public void test_mssql_if_else_span()
    {
        var sql = "IF 1 = 1 SELECT '1' ELSE SELECT '2'";
        var parser = new Parser(new MsSqlDialect()).TryWithSql(sql);
        Assert.Equal(
            parser.ParseStatement().Span(),
            new Span(new Location(1, 1), new Location(1, sql.Length + 1)));
    }

    [Fact]
    public void test_mssql_if_else_multiline_span()
    {
        var sqlLine1 = "IF 1 = 1";
        var sqlLine2 = "SELECT '1'";
        var sqlLine3 = "ELSE SELECT '2'";
        var sql = string.Join("\n", new[] { sqlLine1, sqlLine2, sqlLine3 });
        var parser = new Parser(new MsSqlDialect()).TryWithSql(sql);
        Assert.Equal(
            parser.ParseStatement().Span(),
            new Span(
                new Location(1, 1),
                new Location(3, sqlLine3.Length + 1)));
    }

    [Fact]
    public void test_mssql_if_statements_span()
    {
        // Simple statements
        var sql = "IF 1 = 1 SELECT '1' ELSE SELECT '2'";
        var parser = new Parser(new MsSqlDialect()).TryWithSql(sql);
        switch (parser.ParseStatement())
        {
            case Statement.If { Value: { IfBlock: var ifBlock, ElseBlock: { } elseBlock } }:
                Assert.Equal(
                    ifBlock.Span(),
                    new Span(new Location(1, 1), new Location(1, 20)));
                Assert.Equal(
                    elseBlock.Span(),
                    new Span(new Location(1, 21), new Location(1, 36)));
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }

        // Blocks
        sql = "IF 1 = 1 BEGIN SET @A = 1; END ELSE BEGIN SET @A = 2 END";
        parser = new Parser(new MsSqlDialect()).TryWithSql(sql);
        switch (parser.ParseStatement())
        {
            case Statement.If { Value: { IfBlock: var ifBlock, ElseBlock: { } elseBlock } }:
                Assert.Equal(
                    ifBlock.Span(),
                    new Span(new Location(1, 1), new Location(1, 31)));
                Assert.Equal(
                    elseBlock.Span(),
                    new Span(new Location(1, 32), new Location(1, 57)));
                break;
            case var stmt:
                Assert.Fail($"Unexpected statement: {stmt}");
                break;
        }
    }

    [Fact]
    public void parse_mssql_varbinary_max_length()
    {
        var sql = "CREATE TABLE example (var_binary_col VARBINARY(MAX))";

        switch (MsAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, Columns: var columns } }:
                Assert.Equal(
                    name,
                    ObjectName.From(Sequence.Of(new Ident(
                        Value: "example",
                        QuoteStyle: null,
                        Span: Span.Empty()))));
                Assert.Equal(
                    columns,
                    Sequence.Of(new ColumnDef(
                        Name: new Ident("var_binary_col"),
                        DataType: new DataType.Varbinary(new BinaryLength.Max()),

                        Options: Sequence<ColumnOptionDef>.Empty)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }

        sql = "CREATE TABLE example (var_binary_col VARBINARY(50))";

        switch (MsAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: { Name: var name, Columns: var columns } }:
                Assert.Equal(
                    name,
                    ObjectName.From(Sequence.Of(new Ident(
                        Value: "example",
                        QuoteStyle: null,
                        Span: Span.Empty()))));
                Assert.Equal(
                    columns,
                    Sequence.Of(new ColumnDef(
                        Name: new Ident("var_binary_col"),
                        DataType: new DataType.Varbinary(new BinaryLength.IntegerLength(Length: 50)),

                        Options: Sequence<ColumnOptionDef>.Empty)));
                break;
            default:
                throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public void parse_mssql_table_identifier_with_default_schema()
    {
        Ms().VerifiedStmt("SELECT * FROM mydatabase..MyTable");
    }

    private static TestedDialects Ms()
    {
        return new TestedDialects(new List<Dialect> { new MsSqlDialect() });
    }

    // MS SQL dialect with support for optional semi-colon statement delimiters
    private static TestedDialects Tsql()
    {
        return TestedDialects.NewWithOptions(
            new List<Dialect> { new MsSqlDialect() },
            new ParserOptions(
                TrailingCommas: false,
                Unescape: true,
                RequireSemicolonStmtDelimiter: false));
    }

    private static TestedDialects MsAndGeneric()
    {
        return new TestedDialects(new List<Dialect> { new MsSqlDialect(), new GenericDialect() });
    }

    [Fact]
    public void parse_mssql_merge_with_output()
    {
        var stmt = "MERGE dso.products AS t " +
            "USING dsi.products AS " +
            "s ON s.ProductID = t.ProductID " +
            "WHEN MATCHED AND " +
            "NOT (t.ProductName = s.ProductName OR (ISNULL(t.ProductName, s.ProductName) IS NULL)) " +
            "THEN UPDATE SET t.ProductName = s.ProductName " +
            "WHEN NOT MATCHED BY TARGET " +
            "THEN INSERT (ProductID, ProductName) " +
            "VALUES (s.ProductID, s.ProductName) " +
            "WHEN NOT MATCHED BY SOURCE THEN DELETE " +
            "OUTPUT $action, deleted.ProductID INTO dsi.temp_products";
        MsAndGeneric().VerifiedStmt(stmt);
    }

    [Fact]
    public void parse_create_trigger()
    {
        var createTrigger =
            "CREATE OR ALTER TRIGGER reminder1 " +
            "ON Sales.Customer " +
            "AFTER INSERT, UPDATE " +
            "AS RAISERROR('Notify Customer Relations', 16, 10);";
        var createStmt = Ms().VerifiedStmt(createTrigger);
        Assert.Equal(
            createStmt,
            new Statement.CreateTrigger(new CreateTrigger(
                OrAlter: true,
                Temporary: false,
                OrReplace: false,
                IsConstraint: false,
                Name: ObjectName.From(Sequence.Of(new Ident("reminder1"))),
                Period: new TriggerPeriod.After(),
                PeriodBeforeTable: false,
                Events: Sequence.Of<TriggerEvent>(new TriggerEvent.Insert(), new TriggerEvent.Update(Sequence<Ident>.Empty)),
                TableName: ObjectName.From(Sequence.Of(new Ident("Sales"), new Ident("Customer"))),
                ReferencedTableName: null,
                Referencing: Sequence<TriggerReferencing>.Empty,
                TriggerObject: null,
                Condition: null,
                ExecBody: null,
                StatementsAs: true,
                Statements: new ConditionalStatements.Sequence(
                    Statements: Sequence.Of<Statement>(new Statement.RaisError(
                        Message: new Expr.Value(
                            new Value.SingleQuotedString("Notify Customer Relations")
                                .WithEmptySpan()),
                        Severity: new Expr.Value(
                            new Value.Number("16", false).WithEmptySpan()),
                        State: new Expr.Value(
                            new Value.Number("10", false).WithEmptySpan()),
                        Arguments: Sequence<Expr>.Empty,
                        Options: Sequence<RaisErrorOption>.Empty))),
                Characteristics: null)));

        var multiStatementAsTrigger =
            "CREATE TRIGGER some_trigger ON some_table FOR INSERT " +
            "AS " +
            "DECLARE @var INT; " +
            "RAISERROR('Trigger fired', 10, 1);";
        _ = Ms().VerifiedStmt(multiStatementAsTrigger);

        var multiStatementTrigger =
            "CREATE TRIGGER some_trigger ON some_table FOR INSERT " +
            "AS " +
            "BEGIN " +
            "DECLARE @var INT; " +
            "RAISERROR('Trigger fired', 10, 1); " +
            "END";
        _ = Ms().VerifiedStmt(multiStatementTrigger);

        var createTriggerWithReturn =
            "CREATE TRIGGER some_trigger ON some_table FOR INSERT " +
            "AS " +
            "BEGIN " +
            "RETURN; " +
            "END";
        _ = Ms().VerifiedStmt(createTriggerWithReturn);

        createTriggerWithReturn =
            "CREATE TRIGGER some_trigger ON some_table FOR INSERT " +
            "AS " +
            "BEGIN " +
            "RETURN; " +
            "END";
        _ = Ms().VerifiedStmt(createTriggerWithReturn);

        var createTriggerWithConditional =
            "CREATE TRIGGER some_trigger ON some_table FOR INSERT " +
            "AS " +
            "BEGIN " +
            "IF 1 = 2 " +
            "BEGIN " +
            "RAISERROR('Trigger fired', 10, 1); " +
            "END; " +
            "RETURN; " +
            "END";
        _ = Ms().VerifiedStmt(createTriggerWithConditional);
    }

    [Fact]
    public void parse_drop_trigger()
    {
        var sqlDropTrigger = "DROP TRIGGER emp_stamp;";
        var dropStmt = Ms().OneStatementParsesTo(sqlDropTrigger, "");
        Assert.Equal(
            dropStmt,
            new Statement.DropTrigger(new DropTrigger(
                IfExists: false,
                TriggerName: ObjectName.From(Sequence.Of(new Ident("emp_stamp"))),
                TableName: null,
                Option: null)));
    }

    [Fact]
    public void parse_print()
    {
        var printStringLiteral = "PRINT 'Hello, world!'";
        var printStmt = Ms().VerifiedStmt(printStringLiteral);
        Assert.Equal(
            printStmt,
            new Statement.Print(new PrintStatement(
                Message: new Expr.Value(
                    new Value.SingleQuotedString("Hello, world!").WithEmptySpan()))));

        _ = Ms().VerifiedStmt("PRINT N'Hello, ⛄️!'");
        _ = Ms().VerifiedStmt("PRINT @my_variable");
    }

    [Fact]
    public void parse_mssql_grant()
    {
        Ms().VerifiedStmt("GRANT SELECT ON my_table TO public, db_admin");
    }

    [Fact]
    public void parse_mssql_deny()
    {
        Ms().VerifiedStmt("DENY SELECT ON my_table TO public, db_admin");
    }

    [Fact]
    public void test_tsql_no_semicolon_delimiter()
    {
        var sql =
            "\n" +
            "DECLARE @X AS NVARCHAR(MAX)='x'\n" +
            "DECLARE @Y AS NVARCHAR(MAX)='y'\n" +
            "    ";
        var stmts = Tsql().ParseSqlStatements(sql);
        Assert.Equal(2, stmts.Count);
        Assert.True(stmts.All(s => s is Statement.Declare));

        sql =
            "\n" +
            "SELECT col FROM tbl\n" +
            "IF x=1\n" +
            "  SELECT 1\n" +
            "ELSE\n" +
            "  SELECT 2\n" +
            "    ";
        stmts = Tsql().ParseSqlStatements(sql);
        Assert.Equal(2, stmts.Count);
        Assert.True(stmts[0] is Statement.Query);
        Assert.True(stmts[1] is Statement.If);
    }

    [Fact]
    public void test_sql_keywords_as_table_aliases()
    {
        // Some keywords that should not be parsed as an alias implicitly or explicitly
        var reservedKws = new List<string> { "IF", "ELSE" };
        foreach (var kw in reservedKws)
        {
            foreach (var @explicit in new[] { "", "AS " })
            {
                Assert.Throws<ParserException>(() => Tsql()
                    .ParseSqlStatements($"SELECT * FROM tbl {@explicit}{kw}"));
            }
        }
    }

    [Fact]
    public void test_sql_keywords_as_column_aliases()
    {
        // Some keywords that should not be parsed as an alias implicitly or explicitly
        var reservedKws = new List<string> { "IF", "ELSE" };
        foreach (var kw in reservedKws)
        {
            foreach (var @explicit in new[] { "", "AS " })
            {
                Assert.Throws<ParserException>(() => Tsql()
                    .ParseSqlStatements($"SELECT col {@explicit}{kw} FROM tbl"));
            }
        }
    }

    [Fact]
    public void parse_mssql_begin_end_block()
    {
        // Single statement
        var sql = "BEGIN SELECT 1; END";
        var stmt = Ms().VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.StartTransaction st:
                Assert.True(st.Begin);
                Assert.True(st.HasEndKeyword);
                Assert.True(st.Transaction is null);
                Assert.True(st.Modifier is null);
                Assert.Equal(1, st.Statements.Count);
                break;
            default:
                Assert.Fail($"Expected StartTransaction, got: {stmt}");
                break;
        }

        // Multiple statements
        sql = "BEGIN SELECT 1; SELECT 2; END";
        stmt = Ms().VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.StartTransaction st:
                Assert.True(st.HasEndKeyword);
                Assert.Equal(2, st.Statements.Count);
                break;
            default:
                Assert.Fail($"Expected StartTransaction, got: {stmt}");
                break;
        }

        // DML inside BEGIN/END
        sql = "BEGIN INSERT INTO t VALUES (1); UPDATE t SET x = 2; END";
        stmt = Ms().VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.StartTransaction st:
                Assert.True(st.HasEndKeyword);
                Assert.Equal(2, st.Statements.Count);
                break;
            default:
                Assert.Fail($"Expected StartTransaction, got: {stmt}");
                break;
        }

        // BEGIN TRANSACTION still works
        sql = "BEGIN TRANSACTION";
        stmt = Ms().VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.StartTransaction st:
                Assert.True(st.Begin);
                Assert.False(st.HasEndKeyword);
                Assert.True(st.Transaction is not null);
                break;
            default:
                Assert.Fail($"Expected StartTransaction, got: {stmt}");
                break;
        }
    }

    /// <summary>
    /// MSSQL supports <c>TRAN</c> as shorthand for <c>TRANSACTION</c>.
    /// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/begin-transaction-transact-sql"/>
    /// </summary>
    [Fact]
    public void parse_mssql_tran_shorthand()
    {
        // BEGIN TRAN
        var sql = "BEGIN TRAN";
        var stmt = Ms().VerifiedStmt(sql);
        switch (stmt)
        {
            case Statement.StartTransaction st:
                Assert.True(st.Begin);
                Assert.Equal<BeginTransactionKind?>(st.Transaction, new BeginTransactionKind.Tran());
                Assert.False(st.HasEndKeyword);
                break;
            default:
                Assert.Fail($"Expected StartTransaction, got: {stmt}");
                break;
        }

        // COMMIT TRAN normalizes to COMMIT (same as COMMIT TRANSACTION)
        Ms().OneStatementParsesTo("COMMIT TRAN", "COMMIT");

        // ROLLBACK TRAN normalizes to ROLLBACK (same as ROLLBACK TRANSACTION)
        Ms().OneStatementParsesTo("ROLLBACK TRAN", "ROLLBACK");
    }

    [Fact]
    public void test_tsql_statement_keywords_not_implicit_aliases()
    {
        // T-SQL statement-starting keywords must never be consumed as implicit
        // aliases for a preceding SELECT item or table reference when using
        // newline-delimited multi-statement scripts.

        // Without the fix, the parser would consume a statement-starting keyword
        // as an implicit alias for the preceding SELECT item or table reference,
        // then fail on the next token. Verify parsing succeeds and each input
        // produces the expected number of statements.

        // Keywords that should not become implicit column aliases
        var colAliasCases = new (string, int)[]
        {
            ("select 1\ndeclare @x as int", 2),
            ("select 1\nexec sp_who", 2),
            ("select 1\ninsert into t values (1)", 2),
            ("select 1\nupdate t set col=1", 2),
            ("select 1\ndelete from t", 2),
            ("select 1\ndrop table t", 2),
            ("select 1\ncreate table t (id int)", 2),
            ("select 1\nalter table t add col int", 2),
            ("select 1\nreturn", 2),
        };
        foreach (var (sql, expected) in colAliasCases)
        {
            Sequence<Statement> stmts;
            try
            {
                stmts = Tsql().ParseSqlStatements(sql);
            }
            catch (ParserException e)
            {
                throw new Xunit.Sdk.XunitException($"failed to parse {sql}: {e.Error}");
            }
            Assert.True(
                stmts.Count == expected,
                $"expected {expected} stmts for: {sql} (left: {stmts.Count}, right: {expected})");
        }

        // Keywords that should not become implicit table aliases
        var tblAliasCases = new (string, int)[]
        {
            ("select * from t\ndeclare @x as int", 2),
            ("select * from t\ndrop table t", 2),
            ("select * from t\ncreate table u (id int)", 2),
            ("select * from t\nexec sp_who", 2),
        };
        foreach (var (sql, expected) in tblAliasCases)
        {
            Sequence<Statement> stmts;
            try
            {
                stmts = Tsql().ParseSqlStatements(sql);
            }
            catch (ParserException e)
            {
                throw new Xunit.Sdk.XunitException($"failed to parse {sql}: {e.Error}");
            }
            Assert.True(
                stmts.Count == expected,
                $"expected {expected} stmts for: {sql} (left: {stmts.Count}, right: {expected})");
        }
    }

    [Fact]
    public void test_exec_dynamic_sql()
    {
        // EXEC (@sql) executes a dynamic SQL string held in a variable.
        // It must parse as a single Execute statement and not attempt to parse
        // parameters after the closing paren.
        var stmts = Tsql()
            .ParseSqlStatements("EXEC (@sql)"); // .expect("EXEC (@sql) should parse")
        Assert.Equal(1, stmts.Count);
        Assert.True(
            stmts[0] is Statement.Execute,
            $"expected Execute, got: {stmts[0]}");

        // Verify that a statement following EXEC (@sql) on the next line is parsed
        // as a separate statement and not consumed as a parameter.
        stmts = Tsql()
            .ParseSqlStatements("EXEC (@sql)\nDROP TABLE #tmp"); // .expect("EXEC (@sql) followed by DROP TABLE should parse")
        Assert.Equal(2, stmts.Count);
    }

    [Fact]
    public void test_exec_dynamic_sql_string_concat()
    {
        // EXEC with string concatenation: EXEC ('...' + @var + '...')
        var stmts = Tsql()
            .ParseSqlStatements("EXEC ('SELECT * FROM ' + @TableName + ' WHERE 1=1')"); // .expect("EXEC with string concatenation should parse")
        Assert.Equal(1, stmts.Count);
        Assert.True(
            stmts[0] is Statement.Execute,
            $"expected Execute, got: {stmts[0]}");
    }

    // MSSQL OUTPUT clause on INSERT/UPDATE/DELETE
    // https://learn.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql
    [Fact]
    public void parse_mssql_insert_with_output()
    {
        MsAndGeneric().VerifiedStmt(
            "INSERT INTO customers (name, email) OUTPUT INSERTED.id, INSERTED.name VALUES ('John', 'john@example.com')");
    }

    [Fact]
    public void parse_mssql_insert_with_output_into()
    {
        MsAndGeneric().VerifiedStmt(
            "INSERT INTO customers (name, email) OUTPUT INSERTED.id, INSERTED.name INTO @new_ids VALUES ('John', 'john@example.com')");
    }

    [Fact]
    public void parse_mssql_delete_with_output()
    {
        MsAndGeneric().VerifiedStmt("DELETE FROM customers OUTPUT DELETED.* WHERE id = 1");
    }

    [Fact]
    public void parse_mssql_delete_with_output_into()
    {
        MsAndGeneric().VerifiedStmt(
            "DELETE FROM customers OUTPUT DELETED.id, DELETED.name INTO @deleted_rows WHERE active = 0");
    }

    [Fact]
    public void parse_mssql_update_with_output()
    {
        MsAndGeneric().VerifiedStmt(
            "UPDATE employees SET salary = salary * 1.1 OUTPUT INSERTED.id, DELETED.salary, INSERTED.salary WHERE department = 'Engineering'");
    }

    [Fact]
    public void parse_mssql_update_with_output_into()
    {
        MsAndGeneric().VerifiedStmt(
            "UPDATE employees SET salary = salary * 1.1 OUTPUT INSERTED.id, DELETED.salary, INSERTED.salary INTO @changes WHERE department = 'Engineering'");
    }

    [Fact]
    public void test_collate_on_compound_identifier()
    {
        MsAndGeneric().VerifiedStmt("SELECT t1.a COLLATE Latin1_General_CI_AS FROM t1");
    }

    [Fact]
    public void parse_mssql_money_constants()
    {
        Ms().VerifiedOnlySelect("SELECT CEILING($123.45)");

        var select = Ms().VerifiedOnlySelect("SELECT $123.45");
        Assert.Equal<Expr>(
            new Expr.Value(new Value.Placeholder("$123.45").WithEmptySpan()),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        select = Ms().VerifiedOnlySelect("SELECT $0.99");
        Assert.Equal<Expr>(
            new Expr.Value(new Value.Placeholder("$0.99").WithEmptySpan()),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        select = Ms().VerifiedOnlySelect("SELECT $0.0");
        Assert.Equal<Expr>(
            new Expr.Value(new Value.Placeholder("$0.0").WithEmptySpan()),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        select = Ms().VerifiedOnlySelect("SELECT $123");
        Assert.Equal<Expr>(
            new Expr.Value(new Value.Placeholder("$123").WithEmptySpan()),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));

        select = Ms().VerifiedOnlySelect("SELECT $0");
        Assert.Equal<Expr>(
            new Expr.Value(new Value.Placeholder("$0").WithEmptySpan()),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void parse_bracket_quoted_function_argument_name()
    {
        if (Ms().VerifiedStmt("DROP FUNCTION f([Role] INT)") is not Statement.DropFunction { Value: var drop })
        {
            Assert.Fail("expected a DROP FUNCTION statement");
            return;
        }
        Assert.Equal(
            drop.FuncDesc[0].Args,
            Sequence.Of(new OperateFunctionArg(
                Mode: null,
                Name: Ident.WithQuote('[', "Role"),
                DataType: new DataType.Int(null),
                DefaultExpr: null)));
    }

    [Fact]
    public void parse_bracket_quoted_eq_alias_assignment()
    {
        Ms().OneStatementParsesTo("SELECT a = [from] FROM t", "SELECT [from] AS a FROM t");
    }

    [Fact]
    public void parse_create_proc()
    {
        Ms().OneStatementParsesTo(
            "CREATE PROC test AS BEGIN SELECT 1; END",
            "CREATE PROCEDURE test AS BEGIN SELECT 1; END");
        Ms().OneStatementParsesTo(
            "CREATE OR ALTER PROC test AS BEGIN SELECT 1; END",
            "CREATE OR ALTER PROCEDURE test AS BEGIN SELECT 1; END");

        // .expect_err("PROC should remain MSSQL-specific")
        Assert.Throws<ParserException>(() => new TestedDialects(new List<Dialect> { new GenericDialect() })
            .ParseSqlStatements("CREATE PROC test AS BEGIN SELECT 1; END"));
        MsAndGeneric().VerifiedStmt("SELECT proc FROM jobs");
    }
}
