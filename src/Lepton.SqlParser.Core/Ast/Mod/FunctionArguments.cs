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

/// <summary>The arguments passed to a function call.</summary>
public abstract partial record FunctionArguments : IDisplay
{
    /// <summary>
    /// Used for special functions like <c>CURRENT_TIMESTAMP</c> that are invoked
    /// without parentheses.
    /// </summary>
    public sealed record None : FunctionArguments;

    /// <summary>
    /// On some dialects, a subquery can be passed without surrounding
    /// parentheses if it's the sole argument to the function.
    /// </summary>
    public sealed record Subquery(Query Value) : FunctionArguments;

    /// <summary>
    /// A normal function argument list, including any clauses within it such as
    /// <c>DISTINCT</c> or <c>ORDER BY</c>.
    /// </summary>
    public sealed record List(FunctionArgumentList Value) : FunctionArguments;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case None:
                break;
            case Subquery query:
                f.Write($"({query.Value})");
                break;
            case List args:
                f.Write($"({args.Value})");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionArguments não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
