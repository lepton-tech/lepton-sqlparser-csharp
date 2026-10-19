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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dcl.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>An option in <c>ROLE</c> statement.</summary>
/// <remarks><see href="https://www.postgresql.org/docs/current/sql-createrole.html"/></remarks>
public abstract partial record RoleOption : IDisplay
{
    /// <summary>Enable or disable BYPASSRLS.</summary>
    public sealed record BypassRLS(bool Value) : RoleOption;

    /// <summary>Connection limit expression.</summary>
    public sealed record ConnectionLimit(Expr Value) : RoleOption;

    /// <summary>CREATEDB flag.</summary>
    public sealed record CreateDB(bool Value) : RoleOption;

    /// <summary>CREATEROLE flag.</summary>
    public sealed record CreateRole(bool Value) : RoleOption;

    /// <summary>INHERIT flag.</summary>
    public sealed record Inherit(bool Value) : RoleOption;

    /// <summary>LOGIN flag.</summary>
    public sealed record Login(bool Value) : RoleOption;

    /// <summary>Password value or NULL password.</summary>
    public sealed record Password(Ast.Password Value) : RoleOption;

    /// <summary>Replication privilege flag.</summary>
    public sealed record Replication(bool Value) : RoleOption;

    /// <summary>SUPERUSER flag.</summary>
    public sealed record SuperUser(bool Value) : RoleOption;

    /// <summary><c>VALID UNTIL</c> expression.</summary>
    public sealed record ValidUntil(Expr Value) : RoleOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case BypassRLS value:
                f.Write($"{(value.Value ? "BYPASSRLS" : "NOBYPASSRLS")}");
                break;
            case ConnectionLimit expr:
                f.Write($"CONNECTION LIMIT {expr.Value}");
                break;
            case CreateDB value:
                f.Write($"{(value.Value ? "CREATEDB" : "NOCREATEDB")}");
                break;
            case CreateRole value:
                f.Write($"{(value.Value ? "CREATEROLE" : "NOCREATEROLE")}");
                break;
            case Inherit value:
                f.Write($"{(value.Value ? "INHERIT" : "NOINHERIT")}");
                break;
            case Login value:
                f.Write($"{(value.Value ? "LOGIN" : "NOLOGIN")}");
                break;
            case Password password:
                switch (password.Value)
                {
                    case Ast.Password.PasswordValue expr:
                        f.Write($"PASSWORD {expr.Value}");
                        break;
                    case Ast.Password.NullPassword:
                        f.Write($"PASSWORD NULL");
                        break;
                    default:
                        throw new InvalidOperationException($"Variante de Password não tratada: {password.Value.GetType().Name}");
                }
                break;
            case Replication value:
                f.Write($"{(value.Value ? "REPLICATION" : "NOREPLICATION")}");
                break;
            case SuperUser value:
                f.Write($"{(value.Value ? "SUPERUSER" : "NOSUPERUSER")}");
                break;
            case ValidUntil expr:
                f.Write($"VALID UNTIL {expr.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de RoleOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
