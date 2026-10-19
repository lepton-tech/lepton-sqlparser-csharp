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
/// CREATE POLICY statement.
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-createpolicy.html">PostgreSQL</see></para>
/// </summary>
/// <param name="Name">Name of the policy.</param>
/// <param name="TableName">Table the policy is defined on.</param>
/// <param name="PolicyType">Optional policy type (e.g., <c>PERMISSIVE</c> / <c>RESTRICTIVE</c>).</param>
/// <param name="Command">Optional command the policy applies to (e.g., <c>SELECT</c>).</param>
/// <param name="To">Optional list of grantee owners.</param>
/// <param name="Using">Optional expression for the <c>USING</c> clause.</param>
/// <param name="WithCheck">Optional expression for the <c>WITH CHECK</c> clause.</param>
public sealed partial record CreatePolicy(
    Ident Name,
    [property: VisitWith("visit_relation")] ObjectName TableName,
    CreatePolicyType? PolicyType,
    CreatePolicyCommand? Command,
    Sequence<Owner>? To,
    Expr? Using,
    Expr? WithCheck) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CREATE POLICY {Name} ON {TableName}");
        if (PolicyType is { } policyType)
        {
            f.Write($" AS {policyType}");
        }
        if (Command is { } command)
        {
            f.Write($" FOR {command}");
        }
        if (To is { } to)
        {
            f.Write($" TO {DisplayCommaSeparated(to)}");
        }
        if (Using is { } @using)
        {
            f.Write($" USING ({@using})");
        }
        if (WithCheck is { } withCheck)
        {
            f.Write($" WITH CHECK ({withCheck})");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // `impl From<CreatePolicy> for crate::ast::Statement` (ddl.rs:5950) → `Statement.From(CreatePolicy)`,
    // em Statement.From.Ddl.cs (convencoes.md §13, caso 7).
}
