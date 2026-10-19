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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// <c>&lt;referential_action&gt; =
/// { RESTRICT | CASCADE | SET NULL | NO ACTION | SET DEFAULT }</c>
/// <para>Used in foreign key constraints in <c>ON UPDATE</c> and <c>ON DELETE</c> options.</para>
/// </summary>
// Enum sem dados com Display: records (convencoes.md §13, caso 37).
public abstract partial record ReferentialAction : IDisplay
{
    /// <summary><c>RESTRICT</c> - disallow action if it would break referential integrity.</summary>
    public sealed record Restrict : ReferentialAction;

    /// <summary><c>CASCADE</c> - propagate the action to referencing rows.</summary>
    public sealed record Cascade : ReferentialAction;

    /// <summary><c>SET NULL</c> - set referencing columns to NULL.</summary>
    public sealed record SetNull : ReferentialAction;

    /// <summary><c>NO ACTION</c> - no action at the time; may be deferred.</summary>
    public sealed record NoAction : ReferentialAction;

    /// <summary><c>SET DEFAULT</c> - set referencing columns to their default values.</summary>
    public sealed record SetDefault : ReferentialAction;

    public void Fmt(Formatter f)
    {
        f.WriteStr(this switch
        {
            Restrict => "RESTRICT",
            Cascade => "CASCADE",
            SetNull => "SET NULL",
            NoAction => "NO ACTION",
            SetDefault => "SET DEFAULT",
            _ => throw new InvalidOperationException($"Variante de ReferentialAction não tratada: {GetType().Name}"),
        });
    }

    public sealed override string ToString() => Formatter.Format(this);
}
