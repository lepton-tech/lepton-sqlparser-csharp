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
/// RESET config option:
/// <list type="bullet">
/// <item>RESET <c>configuration_parameter</c></item>
/// <item>RESET ALL</item>
/// </list>
/// </summary>
/// <remarks>Sem <c>Display</c> no Rust (o texto é montado por <c>AlterRoleOperation</c>).</remarks>
public abstract partial record ResetConfig
{
    /// <summary>Reset all configuration parameters.</summary>
    public sealed record ALL : ResetConfig;

    /// <summary>Reset the named configuration parameter.</summary>
    public sealed record ConfigName(ObjectName Value) : ResetConfig;
}
