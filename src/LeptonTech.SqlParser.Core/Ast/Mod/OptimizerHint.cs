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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Query optimizer hints are optionally supported comments after the
/// <c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c>, <c>REPLACE</c>, <c>MERGE</c>, and <c>DELETE</c> keywords in
/// the corresponding statements.
/// </summary>
/// <remarks>See <c>Select::optimizer_hints</c></remarks>
/// <param name="Prefix">
/// An optional prefix between the comment marker and <c>+</c>.
/// <para>
/// Standard optimizer hints like <c>/*+ ... */</c> have an empty prefix,
/// while system-specific hints like <c>/*abc+ ... */</c> have <c>prefix = "abc"</c>.
/// The prefix is any sequence of ASCII alphanumeric characters
/// immediately before the <c>+</c> marker.
/// </para>
/// </param>
/// <param name="Text">the raw text of the optimizer hint without its markers</param>
/// <param name="Style">
/// the style of the comment which <c>text</c> was extracted from,
/// e.g. <c>/*+...*/</c> or <c>--+...</c>
/// <para>Not all dialects support all styles, though.</para>
/// </param>
public sealed partial record OptimizerHint(string Prefix, string Text, OptimizerHintStyle Style) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (Style)
        {
            case OptimizerHintStyle.SingleLine singleLine:
                f.WriteStr(singleLine.Prefix);
                f.WriteStr(Prefix);
                f.WriteStr("+");
                f.WriteStr(Text);
                f.WriteStr("\n");
                break;
            case OptimizerHintStyle.MultiLine:
                f.WriteStr("/*");
                f.WriteStr(Prefix);
                f.WriteStr("+");
                f.WriteStr(Text);
                f.WriteStr("*/");
                break;
            default:
                throw new InvalidOperationException($"Variante de OptimizerHintStyle não tratada: {Style.GetType().Name}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
