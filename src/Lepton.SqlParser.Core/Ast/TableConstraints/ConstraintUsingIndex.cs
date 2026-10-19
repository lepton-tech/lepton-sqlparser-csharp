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

using static Lepton.SqlParser.Core.Ast.DdlModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>PostgreSQL constraint that promotes an existing unique index to a table constraint.</summary>
/// <remarks>
/// <c>[ CONSTRAINT constraint_name ] { UNIQUE | PRIMARY KEY } USING INDEX index_name
///   [ DEFERRABLE | NOT DEFERRABLE ] [ INITIALLY DEFERRED | INITIALLY IMMEDIATE ]</c>
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-altertable.html"/></para>
/// </remarks>
/// <param name="Name">Optional constraint name.</param>
/// <param name="IndexName">The name of the existing unique index to promote.</param>
/// <param name="Characteristics">Optional characteristics like <c>DEFERRABLE</c>.</param>
// Sem `Display` no Rust: o texto vem de FmtWithKeyword, chamado por `TableConstraint`.
public sealed partial record ConstraintUsingIndex(Ident? Name, Ident IndexName, ConstraintCharacteristics? Characteristics) : ISpanned
{
    /// <summary>Format as <c>[CONSTRAINT name] &lt;keyword&gt; USING INDEX index_name [characteristics]</c>.</summary>
    public void FmtWithKeyword(Formatter f, string keyword)
    {
        // use crate::ast::ddl::{display_constraint_name, display_option_spaced};
        f.Write($"{DisplayConstraintName(Name)}{keyword} USING INDEX {IndexName}");
        f.Write($"{DisplayOptionSpaced(Characteristics)}");
    }

    // impl crate::ast::Spanned for ConstraintUsingIndex (table_constraints.rs:623)
    public Span Span()
    {
        // let start = self.name.as_ref().map(|i| i.span).unwrap_or(self.index_name.span);
        var start = Name?.Span ?? IndexName.Span;
        // let end = self.characteristics.as_ref().map(|c| c.span()).unwrap_or(self.index_name.span);
        var end = Characteristics?.Span() ?? IndexName.Span;
        return start.Union(end);
    }
}
