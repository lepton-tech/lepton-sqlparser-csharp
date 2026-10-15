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
// Portado para C# a partir de sqlparser-rs v0.63.0, src/ast/visitor.rs (linhas 497–800: RelationVisitor,
// visit_relations, visit_relations_mut, ExprVisitor, visit_expressions, visit_expressions_mut,
// StatementVisitor, visit_statements, visit_statements_mut).
// Modificado: tradução de Rust para C#.

namespace SqlParser.Core.Ast;

/// <summary>Free functions of <c>ast/visitor.rs</c>.</summary>
// Funções livres de `ast/visitor.rs` (convencoes.md §13, caso 5: tipo homônimo do módulo; a classe
// estática `Visitor` convive com Visitor<TBreak>, como `Sequence` e `Sequence<T>`).
// Cada `struct XVisitor<F>(F)` do Rust implementa `Visitor` e `VisitorMut`; uma classe C# não
// herda de duas classes, então cada uma vira duas classes privadas: `XVisitor<E>` (`Visitor`) e
// `XVisitorMut<E>` (`VisitorMut`) (nomes.md). As closures `FnMut(&T)` viram
// Func<T, TResult>, e `FnMut(&mut T)` viram RefFunc<T, TResult>.
public static class Visitor
{
    // struct RelationVisitor<F>(F);
    // impl<E, F: FnMut(&ObjectName) -> ControlFlow<E>> Visitor for RelationVisitor<F>
    private sealed class RelationVisitor<E> : Visitor<E>
    {
        private readonly Func<ObjectName, ControlFlow<E>> _f;

        public RelationVisitor(Func<ObjectName, ControlFlow<E>> f) => _f = f;

        public override ControlFlow<E> PreVisitRelation(ObjectName relation) => _f(relation);
    }

    // impl<E, F: FnMut(&mut ObjectName) -> ControlFlow<E>> VisitorMut for RelationVisitor<F>
    private sealed class RelationVisitorMut<E> : VisitorMut<E>
    {
        private readonly RefFunc<ObjectName, ControlFlow<E>> _f;

        public RelationVisitorMut(RefFunc<ObjectName, ControlFlow<E>> f) => _f = f;

        public override ControlFlow<E> PostVisitRelation(ref ObjectName relation) => _f(ref relation);
    }

    /// <summary>
    /// Invokes the provided closure on all relations (e.g. table names) present in <paramref name="v"/>
    /// </summary>
    /// <example>
    /// <code>
    /// var sql = "SELECT a FROM foo where x IN (SELECT y FROM bar)";
    /// var statements = Parser.ParseSql(new GenericDialect(), sql);
    ///
    /// // visit statements, capturing relations (table names)
    /// var visited = new List&lt;string&gt;();
    /// Visitor.VisitRelations(statements, relation =&gt;
    /// {
    ///     visited.Add($"RELATION: {relation}");
    ///     return ControlFlow&lt;ValueTuple&gt;.Continue;
    /// });
    ///
    /// // visited == ["RELATION: foo", "RELATION: bar"]
    /// </code>
    /// </example>
    public static ControlFlow<E> VisitRelations<V, E>(V v, Func<ObjectName, ControlFlow<E>> f)
        where V : IVisit
    {
        var visitor = new RelationVisitor<E>(f);
        var flow = v.Visit(visitor);
        if (flow.IsBreak)
        {
            return flow;
        }
        return ControlFlow<E>.Continue;
    }

    /// <summary>
    /// Invokes the provided closure with a mutable reference to all relations (e.g. table names)
    /// present in <paramref name="v"/>.
    /// </summary>
    /// <remarks>
    /// When the closure mutates its argument, the new mutated relation will not be visited again.
    /// </remarks>
    /// <example>
    /// <code>
    /// var sql = "SELECT a FROM foo";
    /// var statements = Parser.ParseSql(new GenericDialect(), sql);
    ///
    /// // visit statements, renaming table foo to bar
    /// Visitor.VisitRelationsMut(ref statements, (ref ObjectName table) =&gt;
    /// {
    ///     table = new ObjectName(Sequence.Of&lt;ObjectNamePart&gt;(new ObjectNamePart.Identifier(new Ident("bar"))));
    ///     return ControlFlow&lt;ValueTuple&gt;.Continue;
    /// });
    ///
    /// // statements[0].ToString() == "SELECT a FROM bar"
    /// </code>
    /// </example>
    public static ControlFlow<E> VisitRelationsMut<V, E>(ref V v, RefFunc<ObjectName, ControlFlow<E>> f)
        where V : IVisitMut<V>
    {
        var visitor = new RelationVisitorMut<E>(f);
        var flow = v.Visit(visitor, out v);
        if (flow.IsBreak)
        {
            return flow;
        }
        return ControlFlow<E>.Continue;
    }

