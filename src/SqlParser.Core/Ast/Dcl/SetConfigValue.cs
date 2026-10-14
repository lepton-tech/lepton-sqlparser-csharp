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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/dcl.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// SET config value option:
/// <list type="bullet">
/// <item>SET <c>configuration_parameter</c> { TO | = } { <c>value</c> | DEFAULT }</item>
/// <item>SET <c>configuration_parameter</c> FROM CURRENT</item>
/// </list>
/// </summary>
/// <remarks>Sem <c>Display</c> no Rust (o texto é montado por <c>AlterRoleOperation</c>).</remarks>
public abstract partial record SetConfigValue
{
    /// <summary>Use the default value.</summary>
    public sealed record Default : SetConfigValue;

    /// <summary>Use the current value (<c>FROM CURRENT</c>).</summary>
    public sealed record FromCurrent : SetConfigValue;

    /// <summary>Set to the provided expression value.</summary>
    /// <remarks>Variante chamada <c>Value</c>: o parâmetro se chama <c>Inner</c> (convencoes.md §6; CS0542).</remarks>
    public sealed record Value(Expr Inner) : SetConfigValue;
}
