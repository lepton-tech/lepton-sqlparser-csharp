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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_oracle.rs.
// Modificado: tradução de Rust para C#.

using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

/// <summary>
/// Test SQL syntax, specific to <see cref="OracleDialect"/>.
/// </summary>
/// <remarks>
/// Notas da tradução (onda-c/C16.md): <c>assert_eq!(a, b)</c> mantém a ordem do Rust, exceto quando
/// um dos lados é literal (regra xUnit2000); <c>assert!(matches!(x, P if g))</c> →
/// <c>Assert.True(x is P &amp;&amp; g)</c>; a mensagem extra de <c>assert_eq!(.., "msg")</c> fica em
/// comentário (o <c>Assert.Equal</c> do xUnit não recebe mensagem). Strings Rust com <c>\</c> no fim da
/// linha (que descarta a quebra e a indentação seguinte) viram concatenações dos mesmos pedaços.
/// </remarks>
#pragma warning disable xUnit2013
public class SqlParserOracleTests
{
    private static TestedDialects Oracle()
    {
        return new TestedDialects(new List<Dialect> { new OracleDialect() });
    }

    /// <summary>Convenience constructor for <see cref="Ast.QuoteDelimitedString"/>.</summary>
    private static Ast.QuoteDelimitedString QuoteDelimitedString(
        char startQuote,
        string value,
        char endQuote)
    {
        return new Ast.QuoteDelimitedString(
            StartQuote: startQuote,
            Value: value,
            EndQuote: endQuote);
    }

