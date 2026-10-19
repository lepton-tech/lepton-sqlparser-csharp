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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 9343–10647:
// parse_plain_options … parse_alter_sort_key).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse plain options.</summary>
    public Sequence<SqlOption> ParsePlainOptions()
    {
        var options = new List<SqlOption>();

        while (ParsePlainOption() is { } option)
        {
            options.Add(option);
            // Some dialects support comma-separated options; it shouldn't introduce ambiguity to
            // consume it for all dialects.
            _ = ConsumeToken(new Token.Comma());
        }

        return options.ToSequence();
    }

    /// <summary>Parse optional inline comment.</summary>
    public CommentDef? ParseOptionalInlineComment()
    {
        CommentDef? comment;
        if (ParseKeyword(Keyword.COMMENT))
        {
            var hasEq = ConsumeToken(new Token.Eq());
            // Rust: `let comment` interno sombreia o externo (caso 72).
            var commentValue = ParseCommentValue();
            comment = hasEq
                ? new CommentDef.WithEq(commentValue)
                : new CommentDef.WithoutEq(commentValue);
        }
        else
        {
            comment = null;
        }
        return comment;
    }

    /// <summary>Parse comment value.</summary>
    public string ParseCommentValue()
    {
        var nextToken = NextToken();
        string value;
        switch (nextToken.Token)
        {
            case Token.SingleQuotedString { Value: var str }:
                value = str;
                break;
            case Token.DollarQuotedString { Value: var dollarStr }:
                value = dollarStr.Value;
                break;
            default:
                value = Expected<string>("string literal", nextToken);
                break;
        }
        return value;
    }

    /// <summary>Parse optional procedure parameters.</summary>
    public Sequence<ProcedureParam>? ParseOptionalProcedureParameters()
    {
        var @params = new List<ProcedureParam>();
        if (!ConsumeToken(new Token.LParen()) || ConsumeToken(new Token.RParen()))
        {
            return @params.ToSequence();
        }
        while (true)
        {
            if (PeekTokenRef().Token is Token.Word)
            {
                @params.Add(ParseProcedureParam());
            }
            var comma = ConsumeToken(new Token.Comma());
            if (ConsumeToken(new Token.RParen()))
            {
                // allow a trailing comma, even though it's not in standard
                break;
            }
            else if (!comma)
            {
                return ExpectedRef<Sequence<ProcedureParam>?>(
                    "',' or ')' after parameter definition",
                    PeekTokenRef());
            }
        }
        return @params.ToSequence();
    }

    /// <summary>Parse columns and constraints.</summary>
    public (Sequence<ColumnDef> Columns, Sequence<TableConstraint> Constraints) ParseColumns()
    {
        var columns = new List<ColumnDef>();
        var constraints = new List<TableConstraint>();
        if (!ConsumeToken(new Token.LParen()) || ConsumeToken(new Token.RParen()))
        {
            return (columns.ToSequence(), constraints.ToSequence());
        }

        while (true)
        {
            if (ParseOptionalTableConstraint() is { } constraint)
            {
                constraints.Add(constraint);
            }
            else if (PeekTokenRef().Token is Token.Word)
            {
                columns.Add(ParseColumnDef());
            }
            else if (PeekTokenRef().Token is Token.SingleQuotedString
                && _dialect.SupportsStringLiteralColumnNames)
            {
                columns.Add(ParseColumnDef());
            }
            else
            {
                return ExpectedRef<(Sequence<ColumnDef>, Sequence<TableConstraint>)>(
                    "column name or constraint definition",
                    PeekTokenRef());
            }

            var comma = ConsumeToken(new Token.Comma());
            var rparen = PeekTokenRef().Token is Token.RParen;

            if (!comma && !rparen)
            {
                return ExpectedRef<(Sequence<ColumnDef>, Sequence<TableConstraint>)>(
                    "',' or ')' after column definition", PeekTokenRef());
            }

            if (rparen
                && (!comma
                    || _dialect.SupportsColumnDefinitionTrailingCommas
                    || _options.TrailingCommas))
            {
                _ = ConsumeToken(new Token.RParen());
                break;
            }
        }

        return (columns.ToSequence(), constraints.ToSequence());
    }

    /// <summary>Parse procedure parameter.</summary>
    public ProcedureParam ParseProcedureParam()
    {
        ArgMode? mode;
        if (ParseKeyword(Keyword.IN))
        {
            mode = new ArgMode.In();
        }
        else if (ParseKeyword(Keyword.OUT))
        {
            mode = new ArgMode.Out();
        }
        else if (ParseKeyword(Keyword.INOUT))
        {
            mode = new ArgMode.InOut();
        }
        else
        {
            mode = null;
        }
        var name = ParseIdentifier();
        var dataType = ParseDataType();
        Expr? @default;
        if (ConsumeToken(new Token.Eq()))
        {
            @default = ParseExpr();
        }
        else
        {
            @default = null;
        }

        return new ProcedureParam(
            Name: name,
            DataType: dataType,
            Mode: mode,
            Default: @default);
    }

    /// <summary>Parse column definition.</summary>
    public ColumnDef ParseColumnDef() => ParseColumnDefInner(false);

    internal ColumnDef ParseColumnDefInner(bool optionalDataType)
    {
        var colName = ParseIdentifier();
        DataType dataType;
        if (IsColumnTypeSqliteUnspecified())
        {
            dataType = new DataType.Unspecified();
        }
        else if (optionalDataType)
        {
            dataType = MaybeParse(parser => parser.ParseDataType())
                ?? new DataType.Unspecified();
        }
        else
        {
            dataType = ParseDataType();
        }
        var options = new List<ColumnOptionDef>();
        while (true)
        {
            if (ParseKeyword(Keyword.CONSTRAINT))
            {
                Ident? name = ParseIdentifier();
                if (ParseOptionalColumnOption() is { } option)
                {
                    options.Add(new ColumnOptionDef(Name: name, Option: option));
                }
                else
                {
                    return ExpectedRef<ColumnDef>(
                        "constraint details after CONSTRAINT <name>",
                        PeekTokenRef());
                }
            }
            else if (ParseOptionalColumnOption() is { } unnamedOption)
            {
                options.Add(new ColumnOptionDef(Name: null, Option: unnamedOption));
            }
            else
            {
                break;
            }
        }
        return new ColumnDef(
            Name: colName,
            DataType: dataType,
            Options: options.ToSequence());
    }

    internal bool IsColumnTypeSqliteUnspecified()
    {
        if (_dialect is SQLiteDialect)
        {
            switch (PeekTokenRef().Token)
            {
                case Token.Word { Value: var word }:
                    return word.Keyword is Keyword.CONSTRAINT
                        or Keyword.PRIMARY
                        or Keyword.NOT
                        or Keyword.UNIQUE
                        or Keyword.CHECK
                        or Keyword.DEFAULT
                        or Keyword.COLLATE
                        or Keyword.REFERENCES
                        or Keyword.GENERATED
                        or Keyword.AS;
                default:
                    return true; // e.g. comma immediately after column name
            }
        }
        else
        {
            return false;
        }
    }

    /// <summary>Parse optional column option.</summary>
    public ColumnOption? ParseOptionalColumnOption()
    {
        // if let Some(option) = self.dialect.parse_column_option(self)? { return option; }
        // (Option<Result<Option<ColumnOption>>> → Option<ColumnOption?>, caso 55)
        var dialectOption = _dialect.ParseColumnOption(this);
        if (dialectOption.IsSome)
        {
            return dialectOption.Value;
        }

        return WithState<ColumnOption?>(
            ParserState.ColumnDefinition,
            parser => parser.ParseOptionalColumnOptionInner());
    }

    internal ColumnOption? ParseOptionalColumnOptionInner()
    {
        if (ParseKeywords(Keyword.CHARACTER, Keyword.SET))
        {
            return new ColumnOption.CharacterSet(
                ParseObjectName(false));
        }
        else if (ParseKeywords(Keyword.COLLATE))
        {
            return new ColumnOption.Collation(
                ParseObjectName(false));
        }
        else if (ParseKeywords(Keyword.NOT, Keyword.NULL))
        {
            return new ColumnOption.NotNull();
        }
        else if (ParseKeywords(Keyword.COMMENT))
        {
            return new ColumnOption.Comment(ParseCommentValue());
        }
        else if (ParseKeyword(Keyword.NULL))
        {
            return new ColumnOption.Null();
        }
        else if (ParseKeyword(Keyword.DEFAULT))
        {
            return new ColumnOption.Default(ParseExpr());
        }
        else if (_dialect is ClickHouseDialect or GenericDialect
            && ParseKeyword(Keyword.MATERIALIZED))
        {
            return new ColumnOption.Materialized(ParseExpr());
        }
        else if (_dialect is ClickHouseDialect or GenericDialect
            && ParseKeyword(Keyword.ALIAS))
        {
            return new ColumnOption.Alias(ParseExpr());
        }
        else if (_dialect is ClickHouseDialect or GenericDialect
            && ParseKeyword(Keyword.EPHEMERAL))
        {
            // The expression is optional for the EPHEMERAL syntax, so we need to check
            // if the column definition has remaining tokens before parsing the expression.
            if (PeekTokenRef().Token is Token.Comma or Token.RParen)
            {
                return new ColumnOption.Ephemeral(null);
            }
            else
            {
                return new ColumnOption.Ephemeral(ParseExpr());
            }
        }
        else if (ParseKeywords(Keyword.PRIMARY, Keyword.KEY))
        {
            var characteristics = ParseConstraintCharacteristics();
            return ColumnOption.From(
                new PrimaryKeyConstraint(
                    Name: null,
                    IndexName: null,
                    IndexType: null,
                    Columns: Sequence<IndexColumn>.Empty,
                    Include: Sequence<Ident>.Empty,
                    IndexOptions: Sequence<IndexOption>.Empty,
                    Characteristics: characteristics));
        }
        else if (ParseKeyword(Keyword.UNIQUE))
        {
            KeyOrIndexDisplay indexTypeDisplay;
            if (_dialect.SupportsKeyColumnOption && ParseKeyword(Keyword.KEY))
            {
                indexTypeDisplay = new KeyOrIndexDisplay.Key();
            }
            else
            {
                indexTypeDisplay = new KeyOrIndexDisplay.None();
            }
            var characteristics = ParseConstraintCharacteristics();
            return ColumnOption.From(
                new UniqueConstraint(
                    Name: null,
                    IndexName: null,
                    IndexTypeDisplay: indexTypeDisplay,
                    IndexType: null,
                    Columns: Sequence<IndexColumn>.Empty,
                    Include: Sequence<Ident>.Empty,
                    IndexOptions: Sequence<IndexOption>.Empty,
                    Characteristics: characteristics,
                    NullsDistinct: new NullsDistinctOption.None()));
        }
        else if (_dialect.SupportsKeyColumnOption && ParseKeyword(Keyword.KEY))
        {
            // In MySQL, `KEY` in a column definition is shorthand for `PRIMARY KEY`.
            // See: https://dev.mysql.com/doc/refman/8.4/en/create-table.html
            var characteristics = ParseConstraintCharacteristics();
            return ColumnOption.From(
                new PrimaryKeyConstraint(
                    Name: null,
                    IndexName: null,
                    IndexType: null,
                    Columns: Sequence<IndexColumn>.Empty,
                    Include: Sequence<Ident>.Empty,
                    IndexOptions: Sequence<IndexOption>.Empty,
                    Characteristics: characteristics));
        }
        else if (ParseKeyword(Keyword.REFERENCES))
        {
            var foreignTable = ParseObjectName(false);
            // PostgreSQL allows omitting the column list and
            // uses the primary key column of the foreign table by default
            var referredColumns = ParseParenthesizedColumnList(IsOptional.Optional, false);
            ConstraintReferenceMatchKind? matchKind = null;
            ReferentialAction? onDelete = null;
            ReferentialAction? onUpdate = null;
            while (true)
            {
                if (matchKind is null && ParseKeyword(Keyword.MATCH))
                {
                    matchKind = ParseMatchKind();
                }
                else if (onDelete is null
                    && ParseKeywords(Keyword.ON, Keyword.DELETE))
                {
                    onDelete = ParseReferentialAction();
                }
                else if (onUpdate is null
                    && ParseKeywords(Keyword.ON, Keyword.UPDATE))
                {
                    onUpdate = ParseReferentialAction();
                }
                else
                {
                    break;
                }
            }
            var characteristics = ParseConstraintCharacteristics();

            return ColumnOption.From(
                new ForeignKeyConstraint(
                    Name: null,                      // Column-level constraints don't have names
                    IndexName: null,                 // Not applicable for column-level constraints
                    Columns: Sequence<Ident>.Empty,  // Not applicable for column-level constraints
                    ForeignTable: foreignTable,
                    ReferredColumns: referredColumns,
                    OnDelete: onDelete,
                    OnUpdate: onUpdate,
                    MatchKind: matchKind,
                    Characteristics: characteristics));
        }
        else if (ParseKeyword(Keyword.CHECK))
        {
            ExpectToken(new Token.LParen());
            // since `CHECK` requires parentheses, we can parse the inner expression in ParserState::Normal
            Expr expr = WithState(ParserState.Normal, p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            var noInherit = ParseKeywords(Keyword.NO, Keyword.INHERIT);

            bool? enforced;
            if (ParseKeyword(Keyword.ENFORCED))
            {
                enforced = true;
            }
            else if (ParseKeywords(Keyword.NOT, Keyword.ENFORCED))
            {
                enforced = false;
            }
            else
            {
                enforced = null;
            }

            return ColumnOption.From(
                new CheckConstraint(
                    Name: null, // Column-level check constraints don't have names
                    Expr: expr,
                    NoInherit: noInherit,
                    Enforced: enforced));
        }
        else if (ParseKeyword(Keyword.AUTO_INCREMENT)
            && _dialect is MySqlDialect or GenericDialect)
        {
            // Support AUTO_INCREMENT for MySQL
            return new ColumnOption.DialectSpecific(Sequence.Of(
                Token.MakeKeyword("AUTO_INCREMENT")));
        }
        else if (ParseKeyword(Keyword.AUTOINCREMENT)
            && _dialect is SQLiteDialect or GenericDialect)
        {
            // Support AUTOINCREMENT for SQLite
            return new ColumnOption.DialectSpecific(Sequence.Of(
                Token.MakeKeyword("AUTOINCREMENT")));
        }
        else if (ParseKeyword(Keyword.ASC)
            && _dialect.SupportsAscDescInColumnDefinition)
        {
            // Support ASC for SQLite
            return new ColumnOption.DialectSpecific(Sequence.Of(
                Token.MakeKeyword("ASC")));
        }
        else if (ParseKeyword(Keyword.DESC)
            && _dialect.SupportsAscDescInColumnDefinition)
        {
            // Support DESC for SQLite
            return new ColumnOption.DialectSpecific(Sequence.Of(
                Token.MakeKeyword("DESC")));
        }
        else if (ParseKeywords(Keyword.ON, Keyword.UPDATE)
            && _dialect is MySqlDialect or GenericDialect)
        {
            var expr = ParseExpr();
            return new ColumnOption.OnUpdate(expr);
        }
        else if (ParseKeyword(Keyword.GENERATED))
        {
            return ParseOptionalColumnOptionGenerated();
        }
        else if (_dialect is BigQueryDialect or GenericDialect
            && ParseKeyword(Keyword.OPTIONS))
        {
            PrevToken();
            return new ColumnOption.Options(
                ParseOptions(Keyword.OPTIONS));
        }
        else if (ParseKeyword(Keyword.AS)
            && _dialect is MySqlDialect or SQLiteDialect or DuckDbDialect or GenericDialect)
        {
            return ParseOptionalColumnOptionAs();
        }
        else if (ParseKeyword(Keyword.SRID)
            && _dialect is MySqlDialect or GenericDialect)
        {
            return new ColumnOption.Srid(ParseExpr());
        }
        else if (ParseKeyword(Keyword.IDENTITY)
            && _dialect is MsSqlDialect or GenericDialect)
        {
            IdentityPropertyFormatKind? parameters;
            if (ConsumeToken(new Token.LParen()))
            {
                var seed = ParseNumber();
                ExpectToken(new Token.Comma());
                var increment = ParseNumber();
                ExpectToken(new Token.RParen());

                parameters = new IdentityPropertyFormatKind.FunctionCall(
                    new IdentityParameters(Seed: seed, Increment: increment));
            }
            else
            {
                parameters = null;
            }
            return new ColumnOption.Identity(
                new IdentityPropertyKind.Identity(new IdentityProperty(
                    Parameters: parameters,
                    Order: null)));
        }
        else if (_dialect is SQLiteDialect or GenericDialect
            && ParseKeywords(Keyword.ON, Keyword.CONFLICT))
        {
            // Support ON CONFLICT for SQLite
            return new ColumnOption.OnConflict(
                ExpectOneOfKeywords(
                    Keyword.ROLLBACK,
                    Keyword.ABORT,
                    Keyword.FAIL,
                    Keyword.IGNORE,
                    Keyword.REPLACE));
        }
        else if (ParseKeyword(Keyword.INVISIBLE))
        {
            return new ColumnOption.Invisible();
        }
        else
        {
            return null;
        }
    }

    internal Tag ParseTag()
    {
        var name = ParseObjectName(false);
        ExpectToken(new Token.Eq());
        var value = ParseLiteralString();

        return new Tag(name, value);
    }

    internal ColumnOption? ParseOptionalColumnOptionGenerated()
    {
        if (ParseKeywords(Keyword.ALWAYS, Keyword.AS, Keyword.IDENTITY))
        {
            var sequenceOptions = Sequence<SequenceOptions>.Empty;
            // Rust: `if self.expect_token(&Token::LParen).is_ok()`. Equivalente exato sem exceção (caso 81):
            // `expect_token(..).is_ok()` é `consume_token(..)`, que só avança quando o token casa; o erro
            // descartado não tem efeito colateral. Idem nos dois ramos seguintes.
            if (ConsumeToken(new Token.LParen()))
            {
                sequenceOptions = ParseCreateSequenceOptions();
                ExpectToken(new Token.RParen());
            }
            return new ColumnOption.Generated(
                GeneratedAs: GeneratedAs.Always,
                SequenceOptions: sequenceOptions,
                GenerationExpr: null,
                GenerationExprMode: null,
                GeneratedKeyword: true);
        }
        else if (ParseKeywords(
            Keyword.BY,
            Keyword.DEFAULT,
            Keyword.AS,
            Keyword.IDENTITY))
        {
            var sequenceOptions = Sequence<SequenceOptions>.Empty;
            // Rust: `if self.expect_token(&Token::LParen).is_ok()` (caso 81).
            if (ConsumeToken(new Token.LParen()))
            {
                sequenceOptions = ParseCreateSequenceOptions();
                ExpectToken(new Token.RParen());
            }
            return new ColumnOption.Generated(
                GeneratedAs: GeneratedAs.ByDefault,
                SequenceOptions: sequenceOptions,
                GenerationExpr: null,
                GenerationExprMode: null,
                GeneratedKeyword: true);
        }
        else if (ParseKeywords(Keyword.ALWAYS, Keyword.AS))
        {
            // Rust: `if self.expect_token(&Token::LParen).is_ok()` (caso 81).
            if (ConsumeToken(new Token.LParen()))
            {
                Expr expr = WithState(ParserState.Normal, p => p.ParseExpr());
                ExpectToken(new Token.RParen());
                GeneratedAs genAs;
                GeneratedExpressionMode? exprMode;
                if (ParseKeywords(Keyword.STORED))
                {
                    (genAs, exprMode) = (
                        GeneratedAs.ExpStored,
                        GeneratedExpressionMode.Stored);
                }
                else if (_dialect is PostgreSqlDialect)
                {
                    // Postgres' AS IDENTITY branches are above, this one needs STORED
                    return ExpectedRef<ColumnOption?>("STORED", PeekTokenRef());
                }
                else if (ParseKeywords(Keyword.VIRTUAL))
                {
                    (genAs, exprMode) = (GeneratedAs.Always, GeneratedExpressionMode.Virtual);
                }
                else
                {
                    (genAs, exprMode) = (GeneratedAs.Always, null);
                }

                return new ColumnOption.Generated(
                    GeneratedAs: genAs,
                    SequenceOptions: null,
                    GenerationExpr: expr,
                    GenerationExprMode: exprMode,
                    GeneratedKeyword: true);
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

    internal ColumnOption? ParseOptionalColumnOptionAs()
    {
        // Some DBs allow 'AS (expr)', shorthand for GENERATED ALWAYS AS
        ExpectToken(new Token.LParen());
        var expr = ParseExpr();
        ExpectToken(new Token.RParen());

        GeneratedAs genAs;
        GeneratedExpressionMode? exprMode;
        if (ParseKeywords(Keyword.STORED))
        {
            (genAs, exprMode) = (
                GeneratedAs.ExpStored,
                GeneratedExpressionMode.Stored);
        }
        else if (ParseKeywords(Keyword.VIRTUAL))
        {
            (genAs, exprMode) = (GeneratedAs.Always, GeneratedExpressionMode.Virtual);
        }
        else
        {
            (genAs, exprMode) = (GeneratedAs.Always, null);
        }

        return new ColumnOption.Generated(
            GeneratedAs: genAs,
            SequenceOptions: null,
            GenerationExpr: expr,
            GenerationExprMode: exprMode,
            GeneratedKeyword: false);
    }

    /// <summary>Parse optional <c>CLUSTERED BY</c> clause for Hive/Generic dialects.</summary>
    public ClusteredBy? ParseOptionalClusteredBy()
    {
        ClusteredBy? clusteredBy;
        if (_dialect is HiveDialect or GenericDialect
            && ParseKeywords(Keyword.CLUSTERED, Keyword.BY))
        {
            var columns = ParseParenthesizedColumnList(IsOptional.Mandatory, false);

            Sequence<OrderByExpr>? sortedBy;
            if (ParseKeywords(Keyword.SORTED, Keyword.BY))
            {
                ExpectToken(new Token.LParen());
                var sortedByColumns = ParseCommaSeparated(p => p.ParseOrderByExpr());
                ExpectToken(new Token.RParen());
                sortedBy = sortedByColumns;
            }
            else
            {
                sortedBy = null;
            }

            ExpectKeywordIs(Keyword.INTO);
            var numBuckets = ParseNumberValue().Value;
            ExpectKeywordIs(Keyword.BUCKETS);
            clusteredBy = new ClusteredBy(
                Columns: columns,
                SortedBy: sortedBy,
                NumBuckets: numBuckets);
        }
        else
        {
            clusteredBy = null;
        }
        return clusteredBy;
    }

    /// <summary>Parse a referential action used in foreign key clauses.</summary>
    /// <remarks>Recognized forms: <c>RESTRICT</c>, <c>CASCADE</c>, <c>SET NULL</c>, <c>NO ACTION</c>, <c>SET DEFAULT</c>.</remarks>
    public ReferentialAction ParseReferentialAction()
    {
        if (ParseKeyword(Keyword.RESTRICT))
        {
            return new ReferentialAction.Restrict();
        }
        else if (ParseKeyword(Keyword.CASCADE))
        {
            return new ReferentialAction.Cascade();
        }
        else if (ParseKeywords(Keyword.SET, Keyword.NULL))
        {
            return new ReferentialAction.SetNull();
        }
        else if (ParseKeywords(Keyword.NO, Keyword.ACTION))
        {
            return new ReferentialAction.NoAction();
        }
        else if (ParseKeywords(Keyword.SET, Keyword.DEFAULT))
        {
            return new ReferentialAction.SetDefault();
        }
        else
        {
            return ExpectedRef<ReferentialAction>(
                "one of RESTRICT, CASCADE, SET NULL, NO ACTION or SET DEFAULT",
                PeekTokenRef());
        }
    }

    /// <summary>Parse a <c>MATCH</c> kind for constraint references: <c>FULL</c>, <c>PARTIAL</c>, or <c>SIMPLE</c>.</summary>
    public ConstraintReferenceMatchKind ParseMatchKind()
    {
        if (ParseKeyword(Keyword.FULL))
        {
            return new ConstraintReferenceMatchKind.Full();
        }
        else if (ParseKeyword(Keyword.PARTIAL))
        {
            return new ConstraintReferenceMatchKind.Partial();
        }
        else if (ParseKeyword(Keyword.SIMPLE))
        {
            return new ConstraintReferenceMatchKind.Simple();
        }
        else
        {
            return ExpectedRef<ConstraintReferenceMatchKind>("one of FULL, PARTIAL or SIMPLE", PeekTokenRef());
        }
    }

    /// <summary>
    /// Parse <c>index_name [ DEFERRABLE | NOT DEFERRABLE ] [ INITIALLY DEFERRED | INITIALLY IMMEDIATE ]</c>
    /// after <c>{ PRIMARY KEY | UNIQUE } USING INDEX</c>.
    /// </summary>
    internal ConstraintUsingIndex ParseConstraintUsingIndex(Ident? name)
    {
        var indexName = ParseIdentifier();
        var characteristics = ParseConstraintCharacteristics();
        return new ConstraintUsingIndex(
            Name: name,
            IndexName: indexName,
            Characteristics: characteristics);
    }

    /// <summary>Parse optional constraint characteristics such as <c>DEFERRABLE</c>, <c>INITIALLY</c> and <c>ENFORCED</c>.</summary>
    public ConstraintCharacteristics? ParseConstraintCharacteristics()
    {
        // `let mut cc = ConstraintCharacteristics::default();` + atribuição de campos: o record é
        // imutável, então cada `cc.campo = v` vira `cc = cc with { Campo = v }` (decisão 6 da B2).
        var cc = ConstraintCharacteristics.Default();

        while (true)
        {
            if (cc.Deferrable is null && ParseKeywords(Keyword.NOT, Keyword.DEFERRABLE))
            {
                cc = cc with { Deferrable = false };
            }
            else if (cc.Deferrable is null && ParseKeyword(Keyword.DEFERRABLE))
            {
                cc = cc with { Deferrable = true };
            }
            else if (cc.Initially is null && ParseKeyword(Keyword.INITIALLY))
            {
                if (ParseKeyword(Keyword.DEFERRED))
                {
                    cc = cc with { Initially = DeferrableInitial.Deferred };
                }
                else if (ParseKeyword(Keyword.IMMEDIATE))
                {
                    cc = cc with { Initially = DeferrableInitial.Immediate };
                }
                else
                {
                    ExpectedRef("one of DEFERRED or IMMEDIATE", PeekTokenRef());
                }
            }
            else if (cc.Enforced is null && ParseKeyword(Keyword.ENFORCED))
            {
                cc = cc with { Enforced = true };
            }
            else if (cc.Enforced is null
                && ParseKeywords(Keyword.NOT, Keyword.ENFORCED))
            {
                cc = cc with { Enforced = false };
            }
            else
            {
                break;
            }
        }

        if (cc.Deferrable is not null || cc.Initially is not null || cc.Enforced is not null)
        {
            return cc;
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse an optional table constraint (e.g. <c>PRIMARY KEY</c>, <c>UNIQUE</c>, <c>FOREIGN KEY</c>, <c>CHECK</c>).</summary>
    public TableConstraint? ParseOptionalTableConstraint()
    {
        Ident? name;
        if (ParseKeyword(Keyword.CONSTRAINT))
        {
            if (_dialect.SupportsConstraintKeywordWithoutName
                && PeekOneOfKeywords(
                        Keyword.CHECK,
                        Keyword.PRIMARY,
                        Keyword.UNIQUE,
                        Keyword.FOREIGN)
                    is not null)
            {
                name = null;
            }
            else
            {
                name = ParseIdentifier();
            }
        }
        else
        {
            name = null;
        }

        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w } when w.Keyword == Keyword.UNIQUE:
            {
                // PostgreSQL: UNIQUE USING INDEX index_name
                // https://www.postgresql.org/docs/current/sql-altertable.html
                if (ParseKeywords(Keyword.USING, Keyword.INDEX))
                {
                    return new TableConstraint.UniqueUsingIndex(
                        ParseConstraintUsingIndex(name));
                }

                var indexTypeDisplay = ParseIndexTypeDisplay();
                if (!(_dialect is GenericDialect or MySqlDialect)
                    && !indexTypeDisplay.IsNone())
                {
                    return ExpectedRef<TableConstraint?>(
                        "`index_name` or `(column_name [, ...])`",
                        PeekTokenRef());
                }

                var nullsDistinct = ParseOptionalNullsDistinct();

                // optional index name
                var indexName = ParseOptionalIdent();
                var indexType = ParseOptionalUsingThenIndexType();

                var columns = ParseParenthesizedIndexColumnList();
                var include = ParseOptionalIncludeColumns();
                var indexOptions = ParseIndexOptions();
                var characteristics = ParseConstraintCharacteristics();
                return TableConstraint.From(
                    new UniqueConstraint(
                        Name: name,
                        IndexName: indexName,
                        IndexTypeDisplay: indexTypeDisplay,
                        IndexType: indexType,
                        Columns: columns,
                        Include: include,
                        IndexOptions: indexOptions,
                        Characteristics: characteristics,
                        NullsDistinct: nullsDistinct));
            }
            case Token.Word { Value: var w } when w.Keyword == Keyword.PRIMARY:
            {
                // after `PRIMARY` always stay `KEY`
                ExpectKeywordIs(Keyword.KEY);

                // PostgreSQL: PRIMARY KEY USING INDEX index_name
                // https://www.postgresql.org/docs/current/sql-altertable.html
                if (ParseKeywords(Keyword.USING, Keyword.INDEX))
                {
                    return new TableConstraint.PrimaryKeyUsingIndex(
                        ParseConstraintUsingIndex(name));
                }

                // optional index name
                var indexName = ParseOptionalIdent();
                var indexType = ParseOptionalUsingThenIndexType();

                var columns = ParseParenthesizedIndexColumnList();
                var include = ParseOptionalIncludeColumns();
                var indexOptions = ParseIndexOptions();
                var characteristics = ParseConstraintCharacteristics();
                return TableConstraint.From(
                    new PrimaryKeyConstraint(
                        Name: name,
                        IndexName: indexName,
                        IndexType: indexType,
                        Columns: columns,
                        Include: include,
                        IndexOptions: indexOptions,
                        Characteristics: characteristics));
            }
            case Token.Word { Value: var w } when w.Keyword == Keyword.FOREIGN:
            {
                ExpectKeywordIs(Keyword.KEY);
                var indexName = ParseOptionalIdent();
                var columns = ParseParenthesizedColumnList(IsOptional.Mandatory, false);
                ExpectKeywordIs(Keyword.REFERENCES);
                var foreignTable = ParseObjectName(false);
                var referredColumns = ParseParenthesizedColumnList(IsOptional.Optional, false);
                ConstraintReferenceMatchKind? matchKind = null;
                ReferentialAction? onDelete = null;
                ReferentialAction? onUpdate = null;
                while (true)
                {
                    if (matchKind is null && ParseKeyword(Keyword.MATCH))
                    {
                        matchKind = ParseMatchKind();
                    }
                    else if (onDelete is null
                        && ParseKeywords(Keyword.ON, Keyword.DELETE))
                    {
                        onDelete = ParseReferentialAction();
                    }
                    else if (onUpdate is null
                        && ParseKeywords(Keyword.ON, Keyword.UPDATE))
                    {
                        onUpdate = ParseReferentialAction();
                    }
                    else
                    {
                        break;
                    }
                }

                var characteristics = ParseConstraintCharacteristics();

                return TableConstraint.From(
                    new ForeignKeyConstraint(
                        Name: name,
                        IndexName: indexName,
                        Columns: columns,
                        ForeignTable: foreignTable,
                        ReferredColumns: referredColumns,
                        OnDelete: onDelete,
                        OnUpdate: onUpdate,
                        MatchKind: matchKind,
                        Characteristics: characteristics));
            }
            case Token.Word { Value: var w } when w.Keyword == Keyword.CHECK:
            {
                ExpectToken(new Token.LParen());
                var expr = ParseExpr();
                ExpectToken(new Token.RParen());
                var noInherit = ParseKeywords(Keyword.NO, Keyword.INHERIT);

                bool? enforced;
                if (ParseKeyword(Keyword.ENFORCED))
                {
                    enforced = true;
                }
                else if (ParseKeywords(Keyword.NOT, Keyword.ENFORCED))
                {
                    enforced = false;
                }
                else
                {
                    enforced = null;
                }

                return TableConstraint.From(
                    new CheckConstraint(
                        Name: name,
                        Expr: expr,
                        NoInherit: noInherit,
                        Enforced: enforced));
            }
            case Token.Word { Value: var w }
                when (w.Keyword == Keyword.INDEX || w.Keyword == Keyword.KEY)
                    && _dialect is GenericDialect or MySqlDialect
                    && name is null:
            {
                var displayAsKey = w.Keyword == Keyword.KEY;

                // Rust: `let name` sombreia o `name` externo (caso 72).
                Ident? indexConstraintName;
                switch (PeekTokenRef().Token)
                {
                    case Token.Word { Value: var word } when word.Keyword == Keyword.USING:
                        indexConstraintName = null;
                        break;
                    default:
                        indexConstraintName = ParseOptionalIdent();
                        break;
                }

                var indexType = ParseOptionalUsingThenIndexType();
                var columns = ParseParenthesizedIndexColumnList();
                var indexOptions = ParseIndexOptions();

                return TableConstraint.From(
                    new IndexConstraint(
                        DisplayAsKey: displayAsKey,
                        Name: indexConstraintName,
                        IndexType: indexType,
                        Columns: columns,
                        IndexOptions: indexOptions));
            }
            case Token.Word { Value: var w }
                when (w.Keyword == Keyword.FULLTEXT || w.Keyword == Keyword.SPATIAL)
                    && _dialect is GenericDialect or MySqlDialect:
            {
                if (name is { } constraintName)
                {
                    return Expected<TableConstraint?>(
                        "FULLTEXT or SPATIAL option without constraint name",
                        new TokenWithSpan(
                            Token: Token.MakeKeyword(constraintName.ToString()),
                            Span: nextToken.Span));
                }

                var fulltext = w.Keyword == Keyword.FULLTEXT;

                var indexTypeDisplay = ParseIndexTypeDisplay();

                var optIndexName = ParseOptionalIdent();

                var columns = ParseParenthesizedIndexColumnList();

                return TableConstraint.From(
                    new FullTextOrSpatialConstraint(
                        Fulltext: fulltext,
                        IndexTypeDisplay: indexTypeDisplay,
                        OptIndexName: optIndexName,
                        Columns: columns));
            }
            case Token.Word { Value: var w }
                when w.Keyword == Keyword.EXCLUDE && _dialect.SupportsExcludeConstraint:
            {
                // `EXCLUDE` is a non-reserved keyword in PostgreSQL, so it is a
                // valid column name. Only treat it as an exclusion constraint
                // when it begins one (named, or followed by `USING` / `(`);
                // otherwise backtrack and let it parse as a column.
                if (name is not null
                    || PeekKeyword(Keyword.USING)
                    || PeekTokenRef().Token is Token.LParen)
                {
                    return TableConstraint.From(ParseExcludeConstraint(name));
                }
                else
                {
                    PrevToken();
                    return null;
                }
            }
            default:
            {
                if (name is not null)
                {
                    return Expected<TableConstraint?>("PRIMARY, UNIQUE, FOREIGN, CHECK, or EXCLUDE", nextToken);
                }
                else
                {
                    PrevToken();
                    return null;
                }
            }
        }
    }

    // The leading `EXCLUDE` keyword is already consumed by the caller.
    internal ExcludeConstraint ParseExcludeConstraint(Ident? name)
    {
        Ident? indexMethod;
        if (ParseKeyword(Keyword.USING))
        {
            indexMethod = ParseIdentifier();
        }
        else
        {
            indexMethod = null;
        }

        ExpectToken(new Token.LParen());
        var elements = ParseCommaSeparated(p => p.ParseExcludeConstraintElement());
        ExpectToken(new Token.RParen());

        Sequence<Ident> include;
        if (ParseKeyword(Keyword.INCLUDE))
        {
            ExpectToken(new Token.LParen());
            var cols = ParseCommaSeparated(p => p.ParseIdentifier());
            ExpectToken(new Token.RParen());
            include = cols;
        }
        else
        {
            include = Sequence<Ident>.Empty;
        }

        Expr? whereClause;
        if (ParseKeyword(Keyword.WHERE))
        {
            ExpectToken(new Token.LParen());
            var predicate = ParseExpr();
            ExpectToken(new Token.RParen());
            whereClause = predicate;
        }
        else
        {
            whereClause = null;
        }

        var characteristics = ParseConstraintCharacteristics();

        return new ExcludeConstraint(
            Name: name,
            IndexMethod: indexMethod,
            Elements: elements,
            Include: include,
            WhereClause: whereClause,
            Characteristics: characteristics);
    }

    internal ExcludeConstraintElement ParseExcludeConstraintElement()
    {
        var column = ParseCreateIndexExpr();
        ExpectKeywordIs(Keyword.WITH);
        var @operator = ParseExcludeConstraintOperator();
        return new ExcludeConstraintElement(Column: column, Operator: @operator);
    }

    /// <summary>Parse the operator that follows <c>WITH</c> in an <c>EXCLUDE</c> element.</summary>
    internal ExcludeConstraintOperator ParseExcludeConstraintOperator()
    {
        if (ParseKeyword(Keyword.OPERATOR))
        {
            return new ExcludeConstraintOperator.PGOperator(
                ParsePgOperatorIdentParts());
        }

        var operatorToken = NextToken();
        return new ExcludeConstraintOperator.Token(
            operatorToken.Token.ToString());
    }

    /// <summary>
    /// Parse the body of a Postgres <c>OPERATOR(schema.op)</c> form: the
    /// parenthesized <c>.</c>-separated path of name parts after the <c>OPERATOR</c>
    /// keyword. Shared between binary expression parsing and exclusion
    /// constraint parsing.
    /// </summary>
    internal Sequence<string> ParsePgOperatorIdentParts()
    {
        ExpectToken(new Token.LParen());
        if (PeekTokenRef().Token is Token.RParen)
        {
            var token = NextToken();
            return Expected<Sequence<string>>("operator name", token);
        }
        var idents = new List<string>();
        while (true)
        {
            AdvanceToken();
            idents.Add(GetCurrentToken().ToString());
            if (!ConsumeToken(new Token.Period()))
            {
                break;
            }
        }
        ExpectToken(new Token.RParen());
        return idents.ToSequence();
    }

    internal NullsDistinctOption ParseOptionalNullsDistinct()
    {
        if (ParseKeyword(Keyword.NULLS))
        {
            var @not = ParseKeyword(Keyword.NOT);
            ExpectKeywordIs(Keyword.DISTINCT);
            if (@not)
            {
                return new NullsDistinctOption.NotDistinct();
            }
            else
            {
                return new NullsDistinctOption.Distinct();
            }
        }
        else
        {
            return new NullsDistinctOption.None();
        }
    }

    /// <summary>Optionally parse a parenthesized list of <c>SqlOption</c>s introduced by <c>keyword</c>.</summary>
    public Sequence<SqlOption>? MaybeParseOptions(Keyword keyword)
    {
        if (PeekTokenRef().Token is Token.Word { Value: var word })
        {
            if (word.Keyword == keyword)
            {
                return ParseOptions(keyword);
            }
        }
        return null;
    }

    /// <summary>Parse a parenthesized list of <c>SqlOption</c>s following <c>keyword</c>, or return an empty vec.</summary>
    public Sequence<SqlOption> ParseOptions(Keyword keyword)
    {
        if (ParseKeyword(keyword))
        {
            ExpectToken(new Token.LParen());
            var options = ParseCommaSeparated0(p => p.ParseSqlOption(), new Token.RParen());
            ExpectToken(new Token.RParen());
            return options;
        }
        else
        {
            return Sequence<SqlOption>.Empty;
        }
    }

    /// <summary>Parse options introduced by one of <c>keywords</c> followed by a parenthesized list.</summary>
    public Sequence<SqlOption> ParseOptionsWithKeywords(params Keyword[] keywords)
    {
        if (ParseKeywords(keywords))
        {
            ExpectToken(new Token.LParen());
            var options = ParseCommaSeparated(p => p.ParseSqlOption());
            ExpectToken(new Token.RParen());
            return options;
        }
        else
        {
            return Sequence<SqlOption>.Empty;
        }
    }

    /// <summary>Parse an index type token (e.g. <c>BTREE</c>, <c>HASH</c>, or a custom identifier).</summary>
    public IndexType ParseIndexType()
    {
        if (ParseKeyword(Keyword.BTREE))
        {
            return new IndexType.BTree();
        }
        else if (ParseKeyword(Keyword.HASH))
        {
            return new IndexType.Hash();
        }
        else if (ParseKeyword(Keyword.GIN))
        {
            return new IndexType.GIN();
        }
        else if (ParseKeyword(Keyword.GIST))
        {
            return new IndexType.GiST();
        }
        else if (ParseKeyword(Keyword.SPGIST))
        {
            return new IndexType.SPGiST();
        }
        else if (ParseKeyword(Keyword.BRIN))
        {
            return new IndexType.BRIN();
        }
        else if (ParseKeyword(Keyword.BLOOM))
        {
            return new IndexType.Bloom();
        }
        else
        {
            return new IndexType.Custom(ParseIdentifier());
        }
    }

    /// <summary>
    /// Optionally parse the <c>USING</c> keyword, followed by an <see cref="IndexType"/>
    /// Example:
    /// <code>
    /// USING BTREE (name, age DESC)
    /// </code>
    /// Optionally parse <c>USING &lt;index_type&gt;</c> and return the parsed <c>IndexType</c> if present.
    /// </summary>
    public IndexType? ParseOptionalUsingThenIndexType()
    {
        if (ParseKeyword(Keyword.USING))
        {
            return ParseIndexType();
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Parse <c>[ident]</c>, mostly <c>ident</c> is name, like:
    /// <c>window_name</c>, <c>index_name</c>, ...
    /// Parse an optional identifier, returning <c>Some(Ident)</c> if present.
    /// </summary>
    public Ident? ParseOptionalIdent() => MaybeParse(parser => parser.ParseIdentifier());

    // #[must_use]: sem equivalente obrigatório no C# (o resultado é sempre usado pelos chamadores).
    /// <summary>Parse optional <c>KEY</c> or <c>INDEX</c> display tokens used in index/constraint declarations.</summary>
    public KeyOrIndexDisplay ParseIndexTypeDisplay()
    {
        if (ParseKeyword(Keyword.KEY))
        {
            return new KeyOrIndexDisplay.Key();
        }
        else if (ParseKeyword(Keyword.INDEX))
        {
            return new KeyOrIndexDisplay.Index();
        }
        else
        {
            return new KeyOrIndexDisplay.None();
        }
    }

    /// <summary>Parse an optional index option such as <c>USING &lt;type&gt;</c> or <c>COMMENT &lt;string&gt;</c>.</summary>
    public IndexOption? ParseOptionalIndexOption()
    {
        if (ParseOptionalUsingThenIndexType() is { } indexType)
        {
            return new IndexOption.Using(indexType);
        }
        else if (ParseKeyword(Keyword.COMMENT))
        {
            var s = ParseLiteralString();
            return new IndexOption.Comment(s);
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse zero or more index options and return them as a vector.</summary>
    public Sequence<IndexOption> ParseIndexOptions()
    {
        var options = new List<IndexOption>();

        while (true)
        {
            switch (ParseOptionalIndexOption())
            {
                case { } indexOption:
                    options.Add(indexOption);
                    break;
                case null:
                    return options.ToSequence();
            }
        }
    }

    /// <summary>Parse an optional <c>INCLUDE (col, ...)</c> clause on a table constraint.</summary>
    public Sequence<Ident> ParseOptionalIncludeColumns()
    {
        if (ParseKeyword(Keyword.INCLUDE))
        {
            ExpectToken(new Token.LParen());
            var columns = ParseCommaSeparated(p => p.ParseIdentifier());
            ExpectToken(new Token.RParen());
            return columns;
        }
        else
        {
            return Sequence<Ident>.Empty;
        }
    }

    /// <summary>Parse a single <c>SqlOption</c> used by various dialect-specific DDL statements.</summary>
    public SqlOption ParseSqlOption()
    {
        var isMssql = _dialect is MsSqlDialect or GenericDialect;

        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var w } when w.Keyword == Keyword.HEAP && isMssql:
                return new SqlOption.Ident(ParseIdentifier());
            case Token.Word { Value: var w } when w.Keyword == Keyword.PARTITION && isMssql:
                return ParseOptionPartition();
            case Token.Word { Value: var w } when w.Keyword == Keyword.CLUSTERED && isMssql:
                return ParseOptionClustered();
            default:
            {
                var name = ParseIdentifier();
                ExpectToken(new Token.Eq());
                var value = ParseExpr();

                return new SqlOption.KeyValue(Key: name, Value: value);
            }
        }
    }

    /// <summary>Parse a <c>CLUSTERED</c> table option (MSSQL-specific syntaxes supported).</summary>
    public SqlOption ParseOptionClustered()
    {
        if (ParseKeywords(
            Keyword.CLUSTERED,
            Keyword.COLUMNSTORE,
            Keyword.INDEX,
            Keyword.ORDER))
        {
            return new SqlOption.Clustered(
                new TableOptionsClustered.ColumnstoreIndexOrder(
                    ParseParenthesizedColumnList(IsOptional.Mandatory, false)));
        }
        else if (ParseKeywords(Keyword.CLUSTERED, Keyword.COLUMNSTORE, Keyword.INDEX))
        {
            return new SqlOption.Clustered(
                new TableOptionsClustered.ColumnstoreIndex());
        }
        else if (ParseKeywords(Keyword.CLUSTERED, Keyword.INDEX))
        {
            ExpectToken(new Token.LParen());

            var columns = ParseCommaSeparated(p =>
            {
                var name = p.ParseIdentifier();
                var asc = p.ParseAscDesc();

                return new ClusteredIndex(Name: name, Asc: asc);
            });

            ExpectToken(new Token.RParen());

            return new SqlOption.Clustered(new TableOptionsClustered.Index(columns));
        }
        else
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                "invalid CLUSTERED sequence"));
        }
    }

    /// <summary>Parse a <c>PARTITION(...) FOR VALUES(...)</c> table option.</summary>
    public SqlOption ParseOptionPartition()
    {
        ExpectKeywordIs(Keyword.PARTITION);
        ExpectToken(new Token.LParen());
        var columnName = ParseIdentifier();

        ExpectKeywordIs(Keyword.RANGE);
        PartitionRangeDirection? rangeDirection;
        if (ParseKeyword(Keyword.LEFT))
        {
            rangeDirection = PartitionRangeDirection.Left;
        }
        else if (ParseKeyword(Keyword.RIGHT))
        {
            rangeDirection = PartitionRangeDirection.Right;
        }
        else
        {
            rangeDirection = null;
        }

        ExpectKeywords(Keyword.FOR, Keyword.VALUES);
        ExpectToken(new Token.LParen());

        var forValues = ParseCommaSeparated(p => p.ParseExpr());

        ExpectToken(new Token.RParen());
        ExpectToken(new Token.RParen());

        return new SqlOption.Partition(
            ColumnName: columnName,
            RangeDirection: rangeDirection,
            ForValues: forValues);
    }

    /// <summary>Parse a parenthesized list of partition expressions and return a <c>Partition</c> value.</summary>
    public Partition ParsePartition()
    {
        ExpectToken(new Token.LParen());
        var partitions = ParseCommaSeparated(p => p.ParseExpr());
        ExpectToken(new Token.RParen());
        return new Partition.Partitions(partitions);
    }

    /// <summary>Parse a parenthesized <c>SELECT</c> projection used for projection-based operations.</summary>
    public ProjectionSelect ParseProjectionSelect()
    {
        ExpectToken(new Token.LParen());
        ExpectKeywordIs(Keyword.SELECT);
        var projection = ParseProjection();
        var groupBy = ParseOptionalGroupBy();
        var orderBy = ParseOptionalOrderBy();
        ExpectToken(new Token.RParen());
        return new ProjectionSelect(
            Projection: projection,
            GroupBy: groupBy,
            OrderBy: orderBy);
    }

    /// <summary>Parse <c>ALTER TABLE ... ADD PROJECTION ...</c> operation.</summary>
    public AlterTableOperation ParseAlterTableAddProjection()
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = ParseIdentifier();
        var query = ParseProjectionSelect();
        return new AlterTableOperation.AddProjection(
            IfNotExists: ifNotExists,
            Name: name,
            Select: query);
    }

    /// <summary>Parse Redshift <c>ALTER SORTKEY (column_list)</c>.</summary>
    /// <remarks>See <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_ALTER_TABLE.html"/></remarks>
    internal AlterTableOperation ParseAlterSortKey()
    {
        ExpectKeywordIs(Keyword.ALTER);
        ExpectKeywordIs(Keyword.SORTKEY);
        ExpectToken(new Token.LParen());
        var columns = ParseCommaSeparated(p => p.ParseExpr());
        ExpectToken(new Token.RParen());
        return new AlterTableOperation.AlterSortKey(Columns: columns);
    }

    /// <summary>
    /// Parse the expression(s) following ClickHouse <c>ORDER BY</c>: either a single
    /// expression or a possibly empty parenthesized list.
    /// </summary>
    internal OneOrManyWithParens<Expr> ParseSortingKey()
    {
        if (ConsumeToken(new Token.LParen()))
        {
            Sequence<Expr> columns;
            if (PeekTokenRef().Token is not Token.RParen)
            {
                columns = ParseCommaSeparated(p => p.ParseExpr());
            }
            else
            {
                columns = Sequence<Expr>.Empty;
            }
            ExpectToken(new Token.RParen());
            return new OneOrManyWithParens<Expr>.Many(columns);
        }
        else
        {
            return new OneOrManyWithParens<Expr>.One(ParseExpr());
        }
    }
}
