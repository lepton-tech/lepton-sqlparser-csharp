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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>An <see cref="AlterOperatorFamily"/> operation</summary>
public abstract partial record AlterOperatorFamilyOperation : IDisplay
{
    /// <summary><c>ADD { OPERATOR ... | FUNCTION ... } [, ...]</c></summary>
    /// <param name="Items">List of operator family items to add</param>
    public sealed record Add(Sequence<OperatorFamilyItem> Items) : AlterOperatorFamilyOperation;

    /// <summary><c>DROP { OPERATOR ... | FUNCTION ... } [, ...]</c></summary>
    /// <param name="Items">List of operator family items to drop</param>
    public sealed record Drop(Sequence<OperatorFamilyDropItem> Items) : AlterOperatorFamilyOperation;

    /// <summary><c>RENAME TO new_name</c></summary>
    /// <param name="NewName">The new name for the operator family.</param>
    public sealed record RenameTo(ObjectName NewName) : AlterOperatorFamilyOperation;

    /// <summary><c>OWNER TO { new_owner | CURRENT_ROLE | CURRENT_USER | SESSION_USER }</c></summary>
    public sealed record OwnerTo(Owner Value) : AlterOperatorFamilyOperation;

    /// <summary><c>SET SCHEMA new_schema</c></summary>
    /// <param name="SchemaName">The target schema name.</param>
    public sealed record SetSchema(ObjectName SchemaName) : AlterOperatorFamilyOperation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Add { Items: var items }:
                f.Write($"ADD {DisplayCommaSeparated(items)}");
                break;
            case Drop { Items: var items }:
                f.Write($"DROP {DisplayCommaSeparated(items)}");
                break;
            case RenameTo { NewName: var newName }:
                f.Write($"RENAME TO {newName}");
                break;
            case OwnerTo { Value: var owner }:
                f.Write($"OWNER TO {owner}");
                break;
            case SetSchema { SchemaName: var schemaName }:
                f.Write($"SET SCHEMA {schemaName}");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterOperatorFamilyOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
