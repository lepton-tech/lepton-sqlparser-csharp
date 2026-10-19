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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>A single column definition in MSSQL's <c>OPENJSON WITH</c> clause.</summary>
/// <remarks>
/// <code>
/// colName type [ column_path ] [ AS JSON ]
/// </code>
/// Reference: <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/openjson-transact-sql?view=sql-server-ver16#syntax"/>
/// </remarks>
/// <param name="Name">The name of the column to be extracted.</param>
/// <param name="Type">The type of the column to be extracted.</param>
/// <param name="Path">The path to the column to be extracted. Must be a literal string.</param>
/// <param name="AsJson">The <c>AS JSON</c> option.</param>
public sealed partial record OpenJsonTableColumn(Ident Name, DataType Type, string? Path, bool AsJson) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Name} {Type}");
        if (Path is { } path)
        {
            // value::escape_single_quote_string → Value.EscapeSingleQuoteString (convencoes.md §13, caso 29)
            f.Write($" '{Value.EscapeSingleQuoteString(path)}'");
        }
        if (AsJson)
        {
            f.Write($" AS JSON");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
