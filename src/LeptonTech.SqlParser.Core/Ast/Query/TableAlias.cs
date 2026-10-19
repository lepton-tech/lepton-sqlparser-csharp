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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>An alias for a table reference, optionally including an explicit <c>AS</c> and column names.</summary>
/// <param name="Explicit">
/// Tells whether the alias was introduced with an explicit, preceding "AS"
/// keyword, e.g. <c>AS name</c>. Typically, the keyword is preceding the name
/// (e.g. <c>.. FROM table AS t ..</c>).
/// </param>
/// <param name="Name">Alias identifier for the table.</param>
/// <param name="Columns">Optional column aliases declared in parentheses after the table alias.</param>
/// <param name="At">
/// Optional PartiQL index alias declared with <c>AT</c>. For example:
/// <code>
/// SELECT element, index FROM bar AS b, b.data.scalar_array AS element AT index
/// </code>
/// See: <see href="https://docs.aws.amazon.com/redshift/latest/dg/query-super.html"/>
/// </param>
public sealed partial record TableAlias(bool Explicit, Ident Name, Sequence<TableAliasColumnDef> Columns, Ident? At) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{(Explicit ? "AS " : "")}{Name}");
        if (!Columns.IsEmpty)
        {
            f.Write($" ({DisplayCommaSeparated(Columns)})");
        }
        if (At is { } at)
        {
            f.Write($" AT {at}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
