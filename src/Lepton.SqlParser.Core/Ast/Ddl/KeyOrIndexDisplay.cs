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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// Representation whether a definition can can contains the KEY or INDEX keywords with the same
/// meaning.
/// <para>
/// This enum initially is directed to <c>FULLTEXT</c>,<c>SPATIAL</c>, and <c>UNIQUE</c> indexes on create table
/// statements of <c>MySQL</c> <see href="https://dev.mysql.com/doc/refman/8.0/en/create-table.html">(1)</see>.
/// </para>
/// </summary>
// O Formatter não tem alinhamento (`f.align()`). A lacuna `{}` corresponde a
// Fmt(Formatter) (`f.align()` é `None`); a lacuna `{:>}`
// (`write!(f, "UNIQUE{:>}", x)`) corresponde a FmtAlignRight(Formatter).
// Enum sem dados com métodos e Display: records (convencoes.md §13, caso 37).
public abstract partial record KeyOrIndexDisplay : IDisplay
{
    /// <summary>Nothing to display</summary>
    public sealed record None : KeyOrIndexDisplay;

    /// <summary>Display the KEY keyword</summary>
    public sealed record Key : KeyOrIndexDisplay;

    /// <summary>Display the INDEX keyword</summary>
    public sealed record Index : KeyOrIndexDisplay;

    /// <summary>Check if this is the <c>None</c> variant.</summary>
    public bool IsNone() => this is None;

    // Rust: `impl fmt::Display for KeyOrIndexDisplay`, formatado com `{}`.
    public void Fmt(Formatter f) => FmtAligned(f, alignRight: false);

    // Rust: a lacuna `{:>}` de `write!` (formatador novo, com `f.align()` igual a
    // `Some(fmt::Alignment::Right)`).
    internal void FmtAlignRight(Formatter f) => FmtAligned(new Formatter(f), alignRight: true);

    // Corpo de `fn fmt`: `alignRight` substitui `matches!(f.align(), Some(fmt::Alignment::Right))`.
    private void FmtAligned(Formatter f, bool alignRight)
    {
        var leftSpace = alignRight;

        if (leftSpace && !IsNone())
        {
            f.WriteChar(' ');
        }

        switch (this)
        {
            case None:
                // write!(f, ""): não escreve nada.
                break;
            case Key:
                f.Write($"KEY");
                break;
            case Index:
                f.Write($"INDEX");
                break;
            default:
                throw new InvalidOperationException($"Variante de KeyOrIndexDisplay não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
