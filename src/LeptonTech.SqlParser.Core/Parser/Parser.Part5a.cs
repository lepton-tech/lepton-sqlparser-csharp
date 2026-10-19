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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 17266–18327:
// parse_base_pattern … parse_grantee_name).
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core;

public sealed partial class Parser
{
    internal MatchRecognizePattern ParseBasePattern()
    {
        switch (NextToken().Token)
        {
            case Token.Caret:
                return new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Start());
            case Token.Placeholder { Value: var s } when s == "$":
                return new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.End());
            case Token.LBrace:
            {
                ExpectToken(new Token.Minus());
                var symbol = new MatchRecognizeSymbol.Named(ParseIdentifier());
                ExpectToken(new Token.Minus());
                ExpectToken(new Token.RBrace());
                return new MatchRecognizePattern.Exclude(symbol);
            }
            case Token.Word { Value: { QuoteStyle: null } w } when w.Value == "PERMUTE":
            {
                ExpectToken(new Token.LParen());
                var symbols = ParseCommaSeparated<MatchRecognizeSymbol>(p =>
                    new MatchRecognizeSymbol.Named(p.ParseIdentifier()));
                ExpectToken(new Token.RParen());
                return new MatchRecognizePattern.Permute(symbols);
            }
            case Token.LParen:
            {
                var pattern = ParsePattern();
                ExpectToken(new Token.RParen());
                return new MatchRecognizePattern.Group(pattern);
            }
            default:
                PrevToken();
                return new MatchRecognizePattern.Symbol(new MatchRecognizeSymbol.Named(ParseIdentifier()));
        }
    }

    internal MatchRecognizePattern ParseRepetitionPattern()
    {
        var pattern = ParseBasePattern();
        var token = NextToken();
        RepetitionQuantifier quantifier;
        switch (token.Token)
        {
            case Token.Mul:
                quantifier = new RepetitionQuantifier.ZeroOrMore();
                break;
            case Token.Plus:
                quantifier = new RepetitionQuantifier.OneOrMore();
                break;
            case Token.Placeholder { Value: var s } when s == "?":
                quantifier = new RepetitionQuantifier.AtMostOne();
                break;
            case Token.LBrace:
            {
                // quantifier is a range like {n} or {n,} or {,m} or {n,m}
                // Rust: `let token = self.next_token();` sombreia o `token` externo (caso 72).
                var rangeToken = NextToken();
                switch (rangeToken.Token)
                {
                    case Token.Comma:
                    {
                        var nextToken = NextToken();
                        if (nextToken.Token is not Token.Number { Value: var n })
                        {
                            return Expected<MatchRecognizePattern>("literal number", nextToken);
                        }
                        ExpectToken(new Token.RBrace());
                        quantifier = new RepetitionQuantifier.AtMost(Parse<uint>(n, rangeToken.Span.Start));
                        break;
                    }
                    // A guarda consome a vírgula, como no Rust (o `when` só roda se o padrão casar).
                    case Token.Number { Value: var n } when ConsumeToken(new Token.Comma()):
                    {
                        var nextToken = NextToken();
                        switch (nextToken.Token)
                        {
                            case Token.Number { Value: var m }:
                                ExpectToken(new Token.RBrace());
                                quantifier = new RepetitionQuantifier.Range(
                                    Parse<uint>(n, rangeToken.Span.Start),
                                    Parse<uint>(m, rangeToken.Span.Start));
                                break;
                            case Token.RBrace:
                                quantifier = new RepetitionQuantifier.AtLeast(Parse<uint>(n, rangeToken.Span.Start));
                                break;
                            default:
                                return Expected<MatchRecognizePattern>("} or upper bound", nextToken);
                        }
                        break;
                    }
                    case Token.Number { Value: var n }:
                        ExpectToken(new Token.RBrace());
                        quantifier = new RepetitionQuantifier.Exactly(Parse<uint>(n, rangeToken.Span.Start));
                        break;
                    default:
                        return Expected<MatchRecognizePattern>("quantifier range", rangeToken);
                }
                break;
            }
            default:
                PrevToken();
                return pattern;
        }
        // Rust: `let quantifier = if .. { Reluctant(..) } else { quantifier };` sombreia o `quantifier`
        // anterior com o mesmo tipo: reatribuição (caso 72).
        if (ConsumeToken(new Token.Placeholder("?")))
        {
            quantifier = new RepetitionQuantifier.Reluctant(quantifier);
        }
        return new MatchRecognizePattern.Repetition(
            pattern,
            quantifier);
    }

    internal MatchRecognizePattern ParseConcatPattern()
    {
        var patterns = new List<MatchRecognizePattern> { ParseRepetitionPattern() };
        while (PeekTokenRef().Token is not (Token.RParen or Token.Pipe))
        {
            patterns.Add(ParseRepetitionPattern());
        }
        // Rust: `match <[MatchRecognizePattern; 1]>::try_from(patterns) { Ok([pattern]) => .., Err(patterns) => .. }`.
        if (patterns.Count == 1)
        {
            return patterns[0];
        }
        return new MatchRecognizePattern.Concat(patterns.ToSequence());
    }

    internal MatchRecognizePattern ParsePattern()
    {
        var pattern = ParseConcatPattern();
        if (ConsumeToken(new Token.Pipe()))
        {
            switch (ParsePattern())
            {
                // flatten nested alternations
                case MatchRecognizePattern.Alternation { Value: var alternationPatterns }:
                {
                    var patterns = new List<MatchRecognizePattern>(alternationPatterns);
                    patterns.Insert(0, pattern);
                    return new MatchRecognizePattern.Alternation(patterns.ToSequence());
                }
                case var next:
                    return new MatchRecognizePattern.Alternation(Sequence.Of(pattern, next));
            }
        }
        else
        {
            return pattern;
        }
    }

    /// <summary>Parses a the timestamp version specifier (i.e. query historical data)</summary>
    public TableVersion? MaybeParseTableVersion()
    {
        if (_dialect.SupportsTableVersioning)
        {
            if (ParseKeywords(Keyword.FOR, Keyword.SYSTEM_TIME, Keyword.AS, Keyword.OF))
            {
                var expr = ParseExpr();
                return new TableVersion.ForSystemTimeAsOf(expr);
            }
            else if (PeekKeyword(Keyword.CHANGES))
            {
                return ParseTableVersionChanges();
            }
            else if (PeekKeyword(Keyword.AT) || PeekKeyword(Keyword.BEFORE))
            {
                var funcName = ParseObjectName(true);
                var func = ParseFunction(funcName);
                return new TableVersion.Function(func);
            }
            else if (ParseKeywords(Keyword.TIMESTAMP, Keyword.AS, Keyword.OF))
            {
                var expr = ParseExpr();
                return new TableVersion.TimestampAsOf(expr);
            }
            else if (ParseKeywords(Keyword.VERSION, Keyword.AS, Keyword.OF))
            {
                var expr = new Expr.Value(ParseNumberValue());
                return new TableVersion.VersionAsOf(expr);
            }
        }
        return null;
    }

    /// <summary>Parses the Snowflake <c>CHANGES</c> clause for change tracking queries.</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// CHANGES (INFORMATION =&gt; DEFAULT)
    ///   AT (TIMESTAMP =&gt; &lt;expr&gt;)
    ///   [END (TIMESTAMP =&gt; &lt;expr&gt;)]
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constructs/changes"/>
    /// </remarks>
    internal TableVersion ParseTableVersionChanges()
    {
        var changesName = ParseObjectName(true);
        var changes = ParseFunction(changesName);
        var atName = ParseObjectName(true);
        var at = ParseFunction(atName);
        Expr? end;
        if (PeekKeyword(Keyword.END))
        {
            var endName = ParseObjectName(true);
            end = ParseFunction(endName);
        }
        else
        {
            end = null;
        }
        return new TableVersion.Changes(ChangesValue: changes, At: at, End: end);
    }

    /// <summary>
    /// Parses MySQL's JSON_TABLE column definition.
    /// For example: <c>id INT EXISTS PATH '$' DEFAULT '0' ON EMPTY ERROR ON ERROR</c>
    /// </summary>
    public JsonTableColumn ParseJsonTableColumnDef()
    {
        if (ParseKeyword(Keyword.NESTED))
        {
            var _hasPathKeyword = ParseKeyword(Keyword.PATH);
            var nestedPath = ParseValue();
            ExpectKeywordIs(Keyword.COLUMNS);
            var columns = ParseParenthesized(p => p.ParseCommaSeparated(inner => inner.ParseJsonTableColumnDef()));
            return new JsonTableColumn.Nested(new JsonTableNestedColumn(
                Path: nestedPath,
                Columns: columns));
        }
        var name = ParseIdentifier();
        if (ParseKeyword(Keyword.FOR))
        {
            ExpectKeywordIs(Keyword.ORDINALITY);
            return new JsonTableColumn.ForOrdinality(name);
        }
        var type = ParseDataType();
        var exists = ParseKeyword(Keyword.EXISTS);
        ExpectKeywordIs(Keyword.PATH);
        var path = ParseValue();
        JsonTableColumnErrorHandling? onEmpty = null;
        JsonTableColumnErrorHandling? onError = null;
        while (ParseJsonTableColumnErrorHandling() is { } errorHandling)
        {
            if (ParseKeyword(Keyword.EMPTY))
            {
                onEmpty = errorHandling;
            }
            else
            {
                ExpectKeywordIs(Keyword.ERROR);
                onError = errorHandling;
            }
        }
        return new JsonTableColumn.Named(new JsonTableNamedColumn(
            Name: name,
            Type: type,
            Path: path,
            Exists: exists,
            OnEmpty: onEmpty,
            OnError: onError));
    }

    /// <summary>Parses MSSQL's <c>OPENJSON WITH</c> column definition.</summary>
    /// <remarks>
    /// <code>
    /// colName type [ column_path ] [ AS JSON ]
    /// </code>
    /// Reference: <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/openjson-transact-sql?view=sql-server-ver16#syntax"/>
    /// </remarks>
    public OpenJsonTableColumn ParseOpenjsonTableColumnDef()
    {
        var name = ParseIdentifier();
        var type = ParseDataType();
        string? path;
        if (PeekToken().Token is Token.SingleQuotedString { Value: var quotedPath })
        {
            NextToken();
            path = quotedPath;
        }
        else
        {
            path = null;
        }
        var asJson = ParseKeyword(Keyword.AS);
        if (asJson)
        {
            ExpectKeywordIs(Keyword.JSON);
        }
        return new OpenJsonTableColumn(
            Name: name,
            Type: type,
            Path: path,
            AsJson: asJson);
    }

    internal JsonTableColumnErrorHandling? ParseJsonTableColumnErrorHandling()
    {
        JsonTableColumnErrorHandling res;
        if (ParseKeyword(Keyword.NULL))
        {
            res = new JsonTableColumnErrorHandling.Null();
        }
        else if (ParseKeyword(Keyword.ERROR))
        {
            res = new JsonTableColumnErrorHandling.Error();
        }
        else if (ParseKeyword(Keyword.DEFAULT))
        {
            res = new JsonTableColumnErrorHandling.Default(ParseValue());
        }
        else
        {
            return null;
        }
        ExpectKeywordIs(Keyword.ON);
        return res;
    }

    /// <summary>Parse a derived table factor (a parenthesized subquery), handling optional LATERAL.</summary>
    public TableFactor ParseDerivedTableFactor(IsLateral lateral)
    {
        var subquery = ParseQuery();
        ExpectToken(new Token.RParen());
        var alias = MaybeParseTableAlias();

        // Parse optional SAMPLE clause after alias
        TableSampleKind? sample = MaybeParseTableSample() is { } tableSample
            ? new TableSampleKind.AfterTableAlias(tableSample)
            : null;

        return new TableFactor.Derived(
            Lateral: lateral switch
            {
                IsLateral.Lateral => true,
                IsLateral.NotLateral => false,
                _ => throw new InvalidOperationException($"Variante de IsLateral não tratada: {lateral}"),
            },
            Subquery: subquery,
            Alias: alias,
            Sample: sample);
    }

    /// <summary>Parses an expression with an optional alias</summary>
    /// <remarks>
    /// Examples:
    /// <code>
    /// SUM(price) AS total_price
    /// </code>
    /// <code>
    /// SUM(price)
    /// </code>
    /// Example
    /// <code>
    /// var sql = "SUM(\"a\") as \"b\"";
    /// var parser = new Parser(new GenericDialect()).TryWithSql(sql);
    /// var exprWithAlias = parser.ParseExprWithAlias();
    /// // exprWithAlias.Alias?.Value == "b"
    /// </code>
    /// </remarks>
    public ExprWithAlias ParseExprWithAlias()
    {
        var expr = ParseExpr();
        Ident? alias;
        if (ParseKeyword(Keyword.AS))
        {
            alias = ParseIdentifier();
        }
        else
        {
            alias = null;
        }

        return new ExprWithAlias(Expr: expr, Alias: alias);
    }

    /// <summary>
    /// Parse an expression followed by an optional alias; Unlike
    /// <see cref="ParseExprWithAlias"/> the "AS" keyword between the expression
    /// and the alias is optional.
    /// </summary>
    internal ExprWithAlias ParseExprWithAliasOptionalAsKeyword()
    {
        var expr = ParseExpr();
        var alias = ParseIdentifierOptionalAlias();
        return new ExprWithAlias(Expr: expr, Alias: alias);
    }

    /// <summary>Parses a plain function call with an optional alias for the <c>PIVOT</c> clause</summary>
    internal ExprWithAlias ParsePivotAggregateFunction()
    {
        string functionName;
        switch (NextToken().Token)
        {
            case Token.Word { Value: var w }:
                functionName = w.Value;
                break;
            default:
                functionName = ExpectedRef<string>("a function identifier", PeekTokenRef());
                break;
        }
        var expr = ParseFunction(ObjectName.From(Sequence.Of(new Ident(functionName))));
        // Rust: `fn validator(..)` aninhada no bloco do `let alias` → função local estática (decisão 9 da B1).
        static bool Validator(bool @explicit, Keyword kw, Parser parser) =>
            // ~ for a PIVOT aggregate function the alias must not be a "FOR"; in any dialect
            kw != Keyword.FOR && parser._dialect.IsSelectItemAlias(@explicit, kw, parser);
        var alias = ParseOptionalAliasInner(null, Validator);
        return new ExprWithAlias(Expr: expr, Alias: alias);
    }

    /// <summary>Parse a PIVOT table factor (ClickHouse/Oracle style pivot), returning a TableFactor.</summary>
    public TableFactor ParsePivotTableFactor(TableFactor table)
    {
        ExpectToken(new Token.LParen());
        var aggregateFunctions = ParseCommaSeparated(p => p.ParsePivotAggregateFunction());
        ExpectKeywordIs(Keyword.FOR);
        Sequence<Expr> valueColumn;
        if (PeekTokenRef().Token is Token.LParen)
        {
            valueColumn = ParseParenthesizedColumnListInner(IsOptional.Mandatory, false, p =>
                p.ParseSubexpr(_dialect.PrecValue(Precedence.Between)));
        }
        else
        {
            valueColumn = Sequence.Of(ParseSubexpr(_dialect.PrecValue(Precedence.Between)));
        }
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
                ParseCommaSeparated(p => p.ParseExprWithAliasOptionalAsKeyword()));
        }
        ExpectToken(new Token.RParen());

        Expr? defaultOnNull;
        if (ParseKeywords(Keyword.DEFAULT, Keyword.ON, Keyword.NULL))
        {
            ExpectToken(new Token.LParen());
            var expr = ParseExpr();
            ExpectToken(new Token.RParen());
            defaultOnNull = expr;
        }
        else
        {
            defaultOnNull = null;
        }

        ExpectToken(new Token.RParen());
        var alias = MaybeParseTableAlias();
        return new TableFactor.Pivot(
            Table: table,
            AggregateFunctions: aggregateFunctions,
            ValueColumn: valueColumn,
            ValueSource: valueSource,
            DefaultOnNull: defaultOnNull,
            Alias: alias);
    }

    /// <summary>Parse an UNPIVOT table factor, returning a TableFactor.</summary>
    public TableFactor ParseUnpivotTableFactor(TableFactor table)
    {
        NullInclusion? nullInclusion;
        if (ParseKeyword(Keyword.INCLUDE))
        {
            ExpectKeywordIs(Keyword.NULLS);
            nullInclusion = new NullInclusion.IncludeNulls();
        }
        else if (ParseKeyword(Keyword.EXCLUDE))
        {
            ExpectKeywordIs(Keyword.NULLS);
            nullInclusion = new NullInclusion.ExcludeNulls();
        }
        else
        {
            nullInclusion = null;
        }
        ExpectToken(new Token.LParen());
        var value = ParseExpr();
        ExpectKeywordIs(Keyword.FOR);
        var name = ParseIdentifier();
        ExpectKeywordIs(Keyword.IN);
        var columns = ParseParenthesizedColumnListInner(IsOptional.Mandatory, false, p =>
            p.ParseExprWithAlias());
        ExpectToken(new Token.RParen());
        var alias = MaybeParseTableAlias();
        return new TableFactor.Unpivot(
            Table: table,
            Value: value,
            NullInclusion: nullInclusion,
            Name: name,
            Columns: columns,
            Alias: alias);
    }

    /// <summary>Parse an object UNPIVOT table factor in FROM clause.</summary>
    /// <remarks>
    /// Syntax:
    /// <c>UNPIVOT expression AS value_alias [AT attribute_alias]</c>
    /// </remarks>
    public TableFactor ParseUnpivotExprTableFactor()
    {
        ExpectKeywordIs(Keyword.UNPIVOT);
        var expression = ParseExpr();
        ExpectKeywordIs(Keyword.AS);
        var valueAlias = ParseIdentifier();
        Ident? attributeAlias;
        if (ParseKeyword(Keyword.AT))
        {
            attributeAlias = ParseIdentifier();
        }
        else
        {
            attributeAlias = null;
        }

        return new TableFactor.UnpivotExpr(
            Expression: expression,
            ValueAlias: valueAlias,
            AttributeAlias: attributeAlias);
    }

    /// <summary>Parse a JOIN constraint (<c>NATURAL</c>, <c>ON &lt;expr&gt;</c>, <c>USING (...)</c>, or no constraint).</summary>
    public JoinConstraint ParseJoinConstraint(bool natural)
    {
        if (natural)
        {
            return new JoinConstraint.Natural();
        }
        else if (ParseKeyword(Keyword.ON))
        {
            var constraint = ParseExpr();
            return new JoinConstraint.On(constraint);
        }
        else if (ParseKeyword(Keyword.USING))
        {
            var columns = ParseParenthesizedQualifiedColumnList(IsOptional.Mandatory, false);
            return new JoinConstraint.Using(columns);
        }
        else
        {
            return new JoinConstraint.None();
            //self.expected_ref("ON, or USING after JOIN", self.peek_token_ref())
        }
    }

    /// <summary>Parse a GRANT statement.</summary>
    public Grant ParseGrant()
    {
        var (privileges, objects) = ParseGrantDenyRevokePrivilegesObjects();

        ExpectKeywordIs(Keyword.TO);
        var grantees = ParseGrantees();

        var withGrantOption =
            ParseKeywords(Keyword.WITH, Keyword.GRANT, Keyword.OPTION);

        CurrentGrantsKind? currentGrants;
        if (ParseKeywords(Keyword.COPY, Keyword.CURRENT, Keyword.GRANTS))
        {
            currentGrants = new CurrentGrantsKind.CopyCurrentGrants();
        }
        else if (ParseKeywords(Keyword.REVOKE, Keyword.CURRENT, Keyword.GRANTS))
        {
            currentGrants = new CurrentGrantsKind.RevokeCurrentGrants();
        }
        else
        {
            currentGrants = null;
        }

        Ident? asGrantor;
        if (ParseKeywords(Keyword.AS))
        {
            asGrantor = ParseIdentifier();
        }
        else
        {
            asGrantor = null;
        }

        Ident? grantedBy;
        if (ParseKeywords(Keyword.GRANTED, Keyword.BY))
        {
            grantedBy = ParseIdentifier();
        }
        else
        {
            grantedBy = null;
        }

        return new Grant(
            Privileges: privileges,
            Objects: objects,
            Grantees: grantees,
            WithGrantOption: withGrantOption,
            AsGrantor: asGrantor,
            GrantedBy: grantedBy,
            CurrentGrants: currentGrants);
    }

    internal Sequence<Grantee> ParseGrantees()
    {
        var values = new List<Grantee>();
        var granteeType = GranteesType.None;
        while (true)
        {
            GranteesType newGranteeType;
            if (ParseKeyword(Keyword.ROLE))
            {
                newGranteeType = GranteesType.Role;
            }
            else if (ParseKeyword(Keyword.USER))
            {
                newGranteeType = GranteesType.User;
            }
            else if (ParseKeyword(Keyword.SHARE))
            {
                newGranteeType = GranteesType.Share;
            }
            else if (ParseKeyword(Keyword.GROUP))
            {
                newGranteeType = GranteesType.Group;
            }
            else if (ParseKeyword(Keyword.PUBLIC))
            {
                newGranteeType = GranteesType.Public;
            }
            else if (ParseKeywords(Keyword.DATABASE, Keyword.ROLE))
            {
                newGranteeType = GranteesType.DatabaseRole;
            }
            else if (ParseKeywords(Keyword.APPLICATION, Keyword.ROLE))
            {
                newGranteeType = GranteesType.ApplicationRole;
            }
            else if (ParseKeyword(Keyword.APPLICATION))
            {
                newGranteeType = GranteesType.Application;
            }
            else
            {
                newGranteeType = granteeType; // keep from previous iteraton, if not specified
            }

            if (_dialect
                .GetReservedGranteesTypes
                .Contains(newGranteeType))
            {
                PrevToken();
            }
            else
            {
                granteeType = newGranteeType;
            }

            Grantee grantee;
            if (granteeType == GranteesType.Public)
            {
                grantee = new Grantee(
                    GranteeType: granteeType,
                    Name: null);
            }
            else
            {
                var name = ParseGranteeName();
                if (ConsumeToken(new Token.Colon()))
                {
                    // Redshift supports namespace prefix for external users and groups:
                    // <Namespace>:<GroupName> or <Namespace>:<UserName>
                    // https://docs.aws.amazon.com/redshift/latest/mgmt/redshift-iam-access-control-native-idp.html
                    var ident = ParseIdentifier();
                    if (name is GranteeName.ObjectName { Value: var @namespace })
                    {
                        name = new GranteeName.ObjectName(ObjectName.From(Sequence.Of(new Ident(
                            $"{@namespace}:{ident}"))));
                    }
                }
                grantee = new Grantee(
                    GranteeType: granteeType,
                    Name: name);
            }

            values.Add(grantee);

            if (!ConsumeToken(new Token.Comma()))
            {
                break;
            }
        }

        return values.ToSequence();
    }

    /// <summary>Parse privileges and optional target objects for GRANT/DENY/REVOKE statements.</summary>
    public (Privileges Privileges, GrantObjects? Objects) ParseGrantDenyRevokePrivilegesObjects()
    {
        Privileges privileges;
        if (ParseKeyword(Keyword.ALL))
        {
            privileges = new Privileges.All(
                WithPrivilegesKeyword: ParseKeyword(Keyword.PRIVILEGES));
        }
        else
        {
            var actions = ParseActionsList();
            privileges = new Privileges.Actions(actions);
        }

        GrantObjects? objects;
        if (ParseKeyword(Keyword.ON))
        {
            if (ParseKeywords(Keyword.ALL, Keyword.TABLES, Keyword.IN, Keyword.SCHEMA))
            {
                objects = new GrantObjects.AllTablesInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.ALL,
                Keyword.EXTERNAL,
                Keyword.TABLES,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.AllExternalTablesInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.ALL,
                Keyword.VIEWS,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.AllViewsInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.ALL,
                Keyword.MATERIALIZED,
                Keyword.VIEWS,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.AllMaterializedViewsInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.ALL,
                Keyword.FUNCTIONS,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.AllFunctionsInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.FUTURE,
                Keyword.SCHEMAS,
                Keyword.IN,
                Keyword.DATABASE))
            {
                objects = new GrantObjects.FutureSchemasInDatabase(
                    Databases: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.FUTURE,
                Keyword.TABLES,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.FutureTablesInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.FUTURE,
                Keyword.EXTERNAL,
                Keyword.TABLES,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.FutureExternalTablesInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.FUTURE,
                Keyword.VIEWS,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.FutureViewsInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.FUTURE,
                Keyword.MATERIALIZED,
                Keyword.VIEWS,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.FutureMaterializedViewsInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.ALL,
                Keyword.SEQUENCES,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.AllSequencesInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(
                Keyword.FUTURE,
                Keyword.SEQUENCES,
                Keyword.IN,
                Keyword.SCHEMA))
            {
                objects = new GrantObjects.FutureSequencesInSchema(
                    Schemas: ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(Keyword.RESOURCE, Keyword.MONITOR))
            {
                objects = new GrantObjects.ResourceMonitors(
                    ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(Keyword.COMPUTE, Keyword.POOL))
            {
                objects = new GrantObjects.ComputePools(
                    ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(Keyword.FAILOVER, Keyword.GROUP))
            {
                objects = new GrantObjects.FailoverGroup(
                    ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(Keyword.REPLICATION, Keyword.GROUP))
            {
                objects = new GrantObjects.ReplicationGroup(
                    ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else if (ParseKeywords(Keyword.EXTERNAL, Keyword.VOLUME))
            {
                objects = new GrantObjects.ExternalVolumes(
                    ParseCommaSeparated(p => p.ParseObjectName(false)));
            }
            else
            {
                var objectType = ParseOneOfKeywords(
                    Keyword.SEQUENCE,
                    Keyword.DATABASE,
                    Keyword.SCHEMA,
                    Keyword.TABLE,
                    Keyword.VIEW,
                    Keyword.WAREHOUSE,
                    Keyword.INTEGRATION,
                    Keyword.VIEW,
                    Keyword.WAREHOUSE,
                    Keyword.INTEGRATION,
                    Keyword.USER,
                    Keyword.CONNECTION,
                    Keyword.PROCEDURE,
                    Keyword.FUNCTION);
                // Rust: `let objects = self.parse_comma_separated(..);` guarda o `Result` e cada braço
                // faz `objects?`. O erro fica guardado e só é relançado no braço que usa o valor
                // (decisão 4 da B1); o braço `Some(unexpected_keyword)` o descarta, como no Rust.
                Sequence<ObjectName> objectsResult = default;
                ParserException? objectsError = null;
                try
                {
                    objectsResult = ParseCommaSeparated(p => p.ParseObjectNameInner(false, true));
                }
                catch (ParserException e)
                {
                    objectsError = e;
                }
                Sequence<ObjectName> UnwrapObjects() =>
                    objectsError is null ? objectsResult : throw StackGuard.Rethrow(objectsError);

                switch (objectType)
                {
                    case Keyword.DATABASE:
                        objects = new GrantObjects.Databases(UnwrapObjects());
                        break;
                    case Keyword.SCHEMA:
                        objects = new GrantObjects.Schemas(UnwrapObjects());
                        break;
                    case Keyword.SEQUENCE:
                        objects = new GrantObjects.Sequences(UnwrapObjects());
                        break;
                    case Keyword.WAREHOUSE:
                        objects = new GrantObjects.Warehouses(UnwrapObjects());
                        break;
                    case Keyword.INTEGRATION:
                        objects = new GrantObjects.Integrations(UnwrapObjects());
                        break;
                    case Keyword.VIEW:
                        objects = new GrantObjects.Views(UnwrapObjects());
                        break;
                    case Keyword.USER:
                        objects = new GrantObjects.Users(UnwrapObjects());
                        break;
                    case Keyword.CONNECTION:
                        objects = new GrantObjects.Connections(UnwrapObjects());
                        break;
                    case Keyword.PROCEDURE or Keyword.FUNCTION:
                    {
                        // Rust: `kw @ (Some(Keyword::PROCEDURE) | Some(Keyword::FUNCTION))`.
                        var kw = objectType;
                        if (UnwrapObjects().First is { } name)
                        {
                            objects = ParseGrantProcedureOrFunction(name, kw);
                        }
                        else
                        {
                            objects = ExpectedRef<GrantObjects?>("procedure or function name", PeekTokenRef());
                        }
                        break;
                    }
                    case Keyword.TABLE or null:
                        objects = new GrantObjects.Tables(UnwrapObjects());
                        break;
                    case var unexpectedKeyword:
                        throw new ParserException(new ParserError.ParserErrorValue(
                            $"Internal parser error: unexpected keyword `{unexpectedKeyword.Value.ToSql()}` in grant objects"));
                }
            }
        }
        else
        {
            objects = null;
        }

        return (privileges, objects);
    }

    internal GrantObjects? ParseGrantProcedureOrFunction(ObjectName name, Keyword? kw)
    {
        Sequence<DataType> argTypes;
        if (ConsumeToken(new Token.LParen()))
        {
            var list = ParseCommaSeparated0(p => p.ParseDataType(), new Token.RParen());
            ExpectToken(new Token.RParen());
            argTypes = list;
        }
        else
        {
            argTypes = Sequence<DataType>.Empty;
        }
        switch (kw)
        {
            case Keyword.PROCEDURE:
                return new GrantObjects.Procedure(
                    Name: name,
                    ArgTypes: argTypes);
            case Keyword.FUNCTION:
                return new GrantObjects.Function(
                    Name: name,
                    ArgTypes: argTypes);
            default:
                return ExpectedRef<GrantObjects?>("procedure or function keywords", PeekTokenRef());
        }
    }

    /// <summary>Parse a single grantable permission/action (used within GRANT statements).</summary>
    public Ast.Action ParseGrantPermission()
    {
        // Rust: `fn parse_columns(parser: &mut Parser)` aninhada → função local estática (decisão 9 da B1).
        // Dentro deste método ela esconde o membro `ParseColumns()` (B3), que não é usado aqui.
        static Sequence<Ident>? ParseColumns(Parser parser)
        {
            var columns = parser.ParseParenthesizedColumnList(IsOptional.Optional, false);
            if (columns.IsEmpty)
            {
                return null;
            }
            else
            {
                return columns;
            }
        }

        // Multi-word privileges
        if (ParseKeywords(Keyword.IMPORTED, Keyword.PRIVILEGES))
        {
            return new Ast.Action.ImportedPrivileges();
        }
        else if (ParseKeywords(Keyword.ADD, Keyword.SEARCH, Keyword.OPTIMIZATION))
        {
            return new Ast.Action.AddSearchOptimization();
        }
        else if (ParseKeywords(Keyword.ATTACH, Keyword.LISTING))
        {
            return new Ast.Action.AttachListing();
        }
        else if (ParseKeywords(Keyword.ATTACH, Keyword.POLICY))
        {
            return new Ast.Action.AttachPolicy();
        }
        else if (ParseKeywords(Keyword.BIND, Keyword.SERVICE, Keyword.ENDPOINT))
        {
            return new Ast.Action.BindServiceEndpoint();
        }
        else if (ParseKeywords(Keyword.DATABASE, Keyword.ROLE))
        {
            var role = ParseObjectName(false);
            return new Ast.Action.DatabaseRole(Role: role);
        }
        else if (ParseKeywords(Keyword.EVOLVE, Keyword.SCHEMA))
        {
            return new Ast.Action.EvolveSchema();
        }
        else if (ParseKeywords(Keyword.IMPORT, Keyword.SHARE))
        {
            return new Ast.Action.ImportShare();
        }
        else if (ParseKeywords(Keyword.MANAGE, Keyword.VERSIONS))
        {
            return new Ast.Action.ManageVersions();
        }
        else if (ParseKeywords(Keyword.MANAGE, Keyword.RELEASES))
        {
            return new Ast.Action.ManageReleases();
        }
        else if (ParseKeywords(Keyword.OVERRIDE, Keyword.SHARE, Keyword.RESTRICTIONS))
        {
            return new Ast.Action.OverrideShareRestrictions();
        }
        else if (ParseKeywords(
            Keyword.PURCHASE,
            Keyword.DATA,
            Keyword.EXCHANGE,
            Keyword.LISTING))
        {
            return new Ast.Action.PurchaseDataExchangeListing();
        }
        else if (ParseKeywords(Keyword.RESOLVE, Keyword.ALL))
        {
            return new Ast.Action.ResolveAll();
        }
        else if (ParseKeywords(Keyword.READ, Keyword.SESSION))
        {
            return new Ast.Action.ReadSession();

            // Single-word privileges
        }
        else if (ParseKeyword(Keyword.APPLY))
        {
            var applyType = ParseActionApplyType();
            return new Ast.Action.Apply(ApplyType: applyType);
        }
        else if (ParseKeyword(Keyword.APPLYBUDGET))
        {
            return new Ast.Action.ApplyBudget();
        }
        else if (ParseKeyword(Keyword.AUDIT))
        {
            return new Ast.Action.Audit();
        }
        else if (ParseKeyword(Keyword.CONNECT))
        {
            return new Ast.Action.Connect();
        }
        else if (ParseKeyword(Keyword.CREATE))
        {
            var objType = MaybeParseActionCreateObjectType();
            return new Ast.Action.Create(ObjType: objType);
        }
        else if (ParseKeyword(Keyword.DELETE))
        {
            return new Ast.Action.Delete();
        }
        else if (ParseKeyword(Keyword.EXEC))
        {
            var objType = MaybeParseActionExecuteObjType();
            return new Ast.Action.Exec(ObjType: objType);
        }
        else if (ParseKeyword(Keyword.EXECUTE))
        {
            var objType = MaybeParseActionExecuteObjType();
            return new Ast.Action.Execute(ObjType: objType);
        }
        else if (ParseKeyword(Keyword.FAILOVER))
        {
            return new Ast.Action.Failover();
        }
        else if (ParseKeyword(Keyword.INSERT))
        {
            return new Ast.Action.Insert(
                Columns: ParseColumns(this));
        }
        else if (ParseKeyword(Keyword.MANAGE))
        {
            var manageType = ParseActionManageType();
            return new Ast.Action.Manage(ManageType: manageType);
        }
        else if (ParseKeyword(Keyword.MODIFY))
        {
            var modifyType = ParseActionModifyType();
            return new Ast.Action.Modify(ModifyType: modifyType);
        }
        else if (ParseKeyword(Keyword.MONITOR))
        {
            var monitorType = ParseActionMonitorType();
            return new Ast.Action.Monitor(MonitorType: monitorType);
        }
        else if (ParseKeyword(Keyword.OPERATE))
        {
            return new Ast.Action.Operate();
        }
        else if (ParseKeyword(Keyword.REFERENCES))
        {
            return new Ast.Action.References(
                Columns: ParseColumns(this));
        }
        else if (ParseKeyword(Keyword.READ))
        {
            return new Ast.Action.Read();
        }
        else if (ParseKeyword(Keyword.REPLICATE))
        {
            return new Ast.Action.Replicate();
        }
        else if (ParseKeyword(Keyword.ROLE))
        {
            var role = ParseObjectName(false);
            return new Ast.Action.Role(RoleValue: role);
        }
        else if (ParseKeyword(Keyword.SELECT))
        {
            return new Ast.Action.Select(
                Columns: ParseColumns(this));
        }
        else if (ParseKeyword(Keyword.TEMPORARY))
        {
            return new Ast.Action.Temporary();
        }
        else if (ParseKeyword(Keyword.TRIGGER))
        {
            return new Ast.Action.Trigger();
        }
        else if (ParseKeyword(Keyword.TRUNCATE))
        {
            return new Ast.Action.Truncate();
        }
        else if (ParseKeyword(Keyword.UPDATE))
        {
            return new Ast.Action.Update(
                Columns: ParseColumns(this));
        }
        else if (ParseKeyword(Keyword.USAGE))
        {
            return new Ast.Action.Usage();
        }
        else if (ParseKeyword(Keyword.OWNERSHIP))
        {
            return new Ast.Action.Ownership();
        }
        else if (ParseKeyword(Keyword.DROP))
        {
            return new Ast.Action.Drop();
        }
        else
        {
            return ExpectedRef<Ast.Action>("a privilege keyword", PeekTokenRef());
        }
    }

    internal ActionCreateObjectType? MaybeParseActionCreateObjectType()
    {
        // Multi-word object types
        if (ParseKeywords(Keyword.APPLICATION, Keyword.PACKAGE))
        {
            return new ActionCreateObjectType.ApplicationPackage();
        }
        else if (ParseKeywords(Keyword.COMPUTE, Keyword.POOL))
        {
            return new ActionCreateObjectType.ComputePool();
        }
        else if (ParseKeywords(Keyword.DATA, Keyword.EXCHANGE, Keyword.LISTING))
        {
            return new ActionCreateObjectType.DataExchangeListing();
        }
        else if (ParseKeywords(Keyword.EXTERNAL, Keyword.VOLUME))
        {
            return new ActionCreateObjectType.ExternalVolume();
        }
        else if (ParseKeywords(Keyword.FAILOVER, Keyword.GROUP))
        {
            return new ActionCreateObjectType.FailoverGroup();
        }
        else if (ParseKeywords(Keyword.NETWORK, Keyword.POLICY))
        {
            return new ActionCreateObjectType.NetworkPolicy();
        }
        else if (ParseKeywords(Keyword.ORGANIZATION, Keyword.LISTING))
        {
            return new ActionCreateObjectType.OrganiationListing();
        }
        else if (ParseKeywords(Keyword.REPLICATION, Keyword.GROUP))
        {
            return new ActionCreateObjectType.ReplicationGroup();
        }
        // Single-word object types
        else if (ParseKeyword(Keyword.ACCOUNT))
        {
            return new ActionCreateObjectType.Account();
        }
        else if (ParseKeyword(Keyword.APPLICATION))
        {
            return new ActionCreateObjectType.Application();
        }
        else if (ParseKeyword(Keyword.DATABASE))
        {
            return new ActionCreateObjectType.Database();
        }
        else if (ParseKeyword(Keyword.INTEGRATION))
        {
            return new ActionCreateObjectType.Integration();
        }
        else if (ParseKeyword(Keyword.ROLE))
        {
            return new ActionCreateObjectType.Role();
        }
        else if (ParseKeyword(Keyword.SCHEMA))
        {
            return new ActionCreateObjectType.Schema();
        }
        else if (ParseKeyword(Keyword.SHARE))
        {
            return new ActionCreateObjectType.Share();
        }
        else if (ParseKeyword(Keyword.USER))
        {
            return new ActionCreateObjectType.User();
        }
        else if (ParseKeyword(Keyword.WAREHOUSE))
        {
            return new ActionCreateObjectType.Warehouse();
        }
        else
        {
            return null;
        }
    }

    internal ActionApplyType ParseActionApplyType()
    {
        if (ParseKeywords(Keyword.AGGREGATION, Keyword.POLICY))
        {
            return new ActionApplyType.AggregationPolicy();
        }
        else if (ParseKeywords(Keyword.AUTHENTICATION, Keyword.POLICY))
        {
            return new ActionApplyType.AuthenticationPolicy();
        }
        else if (ParseKeywords(Keyword.JOIN, Keyword.POLICY))
        {
            return new ActionApplyType.JoinPolicy();
        }
        else if (ParseKeywords(Keyword.MASKING, Keyword.POLICY))
        {
            return new ActionApplyType.MaskingPolicy();
        }
        else if (ParseKeywords(Keyword.PACKAGES, Keyword.POLICY))
        {
            return new ActionApplyType.PackagesPolicy();
        }
        else if (ParseKeywords(Keyword.PASSWORD, Keyword.POLICY))
        {
            return new ActionApplyType.PasswordPolicy();
        }
        else if (ParseKeywords(Keyword.PROJECTION, Keyword.POLICY))
        {
            return new ActionApplyType.ProjectionPolicy();
        }
        else if (ParseKeywords(Keyword.ROW, Keyword.ACCESS, Keyword.POLICY))
        {
            return new ActionApplyType.RowAccessPolicy();
        }
        else if (ParseKeywords(Keyword.SESSION, Keyword.POLICY))
        {
            return new ActionApplyType.SessionPolicy();
        }
        else if (ParseKeyword(Keyword.TAG))
        {
            return new ActionApplyType.Tag();
        }
        else
        {
            return ExpectedRef<ActionApplyType>("GRANT APPLY type", PeekTokenRef());
        }
    }

    internal ActionExecuteObjectType? MaybeParseActionExecuteObjType()
    {
        if (ParseKeywords(Keyword.DATA, Keyword.METRIC, Keyword.FUNCTION))
        {
            return new ActionExecuteObjectType.DataMetricFunction();
        }
        else if (ParseKeywords(Keyword.MANAGED, Keyword.ALERT))
        {
            return new ActionExecuteObjectType.ManagedAlert();
        }
        else if (ParseKeywords(Keyword.MANAGED, Keyword.TASK))
        {
            return new ActionExecuteObjectType.ManagedTask();
        }
        else if (ParseKeyword(Keyword.ALERT))
        {
            return new ActionExecuteObjectType.Alert();
        }
        else if (ParseKeyword(Keyword.TASK))
        {
            return new ActionExecuteObjectType.Task();
        }
        else
        {
            return null;
        }
    }

    internal ActionManageType ParseActionManageType()
    {
        if (ParseKeywords(Keyword.ACCOUNT, Keyword.SUPPORT, Keyword.CASES))
        {
            return new ActionManageType.AccountSupportCases();
        }
        else if (ParseKeywords(Keyword.EVENT, Keyword.SHARING))
        {
            return new ActionManageType.EventSharing();
        }
        else if (ParseKeywords(Keyword.LISTING, Keyword.AUTO, Keyword.FULFILLMENT))
        {
            return new ActionManageType.ListingAutoFulfillment();
        }
        else if (ParseKeywords(Keyword.ORGANIZATION, Keyword.SUPPORT, Keyword.CASES))
        {
            return new ActionManageType.OrganizationSupportCases();
        }
        else if (ParseKeywords(Keyword.USER, Keyword.SUPPORT, Keyword.CASES))
        {
            return new ActionManageType.UserSupportCases();
        }
        else if (ParseKeyword(Keyword.GRANTS))
        {
            return new ActionManageType.Grants();
        }
        else if (ParseKeyword(Keyword.WAREHOUSES))
        {
            return new ActionManageType.Warehouses();
        }
        else
        {
            return ExpectedRef<ActionManageType>("GRANT MANAGE type", PeekTokenRef());
        }
    }

    internal ActionModifyType? ParseActionModifyType()
    {
        if (ParseKeywords(Keyword.LOG, Keyword.LEVEL))
        {
            return new ActionModifyType.LogLevel();
        }
        else if (ParseKeywords(Keyword.TRACE, Keyword.LEVEL))
        {
            return new ActionModifyType.TraceLevel();
        }
        else if (ParseKeywords(Keyword.SESSION, Keyword.LOG, Keyword.LEVEL))
        {
            return new ActionModifyType.SessionLogLevel();
        }
        else if (ParseKeywords(Keyword.SESSION, Keyword.TRACE, Keyword.LEVEL))
        {
            return new ActionModifyType.SessionTraceLevel();
        }
        else
        {
            return null;
        }
    }

    internal ActionMonitorType? ParseActionMonitorType()
    {
        if (ParseKeyword(Keyword.EXECUTION))
        {
            return new ActionMonitorType.Execution();
        }
        else if (ParseKeyword(Keyword.SECURITY))
        {
            return new ActionMonitorType.Security();
        }
        else if (ParseKeyword(Keyword.USAGE))
        {
            return new ActionMonitorType.Usage();
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse a grantee name, possibly with a host qualifier (user@host).</summary>
    public GranteeName ParseGranteeName()
    {
        var name = ParseObjectName(false);
        if (_dialect.SupportsUserHostGrantee
            && name.Value.Count == 1
            && name.Value[0].AsIdent() is not null
            && ConsumeToken(new Token.AtSign()))
        {
            // Rust: `name.0.pop().unwrap().as_ident().unwrap().clone()`: o único elemento (`name` não é
            // mais usado depois).
            var user = name.Value[name.Value.Count - 1].AsIdent()!;
            var host = ParseIdentifier();
            return new GranteeName.UserHost(User: user, Host: host);
        }
        else
        {
            return new GranteeName.ObjectName(name);
        }
    }
}
