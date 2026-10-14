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

namespace SqlParser.Core.Ast;

/// <summary>Specifies the refresh mode for the dynamic table.</summary>
/// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/create-dynamic-table">Snowflake</see></remarks>
public abstract record RefreshModeKind : IDisplay
{
    /// <summary>Automatic refresh mode (<c>AUTO</c>).</summary>
    public sealed record Auto : RefreshModeKind;

    /// <summary>Full refresh mode (<c>FULL</c>).</summary>
    public sealed record Full : RefreshModeKind;

    /// <summary>Incremental refresh mode (<c>INCREMENTAL</c>).</summary>
    public sealed record Incremental : RefreshModeKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Auto:
                f.Write($"AUTO");
                break;
            case Full:
                f.Write($"FULL");
                break;
            case Incremental:
                f.Write($"INCREMENTAL");
                break;
            default:
                throw new InvalidOperationException($"Variante de RefreshModeKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
