extern alias SqlParserCs;

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;
using Lepton.SqlParser.Core;
using Lepton.SqlParser.Core.Ast;
using Action = System.Action;

namespace Lepton.SqlParser.Bench;

/// <summary>Validação das cargas: quais instruções cada biblioteca aceita em cada dialeto.</summary>
public static class Validation
{
    public static bool BothParse(DialectKind dialect, string sql) =>
        OursError(dialect, sql) is null && CsError(dialect, sql) is null;

    public static string? OursError(DialectKind dialect, string sql)
    {
        try
        {
            Libs.OursParse(DialectFactory.Ours(dialect), sql);
            return null;
        }
        catch (Exception e)
        {
            return e.GetType().Name + ": " + e.Message;
        }
    }

    public static string? CsError(DialectKind dialect, string sql)
    {
        try
        {
            Libs.CsParse(DialectFactory.Cs(dialect), sql);
            return null;
        }
        catch (Exception e)
        {
            return e.GetType().Name + ": " + e.Message;
        }
    }
}

/// <summary>
/// Modo <c>--analyze</c>: métricas que explicam os tempos (sem BenchmarkDotNet): aceitação por dialeto,
/// tokens, nós Expr (≈ chamadas de ParseSubexpr/Expr.Fmt, onde o StackGuard verifica a pilha) e exceções
/// de primeira chance lançadas por parse (fluxo de controle por exceção), com os métodos que as lançam.
/// </summary>
public static class Analysis
{
    private static int _counting;
    private static readonly Dictionary<string, int> Throwers = new();
    private static int _thrown;
    private static int _distinct;
    private static readonly HashSet<Exception> Seen = new();

