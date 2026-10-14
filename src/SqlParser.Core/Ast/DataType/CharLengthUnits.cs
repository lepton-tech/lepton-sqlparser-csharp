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
/// Possible units for characters, initially based on 2016 ANSI
/// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#char-length-units">SQL Standard</see>.
/// </summary>
public abstract partial record CharLengthUnits : IDisplay
{
    /// <summary>CHARACTERS unit</summary>
    public sealed record Characters : CharLengthUnits;

    /// <summary>OCTETS unit</summary>
    public sealed record Octets : CharLengthUnits;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Characters:
                f.Write($"CHARACTERS");
                break;
            case Octets:
                f.Write($"OCTETS");
                break;
            default:
                throw new InvalidOperationException($"Variante de CharLengthUnits não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
