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

/// <summary>
/// An item in the <c>MEASURES</c> subclause of a <c>MATCH_RECOGNIZE</c> operation.
/// An item in the <c>MEASURES</c> clause of <c>MATCH_RECOGNIZE</c>.
/// </summary>
/// <remarks>
/// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/match_recognize#measures-specifying-additional-output-columns"/>.
/// </remarks>
/// <param name="Expr">Expression producing the measure value.</param>
/// <param name="Alias">Alias for the measure column.</param>
public sealed partial record Measure(Expr Expr, Ident Alias) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Expr} AS {Alias}");
    }

    public override string ToString() => Formatter.Format(this);
}
