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

namespace SqlParser.Core.Ast;

/// <summary>
/// Indexing method used by that index.
/// <para>
/// This structure isn't present on ANSI, but is found at least in
/// <see href="https://dev.mysql.com/doc/refman/8.0/en/create-table.html"><c>MySQL</c> CREATE TABLE</see>,
/// <see href="https://dev.mysql.com/doc/refman/8.0/en/create-index.html"><c>MySQL</c> CREATE INDEX</see>, and
/// <see href="https://www.postgresql.org/docs/14/sql-createindex.html">Postgresql CREATE INDEX</see> statements.
/// </para>
/// </summary>
public abstract record IndexType : IDisplay
{
    /// <summary>B-Tree index (commonly default for many databases).</summary>
    public sealed record BTree : IndexType;

    /// <summary>Hash index.</summary>
    public sealed record Hash : IndexType;

    /// <summary>Generalized Inverted Index (GIN).</summary>
    public sealed record GIN : IndexType;

    /// <summary>Generalized Search Tree (GiST) index.</summary>
    public sealed record GiST : IndexType;

    /// <summary>Space-partitioned GiST (SPGiST) index.</summary>
    public sealed record SPGiST : IndexType;

    /// <summary>Block Range Index (BRIN).</summary>
    public sealed record BRIN : IndexType;

    /// <summary>Bloom filter based index.</summary>
    public sealed record Bloom : IndexType;

    /// <summary>
    /// Users may define their own index types, which would
    /// not be covered by the above variants.
    /// </summary>
    public sealed record Custom(Ident Value) : IndexType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case BTree:
                f.Write($"BTREE");
                break;
            case Hash:
                f.Write($"HASH");
                break;
            case GIN:
                f.Write($"GIN");
                break;
            case GiST:
                f.Write($"GIST");
                break;
            case SPGiST:
                f.Write($"SPGIST");
                break;
            case BRIN:
                f.Write($"BRIN");
                break;
            case Bloom:
                f.Write($"BLOOM");
                break;
            case Custom { Value: var name }:
                f.Write($"{name}");
                break;
            default:
                throw new InvalidOperationException($"Variante de IndexType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
