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

/// <summary>The after match skip option in a <c>MATCH_RECOGNIZE</c> operation.</summary>
/// <remarks>
/// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/match_recognize#after-match-skip-specifying-where-to-continue-after-a-match"/>.
/// </remarks>
public abstract partial record AfterMatchSkip : IDisplay
{
    /// <summary><c>PAST LAST ROW</c></summary>
    public sealed record PastLastRow : AfterMatchSkip;

    /// <summary><c>TO NEXT ROW</c></summary>
    public sealed record ToNextRow : AfterMatchSkip;

    /// <summary><c>TO FIRST &lt;symbol&gt;</c></summary>
    public sealed record ToFirst(Ident Value) : AfterMatchSkip;

    /// <summary><c>TO LAST &lt;symbol&gt;</c></summary>
    public sealed record ToLast(Ident Value) : AfterMatchSkip;

    public void Fmt(Formatter f)
    {
        f.Write($"AFTER MATCH SKIP ");
        switch (this)
        {
            case PastLastRow:
                f.Write($"PAST LAST ROW");
                break;
            case ToNextRow:
                // Rust: write!(f, " TO NEXT ROW"), com o espaço inicial (gera dois espaços); portado como está.
                f.Write($" TO NEXT ROW");
                break;
            case ToFirst { Value: var symbol }:
                f.Write($"TO FIRST {symbol}");
                break;
            case ToLast { Value: var symbol }:
                f.Write($"TO LAST {symbol}");
                break;
            default:
                throw new InvalidOperationException($"Variante de AfterMatchSkip não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
