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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 6742–8260:
// parse_create_view … parse_discard).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse a <c>CREATE VIEW</c> statement.</summary>
    public CreateView ParseCreateView(
        bool orAlter,
        bool orReplace,
        bool temporary,
        CreateViewParams? createViewParams)
    {
        var secure = ParseKeyword(Keyword.SECURE);
        var materialized = ParseKeyword(Keyword.MATERIALIZED);
        ExpectKeywordIs(Keyword.VIEW);
        var allowUnquotedHyphen = _dialect is BigQueryDialect;
        // Tries to parse IF NOT EXISTS either before name or after name
        // Name before IF NOT EXISTS is supported by snowflake but undocumented
        var ifNotExistsFirst =
            ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = ParseObjectName(allowUnquotedHyphen);
        var nameBeforeNotExists = !ifNotExistsFirst
            && ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var ifNotExists = ifNotExistsFirst || nameBeforeNotExists;
        var copyGrants = ParseKeywords(Keyword.COPY, Keyword.GRANTS);
        // Many dialects support `OR ALTER` right after `CREATE`, but we don't (yet).
        // ANSI SQL and Postgres support RECURSIVE here, but we don't support it either.
        var columns = ParseViewColumns();
        // Snowflake also documents `COPY GRANTS` *after* the column list; accept
        // either position, but not both.
        // <https://docs.snowflake.com/en/sql-reference/sql/create-view#syntax>
        if (!copyGrants)
        {
            copyGrants = ParseKeywords(Keyword.COPY, Keyword.GRANTS);
        }
        CreateTableOptions options = new CreateTableOptions.None();
        var withOptions = ParseOptions(Keyword.WITH);
        if (!withOptions.IsEmpty)
        {
            options = new CreateTableOptions.With(withOptions);
        }

        Sequence<Ident> clusterBy;
        if (ParseKeyword(Keyword.CLUSTER))
        {
            ExpectKeywordIs(Keyword.BY);
            clusterBy = ParseParenthesizedColumnList(IsOptional.Optional, false);
        }
        else
        {
            clusterBy = Sequence<Ident>.Empty;
        }

        if (_dialect is BigQueryDialect or GenericDialect)
        {
            if (MaybeParseOptions(Keyword.OPTIONS) is { } opts)
            {
                if (!opts.IsEmpty)
                {
                    options = new CreateTableOptions.Options(opts);
                }
            }
        }

        ObjectName? to;
        if ((_dialect is ClickHouseDialect or GenericDialect)
            && ParseKeyword(Keyword.TO))
        {
            to = ParseObjectName(false);
        }
        else
        {
            to = null;
        }

        string? comment;
        if (_dialect.SupportsCreateViewCommentSyntax
            && ParseKeyword(Keyword.COMMENT))
        {
            ExpectToken(new Token.Eq());
            comment = ParseCommentValue();
        }
        else
        {
            comment = null;
        }

        ExpectKeywordIs(Keyword.AS);
        var query = ParseQuery();
        // Optional `WITH [ CASCADED | LOCAL ] CHECK OPTION` is widely supported here.

        var withNoSchemaBinding = (_dialect is RedshiftSqlDialect or GenericDialect)
            && ParseKeywords(
                Keyword.WITH,
                Keyword.NO,
                Keyword.SCHEMA,
                Keyword.BINDING);

        return new CreateView(
            OrAlter: orAlter,
            Name: name,
            Columns: columns,
            Query: query,
            Materialized: materialized,
            Secure: secure,
            OrReplace: orReplace,
            Options: options,
            ClusterBy: clusterBy,
            Comment: comment,
            WithNoSchemaBinding: withNoSchemaBinding,
            IfNotExists: ifNotExists,
            Temporary: temporary,
            CopyGrants: copyGrants,
            To: to,
            Params: createViewParams,
            NameBeforeNotExists: nameBeforeNotExists);
    }

    /// <summary>
    /// Parse optional parameters for the <c>CREATE VIEW</c> statement supported by
    /// <see href="https://dev.mysql.com/doc/refman/9.1/en/create-view.html">MySQL</see>.
    /// </summary>
    internal CreateViewParams? ParseCreateViewParams()
    {
        CreateViewAlgorithm? algorithm;
        if (ParseKeyword(Keyword.ALGORITHM))
        {
            ExpectToken(new Token.Eq());
            switch (ExpectOneOfKeywords(
                        Keyword.UNDEFINED,
                        Keyword.MERGE,
                        Keyword.TEMPTABLE))
            {
                case Keyword.UNDEFINED:
                    algorithm = new CreateViewAlgorithm.Undefined();
                    break;
                case Keyword.MERGE:
                    algorithm = new CreateViewAlgorithm.Merge();
                    break;
                case Keyword.TEMPTABLE:
                    algorithm = new CreateViewAlgorithm.TempTable();
                    break;
                default:
                {
                    PrevToken();
                    var found = NextToken();
                    return Expected<CreateViewParams?>("UNDEFINED or MERGE or TEMPTABLE after ALGORITHM =", found);
                }
            }
        }
        else
        {
            algorithm = null;
        }
        GranteeName? definer;
        if (ParseKeyword(Keyword.DEFINER))
        {
            ExpectToken(new Token.Eq());
            definer = ParseGranteeName();
        }
        else
        {
            definer = null;
        }
        CreateViewSecurity? security;
        if (ParseKeywords(Keyword.SQL, Keyword.SECURITY))
        {
            switch (ExpectOneOfKeywords(Keyword.DEFINER, Keyword.INVOKER))
            {
                case Keyword.DEFINER:
                    security = new CreateViewSecurity.Definer();
                    break;
                case Keyword.INVOKER:
                    security = new CreateViewSecurity.Invoker();
                    break;
                default:
                {
                    PrevToken();
                    var found = NextToken();
                    return Expected<CreateViewParams?>("DEFINER or INVOKER after SQL SECURITY", found);
                }
            }
        }
        else
        {
            security = null;
        }
        if (algorithm is not null || definer is not null || security is not null)
        {
            return new CreateViewParams(
                Algorithm: algorithm,
                Definer: definer,
                Security: security);
        }
        else
        {
            return null;
        }
    }

    /// <summary>Parse a <c>CREATE ROLE</c> statement.</summary>
    public CreateRole ParseCreateRole()
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var names = ParseCommaSeparated(p => p.ParseObjectName(false));

        _ = ParseKeyword(Keyword.WITH); // [ WITH ]

        Keyword[] optionalKeywords;
        if (_dialect is MsSqlDialect)
        {
            optionalKeywords = new[] { Keyword.AUTHORIZATION };
        }
        else if (_dialect is PostgreSqlDialect)
        {
            optionalKeywords = new[]
            {
                Keyword.LOGIN,
                Keyword.NOLOGIN,
                Keyword.INHERIT,
                Keyword.NOINHERIT,
                Keyword.BYPASSRLS,
                Keyword.NOBYPASSRLS,
                Keyword.PASSWORD,
                Keyword.CREATEDB,
                Keyword.NOCREATEDB,
                Keyword.CREATEROLE,
                Keyword.NOCREATEROLE,
                Keyword.SUPERUSER,
                Keyword.NOSUPERUSER,
                Keyword.REPLICATION,
                Keyword.NOREPLICATION,
                Keyword.CONNECTION,
                Keyword.VALID,
                Keyword.IN,
                Keyword.ROLE,
                Keyword.ADMIN,
                Keyword.USER,
            };
        }
        else
        {
            optionalKeywords = System.Array.Empty<Keyword>();
        }

        // MSSQL
        ObjectName? authorizationOwner = null;
        // Postgres
        bool? login = null;
        bool? inherit = null;
        bool? bypassrls = null;
        Password? password = null;
        bool? createDb = null;
        bool? createRole = null;
        bool? superuser = null;
        bool? replication = null;
        Expr? connectionLimit = null;
        Expr? validUntil = null;
        var inRole = Sequence<Ident>.Empty;
        var inGroup = Sequence<Ident>.Empty;
        var role = Sequence<Ident>.Empty;
        var user = Sequence<Ident>.Empty;
        var admin = Sequence<Ident>.Empty;

        while (ParseOneOfKeywords(optionalKeywords) is { } keyword)
        {
            // self.tokens.get(self.index - 1).map_or(Location { line: 0, column: 0 }, |t| t.span.start)
            var loc = _index - 1 < _tokens.Count
                ? _tokens[_index - 1].Span.Start
                : new Location(0, 0);
            var stop = false;
            switch (keyword)
            {
                case Keyword.AUTHORIZATION:
                    if (authorizationOwner is not null)
                    {
                        throw ParserErr("Found multiple AUTHORIZATION", loc);
                    }
                    else
                    {
                        authorizationOwner = ParseObjectName(false);
                    }
                    break;
                case Keyword.LOGIN or Keyword.NOLOGIN:
                    if (login is not null)
                    {
                        throw ParserErr("Found multiple LOGIN or NOLOGIN", loc);
                    }
                    else
                    {
                        login = keyword == Keyword.LOGIN;
                    }
                    break;
                case Keyword.INHERIT or Keyword.NOINHERIT:
                    if (inherit is not null)
                    {
                        throw ParserErr("Found multiple INHERIT or NOINHERIT", loc);
                    }
                    else
                    {
                        inherit = keyword == Keyword.INHERIT;
                    }
                    break;
                case Keyword.BYPASSRLS or Keyword.NOBYPASSRLS:
                    if (bypassrls is not null)
                    {
                        throw ParserErr("Found multiple BYPASSRLS or NOBYPASSRLS", loc);
                    }
                    else
                    {
                        bypassrls = keyword == Keyword.BYPASSRLS;
                    }
                    break;
                case Keyword.CREATEDB or Keyword.NOCREATEDB:
                    if (createDb is not null)
                    {
                        throw ParserErr("Found multiple CREATEDB or NOCREATEDB", loc);
                    }
                    else
                    {
                        createDb = keyword == Keyword.CREATEDB;
                    }
                    break;
                case Keyword.CREATEROLE or Keyword.NOCREATEROLE:
                    if (createRole is not null)
                    {
                        throw ParserErr("Found multiple CREATEROLE or NOCREATEROLE", loc);
                    }
                    else
                    {
                        createRole = keyword == Keyword.CREATEROLE;
                    }
                    break;
                case Keyword.SUPERUSER or Keyword.NOSUPERUSER:
                    if (superuser is not null)
                    {
                        throw ParserErr("Found multiple SUPERUSER or NOSUPERUSER", loc);
                    }
                    else
                    {
                        superuser = keyword == Keyword.SUPERUSER;
                    }
                    break;
                case Keyword.REPLICATION or Keyword.NOREPLICATION:
                    if (replication is not null)
                    {
                        throw ParserErr("Found multiple REPLICATION or NOREPLICATION", loc);
                    }
                    else
                    {
                        replication = keyword == Keyword.REPLICATION;
                    }
                    break;
                case Keyword.PASSWORD:
                    if (password is not null)
                    {
                        throw ParserErr("Found multiple PASSWORD", loc);
                    }
                    else
                    {
                        if (ParseKeyword(Keyword.NULL))
                        {
                            password = new Password.NullPassword();
                        }
                        else
                        {
                            password = new Password.PasswordValue(new Expr.Value(ParseValue()));
                        }
                    }
                    break;
                case Keyword.CONNECTION:
                    ExpectKeywordIs(Keyword.LIMIT);
                    if (connectionLimit is not null)
                    {
                        throw ParserErr("Found multiple CONNECTION LIMIT", loc);
                    }
                    else
                    {
                        connectionLimit = new Expr.Value(ParseNumberValue());
                    }
                    break;
                case Keyword.VALID:
                    ExpectKeywordIs(Keyword.UNTIL);
                    if (validUntil is not null)
                    {
                        throw ParserErr("Found multiple VALID UNTIL", loc);
                    }
                    else
                    {
                        validUntil = new Expr.Value(ParseValue());
                    }
                    break;
                case Keyword.IN:
                    if (ParseKeyword(Keyword.ROLE))
                    {
                        if (!inRole.IsEmpty)
                        {
                            throw ParserErr("Found multiple IN ROLE", loc);
                        }
                        else
                        {
                            inRole = ParseCommaSeparated(p => p.ParseIdentifier());
                        }
                    }
                    else if (ParseKeyword(Keyword.GROUP))
                    {
                        if (!inGroup.IsEmpty)
                        {
                            throw ParserErr("Found multiple IN GROUP", loc);
                        }
                        else
                        {
                            inGroup = ParseCommaSeparated(p => p.ParseIdentifier());
                        }
                    }
                    else
                    {
                        ExpectedRef("ROLE or GROUP after IN", PeekTokenRef());
                    }
                    break;
                case Keyword.ROLE:
                    if (!role.IsEmpty)
                    {
                        throw ParserErr("Found multiple ROLE", loc);
                    }
                    else
                    {
                        role = ParseCommaSeparated(p => p.ParseIdentifier());
                    }
                    break;
                case Keyword.USER:
                    if (!user.IsEmpty)
                    {
                        throw ParserErr("Found multiple USER", loc);
                    }
                    else
                    {
                        user = ParseCommaSeparated(p => p.ParseIdentifier());
                    }
                    break;
                case Keyword.ADMIN:
                    if (!admin.IsEmpty)
                    {
                        throw ParserErr("Found multiple ADMIN", loc);
                    }
                    else
                    {
                        admin = ParseCommaSeparated(p => p.ParseIdentifier());
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

        return new CreateRole(
            Names: names,
            IfNotExists: ifNotExists,
            Login: login,
            Inherit: inherit,
            Bypassrls: bypassrls,
            Password: password,
            CreateDb: createDb,
            CreateRoleValue: createRole,
            Replication: replication,
            Superuser: superuser,
            ConnectionLimit: connectionLimit,
            ValidUntil: validUntil,
            InRole: inRole,
            InGroup: inGroup,
            Role: role,
            User: user,
            Admin: admin,
            AuthorizationOwner: authorizationOwner);
    }

    /// <summary>Parse an <c>OWNER</c> clause.</summary>
    public Owner ParseOwner()
    {
        Owner owner;
        switch (ParseOneOfKeywords(Keyword.CURRENT_USER, Keyword.CURRENT_ROLE, Keyword.SESSION_USER))
        {
            case Keyword.CURRENT_USER:
                owner = new Owner.CurrentUser();
                break;
            case Keyword.CURRENT_ROLE:
                owner = new Owner.CurrentRole();
                break;
            case Keyword.SESSION_USER:
                owner = new Owner.SessionUser();
                break;
            case { } unexpectedKeyword:
                throw new ParserException(new ParserError.ParserErrorValue(
                    $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in owner"));
            case null:
            {
                // match self.parse_identifier() { Ok(ident) => .., Err(e) => return Err(..) }
                Ident ident;
                try
                {
                    ident = ParseIdentifier();
                }
                catch (ParserException e)
                {
                    // `{e}`: Display de ParserError ("sql parser error: ...").
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"Expected: CURRENT_USER, CURRENT_ROLE, SESSION_USER or identifier after OWNER TO. {e.Error}"));
                }
                owner = new Owner.Ident(ident);
                break;
            }
        }
        return owner;
    }

    /// <summary>Parses a <c>Statement::CreateDomain</c> statement.</summary>
    internal CreateDomain ParseCreateDomain()
    {
        var name = ParseObjectName(false);
        ExpectKeywordIs(Keyword.AS);
        var dataType = ParseDataType();
        Ident? collation;
        if (ParseKeyword(Keyword.COLLATE))
        {
            collation = ParseIdentifier();
        }
        else
        {
            collation = null;
        }
        Expr? @default;
        if (ParseKeyword(Keyword.DEFAULT))
        {
            @default = ParseExpr();
        }
        else
        {
            @default = null;
        }
        var constraints = new List<TableConstraint>();
        while (ParseOptionalTableConstraint() is { } constraint)
        {
            constraints.Add(constraint);
        }

        return new CreateDomain(
            Name: name,
            DataType: dataType,
            Collation: collation,
            Default: @default,
            Constraints: constraints.ToSequence());
    }

    /// <summary>
    /// <code>
    ///     CREATE POLICY name ON table_name [ AS { PERMISSIVE | RESTRICTIVE } ]
    ///     [ FOR { ALL | SELECT | INSERT | UPDATE | DELETE } ]
    ///     [ TO { role_name | PUBLIC | CURRENT_USER | CURRENT_ROLE | SESSION_USER } [, ...] ]
    ///     [ USING ( using_expression ) ]
    ///     [ WITH CHECK ( with_check_expression ) ]
    /// </code>
    /// <see href="https://www.postgresql.org/docs/current/sql-createpolicy.html">PostgreSQL Documentation</see>
    /// </summary>
    public CreatePolicy ParseCreatePolicy()
    {
        var name = ParseIdentifier();
        ExpectKeywordIs(Keyword.ON);
        var tableName = ParseObjectName(false);

        CreatePolicyType? policyType;
        if (ParseKeyword(Keyword.AS))
        {
            var keyword =
                ExpectOneOfKeywords(Keyword.PERMISSIVE, Keyword.RESTRICTIVE);
            switch (keyword)
            {
                case Keyword.PERMISSIVE:
                    policyType = new CreatePolicyType.Permissive();
                    break;
                case Keyword.RESTRICTIVE:
                    policyType = new CreatePolicyType.Restrictive();
                    break;
                case var unexpectedKeyword:
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in policy type"));
            }
        }
        else
        {
            policyType = null;
        }

        CreatePolicyCommand? command;
        if (ParseKeyword(Keyword.FOR))
        {
            var keyword = ExpectOneOfKeywords(
                Keyword.ALL,
                Keyword.SELECT,
                Keyword.INSERT,
                Keyword.UPDATE,
                Keyword.DELETE);
            switch (keyword)
            {
                case Keyword.ALL:
                    command = new CreatePolicyCommand.All();
                    break;
                case Keyword.SELECT:
                    command = new CreatePolicyCommand.Select();
                    break;
                case Keyword.INSERT:
                    command = new CreatePolicyCommand.Insert();
                    break;
                case Keyword.UPDATE:
                    command = new CreatePolicyCommand.Update();
                    break;
                case Keyword.DELETE:
                    command = new CreatePolicyCommand.Delete();
                    break;
                case var unexpectedKeyword:
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in policy command"));
            }
        }
        else
        {
            command = null;
        }

        Sequence<Owner>? to;
        if (ParseKeyword(Keyword.TO))
        {
            to = ParseCommaSeparated(p => p.ParseOwner());
        }
        else
        {
            to = null;
        }

        Expr? @using;
        if (ParseKeyword(Keyword.USING))
        {
            ExpectToken(new Token.LParen());
            var expr = ParseExpr();
            ExpectToken(new Token.RParen());
            @using = expr;
        }
        else
        {
            @using = null;
        }

        Expr? withCheck;
        if (ParseKeywords(Keyword.WITH, Keyword.CHECK))
        {
            ExpectToken(new Token.LParen());
            var expr = ParseExpr();
            ExpectToken(new Token.RParen());
            withCheck = expr;
        }
        else
        {
            withCheck = null;
        }

        return new CreatePolicy(
            Name: name,
            TableName: tableName,
            PolicyType: policyType,
            Command: command,
            To: to,
            Using: @using,
            WithCheck: withCheck);
    }

    /// <summary>
    /// <code>
    /// CREATE CONNECTOR [IF NOT EXISTS] connector_name
    /// [TYPE datasource_type]
    /// [URL datasource_url]
    /// [COMMENT connector_comment]
    /// [WITH DCPROPERTIES(property_name=property_value, ...)]
    /// </code>
    /// <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=27362034#LanguageManualDDL-CreateDataConnectorCreateConnector">Hive Documentation</see>
    /// </summary>
    public CreateConnector ParseCreateConnector()
    {
        var ifNotExists = ParseKeywords(Keyword.IF, Keyword.NOT, Keyword.EXISTS);
        var name = ParseIdentifier();

        string? connectorType;
        if (ParseKeyword(Keyword.TYPE))
        {
            connectorType = ParseLiteralString();
        }
        else
        {
            connectorType = null;
        }

        string? url;
        if (ParseKeyword(Keyword.URL))
        {
            url = ParseLiteralString();
        }
        else
        {
            url = null;
        }

        var comment = ParseOptionalInlineComment();

        Sequence<SqlOption>? withDcproperties;
        switch (ParseOptionsWithKeywords(Keyword.WITH, Keyword.DCPROPERTIES))
        {
            case var properties when !properties.IsEmpty:
                withDcproperties = properties;
                break;
            default:
                withDcproperties = null;
                break;
        }

        return new CreateConnector(
            Name: name,
            IfNotExists: ifNotExists,
            ConnectorType: connectorType,
            Url: url,
            Comment: comment,
            WithDcproperties: withDcproperties);
    }

    /// <summary>
    /// Parse an operator name, which can contain special characters like +, -, &lt;, &gt;, =
    /// that are tokenized as operator tokens rather than identifiers.
    /// This is used for PostgreSQL CREATE OPERATOR statements.
    /// </summary>
    /// <remarks>Examples: <c>+</c>, <c>myschema.+</c>, <c>pg_catalog.&lt;=</c></remarks>
    internal ObjectName ParseOperatorName()
    {
        var parts = new List<ObjectNamePart>();
        while (true)
        {
            parts.Add(new ObjectNamePart.Identifier(new Ident(
                NextToken().ToString())));
            if (!ConsumeToken(new Token.Period()))
            {
                break;
            }
        }
        return new ObjectName(parts.ToSequence());
    }

    /// <summary>
    /// Parse a <c>Statement::CreateOperator</c>
    /// <see href="https://www.postgresql.org/docs/current/sql-createoperator.html">PostgreSQL Documentation</see>
    /// </summary>
    public CreateOperator ParseCreateOperator()
    {
        var name = ParseOperatorName();
        ExpectToken(new Token.LParen());

        ObjectName? function = null;
        var isProcedure = false;
        DataType? leftArg = null;
        DataType? rightArg = null;
        var options = new List<OperatorOption>();

        while (true)
        {
            var keyword = ExpectOneOfKeywords(
                Keyword.FUNCTION,
                Keyword.PROCEDURE,
                Keyword.LEFTARG,
                Keyword.RIGHTARG,
                Keyword.COMMUTATOR,
                Keyword.NEGATOR,
                Keyword.RESTRICT,
                Keyword.JOIN,
                Keyword.HASHES,
                Keyword.MERGES);

            switch (keyword)
            {
                case Keyword.HASHES when !options.Any(o => o is OperatorOption.Hashes):
                    options.Add(new OperatorOption.Hashes());
                    break;
                case Keyword.MERGES when !options.Any(o => o is OperatorOption.Merges):
                    options.Add(new OperatorOption.Merges());
                    break;
                case Keyword.FUNCTION or Keyword.PROCEDURE when function is null:
                    ExpectToken(new Token.Eq());
                    function = ParseObjectName(false);
                    isProcedure = keyword == Keyword.PROCEDURE;
                    break;
                case Keyword.LEFTARG when leftArg is null:
                    ExpectToken(new Token.Eq());
                    leftArg = ParseDataType();
                    break;
                case Keyword.RIGHTARG when rightArg is null:
                    ExpectToken(new Token.Eq());
                    rightArg = ParseDataType();
                    break;
                case Keyword.COMMUTATOR
                    when !options.Any(o => o is OperatorOption.Commutator):
                    ExpectToken(new Token.Eq());
                    if (ParseKeyword(Keyword.OPERATOR))
                    {
                        ExpectToken(new Token.LParen());
                        var op = ParseOperatorName();
                        ExpectToken(new Token.RParen());
                        options.Add(new OperatorOption.Commutator(op));
                    }
                    else
                    {
                        options.Add(new OperatorOption.Commutator(ParseOperatorName()));
                    }
                    break;
                case Keyword.NEGATOR
                    when !options.Any(o => o is OperatorOption.Negator):
                    ExpectToken(new Token.Eq());
                    if (ParseKeyword(Keyword.OPERATOR))
                    {
                        ExpectToken(new Token.LParen());
                        var op = ParseOperatorName();
                        ExpectToken(new Token.RParen());
                        options.Add(new OperatorOption.Negator(op));
                    }
                    else
                    {
                        options.Add(new OperatorOption.Negator(ParseOperatorName()));
                    }
                    break;
                case Keyword.RESTRICT
                    when !options.Any(o => o is OperatorOption.Restrict):
                    ExpectToken(new Token.Eq());
                    options.Add(new OperatorOption.Restrict(
                        ParseObjectName(false)));
                    break;
                case Keyword.JOIN when !options.Any(o => o is OperatorOption.Join):
                    ExpectToken(new Token.Eq());
                    options.Add(new OperatorOption.Join(ParseObjectName(false)));
                    break;
                default:
                    // `{:?}` de Keyword = nome da variante (caso 46).
                    throw new ParserException(new ParserError.ParserErrorValue(
                        $"Duplicate or unexpected keyword {keyword.ToString()} in CREATE OPERATOR"));
            }

            if (!ConsumeToken(new Token.Comma()))
            {
                break;
            }
        }

        // Expect closing parenthesis
        ExpectToken(new Token.RParen());

        // FUNCTION is required
        // function.ok_or_else(|| ParserError::ParserError(..))?
        if (function is null)
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                "CREATE OPERATOR requires FUNCTION parameter"));
        }

        return new CreateOperator(
            Name: name,
            Function: function,
            IsProcedure: isProcedure,
            LeftArg: leftArg,
            RightArg: rightArg,
            Options: options.ToSequence());
    }

    /// <summary>
    /// Parse a <c>Statement::CreateOperatorFamily</c>
    /// <see href="https://www.postgresql.org/docs/current/sql-createopfamily.html">PostgreSQL Documentation</see>
    /// </summary>
    public CreateOperatorFamily ParseCreateOperatorFamily()
    {
        var name = ParseObjectName(false);
        ExpectKeyword(Keyword.USING);
        var @using = ParseIdentifier();

        return new CreateOperatorFamily(Name: name, Using: @using);
    }

    /// <summary>
    /// Parse a <c>Statement::CreateOperatorClass</c>
    /// <see href="https://www.postgresql.org/docs/current/sql-createopclass.html">PostgreSQL Documentation</see>
    /// </summary>
    public CreateOperatorClass ParseCreateOperatorClass()
    {
        var name = ParseObjectName(false);
        var @default = ParseKeyword(Keyword.DEFAULT);
        ExpectKeywords(Keyword.FOR, Keyword.TYPE);
        var forType = ParseDataType();
        ExpectKeyword(Keyword.USING);
        var @using = ParseIdentifier();

        ObjectName? family;
        if (ParseKeyword(Keyword.FAMILY))
        {
            family = ParseObjectName(false);
        }
        else
        {
            family = null;
        }

        ExpectKeyword(Keyword.AS);

        var items = new List<OperatorClassItem>();
        while (true)
        {
            if (ParseKeyword(Keyword.OPERATOR))
            {
                var strategyNumber = ParseLiteralUint();
                var operatorName = ParseOperatorName();

                // Optional operator argument types
                OperatorArgTypes? opTypes;
                if (ConsumeToken(new Token.LParen()))
                {
                    var left = ParseDataType();
                    ExpectToken(new Token.Comma());
                    var right = ParseDataType();
                    ExpectToken(new Token.RParen());
                    opTypes = new OperatorArgTypes(Left: left, Right: right);
                }
                else
                {
                    opTypes = null;
                }

                // Optional purpose
                OperatorPurpose? purpose;
                if (ParseKeyword(Keyword.FOR))
                {
                    if (ParseKeyword(Keyword.SEARCH))
                    {
                        purpose = new OperatorPurpose.ForSearch();
                    }
                    else if (ParseKeywords(Keyword.ORDER, Keyword.BY))
                    {
                        var sortFamily = ParseObjectName(false);
                        purpose = new OperatorPurpose.ForOrderBy(SortFamily: sortFamily);
                    }
                    else
                    {
                        return ExpectedRef<CreateOperatorClass>("SEARCH or ORDER BY after FOR", PeekTokenRef());
                    }
                }
                else
                {
                    purpose = null;
                }

                items.Add(new OperatorClassItem.Operator(
                    StrategyNumber: strategyNumber,
                    OperatorName: operatorName,
                    OpTypes: opTypes,
                    Purpose: purpose));
            }
            else if (ParseKeyword(Keyword.FUNCTION))
            {
                var supportNumber = ParseLiteralUint();

                // Optional operator types
                Sequence<DataType>? opTypes;
                if (ConsumeToken(new Token.LParen())
                    && PeekTokenRef().Token is not Token.RParen)
                {
                    var types = new List<DataType>();
                    while (true)
                    {
                        types.Add(ParseDataType());
                        if (!ConsumeToken(new Token.Comma()))
                        {
                            break;
                        }
                    }
                    ExpectToken(new Token.RParen());
                    opTypes = types.ToSequence();
                }
                else if (ConsumeToken(new Token.LParen()))
                {
                    ExpectToken(new Token.RParen());
                    opTypes = Sequence<DataType>.Empty;
                }
                else
                {
                    opTypes = null;
                }

                var functionName = ParseObjectName(false);

                // Function argument types
                Sequence<DataType> argumentTypes;
                if (ConsumeToken(new Token.LParen()))
                {
                    var types = new List<DataType>();
                    while (true)
                    {
                        if (PeekTokenRef().Token is Token.RParen)
                        {
                            break;
                        }
                        types.Add(ParseDataType());
                        if (!ConsumeToken(new Token.Comma()))
                        {
                            break;
                        }
                    }
                    ExpectToken(new Token.RParen());
                    argumentTypes = types.ToSequence();
                }
                else
                {
                    argumentTypes = Sequence<DataType>.Empty;
                }

                items.Add(new OperatorClassItem.Function(
                    SupportNumber: supportNumber,
                    OpTypes: opTypes,
                    FunctionName: functionName,
                    ArgumentTypes: argumentTypes));
            }
            else if (ParseKeyword(Keyword.STORAGE))
            {
                var storageType = ParseDataType();
                items.Add(new OperatorClassItem.Storage(StorageType: storageType));
            }
            else
            {
                break;
            }

            // Check for comma separator
            if (!ConsumeToken(new Token.Comma()))
            {
                break;
            }
        }

        return new CreateOperatorClass(
            Name: name,
            Default: @default,
            ForType: forType,
            Using: @using,
            Family: family,
            Items: items.ToSequence());
    }

    /// <summary>Parse a <c>DROP</c> statement.</summary>
    public Statement ParseDrop()
    {
        // MySQL dialect supports `TEMPORARY`
        var temporary = (_dialect is MySqlDialect or GenericDialect or DuckDbDialect)
            && ParseKeyword(Keyword.TEMPORARY);
        var persistent = _dialect is DuckDbDialect
            && ParseOneOfKeywords(Keyword.PERSISTENT) is not null;

        ObjectType objectType;
        if (ParseKeyword(Keyword.TABLE))
        {
            objectType = new ObjectType.Table();
        }
        else if (ParseKeyword(Keyword.COLLATION))
        {
            objectType = new ObjectType.Collation();
        }
        else if (ParseKeyword(Keyword.VIEW))
        {
            objectType = new ObjectType.View();
        }
        else if (ParseKeywords(Keyword.MATERIALIZED, Keyword.VIEW))
        {
            objectType = new ObjectType.MaterializedView();
        }
        else if (ParseKeyword(Keyword.INDEX))
        {
            objectType = new ObjectType.Index();
        }
        else if (ParseKeyword(Keyword.ROLE))
        {
            objectType = new ObjectType.Role();
        }
        else if (ParseKeyword(Keyword.SCHEMA))
        {
            objectType = new ObjectType.Schema();
        }
        else if (ParseKeyword(Keyword.DATABASE))
        {
            objectType = new ObjectType.Database();
        }
        else if (ParseKeyword(Keyword.SEQUENCE))
        {
            objectType = new ObjectType.Sequence();
        }
        else if (ParseKeyword(Keyword.STAGE))
        {
            objectType = new ObjectType.Stage();
        }
        else if (ParseKeyword(Keyword.TYPE))
        {
            objectType = new ObjectType.Type();
        }
        else if (ParseKeyword(Keyword.USER))
        {
            objectType = new ObjectType.User();
        }
        else if (ParseKeyword(Keyword.STREAM))
        {
            objectType = new ObjectType.Stream();
        }
        else if (ParseKeyword(Keyword.WAREHOUSE))
        {
            objectType = new ObjectType.Warehouse();
        }
        else if (ParseKeyword(Keyword.FUNCTION))
        {
            return Statement.From(ParseDropFunction());
        }
        else if (ParseKeyword(Keyword.POLICY))
        {
            return Statement.From(ParseDropPolicy());
        }
        else if (ParseKeyword(Keyword.CONNECTOR))
        {
            return ParseDropConnector();
        }
        else if (ParseKeyword(Keyword.DOMAIN))
        {
            return Statement.From(ParseDropDomain());
        }
        else if (ParseKeyword(Keyword.PROCEDURE))
        {
            return ParseDropProcedure();
        }
        else if (ParseKeyword(Keyword.SECRET))
        {
            return ParseDropSecret(temporary, persistent);
        }
        else if (ParseKeyword(Keyword.TRIGGER))
        {
            return Statement.From(ParseDropTrigger());
        }
        else if (ParseKeyword(Keyword.EXTENSION))
        {
            return ParseDropExtension();
        }
        else if (ParseKeyword(Keyword.OPERATOR))
        {
            // Check if this is DROP OPERATOR FAMILY or DROP OPERATOR CLASS
            if (ParseKeyword(Keyword.FAMILY))
            {
                return ParseDropOperatorFamily();
            }
            else if (ParseKeyword(Keyword.CLASS))
            {
                return ParseDropOperatorClass();
            }
            else
            {
                return ParseDropOperator();
            }
        }
        else
        {
            return ExpectedRef<Statement>(
                "COLLATION, CONNECTOR, DATABASE, EXTENSION, FUNCTION, INDEX, OPERATOR, POLICY, PROCEDURE, ROLE, SCHEMA, SECRET, SEQUENCE, STAGE, TABLE, TRIGGER, TYPE, VIEW, MATERIALIZED VIEW, USER or WAREHOUSE after DROP",
                PeekTokenRef());
        }
        // Many dialects support the non-standard `IF EXISTS` clause and allow
        // specifying multiple objects to delete in a single statement
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var names = ParseCommaSeparated(p => p.ParseObjectName(false));

        var loc = PeekTokenRef().Span.Start;
        var cascade = ParseKeyword(Keyword.CASCADE);
        var restrict = ParseKeyword(Keyword.RESTRICT);
        var purge = ParseKeyword(Keyword.PURGE);
        if (cascade && restrict)
        {
            throw ParserErr("Cannot specify both CASCADE and RESTRICT in DROP", loc);
        }
        if (objectType is ObjectType.Role && (cascade || restrict || purge))
        {
            throw ParserErr(
                "Cannot specify CASCADE, RESTRICT, or PURGE in DROP ROLE",
                loc);
        }
        ObjectName? table;
        if (ParseKeyword(Keyword.ON))
        {
            table = ParseObjectName(false);
        }
        else
        {
            table = null;
        }
        return new Statement.Drop(
            ObjectType: objectType,
            IfExists: ifExists,
            Names: names,
            Cascade: cascade,
            Restrict: restrict,
            Purge: purge,
            Temporary: temporary,
            Table: table);
    }

    internal DropBehavior? ParseOptionalDropBehavior()
    {
        switch (ParseOneOfKeywords(Keyword.CASCADE, Keyword.RESTRICT))
        {
            case Keyword.CASCADE:
                return new DropBehavior.Cascade();
            case Keyword.RESTRICT:
                return new DropBehavior.Restrict();
            default:
                return null;
        }
    }

    /// <summary>
    /// <code>
    /// DROP FUNCTION [ IF EXISTS ] name [ ( [ [ argmode ] [ argname ] argtype [, ...] ] ) ] [, ...]
    /// [ CASCADE | RESTRICT ]
    /// </code>
    /// </summary>
    internal DropFunction ParseDropFunction()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var funcDesc = ParseCommaSeparated(p => p.ParseFunctionDesc());
        var dropBehavior = ParseOptionalDropBehavior();
        return new DropFunction(
            IfExists: ifExists,
            FuncDesc: funcDesc,
            DropBehavior: dropBehavior);
    }

    /// <summary>
    /// <code>
    /// DROP POLICY [ IF EXISTS ] name ON table_name [ CASCADE | RESTRICT ]
    /// </code>
    /// <see href="https://www.postgresql.org/docs/current/sql-droppolicy.html">PostgreSQL Documentation</see>
    /// </summary>
    internal DropPolicy ParseDropPolicy()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var name = ParseIdentifier();
        ExpectKeywordIs(Keyword.ON);
        var tableName = ParseObjectName(false);
        var dropBehavior = ParseOptionalDropBehavior();
        return new DropPolicy(
            IfExists: ifExists,
            Name: name,
            TableName: tableName,
            DropBehavior: dropBehavior);
    }

    /// <summary>
    /// <code>
    /// DROP CONNECTOR [IF EXISTS] name
    /// </code>
    /// See <see href="https://cwiki.apache.org/confluence/pages/viewpage.action?pageId=27362034#LanguageManualDDL-DropConnector">Hive</see>
    /// </summary>
    internal Statement ParseDropConnector()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var name = ParseIdentifier();
        return new Statement.DropConnector(IfExists: ifExists, Name: name);
    }

    /// <summary>
    /// <code>
    /// DROP DOMAIN [ IF EXISTS ] name [ CASCADE | RESTRICT ]
    /// </code>
    /// </summary>
    internal DropDomain ParseDropDomain()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var name = ParseObjectName(false);
        var dropBehavior = ParseOptionalDropBehavior();
        return new DropDomain(
            IfExists: ifExists,
            Name: name,
            DropBehavior: dropBehavior);
    }

    /// <summary>
    /// <code>
    /// DROP PROCEDURE [ IF EXISTS ] name [ ( [ [ argmode ] [ argname ] argtype [, ...] ] ) ] [, ...]
    /// [ CASCADE | RESTRICT ]
    /// </code>
    /// </summary>
    internal Statement ParseDropProcedure()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var procDesc = ParseCommaSeparated(p => p.ParseFunctionDesc());
        var dropBehavior = ParseOptionalDropBehavior();
        return new Statement.DropProcedure(
            IfExists: ifExists,
            ProcDesc: procDesc,
            DropBehavior: dropBehavior);
    }

    internal FunctionDesc ParseFunctionDesc()
    {
        var name = ParseObjectName(false);

        Sequence<OperateFunctionArg>? args;
        if (ConsumeToken(new Token.LParen()))
        {
            if (ConsumeToken(new Token.RParen()))
            {
                args = Sequence<OperateFunctionArg>.Empty;
            }
            else
            {
                // Rust: `let args` interno sombreia o externo (caso 72).
                var parsedArgs = ParseCommaSeparated(p => p.ParseFunctionArg());
                ExpectToken(new Token.RParen());
                args = parsedArgs;
            }
        }
        else
        {
            args = null;
        }

        return new FunctionDesc(Name: name, Args: args);
    }

    /// <summary>
    /// See <see href="https://duckdb.org/docs/sql/statements/create_secret.html">DuckDB Docs</see> for more details.
    /// </summary>
    internal Statement ParseDropSecret(
        bool temporary,
        bool persistent)
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var name = ParseIdentifier();
        Ident? storageSpecifier;
        if (ParseKeyword(Keyword.FROM))
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
            storageSpecifier = null;
        }
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

        return new Statement.DropSecret(
            IfExists: ifExists,
            Temporary: temp,
            Name: name,
            StorageSpecifier: storageSpecifier);
    }

    /// <summary>Parse a <c>DECLARE</c> statement.</summary>
    /// <remarks>
    /// <code>
    /// DECLARE name [ BINARY ] [ ASENSITIVE | INSENSITIVE ] [ [ NO ] SCROLL ]
    ///     CURSOR [ { WITH | WITHOUT } HOLD ] FOR query
    /// </code>
    /// The syntax can vary significantly between warehouses. See the grammar
    /// on the warehouse specific function in such cases.
    /// </remarks>
    public Statement ParseDeclare()
    {
        if (_dialect is BigQueryDialect)
        {
            return ParseBigQueryDeclare();
        }
        if (_dialect is SnowflakeDialect)
        {
            return ParseSnowflakeDeclare();
        }
        if (_dialect is MsSqlDialect)
        {
            return ParseMssqlDeclare();
        }

        var name = ParseIdentifier();

        bool? binary = ParseKeyword(Keyword.BINARY);
        bool? sensitive;
        if (ParseKeyword(Keyword.INSENSITIVE))
        {
            sensitive = true;
        }
        else if (ParseKeyword(Keyword.ASENSITIVE))
        {
            sensitive = false;
        }
        else
        {
            sensitive = null;
        }
        bool? scroll;
        if (ParseKeyword(Keyword.SCROLL))
        {
            scroll = true;
        }
        else if (ParseKeywords(Keyword.NO, Keyword.SCROLL))
        {
            scroll = false;
        }
        else
        {
            scroll = null;
        }

        ExpectKeywordIs(Keyword.CURSOR);
        DeclareType? declareType = new DeclareType.Cursor();

        bool? hold;
        switch (ParseOneOfKeywords(Keyword.WITH, Keyword.WITHOUT))
        {
            case { } keyword:
                ExpectKeywordIs(Keyword.HOLD);

                switch (keyword)
                {
                    case Keyword.WITH:
                        hold = true;
                        break;
                    case Keyword.WITHOUT:
                        hold = false;
                        break;
                    case var unexpectedKeyword:
                        throw new ParserException(new ParserError.ParserErrorValue(
                            $"Internal parser error: unexpected keyword `{unexpectedKeyword.ToSql()}` in cursor hold"));
                }
                break;
            case null:
                hold = null;
                break;
        }

        ExpectKeywordIs(Keyword.FOR);

        Query? query = ParseQuery();

        return new Statement.Declare(
            Stmts: Sequence.Of(new Declare(
                Names: Sequence.Of(name),
                DataType: null,
                Assignment: null,
                DeclareType: declareType,
                Binary: binary,
                Sensitive: sensitive,
                Scroll: scroll,
                Hold: hold,
                ForQuery: query)));
    }

    /// <summary>
    /// Parse a <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#declare">BigQuery</see>
    /// <c>DECLARE</c> statement.
    /// </summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// DECLARE variable_name[, ...] [{ &lt;variable_type&gt; | &lt;DEFAULT expression&gt; }];
    /// </code>
    /// </remarks>
    public Statement ParseBigQueryDeclare()
    {
        var names = ParseCommaSeparated(p => p.ParseIdentifier());

        DataType? dataType;
        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var w } when w.Keyword == Keyword.DEFAULT:
                dataType = null;
                break;
            default:
                dataType = ParseDataType();
                break;
        }

        Expr? expr;
        if (dataType is not null)
        {
            if (ParseKeyword(Keyword.DEFAULT))
            {
                expr = ParseExpr();
            }
            else
            {
                expr = null;
            }
        }
        else
        {
            // If no variable type - default expression must be specified, per BQ docs.
            // i.e `DECLARE foo;` is invalid.
            ExpectKeywordIs(Keyword.DEFAULT);
            expr = ParseExpr();
        }

        // expr.map(|expr| DeclareAssignment::Default(Box::new(expr)))
        DeclareAssignment? assignment = expr is not null ? new DeclareAssignment.Default(expr) : null;
        return new Statement.Declare(
            Stmts: Sequence.Of(new Declare(
                Names: names,
                DataType: dataType,
                Assignment: assignment,
                DeclareType: null,
                Binary: null,
                Sensitive: null,
                Scroll: null,
                Hold: null,
                ForQuery: null)));
    }

    /// <summary>
    /// Parse a <see href="https://docs.snowflake.com/en/sql-reference/snowflake-scripting/declare">Snowflake</see>
    /// <c>DECLARE</c> statement.
    /// </summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// DECLARE
    ///   [{ &lt;variable_declaration&gt;
    ///      | &lt;cursor_declaration&gt;
    ///      | &lt;resultset_declaration&gt;
    ///      | &lt;exception_declaration&gt; }; ... ]
    ///
    /// &lt;variable_declaration&gt;
    /// &lt;variable_name&gt; [&lt;type&gt;] [ { DEFAULT | := } &lt;expression&gt;]
    ///
    /// &lt;cursor_declaration&gt;
    /// &lt;cursor_name&gt; CURSOR FOR &lt;query&gt;
    ///
    /// &lt;resultset_declaration&gt;
    /// &lt;resultset_name&gt; RESULTSET [ { DEFAULT | := } ( &lt;query&gt; ) ] ;
    ///
    /// &lt;exception_declaration&gt;
    /// &lt;exception_name&gt; EXCEPTION [ ( &lt;exception_number&gt; , '&lt;exception_message&gt;' ) ] ;
    /// </code>
    /// </remarks>
    public Statement ParseSnowflakeDeclare()
    {
        var stmts = new List<Declare>();
        while (true)
        {
            var name = ParseIdentifier();
            // let (declare_type, for_query, assigned_expr, data_type) = if .. { (..) } else ..
            DeclareType? declareType;
            Query? forQuery;
            DeclareAssignment? assignedExpr;
            DataType? dataType;
            if (ParseKeyword(Keyword.CURSOR))
            {
                ExpectKeywordIs(Keyword.FOR);
                switch (PeekTokenRef().Token)
                {
                    case Token.Word { Value: var w } when w.Keyword == Keyword.SELECT:
                        declareType = new DeclareType.Cursor();
                        forQuery = ParseQuery();
                        assignedExpr = null;
                        dataType = null;
                        break;
                    default:
                        declareType = new DeclareType.Cursor();
                        forQuery = null;
                        assignedExpr = new DeclareAssignment.For(ParseExpr());
                        dataType = null;
                        break;
                }
            }
            else if (ParseKeyword(Keyword.RESULTSET))
            {
                DeclareAssignment? resultsetAssignedExpr;
                if (PeekTokenRef().Token is not Token.SemiColon)
                {
                    resultsetAssignedExpr = ParseSnowflakeVariableDeclarationExpression();
                }
                else
                {
                    // Nothing more to do. The statement has no further parameters.
                    resultsetAssignedExpr = null;
                }

                (declareType, forQuery, assignedExpr, dataType) =
                    (new DeclareType.ResultSet(), null, resultsetAssignedExpr, null);
            }
            else if (ParseKeyword(Keyword.EXCEPTION))
            {
                DeclareAssignment? exceptionAssignedExpr;
                if (PeekTokenRef().Token is Token.LParen)
                {
                    exceptionAssignedExpr = new DeclareAssignment.Expr(ParseExpr());
                }
                else
                {
                    // Nothing more to do. The statement has no further parameters.
                    exceptionAssignedExpr = null;
                }

                (declareType, forQuery, assignedExpr, dataType) =
                    (new DeclareType.Exception(), null, exceptionAssignedExpr, null);
            }
            else
            {
                // Without an explicit keyword, the only valid option is variable declaration.
                DeclareAssignment? varAssignedExpr;
                DataType? varDataType;
                if (ParseSnowflakeVariableDeclarationExpression() is { } parsedAssignedExpr)
                {
                    varAssignedExpr = parsedAssignedExpr;
                    varDataType = null;
                }
                else if (PeekTokenRef().Token is Token.Word)
                {
                    var parsedDataType = ParseDataType();
                    varAssignedExpr = ParseSnowflakeVariableDeclarationExpression();
                    varDataType = parsedDataType;
                }
                else
                {
                    varAssignedExpr = null;
                    varDataType = null;
                }
                (declareType, forQuery, assignedExpr, dataType) =
                    (null, null, varAssignedExpr, varDataType);
            }
            var stmt = new Declare(
                Names: Sequence.Of(name),
                DataType: dataType,
                Assignment: assignedExpr,
                DeclareType: declareType,
                Binary: null,
                Sensitive: null,
                Scroll: null,
                Hold: null,
                ForQuery: forQuery);

            stmts.Add(stmt);
            if (ConsumeToken(new Token.SemiColon()))
            {
                switch (PeekTokenRef().Token)
                {
                    // ALL_KEYWORDS.binary_search(&w.value.to_uppercase().as_str()).is_err()
                    // (convencoes.md §13, caso 2: Keywords.Lookup devolve NoKeyword se a palavra não é keyword).
                    // `to_uppercase()` → RustString.ToUppercase (convencoes.md §10): com o mapeamento completo,
                    // `"pa<U+00DF>word"` vira PASSWORD e `"<U+FB01>rst"` vira FIRST, como no Rust.
                    case Token.Word { Value: var w }
                        when Keywords.Lookup(RustString.ToUppercase(w.Value)) == Keyword.NoKeyword:
                        // Not a keyword - start of a new declaration.
                        continue;
                    default:
                        // Put back the semicolon, this is the end of the DECLARE statement.
                        PrevToken();
                        break;
                }
            }

            break;
        }

        return new Statement.Declare(Stmts: stmts.ToSequence());
    }

    /// <summary>
    /// Parse a <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/declare-local-variable-transact-sql?view=sql-server-ver16">MsSql</see>
    /// <c>DECLARE</c> statement.
    /// </summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// DECLARE
    /// {
    ///   { @local_variable [AS] data_type [ = value ] }
    ///   | { @cursor_variable_name CURSOR [ FOR ] }
    /// } [ ,...n ]
    /// </code>
    /// </remarks>
    public Statement ParseMssqlDeclare()
    {
        var stmts = ParseCommaSeparated(p => p.ParseMssqlDeclareStmt());

        return new Statement.Declare(Stmts: stmts);
    }

    /// <summary>
    /// Parse the body of a <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/declare-local-variable-transact-sql?view=sql-server-ver16">MsSql</see>
    /// <c>DECLARE</c>statement.
    /// </summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// {
    ///   { @local_variable [AS] data_type [ = value ] }
    ///   | { @cursor_variable_name CURSOR [ FOR ]}
    /// } [ ,...n ]
    /// </code>
    /// </remarks>
    public Declare ParseMssqlDeclareStmt()
    {
        Ident name;
        {
            var ident = ParseIdentifier();
            if (!ident.Value.StartsWith("@", StringComparison.Ordinal)
                && !(PeekTokenRef().Token is Token.Word { Value: var cursorWord }
                     && cursorWord.Keyword == Keyword.CURSOR))
            {
                throw new ParserException(new ParserError.TokenizerError(
                    "Invalid MsSql variable declaration."));
            }
            else
            {
                name = ident;
            }
        }

        DeclareType? declareType;
        DataType? dataType;
        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var w }:
                switch (w.Keyword)
                {
                    case Keyword.CURSOR:
                        NextToken();
                        (declareType, dataType) = (new DeclareType.Cursor(), null);
                        break;
                    case Keyword.AS:
                        NextToken();
                        (declareType, dataType) = (null, ParseDataType());
                        break;
                    default:
                        (declareType, dataType) = (null, ParseDataType());
                        break;
                }
                break;
            default:
                (declareType, dataType) = (null, ParseDataType());
                break;
        }

        Query? forQuery;
        DeclareAssignment? assignment;
        if (PeekKeyword(Keyword.FOR))
        {
            NextToken();
            Query? query = ParseQuery();
            (forQuery, assignment) = (query, null);
        }
        else
        {
            var parsedAssignment = ParseMssqlVariableDeclarationExpression();
            (forQuery, assignment) = (null, parsedAssignment);
        }

        return new Declare(
            Names: Sequence.Of(name),
            DataType: dataType,
            Assignment: assignment,
            DeclareType: declareType,
            Binary: null,
            Sensitive: null,
            Scroll: null,
            Hold: null,
            ForQuery: forQuery);
    }

    /// <summary>Parses the assigned expression in a variable declaration.</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// [ { DEFAULT | := } &lt;expression&gt;]
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/snowflake-scripting/declare#variable-declaration-syntax"/>
    /// </remarks>
    public DeclareAssignment? ParseSnowflakeVariableDeclarationExpression()
    {
        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var w } when w.Keyword == Keyword.DEFAULT:
                NextToken(); // Skip `DEFAULT`
                return new DeclareAssignment.Default(ParseExpr());
            case Token.Assignment:
                NextToken(); // Skip `:=`
                return new DeclareAssignment.DuckAssignment(
                    ParseExpr());
            default:
                return null;
        }
    }

    /// <summary>Parses the assigned expression in a variable declaration.</summary>
    /// <remarks>
    /// Syntax:
    /// <code>
    /// [ = &lt;expression&gt;]
    /// </code>
    /// </remarks>
    public DeclareAssignment? ParseMssqlVariableDeclarationExpression()
    {
        switch (PeekTokenRef().Token)
        {
            case Token.Eq:
                NextToken(); // Skip `=`
                return new DeclareAssignment.MsSqlAssignment(
                    ParseExpr());
            default:
                return null;
        }
    }

    /// <summary>Parse <c>FETCH [direction] { FROM | IN } cursor INTO target;</c> statement.</summary>
    public Statement ParseFetchStatement()
    {
        FetchDirection direction;
        if (ParseKeyword(Keyword.NEXT))
        {
            direction = new FetchDirection.Next();
        }
        else if (ParseKeyword(Keyword.PRIOR))
        {
            direction = new FetchDirection.Prior();
        }
        else if (ParseKeyword(Keyword.FIRST))
        {
            direction = new FetchDirection.First();
        }
        else if (ParseKeyword(Keyword.LAST))
        {
            direction = new FetchDirection.Last();
        }
        else if (ParseKeyword(Keyword.ABSOLUTE))
        {
            direction = new FetchDirection.Absolute(
                Limit: ParseNumberValue());
        }
        else if (ParseKeyword(Keyword.RELATIVE))
        {
            direction = new FetchDirection.Relative(
                Limit: ParseNumberValue());
        }
        else if (ParseKeyword(Keyword.FORWARD))
        {
            if (ParseKeyword(Keyword.ALL))
            {
                direction = new FetchDirection.ForwardAll();
            }
            else
            {
                direction = new FetchDirection.Forward(
                    // TODO: Support optional
                    Limit: ParseNumberValue());
            }
        }
        else if (ParseKeyword(Keyword.BACKWARD))
        {
            if (ParseKeyword(Keyword.ALL))
            {
                direction = new FetchDirection.BackwardAll();
            }
            else
            {
                direction = new FetchDirection.Backward(
                    // TODO: Support optional
                    Limit: ParseNumberValue());
            }
        }
        else if (ParseKeyword(Keyword.ALL))
        {
            direction = new FetchDirection.All();
        }
        else
        {
            direction = new FetchDirection.Count(
                Limit: ParseNumberValue());
        }

        FetchPosition position;
        if (PeekKeyword(Keyword.FROM))
        {
            ExpectKeyword(Keyword.FROM);
            position = new FetchPosition.From();
        }
        else if (PeekKeyword(Keyword.IN))
        {
            ExpectKeyword(Keyword.IN);
            position = new FetchPosition.In();
        }
        else
        {
            throw ParserErr("Expected FROM or IN", PeekTokenRef().Span.Start);
        }

        var name = ParseIdentifier();

        ObjectName? @into;
        if (ParseKeyword(Keyword.INTO))
        {
            @into = ParseObjectName(false);
        }
        else
        {
            @into = null;
        }

        return new Statement.Fetch(
            Name: name,
            Direction: direction,
            Position: position,
            Into: @into);
    }

    /// <summary>Parse a <c>DISCARD</c> statement.</summary>
    public Statement ParseDiscard()
    {
        DiscardObject objectType;
        if (ParseKeyword(Keyword.ALL))
        {
            objectType = new DiscardObject.ALL();
        }
        else if (ParseKeyword(Keyword.PLANS))
        {
            objectType = new DiscardObject.PLANS();
        }
        else if (ParseKeyword(Keyword.SEQUENCES))
        {
            objectType = new DiscardObject.SEQUENCES();
        }
        else if (ParseKeyword(Keyword.TEMP) || ParseKeyword(Keyword.TEMPORARY))
        {
            objectType = new DiscardObject.TEMP();
        }
        else
        {
            return ExpectedRef<Statement>(
                "ALL, PLANS, SEQUENCES, TEMP or TEMPORARY after DISCARD",
                PeekTokenRef());
        }
        return new Statement.Discard(ObjectType: objectType);
    }
}
