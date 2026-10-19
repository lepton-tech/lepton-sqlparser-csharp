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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>DROP FUNCTION statement</summary>
/// <param name="IfExists">Whether to include the <c>IF EXISTS</c> clause.</param>
/// <param name="FuncDesc">One or more functions to drop</param>
/// <param name="DropBehavior"><c>CASCADE</c> or <c>RESTRICT</c></param>
public sealed partial record DropFunction(
    bool IfExists,
    Sequence<FunctionDesc> FuncDesc,
    DropBehavior? DropBehavior) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        f.Write($"DROP FUNCTION{(IfExists ? " IF EXISTS" : "")} {DisplayCommaSeparated(FuncDesc)}");
        if (DropBehavior is { } op)
        {
            f.Write($" {op}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for DropFunction (ddl.rs:4824)
    public Span Span() => Core.Span.Empty();
}
