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

/// <summary>An element of a JSON path.</summary>
public abstract partial record JsonPathElem
{
    /// <summary>
    /// Accesses an object field using dot notation, e.g. <c>obj:foo.bar.baz</c>.
    /// <para>See <see href="https://docs.snowflake.com/en/user-guide/querying-semistructured#dot-notation"/>.</para>
    /// </summary>
    /// <param name="Key">The object key text (without quotes).</param>
    /// <param name="Quoted"><c>true</c> when the key was quoted in the source.</param>
    public sealed record Dot(string Key, bool Quoted) : JsonPathElem;

    /// <summary>
    /// Accesses an object field or array element using bracket notation,
    /// e.g. <c>obj['foo']</c>.
    /// <para>See <see href="https://docs.snowflake.com/en/user-guide/querying-semistructured#bracket-notation"/>.</para>
    /// </summary>
    /// <param name="Key">The expression used as the bracket key (string or numeric expression).</param>
    public sealed record Bracket(Expr Key) : JsonPathElem;

    /// <summary>
    /// Access an object field using colon bracket notation
    /// e.g. <c>obj:['foo']</c>
    /// <para>See <see href="https://docs.databricks.com/en/sql/language-manual/functions/colonsign.html"/></para>
    /// </summary>
    /// <param name="Key">The expression used as the bracket key (string or numeric expression).</param>
    public sealed record ColonBracket(Expr Key) : JsonPathElem;
}
