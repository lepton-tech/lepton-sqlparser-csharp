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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>ANALYZE statement</summary>
/// <remarks>
/// Supported syntax varies by dialect:
/// <list type="bullet">
/// <item>Hive: <c>ANALYZE TABLE t [PARTITION (...)] COMPUTE STATISTICS [NOSCAN] [FOR COLUMNS [col1, ...]] [CACHE METADATA]</c></item>
/// <item>PostgreSQL: <c>ANALYZE [VERBOSE] [t [(col1, ...)]]</c> See <see href="https://www.postgresql.org/docs/current/sql-analyze.html"/></item>
/// <item>General: <c>ANALYZE [TABLE] t</c></item>
/// </list>
/// </remarks>
/// <param name="TableName">Name of the table to analyze. <c>None</c> for bare <c>ANALYZE</c>.</param>
/// <param name="Partitions">Optional partition expressions to restrict the analysis.</param>
/// <param name="ForColumns"><c>true</c> when analyzing specific columns (Hive <c>FOR COLUMNS</c> syntax).</param>
/// <param name="Columns">Columns to analyze.</param>
/// <param name="CacheMetadata">Whether to cache metadata before analyzing.</param>
/// <param name="Noscan">Whether to skip scanning the table.</param>
/// <param name="ComputeStatistics">Whether to compute statistics during analysis.</param>
/// <param name="HasTableKeyword">Whether the <c>TABLE</c> keyword was present.</param>
public sealed partial record Analyze(
    [property: VisitWith("visit_relation")] ObjectName? TableName,
    Sequence<Expr>? Partitions,
    bool ForColumns,
    Sequence<Ident> Columns,
    bool CacheMetadata,
    bool Noscan,
    bool ComputeStatistics,
    bool HasTableKeyword) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"ANALYZE");
        if (TableName is { } tableName)
        {
            if (HasTableKeyword)
            {
                f.Write($" TABLE");
            }
            f.Write($" {tableName}");
        }
        if (!ForColumns && !Columns.IsEmpty)
        {
            f.Write($" ({DisplayCommaSeparated(Columns)})");
        }
        if (Partitions is { } parts)
        {
            if (!parts.IsEmpty)
            {
                f.Write($" PARTITION ({DisplayCommaSeparated(parts)})");
            }
        }
        if (ComputeStatistics)
        {
            f.Write($" COMPUTE STATISTICS");
        }
        if (Noscan)
        {
            f.Write($" NOSCAN");
        }
        if (CacheMetadata)
        {
            f.Write($" CACHE METADATA");
        }
        if (ForColumns)
        {
            f.Write($" FOR COLUMNS");
            if (!Columns.IsEmpty)
            {
                f.Write($" {DisplayCommaSeparated(Columns)}");
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
