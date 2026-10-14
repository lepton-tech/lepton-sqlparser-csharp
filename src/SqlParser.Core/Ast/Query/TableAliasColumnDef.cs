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
/// SQL column definition in a table expression alias.
/// Most of the time, the data type is not specified.
/// But some table-valued functions do require specifying the data type.
/// </summary>
/// <remarks>
/// See <see href="https://www.postgresql.org/docs/17/queries-table-expressions.html#QUERIES-TABLEFUNCTIONS"/>
/// </remarks>
/// <param name="Name">Column name alias</param>
/// <param name="DataType">Some table-valued functions require specifying the data type in the alias.</param>
public sealed partial record TableAliasColumnDef(Ident Name, DataType? DataType) : IDisplay
{
    /// <summary>Create a new table alias column definition with only a name and no type</summary>
    public static TableAliasColumnDef FromName(string name) =>
        new TableAliasColumnDef(new Ident(name), null);

    public void Fmt(Formatter f)
    {
        f.Write($"{Name}");
        if (DataType is { } dataType)
        {
            f.Write($" {dataType}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
