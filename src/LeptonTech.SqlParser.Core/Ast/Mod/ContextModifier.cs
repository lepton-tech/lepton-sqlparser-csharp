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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Optional context modifier for statements that can be or <c>LOCAL</c>, <c>GLOBAL</c>, or <c>SESSION</c>.</summary>
public abstract partial record ContextModifier : IDisplay
{
    /// <summary><c>LOCAL</c> identifier, usually related to transactional states.</summary>
    public sealed record Local : ContextModifier;

    /// <summary><c>SESSION</c> identifier</summary>
    public sealed record Session : ContextModifier;

    /// <summary><c>GLOBAL</c> identifier</summary>
    public sealed record Global : ContextModifier;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Local:
                f.Write($"LOCAL ");
                break;
            case Session:
                f.Write($"SESSION ");
                break;
            case Global:
                f.Write($"GLOBAL ");
                break;
            default:
                throw new InvalidOperationException($"Variante de ContextModifier não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
