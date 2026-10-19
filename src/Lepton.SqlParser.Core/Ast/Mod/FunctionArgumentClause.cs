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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Clauses that can appear inside a function argument list.</summary>
public abstract partial record FunctionArgumentClause : IDisplay
{
    /// <summary>
    /// Indicates how <c>NULL</c>s should be handled in the calculation, e.g. in <c>FIRST_VALUE</c> on
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/navigation_functions#first_value">BigQuery</see>.
    /// <para>Syntax:</para>
    /// <code>
    /// { IGNORE | RESPECT } NULLS ]
    /// </code>
    /// </summary>
    public sealed record IgnoreOrRespectNulls(NullTreatment Value) : FunctionArgumentClause;

    /// <summary>
    /// The inline <c>WHERE</c> filter clause on an aggregate call, e.g.
    /// <c>COUNT(* WHERE cond)</c> / <c>SUM(x WHERE cond)</c> / <c>ARRAY_AGG(x WHERE cond ORDER BY ..)</c>.
    /// Popularized by <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/aggregate_functions#grouping_and_filtering">GoogleSQL</see>;
    /// equivalent to the standard <c>AGG(x) FILTER (WHERE cond)</c>.
    /// Accepted for all dialects since <c>WHERE</c> cannot otherwise begin a function argument.
    /// </summary>
    public sealed record Where(Expr Value) : FunctionArgumentClause;

    /// <summary>
    /// Specifies the the ordering for some ordered set aggregates, e.g. <c>ARRAY_AGG</c> on
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/aggregate_functions#array_agg">BigQuery</see>.
    /// </summary>
    public sealed record OrderBy(Sequence<OrderByExpr> Value) : FunctionArgumentClause;

    /// <summary>Specifies a limit for the <c>ARRAY_AGG</c> and <c>ARRAY_CONCAT_AGG</c> functions on BigQuery.</summary>
    public sealed record Limit(Expr Value) : FunctionArgumentClause;

    /// <summary>
    /// Specifies the behavior on overflow of the <c>LISTAGG</c> function.
    /// <para>See <see href="https://trino.io/docs/current/functions/aggregate.html"/>.</para>
    /// </summary>
    public sealed record OnOverflow(ListAggOnOverflow Value) : FunctionArgumentClause;

    /// <summary>
    /// Specifies a minimum or maximum bound on the input to
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/aggregate_functions#any_value"><c>ANY_VALUE</c></see> on BigQuery.
    /// <para>Syntax:</para>
    /// <code>
    /// HAVING { MAX | MIN } expression
    /// </code>
    /// </summary>
    public sealed record Having(HavingBound Value) : FunctionArgumentClause;

    /// <summary>
    /// The <c>SEPARATOR</c> clause to the
    /// <see href="https://dev.mysql.com/doc/refman/8.0/en/aggregate-functions.html#function_group-concat"><c>GROUP_CONCAT</c></see> function in MySQL.
    /// </summary>
    public sealed record Separator(ValueWithSpan Value) : FunctionArgumentClause;

    /// <summary>
    /// The <c>ON NULL</c> clause for some JSON functions.
    /// <para>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/json-array-transact-sql?view=sql-server-ver16">MSSQL <c>JSON_ARRAY</c></see>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/json-object-transact-sql?view=sql-server-ver16&gt;">MSSQL <c>JSON_OBJECT</c></see>
    /// <see href="https://www.postgresql.org/docs/current/functions-json.html#FUNCTIONS-JSON-PROCESSING">PostgreSQL JSON functions</see>
    /// </para>
    /// </summary>
    public sealed record JsonNullClause(Ast.JsonNullClause Value) : FunctionArgumentClause;

    /// <summary>
    /// The <c>RETURNING</c> clause for some JSON functions in PostgreSQL
    /// <para><see href="https://www.postgresql.org/docs/current/functions-json.html#:~:text=json_object"><c>JSON_OBJECT</c></see></para>
    /// </summary>
    public sealed record JsonReturningClause(Ast.JsonReturningClause Value) : FunctionArgumentClause;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case IgnoreOrRespectNulls nullTreatment:
                f.Write($"{nullTreatment.Value}");
                break;
            case Where expr:
                f.Write($"WHERE {expr.Value}");
                break;
            case OrderBy orderBy:
                f.Write($"ORDER BY {DisplayCommaSeparated(orderBy.Value)}");
                break;
            case Limit limit:
                f.Write($"LIMIT {limit.Value}");
                break;
            case OnOverflow onOverflow:
                f.Write($"{onOverflow.Value}");
                break;
            case Having bound:
                f.Write($"{bound.Value}");
                break;
            case Separator sep:
                f.Write($"SEPARATOR {sep.Value}");
                break;
            case JsonNullClause nullClause:
                f.Write($"{nullClause.Value}");
                break;
            case JsonReturningClause returningClause:
                f.Write($"{returningClause.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionArgumentClause não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
