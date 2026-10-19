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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/table_constraints.rs.
// Modificado: tradução de Rust para C#.

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>The operator that follows <c>WITH</c> in an <c>EXCLUDE</c> constraint element.</summary>
/// <remarks><see href="https://www.postgresql.org/docs/current/sql-createtable.html#SQL-CREATETABLE-EXCLUDE">PostgreSQL</see></remarks>
public abstract partial record ExcludeConstraintOperator : IDisplay
{
    /// <summary>A single operator token, e.g. <c>=</c>, <c>&amp;&amp;</c>, <c>&lt;-&gt;</c>.</summary>
    public sealed record Token(string Value) : ExcludeConstraintOperator;

    /// <summary>Postgres schema-qualified form: <c>OPERATOR(schema.op)</c>.</summary>
    public sealed record PGOperator(Sequence<string> Value) : ExcludeConstraintOperator;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Token token:
                f.WriteStr(token.Value);
                break;
            case PGOperator parts:
                f.Write($"OPERATOR({DisplaySeparated(parts.Value, ".")})");
                break;
            default:
                throw new InvalidOperationException($"Variante de ExcludeConstraintOperator não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
