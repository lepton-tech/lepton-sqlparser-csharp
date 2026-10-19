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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// A <c>CREATE SERVER</c> statement.
/// <para><see href="https://www.postgresql.org/docs/current/sql-createserver.html">PostgreSQL Documentation</see></para>
/// </summary>
/// <param name="Name">The server name.</param>
/// <param name="IfNotExists">Whether <c>IF NOT EXISTS</c> was specified.</param>
/// <param name="ServerType">Optional server type identifier.</param>
/// <param name="Version">Optional server version identifier.</param>
/// <param name="ForeignDataWrapper">Foreign-data wrapper object name.</param>
/// <param name="Options">Optional list of server options.</param>
public sealed partial record CreateServerStatement(
    ObjectName Name,
    bool IfNotExists,
    Ident? ServerType,
    Ident? Version,
    ObjectName ForeignDataWrapper,
    Sequence<CreateServerOption>? Options) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CREATE SERVER {(IfNotExists ? "IF NOT EXISTS " : "")}{Name} ");

        if (ServerType is { } st)
        {
            f.Write($"TYPE {st} ");
        }

        if (Version is { } v)
        {
            f.Write($"VERSION {v} ");
        }

        f.Write($"FOREIGN DATA WRAPPER {ForeignDataWrapper}");

        if (Options is { } o)
        {
            f.Write($" OPTIONS ({DisplayCommaSeparated(o)})");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
