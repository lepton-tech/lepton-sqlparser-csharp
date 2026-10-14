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
/// <see href="https://www.postgresql.org/docs/17/sql-altertable.html">PostgreSQL</see> unique index nulls handling
/// option: <c>[ NULLS [ NOT ] DISTINCT ]</c>
/// </summary>
// Enum sem dados com Display: records (convencoes.md §13, caso 37).
public abstract record NullsDistinctOption : IDisplay
{
    /// <summary>Not specified</summary>
    public sealed record None : NullsDistinctOption;

    /// <summary>NULLS DISTINCT</summary>
    public sealed record Distinct : NullsDistinctOption;

    /// <summary>NULLS NOT DISTINCT</summary>
    public sealed record NotDistinct : NullsDistinctOption;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case None:
                break;
            case Distinct:
                f.Write($" NULLS DISTINCT");
                break;
            case NotDistinct:
                f.Write($" NULLS NOT DISTINCT");
                break;
            default:
                throw new InvalidOperationException($"Variante de NullsDistinctOption não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
