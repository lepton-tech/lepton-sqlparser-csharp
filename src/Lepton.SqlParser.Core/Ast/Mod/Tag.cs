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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Snowflake <c>WITH TAG ( tag_name = '&lt;tag_value&gt;', ...)</c></summary>
/// <remarks>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table"/>
/// </remarks>
/// <param name="Key">The tag key (can be qualified).</param>
/// <param name="Value">The tag value as a string.</param>
// Rust: `fn new(key, value) -> Self` é o próprio construtor primário (convencoes.md §13, caso 7).
public sealed partial record Tag(ObjectName Key, string Value) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Key}='{Value}'");
    }

    public override string ToString() => Formatter.Format(this);
}
