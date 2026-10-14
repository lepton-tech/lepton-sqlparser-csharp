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
/// Sqlite specific syntax
/// <para>
/// See <see href="https://sqlite.org/lang_conflict.html">Sqlite documentation</see>
/// for more details.
/// </para>
/// </summary>
public abstract record SqliteOnConflict : IDisplay
{
    /// <summary>Use ROLLBACK on conflict.</summary>
    public sealed record Rollback : SqliteOnConflict;

    /// <summary>Use ABORT on conflict.</summary>
    public sealed record Abort : SqliteOnConflict;

    /// <summary>Use FAIL on conflict.</summary>
    public sealed record Fail : SqliteOnConflict;

    /// <summary>Use IGNORE on conflict.</summary>
    public sealed record Ignore : SqliteOnConflict;

    /// <summary>Use REPLACE on conflict.</summary>
    public sealed record Replace : SqliteOnConflict;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Rollback:
                f.Write($"OR ROLLBACK");
                break;
            case Abort:
                f.Write($"OR ABORT");
                break;
            case Fail:
                f.Write($"OR FAIL");
                break;
            case Ignore:
                f.Write($"OR IGNORE");
                break;
            case Replace:
                f.Write($"OR REPLACE");
                break;
            default:
                throw new InvalidOperationException($"Variante de SqliteOnConflict não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
