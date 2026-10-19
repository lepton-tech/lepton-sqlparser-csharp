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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/test_utils.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

/// <summary>
/// Tests use the methods on this struct to invoke the parser on one or
/// multiple dialects.
/// </summary>
/// <remarks>
/// Os métodos que no Rust devolvem <c>Result</c> lançam <see cref="ParserException"/>; para conferir
/// um erro, use <c>Assert.Throws&lt;ParserException&gt;(() =&gt; d.ParseSqlStatements(sql))</c> e compare
/// <c>ex.Error</c> (skill port-rust-tests).
/// </remarks>
public sealed class TestedDialects
{
    /// <summary>Rust: <c>pub dialects: Vec&lt;Box&lt;dyn Dialect&gt;&gt;</c>.</summary>
    public List<Dialect> Dialects { get; }

    /// <summary>Rust: <c>pub options: Option&lt;ParserOptions&gt;</c>.</summary>
    public ParserOptions? Options { get; }

    /// <summary>Rust: <c>pub recursion_limit: Option&lt;usize&gt;</c>.</summary>
    public int? RecursionLimit { get; private set; }

    /// <summary>Create a TestedDialects with default options and the given dialects.</summary>
    public TestedDialects(List<Dialect> dialects)
        : this(dialects, null)
    {
    }

    private TestedDialects(List<Dialect> dialects, ParserOptions? options)
    {
        Dialects = dialects;
        Options = options;
        RecursionLimit = null;
    }

    /// <summary>Rust: <c>TestedDialects::new_with_options</c>.</summary>
    public static TestedDialects NewWithOptions(List<Dialect> dialects, ParserOptions options) =>
        new TestedDialects(dialects, options);

    /// <summary>Rust: <c>with_recursion_limit(mut self, ..) -&gt; Self</c> (convencoes.md §13, caso 24).</summary>
    public TestedDialects WithRecursionLimit(int recursionLimit)
    {
        RecursionLimit = recursionLimit;
        return this;
    }

    private Parser NewParser(Dialect dialect)
    {
        var parser = new Parser(dialect);
        if (Options is { } options)
        {
            parser = parser.WithOptions(options);
        }

        if (RecursionLimit is { } recursionLimit)
        {
            parser = parser.WithRecursionLimit(recursionLimit);
        }

        return parser;
    }

    /// <summary>
    /// Run the given function for all of <c>self.dialects</c>, assert that they
    /// return the same result, and return that result.
    /// </summary>
    /// <remarks>
    /// No Rust, <c>f</c> pode devolver um <c>Result</c>, e os erros também são comparados entre dialetos.
    /// Aqui o erro é uma <see cref="ParserException"/>: ela é capturada, comparada pelo
    /// <see cref="ParserException.Error"/> e, se todos os dialetos falharem igual, relançada no fim.
    /// </remarks>
    public T OneOfIdenticalResults<T>(Func<Dialect, T> f)
    {
        if (Dialects.Count == 0)
        {
            throw new InvalidOperationException("tested dialects cannot be empty");
        }

        Dialect? prevDialect = null;
        var prevHasValue = false;
        T prevValue = default!;
        ParserException? prevError = null;
        foreach (var dialect in Dialects)
        {
            var hasValue = false;
            T value = default!;
            ParserException? error = null;
            try
            {
                value = f(dialect);
                hasValue = true;
            }
            catch (ParserException e)
            {
                error = e;
            }

            if (prevDialect is not null)
            {
                var message = $"Parse results with {prevDialect.GetType().Name} are different from {dialect.GetType().Name}";
                if (prevHasValue != hasValue)
                {
                    Assert.Fail($"{message}: {(prevHasValue ? "Ok" : prevError!.Error.ToString())} vs {(hasValue ? "Ok" : error!.Error.ToString())}");
                }
                else if (hasValue)
                {
                    AssertEqualWithMessage(prevValue, value, message);
                }
                else
                {
                    AssertEqualWithMessage(prevError!.Error, error!.Error, message);
                }
            }

            prevDialect = dialect;
            prevHasValue = hasValue;
            prevValue = value;
            prevError = error;
        }

        if (!prevHasValue)
        {
            throw prevError!;
        }
        return prevValue;
    }

