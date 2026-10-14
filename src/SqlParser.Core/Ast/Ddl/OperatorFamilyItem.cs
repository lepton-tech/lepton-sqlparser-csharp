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

/// <summary>An item in an ALTER OPERATOR FAMILY ADD statement</summary>
public abstract record OperatorFamilyItem : IDisplay
{
    /// <summary><c>OPERATOR</c> clause in an operator family modification.</summary>
    /// <param name="StrategyNumber">Strategy number for the operator.</param>
    /// <param name="OperatorName">Operator name referenced by this entry.</param>
    /// <param name="OpTypes">Operator argument types.</param>
    /// <param name="Purpose">Optional purpose such as <c>FOR SEARCH</c> or <c>FOR ORDER BY</c>.</param>
    public sealed record Operator(
        ulong StrategyNumber,
        ObjectName OperatorName,
        Sequence<DataType> OpTypes,
        OperatorPurpose? Purpose) : OperatorFamilyItem;

    /// <summary><c>FUNCTION</c> clause in an operator family modification.</summary>
    /// <param name="SupportNumber">Support function number.</param>
    /// <param name="OpTypes">Optional operator argument types for the function.</param>
    /// <param name="FunctionName">Function name for the support function.</param>
    /// <param name="ArgumentTypes">Function argument types.</param>
    public sealed record Function(
        ulong SupportNumber,
        Sequence<DataType>? OpTypes,
        ObjectName FunctionName,
        Sequence<DataType> ArgumentTypes) : OperatorFamilyItem;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Operator { StrategyNumber: var strategyNumber, OperatorName: var operatorName, OpTypes: var opTypes, Purpose: var purpose }:
            {
                f.Write($"OPERATOR {strategyNumber} {operatorName} ({DisplayCommaSeparated(opTypes)})");
                if (purpose is { } purposeValue)
                {
                    f.Write($" {purposeValue}");
                }
                break;
            }
            case Function { SupportNumber: var supportNumber, OpTypes: var opTypes, FunctionName: var functionName, ArgumentTypes: var argumentTypes }:
            {
                f.Write($"FUNCTION {supportNumber}");
                if (opTypes is { } types)
                {
                    f.Write($" ({DisplayCommaSeparated(types)})");
                }
                f.Write($" {functionName}");
                if (!argumentTypes.IsEmpty)
                {
                    f.Write($"({DisplayCommaSeparated(argumentTypes)})");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de OperatorFamilyItem não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
