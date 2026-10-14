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

/// <summary>Fulltext search modifiers ([1]).</summary>
/// <remarks>[1]: <see href="https://dev.mysql.com/doc/refman/8.0/en/fulltext-search.html#function_match"/></remarks>
public abstract record SearchModifier : IDisplay
{
    /// <summary><c>IN NATURAL LANGUAGE MODE</c>.</summary>
    public sealed record InNaturalLanguageMode : SearchModifier;

    /// <summary><c>IN NATURAL LANGUAGE MODE WITH QUERY EXPANSION</c>.</summary>
    public sealed record InNaturalLanguageModeWithQueryExpansion : SearchModifier;

    /// <summary><c>IN BOOLEAN MODE</c>.</summary>
    public sealed record InBooleanMode : SearchModifier;

    /// <summary><c>WITH QUERY EXPANSION</c>.</summary>
    public sealed record WithQueryExpansion : SearchModifier;

    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case InNaturalLanguageMode:
                f.Write($"IN NATURAL LANGUAGE MODE");
                break;
            case InNaturalLanguageModeWithQueryExpansion:
                f.Write($"IN NATURAL LANGUAGE MODE WITH QUERY EXPANSION");
                break;
            case InBooleanMode:
                f.Write($"IN BOOLEAN MODE");
                break;
            case WithQueryExpansion:
                f.Write($"WITH QUERY EXPANSION");
                break;
            default:
                throw new InvalidOperationException($"Variante de SearchModifier não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
