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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 4467–5201:
// de get_next_precedence até try_parse: navegação de tokens, expected/expect_*, listas
// separadas, maybe_parse/try_parse).
// Modificado: tradução de Rust para C#.

using System.Globalization;
using SqlParser.Core.Ast;

namespace SqlParser.Core;

public sealed partial class Parser
{
    /// <summary>Get the precedence of the next token</summary>
    public byte GetNextPrecedence() => _dialect.GetNextPrecedenceDefault(this);

    /// <summary>
    /// Return the token at the given location, or EOF if the index is beyond
    /// the length of the current set of tokens.
    /// </summary>
    public TokenWithSpan TokenAt(int index) =>
        index >= 0 && index < _tokens.Count ? _tokens[index] : EofToken;

    /// <summary>
    /// Return the first non-whitespace token that has not yet been processed
    /// or Token::EOF
    /// </summary>
    /// <remarks>See <see cref="PeekTokenRef"/> to avoid the copy (em C# os dois devolvem a mesma referência imutável).</remarks>
    public TokenWithSpan PeekToken() => PeekNthToken(0);

    /// <summary>
    /// Return a reference to the first non-whitespace token that has not yet
    /// been processed or Token::EOF
    /// </summary>
    public TokenWithSpan PeekTokenRef() => PeekNthTokenRef(0);

    /// <summary>
    /// Returns the <c>N</c> next non-whitespace tokens that have not yet been
    /// processed.
    /// </summary>
    /// <remarks>
    /// Rust: <c>peek_tokens::&lt;N&gt;()</c>, com <c>N</c> inferido pelo padrão de desestruturação.
    /// Em C#, <c>N</c> vira parâmetro (convencoes.md §13, caso 28).
    /// </remarks>
    public Token[] PeekTokens(int n) =>
        PeekTokensWithLocation(n).Select(withLoc => withLoc.Token).ToArray();

    /// <summary>
    /// Returns the <c>N</c> next non-whitespace tokens with locations that have not
    /// yet been processed.
    /// </summary>
    public TokenWithSpan[] PeekTokensWithLocation(int n)
    {
        var index = _index;
        var result = new TokenWithSpan[n];
        for (var i = 0; i < n; i++)
        {
            while (true)
            {
                var token = index < _tokens.Count ? _tokens[index] : null;
                index += 1;
                if (token is { Token: Token.Whitespace })
                {
                    continue;
                }
                result[i] = token ?? new TokenWithSpan(new Token.EOF(), Span.Empty());
                break;
            }
        }
        return result;
    }

    /// <summary>
    /// Returns references to the <c>N</c> next non-whitespace tokens
    /// that have not yet been processed.
    /// </summary>
    public TokenWithSpan[] PeekTokensRef(int n)
    {
        var index = _index;
        var result = new TokenWithSpan[n];
        for (var i = 0; i < n; i++)
        {
            while (true)
            {
                var token = index < _tokens.Count ? _tokens[index] : null;
                index += 1;
                if (token is { Token: Token.Whitespace })
                {
                    continue;
                }
                result[i] = token ?? EofToken;
                break;
            }
        }
        return result;
    }

    /// <summary>Return nth non-whitespace token that has not yet been processed</summary>
    public TokenWithSpan PeekNthToken(int n) => PeekNthTokenRef(n);

    /// <summary>Return nth non-whitespace token that has not yet been processed</summary>
    public TokenWithSpan PeekNthTokenRef(int n)
    {
        var index = _index;
        while (true)
        {
            index += 1;
            var token = index - 1 < _tokens.Count ? _tokens[index - 1] : null;
            if (token is { Token: Token.Whitespace })
            {
                continue;
            }
            if (n == 0)
            {
                return token ?? EofToken;
            }
            n -= 1;
        }
    }

    /// <summary>
    /// Return the first token, possibly whitespace, that has not yet been processed
    /// (or None if reached end-of-file).
    /// </summary>
    public TokenWithSpan PeekTokenNoSkip() => PeekNthTokenNoSkip(0);

