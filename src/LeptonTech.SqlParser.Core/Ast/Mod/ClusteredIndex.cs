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

/// <summary>A clustered index column specification.</summary>
/// <param name="Name">Column identifier for the clustered index entry.</param>
/// <param name="Asc">
/// Optional sort direction: <c>Some(true)</c> for ASC, <c>Some(false)</c> for DESC, <c>None</c> for unspecified.
/// </param>
public sealed partial record ClusteredIndex(Ident Name, bool? Asc) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{Name}");
        switch (Asc)
        {
            case true:
                f.Write($" ASC");
                break;
            case false:
                f.Write($" DESC");
                break;
            default:
                break;
        }
    }

    public override string ToString() => Formatter.Format(this);
}
