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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 3525–4465:
// parse_struct_field_expr … parse_pg_cast).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>
    /// Parse an expression value for a struct literal
    /// Syntax
    /// <code>
    /// expr [AS name]
    /// </code>
    /// </summary>
    /// <remarks>
    /// For biquery [1], Parameter typed_syntax is set to true if the expression
    /// is to be parsed as a field expression declared using typed
    /// struct syntax [2], and false if using typeless struct syntax [3].
    /// <para>[1]: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#constructing_a_struct"/></para>
    /// <para>[2]: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#typed_struct_syntax"/></para>
    /// <para>[3]: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#typeless_struct_syntax"/></para>
    /// </remarks>
    internal Expr ParseStructFieldExpr(bool typedSyntax)
    {
        var expr = ParseExpr();
        if (ParseKeyword(Keyword.AS))
        {
            if (typedSyntax)
            {
                PrevToken();
                throw ParserErr("Typed syntax does not allow AS", PeekTokenRef().Span.Start);
            }
            var fieldName = ParseIdentifier();
            return new Expr.Named(
                Expr: expr,
                Name: fieldName);
        }
        else
        {
            return expr;
        }
    }

    /// <summary>
    /// Parse a Struct type definition as a sequence of field-value pairs.
    /// The syntax of the Struct elem differs by dialect so it is customised
    /// by the <c>elem_parser</c> argument.
    /// </summary>
    /// <remarks>
    /// Syntax
    /// <code>
    /// Hive:
    /// STRUCT&lt;field_name: field_type&gt;
    ///
    /// BigQuery:
    /// STRUCT&lt;[field_name] field_type&gt;
    /// </code>
    /// </remarks>
    internal (Sequence<StructField> Fields, MatchedTrailingBracket TrailingBracket) ParseStructTypeDef(
        Func<Parser, (StructField Field, MatchedTrailingBracket TrailingBracket)> elemParser)
    {
        ExpectKeywordIs(Keyword.STRUCT);

        // Nothing to do if we have no type information.
        if (PeekTokenRef().Token is not Token.Lt)
        {
            return (Sequence<StructField>.Empty, MatchedTrailingBracket.From(false));
        }
        NextToken();

        var fieldDefs = new List<StructField>();
        MatchedTrailingBracket trailingBracket;
        while (true)
        {
            var (def, elemTrailingBracket) = elemParser(this);
            fieldDefs.Add(def);
            // The struct field definition is finished if it occurs `>>` or comma.
            if (elemTrailingBracket.Value || !ConsumeToken(new Token.Comma()))
            {
                trailingBracket = elemTrailingBracket;
                break;
            }
        }

        return (
            fieldDefs.ToSequence(),
            ExpectClosingAngleBracket(trailingBracket));
    }

    /// <summary>Duckdb Struct Data Type <see href="https://duckdb.org/docs/sql/data_types/struct.html#retrieving-from-structs"/></summary>
    internal Sequence<StructField> ParseDuckdbStructTypeDef()
    {
        ExpectKeywordIs(Keyword.STRUCT);
        ExpectToken(new Token.LParen());
        // `let struct_body = self.parse_comma_separated(..);` sem `?`: o erro só é devolvido depois do
        // `expect_token(&Token::RParen)?`, que tem precedência se também falhar.
        Sequence<StructField> structBody = default;
        ParserException? structBodyError = null;
        try
        {
            structBody = ParseCommaSeparated(parser =>
            {
                var fieldName = parser.ParseIdentifier();
                var fieldType = parser.ParseDataType();

                return new StructField(
                    FieldName: fieldName,
                    FieldType: fieldType,
                    Options: null);
            });
        }
        catch (ParserException e)
        {
            structBodyError = e;
        }
        ExpectToken(new Token.RParen());
        if (structBodyError is not null)
        {
            throw StackGuard.Rethrow(structBodyError);
        }
        return structBody;
    }

    /// <summary>
    /// Parse a field definition in a [struct] or [tuple].
    /// Syntax:
    /// <code>
    /// [field_name] field_type
    /// field_name: field_type
    /// </code>
    /// </summary>
    /// <remarks>
    /// [struct]: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-types#declaring_a_struct_type"/>
    /// [tuple]: <see href="https://clickhouse.com/docs/en/sql-reference/data-types/tuple"/>
    /// [databricks]: <see href="https://docs.databricks.com/en/sql/language-manual/data-types/struct-type.html"/>
    /// </remarks>
    internal (StructField Field, MatchedTrailingBracket TrailingBracket) ParseStructFieldDef()
    {
        // Look beyond the next item to infer whether both field name
        // and type are specified.
        var isNamedField =
            (PeekNthToken(0).Token, PeekNthToken(1).Token)
                is (Token.Word, Token.Word) or (Token.Word, Token.Colon);

        Ident? fieldName;
        if (isNamedField)
        {
            var name = ParseIdentifier();
            _ = ConsumeToken(new Token.Colon());
            fieldName = name;
        }
        else
        {
            fieldName = null;
        }

        var (fieldType, trailingBracket) = ParseDataTypeHelper();

        var options = MaybeParseOptions(Keyword.OPTIONS);
        return (
            new StructField(
                FieldName: fieldName,
                FieldType: fieldType,
                Options: options),
            trailingBracket);
    }

    /// <summary>DuckDB specific: Parse a Union type definition as a sequence of field-value pairs.</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// UNION(field_name field_type[,...])
    /// </code>
    /// [1]: <see href="https://duckdb.org/docs/sql/data_types/union.html"/>
    /// </remarks>
    internal Sequence<UnionField> ParseUnionTypeDef()
    {
        ExpectKeywordIs(Keyword.UNION);

        ExpectToken(new Token.LParen());

        var fields = ParseCommaSeparated(p => new UnionField(
            FieldName: p.ParseIdentifier(),
            FieldType: p.ParseDataType()));

        ExpectToken(new Token.RParen());

        return fields;
    }

    /// <summary>DuckDB and ClickHouse specific: Parse a duckdb [dictionary] or a clickhouse [map] setting</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// {'field_name': expr1[, ... ]}
    /// </code>
    /// [dictionary]: <see href="https://duckdb.org/docs/sql/data_types/struct#creating-structs"/>
    /// [map]: <see href="https://clickhouse.com/docs/operations/settings/settings#additional_table_filters"/>
    /// </remarks>
    internal Expr ParseDictionary()
    {
        ExpectToken(new Token.LBrace());

        var fields = ParseCommaSeparated0(p => p.ParseDictionaryField(), new Token.RBrace());

        ExpectToken(new Token.RBrace());

        return new Expr.Dictionary(fields);
    }

    /// <summary>Parse a field for a duckdb [dictionary] or a clickhouse [map] setting</summary>
    /// <remarks>
    /// Syntax
    /// <code>
    /// 'name': expr
    /// </code>
    /// [dictionary]: <see href="https://duckdb.org/docs/sql/data_types/struct#creating-structs"/>
    /// [map]: <see href="https://clickhouse.com/docs/operations/settings/settings#additional_table_filters"/>
    /// </remarks>
    internal DictionaryField ParseDictionaryField()
    {
        var key = ParseIdentifier();

        ExpectToken(new Token.Colon());

        var expr = ParseExpr();

        return new DictionaryField(
            Key: key,
            Value: expr);
    }

    /// <summary>DuckDB specific: Parse a duckdb [map]</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// Map {key1: value1[, ... ]}
    /// </code>
    /// [map]: <see href="https://duckdb.org/docs/sql/data_types/map.html#creating-maps"/>
    /// </remarks>
    internal Expr ParseDuckdbMapLiteral()
    {
        ExpectToken(new Token.LBrace());
        var fields = ParseCommaSeparated0(p => p.ParseDuckdbMapField(), new Token.RBrace());
        ExpectToken(new Token.RBrace());
        return new Expr.Map(new Map(Entries: fields));
    }

    /// <summary>Parse a field for a duckdb [map]</summary>
    /// <remarks>
    /// Syntax
    /// <code>
    /// key: value
    /// </code>
    /// [map]: <see href="https://duckdb.org/docs/sql/data_types/map.html#creating-maps"/>
    /// </remarks>
    internal MapEntry ParseDuckdbMapField()
    {
        // Stop before `:` so it can act as a key/value separator
        var key = ParseSubexpr(_dialect.PrecValue(Precedence.Colon));

        ExpectToken(new Token.Colon());

        var value = ParseExpr();

        return new MapEntry(
            Key: key,
            Value: value);
    }

    /// <summary>Parse clickhouse [map]</summary>
    /// <remarks>
    /// Syntax
    /// <code>
    /// Map(key_data_type, value_data_type)
    /// </code>
    /// [map]: <see href="https://clickhouse.com/docs/en/sql-reference/data-types/map"/>
    /// </remarks>
    internal (DataType KeyDataType, DataType ValueDataType) ParseClickHouseMapDef()
    {
        ExpectKeywordIs(Keyword.MAP);
        ExpectToken(new Token.LParen());
        var keyDataType = ParseDataType();
        ExpectToken(new Token.Comma());
        var valueDataType = ParseDataType();
        ExpectToken(new Token.RParen());

        return (keyDataType, valueDataType);
    }

    /// <summary>Parse clickhouse [tuple]</summary>
    /// <remarks>
    /// Syntax
    /// <code>
    /// Tuple([field_name] field_type, ...)
    /// </code>
    /// [tuple]: <see href="https://clickhouse.com/docs/en/sql-reference/data-types/tuple"/>
    /// </remarks>
    internal Sequence<StructField> ParseClickHouseTupleDef()
    {
        ExpectKeywordIs(Keyword.TUPLE);
        ExpectToken(new Token.LParen());
        var fieldDefs = new List<StructField>();
        while (true)
        {
            var (def, _) = ParseStructFieldDef();
            fieldDefs.Add(def);
            if (!ConsumeToken(new Token.Comma()))
            {
                break;
            }
        }
        ExpectToken(new Token.RParen());

        return fieldDefs.ToSequence();
    }

    /// <summary>
    /// For nested types that use the angle bracket syntax, this matches either
    /// <c>&gt;</c>, <c>&gt;&gt;</c> or nothing depending on which variant is expected (specified by the previously
    /// matched <c>trailing_bracket</c> argument). It returns whether there is a trailing
    /// left to be matched - (i.e. if '&gt;&gt;' was matched).
    /// </summary>
    internal MatchedTrailingBracket ExpectClosingAngleBracket(MatchedTrailingBracket trailingBracket)
    {
        MatchedTrailingBracket matched;
        if (!trailingBracket.Value)
        {
            switch (PeekTokenRef().Token)
            {
                case Token.Gt:
                    NextToken();
                    matched = MatchedTrailingBracket.From(false);
                    break;
                case Token.ShiftRight:
                    NextToken();
                    matched = MatchedTrailingBracket.From(true);
                    break;
                default:
                    return ExpectedRef<MatchedTrailingBracket>(">", PeekTokenRef());
            }
        }
        else
        {
            matched = MatchedTrailingBracket.From(false);
        }

        return matched;
    }

    /// <summary>Parse an operator following an expression</summary>
    public Expr ParseInfix(Expr expr, byte precedence)
    {
        // allow the dialect to override infix parsing
        if (_dialect.ParseInfix(this, expr, precedence) is { } infix)
        {
            return infix;
        }

        var dialect = _dialect;

        AdvanceToken();
        var tok = GetCurrentToken();
        // Rust: debug!("infix: {tok:?}"): log não traduzido (caso 44).
        var tokIndex = GetCurrentIndex();
        var span = tok.Span;
        BinaryOperator? regularBinaryOperator;
        switch (tok.Token)
        {
            case Token.Spaceship:
                regularBinaryOperator = new BinaryOperator.Spaceship();
                break;
            case Token.DoubleEq:
                regularBinaryOperator = new BinaryOperator.Eq();
                break;
            case Token.Assignment:
                regularBinaryOperator = new BinaryOperator.Assignment();
                break;
            case Token.Eq:
                regularBinaryOperator = new BinaryOperator.Eq();
                break;
            case Token.Neq:
                regularBinaryOperator = new BinaryOperator.NotEq();
                break;
            case Token.Gt:
                regularBinaryOperator = new BinaryOperator.Gt();
                break;
            case Token.GtEq:
                regularBinaryOperator = new BinaryOperator.GtEq();
                break;
            case Token.Lt:
                regularBinaryOperator = new BinaryOperator.Lt();
                break;
            case Token.LtEq:
                regularBinaryOperator = new BinaryOperator.LtEq();
                break;
            case Token.Plus:
                regularBinaryOperator = new BinaryOperator.Plus();
                break;
            case Token.Minus:
                regularBinaryOperator = new BinaryOperator.Minus();
                break;
            case Token.Mul:
                regularBinaryOperator = new BinaryOperator.Multiply();
                break;
            case Token.Mod:
                regularBinaryOperator = new BinaryOperator.Modulo();
                break;
            case Token.StringConcat:
                regularBinaryOperator = new BinaryOperator.StringConcat();
                break;
            case Token.Pipe:
                regularBinaryOperator = new BinaryOperator.BitwiseOr();
                break;
            case Token.Caret:
                // In PostgreSQL, ^ stands for the exponentiation operation,
                // and # stands for XOR. See https://www.postgresql.org/docs/current/functions-math.html
                if (dialect is PostgreSqlDialect)
                {
                    regularBinaryOperator = new BinaryOperator.PGExp();
                }
                else
                {
                    regularBinaryOperator = new BinaryOperator.BitwiseXor();
                }
                break;
            case Token.Ampersand:
                regularBinaryOperator = new BinaryOperator.BitwiseAnd();
                break;
            case Token.Div:
                regularBinaryOperator = new BinaryOperator.Divide();
                break;
            case Token.DuckIntDiv when dialect is DuckDbDialect or GenericDialect:
                regularBinaryOperator = new BinaryOperator.DuckIntegerDivide();
                break;
            case Token.ShiftLeft when dialect.SupportsBitwiseShiftOperators:
                regularBinaryOperator = new BinaryOperator.PGBitwiseShiftLeft();
                break;
            case Token.ShiftRight when dialect.SupportsBitwiseShiftOperators:
                regularBinaryOperator = new BinaryOperator.PGBitwiseShiftRight();
                break;
            case Token.Sharp when dialect is PostgreSqlDialect or RedshiftSqlDialect:
                regularBinaryOperator = new BinaryOperator.PGBitwiseXor();
                break;
            case Token.Overlap when dialect is PostgreSqlDialect or RedshiftSqlDialect:
                regularBinaryOperator = new BinaryOperator.PGOverlap();
                break;
            case Token.Overlap when dialect is PostgreSqlDialect or GenericDialect:
                regularBinaryOperator = new BinaryOperator.PGOverlap();
                break;
            case Token.Overlap when dialect.SupportsDoubleAmpersandOperator:
                regularBinaryOperator = new BinaryOperator.And();
                break;
            case Token.CaretAt when dialect is PostgreSqlDialect or GenericDialect:
                regularBinaryOperator = new BinaryOperator.PGStartsWith();
                break;
            case Token.Tilde:
                regularBinaryOperator = new BinaryOperator.PGRegexMatch();
                break;
            case Token.TildeAsterisk:
                regularBinaryOperator = new BinaryOperator.PGRegexIMatch();
                break;
            case Token.ExclamationMarkTilde:
                regularBinaryOperator = new BinaryOperator.PGRegexNotMatch();
                break;
            case Token.ExclamationMarkTildeAsterisk:
                regularBinaryOperator = new BinaryOperator.PGRegexNotIMatch();
                break;
            case Token.DoubleTilde:
                regularBinaryOperator = new BinaryOperator.PGLikeMatch();
                break;
            case Token.DoubleTildeAsterisk:
                regularBinaryOperator = new BinaryOperator.PGILikeMatch();
                break;
            case Token.ExclamationMarkDoubleTilde:
                regularBinaryOperator = new BinaryOperator.PGNotLikeMatch();
                break;
            case Token.ExclamationMarkDoubleTildeAsterisk:
                regularBinaryOperator = new BinaryOperator.PGNotILikeMatch();
                break;
            case Token.Arrow:
                regularBinaryOperator = new BinaryOperator.Arrow();
                break;
            case Token.LongArrow:
                regularBinaryOperator = new BinaryOperator.LongArrow();
                break;
            case Token.HashArrow:
                regularBinaryOperator = new BinaryOperator.HashArrow();
                break;
            case Token.HashLongArrow:
                regularBinaryOperator = new BinaryOperator.HashLongArrow();
                break;
            case Token.AtArrow:
                regularBinaryOperator = new BinaryOperator.AtArrow();
                break;
            case Token.ArrowAt:
                regularBinaryOperator = new BinaryOperator.ArrowAt();
                break;
            case Token.HashMinus:
                regularBinaryOperator = new BinaryOperator.HashMinus();
                break;
            case Token.AtQuestion:
                regularBinaryOperator = new BinaryOperator.AtQuestion();
                break;
            case Token.AtAt:
                regularBinaryOperator = new BinaryOperator.AtAt();
                break;
            case Token.Question:
                regularBinaryOperator = new BinaryOperator.Question();
                break;
            case Token.QuestionAnd:
                regularBinaryOperator = new BinaryOperator.QuestionAnd();
                break;
            case Token.QuestionPipe:
                regularBinaryOperator = new BinaryOperator.QuestionPipe();
                break;
            case Token.CustomBinaryOperator { Value: var s }:
                regularBinaryOperator = new BinaryOperator.Custom(s);
                break;
            case Token.DoubleSharp when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.DoubleHash();
                break;

            case Token.AmpersandLeftAngleBracket when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.AndLt();
                break;
            case Token.AmpersandRightAngleBracket when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.AndGt();
                break;
            case Token.QuestionMarkDash when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.QuestionDash();
                break;
            case Token.AmpersandLeftAngleBracketVerticalBar
                when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.AndLtPipe();
                break;
            case Token.VerticalBarAmpersandRightAngleBracket
                when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.PipeAndGt();
                break;
            case Token.TwoWayArrow when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.LtDashGt();
                break;
            case Token.LeftAngleBracketCaret when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.LtCaret();
                break;
            case Token.RightAngleBracketCaret when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.GtCaret();
                break;
            case Token.QuestionMarkSharp when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.QuestionHash();
                break;
            case Token.QuestionMarkDoubleVerticalBar when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.QuestionDoublePipe();
                break;
            case Token.QuestionMarkDashVerticalBar when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.QuestionDashPipe();
                break;
            case Token.TildeEqual when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.TildeEq();
                break;
            case Token.ShiftLeftVerticalBar when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.LtLtPipe();
                break;
            case Token.VerticalBarShiftRight when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.PipeGtGt();
                break;
            case Token.AtSign when _dialect.SupportsGeometricTypes:
                regularBinaryOperator = new BinaryOperator.At();
                break;

            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.AND:
                        regularBinaryOperator = new BinaryOperator.And();
                        break;
                    case Keyword.OR:
                        regularBinaryOperator = new BinaryOperator.Or();
                        break;
                    case Keyword.XOR:
                        regularBinaryOperator = new BinaryOperator.Xor();
                        break;
                    case Keyword.OVERLAPS:
                        regularBinaryOperator = new BinaryOperator.Overlaps();
                        break;
                    case Keyword.OPERATOR when dialect is PostgreSqlDialect or GenericDialect:
                        // there are special rules for operator names in
                        // postgres so we can not use 'parse_object'
                        // or similar.
                        // See https://www.postgresql.org/docs/current/sql-createoperator.html
                        regularBinaryOperator = new BinaryOperator.PGCustomBinaryOperator(
                            ParsePgOperatorIdentParts());
                        break;
                    default:
                        regularBinaryOperator = null;
                        break;
                }
                break;
            default:
                regularBinaryOperator = null;
                break;
        }

        tok = TokenAt(tokIndex);
        if (regularBinaryOperator is { } op)
        {
            if (ParseOneOfKeywords(Keyword.ANY, Keyword.ALL, Keyword.SOME) is { } keyword)
            {
                ExpectToken(new Token.LParen());
                Expr right;
                if (PeekSubQuery())
                {
                    // We have a subquery ahead (SELECT\WITH ...) need to rewind and
                    // use the parenthesis for parsing the subquery as an expression.
                    PrevToken(); // LParen
                    right = ParseSubexpr(precedence);
                }
                else
                {
                    // Non-subquery expression
                    var nonSubqueryRight = ParseSubexpr(precedence);
                    ExpectToken(new Token.RParen());
                    right = nonSubqueryRight;
                }

                if (!(op is BinaryOperator.Gt
                    or BinaryOperator.Lt
                    or BinaryOperator.GtEq
                    or BinaryOperator.LtEq
                    or BinaryOperator.Eq
                    or BinaryOperator.NotEq
                    or BinaryOperator.PGRegexMatch
                    or BinaryOperator.PGRegexIMatch
                    or BinaryOperator.PGRegexNotMatch
                    or BinaryOperator.PGRegexNotIMatch
                    or BinaryOperator.PGLikeMatch
                    or BinaryOperator.PGILikeMatch
                    or BinaryOperator.PGNotLikeMatch
                    or BinaryOperator.PGNotILikeMatch))
                {
                    throw ParserErr(
                        $"Expected one of [=, >, <, =>, =<, !=, ~, ~*, !~, !~*, ~~, ~~*, !~~, !~~*] as comparison operator, found: {op}",
                        span.Start);
                }

                switch (keyword)
                {
                    case Keyword.ALL:
                        return new Expr.AllOp(
                            Left: expr,
                            CompareOp: op,
                            Right: right);
                    case Keyword.ANY or Keyword.SOME:
                        return new Expr.AnyOp(
                            Left: expr,
                            CompareOp: op,
                            Right: right,
                            IsSome: keyword == Keyword.SOME);
                    default:
                        // {unexpected_keyword:?} de Keyword = nome da variante (caso 46).
                        throw new ParserException(new ParserError.ParserErrorValue(
                            $"Internal parser error: expected any of {{ALL, ANY, SOME}}, got {keyword.ToString()}"));
                }
            }
            else
            {
                return new Expr.BinaryOp(
                    Left: expr,
                    Op: op,
                    Right: ParseSubexpr(precedence));
            }
        }
        else if (tok.Token is Token.Word { Value: var w })
        {
            switch (w.Keyword)
            {
                case Keyword.IS:
                    if (ParseKeyword(Keyword.NULL))
                    {
                        return new Expr.IsNull(expr);
                    }
                    else if (ParseKeywords(Keyword.NOT, Keyword.NULL))
                    {
                        return new Expr.IsNotNull(expr);
                    }
                    else if (ParseKeywords(Keyword.TRUE))
                    {
                        return new Expr.IsTrue(expr);
                    }
                    else if (ParseKeywords(Keyword.NOT, Keyword.TRUE))
                    {
                        return new Expr.IsNotTrue(expr);
                    }
                    else if (ParseKeywords(Keyword.FALSE))
                    {
                        return new Expr.IsFalse(expr);
                    }
                    else if (ParseKeywords(Keyword.NOT, Keyword.FALSE))
                    {
                        return new Expr.IsNotFalse(expr);
                    }
                    else if (ParseKeywords(Keyword.UNKNOWN))
                    {
                        return new Expr.IsUnknown(expr);
                    }
                    else if (ParseKeywords(Keyword.NOT, Keyword.UNKNOWN))
                    {
                        return new Expr.IsNotUnknown(expr);
                    }
                    else if (ParseKeywords(Keyword.DISTINCT, Keyword.FROM))
                    {
                        // The right operand binds no more loosely than `IS`
                        // itself, so that e.g. `a IS DISTINCT FROM b AND c`
                        // parses as `(a IS DISTINCT FROM b) AND c`.
                        var expr2 = ParseSubexpr(precedence);
                        return new Expr.IsDistinctFrom(expr, expr2);
                    }
                    else if (ParseKeywords(Keyword.NOT, Keyword.DISTINCT, Keyword.FROM))
                    {
                        var expr2 = ParseSubexpr(precedence);
                        return new Expr.IsNotDistinctFrom(expr, expr2);
                    }
                    else if (ParseKeyword(Keyword.JSON))
                    {
                        return ParseIsJsonPredicate(expr, false);
                    }
                    else if (ParseKeywords(Keyword.NOT, Keyword.JSON))
                    {
                        return ParseIsJsonPredicate(expr, true);
                    }
                    else
                    {
                        // else if let Ok(is_normalized) = self.parse_unicode_is_normalized(expr)
                        Expr? isNormalized;
                        try
                        {
                            isNormalized = ParseUnicodeIsNormalized(expr);
                        }
                        catch (ParserException)
                        {
                            isNormalized = null;
                        }
                        if (isNormalized is { } isNormalizedExpr)
                        {
                            return isNormalizedExpr;
                        }
                        else
                        {
                            return ExpectedRef<Expr>(
                                "[NOT] NULL | TRUE | FALSE | DISTINCT | [NOT] JSON [VALUE | SCALAR | ARRAY | OBJECT] [WITH | WITHOUT UNIQUE [KEYS]] | [form] NORMALIZED FROM after IS",
                                PeekTokenRef());
                        }
                    }
                case Keyword.AT:
                    ExpectKeywords(Keyword.TIME, Keyword.ZONE);
                    return new Expr.AtTimeZone(
                        Timestamp: expr,
                        TimeZone: ParseSubexpr(precedence));
                case Keyword.NOT
                    or Keyword.IN
                    or Keyword.BETWEEN
                    or Keyword.LIKE
                    or Keyword.ILIKE
                    or Keyword.SIMILAR
                    or Keyword.REGEXP
                    or Keyword.RLIKE:
                {
                    PrevToken();
                    var negated = ParseKeyword(Keyword.NOT);
                    var regexp = ParseKeyword(Keyword.REGEXP);
                    var rlike = ParseKeyword(Keyword.RLIKE);
                    bool @null;
                    if (!InColumnDefinitionState())
                    {
                        @null = ParseKeyword(Keyword.NULL);
                    }
                    else
                    {
                        @null = false;
                    }
                    if (regexp || rlike)
                    {
                        return new Expr.RLike(
                            Negated: negated,
                            Expr: expr,
                            Pattern: ParseSubexpr(_dialect.PrecValue(Precedence.Like)),
                            Regexp: regexp);
                    }
                    else if (negated && @null)
                    {
                        return new Expr.IsNotNull(expr);
                    }
                    else if (ParseKeyword(Keyword.IN))
                    {
                        return ParseIn(expr, negated);
                    }
                    else if (ParseKeyword(Keyword.BETWEEN))
                    {
                        return ParseBetween(expr, negated);
                    }
                    else if (ParseKeyword(Keyword.LIKE))
                    {
                        return new Expr.Like(
                            Negated: negated,
                            Any: ParseKeyword(Keyword.ANY),
                            Expr: expr,
                            Pattern: ParseSubexpr(_dialect.PrecValue(Precedence.Like)),
                            EscapeChar: ParseEscapeChar());
                    }
                    else if (ParseKeyword(Keyword.ILIKE))
                    {
                        return new Expr.ILike(
                            Negated: negated,
                            Any: ParseKeyword(Keyword.ANY),
                            Expr: expr,
                            Pattern: ParseSubexpr(_dialect.PrecValue(Precedence.Like)),
                            EscapeChar: ParseEscapeChar());
                    }
                    else if (ParseKeywords(Keyword.SIMILAR, Keyword.TO))
                    {
                        return new Expr.SimilarTo(
                            Negated: negated,
                            Expr: expr,
                            Pattern: ParseSubexpr(_dialect.PrecValue(Precedence.Like)),
                            EscapeChar: ParseEscapeChar());
                    }
                    else
                    {
                        return ExpectedRef<Expr>("IN or BETWEEN after NOT", PeekTokenRef());
                    }
                }
                case Keyword.NOTNULL when dialect.SupportsNotnullOperator:
                    return new Expr.IsNotNull(expr);
                case Keyword.MEMBER:
                    if (ParseKeyword(Keyword.OF))
                    {
                        ExpectToken(new Token.LParen());
                        var array = ParseExpr();
                        ExpectToken(new Token.RParen());
                        return new Expr.MemberOf(new MemberOf(
                            Value: expr,
                            Array: array));
                    }
                    else
                    {
                        return ExpectedRef<Expr>("OF after MEMBER", PeekTokenRef());
                    }
                // Can only happen if `get_next_precedence` got out of sync with this function
                default:
                    throw ParserErr(
                        $"No infix parser for token {tok.Token.ToDebugString()}",
                        tok.Span.Start);
            }
        }
        else if (tok.Token is Token.DoubleColon)
        {
            return new Expr.Cast(
                Kind: CastKind.DoubleColon,
                Expr: expr,
                DataType: ParseDataType(),
                Format: null);
        }
        else if (tok.Token is Token.ExclamationMark && _dialect.SupportsFactorialOperator)
        {
            return new Expr.UnaryOp(
                Op: new UnaryOperator.PGPostfixFactorial(),
                Expr: expr);
        }
        else if ((tok.Token is Token.LBracket && _dialect.SupportsPartiql)
            || tok.Token is Token.Colon)
        {
            PrevToken();
            return ParseJsonAccess(expr);
        }
        else
        {
            // Can only happen if `get_next_precedence` got out of sync with this function
            throw ParserErr(
                $"No infix parser for token {tok.Token.ToDebugString()}",
                tok.Span.Start);
        }
    }

    /// <summary>Parse the <c>ESCAPE CHAR</c> portion of <c>LIKE</c>, <c>ILIKE</c>, and <c>SIMILAR TO</c></summary>
    public Expr? ParseEscapeChar()
    {
        if (ParseKeyword(Keyword.ESCAPE))
        {
            return ParseSubexpr(
                _dialect.PrecValue(Precedence.Like));
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Parses an array subscript like
    /// <list type="bullet">
    /// <item><c>[:]</c></item>
    /// <item><c>[l]</c></item>
    /// <item><c>[l:]</c></item>
    /// <item><c>[:u]</c></item>
    /// <item><c>[l:u]</c></item>
    /// <item><c>[l:u:s]</c></item>
    /// </list>
    /// </summary>
    /// <remarks>Parser is right after <c>[</c></remarks>
    internal Subscript ParseSubscriptInner()
    {
        // at either `<lower>:(rest)` or `:(rest)]`
        Expr? lowerBound;
        if (ConsumeToken(new Token.Colon()))
        {
            lowerBound = null;
        }
        else
        {
            // parse expr until we hit a colon (or any token with lower precedence)
            lowerBound = ParseSubexpr(_dialect.PrecValue(Precedence.Colon));
        }

        // check for end
        if (ConsumeToken(new Token.RBracket()))
        {
            if (lowerBound is { } index)
            {
                return new Subscript.Index(IndexValue: index);
            }
            return new Subscript.Slice(
                LowerBound: lowerBound,
                UpperBound: null,
                Stride: null);
        }

        // consume the `:`
        if (lowerBound is not null)
        {
            ExpectToken(new Token.Colon());
        }

        // we are now at either `]`, `<upper>(rest)]`
        Expr? upperBound;
        if (ConsumeToken(new Token.RBracket()))
        {
            return new Subscript.Slice(
                LowerBound: lowerBound,
                UpperBound: null,
                Stride: null);
        }
        else
        {
            // parse expr until we hit a colon (or any token with lower precedence)
            upperBound = ParseSubexpr(_dialect.PrecValue(Precedence.Colon));
        }

        // check for end
        if (ConsumeToken(new Token.RBracket()))
        {
            return new Subscript.Slice(
                LowerBound: lowerBound,
                UpperBound: upperBound,
                Stride: null);
        }

        // we are now at `:]` or `:stride]`
        ExpectToken(new Token.Colon());
        Expr? stride;
        if (ConsumeToken(new Token.RBracket()))
        {
            stride = null;
        }
        else
        {
            stride = ParseExpr();
        }

        if (stride is not null)
        {
            ExpectToken(new Token.RBracket());
        }

        return new Subscript.Slice(
            LowerBound: lowerBound,
            UpperBound: upperBound,
            Stride: stride);
    }

    /// <summary>Parse a multi-dimension array accessing like <c>[1:3][1][1]</c></summary>
    // `chain: &mut Vec<AccessExpr>` vira `List<AccessExpr>` (docs/port/onda-b/B1.md).
    public void ParseMultiDimSubscript(List<AccessExpr> chain)
    {
        while (ConsumeToken(new Token.LBracket()))
        {
            ParseSubscript(chain);
        }
    }

    /// <summary>Parses an array subscript like <c>[1:3]</c></summary>
    /// <remarks>Parser is right after <c>[</c></remarks>
    internal void ParseSubscript(List<AccessExpr> chain)
    {
        var subscript = ParseSubscriptInner();
        chain.Add(new AccessExpr.Subscript(subscript));
    }

    internal JsonPathElem ParseJsonPathObjectKey()
    {
        var token = NextToken();
        switch (token.Token)
        {
            // path segments in SF dot notation can be unquoted or double-quoted;
            // Databricks also supports backtick-quoted identifiers
            // some experimentation suggests that snowflake permits
            // any keyword here unquoted.
            case Token.Word { Value: { QuoteStyle: '"' or '`' or null } word }:
                return new JsonPathElem.Dot(
                    Key: word.Value,
                    Quoted: word.QuoteStyle is not null);

            // This token should never be generated on snowflake or generic
            // dialects, but we handle it just in case this is used on future
            // dialects.
            case Token.DoubleQuotedString { Value: var key }:
                return new JsonPathElem.Dot(Key: key, Quoted: true);

            default:
                return Expected<JsonPathElem>("variant object key name", token);
        }
    }

    internal Expr ParseJsonAccess(Expr expr)
    {
        var path = ParseJsonPath();
        return new Expr.JsonAccess(
            Value: expr,
            Path: path);
    }

    internal JsonPath ParseJsonPath()
    {
        var path = new List<JsonPathElem>();
        while (true)
        {
            var stop = false;
            switch (NextToken().Token)
            {
                case Token.Colon when path.Count == 0 && PeekTokenRef().Equals(new Token.LBracket()):
                {
                    NextToken();
                    var key = ParseWildcardExpr();
                    ExpectToken(new Token.RBracket());
                    path.Add(new JsonPathElem.ColonBracket(Key: key));
                    break;
                }
                case Token.Colon when path.Count == 0:
                    path.Add(ParseJsonPathObjectKey());
                    break;
                case Token.Period when path.Count != 0:
                    path.Add(ParseJsonPathObjectKey());
                    break;
                case Token.LBracket:
                {
                    var key = ParseWildcardExpr();
                    ExpectToken(new Token.RBracket());

                    path.Add(new JsonPathElem.Bracket(Key: key));
                    break;
                }
                default:
                    PrevToken();
                    stop = true;
                    break;
            }
            if (stop)
            {
                break;
            }
        }

#if DEBUG
        // debug_assert! (convencoes.md §13, caso 68)
        if (!(path.Count != 0))
        {
            throw new InvalidOperationException("assertion failed: !path.is_empty()");
        }
#endif
        return new JsonPath(Path: path.ToSequence());
    }

    /// <summary>Parses the parens following the <c>[ NOT ] IN</c> operator.</summary>
    public Expr ParseIn(Expr expr, bool negated)
    {
        // BigQuery allows `IN UNNEST(array_expression)`
        // https://cloud.google.com/bigquery/docs/reference/standard-sql/operators#in_operators
        if (ParseKeyword(Keyword.UNNEST))
        {
            ExpectToken(new Token.LParen());
            var arrayExpr = ParseExpr();
            ExpectToken(new Token.RParen());
            return new Expr.InUnnest(
                Expr: expr,
                ArrayExpr: arrayExpr,
                Negated: negated);
        }
        if (_dialect.SupportsInUnparenthesizedExpr
            && PeekTokenRef().Token is not Token.LParen)
        {
            return new Expr.InList(
                Expr: expr,
                List: Sequence.Of(ParseExpr()),
                Negated: negated);
        }
        ExpectToken(new Token.LParen());
        Expr inOp;
        switch (MaybeParse(p => p.ParseQuery()))
        {
            case { } subquery:
                inOp = new Expr.InSubquery(
                    Expr: expr,
                    Subquery: subquery,
                    Negated: negated);
                break;
            case null:
                inOp = new Expr.InList(
                    Expr: expr,
                    List: _dialect.SupportsInEmptyList
                        ? ParseCommaSeparated0(p => p.ParseExpr(), new Token.RParen())
                        : ParseCommaSeparated(p => p.ParseExpr()),
                    Negated: negated);
                break;
        }
        ExpectToken(new Token.RParen());
        return inOp;
    }

    /// <summary>Parses <c>BETWEEN &lt;low&gt; AND &lt;high&gt;</c>, assuming the <c>BETWEEN</c> keyword was already consumed.</summary>
    public Expr ParseBetween(Expr expr, bool negated)
    {
        // Stop parsing subexpressions for <low> and <high> on tokens with
        // precedence lower than that of `BETWEEN`, such as `AND`, `IS`, etc.
        var low = ParseSubexpr(_dialect.PrecValue(Precedence.Between));
        ExpectKeywordIs(Keyword.AND);
        var high = ParseSubexpr(_dialect.PrecValue(Precedence.Between));
        return new Expr.Between(
            Expr: expr,
            Negated: negated,
            Low: low,
            High: high);
    }

    /// <summary>Parse a PostgreSQL casting style which is in the form of <c>expr::datatype</c>.</summary>
    public Expr ParsePgCast(Expr expr)
    {
        return new Expr.Cast(
            Kind: CastKind.DoubleColon,
            Expr: expr,
            DataType: ParseDataType(),
            Format: null);
    }
}