    /// <summary>Return nth token, possibly whitespace, that has not yet been processed.</summary>
    public TokenWithSpan PeekNthTokenNoSkip(int n) =>
        _index + n < _tokens.Count ? _tokens[_index + n] : new TokenWithSpan(new Token.EOF(), Span.Empty());

    /// <summary>Return nth token, possibly whitespace, that has not yet been processed.</summary>
    internal TokenWithSpan PeekNthTokenNoSkipRef(int n) =>
        _index + n < _tokens.Count ? _tokens[_index + n] : EofToken;

    /// <summary>
    /// Return true if the next tokens exactly <c>expected</c>
    /// Does not advance the current token.
    /// </summary>
    internal bool PeekKeywords(params Keyword[] expected)
    {
        var index = _index;
        var matched = ParseKeywords(expected);
        _index = index;
        return matched;
    }

    /// <summary>Advances to the next non-whitespace token and returns a copy.</summary>
    /// <remarks>
    /// Please use <see cref="AdvanceToken"/> and <see cref="GetCurrentToken"/> to
    /// avoid the copy.
    /// </remarks>
    public TokenWithSpan NextToken()
    {
        AdvanceToken();
        return GetCurrentToken();
    }

    /// <summary>Returns the index of the current token</summary>
    /// <remarks>This can be used with APIs that expect an index, such as <see cref="TokenAt"/></remarks>
    public int GetCurrentIndex() => _index > 0 ? _index - 1 : 0;

    /// <summary>Return the next unprocessed token, possibly whitespace.</summary>
    public TokenWithSpan? NextTokenNoSkip()
    {
        _index += 1;
        return _index - 1 < _tokens.Count ? _tokens[_index - 1] : null;
    }

    /// <summary>Advances the current token to the next non-whitespace token</summary>
    /// <remarks>See <see cref="GetCurrentToken"/> to get the current token after advancing</remarks>
    public void AdvanceToken()
    {
        while (true)
        {
            _index += 1;
            if (_index - 1 < _tokens.Count && _tokens[_index - 1] is { Token: Token.Whitespace })
            {
                continue;
            }
            break;
        }
    }

    /// <summary>Returns a reference to the current token</summary>
    /// <remarks>Does not advance the current token.</remarks>
    public TokenWithSpan GetCurrentToken() => TokenAt(_index > 0 ? _index - 1 : 0);

    /// <summary>Returns a reference to the previous token</summary>
    /// <remarks>Does not advance the current token.</remarks>
    public TokenWithSpan GetPreviousToken() => TokenAt(_index > 1 ? _index - 2 : 0);

    /// <summary>Returns a reference to the next token</summary>
    /// <remarks>Does not advance the current token.</remarks>
    public TokenWithSpan GetNextToken() => TokenAt(_index);

    /// <summary>Seek back the last one non-whitespace token.</summary>
    /// <remarks>
    /// Must be called after <c>next_token()</c>, otherwise might panic. OK to call
    /// after <c>next_token()</c> indicates an EOF.
    /// </remarks>
    // TODO rename to backup_token and deprecate prev_token?
    public void PrevToken()
    {
        while (true)
        {
            if (!(_index > 0))
            {
                throw new InvalidOperationException("assertion failed: self.index > 0");
            }
            _index -= 1;
            if (_index < _tokens.Count && _tokens[_index] is { Token: Token.Whitespace })
            {
                continue;
            }
            return;
        }
    }

    /// <summary>Report <c>found</c> was encountered instead of <c>expected</c></summary>
    /// <remarks>
    /// Rust devolve <c>Err(..)</c>; aqui o método lança <see cref="ParserException"/> e nunca retorna.
    /// O parâmetro de tipo permite usá-lo como expressão: <c>return Expected&lt;Expr&gt;("...", tok);</c>.
    /// Em métodos <c>void</c>, use a sobrecarga sem tipo (convencoes.md §13, caso 45).
    /// </remarks>
    public T Expected<T>(string expected, TokenWithSpan found) =>
        throw ParserErr($"Expected: {expected}, found: {found}", found.Span.Start);

    /// <summary>Report <c>found</c> was encountered instead of <c>expected</c> (forma para métodos <c>void</c>).</summary>
    public void Expected(string expected, TokenWithSpan found) => Expected<object>(expected, found);

