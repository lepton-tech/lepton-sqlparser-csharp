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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Variants for the <c>SET</c> family of statements.</summary>
public abstract partial record Set : IDisplay
{
    /// <summary>
    /// SQL Standard-style
    /// SET a = 1;
    /// <c>SET var = value</c> (standard SQL-style assignment).
    /// </summary>
    /// <param name="Scope">Optional scope modifier (<c>SESSION</c> / <c>LOCAL</c>).</param>
    /// <param name="Hivevar">Whether this is a Hive-style <c>HIVEVAR:</c> assignment.</param>
    /// <param name="Variable">Variable name to assign.</param>
    /// <param name="Values">Values assigned to the variable.</param>
    public sealed record SingleAssignment(ContextModifier? Scope, bool Hivevar, ObjectName Variable, Sequence<Expr> Values) : Set;

    /// <summary>
    /// Snowflake-style
    /// SET (a, b, ..) = (1, 2, ..);
    /// <c>SET (a, b) = (1, 2)</c> (tuple assignment syntax).
    /// </summary>
    /// <param name="Variables">Variables being assigned in tuple form.</param>
    /// <param name="Values">Corresponding values for the variables.</param>
    public sealed record ParenthesizedAssignments(Sequence<ObjectName> Variables, Sequence<Expr> Values) : Set;

    /// <summary>
    /// MySQL-style
    /// SET a = 1, b = 2, ..;
    /// <c>SET a = 1, b = 2</c> (MySQL-style comma-separated assignments).
    /// </summary>
    /// <param name="Assignments">List of <c>SET</c> assignments (MySQL-style comma-separated).</param>
    public sealed record MultipleAssignments(Sequence<SetAssignment> Assignments) : Set;

    /// <summary>
    /// Session authorization for Postgres/Redshift
    /// <code>
    /// SET SESSION AUTHORIZATION { user_name | DEFAULT }
    /// </code>
    /// See <see href="https://www.postgresql.org/docs/current/sql-set-session-authorization.html"/>
    /// See <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_SET_SESSION_AUTHORIZATION.html"/>
    /// </summary>
    // Value: Rust: campo `.0`.
    public sealed record SetSessionAuthorization(SetSessionAuthorizationParam Value) : Set;

    /// <summary>
    /// MS-SQL session
    /// <para>See <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/set-statements-transact-sql"/></para>
    /// </summary>
    // Value: Rust: campo `.0`.
    public sealed record SetSessionParam(SetSessionParamKind Value) : Set;

    /// <summary>
    /// <code>
    /// SET [ SESSION | LOCAL ] ROLE role_name
    /// </code>
    /// Sets session state. Examples: [ANSI][1], [Postgresql][2], [MySQL][3], and [Oracle][4]
    /// <para>[1]: https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#set-role-statement</para>
    /// <para>[2]: https://www.postgresql.org/docs/14/sql-set-role.html</para>
    /// <para>[3]: https://dev.mysql.com/doc/refman/8.0/en/set-role.html</para>
    /// <para>[4]: https://docs.oracle.com/cd/B19306_01/server.102/b14200/statements_10004.htm</para>
    /// </summary>
    /// <param name="ContextModifier">Non-ANSI optional identifier to inform if the role is defined inside the current session (<c>SESSION</c>) or transaction (<c>LOCAL</c>).</param>
    /// <param name="RoleName">Role name. If NONE is specified, then the current role name is removed.</param>
    public sealed record SetRole(ContextModifier? ContextModifier, Ident? RoleName) : Set;

    /// <summary>
    /// <code>
    /// SET TIME ZONE &lt;value&gt;
    /// </code>
    /// Note: this is a PostgreSQL-specific statements
    /// <c>SET TIME ZONE &lt;value&gt;</c> is an alias for <c>SET timezone TO &lt;value&gt;</c> in PostgreSQL
    /// However, we allow it for all dialects.
    /// <c>SET TIME ZONE</c> statement. <c>local</c> indicates the <c>LOCAL</c> keyword.
    /// <c>SET TIME ZONE &lt;value&gt;</c> statement.
    /// </summary>
    /// <param name="Local">Whether the <c>LOCAL</c> keyword was specified.</param>
    /// <param name="Value">Time zone expression value.</param>
    public sealed record SetTimeZone(bool Local, Expr Value) : Set;

