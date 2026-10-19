# Benchmark: SqlParser.Core × SqlParser-cs

Mede o desempenho do nosso parser (`src/SqlParser.Core`, port fiel do sqlparser-rs, e a API pública `src/SqlParser`) e o compara com o [SqlParser-cs](https://github.com/TylerBrinks/SqlParser-cs) (MIT), com o mesmo texto SQL nas duas bibliotecas. Também confere se as duas geram o mesmo SQL no `ToString()`/`ToSql()`.

O projeto `SqlParser.Bench` fica **fora** da solução (`SqlParser.sln`) e não é compilado nem testado pelo CI. Ele não altera nada em `src/`, `tests/` nem `upstream/`.

> **Status: parcial.** A execução foi interrompida em 2026-10-15, por decisão do responsável (prioridade para deixar a biblioteca publicável), depois de medir net8.0 e parte do net48. Os números em [Resultados parciais](#resultados-parciais) são indicativos: a medição foi curta (`ShortRun`) e rodou com outro agente usando a máquina, então as margens de erro são grandes. Para números finais, rode o `Run-Bench.ps1` completo com a máquina livre.

## Como rodar

Pré-requisitos: SDK do .NET 10 (o `global.json` da raiz vale aqui), runtime do .NET 8, Developer Pack do .NET Framework 4.8 (assemblies de referência) e Git for Windows (o script usa o `patch.exe` que vem nele; **não** usa git).

```powershell
# Tudo: prepara o SqlParser-cs, compila, roda --analyze/--crosscheck e o BenchmarkDotNet (net8.0 e net48).
# -Pin 4 fixa as medições no processador lógico 2 (um núcleo P do i7-1255U; ver "Ambiente").
pwsh tools/bench/Run-Bench.ps1 -Pin 4

# Só uma parte (as cópias do SqlParser-cs já preparadas, só net8.0):
pwsh tools/bench/Run-Bench.ps1 -SkipPrepare -SkipNet48 -Pin 4 -Filter '*PipelineBenchmarks*'
```

Resultados: `$env:TEMP/SqlParser.Bench/artifacts` (mude com `-Artifacts`): `analyze-<alvo>.md`, `crosscheck-<alvo>.md` e os relatórios do BenchmarkDotNet em `<alvo>/results/` (Markdown, CSV e HTML). A execução completa leva cerca de 25 minutos.

Passo a passo, sem o script:

```powershell
pwsh tools/bench/Prepare-SqlParserCs.ps1                       # cópias em $env:TEMP/SqlParser.Bench/sqlparser-cs
$env:SqlParserCsDir = "$env:TEMP/SqlParser.Bench/sqlparser-cs"  # lido pelo .csproj (também nos projetos gerados pelo BenchmarkDotNet)
dotnet build tools/bench/SqlParser.Bench -c Release
$exe = 'tools/bench/SqlParser.Bench/bin/Release/net8.0/SqlParser.Bench.exe'   # ou .../net48/SqlParser.Bench.exe
& $exe --pin 4 --analyze      # aceitação, tokens, nós Expr e exceções por parse
& $exe --pin 4 --crosscheck   # compara o SQL gerado pelas duas bibliotecas
& $exe --pin 4 --filter '*'   # BenchmarkDotNet (aceita os argumentos normais: --filter, --list flat, --artifacts...)
```

### Por que cópias do SqlParser-cs

- **Pacote NuGet:** nenhuma versão publicada corresponde ao clone de referência (`upstream/SqlParser-cs`, commit `e1929977`, sem release): o `SqlParserCS` 0.6.5 é do commit `d95fb53` (2025) e o 0.6.6-release0000, do `34214fd`. Além disso, o pacote traz um `SqlParser.dll`, o mesmo nome de arquivo do assembly da nossa API pública.
- **Clone intocado:** o clone não pode ser alterado (nem com `bin/`/`obj/`). O `Prepare-SqlParserCs.ps1` copia `upstream/SqlParser-cs/src/SqlParser` para fora do repositório e acrescenta, na cópia, um `Directory.Build.props` (assembly `SqlParserCs.dll`), um `Directory.Build.targets` (`PackageId` próprio) e renomeia o `.csproj` (`SqlParserCs.Net8.csproj`/`SqlParserCs.Ns20.csproj`): o restore do NuGet funde referências com o mesmo `PackageId`, e o BenchmarkDotNet põe o `obj/` de cada projeto em `obj/<nome do projeto>`. Nenhum `.cs` da cópia `net8` é alterado.
- **net48:** o SqlParser-cs só compila para net8+. Para o .NET Framework, a cópia `ns20` recebe o patch do Plano B (`docs/plano-b/sqlparser-cs-netstandard20.patch`, validado no mesmo commit), que a recompila para `netstandard2.0`.
- O código do benchmark referencia o SqlParser-cs pelo `extern alias SqlParserCs` (os dois projetos usam o namespace `SqlParser`).

## Ambiente

Notebook com Intel Core i7-1255U, Windows 11, .NET 8.0.28 e .NET Framework 4.8. Core em Release, no commit `4f4d77e` (sincronizado com o Rust `dacdbc9`); SqlParser-cs no commit `e1929977`.

## Resultados parciais

**Cargas de trabalho:**
- `Oltp`: `SELECT a, b FROM t WHERE id = 1`;
- `Tpch22`: as 22 consultas TPC-H;
- `Insert1000`: `INSERT` com 1.000 linhas em `VALUES`;
- `Select300`: `SELECT` com 300 colunas;
- `Where200`: `WHERE` com 200 predicados ligados por `AND`.

Valores em média por execução da carga inteira.

**.NET 8** (`run3`):

| Carga | Tokenizar: nosso | Tokenizar: SqlParser-cs | Parse: nosso | Parse: SqlParser-cs |
|---|---:|---:|---:|---:|
| Oltp | 6 µs | 13 µs | 110 µs | 96 µs |
| Tpch22 | 1,9 ms | 6,1 ms | 30 ms | 13 ms |
| Select300 | 0,5 ms | 3,7 ms | 11 ms | 5,8 ms |
| Where200 | 0,5 ms | 2,1 ms | 24 ms | 4,5 ms |
| Insert1000 | 9,5 ms | 10,2 ms | 139 ms | 21 ms |

**.NET Framework 4.8** (`run3`, parcial):

| Carga | Tokenizar: nosso | Tokenizar: SqlParser-cs | Parse: nosso | Parse: SqlParser-cs |
|---|---:|---:|---:|---:|
| Oltp | 2,6 µs | 21 µs | 65 µs | 33 µs |
| Tpch22 | 1,8 ms | 5,9 ms | 15 ms | 5,1 ms |
| Select300 | 0,3 ms | 2,6 ms | 5,5 ms | 2,8 ms |
| Where200 | 0,4 ms | 2,4 ms | 10 ms | 1,8 ms |
| Insert1000 | 6,1 ms | 8,9 ms | 143 ms | 27 ms |

**Leitura dos números:**
- **Tokenizar:** o nosso tokenizer é de 1,5 a 8 vezes mais rápido.
- **Parse:** ficamos de 1,1 a 7 vezes atrás, e a diferença cresce com o número de expressões.
- **Uso comum:** para uma consulta por vez, o custo absoluto é baixo, de 0,1 a 1,5 ms por consulta.

**Causa principal: exceções como controle de fluxo.** Medida com `--analyze`. O Rust devolve `Err` (barato) nas tentativas especulativas de `maybe_parse`/`try_parse`. No C#, cada tentativa que falha lança e captura uma `ParserException`, o que custa de 5 a 11 µs cada (`MicroBenchmarks`).

| Carga | Nós `Expr` | Exceções lançadas por parse (nosso) | Exceções (SqlParser-cs) |
|---|---:|---:|---:|
| Oltp | 5 | 4 | 0 |
| Tpch22 | 1.008 | 688 | 0 |
| Select300 | 300 | 300 | 0 |
| Where200 | 839 | 480 | 0 |
| Insert1000 | 6.000 | 6.000 | 0 |

A maior parte vem de uma única especulação, em `Parser.ParsePrefixInner`: a tentativa de ler um literal tipado (`DATE '...'`, `INTERVAL ...`), que falha em quase toda expressão. É a primeira candidata a otimização. A troca da exceção por um retorno sem exceção mantém o comportamento igual ao do Rust, desde que preserve a mesma restauração de posição e a mesma precedência de erros (`convencoes.md` §13, casos 81 e 124). **Tratado na P2:** ver [Depois da P2](#depois-da-p2).

**Comparação de `ToString()` (`--crosscheck`).** Nas 22 consultas TPC-H, 26 instruções saem idênticas nas duas bibliotecas. As diferenças estão nas consultas 7, 8, 13 e 21 e vêm de correções do sqlparser-rs que o SqlParser-cs (de 2025) ainda não tem:
- o alias de tabela sem `AS` é preservado (`nation n1`);
- `LEFT OUTER JOIN` é preservado.

Nos dois casos, o nosso parser segue o Rust atual.

**Limite de recursão.** Subconsultas aninhadas a partir de 30 níveis dão `recursion limit exceeded` no nosso parser, com o limite padrão de 50. O SqlParser-cs não tem essa proteção. É o comportamento do Rust: cada nível de subconsulta consome mais de uma unidade do limite.

## Depois da P2

A unidade P2 ([`docs/port/onda-d/P2.md`](../../docs/port/onda-d/P2.md)) tirou as exceções das especulações quentes: o `Err` que o `maybe_parse` descarta na especulação de literal tipado (`parse_prefix_inner`) e na de argumento nomeado (`parse_function_args`) passou a ser sinalizado por `null`, e o `MaybeParse` relança uma vez só. O comportamento é idêntico (`convencoes.md` §13, caso 140): 17,6 milhões de comparações entre o Core antigo e o novo, sem nenhuma diferença.

**Como foi medido.** Não com o BenchmarkDotNet, mas com um harness próprio da P2 (fora do repositório): o Core antigo e o novo no mesmo processo (`extern alias`), Release, `GenericDialect`, `Stopwatch`, mediana de 15 amostras de ~150 ms alternando os dois, média de duas rodadas, processo fixado no processador lógico 2 com prioridade alta, máquina sem outra carga. Ambiente da seção [Ambiente](#ambiente). Cargas: as cinco acima, mais `In10000` (`WHERE id IN (1, ..., 10000)`) e `Funcs100` (100 chamadas de função de formas comuns, como `COUNT(*)`, `SUM(a * b)`, `COALESCE(d, 0)`, `ROUND(e, 2)`).

| Carga | Exceções antes → depois | net8.0 antes | net8.0 depois | net48 antes | net48 depois |
|---|---:|---:|---:|---:|---:|
| Oltp | 4 → 0 | 51,7 µs | 3,7 µs | 61,5 µs | 6,5 µs |
| Tpch22 | 688 → 35 | 9,55 ms | 1,28 ms | 11,49 ms | 2,34 ms |
| Select300 | 300 → 0 | 3,82 ms | 0,31 ms | 4,66 ms | 0,59 ms |
| Where200 | 480 → 40 | 6,67 ms | 0,62 ms | 7,45 ms | 0,81 ms |
| Insert1000 | 6.000 → 0 | 91,1 ms | 2,31 ms | 100,6 ms | 5,44 ms |
| In10000 | 10.002 → 1 | 148,9 ms | 3,01 ms | 169,4 ms | 9,97 ms |
| Funcs100 | 361 → 20 | 4,90 ms | 0,41 ms | 5,41 ms | 0,56 ms |

"Exceções" são as exceções distintas por parse da carga inteira. Antes, cada uma era lançada duas vezes (o `TryParse` relançava); depois, uma. As que sobram vêm de especulações que falham fundo na pilha, onde evitar a exceção exigiria mudar a forma de funções grandes do port: `IN (lista)` (uma por lista: o `maybe_parse(parse_query)` falha), chamadas de função cujo primeiro argumento não é um literal ou identificador simples (`COUNT(*)`, `SUM(a * b)`: os modificadores de tipo do literal tipado) e uma cauda longa de DDL e dialetos.

**Cuidado ao medir no .NET 8 com um processador só.** Fixar o processo num processador lógico **e** subir a thread de medição para `ThreadPriority.Highest` deixa sem CPU a thread de fundo da compilação em camadas, e o código fica no tier-0 (no harness da P2: o Core novo dava 20 µs no `Oltp` assim, 3,7 µs sem a prioridade `Highest` e 5,6 µs com `DOTNET_TieredCompilation=0`). O BenchmarkDotNet também roda as medições com prioridade alta, e o `--pin 4` deste benchmark usa um processador só: é uma explicação provável para o net8.0 aparecer mais lento que o net48 nos [Resultados parciais](#resultados-parciais) (não confirmada). Ao refazer, prefira `-Pin 12` (os dois processadores lógicos do mesmo núcleo P) ou confira com `DOTNET_TieredCompilation=0`.

**SqlParser-cs.** A comparação com ele não foi refeita na P2. Os números de parse dele nos resultados parciais (por exemplo, TPC-H em 13 ms no net8.0 e 5,1 ms no net48) vêm de outro método e talvez tenham o viés acima; servem só de ordem de grandeza. Para números finais, rode o `Run-Bench.ps1` completo.
