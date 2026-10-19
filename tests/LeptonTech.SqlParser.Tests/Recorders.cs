// Testes próprios: visitors que registram a sequência de ganchos, em pares com rótulos idênticos (API pública ×
// Core), para comparar a ordem de visita do SqlVisitor/SqlRewriter com a do Visitor<TBreak>/VisitorMut<TBreak>.

using LeptonTech.SqlParser.Core;
using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Tests;

/// <summary>Registra todos os ganchos do <see cref="SqlVisitor"/> com o SQL do nó.</summary>
internal sealed class RecordingSqlVisitor : SqlVisitor
{
    private readonly bool _leaves;

    /// <param name="leaves">Também registra identificadores e valores (folhas mais frequentes).</param>
    public RecordingSqlVisitor(bool leaves = true) => _leaves = leaves;

    public List<string> Visited { get; } = new List<string>();

    private ControlFlow Add(string text)
    {
        Visited.Add(text);
        return ControlFlow.Continue;
    }

    private ControlFlow AddLeaf(string text) => _leaves ? Add(text) : ControlFlow.Continue;

    public override ControlFlow PreVisitQuery(Query query) => Add($"PRE: QUERY: {query}");
    public override ControlFlow PostVisitQuery(Query query) => Add($"POST: QUERY: {query}");
    public override ControlFlow PreVisitSelect(Select select) => Add($"PRE: SELECT: {select}");
    public override ControlFlow PostVisitSelect(Select select) => Add($"POST: SELECT: {select}");
    public override ControlFlow PreVisitRelation(ObjectName relation) => Add($"PRE: RELATION: {relation}");
    public override ControlFlow PostVisitRelation(ObjectName relation) => Add($"POST: RELATION: {relation}");
    public override ControlFlow PreVisitTableFactor(TableFactor tableFactor) => Add($"PRE: TABLE FACTOR: {tableFactor}");
    public override ControlFlow PostVisitTableFactor(TableFactor tableFactor) => Add($"POST: TABLE FACTOR: {tableFactor}");
    public override ControlFlow PreVisitExpression(Expr expression) => Add($"PRE: EXPR: {expression}");
    public override ControlFlow PostVisitExpression(Expr expression) => Add($"POST: EXPR: {expression}");
    public override ControlFlow PreVisitStatement(Statement statement) => Add($"PRE: STATEMENT: {statement}");
    public override ControlFlow PostVisitStatement(Statement statement) => Add($"POST: STATEMENT: {statement}");
    public override ControlFlow PreVisitValue(ValueWithSpan value) => AddLeaf($"PRE: VALUE: {value}");
    public override ControlFlow PostVisitValue(ValueWithSpan value) => AddLeaf($"POST: VALUE: {value}");
    public override ControlFlow PreVisitIdentifier(Ident identifier) => AddLeaf($"PRE: IDENT: {identifier}");
    public override ControlFlow PostVisitIdentifier(Ident identifier) => AddLeaf($"POST: IDENT: {identifier}");
    public override ControlFlow PreVisitOrderBy(OrderBy orderBy) => Add($"PRE: ORDER BY: {orderBy}");
    public override ControlFlow PostVisitOrderBy(OrderBy orderBy) => Add($"POST: ORDER BY: {orderBy}");
    public override ControlFlow PreVisitOrderByExpression(OrderByExpr orderByExpression) => Add($"PRE: ORDER BY EXPR: {orderByExpression}");
    public override ControlFlow PostVisitOrderByExpression(OrderByExpr orderByExpression) => Add($"POST: ORDER BY EXPR: {orderByExpression}");
    public override ControlFlow PreVisitGroupBy(GroupByExpr groupBy) => Add($"PRE: GROUP BY: {groupBy}");
    public override ControlFlow PostVisitGroupBy(GroupByExpr groupBy) => Add($"POST: GROUP BY: {groupBy}");
}

/// <summary>O mesmo registro de <see cref="RecordingSqlVisitor"/>, direto sobre o visitor do Core.</summary>
internal sealed class RecordingCoreVisitor : Visitor<ValueTuple>
{
    public List<string> Visited { get; } = new List<string>();

    private ControlFlow<ValueTuple> Add(string text)
    {
        Visited.Add(text);
        return ControlFlow<ValueTuple>.Continue;
    }

