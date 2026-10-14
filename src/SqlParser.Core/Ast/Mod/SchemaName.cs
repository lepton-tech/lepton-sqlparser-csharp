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

/// <summary>Schema possible naming variants ([1]).</summary>
/// <remarks>[1]: <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#schema-definition"/></remarks>
public abstract record SchemaName : IDisplay
{
    /// <summary>Only schema name specified: <c>&lt;schema name&gt;</c>.</summary>
    public sealed record Simple(ObjectName Value) : SchemaName;

    /// <summary>Only authorization identifier specified: <c>AUTHORIZATION &lt;schema authorization identifier&gt;</c>.</summary>
    public sealed record UnnamedAuthorization(Ident Value) : SchemaName;

    /// <summary>Both schema name and authorization identifier specified: <c>&lt;schema name&gt;  AUTHORIZATION &lt;schema authorization identifier&gt;</c>.</summary>
    /// <param name="Name">Rust: campo <c>.0</c> (o nome do schema). Nome registrado em nomes.md.</param>
    /// <param name="Authorization">Rust: campo <c>.1</c> (o identificador de autorização). Nome registrado em nomes.md.</param>
    public sealed record NamedAuthorization(ObjectName Name, Ident Authorization) : SchemaName;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Simple name:
                f.Write($"{name.Value}");
                break;
            case UnnamedAuthorization authorization:
                f.Write($"AUTHORIZATION {authorization.Value}");
                break;
            case NamedAuthorization named:
                f.Write($"{named.Name} AUTHORIZATION {named.Authorization}");
                break;
            default:
                throw new InvalidOperationException($"Variante de SchemaName não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
