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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/redshift.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://aws.amazon.com/redshift/">RedShift</see>
/// </summary>
public class RedshiftSqlDialect : Dialect
{
    // In most cases the redshift dialect is identical to [`PostgresSqlDialect`].
    //
    // Notable differences:
    // 1. Redshift treats brackets `[` and `]` differently. For example, `SQL SELECT a[1][2] FROM b`
    // in the Postgres dialect, the query will be parsed as an array, while in the Redshift dialect it will
    // be a json path

    /// <summary>
    /// Determine if a character starts a potential nested quoted identifier.
    /// Example: RedShift supports the following quote styles to all mean the same thing:
    /// <code>
    /// SELECT 1 AS foo;
    /// SELECT 1 AS "foo";
    /// SELECT 1 AS [foo];
    /// SELECT 1 AS ["foo"];
    /// </code>
    /// </summary>
    public override bool IsNestedDelimitedIdentifierStart(char ch) => ch == '[';

    /// <summary>
    /// Only applicable whenever <see cref="IsNestedDelimitedIdentifierStart"/> returns true
    /// If the next sequence of tokens potentially represent a nested identifier, then this method
    /// returns a tuple containing the outer quote style, and if present, the inner (nested) quote style.
    /// <para>
    /// Example (Redshift):
    /// </para>
    /// <code>
    /// `["foo"]` => Some(`[`, Some(`"`))
    /// `[foo]` => Some(`[`, None)
    /// `[0]` => None
    /// `"foo"` => None
    /// </code>
    /// </summary>
    public override (char QuoteStart, char? NestedQuoteStart)? PeekNestedDelimitedIdentifierQuotes(PeekableChars chars)
    {
        if (chars.Peek() != '[')
        {
            return null;
        }

        chars.Next();

        var notWhiteChars = chars.SkipWhile(c => char.IsWhiteSpace(c));

        if (notWhiteChars.Peek() is { } ch)
        {
            if (ch == '"')
            {
                return ('[', '"');
            }
            if (IsIdentifierStart(ch))
            {
                return ('[', (char?)null);
            }
        }

        return null;
    }

    // Rust: `PostgreSqlDialect {}` (struct sem campos, custo zero) → instância estática, para não
    // alocar a cada caractere. Ver convencoes.md §13.
    private static readonly PostgreSqlDialect PostgreSqlDialectInstance = new PostgreSqlDialect();

    public override bool IsIdentifierStart(char ch) =>
        // UTF-8 multibyte characters are supported in identifiers via the PostgreSqlDialect.
        // https://docs.aws.amazon.com/redshift/latest/dg/r_names.html
        PostgreSqlDialectInstance.IsIdentifierStart(ch) || ch == '#';

    public override bool IsIdentifierPart(char ch) =>
        // UTF-8 multibyte characters are supported in identifiers via the PostgreSqlDialect.
        // https://docs.aws.amazon.com/redshift/latest/dg/r_names.html
        PostgreSqlDialectInstance.IsIdentifierPart(ch) || ch == '#';

    /// <summary>
    /// See <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_names.html"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '"';

    /// <summary>
    /// redshift has <c>CONVERT(type, value)</c> instead of <c>CONVERT(value, type)</c>
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CONVERT_function.html"/>
    /// </summary>
    public override bool ConvertTypeBeforeValue => true;

    public override bool SupportsConnectBy => true;

    /// <summary>
    /// Redshift expects the <c>TOP</c> option before the <c>ALL/DISTINCT</c> option:
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_SELECT_list.html#r_SELECT_list-parameters"/>
    /// </summary>
    public override bool SupportsTopBeforeDistinct => true;

    /// <summary>
    /// Redshift supports PartiQL: <see href="https://docs.aws.amazon.com/redshift/latest/dg/super-overview.html"/>
    /// </summary>
    public override bool SupportsPartiql => true;

    public override bool SupportsUnpivotExpr => true;

    public override bool SupportsStringEscapeConstant => true;

    /// <summary>
    /// See <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_APPROXIMATE_PERCENTILE_DISC.html"/>
    /// </summary>
    public override bool SupportsApproximatePercentileDisc => true;

    public override bool SupportsGeometricTypes => true;

    public override bool SupportsBitwiseShiftOperators => true;

    public override bool SupportsArrayTypedefWithBrackets => true;

    public override bool AllowExtractSingleQuotes => true;

    public override bool SupportsStringLiteralBackslashEscape => true;

    public override bool SupportsSelectWildcardExclude => true;

    /// <summary>
    /// Redshift supports aliasing wildcard expressions:
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_SELECT_list.html"/>
    /// </summary>
    public override bool SupportsSelectWildcardWithAlias => true;

    public override bool SupportsLeftAssociativeJoinsWithoutParens => false;

    public override bool SupportsSelectExclude => true;

    public override bool SupportsCreateTableLikeParenthesized => true;

    public override bool SupportsStringLiteralConcatenationWithNewline => true;

    public override bool SupportsWindowFunctionNullTreatmentArg => true;

    /// <summary>
    /// <c>INTERVAL</c> is listed as reserved in Redshift docs but the actual parser allows it as an
    /// identifier, consistent with pgsql.
    /// </summary>
    public override bool IsReservedForIdentifier(Keyword kw)
    {
        if (kw is Keyword.INTERVAL)
        {
            return false;
        }
        else
        {
            return Keywords.ReservedForIdentifier.Contains(kw);
        }
    }
}
