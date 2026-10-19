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

/// <summary>These attributes describe the behavior of the function when called with a null argument.</summary>
public abstract partial record FunctionCalledOnNull : IDisplay
{
    /// <summary>Function is called even when inputs are null.</summary>
    public sealed record CalledOnNullInput : FunctionCalledOnNull;

    /// <summary>Function returns null when any input is null.</summary>
    public sealed record ReturnsNullOnNullInput : FunctionCalledOnNull;

    /// <summary>Function is strict about null inputs.</summary>
    public sealed record Strict : FunctionCalledOnNull;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case CalledOnNullInput:
                f.Write($"CALLED ON NULL INPUT");
                break;
            case ReturnsNullOnNullInput:
                f.Write($"RETURNS NULL ON NULL INPUT");
                break;
            case Strict:
                f.Write($"STRICT");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionCalledOnNull não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
