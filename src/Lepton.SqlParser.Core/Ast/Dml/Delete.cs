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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>DELETE statement.</summary>
/// <param name="DeleteToken">Token for the <c>DELETE</c> keyword</param>
/// <param name="OptimizerHints">
/// Query optimizer hints
/// <para>
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/optimizer-hints.html">MySQL</see>
/// <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Comments.html#GUID-D316D545-89E2-4D54-977F-FC97815CD62E">Oracle</see>
/// </para>
/// </param>
/// <param name="Tables">Multi tables delete are supported in mysql</param>
/// <param name="From">FROM</param>
/// <param name="Using">USING (Snowflake, Postgres, MySQL)</param>
/// <param name="Selection">WHERE</param>
/// <param name="Returning">RETURNING</param>
/// <param name="Output">
/// OUTPUT (MSSQL)
/// See <see href="https://learn.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql"/>
/// </param>
/// <param name="OrderBy">ORDER BY (MySQL)</param>
/// <param name="Limit">LIMIT (MySQL)</param>
public sealed partial record Delete(
    AttachedToken DeleteToken,
    Sequence<OptimizerHint> OptimizerHints,
    Sequence<ObjectName> Tables,
    FromTable From,
    Sequence<TableWithJoins>? Using,
    Expr? Selection,
    Sequence<SelectItem>? Returning,
    OutputClause? Output,
    Sequence<OrderByExpr> OrderBy,
    Expr? Limit) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.WriteStr("DELETE");
        foreach (var hint in OptimizerHints)
        {
            f.WriteStr(" ");
            hint.Fmt(f);
        }
        if (!Tables.IsEmpty)
        {
            DisplayUtils.IndentedList(f, Tables);
        }
        switch (From)
        {
            case FromTable.WithFromKeyword from:
                f.WriteStr(" FROM");
                DisplayUtils.IndentedList(f, from.Value);
                break;
            case FromTable.WithoutKeyword from:
                DisplayUtils.IndentedList(f, from.Value);
                break;
            default:
                throw new InvalidOperationException($"Variante de FromTable não tratada: {From.GetType().Name}");
        }
        if (Output is { } output)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.Write($"{output}");
        }
        if (Using is { } @using)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("USING");
            DisplayUtils.IndentedList(f, @using);
        }
        if (Selection is { } selection)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("WHERE");
            SpaceOrNewline.Instance.Fmt(f);
            new Indent<Expr>(selection).Fmt(f);
        }
        if (Returning is { } returning)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("RETURNING");
            DisplayUtils.IndentedList(f, returning);
        }
        if (!OrderBy.IsEmpty)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("ORDER BY");
            DisplayUtils.IndentedList(f, OrderBy);
        }
        if (Limit is { } limit)
        {
            SpaceOrNewline.Instance.Fmt(f);
            f.WriteStr("LIMIT");
            SpaceOrNewline.Instance.Fmt(f);
            new Indent<Expr>(limit).Fmt(f);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
