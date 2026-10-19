extern alias SqlParserCs;

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using LeptonTech.SqlParser.Core;
using LeptonTech.SqlParser.Core.Ast;
using Cs = SqlParserCs::SqlParser;
using CoreDialect = LeptonTech.SqlParser.Core.Dialects.Dialect;
using CsAst = SqlParserCs::SqlParser.Ast;
using CsDialect = SqlParserCs::SqlParser.Dialects.Dialect;

namespace LeptonTech.SqlParser.Bench;

/// <summary>
/// Cargas (1) e (2) nos três dialetos: parse pelo Core, pela API pública e pelo SqlParser-cs (base da razão).
/// Consultas que alguma das bibliotecas não aceita no dialeto ficam de fora (lista em <c>--analyze</c>).
/// </summary>
[MemoryDiagnoser]
public class DialectBenchmarks
{
    [Params(DialectKind.Generic, DialectKind.PostgreSql, DialectKind.MsSql)]
    public DialectKind Dialect;

    [Params("Tpch22", "Oltp")]
    public string Workload = "";

    private CoreDialect _ours = null!;
    private CsDialect _cs = null!;
    private string[] _sql = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ours = DialectFactory.Ours(Dialect);
        _cs = DialectFactory.Cs(Dialect);
        _sql = Workloads.Get(Workload).Where(sql => Validation.BothParse(Dialect, sql)).ToArray();
        Console.WriteLine($"// {Workload}/{Dialect}: {_sql.Length} instrução(ões) aceitas pelas duas bibliotecas");
    }

    [Benchmark]
    public int Core_Parse()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.OursParse(_ours, sql).Count;
        }
        return n;
    }

    [Benchmark]
    public int Api_Parse()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.ApiParse(_ours, sql).Count;
        }
        return n;
    }

    [Benchmark(Baseline = true)]
    public int Cs_Parse()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.CsParse(_cs, sql).Count;
        }
        return n;
    }
}

/// <summary>
/// Cargas (1) a (5) no GenericDialect: tokenização, parse, ToString/ToSql (AST já pronta) e ida e volta
/// (parse + ToString). Em cada categoria, a base da razão é o SqlParser-cs.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class PipelineBenchmarks
{
    [ParamsSource(nameof(Workloads_))]
    public string Workload = "";

    public static IEnumerable<string> Workloads_ => Workloads.PipelineNames;

    private CoreDialect _ours = null!;
    private CsDialect _cs = null!;
    private string[] _sql = null!;
    private Sequence<Statement>[] _oursAst = null!;
    private Cs.Sequence<CsAst.Statement>[] _csAst = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ours = DialectFactory.Ours(DialectKind.Generic);
        _cs = DialectFactory.Cs(DialectKind.Generic);
        _sql = Workloads.Get(Workload);
        _oursAst = _sql.Select(sql => Libs.OursParse(_ours, sql)).ToArray();
        _csAst = _sql.Select(sql => Libs.CsParse(_cs, sql)).ToArray();
    }

    [Benchmark, BenchmarkCategory("1-Tokenize")]
    public int Core_Tokenize()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.OursTokenize(_ours, sql);
        }
        return n;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("1-Tokenize")]
    public int Cs_Tokenize()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.CsTokenize(_cs, sql);
        }
        return n;
    }

    [Benchmark, BenchmarkCategory("2-Parse")]
    public int Core_Parse()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.OursParse(_ours, sql).Count;
        }
        return n;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("2-Parse")]
    public int Cs_Parse()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.CsParse(_cs, sql).Count;
        }
        return n;
    }

    [Benchmark, BenchmarkCategory("3-ToString")]
    public int Core_ToString()
    {
        var n = 0;
        foreach (var ast in _oursAst)
        {
            n += Libs.OursToString(ast);
        }
        return n;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("3-ToString")]
    public int Cs_ToSql()
    {
        var n = 0;
        foreach (var ast in _csAst)
        {
            n += Libs.CsToSql(ast);
        }
        return n;
    }

    [Benchmark, BenchmarkCategory("4-RoundTrip")]
    public int Core_RoundTrip()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.OursToString(Libs.OursParse(_ours, sql));
        }
        return n;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("4-RoundTrip")]
    public int Cs_RoundTrip()
    {
        var n = 0;
        foreach (var sql in _sql)
        {
            n += Libs.CsToSql(Libs.CsParse(_cs, sql));
        }
        return n;
    }
}

/// <summary>Carga (1) consulta a consulta (GenericDialect), para achar as consultas em que perdemos mais.</summary>
[MemoryDiagnoser]
[WarmupCount(2)]
[IterationCount(6)]
public class TpchPerQueryBenchmarks
{
    [ParamsSource(nameof(Queries))]
    public int Query;

    public static IEnumerable<int> Queries => Enumerable.Range(1, 22);

    private CoreDialect _ours = null!;
    private CsDialect _cs = null!;
    private string _sql = "";

