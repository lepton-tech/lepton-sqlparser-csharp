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

using static SqlParser.Core.Ast.AstModule;

namespace SqlParser.Core.Ast;

/// <summary>One item of the comma-separated list following <c>SELECT</c></summary>
public abstract partial record SelectItem : IDisplay
{
    /// <summary>Any expression, not followed by <c>[ AS ] alias</c></summary>
    public sealed record UnnamedExpr(Expr Value) : SelectItem;

    /// <summary>An expression, followed by <c>[ AS ] alias</c></summary>
    /// <param name="Expr">The expression being projected.</param>
    /// <param name="Alias">The alias for the expression.</param>
    public sealed record ExprWithAlias(Expr Expr, Ident Alias) : SelectItem;

    /// <summary>
    /// An expression, followed by <c>[ AS ] (alias1, alias2, ...)</c>
    /// <para><see href="https://spark.apache.org/docs/latest/sql-ref-syntax-qry-select.html">Spark SQL</see></para>
    /// </summary>
    /// <param name="Expr">The expression being projected.</param>
    /// <param name="Aliases">The list of aliases for the expression.</param>
    public sealed record ExprWithAliases(Expr Expr, Sequence<Ident> Aliases) : SelectItem;

    /// <summary>
    /// An expression, followed by a wildcard expansion.
    /// e.g. <c>alias.*</c>, <c>STRUCT&lt;STRING&gt;('foo').*</c>
    /// </summary>
    /// <param name="Kind">Rust: campo <c>.0</c> (nomes.md).</param>
    /// <param name="AdditionalOptions">Rust: campo <c>.1</c> (nomes.md).</param>
    public sealed record QualifiedWildcard(SelectItemQualifiedWildcardKind Kind, WildcardAdditionalOptions AdditionalOptions) : SelectItem;

    /// <summary>An unqualified <c>*</c></summary>
    public sealed record Wildcard(WildcardAdditionalOptions Value) : SelectItem;

    // Rust: o `impl fmt::Display for SelectItem` vem depois de `ReplaceSelectElement` no arquivo.
    public void Fmt(Formatter f)
    {
        switch (this)
        {
            case UnnamedExpr { Value: var expr }:
                expr.Fmt(f);
                break;
            case ExprWithAlias { Expr: var expr, Alias: var alias }:
                expr.Fmt(f);
                f.WriteStr(" AS ");
                alias.Fmt(f);
                break;
            case ExprWithAliases { Expr: var expr, Aliases: var aliases }:
                expr.Fmt(f);
                f.WriteStr(" AS (");
                DisplayCommaSeparated(aliases).Fmt(f);
                f.WriteStr(")");
                break;
            case QualifiedWildcard { Kind: var kind, AdditionalOptions: var additionalOptions }:
                kind.Fmt(f);
                additionalOptions.Fmt(f);
                break;
            case Wildcard { Value: var additionalOptions }:
                f.WriteChar('*');
                additionalOptions.Fmt(f);
                break;
            default:
                throw new InvalidOperationException($"Variante de SelectItem não tratada: {GetType().Name}");
        }
    }

    public sealed override string ToString() => Formatter.Format(this);
}
