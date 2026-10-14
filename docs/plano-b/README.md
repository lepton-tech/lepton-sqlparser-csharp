# Plano B: SqlParser-cs adaptado para `netstandard2.0`

Rota alternativa documentada no [ADR-0001](../adr/0001-abordagem-port-independente-e-plano-b.md). Deve ser acionada **somente** se algum gatilho daquele ADR ocorrer:
- o port independente ficar lento demais;
- a qualidade ficar abaixo do esperado;
- houver necessidade urgente de um parser utilizável.

## O que é

O port C# já existente, [TylerBrinks/SqlParser-cs](https://github.com/TylerBrinks/SqlParser-cs) (MIT, alvo original `net8.0;net9.0;net10.0`), recompilado para `netstandard2.0` com um patch pequeno, **já validado em 2026-10-14**.

| Arquivo | Conteúdo |
|---|---|
| [`sqlparser-cs-netstandard20.patch`](sqlparser-cs-netstandard20.patch) | Biblioteca: retarget para `netstandard2.0`, `LangVersion` 14, PolySharp + System.Memory + Microsoft.Bcl.HashCode. Os 7 *default interface members* viram membros de extensão, mais 8 ajustes de API da BCL. 13 arquivos, cerca de +54/−35 linhas |
| [`sqlparser-cs-tests-net48.patch`](sqlparser-cs-tests-net48.patch) | Testes: retarget para `net48` e 5 ajustes de API |

## Como aplicar

```powershell
# A partir de um clone do SqlParser-cs no commit validado (e1929977a96290f937e9a2a67e3c823b7dc25cd2):
git clone https://github.com/TylerBrinks/SqlParser-cs.git sqlparser-cs-planob
git -C sqlparser-cs-planob checkout e1929977a96290f937e9a2a67e3c823b7dc25cd2
git -C sqlparser-cs-planob apply --ignore-whitespace <repo>/docs/plano-b/sqlparser-cs-netstandard20.patch <repo>/docs/plano-b/sqlparser-cs-tests-net48.patch
dotnet test sqlparser-cs-planob/src/SqlParser.Tests/SqlParser.Tests.csproj
```

**Resultado esperado** (verificado): **1.185 de 1.187 testes** passam no .NET Framework 4.8. As 2 falhas (`VisitorTests.Visitor_Finds_Properties` e `Visitor_Visits_Sql_Parts`) vêm do visitor baseado em reflection: no .NET Framework, a enumeração de propriedades inclui um indexador (`TargetParameterCountException`).

## Limitações conhecidas (o que se aceita ao escolher o Plano B)

Medidas na análise técnica (`../analise-tecnica-e-plano.md` §4–5):

- **Paridade de comportamento:** 54% de 3.054 casos SQL extraídos dos testes do Rust v0.63. São 319 casos com saída divergente **sem erro**, por exemplo `t1 ANTI JOIN t2` lido como alias de tabela `ANTI`.
- Sincronização com o Rust parcial até ~v0.60. Sem registro da versão de origem.
- Sem spans (posições no texto) na AST, sem API de comentários, sem pretty-print.
- Visitor por reflection, só leitura, com bugs: não desce em subconsultas no FROM e ignora `Break`.
- Loop infinito no tokenizer com literal `E'...'` não terminado (issue #89 do projeto; reproduzido com `SELECT E'\'`).
- Manutenção do projeto original praticamente parada. Correções seriam nossas.
- Licença: o código é MIT, mas traduzido de código Apache-2.0 sem atribuição. Ao adotar, incluir a atribuição Apache (ver ADR-0003).

## Se o Plano B for acionado

1. Criar um repositório/pasta própria para o fork, com os dois patches aplicados.
2. Corrigir primeiro: as 2 falhas do visitor (cache das propriedades + ignorar indexadores) e o loop do tokenizer (#89).
3. Registrar a decisão como novo ADR, referenciando o ADR-0001 e o gatilho que motivou a troca.
