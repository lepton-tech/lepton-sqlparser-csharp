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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 1528–2535:
// parse_expr_prefix_by_reserved_word … parse_xmlparse_argument_list).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>
    /// Tries to parse an expression by matching the specified word to known keywords that have a special meaning in the dialect.
    /// Returns `None if no match is found.
    /// </summary>
    internal Expr? ParseExprPrefixByReservedWord(Word w, Span wSpan)
    {
        switch (w.Keyword)
        {
            case Keyword.TRUE or Keyword.FALSE when _dialect.SupportsBooleanLiterals:
                PrevToken();
                return new Expr.Value(ParseValue());
            case Keyword.NULL:
                PrevToken();
                return new Expr.Value(ParseValue());
            case Keyword.CURRENT_CATALOG
                or Keyword.CURRENT_USER
                or Keyword.SESSION_USER
                or Keyword.USER
                when _dialect is PostgreSqlDialect or GenericDialect:
                return new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(w.ToIdent(wSpan))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.None(),
                    NullTreatment: null,
                    Filter: null,
                    Over: null,
                    WithinGroup: Sequence<OrderByExpr>.Empty));
            case Keyword.CURRENT_TIMESTAMP
                or Keyword.CURRENT_TIME
                or Keyword.CURRENT_DATE
                or Keyword.LOCALTIME
                or Keyword.LOCALTIMESTAMP:
                return ParseTimeFunctions(ObjectName.From(Sequence.Of(w.ToIdent(wSpan))));
            case Keyword.CASE:
                return ParseCaseExpr();
            case Keyword.CONVERT:
                return ParseConvertExpr(false);
            case Keyword.TRY_CONVERT when _dialect.SupportsTryConvert:
                return ParseConvertExpr(true);
            case Keyword.CAST:
                return ParseCastExpr(CastKind.Cast);
            case Keyword.TRY_CAST:
                return ParseCastExpr(CastKind.TryCast);
            case Keyword.SAFE_CAST:
                return ParseCastExpr(CastKind.SafeCast);
            case Keyword.EXISTS
                // Support parsing Databricks has a function named `exists`.
                when !(_dialect is DatabricksDialect)
                    || PeekNthTokenRef(1).Token is Token.Word { Value: { Keyword: Keyword.SELECT or Keyword.WITH } }:
                return ParseExistsExpr(false);
            case Keyword.EXTRACT:
                return ParseExtractExpr();
            case Keyword.CEIL:
                return ParseCeilFloorExpr(true);
            case Keyword.FLOOR:
                return ParseCeilFloorExpr(false);
            case Keyword.POSITION when PeekTokenRef().Token is Token.LParen:
                return ParsePositionExpr(w.ToIdent(wSpan));
            case Keyword.SUBSTR or Keyword.SUBSTRING:
                PrevToken();
                return ParseSubstring();
            case Keyword.OVERLAY:
                return ParseOverlayExpr();
            case Keyword.TRIM:
                return ParseTrimExpr();
            case Keyword.INTERVAL:
                return ParseInterval();
            // Treat ARRAY[1,2,3] as an array [1,2,3], otherwise try as subquery or a function call
            case Keyword.ARRAY when PeekTokenRef().Equals(new Token.LBracket()):
                ExpectToken(new Token.LBracket());
                return ParseArrayExpr(true);
            case Keyword.ARRAY
                when PeekTokenRef().Token is Token.LParen
                    && !(_dialect is ClickHouseDialect or DatabricksDialect):
            {
                ExpectToken(new Token.LParen());
                var query = ParseQuery();
                ExpectToken(new Token.RParen());
                return new Expr.Function(new Function(
                    Name: ObjectName.From(Sequence.Of(w.ToIdent(wSpan))),
                    UsesOdbcSyntax: false,
                    Parameters: new FunctionArguments.None(),
                    Args: new FunctionArguments.Subquery(query),
                    Filter: null,
                    NullTreatment: null,
                    Over: null,
                    WithinGroup: Sequence<OrderByExpr>.Empty));
            }
            case Keyword.NOT:
                return ParseNot();
            case Keyword.MATCH when _dialect.SupportsMatchAgainst:
                return ParseMatchAgainst();
            case Keyword.STRUCT when _dialect.SupportsStructLiteral:
            {
                var structExpr = ParseStructLiteral();
                return structExpr;
            }
            case Keyword.PRIOR when _state == ParserState.ConnectBy:
            {
                var expr = ParseSubexpr(_dialect.PrecValue(Precedence.PlusMinus));
                return new Expr.Prior(expr);
            }
            case Keyword.MAP when PeekTokenRef().Equals(new Token.LBrace()) && _dialect.SupportMapLiteralSyntax:
                return ParseDuckdbMapLiteral();
            case Keyword.APPROXIMATE
                when _dialect.SupportsApproximatePercentileDisc
                    && PeekKeyword(Keyword.PERCENTILE_DISC):
                return MaybeParse<Expr>(parser =>
                {
                    var functionName = parser.ParseObjectName(false);
                    var function = parser.ParseFunction(functionName);
                    return new Expr.Prefixed(
                        Prefix: w.ToIdent(wSpan),
                        Value: function);
                });
            case Keyword.LAMBDA when _dialect.SupportsLambdaKeywordSyntax:
                return ParseLambdaExpr();
            case var _ when _dialect.SupportsGeometricTypes:
                switch (w.Keyword)
                {
                    case Keyword.CIRCLE:
                        return ParseGeometricType(new GeometricTypeKind.Circle());
                    case Keyword.BOX:
                        return ParseGeometricType(new GeometricTypeKind.GeometricBox());
                    case Keyword.PATH:
                        return ParseGeometricType(new GeometricTypeKind.GeometricPath());
                    case Keyword.LINE:
                        return ParseGeometricType(new GeometricTypeKind.Line());
                    case Keyword.LSEG:
                        return ParseGeometricType(new GeometricTypeKind.LineSegment());
                    case Keyword.POINT:
                        return ParseGeometricType(new GeometricTypeKind.Point());
                    case Keyword.POLYGON:
                        return ParseGeometricType(new GeometricTypeKind.Polygon());
                    default:
                        return null;
                }
            default:
                return null;
        }
    }

    /// <summary>Tries to parse an expression by a word that is not known to have a special meaning in the dialect.</summary>
    internal Expr ParseExprPrefixByUnreservedWord(Word w, Span wSpan)
    {
        var isOuterJoin = PeekOuterJoinOperator();
        switch (PeekTokenRef().Token)
        {
            case Token.LParen when !isOuterJoin:
            {
                var idParts = Sequence.Of(w.ToIdent(wSpan));
                return ParseFunction(ObjectName.From(idParts));
            }
            // string introducer https://dev.mysql.com/doc/refman/8.0/en/charset-introducer.html
            case Token.SingleQuotedString
                or Token.DoubleQuotedString
                or Token.HexStringLiteral
                when w.Value.StartsWith("_", StringComparison.Ordinal):
                return new Expr.Prefixed(
                    Prefix: w.ToIdent(wSpan),
                    Value: ParseIntroducedStringExpr());
            // string introducer https://dev.mysql.com/doc/refman/8.0/en/charset-introducer.html
            // (braço repetido no Rust, inalcançável: portado como está)
            case Token.SingleQuotedString
                or Token.DoubleQuotedString
                or Token.HexStringLiteral
                when w.Value.StartsWith("_", StringComparison.Ordinal):
                return new Expr.Prefixed(
                    Prefix: w.ToIdent(wSpan),
                    Value: ParseIntroducedStringExpr());
            // An unreserved word (likely an identifier) is followed by an arrow,
            // which indicates a lambda function with a single, untyped parameter.
            // For example: `a -> a * 2`.
            case Token.Arrow when _dialect.SupportsLambdaFunctions:
                ExpectToken(new Token.Arrow());
                return new Expr.Lambda(new LambdaFunction(
                    Params: new OneOrManyWithParens<LambdaFunctionParameter>.One(new LambdaFunctionParameter(
                        Name: w.ToIdent(wSpan),
                        DataType: null)),
                    Body: ParseExpr(),
                    Syntax: LambdaSyntax.Arrow));
            // An unreserved word (likely an identifier) that is followed by another word (likley a data type)
            // which is then followed by an arrow, which indicates a lambda function with a single, typed parameter.
            // For example: `a INT -> a * 2`.
            case Token.Word
                when _dialect.SupportsLambdaFunctions
                    && PeekNthTokenRef(1).Token is Token.Arrow:
            {
                var dataType = ParseDataType();
                ExpectToken(new Token.Arrow());
                return new Expr.Lambda(new LambdaFunction(
                    Params: new OneOrManyWithParens<LambdaFunctionParameter>.One(new LambdaFunctionParameter(
                        Name: w.ToIdent(wSpan),
                        DataType: dataType)),
                    Body: ParseExpr(),
                    Syntax: LambdaSyntax.Arrow));
            }
            default:
                return new Expr.Identifier(w.ToIdent(wSpan));
        }
    }

    /// <summary>
    /// Returns true if the given <c>ObjectName</c> is a single unquoted
    /// identifier matching <c>expected</c> (case-insensitive).
    /// </summary>
    internal static bool IsSimpleUnquotedObjectName(ObjectName name, string expected)
    {
        // if let [ObjectNamePart::Identifier(ident)] = name.0.as_slice()
        if (name.Value.Count == 1 && name.Value[0] is ObjectNamePart.Identifier { Value: var ident })
        {
            return ident.QuoteStyle is null && RustChar.EqIgnoreAsciiCase(ident.Value, expected);
        }
        else
        {
            return false;
        }
    }

    /// <summary>Parse an expression prefix.</summary>
    public Expr ParsePrefix()
    {
        // allow the dialect to override prefix parsing
        if (_dialect.ParsePrefix(this) is { } prefix)
        {
            return prefix;
        }

        // Memoize parse_prefix failures to break 2^N speculation when both
        // prefix arms fail at every level (e.g. `IF(current_time(...x`).
        // The per-arm cache in `parse_prefix_inner` complements this for
        // chains where the reserved arm fails but the unreserved fallback
        // succeeds (e.g. `case-case-...c`).
        var startIndex = _index;
        if (_failedPrefixPositions.TryGetValue(startIndex, out var cached))
        {
            return CachedPrefixError<Expr>(cached, PeekTokenRef());
        }
        // let result = self.parse_prefix_inner(); if let Err(ref e) = result { .. }
        // O Err é tratado e relançado fora do catch (convencoes.md §13, caso 124).
        ParserException e;
        try
        {
            return ParsePrefixInner();
        }
        catch (ParserException error)
        {
            e = error;
        }

        _failedPrefixPositions[startIndex] = ExprPrefixError.From(e.Error);
        throw StackGuard.Rethrow(e);
    }

    /// <summary>Rebuild the error for a cached prefix failure at the <c>found</c> token.</summary>
    internal T CachedPrefixError<T>(ExprPrefixError cached, TokenWithSpan found)
    {
        switch (cached)
        {
            case ExprPrefixError.RecursionLimitExceeded:
                throw new ParserException(new ParserError.RecursionLimitExceeded());
            case ExprPrefixError.Err:
                return ExpectedRef<T>("an expression", found);
            default:
                throw new InvalidOperationException($"Variante de ExprPrefixError não tratada: {cached.GetType().Name}");
        }
    }

    internal Expr ParsePrefixInner()
    {
        // PostgreSQL allows any string literal to be preceded by a type name, indicating that the
        // string literal represents a literal of that type. Some examples:
        //
        //      DATE '2020-05-20'
        //      TIMESTAMP WITH TIME ZONE '2020-05-20 7:43:54'
        //      BOOL 'true'
        //
        // The first two are standard SQL, while the latter is a PostgreSQL extension. Complicating
        // matters is the fact that INTERVAL string literals may optionally be followed by special
        // keywords, e.g.:
        //
        //      INTERVAL '7' DAY
        //
        // Note also that naively `SELECT date` looks like a syntax error because the `date` type
        // name is not followed by a string literal, but in fact in PostgreSQL it is a valid
        // expression that should parse as the column name "date".
        // (`loc` só servia ao `parser_err!("dummy", loc)`, que aqui virou `return null`: caso 140.)
        var loc = PeekTokenRef().Span.Start;
        // PORT-PERF (caso 140): MaybeParseNoMatch e ParseDataTypeNoMatch. Os dois `Err` que este maybe_parse
        // descarta em quase toda expressão (`parse_data_type()?` no primeiro token e `parser_err!("dummy", loc)`)
        // viram `return null`, sem exceção; os demais erros seguem como exceção.
        var optExpr = MaybeParseNoMatch<Expr>(parser =>
        {
            switch (parser.ParseDataTypeNoMatch())
            {
                // PORT-PERF (caso 140): `parser.parse_data_type()?` com `Err` no primeiro token.
                case null:
                    return null;
                case DataType.Interval:
                    return parser.ParseInterval();
                // PostgreSQL allows almost any identifier to be used as custom data type name,
                // and we support that in `parse_data_type()`. But unlike Postgres we don't
                // have a list of globally reserved keywords (since they vary across dialects),
                // so given `NOT 'a' LIKE 'b'`, we'd accept `NOT` as a possible custom data type
                // name, resulting in `NOT 'a'` being recognized as a `TypedString` instead of
                // an unary negation `NOT ('a' LIKE 'b')`. To solve this, we don't accept the
                // `type 'string'` syntax for the custom data types at all ...
                //
                // ... with the exception of `xml '...'` on dialects that support XML
                // expressions, which is a valid PostgreSQL typed string literal.
                case DataType.Custom { Name: var name, Modifiers: var modifiers }
                    when modifiers.IsEmpty
                        && IsSimpleUnquotedObjectName(name, "xml")
                        && parser._dialect.SupportsXmlExpressions:
                    return new Expr.TypedString(new TypedString(
                        DataType: new DataType.Custom(name, modifiers),
                        Value: parser.ParseValue(),
                        UsesOdbcSyntax: false));
                case DataType.Custom:
                    // PORT-PERF (caso 140): parser_err!("dummy", loc), descartado pelo maybe_parse.
                    return null;
                // MySQL supports using the `BINARY` keyword as a cast to binary type.
                case DataType.Binary when _dialect.SupportsBinaryKwAsCast:
                    return new Expr.Cast(
                        Kind: CastKind.Cast,
                        Expr: parser.ParseExpr(),
                        DataType: new DataType.Binary(null),
                        Format: null);
                case var dataType:
                    return new Expr.TypedString(new TypedString(
                        DataType: dataType,
                        Value: parser.ParseValue(),
                        UsesOdbcSyntax: false));
            }
        });

        if (optExpr is { } typedExpr)
        {
            return typedExpr;
        }

        // Cache some dialect properties to avoid lifetime issues with the
        // next_token reference.

        var dialect = _dialect;

        AdvanceToken();
        var nextTokenIndex = GetCurrentIndex();
        var nextToken = GetCurrentToken();
        var span = nextToken.Span;
        Expr expr;
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
            {
                // The word we consumed may fall into one of two cases: it has a special meaning, or not.
                // For example, in Snowflake, the word `interval` may have two meanings depending on the context:
                // `SELECT CURRENT_DATE() + INTERVAL '1 DAY', MAX(interval) FROM tbl;`
                //                          ^^^^^^^^^^^^^^^^      ^^^^^^^^
                //                         interval expression   identifier
                //
                // We first try to parse the word and following tokens as a special expression, and if that fails,
                // we rollback and try to parse it as an identifier.
                // (`let w = w.clone();`: os records são imutáveis, a cópia não é necessária.)

                // Memoize failed speculative reserved-word parses. When
                // the reserved arm (CASE, CURRENT_TIME, etc.) does
                // exponential work but the unreserved fallback ultimately
                // succeeds, the overall `parse_prefix` returns `Ok` and the
                // outer cache never fires. Chains like `case-case-...c`
                // need this per-arm cache to break the doubling.
                Expr? tryParseResult = null;
                ParserException? tryParseError = null;
                try
                {
                    if (_failedReservedWordPrefixPositions.TryGetValue(nextTokenIndex, out var cached))
                    {
                        tryParseResult = CachedPrefixError<Expr?>(cached, GetCurrentToken());
                    }
                    else
                    {
                        tryParseResult = TryParse(parser => parser.ParseExprPrefixByReservedWord(w, span));
                    }
                }
                catch (ParserException error)
                {
                    // O braço `Err(e)` roda fora do catch (convencoes.md §13, caso 124).
                    tryParseError = error;
                }
                // If parsing of the word as a special expression failed, we are facing two options:
                // 1. The statement is malformed, e.g. `SELECT INTERVAL '1 DAI` (`DAI` instead of `DAY`)
                // 2. The word is used as an identifier, e.g. `SELECT MAX(interval) FROM tbl`
                // We first try to parse the word as an identifier and if that fails
                // we rollback and return the parsing error we got from trying to parse a
                // special expression (to maintain backwards compatibility of parsing errors).
                if (tryParseError is not null)
                {
                    _failedReservedWordPrefixPositions[nextTokenIndex] = ExprPrefixError.From(tryParseError.Error);
                    if (!_dialect.IsReservedForIdentifier(w.Keyword))
                    {
                        // if let Ok(Some(expr)) = self.maybe_parse(..): o Err (limite de recursão) é ignorado.
                        Expr? unreservedExpr;
                        try
                        {
                            unreservedExpr = MaybeParse(parser => parser.ParseExprPrefixByUnreservedWord(w, span));
                        }
                        catch (ParserException)
                        {
                            unreservedExpr = null;
                        }
                        if (unreservedExpr is { } fallbackExpr)
                        {
                            return fallbackExpr;
                        }
                    }
                    throw StackGuard.Rethrow(tryParseError);
                }
                switch (tryParseResult)
                {
                    // This word indicated an expression prefix and parsing was successful
                    case { } reservedExpr:
                        expr = reservedExpr;
                        break;

                    // No expression prefix associated with this word
                    case null:
                        expr = ParseExprPrefixByUnreservedWord(w, span);
                        break;
                }
                break;
            } // End of Token::Word
            // array `[1, 2, 3]`
            case Token.LBracket:
                expr = ParseArrayExpr(false);
                break;
            case Token.Minus or Token.Plus:
            {
                var tok = nextToken.Token;
                UnaryOperator op;
                if (tok is Token.Plus)
                {
                    op = new UnaryOperator.Plus();
                }
                else
                {
                    op = new UnaryOperator.Minus();
                }
                expr = new Expr.UnaryOp(
                    Op: op,
                    Expr: ParseSubexpr(_dialect.PrecValue(Precedence.MulDivModOp)));
                break;
            }
            case Token.ExclamationMark when dialect.SupportsBangNotOperator:
                expr = new Expr.UnaryOp(
                    Op: new UnaryOperator.BangNot(),
                    Expr: ParseSubexpr(_dialect.PrecValue(Precedence.UnaryNot)));
                break;
            case Token.DoubleExclamationMark
                or Token.PGSquareRoot
                or Token.PGCubeRoot
                or Token.AtSign
                when dialect is PostgreSqlDialect:
            {
                var tok = nextToken.Token;
                UnaryOperator op;
                switch (tok)
                {
                    case Token.DoubleExclamationMark:
                        op = new UnaryOperator.PGPrefixFactorial();
                        break;
                    case Token.PGSquareRoot:
                        op = new UnaryOperator.PGSquareRoot();
                        break;
                    case Token.PGCubeRoot:
                        op = new UnaryOperator.PGCubeRoot();
                        break;
                    case Token.AtSign:
                        op = new UnaryOperator.PGAbs();
                        break;
                    default:
                        throw new ParserException(new ParserError.ParserErrorValue(
                            "Internal parser error: unexpected unary operator token"));
                }
                expr = new Expr.UnaryOp(
                    Op: op,
                    Expr: ParseSubexpr(_dialect.PrecValue(Precedence.PlusMinus)));
                break;
            }
            case Token.Tilde:
                expr = new Expr.UnaryOp(
                    Op: new UnaryOperator.BitwiseNot(),
                    Expr: ParseSubexpr(_dialect.PrecValue(Precedence.PlusMinus)));
                break;
            case Token.Sharp
                or Token.AtDashAt
                or Token.AtAt
                or Token.QuestionMarkDash
                or Token.QuestionPipe
                when _dialect.SupportsGeometricTypes:
            {
                var tok = nextToken.Token;
                UnaryOperator op;
                switch (tok)
                {
                    case Token.Sharp:
                        op = new UnaryOperator.Hash();
                        break;
                    case Token.AtDashAt:
                        op = new UnaryOperator.AtDashAt();
                        break;
                    case Token.AtAt:
                        op = new UnaryOperator.DoubleAt();
                        break;
                    case Token.QuestionMarkDash:
                        op = new UnaryOperator.QuestionDash();
                        break;
                    case Token.QuestionPipe:
                        op = new UnaryOperator.QuestionPipe();
                        break;
                    default:
                        throw new ParserException(new ParserError.ParserErrorValue(
                            $"Unexpected token in unary operator parsing: {tok.ToDebugString()}"));
                }
                expr = new Expr.UnaryOp(
                    Op: op,
                    Expr: ParseSubexpr(_dialect.PrecValue(Precedence.PlusMinus)));
                break;
            }
            case Token.EscapedStringLiteral when dialect is PostgreSqlDialect or GenericDialect:
                PrevToken();
                expr = new Expr.Value(ParseValue());
                break;
            case Token.UnicodeStringLiteral:
                PrevToken();
                expr = new Expr.Value(ParseValue());
                break;
            case Token.Number
                or Token.SingleQuotedString
                or Token.DoubleQuotedString
                or Token.TripleSingleQuotedString
                or Token.TripleDoubleQuotedString
                or Token.DollarQuotedString
                or Token.SingleQuotedByteStringLiteral
                or Token.DoubleQuotedByteStringLiteral
                or Token.TripleSingleQuotedByteStringLiteral
                or Token.TripleDoubleQuotedByteStringLiteral
                or Token.SingleQuotedRawStringLiteral
                or Token.DoubleQuotedRawStringLiteral
                or Token.TripleSingleQuotedRawStringLiteral
                or Token.TripleDoubleQuotedRawStringLiteral
                or Token.NationalStringLiteral
                or Token.QuoteDelimitedStringLiteral
                or Token.NationalQuoteDelimitedStringLiteral
                or Token.HexStringLiteral:
                PrevToken();
                expr = new Expr.Value(ParseValue());
                break;
            case Token.LParen:
            {
                Expr parenExpr;
                if (TryParseExprSubQuery() is { } subQueryExpr)
                {
                    parenExpr = subQueryExpr;
                }
                else if (TryParseLambda() is { } lambda)
                {
                    return lambda;
                }
                else
                {
                    // Parentheses in expressions switch to "normal" parsing state.
                    // This matters for dialects (SQLite, DuckDB) where `NOT NULL` can
                    // be an alias for `IS NOT NULL`. In column definitions like:
                    //
                    //   CREATE TABLE t (c INT DEFAULT (42 NOT NULL) NOT NULL)
                    //
                    // The `(42 NOT NULL)` is an expression with parens, so it parses
                    // as `IsNotNull(42)`. The trailing `NOT NULL` is outside those
                    // expression parens (the outer parens are CREATE TABLE syntax),
                    // so it remains a column constraint.
                    var exprs = WithState(ParserState.Normal, p =>
                        p.ParseCommaSeparated(q => q.ParseExpr()));
                    switch (exprs.Count)
                    {
                        case 0:
                            throw new ParserException(new ParserError.ParserErrorValue(
                                "Internal parser error: parse_comma_separated returned empty list"));
                        case 1:
                            parenExpr = new Expr.Nested(exprs[0]);
                            break;
                        default:
                            parenExpr = new Expr.Tuple(exprs);
                            break;
                    }
                }
                ExpectToken(new Token.RParen());
                expr = parenExpr;
                break;
            }
            case Token.Placeholder or Token.Colon or Token.AtSign:
                PrevToken();
                expr = new Expr.Value(ParseValue());
                break;
            case Token.LBrace:
                PrevToken();
                expr = ParseLbraceExpr();
                break;
            default:
                return ExpectedAt<Expr>("an expression", nextTokenIndex);
        }

        return expr;
    }

    internal Expr ParseGeometricType(GeometricTypeKind kind)
    {
        return new Expr.TypedString(new TypedString(
            DataType: new DataType.GeometricType(kind),
            Value: ParseValue(),
            UsesOdbcSyntax: false));
    }

    /// <summary>
    /// Try to parse an <c>Expr::CompoundFieldAccess</c> like <c>a.b.c</c> or <c>a.b[1].c</c>.
    /// If all the fields are <c>Expr::Identifier</c>s, return an <c>Expr::CompoundIdentifier</c> instead.
    /// If only the root exists, return the root.
    /// Parses compound expressions which may be delimited by period
    /// or bracket notation.
    /// For example: <c>a.b.c</c>, <c>a.b[1]</c>.
    /// </summary>
    // `mut chain: Vec<AccessExpr>` (posse transferida e alterada) vira `List<AccessExpr>`
    // (docs/port/onda-b/B1.md).
    public Expr ParseCompoundExpr(Expr root, List<AccessExpr> chain)
    {
        TokenWithSpan? endingWildcard = null;
        while (true)
        {
            if (ConsumeToken(new Token.Period()))
            {
                var nextToken = PeekTokenRef();
                var stop = false;
                switch (nextToken.Token)
                {
                    case Token.Mul:
                        // Postgres explicitly allows funcnm(tablenm.*) and the
                        // function array_agg traverses this control flow
                        if (_dialect is PostgreSqlDialect)
                        {
                            endingWildcard = NextToken();
                        }
                        else
                        {
                            // Put back the consumed `.` tokens before exiting.
                            // If this expression is being parsed in the
                            // context of a projection, then the `.*` could imply
                            // a wildcard expansion. For example:
                            // `SELECT STRUCT('foo').* FROM T`
                            PrevToken(); // .
                        }

                        stop = true;
                        break;
                    case Token.SingleQuotedString { Value: var s }:
                    {
                        var expr = new Expr.Identifier(Ident.WithQuoteAndSpan('\'', nextToken.Span, s));
                        chain.Add(new AccessExpr.Dot(expr));
                        AdvanceToken(); // The consumed string
                        break;
                    }
                    case Token.Placeholder { Value: var s }:
                    {
                        // Snowflake uses $1, $2, etc. for positional column references
                        // in staged data queries like: SELECT t.$1 FROM @stage t
                        var expr = new Expr.Identifier(Ident.WithSpan(nextToken.Span, s));
                        chain.Add(new AccessExpr.Dot(expr));
                        AdvanceToken(); // The consumed placeholder
                        break;
                    }
                    // Parse a single field component, restricted to expression types valid
                    // after `.` (so e.g. `T.interval` is a compound identifier, not an
                    // interval expression). Using `parse_prefix` here rather than
                    // `parse_subexpr` avoids 2^N work on inputs like `IF a.b.c...x.#`:
                    // the outer loop already consumes successive `.field` segments, so a
                    // recursive `parse_subexpr` would re-walk the rest of the chain at
                    // every dot.
                    default:
                    {
                        // For a plain `Word` field (not followed by `(`), skip the
                        // speculative `parse_prefix`. The only result the validator
                        // below would accept is `Identifier`, which `parse_identifier`
                        // in the None branch produces directly. This avoids 2^N work
                        // on chains like `.not-b.not-b...` where `parse_prefix` would
                        // descend into `parse_not` and re-walk the remaining chain at
                        // every segment.
                        var wordFieldNoLparen =
                            PeekTokenRef().Token is Token.Word
                                && PeekNthTokenRef(1).Token is not Token.LParen;

                        Expr? expr;
                        if (wordFieldNoLparen)
                        {
                            expr = null;
                        }
                        else
                        {
                            expr = MaybeParse<Expr>(parser =>
                            {
                                var prefixExpr = parser.ParsePrefix();
                                switch (prefixExpr)
                                {
                                    case Expr.CompoundFieldAccess
                                        or Expr.CompoundIdentifier
                                        or Expr.Identifier
                                        or Expr.Value
                                        or Expr.Function:
                                        return prefixExpr;
                                    default:
                                        return parser.ExpectedRef<Expr>(
                                            "an identifier or value",
                                            parser.PeekTokenRef());
                                }
                            });
                        }

                        switch (expr)
                        {
                            // If we get back a compound field access or identifier,
                            // we flatten the nested expression.
                            // For example if the current root is `foo`
                            // and we get back a compound identifier expression `bar.baz`
                            // The full expression should be `foo.bar.baz` (i.e.
                            // a root with an access chain with 2 entries) and not
                            // `foo.(bar.baz)` (i.e. a root with an access chain with
                            // 1 entry`).
                            case Expr.CompoundFieldAccess { Root: var fieldRoot, AccessChain: var accessChain }:
                                chain.Add(new AccessExpr.Dot(fieldRoot));
                                chain.AddRange(accessChain);
                                break;
                            case Expr.CompoundIdentifier { Value: var parts }:
                                foreach (var part in parts)
                                {
                                    chain.Add(new AccessExpr.Dot(new Expr.Identifier(part)));
                                }
                                break;
                            case { } someExpr:
                                chain.Add(new AccessExpr.Dot(someExpr));
                                break;
                            // If the expression is not a valid suffix, fall back to
                            // parsing as an identifier. This handles cases like `T.interval`
                            // where `interval` is a keyword but should be treated as an identifier.
                            case null:
                                chain.Add(new AccessExpr.Dot(new Expr.Identifier(
                                    ParseIdentifier())));
                                break;
                        }
                        break;
                    }
                }
                if (stop)
                {
                    break;
                }
            }
            else if (!_dialect.SupportsPartiql
                && PeekTokenRef().Token is Token.LBracket)
            {
                ParseMultiDimSubscript(chain);
            }
            else
            {
                break;
            }
        }

        var tokIndex = GetCurrentIndex();
        if (endingWildcard is { } wildcardToken)
        {
            if (!IsAllIdent(root, chain))
            {
                return ExpectedRef<Expr>("an identifier or a '*' after '.'", PeekTokenRef());
            }
            return new Expr.QualifiedWildcard(
                Prefix: ObjectName.From(ExprsToIdents(root, chain)),
                Token: new AttachedToken(wildcardToken));
        }
        else if (MaybeParseOuterJoinOperator())
        {
            if (!IsAllIdent(root, chain))
            {
                return ExpectedAt<Expr>("column identifier before (+)", tokIndex);
            }
            var expr = chain.Count == 0
                ? root
                : new Expr.CompoundIdentifier(ExprsToIdents(root, chain));
            return new Expr.OuterJoin(expr);
        }
        else
        {
            return BuildCompoundExpr(root, chain);
        }
    }

    /// <summary>
    /// Combines a root expression and access chain to form
    /// a compound expression. Which may be a <c>Expr::CompoundFieldAccess</c>
    /// or other special cased expressions like <c>Expr::CompoundIdentifier</c>,
    /// <c>Expr::OuterJoin</c>.
    /// </summary>
    internal static Expr BuildCompoundExpr(Expr root, List<AccessExpr> accessChain)
    {
        if (accessChain.Count == 0)
        {
            return root;
        }

        if (IsAllIdent(root, accessChain))
        {
            return new Expr.CompoundIdentifier(ExprsToIdents(
                root,
                accessChain));
        }

        // Flatten qualified function calls.
        // For example, the expression `a.b.c.foo(1,2,3)` should
        // represent a function called `a.b.c.foo`, rather than
        // a composite expression.
        if (root is Expr.Identifier
            && accessChain[accessChain.Count - 1] is AccessExpr.Dot { Value: Expr.Function }
            && Enumerable.Reverse(accessChain)
                .Skip(1) // All except the Function
                .All(access => access is AccessExpr.Dot { Value: Expr.Identifier }))
        {
            // let Some(AccessExpr::Dot(Expr::Function(mut func))) = access_chain.pop() else { .. };
            var popped = accessChain[accessChain.Count - 1];
            accessChain.RemoveAt(accessChain.Count - 1);
            if (popped is not AccessExpr.Dot { Value: Expr.Function { Value: var func } })
            {
                throw ParserErr("expected function expression", root.Span().Start);
            }

            // [root].into_iter().chain(Dot(expr) da cadeia).flat_map(Identifier(ident))
            //     .map(ObjectNamePart::Identifier).chain(func.name.0)
            var exprs = new List<Expr> { root };
            foreach (var access in accessChain)
            {
                if (access is AccessExpr.Dot { Value: var dotExpr })
                {
                    exprs.Add(dotExpr);
                }
            }
            var compoundFuncName = new List<ObjectNamePart>();
            foreach (var e in exprs)
            {
                if (e is Expr.Identifier { Value: var ident })
                {
                    compoundFuncName.Add(new ObjectNamePart.Identifier(ident));
                }
            }
            compoundFuncName.AddRange(func.Name.Value);
            func = func with { Name = new ObjectName(compoundFuncName.ToSequence()) };

            return new Expr.Function(func);
        }

        // Flatten qualified outer join expressions.
        // For example, the expression `T.foo(+)` should
        // represent an outer join on the column name `T.foo`
        // rather than a composite expression.
        if (accessChain.Count == 1
            && accessChain[accessChain.Count - 1] is AccessExpr.Dot { Value: Expr.OuterJoin })
        {
            // let Some(AccessExpr::Dot(Expr::OuterJoin(inner_expr))) = access_chain.pop() else { .. };
            var popped = accessChain[accessChain.Count - 1];
            accessChain.RemoveAt(accessChain.Count - 1);
            if (popped is not AccessExpr.Dot { Value: Expr.OuterJoin { Value: var innerExpr } })
            {
                throw ParserErr("expected (+) expression", root.Span().Start);
            }

            if (!IsAllIdent(root, System.Array.Empty<AccessExpr>()))
            {
                throw ParserErr("column identifier before (+)", root.Span().Start);
            }

            var tokenStart = root.Span().Start;
            var idents = ExprsToIdents(root, new List<AccessExpr>()).ToList();
            switch (innerExpr)
            {
                case Expr.CompoundIdentifier { Value: var suffix }:
                    idents.AddRange(suffix);
                    break;
                case Expr.Identifier { Value: var suffix }:
                    idents.Add(suffix);
                    break;
                default:
                    throw ParserErr("column identifier before (+)", tokenStart);
            }

            return new Expr.OuterJoin(new Expr.CompoundIdentifier(idents.ToSequence()));
        }

        return new Expr.CompoundFieldAccess(
            Root: root,
            AccessChain: accessChain.ToSequence());
    }

    internal static ContextModifier? KeywordToModifier(Keyword k)
    {
        switch (k)
        {
            case Keyword.LOCAL:
                return new ContextModifier.Local();
            case Keyword.GLOBAL:
                return new ContextModifier.Global();
            case Keyword.SESSION:
                return new ContextModifier.Session();
            default:
                return null;
        }
    }

    /// <summary>Check if the root is an identifier and all fields are identifiers.</summary>
    internal static bool IsAllIdent(Expr root, IReadOnlyList<AccessExpr> fields)
    {
        if (root is not Expr.Identifier)
        {
            return false;
        }
        foreach (var x in fields)
        {
            if (x is not AccessExpr.Dot { Value: Expr.Identifier })
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Convert a root and a list of fields to a list of identifiers.</summary>
    internal static Sequence<Ident> ExprsToIdents(Expr root, IReadOnlyList<AccessExpr> fields)
    {
        var idents = new List<Ident>();
        if (root is Expr.Identifier { Value: var rootIdent })
        {
            idents.Add(rootIdent);
            foreach (var x in fields)
            {
                if (x is AccessExpr.Dot { Value: Expr.Identifier { Value: var ident } })
                {
                    idents.Add(ident);
                }
                else
                {
                    throw ParserErr(
                        $"Expected identifier, found: {x}",
                        x.Span().Start);
                }
            }
            return idents.ToSequence();
        }
        else
        {
            throw ParserErr(
                $"Expected identifier, found: {root}",
                root.Span().Start);
        }
    }

    /// <summary>Returns true if the next tokens indicate the outer join operator <c>(+)</c>.</summary>
    internal bool PeekOuterJoinOperator()
    {
        if (!_dialect.SupportsOuterJoinOperator)
        {
            return false;
        }

        var peeked = PeekTokensRef(3);
        var maybeLparen = peeked[0];
        var maybePlus = peeked[1];
        var maybeRparen = peeked[2];
        return maybeLparen.Token is Token.LParen
            && maybePlus.Token is Token.Plus
            && maybeRparen.Token is Token.RParen;
    }

    /// <summary>
    /// If the next tokens indicates the outer join operator <c>(+)</c>, consume
    /// the tokens and return true.
    /// </summary>
    internal bool MaybeParseOuterJoinOperator()
    {
        return _dialect.SupportsOuterJoinOperator
            && ConsumeTokens(new Token.LParen(), new Token.Plus(), new Token.RParen());
    }

    /// <summary>Parse utility options in the form of <c>(option1, option2 arg2, option3 arg3, ...)</c></summary>
    public Sequence<UtilityOption> ParseUtilityOptions()
    {
        ExpectToken(new Token.LParen());
        var options = ParseCommaSeparated(p => p.ParseUtilityOption());
        ExpectToken(new Token.RParen());

        return options;
    }

    internal UtilityOption ParseUtilityOption()
    {
        var name = ParseIdentifier();

        var nextToken = PeekTokenRef();
        if (nextToken.Equals(new Token.Comma()) || nextToken.Equals(new Token.RParen()))
        {
            return new UtilityOption(Name: name, Arg: null);
        }
        var arg = ParseExpr();

        return new UtilityOption(
            Name: name,
            Arg: arg);
    }

    internal Expr? TryParseExprSubQuery()
    {
        if (!PeekSubQuery())
        {
            return null;
        }

        return new Expr.Subquery(ParseQuery());
    }

    internal Expr? TryParseLambda()
    {
        if (!_dialect.SupportsLambdaFunctions)
        {
            return null;
        }
        return MaybeParse<Expr>(p =>
        {
            var @params = p.ParseCommaSeparated(q => q.ParseLambdaFunctionParameter());
            p.ExpectToken(new Token.RParen());
            p.ExpectToken(new Token.Arrow());
            var expr = p.ParseExpr();
            return new Expr.Lambda(new LambdaFunction(
                Params: new OneOrManyWithParens<LambdaFunctionParameter>.Many(@params),
                Body: expr,
                Syntax: LambdaSyntax.Arrow));
        });
    }

    /// <summary>Parses a lambda expression following the <c>LAMBDA</c> keyword syntax.</summary>
    /// <remarks>
    /// Syntax: <c>LAMBDA &lt;params&gt; : &lt;expr&gt;</c>
    /// <para>Examples: <c>LAMBDA x : x + 1</c>; <c>LAMBDA x, i : x &gt; i</c></para>
    /// <para>See <see href="https://duckdb.org/docs/stable/sql/functions/lambda"/></para>
    /// </remarks>
    internal Expr ParseLambdaExpr()
    {
        // Parse the parameters: either a single identifier or comma-separated identifiers
        var @params = ParseLambdaFunctionParameters();
        // Expect the colon separator
        ExpectToken(new Token.Colon());
        // Parse the body expression
        var body = ParseExpr();
        return new Expr.Lambda(new LambdaFunction(
            Params: @params,
            Body: body,
            Syntax: LambdaSyntax.LambdaKeyword));
    }

    /// <summary>Parses the parameters of a lambda function with optional typing.</summary>
    internal OneOrManyWithParens<LambdaFunctionParameter> ParseLambdaFunctionParameters()
    {
        // Parse the parameters: either a single identifier or comma-separated identifiers
        OneOrManyWithParens<LambdaFunctionParameter> @params;
        if (ConsumeToken(new Token.LParen()))
        {
            // Parenthesized parameters: (x, y)
            var parenthesized = ParseCommaSeparated(p => p.ParseLambdaFunctionParameter());
            ExpectToken(new Token.RParen());
            @params = new OneOrManyWithParens<LambdaFunctionParameter>.Many(parenthesized);
        }
        else
        {
            // Unparenthesized parameters: x or x, y
            var unparenthesized = ParseCommaSeparated(p => p.ParseLambdaFunctionParameter());
            if (unparenthesized.Count == 1)
            {
                @params = new OneOrManyWithParens<LambdaFunctionParameter>.One(unparenthesized[0]);
            }
            else
            {
                @params = new OneOrManyWithParens<LambdaFunctionParameter>.Many(unparenthesized);
            }
        }
        return @params;
    }

    /// <summary>Parses a single parameter of a lambda function, with optional typing.</summary>
    internal LambdaFunctionParameter ParseLambdaFunctionParameter()
    {
        var name = ParseIdentifier();
        DataType? dataType;
        switch (PeekTokenRef().Token)
        {
            case Token.Word:
                dataType = MaybeParse(p => p.ParseDataType());
                break;
            default:
                dataType = null;
                break;
        }
        return new LambdaFunctionParameter(Name: name, DataType: dataType);
    }

    /// <summary>
    /// Tries to parse the body of an [ODBC escaping sequence]
    /// i.e. without the enclosing braces
    /// Currently implemented:
    /// Scalar Function Calls
    /// Date, Time, and Timestamp Literals
    /// See <see href="https://learn.microsoft.com/en-us/sql/odbc/reference/develop-app/escape-sequences-in-odbc?view=sql-server-2017"/>
    /// </summary>
    internal Expr? MaybeParseOdbcBody()
    {
        // Attempt 1: Try to parse it as a function.
        if (MaybeParseOdbcFnBody() is { } expr)
        {
            return expr;
        }
        // Attempt 2: Try to parse it as a Date, Time or Timestamp Literal
        return MaybeParseOdbcBodyDatetime();
    }

    /// <summary>Tries to parse the body of an [ODBC Date, Time, and Timestamp Literals] call.</summary>
    /// <remarks>
    /// <code>
    /// {d '2025-07-17'}
    /// {t '14:12:01'}
    /// {ts '2025-07-17 14:12:01'}
    /// </code>
    /// [ODBC Date, Time, and Timestamp Literals]:
    /// <see href="https://learn.microsoft.com/en-us/sql/odbc/reference/develop-app/date-time-and-timestamp-literals?view=sql-server-2017"/>
    /// </remarks>
    internal Expr? MaybeParseOdbcBodyDatetime()
    {
        return MaybeParse<Expr>(p =>
        {
            var token = p.NextToken();
            var wordString = token.Token.ToString();
            DataType dataType;
            switch (wordString)
            {
                case "t":
                    dataType = new DataType.Time(null, new TimezoneInfo.None());
                    break;
                case "d":
                    dataType = new DataType.Date();
                    break;
                case "ts":
                    dataType = new DataType.Timestamp(null, new TimezoneInfo.None());
                    break;
                default:
                    return p.Expected<Expr>("ODBC datetime keyword (t, d, or ts)", token);
            }
            var value = p.ParseValue();
            return new Expr.TypedString(new TypedString(
                DataType: dataType,
                Value: value,
                UsesOdbcSyntax: true));
        });
    }

    /// <summary>
    /// Tries to parse the body of an [ODBC function] call.
    /// i.e. without the enclosing braces
    /// </summary>
    /// <remarks>
    /// <code>
    /// fn myfunc(1,2,3)
    /// </code>
    /// [ODBC function]: <see href="https://learn.microsoft.com/en-us/sql/odbc/reference/develop-app/scalar-function-calls?view=sql-server-2017"/>
    /// </remarks>
    internal Expr? MaybeParseOdbcFnBody()
    {
        return MaybeParse<Expr>(p =>
        {
            p.ExpectKeyword(Keyword.FN);
            var fnName = p.ParseObjectName(false);
            var fnCall = p.ParseFunctionCall(fnName);
            fnCall = fnCall with { UsesOdbcSyntax = true };
            return new Expr.Function(fnCall);
        });
    }

    /// <summary>
    /// Parse the argument list of <c>XMLPARSE({ DOCUMENT | CONTENT } value)</c>,
    /// including the closing parenthesis. The mode word becomes the name of the
    /// single argument, with no operator between it and the value.
    /// </summary>
    internal FunctionArgumentList ParseXmlparseArgumentList()
    {
        var arg = new FunctionArg.Named(
            Name: ParseIdentifier(),
            Arg: new FunctionArgExpr.Expr(ParseExpr()),
            Operator: new FunctionArgOperator.Space());
        ExpectToken(new Token.RParen());
        return new FunctionArgumentList(
            DuplicateTreatment: null,
            Args: Sequence.Of<FunctionArg>(arg),
            Clauses: Sequence<FunctionArgumentClause>.Empty);
    }
}
