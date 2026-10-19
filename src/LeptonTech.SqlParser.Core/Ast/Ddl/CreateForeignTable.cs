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
using static LeptonTech.SqlParser.Core.RustIter;

namespace LeptonTech.SqlParser.Core.Ast;

// Tipo novo do upstream #2515 (Rodada 1 de sincronização; não existe na v0.63.0).

/// <summary>
/// A <c>CREATE FOREIGN TABLE</c> statement.
/// <para>
/// See <see href="https://www.postgresql.org/docs/current/sql-createforeigntable.html">PostgreSQL</see>
/// </para>
/// </summary>
/// <param name="Name">The foreign table name.</param>
/// <param name="IfNotExists">Whether <c>IF NOT EXISTS</c> was specified.</param>
/// <param name="Columns">Column definitions.</param>
/// <param name="Constraints">
/// Table-level constraints (e.g. <c>CHECK (...)</c>, composite <c>FOREIGN KEY</c>).
/// PostgreSQL's grammar accepts these here, but rejects primary key, unique,
/// foreign key and exclusion constraints on a foreign table at execution.
/// </param>
/// <param name="ServerName">The <c>SERVER server_name</c> clause.</param>
/// <param name="Options">Optional <c>OPTIONS (key 'value', ...)</c> clause at the table level.</param>
public sealed partial record CreateForeignTable(
    [property: VisitWith("visit_relation")] ObjectName Name,
    bool IfNotExists,
    Sequence<ColumnDef> Columns,
    Sequence<TableConstraint> Constraints,
    Ident ServerName,
    Sequence<CreateServerOption>? Options) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        // write!(f, "CREATE FOREIGN TABLE {if_not_exists}{name} ({columns}", if_not_exists = .., name = .., columns = ..)
        var ifNotExists = IfNotExists ? "IF NOT EXISTS " : "";
        var name = Name;
        var columns = DisplayCommaSeparated(Columns);
        f.Write($"CREATE FOREIGN TABLE {ifNotExists}{name} ({columns}");
        if (!Columns.IsEmpty && !Constraints.IsEmpty)
        {
            f.Write($", ");
        }
        f.Write($"{DisplayCommaSeparated(Constraints)}");
        f.Write($") SERVER {ServerName}");
        if (Options is { } options)
        {
            f.Write($" OPTIONS ({DisplayCommaSeparated(options)})");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // `impl From<CreateForeignTable> for crate::ast::Statement` → `Statement.From(CreateForeignTable)`,
    // em Statement.From.Ddl.cs (convencoes.md §13, caso 7).

    // impl Spanned for CreateForeignTable (ddl.rs:6065, em dacdbc9)
    public Span Span() =>
        Core.Span.UnionIter(
            Once(Name.Span())
                .Concat(Columns.Select(column => column.Span()))
                .Concat(Constraints.Select(constraint => constraint.Span()))
                .Concat(Once(ServerName.Span))
                .Concat(
                    OptionIter(Options)
                        .SelectMany(i => i)
                        .SelectMany(option => new[] { option.Key.Span, option.Value.Span })));
}
