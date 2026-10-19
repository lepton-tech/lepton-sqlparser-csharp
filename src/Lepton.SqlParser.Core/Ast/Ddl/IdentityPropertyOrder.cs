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
/// The identity column option specifies how values are generated for the auto-incremented column, either in increasing or decreasing order.
/// <para>Syntax</para>
/// <code>
/// ORDER | NOORDER
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
/// </summary>
// Enum sem dados com Display: records (convencoes.md §13, caso 37).
public abstract partial record IdentityPropertyOrder : IDisplay
{
    /// <summary><c>ORDER</c> - preserve ordering for generated values (where supported).</summary>
    public sealed record Order : IdentityPropertyOrder;

    /// <summary><c>NOORDER</c> - do not enforce ordering for generated values.</summary>
    public sealed record NoOrder : IdentityPropertyOrder;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Order:
                f.Write($" ORDER");
                break;
            case NoOrder:
                f.Write($" NOORDER");
                break;
            default:
                throw new InvalidOperationException($"Variante de IdentityPropertyOrder não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
