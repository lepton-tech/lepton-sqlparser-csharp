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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Command that a policy can apply to (FOR clause).
/// <code>
/// FOR [ALL | SELECT | INSERT | UPDATE | DELETE]
/// </code>
/// <see href="https://www.postgresql.org/docs/current/sql-createpolicy.html">PostgreSQL</see>
/// </summary>
// Enum sem dados com Display → records (convencoes.md §13, caso 37).
public abstract partial record CreatePolicyCommand : IDisplay
{
    /// <summary>Applies to all commands.</summary>
    public sealed record All : CreatePolicyCommand;

    /// <summary>Applies to SELECT.</summary>
    public sealed record Select : CreatePolicyCommand;

    /// <summary>Applies to INSERT.</summary>
    public sealed record Insert : CreatePolicyCommand;

    /// <summary>Applies to UPDATE.</summary>
    public sealed record Update : CreatePolicyCommand;

    /// <summary>Applies to DELETE.</summary>
    public sealed record Delete : CreatePolicyCommand;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case All:
                f.Write($"ALL");
                break;
            case Select:
                f.Write($"SELECT");
                break;
            case Insert:
                f.Write($"INSERT");
                break;
            case Update:
                f.Write($"UPDATE");
                break;
            case Delete:
                f.Write($"DELETE");
                break;
            default:
                throw new InvalidOperationException($"Variante de CreatePolicyCommand não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
