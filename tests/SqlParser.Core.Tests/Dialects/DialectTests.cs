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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/mod.rs (#[cfg(test)] mod tests).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

public class DialectTests
{
    // struct DialectHolder<'a> { dialect: &'a dyn Dialect }
    private sealed class DialectHolder
    {
        public DialectHolder(Dialect dialect) => Dialect = dialect;

        public Dialect Dialect { get; }
    }

    [Fact]
    public void test_is_dialect()
    {
        Dialect genericDialect = new GenericDialect();
        Dialect ansiDialect = new AnsiDialect();

        var genericHolder = new DialectHolder(genericDialect);
        var ansiHolder = new DialectHolder(ansiDialect);

        // dialect_of!(holder is A | B) → holder.Dialect is A or B (convencoes.md §7)
        Assert.True(genericHolder.Dialect is GenericDialect or AnsiDialect);
        Assert.True(!(genericHolder.Dialect is AnsiDialect));
        Assert.True(ansiHolder.Dialect is AnsiDialect);
        Assert.True(ansiHolder.Dialect is GenericDialect or AnsiDialect);
        Assert.True(!(ansiHolder.Dialect is GenericDialect or MsSqlDialect));
    }

    [Fact]
    public void test_dialect_from_str()
    {
        Assert.True(ParseDialect("generic") is GenericDialect);
        Assert.True(ParseDialect("mysql") is MySqlDialect);
        Assert.True(ParseDialect("MySql") is MySqlDialect);
        Assert.True(ParseDialect("postgresql") is PostgreSqlDialect);
        Assert.True(ParseDialect("postgres") is PostgreSqlDialect);
        Assert.True(ParseDialect("hive") is HiveDialect);
        Assert.True(ParseDialect("sqlite") is SQLiteDialect);
        Assert.True(ParseDialect("snowflake") is SnowflakeDialect);
        Assert.True(ParseDialect("SnowFlake") is SnowflakeDialect);
        Assert.True(ParseDialect("MsSql") is MsSqlDialect);
        Assert.True(ParseDialect("clickhouse") is ClickHouseDialect);
        Assert.True(ParseDialect("ClickHouse") is ClickHouseDialect);
        Assert.True(ParseDialect("bigquery") is BigQueryDialect);
        Assert.True(ParseDialect("BigQuery") is BigQueryDialect);
        Assert.True(ParseDialect("ansi") is AnsiDialect);
        Assert.True(ParseDialect("ANSI") is AnsiDialect);
        Assert.True(ParseDialect("duckdb") is DuckDbDialect);
        Assert.True(ParseDialect("DuckDb") is DuckDbDialect);
        Assert.True(ParseDialect("DataBricks") is DatabricksDialect);
        Assert.True(ParseDialect("databricks") is DatabricksDialect);
        Assert.True(ParseDialect("teradata") is TeradataDialect);
        Assert.True(ParseDialect("Teradata") is TeradataDialect);

        // error cases
        Assert.True(Dialect.DialectFromStr("Unknown") is null);
        Assert.True(Dialect.DialectFromStr("") is null);
    }

    // fn parse_dialect(v: &str) -> Box<dyn Dialect> { dialect_from_str(v).unwrap() }
    private static Dialect ParseDialect(string v) =>
        Dialect.DialectFromStr(v) ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");

    // #[cfg(feature = "derive-dialect")] no Rust. derive_dialect!(.., preserve_type_id = true, overrides = {..})
    // vira uma subclasse que sobrescreve as propriedades (convencoes.md §13).
    private sealed class EnhancedGenericDialect : GenericDialect
    {
        public override bool SupportsOrderByAll => true;

        public override bool SupportsNestedComments => true;

        public override bool SupportsTripleQuotedString => true;
    }

    [Fact]
    public void test_dialect_override()
    {
        var dialect = new EnhancedGenericDialect();

        Assert.True(dialect.SupportsOrderByAll);
        Assert.True(dialect.SupportsNestedComments);
        Assert.True(dialect.SupportsTripleQuotedString);

        Dialect d = dialect;
        Assert.True(d is GenericDialect);
    }

    [Fact]
    public void identifier_quote_style()
    {
        var tests = new List<(Dialect Dialect, string Ident, char? Expected)>
        {
            (new AnsiDialect(), "id", '"'),
            (new BigQueryDialect(), "id", '`'),
            (new ClickHouseDialect(), "id", '`'),
            (new DatabricksDialect(), "id", '`'),
            (new DuckDbDialect(), "id", '"'),
            (new GenericDialect(), "id", null),
            (new HiveDialect(), "id", '`'),
            (new MsSqlDialect(), "id", '['),
            (new MySqlDialect(), "id", '`'),
            (new OracleDialect(), "id", '"'),
            (new PostgreSqlDialect(), "id", '"'),
            (new RedshiftSqlDialect(), "id", '"'),
            (new SnowflakeDialect(), "id", '"'),
            (new SQLiteDialect(), "id", '`'),
            (new SparkSqlDialect(), "id", '`'),
            (new TeradataDialect(), "id", '"'),
        };

        foreach (var (dialect, ident, expected) in tests)
        {
            var actual = dialect.IdentifierQuoteStyle(ident);

            Assert.Equal(actual, expected);
        }
    }

    /// <summary>
    /// Wrapper for a dialect. In a real-world example, this wrapper
    /// would tweak the behavior of the dialect. For the test case,
    /// it wraps all methods unaltered.
    /// </summary>
    /// <remarks>
    /// Rust: <c>#[derive(Debug)] struct WrappedDialect(MySqlDialect);</c>, declarado dentro da função de
    /// teste (em C#, classe privada aninhada). O wrapper herda de <see cref="MySqlDialect"/> para que
    /// <c>is MySqlDialect</c> continue verdadeiro, o equivalente ao <c>fn dialect()</c> que devolve o
    /// <c>TypeId</c> do dialeto interno (convencoes.md §13, caso 8). Consequência: os membros que o
    /// Rust não repassa caem no padrão do trait, e aqui caem no <see cref="MySqlDialect"/> (DIV-002).
    /// </remarks>
    private sealed class WrappedDialect : MySqlDialect
    {
        /// <summary>Rust: campo <c>.0</c>.</summary>
        private readonly MySqlDialect _inner;

        public WrappedDialect(MySqlDialect inner) => _inner = inner;

        // fn dialect(&self) -> std::any::TypeId { self.0.dialect() }: sem tradução (caso 8; ver acima).

        public override bool IsIdentifierStart(char ch) => _inner.IsIdentifierStart(ch);

        public override bool IsDelimitedIdentifierStart(char ch) => _inner.IsDelimitedIdentifierStart(ch);

        public override bool IsNestedDelimitedIdentifierStart(char ch) => _inner.IsNestedDelimitedIdentifierStart(ch);

        public override (char QuoteStart, char? NestedQuoteStart)? PeekNestedDelimitedIdentifierQuotes(PeekableChars chars) =>
            _inner.PeekNestedDelimitedIdentifierQuotes(chars);

        public override char? IdentifierQuoteStyle(string identifier) => _inner.IdentifierQuoteStyle(identifier);

        public override bool SupportsStringLiteralBackslashEscape => _inner.SupportsStringLiteralBackslashEscape;

        public override bool SupportsFilterDuringAggregation => _inner.SupportsFilterDuringAggregation;

        public override bool SupportsWithinAfterArrayAggregation => _inner.SupportsWithinAfterArrayAggregation;

        public override bool SupportsGroupByExpr => _inner.SupportsGroupByExpr;

        public override bool SupportsInEmptyList => _inner.SupportsInEmptyList;

        public override bool SupportsInUnparenthesizedExpr => _inner.SupportsInUnparenthesizedExpr;

        public override bool ConvertTypeBeforeValue => _inner.ConvertTypeBeforeValue;

        public override Ast.Expr? ParsePrefix(Parser parser) => _inner.ParsePrefix(parser);

        public override Ast.Expr? ParseInfix(Parser parser, Ast.Expr expr, byte precedence) =>
            _inner.ParseInfix(parser, expr, precedence);

        public override byte? GetNextPrecedence(Parser parser) => _inner.GetNextPrecedence(parser);

        public override Ast.Statement? ParseStatement(Parser parser) => _inner.ParseStatement(parser);

        public override bool IsIdentifierPart(char ch) => _inner.IsIdentifierPart(ch);
    }

    [Fact]
    public void parse_with_wrapped_dialect()
    {
        // #[allow(clippy::needless_raw_string_hashes)]: não traduzido.
        // r#"SELECT 'Wayne\'s World'"# → string verbatim (a barra invertida é literal).
        var statement = @"SELECT 'Wayne\'s World'";
        // Parser::parse_sql devolve Result; em C#, ParseSql lança em caso de erro, e
        // `assert!(res1.is_ok())` equivale a a chamada não lançar.
        var res1 = Parser.ParseSql(new MySqlDialect(), statement);
        var res2 = Parser.ParseSql(new WrappedDialect(new MySqlDialect()), statement);
        Assert.Equal(res1, res2);
    }
}
