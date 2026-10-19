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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Tags option of column
/// <para>Syntax</para>
/// <code>
/// [ WITH ] TAG ( &lt;tag_name&gt; = '&lt;tag_value&gt;' [ , &lt;tag_name&gt; = '&lt;tag_value&gt;' , ... ] )
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
/// </summary>
/// <param name="With">
/// This flag indicates that the tags option is declared using the <c>WITH</c> prefix.
/// Example:
/// <code>
/// WITH TAG (A = 'Tag A')
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-table">Snowflake</see>
/// </param>
/// <param name="Tags">List of tags to attach to the column.</param>
public sealed partial record TagsColumnOption(bool With, Sequence<Tag> Tags) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (With)
        {
            f.Write($"WITH ");
        }
        f.Write($"TAG ({DisplayCommaSeparated(Tags)})");
    }

    public override string ToString() => Formatter.Format(this);
}
