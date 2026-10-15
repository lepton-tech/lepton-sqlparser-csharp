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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs (linhas 1–610:
// declaração do Parser, construtor, opções, try_with_sql, parse_statements, parse_sql).
// Modificado: tradução de Rust para C#.

using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Core;

/// <summary>A SQL Parser</summary>
/// <remarks>
/// This struct is the main entry point for parsing SQL queries.
/// <para>Functionality:
/// Parsing SQL: see examples on <c>Parser::new</c> and <see cref="ParseSql"/>;
/// Controlling recursion: See <see cref="WithRecursionLimit"/>;
/// Controlling parser options: See <see cref="WithOptions"/>;
/// Providing your own tokens: See <see cref="WithTokens"/>.</para>
/// <para>Internals: the parser uses a <see cref="Tokenizer"/> to tokenize the input SQL string into a
/// <c>Vec</c> of <see cref="TokenWithSpan"/>s and maintains an <c>index</c> to the current token
/// being processed. The token vec may contain multiple SQL statements.
/// The "current" token is the token at <c>index - 1</c>;
/// the "next" token is the token at <c>index</c>;
/// the "previous" token is the token at <c>index - 2</c>.
/// If <c>index</c> is equal to the length of the token stream, the 'next' token is <c>Token::EOF</c>.</para>
/// </remarks>
// O `Parser` é dividido em arquivos `partial` por trecho do `parser/mod.rs`; o cabeçalho
// de cada arquivo diz qual trecho ele porta (ver docs/port/progresso.md).
public sealed partial class Parser
{
    // By default, allow expressions up to this deep before erroring
    private const int DefaultRemainingDepth = 50;

    // A constant EOF token that can be referenced.
    private static readonly TokenWithSpan EofToken =
        new TokenWithSpan(new Token.EOF(), new Span(new Location(0, 0), new Location(0, 0)));

    /// <summary>The tokens</summary>
    private List<TokenWithSpan> _tokens;

    /// <summary>The index of the first unprocessed token in <c>Parser::tokens</c>.</summary>
    private int _index;

    /// <summary>The current state of the parser.</summary>
    private ParserState _state;

    /// <summary>The SQL dialect to use.</summary>
    private readonly Dialect _dialect;

    /// <summary>
    /// Additional options that allow you to mix &amp; match behavior
    /// otherwise constrained to certain dialects (e.g. trailing
    /// commas) and/or format of parse (e.g. unescaping).
    /// </summary>
    private ParserOptions _options;

    /// <summary>Ensures the stack does not overflow by limiting recursion depth.</summary>
    private RecursionCounter _recursionCounter;

    /// <summary>
    /// Cached failures from <c>parse_prefix</c> calls that returned <c>Err</c>. See
    /// <c>Parser::parse_prefix</c> for the 2^N patterns this guards.
    /// </summary>
    private readonly SortedDictionary<int, ExprPrefixError> _failedPrefixPositions;

    /// <summary>
    /// Cached failures from the speculative reserved-word prefix arm. See
    /// <c>Parser::parse_prefix</c> for the 2^N patterns this guards.
    /// </summary>
    private readonly SortedDictionary<int, ExprPrefixError> _failedReservedWordPrefixPositions;

    /// <summary>
    /// Cached failures from the speculative derived-table arm of
    /// <c>parse_table_factor</c>. See <c>Parser::parse_table_factor</c> for the 2^N
    /// pattern this guards.
    /// </summary>
    private readonly SortedSet<int> _failedDerivedTableFactorPositions;

    /// <summary>
    /// Cached failures of the speculative <c>POSITION(&lt;expr&gt; IN &lt;expr&gt;)</c> arm.
    /// See <c>Parser::parse_position_expr</c> for the 2^N pattern this guards.
    /// Keyed by state too, since <c>PRIOR</c> parses differently in <c>CONNECT BY</c>.
    /// </summary>
    private readonly SortedSet<(int, ParserState)> _failedPositionSyntaxPositions;

    /// <summary>
    /// Cached failures from the speculative subquery arm of <c>Parser::parse_in</c>,
    /// which <c>IN</c> lists nested in a leading subquery would otherwise retry at
    /// every level, taking 2^N time. Maps the arm's start to where a parsed
    /// query lacked its <c>)</c>.
    /// </summary>
    private readonly SortedDictionary<int, int?> _failedInSubqueryPositions;

    /// <summary>Create a parser for a <see cref="Dialect"/></summary>
    /// <remarks>
    /// See also <see cref="ParseSql"/>. Example:
    /// <code>
    /// var statements = new Parser(new GenericDialect())
    ///   .TryWithSql("SELECT * FROM foo")
    ///   .ParseStatements();
    /// </code>
    /// </remarks>
    public Parser(Dialect dialect)
    {
        _tokens = new List<TokenWithSpan>();
        _index = 0;
        _state = ParserState.Normal;
        _dialect = dialect;
        _recursionCounter = new RecursionCounter(DefaultRemainingDepth);
        _options = new ParserOptions().WithTrailingCommas(dialect.SupportsTrailingCommas);
        _failedPrefixPositions = new SortedDictionary<int, ExprPrefixError>();
        _failedReservedWordPrefixPositions = new SortedDictionary<int, ExprPrefixError>();
        _failedDerivedTableFactorPositions = new SortedSet<int>();
        _failedPositionSyntaxPositions = new SortedSet<(int, ParserState)>();
        _failedInSubqueryPositions = new SortedDictionary<int, int?>();
    }

    /// <summary>Specify the maximum recursion limit while parsing.</summary>
    /// <remarks>
    /// <see cref="Parser"/> prevents stack overflows by returning
    /// <c>ParserError::RecursionLimitExceeded</c> if the parser exceeds
    /// this depth while processing the query.
    /// Note: Versions prior to <c>0.63.0</c> did not enforce any limit in builds
    /// without the "std" feature.
    /// </remarks>
    public Parser WithRecursionLimit(int recursionLimit)
    {
        _recursionCounter = new RecursionCounter(recursionLimit);
        return this;
    }

    /// <summary>Specify additional parser options</summary>
    /// <remarks>
    /// <see cref="Parser"/> supports additional options (<see cref="ParserOptions"/>)
    /// that allow you to mix &amp; match behavior otherwise constrained
    /// to certain dialects (e.g. trailing commas).
    /// </remarks>
    public Parser WithOptions(ParserOptions options)
    {
        _options = options;
        return this;
    }

    /// <summary>Reset this parser to parse the specified token stream</summary>
    public Parser WithTokensWithLocations(List<TokenWithSpan> tokens)
    {
        _tokens = tokens;
        _index = 0;
        _failedPrefixPositions.Clear();
        _failedReservedWordPrefixPositions.Clear();
        _failedDerivedTableFactorPositions.Clear();
        _failedPositionSyntaxPositions.Clear();
        _failedInSubqueryPositions.Clear();
        return this;
    }

    /// <summary>Reset this parser state to parse the specified tokens</summary>
    public Parser WithTokens(List<Token> tokens)
    {
        // Put in dummy locations
        var tokensWithLocations = tokens
            .Select(token => new TokenWithSpan(token, Span.Empty()))
            .ToList();
        return WithTokensWithLocations(tokensWithLocations);
    }

    /// <summary>
    /// Tokenize the sql string and sets this <see cref="Parser"/>'s state to
    /// parse the resulting tokens
    /// </summary>
    /// <remarks>Returns an error if there was an error tokenizing the SQL string.</remarks>
    public Parser TryWithSql(string sql)
    {
        // Rust: debug!("Parsing sql '{sql}'..."): logging não é traduzido (convencoes.md §13, caso 44).
        List<TokenWithSpan> tokens;
        try
        {
            tokens = new Tokenizer(_dialect, sql)
                .WithUnescape(_options.Unescape)
                .TokenizeWithLocation();
        }
        catch (TokenizerException e)
        {
            // `?` com `impl From<TokenizerError> for ParserError`
            throw new ParserException(ParserError.From(e.Error));
        }
        return WithTokensWithLocations(tokens);
    }

    /// <summary>Parse potentially multiple statements</summary>
    /// <remarks>
    /// Example:
    /// <code>
    /// var statements = new Parser(new GenericDialect())
    ///   // Parse a SQL string with 2 separate statements
    ///   .TryWithSql("SELECT * FROM foo; SELECT * FROM bar;")
    ///   .ParseStatements();
    /// // statements.Count == 2
    /// </code>
    /// </remarks>
    public Sequence<Statement> ParseStatements()
    {
        var stmts = new List<Statement>();
        var expectingStatementDelimiter = false;
        while (true)
        {
            // ignore empty statements (between successive statement delimiters)
            while (ConsumeToken(new Token.SemiColon()))
            {
                expectingStatementDelimiter = false;
            }

            if (!_options.RequireSemicolonStmtDelimiter)
            {
                expectingStatementDelimiter = false;
            }

            var stop = false;
            switch (PeekTokenRef().Token)
            {
                case Token.EOF:
                    stop = true;
                    break;
                // end of statement
                case Token.Word { Value: var word } when expectingStatementDelimiter && word.Keyword == Keyword.END:
                    stop = true;
                    break;
            }
            if (stop)
            {
                break;
            }

            if (expectingStatementDelimiter)
            {
                return ExpectedRef<Sequence<Statement>>("end of statement", PeekTokenRef());
            }

            var statement = ParseStatement();
            stmts.Add(statement);
            expectingStatementDelimiter = true;
        }
        return stmts.ToSequence();
    }

    /// <summary>
    /// Convenience method to parse a string with one or more SQL
    /// statements into produce an Abstract Syntax Tree (AST).
    /// </summary>
    /// <remarks>
    /// Example: <c>var statements = Parser.ParseSql(new GenericDialect(), "SELECT * FROM foo");</c>
    /// </remarks>
    public static Sequence<Statement> ParseSql(Dialect dialect, string sql) =>
        new Parser(dialect).TryWithSql(sql).ParseStatements();

    /// <summary>
    /// Parses the given <c>sql</c> into an Abstract Syntax Tree (AST), returning
    /// also encountered source code comments.
    /// </summary>
    /// <remarks>See <see cref="ParseSql"/>.</remarks>
    // A tupla Rust `(Vec<Statement>, Comments)` vira uma tupla nomeada (nomes.md).
    public static (Sequence<Statement> Statements, Comments Comments) ParseSqlWithComments(Dialect dialect, string sql)
    {
        var p = new Parser(dialect).TryWithSql(sql);
        var stmts = p.ParseStatements();
        return (stmts, p.IntoComments());
    }

    /// <summary>Consumes this parser returning comments from the parsed token stream.</summary>
    public Comments IntoComments()
    {
        var comments = new Comments();
        foreach (var t in _tokens)
        {
            switch (t.Token)
            {
                case Token.Whitespace { Value: Whitespace.SingleLineComment singleLine }:
                    comments.Offer(new CommentWithSpan(
                        Comment: new Comment.SingleLine(Content: singleLine.Comment, Prefix: singleLine.Prefix),
                        Span: t.Span));
                    break;
                case Token.Whitespace { Value: Whitespace.MultiLineComment multiLine }:
                    comments.Offer(new CommentWithSpan(
                        Comment: new Comment.MultiLine(multiLine.Value),
                        Span: t.Span));
                    break;
            }
        }
        return comments;
    }
}
