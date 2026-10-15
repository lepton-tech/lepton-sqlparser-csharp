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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 18328–19376:
// parse_deny … parse_replace_elements).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse <see cref="Statement.Deny"/></summary>
    public Statement ParseDeny()
    {
        ExpectKeyword(Keyword.DENY);

        var (privileges, parsedObjects) = ParseGrantDenyRevokePrivilegesObjects();
        // Rust: `let objects = match objects { Some(o) => o, None => return parser_err!(..) }` (caso 72).
        GrantObjects objects;
        switch (parsedObjects)
        {
            case { } o:
                objects = o;
                break;
            case null:
                throw ParserErr(
                    "DENY statements must specify an object",
                    PeekTokenRef().Span.Start);
        }

        ExpectKeywordIs(Keyword.TO);
        var grantees = ParseGrantees();
        var cascade = ParseCascadeOption();
        Ident? grantedBy;
        if (ParseKeywords(Keyword.AS))
        {
            grantedBy = ParseIdentifier();
        }
        else
        {
            grantedBy = null;
        }

        return new Statement.Deny(new DenyStatement(
            Privileges: privileges,
            Objects: objects,
            Grantees: grantees,
            Cascade: cascade,
            GrantedBy: grantedBy));
    }

    /// <summary>Parse a REVOKE statement</summary>
    public Revoke ParseRevoke()
    {
        var grantOptionFor =
            ParseKeywords(Keyword.GRANT, Keyword.OPTION, Keyword.FOR);

        var (privileges, objects) = ParseGrantDenyRevokePrivilegesObjects();

        ExpectKeywordIs(Keyword.FROM);
        var grantees = ParseGrantees();

        Ident? grantedBy;
        if (ParseKeywords(Keyword.GRANTED, Keyword.BY))
        {
            grantedBy = ParseIdentifier();
        }
        else
        {
            grantedBy = null;
        }

        var cascade = ParseCascadeOption();

        return new Revoke(
            GrantOptionFor: grantOptionFor,
            Privileges: privileges,
            Objects: objects,
            Grantees: grantees,
            GrantedBy: grantedBy,
            Cascade: cascade);
    }

    /// <summary>Parse an REPLACE statement</summary>
    public Statement ParseReplace(TokenWithSpan replaceToken)
    {
        if (!(_dialect is MySqlDialect or GenericDialect))
        {
            throw ParserErr(
                "Unsupported statement REPLACE",
                PeekTokenRef().Span.Start);
        }

        var insert = ParseInsert(replaceToken);
        // Rust: `if let Statement::Insert(Insert { replace_into, .. }) = &mut insert { *replace_into = true; }`
        // sobre records imutáveis: reconstrução com `with` (decisão 6 da B2).
        if (insert is Statement.Insert { Value: var insertValue } insertStatement)
        {
            insert = insertStatement with { Value = insertValue with { ReplaceInto = true } };
        }

        return insert;
    }

    /// <summary>Parse an INSERT statement, returning a <c>Box</c>ed SetExpr</summary>
    /// <remarks>This is used to reduce the size of the stack frames in debug builds</remarks>
    internal SetExpr ParseInsertSetexprBoxed(TokenWithSpan insertToken) =>
        new SetExpr.Insert(ParseInsert(insertToken));

    /// <summary>Parse an INSERT statement</summary>
    public Statement ParseInsert(TokenWithSpan insertToken)
    {
        var optimizerHints = MaybeParseOptimizerHints();
        var @or = ParseConflictClause();
        MysqlInsertPriority? priority;
        if (!(_dialect is MySqlDialect or GenericDialect))
        {
            priority = null;
        }
        else if (ParseKeyword(Keyword.LOW_PRIORITY))
        {
            priority = new MysqlInsertPriority.LowPriority();
        }
        else if (ParseKeyword(Keyword.DELAYED))
        {
            priority = new MysqlInsertPriority.Delayed();
        }
        else if (ParseKeyword(Keyword.HIGH_PRIORITY))
        {
            priority = new MysqlInsertPriority.HighPriority();
        }
        else
        {
            priority = null;
        }

        var ignore = (_dialect is MySqlDialect or GenericDialect)
            && ParseKeyword(Keyword.IGNORE);

        var replaceInto = false;

        var overwrite = ParseKeyword(Keyword.OVERWRITE);
        var @into = ParseKeyword(Keyword.INTO);

        var local = ParseKeyword(Keyword.LOCAL);

        if (ParseKeyword(Keyword.DIRECTORY))
        {
            var path = ParseLiteralString();
            FileFormat? fileFormat;
            if (ParseKeywords(Keyword.STORED, Keyword.AS))
            {
                fileFormat = ParseFileFormat();
            }
            else
            {
                fileFormat = null;
            }
            var source = ParseQuery();
            return new Statement.Directory(
                Local: local,
                Path: path,
                Overwrite: overwrite,
                FileFormat: fileFormat,
                Source: source);
        }
        else
        {
            // Hive lets you put table here regardless
            var table = ParseKeyword(Keyword.TABLE);
            var tableObject = ParseTableObject();

            // `BY NAME` is an INSERT clause, not a table alias.
            TableAliasWithoutColumns? tableAlias;
            if (_dialect.SupportsInsertTableAlias
                && !PeekKeywords(Keyword.BY, Keyword.NAME)
                && !PeekSubQuery()
                && PeekOneOfKeywords(Keyword.DEFAULT, Keyword.VALUES) is null)
            {
                if (ParseKeyword(Keyword.AS))
                {
                    tableAlias = new TableAliasWithoutColumns(
                        Explicit: true,
                        Alias: ParseIdentifier());
                }
                else
                {
                    tableAlias = MaybeParse(parser => parser.ParseIdentifier()) is { } alias
                        ? new TableAliasWithoutColumns(
                            Explicit: false,
                            Alias: alias)
                        : null;
                }
            }
            else
            {
                tableAlias = null;
            }

            var isMysql = _dialect is MySqlDialect;

            var byName = false;
            // Rust: `let (columns, partitioned, after_columns, output, source, assignments) = if .. { .. } else { .. };`
            // Locais declarados antes e atribuídos em cada ramo (decisão 7 da B2). A tupla interna
            // `(columns, partitioned, after_columns)` do ramo `else` tem os mesmos nomes e é atribuída direto.
            Sequence<ObjectName> columns;
            Sequence<Expr>? partitioned;
            Sequence<Ident> afterColumns;
            OutputClause? output;
            Query? source;
            Sequence<Assignment> assignments;
            if (ParseKeywords(Keyword.DEFAULT, Keyword.VALUES))
            {
                columns = Sequence<ObjectName>.Empty;
                partitioned = null;
                afterColumns = Sequence<Ident>.Empty;
                output = null;
                source = null;
                assignments = Sequence<Assignment>.Empty;
            }
            else
            {
                if (!PeekSubqueryStart())
                {
                    columns =
                        ParseParenthesizedQualifiedColumnList(IsOptional.Optional, isMysql);

                    partitioned = ParseInsertPartition();
                    byName = ParseKeywords(Keyword.BY, Keyword.NAME);
                    // Hive allows you to specify columns after partitions as well if you want.
                    if (_dialect is HiveDialect)
                    {
                        afterColumns = ParseParenthesizedColumnList(IsOptional.Optional, false);
                    }
                    else
                    {
                        afterColumns = Sequence<Ident>.Empty;
                    }
                }
                else
                {
                    // Rust: `Default::default()`.
                    columns = Sequence<ObjectName>.Empty;
                    partitioned = null;
                    afterColumns = Sequence<Ident>.Empty;
                }

                output = MaybeParseOutputClause();

                if (PeekKeyword(Keyword.FORMAT)
                    || PeekKeyword(Keyword.SETTINGS))
                {
                    source = null;
                    assignments = Sequence<Assignment>.Empty;
                }
                else if (_dialect.SupportsInsertSet && ParseKeyword(Keyword.SET))
                {
                    source = null;
                    assignments = ParseCommaSeparated(p => p.ParseAssignment());
                }
                else
                {
                    source = ParseQuery();
                    assignments = Sequence<Assignment>.Empty;
                }
            }

            InputFormatClause? formatClause;
            Sequence<Setting>? settings;
            if (_dialect.SupportsInsertFormat)
            {
                // Settings always comes before `FORMAT` for ClickHouse:
                // <https://clickhouse.com/docs/en/sql-reference/statements/insert-into>
                settings = ParseSettings();

                if (ParseKeyword(Keyword.FORMAT))
                {
                    formatClause = ParseInputFormatClause();
                }
                else
                {
                    formatClause = null;
                }
            }
            else
            {
                // Rust: `Default::default()`.
                formatClause = null;
                settings = null;
            }

            InsertAliases? insertAlias;
            if ((_dialect is MySqlDialect or GenericDialect)
                && ParseKeyword(Keyword.AS))
            {
                var rowAlias = ParseObjectName(false);
                Sequence<Ident>? colAliases = ParseParenthesizedColumnList(IsOptional.Optional, false);
                insertAlias = new InsertAliases(
                    RowAlias: rowAlias,
                    ColAliases: colAliases);
            }
            else
            {
                insertAlias = null;
            }

            OnInsert? @on;
            if (ParseKeyword(Keyword.ON))
            {
                if (ParseKeyword(Keyword.CONFLICT))
                {
                    ConflictTarget? conflictTarget;
                    if (ParseKeywords(Keyword.ON, Keyword.CONSTRAINT))
                    {
                        conflictTarget = new ConflictTarget.OnConstraint(ParseObjectName(false));
                    }
                    else if (PeekTokenRef().Token is Token.LParen)
                    {
                        conflictTarget = new ConflictTarget.Columns(
                            ParseParenthesizedColumnList(IsOptional.Mandatory, false));
                    }
                    else
                    {
                        conflictTarget = null;
                    }

                    ExpectKeywordIs(Keyword.DO);
                    OnConflictAction action;
                    if (ParseKeyword(Keyword.NOTHING))
                    {
                        action = new OnConflictAction.DoNothing();
                    }
                    else
                    {
                        ExpectKeywordIs(Keyword.UPDATE);
                        ExpectKeywordIs(Keyword.SET);
                        // Rust: `let assignments` interno (caso 72).
                        var doUpdateAssignments = ParseCommaSeparated(p => p.ParseAssignment());
                        Expr? selection;
                        if (ParseKeyword(Keyword.WHERE))
                        {
                            selection = ParseExpr();
                        }
                        else
                        {
                            selection = null;
                        }
                        action = new OnConflictAction.DoUpdate(new DoUpdate(
                            Assignments: doUpdateAssignments,
                            Selection: selection));
                    }

                    @on = new OnInsert.OnConflict(new OnConflict(
                        ConflictTarget: conflictTarget,
                        Action: action));
                }
                else
                {
                    ExpectKeywordIs(Keyword.DUPLICATE);
                    ExpectKeywordIs(Keyword.KEY);
                    ExpectKeywordIs(Keyword.UPDATE);
                    var l = ParseCommaSeparated(p => p.ParseAssignment());

                    @on = new OnInsert.DuplicateKeyUpdate(l);
                }
            }
            else
            {
                @on = null;
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

            return Statement.From(new Insert(
                InsertToken: AttachedToken.From(insertToken),
                OptimizerHints: optimizerHints,
                Or: @or,
                Table: tableObject,
                TableAlias: tableAlias,
                Ignore: ignore,
                Into: @into,
                Overwrite: overwrite,
                ByName: byName,
                Partitioned: partitioned,
                Columns: columns,
                AfterColumns: afterColumns,
                Source: source,
                Assignments: assignments,
                HasTableKeyword: table,
                On: @on,
                Returning: returning,
                Output: output,
                ReplaceInto: replaceInto,
                Priority: priority,
                InsertAlias: insertAlias,
                Settings: settings,
                FormatClause: formatClause,
                MultiTableInsertType: null,
                MultiTableIntoClauses: Sequence<MultiTableInsertIntoClause>.Empty,
                MultiTableWhenClauses: Sequence<MultiTableInsertWhenClause>.Empty,
                MultiTableElseClause: null));
        }
    }

    /// <summary>Parses input format clause used for ClickHouse.</summary>
    /// <remarks><see href="https://clickhouse.com/docs/en/interfaces/formats"/></remarks>
    public InputFormatClause ParseInputFormatClause()
    {
        var ident = ParseIdentifier();
        var values =
            MaybeParseValue(p => p.ParseCommaSeparated(inner => inner.ParseExpr()))
            ?? Sequence<Expr>.Empty;

        return new InputFormatClause(Ident: ident, Values: values);
    }

    /// <summary>
    /// Returns true if the immediate tokens look like the
    /// beginning of a subquery. <c>(SELECT ...</c> or <c>((SELECT ...</c> etc.
    /// </summary>
    internal bool PeekSubqueryStart()
    {
        // Handle (SELECT, ((SELECT, (((SELECT, etc.
        // This makes INSERT consistent with other contexts where nested
        // parentheses around subqueries are handled by recursive descent.
        var i = 0;
        while (true)
        {
            switch (PeekNthTokenRef(i).Token)
            {
                case Token.LParen:
                    i += 1;
                    break;
                case Token.Word { Value: var w } when w.Keyword == Keyword.SELECT:
                    return i > 0;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Returns true if the immediate tokens look like the
    /// beginning of a subquery possibly preceded by CTEs;
    /// i.e. <c>(WITH ...</c> or <c>(SELECT ...</c>.
    /// </summary>
    internal bool PeekSubqueryOrCteStart() =>
        PeekTokensRef(2) is
        [
            { Token: Token.LParen },
            { Token: Token.Word { Value.Keyword: Keyword.SELECT or Keyword.WITH } },
        ];

    internal SqliteOnConflict? ParseConflictClause()
    {
        if (ParseKeywords(Keyword.OR, Keyword.REPLACE))
        {
            return new SqliteOnConflict.Replace();
        }
        else if (ParseKeywords(Keyword.OR, Keyword.ROLLBACK))
        {
            return new SqliteOnConflict.Rollback();
        }
        else if (ParseKeywords(Keyword.OR, Keyword.ABORT))
        {
            return new SqliteOnConflict.Abort();
        }
        else if (ParseKeywords(Keyword.OR, Keyword.FAIL))
        {
            return new SqliteOnConflict.Fail();
        }
        else if (ParseKeywords(Keyword.OR, Keyword.IGNORE))
        {
            return new SqliteOnConflict.Ignore();
        }
        else if (ParseKeyword(Keyword.REPLACE))
        {
            return new SqliteOnConflict.Replace();
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse an optional <c>PARTITION (...)</c> clause for INSERT statements.</summary>
    public Sequence<Expr>? ParseInsertPartition()
    {
        if (ParseKeyword(Keyword.PARTITION))
        {
            ExpectToken(new Token.LParen());
            Sequence<Expr>? partitionCols = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            return partitionCols;
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse optional Hive <c>INPUTFORMAT ... SERDE ...</c> clause used by LOAD DATA.</summary>
    public HiveLoadDataFormat? ParseLoadDataTableFormat()
    {
        if (ParseKeyword(Keyword.INPUTFORMAT))
        {
            var inputFormat = ParseExpr();
            ExpectKeywordIs(Keyword.SERDE);
            var serde = ParseExpr();
            return new HiveLoadDataFormat(
                InputFormat: inputFormat,
                Serde: serde);
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse an UPDATE statement, returning a <c>Box</c>ed SetExpr</summary>
    /// <remarks>This is used to reduce the size of the stack frames in debug builds</remarks>
    internal SetExpr ParseUpdateSetexprBoxed(TokenWithSpan updateToken) =>
        new SetExpr.Update(ParseUpdate(updateToken));

    /// <summary>Parse an <c>UPDATE</c> statement and return <c>Statement::Update</c>.</summary>
    public Statement ParseUpdate(TokenWithSpan updateToken)
    {
        var optimizerHints = MaybeParseOptimizerHints();
        var @or = ParseConflictClause();
        var table = ParseTableAndJoins();
        UpdateTableFromKind? fromBeforeSet;
        if (ParseKeyword(Keyword.FROM))
        {
            fromBeforeSet = new UpdateTableFromKind.BeforeSet(
                ParseTableWithJoins());
        }
        else
        {
            fromBeforeSet = null;
        }
        ExpectKeyword(Keyword.SET);
        var assignments = ParseCommaSeparated(p => p.ParseAssignment());

        var output = MaybeParseOutputClause();

        UpdateTableFromKind? @from;
        if (fromBeforeSet is null && ParseKeyword(Keyword.FROM))
        {
            @from = new UpdateTableFromKind.AfterSet(
                ParseTableWithJoins());
        }
        else
        {
            @from = fromBeforeSet;
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
        if (_dialect.SupportsUpdateOrderBy
            && ParseKeywords(Keyword.ORDER, Keyword.BY))
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
            limit = ParseExpr();
        }
        else
        {
            limit = null;
        }
        return Statement.From(new Update(
            UpdateToken: AttachedToken.From(updateToken),
            OptimizerHints: optimizerHints,
            Table: table,
            Assignments: assignments,
            From: @from,
            Selection: selection,
            Returning: returning,
            Output: output,
            Or: @or,
            OrderBy: orderBy,
            Limit: limit));
    }

    /// <summary>Parse a <c>var = expr</c> assignment, used in an UPDATE statement</summary>
    public Assignment ParseAssignment()
    {
        var target = ParseAssignmentTarget();
        if (_dialect.SupportsDoubleEqAssignment)
        {
            if (!ConsumeToken(new Token.Eq()) && !ConsumeToken(new Token.DoubleEq()))
            {
                return Expected<Assignment>("= or == (for assignment)", PeekToken());
            }
        }
        else
        {
            ExpectToken(new Token.Eq());
        }
        var value = ParseExpr();
        return new Assignment(Target: target, Value: value);
    }

    /// <summary>Parse the left-hand side of an assignment, used in an UPDATE statement</summary>
    public AssignmentTarget ParseAssignmentTarget()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            var columns = ParseCommaSeparated(p => p.ParseObjectName(false));
            ExpectToken(new Token.RParen());
            return new AssignmentTarget.Tuple(columns);
        }
        else
        {
            var column = ParseObjectName(false);
            return new AssignmentTarget.ColumnName(column);
        }
    }

    /// <summary>Parse a single function argument, handling named and unnamed variants.</summary>
    public FunctionArg ParseFunctionArgs()
    {
        // Parse the argument expression once, then check for a named-arg
        // operator. Parsing it speculatively and re-parsing on the unnamed
        // path is O(2^depth) on nested calls like `CAST(CASE (CAST(CASE (…`.
        if (_dialect.SupportsNamedFnArgsWithExprName)
        {
            var expr = ParseWildcardExpr();
            // A wildcard is never a named-arg name; only the unnamed form applies.
            if (expr is not (Expr.Wildcard or Expr.QualifiedWildcard))
            {
                if (MaybeParse(p => p.ParseFunctionNamedArgOperator()) is { } @operator)
                {
                    var namedArg = FunctionArgExpr.From(ParseWildcardExpr());
                    return new FunctionArg.ExprNamed(
                        Name: expr,
                        Arg: namedArg,
                        Operator: @operator);
                }
            }
            var exprArgExpr = FunctionArgExprFromWildcard(expr);
            return new FunctionArg.Unnamed(
                MaybeParseAliasedFunctionArg(exprArgExpr));
        }

        var arg = MaybeParse<FunctionArg>(p =>
        {
            var name = p.ParseIdentifier();
            var @operator = p.ParseFunctionNamedArgOperator();
            var arg = FunctionArgExpr.From(p.ParseWildcardExpr());
            return new FunctionArg.Named(
                Name: name,
                Arg: arg,
                Operator: @operator);
        });
        if (arg is { } namedFunctionArg)
        {
            return namedFunctionArg;
        }
        var wildcardExpr = ParseWildcardExpr();
        var argExpr = FunctionArgExprFromWildcard(wildcardExpr);
        return new FunctionArg.Unnamed(
            MaybeParseAliasedFunctionArg(argExpr));
    }

    /// <summary>
    /// Wrap an already-parsed expression as a function argument, parsing any
    /// trailing wildcard options (e.g. Snowflake's <c>HASH(* EXCLUDE(col))</c>).
    /// </summary>
    internal FunctionArgExpr FunctionArgExprFromWildcard(Expr wildcardExpr)
    {
        switch (wildcardExpr)
        {
            case Expr.Wildcard { Value: var token } when _dialect.SupportsSelectWildcardExclude:
            {
                // Parse the options the same way SELECT items do.
                var opts = ParseWildcardAdditionalOptions(token.Value);
                if (opts.OptExclude is not null
                    || opts.OptExcept is not null
                    || opts.OptReplace is not null
                    || opts.OptRename is not null
                    || opts.OptIlike is not null)
                {
                    return new FunctionArgExpr.WildcardWithOptions(opts);
                }
                else
                {
                    return FunctionArgExpr.From(wildcardExpr);
                }
            }
            case var other:
                return FunctionArgExpr.From(other);
        }
    }

    /// <summary>
    /// Parse an optional <c>AS &lt;alias&gt;</c> on an unnamed function argument
    /// (e.g. <c>XMLFOREST(a AS x)</c> in PostgreSQL).
    /// </summary>
    internal FunctionArgExpr MaybeParseAliasedFunctionArg(FunctionArgExpr argExpr)
    {
        switch (argExpr)
        {
            // A guarda consome o `AS`, como no Rust (o `when` só roda se o padrão casar).
            case FunctionArgExpr.Expr { Value: var expr }
                when _dialect.SupportsAliasedFunctionArgs
                    && ParseKeyword(Keyword.AS):
                return new FunctionArgExpr.Expr(new Expr.Named(
                    Expr: expr,
                    Name: ParseIdentifier()));
            case var other:
                return other;
        }
    }

    internal FunctionArgOperator ParseFunctionNamedArgOperator()
    {
        if (ParseKeyword(Keyword.VALUE))
        {
            return new FunctionArgOperator.Value();
        }
        var tok = NextToken();
        switch (tok.Token)
        {
            case Token.RArrow when _dialect.SupportsNamedFnArgsWithRarrowOperator:
                return new FunctionArgOperator.RightArrow();
            case Token.Eq when _dialect.SupportsNamedFnArgsWithEqOperator:
                return new FunctionArgOperator.EqualsVariant();
            case Token.Assignment
                when _dialect
                    .SupportsNamedFnArgsWithAssignmentOperator:
                return new FunctionArgOperator.Assignment();
            case Token.Colon when _dialect.SupportsNamedFnArgsWithColonOperator:
                return new FunctionArgOperator.Colon();
            default:
                PrevToken();
                return Expected<FunctionArgOperator>("argument operator", tok);
        }
    }

    /// <summary>Parse an optional, comma-separated list of function arguments (consumes closing paren).</summary>
    public Sequence<FunctionArg> ParseOptionalArgs()
    {
        if (ConsumeToken(new Token.RParen()))
        {
            return Sequence<FunctionArg>.Empty;
        }
        else
        {
            var args = ParseCommaSeparated(p => p.ParseFunctionArgs());
            ExpectToken(new Token.RParen());
            return args;
        }
    }

    internal TableFunctionArgs ParseTableFunctionArgs()
    {
        if (ConsumeToken(new Token.RParen()))
        {
            return new TableFunctionArgs(
                Args: Sequence<FunctionArg>.Empty,
                Settings: null);
        }
        var args = new List<FunctionArg>();
        // Rust: `let settings = loop { .. break Some(settings); .. break None; };`.
        Sequence<Setting>? settings;
        while (true)
        {
            if (ParseSettings() is { } parsedSettings)
            {
                settings = parsedSettings;
                break;
            }
            args.Add(ParseFunctionArgs());
            if (IsParseCommaSeparatedEnd())
            {
                settings = null;
                break;
            }
        }
        ExpectToken(new Token.RParen());
        return new TableFunctionArgs(Args: args.ToSequence(), Settings: settings);
    }

    /// <summary>
    /// Parses a potentially empty list of arguments to a function
    /// (including the closing parenthesis).
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <code>
    /// FIRST_VALUE(x ORDER BY 1,2,3);
    /// FIRST_VALUE(x IGNORE NULL);
    /// </code>
    /// </remarks>
    internal FunctionArgumentList ParseFunctionArgumentList()
    {
        var clauses = new List<FunctionArgumentClause>();

        // Handle clauses that may exist with an empty argument list

        if (ParseJsonNullClause() is { } nullClause)
        {
            clauses.Add(new FunctionArgumentClause.JsonNullClause(nullClause));
        }

        if (MaybeParseJsonReturningClause() is { } jsonReturningClause)
        {
            clauses.Add(new FunctionArgumentClause.JsonReturningClause(
                jsonReturningClause));
        }

        if (ConsumeToken(new Token.RParen()))
        {
            return new FunctionArgumentList(
                DuplicateTreatment: null,
                Args: Sequence<FunctionArg>.Empty,
                Clauses: clauses.ToSequence());
        }

        var duplicateTreatment = ParseDuplicateTreatment();
        var args = ParseCommaSeparated(p => p.ParseFunctionArgs());

        if (ParseKeyword(Keyword.WHERE))
        {
            clauses.Add(new FunctionArgumentClause.Where(ParseExpr()));
        }

        if (_dialect.SupportsWindowFunctionNullTreatmentArg)
        {
            if (ParseNullTreatment() is { } nullTreatment)
            {
                clauses.Add(new FunctionArgumentClause.IgnoreOrRespectNulls(nullTreatment));
            }
        }

        if (ParseKeywords(Keyword.ORDER, Keyword.BY))
        {
            clauses.Add(new FunctionArgumentClause.OrderBy(
                ParseCommaSeparated(p => p.ParseOrderByExpr())));
        }

        if (ParseKeyword(Keyword.LIMIT))
        {
            clauses.Add(new FunctionArgumentClause.Limit(ParseExpr()));
        }

        if ((_dialect is GenericDialect or BigQueryDialect)
            && ParseKeyword(Keyword.HAVING))
        {
            HavingBoundKind kind;
            switch (ExpectOneOfKeywords(Keyword.MIN, Keyword.MAX))
            {
                case Keyword.MIN:
                    kind = new HavingBoundKind.Min();
                    break;
                case Keyword.MAX:
                    kind = new HavingBoundKind.Max();
                    break;
                case var unexpectedKeyword:
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in having bound"));
            }
            clauses.Add(new FunctionArgumentClause.Having(new HavingBound(
                kind,
                ParseExpr())));
        }

        if ((_dialect is GenericDialect or MySqlDialect)
            && ParseKeyword(Keyword.SEPARATOR))
        {
            clauses.Add(new FunctionArgumentClause.Separator(ParseValue()));
        }

        if (ParseListaggOnOverflow() is { } onOverflow)
        {
            clauses.Add(new FunctionArgumentClause.OnOverflow(onOverflow));
        }

        // Rust: segundo `if let Some(null_clause)` / `if let Some(json_returning_clause)` no mesmo bloco (caso 72).
        if (ParseJsonNullClause() is { } trailingNullClause)
        {
            clauses.Add(new FunctionArgumentClause.JsonNullClause(trailingNullClause));
        }

        if (MaybeParseJsonReturningClause() is { } trailingJsonReturningClause)
        {
            clauses.Add(new FunctionArgumentClause.JsonReturningClause(
                trailingJsonReturningClause));
        }

        ExpectToken(new Token.RParen());
        return new FunctionArgumentList(
            DuplicateTreatment: duplicateTreatment,
            Args: args,
            Clauses: clauses.ToSequence());
    }

    internal JsonNullClause? ParseJsonNullClause()
    {
        if (ParseKeywords(Keyword.ABSENT, Keyword.ON, Keyword.NULL))
        {
            return new JsonNullClause.AbsentOnNull();
        }
        else if (ParseKeywords(Keyword.NULL, Keyword.ON, Keyword.NULL))
        {
            return new JsonNullClause.NullOnNull();
        }
        else
        {
            return null;
        }
    }

    internal JsonReturningClause? MaybeParseJsonReturningClause()
    {
        if (ParseKeyword(Keyword.RETURNING))
        {
            var dataType = ParseDataType();
            return new JsonReturningClause(DataType: dataType);
        }
        else
        {
            return null;
        }
    }

    internal DuplicateTreatment? ParseDuplicateTreatment()
    {
        var loc = PeekTokenRef().Span.Start;
        switch ((
            ParseKeyword(Keyword.ALL),
            ParseKeyword(Keyword.DISTINCT)))
        {
            case (true, false):
                return new DuplicateTreatment.All();
            case (false, true):
                return new DuplicateTreatment.Distinct();
            case (false, false):
                return null;
            case (true, true):
                throw ParserErr("Cannot specify both ALL and DISTINCT", loc);
        }
    }

    /// <summary>Parse a comma-delimited list of projections after SELECT</summary>
    public SelectItem ParseSelectItem()
    {
        // `GetReservedKeywordsForSelectItemOperator` (IReadOnlyList) passado ao `params Keyword[]` (decisão 4 da B4);
        // `format!("{keyword:?}")` → nome da variante (caso 46).
        var prefix = ParseOneOfKeywords(
                _dialect
                    .GetReservedKeywordsForSelectItemOperator.ToArray())
            is { } keyword
            ? new Ident(keyword.ToString())
            : null;

        switch (ParseWildcardExpr())
        {
            // Rust: o `prefix` do padrão sombreia o `prefix` externo (caso 72).
            case Expr.QualifiedWildcard { Prefix: var wildcardPrefix, Token: var token }:
                return new SelectItem.QualifiedWildcard(
                    new SelectItemQualifiedWildcardKind.ObjectName(wildcardPrefix),
                    ParseWildcardAdditionalOptions(token.Value));
            case Expr.Wildcard { Value: var token }:
                return new SelectItem.Wildcard(
                    ParseWildcardAdditionalOptions(token.Value));
            // Rust: `v.value.to_lowercase()` → RustString.ToLowercase (convencoes.md §10).
            case Expr.Identifier { Value: var v } when RustString.ToLowercase(v.Value) == "from" && v.QuoteStyle is null:
                throw ParserErr(
                    $"Expected an expression, found: {v}",
                    PeekTokenRef().Span.Start);
            case Expr.BinaryOp { Left: var left, Op: BinaryOperator.Eq, Right: var right }
                when _dialect.SupportsEqAliasAssignment
                    && left is Expr.Identifier:
            {
                if (right is Expr.Identifier { Value: var v } && RustString.ToLowercase(v.Value) == "from" && v.QuoteStyle is null)
                {
                    throw ParserErr(
                        $"Expected an expression, found: {right}",
                        PeekTokenRef().Span.Start);
                }
                if (left is not Expr.Identifier { Value: var alias })
                {
                    throw ParserErr(
                        "BUG: expected identifier expression as alias",
                        PeekTokenRef().Span.Start);
                }
                return new SelectItem.ExprWithAlias(
                    Expr: right,
                    Alias: alias);
            }
            // A guarda consome `.*`, como no Rust (o `when` só roda se os padrões anteriores falharem).
            case var expr when _dialect.SupportsSelectExprStar
                && ConsumeTokens(new Token.Period(), new Token.Mul()):
            {
                var wildcardToken = GetPreviousToken();
                return new SelectItem.QualifiedWildcard(
                    new SelectItemQualifiedWildcardKind.Expr(expr),
                    ParseWildcardAdditionalOptions(wildcardToken));
            }
            case var expr when _dialect.SupportsSelectItemMultiColumnAlias
                && PeekKeyword(Keyword.AS)
                && PeekNthToken(1).Token is Token.LParen:
            {
                ExpectKeyword(Keyword.AS);
                ExpectToken(new Token.LParen());
                var aliases = ParseCommaSeparated(p => p.ParseIdentifier());
                ExpectToken(new Token.RParen());
                return new SelectItem.ExprWithAliases(
                    Expr: MaybePrefixedExpr(expr, prefix),
                    Aliases: aliases);
            }
            case var expr:
                switch (MaybeParseSelectItemAlias())
                {
                    case { } alias:
                        return new SelectItem.ExprWithAlias(
                            Expr: MaybePrefixedExpr(expr, prefix),
                            Alias: alias);
                    case null:
                        return new SelectItem.UnnamedExpr(MaybePrefixedExpr(expr, prefix));
                }
        }
    }

    /// <summary>Parse an <see cref="WildcardAdditionalOptions"/> information for wildcard select items.</summary>
    /// <remarks>If it is not possible to parse it, will return an option.</remarks>
    public WildcardAdditionalOptions ParseWildcardAdditionalOptions(TokenWithSpan wildcardToken)
    {
        IlikeSelectItem? optIlike;
        if (_dialect.SupportsSelectWildcardIlike)
        {
            optIlike = ParseOptionalSelectItemIlike();
        }
        else
        {
            optIlike = null;
        }
        ExcludeSelectItem? optExclude;
        if (optIlike is null && _dialect.SupportsSelectWildcardExclude)
        {
            optExclude = ParseOptionalSelectItemExclude();
        }
        else
        {
            optExclude = null;
        }
        ExceptSelectItem? optExcept;
        if (_dialect.SupportsSelectWildcardExcept)
        {
            optExcept = ParseOptionalSelectItemExcept();
        }
        else
        {
            optExcept = null;
        }
        ReplaceSelectItem? optReplace;
        if (_dialect.SupportsSelectWildcardReplace)
        {
            optReplace = ParseOptionalSelectItemReplace();
        }
        else
        {
            optReplace = null;
        }
        RenameSelectItem? optRename;
        if (_dialect.SupportsSelectWildcardRename)
        {
            optRename = ParseOptionalSelectItemRename();
        }
        else
        {
            optRename = null;
        }

        Ident? optAlias;
        if (_dialect.SupportsSelectWildcardWithAlias)
        {
            optAlias = MaybeParseSelectItemAlias();
        }
        else
        {
            optAlias = null;
        }

        return new WildcardAdditionalOptions(
            WildcardToken: AttachedToken.From(wildcardToken),
            OptIlike: optIlike,
            OptExclude: optExclude,
            OptExcept: optExcept,
            OptRename: optRename,
            OptReplace: optReplace,
            OptAlias: optAlias);
    }

    /// <summary>Parse an <see cref="IlikeSelectItem">Ilike</see> information for wildcard select items.</summary>
    /// <remarks>If it is not possible to parse it, will return an option.</remarks>
    public IlikeSelectItem? ParseOptionalSelectItemIlike()
    {
        IlikeSelectItem? optIlike;
        if (ParseKeyword(Keyword.ILIKE))
        {
            var nextToken = NextToken();
            string pattern;
            switch (nextToken.Token)
            {
                case Token.SingleQuotedString { Value: var s }:
                    pattern = s;
                    break;
                default:
                    return Expected<IlikeSelectItem?>("ilike pattern", nextToken);
            }
            optIlike = new IlikeSelectItem(Pattern: pattern);
        }
        else
        {
            optIlike = null;
        }
        return optIlike;
    }

    /// <summary>Parse an <see cref="ExcludeSelectItem">Exclude</see> information for wildcard select items.</summary>
    /// <remarks>If it is not possible to parse it, will return an option.</remarks>
    public ExcludeSelectItem? ParseOptionalSelectItemExclude()
    {
        ExcludeSelectItem? optExclude;
        if (ParseKeyword(Keyword.EXCLUDE))
        {
            if (ConsumeToken(new Token.LParen()))
            {
                var columns =
                    ParseCommaSeparated(parser => parser.ParseObjectName(false));
                ExpectToken(new Token.RParen());
                optExclude = new ExcludeSelectItem.Multiple(columns);
            }
            else
            {
                var column = ParseObjectName(false);
                optExclude = new ExcludeSelectItem.Single(column);
            }
        }
        else
        {
            optExclude = null;
        }

        return optExclude;
    }

    /// <summary>Parse an <see cref="ExceptSelectItem">Except</see> information for wildcard select items.</summary>
    /// <remarks>If it is not possible to parse it, will return an option.</remarks>
    public ExceptSelectItem? ParseOptionalSelectItemExcept()
    {
        ExceptSelectItem? optExcept;
        if (ParseKeyword(Keyword.EXCEPT))
        {
            if (PeekTokenRef().Token is Token.LParen)
            {
                var idents = ParseParenthesizedColumnList(IsOptional.Mandatory, false);
                // Rust: `match &idents[..] { [] => .., [first, idents @ ..] => .. }`.
                if (idents.Count == 0)
                {
                    return ExpectedRef<ExceptSelectItem?>(
                        "at least one column should be parsed by the expect clause",
                        PeekTokenRef());
                }
                else
                {
                    optExcept = new ExceptSelectItem(
                        FirstElement: idents[0],
                        AdditionalElements: idents.Skip(1).ToSequence());
                }
            }
            else
            {
                // Clickhouse allows EXCEPT column_name
                var ident = ParseIdentifier();
                optExcept = new ExceptSelectItem(
                    FirstElement: ident,
                    AdditionalElements: Sequence<Ident>.Empty);
            }
        }
        else
        {
            optExcept = null;
        }

        return optExcept;
    }

    /// <summary>Parse a <see cref="RenameSelectItem">Rename</see> information for wildcard select items.</summary>
    public RenameSelectItem? ParseOptionalSelectItemRename()
    {
        RenameSelectItem? optRename;
        if (ParseKeyword(Keyword.RENAME))
        {
            if (ConsumeToken(new Token.LParen()))
            {
                var idents =
                    ParseCommaSeparated(parser => parser.ParseIdentifierWithAlias());
                ExpectToken(new Token.RParen());
                optRename = new RenameSelectItem.Multiple(idents);
            }
            else
            {
                var ident = ParseIdentifierWithAlias();
                optRename = new RenameSelectItem.Single(ident);
            }
        }
        else
        {
            optRename = null;
        }

        return optRename;
    }

    /// <summary>Parse a <see cref="ReplaceSelectItem">Replace</see> information for wildcard select items.</summary>
    public ReplaceSelectItem? ParseOptionalSelectItemReplace()
    {
        ReplaceSelectItem? optReplace;
        if (ParseKeyword(Keyword.REPLACE))
        {
            if (ConsumeToken(new Token.LParen()))
            {
                var items = ParseCommaSeparated(parser => parser.ParseReplaceElements());
                ExpectToken(new Token.RParen());
                optReplace = new ReplaceSelectItem(Items: items);
            }
            else
            {
                var tok = NextToken();
                return Expected<ReplaceSelectItem?>("( after REPLACE but", tok);
            }
        }
        else
        {
            optReplace = null;
        }

        return optReplace;
    }

    /// <summary>Parse a single element of a <c>REPLACE (...)</c> select-item clause.</summary>
    public ReplaceSelectElement ParseReplaceElements()
    {
        var expr = ParseExpr();
        var asKeyword = ParseKeyword(Keyword.AS);
        var ident = ParseIdentifier();
        return new ReplaceSelectElement(
            Expr: expr,
            ColumnName: ident,
            AsKeyword: asKeyword);
    }
}
