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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>An item in a CREATE OPERATOR CLASS statement</summary>
public abstract partial record OperatorClassItem : IDisplay
{
    /// <summary><c>OPERATOR</c> clause describing a specific operator implementation.</summary>
    /// <param name="StrategyNumber">Strategy number identifying the operator position in the opclass.</param>
    /// <param name="OperatorName">The operator name referenced by this clause.</param>
    /// <param name="OpTypes">Optional operator argument types.</param>
    /// <param name="Purpose">Optional purpose such as <c>FOR SEARCH</c> or <c>FOR ORDER BY</c>.</param>
    public sealed record Operator(
        ulong StrategyNumber,
        ObjectName OperatorName,
        OperatorArgTypes? OpTypes,
        OperatorPurpose? Purpose) : OperatorClassItem;

    /// <summary><c>FUNCTION</c> clause describing a support function for the operator class.</summary>
    /// <param name="SupportNumber">Support function number for this entry.</param>
    /// <param name="OpTypes">Optional function argument types for the operator class.</param>
    /// <param name="FunctionName">The function name implementing the support function.</param>
    /// <param name="ArgumentTypes">Function argument types for the support function.</param>
    public sealed record Function(
        ulong SupportNumber,
        Sequence<DataType>? OpTypes,
        ObjectName FunctionName,
        Sequence<DataType> ArgumentTypes) : OperatorClassItem;

    /// <summary><c>STORAGE</c> clause specifying the storage type.</summary>
    /// <param name="StorageType">The storage data type.</param>
    public sealed record Storage(DataType StorageType) : OperatorClassItem;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Operator { StrategyNumber: var strategyNumber, OperatorName: var operatorName, OpTypes: var opTypes, Purpose: var purpose }:
            {
                f.Write($"OPERATOR {strategyNumber} {operatorName}");
                if (opTypes is { } types)
                {
                    f.Write($" ({types})");
                }
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
            case Storage { StorageType: var storageType }:
            {
                f.Write($"STORAGE {storageType}");
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de OperatorClassItem não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
