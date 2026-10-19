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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/no_std_recursion.rs.
// Modificado: tradução de Rust para C#. No Rust o arquivo só compila sem a feature `std`
// (`#![cfg(not(feature = "std"))]`); o Core não tem features, e o limite de recursão vale sempre
// (DIV-005), então os testes rodam incondicionalmente.

using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

public class NoStdRecursionTests
{
    // Rust: `s.repeat(n)`.
    private static string Repeat(string s, int n) => string.Concat(Enumerable.Repeat(s, n));

    [Fact]
    public void with_recursion_limit_applies_without_default_features()
    {
        var dialect = new GenericDialect();
        var parser = new Parser(dialect)
            .WithRecursionLimit(1)
            .TryWithSql("SELECT * FROM foo WHERE (a OR (b OR (c OR d)))");
        var ex = Assert.Throws<ParserException>(() => parser.ParseStatements());

        Assert.Equal(ex.Error, new ParserError.RecursionLimitExceeded());
    }

    [Fact]
    public void default_recursion_limit_applies_without_default_features()
    {
        var dialect = new GenericDialect();
        var sql = $"SELECT * FROM t WHERE {Repeat("(", 200)}a = 1{Repeat(")", 200)}";

        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(dialect, sql));

        Assert.Equal(ex.Error, new ParserError.RecursionLimitExceeded());
    }

    [Fact]
    public void deeply_nested_not_returns_error_without_default_features()
    {
        var dialect = new GenericDialect();
        var sql = $"SELECT * FROM t WHERE {Repeat("NOT ", 1024)}a";

        // Note: Error is generic "expected end of statement" error rather than
        // `RecursionLimitExceeded`. Either way, the important property is that deep
        // nesting errors instead of overflowing the stack.
        Assert.Throws<ParserException>(() => Parser.ParseSql(dialect, sql)); // assert!(result.is_err())
    }

    [Fact]
    public void valid_nested_queries_parse_without_default_features()
    {
        var dialect = new GenericDialect();

        // let result = Parser::parse_sql(..); assert!(result.is_ok()): ParseSql lança
        // ParserException em caso de erro, e a exceção reprova o teste.
        Parser.ParseSql(dialect, "SELECT 1 + (2 + 3)");
    }

    [Fact]
    public void recursion_budget_restores_between_statements_without_default_features()
    {
        var dialect = new GenericDialect();
        var statements = new Parser(dialect)
            .WithRecursionLimit(4)
            .TryWithSql("SELECT 1; SELECT 2; SELECT 3")
            .ParseStatements();

        Assert.Equal(3, statements.Count); // literal como `expected` (xUnit2000)
    }

    [Fact]
    public void deeply_nested_intervals_hit_recursion_limit_without_default_features()
    {
        var dialect = new GenericDialect();
        var sql = $"SELECT {Repeat("INTERVAL ", 1000)}1";

        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(dialect, sql));

        Assert.Equal(ex.Error, new ParserError.RecursionLimitExceeded());
    }

    [Fact]
    public void nested_queries_hit_recursion_limit_without_default_features()
    {
        var dialect = new GenericDialect();
        var sql = $"{Repeat("SELECT 1 WHERE 1 IN (", 100)}SELECT 1{Repeat(")", 100)}";

        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(dialect, sql));

        Assert.Equal(ex.Error, new ParserError.RecursionLimitExceeded());
    }

    [Fact]
    public void nested_table_factors_hit_recursion_limit_without_default_features()
    {
        var dialect = new GenericDialect();
        var sql = $"SELECT * FROM {Repeat("(", 100)}t{Repeat(")", 100)}";

        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(dialect, sql));

        Assert.Equal(ex.Error, new ParserError.RecursionLimitExceeded());
    }
}
