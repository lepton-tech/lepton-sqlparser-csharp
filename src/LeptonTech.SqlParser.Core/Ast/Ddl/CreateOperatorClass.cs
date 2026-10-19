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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// CREATE OPERATOR CLASS statement
/// See <see href="https://www.postgresql.org/docs/current/sql-createopclass.html"/>
/// </summary>
/// <param name="Name">Operator class name (can be schema-qualified)</param>
/// <param name="Default">Whether this is the default operator class for the type</param>
/// <param name="ForType">The data type</param>
/// <param name="Using">Index method (btree, hash, gist, gin, etc.)</param>
/// <param name="Family">Optional operator family name</param>
/// <param name="Items">List of operator class items (operators, functions, storage)</param>
public sealed partial record CreateOperatorClass(
    ObjectName Name,
    bool Default,
    DataType ForType,
    Ident Using,
    ObjectName? Family,
    Sequence<OperatorClassItem> Items) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CREATE OPERATOR CLASS {Name}");
        if (Default)
        {
            f.Write($" DEFAULT");
        }
        f.Write($" FOR TYPE {ForType} USING {Using}");
        if (Family is { } family)
        {
            f.Write($" FAMILY {family}");
        }
        f.Write($" AS {DisplayCommaSeparated(Items)}");
    }

    public override string ToString() => Formatter.Format(this);
}
