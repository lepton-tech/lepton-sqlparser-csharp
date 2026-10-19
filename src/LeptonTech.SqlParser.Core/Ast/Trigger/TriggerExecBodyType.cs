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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/trigger.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Types of trigger body execution body.</summary>
public abstract partial record TriggerExecBodyType : IDisplay
{
    /// <summary>Execute a function</summary>
    public sealed record Function : TriggerExecBodyType;

    /// <summary>Execute a procedure</summary>
    public sealed record Procedure : TriggerExecBodyType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Function:
                f.Write($"FUNCTION");
                break;
            case Procedure:
                f.Write($"PROCEDURE");
                break;
            default:
                throw new InvalidOperationException($"Variante de TriggerExecBodyType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
