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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dml.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// The <c>OVERRIDING</c> clause of a PostgreSQL <c>INSERT</c>: whether values supplied for an identity
/// column are used instead of the values its sequence would generate.
/// </summary>
/// <remarks>
/// See <see href="https://www.postgresql.org/docs/current/sql-insert.html"/>
/// </remarks>
public abstract partial record InsertOverriding : IDisplay
{
    /// <summary><c>OVERRIDING SYSTEM VALUE</c></summary>
    public sealed record System : InsertOverriding;

    /// <summary><c>OVERRIDING USER VALUE</c></summary>
    public sealed record User : InsertOverriding;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case System:
                f.Write($"OVERRIDING SYSTEM VALUE");
                break;
            case User:
                f.Write($"OVERRIDING USER VALUE");
                break;
            default:
                throw new InvalidOperationException($"Variante de InsertOverriding não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
