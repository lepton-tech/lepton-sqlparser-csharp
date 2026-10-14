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

/// <summary>Index column type.</summary>
/// <param name="Column">The indexed column expression.</param>
/// <param name="OperatorClass">Optional operator class (index operator name).</param>
public sealed partial record IndexColumn(OrderByExpr Column, ObjectName? OperatorClass) : IDisplay
{
    /// <summary>Rust: <c>impl From&lt;Ident&gt; for IndexColumn</c>.</summary>
    public static IndexColumn From(Ident c) => new IndexColumn(OrderByExpr.From(c), null);

    /// <summary>Rust: <c>impl&lt;'a&gt; From&lt;&amp;'a str&gt; for IndexColumn</c>.</summary>
    public static IndexColumn From(string c)
    {
        var ident = new Ident(c);
        return From(ident);
    }

    public void Fmt(Formatter f)
    {
        f.Write($"{Column}");
        if (OperatorClass is { } operatorClass)
        {
            f.Write($" {operatorClass}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
