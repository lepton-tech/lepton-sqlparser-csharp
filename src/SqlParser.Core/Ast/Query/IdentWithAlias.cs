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

namespace SqlParser.Core.Ast;

/// <summary>Single aliased identifier</summary>
/// <remarks>
/// Syntax:
/// <code>
/// &lt;ident&gt; AS &lt;alias&gt;
/// </code>
/// </remarks>
/// <param name="Ident">The identifier being aliased.</param>
/// <param name="Alias">The alias to apply to <c>ident</c>.</param>
public sealed partial record IdentWithAlias(Ident Ident, Ident Alias) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Ident} AS {Alias}");
    }

    public override string ToString() => Formatter.Format(this);
}
