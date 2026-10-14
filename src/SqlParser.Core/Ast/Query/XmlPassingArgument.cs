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

/// <summary>
/// Argument passed in the XMLTABLE PASSING clause
/// Argument passed in the <c>XMLTABLE PASSING</c> clause.
/// </summary>
/// <param name="Expr">Expression to pass to the XML table.</param>
/// <param name="Alias">Optional alias for the argument.</param>
/// <param name="ByValue"><c>true</c> if <c>BY VALUE</c> is specified for the argument.</param>
public sealed record XmlPassingArgument(Expr Expr, Ident? Alias, bool ByValue) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (ByValue)
        {
            f.Write($"BY VALUE ");
        }
        f.Write($"{Expr}");
        if (Alias is { } alias)
        {
            f.Write($" AS {alias}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
