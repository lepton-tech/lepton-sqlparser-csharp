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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>The operator used for joining two tables, e.g. <c>INNER</c>, <c>LEFT</c>, <c>CROSS</c>, <c>ASOF</c>, etc.</summary>
// Sem `Display` no Rust (o texto é gerado por `Join`): o tipo não implementa `IDisplay`.
public abstract partial record JoinOperator
{
    /// <summary>Generic <c>JOIN</c> with an optional constraint.</summary>
    public sealed record Join(JoinConstraint Value) : JoinOperator;

    /// <summary><c>INNER JOIN</c> with an optional constraint.</summary>
    public sealed record Inner(JoinConstraint Value) : JoinOperator;

    /// <summary><c>LEFT JOIN</c> with an optional constraint.</summary>
    public sealed record Left(JoinConstraint Value) : JoinOperator;

    /// <summary><c>LEFT OUTER JOIN</c> with an optional constraint.</summary>
    public sealed record LeftOuter(JoinConstraint Value) : JoinOperator;

    /// <summary><c>RIGHT JOIN</c> with an optional constraint.</summary>
    public sealed record Right(JoinConstraint Value) : JoinOperator;

    /// <summary><c>RIGHT OUTER JOIN</c> with an optional constraint.</summary>
    public sealed record RightOuter(JoinConstraint Value) : JoinOperator;

    /// <summary><c>FULL OUTER JOIN</c> with an optional constraint.</summary>
    public sealed record FullOuter(JoinConstraint Value) : JoinOperator;

    /// <summary><c>CROSS JOIN</c> (constraint usage is non-standard).</summary>
    public sealed record CrossJoin(JoinConstraint Value) : JoinOperator;

    /// <summary><c>SEMI JOIN</c> (non-standard)</summary>
    public sealed record Semi(JoinConstraint Value) : JoinOperator;

    /// <summary><c>LEFT SEMI JOIN</c> (non-standard)</summary>
    public sealed record LeftSemi(JoinConstraint Value) : JoinOperator;

    /// <summary><c>RIGHT SEMI JOIN</c> (non-standard)</summary>
    public sealed record RightSemi(JoinConstraint Value) : JoinOperator;

    /// <summary><c>ANTI JOIN</c> (non-standard)</summary>
    public sealed record Anti(JoinConstraint Value) : JoinOperator;

    /// <summary><c>LEFT ANTI JOIN</c> (non-standard)</summary>
    public sealed record LeftAnti(JoinConstraint Value) : JoinOperator;

    /// <summary><c>RIGHT ANTI JOIN</c> (non-standard)</summary>
    public sealed record RightAnti(JoinConstraint Value) : JoinOperator;

    /// <summary><c>CROSS APPLY</c> (non-standard)</summary>
    public sealed record CrossApply : JoinOperator;

    /// <summary><c>OUTER APPLY</c> (non-standard)</summary>
    public sealed record OuterApply : JoinOperator;

    /// <summary>
    /// <c>ASOF</c> joins are used for joining time-series tables whose timestamp columns do not match exactly.
    /// <para>See <see href="https://docs.snowflake.com/en/sql-reference/constructs/asof-join"/>.</para>
    /// </summary>
    /// <param name="MatchCondition">Condition used to match records in the <c>ASOF</c> join.</param>
    /// <param name="Constraint">Additional constraint applied to the <c>ASOF</c> join.</param>
    public sealed record AsOf(Expr MatchCondition, JoinConstraint Constraint) : JoinOperator;

    /// <summary>
    /// <c>STRAIGHT_JOIN</c> (MySQL non-standard behavior)
    /// <para>See <see href="https://dev.mysql.com/doc/refman/8.4/en/join.html"/>.</para>
    /// </summary>
    public sealed record StraightJoin(JoinConstraint Value) : JoinOperator;

    /// <summary>
    /// ClickHouse: <c>ARRAY JOIN</c> for unnesting arrays inline.
    /// <para>See <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/array-join"/>.</para>
    /// </summary>
    public sealed record ArrayJoin : JoinOperator;

    /// <summary>ClickHouse: <c>LEFT ARRAY JOIN</c> for unnesting arrays inline (preserves rows with empty arrays).</summary>
    public sealed record LeftArrayJoin : JoinOperator;

    /// <summary>ClickHouse: <c>INNER ARRAY JOIN</c> for unnesting arrays inline (filters rows with empty arrays).</summary>
    public sealed record InnerArrayJoin : JoinOperator;
}
