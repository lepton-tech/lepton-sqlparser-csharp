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

/// <summary>Variant of <c>WHEN</c> clause used within a <c>MERGE</c> Statement.</summary>
/// <remarks>
/// Example:
/// <code>
/// MERGE INTO T USING U ON FALSE WHEN MATCHED THEN DELETE
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/merge">Snowflake</see>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
/// </remarks>
public abstract partial record MergeClauseKind : IDisplay
{
    /// <summary><c>WHEN MATCHED</c></summary>
    public sealed record Matched : MergeClauseKind;

    /// <summary><c>WHEN NOT MATCHED</c></summary>
    public sealed record NotMatched : MergeClauseKind;

    /// <summary>
    /// <c>WHEN MATCHED BY TARGET</c>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
    /// </summary>
    public sealed record NotMatchedByTarget : MergeClauseKind;

    /// <summary>
    /// <c>WHEN MATCHED BY SOURCE</c>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#merge_statement">BigQuery</see>
    /// </summary>
    public sealed record NotMatchedBySource : MergeClauseKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Matched:
                f.Write($"MATCHED");
                break;
            case NotMatched:
                f.Write($"NOT MATCHED");
                break;
            case NotMatchedByTarget:
                f.Write($"NOT MATCHED BY TARGET");
                break;
            case NotMatchedBySource:
                f.Write($"NOT MATCHED BY SOURCE");
                break;
            default:
                throw new InvalidOperationException($"Variante de MergeClauseKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
