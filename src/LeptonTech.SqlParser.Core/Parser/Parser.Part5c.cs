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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 19377–20305:
// parse_asc_desc … parse_optimize_table).
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>
    /// Parse ASC or DESC, returns an Option with true if ASC, false of DESC or <c>None</c> if none of
    /// them.
    /// </summary>
    public bool? ParseAscDesc()
    {
        if (ParseKeyword(Keyword.ASC))
        {
            return true;
        }
        else if (ParseKeyword(Keyword.DESC))
        {
            return false;
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse ASC or DESC and map to <see cref="OrderBySort"/>.</summary>
    internal OrderBySort? ParseOptionalOrderBySort()
    {
        switch (ParseAscDesc())
        {
            case true:
                return new OrderBySort.Asc();
            case false:
                return new OrderBySort.Desc();
            case null:
                return null;
        }
    }

    /// <summary>Parse an <see cref="OrderByExpr"/> expression.</summary>
    public OrderByExpr ParseOrderByExpr()
    {
        var (orderBy, _) = ParseOrderByExprInner(false);
        return orderBy;
    }

    /// <summary>Parse an <see cref="IndexColumn"/>.</summary>
    public IndexColumn ParseCreateIndexExpr()
    {
        var (column, operatorClass) = ParseOrderByExprInner(true);
        return new IndexColumn(
            Column: column,
            OperatorClass: operatorClass);
    }

    internal (OrderByExpr OrderByExpr, ObjectName? OperatorClass) ParseOrderByExprInner(bool withOperatorClass)
    {
        var expr = ParseExpr();

        ObjectName? operatorClass;
        if (withOperatorClass)
        {
            // We check that if non of the following keywords are present, then we parse an
            // identifier as operator class.
            if (PeekOneOfKeywords(Keyword.ASC, Keyword.DESC, Keyword.NULLS, Keyword.WITH) is not null)
            {
                operatorClass = null;
            }
            else
            {
                operatorClass = MaybeParse(parser => parser.ParseObjectName(false));
            }
        }
        else
        {
            operatorClass = null;
        }

        OrderByOptions options;
        if (!withOperatorClass
            && _dialect.SupportsOrderByUsingOperator
            && ParseKeyword(Keyword.USING))
        {
            var op = ParseOrderByUsingOperator();
            options = new OrderByOptions(
                Sort: new OrderBySort.Using(op),
                NullsFirst: ParseNullOrderingModifier());
        }
        else
        {
            options = ParseOrderByOptions();
        }

        WithFill? withFill;
        if (_dialect.SupportsWithFill
            && ParseKeywords(Keyword.WITH, Keyword.FILL))
        {
            withFill = ParseWithFill();
        }
        else
        {
            withFill = null;
        }

        return (
            new OrderByExpr(
                Expr: expr,
                Options: options,
                WithFill: withFill),
            operatorClass);
    }

    internal ObjectName ParseOrderByUsingOperator()
    {
        if (ParseKeyword(Keyword.OPERATOR))
        {
            ExpectToken(new Token.LParen());
            var operatorName = ParseOperatorName();
            ExpectToken(new Token.RParen());
            return operatorName;
        }

        var token = NextToken();
        return ObjectName.From(Sequence.Of(new Ident(token.Token.ToString())));
    }

    internal bool? ParseNullOrderingModifier()
    {
        if (ParseKeywords(Keyword.NULLS, Keyword.FIRST))
        {
            return true;
        }
        else if (ParseKeywords(Keyword.NULLS, Keyword.LAST))
        {
            return false;
        }
        else
        {
            return null;
        }
    }

    internal OrderByOptions ParseOrderByOptions()
    {
        var sort = ParseOptionalOrderBySort();
        var nullsFirst = ParseNullOrderingModifier();

        return new OrderByOptions(Sort: sort, NullsFirst: nullsFirst);
    }

    // Parse a WITH FILL clause (ClickHouse dialect)
    // that follow the WITH FILL keywords in a ORDER BY clause
    /// <summary>Parse a <c>WITH FILL</c> clause used in ORDER BY (ClickHouse dialect).</summary>
    public WithFill ParseWithFill()
    {
        Expr? @from;
        if (ParseKeyword(Keyword.FROM))
        {
            @from = ParseExpr();
        }
        else
        {
            @from = null;
        }

        Expr? to;
        if (ParseKeyword(Keyword.TO))
        {
            to = ParseExpr();
        }
        else
        {
            to = null;
        }

        Expr? step;
        if (ParseKeyword(Keyword.STEP))
        {
            step = ParseExpr();
        }
        else
        {
            step = null;
        }

        return new WithFill(From: @from, To: to, Step: step);
    }

    /// <summary>
    /// Parse a set of comma separated INTERPOLATE expressions (ClickHouse dialect)
    /// that follow the INTERPOLATE keyword in an ORDER BY clause with the WITH FILL modifier
    /// </summary>
    public Interpolate? ParseInterpolations()
    {
        if (!ParseKeyword(Keyword.INTERPOLATE))
        {
            return null;
        }

        if (ConsumeToken(new Token.LParen()))
        {
            var interpolations =
                ParseCommaSeparated0(p => p.ParseInterpolation(), new Token.RParen());
            ExpectToken(new Token.RParen());
            // INTERPOLATE () and INTERPOLATE ( ... ) variants
            return new Interpolate(
                Exprs: interpolations);
        }

        // INTERPOLATE
        return new Interpolate(Exprs: null);
    }

    /// <summary>Parse a INTERPOLATE expression (ClickHouse dialect)</summary>
    public InterpolateExpr ParseInterpolation()
    {
        var column = ParseIdentifier();
        Expr? expr;
        if (ParseKeyword(Keyword.AS))
        {
            expr = ParseExpr();
        }
        else
        {
            expr = null;
        }
        return new InterpolateExpr(Column: column, Expr: expr);
    }

    /// <summary>
    /// Parse a TOP clause, MSSQL equivalent of LIMIT,
    /// that follows after <c>SELECT [DISTINCT]</c>.
    /// </summary>
    public Top ParseTop()
    {
        TopQuantity? quantity;
        if (ConsumeToken(new Token.LParen()))
        {
            // Rust: `let quantity` interno (caso 72).
            var quantityExpr = ParseExpr();
            ExpectToken(new Token.RParen());
            quantity = new TopQuantity.Expr(quantityExpr);
        }
        else
        {
            var nextToken = NextToken();
            ulong quantityConstant;
            switch (nextToken.Token)
            {
                case Token.Number { Value: var s }:
                    quantityConstant = Parse<ulong>(s, nextToken.Span.Start);
                    break;
                default:
                    quantityConstant = Expected<ulong>("literal int", nextToken);
                    break;
            }
            quantity = new TopQuantity.Constant(quantityConstant);
        }

        var percent = ParseKeyword(Keyword.PERCENT);

        var withTies = ParseKeywords(Keyword.WITH, Keyword.TIES);

        return new Top(
            WithTies: withTies,
            Percent: percent,
            Quantity: quantity);
    }

    /// <summary>Parse a LIMIT clause</summary>
    public Expr? ParseLimit()
    {
        if (ParseKeyword(Keyword.ALL))
        {
            return null;
        }
        else
        {
            return ParseExpr();
        }
    }

    /// <summary>Parse an OFFSET clause</summary>
    public Offset ParseOffset()
    {
        var value = ParseExpr();
        OffsetRows rows;
        if (ParseKeyword(Keyword.ROW))
        {
            rows = new OffsetRows.Row();
        }
        else if (ParseKeyword(Keyword.ROWS))
        {
            rows = new OffsetRows.Rows();
        }
        else
        {
            rows = new OffsetRows.None();
        }
        return new Offset(Value: value, Rows: rows);
    }

    /// <summary>Parse a FETCH clause</summary>
    public Fetch ParseFetch()
    {
        _ = ParseOneOfKeywords(Keyword.FIRST, Keyword.NEXT);

        Expr? quantity;
        bool percent;
        if (ParseOneOfKeywords(Keyword.ROW, Keyword.ROWS) is not null)
        {
            quantity = null;
            percent = false;
        }
        else
        {
            // Rust: `let quantity`/`let percent` internos, com os mesmos nomes dos externos.
            quantity = new Expr.Value(ParseValue());
            percent = ParseKeyword(Keyword.PERCENT);
            _ = ParseOneOfKeywords(Keyword.ROW, Keyword.ROWS);
        }

        bool withTies;
        if (ParseKeyword(Keyword.ONLY))
        {
            withTies = false;
        }
        else
        {
            withTies = ParseKeywords(Keyword.WITH, Keyword.TIES);
        }

        return new Fetch(
            WithTies: withTies,
            Percent: percent,
            Quantity: quantity);
    }

    /// <summary>Parse a FOR UPDATE/FOR SHARE clause</summary>
    public LockClause ParseLock()
    {
        LockType lockType;
        switch (ExpectOneOfKeywords(Keyword.UPDATE, Keyword.SHARE))
        {
            case Keyword.UPDATE:
                lockType = new LockType.Update();
                break;
            case Keyword.SHARE:
                lockType = new LockType.Share();
                break;
            case var unexpectedKeyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: expected any of {{UPDATE, SHARE}}, got {unexpectedKeyword.ToString()}"));
        }
        ObjectName? of;
        if (ParseKeyword(Keyword.OF))
        {
            of = ParseObjectName(false);
        }
        else
        {
            of = null;
        }
        NonBlock? nonblock;
        if (ParseKeyword(Keyword.NOWAIT))
        {
            nonblock = new NonBlock.Nowait();
        }
        else if (ParseKeywords(Keyword.SKIP, Keyword.LOCKED))
        {
            nonblock = new NonBlock.SkipLocked();
        }
        else
        {
            nonblock = null;
        }
        return new LockClause(
            LockType: lockType,
            Of: of,
            Nonblock: nonblock);
    }

    /// <summary>Parse a PostgreSQL <c>LOCK</c> statement.</summary>
    public Lock ParseLockStatement()
    {
        ExpectKeyword(Keyword.LOCK);

        if (PeekKeyword(Keyword.TABLES))
        {
            return ExpectedRef<Lock>("TABLE or a table name", PeekTokenRef());
        }

        _ = ParseKeyword(Keyword.TABLE);
        var tables = ParseCommaSeparated(p => p.ParseLockTableTarget());
        LockTableMode? lockMode;
        if (ParseKeyword(Keyword.IN))
        {
            // Rust: `let lock_mode` interno (caso 72).
            var parsedLockMode = ParseLockTableMode();
            ExpectKeyword(Keyword.MODE);
            lockMode = parsedLockMode;
        }
        else
        {
            lockMode = null;
        }
        var nowait = ParseKeyword(Keyword.NOWAIT);

        return new Lock(
            Tables: tables,
            LockMode: lockMode,
            Nowait: nowait);
    }

    internal LockTableTarget ParseLockTableTarget()
    {
        var only = ParseKeyword(Keyword.ONLY);
        var name = ParseObjectName(false);
        var hasAsterisk = ConsumeToken(new Token.Mul());

        return new LockTableTarget(
            Name: name,
            Only: only,
            HasAsterisk: hasAsterisk);
    }

    internal LockTableMode ParseLockTableMode()
    {
        if (ParseKeywords(Keyword.ACCESS, Keyword.SHARE))
        {
            return new LockTableMode.AccessShare();
        }
        else if (ParseKeywords(Keyword.ACCESS, Keyword.EXCLUSIVE))
        {
            return new LockTableMode.AccessExclusive();
        }
        else if (ParseKeywords(Keyword.ROW, Keyword.SHARE))
        {
            return new LockTableMode.RowShare();
        }
        else if (ParseKeywords(Keyword.ROW, Keyword.EXCLUSIVE))
        {
            return new LockTableMode.RowExclusive();
        }
        else if (ParseKeywords(Keyword.SHARE, Keyword.UPDATE, Keyword.EXCLUSIVE))
        {
            return new LockTableMode.ShareUpdateExclusive();
        }
        else if (ParseKeywords(Keyword.SHARE, Keyword.ROW, Keyword.EXCLUSIVE))
        {
            return new LockTableMode.ShareRowExclusive();
        }
        else if (ParseKeyword(Keyword.SHARE))
        {
            return new LockTableMode.Share();
        }
        else if (ParseKeyword(Keyword.EXCLUSIVE))
        {
            return new LockTableMode.Exclusive();
        }
        else
        {
            return ExpectedRef<LockTableMode>("a PostgreSQL LOCK TABLE mode", PeekTokenRef());
        }
    }

    /// <summary>Parse a VALUES clause</summary>
    public Values ParseValues(bool allowEmpty, bool valueKeyword)
    {
        var explicitRow = false;

        // A closure altera `explicit_row` (variável capturada), como no Rust.
        var rows = ParseCommaSeparated(parser =>
        {
            if (parser.ParseKeyword(Keyword.ROW))
            {
                explicitRow = true;
            }
            return new Parens<Sequence<Expr>>(
                OpeningToken: AttachedToken.From(parser.ExpectToken(new Token.LParen())),
                Content: allowEmpty && parser.PeekTokenRef().Token is Token.RParen
                    ? Sequence<Expr>.Empty
                    : parser.ParseCommaSeparated(p => p.ParseExpr()),
                ClosingToken: AttachedToken.From(parser.ExpectToken(new Token.RParen())));
        });
        return new Values(
            ExplicitRow: explicitRow,
            Rows: rows,
            ValueKeyword: valueKeyword);
    }

    /// <summary>Parse a 'START TRANSACTION' statement</summary>
    public Statement ParseStartTransaction()
    {
        ExpectKeywordIs(Keyword.TRANSACTION);
        return new Statement.StartTransaction(
            Modes: ParseTransactionModes(),
            Begin: false,
            Transaction: new BeginTransactionKind.Transaction(),
            Modifier: null,
            Statements: Sequence<Statement>.Empty,
            Exception: null,
            HasEndKeyword: false);
    }

    /// <summary>Parse a transaction modifier keyword that can follow a <c>BEGIN</c> statement.</summary>
    internal TransactionModifier? ParseTransactionModifier()
    {
        if (!_dialect.SupportsStartTransactionModifier)
        {
            return null;
        }
        else if (ParseKeyword(Keyword.DEFERRED))
        {
            return new TransactionModifier.Deferred();
        }
        else if (ParseKeyword(Keyword.IMMEDIATE))
        {
            return new TransactionModifier.Immediate();
        }
        else if (ParseKeyword(Keyword.EXCLUSIVE))
        {
            return new TransactionModifier.Exclusive();
        }
        else if (ParseKeyword(Keyword.TRY))
        {
            return new TransactionModifier.Try();
        }
        else if (ParseKeyword(Keyword.CATCH))
        {
            return new TransactionModifier.Catch();
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse a 'BEGIN' statement</summary>
    public Statement ParseBegin()
    {
        var modifier = ParseTransactionModifier();
        BeginTransactionKind? transaction;
        switch (ParseOneOfKeywords(Keyword.TRANSACTION, Keyword.WORK, Keyword.TRAN))
        {
            case Keyword.TRANSACTION:
                transaction = new BeginTransactionKind.Transaction();
                break;
            case Keyword.WORK:
                transaction = new BeginTransactionKind.Work();
                break;
            case Keyword.TRAN:
                transaction = new BeginTransactionKind.Tran();
                break;
            default:
                transaction = null;
                break;
        }
        return new Statement.StartTransaction(
            Modes: ParseTransactionModes(),
            Begin: true,
            Transaction: transaction,
            Modifier: modifier,
            Statements: Sequence<Statement>.Empty,
            Exception: null,
            HasEndKeyword: false);
    }

    /// <summary>Parse a 'BEGIN ... EXCEPTION ... END' block</summary>
    public Statement ParseBeginExceptionEnd()
    {
        var statements = ParseStatementList(Keyword.EXCEPTION, Keyword.END);

        Sequence<ExceptionWhen>? exception;
        if (ParseKeyword(Keyword.EXCEPTION))
        {
            var @when = new List<ExceptionWhen>();

            // We can have multiple `WHEN` arms so we consume all cases until `END`
            while (!PeekKeyword(Keyword.END))
            {
                ExpectKeyword(Keyword.WHEN);

                // Each `WHEN` case can have one or more conditions, e.g.
                // WHEN EXCEPTION_1 [OR EXCEPTION_2] THEN
                // So we parse identifiers until the `THEN` keyword.
                var idents = new List<Ident>();

                while (!ParseKeyword(Keyword.THEN))
                {
                    var ident = ParseIdentifier();
                    idents.Add(ident);

                    _ = MaybeParse(p => p.ExpectKeyword(Keyword.OR));
                }

                // Rust: `let statements` interno (caso 72).
                var whenStatements = ParseStatementList(Keyword.WHEN, Keyword.END);

                @when.Add(new ExceptionWhen(Idents: idents.ToSequence(), Statements: whenStatements));
            }

            exception = @when.ToSequence();
        }
        else
        {
            exception = null;
        }

        ExpectKeyword(Keyword.END);

        return new Statement.StartTransaction(
            Begin: true,
            Statements: statements,
            Exception: exception,
            HasEndKeyword: true,
            Transaction: null,
            Modifier: null,
            Modes: Sequence<TransactionMode>.Empty);
    }

    /// <summary>Parse an 'END' statement</summary>
    public Statement ParseEnd()
    {
        TransactionModifier? modifier;
        if (!_dialect.SupportsEndTransactionModifier)
        {
            modifier = null;
        }
        else if (ParseKeyword(Keyword.TRY))
        {
            modifier = new TransactionModifier.Try();
        }
        else if (ParseKeyword(Keyword.CATCH))
        {
            modifier = new TransactionModifier.Catch();
        }
        else
        {
            modifier = null;
        }
        return new Statement.Commit(
            Chain: ParseCommitRollbackChain(),
            End: true,
            Modifier: modifier);
    }

    /// <summary>Parse a list of transaction modes</summary>
    public Sequence<TransactionMode> ParseTransactionModes()
    {
        var modes = new List<TransactionMode>();
        var required = false;
        while (true)
        {
            TransactionMode mode;
            if (ParseKeywords(Keyword.ISOLATION, Keyword.LEVEL))
            {
                TransactionIsolationLevel isoLevel;
                if (ParseKeywords(Keyword.READ, Keyword.UNCOMMITTED))
                {
                    isoLevel = new TransactionIsolationLevel.ReadUncommitted();
                }
                else if (ParseKeywords(Keyword.READ, Keyword.COMMITTED))
                {
                    isoLevel = new TransactionIsolationLevel.ReadCommitted();
                }
                else if (ParseKeywords(Keyword.REPEATABLE, Keyword.READ))
                {
                    isoLevel = new TransactionIsolationLevel.RepeatableRead();
                }
                else if (ParseKeyword(Keyword.SERIALIZABLE))
                {
                    isoLevel = new TransactionIsolationLevel.Serializable();
                }
                else if (ParseKeyword(Keyword.SNAPSHOT))
                {
                    isoLevel = new TransactionIsolationLevel.Snapshot();
                }
                else
                {
                    isoLevel = ExpectedRef<TransactionIsolationLevel>("isolation level", PeekTokenRef());
                }
                mode = new TransactionMode.IsolationLevel(isoLevel);
            }
            else if (ParseKeywords(Keyword.READ, Keyword.ONLY))
            {
                mode = new TransactionMode.AccessMode(new TransactionAccessMode.ReadOnly());
            }
            else if (ParseKeywords(Keyword.READ, Keyword.WRITE))
            {
                mode = new TransactionMode.AccessMode(new TransactionAccessMode.ReadWrite());
            }
            else if (required)
            {
                mode = ExpectedRef<TransactionMode>("transaction mode", PeekTokenRef());
            }
            else
            {
                break;
            }
            modes.Add(mode);
            // ANSI requires a comma after each transaction mode, but
            // PostgreSQL, for historical reasons, does not. We follow
            // PostgreSQL in making the comma optional, since that is strictly
            // more general.
            required = ConsumeToken(new Token.Comma());
        }
        return modes.ToSequence();
    }

    /// <summary>Parse a 'COMMIT' statement</summary>
    public Statement ParseCommit() =>
        new Statement.Commit(
            Chain: ParseCommitRollbackChain(),
            End: false,
            Modifier: null);

    /// <summary>Parse a 'ROLLBACK' statement</summary>
    public Statement ParseRollback()
    {
        var chain = ParseCommitRollbackChain();
        var savepoint = ParseRollbackSavepoint();

        return new Statement.Rollback(Chain: chain, Savepoint: savepoint);
    }

    /// <summary>Parse an 'ABORT' statement</summary>
    /// <remarks>
    /// <code>
    /// ABORT [ TRANSACTION | WORK ] [ AND [ NO ] CHAIN ]
    /// </code>
    /// </remarks>
    public Statement ParseAbort()
    {
        var chain = ParseCommitRollbackChain();

        return new Statement.Rollback(
            Chain: chain,
            Savepoint: null);
    }

    /// <summary>Parse an optional <c>AND [NO] CHAIN</c> clause for <c>COMMIT</c> and <c>ROLLBACK</c> statements</summary>
    public bool ParseCommitRollbackChain()
    {
        _ = ParseOneOfKeywords(Keyword.TRANSACTION, Keyword.WORK, Keyword.TRAN);
        if (ParseKeyword(Keyword.AND))
        {
            var chain = !ParseKeyword(Keyword.NO);
            ExpectKeywordIs(Keyword.CHAIN);
            return chain;
        }
        else
        {
            return false;
        }
    }

    /// <summary>Parse an optional 'TO SAVEPOINT savepoint_name' clause for ROLLBACK statements</summary>
    public Ident? ParseRollbackSavepoint()
    {
        if (ParseKeyword(Keyword.TO))
        {
            _ = ParseKeyword(Keyword.SAVEPOINT);
            var savepoint = ParseIdentifier();

            return savepoint;
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse a 'RAISERROR' statement</summary>
    public Statement ParseRaiserror()
    {
        ExpectToken(new Token.LParen());
        var message = ParseExpr();
        ExpectToken(new Token.Comma());
        var severity = ParseExpr();
        ExpectToken(new Token.Comma());
        var state = ParseExpr();
        Sequence<Expr> arguments;
        if (ConsumeToken(new Token.Comma()))
        {
            arguments = ParseCommaSeparated(p => p.ParseExpr());
        }
        else
        {
            arguments = Sequence<Expr>.Empty;
        }
        ExpectToken(new Token.RParen());
        Sequence<RaisErrorOption> options;
        if (ParseKeyword(Keyword.WITH))
        {
            options = ParseCommaSeparated(p => p.ParseRaiserrorOption());
        }
        else
        {
            options = Sequence<RaisErrorOption>.Empty;
        }
        return new Statement.RaisError(
            Message: message,
            Severity: severity,
            State: state,
            Arguments: arguments,
            Options: options);
    }

    /// <summary>Parse a single <c>RAISERROR</c> option</summary>
    public RaisErrorOption ParseRaiserrorOption()
    {
        switch (ExpectOneOfKeywords(Keyword.LOG, Keyword.NOWAIT, Keyword.SETERROR))
        {
            case Keyword.LOG:
                return new RaisErrorOption.Log();
            case Keyword.NOWAIT:
                return new RaisErrorOption.NoWait();
            case Keyword.SETERROR:
                return new RaisErrorOption.SetError();
            default:
                return ExpectedRef<RaisErrorOption>(
                    "LOG, NOWAIT OR SETERROR raiserror option",
                    PeekTokenRef());
        }
    }

    /// <summary>Parse a MSSQL <c>THROW</c> statement.</summary>
    /// <remarks>See <see cref="Statement.Throw"/></remarks>
    public ThrowStatement ParseThrow()
    {
        ExpectKeywordIs(Keyword.THROW);

        var errorNumber = MaybeParse(p => p.ParseExpr());
        Expr? message;
        Expr? state;
        if (errorNumber is not null)
        {
            ExpectToken(new Token.Comma());
            // Rust: `let message`/`let state` internos, com os mesmos nomes dos externos.
            message = ParseExpr();
            ExpectToken(new Token.Comma());
            state = ParseExpr();
        }
        else
        {
            message = null;
            state = null;
        }

        return new ThrowStatement(
            ErrorNumber: errorNumber,
            Message: message,
            State: state);
    }

    /// <summary>Parse a SQL <c>DEALLOCATE</c> statement</summary>
    public Statement ParseDeallocate()
    {
        var prepare = ParseKeyword(Keyword.PREPARE);
        var name = ParseIdentifier();
        return new Statement.Deallocate(Name: name, Prepare: prepare);
    }

    /// <summary>Parse a SQL <c>EXECUTE</c> statement</summary>
    public Statement ParseExecute()
    {
        var immediate =
            _dialect.SupportsExecuteImmediate && ParseKeyword(Keyword.IMMEDIATE);

        // When `EXEC` is immediately followed by `(`, the content is a dynamic-SQL
        // expression — e.g. `EXEC (@sql)`, `EXEC ('SELECT ...')`, or
        // `EXEC ('SELECT ... FROM ' + @tbl + ' WHERE ...')`.
        // Skip name parsing; the expression ends up in `parameters` via the
        // `has_parentheses` path below, consistent with `EXECUTE IMMEDIATE <expr>`.
        ObjectName? name;
        if (immediate || PeekTokenRef().Token is Token.LParen)
        {
            name = null;
        }
        else
        {
            name = ParseObjectName(false);
        }

        var hasParentheses = ConsumeToken(new Token.LParen());

        var endKws = new[] { Keyword.USING, Keyword.OUTPUT, Keyword.DEFAULT };
        Token endToken;
        switch ((hasParentheses, PeekToken().Token))
        {
            case (true, _):
                endToken = new Token.RParen();
                break;
            case (false, Token.EOF):
                endToken = new Token.EOF();
                break;
            case (false, Token.Word { Value: var w }) when endKws.Contains(w.Keyword):
                endToken = new Token.Word(w);
                break;
            case (false, _):
                endToken = new Token.SemiColon();
                break;
        }

        var parameters = ParseCommaSeparated0(p => p.ParseExpr(), endToken);

        if (hasParentheses)
        {
            ExpectToken(new Token.RParen());
        }

        Sequence<Ident> @into;
        if (ParseKeyword(Keyword.INTO))
        {
            @into = ParseCommaSeparated(p => p.ParseIdentifier());
        }
        else
        {
            @into = Sequence<Ident>.Empty;
        }

        Sequence<ExprWithAlias> @using;
        if (ParseKeyword(Keyword.USING))
        {
            @using = ParseCommaSeparated(p => p.ParseExprWithAlias());
        }
        else
        {
            @using = Sequence<ExprWithAlias>.Empty;
        }

        var output = ParseKeyword(Keyword.OUTPUT);

        var @default = ParseKeyword(Keyword.DEFAULT);

        return new Statement.Execute(
            Immediate: immediate,
            Name: name,
            Parameters: parameters,
            HasParentheses: hasParentheses,
            Into: @into,
            Using: @using,
            Output: output,
            Default: @default);
    }

    /// <summary>Parse a SQL <c>PREPARE</c> statement</summary>
    public Statement ParsePrepare()
    {
        var name = ParseIdentifier();

        var dataTypes = Sequence<DataType>.Empty;
        if (ConsumeToken(new Token.LParen()))
        {
            dataTypes = ParseCommaSeparated(p => p.ParseDataType());
            ExpectToken(new Token.RParen());
        }

        ExpectKeywordIs(Keyword.AS);
        var statement = ParseStatement();
        return new Statement.Prepare(
            Name: name,
            DataTypes: dataTypes,
            Statement: statement);
    }

    /// <summary>Parse a SQL <c>UNLOAD</c> statement</summary>
    public Statement ParseUnload()
    {
        ExpectKeyword(Keyword.UNLOAD);
        ExpectToken(new Token.LParen());
        Query? query;
        string? queryText;
        if (PeekTokenRef().Token is Token.SingleQuotedString)
        {
            query = null;
            queryText = ParseLiteralString();
        }
        else
        {
            query = ParseQuery();
            queryText = null;
        }
        ExpectToken(new Token.RParen());

        ExpectKeywordIs(Keyword.TO);
        var to = ParseIdentifier();
        IamRoleKind? auth;
        if (ParseKeyword(Keyword.IAM_ROLE))
        {
            auth = ParseIamRoleKind();
        }
        else
        {
            auth = null;
        }
        var @with = ParseOptions(Keyword.WITH);
        var options = new List<CopyLegacyOption>();
        while (MaybeParse(parser => parser.ParseCopyLegacyOption()) is { } opt)
        {
            options.Add(opt);
        }
        return new Statement.Unload(
            Query: query,
            QueryText: queryText,
            To: to,
            Auth: auth,
            With: @with,
            Options: options.ToSequence());
    }

    internal SelectInto ParseSelectInto()
    {
        var temporary = ParseOneOfKeywords(Keyword.TEMP, Keyword.TEMPORARY) is not null;
        var unlogged = ParseKeyword(Keyword.UNLOGGED);
        var table = ParseKeyword(Keyword.TABLE);
        var targets = ParseCommaSeparated(p => p.ParseExpr());

        return new SelectInto(
            Temporary: temporary,
            Unlogged: unlogged,
            Table: table,
            Targets: targets);
    }

    internal ValueWithSpan ParsePragmaValue()
    {
        var v = ParseValue();
        switch (v.Value)
        {
            case Value.SingleQuotedString:
                return v;
            case Value.DoubleQuotedString:
                return v;
            case Value.Number:
                return v;
            case Value.Placeholder:
                return v;
            default:
                PrevToken();
                return ExpectedRef<ValueWithSpan>("number or string or ? placeholder", PeekTokenRef());
        }
    }

    /// <summary>PRAGMA [schema-name '.'] pragma-name [('=' pragma-value) | '(' pragma-value ')']</summary>
    public Statement ParsePragma()
    {
        var name = ParseObjectName(false);
        if (ConsumeToken(new Token.LParen()))
        {
            var value = ParsePragmaValue();
            ExpectToken(new Token.RParen());
            return new Statement.Pragma(
                Name: name,
                Value: value,
                IsEq: false);
        }
        else if (ConsumeToken(new Token.Eq()))
        {
            return new Statement.Pragma(
                Name: name,
                Value: ParsePragmaValue(),
                IsEq: true);
        }
        else
        {
            return new Statement.Pragma(
                Name: name,
                Value: null,
                IsEq: false);
        }
    }

    /// <summary><c>INSTALL [extension_name]</c></summary>
    public Statement ParseInstall()
    {
        var extensionName = ParseIdentifier();

        return new Statement.Install(ExtensionName: extensionName);
    }

    /// <summary>Parse a SQL LOAD statement</summary>
    public Statement ParseLoad()
    {
        if (_dialect.SupportsLoadExtension)
        {
            var extensionName = ParseIdentifier();
            return new Statement.Load(ExtensionName: extensionName);
        }
        else if (ParseKeyword(Keyword.DATA) && _dialect.SupportsLoadData)
        {
            var local = ParseOneOfKeywords(Keyword.LOCAL) is not null;
            ExpectKeywordIs(Keyword.INPATH);
            var inpath = ParseLiteralString();
            var overwrite = ParseOneOfKeywords(Keyword.OVERWRITE) is not null;
            ExpectKeywordIs(Keyword.INTO);
            ExpectKeywordIs(Keyword.TABLE);
            var tableName = ParseObjectName(false);
            var partitioned = ParseInsertPartition();
            var tableFormat = ParseLoadDataTableFormat();
            return new Statement.LoadData(
                Local: local,
                Inpath: inpath,
                Overwrite: overwrite,
                TableName: tableName,
                Partitioned: partitioned,
                TableFormat: tableFormat);
        }
        else
        {
            return ExpectedRef<Statement>(
                "`DATA` or an extension name after `LOAD`",
                PeekTokenRef());
        }
    }

    /// <summary>
    /// ClickHouse:
    /// <code>
    /// OPTIMIZE TABLE [db.]name [ON CLUSTER cluster] [PARTITION partition | PARTITION ID 'partition_id'] [FINAL] [DEDUPLICATE [BY expression]]
    /// </code>
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize">ClickHouse</see>
    /// </summary>
    /// <remarks>
    /// Databricks:
    /// <code>
    /// OPTIMIZE table_name [WHERE predicate] [ZORDER BY (col_name1 [, ...])]
    /// </code>
    /// <see href="https://docs.databricks.com/en/sql/language-manual/delta-optimize.html">Databricks</see>
    /// </remarks>
    public Statement ParseOptimizeTable()
    {
        var hasTableKeyword = ParseKeyword(Keyword.TABLE);

        var name = ParseObjectName(false);

        // ClickHouse-specific options
        var onCluster = ParseOptionalOnCluster();

        Partition? partition;
        if (ParseKeyword(Keyword.PARTITION))
        {
            if (ParseKeyword(Keyword.ID))
            {
                partition = new Partition.Identifier(ParseIdentifier());
            }
            else
            {
                partition = new Partition.Expr(ParseExpr());
            }
        }
        else
        {
            partition = null;
        }

        var includeFinal = ParseKeyword(Keyword.FINAL);

        Deduplicate? deduplicate;
        if (ParseKeyword(Keyword.DEDUPLICATE))
        {
            if (ParseKeyword(Keyword.BY))
            {
                deduplicate = new Deduplicate.ByExpression(ParseExpr());
            }
            else
            {
                deduplicate = new Deduplicate.All();
            }
        }
        else
        {
            deduplicate = null;
        }

        // Databricks-specific options
        Expr? predicate;
        if (ParseKeyword(Keyword.WHERE))
        {
            predicate = ParseExpr();
        }
        else
        {
            predicate = null;
        }

        Sequence<Expr>? zorder;
        if (ParseKeywords(Keyword.ZORDER, Keyword.BY))
        {
            ExpectToken(new Token.LParen());
            var columns = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            zorder = columns;
        }
        else
        {
            zorder = null;
        }

        return new Statement.OptimizeTable(
            Name: name,
            HasTableKeyword: hasTableKeyword,
            OnCluster: onCluster,
            Partition: partition,
            IncludeFinal: includeFinal,
            Deduplicate: deduplicate,
            Predicate: predicate,
            Zorder: zorder);
    }
}
