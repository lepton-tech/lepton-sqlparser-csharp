# Análise técnica comparativa e plano de execução

**Projeto:** parser SQL em C# (`netstandard2.0`) com paridade funcional com o [`apache/datafusion-sqlparser-rs`](https://github.com/apache/datafusion-sqlparser-rs)
**Data:** 2026-10-14
**Status:** primeira entrega — análise e plano. Nenhuma funcionalidade implementada e nenhuma operação Git realizada.

> **Atualização (2026-10-14, após a aprovação).** Decisões tomadas depois desta análise; elas **prevalecem** sobre os trechos correspondentes abaixo.
>
> | Tema | Decisão | Onde |
> |---|---|---|
> | Abordagem | Port independente aprovado; Plano B (fork do SqlParser-cs em `netstandard2.0`) documentado com gatilhos objetivos | [ADR-0001](adr/0001-abordagem-port-independente-e-plano-b.md), [`plano-b/`](plano-b/README.md) |
> | Arquitetura | Duas camadas: `SqlParser.Core` (port fiel, nomes do Rust sem exceções) + `SqlParser` (API pública no estilo do SqlParser-cs, customizações). Substitui o nome provisório `SqlParserNet` e a estrutura da §7.2 | [ADR-0002](adr/0002-arquitetura-em-camadas-e-api.md) |
> | Pipeline | **Sem CI por enquanto**: repositório local. Hospedagem (Bitbucket?) e visibilidade a decidir. A §7.8 fica para depois | [ADR-0004](adr/0004-baseline-e-sincronizacao.md) |
> | Licença | Obrigações Apache-2.0 cumpridas desde já; licença do projeto pendente | [ADR-0003](adr/0003-licenciamento-e-atribuicao.md) |
> | Linguagem | C# 14 + PolySharp sobre `netstandard2.0` (risco registrado) | [ADR-0006](adr/0006-versao-da-linguagem-csharp.md) |
> | Processo | Convenções normativas, memória de tradução, skills e boas práticas com fontes | [`port/`](port/), `.claude/skills/` |

---

## 1. Sumário executivo

**Recomendação:** fazer uma **implementação independente**, portando diretamente do Rust (baseline `v0.63.0`) com estrutura 1:1. O SqlParser-cs entra como **referência e fonte seletiva de código** (é MIT), **não como base de fork**.

Os números que sustentam a decisão:

