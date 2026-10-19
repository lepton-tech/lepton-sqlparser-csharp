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

/// <summary>SQL option syntax used in table and server definitions.</summary>
// As variantes aninhadas `Ident` e `NamedParenthesizedList` escondem os tipos de topo homônimos
// dentro deste record: os tipos de topo são qualificados como `Ast.X` (convencoes.md §13, caso 38).
public abstract partial record SqlOption : IDisplay
{
    /// <summary>
    /// Clustered represents the clustered version of table storage for MSSQL.
    /// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-azure-sql-data-warehouse?view=aps-pdw-2016-au7#TableOptions"/></para>
    /// </summary>
    public sealed record Clustered(TableOptionsClustered Value) : SqlOption;

    /// <summary>
    /// Single identifier options, e.g. <c>HEAP</c> for MSSQL.
    /// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-azure-sql-data-warehouse?view=aps-pdw-2016-au7#TableOptions"/></para>
    /// </summary>
    public sealed record Ident(Ast.Ident Value) : SqlOption;

    /// <summary>
    /// Any option that consists of a key value pair where the value is an expression. e.g.
    /// <code>
    ///   WITH(DISTRIBUTION = ROUND_ROBIN)
    /// </code>
    /// </summary>
    /// <param name="Key">The option key identifier.</param>
    /// <param name="Value">The expression value for the option.</param>
    public sealed record KeyValue(Ast.Ident Key, Expr Value) : SqlOption;

    /// <summary>
    /// One or more table partitions and represents which partition the boundary values belong to,
    /// e.g.
    /// <code>
    ///   PARTITION (id RANGE LEFT FOR VALUES (10, 20, 30, 40))
    /// </code>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-azure-sql-data-warehouse?view=aps-pdw-2016-au7#TablePartitionOptions"/>
    /// </summary>
    /// <param name="ColumnName">The partition column name.</param>
    /// <param name="RangeDirection">Optional direction for the partition range (LEFT/RIGHT).</param>
    /// <param name="ForValues">Values that define the partition boundaries.</param>
    public sealed record Partition(
        Ast.Ident ColumnName,
        PartitionRangeDirection? RangeDirection,
        Sequence<Expr> ForValues) : SqlOption;

    /// <summary>Comment parameter (supports <c>=</c> and no <c>=</c> syntax)</summary>
    public sealed record Comment(CommentDef Value) : SqlOption;

    /// <summary>
    /// MySQL TableSpace option
    /// <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table.html"/>
    /// </summary>
    public sealed record TableSpace(TablespaceOption Value) : SqlOption;

    /// <summary>
    /// An option representing a key value pair, where the value is a parenthesized list and with an optional name
    /// e.g.
    /// <code>
    ///   UNION  = (tbl_name[,tbl_name]...) &lt;https://dev.mysql.com/doc/refman/8.4/en/create-table.html&gt;
    ///   ENGINE = ReplicatedMergeTree('/table_name','{replica}', ver) &lt;https://clickhouse.com/docs/engines/table-engines/mergetree-family/replication&gt;
    ///   ENGINE = SummingMergeTree([columns]) &lt;https://clickhouse.com/docs/engines/table-engines/mergetree-family/summingmergetree&gt;
    /// </code>
    /// </summary>
    public sealed record NamedParenthesizedList(Ast.NamedParenthesizedList Value) : SqlOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Clustered c:
                f.Write($"{c.Value}");
                break;
            case Ident ident:
                f.Write($"{ident.Value}");
                break;
            case KeyValue keyValue:
                f.Write($"{keyValue.Key} = {keyValue.Value}");
                break;
            case Partition partition:
            {
                var direction = partition.RangeDirection switch
                {
                    PartitionRangeDirection.Left => " LEFT",
                    PartitionRangeDirection.Right => " RIGHT",
                    null => "",
                    _ => throw new InvalidOperationException(
                        $"Variante de PartitionRangeDirection não tratada: {partition.RangeDirection}"),
                };

                f.Write($"PARTITION ({partition.ColumnName} RANGE{direction} FOR VALUES ({DisplayCommaSeparated(partition.ForValues)}))");
                break;
            }
            case TableSpace tablespaceOption:
                f.Write($"TABLESPACE {tablespaceOption.Value.Name}");
                switch (tablespaceOption.Value.Storage)
                {
                    case StorageType.Disk:
                        f.Write($" STORAGE DISK");
                        break;
                    case StorageType.Memory:
                        f.Write($" STORAGE MEMORY");
                        break;
                    case null:
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Variante de StorageType não tratada: {tablespaceOption.Value.Storage}");
                }
                break;
            case Comment comment:
                switch (comment.Value)
                {
                    case CommentDef.WithEq(var withEq):
                        f.Write($"COMMENT = '{withEq}'");
                        break;
                    case CommentDef.WithoutEq(var withoutEq):
                        f.Write($"COMMENT '{withoutEq}'");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de CommentDef não tratada: {comment.Value.GetType().Name}");
                }
                break;
            case NamedParenthesizedList namedParenthesizedList:
            {
                var value = namedParenthesizedList.Value;
                f.Write($"{value.Key} = ");
                if (value.Name is { } key)
                {
                    f.Write($"{key}");
                }
                if (!value.Values.IsEmpty)
                {
                    f.Write($"({DisplayCommaSeparated(value.Values)})");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de SqlOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
