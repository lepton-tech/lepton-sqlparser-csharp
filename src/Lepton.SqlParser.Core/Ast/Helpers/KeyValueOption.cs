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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/key_value_options.rs.
// Modificado: tradução de Rust para C#.

using static Lepton.SqlParser.Core.Ast.AstModule;

namespace Lepton.SqlParser.Core.Ast;

/// <summary>A single key-value option.</summary>
/// <param name="OptionName">The name of the option.</param>
/// <param name="OptionValue">The value of the option.</param>
public sealed partial record KeyValueOption(string OptionName, KeyValueOptionKind OptionValue) : IDisplay
{
    public void Fmt(Formatter f)
    {
        switch (OptionValue)
        {
            case KeyValueOptionKind.Single value:
                f.Write($"{OptionName}={value.Value}");
                break;
            case KeyValueOptionKind.Multi values:
                f.Write($"{OptionName}=({DisplayCommaSeparated(values.Value)})");
                break;
            case KeyValueOptionKind.KeyValueOptions options:
                f.Write($"{OptionName}=({options.Value})");
                break;
            default:
                throw new InvalidOperationException($"Variante de KeyValueOptionKind não tratada: {OptionValue.GetType().Name}");
        }
    }

    public override string ToString() => Formatter.Format(this);
}
