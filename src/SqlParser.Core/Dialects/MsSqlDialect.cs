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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/mssql.rs.
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.microsoft.com/en-us/sql-server/">Microsoft SQL Server</see>
/// </summary>
public class MsSqlDialect : Dialect
{
    public override bool IsDelimitedIdentifierStart(char ch) => ch == '"' || ch == '[';

    public override bool IsIdentifierStart(char ch) =>
        // See https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-2017#rules-for-regular-identifiers
        char.IsLetter(ch) || ch == '_' || ch == '#' || ch == '@';

    public override bool IsIdentifierPart(char ch) =>
        char.IsLetter(ch)
            || (ch >= '0' && ch <= '9')
            || ch == '@'
            || ch == '$'
            || ch == '#'
            || ch == '_';

    public override char? IdentifierQuoteStyle(string identifier) => '[';

    /// <summary>
    /// SQL Server has <c>CONVERT(type, value)</c> instead of <c>CONVERT(value, type)</c>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/cast-and-convert-transact-sql?view=sql-server-ver16"/>
    /// </summary>
    public override bool ConvertTypeBeforeValue => true;

    public override bool SupportsOuterJoinOperator => true;

    /// <summary>
    /// SQL Server supports <c>$</c> as a prefix for money literals
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/data-types/constants-transact-sql?view=sql-server-ver17#money-constants"/>
    /// </summary>
    public override bool SupportsDollarAsMoneyPrefix => true;

    public override bool SupportsConnectBy => true;

    public override bool SupportsEqAliasAssignment => true;

    public override bool SupportsTryConvert => true;

    /// <summary>
    /// In MSSQL, there is no boolean type, and <c>true</c> and <c>false</c> are valid column names
    /// </summary>
    public override bool SupportsBooleanLiterals => false;

    public override bool SupportsNamedFnArgsWithColonOperator => true;

    public override bool SupportsNamedFnArgsWithExprName => true;

    public override bool SupportsNamedFnArgsWithRarrowOperator => false;

    public override bool SupportsStartTransactionModifier => true;

    public override bool SupportsEndTransactionModifier => true;

    /// <summary>
    /// See: <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/set-statements-transact-sql"/>
    /// </summary>
    public override bool SupportsSetStmtWithoutOperator => true;

    /// <summary>
    /// See: <see href="https://learn.microsoft.com/en-us/sql/relational-databases/tables/querying-data-in-a-system-versioned-temporal-table"/>
    /// </summary>
    public override bool SupportsTableVersioning => true;

    /// <summary>
    /// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/slash-star-comment-transact-sql?view=sql-server-ver16"/>
    /// </summary>
    public override bool SupportsNestedComments => true;

    /// <summary>
    /// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/queries/from-transact-sql"/>
    /// </summary>
    public override bool SupportsObjectNameDoubleDotNotation => true;

    // Rust: `&[GranteesType::Public]` → lista somente leitura em cache (convencoes.md §13, caso 10): um
    // array exposto como IReadOnlyList<T> poderia ser alterado por cast para T[].
    private static readonly IReadOnlyList<GranteesType> ReservedGranteesTypes =
        System.Array.AsReadOnly(new[] { GranteesType.Public });

    /// <summary>
    /// See <see href="https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/server-level-roles"/>
    /// </summary>
    public override IReadOnlyList<GranteesType> GetReservedGranteesTypes => ReservedGranteesTypes;

    public override bool IsSelectItemAlias(bool @explicit, Keyword kw, Parser parser)
    {
        switch (kw)
        {
            // List of keywords that cannot be used as select item (column) aliases in MSSQL
            // regardless of whether the alias is explicit or implicit.
            //
            // These are T-SQL statement-starting keywords; allowing them as implicit aliases
            // causes the parser to consume the keyword as an alias for the previous expression,
            // then fail on the token that follows (e.g. `TABLE`, `@var`, `sp_name`, …).
            case Keyword.IF
                or Keyword.ELSE
                or Keyword.DECLARE
                or Keyword.EXEC
                or Keyword.EXECUTE
                or Keyword.INSERT
                or Keyword.UPDATE
                or Keyword.DELETE
                or Keyword.DROP
                or Keyword.CREATE
                or Keyword.ALTER
                or Keyword.TRUNCATE
                or Keyword.PRINT
                or Keyword.WHILE
                or Keyword.RETURN
                or Keyword.THROW
                or Keyword.RAISERROR
                or Keyword.MERGE:
                return false;
            default:
                // DIV-002: `self.is_column_alias(..)` é chamada virtual em C#.
                return @explicit || IsColumnAlias(kw, parser);
        }
    }