    // struct ExprVisitor<F>(F);
    // impl<E, F: FnMut(&Expr) -> ControlFlow<E>> Visitor for ExprVisitor<F>
    private sealed class ExprVisitor<E> : Visitor<E>
    {
        private readonly Func<Expr, ControlFlow<E>> _f;

        public ExprVisitor(Func<Expr, ControlFlow<E>> f) => _f = f;

        public override ControlFlow<E> PreVisitExpr(Expr expr) => _f(expr);
    }

    // impl<E, F: FnMut(&mut Expr) -> ControlFlow<E>> VisitorMut for ExprVisitor<F>
    private sealed class ExprVisitorMut<E> : VisitorMut<E>
    {
        private readonly RefFunc<Expr, ControlFlow<E>> _f;

        public ExprVisitorMut(RefFunc<Expr, ControlFlow<E>> f) => _f = f;

        public override ControlFlow<E> PostVisitExpr(ref Expr expr) => _f(ref expr);
    }

    /// <summary>
    /// Invokes the provided closure on all expressions (e.g. <c>1 + 2</c>) present in <paramref name="v"/>
    /// </summary>
    /// <example>
    /// <code>
    /// var sql = "SELECT a FROM foo where x IN (SELECT y FROM bar)";
    /// var statements = Parser.ParseSql(new GenericDialect(), sql);
    ///
    /// // visit all expressions
    /// var visited = new List&lt;string&gt;();
    /// Visitor.VisitExpressions(statements, expr =&gt;
    /// {
    ///     visited.Add($"EXPR: {expr}");
    ///     return ControlFlow&lt;ValueTuple&gt;.Continue;
    /// });
    ///
    /// // visited == ["EXPR: a", "EXPR: x IN (SELECT y FROM bar)", "EXPR: x", "EXPR: y"]
    /// </code>
    /// </example>
    public static ControlFlow<E> VisitExpressions<V, E>(V v, Func<Expr, ControlFlow<E>> f)
        where V : IVisit
    {
        var visitor = new ExprVisitor<E>(f);
        var flow = v.Visit(visitor);
        if (flow.IsBreak)
        {
            return flow;
        }
        return ControlFlow<E>.Continue;
    }

    /// <summary>
    /// Invokes the provided closure iteratively with a mutable reference to all expressions
    /// present in <paramref name="v"/>.
    /// </summary>
    /// <remarks>
    /// This performs a depth-first search, so if the closure mutates the expression
    /// </remarks>
    /// <example>
    /// <para><b>Remove all select limits in sub-queries</b></para>
    /// <code>
    /// var sql = "SELECT (SELECT y FROM z LIMIT 9) FROM t LIMIT 3";
    /// var statements = Parser.ParseSql(new GenericDialect(), sql);
    ///
    /// // Remove all select limits in sub-queries
    /// Visitor.VisitExpressionsMut(ref statements, (ref Expr expr) =&gt;
    /// {
    ///     if (expr is Expr.Subquery q)
    ///     {
    ///         expr = q with { Value = q.Value with { LimitClause = null } };
    ///     }
    ///     return ControlFlow&lt;ValueTuple&gt;.Continue;
    /// });
    ///
    /// // statements[0].ToString() == "SELECT (SELECT y FROM z) FROM t LIMIT 3"
    /// </code>
    /// <para><b>Wrap column name in function call</b></para>
    /// <para>
    /// This demonstrates how to effectively replace an expression with another more complicated one
    /// that references the original.
    /// </para>
    /// <code>
    /// var sql = "SELECT x, y FROM t";
    /// var statements = Parser.ParseSql(new GenericDialect(), sql);
    ///
    /// Visitor.VisitExpressionsMut(ref statements, (ref Expr expr) =&gt;
    /// {
    ///     if (expr is Expr.Identifier { Value: var colName } &amp;&amp; colName.Value == "x")
    ///     {
    ///         var oldExpr = expr;
    ///         expr = new Expr.Function(new Function(
    ///             Name: ObjectName.From(Sequence.Of(new Ident("f"))),
    ///             UsesOdbcSyntax: false,
    ///             Parameters: new FunctionArguments.None(),
    ///             Args: new FunctionArguments.List(new FunctionArgumentList(
    ///                 DuplicateTreatment: null,
    ///                 Args: Sequence.Of&lt;FunctionArg&gt;(new FunctionArg.Unnamed(new FunctionArgExpr.Expr(oldExpr))),
    ///                 Clauses: Sequence&lt;FunctionArgumentClause&gt;.Empty)),
    ///             Filter: null,
    ///             NullTreatment: null,
    ///             Over: null,
    ///             WithinGroup: Sequence&lt;OrderByExpr&gt;.Empty));
    ///     }
    ///     return ControlFlow&lt;ValueTuple&gt;.Continue;
    /// });
    ///
    /// // statements[0].ToString() == "SELECT f(x), y FROM t"
    /// </code>
    /// </example>
    // Segundo exemplo: o Rust diz ainda "This example avoids unnecessary allocations by using the
    // [`std::mem`] family of functions."; no C#, basta montar o nó novo com o antigo (a AST é imutável).
    public static ControlFlow<E> VisitExpressionsMut<V, E>(ref V v, RefFunc<Expr, ControlFlow<E>> f)
        where V : IVisitMut<V>
    {
        var flow = v.Visit(new ExprVisitorMut<E>(f), out v);
        if (flow.IsBreak)
        {
            return flow;
        }
        return ControlFlow<E>.Continue;
    }

