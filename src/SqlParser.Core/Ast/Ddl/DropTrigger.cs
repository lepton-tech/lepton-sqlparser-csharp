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
/// DROP TRIGGER
/// <code>
/// DROP TRIGGER [ IF EXISTS ] name ON table_name [ CASCADE | RESTRICT ]
/// </code>
/// </summary>
/// <param name="IfExists">Whether to include the <c>IF EXISTS</c> clause.</param>
/// <param name="TriggerName">The name of the trigger to be dropped.</param>
/// <param name="TableName">The name of the table from which the trigger is to be dropped.</param>
/// <param name="Option"><c>CASCADE</c> or <c>RESTRICT</c></param>
public sealed partial record DropTrigger(
    bool IfExists,
    ObjectName TriggerName,
    ObjectName? TableName,
    ReferentialAction? Option) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"DROP TRIGGER");
        if (IfExists)
        {
            f.Write($" IF EXISTS");
        }
        switch (TableName)
        {
            case { } tableName:
                f.Write($" {TriggerName} ON {tableName}");
                break;
            case null:
                f.Write($" {TriggerName}");
                break;
        }
        if (Option is { } option)
        {
            f.Write($" {option}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
