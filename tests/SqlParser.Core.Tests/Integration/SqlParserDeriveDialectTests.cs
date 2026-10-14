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
// Portado para C# a partir de sqlparser-rs v0.63.0, tests/sqlparser_derive_dialect.rs.
// Modificado: tradução de Rust para C#. Cada `derive_dialect!(Novo, Base, overrides = {..})` declarado
// dentro de uma função de teste vira uma classe privada aninhada `Novo : Base` que sobrescreve os
// membros listados (convencoes.md §13, caso 8). Com ou sem `preserve_type_id`, a subclasse é sempre
// reconhecida como a base (`is Base`): o teste que depende de um `TypeId` próprio fica com
// `Skip = "DIV-002: .."` (docs/port/divergencias.md).

using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

/// <summary>Tests for the <c>derive_dialect!</c> macro.</summary>
public class SqlParserDeriveDialectTests
{
    // derive_dialect!(EnhancedGenericDialect, GenericDialect, overrides = {
    //     supports_order_by_all = true,
    //     supports_triple_quoted_string = true,
    // });
    private sealed class EnhancedGenericDialect : GenericDialect
    {
        public override bool SupportsOrderByAll => true;

        public override bool SupportsTripleQuotedString => true;
    }

    [Fact]
    public void test_method_overrides()
    {
        var dialect = new EnhancedGenericDialect();

        // Overridden methods
        Assert.True(dialect.SupportsOrderByAll);
        Assert.True(dialect.SupportsTripleQuotedString);

        // Non-overridden retains base behavior
        Assert.True(!dialect.SupportsFactorialOperator);

        // Parsing works with the overrides
        var parser = new Parser(dialect)
            .TryWithSql("SELECT '''value''' FROM t ORDER BY ALL");

        // let result = ..parse_statements(); assert!(result.is_ok()): ParseStatements lança
        // ParserException em caso de erro, e a exceção reprova o teste.
        parser.ParseStatements();
    }

    // derive_dialect!(
    //     PreservedTypeDialect,
    //     GenericDialect,
    //     preserve_type_id = true,
    //     overrides = { supports_order_by_all = true }
    // );
    private sealed class PreservedTypeDialect : GenericDialect
    {
        public override bool SupportsOrderByAll => true;
    }

    [Fact]
    public void test_preserve_type_id()
    {
        // Check the override works and the parser recognizes it as the base type
        var dialect = new PreservedTypeDialect();
        Dialect d = dialect;

        Assert.True(dialect.SupportsOrderByAll);
        Assert.True(d is GenericDialect); // d.is::<GenericDialect>()
    }

    // derive_dialect!(
    //     EnhancedMySqlDialect,
    //     MySqlDialect,
    //     overrides = { supports_order_by_all = true }
    // );
    private sealed class EnhancedMySqlDialect : MySqlDialect
    {
        public override bool SupportsOrderByAll => true;
    }

    // derive_dialect!(UniquePostgreSqlDialect, PostgreSqlDialect);
    private sealed class UniquePostgreSqlDialect : PostgreSqlDialect
    {
    }

    [Fact(Skip = "DIV-002: a subclasse de um dialeto embutido é sempre reconhecida como a base (`is PostgreSqlDialect`); não há TypeId próprio sem preserve_type_id")]
    public void test_different_base_dialects()
    {
        var pg = new UniquePostgreSqlDialect();
        var mysql = new EnhancedMySqlDialect();

        // Inherit different base behaviors
        Assert.True(pg.SupportsFilterDuringAggregation); // PostgreSQL feature
        Assert.True(mysql.SupportsStringLiteralBackslashEscape); // MySQL feature
        Assert.True(mysql.SupportsOrderByAll); // Override

        // Each has unique TypeId
        Dialect pgRef = pg;
        Dialect mysqlRef = mysql;
        Assert.True(pgRef is UniquePostgreSqlDialect);
        Assert.True(!(pgRef is PostgreSqlDialect));
        Assert.True(mysqlRef is EnhancedMySqlDialect);
    }

    // derive_dialect!(
    //     BacktickGenericDialect,
    //     GenericDialect,
    //     overrides = { identifier_quote_style = '`' }
    // );
    private sealed class BacktickGenericDialect : GenericDialect
    {
        public override char? IdentifierQuoteStyle(string identifier) => '`';
    }

    // derive_dialect!(
    //     AnotherBacktickDialect,
    //     GenericDialect,
    //     overrides = { identifier_quote_style = '[' }
    // );
    private sealed class AnotherBacktickDialect : GenericDialect
    {
        public override char? IdentifierQuoteStyle(string identifier) => '[';
    }

    // derive_dialect!(
    //     QuotelessPostgreSqlDialect,
    //     PostgreSqlDialect,
    //     preserve_type_id = true,
    //     overrides = { identifier_quote_style = None }
    // );
    private sealed class QuotelessPostgreSqlDialect : PostgreSqlDialect
    {
        public override char? IdentifierQuoteStyle(string identifier) => null;
    }

    [Fact]
    public void test_identifier_quote_style_overrides()
    {
        // Literal como `expected` (o analisador xUnit2000 é erro com TreatWarningsAsErrors).

        // Char literal (auto-wrapped in Some)
        Assert.Equal(
            '`',
            new BacktickGenericDialect().IdentifierQuoteStyle("x"));
        // Another char literal
        Assert.Equal(
            '[',
            new AnotherBacktickDialect().IdentifierQuoteStyle("x"));
        // None (overrides PostgreSQL's default '"')
        // assert_eq!(.., None) → Assert.Null (o analisador xUnit2003 proíbe Assert.Equal com null).
        Assert.Null(
            new QuotelessPostgreSqlDialect().IdentifierQuoteStyle("x"));
    }
}
