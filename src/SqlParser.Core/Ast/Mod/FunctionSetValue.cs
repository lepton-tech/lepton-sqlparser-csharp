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

/// <summary>Value for a SET configuration parameter in a CREATE FUNCTION statement.</summary>
/// <remarks>
/// <see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see>
/// <para>Sem <c>impl Display</c> no Rust: quem formata é <see cref="FunctionDefinitionSetParam"/>.</para>
/// </remarks>
public abstract partial record FunctionSetValue
{
    /// <summary>SET param = DEFAULT / SET param TO DEFAULT</summary>
    public sealed record Default : FunctionSetValue;

    /// <summary>SET param = value1, value2, ...</summary>
    public sealed record Values(Sequence<Expr> Value) : FunctionSetValue;

    /// <summary>SET param FROM CURRENT</summary>
    public sealed record FromCurrent : FunctionSetValue;
}
