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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/value.rs (teste).
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Tests;

public class ValueTests
{
    // Rust:
    //   assert_eq!(format!("{}", escape_quoted_string("a🦀b🦀c", '🦀')), "a🦀🦀b🦀🦀c");
    // O caractere de aspas (U+1F980) está fora do BMP e não cabe num `char` C#; a assinatura
    // `Value.EscapeQuotedString(string, char)` não consegue recebê-lo (DIV-003).
    [Fact(Skip = "DIV-003: aspa fora do BMP não cabe em char (escape_quoted_string com '\U0001F980')")]
    public void test_escape_quoted_string_with_multibyte_quote_char()
    {
    }
}
