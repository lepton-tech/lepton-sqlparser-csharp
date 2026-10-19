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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Target of a <c>LOCK TABLE</c> command
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-lock.html"/></para>
/// </summary>
/// <param name="Name">Name of the table being locked.</param>
/// <param name="Only">Whether <c>ONLY</c> was specified to exclude descendant tables.</param>
/// <param name="HasAsterisk">Whether <c>*</c> was specified to explicitly include descendant tables.</param>
public sealed partial record LockTableTarget(
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
