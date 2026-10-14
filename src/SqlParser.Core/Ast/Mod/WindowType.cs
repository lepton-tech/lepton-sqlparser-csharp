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

/// <summary>The type of a window used in <c>OVER</c> clauses.</summary>
/// <remarks>
/// A window can be either an inline specification (<c>WindowSpec</c>) or a
/// reference to a previously defined named window.
/// <list type="bullet">
/// <item><c>WindowSpec(WindowSpec)</c>: An inline window specification, e.g.
///   <c>OVER (PARTITION BY ... ORDER BY ...)</c>.</item>
/// <item><c>NamedWindow(Ident)</c>: A reference to a named window declared elsewhere.</item>
/// </list>
/// </remarks>
public abstract record WindowType : IDisplay
{
    /// <summary>An inline window specification.</summary>
    /// <param name="Value">Rust: campo <c>.0</c> (tipo qualificado: a variante tem o nome do tipo, convencoes.md §13, caso 38).</param>
    public sealed record WindowSpec(Ast.WindowSpec Value) : WindowType;

    /// <summary>A reference to a previously defined named window.</summary>
    /// <param name="Value">Rust: campo <c>.0</c>.</param>
    public sealed record NamedWindow(Ident Value) : WindowType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case WindowSpec spec:
                f.WriteStr("(");
                NewLine.Instance.Fmt(f);
                new Indent<Ast.WindowSpec>(spec.Value).Fmt(f);
                NewLine.Instance.Fmt(f);
                f.WriteStr(")");
                break;
            case NamedWindow name:
                name.Value.Fmt(f);
                break;
            default:
                throw new InvalidOperationException($"Variante de WindowType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
