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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 2537–3523:
// parse_function … parse_struct_literal).
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse a function call expression named by <c>name</c> and return it as an <c>Expr</c>.</summary>
    public Expr ParseFunction(ObjectName name)
    {
        return new Expr.Function(ParseFunctionCall(name));
    }

    internal Function ParseFunctionCall(ObjectName name)
    {
        ExpectToken(new Token.LParen());

        // Snowflake permits a subquery to be passed as an argument without
        // an enclosing set of parens if it's the only argument.
        if (_dialect.SupportsSubqueryAsFunctionArg && PeekSubQuery())
        {
            var subquery = ParseQuery();
            ExpectToken(new Token.RParen());
            return new Function(
                Name: name,
                UsesOdbcSyntax: false,
                Parameters: new FunctionArguments.None(),
                Args: new FunctionArguments.Subquery(subquery),
                Filter: null,
                NullTreatment: null,
                Over: null,
                WithinGroup: Sequence<OrderByExpr>.Empty);
        }

        FunctionArgumentList args;
        if (_dialect.SupportsXmlExpressions
            && IsSimpleUnquotedObjectName(name, "xmlparse"))
        {
            args = ParseXmlparseArgumentList();
        }
        else
        {
            args = ParseFunctionArgumentList();
        }
        FunctionArguments parameters = new FunctionArguments.None();
        // ClickHouse aggregations support parametric functions like `HISTOGRAM(0.5, 0.6)(x, y)`
        // which (0.5, 0.6) is a parameter to the function.
        if ((_dialect is ClickHouseDialect or GenericDialect)
            && ConsumeToken(new Token.LParen()))
        {
            parameters = new FunctionArguments.List(args);
            args = ParseFunctionArgumentList();
        }

        Sequence<OrderByExpr> withinGroup;
        if (ParseKeywords(Keyword.WITHIN, Keyword.GROUP))
        {
            ExpectToken(new Token.LParen());
            ExpectKeywords(Keyword.ORDER, Keyword.BY);
            var orderBy = ParseCommaSeparated(p => p.ParseOrderByExpr());
            ExpectToken(new Token.RParen());
            withinGroup = orderBy;
        }
        else
        {
            withinGroup = Sequence<OrderByExpr>.Empty;
        }

        Expr? filter;
        if (_dialect.SupportsFilterDuringAggregation
            && ParseKeyword(Keyword.FILTER)
            && ConsumeToken(new Token.LParen())
            && ParseKeyword(Keyword.WHERE))
        {
            var filterExpr = ParseExpr();
            ExpectToken(new Token.RParen());
            filter = filterExpr;
        }
        else
        {
            filter = null;
        }

        // Syntax for null treatment shows up either in the args list
        // or after the function call, but not both.
        NullTreatment? nullTreatment;
        if (args.Clauses.All(clause => clause is not FunctionArgumentClause.IgnoreOrRespectNulls))
        {
            nullTreatment = ParseNullTreatment();
        }
        else
        {
            nullTreatment = null;
        }

        WindowType? over;
        if (ParseKeyword(Keyword.OVER))
        {
            if (ConsumeToken(new Token.LParen()))
            {
                var windowSpec = ParseWindowSpec();
                over = new WindowType.WindowSpec(windowSpec);
            }
            else
            {
                over = new WindowType.NamedWindow(ParseIdentifier());
            }
        }
        else
        {
            over = null;
        }

        return new Function(
            Name: name,
            UsesOdbcSyntax: false,
            Parameters: parameters,
            Args: new FunctionArguments.List(args),
            NullTreatment: nullTreatment,
            Filter: filter,
            Over: over,
            WithinGroup: withinGroup);
    }

    /// <summary>Optionally parses a null treatment clause.</summary>
    internal NullTreatment? ParseNullTreatment()
    {
        switch (ParseOneOfKeywords(Keyword.RESPECT, Keyword.IGNORE))
        {
            case { } keyword:
                ExpectKeywordIs(Keyword.NULLS);

                switch (keyword)
                {
                    case Keyword.RESPECT:
                        return new NullTreatment.RespectNulls();
                    case Keyword.IGNORE:
                        return new NullTreatment.IgnoreNulls();
                    default:
                        return null;
                }
            case null:
                return null;
        }
    }

    /// <summary>Parse time-related function <c>name</c> possibly followed by <c>(...)</c> arguments.</summary>
    public Expr ParseTimeFunctions(ObjectName name)
    {
        FunctionArguments args;
        if (ConsumeToken(new Token.LParen()))
        {
            args = new FunctionArguments.List(ParseFunctionArgumentList());
        }
        else
        {
            args = new FunctionArguments.None();
        }
        return new Expr.Function(new Function(
            Name: name,
            UsesOdbcSyntax: false,
            Parameters: new FunctionArguments.None(),
            Args: args,
            Filter: null,
            Over: null,
            NullTreatment: null,
            WithinGroup: Sequence<OrderByExpr>.Empty));
    }

    /// <summary>Parse window frame <c>UNITS</c> clause: <c>ROWS</c>, <c>RANGE</c>, or <c>GROUPS</c>.</summary>
    public WindowFrameUnits ParseWindowFrameUnits()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.ROWS:
                        return new WindowFrameUnits.Rows();
                    case Keyword.RANGE:
                        return new WindowFrameUnits.Range();
                    case Keyword.GROUPS:
                        return new WindowFrameUnits.Groups();
                    default:
                        return Expected<WindowFrameUnits>("ROWS, RANGE, GROUPS", nextToken);
                }
            default:
                return Expected<WindowFrameUnits>("ROWS, RANGE, GROUPS", nextToken);
        }
    }

    /// <summary>Parse a <c>WINDOW</c> frame definition (units and bounds).</summary>
    public WindowFrame ParseWindowFrame()
    {
        var units = ParseWindowFrameUnits();
        WindowFrameBound startBound;
        WindowFrameBound? endBound;
        if (ParseKeyword(Keyword.BETWEEN))
        {
            var start = ParseWindowFrameBound();
            ExpectKeywordIs(Keyword.AND);
            var end = ParseWindowFrameBound();
            (startBound, endBound) = (start, end);
        }
        else
        {
            (startBound, endBound) = (ParseWindowFrameBound(), null);
        }
        return new WindowFrame(
            Units: units,
            StartBound: startBound,
            EndBound: endBound);
    }

    /// <summary>Parse a window frame bound: <c>CURRENT ROW</c> or <c>&lt;n&gt; PRECEDING|FOLLOWING</c>.</summary>
    public WindowFrameBound ParseWindowFrameBound()
    {
        if (ParseKeywords(Keyword.CURRENT, Keyword.ROW))
        {
            return new WindowFrameBound.CurrentRow();
        }
        else
        {
            Expr? rows;
            if (ParseKeyword(Keyword.UNBOUNDED))
            {
                rows = null;
            }
            else
            {
                switch (PeekTokenRef().Token)
                {
                    case Token.SingleQuotedString:
                        rows = ParseInterval();
                        break;
                    default:
                        rows = ParseExpr();
                        break;
                }
            }
            if (ParseKeyword(Keyword.PRECEDING))
            {
                return new WindowFrameBound.Preceding(rows);
            }
            else if (ParseKeyword(Keyword.FOLLOWING))
            {
                return new WindowFrameBound.Following(rows);
            }
            else
            {
                return ExpectedRef<WindowFrameBound>("PRECEDING or FOLLOWING", PeekTokenRef());
            }
        }
    }

    /// <summary>Parse a group by expr. Group by expr can be one of group sets, roll up, cube, or simple expr.</summary>
    internal Expr ParseGroupByExpr()
    {
        if (_dialect.SupportsGroupByExpr)
        {
            if (ParseKeywords(Keyword.GROUPING, Keyword.SETS))
            {
                ExpectToken(new Token.LParen());
                var result = ParseCommaSeparated(p => p.ParseTuple(true, true));
                ExpectToken(new Token.RParen());
                return new Expr.GroupingSets(result);
            }
            else if (ParseKeyword(Keyword.CUBE))
            {
                ExpectToken(new Token.LParen());
                var result = ParseCommaSeparated(p => p.ParseTuple(true, true));
                ExpectToken(new Token.RParen());
                return new Expr.Cube(result);
            }
            else if (ParseKeyword(Keyword.ROLLUP))
            {
                ExpectToken(new Token.LParen());
                var result = ParseCommaSeparated(p => p.ParseTuple(true, true));
                ExpectToken(new Token.RParen());
                return new Expr.Rollup(result);
            }
            else if (ConsumeTokens(new Token.LParen(), new Token.RParen()))
            {
                // PostgreSQL allow to use empty tuple as a group by expression,
                // e.g. `GROUP BY (), name`. Please refer to GROUP BY Clause section in
                // [PostgreSQL](https://www.postgresql.org/docs/16/sql-select.html)
                return new Expr.Tuple(Sequence<Expr>.Empty);
            }
            else
            {
                return ParseExpr();
            }
        }
        else
        {
            // TODO parse rollup for other dialects
            return ParseExpr();
        }
    }

    /// <summary>
    /// Parse a tuple with <c>(</c> and <c>)</c>.
    /// If <c>lift_singleton</c> is true, then a singleton tuple is lifted to a tuple of length 1, otherwise it will fail.
    /// If <c>allow_empty</c> is true, then an empty tuple is allowed.
    /// </summary>
    internal Sequence<Expr> ParseTuple(bool liftSingleton, bool allowEmpty)
    {
        if (liftSingleton)
        {
            if (ConsumeToken(new Token.LParen()))
            {
                Sequence<Expr> result;
                if (allowEmpty && ConsumeToken(new Token.RParen()))
                {
                    result = Sequence<Expr>.Empty;
                }
                else
                {
                    var exprs = ParseCommaSeparated(p => p.ParseExpr());
                    ExpectToken(new Token.RParen());
                    result = exprs;
                }
                return result;
            }
            else
            {
                return Sequence.Of(ParseExpr());
            }
        }
        else
        {
            ExpectToken(new Token.LParen());
            Sequence<Expr> result;
            if (allowEmpty && ConsumeToken(new Token.RParen()))
            {
                result = Sequence<Expr>.Empty;
            }
            else
            {
                var exprs = ParseCommaSeparated(p => p.ParseExpr());
                ExpectToken(new Token.RParen());
                result = exprs;
            }
            return result;
        }
    }

    /// <summary>Parse a <c>CASE</c> expression and return an <c>Expr::Case</c>.</summary>
    public Expr ParseCaseExpr()
    {
        var caseToken = new AttachedToken(GetCurrentToken());
        Expr? operand = null;
        if (!ParseKeyword(Keyword.WHEN))
        {
            operand = ParseExpr();
            ExpectKeywordIs(Keyword.WHEN);
        }
        var conditions = new List<CaseWhen>();
        while (true)
        {
            var condition = ParseExpr();
            ExpectKeywordIs(Keyword.THEN);
            var result = ParseExpr();
            conditions.Add(new CaseWhen(Condition: condition, Result: result));
            if (!ParseKeyword(Keyword.WHEN))
            {
                break;
            }
        }
        Expr? elseResult;
        if (ParseKeyword(Keyword.ELSE))
        {
            elseResult = ParseExpr();
        }
        else
        {
            elseResult = null;
        }
        var endToken = new AttachedToken(ExpectKeyword(Keyword.END));
        return new Expr.Case(
            CaseToken: caseToken,
            EndToken: endToken,
            Operand: operand,
            Conditions: conditions.ToSequence(),
            ElseResult: elseResult);
    }

    /// <summary>Parse an optional <c>FORMAT</c> clause for <c>CAST</c> expressions.</summary>
    public CastFormat? ParseOptionalCastFormat()
    {
        if (ParseKeyword(Keyword.FORMAT))
        {
            var value = ParseValue();
            switch (ParseOptionalTimeZone())
            {
                case { } tz:
                    return new CastFormat.ValueAtTimeZone(value, tz);
                case null:
                    return new CastFormat.Value(value);
            }
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse an optional <c>AT TIME ZONE</c> clause.</summary>
    public ValueWithSpan? ParseOptionalTimeZone()
    {
        if (ParseKeywords(Keyword.AT, Keyword.TIME, Keyword.ZONE))
        {
            return ParseValue();
        }
        else
        {
            return null;
        }
    }

    /// <summary>mssql-like convert function</summary>
    internal Expr ParseMssqlConvert(bool isTry)
    {
        ExpectToken(new Token.LParen());
        var dataType = ParseDataType();
        ExpectToken(new Token.Comma());
        var expr = ParseExpr();
        Sequence<Expr> styles;
        if (ConsumeToken(new Token.Comma()))
        {
            styles = ParseCommaSeparated(p => p.ParseExpr());
        }
        else
        {
            styles = Sequence<Expr>.Empty; // Default::default()
        }
        ExpectToken(new Token.RParen());
        return new Expr.Convert(
            IsTry: isTry,
            Expr: expr,
            DataType: dataType,
            Charset: null,
            TargetBeforeValue: true,
            Styles: styles);
    }

    /// <summary>
    /// Parse a SQL CONVERT function:
    /// <c>CONVERT('héhé' USING utf8mb4)</c> (MySQL);
    /// <c>CONVERT('héhé', CHAR CHARACTER SET utf8mb4)</c> (MySQL);
    /// <c>CONVERT(DECIMAL(10, 5), 42)</c> (MSSQL) - the type comes first
    /// </summary>
    public Expr ParseConvertExpr(bool isTry)
    {
        if (_dialect.ConvertTypeBeforeValue)
        {
            return ParseMssqlConvert(isTry);
        }
        ExpectToken(new Token.LParen());
        var expr = ParseExpr();
        if (ParseKeyword(Keyword.USING))
        {
            var usingCharset = ParseObjectName(false);
            ExpectToken(new Token.RParen());
            return new Expr.Convert(
                IsTry: isTry,
                Expr: expr,
                DataType: null,
                Charset: usingCharset,
                TargetBeforeValue: false,
                Styles: Sequence<Expr>.Empty);
        }
        ExpectToken(new Token.Comma());
        var dataType = ParseDataType();
        ObjectName? charset;
        if (ParseKeywords(Keyword.CHARACTER, Keyword.SET))
        {
            charset = ParseObjectName(false);
        }
        else
        {
            charset = null;
        }
        ExpectToken(new Token.RParen());
        return new Expr.Convert(
            IsTry: isTry,
            Expr: expr,
            DataType: dataType,
            Charset: charset,
            TargetBeforeValue: false,
            Styles: Sequence<Expr>.Empty);
    }

    /// <summary>Parse a SQL CAST function e.g. <c>CAST(expr AS FLOAT)</c></summary>
    public Expr ParseCastExpr(CastKind kind)
    {
        ExpectToken(new Token.LParen());
        var expr = ParseExpr();
        ExpectKeywordIs(Keyword.AS);
        var dataType = _dialect.SupportsCastEmptyDataTypeToUnspecified
            && PeekTokenRef().Token is Token.RParen
            ? new DataType.Unspecified()
            : ParseDataType();
        var format = ParseOptionalCastFormat();
        ExpectToken(new Token.RParen());
        return new Expr.Cast(
            Kind: kind,
            Expr: expr,
            DataType: dataType,
            Format: format);
    }

    /// <summary>Parse a SQL EXISTS expression e.g. <c>WHERE EXISTS(SELECT ...)</c>.</summary>
    public Expr ParseExistsExpr(bool negated)
    {
        ExpectToken(new Token.LParen());
        var existsNode = new Expr.Exists(
            Negated: negated,
            Subquery: ParseQuery());
        ExpectToken(new Token.RParen());
        return existsNode;
    }

    /// <summary>Parse a SQL <c>EXTRACT</c> expression e.g. <c>EXTRACT(YEAR FROM date)</c>.</summary>
    public Expr ParseExtractExpr()
    {
        ExpectToken(new Token.LParen());
        var field = ParseDateTimeField();

        ExtractSyntax syntax;
        if (ParseKeyword(Keyword.FROM))
        {
            syntax = ExtractSyntax.From;
        }
        else if (_dialect.SupportsExtractCommaSyntax && ConsumeToken(new Token.Comma()))
        {
            syntax = ExtractSyntax.Comma;
        }
        else
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                "Expected 'FROM' or ','"));
        }

        var expr = ParseExpr();
        ExpectToken(new Token.RParen());
        return new Expr.Extract(
            Field: field,
            Expr: expr,
            Syntax: syntax);
    }

    /// <summary>Parse a <c>CEIL</c> or <c>FLOOR</c> expression.</summary>
    public Expr ParseCeilFloorExpr(bool isCeil)
    {
        ExpectToken(new Token.LParen());
        var expr = ParseExpr();
        // Parse `CEIL/FLOOR(expr)`
        CeilFloorKind field;
        if (ParseKeyword(Keyword.TO))
        {
            // Parse `CEIL/FLOOR(expr TO DateTimeField)`
            field = new CeilFloorKind.DateTimeField(ParseDateTimeField());
        }
        else if (ConsumeToken(new Token.Comma()))
        {
            // Parse `CEIL/FLOOR(expr, scale)`
            var v = ParseValue();
            if (v.Value is Value.Number)
            {
                field = new CeilFloorKind.Scale(v);
            }
            else
            {
                throw new ParserException(new ParserError.ParserErrorValue(
                    "Scale field can only be of number type"));
            }
        }
        else
        {
            field = new CeilFloorKind.DateTimeField(new DateTimeField.NoDateTime());
        }
        ExpectToken(new Token.RParen());
        if (isCeil)
        {
            return new Expr.Ceil(
                Expr: expr,
                Field: field);
        }
        else
        {
            return new Expr.Floor(
                Expr: expr,
                Field: field);
        }
    }

    /// <summary>Parse a <c>POSITION</c> expression.</summary>
    public Expr ParsePositionExpr(Ident ident)
    {
        var betweenPrec = _dialect.PrecValue(Precedence.Between);
        // Memoize a failed `IN` form: the function-call fallback re-parses the
        // same argument, so nested `POSITION(POSITION(...))` would take 2^N.
        var memoKey = (_index, _state);
        if (_failedPositionSyntaxPositions.Contains(memoKey))
        {
            return ParseFunction(ObjectName.From(Sequence.Of(ident)));
        }
        var positionExpr = MaybeParse<Expr>(p =>
        {
            // PARSE SELECT POSITION('@' in field)
            p.ExpectToken(new Token.LParen());

            // Parse the subexpr till the IN keyword
            var expr = p.ParseSubexpr(betweenPrec);
            p.ExpectKeywordIs(Keyword.IN);
            var from = p.ParseExpr();
            p.ExpectToken(new Token.RParen());
            return new Expr.Position(
                Expr: expr,
                In: from);
        });
        switch (positionExpr)
        {
            case { } expr:
                return expr;
            // Snowflake supports `position` as an ordinary function call
            // without the special `IN` syntax.
            case null:
                _failedPositionSyntaxPositions.Add(memoKey);
                return ParseFunction(ObjectName.From(Sequence.Of(ident)));
        }
    }

    /// <summary>
    /// Parse <c>SUBSTRING</c>/<c>SUBSTR</c> expressions: <c>SUBSTRING(expr FROM start FOR length)</c> or <c>SUBSTR(expr, start, length)</c>.
    /// </summary>
    public Expr ParseSubstring()
    {
        bool shorthand;
        switch (ExpectOneOfKeywords(Keyword.SUBSTR, Keyword.SUBSTRING))
        {
            case Keyword.SUBSTR:
                shorthand = true;
                break;
            case Keyword.SUBSTRING:
                shorthand = false;
                break;
            default:
                PrevToken();
                return ExpectedRef<Expr>("SUBSTR or SUBSTRING", PeekTokenRef());
        }
        ExpectToken(new Token.LParen());
        var expr = ParseExpr();
        Expr? fromExpr = null;
        var special = ConsumeToken(new Token.Comma());
        if (special || ParseKeyword(Keyword.FROM))
        {
            fromExpr = ParseExpr();
        }

        Expr? toExpr = null;
        if (ParseKeyword(Keyword.FOR) || ConsumeToken(new Token.Comma()))
        {
            toExpr = ParseExpr();
        }
        ExpectToken(new Token.RParen());

        return new Expr.Substring(
            Expr: expr,
            SubstringFrom: fromExpr,
            SubstringFor: toExpr,
            Special: special,
            Shorthand: shorthand);
    }

    /// <summary>Parse an OVERLAY expression.</summary>
    /// <remarks>See <c>Expr::Overlay</c></remarks>
    public Expr ParseOverlayExpr()
    {
        // PARSE OVERLAY (EXPR PLACING EXPR FROM 1 [FOR 3])
        ExpectToken(new Token.LParen());
        var expr = ParseExpr();
        ExpectKeywordIs(Keyword.PLACING);
        var whatExpr = ParseExpr();
        ExpectKeywordIs(Keyword.FROM);
        var fromExpr = ParseExpr();
        Expr? forExpr = null;
        if (ParseKeyword(Keyword.FOR))
        {
            forExpr = ParseExpr();
        }
        ExpectToken(new Token.RParen());

        return new Expr.Overlay(
            Expr: expr,
            OverlayWhat: whatExpr,
            OverlayFrom: fromExpr,
            OverlayFor: forExpr);
    }

    // Rust: o literal `[Keyword::BOTH, Keyword::LEADING, Keyword::TRAILING]` de `parse_trim_expr` (abaixo),
    // em cache para não alocar a cada `TRIM(` (convencoes.md §13, caso 11).
    private static readonly Keyword[] ParseTrimExprTrimWhereKeywords =
        { Keyword.BOTH, Keyword.LEADING, Keyword.TRAILING };

    /// <summary>
    /// <code>
    /// TRIM ([WHERE] ['text' FROM] 'text')
    /// TRIM ('text')
    /// TRIM(&lt;expr&gt;, [, characters]) -- PostgreSQL, DuckDB, Snowflake, BigQuery, Generic
    /// </code>
    /// </summary>
    public Expr ParseTrimExpr()
    {
        ExpectToken(new Token.LParen());
        TrimWhereField? trimWhere = null;
        if (PeekTokenRef().Token is Token.Word { Value: var word })
        {
            if (ParseTrimExprTrimWhereKeywords.Contains(word.Keyword))
            {
                trimWhere = ParseTrimWhere();
            }
        }
        if (_dialect.SupportsCommaSeparatedTrim && ParseKeyword(Keyword.FROM))
        {
            var fromExpr = ParseExpr();
            Sequence<Expr>? trimCharacters = ConsumeToken(new Token.Comma())
                ? ParseCommaSeparated(p => p.ParseExpr())
                : null;
            ExpectToken(new Token.RParen());
            return new Expr.Trim(
                Expr: fromExpr,
                TrimWhere: trimWhere,
                TrimWhat: null,
                TrimCharacters: trimCharacters);
        }
        var expr = ParseExpr();
        if (ParseKeyword(Keyword.FROM))
        {
            var trimWhat = expr;
            var fromExpr = ParseExpr();
            ExpectToken(new Token.RParen());
            return new Expr.Trim(
                Expr: fromExpr,
                TrimWhere: trimWhere,
                TrimWhat: trimWhat,
                TrimCharacters: null);
        }
        else if (_dialect.SupportsCommaSeparatedTrim && ConsumeToken(new Token.Comma()))
        {
            var characters = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            return new Expr.Trim(
                Expr: expr,
                TrimWhere: trimWhere,
                TrimWhat: null,
                TrimCharacters: characters);
        }
        else
        {
            ExpectToken(new Token.RParen());
            return new Expr.Trim(
                Expr: expr,
                TrimWhere: trimWhere,
                TrimWhat: null,
                TrimCharacters: null);
        }
    }

    /// <summary>Parse the <c>WHERE</c> field for a <c>TRIM</c> expression.</summary>
    /// <remarks>See <c>TrimWhereField</c></remarks>
    public TrimWhereField ParseTrimWhere()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.BOTH:
                        return new TrimWhereField.Both();
                    case Keyword.LEADING:
                        return new TrimWhereField.Leading();
                    case Keyword.TRAILING:
                        return new TrimWhereField.Trailing();
                    default:
                        return Expected<TrimWhereField>("trim_where field", nextToken);
                }
            default:
                return Expected<TrimWhereField>("trim_where field", nextToken);
        }
    }

    /// <summary>
    /// Parses an array expression <c>[ex1, ex2, ..]</c>
    /// if <c>named</c> is <c>true</c>, came from an expression like  <c>ARRAY[ex1, ex2]</c>
    /// </summary>
    public Expr ParseArrayExpr(bool named)
    {
        var exprs = ParseCommaSeparated0(p => p.ParseExpr(), new Token.RBracket());
        ExpectToken(new Token.RBracket());
        return new Expr.Array(new Ast.Array(Elem: exprs, Named: named));
    }

    /// <summary>Parse the <c>ON OVERFLOW</c> clause for <c>LISTAGG</c>.</summary>
    /// <remarks>See <c>ListAggOnOverflow</c></remarks>
    public ListAggOnOverflow? ParseListaggOnOverflow()
    {
        if (ParseKeywords(Keyword.ON, Keyword.OVERFLOW))
        {
            if (ParseKeyword(Keyword.ERROR))
            {
                return new ListAggOnOverflow.Error();
            }
            else
            {
                ExpectKeywordIs(Keyword.TRUNCATE);
                Expr? filler;
                switch (PeekTokenRef().Token)
                {
                    case Token.Word { Value: var w }
                        when w.Keyword == Keyword.WITH || w.Keyword == Keyword.WITHOUT:
                        filler = null;
                        break;
                    case Token.SingleQuotedString
                        or Token.EscapedStringLiteral
                        or Token.UnicodeStringLiteral
                        or Token.NationalStringLiteral
                        or Token.QuoteDelimitedStringLiteral
                        or Token.NationalQuoteDelimitedStringLiteral
                        or Token.HexStringLiteral:
                        filler = ParseExpr();
                        break;
                    default:
                        return ExpectedRef<ListAggOnOverflow?>(
                            "either filler, WITH, or WITHOUT in LISTAGG",
                            PeekTokenRef());
                }
                var withCount = ParseKeyword(Keyword.WITH);
                if (!withCount && !ParseKeyword(Keyword.WITHOUT))
                {
                    ExpectedRef("either WITH or WITHOUT in LISTAGG", PeekTokenRef());
                }
                ExpectKeywordIs(Keyword.COUNT);
                return new ListAggOnOverflow.Truncate(Filler: filler, WithCount: withCount);
            }
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse a date/time field for <c>EXTRACT</c>, interval qualifiers, and ceil/floor operations.</summary>
    /// <remarks>
    /// <c>EXTRACT</c> supports a wider set of date/time fields than interval qualifiers,
    /// so this function may need to be split in two.
    /// See <c>DateTimeField</c>
    /// </remarks>
    public DateTimeField ParseDateTimeField()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.YEAR:
                        return new DateTimeField.Year();
                    case Keyword.YEARS:
                        return new DateTimeField.Years();
                    case Keyword.MONTH:
                        return new DateTimeField.Month();
                    case Keyword.MONTHS:
                        return new DateTimeField.Months();
                    case Keyword.WEEK:
                    {
                        Ident? weekDay;
                        if ((_dialect is BigQueryDialect or GenericDialect)
                            && ConsumeToken(new Token.LParen()))
                        {
                            var day = ParseIdentifier();
                            ExpectToken(new Token.RParen());
                            weekDay = day;
                        }
                        else
                        {
                            weekDay = null;
                        }
                        return new DateTimeField.Week(weekDay);
                    }
                    case Keyword.WEEKS:
                        return new DateTimeField.Weeks();
                    case Keyword.DAY:
                        return new DateTimeField.Day();
                    case Keyword.DAYOFWEEK:
                        return new DateTimeField.DayOfWeek();
                    case Keyword.DAYOFYEAR:
                        return new DateTimeField.DayOfYear();
                    case Keyword.DAYS:
                        return new DateTimeField.Days();
                    case Keyword.DATE:
                        return new DateTimeField.Date();
                    case Keyword.DATETIME:
                        return new DateTimeField.Datetime();
                    case Keyword.HOUR:
                        return new DateTimeField.Hour();
                    case Keyword.HOURS:
                        return new DateTimeField.Hours();
                    case Keyword.MINUTE:
                        return new DateTimeField.Minute();
                    case Keyword.MINUTES:
                        return new DateTimeField.Minutes();
                    case Keyword.SECOND:
                        return new DateTimeField.Second();
                    case Keyword.SECONDS:
                        return new DateTimeField.Seconds();
                    case Keyword.CENTURY:
                        return new DateTimeField.Century();
                    case Keyword.DECADE:
                        return new DateTimeField.Decade();
                    case Keyword.DOY:
                        return new DateTimeField.Doy();
                    case Keyword.DOW:
                        return new DateTimeField.Dow();
                    case Keyword.EPOCH:
                        return new DateTimeField.Epoch();
                    case Keyword.ISODOW:
                        return new DateTimeField.Isodow();
                    case Keyword.ISOYEAR:
                        return new DateTimeField.Isoyear();
                    case Keyword.ISOWEEK:
                        return new DateTimeField.IsoWeek();
                    case Keyword.JULIAN:
                        return new DateTimeField.Julian();
                    case Keyword.MICROSECOND:
                        return new DateTimeField.Microsecond();
                    case Keyword.MICROSECONDS:
                        return new DateTimeField.Microseconds();
                    case Keyword.MILLENIUM:
                        return new DateTimeField.Millenium();
                    case Keyword.MILLENNIUM:
                        return new DateTimeField.Millennium();
                    case Keyword.MILLISECOND:
                        return new DateTimeField.Millisecond();
                    case Keyword.MILLISECONDS:
                        return new DateTimeField.Milliseconds();
                    case Keyword.NANOSECOND:
                        return new DateTimeField.Nanosecond();
                    case Keyword.NANOSECONDS:
                        return new DateTimeField.Nanoseconds();
                    case Keyword.QUARTER:
                        return new DateTimeField.Quarter();
                    case Keyword.TIME:
                        return new DateTimeField.Time();
                    case Keyword.TIMEZONE:
                        return new DateTimeField.Timezone();
                    case Keyword.TIMEZONE_ABBR:
                        return new DateTimeField.TimezoneAbbr();
                    case Keyword.TIMEZONE_HOUR:
                        return new DateTimeField.TimezoneHour();
                    case Keyword.TIMEZONE_MINUTE:
                        return new DateTimeField.TimezoneMinute();
                    case Keyword.TIMEZONE_REGION:
                        return new DateTimeField.TimezoneRegion();
                    case var _ when _dialect.AllowExtractCustom:
                    {
                        PrevToken();
                        var custom = ParseIdentifier();
                        return new DateTimeField.Custom(custom);
                    }
                    default:
                        return Expected<DateTimeField>("date/time field", nextToken);
                }
            case Token.SingleQuotedString when _dialect.AllowExtractSingleQuotes:
            {
                PrevToken();
                var custom = ParseIdentifier();
                return new DateTimeField.Custom(custom);
            }
            default:
                return Expected<DateTimeField>("date/time field", nextToken);
        }
    }

    /// <summary>Parse a <c>NOT</c> expression.</summary>
    /// <remarks>Represented in the AST as <c>Expr::UnaryOp</c> with <c>UnaryOperator::Not</c>.</remarks>
    public Expr ParseNot()
    {
        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.EXISTS:
                    {
                        var negated = true;
                        _ = ParseKeyword(Keyword.EXISTS);
                        return ParseExistsExpr(negated);
                    }
                    default:
                        return new Expr.UnaryOp(
                            Op: new UnaryOperator.Not(),
                            Expr: ParseSubexpr(_dialect.PrecValue(Precedence.UnaryNot)));
                }
            default:
                return new Expr.UnaryOp(
                    Op: new UnaryOperator.Not(),
                    Expr: ParseSubexpr(_dialect.PrecValue(Precedence.UnaryNot)));
        }
    }

    /// <summary>Parse expression types that start with a left brace '{'.</summary>
    /// <remarks>
    /// Examples:
    /// <code>
    /// -- Dictionary expr.
    /// {'key1': 'value1', 'key2': 'value2'}
    ///
    /// -- Function call using the ODBC syntax.
    /// { fn CONCAT('foo', 'bar') }
    /// </code>
    /// </remarks>
    internal Expr ParseLbraceExpr()
    {
        var token = ExpectToken(new Token.LBrace());

        if (MaybeParseOdbcBody() is { } fnExpr)
        {
            ExpectToken(new Token.RBrace());
            return fnExpr;
        }

        if (_dialect.SupportsDictionarySyntax)
        {
            PrevToken(); // Put back the '{'
            return ParseDictionary();
        }

        return Expected<Expr>("an expression", token);
    }

    /// <summary>Parses fulltext expressions <c>sqlparser::ast::Expr::MatchAgainst</c></summary>
    /// <remarks>
    /// <para><b>Errors</b></para>
    /// <para>
    /// This method will raise an error if the column list is empty or with invalid identifiers,
    /// the match expression is not a literal string, or if the search modifier is not valid.
    /// </para>
    /// </remarks>
    public Expr ParseMatchAgainst()
    {
        var columns = ParseParenthesizedQualifiedColumnList(IsOptional.Mandatory, false);

        ExpectKeywordIs(Keyword.AGAINST);

        ExpectToken(new Token.LParen());

        // MySQL is too permissive about the value, IMO we can't validate it perfectly on syntax level.
        var matchValue = ParseValue();

        var inNaturalLanguageModeKeywords = new[]
        {
            Keyword.IN,
            Keyword.NATURAL,
            Keyword.LANGUAGE,
            Keyword.MODE,
        };

        var withQueryExpansionKeywords = new[] { Keyword.WITH, Keyword.QUERY, Keyword.EXPANSION };

        var inBooleanModeKeywords = new[] { Keyword.IN, Keyword.BOOLEAN, Keyword.MODE };

        SearchModifier? optSearchModifier;
        if (ParseKeywords(inNaturalLanguageModeKeywords))
        {
            if (ParseKeywords(withQueryExpansionKeywords))
            {
                optSearchModifier = new SearchModifier.InNaturalLanguageModeWithQueryExpansion();
            }
            else
            {
                optSearchModifier = new SearchModifier.InNaturalLanguageMode();
            }
        }
        else if (ParseKeywords(inBooleanModeKeywords))
        {
            optSearchModifier = new SearchModifier.InBooleanMode();
        }
        else if (ParseKeywords(withQueryExpansionKeywords))
        {
            optSearchModifier = new SearchModifier.WithQueryExpansion();
        }
        else
        {
            optSearchModifier = null;
        }

        ExpectToken(new Token.RParen());

        return new Expr.MatchAgainst(
            Columns: columns,
            MatchValue: matchValue,
            OptSearchModifier: optSearchModifier);
    }

    /// <summary>Parse an <c>INTERVAL</c> expression.</summary>
    /// <remarks>
    /// Some syntactically valid intervals:
    /// <code>
    ///   1. INTERVAL '1' DAY
    ///   2. INTERVAL '1-1' YEAR TO MONTH
    ///   3. INTERVAL '1' SECOND
    ///   4. INTERVAL '1:1:1.1' HOUR (5) TO SECOND (5)
    ///   5. INTERVAL '1.1' SECOND (2, 2)
    ///   6. INTERVAL '1:1' HOUR (5) TO MINUTE (5)
    ///   7. (MySql &amp; BigQuery only): INTERVAL 1 DAY
    /// </code>
    /// Note that we do not currently attempt to parse the quoted value.
    /// </remarks>
    public Expr ParseInterval()
    {
        // Rust: #[cfg_attr(feature = "recursive-protection", recursive::recursive)] (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            return StackGuard.Grow(this, static parser => parser.ParseInterval());
        }

        using var _guard = _recursionCounter.TryDecrease();

        // The SQL standard allows an optional sign before the value string, but
        // it is not clear if any implementations support that syntax, so we
        // don't currently try to parse it. (The sign can instead be included
        // inside the value string.)

        // to match the different flavours of INTERVAL syntax, we only allow expressions
        // if the dialect requires an interval qualifier,
        // see https://github.com/sqlparser-rs/sqlparser-rs/pull/1398 for more details
        Expr value;
        if (_dialect.RequireIntervalQualifier)
        {
            // parse a whole expression so `INTERVAL 1 + 1 DAY` is valid
            value = ParseExpr();
        }
        else
        {
            // parse a prefix expression so `INTERVAL 1 DAY` is valid, but `INTERVAL 1 + 1 DAY` is not
            // this also means that `INTERVAL '5 days' > INTERVAL '1 day'` treated properly
            value = ParsePrefix();
        }

        // Following the string literal is a qualifier which indicates the units
        // of the duration specified in the string literal.
        //
        // Note that PostgreSQL allows omitting the qualifier, so we provide
        // this more general implementation.
        DateTimeField? leadingField;
        if (NextTokenIsTemporalUnit())
        {
            leadingField = ParseDateTimeField();
        }
        else if (_dialect.RequireIntervalQualifier)
        {
            throw ParserErr(
                "INTERVAL requires a unit after the literal value",
                PeekTokenRef().Span.Start);
        }
        else
        {
            leadingField = null;
        }

        ulong? leadingPrecision;
        DateTimeField? lastField;
        ulong? fsecPrecision;
        if (leadingField is DateTimeField.Second)
        {
            // SQL mandates special syntax for `SECOND TO SECOND` literals.
            // Instead of
            //     `SECOND [(<leading precision>)] TO SECOND[(<fractional seconds precision>)]`
            // one must use the special format:
            //     `SECOND [( <leading precision> [ , <fractional seconds precision>] )]`
            DateTimeField? secondLastField = null;
            var (secondLeadingPrecision, secondFsecPrecision) = ParseOptionalPrecisionScale();
            (leadingPrecision, lastField, fsecPrecision) = (secondLeadingPrecision, secondLastField, secondFsecPrecision);
        }
        else
        {
            var otherLeadingPrecision = ParseOptionalPrecision();
            if (ParseKeyword(Keyword.TO))
            {
                DateTimeField? toLastField = ParseDateTimeField();
                ulong? toFsecPrecision;
                if (toLastField is DateTimeField.Second)
                {
                    toFsecPrecision = ParseOptionalPrecision();
                }
                else
                {
                    toFsecPrecision = null;
                }
                (leadingPrecision, lastField, fsecPrecision) = (otherLeadingPrecision, toLastField, toFsecPrecision);
            }
            else
            {
                (leadingPrecision, lastField, fsecPrecision) = (otherLeadingPrecision, null, null);
            }
        }

        return new Expr.Interval(new Interval(
            Value: value,
            LeadingField: leadingField,
            LeadingPrecision: leadingPrecision,
            LastField: lastField,
            FractionalSecondsPrecision: fsecPrecision));
    }

    /// <summary>
    /// Peek at the next token and determine if it is a temporal unit
    /// like <c>second</c>.
    /// </summary>
    public bool NextTokenIsTemporalUnit()
    {
        if (PeekTokenRef().Token is Token.Word { Value: var word })
        {
            return word.Keyword
                is Keyword.YEAR
                or Keyword.YEARS
                or Keyword.MONTH
                or Keyword.MONTHS
                or Keyword.WEEK
                or Keyword.WEEKS
                or Keyword.DAY
                or Keyword.DAYS
                or Keyword.HOUR
                or Keyword.HOURS
                or Keyword.MINUTE
                or Keyword.MINUTES
                or Keyword.SECOND
                or Keyword.SECONDS
                or Keyword.CENTURY
                or Keyword.DECADE
                or Keyword.DOW
                or Keyword.DOY
                or Keyword.EPOCH
                or Keyword.ISODOW
                or Keyword.ISOYEAR
                or Keyword.JULIAN
                or Keyword.MICROSECOND
                or Keyword.MICROSECONDS
                or Keyword.MILLENIUM
                or Keyword.MILLENNIUM
                or Keyword.MILLISECOND
                or Keyword.MILLISECONDS
                or Keyword.NANOSECOND
                or Keyword.NANOSECONDS
                or Keyword.QUARTER
                or Keyword.TIMEZONE
                or Keyword.TIMEZONE_HOUR
                or Keyword.TIMEZONE_MINUTE;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Syntax
    /// <code>
    /// -- typed
    /// STRUCT&lt;[field_name] field_type, ...&gt;( expr1 [, ... ])
    /// -- typeless
    /// STRUCT( expr1 [AS field_name] [, ... ])
    /// </code>
    /// </summary>
    internal Expr ParseStructLiteral()
    {
        // Parse the fields definition if exist `<[field_name] field_type, ...>`
        PrevToken();
        var (fields, trailingBracket) =
            ParseStructTypeDef(p => p.ParseStructFieldDef());
        if (trailingBracket.Value)
        {
            throw ParserErr(
                "unmatched > in STRUCT literal",
                PeekTokenRef().Span.Start);
        }

        // Parse the struct values `(expr1 [, ... ])`
        ExpectToken(new Token.LParen());
        var values = ParseCommaSeparated(parser => parser.ParseStructFieldExpr(!fields.IsEmpty));
        ExpectToken(new Token.RParen());

        return new Expr.Struct(Values: values, Fields: fields);
    }
}
