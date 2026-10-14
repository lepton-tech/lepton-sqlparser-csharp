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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>An explicit <c>VALUES</c> clause and its rows.</summary>
/// <param name="ExplicitRow">
/// Was there an explicit <c>ROW</c> keyword (MySQL)?
/// <see href="https://dev.mysql.com/doc/refman/8.0/en/values.html"/>
/// </param>
/// <param name="ValueKeyword">
/// <c>true</c> if <c>VALUE</c> (singular) keyword was used instead of <c>VALUES</c>.
/// <see href="https://dev.mysql.com/doc/refman/9.2/en/insert.html"/>
/// </param>
/// <param name="Rows">The list of rows, each row is a list of expressions.</param>
public sealed partial record Values(bool ExplicitRow, bool ValueKeyword, Sequence<Parens<Sequence<Expr>>> Rows) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (ValueKeyword)
        {
            case true:
                f.WriteStr("VALUE");
                break;
            case false:
                f.WriteStr("VALUES");
                break;
        }
        var prefix = ExplicitRow ? "ROW" : "";
        var delim = "";
        foreach (var row in Rows)
        {
            f.WriteStr(delim);
            delim = ",";
            SpaceOrNewline.Instance.Fmt(f);
            // Indent(format_args!("{prefix}({})", display_comma_separated(row))).fmt(f)
            new Indent<FormatArgs>(new FormatArgs(args => args.Write($"{prefix}({DisplayCommaSeparated(row.Content)})"))).Fmt(f);
        }
    }

    public override string ToString() => Formatter.Format(this);

    /// <summary>
    /// Rust: o valor <c>format_args!(...)</c> (<c>core::fmt::Arguments</c>) usado como <c>Display</c>.
    /// Como no Rust, cada lacuna é formatada com flags novos (sem <c>#</c>) no destino do formatador
    /// recebido; as quebras de linha passam pelo <c>Indent</c> que o envolve (A7.md, "Decisões").
    /// </summary>
    private sealed class FormatArgs : IDisplay
    {
        private readonly Action<Formatter> _write;

        public FormatArgs(Action<Formatter> write) => _write = write;

        public void Fmt(Formatter f) => _write(f);

        public override string ToString() => Formatter.Format(this);
    }
}
