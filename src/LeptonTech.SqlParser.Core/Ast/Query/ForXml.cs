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

/// <summary>Modes for <c>FOR XML</c> clause.</summary>
public abstract partial record ForXml : IDisplay
{
    /// <summary><c>RAW</c> mode with optional root name: <c>RAW('root')</c>.</summary>
    public sealed record Raw(string? Value) : ForXml;

    /// <summary><c>AUTO</c> mode.</summary>
    public sealed record Auto : ForXml;

    /// <summary><c>EXPLICIT</c> mode.</summary>
    public sealed record Explicit : ForXml;

    /// <summary><c>PATH</c> mode with optional root: <c>PATH('root')</c>.</summary>
    public sealed record Path(string? Value) : ForXml;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Raw { Value: var root }:
                f.Write($"RAW");
                if (root is { } rawRoot)
                {
                    f.Write($"('{rawRoot}')");
                }
                break;
            case Auto:
                f.Write($"AUTO");
                break;
            case Explicit:
                f.Write($"EXPLICIT");
                break;
            case Path { Value: var root }:
                f.Write($"PATH");
                if (root is { } pathRoot)
                {
                    f.Write($"('{pathRoot}')");
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de ForXml não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