    public static void Run()
    {
        AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;
        Console.WriteLine($"# Análise ({RuntimeLabel()})");
        Console.WriteLine();

        Console.WriteLine("## Aceitação das consultas por dialeto");
        Console.WriteLine();
        foreach (DialectKind dialect in Enum.GetValues(typeof(DialectKind)))
        {
            var failures = new List<string>();
            for (var i = 0; i < Workloads.Tpch.Length; i++)
            {
                var o = Validation.OursError(dialect, Workloads.Tpch[i]);
                var c = Validation.CsError(dialect, Workloads.Tpch[i]);
                if (o is not null)
                {
                    failures.Add($"TPC-H {i + 1} (nosso): {Trim(o)}");
                }
                if (c is not null)
                {
                    failures.Add($"TPC-H {i + 1} (SqlParser-cs): {Trim(c)}");
                }
            }
            foreach (var name in Workloads.PipelineNames.Skip(1))
            {
                var sql = Workloads.Get(name)[0];
                var o = Validation.OursError(dialect, sql);
                var c = Validation.CsError(dialect, sql);
                if (o is not null)
                {
                    failures.Add($"{name} (nosso): {Trim(o)}");
                }
                if (c is not null)
                {
                    failures.Add($"{name} (SqlParser-cs): {Trim(c)}");
                }
            }
            Console.WriteLine($"- {dialect}: {(failures.Count == 0 ? "tudo aceito pelas duas bibliotecas" : failures.Count + " falha(s)")}");
            foreach (var f in failures)
            {
                Console.WriteLine($"  - {f}");
            }
        }
        Console.WriteLine();

        Console.WriteLine("## Aninhamento profundo com os limites padrão (50 nas duas)");
        Console.WriteLine();
        foreach (var depth in new[] { 10, 20, 30, 40, 45 })
        {
            foreach (var (shape, sql) in new[] { ("Parens", Workloads.NestedParens(depth)), ("Subqueries", Workloads.NestedSubqueries(depth)) })
            {
                var o = Validation.OursError(DialectKind.Generic, sql);
                var c = Validation.CsError(DialectKind.Generic, sql);
                Console.WriteLine($"- {shape} {depth}: nosso = {(o is null ? "ok" : Trim(o))}; SqlParser-cs = {(c is null ? "ok" : Trim(c))}");
            }
        }
        Console.WriteLine();

        Console.WriteLine("## Métricas por carga (GenericDialect, por execução da carga inteira)");
        Console.WriteLine();
        Console.WriteLine("| Carga | Bytes SQL | Tokens (nosso) | Tokens (cs) | Nós Expr (nosso) | Exceções lançadas (nosso) | Despachos (nosso) | Exceções lançadas (cs) |");
        Console.WriteLine("|---|---:|---:|---:|---:|---:|---:|---:|");
        var oursDialect = DialectFactory.Ours(DialectKind.Generic);
        var csDialect = DialectFactory.Cs(DialectKind.Generic);
        var throwersByWorkload = new List<(string, Dictionary<string, int>)>();
        foreach (var name in Workloads.PipelineNames)
        {
            var sqls = Workloads.Get(name);
            var bytes = sqls.Sum(s => s.Length);
            var oursTokens = sqls.Sum(s => Libs.OursTokenize(oursDialect, s));
            var csTokens = sqls.Sum(s => Libs.CsTokenize(csDialect, s));
            var exprs = 0;
            foreach (var sql in sqls)
            {
                Libs.OursParse(oursDialect, sql).VisitExpressions((Expr _) => exprs++);
            }
            var oursExc = CountExceptions(() =>
            {
                foreach (var sql in sqls)
                {
                    Libs.OursParse(oursDialect, sql);
                }
            }, out var oursThrowers);
            var csExc = CountExceptions(() =>
            {
                foreach (var sql in sqls)
                {
                    Libs.CsParse(csDialect, sql);
                }
            }, out var csThrowers);
            Console.WriteLine($"| {name} | {bytes:N0} | {oursTokens:N0} | {csTokens:N0} | {exprs:N0} | {oursExc.Distinct:N0} | {oursExc.Dispatches:N0} | {csExc.Distinct:N0} |");
            throwersByWorkload.Add((name + " (nosso)", oursThrowers));
            throwersByWorkload.Add((name + " (cs)", csThrowers));
        }
        Console.WriteLine();

        Console.WriteLine("## Onde as exceções são lançadas e quem as captura (exceções distintas; top 12)");
        Console.WriteLine();
        foreach (var (name, throwers) in throwersByWorkload)
        {
            if (throwers.Count == 0)
            {
                continue;
            }
            Console.WriteLine($"- {name}:");
            foreach (var kv in throwers.OrderByDescending(kv => kv.Value).Take(12))
            {
                Console.WriteLine($"  - {kv.Value} × {kv.Key}");
            }
        }
        Console.WriteLine();

        Console.WriteLine("## Exceções por consulta TPC-H (GenericDialect)");
        Console.WriteLine();
        Console.WriteLine("| Consulta | Exceções (nosso) | Despachos (nosso) | Exceções (cs) | Tempo nosso (us, Stopwatch) | Tempo cs (us, Stopwatch) |");
        Console.WriteLine("|---|---:|---:|---:|---:|---:|");
        for (var i = 0; i < Workloads.Tpch.Length; i++)
        {
            var sql = Workloads.Tpch[i];
            var o = CountExceptions(() => Libs.OursParse(oursDialect, sql), out _);
            var c = CountExceptions(() => Libs.CsParse(csDialect, sql), out _);
            var to = Time(() => Libs.OursParse(oursDialect, sql));
            var tc = Time(() => Libs.CsParse(csDialect, sql));
            Console.WriteLine($"| {i + 1} | {o.Distinct} | {o.Dispatches} | {c.Distinct} | {to:N1} | {tc:N1} |");
        }
        AppDomain.CurrentDomain.FirstChanceException -= OnFirstChance;
    }

    public static string RuntimeLabel() =>
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    private static (int Distinct, int Dispatches) CountExceptions(Action action, out Dictionary<string, int> throwers)
    {
        lock (Throwers)
        {
            Throwers.Clear();
            Seen.Clear();
        }
        _thrown = 0;
        _distinct = 0;
        Interlocked.Exchange(ref _counting, 1);
        try
        {
            action();
        }
        finally
        {
            Interlocked.Exchange(ref _counting, 0);
        }
        lock (Throwers)
        {
            throwers = new Dictionary<string, int>(Throwers);
        }
        return (_distinct, _thrown);
    }

