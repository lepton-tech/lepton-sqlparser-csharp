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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/clickhouse.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://clickhouse.com/">ClickHouse</see>.
/// </summary>
public class ClickHouseDialect : Dialect
{
    public override bool IsIdentifierStart(char ch) =>
        // See https://clickhouse.com/docs/en/sql-reference/syntax/#syntax-identifiers
        (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || ch == '_';

    public override bool IsIdentifierPart(char ch) => IsIdentifierStart(ch) || (ch >= '0' && ch <= '9');

    /// <summary>
    /// ClickHouse accepts both backticks and double quotes, but its own
    /// formatter emits backticks by default.
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/syntax#identifiers"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '`';

    public override bool SupportsStringLiteralBackslashEscape => true;

    public override bool SupportsSelectWildcardExcept => true;

    public override bool DescribeRequiresTableKeyword => true;

    public override bool RequireIntervalQualifier => true;

    public override bool SupportsLimitComma => true;

    public override bool SupportsInsertTableFunction => true;

    public override bool SupportsInsertFormat => true;

    public override bool SupportsNumericLiteralUnderscores => true;

    public override bool SupportsPartitionByAfterOrderBy => true;

    public override bool SupportsArrayJoinSyntax => true;

    // ClickHouse uses this for some FORMAT expressions in `INSERT` context, e.g. when inserting
    // with FORMAT JSONEachRow a raw JSON key-value expression is valid and expected.
    //
    // [ClickHouse formats](https://clickhouse.com/docs/en/interfaces/formats)
    public override bool SupportsDictionarySyntax => true;

    // See <https://clickhouse.com/docs/sql-reference/operators/in>
    public override bool SupportsInUnparenthesizedExpr => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/functions#higher-order-functions---operator-and-lambdaparams-expr-function"/>
    /// </summary>
    public override bool SupportsLambdaFunctions => true;

    public override bool SupportsFromFirstSelect => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/order-by"/>
    /// </summary>
    public override bool SupportsOrderByAll => true;

    // See <https://clickhouse.com/docs/en/sql-reference/aggregate-functions/grouping_function#grouping-sets>
    public override bool SupportsGroupByExpr => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/group-by#rollup-modifier"/>
    /// </summary>
    public override bool SupportsGroupByWithModifier => true;

    /// <summary>
    /// Supported since 2020.
    /// See <see href="https://clickhouse.com/docs/whats-new/changelog/2020#backward-incompatible-change-2"/>
    /// </summary>
    public override bool SupportsNestedComments => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize"/>
    /// </summary>
    public override bool SupportsOptimizeTable => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/prewhere"/>
    /// </summary>
    public override bool SupportsPrewhere => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/order-by#order-by-expr-with-fill-modifier"/>
    /// </summary>
    public override bool SupportsWithFill => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/limit-by"/>
    /// </summary>
    public override bool SupportsLimitBy => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/order-by#order-by-expr-with-fill-modifier"/>
    /// </summary>
    public override bool SupportsInterpolate => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select#settings-in-select-query"/>
    /// </summary>
    public override bool SupportsSettings => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/format"/>
    /// </summary>
    public override bool SupportsSelectFormat => true;

    /// <summary>
    /// See <see href="https://clickhouse.com/docs/sql-reference/statements/select#replace"/>
    /// </summary>
    public override bool SupportsSelectWildcardReplace => true;

    public override bool SupportsCommaSeparatedTrim => true;
}
