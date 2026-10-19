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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/dialect/mod.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Dialects;

/// <summary>
/// Operators for which precedence must be defined.
/// <para>
/// Higher number -&gt; higher precedence.
/// See expression parsing for how these values are used.
/// </para>
/// </summary>
// #[derive(Debug, Clone, Copy)]: enum sem dados → enum C# (convencoes.md §6).
public enum Precedence
{
    /// <summary>Member access operator <c>.</c> (highest precedence).</summary>
    Period,
    /// <summary>Postgres style type cast <c>::</c>.</summary>
    DoubleColon,
    /// <summary>Timezone operator (e.g. <c>AT TIME ZONE</c>).</summary>
    AtTz,
    /// <summary>Multiplication / Division / Modulo operators (<c>*</c>, <c>/</c>, <c>%</c>).</summary>
    MulDivModOp,
    /// <summary>Addition / Subtraction (<c>+</c>, <c>-</c>).</summary>
    PlusMinus,
    /// <summary>Bitwise <c>XOR</c> operator (<c>^</c>).</summary>
    Xor,
    /// <summary>Bitwise <c>AND</c> operator (<c>&amp;</c>).</summary>
    Ampersand,
    /// <summary>Bitwise <c>CARET</c> (^) for some dialects.</summary>
    Caret,
    /// <summary>Bitwise <c>OR</c> / pipe operator (<c>|</c>).</summary>
    Pipe,
    /// <summary><c>:</c> operator for json/variant access.</summary>
    Colon,
    /// <summary><c>BETWEEN</c> operator.</summary>
    Between,
    /// <summary>Equality operator (<c>=</c>).</summary>
    Eq,
    /// <summary>Pattern matching (<c>LIKE</c>).</summary>
    Like,
    /// <summary><c>IS</c> operator (e.g. <c>IS NULL</c>).</summary>
    Is,
    /// <summary>Other Postgres-specific operators.</summary>
    PgOther,
    /// <summary>Unary <c>NOT</c>.</summary>
    UnaryNot,
    /// <summary>Logical <c>AND</c>.</summary>
    And,
    /// <summary>Logical <c>OR</c> (lowest precedence).</summary>
    Or,
}
