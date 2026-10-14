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

namespace SqlParser.Core.Ast;

/// <summary>Representation of how multiple <c>ColumnOption</c>s are grouped for a column.</summary>
/// <remarks>Sem <c>impl Display</c> no Rust: o texto é escrito por <see cref="ViewColumnDef"/>.</remarks>
public abstract partial record ColumnOptions
{
    /// <summary>Options separated by comma: <c>OPTIONS(a, b, c)</c>.</summary>
    public sealed record CommaSeparated(Sequence<ColumnOption> Value) : ColumnOptions;

    /// <summary>Options separated by spaces: <c>OPTION_A OPTION_B</c>.</summary>
    public sealed record SpaceSeparated(Sequence<ColumnOption> Value) : ColumnOptions;

    /// <summary>Get the column options as a slice.</summary>
    public IReadOnlyList<ColumnOption> AsSlice() =>
        this switch
        {
            CommaSeparated { Value: var options } => options,
            SpaceSeparated { Value: var options } => options,
            _ => throw new InvalidOperationException($"Variante de ColumnOptions não tratada: {GetType().Name}"),
        };
}
