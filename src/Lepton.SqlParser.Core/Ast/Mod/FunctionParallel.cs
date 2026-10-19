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

/// <summary>If it is safe for PostgreSQL to call the function from multiple threads at once</summary>
public abstract partial record FunctionParallel : IDisplay
{
    /// <summary>The function is not safe to run in parallel.</summary>
    public sealed record Unsafe : FunctionParallel;

    /// <summary>The function is restricted for parallel execution.</summary>
    public sealed record Restricted : FunctionParallel;

    /// <summary>The function is safe to run in parallel.</summary>
    public sealed record Safe : FunctionParallel;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Unsafe:
                f.Write($"PARALLEL UNSAFE");
                break;
            case Restricted:
                f.Write($"PARALLEL RESTRICTED");
                break;
            case Safe:
                f.Write($"PARALLEL SAFE");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionParallel não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
