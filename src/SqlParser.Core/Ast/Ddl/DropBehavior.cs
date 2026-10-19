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
/// <c>&lt;drop behavior&gt; ::= CASCADE | RESTRICT</c>.
/// <para>Used in <c>DROP</c> statements.</para>
/// </summary>
// Enum sem dados com Display: records (convencoes.md §13, caso 37).
public abstract partial record DropBehavior : IDisplay
{
    /// <summary><c>RESTRICT</c> - refuse to drop if there are any dependent objects.</summary>
    public sealed record Restrict : DropBehavior;

    /// <summary><c>CASCADE</c> - automatically drop objects that depend on the object being dropped.</summary>
    public sealed record Cascade : DropBehavior;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            Restrict => "RESTRICT",
            Cascade => "CASCADE",
            _ => throw new InvalidOperationException($"Variante de DropBehavior não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
