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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>The sort order for an <c>ORDER BY</c> expression.</summary>
/// <remarks>
/// See PostgreSQL <c>USING</c> operator:
/// <see href="https://www.postgresql.org/docs/current/sql-select.html"/>
/// </remarks>
// Sem `Display` no Rust (o texto é gerado por `OrderByOptions`): o tipo não implementa `IDisplay`.
public abstract partial record OrderBySort
{
    /// <summary><c>ASC</c></summary>
    public sealed record Asc : OrderBySort;

    /// <summary><c>DESC</c></summary>
    public sealed record Desc : OrderBySort;

    /// <summary>
    /// PostgreSQL <c>USING &lt;operator&gt;</c> ordering.
    /// <para>See <see href="https://www.postgresql.org/docs/current/sql-select.html"/></para>
    /// </summary>
    public sealed record Using(ObjectName Value) : OrderBySort;
}
