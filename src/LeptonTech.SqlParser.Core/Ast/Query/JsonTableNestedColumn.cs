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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// A nested column in a JSON_TABLE column list
/// A nested column in a <c>JSON_TABLE</c> column list.
/// </summary>
/// <remarks>
/// See <see href="https://mariadb.com/kb/en/json_table/#nested-paths"/>
/// </remarks>
/// <param name="Path">JSON path expression (must be a literal <c>Value</c>).</param>
/// <param name="Columns">Columns extracted from the matched nested array.</param>
public sealed partial record JsonTableNestedColumn(ValueWithSpan Path, Sequence<JsonTableColumn> Columns) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"NESTED PATH {Path} COLUMNS ({DisplayCommaSeparated(Columns)})");
    }

    public override string ToString() => Formatter.Format(this);
}
