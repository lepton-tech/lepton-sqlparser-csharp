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
/// CREATE EXTENSION statement
/// Note: this is a PostgreSQL-specific statement
/// </summary>
/// <param name="Name">Extension name</param>
/// <param name="IfNotExists">Whether <c>IF NOT EXISTS</c> was specified for the CREATE EXTENSION.</param>
/// <param name="Cascade">Whether <c>CASCADE</c> was specified for the CREATE EXTENSION.</param>
/// <param name="Schema">Optional schema name for the extension.</param>
/// <param name="Version">Optional version for the extension.</param>
public sealed record CreateExtension(
    Ident Name,
    bool IfNotExists,
    bool Cascade,
    Ident? Schema,
    Ident? Version) : IDisplay, ISpanned
{
    public void Fmt(Formatter f)
    {
        var ifNotExists = IfNotExists ? "IF NOT EXISTS " : "";
        f.Write($"CREATE EXTENSION {ifNotExists}{Name}");
        if (Cascade || Schema is not null || Version is not null)
        {
            f.Write($" WITH");

            if (Schema is { } name)
            {
                f.Write($" SCHEMA {name}");
            }
            if (Version is { } version)
            {
                f.Write($" VERSION {version}");
            }
            if (Cascade)
            {
                f.Write($" CASCADE");
            }
        }
    }

    public override string ToString() => Formatter.Format(this);

    // impl Spanned for CreateExtension (ddl.rs:4545)
    public Span Span() => Core.Span.Empty();
}
