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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Represents a single PostgreSQL utility option.</summary>
/// <remarks>
/// A utility option is a key-value pair where the key is an identifier (IDENT) and the value
/// can be one of the following:
/// - A number with an optional sign (<c>+</c> or <c>-</c>). Example: <c>+10</c>, <c>-10.2</c>, <c>3</c>
/// - A non-keyword string. Example: <c>option1</c>, <c>'option2'</c>, <c>"option3"</c>
/// - keyword: <c>TRUE</c>, <c>FALSE</c>, <c>ON</c> (<c>off</c> is also accept).
/// - Empty. Example: <c>ANALYZE</c> (identifier only)
/// <para>
/// Utility options are used in various PostgreSQL DDL statements, including statements such as
/// <c>CLUSTER</c>, <c>EXPLAIN</c>, <c>VACUUM</c>, and <c>REINDEX</c>. These statements format options as <c>( option [, ...] )</c>.
/// </para>
/// <para>
/// <see href="https://www.postgresql.org/docs/current/sql-cluster.html">CLUSTER</see>
/// <see href="https://www.postgresql.org/docs/current/sql-explain.html">EXPLAIN</see>
/// <see href="https://www.postgresql.org/docs/current/sql-vacuum.html">VACUUM</see>
/// <see href="https://www.postgresql.org/docs/current/sql-reindex.html">REINDEX</see>
/// </para>
/// <para>For example, the <c>EXPLAIN</c> AND <c>VACUUM</c> statements with options might look like this:</para>
/// <code>
/// EXPLAIN (ANALYZE, VERBOSE TRUE, FORMAT TEXT) SELECT * FROM my_table;
///
/// VACUUM (VERBOSE, ANALYZE ON, PARALLEL 10) my_table;
/// </code>
/// </remarks>
/// <param name="Name">The option name (identifier).</param>
/// <param name="Arg">Optional argument for the option (number, string, keyword, etc.).</param>
public sealed partial record UtilityOption(Ident Name, Expr? Arg) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (Arg is { } arg)
        {
            f.Write($"{Name} {arg}");
        }
        else
        {
            f.Write($"{Name}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
