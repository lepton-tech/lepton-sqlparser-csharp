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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Modifier for the transaction in the <c>BEGIN</c> syntax
/// <para>SQLite: <see href="https://sqlite.org/lang_transaction.html"/></para>
/// <para>MS-SQL: <see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/try-catch-transact-sql"/></para>
/// </summary>
public abstract partial record TransactionModifier : IDisplay
{
    /// <summary>DEFERRED transaction modifier.</summary>
    public sealed record Deferred : TransactionModifier;

    /// <summary>IMMEDIATE transaction modifier.</summary>
    public sealed record Immediate : TransactionModifier;

    /// <summary>EXCLUSIVE transaction modifier.</summary>
    public sealed record Exclusive : TransactionModifier;

    /// <summary>TRY block modifier (MS-SQL style TRY/CATCH).</summary>
    public sealed record Try : TransactionModifier;

    /// <summary>CATCH block modifier (MS-SQL style TRY/CATCH).</summary>
    public sealed record Catch : TransactionModifier;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            Deferred => "DEFERRED",
            Immediate => "IMMEDIATE",
            Exclusive => "EXCLUSIVE",
            Try => "TRY",
            Catch => "CATCH",
            _ => throw new InvalidOperationException($"Variante de TransactionModifier não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
