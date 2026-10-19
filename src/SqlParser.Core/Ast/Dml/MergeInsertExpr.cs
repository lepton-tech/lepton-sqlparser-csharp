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

/// <summary>The expression used to insert rows within a <c>MERGE</c> statement.</summary>
/// <remarks>
/// Examples
/// <code>
/// INSERT (product, quantity) VALUES(product, quantity)
/// INSERT ROW
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/merge">Snowflake</see>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
/// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/MERGE.html">Oracle</see>
/// </remarks>
/// <param name="InsertToken">The <c>INSERT</c> token that starts the sub-expression.</param>
/// <param name="Columns">
/// Columns (if any) specified by the insert.
/// <para>Example:</para>
/// <code>
/// INSERT (product, quantity) VALUES(product, quantity)
/// INSERT (product, quantity) ROW
/// </code>
/// </param>
/// <param name="KindToken">The token, <c>[VALUES | ROW]</c> starting <c>kind</c>.</param>
/// <param name="Kind">The insert type used by the statement.</param>
/// <param name="InsertPredicate">An optional condition to restrict the insertion (Oracle specific)</param>
public sealed partial record MergeInsertExpr(
    AttachedToken InsertToken,
    Sequence<ObjectName> Columns,
    AttachedToken KindToken,
    MergeInsertKind Kind,
    Expr? InsertPredicate) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (!Columns.IsEmpty)
        {
            f.Write($"({DisplayCommaSeparated(Columns)}) ");
        }
        f.Write($"{Kind}");
        if (InsertPredicate is { } predicate)
        {
            f.Write($" WHERE {predicate}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
