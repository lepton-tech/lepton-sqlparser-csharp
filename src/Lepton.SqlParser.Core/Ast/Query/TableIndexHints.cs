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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>MySQL-style index hints attached to a table (e.g., <c>USE INDEX(...)</c>).</summary>
/// <param name="HintType">Type of hint (e.g., <c>USE</c>, <c>FORCE</c>, or <c>IGNORE</c>).</param>
/// <param name="IndexType">The index type (e.g., <c>INDEX</c>).</param>
/// <param name="ForClause">Optional <c>FOR</c> clause specifying the scope (JOIN / ORDER BY / GROUP BY).</param>
/// <param name="IndexNames">List of index names referred to by the hint.</param>
public sealed partial record TableIndexHints(
    TableIndexHintType HintType,
    TableIndexType IndexType,
    TableIndexHintForClause? ForClause,
    Sequence<Ident> IndexNames) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{HintType} {IndexType} ");
        if (ForClause is { } forClause)
        {
            f.Write($"FOR {forClause} ");
        }
        f.Write($"({DisplayCommaSeparated(IndexNames)})");
    }

    public override string ToString() => Formatter.Format(this);
}
