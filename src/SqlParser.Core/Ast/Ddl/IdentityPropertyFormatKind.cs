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

namespace SqlParser.Core.Ast;

/// <summary>
/// A format of parameters of identity column.
/// <para>
/// It is <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see> specific.
/// Syntax
/// </para>
/// <code>
/// (seed , increment) | START num INCREMENT num
/// </code>
/// <para>
/// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql-identity-property">MS SQL Server</see>
/// uses one way of representing these parameters.
/// Syntax
/// </para>
/// <code>
/// (seed , increment)
/// </code>
/// </summary>
public abstract record IdentityPropertyFormatKind : IDisplay
{
    /// <summary>
    /// A parameters of identity column declared like parameters of function call
    /// Example:
    /// <code>
    ///  (100, 1)
    /// </code>
    /// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql-identity-property">MS SQL Server</see>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </summary>
    public sealed record FunctionCall(IdentityParameters Value) : IdentityPropertyFormatKind;

    /// <summary>
    /// A parameters of identity column declared with keywords <c>START</c> and <c>INCREMENT</c>
    /// Example:
    /// <code>
    ///  START 100 INCREMENT 1
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </summary>
    public sealed record StartAndIncrement(IdentityParameters Value) : IdentityPropertyFormatKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case FunctionCall { Value: var parameters }:
                f.Write($"({parameters.Seed}, {parameters.Increment})");
                break;
            case StartAndIncrement { Value: var parameters }:
                f.Write($" START {parameters.Seed} INCREMENT {parameters.Increment}");
                break;
            default:
                throw new InvalidOperationException($"Variante de IdentityPropertyFormatKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
