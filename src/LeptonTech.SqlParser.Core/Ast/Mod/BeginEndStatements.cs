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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Represents a list of statements enclosed within <c>BEGIN</c> and <c>END</c> keywords.</summary>
/// <remarks>
/// Example:
/// <code>
/// BEGIN
///     SELECT 1;
///     SELECT 2;
/// END
/// </code>
/// </remarks>
/// <param name="BeginToken">Token representing the <c>BEGIN</c> keyword (may include span info).</param>
/// <param name="Statements">Statements contained within the block.</param>
/// <param name="EndToken">Token representing the <c>END</c> keyword (may include span info).</param>
public sealed partial record BeginEndStatements(AttachedToken BeginToken, Sequence<Statement> Statements, AttachedToken EndToken) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // let BeginEndStatements { begin_token: AttachedToken(begin_token), statements, end_token: AttachedToken(end_token) } = self;
        var beginToken = BeginToken.Value;
        var statements = Statements;
        var endToken = EndToken.Value;

        if (beginToken.Token is not Token.EOF)
        {
            f.Write($"{beginToken} ");
        }
        if (!statements.IsEmpty)
        {
            FormatStatementList(f, statements);
        }
        if (endToken.Token is not Token.EOF)
        {
            f.Write($" {endToken}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
