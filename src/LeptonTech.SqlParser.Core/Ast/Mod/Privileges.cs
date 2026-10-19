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

using static LeptonTech.SqlParser.Core.Ast.AstModule;

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Privileges granted in a GRANT statement or revoked in a REVOKE statement.</summary>
public abstract partial record Privileges : IDisplay
{
    /// <summary>All privileges applicable to the object type</summary>
    /// <param name="WithPrivilegesKeyword">Optional keyword from the spec, ignored in practice</param>
    public sealed record All(bool WithPrivilegesKeyword) : Privileges;

    /// <summary>Specific privileges (e.g. <c>SELECT</c>, <c>INSERT</c>)</summary>
    public sealed record Actions(Sequence<Action> Value) : Privileges;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case All all:
                f.Write($"ALL{(all.WithPrivilegesKeyword ? " PRIVILEGES" : "")}");
                break;
            case Actions actions:
                f.Write($"{DisplayCommaSeparated(actions.Value)}");
                break;
            default:
                throw new InvalidOperationException($"Variante de Privileges não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
