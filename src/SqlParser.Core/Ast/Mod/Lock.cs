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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// A <c>LOCK</c> statement.
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-lock.html"/></para>
/// </summary>
/// <param name="Tables">List of tables to lock.</param>
/// <param name="LockMode">Lock mode.</param>
/// <param name="Nowait">Whether <c>NOWAIT</c> was specified.</param>
public sealed partial record Lock(Sequence<LockTableTarget> Tables, LockTableMode? LockMode, bool Nowait) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"LOCK TABLE {DisplayCommaSeparated(Tables)}");
        if (LockMode is { } lockMode)
        {
            f.Write($" IN {lockMode} MODE");
        }
        if (Nowait)
        {
            f.Write($" NOWAIT");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
