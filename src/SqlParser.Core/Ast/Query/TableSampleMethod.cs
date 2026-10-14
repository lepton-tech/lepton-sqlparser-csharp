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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/query.rs.
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>
/// The table sample method names
/// Sampling method used by <c>TABLESAMPLE</c>.
/// </summary>
/// <remarks>
/// Enum sem dados com <c>Display</c> → records (convencoes.md §13, caso 37). A variante <c>System</c>
/// esconde o namespace <c>System</c> dentro deste record: não use nomes qualificados <c>System.X</c> aqui.
/// </remarks>
public abstract partial record TableSampleMethod : IDisplay
{
    /// <summary><c>ROW</c> sampling method.</summary>
    public sealed record Row : TableSampleMethod;

    /// <summary><c>BERNOULLI</c> sampling method.</summary>
    public sealed record Bernoulli : TableSampleMethod;

    /// <summary><c>SYSTEM</c> sampling method.</summary>
    public sealed record System : TableSampleMethod;

    /// <summary><c>BLOCK</c> sampling method.</summary>
    public sealed record Block : TableSampleMethod;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case Bernoulli:
                f.Write($"BERNOULLI");
                break;
            case Row:
                f.Write($"ROW");
                break;
            case System:
                f.Write($"SYSTEM");
                break;
            case Block:
                f.Write($"BLOCK");
                break;
            default:
                throw new InvalidOperationException($"Variante de TableSampleMethod não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
