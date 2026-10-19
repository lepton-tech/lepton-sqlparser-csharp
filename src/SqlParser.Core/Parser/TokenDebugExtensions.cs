// Código próprio do port (sem origem direta no sqlparser-rs): reproduz o texto do
// `#[derive(Debug)]` de `Token` (e dos tipos que ele contém: `Word`, `Whitespace`,
// `DollarQuotedString`, `QuoteDelimitedString`), usado pelo parser em mensagens de erro com `{:?}`
// (ex.: "No infix parser for token ShiftLeft"). Ver docs/port/convencoes.md §8 e docs/port/onda-b/B1.md.

using System.Globalization;
using System.Text;
using SqlParser.Core.Ast;

namespace SqlParser.Core;

/// <summary>
/// Formato <c>Debug</c> do Rust para <see cref="Token"/> (<c>format!("{:?}", token)</c>).
/// </summary>
/// <remarks>
/// Método de extensão com o nome usado pelos outros <c>Debug</c> do port (<c>ToDebugString()</c>, como em
/// <see cref="Location"/> e <see cref="Span"/>), para não alterar o <c>Token.cs</c> do tokenizer; se um dia
/// virar membro de <see cref="Token"/>, as chamadas não mudam.
/// </remarks>
internal static class TokenDebugExtensions
{
    /// <summary>Rust: <c>format!("{:?}", token)</c> (derive <c>Debug</c> de <c>Token</c>).</summary>
    internal static string ToDebugString(this Token token)
    {
        switch (token)
        {
            case Token.Word { Value: var w }:
                return $"Word({DebugWord(w)})";
            case Token.Number n:
                return $"Number({DebugStr(n.Value)}, {DebugBool(n.Long)})";
            case Token.Char { Value: var c }:
                return $"Char({DebugChar(c)})";
            case Token.SingleQuotedString { Value: var s }:
                return Tuple1("SingleQuotedString", DebugStr(s));
            case Token.DoubleQuotedString { Value: var s }:
                return Tuple1("DoubleQuotedString", DebugStr(s));
            case Token.TripleSingleQuotedString { Value: var s }:
                return Tuple1("TripleSingleQuotedString", DebugStr(s));
            case Token.TripleDoubleQuotedString { Value: var s }:
                return Tuple1("TripleDoubleQuotedString", DebugStr(s));
            case Token.DollarQuotedString { Value: var d }:
                return Tuple1(
                    "DollarQuotedString",
                    $"DollarQuotedString {{ value: {DebugStr(d.Value)}, tag: {DebugOptionStr(d.Tag)} }}");
            case Token.SingleQuotedByteStringLiteral { Value: var s }:
                return Tuple1("SingleQuotedByteStringLiteral", DebugStr(s));
            case Token.DoubleQuotedByteStringLiteral { Value: var s }:
                return Tuple1("DoubleQuotedByteStringLiteral", DebugStr(s));
            case Token.TripleSingleQuotedByteStringLiteral { Value: var s }:
                return Tuple1("TripleSingleQuotedByteStringLiteral", DebugStr(s));
            case Token.TripleDoubleQuotedByteStringLiteral { Value: var s }:
                return Tuple1("TripleDoubleQuotedByteStringLiteral", DebugStr(s));
            case Token.SingleQuotedRawStringLiteral { Value: var s }:
                return Tuple1("SingleQuotedRawStringLiteral", DebugStr(s));
            case Token.DoubleQuotedRawStringLiteral { Value: var s }:
                return Tuple1("DoubleQuotedRawStringLiteral", DebugStr(s));
            case Token.TripleSingleQuotedRawStringLiteral { Value: var s }:
                return Tuple1("TripleSingleQuotedRawStringLiteral", DebugStr(s));
            case Token.TripleDoubleQuotedRawStringLiteral { Value: var s }:
                return Tuple1("TripleDoubleQuotedRawStringLiteral", DebugStr(s));
            case Token.NationalStringLiteral { Value: var s }:
                return Tuple1("NationalStringLiteral", DebugStr(s));
            case Token.QuoteDelimitedStringLiteral { Value: var q }:
                return Tuple1("QuoteDelimitedStringLiteral", DebugQuoteDelimitedString(q));
            case Token.NationalQuoteDelimitedStringLiteral { Value: var q }:
                return Tuple1("NationalQuoteDelimitedStringLiteral", DebugQuoteDelimitedString(q));
            case Token.EscapedStringLiteral { Value: var s }:
                return Tuple1("EscapedStringLiteral", DebugStr(s));
            case Token.UnicodeStringLiteral { Value: var s }:
                return Tuple1("UnicodeStringLiteral", DebugStr(s));
            case Token.HexStringLiteral { Value: var s }:
                return Tuple1("HexStringLiteral", DebugStr(s));
            case Token.Whitespace { Value: var ws }:
                return Tuple1("Whitespace", DebugWhitespace(ws));
            case Token.Placeholder { Value: var s }:
                return Tuple1("Placeholder", DebugStr(s));
            case Token.CustomBinaryOperator { Value: var s }:
                return Tuple1("CustomBinaryOperator", DebugStr(s));
            default:
                // Variantes unitárias: o Debug derivado escreve só o nome da variante, que é o nome
                // do record aninhado (convencoes.md §6).
                return token.GetType().Name;
        }
    }

