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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// Hive supports <c>CLUSTERED BY</c> statement in <c>CREATE TABLE</c>.
/// Syntax: <c>CLUSTERED BY (col_name, ...) [SORTED BY (col_name [ASC|DESC], ...)] INTO num_buckets BUCKETS</c>
/// <para><see href="https://cwiki.apache.org/confluence/display/Hive/LanguageManual+DDL#LanguageManualDDL-CreateTable">Hive</see></para>
/// </summary>
/// <param name="Columns">columns used for clustering</param>
/// <param name="SortedBy">optional sorted by expressions</param>
/// <param name="NumBuckets">number of buckets</param>
public sealed partial record ClusteredBy(Sequence<Ident> Columns, Sequence<OrderByExpr>? SortedBy, Value NumBuckets) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CLUSTERED BY ({DisplayCommaSeparated(Columns)})");
        if (SortedBy is { } sortedBy)
        {
            f.Write($" SORTED BY ({DisplayCommaSeparated(sortedBy)})");
        }
        f.Write($" INTO {NumBuckets} BUCKETS");
    }

    public override string ToString() => Formatter.Format(this);
}
