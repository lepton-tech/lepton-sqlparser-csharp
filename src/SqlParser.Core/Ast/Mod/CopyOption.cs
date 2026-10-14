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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>An option in <c>COPY</c> statement.</summary>
/// <remarks><see href="https://www.postgresql.org/docs/14/sql-copy.html"/></remarks>
public abstract partial record CopyOption : IDisplay
{
    /// <summary>FORMAT format_name</summary>
    public sealed record Format(Ident Value) : CopyOption;

    /// <summary>FREEZE [ boolean ]</summary>
    public sealed record Freeze(bool Value) : CopyOption;

    /// <summary>DELIMITER 'delimiter_character'</summary>
    public sealed record Delimiter(char Value) : CopyOption;

    /// <summary>NULL 'null_string'</summary>
    public sealed record Null(string Value) : CopyOption;

    /// <summary>HEADER [ boolean ]</summary>
    public sealed record Header(bool Value) : CopyOption;

    /// <summary>QUOTE 'quote_character'</summary>
    public sealed record Quote(char Value) : CopyOption;

    /// <summary>ESCAPE 'escape_character'</summary>
    public sealed record Escape(char Value) : CopyOption;

    /// <summary>FORCE_QUOTE { ( column_name [, ...] ) | * }</summary>
    public sealed record ForceQuote(Sequence<Ident> Value) : CopyOption;

    /// <summary>FORCE_NOT_NULL ( column_name [, ...] )</summary>
    public sealed record ForceNotNull(Sequence<Ident> Value) : CopyOption;

    /// <summary>FORCE_NULL ( column_name [, ...] )</summary>
    public sealed record ForceNull(Sequence<Ident> Value) : CopyOption;

    /// <summary>ENCODING 'encoding_name'</summary>
    public sealed record Encoding(string Value) : CopyOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Format name:
                f.Write($"FORMAT {name.Value}");
                break;
            case Freeze { Value: true }:
                f.Write($"FREEZE");
                break;
            case Freeze { Value: false }:
                f.Write($"FREEZE FALSE");
                break;
            case Delimiter delimiter:
                f.Write($"DELIMITER '{delimiter.Value}'");
                break;
            case Null nullString:
                f.Write($"NULL '{Ast.Value.EscapeSingleQuoteString(nullString.Value)}'");
                break;
            case Header { Value: true }:
                f.Write($"HEADER");
                break;
            case Header { Value: false }:
                f.Write($"HEADER FALSE");
                break;
            case Quote quote:
                f.Write($"QUOTE '{quote.Value}'");
                break;
            case Escape escape:
                f.Write($"ESCAPE '{escape.Value}'");
                break;
            case ForceQuote forceQuote:
                f.Write($"FORCE_QUOTE ({DisplayCommaSeparated(forceQuote.Value)})");
                break;
            case ForceNotNull forceNotNull:
                f.Write($"FORCE_NOT_NULL ({DisplayCommaSeparated(forceNotNull.Value)})");
                break;
            case ForceNull forceNull:
                f.Write($"FORCE_NULL ({DisplayCommaSeparated(forceNull.Value)})");
                break;
            case Encoding encoding:
                f.Write($"ENCODING '{Ast.Value.EscapeSingleQuoteString(encoding.Value)}'");
                break;
            default:
                throw new InvalidOperationException($"Variante de CopyOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
