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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/value.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

// Rust: `pub struct EscapeEscapedStringLiteral<'a>(&'a str)`, criado por `Value.EscapeEscapedString`.
// O campo `.0` do Rust é privado: campo `_value` e construtor `internal` (convencoes.md §13, casos 20, 25 e 35).
public sealed class EscapeEscapedStringLiteral : IDisplay
{
    private readonly string _value;

    internal EscapeEscapedStringLiteral(string value) => _value = value;

    public void Fmt(Formatter f)
    {
        // Só caracteres ASCII recebem tratamento especial, então percorrer unidades UTF-16 dá a mesma
        // saída que percorrer os `chars()` do Rust (as metades de um par saem juntas, em ordem).
        foreach (var c in _value)
        {
            switch (c)
            {
                case '\'':
                    f.WriteStr(@"\'");
                    break;
                case '\\':
                    f.WriteStr(@"\\");
                    break;
                case '\n':
                    f.WriteStr(@"\n");
                    break;
                case '\t':
                    f.WriteStr(@"\t");
                    break;
                case '\r':
                    f.WriteStr(@"\r");
                    break;
                default:
                    f.Write($"{c}");
                    break;
            }
        }
    }

    public override string ToString() => Formatter.Format(this);
}
