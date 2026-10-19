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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/visitor.rs (linhas 136–321: pub trait Visitor).
// Modificado: tradução de Rust para C#.

namespace Lepton.SqlParser.Core.Ast;

/// <summary>
/// A visitor that can be used to walk an AST tree.
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
/// <item><see cref="Visitor.VisitRelations{V, E}"/></item>
/// <item><see cref="Visitor.VisitExpressions{V, E}"/></item>
/// <item><see cref="Visitor.VisitStatements{V, E}"/></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // A structure that records statements and relations
/// sealed class V : Visitor&lt;ValueTuple&gt;
/// {
///     public List&lt;string&gt; Visited { get; } = new();
///
///     // Visit relations and exprs before children are visited (depth first walk)
///     // Note you can also visit statements and visit exprs after children have been visited
///     public override ControlFlow&lt;ValueTuple&gt; PreVisitRelation(ObjectName relation)
///     {
///         Visited.Add($"PRE: RELATION: {relation}");
///         return ControlFlow&lt;ValueTuple&gt;.Continue;
///     }
///
///     public override ControlFlow&lt;ValueTuple&gt; PreVisitExpr(Expr expr)
///     {
///         Visited.Add($"PRE: EXPR: {expr}");
///         return ControlFlow&lt;ValueTuple&gt;.Continue;
///     }
/// }
///
/// var sql = "SELECT a FROM foo where x IN (SELECT y FROM bar)";
/// var statements = Parser.ParseSql(new GenericDialect(), sql);
///
/// // Drive the visitor through the AST
/// var visitor = new V();
/// statements.Visit(visitor);
///
/// // The visitor has visited statements and expressions in pre-traversal order
/// // visitor.Visited ==
/// //   "PRE: EXPR: a",
/// //   "PRE: RELATION: foo",
/// //   "PRE: EXPR: x IN (SELECT y FROM bar)",
/// //   "PRE: EXPR: x",
/// //   "PRE: EXPR: y",
/// //   "PRE: RELATION: bar",
/// </code>
/// </example>
// Rust: `pub trait Visitor { type Break; .. }` → classe abstrata com ganchos `virtual` que devolvem
// `Continue` (convencoes.md §7: trait com implementações padrão). O tipo associado `Break` vira o
// parâmetro genérico TBreak (`()` → ValueTuple).
public abstract class Visitor<TBreak>
{
    /// <summary>Invoked for any queries that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitQuery(Query query) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any queries that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitQuery(Query query) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <see cref="Ast.Select"/> that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitSelect(Select select) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <see cref="Ast.Select"/> that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitSelect(Select select) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any relations (e.g. tables) that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitRelation(ObjectName relation) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any relations (e.g. tables) that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitRelation(ObjectName relation) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any table factors that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitTableFactor(TableFactor tableFactor) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any table factors that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitTableFactor(TableFactor tableFactor) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any expressions that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitExpr(Expr expr) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any expressions that appear in the AST</summary>
    public virtual ControlFlow<TBreak> PostVisitExpr(Expr expr) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any statements that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitStatement(Statement statement) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any statements that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitStatement(Statement statement) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any Value that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitValue(ValueWithSpan value) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any Value that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitValue(ValueWithSpan value) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any identifiers that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitIdent(Ident ident) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any identifiers that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitIdent(Ident ident) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>ORDER BY</c> clauses that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitOrderBy(OrderBy orderBy) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>ORDER BY</c> clauses that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitOrderBy(OrderBy orderBy) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>ORDER BY</c> expressions that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitOrderByExpr(OrderByExpr orderByExpr) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>ORDER BY</c> expressions that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitOrderByExpr(OrderByExpr orderByExpr) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>GROUP BY</c> clauses that appear in the AST before visiting children</summary>
    public virtual ControlFlow<TBreak> PreVisitGroupBy(GroupByExpr groupBy) => ControlFlow<TBreak>.Continue;

    /// <summary>Invoked for any <c>GROUP BY</c> clauses that appear in the AST after visiting children</summary>
    public virtual ControlFlow<TBreak> PostVisitGroupBy(GroupByExpr groupBy) => ControlFlow<TBreak>.Continue;
}
