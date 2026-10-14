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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dml.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>A WHEN clause in a conditional multi-table INSERT.</summary>
/// <remarks>
/// Syntax:
/// <code>
/// WHEN n1 &gt; 100 THEN
///   INTO t1
///   INTO t2 (c1, c2) VALUES (n1, n2)
/// </code>
/// </remarks>
/// <param name="Condition">The condition for this WHEN clause</param>
/// <param name="IntoClauses">The INTO clauses to execute when the condition is true</param>
public sealed partial record MultiTableInsertWhenClause(Expr Condition, Sequence<MultiTableInsertIntoClause> IntoClauses) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"WHEN {Condition} THEN");
        foreach (var intoClause in IntoClauses)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.Write($"{intoClause}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
