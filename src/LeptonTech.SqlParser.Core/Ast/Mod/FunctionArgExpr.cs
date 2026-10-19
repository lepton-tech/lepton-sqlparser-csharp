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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Expression forms allowed as a function argument.</summary>
// A variante aninhada `Expr` esconde o tipo de topo `Expr` dentro deste record: o tipo
// de topo é qualificado como `Ast.Expr` (convencoes.md §13, caso 38).
public abstract partial record FunctionArgExpr : IDisplay
{
    /// <summary>A normal expression argument.</summary>
    public sealed record Expr(Ast.Expr Value) : FunctionArgExpr;

    /// <summary>Qualified wildcard, e.g. <c>alias.*</c> or <c>schema.table.*</c>.</summary>
    public sealed record QualifiedWildcard(ObjectName Value) : FunctionArgExpr;

    /// <summary>An unqualified <c>*</c> wildcard.</summary>
    public sealed record Wildcard : FunctionArgExpr;

    /// <summary>
    /// An unqualified <c>*</c> wildcard with additional options, e.g. <c>* EXCLUDE(col)</c>.
    /// <para>Used in Snowflake to support expressions like <c>HASH(* EXCLUDE(col))</c>.</para>
    /// </summary>
    public sealed record WildcardWithOptions(WildcardAdditionalOptions Value) : FunctionArgExpr;

    // Rust: `impl From<Expr> for FunctionArgExpr`.
    public static FunctionArgExpr From(Ast.Expr wildcardExpr) =>
        wildcardExpr switch
        {
            Ast.Expr.QualifiedWildcard(var prefix, _) => new QualifiedWildcard(prefix),
            Ast.Expr.Wildcard => new Wildcard(),
            var expr => new Expr(expr),
        };

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Expr expr:
                f.Write($"{expr.Value}");
                break;
            case QualifiedWildcard prefix:
                f.Write($"{prefix.Value}.*");
                break;
            case Wildcard:
                f.WriteStr("*");
                break;
            case WildcardWithOptions opts:
                f.Write($"*{opts.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionArgExpr não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
