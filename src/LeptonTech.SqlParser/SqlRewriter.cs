// Código próprio da camada pública (sem origem no sqlparser-rs nem no SqlParser-cs, que não tem visitor de
// escrita). Ganchos que devolvem o nó novo, como o ExpressionVisitor do .NET. A travessia é a do Core
// (VisitorMut<TBreak>, gerado a partir do derive VisitMut do Rust): esta classe só repassa os ganchos.
// Ver docs/adr/0009-api-publica-fase-2.md.

using LeptonTech.SqlParser.Core;
using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser;

/// <summary>
/// Visitor de reescrita da AST: cada gancho recebe um nó e devolve o nó que fica no lugar dele (o mesmo, se
/// nada mudar). Use com <c>node.Rewrite(rewriter)</c> (<see cref="SqlVisitorExtensions.Rewrite{T}(T, SqlRewriter)"/>),
/// que devolve a árvore nova.
/// </summary>
/// <remarks>
/// <para>
/// <b>Travessia:</b> é exatamente a do <see cref="VisitorMut{TBreak}"/> do Core, que é a do derive <c>VisitMut</c>
/// do sqlparser-rs, na mesma ordem do <see cref="SqlVisitor"/>. Esta classe não percorre nada: ela só adapta os
/// ganchos do Core, que recebem o nó por <c>ref</c>, para ganchos que devolvem o nó.
/// </para>
/// <para>
/// <b>Pré e pós:</b> o nó devolvido por um <c>PreVisit*</c> é o que tem os filhos visitados em seguida (como no
/// Rust, depois de <c>*expr = ...</c> em <c>pre_visit_expr</c>). O nó devolvido por um <c>PostVisit*</c> já não é
/// visitado de novo. Para trocar um nó levando em conta os filhos já reescritos, use o <c>PostVisit*</c>.
/// </para>
/// <para>
/// <b>Imutabilidade:</b> a AST é imutável. A reescrita reconstrói (com <c>with</c>) só os pais dos nós que mudaram;
/// a árvore original não muda. Sem nenhuma troca, <c>Rewrite</c> devolve a mesma instância.
/// </para>
/// <para>
/// <b>Interrupção:</b> um gancho pode chamar <see cref="Break"/>. O nó que ele devolve ainda é usado, nenhum outro
/// gancho é chamado e as trocas feitas até ali ficam no resultado, como no <c>VisitMut</c> do Rust.
/// </para>
/// <para>
/// Um gancho não pode devolver <see langword="null"/> (lança <see cref="InvalidOperationException"/>). Uma
/// instância pode ser reutilizada em várias reescritas, mas não em reescritas simultâneas.
/// </para>
/// <para>
/// <b>Thread dos ganchos:</b> como no <see cref="SqlVisitor"/>, em árvores muito profundas a travessia do Core
/// continua numa thread nova quando falta pilha, e os ganchos seguintes rodam nela (DIV-009 em
/// <c>docs/port/divergencias.md</c>). Quem chamou <c>Rewrite</c> espera o fim e recebe as exceções dos ganchos.
/// Os ganchos não devem depender da thread: estado <c>[ThreadStatic]</c> e <c>ThreadLocal</c> não acompanha, e
/// objetos presos a uma thread (controles do WinForms/WPF, objetos COM de apartamento STA) não podem ser usados
/// dentro de um gancho. Numa thread STA (a thread de UI), a espera bombeia mensagens COM, o que pode reentrar no
/// código da interface.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Troca o nome da tabela "clientes" por "crm.clientes" em qualquer lugar da instrução.
/// sealed class RenameTable : SqlRewriter
/// {
///     public override ObjectName PostVisitRelation(ObjectName relation) =>
///         relation.ToSql() == "clientes"
///             ? ObjectName.From(Sequence.Of(new Ident("crm"), new Ident("clientes")))
///             : relation;
/// }
///
/// var statement = new SqlQueryParser().Parse("SELECT * FROM clientes WHERE id = 1")[0];
/// var rewritten = statement.Rewrite(new RenameTable());
/// // rewritten.ToSql() == "SELECT * FROM crm.clientes WHERE id = 1"
/// </code>
/// </example>
public abstract class SqlRewriter
{
    private VisitorMut<ValueTuple>? _adapter;
    private bool _breakRequested;

