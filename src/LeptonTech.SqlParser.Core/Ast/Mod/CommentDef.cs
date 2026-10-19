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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>Helper to indicate if a comment includes the <c>=</c> in the display form</summary>
public abstract partial record CommentDef : IDisplay
{
    /// <summary>
    /// Includes <c>=</c> when printing the comment, as <c>COMMENT = 'comment'</c>
    /// Does not include <c>=</c> when printing the comment, as <c>COMMENT 'comment'</c>
    /// </summary>
    public sealed record WithEq(string Value) : CommentDef;

    /// <summary>Comment variant that omits the <c>=</c> when displayed.</summary>
    public sealed record WithoutEq(string Value) : CommentDef;

    public void Fmt(Formatter f)
    {
        // Rust: um braço com padrão "ou" (`WithEq(comment) | WithoutEq(comment)`); em C# são
        // duas seções, porque a variável do padrão não pode vir de dois rótulos.
        switch (this)
        {
            case WithEq comment:
                f.Write($"{comment.Value}");
                break;
            case WithoutEq comment:
                f.Write($"{comment.Value}");
                break;
            default:
                throw new InvalidOperationException($"Variante de CommentDef não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
