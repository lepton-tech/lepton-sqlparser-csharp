<#
.SYNOPSIS
    Baixa (ou atualiza) os repositórios de referência fixados em UPSTREAM.json para a pasta upstream/.

.DESCRIPTION
    - upstream/sqlparser-rs : código Rust portado, posicionado no commit "portedThroughCommit".
    - upstream/SqlParser-cs : port C# usado só como referência de API/estilo.
    A pasta upstream/ está no .gitignore: é material de consulta, nunca é compilado nem distribuído.

.PARAMETER Ref
    Opcional. Posiciona o sqlparser-rs em outra referência (tag, branch ou commit),
    por exemplo para inspecionar mudanças posteriores ao baseline: -Ref origin/main

.EXAMPLE
    pwsh tools/upstream/Get-Upstream.ps1
    pwsh tools/upstream/Get-Upstream.ps1 -Ref v0.64.0
#>
[CmdletBinding()]
param(
    [string]$Ref
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$config = Get-Content (Join-Path $root 'UPSTREAM.json') -Raw | ConvertFrom-Json

function Sync-Repo([string]$url, [string]$path, [string]$commitish) {
    $full = Join-Path $root $path
    if (-not (Test-Path (Join-Path $full '.git'))) {
        Write-Host "Clonando $url em $path ..."
        git clone --quiet $url $full
        if ($LASTEXITCODE -ne 0) { throw "Falha ao clonar $url" }
    }
    else {
        Write-Host "Atualizando $path ..."
        git -C $full fetch --quiet --tags origin
        if ($LASTEXITCODE -ne 0) { throw "Falha ao atualizar $path" }
    }
    git -C $full -c advice.detachedHead=false checkout --quiet $commitish
    if ($LASTEXITCODE -ne 0) { throw "Falha ao posicionar $path em $commitish" }
    $head = git -C $full log -1 --format='%H %cs %s'
    Write-Host "  $path -> $head"
}

$rs = $config.'sqlparser-rs'
$target = if ($Ref) { $Ref } else { $rs.portedThroughCommit }
Sync-Repo $rs.repository $rs.localPath $target

$cs = $config.'SqlParser-cs'
Sync-Repo $cs.repository $cs.localPath $cs.commit
