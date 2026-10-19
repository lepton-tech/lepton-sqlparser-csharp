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

/// <summary>Optional type constraint for <c>IS JSON</c>.</summary>
public abstract partial record JsonPredicateType : IDisplay
{
    /// <summary><c>VALUE</c> form.</summary>
    public sealed record Value : JsonPredicateType;

    /// <summary><c>SCALAR</c> form.</summary>
    public sealed record Scalar : JsonPredicateType;

    /// <summary><c>ARRAY</c> form.</summary>
    public sealed record Array : JsonPredicateType;

    /// <summary><c>OBJECT</c> form.</summary>
    public sealed record Object : JsonPredicateType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Value:
                f.Write($"VALUE");
                break;
            case Scalar:
                f.Write($"SCALAR");
                break;
            case Array:
                f.Write($"ARRAY");
                break;
            case Object:
                f.Write($"OBJECT");
                break;
            default:
                throw new InvalidOperationException($"Variante de JsonPredicateType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
