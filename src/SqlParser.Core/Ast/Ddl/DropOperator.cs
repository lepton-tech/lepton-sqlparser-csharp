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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// <c>DROP OPERATOR</c> statement
/// See <see href="https://www.postgresql.org/docs/current/sql-dropoperator.html"/>
/// </summary>
/// <param name="IfExists"><c>IF EXISTS</c> clause</param>
/// <param name="Operators">One or more operators to drop with their signatures</param>
/// <param name="DropBehavior"><c>CASCADE or RESTRICT</c></param>
public sealed record DropOperator(
    bool IfExists,
    Sequence<DropOperatorSignature> Operators,
    DropBehavior? DropBehavior) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        f.Write($"DROP OPERATOR");
        if (IfExists)
        {
            f.Write($" IF EXISTS");
        }
        f.Write($" {DisplayCommaSeparated(Operators)}");
        if (DropBehavior is { } dropBehavior)
        {
            f.Write($" {dropBehavior}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for DropOperator (ddl.rs:5103)
    public Span Span() => Core.Span.Empty();
}
