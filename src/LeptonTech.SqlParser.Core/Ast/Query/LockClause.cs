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

/// <summary><c>FOR ...</c> locking clause.</summary>
/// <param name="LockType">The kind of lock requested (e.g. <c>SHARE</c>, <c>UPDATE</c>).</param>
/// <param name="Of">Optional object name after <c>OF</c> (e.g. <c>FOR UPDATE OF t1</c>).</param>
/// <param name="Nonblock">Optional non-blocking behavior (<c>NOWAIT</c> / <c>SKIP LOCKED</c>).</param>
public sealed partial record LockClause(LockType LockType, ObjectName? Of, NonBlock? Nonblock) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"FOR {LockType}");
        if (Of is { } of)
        {
            f.Write($" OF {of}");
        }
        if (Nonblock is { } nb)
        {
            f.Write($" {nb}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
