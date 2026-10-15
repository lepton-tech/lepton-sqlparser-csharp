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

/// <summary>
/// Key/Value, where the value is a (optionally named) list of identifiers
/// <code>
/// UNION = (tbl_name[,tbl_name]...)
/// ENGINE = ReplicatedMergeTree('/table_name','{replica}', ver)
/// ENGINE = SummingMergeTree([columns])
/// </code>
/// </summary>
/// <param name="Key">The option key (identifier) for this named list.</param>
/// <param name="Name">Optional secondary name associated with the key.</param>
/// <param name="Values">The list of identifier values for the key.</param>
// Sem `impl Display` no Rust.
public sealed partial record NamedParenthesizedList(Ident Key, Ident? Name, Sequence<Ident> Values);
