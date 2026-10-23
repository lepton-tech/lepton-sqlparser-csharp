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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/sqlite.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.sqlite.org">SQLite</see>
/// <para>
/// This dialect allows columns in a
/// <see href="https://sqlite.org/lang_createtable.html"><c>CREATE TABLE</c></see> statement with no
/// type specified, as in <c>CREATE TABLE t1 (a)</c>. In the AST, these columns will
/// have the data type <c>Unspecified</c> (<c>crate::ast::DataType::Unspecified</c>).
/// </para>
/// </summary>
public class SQLiteDialect : Dialect
{
    // see https://www.sqlite.org/lang_keywords.html
    // parse `...`, [...] and "..." as identifier
    // TODO: support depending on the context tread '...' as identifier too.
    public override bool IsDelimitedIdentifierStart(char ch) => ch == '`' || ch == '"' || ch == '[';

    public override char? IdentifierQuoteStyle(string identifier) => '`';

    public override bool IsIdentifierStart(char ch) =>
        // See https://www.sqlite.org/draft/tokenreq.html
        (ch >= 'a' && ch <= 'z')
            || (ch >= 'A' && ch <= 'Z')
            || ch == '_'
            // Port: fora do BMP, as duas metades do par substituto (>= U+D800) passam no teste, o que dá
            // o mesmo resultado do Rust, que recebe o scalar inteiro (convencoes.md §13, caso 14).
            || ch >= '\u0080';

    public override bool SupportsFilterDuringAggregation => true;

    public override bool SupportsStartTransactionModifier => true;

    public override bool IsIdentifierPart(char ch) => IsIdentifierStart(ch) || (ch >= '0' && ch <= '9') || ch == '$';

    public override Statement? ParseStatement(Parser parser)
    {
        if (parser.ParseKeyword(Keyword.REPLACE))
        {
            parser.PrevToken();
            return parser.ParseInsert(parser.GetCurrentToken());
        }
        else
        {
            return null;
        }
    }

    public override Expr? ParseInfix(Parser parser, Expr expr, byte precedence)
    {
        // Parse MATCH, REGEXP and GLOB as operators
        // See <https://www.sqlite.org/lang_expr.html#the_like_glob_regexp_match_and_extract_operators>
        foreach (var (keyword, op) in new (Keyword, BinaryOperator)[]
        {
            (Keyword.REGEXP, new BinaryOperator.Regexp()),
            (Keyword.MATCH, new BinaryOperator.Match()),
            (Keyword.GLOB, new BinaryOperator.Glob()),
        })
        {
            if (parser.ParseKeyword(keyword))
            {
                var left = expr;
                // Rust: `Err(e) => return Some(Err(e))`: o erro propaga como exceção (caso 53).
                var right = parser.ParseSubexpr(precedence);
                return new Expr.BinaryOp(Left: left, Op: op, Right: right);
            }
        }
        return null;
    }

    /// <summary>
    /// <c>&amp;</c> and <c>|</c> share one left-associative precedence level.
    /// See <see href="https://www.sqlite.org/lang_expr.html#operators_and_parse_affecting_attributes"/>
    /// </summary>
    public override byte? GetNextPrecedence(Parser parser)
    {
        switch (parser.PeekTokenRef().Token)
        {
            case Token.Ampersand or Token.Pipe:
                return PrecValue(Precedence.Ampersand);
            default:
                return null;
        }
    }

    public override bool SupportsInEmptyList => true;

    public override bool SupportsLimitComma => true;

    public override bool SupportsAscDescInColumnDefinition => true;

    public override bool SupportsDollarPlaceholder => true;

    /// <summary>
    /// SQLite supports <c>NOTNULL</c> as aliases for <c>IS NOT NULL</c>
    /// See: <see href="https://sqlite.org/syntax/expr.html"/>
    /// </summary>
    public override bool SupportsNotnullOperator => true;

    public override bool SupportsCommaSeparatedTrim => true;

    public override bool SupportsNumericLiteralUnderscores => true;

    public override bool SupportsDoubleEqAssignment => true;

    public override bool SupportsStringLiteralColumnNames => true;

    public override bool SupportsCastEmptyDataTypeToUnspecified => true;

    public override bool SupportsNationalStringLiteral => false;

    public override bool SupportsParensAroundTableFactor => true;

    public override bool SupportsUnterminatedBlockComments => true;
}
