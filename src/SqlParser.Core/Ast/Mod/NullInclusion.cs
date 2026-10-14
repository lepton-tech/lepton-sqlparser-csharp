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
/// Specifies Include / Exclude NULL within UNPIVOT command.
/// For example
/// <c>UNPIVOT (column1 FOR new_column IN (col3, col4, col5, col6))</c>
/// </summary>
public abstract record NullInclusion : IDisplay
{
    /// <summary>Include NULL values in the UNPIVOT output.</summary>
    public sealed record IncludeNulls : NullInclusion;

    /// <summary>Exclude NULL values from the UNPIVOT output.</summary>
    public sealed record ExcludeNulls : NullInclusion;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case IncludeNulls:
                f.Write($"INCLUDE NULLS");
                break;
            case ExcludeNulls:
                f.Write($"EXCLUDE NULLS");
                break;
            default:
                throw new InvalidOperationException($"Variante de NullInclusion não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
