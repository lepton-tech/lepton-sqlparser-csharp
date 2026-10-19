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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Non-blocking lock options for <c>FOR ...</c> clauses.</summary>
// Enum sem dados com `Display` → records (convencoes.md §13, caso 37).
public abstract partial record NonBlock : IDisplay
{
    /// <summary><c>NOWAIT</c> — do not wait for the lock.</summary>
    public sealed record Nowait : NonBlock;

    /// <summary><c>SKIP LOCKED</c> — skip rows that are locked.</summary>
    public sealed record SkipLocked : NonBlock;

    public void Fmt(Formatter f)
    {
        var nonblock = this switch
        {
            Nowait => "NOWAIT",
            SkipLocked => "SKIP LOCKED",
            _ => throw new InvalidOperationException($"Variante de NonBlock não tratada: {GetType().Name}"),
        };
        f.Write($"{nonblock}");
    }

    public sealed override string ToString() => Formatter.Format(this);
}
