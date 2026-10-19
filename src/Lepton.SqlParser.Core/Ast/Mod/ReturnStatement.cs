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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Represents a <c>Return</c> statement.</summary>
/// <remarks>
/// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-trigger-transact-sql">MsSql triggers</see>
/// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-function-transact-sql">MsSql functions</see>
/// </remarks>
/// <param name="Value">Optional return value expression.</param>
public sealed partial record ReturnStatement(ReturnStatementValue? Value) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (Value)
        {
            case ReturnStatementValue.Expr expr:
                f.Write($"RETURN {expr.Value}");
                break;
            case null:
                f.Write($"RETURN");
                break;
            default:
                throw new InvalidOperationException("Variante de ReturnStatementValue não tratada");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
