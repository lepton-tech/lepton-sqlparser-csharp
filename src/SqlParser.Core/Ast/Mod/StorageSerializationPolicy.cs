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

/// <summary>
/// Snowflake StorageSerializationPolicy for Iceberg Tables
/// <code>
/// [ STORAGE_SERIALIZATION_POLICY = { COMPATIBLE | OPTIMIZED } ]
/// </code>
/// </summary>
/// <remarks><see href="https://docs.snowflake.com/en/sql-reference/sql/create-iceberg-table"/></remarks>
public abstract partial record StorageSerializationPolicy : IDisplay
{
    /// <summary>Use compatible serialization mode.</summary>
    public sealed record Compatible : StorageSerializationPolicy;

    /// <summary>Use optimized serialization mode.</summary>
    public sealed record Optimized : StorageSerializationPolicy;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Compatible:
                f.Write($"COMPATIBLE");
                break;
            case Optimized:
                f.Write($"OPTIMIZED");
                break;
            default:
                throw new InvalidOperationException($"Variante de StorageSerializationPolicy não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
