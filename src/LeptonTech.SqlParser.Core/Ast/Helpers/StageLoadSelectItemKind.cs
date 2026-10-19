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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/helpers/stmt_data_loading.rs.
// Modificado: tradução de Rust para C#.

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// This enum enables support for both standard SQL select item expressions
/// and Snowflake-specific ones for data loading.
/// </summary>
public abstract partial record StageLoadSelectItemKind : IDisplay
{
    /// <summary>A standard SQL select item expression.</summary>
    public sealed record SelectItem(Ast.SelectItem Value) : StageLoadSelectItemKind;

    /// <summary>A Snowflake-specific select item used for stage loading.</summary>
    public sealed record StageLoadSelectItem(Ast.StageLoadSelectItem Value) : StageLoadSelectItemKind;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case SelectItem item:
                f.Write($"{item.Value}");
                break;
            case StageLoadSelectItem item:
                f.Write($"{item.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de StageLoadSelectItemKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
