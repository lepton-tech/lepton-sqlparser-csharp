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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>ALTER TABLE statement</summary>
/// <param name="Name">Table name</param>
/// <param name="IfExists">Whether <c>IF EXISTS</c> was specified for the <c>ALTER TABLE</c>.</param>
/// <param name="Only">Whether the <c>ONLY</c> keyword was used (restrict scope to the named table).</param>
/// <param name="Operations">List of <c>ALTER TABLE</c> operations to apply.</param>
/// <param name="Location">Optional Hive <c>SET LOCATION</c> clause for the alter operation.</param>
/// <param name="OnCluster">
/// ClickHouse dialect supports <c>ON CLUSTER</c> clause for ALTER TABLE
/// For example: <c>ALTER TABLE table_name ON CLUSTER cluster_name ADD COLUMN c UInt32</c>
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/alter/update">ClickHouse</see>
/// </param>
/// <param name="TableType">Table type: None for regular tables, Some(AlterTableType) for Iceberg or Dynamic tables</param>
/// <param name="EndToken">Token that represents the end of the statement (semicolon or EOF)</param>
public sealed partial record AlterTable(
    [property: VisitWith("visit_relation")] ObjectName Name,
    bool IfExists,
    bool Only,
    Sequence<AlterTableOperation> Operations,
    HiveSetLocation? Location,
    Ident? OnCluster,
    AlterTableType? TableType,
    AttachedToken EndToken) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (TableType)
        {
            case AlterTableType.Iceberg:
                f.Write($"ALTER ICEBERG TABLE ");
                break;
            case AlterTableType.Dynamic:
                f.Write($"ALTER DYNAMIC TABLE ");
                break;
            case AlterTableType.External:
                f.Write($"ALTER EXTERNAL TABLE ");
                break;
            case null:
                f.Write($"ALTER TABLE ");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterTableType não tratada: {TableType}");
        }

        if (IfExists)
        {
            f.Write($"IF EXISTS ");
        }
        if (Only)
        {
            f.Write($"ONLY ");
        }
        f.Write($"{Name} ");
        if (OnCluster is { } cluster)
        {
            f.Write($"ON CLUSTER {cluster} ");
        }
        f.Write($"{DisplayCommaSeparated(Operations)}");
        if (Location is { } loc)
        {
            f.Write($" {loc}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
