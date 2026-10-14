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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>Action to perform when an <c>ON CONFLICT</c> target is matched.</summary>
public abstract partial record OnConflictAction : IDisplay
{
    /// <summary>Do nothing on conflict.</summary>
    public sealed record DoNothing : OnConflictAction;

    /// <summary>Perform an update on conflict.</summary>
    public sealed record DoUpdate(Ast.DoUpdate Value) : OnConflictAction;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case DoNothing:
                f.Write($"DO NOTHING");
                break;
            case DoUpdate { Value: var doUpdate }:
                f.Write($"DO UPDATE");
                if (!doUpdate.Assignments.IsEmpty)
                {
                    f.Write($" SET {DisplayCommaSeparated(doUpdate.Assignments)}");
                }
                if (doUpdate.Selection is { } selection)
                {
                    f.Write($" WHERE {selection}");
                }
                break;
            default:
                throw new InvalidOperationException($"Variante de OnConflictAction não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
