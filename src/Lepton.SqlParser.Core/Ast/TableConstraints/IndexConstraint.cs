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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/table_constraints.rs.
// Modificado: tradução de Rust para C#.

using static Lepton.SqlParser.Core.Ast.AstModule;
using static Lepton.SqlParser.Core.RustIter;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// MySQLs <see href="https://dev.mysql.com/doc/refman/8.0/en/create-table.html">index definition</see> for index creation.
/// Not present on ANSI so, for now, the usage
/// is restricted to MySQL, as no other dialects that support this syntax were found.
/// </summary>
/// <remarks><c>{INDEX | KEY} [index_name] [index_type] (key_part,...) [index_option]...</c></remarks>
/// <param name="DisplayAsKey">Whether this index starts with KEY (true) or INDEX (false), to maintain the same syntax.</param>
/// <param name="Name">Index name.</param>
/// <param name="IndexType">Optional index type (<c>IndexType</c>).</param>
/// <param name="Columns">Referred column identifier list.</param>
/// <param name="IndexOptions">
/// Optional index options such as <c>USING</c>; see <c>IndexOption</c>.
/// Options applied to the index (e.g., <c>COMMENT</c>, <c>WITH</c> options).
/// </param>
public sealed partial record IndexConstraint(
    bool DisplayAsKey,
    Ident? Name,
    IndexType? IndexType,
    Sequence<IndexColumn> Columns,
    Sequence<IndexOption> IndexOptions) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        f.Write($"{(DisplayAsKey ? "KEY" : "INDEX")}");
        if (Name is { } name)
        {
            f.Write($" {name}");
        }
        if (IndexType is { } indexType)
        {
            f.Write($" USING {indexType}");
        }
        f.Write($" ({DisplayCommaSeparated(Columns)})");
        if (!IndexOptions.IsEmpty)
        {
            f.Write($" {DisplayCommaSeparated(IndexOptions)}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl crate::ast::Spanned for IndexConstraint (table_constraints.rs:415)
    public Span Span()
    {
        // fn union_spans<I: Iterator<Item = Span>>(iter: I) -> Span (aninhada): função local
        // (decisão 9 da B1).
        static Span UnionSpans(IEnumerable<Span> iter) => Core.Span.UnionIter(iter);

        return UnionSpans(
            OptionIter(Name)
                .Select(i => i.Span)
                .Concat(Columns.Select(i => i.Span())));
    }
}
