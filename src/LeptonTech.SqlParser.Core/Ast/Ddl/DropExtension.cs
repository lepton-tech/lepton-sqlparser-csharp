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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// DROP EXTENSION statement
/// Note: this is a PostgreSQL-specific statement
/// <para>References</para>
/// <para>PostgreSQL Documentation: <see href="https://www.postgresql.org/docs/current/sql-dropextension.html"/></para>
/// </summary>
/// <param name="Names">One or more extension names to drop</param>
/// <param name="IfExists">Whether <c>IF EXISTS</c> was specified for the DROP EXTENSION.</param>
/// <param name="CascadeOrRestrict"><c>CASCADE</c> or <c>RESTRICT</c> behaviour for the drop.</param>
public sealed partial record DropExtension(
    Sequence<Ident> Names,
    bool IfExists,
    ReferentialAction? CascadeOrRestrict) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        f.Write($"DROP EXTENSION");
        if (IfExists)
        {
            f.Write($" IF EXISTS");
        }
        f.Write($" {DisplayCommaSeparated(Names)}");
        if (CascadeOrRestrict is { } cascadeOrRestrict)
        {
            f.Write($" {cascadeOrRestrict}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for DropExtension (ddl.rs:4584)
    public Span Span() => Core.Span.Empty();
}
