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

/// <summary>A block within a <c>Statement::Case</c> or <c>Statement::If</c> or <c>Statement::While</c>-like statement</summary>
/// <remarks>
/// Example 1:
/// <code>
/// WHEN EXISTS(SELECT 1) THEN SELECT 1;
/// </code>
/// Example 2:
/// <code>
/// IF TRUE THEN SELECT 1; SELECT 2;
/// </code>
/// Example 3:
/// <code>
/// ELSE SELECT 1; SELECT 2;
/// </code>
/// Example 4:
/// <code>
/// WHILE @@FETCH_STATUS = 0
/// BEGIN
///    FETCH NEXT FROM c1 INTO @var1, @var2;
/// END
/// </code>
/// </remarks>
/// <param name="StartToken">Token representing the start of the block (e.g., WHEN/IF/WHILE).</param>
/// <param name="Condition">Optional condition expression for the block.</param>
/// <param name="ThenToken">Optional token for the <c>THEN</c> keyword.</param>
/// <param name="ConditionalStatements">The statements contained in this conditional block.</param>
public sealed partial record ConditionalStatementBlock(
    AttachedToken StartToken,
    Expr? Condition,
    AttachedToken? ThenToken,
    ConditionalStatements ConditionalStatements) : IDisplay
{
    /// <summary>Get the statements in this conditional block.</summary>
    public Sequence<Statement> Statements() => ConditionalStatements.Statements();

    public void Fmt(Formatter f)
    {
        // let ConditionalStatementBlock { start_token: AttachedToken(start_token), condition, then_token, conditional_statements } = self;
        var startToken = StartToken.Value;
        var condition = Condition;
        var thenToken = ThenToken;
        var conditionalStatements = ConditionalStatements;

        f.Write($"{startToken}");

        if (condition is { } conditionValue)
        {
            f.Write($" {conditionValue}");
        }

        if (thenToken is not null)
        {
            f.Write($" THEN");
        }

        if (!conditionalStatements.Statements().IsEmpty)
        {
            f.Write($" {conditionalStatements}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
