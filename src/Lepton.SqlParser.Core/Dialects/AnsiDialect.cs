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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/ansi.rs.
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Dialects;

/// <summary>
/// A <see cref="Dialect"/> for <see href="https://en.wikipedia.org/wiki/SQL:2011">ANSI SQL</see>.
/// </summary>
public class AnsiDialect : Dialect
{
    /// <summary>
    /// The SQL standard uses double quotes for delimited identifiers.
    /// </summary>
    public override char? IdentifierQuoteStyle(string identifier) => '"';

    public override bool IsIdentifierStart(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');

    public override bool IsIdentifierPart(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '_';

    public override bool RequireIntervalQualifier => true;

    /// <summary>
    /// The SQL standard explicitly states that block comments nest.
    /// </summary>
    public override bool SupportsNestedComments => true;
}
