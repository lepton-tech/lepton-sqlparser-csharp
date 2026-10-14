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

namespace SqlParser.Core.Ast;

/// <summary><c>FORMAT</c> identifier or <c>FORMAT NULL</c> clause, specific to ClickHouse.</summary>
/// <remarks>
/// [ClickHouse]: <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/format"/>
/// </remarks>
public abstract record FormatClause : IDisplay
{
    /// <summary>The format identifier.</summary>
    public sealed record Identifier(Ident Value) : FormatClause;

    /// <summary><c>FORMAT NULL</c> clause.</summary>
    public sealed record Null : FormatClause;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Identifier { Value: var ident }:
                f.Write($"FORMAT {ident}");
                break;
            case Null:
                f.Write($"FORMAT NULL");
                break;
            default:
                throw new InvalidOperationException($"Variante de FormatClause não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
