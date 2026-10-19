<#
.SYNOPSIS
    Gera src/LeptonTech.SqlParser.Core/Ast/Visitor/Generated/*.g.cs: o equivalente C# do derive Visit/VisitMut
    do sqlparser-rs (derive/src/visit.rs) para todos os tipos da AST.

.DESCRIPTION
    No Rust, cada tipo da AST tem #[cfg_attr(feature = "visitor", derive(Visit, VisitMut))], e o derive
    gera a travessia (campos na ordem da declaração, ganchos pre_/post_ de visit(with = "...")).
    Aqui o derive é este gerador:
      1. compila o LeptonTech.SqlParser.Core SEM o código gerado (-p:SqlParserVisitorCodegen=true), numa pasta
         temporária, para que funcione mesmo com o código gerado desatualizado;
      2. roda tools/codegen/VisitorGen, que lê a AST por reflexão (ordem dos campos = construtor primário;
         ganchos = [VisitWith]), confere tudo contra o código Rust do upstream (tipos com derive, nomes e
         ordem dos campos, anotações visit(with)) e escreve um arquivo por pasta da AST.
    Rode sempre que um tipo da AST mudar (ver a skill sync-upstream). Com -Check, só verifica se o código
    gerado está atualizado (código de saída 1 se não estiver).
    Decisão e formato: docs/adr/0007-visitor.md.

.EXAMPLE
    pwsh tools/codegen/Generate-Visitor.ps1
    pwsh tools/codegen/Generate-Visitor.ps1 -Check
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [string]$Upstream = (Join-Path $PSScriptRoot '..\..\upstream\sqlparser-rs')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if (-not (Test-Path (Join-Path $Upstream 'src\ast\visitor.rs'))) {
    throw "Código Rust de referência não encontrado em '$Upstream'. Rode pwsh tools/upstream/Get-Upstream.ps1."
}
$config = Get-Content (Join-Path $root 'UPSTREAM.json') -Raw | ConvertFrom-Json
$version = $config.'sqlparser-rs'.baselineTag
$tmp = Join-Path ([IO.Path]::GetTempPath()) ('sqlparser-visitorgen-' + [Guid]::NewGuid().ToString('N'))

try {
    $bin = Join-Path $tmp 'core'
    $obj = Join-Path $tmp 'core-obj\'
    dotnet build (Join-Path $root 'src\LeptonTech.SqlParser.Core\LeptonTech.SqlParser.Core.csproj') -nologo -v q -c Debug `
        -p:SqlParserVisitorCodegen=true "-p:IntermediateOutputPath=$obj" -o $bin
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o LeptonTech.SqlParser.Core sem o código gerado do visitor.' }

    $gen = Join-Path $tmp 'gen'
    dotnet build (Join-Path $PSScriptRoot 'VisitorGen\VisitorGen.csproj') -nologo -v q -c Release -o $gen
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar o gerador (tools/codegen/VisitorGen).' }

    $genArgs = @(
        '--assembly', (Join-Path $bin 'LeptonTech.SqlParser.Core.dll'),
        '--sources', (Join-Path $root 'src\LeptonTech.SqlParser.Core'),
        '--rust', (Resolve-Path $Upstream).Path,
        '--output', (Join-Path $root 'src\LeptonTech.SqlParser.Core\Ast\Visitor\Generated'),
        '--version', $version,
        '--header', (Join-Path $Upstream 'HEADER')
    )
    if ($Check) { $genArgs += '--check' }
    dotnet (Join-Path $gen 'VisitorGen.dll') @genArgs
    if ($LASTEXITCODE -ne 0) { throw 'O gerador do visitor terminou com erro (ver mensagens acima).' }
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
