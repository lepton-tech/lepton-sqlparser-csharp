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

/// <summary>An expression used in a named window declaration.</summary>
/// <remarks>
/// <code>
/// WINDOW mywindow AS [named_window_expr]
/// </code>
/// </remarks>
public abstract record NamedWindowExpr : IDisplay
{
    /// <summary>
    /// A direct reference to another named window definition.
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/window-function-calls#ref_named_window">BigQuery</see>
    /// <para>Example:</para>
    /// <code>
    /// WINDOW mywindow AS prev_window
    /// </code>
    /// </summary>
    public sealed record NamedWindow(Ident Value) : NamedWindowExpr;

    /// <summary>
    /// A window expression.
    /// <para>Example:</para>
    /// <code>
    /// WINDOW mywindow AS (ORDER BY 1)
    /// </code>
    /// </summary>
    public sealed record WindowSpec(Ast.WindowSpec Value) : NamedWindowExpr;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case NamedWindow { Value: var namedWindow }:
                f.Write($"{namedWindow}");
                break;
            case WindowSpec { Value: var windowSpec }:
                f.Write($"({windowSpec})");
                break;
            default:
                throw new InvalidOperationException($"Variante de NamedWindowExpr não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
