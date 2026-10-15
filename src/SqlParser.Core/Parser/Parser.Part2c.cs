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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 8262–9342:
// parse_create_index … parse_plain_option).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse a <c>CREATE INDEX</c> statement.</summary>
    public CreateIndex ParseCreateIndex(bool unique)
    {
        var concurrently = ParseKeyword(Keyword.CONCURRENTLY);
        var @async = ParseKeyword(Keyword.ASYNC);
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);

        IndexType? @using = null;

        ObjectName? indexName;
        if (ifNotExists || !ParseKeyword(Keyword.ON))
        {
            // Rust: `let index_name` interno sombreia o externo (caso 72).
            var parsedIndexName = ParseObjectName(false);
            // MySQL allows `USING index_type` either before or after `ON table_name`
            @using = ParseOptionalUsingThenIndexType();
            ExpectKeywordIs(Keyword.ON);
            indexName = parsedIndexName;
        }
        else
        {
            indexName = null;
        }

        var tableName = ParseObjectName(false);

        // MySQL allows having two `USING` clauses.
        // In that case, the second clause overwrites the first.
        @using = ParseOptionalUsingThenIndexType() ?? @using;

        var columns = ParseParenthesizedIndexColumnList();

        Sequence<Ident> include;
        if (ParseKeyword(Keyword.INCLUDE))
        {
            ExpectToken(new Token.LParen());
            // Rust: `let columns` interno sombreia o externo (caso 72).
            var includeColumns = ParseCommaSeparated(p => p.ParseIdentifier());
            ExpectToken(new Token.RParen());
            include = includeColumns;
        }
        else
        {
            include = Sequence<Ident>.Empty;
        }

        bool? nullsDistinct;
        if (ParseKeyword(Keyword.NULLS))
        {
            var @not = ParseKeyword(Keyword.NOT);
            ExpectKeywordIs(Keyword.DISTINCT);
            nullsDistinct = !@not;
        }
        else
        {
            nullsDistinct = null;
        }

        Sequence<Expr> @with;
        if (_dialect.SupportsCreateIndexWithClause
            && ParseKeyword(Keyword.WITH))
        {
            ExpectToken(new Token.LParen());
            var withParams = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            @with = withParams;
        }
        else
        {
            @with = Sequence<Expr>.Empty;
        }

        Expr? predicate;
        if (ParseKeyword(Keyword.WHERE))
        {
            predicate = ParseExpr();
        }
        else
        {
            predicate = null;
        }

        // MySQL options (including the modern style of `USING` after the column list instead of
        // before, which is deprecated) shouldn't conflict with other preceding options (e.g. `WITH
        // PARSER` won't be caught by the above `WITH` clause parsing because MySQL doesn't set that
        // support flag). This is probably invalid syntax for other dialects, but it is simpler to
        // parse it anyway (as we do inside `ALTER TABLE` and `CREATE TABLE` parsing).
        var indexOptions = ParseIndexOptions();

        // MySQL allows `ALGORITHM` and `LOCK` options. Unlike in `ALTER TABLE`, they need not be comma separated.
        var alterOptions = new List<AlterTableOperation>();
        while (PeekOneOfKeywords(Keyword.ALGORITHM, Keyword.LOCK) is not null)
        {
            alterOptions.Add(ParseAlterTableOperation());
        }

        return new CreateIndex(
            Name: indexName,
            TableName: tableName,
            Using: @using,
            Columns: columns,
            Unique: unique,
            Concurrently: concurrently,
            Async: @async,
            IfNotExists: ifNotExists,
            Include: include,
            NullsDistinct: nullsDistinct,
            With: @with,
            Predicate: predicate,
            IndexOptions: indexOptions,
            AlterOptions: alterOptions.ToSequence());
    }

    /// <summary>Parse a <c>CREATE EXTENSION</c> statement.</summary>
    public CreateExtension ParseCreateExtension()
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = ParseIdentifier();

        // let (schema, version, cascade) = if .. { (..) } else { (None, None, false) };
        Ident? schema;
        Ident? version;
        bool cascade;
        if (ParseKeyword(Keyword.WITH))
        {
            if (ParseKeyword(Keyword.SCHEMA))
            {
                schema = ParseIdentifier();
            }
            else
            {
                schema = null;
            }

            if (ParseKeyword(Keyword.VERSION))
            {
                version = ParseIdentifier();
            }
            else
            {
                version = null;
            }

            cascade = ParseKeyword(Keyword.CASCADE);
        }
        else
        {
            (schema, version, cascade) = (null, null, false);
        }

        return new CreateExtension(
            Name: name,
            IfNotExists: ifNotExists,
            Schema: schema,
            Version: version,
            Cascade: cascade);
    }

    /// <summary>Parse a PostgreSQL-specific <c>Statement::CreateCollation</c> statement.</summary>
    public CreateCollation ParseCreateCollation()
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = ParseObjectName(false);

        CreateCollationDefinition definition;
        if (ParseKeyword(Keyword.FROM))
        {
            definition = new CreateCollationDefinition.From(ParseObjectName(false));
        }
        else if (ConsumeToken(new Token.LParen()))
        {
            var options = ParseCommaSeparated(p => p.ParseSqlOption());
            ExpectToken(new Token.RParen());
            definition = new CreateCollationDefinition.Options(options);
        }
        else
        {
            return ExpectedRef<CreateCollation>(
                "FROM or parenthesized option list after CREATE COLLATION name",
                PeekTokenRef());
        }

        return new CreateCollation(
            IfNotExists: ifNotExists,
            Name: name,
            Definition: definition);
    }

    /// <summary>Parse a PostgreSQL-specific <c>Statement::DropExtension</c> statement.</summary>
    public Statement ParseDropExtension()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var names = ParseCommaSeparated(p => p.ParseIdentifier());
        var cascadeOrRestrict =
            ParseOneOfKeywords(Keyword.CASCADE, Keyword.RESTRICT);
        // cascade_or_restrict.map(|k| match k { .. }).transpose()?
        ReferentialAction? cascadeOrRestrictAction;
        switch (cascadeOrRestrict)
        {
            case null:
                cascadeOrRestrictAction = null;
                break;
            case Keyword.CASCADE:
                cascadeOrRestrictAction = new ReferentialAction.Cascade();
                break;
            case Keyword.RESTRICT:
                cascadeOrRestrictAction = new ReferentialAction.Restrict();
                break;
            default:
                cascadeOrRestrictAction = ExpectedRef<ReferentialAction>("CASCADE or RESTRICT", PeekTokenRef());
                break;
        }
        return new Statement.DropExtension(new DropExtension(
            Names: names,
            IfExists: ifExists,
            CascadeOrRestrict: cascadeOrRestrictAction));
    }

    /// <summary>Parse a<c>Statement::DropOperator</c> statement.</summary>
    public Statement ParseDropOperator()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var operators = ParseCommaSeparated(p => p.ParseDropOperatorSignature());
        var dropBehavior = ParseOptionalDropBehavior();
        return new Statement.DropOperator(new DropOperator(
            IfExists: ifExists,
            Operators: operators,
            DropBehavior: dropBehavior));
    }

    /// <summary>
    /// Parse an operator signature for a <c>Statement::DropOperator</c>
    /// Format: <c>name ( { left_type | NONE } , right_type )</c>
    /// </summary>
    internal DropOperatorSignature ParseDropOperatorSignature()
    {
        var name = ParseOperatorName();
        ExpectToken(new Token.LParen());

        // Parse left operand type (or NONE for prefix operators)
        DataType? leftType;
        if (ParseKeyword(Keyword.NONE))
        {
            leftType = null;
        }
        else
        {
            leftType = ParseDataType();
        }

        ExpectToken(new Token.Comma());

        // Parse right operand type (always required)
        var rightType = ParseDataType();

        ExpectToken(new Token.RParen());

        return new DropOperatorSignature(
            Name: name,
            LeftType: leftType,
            RightType: rightType);
    }

    /// <summary>
    /// Parse a <c>Statement::DropOperatorFamily</c>
    /// <see href="https://www.postgresql.org/docs/current/sql-dropopfamily.html">PostgreSQL Documentation</see>
    /// </summary>
    public Statement ParseDropOperatorFamily()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var names = ParseCommaSeparated(p => p.ParseObjectName(false));
        ExpectKeyword(Keyword.USING);
        var @using = ParseIdentifier();
        var dropBehavior = ParseOptionalDropBehavior();
        return new Statement.DropOperatorFamily(new DropOperatorFamily(
            IfExists: ifExists,
            Names: names,
            Using: @using,
            DropBehavior: dropBehavior));
    }

    /// <summary>
    /// Parse a <c>Statement::DropOperatorClass</c>
    /// <see href="https://www.postgresql.org/docs/current/sql-dropopclass.html">PostgreSQL Documentation</see>
    /// </summary>
    public Statement ParseDropOperatorClass()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var names = ParseCommaSeparated(p => p.ParseObjectName(false));
        ExpectKeyword(Keyword.USING);
        var @using = ParseIdentifier();
        var dropBehavior = ParseOptionalDropBehavior();
        return new Statement.DropOperatorClass(new DropOperatorClass(
            IfExists: ifExists,
            Names: names,
            Using: @using,
            DropBehavior: dropBehavior));
    }

    /// <summary>Parse Hive distribution style.</summary>
    /// <remarks>TODO: Support parsing for <c>SKEWED</c> distribution style.</remarks>
    public HiveDistributionStyle ParseHiveDistribution()
    {
        if (ParseKeywords(Keyword.PARTITIONED, Keyword.BY))
        {
            ExpectToken(new Token.LParen());
            var columns =
                ParseCommaSeparated(parser => parser.ParseColumnDefInner(true));
            ExpectToken(new Token.RParen());
            return new HiveDistributionStyle.PARTITIONED(Columns: columns);
        }
        else
        {
            return new HiveDistributionStyle.NONE();
        }
    }

    /// <summary>Parse Redshift <c>DISTSTYLE { AUTO | EVEN | KEY | ALL }</c>.</summary>
    /// <remarks>See <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CREATE_TABLE_NEW.html"/></remarks>
    internal DistStyle ParseDistStyle()
    {
        var token = NextToken();
        switch (token.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.AUTO:
                        return new DistStyle.Auto();
                    case Keyword.EVEN:
                        return new DistStyle.Even();
                    case Keyword.KEY:
                        return new DistStyle.Key();
                    case Keyword.ALL:
                        return new DistStyle.All();
                    default:
                        return Expected<DistStyle>("AUTO, EVEN, KEY, or ALL", token);
                }
            default:
                return Expected<DistStyle>("AUTO, EVEN, KEY, or ALL", token);
        }
    }

    /// <summary>Parse Hive formats.</summary>
    // `hive_format.get_or_insert_with(HiveFormat::default).campo = valor` →
    // `hiveFormat = (hiveFormat ?? HiveFormat.Default()) with { Campo = valor }` (o record é imutável).
    public HiveFormat? ParseHiveFormats()
    {
        HiveFormat? hiveFormat = null;
        while (true)
        {
            var stop = false;
            switch (ParseOneOfKeywords(
                        Keyword.ROW,
                        Keyword.STORED,
                        Keyword.LOCATION,
                        Keyword.WITH,
                        Keyword.USING))
            {
                case Keyword.ROW:
                {
                    hiveFormat = (hiveFormat ?? HiveFormat.Default()) with
                    {
                        RowFormat = ParseRowFormat(),
                    };
                    break;
                }
                case Keyword.STORED:
                {
                    ExpectKeywordIs(Keyword.AS);
                    if (ParseKeyword(Keyword.INPUTFORMAT))
                    {
                        var inputFormat = ParseExpr();
                        ExpectKeywordIs(Keyword.OUTPUTFORMAT);
                        var outputFormat = ParseExpr();
                        hiveFormat = (hiveFormat ?? HiveFormat.Default()) with
                        {
                            Storage = new HiveIOFormat.IOF(
                                InputFormat: inputFormat,
                                OutputFormat: outputFormat),
                        };
                    }
                    else
                    {
                        var format = ParseFileFormat();
                        hiveFormat = (hiveFormat ?? HiveFormat.Default()) with
                        {
                            Storage = new HiveIOFormat.FileFormat(Format: format),
                        };
                    }
                    break;
                }
                case Keyword.LOCATION:
                {
                    hiveFormat = (hiveFormat ?? HiveFormat.Default()) with
                    {
                        Location = ParseLiteralString(),
                    };
                    break;
                }
                case Keyword.WITH:
                {
                    PrevToken();
                    var properties =
                        ParseOptionsWithKeywords(Keyword.WITH, Keyword.SERDEPROPERTIES);
                    if (!properties.IsEmpty)
                    {
                        hiveFormat = (hiveFormat ?? HiveFormat.Default()) with
                        {
                            SerdeProperties = properties,
                        };
                    }
                    else
                    {
                        stop = true;
                    }
                    break;
                }
                case Keyword.USING when _dialect.SupportsCreateTableUsing:
                {
                    var format = ParseIdentifier();
                    hiveFormat = (hiveFormat ?? HiveFormat.Default()) with
                    {
                        Storage = new HiveIOFormat.Using(Format: format),
                    };
                    break;
                }
                case Keyword.USING:
                {
                    // USING is not a table format keyword in this dialect; put it back
                    PrevToken();
                    stop = true;
                    break;
                }
                case null:
                    stop = true;
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

        return hiveFormat;
    }

    /// <summary>Parse Hive row format.</summary>
    public HiveRowFormat ParseRowFormat()
    {
        ExpectKeywordIs(Keyword.FORMAT);
        switch (ParseOneOfKeywords(Keyword.SERDE, Keyword.DELIMITED))
        {
            case Keyword.SERDE:
            {
                var @class = ParseLiteralString();
                return new HiveRowFormat.SERDE(Class: @class);
            }
            default:
            {
                var rowDelimiters = new List<HiveRowDelimiter>();

                while (true)
                {
                    var stop = false;
                    switch (ParseOneOfKeywords(
                                Keyword.FIELDS,
                                Keyword.COLLECTION,
                                Keyword.MAP,
                                Keyword.LINES,
                                Keyword.NULL))
                    {
                        case Keyword.FIELDS
                            when ParseKeywords(Keyword.TERMINATED, Keyword.BY):
                        {
                            rowDelimiters.Add(new HiveRowDelimiter(
                                Delimiter: new HiveDelimiter.FieldsTerminatedBy(),
                                Char: ParseIdentifier()));

                            if (ParseKeywords(Keyword.ESCAPED, Keyword.BY))
                            {
                                rowDelimiters.Add(new HiveRowDelimiter(
                                    Delimiter: new HiveDelimiter.FieldsEscapedBy(),
                                    Char: ParseIdentifier()));
                            }
                            break;
                        }
                        case Keyword.COLLECTION
                            when ParseKeywords(
                                Keyword.ITEMS,
                                Keyword.TERMINATED,
                                Keyword.BY):
                        {
                            rowDelimiters.Add(new HiveRowDelimiter(
                                Delimiter: new HiveDelimiter.CollectionItemsTerminatedBy(),
                                Char: ParseIdentifier()));
                            break;
                        }
                        case Keyword.MAP
                            when ParseKeywords(
                                Keyword.KEYS,
                                Keyword.TERMINATED,
                                Keyword.BY):
                        {
                            rowDelimiters.Add(new HiveRowDelimiter(
                                Delimiter: new HiveDelimiter.MapKeysTerminatedBy(),
                                Char: ParseIdentifier()));
                            break;
                        }
                        case Keyword.LINES
                            when ParseKeywords(Keyword.TERMINATED, Keyword.BY):
                        {
                            rowDelimiters.Add(new HiveRowDelimiter(
                                Delimiter: new HiveDelimiter.LinesTerminatedBy(),
                                Char: ParseIdentifier()));
                            break;
                        }
                        case Keyword.NULL
                            when ParseKeywords(Keyword.DEFINED, Keyword.AS):
                        {
                            rowDelimiters.Add(new HiveRowDelimiter(
                                Delimiter: new HiveDelimiter.NullDefinedAs(),
                                Char: ParseIdentifier()));
                            break;
                        }
                        case Keyword.NULL:
                            stop = true;
                            break;
                        default:
                        {
                            stop = true;
                            break;
                        }
                    }
                    if (stop)
                    {
                        break;
                    }
                }

                return new HiveRowFormat.DELIMITED(
                    Delimiters: rowDelimiters.ToSequence());
            }
        }
    }

    internal Ident? ParseOptionalOnCluster()
    {
        if (ParseKeywords(Keyword.ON, Keyword.CLUSTER))
        {
            return ParseIdentifier();
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse <c>CREATE TABLE</c> statement.</summary>
    public CreateTable ParseCreateTable(
        bool orReplace,
        bool temporary,
        bool unlogged,
        bool? global,
        bool transient,
        bool @volatile,
        bool? multiset)
    {
        var allowUnquotedHyphen = _dialect is BigQueryDialect;
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var tableName = ParseObjectName(allowUnquotedHyphen);

        bool? fallback;
        if (_dialect.SupportsLeadingCommaBeforeTableOptions
            && ConsumeToken(new Token.Comma()))
        {
            // Rust: `let fallback` interno sombreia o externo (caso 72).
            var parsedFallback = MaybeParseFallback();
            if (parsedFallback is null)
            {
                PrevToken(); // Put back comma.
            }
            fallback = parsedFallback;
        }
        else
        {
            fallback = null;
        }

        // PostgreSQL PARTITION OF for child partition tables
        // Note: This is a PostgreSQL-specific feature, but the dialect check was intentionally
        // removed to allow GenericDialect and other dialects to parse this syntax. This enables
        // multi-dialect SQL tools to work with PostgreSQL-specific DDL statements.
        //
        // PARTITION OF can be combined with other table definition clauses in the AST,
        // though PostgreSQL itself prohibits PARTITION OF with AS SELECT or LIKE clauses.
        // The parser accepts these combinations for flexibility; semantic validation
        // is left to downstream tools.
        // Child partitions can have their own constraints and indexes.
        ObjectName? partitionOf;
        if (ParseKeywords(Keyword.PARTITION, Keyword.OF))
        {
            partitionOf = ParseObjectName(allowUnquotedHyphen);
        }
        else
        {
            partitionOf = null;
        }

        // Clickhouse has `ON CLUSTER 'cluster'` syntax for DDLs
        var onCluster = ParseOptionalOnCluster();

        var like = MaybeParseCreateTableLike(allowUnquotedHyphen);

        ObjectName? clone;
        if (ParseKeyword(Keyword.CLONE))
        {
            // self.parse_object_name(..).ok(): o erro vira None, sem restaurar a posição.
            try
            {
                clone = ParseObjectName(allowUnquotedHyphen);
            }
            catch (ParserException)
            {
                clone = null;
            }
        }
        else
        {
            clone = null;
        }

        // parse optional column list (schema)
        var (columns, constraints) = ParseColumns();
        CommentDef? commentAfterColumnDef;
        if (_dialect is HiveDialect && ParseKeyword(Keyword.COMMENT))
        {
            var nextToken = NextToken();
            switch (nextToken.Token)
            {
                case Token.SingleQuotedString { Value: var str }:
                    commentAfterColumnDef = new CommentDef.WithoutEq(str);
                    break;
                default:
                    commentAfterColumnDef = Expected<CommentDef?>("comment", nextToken);
                    break;
            }
        }
        else
        {
            commentAfterColumnDef = null;
        }

        // PostgreSQL PARTITION OF: partition bound specification
        ForValues? forValues;
        if (partitionOf is not null)
        {
            if (PeekKeyword(Keyword.FOR) || PeekKeyword(Keyword.DEFAULT))
            {
                forValues = ParsePartitionForValues();
            }
            else
            {
                return ExpectedRef<CreateTable>(
                    "FOR VALUES or DEFAULT after PARTITION OF",
                    PeekTokenRef());
            }
        }
        else
        {
            forValues = null;
        }

        // SQLite supports `WITHOUT ROWID` at the end of `CREATE TABLE`
        var withoutRowid = ParseKeywords(Keyword.WITHOUT, Keyword.ROWID);

        var hiveDistribution = ParseHiveDistribution();
        var clusteredBy = ParseOptionalClusteredBy();
        var hiveFormats = ParseHiveFormats();

        var createTableConfig = ParseOptionalCreateTableConfig();

        // ClickHouse supports `PRIMARY KEY`, before `ORDER BY`
        // https://clickhouse.com/docs/en/sql-reference/statements/create/table#primary-key
        Expr? primaryKey;
        if ((_dialect is ClickHouseDialect or GenericDialect)
            && ParseKeywords(Keyword.PRIMARY, Keyword.KEY))
        {
            primaryKey = ParseExpr();
        }
        else
        {
            primaryKey = null;
        }

        OneOrManyWithParens<Expr>? orderBy;
        if (ParseKeywords(Keyword.ORDER, Keyword.BY))
        {
            if (ConsumeToken(new Token.LParen()))
            {
                // Rust: `let columns` interno sombreia o externo (caso 72).
                Sequence<Expr> orderByColumns;
                if (PeekTokenRef().Token is not Token.RParen)
                {
                    orderByColumns = ParseCommaSeparated(p => p.ParseExpr());
                }
                else
                {
                    orderByColumns = Sequence<Expr>.Empty;
                }
                ExpectToken(new Token.RParen());
                orderBy = new OneOrManyWithParens<Expr>.Many(orderByColumns);
            }
            else
            {
                orderBy = new OneOrManyWithParens<Expr>.One(ParseExpr());
            }
        }
        else
        {
            orderBy = null;
        }

        // ClickHouse allows PARTITION BY after ORDER BY
        // https://clickhouse.com/docs/en/sql-reference/statements/create/table#partition-by
        Expr? partitionBy;
        if (createTableConfig.PartitionBy is null
            && _dialect.SupportsPartitionByAfterOrderBy
            && ParseKeywords(Keyword.PARTITION, Keyword.BY))
        {
            partitionBy = ParseExpr();
        }
        else
        {
            partitionBy = createTableConfig.PartitionBy;
        }

        OnCommit? onCommit;
        if (ParseKeywords(Keyword.ON, Keyword.COMMIT))
        {
            onCommit = ParseCreateTableOnCommit();
        }
        else
        {
            onCommit = null;
        }

        var strict = ParseKeyword(Keyword.STRICT);

        // Redshift: BACKUP YES|NO
        bool? backup;
        if (ParseKeyword(Keyword.BACKUP))
        {
            var keyword = ExpectOneOfKeywords(Keyword.YES, Keyword.NO);
            backup = keyword == Keyword.YES;
        }
        else
        {
            backup = null;
        }

        // Redshift: DISTSTYLE, DISTKEY, SORTKEY
        DistStyle? diststyle;
        if (ParseKeyword(Keyword.DISTSTYLE))
        {
            diststyle = ParseDistStyle();
        }
        else
        {
            diststyle = null;
        }
        Expr? distkey;
        if (ParseKeyword(Keyword.DISTKEY))
        {
            ExpectToken(new Token.LParen());
            var expr = ParseExpr();
            ExpectToken(new Token.RParen());
            distkey = expr;
        }
        else
        {
            distkey = null;
        }
        Sequence<Expr>? sortkey;
        if (ParseKeyword(Keyword.SORTKEY))
        {
            ExpectToken(new Token.LParen());
            // Rust: `let columns` interno sombreia o externo (caso 72).
            var sortkeyColumns = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            sortkey = sortkeyColumns;
        }
        else
        {
            sortkey = null;
        }

        // Parse optional `AS ( query )`
        Query? query;
        if (ParseKeyword(Keyword.AS))
        {
            query = ParseQuery();
        }
        else if (_dialect.SupportsCreateTableSelect && ParseKeyword(Keyword.SELECT))
        {
            // rewind the SELECT keyword
            PrevToken();
            query = ParseQuery();
        }
        else
        {
            query = null;
        }

        // `WITH DATA` clause only applies if there is a query body.
        WithData? withData;
        if (query is not null)
        {
            withData = MaybeParseWithData();
        }
        else
        {
            withData = null;
        }

        return new CreateTableBuilder(tableName)
            .SetTemporary(temporary)
            .SetUnlogged(unlogged)
            .SetColumns(columns)
            .SetConstraints(constraints)
            .SetOrReplace(orReplace)
            .SetIfNotExists(ifNotExists)
            .SetTransient(transient)
            .SetVolatile(@volatile)
            .SetMultiset(multiset)
            .SetFallback(fallback)
            .SetHiveDistribution(hiveDistribution)
            .SetHiveFormats(hiveFormats)
            .SetGlobal(global)
            .SetQuery(query)
            .SetWithoutRowid(withoutRowid)
            .SetLike(like)
            .CloneClause(clone)
            .CommentAfterColumnDef(commentAfterColumnDef)
            .SetOrderBy(orderBy)
            .SetOnCommit(onCommit)
            .SetOnCluster(onCluster)
            .SetClusteredBy(clusteredBy)
            .SetPartitionBy(partitionBy)
            .SetClusterBy(createTableConfig.ClusterBy)
            .SetInherits(createTableConfig.Inherits)
            .SetPartitionOf(partitionOf)
            .SetForValues(forValues)
            .SetTableOptions(createTableConfig.TableOptions)
            .SetPrimaryKey(primaryKey)
            .SetWithData(withData)
            .SetStrict(strict)
            .SetBackup(backup)
            .SetDiststyle(diststyle)
            .SetDistkey(distkey)
            .SetSortkey(sortkey)
            .Build();
    }

    /// <summary>Parse <c>MULTISET</c> table-kind prefix on <c>CREATE TABLE</c>.</summary>
    internal bool? MaybeParseMultiset()
    {
        switch (ParseOneOfKeywords(Keyword.SET, Keyword.MULTISET))
        {
            case Keyword.MULTISET:
                return true;
            case Keyword.SET:
                return false;
            default:
                return null;
        }
    }

    /// <summary>Parse <c>FALLBACK</c> option on a <c>CREATE TABLE</c> statement,</summary>
    internal bool? MaybeParseFallback()
    {
        if (ParseKeywords(Keyword.NO, Keyword.FALLBACK))
        {
            return false;
        }
        else if (ParseKeyword(Keyword.FALLBACK))
        {
            return true;
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse <see cref="WithData"/> clause on <c>CREATE TABLE ... AS</c> statement.</summary>
    internal WithData? MaybeParseWithData()
    {
        bool data;
        if (ParseKeywords(Keyword.WITH, Keyword.DATA))
        {
            data = true;
        }
        else if (ParseKeywords(Keyword.WITH, Keyword.NO, Keyword.DATA))
        {
            data = false;
        }
        else
        {
            return null;
        }

        bool? statistics;
        if (ParseKeywords(Keyword.AND, Keyword.STATISTICS))
        {
            statistics = true;
        }
        else if (ParseKeywords(Keyword.AND, Keyword.NO, Keyword.STATISTICS))
        {
            statistics = false;
        }
        else
        {
            statistics = null;
        }

        return new WithData(Data: data, Statistics: statistics);
    }

    internal CreateTableLikeKind? MaybeParseCreateTableLike(
        bool allowUnquotedHyphen)
    {
        CreateTableLikeKind? like;
        if (_dialect.SupportsCreateTableLikeParenthesized
            && ConsumeToken(new Token.LParen()))
        {
            if (ParseKeyword(Keyword.LIKE))
            {
                var name = ParseObjectName(allowUnquotedHyphen);
                CreateTableLikeDefaults? defaults;
                if (ParseKeywords(Keyword.INCLUDING, Keyword.DEFAULTS))
                {
                    defaults = new CreateTableLikeDefaults.Including();
                }
                else if (ParseKeywords(Keyword.EXCLUDING, Keyword.DEFAULTS))
                {
                    defaults = new CreateTableLikeDefaults.Excluding();
                }
                else
                {
                    defaults = null;
                }
                ExpectToken(new Token.RParen());
                like = new CreateTableLikeKind.Parenthesized(new CreateTableLike(
                    Name: name,
                    Defaults: defaults));
            }
            else
            {
                // Rollback the '(' it's probably the columns list
                PrevToken();
                like = null;
            }
        }
        else if (ParseKeyword(Keyword.LIKE) || ParseKeyword(Keyword.ILIKE))
        {
            var name = ParseObjectName(allowUnquotedHyphen);
            like = new CreateTableLikeKind.Plain(new CreateTableLike(
                Name: name,
                Defaults: null));
        }
        else
        {
            like = null;
        }
        return like;
    }

    internal OnCommit ParseCreateTableOnCommit()
    {
        if (ParseKeywords(Keyword.DELETE, Keyword.ROWS))
        {
            return OnCommit.DeleteRows;
        }
        else if (ParseKeywords(Keyword.PRESERVE, Keyword.ROWS))
        {
            return OnCommit.PreserveRows;
        }
        else if (ParseKeywords(Keyword.DROP))
        {
            return OnCommit.Drop;
        }
        else
        {
            // parser_err!(MSG, self.peek_token_ref()): o segundo argumento é o token (não a posição), então a
            // mensagem termina com o Display do token, sem " at Line: ..". ParserErr só aceita Location.
            throw new ParserException(new ParserError.ParserErrorValue(
                "Expecting DELETE ROWS, PRESERVE ROWS or DROP" + PeekTokenRef().ToString()));
        }
    }

    /// <summary>Parse <see cref="ForValues"/> of a <c>PARTITION OF</c> clause.</summary>
    /// <remarks>
    /// Parses: <c>FOR VALUES partition_bound_spec | DEFAULT</c>
    /// <see href="https://www.postgresql.org/docs/current/sql-createtable.html">PostgreSQL</see>
    /// </remarks>
    internal ForValues ParsePartitionForValues()
    {
        if (ParseKeyword(Keyword.DEFAULT))
        {
            return new ForValues.Default();
        }

        ExpectKeywords(Keyword.FOR, Keyword.VALUES);

        if (ParseKeyword(Keyword.IN))
        {
            // FOR VALUES IN (expr, ...)
            ExpectToken(new Token.LParen());
            if (PeekTokenRef().Token is Token.RParen)
            {
                return ExpectedRef<ForValues>("at least one value", PeekTokenRef());
            }
            var values = ParseCommaSeparated(p => p.ParseExpr());
            ExpectToken(new Token.RParen());
            return new ForValues.In(values);
        }
        else if (ParseKeyword(Keyword.FROM))
        {
            // FOR VALUES FROM (...) TO (...)
            ExpectToken(new Token.LParen());
            if (PeekTokenRef().Token is Token.RParen)
            {
                return ExpectedRef<ForValues>("at least one value", PeekTokenRef());
            }
            var @from = ParseCommaSeparated(p => p.ParsePartitionBoundValue());
            ExpectToken(new Token.RParen());
            ExpectKeyword(Keyword.TO);
            ExpectToken(new Token.LParen());
            if (PeekTokenRef().Token is Token.RParen)
            {
                return ExpectedRef<ForValues>("at least one value", PeekTokenRef());
            }
            var to = ParseCommaSeparated(p => p.ParsePartitionBoundValue());
            ExpectToken(new Token.RParen());
            return new ForValues.From(FromValue: @from, To: to);
        }
        else if (ParseKeyword(Keyword.WITH))
        {
            // FOR VALUES WITH (MODULUS n, REMAINDER r)
            ExpectToken(new Token.LParen());
            ExpectKeyword(Keyword.MODULUS);
            var modulus = ParseLiteralUint();
            ExpectToken(new Token.Comma());
            ExpectKeyword(Keyword.REMAINDER);
            var remainder = ParseLiteralUint();
            ExpectToken(new Token.RParen());
            return new ForValues.With(Modulus: modulus, Remainder: remainder);
        }
        else
        {
            return ExpectedRef<ForValues>("IN, FROM, or WITH after FOR VALUES", PeekTokenRef());
        }
    }

    /// <summary>Parse a single partition bound value (MINVALUE, MAXVALUE, or expression).</summary>
    internal PartitionBoundValue ParsePartitionBoundValue()
    {
        if (ParseKeyword(Keyword.MINVALUE))
        {
            return new PartitionBoundValue.MinValue();
        }
        else if (ParseKeyword(Keyword.MAXVALUE))
        {
            return new PartitionBoundValue.MaxValue();
        }
        else
        {
            return new PartitionBoundValue.Expr(ParseExpr());
        }
    }

    /// <summary>Parse configuration like inheritance, partitioning, clustering information during the table creation.</summary>
    /// <remarks>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#syntax_2">BigQuery</see>
    /// <see href="https://www.postgresql.org/docs/current/ddl-partitioning.html">PostgreSQL</see>
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table.html">MySql</see>
    /// </remarks>
    internal CreateTableConfiguration ParseOptionalCreateTableConfig()
    {
        CreateTableOptions tableOptions = new CreateTableOptions.None();

        Sequence<ObjectName>? inherits;
        if (ParseKeyword(Keyword.INHERITS))
        {
            inherits = ParseParenthesizedQualifiedColumnList(IsOptional.Mandatory, false);
        }
        else
        {
            inherits = null;
        }

        // PostgreSQL supports `WITH ( options )`, before `AS`
        var withOptions = ParseOptions(Keyword.WITH);
        if (!withOptions.IsEmpty)
        {
            tableOptions = new CreateTableOptions.With(withOptions);
        }

        var tableProperties = ParseOptions(Keyword.TBLPROPERTIES);
        if (!tableProperties.IsEmpty)
        {
            tableOptions = new CreateTableOptions.TableProperties(tableProperties);
        }
        Expr? partitionBy;
        if ((_dialect is BigQueryDialect or PostgreSqlDialect or GenericDialect)
            && ParseKeywords(Keyword.PARTITION, Keyword.BY))
        {
            partitionBy = ParseExpr();
        }
        else
        {
            partitionBy = null;
        }

        WrappedCollection<Sequence<Expr>>? clusterBy = null;
        if (_dialect is BigQueryDialect or GenericDialect)
        {
            if (ParseKeywords(Keyword.CLUSTER, Keyword.BY))
            {
                clusterBy = new WrappedCollection<Sequence<Expr>>.NoWrapping(
                    ParseCommaSeparated(p => p.ParseExpr()));
            }

            if (PeekTokenRef().Token is Token.Word { Value: var word })
            {
                if (word.Keyword == Keyword.OPTIONS)
                {
                    tableOptions =
                        new CreateTableOptions.Options(ParseOptions(Keyword.OPTIONS));
                }
            }
        }

        // table_options == CreateTableOptions::None (caso 69)
        if (!(_dialect is HiveDialect) && tableOptions is CreateTableOptions.None)
        {
            var plainOptions = ParsePlainOptions();
            if (!plainOptions.IsEmpty)
            {
                tableOptions = new CreateTableOptions.Plain(plainOptions);
            }
        }

        return new CreateTableConfiguration(
            PartitionBy: partitionBy,
            ClusterBy: clusterBy,
            Inherits: inherits,
            TableOptions: tableOptions);
    }

    internal SqlOption? ParsePlainOption()
    {
        // Single parameter option
        // <https://dev.mysql.com/doc/refman/8.4/en/create-table.html>
        if (ParseKeywords(Keyword.START, Keyword.TRANSACTION))
        {
            return new SqlOption.Ident(new Ident("START TRANSACTION"));
        }

        // Custom option
        // <https://dev.mysql.com/doc/refman/8.4/en/create-table.html>
        if (ParseKeywords(Keyword.COMMENT))
        {
            var hasEq = ConsumeToken(new Token.Eq());
            var value = NextToken();

            SqlOption? comment;
            switch ((hasEq, value.Token))
            {
                case (true, Token.SingleQuotedString { Value: var s }):
                    comment = new SqlOption.Comment(new CommentDef.WithEq(s));
                    break;
                case (false, Token.SingleQuotedString { Value: var s }):
                    comment = new SqlOption.Comment(new CommentDef.WithoutEq(s));
                    break;
                case (_, var token):
                    comment = Expected<SqlOption?>("Token::SingleQuotedString", TokenWithSpan.Wrap(token));
                    break;
            }
            return comment;
        }

        // <https://dev.mysql.com/doc/refman/8.4/en/create-table.html>
        // <https://clickhouse.com/docs/sql-reference/statements/create/table>
        if (ParseKeywords(Keyword.ENGINE))
        {
            _ = ConsumeToken(new Token.Eq());
            var value = NextToken();

            SqlOption? engine;
            switch (value.Token)
            {
                case Token.Word { Value: var w }:
                {
                    Sequence<Ident> parameters;
                    if (PeekTokenRef().Token is Token.LParen)
                    {
                        parameters = ParseParenthesizedIdentifiers();
                    }
                    else
                    {
                        parameters = Sequence<Ident>.Empty;
                    }

                    engine = new SqlOption.NamedParenthesizedList(
                        new NamedParenthesizedList(
                            Key: new Ident("ENGINE"),
                            Name: new Ident(w.Value),
                            Values: parameters));
                    break;
                }
                default:
                    return Expected<SqlOption?>("Token::Word", value);
            }

            return engine;
        }

        // <https://dev.mysql.com/doc/refman/8.4/en/create-table.html>
        if (ParseKeywords(Keyword.TABLESPACE))
        {
            _ = ConsumeToken(new Token.Eq());
            var value = NextToken();

            // Braço `Token::Word(Word { value: name, .. }) | Token::SingleQuotedString(name) => { .. }`:
            // o C# não liga variáveis em padrões `or`; o `name` é extraído antes e o corpo do braço vem depois.
            string name;
            switch (value.Token)
            {
                case Token.Word { Value: var tablespaceWord }:
                    name = tablespaceWord.Value;
                    break;
                case Token.SingleQuotedString { Value: var tablespaceString }:
                    name = tablespaceString;
                    break;
                default:
                    return Expected<SqlOption?>("Token::Word", value);
            }

            StorageType? storage;
            switch (ParseKeyword(Keyword.STORAGE))
            {
                case true:
                {
                    _ = ConsumeToken(new Token.Eq());
                    var storageToken = NextToken();
                    switch (storageToken.Token)
                    {
                        case Token.Word { Value: var w }:
                            // PORT-DEVIATION DIV-001: `to_uppercase()` → `ToUpperInvariant()` (mapeamento simples).
                            // O Rust aceita `DISK` escrito com U+0131 no lugar do `i` ou com U+017F no lugar do
                            // `s`; o .NET recusa o U+0131 em todos os alvos e o U+017F no .NET Framework 4.8.
                            switch (w.Value.ToUpperInvariant())
                            {
                                case "DISK":
                                    storage = StorageType.Disk;
                                    break;
                                case "MEMORY":
                                    storage = StorageType.Memory;
                                    break;
                                default:
                                    storage = Expected<StorageType?>("Storage type (DISK or MEMORY)", storageToken);
                                    break;
                            }
                            break;
                        default:
                            storage = Expected<StorageType?>("Token::Word", storageToken);
                            break;
                    }
                    break;
                }
                case false:
                    storage = null;
                    break;
            }

            return new SqlOption.TableSpace(new TablespaceOption(
                Name: name,
                Storage: storage));
        }

        // <https://dev.mysql.com/doc/refman/8.4/en/create-table.html>
        if (ParseKeyword(Keyword.UNION))
        {
            _ = ConsumeToken(new Token.Eq());
            var value = NextToken();

            switch (value.Token)
            {
                case Token.LParen:
                {
                    var tables =
                        ParseCommaSeparated0(p => p.ParseIdentifier(), new Token.RParen());
                    ExpectToken(new Token.RParen());

                    return new SqlOption.NamedParenthesizedList(
                        new NamedParenthesizedList(
                            Key: new Ident("UNION"),
                            Name: null,
                            Values: tables));
                }
                default:
                    return Expected<SqlOption?>("Token::LParen", value);
            }
        }

        // Key/Value parameter option
        Ident key;
        if (ParseKeywords(Keyword.DEFAULT, Keyword.CHARSET))
        {
            key = new Ident("DEFAULT CHARSET");
        }
        else if (ParseKeyword(Keyword.CHARSET))
        {
            key = new Ident("CHARSET");
        }
        else if (ParseKeywords(Keyword.DEFAULT, Keyword.CHARACTER, Keyword.SET))
        {
            key = new Ident("DEFAULT CHARACTER SET");
        }
        else if (ParseKeywords(Keyword.CHARACTER, Keyword.SET))
        {
            key = new Ident("CHARACTER SET");
        }
        else if (ParseKeywords(Keyword.DEFAULT, Keyword.COLLATE))
        {
            key = new Ident("DEFAULT COLLATE");
        }
        else if (ParseKeyword(Keyword.COLLATE))
        {
            key = new Ident("COLLATE");
        }
        else if (ParseKeywords(Keyword.DATA, Keyword.DIRECTORY))
        {
            key = new Ident("DATA DIRECTORY");
        }
        else if (ParseKeywords(Keyword.INDEX, Keyword.DIRECTORY))
        {
            key = new Ident("INDEX DIRECTORY");
        }
        else if (ParseKeyword(Keyword.KEY_BLOCK_SIZE))
        {
            key = new Ident("KEY_BLOCK_SIZE");
        }
        else if (ParseKeyword(Keyword.ROW_FORMAT))
        {
            key = new Ident("ROW_FORMAT");
        }
        else if (ParseKeyword(Keyword.PACK_KEYS))
        {
            key = new Ident("PACK_KEYS");
        }
        else if (ParseKeyword(Keyword.STATS_AUTO_RECALC))
        {
            key = new Ident("STATS_AUTO_RECALC");
        }
        else if (ParseKeyword(Keyword.STATS_PERSISTENT))
        {
            key = new Ident("STATS_PERSISTENT");
        }
        else if (ParseKeyword(Keyword.STATS_SAMPLE_PAGES))
        {
            key = new Ident("STATS_SAMPLE_PAGES");
        }
        else if (ParseKeyword(Keyword.DELAY_KEY_WRITE))
        {
            key = new Ident("DELAY_KEY_WRITE");
        }
        else if (ParseKeyword(Keyword.COMPRESSION))
        {
            key = new Ident("COMPRESSION");
        }
        else if (ParseKeyword(Keyword.ENCRYPTION))
        {
            key = new Ident("ENCRYPTION");
        }
        else if (ParseKeyword(Keyword.MAX_ROWS))
        {
            key = new Ident("MAX_ROWS");
        }
        else if (ParseKeyword(Keyword.MIN_ROWS))
        {
            key = new Ident("MIN_ROWS");
        }
        else if (ParseKeyword(Keyword.AUTOEXTEND_SIZE))
        {
            key = new Ident("AUTOEXTEND_SIZE");
        }
        else if (ParseKeyword(Keyword.AVG_ROW_LENGTH))
        {
            key = new Ident("AVG_ROW_LENGTH");
        }
        else if (ParseKeyword(Keyword.CHECKSUM))
        {
            key = new Ident("CHECKSUM");
        }
        else if (ParseKeyword(Keyword.CONNECTION))
        {
            key = new Ident("CONNECTION");
        }
        else if (ParseKeyword(Keyword.ENGINE_ATTRIBUTE))
        {
            key = new Ident("ENGINE_ATTRIBUTE");
        }
        else if (ParseKeyword(Keyword.PASSWORD))
        {
            key = new Ident("PASSWORD");
        }
        else if (ParseKeyword(Keyword.SECONDARY_ENGINE_ATTRIBUTE))
        {
            key = new Ident("SECONDARY_ENGINE_ATTRIBUTE");
        }
        else if (ParseKeyword(Keyword.INSERT_METHOD))
        {
            key = new Ident("INSERT_METHOD");
        }
        else if (ParseKeyword(Keyword.AUTO_INCREMENT))
        {
            key = new Ident("AUTO_INCREMENT");
        }
        else
        {
            return null;
        }

        _ = ConsumeToken(new Token.Eq());

        // Rust: `let value` no nível do método; o C# não aceita o mesmo nome nos blocos `if` acima (caso 72).
        // match self.maybe_parse(|parser| parser.parse_value())?.map(Expr::Value) { Some(expr) => expr, None => .. }
        Expr optionValue;
        if (MaybeParse(parser => parser.ParseValue()) is { } parsedValue)
        {
            optionValue = new Expr.Value(parsedValue);
        }
        else
        {
            optionValue = new Expr.Identifier(ParseIdentifier());
        }

        return new SqlOption.KeyValue(Key: key, Value: optionValue);
    }
}
