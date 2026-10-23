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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// An item of SQLite's <c>table-options</c> list, which follows the column
/// definitions of <c>CREATE TABLE</c>.
/// </summary>
/// <remarks>
/// See <see href="https://www.sqlite.org/lang_createtable.html">SQLite documentation</see>.
/// </remarks>
public abstract partial record SqliteTableOption : IDisplay
{
    /// <summary><c>WITHOUT ROWID</c></summary>
    public sealed record WithoutRowid : SqliteTableOption;

    /// <summary><c>STRICT</c></summary>
    public sealed record Strict : SqliteTableOption;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            WithoutRowid => "WITHOUT ROWID",
            Strict => "STRICT",
            _ => throw new InvalidOperationException($"Variante de SqliteTableOption não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
