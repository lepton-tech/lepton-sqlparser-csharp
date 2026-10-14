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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dml.rs.
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>The kind of update used within a <c>MERGE</c> statement.</summary>
public abstract record MergeUpdateKind : IDisplay
{
    /// <summary>Standard update with explicit assignments.</summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// UPDATE SET quantity = source.quantity, name = source.name
    /// </code>
    /// </remarks>
    public sealed record Set(Sequence<Assignment> Value) : MergeUpdateKind;

    /// <summary>The <c>*</c> wildcard to update all columns from the source.</summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// UPDATE SET *
    /// </code>
    /// <see href="https://docs.databricks.com/en/sql/language-manual/delta-merge-into.html">Databricks</see>
    /// </remarks>
    public sealed record Wildcard : MergeUpdateKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Set assignments:
                f.Write($"SET {DisplayCommaSeparated(assignments.Value)}");
                break;
            case Wildcard:
                f.Write($"SET *");
                break;
            default:
                throw new InvalidOperationException($"Variante de MergeUpdateKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
