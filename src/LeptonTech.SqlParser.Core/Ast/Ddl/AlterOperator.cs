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

/// <summary>
/// <c>ALTER OPERATOR</c> statement
/// See <see href="https://www.postgresql.org/docs/current/sql-alteroperator.html"/>
/// </summary>
/// <param name="Name">Operator name (can be schema-qualified)</param>
/// <param name="LeftType">Left operand type (<c>None</c> if no left operand)</param>
/// <param name="RightType">Right operand type</param>
/// <param name="Operation">The operation to perform</param>
public sealed partial record AlterOperator(
    ObjectName Name,
    DataType? LeftType,
    DataType RightType,
    AlterOperatorOperation Operation) : IDisplay
{
    // Rust: impl fmt::Display for AlterOperator (declarado depois de OperatorOption em ddl.rs).
    public void Fmt(Formatter f)
    {
        f.Write($"ALTER OPERATOR {Name} (");
        if (LeftType is { } leftType)
        {
            f.Write($"{leftType}");
        }
        else
        {
            f.Write($"NONE");
        }
        f.Write($", {RightType}) {Operation}");
    }

    public override string ToString() => Formatter.Format(this);
}
