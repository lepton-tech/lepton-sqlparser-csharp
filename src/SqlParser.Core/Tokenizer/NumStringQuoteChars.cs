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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/tokenizer.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core;

/// <summary>Represents how many quote characters enclose a string literal.</summary>
// Privado do módulo `tokenizer` no Rust → internal (convencoes.md §13).
internal abstract record NumStringQuoteChars
{
    /// <summary>e.g. <c>"abc"</c>, <c>'abc'</c>, <c>r'abc'</c></summary>
    public sealed record One : NumStringQuoteChars;

    /// <summary>e.g. <c>"""abc"""</c>, <c>'''abc'''</c>, <c>r'''abc'''</c></summary>
    // Rust: Many(NonZeroU8) → byte + validação (convencoes.md §5).
    public sealed record Many : NumStringQuoteChars
    {
        public Many(byte value)
        {
            if (value == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "NonZeroU8 não pode ser zero.");
            }
            Value = value;
        }

        /// <summary>Rust: o <c>NonZeroU8</c> (<c>count.get()</c>).</summary>
        public byte Value { get; }
    }
}
