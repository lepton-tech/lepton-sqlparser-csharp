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
/// MySQL <see href="https://dev.mysql.com/doc/refman/8.3/en/create-table.html">definition</see> for <c>PRIMARY KEY</c> constraints statements:
/// <c>[CONSTRAINT [&lt;name&gt;]] PRIMARY KEY [index_name] [index_type] (&lt;columns&gt;) &lt;index_options&gt;</c>
/// </summary>
/// <remarks>
/// Actually the specification have no <c>[index_name]</c> but the next query will complete successfully:
/// <code>
/// CREATE TABLE unspec_table (
///   xid INT NOT NULL,
///   CONSTRAINT p_name PRIMARY KEY index_name USING BTREE (xid)
/// );
/// </code>
/// where:
/// <list type="bullet">
/// <item>index_type (<c>IndexType</c>) is <c>USING {BTREE | HASH}</c></item>
/// <item>index_options (<c>IndexOption</c>) is <c>{index_type | COMMENT 'string' | ... %currently unsupported stmts% } ...</c></item>
/// </list>
/// </remarks>
/// <param name="Name">
/// Constraint name.
/// <para>Can be not the same as <c>index_name</c></para>
/// </param>
/// <param name="IndexName">Index name</param>
/// <param name="IndexType">Optional <c>USING</c> of index type (<c>IndexType</c>) statement before columns.</param>
/// <param name="Columns">Identifiers of the columns that form the primary key.</param>
/// <param name="Include">INCLUDE clause: <see href="https://www.postgresql.org/docs/current/sql-createtable.html"/></param>
/// <param name="IndexOptions">Optional index options such as <c>USING</c>.</param>
/// <param name="Characteristics">Optional characteristics like <c>DEFERRABLE</c>.</param>
public sealed partial record PrimaryKeyConstraint(
    Ident? Name,
    Ident? IndexName,
    IndexType? IndexType,
    Sequence<IndexColumn> Columns,
    Sequence<Ident> Include,
    Sequence<IndexOption> IndexOptions,
    ConstraintCharacteristics? Characteristics) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        // use crate::ast::ddl::{display_constraint_name, display_option, display_option_spaced};
        f.Write($"{DisplayConstraintName(Name)}PRIMARY KEY{DisplayOptionSpaced(IndexName)}{DisplayOption(" USING ", "", IndexType)} ({DisplayCommaSeparated(Columns)})");

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

    // impl crate::ast::Spanned for PrimaryKeyConstraint (table_constraints.rs:497)
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
