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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_comments.rs.
// Modificado: tradução de Rust para C#. A string `r#"..."#` com várias linhas virou concatenação
// com "\n" explícito (o arquivo C# pode estar em CRLF).

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

/// <summary>Test comment extraction from SQL source code.</summary>
public class SqlParserCommentsTests
{
    [Fact]
    public void parse_sql_with_comments()
    {
        var sql =
            "\n" +
            "-- second line comment\n" +
            "select * from /* inline comment after `from` */ dual;\n" +
            "\n" +
            "/*select\n" +
            "some\n" +
            "more*/\n" +
            "\n" +
            "  -- end-of-script-with-no-newline";

        var comments = TestUtils.AllDialects().OneOfIdenticalResults(dialect =>
            Parser.ParseSqlWithComments(dialect, sql).Comments.ToVec());

        Assert.Equal(
            comments,
            new List<CommentWithSpan>
            {
                new CommentWithSpan(
                    Comment: new Comment.SingleLine(
                        Content: " second line comment",
                        Prefix: "--"),
                    Span: new Span(Location.From((2, 1)), Location.From((2, 23))),
                    PrevTokenEnd: null,
                    NextTokenStart: new Location(3, 1)),
                new CommentWithSpan(
                    Comment: new Comment.MultiLine(" inline comment after `from` "),
                    Span: new Span(Location.From((3, 15)), Location.From((3, 48))),
                    PrevTokenEnd: new Location(3, 14),
                    NextTokenStart: new Location(3, 49)),
                new CommentWithSpan(
                    Comment: new Comment.MultiLine("select\nsome\nmore"),
                    Span: new Span(Location.From((5, 1)), Location.From((7, 7))),
                    PrevTokenEnd: new Location(3, 54),
                    NextTokenStart: null),
                new CommentWithSpan(
                    Comment: new Comment.SingleLine(
                        Content: " end-of-script-with-no-newline",
                        Prefix: "--"),
                    Span: new Span(Location.From((9, 3)), Location.From((9, 35))),
                    PrevTokenEnd: new Location(3, 54),
                    NextTokenStart: null),
            });
    }

    [Fact]
    public void parse_sql_with_comments_between_tokens()
    {
        var sql =
            "SELECT 1; /* a */\n" +
            "\n" +
            "-- b\n" +
            "/* c */ SELECT 2";
        var comments = TestUtils.AllDialects().OneOfIdenticalResults(dialect =>
            Parser.ParseSqlWithComments(dialect, sql).Comments.ToVec());

        Assert.Equal(
            comments,
            new List<CommentWithSpan>
            {
                new CommentWithSpan(
                    Comment: new Comment.MultiLine(" a "),
                    Span: new Span(Location.From((1, 11)), Location.From((1, 18))),
                    PrevTokenEnd: new Location(1, 10),
                    NextTokenStart: new Location(4, 9)),
                new CommentWithSpan(
                    Comment: new Comment.SingleLine(
                        Content: " b",
                        Prefix: "--"),
                    Span: new Span(Location.From((3, 1)), Location.From((3, 5))),
                    PrevTokenEnd: new Location(1, 10),
                    NextTokenStart: new Location(4, 9)),
                new CommentWithSpan(
                    Comment: new Comment.MultiLine(" c "),
                    Span: new Span(Location.From((4, 1)), Location.From((4, 8))),
                    PrevTokenEnd: new Location(1, 10),
                    NextTokenStart: new Location(4, 9)),
            });
    }

    [Fact]
    public void parse_sql_with_comments_only()
    {
        foreach (var (sql, expected) in new (string, List<CommentWithSpan>)[]
        {
            ("", new List<CommentWithSpan>()),
            (
                "-- a\n" +
                "/* b */",
                new List<CommentWithSpan>
                {
                    new CommentWithSpan(
                        Comment: new Comment.SingleLine(
                            Content: " a",
                            Prefix: "--"),
                        Span: new Span(Location.From((1, 1)), Location.From((1, 5))),
                        PrevTokenEnd: null,
                        NextTokenStart: null),
                    new CommentWithSpan(
                        Comment: new Comment.MultiLine(" b "),
                        Span: new Span(Location.From((2, 1)), Location.From((2, 8))),
                        PrevTokenEnd: null,
                        NextTokenStart: null),
                }
            ),
        })
        {
            var comments = TestUtils.AllDialects().OneOfIdenticalResults(dialect =>
                Parser.ParseSqlWithComments(dialect, sql).Comments.ToVec());
            Assert.Equal(comments, expected);
        }
    }

    [Fact]
    public void into_comments_with_eof()
    {
        var tokens = new Tokenizer(new GenericDialect(), "SELECT 1; -- tail")
            .TokenizeWithLocation();
        var eof = tokens[tokens.Count - 1].Span.End;
        tokens.Add(new TokenWithSpan(
            Token: new Token.EOF(),
            Span: new Span(eof, eof)));
        var comments = new Parser(new GenericDialect())
            .WithTokensWithLocations(tokens)
            .IntoComments();

        Assert.Equal(
            comments.ToVec(),
            new List<CommentWithSpan>
            {
                new CommentWithSpan(
                    Comment: new Comment.SingleLine(
                        Content: " tail",
                        Prefix: "--"),
                    Span: new Span(Location.From((1, 11)), Location.From((1, 18))),
                    PrevTokenEnd: new Location(1, 10),
                    NextTokenStart: null),
            });
    }
}
