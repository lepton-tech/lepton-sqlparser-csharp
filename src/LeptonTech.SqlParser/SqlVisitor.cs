// Código próprio da camada pública (sem origem no sqlparser-rs). Imita o formato de uso do Visitor do
// SqlParser-cs (classe base com PreVisit*/PostVisit* que devolvem ControlFlow, node.Visit(visitor)) sem
// copiar código dele. A travessia é a do Core (Visitor<TBreak>, gerado a partir do derive Visit do Rust):
// esta classe só repassa os ganchos. Ver docs/adr/0009-api-publica-fase-2.md.

using LeptonTech.SqlParser.Core;
using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser;

/// <summary>
/// Visitor somente leitura da AST, no estilo do SqlParser-cs: derive desta classe, sobrescreva os ganchos de
/// interesse e chame <c>node.Visit(visitor)</c> (<see cref="SqlVisitorExtensions.Visit{T}(T, SqlVisitor)"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Travessia:</b> é exatamente a do Core (<see cref="Visitor{TBreak}"/>), que é a do derive <c>Visit</c> do
/// sqlparser-rs: em profundidade, os campos de cada nó na ordem da declaração no Rust. <c>PreVisit*</c> é
/// chamado antes dos filhos do nó, e <c>PostVisit*</c>, depois. Esta classe não percorre nada: ela só adapta os
/// ganchos do Core (nomes e tipo de retorno).
/// </para>
/// <para>
/// <b>Interrupção:</b> um gancho que devolve <see cref="ControlFlow.Break"/> encerra a visita na hora; nenhum
/// outro gancho é chamado (nem os <c>PostVisit*</c> pendentes), e <c>Visit</c> devolve
/// <see cref="ControlFlow.Break"/>.
/// </para>
/// <para>
/// <b>Correspondência com o Core:</b> <c>PreVisitExpression</c> = <c>PreVisitExpr</c>,
/// <c>PreVisitIdentifier</c> = <c>PreVisitIdent</c> e <c>PreVisitOrderByExpression</c> = <c>PreVisitOrderByExpr</c>;
/// os demais têm o mesmo nome. Os tipos dos parâmetros são os da AST do Core.
/// </para>
/// <para>
/// Uma instância pode ser reutilizada em várias visitas, mas não em visitas simultâneas (o estado é o do
/// próprio visitor).
/// </para>
/// <para>
/// <b>Thread dos ganchos:</b> em árvores muito profundas (centenas de níveis no build Debug, como uma cadeia
/// longa de <c>OR</c>), a travessia do Core continua numa thread nova quando falta pilha, e os ganchos seguintes
/// rodam nela (DIV-009 em <c>docs/port/divergencias.md</c>; é o equivalente do <c>#[recursive::recursive]</c>
/// do Rust). Quem chamou <c>Visit</c> espera o fim, e uma exceção de um gancho chega a ele com o tipo e o rastro
/// de pilha originais. Os ganchos não devem depender da thread: estado <c>[ThreadStatic]</c> e
/// <c>ThreadLocal</c> não acompanha (a cultura e o <c>AsyncLocal</c> acompanham), e objetos presos a uma thread,
/// como controles do WinForms/WPF e objetos COM de apartamento STA, não podem ser usados dentro de um gancho.
/// Numa thread STA (a thread de UI), a espera bombeia mensagens COM, o que pode reentrar no código da interface.
/// Para atualizar a interface, junte os dados no visitor e use-os depois que <c>Visit</c> voltar, ou chame
/// <c>Visit</c> fora da thread de UI.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// sealed class TableCounter : SqlVisitor
/// {
///     public int Count { get; private set; }
///
///     public override ControlFlow PreVisitRelation(ObjectName relation)
///     {
///         Count++;
///         return ControlFlow.Continue;
///     }
/// }
///
/// var counter = new TableCounter();
/// new SqlQueryParser().Parse("SELECT * FROM a JOIN b ON a.id = b.id").Visit(counter);
/// // counter.Count == 2
/// </code>
/// </example>
public abstract class SqlVisitor
{
    private Visitor<ValueTuple>? _adapter;

    /// <summary>Construtor para as classes derivadas.</summary>
    protected SqlVisitor()
    {
    }

    /// <summary>O visitor do Core que repassa os ganchos para esta instância.</summary>
    internal Visitor<ValueTuple> CoreVisitor => _adapter ??= new Adapter(this);

    /// <summary>
    /// Chamado para cada <see cref="Query"/> (consulta da instrução, subconsultas, CTEs, tabelas derivadas...),
    /// antes dos filhos.
    /// </summary>
    public virtual ControlFlow PreVisitQuery(Query query) => ControlFlow.Continue;

    /// <summary>Chamado para cada <see cref="Query"/>, depois dos filhos.</summary>
    public virtual ControlFlow PostVisitQuery(Query query) => ControlFlow.Continue;

    /// <summary>
    /// Chamado para cada bloco <see cref="Select"/> (cada lado de um <c>UNION</c> é um bloco), antes dos filhos.
    /// </summary>
    public virtual ControlFlow PreVisitSelect(Select select) => ControlFlow.Continue;

