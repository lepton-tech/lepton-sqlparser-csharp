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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/data_type.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Type of brackets used for <c>MAP</c> types.</summary>
// Enum sem dados e sem `impl`: `enum` C# (convencoes.md §6, regra 1).
public enum MapBracketKind
{
    /// <summary>Example: <c>Map(String, UInt16)</c></summary>
    Parentheses,

    /// <summary>Example: <c>MAP(VARCHAR, NUMBER NOT NULL)</c></summary>
    ParenthesesNotNull,

    /// <summary>Example: <c>MAP&lt;STRING, INT&gt;</c></summary>
    AngleBrackets,
}
