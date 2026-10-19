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

namespace SqlParser.Core.Ast;

/// <summary>The <c>FROM</c> clause of an <c>UPDATE TABLE</c> statement</summary>
// Sem `Display` no Rust: o tipo não implementa `IDisplay`.
public abstract partial record UpdateTableFromKind
{
    /// <summary>
    /// Update Statement where the 'FROM' clause is before the 'SET' keyword (Supported by Snowflake)
    /// For Example: <c>UPDATE FROM t1 SET t1.name='aaa'</c>
    /// </summary>
    public sealed record BeforeSet(Sequence<TableWithJoins> Value) : UpdateTableFromKind;

    /// <summary>
    /// Update Statement where the 'FROM' clause is after the 'SET' keyword (Which is the standard way)
    /// For Example: <c>UPDATE SET t1.name='aaa' FROM t1</c>
    /// </summary>
    public sealed record AfterSet(Sequence<TableWithJoins> Value) : UpdateTableFromKind;
}
