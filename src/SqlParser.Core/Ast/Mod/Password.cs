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

/// <summary>Password specification variants used in user-related statements.</summary>
public abstract partial record Password
{
    /// <summary>A concrete password expression.</summary>
    /// <remarks>
    /// Rust: variante <c>Password::Password(Expr)</c>. Renomeada para <c>PasswordValue</c> porque um tipo
    /// aninhado não pode ter o nome do tipo que o contém (CS0542). Ver "Nomes" em docs/port/onda-a/A1.md.
    /// </remarks>
    /// <param name="Value">Rust: campo <c>.0</c>.</param>
    public sealed record PasswordValue(Expr Value) : Password;

    /// <summary>Represents a <c>NULL</c> password.</summary>
    public sealed record NullPassword : Password;
}