    private static string Tuple1(string name, string field) => name + "(" + field + ")";

    private static string DebugBool(bool b) => b ? "true" : "false";

    // derive(Debug) de `Word { value, quote_style, keyword }`; `Keyword` usa o nome da variante (caso 46).
    private static string DebugWord(Word w) =>
        $"Word {{ value: {DebugStr(w.Value)}, quote_style: {DebugOptionChar(w.QuoteStyle)}, keyword: {w.Keyword.ToString()} }}";

    private static string DebugQuoteDelimitedString(QuoteDelimitedString q) =>
        $"QuoteDelimitedString {{ start_quote: {DebugChar(q.StartQuote.ToString())}, value: {DebugStr(q.Value)}, end_quote: {DebugChar(q.EndQuote.ToString())} }}";

    private static string DebugWhitespace(Whitespace ws)
    {
        switch (ws)
        {
            case Whitespace.SingleLineComment c:
                return $"SingleLineComment {{ comment: {DebugStr(c.Comment)}, prefix: {DebugStr(c.Prefix)} }}";
            case Whitespace.MultiLineComment m:
                return Tuple1("MultiLineComment", DebugStr(m.Value));
            default:
                return ws.GetType().Name;
        }
    }

    private static string DebugOptionChar(char? c) => c is { } ch ? $"Some({DebugChar(ch.ToString())})" : "None";

    private static string DebugOptionStr(string? s) => s is { } str ? $"Some({DebugStr(str)})" : "None";

    // `<str as Debug>`: entre aspas duplas, com `"` e `\` escapados. `internal` (B3): também usado
    // por `parse_literal_char` (`format!("Expect a char, found {s:?}")`).
    internal static string DebugStr(string s)
    {
        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var c in s)
        {
            AppendEscapeDebug(sb, c, escapeDoubleQuote: true, escapeSingleQuote: false);
        }
        sb.Append('"');
        return sb.ToString();
    }

    // `<char as Debug>`: entre aspas simples, com `'` e `\` escapados. Recebe o scalar value como string
    // (1 ou 2 unidades UTF-16, como `Token.Char`).
    private static string DebugChar(string c)
    {
        var sb = new StringBuilder();
        sb.Append('\'');
        foreach (var unit in c)
        {
            AppendEscapeDebug(sb, unit, escapeDoubleQuote: false, escapeSingleQuote: true);
        }
        sb.Append('\'');
        return sb.ToString();
    }

    // Aproximação de `char::escape_debug`: escapes nomeados do Rust e `\u{..}` (hexadecimal minúsculo)
    // para caracteres de controle. O Rust também escapa outros caracteres "não imprimíveis" e marcas
    // combinantes iniciais pela tabela Unicode própria; aqui só os de controle (docs/port/onda-b/B1.md).
    // PORT-DEVIATION DIV-007: só escapes nomeados e `char.IsControl`, percorrendo unidades UTF-16; o Rust
    // usa as tabelas Unicode próprias (Grapheme_Extend, espaços especiais) e trata scalar values.
    private static void AppendEscapeDebug(StringBuilder sb, char c, bool escapeDoubleQuote, bool escapeSingleQuote)
    {
        const char backslash = (char)92;
        switch (c)
        {
            case '\t':
                sb.Append(backslash).Append('t');
                break;
            case '\r':
                sb.Append(backslash).Append('r');
                break;
            case '\n':
                sb.Append(backslash).Append('n');
                break;
            case '\0':
                sb.Append(backslash).Append('0');
                break;
            case (char)92:
                sb.Append(backslash).Append(backslash);
                break;
            case '"' when escapeDoubleQuote:
                sb.Append(backslash).Append('"');
                break;
            case '\'' when escapeSingleQuote:
                sb.Append(backslash).Append('\'');
                break;
            default:
                if (char.IsControl(c))
                {
                    sb.Append(backslash).Append('u').Append('{')
                        .Append(((int)c).ToString("x", CultureInfo.InvariantCulture))
                        .Append('}');
                }
                else
                {
                    sb.Append(c);
                }
                break;
        }
    }
}
