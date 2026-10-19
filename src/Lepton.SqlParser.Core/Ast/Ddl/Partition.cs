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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// PARTITION statement used in ALTER TABLE et al. such as in Hive and ClickHouse SQL.
/// For example, ClickHouse's OPTIMIZE TABLE supports syntax like PARTITION ID 'partition_id' and PARTITION expr.
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/optimize">ClickHouse</see>
/// </summary>
public abstract partial record Partition : IDisplay
{
    /// <summary>ClickHouse supports PARTITION ID 'partition_id' syntax.</summary>
    public sealed record Identifier(Ident Value) : Partition;

    /// <summary>ClickHouse supports PARTITION expr syntax.</summary>
    // A variante esconde o tipo de topo homônimo: tipo qualificado aqui e nas variantes seguintes
    // (convencoes.md §6 e §13, caso 38).
    public sealed record Expr(Ast.Expr Value) : Partition;

    /// <summary>
    /// ClickHouse supports PART expr which represents physical partition in disk.
    /// <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/partition#attach-partitionpart">ClickHouse</see>
    /// </summary>
    public sealed record Part(Ast.Expr Value) : Partition;

    /// <summary>Hive supports multiple partitions in PARTITION (part1, part2, ...) syntax.</summary>
    public sealed record Partitions(Sequence<Ast.Expr> Value) : Partition;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Identifier { Value: var id }:
                f.Write($"PARTITION ID {id}");
                break;
            case Expr { Value: var expr }:
                f.Write($"PARTITION {expr}");
                break;
            case Part { Value: var expr }:
                f.Write($"PART {expr}");
                break;
            case Partitions { Value: var partitions }:
                f.Write($"PARTITION ({DisplayCommaSeparated(partitions)})");
                break;
            default:
                throw new InvalidOperationException($"Variante de Partition não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
