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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Free functions of the <c>ddl</c> module (<c>src/ast/ddl.rs</c>).</summary>
// Funções livres do módulo `ddl` (`src/ast/ddl.rs`). O módulo não tem um tipo homônimo,
// então as funções ficam em `DdlModule` (convencoes.md §13, caso 29). Use
// `using static LeptonTech.SqlParser.Core.Ast.DdlModule;` para chamá-las sem prefixo, como no Rust.
// Este arquivo porta as funções livres de `ddl.rs` até a linha 2909.
public static partial class DdlModule
{
    // Rust: #[must_use]
    //       pub(crate) fn display_constraint_name(name: &'_ Option<Ident>) -> impl fmt::Display + '_
    // O tipo de retorno opaco (`impl fmt::Display`) vira IDisplay; o struct local vira classe aninhada privada.
    internal static IDisplay DisplayConstraintName(Ident? name) => new ConstraintName(name);

    // Rust: struct ConstraintName<'a>(&'a Option<Ident>); (local a display_constraint_name)
    private sealed class ConstraintName : IDisplay
    {
        public ConstraintName(Ident? value) => Value = value;

        // Rust: campo `.0`.
        public Ident? Value { get; }

        public void Fmt(Formatter f)
        {
            if (Value is { } name)
            {
                f.Write($"CONSTRAINT {name} ");
            }
        }

        public override string ToString() => Formatter.Format(this);
    }

    /// <summary>
    /// If <c>option</c> is
    /// <list type="bullet">
    /// <item><c>Some(inner)</c> =&gt; create display struct for <c>"{prefix}{inner}{postfix}"</c></item>
    /// <item><c>_</c> =&gt; do nothing</item>
    /// </list>
    /// </summary>
    // Rust: #[must_use]
    //       pub(crate) fn display_option<'a, T: fmt::Display>(prefix: &'a str, postfix: &'a str, option: &'a Option<T>)
    //           -> impl fmt::Display + 'a
    internal static IDisplay DisplayOption<T>(string prefix, string postfix, T? option) =>
        new OptionDisplay<T>(prefix, postfix, option);

    // Rust: struct OptionDisplay<'a, T>(&'a str, &'a str, &'a Option<T>); (local a display_option)
    private sealed class OptionDisplay<T> : IDisplay
    {
        public OptionDisplay(string prefix, string postfix, T? option)
        {
            Prefix = prefix;
            Postfix = postfix;
            Option = option;
        }

        // Rust: campo `.0`.
        public string Prefix { get; }

        // Rust: campo `.1`.
        public string Postfix { get; }

        // Rust: campo `.2`.
        public T? Option { get; }

        public void Fmt(Formatter f)
        {
            if (Option is { } inner)
            {
                var (prefix, postfix) = (Prefix, Postfix);
                f.Write($"{prefix}{inner}{postfix}");
            }
        }

        public override string ToString() => Formatter.Format(this);
    }

    /// <summary>
    /// If <c>option</c> is
    /// <list type="bullet">
    /// <item><c>Some(inner)</c> =&gt; create display struct for <c>" {inner}"</c></item>
    /// <item><c>_</c> =&gt; do nothing</item>
    /// </list>
    /// </summary>
    // Rust: #[must_use]
    //       pub(crate) fn display_option_spaced<T: fmt::Display>(option: &Option<T>) -> impl fmt::Display + '_
    internal static IDisplay DisplayOptionSpaced<T>(T? option) => DisplayOption(" ", "", option);
}
