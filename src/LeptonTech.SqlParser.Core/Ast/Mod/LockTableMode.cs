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
/// PostgreSQL lock modes for <c>LOCK TABLE</c>.
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-lock.html"/></para>
/// </summary>
public abstract partial record LockTableMode : IDisplay
{
    /// <summary><c>ACCESS SHARE</c></summary>
    public sealed record AccessShare : LockTableMode;

    /// <summary><c>ROW SHARE</c></summary>
    public sealed record RowShare : LockTableMode;

    /// <summary><c>ROW EXCLUSIVE</c></summary>
    public sealed record RowExclusive : LockTableMode;

    /// <summary><c>SHARE UPDATE EXCLUSIVE</c></summary>
    public sealed record ShareUpdateExclusive : LockTableMode;

    /// <summary><c>SHARE</c></summary>
    public sealed record Share : LockTableMode;

    /// <summary><c>SHARE ROW EXCLUSIVE</c></summary>
    public sealed record ShareRowExclusive : LockTableMode;

    /// <summary><c>EXCLUSIVE</c></summary>
    public sealed record Exclusive : LockTableMode;

    /// <summary><c>ACCESS EXCLUSIVE</c></summary>
    public sealed record AccessExclusive : LockTableMode;

    public void Fmt(Formatter f)
    {
        var text = this switch
        {
            AccessShare => "ACCESS SHARE",
            RowShare => "ROW SHARE",
            RowExclusive => "ROW EXCLUSIVE",
            ShareUpdateExclusive => "SHARE UPDATE EXCLUSIVE",
            Share => "SHARE",
            ShareRowExclusive => "SHARE ROW EXCLUSIVE",
            Exclusive => "EXCLUSIVE",
            AccessExclusive => "ACCESS EXCLUSIVE",
            _ => throw new InvalidOperationException($"Variante de LockTableMode não tratada: {GetType().Name}"),
        };
        f.Write($"{text}");
    }

    public sealed override string ToString() => Formatter.Format(this);
}
