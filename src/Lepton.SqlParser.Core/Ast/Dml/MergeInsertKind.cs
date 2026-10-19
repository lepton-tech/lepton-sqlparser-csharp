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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>The type of expression used to insert rows within a <c>MERGE</c> statement.</summary>
/// <remarks>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/merge">Snowflake</see>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
/// </remarks>
public abstract partial record MergeInsertKind : IDisplay
{
    /// <summary>The insert expression is defined from an explicit <c>VALUES</c> clause</summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// INSERT VALUES(product, quantity)
    /// </code>
    /// </remarks>
    public sealed record Values(Ast.Values Value) : MergeInsertKind;

    /// <summary>The insert expression is defined using only the <c>ROW</c> keyword.</summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// INSERT ROW
    /// </code>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
    /// </remarks>
    public sealed record Row : MergeInsertKind;

    /// <summary>The insert expression uses the <c>*</c> wildcard to insert all columns.</summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// INSERT *
    /// </code>
    /// <see href="https://docs.databricks.com/en/sql/language-manual/delta-merge-into.html">Databricks</see>
    /// </remarks>
    public sealed record Wildcard : MergeInsertKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Values values:
                f.Write($"{values.Value}");
                break;
            case Row:
                f.Write($"ROW");
                break;
            case Wildcard:
                f.Write($"*");
                break;
            default:
                throw new InvalidOperationException($"Variante de MergeInsertKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
