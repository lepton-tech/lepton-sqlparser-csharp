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

/// <summary>Forms of function arguments (named, expression-named, or positional).</summary>
public abstract partial record FunctionArg : IDisplay
{
    /// <summary>
    /// <c>name</c> is identifier
    /// <para>Enabled when <c>Dialect::supports_named_fn_args_with_expr_name</c> returns 'false'</para>
    /// </summary>
    /// <param name="Name">The identifier name of the argument.</param>
    /// <param name="Arg">The argument expression or wildcard form.</param>
    /// <param name="Operator">The operator separating name and value.</param>
    public sealed record Named(Ident Name, FunctionArgExpr Arg, FunctionArgOperator Operator) : FunctionArg;

    /// <summary>
    /// <c>name</c> is arbitrary expression
    /// <para>Enabled when <c>Dialect::supports_named_fn_args_with_expr_name</c> returns 'true'</para>
    /// </summary>
    /// <param name="Name">The expression used as the argument name.</param>
    /// <param name="Arg">The argument expression or wildcard form.</param>
    /// <param name="Operator">The operator separating name and value.</param>
    public sealed record ExprNamed(Expr Name, FunctionArgExpr Arg, FunctionArgOperator Operator) : FunctionArg;

    /// <summary>An unnamed argument (positional), given by expression or wildcard.</summary>
    public sealed record Unnamed(FunctionArgExpr Value) : FunctionArg;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Named named:
                FmtNamedFunctionArg(f, named.Name, named.Operator, named.Arg);
                break;
            case ExprNamed exprNamed:
                FmtNamedFunctionArg(f, exprNamed.Name, exprNamed.Operator, exprNamed.Arg);
                break;
            case Unnamed unnamedArg:
                f.Write($"{unnamedArg.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionArg não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
