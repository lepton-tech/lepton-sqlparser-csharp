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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// Target of a <c>TRUNCATE TABLE</c> command
/// <para>
/// Note this is its own struct because <c>visit_relation</c> requires an <c>ObjectName</c> (not a <c>Vec&lt;ObjectName&gt;</c>)
/// </para>
/// </summary>
/// <param name="Name">name of the table being truncated</param>
/// <param name="Only">
/// Postgres-specific option: explicitly exclude descendants (also default without ONLY)
/// <code>
/// TRUNCATE TABLE ONLY name
/// </code>
/// <see href="https://www.postgresql.org/docs/current/sql-truncate.html"/>
/// </param>
/// <param name="HasAsterisk">
/// Postgres-specific option: asterisk after table name to explicitly indicate descendants
/// <code>
/// TRUNCATE TABLE name [ * ]
/// </code>
/// <see href="https://www.postgresql.org/docs/current/sql-truncate.html"/>
/// </param>
public sealed partial record TruncateTableTarget(
    [property: VisitWith("visit_relation")] ObjectName Name,
    bool Only,
    bool HasAsterisk) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (Only)
        {
            f.Write($"ONLY ");
        }
        f.Write($"{Name}");
        if (HasAsterisk)
        {
            f.Write($" *");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
