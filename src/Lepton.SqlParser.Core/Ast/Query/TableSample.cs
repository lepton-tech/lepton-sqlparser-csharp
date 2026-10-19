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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Represents a <c>TABLESAMPLE</c> clause and its options.</summary>
/// <param name="Modifier">Modifier (e.g. <c>SAMPLE</c> or <c>TABLESAMPLE</c>).</param>
/// <param name="Name">Optional sampling method name (e.g. <c>BERNOULLI</c>, <c>SYSTEM</c>).</param>
/// <param name="Quantity">Optional sampling quantity (value and optional unit).</param>
/// <param name="Seed">Optional seed clause.</param>
/// <param name="Bucket">Optional bucket specification for <c>BUCKET ... OUT OF ...</c>-style sampling.</param>
/// <param name="Offset">Optional offset expression for sampling.</param>
public sealed partial record TableSample(
    TableSampleModifier Modifier,
    TableSampleMethod? Name,
    TableSampleQuantity? Quantity,
    TableSampleSeed? Seed,
    TableSampleBucket? Bucket,
    Expr? Offset) : IDisplay
{
    // Rust: o `impl fmt::Display for TableSample` vem depois de `TableSampleBucket` no arquivo.
    public void Fmt(Formatter f)
    {
        f.Write($"{Modifier}");
        if (Name is { } name)
        {
            f.Write($" {name}");
        }
        if (Quantity is { } quantity)
        {
            f.Write($" {quantity}");
        }
        if (Seed is { } seed)
        {
            f.Write($" {seed}");
        }
        if (Bucket is { } bucket)
        {
            f.Write($" ({bucket})");
        }
        if (Offset is { } offset)
        {
            f.Write($" OFFSET {offset}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
