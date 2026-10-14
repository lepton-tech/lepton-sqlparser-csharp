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

namespace SqlParser.Core.Ast;

/// <summary>
/// Options for <c>CAST</c> / <c>TRY_CAST</c>
/// BigQuery: <see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/format-elements#formatting_syntax"/>
/// </summary>
public abstract partial record CastFormat : IDisplay
{
    /// <summary>A simple cast format specified by a <c>Value</c>.</summary>
    /// <param name="Inner">Rust: campo <c>.0</c> (variante chamada <c>Value</c>: parâmetro <c>Inner</c>, convencoes.md §6).</param>
    public sealed record Value(ValueWithSpan Inner) : CastFormat;

    /// <summary>A cast format with an explicit time zone: <c>(format, timezone)</c>.</summary>
    /// <param name="Format">Rust: campo <c>.0</c> (o formato).</param>
    /// <param name="TimeZone">Rust: campo <c>.1</c> (o fuso horário).</param>
    public sealed record ValueAtTimeZone(ValueWithSpan Format, ValueWithSpan TimeZone) : CastFormat;

    // Rust: `impl fmt::Display for CastFormat` fica em mod.rs depois de `OneOrManyWithParens`;
    // aqui vem junto do tipo (um tipo por arquivo).
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Value v:
                f.Write($"{v.Inner}");
                break;
            case ValueAtTimeZone valueAtTimeZone:
                f.Write($"{valueAtTimeZone.Format} AT TIME ZONE {valueAtTimeZone.TimeZone}");
                break;
            default:
                throw new InvalidOperationException($"Variante de CastFormat não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
