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

/// <summary>A member of an ENUM type.</summary>
/// <remarks>Sem <c>Display</c> no Rust (o texto é montado pelo <c>Display</c> de <c>DataType::Enum</c>).</remarks>
public abstract partial record EnumMember
{
    /// <summary>Just a name.</summary>
    public sealed record Name(string Value) : EnumMember;

    /// <summary>
    /// ClickHouse allows to specify an integer value for each enum value.
    /// <see href="https://clickhouse.com/docs/en/sql-reference/data-types/enum">ClickHouse</see>
    /// </summary>
    /// <remarks>Rust: <c>NamedValue(String, Expr)</c>; nomes dos campos em nomes.md.</remarks>
    public sealed record NamedValue(string Name, Expr Value) : EnumMember
    {
        // Esconde a variante herdada EnumMember.Name (CS8866 sem a declaração explícita).
        public new string Name { get; init; } = Name;
    }
}
