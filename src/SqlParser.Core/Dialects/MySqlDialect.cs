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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/mysql.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.mysql.com/">MySQL</see>
/// </summary>
public class MySqlDialect : Dialect
{
    // Constante de módulo no Rust (antes do struct); usada só por is_table_factor_alias (Fase 2).
    private static readonly Keyword[] ReservedForTableAliasMysql =
    {
        Keyword.USE,
        Keyword.IGNORE,
        Keyword.FORCE,
        Keyword.STRAIGHT_JOIN,
    };

    public override bool IsIdentifierStart(char ch) =>
        // See https://dev.mysql.com/doc/refman/8.0/en/identifiers.html.
        // Identifiers which begin with a digit are recognized while tokenizing numbers,
        // so they can be distinguished from exponent numeric literals.
        // MySQL also implements non ascii utf-8 charecters
        char.IsLetter(ch)
            || ch == '_'
            || ch == '$'
            || ch == '@'
            || (ch >= '\u0080' && ch <= '\uffff' && !char.IsSurrogate(ch))
            || ch > '\u007f';

    public override bool IsIdentifierPart(char ch) =>
        IsIdentifierStart(ch) || (ch >= '0' && ch <= '9') ||
        // MySQL implements Unicode characters in identifiers.
        ch > '\u007f';

    public override bool IsDelimitedIdentifierStart(char ch) => ch == '`';

    public override char? IdentifierQuoteStyle(string identifier) => '`';

    // See https://dev.mysql.com/doc/refman/8.0/en/string-literals.html#character-escape-sequences
    public override bool SupportsStringLiteralBackslashEscape => true;

    /// <summary>
    /// see <see href="https://dev.mysql.com/doc/refman/8.4/en/string-functions.html#function_concat"/>
    /// </summary>
    public override bool SupportsStringLiteralConcatenation => true;

    public override bool IgnoresWildcardEscapes => true;

    public override bool SupportsNumericPrefix => true;

    public override bool SupportsBitwiseShiftOperators => true;

    /// <summary>
    /// see <see href="https://dev.mysql.com/doc/refman/8.4/en/comments.html"/>
    /// </summary>
    public override bool SupportsMultilineCommentHints => true;

    // PENDENTE (Fase 2, depende de Parser/AST): parse_infix (mysql.rs:97).

    // PENDENTE (Fase 2, depende de Parser/AST): parse_statement (mysql.rs:120).

    public override bool RequireIntervalQualifier => true;

    public override bool SupportsLimitComma => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table-select.html"/>
    /// </summary>
    public override bool SupportsCreateTableSelect => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/insert.html"/>
    /// </summary>
    public override bool SupportsInsertSet => true;

    public override bool SupportsUserHostGrantee => true;

    // PENDENTE (Fase 2, depende de Parser/AST): is_table_factor_alias (mysql.rs:152).

    public override bool SupportsTableHints => true;

    public override bool RequiresSingleLineCommentWhitespace => true;

    public override bool SupportsMatchAgainst => true;

    public override bool SupportsSelectModifiers => true;

    public override bool SupportsSetNames => true;

    public override bool SupportsCommaSeparatedSetAssignments => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/update.html"/>
    /// </summary>
    public override bool SupportsUpdateOrderBy => true;

    public override bool SupportsDataTypeSignedSuffix => true;

    public override bool SupportsCrossJoinConstraint => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/expressions.html"/>
    /// </summary>
    public override bool SupportsDoubleAmpersandOperator => true;

    /// <summary>
    /// Deprecated functionality by MySQL but still supported
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/cast-functions.html#operator_binary"/>
    /// </summary>
    public override bool SupportsBinaryKwAsCast => true;

    public override bool SupportsCommentOptimizerHint => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table.html"/>
    /// </summary>
    public override bool SupportsConstraintKeywordWithoutName => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table.html"/>
    /// </summary>
    public override bool SupportsKeyColumnOption => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/9.7/en/group-by-modifiers.html"/>
    /// </summary>
    public override bool SupportsGroupByWithModifier => true;

    public override bool SupportsLeftAssociativeJoinsWithoutParens => false;

    // PENDENTE (Fase 2, depende de Parser/AST): funções livres de mysql.rs: parse_lock_tables (mysql.rs:232),
    // parse_lock_table (mysql.rs:238), parse_lock_tables_type (mysql.rs:252) e parse_unlock_tables (mysql.rs:272).
}
