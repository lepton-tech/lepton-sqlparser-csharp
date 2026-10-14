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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>The table sample modifier options</summary>
/// <remarks>Sem <c>Display</c> no Rust: o tipo não implementa <c>IDisplay</c>.</remarks>
public abstract record TableSampleKind
{
    /// <summary>Table sample located before the table alias option</summary>
    public sealed record BeforeTableAlias(TableSample Value) : TableSampleKind;

    /// <summary>Table sample located after the table alias option</summary>
    public sealed record AfterTableAlias(TableSample Value) : TableSampleKind;
}
