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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/parser/mod.rs.
// Modificado: tradução de Rust para C#.
//
// Tipos auxiliares pequenos do topo de parser/mod.rs, na ordem do Rust, num só arquivo
// (convencoes.md §2: tipos pequenos e muito ligados podem dividir um arquivo).

using SqlParser.Core.Ast;

namespace SqlParser.Core;

/// <summary>Indicates whether a parser element is optional or mandatory.</summary>
public enum IsOptional
{
    /// <summary>The element is optional.</summary>
    Optional,
    /// <summary>The element is mandatory.</summary>
    Mandatory,
}

/// <summary>Indicates if a table expression is lateral.</summary>
public enum IsLateral
{
    /// <summary>The expression is lateral.</summary>
    Lateral,
    /// <summary>The expression is not lateral.</summary>
    NotLateral,
}

/// <summary>Represents a wildcard expression used in SELECT lists.</summary>
public abstract record WildcardExpr
{
    /// <summary>A specific expression used instead of a wildcard.</summary>
    public sealed record Expr(Ast.Expr Value) : WildcardExpr;

    /// <summary>A qualified wildcard like <c>table.*</c>.</summary>
    public sealed record QualifiedWildcard(ObjectName Value) : WildcardExpr;

    /// <summary>An unqualified <c>*</c> wildcard.</summary>
    public sealed record Wildcard : WildcardExpr;
}

/// <summary>
/// Composite types declarations using angle brackets syntax can be arbitrary
/// nested such that the following declaration is possible:
///      <c>ARRAY&lt;ARRAY&lt;INT&gt;&gt;</c>
/// But the tokenizer recognizes the <c>&gt;&gt;</c> as a ShiftRight token.
/// We work around that limitation when parsing a data type by accepting
/// either a <c>&gt;</c> or <c>&gt;&gt;</c> token in such cases, remembering which variant we
/// matched.
/// In the latter case having matched a <c>&gt;&gt;</c>, the parent type will not look to
/// match its closing <c>&gt;</c> as a result since that will have taken place at the
/// child type.
///
/// See <c>Parser::parse_data_type</c> for details
/// </summary>
internal sealed record MatchedTrailingBracket(bool Value)
{
    // Rust: `impl From<bool> for MatchedTrailingBracket`.
    public static MatchedTrailingBracket From(bool value) => new MatchedTrailingBracket(value);
}

/// <summary>Options that control how the <see cref="Parser"/> parses SQL text</summary>
/// <param name="TrailingCommas">Allow trailing commas in lists (e.g. <c>a, b,</c>).</param>
/// <param name="Unescape">Controls how literal values are unescaped. See <c>Tokenizer::with_unescape</c> for more details.</param>
/// <param name="RequireSemicolonStmtDelimiter">Controls if the parser expects a semi-colon token between statements. Default is <c>true</c>.</param>
// Struct do Rust com campos públicos e builders `with_x(mut self, ..) -> Self` que consomem o valor:
// aqui um record imutável cujos `WithX` devolvem uma cópia (o mesmo efeito observável).
public sealed record ParserOptions(bool TrailingCommas, bool Unescape, bool RequireSemicolonStmtDelimiter)
{
    /// <summary>Create a new <see cref="ParserOptions"/></summary>
    // Rust: `ParserOptions::new()` / `Default`.
    public ParserOptions()
        : this(TrailingCommas: false, Unescape: true, RequireSemicolonStmtDelimiter: true)
    {
    }

    /// <summary>
    /// Set if trailing commas are allowed.
    /// If this option is <c>false</c> (the default), the following SQL will
    /// not parse. If the option is <c>true</c>, the SQL will parse.
    /// <code>
    ///  SELECT
    ///   foo,
    ///   bar,
    ///  FROM baz
    /// </code>
    /// </summary>
    public ParserOptions WithTrailingCommas(bool trailingCommas) => this with { TrailingCommas = trailingCommas };

    /// <summary>Set if literal values are unescaped. Defaults to true. See <c>Tokenizer::with_unescape</c> for more details.</summary>
    public ParserOptions WithUnescape(bool unescape) => this with { Unescape = unescape };
}

/// <summary>The state of the parser.</summary>
// Rust: #[derive(Copy, Clone, PartialEq, Eq, PartialOrd, Ord)] (o `PartialEq`/`Ord` entrou no upstream
// #2619, para a chave de `failed_position_syntax_positions`): o `enum` C# já tem igualdade e ordem pelos
// valores, na ordem de declaração, como o derive.
internal enum ParserState
{
    /// <summary>The default state of the parser.</summary>
    Normal,
    /// <summary>
    /// The state when parsing a CONNECT BY expression. This allows parsing
    /// PRIOR expressions while still allowing prior as an identifier name
    /// in other contexts.
    /// </summary>
    ConnectBy,
    /// <summary>
    /// The state when parsing column definitions.  This state prohibits
    /// NOT NULL as an alias for IS NOT NULL.  For example:
    /// <code>CREATE TABLE foo (abc BIGINT NOT NULL);</code>
    /// </summary>
    ColumnDefinition,
}

/// <summary>
/// Copy marker for a <see cref="ParserError"/> cached by the <c>parse_prefix</c> failure
/// memoization, so the caches hold no strings.
/// </summary>
internal abstract record ExprPrefixError
{
    public sealed record RecursionLimitExceeded : ExprPrefixError;

    public sealed record Err : ExprPrefixError;

    // Rust: `impl From<&ParserError> for ExprPrefixError`.
    public static ExprPrefixError From(ParserError e) =>
        e switch
        {
            ParserError.RecursionLimitExceeded => new RecursionLimitExceeded(),
            _ => new Err(),
        };
}
