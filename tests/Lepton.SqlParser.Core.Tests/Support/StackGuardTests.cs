// Testes próprios do port (não existem no sqlparser-rs): verificam o equivalente do
// `#[recursive::recursive]` (Support/StackGuard.cs) e a propagação de erros sem acúmulo de pilha
// (convencoes.md §13, casos 123 e 124; divergencias.md, DIV-005; docs/port/onda-d/D1.md).
// Cada teste roda numa thread com pilha pequena, para forçar trocas de pilha mesmo em entradas modestas.

using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Text;
using Lepton.SqlParser.Core.Ast;
using Lepton.SqlParser.Core.Dialects;

namespace Lepton.SqlParser.Core.Tests;

public class StackGuardTests
{
    // 256 KB: o mínimo que o .NET Framework respeita. Na zona vermelha de 128 KiB (.NET Core) ou de metade
    // da pilha (.NET Framework), sobra pouco para a recursão, então as trocas de pilha acontecem cedo.
    private const int PilhaPequena = 256 * 1024;

    private static T RodarNumaThread<T>(int maxStackSize, Func<T> f)
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
            maxStackSize);
        thread.Start();
        thread.Join();
        error?.Throw();
        return result;
    }

    private static string Aninhada(int depth) =>
        depth == 0 ? "x = 1" : $"(x = 1 OR x = 1 AND ({Aninhada(depth - 1)}))";

    private static Sequence<Statement> Parse(string sql, int recursionLimit) =>
        new Parser(new GenericDialect()).TryWithSql(sql).WithRecursionLimit(recursionLimit).ParseStatements();

    [Fact]
    public void Display_de_5000_somas_encadeadas_numa_pilha_pequena()
    {
        var sql = "SELECT " + string.Join(" + ", Enumerable.Repeat("1", 5000));

        var texto = RodarNumaThread(PilhaPequena, () => Parser.ParseSql(new GenericDialect(), sql)[0].ToString());

        Assert.Equal(sql, texto);
    }

    [Fact]
    public void Span_de_5000_somas_encadeadas_numa_pilha_pequena()
    {
        // Expr.Span() não tem #[recursive::recursive] no Rust; aqui recebe a mesma proteção do Display (DIV-005).
        var sql = "SELECT " + string.Join(" + ", Enumerable.Repeat("1", 5000));
        var statement = Parser.ParseSql(new GenericDialect(), sql)[0];

        var esperado = RodarNumaThread(64 * 1024 * 1024, statement.Span);
        var obtido = RodarNumaThread(PilhaPequena, statement.Span);

        Assert.Equal(esperado, obtido);
        Assert.Equal(sql.Length + 1, (int)obtido.End.Column);
    }

    [Fact]
    public void Parse_e_display_de_500_niveis_numa_pilha_pequena()
    {
        var sql = $"SELECT pk FROM tab0 WHERE {Aninhada(500)}";

        var texto = RodarNumaThread(PilhaPequena, () => Parse(sql, 5000)[0].ToString());

        Assert.Equal(sql, texto);
    }

    [Fact]
    public void Display_de_80_subconsultas_aninhadas_numa_pilha_pequena()
    {
        // Query.Fmt não tem #[recursive::recursive] no Rust; aqui recebe a proteção (DIV-005; revisão R4, item 2).
        var sql = string.Concat(Enumerable.Repeat("SELECT * FROM (", 80)) + "SELECT 1"
            + string.Concat(Enumerable.Repeat(") AS x", 80));

        var texto = RodarNumaThread(PilhaPequena, () => Parse(sql, 200)[0].ToString());

        Assert.Equal(sql, texto);
    }

    [Fact]
    public void Display_de_5000_UNION_ALL_numa_pilha_pequena()
    {
        // SetExpr.Fmt: a cadeia de operações de conjunto não é limitada pelo contador de recursão (DIV-005).
        var sql = string.Join(" UNION ALL ", Enumerable.Range(0, 5000).Select(i => $"SELECT {i}"));

        var texto = RodarNumaThread(PilhaPequena, () => Parser.ParseSql(new GenericDialect(), sql)[0].ToString());

        Assert.Equal(sql, texto);
    }

    [Fact]
    public void Display_de_200_juncoes_entre_parenteses_numa_pilha_pequena()
    {
        // TableFactor.Fmt: os níveis de NestedJoin não passam por Query.Fmt nem por Expr.Fmt (DIV-005).
        var sql = "SELECT * FROM " + new string('(', 200) + "a JOIN b ON a.id = b.id" + new string(')', 200);

        var texto = RodarNumaThread(PilhaPequena, () => Parse(sql, 1000)[0].ToString());

        Assert.Equal(sql, texto);
    }

    [Fact]
    public void Span_de_80_subconsultas_aninhadas_numa_pilha_pequena()
    {
        // Query.Span() e TableFactor.Span() não têm #[recursive::recursive] no Rust; aqui recebem a proteção
        // (DIV-005; unidade P2, tarefa 0). Antes, este Span() estourava a thread de 256 KB.
        var sql = string.Concat(Enumerable.Repeat("SELECT * FROM (", 80)) + "SELECT 1"
            + string.Concat(Enumerable.Repeat(") AS x", 80));
        var statement = Parse(sql, 200)[0];

        var esperado = RodarNumaThread(64 * 1024 * 1024, statement.Span);
        var obtido = RodarNumaThread(PilhaPequena, statement.Span);

        Assert.Equal(esperado, obtido);
        Assert.Equal(sql.Length + 1, (int)obtido.End.Column);
    }

    [Fact]
    public void Span_de_5000_UNION_ALL_numa_pilha_pequena()
    {
        // SetExpr.Span(): a cadeia de operações de conjunto não é limitada pelo contador de recursão (DIV-005).
        var sql = string.Join(" UNION ALL ", Enumerable.Range(0, 5000).Select(i => $"SELECT {i}"));
        var statement = Parser.ParseSql(new GenericDialect(), sql)[0];

        var esperado = RodarNumaThread(64 * 1024 * 1024, statement.Span);
        var obtido = RodarNumaThread(PilhaPequena, statement.Span);

        Assert.Equal(esperado, obtido);
        Assert.Equal(sql.Length + 1, (int)obtido.End.Column);
    }

    [Fact]
    public void Span_de_200_juncoes_entre_parenteses_numa_pilha_pequena()
    {
        // TableFactor.Span(): os níveis de NestedJoin não passam por Query.Span() nem por Expr.Span() (DIV-005).
        var sql = "SELECT * FROM " + new string('(', 200) + "a JOIN b ON a.id = b.id" + new string(')', 200);
        var statement = Parse(sql, 1000)[0];

        var esperado = RodarNumaThread(64 * 1024 * 1024, statement.Span);
        var obtido = RodarNumaThread(PilhaPequena, statement.Span);

        Assert.Equal(esperado, obtido);
    }

    [Fact]
    public void Erro_de_sintaxe_a_500_niveis_vira_ParserException()
    {
        // O erro sobe por centenas de quadros (e por várias trocas de pilha) até o chamador.
        var sql = $"SELECT pk FROM tab0 WHERE {Aninhada(500)}".Replace("AND (x = 1)", "AND (x = )");

        var ex = Assert.Throws<ParserException>(() => RodarNumaThread(PilhaPequena, () => Parse(sql, 5000)));

        var error = Assert.IsType<ParserError.ParserErrorValue>(ex.Error);
        Assert.StartsWith("Expected: an expression, found: )", error.Value);
    }

    [Fact]
    public void Limite_de_recursao_nao_depende_da_pilha_disponivel()
    {
        // Como no Rust, RecursionLimitExceeded só ocorre quando o contador chega a 0. O menor limite que
        // aceita a consulta é o mesmo numa pilha grande e numa pequena (antes, a falta de pilha também
        // virava RecursionLimitExceeded).
        var sql = "SELECT " + new string('(', 300) + "1" + new string(')', 300);

        static bool Aceita(string s, int limite)
        {
            try
            {
                Parse(s, limite);
                return true;
            }
            catch (ParserException e) when (e.Error is ParserError.RecursionLimitExceeded)
            {
                return false;
            }
        }

        var minimo = RodarNumaThread(64 * 1024 * 1024, () =>
        {
            int lo = 1, hi = 10_000;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (Aceita(sql, mid))
                {
                    hi = mid;
                }
                else
                {
                    lo = mid + 1;
                }
            }
            return lo;
        });

        Assert.True(RodarNumaThread(PilhaPequena, () => Aceita(sql, minimo)));
        Assert.False(RodarNumaThread(PilhaPequena, () => Aceita(sql, minimo - 1)));
    }

    [Fact]
    public void Grow_relanca_a_mesma_excecao_na_thread_original()
    {
        var original = new InvalidOperationException("falha");

        var ex = Assert.Throws<InvalidOperationException>(
            () => StackGuard.Grow<int>(() => throw original));

        Assert.Same(original, ex);
    }

    [Fact]
    public void Grow_roda_em_outra_thread_com_a_cultura_atual()
    {
        var anterior = CultureInfo.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-BR");
            var chamador = Thread.CurrentThread.ManagedThreadId;

            var (threadId, cultura) = StackGuard.Grow(
                () => (Thread.CurrentThread.ManagedThreadId, CultureInfo.CurrentCulture.Name));

            Assert.NotEqual(chamador, threadId);
            Assert.Equal("pt-BR", cultura);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = anterior;
        }
    }

    [Fact]
    public void Rethrow_fora_do_catch_preserva_o_rastro_original()
    {
        static void Lanca() => throw new ParserException(new ParserError.RecursionLimitExceeded());

        ParserException capturada;
        try
        {
            Lanca();
            return;
        }
        catch (ParserException e)
        {
            capturada = e;
        }

        System.Action relancar = () => throw StackGuard.Rethrow(capturada);
        var ex = Assert.Throws<ParserException>(relancar);

        Assert.Same(capturada, ex);
        Assert.Contains(nameof(Lanca), ex.StackTrace);
    }

    [Fact]
    public void IsStackLow_e_falso_com_pilha_folgada()
    {
        Assert.False(RodarNumaThread(4 * 1024 * 1024, StackGuard.IsStackLow));
    }

    [Fact]
    public void Display_profundo_continua_escrevendo_no_mesmo_formatador()
    {
        // A troca de pilha no meio do Expr.Fmt escreve no mesmo StringBuilder (pretty-print incluído).
        var sql = "SELECT " + string.Join(" + ", Enumerable.Repeat("1", 3000));
        var statement = Parser.ParseSql(new GenericDialect(), sql)[0];

        var texto = RodarNumaThread(PilhaPequena, () =>
        {
            var sb = new StringBuilder();
            statement.Fmt(new Formatter(sb, alternate: true));
            return sb.ToString();
        });

        Assert.Equal(Formatter.FormatAlternate(statement), texto);
        Assert.EndsWith("1 + 1", texto);
    }
}
