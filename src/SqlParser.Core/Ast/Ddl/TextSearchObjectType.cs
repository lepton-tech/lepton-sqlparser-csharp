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

namespace SqlParser.Core.Ast;

/// <summary>
/// Text search object kind.
/// <para>See <see href="https://www.postgresql.org/docs/current/textsearch-intro.html">PostgreSQL</see>.</para>
/// </summary>
// Enum sem dados com Display → records (convencoes.md §13, caso 37).
public abstract record TextSearchObjectType : IDisplay
{
    /// <summary><c>DICTIONARY</c></summary>
    public sealed record Dictionary : TextSearchObjectType;

    /// <summary><c>CONFIGURATION</c></summary>
    public sealed record Configuration : TextSearchObjectType;

    /// <summary><c>TEMPLATE</c></summary>
    public sealed record Template : TextSearchObjectType;

    /// <summary><c>PARSER</c></summary>
    public sealed record Parser : TextSearchObjectType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Dictionary:
                f.Write($"DICTIONARY");
                break;
            case Configuration:
                f.Write($"CONFIGURATION");
                break;
            case Template:
                f.Write($"TEMPLATE");
                break;
            case Parser:
                f.Write($"PARSER");
                break;
            default:
                throw new InvalidOperationException($"Variante de TextSearchObjectType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
