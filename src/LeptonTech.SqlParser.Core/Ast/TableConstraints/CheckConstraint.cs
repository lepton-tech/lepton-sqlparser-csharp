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

using static LeptonTech.SqlParser.Core.Ast.DdlModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A <c>CHECK</c> constraint (<c>[ CONSTRAINT &lt;name&gt; ] CHECK (&lt;expr&gt;) [NO INHERIT] [[NOT] ENFORCED]</c>).</summary>
/// <param name="Name">Optional constraint name.</param>
/// <param name="Expr">The boolean expression the CHECK constraint enforces.</param>
/// <param name="NoInherit">
/// PostgreSQL-specific <c>NO INHERIT</c> flag: child tables do not inherit the constraint.
/// <see href="https://www.postgresql.org/docs/current/sql-createtable.html"/>
/// </param>
/// <param name="Enforced">
/// MySQL-specific <c>ENFORCED</c> / <c>NOT ENFORCED</c> flag.
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table.html"/>
/// </param>
public sealed partial record CheckConstraint(Ident? Name, Expr Expr, bool NoInherit, bool? Enforced) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        // use crate::ast::ddl::display_constraint_name;
        f.Write($"{DisplayConstraintName(Name)}CHECK ({Expr})");
        if (NoInherit)
        {
            f.Write($" NO INHERIT");
        }
        if (Enforced is { } b)
        {
            f.Write($" {(b ? "ENFORCED" : "NOT ENFORCED")}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl crate::ast::Spanned for CheckConstraint (table_constraints.rs:222)
    public Span Span() =>
        Expr
            .Span()
            .UnionOpt(Name?.Span);
}
