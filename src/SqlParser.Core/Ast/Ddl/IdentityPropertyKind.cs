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
/// Identity is a column option for defining an identity or autoincrement column in a <c>CREATE TABLE</c> statement.
/// <para>Syntax</para>
/// <code>
/// { IDENTITY | AUTOINCREMENT } [ (seed , increment) | START num INCREMENT num ] [ ORDER | NOORDER ]
/// </code>
/// <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql-identity-property">MS SQL Server</see>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
/// </summary>
public abstract partial record IdentityPropertyKind : IDisplay
{
    /// <summary>
    /// An identity property declared via the <c>AUTOINCREMENT</c> key word
    /// Example:
    /// <code>
    ///  AUTOINCREMENT(100, 1) NOORDER
    ///  AUTOINCREMENT START 100 INCREMENT 1 ORDER
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// </summary>
    public sealed record Autoincrement(IdentityProperty Value) : IdentityPropertyKind;

    /// <summary>
    /// An identity property declared via the <c>IDENTITY</c> key word
    /// Example, <see href="https://learn.microsoft.com/en-us/sql/t-sql/statements/create-table-transact-sql-identity-property">MS SQL Server</see>
    /// or <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>:
    /// <code>
    ///  IDENTITY(100, 1)
    /// </code>
    /// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
    /// <code>
    ///  IDENTITY(100, 1) ORDER
    ///  IDENTITY START 100 INCREMENT 1 NOORDER
    /// </code>
    /// </summary>
    public sealed record Identity(IdentityProperty Value) : IdentityPropertyKind;

    public void Fmt(Formatter f)
    {
        var (command, property) = this switch
        {
            Identity { Value: var identityProperty } => ("IDENTITY", identityProperty),
            Autoincrement { Value: var autoincrementProperty } => ("AUTOINCREMENT", autoincrementProperty),
            _ => throw new InvalidOperationException($"Variante de IdentityPropertyKind não tratada: {GetType().Name}"),
        };
        f.Write($"{command}");
        if (property.Parameters is { } parameters)
        {
            f.Write($"{parameters}");
        }
        if (property.Order is { } order)
        {
            f.Write($"{order}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
