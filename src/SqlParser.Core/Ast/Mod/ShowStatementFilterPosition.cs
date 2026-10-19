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

/// <summary>Where a <c>SHOW</c> filter appears relative to the main clause.</summary>
// Sem `impl Display` no Rust: quem formata é ShowStatementOptions.
public abstract partial record ShowStatementFilterPosition
{
    /// <summary>Put the filter in an infix position (e.g. <c>SHOW COLUMNS LIKE '%name%' IN TABLE tbl</c>).</summary>
    public sealed record Infix(ShowStatementFilter Value) : ShowStatementFilterPosition; // For example: SHOW COLUMNS LIKE '%name%' IN TABLE tbl

    /// <summary>Put the filter in a suffix position (e.g. <c>SHOW COLUMNS IN tbl LIKE '%name%'</c>).</summary>
    public sealed record Suffix(ShowStatementFilter Value) : ShowStatementFilterPosition; // For example: SHOW COLUMNS IN tbl LIKE '%name%'
}
