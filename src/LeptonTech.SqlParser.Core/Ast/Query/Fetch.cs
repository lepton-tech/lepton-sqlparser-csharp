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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary><c>FETCH</c> clause options.</summary>
/// <param name="WithTies"><c>WITH TIES</c> option is present.</param>
/// <param name="Percent"><c>PERCENT</c> modifier is present.</param>
/// <param name="Quantity">Optional quantity expression (e.g. <c>FETCH FIRST 10 ROWS</c>).</param>
public sealed partial record Fetch(bool WithTies, bool Percent, Expr? Quantity) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var extension = WithTies ? "WITH TIES" : "ONLY";
        if (Quantity is { } quantity)
        {
            var percent = Percent ? " PERCENT" : "";
            f.Write($"FETCH FIRST {quantity}{percent} ROWS {extension}");
        }
        else
        {
            f.Write($"FETCH FIRST ROWS {extension}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
