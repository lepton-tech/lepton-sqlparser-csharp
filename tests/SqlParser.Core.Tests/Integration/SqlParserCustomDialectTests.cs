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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_custom_dialect.rs.
// Modificado: tradução de Rust para C#. Cada `struct MyDialect` declarado dentro de uma função de
// teste vira `MyDialect` aninhado numa classe estática privada com o nome da função em PascalCase
// (`CustomPrefixParser.MyDialect`), o equivalente ao escopo da função no Rust.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

/// <summary>Test the ability for dialects to override parsing</summary>
public class SqlParserCustomDialectTests
{
    private static class CustomPrefixParser
    {
        // #[derive(Debug)] struct MyDialect {}
        internal sealed class MyDialect : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);

            public override Expr? ParsePrefix(Parser parser)
            {
                if (parser.ConsumeToken(new Token.Number("1", false)))
                {
                    return new Expr.Value(new Value.Null().WithEmptySpan());
                }
                else
                {
                    return null;
                }
            }
        }
    }

    [Fact]
    public void custom_prefix_parser()
    {
        var dialect = new CustomPrefixParser.MyDialect();
        var sql = "SELECT 1 + 2";
        var ast = Parser.ParseSql(dialect, sql);
        var query = ast[0];
        Assert.Equal("SELECT NULL + 2", $"{query}");
    }

    private static class CustomInfixParser
    {
        // #[derive(Debug)] struct MyDialect {}
        internal sealed class MyDialect : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);

            public override Expr? ParseInfix(
                Parser parser,
                Expr expr,
                byte precedence)
            {
                _ = precedence; // _precedence
                if (parser.ConsumeToken(new Token.Plus()))
                {
                    return new Expr.BinaryOp(
                        Left: expr,
                        Op: new BinaryOperator.Multiply(), // translate Plus to Multiply
                        Right: parser.ParseExpr());
                }
                else
                {
                    return null;
                }
            }
        }
    }

    [Fact]
    public void custom_infix_parser()
    {
        var dialect = new CustomInfixParser.MyDialect();
        var sql = "SELECT 1 + 2";
        var ast = Parser.ParseSql(dialect, sql);
        var query = ast[0];
        Assert.Equal("SELECT 1 * 2", $"{query}");
    }

    private static class CustomStatementParser
    {
        // #[derive(Debug)] struct MyDialect {}
        internal sealed class MyDialect : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);

            public override Statement? ParseStatement(Parser parser)
            {
                if (parser.ParseKeyword(Keyword.SELECT))
                {
                    for (var i = 0; i < 3; i++)
                    {
                        _ = parser.NextToken();
                    }
                    return new Statement.Commit(
                        Chain: false,
                        End: false,
                        Modifier: null);
                }
                else
                {
                    return null;
                }
            }
        }
    }

    [Fact]
    public void custom_statement_parser()
    {
        var dialect = new CustomStatementParser.MyDialect();
        var sql = "SELECT 1 + 2";
        var ast = Parser.ParseSql(dialect, sql);
        var query = ast[0];
        Assert.Equal("COMMIT", $"{query}");
    }

    private static class TestMapSyntaxNotSupportDefault
    {
        // #[derive(Debug)] struct MyDialect {}
        internal sealed class MyDialect : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);
        }
    }

    [Fact]
    public void test_map_syntax_not_support_default()
    {
        var dialect = new TestMapSyntaxNotSupportDefault.MyDialect();
        var sql = "SELECT MAP {1: 2}";
        Assert.Throws<ParserException>(() => Parser.ParseSql(dialect, sql)); // assert!(ast.is_err())
    }

    private static class CustomDialectSupportsTablePartitions
    {
        // #[derive(Debug)] struct MyDialect {}
        internal sealed class MyDialect : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);

            public override bool SupportsTablePartitions => true;
        }

        // #[derive(Debug)] struct WithoutPartitions {}
        internal sealed class WithoutPartitions : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);
        }
    }

    [Fact]
    public void custom_dialect_supports_table_partitions()
    {
        var sql = "SELECT * FROM employees PARTITION (p0, p1)";
        var ast = Parser.ParseSql(new CustomDialectSupportsTablePartitions.MyDialect(), sql);
        Assert.Equal(sql, $"{ast[0]}");

        // A dialect that does not opt in still rejects the clause.
        // assert!(Parser::parse_sql(&WithoutPartitions {}, sql).is_err())
        Assert.Throws<ParserException>(() => Parser.ParseSql(new CustomDialectSupportsTablePartitions.WithoutPartitions(), sql));
    }

    private static bool IsIdentifierStart(char ch) =>
        RustChar.IsAsciiLowercase(ch) || RustChar.IsAsciiUppercase(ch) || ch == '_';

    private static bool IsIdentifierPart(char ch) =>
        RustChar.IsAsciiLowercase(ch)
        || RustChar.IsAsciiUppercase(ch)
        || RustChar.IsAsciiDigit(ch)
        || ch == '$'
        || ch == '_';

    private static class CustomDialectLambdaKeywordSyntaxWithoutArrow
    {
        // #[derive(Debug)] struct MyDialect {}
        internal sealed class MyDialect : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);

            public override bool SupportsLambdaKeywordSyntax => true;
        }
    }

    [Fact]
    public void custom_dialect_lambda_keyword_syntax_without_arrow()
    {
        // A dialect that gives `->` its own meaning can still support lambdas
        // through the `LAMBDA` keyword spelling.
        var dialect = new CustomDialectLambdaKeywordSyntaxWithoutArrow.MyDialect();

        // The `LAMBDA` spelling parses.
        var sql = "SELECT transform(xs, lambda x : x + 1)";
        Assert.Equal(
            sql,
            $"{Parser.ParseSql(dialect, sql)[0]}");

        // `->` keeps whatever meaning the dialect gives it, rather than
        // introducing a lambda parameter.
        sql = "SELECT a -> 'b'";
        var ast = Parser.ParseSql(dialect, sql);
        switch (ast[0])
        {
            case Statement.Query { Value: var query }:
                if (TestUtils.ExprFromProjection(TestUtils.Only(query.Body.AsSelect()!.Projection))
                    is not Expr.BinaryOp { Op: var op })
                {
                    throw new InvalidOperationException("expected `->` to stay a binary operator");
                }
                Assert.Equal<BinaryOperator>(new BinaryOperator.Arrow(), op);
                break;
            case var stmt:
                throw new InvalidOperationException($"unexpected statement {stmt}");
        }
    }

    private static class CustomDialectLambdaKeywordDefaultsToArrowSupport
    {
        // #[derive(Debug)] struct MyDialect {}
        internal sealed class MyDialect : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);

            public override bool SupportsLambdaFunctions => true;
        }
    }

    [Fact]
    public void custom_dialect_lambda_keyword_defaults_to_arrow_support()
    {
        // Dialects that opt into the `->` spelling get the `LAMBDA` spelling too,
        // so the new capability does not change any existing dialect.
        var dialect = new CustomDialectLambdaKeywordDefaultsToArrowSupport.MyDialect();
        Assert.True(dialect.SupportsLambdaKeywordSyntax);
        foreach (var sql in new[]
        {
            "SELECT transform(xs, lambda x : x + 1)",
            "SELECT transform(xs, x -> x + 1)",
        })
        {
            Assert.Equal(
                sql,
                $"{Parser.ParseSql(dialect, sql)[0]}");
        }
    }

    private static class CustomDialectLambdaArrowSyntaxWithoutKeyword
    {
        // #[derive(Debug)] struct MyDialect {}
        internal sealed class MyDialect : Dialect
        {
            public override bool IsIdentifierStart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierStart(ch);

            public override bool IsIdentifierPart(char ch) =>
                SqlParserCustomDialectTests.IsIdentifierPart(ch);

            public override bool SupportsLambdaFunctions => true;

            public override bool SupportsLambdaKeywordSyntax => false;
        }
    }

    [Fact]
    public void custom_dialect_lambda_arrow_syntax_without_keyword()
    {
        // Arrow lambdas stay on while the `LAMBDA` keyword spelling is off,
        // as in engines like Spark and Snowflake.
        var dialect = new CustomDialectLambdaArrowSyntaxWithoutKeyword.MyDialect();

        var sql = "SELECT transform(xs, x -> x + 1)";
        Assert.Equal(
            sql,
            $"{Parser.ParseSql(dialect, sql)[0]}");
        // assert!(Parser::parse_sql(..).is_err())
        Assert.Throws<ParserException>(() => Parser.ParseSql(dialect, "SELECT transform(xs, lambda x : x + 1)"));
    }
}
