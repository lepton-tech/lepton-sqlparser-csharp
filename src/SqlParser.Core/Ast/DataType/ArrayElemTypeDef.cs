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
/// Represents the data type of the elements in an array (if any) as well as
/// the syntax used to declare the array.
/// </summary>
/// <remarks>
/// For example: Bigquery/Hive use <c>ARRAY&lt;INT&gt;</c> whereas snowflake uses ARRAY.
/// </remarks>
// Sem `Display` no Rust (o texto é montado pelo `Display` de `DataType::Array`).
public abstract partial record ArrayElemTypeDef
{
    /// <summary>Use <c>ARRAY</c> style without an explicit element type.</summary>
    public sealed record None : ArrayElemTypeDef;

    /// <summary>Angle-bracket style, e.g. <c>ARRAY&lt;INT&gt;</c>.</summary>
    public sealed record AngleBracket(DataType Value) : ArrayElemTypeDef;

    /// <summary>Square-bracket style, e.g. <c>INT[]</c> or <c>INT[2]</c>.</summary>
    // Rust: `SquareBracket(Box<DataType>, Option<u64>)`; nomes dos campos em nomes.md.
    public sealed record SquareBracket(DataType DataType, ulong? Size) : ArrayElemTypeDef;

    /// <summary>Parenthesis style, e.g. <c>Array(Int64)</c>.</summary>
    public sealed record Parenthesis(DataType Value) : ArrayElemTypeDef;

    /// <summary>Qualified by a data type and optional size, e.g. <c>INT ARRAY</c> or <c>INT ARRAY[4]</c>.</summary>
    // Rust: `Qualified(Box<DataType>, Option<u64>)`; nomes dos campos em nomes.md.
    public sealed record Qualified(DataType DataType, ulong? Size) : ArrayElemTypeDef;
}