    public override ControlFlow<ValueTuple> PreVisitQuery(Query query) => Add($"PRE: QUERY: {query}");
    public override ControlFlow<ValueTuple> PostVisitQuery(Query query) => Add($"POST: QUERY: {query}");
    public override ControlFlow<ValueTuple> PreVisitSelect(Select select) => Add($"PRE: SELECT: {select}");
    public override ControlFlow<ValueTuple> PostVisitSelect(Select select) => Add($"POST: SELECT: {select}");
    public override ControlFlow<ValueTuple> PreVisitRelation(ObjectName relation) => Add($"PRE: RELATION: {relation}");
    public override ControlFlow<ValueTuple> PostVisitRelation(ObjectName relation) => Add($"POST: RELATION: {relation}");
    public override ControlFlow<ValueTuple> PreVisitTableFactor(TableFactor tableFactor) => Add($"PRE: TABLE FACTOR: {tableFactor}");
    public override ControlFlow<ValueTuple> PostVisitTableFactor(TableFactor tableFactor) => Add($"POST: TABLE FACTOR: {tableFactor}");
    public override ControlFlow<ValueTuple> PreVisitExpr(Expr expr) => Add($"PRE: EXPR: {expr}");
    public override ControlFlow<ValueTuple> PostVisitExpr(Expr expr) => Add($"POST: EXPR: {expr}");
    public override ControlFlow<ValueTuple> PreVisitStatement(Statement statement) => Add($"PRE: STATEMENT: {statement}");
    public override ControlFlow<ValueTuple> PostVisitStatement(Statement statement) => Add($"POST: STATEMENT: {statement}");
    public override ControlFlow<ValueTuple> PreVisitValue(ValueWithSpan value) => Add($"PRE: VALUE: {value}");
    public override ControlFlow<ValueTuple> PostVisitValue(ValueWithSpan value) => Add($"POST: VALUE: {value}");
    public override ControlFlow<ValueTuple> PreVisitIdent(Ident ident) => Add($"PRE: IDENT: {ident}");
    public override ControlFlow<ValueTuple> PostVisitIdent(Ident ident) => Add($"POST: IDENT: {ident}");
    public override ControlFlow<ValueTuple> PreVisitOrderBy(OrderBy orderBy) => Add($"PRE: ORDER BY: {orderBy}");
    public override ControlFlow<ValueTuple> PostVisitOrderBy(OrderBy orderBy) => Add($"POST: ORDER BY: {orderBy}");
    public override ControlFlow<ValueTuple> PreVisitOrderByExpr(OrderByExpr orderByExpr) => Add($"PRE: ORDER BY EXPR: {orderByExpr}");
    public override ControlFlow<ValueTuple> PostVisitOrderByExpr(OrderByExpr orderByExpr) => Add($"POST: ORDER BY EXPR: {orderByExpr}");
    public override ControlFlow<ValueTuple> PreVisitGroupBy(GroupByExpr groupBy) => Add($"PRE: GROUP BY: {groupBy}");
    public override ControlFlow<ValueTuple> PostVisitGroupBy(GroupByExpr groupBy) => Add($"POST: GROUP BY: {groupBy}");
}

/// <summary>
/// Registra todos os ganchos do <see cref="SqlRewriter"/> e põe em maiúsculas os identificadores no <c>pre</c>,
/// para que a comparação com o Core também cubra a troca de nós durante a visita.
/// </summary>
internal sealed class RecordingRewriter : SqlRewriter
{
    public List<string> Visited { get; } = new List<string>();

    private T Add<T>(string text, T node)
    {
        Visited.Add(text);
        return node;
    }

    public override Query PreVisitQuery(Query query) => Add($"PRE: QUERY: {query}", query);
    public override Query PostVisitQuery(Query query) => Add($"POST: QUERY: {query}", query);
    public override Select PreVisitSelect(Select select) => Add($"PRE: SELECT: {select}", select);
    public override Select PostVisitSelect(Select select) => Add($"POST: SELECT: {select}", select);
    public override ObjectName PreVisitRelation(ObjectName relation) => Add($"PRE: RELATION: {relation}", relation);
    public override ObjectName PostVisitRelation(ObjectName relation) => Add($"POST: RELATION: {relation}", relation);
    public override TableFactor PreVisitTableFactor(TableFactor tableFactor) => Add($"PRE: TABLE FACTOR: {tableFactor}", tableFactor);
    public override TableFactor PostVisitTableFactor(TableFactor tableFactor) => Add($"POST: TABLE FACTOR: {tableFactor}", tableFactor);
    public override Expr PreVisitExpression(Expr expression) => Add($"PRE: EXPR: {expression}", expression);
    public override Expr PostVisitExpression(Expr expression) => Add($"POST: EXPR: {expression}", expression);
    public override Statement PreVisitStatement(Statement statement) => Add($"PRE: STATEMENT: {statement}", statement);
    public override Statement PostVisitStatement(Statement statement) => Add($"POST: STATEMENT: {statement}", statement);
    public override ValueWithSpan PreVisitValue(ValueWithSpan value) => Add($"PRE: VALUE: {value}", value);
    public override ValueWithSpan PostVisitValue(ValueWithSpan value) => Add($"POST: VALUE: {value}", value);

    public override Ident PreVisitIdentifier(Ident identifier) =>
        Add($"PRE: IDENT: {identifier}", identifier with { Value = identifier.Value.ToUpperInvariant() });

