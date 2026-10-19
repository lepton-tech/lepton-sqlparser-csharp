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

/// <summary>The principal that receives the privileges</summary>
/// <param name="GranteeType">The category/type of grantee (role, user, share, etc.).</param>
/// <param name="Name">Optional name of the grantee (identifier or user@host).</param>
public sealed partial record Grantee(GranteesType GranteeType, GranteeName? Name) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var keyword = GranteeType switch
        {
            GranteesType.Role => "ROLE",
            GranteesType.Share => "SHARE",
            GranteesType.User => "USER",
            GranteesType.Group => "GROUP",
            GranteesType.Public => "PUBLIC",
            GranteesType.DatabaseRole => "DATABASE ROLE",
            GranteesType.Application => "APPLICATION",
            GranteesType.ApplicationRole => "APPLICATION ROLE",
            GranteesType.None => "",
            _ => throw new InvalidOperationException($"Variante de GranteesType não tratada: {GranteeType}"),
        };
        f.WriteStr(keyword);
        if (Name is { } name)
        {
            if (keyword.Length != 0)
            {
                f.WriteStr(" ");
            }
            name.Fmt(f);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
