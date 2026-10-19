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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary><c>ALTER FUNCTION</c> / <c>ALTER AGGREGATE</c> statement.</summary>
/// <param name="Kind">Object type being altered.</param>
/// <param name="Function">Function or aggregate signature.</param>
/// <param name="AggregateOrderBy">
/// <c>ORDER BY</c> argument list for aggregate signatures.
/// <para>This is only used for <c>ALTER AGGREGATE</c>.</para>
/// </param>
/// <param name="AggregateStar">
/// Whether the aggregate signature uses <c>*</c>.
/// <para>This is only used for <c>ALTER AGGREGATE</c>.</para>
/// </param>
/// <param name="Operation">Operation applied to the object.</param>
public sealed partial record AlterFunction(
    AlterFunctionKind Kind,
    FunctionDesc Function,
    Sequence<OperateFunctionArg>? AggregateOrderBy,
    bool AggregateStar,
    AlterFunctionOperation Operation) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        f.Write($"ALTER {Kind} ");
        switch (Kind)
        {
            case AlterFunctionKind.Function:
            {
                f.Write($"{Function} ");
                break;
            }
            case AlterFunctionKind.Aggregate:
            {
                f.Write($"{Function.Name}(");
                if (AggregateStar)
                {
                    f.Write($"*");
                }
                else
                {
                    if (Function.Args is { } args)
                    {
                        f.Write($"{DisplayCommaSeparated(args)}");
                    }
                    if (AggregateOrderBy is { } orderByArgs)
                    {
                        if (Function.Args is { } functionArgs && !functionArgs.IsEmpty)
                        {
                            f.Write($" ");
                        }
                        f.Write($"ORDER BY {DisplayCommaSeparated(orderByArgs)}");
                    }
                }
                f.Write($") ");
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de AlterFunctionKind não tratada: {Kind.GetType().Name}");
        }
        f.Write($"{Operation}");
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for AlterFunction (ddl.rs:5647)
    public Span Span() => Core.Span.Empty();
}