    /// <summary>Chamado para cada bloco <see cref="Select"/>, depois dos filhos.</summary>
    public virtual ControlFlow PostVisitSelect(Select select) => ControlFlow.Continue;

    /// <summary>
    /// Chamado para cada relação (nome de tabela, visão etc.), antes dos filhos. São os campos que o
    /// sqlparser-rs marca com <c>visit(with = "visit_relation")</c>: o nome em <c>TableFactor.Table</c>
    /// (<c>FROM</c>, <c>JOIN</c>, alvo de <c>UPDATE</c>/<c>DELETE</c>/<c>MERGE</c>), o alvo de <c>INSERT INTO</c>,
    /// os nomes criados ou alterados por <c>CREATE TABLE</c>/<c>VIEW</c>/<c>VIRTUAL TABLE</c>,
    /// <c>ALTER TABLE</c>/<c>VIEW</c>, a tabela de <c>CREATE INDEX</c>, <c>TRUNCATE</c>, <c>LOCK</c>,
    /// <c>SHOW ... FROM</c>, entre outros (lista completa no ADR-0009).
    /// </summary>
    /// <remarks>Os nomes de <c>DROP TABLE</c> não são relações no sqlparser-rs v0.63.0.</remarks>
    public virtual ControlFlow PreVisitRelation(ObjectName relation) => ControlFlow.Continue;

    /// <summary>Chamado para cada relação, depois dos filhos.</summary>
    public virtual ControlFlow PostVisitRelation(ObjectName relation) => ControlFlow.Continue;

    /// <summary>
    /// Chamado para cada <see cref="TableFactor"/> (item de <c>FROM</c> ou de <c>JOIN</c>, tabela derivada,
    /// função de tabela...), antes dos filhos.
    /// </summary>
    public virtual ControlFlow PreVisitTableFactor(TableFactor tableFactor) => ControlFlow.Continue;

    /// <summary>Chamado para cada <see cref="TableFactor"/>, depois dos filhos.</summary>
    public virtual ControlFlow PostVisitTableFactor(TableFactor tableFactor) => ControlFlow.Continue;

    /// <summary>Chamado para cada expressão (<see cref="Expr"/>), antes dos filhos (Core: <c>PreVisitExpr</c>).</summary>
    public virtual ControlFlow PreVisitExpression(Expr expression) => ControlFlow.Continue;

    /// <summary>Chamado para cada expressão, depois dos filhos (Core: <c>PostVisitExpr</c>).</summary>
    public virtual ControlFlow PostVisitExpression(Expr expression) => ControlFlow.Continue;

    /// <summary>
    /// Chamado para cada instrução (<see cref="Statement"/>), antes dos filhos. Inclui instruções aninhadas
    /// (por exemplo, a de um <c>EXPLAIN</c> ou dos blocos de <c>IF</c>/<c>CASE</c>).
    /// </summary>
    public virtual ControlFlow PreVisitStatement(Statement statement) => ControlFlow.Continue;

    /// <summary>Chamado para cada instrução, depois dos filhos.</summary>
    public virtual ControlFlow PostVisitStatement(Statement statement) => ControlFlow.Continue;

    /// <summary>Chamado para cada valor literal (<see cref="ValueWithSpan"/>), antes dos filhos.</summary>
    public virtual ControlFlow PreVisitValue(ValueWithSpan value) => ControlFlow.Continue;

    /// <summary>Chamado para cada valor literal, depois dos filhos.</summary>
    public virtual ControlFlow PostVisitValue(ValueWithSpan value) => ControlFlow.Continue;

    /// <summary>
    /// Chamado para cada identificador (<see cref="Ident"/>) da AST, antes dos filhos (Core: <c>PreVisitIdent</c>).
    /// </summary>
    public virtual ControlFlow PreVisitIdentifier(Ident identifier) => ControlFlow.Continue;

    /// <summary>Chamado para cada identificador, depois dos filhos (Core: <c>PostVisitIdent</c>).</summary>
    public virtual ControlFlow PostVisitIdentifier(Ident identifier) => ControlFlow.Continue;

    /// <summary>Chamado para cada cláusula <c>ORDER BY</c> (<see cref="OrderBy"/>), antes dos filhos.</summary>
    public virtual ControlFlow PreVisitOrderBy(OrderBy orderBy) => ControlFlow.Continue;

    /// <summary>Chamado para cada cláusula <c>ORDER BY</c>, depois dos filhos.</summary>
    public virtual ControlFlow PostVisitOrderBy(OrderBy orderBy) => ControlFlow.Continue;

    /// <summary>
    /// Chamado para cada item de <c>ORDER BY</c> (<see cref="OrderByExpr"/>), antes dos filhos
    /// (Core: <c>PreVisitOrderByExpr</c>).
    /// </summary>
    public virtual ControlFlow PreVisitOrderByExpression(OrderByExpr orderByExpression) => ControlFlow.Continue;

    /// <summary>Chamado para cada item de <c>ORDER BY</c>, depois dos filhos (Core: <c>PostVisitOrderByExpr</c>).</summary>
    public virtual ControlFlow PostVisitOrderByExpression(OrderByExpr orderByExpression) => ControlFlow.Continue;

