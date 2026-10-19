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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dcl.rs.
// Modificado: tradução de Rust para C#.

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// CREATE ROLE statement
/// See <see href="https://www.postgresql.org/docs/current/sql-createrole.html">PostgreSQL</see>
/// </summary>
/// <param name="Names">Role names to create.</param>
/// <param name="IfNotExists">Whether <c>IF NOT EXISTS</c> was specified.</param>
/// <param name="Login">Whether <c>LOGIN</c> was specified.</param>
/// <param name="Inherit">Whether <c>INHERIT</c> was specified.</param>
/// <param name="Bypassrls">Whether <c>BYPASSRLS</c> was specified.</param>
/// <param name="Password">Optional password for the role.</param>
/// <param name="Superuser">Whether <c>SUPERUSER</c> was specified.</param>
/// <param name="CreateDb">Whether <c>CREATEDB</c> was specified.</param>
/// <param name="CreateRoleValue">Whether <c>CREATEROLE</c> was specified.</param>
/// <param name="Replication">Whether <c>REPLICATION</c> privilege was specified.</param>
/// <param name="ConnectionLimit">Optional connection limit expression.</param>
/// <param name="ValidUntil">Optional account validity expression.</param>
/// <param name="InRole">Members of <c>IN ROLE</c> clause.</param>
/// <param name="InGroup">Members of <c>IN GROUP</c> clause.</param>
/// <param name="Role">Roles listed in <c>ROLE</c> clause.</param>
/// <param name="User">Users listed in <c>USER</c> clause.</param>
/// <param name="Admin">Admin users listed in <c>ADMIN</c> clause.</param>
/// <param name="AuthorizationOwner">Optional authorization owner.</param>
// O campo Rust `create_role` tem o nome do tipo que o contém (CS0542): sufixo `Value`
// (convencoes.md §13, caso 41) → `CreateRoleValue`.
public sealed partial record CreateRole(
    Sequence<ObjectName> Names,
    bool IfNotExists,
    // Postgres
    bool? Login,
    bool? Inherit,
    bool? Bypassrls,
    Password? Password,
    bool? Superuser,
    bool? CreateDb,
    bool? CreateRoleValue,
    bool? Replication,
    Expr? ConnectionLimit,
    Expr? ValidUntil,
    Sequence<Ident> InRole,
    Sequence<Ident> InGroup,
    Sequence<Ident> Role,
    Sequence<Ident> User,
    Sequence<Ident> Admin,
    // MSSQL
    ObjectName? AuthorizationOwner) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        // Argumentos nomeados do write! do Rust.
        var ifNotExists = IfNotExists ? "IF NOT EXISTS " : "";
        var names = DisplaySeparated(Names, ", ");
        var superuser = Superuser switch
        {
            true => " SUPERUSER",
            false => " NOSUPERUSER",
            null => "",
        };
        var createDb = CreateDb switch
        {
            true => " CREATEDB",
            false => " NOCREATEDB",
            null => "",
        };
        var createRole = CreateRoleValue switch
        {
            true => " CREATEROLE",
            false => " NOCREATEROLE",
            null => "",
        };
        var inherit = Inherit switch
        {
            true => " INHERIT",
            false => " NOINHERIT",
            null => "",
        };
        var login = Login switch
        {
            true => " LOGIN",
            false => " NOLOGIN",
            null => "",
        };
        var replication = Replication switch
        {
            true => " REPLICATION",
            false => " NOREPLICATION",
            null => "",
        };
        var bypassrls = Bypassrls switch
        {
            true => " BYPASSRLS",
            false => " NOBYPASSRLS",
            null => "",
        };
        f.Write($"CREATE ROLE {ifNotExists}{names}{superuser}{createDb}{createRole}{inherit}{login}{replication}{bypassrls}");
        if (ConnectionLimit is { } limit)
        {
            f.Write($" CONNECTION LIMIT {limit}");
        }
        switch (Password)
        {
            case Ast.Password.PasswordValue pass:
                f.Write($" PASSWORD {pass.Value}");
                break;
            case Ast.Password.NullPassword:
                f.Write($" PASSWORD NULL");
                break;
            case null:
                break;
            default:
                throw new InvalidOperationException($"Variante de Password não tratada: {Password?.GetType().Name}");
        }
        if (ValidUntil is { } until)
        {
            f.Write($" VALID UNTIL {until}");
        }
        if (!InRole.IsEmpty)
        {
            f.Write($" IN ROLE {DisplayCommaSeparated(InRole)}");
        }
        if (!InGroup.IsEmpty)
        {
            f.Write($" IN GROUP {DisplayCommaSeparated(InGroup)}");
        }
        if (!Role.IsEmpty)
        {
            f.Write($" ROLE {DisplayCommaSeparated(Role)}");
        }
        if (!User.IsEmpty)
        {
            f.Write($" USER {DisplayCommaSeparated(User)}");
        }
        if (!Admin.IsEmpty)
        {
            f.Write($" ADMIN {DisplayCommaSeparated(Admin)}");
        }
        if (AuthorizationOwner is { } owner)
        {
            f.Write($" AUTHORIZATION {owner}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for CreateRole (dcl.rs:428)
    public Span Span() => Core.Span.Empty();
}
