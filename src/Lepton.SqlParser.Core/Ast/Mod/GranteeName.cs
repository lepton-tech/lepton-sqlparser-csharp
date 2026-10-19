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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Users/roles designated in a GRANT/REVOKE</summary>
public abstract partial record GranteeName : IDisplay
{
    /// <summary>A bare identifier</summary>
    public sealed record ObjectName(Ast.ObjectName Value) : GranteeName;

    /// <summary>A MySQL user/host pair such as 'root'@'%'</summary>
    /// <param name="User">The user identifier portion.</param>
    /// <param name="Host">The host identifier portion.</param>
    public sealed record UserHost(Ident User, Ident Host) : GranteeName;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case ObjectName name:
                name.Value.Fmt(f);
                break;
            case UserHost userHost:
                f.Write($"{userHost.User}@{userHost.Host}");
                break;
            default:
                throw new InvalidOperationException($"Variante de GranteeName não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
