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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_postgres.rs (linhas 3409–6827).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

// Test SQL syntax specific to PostgreSQL. The parser based on the
// generic dialect is also tested (on the inputs it can handle).
//
// Notas da tradução (docs/port/onda-c/C7.md):
// - Esta classe cobre só o trecho 3409–6827 do arquivo Rust; as funções-atalho do arquivo
//   (`pg()`, `pg_and_generic()`) são copiadas para cá como métodos privados.
// - `assert_eq!(a, b)` mantém a ordem do Rust, exceto quando um dos lados é literal: o literal vai na
//   posição "expected" (regra xUnit2000, como em AstModTests/SpansTests).
// - `assert_eq!(1, x.len())` usa `Assert.Equal(1, x.Count)`; o xUnit2013 fica suprimido neste arquivo.
// - `panic!(msg)`/`unreachable!()` → `Assert.Fail(msg)`; `.unwrap()` de `Option` → `Unwrap(x)`.
// - Strings Rust com várias linhas viram concatenações com `\n` explícito (o arquivo C# é CRLF).
// - Sequências `\u` + 4 dígitos dentro de SQL usam o marcador `{BS}` trocado por uma barra invertida,
//   para que nenhuma ferramenta de edição as converta no caractere literal.
// - `#[cfg(feature = "bigdecimal")] fn test_simple_postgres_insert_with_alias` não é portado: o Core não
//   tem a feature `bigdecimal` (`Value::Number` guarda `String`); a variante sem a feature está portada.
#pragma warning disable xUnit2013
public class SqlParserPostgresTests2
{
    private const string Backslash = "\\";

    // Rust: `Option::unwrap()` (panic se `None`).
    private static T Unwrap<T>(T? value)
        where T : class =>
        value ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");

    // fn pg() -> TestedDialects (sqlparser_postgres.rs:4167)
    private static TestedDialects Pg() =>
        new TestedDialects(new List<Dialect> { new PostgreSqlDialect() });

    // fn pg_and_generic() -> TestedDialects (sqlparser_postgres.rs:4171)
    private static TestedDialects PgAndGeneric() =>
        new TestedDialects(new List<Dialect>
        {
            new PostgreSqlDialect(),
            new GenericDialect(),
        });

    [Fact]
    public void parse_create_index_with_predicate()
    {
        var sql = "CREATE INDEX IF NOT EXISTS my_index ON my_table(col1, col2) WHERE col3 IS NULL";
        switch (Pg().VerifiedStmt(sql))
        {
            case Statement.CreateIndex
            {
                Value:
                {
                    Name.Value: var name,
                    TableName.Value: var tableName,
                    NullsDistinct: null,
                    Predicate: not null,
                } createIndex,
            }:
                TestUtils.AssertEqVec(new[] { "my_index" }, name);
                TestUtils.AssertEqVec(new[] { "my_table" }, tableName);
                Assert.Null(createIndex.Using);
                Assert.False(createIndex.Unique);
                Assert.False(createIndex.Concurrently);
                Assert.False(createIndex.Async);
                Assert.True(createIndex.IfNotExists);
                TestUtils.AssertEqVec(new[] { "col1", "col2" }, createIndex.Columns);
                Assert.True(createIndex.Include.IsEmpty);
                Assert.True(createIndex.With.IsEmpty);
                Assert.True(createIndex.IndexOptions.IsEmpty);
                Assert.True(createIndex.AlterOptions.IsEmpty);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }
    }

    [Fact]
    public void parse_create_index_with_include()
    {
        var sql = "CREATE INDEX IF NOT EXISTS my_index ON my_table(col1, col2) INCLUDE (col3, col4)";
        switch (Pg().VerifiedStmt(sql))
        {
            case Statement.CreateIndex
            {
                Value:
                {
                    Name.Value: var name,
                    TableName.Value: var tableName,
                    NullsDistinct: null,
                    Predicate: null,
                } createIndex,
            }:
                TestUtils.AssertEqVec(new[] { "my_index" }, name);
                TestUtils.AssertEqVec(new[] { "my_table" }, tableName);
                Assert.Null(createIndex.Using);
                Assert.False(createIndex.Unique);
                Assert.False(createIndex.Concurrently);
                Assert.False(createIndex.Async);
                Assert.True(createIndex.IfNotExists);
                TestUtils.AssertEqVec(new[] { "col1", "col2" }, createIndex.Columns);
                TestUtils.AssertEqVec(new[] { "col3", "col4" }, createIndex.Include);
                Assert.True(createIndex.With.IsEmpty);
                Assert.True(createIndex.IndexOptions.IsEmpty);
                Assert.True(createIndex.AlterOptions.IsEmpty);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }
    }

    [Fact]
    public void parse_create_index_with_nulls_distinct()
    {
        var sql = "CREATE INDEX IF NOT EXISTS my_index ON my_table(col1, col2) NULLS NOT DISTINCT";
        switch (Pg().VerifiedStmt(sql))
        {
            case Statement.CreateIndex
            {
                Value:
                {
                    Name.Value: var name,
                    TableName.Value: var tableName,
                    NullsDistinct: bool nullsDistinct,
                    Predicate: null,
                } createIndex,
            }:
                TestUtils.AssertEqVec(new[] { "my_index" }, name);
                TestUtils.AssertEqVec(new[] { "my_table" }, tableName);
                Assert.Null(createIndex.Using);
                Assert.False(createIndex.Unique);
                Assert.False(createIndex.Concurrently);
                Assert.False(createIndex.Async);
                Assert.True(createIndex.IfNotExists);
                TestUtils.AssertEqVec(new[] { "col1", "col2" }, createIndex.Columns);
                Assert.True(createIndex.Include.IsEmpty);
                Assert.False(nullsDistinct);
                Assert.True(createIndex.With.IsEmpty);
                Assert.True(createIndex.IndexOptions.IsEmpty);
                Assert.True(createIndex.AlterOptions.IsEmpty);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }

        sql = "CREATE INDEX IF NOT EXISTS my_index ON my_table(col1, col2) NULLS DISTINCT";
        switch (Pg().VerifiedStmt(sql))
        {
            case Statement.CreateIndex
            {
                Value:
                {
                    Name.Value: var name,
                    TableName.Value: var tableName,
                    NullsDistinct: bool nullsDistinct,
                    Predicate: null,
                } createIndex,
            }:
                TestUtils.AssertEqVec(new[] { "my_index" }, name);
                TestUtils.AssertEqVec(new[] { "my_table" }, tableName);
                Assert.Null(createIndex.Using);
                Assert.False(createIndex.Unique);
                Assert.False(createIndex.Concurrently);
                Assert.False(createIndex.Async);
                Assert.True(createIndex.IfNotExists);
                TestUtils.AssertEqVec(new[] { "col1", "col2" }, createIndex.Columns);
                Assert.True(createIndex.Include.IsEmpty);
                Assert.True(nullsDistinct);
                Assert.True(createIndex.With.IsEmpty);
                Assert.True(createIndex.IndexOptions.IsEmpty);
                Assert.True(createIndex.AlterOptions.IsEmpty);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }
    }

    [Fact]
    public void parse_array_subquery_expr()
    {
        var sql = "SELECT ARRAY(SELECT 1 UNION SELECT 2)";
        var select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("ARRAY"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.Subquery(new Query(
                    With: null,
                    Body: new SetExpr.SetOperation(
                        Op: new SetOperator.Union(),
                        SetQuantifier: new SetQuantifier.None(),
                        Left: new SetExpr.Select(new Select(
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
                            Qualify: null,
                            WindowBeforeQualify: false,
                            ValueTableMode: null,
                            ConnectBy: Sequence<ConnectByKind>.Empty,
                            Flavor: SelectFlavor.Standard)),
                        Right: new SetExpr.Select(new Select(
                            SelectToken: AttachedToken.Empty(),
                            OptimizerHints: Sequence<OptimizerHint>.Empty,
                            Distinct: null,
                            SelectModifiers: null,
                            Top: null,
                            TopBeforeDistinct: false,
                            Projection: Sequence.Of<SelectItem>(new SelectItem.UnnamedExpr(new Expr.Value(
                                TestUtils.Number("2").WithEmptySpan()))),
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
                            Flavor: SelectFlavor.Standard))),
                    OrderBy: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty)),
                Filter: null,
                NullTreatment: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(TestUtils.Only(select.Projection)));
    }

    [Fact]
    public void test_transaction_statement()
    {
        var statement = Pg().VerifiedStmt("SET TRANSACTION SNAPSHOT '000003A1-1'");
        Assert.Equal(
            statement,
            new Statement.Set(new Set.SetTransaction(
                Modes: Sequence<TransactionMode>.Empty,
                Snapshot: new Value.SingleQuotedString("000003A1-1").WithEmptySpan(),
                Session: false)));
        statement = Pg().VerifiedStmt("SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY, READ WRITE, ISOLATION LEVEL SERIALIZABLE");
        Assert.Equal(
            statement,
            new Statement.Set(new Set.SetTransaction(
                Modes: Sequence.Of<TransactionMode>(
                    new TransactionMode.AccessMode(new TransactionAccessMode.ReadOnly()),
                    new TransactionMode.AccessMode(new TransactionAccessMode.ReadWrite()),
                    new TransactionMode.IsolationLevel(new TransactionIsolationLevel.Serializable())),
                Snapshot: null,
                Session: true)));
    }