| Indicador | Resultado |
|---|---|
| Esforço para o SqlParser-cs compilar em `netstandard2.0` | **baixo**: 13 arquivos, +54/−35 linhas e 3 pacotes de polyfill (medido) |
| Testes do próprio SqlParser-cs no .NET Framework 4.8 | **1.185 de 1.187** passam; as 2 falhas estão no visitor baseado em reflection |
| Paridade de comportamento: casos SQL extraídos dos testes do Rust, rodados no SqlParser-cs | **54%** (1.652 de 3.054 casos positivos) |
| Saída divergente sem erro ("divergência silenciosa") | **319 casos**: parseia, mas produz árvore/SQL diferente do Rust |
| Funções `parse_*` do Rust com equivalente de mesmo nome no C# | 52% (265 de 509) |
| Hooks do trait `Dialect` (Rust → C#) | 185 → 56 (152 → 36 `supports_*`) |
| Atraso em relação ao upstream | sincronização parcial até ~v0.60; faltam v0.61–v0.63 + HEAD (~290 commits, +16 mil linhas em `src/`) |
| Manutenção do SqlParser-cs | 94% dos commits de um único autor; 5 commits nos últimos 12 meses; última versão estável no NuGet em 2025-04 |

**Por que não um fork, se a portabilidade é barata?**
- O gargalo não é o framework-alvo. É a **paridade** e a **capacidade de acompanhar o upstream**.
- O SqlParser-cs diverge do Rust na estrutura do parser, no modelo de dialetos (checagem de tipo em vez de flags `supports_*`), na AST (sem spans, formas antigas de vários nós) e no visitor.
- Alcançar a v0.63 exigiria reescrever a maior parte do parser, dos dialetos e do visitor e mexer em quase toda a AST, sem ter as vantagens de uma base limpa e alinhada ao Rust.
- Sem correspondência 1:1 com o Rust, cada PR do upstream exige "tradução estrutural". Foi assim que o SqlParser-cs acumulou a defasagem atual.

**O que se aproveita do SqlParser-cs:**
- o padrão de AST com `abstract record` e registros aninhados;
- a infraestrutura de escrita de SQL (writer com interpolated string handler);
- os `ToSql` como referência de port;
- as lições das issues abertas.

Todo código copiado mantém o aviso MIT.

---

## 2. Escopo e metodologia

**Versões analisadas:**

| Repositório | Revisão | Data |
|---|---|---|
| sqlparser-rs | `dacdbc9` (HEAD; release mais recente `v0.63.0`, de 2026-09-08) | 2026-10-12 |
| SqlParser-cs | `e192997` (HEAD) | 2026-06-07 |

**O que foi feito:**

1. **Leitura estrutural** dos dois códigos-fonte: módulos, contagens, histórico Git, changelogs, CI e licenças.
2. **Paridade estrutural:** comparação automatizada das variantes dos principais tipos-soma da AST, das keywords, dos dialetos e dos hooks de dialeto.
3. **Experimento `netstandard2.0`:**
   - retarget da biblioteca C# para `netstandard2.0`, em cópia temporária;
   - correção mínima dos erros de compilação;
   - execução da suíte de testes original em **.NET Framework 4.8**.
4. **Teste diferencial:**
   - extração automática de **4.089 casos SQL** (3.853 SQLs distintos) dos testes Rust: `verified_stmt`, `one_statement_parses_to`, `verified_expr`, casos negativos etc.;
   - execução desses casos contra o SqlParser-cs (net8.0), com a mesma semântica de verificação: parse → `ToSql` → comparação exata com o SQL esperado.
5. **Sinais públicos:** issues abertas no GitHub e histórico de versões no NuGet.

**Limitações:**
- As contagens de paridade estrutural casam por nome, então **superestimam** a paridade real: uma variante existir não garante que os campos sejam iguais.
- O teste diferencial deixou de fora 777 casos. Os principais motivos:
  - dialetos selecionados por capacidade (`all_dialects_where(...)`);
  - helpers incomuns;
  - a categoria "parse-only", em que o extrator confundiu alguns testes negativos com positivos.
- Os experimentos rodaram em diretório temporário, e não há Rust instalado na máquina. Nada foi compilado do lado Rust.

---

## 3. sqlparser-rs (Rust)

### 3.1 Licença

- Apache-2.0 (`LICENSE.TXT`).
- `NOTICE.TXT` foi adicionado em 2026-09-10 (#2496), portanto **depois** da v0.63.0.
- Cabeçalho ASF em 65 de 69 arquivos `.rs`.
- A CI verifica licenças com Apache RAT.

### 3.2 Arquitetura

| Módulo | Linhas | Papel |
|---|---|---|
| `src/tokenizer.rs` | 4.679 | `Token` (105 variantes), `Whitespace` (comentários como tokens), `Location`/`Span`, `TokenWithSpan`, unescape |
| `src/parser/mod.rs` + `alter.rs` + `merge.rs` | ~23.100 | Parser descendente recursivo + Pratt para expressões; ~546 funções `parse_*`/`maybe_parse_*` |
| `src/ast/*` | ~33.000 | ~535 tipos públicos. `Statement` com 141 variantes, `Expr` com 64. 455 `impl Display` |
| `src/ast/spans.rs` | 3.201 | Trait `Spanned` com 120 implementações; cobertura parcial: 96 de 133 braços de `Statement` retornam `Span::empty()` |
| `src/ast/visitor.rs` + `derive/` | 1.376 + proc-macro | `Visit`/`VisitMut` gerados por `#[derive]`, com hooks pré/pós (relações, expressões, statements, valores, select, ident, order by, group by) |
| `src/dialect/*` | ~6.900 | Trait `Dialect` com **185 métodos (152 `supports_*`)** e 16 dialetos |
| `src/keywords.rs` | 1.321 | Macro `define_keywords!`: ~1.104 keywords e listas de reservadas para alias |
| `src/ast/comments.rs`, `display_utils.rs` | 338 + 135 | API de comentários (v0.61) e pretty-print via `{:#}` (v0.57) |

**Pontos de projeto que o port precisa reproduzir:**

- **Expressões (Pratt):**
  - O fluxo é `parse_subexpr` → `parse_prefix` → laço de `get_next_precedence`/`parse_infix`.
  - A precedência é definida pelo dialeto (`prec_value`). O PostgreSQL tem tabela própria.
- **Dialetos:** o comportamento muda de duas formas:
  1. hooks (`parse_statement`, `parse_prefix`, `parse_infix`, `get_next_precedence`, `parse_column_option`);
  2. flags `supports_*`, consultadas 181 vezes no parser.
- **Macros legadas:** `dialect_of!`/`dialect_is!` caíram de 156 para 101 ocorrências desde a v0.53. O próprio `AGENTS.md` do projeto proíbe novas ocorrências: o upstream está migrando para flags.
- **Proteção de recursão:**
  - limite padrão de 50, verificado em 6 pontos (statement, subexpr, interval, data type, query, table factor);
  - crescimento de pilha (`recursive`) ativado por padrão.
- **Memoização de backtracking (v0.63):** 5 tabelas de posições que já falharam, para evitar tempo exponencial.
- **Erros:**
  - tipos `ParserError::{TokenizerError, ParserError, RecursionLimitExceeded}`;
  - mensagens no formato `Expected: X, found: Y` + ` at Line: L, Column: C`;
  - 158 testes conferem a mensagem exata.
- **Igualdade da AST:** spans são ignorados em `Eq`/`Hash` (`Ident`, `AttachedToken`).
- **Features de compilação:**
  - `serde` serializa a AST inteira, incluindo spans. Isso é essencial para a estratégia de oráculo da seção 7.5;
  - `visitor`, `bigdecimal` (troca o tipo de `Value::Number`), `no_std`.

### 3.3 Ritmo e natureza da evolução

| Release | Data | Commits | `src/` (+/−) | `tests/` (+/−) |
|---|---|---|---|---|
| v0.53 – v0.59 | 2024-12 → 2025-09 | 41–60 por release | +1.900 a +5.200 por release | — |
| v0.60.0 | 2025-12-02 | 39 | +4.979 / −1.639 | +2.461 / −774 |
| v0.61.0 | 2026-02-06 | 66 | +7.865 / −1.748 | +3.561 / −602 |
| v0.62.0 | 2026-05-01 | 80 | +4.585 / −925 | +3.344 / −294 |
| v0.63.0 | 2026-09-08 | 80 | +2.712 / −452 | +2.835 / −542 |
| HEAD (pós-v0.63) | até 2026-10-12 | 63 | +1.169 / −240 | +1.721 / −42 |

**Observações relevantes para o port:**

- **Ritmo:** uma release a cada 1–4 meses, com 40–80 PRs. Foram 316 commits nos últimos 12 meses.
- **Quebras de AST sem rótulo:**
  - O changelog é gerado de títulos e labels de PR, e o rótulo "Breaking" aparece em só 2 de ~600 entradas.
  - Mesmo assim, **quase toda release quebra a AST**: campos novos, mudanças de tipo (`Value` → `ValueWithSpan`, `Ident` → `ObjectName`), variantes reestruturadas.
  - O acompanhamento, portanto, **não pode depender de labels**: precisa calcular o diff de API.
- **Grandes refatorações recentes:**
  - variantes de `Statement` passaram a ter structs dedicadas (v0.60: `Update`, `CreateView`, `AlterTable`… v0.61: `Grant`, `Revoke`, `Merge`, policies);
  - `CompoundFieldAccess` e `ObjectNamePart` (v0.54–v0.55);
  - `ValueWithSpan` (v0.55/v0.62).
- **Sinais aproveitáveis para automação:**
  - commits squash com `(#NNNN)` no título (100% desde a v0.52);
  - títulos no formato `<DIALETO>: descrição` (~50%);
  - labeler automático de dialetos;
  - testes acrescentados no fim de `tests/sqlparser_<dialeto>.rs`.

### 3.4 Testes

- **Volume:** 1.490 `#[test]` em `tests/` e 139 em `src/`, somando ~55 mil linhas de teste.
- **Helpers:** os testes usam majoritariamente helpers de round-trip textual:

  | Helper | Chamadas |
  |---|---|
  | `verified_stmt` | 2.420 |
  | `verified_only_select` | 607 |
  | `one_statement_parses_to` | 428 |
  | `verified_expr` | 207 |
  | outros | … |

- **Corpus extraível:**
  - **93% dos SQLs** (4.089 de 4.397 chamadas) são extraíveis mecanicamente como literais, junto com o conjunto de dialetos alvo;
  - são 3.062 round-trips de identidade, 320 pares (entrada → forma canônica) e 324 casos negativos.
- **Asserções estruturais:** ~968 testes também verificam a estrutura da AST. A estratégia de oráculo JSON (seção 7.5) evita portar essas asserções uma a uma.

---

## 4. SqlParser-cs (C#)

### 4.1 Licença e atribuição

- **Licença:** MIT ("Copyright (c) 2023 Tyler Brinks").
- **Atribuição ausente:**
  - Não há texto Apache-2.0, NOTICE nem cabeçalhos de origem.
  - A única menção ao projeto Rust está no README.
- **Consequência:**
  - Como é uma tradução do sqlparser-rs, juridicamente é uma obra derivada de código Apache-2.0.
  - **Quem reutilizar esse código herda a obrigação de atribuição ao Apache** (ver 7.9).
- **Pacote NuGet:** a issue #86 aponta que o pacote não traz informação de licença.

### 4.2 Arquitetura

| Componente | Situação |
|---|---|
| Frameworks | `net8.0;net9.0;net10.0`, C# 12, `ImplicitUsings`, `Nullable` |
| Tamanho | 38.240 linhas em 291 arquivos: parser 14,7 mil, AST 17 mil, dialetos 2,2 mil, tokenizer 1,2 mil + 90 classes de token |
| Parser | Arquivos `partial` (`Parser.cs` 7,4 mil, `Statements`, `Expressions`, `Base`, `Sequences`). Backtracking por exceções, com estado não reentrante (`MaybeParse`, 50 usos). Recursão protegida em 3 pontos (o Rust protege 6) |
| Dialetos | 14: faltam Spark e Teradata. Classe base com 56 membros. Comportamento decidido por ~160 checagens `_dialect is XDialect` |
| AST | `abstract record` com registros aninhados. Igualdade por valor. `Sequence<T>` deriva de `List<T>` (mutável, com hash baseado em conteúdo). **Sem spans, sem API de comentários, sem pretty-print** |
| Visitor | Por **reflection**, só leitura, 6 pares de hooks. Bugs identificados por leitura: não desce em table factors que não sejam `Table`; ignora `Break` dos filhos; hook de relação em 1 lugar contra 22 no Rust |
| ToSql | 735 implementações via `SqlTextWriter` + interpolated string handler. Equivalente ao `Display` do Rust |
| API pública | ~1.321 tipos públicos e 296 métodos `Parse*` públicos. Quase qualquer refatoração quebra a API |

### 4.3 Manutenção

- **Commits:** 363 no total, 94% de um único autor. Cinco commits nos últimos 12 meses. Último código de autoria do mantenedor em 2026-02-06; três PRs de terceiros foram integrados em 2026-06-07.
- **Histórico de sincronização com o Rust:**
  - port inicial num único commit de 32 mil linhas (2023);
  - espelhamento PR a PR em 2024;
  - um commit "big-bang" em 2026-02 (+10,5 mil linhas, aparentemente assistido por IA).
  - **Não há mecanismo de rastreamento do upstream**, nem registro da versão Rust correspondente.
- **NuGet (`SqlParserCS`):**
  - estável 0.6.5 em 2025-04-02 (net7/net8);
  - pré-release 0.6.6 em 2026-02-06;
  - ~781 mil downloads;
  - a issue #88 pede uma nova release.
- **Issues abertas relevantes (30 no total):**

  | Issue | Assunto |
  |---|---|
  | #89 | Tokenizer em **loop infinito** com literais `E'...'` não terminados. Reproduzido no teste diferencial com `SELECT E'\'` |
  | #78 | Escaping incorreto no dialeto Redshift, relatado como vulnerabilidade de SQL injection |
  | #76 | Pedido de visitor mutável |
  | #50 | Pedido de recuperar o texto original (spans) |
  | #67 | Uso em frameworks antigos |

### 4.4 Testes

- **Volume:** 1.156 métodos (1.187 casos executados), **100% aprovados em net8.0**. 721 nomes de teste correspondem a testes do Rust, o que mostra que foram traduzidos.
- **Verificações mais fracas que as do Rust:**
  - comparação de round-trip **case-insensitive**;
  - não verifica se dialetos diferentes produzem a mesma AST;
  - `AllDialects` omite ClickHouse e Oracle.
- **Cobertura:** cerca de 1.960 asserções SQL, contra ~4.400 no Rust.

---

## 5. Comparação

### 5.1 Visão lado a lado

| Aspecto | sqlparser-rs | SqlParser-cs |
|---|---|---|
| Licença | Apache-2.0 (+NOTICE) | MIT, sem atribuição ao Apache |
| Estado | v0.63.0 + HEAD ativo | Parcialmente alinhado até ~v0.60; parado desde 2026-06 |
| Código | ~72 mil linhas | ~38 mil linhas |
| Testes | 1.629 testes / ~4.400 asserções SQL | 1.156 testes / ~1.960 asserções SQL |
| Dialetos | 16 | 14 |
| Hooks de dialeto | 185 (152 `supports_*`) | 56 (36 `Supports*`) |
| Keywords | ~1.104 | 845 (faltam 266) |
| Variantes dos 11 principais tipos-soma da AST | 483 | 406 equivalentes por nome (84%) |
| `Statement` | 141 variantes | 116 equivalentes |
| Spans na AST | Sim | Não (tokens só têm posição inicial) |
| Visitor | Gerado; `Visit` + `VisitMut`; 11 pares de hooks | Reflection; só leitura; 6 pares; com bugs |
| Pretty-print / comentários | Sim / Sim | Não / Não |
| Recursão | 6 pontos + crescimento de pilha | 3 pontos (risco de `StackOverflowException`, que derruba o processo) |
| Backtracking | `Result` + memoização de falhas | Exceções, estado não reentrante |
| Atividade (12 meses) | 316 commits, comunidade ASF | 5 commits |

### 5.2 Divergências estruturais críticas, em especial para análise de SELECT

1. **Sem spans.** Não há como mapear nós da AST de volta ao texto original.
2. **Acesso a campos/índices no modelo antigo.** O C# ainda usa `MapAccess`/`Subscript`/`CompositeAccess` em vez de `CompoundFieldAccess`. Expressões como `a[1].b` geram árvores de outro formato.
3. **`Select` com 19 de 24 campos.** Faltam `optimizer_hints`, `select_modifiers`, `exclude`, `flavor` (FROM-first) e `select_token`.
4. **`Query` sem `pipe_operators`.** `LIMIT x,y` do MySQL é reescrito para `LIMIT y OFFSET x`, o que perde a forma original.
5. **`ObjectName` sem `ObjectNamePart`.** `expr.*` não é representável.
6. **Joins perdem fidelidade.** `JOIN` e `INNER JOIN` viram o mesmo nó. Faltam `SEMI`, `ANTI` e `ARRAY JOIN`.
7. **121 flags `supports_*` ausentes.** Um dialeto customizado não consegue habilitar recursos sem herdar de um dialeto pronto.

### 5.3 Paridade de comportamento (teste diferencial)

Corpus extraído dos testes do Rust (v0.63+), rodado contra o SqlParser-cs com verificação idêntica à do Rust.

**Por tipo de caso:**

| Tipo de caso | Aprovados | Total | % |
|---|---|---|---|
| Round-trip de statement (`verified_stmt` e similares) | 1.345 | 2.507 | 53,6% |
| Forma canônica (`one_statement_parses_to`) | 202 | 389 | 51,9% |
| Round-trip de expressão (`verified_expr`) | 103 | 145 | 71,0% |
| Expressão canônica | 2 | 13 | 15,4% |
| **Total de casos positivos** | **1.652** | **3.054** | **54,1%** |
| Casos negativos (deve rejeitar) | 232 | 258 | 89,9% (+1 loop infinito) |

**Por arquivo de teste do Rust** (positivos e negativos):

| Arquivo | % |
|---|---|
| hive | 94% |
| duckdb | 77% |
| postgres | 69% |
| common | 63% |
| clickhouse | 63% |
| sqlite | 59% |
| mysql | 53% |
| bigquery | 51% |
| snowflake | 44% |
| databricks | 38% |
| mssql | 36% |
| oracle | 33% |
| redshift | 33% |
| spark | 0% (dialeto ausente) |
| teradata | 0% (dialeto ausente) |

**Como os casos falham:**
- ~1.025 dão erro de parse;
- 319 **parseiam, mas produzem saída diferente**;
- 63 são de dialetos ausentes.

**Exemplos de divergência silenciosa** (o SQL é aceito, mas a árvore está errada):

| Entrada | Saída do SqlParser-cs |
|---|---|
| `SELECT * FROM t1 ANTI JOIN t2 ON c1 = c2` | `SELECT * FROM t1 AS ANTI JOIN t2 ...` (`ANTI` virou alias) |
| `SELECT INTERVAL '1' SECONDS` | `SELECT INTERVAL '1' AS SECONDS` |
| `select * from foo-123.bar` (BigQuery) | `SELECT * FROM foo- AS bar` |
| `SHOW TERSE OBJECTS LIKE '%test%' IN abc` (Snowflake) | `SHOW TERSE OBJECTS LIKE IN abc` (o padrão foi perdido) |

São justamente os erros que o requisito "evitar divergências silenciosas" quer impedir. Eles só aparecem com **comparação automatizada contra o Rust**.

### 5.4 Viabilidade em `netstandard2.0` (experimento)

| Etapa | Erros únicos de compilação |
|---|---|
| Retarget direto para `netstandard2.0` (LangVersion latest) | 1.140, quase todos por falta de `IsExternalInit` (records/`init`) |
| + PolySharp (só em build) + System.Memory | 7: *default interface members* (CS8701), bloqueio real no .NET Framework |
| Após mover os DIMs para extensões | 15 lacunas de BCL (`HashCode`, `char.IsAscii`, `Enum.GetNames<T>`, `IsAssignableTo`, sobrecargas) |
| Após correções + Microsoft.Bcl.HashCode | **0 erros**. Patch total: 13 arquivos, +54/−35 linhas |

Resultados:
- A suíte original, adaptada para `net48` (5 ajustes em testes), teve **1.185 de 1.187 aprovados**.
- As 2 falhas são `TargetParameterCountException` no visitor por reflection: em .NET Framework, a enumeração de propriedades inclui um indexador.

**Conclusão:** `netstandard2.0` **não** é obstáculo, nem para um fork nem para uma implementação nova. A biblioteca pode ficar **sem dependências de runtime**:
- PolySharp só atua na compilação;
- `HashCode` e `Span` podem ser evitados com código próprio.

---

## 6. Fork ou implementação independente

### 6.1 Opções avaliadas

- **A — Fork do SqlParser-cs:** retarget para `netstandard2.0` e evolução incremental até a paridade.
- **B — Implementação independente, port direto do Rust**, sem reaproveitar nada do SqlParser-cs.
- **C — Implementação independente com estrutura 1:1 ao Rust e reaproveitamento seletivo do SqlParser-cs** (padrões, infraestrutura de escrita de SQL, `ToSql` como referência, lições das issues).

### 6.2 Avaliação

| Critério | A — Fork | C — Independente + reúso seletivo |
|---|---|---|
| Tempo até ter algo utilizável | **Curto** (dias) | Médio (preview focado em SELECT após as Fases 0–2) |
| Paridade com a v0.63 | Exige retrabalho em parser, dialetos, visitor e quase toda a AST | Construída por design, módulo a módulo |
| Acompanhamento do upstream | **Caro**: cada PR Rust precisa ser "traduzido" para outra estrutura | Barato: diff Rust → arquivo/método C# correspondente |
| Divergência silenciosa | Alta (319 casos já existentes, testes case-insensitive) | Controlada por oráculo JSON + ratchet desde o primeiro dia |
| API pública | Herda 1.321 tipos públicos com nomes divergentes (`Expression` vs `Expr`) | Nomes mecânicos a partir do Rust |
| Spans, `VisitMut`, comentários, pretty-print | Mudanças transversais sobre código existente | Previstos desde a fundação |
| Licenciamento | Exige regularizar a atribuição Apache retroativamente | Atribuição correta desde o início |
| Dependência de terceiros | Mantenedor praticamente inativo; contribuição upstream improvável (o alvo dele é net8+) | Nenhuma |

### 6.3 Recomendação

**Opção C.** Uma "substituição gradual" dentro de um fork (refatorar módulo por módulo) **não funciona bem** aqui:
- a AST é compartilhada por parser, `ToSql`, visitor e testes;
- as mudanças necessárias são transversais (spans, `ObjectNamePart`, `CompoundFieldAccess`, `ValueWithSpan`, structs de `Statement`) e reverberam por todo o código.

Na prática, seria uma reescrita feita com mais atrito.

**O que reaproveitar do SqlParser-cs, mantendo o aviso MIT em `THIRD-PARTY-NOTICES`:**
- o padrão `abstract record` + registros aninhados (com regras de nome ajustadas, ver 7.3);
- `SqlTextWriter` + interpolated string handler (funciona em `netstandard2.0` com atributos polyfill);
- implementações `ToSql`, **como referência** para portar os `Display` do Rust. A saída é sempre validada contra o oráculo;
- a lista de issues abertas, como requisitos e casos de teste (#89 loop do tokenizer, #78 escaping, #76 visitor mutável, #50 texto original).

**Alternativa provisória (opcional):** se houver necessidade imediata de um parser em `netstandard2.0`, o patch de 54 linhas do experimento pode ser usado **internamente** enquanto o novo parser amadurece. Não deve ser a base do projeto.

---

## 7. Arquitetura proposta

### 7.1 Princípios

1. **Correspondência 1:1 com o Rust:**
   - cada arquivo `.rs` tem um destino conhecido em C#;
   - cada `fn parse_x` vira `ParseX`;
   - cada `supports_x` vira `SupportsX`.

   Desvios ficam registrados numa tabela de exceções.
2. **O Rust é o oráculo.** A paridade é medida por comparação automatizada (texto canônico, AST em JSON e mensagens de erro), não por inspeção.
3. **Zero dependências de runtime.** Só o `netstandard2.0`. Geradores e polyfills atuam apenas na compilação.
4. **Nada de divergência silenciosa:**
   - toda incompatibilidade conhecida fica numa lista versionada (`xfail`);
   - a CI falha se um caso que passava regredir, ou se um `xfail` começar a passar sem a lista ser atualizada (ratchet).

### 7.2 Estrutura da solução (nome provisório `SqlParserNet`)

```
src/
  SqlParserNet/                  netstandard2.0, sem dependências de runtime
    Tokenizer/                   ← tokenizer.rs
    Parser/                      ← parser/mod.rs (partial por seção), alter.rs, merge.rs
    Ast/                         ← ast/mod.rs, query.rs, ddl.rs, dml.rs, dcl.rs, data_type.rs,
                                   value.rs, operator.rs, spans.rs, comments.rs, helpers/
    Dialect/                     ← dialect/mod.rs + um arquivo por dialeto
    Keywords.g.cs                ← gerado a partir de keywords.rs
  SqlParserNet.Generators/       gerador Roslyn (netstandard2.0, só compilação):
                                 Visitor/Rewriter, escritor JSON "serde-compatível"
tests/
  SqlParserNet.Tests/            net48; net8.0; net10.0. Um arquivo por arquivo de teste Rust
  SqlParserNet.Conformance/      corpus + goldens do oráculo + lista xfail
tools/
  oracle/                        (Rust, só em dev/CI) sqlparser-rs fixado → goldens JSONL
  UpstreamTracker/               (.NET) diff do upstream, extração de corpus, relatório
docs/
  adr/  compatibility.md  upstream-sync.md
UPSTREAM.json                    versão/commit do Rust portados
LICENSE  NOTICE  THIRD-PARTY-NOTICES.md
```

### 7.3 Mapeamento Rust → C#

| Rust | C# |
|---|---|
| `enum E { A(T), B { x: X }, C }` | `abstract record E` com `sealed record A(T Value) : E`, `sealed record B(X X) : E`, `sealed record C : E` |
| `enum` só com variantes unitárias | `enum` C# |
| `struct S { … }` | `sealed record S(...)` (construtor posicional) |
| `Option<T>` / `Box<T>` / `Vec<T>` | `T?` / `T` / `ValueList<T>` (`IReadOnlyList<T>` imutável, com igualdade estrutural) |
| `impl Display` / `{:#}` | `ToSql(SqlWriter)` + `ToString()` retornando SQL / `SqlWriter` em modo pretty |
| `Result<T, ParserError>` | Exceções (`TokenizerException`, `ParserException`, `RecursionLimitExceededException`, todas derivadas de `SqlParserException`) com **mensagens idênticas às do Rust** + API `TryParse` |
| `trait Dialect` (valores padrão) | `abstract class Dialect` com membros `virtual` e os mesmos padrões |
| `dialect_of!(self is A \| B)` | Helper `DialectOf<A, B>()`, mantido enquanto o upstream mantiver |
| `#[derive(Visit, VisitMut)]` | Gerador Roslyn: `AstVisitor` (pré/pós, `ControlFlow`) e `AstRewriter` (equivalente imutável de `VisitMut`) |
| `PartialEq/Eq/Hash` ignorando spans | Igualdade de `record`, com `Span` excluído da igualdade |
| `define_keywords!` | `Keyword` enum + array ordenado, gerados |
| `trait Spanned` | `ISpanned.Span` |
| `RecursionCounter` + `recursive` | `DepthGuard` nos mesmos 6 pontos + `RuntimeHelpers.EnsureSufficientExecutionStack()`. No .NET, estouro de pilha encerra o processo, então é proteção obrigatória para entrada não confiável |
| feature `serde` | Escritor JSON gerado, no formato do serde (externally tagged, snake_case), usado nos testes de conformidade |

**Regras de nomenclatura:**
- `snake_case` → `PascalCase` de forma mecânica. Os nomes de tipos Rust são mantidos (`Expr`, `SetExpr`, `TableFactor`), e não `Expression`.
- Conflitos do C#, como um membro com o mesmo nome do tipo que o contém (CS0542), seguem uma regra fixa documentada em ADR.

**Mutabilidade:** a AST do Rust é mutável (`VisitMut`). A recomendação é uma **AST imutável + `AstRewriter`** (modelo do Roslyn): dá segurança para uso em threads e igualdade por valor. É uma decisão em aberto (seção 10).

### 7.4 API pública (esboço)

```csharp
using SqlParserNet;
using SqlParserNet.Ast;
using SqlParserNet.Dialect;

var statements = Parser.ParseSql(new PostgreSqlDialect(), "SELECT a FROM t WHERE b > 1");

if (statements[0] is Statement.Query q && q.Value.Body is SetExpr.Select s)
{
    foreach (var item in s.Value.Projection) { /* ... */ }
}

string sql    = statements[0].ToString();            // equivale ao Display do Rust
string pretty = statements[0].ToSql(pretty: true);   // equivale a {:#}
Span   where_ = s.Value.Selection!.Span;              // posição no texto original

sealed class TableCollector : AstVisitor
{
    public List<ObjectName> Tables { get; } = new List<ObjectName>();
    public override ControlFlow PreVisitRelation(ObjectName relation)
    {
        Tables.Add(relation);
        return ControlFlow.Continue;
    }
}
```

**Consumidores em .NET Framework com C# 7.3** (padrão desse framework) não conseguem usar `init` nem `with`. Para eles:
- todos os nós terão construtores posicionais;
- a documentação recomendará `LangVersion` ≥ 9.

### 7.5 Estratégia de testes de compatibilidade

1. **Corpus extraído do Rust:**
   - uma ferramenta extrai dos `tests/*.rs` cada SQL, o conjunto de dialetos, o tipo de verificação (identidade, canônica, negativo, expressão) e as opções do parser;
   - o resultado fica versionado por tag do upstream.
2. **Oráculo Rust (somente desenvolvimento/CI):**
   - um binário Cargo em `tools/oracle` usa o sqlparser-rs **fixado na mesma revisão** de `UPSTREAM.json`, com a feature `serde`;
   - para cada caso e dialeto, gera o SQL canônico, o SQL pretty, a AST em JSON (com spans) e a mensagem de erro;
   - os goldens ficam no repositório (o tamanho e a compactação serão avaliados na Fase 0).
3. **Testes de conformidade em C#:**
   - comparam texto, **AST JSON** (pega divergências que o round-trip textual não pega), spans e mensagens de erro;
   - rodam em `net48`, `net8.0` e `net10.0`.
4. **Lista `xfail` + ratchet:** toda diferença conhecida tem entrada com motivo. A CI quebra em regressões e em `xfail` "curados" que não foram removidos.
5. **Testes portados à mão:** só o que não cabe no corpus (APIs de visitor, spans, opções, dialetos customizados).
6. **Fuzzing diferencial** (noturno): mutação das 197 sementes do upstream (`fuzz/fuzz_seeds`) e do corpus, comparando com o oráculo. Inclui teste de robustez: sem loops infinitos (bug #89 do SqlParser-cs) e sem estouro de pilha.

### 7.6 Acompanhamento do upstream

**`UPSTREAM.json`** registra o repositório, a tag e o commit-base. Também registra o último commit portado.

**`tools/UpstreamTracker`** faz o seguinte:
1. lista os commits do upstream desde o último portado (squash, com `#PR`);
2. classifica cada commit por área: tokenizer, parser, AST, `dialect:<x>`, testes, infraestrutura;
3. calcula o **diff da API da AST**: tipos, campos e variantes novos, removidos ou alterados;
4. detecta flags `supports_*` novas ou renomeadas e mudanças em keywords;
5. reextrai o corpus, roda os casos novos contra o build atual e lista os que falham;
6. gera um relatório Markdown/JSON e, opcionalmente, uma issue por PR do upstream (labels `upstream-port`, `dialect:x`, `ast-breaking`).

**Ledger de sincronização** (`docs/upstream-ledger.json`):
- cada PR do upstream tem um destes estados: `portado` (com link do PR C#), `não-aplicável` (com motivo) ou `pendente`;
- nenhum PR é pulado sem registro.

**Fluxo de trabalho:**
- um PR do upstream vira um PR no C#, com título `Port apache/datafusion-sqlparser-rs#NNNN: <título>`;
- a CI semanal roda o tracker e mantém uma issue "Status de sincronização".

**Fechamento de versão:**
- quando todos os PRs até a tag `vX.Y.Z` estiverem portados ou justificados e os goldens tiverem sido regenerados nessa tag, publica-se a versão correspondente.

### 7.7 Versionamento e estabilidade da API

**A tensão:** o upstream quebra a AST em quase toda release (versionamento 0.x), e o requisito pede uma "API pública estável".

**Proposta:**
- **Versão do pacote = versão-alvo do upstream** (`0.63.x`). O patch é nosso, e o upstream só publica `.0`.
- **Contrato de mudanças:** `Microsoft.CodeAnalysis.PublicApiAnalyzers` (arquivos `PublicAPI.*.txt`) + `EnablePackageValidation`. Toda mudança de API fica explícita no PR e no changelog.
- **Camada estável (opcional, mais tarde):** utilitários de análise acima da AST bruta, com SemVer próprio. Exemplos: tabelas e colunas referenciadas, funções, parâmetros. É útil se houver consumidores focados em análise de SELECT.

### 7.8 CI/CD e NuGet

- **CI (GitHub Actions, a confirmar):**
  - `windows-latest`: build + testes em `net48`, `net8.0` e `net10.0`;
  - `ubuntu-latest`: `net8.0`/`net10.0`;
  - verificações:
    - `TreatWarningsAsErrors` e formatação;
    - API pública;
    - validação de pacote;
    - cabeçalhos de licença;
    - ratchet de conformidade.
- **Jobs agendados:**
  - tracker do upstream (semanal);
  - regeneração de goldens quando `UPSTREAM.json` mudar (instala o toolchain Rust só nesse job);
  - fuzzing diferencial (noturno).
- **Empacotamento:**
  - SourceLink, build determinístico, `.snupkg`, `PackageLicenseExpression`, README e NOTICE no pacote;
  - assinatura de nome forte (recomendada para consumidores .NET Framework com nome forte).
- **Publicação:**
  - a partir de tag, via API key em secret, ou Trusted Publishing do nuget.org se disponível para a conta;
  - previews (`0.63.0-preview.N`) antes da versão estável.

### 7.9 Licenciamento e atribuição

Isto não é parecer jurídico; para uso corporativo, recomenda-se validação com o jurídico.

- **Licença do projeto:** recomenda-se **Apache-2.0**. Acompanha o upstream e simplifica a conformidade de uma obra derivada. Código MIT pode ser incorporado a ela.
- **Obrigações Apache-2.0** (§4):
  - incluir `LICENSE`;
  - reproduzir o conteúdo do `NOTICE.TXT` do sqlparser-rs no nosso `NOTICE`;
  - manter o aviso de copyright/atribuição ASF nos arquivos portados;
  - marcar de forma visível que foram alterados. Exemplo: "Portado de `src/ast/query.rs` (sqlparser-rs v0.63.0) para C#".
  - O corpus de testes extraído também é derivado e segue a mesma regra.
- **MIT (SqlParser-cs):** todo trecho copiado leva o aviso "Copyright (c) 2023 Tyler Brinks" + o texto MIT em `THIRD-PARTY-NOTICES.md`.
- **Marcas** (§6): o nome do pacote não deve sugerir endosso. Evitar "Apache" e "DataFusion" no nome. O ID `SqlParserCS` já está em uso no NuGet.

---

## 8. Plano incremental

As estimativas consideram **1 desenvolvedor dedicado**, sem contar aceleração por IA. São **grosseiras** e devem ser recalibradas ao fim da Fase 2, medindo o volume portado por semana. Volume a portar: ~72 mil linhas Rust (incluindo `Display` e spans) + ~55 mil linhas de testes, em boa parte absorvidas pelo corpus.

| Fase | Conteúdo | Critério de saída | Estimativa |
|---|---|---|---|
| **0 — Fundação** | ADRs (nome, licença, nomenclatura, mutabilidade, versionamento); solução e `Directory.Build.props`; testes multi-target (`net48`/`net8`/`net10`); CI básica; LICENSE/NOTICE/cabeçalhos; `UPSTREAM.json` fixado em `v0.63.0`; extrator de corpus; oráculo Rust e goldens; harness de conformidade com ratchet (tudo `xfail` no início) | CI verde; placar de conformidade publicado (linha de base 0%) | 1–2 semanas |
| **1 — Léxico e infraestrutura** | Tokenizer 1:1 (tokens, whitespace/comentários, `Location`/`Span`, unescape); keywords geradas; `Dialect` base com os 185 membros e padrões; `GenericDialect`/`AnsiDialect`; exceções com mensagens idênticas | 139 testes de tokenizer portados aprovados; fuzz sem travamentos | 2–3 semanas |
| **2 — AST e consultas (SELECT)** | Spike de geração das declarações da AST a partir do Rust (com decisão de go/no-go); AST de expressões/query/select/joins/tipos/valores com spans e `ToSql` (inclusive pretty); parser de expressões (Pratt), query, CTE, joins, set ops, funções e janelas; `DepthGuard` + memoização; gerador de Visitor/Rewriter | ≥ 95% dos casos SELECT/expressão de `sqlparser_common.rs` (Generic/Ansi) iguais ao oráculo, **inclusive AST JSON**; testes de visitor portados. **Preview `0.63.0-preview.1`** | 4–6 semanas |
| **3 — Statements** | DML (INSERT/UPDATE/DELETE/MERGE), DDL (CREATE/ALTER/DROP de tabelas, views, índices, funções, triggers…), DCL e TCL, SHOW/SET | `sqlparser_common.rs` 100% aprovado ou com `xfail` justificado | 4–6 semanas |
| **4 — Dialetos** | Um dialeto por vez: classe 1:1, caminhos específicos do parser, corpus e testes do arquivo correspondente. Ordem sugerida, a confirmar: PostgreSQL, MsSql, MySQL, SQLite, Oracle, Snowflake, BigQuery, Redshift, DuckDB, ClickHouse, Hive, Databricks, Spark, Teradata | Cada dialeto com 100% do corpus ou `xfail` justificado | 6–10 semanas |
| **5 — Complementos e robustez** (pode correr em paralelo à 4) | API de comentários; cobertura de `Spanned`; benchmarks (BenchmarkDotNet em `net48` e `net8`, usando o SqlParser-cs como referência); fuzzing diferencial; segurança de uso em threads | Sem regressão de desempenho relevante; fuzz noturno estável | 2–4 semanas |
| **6 — Sincronização e 1.0** | Alcançar o HEAD do upstream via tracker; ledger completo; automação semanal; `compatibility.md` gerado a partir do `xfail`; release estável | Ledger sem pendências até a tag-alvo; pacote estável publicado | 1–2 semanas + contínuo |

- **Total estimado:** ~5–7 meses até a paridade completa com a v0.63 e todos os dialetos. Um preview utilizável para análise de SELECT sai ao fim da Fase 2, ~2–2,5 meses.
- **Manutenção contínua:** ~1–3 semanas de porte por release do upstream (+2,7 a +7,9 mil linhas em `src/` por release, a cada 1–4 meses).

---

## 9. Riscos e mitigação

| Risco | Mitigação |
|---|---|
| Alvo móvel: o upstream muda ~5 mil linhas por trimestre | Baseline fixado por tag; tracker + ledger; port PR a PR |
| Idiomas Rust sem equivalente direto (macros, `maybe_parse` com closures, pattern matching sobre tokens) | Convenções em ADR; geração de código para keywords, visitor e JSON; spike de geração da AST na Fase 2 |
| Desempenho no .NET Framework (backtracking com exceções) | Benchmarks desde a Fase 2; memoização igual à do Rust; se necessário, caminho especulativo interno sem exceções |
| `StackOverflowException` derruba o processo em .NET | `DepthGuard` nos 6 pontos + `EnsureSufficientExecutionStack`; testes com entradas profundamente aninhadas |
| Divergências silenciosas | Oráculo com AST JSON; ratchet; fuzzing diferencial |
| Quebras de API herdadas do upstream | Política de versão espelhada; `PublicApiAnalyzers`; camada estável opcional |
| Consumidores com C# 7.3 | Construtores posicionais; documentação de `LangVersion`; evitar `required` na API pública |
| Tamanho dos goldens | Avaliar na Fase 0 (compactação, hash por dialeto quando a AST for idêntica) |
| Conformidade de licença e marca | Cabeçalhos, NOTICE, THIRD-PARTY-NOTICES, verificação na CI; nome neutro; revisão jurídica |
| Concentração de conhecimento | Processo documentado e automatizado (tracker, ledger, ADRs) |

---

## 10. Decisões pendentes

1. **Abordagem:** confirmar a opção C (implementação independente com reúso seletivo) em vez do fork.
2. **Nome do pacote/namespace e licença do projeto.** Sugestão: Apache-2.0 e um nome neutro.
3. **Hospedagem e CI:** GitHub Actions? NuGet.org público ou feed privado?
4. **Prioridade de dialetos e escopo inicial:** foco em SELECT primeiro? Quais dialetos são críticos para os consumidores?
5. **Política de versão/API:** espelhar a versão do upstream (`0.63.x`)? Haverá necessidade de uma camada estável?
6. **Mutabilidade da AST:** imutável + `AstRewriter` (recomendado) ou mutável como no Rust?
7. **Baseline:** tag `v0.63.0` (recomendado) e depois alcançar o HEAD.
8. **Toolchain Rust na CI** para o oráculo: aceitável? (É dependência de desenvolvimento, nunca de runtime.)

---

## Apêndice A — Reprodutibilidade dos experimentos

Os experimentos foram feitos em cópias no diretório temporário da sessão e **não fazem parte deste repositório**. A recomendação é recriá-los como ferramentas versionadas na Fase 0.

**Retarget para `netstandard2.0`:**
- `LangVersion latest` + PolySharp + System.Memory + Microsoft.Bcl.HashCode;
- DIMs de `IElement.Visit`, `IIfNotExists.IfNotExistsText` e `INegated.NegatedText` convertidos em extensões;
- 8 ajustes de API de BCL.

**Testes em `net48`:**
- projeto de testes retargetado para `net48`, com `Newtonsoft.Json` explícito;
- 5 ajustes: `Zip` com seletor, `Split(string[])`, `Enum.GetNames(Type)`, `Path.Combine` e a chamada estática do visitor.

**Extração do corpus:**
- script que reconhece os helpers de teste do Rust (`verified_stmt`, `one_statement_parses_to`, `verified_expr`, `parse_sql_statements` + `is_err`/`unwrap_err`…);
- resolve literais e variáveis `let`, e seletores de dialeto (`all_dialects()`, `pg()`, `pg_and_generic()`, `TestedDialects::new`…).

**Harness diferencial:**
- console .NET 8 que, para cada caso e dialeto, executa parse → `ToSql` e compara exatamente;
- timeout de 3 s por caso (detectou o travamento em `SELECT E'\'`, compatível com a issue #89).
