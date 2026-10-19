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

/// <summary>
/// An <c>ALTER Policy</c> (<c>Statement::AlterPolicy</c>) operation
/// <para><see href="https://www.postgresql.org/docs/current/sql-altertable.html">PostgreSQL Documentation</see></para>
/// </summary>
public abstract partial record AlterPolicyOperation : IDisplay
{
    /// <summary>Rename the policy to <c>new_name</c>.</summary>
    /// <param name="NewName">The new identifier for the policy.</param>
    public sealed record Rename(Ident NewName) : AlterPolicyOperation;

    /// <summary>Apply/modify policy properties.</summary>
    /// <param name="To">Optional list of owners the policy applies to.</param>
    /// <param name="Using">Optional <c>USING</c> expression for the policy.</param>
    /// <param name="WithCheck">Optional <c>WITH CHECK</c> expression for the policy.</param>
    public sealed record Apply(Sequence<Owner>? To, Expr? Using, Expr? WithCheck) : AlterPolicyOperation;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Rename rename:
                f.Write($" RENAME TO {rename.NewName}");
                break;
            case Apply apply:
            {
                if (apply.To is { } to)
                {
                    f.Write($" TO {DisplayCommaSeparated(to)}");
                }
                if (apply.Using is { } @using)
                {
                    f.Write($" USING ({@using})");
                }
                if (apply.WithCheck is { } withCheck)
                {
                    f.Write($" WITH CHECK ({withCheck})");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Variante de AlterPolicyOperation não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
