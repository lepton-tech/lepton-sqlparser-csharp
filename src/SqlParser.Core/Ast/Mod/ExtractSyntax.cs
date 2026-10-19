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

/// <summary><c>EXTRACT</c> syntax variants.</summary>
/// <remarks>
/// In Snowflake dialect, the <c>EXTRACT</c> expression can support either the <c>from</c> syntax
/// or the comma syntax.
/// <para>See <see href="https://docs.snowflake.com/en/sql-reference/functions/extract"/></para>
/// </remarks>
// Enum sem dados e sem nenhum `impl` no Rust: `enum` C# (convencoes.md §6, regra 1).
public enum ExtractSyntax
{
    /// <summary><c>EXTRACT( &lt;date_or_time_part&gt; FROM &lt;date_or_time_expr&gt; )</c></summary>
    From,

    /// <summary><c>EXTRACT( &lt;date_or_time_part&gt; , &lt;date_or_timestamp_expr&gt; )</c></summary>
    Comma,
}
