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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/visitor.rs (linhas 323–495: pub trait VisitorMut).
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// A visitor that can be used to mutate an AST tree.
/// </summary>
/// <typeparam name="TBreak">
/// Type returned when the recursion returns early.
/// </typeparam>
/// <remarks>
/// <para>
/// <c>pre_visit_</c> methods are invoked before visiting all children of the
/// node and <c>post_visit_</c> methods are invoked after visiting all
/// children of the node.
/// </para>
/// <para><b>See also</b></para>
/// <para>These methods provide a more concise way of visiting nodes of a certain type:</para>
/// <list type="bullet">
/// <item><see cref="Visitor.VisitRelationsMut{V, E}"/></item>
/// <item><see cref="Visitor.VisitExpressionsMut{V, E}"/></item>
/// <item><see cref="Visitor.VisitStatementsMut{V, E}"/></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // A visitor that replaces "to_replace" with "replaced" in all expressions
/// sealed class Replacer : VisitorMut&lt;ValueTuple&gt;
/// {
///     // Visit each expression after its children have been visited
///     public override ControlFlow&lt;ValueTuple&gt; PostVisitExpr(ref Expr expr)
///     {
///         if (expr is Expr.Identifier { Value: var ident })
///         {
///             expr = new Expr.Identifier(ident with { Value = ident.Value.Replace("to_replace", "replaced") });
///         }
///         return ControlFlow&lt;ValueTuple&gt;.Continue;
///     }
/// }
///
/// var sql = "SELECT to_replace FROM foo where to_replace IN (SELECT to_replace FROM bar)";
/// var statements = Parser.ParseSql(new GenericDialect(), sql);
///
/// // Drive the visitor through the AST
/// statements.Visit(new Replacer(), out statements);
///
/// // statements[0].ToString() == "SELECT replaced FROM foo WHERE replaced IN (SELECT replaced FROM bar)"
/// </code>
/// </example>
// O `&mut T` dos ganchos vira `ref T`: como a AST é imutável, o gancho altera um nó trocando-o
// (`expr = expr with { .. }` ou um nó novo), e a visita reconstrói os pais com `with`
// (ver IVisitMut<TSelf>). Os filhos visitados depois do gancho `pre_` são os do nó novo,
// como no Rust.
// TBreak: Rust `type Break` (`()` → ValueTuple).
public abstract class VisitorMut<TBreak>
{
    /// <summary>Invoked for any queries that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitQuery(ref Query query) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any queries that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitQuery(ref Query query) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <see cref="Ast.Select"/> that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitSelect(ref Select select) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <see cref="Ast.Select"/> that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitSelect(ref Select select) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any relations (e.g. tables) that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitRelation(ref ObjectName relation) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any relations (e.g. tables) that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitRelation(ref ObjectName relation) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any table factors that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitTableFactor(ref TableFactor tableFactor) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any table factors that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitTableFactor(ref TableFactor tableFactor) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any expressions that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitExpr(ref Expr expr) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any expressions that appear in the AST</summary>
    public virtual ControlFlow<TBreak> PostVisitExpr(ref Expr expr) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any statements that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitStatement(ref Statement statement) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any statements that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitStatement(ref Statement statement) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any value that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitValue(ref ValueWithSpan value) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any statements that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitValue(ref ValueWithSpan value) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any identifiers that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitIdent(ref Ident ident) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any identifiers that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitIdent(ref Ident ident) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>ORDER BY</c> clauses that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitOrderBy(ref OrderBy orderBy) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>ORDER BY</c> clauses that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitOrderBy(ref OrderBy orderBy) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>ORDER BY</c> expressions that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitOrderByExpr(ref OrderByExpr orderByExpr) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>ORDER BY</c> expressions that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitOrderByExpr(ref OrderByExpr orderByExpr) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>GROUP BY</c> clauses that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitGroupBy(ref GroupByExpr groupBy) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>GROUP BY</c> clauses that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitGroupBy(ref GroupByExpr groupBy) => ControlFlow<TBreak>.Continue;
}
