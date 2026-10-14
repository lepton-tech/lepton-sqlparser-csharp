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

/// <summary>Value for a session boolean-like parameter (ON/OFF).</summary>
public abstract record SessionParamValue : IDisplay
{
    /// <summary>Session parameter enabled.</summary>
    public sealed record On : SessionParamValue;

    /// <summary>Session parameter disabled.</summary>
    public sealed record Off : SessionParamValue;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case On:
                f.Write($"ON");
                break;
            case Off:
                f.Write($"OFF");
                break;
            default:
                throw new InvalidOperationException($"Variante de SessionParamValue não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
