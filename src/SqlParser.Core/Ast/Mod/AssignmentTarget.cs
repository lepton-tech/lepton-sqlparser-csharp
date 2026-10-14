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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// Left-hand side of an assignment in an UPDATE statement,
/// e.g. <c>foo</c> in <c>foo = 5</c> (ColumnName assignment) or
/// <c>(a, b)</c> in <c>(a, b) = (1, 2)</c> (Tuple assignment).
/// </summary>
public abstract partial record AssignmentTarget : IDisplay
{
    /// <summary>A single column</summary>
    public sealed record ColumnName(ObjectName Value) : AssignmentTarget;

    /// <summary>A tuple of columns</summary>
    public sealed record Tuple(Sequence<ObjectName> Value) : AssignmentTarget;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case ColumnName column:
                f.Write($"{column.Value}");
                break;
            case Tuple columns:
                f.Write($"({DisplayCommaSeparated(columns.Value)})");
                break;
            default:
                throw new InvalidOperationException($"Variante de AssignmentTarget não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
