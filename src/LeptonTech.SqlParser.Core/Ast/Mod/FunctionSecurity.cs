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

/// <summary>Security attribute for functions: SECURITY DEFINER or SECURITY INVOKER.</summary>
/// <remarks><see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see></remarks>
public abstract partial record FunctionSecurity : IDisplay
{
    /// <summary>Execute the function with the privileges of the user who defined it.</summary>
    public sealed record Definer : FunctionSecurity;

    /// <summary>Execute the function with the privileges of the user who invokes it.</summary>
    public sealed record Invoker : FunctionSecurity;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Definer:
                f.Write($"SECURITY DEFINER");
                break;
            case Invoker:
                f.Write($"SECURITY INVOKER");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionSecurity não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
