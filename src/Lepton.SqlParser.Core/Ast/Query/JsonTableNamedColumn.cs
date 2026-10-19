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

/// <summary>A single column definition in MySQL's <c>JSON_TABLE</c> table valued function.</summary>
/// <remarks>
/// See <see href="https://mariadb.com/kb/en/json_table/#path-columns"/>
/// <code>
///         value VARCHAR(20) PATH '$'
/// </code>
/// </remarks>
/// <param name="Name">The name of the column to be extracted.</param>
/// <param name="Type">The type of the column to be extracted.</param>
/// <param name="Path">The path to the column to be extracted. Must be a literal string.</param>
/// <param name="Exists">true if the column is a boolean set to true if the given path exists</param>
/// <param name="OnEmpty">The empty handling clause of the column</param>
/// <param name="OnError">The error handling clause of the column</param>
public sealed partial record JsonTableNamedColumn(
    Ident Name,
    DataType Type,
    ValueWithSpan Path,
    bool Exists,
    JsonTableColumnErrorHandling? OnEmpty,
    JsonTableColumnErrorHandling? OnError) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Name} {Type}{(Exists ? " EXISTS" : "")} PATH {Path}");
        if (OnEmpty is { } onEmpty)
        {
            f.Write($" {onEmpty} ON EMPTY");
        }
        if (OnError is { } onError)
        {
            f.Write($" {onError} ON ERROR");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
