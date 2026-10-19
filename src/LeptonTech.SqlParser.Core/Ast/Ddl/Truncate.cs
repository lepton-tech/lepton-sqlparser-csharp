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

using static LeptonTech.SqlParser.Core.Ast.AstModule;
using static LeptonTech.SqlParser.Core.RustIter;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// A <c>TRUNCATE</c> statement.
/// <code>
/// TRUNCATE TABLE [IF EXISTS] table_names [PARTITION (partitions)] [RESTART IDENTITY | CONTINUE IDENTITY] [CASCADE | RESTRICT] [ON CLUSTER cluster_name]
/// </code>
/// </summary>
/// <param name="TableNames">Table names to truncate</param>
/// <param name="Partitions">Optional partition specification</param>
/// <param name="Table">TABLE - optional keyword</param>
/// <param name="IfExists">Snowflake/Redshift-specific option: [ IF EXISTS ]</param>
/// <param name="Identity">Postgres-specific option: [ RESTART IDENTITY | CONTINUE IDENTITY ]</param>
/// <param name="Cascade">Postgres-specific option: [ CASCADE | RESTRICT ]</param>
/// <param name="OnCluster">
/// ClickHouse-specific option: [ ON CLUSTER cluster_name ]
/// <see href="https://clickhouse.com/docs/en/sql-reference/statements/truncate/">ClickHouse</see>
/// </param>
public sealed partial record Truncate(
    Sequence<TruncateTableTarget> TableNames,
    Sequence<Expr>? Partitions,
    bool Table,
    bool IfExists,
    TruncateIdentityOption? Identity,
    CascadeOption? Cascade,
    Ident? OnCluster) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        var table = Table ? "TABLE " : "";
        var ifExists = IfExists ? "IF EXISTS " : "";

        f.Write($"TRUNCATE {table}{ifExists}{DisplayCommaSeparated(TableNames)}");

        if (Identity is { } identity)
        {
            switch (identity)
            {
                case TruncateIdentityOption.Restart:
                    f.Write($" RESTART IDENTITY");
                    break;
                case TruncateIdentityOption.Continue:
                    f.Write($" CONTINUE IDENTITY");
                    break;
                default:
                    throw new InvalidOperationException($"Variante de TruncateIdentityOption não tratada: {identity}");
            }
        }
        if (Cascade is { } cascade)
        {
            switch (cascade)
            {
                case CascadeOption.Cascade:
                    f.Write($" CASCADE");
                    break;
                case CascadeOption.Restrict:
                    f.Write($" RESTRICT");
                    break;
                default:
                    throw new InvalidOperationException($"Variante de CascadeOption não tratada: {cascade.GetType().Name}");
            }
        }

        if (Partitions is { } parts)
        {
            if (!parts.IsEmpty)
            {
                f.Write($" PARTITION ({DisplayCommaSeparated(parts)})");
            }
        }
        if (OnCluster is { } onCluster)
        {
            f.Write($" ON CLUSTER {onCluster}");
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for Truncate (ddl.rs:4320)
    public Span Span() =>
        Core.Span.UnionIter(
            TableNames.Select(i => i.Name.Span()).Concat(
                OptionIter(Partitions)
                    .SelectMany(i => i.Select(k => k.Span()))));
}
