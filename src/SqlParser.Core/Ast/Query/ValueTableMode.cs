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

namespace SqlParser.Core.Ast;

/// <summary>
/// BigQuery supports ValueTables which have 2 modes:
/// <c>SELECT [ALL | DISTINCT] AS STRUCT</c>
/// <c>SELECT [ALL | DISTINCT] AS VALUE</c>
/// <para>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#value_tables"/>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/query-syntax#select_list"/>
/// </para>
/// Mode of BigQuery value tables, e.g. <c>AS STRUCT</c> or <c>AS VALUE</c>.
/// </summary>
// Enum sem dados com `Display` → records (convencoes.md §13, caso 37).
public abstract partial record ValueTableMode : IDisplay
{
    /// <summary><c>AS STRUCT</c></summary>
    public sealed record AsStruct : ValueTableMode;

    /// <summary><c>AS VALUE</c></summary>
    public sealed record AsValue : ValueTableMode;

    /// <summary><c>DISTINCT AS STRUCT</c></summary>
    public sealed record DistinctAsStruct : ValueTableMode;

    /// <summary><c>DISTINCT AS VALUE</c></summary>
    public sealed record DistinctAsValue : ValueTableMode;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case AsStruct:
                f.Write($"AS STRUCT");
                break;
            case AsValue:
                f.Write($"AS VALUE");
                break;
            case DistinctAsStruct:
                f.Write($"DISTINCT AS STRUCT");
                break;
            case DistinctAsValue:
                f.Write($"DISTINCT AS VALUE");
                break;
            default:
                throw new InvalidOperationException($"Variante de ValueTableMode não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
