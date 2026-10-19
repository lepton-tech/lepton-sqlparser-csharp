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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>This represents everything inside the parentheses when calling a function.</summary>
/// <param name="DuplicateTreatment"><c>[ ALL | DISTINCT ]</c></param>
/// <param name="Args">The function arguments.</param>
/// <param name="Clauses">Additional clauses specified within the argument list.</param>
public sealed partial record FunctionArgumentList(
    DuplicateTreatment? DuplicateTreatment,
    Sequence<FunctionArg> Args,
    Sequence<FunctionArgumentClause> Clauses) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (DuplicateTreatment is { } duplicateTreatment)
        {
            f.Write($"{duplicateTreatment} ");
        }
        f.Write($"{DisplayCommaSeparated(Args)}");
        if (!Clauses.IsEmpty)
        {
            if (!Args.IsEmpty)
            {
                f.Write($" ");
            }
            f.Write($"{DisplaySeparated(Clauses, " ")}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
