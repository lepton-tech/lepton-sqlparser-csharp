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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// Column definition specified in a <c>CREATE VIEW</c> statement.
/// <para>Syntax</para>
/// <code>
/// &lt;name&gt; [data_type][OPTIONS(option, ...)]
///
/// option: &lt;name&gt; = &lt;value&gt;
/// </code>
/// <para>Examples:</para>
/// <code>
/// name
/// age OPTIONS(description = "age column", tag = "prod")
/// amount COMMENT 'The total amount for the order line'
/// created_at DateTime64
/// </code>
/// </summary>
/// <param name="Name">Column identifier.</param>
/// <param name="DataType">Optional data type for the column.</param>
/// <param name="Options">Optional column options (defaults, comments, etc.).</param>
public sealed partial record ViewColumnDef(Ident Name, DataType? DataType, ColumnOptions? Options) : IDisplay
{
    // Rust: impl fmt::Display for ViewColumnDef (declarado depois de ColumnOptions em ddl.rs).
    public void Fmt(Formatter f)
    {
        f.Write($"{Name}");
        if (DataType is { } dataType)
        {
            f.Write($" {dataType}");
        }
        if (Options is { } options)
        {
            switch (options)
            {
                case ColumnOptions.CommaSeparated { Value: var columnOptions }:
                    f.Write($" {DisplayCommaSeparated(columnOptions)}");
                    break;
                case ColumnOptions.SpaceSeparated { Value: var columnOptions }:
                    f.Write($" {DisplaySeparated(columnOptions, " ")}");
                    break;
                default:
                    throw new InvalidOperationException($"Variante de ColumnOptions não tratada: {options.GetType().Name}");
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
