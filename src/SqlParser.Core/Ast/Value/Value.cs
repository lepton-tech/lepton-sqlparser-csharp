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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/value.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>Primitive SQL values such as number and string</summary>
// As funções livres do módulo `value` (`escape_quoted_string` etc.) são membros estáticos
// deste tipo homônimo (convencoes.md §13, casos 5 e 29). `#[derive(PartialOrd, Ord)]` não é
// traduzido (caso 23).
public abstract partial record Value : IDisplay
{
    /// <summary>Numeric literal</summary>
    // Variante sem a feature `bigdecimal` (a padrão): `Number(String, bool)`. Os nomes dos
    // campos seguem `Token::Number` (nomes.md): `Long` é o sufixo `L`.
    public sealed record Number(string Value, bool Long) : Value;

    /// <summary>'string value'</summary>
    public sealed record SingleQuotedString(string Value) : Value;

    /// <summary>Dollar-quoted string literal, e.g. <c>$$...$$</c> or <c>$tag$...$tag$</c> (Postgres syntax).</summary>
    public sealed record DollarQuotedString(Ast.DollarQuotedString Value) : Value;

    /// <summary>
    /// Triple single quoted strings: Example '''abc'''
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">BigQuery</see>
    /// </summary>
    public sealed record TripleSingleQuotedString(string Value) : Value;

    /// <summary>
    /// Triple double quoted strings: Example """abc"""
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">BigQuery</see>
    /// </summary>
    public sealed record TripleDoubleQuotedString(string Value) : Value;

    /// <summary>
    /// e'string value' (postgres extension)
    /// See <see href="https://www.postgresql.org/docs/8.3/sql-syntax-lexical.html#SQL-SYNTAX-STRINGS">Postgres docs</see>
    /// for more details.
    /// </summary>
    public sealed record EscapedStringLiteral(string Value) : Value;

    /// <summary>
    /// u&amp;'string value' (postgres extension)
    /// See <see href="https://www.postgresql.org/docs/current/sql-syntax-lexical.html#SQL-SYNTAX-STRINGS-UESCAPE">Postgres docs</see>
    /// for more details.
    /// </summary>
    public sealed record UnicodeStringLiteral(string Value) : Value;

    /// <summary>B'string value'</summary>
    public sealed record SingleQuotedByteStringLiteral(string Value) : Value;

    /// <summary>B"string value"</summary>
    public sealed record DoubleQuotedByteStringLiteral(string Value) : Value;

    /// <summary>
    /// Triple single quoted literal with byte string prefix. Example <c>B'''abc'''</c>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">BigQuery</see>
    /// </summary>
    public sealed record TripleSingleQuotedByteStringLiteral(string Value) : Value;

    /// <summary>
    /// Triple double quoted literal with byte string prefix. Example <c>B"""abc"""</c>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">BigQuery</see>
    /// </summary>
    public sealed record TripleDoubleQuotedByteStringLiteral(string Value) : Value;

    /// <summary>
    /// Single quoted literal with raw string prefix. Example <c>R'abc'</c>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">BigQuery</see>
    /// </summary>
    public sealed record SingleQuotedRawStringLiteral(string Value) : Value;

    /// <summary>
    /// Double quoted literal with raw string prefix. Example <c>R"abc"</c>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">BigQuery</see>
    /// </summary>
    public sealed record DoubleQuotedRawStringLiteral(string Value) : Value;

    /// <summary>
    /// Triple single quoted literal with raw string prefix. Example <c>R'''abc'''</c>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">BigQuery</see>
    /// </summary>
    public sealed record TripleSingleQuotedRawStringLiteral(string Value) : Value;

    /// <summary>
    /// Triple double quoted literal with raw string prefix. Example <c>R"""abc"""</c>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#quoted_literals">BigQuery</see>
    /// </summary>
    public sealed record TripleDoubleQuotedRawStringLiteral(string Value) : Value;

    /// <summary>N'string value'</summary>
    public sealed record NationalStringLiteral(string Value) : Value;

    /// <summary>
    /// Quote delimited literal. Examples <c>Q'{ab'c}'</c>, <c>Q'|ab'c|'</c>, <c>Q'|ab|c|'</c>
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Literals.html#GUID-1824CBAA-6E16-4921-B2A6-112FB02248DA">Oracle</see>
    /// </summary>
    public sealed record QuoteDelimitedStringLiteral(QuoteDelimitedString Value) : Value;

    /// <summary>
    /// "National" quote delimited literal. Examples <c>Q'{ab'c}'</c>, <c>Q'|ab'c|'</c>, <c>Q'|ab|c|'</c>
    /// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Literals.html#GUID-1824CBAA-6E16-4921-B2A6-112FB02248DA">Oracle</see>
    /// </summary>
    public sealed record NationalQuoteDelimitedStringLiteral(QuoteDelimitedString Value) : Value;

    /// <summary>X'hex value'</summary>
    public sealed record HexStringLiteral(string Value) : Value;

    /// <summary>Double quoted string literal, e.g. <c>"abc"</c>.</summary>
    public sealed record DoubleQuotedString(string Value) : Value;

    /// <summary>Boolean value true or false</summary>
    public sealed record Boolean(bool Value) : Value;

    /// <summary><c>NULL</c> value</summary>
    public sealed record Null : Value;

    /// <summary><c>?</c> or <c>$</c> Prepared statement arg placeholder</summary>
    public sealed record Placeholder(string Value) : Value;

    // Rust: `impl From<ValueWithSpan> for Value`.
    public static Value From(ValueWithSpan value) => value.Value;

