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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Assignment for a <c>SET</c> statement (name [=|TO] value)</summary>
/// <param name="Scope">Optional context scope (e.g., SESSION or LOCAL).</param>
/// <param name="Name">Assignment target name.</param>
/// <param name="Value">Assigned expression value.</param>
public sealed partial record SetAssignment(ContextModifier? Scope, ObjectName Name, Expr Value) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // self.scope.map(|s| format!("{s}")).unwrap_or_default()
        var scope = Scope is { } s ? Formatter.Format(s) : "";
        f.Write($"{scope}{Name} = {Value}");
    }

    public override string ToString() => Formatter.Format(this);
}
