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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 611–1526:
// parse_statement … parse_rename).
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>
    /// Parse a single top-level statement (such as SELECT, INSERT, CREATE, etc.),
    /// stopping before the statement separator, if any.
    /// </summary>
    public Statement ParseStatement()
    {
        using var _guard = _recursionCounter.TryDecrease();

        // allow the dialect to override statement parsing
        if (_dialect.ParseStatement(this) is { } statement)
        {
            return statement;
        }

        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.KILL:
                        return ParseKill();
                    case Keyword.FLUSH:
                        return ParseFlush();
                    case Keyword.DESC:
                        return ParseExplain(new DescribeAlias.Desc());
                    case Keyword.DESCRIBE:
                        return ParseExplain(new DescribeAlias.Describe());
                    case Keyword.EXPLAIN:
                        return ParseExplain(new DescribeAlias.Explain());
                    case Keyword.ANALYZE:
                        return Statement.From(ParseAnalyze());
                    case Keyword.CASE:
                        PrevToken();
                        return Statement.From(ParseCaseStmt());
                    case Keyword.IF:
                        PrevToken();
                        return Statement.From(ParseIfStmt());
                    case Keyword.WHILE:
                        PrevToken();
                        return Statement.From(ParseWhile());
                    case Keyword.RAISE:
                        PrevToken();
                        return Statement.From(ParseRaiseStmt());
                    case Keyword.SELECT or Keyword.WITH or Keyword.VALUES or Keyword.FROM:
                        PrevToken();
                        return Statement.From(ParseQuery());
                    case Keyword.TRUNCATE:
                        return Statement.From(ParseTruncate());
                    case Keyword.ATTACH:
                        if (_dialect is DuckDbDialect)
                        {
                            return ParseAttachDuckdbDatabase();
                        }
                        else
                        {
                            return ParseAttachDatabase();
                        }
                    case Keyword.DETACH when _dialect.SupportsDetach:
                        return ParseDetachDuckdbDatabase();
                    case Keyword.MSCK:
                        return Statement.From(ParseMsck());
                    case Keyword.CREATE:
                        return ParseCreate();
                    case Keyword.CACHE:
                        return ParseCacheTable();
                    case Keyword.DROP:
                        return ParseDrop();
                    case Keyword.DISCARD:
                        return ParseDiscard();
                    case Keyword.DECLARE:
                        return ParseDeclare();
                    case Keyword.FETCH:
                        return ParseFetchStatement();
                    case Keyword.DELETE:
                        return ParseDelete(nextToken);
                    case Keyword.INSERT:
                        return ParseInsert(nextToken);
                    case Keyword.REPLACE:
                        return ParseReplace(nextToken);
                    case Keyword.UNCACHE:
                        return ParseUncacheTable();
                    case Keyword.UPDATE:
                        return ParseUpdate(nextToken);
                    case Keyword.ALTER:
                        return ParseAlter();
                    case Keyword.CALL:
                        return ParseCall();
                    case Keyword.COPY:
                        return ParseCopy();
                    case Keyword.OPEN:
                        PrevToken();
                        return ParseOpen();
                    case Keyword.CLOSE:
                        return ParseClose();
                    case Keyword.SET:
                        return ParseSet();
                    case Keyword.SHOW:
                        return ParseShow();
                    case Keyword.USE:
                        return ParseUse();
                    case Keyword.GRANT:
                        return Statement.From(ParseGrant());
                    case Keyword.DENY:
                        PrevToken();
                        return ParseDeny();
                    case Keyword.REVOKE:
                        return Statement.From(ParseRevoke());
                    case Keyword.START:
                        return ParseStartTransaction();
                    case Keyword.BEGIN:
                        return ParseBegin();
                    case Keyword.END:
                        return ParseEnd();
                    case Keyword.SAVEPOINT:
                        return ParseSavepoint();
                    case Keyword.RELEASE:
                        return ParseRelease();
                    case Keyword.COMMIT:
                        return ParseCommit();
                    case Keyword.RAISERROR:
                        return ParseRaiserror();
                    case Keyword.THROW:
                        PrevToken();
                        return Statement.From(ParseThrow());
                    case Keyword.ROLLBACK:
                        return ParseRollback();
                    case Keyword.ABORT:
                        return ParseAbort();
                    case Keyword.ASSERT:
                        return ParseAssert();
                    // `PREPARE`, `EXECUTE` and `DEALLOCATE` are Postgres-specific
                    // syntaxes. They are used for Postgres prepared statement.
                    case Keyword.DEALLOCATE:
                        return ParseDeallocate();
                    case Keyword.EXECUTE or Keyword.EXEC:
                        return ParseExecute();
                    case Keyword.PREPARE:
                        return ParsePrepare();
                    case Keyword.MERGE:
                        return Statement.From(ParseMerge(nextToken));
                    // `LISTEN`, `UNLISTEN` and `NOTIFY` are Postgres-specific
                    // syntaxes. They are used for Postgres statement.
                    case Keyword.LISTEN when _dialect.SupportsListenNotify:
                        return ParseListen();
                    case Keyword.UNLISTEN when _dialect.SupportsListenNotify:
                        return ParseUnlisten();
                    case Keyword.NOTIFY when _dialect.SupportsListenNotify:
                        return ParseNotify();
                    // `PRAGMA` is sqlite specific https://www.sqlite.org/pragma.html
                    case Keyword.PRAGMA:
                        return ParsePragma();
                    case Keyword.UNLOAD:
                        PrevToken();
                        return ParseUnload();
                    case Keyword.RENAME:
                        return ParseRename();
                    // `INSTALL` is duckdb specific https://duckdb.org/docs/extensions/overview
                    case Keyword.INSTALL when _dialect.SupportsInstall:
                        return ParseInstall();
                    case Keyword.LOAD:
                        return ParseLoad();
                    case Keyword.LOCK:
                        PrevToken();
                        return Statement.From(ParseLockStatement());
                    case Keyword.OPTIMIZE when _dialect.SupportsOptimizeTable:
                        return ParseOptimizeTable();
                    // `COMMENT` is snowflake specific https://docs.snowflake.com/en/sql-reference/sql/comment
                    case Keyword.COMMENT when _dialect.SupportsCommentOn:
                        return ParseComment();
                    case Keyword.PRINT:
                        return ParsePrint();
                    // `WAITFOR` is MSSQL specific https://learn.microsoft.com/en-us/sql/t-sql/language-elements/waitfor-transact-sql
                    case Keyword.WAITFOR:
                        return ParseWaitfor();
                    case Keyword.RETURN:
                        return ParseReturn();
                    case Keyword.EXPORT:
                        PrevToken();
                        return ParseExportData();
                    case Keyword.VACUUM:
                        PrevToken();
                        return ParseVacuum();
                    case Keyword.RESET:
                        return Statement.From(ParseReset());
                    default:
                        return Expected<Statement>("an SQL statement", nextToken);
                }
            case Token.LParen:
                PrevToken();
                return Statement.From(ParseQuery());
            default:
                return Expected<Statement>("an SQL statement", nextToken);
        }
    }

    /// <summary>Parse a <c>CASE</c> statement.</summary>
    /// <remarks>See <c>Statement::Case</c></remarks>
    public CaseStatement ParseCaseStmt()
    {
        var caseToken = ExpectKeyword(Keyword.CASE);

        Expr? matchExpr;
        if (PeekKeyword(Keyword.WHEN))
        {
            matchExpr = null;
        }
        else
        {
            matchExpr = ParseExpr();
        }

        ExpectKeywordIs(Keyword.WHEN);
        var whenBlocks = ParseKeywordSeparated(Keyword.WHEN, parser =>
            parser.ParseConditionalStatementBlock(Keyword.WHEN, Keyword.ELSE, Keyword.END));

        ConditionalStatementBlock? elseBlock;
        if (ParseKeyword(Keyword.ELSE))
        {
            elseBlock = ParseConditionalStatementBlock(Keyword.END);
        }
        else
        {
            elseBlock = null;
        }

        var endCaseToken = ExpectKeyword(Keyword.END);
        if (PeekKeyword(Keyword.CASE))
        {
            endCaseToken = ExpectKeyword(Keyword.CASE);
        }

        return new CaseStatement(
            CaseToken: new AttachedToken(caseToken),
            MatchExpr: matchExpr,
            WhenBlocks: whenBlocks,
            ElseBlock: elseBlock,
            EndCaseToken: new AttachedToken(endCaseToken));
    }

    /// <summary>Parse an <c>IF</c> statement.</summary>
    /// <remarks>See <c>Statement::If</c></remarks>
    public IfStatement ParseIfStmt()
    {
        ExpectKeywordIs(Keyword.IF);
        var ifBlock = ParseConditionalStatementBlock(
            Keyword.ELSE,
            Keyword.ELSEIF,
            Keyword.END);

        Sequence<ConditionalStatementBlock> elseifBlocks;
        if (ParseKeyword(Keyword.ELSEIF))
        {
            elseifBlocks = ParseKeywordSeparated(Keyword.ELSEIF, parser =>
                parser.ParseConditionalStatementBlock(
                    Keyword.ELSEIF,
                    Keyword.ELSE,
                    Keyword.END));
        }
        else
        {
            elseifBlocks = Sequence<ConditionalStatementBlock>.Empty;
        }

        ConditionalStatementBlock? elseBlock;
        if (ParseKeyword(Keyword.ELSE))
        {
            elseBlock = ParseConditionalStatementBlock(Keyword.END);
        }
        else
        {
            elseBlock = null;
        }

        ExpectKeywordIs(Keyword.END);
        var endToken = ExpectKeyword(Keyword.IF);

        return new IfStatement(
            IfBlock: ifBlock,
            ElseifBlocks: elseifBlocks,
            ElseBlock: elseBlock,
            EndToken: new AttachedToken(endToken));
    }

    /// <summary>Parse a <c>WHILE</c> statement.</summary>
    /// <remarks>See <c>Statement::While</c></remarks>
    internal WhileStatement ParseWhile()
    {
        ExpectKeywordIs(Keyword.WHILE);
        var whileBlock = ParseConditionalStatementBlock(Keyword.END);

        return new WhileStatement(WhileBlock: whileBlock);
    }

    /// <summary>
    /// Parses an expression and associated list of statements
    /// belonging to a conditional statement like <c>IF</c> or <c>WHEN</c> or <c>WHILE</c>.
    /// </summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// IF condition THEN statement1; statement2;
    /// </code>
    /// </remarks>
    internal ConditionalStatementBlock ParseConditionalStatementBlock(params Keyword[] terminalKeywords)
    {
        var startToken = GetCurrentToken(); // self.expect_keyword(keyword)?;
        AttachedToken? thenToken = null;

        Expr? condition;
        switch (startToken.Token)
        {
            case Token.Word { Value: var w } when w.Keyword == Keyword.ELSE:
                condition = null;
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.WHILE:
            {
                var expr = ParseExpr();
                condition = expr;
                break;
            }
            default:
            {
                var expr = ParseExpr();
                thenToken = new AttachedToken(ExpectKeyword(Keyword.THEN));
                condition = expr;
                break;
            }
        }

        var conditionalStatements = ParseConditionalStatements(terminalKeywords);

        return new ConditionalStatementBlock(
            StartToken: new AttachedToken(startToken),
            Condition: condition,
            ThenToken: thenToken,
            ConditionalStatements: conditionalStatements);
    }

    /// <summary>
    /// Parse a BEGIN/END block or a sequence of statements
    /// This could be inside of a conditional (IF, CASE, WHILE etc.) or an object body defined optionally BEGIN/END and one or more statements.
    /// </summary>
    internal ConditionalStatements ParseConditionalStatements(params Keyword[] terminalKeywords)
    {
        ConditionalStatements conditionalStatements;
        if (PeekKeyword(Keyword.BEGIN))
        {
            var beginToken = ExpectKeyword(Keyword.BEGIN);
            var statements = ParseStatementList(terminalKeywords);
            var endToken = ExpectKeyword(Keyword.END);

            conditionalStatements = new ConditionalStatements.BeginEnd(new BeginEndStatements(
                BeginToken: new AttachedToken(beginToken),
                Statements: statements,
                EndToken: new AttachedToken(endToken)));
        }
        else
        {
            conditionalStatements = new ConditionalStatements.Sequence(
                Statements: ParseStatementList(terminalKeywords));
        }
        return conditionalStatements;
    }

    /// <summary>Parse a <c>RAISE</c> statement.</summary>
    /// <remarks>See <c>Statement::Raise</c></remarks>
    public RaiseStatement ParseRaiseStmt()
    {
        ExpectKeywordIs(Keyword.RAISE);

        RaiseStatementValue? value;
        if (ParseKeywords(Keyword.USING, Keyword.MESSAGE))
        {
            ExpectToken(new Token.Eq());
            value = new RaiseStatementValue.UsingMessage(ParseExpr());
        }
        else
        {
            value = MaybeParse<RaiseStatementValue>(parser => new RaiseStatementValue.Expr(parser.ParseExpr()));
        }

        return new RaiseStatement(Value: value);
    }

    /// <summary>Parse a COMMENT statement.</summary>
    /// <remarks>See <c>Statement::Comment</c></remarks>
    public Statement ParseComment()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);

        ExpectKeywordIs(Keyword.ON);
        var token = NextToken();

        CommentObject objectType;
        ObjectName objectName;
        switch (token.Token)
        {
            case Token.Word { Value: var w } when w.Keyword == Keyword.COLLATION:
                (objectType, objectName) = (new CommentObject.Collation(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.COLUMN:
                (objectType, objectName) = (new CommentObject.Column(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.DATABASE:
                (objectType, objectName) = (new CommentObject.Database(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.DOMAIN:
                (objectType, objectName) = (new CommentObject.Domain(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.EXTENSION:
                (objectType, objectName) = (new CommentObject.Extension(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.FUNCTION:
                (objectType, objectName) = (new CommentObject.Function(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.INDEX:
                (objectType, objectName) = (new CommentObject.Index(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.MATERIALIZED:
                ExpectKeywordIs(Keyword.VIEW);
                (objectType, objectName) = (
                    new CommentObject.MaterializedView(),
                    ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.PROCEDURE:
                (objectType, objectName) = (new CommentObject.Procedure(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.ROLE:
                (objectType, objectName) = (new CommentObject.Role(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.SCHEMA:
                (objectType, objectName) = (new CommentObject.Schema(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.SEQUENCE:
                (objectType, objectName) = (new CommentObject.Sequence(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.TABLE:
                (objectType, objectName) = (new CommentObject.Table(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.TYPE:
                (objectType, objectName) = (new CommentObject.Type(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.USER:
                (objectType, objectName) = (new CommentObject.User(), ParseObjectName(false));
                break;
            case Token.Word { Value: var w } when w.Keyword == Keyword.VIEW:
                (objectType, objectName) = (new CommentObject.View(), ParseObjectName(false));
                break;
            default:
                return Expected<Statement>("comment object_type", token);
        }

        ExpectKeywordIs(Keyword.IS);
        string? comment;
        if (ParseKeyword(Keyword.NULL))
        {
            comment = null;
        }
        else
        {
            comment = ParseLiteralString();
        }
        return new Statement.Comment(
            ObjectType: objectType,
            ObjectName: objectName,
            CommentValue: comment,
            IfExists: ifExists);
    }

    /// <summary>Parse <c>FLUSH</c> statement.</summary>
    public Statement ParseFlush()
    {
        string? channel = null;
        var tables = Sequence<ObjectName>.Empty;
        var readLock = false;
        var export = false;

        if (!(_dialect is MySqlDialect or GenericDialect))
        {
            throw ParserErr(
                "Unsupported statement FLUSH",
                PeekTokenRef().Span.Start);
        }

        FlushLocation? location;
        if (ParseKeyword(Keyword.NO_WRITE_TO_BINLOG))
        {
            location = new FlushLocation.NoWriteToBinlog();
        }
        else if (ParseKeyword(Keyword.LOCAL))
        {
            location = new FlushLocation.Local();
        }
        else
        {
            location = null;
        }

        FlushType objectType;
        if (ParseKeywords(Keyword.BINARY, Keyword.LOGS))
        {
            objectType = new FlushType.BinaryLogs();
        }
        else if (ParseKeywords(Keyword.ENGINE, Keyword.LOGS))
        {
            objectType = new FlushType.EngineLogs();
        }
        else if (ParseKeywords(Keyword.ERROR, Keyword.LOGS))
        {
            objectType = new FlushType.ErrorLogs();
        }
        else if (ParseKeywords(Keyword.GENERAL, Keyword.LOGS))
        {
            objectType = new FlushType.GeneralLogs();
        }
        else if (ParseKeywords(Keyword.HOSTS))
        {
            objectType = new FlushType.Hosts();
        }
        else if (ParseKeyword(Keyword.PRIVILEGES))
        {
            objectType = new FlushType.Privileges();
        }
        else if (ParseKeyword(Keyword.OPTIMIZER_COSTS))
        {
            objectType = new FlushType.OptimizerCosts();
        }
        else if (ParseKeywords(Keyword.RELAY, Keyword.LOGS))
        {
            if (ParseKeywords(Keyword.FOR, Keyword.CHANNEL))
            {
                channel = ParseObjectName(false).ToString();
            }
            objectType = new FlushType.RelayLogs();
        }
        else if (ParseKeywords(Keyword.SLOW, Keyword.LOGS))
        {
            objectType = new FlushType.SlowLogs();
        }
        else if (ParseKeyword(Keyword.STATUS))
        {
            objectType = new FlushType.Status();
        }
        else if (ParseKeyword(Keyword.USER_RESOURCES))
        {
            objectType = new FlushType.UserResources();
        }
        else if (ParseKeywords(Keyword.LOGS))
        {
            objectType = new FlushType.Logs();
        }
        else if (ParseKeywords(Keyword.TABLES))
        {
            while (true)
            {
                var nextToken = NextToken();
                var stop = false;
                switch (nextToken.Token)
                {
                    case Token.Word { Value: var w }:
                        switch (w.Keyword)
                        {
                            case Keyword.WITH:
                                readLock = ParseKeywords(Keyword.READ, Keyword.LOCK);
                                break;
                            case Keyword.FOR:
                                export = ParseKeyword(Keyword.EXPORT);
                                break;
                            case Keyword.NoKeyword:
                                PrevToken();
                                tables = ParseCommaSeparated(p => p.ParseObjectName(false));
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        stop = true;
                        break;
                }
                if (stop)
                {
                    break;
                }
            }

            objectType = new FlushType.Tables();
        }
        else
        {
            // Rust: string com `\` no fim da linha, que remove a quebra e a indentação seguinte
            // (por isso não há espaço depois de "OPTIMIZER_COSTS,").
            return ExpectedRef<Statement>(
                "BINARY LOGS, ENGINE LOGS, ERROR LOGS, GENERAL LOGS, HOSTS, LOGS, PRIVILEGES, OPTIMIZER_COSTS,"
                + "RELAY LOGS [FOR CHANNEL channel], SLOW LOGS, STATUS, USER_RESOURCES",
                PeekTokenRef());
        }

        return new Statement.Flush(
            ObjectType: objectType,
            Location: location,
            Channel: channel,
            ReadLock: readLock,
            Export: export,
            Tables: tables);
    }

    /// <summary>Parse <c>MSCK</c> statement.</summary>
    public Msck ParseMsck()
    {
        var repair = ParseKeyword(Keyword.REPAIR);
        ExpectKeywordIs(Keyword.TABLE);
        var tableName = ParseObjectName(false);
        // maybe_parse(..) devolve Option<Option<AddDropSync>> (caso 55) e `.unwrap_or_default()` junta os dois None.
        var partitionAction = MaybeParseValue(parser =>
            {
                AddDropSync? pa;
                switch (parser.ParseOneOfKeywords(
                            Keyword.ADD,
                            Keyword.DROP,
                            Keyword.SYNC))
                {
                    case Keyword.ADD:
                        pa = new AddDropSync.ADD();
                        break;
                    case Keyword.DROP:
                        pa = new AddDropSync.DROP();
                        break;
                    case Keyword.SYNC:
                        pa = new AddDropSync.SYNC();
                        break;
                    default:
                        pa = null;
                        break;
                }
                parser.ExpectKeywordIs(Keyword.PARTITIONS);
                return Option<AddDropSync?>.Some(pa);
            }) is { } maybePartitionAction
            ? maybePartitionAction.Value
            : null;
        return new Msck(
            Repair: repair,
            TableName: tableName,
            PartitionAction: partitionAction);
    }

    /// <summary>Parse <c>TRUNCATE</c> statement.</summary>
    public Truncate ParseTruncate()
    {
        var table = ParseKeyword(Keyword.TABLE);
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);

        var tableNames = ParseCommaSeparated(p =>
        {
            var only = p.ParseKeyword(Keyword.ONLY);
            var name = p.ParseObjectName(false);
            var hasAsterisk = p.ConsumeToken(new Token.Mul());
            return new TruncateTableTarget(
                Name: name,
                Only: only,
                HasAsterisk: hasAsterisk);
        });

        Sequence<Expr>? partitions = null;
        if (ParseKeyword(Keyword.PARTITION))
        {
            ExpectToken(new Token.LParen());
            partitions = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
        }

        TruncateIdentityOption? identity = null;
        CascadeOption? cascade = null;

        if (_dialect is PostgreSqlDialect or GenericDialect)
        {
            if (ParseKeywords(Keyword.RESTART, Keyword.IDENTITY))
            {
                identity = TruncateIdentityOption.Restart;
            }
            else if (ParseKeywords(Keyword.CONTINUE, Keyword.IDENTITY))
            {
                identity = TruncateIdentityOption.Continue;
            }
            else
            {
                identity = null;
            }

            cascade = ParseCascadeOption();
        }

        var onCluster = ParseOptionalOnCluster();

        return new Truncate(
            TableNames: tableNames,
            Partitions: partitions,
            Table: table,
            IfExists: ifExists,
            Identity: identity,
            Cascade: cascade,
            OnCluster: onCluster);
    }

    internal CascadeOption? ParseCascadeOption()
    {
        if (ParseKeyword(Keyword.CASCADE))
        {
            return new CascadeOption.Cascade();
        }
        else if (ParseKeyword(Keyword.RESTRICT))
        {
            return new CascadeOption.Restrict();
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse options for <c>ATTACH DUCKDB DATABASE</c> statement.</summary>
    public Sequence<AttachDuckDBDatabaseOption> ParseAttachDuckdbDatabaseOptions()
    {
        if (!ConsumeToken(new Token.LParen()))
        {
            return Sequence<AttachDuckDBDatabaseOption>.Empty;
        }

        var options = new List<AttachDuckDBDatabaseOption>();
        while (true)
        {
            if (ParseKeyword(Keyword.READ_ONLY))
            {
                bool? boolean;
                if (ParseKeyword(Keyword.TRUE))
                {
                    boolean = true;
                }
                else if (ParseKeyword(Keyword.FALSE))
                {
                    boolean = false;
                }
                else
                {
                    boolean = null;
                }
                options.Add(new AttachDuckDBDatabaseOption.ReadOnly(boolean));
            }
            else if (ParseKeyword(Keyword.TYPE))
            {
                var ident = ParseIdentifier();
                options.Add(new AttachDuckDBDatabaseOption.Type(ident));
            }
            else
            {
                return ExpectedRef<Sequence<AttachDuckDBDatabaseOption>>(
                    "expected one of: ), READ_ONLY, TYPE", PeekTokenRef());
            }

            if (ConsumeToken(new Token.RParen()))
            {
                return options.ToSequence();
            }
            else if (ConsumeToken(new Token.Comma()))
            {
                continue;
            }
            else
            {
                return ExpectedRef<Sequence<AttachDuckDBDatabaseOption>>(
                    "expected one of: ')', ','", PeekTokenRef());
            }
        }
    }

    /// <summary>Parse <c>ATTACH DUCKDB DATABASE</c> statement.</summary>
    public Statement ParseAttachDuckdbDatabase()
    {
        var database = ParseKeyword(Keyword.DATABASE);
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var databasePath = ParseIdentifier();
        Ident? databaseAlias;
        if (ParseKeyword(Keyword.AS))
        {
            databaseAlias = ParseIdentifier();
        }
        else
        {
            databaseAlias = null;
        }

        var attachOptions = ParseAttachDuckdbDatabaseOptions();
        return new Statement.AttachDuckDBDatabase(
            IfNotExists: ifNotExists,
            Database: database,
            DatabasePath: databasePath,
            DatabaseAlias: databaseAlias,
            AttachOptions: attachOptions);
    }

    /// <summary>Parse <c>DETACH DUCKDB DATABASE</c> statement.</summary>
    public Statement ParseDetachDuckdbDatabase()
    {
        var database = ParseKeyword(Keyword.DATABASE);
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var databaseAlias = ParseIdentifier();
        return new Statement.DetachDuckDBDatabase(
            IfExists: ifExists,
            Database: database,
            DatabaseAlias: databaseAlias);
    }

    /// <summary>Parse <c>ATTACH DATABASE</c> statement.</summary>
    public Statement ParseAttachDatabase()
    {
        var database = ParseKeyword(Keyword.DATABASE);
        var databaseFileName = ParseExpr();
        ExpectKeywordIs(Keyword.AS);
        var schemaName = ParseIdentifier();
        return new Statement.AttachDatabase(
            Database: database,
            SchemaName: schemaName,
            DatabaseFileName: databaseFileName);
    }

    /// <summary>Parse <c>ANALYZE</c> statement.</summary>
    public Analyze ParseAnalyze()
    {
        var hasTableKeyword = ParseKeyword(Keyword.TABLE);
        var tableName = MaybeParse(parser => parser.ParseObjectName(false));
        var forColumns = false;
        var cacheMetadata = false;
        var noscan = false;
        Sequence<Expr>? partitions = null;
        var computeStatistics = false;
        var columns = Sequence<Ident>.Empty;

        // PostgreSQL syntax: ANALYZE t (col1, col2)
        if (tableName is not null && ConsumeToken(new Token.LParen()))
        {
            columns = ParseCommaSeparated(p => p.ParseIdentifier());
            ExpectToken(new Token.RParen());
        }

        while (true)
        {
            var stop = false;
            switch (ParseOneOfKeywords(
                        Keyword.PARTITION,
                        Keyword.FOR,
                        Keyword.CACHE,
                        Keyword.NOSCAN,
                        Keyword.COMPUTE))
            {
                case Keyword.PARTITION:
                    ExpectToken(new Token.LParen());
                    partitions = ParseCommaSeparated(p => p.ParseExpr());
                    ExpectToken(new Token.RParen());
                    break;
                case Keyword.NOSCAN:
                    noscan = true;
                    break;
                case Keyword.FOR:
                    ExpectKeywordIs(Keyword.COLUMNS);

                    columns = MaybeParseValue(parser =>
                            parser.ParseCommaSeparated(p => p.ParseIdentifier()))
                        ?? Sequence<Ident>.Empty;
                    forColumns = true;
                    break;
                case Keyword.CACHE:
                    ExpectKeywordIs(Keyword.METADATA);
                    cacheMetadata = true;
                    break;
                case Keyword.COMPUTE:
                    ExpectKeywordIs(Keyword.STATISTICS);
                    computeStatistics = true;
                    break;
                default:
                    stop = true;
                    break;
            }
            if (stop)
            {
                break;
            }
        }

        return new Analyze(
            HasTableKeyword: hasTableKeyword,
            TableName: tableName,
            ForColumns: forColumns,
            Columns: columns,
            Partitions: partitions,
            CacheMetadata: cacheMetadata,
            Noscan: noscan,
            ComputeStatistics: computeStatistics);
    }

    /// <summary>Parse a new expression including wildcard &amp; qualified wildcard.</summary>
    public Expr ParseWildcardExpr()
    {
        var index = _index;

        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word or Token.SingleQuotedString
                when PeekTokenRef().Token is Token.Period:
            {
                var t = nextToken.Token;
                Ident firstPart;
                switch (t)
                {
                    case Token.Word { Value: var w }:
                        firstPart = w.IntoIdent(nextToken.Span);
                        break;
                    case Token.SingleQuotedString { Value: var s }:
                        firstPart = Ident.WithQuote('\'', s);
                        break;
                    default:
                        throw new ParserException(new ParserError.ParserErrorValue(
                            "Internal parser error: unexpected token type"));
                }
                var idParts = new List<Ident> { firstPart };

                while (ConsumeToken(new Token.Period()))
                {
                    var partToken = NextToken();
                    // `break` do laço dentro do `match`: flag `stop` (convencoes.md §13, caso 100).
                    var stop = false;
                    switch (partToken.Token)
                    {
                        case Token.Word { Value: var w }:
                            idParts.Add(w.IntoIdent(partToken.Span));
                            break;
                        case Token.SingleQuotedString { Value: var s }:
                            // SQLite has single-quoted identifiers
                            idParts.Add(Ident.WithQuote('\'', s));
                            break;
                        case Token.Placeholder { Value: var s }:
                            // Snowflake uses $1, $2, etc. for positional column references
                            // in staged data queries like: SELECT t.$1 FROM @stage t
                            idParts.Add(new Ident(s));
                            break;
                        case Token.Mul:
                            return new Expr.QualifiedWildcard(
                                Prefix: ObjectName.From(idParts.ToSequence()),
                                Token: new AttachedToken(partToken));
                        // A field index such as `t.1` is parsed as an expression.
                        case Token.Number when _dialect.SupportsTupleElementAccess:
                            stop = true;
                            break;
                        default:
                            return Expected<Expr>("an identifier or a '*' after '.'", partToken);
                    }
                    if (stop)
                    {
                        break;
                    }
                }
                break;
            }
            case Token.Mul:
                return new Expr.Wildcard(new AttachedToken(nextToken));
            // Handle parenthesized wildcard: (*)
            case Token.LParen:
            {
                var peeked = PeekTokensRef(2);
                var maybeMul = peeked[0];
                var maybeRparen = peeked[1];
                if (maybeMul.Token is Token.Mul && maybeRparen.Token is Token.RParen)
                {
                    var mulToken = NextToken(); // consume Mul
                    NextToken(); // consume RParen
                    return new Expr.Wildcard(new AttachedToken(mulToken));
                }
                break;
            }
            default:
                break;
        }

        _index = index;
        return ParseExpr();
    }

    /// <summary>Parse a new expression.</summary>
    public Expr ParseExpr() => ParseSubexpr(_dialect.PrecUnknown);

    // Rust: o literal `&[Keyword::ASC, Keyword::DESC, Keyword::GROUP]` de `validator` (abaixo), em cache
    // para não alocar a cada alias (convencoes.md §13, caso 11).
    private static readonly Keyword[] ParseExprWithAliasAndOrderByValidatorKeywords =
        { Keyword.ASC, Keyword.DESC, Keyword.GROUP };

    /// <summary>Parse expression with optional alias and order by.</summary>
    public ExprWithAliasAndOrderBy ParseExprWithAliasAndOrderBy()
    {
        var expr = ParseExpr();

        // Rust: `fn validator(..)` aninhada → função local estática (convencoes.md §13, caso 67).
        static bool Validator(bool @explicit, Keyword kw, Parser _parser) =>
            @explicit || !ParseExprWithAliasAndOrderByValidatorKeywords.Contains(kw);

        var alias = ParseOptionalAliasInner(null, AliasKind.Column, Validator);
        var orderBy = new OrderByOptions(
            Sort: ParseOptionalOrderBySort(),
            NullsFirst: null);
        return new ExprWithAliasAndOrderBy(
            Expr: new ExprWithAlias(Expr: expr, Alias: alias),
            OrderBy: orderBy);
    }

    /// <summary>Parse tokens until the precedence changes.</summary>
    public Expr ParseSubexpr(byte precedence)
    {
        // Rust: #[cfg_attr(feature = "recursive-protection", recursive::recursive)] (convencoes.md §13, caso 123).
        if (StackGuard.IsStackLow())
        {
            return StackGuard.Grow(this, precedence, static (parser, p) => parser.ParseSubexpr(p));
        }

        using var _guard = _recursionCounter.TryDecrease();
        // Rust: debug!("parsing expr"): log não traduzido (caso 44).
        var expr = ParsePrefix();

        expr = ParseCompoundExpr(expr, new List<AccessExpr>());

        // Parse an optional collation cast operator following `expr`.
        //
        // For example (MSSQL): t1.a COLLATE Latin1_General_CI_AS
        if (!InColumnDefinitionState() && ParseKeyword(Keyword.COLLATE))
        {
            expr = new Expr.Collate(
                Expr: expr,
                Collation: ParseObjectName(false));
        }

        // Rust: debug!("prefix: {expr:?}"): log não traduzido (caso 44).
        while (true)
        {
            var nextPrecedence = GetNextPrecedence();
            // Rust: debug!("next precedence: {next_precedence:?}"): log não traduzido (caso 44).

            if (precedence >= nextPrecedence)
            {
                break;
            }

            // The period operator is handled exclusively by the
            // compound field access parsing.
            if (PeekTokenRef().Token is Token.Period)
            {
                break;
            }

            expr = ParseInfix(expr, nextPrecedence);
        }
        return expr;
    }

    /// <summary>Parse <c>ASSERT</c> statement.</summary>
    public Statement ParseAssert()
    {
        var condition = ParseExpr();
        Expr? message;
        if (ParseKeyword(Keyword.AS))
        {
            message = ParseExpr();
        }
        else
        {
            message = null;
        }

        return new Statement.Assert(Condition: condition, Message: message);
    }

    /// <summary>Parse <c>SAVEPOINT</c> statement.</summary>
    public Statement ParseSavepoint()
    {
        var name = ParseIdentifier();
        return new Statement.Savepoint(Name: name);
    }

    /// <summary>Parse <c>RELEASE</c> statement.</summary>
    public Statement ParseRelease()
    {
        _ = ParseKeyword(Keyword.SAVEPOINT);
        var name = ParseIdentifier();

        return new Statement.ReleaseSavepoint(Name: name);
    }

    /// <summary>Parse <c>LISTEN</c> statement.</summary>
    public Statement ParseListen()
    {
        var channel = ParseIdentifier();
        return new Statement.LISTEN(Channel: channel);
    }

    /// <summary>Parse <c>UNLISTEN</c> statement.</summary>
    public Statement ParseUnlisten()
    {
        Ident channel;
        if (ConsumeToken(new Token.Mul()))
        {
            channel = new Ident(new Expr.Wildcard(AttachedToken.Empty()).ToString());
        }
        else
        {
            // match self.parse_identifier() { Ok(expr) => expr, _ => { .. } }
            try
            {
                channel = ParseIdentifier();
            }
            catch (ParserException)
            {
                PrevToken();
                return ExpectedRef<Statement>("wildcard or identifier", PeekTokenRef());
            }
        }
        return new Statement.UNLISTEN(Channel: channel);
    }

    /// <summary>Parse <c>NOTIFY</c> statement.</summary>
    public Statement ParseNotify()
    {
        var channel = ParseIdentifier();
        string? payload;
        if (ConsumeToken(new Token.Comma()))
        {
            payload = ParseLiteralString();
        }
        else
        {
            payload = null;
        }
        return new Statement.NOTIFY(Channel: channel, Payload: payload);
    }

    /// <summary>Parses a <c>RENAME TABLE</c> statement. See <c>Statement::RenameTable</c></summary>
    public Statement ParseRename()
    {
        if (PeekKeyword(Keyword.TABLE))
        {
            ExpectKeyword(Keyword.TABLE);
            var renameTables = ParseCommaSeparated(parser =>
            {
                var oldName = parser.ParseObjectName(false);
                parser.ExpectKeyword(Keyword.TO);
                var newName = parser.ParseObjectName(false);

                return new RenameTable(OldName: oldName, NewName: newName);
            });
            return Statement.From(renameTables);
        }
        else
        {
            return ExpectedRef<Statement>("KEYWORD `TABLE` after RENAME", PeekTokenRef());
        }
    }
}
