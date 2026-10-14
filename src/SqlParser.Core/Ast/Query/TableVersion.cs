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

/// <summary>Specifies a table version selection, e.g. <c>FOR SYSTEM_TIME AS OF</c> or <c>AT(...)</c>.</summary>
public abstract partial record TableVersion : IDisplay
{
    /// <summary>
    /// When the table version is defined using <c>FOR SYSTEM_TIME AS OF</c>.
    /// For example: <c>SELECT * FROM tbl FOR SYSTEM_TIME AS OF TIMESTAMP_SUB(CURRENT_TIMESTAMP(), INTERVAL 1 HOUR)</c>
    /// </summary>
    public sealed record ForSystemTimeAsOf(Expr Value) : TableVersion;

    /// <summary>
    /// When the table version is defined using <c>TIMESTAMP AS OF</c>.
    /// Databricks supports this syntax.
    /// For example: <c>SELECT * FROM tbl TIMESTAMP AS OF CURRENT_TIMESTAMP() - INTERVAL 1 HOUR</c>
    /// </summary>
    public sealed record TimestampAsOf(Expr Value) : TableVersion;

    /// <summary>
    /// When the table version is defined using <c>VERSION AS OF</c>.
    /// Databricks supports this syntax.
    /// For example: <c>SELECT * FROM tbl VERSION AS OF 2</c>
    /// </summary>
    public sealed record VersionAsOf(Expr Value) : TableVersion;

    /// <summary>
    /// When the table version is defined using a function.
    /// For example: <c>SELECT * FROM tbl AT(TIMESTAMP =&gt; '2020-08-14 09:30:00')</c>
    /// </summary>
    public sealed record Function(Expr Value) : TableVersion;

    /// <summary>
    /// Snowflake <c>CHANGES</c> clause for change tracking queries.
    /// For example:
    /// <code>
    /// SELECT * FROM t
    ///   CHANGES(INFORMATION =&gt; DEFAULT)
    ///   AT(TIMESTAMP =&gt; TO_TIMESTAMP_TZ('...'))
    ///   END(TIMESTAMP =&gt; TO_TIMESTAMP_TZ('...'))
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/constructs/changes"/>
    /// </summary>
    /// <param name="ChangesValue">
    /// The <c>CHANGES(INFORMATION =&gt; ...)</c> function-call expression.
    /// <para>Rust: campo <c>changes</c>; sufixo <c>Value</c> por CS0542 (convencoes.md §13, caso 41).</para>
    /// </param>
    /// <param name="At">The <c>AT(TIMESTAMP =&gt; ...)</c> function-call expression.</param>
    /// <param name="End">The optional <c>END(TIMESTAMP =&gt; ...)</c> function-call expression.</param>
    public sealed record Changes(Expr ChangesValue, Expr At, Expr? End) : TableVersion;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case ForSystemTimeAsOf { Value: var e }:
                f.Write($"FOR SYSTEM_TIME AS OF {e}");
                break;
            case TimestampAsOf { Value: var e }:
                f.Write($"TIMESTAMP AS OF {e}");
                break;
            case VersionAsOf { Value: var e }:
                f.Write($"VERSION AS OF {e}");
                break;
            case Function { Value: var func }:
                f.Write($"{func}");
                break;
            case Changes { ChangesValue: var changes, At: var at, End: var end }:
                f.Write($"{changes} {at}");
                if (end is { } endExpr)
                {
                    f.Write($" {endExpr}");
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de TableVersion não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
