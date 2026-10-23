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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/comments.rs (mod tests).
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Core.Tests;

public class CommentsTests
{
    [Fact]
    public void test_find()
    {
        // ```
        // -- abc
        //   /* hello */--, world
        // /* def
        //  ghi
        //  jkl
        // */
        // ```
        var comments = new Comments();
        comments.Offer(new CommentWithSpan(
            Comment: new Comment.SingleLine(Content: " abc", Prefix: "--"),
            Span: new Span(Location.From((1, 1)), Location.From((1, 7))),
            PrevTokenEnd: null,
            NextTokenStart: null));
        comments.Offer(new CommentWithSpan(
            Comment: new Comment.MultiLine(" hello "),
            Span: new Span(Location.From((2, 3)), Location.From((2, 14))),
            PrevTokenEnd: null,
            NextTokenStart: null));
        comments.Offer(new CommentWithSpan(
            Comment: new Comment.SingleLine(Content: ", world", Prefix: "--"),
            Span: new Span(Location.From((2, 14)), Location.From((2, 21))),
            PrevTokenEnd: null,
            NextTokenStart: null));
        comments.Offer(new CommentWithSpan(
            Comment: new Comment.MultiLine(" def\n ghi\n jkl\n"),
            Span: new Span(Location.From((3, 3)), Location.From((7, 1))),
            PrevTokenEnd: null,
            NextTokenStart: null));

        // ~ end-points only --------------------------------------------------
        Assert.Equal(Find(comments, RangeBounds.RangeTo(new Location(0, 0))), new List<string>());
        Assert.Equal(Find(comments, RangeBounds.RangeTo(new Location(2, 1))), new List<string> { " abc" });
        Assert.Equal(Find(comments, RangeBounds.RangeTo(new Location(2, 3))), new List<string> { " abc" });
        Assert.Equal(
            Find(comments, RangeBounds.RangeToInclusive(new Location(2, 3))),
            new List<string> { " abc", " hello " });
        Assert.Equal(
            Find(comments, RangeBounds.RangeToInclusive(new Location(2, 3))),
            new List<string> { " abc", " hello " });
        Assert.Equal(
            Find(comments, RangeBounds.RangeTo(new Location(2, 15))),
            new List<string> { " abc", " hello ", ", world" });

        // ~ start-points only ------------------------------------------------
        Assert.Equal(
            Find(comments, RangeBounds.RangeFrom(new Location(1000, 1000))),
            new List<string>());
        Assert.Equal(
            Find(comments, RangeBounds.RangeFrom(new Location(2, 14))),
            new List<string> { ", world", " def\n ghi\n jkl\n" });
        Assert.Equal(
            Find(comments, RangeBounds.RangeFrom(new Location(2, 15))),
            new List<string> { " def\n ghi\n jkl\n" });
        Assert.Equal(
            Find(comments, RangeBounds.RangeFrom(new Location(0, 0))),
            new List<string> { " abc", " hello ", ", world", " def\n ghi\n jkl\n" });
        Assert.Equal(
            Find(comments, RangeBounds.RangeFrom(new Location(1, 1))),
            new List<string> { " abc", " hello ", ", world", " def\n ghi\n jkl\n" });

        // ~ ranges -----------------------------------------------------------
        Assert.Equal(
            Find(comments, RangeBounds.Range(new Location(2, 1), new Location(1, 1))),
            new List<string>());
        Assert.Equal(
            Find(comments, RangeBounds.Range(new Location(1, 1), new Location(2, 3))),
            new List<string> { " abc" });
        Assert.Equal(
            Find(comments, RangeBounds.RangeInclusive(new Location(1, 1), new Location(2, 3))),
            new List<string> { " abc", " hello " });
        Assert.Equal(
            Find(comments, RangeBounds.RangeInclusive(new Location(1, 1), new Location(2, 10))),
            new List<string> { " abc", " hello " });
        Assert.Equal(
            Find(comments, RangeBounds.RangeInclusive(new Location(1, 1), new Location(2, 14))),
            new List<string> { " abc", " hello ", ", world" });
        Assert.Equal(
            Find(comments, RangeBounds.Range(new Location(1, 1), new Location(2, 15))),
            new List<string> { " abc", " hello ", ", world" });

        // ~ find everything --------------------------------------------------
        Assert.Equal(
            Find(comments, RangeBounds.RangeFull<Location>()),
            new List<string> { " abc", " hello ", ", world", " def\n ghi\n jkl\n" });
    }

    private static List<string> Find(Comments comments, RangeBounds<Location> range) =>
        comments.Find(range).Select(c => c.Comment.AsStr()).ToList();

    [Fact]
    public void test_find_before()
    {
        var firstToken = new Location(2, 1);
        var secondToken = new Location(4, 1);
        var comments = new Comments();
        foreach (var comment in new[]
        {
            new CommentWithSpan(
                Comment: new Comment.SingleLine(Content: " first", Prefix: "--"),
                Span: new Span(Location.From((1, 1)), Location.From((1, 9))),
                PrevTokenEnd: null,
                NextTokenStart: firstToken),
            new CommentWithSpan(
                Comment: new Comment.SingleLine(Content: " second", Prefix: "--"),
                Span: new Span(Location.From((2, 11)), Location.From((2, 20))),
                PrevTokenEnd: new Location(2, 10),
                NextTokenStart: secondToken),
            new CommentWithSpan(
                Comment: new Comment.SingleLine(Content: " third", Prefix: "--"),
                Span: new Span(Location.From((3, 1)), Location.From((3, 9))),
                PrevTokenEnd: new Location(2, 10),
                NextTokenStart: secondToken),
            new CommentWithSpan(
                Comment: new Comment.SingleLine(Content: " tail", Prefix: "--"),
                Span: new Span(Location.From((4, 11)), Location.From((4, 18))),
                PrevTokenEnd: new Location(4, 10),
                NextTokenStart: null),
        })
        {
            comments.Offer(comment);
        }

        static List<string> FindBefore(Comments comments, Location location) =>
            comments.FindBefore(location).Select(c => c.Comment.AsStr()).ToList();

        Assert.Equal(FindBefore(comments, firstToken), new List<string> { " first" });
        Assert.Equal(
            FindBefore(comments, secondToken),
            new List<string> { " second", " third" });
        foreach (var location in new[]
        {
            new Location(0, 0),
            new Location(1, 3),
            new Location(2, 8),
            new Location(3, 12),
            new Location(4, 3),
            new Location(5, 1),
        })
        {
            TestUtils.AssertEqualWithMessage(
                FindBefore(comments, location),
                new List<string>(),
                location.ToDebugString());
        }
        Assert.Equal(
            FindBefore(new Comments(), firstToken),
            new List<string>());
    }
}
