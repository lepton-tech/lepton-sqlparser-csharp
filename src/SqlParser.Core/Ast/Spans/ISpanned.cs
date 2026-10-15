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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/spans.rs (linhas 58–101: pub trait Spanned).
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// Trait for AST nodes that have a source location information.
/// </summary>
/// <remarks>
/// <para><b>Notes:</b></para>
/// <para>Source <see cref="Core.Span"/> are not yet complete. They may be missing:</para>
/// <list type="number">
/// <item>keywords or other tokens</item>
/// <item>span information entirely, in which case they return <see cref="Core.Span.Empty"/>.</item>
/// </list>
/// <para>
/// Note Some impl blocks (rendered below) are annotated with which nodes are
/// missing spans. See <see href="https://github.com/apache/datafusion-sqlparser-rs/issues/1548">this ticket</see>
/// for additional information and status.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var dialect = new GenericDialect();
/// var sql = "SELECT *\n  FROM table_1";
/// var statements = new Parser(dialect)
///   .TryWithSql(sql)
///   .ParseStatements();
/// // Get the span of the first statement (SELECT)
/// var span = statements[0].Span();
/// // statement starts at line 1, column 1 (1 based, not 0 based)
/// // span.Start == new Location(1, 1)
/// // statement ends on line 2, column 15
/// // span.End == new Location(2, 15)
/// </code>
/// </example>
// Rust: `pub trait Spanned` → `ISpanned` (prefixo `I`, como `IDisplay`, caso 4).
// Cada tipo implementa `public Span Span()`; os tipos que já têm a propriedade `Span`
// (campo `span` no Rust: `TokenWithSpan`, `ValueWithSpan`, `CommentWithSpan`)
// implementam `ISpanned.Span()` de forma explícita, e quem chama `x.span()` sobre eles
// escreve `((ISpanned)x).Span()` (docs/port/onda-b/B7.md).
public interface ISpanned
{
    /// <summary>
    /// Return the <see cref="Core.Span"/> (the minimum and maximum <see cref="Location"/>) for this AST
    /// node, by recursively combining the spans of its children.
    /// </summary>
    Span Span();
}