    private static void AssertEqualWithMessage<TValue>(TValue expected, TValue actual, string message) =>
        TestUtils.AssertEqualWithMessage(expected, actual, message);

    /// <summary>Rust: <c>run_parser_method</c>: cria o parser (com as opções) e aplica <paramref name="f"/> em cada dialeto.</summary>
    public T RunParserMethod<T>(string sql, Func<Parser, T> f) =>
        OneOfIdenticalResults(dialect =>
        {
            var parser = NewParser(dialect).TryWithSql(sql);
            return f(parser);
        });

    /// <summary>Parses a single SQL string into multiple statements, ensuring the result is the same for all tested dialects.</summary>
    public Sequence<Statement> ParseSqlStatements(string sql) =>
        OneOfIdenticalResults(dialect =>
        {
            var tokenizer = new Tokenizer(dialect, sql);
            if (Options is { } options)
            {
                tokenizer = tokenizer.WithUnescape(options.Unescape);
            }
            List<Token> tokens;
            try
            {
                tokens = tokenizer.Tokenize();
            }
            catch (TokenizerException e)
            {
                // `?` com `impl From<TokenizerError> for ParserError`
                throw new ParserException(ParserError.From(e.Error));
            }
            return NewParser(dialect)
                .WithTokens(tokens)
                .ParseStatements();
        });

    /// <summary>
    /// Ensures that <c>sql</c> parses as a single <see cref="Statement"/> for all tested
    /// dialects.
    /// </summary>
    /// <remarks>
    /// In general, the canonical SQL should be the same (see crate
    /// documentation for rationale) and you should prefer the <c>verified_</c>
    /// variants in testing, such as <see cref="VerifiedStmt"/> or
    /// <see cref="VerifiedQuery"/>.
    /// If <c>canonical</c> is non empty,this function additionally asserts
    /// that:
    /// 1. parsing <c>sql</c> results in the same <see cref="Statement"/> as parsing
    /// <c>canonical</c>.
    /// 2. re-serializing the result of parsing <c>sql</c> produces the same
    /// <c>canonical</c> sql string
    /// For multiple statements, use <see cref="StatementsParseTo"/>.
    /// </remarks>
    public Statement OneStatementParsesTo(string sql, string canonical)
    {
        var statements = Expect(() => ParseSqlStatements(sql), sql);
        Assert.Single(statements); // assert_eq!(statements.len(), 1)
        if (canonical.Length != 0 && sql != canonical)
        {
            Assert.Equal(ParseSqlStatements(canonical), statements);
        }

        var onlyStatement = statements[0];

        if (canonical.Length != 0)
        {
            Assert.Equal(canonical, onlyStatement.ToString());
        }
        return onlyStatement;
    }

    /// <summary>
    /// The same as <see cref="OneStatementParsesTo"/> but it works for a multiple statements
    /// </summary>
    public Sequence<Statement> StatementsParseTo(string sql, string canonical)
    {
        var statements = Expect(() => ParseSqlStatements(sql), sql);
        if (canonical.Length != 0 && sql != canonical)
        {
            Assert.Equal(ParseSqlStatements(canonical), statements);
        }
        else
        {
            Assert.Equal(
                sql,
                string.Join("; ", statements.Select(s => s.ToString())));
        }
        return statements;
    }

    /// <summary>
    /// Ensures that <c>sql</c> parses as an <see cref="Expr"/>, and that
    /// re-serializing the parse result produces canonical
    /// </summary>
    public Expr ExprParsesTo(string sql, string canonical)
    {
        var ast = RunParserMethod(sql, parser => parser.ParseExpr());
        Assert.Equal(canonical, ast.ToString());
        return ast;
    }

