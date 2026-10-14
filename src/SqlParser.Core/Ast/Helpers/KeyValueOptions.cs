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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/key_value_options.rs.
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>A collection of key-value options.</summary>
/// <remarks>
/// Key-value options for SQL statements.
/// See <see href="https://docs.snowflake.com/en/sql-reference/commands-data-loading">this page</see> for more details.
/// </remarks>
/// <param name="Options">The list of key-value options.</param>
/// <param name="Delimiter">The delimiter used between options.</param>
public sealed record KeyValueOptions(Sequence<KeyValueOption> Options, KeyValueOptionsDelimiter Delimiter) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var sep = Delimiter switch
        {
            KeyValueOptionsDelimiter.Space => " ",
            KeyValueOptionsDelimiter.Comma => ", ",
            _ => throw new InvalidOperationException($"Variante de KeyValueOptionsDelimiter não tratada: {Delimiter}"),
        };
        f.Write($"{DisplaySeparated(Options, sep)}");
    }

    public override string ToString() => Formatter.Format(this);
}
