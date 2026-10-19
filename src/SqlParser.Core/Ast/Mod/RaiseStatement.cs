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

/// <summary>A <c>RAISE</c> statement.</summary>
/// <remarks>
/// Examples:
/// <code>
/// RAISE USING MESSAGE = 'error';
///
/// RAISE myerror;
/// </code>
/// <para><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#raise">BigQuery</see></para>
/// <para><see href="https://docs.snowflake.com/en/sql-reference/snowflake-scripting/raise">Snowflake</see></para>
/// </remarks>
/// <param name="Value">Optional value provided to the RAISE statement.</param>
public sealed partial record RaiseStatement(RaiseStatementValue? Value) : IDisplay
{
    public void Fmt(Formatter f)
    {
        // let RaiseStatement { value } = self;
        var value = Value;

        f.Write($"RAISE");
        if (value is { } valueValue)
        {
            f.Write($" {valueValue}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
