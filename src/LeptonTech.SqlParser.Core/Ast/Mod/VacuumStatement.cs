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

/// <summary>Re-sorts rows and reclaims space in either a specified table or all tables in the current database</summary>
/// <remarks>
/// <code>
/// VACUUM [ FULL | SORT ONLY | DELETE ONLY | REINDEX | RECLUSTER ] [ [ table_name ] [ TO threshold PERCENT ] [ BOOST ] ]
/// </code>
/// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_VACUUM_command.html">Redshift</see>
/// </remarks>
/// <param name="Full">Whether <c>FULL</c> was specified.</param>
/// <param name="SortOnly">Whether <c>SORT ONLY</c> was specified.</param>
/// <param name="DeleteOnly">Whether <c>DELETE ONLY</c> was specified.</param>
/// <param name="Reindex">Whether <c>REINDEX</c> was specified.</param>
/// <param name="Recluster">Whether <c>RECLUSTER</c> was specified.</param>
/// <param name="TableName">Optional table to run <c>VACUUM</c> on.</param>
/// <param name="Threshold">Optional threshold value (percent) for <c>TO threshold PERCENT</c>.</param>
/// <param name="Boost">Whether <c>BOOST</c> was specified.</param>
public sealed partial record VacuumStatement(
    bool Full,
    bool SortOnly,
    bool DeleteOnly,
    bool Reindex,
    bool Recluster,
    ObjectName? TableName,
    ValueWithSpan? Threshold,
    bool Boost) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"VACUUM{(Full ? " FULL" : "")}{(SortOnly ? " SORT ONLY" : "")}{(DeleteOnly ? " DELETE ONLY" : "")}{(Reindex ? " REINDEX" : "")}{(Recluster ? " RECLUSTER" : "")}");
        if (TableName is { } tableName)
        {
            f.Write($" {tableName}");
        }
        if (Threshold is { } threshold)
        {
            f.Write($" TO {threshold} PERCENT");
        }
        if (Boost)
        {
            f.Write($" BOOST");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
