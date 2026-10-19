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

/// <summary>Specifies the behavior of the initial refresh of the dynamic table.</summary>
/// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/create-dynamic-table">Snowflake</see></remarks>
public abstract partial record InitializeKind : IDisplay
{
    /// <summary>Initialize on creation (<c>ON CREATE</c>).</summary>
    public sealed record OnCreate : InitializeKind;

    /// <summary>Initialize on schedule (<c>ON SCHEDULE</c>).</summary>
    public sealed record OnSchedule : InitializeKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case OnCreate:
                f.Write($"ON_CREATE");
                break;
            case OnSchedule:
                f.Write($"ON_SCHEDULE");
                break;
            default:
                throw new InvalidOperationException($"Variante de InitializeKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
