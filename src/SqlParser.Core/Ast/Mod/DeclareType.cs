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

/// <summary>Represents the type of a <c>DECLARE</c> statement.</summary>
public abstract partial record DeclareType : IDisplay
{
    /// <summary>
    /// Cursor variable type. e.g. [Snowflake] [PostgreSQL] [MsSql]
    /// <para>[Snowflake]: https://docs.snowflake.com/en/developer-guide/snowflake-scripting/cursors#declaring-a-cursor</para>
    /// <para>[PostgreSQL]: https://www.postgresql.org/docs/current/plpgsql-cursors.html</para>
    /// <para>[MsSql]: https://learn.microsoft.com/en-us/sql/t-sql/language-elements/declare-cursor-transact-sql</para>
    /// </summary>
    public sealed record Cursor : DeclareType;

    /// <summary>
    /// Result set variable type. [Snowflake]
    /// <para>Syntax:</para>
    /// <code>
    /// &lt;resultset_name&gt; RESULTSET [ { DEFAULT | := } ( &lt;query&gt; ) ] ;
    /// </code>
    /// <para>[Snowflake]: https://docs.snowflake.com/en/sql-reference/snowflake-scripting/declare#resultset-declaration-syntax</para>
    /// </summary>
    public sealed record ResultSet : DeclareType;

    /// <summary>
    /// Exception declaration syntax. [Snowflake]
    /// <para>Syntax:</para>
    /// <code>
    /// &lt;exception_name&gt; EXCEPTION [ ( &lt;exception_number&gt; , '&lt;exception_message&gt;' ) ] ;
    /// </code>
    /// <para>[Snowflake]: https://docs.snowflake.com/en/sql-reference/snowflake-scripting/declare#exception-declaration-syntax</para>
    /// </summary>
    public sealed record Exception : DeclareType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Cursor:
                f.Write($"CURSOR");
                break;
            case ResultSet:
                f.Write($"RESULTSET");
                break;
            case Exception:
                f.Write($"EXCEPTION");
                break;
            default:
                throw new InvalidOperationException($"Variante de DeclareType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
