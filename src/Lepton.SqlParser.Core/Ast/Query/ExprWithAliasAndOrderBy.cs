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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>An expression optionally followed by an alias and order by options.</summary>
/// <remarks>
/// Example:
/// <code>
/// 42 AS myint ASC
/// </code>
/// </remarks>
/// <param name="Expr">Expression with optional alias.</param>
/// <param name="OrderBy">Ordering options applied to the expression.</param>
public sealed partial record ExprWithAliasAndOrderBy(ExprWithAlias Expr, OrderByOptions OrderBy) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Expr}{OrderBy}");
    }

    public override string ToString() => Formatter.Format(this);
}
