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

using static SqlParser.Core.Ast.AstModule;
using static SqlParser.Core.RustIter;

namespace SqlParser.Core.Ast;

/// <summary>
/// MySQLs <see href="https://dev.mysql.com/doc/refman/8.0/en/fulltext-natural-language.html">fulltext</see> definition. Since the
/// <see href="https://dev.mysql.com/doc/refman/8.0/en/spatial-types.html"><c>SPATIAL</c></see> definition is exactly the same,
/// and MySQL displays both the same way, it is part of this definition as well.
/// </summary>
/// <remarks>
/// Supported syntax:
/// <code>
/// {FULLTEXT | SPATIAL} [INDEX | KEY] [index_name] (key_part,...)
///
/// key_part: col_name
/// </code>
/// </remarks>
/// <param name="Fulltext">Whether this is a <c>FULLTEXT</c> (true) or <c>SPATIAL</c> (false) definition.</param>
/// <param name="IndexTypeDisplay">Whether the type is followed by the keyword <c>KEY</c>, <c>INDEX</c>, or no keyword at all.</param>
/// <param name="OptIndexName">Optional index name.</param>
/// <param name="Columns">Referred column identifier list.</param>
public sealed partial record FullTextOrSpatialConstraint(
    bool Fulltext,
    KeyOrIndexDisplay IndexTypeDisplay,
    Ident? OptIndexName,
    Sequence<IndexColumn> Columns) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        if (Fulltext)
        {
            f.Write($"FULLTEXT");
        }
        else
        {
            f.Write($"SPATIAL");
        }

        // write!(f, "{:>}", self.index_type_display): a lacuna `{:>}` (alinhamento à direita) não existe
        // no Formatter; KeyOrIndexDisplay.FmtAlignRight a reproduz (convencoes.md §13, caso 66).
        IndexTypeDisplay.FmtAlignRight(f);

        if (OptIndexName is { } name)
        {
            f.Write($" {name}");
        }

        f.Write($" ({DisplayCommaSeparated(Columns)})");
    }

    public override string ToString() => Formatter.Format(this);

    // impl crate::ast::Spanned for FullTextOrSpatialConstraint (table_constraints.rs:358)
    public Span Span()
    {
        // fn union_spans<I: Iterator<Item = Span>>(iter: I) -> Span (aninhada): função local
        // (decisão 9 da B1).
        static Span UnionSpans(IEnumerable<Span> iter) => Core.Span.UnionIter(iter);

        return UnionSpans(
            OptionIter(OptIndexName)
                .Select(i => i.Span)
                .Concat(Columns.Select(i => i.Span())));
    }
}
