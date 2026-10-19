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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>A hive LATERAL VIEW with potential column aliases</summary>
/// <param name="LateralViewValue">
/// LATERAL VIEW
/// </param>
/// <param name="LateralViewName">LATERAL VIEW table name</param>
/// <param name="LateralColAlias">LATERAL VIEW optional column aliases</param>
/// <param name="Outer">LATERAL VIEW OUTER</param>
// LateralViewValue: Rust: campo `lateral_view`; sufixo `Value` por CS0542 (convencoes.md §13, caso 41).
public sealed partial record LateralView(
    Expr LateralViewValue,
    ObjectName LateralViewName,
    Sequence<Ident> LateralColAlias,
    bool Outer) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($" LATERAL VIEW{(Outer ? " OUTER" : "")} {LateralViewValue} {LateralViewName}");
        if (!LateralColAlias.IsEmpty)
        {
            f.Write($" AS {DisplayCommaSeparated(LateralColAlias)}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
