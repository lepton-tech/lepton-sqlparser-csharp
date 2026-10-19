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

namespace LeptonTech.SqlParser.Core.Ast;

/// <summary>
/// Represents an expression behind a wildcard expansion in a projection.
/// <c>SELECT T.* FROM T;</c>
/// </summary>
public abstract partial record SelectItemQualifiedWildcardKind : IDisplay
{
    /// <summary>
    /// Expression is an object name.
    /// e.g. <c>alias.*</c> or even <c>schema.table.*</c>
    /// </summary>
    public sealed record ObjectName(Ast.ObjectName Value) : SelectItemQualifiedWildcardKind;

    /// <summary>
    /// Select star on an arbitrary expression.
    /// e.g. <c>STRUCT&lt;STRING&gt;('foo').*</c>
    /// </summary>
    public sealed record Expr(Ast.Expr Value) : SelectItemQualifiedWildcardKind;

    // Rust: o `impl fmt::Display for SelectItemQualifiedWildcardKind` vem depois de `SelectItem` no arquivo.
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case ObjectName { Value: var objectName }:
                f.Write($"{objectName}.*");
                break;
            case Expr { Value: var expr }:
                f.Write($"{expr}.*");
                break;
            default:
                throw new InvalidOperationException($"Variante de SelectItemQualifiedWildcardKind não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
