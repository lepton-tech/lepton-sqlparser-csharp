<#
.SYNOPSIS
    Gera o pacote LeptonTI.SqlParser (.nupkg e .snupkg) em artifacts/, depois de compilar em Release e rodar
    todos os testes.

.DESCRIPTION
    Passos, na ordem (qualquer falha interrompe o script, e nada é empacotado):
      1. restaura a solução (SqlParser.sln);
      2. compila em Release, com build reproduzível (ContinuousIntegrationBuild: caminhos /_/ nos PDBs);
      3. roda a suíte inteira em Release (net48, net8.0 e net10.0);
      4. empacota src/SqlParser (sem recompilar) em artifacts/: um pacote só, com SqlParser.dll e
         SqlParser.Core.dll (docs/adr/0010-empacotamento.md);
      5. mostra o caminho e a lista de arquivos do .nupkg e do .snupkg.
    Nada é publicado: artifacts/ funciona como feed local (ver tools/pack/Test-Package.ps1).

.EXAMPLE
    pwsh tools/pack/Pack.ps1
#>
[CmdletBinding()]
param(
    [string]$Output = (Join-Path $PSScriptRoot '..\..\artifacts')
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sln = Join-Path $root 'SqlParser.sln'
$project = Join-Path $root 'src\SqlParser\SqlParser.csproj'
New-Item -ItemType Directory -Force $Output | Out-Null
$Output = (Resolve-Path $Output).Path

# Build reproduzível só dentro de um repositório Git: sem ele, o SDK não tem a raiz dos fontes (SourceRoot).
$ciArgs = @()
if (Test-Path (Join-Path $root '.git')) { $ciArgs += '-p:ContinuousIntegrationBuild=true' }
else { Write-Warning 'Fora de um repositório Git: build sem ContinuousIntegrationBuild (caminhos locais nos PDBs).' }

function Invoke-Step([string]$name, [scriptblock]$command) {
    Write-Host ''
    Write-Host "==> $name" -ForegroundColor Cyan
    & $command
    if ($LASTEXITCODE -ne 0) { throw "Falhou: $name (código de saída $LASTEXITCODE)." }
}

Invoke-Step 'Restauração' { dotnet restore $sln -nologo -v q }
Invoke-Step 'Build (Release)' { dotnet build $sln -c Release --no-restore -nologo -v q @ciArgs }
Invoke-Step 'Testes (Release: net48, net8.0, net10.0)' { dotnet test $sln -c Release --no-build -nologo -v q }
Invoke-Step 'Empacotamento' { dotnet pack $project -c Release --no-build -nologo -v q -o $Output @ciArgs }

# Versão calculada pelo MSBuild (VersionPrefix/VersionSuffix de Directory.Build.props).
$version = (dotnet msbuild $project -nologo -getProperty:PackageVersion -p:Configuration=Release).Trim()
if ($LASTEXITCODE -ne 0 -or -not $version) { throw 'Não foi possível ler a PackageVersion do projeto.' }
$id = 'LeptonTI.SqlParser'

Add-Type -AssemblyName System.IO.Compression.FileSystem
foreach ($ext in 'nupkg', 'snupkg') {
    $file = Join-Path $Output "$id.$version.$ext"
    if (-not (Test-Path $file)) { throw "Pacote esperado não encontrado: $file" }
    $info = Get-Item $file
    Write-Host ''
    Write-Host ("Pacote: {0} ({1:N0} bytes)" -f $info.FullName, $info.Length) -ForegroundColor Green
    $zip = [IO.Compression.ZipFile]::OpenRead($file)
    try {
        $zip.Entries |
            Where-Object { $_.FullName -notmatch '^(_rels/|package/|\[Content_Types\]\.xml$)' } |
            Sort-Object FullName |
            ForEach-Object { Write-Host ('  {0,12:N0}  {1}' -f $_.Length, $_.FullName) }
    }
    finally { $zip.Dispose() }
}
