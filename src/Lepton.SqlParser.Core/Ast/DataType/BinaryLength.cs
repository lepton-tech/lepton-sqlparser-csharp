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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// Information about <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#binary-length">binary length</see>,
/// including length and possibly unit.
/// </summary>
public abstract partial record BinaryLength : IDisplay
{
    /// <summary>Integer length for binary types (e.g. <c>VARBINARY(100)</c>).</summary>
    /// <param name="Length">Default (if VARYING)</param>
    public sealed record IntegerLength(ulong Length) : BinaryLength;

    /// <summary>VARBINARY(MAX) used in T-SQL (Microsoft SQL Server).</summary>
    public sealed record Max : BinaryLength;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case IntegerLength il:
                f.Write($"{il.Length}");
                break;
            case Max:
                f.Write($"MAX");
                break;
            default:
                throw new InvalidOperationException($"Variante de BinaryLength não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
