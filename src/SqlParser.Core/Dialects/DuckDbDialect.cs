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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/duckdb.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://duckdb.org/">DuckDB</see>
/// </summary>
public class DuckDbDialect : Dialect
{
    // In most cases the redshift dialect is identical to [`PostgresSqlDialect`].

    public override bool SupportsTrailingCommas => true;

    public override bool IsIdentifierStart(char ch) => char.IsLetter(ch) || ch == '_';

    public override bool IsIdentifierPart(char ch) => char.IsLetter(ch) || (ch >= '0' && ch <= '9') || ch == '$' || ch == '_';

    /// <summary>
    /// See <see href="https://duckdb.org/docs/stable/sql/dialect/keywords_and_identifiers"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '"';

    public override bool SupportsFilterDuringAggregation => true;

    public override bool SupportsGroupByExpr => true;

    public override bool SupportsBitwiseShiftOperators => true;

    public override bool SupportsNamedFnArgsWithEqOperator => true;

    public override bool SupportsNamedFnArgsWithAssignmentOperator => true;

    // DuckDB uses this syntax for `STRUCT`s.
    //
    // https://duckdb.org/docs/sql/data_types/struct.html#creating-structs
    public override bool SupportsDictionarySyntax => true;

    // DuckDB uses this syntax for `MAP`s.
    //
    // https://duckdb.org/docs/sql/data_types/map.html#creating-maps
    public override bool SupportMapLiteralSyntax => true;

    /// <summary>
    /// See <see href="https://duckdb.org/docs/stable/sql/functions/lambda"/>
    /// </summary>
    public override bool SupportsLambdaFunctions => true;

    /// <summary>
    /// Returns true if this dialect allows the <c>EXTRACT</c> function to use single quotes in the part being extracted.
    /// </summary>
    public override bool AllowExtractSingleQuotes => true;

    // DuckDB is compatible with PostgreSQL syntax for this statement,
    // although not all features may be implemented.
    public override bool SupportsExplainWithUtilityOptions => true;

    /// <summary>
    /// See DuckDB <see href="https://duckdb.org/docs/sql/statements/load_and_install.html#load"/>
    /// </summary>
    public override bool SupportsLoadExtension => true;

    // See DuckDB <https://duckdb.org/docs/sql/data_types/array.html#defining-an-array-field>
    public override bool SupportsArrayTypedefWithBrackets => true;

    public override bool SupportsFromFirstSelect => true;

    /// <summary>
    /// See DuckDB <see href="https://duckdb.org/docs/sql/query_syntax/orderby.html#order-by-all-examples"/>
    /// </summary>
    public override bool SupportsOrderByAll => true;

    public override bool SupportsSelectWildcardExclude => true;

    /// <summary>
    /// DuckDB supports <c>NOTNULL</c> as an alias for <c>IS NOT NULL</c>,
    /// see DuckDB Comparisons <see href="https://duckdb.org/docs/stable/sql/expressions/comparison_operators#between-and-is-not-null"/>
    /// </summary>
    public override bool SupportsNotnullOperator => true;

    /// <summary>
    /// See <see href="https://duckdb.org/docs/extensions/overview"/>
    /// </summary>
    public override bool SupportsInstall => true;

    /// <summary>
    /// See <see href="https://duckdb.org/docs/sql/statements/attach#detach-syntax"/>
    /// </summary>
    public override bool SupportsDetach => true;

    /// <summary>
    /// See <see href="https://duckdb.org/docs/sql/query_syntax/select#replace-clause"/>
    /// </summary>
    public override bool SupportsSelectWildcardReplace => true;

    public override bool SupportsCommaSeparatedTrim => true;

    public override bool SupportsNumericLiteralUnderscores => true;
}
