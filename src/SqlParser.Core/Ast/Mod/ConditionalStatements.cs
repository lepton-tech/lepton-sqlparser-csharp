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

/// <summary>A list of statements in a <c>ConditionalStatementBlock</c>.</summary>
/// <remarks>Statements used inside conditional blocks (<c>IF</c>, <c>WHEN</c>, <c>WHILE</c>).</remarks>
public abstract partial record ConditionalStatements : IDisplay
{
    /// <summary>Simple sequence of statements (no <c>BEGIN</c>/<c>END</c>).</summary>
    /// <param name="Statements">The statements in the sequence.</param>
    public sealed record Sequence(Sequence<Statement> Statements) : ConditionalStatements
    {
        // O método herdado `Statements()` impede que o parâmetro posicional gere a propriedade (CS8866):
        // ela é declarada explicitamente com `new`, mantendo o nome mecânico. Ver docs/port/onda-a/A1.md.

        /// <summary>The statements in the sequence.</summary>
        public new Sequence<Statement> Statements { get; init; } = Statements;
    }

    /// <summary>Block enclosed by <c>BEGIN</c> and <c>END</c>.</summary>
    // Value: Rust: campo `.0`.
    public sealed record BeginEnd(BeginEndStatements Value) : ConditionalStatements;

    /// <summary>Get the statements in this conditional statements block.</summary>
    public Sequence<Statement> Statements()
    {
        switch (this)
        {
            case Sequence sequence:
                return sequence.Statements;
            case BeginEnd bes:
                return bes.Value.Statements;
            default:
                throw new InvalidOperationException($"Variante de ConditionalStatements não tratada: {GetType().Name}");
        }
    }

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Sequence sequence:
            {
                var statements = sequence.Statements;
                if (!statements.IsEmpty)
                {
                    FormatStatementList(f, statements);
                }
                break;
            }
            case BeginEnd bes:
                f.Write($"{bes.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de ConditionalStatements não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
