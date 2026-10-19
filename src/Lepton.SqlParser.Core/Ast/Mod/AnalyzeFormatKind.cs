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

/// <summary>How the <c>ANALYZE</c>/<c>EXPLAIN ANALYZE</c> format is specified.</summary>
public abstract partial record AnalyzeFormatKind : IDisplay
{
    /// <summary>Format provided as a keyword, e.g. <c>FORMAT JSON</c>.</summary>
    public sealed record Keyword(AnalyzeFormat Value) : AnalyzeFormatKind;

    /// <summary>Format provided as an assignment, e.g. <c>FORMAT=JSON</c>.</summary>
    public sealed record Assignment(AnalyzeFormat Value) : AnalyzeFormatKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Keyword format:
                f.Write($"FORMAT {format.Value}");
                break;
            case Assignment format:
                f.Write($"FORMAT={format.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de AnalyzeFormatKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
