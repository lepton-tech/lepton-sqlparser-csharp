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

namespace SqlParser.Core.Ast;

/// <summary>The kind of <c>ORDER BY</c> clause: either <c>ALL</c> with modifiers or a list of expressions.</summary>
/// <remarks>Sem <c>Display</c> no Rust (o texto é gerado por <c>OrderBy</c>): o tipo não implementa <c>IDisplay</c>.</remarks>
public abstract record OrderByKind
{
    /// <summary>
    /// <c>GROUP BY ALL</c>/<c>ORDER BY ALL</c> syntax with optional modifiers.
    /// <para>
    /// [DuckDB]:  <see href="https://duckdb.org/docs/sql/query_syntax/orderby"/>
    /// [ClickHouse]: <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/order-by"/>
    /// </para>
    /// </summary>
    public sealed record All(OrderByOptions Value) : OrderByKind;

    /// <summary>A standard list of ordering expressions.</summary>
    public sealed record Expressions(Sequence<OrderByExpr> Value) : OrderByKind;
}
