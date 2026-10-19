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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>An item in an ALTER OPERATOR FAMILY DROP statement</summary>
public abstract partial record OperatorFamilyDropItem : IDisplay
{
    /// <summary><c>OPERATOR</c> clause for DROP within an operator family.</summary>
    /// <param name="StrategyNumber">Strategy number for the operator.</param>
    /// <param name="OpTypes">Operator argument types.</param>
    public sealed record Operator(ulong StrategyNumber, Sequence<DataType> OpTypes) : OperatorFamilyDropItem;

    /// <summary><c>FUNCTION</c> clause for DROP within an operator family.</summary>
    /// <param name="SupportNumber">Support function number.</param>
    /// <param name="OpTypes">Operator argument types for the function.</param>
    public sealed record Function(ulong SupportNumber, Sequence<DataType> OpTypes) : OperatorFamilyDropItem;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Operator { StrategyNumber: var strategyNumber, OpTypes: var opTypes }:
                f.Write($"OPERATOR {strategyNumber} ({DisplayCommaSeparated(opTypes)})");
                break;
            case Function { SupportNumber: var supportNumber, OpTypes: var opTypes }:
                f.Write($"FUNCTION {supportNumber} ({DisplayCommaSeparated(opTypes)})");
                break;
            default:
                throw new InvalidOperationException($"Variante de OperatorFamilyDropItem não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
