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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>An <c>ALTER TYPE</c> statement (<c>Statement::AlterType</c>)</summary>
/// <param name="Name">Name of the type being altered (may be schema-qualified).</param>
/// <param name="Operation">The specific alteration operation to perform.</param>
// Sem `impl Display` no Rust: o SQL é escrito pelo `Display` de `Statement::AlterType`.
public sealed partial record AlterType(ObjectName Name, AlterTypeOperation Operation);
