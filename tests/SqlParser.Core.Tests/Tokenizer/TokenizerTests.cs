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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/tokenizer.rs (#[cfg(test)] mod tests).
// Modificado: tradução de Rust para C#. Nomes dos testes e casos idênticos ao Rust, na mesma ordem.

using System.Globalization;
using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;
using static SqlParser.Core.Tests.TestUtils;

namespace SqlParser.Core.Tests;

public class TokenizerTests
{
    [Fact]
    public void tokenizer_error_impl()
    {
        var err = new TokenizerError("test", new Location(1, 1));
        {
            // use core::error::Error; assert!(err.source().is_none());
            var ex = new TokenizerException(err);
            Assert.Null(ex.InnerException);
        }
        Assert.Equal("test at Line: 1, Column: 1", err.ToString());
    }

    [Fact]
    public void tokenize_select_1()
    {
        var sql = "SELECT 1";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1", false),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_select_float()
    {
        var sql = "SELECT .1";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number(".1", false),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_with_mapper()
    {
        var sql = "SELECT ?";
        var dialect = new GenericDialect();
        var paramNum = 1;

        var tokens = new List<TokenWithSpan>();
        new Tokenizer(dialect, sql)
            .TokenizeWithLocationIntoBufWithMapper(tokens, tokenSpan =>
            {
                Token token;
                if (tokenSpan.Token is Token.Placeholder { Value: var n })
                {
                    string value;
                    if (n == "?")
                    {
                        var ret = "$" + paramNum.ToString(CultureInfo.InvariantCulture);
                        paramNum += 1;
                        value = ret;
                    }
                    else
                    {
                        value = n;
                    }
                    token = new Token.Placeholder(value);
                }
                else
                {
                    token = tokenSpan.Token;
                }
                return tokenSpan with { Token = token };
            });
        var actual = tokens.Select(t => t.Token).ToList();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Placeholder("$1"),
        };

        Compare(expected, actual);
    }

    [Fact]
    public void tokenize_clickhouse_double_equal()
    {
        var sql = "SELECT foo=='1'";
        var dialect = new ClickHouseDialect();
        var tokenizer = new Tokenizer(dialect, sql);
        var tokens = tokenizer.Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Word(new Word("foo", null, Keyword.NoKeyword)),
            new Token.DoubleEq(),
            new Token.SingleQuotedString("1"),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_numeric_literal_underscore()
    {
        var dialect = new GenericDialect();
        var sql = "SELECT 10_000";
        var tokenizer = new Tokenizer(dialect, sql);
        var tokens = tokenizer.Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("10", false),
            Token.MakeWord("_000", null),
        };
        Compare(expected, tokens);

        var numericUnderscoreDialects =
            AllDialectsWhere(d => d.SupportsNumericLiteralUnderscores);

        numericUnderscoreDialects.TokenizesTo(
            "SELECT 10_000, _10_000, 1_000.123, 1_000.123_456, 1e1_0",
            new List<Token>
            {
                Token.MakeKeyword("SELECT"),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("10_000", false),
                new Token.Comma(),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("_10_000", null), // leading underscore tokenizes as a word (parsed as column identifier)
                new Token.Comma(),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1_000.123", false), // with decimal digits
                new Token.Comma(),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1_000.123_456", false), // with an underscore in the decimal digits
                new Token.Comma(),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1e1_0", false), // with an underscore in the exponent
            });

        numericUnderscoreDialects.TokenizesTo(
            "0xFF_FF",
            new List<Token> { new Token.HexStringLiteral("FF_FF") });

        foreach (var d in numericUnderscoreDialects.Dialects)
        {
            foreach (var errSql in new[]
            {
                "SELECT 10_00_",
                "SELECT 10___0",
                "SELECT 1_000.123_",
                "SELECT 1._000",
                "SELECT 1_a",
                "SELECT 1e_1",
                "SELECT 1e1_",
                "SELECT 1e1__0",
                "SELECT 0x_1",
                "SELECT 0x1_",
            })
            {
                var err = Assert.Throws<TokenizerException>(() => new Tokenizer(d, errSql).Tokenize()).Error;
                Assert.Equal("Unexpected character '_'", err.Message);
            }
        }
    }

    [Fact]
    public void tokenize_select_exponent()
    {
        var sql = "SELECT 1e10, 1e-10, 1e+10, 1ea, 1e-10a, 1e-10-10";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1e10", false),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1e-10", false),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1e+10", false),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1", false),
            Token.MakeWord("ea", null),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1e-10", false),
            Token.MakeWord("a", null),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1e-10", false),
            new Token.Minus(),
            new Token.Number("10", false),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_scalar_function()
    {
        var sql = "SELECT sqrt(1)";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("sqrt", null),
            new Token.LParen(),
            new Token.Number("1", false),
            new Token.RParen(),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_string_string_concat()
    {
        var sql = "SELECT 'a' || 'b'";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("a"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.StringConcat(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("b"),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_bitwise_op()
    {
        var sql = "SELECT one | two ^ three";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("one", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Pipe(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("two", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Caret(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("three", null),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_logical_xor()
    {
        var sql =
            "SELECT true XOR true, false XOR false, true XOR false, false XOR true";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("true"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("XOR"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("true"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("false"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("XOR"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("false"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("true"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("XOR"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("false"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("false"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("XOR"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("true"),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_simple_select()
    {
        var sql = "SELECT * FROM customer WHERE id = 1 LIMIT 5";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Mul(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("FROM"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("customer", null),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("WHERE"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("id", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Eq(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1", false),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("LIMIT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("5", false),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_explain_select()
    {
        var sql = "EXPLAIN SELECT * FROM customer WHERE id = 1";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("EXPLAIN"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Mul(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("FROM"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("customer", null),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("WHERE"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("id", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Eq(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1", false),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_explain_analyze_select()
    {
        var sql = "EXPLAIN ANALYZE SELECT * FROM customer WHERE id = 1";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("EXPLAIN"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("ANALYZE"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Mul(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("FROM"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("customer", null),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("WHERE"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("id", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Eq(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1", false),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_string_predicate()
    {
        var sql = "SELECT * FROM customer WHERE salary != 'Not Provided'";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Mul(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("FROM"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("customer", null),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("WHERE"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("salary", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Neq(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("Not Provided"),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_invalid_string()
    {
        // "\n💝مصطفىh" (💝 = U+1F49D, fora do BMP)
        var sql = "\n\U0001F49D\u0645\u0635\u0637\u0641\u0649h";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        // println!("tokens: {:#?}", tokens);
        var expected = new List<Token>
        {
            new Token.Whitespace(new Whitespace.Newline()),
            new Token.Char("\U0001F49D"),
            Token.MakeWord("\u0645\u0635\u0637\u0641\u0649h", null),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_newline_in_string_literal()
    {
        var sql = "'foo\r\nbar\nbaz'";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token> { new Token.SingleQuotedString("foo\r\nbar\nbaz") };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_unterminated_string_literal()
    {
        var sql = "select 'foo";

        var dialect = new GenericDialect();
        var tokenizer = new Tokenizer(dialect, sql);
        var ex = Assert.Throws<TokenizerException>(() => tokenizer.Tokenize());
        Assert.Equal(
            new TokenizerError("Unterminated string literal", new Location(1, 8)),
            ex.Error);
    }

    [Fact]
    public void tokenize_unterminated_string_literal_utf8()
    {
        // "SELECT \"なにか\" FROM Y WHERE \"なにか\" = 'test;"
        var sql = "SELECT \"\u306a\u306b\u304b\" FROM Y WHERE \"\u306a\u306b\u304b\" = 'test;";

        var dialect = new GenericDialect();
        var tokenizer = new Tokenizer(dialect, sql);
        var ex = Assert.Throws<TokenizerException>(() => tokenizer.Tokenize());
        Assert.Equal(
            new TokenizerError("Unterminated string literal", new Location(1, 35)),
            ex.Error);
    }

    [Fact]
    public void tokenize_invalid_string_cols()
    {
        // "\n\nSELECT * FROM table\t💝مصطفىh" (💝 = U+1F49D, fora do BMP)
        var sql = "\n\nSELECT * FROM table\t\U0001F49D\u0645\u0635\u0637\u0641\u0649h";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        // println!("tokens: {:#?}", tokens);
        var expected = new List<Token>
        {
            new Token.Whitespace(new Whitespace.Newline()),
            new Token.Whitespace(new Whitespace.Newline()),
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Mul(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("FROM"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("table"),
            new Token.Whitespace(new Whitespace.Tab()),
            new Token.Char("\U0001F49D"),
            Token.MakeWord("\u0645\u0635\u0637\u0641\u0649h", null),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_dollar_quoted_string_tagged()
    {
        var testCases = new List<(string, List<Token>)>
        {
            (
                "SELECT $tag$dollar '$' quoted strings have $tags like this$ or like this $$$tag$",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.DollarQuotedString(new DollarQuotedString(
                        "dollar '$' quoted strings have $tags like this$ or like this $$",
                        "tag")),
                }
            ),
            (
                "SELECT $abc$x$ab$abc$",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.DollarQuotedString(new DollarQuotedString("x$ab", "abc")),
                }
            ),
            (
                "SELECT $abc$$abc$",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.DollarQuotedString(new DollarQuotedString("", "abc")),
                }
            ),
            (
                "0$abc$$abc$1",
                new List<Token>
                {
                    new Token.Number("0", false),
                    new Token.DollarQuotedString(new DollarQuotedString("", "abc")),
                    new Token.Number("1", false),
                }
            ),
            (
                "$function$abc$q$data$q$$function$",
                new List<Token>
                {
                    new Token.DollarQuotedString(new DollarQuotedString("abc$q$data$q$", "function")),
                }
            ),
        };

        var dialect = new GenericDialect();
        foreach (var (sql, expected) in testCases)
        {
            var tokens = new Tokenizer(dialect, sql).Tokenize();
            Compare(expected, tokens);
        }
    }

    [Fact]
    public void tokenize_dollar_quoted_string_tagged_unterminated()
    {
        var sql = "SELECT $tag$dollar '$' quoted strings have $tags like this$ or like this $$$different tag$";
        var dialect = new GenericDialect();
        var ex = Assert.Throws<TokenizerException>(() => new Tokenizer(dialect, sql).Tokenize());
        Assert.Equal(
            new TokenizerError("Unterminated dollar-quoted, expected $", new Location(1, 91)),
            ex.Error);
    }

    [Fact]
    public void tokenize_dollar_quoted_string_tagged_unterminated_mirror()
    {
        var sql = "SELECT $abc$abc$";
        var dialect = new GenericDialect();
        var ex = Assert.Throws<TokenizerException>(() => new Tokenizer(dialect, sql).Tokenize());
        Assert.Equal(
            new TokenizerError("Unterminated dollar-quoted, expected $", new Location(1, 17)),
            ex.Error);
    }

    [Fact]
    public void tokenize_dollar_placeholder()
    {
        var sql = "SELECT $$, $$ABC$$, $ABC$, $ABC";
        var dialect = new SQLiteDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        Assert.Equal(
            tokens,
            new List<Token>
            {
                Token.MakeKeyword("SELECT"),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Placeholder("$$"),
                new Token.Comma(),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Placeholder("$$ABC$$"),
                new Token.Comma(),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Placeholder("$ABC$"),
                new Token.Comma(),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Placeholder("$ABC"),
            });
    }

    [Fact]
    public void tokenize_nested_dollar_quoted_strings()
    {
        var sql = "SELECT $tag$dollar $nested$ string$tag$";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.DollarQuotedString(new DollarQuotedString("dollar $nested$ string", "tag")),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_dollar_quoted_string_untagged_empty()
    {
        var sql = "SELECT $$$$";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.DollarQuotedString(new DollarQuotedString("", null)),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_dollar_quoted_string_untagged()
    {
        var sql =
            "SELECT $$within dollar '$' quoted strings have $tags like this$ $$";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.DollarQuotedString(new DollarQuotedString(
                "within dollar '$' quoted strings have $tags like this$ ",
                null)),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_dollar_quoted_string_untagged_unterminated()
    {
        var sql =
            "SELECT $$dollar '$' quoted strings have $tags like this$ or like this $different tag$";
        var dialect = new GenericDialect();
        var ex = Assert.Throws<TokenizerException>(() => new Tokenizer(dialect, sql).Tokenize());
        Assert.Equal(
            new TokenizerError("Unterminated dollar-quoted string", new Location(1, 86)),
            ex.Error);
    }

    [Fact]
    public void tokenize_right_arrow()
    {
        var sql = "FUNCTION(key=>value)";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeWord("FUNCTION", null),
            new Token.LParen(),
            Token.MakeWord("key", null),
            new Token.RArrow(),
            Token.MakeWord("value", null),
            new Token.RParen(),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_is_null()
    {
        var sql = "a IS NULL";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeWord("a", null),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("IS"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("NULL"),
        };

        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_comment()
    {
        var testCases = new List<(string, List<Token>)>
        {
            (
                "0--this is a comment\n1",
                new List<Token>
                {
                    new Token.Number("0", false),
                    new Token.Whitespace(new Whitespace.SingleLineComment(
                        Prefix: "--",
                        Comment: "this is a comment")),
                    new Token.Whitespace(new Whitespace.Newline()),
                    new Token.Number("1", false),
                }
            ),
            (
                "0--this is a comment\r1",
                new List<Token>
                {
                    new Token.Number("0", false),
                    new Token.Whitespace(new Whitespace.SingleLineComment(
                        Prefix: "--",
                        Comment: "this is a comment\r1")),
                }
            ),
            (
                "0--this is a comment\r\n1",
                new List<Token>
                {
                    new Token.Number("0", false),
                    new Token.Whitespace(new Whitespace.SingleLineComment(
                        Prefix: "--",
                        Comment: "this is a comment\r")),
                    new Token.Whitespace(new Whitespace.Newline()),
                    new Token.Number("1", false),
                }
            ),
        };

        var dialect = new GenericDialect();

        foreach (var (sql, expected) in testCases)
        {
            var tokens = new Tokenizer(dialect, sql).Tokenize();
            Compare(expected, tokens);
        }
    }

    [Fact]
    public void tokenize_comment_postgres()
    {
        var sql = "1--\r0";

        var dialect = new PostgreSqlDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            new Token.Number("1", false),
            new Token.Whitespace(new Whitespace.SingleLineComment(
                Prefix: "--",
                Comment: "")),
            new Token.Whitespace(new Whitespace.Newline()), // Postgres treats \r as newline in single-line comments
            new Token.Number("0", false),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_comment_at_eof()
    {
        var sql = "--this is a comment";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            new Token.Whitespace(new Whitespace.SingleLineComment(
                Prefix: "--",
                Comment: "this is a comment")),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_multiline_comment()
    {
        var sql = "0/*multi-line\n* /comment*/1";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            new Token.Number("0", false),
            new Token.Whitespace(new Whitespace.MultiLineComment(
                "multi-line\n* /comment")),
            new Token.Number("1", false),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_nested_multiline_comment()
    {
        AllDialectsWhere(d => d.SupportsNestedComments).TokenizesTo(
            "0/*multi-line\n* \n/* comment \n /*comment*/*/ */ /comment*/1",
            new List<Token>
            {
                new Token.Number("0", false),
                new Token.Whitespace(new Whitespace.MultiLineComment(
                    "multi-line\n* \n/* comment \n /*comment*/*/ ")),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Div(),
                new Token.Word(new Word("comment", null, Keyword.COMMENT)),
                new Token.Mul(),
                new Token.Div(),
                new Token.Number("1", false),
            });

        AllDialectsWhere(d => d.SupportsNestedComments).TokenizesTo(
            "0/*multi-line\n* \n/* comment \n /*comment/**/ */ /comment*/*/1",
            new List<Token>
            {
                new Token.Number("0", false),
                new Token.Whitespace(new Whitespace.MultiLineComment(
                    "multi-line\n* \n/* comment \n /*comment/**/ */ /comment*/")),
                new Token.Number("1", false),
            });

        AllDialectsWhere(d => d.SupportsNestedComments).TokenizesTo(
            "SELECT 1/* a /* b */ c */0",
            new List<Token>
            {
                Token.MakeKeyword("SELECT"),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1", false),
                new Token.Whitespace(new Whitespace.MultiLineComment(" a /* b */ c ")),
                new Token.Number("0", false),
            });
    }

    [Fact]
    public void tokenize_nested_multiline_comment_empty()
    {
        AllDialectsWhere(d => d.SupportsNestedComments).TokenizesTo(
            "select 1/*/**/*/0",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1", false),
                new Token.Whitespace(new Whitespace.MultiLineComment("/**/")),
                new Token.Number("0", false),
            });
    }

    [Fact]
    public void tokenize_nested_comments_if_not_supported()
    {
        AllDialectsExcept(d => d.SupportsNestedComments).TokenizesTo(
            "SELECT 1/*/* nested comment */*/0",
            new List<Token>
            {
                Token.MakeKeyword("SELECT"),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1", false),
                new Token.Whitespace(new Whitespace.MultiLineComment(
                    "/* nested comment ")),
                new Token.Mul(),
                new Token.Div(),
                new Token.Number("0", false),
            });
    }

    [Fact]
    public void tokenize_multiline_comment_with_even_asterisks()
    {
        var sql = "\n/** Comment **/\n";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            new Token.Whitespace(new Whitespace.Newline()),
            new Token.Whitespace(new Whitespace.MultiLineComment("* Comment *")),
            new Token.Whitespace(new Whitespace.Newline()),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_unicode_whitespace()
    {
        var sql = " \u2003\n";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Whitespace(new Whitespace.Newline()),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_mismatched_quotes()
    {
        var sql = "\"foo";

        var dialect = new GenericDialect();
        var tokenizer = new Tokenizer(dialect, sql);
        var ex = Assert.Throws<TokenizerException>(() => tokenizer.Tokenize());
        Assert.Equal(
            new TokenizerError("Expected close delimiter '\"' before EOF.", new Location(1, 1)),
            ex.Error);
    }

    [Fact]
    public void tokenize_newlines()
    {
        var sql = "line1\nline2\rline3\r\nline4\r";

        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeWord("line1", null),
            new Token.Whitespace(new Whitespace.Newline()),
            Token.MakeWord("line2", null),
            new Token.Whitespace(new Whitespace.Newline()),
            Token.MakeWord("line3", null),
            new Token.Whitespace(new Whitespace.Newline()),
            Token.MakeWord("line4", null),
            new Token.Whitespace(new Whitespace.Newline()),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_mssql_top()
    {
        var sql = "SELECT TOP 5 [bar] FROM foo";
        var dialect = new MsSqlDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("TOP"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("5", false),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("bar", '['),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("FROM"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("foo", null),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_pg_regex_match()
    {
        var sql = "SELECT col ~ '^a', col ~* '^a', col !~ '^a', col !~* '^a'";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("col", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Tilde(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("^a"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("col", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.TildeAsterisk(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("^a"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("col", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.ExclamationMarkTilde(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("^a"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("col", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.ExclamationMarkTildeAsterisk(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("^a"),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_pg_like_match()
    {
        var sql = "SELECT col ~~ '_a%', col ~~* '_a%', col !~~ '_a%', col !~~* '_a%'";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("col", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.DoubleTilde(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("_a%"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("col", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.DoubleTildeAsterisk(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("_a%"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("col", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.ExclamationMarkDoubleTilde(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("_a%"),
            new Token.Comma(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("col", null),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.ExclamationMarkDoubleTildeAsterisk(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.SingleQuotedString("_a%"),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_quoted_identifier()
    {
        // r#" "a "" b" "a """ "c """"" "#
        var sql = " \"a \"\" b\" \"a \"\"\" \"c \"\"\"\"\" ";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("a \" b", '"'),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("a \"", '"'),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("c \"\"", '"'),
            new Token.Whitespace(new Whitespace.Space()),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_snowflake_div()
    {
        var sql = "field/1000";
        var dialect = new SnowflakeDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeWord("field", null),
            new Token.Div(),
            new Token.Number("1000", false),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_quoted_identifier_with_no_escape()
    {
        // r#" "a "" b" "a """ "c """"" "#
        var sql = " \"a \"\" b\" \"a \"\"\" \"c \"\"\"\"\" ";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql)
            .WithUnescape(false)
            .Tokenize();
        var expected = new List<Token>
        {
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("a \"\" b", '"'),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("a \"\"", '"'),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("c \"\"\"\"", '"'),
            new Token.Whitespace(new Whitespace.Space()),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_with_location()
    {
        var sql = "SELECT a,\n b";
        var dialect = new GenericDialect();
        var tokens = new Tokenizer(dialect, sql)
            .TokenizeWithLocation();
        var expected = new List<TokenWithSpan>
        {
            TokenWithSpan.At(Token.MakeKeyword("SELECT"), Location.From((1, 1)), Location.From((1, 7))),
            TokenWithSpan.At(
                new Token.Whitespace(new Whitespace.Space()),
                Location.From((1, 7)),
                Location.From((1, 8))),
            TokenWithSpan.At(Token.MakeWord("a", null), Location.From((1, 8)), Location.From((1, 9))),
            TokenWithSpan.At(new Token.Comma(), Location.From((1, 9)), Location.From((1, 10))),
            TokenWithSpan.At(
                new Token.Whitespace(new Whitespace.Newline()),
                Location.From((1, 10)),
                Location.From((2, 1))),
            TokenWithSpan.At(
                new Token.Whitespace(new Whitespace.Space()),
                Location.From((2, 1)),
                Location.From((2, 2))),
            TokenWithSpan.At(Token.MakeWord("b", null), Location.From((2, 2)), Location.From((2, 3))),
        };
        Compare(expected, tokens);
    }

    private static void Compare<T>(List<T> expected, List<T> actual)
    {
        //println!("------------------------------");
        //println!("tokens   = {:?}", actual);
        //println!("expected = {:?}", expected);
        //println!("------------------------------");
        Assert.Equal(expected, actual);
    }

    private static void CheckUnescape(string s, string? expected)
    {
        var quoted = "'" + s + "'";
        var state = new State(quoted, 0, 0);

        Assert.Equal(
            expected,
            Tokenizer.UnescapeSingleQuotedString(state));
    }

    [Fact]
    public void test_unescape()
    {
        CheckUnescape(@"\b", "\u0008");
        CheckUnescape(@"\f", "\u000C");
        CheckUnescape(@"\t", "\t");
        CheckUnescape(@"\r\n", "\r\n");
        CheckUnescape(@"\/", "/");
        CheckUnescape(@"/", "/");
        CheckUnescape(@"\\", "\\");

        // 16 and 32-bit hexadecimal Unicode character value
        CheckUnescape(@"\u0001", "\u0001");
        CheckUnescape(@"\u4c91", "\u4c91");
        CheckUnescape(@"\u4c916", "\u4c91" + "6");
        CheckUnescape(@"\u4c", null);
        CheckUnescape(@"\u0000", null);
        CheckUnescape(@"\U0010FFFF", "\U0010FFFF");
        CheckUnescape(@"\U00110000", null);
        CheckUnescape(@"\U00000000", null);
        CheckUnescape(@"\u", null);
        CheckUnescape(@"\U", null);
        CheckUnescape(@"\U1010FFFF", null);

        // hexadecimal byte value
        CheckUnescape(@"\x4B", "\u004b");
        CheckUnescape(@"\x4", "\u0004");
        CheckUnescape(@"\x4L", "\u0004" + "L");
        CheckUnescape(@"\x", "x");
        CheckUnescape(@"\xP", "xP");
        CheckUnescape(@"\x0", null);
        CheckUnescape(@"\xCAD", null);
        CheckUnescape(@"\xA9", null);

        // octal byte value
        CheckUnescape(@"\1", "\u0001");
        CheckUnescape(@"\12", "\u000a");
        CheckUnescape(@"\123", "\u0053");
        CheckUnescape(@"\1232", "\u0053" + "2");
        CheckUnescape(@"\4", "\u0004");
        CheckUnescape(@"\45", "\u0025");
        CheckUnescape(@"\450", "\u0028");
        CheckUnescape(@"\603", null);
        CheckUnescape(@"\0", null);
        CheckUnescape(@"\080", null);

        // others
        CheckUnescape(@"\9", "9");
        CheckUnescape(@"''", "'");
        CheckUnescape(
            @"Hello\r\nRust/\u4c91 SQL Parser\U0010ABCD\1232",
            "Hello\r\nRust/\u4c91 SQL Parser\U0010ABCD\u0053" + "2");
        CheckUnescape(@"Hello\0", null);
        CheckUnescape(@"Hello\xCADRust", null);
    }

    // #[derive(Debug)] struct NumericPrefixDialect; impl Dialect for NumericPrefixDialect { ... }
    // (no Rust é declarado dentro do teste tokenize_numeric_prefix_trait)
    private sealed class NumericPrefixDialect : Dialect
    {
        public override bool IsIdentifierStart(char ch) =>
            (ch >= 'a' && ch <= 'z')
            || (ch >= 'A' && ch <= 'Z')
            || (ch >= '0' && ch <= '9')
            || ch == '$';

        public override bool IsIdentifierPart(char ch) =>
            (ch >= 'a' && ch <= 'z')
            || (ch >= 'A' && ch <= 'Z')
            || (ch >= '0' && ch <= '9')
            || ch == '_'
            || ch == '$'
            || ch == '{'
            || ch == '}';

        public override bool SupportsNumericPrefix => true;
    }

    [Fact]
    public void tokenize_numeric_prefix_trait()
    {
        TokenizeNumericPrefixInner(new NumericPrefixDialect());
        TokenizeNumericPrefixInner(new HiveDialect());
        TokenizeNumericPrefixInner(new MySqlDialect());
    }

    private static void TokenizeNumericPrefixInner(Dialect dialect)
    {
        var sql = "SELECT * FROM 1";
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Mul(),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("FROM"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1", false),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_quoted_string_escape()
    {
        var dialect = new SnowflakeDialect();
        foreach (var (sql, expected, expectedUnescaped) in new (string, string, string)[]
        {
            (@"'%a\'%b'", @"%a\'%b", @"%a'%b"),
            (@"'a\'\'b\'c\'d'", @"a\'\'b\'c\'d", @"a''b'c'd"),
            (@"'\\'", @"\\", @"\"),
            (
                @"'\0\a\b\f\n\r\t\Z'",
                @"\0\a\b\f\n\r\t\Z",
                "\0\u0007\u0008\u000C\n\r\t\u001A"
            ),
            (@"'\""'", @"\""", "\""),
            (@"'\\a\\b\'c'", @"\\a\\b\'c", @"\a\b'c"),
            (@"'\'abcd'", @"\'abcd", @"'abcd"),
            (@"'''a''b'", @"''a''b", @"'a'b"),
            (@"'\q'", @"\q", @"q"),
            (@"'\%\_'", @"\%\_", @"%_"),
            (@"'\\%\\_'", @"\\%\\_", @"\%\_"),
        })
        {
            var tokens = new Tokenizer(dialect, sql)
                .WithUnescape(false)
                .Tokenize();
            var expectedTokens = new List<Token> { new Token.SingleQuotedString(expected) };
            Compare(expectedTokens, tokens);

            tokens = new Tokenizer(dialect, sql)
                .WithUnescape(true)
                .Tokenize();
            expectedTokens = new List<Token> { new Token.SingleQuotedString(expectedUnescaped) };
            Compare(expectedTokens, tokens);
        }

        foreach (var sql in new[] { @"'\'", @"'ab\'" })
        {
            var tokenizer = new Tokenizer(dialect, sql);
            Assert.Equal(
                "Unterminated string literal",
                Assert.Throws<TokenizerException>(() => tokenizer.Tokenize()).Error.Message);
        }

        // Non-escape dialect
        foreach (var (sql, expected) in new[] { (@"'\'", @"\"), (@"'ab\'", @"ab\") })
        {
            var genericDialect = new GenericDialect();
            var tokens = new Tokenizer(genericDialect, sql).Tokenize();

            var expectedTokens = new List<Token> { new Token.SingleQuotedString(expected) };

            Compare(expectedTokens, tokens);
        }

        // MySQL special case for LIKE escapes
        foreach (var (sql, expected) in new[] { (@"'\%'", @"\%"), (@"'\_'", @"\_") })
        {
            var mySqlDialect = new MySqlDialect();
            var tokens = new Tokenizer(mySqlDialect, sql).Tokenize();

            var expectedTokens = new List<Token> { new Token.SingleQuotedString(expected) };

            Compare(expectedTokens, tokens);
        }
    }

    [Fact]
    public void tokenize_triple_quoted_string()
    {
        void Check(
            char q, // The quote character to test
            char r, // An alternate quote character.
            Func<string, Token> quoteToken)
        {
            var bigQueryDialect = new BigQueryDialect();

            foreach (var (sql, expected, expectedUnescaped) in new (string, string, string)[]
            {
                // Empty string
                ($"{q}{q}{q}{q}{q}{q}", "", ""),
                // Should not count escaped quote as end of string.
                (
                    $@"{q}{q}{q}ab{q}{q}\{q}{q}cd{q}{q}{q}",
                    $@"ab{q}{q}\{q}{q}cd",
                    $"ab{q}{q}{q}{q}cd"
                ),
                // Simple string
                (
                    $"{q}{q}{q}abc{q}{q}{q}",
                    "abc",
                    "abc"
                ),
                // Mix single-double quotes unescaped.
                (
                    $"{q}{q}{q}ab{r}{r}{r}c{r}def{r}{r}{r}{q}{q}{q}",
                    $"ab{r}{r}{r}c{r}def{r}{r}{r}",
                    $"ab{r}{r}{r}c{r}def{r}{r}{r}"
                ),
                // Escaped quote.
                (
                    $@"{q}{q}{q}ab{q}{q}c{q}{q}\{q}de{q}{q}f{q}{q}{q}",
                    $@"ab{q}{q}c{q}{q}\{q}de{q}{q}f",
                    $"ab{q}{q}c{q}{q}{q}de{q}{q}f"
                ),
                // backslash-escaped quote characters.
                (
                    $@"{q}{q}{q}a\'\'b\'c\'d{q}{q}{q}",
                    @"a\'\'b\'c\'d",
                    @"a''b'c'd"
                ),
                // backslash-escaped characters
                (
                    $@"{q}{q}{q}abc\0\n\rdef{q}{q}{q}",
                    @"abc\0\n\rdef",
                    "abc\0\n\rdef"
                ),
            })
            {
                var tokens = new Tokenizer(bigQueryDialect, sql)
                    .WithUnescape(false)
                    .Tokenize();
                var expectedTokens = new List<Token> { quoteToken(expected) };
                Compare(expectedTokens, tokens);

                tokens = new Tokenizer(bigQueryDialect, sql)
                    .WithUnescape(true)
                    .Tokenize();
                expectedTokens = new List<Token> { quoteToken(expectedUnescaped) };
                Compare(expectedTokens, tokens);
            }

            foreach (var sql in new[]
            {
                $@"{q}{q}{q}{q}{q}\{q}",
                $@"{q}{q}{q}abc{q}{q}\{q}",
                $"{q}{q}{q}{q}",
                $"{q}{q}{q}{r}{r}",
                $"{q}{q}{q}abc{q}",
                $"{q}{q}{q}abc{q}{q}",
                $"{q}{q}{q}abc",
            })
            {
                var errDialect = new BigQueryDialect();
                var tokenizer = new Tokenizer(errDialect, sql);
                Assert.Equal(
                    "Unterminated string literal",
                    Assert.Throws<TokenizerException>(() => tokenizer.Tokenize()).Error.Message);
            }
        }

        Check('"', '\'', v => new Token.TripleDoubleQuotedString(v));

        Check('\'', '"', v => new Token.TripleSingleQuotedString(v));

        var dialect = new BigQueryDialect();

        var sql1 = "\"\"''"; // r#"""''"#
        var tokens1 = new Tokenizer(dialect, sql1)
            .WithUnescape(true)
            .Tokenize();
        var expected1 = new List<Token>
        {
            new Token.DoubleQuotedString(""),
            new Token.SingleQuotedString(""),
        };
        Compare(expected1, tokens1);

        var sql2 = "''\"\""; // r#"''"""#
        var tokens2 = new Tokenizer(dialect, sql2)
            .WithUnescape(true)
            .Tokenize();
        var expected2 = new List<Token>
        {
            new Token.SingleQuotedString(""),
            new Token.DoubleQuotedString(""),
        };
        Compare(expected2, tokens2);

        // Non-triple quoted string dialect
        var snowflakeDialect = new SnowflakeDialect();
        var sql3 = "''''''"; // r#"''''''"#
        var tokens3 = new Tokenizer(snowflakeDialect, sql3).Tokenize();
        var expected3 = new List<Token> { new Token.SingleQuotedString("''") };
        Compare(expected3, tokens3);
    }

    [Fact]
    public void test_mysql_users_grantees()
    {
        var dialect = new MySqlDialect();

        var sql = "CREATE USER `root`@`%`";
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("CREATE"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("USER"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("root", '`'),
            new Token.AtSign(),
            Token.MakeWord("%", '`'),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void test_postgres_abs_without_space_and_string_literal()
    {
        var dialect = new MySqlDialect();

        var sql = "SELECT @'1'";
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.AtSign(),
            new Token.SingleQuotedString("1"),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void test_postgres_abs_without_space_and_quoted_column()
    {
        var dialect = new MySqlDialect();

        var sql = "SELECT @\"bar\" FROM foo"; // r#"SELECT @"bar" FROM foo"#
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.AtSign(),
            new Token.DoubleQuotedString("bar"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeKeyword("FROM"),
            new Token.Whitespace(new Whitespace.Space()),
            Token.MakeWord("foo", null),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void test_national_strings_backslash_escape_not_supported()
    {
        AllDialectsWhere(dialect =>
            !dialect.SupportsStringLiteralBackslashEscape
                && dialect.SupportsNationalStringLiteral)
        .TokenizesTo(
            "select n'''''\\'",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.NationalStringLiteral("''\\"),
            });
    }

    [Fact]
    public void test_national_strings_backslash_escape_supported()
    {
        AllDialectsWhere(dialect => dialect.SupportsStringLiteralBackslashEscape)
            .TokenizesTo(
                "select n'''''\\''",
                new List<Token>
                {
                    Token.MakeKeyword("select"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.NationalStringLiteral("'''"),
                });
    }

    [Fact]
    public void test_string_escape_constant_not_supported()
    {
        AllDialectsWhere(dialect => !dialect.SupportsStringEscapeConstant).TokenizesTo(
            "select e'...'",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("e", null),
                new Token.SingleQuotedString("..."),
            });

        AllDialectsWhere(dialect => !dialect.SupportsStringEscapeConstant).TokenizesTo(
            "select E'...'",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("E", null),
                new Token.SingleQuotedString("..."),
            });
    }

    [Fact]
    public void test_string_escape_constant_supported()
    {
        AllDialectsWhere(dialect => dialect.SupportsStringEscapeConstant).TokenizesTo(
            "select e'\\''",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.EscapedStringLiteral("'"),
            });

        AllDialectsWhere(dialect => dialect.SupportsStringEscapeConstant).TokenizesTo(
            "select E'\\''",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.EscapedStringLiteral("'"),
            });
    }

    [Fact]
    public void test_whitespace_required_after_single_line_comment()
    {
        AllDialectsWhere(dialect => dialect.RequiresSingleLineCommentWhitespace)
            .TokenizesTo(
                "SELECT --'abc'",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.Minus(),
                    new Token.Minus(),
                    new Token.SingleQuotedString("abc"),
                });

        AllDialectsWhere(dialect => dialect.RequiresSingleLineCommentWhitespace)
            .TokenizesTo(
                "SELECT -- 'abc'",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.Whitespace(new Whitespace.SingleLineComment(
                        Prefix: "--",
                        Comment: " 'abc'")),
                });

        AllDialectsWhere(dialect => dialect.RequiresSingleLineCommentWhitespace)
            .TokenizesTo(
                "SELECT --",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.Minus(),
                    new Token.Minus(),
                });

        AllDialectsWhere(d => d.RequiresSingleLineCommentWhitespace).TokenizesTo(
            "--\n-- Table structure for table...\n--\n",
            new List<Token>
            {
                new Token.Whitespace(new Whitespace.SingleLineComment(
                    Prefix: "--",
                    Comment: "")),
                new Token.Whitespace(new Whitespace.Newline()),
                new Token.Whitespace(new Whitespace.SingleLineComment(
                    Prefix: "--",
                    Comment: " Table structure for table...")),
                new Token.Whitespace(new Whitespace.Newline()),
                new Token.Whitespace(new Whitespace.SingleLineComment(
                    Prefix: "--",
                    Comment: "")),
                new Token.Whitespace(new Whitespace.Newline()),
            });
    }

    [Fact]
    public void test_whitespace_not_required_after_single_line_comment()
    {
        AllDialectsWhere(dialect => !dialect.RequiresSingleLineCommentWhitespace)
            .TokenizesTo(
                "SELECT --'abc'",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.Whitespace(new Whitespace.SingleLineComment(
                        Prefix: "--",
                        Comment: "'abc'")),
                });

        AllDialectsWhere(dialect => !dialect.RequiresSingleLineCommentWhitespace)
            .TokenizesTo(
                "SELECT -- 'abc'",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.Whitespace(new Whitespace.SingleLineComment(
                        Prefix: "--",
                        Comment: " 'abc'")),
                });

        AllDialectsWhere(dialect => !dialect.RequiresSingleLineCommentWhitespace)
            .TokenizesTo(
                "SELECT --",
                new List<Token>
                {
                    Token.MakeKeyword("SELECT"),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.Whitespace(new Whitespace.SingleLineComment(
                        Prefix: "--",
                        Comment: "")),
                });
    }

    [Fact]
    public void test_tokenize_identifiers_numeric_prefix()
    {
        AllDialectsWhere(dialect => dialect.SupportsNumericPrefix)
            .TokenizesTo("123abc", new List<Token> { Token.MakeWord("123abc", null) });

        AllDialectsWhere(dialect => dialect.SupportsNumericPrefix)
            .TokenizesTo("12e34", new List<Token> { new Token.Number("12e34", false) });

        AllDialectsWhere(dialect => dialect.SupportsNumericPrefix).TokenizesTo(
            "t.12e34",
            new List<Token>
            {
                Token.MakeWord("t", null),
                new Token.Period(),
                Token.MakeWord("12e34", null),
            });

        AllDialectsWhere(dialect => dialect.SupportsNumericPrefix).TokenizesTo(
            "t.1two3",
            new List<Token>
            {
                Token.MakeWord("t", null),
                new Token.Period(),
                Token.MakeWord("1two3", null),
            });
    }

    [Fact]
    public void tokenize_period_underscore()
    {
        var sql = "SELECT table._col";
        // a dialect that supports underscores in numeric literals
        var dialect = new PostgreSqlDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();

        var expected = new List<Token>
        {
            Token.MakeKeyword("SELECT"),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Word(new Word("table", null, Keyword.TABLE)),
            new Token.Period(),
            new Token.Word(new Word("_col", null, Keyword.NoKeyword)),
        };

        Compare(expected, tokens);

        // if let Ok(tokens) = ... { panic!("Tokenizer should have failed on {sql}, but it succeeded with {tokens:?}") }
        var sql2 = "SELECT ._123";
        Assert.Throws<TokenizerException>(() => new Tokenizer(dialect, sql2).Tokenize());

        var sql3 = "SELECT ._abc";
        Assert.Throws<TokenizerException>(() => new Tokenizer(dialect, sql3).Tokenize());
    }

    [Fact]
    public void tokenize_question_mark()
    {
        var dialect = new PostgreSqlDialect();
        var sql = "SELECT x ? y";
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        Compare(
            tokens,
            new List<Token>
            {
                Token.MakeKeyword("SELECT"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("x", null),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Question(),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("y", null),
            });
    }

    [Fact]
    public void tokenize_multiline_comment_with_comment_hint()
    {
        var sql = "0/*! word */1";

        var dialect = new MySqlDialect();
        var tokens = new Tokenizer(dialect, sql).Tokenize();
        var expected = new List<Token>
        {
            new Token.Number("0", false),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Word(new Word("word", null, Keyword.NoKeyword)),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1", false),
        };
        Compare(expected, tokens);
    }

    [Fact]
    public void tokenize_multiline_comment_with_comment_hint_and_version()
    {
        var sqlMulti = "0 /*!50110 KEY_BLOCK_SIZE = 1024*/ 1";
        var dialect = new MySqlDialect();
        var tokens = new Tokenizer(dialect, sqlMulti).Tokenize();
        var expected = new List<Token>
        {
            new Token.Number("0", false),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Word(new Word("KEY_BLOCK_SIZE", null, Keyword.KEY_BLOCK_SIZE)),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Eq(),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1024", false),
            new Token.Whitespace(new Whitespace.Space()),
            new Token.Number("1", false),
        };
        Compare(expected, tokens);

        tokens = new Tokenizer(dialect, "0 /*!50110 */ 1")
            .Tokenize();
        Compare(
            new List<Token>
            {
                new Token.Number("0", false),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1", false),
            },
            tokens);

        tokens = new Tokenizer(dialect, "0 /*!*/ 1").Tokenize();
        Compare(
            new List<Token>
            {
                new Token.Number("0", false),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1", false),
            },
            tokens);
        tokens = new Tokenizer(dialect, "0 /*!   */ 1").Tokenize();
        Compare(
            new List<Token>
            {
                new Token.Number("0", false),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Number("1", false),
            },
            tokens);
    }

    [Fact]
    public void tokenize_lt()
    {
        AllDialects().TokenizesTo(
            "select a <-50",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("a", null),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Lt(),
                new Token.Minus(),
                new Token.Number("50", false),
            });
        AllDialects().TokenizesTo(
            "select a <+50",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("a", null),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Lt(),
                new Token.Plus(),
                new Token.Number("50", false),
            });
        AllDialects().TokenizesTo(
            "select a <=-50",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("a", null),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.LtEq(),
                new Token.Minus(),
                new Token.Number("50", false),
            });
        AllDialects().TokenizesTo(
            "select a <=+50",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("a", null),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.LtEq(),
                new Token.Plus(),
                new Token.Number("50", false),
            });
        AllDialectsWhere(d => d.SupportsGeometricTypes).TokenizesTo(
            "select a <->b",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("a", null),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.TwoWayArrow(),
                Token.MakeWord("b", null),
            });

        AllDialects().TokenizesTo(
            "select a <-b",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("a", null),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Lt(),
                new Token.Minus(),
                Token.MakeWord("b", null),
            });
        AllDialects().TokenizesTo(
            "select a <+b",
            new List<Token>
            {
                Token.MakeKeyword("select"),
                new Token.Whitespace(new Whitespace.Space()),
                Token.MakeWord("a", null),
                new Token.Whitespace(new Whitespace.Space()),
                new Token.Lt(),
                new Token.Plus(),
                Token.MakeWord("b", null),
            });
    }

    [Fact]
    public void test_word_display_quote_escaping()
    {
        Assert.Equal(
            "\"a\"\"b\"",
            new Word(Value: "a\"b", QuoteStyle: '"', Keyword: Keyword.NoKeyword).ToString());
        Assert.Equal(
            "`a``b`",
            new Word(Value: "a`b", QuoteStyle: '`', Keyword: Keyword.NoKeyword).ToString());
        Assert.Equal(
            "[a b]",
            new Word(Value: "a b", QuoteStyle: '[', Keyword: Keyword.NoKeyword).ToString());
    }

    [Fact]
    public void tokenize_period_before_digits_after_identifier_or_bracket()
    {
        var dialect = new ClickHouseDialect();
        foreach (var (sql, expected) in new (string, List<Token>)[]
        {
            (
                "t.1",
                new List<Token>
                {
                    Token.MakeWord("t", null),
                    new Token.Period(),
                    new Token.Number("1", false),
                }
            ),
            (
                "(1).1",
                new List<Token>
                {
                    new Token.LParen(),
                    new Token.Number("1", false),
                    new Token.RParen(),
                    new Token.Period(),
                    new Token.Number("1", false),
                }
            ),
            (
                "a[1].1",
                new List<Token>
                {
                    Token.MakeWord("a", null),
                    new Token.LBracket(),
                    new Token.Number("1", false),
                    new Token.RBracket(),
                    new Token.Period(),
                    new Token.Number("1", false),
                }
            ),
            (
                "t.1.2",
                new List<Token>
                {
                    Token.MakeWord("t", null),
                    new Token.Period(),
                    new Token.Number("1", false),
                    new Token.Period(),
                    new Token.Number("2", false),
                }
            ),
            (
                "t .1",
                new List<Token>
                {
                    Token.MakeWord("t", null),
                    new Token.Whitespace(new Whitespace.Space()),
                    new Token.Number(".1", false),
                }
            ),
            ("1.5", new List<Token> { new Token.Number("1.5", false) }),
            (".5", new List<Token> { new Token.Number(".5", false) }),
        })
        {
            var tokens = new Tokenizer(dialect, sql).Tokenize();
            Compare(expected, tokens);
        }

        // `let tokens` repetido fora do laço: nome distinto (CS0136, caso 72).
        var genericTokens = new Tokenizer(new GenericDialect(), "t.1")
            .Tokenize();
        Compare(
            new List<Token>
            {
                Token.MakeWord("t", null),
                new Token.Number(".1", false),
            },
            genericTokens);
    }
}
