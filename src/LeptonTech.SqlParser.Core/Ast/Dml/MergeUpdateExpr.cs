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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>The expression used to update rows within a <c>MERGE</c> statement.</summary>
/// <remarks>
/// Examples
/// <code>
/// UPDATE SET quantity = T.quantity + S.quantity
/// UPDATE SET *
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/merge">Snowflake</see>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
/// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/MERGE.html">Oracle</see>
/// <see href="https://docs.databricks.com/en/sql/language-manual/delta-merge-into.html">Databricks</see>
/// </remarks>
/// <param name="UpdateToken">The <c>UPDATE</c> token that starts the sub-expression.</param>
/// <param name="Kind">The kind of update: explicit assignments or <c>*</c> shorthand.</param>
/// <param name="UpdatePredicate"><c>where_clause</c> for the update (Oracle specific)</param>
/// <param name="DeletePredicate"><c>delete_clause</c> for the update "delete where" (Oracle specific)</param>
public sealed partial record MergeUpdateExpr(
    AttachedToken UpdateToken,
    MergeUpdateKind Kind,
    Expr? UpdatePredicate,
    Expr? DeletePredicate) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Kind}");
        if (UpdatePredicate is { } predicate)
        {
            f.Write($" WHERE {predicate}");
        }
        if (DeletePredicate is { } deletePredicate)
        {
            f.Write($" DELETE WHERE {deletePredicate}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