    /// <summary>Construtor para as classes derivadas.</summary>
    protected SqlRewriter()
    {
    }

    /// <summary>O visitor mutável do Core que repassa os ganchos para esta instância.</summary>
    internal VisitorMut<ValueTuple> CoreVisitor => _adapter ??= new Adapter(this);

    /// <summary>Prepara uma nova reescrita (limpa um <see cref="Break"/> de uma reescrita anterior).</summary>
    internal void Reset() => _breakRequested = false;

    /// <summary>
    /// Interrompe a reescrita depois do gancho atual. O nó que o gancho devolver ainda é usado; nenhum outro gancho
    /// é chamado; as trocas feitas até aqui ficam no resultado.
    /// </summary>
    protected void Break() => _breakRequested = true;

    /// <summary>Chamado para cada <see cref="Query"/>, antes dos filhos.</summary>
    public virtual Query PreVisitQuery(Query query) => query;

    /// <summary>Chamado para cada <see cref="Query"/>, depois dos filhos.</summary>
    public virtual Query PostVisitQuery(Query query) => query;

    /// <summary>Chamado para cada bloco <see cref="Select"/>, antes dos filhos.</summary>
    public virtual Select PreVisitSelect(Select select) => select;

    /// <summary>Chamado para cada bloco <see cref="Select"/>, depois dos filhos.</summary>
    public virtual Select PostVisitSelect(Select select) => select;

    /// <summary>
    /// Chamado para cada relação (nome de tabela, visão etc.), antes dos filhos. Mesmos pontos de
    /// <see cref="SqlVisitor.PreVisitRelation(ObjectName)"/>.
    /// </summary>
    public virtual ObjectName PreVisitRelation(ObjectName relation) => relation;

    /// <summary>Chamado para cada relação, depois dos filhos.</summary>
    public virtual ObjectName PostVisitRelation(ObjectName relation) => relation;

    /// <summary>Chamado para cada <see cref="TableFactor"/>, antes dos filhos.</summary>
    public virtual TableFactor PreVisitTableFactor(TableFactor tableFactor) => tableFactor;

    /// <summary>Chamado para cada <see cref="TableFactor"/>, depois dos filhos.</summary>
    public virtual TableFactor PostVisitTableFactor(TableFactor tableFactor) => tableFactor;

    /// <summary>Chamado para cada expressão (<see cref="Expr"/>), antes dos filhos (Core: <c>PreVisitExpr</c>).</summary>
    public virtual Expr PreVisitExpression(Expr expression) => expression;

    /// <summary>Chamado para cada expressão, depois dos filhos (Core: <c>PostVisitExpr</c>).</summary>
    public virtual Expr PostVisitExpression(Expr expression) => expression;

    /// <summary>Chamado para cada instrução (<see cref="Statement"/>), antes dos filhos.</summary>
    public virtual Statement PreVisitStatement(Statement statement) => statement;

    /// <summary>Chamado para cada instrução, depois dos filhos.</summary>
    public virtual Statement PostVisitStatement(Statement statement) => statement;

    /// <summary>Chamado para cada valor literal (<see cref="ValueWithSpan"/>), antes dos filhos.</summary>
    public virtual ValueWithSpan PreVisitValue(ValueWithSpan value) => value;

    /// <summary>Chamado para cada valor literal, depois dos filhos.</summary>
    public virtual ValueWithSpan PostVisitValue(ValueWithSpan value) => value;

