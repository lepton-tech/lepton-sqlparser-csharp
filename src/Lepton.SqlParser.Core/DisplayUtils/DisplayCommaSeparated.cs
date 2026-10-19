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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/display_utils.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core;

/// <summary>
/// A value that displays a comma-separated list of values.
/// When pretty-printed (using {:#}), it displays each value on a new line.
/// </summary>
// Não confundir com `ast::display_comma_separated` (→ `AstModule.DisplayCommaSeparated`), que
// não quebra linhas no pretty-print. Os dois existem no Rust com o mesmo papel em contextos diferentes.
internal sealed class DisplayCommaSeparated<T> : IDisplay
{
    public DisplayCommaSeparated(IReadOnlyList<T> value) => Value = value;

    // Rust: campo `.0`.
    public IReadOnlyList<T> Value { get; }

    public void Fmt(Formatter f)
    {
        var first = true;
        foreach (var t in Value)
        {
            if (!first)
            {
                f.WriteChar(',');
                SpaceOrNewline.Instance.Fmt(f);
            }
            first = false;
            f.FmtInPlace(t);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
