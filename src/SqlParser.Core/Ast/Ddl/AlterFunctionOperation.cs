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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>Operation for <c>ALTER FUNCTION</c> / <c>ALTER AGGREGATE</c>.</summary>
public abstract partial record AlterFunctionOperation : IDisplay
{
    /// <summary><c>RENAME TO new_name</c></summary>
    /// <param name="NewName">New unqualified function or aggregate name.</param>
    public sealed record RenameTo(Ident NewName) : AlterFunctionOperation;

    /// <summary><c>OWNER TO { new_owner | CURRENT_ROLE | CURRENT_USER | SESSION_USER }</c></summary>
    public sealed record OwnerTo(Owner Value) : AlterFunctionOperation;

    /// <summary><c>SET SCHEMA schema_name</c></summary>
    /// <param name="SchemaName">The target schema name.</param>
    public sealed record SetSchema(ObjectName SchemaName) : AlterFunctionOperation;

    /// <summary><c>[ NO ] DEPENDS ON EXTENSION extension_name</c></summary>
    /// <param name="No"><c>true</c> when <c>NO DEPENDS ON EXTENSION</c>.</param>
    /// <param name="ExtensionName">Extension name.</param>
    public sealed record DependsOnExtension(bool No, ObjectName ExtensionName) : AlterFunctionOperation;

    /// <summary><c>action [ ... ] [ RESTRICT ]</c> (function only).</summary>
    /// <param name="ActionsValue">
    /// One or more function actions.
    /// <para>Rust: campo <c>actions</c>. Sufixo <c>Value</c> por CS0542 (convencoes.md §13, caso 41).</para>
    /// </param>
    /// <param name="Restrict">Whether <c>RESTRICT</c> is present.</param>
    public sealed record Actions(Sequence<AlterFunctionAction> ActionsValue, bool Restrict) : AlterFunctionOperation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case RenameTo { NewName: var newName }:
                f.Write($"RENAME TO {newName}");
                break;
            case OwnerTo { Value: var owner }:
                f.Write($"OWNER TO {owner}");
                break;
            case SetSchema { SchemaName: var schemaName }:
                f.Write($"SET SCHEMA {schemaName}");
                break;
            case DependsOnExtension { No: var no, ExtensionName: var extensionName }:
                if (no)
                {
                    f.Write($"NO DEPENDS ON EXTENSION {extensionName}");
                }
                else
                {
                    f.Write($"DEPENDS ON EXTENSION {extensionName}");
                }
                break;
            case Actions { ActionsValue: var actions, Restrict: var restrict }:
                f.Write($"{DisplaySeparated(actions, " ")}");
                if (restrict)
                {
                    f.Write($" RESTRICT");
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterFunctionOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
