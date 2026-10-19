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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>Snowflake <c>EXCLUDE</c> information.</summary>
/// <remarks>
/// Syntax:
/// <code>
/// &lt;col_name&gt;
/// | (&lt;col_name&gt;, &lt;col_name&gt;, ...)
/// </code>
/// </remarks>
public abstract partial record ExcludeSelectItem : IDisplay
{
    /// <summary>
    /// Single column name without parenthesis.
    /// <para>Syntax:</para>
    /// <code>
    /// &lt;col_name&gt;
    /// </code>
    /// </summary>
    public sealed record Single(ObjectName Value) : ExcludeSelectItem;

    /// <summary>
    /// Multiple column names inside parenthesis.
    /// <para>Syntax:</para>
    /// <code>
    /// (&lt;col_name&gt;, &lt;col_name&gt;, ...)
    /// </code>
    /// </summary>
    public sealed record Multiple(Sequence<ObjectName> Value) : ExcludeSelectItem;

    public void Fmt(Formatter f)
    {
        f.Write($"EXCLUDE");
        switch (this)
        {
            case Single { Value: var column }:
                f.Write($" {column}");
                break;
            case Multiple { Value: var columns }:
                f.Write($" ({DisplayCommaSeparated(columns)})");
                break;
            default:
                throw new InvalidOperationException($"Variante de ExcludeSelectItem não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
