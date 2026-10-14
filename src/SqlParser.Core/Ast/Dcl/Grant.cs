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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dcl.rs.
// Modificado: tradução de Rust para C#.

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>GRANT privileges ON objects TO grantees</summary>
/// <remarks><c>impl From&lt;Grant&gt; for Statement</c> → <c>Statement.From(Grant)</c> (<c>Dcl/Statement.Dcl.cs</c>).</remarks>
/// <param name="Privileges">Privileges being granted.</param>
/// <param name="Objects">Optional objects the privileges apply to.</param>
/// <param name="Grantees">List of grantees receiving the privileges.</param>
/// <param name="WithGrantOption">Whether <c>WITH GRANT OPTION</c> is present.</param>
/// <param name="AsGrantor">Optional <c>AS GRANTOR</c> identifier.</param>
/// <param name="GrantedBy">
/// Optional <c>GRANTED BY</c> identifier.
/// <para><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/dcl-statements">BigQuery</see></para>
/// </param>
/// <param name="CurrentGrants">
/// Optional <c>CURRENT GRANTS</c> modifier.
/// <para><see href="https://docs.snowflake.com/en/sql-reference/sql/grant-privilege">Snowflake</see></para>
/// </param>
public sealed record Grant(
    Privileges Privileges,
    GrantObjects? Objects,
    Sequence<Grantee> Grantees,
    bool WithGrantOption,
    Ident? AsGrantor,
    Ident? GrantedBy,
    CurrentGrantsKind? CurrentGrants) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"GRANT {Privileges}");
        if (Objects is { } objects)
        {
            f.Write($" ON {objects}");
        }
        f.Write($" TO {DisplayCommaSeparated(Grantees)}");
        // Printed in the order the parser reads them, so the output re-parses.
        if (WithGrantOption)
        {
            f.Write($" WITH GRANT OPTION");
        }
        if (CurrentGrants is { } currentGrants)
        {
            f.Write($" {currentGrants}");
        }
        if (AsGrantor is { } asGrantor)
        {
            f.Write($" AS {asGrantor}");
        }
        if (GrantedBy is { } grantedBy)
        {
            f.Write($" GRANTED BY {grantedBy}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
