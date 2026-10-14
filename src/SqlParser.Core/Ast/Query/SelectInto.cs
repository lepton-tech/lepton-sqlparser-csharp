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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary><c>SELECT INTO</c> clause options.</summary>
/// <param name="Temporary"><c>TEMPORARY</c> modifier.</param>
/// <param name="Unlogged"><c>UNLOGGED</c> modifier.</param>
/// <param name="Table"><c>TABLE</c> keyword present.</param>
/// <param name="Targets">
/// Target(s) of the <c>INTO</c> clause.
/// <para>
/// [Postgres]: <see href="https://www.postgresql.org/docs/current/sql-selectinto.html"/>
/// [MySQL]: <see href="https://dev.mysql.com/doc/refman/9.7/en/select-into.html"/>
/// </para>
/// </param>
public sealed partial record SelectInto(bool Temporary, bool Unlogged, bool Table, Sequence<Expr> Targets) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var temporary = Temporary ? " TEMPORARY" : "";
        var unlogged = Unlogged ? " UNLOGGED" : "";
        var table = Table ? " TABLE" : "";

        f.Write($"INTO{temporary}{unlogged}{table} {DisplayCommaSeparated(Targets)}");
    }

    public override string ToString() => Formatter.Format(this);
}