    public override bool IsTableFactorAlias(bool @explicit, Keyword kw, Parser parser)
    {
        switch (kw)
        {
            // List of keywords that cannot be used as table aliases in MSSQL
            // regardless of whether the alias is explicit or implicit.
            //
            // These are T-SQL statement-starting keywords. Without blocking them here,
            // a bare `SELECT * FROM t` followed by a newline and one of these keywords
            // would cause the parser to consume the keyword as a table alias for `t`,
            // then fail on the token that follows (e.g. `@var`, `sp_name`, `TABLE`, …).
            //
            // `SET` is already covered by the global `RESERVED_FOR_TABLE_ALIAS` list;
            // the keywords below are MSSQL-specific additions.
            case Keyword.IF
                or Keyword.ELSE
                or Keyword.DECLARE
                or Keyword.EXEC
                or Keyword.EXECUTE
                or Keyword.INSERT
                or Keyword.UPDATE
                or Keyword.DELETE
                or Keyword.DROP
                or Keyword.CREATE
                or Keyword.ALTER
                or Keyword.TRUNCATE
                or Keyword.PRINT
                or Keyword.WHILE
                or Keyword.RETURN
                or Keyword.THROW
                or Keyword.RAISERROR
                or Keyword.MERGE:
                return false;
            default:
                // DIV-002: `self.is_table_alias(..)` é chamada virtual em C#.
                return @explicit || IsTableAlias(kw, parser);
        }
    }

    public override Statement? ParseStatement(Parser parser)
    {
        if (parser.ParseKeyword(Keyword.BEGIN))
        {
            // Check if this is a BEGIN...END block rather than BEGIN TRANSACTION
            // Rust: `parser.maybe_parse(|p| { .. Ok(()) }).unwrap_or(None).is_some()`. A closure devolve
            // `()`; aqui devolve `true` como valor de unidade. `.unwrap_or(None)` descarta o `Err`
            // (limite de recursão) do `maybe_parse` (decisão 3 da B1).
            bool? maybeBlock;
            try
            {
                maybeBlock = parser.MaybeParseValue(p =>
                {
                    if (p.ParseTransactionModifier() is not null
                        || p.ParseOneOfKeywords(
                            Keyword.TRANSACTION,
                            Keyword.WORK,
                            Keyword.TRAN) is not null
                        || p.PeekTokenRef().Token is Token.SemiColon or Token.EOF)
                    {
                        return p.ExpectedRef<bool>("statement", p.PeekTokenRef());
                    }
                    else
                    {
                        return true;
                    }
                });
            }
            catch (ParserException)
            {
                maybeBlock = null;
            }
            var isBlock = maybeBlock is not null;
            if (isBlock)
            {
                return parser.ParseBeginExceptionEnd();
            }
            else
            {
                parser.PrevToken();
                return null;
            }
        }
        else if (parser.PeekKeyword(Keyword.IF))
        {
            return ParseIfStmt(parser);
        }
        else if (parser.ParseKeywords(Keyword.CREATE, Keyword.TRIGGER))
        {
            return ParseCreateTrigger(parser, false);
        }
        else if (parser.ParseKeywords(
            Keyword.CREATE,
            Keyword.OR,
            Keyword.ALTER,
            Keyword.TRIGGER))
        {
            return ParseCreateTrigger(parser, true);
        }
        else
        {
            return null;
        }
    }

    public override byte? GetNextPrecedence(Parser parser)
    {
        var token = parser.PeekTokenRef();
        switch (token.Token)
        {
            // lowest prec to prevent it from turning into a binary op
            case Token.Colon:
                return PrecUnknown;
            default:
                return null;
        }
    }

    // Rust: bloco `impl MsSqlDialect` (métodos próprios, não do trait: não virtuais; `fn` privada →
    // `internal`, convencoes.md §13, caso 52).

