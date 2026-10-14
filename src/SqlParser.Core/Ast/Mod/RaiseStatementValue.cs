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

/// <summary>Represents the error value of a <c>RaiseStatement</c>.</summary>
/// <remarks>
/// A variante <c>Expr</c> esconde o tipo de topo <c>Expr</c> dentro deste record: o tipo dos campos é
/// qualificado como <c>Ast.Expr</c> (convencoes.md §13, caso 38).
/// </remarks>
public abstract partial record RaiseStatementValue : IDisplay
{
    /// <summary><c>RAISE USING MESSAGE = 'error'</c></summary>
    /// <param name="Value">Rust: campo <c>.0</c>.</param>
    public sealed record UsingMessage(Ast.Expr Value) : RaiseStatementValue;

    /// <summary><c>RAISE myerror</c></summary>
    /// <param name="Value">Rust: campo <c>.0</c>.</param>
    public sealed record Expr(Ast.Expr Value) : RaiseStatementValue;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Expr expr:
                f.Write($"{expr.Value}");
                break;
            case UsingMessage expr:
                f.Write($"USING MESSAGE = {expr.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de RaiseStatementValue não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