    /// <summary>
    /// <code>
    /// SET NAMES 'charset_name' [COLLATE 'collation_name']
    /// </code>
    /// </summary>
    /// <param name="CharsetName">Character set name to set.</param>
    /// <param name="CollationName">Optional collation name.</param>
    public sealed record SetNames(Ident CharsetName, string? CollationName) : Set;

    /// <summary>
    /// <code>
    /// SET NAMES DEFAULT
    /// </code>
    /// Note: this is a MySQL-specific statement.
    /// </summary>
    // Rust: variante-struct sem campos (`SetNamesDefault {}`), tratada como unitária.
    public sealed record SetNamesDefault : Set;

    /// <summary>
    /// <code>
    /// SET TRANSACTION ...
    /// </code>
    /// </summary>
    /// <param name="Modes">Transaction modes (e.g., ISOLATION LEVEL, READ ONLY).</param>
    /// <param name="Snapshot">Optional snapshot value for transaction snapshot control.</param>
    /// <param name="Session"><c>true</c> when the <c>SESSION</c> keyword was used.</param>
    public sealed record SetTransaction(Sequence<TransactionMode> Modes, ValueWithSpan? Snapshot, bool Session) : Set;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case ParenthesizedAssignments parenthesizedAssignments:
                f.Write($"SET ({DisplayCommaSeparated(parenthesizedAssignments.Variables)}) = ({DisplayCommaSeparated(parenthesizedAssignments.Values)})");
                break;
            case MultipleAssignments multipleAssignments:
                f.Write($"SET {DisplayCommaSeparated(multipleAssignments.Assignments)}");
                break;
            case SetRole setRole:
            {
                var roleName = setRole.RoleName ?? new Ident("NONE");
                // modifier = context_modifier.map(|m| format!("{m}")).unwrap_or_default()
                var modifier = setRole.ContextModifier is { } m ? Formatter.Format(m) : "";
                f.Write($"SET {modifier}ROLE {roleName}");
                break;
            }
            case SetSessionAuthorization kind:
                f.Write($"SET SESSION AUTHORIZATION {kind.Value}");
                break;
            case SetSessionParam kind:
                f.Write($"SET {kind.Value}");
                break;
            case SetTransaction setTransaction:
            {
                if (setTransaction.Session)
                {
                    f.Write($"SET SESSION CHARACTERISTICS AS TRANSACTION");
                }
                else
                {
                    f.Write($"SET TRANSACTION");
                }
                if (!setTransaction.Modes.IsEmpty)
                {
                    f.Write($" {DisplayCommaSeparated(setTransaction.Modes)}");
                }
                if (setTransaction.Snapshot is { } snapshotId)
                {
                    f.Write($" SNAPSHOT {snapshotId}");
                }
                break;
            }
            case SetTimeZone setTimeZone:
            {
                f.WriteStr("SET ");
                if (setTimeZone.Local)
                {
                    f.WriteStr("LOCAL ");
                }
                f.Write($"TIME ZONE {setTimeZone.Value}");
                break;
            }
            case SetNames setNames:
            {
                f.Write($"SET NAMES {setNames.CharsetName}");

                if (setNames.CollationName is { } collation)
                {
                    f.WriteStr(" COLLATE ");
                    f.WriteStr(collation);
                }
                break;
            }
            case SetNamesDefault:
                f.WriteStr("SET NAMES DEFAULT");
                break;
            case SingleAssignment singleAssignment:
            {
                // scope.map(|s| format!("{s}")).unwrap_or_default()
                var scope = singleAssignment.Scope is { } s ? Formatter.Format(s) : "";
                f.Write($"SET {scope}{(singleAssignment.Hivevar ? "HIVEVAR:" : "")}{singleAssignment.Variable} = {DisplayCommaSeparated(singleAssignment.Values)}");
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de Set não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
