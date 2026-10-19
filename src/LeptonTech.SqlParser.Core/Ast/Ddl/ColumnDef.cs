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

/// <summary>SQL column definition</summary>
/// <param name="Name">Column name.</param>
/// <param name="DataType">Column data type.</param>
/// <param name="Options">Column options (defaults, constraints, generated, etc.).</param>
public sealed partial record ColumnDef(Ident Name, DataType DataType, Sequence<ColumnOptionDef> Options) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // Rust: `self.data_type == DataType::Unspecified` (variante unitária: o teste de tipo equivale à igualdade).
        if (DataType is Ast.DataType.Unspecified)
        {
            f.Write($"{Name}");
        }
        else
        {
            f.Write($"{Name} {DataType}");
        }
        foreach (var option in Options)
        {
            f.Write($" {option}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
