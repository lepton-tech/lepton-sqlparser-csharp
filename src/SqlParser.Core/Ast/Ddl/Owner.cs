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

/// <summary>New owner specification for <c>ALTER TABLE ... OWNER TO ...</c></summary>
public abstract partial record Owner : IDisplay
{
    /// <summary>A specific user/role identifier.</summary>
    // A variante esconde o tipo de topo homônimo: tipo qualificado (convencoes.md §6 e §13, caso 38).
    public sealed record Ident(Ast.Ident Value) : Owner;

    /// <summary><c>CURRENT_ROLE</c> keyword.</summary>
    public sealed record CurrentRole : Owner;

    /// <summary><c>CURRENT_USER</c> keyword.</summary>
    public sealed record CurrentUser : Owner;

    /// <summary><c>SESSION_USER</c> keyword.</summary>
    public sealed record SessionUser : Owner;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Ident ident:
                f.Write($"{ident.Value}");
                break;
            case CurrentRole:
                f.Write($"CURRENT_ROLE");
                break;
            case CurrentUser:
                f.Write($"CURRENT_USER");
                break;
            case SessionUser:
                f.Write($"SESSION_USER");
                break;
            default:
                throw new InvalidOperationException($"Variante de Owner não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