    private static void OnFirstChance(object? sender, FirstChanceExceptionEventArgs e)
    {
        if (Volatile.Read(ref _counting) == 0)
        {
            return;
        }
        // Cada despacho conta (throw original e relançamentos por ExceptionDispatchInfo); a mesma instância
        // relançada conta uma vez só como "exceção distinta".
        Interlocked.Increment(ref _thrown);
        lock (Throwers)
        {
            if (Seen.Add(e.Exception))
            {
                _distinct++;
                var key = $"{e.Exception.GetType().Name} lançada em {Describe(e.Exception.TargetSite)}, especulação de {SpeculationSite()}";
                Throwers[key] = Throwers.TryGetValue(key, out var n) ? n + 1 : 1;
            }
        }
    }

    /// <summary>
    /// Método que chamou o MaybeParse/TryParse mais interno ativo no momento do throw (quem vai capturar a
    /// exceção). Na notificação de primeira chance a pilha ainda não foi desenrolada. Atribuição aproximada:
    /// um catch próprio no meio do caminho não é detectado.
    /// </summary>
    private static string SpeculationSite()
    {
        var frames = new StackTrace(false).GetFrames() ?? System.Array.Empty<StackFrame>();
        for (var i = 0; i < frames.Length; i++)
        {
            if (!IsSpeculation(frames[i].GetMethod()?.Name))
            {
                continue;
            }
            var j = i;
            while (j < frames.Length && IsSpeculation(frames[j].GetMethod()?.Name))
            {
                j++;
            }
            return j < frames.Length ? Describe(frames[j].GetMethod()) : "?";
        }
        return "(nenhuma: fora de MaybeParse/TryParse)";
    }

    private static bool IsSpeculation(string? name) => name is "TryParse" or "MaybeParse" or "MaybeParseValue";

    /// <summary>Nome do método; para lambdas (<c>&lt;Metodo&gt;b__0</c>), o do método que as contém.</summary>
    private static string Describe(System.Reflection.MethodBase? method)
    {
        if (method is null)
        {
            return "?";
        }
        var name = method.Name;
        if (name.StartsWith("<", StringComparison.Ordinal) && name.IndexOf('>') > 1)
        {
            name = name.Substring(1, name.IndexOf('>') - 1) + " (lambda)";
        }
        var type = method.DeclaringType;
        while (type is not null && type.Name.StartsWith("<", StringComparison.Ordinal))
        {
            type = type.DeclaringType;
        }
        return (type?.Name ?? "?") + "." + name;
    }

    /// <summary>Mediana em microssegundos de várias execuções, depois de aquecer (só indicativo).</summary>
    private static double Time(Action action)
    {
        for (var i = 0; i < 50; i++)
        {
            action();
        }
        var samples = new List<double>();
        var sw = new Stopwatch();
        for (var r = 0; r < 15; r++)
        {
            sw.Restart();
            for (var i = 0; i < 20; i++)
            {
                action();
            }
            sw.Stop();
            samples.Add(sw.Elapsed.TotalMilliseconds * 1000.0 / 20);
        }
        samples.Sort();
        return samples[samples.Count / 2];
    }

    internal static string Trim(string s) => s.Length > 160 ? s.Substring(0, 160) + "..." : s;
}

