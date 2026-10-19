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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>Represents the different syntactic forms of <c>LIMIT</c> clauses.</summary>
public abstract partial record LimitClause : IDisplay
{
    /// <summary>
    /// Standard SQL <c>LIMIT</c> syntax (optionally <c>BY</c> and <c>OFFSET</c>).
    /// <para><c>LIMIT &lt;limit&gt; [BY &lt;expr&gt;,&lt;expr&gt;,...] [OFFSET &lt;offset&gt;]</c></para>
    /// </summary>
    /// <param name="Limit"><c>LIMIT { &lt;N&gt; | ALL }</c> expression.</param>
    /// <param name="Offset">Optional <c>OFFSET</c> expression with optional <c>ROW(S)</c> keyword.</param>
    /// <param name="LimitBy">Optional <c>BY { &lt;expr&gt;,... }</c> list used by some dialects (ClickHouse).</param>
    public sealed record LimitOffset(Expr? Limit, Offset? Offset, Sequence<Expr> LimitBy) : LimitClause;

    /// <summary>MySQL-specific syntax: <c>LIMIT &lt;offset&gt;, &lt;limit&gt;</c> (order reversed).</summary>
    /// <param name="Offset">The offset expression.</param>
    /// <param name="Limit">The limit expression.</param>
    public sealed record OffsetCommaLimit(Expr Offset, Expr Limit) : LimitClause;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case LimitOffset { Limit: var limit, LimitBy: var limitBy, Offset: var offset }:
                if (limit is { } limitExpr)
                {
                    f.Write($" LIMIT {limitExpr}");
                }
                if (offset is { } offsetValue)
                {
                    f.Write($" {offsetValue}");
                }
                if (!limitBy.IsEmpty)
                {
#if DEBUG
                    // debug_assert!(limit.is_some()) → só em build de depuração, como no Rust (A7.md, "Decisões").
                    if (!(limit is not null))
                    {
                        throw new InvalidOperationException("assertion failed: limit.is_some()");
                    }
#endif
                    f.Write($" BY {DisplaySeparated(limitBy, ", ")}");
                }
                break;
            case OffsetCommaLimit { Offset: var offset, Limit: var limit }:
                f.Write($" LIMIT {offset}, {limit}");
                break;
            default:
                throw new InvalidOperationException($"Variante de LimitClause não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
