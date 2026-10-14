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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// PostgreSQL partition bound specification for <c>PARTITION OF</c>.
/// <para>Specifies partition bounds for a child partition table.</para>
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-createtable.html">PostgreSQL</see></para>
/// </summary>
public abstract partial record ForValues : IDisplay
{
    /// <summary><c>FOR VALUES IN (expr, ...)</c></summary>
    public sealed record In(Sequence<Expr> Value) : ForValues;

    /// <summary><c>FOR VALUES FROM (expr|MINVALUE|MAXVALUE, ...) TO (expr|MINVALUE|MAXVALUE, ...)</c></summary>
    /// <param name="FromValue">
    /// The lower bound values for the partition.
    /// <para>Rust: campo <c>from</c>. Sufixo <c>Value</c> por CS0542 (convencoes.md §13, caso 41).</para>
    /// </param>
    /// <param name="To">The upper bound values for the partition.</param>
    public sealed record From(Sequence<PartitionBoundValue> FromValue, Sequence<PartitionBoundValue> To) : ForValues;

    /// <summary><c>FOR VALUES WITH (MODULUS n, REMAINDER r)</c></summary>
    /// <param name="Modulus">The modulus value for hash partitioning.</param>
    /// <param name="Remainder">The remainder value for hash partitioning.</param>
    public sealed record With(ulong Modulus, ulong Remainder) : ForValues;

    /// <summary><c>DEFAULT</c></summary>
    public sealed record Default : ForValues;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case In { Value: var values }:
                f.Write($"FOR VALUES IN ({DisplayCommaSeparated(values)})");
                break;
            case From { FromValue: var from, To: var to }:
                f.Write($"FOR VALUES FROM ({DisplayCommaSeparated(from)}) TO ({DisplayCommaSeparated(to)})");
                break;
            case With { Modulus: var modulus, Remainder: var remainder }:
                f.Write($"FOR VALUES WITH (MODULUS {modulus}, REMAINDER {remainder})");
                break;
            case Default:
                f.Write($"DEFAULT");
                break;
            default:
                throw new InvalidOperationException($"Variante de ForValues não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
