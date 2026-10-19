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
/// under <c>globalPrivileges</c> in the <c>CREATE</c> privilege.
/// </summary>
public abstract partial record ActionCreateObjectType : IDisplay
{
    /// <summary>An account-level object.</summary>
    public sealed record Account : ActionCreateObjectType;

    /// <summary>An application object.</summary>
    public sealed record Application : ActionCreateObjectType;

    /// <summary>An application package object.</summary>
    public sealed record ApplicationPackage : ActionCreateObjectType;

    /// <summary>A compute pool object.</summary>
    public sealed record ComputePool : ActionCreateObjectType;

    /// <summary>A data exchange listing.</summary>
    public sealed record DataExchangeListing : ActionCreateObjectType;

    /// <summary>A database object.</summary>
    public sealed record Database : ActionCreateObjectType;

    /// <summary>An external volume object.</summary>
    public sealed record ExternalVolume : ActionCreateObjectType;

    /// <summary>A failover group object.</summary>
    public sealed record FailoverGroup : ActionCreateObjectType;

    /// <summary>An integration object.</summary>
    public sealed record Integration : ActionCreateObjectType;

    /// <summary>A network policy object.</summary>
    public sealed record NetworkPolicy : ActionCreateObjectType;

    /// <summary>An organization listing.</summary>
    // Grafia `Organiation` mantida igual à do Rust.
    public sealed record OrganiationListing : ActionCreateObjectType;

    /// <summary>A replication group object.</summary>
    public sealed record ReplicationGroup : ActionCreateObjectType;

    /// <summary>A role object.</summary>
    public sealed record Role : ActionCreateObjectType;

    /// <summary>A schema object.</summary>
    public sealed record Schema : ActionCreateObjectType;

    /// <summary>A share object.</summary>
    public sealed record Share : ActionCreateObjectType;

    /// <summary>A user object.</summary>
    public sealed record User : ActionCreateObjectType;

    /// <summary>A warehouse object.</summary>
    public sealed record Warehouse : ActionCreateObjectType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Account:
                f.Write($"ACCOUNT");
                break;
            case Application:
                f.Write($"APPLICATION");
                break;
            case ApplicationPackage:
                f.Write($"APPLICATION PACKAGE");
                break;
            case ComputePool:
                f.Write($"COMPUTE POOL");
                break;
            case DataExchangeListing:
                f.Write($"DATA EXCHANGE LISTING");
                break;
            case Database:
                f.Write($"DATABASE");
                break;
            case ExternalVolume:
                f.Write($"EXTERNAL VOLUME");
                break;
            case FailoverGroup:
                f.Write($"FAILOVER GROUP");
                break;
            case Integration:
                f.Write($"INTEGRATION");
                break;
            case NetworkPolicy:
                f.Write($"NETWORK POLICY");
                break;
            case OrganiationListing:
                f.Write($"ORGANIZATION LISTING");
                break;
            case ReplicationGroup:
                f.Write($"REPLICATION GROUP");
                break;
            case Role:
                f.Write($"ROLE");
                break;
            case Schema:
                f.Write($"SCHEMA");
                break;
            case Share:
                f.Write($"SHARE");
                break;
            case User:
                f.Write($"USER");
                break;
            case Warehouse:
                f.Write($"WAREHOUSE");
                break;
            default:
                throw new InvalidOperationException($"Variante de ActionCreateObjectType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
