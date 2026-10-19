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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// Alignment specification for PostgreSQL user-defined base types.
/// <para>
/// Specifies the storage alignment requirement for values of the data type.
/// The allowed values equate to alignment on 1, 2, 4, or 8 byte boundaries.
/// Note that variable-length types must have an alignment of at least 4, since
/// they necessarily contain an int4 as their first component.
/// </para>
/// <para>PostgreSQL Documentation</para>
/// <para>See: <see href="https://www.postgresql.org/docs/current/sql-createtype.html"/></para>
/// <para>Examples</para>
/// <code>
/// CREATE TYPE mytype (
///     INPUT = in_func,
///     OUTPUT = out_func,
///     ALIGNMENT = int4  -- 4-byte alignment
/// );
/// </code>
/// </summary>
// Enum sem dados com Display: records (convencoes.md §13, caso 37).
public abstract partial record Alignment : IDisplay
{
    /// <summary>Single-byte alignment: <c>ALIGNMENT = char</c></summary>
    public sealed record Char : Alignment;

    /// <summary>2-byte alignment: <c>ALIGNMENT = int2</c></summary>
    public sealed record Int2 : Alignment;

    /// <summary>4-byte alignment: <c>ALIGNMENT = int4</c></summary>
    public sealed record Int4 : Alignment;

    /// <summary>8-byte alignment: <c>ALIGNMENT = double</c></summary>
    public sealed record Double : Alignment;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Char:
                f.Write($"char");
                break;
            case Int2:
                f.Write($"int2");
                break;
            case Int4:
                f.Write($"int4");
                break;
            case Double:
                f.Write($"double");
                break;
            default:
                throw new InvalidOperationException($"Variante de Alignment não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
