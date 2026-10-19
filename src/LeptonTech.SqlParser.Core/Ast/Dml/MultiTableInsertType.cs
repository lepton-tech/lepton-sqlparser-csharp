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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dml.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>The type of multi-table INSERT statement(Snowflake).</summary>
/// <remarks>See: <see href="https://docs.snowflake.com/en/sql-reference/sql/insert-multi-table"/></remarks>
public abstract partial record MultiTableInsertType : IDisplay
{
    /// <summary><c>INSERT ALL</c> - all matching WHEN clauses are executed</summary>
    public sealed record All : MultiTableInsertType;

    /// <summary><c>INSERT FIRST</c> - only the first matching WHEN clause is executed</summary>
    public sealed record First : MultiTableInsertType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case All:
                f.Write($"ALL");
                break;
            case First:
                f.Write($"FIRST");
                break;
            default:
                throw new InvalidOperationException($"Variante de MultiTableInsertType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
