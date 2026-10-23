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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Separator written between the declarations collected by one <c>DECLARE</c> keyword.</summary>
public enum DeclareSeparator
{
    /// <summary>
    /// Each declaration ends with <c>;</c>, as in a Snowflake Scripting <c>DECLARE</c> section.
    /// <para><see href="https://docs.snowflake.com/en/sql-reference/snowflake-scripting/declare">Snowflake</see></para>
    /// </summary>
    Semicolon,

    /// <summary>
    /// One comma-separated list, as in <c>DECLARE @a INT, @b INT</c>.
    /// <para><see href="https://learn.microsoft.com/en-us/sql/t-sql/language-elements/declare-local-variable-transact-sql">MsSql</see></para>
    /// </summary>
    Comma,
}
