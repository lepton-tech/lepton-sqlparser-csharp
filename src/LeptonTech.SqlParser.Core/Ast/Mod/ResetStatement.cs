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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Resets a session parameter to its default value.
/// <code>
/// RESET { ALL | SESSION AUTHORIZATION | &lt;configuration_parameter&gt; }
/// </code>
/// </summary>
/// <param name="Reset">The reset action to perform (either <c>ALL</c> or a specific configuration parameter).</param>
// No Rust, o `impl Display for ResetStatement` vem depois do de `OptimizerHint`; aqui fica no próprio tipo.
public sealed partial record ResetStatement(Reset Reset) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (Reset)
        {
            case Ast.Reset.ALL:
                f.Write($"RESET ALL");
                break;
            case Ast.Reset.SessionAuthorization:
                f.Write($"RESET SESSION AUTHORIZATION");
                break;
            case Ast.Reset.ConfigurationParameter param:
                f.Write($"RESET {param.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de Reset não tratada: {Reset.GetType().Name}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
