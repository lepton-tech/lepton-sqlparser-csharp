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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>An <c>ALTER SCHEMA</c> (<c>Statement::AlterSchema</c>) statement.</summary>
/// <param name="Name">The schema name to alter.</param>
/// <param name="IfExists">Whether <c>IF EXISTS</c> was specified.</param>
/// <param name="Operations">The list of operations to perform on the schema.</param>
public sealed partial record AlterSchema(ObjectName Name, bool IfExists, Sequence<AlterSchemaOperation> Operations) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"ALTER SCHEMA ");
        if (IfExists)
        {
            f.Write($"IF EXISTS ");
        }
        f.Write($"{Name}");
        foreach (var operation in Operations)
        {
            f.Write($" {operation}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
