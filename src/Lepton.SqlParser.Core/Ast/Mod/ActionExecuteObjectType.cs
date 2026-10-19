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

/// <summary>
/// See <see href="https://docs.snowflake.com/en/sql-reference/sql/grant-privilege"/>
/// under <c>globalPrivileges</c> in the <c>EXECUTE</c> privilege.
/// </summary>
public abstract partial record ActionExecuteObjectType : IDisplay
{
    /// <summary>Alert object.</summary>
    public sealed record Alert : ActionExecuteObjectType;

    /// <summary>Data metric function object.</summary>
    public sealed record DataMetricFunction : ActionExecuteObjectType;

    /// <summary>Managed alert object.</summary>
    public sealed record ManagedAlert : ActionExecuteObjectType;

    /// <summary>Managed task object.</summary>
    public sealed record ManagedTask : ActionExecuteObjectType;

    /// <summary>Task object.</summary>
    public sealed record Task : ActionExecuteObjectType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Alert:
                f.Write($"ALERT");
                break;
            case DataMetricFunction:
                f.Write($"DATA METRIC FUNCTION");
                break;
            case ManagedAlert:
                f.Write($"MANAGED ALERT");
                break;
            case ManagedTask:
                f.Write($"MANAGED TASK");
                break;
            case Task:
                f.Write($"TASK");
                break;
            default:
                throw new InvalidOperationException($"Variante de ActionExecuteObjectType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
