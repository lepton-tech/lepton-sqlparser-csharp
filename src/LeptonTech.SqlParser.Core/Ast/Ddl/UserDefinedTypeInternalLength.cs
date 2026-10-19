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
/// Internal length specification for PostgreSQL user-defined base types.
/// <para>
/// Specifies the internal length in bytes of the new type's internal representation.
/// The default assumption is that it is variable-length.
/// </para>
/// <para>PostgreSQL Documentation</para>
/// <para>See: <see href="https://www.postgresql.org/docs/current/sql-createtype.html"/></para>
/// <para>Examples</para>
/// <code>
/// CREATE TYPE mytype (
///     INPUT = in_func,
///     OUTPUT = out_func,
///     INTERNALLENGTH = 16  -- Fixed 16-byte length
/// );
///
/// CREATE TYPE mytype2 (
///     INPUT = in_func,
///     OUTPUT = out_func,
///     INTERNALLENGTH = VARIABLE  -- Variable length
/// );
/// </code>
/// </summary>
public abstract partial record UserDefinedTypeInternalLength : IDisplay
{
    /// <summary>Fixed internal length: <c>INTERNALLENGTH = &lt;number&gt;</c></summary>
    public sealed record Fixed(ulong Value) : UserDefinedTypeInternalLength;

    /// <summary>Variable internal length: <c>INTERNALLENGTH = VARIABLE</c></summary>
    public sealed record Variable : UserDefinedTypeInternalLength;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Fixed { Value: var n }:
                f.Write($"{n}");
                break;
            case Variable:
                f.Write($"VARIABLE");
                break;
            default:
                throw new InvalidOperationException($"Variante de UserDefinedTypeInternalLength não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
