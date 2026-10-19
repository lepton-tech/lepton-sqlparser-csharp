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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Operation for <c>ALTER TEXT SEARCH ...</c>.
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-altertsdictionary.html">PostgreSQL</see>.</para>
/// </summary>
public abstract partial record AlterTextSearchOperation : IDisplay
{
    /// <summary><c>RENAME TO new_name</c></summary>
    /// <param name="NewName">New name.</param>
    public sealed record RenameTo(Ident NewName) : AlterTextSearchOperation;

    /// <summary><c>OWNER TO ...</c></summary>
    public sealed record OwnerTo(Owner Value) : AlterTextSearchOperation;

    /// <summary><c>SET SCHEMA schema_name</c></summary>
    /// <param name="SchemaName">Target schema.</param>
    public sealed record SetSchema(ObjectName SchemaName) : AlterTextSearchOperation;

    /// <summary><c>( option [= value] [, ...] )</c></summary>
    /// <param name="Options">Text search options to apply.</param>
    public sealed record SetOptions(Sequence<AlterTextSearchOption> Options) : AlterTextSearchOperation;

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
            case SetOptions { Options: var options }:
                f.Write($"({DisplayCommaSeparated(options)})");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterTextSearchOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
