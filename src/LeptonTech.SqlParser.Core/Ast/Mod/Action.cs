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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A privilege on a database object (table, sequence, etc.).</summary>
// Fora de `LeptonTech.SqlParser.Core.Ast`, o nome `Action` é ambíguo com `System.Action` (using global
// implícito): use `Ast.Action` nesses arquivos.
public abstract partial record Action : IDisplay
{
    /// <summary>Add a search optimization.</summary>
    public sealed record AddSearchOptimization : Action;

    /// <summary>Apply an <c>APPLY</c> operation with a specific type.</summary>
    /// <param name="ApplyType">The type of apply operation.</param>
    public sealed record Apply(ActionApplyType ApplyType) : Action;

    /// <summary>Apply a budget operation.</summary>
    public sealed record ApplyBudget : Action;

    /// <summary>Attach a listing.</summary>
    public sealed record AttachListing : Action;

    /// <summary>Attach a policy.</summary>
    public sealed record AttachPolicy : Action;

    /// <summary>Audit operation.</summary>
    public sealed record Audit : Action;

    /// <summary>Bind a service endpoint.</summary>
    public sealed record BindServiceEndpoint : Action;

    /// <summary>Connect permission.</summary>
    public sealed record Connect : Action;

    /// <summary>Create action, optionally specifying an object type.</summary>
    /// <param name="ObjType">Optional object type to create.</param>
    public sealed record Create(ActionCreateObjectType? ObjType) : Action;

    /// <summary>Actions related to database roles.</summary>
    /// <param name="Role">The role name.</param>
    public sealed record DatabaseRole(ObjectName Role) : Action
    {
        // Propriedade declarada com `new`: o campo tem o nome da variante irmã `Action.Role` (CS8866).
        /// <summary>The role name.</summary>
        public new ObjectName Role { get; init; } = Role;
    }

    /// <summary>Delete permission.</summary>
    public sealed record Delete : Action;

    /// <summary>Drop permission.</summary>
    public sealed record Drop : Action;

    /// <summary>Evolve schema permission.</summary>
    public sealed record EvolveSchema : Action;

    /// <summary>Exec action (execute) with optional object type.</summary>
    /// <param name="ObjType">Optional execute object type.</param>
    public sealed record Exec(ActionExecuteObjectType? ObjType) : Action;

    /// <summary>Execute action with optional object type.</summary>
    /// <param name="ObjType">Optional execute object type.</param>
    public sealed record Execute(ActionExecuteObjectType? ObjType) : Action;

    /// <summary>Failover operation.</summary>
    public sealed record Failover : Action;

    /// <summary>Use imported privileges.</summary>
    public sealed record ImportedPrivileges : Action;

    /// <summary>Import a share.</summary>
    public sealed record ImportShare : Action;

    /// <summary>Insert rows with optional column list.</summary>
    /// <param name="Columns">Optional list of target columns for insert.</param>
    public sealed record Insert(Sequence<Ident>? Columns) : Action;

    /// <summary>Manage operation with a specific manage type.</summary>
    /// <param name="ManageType">The specific manage sub-type.</param>
    public sealed record Manage(ActionManageType ManageType) : Action;

    /// <summary>Manage releases.</summary>
    public sealed record ManageReleases : Action;

    /// <summary>Manage versions.</summary>
    public sealed record ManageVersions : Action;

    /// <summary>Modify operation with an optional modify type.</summary>
    /// <param name="ModifyType">The optional modify sub-type.</param>
    public sealed record Modify(ActionModifyType? ModifyType) : Action;

    /// <summary>Monitor operation with an optional monitor type.</summary>
    /// <param name="MonitorType">The optional monitor sub-type.</param>
    public sealed record Monitor(ActionMonitorType? MonitorType) : Action;

    /// <summary>Operate permission.</summary>
    public sealed record Operate : Action;

    /// <summary>Override share restrictions.</summary>
    public sealed record OverrideShareRestrictions : Action;

    /// <summary>Ownership permission.</summary>
    public sealed record Ownership : Action;

    /// <summary>Purchase a data exchange listing.</summary>
    public sealed record PurchaseDataExchangeListing : Action;

    /// <summary>Read access.</summary>
    public sealed record Read : Action;

    /// <summary>Read session-level access.</summary>
    public sealed record ReadSession : Action;

    /// <summary>References with optional column list.</summary>
    /// <param name="Columns">Optional list of referenced column identifiers.</param>
    public sealed record References(Sequence<Ident>? Columns) : Action;

    /// <summary>Replication permission.</summary>
    public sealed record Replicate : Action;

    /// <summary>Resolve all references.</summary>
    public sealed record ResolveAll : Action;

    /// <summary>Role-related permission with target role name.</summary>
    /// <param name="RoleValue">
    /// The target role name.
    /// </param>
    // RoleValue: Rust: campo `role`; sufixo `Value` por CS0542 (convencoes.md §13, caso 41; nomes.md).
    public sealed record Role(ObjectName RoleValue) : Action;

