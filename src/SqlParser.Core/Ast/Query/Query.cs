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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// The most complete variant of a <c>SELECT</c> query expression, optionally
/// including <c>WITH</c>, <c>UNION</c> / other set operations, and <c>ORDER BY</c>.
/// </summary>
/// <param name="With">WITH (common table expressions, or CTEs)</param>
/// <param name="Body">SELECT or UNION / EXCEPT / INTERSECT</param>
/// <param name="OrderBy">ORDER BY</param>
/// <param name="LimitClause"><c>LIMIT ... OFFSET ... | LIMIT &lt;offset&gt;, &lt;limit&gt;</c></param>
/// <param name="Fetch"><c>FETCH { FIRST | NEXT } &lt;N&gt; [ PERCENT ] { ROW | ROWS } | { ONLY | WITH TIES }</c></param>
/// <param name="Locks"><c>FOR { UPDATE | SHARE } [ OF table_name ] [ SKIP LOCKED | NOWAIT ]</c></param>
/// <param name="ForClause">
/// <c>FOR XML { RAW | AUTO | EXPLICIT | PATH } [ , ELEMENTS ]</c>
/// <c>FOR JSON { AUTO | PATH } [ , INCLUDE_NULL_VALUES ]</c>
/// (MSSQL-specific)
/// </param>
/// <param name="Settings">
/// ClickHouse syntax: <c>SELECT * FROM t SETTINGS key1 = value1, key2 = value2</c>
/// <para><see href="https://clickhouse.com/docs/en/sql-reference/statements/select#settings-in-select-query">ClickHouse</see></para>
/// </param>
/// <param name="FormatClause">
/// <c>SELECT * FROM t FORMAT JSONCompact</c>
/// <para>
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/select/format">ClickHouse</see>
/// (ClickHouse-specific)
/// </para>
/// </param>
/// <param name="PipeOperators">Pipe operator</param>
/// <remarks>
/// O módulo Rust <c>query</c> tem um tipo homônimo (<c>Query</c>): funções livres do módulo seriam
/// membros estáticos deste record (convencoes.md §13, caso 5). Em v0.63.0, <c>query.rs</c> não tem
/// funções livres.
/// </remarks>
[VisitWith("visit_query")]
public sealed partial record Query(
    With? With,
    SetExpr Body,
    OrderBy? OrderBy,
    LimitClause? LimitClause,
    Fetch? Fetch,
    Sequence<LockClause> Locks,
    ForClause? ForClause,
    Sequence<Setting>? Settings,
    FormatClause? FormatClause,
    Sequence<PipeOperator> PipeOperators) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (With is { } withClause)
        {
            withClause.Fmt(f);
            SpaceOrNewline.Instance.Fmt(f);
        }
        Body.Fmt(f);
        if (OrderBy is { } orderBy)
        {
            f.WriteStr(" ");
            orderBy.Fmt(f);
        }

        if (LimitClause is { } limitClause)
        {
            limitClause.Fmt(f);
        }
        if (Settings is { } settings)
        {
            f.WriteStr(" SETTINGS ");
            DisplayCommaSeparated(settings).Fmt(f);
        }
        if (Fetch is { } fetch)
        {
            f.WriteStr(" ");
            fetch.Fmt(f);
        }
        if (!Locks.IsEmpty)
        {
            f.WriteStr(" ");
            DisplaySeparated(Locks, " ").Fmt(f);
        }
        if (ForClause is { } forClause)
        {
            f.WriteStr(" ");
            forClause.Fmt(f);
        }
        if (FormatClause is { } format)
        {
            f.WriteStr(" ");
            format.Fmt(f);
        }
        foreach (var pipeOperator in PipeOperators)
        {
            f.WriteStr(" |> ");
            pipeOperator.Fmt(f);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
