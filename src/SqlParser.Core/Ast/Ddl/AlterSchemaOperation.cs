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

/// <summary>
/// An <c>ALTER SCHEMA</c> (<c>Statement::AlterSchema</c>) operation.
/// <para>See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#alter_schema_collate_statement">BigQuery</see></para>
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-alterschema.html">PostgreSQL</see></para>
/// </summary>
public abstract partial record AlterSchemaOperation : IDisplay
{
    /// <summary>Set the default collation for the schema.</summary>
    /// <param name="Collate">The collation to set as default.</param>
    public sealed record SetDefaultCollate(Expr Collate) : AlterSchemaOperation;

    /// <summary>Add a replica to the schema.</summary>
    /// <param name="Replica">The replica to add.</param>
    /// <param name="Options">Optional options for the replica.</param>
    public sealed record AddReplica(Ident Replica, Sequence<SqlOption>? Options) : AlterSchemaOperation;

    /// <summary>Drop a replica from the schema.</summary>
    /// <param name="Replica">The replica to drop.</param>
    public sealed record DropReplica(Ident Replica) : AlterSchemaOperation;

    /// <summary>Set options for the schema.</summary>
    /// <param name="Options">The options to set.</param>
    public sealed record SetOptionsParens(Sequence<SqlOption> Options) : AlterSchemaOperation;

    /// <summary>Rename the schema.</summary>
    /// <param name="Name">The new name for the schema.</param>
    public sealed record Rename(ObjectName Name) : AlterSchemaOperation;

    /// <summary>Change the owner of the schema.</summary>
    /// <param name="Owner">The new owner of the schema.</param>
    public sealed record OwnerTo(Owner Owner) : AlterSchemaOperation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case SetDefaultCollate { Collate: var collate }:
                f.Write($"SET DEFAULT COLLATE {collate}");
                break;
            case AddReplica { Replica: var replica, Options: var options }:
                f.Write($"ADD REPLICA {replica}");
                if (options is { } optionsValue)
                {
                    f.Write($" OPTIONS ({DisplayCommaSeparated(optionsValue)})");
                }
                break;
            case DropReplica { Replica: var replica }:
                f.Write($"DROP REPLICA {replica}");
                break;
            case SetOptionsParens { Options: var options }:
                f.Write($"SET OPTIONS ({DisplayCommaSeparated(options)})");
                break;
            case Rename { Name: var name }:
                f.Write($"RENAME TO {name}");
                break;
            case OwnerTo { Owner: var owner }:
                f.Write($"OWNER TO {owner}");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterSchemaOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
