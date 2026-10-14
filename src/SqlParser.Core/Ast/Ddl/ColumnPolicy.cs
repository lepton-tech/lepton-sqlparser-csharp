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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// Column policy that identify a security policy of access to a column.
/// <para>Syntax</para>
/// <code>
/// [ WITH ] MASKING POLICY &lt;policy_name&gt; [ USING ( &lt;col_name&gt; , &lt;cond_col1&gt; , ... ) ]
/// [ WITH ] PROJECTION POLICY &lt;policy_name&gt;
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
/// </summary>
public abstract partial record ColumnPolicy : IDisplay
{
    /// <summary><c>MASKING POLICY (&lt;property&gt;)</c></summary>
    public sealed record MaskingPolicy(ColumnPolicyProperty Value) : ColumnPolicy;

    /// <summary><c>PROJECTION POLICY (&lt;property&gt;)</c></summary>
    public sealed record ProjectionPolicy(ColumnPolicyProperty Value) : ColumnPolicy;

    public void Fmt(Formatter f)
    {
        var (command, property) = this switch
        {
            MaskingPolicy { Value: var maskingProperty } => ("MASKING POLICY", maskingProperty),
            ProjectionPolicy { Value: var projectionProperty } => ("PROJECTION POLICY", projectionProperty),
            _ => throw new InvalidOperationException($"Variante de ColumnPolicy não tratada: {GetType().Name}"),
        };
        if (property.With)
        {
            f.Write($"WITH ");
        }
        f.Write($"{command} {property.PolicyName}");
        if (property.UsingColumns is { } usingColumns)
        {
            f.Write($" USING ({DisplayCommaSeparated(usingColumns)})");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
