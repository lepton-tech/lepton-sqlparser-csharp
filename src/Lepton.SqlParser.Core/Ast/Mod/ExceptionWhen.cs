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

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// A representation of a <c>WHEN</c> arm with all the identifiers catched and the statements to execute
/// for the arm.
/// </summary>
/// <remarks>
/// Snowflake: <see href="https://docs.snowflake.com/en/sql-reference/snowflake-scripting/exception"/>
/// BigQuery: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/procedural-language#beginexceptionend"/>
/// </remarks>
/// <param name="Idents">Identifiers that trigger this branch (error conditions).</param>
/// <param name="Statements">Statements to execute when the condition matches.</param>
public sealed partial record ExceptionWhen(Sequence<Ident> Idents, Sequence<Statement> Statements) : IDisplay
{
    public void Fmt(Formatter f)
    {
        var idents = DisplaySeparated(Idents, " OR ");
        f.Write($"WHEN {idents} THEN");

        if (!Statements.IsEmpty)
        {
            f.Write($" ");
            FormatStatementList(f, Statements);
        }
    }

    public override string ToString() => Formatter.Format(this);
}
