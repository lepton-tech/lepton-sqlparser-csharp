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

/// <summary>
/// Funções livres do módulo <c>ast</c> (<c>src/ast/mod.rs</c>). O módulo não tem um tipo homônimo,
/// então as funções ficam em <c>AstModule</c> (convencoes.md §13, caso 29). Use
/// <c>using static SqlParser.Core.Ast.AstModule;</c> para chamá-las sem prefixo, como no Rust.
/// </summary>
public static partial class AstModule
{
    // Rust: pub(crate) fn display_separated<'a, T>(slice: &'a [T], sep: &'static str) -> DisplaySeparated<'a, T>
    internal static DisplaySeparated<T> DisplaySeparated<T>(IReadOnlyList<T> slice, string sep) =>
        new DisplaySeparated<T>(slice, sep);

    // Rust: pub(crate) fn display_comma_separated<T>(slice: &[T]) -> DisplaySeparated<'_, T>
    internal static DisplaySeparated<T> DisplayCommaSeparated<T>(IReadOnlyList<T> slice) =>
        new DisplaySeparated<T>(slice, ", ");

    /// <summary>
    /// Writes the given statements to the formatter, each ending with
    /// a semicolon and space separated.
    /// </summary>
    internal static void FormatStatementList(Formatter f, IReadOnlyList<Statement> statements)
    {
        f.Write($"{DisplaySeparated(statements, "; ")}");
        // We manually insert semicolon for the last statement,
        // since display_separated doesn't handle that case.
        f.Write($";");
    }
}

/// <summary>Helper used to format a slice using a separator string (e.g., <c>", "</c>).</summary>
public sealed class DisplaySeparated<T> : IDisplay
{
    private readonly IReadOnlyList<T> _slice;
    private readonly string _sep;

    internal DisplaySeparated(IReadOnlyList<T> slice, string sep)
    {
        _slice = slice;
        _sep = sep;
    }

    public void Fmt(Formatter f)
    {
        var delim = "";
        foreach (var t in _slice)
        {
            f.WriteStr(delim);
            delim = _sep;
            f.FmtInPlace(t);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
