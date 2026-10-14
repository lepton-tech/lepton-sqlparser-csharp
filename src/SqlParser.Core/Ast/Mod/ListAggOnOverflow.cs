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

/// <summary>The <c>ON OVERFLOW</c> clause of a LISTAGG invocation</summary>
public abstract record ListAggOnOverflow : IDisplay
{
    /// <summary><c>ON OVERFLOW ERROR</c></summary>
    public sealed record Error : ListAggOnOverflow;

    /// <summary><c>ON OVERFLOW TRUNCATE [ &lt;filler&gt; ] WITH[OUT] COUNT</c></summary>
    /// <param name="Filler">Optional filler expression used when truncating.</param>
    /// <param name="WithCount">Whether to include a count when truncating.</param>
    public sealed record Truncate(Expr? Filler, bool WithCount) : ListAggOnOverflow;

    public void Fmt(Formatter f)
    {
        f.Write($"ON OVERFLOW");
        switch (this)
        {
            case Error:
                f.Write($" ERROR");
                break;
            case Truncate truncate:
                f.Write($" TRUNCATE");
                if (truncate.Filler is { } filler)
                {
                    f.Write($" {filler}");
                }
                if (truncate.WithCount)
                {
                    f.Write($" WITH");
                }
                else
                {
                    f.Write($" WITHOUT");
                }
                f.Write($" COUNT");
                break;
            default:
                throw new InvalidOperationException($"Variante de ListAggOnOverflow não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
