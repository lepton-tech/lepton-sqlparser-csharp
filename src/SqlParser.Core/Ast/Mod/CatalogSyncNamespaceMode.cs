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

/// <summary>
/// Snowflake CatalogSyncNamespaceMode
/// <code>
/// [ CATALOG_SYNC_NAMESPACE_MODE = { NEST | FLATTEN } ]
/// </code>
/// </summary>
/// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/create-database"/></remarks>
public abstract record CatalogSyncNamespaceMode : IDisplay
{
    /// <summary>Nest namespaces when syncing catalog.</summary>
    public sealed record Nest : CatalogSyncNamespaceMode;

    /// <summary>Flatten namespaces when syncing catalog.</summary>
    public sealed record Flatten : CatalogSyncNamespaceMode;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Nest:
                f.Write($"NEST");
                break;
            case Flatten:
                f.Write($"FLATTEN");
                break;
            default:
                throw new InvalidOperationException($"Variante de CatalogSyncNamespaceMode não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
