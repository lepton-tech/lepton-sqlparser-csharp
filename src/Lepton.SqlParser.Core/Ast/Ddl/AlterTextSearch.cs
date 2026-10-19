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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// <c>ALTER TEXT SEARCH ...</c> statement.
/// <para>See <see href="https://www.postgresql.org/docs/current/sql-altertsdictionary.html">PostgreSQL</see>.</para>
/// </summary>
/// <param name="ObjectType">The specific text search object type.</param>
/// <param name="Name">Object name.</param>
/// <param name="Operation">Operation to apply.</param>
public sealed partial record AlterTextSearch(
    TextSearchObjectType ObjectType,
    ObjectName Name,
    AlterTextSearchOperation Operation) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        f.Write($"ALTER TEXT SEARCH {ObjectType} {Name} {Operation}");
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for AlterTextSearch (ddl.rs:5802)
    public Span Span() => Core.Span.Empty();
}