    // struct StatementVisitor<F>(F);
    // impl<E, F: FnMut(&Statement) -> ControlFlow<E>> Visitor for StatementVisitor<F>
    private sealed class StatementVisitor<E> : Visitor<E>
    {
        private readonly Func<Statement, ControlFlow<E>> _f;

        public StatementVisitor(Func<Statement, ControlFlow<E>> f) => _f = f;

        public override ControlFlow<E> PreVisitStatement(Statement statement) => _f(statement);
    }

    // impl<E, F: FnMut(&mut Statement) -> ControlFlow<E>> VisitorMut for StatementVisitor<F>
    private sealed class StatementVisitorMut<E> : VisitorMut<E>
    {
        private readonly RefFunc<Statement, ControlFlow<E>> _f;

        public StatementVisitorMut(RefFunc<Statement, ControlFlow<E>> f) => _f = f;

        public override ControlFlow<E> PostVisitStatement(ref Statement statement) => _f(ref statement);
    }

    /// <summary>
    /// Invokes the provided closure iteratively with a mutable reference to all statements
    /// present in <paramref name="v"/> (e.g. <c>SELECT</c>, <c>CREATE TABLE</c>, etc).
    /// </summary>
    /// <example>
    /// <code>
    /// var sql = "SELECT a FROM foo where x IN (SELECT y FROM bar); CREATE TABLE baz(q int)";
    /// var statements = Parser.ParseSql(new GenericDialect(), sql);
    ///
    /// // visit all statements
    /// var visited = new List&lt;string&gt;();
    /// Visitor.VisitStatements(statements, stmt =&gt;
    /// {
    ///     visited.Add($"STATEMENT: {stmt}");
    ///     return ControlFlow&lt;ValueTuple&gt;.Continue;
    /// });
    ///
    /// // visited == ["STATEMENT: SELECT a FROM foo WHERE x IN (SELECT y FROM bar)", "STATEMENT: CREATE TABLE baz (q INT)"]
    /// </code>
    /// </example>
    public static ControlFlow<E> VisitStatements<V, E>(V v, Func<Statement, ControlFlow<E>> f)
        where V : IVisit
    {
        var visitor = new StatementVisitor<E>(f);
        var flow = v.Visit(visitor);
        if (flow.IsBreak)
        {
            return flow;
        }
        return ControlFlow<E>.Continue;
    }

    /// <summary>
    /// Invokes the provided closure on all statements (e.g. <c>SELECT</c>, <c>CREATE TABLE</c>, etc) present in <paramref name="v"/>
    /// </summary>
    /// <example>
    /// <code>
    /// var sql = "SELECT x FROM foo LIMIT 9+$limit; SELECT * FROM t LIMIT f()";
    /// var statements = Parser.ParseSql(new GenericDialect(), sql);
    ///
    /// // Remove all select limits in outer statements (not in sub-queries)
    /// Visitor.VisitStatementsMut(ref statements, (ref Statement stmt) =&gt;
    /// {
    ///     if (stmt is Statement.Query q)
    ///     {
    ///         stmt = q with { Value = q.Value with { LimitClause = null } };
    ///     }
    ///     return ControlFlow&lt;ValueTuple&gt;.Continue;
    /// });
    ///
    /// // statements[0].ToString() == "SELECT x FROM foo"
    /// // statements[1].ToString() == "SELECT * FROM t"
    /// </code>
    /// </example>
    public static ControlFlow<E> VisitStatementsMut<V, E>(ref V v, RefFunc<Statement, ControlFlow<E>> f)
        where V : IVisitMut<V>
    {
        var flow = v.Visit(new StatementVisitorMut<E>(f), out v);
        if (flow.IsBreak)
        {
            return flow;
        }
        return ControlFlow<E>.Continue;
    }
}
