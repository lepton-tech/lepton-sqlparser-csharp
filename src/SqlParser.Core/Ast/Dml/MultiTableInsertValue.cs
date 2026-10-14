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

namespace SqlParser.Core.Ast;

/// <summary>A value in a multi-table INSERT VALUES clause.</summary>
public abstract record MultiTableInsertValue : IDisplay
{
    /// <summary>A column reference or expression from the source</summary>
    public sealed record Expr(Ast.Expr Value) : MultiTableInsertValue;

    /// <summary>The DEFAULT keyword</summary>
    public sealed record Default : MultiTableInsertValue;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Expr expr:
                f.Write($"{expr.Value}");
                break;
            case Default:
                f.Write($"DEFAULT");
                break;
            default:
                throw new InvalidOperationException($"Variante de MultiTableInsertValue não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
