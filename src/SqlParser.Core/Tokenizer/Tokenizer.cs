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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/tokenizer.rs.
// Modificado: tradução de Rust para C#.

// SQL Tokenizer
//
// The tokenizer (a.k.a. lexer) converts a string into a sequence of tokens.
//
// The tokens then form the input for the parser, which outputs an Abstract Syntax Tree (AST).

using System.Globalization;
using System.Text;
using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

/// <summary>SQL Tokenizer</summary>
/// <remarks>
/// As funções livres do módulo <c>tokenizer</c> do Rust (<c>keyword_lookup</c>, <c>peeking_take_while</c>,
/// <c>unescape_single_quoted_string</c>...) são membros estáticos desta classe (convencoes.md §13 #5).
/// </remarks>
public class Tokenizer
{
    /// <summary>Case-insensitive keyword lookup using binary search over <c>ALL_KEYWORDS</c>.</summary>
    // Rust: função livre privada do módulo (declarada antes de `struct Tokenizer`) → internal static.
    // A comparação é a do Rust: bytes do keyword contra `to_ascii_uppercase` dos bytes da palavra.
    // Comparar unidades UTF-16 dá a mesma ordem: as keywords são ASCII, e qualquer caractere não
    // ASCII é maior que todo byte ASCII tanto em UTF-8 quanto em UTF-16.
    internal static Keyword KeywordLookup(string word, char? quoteStyle)
    {
        if (quoteStyle is not null)
        {
            return Keyword.NoKeyword;
        }

        var allKeywords = Keywords.AllKeywordsArray;
        var lo = 0;
        var hi = allKeywords.Length;
        while (lo < hi)
        {
            var mid = lo + ((hi - lo) / 2);
            var cmp = CompareKeywordProbe(allKeywords[mid], word);
            if (cmp == 0)
            {
                return Keywords.AllKeywordsIndexArray[mid];
            }
            if (cmp < 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }
        return Keyword.NoKeyword;
    }

    // O closure de `binary_search_by` em `keyword_lookup`.
    private static int CompareKeywordProbe(string probe, string word)
    {
        var n = Math.Min(probe.Length, word.Length);
        for (var i = 0; i < n; i++)
        {
            var cmp = probe[i].CompareTo(RustChar.ToAsciiUppercase(word[i]));
            if (cmp != 0)
            {
                return cmp;
            }
        }
        return probe.Length.CompareTo(word.Length);
    }

    private readonly Dialect _dialect;
    private readonly string _query;

    /// <summary>
    /// If true (the default), the tokenizer will un-escape literal
    /// SQL strings See <see cref="WithUnescape"/> for more details.
    /// </summary>
    private bool _unescape;

    /// <summary>Create a new SQL tokenizer for the specified SQL statement</summary>
    /// <example>
    /// <code>
    /// var dialect = new GenericDialect();
    /// var query = "SELECT 'foo'";
    ///
    /// // Parsing the query
    /// var tokens = new Tokenizer(dialect, query).Tokenize();
    ///
    /// // tokens == [
    /// //   Token.MakeWord("SELECT", null),
    /// //   new Token.Whitespace(new Whitespace.Space()),
    /// //   new Token.SingleQuotedString("foo"),
    /// // ]
    /// </code>
    /// </example>
    public Tokenizer(Dialect dialect, string query)
    {
        _dialect = dialect;
        _query = query;
        _unescape = true;
    }

    /// <summary>Set unescape mode</summary>
    /// <remarks>
    /// When true (default) the tokenizer unescapes literal values
    /// (for example, <c>""</c> in SQL is unescaped to the literal <c>"</c>).
    ///
    /// When false, the tokenizer provides the raw strings as provided
    /// in the query.  This can be helpful for programs that wish to
    /// recover the *exact* original query text without normalizing
    /// the escaping
    /// </remarks>
    /// <example>
    /// <code>
    /// var dialect = new GenericDialect();
    /// var query = "\"Foo \"\" Bar\"";
    /// var unescaped = Token.MakeWord("Foo \" Bar", '"');
    /// var original  = Token.MakeWord("Foo \"\" Bar", '"');
    ///
    /// // Parsing with unescaping (default)
    /// var tokens = new Tokenizer(dialect, query).Tokenize();
    /// // tokens == [unescaped]
    ///
    /// // Parsing with unescape = false
    /// tokens = new Tokenizer(dialect, query).WithUnescape(false).Tokenize();
    /// // tokens == [original]
    /// </code>
    /// </example>
    // Rust: `fn with_unescape(mut self, ..) -> Self` → altera esta instância e a devolve (convencoes.md §13).
    public Tokenizer WithUnescape(bool unescape)
    {
        _unescape = unescape;
        return this;
    }

    /// <summary>Tokenize the statement and produce a vector of tokens</summary>
    /// <exception cref="TokenizerException">Rust: <c>Err(TokenizerError)</c>.</exception>
    public List<Token> Tokenize()
    {
        var twl = TokenizeWithLocation();
        var tokens = new List<Token>(twl.Count);
        foreach (var t in twl)
        {
            tokens.Add(t.Token);
        }
        return tokens;
    }

    /// <summary>Tokenize the statement and produce a vector of tokens with location information</summary>
    /// <exception cref="TokenizerException">Rust: <c>Err(TokenizerError)</c>.</exception>
    public List<TokenWithSpan> TokenizeWithLocation()
    {
        var tokens = new List<TokenWithSpan>();
        TokenizeWithLocationIntoBuf(tokens);
        return tokens;
    }

    /// <summary>
    /// Tokenize the statement and append tokens with location information into the provided buffer.
    /// If an error is thrown, the buffer will contain all tokens that were successfully parsed before the error.
    /// </summary>
    /// <exception cref="TokenizerException">Rust: <c>Err(TokenizerError)</c>.</exception>
    public void TokenizeWithLocationIntoBuf(List<TokenWithSpan> buf)
    {
        TokenizeWithLocationIntoBufWithMapper(buf, token => token);
    }

    /// <summary>
    /// Tokenize the statement and produce a vector of tokens, mapping each token
    /// with provided <c>mapper</c>
    /// </summary>
    /// <exception cref="TokenizerException">Rust: <c>Err(TokenizerError)</c>.</exception>
    public void TokenizeWithLocationIntoBufWithMapper(
        List<TokenWithSpan> buf,
        Func<TokenWithSpan, TokenWithSpan> mapper)
    {
        var state = new State(_query, 1, 1);

        var location = state.Location();
        while (NextToken(state, buf.Count > 0 ? buf[buf.Count - 1].Token : null) is { } token)
        {
            var span = location.SpanTo(state.Location());

            // Check if this is a multiline comment hint that should be expanded
            if (token is Token.Whitespace { Value: Whitespace.MultiLineComment { Value: var comment } }
                && _dialect.SupportsMultilineCommentHints
                && comment.StartsWith("!", StringComparison.Ordinal))
            {
                // Re-tokenize the hints and add them to the buffer
                TokenizeCommentHints(comment, span, buf, mapper);
            }
            else
            {
                buf.Add(mapper(new TokenWithSpan(token, span)));
            }

            location = state.Location();
        }
    }

    /// <summary>
    /// Re-tokenize optimizer hints from a multiline comment and add them to the buffer.
    /// For example, <c>/*!50110 KEY_BLOCK_SIZE = 1024*/</c> becomes tokens for <c>KEY_BLOCK_SIZE = 1024</c>
    /// </summary>
    private void TokenizeCommentHints(
        string comment,
        Span span,
        List<TokenWithSpan> buf,
        Func<TokenWithSpan, TokenWithSpan> mapper)
    {
        // Strip the leading '!' and any version digits (e.g., "50110")
        var hintContent = comment.StartsWith("!", StringComparison.Ordinal) ? comment.Substring(1) : comment;
        hintContent = hintContent.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

        // If there's no content after stripping, nothing to tokenize
        if (hintContent.Length == 0)
        {
            return;
        }

        // Create a new tokenizer for the hint content
        var inner = new Tokenizer(_dialect, hintContent).WithUnescape(_unescape);

        // Create a state for tracking position within the hint
        var state = new State(hintContent, span.Start.Line, span.Start.Column);

        // Tokenize the hint content and add tokens to the buffer
        var location = state.Location();
        while (inner.NextToken(state, buf.Count > 0 ? buf[buf.Count - 1].Token : null) is { } token)
        {
            var tokenSpan = location.SpanTo(state.Location());
            buf.Add(mapper(new TokenWithSpan(token, tokenSpan)));
            location = state.Location();
        }
    }

    // Tokenize the identifier or keywords in `ch`
    // Rust: `ch: impl IntoIterator<Item = char>` → char[].
    private Token? TokenizeIdentifierOrKeyword(char[] ch, State chars)
    {
        chars.Next(); // consume the first char
        var first = new string(ch);
        // No Rust, o último elemento de `ch` é um scalar value completo. Em C# ele pode ser a metade alta
        // de um par substituto (ramos `[ch, *sch]` de '%', '#', '@@' e '@'). Nesse caso, consome também a
        // metade baixa, para o par não ficar dividido entre dois tokens (convencoes.md §10; DIV-003).
        if (ch.Length > 0 && char.IsHighSurrogate(ch[ch.Length - 1]) && chars.Peek() is { } low && char.IsLowSurrogate(low))
        {
            chars.Next();
            first += low;
        }
        var word = TokenizeWord(first, chars);

        // TODO: implement parsing of exponent here
        if (AllAsciiDigitsOrPeriod(word))
        {
            var innerState = new State(word, 0, 0);
            var s = PeekingTakeWhile(innerState, c => (c >= '0' && c <= '9') || c == '.');
            var s2 = PeekingTakeWhile(chars, c => (c >= '0' && c <= '9') || c == '.');
            s += s2;
            return new Token.Number(s, false);
        }

        return Token.MakeWordOwned(word, null);
    }

    // `word.chars().all(|x| x.is_ascii_digit() || x == '.')`
    private static bool AllAsciiDigitsOrPeriod(string word)
    {
        foreach (var x in word)
        {
            if (!(RustChar.IsAsciiDigit(x) || x == '.'))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Get the next token or return None</summary>
    // Rust: `match chars.peek() { Some(&ch) => match ch { ... }, None => Ok(None) }`. O `match` com
    // guardas vira UM switch com `case ... when`, na mesma ordem (a guarda que falha cai no próximo caso).
    private Token? NextToken(State chars, Token? prevToken)
    {
        if (chars.Peek() is not { } ch)
        {
            return null;
        }

        switch (ch)
        {
            case ' ':
                return ConsumeAndReturn(chars, new Token.Whitespace(new Whitespace.Space()));
            case '\t':
                return ConsumeAndReturn(chars, new Token.Whitespace(new Whitespace.Tab()));
            case '\n':
                return ConsumeAndReturn(chars, new Token.Whitespace(new Whitespace.Newline()));
            case '\r':
            {
                // Emit a single Whitespace::Newline token for \r and \r\n
                chars.Next();
                if (chars.Peek() == '\n')
                {
                    chars.Next();
                }
                return new Token.Whitespace(new Whitespace.Newline());
            }
            // BigQuery and MySQL use b or B for byte string literal, Postgres for bit strings
            case 'B' or 'b' when _dialect is BigQueryDialect or PostgreSqlDialect or MySqlDialect or GenericDialect:
            {
                var b = ch;
                chars.Next(); // consume
                switch (chars.Peek())
                {
                    case '\'':
                    {
                        if (_dialect.SupportsTripleQuotedString)
                        {
                            return TokenizeSingleOrTripleQuotedString(
                                chars,
                                '\'',
                                false,
                                v => new Token.SingleQuotedByteStringLiteral(v),
                                v => new Token.TripleSingleQuotedByteStringLiteral(v));
                        }
                        var s = TokenizeSingleQuotedString(chars, '\'', false);
                        return new Token.SingleQuotedByteStringLiteral(s);
                    }
                    case '"':
                    {
                        if (_dialect.SupportsTripleQuotedString)
                        {
                            return TokenizeSingleOrTripleQuotedString(
                                chars,
                                '"',
                                false,
                                v => new Token.DoubleQuotedByteStringLiteral(v),
                                v => new Token.TripleDoubleQuotedByteStringLiteral(v));
                        }
                        var s = TokenizeSingleQuotedString(chars, '"', false);
                        return new Token.DoubleQuotedByteStringLiteral(s);
                    }
                    default:
                    {
                        // regular identifier starting with an "b" or "B"
                        var s = TokenizeWord(b.ToString(), chars);
                        return Token.MakeWordOwned(s, null);
                    }
                }
            }
            // BigQuery uses r or R for raw string literal
            case 'R' or 'r' when _dialect is BigQueryDialect or GenericDialect:
            {
                var b = ch;
                chars.Next(); // consume
                switch (chars.Peek())
                {
                    case '\'':
                        return TokenizeSingleOrTripleQuotedString(
                            chars,
                            '\'',
                            false,
                            v => new Token.SingleQuotedRawStringLiteral(v),
                            v => new Token.TripleSingleQuotedRawStringLiteral(v));
                    case '"':
                        return TokenizeSingleOrTripleQuotedString(
                            chars,
                            '"',
                            false,
                            v => new Token.DoubleQuotedRawStringLiteral(v),
                            v => new Token.TripleDoubleQuotedRawStringLiteral(v));
                    default:
                    {
                        // regular identifier starting with an "r" or "R"
                        var s = TokenizeWord(b.ToString(), chars);
                        return Token.MakeWordOwned(s, null);
                    }
                }
            }
            // Redshift uses lower case n for national string literal
            case 'N' or 'n':
            {
                var n = ch;
                chars.Next(); // consume, to check the next char
                switch (chars.Peek())
                {
                    case '\'':
                    {
                        // N'...' - a <national character string literal>
                        var backslashEscape = _dialect.SupportsStringLiteralBackslashEscape;
                        var s = TokenizeSingleQuotedString(chars, '\'', backslashEscape);
                        return new Token.NationalStringLiteral(s);
                    }
                    case char q when (q == 'q' || q == 'Q') && _dialect.SupportsQuoteDelimitedString:
                    {
                        chars.Next(); // consume and check the next char
                        if (chars.Peek() == '\'')
                        {
                            var s = TokenizeQuoteDelimitedString(chars, new[] { n, q });
                            return new Token.NationalQuoteDelimitedStringLiteral(s);
                        }
                        else
                        {
                            var s = TokenizeWord(new string(new[] { n, q }), chars);
                            return Token.MakeWordOwned(s, null);
                        }
                    }
                    default:
                    {
                        // regular identifier starting with an "N"
                        var s = TokenizeWord(n.ToString(), chars);
                        return Token.MakeWordOwned(s, null);
                    }
                }
            }
            case 'Q' or 'q' when _dialect.SupportsQuoteDelimitedString:
            {
                var q = ch;
                chars.Next(); // consume and check the next char
                if (chars.Peek() == '\'')
                {
                    var s = TokenizeQuoteDelimitedString(chars, new[] { q });
                    return new Token.QuoteDelimitedStringLiteral(s);
                }
                else
                {
                    var s = TokenizeWord(q.ToString(), chars);
                    return Token.MakeWordOwned(s, null);
                }
            }
            // PostgreSQL accepts "escape" string constants, which are an extension to the SQL standard.
            case 'e' or 'E' when _dialect.SupportsStringEscapeConstant:
            {
                var x = ch;
                var startingLoc = chars.Location();
                chars.Next(); // consume, to check the next char
                switch (chars.Peek())
                {
                    case '\'':
                    {
                        var s = TokenizeEscapedSingleQuotedString(startingLoc, chars);
                        return new Token.EscapedStringLiteral(s);
                    }
                    default:
                    {
                        // regular identifier starting with an "E" or "e"
                        var s = TokenizeWord(x.ToString(), chars);
                        return Token.MakeWordOwned(s, null);
                    }
                }
            }
            // Unicode string literals like U&'first \000A second' are supported in some dialects, including PostgreSQL
            case 'u' or 'U' when _dialect.SupportsUnicodeStringLiteral:
            {
                var x = ch;
                chars.Next(); // consume, to check the next char
                if (chars.Peek() == '&')
                {
                    // we cannot advance the iterator here, as we need to consume the '&' later if the 'u' was an identifier
                    var charsClone = chars.Peekable; // chars.peekable.clone(): cópia do struct
                    charsClone.Next(); // consume the '&' in the clone
                    if (charsClone.Peek() == '\'')
                    {
                        chars.Next(); // consume the '&' in the original iterator
                        var s = UnescapeUnicodeSingleQuotedString(chars);
                        return new Token.UnicodeStringLiteral(s);
                    }
                }
                {
                    // regular identifier starting with an "U" or "u"
                    var s = TokenizeWord(x.ToString(), chars);
                    return Token.MakeWordOwned(s, null);
                }
            }
            // The spec only allows an uppercase 'X' to introduce a hex
            // string, but PostgreSQL, at least, allows a lowercase 'x' too.
            case 'x' or 'X':
            {
                var x = ch;
                chars.Next(); // consume, to check the next char
                switch (chars.Peek())
                {
                    case '\'':
                    {
                        // X'...' - a <binary string literal>
                        var s = TokenizeSingleQuotedString(chars, '\'', true);
                        return new Token.HexStringLiteral(s);
                    }
                    default:
                    {
                        // regular identifier starting with an "X"
                        var s = TokenizeWord(x.ToString(), chars);
                        return Token.MakeWordOwned(s, null);
                    }
                }
            }
            // single quoted string
            case '\'':
            {
                if (_dialect.SupportsTripleQuotedString)
                {
                    return TokenizeSingleOrTripleQuotedString(
                        chars,
                        '\'',
                        _dialect.SupportsStringLiteralBackslashEscape,
                        v => new Token.SingleQuotedString(v),
                        v => new Token.TripleSingleQuotedString(v));
                }
                var s = TokenizeSingleQuotedString(
                    chars,
                    '\'',
                    _dialect.SupportsStringLiteralBackslashEscape);

                return new Token.SingleQuotedString(s);
            }
            // double quoted string
            case '"' when !_dialect.IsDelimitedIdentifierStart(ch) && !_dialect.IsIdentifierStart(ch):
            {
                if (_dialect.SupportsTripleQuotedString)
                {
                    return TokenizeSingleOrTripleQuotedString(
                        chars,
                        '"',
                        _dialect.SupportsStringLiteralBackslashEscape,
                        v => new Token.DoubleQuotedString(v),
                        v => new Token.TripleDoubleQuotedString(v));
                }
                var s = TokenizeSingleQuotedString(
                    chars,
                    '"',
                    _dialect.SupportsStringLiteralBackslashEscape);

                return new Token.DoubleQuotedString(s);
            }
            // delimited (quoted) identifier
            case var quoteStart when _dialect.IsDelimitedIdentifierStart(ch):
            {
                var word = TokenizeQuotedIdentifier(quoteStart, chars);
                return Token.MakeWordOwned(word, quoteStart);
            }
            // Potentially nested delimited (quoted) identifier
            // (no Rust o padrão liga `quote_start`, que é o próprio `ch`; abaixo `quote_start` é
            // redeclarado pelo `let Some((quote_start, ..))`, então o C# usa `ch` para o valor externo)
            case var _ when _dialect.IsNestedDelimitedIdentifierStart(ch)
                && _dialect.PeekNestedDelimitedIdentifierQuotes(chars.Peekable) is not null:
            {
                if (_dialect.PeekNestedDelimitedIdentifierQuotes(chars.Peekable) is not { } quotes)
                {
                    return TokenizerErrorAt<Token?>(
                        chars.Location(),
                        $"Expected nested delimiter '{ch}' before EOF.");
                }
                var quoteStart = quotes.QuoteStart;

                if (quotes.NestedQuoteStart is not { } nestedQuoteStart)
                {
                    var word0 = TokenizeQuotedIdentifier(quoteStart, chars);
                    return Token.MakeWordOwned(word0, quoteStart);
                }

                var word = new List<string>();
                var quoteEnd = Word.MatchingEndQuote(quoteStart);
                var nestedQuoteEnd = Word.MatchingEndQuote(nestedQuoteStart);
                var errorLoc = chars.Location();

                chars.Next(); // skip the first delimiter
                PeekingTakeWhile(chars, c => char.IsWhiteSpace(c));
                if (chars.Peek() != nestedQuoteStart)
                {
                    return TokenizerErrorAt<Token?>(
                        errorLoc,
                        $"Expected nested delimiter '{nestedQuoteStart}' before EOF.");
                }
                word.Add(nestedQuoteStart.ToString());
                word.Add(TokenizeQuotedIdentifier(nestedQuoteEnd, chars));
                word.Add(nestedQuoteEnd.ToString());
                PeekingTakeWhile(chars, c => char.IsWhiteSpace(c));
                if (chars.Peek() != quoteEnd)
                {
                    return TokenizerErrorAt<Token?>(
                        errorLoc,
                        $"Expected close delimiter '{quoteEnd}' before EOF.");
                }
                chars.Next(); // skip close delimiter

                return Token.MakeWordOwned(string.Concat(word), quoteStart);
            }
            // numbers and period
            case (>= '0' and <= '9') or '.':
            {
                // special case where if ._ is encountered after a word then that word
                // is a table and the _ is the start of the col name.
                // if the prev token is not a word, then this is not a valid sql
                // word or number.
                if (ch == '.' && chars.Peekable.Nth(1) == '_')
                {
                    if (prevToken is Token.Word)
                    {
                        chars.Next();
                        return new Token.Period();
                    }

                    return TokenizerErrorAt<Token?>(
                        chars.Location(),
                        "Unexpected character '_'");
                }

                var s = TokenizeNumberPart(chars, RustChar.IsAsciiDigit);

                // match binary literal that starts with 0x
                if (s == "0" && chars.Peek() == 'x')
                {
                    chars.Next();
                    var s2 = TokenizeNumberPart(chars, RustChar.IsAsciiHexdigit);
                    return new Token.HexStringLiteral(s2);
                }

                // match one period
                if (chars.Peek() == '.')
                {
                    s += '.';
                    chars.Next();
                }

                // If the dialect supports identifiers that start with a numeric prefix
                // and we have now consumed a dot, check if the previous token was a Word.
                // If so, what follows is definitely not part of a decimal number and
                // we should yield the dot as a dedicated token so compound identifiers
                // starting with digits can be parsed correctly.
                if (s == "." && _dialect.SupportsNumericPrefix)
                {
                    if (prevToken is Token.Word)
                    {
                        return new Token.Period();
                    }
                }

                // Consume fractional digits.
                s += TokenizeNumberPart(chars, RustChar.IsAsciiDigit);

                // No fraction -> Token::Period
                if (s == ".")
                {
                    return new Token.Period();
                }

                // Parse exponent as number
                var exponentPart = "";
                if (chars.Peek() == 'e' || chars.Peek() == 'E')
                {
                    var charClone = chars.Peekable; // chars.peekable.clone(): cópia do struct
                    exponentPart += charClone.Next()
                        ?? throw new InvalidOperationException("called `Option::unwrap()` on a `None` value");

                    // Optional sign
                    if (charClone.Peek() is { } sign && (sign == '+' || sign == '-'))
                    {
                        exponentPart += sign;
                        charClone.Next();
                    }

                    // Definitely an exponent, get original iterator up to speed and use it
                    if (charClone.Peek() is { } c
                        && (RustChar.IsAsciiDigit(c)
                            || (c == '_' && _dialect.SupportsNumericLiteralUnderscores)))
                    {
                        for (var i = 0; i < exponentPart.Length; i++)
                        {
                            chars.Next();
                        }
                        exponentPart += TokenizeNumberPart(chars, RustChar.IsAsciiDigit);
                        s += exponentPart;
                    }
                    // Not an exponent, discard the work done
                }

                // If the dialect supports identifiers that start with a numeric prefix,
                // we need to check if the value is in fact an identifier and must thus
                // be tokenized as a word.
                if (_dialect.SupportsNumericPrefix)
                {
                    if (exponentPart.Length == 0)
                    {
                        // If it is not a number with an exponent, it may be
                        // an identifier starting with digits.
                        var word = PeekingTakeWhile(chars, c => _dialect.IsIdentifierPart(c));

                        if (word.Length != 0)
                        {
                            s += word;
                            return Token.MakeWordOwned(s, null);
                        }
                    }
                    else if (prevToken is Token.Period)
                    {
                        // If the previous token was a period, thus not belonging to a number,
                        // the value we have is part of an identifier.
                        return Token.MakeWordOwned(s, null);
                    }
                }

                bool @long;
                if (chars.Peek() == 'L')
                {
                    chars.Next();
                    @long = true;
                }
                else
                {
                    @long = false;
                }
                return new Token.Number(s, @long);
            }
            // punctuation
            case '(':
                return ConsumeAndReturn(chars, new Token.LParen());
            case ')':
                return ConsumeAndReturn(chars, new Token.RParen());
            case ',':
                return ConsumeAndReturn(chars, new Token.Comma());
            // operators
            case '-':
            {
                chars.Next(); // consume the '-'

                switch (chars.Peek())
                {
                    case '-':
                    {
                        var isComment = true;
                        if (_dialect.RequiresSingleLineCommentWhitespace)
                        {
                            isComment = chars.Peekable.Nth(1) is { } next && char.IsWhiteSpace(next);
                        }

                        if (isComment)
                        {
                            chars.Next(); // consume second '-'
                            var comment = TokenizeSingleLineComment(chars);
                            return new Token.Whitespace(new Whitespace.SingleLineComment(comment, "--"));
                        }

                        return StartBinop(chars, "-", new Token.Minus());
                    }
                    case '>':
                    {
                        chars.Next();
                        switch (chars.Peek())
                        {
                            case '>':
                                return ConsumeForBinop(chars, "->>", new Token.LongArrow());
                            default:
                                return StartBinop(chars, "->", new Token.Arrow());
                        }
                    }
                    // a regular '-' operator
                    default:
                        return StartBinop(chars, "-", new Token.Minus());
                }
            }
            case '/':
            {
                chars.Next(); // consume the '/'
                switch (chars.Peek())
                {
                    case '*':
                        chars.Next(); // consume the '*', starting a multi-line comment
                        return TokenizeMultilineComment(chars);
                    case '/' when _dialect is SnowflakeDialect:
                    {
                        chars.Next(); // consume the second '/', starting a snowflake single-line comment
                        var comment = TokenizeSingleLineComment(chars);
                        return new Token.Whitespace(new Whitespace.SingleLineComment(comment, "//"));
                    }
                    case '/' when _dialect is DuckDbDialect or GenericDialect:
                        return ConsumeAndReturn(chars, new Token.DuckIntDiv());
                    // a regular '/' operator
                    default:
                        return new Token.Div();
                }
            }
            case '+':
                return ConsumeAndReturn(chars, new Token.Plus());
            case '*':
                return ConsumeAndReturn(chars, new Token.Mul());
            case '%':
            {
                chars.Next(); // advance past '%'
                switch (chars.Peek())
                {
                    case char s when char.IsWhiteSpace(s):
                        return new Token.Mod();
                    case char sch when _dialect.IsIdentifierStart('%'):
                        return TokenizeIdentifierOrKeyword(new[] { ch, sch }, chars);
                    default:
                        return StartBinop(chars, "%", new Token.Mod());
                }
            }
            case '|':
            {
                chars.Next(); // consume the '|'
                switch (chars.Peek())
                {
                    case '/':
                        return ConsumeForBinop(chars, "|/", new Token.PGSquareRoot());
                    case '|':
                    {
                        chars.Next(); // consume the second '|'
                        switch (chars.Peek())
                        {
                            case '/':
                                return ConsumeForBinop(chars, "||/", new Token.PGCubeRoot());
                            default:
                                return StartBinop(chars, "||", new Token.StringConcat());
                        }
                    }
                    case '&' when _dialect.SupportsGeometricTypes:
                    {
                        chars.Next(); // consume
                        switch (chars.Peek())
                        {
                            case '>':
                                return ConsumeForBinop(
                                    chars,
                                    "|&>",
                                    new Token.VerticalBarAmpersandRightAngleBracket());
                            default:
                                return StartBinopOpt(chars, "|&", null);
                        }
                    }
                    case '>' when _dialect.SupportsGeometricTypes:
                    {
                        chars.Next(); // consume
                        switch (chars.Peek())
                        {
                            case '>':
                                return ConsumeForBinop(
                                    chars,
                                    "|>>",
                                    new Token.VerticalBarShiftRight());
                            default:
                                return StartBinopOpt(chars, "|>", null);
                        }
                    }
                    case '>' when _dialect.SupportsPipeOperator:
                        return ConsumeForBinop(chars, "|>", new Token.VerticalBarRightAngleBracket());
                    // Bitshift '|' operator
                    default:
                        return StartBinop(chars, "|", new Token.Pipe());
                }
            }
            case '=':
            {
                chars.Next(); // consume
                switch (chars.Peek())
                {
                    case '>':
                        return ConsumeAndReturn(chars, new Token.RArrow());
                    case '=':
                        return ConsumeAndReturn(chars, new Token.DoubleEq());
                    default:
                        return new Token.Eq();
                }
            }
            case '!':
            {
                chars.Next(); // consume
                switch (chars.Peek())
                {
                    case '=':
                        return ConsumeAndReturn(chars, new Token.Neq());
                    case '!':
                        return ConsumeAndReturn(chars, new Token.DoubleExclamationMark());
                    case '~':
                    {
                        chars.Next();
                        switch (chars.Peek())
                        {
                            case '*':
                                return ConsumeAndReturn(chars, new Token.ExclamationMarkTildeAsterisk());
                            case '~':
                            {
                                chars.Next();
                                switch (chars.Peek())
                                {
                                    case '*':
                                        return ConsumeAndReturn(
                                            chars,
                                            new Token.ExclamationMarkDoubleTildeAsterisk());
                                    default:
                                        return new Token.ExclamationMarkDoubleTilde();
                                }
                            }
                            default:
                                return new Token.ExclamationMarkTilde();
                        }
                    }
                    default:
                        return new Token.ExclamationMark();
                }
            }
            case '<':
            {
                chars.Next(); // consume
                switch (chars.Peek())
                {
                    case '=':
                    {
                        chars.Next();
                        switch (chars.Peek())
                        {
                            case '>':
                                return ConsumeForBinop(chars, "<=>", new Token.Spaceship());
                            // `<=+` and `<=-` are not valid combined operators; treat `<=` as
                            // the operator and leave `+`/`-` to be tokenized separately.
                            case '+' or '-':
                                return new Token.LtEq();
                            default:
                                return StartBinop(chars, "<=", new Token.LtEq());
                        }
                    }
                    case '|' when _dialect.SupportsGeometricTypes:
                        return ConsumeForBinop(chars, "<<|", new Token.ShiftLeftVerticalBar());
                    case '>':
                        return ConsumeForBinop(chars, "<>", new Token.Neq());
                    case '<' when _dialect.SupportsGeometricTypes:
                    {
                        chars.Next(); // consume
                        switch (chars.Peek())
                        {
                            case '|':
                                return ConsumeForBinop(
                                    chars,
                                    "<<|",
                                    new Token.ShiftLeftVerticalBar());
                            default:
                                return StartBinop(chars, "<<", new Token.ShiftLeft());
                        }
                    }
                    case '<':
                        return ConsumeForBinop(chars, "<<", new Token.ShiftLeft());
                    // `<+` is not a valid combined operator; treat `<` as the operator
                    // and leave `+` to be tokenized separately.
                    case '+':
                        return new Token.Lt();
                    case '-' when _dialect.SupportsGeometricTypes:
                    {
                        if (chars.Peekable.Nth(1) == '>')
                        {
                            chars.Next(); // consume `-`
                            return ConsumeForBinop(chars, "<->", new Token.TwoWayArrow());
                        }
                        else
                        {
                            return new Token.Lt();
                        }
                    }
                    case '^' when _dialect.SupportsGeometricTypes:
                        return ConsumeForBinop(chars, "<^", new Token.LeftAngleBracketCaret());
                    case '@':
                        return ConsumeForBinop(chars, "<@", new Token.ArrowAt());
                    default:
                        return StartBinop(chars, "<", new Token.Lt());
                }
            }
            case '>':
            {
                chars.Next(); // consume
                switch (chars.Peek())
                {
                    case '=':
                        return ConsumeForBinop(chars, ">=", new Token.GtEq());
                    case '>':
                        return ConsumeForBinop(chars, ">>", new Token.ShiftRight());
                    case '^' when _dialect.SupportsGeometricTypes:
                        return ConsumeForBinop(chars, ">^", new Token.RightAngleBracketCaret());
                    default:
                        return StartBinop(chars, ">", new Token.Gt());
                }
            }
            case ':':
            {
                chars.Next();
                switch (chars.Peek())
                {
                    case ':':
                        return ConsumeAndReturn(chars, new Token.DoubleColon());
                    case '=':
                        return ConsumeAndReturn(chars, new Token.Assignment());
                    default:
                        return new Token.Colon();
                }
            }
            case ';':
                return ConsumeAndReturn(chars, new Token.SemiColon());
            case '\\':
                return ConsumeAndReturn(chars, new Token.Backslash());
            case '[':
                return ConsumeAndReturn(chars, new Token.LBracket());
            case ']':
                return ConsumeAndReturn(chars, new Token.RBracket());
            case '&':
            {
                chars.Next(); // consume the '&'
                switch (chars.Peek())
                {
                    case '>' when _dialect.SupportsGeometricTypes:
                        chars.Next();
                        return ConsumeAndReturn(chars, new Token.AmpersandRightAngleBracket());
                    case '<' when _dialect.SupportsGeometricTypes:
                    {
                        chars.Next(); // consume
                        switch (chars.Peek())
                        {
                            case '|':
                                return ConsumeAndReturn(
                                    chars,
                                    new Token.AmpersandLeftAngleBracketVerticalBar());
                            default:
                                return StartBinop(chars, "&<", new Token.AmpersandLeftAngleBracket());
                        }
                    }
                    case '&':
                        chars.Next(); // consume the second '&'
                        return StartBinop(chars, "&&", new Token.Overlap());
                    // Bitshift '&' operator
                    default:
                        return StartBinop(chars, "&", new Token.Ampersand());
                }
            }
            case '^':
            {
                chars.Next(); // consume the '^'
                switch (chars.Peek())
                {
                    case '@':
                        return ConsumeAndReturn(chars, new Token.CaretAt());
                    default:
                        return new Token.Caret();
                }
            }
            case '{':
                return ConsumeAndReturn(chars, new Token.LBrace());
            case '}':
                return ConsumeAndReturn(chars, new Token.RBrace());
            case '#' when _dialect is SnowflakeDialect or BigQueryDialect or MySqlDialect or HiveDialect:
            {
                chars.Next(); // consume the '#', starting a snowflake single-line comment
                var comment = TokenizeSingleLineComment(chars);
                return new Token.Whitespace(new Whitespace.SingleLineComment(comment, "#"));
            }
            case '~':
            {
                chars.Next(); // consume
                switch (chars.Peek())
                {
                    case '*':
                        return ConsumeForBinop(chars, "~*", new Token.TildeAsterisk());
                    case '=' when _dialect.SupportsGeometricTypes:
                        return ConsumeForBinop(chars, "~=", new Token.TildeEqual());
                    case '~':
                    {
                        chars.Next();
                        switch (chars.Peek())
                        {
                            case '*':
                                return ConsumeForBinop(chars, "~~*", new Token.DoubleTildeAsterisk());
                            default:
                                return StartBinop(chars, "~~", new Token.DoubleTilde());
                        }
                    }
                    default:
                        return StartBinop(chars, "~", new Token.Tilde());
                }
            }
            case '#':
            {
                chars.Next();
                switch (chars.Peek())
                {
                    case '-':
                        return ConsumeForBinop(chars, "#-", new Token.HashMinus());
                    case '>':
                    {
                        chars.Next();
                        switch (chars.Peek())
                        {
                            case '>':
                                return ConsumeForBinop(chars, "#>>", new Token.HashLongArrow());
                            default:
                                return StartBinop(chars, "#>", new Token.HashArrow());
                        }
                    }
                    case ' ':
                        return new Token.Sharp();
                    case '#' when _dialect.SupportsGeometricTypes:
                        return ConsumeForBinop(chars, "##", new Token.DoubleSharp());
                    case char sch when _dialect.IsIdentifierStart('#'):
                        return TokenizeIdentifierOrKeyword(new[] { ch, sch }, chars);
                    default:
                        return StartBinop(chars, "#", new Token.Sharp());
                }
            }
            case '@':
            {
                chars.Next();
                switch (chars.Peek())
                {
                    case '@' when _dialect.SupportsGeometricTypes:
                        return ConsumeAndReturn(chars, new Token.AtAt());
                    case '-' when _dialect.SupportsGeometricTypes:
                    {
                        chars.Next();
                        switch (chars.Peek())
                        {
                            case '@':
                                return ConsumeAndReturn(chars, new Token.AtDashAt());
                            default:
                                return StartBinopOpt(chars, "@-", null);
                        }
                    }
                    case '>':
                        return ConsumeAndReturn(chars, new Token.AtArrow());
                    case '?':
                        return ConsumeAndReturn(chars, new Token.AtQuestion());
                    case '@':
                    {
                        chars.Next();
                        switch (chars.Peek())
                        {
                            case ' ':
                                return new Token.AtAt();
                            case char tch when _dialect.IsIdentifierStart('@'):
                                return TokenizeIdentifierOrKeyword(new[] { ch, '@', tch }, chars);
                            default:
                                return new Token.AtAt();
                        }
                    }
                    case ' ':
                        return new Token.AtSign();
                    // We break on quotes here, because no dialect allows identifiers starting
                    // with @ and containing quotation marks (e.g. `@'foo'`) unless they are
                    // quoted, which is tokenized as a quoted string, not here (e.g.
                    // `"@'foo'"`). Further, at least two dialects parse `@` followed by a
                    // quoted string as two separate tokens, which this allows. For example,
                    // Postgres parses `@'1'` as the absolute value of '1' which is implicitly
                    // cast to a numeric type. And when parsing MySQL-style grantees (e.g.
                    // `GRANT ALL ON *.* to 'root'@'localhost'`), we also want separate tokens
                    // for the user, the `@`, and the host.
                    case '\'':
                        return new Token.AtSign();
                    case '"':
                        return new Token.AtSign();
                    case '`':
                        return new Token.AtSign();
                    case char sch when _dialect.IsIdentifierStart('@'):
                        return TokenizeIdentifierOrKeyword(new[] { ch, sch }, chars);
                    default:
                        return new Token.AtSign();
                }
            }
            // Postgres uses ? for jsonb operators, not prepared statements
            case '?' when _dialect.SupportsGeometricTypes:
            {
                chars.Next(); // consume
                switch (chars.Peek())
                {
                    case '|':
                    {
                        chars.Next();
                        switch (chars.Peek())
                        {
                            case '|':
                                return ConsumeAndReturn(
                                    chars,
                                    new Token.QuestionMarkDoubleVerticalBar());
                            default:
                                return new Token.QuestionPipe();
                        }
                    }

                    case '&':
                        return ConsumeAndReturn(chars, new Token.QuestionAnd());
                    case '-':
                    {
                        chars.Next(); // consume
                        switch (chars.Peek())
                        {
                            case '|':
                                return ConsumeAndReturn(chars, new Token.QuestionMarkDashVerticalBar());
                            default:
                                return new Token.QuestionMarkDash();
                        }
                    }
                    case '#':
                        return ConsumeAndReturn(chars, new Token.QuestionMarkSharp());
                    default:
                        return new Token.Question();
                }
            }
            case '?':
            {
                chars.Next();
                var s = PeekingTakeWhile(chars, c => char.IsNumber(c));
                return new Token.Placeholder("?" + s);
            }

            // identifier or keyword
            case var c when _dialect.IsIdentifierStart(c):
                return TokenizeIdentifierOrKeyword(new[] { c }, chars);
            case '$':
                return TokenizeDollarPrecededValue(chars);

            // whitespace check (including unicode chars) should be last as it covers some of the chars above
            case var c when char.IsWhiteSpace(c):
                return ConsumeAndReturn(chars, new Token.Whitespace(new Whitespace.Space()));
            default:
            {
                var other = ch;
                // `char` do Rust = scalar value (convencoes.md §10): um par substituto (caractere fora
                // do BMP) é UM Token::Char, como no Rust.
                if (char.IsHighSurrogate(other) && chars.Peekable.Nth(1) is { } low && char.IsLowSurrogate(low))
                {
                    chars.Next();
                    return ConsumeAndReturn(chars, new Token.Char(new string(new[] { other, low })));
                }
                return ConsumeAndReturn(chars, new Token.Char(other.ToString()));
            }
        }
    }

    /// <summary>Consume the next character, then parse a custom binary operator. The next character should be included in the prefix</summary>
    private Token? ConsumeForBinop(State chars, string prefix, Token @default)
    {
        chars.Next(); // consume the first char
        return StartBinopOpt(chars, prefix, @default);
    }

    /// <summary>parse a custom binary operator</summary>
    private Token? StartBinop(State chars, string prefix, Token @default)
    {
        return StartBinopOpt(chars, prefix, @default);
    }

    /// <summary>parse a custom binary operator</summary>
    private Token? StartBinopOpt(State chars, string prefix, Token? @default)
    {
        string? custom = null;
        while (chars.Peek() is { } ch)
        {
            if (!_dialect.IsCustomOperatorPart(ch))
            {
                break;
            }

            custom = (custom ?? prefix) + ch; // custom.get_or_insert_with(|| prefix.to_string()).push(ch)
            chars.Next();
        }
        if (custom is not null)
        {
            return new Token.CustomBinaryOperator(custom);
        }
        if (@default is { } tok)
        {
            return tok;
        }
        return TokenizerErrorAt<Token?>(
            chars.Location(),
            $"Expected a valid binary operator after '{prefix}'");
    }

    /// <summary>Tokenize dollar preceded value (i.e: a string/placeholder)</summary>
    private Token TokenizeDollarPrecededValue(State chars)
    {
        var s = new StringBuilder();
        var value = new StringBuilder();

        chars.Next();

        // If the dialect does not support dollar-quoted strings, then `$$` is rather a placeholder.
        if (chars.Peek() == '$' && !_dialect.SupportsDollarPlaceholder)
        {
            chars.Next();

            var isTerminated = false;
            char? prev = null;

            while (chars.Peek() is { } ch)
            {
                if (prev == '$')
                {
                    if (ch == '$')
                    {
                        chars.Next();
                        isTerminated = true;
                        break;
                    }
                    else
                    {
                        s.Append('$');
                        s.Append(ch);
                    }
                }
                else if (ch != '$')
                {
                    s.Append(ch);
                }

                prev = ch;
                chars.Next();
            }

            return chars.Peek() is null && !isTerminated
                ? TokenizerErrorAt<Token>(chars.Location(), "Unterminated dollar-quoted string")
                : new Token.DollarQuotedString(new DollarQuotedString(s.ToString(), null));
        }
        else
        {
            value.Append(PeekingTakeWhile(chars, ch =>
                char.IsLetterOrDigit(ch)
                || ch == '_'
                // Allow $ as a placeholder character if the dialect supports it
                || (ch == '$' && _dialect.SupportsDollarPlaceholder)));

            // If the dialect supports a dollar sign as a money prefix (e.g. SQL Server),
            // and the value so far is all digits, check for a decimal part, e.g. `$123.45`
            if (chars.Peek() == '.'
                && _dialect.SupportsDollarAsMoneyPrefix
                && value.Length != 0
                && AllAsciiDigits(value))
            {
                value.Append('.');
                chars.Next();
                value.Append(PeekingTakeWhile(chars, RustChar.IsAsciiDigit));
                return new Token.Placeholder("$" + value);
            }

            // If the dialect does not support dollar-quoted strings, don't look for the end delimiter.
            if (chars.Peek() == '$' && !_dialect.SupportsDollarPlaceholder)
            {
                chars.Next();

                var temp = new StringBuilder();
                var endDelimiter = "$" + value + "$";

                while (true)
                {
                    if (chars.Next() is { } ch)
                    {
                        temp.Append(ch);

                        if (EndsWith(temp, endDelimiter))
                        {
                            s.Append(temp.ToString(0, temp.Length - endDelimiter.Length));
                            break;
                        }
                    }
                    else
                    {
                        if (EndsWith(temp, endDelimiter))
                        {
                            s.Append(temp.ToString(0, temp.Length - endDelimiter.Length));
                            break;
                        }

                        return TokenizerErrorAt<Token>(
                            chars.Location(),
                            "Unterminated dollar-quoted, expected $");
                    }
                }
            }
            else
            {
                return new Token.Placeholder("$" + value);
            }
        }

        return new Token.DollarQuotedString(new DollarQuotedString(
            s.ToString(),
            value.Length == 0 ? null : value.ToString()));
    }

    // `value.chars().all(|c| c.is_ascii_digit())`
    private static bool AllAsciiDigits(StringBuilder value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (!RustChar.IsAsciiDigit(value[i]))
            {
                return false;
            }
        }
        return true;
    }

    // `temp.ends_with(&end_delimiter)` (comparação ordinal)
    private static bool EndsWith(StringBuilder temp, string suffix)
    {
        if (temp.Length < suffix.Length)
        {
            return false;
        }
        var offset = temp.Length - suffix.Length;
        for (var i = 0; i < suffix.Length; i++)
        {
            if (temp[offset + i] != suffix[i])
            {
                return false;
            }
        }
        return true;
    }

    // Rust: `fn tokenizer_error<R>(..) -> Result<R, TokenizerError>` → método que LANÇA; o tipo de
    // retorno genérico permite usá-lo em expressões, como no Rust. Nome com sufixo `At` para não
    // colidir com o tipo `TokenizerError` (docs/port/nomes.md).
    private T TokenizerErrorAt<T>(Location loc, string message) =>
        throw new TokenizerException(new TokenizerError(message, loc));

    private string TokenizeNumberPart(State chars, Func<char, bool> isDigit)
    {
        var supportsSeparator = _dialect.SupportsNumericLiteralUnderscores;
        var s = new StringBuilder();

        while (chars.Peek() is { } ch)
        {
            if (isDigit(ch))
            {
                chars.Next();
                s.Append(ch);
            }
            else if (supportsSeparator && ch == '_')
            {
                var nextChar = chars.Peekable.Nth(1);
                if (s.Length == 0 || !(nextChar is { } nc && isDigit(nc)))
                {
                    return TokenizerErrorAt<string>(chars.Location(), "Unexpected character '_'");
                }
                chars.Next();
                s.Append(ch);
            }
            else
            {
                break;
            }
        }

        return s.ToString();
    }

    // Consume characters until newline
    private string TokenizeSingleLineComment(State chars)
    {
        return PeekingTakeWhile(chars, ch => ch switch
        {
            '\n' => false,                                    // Always stop at \n
            '\r' when _dialect is PostgreSqlDialect => false, // Stop at \r for Postgres
            _ => true,                                        // Keep consuming for other characters
        });
    }

    /// <summary>Tokenize an identifier or keyword, after the first char is already consumed.</summary>
    // Rust: `first_chars: impl Into<String>` → string.
    private string TokenizeWord(string firstChars, State chars)
    {
        var s = firstChars;
        s += PeekingTakeWhile(chars, ch => _dialect.IsIdentifierPart(ch));
        return s;
    }

    /// <summary>Read a quoted identifier</summary>
    private string TokenizeQuotedIdentifier(char quoteStart, State chars)
    {
        var errorLoc = chars.Location();
        chars.Next(); // consume the opening quote
        var quoteEnd = Word.MatchingEndQuote(quoteStart);
        var (s, lastChar) = ParseQuotedIdent(chars, quoteEnd);

        if (lastChar == quoteEnd)
        {
            return s;
        }
        else
        {
            return TokenizerErrorAt<string>(
                errorLoc,
                $"Expected close delimiter '{quoteEnd}' before EOF.");
        }
    }

    /// <summary>Read a single quoted string, starting with the opening quote.</summary>
    private string TokenizeEscapedSingleQuotedString(Location startingLoc, State chars)
    {
        if (UnescapeSingleQuotedString(chars) is { } s)
        {
            return s;
        }

        return TokenizerErrorAt<string>(startingLoc, "Unterminated encoded string literal");
    }

    /// <summary>
    /// Reads a string literal quoted by a single or triple quote characters.
    /// Examples: <c>'abc'</c>, <c>'''abc'''</c>, <c>"""abc"""</c>.
    /// </summary>
    // Rust: `F: Fn(String) -> Token` (os construtores de variante) → Func<string, Token>.
    private Token? TokenizeSingleOrTripleQuotedString(
        State chars,
        char quoteStyle,
        bool backslashEscape,
        Func<string, Token> singleQuoteToken,
        Func<string, Token> tripleQuoteToken)
    {
        var errorLoc = chars.Location();

        byte numOpeningQuotes = 0;
        for (var i = 0; i < 3; i++)
        {
            if (chars.Peek() == quoteStyle)
            {
                chars.Next(); // Consume quote.
                numOpeningQuotes += 1;
            }
            else
            {
                break;
            }
        }

        Func<string, Token> tokenFn;
        NumStringQuoteChars numQuoteChars;
        switch (numOpeningQuotes)
        {
            case 1:
                tokenFn = singleQuoteToken;
                numQuoteChars = new NumStringQuoteChars.One();
                break;
            case 2:
                // If we matched double quotes, then this is an empty string.
                return singleQuoteToken("");
            case 3:
                // Rust: `let Some(num_quote_chars) = NonZeroU8::new(3) else { return tokenizer_error(..) }`.
                // NonZeroU8::new só devolve None para 0, então o ramo é inalcançável. Mesmo assim é
                // transcrito, para manter a correspondência 1:1 com o Rust (checklist de revisão §1).
                byte numQuoteCount = 3;
                if (numQuoteCount == 0)
                {
                    return TokenizerErrorAt<Token?>(errorLoc, "invalid number of opening quotes");
                }
                tokenFn = tripleQuoteToken;
                numQuoteChars = new NumStringQuoteChars.Many(numQuoteCount);
                break;
            default:
                return TokenizerErrorAt<Token?>(errorLoc, "invalid string literal opening");
        }

        var settings = new TokenizeQuotedStringSettings(
            QuoteStyle: quoteStyle,
            NumQuoteChars: numQuoteChars,
            NumOpeningQuotesToConsume: 0,
            BackslashEscape: backslashEscape);

        return tokenFn(TokenizeQuotedString(chars, settings));
    }

    /// <summary>Reads a string literal quoted by a single quote character.</summary>
    private string TokenizeSingleQuotedString(State chars, char quoteStyle, bool backslashEscape)
    {
        return TokenizeQuotedString(
            chars,
            new TokenizeQuotedStringSettings(
                QuoteStyle: quoteStyle,
                NumQuoteChars: new NumStringQuoteChars.One(),
                NumOpeningQuotesToConsume: 1,
                BackslashEscape: backslashEscape));
    }

    /// <summary>
    /// Reads a quote delimited string expecting <c>chars.next()</c> to deliver a quote.
    /// See https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Literals.html#GUID-1824CBAA-6E16-4921-B2A6-112FB02248DA
    /// </summary>
    private QuoteDelimitedString TokenizeQuoteDelimitedString(
        State chars,
        // the prefix that introduced the possible literal or word,
        // e.g. "Q" or "nq"
        char[] literalPrefix)
    {
        var literalStartLoc = chars.Location();
        chars.Next();

        var startQuoteLoc = chars.Location();
        var next = chars.Next();
        if (next is null or ' ' or '\t' or '\r' or '\n')
        {
            return TokenizerErrorAt<QuoteDelimitedString>(
                startQuoteLoc,
                $"Invalid space, tab, newline, or EOF after '{new string(literalPrefix)}''");
        }
        var startQuote = next.Value;
        var endQuote = startQuote switch
        {
            '[' => ']',
            '{' => '}',
            '<' => '>',
            '(' => ')',
            var c => c,
        };

        // read the string literal until the "quote character" following a by literal quote
        var value = new StringBuilder();
        while (chars.Next() is { } ch)
        {
            if (ch == endQuote)
            {
                if (chars.Peek() == '\'')
                {
                    chars.Next(); // ~ consume the quote
                    return new QuoteDelimitedString(startQuote, value.ToString(), endQuote);
                }
            }
            value.Append(ch);
        }

        return TokenizerErrorAt<QuoteDelimitedString>(literalStartLoc, "Unterminated string literal");
    }

    /// <summary>Read a quoted string.</summary>
    private string TokenizeQuotedString(State chars, TokenizeQuotedStringSettings settings)
    {
        var s = new StringBuilder();
        var errorLoc = chars.Location();

        // Consume any opening quotes.
        for (var i = 0; i < settings.NumOpeningQuotesToConsume; i++)
        {
            if (chars.Next() != settings.QuoteStyle)
            {
                return TokenizerErrorAt<string>(errorLoc, "invalid string literal opening");
            }
        }

        var numConsecutiveQuotes = 0;
        while (chars.Peek() is { } ch)
        {
            NumStringQuoteChars? pendingFinalQuote = settings.NumQuoteChars switch
            {
                NumStringQuoteChars.One => new NumStringQuoteChars.One(),
                NumStringQuoteChars.Many n when numConsecutiveQuotes + 1 == n.Value => n,
                NumStringQuoteChars.Many => null,
                _ => throw new InvalidOperationException(
                    $"Variante de NumStringQuoteChars não tratada: {settings.NumQuoteChars.GetType().Name}"),
            };

            if (ch == settings.QuoteStyle && pendingFinalQuote is not null)
            {
                chars.Next(); // consume

                if (pendingFinalQuote is NumStringQuoteChars.Many { Value: var count })
                {
                    // For an initial string like `"""abc"""`, at this point we have
                    // `abc""` in the buffer and have now matched the final `"`.
                    // However, the string to return is simply `abc`, so we strip off
                    // the trailing quotes before returning.
                    var length = s.Length;
                    for (var i = 1; i < count; i++)
                    {
                        if (length > 0)
                        {
                            length--; // buf.next_back()
                        }
                    }
                    return s.ToString(0, length);
                }
                else if (chars.Peek() is { } c && c == settings.QuoteStyle)
                {
                    s.Append(ch);
                    if (!_unescape)
                    {
                        // In no-escape mode, the given query has to be saved completely
                        s.Append(ch);
                    }
                    chars.Next();
                }
                else
                {
                    return s.ToString();
                }
            }
            else if (ch == '\\' && settings.BackslashEscape)
            {
                // consume backslash
                chars.Next();

                numConsecutiveQuotes = 0;

                if (chars.Peek() is { } next)
                {
                    if (!_unescape
                        || (_dialect.IgnoresWildcardEscapes
                            && (next == '%' || next == '_')))
                    {
                        // In no-escape mode, the given query has to be saved completely
                        // including backslashes. Similarly, with ignore_like_wildcard_escapes,
                        // the backslash is not stripped.
                        s.Append(ch);
                        s.Append(next);
                        chars.Next(); // consume next
                    }
                    else
                    {
                        var n = next switch
                        {
                            '0' => '\0',
                            'a' => '\u0007',
                            'b' => '\u0008',
                            'f' => '\u000C',
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            'Z' => '\u001A',
                            _ => next,
                        };
                        s.Append(n);
                        chars.Next(); // consume next
                    }
                }
            }
            else
            {
                chars.Next(); // consume ch

                if (ch == settings.QuoteStyle)
                {
                    numConsecutiveQuotes += 1;
                }
                else
                {
                    numConsecutiveQuotes = 0;
                }

                s.Append(ch);
            }
        }
        return TokenizerErrorAt<string>(errorLoc, "Unterminated string literal");
    }

    private Token? TokenizeMultilineComment(State chars)
    {
        var s = new StringBuilder();
        var nested = 1;
        var supportsNestedComments = _dialect.SupportsNestedComments;
        while (true)
        {
            var next = chars.Next();
            if (next == '/' && chars.Peek() == '*' && supportsNestedComments)
            {
                chars.Next(); // consume the '*'
                s.Append('/');
                s.Append('*');
                nested += 1;
            }
            else if (next == '*' && chars.Peek() == '/')
            {
                chars.Next(); // consume the '/'
                nested -= 1;
                if (nested == 0)
                {
                    return new Token.Whitespace(new Whitespace.MultiLineComment(s.ToString()));
                }
                s.Append('*');
                s.Append('/');
            }
            else if (next is { } ch)
            {
                s.Append(ch);
            }
            else
            {
                return TokenizerErrorAt<Token?>(
                    chars.Location(),
                    "Unexpected EOF while in a multi-line comment");
            }
        }
    }

    private (string, char?) ParseQuotedIdent(State chars, char quoteEnd)
    {
        char? lastChar = null;
        var s = new StringBuilder();
        while (chars.Next() is { } ch)
        {
            if (ch == quoteEnd)
            {
                if (chars.Peek() == quoteEnd)
                {
                    chars.Next();
                    s.Append(ch);
                    if (!_unescape)
                    {
                        // In no-escape mode, the given query has to be saved completely
                        s.Append(ch);
                    }
                }
                else
                {
                    lastChar = quoteEnd;
                    break;
                }
            }
            else
            {
                s.Append(ch);
            }
        }
        return (s.ToString(), lastChar);
    }

    private Token? ConsumeAndReturn(State chars, Token t)
    {
        chars.Next();
        return t;
    }

    /// <summary>
    /// Read from <c>chars</c> until <c>predicate</c> returns <c>false</c> or EOF is hit.
    /// Return the characters read as String, and keep the first non-matching
    /// char available as <c>chars.next()</c>.
    /// </summary>
    // Rust: função livre privada do módulo → internal static (convencoes.md §13 #5).
    internal static string PeekingTakeWhile(State chars, Func<char, bool> predicate)
    {
        var s = new StringBuilder();
        while (chars.Peek() is { } ch)
        {
            if (predicate(ch))
            {
                chars.Next(); // consume
                s.Append(ch);
            }
            else
            {
                break;
            }
        }
        return s.ToString();
    }

    // Rust: função livre privada do módulo → internal static (convencoes.md §13 #5).
    internal static string? UnescapeSingleQuotedString(State chars)
    {
        return new Unescape(chars).DoUnescape();
    }

    // Rust: função livre privada do módulo → internal static (convencoes.md §13 #5).
    internal static string UnescapeUnicodeSingleQuotedString(State chars)
    {
        var unescaped = new StringBuilder();
        chars.Next(); // consume the opening quote
        while (chars.Next() is { } c)
        {
            switch (c)
            {
                case '\'':
                    if (chars.Peek() == '\'')
                    {
                        chars.Next();
                        unescaped.Append('\'');
                    }
                    else
                    {
                        return unescaped.ToString();
                    }
                    break;
                case '\\':
                    switch (chars.Peek())
                    {
                        case '\\':
                            chars.Next();
                            unescaped.Append('\\');
                            break;
                        case '+':
                            chars.Next();
                            RustChar.PushScalar(unescaped, TakeCharFromHexDigits(chars, 6));
                            break;
                        default:
                            RustChar.PushScalar(unescaped, TakeCharFromHexDigits(chars, 4));
                            break;
                    }
                    break;
                default:
                    unescaped.Append(c);
                    break;
            }
        }
        throw new TokenizerException(new TokenizerError(
            "Unterminated unicode encoded string literal",
            chars.Location()));
    }

    // Rust: função livre privada do módulo → internal static (convencoes.md §13 #5).
    // Devolve o scalar value (o `char` do Rust pode estar fora do BMP; convencoes.md §13).
    internal static int TakeCharFromHexDigits(State chars, int maxDigits)
    {
        uint result = 0;
        for (var i = 0; i < maxDigits; i++)
        {
            var nextChar = chars.Next() ?? throw new TokenizerException(new TokenizerError(
                "Unexpected EOF while parsing hex digit in escaped unicode string.",
                chars.Location()));
            var digit = RustChar.ToDigit(nextChar, 16) ?? throw new TokenizerException(new TokenizerError(
                $"Invalid hex digit in escaped unicode string: {nextChar}",
                chars.Location()));
            result = (result * 16) + digit;
        }
        return RustChar.FromU32(result) ?? throw new TokenizerException(new TokenizerError(
            "Invalid unicode character: " + result.ToString("x", CultureInfo.InvariantCulture),
            chars.Location()));
    }
}
