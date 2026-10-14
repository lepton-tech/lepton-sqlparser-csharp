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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 5200–6740:
// parse_all_or_distinct … parse_analyze_format).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>
    /// Parse either <c>ALL</c>, <c>DISTINCT</c> or <c>DISTINCT ON (...)</c>. Returns <c>None</c> if <c>ALL</c> is parsed
    /// and results in a <see cref="ParserError"/> if both <c>ALL</c> and <c>DISTINCT</c> are found.
    /// </summary>
    public Distinct? ParseAllOrDistinct()
    {
        var loc = PeekTokenRef().Span.Start;
        Distinct? distinct;
        switch (ParseOneOfKeywords(Keyword.ALL, Keyword.DISTINCT))
        {
            case Keyword.ALL:
                if (PeekKeyword(Keyword.DISTINCT))
                {
                    throw ParserErr("Cannot specify ALL then DISTINCT", loc);
                }
                distinct = new Distinct.All();
                break;
            case Keyword.DISTINCT:
                if (PeekKeyword(Keyword.ALL))
                {
                    throw ParserErr("Cannot specify DISTINCT then ALL", loc);
                }
                distinct = new Distinct.DistinctValue();
                break;
            case null:
                return null;
            default:
                throw ParserErr("ALL or DISTINCT", loc);
        }

        // let Some(Distinct::Distinct) = distinct else { return Ok(distinct); };
        if (distinct is not Distinct.DistinctValue)
        {
            return distinct;
        }
        if (!ParseKeyword(Keyword.ON))
        {
            return new Distinct.DistinctValue();
        }

        ExpectToken(new Token.LParen());
        Sequence<Expr> colNames;
        if (ConsumeToken(new Token.RParen()))
        {
            PrevToken();
            colNames = Sequence<Expr>.Empty;
        }
        else
        {
            colNames = ParseCommaSeparated(p => p.ParseExpr());
        }
        ExpectToken(new Token.RParen());
        return new Distinct.On(colNames);
    }

    /// <summary>Parse a SQL CREATE statement</summary>
    public Statement ParseCreate()
    {
        var orReplace = ParseKeywords(Keyword.OR, Keyword.REPLACE);
        var orAlter = ParseKeywords(Keyword.OR, Keyword.ALTER);
        var multiset = MaybeParseMultiset();
        var local = ParseOneOfKeywords(Keyword.LOCAL) is not null;
        var global = ParseOneOfKeywords(Keyword.GLOBAL) is not null;
        var transient = ParseOneOfKeywords(Keyword.TRANSIENT) is not null;
        // Rust: `let global: Option<bool> = ..` sombreia o `global` anterior (caso 72).
        bool? globalOption;
        if (global)
        {
            globalOption = true;
        }
        else if (local)
        {
            globalOption = false;
        }
        else
        {
            globalOption = null;
        }
        var temporary = ParseOneOfKeywords(Keyword.TEMP, Keyword.TEMPORARY) is not null;
        var @volatile = ParseKeyword(Keyword.VOLATILE);
        var unlogged = PeekKeywords(Keyword.UNLOGGED, Keyword.TABLE);
        if (unlogged)
        {
            ExpectKeyword(Keyword.UNLOGGED);
        }
        var persistent = _dialect is DuckDbDialect
            && ParseOneOfKeywords(Keyword.PERSISTENT) is not null;
        var createViewParams = ParseCreateViewParams();
        if (PeekKeywords(Keyword.SNAPSHOT, Keyword.TABLE))
        {
            return Statement.From(ParseCreateSnapshotTable());
        }
        else if (PeekKeywords(Keyword.TEXT, Keyword.SEARCH))
        {
            return Statement.From(ParseCreateTextSearch());
        }
        else if (ParseKeyword(Keyword.TABLE))
        {
            return Statement.From(ParseCreateTable(
                orReplace, temporary, unlogged, globalOption, transient, @volatile, multiset));
        }
        else if (PeekKeyword(Keyword.MATERIALIZED)
            || PeekKeyword(Keyword.VIEW)
            || PeekKeywords(Keyword.SECURE, Keyword.MATERIALIZED, Keyword.VIEW)
            || PeekKeywords(Keyword.SECURE, Keyword.VIEW))
        {
            return Statement.From(ParseCreateView(orAlter, orReplace, temporary, createViewParams));
        }
        else if (ParseKeyword(Keyword.POLICY))
        {
            return Statement.From(ParseCreatePolicy());
        }
        else if (ParseKeyword(Keyword.EXTERNAL))
        {
            return Statement.From(ParseCreateExternalTable(orReplace));
        }
        else if (ParseKeyword(Keyword.FUNCTION))
        {
            return ParseCreateFunction(orAlter, orReplace, temporary);
        }
        else if (ParseKeyword(Keyword.DOMAIN))
        {
            return Statement.From(ParseCreateDomain());
        }
        else if (ParseKeyword(Keyword.TRIGGER))
        {
            return Statement.From(ParseCreateTrigger(temporary, orAlter, orReplace, false));
        }
        else if (ParseKeywords(Keyword.CONSTRAINT, Keyword.TRIGGER))
        {
            return Statement.From(ParseCreateTrigger(temporary, orAlter, orReplace, true));
        }
        else if (ParseKeyword(Keyword.MACRO))
        {
            return ParseCreateMacro(orReplace, temporary);
        }
        else if (ParseKeyword(Keyword.SECRET))
        {
            return ParseCreateSecret(orReplace, temporary, persistent);
        }
        else if (ParseKeyword(Keyword.USER))
        {
            return Statement.From(ParseCreateUser(orReplace));
        }
        else if (ParseKeyword(Keyword.SCHEMA))
        {
            return ParseCreateSchema(orReplace);
        }
        else if (ParseKeyword(Keyword.WAREHOUSE))
        {
            return Statement.From(ParseCreateWarehouse(orReplace));
        }
        else if (orReplace)
        {
            return ExpectedRef<Statement>(
                "[EXTERNAL] TABLE or [MATERIALIZED] VIEW or FUNCTION or SCHEMA or WAREHOUSE after CREATE OR REPLACE",
                PeekTokenRef());
        }
        else if (ParseKeyword(Keyword.EXTENSION))
        {
            return Statement.From(ParseCreateExtension());
        }
        else if (ParseKeyword(Keyword.INDEX))
        {
            return Statement.From(ParseCreateIndex(false));
        }
        else if (ParseKeywords(Keyword.UNIQUE, Keyword.INDEX))
        {
            return Statement.From(ParseCreateIndex(true));
        }
        else if (ParseKeyword(Keyword.VIRTUAL))
        {
            return ParseCreateVirtualTable();
        }
        else if (ParseKeyword(Keyword.DATABASE))
        {
            return ParseCreateDatabase();
        }
        else if (ParseKeyword(Keyword.ROLE))
        {
            return Statement.From(ParseCreateRole());
        }
        else if (ParseKeyword(Keyword.SEQUENCE))
        {
            return ParseCreateSequence(temporary);
        }
        else if (ParseKeyword(Keyword.COLLATION))
        {
            return Statement.From(ParseCreateCollation());
        }
        else if (ParseKeyword(Keyword.TYPE))
        {
            return ParseCreateType();
        }
        else if (ParseKeyword(Keyword.PROCEDURE))
        {
            return ParseCreateProcedure(orAlter);
        }
        else if (ParseKeyword(Keyword.CONNECTOR))
        {
            return Statement.From(ParseCreateConnector());
        }
        else if (ParseKeyword(Keyword.OPERATOR))
        {
            // Check if this is CREATE OPERATOR FAMILY or CREATE OPERATOR CLASS
            if (ParseKeyword(Keyword.FAMILY))
            {
                return Statement.From(ParseCreateOperatorFamily());
            }
            else if (ParseKeyword(Keyword.CLASS))
            {
                return Statement.From(ParseCreateOperatorClass());
            }
            else
            {
                return Statement.From(ParseCreateOperator());
            }
        }
        else if (ParseKeyword(Keyword.SERVER))
        {
            return ParsePgCreateServer();
        }
        else
        {
            return ExpectedRef<Statement>("an object type after CREATE", PeekTokenRef());
        }
    }

    internal TextSearchObjectType ParseTextSearchObjectType()
    {
        switch (ExpectOneOfKeywords(
                    Keyword.DICTIONARY,
                    Keyword.CONFIGURATION,
                    Keyword.TEMPLATE,
                    Keyword.PARSER))
        {
            case Keyword.DICTIONARY:
                return new TextSearchObjectType.Dictionary();
            case Keyword.CONFIGURATION:
                return new TextSearchObjectType.Configuration();
            case Keyword.TEMPLATE:
                return new TextSearchObjectType.Template();
            case Keyword.PARSER:
                return new TextSearchObjectType.Parser();
            case var unexpectedKeyword:
                // `{unexpected_keyword:?}`: Debug de Keyword = nome da variante (caso 46).
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: expected any of {{DICTIONARY, CONFIGURATION, TEMPLATE, PARSER}}, got {unexpectedKeyword.ToString()}"));
        }
    }

    /// <summary>Parse a <c>CREATE TEXT SEARCH ...</c> statement.</summary>
    public CreateTextSearch ParseCreateTextSearch()
    {
        ExpectKeywords(Keyword.TEXT, Keyword.SEARCH);
        var objectType = ParseTextSearchObjectType();
        var name = ParseObjectName(false);
        ExpectToken(new Token.LParen());
        var options = ParseCommaSeparated(p => p.ParseSqlOption());
        ExpectToken(new Token.RParen());
        return new CreateTextSearch(
            ObjectType: objectType,
            Name: name,
            Options: options);
    }

    internal AlterTextSearchOption ParseAlterTextSearchOption()
    {
        var key = ParseIdentifier();
        Expr? value;
        if (ConsumeToken(new Token.Eq()))
        {
            value = ParseExpr();
        }
        else
        {
            value = null;
        }
        return new AlterTextSearchOption(Key: key, Value: value);
    }

    /// <summary>Parse an <c>ALTER TEXT SEARCH ...</c> statement.</summary>
    public AlterTextSearch ParseAlterTextSearch()
    {
        ExpectKeywords(Keyword.TEXT, Keyword.SEARCH);
        var objectType = ParseTextSearchObjectType();
        var name = ParseObjectName(false);

        AlterTextSearchOperation operation;
        if (ParseKeywords(Keyword.RENAME, Keyword.TO))
        {
            operation = new AlterTextSearchOperation.RenameTo(
                NewName: ParseIdentifier());
        }
        else if (ParseKeywords(Keyword.OWNER, Keyword.TO))
        {
            operation = new AlterTextSearchOperation.OwnerTo(ParseOwner());
        }
        else if (ParseKeywords(Keyword.SET, Keyword.SCHEMA))
        {
            operation = new AlterTextSearchOperation.SetSchema(
                SchemaName: ParseObjectName(false));
        }
        else if (ConsumeToken(new Token.LParen()))
        {
            var options = ParseCommaSeparated(p => p.ParseAlterTextSearchOption());
            ExpectToken(new Token.RParen());
            operation = new AlterTextSearchOperation.SetOptions(Options: options);
        }
        else
        {
            var expected = "RENAME TO, OWNER TO, SET SCHEMA, or (...) after ALTER TEXT SEARCH";
            return ExpectedRef<AlterTextSearch>(expected, PeekTokenRef());
        }

        return new AlterTextSearch(
            ObjectType: objectType,
            Name: name,
            Operation: operation);
    }

    internal CreateUser ParseCreateUser(bool orReplace)
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = ParseIdentifier();
        var options = ParseKeyValueOptions(false, Keyword.WITH, Keyword.TAG)
            .Options;
        var withTags = ParseKeyword(Keyword.WITH);
        Sequence<KeyValueOption> tags;
        if (ParseKeyword(Keyword.TAG))
        {
            tags = ParseKeyValueOptions(true).Options;
        }
        else
        {
            tags = Sequence<KeyValueOption>.Empty;
        }
        return new CreateUser(
            OrReplace: orReplace,
            IfNotExists: ifNotExists,
            Name: name,
            Options: new KeyValueOptions(
                Options: options,
                Delimiter: KeyValueOptionsDelimiter.Space),
            WithTags: withTags,
            Tags: new KeyValueOptions(
                Options: tags,
                Delimiter: KeyValueOptionsDelimiter.Comma));
    }

    /// <summary>Parse a <c>CREATE WAREHOUSE</c> statement.</summary>
    /// <remarks>See <see href="https://docs.snowflake.com/en/sql-reference/sql/create-warehouse"/></remarks>
    internal CreateWarehouse ParseCreateWarehouse(bool orReplace)
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = ParseObjectName(false);
        _ = ParseKeyword(Keyword.WITH);
        var options = ParseKeyValueOptions(false);
        return new CreateWarehouse(
            OrReplace: orReplace,
            IfNotExists: ifNotExists,
            Name: name,
            Options: options);
    }

    /// <summary>
    /// See <see href="https://duckdb.org/docs/sql/statements/create_secret.html">DuckDB Docs</see> for more details.
    /// </summary>
    public Statement ParseCreateSecret(
        bool orReplace,
        bool temporary,
        bool persistent)
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);

        Ident? storageSpecifier = null;
        Ident? name = null;
        if (PeekTokenRef().Token is not Token.LParen)
        {
            if (ParseKeyword(Keyword.IN))
            {
                // self.parse_identifier().ok(): o erro vira None, sem restaurar a posição.
                try
                {
                    storageSpecifier = ParseIdentifier();
                }
                catch (ParserException)
                {
                    storageSpecifier = null;
                }
            }
            else
            {
                try
                {
                    name = ParseIdentifier();
                }
                catch (ParserException)
                {
                    name = null;
                }
            }

            // Storage specifier may follow the name
            if (storageSpecifier is null
                && PeekTokenRef().Token is not Token.LParen
                && ParseKeyword(Keyword.IN))
            {
                try
                {
                    storageSpecifier = ParseIdentifier();
                }
                catch (ParserException)
                {
                    storageSpecifier = null;
                }
            }
        }

        ExpectToken(new Token.LParen());
        ExpectKeywordIs(Keyword.TYPE);
        var secretType = ParseIdentifier();

        var options = new List<SecretOption>();
        if (ConsumeToken(new Token.Comma()))
        {
            options.AddRange(ParseCommaSeparated(p =>
            {
                var key = p.ParseIdentifier();
                var value = p.ParseIdentifier();
                return new SecretOption(Key: key, Value: value);
            }));
        }
        ExpectToken(new Token.RParen());

        bool? temp;
        switch ((temporary, persistent))
        {
            case (true, false):
                temp = true;
                break;
            case (false, true):
                temp = false;
                break;
            case (false, false):
                temp = null;
                break;
            default:
                temp = ExpectedRef<bool?>("TEMPORARY or PERSISTENT", PeekTokenRef());
                break;
        }

        return new Statement.CreateSecret(
            OrReplace: orReplace,
            Temporary: temp,
            IfNotExists: ifNotExists,
            Name: name,
            StorageSpecifier: storageSpecifier,
            SecretType: secretType,
            Options: options.ToSequence());
    }

    /// <summary>Parse a CACHE TABLE statement</summary>
    public Statement ParseCacheTable()
    {
        ObjectName? tableFlag = null;
        var options = Sequence<SqlOption>.Empty;
        var hasAs = false;
        Query? query = null;
        if (ParseKeyword(Keyword.TABLE))
        {
            var tableName = ParseObjectName(false);
            if (PeekTokenRef().Token is not Token.EOF)
            {
                if (PeekTokenRef().Token is Token.Word { Value: var word })
                {
                    if (word.Keyword == Keyword.OPTIONS)
                    {
                        options = ParseOptions(Keyword.OPTIONS);
                    }
                }

                if (PeekTokenRef().Token is not Token.EOF)
                {
                    var (a, q) = ParseAsQuery();
                    hasAs = a;
                    query = q;
                }

                return new Statement.Cache(
                    TableFlag: tableFlag,
                    TableName: tableName,
                    HasAs: hasAs,
                    Options: options,
                    Query: query);
            }
            else
            {
                return new Statement.Cache(
                    TableFlag: tableFlag,
                    TableName: tableName,
                    HasAs: hasAs,
                    Options: options,
                    Query: query);
            }
        }
        else
        {
            tableFlag = ParseObjectName(false);
            if (ParseKeyword(Keyword.TABLE))
            {
                var tableName = ParseObjectName(false);
                if (PeekTokenRef().Token is not Token.EOF)
                {
                    if (PeekTokenRef().Token is Token.Word { Value: var word })
                    {
                        if (word.Keyword == Keyword.OPTIONS)
                        {
                            options = ParseOptions(Keyword.OPTIONS);
                        }
                    }

                    if (PeekTokenRef().Token is not Token.EOF)
                    {
                        var (a, q) = ParseAsQuery();
                        hasAs = a;
                        query = q;
                    }

                    return new Statement.Cache(
                        TableFlag: tableFlag,
                        TableName: tableName,
                        HasAs: hasAs,
                        Options: options,
                        Query: query);
                }
                else
                {
                    return new Statement.Cache(
                        TableFlag: tableFlag,
                        TableName: tableName,
                        HasAs: hasAs,
                        Options: options,
                        Query: query);
                }
            }
            else
            {
                if (PeekTokenRef().Token is Token.EOF)
                {
                    PrevToken();
                }
                return ExpectedRef<Statement>("a `TABLE` keyword", PeekTokenRef());
            }
        }
    }

    /// <summary>Parse 'AS' before as query,such as <c>WITH XXX AS SELECT XXX</c> oer <c>CACHE TABLE AS SELECT XXX</c></summary>
    public (bool HasAs, Query Query) ParseAsQuery()
    {
        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var word }:
                switch (word.Keyword)
                {
                    case Keyword.AS:
                        NextToken();
                        return (true, ParseQuery());
                    default:
                        return (false, ParseQuery());
                }
            default:
                return ExpectedRef<(bool, Query)>("a QUERY statement", PeekTokenRef());
        }
    }

    /// <summary>Parse a UNCACHE TABLE statement</summary>
    public Statement ParseUncacheTable()
    {
        ExpectKeywordIs(Keyword.TABLE);
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var tableName = ParseObjectName(false);
        return new Statement.UNCache(
            TableName: tableName,
            IfExists: ifExists);
    }

    /// <summary>SQLite-specific <c>CREATE VIRTUAL TABLE</c></summary>
    public Statement ParseCreateVirtualTable()
    {
        ExpectKeywordIs(Keyword.TABLE);
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var tableName = ParseObjectName(false);
        ExpectKeywordIs(Keyword.USING);
        var moduleName = ParseIdentifier();
        // SQLite docs note that module "arguments syntax is sufficiently
        // general that the arguments can be made to appear as column
        // definitions in a traditional CREATE TABLE statement", but
        // we don't implement that.
        var moduleArgs = ParseParenthesizedColumnList(IsOptional.Optional, false);
        return new Statement.CreateVirtualTable(
            Name: tableName,
            IfNotExists: ifNotExists,
            ModuleName: moduleName,
            ModuleArgs: moduleArgs);
    }

    /// <summary>Parse a <c>CREATE SCHEMA</c> statement.</summary>
    public Statement ParseCreateSchema(bool orReplace)
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);

        var schemaName = ParseSchemaName();

        Expr? defaultCollateSpec;
        if (ParseKeywords(Keyword.DEFAULT, Keyword.COLLATE))
        {
            defaultCollateSpec = ParseExpr();
        }
        else
        {
            defaultCollateSpec = null;
        }

        Sequence<SqlOption>? @with;
        if (PeekKeyword(Keyword.WITH))
        {
            @with = ParseOptions(Keyword.WITH);
        }
        else
        {
            @with = null;
        }

        Sequence<SqlOption>? options;
        if (PeekKeyword(Keyword.OPTIONS))
        {
            options = ParseOptions(Keyword.OPTIONS);
        }
        else
        {
            options = null;
        }

        ObjectName? clone;
        if (ParseKeyword(Keyword.CLONE))
        {
            clone = ParseObjectName(false);
        }
        else
        {
            clone = null;
        }

        return new Statement.CreateSchema(
            SchemaName: schemaName,
            OrReplace: orReplace,
            IfNotExists: ifNotExists,
            With: @with,
            Options: options,
            DefaultCollateSpec: defaultCollateSpec,
            CloneValue: clone);
    }

    internal SchemaName ParseSchemaName()
    {
        if (ParseKeyword(Keyword.AUTHORIZATION))
        {
            return new SchemaName.UnnamedAuthorization(ParseIdentifier());
        }
        else
        {
            var name = ParseObjectName(false);

            if (ParseKeyword(Keyword.AUTHORIZATION))
            {
                return new SchemaName.NamedAuthorization(
                    name,
                    ParseIdentifier());
            }
            else
            {
                return new SchemaName.Simple(name);
            }
        }
    }

    /// <summary>Parse a <c>CREATE DATABASE</c> statement.</summary>
    public Statement ParseCreateDatabase()
    {
        var ine = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var dbName = ParseObjectName(false);
        string? location = null;
        string? managedLocation = null;
        while (true)
        {
            var stop = false;
            switch (ParseOneOfKeywords(Keyword.LOCATION, Keyword.MANAGEDLOCATION))
            {
                case Keyword.LOCATION:
                    location = ParseLiteralString();
                    break;
                case Keyword.MANAGEDLOCATION:
                    managedLocation = ParseLiteralString();
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
        ObjectName? clone;
        if (ParseKeyword(Keyword.CLONE))
        {
            clone = ParseObjectName(false);
        }
        else
        {
            clone = null;
        }

        // Parse MySQL-style [DEFAULT] CHARACTER SET and [DEFAULT] COLLATE options
        //
        // Note: The docs only mention `CHARACTER SET`, but `CHARSET` is also supported.
        // Furthermore, MySQL will only accept one character set, raising an error if there is more
        // than one, but will accept multiple collations and use the last one.
        //
        // <https://dev.mysql.com/doc/refman/8.4/en/create-database.html>
        string? defaultCharset = null;
        string? defaultCollation = null;
        while (true)
        {
            var hasDefault = ParseKeyword(Keyword.DEFAULT);
            if ((defaultCharset is null && ParseKeywords(Keyword.CHARACTER, Keyword.SET))
                || ParseKeyword(Keyword.CHARSET))
            {
                _ = ConsumeToken(new Token.Eq());
                defaultCharset = ParseIdentifier().Value;
            }
            else if (ParseKeyword(Keyword.COLLATE))
            {
                _ = ConsumeToken(new Token.Eq());
                defaultCollation = ParseIdentifier().Value;
            }
            else if (hasDefault)
            {
                // DEFAULT keyword not followed by CHARACTER SET, CHARSET, or COLLATE
                PrevToken();
                break;
            }
            else
            {
                break;
            }
        }

        return new Statement.CreateDatabase(
            DbName: dbName,
            IfNotExists: ine,
            Location: location,
            ManagedLocation: managedLocation,
            OrReplace: false,
            Transient: false,
            CloneValue: clone,
            DataRetentionTimeInDays: null,
            MaxDataExtensionTimeInDays: null,
            ExternalVolume: null,
            Catalog: null,
            ReplaceInvalidCharacters: null,
            DefaultDdlCollation: null,
            StorageSerializationPolicy: null,
            Comment: null,
            DefaultCharset: defaultCharset,
            DefaultCollation: defaultCollation,
            CatalogSync: null,
            CatalogSyncNamespaceMode: null,
            CatalogSyncNamespaceFlattenDelimiter: null,
            WithTags: null,
            WithContacts: null);
    }

    /// <summary>Parse an optional <c>USING</c> clause for <c>CREATE FUNCTION</c>.</summary>
    public CreateFunctionUsing? ParseOptionalCreateFunctionUsing()
    {
        if (!ParseKeyword(Keyword.USING))
        {
            return null;
        }
        var keyword =
            ExpectOneOfKeywords(Keyword.JAR, Keyword.FILE, Keyword.ARCHIVE);

        var uri = ParseLiteralString();

        switch (keyword)
        {
            case Keyword.JAR:
                return new CreateFunctionUsing.Jar(uri);
            case Keyword.FILE:
                return new CreateFunctionUsing.File(uri);
            case Keyword.ARCHIVE:
                return new CreateFunctionUsing.Archive(uri);
            default:
                // O texto "{:?}" é literal no Rust (não é um format!); `format!("{keyword:?}")` = nome da variante (caso 46).
                return Expected<CreateFunctionUsing?>(
                    "JAR, FILE or ARCHIVE, got {:?}",
                    TokenWithSpan.Wrap(Token.MakeKeyword(keyword.ToString())));
        }
    }

    /// <summary>Parse a <c>CREATE FUNCTION</c> statement.</summary>
    public Statement ParseCreateFunction(
        bool orAlter,
        bool orReplace,
        bool temporary)
    {
        if (_dialect is HiveDialect)
        {
            return Statement.From(ParseHiveCreateFunction(orReplace, temporary));
        }
        else if (_dialect is PostgreSqlDialect or GenericDialect)
        {
            return Statement.From(ParsePostgresCreateFunction(orReplace, temporary));
        }
        else if (_dialect is DuckDbDialect)
        {
            return ParseCreateMacro(orReplace, temporary);
        }
        else if (_dialect is BigQueryDialect)
        {
            return Statement.From(ParseBigqueryCreateFunction(orReplace, temporary));
        }
        else if (_dialect is MsSqlDialect)
        {
            return Statement.From(ParseMssqlCreateFunction(orAlter, orReplace, temporary));
        }
        else
        {
            PrevToken();
            return ExpectedRef<Statement>("an object type after CREATE", PeekTokenRef());
        }
    }

    // Rust: `#[derive(Default)] struct Body { .. }` local de parse_postgres_create_function →
    // classe privada aninhada com o nome mecânico (convencoes.md §13, caso 67); `Body::default()` → `new Body()`.
    private sealed class Body
    {
        public Ident? Language;
        public FunctionBehavior? Behavior;
        public CreateFunctionBody? FunctionBody;
        public FunctionCalledOnNull? CalledOnNull;
        public FunctionParallel? Parallel;
        public FunctionSecurity? Security;
    }

    /// <summary>Parse <c>CREATE FUNCTION</c> for <see href="https://www.postgresql.org/docs/15/sql-createfunction.html">PostgreSQL</see></summary>
    internal CreateFunction ParsePostgresCreateFunction(
        bool orReplace,
        bool temporary)
    {
        var name = ParseObjectName(false);

        ExpectToken(new Token.LParen());
        Sequence<OperateFunctionArg> args;
        if (PeekTokenRef().Token is not Token.RParen)
        {
            args = ParseCommaSeparated(p => p.ParseFunctionArg());
        }
        else
        {
            args = Sequence<OperateFunctionArg>.Empty;
        }
        ExpectToken(new Token.RParen());

        FunctionReturnType? returnType;
        if (ParseKeyword(Keyword.RETURNS))
        {
            returnType = ParseFunctionReturnType();
        }
        else
        {
            returnType = null;
        }

        var body = new Body();
        var setParams = new List<FunctionDefinitionSetParam>();

        // Rust: `fn ensure_not_set<T>` aninhada (declarada dentro do loop) → função local estática.
        static void EnsureNotSet<T>(T? field, string name)
            where T : class
        {
            if (field is not null)
            {
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"{name} specified more than once"));
            }
        }

        while (true)
        {
            if (ParseKeyword(Keyword.AS))
            {
                EnsureNotSet(body.FunctionBody, "AS");
                body.FunctionBody = ParseCreateFunctionBodyString();
            }
            else if (ParseKeyword(Keyword.LANGUAGE))
            {
                EnsureNotSet(body.Language, "LANGUAGE");
                body.Language = ParseIdentifier();
            }
            else if (ParseKeyword(Keyword.IMMUTABLE))
            {
                EnsureNotSet(body.Behavior, "IMMUTABLE | STABLE | VOLATILE");
                body.Behavior = new FunctionBehavior.Immutable();
            }
            else if (ParseKeyword(Keyword.STABLE))
            {
                EnsureNotSet(body.Behavior, "IMMUTABLE | STABLE | VOLATILE");
                body.Behavior = new FunctionBehavior.Stable();
            }
            else if (ParseKeyword(Keyword.VOLATILE))
            {
                EnsureNotSet(body.Behavior, "IMMUTABLE | STABLE | VOLATILE");
                body.Behavior = new FunctionBehavior.Volatile();
            }
            else if (ParseKeywords(
                         Keyword.CALLED,
                         Keyword.ON,
                         Keyword.NULL,
                         Keyword.INPUT))
            {
                EnsureNotSet(
                    body.CalledOnNull,
                    "CALLED ON NULL INPUT | RETURNS NULL ON NULL INPUT | STRICT");
                body.CalledOnNull = new FunctionCalledOnNull.CalledOnNullInput();
            }
            else if (ParseKeywords(
                         Keyword.RETURNS,
                         Keyword.NULL,
                         Keyword.ON,
                         Keyword.NULL,
                         Keyword.INPUT))
            {
                EnsureNotSet(
                    body.CalledOnNull,
                    "CALLED ON NULL INPUT | RETURNS NULL ON NULL INPUT | STRICT");
                body.CalledOnNull = new FunctionCalledOnNull.ReturnsNullOnNullInput();
            }
            else if (ParseKeyword(Keyword.STRICT))
            {
                EnsureNotSet(
                    body.CalledOnNull,
                    "CALLED ON NULL INPUT | RETURNS NULL ON NULL INPUT | STRICT");
                body.CalledOnNull = new FunctionCalledOnNull.Strict();
            }
            else if (ParseKeyword(Keyword.PARALLEL))
            {
                EnsureNotSet(body.Parallel, "PARALLEL { UNSAFE | RESTRICTED | SAFE }");
                if (ParseKeyword(Keyword.UNSAFE))
                {
                    body.Parallel = new FunctionParallel.Unsafe();
                }
                else if (ParseKeyword(Keyword.RESTRICTED))
                {
                    body.Parallel = new FunctionParallel.Restricted();
                }
                else if (ParseKeyword(Keyword.SAFE))
                {
                    body.Parallel = new FunctionParallel.Safe();
                }
                else
                {
                    return ExpectedRef<CreateFunction>("one of UNSAFE | RESTRICTED | SAFE", PeekTokenRef());
                }
            }
            else if (ParseKeyword(Keyword.SECURITY))
            {
                EnsureNotSet(body.Security, "SECURITY { DEFINER | INVOKER }");
                if (ParseKeyword(Keyword.DEFINER))
                {
                    body.Security = new FunctionSecurity.Definer();
                }
                else if (ParseKeyword(Keyword.INVOKER))
                {
                    body.Security = new FunctionSecurity.Invoker();
                }
                else
                {
                    return ExpectedRef<CreateFunction>("DEFINER or INVOKER", PeekTokenRef());
                }
            }
            else if (ParseKeyword(Keyword.SET))
            {
                // Rust: `let name` sombreia o nome da função (caso 72).
                var paramName = ParseObjectName(false);
                FunctionSetValue value;
                if (ParseKeywords(Keyword.FROM, Keyword.CURRENT))
                {
                    value = new FunctionSetValue.FromCurrent();
                }
                else
                {
                    if (!ConsumeToken(new Token.Eq()) && !ParseKeyword(Keyword.TO))
                    {
                        return ExpectedRef<CreateFunction>("= or TO", PeekTokenRef());
                    }
                    if (ParseKeyword(Keyword.DEFAULT))
                    {
                        value = new FunctionSetValue.Default();
                    }
                    else
                    {
                        var values = ParseCommaSeparated(p => p.ParseExpr());
                        value = new FunctionSetValue.Values(values);
                    }
                }
                setParams.Add(new FunctionDefinitionSetParam(Name: paramName, Value: value));
            }
            else if (ParseKeyword(Keyword.RETURN))
            {
                EnsureNotSet(body.FunctionBody, "RETURN");
                body.FunctionBody = new CreateFunctionBody.Return(ParseExpr());
            }
            else
            {
                break;
            }
        }

        return new CreateFunction(
            OrAlter: false,
            OrReplace: orReplace,
            Temporary: temporary,
            Name: name,
            Args: args,
            ReturnType: returnType,
            Behavior: body.Behavior,
            CalledOnNull: body.CalledOnNull,
            Parallel: body.Parallel,
            Security: body.Security,
            SetParams: setParams.ToSequence(),
            Language: body.Language,
            FunctionBody: body.FunctionBody,
            IfNotExists: false,
            Using: null,
            DeterminismSpecifier: null,
            Options: null,
            RemoteConnection: null);
    }

    /// <summary>
    /// Parse <c>CREATE FUNCTION</c> for
    /// <see href="https://cwiki.apache.org/confluence/display/hive/languagemanual+ddl#LanguageManualDDL-Create/Drop/ReloadFunction">Hive</see>
    /// </summary>
    internal CreateFunction ParseHiveCreateFunction(
        bool orReplace,
        bool temporary)
    {
        var name = ParseObjectName(false);
        ExpectKeywordIs(Keyword.AS);

        var body = ParseCreateFunctionBodyString();
        var @using = ParseOptionalCreateFunctionUsing();

        return new CreateFunction(
            OrAlter: false,
            OrReplace: orReplace,
            Temporary: temporary,
            Name: name,
            FunctionBody: body,
            Using: @using,
            IfNotExists: false,
            Args: null,
            ReturnType: null,
            Behavior: null,
            CalledOnNull: null,
            Parallel: null,
            Security: null,
            SetParams: Sequence<FunctionDefinitionSetParam>.Empty,
            Language: null,
            DeterminismSpecifier: null,
            Options: null,
            RemoteConnection: null);
    }

    /// <summary>
    /// Parse <c>CREATE FUNCTION</c> for
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_function_statement">BigQuery</see>
    /// </summary>
    internal CreateFunction ParseBigqueryCreateFunction(
        bool orReplace,
        bool temporary)
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var (name, args) = ParseCreateFunctionNameAndParams();

        FunctionReturnType? returnType;
        if (ParseKeyword(Keyword.RETURNS))
        {
            returnType = ParseFunctionReturnType();
        }
        else
        {
            returnType = null;
        }

        FunctionDeterminismSpecifier? determinismSpecifier;
        if (ParseKeyword(Keyword.DETERMINISTIC))
        {
            determinismSpecifier = new FunctionDeterminismSpecifier.Deterministic();
        }
        else if (ParseKeywords(Keyword.NOT, Keyword.DETERMINISTIC))
        {
            determinismSpecifier = new FunctionDeterminismSpecifier.NotDeterministic();
        }
        else
        {
            determinismSpecifier = null;
        }

        Ident? language;
        if (ParseKeyword(Keyword.LANGUAGE))
        {
            language = ParseIdentifier();
        }
        else
        {
            language = null;
        }

        ObjectName? remoteConnection;
        if (ParseKeywords(Keyword.REMOTE, Keyword.WITH, Keyword.CONNECTION))
        {
            remoteConnection = ParseObjectName(false);
        }
        else
        {
            remoteConnection = null;
        }

        // `OPTIONS` may come before of after the function body but
        // may be specified at most once.
        var options = MaybeParseOptions(Keyword.OPTIONS);

        CreateFunctionBody? functionBody;
        if (remoteConnection is null)
        {
            ExpectKeywordIs(Keyword.AS);
            var expr = ParseExpr();
            if (options is null)
            {
                options = MaybeParseOptions(Keyword.OPTIONS);
                functionBody = new CreateFunctionBody.AsBeforeOptions(
                    Body: expr,
                    LinkSymbol: null);
            }
            else
            {
                functionBody = new CreateFunctionBody.AsAfterOptions(expr);
            }
        }
        else
        {
            functionBody = null;
        }

        return new CreateFunction(
            OrAlter: false,
            OrReplace: orReplace,
            Temporary: temporary,
            IfNotExists: ifNotExists,
            Name: name,
            Args: args,
            ReturnType: returnType,
            FunctionBody: functionBody,
            Language: language,
            DeterminismSpecifier: determinismSpecifier,
            Options: options,
            RemoteConnection: remoteConnection,
            Using: null,
            Behavior: null,
            CalledOnNull: null,
            Parallel: null,
            Security: null,
            SetParams: Sequence<FunctionDefinitionSetParam>.Empty);
    }

    /// <summary>
    /// Parse <c>CREATE FUNCTION</c> for
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql">MsSql</see>
    /// </summary>
    internal CreateFunction ParseMssqlCreateFunction(
        bool orAlter,
        bool orReplace,
        bool temporary)
    {
        var (name, args) = ParseCreateFunctionNameAndParams();

        ExpectKeyword(Keyword.RETURNS);

        var returnTable = MaybeParse<DataType>(p =>
        {
            var returnTableName = p.ParseIdentifier();

            p.ExpectKeywordIs(Keyword.TABLE);
            p.PrevToken();

            Sequence<ColumnDef> tableColumnDefs;
            switch (p.ParseDataType())
            {
                case DataType.Table { Value: { } defs } when !defs.IsEmpty:
                    tableColumnDefs = defs;
                    break;
                default:
                    throw ParserErr(
                        "Expected table column definitions after TABLE keyword",
                        p.PeekTokenRef().Span.Start);
            }

            return new DataType.NamedTable(
                Name: new ObjectName(Sequence.Of<ObjectNamePart>(new ObjectNamePart.Identifier(returnTableName))),
                Columns: tableColumnDefs);
        });

        DataType dataType;
        if (returnTable is { } tableType)
        {
            dataType = tableType;
        }
        else
        {
            dataType = ParseDataType();
        }
        FunctionReturnType? returnType = new FunctionReturnType.DataType(dataType);

        _ = ParseKeyword(Keyword.AS);

        CreateFunctionBody? functionBody;
        if (PeekKeyword(Keyword.BEGIN))
        {
            var beginToken = ExpectKeyword(Keyword.BEGIN);
            var statements = ParseStatementList(Keyword.END);
            var endToken = ExpectKeyword(Keyword.END);

            functionBody = new CreateFunctionBody.AsBeginEnd(new BeginEndStatements(
                BeginToken: new AttachedToken(beginToken),
                Statements: statements,
                EndToken: new AttachedToken(endToken)));
        }
        else if (ParseKeyword(Keyword.RETURN))
        {
            if (PeekTokenRef().Token is Token.LParen)
            {
                functionBody = new CreateFunctionBody.AsReturnExpr(ParseExpr());
            }
            else if (PeekKeyword(Keyword.SELECT))
            {
                var select = ParseSelect();
                functionBody = new CreateFunctionBody.AsReturnSelect(select);
            }
            else
            {
                throw ParserErr(
                    "Expected a subquery (or bare SELECT statement) after RETURN",
                    PeekTokenRef().Span.Start);
            }
        }
        else
        {
            throw ParserErr("Unparsable function body", PeekTokenRef().Span.Start);
        }

        return new CreateFunction(
            OrAlter: orAlter,
            OrReplace: orReplace,
            Temporary: temporary,
            IfNotExists: false,
            Name: name,
            Args: args,
            ReturnType: returnType,
            FunctionBody: functionBody,
            Language: null,
            DeterminismSpecifier: null,
            Options: null,
            RemoteConnection: null,
            Using: null,
            Behavior: null,
            CalledOnNull: null,
            Parallel: null,
            Security: null,
            SetParams: Sequence<FunctionDefinitionSetParam>.Empty);
    }

    internal FunctionReturnType ParseFunctionReturnType()
    {
        if (ParseKeyword(Keyword.SETOF))
        {
            return new FunctionReturnType.SetOf(ParseDataType());
        }
        else
        {
            return new FunctionReturnType.DataType(ParseDataType());
        }
    }

    internal (ObjectName Name, Sequence<OperateFunctionArg> Args) ParseCreateFunctionNameAndParams()
    {
        var name = ParseObjectName(false);
        Func<Parser, OperateFunctionArg> parseFunctionParam =
            parser =>
            {
                // Rust: `let name` da closure sombreia o `name` externo (caso 72).
                var paramName = parser.ParseIdentifier();
                var dataType = parser.ParseDataType();
                Expr? defaultExpr;
                if (parser.ConsumeToken(new Token.Eq()))
                {
                    defaultExpr = parser.ParseExpr();
                }
                else
                {
                    defaultExpr = null;
                }

                return new OperateFunctionArg(
                    Mode: null,
                    Name: paramName,
                    DataType: dataType,
                    DefaultExpr: defaultExpr);
            };
        ExpectToken(new Token.LParen());
        var args = ParseCommaSeparated0(parseFunctionParam, new Token.RParen());
        ExpectToken(new Token.RParen());
        return (name, args);
    }

    internal OperateFunctionArg ParseFunctionArg()
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
        else if (ParseKeyword(Keyword.VARIADIC))
        {
            mode = new ArgMode.Variadic();
        }
        else
        {
            mode = null;
        }

        // parse: [ argname ] argtype
        Ident? name = null;
        var dataType = ParseDataType();

        // To check whether the first token is a name or a type, we need to
        // peek the next token, which if it is another type keyword, then the
        // first token is a name and not a type in itself.
        var dataTypeIdx = GetCurrentIndex();

        // DEFAULT will be parsed as `DataType::Custom`, which is undesirable in this context
        static DataType ParseDataTypeNoDefault(Parser parser)
        {
            if (parser.PeekKeyword(Keyword.DEFAULT))
            {
                // This dummy error is ignored in `maybe_parse`
                throw ParserErr(
                    "The DEFAULT keyword is not a type",
                    parser.PeekTokenRef().Span.Start);
            }
            else
            {
                return parser.ParseDataType();
            }
        }

        if (MaybeParse<DataType>(ParseDataTypeNoDefault) is { } nextDataType)
        {
            var token = TokenAt(dataTypeIdx);

            // We ensure that the token is a `Word` token, and not other special tokens.
            switch (token.Token)
            {
                case Token.Word { Value: var word }:
                    name = word.IntoIdent(token.Span);
                    break;
                default:
                    return Expected<OperateFunctionArg>("a name or type", token);
            }

            dataType = nextDataType;
        }

        Expr? defaultExpr;
        if (ParseKeyword(Keyword.DEFAULT) || ConsumeToken(new Token.Eq()))
        {
            defaultExpr = ParseExpr();
        }
        else
        {
            defaultExpr = null;
        }
        return new OperateFunctionArg(
            Mode: mode,
            Name: name,
            DataType: dataType,
            DefaultExpr: defaultExpr);
    }

    internal OperateFunctionArg ParseAggregateFunctionArg()
    {
        ArgMode? mode;
        if (ParseKeyword(Keyword.IN))
        {
            mode = new ArgMode.In();
        }
        else
        {
            if (PeekOneOfKeywords(Keyword.OUT, Keyword.INOUT, Keyword.VARIADIC) is not null)
            {
                return ExpectedRef<OperateFunctionArg>(
                    "IN or argument type in aggregate signature",
                    PeekTokenRef());
            }
            mode = null;
        }

        // Parse: [ argname ] argtype, but do not consume ORDER from
        // `... argtype ORDER BY ...` as a type-name disambiguator.
        Ident? name = null;
        var dataType = ParseDataType();
        var dataTypeIdx = GetCurrentIndex();

        static DataType ParseDataTypeForAggregateArg(Parser parser)
        {
            if (parser.PeekKeyword(Keyword.DEFAULT)
                || parser.PeekKeyword(Keyword.ORDER)
                || parser.PeekTokenRef().Token is Token.Comma
                || parser.PeekTokenRef().Token is Token.RParen)
            {
                // Dummy error ignored by maybe_parse
                throw ParserErr(
                    "The current token cannot start an aggregate argument type",
                    parser.PeekTokenRef().Span.Start);
            }
            else
            {
                return parser.ParseDataType();
            }
        }

        if (MaybeParse<DataType>(ParseDataTypeForAggregateArg) is { } nextDataType)
        {
            var token = TokenAt(dataTypeIdx);
            switch (token.Token)
            {
                case Token.Word { Value: var word }:
                    name = word.IntoIdent(token.Span);
                    break;
                default:
                    return Expected<OperateFunctionArg>("a name or type", token);
            }

            dataType = nextDataType;
        }

        if (PeekKeyword(Keyword.DEFAULT) || PeekTokenRef().Token is Token.Eq)
        {
            return ExpectedRef<OperateFunctionArg>(
                "',' or ')' or ORDER BY after aggregate argument type",
                PeekTokenRef());
        }

        return new OperateFunctionArg(
            Mode: mode,
            Name: name,
            DataType: dataType,
            DefaultExpr: null);
    }

    /// <summary>
    /// Parse statements of the DropTrigger type such as:
    /// <code>
    /// DROP TRIGGER [ IF EXISTS ] name ON table_name [ CASCADE | RESTRICT ]
    /// </code>
    /// </summary>
    public DropTrigger ParseDropTrigger()
    {
        if (!(_dialect is PostgreSqlDialect or SQLiteDialect or GenericDialect or MySqlDialect or MsSqlDialect))
        {
            PrevToken();
            return ExpectedRef<DropTrigger>("an object type after DROP", PeekTokenRef());
        }
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var triggerName = ParseObjectName(false);
        ObjectName? tableName;
        if (ParseKeyword(Keyword.ON))
        {
            tableName = ParseObjectName(false);
        }
        else
        {
            tableName = null;
        }
        ReferentialAction? option;
        switch (ParseOneOfKeywords(Keyword.CASCADE, Keyword.RESTRICT))
        {
            case Keyword.CASCADE:
                option = new ReferentialAction.Cascade();
                break;
            case Keyword.RESTRICT:
                option = new ReferentialAction.Restrict();
                break;
            case { } unexpectedKeyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: expected any of {{CASCADE, RESTRICT}}, got {unexpectedKeyword.ToString()}"));
            case null:
                option = null;
                break;
        }
        return new DropTrigger(
            IfExists: ifExists,
            TriggerName: triggerName,
            TableName: tableName,
            Option: option);
    }

    /// <summary>Parse a <c>CREATE TRIGGER</c> statement.</summary>
    public CreateTrigger ParseCreateTrigger(
        bool temporary,
        bool orAlter,
        bool orReplace,
        bool isConstraint)
    {
        if (!(_dialect is PostgreSqlDialect or SQLiteDialect or GenericDialect or MySqlDialect or MsSqlDialect))
        {
            PrevToken();
            return ExpectedRef<CreateTrigger>("an object type after CREATE", PeekTokenRef());
        }

        var name = ParseObjectName(false);
        var period = MaybeParse(parser => parser.ParseTriggerPeriod());

        var events = ParseKeywordSeparated(Keyword.OR, p => p.ParseTriggerEvent());
        ExpectKeywordIs(Keyword.ON);
        var tableName = ParseObjectName(false);

        ObjectName? referencedTableName;
        if (ParseKeyword(Keyword.FROM))
        {
            // self.parse_object_name(true).ok(): o erro vira None, sem restaurar a posição.
            try
            {
                referencedTableName = ParseObjectName(true);
            }
            catch (ParserException)
            {
                referencedTableName = null;
            }
        }
        else
        {
            referencedTableName = null;
        }

        var characteristics = ParseConstraintCharacteristics();

        var referencing = new List<TriggerReferencing>();
        if (ParseKeyword(Keyword.REFERENCING))
        {
            while (ParseTriggerReferencing() is { } refer)
            {
                referencing.Add(refer);
            }
        }

        TriggerObjectKind? triggerObject;
        if (ParseKeyword(Keyword.FOR))
        {
            var includeEach = ParseKeyword(Keyword.EACH);
            // Rust: `let trigger_object` interno sombreia o externo (caso 72).
            TriggerObject triggerObjectValue;
            switch (ExpectOneOfKeywords(Keyword.ROW, Keyword.STATEMENT))
            {
                case Keyword.ROW:
                    triggerObjectValue = new TriggerObject.Row();
                    break;
                case Keyword.STATEMENT:
                    triggerObjectValue = new TriggerObject.Statement();
                    break;
                case var unexpectedKeyword:
                    // `{unexpected_keyword}`: Display de Keyword (ToSql, convencoes.md §13, caso 3).
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in ROW/STATEMENT"));
            }

            if (includeEach)
            {
                triggerObject = new TriggerObjectKind.ForEach(triggerObjectValue);
            }
            else
            {
                triggerObject = new TriggerObjectKind.For(triggerObjectValue);
            }
        }
        else
        {
            _ = ParseKeyword(Keyword.FOR);

            triggerObject = null;
        }

        // self.parse_keyword(WHEN).then(|| self.parse_expr()).transpose()?
        Expr? condition;
        if (ParseKeyword(Keyword.WHEN))
        {
            condition = ParseExpr();
        }
        else
        {
            condition = null;
        }

        TriggerExecBody? execBody = null;
        ConditionalStatements? statements = null;
        if (ParseKeyword(Keyword.EXECUTE))
        {
            execBody = ParseTriggerExecBody();
        }
        else
        {
            statements = ParseConditionalStatements(Keyword.END);
        }

        return new CreateTrigger(
            OrAlter: orAlter,
            Temporary: temporary,
            OrReplace: orReplace,
            IsConstraint: isConstraint,
            Name: name,
            Period: period,
            PeriodBeforeTable: true,
            Events: events,
            TableName: tableName,
            ReferencedTableName: referencedTableName,
            Referencing: referencing.ToSequence(),
            TriggerObject: triggerObject,
            Condition: condition,
            ExecBody: execBody,
            StatementsAs: false,
            Statements: statements,
            Characteristics: characteristics);
    }

    /// <summary>Parse the period part of a trigger (<c>BEFORE</c>, <c>AFTER</c>, etc.).</summary>
    public TriggerPeriod ParseTriggerPeriod()
    {
        switch (ExpectOneOfKeywords(
                    Keyword.FOR,
                    Keyword.BEFORE,
                    Keyword.AFTER,
                    Keyword.INSTEAD))
        {
            case Keyword.FOR:
                return new TriggerPeriod.For();
            case Keyword.BEFORE:
                return new TriggerPeriod.Before();
            case Keyword.AFTER:
                return new TriggerPeriod.After();
            case Keyword.INSTEAD:
                // self.expect_keyword_is(Keyword::OF).map(|_| TriggerPeriod::InsteadOf)?
                ExpectKeywordIs(Keyword.OF);
                return new TriggerPeriod.InsteadOf();
            case var unexpectedKeyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in trigger period"));
        }
    }

    /// <summary>Parse the event part of a trigger (<c>INSERT</c>, <c>UPDATE</c>, etc.).</summary>
    public TriggerEvent ParseTriggerEvent()
    {
        switch (ExpectOneOfKeywords(
                    Keyword.INSERT,
                    Keyword.UPDATE,
                    Keyword.DELETE,
                    Keyword.TRUNCATE))
        {
            case Keyword.INSERT:
                return new TriggerEvent.Insert();
            case Keyword.UPDATE:
                if (ParseKeyword(Keyword.OF))
                {
                    var cols = ParseCommaSeparated(p => p.ParseIdentifier());
                    return new TriggerEvent.Update(cols);
                }
                else
                {
                    return new TriggerEvent.Update(Sequence<Ident>.Empty);
                }
            case Keyword.DELETE:
                return new TriggerEvent.Delete();
            case Keyword.TRUNCATE:
                return new TriggerEvent.Truncate();
            case var unexpectedKeyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in trigger event"));
        }
    }

    /// <summary>Parse the <c>REFERENCING</c> clause of a trigger.</summary>
    public TriggerReferencing? ParseTriggerReferencing()
    {
        TriggerReferencingType referType;
        switch (ParseOneOfKeywords(Keyword.OLD, Keyword.NEW))
        {
            case Keyword.OLD when ParseKeyword(Keyword.TABLE):
                referType = new TriggerReferencingType.OldTable();
                break;
            case Keyword.NEW when ParseKeyword(Keyword.TABLE):
                referType = new TriggerReferencingType.NewTable();
                break;
            default:
                return null;
        }

        var isAs = ParseKeyword(Keyword.AS);
        var transitionRelationName = ParseObjectName(false);
        return new TriggerReferencing(
            ReferType: referType,
            IsAs: isAs,
            TransitionRelationName: transitionRelationName);
    }

    /// <summary>Parse the execution body of a trigger (<c>FUNCTION</c> or <c>PROCEDURE</c>).</summary>
    public TriggerExecBody ParseTriggerExecBody()
    {
        // Os campos do literal são avaliados na ordem do Rust: exec_type, depois func_desc.
        TriggerExecBodyType execType;
        switch (ExpectOneOfKeywords(Keyword.FUNCTION, Keyword.PROCEDURE))
        {
            case Keyword.FUNCTION:
                execType = new TriggerExecBodyType.Function();
                break;
            case Keyword.PROCEDURE:
                execType = new TriggerExecBodyType.Procedure();
                break;
            case var unexpectedKeyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in trigger exec body"));
        }
        return new TriggerExecBody(
            ExecType: execType,
            FuncDesc: ParseFunctionDesc());
    }

    /// <summary>Parse a <c>CREATE MACRO</c> statement.</summary>
    public Statement ParseCreateMacro(
        bool orReplace,
        bool temporary)
    {
        if (_dialect is DuckDbDialect or GenericDialect)
        {
            var name = ParseObjectName(false);
            ExpectToken(new Token.LParen());
            Sequence<MacroArg>? args;
            if (ConsumeToken(new Token.RParen()))
            {
                PrevToken();
                args = null;
            }
            else
            {
                args = ParseCommaSeparated(p => p.ParseMacroArg());
            }

            ExpectToken(new Token.RParen());
            ExpectKeywordIs(Keyword.AS);

            MacroDefinition definition;
            if (ParseKeyword(Keyword.TABLE))
            {
                definition = new MacroDefinition.Table(ParseQuery());
            }
            else
            {
                definition = new MacroDefinition.Expr(ParseExpr());
            }
            return new Statement.CreateMacro(
                OrReplace: orReplace,
                Temporary: temporary,
                Name: name,
                Args: args,
                Definition: definition);
        }
        else
        {
            PrevToken();
            return ExpectedRef<Statement>("an object type after CREATE", PeekTokenRef());
        }
    }

    internal MacroArg ParseMacroArg()
    {
        var name = ParseIdentifier();

        Expr? defaultExpr;
        if (ConsumeToken(new Token.Assignment()) || ConsumeToken(new Token.RArrow()))
        {
            defaultExpr = ParseExpr();
        }
        else
        {
            defaultExpr = null;
        }
        return new MacroArg(Name: name, DefaultExpr: defaultExpr);
    }

    /// <summary>Parse a <c>CREATE EXTERNAL TABLE</c> statement.</summary>
    public CreateTable ParseCreateExternalTable(
        bool orReplace)
    {
        ExpectKeywordIs(Keyword.TABLE);
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var tableName = ParseObjectName(false);
        var (columns, constraints) = ParseColumns();

        var hiveDistribution = ParseHiveDistribution();
        var hiveFormats = ParseHiveFormats();

        FileFormat? fileFormat;
        if (hiveFormats is { } hf)
        {
            if (hf.Storage is { } ff)
            {
                switch (ff)
                {
                    case HiveIOFormat.FileFormat { Format: var format }:
                        fileFormat = format;
                        break;
                    default:
                        fileFormat = null;
                        break;
                }
            }
            else
            {
                fileFormat = null;
            }
        }
        else
        {
            fileFormat = null;
        }
        // hive_formats.as_ref().and_then(|hf| hf.location.clone())
        var location = hiveFormats?.Location;

        ObjectName? withConnection;
        if (ParseKeywords(Keyword.WITH, Keyword.CONNECTION))
        {
            withConnection = ParseObjectName(false);
        }
        else
        {
            withConnection = null;
        }
        var tableProperties = ParseOptions(Keyword.TBLPROPERTIES);
        CreateTableOptions tableOptions;
        if (!tableProperties.IsEmpty)
        {
            tableOptions = new CreateTableOptions.TableProperties(tableProperties);
        }
        else if (MaybeParseOptions(Keyword.OPTIONS) is { } options)
        {
            tableOptions = new CreateTableOptions.Options(options);
        }
        else
        {
            tableOptions = new CreateTableOptions.None();
        }
        return new CreateTableBuilder(tableName)
            .SetColumns(columns)
            .SetConstraints(constraints)
            .SetHiveDistribution(hiveDistribution)
            .SetHiveFormats(hiveFormats)
            .SetTableOptions(tableOptions)
            .SetWithConnection(withConnection)
            .SetOrReplace(orReplace)
            .SetIfNotExists(ifNotExists)
            .SetExternal(true)
            .SetFileFormat(fileFormat)
            .SetLocation(location)
            .Build();
    }

    /// <summary>Parse <c>CREATE SNAPSHOT TABLE</c> statement.</summary>
    /// <remarks><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_snapshot_table_statement"/></remarks>
    public CreateTable ParseCreateSnapshotTable()
    {
        ExpectKeywords(Keyword.SNAPSHOT, Keyword.TABLE);
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var tableName = ParseObjectName(true);

        ExpectKeywordIs(Keyword.CLONE);
        ObjectName? clone = ParseObjectName(true);

        TableVersion? version;
        if (ParseKeywords(Keyword.FOR, Keyword.SYSTEM_TIME, Keyword.AS, Keyword.OF))
        {
            version = new TableVersion.ForSystemTimeAsOf(ParseExpr());
        }
        else
        {
            version = null;
        }

        CreateTableOptions tableOptions;
        if (MaybeParseOptions(Keyword.OPTIONS) is { } options)
        {
            tableOptions = new CreateTableOptions.Options(options);
        }
        else
        {
            tableOptions = new CreateTableOptions.None();
        }

        return new CreateTableBuilder(tableName)
            .SetSnapshot(true)
            .SetIfNotExists(ifNotExists)
            .CloneClause(clone)
            .SetVersion(version)
            .SetTableOptions(tableOptions)
            .Build();
    }

    /// <summary>Parse a file format for external tables.</summary>
    public FileFormat ParseFileFormat()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.AVRO:
                        return new FileFormat.AVRO();
                    case Keyword.JSONFILE:
                        return new FileFormat.JSONFILE();
                    case Keyword.ORC:
                        return new FileFormat.ORC();
                    case Keyword.PARQUET:
                        return new FileFormat.PARQUET();
                    case Keyword.RCFILE:
                        return new FileFormat.RCFILE();
                    case Keyword.SEQUENCEFILE:
                        return new FileFormat.SEQUENCEFILE();
                    case Keyword.TEXTFILE:
                        return new FileFormat.TEXTFILE();
                    default:
                        return Expected<FileFormat>("fileformat", nextToken);
                }
            default:
                return Expected<FileFormat>("fileformat", nextToken);
        }
    }

    internal AnalyzeFormatKind ParseAnalyzeFormatKind()
    {
        if (ConsumeToken(new Token.Eq()))
        {
            return new AnalyzeFormatKind.Assignment(ParseAnalyzeFormat());
        }
        else
        {
            return new AnalyzeFormatKind.Keyword(ParseAnalyzeFormat());
        }
    }

    /// <summary>Parse an <c>ANALYZE FORMAT</c>.</summary>
    public AnalyzeFormat ParseAnalyzeFormat()
    {
        var nextToken = NextToken();
        switch (nextToken.Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.TEXT:
                        return new AnalyzeFormat.TEXT();
                    case Keyword.GRAPHVIZ:
                        return new AnalyzeFormat.GRAPHVIZ();
                    case Keyword.JSON:
                        return new AnalyzeFormat.JSON();
                    case Keyword.TREE:
                        return new AnalyzeFormat.TREE();
                    default:
                        return Expected<AnalyzeFormat>("fileformat", nextToken);
                }
            default:
                return Expected<AnalyzeFormat>("fileformat", nextToken);
        }
    }
}
