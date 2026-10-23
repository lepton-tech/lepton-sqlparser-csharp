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

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.mysql.com/">MySQL</see>
/// </summary>
public class MySqlDialect : Dialect
{
    // Constante de módulo no Rust (antes do struct); usada só por is_table_factor_alias.
    private static readonly Keyword[] ReservedForTableAliasMysql =
    {
        Keyword.USE,
        Keyword.IGNORE,
        Keyword.FORCE,
        Keyword.STRAIGHT_JOIN,
    };

    /// <summary>
    /// Only select-list aliases may use string quoting, see
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/problems-with-alias.html"/>
    /// </summary>
    public override bool SupportsStringLiteralTableAlias(bool @explicit) => false;

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

    public override bool SupportsLiteralPrefixInQualifiedName => true;

    public override bool SupportsBitwiseShiftOperators => true;

    /// <summary>
    /// see <see href="https://dev.mysql.com/doc/refman/8.4/en/comments.html"/>
    /// </summary>
    public override bool SupportsMultilineCommentHints => true;

    public override Expr? ParseInfix(Parser parser, Expr expr, byte precedence)
    {
        // Parse DIV as an operator
        if (parser.ParseKeyword(Keyword.DIV))
        {
            var left = expr;
            // Rust: `match parser.parse_subexpr(precedence) { Ok(expr) => expr, Err(e) => return Some(Err(e)) }`:
            // o erro propaga como exceção (convencoes.md §13, caso 53).
            var right = parser.ParseSubexpr(precedence);
            return new Expr.BinaryOp(
                Left: left,
                Op: new BinaryOperator.MyIntegerDivide(),
                Right: right);
        }
        else
        {
            return null;
        }
    }

    public override Statement? ParseStatement(Parser parser)
    {
        if (parser.ParseKeywords(Keyword.LOCK, Keyword.TABLES))
        {
            return ParseLockTables(parser);
        }
        else if (parser.ParseKeywords(Keyword.UNLOCK, Keyword.TABLES))
        {
            return ParseUnlockTables(parser);
        }
        else
        {
            return null;
        }
    }

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

    public override bool IsTableFactorAlias(bool @explicit, Keyword kw, Parser parser) =>
        @explicit
            || (!Keywords.ReservedForTableAlias.Contains(kw)
                && !ReservedForTableAliasMysql.Contains(kw));

    public override bool SupportsTableHints => true;

    public override bool RequiresSingleLineCommentWhitespace => true;

    public override bool SupportsHashComment => true;

    public override bool SupportsMatchAgainst => true;

    public override bool SupportsSelectModifiers => true;

    public override bool SupportsSetNames => true;

    public override bool SupportsCommaSeparatedSetAssignments => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html"/>
    /// </summary>
    public override bool SupportsAlterColumnPosition => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/update.html"/>
    /// </summary>
    public override bool SupportsUpdateOrderBy => true;

    public override bool SupportsDataTypeSignedSuffix => true;

    public override bool SupportsDataTypeZerofillSuffix => true;

    public override bool SupportsCrossJoinConstraint => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/expressions.html"/>
    /// </summary>
    public override bool SupportsDoubleAmpersandOperator => true;

    /// <summary>
    /// <c>&amp;&amp;</c> binds like <c>AND</c>.
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/operator-precedence.html"/>
    /// </summary>
    public override byte? GetNextPrecedence(Parser parser)
    {
        switch (parser.PeekTokenRef().Token)
        {
            case Token.Overlap:
                return PrecValue(Precedence.And);
            default:
                return null;
        }
    }

    /// <summary>
    /// Deprecated functionality by MySQL but still supported
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/cast-functions.html#operator_binary"/>
    /// </summary>
    public override bool SupportsBinaryKwAsCast => true;

    /// <summary>
    /// See: <see href="https://dev.mysql.com/doc/refman/8.4/en/partitioning-selection.html"/>
    /// </summary>
    public override bool SupportsTablePartitions => true;

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

    // Rust: funções livres de mysql.rs (depois do `impl Dialect`) → membros `static` do tipo do arquivo
    // (convencoes.md §13, caso 5); `fn` privada → `internal` (casos 20 e 52).

    /// <summary>
    /// <c>LOCK TABLES</c>
    /// <see href="https://dev.mysql.com/doc/refman/8.0/en/lock-tables.html"/>
    /// </summary>
    internal static Statement ParseLockTables(Parser parser)
    {
        var tables = parser.ParseCommaSeparated<LockTable>(ParseLockTable);
        return new Statement.LockTables(Tables: tables);
    }

    // tbl_name [[AS] alias] lock_type
    internal static LockTable ParseLockTable(Parser parser)
    {
        var table = parser.ParseIdentifier();
        var alias =
            parser.ParseOptionalAlias(Keyword.READ, Keyword.WRITE, Keyword.LOW_PRIORITY);
        var lockType = ParseLockTablesType(parser);

        return new LockTable(
            Table: table,
            Alias: alias,
            LockType: lockType);
    }

    // READ [LOCAL] | [LOW_PRIORITY] WRITE
    internal static LockTableType ParseLockTablesType(Parser parser)
    {
        if (parser.ParseKeyword(Keyword.READ))
        {
            if (parser.ParseKeyword(Keyword.LOCAL))
            {
                return new LockTableType.Read(Local: true);
            }
            else
            {
                return new LockTableType.Read(Local: false);
            }
        }
        else if (parser.ParseKeyword(Keyword.WRITE))
        {
            return new LockTableType.Write(
                LowPriority: false);
        }
        else if (parser.ParseKeywords(Keyword.LOW_PRIORITY, Keyword.WRITE))
        {
            return new LockTableType.Write(LowPriority: true);
        }
        else
        {
            return parser.ExpectedRef<LockTableType>("an lock type in LOCK TABLES", parser.PeekTokenRef());
        }
    }

    /// <summary>
    /// UNLOCK TABLES
    /// <see href="https://dev.mysql.com/doc/refman/8.0/en/lock-tables.html"/>
    /// </summary>
    internal static Statement ParseUnlockTables(Parser _parser) => new Statement.UnlockTables();
}
