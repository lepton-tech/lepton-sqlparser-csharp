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

/// <summary>Specific direction for FETCH statement</summary>
public abstract partial record FetchDirection : IDisplay
{
    /// <summary>Fetch a specific count of rows.</summary>
    /// <param name="Limit">The limit value for the count.</param>
    public sealed record Count(ValueWithSpan Limit) : FetchDirection;

    /// <summary>Fetch the next row.</summary>
    public sealed record Next : FetchDirection;

    /// <summary>Fetch the prior row.</summary>
    public sealed record Prior : FetchDirection;

    /// <summary>Fetch the first row.</summary>
    public sealed record First : FetchDirection;

    /// <summary>Fetch the last row.</summary>
    public sealed record Last : FetchDirection;

    /// <summary>Fetch an absolute row by index.</summary>
    /// <param name="Limit">The absolute index value.</param>
    public sealed record Absolute(ValueWithSpan Limit) : FetchDirection;

    /// <summary>Fetch a row relative to the current position.</summary>
    /// <param name="Limit">The relative offset value.</param>
    public sealed record Relative(ValueWithSpan Limit) : FetchDirection;

    /// <summary>Fetch all rows.</summary>
    public sealed record All : FetchDirection;

    // FORWARD
    // FORWARD count
    /// <summary>Fetch forward by an optional limit.</summary>
    /// <param name="Limit">Optional forward limit.</param>
    public sealed record Forward(ValueWithSpan? Limit) : FetchDirection;

    /// <summary>Fetch all forward rows.</summary>
    public sealed record ForwardAll : FetchDirection;

    // BACKWARD
    // BACKWARD count
    /// <summary>Fetch backward by an optional limit.</summary>
    /// <param name="Limit">Optional backward limit.</param>
    public sealed record Backward(ValueWithSpan? Limit) : FetchDirection;

    /// <summary>Fetch all backward rows.</summary>
    public sealed record BackwardAll : FetchDirection;

    public void Fmt(Formatter f)
    {
        // f.write_str(&limit.to_string()) → f.WriteStr(Formatter.Format(limit)): o Rust formata o valor
        // à parte (to_string) e escreve o texto pronto.
        switch (this)
        {
            case Count count:
                f.WriteStr(Formatter.Format(count.Limit));
                break;
            case Next:
                f.WriteStr("NEXT");
                break;
            case Prior:
                f.WriteStr("PRIOR");
                break;
            case First:
                f.WriteStr("FIRST");
                break;
            case Last:
                f.WriteStr("LAST");
                break;
            case Absolute absolute:
                f.WriteStr("ABSOLUTE ");
                f.WriteStr(Formatter.Format(absolute.Limit));
                break;
            case Relative relative:
                f.WriteStr("RELATIVE ");
                f.WriteStr(Formatter.Format(relative.Limit));
                break;
            case All:
                f.WriteStr("ALL");
                break;
            case Forward forward:
                f.WriteStr("FORWARD");

                if (forward.Limit is { } forwardLimit)
                {
                    f.WriteStr(" ");
                    f.WriteStr(Formatter.Format(forwardLimit));
                }
                break;
            case ForwardAll:
                f.WriteStr("FORWARD ALL");
                break;
            case Backward backward:
                f.WriteStr("BACKWARD");

                if (backward.Limit is { } backwardLimit)
                {
                    f.WriteStr(" ");
                    f.WriteStr(Formatter.Format(backwardLimit));
                }
                break;
            case BackwardAll:
                f.WriteStr("BACKWARD ALL");
                break;
            default:
                throw new InvalidOperationException($"Variante de FetchDirection não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
