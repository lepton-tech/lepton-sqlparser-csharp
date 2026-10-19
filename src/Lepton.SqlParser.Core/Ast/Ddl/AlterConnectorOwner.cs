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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>New connector owner specification for <c>ALTER CONNECTOR ... OWNER TO ...</c></summary>
public abstract partial record AlterConnectorOwner : IDisplay
{
    /// <summary><c>USER &lt;ident&gt;</c> connector owner.</summary>
    public sealed record User(Ident Value) : AlterConnectorOwner;

    /// <summary><c>ROLE &lt;ident&gt;</c> connector owner.</summary>
    public sealed record Role(Ident Value) : AlterConnectorOwner;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case User user:
                f.Write($"USER {user.Value}");
                break;
            case Role role:
                f.Write($"ROLE {role.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de AlterConnectorOwner não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
