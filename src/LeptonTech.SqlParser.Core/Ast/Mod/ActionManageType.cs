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

/// <summary>
/// See <see href="https://docs.snowflake.com/en/sql-reference/sql/grant-privilege"/>
/// under <c>globalPrivileges</c> in the <c>MANAGE</c> privilege.
/// </summary>
public abstract partial record ActionManageType : IDisplay
{
    /// <summary>Account support cases management.</summary>
    public sealed record AccountSupportCases : ActionManageType;

    /// <summary>Event sharing management.</summary>
    public sealed record EventSharing : ActionManageType;

    /// <summary>Grants management.</summary>
    public sealed record Grants : ActionManageType;

    /// <summary>Listing auto-fulfillment management.</summary>
    public sealed record ListingAutoFulfillment : ActionManageType;

    /// <summary>Organization support cases management.</summary>
    public sealed record OrganizationSupportCases : ActionManageType;

    /// <summary>User support cases management.</summary>
    public sealed record UserSupportCases : ActionManageType;

    /// <summary>Warehouses management.</summary>
    public sealed record Warehouses : ActionManageType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case AccountSupportCases:
                f.Write($"ACCOUNT SUPPORT CASES");
                break;
            case EventSharing:
                f.Write($"EVENT SHARING");
                break;
            case Grants:
                f.Write($"GRANTS");
                break;
            case ListingAutoFulfillment:
                f.Write($"LISTING AUTO FULFILLMENT");
                break;
            case OrganizationSupportCases:
                f.Write($"ORGANIZATION SUPPORT CASES");
                break;
            case UserSupportCases:
                f.Write($"USER SUPPORT CASES");
                break;
            case Warehouses:
                f.Write($"WAREHOUSES");
                break;
            default:
                throw new InvalidOperationException($"Variante de ActionManageType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
