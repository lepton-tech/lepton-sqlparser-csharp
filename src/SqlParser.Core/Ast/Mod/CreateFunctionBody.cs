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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/mod.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// Represent the expression body of a <c>CREATE FUNCTION</c> statement as well as
/// where within the statement, the body shows up.
/// </summary>
/// <remarks>
/// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#syntax_11">BigQuery</see>
/// <see href="https://www.postgresql.org/docs/15/sql-createfunction.html">PostgreSQL</see>
/// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql">MsSql</see>
/// </remarks>
// Sem `impl Display` no Rust: quem formata é o `Display` de `CreateFunction`.
public abstract partial record CreateFunctionBody
{
    /// <summary>
    /// A function body expression using the 'AS' keyword and shows up
    /// before any <c>OPTIONS</c> clause.
    /// <para>Example:</para>
    /// <code>
    /// CREATE FUNCTION myfunc(x FLOAT64, y FLOAT64) RETURNS FLOAT64
    /// AS (x * y)
    /// OPTIONS(description="desc");
    /// </code>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#syntax_11">BigQuery</see>
    /// <see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see>
    /// </summary>
    /// <param name="Body">The primary expression.</param>
    /// <param name="LinkSymbol">
    /// Link symbol if the primary expression contains the name of shared library file.
    /// <para>Example:</para>
    /// <code>
    /// CREATE FUNCTION cas_in(input cstring) RETURNS cas
    /// AS 'MODULE_PATHNAME', 'cas_in_wrapper'
    /// </code>
    /// <see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see>
    /// </param>
    public sealed record AsBeforeOptions(Expr Body, Expr? LinkSymbol) : CreateFunctionBody;

    /// <summary>
    /// A function body expression using the 'AS' keyword and shows up
    /// after any <c>OPTIONS</c> clause.
    /// <para>Example:</para>
    /// <code>
    /// CREATE FUNCTION myfunc(x FLOAT64, y FLOAT64) RETURNS FLOAT64
    /// OPTIONS(description="desc")
    /// AS (x * y);
    /// </code>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#syntax_11">BigQuery</see>
    /// </summary>
    public sealed record AsAfterOptions(Expr Value) : CreateFunctionBody;

    /// <summary>
    /// Function body with statements before the <c>RETURN</c> keyword.
    /// <para>Example:</para>
    /// <code>
    /// CREATE FUNCTION my_scalar_udf(a INT, b INT)
    /// RETURNS INT
    /// AS
    /// BEGIN
    ///     DECLARE c INT;
    ///     SET c = a + b;
    ///     RETURN c;
    /// END
    /// </code>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql">MsSql</see>
    /// </summary>
    public sealed record AsBeginEnd(BeginEndStatements Value) : CreateFunctionBody;

    /// <summary>
    /// Function body expression using the 'RETURN' keyword.
    /// <para>Example:</para>
    /// <code>
    /// CREATE FUNCTION myfunc(a INTEGER, IN b INTEGER = 1) RETURNS INTEGER
    /// LANGUAGE SQL
    /// RETURN a + b;
    /// </code>
    /// <see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see>
    /// </summary>
    public sealed record Return(Expr Value) : CreateFunctionBody;

    /// <summary>
    /// Function body expression using the 'AS RETURN' keywords
    /// <para>Example:</para>
    /// <code>
    /// CREATE FUNCTION myfunc(a INT, b INT)
    /// RETURNS TABLE
    /// AS RETURN (SELECT a + b AS sum);
    /// </code>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql">MsSql</see>
    /// </summary>
    public sealed record AsReturnExpr(Expr Value) : CreateFunctionBody;

    /// <summary>
    /// Function body expression using the 'AS RETURN' keywords, with an un-parenthesized SELECT query
    /// <para>Example:</para>
    /// <code>
    /// CREATE FUNCTION myfunc(a INT, b INT)
    /// RETURNS TABLE
    /// AS RETURN SELECT a + b AS sum;
    /// </code>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql?view=sql-server-ver16#select_stmt">MsSql</see>
    /// </summary>
    public sealed record AsReturnSelect(Select Value) : CreateFunctionBody;
}
