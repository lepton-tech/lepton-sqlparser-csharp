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
/// <code>
/// CREATE [ OR REPLACE ] WAREHOUSE [ IF NOT EXISTS ] &lt;name&gt;
///   [ [ WITH ] &lt;property&gt; = &lt;value&gt; [ ... ] ]
/// </code>
/// Snowflake-specific statement to create a virtual warehouse.
/// </summary>
/// <remarks>See <see href="https://docs.snowflake.com/en/sql-reference/sql/create-warehouse"/></remarks>
/// <param name="OrReplace"><c>OR REPLACE</c> flag.</param>
/// <param name="IfNotExists"><c>IF NOT EXISTS</c> flag.</param>
/// <param name="Name">Warehouse name.</param>
/// <param name="Options">Warehouse properties and parameters (e.g. <c>WAREHOUSE_SIZE = 'XSMALL'</c>).</param>
public sealed record CreateWarehouse(bool OrReplace, bool IfNotExists, ObjectName Name, KeyValueOptions Options) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CREATE");
        if (OrReplace)
        {
            f.Write($" OR REPLACE");
        }
        f.Write($" WAREHOUSE");
        if (IfNotExists)
        {
            f.Write($" IF NOT EXISTS");
        }
        f.Write($" {Name}");
        if (!Options.Options.IsEmpty)
        {
            f.Write($" {Options}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
