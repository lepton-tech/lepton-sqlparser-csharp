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

namespace SqlParser.Core.Ast;

/// <summary>Types of database objects referenced by DDL statements.</summary>
public abstract record ObjectType : IDisplay
{
    /// <summary>A collation.</summary>
    public sealed record Collation : ObjectType;

    /// <summary>A table.</summary>
    public sealed record Table : ObjectType;

    /// <summary>A view.</summary>
    public sealed record View : ObjectType;

    /// <summary>A materialized view.</summary>
    public sealed record MaterializedView : ObjectType;

    /// <summary>An index.</summary>
    public sealed record Index : ObjectType;

    /// <summary>A schema.</summary>
    public sealed record Schema : ObjectType;

    /// <summary>A database.</summary>
    public sealed record Database : ObjectType;

    /// <summary>A role.</summary>
    public sealed record Role : ObjectType;

    /// <summary>A sequence.</summary>
    public sealed record Sequence : ObjectType;

    /// <summary>A stage.</summary>
    public sealed record Stage : ObjectType;

    /// <summary>A type definition.</summary>
    public sealed record Type : ObjectType;

    /// <summary>A user.</summary>
    public sealed record User : ObjectType;

    /// <summary>A stream.</summary>
    public sealed record Stream : ObjectType;

    /// <summary>A warehouse.</summary>
    public sealed record Warehouse : ObjectType;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            Collation => "COLLATION",
            Table => "TABLE",
            View => "VIEW",
            MaterializedView => "MATERIALIZED VIEW",
            Index => "INDEX",
            Schema => "SCHEMA",
            Database => "DATABASE",
            Role => "ROLE",
            Sequence => "SEQUENCE",
            Stage => "STAGE",
            Type => "TYPE",
            User => "USER",
            Stream => "STREAM",
            Warehouse => "WAREHOUSE",
            _ => throw new InvalidOperationException($"Variante de ObjectType não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
