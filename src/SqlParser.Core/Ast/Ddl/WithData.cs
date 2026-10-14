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

namespace SqlParser.Core.Ast;

/// <summary>
/// <c>WITH DATA</c> clause on <c>CREATE TABLE ... AS</c> statement.
/// <para><see href="https://docs.teradata.com/r/Enterprise_IntelliFlex_VMware/SQL-Data-Definition-Language-Syntax-and-Examples/Table-Statements/CREATE-TABLE-and-CREATE-TABLE-AS/Syntax-Elements/AS_clause/WITH-Clause-Phrase">Teradata</see></para>
/// </summary>
/// <param name="Data"><c>true</c> for <c>WITH DATA</c>, <c>false</c> for <c>WITH NO DATA</c>.</param>
/// <param name="Statistics">
/// <c>Some(true)</c> for <c>AND STATISTICS</c>, <c>Some(false)</c> for <c>AND NO STATISTICS</c>,
/// <c>None</c> if the <c>AND [NO] STATISTICS</c> sub-clause is omitted.
/// </param>
public sealed record WithData(bool Data, bool? Statistics) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.WriteStr("WITH ");
        if (!Data)
        {
            f.WriteStr("NO ");
        }
        f.WriteStr("DATA");
        if (Statistics is { } stats)
        {
            f.WriteStr(" AND ");
            if (!stats)
            {
                f.WriteStr("NO ");
            }
            f.WriteStr("STATISTICS");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