    /// <summary>report <c>found</c> was encountered instead of <c>expected</c></summary>
    public T ExpectedRef<T>(string expected, TokenWithSpan found) =>
        throw ParserErr($"Expected: {expected}, found: {found}", found.Span.Start);

    /// <summary>report <c>found</c> was encountered instead of <c>expected</c> (forma para métodos <c>void</c>).</summary>
    public void ExpectedRef(string expected, TokenWithSpan found) => ExpectedRef<object>(expected, found);

    /// <summary>Report that the token at <c>index</c> was found instead of <c>expected</c>.</summary>
    public T ExpectedAt<T>(string expected, int index)
    {
        var found = index >= 0 && index < _tokens.Count ? _tokens[index] : EofToken;
        throw ParserErr($"Expected: {expected}, found: {found}", found.Span.Start);
    }

    /// <summary>
    /// Rust: macro <c>parser_err!(MSG, loc)</c> = <c>Err(ParserError::ParserError(format!("{}{}", MSG, loc)))</c>.
    /// Devolve a exceção para ser lançada: <c>throw ParserErr("msg", loc);</c> (convencoes.md §13, caso 45).
    /// </summary>
    internal static ParserException ParserErr(string message, Location loc) =>
        new ParserException(new ParserError.ParserErrorValue(message + loc.ToString()));

    /// <summary>
    /// If the current token is the <c>expected</c> keyword, consume it and returns
    /// true. Otherwise, no tokens are consumed and returns false.
    /// </summary>
    public bool ParseKeyword(Keyword expected)
    {
        if (PeekKeyword(expected))
        {
            AdvanceToken();
            return true;
        }
        return false;
    }

    /// <summary>Check if the current token is the expected keyword without consuming it.</summary>
    /// <remarks>Returns true if the current token matches the expected keyword.</remarks>
    public bool PeekKeyword(Keyword expected) =>
        PeekTokenRef().Token is Token.Word { Value: var w } && expected == w.Keyword;

    /// <summary>
    /// If the current token is the <c>expected</c> keyword followed by
    /// specified tokens, consume them and returns true.
    /// Otherwise, no tokens are consumed and returns false.
    /// </summary>
    /// <remarks>
    /// Note that if the length of <c>tokens</c> is too long, this function will
    /// not be efficient as it does a loop on the tokens with <c>peek_nth_token</c>
    /// each time.
    /// </remarks>
    public bool ParseKeywordWithTokens(Keyword expected, params Token[] tokens) =>
        KeywordWithTokens(expected, tokens, true);

    /// <summary>
    /// Peeks to see if the current token is the <c>expected</c> keyword followed by specified tokens
    /// without consuming them.
    /// </summary>
    /// <remarks>See <see cref="ParseKeywordWithTokens"/> for details.</remarks>
    internal bool PeekKeywordWithTokens(Keyword expected, params Token[] tokens) =>
        KeywordWithTokens(expected, tokens, false);

