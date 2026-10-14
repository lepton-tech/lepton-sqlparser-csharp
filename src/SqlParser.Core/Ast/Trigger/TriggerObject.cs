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

namespace SqlParser.Core.Ast;

/// <summary>
/// This specifies whether the trigger function should be fired once for every row affected by the trigger event,
/// or just once per SQL statement.
/// </summary>
public abstract partial record TriggerObject : IDisplay
{
    /// <summary>The trigger fires once for each row affected by the triggering event</summary>
    public sealed record Row : TriggerObject;

    /// <summary>The trigger fires once for the triggering SQL statement</summary>
    public sealed record Statement : TriggerObject;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Row:
                f.Write($"ROW");
                break;
            case Statement:
                f.Write($"STATEMENT");
                break;
            default:
                throw new InvalidOperationException($"Variante de TriggerObject não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
