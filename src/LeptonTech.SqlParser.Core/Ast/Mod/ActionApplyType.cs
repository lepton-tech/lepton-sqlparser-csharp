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
/// under <c>globalPrivileges</c> in the <c>APPLY</c> privilege.
/// </summary>
public abstract partial record ActionApplyType : IDisplay
{
    /// <summary>Apply an aggregation policy.</summary>
    public sealed record AggregationPolicy : ActionApplyType;

    /// <summary>Apply an authentication policy.</summary>
    public sealed record AuthenticationPolicy : ActionApplyType;

    /// <summary>Apply a join policy.</summary>
    public sealed record JoinPolicy : ActionApplyType;

    /// <summary>Apply a masking policy.</summary>
    public sealed record MaskingPolicy : ActionApplyType;

    /// <summary>Apply a packages policy.</summary>
    public sealed record PackagesPolicy : ActionApplyType;

    /// <summary>Apply a password policy.</summary>
    public sealed record PasswordPolicy : ActionApplyType;

    /// <summary>Apply a projection policy.</summary>
    public sealed record ProjectionPolicy : ActionApplyType;

    /// <summary>Apply a row access policy.</summary>
    public sealed record RowAccessPolicy : ActionApplyType;

    /// <summary>Apply a session policy.</summary>
    public sealed record SessionPolicy : ActionApplyType;

    /// <summary>Apply a tag.</summary>
    public sealed record Tag : ActionApplyType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case AggregationPolicy:
                f.Write($"AGGREGATION POLICY");
                break;
            case AuthenticationPolicy:
                f.Write($"AUTHENTICATION POLICY");
                break;
            case JoinPolicy:
                f.Write($"JOIN POLICY");
                break;
            case MaskingPolicy:
                f.Write($"MASKING POLICY");
                break;
            case PackagesPolicy:
                f.Write($"PACKAGES POLICY");
                break;
            case PasswordPolicy:
                f.Write($"PASSWORD POLICY");
                break;
            case ProjectionPolicy:
                f.Write($"PROJECTION POLICY");
                break;
            case RowAccessPolicy:
                f.Write($"ROW ACCESS POLICY");
                break;
            case SessionPolicy:
                f.Write($"SESSION POLICY");
                break;
            case Tag:
                f.Write($"TAG");
                break;
            default:
                throw new InvalidOperationException($"Variante de ActionApplyType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
