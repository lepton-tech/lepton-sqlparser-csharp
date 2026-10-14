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

namespace SqlParser.Core.Ast;

/// <summary>An <c>IF</c> statement.</summary>
/// <remarks>
/// Example (BigQuery or Snowflake):
/// <code>
/// IF TRUE THEN
///     SELECT 1;
///     SELECT 2;
/// ELSEIF TRUE THEN
///     SELECT 3;
/// ELSE
///     SELECT 4;
/// END IF
/// </code>
/// <para><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#if">BigQuery</see></para>
/// <para><see href="https://docs.snowflake.com/en/sql-reference/snowflake-scripting/if">Snowflake</see></para>
/// <para>Example (MSSQL):</para>
/// <code>
/// IF 1=1 SELECT 1 ELSE SELECT 2
/// </code>
/// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/if-else-transact-sql?view=sql-server-ver16">MSSQL</see></para>
/// </remarks>
/// <param name="IfBlock">The initial <c>IF</c> block containing the condition and statements.</param>
/// <param name="ElseifBlocks">Additional <c>ELSEIF</c> blocks.</param>
/// <param name="ElseBlock">Optional <c>ELSE</c> block.</param>
/// <param name="EndToken">Optional trailing <c>END</c> token for the <c>IF</c> statement.</param>
public sealed partial record IfStatement(
    ConditionalStatementBlock IfBlock,
    Sequence<ConditionalStatementBlock> ElseifBlocks,
    ConditionalStatementBlock? ElseBlock,
    AttachedToken? EndToken) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // let IfStatement { if_block, elseif_blocks, else_block, end_token } = self;
        var ifBlock = IfBlock;
        var elseifBlocks = ElseifBlocks;
        var elseBlock = ElseBlock;
        var endToken = EndToken;

        f.Write($"{ifBlock}");

        foreach (var elseifBlock in elseifBlocks)
        {
            f.Write($" {elseifBlock}");
        }

        if (elseBlock is { } elseBlockValue)
        {
            f.Write($" {elseBlockValue}");
        }

        if (endToken is { Value: var endTokenValue })
        {
            f.Write($" END {endTokenValue}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
