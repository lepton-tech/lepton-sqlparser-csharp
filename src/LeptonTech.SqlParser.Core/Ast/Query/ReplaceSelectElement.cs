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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Syntax:
/// <code>
/// &lt;expr&gt; [AS] &lt;column_name&gt;
/// </code>
/// </summary>
/// <param name="Expr">Expression producing the replacement value.</param>
/// <param name="ColumnName">The target column name for the replacement.</param>
/// <param name="AsKeyword">Whether the <c>AS</c> keyword was present in the original syntax.</param>
public sealed partial record ReplaceSelectElement(Expr Expr, Ident ColumnName, bool AsKeyword) : IDisplay
{
    public void Fmt(Formatter f)
    {
        if (AsKeyword)
        {
            f.Write($"{Expr} AS {ColumnName}");
        }
        else
        {
            f.Write($"{Expr} {ColumnName}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
