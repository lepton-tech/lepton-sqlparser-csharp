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

namespace SqlParser.Core.Ast;

/// <summary>One element in an <c>EXCLUDE</c> constraint's element list.</summary>
/// <remarks><see href="https://www.postgresql.org/docs/current/sql-createtable.html#SQL-CREATETABLE-EXCLUDE">PostgreSQL</see></remarks>
/// <param name="Column">
/// The index column (<c>{ column_name | ( expression ) } [ opclass ] [ ASC | DESC ] [ NULLS { FIRST | LAST } ]</c>).
/// </param>
/// <param name="Operator">The exclusion operator.</param>
public sealed partial record ExcludeConstraintElement(IndexColumn Column, ExcludeConstraintOperator Operator) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Column} WITH {Operator}");
    }

    public override string ToString() => Formatter.Format(this);
}
