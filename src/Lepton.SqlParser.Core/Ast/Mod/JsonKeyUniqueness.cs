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

/// <summary>Optional duplicate-key handling for <c>IS JSON</c>.</summary>
public abstract partial record JsonKeyUniqueness : IDisplay
{
    /// <summary><c>WITH UNIQUE KEYS</c> form.</summary>
    public sealed record WithUniqueKeys : JsonKeyUniqueness;

    /// <summary><c>WITHOUT UNIQUE KEYS</c> form.</summary>
    public sealed record WithoutUniqueKeys : JsonKeyUniqueness;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case WithUniqueKeys:
                f.Write($"WITH UNIQUE KEYS");
                break;
            case WithoutUniqueKeys:
                f.Write($"WITHOUT UNIQUE KEYS");
                break;
            default:
                throw new InvalidOperationException($"Variante de JsonKeyUniqueness não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
