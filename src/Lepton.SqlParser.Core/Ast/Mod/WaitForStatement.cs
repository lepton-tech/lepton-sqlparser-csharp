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

/// <summary>MSSQL <c>WAITFOR</c> statement.</summary>
/// <remarks>See: <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/waitfor-transact-sql"/></remarks>
/// <param name="WaitType"><c>DELAY</c> or <c>TIME</c>.</param>
/// <param name="Expr">The time expression.</param>
public sealed partial record WaitForStatement(WaitForType WaitType, Expr Expr) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"WAITFOR {WaitType} {Expr}");
    }

    public override string ToString() => Formatter.Format(this);
}