/// <summary>
/// Modo <c>--crosscheck</c>: compara o SQL gerado pelas duas bibliotecas (o nosso <c>ToString()</c> e o
/// <c>ToSql()</c> do SqlParser-cs) para cada instrução. As duas derivam do <c>Display</c> do Rust; diferença
/// indica divergência em uma delas. Também confere a estabilidade do nosso round trip.
/// </summary>
public static class CrossCheck
{
    public static void Run()
    {
        Console.WriteLine($"# Comparação de ToString ({Analysis.RuntimeLabel()})");
        foreach (DialectKind dialect in Enum.GetValues(typeof(DialectKind)))
        {
            Console.WriteLine();
            Console.WriteLine($"## {dialect}");
            var ours = DialectFactory.Ours(dialect);
            var cs = DialectFactory.Cs(dialect);
            var cases = new List<(string Name, string Sql)>();
            for (var i = 0; i < Workloads.Tpch.Length; i++)
            {
                cases.Add(($"TPC-H {i + 1}", Workloads.Tpch[i]));
            }
            cases.Add(("Oltp", Workloads.Oltp));
            cases.Add(("Insert1000", Workloads.Insert(1000)));
            cases.Add(("Select300", Workloads.SelectColumns(300)));
            cases.Add(("Where200", Workloads.WherePredicates(200)));
            cases.Add(("Parens40", Workloads.NestedParens(40)));
            cases.Add(("Subqueries40", Workloads.NestedSubqueries(40)));
            var equal = 0;
            foreach (var (name, sql) in cases)
            {
                string[] o, c;
                try
                {
                    o = Libs.OursStrings(Libs.OursParse(ours, sql, DeepNestingBenchmarks.RecursionLimit)).ToArray();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"- {name}: nosso parser falhou: {Analysis.Trim(e.Message)}");
                    continue;
                }
                try
                {
                    c = Libs.CsStrings(Libs.CsParse(cs, sql, new SqlParserCs::SqlParser.ParserOptions { RecursionLimit = DeepNestingBenchmarks.RecursionLimit, TrailingCommas = cs.SupportsTrailingCommas })).ToArray();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"- {name}: SqlParser-cs falhou: {Analysis.Trim(e.Message)}");
                    continue;
                }
                if (o.Length != c.Length)
                {
                    Console.WriteLine($"- {name}: número de instruções diferente ({o.Length} × {c.Length})");
                    continue;
                }
                for (var k = 0; k < o.Length; k++)
                {
                    // Estabilidade do nosso round trip: parse(ToString(ast)).ToString() == ToString(ast).
                    var again = string.Join("; ", Libs.OursStrings(Libs.OursParse(ours, o[k], DeepNestingBenchmarks.RecursionLimit)));
                    if (again != o[k])
                    {
                        Console.WriteLine($"- {name}#{k + 1}: nosso round trip não é estável");
                    }
                    if (o[k] == c[k])
                    {
                        equal++;
                        continue;
                    }
                    Console.WriteLine($"- {name}{(o.Length > 1 ? "#" + (k + 1) : "")}: diferente");
                    foreach (var line in WordDiff(o[k], c[k]))
                    {
                        Console.WriteLine("  " + line);
                    }
                }
            }
            Console.WriteLine($"- instruções com saída idêntica: {equal}");
        }
    }

    /// <summary>Diferença por palavras (LCS): "- nosso" / "+ cs", com um pouco de contexto.</summary>
    private static IEnumerable<string> WordDiff(string a, string b)
    {
        var x = Split(a);
        var y = Split(b);
        var n = x.Length;
        var m = y.Length;
        if ((long)n * m > 4_000_000)
        {
            yield return $"(texto grande: {n} × {m} palavras) nosso: {Analysis.Trim(a)}";
            yield return $"cs: {Analysis.Trim(b)}";
            yield break;
        }
        var lcs = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
        {
            for (var j = m - 1; j >= 0; j--)
            {
                lcs[i, j] = x[i] == y[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
            }
        }
        var ii = 0;
        var jj = 0;
        var shown = 0;
        while (ii < n || jj < m)
        {
            if (ii < n && jj < m && x[ii] == y[jj])
            {
                ii++;
                jj++;
                continue;
            }
            var startI = ii;
            var startJ = jj;
            while ((ii < n || jj < m) && !(ii < n && jj < m && x[ii] == y[jj]))
            {
                if (jj >= m || (ii < n && lcs[ii + 1, jj] >= lcs[ii, jj + 1]))
                {
                    ii++;
                }
                else
                {
                    jj++;
                }
            }
            var before = string.Join(" ", x.Skip(Math.Max(0, startI - 4)).Take(Math.Min(4, startI)));
            var after = string.Join(" ", x.Skip(ii).Take(2));
            var removed = string.Join(" ", x.Skip(startI).Take(ii - startI));
            var added = string.Join(" ", y.Skip(startJ).Take(jj - startJ));
            yield return $"`... {before} [nosso: {(removed.Length == 0 ? "(nada)" : removed)} | cs: {(added.Length == 0 ? "(nada)" : added)}] {after} ...`";
            if (++shown >= 12)
            {
                yield return "(mais diferenças omitidas)";
                yield break;
            }
        }
    }

    private static string[] Split(string s) =>
        s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
}
