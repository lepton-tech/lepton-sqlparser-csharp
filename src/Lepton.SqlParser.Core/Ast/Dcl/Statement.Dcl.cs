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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dcl.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

// Os `impl From<X> for Statement` de dcl.rs: membros acrescentados ao `partial record Statement`
// (Mod/Statement.cs), como os demais `impl From<X> for Statement` (convencoes.md §13, caso 7).
public abstract partial record Statement
{
    // Rust: `impl From<Grant> for Statement` (`src/ast/dcl.rs`).
    public static Statement From(Ast.Grant v) => new Grant(v);

    // Rust: `impl From<Revoke> for Statement` (`src/ast/dcl.rs`).
    public static Statement From(Ast.Revoke v) => new Revoke(v);
}
