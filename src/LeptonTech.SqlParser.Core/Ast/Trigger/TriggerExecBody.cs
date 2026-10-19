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

/// <summary>
/// This keyword immediately precedes the declaration of one or two relation names that provide access to the
/// transition relations of the triggering statement
/// </summary>
/// <param name="ExecType">Whether the body is a <c>FUNCTION</c> or <c>PROCEDURE</c> invocation.</param>
/// <param name="FuncDesc">Description of the function/procedure to execute.</param>
public sealed partial record TriggerExecBody(TriggerExecBodyType ExecType, FunctionDesc FuncDesc) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"{ExecType} {FuncDesc}");
    }

    public override string ToString() => Formatter.Format(this);
}
