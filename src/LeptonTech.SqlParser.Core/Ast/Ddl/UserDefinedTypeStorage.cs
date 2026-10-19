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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Storage specification for PostgreSQL user-defined base types.
/// <para>Specifies the storage strategy for values of the data type:</para>
/// <list type="bullet">
/// <item><c>plain</c>: Prevents compression and out-of-line storage (for fixed-length types)</item>
/// <item><c>external</c>: Allows out-of-line storage but not compression</item>
/// <item><c>extended</c>: Allows both compression and out-of-line storage (default for most types)</item>
/// <item><c>main</c>: Allows compression but discourages out-of-line storage</item>
/// </list>
/// <para>PostgreSQL Documentation</para>
/// <para>See: <see href="https://www.postgresql.org/docs/current/sql-createtype.html"/></para>
/// <para>Examples</para>
/// <code>
/// CREATE TYPE mytype (
///     INPUT = in_func,
///     OUTPUT = out_func,
///     STORAGE = plain
/// );
/// </code>
/// </summary>
// Enum sem dados com Display: records (convencoes.md §13, caso 37).
public abstract partial record UserDefinedTypeStorage : IDisplay
{
    /// <summary>No compression or out-of-line storage: <c>STORAGE = plain</c></summary>
    public sealed record Plain : UserDefinedTypeStorage;

    /// <summary>Out-of-line storage allowed, no compression: <c>STORAGE = external</c></summary>
    public sealed record External : UserDefinedTypeStorage;

    /// <summary>Both compression and out-of-line storage allowed: <c>STORAGE = extended</c></summary>
    public sealed record Extended : UserDefinedTypeStorage;

    /// <summary>Compression allowed, out-of-line discouraged: <c>STORAGE = main</c></summary>
    public sealed record Main : UserDefinedTypeStorage;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Plain:
                f.Write($"plain");
                break;
            case External:
                f.Write($"external");
                break;
            case Extended:
                f.Write($"extended");
                break;
            case Main:
                f.Write($"main");
                break;
            default:
                throw new InvalidOperationException($"Variante de UserDefinedTypeStorage não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
