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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Joins a table to itself to process hierarchical data in the table.</summary>
/// <remarks>
/// See <see href="https://docs.snowflake.com/en/sql-reference/constructs/connect-by"/>.
/// See <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Hierarchical-Queries.html"/>
/// </remarks>
public abstract partial record ConnectByKind : IDisplay
{
    /// <summary>CONNECT BY</summary>
    /// <param name="ConnectToken">the <c>CONNECT</c> token</param>
    /// <param name="Nocycle">
    /// [CONNECT BY] NOCYCLE
    /// <para>Optional on <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Hierarchical-Queries.html#GUID-0118DF1D-B9A9-41EB-8556-C6E7D6A5A84E__GUID-5377971A-F518-47E4-8781-F06FEB3EF993">Oracle</see></para>
    /// </param>
    /// <param name="Relationships">join conditions denoting the hierarchical relationship</param>
    public sealed record ConnectBy(AttachedToken ConnectToken, bool Nocycle, Sequence<Expr> Relationships) : ConnectByKind;

    /// <summary>
    /// START WITH
    /// <para>
    /// Optional on <see href="https://docs.oracle.com/en/database/oracle/oracle-database/21/sqlrf/Hierarchical-Queries.html#GUID-0118DF1D-B9A9-41EB-8556-C6E7D6A5A84E">Oracle</see>
    /// when comming <i>after</i> the <c>CONNECT BY</c>.
    /// </para>
    /// </summary>
    /// <param name="StartToken">the <c>START</c> token</param>
    /// <param name="Condition">condition selecting the root rows of the hierarchy</param>
    public sealed record StartWith(AttachedToken StartToken, Expr Condition) : ConnectByKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case ConnectBy { Nocycle: var nocycle, Relationships: var relationships }:
                f.Write($"CONNECT BY {(nocycle ? "NOCYCLE " : "")}{DisplayCommaSeparated(relationships)}");
                break;
            case StartWith { Condition: var condition }:
                f.Write($"START WITH {condition}");
                break;
            default:
                throw new InvalidOperationException($"Variante de ConnectByKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
