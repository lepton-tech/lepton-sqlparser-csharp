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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Represents an expression assignment within a variable <c>DECLARE</c> statement.</summary>
/// <remarks>
/// Examples:
/// <code>
/// DECLARE variable_name := 42
/// DECLARE variable_name DEFAULT 42
/// </code>
/// </remarks>
// A variante `Expr` esconde o tipo de topo `Expr` dentro deste record: o tipo dos campos é
// qualificado como `Ast.Expr` (convencoes.md §13, caso 38).
public abstract partial record DeclareAssignment : IDisplay
{
    /// <summary>Plain expression specified.</summary>
    // Value: Rust: campo `.0`.
    public sealed record Expr(Ast.Expr Value) : DeclareAssignment;

    /// <summary>Expression assigned via the <c>DEFAULT</c> keyword</summary>
    // Value: Rust: campo `.0`.
    public sealed record Default(Ast.Expr Value) : DeclareAssignment;

    /// <summary>
    /// Expression assigned via the <c>:=</c> syntax
    /// <para>Example:</para>
    /// <code>
    /// DECLARE variable_name := 42;
    /// </code>
    /// </summary>
    // Value: Rust: campo `.0`.
    public sealed record DuckAssignment(Ast.Expr Value) : DeclareAssignment;

    /// <summary>
    /// Expression via the <c>FOR</c> keyword
    /// <para>Example:</para>
    /// <code>
    /// DECLARE c1 CURSOR FOR res
    /// </code>
    /// </summary>
    // Value: Rust: campo `.0`.
    public sealed record For(Ast.Expr Value) : DeclareAssignment;

    /// <summary>
    /// Expression via the <c>=</c> syntax.
    /// <para>Example:</para>
    /// <code>
    /// DECLARE @variable AS INT = 100
    /// </code>
    /// </summary>
    // Value: Rust: campo `.0`.
    public sealed record MsSqlAssignment(Ast.Expr Value) : DeclareAssignment;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Expr expr:
                f.Write($"{expr.Value}");
                break;
            case Default expr:
                f.Write($"DEFAULT {expr.Value}");
                break;
            case DuckAssignment expr:
                f.Write($":= {expr.Value}");
                break;
            case MsSqlAssignment expr:
                f.Write($"= {expr.Value}");
                break;
            case For expr:
                f.Write($"FOR {expr.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de DeclareAssignment não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