    /// <summary>Oracle: <c>||</c> has a lower precedence than <c>*</c> and <c>/</c></summary>
    [Fact]
    public void muldiv_have_higher_precedence_than_strconcat()
    {
        // ...............  A .. B ...... C .. D ...........
        var sql = "SELECT 3 / 5 || 'asdf' || 7 * 9 FROM dual";
        var select = Oracle().VerifiedOnlySelect(sql);
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            TestUtils.ExprFromProjection(select.Projection[0]),
            // (C || D)
            new Expr.BinaryOp(
                // (A || B)
                Left: new Expr.BinaryOp(
                    // A
                    Left: new Expr.BinaryOp(
                        Left: new Expr.Value(ValueWithSpan.From(TestUtils.Number("3"))),
                        Op: new BinaryOperator.Divide(),
                        Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("5")))),
                    Op: new BinaryOperator.StringConcat(),
                    Right: new Expr.Value(new ValueWithSpan(
                        Value: new Value.SingleQuotedString("asdf"),
                        Span: Span.Empty()))),
                Op: new BinaryOperator.StringConcat(),
                // D
                Right: new Expr.BinaryOp(
                    Left: new Expr.Value(ValueWithSpan.From(TestUtils.Number("7"))),
                    Op: new BinaryOperator.Multiply(),
                    Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("9"))))));
    }

    /// <summary>Oracle: <c>+</c>, <c>-</c>, and <c>||</c> have the same precedence and parse from left-to-right</summary>
    [Fact]
    public void plusminus_have_same_precedence_as_strconcat()
    {
        // ................ A .. B .... C .. D ............
        var sql = "SELECT 3 + 5 || '.3' || 7 - 9 FROM dual";
        var select = Oracle().VerifiedOnlySelect(sql);
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            TestUtils.ExprFromProjection(select.Projection[0]),
            // D
            new Expr.BinaryOp(
                Left: new Expr.BinaryOp(
                    // B
                    Left: new Expr.BinaryOp(
                        // A
                        Left: new Expr.BinaryOp(
                            Left: new Expr.Value(ValueWithSpan.From(TestUtils.Number("3"))),
                            Op: new BinaryOperator.Plus(),
                            Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("5")))),
                        Op: new BinaryOperator.StringConcat(),
                        Right: new Expr.Value(new ValueWithSpan(
                            Value: new Value.SingleQuotedString(".3"),
                            Span: Span.Empty()))),
                    Op: new BinaryOperator.StringConcat(),
                    Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("7")))),
                Op: new BinaryOperator.Minus(),
                Right: new Expr.Value(ValueWithSpan.From(TestUtils.Number("9")))));
    }

    [Fact]
    public void parse_quote_delimited_string()
    {
        var dialect = TestUtils.AllDialectsWhere(d => d.SupportsQuoteDelimitedString);
        var sql = "SELECT Q'.abc.', "
            + "Q'Xab'cX', "
            + "Q'|abc'''|', "
            + "Q'{abc}d}', "
            + "Q'[]abc[]', "
            + "Q'<a'bc>', "
            + "Q'<<<a'bc>', "
            + "Q'('abc'('abc)', "
            + "Q'(abc'def))', "
            + "Q'(abc'def)))' "
            + "FROM dual";
        var select = dialect.VerifiedOnlySelect(sql);
        Assert.Equal(10, select.Projection.Count);
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('.', "abc", '.'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('X', "ab'c", 'X'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('|', "abc'''", '|'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[2]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('{', "abc}d", '}'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[3]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('[', "]abc[", ']'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[4]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('<', "a'bc", '>'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[5]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('<', "<<a'bc", '>'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[6]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('(', "'abc'('abc", ')'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[7]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('(', "abc'def)", ')'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[8]));
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('(', "abc'def))", ')'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[9]));
    }

    [Fact]
    public void parse_invalid_quote_delimited_strings()
    {
        var dialect = TestUtils.AllDialectsWhere(d => d.SupportsQuoteDelimitedString);
        // invalid quote delimiter
        foreach (var q in new[] { ' ', '\t', '\r', '\n' })
        {
            // "with quote char {q:?}"
            var ex = Assert.Throws<ParserException>(() => dialect.ParseSqlStatements($"SELECT Q'{q}abc{q}' FROM dual"));
            Assert.Equal(
                new ParserError.TokenizerError(
                    "Invalid space, tab, newline, or EOF after 'Q'' at Line: 1, Column: 10"),
                ex.Error);
        }
        // invalid eof after quote
        // "with EOF quote char"
        var exEof = Assert.Throws<ParserException>(() => dialect.ParseSqlStatements("SELECT Q'"));
        Assert.Equal(
            new ParserError.TokenizerError(
                "Invalid space, tab, newline, or EOF after 'Q'' at Line: 1, Column: 10"),
            exEof.Error);
        // unterminated string
        // "with EOF quote char"
        var exUnterminated = Assert.Throws<ParserException>(() => dialect.ParseSqlStatements("SELECT Q'|asdfa...."));
        Assert.Equal(
            new ParserError.TokenizerError(
                "Unterminated string literal at Line: 1, Column: 9"),
            exUnterminated.Error);
    }

    [Fact]
    public void parse_quote_delimited_string_lowercase()
    {
        var dialect = TestUtils.AllDialectsWhere(d => d.SupportsQuoteDelimitedString);
        var sql = "select q'!a'b'c!d!' from dual";
        var select = dialect.VerifiedOnlySelectWithCanonical(sql, "SELECT Q'!a'b'c!d!' FROM dual");
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            new Expr.Value(
                new Value.QuoteDelimitedStringLiteral(QuoteDelimitedString('!', "a'b'c!d", '!'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[0]));
    }

    [Fact]
    public void parse_quote_delimited_string_but_is_a_word()
    {
        var dialect = TestUtils.AllDialectsWhere(d => d.SupportsQuoteDelimitedString);
        var sql = "SELECT q, quux, q.abc FROM dual q";
        var select = dialect.VerifiedOnlySelect(sql);
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            new Expr.Identifier(Ident.WithSpan(Span.Empty(), "q")),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Identifier(Ident.WithSpan(Span.Empty(), "quux")),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.CompoundIdentifier(Sequence.Of(
                Ident.WithSpan(Span.Empty(), "q"),
                Ident.WithSpan(Span.Empty(), "abc"))),
            TestUtils.ExprFromProjection(select.Projection[2]));
    }

    [Fact]
    public void parse_national_quote_delimited_string()
    {
        var dialect = TestUtils.AllDialectsWhere(d => d.SupportsQuoteDelimitedString);
        var sql = "SELECT NQ'.abc.' FROM dual";
        var select = dialect.VerifiedOnlySelect(sql);
        Assert.Equal(1, select.Projection.Count);
        Assert.Equal(
            new Expr.Value(
                new Value.NationalQuoteDelimitedStringLiteral(QuoteDelimitedString('.', "abc", '.'))
                    .WithEmptySpan()),
            TestUtils.ExprFromProjection(select.Projection[0]));
    }

    [Fact]
    public void parse_national_quote_delimited_string_lowercase()
    {
        var dialect = TestUtils.AllDialectsWhere(d => d.SupportsQuoteDelimitedString);
        foreach (var prefix in new[] { "nq", "Nq", "nQ", "NQ" })
        {
            var select = dialect.VerifiedOnlySelectWithCanonical(
                $"select {prefix}'!a'b'c!d!' from dual",
                "SELECT NQ'!a'b'c!d!' FROM dual");
            Assert.Equal(1, select.Projection.Count);
            Assert.Equal(
                new Expr.Value(
                    new Value.NationalQuoteDelimitedStringLiteral(QuoteDelimitedString(
                        '!', "a'b'c!d", '!'))
                    .WithEmptySpan()),
                TestUtils.ExprFromProjection(select.Projection[0]));
        }
    }

    [Fact]
    public void parse_national_quote_delimited_string_but_is_a_word()
    {
        var dialect = TestUtils.AllDialectsWhere(d => d.SupportsQuoteDelimitedString);
        var sql = "SELECT nq, nqoo, nq.abc FROM dual q";
        var select = dialect.VerifiedOnlySelect(sql);
        Assert.Equal(3, select.Projection.Count);
        Assert.Equal(
            new Expr.Identifier(Ident.WithSpan(Span.Empty(), "nq")),
            TestUtils.ExprFromProjection(select.Projection[0]));
        Assert.Equal(
            new Expr.Identifier(Ident.WithSpan(Span.Empty(), "nqoo")),
            TestUtils.ExprFromProjection(select.Projection[1]));
        Assert.Equal(
            new Expr.CompoundIdentifier(Sequence.Of(
                Ident.WithSpan(Span.Empty(), "nq"),
                Ident.WithSpan(Span.Empty(), "abc"))),
            TestUtils.ExprFromProjection(select.Projection[2]));
    }

    [Fact]
    public void test_optimizer_hints()
    {
        var oracleDialect = Oracle();

        // selects: all `/*+...*/` comments are collected as hints
        var select = oracleDialect.VerifiedOnlySelectWithCanonical(
            "SELECT /*+one two three*/ /*+four five six*/ 1 FROM dual",
            "SELECT /*+one two three*/ /*+four five six*/ 1 FROM dual");
        Assert.Equal(2, select.OptimizerHints.Count);
        Assert.Equal("one two three", select.OptimizerHints[0].Text);
        Assert.Equal("", select.OptimizerHints[0].Prefix);
        Assert.Equal("four five six", select.OptimizerHints[1].Text);

        // regular comments are skipped, hints after them are still collected
        select = oracleDialect.VerifiedOnlySelectWithCanonical(
            "SELECT /*one two three*/ /*+four five six*/ 1 FROM dual",
            "SELECT /*+four five six*/ 1 FROM dual");
        Assert.Equal(1, select.OptimizerHints.Count);
        Assert.Equal("four five six", select.OptimizerHints[0].Text);

        select = oracleDialect.VerifiedOnlySelectWithCanonical(
            "SELECT --+ one two three /* asdf */\n 1 FROM dual",
            "SELECT --+ one two three /* asdf */\n 1 FROM dual");
        Assert.Equal(1, select.OptimizerHints.Count);
        Assert.Equal(" one two three /* asdf */", select.OptimizerHints[0].Text);
        Assert.Equal("", select.OptimizerHints[0].Prefix);

        // inserts
        oracleDialect.VerifiedStmt("INSERT /*+ append */ INTO t1 SELECT * FROM all_objects");

        // updates
        oracleDialect.VerifiedStmt("UPDATE /*+ DISABLE_PARALLEL_DML */ table_name SET column1 = 1");

        // deletes
        oracleDialect.VerifiedStmt("DELETE --+ ENABLE_PARALLEL_DML\n FROM table_name");

        // merges
        oracleDialect.VerifiedStmt(
            "MERGE /*+ CLUSTERING */ INTO people_target pt "
            + "USING people_source ps "
            + "ON (pt.person_id = ps.person_id) "
            + "WHEN NOT MATCHED THEN INSERT "
            + "(pt.person_id, pt.first_name, pt.last_name, pt.title) "
            + "VALUES (ps.person_id, ps.first_name, ps.last_name, ps.title)");

        // single-line prefixed hint (Oracle supports `--` without trailing whitespace)
        select = oracleDialect.VerifiedOnlySelectWithCanonical(
            "SELECT --abc+ text\n 1 FROM dual",
            "SELECT --abc+ text\n 1 FROM dual");
        Assert.Equal(1, select.OptimizerHints.Count);
        Assert.Equal("abc", select.OptimizerHints[0].Prefix);
        Assert.Equal(" text", select.OptimizerHints[0].Text);
    }

    [Fact]
    public void test_connect_by()
    {
        var oracleDialect = Oracle();

        oracleDialect.VerifiedOnlySelect(
            "SELECT last_name AS \"Employee\", CONNECT_BY_ISCYCLE AS \"Cycle\", "
            + "LEVEL, "
            + "SYS_CONNECT_BY_PATH(last_name, '/') AS \"Path\" "
            + "FROM employees "
            + "WHERE level <= 3 AND department_id = 80 "
            + "START WITH last_name = 'King' "
            + "CONNECT BY NOCYCLE PRIOR employee_id = manager_id AND LEVEL <= 4 "
            + "ORDER BY \"Employee\", \"Cycle\", LEVEL, \"Path\"");

        // CONNECT_BY_ROOT
        oracleDialect.VerifiedOnlySelect(
            "SELECT last_name AS \"Employee\", CONNECT_BY_ROOT last_name AS \"Manager\", "
            + "LEVEL - 1 AS \"Pathlen\", SYS_CONNECT_BY_PATH(last_name, '/') AS \"Path\" "
            + "FROM employees "
            + "WHERE LEVEL > 1 AND department_id = 110 "
            + "CONNECT BY PRIOR employee_id = manager_id "
            + "ORDER BY \"Employee\", \"Manager\", \"Pathlen\", \"Path\"");
    }

    [Fact]
    public void test_insert_with_table_alias()
    {
        var oracleDialect = Oracle();

        static void VerifyTableNameWithAlias(Statement stmt, string expTableName, string expTableAlias)
        {
            Assert.True(stmt is Statement.Insert
            {
                Value:
                {
                    Table: TableObject.TableName { Value: var tableName },
                    TableAlias:
                    {
                        Explicit: false,
                        Alias: { Value: var tableAlias, QuoteStyle: null },
                    },
                },
            }
                && tableAlias == expTableAlias
                && tableName == ObjectName.From(Sequence.Of(new Ident(
                    Value: expTableName,
                    QuoteStyle: null,
                    Span: Span.Empty()))));
        }

        var stmt = oracleDialect.VerifiedStmt(
            "INSERT INTO foo_t t "
            + "SELECT 1, 2, 3 FROM dual");
        VerifyTableNameWithAlias(stmt, "foo_t", "t");

        stmt = oracleDialect.VerifiedStmt(
            "INSERT INTO foo_t asdf (a, b, c) "
            + "SELECT 1, 2, 3 FROM dual");
        VerifyTableNameWithAlias(stmt, "foo_t", "asdf");

        stmt = oracleDialect.VerifiedStmt(
            "INSERT INTO foo_t t (a, b, c) "
            + "VALUES (1, 2, 3)");
        VerifyTableNameWithAlias(stmt, "foo_t", "t");

        stmt = oracleDialect.VerifiedStmt(
            "INSERT INTO foo_t t "
            + "VALUES (1, 2, 3)");
        VerifyTableNameWithAlias(stmt, "foo_t", "t");

        stmt =
            oracleDialect.VerifiedStmt("INSERT INTO foo_t t (t.id, t.val) SELECT 1, 2 FROM dual");
        VerifyTableNameWithAlias(stmt, "foo_t", "t");
        if (stmt is Statement.Insert { Value: var insert })
        {
            Assert.Equal(
                Sequence.Of(
                    ObjectName.From(Sequence.Of(new Ident("t"), new Ident("id"))),
                    ObjectName.From(Sequence.Of(new Ident("t"), new Ident("val")))),
                insert.Columns);
        }
        else
        {
            throw new InvalidOperationException("not an insert statement");
        }
    }

    [Fact]
    public void test_insert_without_alias()
    {
        var oracleDialect = Oracle();

        // check DEFAULT
        var sql = "INSERT INTO t default SELECT 'a' FROM dual";
        var ex = Assert.Throws<ParserException>(() => oracleDialect.ParseSqlStatements(sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Expected: SELECT, VALUES, or a subquery in the query body, found: default"));

        // check SELECT
        sql = "INSERT INTO t SELECT 'a' FROM dual";
        var stmt = oracleDialect.VerifiedStmt(sql);
        Assert.True(stmt is Statement.Insert
        {
            Value:
            {
                TableAlias: null,
                Source: { Body: SetExpr.Select },
            },
        });

        // check WITH
        sql = "INSERT INTO dual WITH w AS (SELECT 1 AS y FROM dual) SELECT y FROM w";
        stmt = oracleDialect.VerifiedStmt(sql);
        Assert.True(stmt is Statement.Insert
        {
            Value:
            {
                TableAlias: null,
                Source: { Body: SetExpr.Select },
            },
        });

        // check VALUES
        sql = "INSERT INTO t VALUES (1)";
        stmt = oracleDialect.VerifiedStmt(sql);
        Assert.True(stmt is Statement.Insert
        {
            Value:
            {
                TableAlias: null,
                Source: { Body: SetExpr.Values },
            },
        });
    }
}
