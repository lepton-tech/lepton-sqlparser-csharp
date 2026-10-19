<#
.SYNOPSIS
    Gera src/SqlParser.Core/Support/RustCaseMapping.g.cs (tabelas de maiúsculas/minúsculas do Rust) a partir
    do Unicode Character Database (UCD).

.DESCRIPTION
    `str::to_uppercase`/`str::to_lowercase` e `char::to_uppercase`/`char::to_lowercase` do Rust usam tabelas
    geradas pelo `src/tools/unicode-table-generator` do repositório rust-lang/rust, na versão do Unicode de
    `core::char::UNICODE_VERSION`. Este script reproduz as partes dessas tabelas de que o port precisa
    (DIV-001, resolvida na onda D, unidade D6: docs/port/onda-d/D6.md):

    - `conversions::to_lower`/`to_upper`: mapeamento simples de UnicodeData.txt (campos 13 e 12), sobrescrito
      pelas linhas SEM condição de SpecialCasing.txt (mapeamento completo, até 3 scalar values). As linhas com
      condição (Final_Sigma, After_I, idiomas tr/az/lt...) são ignoradas, como no Rust; o sigma final é
      tratado à parte em `str::to_lowercase` (RustString.cs). Mapeamentos iguais ao próprio code point não
      entram, e os code points ASCII ficam fora das tabelas (o Rust os trata com `to_ascii_*`).
    - `is_cased` (`Lowercase || Uppercase || Lt`) e `is_case_ignorable` (`Case_Ignorable`), usados pela regra
      do sigma final: DerivedCoreProperties.txt e a categoria geral `Lt` de UnicodeData.txt, sem o ASCII.

    Os arquivos do UCD são baixados de https://www.unicode.org/Public/<versão>/ucd/ para -UcdDir (uma pasta
    temporária, fora do repositório) quando ainda não estão lá. A versão padrão é a da toolchain estável do
    Rust de referência (Rust 1.99.0: `char::UNICODE_VERSION` = (17, 0, 0); ver docs/port/onda-d/D6.md). Quando
    uma toolchain estável nova mudar a versão do Unicode, confira `char::UNICODE_VERSION` na documentação da
    std e rode o script com -UnicodeVersion e -RustToolchain.

    O gerado só tem números (code points em hexadecimal), sem literais de caractere nem sequências de escape.
    O script confere a si mesmo: as sequências expandidas reproduzem os mapeamentos, `Lowercase + Uppercase +
    Lt` é igual à propriedade derivada `Cased` e o `Case_Ignorable` do ASCII é o que o Rust fixa no código.

.EXAMPLE
    pwsh tools/codegen/Generate-CaseMapping.ps1
    pwsh tools/codegen/Generate-CaseMapping.ps1 -UnicodeVersion 17.0.0 -RustToolchain 1.99.0 -UcdDir C:/temp/ucd-17.0.0
#>
[CmdletBinding()]
param(
    [string]$UnicodeVersion = '17.0.0',
    # Só para o cabeçalho do gerado: a toolchain estável cujo `char::UNICODE_VERSION` é -UnicodeVersion.
    [string]$RustToolchain = '1.99.0',
    [string]$UcdDir = (Join-Path ([System.IO.Path]::GetTempPath()) "sqlparser-ucd/$UnicodeVersion"),
    [string]$Output = (Join-Path $PSScriptRoot '..\..\src\SqlParser.Core\Support\RustCaseMapping.g.cs')
)

$ErrorActionPreference = 'Stop'

# --- Download (se faltar) -------------------------------------------------------------------
$files = 'UnicodeData.txt', 'SpecialCasing.txt', 'DerivedCoreProperties.txt'
New-Item -ItemType Directory -Force $UcdDir | Out-Null
foreach ($f in $files) {
    $target = Join-Path $UcdDir $f
    if (-not (Test-Path $target)) {
        $url = "https://www.unicode.org/Public/$UnicodeVersion/ucd/$f"
        Write-Host "Baixando $url"
        Invoke-WebRequest -Uri $url -OutFile $target -UseBasicParsing
    }
}

