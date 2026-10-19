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

/// <summary>Output formats supported for <c>ANALYZE</c>/<c>EXPLAIN ANALYZE</c>.</summary>
public abstract partial record AnalyzeFormat : IDisplay
{
    /// <summary>Plain text format.</summary>
    public sealed record TEXT : AnalyzeFormat;

    /// <summary>Graphviz DOT format.</summary>
    public sealed record GRAPHVIZ : AnalyzeFormat;

    /// <summary>JSON format.</summary>
    public sealed record JSON : AnalyzeFormat;

    /// <summary>Traditional explain output.</summary>
    public sealed record TRADITIONAL : AnalyzeFormat;

    /// <summary>Tree-style explain output.</summary>
    public sealed record TREE : AnalyzeFormat;

    // No Rust, `impl fmt::Display for AnalyzeFormat` fica depois de `JsonKeyUniqueness` (mod.rs:8531);
    // aqui fica no arquivo do tipo (um tipo de topo por arquivo).
    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            TEXT => "TEXT",
            GRAPHVIZ => "GRAPHVIZ",
            JSON => "JSON",
            TRADITIONAL => "TRADITIONAL",
            TREE => "TREE",
            _ => throw new InvalidOperationException($"Variante de AnalyzeFormat não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
