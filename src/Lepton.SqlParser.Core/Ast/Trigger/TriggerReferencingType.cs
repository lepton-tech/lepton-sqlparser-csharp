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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// This clause indicates whether the following relation name is for the before-image transition relation
/// or the after-image transition relation
/// </summary>
public abstract partial record TriggerReferencingType : IDisplay
{
    /// <summary>The transition relation containing the old rows affected by the triggering statement</summary>
    public sealed record OldTable : TriggerReferencingType;

    /// <summary>The transition relation containing the new rows affected by the triggering statement</summary>
    public sealed record NewTable : TriggerReferencingType;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case OldTable:
                f.Write($"OLD TABLE");
                break;
            case NewTable:
                f.Write($"NEW TABLE");
                break;
            default:
                throw new InvalidOperationException($"Variante de TriggerReferencingType não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
