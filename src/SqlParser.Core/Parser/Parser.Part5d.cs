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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 20306–21240:
// parse_create_sequence … parse_reset, mais a função livre maybe_prefixed_expr).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>
    /// <code>
    /// CREATE [ { TEMPORARY | TEMP } ] SEQUENCE [ IF NOT EXISTS ] &lt;sequence_name&gt;
    /// </code>
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.postgresql.org/docs/current/sql-createsequence.html">Postgres docs</see> for more details.
    /// </remarks>
    public Statement ParseCreateSequence(bool temporary)
    {
        //[ IF NOT EXISTS ]
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        //name
        var name = ParseObjectName(false);
        //[ AS data_type ]
        DataType? dataType = null;
        if (ParseKeywords(Keyword.AS))
        {
            dataType = ParseDataType();
        }
        var sequenceOptions = ParseCreateSequenceOptions();
        // [ OWNED BY { table_name.column_name | NONE } ]
        ObjectName? ownedBy;
        if (ParseKeywords(Keyword.OWNED, Keyword.BY))
        {
            if (ParseKeywords(Keyword.NONE))
            {
                ownedBy = ObjectName.From(Sequence.Of(new Ident("NONE")));
            }
            else
            {
                ownedBy = ParseObjectName(false);
            }
        }
        else
        {
            ownedBy = null;
        }
        return new Statement.CreateSequence(
            Temporary: temporary,
            IfNotExists: ifNotExists,
            Name: name,
            DataType: dataType,
            SequenceOptions: sequenceOptions,
            OwnedBy: ownedBy);
    }

    internal Sequence<SequenceOptions> ParseCreateSequenceOptions()
    {
        var sequenceOptions = new List<SequenceOptions>();
        //[ INCREMENT [ BY ] increment ]
        if (ParseKeywords(Keyword.INCREMENT))
        {
            if (ParseKeywords(Keyword.BY))
            {
                sequenceOptions.Add(new SequenceOptions.IncrementBy(ParseNumber(), true));
            }
            else
            {
                sequenceOptions.Add(new SequenceOptions.IncrementBy(ParseNumber(), false));
            }
        }
        //[ MINVALUE minvalue | NO MINVALUE ]
        if (ParseKeyword(Keyword.MINVALUE))
        {
            sequenceOptions.Add(new SequenceOptions.MinValue(ParseNumber()));
        }
        else if (ParseKeywords(Keyword.NO, Keyword.MINVALUE))
        {
            sequenceOptions.Add(new SequenceOptions.MinValue(null));
        }
        //[ MAXVALUE maxvalue | NO MAXVALUE ]
        if (ParseKeywords(Keyword.MAXVALUE))
        {
            sequenceOptions.Add(new SequenceOptions.MaxValue(ParseNumber()));
        }
        else if (ParseKeywords(Keyword.NO, Keyword.MAXVALUE))
        {
            sequenceOptions.Add(new SequenceOptions.MaxValue(null));
        }

        //[ START [ WITH ] start ]
        if (ParseKeywords(Keyword.START))
        {
            if (ParseKeywords(Keyword.WITH))
            {
                sequenceOptions.Add(new SequenceOptions.StartWith(ParseNumber(), true));
            }
            else
            {
                sequenceOptions.Add(new SequenceOptions.StartWith(ParseNumber(), false));
            }
        }
        //[ CACHE cache ]
        if (ParseKeywords(Keyword.CACHE))
        {
            sequenceOptions.Add(new SequenceOptions.Cache(ParseNumber()));
        }
        // [ [ NO ] CYCLE ]
        if (ParseKeywords(Keyword.NO, Keyword.CYCLE))
        {
            sequenceOptions.Add(new SequenceOptions.Cycle(true));
        }
        else if (ParseKeywords(Keyword.CYCLE))
        {
            sequenceOptions.Add(new SequenceOptions.Cycle(false));
        }

        return sequenceOptions.ToSequence();
    }

    /// <summary>Parse a <c>CREATE SERVER</c> statement.</summary>
    /// <remarks>See <see cref="Statement.CreateServer"/></remarks>
    public Statement ParsePgCreateServer()
    {
        var ine = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = ParseObjectName(false);

        Ident? serverType;
        if (ParseKeyword(Keyword.TYPE))
        {
            serverType = ParseIdentifier();
        }
        else
        {
            serverType = null;
        }

        Ident? version;
        if (ParseKeyword(Keyword.VERSION))
        {
            version = ParseIdentifier();
        }
        else
        {
            version = null;
        }

        ExpectKeywords(Keyword.FOREIGN, Keyword.DATA, Keyword.WRAPPER);
        var foreignDataWrapper = ParseObjectName(false);

        Sequence<CreateServerOption>? options = null;
        if (ParseKeyword(Keyword.OPTIONS))
        {
            ExpectToken(new Token.LParen());
            options = ParseCommaSeparated(p =>
            {
                var key = p.ParseIdentifier();
                var value = p.ParseIdentifier();
                return new CreateServerOption(Key: key, Value: value);
            });
            ExpectToken(new Token.RParen());
        }

        return new Statement.CreateServer(new CreateServerStatement(
            Name: name,
            IfNotExists: ine,
            ServerType: serverType,
            Version: version,
            ForeignDataWrapper: foreignDataWrapper,
            Options: options));
    }

    /// <summary>The index of the first unprocessed token.</summary>
    public int Index() => _index;

    /// <summary>Parse a named window definition.</summary>
    public NamedWindowDefinition ParseNamedWindow()
    {
        var ident = ParseIdentifier();
        ExpectKeywordIs(Keyword.AS);

        NamedWindowExpr windowExpr;
        if (ConsumeToken(new Token.LParen()))
        {
            windowExpr = new NamedWindowExpr.WindowSpec(ParseWindowSpec());
        }
        else if (_dialect.SupportsWindowClauseNamedWindowReference)
        {
            windowExpr = new NamedWindowExpr.NamedWindow(ParseIdentifier());
        }
        else
        {
            return ExpectedRef<NamedWindowDefinition>("(", PeekTokenRef());
        }

        return new NamedWindowDefinition(ident, windowExpr);
    }

    /// <summary>Parse <c>CREATE PROCEDURE</c> statement.</summary>
    public Statement ParseCreateProcedure(bool orAlter)
    {
        var name = ParseObjectName(false);
        var @params = ParseOptionalProcedureParameters();

        Ident? language;
        if (ParseKeyword(Keyword.LANGUAGE))
        {
            language = ParseIdentifier();
        }
        else
        {
            language = null;
        }

        ExpectKeywordIs(Keyword.AS);

        var body = ParseConditionalStatements(Keyword.END);

        return new Statement.CreateProcedure(
            Name: name,
            OrAlter: orAlter,
            Params: @params,
            Language: language,
            Body: body);
    }

    /// <summary>Parse a window specification.</summary>
    public WindowSpec ParseWindowSpec()
    {
        Ident? windowName;
        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var word } when word.Keyword == Keyword.NoKeyword:
                windowName = ParseOptionalIdent();
                break;
            default:
                windowName = null;
                break;
        }

        Sequence<Expr> partitionBy;
        if (ParseKeywords(Keyword.PARTITION, Keyword.BY))
        {
            partitionBy = ParseCommaSeparated(p => p.ParseExpr());
        }
        else
        {
            partitionBy = Sequence<Expr>.Empty;
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

        WindowFrame? windowFrame;
        if (!ConsumeToken(new Token.RParen()))
        {
            // Rust: `let window_frame` interno (caso 72).
            var parsedWindowFrame = ParseWindowFrame();
            ExpectToken(new Token.RParen());
            windowFrame = parsedWindowFrame;
        }
        else
        {
            windowFrame = null;
        }
        return new WindowSpec(
            WindowName: windowName,
            PartitionBy: partitionBy,
            OrderBy: orderBy,
            WindowFrame: windowFrame);
    }

    /// <summary>Parse <c>CREATE TYPE</c> statement.</summary>
    public Statement ParseCreateType()
    {
        var name = ParseObjectName(false);

        // Check if we have AS keyword
        var hasAs = ParseKeyword(Keyword.AS);

        if (!hasAs)
        {
            // Two cases: CREATE TYPE name; or CREATE TYPE name (options);
            if (ConsumeToken(new Token.LParen()))
            {
                // CREATE TYPE name (options) - SQL definition without AS
                var options = ParseCreateTypeSqlDefinitionOptions();
                ExpectToken(new Token.RParen());
                return new Statement.CreateType(
                    Name: name,
                    Representation: new UserDefinedTypeRepresentation.SqlDefinition(Options: options));
            }

            // CREATE TYPE name; - no representation
            return new Statement.CreateType(
                Name: name,
                Representation: null);
        }

        // We have AS keyword
        if (ParseKeyword(Keyword.ENUM))
        {
            // CREATE TYPE name AS ENUM (labels)
            return ParseCreateTypeEnum(name);
        }
        else if (ParseKeyword(Keyword.RANGE))
        {
            // CREATE TYPE name AS RANGE (options)
            return ParseCreateTypeRange(name);
        }
        else if (ConsumeToken(new Token.LParen()))
        {
            // CREATE TYPE name AS (attributes) - Composite
            return ParseCreateTypeComposite(name);
        }
        else
        {
            return ExpectedRef<Statement>("ENUM, RANGE, or '(' after AS", PeekTokenRef());
        }
    }

    /// <summary>Parse remainder of <c>CREATE TYPE AS (attributes)</c> statement (composite type)</summary>
    /// <remarks>See <see href="https://www.postgresql.org/docs/current/sql-createtype.html">PostgreSQL</see></remarks>
    internal Statement ParseCreateTypeComposite(ObjectName name)
    {
        if (ConsumeToken(new Token.RParen()))
        {
            // Empty composite type
            return new Statement.CreateType(
                Name: name,
                Representation: new UserDefinedTypeRepresentation.Composite(
                    Attributes: Sequence<UserDefinedTypeCompositeAttributeDef>.Empty));
        }

        var attributes = new List<UserDefinedTypeCompositeAttributeDef>();
        while (true)
        {
            var attrName = ParseIdentifier();
            var attrDataType = ParseDataType();
            ObjectName? attrCollation;
            if (ParseKeyword(Keyword.COLLATE))
            {
                attrCollation = ParseObjectName(false);
            }
            else
            {
                attrCollation = null;
            }
            attributes.Add(new UserDefinedTypeCompositeAttributeDef(
                Name: attrName,
                DataType: attrDataType,
                Collation: attrCollation));

            if (!ConsumeToken(new Token.Comma()))
            {
                break;
            }
        }
        ExpectToken(new Token.RParen());

        return new Statement.CreateType(
            Name: name,
            Representation: new UserDefinedTypeRepresentation.Composite(Attributes: attributes.ToSequence()));
    }

    /// <summary>
    /// Parse remainder of <c>CREATE TYPE AS ENUM</c> statement (see <see cref="Statement.CreateType"/> and
    /// <see cref="ParseCreateType"/>)
    /// </summary>
    /// <remarks>See <see href="https://www.postgresql.org/docs/current/sql-createtype.html">PostgreSQL</see></remarks>
    public Statement ParseCreateTypeEnum(ObjectName name)
    {
        ExpectToken(new Token.LParen());
        var labels = ParseCommaSeparated0(p => p.ParseIdentifier(), new Token.RParen());
        ExpectToken(new Token.RParen());

        return new Statement.CreateType(
            Name: name,
            Representation: new UserDefinedTypeRepresentation.Enum(Labels: labels));
    }

    /// <summary>Parse remainder of <c>CREATE TYPE AS RANGE</c> statement</summary>
    /// <remarks>See <see href="https://www.postgresql.org/docs/current/sql-createtype.html">PostgreSQL</see></remarks>
    internal Statement ParseCreateTypeRange(ObjectName name)
    {
        ExpectToken(new Token.LParen());
        var options = ParseCommaSeparated0(p => p.ParseRangeOption(), new Token.RParen());
        ExpectToken(new Token.RParen());

        return new Statement.CreateType(
            Name: name,
            Representation: new UserDefinedTypeRepresentation.Range(Options: options));
    }

    /// <summary>Parse a single range option for a <c>CREATE TYPE AS RANGE</c> statement</summary>
    internal UserDefinedTypeRangeOption ParseRangeOption()
    {
        var keyword = ParseOneOfKeywords(
            Keyword.SUBTYPE,
            Keyword.SUBTYPE_OPCLASS,
            Keyword.COLLATION,
            Keyword.CANONICAL,
            Keyword.SUBTYPE_DIFF,
            Keyword.MULTIRANGE_TYPE_NAME);

        switch (keyword)
        {
            case Keyword.SUBTYPE:
            {
                ExpectToken(new Token.Eq());
                var dataType = ParseDataType();
                return new UserDefinedTypeRangeOption.Subtype(dataType);
            }
            case Keyword.SUBTYPE_OPCLASS:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeRangeOption.SubtypeOpClass(name);
            }
            case Keyword.COLLATION:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeRangeOption.Collation(name);
            }
            case Keyword.CANONICAL:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeRangeOption.Canonical(name);
            }
            case Keyword.SUBTYPE_DIFF:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeRangeOption.SubtypeDiff(name);
            }
            case Keyword.MULTIRANGE_TYPE_NAME:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeRangeOption.MultirangeTypeName(name);
            }
            default:
                return ExpectedRef<UserDefinedTypeRangeOption>("range option keyword", PeekTokenRef());
        }
    }

    /// <summary>Parse SQL definition options for CREATE TYPE (options)</summary>
    internal Sequence<UserDefinedTypeSqlDefinitionOption> ParseCreateTypeSqlDefinitionOptions() =>
        ParseCommaSeparated0(p => p.ParseSqlDefinitionOption(), new Token.RParen());

    /// <summary>Parse a single SQL definition option for CREATE TYPE (options)</summary>
    internal UserDefinedTypeSqlDefinitionOption ParseSqlDefinitionOption()
    {
        var keyword = ParseOneOfKeywords(
            Keyword.INPUT,
            Keyword.OUTPUT,
            Keyword.RECEIVE,
            Keyword.SEND,
            Keyword.TYPMOD_IN,
            Keyword.TYPMOD_OUT,
            Keyword.ANALYZE,
            Keyword.SUBSCRIPT,
            Keyword.INTERNALLENGTH,
            Keyword.PASSEDBYVALUE,
            Keyword.ALIGNMENT,
            Keyword.STORAGE,
            Keyword.LIKE,
            Keyword.CATEGORY,
            Keyword.PREFERRED,
            Keyword.DEFAULT,
            Keyword.ELEMENT,
            Keyword.DELIMITER,
            Keyword.COLLATABLE);

        switch (keyword)
        {
            case Keyword.INPUT:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.Input(name);
            }
            case Keyword.OUTPUT:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.Output(name);
            }
            case Keyword.RECEIVE:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.Receive(name);
            }
            case Keyword.SEND:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.Send(name);
            }
            case Keyword.TYPMOD_IN:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.TypmodIn(name);
            }
            case Keyword.TYPMOD_OUT:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.TypmodOut(name);
            }
            case Keyword.ANALYZE:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.Analyze(name);
            }
            case Keyword.SUBSCRIPT:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.Subscript(name);
            }
            case Keyword.INTERNALLENGTH:
            {
                ExpectToken(new Token.Eq());
                if (ParseKeyword(Keyword.VARIABLE))
                {
                    return new UserDefinedTypeSqlDefinitionOption.InternalLength(
                        new UserDefinedTypeInternalLength.Variable());
                }
                else
                {
                    var value = ParseLiteralUint();
                    return new UserDefinedTypeSqlDefinitionOption.InternalLength(
                        new UserDefinedTypeInternalLength.Fixed(value));
                }
            }
            case Keyword.PASSEDBYVALUE:
                return new UserDefinedTypeSqlDefinitionOption.PassedByValue();
            case Keyword.ALIGNMENT:
            {
                ExpectToken(new Token.Eq());
                var alignKeyword = ParseOneOfKeywords(
                    Keyword.CHAR,
                    Keyword.INT2,
                    Keyword.INT4,
                    Keyword.DOUBLE);
                switch (alignKeyword)
                {
                    case Keyword.CHAR:
                        return new UserDefinedTypeSqlDefinitionOption.Alignment(
                            new Alignment.Char());
                    case Keyword.INT2:
                        return new UserDefinedTypeSqlDefinitionOption.Alignment(
                            new Alignment.Int2());
                    case Keyword.INT4:
                        return new UserDefinedTypeSqlDefinitionOption.Alignment(
                            new Alignment.Int4());
                    case Keyword.DOUBLE:
                        return new UserDefinedTypeSqlDefinitionOption.Alignment(
                            new Alignment.Double());
                    default:
                        return ExpectedRef<UserDefinedTypeSqlDefinitionOption>(
                            "alignment value (char, int2, int4, or double)",
                            PeekTokenRef());
                }
            }
            case Keyword.STORAGE:
            {
                ExpectToken(new Token.Eq());
                var storageKeyword = ParseOneOfKeywords(
                    Keyword.PLAIN,
                    Keyword.EXTERNAL,
                    Keyword.EXTENDED,
                    Keyword.MAIN);
                switch (storageKeyword)
                {
                    case Keyword.PLAIN:
                        return new UserDefinedTypeSqlDefinitionOption.Storage(
                            new UserDefinedTypeStorage.Plain());
                    case Keyword.EXTERNAL:
                        return new UserDefinedTypeSqlDefinitionOption.Storage(
                            new UserDefinedTypeStorage.External());
                    case Keyword.EXTENDED:
                        return new UserDefinedTypeSqlDefinitionOption.Storage(
                            new UserDefinedTypeStorage.Extended());
                    case Keyword.MAIN:
                        return new UserDefinedTypeSqlDefinitionOption.Storage(
                            new UserDefinedTypeStorage.Main());
                    default:
                        return ExpectedRef<UserDefinedTypeSqlDefinitionOption>(
                            "storage value (plain, external, extended, or main)",
                            PeekTokenRef());
                }
            }
            case Keyword.LIKE:
            {
                ExpectToken(new Token.Eq());
                var name = ParseObjectName(false);
                return new UserDefinedTypeSqlDefinitionOption.Like(name);
            }
            case Keyword.CATEGORY:
            {
                ExpectToken(new Token.Eq());
                var categoryStr = ParseLiteralString();
                // Rust: `category_str.chars().next().ok_or_else(..)?`: o primeiro scalar value (1 ou 2 unidades
                // UTF-16; `Category(string Value)`, nomes.md).
                if (categoryStr.Length == 0)
                {
                    throw new ParserException(new ParserError.ParserErrorValue(
                        "CATEGORY value must be a single character"));
                }
                var categoryChar = char.IsSurrogatePair(categoryStr, 0)
                    ? categoryStr.Substring(0, 2)
                    : categoryStr.Substring(0, 1);
                return new UserDefinedTypeSqlDefinitionOption.Category(categoryChar);
            }
            case Keyword.PREFERRED:
            {
                ExpectToken(new Token.Eq());
                var value =
                    ParseKeyword(Keyword.TRUE) || !ParseKeyword(Keyword.FALSE);
                return new UserDefinedTypeSqlDefinitionOption.Preferred(value);
            }
            case Keyword.DEFAULT:
            {
                ExpectToken(new Token.Eq());
                var expr = ParseExpr();
                return new UserDefinedTypeSqlDefinitionOption.Default(expr);
            }
            case Keyword.ELEMENT:
            {
                ExpectToken(new Token.Eq());
                var dataType = ParseDataType();
                return new UserDefinedTypeSqlDefinitionOption.Element(dataType);
            }
            case Keyword.DELIMITER:
            {
                ExpectToken(new Token.Eq());
                var delimiter = ParseLiteralString();
                return new UserDefinedTypeSqlDefinitionOption.Delimiter(delimiter);
            }
            case Keyword.COLLATABLE:
            {
                ExpectToken(new Token.Eq());
                var value =
                    ParseKeyword(Keyword.TRUE) || !ParseKeyword(Keyword.FALSE);
                return new UserDefinedTypeSqlDefinitionOption.Collatable(value);
            }
            default:
                return ExpectedRef<UserDefinedTypeSqlDefinitionOption>("SQL definition option keyword", PeekTokenRef());
        }
    }

    internal Sequence<Ident> ParseParenthesizedIdentifiers()
    {
        ExpectToken(new Token.LParen());
        var idents = ParseCommaSeparated0(p => p.ParseIdentifier(), new Token.RParen());
        ExpectToken(new Token.RParen());
        return idents;
    }

    internal MySQLColumnPosition? ParseColumnPosition()
    {
        if (_dialect is MySqlDialect or GenericDialect)
        {
            if (ParseKeyword(Keyword.FIRST))
            {
                return new MySQLColumnPosition.First();
            }
            else if (ParseKeyword(Keyword.AFTER))
            {
                var ident = ParseIdentifier();
                return new MySQLColumnPosition.After(ident);
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse <see cref="Statement.Print"/></summary>
    internal Statement ParsePrint() =>
        new Statement.Print(new PrintStatement(
            Message: ParseExpr()));

    /// <summary>Parse <see cref="Statement.WaitFor"/></summary>
    /// <remarks>See: <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/waitfor-transact-sql"/></remarks>
    internal Statement ParseWaitfor()
    {
        WaitForType waitType;
        if (ParseKeyword(Keyword.DELAY))
        {
            waitType = new WaitForType.Delay();
        }
        else if (ParseKeyword(Keyword.TIME))
        {
            waitType = new WaitForType.Time();
        }
        else
        {
            return ExpectedRef<Statement>("DELAY or TIME", PeekTokenRef());
        }
        var expr = ParseExpr();
        return new Statement.WaitFor(new WaitForStatement(WaitType: waitType, Expr: expr));
    }

    /// <summary>Parse <see cref="Statement.Return"/></summary>
    internal Statement ParseReturn()
    {
        switch (MaybeParse(p => p.ParseExpr()))
        {
            case { } expr:
                return new Statement.Return(new ReturnStatement(
                    Value: new ReturnStatementValue.Expr(expr)));
            case null:
                return new Statement.Return(new ReturnStatement(Value: null));
        }
    }

    /// <summary>/// Parse a <c>EXPORT DATA</c> statement.</summary>
    /// <remarks>See <see cref="Statement.ExportData"/></remarks>
    internal Statement ParseExportData()
    {
        ExpectKeywords(Keyword.EXPORT, Keyword.DATA);

        ObjectName? connection;
        if (ParseKeywords(Keyword.WITH, Keyword.CONNECTION))
        {
            connection = ParseObjectName(false);
        }
        else
        {
            connection = null;
        }
        ExpectKeyword(Keyword.OPTIONS);
        ExpectToken(new Token.LParen());
        var options = ParseCommaSeparated(p => p.ParseSqlOption());
        ExpectToken(new Token.RParen());
        ExpectKeyword(Keyword.AS);
        var query = ParseQuery();
        return new Statement.ExportData(new ExportData(
            Options: options,
            Query: query,
            Connection: connection));
    }

    internal Statement ParseVacuum()
    {
        ExpectKeyword(Keyword.VACUUM);
        var full = ParseKeyword(Keyword.FULL);
        var sortOnly = ParseKeywords(Keyword.SORT, Keyword.ONLY);
        var deleteOnly = ParseKeywords(Keyword.DELETE, Keyword.ONLY);
        var reindex = ParseKeyword(Keyword.REINDEX);
        var recluster = ParseKeyword(Keyword.RECLUSTER);
        // Rust: `let (table_name, threshold, boost) = match self.maybe_parse(..)? { .. }` (decisão 7 da B2).
        ObjectName? tableName;
        ValueWithSpan? threshold;
        bool boost;
        switch (MaybeParse(p => p.ParseObjectName(false)))
        {
            case { } parsedTableName:
            {
                // Rust: `let threshold`/`let boost` internos, com os mesmos nomes dos externos.
                if (ParseKeyword(Keyword.TO))
                {
                    var value = ParseValue();
                    ExpectKeyword(Keyword.PERCENT);
                    threshold = value;
                }
                else
                {
                    threshold = null;
                }
                boost = ParseKeyword(Keyword.BOOST);
                tableName = parsedTableName;
                break;
            }
            default:
                tableName = null;
                threshold = null;
                boost = false;
                break;
        }
        return new Statement.Vacuum(new VacuumStatement(
            Full: full,
            SortOnly: sortOnly,
            DeleteOnly: deleteOnly,
            Reindex: reindex,
            Recluster: recluster,
            TableName: tableName,
            Threshold: threshold,
            Boost: boost));
    }

    /// <summary>Consume the parser and return its underlying token buffer</summary>
    // O `Vec<TokenWithSpan>` vira o próprio `List<TokenWithSpan>` interno (o Rust consome o parser).
    public List<TokenWithSpan> IntoTokens() => _tokens;

    /// <summary>Returns true if the next keyword indicates a sub query, i.e. SELECT or WITH</summary>
    internal bool PeekSubQuery() =>
        PeekOneOfKeywords(Keyword.SELECT, Keyword.WITH)
            is not null;

    internal ShowStatementOptions ParseShowStmtOptions()
    {
        ShowStatementIn? showIn;
        ShowStatementFilterPosition? filterPosition = null;
        if (_dialect.SupportsShowLikeBeforeIn)
        {
            if (ParseShowStatementFilter() is { } filter)
            {
                filterPosition = new ShowStatementFilterPosition.Infix(filter);
            }
            showIn = MaybeParseShowStmtIn();
        }
        else
        {
            showIn = MaybeParseShowStmtIn();
            if (ParseShowStatementFilter() is { } filter)
            {
                filterPosition = new ShowStatementFilterPosition.Suffix(filter);
            }
        }
        var startsWith = MaybeParseShowStmtStartsWith();
        var limit = MaybeParseShowStmtLimit();
        var @from = MaybeParseShowStmtFrom();
        return new ShowStatementOptions(
            FilterPosition: filterPosition,
            ShowIn: showIn,
            StartsWith: startsWith,
            Limit: limit,
            LimitFrom: @from);
    }

    internal ShowStatementIn? MaybeParseShowStmtIn()
    {
        ShowStatementInClause clause;
        switch (ParseOneOfKeywords(Keyword.FROM, Keyword.IN))
        {
            case Keyword.FROM:
                clause = new ShowStatementInClause.FROM();
                break;
            case Keyword.IN:
                clause = new ShowStatementInClause.IN();
                break;
            case null:
                return null;
            default:
                return ExpectedRef<ShowStatementIn?>("FROM or IN", PeekTokenRef());
        }

        ShowStatementInParentType? parentType;
        ObjectName? parentName;
        switch (ParseOneOfKeywords(
            Keyword.ACCOUNT,
            Keyword.DATABASE,
            Keyword.SCHEMA,
            Keyword.TABLE,
            Keyword.VIEW))
        {
            // If we see these next keywords it means we don't have a parent name
            // Rust: `|` sem curto-circuito; mantido.
            case Keyword.DATABASE
                when PeekKeywords(Keyword.STARTS, Keyword.WITH)
                    | PeekKeyword(Keyword.LIMIT):
                parentType = new ShowStatementInParentType.Database();
                parentName = null;
                break;
            case Keyword.SCHEMA
                when PeekKeywords(Keyword.STARTS, Keyword.WITH)
                    | PeekKeyword(Keyword.LIMIT):
                parentType = new ShowStatementInParentType.Schema();
                parentName = null;
                break;
            case { } parentKw:
            {
                // The parent name here is still optional, for example:
                // SHOW TABLES IN ACCOUNT, so parsing the object name
                // may fail because the statement ends.
                var optionalParentName = MaybeParse(p => p.ParseObjectName(false));
                switch (parentKw)
                {
                    case Keyword.ACCOUNT:
                        parentType = new ShowStatementInParentType.Account();
                        parentName = optionalParentName;
                        break;
                    case Keyword.DATABASE:
                        parentType = new ShowStatementInParentType.Database();
                        parentName = optionalParentName;
                        break;
                    case Keyword.SCHEMA:
                        parentType = new ShowStatementInParentType.Schema();
                        parentName = optionalParentName;
                        break;
                    case Keyword.TABLE:
                        parentType = new ShowStatementInParentType.Table();
                        parentName = optionalParentName;
                        break;
                    case Keyword.VIEW:
                        parentType = new ShowStatementInParentType.View();
                        parentName = optionalParentName;
                        break;
                    default:
                        return ExpectedRef<ShowStatementIn?>(
                            "one of ACCOUNT, DATABASE, SCHEMA, TABLE or VIEW",
                            PeekTokenRef());
                }
                break;
            }
            case null:
            {
                // Parsing MySQL style FROM tbl_name FROM db_name
                // which is equivalent to FROM tbl_name.db_name
                var mysqlParentName = ParseObjectName(false);
                if (ParseOneOfKeywords(Keyword.FROM, Keyword.IN)
                    is not null)
                {
                    // Rust: `parent_name.0.insert(0, ..)` sobre record imutável: nova lista + `with`.
                    var parts = new List<ObjectNamePart>(mysqlParentName.Value);
                    parts.Insert(0, new ObjectNamePart.Identifier(ParseIdentifier()));
                    mysqlParentName = mysqlParentName with { Value = parts.ToSequence() };
                }
                parentType = null;
                parentName = mysqlParentName;
                break;
            }
        }

        return new ShowStatementIn(
            Clause: clause,
            ParentType: parentType,
            ParentName: parentName);
    }

    internal ValueWithSpan? MaybeParseShowStmtStartsWith()
    {
        if (ParseKeywords(Keyword.STARTS, Keyword.WITH))
        {
            return ParseValue();
        }
        else
        {
            return null;
        }
    }

    internal Expr? MaybeParseShowStmtLimit()
    {
        if (ParseKeyword(Keyword.LIMIT))
        {
            return ParseLimit();
        }
        else
        {
            return null;
        }
    }

    internal ValueWithSpan? MaybeParseShowStmtFrom()
    {
        if (ParseKeyword(Keyword.FROM))
        {
            return ParseValue();
        }
        else
        {
            return null;
        }
    }

    internal bool InColumnDefinitionState() => _state is ParserState.ColumnDefinition;

    /// <summary>Parses options provided in key-value format.</summary>
    /// <param name="parenthesized">true if the options are enclosed in parenthesis</param>
    /// <param name="endWords">a list of keywords that any of them indicates the end of the options section</param>
    internal KeyValueOptions ParseKeyValueOptions(bool parenthesized, params Keyword[] endWords)
    {
        var options = new List<KeyValueOption>();
        var delimiter = KeyValueOptionsDelimiter.Space;
        if (parenthesized)
        {
            ExpectToken(new Token.LParen());
        }
        while (true)
        {
            // Rust: `break` do `loop` dentro do `match` usado como instrução → flag `stop` (decisão 1 da B4).
            var stop = false;
            switch (NextToken().Token)
            {
                case Token.RParen:
                    if (parenthesized)
                    {
                        stop = true;
                    }
                    else
                    {
                        return ExpectedRef<KeyValueOptions>(" another option or EOF", PeekTokenRef());
                    }
                    break;
                case Token.EOF:
                    stop = true;
                    break;
                case Token.SemiColon:
                    PrevToken();
                    stop = true;
                    break;
                case Token.Comma:
                    delimiter = KeyValueOptionsDelimiter.Comma;
                    continue;
                case Token.Word { Value: var w } when !endWords.Contains(w.Keyword):
                    options.Add(ParseKeyValueOption(w));
                    break;
                case Token.Word { Value: var w } when endWords.Contains(w.Keyword):
                    PrevToken();
                    stop = true;
                    break;
                default:
                    return ExpectedRef<KeyValueOptions>(
                        "another option, EOF, SemiColon, Comma or ')'",
                        PeekTokenRef());
            }
            if (stop)
            {
                break;
            }
        }

        return new KeyValueOptions(Delimiter: delimiter, Options: options.ToSequence());
    }

    /// <summary>Parses a <c>KEY = VALUE</c> construct based on the specified key</summary>
    internal KeyValueOption ParseKeyValueOption(Word key)
    {
        ExpectToken(new Token.Eq());
        var peekedToken = PeekToken();
        switch (peekedToken.Token)
        {
            case Token.SingleQuotedString:
                return new KeyValueOption(
                    OptionName: key.Value,
                    OptionValue: new KeyValueOptionKind.Single(ParseValue()));
            case Token.Word { Value: var word }
                when word.Keyword == Keyword.TRUE || word.Keyword == Keyword.FALSE:
                return new KeyValueOption(
                    OptionName: key.Value,
                    OptionValue: new KeyValueOptionKind.Single(ParseValue()));
            case Token.Number:
                return new KeyValueOption(
                    OptionName: key.Value,
                    OptionValue: new KeyValueOptionKind.Single(ParseValue()));
            case Token.Word { Value: var word }:
                NextToken();
                return new KeyValueOption(
                    OptionName: key.Value,
                    OptionValue: new KeyValueOptionKind.Single(
                        new Value.Placeholder(word.Value).WithSpan(peekedToken.Span)));
            case Token.LParen:
            {
                // Can be a list of values or a list of key value properties.
                // Try to parse a list of values and if that fails, try to parse
                // a list of key-value properties.
                var parsedValues = MaybeParseValue(parser =>
                {
                    parser.ExpectToken(new Token.LParen());
                    // Rust: `let values = parser.parse_comma_separated0(..);` guarda o `Result` sem `?`; o erro
                    // do `expect_token` seguinte tem precedência (decisão 4 da B1).
                    Sequence<ValueWithSpan> values = default;
                    ParserException? valuesError = null;
                    try
                    {
                        values = parser.ParseCommaSeparated0(p => p.ParseValue(), new Token.RParen());
                    }
                    catch (ParserException e)
                    {
                        valuesError = e;
                    }
                    parser.ExpectToken(new Token.RParen());
                    if (valuesError is not null)
                    {
                        throw StackGuard.Rethrow(valuesError);
                    }
                    return values;
                });
                switch (parsedValues)
                {
                    case { } values:
                        return new KeyValueOption(
                            OptionName: key.Value,
                            OptionValue: new KeyValueOptionKind.Multi(values));
                    case null:
                        return new KeyValueOption(
                            OptionName: key.Value,
                            OptionValue: new KeyValueOptionKind.KeyValueOptions(
                                ParseKeyValueOptions(true)));
                }
            }
            default:
                return ExpectedRef<KeyValueOption>("expected option value", PeekTokenRef());
        }
    }

    /// <summary>Parses a RESET statement</summary>
    internal ResetStatement ParseReset()
    {
        if (ParseKeyword(Keyword.ALL))
        {
            return new ResetStatement(Reset: new Reset.ALL());
        }

        if (ParseKeywords(Keyword.SESSION, Keyword.AUTHORIZATION))
        {
            return new ResetStatement(
                Reset: new Reset.SessionAuthorization());
        }

        var obj = ParseObjectName(false);
        return new ResetStatement(
            Reset: new Reset.ConfigurationParameter(obj));
    }

    // Rust: fim do bloco `impl Parser`. A função livre abaixo (mod.rs:21231, unidade B6) vira membro
    // `static` do tipo homônimo do módulo (convencoes.md §13, caso 5); `fn` privada → `internal` (caso 52).
    // O `impl Word` seguinte (21242–21268) está em Word.Parser.cs.

    internal static Expr MaybePrefixedExpr(Expr expr, Ident? prefix)
    {
        if (prefix is not null)
        {
            return new Expr.Prefixed(
                Prefix: prefix,
                Value: expr);
        }
        else
        {
            return expr;
        }
    }
}
