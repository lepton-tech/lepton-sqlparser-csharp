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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// MySQL-specific SELECT modifiers that appear after the SELECT keyword.
/// </summary>
/// <remarks>
/// These modifiers affect query execution and optimization. They can appear in any order after
/// SELECT and before the column list, can be repeated, and can be interleaved with
/// DISTINCT/DISTINCTROW/ALL:
/// <code>
/// SELECT
///     [ALL | DISTINCT | DISTINCTROW]
///     [HIGH_PRIORITY]
///     [STRAIGHT_JOIN]
///     [SQL_SMALL_RESULT] [SQL_BIG_RESULT] [SQL_BUFFER_RESULT]
///     [SQL_NO_CACHE] [SQL_CALC_FOUND_ROWS]
///     select_expr [, select_expr] ...
/// </code>
/// See <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html">MySQL SELECT</see>.
/// </remarks>
/// <param name="HighPriority">
/// <c>HIGH_PRIORITY</c> gives the SELECT higher priority than statements that update a table.
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html"/>
/// </param>
/// <param name="StraightJoin">
/// <c>STRAIGHT_JOIN</c> forces the optimizer to join tables in the order listed in the FROM clause.
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html"/>
/// </param>
/// <param name="SqlSmallResult">
/// <c>SQL_SMALL_RESULT</c> hints that the result set is small, using in-memory temp tables.
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html"/>
/// </param>
/// <param name="SqlBigResult">
/// <c>SQL_BIG_RESULT</c> hints that the result set is large, using disk-based temp tables.
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html"/>
/// </param>
/// <param name="SqlBufferResult">
/// <c>SQL_BUFFER_RESULT</c> forces the result to be put into a temporary table to release locks early.
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html"/>
/// </param>
/// <param name="SqlNoCache">
/// <c>SQL_NO_CACHE</c> tells MySQL not to cache the query result. (Deprecated in 8.4+.)
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html"/>
/// </param>
/// <param name="SqlCalcFoundRows">
/// <c>SQL_CALC_FOUND_ROWS</c> tells MySQL to calculate the total number of rows. (Deprecated in 8.0.17+.)
/// - <see href="https://dev.mysql.com/doc/refman/8.4/en/select.html">MySQL SELECT modifiers</see>
/// - <see href="https://dev.mysql.com/doc/refman/8.4/en/information-functions.html#function_found-rows"><c>FOUND_ROWS()</c></see>
/// </param>
public sealed partial record SelectModifiers(
    bool HighPriority,
    bool StraightJoin,
    bool SqlSmallResult,
    bool SqlBigResult,
    bool SqlBufferResult,
    bool SqlNoCache,
    bool SqlCalcFoundRows) : IDisplay
{
    // Rust: `#[derive(Default)]` (todos os campos `false`; convencoes.md §13, caso 42).
    public static SelectModifiers Default() =>
        new SelectModifiers(
            HighPriority: false,
            StraightJoin: false,
            SqlSmallResult: false,
            SqlBigResult: false,
            SqlBufferResult: false,
            SqlNoCache: false,
            SqlCalcFoundRows: false);

    public void Fmt(Formatter f)
    {
        if (HighPriority)
        {
            f.WriteStr(" HIGH_PRIORITY");
        }
        if (StraightJoin)
        {
            f.WriteStr(" STRAIGHT_JOIN");
        }
        if (SqlSmallResult)
        {
            f.WriteStr(" SQL_SMALL_RESULT");
        }
        if (SqlBigResult)
        {
            f.WriteStr(" SQL_BIG_RESULT");
        }
        if (SqlBufferResult)
        {
            f.WriteStr(" SQL_BUFFER_RESULT");
        }
        if (SqlNoCache)
        {
            f.WriteStr(" SQL_NO_CACHE");
        }
        if (SqlCalcFoundRows)
        {
            f.WriteStr(" SQL_CALC_FOUND_ROWS");
        }
    }

    public override string ToString() => Formatter.Format(this);

    /// <summary>Returns true if any of the modifiers are set.</summary>
    public bool IsAnySet()
    {
        // Using irrefutable destructuring to catch fields added in the future
        // (no C#, a desestruturação posicional também falha na compilação se um campo for acrescentado)
        var (highPriority, straightJoin, sqlSmallResult, sqlBigResult, sqlBufferResult, sqlNoCache, sqlCalcFoundRows) = this;
        return highPriority
            || straightJoin
            || sqlSmallResult
            || sqlBigResult
            || sqlBufferResult
            || sqlNoCache
            || sqlCalcFoundRows;
    }
}
