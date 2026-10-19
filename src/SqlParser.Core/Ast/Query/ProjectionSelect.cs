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

/// <summary>
/// Query syntax for ClickHouse ADD PROJECTION statement.
/// Its syntax is similar to SELECT statement, but it is used to add a new projection to a table.
/// Syntax is <c>SELECT &lt;COLUMN LIST EXPR&gt; [GROUP BY] [ORDER BY]</c>
/// </summary>
/// <remarks>
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/projection#add-projection">ClickHouse</see>
/// </remarks>
/// <param name="Projection">The list of projected select items.</param>
/// <param name="OrderBy">Optional <c>ORDER BY</c> clause for the projection-select.</param>
/// <param name="GroupBy">Optional <c>GROUP BY</c> clause for the projection-select.</param>
public sealed partial record ProjectionSelect(Sequence<SelectItem> Projection, OrderBy? OrderBy, GroupByExpr? GroupBy) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"SELECT {DisplayCommaSeparated(Projection)}");
        if (GroupBy is { } groupBy)
        {
            f.Write($" {groupBy}");
        }
        if (OrderBy is { } orderBy)
        {
            f.Write($" {orderBy}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