    /// <summary>If the underlying literal is a string, regardless of quote style, returns the associated string value</summary>
    public string? IntoString() =>
        this switch
        {
            SingleQuotedString s => s.Value,
            DoubleQuotedString s => s.Value,
            TripleSingleQuotedString s => s.Value,
            TripleDoubleQuotedString s => s.Value,
            SingleQuotedByteStringLiteral s => s.Value,
            DoubleQuotedByteStringLiteral s => s.Value,
            TripleSingleQuotedByteStringLiteral s => s.Value,
            TripleDoubleQuotedByteStringLiteral s => s.Value,
            SingleQuotedRawStringLiteral s => s.Value,
            DoubleQuotedRawStringLiteral s => s.Value,
            TripleSingleQuotedRawStringLiteral s => s.Value,
            TripleDoubleQuotedRawStringLiteral s => s.Value,
            EscapedStringLiteral s => s.Value,
            UnicodeStringLiteral s => s.Value,
            NationalStringLiteral s => s.Value,
            HexStringLiteral s => s.Value,
            DollarQuotedString s => s.Value.Value,
            QuoteDelimitedStringLiteral s => s.Value.Value,
            NationalQuoteDelimitedStringLiteral s => s.Value.Value,
            _ => null,
        };

    /// <summary>Attach the provided <c>span</c> to this <c>Value</c> and return <c>ValueWithSpan</c>.</summary>
    public ValueWithSpan WithSpan(Span span) => new ValueWithSpan(this, span);

    /// <summary>Convenience for attaching an empty span to this <c>Value</c>.</summary>
    public ValueWithSpan WithEmptySpan() => WithSpan(Span.Empty());

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Number v:
                f.Write($"{v.Value}{(v.Long ? "L" : "")}");
                break;
            case DoubleQuotedString v:
                f.Write($"\"{EscapeDoubleQuoteString(v.Value)}\"");
                break;
            case SingleQuotedString v:
                f.Write($"'{EscapeSingleQuoteString(v.Value)}'");
                break;
            case TripleSingleQuotedString v:
                f.Write($"'''{v.Value}'''");
                break;
            case TripleDoubleQuotedString v:
                f.Write($"\"\"\"{v.Value}\"\"\"");
                break;
            case DollarQuotedString v:
                f.Write($"{v.Value}");
                break;
            case EscapedStringLiteral v:
                f.Write($"E'{EscapeEscapedString(v.Value)}'");
                break;
            case UnicodeStringLiteral v:
                f.Write($"U&'{EscapeUnicodeString(v.Value)}'");
                break;
            case NationalStringLiteral v:
                f.Write($"N'{EscapeSingleQuoteString(v.Value)}'");
                break;
            case QuoteDelimitedStringLiteral v:
                v.Value.Fmt(f);
                break;
            case NationalQuoteDelimitedStringLiteral v:
                f.Write($"N{v.Value}");
                break;
            case HexStringLiteral v:
                f.Write($"X'{v.Value}'");
                break;
            case Boolean v:
                f.Write($"{v.Value}");
                break;
            case SingleQuotedByteStringLiteral v:
                f.Write($"B'{v.Value}'");
                break;
            case DoubleQuotedByteStringLiteral v:
                f.Write($"B\"{v.Value}\"");
                break;
            case TripleSingleQuotedByteStringLiteral v:
                f.Write($"B'''{v.Value}'''");
                break;
            case TripleDoubleQuotedByteStringLiteral v:
                f.Write($"B\"\"\"{v.Value}\"\"\"");
                break;
            case SingleQuotedRawStringLiteral v:
                f.Write($"R'{v.Value}'");
                break;
            case DoubleQuotedRawStringLiteral v:
                f.Write($"R\"{v.Value}\"");
                break;
            case TripleSingleQuotedRawStringLiteral v:
                f.Write($"R'''{v.Value}'''");
                break;
            case TripleDoubleQuotedRawStringLiteral v:
                f.Write($"R\"\"\"{v.Value}\"\"\"");
                break;
            case Null:
                f.Write($"NULL");
                break;
            case Placeholder v:
                f.Write($"{v.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de Value não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);

    // Funções livres de value.rs (convencoes.md §13, caso 5), na ordem do Rust.

    /// <summary>
    /// Return a helper which formats <c>string</c> for inclusion inside a quoted
    /// literal that uses <c>quote</c> as the delimiter.
    /// </summary>
    // O `quote: char` do Rust pode estar fora do BMP (o teste usa `'🦀'`), então vira `int`
    // com o scalar value (convencoes.md §13, caso 18). Um `char` C# converte implicitamente.
    public static EscapeQuotedString EscapeQuotedString(string @string, int quote) =>
        new EscapeQuotedString(@string, quote);

    /// <summary>Convenience wrapper for escaping strings for single-quoted literals (<c>'</c>).</summary>
    public static EscapeQuotedString EscapeSingleQuoteString(string s) => EscapeQuotedString(s, '\'');

    /// <summary>Convenience wrapper for escaping strings for double-quoted literals (<c>"</c>).</summary>
    public static EscapeQuotedString EscapeDoubleQuoteString(string s) => EscapeQuotedString(s, '\"');

    /// <summary>
    /// Return a helper which escapes characters for string literals that use
    /// PostgreSQL-style escaped string literals (e.g. <c>E'...'</c>).
    /// </summary>
    public static EscapeEscapedStringLiteral EscapeEscapedString(string s) => new EscapeEscapedStringLiteral(s);

    /// <summary>
    /// Return a helper which escapes non-ASCII characters using <c>\XXXX</c> or
    /// <c>\+XXXXXX</c> Unicode escape formats (used for <c>U&amp;'...'</c> style literals).
    /// </summary>
    public static EscapeUnicodeStringLiteral EscapeUnicodeString(string s) => new EscapeUnicodeStringLiteral(s);
}
