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

namespace SqlParser.Core.Ast;

/// <summary>Operations supported by <c>ALTER COLLATION</c>.</summary>
public abstract partial record AlterCollationOperation : IDisplay
{
    /// <summary>
    /// Rename the collation.
    /// <code>
    /// ALTER COLLATION name RENAME TO new_name
    /// </code>
    /// </summary>
    /// <param name="NewName">New collation name.</param>
    public sealed record RenameTo(Ident NewName) : AlterCollationOperation;

    /// <summary>
    /// Change the collation owner.
    /// <code>
    /// ALTER COLLATION name OWNER TO role_name
    /// </code>
    /// </summary>
    public sealed record OwnerTo(Owner Value) : AlterCollationOperation;

    /// <summary>
    /// Move the collation to another schema.
    /// <code>
    /// ALTER COLLATION name SET SCHEMA new_schema
    /// </code>
    /// </summary>
    /// <param name="SchemaName">Target schema name.</param>
    public sealed record SetSchema(ObjectName SchemaName) : AlterCollationOperation;

    /// <summary>
    /// Refresh collation version metadata.
    /// <code>
    /// ALTER COLLATION name REFRESH VERSION
    /// </code>
    /// </summary>
    public sealed record RefreshVersion : AlterCollationOperation;

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
            case RefreshVersion:
                f.Write($"REFRESH VERSION");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterCollationOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