function Read-UcdLines([string]$name) {
    [System.IO.File]::ReadAllLines((Join-Path $UcdDir $name), [System.Text.Encoding]::UTF8)
}

# Confere a versão nos cabeçalhos (ex.: "# SpecialCasing-17.0.0.txt").
foreach ($f in 'SpecialCasing.txt', 'DerivedCoreProperties.txt') {
    $first = (Read-UcdLines $f)[0]
    $expected = '# ' + [System.IO.Path]::GetFileNameWithoutExtension($f) + "-$UnicodeVersion.txt"
    if ($first.Trim() -ne $expected) { throw "Versão inesperada em ${f}: '$first' (esperado '$expected')" }
}

function ConvertFrom-Hex([string]$hex) { [Convert]::ToInt32($hex.Trim(), 16) }

# --- UnicodeData.txt: mapeamentos simples e a categoria Lt ----------------------------------
$toLower = New-Object 'System.Collections.Generic.SortedDictionary[int,int[]]'
$toUpper = New-Object 'System.Collections.Generic.SortedDictionary[int,int[]]'
$lt = New-Object 'System.Collections.Generic.HashSet[int]'
foreach ($line in (Read-UcdLines 'UnicodeData.txt')) {
    if ($line.Length -eq 0) { continue }
    $fields = $line.Split(';')
    if ($fields.Count -ne 15) { throw "Linha de UnicodeData.txt inesperada: $line" }
    $cp = ConvertFrom-Hex $fields[0]
    $gc = $fields[2]
    # As faixas "<..., First>"/"<..., Last>" (ideogramas, Hangul, uso privado...) não têm mapeamentos nem Lt;
    # o UnicodeDataExpander do Rust as expandiria, sem efeito nas tabelas daqui.
    if ($fields[1].EndsWith(', First>') -or $fields[1].EndsWith(', Last>')) {
        if ($fields[12] -or $fields[13] -or $gc -eq 'Lt') { throw "Faixa com mapeamento: $line" }
        continue
    }
    if ($gc -eq 'Lt') { [void]$lt.Add($cp) }
    if ($fields[13]) {
        $mapped = ConvertFrom-Hex $fields[13]
        if ($mapped -ne $cp) { $toLower[$cp] = @($mapped) }
    }
    if ($fields[12]) {
        $mapped = ConvertFrom-Hex $fields[12]
        if ($mapped -ne $cp) { $toUpper[$cp] = @($mapped) }
    }
}

# --- SpecialCasing.txt: só as linhas sem condição ------------------------------------------
# Formato: <code>; <lower>; <title>; <upper>; (<condition_list>;)? # <comment>
$specialCount = 0
foreach ($line in (Read-UcdLines 'SpecialCasing.txt')) {
    $data = $line
    $hash = $data.IndexOf('#')
    if ($hash -ge 0) { $data = $data.Substring(0, $hash) }
    if ($data.Trim().Length -eq 0) { continue }
    $fields = $data.Split(';')
    if ($fields.Count -lt 5) { throw "Linha de SpecialCasing.txt inesperada: $line" }
    $conditions = $fields[4].Trim()
    if ($conditions) { continue } # Rust: "Skip conditional case mappings"
    $cp = ConvertFrom-Hex $fields[0]
    foreach ($pair in @(@(1, $toLower), @(3, $toUpper))) {
        $mapping = @($fields[$pair[0]].Trim().Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { ConvertFrom-Hex $_ })
        if ($mapping.Count -lt 1 -or $mapping.Count -gt 3) { throw "Mapeamento inesperado: $line" }
        # Rust: `to_mapping(&[row.codepoint], ..)` devolve None quando o mapeamento é o próprio code point.
        if ($mapping.Count -eq 1 -and $mapping[0] -eq $cp) { continue }
        $pair[1][$cp] = $mapping
    }
    $specialCount++
}

