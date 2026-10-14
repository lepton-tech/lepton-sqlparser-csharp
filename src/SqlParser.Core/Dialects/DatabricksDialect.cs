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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/databricks.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://www.databricks.com/">Databricks SQL</see>
/// <para>
/// See <see href="https://docs.databricks.com/en/sql/language-manual/index.html"/>.
/// </para>
/// </summary>
public class DatabricksDialect : Dialect
{
    // see https://docs.databricks.com/en/sql/language-manual/sql-ref-identifiers.html
    public override bool IsDelimitedIdentifierStart(char ch) => ch is '`';

    /// <summary>
    /// See <see href="https://docs.databricks.com/en/sql/language-manual/sql-ref-identifiers.html"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '`';

    public override bool IsIdentifierStart(char ch) => ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';

    public override bool IsIdentifierPart(char ch) => ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';

    public override bool SupportsNumericPrefix => true;

    public override bool SupportsFilterDuringAggregation => true;

    // https://docs.databricks.com/en/sql/language-manual/sql-ref-syntax-qry-select-groupby.html
    public override bool SupportsGroupByExpr => true;

    /// <summary>
    /// <see href="https://docs.databricks.com/gcp/en/delta/history#delta-time-travel-syntax"/>
    /// </summary>
    public override bool SupportsTableVersioning => true;

    public override bool SupportsLambdaFunctions => true;

    // https://docs.databricks.com/en/sql/language-manual/sql-ref-syntax-qry-select.html#syntax
    public override bool SupportsSelectWildcardExcept => true;

    public override bool RequireIntervalQualifier => true;

    // See https://docs.databricks.com/en/sql/language-manual/functions/struct.html
    public override bool SupportsStructLiteral => true;

    /// <summary>
    /// See <see href="https://docs.databricks.com/aws/en/sql/language-manual/sql-ref-syntax-comment"/>
    /// </summary>
    public override bool SupportsNestedComments => true;

    /// <summary>
    /// See <see href="https://docs.databricks.com/en/sql/language-manual/sql-ref-syntax-qry-select-groupby.html"/>
    /// </summary>
    public override bool SupportsGroupByWithModifier => true;

    /// <summary>
    /// See <see href="https://docs.databricks.com/en/sql/language-manual/sql-ref-syntax-qry-select-values.html"/>
    /// </summary>
    public override bool SupportsValuesAsTableFactor => true;

    /// <summary>
    /// See <see href="https://docs.databricks.com/en/sql/language-manual/delta-optimize.html"/>
    /// </summary>
    public override bool SupportsOptimizeTable => true;

    /// <summary>
    /// See <see href="https://docs.databricks.com/aws/en/sql/language-manual/functions/bangsign"/>
    /// </summary>
    public override bool SupportsBangNotOperator => true;

    /// <summary>
    /// See <see href="https://docs.databricks.com/aws/en/sql/language-manual/sql-ref-syntax-qry-select-cte"/>
    /// </summary>
    public override bool SupportsCteWithoutAs => true;

    public override bool SupportsSelectItemMultiColumnAlias => true;

    public override bool SupportsMapLiteralWithAngleBrackets => true;

    public override bool SupportsStringLiteralBackslashEscape => true;

    public override bool SupportsSelectWildcardReplace => true;

    public override bool SupportsFromFirstSelect => true;

    public override bool SupportsPipeOperator => true;
}
