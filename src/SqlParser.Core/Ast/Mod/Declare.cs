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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>
/// A <c>DECLARE</c> statement.
/// [PostgreSQL] [Snowflake] [BigQuery]
/// </summary>
/// <remarks>
/// Examples:
/// <code>
/// DECLARE variable_name := 42
/// DECLARE liahona CURSOR FOR SELECT * FROM films;
/// </code>
/// <para>[PostgreSQL]: https://www.postgresql.org/docs/current/sql-declare.html</para>
/// <para>[Snowflake]: https://docs.snowflake.com/en/sql-reference/snowflake-scripting/declare</para>
/// <para>[BigQuery]: https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#declare</para>
/// </remarks>
/// <param name="Names">
/// The name(s) being declared.
/// Example: <c>DECLARE a, b, c DEFAULT 42;</c>
/// </param>
/// <param name="DataType">
/// Data-type assigned to the declared variable.
/// Example: <c>DECLARE x INT64 DEFAULT 42;</c>
/// </param>
/// <param name="Assignment">Expression being assigned to the declared variable.</param>
/// <param name="DeclareType">Represents the type of the declared variable.</param>
/// <param name="Binary">Causes the cursor to return data in binary rather than in text format.</param>
/// <param name="Sensitive">
/// None = Not specified
/// Some(true) = INSENSITIVE
/// Some(false) = ASENSITIVE
/// </param>
/// <param name="Scroll">
/// None = Not specified
/// Some(true) = SCROLL
/// Some(false) = NO SCROLL
/// </param>
/// <param name="Hold">
/// None = Not specified
/// Some(true) = WITH HOLD, specifies that the cursor can continue to be used after the transaction that created it successfully commits
/// Some(false) = WITHOUT HOLD, specifies that the cursor cannot be used outside of the transaction that created it
/// </param>
/// <param name="ForQuery"><c>FOR &lt;query&gt;</c> clause in a CURSOR declaration.</param>
public sealed record Declare(
    Sequence<Ident> Names,
    DataType? DataType,
    DeclareAssignment? Assignment,
    DeclareType? DeclareType,
    bool? Binary,
    bool? Sensitive,
    bool? Scroll,
    bool? Hold,
    Query? ForQuery) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // let Declare { names, data_type, assignment, declare_type, binary, sensitive, scroll, hold, for_query } = self;
        var names = Names;
        var dataType = DataType;
        var assignment = Assignment;
        var declareType = DeclareType;
        var binary = Binary;
        var sensitive = Sensitive;
        var scroll = Scroll;
        var hold = Hold;
        var forQuery = ForQuery;
        f.Write($"{DisplayCommaSeparated(names)}");

        if (binary is true)
        {
            f.Write($" BINARY");
        }

        if (sensitive is { } sensitiveValue)
        {
            if (sensitiveValue)
            {
                f.Write($" INSENSITIVE");
            }
            else
            {
                f.Write($" ASENSITIVE");
            }
        }

        if (scroll is { } scrollValue)
        {
            if (scrollValue)
            {
                f.Write($" SCROLL");
            }
            else
            {
                f.Write($" NO SCROLL");
            }
        }

        if (declareType is { } declareTypeValue)
        {
            f.Write($" {declareTypeValue}");
        }

        if (hold is { } holdValue)
        {
            if (holdValue)
            {
                f.Write($" WITH HOLD");
            }
            else
            {
                f.Write($" WITHOUT HOLD");
            }
        }

        if (forQuery is { } query)
        {
            f.Write($" FOR {query}");
        }

        if (dataType is { } dataTypeValue)
        {
            f.Write($" {dataTypeValue}");
        }

        if (assignment is { } expr)
        {
            f.Write($" {expr}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
