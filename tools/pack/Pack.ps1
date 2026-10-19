<#
.SYNOPSIS
    Gera o pacote LeptonTI.SqlParser (.nupkg) em artifacts/, depois de compilar em Release e rodar todos os testes.

.DESCRIPTION
    Passos, na ordem (qualquer falha interrompe o script, e nada é empacotado):
      0. confere que a árvore de trabalho do Git está limpa (git status --porcelain, somente leitura): o commit
         gravado nas DLLs (InformationalVersion) e no .nuspec só identifica o conteúdo se não houver alterações
         não commitadas nem arquivos novos fora do .gitignore. Com alterações, o script para, a menos que
         -AllowDirty seja passado;
      1. restaura a solução (SqlParser.sln);
      2. compila em Release, com build reproduzível (ContinuousIntegrationBuild: caminhos /_/ nos PDBs);
      3. roda a suíte inteira em Release (net48, net8.0 e net10.0);
      4. empacota src/SqlParser (sem recompilar) em artifacts/: um pacote só, com SqlParser.dll e
         SqlParser.Core.dll, cada uma com o PDB portátil embutido (docs/adr/0010-empacotamento.md);
      5. mostra o caminho e a lista de arquivos do .nupkg e a InformationalVersion das DLLs (e apaga um .snupkg
         antigo da mesma versão, de antes dos PDBs embutidos).
    Nada é publicado: artifacts/ funciona como feed local (ver tools/pack/Test-Package.ps1).

.PARAMETER AllowDirty
    Empacota mesmo com alterações não commitadas (ou fora de um repositório Git). A InformationalVersion das DLLs
    termina em ".dirty", para que o pacote não se passe pelo do commit. Use só para testes locais.

.EXAMPLE
    pwsh tools/pack/Pack.ps1
    pwsh tools/pack/Pack.ps1 -AllowDirty
#>
[CmdletBinding()]
param(
    [string]$Output = (Join-Path $PSScriptRoot '..\..\artifacts'),
    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$sln = Join-Path $root 'SqlParser.sln'
$project = Join-Path $root 'src\SqlParser\SqlParser.csproj'

# Árvore de trabalho limpa? (Só leitura: git status não altera nada.) Num worktree, .git é um arquivo.
$insideGit = Test-Path (Join-Path $root '.git')
$dirtyArgs = @()
if ($insideGit) {
    $changes = @(git -C $root status --porcelain)
    if ($LASTEXITCODE -ne 0) { throw "Falhou: git status (código de saída $LASTEXITCODE)." }
    if ($changes.Count -gt 0) {
        Write-Host "Alterações não commitadas ($($changes.Count)):" -ForegroundColor Yellow
        $changes | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
        if ($changes.Count -gt 20) { Write-Host '  ...' }
        if (-not $AllowDirty) {
            throw ('A árvore de trabalho tem alterações não commitadas: o commit gravado no pacote não identificaria o ' +
                'conteúdo. Faça o commit (ou descarte as alterações) e rode de novo, ou use -AllowDirty para um pacote ' +
                'de teste, marcado com ".dirty" na InformationalVersion.')
        }
        Write-Warning 'Empacotando com alterações não commitadas (-AllowDirty): a InformationalVersion terminará em ".dirty".'
        $dirtyArgs += '-p:SqlParserDirtyBuild=true'
    }
}
else {
    if (-not $AllowDirty) {
        throw ('Fora de um repositório Git: não há commit que identifique o conteúdo. Use -AllowDirty para um pacote ' +
            'de teste, marcado com ".dirty" na InformationalVersion.')
    }
    Write-Warning 'Fora de um repositório Git (-AllowDirty): build sem ContinuousIntegrationBuild (caminhos locais nos PDBs) e InformationalVersion com ".dirty".'
    $dirtyArgs += '-p:SqlParserDirtyBuild=true'
}

New-Item -ItemType Directory -Force $Output | Out-Null
$Output = (Resolve-Path $Output).Path

# Build reproduzível só dentro de um repositório Git: sem ele, o SDK não tem a raiz dos fontes (SourceRoot).
$ciArgs = @()
if ($insideGit) { $ciArgs += '-p:ContinuousIntegrationBuild=true' }

function Invoke-Step([string]$name, [scriptblock]$command) {
    Write-Host ''
    Write-Host "==> $name" -ForegroundColor Cyan
    & $command
    if ($LASTEXITCODE -ne 0) { throw "Falhou: $name (código de saída $LASTEXITCODE)." }
}

Invoke-Step 'Restauração' { dotnet restore $sln -nologo -v q }
Invoke-Step 'Build (Release)' { dotnet build $sln -c Release --no-restore -nologo -v q @ciArgs @dirtyArgs }
Invoke-Step 'Testes (Release: net48, net8.0, net10.0)' { dotnet test $sln -c Release --no-build -nologo -v q }
Invoke-Step 'Empacotamento' { dotnet pack $project -c Release --no-build -nologo -v q -o $Output @ciArgs @dirtyArgs }

# Versão calculada pelo MSBuild (VersionPrefix/VersionSuffix de Directory.Build.props).
$version = (dotnet msbuild $project -nologo -getProperty:PackageVersion -p:Configuration=Release).Trim()
if ($LASTEXITCODE -ne 0 -or -not $version) { throw 'Não foi possível ler a PackageVersion do projeto.' }
$id = 'LeptonTI.SqlParser'

Add-Type -AssemblyName System.IO.Compression.FileSystem
$file = Join-Path $Output "$id.$version.nupkg"
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

# As DLLs empacotadas são as da compilação acima (pack sem recompilar).
$binDir = Join-Path $root 'src\SqlParser\bin\Release\netstandard2.0'
foreach ($dll in 'SqlParser.dll', 'SqlParser.Core.dll') {
    $productVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $binDir $dll)).ProductVersion
    Write-Host ("  {0}: {1}" -f $dll, $productVersion)
}

# Um .snupkg da mesma versão é de um empacotamento anterior (os PDBs agora vão embutidos) e não combina mais com o .nupkg.
$staleSymbols = Join-Path $Output "$id.$version.snupkg"
if (Test-Path $staleSymbols) {
    Remove-Item -Force $staleSymbols
    Write-Host "Removido o pacote de símbolos antigo: $staleSymbols"
}
