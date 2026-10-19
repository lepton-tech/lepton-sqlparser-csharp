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

/// <summary>
/// A single CTE (used after <c>WITH</c>): <c>&lt;alias&gt; [(col1, col2, ...)] AS &lt;materialized&gt; ( &lt;query&gt; )</c>
/// The names in the column list before <c>AS</c>, when specified, replace the names
/// of the columns returned by the query. The parser does not validate that the
/// number of columns in the query matches the number of columns in the query.
/// </summary>
/// <param name="Alias">The CTE alias (name introduced before the <c>AS</c> keyword).</param>
/// <param name="Query">The query that defines the CTE body.</param>
/// <param name="From">Optional <c>FROM</c> identifier for materialized CTEs.</param>
/// <param name="Materialized">Optional <c>AS MATERIALIZED</c> / <c>AS NOT MATERIALIZED</c> hint.</param>
/// <param name="ClosingParenToken">Token for the closing parenthesis of the CTE definition.</param>
public sealed partial record Cte(
    TableAlias Alias,
    Query Query,
    Ident? From,
    CteAsMaterialized? Materialized,
    AttachedToken ClosingParenToken) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (Materialized)
        {
            case null:
                Alias.Fmt(f);
                f.WriteStr(" AS (");
                NewLine.Instance.Fmt(f);
                new Indent<Query>(Query).Fmt(f);
                NewLine.Instance.Fmt(f);
                f.WriteStr(")");
                break;
            case { } materialized:
                Alias.Fmt(f);
                f.WriteStr(" AS ");
                materialized.Fmt(f);
                f.WriteStr(" (");
                NewLine.Instance.Fmt(f);
                new Indent<Query>(Query).Fmt(f);
                NewLine.Instance.Fmt(f);
                f.WriteStr(")");
                break;
        }
        if (From is { } fr)
        {
            f.Write($" FROM {fr}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