    /// <summary>
    /// Chamado para cada cláusula <c>GROUP BY</c> (<see cref="GroupByExpr"/>), antes dos filhos. Todo
    /// <see cref="Select"/> tem uma, mesmo sem <c>GROUP BY</c> no texto (lista vazia), como no Rust.
    /// </summary>
    public virtual ControlFlow PreVisitGroupBy(GroupByExpr groupBy) => ControlFlow.Continue;

    /// <summary>Chamado para cada cláusula <c>GROUP BY</c>, depois dos filhos.</summary>
    public virtual ControlFlow PostVisitGroupBy(GroupByExpr groupBy) => ControlFlow.Continue;

    /// <summary>Converte o resultado de um gancho no <c>ControlFlow&lt;ValueTuple&gt;</c> do Core.</summary>
    internal static ControlFlow<ValueTuple> ToCore(ControlFlow flow) =>
        flow == ControlFlow.Break ? ControlFlow<ValueTuple>.Break(default) : ControlFlow<ValueTuple>.Continue;

    /// <summary>Converte o resultado de uma visita do Core no <see cref="ControlFlow"/> público.</summary>
    internal static ControlFlow FromCore(ControlFlow<ValueTuple> flow) =>
        flow.IsBreak ? ControlFlow.Break : ControlFlow.Continue;

    /// <summary>Repassa cada gancho do visitor do Core para o gancho correspondente do <see cref="SqlVisitor"/>.</summary>
    private sealed class Adapter : Visitor<ValueTuple>
    {
        private readonly SqlVisitor _visitor;

        public Adapter(SqlVisitor visitor) => _visitor = visitor;

        public override ControlFlow<ValueTuple> PreVisitQuery(Query query) => ToCore(_visitor.PreVisitQuery(query));

        public override ControlFlow<ValueTuple> PostVisitQuery(Query query) => ToCore(_visitor.PostVisitQuery(query));

        public override ControlFlow<ValueTuple> PreVisitSelect(Select select) => ToCore(_visitor.PreVisitSelect(select));

        public override ControlFlow<ValueTuple> PostVisitSelect(Select select) => ToCore(_visitor.PostVisitSelect(select));

        public override ControlFlow<ValueTuple> PreVisitRelation(ObjectName relation) => ToCore(_visitor.PreVisitRelation(relation));

        public override ControlFlow<ValueTuple> PostVisitRelation(ObjectName relation) => ToCore(_visitor.PostVisitRelation(relation));

        public override ControlFlow<ValueTuple> PreVisitTableFactor(TableFactor tableFactor) =>
            ToCore(_visitor.PreVisitTableFactor(tableFactor));

        public override ControlFlow<ValueTuple> PostVisitTableFactor(TableFactor tableFactor) =>
            ToCore(_visitor.PostVisitTableFactor(tableFactor));

        public override ControlFlow<ValueTuple> PreVisitExpr(Expr expr) => ToCore(_visitor.PreVisitExpression(expr));

        public override ControlFlow<ValueTuple> PostVisitExpr(Expr expr) => ToCore(_visitor.PostVisitExpression(expr));

        public override ControlFlow<ValueTuple> PreVisitStatement(Statement statement) => ToCore(_visitor.PreVisitStatement(statement));

        public override ControlFlow<ValueTuple> PostVisitStatement(Statement statement) => ToCore(_visitor.PostVisitStatement(statement));

        public override ControlFlow<ValueTuple> PreVisitValue(ValueWithSpan value) => ToCore(_visitor.PreVisitValue(value));

        public override ControlFlow<ValueTuple> PostVisitValue(ValueWithSpan value) => ToCore(_visitor.PostVisitValue(value));

        public override ControlFlow<ValueTuple> PreVisitIdent(Ident ident) => ToCore(_visitor.PreVisitIdentifier(ident));

        public override ControlFlow<ValueTuple> PostVisitIdent(Ident ident) => ToCore(_visitor.PostVisitIdentifier(ident));

        public override ControlFlow<ValueTuple> PreVisitOrderBy(OrderBy orderBy) => ToCore(_visitor.PreVisitOrderBy(orderBy));

        public override ControlFlow<ValueTuple> PostVisitOrderBy(OrderBy orderBy) => ToCore(_visitor.PostVisitOrderBy(orderBy));

        public override ControlFlow<ValueTuple> PreVisitOrderByExpr(OrderByExpr orderByExpr) =>
            ToCore(_visitor.PreVisitOrderByExpression(orderByExpr));

        public override ControlFlow<ValueTuple> PostVisitOrderByExpr(OrderByExpr orderByExpr) =>
            ToCore(_visitor.PostVisitOrderByExpression(orderByExpr));

        public override ControlFlow<ValueTuple> PreVisitGroupBy(GroupByExpr groupBy) => ToCore(_visitor.PreVisitGroupBy(groupBy));

        public override ControlFlow<ValueTuple> PostVisitGroupBy(GroupByExpr groupBy) => ToCore(_visitor.PostVisitGroupBy(groupBy));
    }
}
