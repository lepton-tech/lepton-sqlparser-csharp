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

namespace SqlParser.Core.Ast;

/// <summary>Function argument in CREATE OR DROP FUNCTION.</summary>
/// <param name="Mode">Optional argument mode (<c>IN</c>, <c>OUT</c>, <c>INOUT</c>).</param>
/// <param name="Name">Optional argument identifier/name.</param>
/// <param name="DataType">The data type of the argument.</param>
/// <param name="DefaultExpr">Optional default expression for the argument.</param>
public sealed record OperateFunctionArg(ArgMode? Mode, Ident? Name, DataType DataType, Expr? DefaultExpr) : IDisplay
{
    /// <summary>Returns an unnamed argument.</summary>
    public static OperateFunctionArg Unnamed(DataType dataType) =>
        new OperateFunctionArg(Mode: null, Name: null, DataType: dataType, DefaultExpr: null);

    /// <summary>Returns an argument with name.</summary>
    public static OperateFunctionArg WithName(string name, DataType dataType) =>
        new OperateFunctionArg(Mode: null, Name: Ident.From(name), DataType: dataType, DefaultExpr: null);

    public void Fmt(Formatter f)
    {
        if (Mode is { } mode)
        {
            f.Write($"{mode} ");
        }
        if (Name is { } name)
        {
            f.Write($"{name} ");
        }
        f.Write($"{DataType}");
        if (DefaultExpr is { } defaultExpr)
        {
            f.Write($" = {defaultExpr}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
