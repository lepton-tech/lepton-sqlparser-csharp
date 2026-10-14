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
/// <c>RenameTableNameKind</c> is the kind used in an <c>ALTER TABLE _ RENAME</c> statement.
/// <para>Note: <see href="https://dev.mysql.com/doc/refman/8.4/en/alter-table.html">MySQL</see> is the only database that supports the AS keyword for this operation.</para>
/// </summary>
public abstract record RenameTableNameKind : IDisplay, ISpanned
{
    /// <summary><c>AS new_table_name</c></summary>
    public sealed record As(ObjectName Value) : RenameTableNameKind;

    /// <summary><c>TO new_table_name</c></summary>
    public sealed record To(ObjectName Value) : RenameTableNameKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case As { Value: var name }:
                f.Write($"AS {name}");
                break;
            case To { Value: var name }:
                f.Write($"TO {name}");
                break;
            default:
                throw new InvalidOperationException($"Variante de RenameTableNameKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);

    // impl Spanned for RenameTableNameKind (ddl.rs:3989)
    public Span Span() =>
        this switch
        {
            As name => name.Value.Span(),
            To name => name.Value.Span(),
            _ => throw new InvalidOperationException($"Variante de RenameTableNameKind não tratada: {GetType().Name}"),
        };
}
