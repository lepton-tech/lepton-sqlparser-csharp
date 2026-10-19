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

        Comments comments;
        try
        {
            (_, comments) = Parser.ParseSqlWithComments(new GenericDialect(), sql);
        }
        catch (ParserException e)
        {
            throw new InvalidOperationException($"Invalid sql script: {e.Error}");
        }

        Assert.Equal(
            comments.ToVec(), // Vec::from(comments)
            new List<CommentWithSpan>
            {
                new CommentWithSpan(
                    Comment: new Comment.SingleLine(
                        Content: " second line comment",
                        Prefix: "--"),
                    Span: new Span(Location.From((2, 1)), Location.From((2, 23)))),
                new CommentWithSpan(
                    Comment: new Comment.MultiLine(" inline comment after `from` "),
                    Span: new Span(Location.From((3, 15)), Location.From((3, 48)))),
                new CommentWithSpan(
                    Comment: new Comment.MultiLine("select\nsome\nmore"),
                    Span: new Span(Location.From((5, 1)), Location.From((7, 7)))),
                new CommentWithSpan(
                    Comment: new Comment.SingleLine(
                        Content: " end-of-script-with-no-newline",
                        Prefix: "--"),
                    Span: new Span(Location.From((9, 3)), Location.From((9, 35)))),
            });
    }
}
