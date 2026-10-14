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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 14520–15606:
// parse_query … maybe_parse_connect_by).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>
    /// Parse a query expression, i.e. a <c>SELECT</c> statement optionally
    /// preceded with some <c>WITH</c> CTE declarations and optionally followed
    /// by <c>ORDER BY</c>. Unlike some other parse_... methods, this one doesn't
    /// expect the initial keyword to be already consumed
    /// </summary>
    public Query ParseQuery()
    {
        using var _guard = _recursionCounter.TryDecrease();
        With? @with;
        if (ParseKeyword(Keyword.WITH))
        {
            var withToken = GetCurrentToken();
            @with = new With(
                WithToken: AttachedToken.From(withToken),
                Recursive: ParseKeyword(Keyword.RECURSIVE),
                CteTables: ParseCommaSeparated(p => p.ParseCte()));
        }
        else
        {
            @with = null;
        }
        if (ParseKeyword(Keyword.INSERT))
        {
            return new Query(
                With: @with,
                Body: ParseInsertSetexprBoxed(GetCurrentToken()),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty);
        }
        else if (ParseKeyword(Keyword.UPDATE))
        {
            return new Query(
                With: @with,
                Body: ParseUpdateSetexprBoxed(GetCurrentToken()),
                OrderBy: null,
                LimitClause: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty);
        }
        else if (ParseKeyword(Keyword.DELETE))
        {
            return new Query(
                With: @with,
                Body: ParseDeleteSetexprBoxed(GetCurrentToken()),
                LimitClause: null,
                OrderBy: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty);
        }
        else if (ParseKeyword(Keyword.MERGE))
        {
            return new Query(
                With: @with,
                Body: ParseMergeSetexprBoxed(GetCurrentToken()),
                LimitClause: null,
                OrderBy: null,
                Fetch: null,
                Locks: Sequence<LockClause>.Empty,
                ForClause: null,
                Settings: null,
                FormatClause: null,
                PipeOperators: Sequence<PipeOperator>.Empty);
        }
        else
        {
            var body = ParseQueryBody(_dialect.PrecUnknown);

            var orderBy = ParseOptionalOrderBy();

            var limitClause = ParseOptionalLimitClause();

            var settings = ParseSettings();

            Fetch? fetch;
            if (ParseKeyword(Keyword.FETCH))
            {
                fetch = ParseFetch();
            }
            else
            {
                fetch = null;
            }

            ForClause? forClause = null;
            var locks = new List<LockClause>();
            while (ParseKeyword(Keyword.FOR))
            {
                if (ParseForClause() is { } parsedForClause)
                {
                    forClause = parsedForClause;
                    break;
                }
                else
                {
                    locks.Add(ParseLock());
                }
            }

            // Some databases (e.g. PostgreSQL) accept `LIMIT`/`OFFSET` after the
            // row-locking clause (`... FOR UPDATE SKIP LOCKED LIMIT 5`) as well
            // as before it. The locking clause above is parsed for every
            // dialect, so accept a trailing limit here too rather than gating it
            // behind a dialect flag.
            if (limitClause is null)
            {
                limitClause = ParseOptionalLimitClause();
            }

            FormatClause? formatClause;
            if (_dialect.SupportsSelectFormat && ParseKeyword(Keyword.FORMAT))
            {
                if (ParseKeyword(Keyword.NULL))
                {
                    formatClause = new FormatClause.Null();
                }
                else
                {
                    var ident = ParseIdentifier();
                    formatClause = new FormatClause.Identifier(ident);
                }
            }
            else
            {
                formatClause = null;
            }

            Sequence<PipeOperator> pipeOperators;
            if (_dialect.SupportsPipeOperator)
            {
                pipeOperators = ParsePipeOperators();
            }
            else
            {
                pipeOperators = Sequence<PipeOperator>.Empty;
            }

            return new Query(
                With: @with,
                Body: body,
                OrderBy: orderBy,
                LimitClause: limitClause,
                Fetch: fetch,
                Locks: locks.ToSequence(),
                ForClause: forClause,
                Settings: settings,
                FormatClause: formatClause,
                PipeOperators: pipeOperators);
        }
    }

    internal Sequence<PipeOperator> ParsePipeOperators()
    {
        var pipeOperators = new List<PipeOperator>();

        while (ConsumeToken(new Token.VerticalBarRightAngleBracket()))
        {
            var kw = ExpectOneOfKeywords(
                Keyword.SELECT,
                Keyword.EXTEND,
                Keyword.SET,
                Keyword.DROP,
                Keyword.AS,
                Keyword.WHERE,
                Keyword.LIMIT,
                Keyword.AGGREGATE,
                Keyword.ORDER,
                Keyword.TABLESAMPLE,
                Keyword.RENAME,
                Keyword.UNION,
                Keyword.INTERSECT,
                Keyword.EXCEPT,
                Keyword.CALL,
                Keyword.PIVOT,
                Keyword.UNPIVOT,
                Keyword.JOIN,
                Keyword.INNER,
                Keyword.LEFT,
                Keyword.RIGHT,
                Keyword.FULL,
                Keyword.CROSS);
            switch (kw)
            {
                case Keyword.SELECT:
                {
                    var exprs = ParseCommaSeparated(p => p.ParseSelectItem());
                    pipeOperators.Add(new PipeOperator.Select(Exprs: exprs));
                    break;
                }
                case Keyword.EXTEND:
                {
                    var exprs = ParseCommaSeparated(p => p.ParseSelectItem());
                    pipeOperators.Add(new PipeOperator.Extend(Exprs: exprs));
                    break;
                }
                case Keyword.SET:
                {
                    var assignments = ParseCommaSeparated(p => p.ParseAssignment());
                    pipeOperators.Add(new PipeOperator.Set(Assignments: assignments));
                    break;
                }
                case Keyword.DROP:
                {
                    var columns = ParseIdentifiers();
                    pipeOperators.Add(new PipeOperator.Drop(Columns: columns));
                    break;
                }
                case Keyword.AS:
                {
                    var alias = ParseIdentifier();
                    pipeOperators.Add(new PipeOperator.As(Alias: alias));
                    break;
                }
                case Keyword.WHERE:
                {
                    var expr = ParseExpr();
                    pipeOperators.Add(new PipeOperator.Where(Expr: expr));
                    break;
                }
                case Keyword.LIMIT:
                {
                    var expr = ParseExpr();
                    Expr? offset;
                    if (ParseKeyword(Keyword.OFFSET))
                    {
                        offset = ParseExpr();
                    }
                    else
                    {
                        offset = null;
                    }
                    pipeOperators.Add(new PipeOperator.Limit(Expr: expr, Offset: offset));
                    break;
                }
                case Keyword.AGGREGATE:
                {
                    Sequence<ExprWithAliasAndOrderBy> fullTableExprs;
                    if (PeekKeyword(Keyword.GROUP))
                    {
                        fullTableExprs = Sequence<ExprWithAliasAndOrderBy>.Empty;
                    }
                    else
                    {
                        fullTableExprs = ParseCommaSeparated(parser =>
                            parser.ParseExprWithAliasAndOrderBy());
                    }

                    Sequence<ExprWithAliasAndOrderBy> groupByExpr;
                    if (ParseKeywords(Keyword.GROUP, Keyword.BY))
                    {
                        groupByExpr = ParseCommaSeparated(parser =>
                            parser.ParseExprWithAliasAndOrderBy());
                    }
                    else
                    {
                        groupByExpr = Sequence<ExprWithAliasAndOrderBy>.Empty;
                    }

                    pipeOperators.Add(new PipeOperator.Aggregate(
                        FullTableExprs: fullTableExprs,
                        GroupByExpr: groupByExpr));
                    break;
                }
                case Keyword.ORDER:
                {
                    ExpectOneOfKeywords(Keyword.BY);
                    var exprs = ParseCommaSeparated(p => p.ParseOrderByExpr());
                    pipeOperators.Add(new PipeOperator.OrderBy(Exprs: exprs));
                    break;
                }
                case Keyword.TABLESAMPLE:
                {
                    var sample = ParseTableSample(new TableSampleModifier.TableSample());
                    pipeOperators.Add(new PipeOperator.TableSample(Sample: sample));
                    break;
                }
                case Keyword.RENAME:
                {
                    var mappings =
                        ParseCommaSeparated(p => p.ParseIdentifierWithOptionalAlias());
                    pipeOperators.Add(new PipeOperator.Rename(Mappings: mappings));
                    break;
                }
                case Keyword.UNION:
                {
                    var setQuantifier = ParseSetQuantifier(new SetOperator.Union());
                    var queries = ParsePipeOperatorQueries();
                    pipeOperators.Add(new PipeOperator.Union(
                        SetQuantifier: setQuantifier,
                        Queries: queries));
                    break;
                }
                case Keyword.INTERSECT:
                {
                    var setQuantifier =
                        ParseDistinctRequiredSetQuantifier("INTERSECT");
                    var queries = ParsePipeOperatorQueries();
                    pipeOperators.Add(new PipeOperator.Intersect(
                        SetQuantifier: setQuantifier,
                        Queries: queries));
                    break;
                }
                case Keyword.EXCEPT:
                {
                    var setQuantifier = ParseDistinctRequiredSetQuantifier("EXCEPT");
                    var queries = ParsePipeOperatorQueries();
                    pipeOperators.Add(new PipeOperator.Except(
                        SetQuantifier: setQuantifier,
                        Queries: queries));
                    break;
                }
                case Keyword.CALL:
                {
                    var functionName = ParseObjectName(false);
                    var functionExpr = ParseFunction(functionName);
                    if (functionExpr is Expr.Function { Value: var function })
                    {
                        var alias = ParseIdentifierOptionalAlias();
                        pipeOperators.Add(new PipeOperator.Call(Function: function, Alias: alias));
                    }
                    else
                    {
                        throw new ParserException(new ParserError.ParserErrorValue(
                            "Expected function call after CALL"));
                    }
                    break;
                }
                case Keyword.PIVOT:
                {
                    ExpectToken(new Token.LParen());
                    var aggregateFunctions =
                        ParseCommaSeparated(p => p.ParsePivotAggregateFunction());
                    ExpectKeywordIs(Keyword.FOR);
                    var valueColumn = ParsePeriodSeparated(p => p.ParseIdentifier());
                    ExpectKeywordIs(Keyword.IN);

                    ExpectToken(new Token.LParen());
                    PivotValueSource valueSource;
                    if (ParseKeyword(Keyword.ANY))
                    {
                        Sequence<OrderByExpr> orderBy;
                        if (ParseKeywords(Keyword.ORDER, Keyword.BY))
                        {
                            orderBy = ParseCommaSeparated(p => p.ParseOrderByExpr());
                        }
                        else
                        {
                            orderBy = Sequence<OrderByExpr>.Empty;
                        }
                        valueSource = new PivotValueSource.Any(orderBy);
                    }
                    else if (PeekSubQuery())
                    {
                        valueSource = new PivotValueSource.Subquery(ParseQuery());
                    }
                    else
                    {
                        valueSource = new PivotValueSource.List(
                            ParseCommaSeparated(p => p.ParseExprWithAlias()));
                    }
                    ExpectToken(new Token.RParen());
                    ExpectToken(new Token.RParen());

                    var alias = ParseIdentifierOptionalAlias();

                    pipeOperators.Add(new PipeOperator.Pivot(
                        AggregateFunctions: aggregateFunctions,
                        ValueColumn: valueColumn,
                        ValueSource: valueSource,
                        Alias: alias));
                    break;
                }
                case Keyword.UNPIVOT:
                {
                    ExpectToken(new Token.LParen());
                    var valueColumn = ParseIdentifier();
                    ExpectKeyword(Keyword.FOR);
                    var nameColumn = ParseIdentifier();
                    ExpectKeyword(Keyword.IN);

                    ExpectToken(new Token.LParen());
                    var unpivotColumns = ParseCommaSeparated(p => p.ParseIdentifier());
                    ExpectToken(new Token.RParen());

                    ExpectToken(new Token.RParen());

                    var alias = ParseIdentifierOptionalAlias();

                    pipeOperators.Add(new PipeOperator.Unpivot(
                        ValueColumn: valueColumn,
                        NameColumn: nameColumn,
                        UnpivotColumns: unpivotColumns,
                        Alias: alias));
                    break;
                }
                case Keyword.JOIN
                    or Keyword.INNER
                    or Keyword.LEFT
                    or Keyword.RIGHT
                    or Keyword.FULL
                    or Keyword.CROSS:
                {
                    PrevToken();
                    var joins = ParseJoins();
                    if (joins.Count != 1)
                    {
                        throw new ParserException(new ParserError.ParserErrorValue(
                            "Join pipe operator must have a single join"));
                    }
                    // Rust: `joins.swap_remove(0)` com um único elemento.
                    var join = joins[0];
                    pipeOperators.Add(new PipeOperator.Join(join));
                    break;
                }
                case var unhandled:
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"`expect_one_of_keywords` further up allowed unhandled keyword: {unhandled.ToString()}"));
            }
        }
        return pipeOperators.ToSequence();
    }

    internal Sequence<Setting>? ParseSettings()
    {
        Sequence<Setting>? settings;
        if (_dialect.SupportsSettings && ParseKeyword(Keyword.SETTINGS))
        {
            var keyValues = ParseCommaSeparated(p =>
            {
                var key = p.ParseIdentifier();
                p.ExpectToken(new Token.Eq());
                var value = p.ParseExpr();
                return new Setting(Key: key, Value: value);
            });
            settings = keyValues;
        }
        else
        {
            settings = null;
        }
        return settings;
    }

    /// <summary>Parse a mssql <c>FOR [XML | JSON | BROWSE]</c> clause</summary>
    public ForClause? ParseForClause()
    {
        if (ParseKeyword(Keyword.XML))
        {
            return ParseForXml();
        }
        else if (ParseKeyword(Keyword.JSON))
        {
            return ParseForJson();
        }
        else if (ParseKeyword(Keyword.BROWSE))
        {
            return new ForClause.Browse();
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse a mssql <c>FOR XML</c> clause</summary>
    public ForClause ParseForXml()
    {
        ForXml forXml;
        if (ParseKeyword(Keyword.RAW))
        {
            string? elementName = null;
            if (PeekTokenRef().Token is Token.LParen)
            {
                ExpectToken(new Token.LParen());
                elementName = ParseLiteralString();
                ExpectToken(new Token.RParen());
            }
            forXml = new ForXml.Raw(elementName);
        }
        else if (ParseKeyword(Keyword.AUTO))
        {
            forXml = new ForXml.Auto();
        }
        else if (ParseKeyword(Keyword.EXPLICIT))
        {
            forXml = new ForXml.Explicit();
        }
        else if (ParseKeyword(Keyword.PATH))
        {
            string? elementName = null;
            if (PeekTokenRef().Token is Token.LParen)
            {
                ExpectToken(new Token.LParen());
                elementName = ParseLiteralString();
                ExpectToken(new Token.RParen());
            }
            forXml = new ForXml.Path(elementName);
        }
        else
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                "Expected FOR XML [RAW | AUTO | EXPLICIT | PATH ]"));
        }
        var elements = false;
        var binaryBase64 = false;
        string? root = null;
        var type = false;
        while (PeekTokenRef().Token is Token.Comma)
        {
            NextToken();
            if (ParseKeyword(Keyword.ELEMENTS))
            {
                elements = true;
            }
            else if (ParseKeyword(Keyword.BINARY))
            {
                ExpectKeywordIs(Keyword.BASE64);
                binaryBase64 = true;
            }
            else if (ParseKeyword(Keyword.ROOT))
            {
                ExpectToken(new Token.LParen());
                root = ParseLiteralString();
                ExpectToken(new Token.RParen());
            }
            else if (ParseKeyword(Keyword.TYPE))
            {
                type = true;
            }
        }
        return new ForClause.Xml(
            ForXml: forXml,
            Elements: elements,
            BinaryBase64: binaryBase64,
            Root: root,
            Type: type);
    }

    /// <summary>Parse a mssql <c>FOR JSON</c> clause</summary>
    public ForClause ParseForJson()
    {
        ForJson forJson;
        if (ParseKeyword(Keyword.AUTO))
        {
            forJson = new ForJson.Auto();
        }
        else if (ParseKeyword(Keyword.PATH))
        {
            forJson = new ForJson.Path();
        }
        else
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                "Expected FOR JSON [AUTO | PATH ]"));
        }
        string? root = null;
        var includeNullValues = false;
        var withoutArrayWrapper = false;
        while (PeekTokenRef().Token is Token.Comma)
        {
            NextToken();
            if (ParseKeyword(Keyword.ROOT))
            {
                ExpectToken(new Token.LParen());
                root = ParseLiteralString();
                ExpectToken(new Token.RParen());
            }
            else if (ParseKeyword(Keyword.INCLUDE_NULL_VALUES))
            {
                includeNullValues = true;
            }
            else if (ParseKeyword(Keyword.WITHOUT_ARRAY_WRAPPER))
            {
                withoutArrayWrapper = true;
            }
        }
        return new ForClause.Json(
            ForJson: forJson,
            Root: root,
            IncludeNullValues: includeNullValues,
            WithoutArrayWrapper: withoutArrayWrapper);
    }

    /// <summary>Parse a CTE (<c>alias [( col1, col2, ... )] [AS] (subquery)</c>)</summary>
    public Cte ParseCte()
    {
        var name = ParseIdentifier();

        var asOptional = _dialect.SupportsCteWithoutAs;

        // If AS is optional, first try to parse `name (query)` directly
        if (asOptional && !PeekKeyword(Keyword.AS))
        {
            // Rust: `if let Some((query, closing_paren_token)) = self.maybe_parse(..)?`; o `cte` deste
            // bloco sombreia o de fora (caso 72: `directCte`; a tupla fica em `parsed`).
            if (MaybeParseValue(p =>
                {
                    p.ExpectToken(new Token.LParen());
                    var query = p.ParseQuery();
                    var closingParenToken = p.ExpectToken(new Token.RParen());
                    return (Query: query, ClosingParenToken: closingParenToken);
                }) is { } parsed)
            {
                var directCte = new Cte(
                    Alias: new TableAlias(
                        Explicit: false,
                        Name: name,
                        Columns: Sequence<TableAliasColumnDef>.Empty,
                        At: null),
                    Query: parsed.Query,
                    From: null,
                    Materialized: null,
                    ClosingParenToken: AttachedToken.From(parsed.ClosingParenToken));
                if (ParseKeyword(Keyword.FROM))
                {
                    directCte = directCte with { From = ParseIdentifier() };
                }
                return directCte;
            }
        }

        // Determine column definitions and consume AS
        Sequence<TableAliasColumnDef> columns;
        if (ParseKeyword(Keyword.AS))
        {
            columns = Sequence<TableAliasColumnDef>.Empty;
        }
        else
        {
            columns = ParseTableAliasColumnDefs();
            if (asOptional)
            {
                _ = ParseKeyword(Keyword.AS);
            }
            else
            {
                ExpectKeywordIs(Keyword.AS);
            }
        }

        CteAsMaterialized? isMaterialized = null;
        if (_dialect is PostgreSqlDialect)
        {
            if (ParseKeyword(Keyword.MATERIALIZED))
            {
                isMaterialized = new CteAsMaterialized.Materialized();
            }
            else if (ParseKeywords(Keyword.NOT, Keyword.MATERIALIZED))
            {
                isMaterialized = new CteAsMaterialized.NotMaterialized();
            }
        }

        ExpectToken(new Token.LParen());
        var query = ParseQuery();
        var closingParenToken = ExpectToken(new Token.RParen());

        var cte = new Cte(
            Alias: new TableAlias(
                Explicit: false,
                Name: name,
                Columns: columns,
                At: null),
            Query: query,
            From: null,
            Materialized: isMaterialized,
            ClosingParenToken: AttachedToken.From(closingParenToken));
        if (_dialect.SupportsFromFirstInsert && ParseKeyword(Keyword.FROM))
        {
            cte = cte with { From = ParseIdentifier() };
        }
        return cte;
    }

    /// <summary>
    /// Parse a "query body", which is an expression with roughly the
    /// following grammar:
    /// <code>
    ///   query_body ::= restricted_select | '(' subquery ')' | set_operation
    ///   restricted_select ::= 'SELECT' [expr_list] [ from ] [ where ] [ groupby_having ]
    ///   subquery ::= query_body [ order_by_limit ]
    ///   set_operation ::= query_body { 'UNION' | 'EXCEPT' | 'INTERSECT' } [ 'ALL' ] query_body
    /// </code>
    /// </summary>
    public SetExpr ParseQueryBody(byte precedence)
    {
        // We parse the expression using a Pratt parser, as in `parse_expr()`.
        // Start by parsing a restricted SELECT or a `(subquery)`:
        SetExpr expr;
        if (PeekKeyword(Keyword.SELECT)
            || (PeekKeyword(Keyword.FROM) && _dialect.SupportsFromFirstSelect))
        {
            expr = new SetExpr.Select(ParseSelect());
        }
        else if (ConsumeToken(new Token.LParen()))
        {
            // CTEs are not allowed here, but the parser currently accepts them
            var subquery = ParseQuery();
            ExpectToken(new Token.RParen());
            expr = new SetExpr.Query(subquery);
        }
        else if (ParseKeyword(Keyword.VALUES))
        {
            var isMysql = _dialect is MySqlDialect;
            expr = new SetExpr.Values(ParseValues(isMysql, false));
        }
        else if (ParseKeyword(Keyword.VALUE))
        {
            var isMysql = _dialect is MySqlDialect;
            expr = new SetExpr.Values(ParseValues(isMysql, true));
        }
        else if (ParseKeyword(Keyword.TABLE))
        {
            expr = new SetExpr.Table(ParseAsTable());
        }
        else
        {
            return ExpectedRef<SetExpr>(
                "SELECT, VALUES, or a subquery in the query body",
                PeekTokenRef());
        }

        return ParseRemainingSetExprs(expr, precedence);
    }

    /// <summary>Parse any extra set expressions that may be present in a query body</summary>
    /// <remarks>(this is its own function to reduce required stack size in debug builds)</remarks>
    internal SetExpr ParseRemainingSetExprs(SetExpr expr, byte precedence)
    {
        while (true)
        {
            // The query can be optionally followed by a set operator:
            var op = ParseSetOperator(PeekToken().Token);
            byte nextPrecedence;
            switch (op)
            {
                // UNION and EXCEPT have the same binding power and evaluate left-to-right
                case SetOperator.Union or SetOperator.Except or SetOperator.Minus:
                    nextPrecedence = 10;
                    break;
                // INTERSECT has higher precedence than UNION/EXCEPT
                case SetOperator.Intersect:
                    nextPrecedence = 20;
                    break;
                // Unexpected token or EOF => stop parsing the query body
                case null:
                    // Rust: `None => break`; depois do laço só há `Ok(expr.into())`.
                    return expr;
                default:
                    throw new InvalidOperationException($"Variante de SetOperator não tratada: {op.GetType().Name}");
            }
            if (precedence >= nextPrecedence)
            {
                break;
            }
            NextToken(); // skip past the set operator
            var setQuantifier = ParseSetQuantifier(op);
            expr = new SetExpr.SetOperation(
                Left: expr,
                Op: op,
                SetQuantifier: setQuantifier,
                Right: ParseQueryBody(nextPrecedence));
        }

        return expr;
    }

    /// <summary>Parse a set operator token into its <c>SetOperator</c> variant.</summary>
    public SetOperator? ParseSetOperator(Token token)
    {
        switch (token)
        {
            case Token.Word { Value: var w } when w.Keyword == Keyword.UNION:
                return new SetOperator.Union();
            case Token.Word { Value: var w } when w.Keyword == Keyword.EXCEPT:
                return new SetOperator.Except();
            case Token.Word { Value: var w } when w.Keyword == Keyword.INTERSECT:
                return new SetOperator.Intersect();
            case Token.Word { Value: var w } when w.Keyword == Keyword.MINUS:
                return new SetOperator.Minus();
            default:
                return null;
        }
    }

    /// <summary>Parse a set quantifier (e.g., <c>ALL</c>, <c>DISTINCT BY NAME</c>) for the given set operator.</summary>
    public SetQuantifier ParseSetQuantifier(SetOperator? op)
    {
        switch (op)
        {
            case SetOperator.Except
                or SetOperator.Intersect
                or SetOperator.Union
                or SetOperator.Minus:
                if (ParseKeywords(Keyword.DISTINCT, Keyword.BY, Keyword.NAME))
                {
                    return new SetQuantifier.DistinctByName();
                }
                else if (ParseKeywords(Keyword.BY, Keyword.NAME))
                {
                    return new SetQuantifier.ByName();
                }
                else if (ParseKeyword(Keyword.ALL))
                {
                    if (ParseKeywords(Keyword.BY, Keyword.NAME))
                    {
                        return new SetQuantifier.AllByName();
                    }
                    else
                    {
                        return new SetQuantifier.All();
                    }
                }
                else if (ParseKeyword(Keyword.DISTINCT))
                {
                    return new SetQuantifier.Distinct();
                }
                else
                {
                    return new SetQuantifier.None();
                }
            default:
                return new SetQuantifier.None();
        }
    }

    /// <summary>Parse a restricted <c>SELECT</c> statement (no CTEs / <c>UNION</c> / <c>ORDER BY</c>)</summary>
    public Select ParseSelect()
    {
        Sequence<TableWithJoins>? fromFirst = null;

        if (_dialect.SupportsFromFirstSelect && PeekKeyword(Keyword.FROM))
        {
            var fromToken = ExpectKeyword(Keyword.FROM);
            // Rust: `let from` deste bloco; `@from` é declarado adiante no método (caso 72).
            var fromTables = ParseTableWithJoins();
            if (!PeekKeyword(Keyword.SELECT))
            {
                return new Select(
                    SelectToken: new AttachedToken(fromToken),
                    OptimizerHints: Sequence<OptimizerHint>.Empty,
                    Distinct: null,
                    SelectModifiers: null,
                    Top: null,
                    TopBeforeDistinct: false,
                    Projection: Sequence<SelectItem>.Empty,
                    Exclude: null,
                    Into: null,
                    From: fromTables,
                    LateralViews: Sequence<LateralView>.Empty,
                    Prewhere: null,
                    Selection: null,
                    GroupBy: new GroupByExpr.Expressions(Sequence<Expr>.Empty, Sequence<GroupByWithModifier>.Empty),
                    ClusterBy: Sequence<Expr>.Empty,
                    DistributeBy: Sequence<Expr>.Empty,
                    SortBy: Sequence<OrderByExpr>.Empty,
                    Having: null,
                    NamedWindow: Sequence<NamedWindowDefinition>.Empty,
                    WindowBeforeQualify: false,
                    Qualify: null,
                    ValueTableMode: null,
                    ConnectBy: Sequence<ConnectByKind>.Empty,
                    Flavor: SelectFlavor.FromFirstNoSelect);
            }
            fromFirst = fromTables;
        }

        var selectToken = ExpectKeyword(Keyword.SELECT);
        var optimizerHints = MaybeParseOptimizerHints();
        var valueTableMode = ParseValueTableMode();

        SelectModifiers? selectModifiers;
        Distinct? distinctSelectModifier;
        if (_dialect.SupportsSelectModifiers)
        {
            (selectModifiers, distinctSelectModifier) = ParseSelectModifiers();
        }
        else
        {
            (selectModifiers, distinctSelectModifier) = (null, null);
        }

        var topBeforeDistinct = false;
        Top? top = null;
        if (_dialect.SupportsTopBeforeDistinct && ParseKeyword(Keyword.TOP))
        {
            top = ParseTop();
            topBeforeDistinct = true;
        }

        Distinct? distinct;
        if (distinctSelectModifier is not null)
        {
            distinct = distinctSelectModifier;
        }
        else
        {
            distinct = ParseAllOrDistinct();
        }

        if (!_dialect.SupportsTopBeforeDistinct && ParseKeyword(Keyword.TOP))
        {
            top = ParseTop();
        }

        Sequence<SelectItem> projection;
        if (_dialect.SupportsEmptyProjections && PeekKeyword(Keyword.FROM))
        {
            projection = Sequence<SelectItem>.Empty;
        }
        else
        {
            projection = ParseProjection();
        }

        ExcludeSelectItem? exclude;
        if (_dialect.SupportsSelectExclude)
        {
            exclude = ParseOptionalSelectItemExclude();
        }
        else
        {
            exclude = null;
        }

        SelectInto? into;
        if (ParseKeyword(Keyword.INTO))
        {
            into = ParseSelectInto();
        }
        else
        {
            into = null;
        }

        // Note that for keywords to be properly handled here, they need to be
        // added to `RESERVED_FOR_COLUMN_ALIAS` / `RESERVED_FOR_TABLE_ALIAS`,
        // otherwise they may be parsed as an alias as part of the `projection`
        // or `from`.

        // Rust: `let (from, from_first) = if let Some(from) = from_first.take() { .. }`; o
        // `from_first` booleano sombreia o `Option` (caso 72: `isFromFirst`).
        Sequence<TableWithJoins> @from;
        bool isFromFirst;
        if (fromFirst is { } takenFromFirst)
        {
            fromFirst = null;
            (@from, isFromFirst) = (takenFromFirst, true);
        }
        else if (ParseKeyword(Keyword.FROM))
        {
            (@from, isFromFirst) = (ParseTableWithJoins(), false);
        }
        else
        {
            (@from, isFromFirst) = (Sequence<TableWithJoins>.Empty, false);
        }

        var lateralViews = new List<LateralView>();
        while (true)
        {
            if (ParseKeywords(Keyword.LATERAL, Keyword.VIEW))
            {
                var outer = ParseKeyword(Keyword.OUTER);
                var lateralView = ParseExpr();
                var lateralViewName = ParseObjectName(false);
                // Rust: `.into_iter().flatten().collect()` sobre `Vec<Option<Ident>>`.
                var lateralColAlias = ParseCommaSeparated(parser =>
                        parser.ParseOptionalAlias(
                            Keyword.WHERE,
                            Keyword.GROUP,
                            Keyword.CLUSTER,
                            Keyword.HAVING,
                            Keyword.LATERAL)) // This couldn't possibly be a bad idea
                    .OfType<Ident>()
                    .ToSequence();

                lateralViews.Add(new LateralView(
                    LateralViewValue: lateralView,
                    LateralViewName: lateralViewName,
                    LateralColAlias: lateralColAlias,
                    Outer: outer));
            }
            else
            {
                break;
            }
        }

        Expr? prewhere;
        if (_dialect.SupportsPrewhere && ParseKeyword(Keyword.PREWHERE))
        {
            prewhere = ParseExpr();
        }
        else
        {
            prewhere = null;
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

        var connectBy = MaybeParseConnectBy();

        var groupBy = ParseOptionalGroupBy()
            ?? new GroupByExpr.Expressions(Sequence<Expr>.Empty, Sequence<GroupByWithModifier>.Empty);

        Sequence<Expr> clusterBy;
        if (ParseKeywords(Keyword.CLUSTER, Keyword.BY))
        {
            clusterBy = ParseCommaSeparated(p => p.ParseExpr());
        }
        else
        {
            clusterBy = Sequence<Expr>.Empty;
        }

        Sequence<Expr> distributeBy;
        if (ParseKeywords(Keyword.DISTRIBUTE, Keyword.BY))
        {
            distributeBy = ParseCommaSeparated(p => p.ParseExpr());
        }
        else
        {
            distributeBy = Sequence<Expr>.Empty;
        }

        Sequence<OrderByExpr> sortBy;
        if (ParseKeywords(Keyword.SORT, Keyword.BY))
        {
            sortBy = ParseCommaSeparated(p => p.ParseOrderByExpr());
        }
        else
        {
            sortBy = Sequence<OrderByExpr>.Empty;
        }

        Expr? having;
        if (ParseKeyword(Keyword.HAVING))
        {
            having = ParseExpr();
        }
        else
        {
            having = null;
        }

        // Accept QUALIFY and WINDOW in any order and flag accordingly.
        Sequence<NamedWindowDefinition> namedWindows;
        Expr? qualify;
        bool windowBeforeQualify;
        if (ParseKeyword(Keyword.WINDOW))
        {
            // Rust: `let named_windows` interno sombreia o externo (caso 72).
            var parsedNamedWindows = ParseCommaSeparated(p => p.ParseNamedWindow());
            if (ParseKeyword(Keyword.QUALIFY))
            {
                (namedWindows, qualify, windowBeforeQualify) = (parsedNamedWindows, ParseExpr(), true);
            }
            else
            {
                (namedWindows, qualify, windowBeforeQualify) = (parsedNamedWindows, null, true);
            }
        }
        else if (ParseKeyword(Keyword.QUALIFY))
        {
            // Rust: `let qualify` interno sombreia o externo (caso 72).
            Expr? parsedQualify = ParseExpr();
            if (ParseKeyword(Keyword.WINDOW))
            {
                (namedWindows, qualify, windowBeforeQualify) = (
                    ParseCommaSeparated(p => p.ParseNamedWindow()),
                    parsedQualify,
                    false);
            }
            else
            {
                (namedWindows, qualify, windowBeforeQualify) = (Sequence<NamedWindowDefinition>.Empty, parsedQualify, false);
            }
        }
        else
        {
            // Rust: `Default::default()` da tupla.
            (namedWindows, qualify, windowBeforeQualify) = (Sequence<NamedWindowDefinition>.Empty, null, false);
        }

        return new Select(
            SelectToken: new AttachedToken(selectToken),
            OptimizerHints: optimizerHints,
            Distinct: distinct,
            SelectModifiers: selectModifiers,
            Top: top,
            TopBeforeDistinct: topBeforeDistinct,
            Projection: projection,
            Exclude: exclude,
            Into: into,
            From: @from,
            LateralViews: lateralViews.ToSequence(),
            Prewhere: prewhere,
            Selection: selection,
            GroupBy: groupBy,
            ClusterBy: clusterBy,
            DistributeBy: distributeBy,
            SortBy: sortBy,
            Having: having,
            NamedWindow: namedWindows,
            WindowBeforeQualify: windowBeforeQualify,
            Qualify: qualify,
            ValueTableMode: valueTableMode,
            ConnectBy: connectBy,
            Flavor: isFromFirst
                ? SelectFlavor.FromFirst
                : SelectFlavor.Standard);
    }

    /// <summary>Parses optimizer hints at the current token position.</summary>
    /// <remarks>
    /// Collects all <c>/*prefix+...*/</c> and <c>--prefix+...</c> patterns.
    /// The <c>prefix</c> is any run of ASCII alphanumeric characters between the
    /// comment marker and <c>+</c> (e.g. <c>""</c> for <c>/*+...*/</c>, <c>"abc"</c> for <c>/*abc+...*/</c>).
    /// <para>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/optimizer-hints.html#optimizer-hints-overview">MySQL</see>
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Comments.html#GUID-D316D545-89E2-4D54-977F-FC97815CD62E">Oracle</see>
    /// </para>
    /// </remarks>
    internal Sequence<OptimizerHint> MaybeParseOptimizerHints()
    {
        var supportsHints = _dialect.SupportsCommentOptimizerHint;
        if (!supportsHints)
        {
            return Sequence<OptimizerHint>.Empty;
        }
        var hints = new List<OptimizerHint>();
        while (true)
        {
            var t = PeekNthTokenNoSkipRef(0);
            if (t.Token is not Token.Whitespace { Value: var ws })
            {
                break;
            }
            switch (ws)
            {
                case Whitespace.SingleLineComment { Comment: var comment, Prefix: var prefix }:
                    if (ExtractHintPrefixAndText(comment) is { } singleLineHint)
                    {
                        hints.Add(new OptimizerHint(
                            Prefix: singleLineHint.Prefix,
                            Text: singleLineHint.Text,
                            Style: new OptimizerHintStyle.SingleLine(
                                Prefix: prefix)));
                    }
                    NextTokenNoSkip();
                    break;
                case Whitespace.MultiLineComment { Value: var comment }:
                    if (ExtractHintPrefixAndText(comment) is { } multiLineHint)
                    {
                        hints.Add(new OptimizerHint(
                            Prefix: multiLineHint.Prefix,
                            Text: multiLineHint.Text,
                            Style: new OptimizerHintStyle.MultiLine()));
                    }
                    NextTokenNoSkip();
                    break;
                case Whitespace.Space or Whitespace.Tab or Whitespace.Newline:
                    NextTokenNoSkip();
                    break;
                default:
                    throw new InvalidOperationException($"Variante de Whitespace não tratada: {ws.GetType().Name}");
            }
        }
        return hints.ToSequence();
    }

    /// <summary>
    /// Checks if a comment's content starts with <c>[ASCII-alphanumeric]*+</c>
    /// and returns <c>(prefix, text_after_plus)</c> if so.
    /// </summary>
    internal static (string Prefix, string Text)? ExtractHintPrefixAndText(string comment)
    {
        // Rust: `comment.split_once('+')?`.
        var plusIndex = comment.IndexOf('+');
        if (plusIndex < 0)
        {
            return null;
        }
        var beforePlus = comment.Substring(0, plusIndex);
        var text = comment.Substring(plusIndex + 1);
        // `is_ascii_alphanumeric()`: comparação explícita de intervalo (convencoes.md §10).
        if (beforePlus.All(c => RustChar.IsAsciiDigit(c) || RustChar.IsAsciiLowercase(c) || RustChar.IsAsciiUppercase(c)))
        {
            return (beforePlus, text);
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parses MySQL SELECT modifiers and DISTINCT/ALL in any order.</summary>
    /// <remarks>
    /// Manual testing shows odifiers can appear in any order, and modifiers other than DISTINCT/ALL
    /// can be repeated.
    /// <para><see href="https://dev.mysql.com/doc/refman/8.4/en/select.html"/></para>
    /// </remarks>
    internal (SelectModifiers? SelectModifiers, Distinct? Distinct) ParseSelectModifiers()
    {
        var modifiers = SelectModifiers.Default();
        Distinct? distinct = null;

        var keywords = new[]
        {
            Keyword.ALL,
            Keyword.DISTINCT,
            Keyword.DISTINCTROW,
            Keyword.HIGH_PRIORITY,
            Keyword.STRAIGHT_JOIN,
            Keyword.SQL_SMALL_RESULT,
            Keyword.SQL_BIG_RESULT,
            Keyword.SQL_BUFFER_RESULT,
            Keyword.SQL_NO_CACHE,
            Keyword.SQL_CALC_FOUND_ROWS,
        };

        while (ParseOneOfKeywords(keywords) is { } keyword)
        {
            switch (keyword)
            {
                case Keyword.ALL or Keyword.DISTINCT when distinct is null:
                    PrevToken();
                    distinct = ParseAllOrDistinct();
                    break;
                // DISTINCTROW is a MySQL-specific legacy (but not deprecated) alias for DISTINCT
                case Keyword.DISTINCTROW when distinct is null:
                    distinct = new Distinct.DistinctValue();
                    break;
                // Rust: `modifiers.x = true` sobre record imutável → `with` (decisão 3 da B3).
                case Keyword.HIGH_PRIORITY:
                    modifiers = modifiers with { HighPriority = true };
                    break;
                case Keyword.STRAIGHT_JOIN:
                    modifiers = modifiers with { StraightJoin = true };
                    break;
                case Keyword.SQL_SMALL_RESULT:
                    modifiers = modifiers with { SqlSmallResult = true };
                    break;
                case Keyword.SQL_BIG_RESULT:
                    modifiers = modifiers with { SqlBigResult = true };
                    break;
                case Keyword.SQL_BUFFER_RESULT:
                    modifiers = modifiers with { SqlBufferResult = true };
                    break;
                case Keyword.SQL_NO_CACHE:
                    modifiers = modifiers with { SqlNoCache = true };
                    break;
                case Keyword.SQL_CALC_FOUND_ROWS:
                    modifiers = modifiers with { SqlCalcFoundRows = true };
                    break;
                default:
                    PrevToken();
                    return ExpectedRef<(SelectModifiers?, Distinct?)>(
                        "HIGH_PRIORITY, STRAIGHT_JOIN, or other MySQL select modifier",
                        PeekTokenRef());
            }
        }

        // Avoid polluting the AST with `Some(SelectModifiers::default())` empty value unless there
        // actually were some modifiers set.
        SelectModifiers? selectModifiers;
        if (modifiers.IsAnySet())
        {
            selectModifiers = modifiers;
        }
        else
        {
            selectModifiers = null;
        }
        return (selectModifiers, distinct);
    }

    internal ValueTableMode? ParseValueTableMode()
    {
        if (!(_dialect is BigQueryDialect))
        {
            return null;
        }

        ValueTableMode? mode;
        if (ParseKeywords(Keyword.DISTINCT, Keyword.AS, Keyword.VALUE))
        {
            mode = new ValueTableMode.DistinctAsValue();
        }
        else if (ParseKeywords(Keyword.DISTINCT, Keyword.AS, Keyword.STRUCT))
        {
            mode = new ValueTableMode.DistinctAsStruct();
        }
        else if (ParseKeywords(Keyword.AS, Keyword.VALUE)
            || ParseKeywords(Keyword.ALL, Keyword.AS, Keyword.VALUE))
        {
            mode = new ValueTableMode.AsValue();
        }
        else if (ParseKeywords(Keyword.AS, Keyword.STRUCT)
            || ParseKeywords(Keyword.ALL, Keyword.AS, Keyword.STRUCT))
        {
            mode = new ValueTableMode.AsStruct();
        }
        else if (ParseKeyword(Keyword.AS))
        {
            mode = ExpectedRef<ValueTableMode?>("VALUE or STRUCT", PeekTokenRef());
        }
        else
        {
            mode = null;
        }

        return mode;
    }

    /// <summary>Invoke <c>f</c> after first setting the parser's <c>ParserState</c> to <c>state</c>.</summary>
    /// <remarks>
    /// Upon return, restores the parser's state to what it started at.
    /// <para>
    /// Rust: <c>let res = f(self); self.state = current_state; res</c> restaura o estado também quando
    /// <c>f</c> devolve <c>Err</c>; com exceções, isso é um <c>try/finally</c>.
    /// </para>
    /// </remarks>
    internal T WithState<T>(ParserState state, Func<Parser, T> f)
    {
        var currentState = _state;
        _state = state;
        try
        {
            return f(this);
        }
        finally
        {
            _state = currentState;
        }
    }

    /// <summary>Parse a <c>CONNECT BY</c> clause (Oracle-style hierarchical query support).</summary>
    public Sequence<ConnectByKind> MaybeParseConnectBy()
    {
        var clauses = new List<ConnectByKind>(2);
        while (true)
        {
            if (ParseKeywordsIndexed(Keyword.START, Keyword.WITH) is { } startIdx)
            {
                clauses.Add(new ConnectByKind.StartWith(
                    StartToken: AttachedToken.From(TokenAt(startIdx)),
                    Condition: ParseExpr()));
            }
            else if (ParseKeywordsIndexed(Keyword.CONNECT, Keyword.BY) is { } connectIdx)
            {
                clauses.Add(new ConnectByKind.ConnectBy(
                    ConnectToken: AttachedToken.From(TokenAt(connectIdx)),
                    Nocycle: ParseKeyword(Keyword.NOCYCLE),
                    Relationships: WithState(ParserState.ConnectBy, parser =>
                        parser.ParseCommaSeparated(p => p.ParseExpr()))));
            }
            else
            {
                break;
            }
        }
        return clauses.ToSequence();
    }
}
