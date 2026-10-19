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

/// <summary>Creates a user</summary>
/// <remarks>
/// Syntax:
/// <code>
/// CREATE [OR REPLACE] USER [IF NOT EXISTS] &lt;name&gt; [OPTIONS]
/// </code>
/// <see href="https://docs.snowflake.com/en/sql-reference/sql/create-user">Snowflake</see>
/// </remarks>
/// <param name="OrReplace">Replace existing user if present.</param>
/// <param name="IfNotExists">Only create the user if it does not already exist.</param>
/// <param name="Name">The name of the user to create.</param>
/// <param name="Options">Key/value options for user creation.</param>
/// <param name="WithTags">Whether tags are specified using <c>WITH TAG</c>.</param>
/// <param name="Tags">Tags for the user.</param>
public sealed partial record CreateUser(
    bool OrReplace,
    bool IfNotExists,
    Ident Name,
    KeyValueOptions Options,
    bool WithTags,
    KeyValueOptions Tags) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"CREATE");
        if (OrReplace)
        {
            f.Write($" OR REPLACE");
        }
        f.Write($" USER");
        if (IfNotExists)
        {
            f.Write($" IF NOT EXISTS");
        }
        f.Write($" {Name}");
        if (!Options.Options.IsEmpty)
        {
            f.Write($" {Options}");
        }
        if (!Tags.Options.IsEmpty)
        {
            if (WithTags)
            {
                f.Write($" WITH");
            }
            f.Write($" TAG ({Tags})");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
