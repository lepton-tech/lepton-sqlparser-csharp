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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// A value in a partition bound specification.
/// <para>
/// Used in RANGE partition bounds where values can be expressions,
/// MINVALUE (negative infinity), or MAXVALUE (positive infinity).
/// </para>
/// </summary>
public abstract partial record PartitionBoundValue : IDisplay
{
    /// <summary>An expression representing a partition bound value.</summary>
    // Variante tupla cujo tipo tem o mesmo nome: tipo qualificado (convencoes.md §6 e §13, caso 38).
    public sealed record Expr(Ast.Expr Value) : PartitionBoundValue;

    /// <summary>Represents negative infinity in partition bounds.</summary>
    public sealed record MinValue : PartitionBoundValue;

    /// <summary>Represents positive infinity in partition bounds.</summary>
    public sealed record MaxValue : PartitionBoundValue;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Expr { Value: var expr }:
                f.Write($"{expr}");
                break;
            case MinValue:
                f.Write($"MINVALUE");
                break;
            case MaxValue:
                f.Write($"MAXVALUE");
                break;
            default:
                throw new InvalidOperationException($"Variante de PartitionBoundValue não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
