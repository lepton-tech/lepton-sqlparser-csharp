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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/alter.rs.
// Modificado: tradução de Rust para C#.
//
// Rust: `//! SQL Parser for ALTER`. O submódulo tem um bloco `impl Parser<'_>`; em C#, um `partial`
// do Parser (convencoes.md §2).

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Parse <c>ALTER ROLE</c> statement</summary>
    public Statement ParseAlterRole()
    {
        if (_dialect is PostgreSqlDialect)
        {
            return ParsePgAlterRole();
        }
        else if (_dialect is MsSqlDialect)
        {
            return ParseMssqlAlterRole();
        }

        throw new ParserException(new ParserError.ParserErrorValue(
            "ALTER ROLE is only support for PostgreSqlDialect, MsSqlDialect"));
    }

    /// <summary>
    /// Parse ALTER POLICY statement
    /// <code>
    /// ALTER POLICY policy_name ON table_name [ RENAME TO new_name ]
    /// or
    /// ALTER POLICY policy_name ON table_name
    /// [ TO { role_name | PUBLIC | CURRENT_ROLE | CURRENT_USER | SESSION_USER } [, ...] ]
    /// [ USING ( using_expression ) ]
    /// [ WITH CHECK ( check_expression ) ]
    /// </code>
    /// <see href="https://www.postgresql.org/docs/current/sql-alterpolicy.html">PostgreSQL</see>
    /// </summary>
    public AlterPolicy ParseAlterPolicy()
    {
        var name = ParseIdentifier();
        ExpectKeywordIs(Keyword.ON);
        var tableName = ParseObjectName(false);

        if (ParseKeyword(Keyword.RENAME))
        {
            ExpectKeywordIs(Keyword.TO);
            var newName = ParseIdentifier();
            return new AlterPolicy(
                Name: name,
                TableName: tableName,
                Operation: new AlterPolicyOperation.Rename(NewName: newName));
        }
        else
        {
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
            return new AlterPolicy(
                Name: name,
                TableName: tableName,
                Operation: new AlterPolicyOperation.Apply(
                    To: to,
                    Using: @using,
                    WithCheck: withCheck));
        }
    }

    /// <summary>
    /// Parse an <c>ALTER CONNECTOR</c> statement
    /// <code>
    /// ALTER CONNECTOR connector_name SET DCPROPERTIES(property_name=property_value, ...);
    ///
    /// ALTER CONNECTOR connector_name SET URL new_url;
    ///
    /// ALTER CONNECTOR connector_name SET OWNER [USER|ROLE] user_or_role;
    /// </code>
    /// </summary>
    public Statement ParseAlterConnector()
    {
        var name = ParseIdentifier();
        ExpectKeywordIs(Keyword.SET);

        // Rust: `match .. { properties if !properties.is_empty() => Some(properties), _ => None }`
        // (o `properties` do braço sombreia o externo: caso 72).
        Sequence<SqlOption>? properties;
        switch (ParseOptionsWithKeywords(Keyword.DCPROPERTIES))
        {
            case var parsedProperties when !parsedProperties.IsEmpty:
                properties = parsedProperties;
                break;
            default:
                properties = null;
                break;
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

        // Rust: os `let owner` internos sombreiam o externo (caso 72: `ownerName`).
        AlterConnectorOwner? owner;
        if (ParseKeywords(Keyword.OWNER, Keyword.USER))
        {
            var ownerName = ParseIdentifier();
            owner = new AlterConnectorOwner.User(ownerName);
        }
        else if (ParseKeywords(Keyword.OWNER, Keyword.ROLE))
        {
            var ownerName = ParseIdentifier();
            owner = new AlterConnectorOwner.Role(ownerName);
        }
        else
        {
            owner = null;
        }

        return new Statement.AlterConnector(
            Name: name,
            Properties: properties,
            Url: url,
            Owner: owner);
    }

    /// <summary>
    /// Parse an <c>ALTER USER</c> statement
    /// <code>
    /// ALTER USER [ IF EXISTS ] [ &lt;name&gt; ] [ OPTIONS ]
    /// </code>
    /// </summary>
    public AlterUser ParseAlterUser()
    {
        var ifExists = ParseKeywords(Keyword.IF, Keyword.EXISTS);
        var name = ParseIdentifier();
        _ = ParseKeyword(Keyword.WITH);
        Ident? renameTo;
        if (ParseKeywords(Keyword.RENAME, Keyword.TO))
        {
            renameTo = ParseIdentifier();
        }
        else
        {
            renameTo = null;
        }
        var resetPassword = ParseKeywords(Keyword.RESET, Keyword.PASSWORD);
        var abortAllQueries =
            ParseKeywords(Keyword.ABORT, Keyword.ALL, Keyword.QUERIES);
        AlterUserAddRoleDelegation? addRoleDelegation;
        if (ParseKeywords(
            Keyword.ADD,
            Keyword.DELEGATED,
            Keyword.AUTHORIZATION,
            Keyword.OF,
            Keyword.ROLE))
        {
            var role = ParseIdentifier();
            ExpectKeywords(Keyword.TO, Keyword.SECURITY, Keyword.INTEGRATION);
            var integration = ParseIdentifier();
            addRoleDelegation = new AlterUserAddRoleDelegation(Role: role, Integration: integration);
        }
        else
        {
            addRoleDelegation = null;
        }
        AlterUserRemoveRoleDelegation? removeRoleDelegation;
        if (ParseKeywords(Keyword.REMOVE, Keyword.DELEGATED))
        {
            Ident? role;
            if (ParseKeywords(Keyword.AUTHORIZATION, Keyword.OF, Keyword.ROLE))
            {
                role = ParseIdentifier();
            }
            else if (ParseKeyword(Keyword.AUTHORIZATIONS))
            {
                role = null;
            }
            else
            {
                return ExpectedRef<AlterUser>(
                    "REMOVE DELEGATED AUTHORIZATION OF ROLE | REMOVE DELEGATED AUTHORIZATIONS",
                    PeekTokenRef());
            }
            ExpectKeywords(Keyword.FROM, Keyword.SECURITY, Keyword.INTEGRATION);
            var integration = ParseIdentifier();
            removeRoleDelegation = new AlterUserRemoveRoleDelegation(Role: role, Integration: integration);
        }
        else
        {
            removeRoleDelegation = null;
        }
        var enrollMfa = ParseKeywords(Keyword.ENROLL, Keyword.MFA);
        MfaMethodKind? setDefaultMfaMethod;
        if (ParseKeywords(Keyword.SET, Keyword.DEFAULT_MFA_METHOD))
        {
            setDefaultMfaMethod = ParseMfaMethod();
        }
        else
        {
            setDefaultMfaMethod = null;
        }
        MfaMethodKind? removeMfaMethod;
        if (ParseKeywords(Keyword.REMOVE, Keyword.MFA, Keyword.METHOD))
        {
            removeMfaMethod = ParseMfaMethod();
        }
        else
        {
            removeMfaMethod = null;
        }
        AlterUserModifyMfaMethod? modifyMfaMethod;
        if (ParseKeywords(Keyword.MODIFY, Keyword.MFA, Keyword.METHOD))
        {
            var method = ParseMfaMethod();
            ExpectKeywords(Keyword.SET, Keyword.COMMENT);
            var comment = ParseLiteralString();
            modifyMfaMethod = new AlterUserModifyMfaMethod(Method: method, Comment: comment);
        }
        else
        {
            modifyMfaMethod = null;
        }
        AlterUserAddMfaMethodOtp? addMfaMethodOtp;
        if (ParseKeywords(Keyword.ADD, Keyword.MFA, Keyword.METHOD, Keyword.OTP))
        {
            ValueWithSpan? count;
            if (ParseKeyword(Keyword.COUNT))
            {
                ExpectToken(new Token.Eq());
                count = ParseValue();
            }
            else
            {
                count = null;
            }
            addMfaMethodOtp = new AlterUserAddMfaMethodOtp(Count: count);
        }
        else
        {
            addMfaMethodOtp = null;
        }
        AlterUserSetPolicy? setPolicy;
        if (ParseKeywords(Keyword.SET, Keyword.AUTHENTICATION, Keyword.POLICY))
        {
            setPolicy = new AlterUserSetPolicy(
                PolicyKind: new UserPolicyKind.Authentication(),
                Policy: ParseIdentifier());
        }
        else if (ParseKeywords(Keyword.SET, Keyword.PASSWORD, Keyword.POLICY))
        {
            setPolicy = new AlterUserSetPolicy(
                PolicyKind: new UserPolicyKind.Password(),
                Policy: ParseIdentifier());
        }
        else if (ParseKeywords(Keyword.SET, Keyword.SESSION, Keyword.POLICY))
        {
            setPolicy = new AlterUserSetPolicy(
                PolicyKind: new UserPolicyKind.Session(),
                Policy: ParseIdentifier());
        }
        else
        {
            setPolicy = null;
        }

        UserPolicyKind? unsetPolicy;
        if (ParseKeywords(Keyword.UNSET, Keyword.AUTHENTICATION, Keyword.POLICY))
        {
            unsetPolicy = new UserPolicyKind.Authentication();
        }
        else if (ParseKeywords(Keyword.UNSET, Keyword.PASSWORD, Keyword.POLICY))
        {
            unsetPolicy = new UserPolicyKind.Password();
        }
        else if (ParseKeywords(Keyword.UNSET, Keyword.SESSION, Keyword.POLICY))
        {
            unsetPolicy = new UserPolicyKind.Session();
        }
        else
        {
            unsetPolicy = null;
        }

        KeyValueOptions setTag;
        if (ParseKeywords(Keyword.SET, Keyword.TAG))
        {
            setTag = ParseKeyValueOptions(false);
        }
        else
        {
            setTag = new KeyValueOptions(
                Delimiter: KeyValueOptionsDelimiter.Comma,
                Options: Sequence<KeyValueOption>.Empty);
        }

        Sequence<string> unsetTag;
        if (ParseKeywords(Keyword.UNSET, Keyword.TAG))
        {
            unsetTag = ParseCommaSeparated(p => p.ParseIdentifier())
                .Select(i => i.ToString())
                .ToSequence();
        }
        else
        {
            unsetTag = Sequence<string>.Empty;
        }

        KeyValueOptions setProps;
        if (ParseKeyword(Keyword.SET))
        {
            setProps = ParseKeyValueOptions(false);
        }
        else
        {
            setProps = new KeyValueOptions(
                Delimiter: KeyValueOptionsDelimiter.Comma,
                Options: Sequence<KeyValueOption>.Empty);
        }

        Sequence<string> unsetProps;
        if (ParseKeyword(Keyword.UNSET))
        {
            unsetProps = ParseCommaSeparated(p => p.ParseIdentifier())
                .Select(i => i.ToString())
                .ToSequence();
        }
        else
        {
            unsetProps = Sequence<string>.Empty;
        }

        var encrypted = ParseKeyword(Keyword.ENCRYPTED);
        // Rust: o `let password` interno sombreia o externo (caso 72: `passwordText`).
        AlterUserPassword? password;
        if (ParseKeyword(Keyword.PASSWORD))
        {
            string? passwordText;
            if (ParseKeyword(Keyword.NULL))
            {
                passwordText = null;
            }
            else
            {
                passwordText = ParseLiteralString();
            }
            password = new AlterUserPassword(
                Encrypted: encrypted,
                Password: passwordText);
        }
        else
        {
            password = null;
        }

        return new AlterUser(
            IfExists: ifExists,
            Name: name,
            RenameTo: renameTo,
            ResetPassword: resetPassword,
            AbortAllQueries: abortAllQueries,
            AddRoleDelegation: addRoleDelegation,
            RemoveRoleDelegation: removeRoleDelegation,
            EnrollMfa: enrollMfa,
            SetDefaultMfaMethod: setDefaultMfaMethod,
            RemoveMfaMethod: removeMfaMethod,
            ModifyMfaMethod: modifyMfaMethod,
            AddMfaMethodOtp: addMfaMethodOtp,
            SetPolicy: setPolicy,
            UnsetPolicy: unsetPolicy,
            SetTag: setTag,
            UnsetTag: unsetTag,
            SetProps: setProps,
            UnsetProps: unsetProps,
            Password: password);
    }

    internal MfaMethodKind ParseMfaMethod()
    {
        if (ParseKeyword(Keyword.PASSKEY))
        {
            return new MfaMethodKind.PassKey();
        }
        else if (ParseKeyword(Keyword.TOTP))
        {
            return new MfaMethodKind.Totp();
        }
        else if (ParseKeyword(Keyword.DUO))
        {
            return new MfaMethodKind.Duo();
        }
        else
        {
            return ExpectedRef<MfaMethodKind>("PASSKEY, TOTP or DUO", PeekTokenRef());
        }
    }

    internal Statement ParseMssqlAlterRole()
    {
        var roleName = ParseIdentifier();

        AlterRoleOperation operation;
        if (ParseKeywords(Keyword.ADD, Keyword.MEMBER))
        {
            var memberName = ParseIdentifier();
            operation = new AlterRoleOperation.AddMember(MemberName: memberName);
        }
        else if (ParseKeywords(Keyword.DROP, Keyword.MEMBER))
        {
            var memberName = ParseIdentifier();
            operation = new AlterRoleOperation.DropMember(MemberName: memberName);
        }
        else if (ParseKeywords(Keyword.WITH, Keyword.NAME))
        {
            if (ConsumeToken(new Token.Eq()))
            {
                // Rust: `let role_name` interno sombreia o externo (caso 72).
                var newRoleName = ParseIdentifier();
                operation = new AlterRoleOperation.RenameRole(RoleName: newRoleName);
            }
            else
            {
                return ExpectedRef<Statement>("= after WITH NAME ", PeekTokenRef());
            }
        }
        else
        {
            return ExpectedRef<Statement>("'ADD' or 'DROP' or 'WITH NAME'", PeekTokenRef());
        }

        return new Statement.AlterRole(
            Name: roleName,
            Operation: operation);
    }

    internal Statement ParsePgAlterRole()
    {
        var roleName = ParseIdentifier();

        // [ IN DATABASE _`database_name`_ ]
        ObjectName? inDatabase;
        if (ParseKeywords(Keyword.IN, Keyword.DATABASE))
        {
            // Rust: `self.parse_object_name(false).ok()` (decisão 1 da B2: sem restaurar a posição).
            try
            {
                inDatabase = ParseObjectName(false);
            }
            catch (ParserException)
            {
                inDatabase = null;
            }
        }
        else
        {
            inDatabase = null;
        }

        AlterRoleOperation operation;
        if (ParseKeyword(Keyword.RENAME))
        {
            if (ParseKeyword(Keyword.TO))
            {
                // Rust: `let role_name` interno sombreia o externo (caso 72).
                var newRoleName = ParseIdentifier();
                operation = new AlterRoleOperation.RenameRole(RoleName: newRoleName);
            }
            else
            {
                return ExpectedRef<Statement>("TO after RENAME", PeekTokenRef());
            }
        // SET
        }
        else if (ParseKeyword(Keyword.SET))
        {
            var configName = ParseObjectName(false);
            // FROM CURRENT
            if (ParseKeywords(Keyword.FROM, Keyword.CURRENT))
            {
                operation = new AlterRoleOperation.Set(
                    ConfigName: configName,
                    ConfigValue: new SetConfigValue.FromCurrent(),
                    InDatabase: inDatabase);
            // { TO | = } { value | DEFAULT }
            }
            else if (ConsumeToken(new Token.Eq()) || ParseKeyword(Keyword.TO))
            {
                if (ParseKeyword(Keyword.DEFAULT))
                {
                    operation = new AlterRoleOperation.Set(
                        ConfigName: configName,
                        ConfigValue: new SetConfigValue.Default(),
                        InDatabase: inDatabase);
                }
                else
                {
                    // Rust: `else if let Ok(expr) = self.parse_expr()`: o erro é descartado, sem
                    // restaurar a posição (decisão 3 da B1).
                    Expr? expr;
                    try
                    {
                        expr = ParseExpr();
                    }
                    catch (ParserException)
                    {
                        expr = null;
                    }
                    if (expr is not null)
                    {
                        operation = new AlterRoleOperation.Set(
                            ConfigName: configName,
                            ConfigValue: new SetConfigValue.Value(expr),
                            InDatabase: inDatabase);
                    }
                    else
                    {
                        operation = ExpectedRef<AlterRoleOperation>("config value", PeekTokenRef());
                    }
                }
            }
            else
            {
                operation = ExpectedRef<AlterRoleOperation>("'TO' or '=' or 'FROM CURRENT'", PeekTokenRef());
            }
        // RESET
        }
        else if (ParseKeyword(Keyword.RESET))
        {
            if (ParseKeyword(Keyword.ALL))
            {
                operation = new AlterRoleOperation.Reset(
                    ConfigName: new ResetConfig.ALL(),
                    InDatabase: inDatabase);
            }
            else
            {
                var configName = ParseObjectName(false);
                operation = new AlterRoleOperation.Reset(
                    ConfigName: new ResetConfig.ConfigName(configName),
                    InDatabase: inDatabase);
            }
        // option
        }
        else
        {
            // [ WITH ]
            _ = ParseKeyword(Keyword.WITH);
            // option
            var options = new List<RoleOption>();
            while (MaybeParse(parser => parser.ParsePgRoleOption()) is { } opt)
            {
                options.Add(opt);
            }
            // check option
            if (options.Count == 0)
            {
                return ExpectedRef<Statement>("option", PeekTokenRef());
            }

            operation = new AlterRoleOperation.WithOptions(Options: options.ToSequence());
        }

        return new Statement.AlterRole(
            Name: roleName,
            Operation: operation);
    }

    internal RoleOption ParsePgRoleOption()
    {
        RoleOption option;
        switch (ParseOneOfKeywords(
            Keyword.BYPASSRLS,
            Keyword.NOBYPASSRLS,
            Keyword.CONNECTION,
            Keyword.CREATEDB,
            Keyword.NOCREATEDB,
            Keyword.CREATEROLE,
            Keyword.NOCREATEROLE,
            Keyword.INHERIT,
            Keyword.NOINHERIT,
            Keyword.LOGIN,
            Keyword.NOLOGIN,
            Keyword.PASSWORD,
            Keyword.REPLICATION,
            Keyword.NOREPLICATION,
            Keyword.SUPERUSER,
            Keyword.NOSUPERUSER,
            Keyword.VALID))
        {
            case Keyword.BYPASSRLS:
                option = new RoleOption.BypassRLS(true);
                break;
            case Keyword.NOBYPASSRLS:
                option = new RoleOption.BypassRLS(false);
                break;
            case Keyword.CONNECTION:
                ExpectKeywordIs(Keyword.LIMIT);
                option = new RoleOption.ConnectionLimit(new Expr.Value(ParseNumberValue()));
                break;
            case Keyword.CREATEDB:
                option = new RoleOption.CreateDB(true);
                break;
            case Keyword.NOCREATEDB:
                option = new RoleOption.CreateDB(false);
                break;
            case Keyword.CREATEROLE:
                option = new RoleOption.CreateRole(true);
                break;
            case Keyword.NOCREATEROLE:
                option = new RoleOption.CreateRole(false);
                break;
            case Keyword.INHERIT:
                option = new RoleOption.Inherit(true);
                break;
            case Keyword.NOINHERIT:
                option = new RoleOption.Inherit(false);
                break;
            case Keyword.LOGIN:
                option = new RoleOption.Login(true);
                break;
            case Keyword.NOLOGIN:
                option = new RoleOption.Login(false);
                break;
            case Keyword.PASSWORD:
            {
                Password password;
                if (ParseKeyword(Keyword.NULL))
                {
                    password = new Password.NullPassword();
                }
                else
                {
                    password = new Password.PasswordValue(new Expr.Value(ParseValue()));
                }
                option = new RoleOption.Password(password);
                break;
            }
            case Keyword.REPLICATION:
                option = new RoleOption.Replication(true);
                break;
            case Keyword.NOREPLICATION:
                option = new RoleOption.Replication(false);
                break;
            case Keyword.SUPERUSER:
                option = new RoleOption.SuperUser(true);
                break;
            case Keyword.NOSUPERUSER:
                option = new RoleOption.SuperUser(false);
                break;
            case Keyword.VALID:
                ExpectKeywordIs(Keyword.UNTIL);
                option = new RoleOption.ValidUntil(new Expr.Value(ParseValue()));
                break;
            default:
                option = ExpectedRef<RoleOption>("option", PeekTokenRef());
                break;
        }

        return option;
    }
}
