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
/// An <c>MSCK</c> statement.
/// <code>
/// MSCK [REPAIR] TABLE table_name [ADD|DROP|SYNC PARTITIONS]
/// </code>
/// MSCK (Hive) - MetaStore Check command
/// </summary>
/// <param name="TableName">Table name to check</param>
/// <param name="Repair">Whether to repair the table</param>
/// <param name="PartitionAction">Partition action (ADD, DROP, or SYNC)</param>
public sealed record Msck(
    [property: VisitWith("visit_relation")] ObjectName TableName,
    bool Repair,
    AddDropSync? PartitionAction) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        var repair = Repair ? "REPAIR " : "";
        f.Write($"MSCK {repair}TABLE {TableName}");
        if (PartitionAction is { } pa)
        {
            f.Write($" {pa}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for Msck (ddl.rs:4366)
    public Span Span() => TableName.Span();
}
