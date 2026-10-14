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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>A dictionary field within a dictionary.</summary>
/// <remarks>[DuckDB]: https://duckdb.org/docs/sql/data_types/struct#creating-structs</remarks>
/// <param name="Key">Dictionary key identifier.</param>
/// <param name="Value">Value expression for the dictionary entry.</param>
public sealed record DictionaryField(Ident Key, Expr Value) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Key}: {Value}");
    }

    public override string ToString() => Formatter.Format(this);
}
