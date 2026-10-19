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
using static SqlParser.Core.Ast.DdlModule;
using static SqlParser.Core.RustIter;

namespace SqlParser.Core.Ast;

/// <summary>
/// A referential integrity constraint (<c>[ CONSTRAINT &lt;name&gt; ] FOREIGN KEY (&lt;columns&gt;)
/// REFERENCES &lt;foreign_table&gt; (&lt;referred_columns&gt;) [ MATCH { FULL | PARTIAL | SIMPLE } ]
/// { [ON DELETE &lt;referential_action&gt;] [ON UPDATE &lt;referential_action&gt;] |
///   [ON UPDATE &lt;referential_action&gt;] [ON DELETE &lt;referential_action&gt;]
/// }</c>).
/// </summary>
/// <param name="Name">Optional constraint name.</param>
/// <param name="IndexName">
/// MySQL-specific index name associated with the foreign key.
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table-foreign-keys.html"/>
/// </param>
/// <param name="Columns">Columns in the local table that participate in the foreign key.</param>
/// <param name="ForeignTable">Referenced foreign table name.</param>
/// <param name="ReferredColumns">Columns in the referenced table.</param>
/// <param name="OnDelete">Action to perform <c>ON DELETE</c>.</param>
/// <param name="OnUpdate">Action to perform <c>ON UPDATE</c>.</param>
/// <param name="MatchKind">Optional <c>MATCH</c> kind (FULL | PARTIAL | SIMPLE).</param>
/// <param name="Characteristics">Optional characteristics (e.g., <c>DEFERRABLE</c>).</param>
public sealed partial record ForeignKeyConstraint(
    Ident? Name,
    Ident? IndexName,
    Sequence<Ident> Columns,
    ObjectName ForeignTable,
    Sequence<Ident> ReferredColumns,
    ReferentialAction? OnDelete,
    ReferentialAction? OnUpdate,
    ConstraintReferenceMatchKind? MatchKind,
    ConstraintCharacteristics? Characteristics) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        // use crate::ast::ddl::{display_constraint_name, display_option_spaced};
        f.Write($"{DisplayConstraintName(Name)}FOREIGN KEY{DisplayOptionSpaced(IndexName)} ({DisplayCommaSeparated(Columns)}) REFERENCES {ForeignTable}");
        if (!ReferredColumns.IsEmpty)
        {
            f.Write($"({DisplayCommaSeparated(ReferredColumns)})");
        }
        if (MatchKind is { } matchKind)
        {
            f.Write($" {matchKind}");
        }
        if (OnDelete is { } onDeleteAction)
        {
            f.Write($" ON DELETE {onDeleteAction}");
        }
        if (OnUpdate is { } onUpdateAction)
        {
            f.Write($" ON UPDATE {onUpdateAction}");
        }
        if (Characteristics is { } characteristics)
        {
            f.Write($" {characteristics}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl crate::ast::Spanned for ForeignKeyConstraint (table_constraints.rs:290)
    public Span Span()
    {
        // fn union_spans<I: Iterator<Item = Span>>(iter: I) -> Span (aninhada): função local
        // (decisão 9 da B1).
        static Span UnionSpans(IEnumerable<Span> iter) => Core.Span.UnionIter(iter);

        return UnionSpans(
            OptionIter(Name)
                .Select(i => i.Span)
                .Concat(OptionIter(IndexName).Select(i => i.Span))
                .Concat(Columns.Select(i => i.Span))
                .Concat(Once(ForeignTable.Span()))
                .Concat(ReferredColumns.Select(i => i.Span))
                .Concat(OptionIter(OnDelete).Select(i => i.Span()))
                .Concat(OptionIter(OnUpdate).Select(i => i.Span()))
                .Concat(OptionIter(Characteristics).Select(i => i.Span())));
    }
}
