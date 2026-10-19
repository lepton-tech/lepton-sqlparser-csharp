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

/// <summary>A lambda function.</summary>
/// <param name="Params">The parameters to the lambda function.</param>
/// <param name="Body">The body of the lambda function.</param>
/// <param name="Syntax">The syntax style used to write the lambda function.</param>
public sealed partial record LambdaFunction(OneOrManyWithParens<LambdaFunctionParameter> Params, Expr Body, LambdaSyntax Syntax) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (Syntax)
        {
            case LambdaSyntax.Arrow:
                f.Write($"{Params} -> {Body}");
                break;
            case LambdaSyntax.LambdaKeyword:
            {
                // For lambda keyword syntax, display params without parentheses
                // e.g., `lambda x, y : expr` not `lambda (x, y) : expr`
                f.Write($"lambda ");
                switch (Params)
                {
                    case OneOrManyWithParens<LambdaFunctionParameter>.One { Value: var p }:
                        f.Write($"{p}");
                        break;
                    case OneOrManyWithParens<LambdaFunctionParameter>.Many { Value: var ps }:
                        f.Write($"{DisplayCommaSeparated(ps)}");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de OneOrManyWithParens não tratada: {Params.GetType().Name}");
                }
                f.Write($" : {Body}");
                break;
            }
            default:
                throw new InvalidOperationException($"Valor de LambdaSyntax não tratado: {Syntax}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