# --- DerivedCoreProperties.txt -------------------------------------------------------------
$props = @{}
foreach ($name in 'Lowercase', 'Uppercase', 'Case_Ignorable', 'Cased') {
    $props[$name] = New-Object 'System.Collections.Generic.HashSet[int]'
}
foreach ($line in (Read-UcdLines 'DerivedCoreProperties.txt')) {
    $data = $line
    $hash = $data.IndexOf('#')
    if ($hash -ge 0) { $data = $data.Substring(0, $hash) }
    if ($data.Trim().Length -eq 0) { continue }
    $fields = $data.Split(';')
    $name = $fields[1].Trim()
    if (-not $props.ContainsKey($name)) { continue }
    $range = $fields[0].Trim()
    $dots = $range.IndexOf('..')
    if ($dots -ge 0) {
        $from = ConvertFrom-Hex $range.Substring(0, $dots)
        $to = ConvertFrom-Hex $range.Substring($dots + 2)
    } else {
        $from = ConvertFrom-Hex $range
        $to = $from
    }
    for ($c = $from; $c -le $to; $c++) { [void]$props[$name].Add($c) }
}

# `char::is_cased` = `Lowercase || Uppercase || Lt` (fora do ASCII). Confere com a propriedade derivada `Cased`.
$cased = New-Object 'System.Collections.Generic.HashSet[int]'
foreach ($c in $props['Lowercase']) { [void]$cased.Add($c) }
foreach ($c in $props['Uppercase']) { [void]$cased.Add($c) }
foreach ($c in $lt) { [void]$cased.Add($c) }
if (-not $cased.SetEquals($props['Cased'])) { throw 'Lowercase + Uppercase + Lt difere de Cased' }

# `char::is_case_ignorable` trata o ASCII à parte (`'`, `.`, `:`, `^` e crase): confere com o UCD.
$asciiIgnorable = @($props['Case_Ignorable'] | Where-Object { $_ -le 0x7F } | Sort-Object)
if (($asciiIgnorable -join ',') -ne ((0x27, 0x2E, 0x3A, 0x5E, 0x60) -join ',')) {
    throw "Case_Ignorable no ASCII mudou: $($asciiIgnorable -join ',')"
}

# --- Tabelas compactas ----------------------------------------------------------------------
# Sem o ASCII (Rust: `to_lower.retain(|&c, _| c > 0x7f)`; propriedades com `.filter(|c| !c.is_ascii())`).
function Get-NonAscii($dict) {
    $result = New-Object 'System.Collections.Generic.SortedDictionary[int,int[]]'
    foreach ($kv in $dict.GetEnumerator()) { if ($kv.Key -gt 0x7F) { $result[$kv.Key] = $kv.Value } }
    return , $result
}

# Mapeamentos de um scalar value → sequências (início, fim, passo 1 ou 2, deslocamento). Cada sequência
# cobre chaves consecutivas da lista ordenada, então os intervalos [início, fim] não se sobrepõem. Num passo
# 2, o code point do meio nunca é chave de um mapeamento simples (as chaves são consecutivas na lista); se for
# chave de um mapeamento múltiplo, a busca encontra a tabela múltipla antes.
function Get-Runs($dict) {
    $keys = @($dict.Keys | Where-Object { $dict[$_].Count -eq 1 })
    $runs = New-Object System.Collections.Generic.List[int[]]
    $i = 0
    while ($i -lt $keys.Count) {
        $start = $keys[$i]
        $delta = $dict[$start][0] - $start
        $j = $i
        while ($j + 1 -lt $keys.Count -and $keys[$j + 1] -eq $keys[$j] + 1 -and ($dict[$keys[$j + 1]][0] - $keys[$j + 1]) -eq $delta) { $j++ }
        if ($j -gt $i) {
            $runs.Add(@($start, $keys[$j], 1, $delta)); $i = $j + 1; continue
        }
        while ($j + 1 -lt $keys.Count -and $keys[$j + 1] -eq $keys[$j] + 2 -and ($dict[$keys[$j + 1]][0] - $keys[$j + 1]) -eq $delta) { $j++ }
        $stride = if ($j -gt $i) { 2 } else { 1 }
        $runs.Add(@($start, $keys[$j], $stride, $delta)); $i = $j + 1
    }
    # Autoverificação: as sequências expandidas reproduzem exatamente os mapeamentos simples.
    $expanded = @{}
    $previousEnd = -1
    foreach ($r in $runs) {
        if ($r[0] -le $previousEnd) { throw 'Sequências sobrepostas' }
        $previousEnd = $r[1]
        for ($c = $r[0]; $c -le $r[1]; $c += $r[2]) { $expanded[$c] = $c + $r[3] }
    }
    if ($expanded.Count -ne $keys.Count) { throw 'Sequências não cobrem os mapeamentos simples' }
    foreach ($k in $keys) { if ($expanded[$k] -ne $dict[$k][0]) { throw "Sequência errada em $k" } }
    return , $runs
}

