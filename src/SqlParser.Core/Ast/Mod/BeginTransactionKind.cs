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

/// <summary>Transaction started with [ TRANSACTION | WORK | TRAN ]</summary>
public abstract partial record BeginTransactionKind : IDisplay
{
    /// <summary>Standard <c>TRANSACTION</c> keyword.</summary>
    public sealed record Transaction : BeginTransactionKind;

    /// <summary>Alternate <c>WORK</c> keyword.</summary>
    public sealed record Work : BeginTransactionKind;

    /// <summary>
    /// MSSQL shorthand <c>TRAN</c> keyword.
    /// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/begin-transaction-transact-sql"/>
    /// </summary>
    public sealed record Tran : BeginTransactionKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Transaction:
                f.Write($"TRANSACTION");
                break;
            case Work:
                f.Write($"WORK");
                break;
            case Tran:
                f.Write($"TRAN");
                break;
            default:
                throw new InvalidOperationException($"Variante de BeginTransactionKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
