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

/// <summary>Checks membership of a value in a JSON array</summary>
/// <remarks>
/// Syntax:
/// <code>
/// &lt;value&gt; MEMBER OF(&lt;array&gt;)
/// </code>
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/json-search-functions.html#operator_member-of">MySQL</see>
/// </remarks>
/// <param name="Value">The value to check for membership.</param>
/// <param name="Array">The JSON array expression to check against.</param>
public sealed partial record MemberOf(Expr Value, Expr Array) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Value} MEMBER OF({Array})");
    }

    public override string ToString() => Formatter.Format(this);
}
