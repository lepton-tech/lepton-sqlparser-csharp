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

namespace LeptonTech.SqlParser.Core;

/// <summary>
/// Utilities for formatting SQL AST nodes with pretty printing support.
/// </summary>
/// <remarks>
/// The module provides formatters that implement the <c>Display</c> trait with support
/// for both regular (<c>{}</c>) and pretty (<c>{:#}</c>) formatting modes. Pretty printing
/// adds proper indentation and line breaks to make SQL statements more readable.
/// </remarks>
// Funções e constantes livres do módulo `display_utils` (convencoes.md §13, caso 29).
internal static class DisplayUtils
{
    // Nome mantido em MAIÚSCULAS: é a constante privada `INDENT` do Rust, usada só neste módulo.
    internal const string INDENT = "  ";

    /// <summary>Displays a whitespace, followed by a comma-separated list that is indented when pretty-printed.</summary>
    public static void IndentedList<T>(Formatter f, IReadOnlyList<T> items)
    {
        SpaceOrNewline.Instance.Fmt(f);
        new Indent<DisplayCommaSeparated<T>>(new DisplayCommaSeparated<T>(items)).Fmt(f);
    }
}
