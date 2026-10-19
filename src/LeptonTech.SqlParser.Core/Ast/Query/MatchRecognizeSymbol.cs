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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A symbol in a <c>MATCH_RECOGNIZE</c> pattern.</summary>
public abstract partial record MatchRecognizeSymbol : IDisplay
{
    /// <summary>A named symbol, e.g. <c>S1</c>.</summary>
    public sealed record Named(Ident Value) : MatchRecognizeSymbol;

    /// <summary>A virtual symbol representing the start of the of partition (<c>^</c>).</summary>
    public sealed record Start : MatchRecognizeSymbol;

    /// <summary>A virtual symbol representing the end of the partition (<c>$</c>).</summary>
    public sealed record End : MatchRecognizeSymbol;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Named { Value: var symbol }:
                f.Write($"{symbol}");
                break;
            case Start:
                f.Write($"^");
                break;
            case End:
                f.Write($"$");
                break;
            default:
                throw new InvalidOperationException($"Variante de MatchRecognizeSymbol não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
