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
using static Lepton.SqlParser.Core.Ast.DdlModule;
using static Lepton.SqlParser.Core.RustIter;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Unique constraint definition.</summary>
/// <param name="Name">
/// Constraint name.
/// <para>Can be not the same as <c>index_name</c></para>
/// </param>
/// <param name="IndexName">Index name</param>
/// <param name="IndexTypeDisplay">Whether the type is followed by the keyword <c>KEY</c>, <c>INDEX</c>, or no keyword at all.</param>
/// <param name="IndexType">Optional <c>USING</c> of index type (<c>IndexType</c>) statement before columns.</param>
/// <param name="Columns">Identifiers of the columns that are unique.</param>
/// <param name="Include">INCLUDE clause: <see href="https://www.postgresql.org/docs/current/sql-createtable.html"/></param>
/// <param name="IndexOptions">Optional index options such as <c>USING</c>.</param>
/// <param name="Characteristics">Optional characteristics like <c>DEFERRABLE</c>.</param>
/// <param name="NullsDistinct">Optional Postgres nulls handling: <c>[ NULLS [ NOT ] DISTINCT ]</c></param>
public sealed partial record UniqueConstraint(
    Ident? Name,
    Ident? IndexName,
    KeyOrIndexDisplay IndexTypeDisplay,
    IndexType? IndexType,
    Sequence<IndexColumn> Columns,
    Sequence<Ident> Include,
    Sequence<IndexOption> IndexOptions,
    ConstraintCharacteristics? Characteristics,
    NullsDistinctOption NullsDistinct) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        // use crate::ast::ddl::{display_constraint_name, display_option, display_option_spaced};
        // write!(f, "{}UNIQUE{}{:>}{}{} ({})", ...): a lacuna `{:>}` de index_type_display (alinhamento à
        // direita) não existe no Formatter; KeyOrIndexDisplay.FmtAlignRight a reproduz, no meio do
        // mesmo write! (convencoes.md §13, caso 66).
        f.Write($"{DisplayConstraintName(Name)}UNIQUE{NullsDistinct}");
        IndexTypeDisplay.FmtAlignRight(f);
        f.Write($"{DisplayOptionSpaced(IndexName)}{DisplayOption(" USING ", "", IndexType)} ({DisplayCommaSeparated(Columns)})");

        if (!Include.IsEmpty)
        {
            f.Write($" INCLUDE ({DisplayCommaSeparated(Include)})");
        }

        if (!IndexOptions.IsEmpty)
        {
            f.Write($" {DisplaySeparated(IndexOptions, " ")}");
        }

        f.Write($"{DisplayOptionSpaced(Characteristics)}");
    }

    public override string ToString() => Formatter.Format(this);

    // impl crate::ast::Spanned for UniqueConstraint (table_constraints.rs:571)
    public Span Span()
    {
        // fn union_spans<I: Iterator<Item = Span>>(iter: I) -> Span (aninhada): função local
        // (decisão 9 da B1).
        static Span UnionSpans(IEnumerable<Span> iter) => Core.Span.UnionIter(iter);

        return UnionSpans(
            OptionIter(Name)
                .Select(i => i.Span)
                .Concat(OptionIter(IndexName).Select(i => i.Span))
                .Concat(Columns.Select(i => i.Span()))
                .Concat(Include.Select(i => i.Span))
                .Concat(OptionIter(Characteristics).Select(i => i.Span())));
    }
}
