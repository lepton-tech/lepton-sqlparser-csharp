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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/data_type.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// Additional information for <c>NUMERIC</c>, <c>DECIMAL</c>, and <c>DEC</c> data types
/// following the 2016 <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#exact-numeric-type">SQL Standard</see>.
/// </summary>
public abstract record ExactNumberInfo : IDisplay
{
    /// <summary>No additional information, e.g. <c>DECIMAL</c>.</summary>
    public sealed record None : ExactNumberInfo;

    /// <summary>Only precision information, e.g. <c>DECIMAL(10)</c>.</summary>
    public sealed record Precision(ulong Value) : ExactNumberInfo;

    /// <summary>Precision and scale information, e.g. <c>DECIMAL(10,2)</c>.</summary>
    /// <remarks>Rust: <c>PrecisionAndScale(u64, i64)</c>; nomes dos campos em nomes.md.</remarks>
    public sealed record PrecisionAndScale(ulong Precision, long Scale) : ExactNumberInfo
    {
        // Esconde a variante herdada ExactNumberInfo.Precision (CS8866 sem a declaração explícita).
        public new ulong Precision { get; init; } = Precision;
    }

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case None:
                f.Write($"");
                break;
            case Precision p:
                f.Write($"({p.Value})");
                break;
            case PrecisionAndScale ps:
                f.Write($"({ps.Precision},{ps.Scale})");
                break;
            default:
                throw new InvalidOperationException($"Variante de ExactNumberInfo não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
