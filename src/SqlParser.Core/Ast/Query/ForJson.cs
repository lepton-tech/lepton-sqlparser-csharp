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

namespace SqlParser.Core.Ast;

/// <summary>Modes for <c>FOR JSON</c> clause.</summary>
/// <remarks>Enum sem dados com <c>Display</c> → records (convencoes.md §13, caso 37).</remarks>
public abstract partial record ForJson : IDisplay
{
    /// <summary><c>AUTO</c> mode.</summary>
    public sealed record Auto : ForJson;

    /// <summary><c>PATH</c> mode.</summary>
    public sealed record Path : ForJson;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Auto:
                f.Write($"AUTO");
                break;
            case Path:
                f.Write($"PATH");
                break;
            default:
                throw new InvalidOperationException($"Variante de ForJson não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
