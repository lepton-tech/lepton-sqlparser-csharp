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

/// <summary>A value that inserts a space when displayed with {}, but a newline when displayed with {:#}.</summary>
// Struct unitário do Rust: use Instance (convencoes.md §13, caso 33).
internal sealed class SpaceOrNewline : IDisplay
{
    public static readonly SpaceOrNewline Instance = new();

    private SpaceOrNewline()
    {
    }

    public void Fmt(Formatter f)
    {
        if (f.Alternate)
        {
            f.WriteChar('\n');
        }
        else
        {
            f.WriteChar(' ');
        }
    }

    public override string ToString() => Formatter.Format(this);
}
