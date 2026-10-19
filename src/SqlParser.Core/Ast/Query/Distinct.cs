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

/// <summary><c>ALL</c>, <c>DISTINCT</c>, or <c>DISTINCT ON (...)</c> modifiers for <c>SELECT</c> lists.</summary>
public abstract partial record Distinct : IDisplay
{
    /// <summary>
    /// <c>ALL</c> (keep duplicate rows)
    /// <para>
    /// Generally this is the default if omitted, but omission should be represented as
    /// <c>None::&lt;Option&lt;Distinct&gt;&gt;</c>
    /// </para>
    /// </summary>
    public sealed record All : Distinct;

    /// <summary>
    /// <c>DISTINCT</c> (remove duplicate rows)
    /// </summary>
    // Rust: variante `Distinct::Distinct`. Renomeada com o sufixo `Value` por CS0542 (um tipo
    // aninhado não pode ter o nome do tipo que o contém), por analogia ao caso 41 (A7.md, "Nomes").
    public sealed record DistinctValue : Distinct;

    /// <summary><c>DISTINCT ON (...)</c> (Postgres extension)</summary>
    public sealed record On(Sequence<Expr> Value) : Distinct;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case All:
                f.Write($"ALL");
                break;
            case DistinctValue:
                f.Write($"DISTINCT");
                break;
            case On { Value: var colNames }:
            {
                var colNamesDisplay = DisplayCommaSeparated(colNames);
                f.Write($"DISTINCT ON ({colNamesDisplay})");
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de Distinct não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
