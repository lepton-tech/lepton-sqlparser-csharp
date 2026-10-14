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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/trigger.rs.
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>Used to describe trigger events</summary>
public abstract partial record TriggerEvent : IDisplay
{
    /// <summary>Trigger on INSERT event</summary>
    public sealed record Insert : TriggerEvent;

    /// <summary>Trigger on UPDATE event, with optional list of columns</summary>
    public sealed record Update(Sequence<Ident> Value) : TriggerEvent;

    /// <summary>Trigger on DELETE event</summary>
    public sealed record Delete : TriggerEvent;

    /// <summary>Trigger on TRUNCATE event</summary>
    public sealed record Truncate : TriggerEvent;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Insert:
                f.Write($"INSERT");
                break;
            case Update update:
                f.Write($"UPDATE");
                if (!update.Value.IsEmpty)
                {
                    f.Write($" OF");
                    f.Write($" {DisplayCommaSeparated(update.Value)}");
                }
                break;
            case Delete:
                f.Write($"DELETE");
                break;
            case Truncate:
                f.Write($"TRUNCATE");
                break;
            default:
                throw new InvalidOperationException($"Variante de TriggerEvent não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
