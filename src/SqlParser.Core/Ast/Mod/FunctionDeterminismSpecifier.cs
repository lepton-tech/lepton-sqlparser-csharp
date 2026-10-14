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

/// <summary>[BigQuery] Determinism specifier used in a UDF definition.</summary>
/// <remarks><see href="https://cloud.google.com/bigquery/docs/reference/standard-sql/data-definition-language#syntax_11">BigQuery</see></remarks>
public abstract record FunctionDeterminismSpecifier : IDisplay
{
    /// <summary>Function is deterministic.</summary>
    public sealed record Deterministic : FunctionDeterminismSpecifier;

    /// <summary>Function is not deterministic.</summary>
    public sealed record NotDeterministic : FunctionDeterminismSpecifier;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Deterministic:
                f.Write($"DETERMINISTIC");
                break;
            case NotDeterministic:
                f.Write($"NOT DETERMINISTIC");
                break;
            default:
                throw new InvalidOperationException($"Variante de FunctionDeterminismSpecifier não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
