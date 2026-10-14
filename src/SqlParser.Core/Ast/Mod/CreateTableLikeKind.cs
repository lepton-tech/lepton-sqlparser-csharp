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
/// Specifies how to create a new table based on an existing table's schema.
/// <code>
/// CREATE TABLE new LIKE old ...
/// </code>
/// </summary>
/// <remarks>Sem <c>impl Display</c> no Rust: quem formata é o <c>Display</c> de <c>CreateTable</c>.</remarks>
public abstract partial record CreateTableLikeKind
{
    /// <summary>
    /// <code>
    /// CREATE TABLE new (LIKE old ...)
    /// </code>
    /// <see href="https://docs.aws.amazon.com/redshift/latest/dg/r_CREATE_TABLE_NEW.html">Redshift</see>
    /// </summary>
    public sealed record Parenthesized(CreateTableLike Value) : CreateTableLikeKind;

    /// <summary>
    /// <code>
    /// CREATE TABLE new LIKE old ...
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table#label-create-table-like">Snowflake</see>
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#create_table_like">BigQuery</see>
    /// </summary>
    public sealed record Plain(CreateTableLike Value) : CreateTableLikeKind;
}
