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
/// A <c>DENY</c> statement
/// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/deny-transact-sql">MsSql</see></para>
/// </summary>
/// <param name="Privileges">The privileges to deny.</param>
/// <param name="Objects">The objects the privileges apply to.</param>
/// <param name="Grantees">The grantees (users/roles) to whom the denial applies.</param>
/// <param name="GrantedBy">Optional identifier of the principal that performed the grant.</param>
/// <param name="Cascade">Optional cascade option controlling dependent objects.</param>
public sealed partial record DenyStatement(
    Privileges Privileges,
    GrantObjects Objects,
    Sequence<Grantee> Grantees,
    Ident? GrantedBy,
    CascadeOption? Cascade) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"DENY {Privileges}");
        f.Write($" ON {Objects}");
        if (!Grantees.IsEmpty)
        {
            f.Write($" TO {DisplayCommaSeparated(Grantees)}");
        }
        if (Cascade is { } cascade)
        {
            f.Write($" {cascade}");
        }
        if (GrantedBy is { } grantedBy)
        {
            f.Write($" AS {grantedBy}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
