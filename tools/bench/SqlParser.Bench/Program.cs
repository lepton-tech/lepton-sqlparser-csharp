using System.Diagnostics;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace SqlParser.Bench;

/// <summary>
/// Uso (ver tools/bench/README.md):
///   SqlParser.Bench [--pin MASCARA] --analyze      métricas auxiliares (aceitação, exceções por parse, nós Expr)
///   SqlParser.Bench [--pin MASCARA] --crosscheck   compara o ToString das duas bibliotecas
///   SqlParser.Bench [--pin MASCARA] [args do BenchmarkDotNet, ex.: --filter *Pipeline*]
/// <c>--pin</c> fixa o processo (e os processos de medição) nos processadores lógicos da máscara. Em CPUs
/// híbridas (núcleos P e E), sem isso o Windows alterna o benchmark entre tipos de núcleo e os tempos variam
/// até 2,3× de uma medição para outra.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        var list = args.ToList();
        long? pin = null;
        var pinIndex = list.IndexOf("--pin");
        if (pinIndex >= 0 && pinIndex + 1 < list.Count)
        {
            pin = long.Parse(list[pinIndex + 1], System.Globalization.CultureInfo.InvariantCulture);
            list.RemoveRange(pinIndex, 2);
        }
        args = list.ToArray();

        if (args.Length > 0 && (args[0] == "--analyze" || args[0] == "--crosscheck"))
        {
            if (pin is { } mask)
            {
                Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)mask;
            }
            if (args[0] == "--analyze")
            {
                Analysis.Run();
            }
            else
            {
                CrossCheck.Run();
            }
            return 0;
        }

        // Job padrão com menos iterações que o Default do BenchmarkDotNet, para a execução completa caber
        // em ~30 minutos: 1 processo por benchmark, 3 aquecimentos, 10 medições de ~250 ms.
        var job = Job.Default
            .WithLaunchCount(1)
            .WithWarmupCount(3)
            .WithIterationCount(10)
            .WithIterationTime(TimeInterval.FromMilliseconds(250));
        if (pin is { } affinity)
        {
            job = job.WithAffinity((IntPtr)affinity);
        }
        var config = DefaultConfig.Instance
            .AddJob(job.AsDefault())
            .AddDiagnoser(MemoryDiagnoser.Default);
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        return 0;
    }
}
