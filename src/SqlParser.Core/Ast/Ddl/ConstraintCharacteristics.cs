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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// <c>&lt;constraint_characteristics&gt; = [ DEFERRABLE | NOT DEFERRABLE ] [ INITIALLY DEFERRED | INITIALLY IMMEDIATE ] [ ENFORCED | NOT ENFORCED ]</c>
/// <para>Used in UNIQUE and foreign key constraints. The individual settings may occur in any order.</para>
/// </summary>
/// <param name="Deferrable"><c>[ DEFERRABLE | NOT DEFERRABLE ]</c></param>
/// <param name="Initially"><c>[ INITIALLY DEFERRED | INITIALLY IMMEDIATE ]</c></param>
/// <param name="Enforced"><c>[ ENFORCED | NOT ENFORCED ]</c></param>
public sealed partial record ConstraintCharacteristics(bool? Deferrable, DeferrableInitial? Initially, bool? Enforced) : IDisplay
{
    /// <summary>Rust: <c>#[derive(Default)]</c> (<c>ConstraintCharacteristics::default()</c>).</summary>
    public static ConstraintCharacteristics Default() => new ConstraintCharacteristics(null, null, null);

    private string? DeferrableText() =>
        Deferrable is { } deferrable
            ? (deferrable ? "DEFERRABLE" : "NOT DEFERRABLE")
            : null;

    private string? InitiallyImmediateText() =>
        Initially is { } initiallyImmediate
            ? initiallyImmediate switch
            {
                DeferrableInitial.Immediate => "INITIALLY IMMEDIATE",
                DeferrableInitial.Deferred => "INITIALLY DEFERRED",
                _ => throw new InvalidOperationException($"Variante de DeferrableInitial não tratada: {initiallyImmediate}"),
            }
            : null;

    private string? EnforcedText() =>
        Enforced is { } enforced
            ? (enforced ? "ENFORCED" : "NOT ENFORCED")
            : null;

    public void Fmt(Formatter f)
    {
        var deferrable = DeferrableText();
        var initiallyImmediate = InitiallyImmediateText();
        var enforced = EnforcedText();

        // match (deferrable, initially_immediate, enforced): os nomes das ligações do Rust repetiriam os
        // das variáveis acima (sombreamento proibido em C#), por isso o sufixo `Text`.
        switch (deferrable, initiallyImmediate, enforced)
        {
            case (null, null, null):
                break;
            case (null, null, { } enforcedText):
                f.Write($"{enforcedText}");
                break;
            case (null, { } initial, null):
                f.Write($"{initial}");
                break;
            case (null, { } initial, { } enforcedText):
                f.Write($"{initial} {enforcedText}");
                break;
            case ({ } deferrableText, null, null):
                f.Write($"{deferrableText}");
                break;
            case ({ } deferrableText, null, { } enforcedText):
                f.Write($"{deferrableText} {enforcedText}");
                break;
            case ({ } deferrableText, { } initial, null):
                f.Write($"{deferrableText} {initial}");
                break;
            case ({ } deferrableText, { } initial, { } enforcedText):
                f.Write($"{deferrableText} {initial} {enforcedText}");
                break;
            default:
                // Inalcançável (os 8 braços cobrem todas as combinações), mas mantém a regra da §6.3.
                throw new InvalidOperationException("Combinação de ConstraintCharacteristics não tratada");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