    /// <summary>Chamado para cada identificador (<see cref="Ident"/>), antes dos filhos (Core: <c>PreVisitIdent</c>).</summary>
    public virtual Ident PreVisitIdentifier(Ident identifier) => identifier;

    /// <summary>Chamado para cada identificador, depois dos filhos (Core: <c>PostVisitIdent</c>).</summary>
    public virtual Ident PostVisitIdentifier(Ident identifier) => identifier;

    /// <summary>Chamado para cada cláusula <c>ORDER BY</c> (<see cref="OrderBy"/>), antes dos filhos.</summary>
    public virtual OrderBy PreVisitOrderBy(OrderBy orderBy) => orderBy;

    /// <summary>Chamado para cada cláusula <c>ORDER BY</c>, depois dos filhos.</summary>
    public virtual OrderBy PostVisitOrderBy(OrderBy orderBy) => orderBy;

    /// <summary>
    /// Chamado para cada item de <c>ORDER BY</c> (<see cref="OrderByExpr"/>), antes dos filhos
    /// (Core: <c>PreVisitOrderByExpr</c>).
    /// </summary>
    public virtual OrderByExpr PreVisitOrderByExpression(OrderByExpr orderByExpression) => orderByExpression;

    /// <summary>Chamado para cada item de <c>ORDER BY</c>, depois dos filhos (Core: <c>PostVisitOrderByExpr</c>).</summary>
    public virtual OrderByExpr PostVisitOrderByExpression(OrderByExpr orderByExpression) => orderByExpression;

    /// <summary>Chamado para cada cláusula <c>GROUP BY</c> (<see cref="GroupByExpr"/>), antes dos filhos.</summary>
    public virtual GroupByExpr PreVisitGroupBy(GroupByExpr groupBy) => groupBy;

    /// <summary>Chamado para cada cláusula <c>GROUP BY</c>, depois dos filhos.</summary>
    public virtual GroupByExpr PostVisitGroupBy(GroupByExpr groupBy) => groupBy;

    /// <summary>
    /// Repassa cada gancho do visitor mutável do Core para o gancho correspondente do <see cref="SqlRewriter"/>:
    /// o nó devolvido substitui o da referência, e um <see cref="Break"/> pedido vira <c>ControlFlow.Break</c>.
    /// </summary>
    private sealed class Adapter : VisitorMut<ValueTuple>
    {
        private readonly SqlRewriter _rewriter;

        public Adapter(SqlRewriter rewriter) => _rewriter = rewriter;

        private ControlFlow<ValueTuple> Replace<T>(ref T node, T? replacement, string hook)
            where T : class
        {
            node = replacement ?? throw new InvalidOperationException(
                $"O gancho {nameof(SqlRewriter)}.{hook} devolveu null; devolva o próprio nó para mantê-lo.");
            return _rewriter._breakRequested ? ControlFlow<ValueTuple>.Break(default) : ControlFlow<ValueTuple>.Continue;
        }

        public override ControlFlow<ValueTuple> PreVisitQuery(ref Query query) =>
            Replace(ref query, _rewriter.PreVisitQuery(query), nameof(PreVisitQuery));

        public override ControlFlow<ValueTuple> PostVisitQuery(ref Query query) =>
            Replace(ref query, _rewriter.PostVisitQuery(query), nameof(PostVisitQuery));

        public override ControlFlow<ValueTuple> PreVisitSelect(ref Select select) =>
            Replace(ref select, _rewriter.PreVisitSelect(select), nameof(PreVisitSelect));

        public override ControlFlow<ValueTuple> PostVisitSelect(ref Select select) =>
            Replace(ref select, _rewriter.PostVisitSelect(select), nameof(PostVisitSelect));

        public override ControlFlow<ValueTuple> PreVisitRelation(ref ObjectName relation) =>
            Replace(ref relation, _rewriter.PreVisitRelation(relation), nameof(PreVisitRelation));

