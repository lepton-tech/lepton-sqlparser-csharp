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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// Table type for ALTER TABLE statements.
/// Used to distinguish between regular tables, Iceberg tables, and Dynamic tables.
/// </summary>
// Enum sem dados e sem nenhum `impl` no Rust → enum C# (convencoes.md §6, regra 1; §13, caso 37).
public enum AlterTableType
{
    /// <summary>
    /// Iceberg table type
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-iceberg-table"/>
    /// </summary>
    Iceberg,

    /// <summary>
    /// Dynamic table type
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-dynamic-table"/>
    /// </summary>
    Dynamic,

    /// <summary>
    /// External table type
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/alter-external-table"/>
    /// </summary>
    External,
}
