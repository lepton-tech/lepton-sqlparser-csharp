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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>An <c>ALTER ROLE</c> (<c>Statement::AlterRole</c>) operation</summary>
public abstract partial record AlterRoleOperation : IDisplay
{
    /// <summary>Generic</summary>
    /// <param name="RoleName">Role name to rename.</param>
    public sealed record RenameRole(Ident RoleName) : AlterRoleOperation;

    /// <summary>
    /// MS SQL Server
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/alter-role-transact-sql"/>
    /// </summary>
    /// <param name="MemberName">Member name to add to the role.</param>
    public sealed record AddMember(Ident MemberName) : AlterRoleOperation;

    /// <summary>
    /// MS SQL Server
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/alter-role-transact-sql"/>
    /// </summary>
    /// <param name="MemberName">Member name to remove from the role.</param>
    public sealed record DropMember(Ident MemberName) : AlterRoleOperation;

    /// <summary>
    /// PostgreSQL
    /// <see href="https://www.postgresql.org/docs/current/sql-alterrole.html"/>
    /// </summary>
    /// <param name="Options">Role options to apply.</param>
    public sealed record WithOptions(Sequence<RoleOption> Options) : AlterRoleOperation;

    /// <summary>
    /// PostgreSQL
    /// <see href="https://www.postgresql.org/docs/current/sql-alterrole.html"/>
    /// </summary>
    /// <remarks><c>SET configuration_parameter { TO | = } { value | DEFAULT }</c></remarks>
    /// <param name="ConfigName">Configuration name to set.</param>
    /// <param name="ConfigValue">Value to assign to the configuration.</param>
    /// <param name="InDatabase">Optional database scope for the setting.</param>
    public sealed record Set(ObjectName ConfigName, SetConfigValue ConfigValue, ObjectName? InDatabase) : AlterRoleOperation;

    /// <summary>
    /// PostgreSQL
    /// <see href="https://www.postgresql.org/docs/current/sql-alterrole.html"/>
    /// </summary>
    /// <remarks><c>RESET configuration_parameter</c> | <c>RESET ALL</c></remarks>
    /// <param name="ConfigName">Configuration to reset.</param>
    /// <param name="InDatabase">Optional database scope for the reset.</param>
    public sealed record Reset(ResetConfig ConfigName, ObjectName? InDatabase) : AlterRoleOperation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case RenameRole renameRole:
                f.Write($"RENAME TO {renameRole.RoleName}");
                break;
            case AddMember addMember:
                f.Write($"ADD MEMBER {addMember.MemberName}");
                break;
            case DropMember dropMember:
                f.Write($"DROP MEMBER {dropMember.MemberName}");
                break;
            case WithOptions withOptions:
                f.Write($"WITH {DisplaySeparated(withOptions.Options, " ")}");
                break;
            case Set set:
            {
                if (set.InDatabase is { } databaseName)
                {
                    f.Write($"IN DATABASE {databaseName} ");
                }

                switch (set.ConfigValue)
                {
                    case SetConfigValue.Default:
                        f.Write($"SET {set.ConfigName} TO DEFAULT");
                        break;
                    case SetConfigValue.FromCurrent:
                        f.Write($"SET {set.ConfigName} FROM CURRENT");
                        break;
                    case SetConfigValue.Value expr:
                        f.Write($"SET {set.ConfigName} TO {expr.Inner}");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de SetConfigValue não tratada: {set.ConfigValue.GetType().Name}");
                }
                break;
            }
            case Reset reset:
            {
                if (reset.InDatabase is { } databaseName)
                {
                    f.Write($"IN DATABASE {databaseName} ");
                }

                switch (reset.ConfigName)
                {
                    case ResetConfig.ALL:
                        f.Write($"RESET ALL");
                        break;
                    case ResetConfig.ConfigName name:
                        f.Write($"RESET {name.Value}");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de ResetConfig não tratada: {reset.ConfigName.GetType().Name}");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de AlterRoleOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
