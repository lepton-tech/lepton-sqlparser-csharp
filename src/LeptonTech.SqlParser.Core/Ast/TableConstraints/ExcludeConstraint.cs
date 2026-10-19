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

using static LeptonTech.SqlParser.Core.Ast.AstModule;
using static LeptonTech.SqlParser.Core.Ast.DdlModule;
using static LeptonTech.SqlParser.Core.RustIter;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>An <c>EXCLUDE</c> constraint.</summary>
/// <remarks><see href="https://www.postgresql.org/docs/current/sql-createtable.html#SQL-CREATETABLE-EXCLUDE">PostgreSQL</see></remarks>
/// <param name="Name">Optional constraint name.</param>
/// <param name="IndexMethod">Optional index method (e.g. <c>gist</c>, <c>spgist</c>).</param>
/// <param name="Elements">The list of index expressions with their exclusion operators.</param>
/// <param name="Include">Optional list of additional columns to include in the index.</param>
/// <param name="WhereClause">Optional <c>WHERE</c> predicate to restrict the constraint to a subset of rows.</param>
/// <param name="Characteristics">Optional constraint characteristics like <c>DEFERRABLE</c>.</param>
public sealed partial record ExcludeConstraint(
    Ident? Name,
    Ident? IndexMethod,
    Sequence<ExcludeConstraintElement> Elements,
    Sequence<Ident> Include,
    Expr? WhereClause,
    ConstraintCharacteristics? Characteristics) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        // use crate::ast::ddl::display_constraint_name;
        f.Write($"{DisplayConstraintName(Name)}EXCLUDE");
        if (IndexMethod is { } method)
        {
            f.Write($" USING {method}");
        }
        f.Write($" ({DisplayCommaSeparated(Elements)})");
        if (!Include.IsEmpty)
        {
            f.Write($" INCLUDE ({DisplayCommaSeparated(Include)})");
        }
        if (WhereClause is { } predicate)
        {
            f.Write($" WHERE ({predicate})");
        }
        if (Characteristics is { } characteristics)
        {
            f.Write($" {characteristics}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl crate::ast::Spanned for ExcludeConstraint (table_constraints.rs:724)
    public Span Span() =>
        Core.Span.UnionIter(
            OptionIter(Name)
                .Select(i => i.Span)
                .Concat(OptionIter(IndexMethod).Select(i => i.Span))
                .Concat(Elements.Select(e => e.Span()))
                .Concat(Include.Select(i => i.Span))
                .Concat(OptionIter(WhereClause).Select(e => e.Span()))
                .Concat(OptionIter(Characteristics).Select(c => c.Span())));
}
