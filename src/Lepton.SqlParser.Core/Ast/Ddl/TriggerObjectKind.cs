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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>Whether the syntax used for the trigger object (ROW or STATEMENT) is <c>FOR</c> or <c>FOR EACH</c>.</summary>
public abstract partial record TriggerObjectKind : IDisplay
{
    /// <summary>The <c>FOR</c> syntax is used.</summary>
    public sealed record For(TriggerObject Value) : TriggerObjectKind;

    /// <summary>The <c>FOR EACH</c> syntax is used.</summary>
    public sealed record ForEach(TriggerObject Value) : TriggerObjectKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case For { Value: var obj }:
                f.Write($"FOR {obj}");
                break;
            case ForEach { Value: var obj }:
                f.Write($"FOR EACH {obj}");
                break;
            default:
                throw new InvalidOperationException($"Variante de TriggerObjectKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
