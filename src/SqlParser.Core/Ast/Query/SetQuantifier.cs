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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>A quantifier for <c>SetOperator</c>.</summary>
// Enum sem dados com `Display` → records (convencoes.md §13, caso 37).
// TODO: Restrict parsing specific SetQuantifier in some specific dialects.
// For example, BigQuery does not support `DISTINCT` for `EXCEPT` and `INTERSECT`
public abstract partial record SetQuantifier : IDisplay
{
    /// <summary><c>ALL</c> quantifier</summary>
    public sealed record All : SetQuantifier;

    /// <summary><c>DISTINCT</c> quantifier</summary>
    public sealed record Distinct : SetQuantifier;

    /// <summary><c>BY NAME</c> quantifier</summary>
    public sealed record ByName : SetQuantifier;

    /// <summary><c>ALL BY NAME</c> quantifier</summary>
    public sealed record AllByName : SetQuantifier;

    /// <summary><c>DISTINCT BY NAME</c> quantifier</summary>
    public sealed record DistinctByName : SetQuantifier;

    /// <summary>No quantifier specified</summary>
    public sealed record None : SetQuantifier;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case All:
                f.Write($"ALL");
                break;
            case Distinct:
                f.Write($"DISTINCT");
                break;
            case ByName:
                f.Write($"BY NAME");
                break;
            case AllByName:
                f.Write($"ALL BY NAME");
                break;
            case DistinctByName:
                f.Write($"DISTINCT BY NAME");
                break;
            case None:
                break;
            default:
                throw new InvalidOperationException($"Variante de SetQuantifier não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
