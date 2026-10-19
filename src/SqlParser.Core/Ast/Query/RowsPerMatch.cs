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

/// <summary>The rows per match option in a <c>MATCH_RECOGNIZE</c> operation.</summary>
/// <remarks>
/// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/match_recognize#row-s-per-match-specifying-the-rows-to-return"/>.
/// </remarks>
public abstract partial record RowsPerMatch : IDisplay
{
    /// <summary><c>ONE ROW PER MATCH</c></summary>
    public sealed record OneRow : RowsPerMatch;

    /// <summary><c>ALL ROWS PER MATCH &lt;mode&gt;</c></summary>
    public sealed record AllRows(EmptyMatchesMode? Value) : RowsPerMatch;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case OneRow:
                f.Write($"ONE ROW PER MATCH");
                break;
            case AllRows { Value: var mode }:
                f.Write($"ALL ROWS PER MATCH");
                if (mode is { } m)
                {
                    f.Write($" {m}");
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de RowsPerMatch não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
