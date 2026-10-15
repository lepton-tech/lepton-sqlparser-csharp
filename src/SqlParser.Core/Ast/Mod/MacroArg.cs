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

/// <summary><c>NAME = &lt;EXPR&gt;</c> arguments for DuckDB macros</summary>
/// <remarks>
/// See <see href="https://duckdb.org/docs/sql/statements/create_macro">Create Macro - DuckDB</see>
/// for more details
/// </remarks>
/// <param name="Name">The argument name.</param>
/// <param name="DefaultExpr">Optional default expression for the argument.</param>
public sealed partial record MacroArg(Ident Name, Expr? DefaultExpr) : IDisplay
{
    /// <summary>Returns an argument with name.</summary>
    // Rust: `fn new(name: &str) -> Self` (convencoes.md §13, caso 7).
    public MacroArg(string name)
        : this(Ident.From(name), null)
    {
    }

    public void Fmt(Formatter f)
    {
        f.Write($"{Name}");
        if (DefaultExpr is { } defaultExpr)
        {
            f.Write($" := {defaultExpr}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
