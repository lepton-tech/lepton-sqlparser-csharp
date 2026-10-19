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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>A <c>FROM</c> clause within a <c>DELETE</c> statement.</summary>
/// <remarks>
/// Syntax
/// <code>
/// [FROM] table
/// </code>
/// </remarks>
public abstract partial record FromTable : IDisplay
{
    /// <summary>An explicit <c>FROM</c> keyword was specified.</summary>
    // Value: Rust: campo `.0`.
    public sealed record WithFromKeyword(Sequence<TableWithJoins> Value) : FromTable;

    /// <summary>
    /// BigQuery: <c>FROM</c> keyword was omitted.
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dml-syntax#delete_statement"/>
    /// </summary>
    // Value: Rust: campo `.0`.
    public sealed record WithoutKeyword(Sequence<TableWithJoins> Value) : FromTable;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case WithFromKeyword tables:
                f.Write($"FROM {DisplayCommaSeparated(tables.Value)}");
                break;
            case WithoutKeyword tables:
                f.Write($"{DisplayCommaSeparated(tables.Value)}");
                break;
            default:
                throw new InvalidOperationException($"Variante de FromTable não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
