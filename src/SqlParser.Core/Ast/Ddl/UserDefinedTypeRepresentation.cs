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

/// <summary>SQL user defined type definition</summary>
public abstract partial record UserDefinedTypeRepresentation : IDisplay
{
    /// <summary>Composite type: <c>CREATE TYPE name AS (attributes)</c></summary>
    /// <param name="Attributes">List of attributes for the composite type.</param>
    public sealed record Composite(Sequence<UserDefinedTypeCompositeAttributeDef> Attributes) : UserDefinedTypeRepresentation;

    /// <summary>
    /// Enum type: <c>CREATE TYPE name AS ENUM (labels)</c>
    /// <para>Note: this is PostgreSQL-specific. See <see href="https://www.postgresql.org/docs/current/sql-createtype.html"/></para>
    /// Enum type: <c>CREATE TYPE name AS ENUM (labels)</c>
    /// </summary>
    /// <param name="Labels">Labels that make up the enum type.</param>
    public sealed record Enum(Sequence<Ident> Labels) : UserDefinedTypeRepresentation;

    /// <summary>
    /// Range type: <c>CREATE TYPE name AS RANGE (options)</c>
    /// <para>Note: this is PostgreSQL-specific. See <see href="https://www.postgresql.org/docs/current/sql-createtype.html"/></para>
    /// </summary>
    /// <param name="Options">Options for the range type definition.</param>
    public sealed record Range(Sequence<UserDefinedTypeRangeOption> Options) : UserDefinedTypeRepresentation;

    /// <summary>
    /// Base type (SQL definition): <c>CREATE TYPE name (options)</c>
    /// <para>Note the lack of <c>AS</c> keyword</para>
    /// <para>Note: this is PostgreSQL-specific. See <see href="https://www.postgresql.org/docs/current/sql-createtype.html"/></para>
    /// </summary>
    /// <param name="Options">Options for SQL definition of the user-defined type.</param>
    public sealed record SqlDefinition(Sequence<UserDefinedTypeSqlDefinitionOption> Options) : UserDefinedTypeRepresentation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Composite { Attributes: var attributes }:
                f.Write($"AS ({DisplayCommaSeparated(attributes)})");
                break;
            case Enum { Labels: var labels }:
                f.Write($"AS ENUM ({DisplayCommaSeparated(labels)})");
                break;
            case Range { Options: var options }:
                f.Write($"AS RANGE ({DisplayCommaSeparated(options)})");
                break;
            case SqlDefinition { Options: var options }:
                f.Write($"({DisplayCommaSeparated(options)})");
                break;
            default:
                throw new InvalidOperationException($"Variante de UserDefinedTypeRepresentation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
