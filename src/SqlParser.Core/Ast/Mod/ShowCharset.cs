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

/// <summary>A Show Charset statement</summary>
/// <param name="IsShorthand">
/// The statement can be written as <c>SHOW CHARSET</c> or <c>SHOW CHARACTER SET</c>
/// true means CHARSET was used and false means CHARACTER SET was used
/// </param>
/// <param name="Filter">Optional <c>LIKE</c>/<c>WHERE</c>-style filter for the statement.</param>
public sealed record ShowCharset(bool IsShorthand, ShowStatementFilter? Filter) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"SHOW");
        if (IsShorthand)
        {
            f.Write($" CHARSET");
        }
        else
        {
            f.Write($" CHARACTER SET");
        }
        if (Filter is { } filter)
        {
            f.Write($" {filter}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
