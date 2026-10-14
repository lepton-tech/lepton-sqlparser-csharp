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
// Modificado: tradução de Rust para C#. Só as partes que não dependem do parser; o restante
// está listado em docs/port/progresso.md (Fase 2).

using SqlParser.Core.Dialects;

namespace SqlParser.Core.Tests;

/// <summary>
/// Tests use the methods on this struct to invoke the parser on one or
/// multiple dialects.
/// </summary>
public sealed class TestedDialects
{
    /// <summary>Rust: <c>pub dialects: Vec&lt;Box&lt;dyn Dialect&gt;&gt;</c>.</summary>
    public List<Dialect> Dialects { get; }

    // PENDENTE (Fase 2): `pub options: Option<ParserOptions>` e `pub recursion_limit: Option<usize>`,
    // com `new_with_options`, `with_recursion_limit` e `new_parser`. Dependem de `ParserOptions`/`Parser`.

    /// <summary>Create a TestedDialects with default options and the given dialects.</summary>
    public TestedDialects(List<Dialect> dialects)
    {
        Dialects = dialects;
    }

    // PENDENTE (Fase 2): one_of_identical_results, run_parser_method, parse_sql_statements,
    // one_statement_parses_to, statements_parse_to, expr_parses_to, verified_stmt, verified_query,
    // verified_query_with_canonical, verified_only_select, verified_only_select_with_canonical,
    // verified_expr. Dependem do Parser e da AST.

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
            // PENDENTE (Fase 2): if let Some(options) = &self.options {
            //     tokenizer = tokenizer.with_unescape(options.unescape); }
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
}
