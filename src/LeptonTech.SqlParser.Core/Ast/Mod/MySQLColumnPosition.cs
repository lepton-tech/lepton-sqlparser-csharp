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

/// <summary>MySQL <c>ALTER TABLE</c> only  [FIRST | AFTER column_name]</summary>
/// <remarks>MySQL <c>ALTER TABLE</c> column position specifier: <c>FIRST</c> or <c>AFTER &lt;column&gt;</c>.</remarks>
public abstract partial record MySQLColumnPosition : IDisplay
{
    /// <summary>Place the column first in the table.</summary>
    public sealed record First : MySQLColumnPosition;

    /// <summary>Place the column after the specified identifier.</summary>
    public sealed record After(Ident Value) : MySQLColumnPosition;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case First:
                f.Write($"FIRST");
                break;
            case After { Value: var ident }:
                f.Write($"AFTER {ident}");
                break;
            default:
                throw new InvalidOperationException($"Variante de MySQLColumnPosition não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
