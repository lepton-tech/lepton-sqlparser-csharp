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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Source for the <c>COPY</c> command: a table or a query.</summary>
public abstract partial record CopySource
{
    /// <summary>Copy from a table with optional column list.</summary>
    /// <param name="TableName">The name of the table to copy from.</param>
    /// <param name="Columns">
    /// A list of column names to copy. Empty list means that all columns
    /// are copied.
    /// </param>
    public sealed record Table(ObjectName TableName, Sequence<Ident> Columns) : CopySource;

    /// <summary>Copy from the results of a query.</summary>
    public sealed record Query(Ast.Query Value) : CopySource;
}