    [GlobalSetup]
    public void Setup()
    {
        _ours = DialectFactory.Ours(DialectKind.Generic);
        _cs = DialectFactory.Cs(DialectKind.Generic);
        _sql = Workloads.Tpch[Query - 1];
    }

    [Benchmark]
    public int Core_Parse() => Libs.OursParse(_ours, _sql).Count;

    [Benchmark(Baseline = true)]
    public int Cs_Parse() => Libs.CsParse(_cs, _sql).Count;
}

/// <summary>
/// Aninhamento profundo (custo indireto do StackGuard: as funções recursivas do Core verificam a pilha a cada
/// chamada). Limite de recursão 200 nas duas bibliotecas, para nenhuma recusar a profundidade 40.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DeepNestingBenchmarks
{
    public const int RecursionLimit = 200;

    [Params("Parens", "Subqueries")]
    public string Shape = "";

    [Params(1, 40)]
    public int Depth;

    private CoreDialect _ours = null!;
    private CsDialect _cs = null!;
    private Cs.ParserOptions _csOptions = null!;
    private string _sql = "";
    private Sequence<Statement> _oursAst;
    private Cs.Sequence<CsAst.Statement> _csAst = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ours = DialectFactory.Ours(DialectKind.Generic);
        _cs = DialectFactory.Cs(DialectKind.Generic);
        _csOptions = new Cs.ParserOptions { RecursionLimit = RecursionLimit, TrailingCommas = _cs.SupportsTrailingCommas };
        _sql = Shape == "Parens" ? Workloads.NestedParens(Depth) : Workloads.NestedSubqueries(Depth);
        _oursAst = Libs.OursParse(_ours, _sql, RecursionLimit);
        _csAst = Libs.CsParse(_cs, _sql, _csOptions);
    }

    [Benchmark, BenchmarkCategory("1-Parse")]
    public int Core_Parse() => Libs.OursParse(_ours, _sql, RecursionLimit).Count;

    [Benchmark(Baseline = true), BenchmarkCategory("1-Parse")]
    public int Cs_Parse() => Libs.CsParse(_cs, _sql, _csOptions).Count;

    [Benchmark, BenchmarkCategory("2-ToString")]
    public int Core_ToString() => Libs.OursToString(_oursAst);

    [Benchmark(Baseline = true), BenchmarkCategory("2-ToString")]
    public int Cs_ToSql() => Libs.CsToSql(_csAst);
}

/// <summary>
/// Microbenchmarks para estimar custos que não dá para isolar sem alterar o Core:
/// a verificação do StackGuard (réplica exata de <c>StackGuard.IsStackLow</c>, que é internal), o
/// construtor do <c>Parser</c> e o lançamento de <c>ParserException</c> (fluxo de controle por exceção).
/// </summary>
[MemoryDiagnoser]
public class MicroBenchmarks
{
    private const int N = 1000;
    private CoreDialect _ours = null!;
    private CsDialect _cs = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ours = DialectFactory.Ours(DialectKind.Generic);
        _cs = DialectFactory.Cs(DialectKind.Generic);
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int EmptyLoop()
    {
        var n = 0;
        for (var i = 0; i < N; i++)
        {
            n += NotLow(i);
        }
        return n;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int StackGuard_IsStackLow()
    {
        var n = 0;
        for (var i = 0; i < N; i++)
        {
            n += IsStackLowReplica() ? 1 : 0;
        }
        return n;
    }

    [Benchmark]
    public object Core_NewParser() => new Parser(_ours);

    [Benchmark]
    public object Cs_NewParser() => new Cs.Parser();

    /// <summary>Um <c>throw</c> + <c>catch</c> de <c>ParserException</c>, com a mensagem montada como no Core.</summary>
    [Benchmark]
    public int ParserException_ThrowCatch()
    {
        try
        {
            ThrowExpected();
            return 0;
        }
        catch (ParserException e)
        {
            return e.Message.Length;
        }
    }

    /// <summary>
    /// O padrão de <c>MaybeParse</c> + <c>TryParse</c> do Core numa especulação que falha: throw, catch,
    /// relançamento por <c>ExceptionDispatchInfo</c> fora do catch e catch com filtro.
    /// </summary>
    [Benchmark]
    public int ParserException_MaybeParsePattern()
    {
        try
        {
            return TryParseReplica();
        }
        catch (ParserException e) when (e.Error is not ParserError.RecursionLimitExceeded)
        {
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NotLow(int i) => i & 1;

    /// <summary>Cópia literal de <c>LeptonTech.SqlParser.Core.StackGuard.IsStackLow</c> (Support/StackGuard.cs).</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsStackLowReplica()
    {
        try
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
            return false;
        }
        catch (InsufficientExecutionStackException)
        {
            return true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowExpected() =>
        throw new ParserException(new ParserError.ParserErrorValue("Expected: an expression, found: ) at Line: 1, Column: 20"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TryParseReplica()
    {
        ParserException e;
        try
        {
            ThrowExpected();
            return 0;
        }
        catch (ParserException error)
        {
            e = error;
        }
        ExceptionDispatchInfo.Capture(e).Throw();
        return 1;
    }
}