function Get-Ranges($set) {
    $sorted = @($set | Where-Object { $_ -gt 0x7F } | Sort-Object)
    $ranges = New-Object System.Collections.Generic.List[int[]]
    $i = 0
    while ($i -lt $sorted.Count) {
        $j = $i
        while ($j + 1 -lt $sorted.Count -and $sorted[$j + 1] -eq $sorted[$j] + 1) { $j++ }
        $ranges.Add(@($sorted[$i], $sorted[$j])); $i = $j + 1
    }
    return , $ranges
}

$lower = Get-NonAscii $toLower
$upper = Get-NonAscii $toUpper
$lowerRuns = Get-Runs $lower
$upperRuns = Get-Runs $upper
$casedRanges = Get-Ranges $cased
$ignorableRanges = Get-Ranges $props['Case_Ignorable']

# --- Emissão ---------------------------------------------------------------------------------
function Format-Hex([int]$c) { '0x{0:X4}' -f $c }

$sb = New-Object System.Text.StringBuilder
function Add-Line([string]$text) { [void]$sb.AppendLine($text) }

function Add-Runs([string]$name, $runs, [string]$rust) {
    Add-Line "    // $rust, mapeamentos de um scalar value, sem o ASCII. Quádruplas: início, fim"
    Add-Line '    // (inclusive), passo (1 ou 2) e deslocamento somado ao code point.'
    Add-Line "    private static readonly int[] $name ="
    Add-Line '    {'
    foreach ($r in $runs) { Add-Line ("        {0}, {1}, {2}, {3}," -f (Format-Hex $r[0]), (Format-Hex $r[1]), $r[2], $r[3]) }
    Add-Line '    };'
}

function Add-Multi([string]$name, $dict, [string]$rust) {
    Add-Line "    // $rust, mapeamentos de 2 ou 3 scalar values (SpecialCasing.txt). Quádruplas: code"
    Add-Line '    // point e os três scalar values do `[char; 3]` do Rust (0 no lugar do `''\0''`).'
    Add-Line "    private static readonly int[] $name ="
    Add-Line '    {'
    foreach ($k in $dict.Keys) {
        $m = $dict[$k]
        if ($m.Count -eq 1) { continue }
        $c2 = if ($m.Count -eq 3) { $m[2] } else { 0 }
        Add-Line ("        {0}, {1}, {2}, {3}," -f (Format-Hex $k), (Format-Hex $m[0]), (Format-Hex $m[1]), (Format-Hex $c2))
    }
    Add-Line '    };'
}

function Add-Ranges([string]$name, $ranges, [string]$rust) {
    Add-Line "    // $rust, sem o ASCII. Pares: início e fim (inclusive)."
    Add-Line "    private static readonly int[] $name ="
    Add-Line '    {'
    for ($i = 0; $i -lt $ranges.Count; $i += 4) {
        $items = @()
        for ($j = $i; $j -lt [Math]::Min($i + 4, $ranges.Count); $j++) {
            $items += ('{0}, {1},' -f (Format-Hex $ranges[$j][0]), (Format-Hex $ranges[$j][1]))
        }
        Add-Line ('        ' + ($items -join ' '))
    }
    Add-Line '    };'
}