    internal bool KeywordWithTokens(Keyword expected, Token[] tokens, bool consume)
    {
        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var w } when expected == w.Keyword:
            {
                for (var idx = 0; idx < tokens.Length; idx++)
                {
                    if (!PeekNthTokenRef(idx + 1).Token.Equals(tokens[idx]))
                    {
                        return false;
                    }
                }

                if (consume)
                {
                    for (var i = 0; i < tokens.Length + 1; i++)
                    {
                        AdvanceToken();
                    }
                }

                return true;
            }
            default:
                return false;
        }
    }

    /// <summary>
    /// If the current and subsequent tokens exactly match the <c>keywords</c>
    /// sequence, consume them and returns true. Otherwise, no tokens are
    /// consumed and returns false
    /// </summary>
    public bool ParseKeywords(params Keyword[] keywords) => ParseKeywordsIndexed(keywords) is not null;

    /// <summary>
    /// Just like <see cref="ParseKeywords"/>, but - upon success - returns the
    /// token index of the first keyword.
    /// </summary>
    internal int? ParseKeywordsIndexed(params Keyword[] keywords)
    {
        var startIndex = _index;
        int? firstKeywordIndex = null;
        foreach (var keyword in keywords)
        {
            if (!ParseKeyword(keyword))
            {
                _index = startIndex;
                return null;
            }
            if (firstKeywordIndex is null)
            {
                firstKeywordIndex = _index > 0 ? _index - 1 : 0;
            }
        }
        return firstKeywordIndex;
    }

    /// <summary>
    /// If the current token is one of the given <c>keywords</c>, returns the keyword
    /// that matches, without consuming the token. Otherwise, returns <c>None</c>.
    /// </summary>
    public Keyword? PeekOneOfKeywords(params Keyword[] keywords)
    {
        foreach (var keyword in keywords)
        {
            if (PeekKeyword(keyword))
            {
                return keyword;
            }
        }
        return null;
    }

    /// <summary>
    /// If the current token is one of the given <c>keywords</c>, consume the token
    /// and return the keyword that matches. Otherwise, no tokens are consumed
    /// and returns <c>None</c>.
    /// </summary>
    public Keyword? ParseOneOfKeywords(params Keyword[] keywords)
    {
        switch (PeekTokenRef().Token)
        {
            case Token.Word { Value: var w }:
                foreach (var keyword in keywords)
                {
                    if (keyword == w.Keyword)
                    {
                        AdvanceToken();
                        return keyword;
                    }
                }
                return null;
            default:
                return null;
        }
    }

    /// <summary>
    /// If the current token is one of the expected keywords, consume the token
    /// and return the keyword that matches. Otherwise, return an error.
    /// </summary>
    public Keyword ExpectOneOfKeywords(params Keyword[] keywords)
    {
        if (ParseOneOfKeywords(keywords) is { } keyword)
        {
            return keyword;
        }
        // format!("{x:?}") de Keyword = nome da variante (enum C#: ToString()); convencoes.md §13, caso 46.
        var keywordNames = keywords.Select(x => x.ToString()).ToList();
        return ExpectedRef<Keyword>($"one of {string.Join(" or ", keywordNames)}", PeekTokenRef());
    }

    /// <summary>
    /// If the current token is the <c>expected</c> keyword, consume the token.
    /// Otherwise, return an error.
    /// </summary>
    // todo deprecate in favor of expected_keyword_is
    public TokenWithSpan ExpectKeyword(Keyword expected)
    {
        if (ParseKeyword(expected))
        {
            return GetCurrentToken();
        }
        return ExpectedRef<TokenWithSpan>(expected.ToString(), PeekTokenRef());
    }

    /// <summary>
    /// If the current token is the <c>expected</c> keyword, consume the token.
    /// Otherwise, return an error.
    /// </summary>
    /// <remarks>
    /// This differs from expect_keyword only in that the matched keyword
    /// token is not returned.
    /// </remarks>
    public void ExpectKeywordIs(Keyword expected)
    {
        if (ParseKeyword(expected))
        {
            return;
        }
        ExpectedRef(expected.ToString(), PeekTokenRef());
    }

    /// <summary>
    /// If the current and subsequent tokens exactly match the <c>keywords</c>
    /// sequence, consume them and returns Ok. Otherwise, return an Error.
    /// </summary>
    public void ExpectKeywords(params Keyword[] expected)
    {
        foreach (var kw in expected)
        {
            ExpectKeywordIs(kw);
        }
    }

    /// <summary>Consume the next token if it matches the expected token, otherwise return false</summary>
    /// <remarks>See <see cref="AdvanceToken"/> to consume the token unconditionally</remarks>
    public bool ConsumeToken(Token expected)
    {
        if (PeekTokenRef().Equals(expected))
        {
            AdvanceToken();
            return true;
        }
        return false;
    }

    /// <summary>
    /// If the current and subsequent tokens exactly match the <c>tokens</c>
    /// sequence, consume them and returns true. Otherwise, no tokens are
    /// consumed and returns false
    /// </summary>
    public bool ConsumeTokens(params Token[] tokens)
    {
        var index = _index;
        foreach (var token in tokens)
        {
            if (!ConsumeToken(token))
            {
                _index = index;
                return false;
            }
        }
        return true;
    }

    /// <summary>Bail out if the current token is not an expected keyword, or consume it if it is</summary>
    public TokenWithSpan ExpectToken(Token expected)
    {
        if (PeekTokenRef().Equals(expected))
        {
            return NextToken();
        }
        return ExpectedRef<TokenWithSpan>(expected.ToString(), PeekTokenRef());
    }

    /// <summary>
    /// Rust: <c>fn parse&lt;T: FromStr&gt;(s: String, loc: Location)</c>. Os tipos usados pelo parser são
    /// <c>String</c>, <c>u64</c>, <c>i64</c> e <c>u32</c>; a mensagem de erro reproduz a do Rust
    /// (<c>core::any::type_name</c> + o <c>Display</c> de <c>ParseIntError</c>) — convencoes.md §13, caso 47.
    /// </summary>
    internal static T Parse<T>(string s, Location loc)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)s;
        }

        string typeName;
        object? value;
        string? error;
        if (typeof(T) == typeof(ulong))
        {
            typeName = "u64";
            error = RustParseInteger(s, signed: false, out var bits, out var negative, ulong.MaxValue);
            value = error is null ? bits : null;
            _ = negative;
        }
        else if (typeof(T) == typeof(uint))
        {
            typeName = "u32";
            error = RustParseInteger(s, signed: false, out var bits, out _, uint.MaxValue);
            value = error is null ? (uint)bits : null;
        }
        else if (typeof(T) == typeof(long))
        {
            typeName = "i64";
            error = RustParseInteger(s, signed: true, out var bits, out var negative, long.MaxValue);
            value = error is null ? (negative ? (bits == 9223372036854775808UL ? long.MinValue : -(long)bits) : (long)bits) : null;
        }
        else
        {
            throw new InvalidOperationException($"Parser.Parse<{typeof(T).Name}> não suportado (adicione o tipo, convencoes.md §13, caso 47).");
        }

        if (error is not null)
        {
            throw new ParserException(new ParserError.ParserErrorValue(
                $"Could not parse '{s}' as {typeName}: {error}{loc}"));
        }
        return (T)value!;
    }

    // Equivalente ao `from_str_radix(s, 10)` dos inteiros do Rust: aceita um sinal inicial ('+', e '-' só
    // nos tipos com sinal) e devolve a mensagem do `ParseIntError` em caso de erro.
    // `internal` (B6): também usado por SnowflakeDialect para o `str::parse::<i32>()` de
    // `parse_select_item_for_data_load`, cuja mensagem não passa por Parse<T>.
    internal static string? RustParseInteger(string s, bool signed, out ulong magnitude, out bool negative, ulong max)
    {
        magnitude = 0;
        negative = false;
        if (s.Length == 0)
        {
            return "cannot parse integer from empty string";
        }
        var start = 0;
        if (s[0] == '+' || (s[0] == '-' && signed))
        {
            negative = s[0] == '-';
            start = 1;
            if (s.Length == 1)
            {
                return "invalid digit found in string";
            }
        }
        // O limite do valor negativo de i64 é 2^63 (um a mais que o positivo).
        var limit = negative ? max + 1 : max;
        for (var i = start; i < s.Length; i++)
        {
            var c = s[i];
            if (c < '0' || c > '9')
            {
                return "invalid digit found in string";
            }
            var digit = (ulong)(c - '0');
            if (magnitude > (limit - digit) / 10)
            {
                return negative ? "number too small to fit in target type" : "number too large to fit in target type";
            }
            magnitude = magnitude * 10 + digit;
        }
        return null;
    }

    /// <summary>Parse a comma-separated list of 1+ SelectItem</summary>
    public Sequence<SelectItem> ParseProjection()
    {
        // BigQuery and Snowflake allow trailing commas, but only in project lists
        // e.g. `SELECT 1, 2, FROM t`
        // https://cloud.google.com/bigquery/docs/reference/standard-sql/lexical#trailing_commas
        // https://docs.snowflake.com/en/release-notes/2024/8_11#select-supports-trailing-commas

        var trailingCommas = _options.TrailingCommas | _dialect.SupportsProjectionTrailingCommas;

        return ParseCommaSeparatedWithTrailingCommas(
            p => p.ParseSelectItem(),
            trailingCommas,
            IsReservedForColumnAlias);
    }

    /// <summary>Parse a list of actions for <c>GRANT</c> statements.</summary>
    public Sequence<Ast.Action> ParseActionsList()
    {
        var values = new List<Ast.Action>();
        while (true)
        {
            values.Add(ParseGrantPermission());
            if (!ConsumeToken(new Token.Comma()))
            {
                break;
            }
            else if (_options.TrailingCommas)
            {
                var stop = false;
                switch (PeekTokenRef().Token)
                {
                    case Token.Word { Value: var kw } when kw.Keyword == Keyword.ON:
                        stop = true;
                        break;
                    case Token.RParen or Token.SemiColon or Token.EOF or Token.RBracket or Token.RBrace:
                        stop = true;
                        break;
                    default:
                        continue;
                }
                if (stop)
                {
                    break;
                }
            }
        }
        return values.ToSequence();
    }

    /// <summary>Parse a list of <see cref="TableWithJoins"/></summary>
    internal Sequence<TableWithJoins> ParseTableWithJoins()
    {
        var trailingCommas = _dialect.SupportsFromTrailingCommas;

        return ParseCommaSeparatedWithTrailingCommas(
            p => p.ParseTableAndJoins(),
            trailingCommas,
            (kw, parser) => !_dialect.IsTableFactor(kw, parser));
    }

    /// <summary>
    /// Parse the comma of a comma-separated syntax element.
    /// <c>R</c> is a predicate that should return true if the next
    /// keyword is a reserved keyword.
    /// Allows for control over trailing commas
    /// </summary>
    /// <remarks>Returns true if there is a next element</remarks>
    internal bool IsParseCommaSeparatedEndWithTrailingCommas(
        bool trailingCommas,
        Func<Keyword, Parser, bool> isReservedKeyword)
    {
        if (!ConsumeToken(new Token.Comma()))
        {
            return true;
        }
        else if (trailingCommas)
        {
            var token = NextToken().Token;
            var isEnd = token switch
            {
                Token.Word { Value: var kw } when isReservedKeyword(kw.Keyword, this) => true,
                Token.RParen or Token.SemiColon or Token.EOF or Token.RBracket or Token.RBrace => true,
                _ => false,
            };
            PrevToken();

            return isEnd;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Parse the comma of a comma-separated syntax element.
    /// Returns true if there is a next element
    /// </summary>
    internal bool IsParseCommaSeparatedEnd() =>
        IsParseCommaSeparatedEndWithTrailingCommas(
            _options.TrailingCommas,
            IsReservedForColumnAlias);

    /// <summary>Parse a comma-separated list of 1+ items accepted by <c>F</c></summary>
    public Sequence<T> ParseCommaSeparated<T>(Func<Parser, T> f) =>
        ParseCommaSeparatedWithTrailingCommas(
            f,
            _options.TrailingCommas,
            IsReservedForColumnAlias);

    /// <summary>
    /// Parse a comma-separated list of 1+ items accepted by <c>F</c>.
    /// <c>R</c> is a predicate that should return true if the next
    /// keyword is a reserved keyword.
    /// Allows for control over trailing commas.
    /// </summary>
    internal Sequence<T> ParseCommaSeparatedWithTrailingCommas<T>(
        Func<Parser, T> f,
        bool trailingCommas,
        Func<Keyword, Parser, bool> isReservedKeyword)
    {
        var values = new List<T>();
        while (true)
        {
            values.Add(f(this));
            if (IsParseCommaSeparatedEndWithTrailingCommas(trailingCommas, isReservedKeyword))
            {
                break;
            }
        }
        return values.ToSequence();
    }

    /// <summary>Parse a period-separated list of 1+ items accepted by <c>F</c></summary>
    internal Sequence<T> ParsePeriodSeparated<T>(Func<Parser, T> f)
    {
        var values = new List<T>();
        while (true)
        {
            values.Add(f(this));
            if (!ConsumeToken(new Token.Period()))
            {
                break;
            }
        }
        return values.ToSequence();
    }

    /// <summary>Parse a keyword-separated list of 1+ items accepted by <c>F</c></summary>
    public Sequence<T> ParseKeywordSeparated<T>(Keyword keyword, Func<Parser, T> f)
    {
        var values = new List<T>();
        while (true)
        {
            values.Add(f(this));
            if (!ParseKeyword(keyword))
            {
                break;
            }
        }
        return values.ToSequence();
    }

    /// <summary>Parse an expression enclosed in parentheses.</summary>
    public T ParseParenthesized<T>(Func<Parser, T> f)
    {
        ExpectToken(new Token.LParen());
        var res = f(this);
        ExpectToken(new Token.RParen());
        return res;
    }

    /// <summary>Parse a comma-separated list of 0+ items accepted by <c>F</c></summary>
    /// <param name="f">função que interpreta um item</param>
    /// <param name="endToken">expected end token for the closure (e.g. <c>Token::RParen</c>, <c>Token::RBrace</c> ...)</param>
    public Sequence<T> ParseCommaSeparated0<T>(Func<Parser, T> f, Token endToken)
    {
        if (PeekTokenRef().Token.Equals(endToken))
        {
            return Sequence<T>.Empty;
        }

        if (_options.TrailingCommas)
        {
            var next = PeekTokens(2);
            if (next[0] is Token.Comma && next[1].Equals(endToken))
            {
                _ = ConsumeToken(new Token.Comma());
                return Sequence<T>.Empty;
            }
        }

        return ParseCommaSeparated(f);
    }

    /// <summary>
    /// Parses 0 or more statements, each followed by a semicolon.
    /// If the next token is any of <c>terminal_keywords</c> then no more
    /// statements will be parsed.
    /// </summary>
    internal Sequence<Statement> ParseStatementList(params Keyword[] terminalKeywords)
    {
        var values = new List<Statement>();
        while (true)
        {
            var stop = false;
            switch (PeekNthTokenRef(0).Token)
            {
                case Token.EOF:
                    stop = true;
                    break;
                case Token.Word { Value: var w } when w.QuoteStyle is null && terminalKeywords.Contains(w.Keyword):
                    stop = true;
                    break;
            }
            if (stop)
            {
                break;
            }

            values.Add(ParseStatement());
            ExpectToken(new Token.SemiColon());
        }
        return values.ToSequence();
    }

    /// <summary>
    /// Default implementation of a predicate that returns true if
    /// the specified keyword is reserved for column alias.
    /// See <c>Dialect::is_column_alias</c>
    /// </summary>
    internal static bool IsReservedForColumnAlias(Keyword kw, Parser parser) =>
        !parser._dialect.IsColumnAlias(kw, parser);

    /// <summary>
    /// Run a parser method <c>f</c>, reverting back to the current position if unsuccessful.
    /// Returns <c>ParserError::RecursionLimitExceeded</c> if <c>f</c> returns a <c>RecursionLimitExceeded</c>.
    /// Returns <c>Ok(None)</c> if <c>f</c> returns any other error.
    /// </summary>
    /// <remarks>
    /// Para <c>T</c> tipo-valor (ex.: <c>u64</c>), use <see cref="MaybeParseValue{T}"/>, que devolve
    /// <c>Nullable&lt;T&gt;</c> (convencoes.md §13, caso 48).
    /// </remarks>
    public T? MaybeParse<T>(Func<Parser, T> f)
        where T : class
    {
        try
        {
            return TryParse(f);
        }
        catch (ParserException e) when (e.Error is not ParserError.RecursionLimitExceeded)
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="MaybeParse{T}"/> para tipos-valor: <c>Option&lt;u64&gt;</c> etc. viram <c>Nullable&lt;T&gt;</c>.
    /// </summary>
    public T? MaybeParseValue<T>(Func<Parser, T> f)
        where T : struct
    {
        try
        {
            return TryParse(f);
        }
        catch (ParserException e) when (e.Error is not ParserError.RecursionLimitExceeded)
        {
            return null;
        }
    }

    /// <summary>Run a parser method <c>f</c>, reverting back to the current position if unsuccessful.</summary>
    public T TryParse<T>(Func<Parser, T> f)
    {
        var index = _index;
        try
        {
            return f(this);
        }
        catch (ParserException)
        {
            // Unwind stack if limit exceeded
            _index = index;
            throw;
        }
    }
}