    /// <summary>
    /// <code>
    /// IF boolean_expression
    ///     { sql_statement | statement_block }
    /// [ ELSE
    ///     { sql_statement | statement_block } ]
    /// </code>
    /// </summary>
    internal Statement ParseIfStmt(Parser parser)
    {
        var ifToken = parser.ExpectKeyword(Keyword.IF);

        var condition = parser.ParseExpr();

        ConditionalStatementBlock ifBlock;
        if (parser.PeekKeyword(Keyword.BEGIN))
        {
            var beginToken = parser.ExpectKeyword(Keyword.BEGIN);
            var statements = ParseStatementList(parser, Keyword.END);
            var endToken = parser.ExpectKeyword(Keyword.END);
            ifBlock = new ConditionalStatementBlock(
                StartToken: new AttachedToken(ifToken),
                Condition: condition,
                ThenToken: null,
                ConditionalStatements: new ConditionalStatements.BeginEnd(new BeginEndStatements(
                    BeginToken: new AttachedToken(beginToken),
                    Statements: statements,
                    EndToken: new AttachedToken(endToken))));
        }
        else
        {
            var stmt = parser.ParseStatement();
            ifBlock = new ConditionalStatementBlock(
                StartToken: new AttachedToken(ifToken),
                Condition: condition,
                ThenToken: null,
                ConditionalStatements: new ConditionalStatements.Sequence(
                    Statements: Sequence.Of(stmt)));
        }

        var priorStatementEndedWithSemiColon = false;
        while (parser.PeekTokenRef().Token is Token.SemiColon)
        {
            parser.AdvanceToken();
            priorStatementEndedWithSemiColon = true;
        }

        ConditionalStatementBlock? elseBlock = null;
        if (parser.PeekKeyword(Keyword.ELSE))
        {
            var elseToken = parser.ExpectKeyword(Keyword.ELSE);
            if (parser.PeekKeyword(Keyword.BEGIN))
            {
                var beginToken = parser.ExpectKeyword(Keyword.BEGIN);
                var statements = ParseStatementList(parser, Keyword.END);
                var endToken = parser.ExpectKeyword(Keyword.END);
                elseBlock = new ConditionalStatementBlock(
                    StartToken: new AttachedToken(elseToken),
                    Condition: null,
                    ThenToken: null,
                    ConditionalStatements: new ConditionalStatements.BeginEnd(new BeginEndStatements(
                        BeginToken: new AttachedToken(beginToken),
                        Statements: statements,
                        EndToken: new AttachedToken(endToken))));
            }
            else
            {
                var stmt = parser.ParseStatement();
                elseBlock = new ConditionalStatementBlock(
                    StartToken: new AttachedToken(elseToken),
                    Condition: null,
                    ThenToken: null,
                    ConditionalStatements: new ConditionalStatements.Sequence(
                        Statements: Sequence.Of(stmt)));
            }
        }
        else if (priorStatementEndedWithSemiColon)
        {
            parser.PrevToken();
        }

        return Statement.From(new IfStatement(
            IfBlock: ifBlock,
            ElseBlock: elseBlock,
            ElseifBlocks: Sequence<ConditionalStatementBlock>.Empty,
            EndToken: null));
    }

    /// <summary>
    /// Parse <c>CREATE TRIGGER</c> for <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-trigger-transact-sql">MsSql</see>
    /// </summary>
    internal Statement ParseCreateTrigger(Parser parser, bool orAlter)
    {
        var name = parser.ParseObjectName(false);
        parser.ExpectKeywordIs(Keyword.ON);
        var tableName = parser.ParseObjectName(false);
        var period = parser.ParseTriggerPeriod();
        var events = parser.ParseCommaSeparated(p => p.ParseTriggerEvent());

        parser.ExpectKeywordIs(Keyword.AS);
        var statements = parser.ParseConditionalStatements(Keyword.END);

        return Statement.From(new CreateTrigger(
            OrAlter: orAlter,
            Temporary: false,
            OrReplace: false,
            IsConstraint: false,
            Name: name,
            Period: period,
            PeriodBeforeTable: false,
            Events: events,
            TableName: tableName,
            ReferencedTableName: null,
            Referencing: Sequence<TriggerReferencing>.Empty,
            TriggerObject: null,
            Condition: null,
            ExecBody: null,
            StatementsAs: true,
            Statements: statements,
            Characteristics: null));
    }

    /// <summary>Parse a sequence of statements, optionally separated by semicolon.</summary>
    /// <remarks>Stops parsing when reaching EOF or the given keyword.</remarks>
    internal Sequence<Statement> ParseStatementList(Parser parser, Keyword? terminalKeyword)
    {
        var stmts = new List<Statement>();
        while (true)
        {
            if (parser.PeekTokenRef().Token is Token.EOF)
            {
                break;
            }
            if (terminalKeyword is { } term)
            {
                if (parser.PeekKeyword(term))
                {
                    break;
                }
            }
            stmts.Add(parser.ParseStatement());
            while (parser.PeekTokenRef().Token is Token.SemiColon)
            {
                parser.AdvanceToken();
            }
        }
        return stmts.ToSequence();
    }
}
