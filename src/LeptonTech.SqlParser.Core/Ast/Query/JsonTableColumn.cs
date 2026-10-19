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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A single column definition in MySQL's <c>JSON_TABLE</c> table valued function.</summary>
/// <remarks>
/// See
/// - <see href="https://dev.mysql.com/doc/refman/8.0/en/json-table-functions.html#function_json-table">MySQL's JSON_TABLE documentation</see>
/// - <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/JSON_TABLE.html">Oracle's JSON_TABLE documentation</see>
/// - <see href="https://mariadb.com/kb/en/json_table/">MariaDB's JSON_TABLE documentation</see>
/// <code>
/// SELECT *
/// FROM JSON_TABLE(
///     '["a", "b"]',
///     '$[*]' COLUMNS (
///         name FOR ORDINALITY,
///         value VARCHAR(20) PATH '$',
///         NESTED PATH '$[*]' COLUMNS (
///             value VARCHAR(20) PATH '$'
///         )
///     )
/// ) AS jt;
/// </code>
/// </remarks>
public abstract partial record JsonTableColumn : IDisplay
{
    /// <summary>A named column with a JSON path</summary>
    public sealed record Named(JsonTableNamedColumn Value) : JsonTableColumn;

    /// <summary>The FOR ORDINALITY column, which is a special column that returns the index of the current row in a JSON array.</summary>
    public sealed record ForOrdinality(Ident Value) : JsonTableColumn;

    /// <summary>A set of nested columns, which extracts data from a nested JSON array.</summary>
    public sealed record Nested(JsonTableNestedColumn Value) : JsonTableColumn;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Named { Value: var jsonTableNamedColumn }:
                f.Write($"{jsonTableNamedColumn}");
                break;
            case ForOrdinality { Value: var ident }:
                f.Write($"{ident} FOR ORDINALITY");
                break;
            case Nested { Value: var jsonTableNestedColumn }:
                f.Write($"{jsonTableNestedColumn}");
                break;
            default:
                throw new InvalidOperationException($"Variante de JsonTableColumn não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
