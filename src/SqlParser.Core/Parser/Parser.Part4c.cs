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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 15607–16459:
// parse_as_table … peek_parens_less_nested_join).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse <c>CREATE TABLE x AS TABLE y</c></summary>
    public Table ParseAsTable()
    {
        var firstName = ParseIdentifier();
        if (ConsumeToken(new Token.Period()))
        {
            var secondName = ParseIdentifier();
            return new Table(
                TableName: secondName,
                SchemaName: firstName);
        }
        else
        {
            return new Table(
                TableName: firstName,
                SchemaName: null);
        }
    }

    /// <summary>Parse a <c>SET ROLE</c> statement. Expects SET to be consumed already.</summary>
    internal Statement ParseSetRole(ContextModifier? modifier)
    {
        ExpectKeywordIs(Keyword.ROLE);

        Ident? roleName;
        if (ParseKeyword(Keyword.NONE))
        {
            roleName = null;
        }
        else
        {
            roleName = ParseIdentifier();
        }
        return new Statement.Set(new Set.SetRole(
            ContextModifier: modifier,
            RoleName: roleName));
    }

    internal Sequence<Expr> ParseSetValues(bool parenthesizedAssignment)
    {
        var values = new List<Expr>();

        if (parenthesizedAssignment)
        {
            ExpectToken(new Token.LParen());
        }

        while (true)
        {
            Expr value;
            if (TryParseExprSubQuery() is { } subQueryExpr)
            {
                value = subQueryExpr;
            }
            else
            {
                // Rust: `else if let Ok(expr) = self.parse_expr()`: o erro é descartado, sem
                // restaurar a posição (decisão 1 da B2).
                Expr? parsedExpr;
                try
                {
                    parsedExpr = ParseExpr();
                }
                catch (ParserException)
                {
                    parsedExpr = null;
                }
                if (parsedExpr is { } expr)
                {
                    value = expr;
                }
                else
                {
                    value = ExpectedRef<Expr>("variable value", PeekTokenRef());
                }
            }

            values.Add(value);
            if (ConsumeToken(new Token.Comma()))
            {
                continue;
            }

            if (parenthesizedAssignment)
            {
                ExpectToken(new Token.RParen());
            }
            return values.ToSequence();
        }
    }

    internal ContextModifier? ParseContextModifier()
    {
        // Rust: `self.parse_one_of_keywords(..)?` sobre `Option`.
        if (ParseOneOfKeywords(Keyword.SESSION, Keyword.LOCAL, Keyword.GLOBAL) is not { } modifier)
        {
            return null;
        }

        return KeywordToModifier(modifier);
    }

    /// <summary>Parse a single SET statement assignment <c>var = expr</c>.</summary>
    internal SetAssignment ParseSetAssignment()
    {
        var scope = ParseContextModifier();

        ObjectName name;
        if (_dialect.SupportsParenthesizedSetVariables
            && ConsumeToken(new Token.LParen()))
        {
            // Parenthesized assignments are handled in the `parse_set` function after
            // trying to parse list of assignments using this function.
            // If a dialect supports both, and we find a LParen, we early exit from this function.
            name = ExpectedRef<ObjectName>("Unparenthesized assignment", PeekTokenRef());
        }
        else
        {
            name = ParseObjectName(false);
        }

        if (!(ConsumeToken(new Token.Eq()) || ParseKeyword(Keyword.TO)))
        {
            return ExpectedRef<SetAssignment>("assignment operator", PeekTokenRef());
        }

        var value = ParseExpr();

        return new SetAssignment(Scope: scope, Name: name, Value: value);
    }

    internal Statement ParseSet()
    {
        var hivevar = ParseKeyword(Keyword.HIVEVAR);

        // Modifier is either HIVEVAR: or a ContextModifier (LOCAL, SESSION, etc), not both
        ContextModifier? scope;
        if (!hivevar)
        {
            scope = ParseContextModifier();
        }
        else
        {
            scope = null;
        }

        if (hivevar)
        {
            ExpectToken(new Token.Colon());
        }

        if (MaybeParse(parser => parser.ParseSetRole(scope)) is { } setRoleStmt)
        {
            return setRoleStmt;
        }

        // Handle special cases first
        if (ParseKeywords(Keyword.TIME, Keyword.ZONE)
            || ParseKeyword(Keyword.TIMEZONE))
        {
            if (ConsumeToken(new Token.Eq()) || ParseKeyword(Keyword.TO))
            {
                return Statement.From(new Set.SingleAssignment(
                    Scope: scope,
                    Hivevar: hivevar,
                    Variable: ObjectName.From(Sequence.Of(Ident.From("TIMEZONE"))),
                    Values: ParseSetValues(false)));
            }
            else
            {
                // A shorthand alias for SET TIME ZONE that doesn't require
                // the assignment operator. It's originally PostgreSQL specific,
                // but we allow it for all the dialects
                return Statement.From(new Set.SetTimeZone(
                    Local: scope is ContextModifier.Local,
                    Value: ParseExpr()));
            }
        }
        else if (_dialect.SupportsSetNames && ParseKeyword(Keyword.NAMES))
        {
            if (ParseKeyword(Keyword.DEFAULT))
            {
                return Statement.From(new Set.SetNamesDefault());
            }
            var charsetName = ParseIdentifier();
            string? collationName;
            if (ParseOneOfKeywords(Keyword.COLLATE) is not null)
            {
                collationName = ParseLiteralString();
            }
            else
            {
                collationName = null;
            }

            return Statement.From(new Set.SetNames(
                CharsetName: charsetName,
                CollationName: collationName));
        }
        else if (ParseKeyword(Keyword.CHARACTERISTICS))
        {
            ExpectKeywords(Keyword.AS, Keyword.TRANSACTION);
            return Statement.From(new Set.SetTransaction(
                Modes: ParseTransactionModes(),
                Snapshot: null,
                Session: true));
        }
        else if (ParseKeyword(Keyword.TRANSACTION))
        {
            if (ParseKeyword(Keyword.SNAPSHOT))
            {
                var snapshotId = ParseValue();
                return Statement.From(new Set.SetTransaction(
                    Modes: Sequence<TransactionMode>.Empty,
                    Snapshot: snapshotId,
                    Session: false));
            }
            return Statement.From(new Set.SetTransaction(
                Modes: ParseTransactionModes(),
                Snapshot: null,
                Session: false));
        }
        else if (ParseKeyword(Keyword.AUTHORIZATION))
        {
            // Rust: `let scope = match scope { .. }` sombreia o externo (caso 72).
            ContextModifier authorizationScope;
            switch (scope)
            {
                case { } s:
                    authorizationScope = s;
                    break;
                case null:
                    return ExpectedAt<Statement>(
                        "SESSION, LOCAL, or other scope modifier before AUTHORIZATION",
                        GetCurrentIndex());
            }
            SetSessionAuthorizationParamKind authValue;
            if (ParseKeyword(Keyword.DEFAULT))
            {
                authValue = new SetSessionAuthorizationParamKind.Default();
            }
            else
            {
                var value = ParseIdentifier();
                authValue = new SetSessionAuthorizationParamKind.User(value);
            }
            return Statement.From(new Set.SetSessionAuthorization(new SetSessionAuthorizationParam(
                Scope: authorizationScope,
                Kind: authValue)));
        }

        if (_dialect.SupportsCommaSeparatedSetAssignments)
        {
            if (scope is not null)
            {
                PrevToken();
            }

            if (MaybeParseValue(parser => parser.ParseCommaSeparated(p => p.ParseSetAssignment())) is { } assignments)
            {
                if (assignments.Count > 1)
                {
                    return Statement.From(new Set.MultipleAssignments(Assignments: assignments));
                }
                else
                {
                    // Rust: `let SetAssignment { scope, name, value } = assignments.into_iter().next()
                    // .ok_or_else(..)?;` (o `scope` sombreia o externo; caso 72).
                    if (assignments.First is not { } firstAssignment)
                    {
                        throw new ParserException(new ParserError.ParserErrorValue(
                            "Expected at least one assignment"));
                    }
                    var assignmentScope = firstAssignment.Scope;
                    var name = firstAssignment.Name;
                    var value = firstAssignment.Value;

                    return Statement.From(new Set.SingleAssignment(
                        Scope: assignmentScope,
                        Hivevar: hivevar,
                        Variable: name,
                        Values: Sequence.Of(value)));
                }
            }
        }

        OneOrManyWithParens<ObjectName> variables;
        if (_dialect.SupportsParenthesizedSetVariables
            && ConsumeToken(new Token.LParen()))
        {
            var vars = new OneOrManyWithParens<ObjectName>.Many(
                ParseCommaSeparated(parser => parser.ParseIdentifier())
                    .Select(ident => ObjectName.From(Sequence.Of(ident)))
                    .ToSequence());
            ExpectToken(new Token.RParen());
            variables = vars;
        }
        else
        {
            variables = new OneOrManyWithParens<ObjectName>.One(ParseObjectName(false));
        }

        if (ConsumeToken(new Token.Eq()) || ParseKeyword(Keyword.TO))
        {
            Set stmt;
            switch (variables)
            {
                case OneOrManyWithParens<ObjectName>.One { Value: var @var }:
                    stmt = new Set.SingleAssignment(
                        Scope: scope,
                        Hivevar: hivevar,
                        Variable: @var,
                        Values: ParseSetValues(false));
                    break;
                case OneOrManyWithParens<ObjectName>.Many { Value: var vars }:
                    stmt = new Set.ParenthesizedAssignments(
                        Variables: vars,
                        Values: ParseSetValues(true));
                    break;
                default:
                    throw new InvalidOperationException($"Variante de OneOrManyWithParens não tratada: {variables.GetType().Name}");
            }

            return Statement.From(stmt);
        }

        if (_dialect.SupportsSetStmtWithoutOperator)
        {
            PrevToken();
            return ParseSetSessionParams();
        }

        return ExpectedRef<Statement>("equals sign or TO", PeekTokenRef());
    }

    /// <summary>Parse session parameter assignments after <c>SET</c> when no <c>=</c> or <c>TO</c> is present.</summary>
    public Statement ParseSetSessionParams()
    {
        if (ParseKeyword(Keyword.STATISTICS))
        {
            SessionParamStatsTopic topic;
            switch (ParseOneOfKeywords(
                Keyword.IO,
                Keyword.PROFILE,
                Keyword.TIME,
                Keyword.XML))
            {
                case Keyword.IO:
                    topic = new SessionParamStatsTopic.IO();
                    break;
                case Keyword.PROFILE:
                    topic = new SessionParamStatsTopic.Profile();
                    break;
                case Keyword.TIME:
                    topic = new SessionParamStatsTopic.Time();
                    break;
                case Keyword.XML:
                    topic = new SessionParamStatsTopic.Xml();
                    break;
                default:
                    return ExpectedRef<Statement>("IO, PROFILE, TIME or XML", PeekTokenRef());
            }
            var value = ParseSessionParamValue();
            return Statement.From(
                new Set.SetSessionParam(new SetSessionParamKind.Statistics(new SetSessionParamStatistics(
                    Topic: topic,
                    Value: value))));
        }
        else if (ParseKeyword(Keyword.IDENTITY_INSERT))
        {
            var obj = ParseObjectName(false);
            var value = ParseSessionParamValue();
            return Statement.From(new Set.SetSessionParam(new SetSessionParamKind.IdentityInsert(
                new SetSessionParamIdentityInsert(Obj: obj, Value: value))));
        }
        else if (ParseKeyword(Keyword.OFFSETS))
        {
            var keywords = ParseCommaSeparated(parser =>
            {
                var nextToken = parser.NextToken();
                switch (nextToken.Token)
                {
                    case Token.Word { Value: var w }:
                        return w.ToString();
                    default:
                        return parser.Expected<string>("SQL keyword", nextToken);
                }
            });
            var value = ParseSessionParamValue();
            return Statement.From(
                new Set.SetSessionParam(new SetSessionParamKind.Offsets(new SetSessionParamOffsets(
                    Keywords: keywords,
                    Value: value))));
        }
        else
        {
            var names = ParseCommaSeparated(parser =>
            {
                var nextToken = parser.NextToken();
                switch (nextToken.Token)
                {
                    case Token.Word { Value: var w }:
                        return w.ToString();
                    default:
                        return parser.Expected<string>("Session param name", nextToken);
                }
            });
            var value = ParseExpr().ToString();
            return Statement.From(
                new Set.SetSessionParam(new SetSessionParamKind.Generic(new SetSessionParamGeneric(
                    Names: names,
                    Value: value))));
        }
    }

    internal SessionParamValue ParseSessionParamValue()
    {
        if (ParseKeyword(Keyword.ON))
        {
            return new SessionParamValue.On();
        }
        else if (ParseKeyword(Keyword.OFF))
        {
            return new SessionParamValue.Off();
        }
        else
        {
            return ExpectedRef<SessionParamValue>("ON or OFF", PeekTokenRef());
        }
    }

    /// <summary>Parse a <c>SHOW</c> statement and dispatch to specific SHOW handlers.</summary>
    public Statement ParseShow()
    {
        var terse = ParseKeyword(Keyword.TERSE);
        var extended = ParseKeyword(Keyword.EXTENDED);
        var full = ParseKeyword(Keyword.FULL);
        var session = ParseKeyword(Keyword.SESSION);
        var global = ParseKeyword(Keyword.GLOBAL);
        var external = ParseKeyword(Keyword.EXTERNAL);
        if (ParseOneOfKeywords(Keyword.COLUMNS, Keyword.FIELDS) is not null)
        {
            return ParseShowColumns(extended, full);
        }
        else if (ParseKeyword(Keyword.TABLES))
        {
            return ParseShowTables(terse, extended, full, external);
        }
        else if (ParseKeywords(Keyword.MATERIALIZED, Keyword.VIEWS))
        {
            return ParseShowViews(terse, true);
        }
        else if (ParseKeyword(Keyword.VIEWS))
        {
            return ParseShowViews(terse, false);
        }
        else if (ParseKeyword(Keyword.FUNCTIONS))
        {
            return ParseShowFunctions();
        }
        else if (ParseKeyword(Keyword.PROCESSLIST))
        {
            return new Statement.ShowProcessList(Full: full);
        }
        else if (extended || full)
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                "EXTENDED/FULL are not supported with this type of SHOW query"));
        }
        else if (ParseOneOfKeywords(Keyword.CREATE) is not null)
        {
            return ParseShowCreate();
        }
        else if (ParseKeyword(Keyword.COLLATION))
        {
            return ParseShowCollation();
        }
        else if (ParseKeyword(Keyword.VARIABLES)
            && (_dialect is MySqlDialect or GenericDialect))
        {
            return new Statement.ShowVariables(
                Filter: ParseShowStatementFilter(),
                Session: session,
                Global: global);
        }
        else if (ParseKeyword(Keyword.STATUS)
            && (_dialect is MySqlDialect or GenericDialect))
        {
            return new Statement.ShowStatus(
                Filter: ParseShowStatementFilter(),
                Session: session,
                Global: global);
        }
        else if (ParseKeyword(Keyword.CATALOGS))
        {
            return ParseShowCatalogs(terse);
        }
        else if (ParseKeyword(Keyword.DATABASES))
        {
            return ParseShowDatabases(terse);
        }
        else if (ParseKeyword(Keyword.SCHEMAS))
        {
            return ParseShowSchemas(terse);
        }
        else if (ParseKeywords(Keyword.CHARACTER, Keyword.SET))
        {
            return ParseShowCharset(false);
        }
        else if (ParseKeyword(Keyword.CHARSET))
        {
            return ParseShowCharset(true);
        }
        else
        {
            return new Statement.ShowVariable(
                Variable: ParseIdentifiers());
        }
    }

    internal Statement ParseShowCharset(bool isShorthand)
    {
        // parse one of keywords
        return new Statement.ShowCharset(new ShowCharset(
            IsShorthand: isShorthand,
            Filter: ParseShowStatementFilter()));
    }

    internal Statement ParseShowCatalogs(bool terse)
    {
        var history = ParseKeyword(Keyword.HISTORY);
        var showOptions = ParseShowStmtOptions();
        return new Statement.ShowCatalogs(
            Terse: terse,
            History: history,
            ShowOptions: showOptions);
    }

    internal Statement ParseShowDatabases(bool terse)
    {
        var history = ParseKeyword(Keyword.HISTORY);
        var showOptions = ParseShowStmtOptions();
        return new Statement.ShowDatabases(
            Terse: terse,
            History: history,
            ShowOptions: showOptions);
    }

    internal Statement ParseShowSchemas(bool terse)
    {
        var history = ParseKeyword(Keyword.HISTORY);
        var showOptions = ParseShowStmtOptions();
        return new Statement.ShowSchemas(
            Terse: terse,
            History: history,
            ShowOptions: showOptions);
    }

    /// <summary>Parse <c>SHOW CREATE &lt;object&gt;</c> returning the corresponding <c>ShowCreate</c> statement.</summary>
    public Statement ParseShowCreate()
    {
        ShowCreateObject objType;
        switch (ExpectOneOfKeywords(
            Keyword.TABLE,
            Keyword.TRIGGER,
            Keyword.FUNCTION,
            Keyword.PROCEDURE,
            Keyword.EVENT,
            Keyword.VIEW))
        {
            case Keyword.TABLE:
                objType = new ShowCreateObject.Table();
                break;
            case Keyword.TRIGGER:
                objType = new ShowCreateObject.Trigger();
                break;
            case Keyword.FUNCTION:
                objType = new ShowCreateObject.Function();
                break;
            case Keyword.PROCEDURE:
                objType = new ShowCreateObject.Procedure();
                break;
            case Keyword.EVENT:
                objType = new ShowCreateObject.Event();
                break;
            case Keyword.VIEW:
                objType = new ShowCreateObject.View();
                break;
            case var keyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Unable to map keyword to ShowCreateObject: {keyword.ToString()}"));
        }

        var objName = ParseObjectName(false);

        return new Statement.ShowCreate(ObjType: objType, ObjName: objName);
    }

    /// <summary>Parse <c>SHOW COLUMNS</c>/<c>SHOW FIELDS</c> and return a <c>ShowColumns</c> statement.</summary>
    public Statement ParseShowColumns(bool extended, bool full)
    {
        var showOptions = ParseShowStmtOptions();
        return new Statement.ShowColumns(
            Extended: extended,
            Full: full,
            ShowOptions: showOptions);
    }

    internal Statement ParseShowTables(bool terse, bool extended, bool full, bool external)
    {
        var history = !external && ParseKeyword(Keyword.HISTORY);
        var showOptions = ParseShowStmtOptions();
        return new Statement.ShowTables(
            Terse: terse,
            History: history,
            Extended: extended,
            Full: full,
            External: external,
            ShowOptions: showOptions);
    }

    internal Statement ParseShowViews(bool terse, bool materialized)
    {
        var showOptions = ParseShowStmtOptions();
        return new Statement.ShowViews(
            Materialized: materialized,
            Terse: terse,
            ShowOptions: showOptions);
    }

    /// <summary>Parse <c>SHOW FUNCTIONS</c> and optional filter.</summary>
    public Statement ParseShowFunctions()
    {
        var filter = ParseShowStatementFilter();
        return new Statement.ShowFunctions(Filter: filter);
    }

    /// <summary>Parse <c>SHOW COLLATION</c> and optional filter.</summary>
    public Statement ParseShowCollation()
    {
        var filter = ParseShowStatementFilter();
        return new Statement.ShowCollation(Filter: filter);
    }

    /// <summary>Parse an optional filter used by <c>SHOW</c> statements (LIKE, ILIKE, WHERE, or literal).</summary>
    public ShowStatementFilter? ParseShowStatementFilter()
    {
        if (ParseKeyword(Keyword.LIKE))
        {
            return new ShowStatementFilter.Like(
                ParseLiteralString());
        }
        else if (ParseKeyword(Keyword.ILIKE))
        {
            return new ShowStatementFilter.ILike(
                ParseLiteralString());
        }
        else if (ParseKeyword(Keyword.WHERE))
        {
            return new ShowStatementFilter.Where(ParseExpr());
        }
        else
        {
            // Rust: `.map_or(Ok(None), |filter| Ok(Some(ShowStatementFilter::NoKeyword(filter))))`.
            if (MaybeParse(parser => parser.ParseLiteralString()) is { } filter)
            {
                return new ShowStatementFilter.NoKeyword(filter);
            }
            return null;
        }
    }

    /// <summary>Parse a <c>USE</c> statement (database/catalog/schema/warehouse/role selection).</summary>
    public Statement ParseUse()
    {
        // Determine which keywords are recognized by the current dialect
        Keyword? parsedKeyword;
        if (_dialect is HiveDialect)
        {
            // HiveDialect accepts USE DEFAULT; statement without any db specified
            if (ParseKeyword(Keyword.DEFAULT))
            {
                return new Statement.Use(new Use.Default());
            }
            parsedKeyword = null; // HiveDialect doesn't expect any other specific keyword after `USE`
        }
        else if (_dialect is DatabricksDialect)
        {
            parsedKeyword = ParseOneOfKeywords(Keyword.CATALOG, Keyword.DATABASE, Keyword.SCHEMA);
        }
        else if (_dialect is SnowflakeDialect)
        {
            parsedKeyword = ParseOneOfKeywords(
                Keyword.DATABASE,
                Keyword.SCHEMA,
                Keyword.WAREHOUSE,
                Keyword.ROLE,
                Keyword.SECONDARY);
        }
        else
        {
            parsedKeyword = null; // No specific keywords for other dialects, including GenericDialect
        }

        Use result;
        if (parsedKeyword is Keyword.SECONDARY)
        {
            result = ParseSecondaryRoles();
        }
        else
        {
            var objName = ParseObjectName(false);
            switch (parsedKeyword)
            {
                case Keyword.CATALOG:
                    result = new Use.Catalog(objName);
                    break;
                case Keyword.DATABASE:
                    result = new Use.Database(objName);
                    break;
                case Keyword.SCHEMA:
                    result = new Use.Schema(objName);
                    break;
                case Keyword.WAREHOUSE:
                    result = new Use.Warehouse(objName);
                    break;
                case Keyword.ROLE:
                    result = new Use.Role(objName);
                    break;
                default:
                    result = new Use.Object(objName);
                    break;
            }
        }

        return new Statement.Use(result);
    }

    internal Use ParseSecondaryRoles()
    {
        ExpectOneOfKeywords(Keyword.ROLES, Keyword.ROLE);
        if (ParseKeyword(Keyword.NONE))
        {
            return new Use.SecondaryRoles(new SecondaryRoles.None());
        }
        else if (ParseKeyword(Keyword.ALL))
        {
            return new Use.SecondaryRoles(new SecondaryRoles.All());
        }
        else
        {
            var roles = ParseCommaSeparated(parser => parser.ParseIdentifier());
            return new Use.SecondaryRoles(new SecondaryRoles.List(roles));
        }
    }

    /// <summary>Parse a table factor followed by any join clauses, returning <c>TableWithJoins</c>.</summary>
    public TableWithJoins ParseTableAndJoins()
    {
        var relation = ParseTableFactor();
        // Note that for keywords to be properly handled here, they need to be
        // added to `RESERVED_FOR_TABLE_ALIAS`, otherwise they may be parsed as
        // a table alias.
        var joins = ParseJoins();
        return new TableWithJoins(Relation: relation, Joins: joins);
    }

    internal Sequence<Join> ParseJoins()
    {
        var joins = new List<Join>();
        while (true)
        {
            var global = ParseKeyword(Keyword.GLOBAL);
            Join join;
            if (ParseKeyword(Keyword.CROSS))
            {
                JoinOperator joinOperator;
                if (ParseKeyword(Keyword.JOIN))
                {
                    joinOperator = new JoinOperator.CrossJoin(new JoinConstraint.None());
                }
                else if (ParseKeyword(Keyword.APPLY))
                {
                    // MSSQL extension, similar to CROSS JOIN LATERAL
                    joinOperator = new JoinOperator.CrossApply();
                }
                else
                {
                    return ExpectedRef<Sequence<Join>>("JOIN or APPLY after CROSS", PeekTokenRef());
                }
                var relation = ParseTableFactor();
                // Rust: `let join_operator = if .. { .. } else { join_operator };` (reatribuição).
                if (joinOperator is JoinOperator.CrossJoin
                    && _dialect.SupportsCrossJoinConstraint)
                {
                    var constraint = ParseJoinConstraint(false);
                    joinOperator = new JoinOperator.CrossJoin(constraint);
                }
                join = new Join(
                    Relation: relation,
                    Global: global,
                    JoinOperator: joinOperator);
            }
            else if (ParseKeyword(Keyword.OUTER))
            {
                // MSSQL extension, similar to LEFT JOIN LATERAL .. ON 1=1
                ExpectKeywordIs(Keyword.APPLY);
                join = new Join(
                    Relation: ParseTableFactor(),
                    Global: global,
                    JoinOperator: new JoinOperator.OuterApply());
            }
            else if (ParseKeyword(Keyword.ASOF))
            {
                ExpectKeywordIs(Keyword.JOIN);
                var relation = ParseTableFactor();
                ExpectKeywordIs(Keyword.MATCH_CONDITION);
                var matchCondition = ParseParenthesized(p => p.ParseExpr());
                join = new Join(
                    Relation: relation,
                    Global: global,
                    JoinOperator: new JoinOperator.AsOf(
                        MatchCondition: matchCondition,
                        Constraint: ParseJoinConstraint(false)));
            }
            else if (_dialect.SupportsArrayJoinSyntax
                && ParseKeywords(Keyword.INNER, Keyword.ARRAY, Keyword.JOIN))
            {
                // ClickHouse: INNER ARRAY JOIN
                join = new Join(
                    Relation: ParseTableFactor(),
                    Global: global,
                    JoinOperator: new JoinOperator.InnerArrayJoin());
            }
            else if (_dialect.SupportsArrayJoinSyntax
                && ParseKeywords(Keyword.LEFT, Keyword.ARRAY, Keyword.JOIN))
            {
                // ClickHouse: LEFT ARRAY JOIN
                join = new Join(
                    Relation: ParseTableFactor(),
                    Global: global,
                    JoinOperator: new JoinOperator.LeftArrayJoin());
            }
            else if (_dialect.SupportsArrayJoinSyntax
                && ParseKeywords(Keyword.ARRAY, Keyword.JOIN))
            {
                // ClickHouse: ARRAY JOIN
                join = new Join(
                    Relation: ParseTableFactor(),
                    Global: global,
                    JoinOperator: new JoinOperator.ArrayJoin());
            }
            else
            {
                var natural = ParseKeyword(Keyword.NATURAL);
                var peekKeyword = PeekTokenRef().Token is Token.Word { Value: var peekWord }
                    ? peekWord.Keyword
                    : Keyword.NoKeyword;

                // Rust: `join_operator_type` recebe construtores de variante (`JoinOperator::Inner`),
                // usados como função `fn(JoinConstraint) -> JoinOperator` (caso 27: lambdas).
                Func<JoinConstraint, JoinOperator> joinOperatorType;
                switch (peekKeyword)
                {
                    case Keyword.INNER or Keyword.JOIN:
                    {
                        var inner = ParseKeyword(Keyword.INNER); // [ INNER ]
                        ExpectKeywordIs(Keyword.JOIN);
                        if (inner)
                        {
                            joinOperatorType = c => new JoinOperator.Inner(c);
                        }
                        else
                        {
                            joinOperatorType = c => new JoinOperator.Join(c);
                        }
                        break;
                    }
                    case Keyword.LEFT or Keyword.RIGHT:
                    {
                        var kw = peekKeyword;
                        _ = NextToken(); // consume LEFT/RIGHT
                        var isLeft = kw == Keyword.LEFT;
                        var joinType = ParseOneOfKeywords(
                            Keyword.OUTER,
                            Keyword.SEMI,
                            Keyword.ANTI,
                            Keyword.JOIN);
                        switch (joinType)
                        {
                            case Keyword.OUTER:
                                ExpectKeywordIs(Keyword.JOIN);
                                if (isLeft)
                                {
                                    joinOperatorType = c => new JoinOperator.LeftOuter(c);
                                }
                                else
                                {
                                    joinOperatorType = c => new JoinOperator.RightOuter(c);
                                }
                                break;
                            case Keyword.SEMI:
                                ExpectKeywordIs(Keyword.JOIN);
                                if (isLeft)
                                {
                                    joinOperatorType = c => new JoinOperator.LeftSemi(c);
                                }
                                else
                                {
                                    joinOperatorType = c => new JoinOperator.RightSemi(c);
                                }
                                break;
                            case Keyword.ANTI:
                                ExpectKeywordIs(Keyword.JOIN);
                                if (isLeft)
                                {
                                    joinOperatorType = c => new JoinOperator.LeftAnti(c);
                                }
                                else
                                {
                                    joinOperatorType = c => new JoinOperator.RightAnti(c);
                                }
                                break;
                            case Keyword.JOIN:
                                if (isLeft)
                                {
                                    joinOperatorType = c => new JoinOperator.Left(c);
                                }
                                else
                                {
                                    joinOperatorType = c => new JoinOperator.Right(c);
                                }
                                break;
                            default:
                                throw new ParserException(new ParserError.ParserErrorValue(
                                    $"expected OUTER, SEMI, ANTI or JOIN after {kw.ToString()}"));
                        }
                        break;
                    }
                    case Keyword.ANTI:
                        _ = NextToken(); // consume ANTI
                        ExpectKeywordIs(Keyword.JOIN);
                        joinOperatorType = c => new JoinOperator.Anti(c);
                        break;
                    case Keyword.SEMI:
                        _ = NextToken(); // consume SEMI
                        ExpectKeywordIs(Keyword.JOIN);
                        joinOperatorType = c => new JoinOperator.Semi(c);
                        break;
                    case Keyword.FULL:
                        _ = NextToken(); // consume FULL
                        _ = ParseKeyword(Keyword.OUTER); // [ OUTER ]
                        ExpectKeywordIs(Keyword.JOIN);
                        joinOperatorType = c => new JoinOperator.FullOuter(c);
                        break;
                    case Keyword.OUTER:
                        return ExpectedRef<Sequence<Join>>("LEFT, RIGHT, or FULL", PeekTokenRef());
                    case Keyword.STRAIGHT_JOIN:
                        _ = NextToken(); // consume STRAIGHT_JOIN
                        joinOperatorType = c => new JoinOperator.StraightJoin(c);
                        break;
                    case var _ when natural:
                        return ExpectedRef<Sequence<Join>>("a join type after NATURAL", PeekTokenRef());
                    default:
                        // Rust: `_ => break`; depois do laço só há `Ok(joins)`.
                        return joins.ToSequence();
                }
                var relation = ParseTableFactor();

                if (!_dialect.SupportsLeftAssociativeJoinsWithoutParens
                    && !natural
                    && PeekParensLessNestedJoin())
                {
                    // Rust: `let joins` interno sombreia o externo (caso 72).
                    var nestedJoins = ParseJoins();
                    relation = new TableFactor.NestedJoin(
                        TableWithJoins: new TableWithJoins(Relation: relation, Joins: nestedJoins),
                        Alias: null);
                }

                var joinConstraint = ParseJoinConstraint(natural);
                join = new Join(
                    Relation: relation,
                    Global: global,
                    JoinOperator: joinOperatorType(joinConstraint));
            }
            joins.Add(join);
        }
    }

    internal bool PeekParensLessNestedJoin() =>
        PeekTokenRef().Token is Token.Word
        {
            Value.Keyword: Keyword.JOIN
                or Keyword.INNER
                or Keyword.LEFT
                or Keyword.RIGHT
                or Keyword.FULL,
        };
}
