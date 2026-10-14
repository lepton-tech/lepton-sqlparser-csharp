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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dcl.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>A <c>USE</c> (<c>Statement::Use</c>) operation</summary>
public abstract partial record Use : IDisplay
{
    /// <summary>Switch to the given catalog (e.g. <c>USE CATALOG ...</c>).</summary>
    public sealed record Catalog(ObjectName Value) : Use;

    /// <summary>Switch to the given schema (e.g. <c>USE SCHEMA ...</c>).</summary>
    public sealed record Schema(ObjectName Value) : Use;

    /// <summary>Switch to the given database (e.g. <c>USE DATABASE ...</c>).</summary>
    public sealed record Database(ObjectName Value) : Use;

    /// <summary>Switch to the given warehouse (e.g. <c>USE WAREHOUSE ...</c>).</summary>
    public sealed record Warehouse(ObjectName Value) : Use;

    /// <summary>Switch to the given role (e.g. <c>USE ROLE ...</c>).</summary>
    public sealed record Role(ObjectName Value) : Use;

    /// <summary>Use secondary roles specification (e.g. <c>USE SECONDARY ROLES ...</c>).</summary>
    public sealed record SecondaryRoles(Ast.SecondaryRoles Value) : Use;

    /// <summary>Use the specified object (e.g. <c>USE foo.bar</c>).</summary>
    public sealed record Object(ObjectName Value) : Use;

    /// <summary>Reset to default (e.g. <c>USE DEFAULT</c>).</summary>
    public sealed record Default : Use;

    public void Fmt(Formatter f)
    {
        f.WriteStr("USE ");
        switch (this)
        {
            case Catalog name:
                f.Write($"CATALOG {name.Value}");
                break;
            case Schema name:
                f.Write($"SCHEMA {name.Value}");
                break;
            case Database name:
                f.Write($"DATABASE {name.Value}");
                break;
            case Warehouse name:
                f.Write($"WAREHOUSE {name.Value}");
                break;
            case Role name:
                f.Write($"ROLE {name.Value}");
                break;
            case SecondaryRoles secondaryRoles:
                f.Write($"SECONDARY ROLES {secondaryRoles.Value}");
                break;
            case Object name:
                f.Write($"{name.Value}");
                break;
            case Default:
                f.Write($"DEFAULT");
                break;
            default:
                throw new InvalidOperationException($"Variante de Use não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
