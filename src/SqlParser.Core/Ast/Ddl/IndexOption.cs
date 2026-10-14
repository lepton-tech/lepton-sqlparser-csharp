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
/// MySQL index option, used in <see href="https://dev.mysql.com/doc/refman/8.4/en/create-table.html"><c>CREATE TABLE</c></see>,
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/create-index.html"><c>CREATE INDEX</c></see>, and
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html"><c>ALTER TABLE</c></see>.
/// </summary>
public abstract record IndexOption : IDisplay
{
    /// <summary>
    /// <c>USING { BTREE | HASH }</c>: Index type to use for the index.
    /// <para>Note that we permissively parse non-MySQL index types, like <c>GIN</c>.</para>
    /// </summary>
    public sealed record Using(IndexType Value) : IndexOption;

    /// <summary><c>COMMENT 'string'</c>: Specifies a comment for the index.</summary>
    public sealed record Comment(string Value) : IndexOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Using { Value: var indexType }:
                f.Write($"USING {indexType}");
                break;
            case Comment { Value: var s }:
                f.Write($"COMMENT '{s}'");
                break;
            default:
                throw new InvalidOperationException($"Variante de IndexOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