    /// <summary>
    /// Ensures that <c>sql</c> parses as a single <see cref="Statement"/>, and that
    /// re-serializing the parse result produces the same <c>sql</c>
    /// string (is not modified after a serialization round-trip).
    /// </summary>
    /// <remarks>Rust: <c>verified_stmt</c>.</remarks>
    public Statement VerifiedStmt(string sql) => OneStatementParsesTo(sql, sql);

    /// <summary>
    /// Ensures that <c>sql</c> parses as a single <see cref="Query"/>, and that
    /// re-serializing the parse result produces the same <c>sql</c>
    /// string (is not modified after a serialization round-trip).
    /// </summary>
    public Query VerifiedQuery(string sql) =>
        VerifiedStmt(sql) switch
        {
            Statement.Query query => query.Value,
            _ => throw new InvalidOperationException("Expected Query"),
        };

    /// <summary>
    /// Ensures that <c>sql</c> parses as a single <see cref="Query"/>, and that
    /// re-serializing the parse result matches the given canonical
    /// sql string.
    /// </summary>
    public Query VerifiedQueryWithCanonical(string query, string canonical) =>
        OneStatementParsesTo(query, canonical) switch
        {
            Statement.Query q => q.Value,
            _ => throw new InvalidOperationException("Expected Query"),
        };

    /// <summary>
    /// Ensures that <c>sql</c> parses as a single <see cref="Select"/>, and that
    /// re-serializing the parse result produces the same <c>sql</c>
    /// string (is not modified after a serialization round-trip).
    /// </summary>
    public Select VerifiedOnlySelect(string query) =>
        VerifiedQuery(query).Body switch
        {
            SetExpr.Select s => s.Value,
            _ => throw new InvalidOperationException("Expected SetExpr::Select"),
        };

    /// <summary>
    /// Ensures that <c>sql</c> parses as a single <see cref="Select"/>, and that additionally:
    /// 1. parsing <c>sql</c> results in the same <see cref="Statement"/> as parsing
    /// <c>canonical</c>.
    /// 2. re-serializing the result of parsing <c>sql</c> produces the same
    /// <c>canonical</c> sql string
    /// </summary>
    public Select VerifiedOnlySelectWithCanonical(string query, string canonical)
    {
        var q = OneStatementParsesTo(query, canonical) switch
        {
            Statement.Query qq => qq.Value,
            _ => throw new InvalidOperationException("Expected Query"),
        };
        return q.Body switch
        {
            SetExpr.Select s => s.Value,
            _ => throw new InvalidOperationException("Expected SetExpr::Select"),
        };
    }

    /// <summary>
    /// Ensures that <c>sql</c> parses as an <see cref="Expr"/>, and that
    /// re-serializing the parse result produces the same <c>sql</c>
    /// string (is not modified after a serialization round-trip).
    /// </summary>
    public Expr VerifiedExpr(string sql) => ExprParsesTo(sql, sql);

    /// <summary>Check that the tokenizer returns the expected tokens for the given SQL.</summary>
    public void TokenizesTo(string sql, List<Token> expected)
    {
        if (Dialects.Count == 0)
        {
            throw new InvalidOperationException("No dialects to test");
        }

        foreach (var dialect in Dialects)
        {
            var tokenizer = new Tokenizer(dialect, sql);
            if (Options is { } options)
            {
                tokenizer = tokenizer.WithUnescape(options.Unescape);
            }
            var tokens = tokenizer.Tokenize();
            try
            {
                Assert.Equal(expected, tokens);
            }
            catch (Xunit.Sdk.XunitException e)
            {
                // assert_eq!(expected, tokens, "Tokenized differently for {dialect:?}")
                Assert.Fail($"Tokenized differently for {dialect.GetType().Name}{Environment.NewLine}{e.Message}");
            }
        }
    }

    // Rust: `.expect(sql)`: em caso de erro, falha com "{sql}: {erro}".
    private static TResult Expect<TResult>(Func<TResult> f, string message)
    {
        try
        {
            return f();
        }
        catch (ParserException e)
        {
            throw new Xunit.Sdk.XunitException($"{message}: {e.Error}");
        }
    }
}
