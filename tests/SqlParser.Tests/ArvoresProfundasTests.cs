// Testes próprios da API pública com árvores profundas (revisão R4, itens 1, 2 e 8; docs/port/onda-d/R4.md).
// Cada teste roda numa thread dedicada com pilha pequena (256 KB, o mínimo que o .NET Framework respeita e o
// cenário de IIS da revisão), para forçar trocas de pilha mesmo em entradas modestas. Um estouro de pilha
// derruba o processo inteiro: estes testes só existem depois das correções e cobrem apenas caminhos protegidos
// (DIV-005 e DIV-009 em docs/port/divergencias.md); a igualdade dos records (DIV-010) não é exercitada em
// profundidade aqui.

using System.Runtime.ExceptionServices;
using SqlParser.Core.Ast;

namespace SqlParser.Tests;

public class ArvoresProfundasTests
{
    private const int PilhaPequena = 256 * 1024;

    private static T RodarNumaThread<T>(Func<T> f)
    {
        T result = default!;
        ExceptionDispatchInfo? error = null;
        var thread = new Thread(
            () =>
            {
                try
                {
                    result = f();
                }
                catch (Exception e)
                {
                    error = ExceptionDispatchInfo.Capture(e);
                }
            },
            PilhaPequena);
        thread.Start();
        thread.Join();
        error?.Throw();
        return result;
    }

    private static string SubconsultasAninhadas(int depth) =>
        string.Concat(Enumerable.Repeat("SELECT * FROM (", depth)) + "SELECT 1" + string.Concat(Enumerable.Repeat(") AS x", depth));

    [Fact]
    public void ToSql_de_80_subconsultas_aninhadas_com_limite_elevado()
    {
        // R4, item 2: o parse aceitava (limite 200) e o ToSql estourava a pilha (Query.Fmt sem StackGuard).
        var sql = SubconsultasAninhadas(80);

        var (compacto, pretty) = RodarNumaThread(() =>
        {
            var statement = Assert.Single(new SqlQueryParser(recursionLimit: 200).Parse(sql));
            return (statement.ToSql(), statement.ToSql(pretty: true));
        });

        Assert.Equal(sql, compacto);
        Assert.StartsWith("SELECT\n  *\nFROM\n  (\n    SELECT\n", pretty);
    }

    [Fact]
    public void ToSql_de_200_juncoes_entre_parenteses_com_limite_elevado()
    {
        // TableFactor.Fmt: os níveis de NestedJoin não passam por Query.Fmt nem por Expr.Fmt.
        var sql = "SELECT * FROM " + new string('(', 200) + "a JOIN b ON a.id = b.id" + new string(')', 200);

        var texto = RodarNumaThread(() => Assert.Single(new SqlQueryParser(recursionLimit: 1000).Parse(sql)).ToSql());

        Assert.Equal(sql, texto);
    }

    [Fact]
    public void ToSql_de_5000_UNION_ALL_com_o_limite_padrao()
    {
        // SetExpr.Fmt: a cadeia de operações de conjunto não é limitada pelo contador de recursão e estourava a
        // pilha de 1 MB da thread principal com cerca de 5.000 termos (DIV-005).
        var sql = string.Join(" UNION ALL ", Enumerable.Range(0, 5000).Select(i => $"SELECT {i}"));

        var texto = RodarNumaThread(() => new SqlQueryParser().Parse(sql).ToSql());

        Assert.Equal(sql, texto);
    }

    [Fact]
    public void Comparar_ASTs_profundas_pelo_SQL_gerado()
    {
        // DIV-010: ==, Equals e GetHashCode dos records são recursivos e sem StackGuard; com 8.000 ORs estouravam a
        // pilha (R4, item 1). A forma segura documentada é comparar o SQL gerado, que é protegido.
        var sql = "SELECT * FROM t WHERE " + string.Join(" OR ", Enumerable.Range(0, 8000).Select(i => $"id = {i}"));

        var iguais = RodarNumaThread(() =>
        {
            var parser = new SqlQueryParser();
            return parser.Parse(sql).ToSql() == parser.Parse(sql).ToSql();
        });

        Assert.True(iguais);
    }

    [Fact]
    public void Ganchos_do_visitor_podem_rodar_em_outra_thread()
    {
        // R4, item 8 (DIV-009): numa árvore profunda, a travessia continua numa thread nova quando falta pilha, e os
        // ganchos seguintes rodam nela. O resultado é o mesmo, e a exceção de um gancho volta para quem chamou Visit.
        var sql = "SELECT * FROM t WHERE " + string.Join(" OR ", Enumerable.Range(0, 5000).Select(i => $"id = {i}"));
        var statements = new SqlQueryParser().Parse(sql);

        var (chamador, contador) = RodarNumaThread(() =>
        {
            var visitor = new ThreadRecorder();
            _ = statements.Visit(visitor);
            return (Environment.CurrentManagedThreadId, visitor);
        });

        Assert.Equal(4999 + 5000 + 2 * 5000, contador.Expressions); // 4.999 OR, 5.000 "=", 10.000 folhas
        Assert.Contains(chamador, contador.Threads);
        Assert.True(contador.Threads.Count > 1, "Esperava ganchos em mais de uma thread.");

        var erro = Assert.Throws<InvalidOperationException>(() => RodarNumaThread(() => statements.Visit(new FailingVisitor(4000))));
        Assert.Equal("gancho 4000", erro.Message);
    }

    private sealed class ThreadRecorder : SqlVisitor
    {
        public HashSet<int> Threads { get; } = new HashSet<int>();

        public int Expressions { get; private set; }

        public override ControlFlow PreVisitExpression(Expr expression)
        {
            Expressions++;
            _ = Threads.Add(Environment.CurrentManagedThreadId);
            return ControlFlow.Continue;
        }
    }

    private sealed class FailingVisitor : SqlVisitor
    {
        private readonly int _failAt;
        private int _count;

        public FailingVisitor(int failAt) => _failAt = failAt;

        public override ControlFlow PreVisitExpression(Expr expression) =>
            ++_count == _failAt ? throw new InvalidOperationException($"gancho {_failAt}") : ControlFlow.Continue;
    }
}
