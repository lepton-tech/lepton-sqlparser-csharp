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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>A SET configuration_parameter clause in a CREATE FUNCTION statement.</summary>
/// <remarks><see href="https://www.postgresql.org/docs/current/sql-createfunction.html">PostgreSQL</see></remarks>
/// <param name="Name">The name of the configuration parameter.</param>
/// <param name="Value">The value to set for the parameter.</param>
public sealed partial record FunctionDefinitionSetParam(ObjectName Name, FunctionSetValue Value) : IDisplay
{
    public void Fmt(Formatter f)
    {
        f.Write($"SET {Name} ");
        switch (Value)
        {
            case FunctionSetValue.Default:
                f.Write($"= DEFAULT");
                break;
            case FunctionSetValue.Values values:
                f.Write($"= {DisplayCommaSeparated(values.Value)}");
                break;
            case FunctionSetValue.FromCurrent:
                f.Write($"FROM CURRENT");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionSetValue não tratada: {Value.GetType().Name}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
