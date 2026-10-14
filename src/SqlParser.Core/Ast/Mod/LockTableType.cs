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

/// <summary>The type of lock used in <c>LOCK TABLE</c> statements.</summary>
public abstract partial record LockTableType : IDisplay
{
    /// <summary>Shared/read lock. If <c>local</c> is true, it's a local read lock.</summary>
    /// <param name="Local">Whether the read lock is local.</param>
    public sealed record Read(bool Local) : LockTableType;

    /// <summary>Exclusive/write lock. If <c>low_priority</c> is true, the write is low priority.</summary>
    /// <param name="LowPriority">Whether the write lock is low priority.</param>
    public sealed record Write(bool LowPriority) : LockTableType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Read read:
                f.Write($"READ");
                if (read.Local)
                {
                    f.Write($" LOCAL");
                }
                break;
            case Write write:
                if (write.LowPriority)
                {
                    f.Write($"LOW_PRIORITY ");
                }
                f.Write($"WRITE");
                break;
            default:
                throw new InvalidOperationException($"Variante de LockTableType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
