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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>CREATE INDEX statement.</summary>
/// <param name="Name">index name</param>
/// <param name="TableName">table name</param>
/// <param name="Using">
/// Index type used in the statement. Can also be found inside <see cref="IndexOptions"/>
/// depending on the position of the option within the statement.
/// </param>
/// <param name="Columns">columns included in the index</param>
/// <param name="Unique">whether the index is unique</param>
/// <param name="Concurrently">whether the index is created concurrently</param>
/// <param name="Async">
/// whether the index is created asynchronously
/// (<see href="https://docs.aws.amazon.com/aurora-dsql/latest/userguide/working-with-create-index-async.html">DSQL</see>).
/// </param>
/// <param name="IfNotExists">IF NOT EXISTS clause</param>
/// <param name="Include">INCLUDE clause: <see href="https://www.postgresql.org/docs/current/sql-createindex.html"/></param>
/// <param name="NullsDistinct">NULLS DISTINCT / NOT DISTINCT clause: <see href="https://www.postgresql.org/docs/current/sql-createindex.html"/></param>
/// <param name="With">WITH clause: <see href="https://www.postgresql.org/docs/current/sql-createindex.html"/></param>
/// <param name="Predicate">WHERE clause: <see href="https://www.postgresql.org/docs/current/sql-createindex.html"/></param>
/// <param name="IndexOptions">Index options: <see href="https://www.postgresql.org/docs/current/sql-createindex.html"/></param>
/// <param name="AlterOptions">
/// <see href="https://dev.mysql.com/doc/refman/8.4/en/create-index.html">MySQL</see> allows a subset of options
/// normally used for <c>ALTER TABLE</c>:
/// <list type="bullet">
/// <item><c>ALGORITHM</c></item>
/// <item><c>LOCK</c></item>
/// </list>
/// </param>
public sealed partial record CreateIndex(
    ObjectName? Name,
    [property: VisitWith("visit_relation")] ObjectName TableName,
    IndexType? Using,
    Sequence<IndexColumn> Columns,
    bool Unique,
    bool Concurrently,
    bool Async,
    bool IfNotExists,
    Sequence<Ident> Include,
    bool? NullsDistinct,
    Sequence<Expr> With,
    Expr? Predicate,
    Sequence<IndexOption> IndexOptions,
    Sequence<AlterTableOperation> AlterOptions) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CREATE {(Unique ? "UNIQUE " : "")}INDEX {(Concurrently ? "CONCURRENTLY " : "")}{(Async ? "ASYNC " : "")}{(IfNotExists ? "IF NOT EXISTS " : "")}");
        if (Name is { } name)
        {
            f.Write($"{name} ");
        }
        f.Write($"ON {TableName}");
        if (Using is { } @using)
        {
            f.Write($" USING {@using} ");
        }
        f.Write($"({DisplayCommaSeparated(Columns)})");
        if (!Include.IsEmpty)
        {
            f.Write($" INCLUDE ({DisplayCommaSeparated(Include)})");
        }
        if (NullsDistinct is { } value)
        {
            if (value)
            {
                f.Write($" NULLS DISTINCT");
            }
            else
            {
                f.Write($" NULLS NOT DISTINCT");
            }
        }
        if (!With.IsEmpty)
        {
            f.Write($" WITH ({DisplayCommaSeparated(With)})");
        }
        if (Predicate is { } predicate)
        {
            f.Write($" WHERE {predicate}");
        }
        if (!IndexOptions.IsEmpty)
        {
            f.Write($" {DisplaySeparated(IndexOptions, " ")}");
        }
        if (!AlterOptions.IsEmpty)
        {
            f.Write($" {DisplaySeparated(AlterOptions, " ")}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
