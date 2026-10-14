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

using static SqlParser.Core.Ast.DdlModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// An optionally-named <c>ColumnOption</c>: <c>[ CONSTRAINT &lt;name&gt; ] &lt;column-option&gt;</c>.
/// <para>
/// Note that implementations are substantially more permissive than the ANSI
/// specification on what order column options can be presented in, and whether
/// they are allowed to be named. The specification distinguishes between
/// constraints (NOT NULL, UNIQUE, PRIMARY KEY, and CHECK), which can be named
/// and can appear in any order, and other options (DEFAULT, GENERATED), which
/// cannot be named and must appear in a fixed order. <c>PostgreSQL</c>, however,
/// allows preceding any option with <c>CONSTRAINT &lt;name&gt;</c>, even those that are
/// not really constraints, like NULL and DEFAULT. MSSQL is less permissive,
/// allowing DEFAULT, UNIQUE, PRIMARY KEY and CHECK to be named, but not NULL or
/// NOT NULL constraints (the last of which is in violation of the spec).
/// </para>
/// <para>
/// For maximum flexibility, we don't distinguish between constraint and
/// non-constraint options, lumping them all together under the umbrella of
/// "column options," and we allow any column option to be named.
/// </para>
/// </summary>
/// <param name="Name">Optional name of the constraint.</param>
/// <param name="Option">The actual column option (e.g. <c>NOT NULL</c>, <c>DEFAULT</c>, <c>GENERATED</c>, ...).</param>
public sealed partial record ColumnOptionDef(Ident? Name, ColumnOption Option) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{DisplayConstraintName(Name)}{Option}");
    }

    public override string ToString() => Formatter.Format(this);
}
