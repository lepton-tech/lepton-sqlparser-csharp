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

namespace SqlParser.Core.Ast;

/// <summary>
/// Represents the different options available for <c>SHOW</c>
/// statements to filter the results. Example from Snowflake:
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/show-tables"/>
/// </summary>
/// <param name="ShowIn">Optional scope to show in (for example: TABLE, SCHEMA).</param>
/// <param name="StartsWith">Optional <c>STARTS WITH</c> filter value.</param>
/// <param name="Limit">Optional <c>LIMIT</c> expression.</param>
/// <param name="LimitFrom">Optional <c>FROM</c> value used with <c>LIMIT</c>.</param>
/// <param name="FilterPosition">Optional filter position (infix or suffix) for <c>LIKE</c>/<c>FILTER</c>.</param>
public sealed record ShowStatementOptions(
    ShowStatementIn? ShowIn,
    ValueWithSpan? StartsWith,
    Expr? Limit,
    ValueWithSpan? LimitFrom,
    ShowStatementFilterPosition? FilterPosition) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // O Rust monta cada pedaço com `format!` (String) antes do `write!`; aqui também:
        // `format!(" {x}")` → " " + Formatter.Format(x) (ver docs/port/onda-a/A4.md).
        string likeInInfix;
        string likeInSuffix;
        switch (FilterPosition)
        {
            case ShowStatementFilterPosition.Infix filter:
                (likeInInfix, likeInSuffix) = (" " + Formatter.Format(filter.Value), "");
                break;
            case ShowStatementFilterPosition.Suffix filter:
                (likeInInfix, likeInSuffix) = ("", " " + Formatter.Format(filter.Value));
                break;
            case null:
                (likeInInfix, likeInSuffix) = ("", "");
                break;
            default:
                throw new InvalidOperationException("Variante de ShowStatementFilterPosition não tratada");
        }
        var showIn = ShowIn switch
        {
            { } i => " " + Formatter.Format(i),
            null => "",
        };
        var startsWith = StartsWith switch
        {
            { } s => " STARTS WITH " + Formatter.Format(s),
            null => "",
        };
        var limit = Limit switch
        {
            { } l => " LIMIT " + Formatter.Format(l),
            null => "",
        };
        var from = LimitFrom switch
        {
            { } fr => " FROM " + Formatter.Format(fr),
            null => "",
        };
        f.Write($"{likeInInfix}{showIn}{startsWith}{limit}{from}{likeInSuffix}");
    }

    public override string ToString() => Formatter.Format(this);
}
