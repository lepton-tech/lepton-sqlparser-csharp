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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>Sql options of a <c>CREATE TABLE</c> statement.</summary>
/// <remarks>Options allowed within a <c>CREATE TABLE</c> statement.</remarks>
public abstract partial record CreateTableOptions : IDisplay
{
    /// <summary>No options specified.</summary>
    // Rust: `#[default]`; ver Default.
    public sealed record None : CreateTableOptions;

    /// <summary>Options specified using the <c>WITH</c> keyword, e.g. <c>WITH (k = v)</c>.</summary>
    // Value: Rust: campo `.0`.
    public sealed record With(Sequence<SqlOption> Value) : CreateTableOptions;

    /// <summary>Options specified using the <c>OPTIONS(...)</c> clause.</summary>
    // Value: Rust: campo `.0`.
    public sealed record Options(Sequence<SqlOption> Value) : CreateTableOptions;

    /// <summary>Plain space-separated options.</summary>
    // Value: Rust: campo `.0`.
    public sealed record Plain(Sequence<SqlOption> Value) : CreateTableOptions;

    /// <summary>Table properties (e.g., TBLPROPERTIES / storage properties).</summary>
    // Value: Rust: campo `.0`.
    public sealed record TableProperties(Sequence<SqlOption> Value) : CreateTableOptions;

    // Rust: `#[derive(Default)]` com `#[default] None` (convencoes.md §13, caso 42).
    public static CreateTableOptions Default() => new None();

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case With withOptions:
                f.Write($"WITH ({DisplayCommaSeparated(withOptions.Value)})");
                break;
            case Options options:
                f.Write($"OPTIONS({DisplayCommaSeparated(options.Value)})");
                break;
            case TableProperties options:
                f.Write($"TBLPROPERTIES ({DisplayCommaSeparated(options.Value)})");
                break;
            case Plain options:
                f.Write($"{DisplaySeparated(options.Value, " ")}");
                break;
            case None:
                break;
            default:
                throw new InvalidOperationException($"Variante de CreateTableOptions não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
