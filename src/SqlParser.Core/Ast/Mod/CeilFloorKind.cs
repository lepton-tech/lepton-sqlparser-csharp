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

namespace SqlParser.Core.Ast;

/// <summary>The syntax used in a CEIL or FLOOR expression.</summary>
/// <remarks>
/// The <c>CEIL/FLOOR(&lt;datetime value expression&gt; TO &lt;time unit&gt;)</c> is an Amazon Kinesis Data Analytics extension.
/// See <see href="https://docs.aws.amazon.com/kinesisanalytics/latest/sqlref/sql-reference-ceil.html"/> for
/// details.
/// <para>
/// Other dialects either support <c>CEIL/FLOOR( &lt;expr&gt; [, &lt;scale&gt;])</c> format or just
/// <c>CEIL/FLOOR(&lt;expr&gt;)</c>.
/// </para>
/// </remarks>
public abstract partial record CeilFloorKind
{
    /// <summary><c>CEIL( &lt;expr&gt; TO &lt;DateTimeField&gt;)</c></summary>
    // Value: Rust: campo `.0` (tipo qualificado: a variante tem o nome do tipo, convencoes.md §13, caso 38).
    public sealed record DateTimeField(Ast.DateTimeField Value) : CeilFloorKind;

    /// <summary><c>CEIL( &lt;expr&gt; [, &lt;scale&gt;])</c></summary>
    // Value: Rust: campo `.0`.
    public sealed record Scale(ValueWithSpan Value) : CeilFloorKind;
}
