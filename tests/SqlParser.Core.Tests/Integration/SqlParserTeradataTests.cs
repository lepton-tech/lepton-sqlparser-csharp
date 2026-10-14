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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_teradata.rs.
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

/// <summary>Test SQL syntax, specific to <see cref="TeradataDialect"/>.</summary>
public class SqlParserTeradataTests
{
    private static TestedDialects Teradata() =>
        new TestedDialects(new List<Dialect> { new TeradataDialect() });

    private static TestedDialects TeradataAndGeneric() =>
        new TestedDialects(new List<Dialect> { new TeradataDialect(), new GenericDialect() });

    [Fact]
    public void dialect_methods()
    {
        Dialect d = new TeradataDialect();
        // Literal como `expected` (o analisador xUnit2000 é erro com TreatWarningsAsErrors).
        Assert.Equal('"', d.IdentifierQuoteStyle("x"));
        Assert.True(d.IsDelimitedIdentifierStart('"'));
        Assert.True(!d.IsDelimitedIdentifierStart('`'));
        Assert.True(d.IsIdentifierStart('$'));
        Assert.True(d.IsIdentifierStart('#'));
        Assert.True(d.IsIdentifierPart('$'));
        Assert.True(d.IsIdentifierPart('#'));
        Assert.True(d.SupportsGroupByExpr);
        Assert.True(!d.SupportsBooleanLiterals);
        Assert.True(d.RequireIntervalQualifier);
        Assert.True(d.SupportsCommentOn);
        Assert.True(d.SupportsCreateTableSelect);
        Assert.True(d.SupportsExecuteImmediate);
        Assert.True(d.SupportsTopBeforeDistinct);
        Assert.True(d.SupportsWindowFunctionNullTreatmentArg);
        Assert.True(d.SupportsStringLiteralConcatenation);
        Assert.True(d.SupportsLeadingCommaBeforeTableOptions);
    }

    [Fact]
    public void parse_identifier()
    {
        // concat!(..)
        Teradata().VerifiedStmt(
            "SELECT " +
            "NULL AS foo, " +
            "NULL AS _bar, " +
            "NULL AS #baz, " +
            "NULL AS $qux, " +
            "NULL AS a$1, " +
            "NULL AS a#1, " +
            "NULL AS a_1, " +
            "NULL AS \"quoted id\""); // r#"NULL AS "quoted id""#
    }

    [Fact]
    public void parse_create_table_multiset()
    {
        TeradataAndGeneric().VerifiedStmt("CREATE MULTISET TABLE foo (id INT)");
        TeradataAndGeneric().VerifiedStmt("CREATE SET TABLE foo (id INT)");
    }

    [Fact]
    public void parse_create_table_volatile()
    {
        TeradataAndGeneric().VerifiedStmt("CREATE VOLATILE TABLE foo (id INT)");
        TeradataAndGeneric().VerifiedStmt("CREATE MULTISET VOLATILE TABLE foo (id INT)");
    }

    [Fact]
    public void parse_create_table_fallback()
    {
        Teradata().VerifiedStmt("CREATE TABLE foo, FALLBACK (id INT)");
        Teradata().VerifiedStmt("CREATE TABLE foo, NO FALLBACK (id INT)");
        Teradata().VerifiedStmt("CREATE MULTISET TABLE foo, NO FALLBACK (id INT)");
    }

    [Fact]
    public void parse_create_table_as_with_data()
    {
        TeradataAndGeneric().VerifiedStmt("CREATE TABLE foo AS (SELECT 1 AS a) WITH DATA");
        TeradataAndGeneric().VerifiedStmt("CREATE TABLE foo AS (SELECT 1 AS a) WITH NO DATA");
        TeradataAndGeneric()
            .VerifiedStmt("CREATE TABLE foo AS (SELECT 1 AS a) WITH DATA AND STATISTICS");
        TeradataAndGeneric()
            .VerifiedStmt("CREATE TABLE foo AS (SELECT 1 AS a) WITH DATA AND NO STATISTICS");
        TeradataAndGeneric()
            .VerifiedStmt("CREATE TABLE foo AS (SELECT 1 AS a) WITH NO DATA AND STATISTICS");
        TeradataAndGeneric()
            .VerifiedStmt("CREATE TABLE foo AS (SELECT 1 AS a) WITH NO DATA AND NO STATISTICS");
    }

    [Fact]
    public void parse_create_table_options()
    {
        // concat!(..)
        Teradata().VerifiedStmt(
            "CREATE MULTISET VOLATILE TABLE foo, NO FALLBACK " +
            "(id INT, name VARCHAR(100)) " +
            "ON COMMIT PRESERVE ROWS");
    }

    [Fact]
    public void parse_leading_comma_before_table_options()
    {
        var dialect = TestUtils.AllDialectsWhere(d => d.SupportsLeadingCommaBeforeTableOptions);
        dialect.VerifiedStmt("CREATE TABLE foo, FALLBACK (id INT)");

        var unsupportedDialects =
            TestUtils.AllDialectsWhere(d => !d.SupportsLeadingCommaBeforeTableOptions);
        Assert.Throws<ParserException>(() => unsupportedDialects
            .ParseSqlStatements("CREATE TABLE foo, FALLBACK (id INT)"));
    }
}