$multiLower = @($lower.Keys | Where-Object { $lower[$_].Count -gt 1 }).Count
$multiUpper = @($upper.Keys | Where-Object { $upper[$_].Count -gt 1 }).Count

Add-Line '// Código próprio do port (sem origem no sqlparser-rs): as tabelas de maiúsculas e minúsculas que a'
Add-Line '// biblioteca padrão do Rust usa em `char::to_lowercase`/`to_uppercase` e `str::to_lowercase`/`to_uppercase`'
Add-Line '// (`core::unicode::conversions` e as propriedades `Lowercase`, `Uppercase`, `Lt` e `Case_Ignorable`),'
Add-Line "// extraídas do Unicode Character Database $UnicodeVersion (o ``char::UNICODE_VERSION`` do Rust $RustToolchain) como"
Add-Line '// faz o `src/tools/unicode-table-generator` do Rust. A lógica fica em RustCaseMapping.cs e RustString.cs.'
Add-Line '// Ver docs/port/divergencias.md (DIV-001) e docs/port/onda-d/D6.md.'
Add-Line '//'
Add-Line '// Dados derivados do Unicode Character Database: Copyright (c) 1991-2026 Unicode, Inc., Unicode License'
Add-Line '// v3 (licenses/Unicode-LICENSE.txt; THIRD-PARTY-NOTICES.md).'
Add-Line '//'
Add-Line '// <auto-generated>'
Add-Line '// Gerado por tools/codegen/Generate-CaseMapping.ps1 (pwsh tools/codegen/Generate-CaseMapping.ps1).'
Add-Line '// NÃO EDITE À MÃO: rode o script novamente.'
Add-Line '// </auto-generated>'
Add-Line ''
Add-Line '#nullable enable'
Add-Line ''
Add-Line 'namespace SqlParser.Core;'
Add-Line ''
Add-Line 'internal static partial class RustCaseMapping'
Add-Line '{'
Add-Line '    // Rust: `core::char::UNICODE_VERSION` da toolchain de referência (docs/port/onda-d/D6.md).'
Add-Line "    internal const string UnicodeVersion = `"$UnicodeVersion`";"
Add-Line ''
Add-Runs 'LowerRuns' $lowerRuns '`conversions::to_lower`'
Add-Line ''
Add-Multi 'LowerMulti' $lower '`conversions::to_lower`'
Add-Line ''
Add-Runs 'UpperRuns' $upperRuns '`conversions::to_upper`'
Add-Line ''
Add-Multi 'UpperMulti' $upper '`conversions::to_upper`'
Add-Line ''
Add-Ranges 'CasedRanges' $casedRanges '`Lowercase || Uppercase || Lt` (`char::is_cased`)'
Add-Line ''
Add-Ranges 'CaseIgnorableRanges' $ignorableRanges '`Case_Ignorable` (`char::is_case_ignorable`)'
Add-Line '}'

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$outFull = [System.IO.Path]::GetFullPath($Output)
[System.IO.File]::WriteAllText($outFull, $sb.ToString().Replace("`r`n", "`n").Replace("`n", "`r`n"), $utf8NoBom)
Write-Host ("Gerado {0} (Unicode {1}): to_lower {2} mapeamentos ({3} sequências, {4} múltiplos), to_upper {5} ({6} sequências, {7} múltiplos), {8} linhas sem condição de SpecialCasing, Cased {9} faixas, Case_Ignorable {10} faixas." -f
    $Output, $UnicodeVersion, $lower.Count, $lowerRuns.Count, $multiLower, $upper.Count, $upperRuns.Count, $multiUpper, $specialCount, $casedRanges.Count, $ignorableRanges.Count)
