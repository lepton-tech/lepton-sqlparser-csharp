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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// An element of a <c>Expr::CompoundFieldAccess</c>.
/// It can be an expression or a subscript.
/// </summary>
public abstract partial record AccessExpr : IDisplay
{
    /// <summary>Accesses a field using dot notation, e.g. <c>foo.bar.baz</c>.</summary>
    // Value: Rust: campo `.0`.
    public sealed record Dot(Expr Value) : AccessExpr;

    /// <summary>Accesses a field or array element using bracket notation, e.g. <c>foo['bar']</c>.</summary>
    // Value: Rust: campo `.0` (tipo qualificado: a variante tem o nome do tipo, convencoes.md §13, caso 38).
    public sealed record Subscript(Ast.Subscript Value) : AccessExpr;

    // Rust: `const fn is_number_expr` fica entre `pub enum AccessExpr` e este `impl Display`;
    // no C#, está em AstModule.cs (convencoes.md §13, caso 135).
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Dot { Value: var expr } when IsNumberExpr(expr):
                f.Write($" . {expr}");
                break;
            case Dot dot:
                f.Write($".{dot.Value}");
                break;
            case Subscript subscript:
                f.Write($"[{subscript.Value}]");
                break;
            default:
                throw new InvalidOperationException($"Variante de AccessExpr não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
