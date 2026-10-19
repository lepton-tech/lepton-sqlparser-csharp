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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dml.rs.
// Modificado: tradução de Rust para C#.

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>An <c>OUTPUT</c> clause on <c>MERGE</c>, <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c> (MSSQL).</summary>
/// <remarks>
/// Example:
/// OUTPUT $action, deleted.* INTO dbo.temp_products;
/// <see href="https://learn.microsoft.com/en-us/sql/t-sql/queries/output-clause-transact-sql"/>
/// </remarks>
public abstract partial record OutputClause : IDisplay
{
    /// <summary><c>OUTPUT</c> clause</summary>
    /// <param name="OutputToken">The <c>OUTPUT</c> token that starts the sub-expression.</param>
    /// <param name="SelectItems">The select items to output</param>
    /// <param name="IntoTable">Optional <c>INTO</c> table to direct the output</param>
    public sealed record Output(AttachedToken OutputToken, Sequence<SelectItem> SelectItems, SelectInto? IntoTable) : OutputClause;

    /// <summary><c>RETURNING</c> clause</summary>
    /// <param name="ReturningToken">The <c>RETURNING</c> token that starts the sub-expression.</param>
    /// <param name="SelectItems">The select items to return</param>
    public sealed record Returning(AttachedToken ReturningToken, Sequence<SelectItem> SelectItems) : OutputClause;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Output output:
                f.WriteStr("OUTPUT ");
                DisplayCommaSeparated(output.SelectItems).Fmt(f);
                if (output.IntoTable is { } intoTable)
                {
                    f.WriteStr(" ");
                    intoTable.Fmt(f);
                }
                break;
            case Returning returning:
                f.WriteStr("RETURNING ");
                DisplayCommaSeparated(returning.SelectItems).Fmt(f);
                break;
            default:
                throw new InvalidOperationException($"Variante de OutputClause não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
