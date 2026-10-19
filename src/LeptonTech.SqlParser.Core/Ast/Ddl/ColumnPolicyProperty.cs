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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Properties describing a column policy (masking or projection).</summary>
/// <param name="With">
/// This flag indicates that the column policy option is declared using the <c>WITH</c> prefix.
/// Example
/// <code>
/// WITH PROJECTION POLICY sample_policy
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
/// </param>
/// <param name="PolicyName">The name of the policy to apply to the column.</param>
/// <param name="UsingColumns">Optional list of column identifiers referenced by the policy.</param>
// Sem `impl Display` no Rust: o texto é escrito por ColumnPolicy.
public sealed partial record ColumnPolicyProperty(bool With, ObjectName PolicyName, Sequence<Ident>? UsingColumns);
