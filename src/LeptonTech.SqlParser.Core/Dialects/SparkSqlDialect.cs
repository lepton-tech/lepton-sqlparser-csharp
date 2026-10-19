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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/spark.rs.
// Modificado: tradução de Rust para C#.

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://spark.apache.org/docs/latest/sql-ref.html">Apache Spark SQL</see>.
/// <para>
/// See <see href="https://spark.apache.org/docs/latest/sql-ref-syntax.html"/>.
/// </para>
/// </summary>
public class SparkSqlDialect : Dialect
{
    // See https://spark.apache.org/docs/latest/sql-ref-identifier.html
    public override bool IsDelimitedIdentifierStart(char ch) => ch is '`';

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-identifier.html"/>
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '`';

    public override bool IsIdentifierStart(char ch) => ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';

    public override bool IsIdentifierPart(char ch) => ch is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-functions-builtin-agg.html"/>
    /// </summary>
    public override bool SupportsFilterDuringAggregation => true;

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-syntax-qry-select-groupby.html"/>
    /// </summary>
    public override bool SupportsGroupByExpr => true;

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-syntax-qry-select-groupby.html"/>
    /// </summary>
    public override bool SupportsGroupByWithModifier => true;

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-functions-builtin-higher-order-func.html"/>
    /// </summary>
    public override bool SupportsLambdaFunctions => true;

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-syntax-qry-select.html"/>
    /// </summary>
    public override bool SupportsSelectWildcardExcept => true;

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-datatypes.html"/>
    /// </summary>
    public override bool SupportsStructLiteral => true;

    public override bool SupportsNestedComments => true;

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-syntax-ddl-create-table-datasource.html"/>
    /// </summary>
    public override bool SupportsCreateTableUsing => true;

    /// <summary>
    /// <c>LONG</c> is an alias for <c>BIGINT</c> in Spark SQL.
    /// <para>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-datatypes.html"/>
    /// </para>
    /// </summary>
    public override bool SupportsLongTypeAsBigint => true;

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-syntax-qry-select.html"/>
    /// </summary>
    public override bool SupportsValuesAsTableFactor => true;

    public override bool RequireIntervalQualifier => true;

    public override bool SupportsBangNotOperator => true;

    public override bool SupportsSelectItemMultiColumnAlias => true;

    public override bool SupportsCteWithoutAs => true;

    /// <summary>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-datatypes.html"/>
    /// </summary>
    public override bool SupportsMapLiteralWithAngleBrackets => true;

    /// <summary>
    /// See:
    /// - <see href="https://spark.apache.org/docs/latest/sql-pipe-syntax.html"/>
    /// - <see href="https://issues.apache.org/jira/browse/SPARK-49528"/>
    /// </summary>
    public override bool SupportsPipeOperator => true;

    /// <summary>Parse the <c>DIV</c> keyword as integer division.</summary>
    /// <remarks>
    /// Example: <c>SELECT 10 DIV 3</c> returns <c>3</c>.
    /// <para>
    /// See <see href="https://spark.apache.org/docs/latest/sql-ref-functions-builtin-math.html"/>
    /// </para>
    /// </remarks>
    public override Expr? ParseInfix(Parser parser, Expr expr, byte precedence)
    {
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
}
