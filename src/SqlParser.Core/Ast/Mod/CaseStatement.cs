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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>A <c>CASE</c> statement.</summary>
/// <remarks>
/// Examples:
/// <code>
/// CASE
///     WHEN EXISTS(SELECT 1)
///         THEN SELECT 1 FROM T;
///     WHEN EXISTS(SELECT 2)
///         THEN SELECT 1 FROM U;
///     ELSE
///         SELECT 1 FROM V;
/// END CASE;
/// </code>
/// <para><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#case_search_expression">BigQuery</see></para>
/// <para><see href="https://docs.snowflake.com/en/sql-reference/snowflake-scripting/case">Snowflake</see></para>
/// </remarks>
/// <param name="CaseToken">The <c>CASE</c> token that starts the statement.</param>
/// <param name="MatchExpr">Optional expression to match against in <c>CASE ... WHEN</c>.</param>
/// <param name="WhenBlocks">The <c>WHEN ... THEN</c> blocks of the <c>CASE</c> statement.</param>
/// <param name="ElseBlock">Optional <c>ELSE</c> block for the <c>CASE</c> statement.</param>
/// <param name="EndCaseToken">The last token of the statement (<c>END</c> or <c>CASE</c>).</param>
public sealed partial record CaseStatement(
    AttachedToken CaseToken,
    Expr? MatchExpr,
    Sequence<ConditionalStatementBlock> WhenBlocks,
    ConditionalStatementBlock? ElseBlock,
    AttachedToken EndCaseToken) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // let CaseStatement { case_token: _, match_expr, when_blocks, else_block, end_case_token: AttachedToken(end) } = self;
        var matchExpr = MatchExpr;
        var whenBlocks = WhenBlocks;
        var elseBlock = ElseBlock;
        var end = EndCaseToken.Value;

        f.Write($"CASE");

        if (matchExpr is { } expr)
        {
            f.Write($" {expr}");
        }

        if (!whenBlocks.IsEmpty)
        {
            f.Write($" {DisplaySeparated(whenBlocks, " ")}");
        }

        if (elseBlock is { } elseBlockValue)
        {
            f.Write($" {elseBlockValue}");
        }

        f.Write($" END");

        if (end.Token is Token.Word { Value: var w })
        {
            if (w.Keyword == Keyword.CASE)
            {
                f.Write($" CASE");
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
