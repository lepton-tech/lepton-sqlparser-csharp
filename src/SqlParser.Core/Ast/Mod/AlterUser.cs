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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>Modifies the properties of a user</summary>
/// <remarks>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-user">Snowflake Syntax:</see>
/// <code>
/// ALTER USER [ IF EXISTS ] [ &lt;name&gt; ] [ OPTIONS ]
/// </code>
/// <see href="https://www.postgresql.org/docs/current/sql-alteruser.html">PostgreSQL Syntax:</see>
/// <code>
/// ALTER USER &lt;role_specification&gt; [ WITH ] option [ ... ]
/// </code>
/// <para>No Rust, o <c>impl Display for AlterUser</c> vem depois de <c>UserPolicyKind</c>; aqui fica no próprio tipo.</para>
/// </remarks>
/// <param name="IfExists">Whether to only alter the user if it exists.</param>
/// <param name="Name">The name of the user to alter.</param>
/// <param name="RenameTo">
/// Optional new name for the user (Snowflake-specific).
/// See: <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-user#syntax"/>
/// </param>
/// <param name="ResetPassword">Reset the user's password.</param>
/// <param name="AbortAllQueries">Abort all running queries for the user.</param>
/// <param name="AddRoleDelegation">Optionally add a delegated role authorization.</param>
/// <param name="RemoveRoleDelegation">Optionally remove a delegated role authorization.</param>
/// <param name="EnrollMfa">Enroll the user in MFA.</param>
/// <param name="SetDefaultMfaMethod">Set the default MFA method for the user.</param>
/// <param name="RemoveMfaMethod">Remove the user's default MFA method.</param>
/// <param name="ModifyMfaMethod">Modify an MFA method for the user.</param>
/// <param name="AddMfaMethodOtp">Add an MFA OTP method with optional count.</param>
/// <param name="SetPolicy">Set a user policy.</param>
/// <param name="UnsetPolicy">Unset a user policy.</param>
/// <param name="SetTag">Key/value tag options to set on the user.</param>
/// <param name="UnsetTag">Tags to unset on the user.</param>
/// <param name="SetProps">Key/value properties to set on the user.</param>
/// <param name="UnsetProps">Properties to unset on the user.</param>
/// <param name="Password">The following options are PostgreSQL-specific: <see href="https://www.postgresql.org/docs/current/sql-alteruser.html"/></param>
public sealed partial record AlterUser(
    bool IfExists,
    Ident Name,
    Ident? RenameTo,
    bool ResetPassword,
    bool AbortAllQueries,
    AlterUserAddRoleDelegation? AddRoleDelegation,
    AlterUserRemoveRoleDelegation? RemoveRoleDelegation,
    bool EnrollMfa,
    MfaMethodKind? SetDefaultMfaMethod,
    MfaMethodKind? RemoveMfaMethod,
    AlterUserModifyMfaMethod? ModifyMfaMethod,
    AlterUserAddMfaMethodOtp? AddMfaMethodOtp,
    AlterUserSetPolicy? SetPolicy,
    UserPolicyKind? UnsetPolicy,
    KeyValueOptions SetTag,
    Sequence<string> UnsetTag,
    KeyValueOptions SetProps,
    Sequence<string> UnsetProps,
    AlterUserPassword? Password) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"ALTER");
        f.Write($" USER");
        if (IfExists)
        {
            f.Write($" IF EXISTS");
        }
        f.Write($" {Name}");
        if (RenameTo is { } newName)
        {
            f.Write($" RENAME TO {newName}");
        }
        if (ResetPassword)
        {
            f.Write($" RESET PASSWORD");
        }
        if (AbortAllQueries)
        {
            f.Write($" ABORT ALL QUERIES");
        }
        if (AddRoleDelegation is { } addRoleDelegation)
        {
            var role = addRoleDelegation.Role;
            var integration = addRoleDelegation.Integration;
            f.Write($" ADD DELEGATED AUTHORIZATION OF ROLE {role} TO SECURITY INTEGRATION {integration}");
        }
        if (RemoveRoleDelegation is { } removeRoleDelegation)
        {
            f.Write($" REMOVE DELEGATED");
            switch (removeRoleDelegation.Role)
            {
                case { } role:
                    f.Write($" AUTHORIZATION OF ROLE {role}");
                    break;
                case null:
                    f.Write($" AUTHORIZATIONS");
                    break;
            }
            var integration = removeRoleDelegation.Integration;
            f.Write($" FROM SECURITY INTEGRATION {integration}");
        }
        if (EnrollMfa)
        {
            f.Write($" ENROLL MFA");
        }
        if (SetDefaultMfaMethod is { } defaultMethod)
        {
            f.Write($" SET DEFAULT_MFA_METHOD {defaultMethod}");
        }
        if (RemoveMfaMethod is { } removeMethod)
        {
            f.Write($" REMOVE MFA METHOD {removeMethod}");
        }
        if (ModifyMfaMethod is { } modify)
        {
            var method = modify.Method;
            var comment = modify.Comment;
            f.Write($" MODIFY MFA METHOD {method} SET COMMENT '{Ast.Value.EscapeSingleQuoteString(comment)}'");
        }
        if (AddMfaMethodOtp is { } addMfaMethodOtp)
        {
            f.Write($" ADD MFA METHOD OTP");
            if (addMfaMethodOtp.Count is { } count)
            {
                f.Write($" COUNT = {count}");
            }
        }
        if (SetPolicy is { } policy)
        {
            var policyKind = policy.PolicyKind;
            var name = policy.Policy;
            f.Write($" SET {policyKind} POLICY {name}");
        }
        if (UnsetPolicy is { } unsetPolicyKind)
        {
            f.Write($" UNSET {unsetPolicyKind} POLICY");
        }
        if (!SetTag.Options.IsEmpty)
        {
            f.Write($" SET TAG {SetTag}");
        }
        if (!UnsetTag.IsEmpty)
        {
            f.Write($" UNSET TAG {DisplayCommaSeparated(UnsetTag)}");
        }
        var hasProps = !SetProps.Options.IsEmpty;
        if (hasProps)
        {
            f.Write($" SET");
            f.Write($" {SetProps}");
        }
        if (!UnsetProps.IsEmpty)
        {
            f.Write($" UNSET {DisplayCommaSeparated(UnsetProps)}");
        }
        if (Password is { } password)
        {
            f.Write($" {password}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
