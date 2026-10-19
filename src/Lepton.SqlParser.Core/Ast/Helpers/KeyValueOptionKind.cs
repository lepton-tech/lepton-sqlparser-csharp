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

namespace Lepton.SqlParser.Core.Ast;

/// <summary>The kind of value for a key-value option.</summary>
/// <remarks>
/// An option can have a single value, multiple values or a nested list of values.
/// <para>
/// A value can be numeric, boolean, etc. Enum-style values are represented
/// as Value::Placeholder. For example: MFA_METHOD=SMS will be represented as
/// <c>Value::Placeholder("SMS".to_string)</c>.
/// </para>
/// </remarks>
// Sem `Display` no Rust (o texto é montado por `KeyValueOption`).
public abstract partial record KeyValueOptionKind
{
    /// <summary>A single value.</summary>
    public sealed record Single(ValueWithSpan Value) : KeyValueOptionKind;

    /// <summary>Multiple values.</summary>
    public sealed record Multi(Sequence<ValueWithSpan> Value) : KeyValueOptionKind;

    /// <summary>A nested list of key-value options.</summary>
    public sealed record KeyValueOptions(Ast.KeyValueOptions Value) : KeyValueOptionKind;
}
