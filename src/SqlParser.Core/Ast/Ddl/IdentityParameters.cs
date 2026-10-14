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

/// <summary>Parameters specifying seed and increment for identity columns.</summary>
/// <param name="Seed">The initial seed expression for the identity column.</param>
/// <param name="Increment">The increment expression for the identity column.</param>
/// <remarks>Sem <c>impl Display</c> no Rust: o texto é escrito por <see cref="IdentityPropertyFormatKind"/>.</remarks>
public sealed partial record IdentityParameters(Expr Seed, Expr Increment);