    public override Ident PostVisitIdentifier(Ident identifier) => Add($"POST: IDENT: {identifier}", identifier);
    public override OrderBy PreVisitOrderBy(OrderBy orderBy) => Add($"PRE: ORDER BY: {orderBy}", orderBy);
    public override OrderBy PostVisitOrderBy(OrderBy orderBy) => Add($"POST: ORDER BY: {orderBy}", orderBy);
    public override OrderByExpr PreVisitOrderByExpression(OrderByExpr orderByExpression) => Add($"PRE: ORDER BY EXPR: {orderByExpression}", orderByExpression);
    public override OrderByExpr PostVisitOrderByExpression(OrderByExpr orderByExpression) => Add($"POST: ORDER BY EXPR: {orderByExpression}", orderByExpression);
    public override GroupByExpr PreVisitGroupBy(GroupByExpr groupBy) => Add($"PRE: GROUP BY: {groupBy}", groupBy);
    public override GroupByExpr PostVisitGroupBy(GroupByExpr groupBy) => Add($"POST: GROUP BY: {groupBy}", groupBy);
}

/// <summary>O mesmo que <see cref="RecordingRewriter"/>, direto sobre o visitor mutável do Core.</summary>
internal sealed class RecordingCoreMutator : VisitorMut<ValueTuple>
{
    public List<string> Visited { get; } = new List<string>();

    private ControlFlow<ValueTuple> Add(string text)
    {
        Visited.Add(text);
        return ControlFlow<ValueTuple>.Continue;
    }

    public override ControlFlow<ValueTuple> PreVisitQuery(ref Query query) => Add($"PRE: QUERY: {query}");
    public override ControlFlow<ValueTuple> PostVisitQuery(ref Query query) => Add($"POST: QUERY: {query}");
    public override ControlFlow<ValueTuple> PreVisitSelect(ref Select select) => Add($"PRE: SELECT: {select}");
    public override ControlFlow<ValueTuple> PostVisitSelect(ref Select select) => Add($"POST: SELECT: {select}");
    public override ControlFlow<ValueTuple> PreVisitRelation(ref ObjectName relation) => Add($"PRE: RELATION: {relation}");
    public override ControlFlow<ValueTuple> PostVisitRelation(ref ObjectName relation) => Add($"POST: RELATION: {relation}");
    public override ControlFlow<ValueTuple> PreVisitTableFactor(ref TableFactor tableFactor) => Add($"PRE: TABLE FACTOR: {tableFactor}");
    public override ControlFlow<ValueTuple> PostVisitTableFactor(ref TableFactor tableFactor) => Add($"POST: TABLE FACTOR: {tableFactor}");
    public override ControlFlow<ValueTuple> PreVisitExpr(ref Expr expr) => Add($"PRE: EXPR: {expr}");
    public override ControlFlow<ValueTuple> PostVisitExpr(ref Expr expr) => Add($"POST: EXPR: {expr}");
    public override ControlFlow<ValueTuple> PreVisitStatement(ref Statement statement) => Add($"PRE: STATEMENT: {statement}");
    public override ControlFlow<ValueTuple> PostVisitStatement(ref Statement statement) => Add($"POST: STATEMENT: {statement}");
    public override ControlFlow<ValueTuple> PreVisitValue(ref ValueWithSpan value) => Add($"PRE: VALUE: {value}");
    public override ControlFlow<ValueTuple> PostVisitValue(ref ValueWithSpan value) => Add($"POST: VALUE: {value}");

    public override ControlFlow<ValueTuple> PreVisitIdent(ref Ident ident)
    {
        var flow = Add($"PRE: IDENT: {ident}");
        ident = ident with { Value = ident.Value.ToUpperInvariant() };
        return flow;
    }

    public override ControlFlow<ValueTuple> PostVisitIdent(ref Ident ident) => Add($"POST: IDENT: {ident}");
    public override ControlFlow<ValueTuple> PreVisitOrderBy(ref OrderBy orderBy) => Add($"PRE: ORDER BY: {orderBy}");
    public override ControlFlow<ValueTuple> PostVisitOrderBy(ref OrderBy orderBy) => Add($"POST: ORDER BY: {orderBy}");
    public override ControlFlow<ValueTuple> PreVisitOrderByExpr(ref OrderByExpr orderByExpr) => Add($"PRE: ORDER BY EXPR: {orderByExpr}");
    public override ControlFlow<ValueTuple> PostVisitOrderByExpr(ref OrderByExpr orderByExpr) => Add($"POST: ORDER BY EXPR: {orderByExpr}");
    public override ControlFlow<ValueTuple> PreVisitGroupBy(ref GroupByExpr groupBy) => Add($"PRE: GROUP BY: {groupBy}");
    public override ControlFlow<ValueTuple> PostVisitGroupBy(ref GroupByExpr groupBy) => Add($"POST: GROUP BY: {groupBy}");
}
