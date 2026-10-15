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

/// <summary>
/// A <see href="https://www.postgresql.org/docs/current/sql-select.html#SQL-TABLE"><c>TABLE</c> command</see>
/// </summary>
/// <param name="TableName">Optional table name (absent for e.g. <c>TABLE</c> command without argument).</param>
/// <param name="SchemaName">Optional schema/catalog name qualifying the table.</param>
public sealed partial record Table(Ident? TableName, Ident? SchemaName) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (TableName is { } tableName)
        {
            if (SchemaName is { } schemaName)
            {
                f.Write($"TABLE {schemaName}.{tableName}");
            }
            else
            {
                f.Write($"TABLE {tableName}");
            }
        }
        else
        {
            f.Write($"TABLE");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
