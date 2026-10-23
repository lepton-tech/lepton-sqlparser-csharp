<#
.SYNOPSIS
    Testa o pacote LeptonTech.SqlParser de artifacts/ como um consumidor externo: dois apps de console
    (net48 e net8.0, em tests/PackageSmoke/, fora da solução) instalam o pacote do feed local e o usam.

.DESCRIPTION
    1. escolhe o .nupkg mais recente de artifacts/ (ou a versão passada em -Version);
    2. apaga a entrada do pacote no cache global do NuGet, para que a restauração use o .nupkg recém-gerado;
    3. para cada app (net48 e net8.0): apaga bin/obj, restaura (tests/PackageSmoke/nuget.config: LeptonTech.* só do
       feed local), compila e executa. O app analisa SQL em três dialetos, imprime ToSql(pretty) e
       GetReferencedTables e sai com 0 se tudo conferir.
    Gere o pacote antes com tools/pack/Pack.ps1.

.EXAMPLE
    pwsh tools/pack/Test-Package.ps1
    pwsh tools/pack/Test-Package.ps1 -Version 0.63.1-preview.5
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Artifacts = (Join-Path $PSScriptRoot '..\..\artifacts')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$smoke = Join-Path $root 'tests\PackageSmoke'
$id = 'LeptonTech.SqlParser'

if (-not (Test-Path $Artifacts)) { throw "Pasta '$Artifacts' não encontrada. Rode antes: pwsh tools/pack/Pack.ps1" }
if (-not $Version) {
    $latest = Get-ChildItem $Artifacts -Filter "$id.*.nupkg" |
        Where-Object { $_.Name -notlike '*.symbols.nupkg' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (-not $latest) { throw "Nenhum $id.*.nupkg em '$Artifacts'. Rode antes: pwsh tools/pack/Pack.ps1" }
    $Version = $latest.BaseName.Substring($id.Length + 1)
}
$package = Join-Path $Artifacts "$id.$Version.nupkg"
if (-not (Test-Path $package)) { throw "Pacote não encontrado: $package" }
Write-Host "Pacote: $((Resolve-Path $package).Path)" -ForegroundColor Cyan

# Cache global do NuGet: sem isto, uma versão de mesmo número restaurada antes seria reaproveitada.
$globalPackages = ((dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', '').Trim()
$cached = Join-Path $globalPackages ("$id/$Version".ToLowerInvariant())
if (Test-Path $cached) {
    Remove-Item -Recurse -Force $cached
    Write-Host "Cache limpo: $cached"
}

$results = [ordered]@{}
foreach ($app in 'Net48', 'Net8') {
    $dir = Join-Path $smoke $app
    $project = Join-Path $dir "PackageSmoke.$app.csproj"
    Write-Host ''
    Write-Host "==> $app" -ForegroundColor Cyan
    foreach ($sub in 'bin', 'obj') {
        $path = Join-Path $dir $sub
        if (Test-Path $path) { Remove-Item -Recurse -Force $path }
    }
    dotnet build $project -c Release -nologo -v q "-p:LeptonTechSqlParserVersion=$Version"
    if ($LASTEXITCODE -ne 0) { $results[$app] = $LASTEXITCODE; continue }
    $tfm = if ($app -eq 'Net48') { 'net48' } else { 'net8.0' }
    & (Join-Path $dir "bin\Release\$tfm\PackageSmoke.$app.exe")
    $results[$app] = $LASTEXITCODE
}

Write-Host ''
$failed = $false
foreach ($app in $results.Keys) {
    if ($results[$app] -eq 0) { Write-Host "$app`: OK" -ForegroundColor Green }
    else { Write-Host "$app`: FALHOU (código $($results[$app]))" -ForegroundColor Red; $failed = $true }
}
if ($failed) { exit 1 }