    [Fact]
    public void test_json()
    {
        var sql = "SELECT params ->> 'name' FROM events";
        var select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("params")),
                Op: new BinaryOperator.LongArrow(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("name").WithEmptySpan()))),
            select.Projection[0]);

        sql = "SELECT params -> 'name' FROM events";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("params")),
                Op: new BinaryOperator.Arrow(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("name").WithEmptySpan()))),
            select.Projection[0]);

        sql = "SELECT info -> 'items' ->> 'product' FROM orders";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("info")),
                    Op: new BinaryOperator.Arrow(),
                    Right: new Expr.Value(
                        new Value.SingleQuotedString("items").WithEmptySpan())),
                Op: new BinaryOperator.LongArrow(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("product").WithEmptySpan()))),
            select.Projection[0]);

        // the RHS can be a number (array element access)
        sql = "SELECT obj -> 42";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("obj")),
                Op: new BinaryOperator.Arrow(),
                Right: Expr.MakeValue(TestUtils.Number("42")))),
            select.Projection[0]);

        // the RHS can be an identifier
        sql = "SELECT obj -> key";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("obj")),
                Op: new BinaryOperator.Arrow(),
                Right: new Expr.Identifier(new Ident("key")))),
            select.Projection[0]);

        // -> operator has lower precedence than arithmetic ops
        sql = "SELECT obj -> 3 * 2";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("obj")),
                Op: new BinaryOperator.Arrow(),
                Right: new Expr.BinaryOp(
                    Left: Expr.MakeValue(TestUtils.Number("3")),
                    Op: new BinaryOperator.Multiply(),
                    Right: Expr.MakeValue(TestUtils.Number("2"))))),
            select.Projection[0]);

        sql = "SELECT info #> '{a,b,c}' FROM orders";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("info")),
                Op: new BinaryOperator.HashArrow(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("{a,b,c}").WithEmptySpan()))),
            select.Projection[0]);

        sql = "SELECT info #>> '{a,b,c}' FROM orders";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("info")),
                Op: new BinaryOperator.HashLongArrow(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("{a,b,c}").WithEmptySpan()))),
            select.Projection[0]);

        sql = "SELECT info FROM orders WHERE info @> '{\"a\": 1}'";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("info")),
                Op: new BinaryOperator.AtArrow(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("{\"a\": 1}").WithEmptySpan())),
            Unwrap(select.Selection));

        sql = "SELECT info FROM orders WHERE '{\"a\": 1}' <@ info";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Value(
                    new Value.SingleQuotedString("{\"a\": 1}").WithEmptySpan()),
                Op: new BinaryOperator.ArrowAt(),
                Right: new Expr.Identifier(new Ident("info"))),
            Unwrap(select.Selection));

        sql = "SELECT info #- ARRAY['a', 'b'] FROM orders";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new SelectItem.UnnamedExpr(new Expr.BinaryOp(
                Left: new Expr.Identifier(Ident.From("info")),
                Op: new BinaryOperator.HashMinus(),
                Right: new Expr.Array(new Ast.Array(
                    Elem: Sequence.Of<Expr>(
                        new Expr.Value(new Value.SingleQuotedString("a").WithEmptySpan()),
                        new Expr.Value(new Value.SingleQuotedString("b").WithEmptySpan())),
                    Named: true)))),
            select.Projection[0]);

        sql = "SELECT info FROM orders WHERE info @? '$.a'";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(Ident.From("info")),
                Op: new BinaryOperator.AtQuestion(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("$.a").WithEmptySpan())),
            Unwrap(select.Selection));

        sql = "SELECT info FROM orders WHERE info @@ '$.a'";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(Ident.From("info")),
                Op: new BinaryOperator.AtAt(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("$.a").WithEmptySpan())),
            Unwrap(select.Selection));

        sql = "SELECT info FROM orders WHERE info ? 'b'";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("info")),
                Op: new BinaryOperator.Question(),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("b").WithEmptySpan())),
            Unwrap(select.Selection));

        sql = "SELECT info FROM orders WHERE info ?& ARRAY['b', 'c']";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("info")),
                Op: new BinaryOperator.QuestionAnd(),
                Right: new Expr.Array(new Ast.Array(
                    Elem: Sequence.Of<Expr>(
                        new Expr.Value(new Value.SingleQuotedString("b").WithEmptySpan()),
                        new Expr.Value(new Value.SingleQuotedString("c").WithEmptySpan())),
                    Named: true))),
            Unwrap(select.Selection));

        sql = "SELECT info FROM orders WHERE info ?| ARRAY['b', 'c']";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("info")),
                Op: new BinaryOperator.QuestionPipe(),
                Right: new Expr.Array(new Ast.Array(
                    Elem: Sequence.Of<Expr>(
                        new Expr.Value(new Value.SingleQuotedString("b").WithEmptySpan()),
                        new Expr.Value(new Value.SingleQuotedString("c").WithEmptySpan())),
                    Named: true))),
            Unwrap(select.Selection));
    }

    [Fact]
    public void json_object_colon_syntax()
    {
        switch (Pg().VerifiedExpr("JSON_OBJECT('name' : 'value')"))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value.Args: var args } }:
                Assert.True(
                    args is { Count: 1 }
                        && args[0] is FunctionArg.ExprNamed { Operator: FunctionArgOperator.Colon },
                    $"Invalid function argument: {string.Join(", ", args)}");
                break;
            case var other:
                Assert.Fail($"Expected: JSON_OBJECT('name' : 'value') to be parsed as a function, but got {other}");
                break;
        }
    }

    [Fact]
    public void json_object_value_syntax()
    {
        switch (Pg().VerifiedExpr("JSON_OBJECT('name' VALUE 'value')"))
        {
            case Expr.Function { Value.Args: FunctionArguments.List { Value.Args: var args } }:
                Assert.True(
                    args is { Count: 1 }
                        && args[0] is FunctionArg.ExprNamed { Operator: FunctionArgOperator.Value },
                    $"Invalid function argument: {string.Join(", ", args)}");
                break;
            case var other:
                Assert.Fail($"Expected: JSON_OBJECT('name' VALUE 'value') to be parsed as a function, but got {other}");
                break;
        }
    }

    [Fact]
    public void parse_json_object()
    {
        var sql = "JSON_OBJECT('name' VALUE 'value' NULL ON NULL)";
        var expr = Pg().VerifiedExpr(sql);
        Assert.True(
            expr is Expr.Function
            {
                Value:
                {
                    Name.Value: var parts,
                    Args: FunctionArguments.List { Value: { Args: var args, Clauses: var clauses } },
                },
            }
                && parts == Sequence.Of<ObjectNamePart>(new ObjectNamePart.Identifier(new Ident("JSON_OBJECT")))
                && args is { Count: 1 }
                && args[0] is FunctionArg.ExprNamed { Operator: FunctionArgOperator.Value }
                && clauses == Sequence.Of<FunctionArgumentClause>(
                    new FunctionArgumentClause.JsonNullClause(new JsonNullClause.NullOnNull())),
            $"Failed to parse JSON_OBJECT with expected structure, got: {expr}");

        sql = "JSON_OBJECT('name' VALUE 'value' RETURNING JSONB)";
        expr = Pg().VerifiedExpr(sql);
        Assert.True(
            expr is Expr.Function
            {
                Value:
                {
                    Name.Value: var parts2,
                    Args: FunctionArguments.List { Value: { Args: var args2, Clauses: var clauses2 } },
                },
            }
                && parts2 == Sequence.Of<ObjectNamePart>(new ObjectNamePart.Identifier(new Ident("JSON_OBJECT")))
                && args2 is { Count: 1 }
                && args2[0] is FunctionArg.ExprNamed { Operator: FunctionArgOperator.Value }
                && clauses2 == Sequence.Of<FunctionArgumentClause>(
                    new FunctionArgumentClause.JsonReturningClause(new JsonReturningClause(new DataType.JSONB()))),
            $"Failed to parse JSON_OBJECT with expected structure, got: {expr}");

        sql = "JSON_OBJECT(RETURNING JSONB)";
        expr = Pg().VerifiedExpr(sql);
        Assert.True(
            expr is Expr.Function
            {
                Value:
                {
                    Name.Value: var parts3,
                    Args: FunctionArguments.List { Value: { Args: var args3, Clauses: var clauses3 } },
                },
            }
                && parts3 == Sequence.Of<ObjectNamePart>(new ObjectNamePart.Identifier(new Ident("JSON_OBJECT")))
                && args3.IsEmpty
                && clauses3 == Sequence.Of<FunctionArgumentClause>(
                    new FunctionArgumentClause.JsonReturningClause(new JsonReturningClause(new DataType.JSONB()))),
            $"Failed to parse JSON_OBJECT with expected structure, got: {expr}");
    }

    [Fact]
    public void parse_json_table_is_not_reserved()
    {
        // JSON_TABLE is not a reserved keyword in PostgreSQL, even though it is in SQL:2023
        // see: https://en.wikipedia.org/wiki/List_of_SQL_reserved_words
        var from = PgAndGeneric().VerifiedOnlySelect("SELECT * FROM JSON_TABLE").From;
        Assert.Equal(1, from.Count);
        switch (from[0].Relation)
        {
            case TableFactor.Table { Name.Value: var name }:
                Assert.Equal(
                    new ObjectNamePart.Identifier(new Ident("JSON_TABLE")),
                    name[0]);
                break;
            case var other:
                Assert.Fail($"Expected: JSON_TABLE to be parsed as a table name, but got {other}");
                break;
        }
    }

    [Fact]
    public void test_composite_value()
    {
        var sql = "SELECT (on_hand.item).name FROM on_hand WHERE (on_hand.item).price > 9";
        var select = Pg().VerifiedOnlySelect(sql);

        if (TestUtils.ExprFromProjection(select.Projection[0]) is not Expr.CompoundFieldAccess { Root: var root, AccessChain: var accessChain })
        {
            Assert.Fail($"expected projection: got {select.Projection[0]}");
            return;
        }
        Assert.Equal(
            root,
            new Expr.Nested(new Expr.CompoundIdentifier(Sequence.Of(
                new Ident("on_hand"),
                new Ident("item")))));
        Assert.Equal(
            accessChain,
            Sequence.Of<AccessExpr>(new AccessExpr.Dot(new Expr.Identifier(new Ident("name")))));

        Assert.Equal(
            Unwrap(select.Selection),
            new Expr.BinaryOp(
                Left: new Expr.CompoundFieldAccess(
                    Root: new Expr.Nested(new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("on_hand"),
                        new Ident("item")))),
                    AccessChain: Sequence.Of<AccessExpr>(new AccessExpr.Dot(new Expr.Identifier(new Ident("price"))))),
                Op: new BinaryOperator.Gt(),
                Right: Expr.MakeValue(TestUtils.Number("9"))));

        sql = "SELECT (information_schema._pg_expandarray(ARRAY['i', 'i'])).n";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.CompoundFieldAccess(
                Root: new Expr.Nested(new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(
                        new Ident("information_schema"),
                        new Ident("_pg_expandarray"))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.List(new FunctionArgumentList(
                        DuplicateTreatment: null,
                        Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(new Expr.Array(
                            new Ast.Array(
                                Elem: Sequence.Of<Expr>(
                                    new Expr.Value(
                                        new Value.SingleQuotedString("i").WithEmptySpan()),
                                    new Expr.Value(
                                        new Value.SingleQuotedString("i").WithEmptySpan())),
                                Named: true))))),
                        Clauses: Sequence<FunctionArgumentClause>.Empty)),
                    NullTreatment: null,
                    Filter: null,
                    Over: null,
                    WithinGroup: Sequence<OrderByExpr>.Empty))),
                AccessChain: Sequence.Of<AccessExpr>(new AccessExpr.Dot(new Expr.Identifier(new Ident("n"))))),
            TestUtils.ExprFromProjection(select.Projection[0]));
    }

    [Fact]
    public void parse_quoted_identifier()
    {
        PgAndGeneric().VerifiedStmt("SELECT \"quoted \"\" ident\"");
    }

    [Fact]
    public void parse_quoted_identifier_2()
    {
        PgAndGeneric().VerifiedStmt("SELECT \"\"\"quoted ident\"\"\"");
    }

    [Fact]
    public void parse_local_and_global()
    {
        PgAndGeneric().VerifiedStmt("CREATE LOCAL TEMPORARY TABLE table (COL INT)");
    }

    [Fact]
    public void parse_on_commit()
    {
        PgAndGeneric()
            .VerifiedStmt("CREATE TEMPORARY TABLE table (COL INT) ON COMMIT PRESERVE ROWS");

        PgAndGeneric().VerifiedStmt("CREATE TEMPORARY TABLE table (COL INT) ON COMMIT DELETE ROWS");

        PgAndGeneric().VerifiedStmt("CREATE TEMPORARY TABLE table (COL INT) ON COMMIT DROP");
    }

    [Fact]
    public void parse_xmlforest_aliased_arguments()
    {
        var select = PgAndGeneric().VerifiedOnlySelect("SELECT XMLFOREST(a AS x, b)");
        Assert.Equal(
            TestUtils.ExprFromProjection(select.Projection[0]),
            TestUtils.Call(
                "XMLFOREST",
                new Expr[]
                {
                    new Expr.Named(
                        Expr: new Expr.Identifier(new Ident("a")),
                        Name: new Ident("x")),
                    new Expr.Identifier(new Ident("b")),
                }));
    }

    [Fact]
    public void parse_xmlparse()
    {
        // The parser only distinguishes the two modes, so the corpus covers those
        // plus a non-literal argument.
        var statements = new[]
        {
            "SELECT XMLPARSE(CONTENT '')",
            "SELECT XMLPARSE(CONTENT '<abc>x</abc>')",
            "SELECT XMLPARSE(DOCUMENT '<abc>x</abc>')",
            "SELECT XMLPARSE(DOCUMENT col || '</abc>')",
        };
        foreach (var sql in statements)
        {
            Pg().VerifiedStmt(sql);
        }
    }

    [Fact]
    public void parse_xml_typed_string()
    {
        // xml '...' should parse as a TypedString on PostgreSQL and Generic
        var sql = "SELECT xml '<foo/>'";
        var select = PgAndGeneric().VerifiedOnlySelect(sql);
        switch (TestUtils.ExprFromProjection(select.Projection[0]))
        {
            case Expr.TypedString
            {
                Value:
                {
                    DataType: DataType.Custom { Name: var name, Modifiers: var modifiers },
                    Value: var value,
                    UsesOdbcSyntax: false,
                },
            }:
                Assert.Equal("xml", name.ToString());
                Assert.True(modifiers.IsEmpty);
                Assert.Equal(value.Value, new Value.SingleQuotedString("<foo/>"));
                break;
            case var other:
                Assert.Fail($"Expected TypedString, got: {other}");
                break;
        }
    }

    [Fact]
    public void parse_escaped_literal_string()
    {
        var sql = @"SELECT E's1 \n s1', E's2 \\n s2', E's3 \\\n s3', E's4 \\\\n s4', E'\'', E'foo \\'";
        var select = PgAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(6, select.Projection.Count);
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("s1 \n s1").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("s2 \\n s2").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("s3 \\\n s3").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[2]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("s4 \\\\n s4").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[3]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("'").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[4]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("foo \\").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[5]));

        sql = @"SELECT E'\'";
        var ex = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql));
        Assert.Equal(
            "sql parser error: Unterminated encoded string literal at Line: 1, Column: 8",
            ex.Error.ToString());

        sql = @"SELECT E'{BS}u0001', E'{BS}U0010FFFF', E'\xC', E'\x25', E'\2', E'\45', E'\445'".Replace("{BS}", Backslash);
        var canonical = "";
        select = PgAndGeneric().VerifiedOnlySelectWithCanonical(sql, canonical);
        Assert.Equal(7, select.Projection.Count);
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral(char.ConvertFromUtf32(0x0001)).WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral(char.ConvertFromUtf32(0x10ffff)).WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral(char.ConvertFromUtf32(0x000c)).WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[2]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("%").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[3]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral(char.ConvertFromUtf32(0x0002)).WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[4]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("%").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[5]));
        Assert.Equal(
            new Expr.Value(new Value.EscapedStringLiteral("%").WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[6]));

        static void NegativeCast(string[] sqls)
        {
            foreach (var sql in sqls)
            {
                var ex = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements(sql));
                Assert.Equal(
                    "sql parser error: Unterminated encoded string literal at Line: 1, Column: 8",
                    ex.Error.ToString());
            }
        }

        NegativeCast(new[]
        {
            @"SELECT E'{BS}u0000'".Replace("{BS}", Backslash),
            @"SELECT E'{BS}U00110000'".Replace("{BS}", Backslash),
            @"SELECT E'{BS}u{0001}'".Replace("{BS}", Backslash),
            @"SELECT E'\xCAD'",
            @"SELECT E'\080'",
        });
    }

    [Fact]
    public void parse_declare()
    {
        PgAndGeneric()
            .VerifiedStmt("DECLARE \"SQL_CUR0x7fa44801bc00\" CURSOR WITH HOLD FOR SELECT 1");
        PgAndGeneric()
            .VerifiedStmt("DECLARE \"SQL_CUR0x7fa44801bc00\" CURSOR WITHOUT HOLD FOR SELECT 1");
        PgAndGeneric().VerifiedStmt("DECLARE \"SQL_CUR0x7fa44801bc00\" BINARY CURSOR FOR SELECT 1");
        PgAndGeneric()
            .VerifiedStmt("DECLARE \"SQL_CUR0x7fa44801bc00\" ASENSITIVE CURSOR FOR SELECT 1");
        PgAndGeneric()
            .VerifiedStmt("DECLARE \"SQL_CUR0x7fa44801bc00\" INSENSITIVE CURSOR FOR SELECT 1");
        PgAndGeneric().VerifiedStmt("DECLARE \"SQL_CUR0x7fa44801bc00\" SCROLL CURSOR FOR SELECT 1");
        PgAndGeneric()
            .VerifiedStmt("DECLARE \"SQL_CUR0x7fa44801bc00\" NO SCROLL CURSOR FOR SELECT 1");
        PgAndGeneric().VerifiedStmt("DECLARE \"SQL_CUR0x7fa44801bc00\" BINARY INSENSITIVE SCROLL CURSOR WITH HOLD FOR SELECT * FROM table_name LIMIT 2222");
    }

    [Fact]
    public void parse_current_functions()
    {
        var sql = "SELECT CURRENT_CATALOG, CURRENT_USER, SESSION_USER, USER";
        var select = PgAndGeneric().VerifiedOnlySelect(sql);
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("CURRENT_CATALOG"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.None(),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("CURRENT_USER"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.None(),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("SESSION_USER"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.None(),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(select.Projection[2]));
        Assert.Equal(
            new Expr.Function(new Function(
                Name: ObjectName.From(Sequence.Of(new Ident("USER"))),
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.None(),
                NullTreatment: null,
                Filter: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty)),
            TestUtils.ExprFromProjection(select.Projection[3]));
    }

    [Fact]
    public void parse_fetch()
    {
        PgAndGeneric().VerifiedStmt("FETCH 2048 IN \"SQL_CUR0x7fa44801bc00\"");
        PgAndGeneric().VerifiedStmt("FETCH 2048 IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric().VerifiedStmt("FETCH NEXT IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric().VerifiedStmt("FETCH PRIOR IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric().VerifiedStmt("FETCH FIRST IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric().VerifiedStmt("FETCH LAST IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric()
            .VerifiedStmt("FETCH ABSOLUTE 2048 IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric()
            .VerifiedStmt("FETCH RELATIVE 2048 IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric().VerifiedStmt("FETCH ALL IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric().VerifiedStmt("FETCH ALL IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric()
            .VerifiedStmt("FETCH FORWARD 2048 IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric()
            .VerifiedStmt("FETCH FORWARD ALL IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric()
            .VerifiedStmt("FETCH BACKWARD 2048 IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
        PgAndGeneric()
            .VerifiedStmt("FETCH BACKWARD ALL IN \"SQL_CUR0x7fa44801bc00\" INTO \"new_table\"");
    }

    [Fact]
    public void parse_custom_operator()
    {
        // operator with a database and schema
        var sql = "SELECT * FROM events WHERE relname OPERATOR(database.pg_catalog.~) '^(table)$'";
        var select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            select.Selection,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("relname", null, Span.Empty())),
                Op: new BinaryOperator.PGCustomBinaryOperator(Sequence.Of(
                    "database",
                    "pg_catalog",
                    "~")),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("^(table)$").WithEmptySpan())));

        // operator with a schema
        sql = "SELECT * FROM events WHERE relname OPERATOR(pg_catalog.~) '^(table)$'";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            select.Selection,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("relname", null, Span.Empty())),
                Op: new BinaryOperator.PGCustomBinaryOperator(Sequence.Of("pg_catalog", "~")),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("^(table)$").WithEmptySpan())));

        // custom operator without a schema
        sql = "SELECT * FROM events WHERE relname OPERATOR(~) '^(table)$'";
        select = Pg().VerifiedOnlySelect(sql);
        Assert.Equal(
            select.Selection,
            new Expr.BinaryOp(
                Left: new Expr.Identifier(new Ident("relname", null, Span.Empty())),
                Op: new BinaryOperator.PGCustomBinaryOperator(Sequence.Of("~")),
                Right: new Expr.Value(
                    new Value.SingleQuotedString("^(table)$").WithEmptySpan())));
    }

    [Fact]
    public void parse_operator_empty_parens_rejected()
    {
        var ex = Assert.Throws<ParserException>(() => PgAndGeneric().ParseSqlStatements("SELECT a OPERATOR() b"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: operator name, found: )"),
            ex.Error);
    }

    [Fact]
    public void parse_create_role()
    {
        var sql = "CREATE ROLE IF NOT EXISTS mysql_a, mysql_b";
        switch (Pg().VerifiedStmt(sql))
        {
            case Statement.CreateRole { Value: var createRole }:
                TestUtils.AssertEqVec(new[] { "mysql_a", "mysql_b" }, createRole.Names);
                Assert.True(createRole.IfNotExists);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }

        sql = "CREATE ROLE abc LOGIN PASSWORD NULL";
        {
            var statements = Pg().ParseSqlStatements(sql);
            if (statements is not { Count: 1 } || statements[0] is not Statement.CreateRole { Value: var createRole })
            {
                Assert.Fail($"Failed to parse CREATE ROLE test case: {string.Join("; ", statements)}");
                return;
            }
            TestUtils.AssertEqVec(new[] { "abc" }, createRole.Names);
            Assert.True(createRole.Login);
            Assert.Equal(createRole.Password, new Password.NullPassword());
        }

        sql = "CREATE ROLE abc WITH LOGIN PASSWORD NULL";
        {
            var statements = Pg().ParseSqlStatements(sql);
            if (statements is not { Count: 1 } || statements[0] is not Statement.CreateRole { Value: var createRole })
            {
                Assert.Fail($"Failed to parse CREATE ROLE test case: {string.Join("; ", statements)}");
                return;
            }
            TestUtils.AssertEqVec(new[] { "abc" }, createRole.Names);
            Assert.True(createRole.Login);
            Assert.Equal(createRole.Password, new Password.NullPassword());
        }

        sql = "CREATE ROLE magician WITH SUPERUSER CREATEROLE NOCREATEDB BYPASSRLS INHERIT PASSWORD 'abcdef' LOGIN VALID UNTIL '2025-01-01' IN ROLE role1, role2 ROLE role3 ADMIN role4, role5 REPLICATION";
        // Roundtrip order of optional parameters is not preserved
        {
            var statements = Pg().ParseSqlStatements(sql);
            if (statements is not { Count: 1 } || statements[0] is not Statement.CreateRole { Value: var createRole })
            {
                Assert.Fail($"Failed to parse CREATE ROLE test case: {string.Join("; ", statements)}");
                return;
            }
            TestUtils.AssertEqVec(new[] { "magician" }, createRole.Names);
            Assert.False(createRole.IfNotExists);
            Assert.True(createRole.Login);
            Assert.True(createRole.Inherit);
            Assert.True(createRole.Bypassrls);
            Assert.Equal(
                createRole.Password,
                new Password.PasswordValue(new Expr.Value(
                    new Value.SingleQuotedString("abcdef").WithEmptySpan())));
            Assert.True(createRole.Superuser);
            Assert.False(createRole.CreateDb);
            Assert.True(createRole.CreateRoleValue);
            Assert.True(createRole.Replication);
            Assert.Null(createRole.ConnectionLimit);
            Assert.Equal(
                createRole.ValidUntil,
                new Expr.Value(
                    new Value.SingleQuotedString("2025-01-01").WithEmptySpan()));
            TestUtils.AssertEqVec(new[] { "role1", "role2" }, createRole.InRole);
            Assert.True(createRole.InGroup.IsEmpty);
            TestUtils.AssertEqVec(new[] { "role3" }, createRole.Role);
            TestUtils.AssertEqVec(new[] { "role4", "role5" }, createRole.Admin);
            Assert.Null(createRole.AuthorizationOwner);
        }

        sql = "CREATE ROLE abc WITH USER foo, bar ROLE baz ";
        {
            var statements = Pg().ParseSqlStatements(sql);
            if (statements is not { Count: 1 } || statements[0] is not Statement.CreateRole { Value: var createRole })
            {
                Assert.Fail($"Failed to parse CREATE ROLE test case: {string.Join("; ", statements)}");
                return;
            }
            TestUtils.AssertEqVec(new[] { "abc" }, createRole.Names);
            TestUtils.AssertEqVec(new[] { "foo", "bar" }, createRole.User);
            TestUtils.AssertEqVec(new[] { "baz" }, createRole.Role);
        }

        var negatables = new[]
        {
            "BYPASSRLS",
            "CREATEDB",
            "CREATEROLE",
            "INHERIT",
            "LOGIN",
            "REPLICATION",
            "SUPERUSER",
        };

        foreach (var negatableKw in negatables)
        {
            sql = $"CREATE ROLE abc {negatableKw} NO{negatableKw}";
            bool isOk;
            try
            {
                Pg().ParseSqlStatements(sql);
                isOk = true;
            }
            catch (ParserException)
            {
                isOk = false;
            }
            if (isOk)
            {
                Assert.Fail($"Should not be able to parse CREATE ROLE containing both negated and non-negated versions of the same keyword: {negatableKw}");
            }
        }
    }

    [Fact]
    public void parse_alter_role()
    {
        var sql = "ALTER ROLE old_name RENAME TO new_name";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident("old_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.RenameRole(
                    RoleName: new Ident("new_name", null, Span.Empty()))));

        sql = "ALTER ROLE role_name WITH SUPERUSER CREATEDB CREATEROLE INHERIT LOGIN REPLICATION BYPASSRLS CONNECTION LIMIT 100 PASSWORD 'abcdef' VALID UNTIL '2025-01-01'";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident("role_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.WithOptions(
                    Options: Sequence.Of<RoleOption>(
                        new RoleOption.SuperUser(true),
                        new RoleOption.CreateDB(true),
                        new RoleOption.CreateRole(true),
                        new RoleOption.Inherit(true),
                        new RoleOption.Login(true),
                        new RoleOption.Replication(true),
                        new RoleOption.BypassRLS(true),
                        new RoleOption.ConnectionLimit(Expr.MakeValue(TestUtils.Number("100"))),
                        new RoleOption.Password(
                            new Password.PasswordValue(new Expr.Value(
                                new Value.SingleQuotedString("abcdef").WithEmptySpan()))),
                        new RoleOption.ValidUntil(new Expr.Value(
                            new Value.SingleQuotedString("2025-01-01").WithEmptySpan()))))));

        sql = "ALTER ROLE role_name WITH NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOLOGIN NOREPLICATION NOBYPASSRLS PASSWORD NULL";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident("role_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.WithOptions(
                    Options: Sequence.Of<RoleOption>(
                        new RoleOption.SuperUser(false),
                        new RoleOption.CreateDB(false),
                        new RoleOption.CreateRole(false),
                        new RoleOption.Inherit(false),
                        new RoleOption.Login(false),
                        new RoleOption.Replication(false),
                        new RoleOption.BypassRLS(false),
                        new RoleOption.Password(new Password.NullPassword())))));

        sql = "ALTER ROLE role_name SET maintenance_work_mem FROM CURRENT";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident("role_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.Set(
                    ConfigName: ObjectName.From(Sequence.Of(new Ident("maintenance_work_mem", null, Span.Empty()))),
                    ConfigValue: new SetConfigValue.FromCurrent(),
                    InDatabase: null)));

        sql = "ALTER ROLE role_name IN DATABASE database_name SET maintenance_work_mem = 100000";
        Assert.Equal(
            Pg().ParseSqlStatements(sql),
            Sequence.Of<Statement>(new Statement.AlterRole(
                Name: new Ident("role_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.Set(
                    ConfigName: ObjectName.From(Sequence.Of(new Ident("maintenance_work_mem", null, Span.Empty()))),
                    ConfigValue: new SetConfigValue.Value(new Expr.Value(
                        TestUtils.Number("100000").WithEmptySpan())),
                    InDatabase: ObjectName.From(Sequence.Of(new Ident("database_name", null, Span.Empty())))))));

        sql = "ALTER ROLE role_name IN DATABASE database_name SET maintenance_work_mem TO 100000";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident("role_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.Set(
                    ConfigName: ObjectName.From(Sequence.Of(new Ident("maintenance_work_mem", null, Span.Empty()))),
                    ConfigValue: new SetConfigValue.Value(new Expr.Value(
                        TestUtils.Number("100000").WithEmptySpan())),
                    InDatabase: ObjectName.From(Sequence.Of(new Ident("database_name", null, Span.Empty()))))));

        sql = "ALTER ROLE role_name IN DATABASE database_name SET maintenance_work_mem TO DEFAULT";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident("role_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.Set(
                    ConfigName: ObjectName.From(Sequence.Of(new Ident("maintenance_work_mem", null, Span.Empty()))),
                    ConfigValue: new SetConfigValue.Default(),
                    InDatabase: ObjectName.From(Sequence.Of(new Ident("database_name", null, Span.Empty()))))));

        sql = "ALTER ROLE role_name RESET ALL";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident("role_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.Reset(
                    ConfigName: new ResetConfig.ALL(),
                    InDatabase: null)));

        sql = "ALTER ROLE role_name IN DATABASE database_name RESET maintenance_work_mem";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.AlterRole(
                Name: new Ident("role_name", null, Span.Empty()),
                Operation: new AlterRoleOperation.Reset(
                    ConfigName: new ResetConfig.ConfigName(ObjectName.From(Sequence.Of(new Ident("maintenance_work_mem", null, Span.Empty())))),
                    InDatabase: ObjectName.From(Sequence.Of(new Ident("database_name", null, Span.Empty()))))));
    }

    [Fact]
    public void parse_alter_user()
    {
        // `ALTER USER` is a PostgreSQL synonym for `ALTER ROLE`, so it round-trips to `ALTER ROLE`.
        var canonical = "ALTER ROLE old_name RENAME TO new_name";
        Assert.Equal(
            Pg().OneStatementParsesTo("ALTER USER old_name RENAME TO new_name", canonical),
            new Statement.AlterRole(
                Name: new Ident("old_name"),
                Operation: new AlterRoleOperation.RenameRole(
                    RoleName: new Ident("new_name"))));

        canonical = "ALTER ROLE bob WITH SUPERUSER PASSWORD 'x' CONNECTION LIMIT 5";
        Assert.Equal(
            Pg().OneStatementParsesTo(
                "ALTER USER bob WITH SUPERUSER PASSWORD 'x' CONNECTION LIMIT 5",
                canonical),
            new Statement.AlterRole(
                Name: new Ident("bob"),
                Operation: new AlterRoleOperation.WithOptions(
                    Options: Sequence.Of<RoleOption>(
                        new RoleOption.SuperUser(true),
                        new RoleOption.Password(new Password.PasswordValue(new Expr.Value(
                            new Value.SingleQuotedString("x").WithEmptySpan()))),
                        new RoleOption.ConnectionLimit(Expr.MakeValue(TestUtils.Number("5")))))));

        Assert.Equal(
            Pg().OneStatementParsesTo(
                "ALTER USER bob SET search_path TO public",
                "ALTER ROLE bob SET search_path TO public"),
            new Statement.AlterRole(
                Name: new Ident("bob"),
                Operation: new AlterRoleOperation.Set(
                    ConfigName: ObjectName.From(Sequence.Of(new Ident("search_path"))),
                    ConfigValue: new SetConfigValue.Value(new Expr.Identifier(new Ident("public"))),
                    InDatabase: null)));
    }

    [Fact]
    public void parse_delimited_identifiers()
    {
        // check that quoted identifiers in any position remain quoted after serialization
        var select = Pg().VerifiedOnlySelect(
            "SELECT \"alias\".\"bar baz\", \"myfun\"(), \"simple id\" AS \"column alias\" FROM \"a table\" AS \"alias\"");
        // check FROM
        switch (TestUtils.Only(select.From).Relation)
        {
            case TableFactor.Table
            {
                Name: var name,
                Alias: var alias,
                Args: var args,
                WithHints: var withHints,
                Version: var version,
            }:
                Assert.Equal(
                    ObjectName.From(Sequence.Of(Ident.WithQuote('"', "a table"))),
                    name);
                Assert.Equal(Ident.WithQuote('"', "alias"), Unwrap(alias).Name);
                Assert.Null(args);
                Assert.True(withHints.IsEmpty);
                Assert.Null(version);
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
            case SelectItem.ExprWithAlias { Expr: var expr, Alias: var alias }:
                Assert.Equal(new Expr.Identifier(Ident.WithQuote('"', "simple id")), expr);
                Assert.Equal(Ident.WithQuote('"', "column alias"), alias);
                break;
            default:
                Assert.Fail("Expected: ExprWithAlias");
                break;
        }

        Pg().VerifiedStmt("CREATE TABLE \"foo\" (\"bar\" \"int\")");
        Pg().VerifiedStmt("ALTER TABLE foo ADD CONSTRAINT \"bar\" PRIMARY KEY (baz)");
        Pg().VerifiedStmt("UPDATE foo SET \"bar\" = 5");
    }

    [Fact]
    public void parse_update_has_keyword()
    {
        Pg().OneStatementParsesTo(
            "UPDATE test SET name=$1,\n"
            + "                value=$2,\n"
            + "                where=$3,\n"
            + "                create=$4,\n"
            + "                is_default=$5,\n"
            + "                classification=$6,\n"
            + "                sort=$7\n"
            + "                WHERE id=$8",
            "UPDATE test SET name = $1, value = $2, where = $3, create = $4, is_default = $5, classification = $6, sort = $7 WHERE id = $8");
    }

    [Fact]
    public void parse_update_in_with_subquery()
    {
        PgAndGeneric().VerifiedStmt("WITH \"result\" AS (UPDATE \"Hero\" SET \"name\" = 'Captain America', \"number_of_movies\" = \"number_of_movies\" + 1 WHERE \"secret_identity\" = 'Sam Wilson' RETURNING \"id\", \"name\", \"secret_identity\", \"number_of_movies\") SELECT * FROM \"result\"");
    }

    [Fact]
    public void parser_create_function_with_args()
    {
        var sql1 = "CREATE OR REPLACE FUNCTION check_strings_different(str1 VARCHAR, str2 VARCHAR) RETURNS BOOLEAN LANGUAGE plpgsql AS $$\n"
            + "BEGIN\n"
            + "    IF str1 <> str2 THEN\n"
            + "        RETURN TRUE;\n"
            + "    ELSE\n"
            + "        RETURN FALSE;\n"
            + "    END IF;\n"
            + "END;\n"
            + "$$";

        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql1),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: true,
                Temporary: false,
                Name: ObjectName.From(Sequence.Of(new Ident("check_strings_different"))),
                Args: Sequence.Of(
                    OperateFunctionArg.WithName(
                        "str1",
                        new DataType.Varchar(null)),
                    OperateFunctionArg.WithName(
                        "str2",
                        new DataType.Varchar(null))),
                ReturnType: new FunctionReturnType.DataType(new DataType.Boolean()),
                Language: Ident.From("plpgsql"),
                Behavior: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                FunctionBody: new CreateFunctionBody.AsBeforeOptions(
                    Body: new Expr.Value(
                        new Value.DollarQuotedString(new DollarQuotedString(Value: "\nBEGIN\n    IF str1 <> str2 THEN\n        RETURN TRUE;\n    ELSE\n        RETURN FALSE;\n    END IF;\nEND;\n", Tag: null)).WithEmptySpan()),
                    LinkSymbol: null),
                IfNotExists: false,
                Using: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));

        var sql2 = "CREATE OR REPLACE FUNCTION check_not_zero(int1 INT) RETURNS BOOLEAN LANGUAGE plpgsql AS $$\n"
            + "BEGIN\n"
            + "    IF int1 <> 0 THEN\n"
            + "        RETURN TRUE;\n"
            + "    ELSE\n"
            + "        RETURN FALSE;\n"
            + "    END IF;\n"
            + "END;\n"
            + "$$";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql2),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: true,
                Temporary: false,
                Name: ObjectName.From(Sequence.Of(new Ident("check_not_zero"))),
                Args: Sequence.Of(
                    OperateFunctionArg.WithName(
                        "int1",
                        new DataType.Int(null))),
                ReturnType: new FunctionReturnType.DataType(new DataType.Boolean()),
                Language: Ident.From("plpgsql"),
                Behavior: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                FunctionBody: new CreateFunctionBody.AsBeforeOptions(
                    Body: new Expr.Value(
                        new Value.DollarQuotedString(new DollarQuotedString(Value: "\nBEGIN\n    IF int1 <> 0 THEN\n        RETURN TRUE;\n    ELSE\n        RETURN FALSE;\n    END IF;\nEND;\n", Tag: null)).WithEmptySpan()),
                    LinkSymbol: null),
                IfNotExists: false,
                Using: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));

        var sql3 = "CREATE OR REPLACE FUNCTION check_values_different(a INT, b INT) RETURNS BOOLEAN LANGUAGE plpgsql AS $$\n"
            + "BEGIN\n"
            + "    IF a <> b THEN\n"
            + "        RETURN TRUE;\n"
            + "    ELSE\n"
            + "        RETURN FALSE;\n"
            + "    END IF;\n"
            + "END;\n"
            + "$$";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql3),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: true,
                Temporary: false,
                Name: ObjectName.From(Sequence.Of(new Ident("check_values_different"))),
                Args: Sequence.Of(
                    OperateFunctionArg.WithName(
                        "a",
                        new DataType.Int(null)),
                    OperateFunctionArg.WithName(
                        "b",
                        new DataType.Int(null))),
                ReturnType: new FunctionReturnType.DataType(new DataType.Boolean()),
                Language: Ident.From("plpgsql"),
                Behavior: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                FunctionBody: new CreateFunctionBody.AsBeforeOptions(
                    Body: new Expr.Value(
                        new Value.DollarQuotedString(new DollarQuotedString(Value: "\nBEGIN\n    IF a <> b THEN\n        RETURN TRUE;\n    ELSE\n        RETURN FALSE;\n    END IF;\nEND;\n", Tag: null)).WithEmptySpan()),
                    LinkSymbol: null),
                IfNotExists: false,
                Using: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));

        var sql4 = "CREATE OR REPLACE FUNCTION check_values_different(int1 INT, int2 INT) RETURNS BOOLEAN LANGUAGE plpgsql AS $$\n"
            + "BEGIN\n"
            + "    IF int1 <> int2 THEN\n"
            + "        RETURN TRUE;\n"
            + "    ELSE\n"
            + "        RETURN FALSE;\n"
            + "    END IF;\n"
            + "END;\n"
            + "$$";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql4),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: true,
                Temporary: false,
                Name: ObjectName.From(Sequence.Of(new Ident("check_values_different"))),
                Args: Sequence.Of(
                    OperateFunctionArg.WithName(
                        "int1",
                        new DataType.Int(null)),
                    OperateFunctionArg.WithName(
                        "int2",
                        new DataType.Int(null))),
                ReturnType: new FunctionReturnType.DataType(new DataType.Boolean()),
                Language: Ident.From("plpgsql"),
                Behavior: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                FunctionBody: new CreateFunctionBody.AsBeforeOptions(
                    Body: new Expr.Value(
                        new Value.DollarQuotedString(new DollarQuotedString(Value: "\nBEGIN\n    IF int1 <> int2 THEN\n        RETURN TRUE;\n    ELSE\n        RETURN FALSE;\n    END IF;\nEND;\n", Tag: null)).WithEmptySpan()),
                    LinkSymbol: null),
                IfNotExists: false,
                Using: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));

        var sql5 = "CREATE OR REPLACE FUNCTION foo(a TIMESTAMP WITH TIME ZONE, b VARCHAR) RETURNS BOOLEAN LANGUAGE plpgsql AS $$\n"
            + "    BEGIN\n"
            + "        RETURN TRUE;\n"
            + "    END;\n"
            + "    $$";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql5),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: true,
                Temporary: false,
                Name: ObjectName.From(Sequence.Of(new Ident("foo"))),
                Args: Sequence.Of(
                    OperateFunctionArg.WithName(
                        "a",
                        new DataType.Timestamp(null, new TimezoneInfo.WithTimeZone())),
                    OperateFunctionArg.WithName("b", new DataType.Varchar(null))),
                ReturnType: new FunctionReturnType.DataType(new DataType.Boolean()),
                Language: Ident.From("plpgsql"),
                Behavior: null,
                CalledOnNull: null,
                Parallel: null,
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                FunctionBody: new CreateFunctionBody.AsBeforeOptions(
                    Body: new Expr.Value(
                        new Value.DollarQuotedString(new DollarQuotedString(
                            Value: "\n    BEGIN\n        RETURN TRUE;\n    END;\n    ",
                            Tag: null))
                        .WithEmptySpan()),
                    LinkSymbol: null),
                IfNotExists: false,
                Using: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));

        var incorrectSql = "CREATE FUNCTION add(function(struct<a,b> int64), b INTEGER) RETURNS INTEGER LANGUAGE SQL IMMUTABLE STRICT PARALLEL SAFE AS 'select $1 + $2;'";
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(incorrectSql));
    }

    [Fact]
    public void parse_create_function()
    {
        var sql = "CREATE FUNCTION add(INTEGER, INTEGER) RETURNS INTEGER LANGUAGE SQL IMMUTABLE STRICT PARALLEL SAFE AS 'select $1 + $2;'";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: false,
                Temporary: false,
                Name: ObjectName.From(Sequence.Of(new Ident("add"))),
                Args: Sequence.Of(
                    OperateFunctionArg.Unnamed(new DataType.Integer(null)),
                    OperateFunctionArg.Unnamed(new DataType.Integer(null))),
                ReturnType: new FunctionReturnType.DataType(new DataType.Integer(null)),
                Language: Ident.From("SQL"),
                Behavior: new FunctionBehavior.Immutable(),
                CalledOnNull: new FunctionCalledOnNull.Strict(),
                Parallel: new FunctionParallel.Safe(),
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                FunctionBody: new CreateFunctionBody.AsBeforeOptions(
                    Body: new Expr.Value(
                        new Value.SingleQuotedString("select $1 + $2;").WithEmptySpan()),
                    LinkSymbol: null),
                IfNotExists: false,
                Using: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));
    }

    [Fact]
    public void parse_create_function_detailed()
    {
        PgAndGeneric().VerifiedStmt("CREATE OR REPLACE FUNCTION add(a INTEGER, IN b INTEGER = 1) RETURNS INTEGER LANGUAGE SQL IMMUTABLE PARALLEL RESTRICTED RETURN a + b");
        PgAndGeneric().VerifiedStmt("CREATE OR REPLACE FUNCTION add(a INTEGER, IN b INTEGER = 1) RETURNS INTEGER LANGUAGE SQL IMMUTABLE RETURNS NULL ON NULL INPUT PARALLEL RESTRICTED RETURN a + b");
        PgAndGeneric().VerifiedStmt("CREATE OR REPLACE FUNCTION add(a INTEGER, IN b INTEGER = 1) RETURNS INTEGER LANGUAGE SQL STABLE PARALLEL UNSAFE RETURN a + b");
        PgAndGeneric().VerifiedStmt("CREATE OR REPLACE FUNCTION add(a INTEGER, IN b INTEGER = 1) RETURNS INTEGER LANGUAGE SQL STABLE CALLED ON NULL INPUT PARALLEL UNSAFE RETURN a + b");
        PgAndGeneric().VerifiedStmt("CREATE OR REPLACE FUNCTION increment(i INTEGER) RETURNS INTEGER LANGUAGE plpgsql AS $$ BEGIN RETURN i + 1; END; $$");
        PgAndGeneric().VerifiedStmt("CREATE OR REPLACE FUNCTION no_arg() RETURNS VOID LANGUAGE plpgsql AS $$ BEGIN DELETE FROM my_table; END; $$");
        PgAndGeneric().VerifiedStmt("CREATE OR REPLACE FUNCTION return_table(i INTEGER) RETURNS TABLE(id UUID, is_active BOOLEAN) LANGUAGE plpgsql AS $$ BEGIN RETURN QUERY SELECT NULL::UUID, NULL::BOOLEAN; END; $$");
        PgAndGeneric().OneStatementParsesTo(
            "CREATE FUNCTION add(INTEGER, INTEGER DEFAULT 1) RETURNS INTEGER AS 'select $1 + $2;'",
            "CREATE FUNCTION add(INTEGER, INTEGER = 1) RETURNS INTEGER AS 'select $1 + $2;'");
    }

    [Fact]
    public void parse_create_function_returns_setof()
    {
        PgAndGeneric().VerifiedStmt(
            "CREATE FUNCTION get_users() RETURNS SETOF TEXT LANGUAGE sql AS 'SELECT name FROM users'");
        PgAndGeneric().VerifiedStmt(
            "CREATE FUNCTION get_ids() RETURNS SETOF INTEGER LANGUAGE sql AS 'SELECT id FROM users'");
        PgAndGeneric().VerifiedStmt(
            "CREATE FUNCTION get_all() RETURNS SETOF my_schema.\"MyType\" LANGUAGE sql AS 'SELECT * FROM t'");
        PgAndGeneric().VerifiedStmt(
            "CREATE FUNCTION get_rows() RETURNS SETOF RECORD LANGUAGE sql AS 'SELECT * FROM t'");

        var sql = "CREATE FUNCTION get_names() RETURNS SETOF TEXT LANGUAGE sql AS 'SELECT name FROM t'";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateFunction { Value.ReturnType: var returnType }:
                Assert.Equal(returnType, new FunctionReturnType.SetOf(new DataType.Text()));
                break;
            default:
                Assert.Fail("Expected CreateFunction");
                break;
        }
    }

    [Fact]
    public void parse_create_function_with_security()
    {
        var sql =
            "CREATE FUNCTION test_fn() RETURNS void LANGUAGE sql SECURITY DEFINER AS $$ SELECT 1 $$";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateFunction { Value.Security: var security }:
                Assert.Equal(security, new FunctionSecurity.Definer());
                break;
            default:
                Assert.Fail("Expected CreateFunction");
                break;
        }

        var sql2 =
            "CREATE FUNCTION test_fn() RETURNS void LANGUAGE sql SECURITY INVOKER AS $$ SELECT 1 $$";
        switch (PgAndGeneric().VerifiedStmt(sql2))
        {
            case Statement.CreateFunction { Value.Security: var security }:
                Assert.Equal(security, new FunctionSecurity.Invoker());
                break;
            default:
                Assert.Fail("Expected CreateFunction");
                break;
        }
    }

    [Fact]
    public void parse_create_function_with_set_params()
    {
        var sql =
            "CREATE FUNCTION test_fn() RETURNS void LANGUAGE sql SET search_path = auth, pg_temp, public AS $$ SELECT 1 $$";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateFunction { Value.SetParams: var setParams }:
                Assert.Equal(1, setParams.Count);
                Assert.Equal("search_path", setParams[0].Name.ToString());
                break;
            default:
                Assert.Fail("Expected CreateFunction");
                break;
        }

        // Test multiple SET params
        var sql2 =
            "CREATE FUNCTION test_fn() RETURNS void LANGUAGE sql SET search_path = public SET statement_timeout = '5s' AS $$ SELECT 1 $$";
        switch (PgAndGeneric().VerifiedStmt(sql2))
        {
            case Statement.CreateFunction { Value.SetParams: var setParams }:
                Assert.Equal(2, setParams.Count);
                break;
            default:
                Assert.Fail("Expected CreateFunction");
                break;
        }

        // Test FROM CURRENT
        var sql3 =
            "CREATE FUNCTION test_fn() RETURNS void LANGUAGE sql SET search_path FROM CURRENT AS $$ SELECT 1 $$";
        switch (PgAndGeneric().VerifiedStmt(sql3))
        {
            case Statement.CreateFunction { Value.SetParams: var setParams }:
                Assert.Equal(1, setParams.Count);
                Assert.True(setParams[0].Value is FunctionSetValue.FromCurrent);
                break;
            default:
                Assert.Fail("Expected CreateFunction");
                break;
        }
    }

    [Fact]
    public void parse_incorrect_create_function_parallel()
    {
        var sql = "CREATE FUNCTION add(INTEGER, INTEGER) RETURNS INTEGER LANGUAGE SQL PARALLEL BLAH AS 'select $1 + $2;'";
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(sql));
    }

    [Fact]
    public void parse_create_function_c_with_module_pathname()
    {
        var sql = "CREATE FUNCTION cas_in(input cstring) RETURNS cas LANGUAGE c IMMUTABLE PARALLEL SAFE AS 'MODULE_PATHNAME', 'cas_in_wrapper'";
        Assert.Equal(
            PgAndGeneric().VerifiedStmt(sql),
            new Statement.CreateFunction(new CreateFunction(
                OrAlter: false,
                OrReplace: false,
                Temporary: false,
                Name: ObjectName.From(Sequence.Of(new Ident("cas_in"))),
                Args: Sequence.Of(OperateFunctionArg.WithName(
                    "input",
                    new DataType.Custom(ObjectName.From(Sequence.Of(new Ident("cstring"))), Sequence<string>.Empty))),
                ReturnType: new FunctionReturnType.DataType(new DataType.Custom(
                    ObjectName.From(Sequence.Of(new Ident("cas"))),
                    Sequence<string>.Empty)),
                Language: Ident.From("c"),
                Behavior: new FunctionBehavior.Immutable(),
                CalledOnNull: null,
                Parallel: new FunctionParallel.Safe(),
                Security: null,
                SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
                FunctionBody: new CreateFunctionBody.AsBeforeOptions(
                    Body: new Expr.Value(
                        new Value.SingleQuotedString("MODULE_PATHNAME").WithEmptySpan()),
                    LinkSymbol: new Expr.Value(
                        new Value.SingleQuotedString("cas_in_wrapper").WithEmptySpan())),
                IfNotExists: false,
                Using: null,
                DeterminismSpecifier: null,
                Options: null,
                RemoteConnection: null)));

        // Test that attribute order flexibility works (IMMUTABLE before LANGUAGE)
        var sqlAltOrder = "CREATE FUNCTION cas_in(input cstring) RETURNS cas IMMUTABLE PARALLEL SAFE LANGUAGE c AS 'MODULE_PATHNAME', 'cas_in_wrapper'";
        PgAndGeneric().OneStatementParsesTo(
            sqlAltOrder,
            "CREATE FUNCTION cas_in(input cstring) RETURNS cas LANGUAGE c IMMUTABLE PARALLEL SAFE AS 'MODULE_PATHNAME', 'cas_in_wrapper'");
    }

    [Fact]
    public void parse_drop_function()
    {
        var sql = "DROP FUNCTION IF EXISTS test_func";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropFunction(new DropFunction(
                IfExists: true,
                FuncDesc: Sequence.Of(new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("test_func", null, Span.Empty()))),
                    Args: null)),
                DropBehavior: null)));

        sql = "DROP FUNCTION IF EXISTS test_func(a INTEGER, IN b INTEGER = 1)";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropFunction(new DropFunction(
                IfExists: true,
                FuncDesc: Sequence.Of(new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("test_func", null, Span.Empty()))),
                    Args: Sequence.Of(
                        OperateFunctionArg.WithName("a", new DataType.Integer(null)),
                        new OperateFunctionArg(
                            Mode: new ArgMode.In(),
                            Name: Ident.From("b"),
                            DataType: new DataType.Integer(null),
                            DefaultExpr: new Expr.Value(
                                new Value.Number("1", false).WithEmptySpan()))))),
                DropBehavior: null)));

        sql = "DROP FUNCTION IF EXISTS test_func1(a INTEGER, IN b INTEGER = 1), test_func2(a VARCHAR, IN b INTEGER = 1)";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropFunction(new DropFunction(
                IfExists: true,
                FuncDesc: Sequence.Of(
                    new FunctionDesc(
                        Name: ObjectName.From(Sequence.Of(new Ident("test_func1", null, Span.Empty()))),
                        Args: Sequence.Of(
                            OperateFunctionArg.WithName("a", new DataType.Integer(null)),
                            new OperateFunctionArg(
                                Mode: new ArgMode.In(),
                                Name: Ident.From("b"),
                                DataType: new DataType.Integer(null),
                                DefaultExpr: new Expr.Value(
                                    new Value.Number("1", false).WithEmptySpan())))),
                    new FunctionDesc(
                        Name: ObjectName.From(Sequence.Of(new Ident("test_func2", null, Span.Empty()))),
                        Args: Sequence.Of(
                            OperateFunctionArg.WithName("a", new DataType.Varchar(null)),
                            new OperateFunctionArg(
                                Mode: new ArgMode.In(),
                                Name: Ident.From("b"),
                                DataType: new DataType.Integer(null),
                                DefaultExpr: new Expr.Value(
                                    new Value.Number("1", false).WithEmptySpan()))))),
                DropBehavior: null)));
    }

    [Fact]
    public void parse_drop_domain()
    {
        var sql = "DROP DOMAIN IF EXISTS jpeg_domain";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropDomain(new DropDomain(
                IfExists: true,
                Name: ObjectName.From(Sequence.Of(new Ident("jpeg_domain", null, Span.Empty()))),
                DropBehavior: null)));

        sql = "DROP DOMAIN jpeg_domain";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropDomain(new DropDomain(
                IfExists: false,
                Name: ObjectName.From(Sequence.Of(new Ident("jpeg_domain", null, Span.Empty()))),
                DropBehavior: null)));

        sql = "DROP DOMAIN IF EXISTS jpeg_domain CASCADE";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropDomain(new DropDomain(
                IfExists: true,
                Name: ObjectName.From(Sequence.Of(new Ident("jpeg_domain", null, Span.Empty()))),
                DropBehavior: new DropBehavior.Cascade())));

        sql = "DROP DOMAIN IF EXISTS jpeg_domain RESTRICT";

        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropDomain(new DropDomain(
                IfExists: true,
                Name: ObjectName.From(Sequence.Of(new Ident("jpeg_domain", null, Span.Empty()))),
                DropBehavior: new DropBehavior.Restrict())));
    }

    [Fact]
    public void parse_drop_procedure()
    {
        var sql = "DROP PROCEDURE IF EXISTS test_proc";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropProcedure(
                IfExists: true,
                ProcDesc: Sequence.Of(new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("test_proc", null, Span.Empty()))),
                    Args: null)),
                DropBehavior: null));

        sql = "DROP PROCEDURE IF EXISTS test_proc(a INTEGER, IN b INTEGER = 1)";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropProcedure(
                IfExists: true,
                ProcDesc: Sequence.Of(new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("test_proc", null, Span.Empty()))),
                    Args: Sequence.Of(
                        OperateFunctionArg.WithName("a", new DataType.Integer(null)),
                        new OperateFunctionArg(
                            Mode: new ArgMode.In(),
                            Name: Ident.From("b"),
                            DataType: new DataType.Integer(null),
                            DefaultExpr: new Expr.Value(
                                new Value.Number("1", false).WithEmptySpan()))))),
                DropBehavior: null));

        sql = "DROP PROCEDURE IF EXISTS test_proc1(a INTEGER, IN b INTEGER = 1), test_proc2(a VARCHAR, IN b INTEGER = 1)";
        Assert.Equal(
            Pg().VerifiedStmt(sql),
            new Statement.DropProcedure(
                IfExists: true,
                ProcDesc: Sequence.Of(
                    new FunctionDesc(
                        Name: ObjectName.From(Sequence.Of(new Ident("test_proc1", null, Span.Empty()))),
                        Args: Sequence.Of(
                            OperateFunctionArg.WithName("a", new DataType.Integer(null)),
                            new OperateFunctionArg(
                                Mode: new ArgMode.In(),
                                Name: Ident.From("b"),
                                DataType: new DataType.Integer(null),
                                DefaultExpr: new Expr.Value(
                                    new Value.Number("1", false).WithEmptySpan())))),
                    new FunctionDesc(
                        Name: ObjectName.From(Sequence.Of(new Ident("test_proc2", null, Span.Empty()))),
                        Args: Sequence.Of(
                            OperateFunctionArg.WithName("a", new DataType.Varchar(null)),
                            new OperateFunctionArg(
                                Mode: new ArgMode.In(),
                                Name: Ident.From("b"),
                                DataType: new DataType.Integer(null),
                                DefaultExpr: new Expr.Value(
                                    new Value.Number("1", false).WithEmptySpan()))))),
                DropBehavior: null));

        var ex = Assert.Throws<ParserException>(() => Pg().ParseSqlStatements("DROP PROCEDURE testproc DROP"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: DROP"),
            ex.Error);

        ex = Assert.Throws<ParserException>(() => Pg().ParseSqlStatements("DROP PROCEDURE testproc SET NULL"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: end of statement, found: SET"),
            ex.Error);
    }

    [Fact]
    public void parse_dollar_quoted_string()
    {
        var sql = "SELECT $$hello$$, $tag_name$world$tag_name$, $$Foo$Bar$$, $$Foo$Bar$$col_name, $$$$, $tag_name$$tag_name$";

        var stmt = Pg().ParseSqlStatements(sql);

        var projection = Unwrap(stmt.First) switch
        {
            Statement.Query { Value: var query } => query.Body switch
            {
                SetExpr.Select { Value: var select } => select.Projection,
                _ => throw new InvalidOperationException("unreachable"),
            },
            _ => throw new InvalidOperationException("unreachable"),
        };

        Assert.Equal(
            new Expr.Value(
                new Value.DollarQuotedString(new DollarQuotedString(
                    Tag: null,
                    Value: "hello"))
                .WithEmptySpan()),
            TestUtils.ExprFromProjection(projection[0]));

        Assert.Equal(
            new Expr.Value(
                new Value.DollarQuotedString(new DollarQuotedString(
                    Tag: "tag_name",
                    Value: "world"))
                .WithEmptySpan()),
            TestUtils.ExprFromProjection(projection[1]));

        Assert.Equal(
            new Expr.Value(
                new Value.DollarQuotedString(new DollarQuotedString(
                    Tag: null,
                    Value: "Foo$Bar"))
                .WithEmptySpan()),
            TestUtils.ExprFromProjection(projection[2]));

        Assert.Equal(
            projection[3],
            new SelectItem.ExprWithAlias(
                Expr: new Expr.Value(
                    new Value.DollarQuotedString(new DollarQuotedString(
                        Tag: null,
                        Value: "Foo$Bar"))
                    .WithEmptySpan()),
                Alias: new Ident("col_name", null, Span.Empty())));

        Assert.Equal(
            TestUtils.ExprFromProjection(projection[4]),
            new Expr.Value(
                new Value.DollarQuotedString(new DollarQuotedString(
                    Tag: null,
                    Value: ""))
                .WithEmptySpan()));

        Assert.Equal(
            TestUtils.ExprFromProjection(projection[5]),
            new Expr.Value(
                new Value.DollarQuotedString(new DollarQuotedString(
                    Tag: "tag_name",
                    Value: ""))
                .WithEmptySpan()));
    }

    [Fact]
    public void parse_incorrect_dollar_quoted_string()
    {
        var sql = "SELECT $x$hello$$";
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(sql));

        sql = "SELECT $hello$$";
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(sql));

        sql = "SELECT $$$";
        Assert.Throws<ParserException>(() => Pg().ParseSqlStatements(sql));
    }

    [Fact]
    public void parse_select_group_by_grouping_sets()
    {
        var select = PgAndGeneric().VerifiedOnlySelect(
            "SELECT brand, size, sum(sales) FROM items_sold GROUP BY size, GROUPING SETS ((brand), (size), ())");
        Assert.Equal(
            new GroupByExpr.Expressions(
                Sequence.Of<Expr>(
                    new Expr.Identifier(new Ident("size")),
                    new Expr.GroupingSets(Sequence.Of(
                        Sequence.Of<Expr>(new Expr.Identifier(new Ident("brand"))),
                        Sequence.Of<Expr>(new Expr.Identifier(new Ident("size"))),
                        Sequence<Expr>.Empty))),
                Sequence<GroupByWithModifier>.Empty),
            select.GroupBy);
    }

    [Fact]
    public void parse_select_group_by_rollup()
    {
        var select = PgAndGeneric().VerifiedOnlySelect(
            "SELECT brand, size, sum(sales) FROM items_sold GROUP BY size, ROLLUP (brand, size)");
        Assert.Equal(
            new GroupByExpr.Expressions(
                Sequence.Of<Expr>(
                    new Expr.Identifier(new Ident("size")),
                    new Expr.Rollup(Sequence.Of(
                        Sequence.Of<Expr>(new Expr.Identifier(new Ident("brand"))),
                        Sequence.Of<Expr>(new Expr.Identifier(new Ident("size")))))),
                Sequence<GroupByWithModifier>.Empty),
            select.GroupBy);
    }

    [Fact]
    public void parse_select_group_by_cube()
    {
        var select = PgAndGeneric().VerifiedOnlySelect(
            "SELECT brand, size, sum(sales) FROM items_sold GROUP BY size, CUBE (brand, size)");
        Assert.Equal(
            new GroupByExpr.Expressions(
                Sequence.Of<Expr>(
                    new Expr.Identifier(new Ident("size")),
                    new Expr.Cube(Sequence.Of(
                        Sequence.Of<Expr>(new Expr.Identifier(new Ident("brand"))),
                        Sequence.Of<Expr>(new Expr.Identifier(new Ident("size")))))),
                Sequence<GroupByWithModifier>.Empty),
            select.GroupBy);
    }

    [Fact]
    public void parse_truncate()
    {
        var truncate = PgAndGeneric().VerifiedStmt("TRUNCATE db.table_name");
        var tableName = ObjectName.From(Sequence.Of(new Ident("db"), new Ident("table_name")));
        var tableNames = Sequence.Of(new TruncateTableTarget(
            Name: tableName,
            Only: false,
            HasAsterisk: false));
        Assert.Equal(
            new Statement.Truncate(new Truncate(
                TableNames: tableNames,
                Partitions: null,
                Table: false,
                IfExists: false,
                Identity: null,
                Cascade: null,
                OnCluster: null)),
            truncate);
    }

    [Fact]
    public void parse_truncate_with_options()
    {
        var truncate = PgAndGeneric()
            .VerifiedStmt("TRUNCATE TABLE ONLY db.table_name RESTART IDENTITY CASCADE");

        var tableName = ObjectName.From(Sequence.Of(new Ident("db"), new Ident("table_name")));
        var tableNames = Sequence.Of(new TruncateTableTarget(
            Name: tableName,
            Only: true,
            HasAsterisk: false));

        Assert.Equal(
            new Statement.Truncate(new Truncate(
                TableNames: tableNames,
                Partitions: null,
                Table: true,
                IfExists: false,
                Identity: TruncateIdentityOption.Restart,
                Cascade: new CascadeOption.Cascade(),
                OnCluster: null)),
            truncate);
    }

    [Fact]
    public void parse_truncate_with_table_list()
    {
        var truncate = Pg().VerifiedStmt(
            "TRUNCATE TABLE db.table_name, db.other_table_name RESTART IDENTITY CASCADE");

        var tableNameA = ObjectName.From(Sequence.Of(new Ident("db"), new Ident("table_name")));
        var tableNameB = ObjectName.From(Sequence.Of(new Ident("db"), new Ident("other_table_name")));

        var tableNames = Sequence.Of(
            new TruncateTableTarget(
                Name: tableNameA,
                Only: false,
                HasAsterisk: false),
            new TruncateTableTarget(
                Name: tableNameB,
                Only: false,
                HasAsterisk: false));

        Assert.Equal(
            new Statement.Truncate(new Truncate(
                TableNames: tableNames,
                Partitions: null,
                Table: true,
                IfExists: false,
                Identity: TruncateIdentityOption.Restart,
                Cascade: new CascadeOption.Cascade(),
                OnCluster: null)),
            truncate);
    }

    [Fact]
    public void parse_truncate_with_descendant()
    {
        var truncate = PgAndGeneric().VerifiedStmt("TRUNCATE TABLE t *");

        var tableNames = Sequence.Of(new TruncateTableTarget(
            Name: ObjectName.From(Sequence.Of(new Ident("t"))),
            Only: false,
            HasAsterisk: true));

        Assert.Equal(
            new Statement.Truncate(new Truncate(
                TableNames: tableNames,
                Partitions: null,
                Table: true,
                IfExists: false,
                Identity: null,
                Cascade: null,
                OnCluster: null)),
            truncate);

        truncate = PgAndGeneric()
            .VerifiedStmt("TRUNCATE TABLE ONLY parent, child *, grandchild RESTART IDENTITY");

        tableNames = Sequence.Of(
            new TruncateTableTarget(
                Name: ObjectName.From(Sequence.Of(new Ident("parent"))),
                Only: true,
                HasAsterisk: false),
            new TruncateTableTarget(
                Name: ObjectName.From(Sequence.Of(new Ident("child"))),
                Only: false,
                HasAsterisk: true),
            new TruncateTableTarget(
                Name: ObjectName.From(Sequence.Of(new Ident("grandchild"))),
                Only: false,
                HasAsterisk: false));

        Assert.Equal(
            new Statement.Truncate(new Truncate(
                TableNames: tableNames,
                Partitions: null,
                Table: true,
                IfExists: false,
                Identity: TruncateIdentityOption.Restart,
                Cascade: null,
                OnCluster: null)),
            truncate);
    }

    [Fact]
    public void parse_select_regexp_as_column_name()
    {
        PgAndGeneric().VerifiedOnlySelect(
            "SELECT REGEXP.REGEXP AS REGEXP FROM REGEXP AS REGEXP WHERE REGEXP.REGEXP");
    }

    [Fact]
    public void parse_create_table_with_alias()
    {
        var sql = "CREATE TABLE public.datatype_aliases\n"
            + "    (\n"
            + "      int8_col INT8,\n"
            + "      int4_col INT4,\n"
            + "      int2_col INT2,\n"
            + "      float8_col FLOAT8,\n"
            + "      float4_col FLOAT4,\n"
            + "      bool_col BOOL\n"
            + "    );";
        switch (PgAndGeneric().OneStatementParsesTo(sql, ""))
        {
            case Statement.CreateTable
            {
                Value:
                {
                    Name: var name,
                    Columns: var columns,
                    Constraints: var constraints,
                    IfNotExists: false,
                    External: false,
                    FileFormat: null,
                    Location: null,
                },
            }:
                Assert.Equal("public.datatype_aliases", name.ToString());
                Assert.Equal(
                    columns,
                    Sequence.Of(
                        new ColumnDef(
                            Name: Ident.From("int8_col"),
                            DataType: new DataType.Int8(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: Ident.From("int4_col"),
                            DataType: new DataType.Int4(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: Ident.From("int2_col"),
                            DataType: new DataType.Int2(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: Ident.From("float8_col"),
                            DataType: new DataType.Float8(),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: Ident.From("float4_col"),
                            DataType: new DataType.Float4(),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: Ident.From("bool_col"),
                            DataType: new DataType.Bool(),
                            Options: Sequence<ColumnOptionDef>.Empty)));
                Assert.True(constraints.IsEmpty);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }
    }

    [Fact]
    public void parse_create_table_with_partition_by()
    {
        var sql = "CREATE TABLE t1 (a INT, b TEXT) PARTITION BY RANGE(a)";
        switch (PgAndGeneric().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value: var createTable }:
                Assert.Equal("t1", createTable.Name.ToString());
                Assert.Equal(
                    Sequence.Of(
                        new ColumnDef(
                            Name: Ident.From("a"),
                            DataType: new DataType.Int(null),
                            Options: Sequence<ColumnOptionDef>.Empty),
                        new ColumnDef(
                            Name: Ident.From("b"),
                            DataType: new DataType.Text(),
                            Options: Sequence<ColumnOptionDef>.Empty)),
                    createTable.Columns);
                switch (Unwrap(createTable.PartitionBy))
                {
                    case Expr.Function { Value: var f }:
                        Assert.Equal("RANGE", f.Name.ToString());
                        Assert.Equal(
                            new FunctionArguments.List(new FunctionArgumentList(
                                DuplicateTreatment: null,
                                Clauses: Sequence<FunctionArgumentClause>.Empty,
                                Args: Sequence.Of<FunctionArg>(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(
                                    new Expr.Identifier(new Ident("a"))))))),
                            f.Args);
                        break;
                    default:
                        Assert.Fail("unreachable");
                        break;
                }
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }
    }

    [Fact]
    public void parse_create_unlogged_table()
    {
        var sql = "CREATE UNLOGGED TABLE public.unlogged2 (a int primary key)";
        switch (PgAndGeneric().OneStatementParsesTo(
            sql,
            "CREATE UNLOGGED TABLE public.unlogged2 (a INT PRIMARY KEY)"))
        {
            case Statement.CreateTable { Value: { Name: var name, Unlogged: var unlogged } }:
                Assert.True(unlogged);
                Assert.Equal("public.unlogged2", name.ToString());
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }

        sql = "CREATE UNLOGGED TABLE pg_temp.unlogged3 (a int primary key)";
        switch (PgAndGeneric().OneStatementParsesTo(
            sql,
            "CREATE UNLOGGED TABLE pg_temp.unlogged3 (a INT PRIMARY KEY)"))
        {
            case Statement.CreateTable { Value: { Name: var name, Unlogged: var unlogged } }:
                Assert.True(unlogged);
                Assert.Equal("pg_temp.unlogged3", name.ToString());
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }

        sql = "CREATE UNLOGGED TABLE unlogged1 (a int) PARTITION BY RANGE (a)";
        switch (PgAndGeneric().OneStatementParsesTo(
            sql,
            "CREATE UNLOGGED TABLE unlogged1 (a INT) PARTITION BY RANGE(a)"))
        {
            case Statement.CreateTable
            {
                Value:
                {
                    Name: var name,
                    Unlogged: var unlogged,
                    PartitionBy: var partitionBy,
                },
            }:
                Assert.True(unlogged);
                Assert.Equal("unlogged1", name.ToString());
                Assert.NotNull(partitionBy);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }

        var ex = Assert.Throws<ParserException>(() => Pg().ParseSqlStatements("CREATE UNLOGGED VIEW v AS SELECT 1"));
        Assert.Equal(
            new ParserError.ParserErrorValue("Expected: an object type after CREATE, found: UNLOGGED"),
            ex.Error);
    }

    [Fact]
    public void parse_join_constraint_unnest_alias()
    {
        Assert.Equal(
            TestUtils.Only(
                Pg().VerifiedOnlySelect("SELECT * FROM t1 JOIN UNNEST(t1.a) AS f ON c1 = c2")
                    .From)
            .Joins,
            Sequence.Of(new Join(
                Relation: new TableFactor.UNNEST(
                    Alias: TestUtils.TableAlias(true, "f"),
                    ArrayExprs: Sequence.Of<Expr>(new Expr.CompoundIdentifier(Sequence.Of(
                        new Ident("t1"),
                        new Ident("a")))),
                    WithOffset: false,
                    WithOffsetAlias: null,
                    WithOrdinality: false),
                Global: false,
                JoinOperator: new JoinOperator.Join(new JoinConstraint.On(new Expr.BinaryOp(
                    Left: new Expr.Identifier(Ident.From("c1")),
                    Op: new BinaryOperator.Eq(),
                    Right: new Expr.Identifier(Ident.From("c2"))))))));
    }

    [Fact]
    public void test_complex_postgres_insert_with_alias()
    {
        var sql1 = "WITH existing AS (SELECT test_table.id FROM test_tables AS test_table WHERE (a = 12) AND (b = 34)), inserted AS (INSERT INTO test_tables AS test_table (id, a, b, c) VALUES (DEFAULT, 56, 78, 90) ON CONFLICT(a, b) DO UPDATE SET c = EXCLUDED.c WHERE (test_table.c <> EXCLUDED.c)) SELECT c FROM existing";

        Pg().VerifiedStmt(sql1);
    }

    // #[cfg(not(feature = "bigdecimal"))]
    [Fact]
    public void test_simple_postgres_insert_with_alias()
    {
        var sql2 = "INSERT INTO test_tables AS test_table (id, a) VALUES (DEFAULT, 123)";

        var statement = Pg().VerifiedStmt(sql2);

        Assert.Equal(
            statement,
            new Statement.Insert(new Insert(
                InsertToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Or: null,
                Ignore: false,
                Into: true,
                Table: new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("test_tables", null, Span.Empty())))),
                TableAlias: new TableAliasWithoutColumns(
                    Explicit: true,
                    Alias: new Ident("test_table", null, Span.Empty())),
                Columns: Sequence.Of(
                    ObjectName.From(new Ident("id", null, Span.Empty())),
                    ObjectName.From(new Ident("a", null, Span.Empty()))),
                ByName: false,
                Overwrite: false,
                Source: new Query(
                    With: null,
                    Body: new SetExpr.Values(new Values(
                        ValueKeyword: false,
                        ExplicitRow: false,
                        Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                            new Expr.Identifier(new Ident("DEFAULT")),
                            new Expr.Value(new Value.Number("123", false).WithEmptySpan())))))),
                    OrderBy: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty),
                Assignments: Sequence<Assignment>.Empty,
                Partitioned: null,
                AfterColumns: Sequence<Ident>.Empty,
                HasTableKeyword: false,
                On: null,
                Returning: null,
                Output: null,
                ReplaceInto: false,
                Priority: null,
                InsertAlias: null,
                Settings: null,
                FormatClause: null,
                MultiTableInsertType: null,
                MultiTableIntoClauses: Sequence<MultiTableInsertIntoClause>.Empty,
                MultiTableWhenClauses: Sequence<MultiTableInsertWhenClause>.Empty,
                MultiTableElseClause: null)));
    }

    // #[cfg(feature = "bigdecimal")] fn test_simple_postgres_insert_with_alias (sqlparser_postgres.rs:6151):
    // não portado; o Core não tem a feature `bigdecimal` (ver as notas no topo da classe).

    [Fact]
    public void test_simple_insert_with_quoted_alias()
    {
        var sql = "INSERT INTO test_tables AS \"Test_Table\" (id, a) VALUES (DEFAULT, '0123')";

        var statement = Pg().VerifiedStmt(sql);

        Assert.Equal(
            statement,
            new Statement.Insert(new Insert(
                InsertToken: AttachedToken.Empty(),
                OptimizerHints: Sequence<OptimizerHint>.Empty,
                Or: null,
                Ignore: false,
                Into: true,
                Table: new TableObject.TableName(ObjectName.From(Sequence.Of(new Ident("test_tables", null, Span.Empty())))),
                TableAlias: new TableAliasWithoutColumns(
                    Explicit: true,
                    Alias: new Ident("Test_Table", '"', Span.Empty())),
                Columns: Sequence.Of(
                    ObjectName.From(new Ident("id", null, Span.Empty())),
                    ObjectName.From(new Ident("a", null, Span.Empty()))),
                ByName: false,
                Overwrite: false,
                Source: new Query(
                    With: null,
                    Body: new SetExpr.Values(new Values(
                        ValueKeyword: false,
                        ExplicitRow: false,
                        Rows: Sequence.Of(Parens<Sequence<Expr>>.WithEmptySpan(Sequence.Of<Expr>(
                            new Expr.Identifier(new Ident("DEFAULT")),
                            new Expr.Value(
                                new Value.SingleQuotedString("0123").WithEmptySpan())))))),
                    OrderBy: null,
                    LimitClause: null,
                    Fetch: null,
                    Locks: Sequence<LockClause>.Empty,
                    ForClause: null,
                    Settings: null,
                    FormatClause: null,
                    PipeOperators: Sequence<PipeOperator>.Empty),
                Assignments: Sequence<Assignment>.Empty,
                Partitioned: null,
                AfterColumns: Sequence<Ident>.Empty,
                HasTableKeyword: false,
                On: null,
                Returning: null,
                Output: null,
                ReplaceInto: false,
                Priority: null,
                InsertAlias: null,
                Settings: null,
                FormatClause: null,
                MultiTableInsertType: null,
                MultiTableIntoClauses: Sequence<MultiTableInsertIntoClause>.Empty,
                MultiTableWhenClauses: Sequence<MultiTableInsertWhenClause>.Empty,
                MultiTableElseClause: null)));
    }

    [Fact]
    public void parse_array_agg()
    {
        // follows general function with wildcard code path
        var sql = "SELECT GREATEST(sections_tbl.*) AS sections FROM sections_tbl";
        Pg().VerifiedStmt(sql);

        // follows special-case array_agg code path
        var sql2 = "SELECT ARRAY_AGG(sections_tbl.*) AS sections FROM sections_tbl";
        Pg().VerifiedStmt(sql2);

        // handles multi-part identifier with general code path
        var sql3 = "SELECT GREATEST(my_schema.sections_tbl.*) AS sections FROM sections_tbl";
        Pg().VerifiedStmt(sql3);

        // handles multi-part identifier with array_agg code path
        var sql4 = "SELECT ARRAY_AGG(my_schema.sections_tbl.*) AS sections FROM sections_tbl";
        Pg().VerifiedStmt(sql4);
    }

    [Fact]
    public void parse_mat_cte()
    {
        var sql = "WITH cte AS MATERIALIZED (SELECT id FROM accounts) SELECT id FROM cte";
        Pg().VerifiedStmt(sql);

        var sql2 = "WITH cte AS NOT MATERIALIZED (SELECT id FROM accounts) SELECT id FROM cte";
        Pg().VerifiedStmt(sql2);
    }

    [Fact]
    public void parse_at_time_zone()
    {
        PgAndGeneric().VerifiedExpr("CURRENT_TIMESTAMP AT TIME ZONE tz");
        PgAndGeneric().VerifiedExpr("CURRENT_TIMESTAMP AT TIME ZONE ('America/' || 'Los_Angeles')");

        // check precedence
        var expr = new Expr.BinaryOp(
            Left: new Expr.AtTimeZone(
                Timestamp: new Expr.TypedString(new TypedString(
                    DataType: new DataType.Timestamp(null, new TimezoneInfo.None()),
                    Value: new ValueWithSpan(
                        Value: new Value.SingleQuotedString("2001-09-28 01:00"),
                        Span: Span.Empty()),
                    UsesOdbcSyntax: false)),
                TimeZone: new Expr.Cast(
                    Kind: CastKind.DoubleColon,
                    Expr: new Expr.Value(
                        new Value.SingleQuotedString("America/Los_Angeles").WithEmptySpan()),
                    DataType: new DataType.Text(),
                    Format: null)),
            Op: new BinaryOperator.Plus(),
            Right: new Expr.Interval(new Interval(
                Value: new Expr.Value(
                    new Value.SingleQuotedString("23 hours").WithEmptySpan()),
                LeadingField: null,
                LeadingPrecision: null,
                LastField: null,
                FractionalSecondsPrecision: null)));
        Assert.Equal(
            PgAndGeneric().VerifiedExpr(
                "TIMESTAMP '2001-09-28 01:00' AT TIME ZONE 'America/Los_Angeles'::TEXT + INTERVAL '23 hours'"),
            expr);
    }

    [Fact]
    public void parse_interval_data_type()
    {
        PgAndGeneric().VerifiedStmt("CREATE TABLE t (i INTERVAL)");
        for (var p = 0; p <= 6; p++)
        {
            PgAndGeneric().VerifiedStmt($"CREATE TABLE t (i INTERVAL({p}))");
            PgAndGeneric().VerifiedStmt($"SELECT '1 second'::INTERVAL({p})");
            PgAndGeneric().VerifiedStmt($"SELECT CAST('1 second' AS INTERVAL({p}))");
        }
        var fields = new[]
        {
            "YEAR",
            "MONTH",
            "DAY",
            "HOUR",
            "MINUTE",
            "SECOND",
            "YEAR TO MONTH",
            "DAY TO HOUR",
            "DAY TO MINUTE",
            "DAY TO SECOND",
            "HOUR TO MINUTE",
            "HOUR TO SECOND",
            "MINUTE TO SECOND",
        };
        foreach (var field in fields)
        {
            PgAndGeneric().VerifiedStmt($"CREATE TABLE t (i INTERVAL {field})");
            PgAndGeneric().VerifiedStmt($"SELECT '1 second'::INTERVAL {field}");
            PgAndGeneric().VerifiedStmt($"SELECT CAST('1 second' AS INTERVAL {field})");
        }
        for (var p = 0; p <= 6; p++)
        {
            foreach (var field in fields)
            {
                PgAndGeneric().VerifiedStmt($"CREATE TABLE t (i INTERVAL {field}({p}))");
                PgAndGeneric().VerifiedStmt($"SELECT '1 second'::INTERVAL {field}({p})");
                PgAndGeneric()
                    .VerifiedStmt($"SELECT CAST('1 second' AS INTERVAL {field}({p}))");
            }
        }
    }

    [Fact]
    public void parse_interval_keyword_as_unquoted_identifier()
    {
        Pg().VerifiedStmt("SELECT MAX(interval) FROM tbl");
        Pg().VerifiedExpr("INTERVAL '1 day'");
    }

    [Fact]
    public void parse_create_table_with_options()
    {
        var sql = "CREATE TABLE t (c INT) WITH (foo = 'bar', a = 123)";
        switch (Pg().VerifiedStmt(sql))
        {
            case Statement.CreateTable { Value.TableOptions: var tableOptions }:
                var withOptions = tableOptions switch
                {
                    CreateTableOptions.With { Value: var options } => options,
                    _ => throw new InvalidOperationException("unreachable"),
                };
                Assert.Equal(
                    Sequence.Of<SqlOption>(
                        new SqlOption.KeyValue(
                            Key: Ident.From("foo"),
                            Value: new Expr.Value(
                                new Value.SingleQuotedString("bar").WithEmptySpan())),
                        new SqlOption.KeyValue(
                            Key: Ident.From("a"),
                            Value: Expr.MakeValue(TestUtils.Number("123")))),
                    withOptions);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }
    }

    [Fact]
    public void test_table_function_with_ordinality()
    {
        var from = PgAndGeneric()
            .VerifiedOnlySelect("SELECT * FROM generate_series(1, 10) WITH ORDINALITY AS t")
            .From;
        Assert.Equal(1, from.Count);
        switch (from[0].Relation)
        {
            case TableFactor.Table
            {
                Name: var name,
                WithOrdinality: true,
            }:
                Assert.Equal("generate_series", name.ToString());
                break;
            default:
                Assert.Fail("Expecting TableFactor::Table with ordinality");
                break;
        }
    }

    [Fact]
    public void test_lateral_function_with_ordinality_and_column_aliases()
    {
        var from = Pg()
            .VerifiedOnlySelect(
                "SELECT * FROM tbl, "
                + "LATERAL json_array_elements(c1::JSON) "
                + "WITH ORDINALITY AS t (c1, index)")
            .From;
        Assert.Equal(2, from.Count);
        switch (from[1].Relation)
        {
            case TableFactor.Function
            {
                Lateral: true,
                Name: var name,
                WithOrdinality: true,
                Alias: { } alias,
            }:
                Assert.Equal("json_array_elements", name.ToString());
                Assert.Equal("t", alias.Name.Value);
                Assert.Equal(2, alias.Columns.Count);
                Assert.Equal("c1", alias.Columns[0].Name.Value);
                Assert.Equal("index", alias.Columns[1].Name.Value);
                break;
            default:
                Assert.Fail("Expecting TableFactor::Function with ordinality and alias columns");
                break;
        }
    }

    [Fact]
    public void test_table_unnest_with_ordinality()
    {
        var from = PgAndGeneric()
            .VerifiedOnlySelect("SELECT * FROM UNNEST([10, 20, 30]) WITH ORDINALITY AS t")
            .From;
        Assert.Equal(1, from.Count);
        switch (from[0].Relation)
        {
            case TableFactor.UNNEST
            {
                WithOrdinality: true,
            }:
                break;
            default:
                Assert.Fail("Expecting TableFactor::UNNEST with ordinality");
                break;
        }
    }

    [Fact]
    public void test_escaped_string_literal()
    {
        switch (Pg().VerifiedExpr(@"E'\n'"))
        {
            case Expr.Value
            {
                Inner: { Value: Value.EscapedStringLiteral { Value: var s } },
            }:
                Assert.Equal("\n", s);
                break;
            default:
                Assert.Fail("unreachable");
                break;
        }
    }

    [Fact]
    public void parse_create_domain()
    {
        var sql1 = "CREATE DOMAIN my_domain AS INTEGER CHECK (VALUE > 0)";
        var expected = new Statement.CreateDomain(new CreateDomain(
            Name: ObjectName.From(Sequence.Of(new Ident("my_domain"))),
            DataType: new DataType.Integer(null),
            Collation: null,
            Default: null,
            Constraints: Sequence.Of(TableConstraint.From(new CheckConstraint(
                Name: null,
                Expr: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("VALUE")),
                    Op: new BinaryOperator.Gt(),
                    Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("0")))),
                NoInherit: false,
                Enforced: null)))));

        Assert.Equal(Pg().VerifiedStmt(sql1), expected);

        var sql2 = "CREATE DOMAIN my_domain AS INTEGER COLLATE \"en_US\" CHECK (VALUE > 0)";
        expected = new Statement.CreateDomain(new CreateDomain(
            Name: ObjectName.From(Sequence.Of(new Ident("my_domain"))),
            DataType: new DataType.Integer(null),
            Collation: Ident.WithQuote('"', "en_US"),
            Default: null,
            Constraints: Sequence.Of(TableConstraint.From(new CheckConstraint(
                Name: null,
                Expr: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("VALUE")),
                    Op: new BinaryOperator.Gt(),
                    Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("0")))),
                NoInherit: false,
                Enforced: null)))));

        Assert.Equal(Pg().VerifiedStmt(sql2), expected);

        var sql3 = "CREATE DOMAIN my_domain AS INTEGER DEFAULT 1 CHECK (VALUE > 0)";
        expected = new Statement.CreateDomain(new CreateDomain(
            Name: ObjectName.From(Sequence.Of(new Ident("my_domain"))),
            DataType: new DataType.Integer(null),
            Collation: null,
            Default: new Expr.Value(ValueWithSpan.From(TestUtils.Number("1"))),
            Constraints: Sequence.Of(TableConstraint.From(new CheckConstraint(
                Name: null,
                Expr: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("VALUE")),
                    Op: new BinaryOperator.Gt(),
                    Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("0")))),
                NoInherit: false,
                Enforced: null)))));

        Assert.Equal(Pg().VerifiedStmt(sql3), expected);

        var sql4 = "CREATE DOMAIN my_domain AS INTEGER COLLATE \"en_US\" DEFAULT 1 CHECK (VALUE > 0)";
        expected = new Statement.CreateDomain(new CreateDomain(
            Name: ObjectName.From(Sequence.Of(new Ident("my_domain"))),
            DataType: new DataType.Integer(null),
            Collation: Ident.WithQuote('"', "en_US"),
            Default: new Expr.Value(ValueWithSpan.From(TestUtils.Number("1"))),
            Constraints: Sequence.Of(TableConstraint.From(new CheckConstraint(
                Name: null,
                Expr: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("VALUE")),
                    Op: new BinaryOperator.Gt(),
                    Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("0")))),
                NoInherit: false,
                Enforced: null)))));

        Assert.Equal(Pg().VerifiedStmt(sql4), expected);

        var sql5 = "CREATE DOMAIN my_domain AS INTEGER CONSTRAINT my_constraint CHECK (VALUE > 0)";
        expected = new Statement.CreateDomain(new CreateDomain(
            Name: ObjectName.From(Sequence.Of(new Ident("my_domain"))),
            DataType: new DataType.Integer(null),
            Collation: null,
            Default: null,
            Constraints: Sequence.Of(TableConstraint.From(new CheckConstraint(
                Name: new Ident("my_constraint"),
                Expr: new Expr.BinaryOp(
                    Left: new Expr.Identifier(new Ident("VALUE")),
                    Op: new BinaryOperator.Gt(),
                    Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("0")))),
                NoInherit: false,
                Enforced: null)))));

        Assert.Equal(Pg().VerifiedStmt(sql5), expected);
    }

    [Fact]
    public void parse_create_simple_before_insert_trigger()
    {
        var sql = "CREATE TRIGGER check_insert BEFORE INSERT ON accounts FOR EACH ROW EXECUTE FUNCTION check_account_insert";
        var expected = new Statement.CreateTrigger(new CreateTrigger(
            OrAlter: false,
            Temporary: false,
            OrReplace: false,
            IsConstraint: false,
            Name: ObjectName.From(Sequence.Of(new Ident("check_insert"))),
            Period: new TriggerPeriod.Before(),
            PeriodBeforeTable: true,
            Events: Sequence.Of<TriggerEvent>(new TriggerEvent.Insert()),
            TableName: ObjectName.From(Sequence.Of(new Ident("accounts"))),
            ReferencedTableName: null,
            Referencing: Sequence<TriggerReferencing>.Empty,
            TriggerObject: new TriggerObjectKind.ForEach(new TriggerObject.Row()),
            Condition: null,
            ExecBody: new TriggerExecBody(
                ExecType: new TriggerExecBodyType.Function(),
                FuncDesc: new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("check_account_insert"))),
                    Args: null)),
            StatementsAs: false,
            Statements: null,
            Characteristics: null));

        Assert.Equal(Pg().VerifiedStmt(sql), expected);
    }

    [Fact]
    public void parse_create_after_update_trigger_with_condition()
    {
        var sql = "CREATE TRIGGER check_update AFTER UPDATE ON accounts FOR EACH ROW WHEN (NEW.balance > 10000) EXECUTE FUNCTION check_account_update";
        var expected = new Statement.CreateTrigger(new CreateTrigger(
            OrAlter: false,
            Temporary: false,
            OrReplace: false,
            IsConstraint: false,
            Name: ObjectName.From(Sequence.Of(new Ident("check_update"))),
            Period: new TriggerPeriod.After(),
            PeriodBeforeTable: true,
            Events: Sequence.Of<TriggerEvent>(new TriggerEvent.Update(Sequence<Ident>.Empty)),
            TableName: ObjectName.From(Sequence.Of(new Ident("accounts"))),
            ReferencedTableName: null,
            Referencing: Sequence<TriggerReferencing>.Empty,
            TriggerObject: new TriggerObjectKind.ForEach(new TriggerObject.Row()),
            Condition: new Expr.Nested(new Expr.BinaryOp(
                Left: new Expr.CompoundIdentifier(Sequence.Of(
                    new Ident("NEW"),
                    new Ident("balance"))),
                Op: new BinaryOperator.Gt(),
                Right: Expr.MakeValue(TestUtils.Number("10000")))),
            ExecBody: new TriggerExecBody(
                ExecType: new TriggerExecBodyType.Function(),
                FuncDesc: new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("check_account_update"))),
                    Args: null)),
            StatementsAs: false,
            Statements: null,
            Characteristics: null));

        Assert.Equal(Pg().VerifiedStmt(sql), expected);
    }

    [Fact]
    public void parse_create_instead_of_delete_trigger()
    {
        var sql = "CREATE TRIGGER check_delete INSTEAD OF DELETE ON accounts FOR EACH ROW EXECUTE FUNCTION check_account_deletes";
        var expected = new Statement.CreateTrigger(new CreateTrigger(
            OrAlter: false,
            Temporary: false,
            OrReplace: false,
            IsConstraint: false,
            Name: ObjectName.From(Sequence.Of(new Ident("check_delete"))),
            Period: new TriggerPeriod.InsteadOf(),
            PeriodBeforeTable: true,
            Events: Sequence.Of<TriggerEvent>(new TriggerEvent.Delete()),
            TableName: ObjectName.From(Sequence.Of(new Ident("accounts"))),
            ReferencedTableName: null,
            Referencing: Sequence<TriggerReferencing>.Empty,
            TriggerObject: new TriggerObjectKind.ForEach(new TriggerObject.Row()),
            Condition: null,
            ExecBody: new TriggerExecBody(
                ExecType: new TriggerExecBodyType.Function(),
                FuncDesc: new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("check_account_deletes"))),
                    Args: null)),
            StatementsAs: false,
            Statements: null,
            Characteristics: null));

        Assert.Equal(Pg().VerifiedStmt(sql), expected);
    }

    [Fact]
    public void parse_create_trigger_with_multiple_events_and_deferrable()
    {
        var sql = "CREATE CONSTRAINT TRIGGER check_multiple_events BEFORE INSERT OR UPDATE OR DELETE ON accounts DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION check_account_changes";
        var expected = new Statement.CreateTrigger(new CreateTrigger(
            OrAlter: false,
            Temporary: false,
            OrReplace: false,
            IsConstraint: true,
            Name: ObjectName.From(Sequence.Of(new Ident("check_multiple_events"))),
            Period: new TriggerPeriod.Before(),
            PeriodBeforeTable: true,
            Events: Sequence.Of<TriggerEvent>(
                new TriggerEvent.Insert(),
                new TriggerEvent.Update(Sequence<Ident>.Empty),
                new TriggerEvent.Delete()),
            TableName: ObjectName.From(Sequence.Of(new Ident("accounts"))),
            ReferencedTableName: null,
            Referencing: Sequence<TriggerReferencing>.Empty,
            TriggerObject: new TriggerObjectKind.ForEach(new TriggerObject.Row()),
            Condition: null,
            ExecBody: new TriggerExecBody(
                ExecType: new TriggerExecBodyType.Function(),
                FuncDesc: new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("check_account_changes"))),
                    Args: null)),
            StatementsAs: false,
            Statements: null,
            Characteristics: new ConstraintCharacteristics(
                Deferrable: true,
                Initially: DeferrableInitial.Deferred,
                Enforced: null)));

        Assert.Equal(Pg().VerifiedStmt(sql), expected);
    }

    [Fact]
    public void parse_create_trigger_with_referencing()
    {
        var sql = "CREATE TRIGGER check_referencing BEFORE INSERT ON accounts REFERENCING NEW TABLE AS new_accounts OLD TABLE AS old_accounts FOR EACH ROW EXECUTE FUNCTION check_account_referencing";
        var expected = new Statement.CreateTrigger(new CreateTrigger(
            OrAlter: false,
            Temporary: false,
            OrReplace: false,
            IsConstraint: false,
            Name: ObjectName.From(Sequence.Of(new Ident("check_referencing"))),
            Period: new TriggerPeriod.Before(),
            PeriodBeforeTable: true,
            Events: Sequence.Of<TriggerEvent>(new TriggerEvent.Insert()),
            TableName: ObjectName.From(Sequence.Of(new Ident("accounts"))),
            ReferencedTableName: null,
            Referencing: Sequence.Of(
                new TriggerReferencing(
                    ReferType: new TriggerReferencingType.NewTable(),
                    IsAs: true,
                    TransitionRelationName: ObjectName.From(Sequence.Of(new Ident("new_accounts")))),
                new TriggerReferencing(
                    ReferType: new TriggerReferencingType.OldTable(),
                    IsAs: true,
                    TransitionRelationName: ObjectName.From(Sequence.Of(new Ident("old_accounts"))))),
            TriggerObject: new TriggerObjectKind.ForEach(new TriggerObject.Row()),
            Condition: null,
            ExecBody: new TriggerExecBody(
                ExecType: new TriggerExecBodyType.Function(),
                FuncDesc: new FunctionDesc(
                    Name: ObjectName.From(Sequence.Of(new Ident("check_account_referencing"))),
                    Args: null)),
            StatementsAs: false,
            Statements: null,
            Characteristics: null));

        Assert.Equal(Pg().VerifiedStmt(sql), expected);
    }
}
