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
/// under <c>globalPrivileges</c> in the <c>MODIFY</c> privilege.
/// </summary>
public abstract partial record ActionModifyType : IDisplay
{
    /// <summary>Modify log level.</summary>
    public sealed record LogLevel : ActionModifyType;

    /// <summary>Modify trace level.</summary>
    public sealed record TraceLevel : ActionModifyType;

    /// <summary>Modify session log level.</summary>
    public sealed record SessionLogLevel : ActionModifyType;

    /// <summary>Modify session trace level.</summary>
    public sealed record SessionTraceLevel : ActionModifyType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case LogLevel:
                f.Write($"LOG LEVEL");
                break;
            case TraceLevel:
                f.Write($"TRACE LEVEL");
                break;
            case SessionLogLevel:
                f.Write($"SESSION LOG LEVEL");
                break;
            case SessionTraceLevel:
                f.Write($"SESSION TRACE LEVEL");
                break;
            default:
                throw new InvalidOperationException($"Variante de ActionModifyType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
