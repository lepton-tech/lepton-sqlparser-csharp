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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>The right-hand side of an <c>IN</c> predicate, see <see cref="Expr.In"/>.</summary>
public abstract partial record InTarget : IDisplay
{
    /// <summary><c>(val1, val2, ...)</c></summary>
    public sealed record List(Sequence<Expr> Value) : InTarget;

    /// <summary><c>(SELECT ...)</c></summary>
    public sealed record Subquery(Query Value) : InTarget;

    /// <summary>
    /// <c>UNNEST(array_expression)</c>, see
    /// <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/operators#in_operators">BigQuery</see>
    /// </summary>
    public sealed record Unnest(Expr Value) : InTarget;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case List list:
                f.Write($"({DisplayCommaSeparated(list.Value)})");
                break;
            case Subquery subquery:
                f.Write($"({subquery.Value})");
                break;
            case Unnest arrayExpr:
                f.Write($"UNNEST({arrayExpr.Value})");
                break;
            default:
                throw new InvalidOperationException($"Variante de InTarget não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
