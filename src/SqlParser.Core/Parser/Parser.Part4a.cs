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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 13250–14519:
// parse_returns_table_column … parse_explain).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    internal ColumnDef ParseReturnsTableColumn() => ParseColumnDef();

    internal Sequence<ColumnDef> ParseReturnsTableColumns()
    {
        ExpectToken(new Token.LParen());
        var columns = ParseCommaSeparated(p => p.ParseReturnsTableColumn());
        ExpectToken(new Token.RParen());
        return columns;
    }

    /// <summary>Parse a parenthesized, comma-separated list of single-quoted strings.</summary>
    public Sequence<string> ParseStringValues()
    {
        ExpectToken(new Token.LParen());
        var values = new List<string>();
        while (true)
        {
            var nextToken = NextToken();
            switch (nextToken.Token)
            {
                case Token.SingleQuotedString { Value: var value }:
                    values.Add(value);
                    break;
                default:
                    Expected("a string", nextToken);
                    break;
            }
            // Rust: `let next_token` sombreado (caso 72).
            var separatorToken = NextToken();
            var stop = false;
            switch (separatorToken.Token)
            {
                case Token.Comma:
                    break;
                case Token.RParen:
                    stop = true;
                    break;
                default:
                    Expected(", or }", separatorToken);
                    break;
            }
            if (stop)
            {
                break;
            }
        }
        return values.ToSequence();
    }

    /// <summary>Strictly parse <c>identifier AS identifier</c></summary>
    public IdentWithAlias ParseIdentifierWithAlias()
    {
        var ident = ParseIdentifier();
        ExpectKeywordIs(Keyword.AS);
        var alias = ParseIdentifier();
        return new IdentWithAlias(Ident: ident, Alias: alias);
    }

    /// <summary>Parse <c>identifier [AS] identifier</c> where the AS keyword is optional</summary>
    internal IdentWithAlias ParseIdentifierWithOptionalAlias()
    {
        var ident = ParseIdentifier();
        var _afterAs = ParseKeyword(Keyword.AS);
        var alias = ParseIdentifier();
        return new IdentWithAlias(Ident: ident, Alias: alias);
    }

    /// <summary>Parse comma-separated list of parenthesized queries for pipe operators</summary>
    internal Sequence<Query> ParsePipeOperatorQueries()
    {
        return ParseCommaSeparated(parser =>
        {
            parser.ExpectToken(new Token.LParen());
            var query = parser.ParseQuery();
            parser.ExpectToken(new Token.RParen());
            return query;
        });
    }

    /// <summary>Parse set quantifier for pipe operators that require DISTINCT. E.g. INTERSECT and EXCEPT</summary>
    internal SetQuantifier ParseDistinctRequiredSetQuantifier(string operatorName)
    {
        var quantifier = ParseSetQuantifier(new SetOperator.Intersect());
        switch (quantifier)
        {
            case SetQuantifier.Distinct or SetQuantifier.DistinctByName:
                return quantifier;
            default:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"{operatorName} pipe operator requires DISTINCT modifier"));
        }
    }

    /// <summary>Parse optional identifier alias (with or without AS keyword)</summary>
    internal Ident? ParseIdentifierOptionalAlias()
    {
        if (ParseKeyword(Keyword.AS))
        {
            return ParseIdentifier();
        }
        else
        {
            // Check if the next token is an identifier (implicit alias)
            return MaybeParse(parser => parser.ParseIdentifier());
        }
    }

    /// <summary>Optionally parses an alias for a select list item</summary>
    internal Ident? MaybeParseSelectItemAlias()
    {
        // Rust: `fn validator(..)` aninhada → função local estática (decisão 9 da B1).
        static bool Validator(bool @explicit, Keyword kw, Parser parser) =>
            parser._dialect.IsSelectItemAlias(@explicit, kw, parser);
        return ParseOptionalAliasInner(null, Validator);
    }

    /// <summary>
    /// Optionally parses an alias for a table like in <c>... FROM generate_series(1, 10) AS t (col)</c>.
    /// In this case, the alias is allowed to optionally name the columns in the table, in
    /// addition to the table itself.
    /// </summary>
    public TableAlias? MaybeParseTableAlias()
    {
        static bool Validator(bool @explicit, Keyword kw, Parser parser) =>
            parser._dialect.IsTableFactorAlias(@explicit, kw, parser);
        var @explicit = PeekKeyword(Keyword.AS);
        switch (ParseOptionalAliasInner(null, Validator))
        {
            case { } name:
            {
                var columns = ParseTableAliasColumnDefs();
                Ident? at;
                if (_dialect.SupportsPartiql && ParseKeyword(Keyword.AT))
                {
                    at = ParseIdentifier();
                }
                else
                {
                    at = null;
                }
                return new TableAlias(
                    Explicit: @explicit,
                    Name: name,
                    Columns: columns,
                    At: at);
            }
            case null:
                return null;
        }
    }

    internal Sequence<TableIndexHints> ParseTableIndexHints()
    {
        var hints = new List<TableIndexHints>();
        // Rust: `hint_type` (Keyword) sombreado pelo `hint_type` (TableIndexHintType) (caso 72).
        while (ParseOneOfKeywords(Keyword.USE, Keyword.IGNORE, Keyword.FORCE) is { } hintTypeKeyword)
        {
            TableIndexHintType hintType;
            switch (hintTypeKeyword)
            {
                case Keyword.USE:
                    hintType = new TableIndexHintType.Use();
                    break;
                case Keyword.IGNORE:
                    hintType = new TableIndexHintType.Ignore();
                    break;
                case Keyword.FORCE:
                    hintType = new TableIndexHintType.Force();
                    break;
                default:
                    return ExpectedRef<Sequence<TableIndexHints>>(
                        "expected to match USE/IGNORE/FORCE keyword",
                        PeekTokenRef());
            }
            TableIndexType indexType;
            switch (ParseOneOfKeywords(Keyword.INDEX, Keyword.KEY))
            {
                case Keyword.INDEX:
                    indexType = new TableIndexType.Index();
                    break;
                case Keyword.KEY:
                    indexType = new TableIndexType.Key();
                    break;
                default:
                    return ExpectedRef<Sequence<TableIndexHints>>(
                        "expected to match INDEX/KEY keyword",
                        PeekTokenRef());
            }
            TableIndexHintForClause? forClause;
            if (ParseKeyword(Keyword.FOR))
            {
                TableIndexHintForClause clause;
                if (ParseKeyword(Keyword.JOIN))
                {
                    clause = new TableIndexHintForClause.Join();
                }
                else if (ParseKeywords(Keyword.ORDER, Keyword.BY))
                {
                    clause = new TableIndexHintForClause.OrderBy();
                }
                else if (ParseKeywords(Keyword.GROUP, Keyword.BY))
                {
                    clause = new TableIndexHintForClause.GroupBy();
                }
                else
                {
                    return ExpectedRef<Sequence<TableIndexHints>>(
                        "expected to match FOR/ORDER BY/GROUP BY table hint in for clause",
                        PeekTokenRef());
                }
                forClause = clause;
            }
            else
            {
                forClause = null;
            }

            ExpectToken(new Token.LParen());
            Sequence<Ident> indexNames;
            if (PeekTokenRef().Token is not Token.RParen)
            {
                indexNames = ParseCommaSeparated(p => p.ParseIdentifier());
            }
            else
            {
                indexNames = Sequence<Ident>.Empty;
            }
            ExpectToken(new Token.RParen());
            hints.Add(new TableIndexHints(
                HintType: hintType,
                IndexType: indexType,
                ForClause: forClause,
                IndexNames: indexNames));
        }
        return hints.ToSequence();
    }

    /// <summary>
    /// Wrapper for parse_optional_alias_inner, left for backwards-compatibility
    /// but new flows should use the context-specific methods such as <c>maybe_parse_select_item_alias</c>
    /// and <c>maybe_parse_table_alias</c>.
    /// </summary>
    public Ident? ParseOptionalAlias(params Keyword[] reservedKwds)
    {
        static bool Validator(bool _explicit, Keyword _kw, Parser _parser) => false;
        return ParseOptionalAliasInner(reservedKwds, Validator);
    }

    /// <summary>
    /// Parses an optional alias after a SQL element such as a select list item
    /// or a table name.
    /// </summary>
    /// <remarks>
    /// This method accepts an optional list of reserved keywords or a function
    /// to call to validate if a keyword should be parsed as an alias, to allow
    /// callers to customize the parsing logic based on their context.
    /// </remarks>
    internal Ident? ParseOptionalAliasInner(
        Keyword[]? reservedKwds,
        Func<bool, Keyword, Parser, bool> validator)
    {
        var afterAs = ParseKeyword(Keyword.AS);

        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            // Accepts a keyword as an alias if the AS keyword explicitly indicate an alias or if the
            // caller provided a list of reserved keywords and the keyword is not on that list.
            case Token.Word { Value: var w }
                when reservedKwds is not null
                    && (afterAs || (reservedKwds is { } x && !x.Contains(w.Keyword))):
                return w.IntoIdent(nextToken.Span);
            // Accepts a keyword as alias based on the caller's context, such as to what SQL element
            // this word is a potential alias of using the validator call-back. This allows for
            // dialect-specific logic.
            case Token.Word { Value: var w } when validator(afterAs, w.Keyword, this):
                return w.IntoIdent(nextToken.Span);
            // For backwards-compatibility, we accept quoted strings as aliases regardless of the context.
            case Token.SingleQuotedString { Value: var s }:
                return Ident.WithQuote('\'', s);
            case Token.DoubleQuotedString { Value: var s }:
                return Ident.WithQuote('"', s);
            default:
                if (afterAs)
                {
                    return Expected<Ident?>("an identifier after AS", nextToken);
                }
                PrevToken();
                return null; // no alias found
        }
    }

    /// <summary>Parse an optional <c>GROUP BY</c> clause, returning <c>Some(GroupByExpr)</c> when present.</summary>
    public GroupByExpr? ParseOptionalGroupBy()
    {
        if (ParseKeywords(Keyword.GROUP, Keyword.BY))
        {
            Sequence<Expr>? expressions;
            if (ParseKeyword(Keyword.ALL))
            {
                expressions = null;
            }
            else
            {
                expressions = ParseCommaSeparated(p => p.ParseGroupByExpr());
            }

            var modifiers = new List<GroupByWithModifier>();
            if (_dialect.SupportsGroupByWithModifier)
            {
                while (true)
                {
                    if (!ParseKeyword(Keyword.WITH))
                    {
                        break;
                    }
                    var keyword = ExpectOneOfKeywords(
                        Keyword.ROLLUP,
                        Keyword.CUBE,
                        Keyword.TOTALS);
                    switch (keyword)
                    {
                        case Keyword.ROLLUP:
                            modifiers.Add(new GroupByWithModifier.Rollup());
                            break;
                        case Keyword.CUBE:
                            modifiers.Add(new GroupByWithModifier.Cube());
                            break;
                        case Keyword.TOTALS:
                            modifiers.Add(new GroupByWithModifier.Totals());
                            break;
                        default:
                            throw ParserErr(
                                "BUG: expected to match GroupBy modifier keyword",
                                PeekTokenRef().Span.Start);
                    }
                }
            }
            if (ParseKeywords(Keyword.GROUPING, Keyword.SETS))
            {
                ExpectToken(new Token.LParen());
                var result = ParseCommaSeparated(p =>
                {
                    if (p.PeekTokenRef().Token is Token.LParen)
                    {
                        return p.ParseTuple(true, true);
                    }
                    else
                    {
                        return Sequence.Of(p.ParseExpr());
                    }
                });
                ExpectToken(new Token.RParen());
                modifiers.Add(new GroupByWithModifier.GroupingSets(new Expr.GroupingSets(
                    result)));
            }
            GroupByExpr groupBy;
            switch (expressions)
            {
                case null:
                    groupBy = new GroupByExpr.All(modifiers.ToSequence());
                    break;
                case { } exprs:
                    groupBy = new GroupByExpr.Expressions(exprs, modifiers.ToSequence());
                    break;
            }
            return groupBy;
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse an optional <c>ORDER BY</c> clause, returning <c>Some(OrderBy)</c> when present.</summary>
    public OrderBy? ParseOptionalOrderBy()
    {
        if (ParseKeywords(Keyword.ORDER, Keyword.BY))
        {
            OrderBy orderBy;
            if (_dialect.SupportsOrderByAll && ParseKeyword(Keyword.ALL))
            {
                var orderByOptions = ParseOrderByOptions();
                orderBy = new OrderBy(
                    Kind: new OrderByKind.All(orderByOptions),
                    Interpolate: null);
            }
            else
            {
                var exprs = ParseCommaSeparated(p => p.ParseOrderByExpr());
                Interpolate? interpolate;
                if (_dialect.SupportsInterpolate)
                {
                    interpolate = ParseInterpolations();
                }
                else
                {
                    interpolate = null;
                }
                orderBy = new OrderBy(
                    Kind: new OrderByKind.Expressions(exprs),
                    Interpolate: interpolate);
            }
            return orderBy;
        }
        else
        {
            return null;
        }
    }

    internal LimitClause? ParseOptionalLimitClause()
    {
        Offset? offset;
        if (ParseKeyword(Keyword.OFFSET))
        {
            offset = ParseOffset();
        }
        else
        {
            offset = null;
        }

        // Rust: `limit: Option<Option<Expr>>` (convencoes.md §13, caso 55).
        Option<Expr?> limit;
        Sequence<Expr>? limitBy;
        if (ParseKeyword(Keyword.LIMIT))
        {
            var expr = ParseLimit();

            if (_dialect.SupportsLimitComma
                && offset is null
                && expr is not null // ALL not supported with comma
                && ConsumeToken(new Token.Comma()))
            {
                // Rust: `let offset = expr.ok_or_else(..)?;` (decisão 11 da B2: teste + uso de `expr`).
                if (expr is null)
                {
                    throw new ParserException(new ParserError.ParserErrorValue(
                        "Missing offset for LIMIT <offset>, <limit>"));
                }
                return new LimitClause.OffsetCommaLimit(
                    Offset: expr,
                    Limit: ParseExpr());
            }

            Sequence<Expr>? parsedLimitBy;
            if (_dialect.SupportsLimitBy && ParseKeyword(Keyword.BY))
            {
                parsedLimitBy = ParseCommaSeparated(p => p.ParseExpr());
            }
            else
            {
                parsedLimitBy = null;
            }

            (limit, limitBy) = (Option<Expr?>.Some(expr), parsedLimitBy);
        }
        else
        {
            (limit, limitBy) = (Option<Expr?>.None, null);
        }

        if (offset is null && limit.IsSome && ParseKeyword(Keyword.OFFSET))
        {
            offset = ParseOffset();
        }

        // `limit != Some(None)` com `limit.is_some()` já testado: o valor interno não é `None`.
        if (offset is not null || (limit.IsSome && limit.Value is not null) || limitBy is not null)
        {
            return new LimitClause.LimitOffset(
                Limit: limit.IsSome ? limit.Value : null,
                Offset: offset,
                LimitBy: limitBy ?? Sequence<Expr>.Empty);
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Parse a table object for insertion
    /// e.g. <c>some_database.some_table</c> or <c>FUNCTION some_table_func(...)</c>
    /// </summary>
    public TableObject ParseTableObject()
    {
        if (_dialect.SupportsInsertTableFunction && ParseKeyword(Keyword.FUNCTION))
        {
            var fnName = ParseObjectName(false);
            return new TableObject.TableFunction(ParseFunctionCall(fnName));
        }
        else if (_dialect.SupportsInsertTableQuery && PeekSubqueryOrCteStart())
        {
            return new TableObject.TableQuery(ParseParenthesized(p => p.ParseQuery()));
        }
        else
        {
            return new TableObject.TableName(ParseObjectName(false));
        }
    }

    /// <summary>
    /// Parse a possibly qualified, possibly quoted identifier, e.g.
    /// <c>foo</c> or <c>myschema."table"</c>
    /// </summary>
    /// <remarks>
    /// The <c>in_table_clause</c> parameter indicates whether the object name is a table in a FROM, JOIN,
    /// or similar table clause. Currently, this is used only to support unquoted hyphenated identifiers
    /// in this context on BigQuery.
    /// </remarks>
    public ObjectName ParseObjectName(bool inTableClause) => ParseObjectNameInner(inTableClause, false);

    /// <summary>
    /// Parse a possibly qualified, possibly quoted identifier, e.g.
    /// <c>foo</c> or <c>myschema."table"</c>
    /// </summary>
    /// <remarks>
    /// The <c>in_table_clause</c> parameter indicates whether the object name is a table in a FROM, JOIN,
    /// or similar table clause. Currently, this is used only to support unquoted hyphenated identifiers
    /// in this context on BigQuery.
    /// <para>
    /// The <c>allow_wildcards</c> parameter indicates whether to allow for wildcards in the object name
    /// e.g. *, *.*, <c>foo</c>.*, or "foo"."bar"
    /// </para>
    /// </remarks>
    internal ObjectName ParseObjectNameInner(bool inTableClause, bool allowWildcards)
    {
        var parts = new List<ObjectNamePart>();
        if (_dialect is BigQueryDialect && inTableClause)
        {
            while (true)
            {
                var (ident, endWithPeriod) = ParseUnquotedHyphenatedIdentifier();
                parts.Add(new ObjectNamePart.Identifier(ident));
                if (!ConsumeToken(new Token.Period()) && !endWithPeriod)
                {
                    break;
                }
            }
        }
        else
        {
            while (true)
            {
                if (allowWildcards && PeekTokenRef().Token is Token.Mul)
                {
                    var span = NextToken().Span;
                    parts.Add(new ObjectNamePart.Identifier(new Ident(
                        Value: new Token.Mul().ToString(),
                        QuoteStyle: null,
                        Span: span)));
                }
                else if (_dialect is BigQueryDialect && inTableClause)
                {
                    var (ident, endWithPeriod) = ParseUnquotedHyphenatedIdentifier();
                    parts.Add(new ObjectNamePart.Identifier(ident));
                    if (!ConsumeToken(new Token.Period()) && !endWithPeriod)
                    {
                        break;
                    }
                }
                else if (_dialect.SupportsObjectNameDoubleDotNotation
                    && parts.Count == 1
                    && PeekTokenRef().Token is Token.Period)
                {
                    // Empty string here means default schema
                    parts.Add(new ObjectNamePart.Identifier(new Ident("")));
                }
                else
                {
                    var ident = ParseIdentifier();
                    ObjectNamePart part;
                    if (_dialect.IsIdentifierGeneratingFunctionName(ident, parts))
                    {
                        ExpectToken(new Token.LParen());
                        var args = ParseCommaSeparated0(p => p.ParseFunctionArgs(), new Token.RParen());
                        ExpectToken(new Token.RParen());
                        part = new ObjectNamePart.Function(new ObjectNamePartFunction(Name: ident, Args: args));
                    }
                    else
                    {
                        part = new ObjectNamePart.Identifier(ident);
                    }
                    parts.Add(part);
                }

                if (!ConsumeToken(new Token.Period()))
                {
                    break;
                }
            }
        }

        // BigQuery accepts any number of quoted identifiers of a table name.
        // https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_identifiers
        if (_dialect is BigQueryDialect
            && parts.Any(part => part.AsIdent() is { } ident && ident.Value.Contains(".")))
        {
            // Rust: `into_iter().flat_map(..).collect()`; laço explícito (convencoes.md §9).
            var splitParts = new List<ObjectNamePart>();
            foreach (var part in parts)
            {
                switch (part.AsIdent())
                {
                    case { } ident:
                        foreach (var value in ident.Value.Split('.'))
                        {
                            splitParts.Add(new ObjectNamePart.Identifier(new Ident(
                                Value: value,
                                QuoteStyle: ident.QuoteStyle,
                                Span: ident.Span)));
                        }
                        break;
                    case null:
                        splitParts.Add(part);
                        break;
                }
            }
            parts = splitParts;
        }

        return new ObjectName(parts.ToSequence());
    }

    /// <summary>Parse identifiers</summary>
    public Sequence<Ident> ParseIdentifiers()
    {
        var idents = new List<Ident>();
        while (true)
        {
            var token = PeekTokenRef();
            var stop = false;
            switch (token.Token)
            {
                case Token.Word { Value: var w }:
                    idents.Add(w.ToIdent(token.Span));
                    break;
                case Token.EOF or Token.Eq or Token.SemiColon or Token.VerticalBarRightAngleBracket:
                    stop = true;
                    break;
                default:
                    break;
            }
            if (stop)
            {
                break;
            }
            AdvanceToken();
        }
        return idents.ToSequence();
    }

    /// <summary>Parse identifiers of form ident1[.identN]*</summary>
    /// <remarks>
    /// Similar in functionality to <see cref="ParseIdentifiers"/>, with difference
    /// being this function is much more strict about parsing a valid multipart identifier, not
    /// allowing extraneous tokens to be parsed, otherwise it fails.
    /// <para>For example:</para>
    /// <code>
    /// var dialect = new GenericDialect();
    /// var expected = Sequence.Of(new Ident("one"), new Ident("two"));
    ///
    /// // expected usage
    /// var actual = new Parser(dialect).TryWithSql("one.two").ParseMultipartIdentifier();
    /// // actual == expected
    ///
    /// // parse_identifiers is more loose on what it allows, parsing successfully
    /// actual = new Parser(dialect).TryWithSql("one + two").ParseIdentifiers();
    /// // actual == expected
    ///
    /// // expected to strictly fail due to + separator
    /// // new Parser(dialect).TryWithSql("one + two").ParseMultipartIdentifier() lança
    /// // "sql parser error: Unexpected token in identifier: +"
    /// </code>
    /// </remarks>
    public Sequence<Ident> ParseMultipartIdentifier()
    {
        var idents = new List<Ident>();

        // expecting at least one word for identifier
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                idents.Add(w.IntoIdent(nextToken.Span));
                break;
            case Token.EOF:
                throw new ParserException(new ParserError.ParserErrorValue(
                    "Empty input when parsing identifier"));
            case var token:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Unexpected token in identifier: {token}"));
        }

        // parse optional next parts if exist
        while (true)
        {
            var stop = false;
            switch (NextToken().Token)
            {
                // ensure that optional period is succeeded by another identifier
                case Token.Period:
                {
                    // Rust: `let next_token` sombreado (caso 72).
                    var partToken = NextToken();
                    switch (partToken.Token)
                    {
                        case Token.Word { Value: var w }:
                            idents.Add(w.IntoIdent(partToken.Span));
                            break;
                        case Token.EOF:
                            throw new ParserException(new ParserError.ParserErrorValue(
                                "Trailing period in identifier"));
                        case var token:
                            throw new ParserException(new ParserError.ParserErrorValue(
                                $"Unexpected token following period in identifier: {token}"));
                    }
                    break;
                }
                case Token.EOF:
                    stop = true;
                    break;
                case var token:
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"Unexpected token in identifier: {token}"));
            }
            if (stop)
            {
                break;
            }
        }

        return idents.ToSequence();
    }

    /// <summary>Parse a simple one-word identifier (possibly quoted, possibly a keyword)</summary>
    public Ident ParseIdentifier()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                return w.IntoIdent(nextToken.Span);
            case Token.SingleQuotedString { Value: var s }:
                return Ident.WithQuote('\'', s);
            case Token.DoubleQuotedString { Value: var s }:
                return Ident.WithQuote('"', s);
            default:
                return Expected<Ident>("identifier", nextToken);
        }
    }

    /// <summary>
    /// On BigQuery, hyphens are permitted in unquoted identifiers inside of a FROM or
    /// TABLE clause.
    /// </summary>
    /// <remarks>
    /// The first segment must be an ordinary unquoted identifier, e.g. it must not start
    /// with a digit. Subsequent segments are either must either be valid identifiers or
    /// integers, e.g. foo-123 is allowed, but foo-123a is not.
    /// <para><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical">BigQuery-lexical</see></para>
    /// <para>Return a tuple of the identifier and a boolean indicating it ends with a period.</para>
    /// </remarks>
    internal (Ident Ident, bool EndWithPeriod) ParseUnquotedHyphenatedIdentifier()
    {
        switch (PeekToken().Token)
        {
            case Token.Word { Value: var w }:
            {
                var quoteStyleIsNone = w.QuoteStyle is null;
                var requiresWhitespace = false;
                var ident = w.IntoIdent(NextToken().Span);
                if (quoteStyleIsNone)
                {
                    while (PeekTokenNoSkip().Token is Token.Minus)
                    {
                        NextToken();
                        // Rust: `ident.value.push('-')` sobre record imutável → `with`.
                        ident = ident with { Value = ident.Value + "-" };

                        var token = NextTokenNoSkip() ?? TokenWithSpan.Wrap(new Token.EOF());
                        switch (token.Token)
                        {
                            case Token.Word { Value: var nextWord } when nextWord.QuoteStyle is null:
                                ident = ident with { Value = ident.Value + nextWord.Value };
                                requiresWhitespace = false;
                                break;
                            case Token.Number { Value: var s, Long: false }:
                                // A number token can represent a decimal value ending with a period, e.g., `Number('123.')`.
                                // However, for an [ObjectName], it is part of a hyphenated identifier, e.g., `foo-123.bar`.
                                //
                                // If a number token is followed by a period, it is part of an [ObjectName].
                                // Return the identifier with `true` if the number token is followed by a period, indicating that
                                // parsing should continue for the next part of the hyphenated identifier.
                                if (s.EndsWith(".", StringComparison.Ordinal))
                                {
                                    // Rust: `let Some(s) = s.split('.').next().filter(..) else { .. }`; o
                                    // `s` interno sombreia o externo (caso 72: `firstSegment`).
                                    var firstSegment = s.Split('.')[0];
                                    if (!(firstSegment.Length != 0 && firstSegment.All(RustChar.IsAsciiDigit)))
                                    {
                                        return Expected<(Ident, bool)>(
                                            "continuation of hyphenated identifier",
                                            new TokenWithSpan(new Token.Number(s, false), token.Span));
                                    }
                                    ident = ident with { Value = ident.Value + firstSegment };
                                    return (ident, true);
                                }
                                else
                                {
                                    ident = ident with { Value = ident.Value + s };
                                }
                                // If next token is period, then it is part of an ObjectName and we don't expect whitespace
                                // after the number.
                                requiresWhitespace = PeekTokenRef().Token is not Token.Period;
                                break;
                            default:
                                return Expected<(Ident, bool)>("continuation of hyphenated identifier", token);
                        }
                    }

                    // If the last segment was a number, we must check that it's followed by whitespace,
                    // otherwise foo-123a will be parsed as `foo-123` with the alias `a`.
                    if (requiresWhitespace)
                    {
                        var token = NextToken();
                        if (token.Token is not (Token.EOF or Token.Whitespace))
                        {
                            return Expected<(Ident, bool)>("whitespace following hyphenated identifier", token);
                        }
                    }
                }
                return (ident, false);
            }
            default:
                return (ParseIdentifier(), false);
        }
    }

    /// <summary>Parses a parenthesized, comma-separated list of column definitions within a view.</summary>
    internal Sequence<ViewColumnDef> ParseViewColumns()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            if (PeekTokenRef().Token is Token.RParen)
            {
                NextToken();
                return Sequence<ViewColumnDef>.Empty;
            }
            else
            {
                var cols = ParseCommaSeparatedWithTrailingCommas(
                    p => p.ParseViewColumn(),
                    _dialect.SupportsColumnDefinitionTrailingCommas,
                    IsReservedForColumnAlias);
                ExpectToken(new Token.RParen());
                return cols;
            }
        }
        else
        {
            return Sequence<ViewColumnDef>.Empty;
        }
    }

    /// <summary>Parses a column definition within a view.</summary>
    internal ViewColumnDef ParseViewColumn()
    {
        var name = ParseIdentifier();
        var options = ParseViewColumnOptions();
        DataType? dataType;
        if (_dialect is ClickHouseDialect)
        {
            dataType = ParseDataType();
        }
        else
        {
            dataType = null;
        }
        return new ViewColumnDef(
            Name: name,
            DataType: dataType,
            Options: options);
    }

    internal ColumnOptions? ParseViewColumnOptions()
    {
        var options = new List<ColumnOption>();
        while (true)
        {
            var option = ParseOptionalColumnOption();
            // Rust: `if let Some(option) = option` sombreia (caso 72).
            if (option is { } someOption)
            {
                options.Add(someOption);
            }
            else
            {
                break;
            }
        }
        if (options.Count == 0)
        {
            return null;
        }
        else if (_dialect.SupportsSpaceSeparatedColumnOptions)
        {
            return new ColumnOptions.SpaceSeparated(options.ToSequence());
        }
        else
        {
            return new ColumnOptions.CommaSeparated(options.ToSequence());
        }
    }

    /// <summary>
    /// Parses a parenthesized comma-separated list of unqualified, possibly quoted identifiers.
    /// For example: <c>(col1, "col 2", ...)</c>
    /// </summary>
    public Sequence<Ident> ParseParenthesizedColumnList(IsOptional optional, bool allowEmpty) =>
        ParseParenthesizedColumnListInner(optional, allowEmpty, p => p.ParseIdentifier());

    /// <summary>Parse a parenthesized list of compound identifiers as expressions.</summary>
    public Sequence<Expr> ParseParenthesizedCompoundIdentifierList(IsOptional optional, bool allowEmpty) =>
        ParseParenthesizedColumnListInner<Expr>(optional, allowEmpty, p =>
            new Expr.CompoundIdentifier(
                p.ParsePeriodSeparated(inner => inner.ParseIdentifier())));

    /// <summary>
    /// Parses a parenthesized comma-separated list of index columns, which can be arbitrary
    /// expressions with ordering information (and an opclass in some dialects).
    /// </summary>
    internal Sequence<IndexColumn> ParseParenthesizedIndexColumnList() =>
        ParseParenthesizedColumnListInner(IsOptional.Mandatory, false, p => p.ParseCreateIndexExpr());

    /// <summary>
    /// Parses a parenthesized comma-separated list of qualified, possibly quoted identifiers.
    /// For example: <c>(db1.sc1.tbl1.col1, db1.sc1.tbl1."col 2", ...)</c>
    /// </summary>
    public Sequence<ObjectName> ParseParenthesizedQualifiedColumnList(IsOptional optional, bool allowEmpty) =>
        ParseParenthesizedColumnListInner(optional, allowEmpty, p => p.ParseObjectName(true));

    /// <summary>
    /// Parses a parenthesized comma-separated list of columns using
    /// the provided function to parse each element.
    /// </summary>
    internal Sequence<T> ParseParenthesizedColumnListInner<T>(
        IsOptional optional,
        bool allowEmpty,
        Func<Parser, T> f)
    {
        if (ConsumeToken(new Token.LParen()))
        {
            if (allowEmpty && PeekTokenRef().Token is Token.RParen)
            {
                NextToken();
                return Sequence<T>.Empty;
            }
            else
            {
                var cols = ParseCommaSeparated(p => f(p));
                ExpectToken(new Token.RParen());
                return cols;
            }
        }
        else if (optional == IsOptional.Optional)
        {
            return Sequence<T>.Empty;
        }
        else
        {
            return ExpectedRef<Sequence<T>>("a list of columns in parentheses", PeekTokenRef());
        }
    }

    /// <summary>Parses a parenthesized comma-separated list of table alias column definitions.</summary>
    internal Sequence<TableAliasColumnDef> ParseTableAliasColumnDefs()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            var cols = ParseCommaSeparated(p =>
            {
                var name = p.ParseIdentifier();
                var dataType = p.MaybeParse(inner => inner.ParseDataType());
                return new TableAliasColumnDef(Name: name, DataType: dataType);
            });
            ExpectToken(new Token.RParen());
            return cols;
        }
        else
        {
            return Sequence<TableAliasColumnDef>.Empty;
        }
    }

    /// <summary>Parse an unsigned precision value enclosed in parentheses, e.g. <c>(10)</c>.</summary>
    public ulong ParsePrecision()
    {
        ExpectToken(new Token.LParen());
        var n = ParseLiteralUint();
        ExpectToken(new Token.RParen());
        return n;
    }

    /// <summary>Parse an optional precision <c>(n)</c> and return it as <c>Some(n)</c> when present.</summary>
    public ulong? ParseOptionalPrecision()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            var n = ParseLiteralUint();
            ExpectToken(new Token.RParen());
            return n;
        }
        else
        {
            return null;
        }
    }

    internal IntervalFields? MaybeParseOptionalIntervalFields()
    {
        switch (ParseOneOfKeywords(
            // Can be followed by `TO` option
            Keyword.YEAR,
            Keyword.DAY,
            Keyword.HOUR,
            Keyword.MINUTE,
            // No `TO` option
            Keyword.MONTH,
            Keyword.SECOND))
        {
            case Keyword.YEAR:
                if (PeekKeyword(Keyword.TO))
                {
                    ExpectKeyword(Keyword.TO);
                    ExpectKeyword(Keyword.MONTH);
                    return new IntervalFields.YearToMonth();
                }
                else
                {
                    return new IntervalFields.Year();
                }
            case Keyword.DAY:
                if (PeekKeyword(Keyword.TO))
                {
                    ExpectKeyword(Keyword.TO);
                    switch (ExpectOneOfKeywords(
                        Keyword.HOUR,
                        Keyword.MINUTE,
                        Keyword.SECOND))
                    {
                        case Keyword.HOUR:
                            return new IntervalFields.DayToHour();
                        case Keyword.MINUTE:
                            return new IntervalFields.DayToMinute();
                        case Keyword.SECOND:
                            return new IntervalFields.DayToSecond();
                        default:
                            PrevToken();
                            return ExpectedRef<IntervalFields?>("HOUR, MINUTE, or SECOND", PeekTokenRef());
                    }
                }
                else
                {
                    return new IntervalFields.Day();
                }
            case Keyword.HOUR:
                if (PeekKeyword(Keyword.TO))
                {
                    ExpectKeyword(Keyword.TO);
                    switch (ExpectOneOfKeywords(Keyword.MINUTE, Keyword.SECOND))
                    {
                        case Keyword.MINUTE:
                            return new IntervalFields.HourToMinute();
                        case Keyword.SECOND:
                            return new IntervalFields.HourToSecond();
                        default:
                            PrevToken();
                            return ExpectedRef<IntervalFields?>("MINUTE or SECOND", PeekTokenRef());
                    }
                }
                else
                {
                    return new IntervalFields.Hour();
                }
            case Keyword.MINUTE:
                if (PeekKeyword(Keyword.TO))
                {
                    ExpectKeyword(Keyword.TO);
                    ExpectKeyword(Keyword.SECOND);
                    return new IntervalFields.MinuteToSecond();
                }
                else
                {
                    return new IntervalFields.Minute();
                }
            case Keyword.MONTH:
                return new IntervalFields.Month();
            case Keyword.SECOND:
                return new IntervalFields.Second();
            case { }:
                PrevToken();
                return ExpectedRef<IntervalFields?>(
                    "YEAR, MONTH, DAY, HOUR, MINUTE, or SECOND",
                    PeekTokenRef());
            case null:
                return null;
        }
    }

    /// <summary>
    /// Parse datetime64 [1]
    /// Syntax
    /// <code>
    /// DateTime64(precision[, timezone])
    /// </code>
    /// </summary>
    /// <remarks>[1]: <see href="https://clickhouse.com/docs/en/sql-reference/data-types/datetime64"/></remarks>
    public (ulong Precision, string? TimeZone) ParseDatetime64()
    {
        ExpectKeywordIs(Keyword.DATETIME64);
        ExpectToken(new Token.LParen());
        var precision = ParseLiteralUint();
        string? timeZone;
        if (ConsumeToken(new Token.Comma()))
        {
            timeZone = ParseLiteralString();
        }
        else
        {
            timeZone = null;
        }
        ExpectToken(new Token.RParen());
        return (precision, timeZone);
    }

    /// <summary>Parse an optional character length specification <c>(n | MAX [CHARACTERS|OCTETS])</c>.</summary>
    public CharacterLength? ParseOptionalCharacterLength()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            var characterLength = ParseCharacterLength();
            ExpectToken(new Token.RParen());
            return characterLength;
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse an optional binary length specification like <c>(n)</c>.</summary>
    public BinaryLength? ParseOptionalBinaryLength()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            var binaryLength = ParseBinaryLength();
            ExpectToken(new Token.RParen());
            return binaryLength;
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse a character length, handling <c>MAX</c> or integer lengths with optional units.</summary>
    public CharacterLength ParseCharacterLength()
    {
        if (ParseKeyword(Keyword.MAX))
        {
            return new CharacterLength.Max();
        }
        var length = ParseLiteralUint();
        CharLengthUnits? unit;
        if (ParseKeyword(Keyword.CHARACTERS))
        {
            unit = new CharLengthUnits.Characters();
        }
        else if (ParseKeyword(Keyword.OCTETS))
        {
            unit = new CharLengthUnits.Octets();
        }
        else
        {
            unit = null;
        }
        return new CharacterLength.IntegerLength(Length: length, Unit: unit);
    }

    /// <summary>Parse a binary length specification, returning <c>BinaryLength</c>.</summary>
    public BinaryLength ParseBinaryLength()
    {
        if (ParseKeyword(Keyword.MAX))
        {
            return new BinaryLength.Max();
        }
        var length = ParseLiteralUint();
        return new BinaryLength.IntegerLength(Length: length);
    }

    /// <summary>
    /// Parse an optional <c>(precision[, scale])</c> and return <c>(Option&lt;precision&gt;, Option&lt;scale&gt;)</c>.
    /// </summary>
    public (ulong? Precision, ulong? Scale) ParseOptionalPrecisionScale()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            var n = ParseLiteralUint();
            ulong? scale;
            if (ConsumeToken(new Token.Comma()))
            {
                scale = ParseLiteralUint();
            }
            else
            {
                scale = null;
            }
            ExpectToken(new Token.RParen());
            return (n, scale);
        }
        else
        {
            return (null, null);
        }
    }

    /// <summary>Parse exact-number precision/scale info like <c>(precision[, scale])</c> for decimal types.</summary>
    public ExactNumberInfo ParseExactNumberOptionalPrecisionScale()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            var precision = ParseLiteralUint();
            long? scale;
            if (ConsumeToken(new Token.Comma()))
            {
                scale = ParseSignedInteger();
            }
            else
            {
                scale = null;
            }

            ExpectToken(new Token.RParen());

            switch (scale)
            {
                case null:
                    return new ExactNumberInfo.Precision(precision);
                case { } someScale:
                    return new ExactNumberInfo.PrecisionAndScale(precision, someScale);
            }
        }
        else
        {
            return new ExactNumberInfo.None();
        }
    }

    /// <summary>Parse an optionally signed integer literal.</summary>
    internal long ParseSignedInteger()
    {
        var isNegative = ConsumeToken(new Token.Minus());

        if (!isNegative)
        {
            _ = ConsumeToken(new Token.Plus());
        }

        var currentToken = PeekTokenRef();
        switch (currentToken.Token)
        {
            case Token.Number { Value: var s }:
            {
                var spanStart = currentToken.Span.Start;
                AdvanceToken();
                var value = Parse<long>(s, spanStart);
                return isNegative ? -value : value;
            }
            default:
                return ExpectedRef<long>("number", currentToken);
        }
    }

    /// <summary>Parse optional type modifiers appearing in parentheses e.g. <c>(UNSIGNED, ZEROFILL)</c>.</summary>
    public Sequence<string>? ParseOptionalTypeModifiers()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            var modifiers = new List<string>();
            while (true)
            {
                var nextToken = NextToken();
                var stop = false;
                switch (nextToken.Token)
                {
                    case Token.Word { Value: var w }:
                        modifiers.Add(w.ToString());
                        break;
                    case Token.Number { Value: var n }:
                        modifiers.Add(n);
                        break;
                    case Token.SingleQuotedString { Value: var s }:
                        modifiers.Add(s);
                        break;

                    case Token.Comma:
                        continue;
                    case Token.RParen:
                        stop = true;
                        break;
                    default:
                        Expected("type modifiers", nextToken);
                        break;
                }
                if (stop)
                {
                    break;
                }
            }

            return modifiers.ToSequence();
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse a parenthesized sub data type</summary>
    /// <remarks>
    /// Rust: <c>F: FnOnce(Box&lt;DataType&gt;) -&gt; DataType</c> → <c>Func&lt;DataType, DataType&gt;</c>
    /// (convencoes.md §13, caso 50; <c>Box&lt;T&gt;</c> → <c>T</c>).
    /// </remarks>
    internal DataType ParseSubType(Func<DataType, DataType> parentType)
    {
        ExpectToken(new Token.LParen());
        var insideType = ParseDataType();
        ExpectToken(new Token.RParen());
        return parentType(insideType);
    }

    /// <summary>Parse a DELETE statement, returning a <c>Box</c>ed SetExpr</summary>
    /// <remarks>This is used to reduce the size of the stack frames in debug builds</remarks>
    internal SetExpr ParseDeleteSetexprBoxed(TokenWithSpan deleteToken) =>
        new SetExpr.Delete(ParseDelete(deleteToken));

    /// <summary>Parse a <c>DELETE</c> statement and return <c>Statement::Delete</c>.</summary>
    public Statement ParseDelete(TokenWithSpan deleteToken)
    {
        var optimizerHints = MaybeParseOptimizerHints();
        Sequence<ObjectName> tables;
        bool withFromKeyword;
        if (!ParseKeyword(Keyword.FROM))
        {
            // `FROM` keyword is optional in BigQuery SQL.
            // https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#delete_statement
            if (_dialect is BigQueryDialect or OracleDialect or GenericDialect)
            {
                (tables, withFromKeyword) = (Sequence<ObjectName>.Empty, false);
            }
            else
            {
                tables = ParseCommaSeparated(p => p.ParseObjectName(false));
                ExpectKeywordIs(Keyword.FROM);
                withFromKeyword = true;
            }
        }
        else
        {
            (tables, withFromKeyword) = (Sequence<ObjectName>.Empty, true);
        }

        var @from = ParseCommaSeparated(p => p.ParseTableAndJoins());

        var output = MaybeParseOutputClause();

        Sequence<TableWithJoins>? @using;
        if (ParseKeyword(Keyword.USING))
        {
            @using = ParseCommaSeparated(p => p.ParseTableAndJoins());
        }
        else
        {
            @using = null;
        }
        Expr? selection;
        if (ParseKeyword(Keyword.WHERE))
        {
            selection = ParseExpr();
        }
        else
        {
            selection = null;
        }
        Sequence<SelectItem>? returning;
        if (ParseKeyword(Keyword.RETURNING))
        {
            returning = ParseCommaSeparated(p => p.ParseSelectItem());
        }
        else
        {
            returning = null;
        }
        Sequence<OrderByExpr> orderBy;
        if (ParseKeywords(Keyword.ORDER, Keyword.BY))
        {
            orderBy = ParseCommaSeparated(p => p.ParseOrderByExpr());
        }
        else
        {
            orderBy = Sequence<OrderByExpr>.Empty;
        }
        Expr? limit;
        if (ParseKeyword(Keyword.LIMIT))
        {
            limit = ParseLimit();
        }
        else
        {
            limit = null;
        }

        return new Statement.Delete(new Delete(
            DeleteToken: AttachedToken.From(deleteToken),
            OptimizerHints: optimizerHints,
            Tables: tables,
            From: withFromKeyword
                ? (FromTable)new FromTable.WithFromKeyword(@from)
                : new FromTable.WithoutKeyword(@from),
            Using: @using,
            Selection: selection,
            Returning: returning,
            Output: output,
            OrderBy: orderBy,
            Limit: limit));
    }

    /// <summary>
    /// Parse a <c>KILL</c> statement, optionally specifying <c>CONNECTION</c>, <c>QUERY</c>, or <c>MUTATION</c>.
    /// KILL [CONNECTION | QUERY | MUTATION] processlist_id
    /// </summary>
    public Statement ParseKill()
    {
        var modifierKeyword =
            ParseOneOfKeywords(Keyword.CONNECTION, Keyword.QUERY, Keyword.MUTATION);

        var id = ParseLiteralUint();

        KillType? modifier;
        switch (modifierKeyword)
        {
            case Keyword.CONNECTION:
                modifier = new KillType.Connection();
                break;
            case Keyword.QUERY:
                modifier = new KillType.Query();
                break;
            case Keyword.MUTATION:
                if (_dialect is ClickHouseDialect or GenericDialect)
                {
                    modifier = new KillType.Mutation();
                }
                else
                {
                    modifier = ExpectedRef<KillType?>(
                        "Unsupported type for KILL, allowed: CONNECTION | QUERY",
                        PeekTokenRef());
                }
                break;
            default:
                modifier = null;
                break;
        }

        return new Statement.Kill(Modifier: modifier, Id: id);
    }

    /// <summary>Parse an <c>EXPLAIN</c> statement, handling dialect-specific options and modifiers.</summary>
    public Statement ParseExplain(DescribeAlias describeAlias)
    {
        var analyze = false;
        var verbose = false;
        var queryPlan = false;
        var estimate = false;
        AnalyzeFormatKind? format = null;
        Sequence<UtilityOption>? options = null;

        // Note: DuckDB is compatible with PostgreSQL syntax for this statement,
        // although not all features may be implemented.
        if (describeAlias is DescribeAlias.Explain
            && _dialect.SupportsExplainWithUtilityOptions
            && PeekTokenRef().Token is Token.LParen)
        {
            options = ParseUtilityOptions();
        }
        else if (ParseKeywords(Keyword.QUERY, Keyword.PLAN))
        {
            queryPlan = true;
        }
        else if (ParseKeyword(Keyword.ESTIMATE))
        {
            estimate = true;
        }
        else
        {
            analyze = ParseKeyword(Keyword.ANALYZE);
            verbose = ParseKeyword(Keyword.VERBOSE);
            if (ParseKeyword(Keyword.FORMAT))
            {
                format = ParseAnalyzeFormatKind();
            }
        }

        switch (MaybeParse(parser => parser.ParseStatement()))
        {
            case Statement.Explain or Statement.ExplainTable:
                throw new ParserException(new ParserError.ParserErrorValue(
                    "Explain must be root of the plan"));
            case { } statement:
                return new Statement.Explain(
                    DescribeAlias: describeAlias,
                    Analyze: analyze,
                    Verbose: verbose,
                    QueryPlan: queryPlan,
                    Estimate: estimate,
                    Statement: statement,
                    Format: format,
                    Options: options);
            default:
            {
                HiveDescribeFormat? hiveFormat;
                switch (ParseOneOfKeywords(Keyword.EXTENDED, Keyword.FORMATTED))
                {
                    case Keyword.EXTENDED:
                        hiveFormat = new HiveDescribeFormat.Extended();
                        break;
                    case Keyword.FORMATTED:
                        hiveFormat = new HiveDescribeFormat.Formatted();
                        break;
                    default:
                        hiveFormat = null;
                        break;
                }

                bool hasTableKeyword;
                if (_dialect.DescribeRequiresTableKeyword)
                {
                    // only allow to use TABLE keyword for DESC|DESCRIBE statement
                    hasTableKeyword = ParseKeyword(Keyword.TABLE);
                }
                else
                {
                    hasTableKeyword = false;
                }

                var tableName = ParseObjectName(false);
                return new Statement.ExplainTable(
                    DescribeAlias: describeAlias,
                    HiveFormat: hiveFormat,
                    HasTableKeyword: hasTableKeyword,
                    TableName: tableName);
            }
        }
    }
}
