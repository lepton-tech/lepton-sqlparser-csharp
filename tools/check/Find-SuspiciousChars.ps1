<#
.SYNOPSIS
    Procura caracteres suspeitos (de controle, U+FFFF, não-ASCII inesperados) em código e documentação.

.DESCRIPTION
    Motivo: as ferramentas de edição de agentes podem converter sequências de escape (por exemplo,
    a barra invertida seguida de "u" e 4 dígitos hexadecimais) no caractere literal correspondente.
    Um teste com o dado alterado continua "verde", mas passa a testar outra coisa. Rode este script
    depois de portar código com literais de string/char e confira cada ocorrência listada.

    São aceitos sem aviso: letras acentuadas do português, travessões, aspas tipográficas e os
    símbolos usados na documentação. Todo o resto é listado, com arquivo:linha e o code point.

.EXAMPLE
    pwsh tools/check/Find-SuspiciousChars.ps1
    pwsh tools/check/Find-SuspiciousChars.ps1 -Path src
#>
[CmdletBinding()]
param(
    [string[]]$Path = @('src', 'tests', 'docs', '.claude', 'tools')
)

$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
# Aspas tipográficas simples (U+2018/U+2019) entram por código: dentro de uma string entre aspas
# simples, o PowerShell as trata como delimitadores.
$allowed = 'áàâãéêíóôõúüçÁÀÂÃÉÊÍÓÔÕÚÜÇ–—“”…§→←↔⬜🟨✅⏸️·×ºª≥≠−─►' + [char]0x2018 + [char]0x2019
$count = 0
# Aceita tanto -Path src,tests (array) quanto "src,tests" (string única, comum com pwsh -File).
$targets = $Path | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ } |
    ForEach-Object { Join-Path $root $_ } | Where-Object { Test-Path $_ }
if (-not $targets) { throw "Nenhum caminho válido em -Path: $($Path -join ', ')" }
$files = Get-ChildItem -Path $targets -Recurse -File -Include *.cs, *.md, *.ps1, *.js, *.json, *.csproj, *.props |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|upstream)[\\/]' }

foreach ($file in $files) {
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadAllLines($file.FullName, [System.Text.Encoding]::UTF8)) {
        $lineNumber++
        $found = New-Object System.Collections.Generic.List[string]
        foreach ($ch in $line.ToCharArray()) {
            $c = [int]$ch
            $isControl = ($c -lt 32 -and $c -ne 9) -or $c -eq 127
            $isUnexpected = $c -gt 127 -and -not $allowed.Contains([string]$ch) -and -not [char]::IsSurrogate($ch) -and $c -ne 0xFE0F
            if ($isControl -or $isUnexpected) { $found.Add(('U+{0:X4}' -f $c)) }
        }
        if ($found.Count -gt 0) {
            $count++
            $relative = $file.FullName.Substring($root.Path.Length + 1)
            $text = $line.Trim()
            Write-Output ("{0}:{1}: {2} | {3}" -f $relative, $lineNumber, (($found | Select-Object -Unique) -join ','), $text.Substring(0, [Math]::Min(100, $text.Length)))
        }
    }
}
Write-Output "Linhas com caracteres a conferir: $count"
