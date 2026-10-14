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

namespace SqlParser.Core.Ast;

/// <summary>The syntax used for in a cast expression.</summary>
/// <remarks>Enum sem dados e sem nenhum <c>impl</c> no Rust: <c>enum</c> C# (convencoes.md §6, regra 1).</remarks>
public enum CastKind
{
    /// <summary>The standard SQL cast syntax, e.g. <c>CAST(&lt;expr&gt; as &lt;datatype&gt;)</c></summary>
    Cast,

    /// <summary>
    /// A cast that returns <c>NULL</c> on failure, e.g. <c>TRY_CAST(&lt;expr&gt; as &lt;datatype&gt;)</c>.
    /// <para>See <see href="https://docs.snowflake.com/en/sql-reference/functions/try_cast"/>.</para>
    /// <para>See <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/try-cast-transact-sql"/>.</para>
    /// </summary>
    TryCast,

    /// <summary>
    /// A cast that returns <c>NULL</c> on failure, bigQuery-specific ,  e.g. <c>SAFE_CAST(&lt;expr&gt; as &lt;datatype&gt;)</c>.
    /// <para>See <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/functions-and-operators#safe_casting"/>.</para>
    /// </summary>
    SafeCast,

    /// <summary><c>&lt;expr&gt; :: &lt;datatype&gt;</c></summary>
    DoubleColon,
}
