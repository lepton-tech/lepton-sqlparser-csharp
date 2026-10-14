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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>An INTO clause in a multi-table INSERT.</summary>
/// <remarks>
/// Syntax:
/// <code>
/// INTO &lt;target_table&gt; [ ( &lt;target_col_name&gt; [ , ... ] ) ] [ VALUES ( { &lt;source_col_name&gt; | DEFAULT | NULL } [ , ... ] ) ]
/// </code>
/// </remarks>
/// <param name="TableName">The target table</param>
/// <param name="Columns">The target columns (optional)</param>
/// <param name="Values">The VALUES clause (optional)</param>
public sealed record MultiTableInsertIntoClause(ObjectName TableName, Sequence<Ident> Columns, MultiTableInsertValues? Values) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"INTO {TableName}");
        if (!Columns.IsEmpty)
        {
            f.Write($" ({DisplayCommaSeparated(Columns)})");
        }
        if (Values is { } values)
        {
            f.Write($" VALUES ({DisplayCommaSeparated(values.Values)})");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
