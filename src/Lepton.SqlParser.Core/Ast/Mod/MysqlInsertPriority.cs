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
/// Mysql specific syntax
/// <para>
/// See <see href="https://dev.mysql.com/doc/refman/8.0/en/replace.html">Mysql documentation</see>
/// See <see href="https://dev.mysql.com/doc/refman/8.0/en/insert.html">Mysql documentation</see>
/// for more details.
/// </para>
/// </summary>
public abstract partial record MysqlInsertPriority : IDisplay
{
    /// <summary>LOW_PRIORITY modifier for INSERT/REPLACE.</summary>
    public sealed record LowPriority : MysqlInsertPriority;

    /// <summary>DELAYED modifier for INSERT/REPLACE.</summary>
    public sealed record Delayed : MysqlInsertPriority;

    /// <summary>HIGH_PRIORITY modifier for INSERT/REPLACE.</summary>
    public sealed record HighPriority : MysqlInsertPriority;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case LowPriority:
                f.Write($"LOW_PRIORITY");
                break;
            case Delayed:
                f.Write($"DELAYED");
                break;
            case HighPriority:
                f.Write($"HIGH_PRIORITY");
                break;
            default:
                throw new InvalidOperationException($"Variante de MysqlInsertPriority não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
