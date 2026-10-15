<#
.SYNOPSIS
    Prepara as cópias do SqlParser-cs usadas pelo benchmark (tools/bench/SqlParser.Bench).

.DESCRIPTION
    O SqlParser-cs gera um assembly chamado "SqlParser.dll", o mesmo nome do assembly da nossa API
    pública (src/SqlParser). Os dois não podem conviver na mesma pasta de saída, e o clone de referência
    em upstream/SqlParser-cs não pode ser alterado. Por isso o benchmark referencia CÓPIAS do clone,
    feitas fora do repositório. Em cada cópia, um Directory.Build.props renomeia o assembly para
    "SqlParserCs.dll", e um Directory.Build.targets dá um PackageId próprio ao projeto: o restore do
    NuGet identifica referências de projeto pelo PackageId, e duas referências com o mesmo id (uma por
    alvo) seriam fundidas numa só. O .csproj de cada cópia é renomeado (SqlParserCs.Net8.csproj e
    SqlParserCs.Ns20.csproj; motivo junto da função Rename-Csproj). Nenhum arquivo-fonte (.cs) da cópia
    "net8" é alterado.

    São criadas duas cópias:
      <Destino>/net8  cópia fiel de upstream/SqlParser-cs/src/SqlParser (alvos net8.0;net9.0;net10.0).
                      Usada pelo benchmark em net8.0.
      <Destino>/ns20  a mesma cópia com o patch do Plano B (docs/plano-b/sqlparser-cs-netstandard20.patch),
                      que a recompila para netstandard2.0. Usada pelo benchmark em net48.

    O patch é aplicado com o GNU patch (vem com o Git for Windows em usr/bin/patch.exe). Não usa git.

.PARAMETER Destination
    Pasta de destino. Padrão: $env:TEMP/SqlParser.Bench/sqlparser-cs. O projeto de benchmark lê o mesmo
    caminho da propriedade/variável de ambiente SqlParserCsDir (com o mesmo padrão).

.EXAMPLE
    pwsh tools/bench/Prepare-SqlParserCs.ps1
    pwsh tools/bench/Prepare-SqlParserCs.ps1 -Destination C:/temp/sqlparser-cs
#>
[CmdletBinding()]
param(
    [string]$Destination = (Join-Path ([IO.Path]::GetTempPath()) 'SqlParser.Bench/sqlparser-cs')
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$source = Join-Path $root 'upstream/SqlParser-cs/src/SqlParser'
$patchFile = Join-Path $root 'docs/plano-b/sqlparser-cs-netstandard20.patch'

if (-not (Test-Path $source)) {
    throw "Clone do SqlParser-cs não encontrado em '$source'. Rode: pwsh tools/upstream/Get-Upstream.ps1"
}

$patchExe = (Get-Command patch -ErrorAction SilentlyContinue)?.Source
if (-not $patchExe) {
    $gitExe = (Get-Command git -ErrorAction SilentlyContinue)?.Source
    $candidates = @('C:/Program Files/Git/usr/bin/patch.exe')
    if ($gitExe) {
        $candidates = @((Join-Path (Split-Path (Split-Path $gitExe)) 'usr/bin/patch.exe')) + $candidates
    }
    $patchExe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $patchExe) {
    throw 'GNU patch não encontrado (procure usr/bin/patch.exe na instalação do Git for Windows).'
}

# Directory.Build.props: importado ANTES do .csproj. O .csproj do SqlParser-cs não define AssemblyName,
# então o valor daqui vale, e todas as propriedades derivadas (TargetName, TargetFileName...) o seguem.
$props = @'
<Project>
  <!-- Gerado por tools/bench/Prepare-SqlParserCs.ps1. Não altera nenhum arquivo-fonte do SqlParser-cs. -->
  <PropertyGroup>
    <AssemblyName>SqlParserCs</AssemblyName>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <NoWarn>$(NoWarn);CS1591;CS8981;NU1603;NU1701</NoWarn>
  </PropertyGroup>
</Project>
'@

# Directory.Build.targets: importado DEPOIS do .csproj, para sobrepor o PackageId que ele define
# (SqlParserCS). O restore só lê o valor final da avaliação.
function Get-Targets([string]$packageId) {
    return @"
<Project>
  <!-- Gerado por tools/bench/Prepare-SqlParserCs.ps1. Não altera nenhum arquivo-fonte do SqlParser-cs. -->
  <PropertyGroup>
    <PackageId>$packageId</PackageId>
  </PropertyGroup>
</Project>
"@
}

function New-Copy([string]$name, [string]$packageId) {
    $dir = Join-Path $Destination $name
    if (Test-Path $dir) {
        Remove-Item -Recurse -Force $dir
    }
    $target = Join-Path $dir 'src/SqlParser'
    New-Item -ItemType Directory -Force $target | Out-Null
    Get-ChildItem -Force $source | Where-Object { $_.Name -notin @('bin', 'obj') } |
        Copy-Item -Destination $target -Recurse -Force
    Set-Content -Path (Join-Path $dir 'Directory.Build.props') -Value $props -Encoding utf8NoBOM
    Set-Content -Path (Join-Path $dir 'Directory.Build.targets') -Value (Get-Targets $packageId) -Encoding utf8NoBOM
    return $dir
}

# O .csproj de cada cópia também é renomeado (SqlParser.csproj -> SqlParserCs.<Variante>.csproj). Ao rodar, o
# BenchmarkDotNet compila os projetos com a pasta obj/ de cada um em obj/<nome do projeto>/, e dois
# projetos com o mesmo nome (o nosso src/SqlParser e o SqlParser-cs, ou as duas cópias entre si, que o
# restore do projeto gerado avalia juntas) dividiriam a mesma pasta.
function Rename-Csproj([string]$dir, [string]$name) {
    Rename-Item -Path (Join-Path $dir 'src/SqlParser/SqlParser.csproj') -NewName "$name.csproj"
}

# Cópia fiel (net8.0). O "SqlParser - Backup.csproj" do clone fica fora: dois projetos na mesma pasta
# dividiriam a pasta obj/.
$net8 = New-Copy 'net8' 'SqlParserCs.Net8'
Remove-Item -Force (Join-Path $net8 'src/SqlParser/SqlParser - Backup.csproj') -ErrorAction SilentlyContinue
Rename-Csproj $net8 'SqlParserCs.Net8'
Write-Host "Cópia net8.0: $net8"

# Cópia com o patch do Plano B (netstandard2.0). O patch foi gerado com --strip-trailing-cr: os
# arquivos da cópia passam para LF antes de aplicá-lo (só na cópia).
$ns20 = New-Copy 'ns20' 'SqlParserCs.Ns20'
Get-ChildItem -Recurse -File (Join-Path $ns20 'src') -Include *.cs, *.csproj | ForEach-Object {
    $text = [IO.File]::ReadAllText($_.FullName)
    if ($text.Contains("`r`n")) {
        $bytes = [IO.File]::ReadAllBytes($_.FullName)
        $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        [IO.File]::WriteAllText($_.FullName, $text.Replace("`r`n", "`n"), [Text.UTF8Encoding]::new($hasBom))
    }
}
Push-Location $ns20
try {
    & $patchExe -p1 --forward --no-backup-if-mismatch -i $patchFile
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao aplicar $patchFile (código $LASTEXITCODE)."
    }
}
finally {
    Pop-Location
}
Rename-Csproj $ns20 'SqlParserCs.Ns20'
Write-Host "Cópia netstandard2.0 (Plano B): $ns20"
