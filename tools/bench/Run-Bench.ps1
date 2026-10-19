<#
.SYNOPSIS
    Compila e roda o benchmark LeptonTech.SqlParser.Core/LeptonTech.SqlParser × SqlParser-cs (tools/bench/LeptonTech.SqlParser.Bench).

.DESCRIPTION
    1. Prepara as cópias do SqlParser-cs (Prepare-SqlParserCs.ps1), a menos que -SkipPrepare.
    2. Compila o benchmark em Release (net8.0 e net48).
    3. Roda os modos auxiliares --analyze e --crosscheck nos dois alvos (saída em <Artifacts>/*.md).
    4. Roda o BenchmarkDotNet em net8.0 e depois em net48 (o net48 pula TpchPerQueryBenchmarks).
       Os relatórios ficam em <Artifacts>/net8.0/results e <Artifacts>/net48/results.

    A execução completa leva cerca de 25 minutos num notebook (i7-1255U). Feche programas pesados e
    deixe o computador na tomada, com o plano de energia em "melhor desempenho".

.PARAMETER Pin
    Máscara de afinidade (processadores lógicos) para todos os processos de medição; 0 = sem fixação.
    Em CPUs híbridas, fixe num núcleo P: no i7-1255U, o processador lógico 2 (máscara 4). Sem isso o
    Windows alterna os processos entre núcleos P e E, e os tempos variam até 2,3×.

.PARAMETER Filter
    Filtros do BenchmarkDotNet (padrão: todos). Ex.: -Filter '*PipelineBenchmarks*'

.EXAMPLE
    pwsh tools/bench/Run-Bench.ps1 -Pin 4
    pwsh tools/bench/Run-Bench.ps1 -SkipPrepare -SkipNet48 -Filter '*MicroBenchmarks*'
#>
[CmdletBinding()]
param(
    [string]$SqlParserCsDir = (Join-Path ([IO.Path]::GetTempPath()) 'LeptonTech.SqlParser.Bench/sqlparser-cs'),
    [string]$Artifacts = (Join-Path ([IO.Path]::GetTempPath()) 'LeptonTech.SqlParser.Bench/artifacts'),
    [string[]]$Filter = @('*'),
    [switch]$SkipPrepare,
    [switch]$SkipNet48,
    [switch]$SkipBenchmarks,
    [long]$Pin = 0
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'LeptonTech.SqlParser.Bench/LeptonTech.SqlParser.Bench.csproj'
$bin = Join-Path $PSScriptRoot 'LeptonTech.SqlParser.Bench/bin/Release'

if (-not $SkipPrepare) {
    & (Join-Path $PSScriptRoot 'Prepare-SqlParserCs.ps1') -Destination $SqlParserCsDir
}

# O projeto (e os projetos que o BenchmarkDotNet gera a partir dele) lê o caminho desta variável.
$env:SqlParserCsDir = $SqlParserCsDir

dotnet build $project -c Release -nologo -v q
if ($LASTEXITCODE -ne 0) {
    throw "Falha na compilação ($LASTEXITCODE)."
}

New-Item -ItemType Directory -Force $Artifacts | Out-Null
$pinArgs = if ($Pin -ne 0) { @("--pin", "$Pin") } else { @() }
# O programa escreve UTF-8; sem isto o PowerShell decodificaria a saída com a página de código do console.
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$targets = @('net8.0') + $(if ($SkipNet48) { @() } else { @('net48') })
foreach ($tfm in $targets) {
    $exe = Join-Path $bin "$tfm/LeptonTech.SqlParser.Bench.exe"
    & $exe @pinArgs --analyze | Set-Content -Encoding utf8NoBOM (Join-Path $Artifacts "analyze-$tfm.md")
    & $exe @pinArgs --crosscheck | Set-Content -Encoding utf8NoBOM (Join-Path $Artifacts "crosscheck-$tfm.md")
}

if ($SkipBenchmarks) {
    return
}

foreach ($tfm in $targets) {
    $exe = Join-Path $bin "$tfm/LeptonTech.SqlParser.Bench.exe"
    $filters = $Filter
    if ($tfm -eq 'net48' -and $Filter.Count -eq 1 -and $Filter[0] -eq '*') {
        # Por consulta só em net8.0, para a execução completa caber em ~30 minutos.
        $filters = @('*DialectBenchmarks*', '*PipelineBenchmarks*', '*DeepNestingBenchmarks*', '*MicroBenchmarks*')
    }
    & $exe @pinArgs --filter @filters --artifacts (Join-Path $Artifacts $tfm)
    if ($LASTEXITCODE -ne 0) {
        throw "BenchmarkDotNet falhou em $tfm ($LASTEXITCODE)."
    }
}
Write-Host "Resultados em $Artifacts"
