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

// Parte (partial) do tipo `Statement` (unidade A2, Ast/Mod/Statement.cs) com os
// `impl From<X> for crate::ast::Statement` de src/ast/ddl.rs (linhas 5950–5997), na ordem do Rust:
// `Statement.From(x)` (convencoes.md §13, caso 7). Os tipos dos parâmetros são qualificados com `Ast.`
// porque as variantes aninhadas de `Statement` escondem os tipos de topo homônimos (caso 38).
public abstract partial record Statement
{
    /// <summary>Rust: <c>impl From&lt;CreatePolicy&gt; for crate::ast::Statement</c>.</summary>
    public static Statement From(Ast.CreatePolicy v) => new CreatePolicy(v);

    /// <summary>Rust: <c>impl From&lt;DropPolicy&gt; for crate::ast::Statement</c>.</summary>
    public static Statement From(Ast.DropPolicy v) => new DropPolicy(v);

    /// <summary>Rust: <c>impl From&lt;AlterPolicy&gt; for crate::ast::Statement</c>.</summary>
    public static Statement From(Ast.AlterPolicy v) => new AlterPolicy(v);
}
