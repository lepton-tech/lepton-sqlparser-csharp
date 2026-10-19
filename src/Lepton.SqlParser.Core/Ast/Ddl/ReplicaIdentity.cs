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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/ddl.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// ALTER TABLE operation REPLICA IDENTITY values
/// See <see href="https://www.postgresql.org/docs/current/sql-altertable.html">Postgres ALTER TABLE docs</see>
/// </summary>
public abstract partial record ReplicaIdentity : IDisplay
{
    /// <summary>No replica identity (<c>REPLICA IDENTITY NOTHING</c>).</summary>
    public sealed record Nothing : ReplicaIdentity;

    /// <summary>Full replica identity (<c>REPLICA IDENTITY FULL</c>).</summary>
    public sealed record Full : ReplicaIdentity;

    /// <summary>Default replica identity (<c>REPLICA IDENTITY DEFAULT</c>).</summary>
    public sealed record Default : ReplicaIdentity;

    /// <summary>Use the given index as replica identity (<c>REPLICA IDENTITY USING INDEX</c>).</summary>
    public sealed record Index(Ident Value) : ReplicaIdentity;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Nothing:
                f.WriteStr("NOTHING");
                break;
            case Full:
                f.WriteStr("FULL");
                break;
            case Default:
                f.WriteStr("DEFAULT");
                break;
            case Index idx:
                f.Write($"USING INDEX {idx.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de ReplicaIdentity não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