    /// <summary>Select permission with optional column list.</summary>
    /// <param name="Columns">Optional list of selected columns.</param>
    public sealed record Select(Sequence<Ident>? Columns) : Action;

    /// <summary>Temporary object permission.</summary>
    public sealed record Temporary : Action;

    /// <summary>Trigger-related permission.</summary>
    public sealed record Trigger : Action;

    /// <summary>Truncate permission.</summary>
    public sealed record Truncate : Action;

    /// <summary>Update permission with optional affected columns.</summary>
    /// <param name="Columns">Optional list of columns affected by update.</param>
    public sealed record Update(Sequence<Ident>? Columns) : Action;

    /// <summary>Usage permission.</summary>
    public sealed record Usage : Action;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case AddSearchOptimization:
                f.WriteStr("ADD SEARCH OPTIMIZATION");
                break;
            case Apply apply:
                f.Write($"APPLY {apply.ApplyType}");
                break;
            case ApplyBudget:
                f.WriteStr("APPLYBUDGET");
                break;
            case AttachListing:
                f.WriteStr("ATTACH LISTING");
                break;
            case AttachPolicy:
                f.WriteStr("ATTACH POLICY");
                break;
            case Audit:
                f.WriteStr("AUDIT");
                break;
            case BindServiceEndpoint:
                f.WriteStr("BIND SERVICE ENDPOINT");
                break;
            case Connect:
                f.WriteStr("CONNECT");
                break;
            case Create create:
                f.WriteStr("CREATE");
                if (create.ObjType is { } createObjType)
                {
                    f.Write($" {createObjType}");
                }
                break;
            case DatabaseRole databaseRole:
                f.Write($"DATABASE ROLE {databaseRole.Role}");
                break;
            case Delete:
                f.WriteStr("DELETE");
                break;
            case Drop:
                f.WriteStr("DROP");
                break;
            case EvolveSchema:
                f.WriteStr("EVOLVE SCHEMA");
                break;
            case Exec exec:
                f.WriteStr("EXEC");
                if (exec.ObjType is { } execObjType)
                {
                    f.Write($" {execObjType}");
                }
                break;
            case Execute execute:
                f.WriteStr("EXECUTE");
                if (execute.ObjType is { } executeObjType)
                {
                    f.Write($" {executeObjType}");
                }
                break;
            case Failover:
                f.WriteStr("FAILOVER");
                break;
            case ImportedPrivileges:
                f.WriteStr("IMPORTED PRIVILEGES");
                break;
            case ImportShare:
                f.WriteStr("IMPORT SHARE");
                break;
            case Insert:
                f.WriteStr("INSERT");
                break;
            case Manage manage:
                f.Write($"MANAGE {manage.ManageType}");
                break;
            case ManageReleases:
                f.WriteStr("MANAGE RELEASES");
                break;
            case ManageVersions:
                f.WriteStr("MANAGE VERSIONS");
                break;
            case Modify modify:
                f.Write($"MODIFY");
                if (modify.ModifyType is { } modifyType)
                {
                    f.Write($" {modifyType}");
                }
                break;
            case Monitor monitor:
                f.Write($"MONITOR");
                if (monitor.MonitorType is { } monitorType)
                {
                    f.Write($" {monitorType}");
                }
                break;
            case Operate:
                f.WriteStr("OPERATE");
                break;
            case OverrideShareRestrictions:
                f.WriteStr("OVERRIDE SHARE RESTRICTIONS");
                break;
            case Ownership:
                f.WriteStr("OWNERSHIP");
                break;
            case PurchaseDataExchangeListing:
                f.WriteStr("PURCHASE DATA EXCHANGE LISTING");
                break;
            case Read:
                f.WriteStr("READ");
                break;
            case ReadSession:
                f.WriteStr("READ SESSION");
                break;
            case References:
                f.WriteStr("REFERENCES");
                break;
            case Replicate:
                f.WriteStr("REPLICATE");
                break;
            case ResolveAll:
                f.WriteStr("RESOLVE ALL");
                break;
            case Role role:
                f.Write($"ROLE {role.RoleValue}");
                break;
            case Select:
                f.WriteStr("SELECT");
                break;
            case Temporary:
                f.WriteStr("TEMPORARY");
                break;
            case Trigger:
                f.WriteStr("TRIGGER");
                break;
            case Truncate:
                f.WriteStr("TRUNCATE");
                break;
            case Update:
                f.WriteStr("UPDATE");
                break;
            case Usage:
                f.WriteStr("USAGE");
                break;
            default:
                throw new InvalidOperationException($"Variante de Action não tratada: {GetType().Name}");
        }

        // match self { Action::Insert { columns } | Action::References { columns }
        //     | Action::Select { columns } | Action::Update { columns } => { .. }, _ => () }
        var columns = this switch
        {
            Insert insert => insert.Columns,
            References references => references.Columns,
            Select select => select.Columns,
            Update update => update.Columns,
            _ => null,
        };
        if (columns is { } cols)
        {
            f.Write($" ({DisplayCommaSeparated(cols)})");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
