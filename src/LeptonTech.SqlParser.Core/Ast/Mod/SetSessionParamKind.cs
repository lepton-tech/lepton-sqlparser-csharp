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

/// <summary>Kind of session parameter being set by <c>SET SESSION</c>.</summary>
public abstract partial record SetSessionParamKind : IDisplay
{
    /// <summary>Generic session parameter (name/value pair).</summary>
    public sealed record Generic(SetSessionParamGeneric Value) : SetSessionParamKind;

    /// <summary>Identity insert related parameter.</summary>
    public sealed record IdentityInsert(SetSessionParamIdentityInsert Value) : SetSessionParamKind;

    /// <summary>Offsets-related parameter.</summary>
    public sealed record Offsets(SetSessionParamOffsets Value) : SetSessionParamKind;

    /// <summary>Statistics-related parameter.</summary>
    public sealed record Statistics(SetSessionParamStatistics Value) : SetSessionParamKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Generic x:
                f.Write($"{x.Value}");
                break;
            case IdentityInsert x:
                f.Write($"{x.Value}");
                break;
            case Offsets x:
                f.Write($"{x.Value}");
                break;
            case Statistics x:
                f.Write($"{x.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de SetSessionParamKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
