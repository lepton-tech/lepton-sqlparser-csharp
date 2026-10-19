// Código próprio da camada pública (sem origem no sqlparser-rs). node.Visit(visitor) imita o formato de uso do
// SqlParser-cs sem copiar código dele. Os auxiliares com lambdas delegam às funções do Core
// (Visitor.VisitRelations, VisitExpressions, VisitStatements e as versões *Mut, portadas de ast/visitor.rs),
// sem percorrer nada por conta própria. Ver docs/adr/0009-api-publica-fase-2.md.

using SqlParser.Core;
using SqlParser.Core.Ast;

namespace SqlParser;

/// <summary>
/// Extensões para visitar e reescrever a AST: <c>Visit</c> (com <see cref="SqlVisitor"/>), <c>Rewrite</c> (com
/// <see cref="SqlRewriter"/>) e atalhos com lambdas para relações, expressões e instruções.
/// </summary>
/// <remarks>
/// <para>
/// Valem para qualquer nó visitável do Core: <see cref="Statement"/>, <see cref="Query"/>, <see cref="Expr"/>,
/// <see cref="Sequence{T}"/> de instruções (o retorno de <c>SqlQueryParser.Parse</c>) etc.
/// As versões de leitura exigem <see cref="IVisit"/>; as de reescrita exigem <see cref="IVisitMut{TSelf}"/> do
/// próprio tipo, então o nó deve estar tipado pelo tipo-base (<c>Statement</c>, e não <c>Statement.Query</c>),
/// porque a reescrita pode trocar a variante.
/// </para>
/// <para>
/// <b>Ordem dos atalhos</b> (herdada do Rust): os de leitura (<c>VisitRelations</c>, <c>VisitExpressions</c>,
/// <c>VisitStatements</c>) usam o gancho <c>pre</c>, ou seja, o pai antes dos filhos; os de reescrita
/// (<c>RewriteRelations</c>, <c>RewriteExpressions</c>, <c>RewriteStatements</c>) usam o gancho <c>post</c>, ou
/// seja, a função recebe o nó com os filhos já reescritos, e o nó devolvido não é visitado de novo.
/// </para>
/// <para>
/// <b>Thread:</b> em árvores muito profundas, os ganchos e as funções passadas aos atalhos podem rodar numa thread
/// nova, criada quando falta pilha (DIV-009; ver "Thread dos ganchos" em <see cref="SqlVisitor"/>).
/// </para>
/// </remarks>
public static class SqlVisitorExtensions
{
    /// <summary>Percorre <paramref name="node"/> chamando os ganchos de <paramref name="visitor"/>.</summary>
    /// <typeparam name="T">Tipo do nó.</typeparam>
    /// <param name="node">Nó inicial (por exemplo, uma instrução ou a sequência devolvida pelo parser).</param>
    /// <param name="visitor">O visitor.</param>
    /// <returns><see cref="ControlFlow.Break"/> se algum gancho interrompeu a visita; senão, <see cref="ControlFlow.Continue"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="visitor"/> é <see langword="null"/>.</exception>
    public static ControlFlow Visit<T>(this T node, SqlVisitor visitor)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        ThrowIfNull(visitor, nameof(visitor));
        return SqlVisitor.FromCore(node.Visit(visitor.CoreVisitor));
    }

    /// <summary>
    /// Reescreve <paramref name="node"/> com os ganchos de <paramref name="rewriter"/> e devolve a árvore nova. A
    /// árvore original não muda.
    /// </summary>
    /// <typeparam name="T">Tipo do nó: o tipo-base (por exemplo, <see cref="Statement"/>), porque a reescrita pode trocar a variante.</typeparam>
    /// <param name="node">Nó inicial.</param>
    /// <param name="rewriter">O visitor de reescrita.</param>
    /// <returns>
    /// A árvore reescrita; a mesma instância (ou, para <see cref="Sequence{T}"/>, a mesma memória) se nenhum gancho
    /// trocou nada.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="rewriter"/> é <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Um gancho devolveu <see langword="null"/>.</exception>
    public static T Rewrite<T>(this T node, SqlRewriter rewriter)
        where T : IVisitMut<T>
    {
        ThrowIfNull(node, nameof(node));
        ThrowIfNull(rewriter, nameof(rewriter));
        rewriter.Reset();
        _ = node.Visit(rewriter.CoreVisitor, out var result);
        return result;
    }

    /// <summary>
    /// Chama <paramref name="visit"/> para cada relação (nome de tabela, visão etc.) de <paramref name="node"/>, na
    /// ordem de visita e com repetições (Rust: <c>visit_relations</c>).
    /// </summary>
    /// <returns><see cref="ControlFlow.Break"/> se <paramref name="visit"/> interrompeu a visita.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="visit"/> é <see langword="null"/>.</exception>
    public static ControlFlow VisitRelations<T>(this T node, Func<ObjectName, ControlFlow> visit)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        ThrowIfNull(visit, nameof(visit));
        return SqlVisitor.FromCore(Visitor.VisitRelations<T, ValueTuple>(node, relation => SqlVisitor.ToCore(visit(relation))));
    }

    /// <summary>
    /// Chama <paramref name="visit"/> para cada relação (nome de tabela, visão etc.) de <paramref name="node"/>, na
    /// ordem de visita e com repetições (Rust: <c>visit_relations</c>).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="visit"/> é <see langword="null"/>.</exception>
    public static void VisitRelations<T>(this T node, Action<ObjectName> visit)
        where T : IVisit
    {
        ThrowIfNull(visit, nameof(visit));
        _ = node.VisitRelations(relation =>
        {
            visit(relation);
            return ControlFlow.Continue;
        });
    }

    /// <summary>
    /// Chama <paramref name="visit"/> para cada expressão de <paramref name="node"/>, o pai antes dos filhos
    /// (Rust: <c>visit_expressions</c>).
    /// </summary>
    /// <returns><see cref="ControlFlow.Break"/> se <paramref name="visit"/> interrompeu a visita.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="visit"/> é <see langword="null"/>.</exception>
    public static ControlFlow VisitExpressions<T>(this T node, Func<Expr, ControlFlow> visit)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        ThrowIfNull(visit, nameof(visit));
        return SqlVisitor.FromCore(Visitor.VisitExpressions<T, ValueTuple>(node, expr => SqlVisitor.ToCore(visit(expr))));
    }

    /// <summary>
    /// Chama <paramref name="visit"/> para cada expressão de <paramref name="node"/>, o pai antes dos filhos
    /// (Rust: <c>visit_expressions</c>).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="visit"/> é <see langword="null"/>.</exception>
    public static void VisitExpressions<T>(this T node, Action<Expr> visit)
        where T : IVisit
    {
        ThrowIfNull(visit, nameof(visit));
        _ = node.VisitExpressions(expr =>
        {
            visit(expr);
            return ControlFlow.Continue;
        });
    }

    /// <summary>
    /// Chama <paramref name="visit"/> para cada instrução de <paramref name="node"/>, inclusive as aninhadas, a
    /// externa antes das internas (Rust: <c>visit_statements</c>).
    /// </summary>
    /// <returns><see cref="ControlFlow.Break"/> se <paramref name="visit"/> interrompeu a visita.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="visit"/> é <see langword="null"/>.</exception>
    public static ControlFlow VisitStatements<T>(this T node, Func<Statement, ControlFlow> visit)
        where T : IVisit
    {
        ThrowIfNull(node, nameof(node));
        ThrowIfNull(visit, nameof(visit));
        return SqlVisitor.FromCore(Visitor.VisitStatements<T, ValueTuple>(node, statement => SqlVisitor.ToCore(visit(statement))));
    }

    /// <summary>
    /// Chama <paramref name="visit"/> para cada instrução de <paramref name="node"/>, inclusive as aninhadas, a
    /// externa antes das internas (Rust: <c>visit_statements</c>).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="visit"/> é <see langword="null"/>.</exception>
    public static void VisitStatements<T>(this T node, Action<Statement> visit)
        where T : IVisit
    {
        ThrowIfNull(visit, nameof(visit));
        _ = node.VisitStatements(statement =>
        {
            visit(statement);
            return ControlFlow.Continue;
        });
    }

    /// <summary>
    /// Troca cada relação (nome de tabela, visão etc.) de <paramref name="node"/> pelo valor que
    /// <paramref name="rewrite"/> devolver e devolve a árvore nova (Rust: <c>visit_relations_mut</c>, gancho
    /// <c>post</c>).
    /// </summary>
    /// <returns>A árvore reescrita; a mesma instância se nada mudou.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="rewrite"/> é <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="rewrite"/> devolveu <see langword="null"/>.</exception>
    public static T RewriteRelations<T>(this T node, Func<ObjectName, ObjectName> rewrite)
        where T : IVisitMut<T>
    {
        ThrowIfNull(node, nameof(node));
        ThrowIfNull(rewrite, nameof(rewrite));
        _ = Visitor.VisitRelationsMut<T, ValueTuple>(ref node, (ref ObjectName relation) =>
        {
            relation = NotNull(rewrite(relation), nameof(rewrite));
            return ControlFlow<ValueTuple>.Continue;
        });
        return node;
    }

    /// <summary>
    /// Troca cada expressão de <paramref name="node"/> pelo valor que <paramref name="rewrite"/> devolver e devolve a
    /// árvore nova (Rust: <c>visit_expressions_mut</c>, gancho <c>post</c>: os filhos são reescritos antes do pai,
    /// e a expressão devolvida não é visitada de novo).
    /// </summary>
    /// <returns>A árvore reescrita; a mesma instância se nada mudou.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="rewrite"/> é <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="rewrite"/> devolveu <see langword="null"/>.</exception>
    public static T RewriteExpressions<T>(this T node, Func<Expr, Expr> rewrite)
        where T : IVisitMut<T>
    {
        ThrowIfNull(node, nameof(node));
        ThrowIfNull(rewrite, nameof(rewrite));
        _ = Visitor.VisitExpressionsMut<T, ValueTuple>(ref node, (ref Expr expr) =>
        {
            expr = NotNull(rewrite(expr), nameof(rewrite));
            return ControlFlow<ValueTuple>.Continue;
        });
        return node;
    }

    /// <summary>
    /// Troca cada instrução de <paramref name="node"/>, inclusive as aninhadas, pelo valor que
    /// <paramref name="rewrite"/> devolver e devolve a árvore nova (Rust: <c>visit_statements_mut</c>, gancho
    /// <c>post</c>).
    /// </summary>
    /// <returns>A árvore reescrita; a mesma instância se nada mudou.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> ou <paramref name="rewrite"/> é <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="rewrite"/> devolveu <see langword="null"/>.</exception>
    public static T RewriteStatements<T>(this T node, Func<Statement, Statement> rewrite)
        where T : IVisitMut<T>
    {
        ThrowIfNull(node, nameof(node));
        ThrowIfNull(rewrite, nameof(rewrite));
        _ = Visitor.VisitStatementsMut<T, ValueTuple>(ref node, (ref Statement statement) =>
        {
            statement = NotNull(rewrite(statement), nameof(rewrite));
            return ControlFlow<ValueTuple>.Continue;
        });
        return node;
    }

    private static void ThrowIfNull<T>(T value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    private static T NotNull<T>(T? value, string function)
        where T : class =>
        value ?? throw new InvalidOperationException($"A função '{function}' devolveu null; devolva o próprio nó para mantê-lo.");
}
