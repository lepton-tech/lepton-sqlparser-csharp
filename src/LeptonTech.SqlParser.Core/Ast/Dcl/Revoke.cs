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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// REVOKE privileges ON objects FROM grantees
/// See <see href="https://www.postgresql.org/docs/current/sql-revoke.html">PostgreSQL</see>
/// </summary>
/// <param name="GrantOptionFor">
/// Whether <c>GRANT OPTION FOR</c> is present, withdrawing the right to grant
/// the privileges rather than the privileges themselves.
/// </param>
/// <param name="Privileges">Privileges to revoke.</param>
/// <param name="Objects">Optional objects from which to revoke.</param>
/// <param name="Grantees">Grantees affected by the revoke.</param>
/// <param name="GrantedBy">
/// Optional <c>GRANTED BY</c> identifier.
/// <para><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dcl-statements">BigQuery</see></para>
/// </param>
/// <param name="Cascade">Optional <c>CASCADE</c>/<c>RESTRICT</c> behavior.</param>
// `impl From<Revoke> for Statement` → `Statement.From(Revoke)` (`Dcl/Statement.Dcl.cs`).
public sealed partial record Revoke(
    bool GrantOptionFor,
    Privileges Privileges,
    GrantObjects? Objects,
    Sequence<Grantee> Grantees,
    Ident? GrantedBy,
    CascadeOption? Cascade) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"REVOKE ");
        if (GrantOptionFor)
        {
            f.Write($"GRANT OPTION FOR ");
        }
        f.Write($"{Privileges}");
        if (Objects is { } objects)
        {
            f.Write($" ON {objects}");
        }
        f.Write($" FROM {DisplayCommaSeparated(Grantees)}");
        if (GrantedBy is { } grantedBy)
        {
            f.Write($" GRANTED BY {grantedBy}");
        }
        if (Cascade is { } cascade)
        {
            f.Write($" {cascade}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
