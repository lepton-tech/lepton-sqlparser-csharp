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

namespace SqlParser.Core.Ast;

/// <summary>Defines the options for an XmlTable column: Named or ForOrdinality</summary>
/// <remarks>Sem <c>Display</c> no Rust (o texto é gerado por <c>XmlTableColumn</c>): o tipo não implementa <c>IDisplay</c>.</remarks>
public abstract partial record XmlTableColumnOption
{
    /// <summary>A named column with a type, optional path, and default value.</summary>
    /// <param name="Type">The type of the column to be extracted.</param>
    /// <param name="Path">The path to the column to be extracted. If None, defaults to the column name.</param>
    /// <param name="Default">Default value if path does not match</param>
    /// <param name="Nullable">Whether the column is nullable (NULL=true, NOT NULL=false)</param>
    public sealed record NamedInfo(DataType Type, Expr? Path, Expr? Default, bool Nullable) : XmlTableColumnOption;

    /// <summary>The FOR ORDINALITY marker</summary>
    public sealed record ForOrdinality : XmlTableColumnOption;
}