        public override ControlFlow<ValueTuple> PostVisitRelation(ref ObjectName relation) =>
            Replace(ref relation, _rewriter.PostVisitRelation(relation), nameof(PostVisitRelation));

        public override ControlFlow<ValueTuple> PreVisitTableFactor(ref TableFactor tableFactor) =>
            Replace(ref tableFactor, _rewriter.PreVisitTableFactor(tableFactor), nameof(PreVisitTableFactor));

        public override ControlFlow<ValueTuple> PostVisitTableFactor(ref TableFactor tableFactor) =>
            Replace(ref tableFactor, _rewriter.PostVisitTableFactor(tableFactor), nameof(PostVisitTableFactor));

        public override ControlFlow<ValueTuple> PreVisitExpr(ref Expr expr) =>
            Replace(ref expr, _rewriter.PreVisitExpression(expr), nameof(SqlRewriter.PreVisitExpression));

        public override ControlFlow<ValueTuple> PostVisitExpr(ref Expr expr) =>
            Replace(ref expr, _rewriter.PostVisitExpression(expr), nameof(SqlRewriter.PostVisitExpression));

        public override ControlFlow<ValueTuple> PreVisitStatement(ref Statement statement) =>
            Replace(ref statement, _rewriter.PreVisitStatement(statement), nameof(PreVisitStatement));

        public override ControlFlow<ValueTuple> PostVisitStatement(ref Statement statement) =>
            Replace(ref statement, _rewriter.PostVisitStatement(statement), nameof(PostVisitStatement));

        public override ControlFlow<ValueTuple> PreVisitValue(ref ValueWithSpan value) =>
            Replace(ref value, _rewriter.PreVisitValue(value), nameof(PreVisitValue));

        public override ControlFlow<ValueTuple> PostVisitValue(ref ValueWithSpan value) =>
            Replace(ref value, _rewriter.PostVisitValue(value), nameof(PostVisitValue));

        public override ControlFlow<ValueTuple> PreVisitIdent(ref Ident ident) =>
            Replace(ref ident, _rewriter.PreVisitIdentifier(ident), nameof(SqlRewriter.PreVisitIdentifier));

        public override ControlFlow<ValueTuple> PostVisitIdent(ref Ident ident) =>
            Replace(ref ident, _rewriter.PostVisitIdentifier(ident), nameof(SqlRewriter.PostVisitIdentifier));

        public override ControlFlow<ValueTuple> PreVisitOrderBy(ref OrderBy orderBy) =>
            Replace(ref orderBy, _rewriter.PreVisitOrderBy(orderBy), nameof(PreVisitOrderBy));

        public override ControlFlow<ValueTuple> PostVisitOrderBy(ref OrderBy orderBy) =>
            Replace(ref orderBy, _rewriter.PostVisitOrderBy(orderBy), nameof(PostVisitOrderBy));

        public override ControlFlow<ValueTuple> PreVisitOrderByExpr(ref OrderByExpr orderByExpr) =>
            Replace(ref orderByExpr, _rewriter.PreVisitOrderByExpression(orderByExpr), nameof(SqlRewriter.PreVisitOrderByExpression));

        public override ControlFlow<ValueTuple> PostVisitOrderByExpr(ref OrderByExpr orderByExpr) =>
            Replace(ref orderByExpr, _rewriter.PostVisitOrderByExpression(orderByExpr), nameof(SqlRewriter.PostVisitOrderByExpression));

        public override ControlFlow<ValueTuple> PreVisitGroupBy(ref GroupByExpr groupBy) =>
            Replace(ref groupBy, _rewriter.PreVisitGroupBy(groupBy), nameof(PreVisitGroupBy));

        public override ControlFlow<ValueTuple> PostVisitGroupBy(ref GroupByExpr groupBy) =>
            Replace(ref groupBy, _rewriter.PostVisitGroupBy(groupBy), nameof(PostVisitGroupBy));
    }
}
