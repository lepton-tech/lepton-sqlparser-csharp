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

/// <summary>The mode of an argument in CREATE FUNCTION.</summary>
public abstract partial record ArgMode : IDisplay
{
    /// <summary><c>IN</c> mode.</summary>
    public sealed record In : ArgMode;

    /// <summary><c>OUT</c> mode.</summary>
    public sealed record Out : ArgMode;

    /// <summary><c>INOUT</c> mode.</summary>
    public sealed record InOut : ArgMode;

    /// <summary><c>VARIADIC</c> mode.</summary>
    public sealed record Variadic : ArgMode;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case In:
                f.Write($"IN");
                break;
            case Out:
                f.Write($"OUT");
                break;
            case InOut:
                f.Write($"INOUT");
                break;
            case Variadic:
                f.Write($"VARIADIC");
                break;
            default:
                throw new InvalidOperationException($"Variante de ArgMode não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
