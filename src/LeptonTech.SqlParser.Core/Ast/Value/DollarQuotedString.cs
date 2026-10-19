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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/value.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>A dollar-quoted string literal, e.g. <c>$$...$$</c> or <c>$tag$...$tag$</c>.</summary>
/// <param name="Value">Inner string contents.</param>
/// <param name="Tag">Optional tag used in the opening/closing delimiter.</param>
public sealed partial record DollarQuotedString(string Value, string? Tag) : IDisplay
{
    /// <inheritdoc/>
    public void Fmt(Formatter f)
    {
        switch (Tag)
        {
            case { } tag:
                // write!(f, "${}${}${}$", tag, self.value, tag)
                f.WriteStr("$");
                f.WriteStr(tag);
                f.WriteStr("$");
                f.WriteStr(Value);
                f.WriteStr("$");
                f.WriteStr(tag);
                f.WriteStr("$");
                break;
            case null:
                // write!(f, "$${}$$", self.value)
                f.WriteStr("$$");
                f.WriteStr(Value);
                f.WriteStr("$$");
                break;
        }
    }

    // Rust: `impl fmt::Display for DollarQuotedString`.
    public override string ToString() => Formatter.Format(this);
}
