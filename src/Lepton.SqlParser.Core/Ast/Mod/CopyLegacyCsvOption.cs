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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>A <c>CSV</c> option in <c>COPY</c> statement before PostgreSQL version 9.0.</summary>
/// <remarks><see href="https://www.postgresql.org/docs/8.4/sql-copy.html"/></remarks>
public abstract partial record CopyLegacyCsvOption : IDisplay
{
    /// <summary>HEADER</summary>
    public sealed record Header : CopyLegacyCsvOption;

    /// <summary>QUOTE [ AS ] 'quote_character'</summary>
    public sealed record Quote(char Value) : CopyLegacyCsvOption;

    /// <summary>ESCAPE [ AS ] 'escape_character'</summary>
    public sealed record Escape(char Value) : CopyLegacyCsvOption;

    /// <summary>FORCE QUOTE { column_name [, ...] | * }</summary>
    public sealed record ForceQuote(Sequence<Ident> Value) : CopyLegacyCsvOption;

    /// <summary>FORCE NOT NULL column_name [, ...]</summary>
    public sealed record ForceNotNull(Sequence<Ident> Value) : CopyLegacyCsvOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Header:
                f.Write($"HEADER");
                break;
            case Quote quote:
                f.Write($"QUOTE '{quote.Value}'");
                break;
            case Escape escape:
                f.Write($"ESCAPE '{escape.Value}'");
                break;
            case ForceQuote columns:
                f.Write($"FORCE QUOTE {DisplayCommaSeparated(columns.Value)}");
                break;
            case ForceNotNull columns:
                f.Write($"FORCE NOT NULL {DisplayCommaSeparated(columns.Value)}");
                break;
            default:
                throw new InvalidOperationException($"Variante de CopyLegacyCsvOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
