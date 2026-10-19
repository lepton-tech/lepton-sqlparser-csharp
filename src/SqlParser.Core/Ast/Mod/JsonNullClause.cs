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

/// <summary>MSSQL's json null clause</summary>
/// <remarks>
/// <code>
/// &lt;json_null_clause&gt; ::=
///       NULL ON NULL
///     | ABSENT ON NULL
/// </code>
/// <see href="https://learn.microsoft.com/en-us/sql/t-sql/functions/json-object-transact-sql?view=sql-server-ver16#json_null_clause"/>
/// </remarks>
public abstract partial record JsonNullClause : IDisplay
{
    /// <summary><c>NULL ON NULL</c> behavior for JSON functions.</summary>
    public sealed record NullOnNull : JsonNullClause;

    /// <summary><c>ABSENT ON NULL</c> behavior for JSON functions.</summary>
    public sealed record AbsentOnNull : JsonNullClause;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case NullOnNull:
                f.Write($"NULL ON NULL");
                break;
            case AbsentOnNull:
                f.Write($"ABSENT ON NULL");
                break;
            default:
                throw new InvalidOperationException($"Variante de JsonNullClause não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
