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

// Funções livres de src/ast/mod.rs, linhas 6522–9461 (convencoes.md §13, caso 29).
public static partial class AstModule
{
    /// <summary>
    /// Whether <c>expr</c> renders with an operator character first. A prefix <c>-</c> or <c>!</c>
    /// must not abut one, since <c>--</c> starts a line comment and compound tokens
    /// like <c>!!</c> or <c>!~</c> alter the parsed AST or fail to parse.
    /// </summary>
    // Rust: `fn starts_with_operator_char(mut expr: &Expr)` (privada do módulo → `internal`,
    // convencoes.md §13, caso 20); o parâmetro `mut` é reatribuído no laço, como no Rust.
    internal static bool StartsWithOperatorChar(Expr expr)
    {
        while (true)
        {
            switch (expr)
            {
                case Expr.UnaryOp { Op: var op }:
                    return op is not UnaryOperator.Not;
                case Expr.BinaryOp { Left: var left }:
                    expr = left;
                    break;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// <c>FunctionArgOperator::Space</c> has no token of its own, so the name and the
    /// value are separated by a single space instead.
    /// </summary>
    // Rust: `fn fmt_named_function_arg` (privada do módulo → `internal`, convencoes.md §13, caso 20).
    // O parâmetro `name: &impl fmt::Display` vira IDisplay.
    internal static void FmtNamedFunctionArg(
        Formatter f,
        IDisplay name,
        FunctionArgOperator @operator,
        FunctionArgExpr arg)
    {
        switch (@operator)
        {
            case FunctionArgOperator.Space:
                f.Write($"{name} {arg}");
                break;
            default:
                f.Write($"{name} {@operator} {arg}");
                break;
        }
    }
}
