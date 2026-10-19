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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (#[cfg(test)] mod tests).
// Modificado: tradução de Rust para C#. O submódulo `mod test_parse_data_type` foi achatado nesta
// classe, na mesma posição; as macros locais (`test_parse_data_type!`, `test_parse_schema_name!`...)
// viraram métodos `private static` (docs/port/onda-c/C17.md, Decisões).

using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

public class ParserTests
{
    /// <summary>
    /// Rust: <c>run_parser_method(sql, |parser| { .. })</c> com closure que devolve <c>()</c>. O
    /// <c>()</c> vira <see cref="ValueTuple"/>, que também é igual entre dialetos.
    /// </summary>
    private static void RunParserMethod(TestedDialects dialects, string sql, Action<Parser> f) =>
        dialects.RunParserMethod(sql, parser =>
        {
            f(parser);
            return ValueTuple.Create();
        });

    [Fact]
    public void test_prev_index()
    {
        var sql = "SELECT version";
        // assert_eq!(parser.peek_token(), Token::..): `impl PartialEq<Token> for TokenWithSpan`
        // compara só o `token`; aqui, `.Token` (convencoes.md §13, caso 21).
        RunParserMethod(TestUtils.AllDialects(), sql, parser =>
        {
            Assert.Equal(parser.PeekToken().Token, Token.MakeKeyword("SELECT"));
            Assert.Equal(parser.NextToken().Token, Token.MakeKeyword("SELECT"));
            parser.PrevToken();
            Assert.Equal(parser.NextToken().Token, Token.MakeKeyword("SELECT"));
            Assert.Equal(parser.NextToken().Token, Token.MakeWord("version", null));
            parser.PrevToken();
            Assert.Equal(parser.PeekToken().Token, Token.MakeWord("version", null));
            Assert.Equal(parser.NextToken().Token, Token.MakeWord("version", null));
            Assert.Equal(parser.PeekToken().Token, new Token.EOF());
            parser.PrevToken();
            Assert.Equal(parser.NextToken().Token, Token.MakeWord("version", null));
            Assert.Equal(parser.NextToken().Token, new Token.EOF());
            Assert.Equal(parser.NextToken().Token, new Token.EOF());
            parser.PrevToken();
        });
    }

    [Fact]
    public void test_peek_tokens()
    {
        // peek_tokens::<N>() com N inferido pelo padrão → PeekTokens(N) (convencoes.md §13, caso 28).
        RunParserMethod(TestUtils.AllDialects(), "SELECT foo AS bar FROM baz", parser =>
        {
            Assert.True(parser.PeekTokens(1) is
                [Token.Word { Value.Keyword: Keyword.SELECT }]);

            Assert.True(parser.PeekTokens(3) is
                [
                    Token.Word { Value.Keyword: Keyword.SELECT },
                    Token.Word,
                    Token.Word { Value.Keyword: Keyword.AS },
                ]);

            for (var i = 0; i < 4; i++)
            {
                parser.NextToken();
            }

            Assert.True(parser.PeekTokens(4) is
                [
                    Token.Word { Value.Keyword: Keyword.FROM },
                    Token.Word,
                    Token.EOF,
                    Token.EOF,
                ]);
        });
    }

    // #[cfg(test)] mod test_parse_data_type { .. } (achatado aqui).

    // macro_rules! test_parse_data_type
    private static void TestParseDataType(TestedDialects dialect, string input, DataType expectedType) =>
        RunParserMethod(dialect, input, parser =>
        {
            var dataType = parser.ParseDataType();
            Assert.Equal(expectedType, dataType);
            Assert.Equal(input, dataType.ToString());
        });

    [Fact]
    public void test_ansii_character_string_types()
    {
        // Character string types: <https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-string-type>
        var dialect =
            new TestedDialects(new List<Dialect> { new GenericDialect(), new AnsiDialect() });

        TestParseDataType(dialect, "CHARACTER", new DataType.Character(null));

        TestParseDataType(
            dialect,
            "CHARACTER(20)",
            new DataType.Character(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: null)));

        TestParseDataType(
            dialect,
            "CHARACTER(20 CHARACTERS)",
            new DataType.Character(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: new CharLengthUnits.Characters())));

        TestParseDataType(
            dialect,
            "CHARACTER(20 OCTETS)",
            new DataType.Character(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: new CharLengthUnits.Octets())));

        TestParseDataType(dialect, "CHAR", new DataType.Char(null));

        TestParseDataType(
            dialect,
            "CHAR(20)",
            new DataType.Char(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: null)));

        TestParseDataType(
            dialect,
            "CHAR(20 CHARACTERS)",
            new DataType.Char(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: new CharLengthUnits.Characters())));

        TestParseDataType(
            dialect,
            "CHAR(20 OCTETS)",
            new DataType.Char(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: new CharLengthUnits.Octets())));

        TestParseDataType(
            dialect,
            "CHARACTER VARYING(20)",
            new DataType.CharacterVarying(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: null)));

        TestParseDataType(
            dialect,
            "CHARACTER VARYING(20 CHARACTERS)",
            new DataType.CharacterVarying(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: new CharLengthUnits.Characters())));

        TestParseDataType(
            dialect,
            "CHARACTER VARYING(20 OCTETS)",
            new DataType.CharacterVarying(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: new CharLengthUnits.Octets())));

        TestParseDataType(
            dialect,
            "CHAR VARYING(20)",
            new DataType.CharVarying(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: null)));

        TestParseDataType(
            dialect,
            "CHAR VARYING(20 CHARACTERS)",
            new DataType.CharVarying(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: new CharLengthUnits.Characters())));

        TestParseDataType(
            dialect,
            "CHAR VARYING(20 OCTETS)",
            new DataType.CharVarying(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: new CharLengthUnits.Octets())));

        TestParseDataType(
            dialect,
            "VARCHAR(20)",
            new DataType.Varchar(new CharacterLength.IntegerLength(
                Length: 20,
                Unit: null)));
    }

    [Fact]
    public void test_ansii_character_large_object_types()
    {
        // Character large object types: <https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-large-object-length>
        var dialect =
            new TestedDialects(new List<Dialect> { new GenericDialect(), new AnsiDialect() });

        TestParseDataType(
            dialect,
            "CHARACTER LARGE OBJECT",
            new DataType.CharacterLargeObject(null));
        TestParseDataType(
            dialect,
            "CHARACTER LARGE OBJECT(20)",
            new DataType.CharacterLargeObject(20));

        TestParseDataType(
            dialect,
            "CHAR LARGE OBJECT",
            new DataType.CharLargeObject(null));
        TestParseDataType(
            dialect,
            "CHAR LARGE OBJECT(20)",
            new DataType.CharLargeObject(20));

        TestParseDataType(dialect, "CLOB", new DataType.Clob(null));
        TestParseDataType(dialect, "CLOB(20)", new DataType.Clob(20));
    }

    [Fact]
    public void test_parse_custom_types()
    {
        var dialect =
            new TestedDialects(new List<Dialect> { new GenericDialect(), new AnsiDialect() });

        TestParseDataType(
            dialect,
            "GEOMETRY",
            new DataType.Custom(ObjectName.From(Sequence.Of(Ident.From("GEOMETRY"))), Sequence<string>.Empty));

        TestParseDataType(
            dialect,
            "GEOMETRY(POINT)",
            new DataType.Custom(
                ObjectName.From(Sequence.Of(Ident.From("GEOMETRY"))),
                Sequence.Of("POINT")));

        TestParseDataType(
            dialect,
            "GEOMETRY(POINT, 4326)",
            new DataType.Custom(
                ObjectName.From(Sequence.Of(Ident.From("GEOMETRY"))),
                Sequence.Of("POINT", "4326")));
    }

    [Fact]
    public void test_ansii_exact_numeric_types()
    {
        // Exact numeric types: <https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#exact-numeric-type>
        var dialect = new TestedDialects(new List<Dialect>
        {
            new GenericDialect(),
            new AnsiDialect(),
            new PostgreSqlDialect(),
        });

        TestParseDataType(dialect, "NUMERIC", new DataType.Numeric(new ExactNumberInfo.None()));

        TestParseDataType(
            dialect,
            "NUMERIC(2)",
            new DataType.Numeric(new ExactNumberInfo.Precision(2)));

        TestParseDataType(
            dialect,
            "NUMERIC(2,10)",
            new DataType.Numeric(new ExactNumberInfo.PrecisionAndScale(2, 10)));

        TestParseDataType(dialect, "DECIMAL", new DataType.Decimal(new ExactNumberInfo.None()));

        TestParseDataType(
            dialect,
            "DECIMAL(2)",
            new DataType.Decimal(new ExactNumberInfo.Precision(2)));

        TestParseDataType(
            dialect,
            "DECIMAL(2,10)",
            new DataType.Decimal(new ExactNumberInfo.PrecisionAndScale(2, 10)));

        TestParseDataType(dialect, "DEC", new DataType.Dec(new ExactNumberInfo.None()));

        TestParseDataType(
            dialect,
            "DEC(2)",
            new DataType.Dec(new ExactNumberInfo.Precision(2)));

        TestParseDataType(
            dialect,
            "DEC(2,10)",
            new DataType.Dec(new ExactNumberInfo.PrecisionAndScale(2, 10)));

        // Test negative scale values.
        TestParseDataType(
            dialect,
            "NUMERIC(10,-2)",
            new DataType.Numeric(new ExactNumberInfo.PrecisionAndScale(10, -2)));

        TestParseDataType(
            dialect,
            "DECIMAL(1000,-10)",
            new DataType.Decimal(new ExactNumberInfo.PrecisionAndScale(1000, -10)));

        TestParseDataType(
            dialect,
            "DEC(5,-1000)",
            new DataType.Dec(new ExactNumberInfo.PrecisionAndScale(5, -1000)));

        TestParseDataType(
            dialect,
            "NUMERIC(10,-5)",
            new DataType.Numeric(new ExactNumberInfo.PrecisionAndScale(10, -5)));

        TestParseDataType(
            dialect,
            "DECIMAL(20,-10)",
            new DataType.Decimal(new ExactNumberInfo.PrecisionAndScale(20, -10)));

        TestParseDataType(
            dialect,
            "DEC(5,-2)",
            new DataType.Dec(new ExactNumberInfo.PrecisionAndScale(5, -2)));

        RunParserMethod(dialect, "NUMERIC(10,+5)", parser =>
        {
            var dataType = parser.ParseDataType();
            Assert.Equal(
                new DataType.Numeric(new ExactNumberInfo.PrecisionAndScale(10, 5)),
                dataType);
            // Note: Explicit '+' sign is not preserved in output, which is correct
            Assert.Equal("NUMERIC(10,5)", dataType.ToString());
        });
    }

    [Fact]
    public void test_ansii_date_type()
    {
        // Datetime types: <https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#datetime-type>
        var dialect =
            new TestedDialects(new List<Dialect> { new GenericDialect(), new AnsiDialect() });

        TestParseDataType(dialect, "DATE", new DataType.Date());

        TestParseDataType(dialect, "TIME", new DataType.Time(null, new TimezoneInfo.None()));

        TestParseDataType(
            dialect,
            "TIME(6)",
            new DataType.Time(6, new TimezoneInfo.None()));

        TestParseDataType(
            dialect,
            "TIME WITH TIME ZONE",
            new DataType.Time(null, new TimezoneInfo.WithTimeZone()));

        TestParseDataType(
            dialect,
            "TIME(6) WITH TIME ZONE",
            new DataType.Time(6, new TimezoneInfo.WithTimeZone()));

        TestParseDataType(
            dialect,
            "TIME WITHOUT TIME ZONE",
            new DataType.Time(null, new TimezoneInfo.WithoutTimeZone()));

        TestParseDataType(
            dialect,
            "TIME(6) WITHOUT TIME ZONE",
            new DataType.Time(6, new TimezoneInfo.WithoutTimeZone()));

        TestParseDataType(
            dialect,
            "TIMESTAMP",
            new DataType.Timestamp(null, new TimezoneInfo.None()));

        TestParseDataType(
            dialect,
            "TIMESTAMP(22)",
            new DataType.Timestamp(22, new TimezoneInfo.None()));

        TestParseDataType(
            dialect,
            "TIMESTAMP(22) WITH TIME ZONE",
            new DataType.Timestamp(22, new TimezoneInfo.WithTimeZone()));

        TestParseDataType(
            dialect,
            "TIMESTAMP(33) WITHOUT TIME ZONE",
            new DataType.Timestamp(33, new TimezoneInfo.WithoutTimeZone()));
    }

    // Fim de mod test_parse_data_type.

    // macro_rules! test_parse_schema_name (declarada dentro de test_parse_schema_name).
    // The expected name should be identical as the input name, that's why I don't receive both
    private static void TestParseSchemaName(string input, SchemaName expectedName) =>
        RunParserMethod(TestUtils.AllDialects(), input, parser =>
        {
            var schemaName = parser.ParseSchemaName();
            // Validate that the structure is the same as expected
            Assert.Equal(schemaName, expectedName);
            // Validate that the input and the expected structure serialization are the same
            Assert.Equal(schemaName.ToString(), input);
        });

    [Fact]
    public void test_parse_schema_name()
    {
        var dummyName = ObjectName.From(Sequence.Of(new Ident("dummy_name")));
        var dummyAuthorization = new Ident("dummy_authorization");

        TestParseSchemaName(
            $"{dummyName}",
            new SchemaName.Simple(dummyName));

        TestParseSchemaName(
            $"AUTHORIZATION {dummyAuthorization}",
            new SchemaName.UnnamedAuthorization(dummyAuthorization));
        TestParseSchemaName(
            $"{dummyName} AUTHORIZATION {dummyAuthorization}",
            new SchemaName.NamedAuthorization(dummyName, dummyAuthorization));
    }

    // macro_rules! test_parse_table_constraint (declarada dentro de mysql_parse_index_table_constraint).
    private static void TestParseTableConstraint(TestedDialects dialect, string input, TableConstraint expected) =>
        RunParserMethod(dialect, input, parser =>
        {
            // parse_optional_table_constraint().unwrap().unwrap()
            var constraint = parser.ParseOptionalTableConstraint()
                ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");
            // Validate that the structure is the same as expected
            Assert.Equal(constraint, expected);
            // Validate that the input and the expected structure serialization are the same
            Assert.Equal(constraint.ToString(), input);
        });

    // fn mk_expected_col (declarada dentro de mysql_parse_index_table_constraint).
    private static IndexColumn MkExpectedCol(string name) =>
        new IndexColumn(
            Column: new OrderByExpr(
                Expr: new Expr.Identifier(Ident.From(name)),
                Options: new OrderByOptions(
                    Sort: null,
                    NullsFirst: null),
                WithFill: null),
            OperatorClass: null);

    [Fact]
    public void mysql_parse_index_table_constraint()
    {
        var dialect =
            new TestedDialects(new List<Dialect> { new GenericDialect(), new MySqlDialect() });

        TestParseTableConstraint(
            dialect,
            "INDEX (c1)",
            TableConstraint.From(new IndexConstraint(
                DisplayAsKey: false,
                Name: null,
                IndexType: null,
                Columns: Sequence.Of(MkExpectedCol("c1")),
                IndexOptions: Sequence<IndexOption>.Empty)));

        TestParseTableConstraint(
            dialect,
            "KEY (c1)",
            TableConstraint.From(new IndexConstraint(
                DisplayAsKey: true,
                Name: null,
                IndexType: null,
                Columns: Sequence.Of(MkExpectedCol("c1")),
                IndexOptions: Sequence<IndexOption>.Empty)));

        TestParseTableConstraint(
            dialect,
            "INDEX 'index' (c1, c2)",
            new TableConstraint.Index(new IndexConstraint(
                DisplayAsKey: false,
                Name: Ident.WithQuote('\'', "index"),
                IndexType: null,
                Columns: Sequence.Of(MkExpectedCol("c1"), MkExpectedCol("c2")),
                IndexOptions: Sequence<IndexOption>.Empty)));

        TestParseTableConstraint(
            dialect,
            "INDEX USING BTREE (c1)",
            TableConstraint.From(new IndexConstraint(
                DisplayAsKey: false,
                Name: null,
                IndexType: new IndexType.BTree(),
                Columns: Sequence.Of(MkExpectedCol("c1")),
                IndexOptions: Sequence<IndexOption>.Empty)));

        TestParseTableConstraint(
            dialect,
            "INDEX USING HASH (c1)",
            TableConstraint.From(new IndexConstraint(
                DisplayAsKey: false,
                Name: null,
                IndexType: new IndexType.Hash(),
                Columns: Sequence.Of(MkExpectedCol("c1")),
                IndexOptions: Sequence<IndexOption>.Empty)));

        TestParseTableConstraint(
            dialect,
            "INDEX idx_name USING BTREE (c1)",
            TableConstraint.From(new IndexConstraint(
                DisplayAsKey: false,
                Name: new Ident("idx_name"),
                IndexType: new IndexType.BTree(),
                Columns: Sequence.Of(MkExpectedCol("c1")),
                IndexOptions: Sequence<IndexOption>.Empty)));

        TestParseTableConstraint(
            dialect,
            "INDEX idx_name USING HASH (c1)",
            TableConstraint.From(new IndexConstraint(
                DisplayAsKey: false,
                Name: new Ident("idx_name"),
                IndexType: new IndexType.Hash(),
                Columns: Sequence.Of(MkExpectedCol("c1")),
                IndexOptions: Sequence<IndexOption>.Empty)));
    }

    [Fact]
    public void test_tokenizer_error_loc()
    {
        var sql = "foo '";
        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), sql));
        Assert.Equal(
            ex.Error,
            new ParserError.TokenizerError(
                "Unterminated string literal at Line: 1, Column: 5"));
    }

    [Fact]
    public void test_parser_error_loc()
    {
        var sql = "SELECT this is a syntax error";
        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), sql));
        // let ParserError::ParserError(msg) = .. else { panic!("expected ParserError::ParserError") };
        var msg = Assert.IsType<ParserError.ParserErrorValue>(ex.Error).Value;
        // assert!(msg.ends_with(..), "unexpected error message: {msg}"): Assert.EndsWith mostra `msg` na falha.
        Assert.EndsWith("found: a at Line: 1, Column: 16", msg, StringComparison.Ordinal);
    }

    [Fact]
    public void test_nested_explain_error()
    {
        var sql = "EXPLAIN EXPLAIN SELECT 1";
        var ex = Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), sql));
        Assert.Equal(
            ex.Error,
            new ParserError.ParserErrorValue(
                "Explain must be root of the plan"));
    }

    [Fact]
    public void test_parse_multipart_identifier_positive()
    {
        var dialect = new TestedDialects(new List<Dialect> { new GenericDialect() });

        // parse multipart with quotes
        var expected = Sequence.Of(
            new Ident(
                Value: "CATALOG",
                QuoteStyle: null,
                Span: Span.Empty()),
            new Ident(
                Value: "F(o)o. \"bar",
                QuoteStyle: '"',
                Span: Span.Empty()),
            new Ident(
                Value: "table",
                QuoteStyle: null,
                Span: Span.Empty()));
        RunParserMethod(dialect, """CATALOG."F(o)o. ""bar".table""", parser =>
        {
            var actual = parser.ParseMultipartIdentifier();
            Assert.Equal(expected, actual);
        });

        // allow whitespace between ident parts
        var expected2 = Sequence.Of(
            new Ident(
                Value: "CATALOG",
                QuoteStyle: null,
                Span: Span.Empty()),
            new Ident(
                Value: "table",
                QuoteStyle: null,
                Span: Span.Empty()));
        RunParserMethod(dialect, "CATALOG . table", parser =>
        {
            var actual = parser.ParseMultipartIdentifier();
            Assert.Equal(expected2, actual);
        });
    }

    // macro_rules! test_parse_multipart_identifier_error (declarada dentro de test_parse_multipart_identifier_negative).
    private static void TestParseMultipartIdentifierError(string input, string expectedErr) =>
        RunParserMethod(TestUtils.AllDialects(), input, parser =>
        {
            var actualErr = Assert.Throws<ParserException>(() => parser.ParseMultipartIdentifier()).Error;
            Assert.Equal(actualErr.ToString(), expectedErr);
        });

    [Fact]
    public void test_parse_multipart_identifier_negative()
    {
        TestParseMultipartIdentifierError(
            "",
            "sql parser error: Empty input when parsing identifier");

        TestParseMultipartIdentifierError(
            "*schema.table",
            "sql parser error: Unexpected token in identifier: *");

        TestParseMultipartIdentifierError(
            "schema.table*",
            "sql parser error: Unexpected token in identifier: *");

        TestParseMultipartIdentifierError(
            "schema.table.",
            "sql parser error: Trailing period in identifier");

        TestParseMultipartIdentifierError(
            "schema.*",
            "sql parser error: Unexpected token following period in identifier: *");
    }

    [Fact]
    public void test_mysql_partition_selection()
    {
        var sql = "SELECT * FROM employees PARTITION (p0, p2)";
        var expected = new List<string> { "p0", "p2" };

        var ast = Parser.ParseSql(new MySqlDialect(), sql);
        Assert.Single(ast); // assert_eq!(ast.len(), 1)
        if (ast[0] is Statement.Query { Value: var v })
        {
            if (v.Body is SetExpr.Select { Value: var select })
            {
                Assert.Single(select.From); // assert_eq!(select.from.len(), 1)
                TableWithJoins from = select.From[0];
                var tableFactor = from.Relation;
                if (tableFactor is TableFactor.Table { Partitions: var partitions })
                {
                    var actual = partitions
                        .Select(ident => ident.Value)
                        .ToList();
                    Assert.Equal(expected, actual);
                }
            }
        }
        else
        {
            throw new InvalidOperationException("fail to parse mysql partition selection");
        }
    }

    [Fact]
    public void test_replace_into_placeholders()
    {
        var sql = "REPLACE INTO t (a) VALUES (&a)";

        Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), sql));
    }

    [Fact]
    public void test_replace_into_set_placeholder()
    {
        var sql = "REPLACE INTO t SET ?";

        Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), sql));
    }

    [Fact]
    public void test_replace_incomplete()
    {
        var sql = "REPLACE";

        Assert.Throws<ParserException>(() => Parser.ParseSql(new MySqlDialect(), sql));
    }

    [Fact]
    public void test_placeholder_invalid_whitespace()
    {
        foreach (var w in new[] { "  ", "/*invalid*/" })
        {
            var sql = $"\nSELECT\n  :{w}fooBar";
            Assert.Throws<ParserException>(() => Parser.ParseSql(new GenericDialect(), sql));
        }
    }
}
