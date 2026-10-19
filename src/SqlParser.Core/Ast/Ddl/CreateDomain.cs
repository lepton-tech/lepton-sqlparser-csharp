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

/// <summary>
/// <code>
/// CREATE DOMAIN name [ AS ] data_type
///         [ COLLATE collation ]
///         [ DEFAULT expression ]
///         [ domain_constraint [ ... ] ]
///
///     where domain_constraint is:
///
///     [ CONSTRAINT constraint_name ]
///     { NOT NULL | NULL | CHECK (expression) }
/// </code>
/// See <see href="https://www.postgresql.org/docs/current/sql-createdomain.html">PostgreSQL</see>
/// </summary>
/// <param name="Name">The name of the domain to be created.</param>
/// <param name="DataType">The data type of the domain.</param>
/// <param name="Collation">The collation of the domain.</param>
/// <param name="Default">The default value of the domain.</param>
/// <param name="Constraints">The constraints of the domain.</param>
public sealed partial record CreateDomain(
    ObjectName Name,
    DataType DataType,
    Ident? Collation,
    Expr? Default,
    Sequence<TableConstraint> Constraints) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CREATE DOMAIN {Name} AS {DataType}");
        if (Collation is { } collation)
        {
            f.Write($" COLLATE {collation}");
        }
        if (Default is { } @default)
        {
            f.Write($" DEFAULT {@default}");
        }
        if (!Constraints.IsEmpty)
        {
            f.Write($" {DisplaySeparated(Constraints, " ")}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
