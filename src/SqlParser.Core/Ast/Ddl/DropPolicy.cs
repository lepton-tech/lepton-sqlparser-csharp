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

namespace SqlParser.Core.Ast;

/// <summary>
/// DROP POLICY statement.
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-droppolicy.html">PostgreSQL</see></para>
/// </summary>
/// <param name="IfExists"><c>true</c> when <c>IF EXISTS</c> was present.</param>
/// <param name="Name">Name of the policy to drop.</param>
/// <param name="TableName">Name of the table the policy applies to.</param>
/// <param name="DropBehavior">Optional drop behavior (<c>CASCADE</c> or <c>RESTRICT</c>).</param>
public sealed partial record DropPolicy(
    bool IfExists,
    Ident Name,
    [property: VisitWith("visit_relation")] ObjectName TableName,
    DropBehavior? DropBehavior) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var ifExists = IfExists ? "IF EXISTS " : "";
        f.Write($"DROP POLICY {ifExists}{Name} ON {TableName}");
        if (DropBehavior is { } behavior)
        {
            f.Write($" {behavior}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // `impl From<DropPolicy> for crate::ast::Statement` (ddl.rs:5956) → `Statement.From(DropPolicy)`,
    // em Statement.From.Ddl.cs (convencoes.md §13, caso 7).
}
