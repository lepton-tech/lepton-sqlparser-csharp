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
/// A constant of form <c>&lt;data_type&gt; 'value'</c>.
/// This can represent ANSI SQL <c>DATE</c>, <c>TIME</c>, and <c>TIMESTAMP</c> literals (such as <c>DATE '2020-01-01'</c>),
/// as well as constants of other types (a non-standard PostgreSQL extension).
/// </summary>
/// <param name="DataType">The data type of the typed string (e.g. DATE, TIME, TIMESTAMP).</param>
/// <param name="Value">
/// The value of the constant.
/// Hint: you can unwrap the string value using <c>value.into_string()</c>.
/// </param>
/// <param name="UsesOdbcSyntax">
/// Flags whether this TypedString uses the
/// <see href="https://learn.microsoft.com/en-us/sql/odbc/reference/develop-app/date-time-and-timestamp-literals?view=sql-server-2017">ODBC syntax</see>.
/// <para>Example:</para>
/// <code>
/// -- An ODBC date literal:
/// SELECT {d '2025-07-16'}
/// -- This is equivalent to the standard ANSI SQL literal:
/// SELECT DATE '2025-07-16'
/// </code>
/// </param>
public sealed partial record TypedString(DataType DataType, ValueWithSpan Value, bool UsesOdbcSyntax) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var dataType = DataType;
        var value = Value;
        switch (UsesOdbcSyntax)
        {
            case false:
                f.Write($"{dataType}");
                f.Write($" {value}");
                break;
            case true:
            {
                var prefix = dataType switch
                {
                    Ast.DataType.Date => "d",
                    Ast.DataType.Time => "t",
                    Ast.DataType.Timestamp => "ts",
                    _ => "?",
                };
                f.Write($"{{{prefix} {value}}}");
                break;
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
