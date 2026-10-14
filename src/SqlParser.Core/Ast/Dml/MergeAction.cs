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

/// <summary>Underlying statement of a <c>WHEN</c> clause within a <c>MERGE</c> Statement</summary>
/// <remarks>
/// Example
/// <code>
/// INSERT (product, quantity) VALUES(product, quantity)
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/merge">Snowflake</see>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
/// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/MERGE.html">Oracle</see>
/// </remarks>
public abstract partial record MergeAction : IDisplay
{
    /// <summary>An <c>INSERT</c> clause</summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// INSERT (product, quantity) VALUES(product, quantity)
    /// </code>
    /// </remarks>
    public sealed record Insert(MergeInsertExpr Value) : MergeAction;

    /// <summary>An <c>UPDATE</c> clause</summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// UPDATE SET quantity = T.quantity + S.quantity
    /// </code>
    /// </remarks>
    public sealed record Update(MergeUpdateExpr Value) : MergeAction;

    /// <summary>A plain <c>DELETE</c> clause</summary>
    /// <param name="DeleteToken">The <c>DELETE</c> token that starts the sub-expression.</param>
    public sealed record Delete(AttachedToken DeleteToken) : MergeAction;

    /// <summary>A <c>DO NOTHING</c> clause.</summary>
    /// <param name="DoToken">The <c>DO</c> token that starts the sub-expression.</param>
    /// <param name="NothingToken">The <c>NOTHING</c> token that ends the sub-expression.</param>
    public sealed record DoNothing(AttachedToken DoToken, AttachedToken NothingToken) : MergeAction;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Insert insert:
                f.Write($"INSERT {insert.Value}");
                break;
            case Update update:
                f.Write($"UPDATE {update.Value}");
                break;
            case Delete:
                f.Write($"DELETE");
                break;
            case DoNothing:
                f.Write($"DO NOTHING");
                break;
            default:
                throw new InvalidOperationException($"Variante de MergeAction não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
