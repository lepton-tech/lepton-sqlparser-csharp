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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A <c>MERGE</c> statement.</summary>
/// <param name="MergeToken">The <c>MERGE</c> token that starts the statement.</param>
/// <param name="OptimizerHints">
/// Query optimizer hints
/// <para><see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Comments.html#GUID-D316D545-89E2-4D54-977F-FC97815CD62E">Oracle</see></para>
/// </param>
/// <param name="Into">optional INTO keyword</param>
/// <param name="Table">Specifies the table to merge</param>
/// <param name="Source">Specifies the table or subquery to join with the target table</param>
/// <param name="On">Specifies the expression on which to join the target table and source</param>
/// <param name="Clauses">Specifies the actions to perform when values match or do not match.</param>
/// <param name="Output">Specifies the output to save changes in MSSQL</param>
public sealed partial record Merge(
    AttachedToken MergeToken,
    Sequence<OptimizerHint> OptimizerHints,
    bool Into,
    TableFactor Table,
    TableFactor Source,
    Expr On,
    Sequence<MergeClause> Clauses,
    OutputClause? Output) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.WriteStr("MERGE");
        foreach (var hint in OptimizerHints)
        {
            f.Write($" {hint}");
        }
        if (Into)
        {
            f.Write($" INTO");
        }
        f.Write($" {Table} USING {Source} ");
        f.Write($"ON {On} ");
        f.Write($"{DisplaySeparated(Clauses, " ")}");
        if (Output is { } output)
        {
            f.Write($" {output}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
