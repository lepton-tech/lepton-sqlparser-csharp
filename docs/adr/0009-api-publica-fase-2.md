# ADR-0009: API pública, fase 2 (visitors, reescrita e utilitários de análise)

- **Status:** Aceita para a fase 2, com itens a confirmar pelo responsável (seção "A confirmar")
- **Data:** 2026-10-14

## Contexto

- O [ADR-0002](0002-arquitetura-em-camadas-e-api.md) prevê, na camada pública, "visitors no estilo SqlParser-cs" e "utilitários de análise". O [ADR-0008](0008-api-publica-fase-1.md) deixou os dois para esta fase.
- O Core já tem o visitor do Rust ([ADR-0007](0007-visitor.md)): `IVisit`/`IVisitMut<TSelf>`, as classes `Visitor<TBreak>`/`VisitorMut<TBreak>` (ganchos `virtual`; no mutável, por `ref`), `ControlFlow<TBreak>` e as funções `Visitor.VisitRelations`/`VisitExpressions`/`VisitStatements` e as versões `*Mut`. A travessia é gerada e conferida contra o Rust.
- O que o SqlParser-cs oferece (referência de desenho):
  - `abstract class Visitor` com `PreVisit*`/`PostVisit*` para `Default` (qualquer `IElement`), `Query`, `TableFactor`, `Relation`, `Expression` e `Statement`, que devolvem `enum ControlFlow { Continue, Break }`;
  - `IElement.Visit(visitor)` e `Sequence<T>.Visit(visitor)`, com travessia por reflexão em tempo de execução, ordenada por `[Visit(n)]`;
  - nenhum visitor de escrita e nenhum utilitário de análise. Há só conversões nos tipos da AST (`AsQuery()`, `AsSelect()`, `AsTable()`), que não reproduzimos: com os tipos do Core, `is`/`as` fazem o mesmo.

## Decisões

### 1. Adaptadores finos sobre o visitor do Core

- `SqlVisitor` e `SqlRewriter` **não percorrem nada**. Cada um guarda um adaptador privado (`Visitor<ValueTuple>` ou `VisitorMut<ValueTuple>`) que repassa cada gancho do Core para o gancho público correspondente. A ordem de visita é, por construção, a do Core, que é a do derive do Rust.
- Os atalhos com lambdas chamam as funções do Core (`Visitor.VisitRelations` etc.), e os utilitários de análise são construídos sobre `SqlVisitor` e esses atalhos.
- **Composição, e não herança:** se `SqlVisitor` herdasse de `Visitor<ValueTuple>`, a mesma classe exporia os dois conjuntos de ganchos (`PreVisitExpr` e `PreVisitExpression`), com resultados diferentes.
- Os testes comparam, em consultas complexas de 7 dialetos, a sequência completa de ganchos (inclusive identificadores e valores) do `SqlVisitor` com a do `Visitor<TBreak>`, e a do `SqlRewriter` com a do `VisitorMut<TBreak>`.

### 2. `SqlVisitor` e `ControlFlow`

```csharp
public enum ControlFlow { Continue = 0, Break = 1 }

public abstract class SqlVisitor
{
    protected SqlVisitor();
    public virtual ControlFlow PreVisitQuery(Query query);            // e PostVisitQuery
    public virtual ControlFlow PreVisitSelect(Select select);         // e PostVisitSelect
    public virtual ControlFlow PreVisitRelation(ObjectName relation); // e PostVisitRelation
    public virtual ControlFlow PreVisitTableFactor(TableFactor tableFactor);
    public virtual ControlFlow PreVisitExpression(Expr expression);
    public virtual ControlFlow PreVisitStatement(Statement statement);
    public virtual ControlFlow PreVisitValue(ValueWithSpan value);
    public virtual ControlFlow PreVisitIdentifier(Ident identifier);
    public virtual ControlFlow PreVisitOrderBy(OrderBy orderBy);
    public virtual ControlFlow PreVisitOrderByExpression(OrderByExpr orderByExpression);
    public virtual ControlFlow PreVisitGroupBy(GroupByExpr groupBy);
    // ... e os PostVisit* correspondentes
}
```

- **Nome `SqlVisitor`:** `Visitor` colidiria (CS0104) com a classe estática `Visitor` e com `Visitor<TBreak>` do Core, porque o consumidor quase sempre declara `using SqlParser.Core.Ast;`. O prefixo `Sql` segue `SqlQueryParser`.
- **`ControlFlow` sem valor**, como no SqlParser-cs. Não conflita com `ControlFlow<TBreak>` do Core: a aridade diferencia os dois nomes. Quem precisar de um valor no `Break` deriva direto de `Visitor<TBreak>`. Só `Break` interrompe; qualquer outro valor segue a visita.
- **Nomes dos ganchos:** o nome do SqlParser-cs quando existe (`PreVisitExpression`); nos demais, o do Core por extenso, como pedem as diretrizes do .NET (`PreVisitIdentifier`, `PreVisitOrderByExpression`). Os tipos dos parâmetros são os da AST do Core.
- **Sem `PreVisitDefault`:** o visitor do Rust só tem ganchos para 11 tipos e poda os campos que não alcançam nenhum deles (ADR-0007). Um gancho para "qualquer nó" exigiria percorrer a AST por conta própria, o que esta camada não faz.
- **`PreVisitRelation` vale para os 21 campos de relação do Rust** (abaixo), e não só para as tabelas do `FROM`, como no SqlParser-cs.
- **Sem estado entre visitas**, exceto o do próprio visitor derivado. O adaptador é criado uma vez por instância.

### 3. `SqlRewriter`: reescrita sem `ref`

```csharp
public abstract class SqlRewriter
{
    protected SqlRewriter();
    protected void Break();
    public virtual Expr PreVisitExpression(Expr expression);   // e os outros 21 ganchos, com o mesmo padrão
}
```

- O SqlParser-cs não tem visitor de escrita. O `VisitorMut` do Core recebe o nó por `ref`; aqui cada gancho **devolve o nó** que fica no lugar, como o `ExpressionVisitor` do .NET e o `CSharpSyntaxRewriter` do Roslyn.
- `PreVisit*` troca o nó antes de visitar os filhos (os filhos visitados são os do nó novo); `PostVisit*` troca depois, e o nó devolvido não é visitado de novo. É o comportamento do `VisitMut` do Rust.
- **Interrupção:** `protected void Break()`. O nó devolvido pelo gancho que chamou `Break()` ainda é usado, nenhum outro gancho roda e as trocas anteriores ficam no resultado, como no Rust. O pedido vale só para a reescrita em curso: `Rewrite` o limpa no início.
  - Alternativa rejeitada: devolver `(X, ControlFlow)` ou receber `out ControlFlow`, que pesariam em todos os ganchos por um recurso raro.
- **`null`** devolvido por um gancho ou por uma função de `Rewrite*` lança `InvalidOperationException`, com o nome do gancho. Sem isso, o erro só apareceria depois, no `ToSql()`.
- A AST continua imutável: a reescrita reconstrói só os pais do que mudou, e sem trocas devolve a mesma instância (ADR-0007, decisão 3).

### 4. Extensões: `Visit`, `Rewrite` e atalhos com lambdas

```csharp
public static class SqlVisitorExtensions
{
    public static ControlFlow Visit<T>(this T node, SqlVisitor visitor) where T : IVisit;
    public static T Rewrite<T>(this T node, SqlRewriter rewriter) where T : IVisitMut<T>;

    public static ControlFlow VisitRelations<T>(this T node, Func<ObjectName, ControlFlow> visit) where T : IVisit;
    public static void VisitRelations<T>(this T node, Action<ObjectName> visit) where T : IVisit;
    // VisitExpressions (Expr) e VisitStatements (Statement): as mesmas duas formas

    public static T RewriteRelations<T>(this T node, Func<ObjectName, ObjectName> rewrite) where T : IVisitMut<T>;
    public static T RewriteExpressions<T>(this T node, Func<Expr, Expr> rewrite) where T : IVisitMut<T>;
    public static T RewriteStatements<T>(this T node, Func<Statement, Statement> rewrite) where T : IVisitMut<T>;
}
```

- **Genéricos com restrição** (`where T : IVisit`), e não `this IVisit node`, para não fazer boxing da `Sequence<Statement>` (uma `struct`) devolvida pelo parser.
- `node.Visit(visitor)` não conflita com o `Visit<TBreak>(Visitor<TBreak>)` de instância do Core: com um `SqlVisitor` como argumento, o método de instância não se aplica e o compilador usa a extensão.
- **`Rewrite` exige `IVisitMut<T>` do próprio tipo:** o nó deve estar tipado pelo tipo-base (`Statement`, e não `Statement.Query`), porque um gancho pode trocar a variante. Para a sequência do parser, `T` é `Sequence<Statement>`.
- **Duas formas nos atalhos de leitura:** `Action<T>` para o caso comum (coletar) e `Func<T, ControlFlow>` para interromper. Não há ambiguidade: uma lambda que devolve `ControlFlow` prefere a `Func` (regra de conversão melhor do C#).
- **Ordem herdada do Rust:** os atalhos de leitura usam o gancho `pre` (pai antes dos filhos); os de reescrita usam o gancho `post` (a função recebe o nó com os filhos já reescritos). É o que fazem `visit_*` e `visit_*_mut` em `ast/visitor.rs`.
- Os atalhos de reescrita não oferecem `Break` (para isso, `SqlRewriter`).

### 5. Utilitários de análise: semântica exata

```csharp
public static class SqlAnalysisExtensions
{
    public static IReadOnlyList<ObjectName> GetReferencedTables<T>(this T node) where T : IVisit;
    public static IReadOnlyList<ObjectName> GetReferencedColumns<T>(this T node) where T : IVisit;
    public static IReadOnlyList<ObjectName> GetReferencedFunctions<T>(this T node) where T : IVisit;
    public static SqlStatementKind GetStatementKind(this Statement statement);
}
```

**Regras comuns:**
- A análise é **sintática**: olha só a AST, sem catálogo e sem dialeto.
- Os nomes saem como no texto (aspas e maiúsculas originais), sem repetição, na ordem da primeira ocorrência na travessia do visitor.
- Dois nomes são iguais quando têm as mesmas partes, com o mesmo texto e o mesmo estilo de aspas (igualdade de `ObjectName`). `t`, `T`, `"t"` e `dbo.t` são quatro nomes.
- O retorno é `IReadOnlyList<ObjectName>` (somente leitura), inclusive para colunas: `ObjectName` já representa um nome de várias partes e gera `t.a` como o `CompoundIdentifier`.

**`GetReferencedTables`:** as relações (gancho `PreVisitRelation`) menos as referências a CTEs, mais os alvos de `INTO`.

As relações são os 21 campos que o sqlparser-rs v0.63.0 marca com `visit(with = "visit_relation")`:

| Onde | Campo (Rust) |
|---|---|
| `FROM`, `JOIN`, alvos de `UPDATE`/`DELETE`/`MERGE` | `TableFactor::Table.name` (inclui funções de tabela com nome, `FROM f(1)`) |
| `INSERT INTO t` | `TableObject::TableName` |
| `CREATE TABLE t` (e `PARTITION OF p`) | `CreateTable.name`, `CreateTable.partition_of` |
| `CREATE VIEW v`, `ALTER VIEW v` | `CreateView.name`, `Statement::AlterView.name` |
| `CREATE VIRTUAL TABLE t` | `Statement::CreateVirtualTable.name` |
| `ALTER TABLE t` | `AlterTable.name` |
| `CREATE INDEX i ON t` | `CreateIndex.table_name` |
| `CREATE`/`ALTER`/`DROP POLICY p ON t` | `CreatePolicy.table_name`, `AlterPolicy.table_name`, `DropPolicy.table_name` |
| `TRUNCATE t`, `LOCK TABLE t` (PostgreSQL) | `TruncateTableTarget.name`, `LockTableTarget.name` |
| `SHOW ... FROM x`/`IN x`: uma tabela (`SHOW COLUMNS FROM t`) ou um banco/esquema (`SHOW TABLES FROM db`) | `ShowStatementIn.parent_name` |
| `DESCRIBE t`/`EXPLAIN t` | `Statement::ExplainTable.table_name` |
| `ANALYZE t`, `MSCK REPAIR TABLE t` | `Analyze.table_name`, `Msck.table_name` |
| `CACHE TABLE t`, `UNCACHE TABLE t` | `Statement::Cache.table_name`, `Statement::UNCache.table_name` |
| `LIST @stage`, `REMOVE @stage` (Snowflake) | `FileStagingCommand.stage` |

- Os nomes de `DROP TABLE` (`Statement::Drop.names`) **não** são relações no Rust e não aparecem.
- Entram também os alvos de DDL e DML (a tabela criada por `CREATE TABLE t2 AS SELECT ...`, o alvo de `INSERT`) e um alias usado como alvo (`UPDATE t ... FROM produtos AS t`, no SQL Server), porque a análise não resolve aliases.
- **Funções de tabela:** só a chamada direta no `FROM` (`FROM generate_series(1, 3)`, `CROSS APPLY dbo.fn(x)`) é relação no Rust (`TableFactor::Table` com `args`), e aparece aqui **e** em `GetReferencedFunctions`. `LATERAL fn(x)`/`LATERAL FLATTEN(...)` (`TableFactor::Function`) e `TABLE(fn(x))` (`TableFactor::TableFunction`) não são relações e só aparecem nas funções.

**Alvos de `INTO` (F1, revisão R4, item 4):** o nome que recebe as linhas em `SELECT ... INTO t` (SQL Server, PostgreSQL) e em `OUTPUT ... INTO t` (SQL Server) entra como tabela. O Rust guarda esses alvos como expressões (`SelectInto.targets: Vec<Expr>`, campo sem `visit_relation`), e antes da F1 eles saíam em `GetReferencedColumns` e sumiam das tabelas.
- Entram os alvos que são identificadores (`#tmp`, `@ids`) ou identificadores compostos (`dbo.novo`), na ordem da travessia (o `INTO` vem depois da lista do `SELECT` e antes do `FROM`).
- O alvo nunca é tratado como CTE: ele cria ou recebe linhas.
- Como a análise é sintática, uma variável usada como alvo (`SELECT a INTO @x` do MySQL, o `INTO` do PL/pgSQL) também aparece como tabela.
- Implementação: ao visitar um `Select` ou uma instrução de DML com `OUTPUT ... INTO`, os alvos são registrados por referência; quando o gancho de expressão chega a eles, viram tabela (e são pulados nas colunas).

**CTEs não são tabelas:** uma relação de uma parte só cujo nome é o de uma CTE visível no ponto da referência é descartada.
- No corpo da consulta (e em `ORDER BY`, `LIMIT` etc.) são visíveis todas as CTEs do seu `WITH` e dos `WITH` das consultas que a contêm.
- Dentro da definição de uma CTE são visíveis as CTEs anteriores do mesmo `WITH`.
- **Autorreferência** (decidida na F1, revisão R4, item 5): o próprio nome, dentro da definição, só é a CTE quando ela pode ser recursiva: o `WITH` tem `RECURSIVE` (PostgreSQL, MySQL, SQLite...) ou a definição é uma operação de conjunto (`UNION`, `UNION ALL`, `EXCEPT`, `INTERSECT`, também entre parênteses), a forma de CTE recursiva sem a palavra do SQL Server e do Oracle. Fora desses casos, o nome é a tabela de verdade: `WITH orders AS (SELECT * FROM orders WHERE x = 1) SELECT * FROM orders` devolve `orders`. Antes da F1 a autorreferência contava sempre como CTE, e a lista ficava vazia, o que deixaria passar a tabela num controle de acesso.
- Uma CTE posterior do mesmo `WITH` não é visível: `WITH a AS (SELECT * FROM b), b AS (...)` devolve `b` como tabela (inclusive com `RECURSIVE`, em que o PostgreSQL aceitaria a referência adiante; o erro, aqui, é para o lado seguro).
- Fora do escopo (outra instrução, uma tabela derivada vizinha), o mesmo nome volta a ser tabela.
- Nomes qualificados (`s.cte`) nunca são CTE.
- O escopo depende do nó analisado: uma subconsulta analisada sozinha não vê o `WITH` de fora.

**Comparação do nome da CTE com a referência** (documentada na F1, revisão R4, item 10): se nenhum dos dois lados tem aspas, compara sem diferenciar maiúsculas; se algum lado tem aspas (de qualquer estilo: `"c"`, `[c]`, `` `c` ``), compara o texto sem as aspas diferenciando maiúsculas. A análise não conhece o dialeto, e cada banco tem uma regra; esta não segue nenhum deles exatamente:

| Consulta | Aqui | No banco (em negrito, onde difere) |
|---|---|---|
| `WITH c ... FROM C` | CTE | CTE no PostgreSQL, Oracle, Snowflake e SQL Server com ordenação sem diferenciar maiúsculas; **tabela** no SQL Server com ordenação que diferencia |
| `WITH "c" ... FROM C` | tabela | **CTE** no PostgreSQL (`C` vira `c`); tabela no Oracle e no Snowflake |
| `WITH "C" ... FROM c` | tabela | tabela no PostgreSQL; **CTE** no Oracle e no Snowflake (`c` vira `C`) |
| `WITH "c" ... FROM c` e `WITH c ... FROM "c"` | CTE | CTE no PostgreSQL; **tabela** no Oracle e no Snowflake |
| `WITH "C" ... FROM C` e `WITH C ... FROM "C"` | CTE | **tabela** no PostgreSQL; CTE no Oracle e no Snowflake |
| `WITH [C] ... FROM c` | tabela | **CTE** no SQL Server com ordenação sem diferenciar maiúsculas |

- Quando "aqui" diz tabela e o banco diz CTE, a lista tem um nome a mais (falso positivo; para controle de acesso, o lado seguro).
- Quando "aqui" diz CTE e o banco diz tabela (linhas 1, 4 e 5, nos bancos em negrito), a tabela de verdade some da lista. Só acontece quando o nome da CTE e o da tabela diferem apenas por maiúsculas ou por aspas, um caso raro; quem usa a lista para controle de acesso nesses bancos deve recusar ou normalizar esses nomes antes.
- Os testes `CTEs_nao_sao_tabelas` fixam a regra, inclusive com aspas de um lado só.

**`GetReferencedColumns`:** cada `Expr.Identifier` (`a`) e `Expr.CompoundIdentifier` (`t.a`, `s.t.a`) entregue pelo gancho de expressão.
- `a` e `t.a` são entradas diferentes: a análise não resolve a tabela de cada coluna.
- Todo identificador usado como expressão entra, mesmo quando não é coluna: um alias usado no `ORDER BY`, um parâmetro de lambda, uma variável `@x` do SQL Server, a unidade de `DATEADD(day, ...)`, uma dica `WITH (NOLOCK)` do SQL Server.
- Não entram nomes que a AST não guarda como expressão: `*` e `t.*`, a lista de colunas de `INSERT INTO t (a, b)`, o alvo de `UPDATE ... SET a = 1` (o lado direito entra), as colunas de `CREATE TABLE` e os aliases (`AS x`).
- Não entram os alvos de `SELECT ... INTO` e `OUTPUT ... INTO`, que são tabelas (F1).

**`GetReferencedFunctions`:** o nome de cada `Expr.Function` (escalar, de agregação, de janela, definida pelo usuário) e de cada função de tabela no `FROM`, como escrito: `COUNT` e `count` são entradas diferentes.
- **Funções de tabela** (F1, revisão R4, item 6), nas três formas da AST: `LATERAL fn(x)` e `LATERAL FLATTEN(...)` (`TableFactor.Function`, nome pelo gancho de `TableFactor`); `TABLE(fn(x))` (`TableFactor.TableFunction`, cuja expressão já é uma `Expr.Function`); e a chamada direta no `FROM` (`TableFactor.Table` com `Args`), que também é relação. Na mesma ordem da travessia: o nome da função de tabela vem antes das funções dos seus argumentos.
- No SQL Server, a dica antiga sem `WITH` (`FROM t (NOLOCK)`) é guardada pelo Rust como argumentos de função de tabela, então `t` também aparece nas funções. Com `WITH (NOLOCK)`, não.
- Não entram as construções que o parser representa com nós próprios (`CAST`, `EXTRACT`, `SUBSTRING`, `TRIM`, `POSITION`, `UNNEST`, `JSON_TABLE`, `OPENJSON`, `XMLTABLE`...) nem os procedimentos de `CALL`.

### 6. Tipo da instrução: `SqlStatementKind`

- Classificação grossa pelo comando que inicia a instrução: `Query`, `Insert`, `Update`, `Delete`, `Merge`, `Create`, `Alter`, `Drop`, `Truncate`, `Grant`, `Revoke`, `Deny`, `Transaction`, `Block` e `Other` (valor padrão, `0`).
- `Create`, `Alter` e `Drop` valem para toda variante cujo **nome começa** com `Create`, `Alter` ou `Drop`. Os nomes das variantes são os do Rust (convencoes.md §6), então variantes novas do upstream entram sozinhas. Os demais membros correspondem a variantes fixas.
- `Transaction` = `StartTransaction` sem bloco (`START TRANSACTION`, `BEGIN`, `BEGIN TRANSACTION`), `Commit` (`COMMIT`, `END`), `Rollback`, `Savepoint` e `ReleaseSavepoint`.
- **`Block`** (F1, revisão R4, item 3) = bloco `BEGIN ... END` do SQL Server, do BigQuery e do Snowflake, inclusive com `EXCEPTION WHEN ...`. O Rust guarda o bloco na mesma variante do `BEGIN [TRANSACTION]` (`Statement::StartTransaction`, montada por `parse_begin_exception_end` com `has_end_keyword: true` e as instruções em `statements`), mas o bloco não abre transação. O critério é `HasEndKeyword`: `BEGIN` seguido de `END` em instruções separadas (PostgreSQL) continua `Transaction` + `Transaction` (`StartTransaction` e `Commit`). `Block` foi acrescentado no fim do enum, para não mudar os valores dos membros existentes.
- Uma `Statement.Query` cujo corpo é `INSERT`/`UPDATE`/`DELETE`/`MERGE` (o `WITH ... INSERT` do PostgreSQL) é classificada pela instrução interna. `SELECT ... INTO` continua `Query`, como no Rust.
- **Alternativas rejeitadas:**
  - um enum com um membro por variante (cerca de 150), que duplicaria a hierarquia de tipos e mudaria a cada versão do upstream; para isso já existe `statement is Statement.X`;
  - uma `string` com o nome da variante, que é só `statement.GetType().Name`.

### 7. Testes e licença

- Testes em `tests/SqlParser.Tests` (net48, net8.0 e net10.0):
  - `SqlVisitorTests`: ordem igual à do Core em 8 instruções de 7 dialetos; `Break`; subárvores; atalhos de leitura comparados com as funções do Core;
  - `SqlRewriterTests`: ordem e trocas iguais às do `VisitorMut`; renomear tabela e trocar literais com ida e volta pelo `ToSql()` e por um novo parse; `Break`; troca da raiz; `null`; atalhos de reescrita;
  - `SqlAnalysisTests`: tabelas (inclusive as regras de CTE e os alvos de `INTO`), colunas, funções (inclusive as de tabela) e o tipo de 41 instruções, em vários dialetos;
  - `ArvoresProfundasTests` (F1): `ToSql` de consultas aninhadas e de cadeias de `UNION`, comparação segura de ASTs profundas e ganchos do visitor em outra thread, numa thread de 256 KB;
  - `ReadmeExamplesTests`: exemplos 8 a 11 do README.
  - Os visitors de registro ficam em `Recorders.cs`.
- Nenhum código foi copiado do SqlParser-cs. São imitados como referência de desenho: o enum `ControlFlow` (`Continue`, `Break`), os nomes `PreVisit*`/`PostVisit*` e `PreVisitExpression`, e o uso `node.Visit(visitor)`. Registrado em `THIRD-PARTY-NOTICES.md`.

## A confirmar

Decisões tomadas para esta fase, que o responsável deve confirmar ou trocar:

1. **Nomes das classes:** `SqlVisitor` e `SqlRewriter`. Alternativas: `SqlAstVisitor`/`SqlAstRewriter`, ou `SqlVisitorMut` (mais perto do Rust).
2. **`ControlFlow` público com o mesmo nome do tipo do Core** (aridades diferentes, sem conflito de compilação). Alternativa: outro nome (`VisitAction`), que se afasta do SqlParser-cs.
3. **Nomes dos ganchos** no estilo .NET (`PreVisitExpression`, `PreVisitIdentifier`, `PreVisitOrderByExpression`), em vez dos do Core (`PreVisitExpr`, `PreVisitIdent`, `PreVisitOrderByExpr`).
4. **Regras de CTE em `GetReferencedTables`:**
   - ~~autorreferência sempre como CTE (certo no SQL Server e no Oracle; no PostgreSQL sem `RECURSIVE`, o nome se referiria a uma tabela)~~ **Decidido na F1 (revisão R4, item 5):** a autorreferência só é CTE com `RECURSIVE` ou com a definição numa operação de conjunto; senão, é a tabela (decisão 5);
   - comparação de maiúsculas (sem aspas: ignora; com aspas: diferencia): mantida e **documentada na F1** (revisão R4, item 10), com a tabela de diferenças por banco (decisão 5);
   - um alvo de `INSERT`/`UPDATE`/`DELETE` com o mesmo nome de uma CTE do mesmo `WITH` é tratado como CTE (colisão rara; o banco usaria a tabela). O alvo de `SELECT ... INTO`/`OUTPUT ... INTO` nunca é CTE (F1).
5. **Colunas de DML:** incluir em `GetReferencedColumns` a lista de colunas de `INSERT` e os alvos de `UPDATE ... SET` (hoje só entram expressões).
6. **`DROP TABLE`:** incluir os nomes de `Statement.Drop` em `GetReferencedTables`, embora o Rust não os marque como relação.
7. **Membros de `SqlStatementKind`:** acrescentar `Show`, `Explain`, `Set`, `Use` ou `Call`? Classificar `RENAME TABLE` como `Alter`? Separar `CREATE USER`/`ROLE` de `Create`? (`Block` entrou na F1, para os blocos `BEGIN ... END`.)
8. **Tipo das colunas:** `ObjectName` (atual) ou um tipo próprio (por exemplo, `ColumnReference` com `Qualifier` e `Name`).
9. **Interrupção no `SqlRewriter`** por `protected void Break()` (atual) ou por um gancho que devolva também um `ControlFlow`.

## Impacto da Rodada 1 de sincronização com o upstream (2026-10-15)

A Rodada 1 levou o Core de `v0.63.0` a `dacdbc9` (63 PRs; `docs/port/upstream-ledger.md`). A camada pública **não precisou de mudança de código**: nenhum tipo de `src/SqlParser` constrói os nós alterados, e os 220 testes de `tests/SqlParser.Tests` passam sem alteração nos três alvos. Mas a camada expõe a AST, os dialetos e o `Parser` do Core diretamente (ADR-0008, decisão 1), então as quebras do Core chegam ao consumidor. A próxima versão do pacote deve ser tratada como **quebra de compatibilidade**.

**Assinaturas públicas do Core que mudaram (quebra de código-fonte):**

| Antes | Depois | PR |
|---|---|---|
| `Insert(.., Sequence<ObjectName> Columns, bool Overwrite, ..)` (26 parâmetros) | `Insert(.., Sequence<ObjectName> Columns, bool ByName, bool Overwrite, ..)` (27): construtor e `Deconstruct` | #2403 |
| `Table(string? TableName, string? SchemaName)` | `Table(Ident? TableName, Ident? SchemaName)` | #2540 |
| `Expr.InList(Expr Expr, Sequence<Expr> List, bool Negated)` | `Expr.InList(Expr Expr, Sequence<Expr> List, bool Negated, bool Global)` | #2598 |
| `Expr.InSubquery(Expr Expr, Query Subquery, bool Negated)` | `Expr.InSubquery(Expr Expr, Query Subquery, bool Negated, bool Global)` | #2598 |
| `Parser.ParseIn(Expr expr, bool negated)` | `Parser.ParseIn(Expr expr, bool negated, bool global)` | #2598 |

**Quebra binária sem mudança de código-fonte** (quem compilou contra o Core precisa recompilar): o `enum` `Keyword` foi renumerado (`APPROXIMATE`, #2480, e `PROC`, #2448), e `MapBracketKind.AngleBrackets` passou de 1 a 2 com o membro novo `ParenthesesNotNull` (#2489).

**Acréscimos** (não quebram quem só consome; quebram quem faz `switch` exaustivo sobre as hierarquias): `CreateForeignTable` e `Statement.CreateForeignTable` (#2515), `DataType.Collate` (#2553), `DataType.Object` (#2489), `ArrayElemTypeDef.ParenthesisNotNull` (#2488), `AlterTableOperation.ModifyOrderBy` (#2569), `RepetitionQuantifier.Reluctant` (#2510), `Parser.ParseCreateForeignTable()` e 17 propriedades `virtual` novas no `Dialect` (`SupportsGlobalIn`, `SupportsTupleElementAccess`, `SupportsStages`...).

**Comportamento visível pela camada pública:**
- `ToSql()` gera outro texto em vários casos (operadores prefixos com espaço, aspas escapadas em literais, `AFTER` com aspas, `CAST(x AS)`, `TABLE "t"` com as aspas preservadas, `GLOBAL IN`).
- A mesma entrada pode dar outra AST: `S*?` no `MATCH_RECOGNIZE`, `IN ((SELECT 1), 2)` (agora `InList`), `IN expr` sem parênteses no ClickHouse, `OBJECT(..)`, `TRIM(BOTH FROM ..)`.
- `SqlVisitor`/`SqlRewriter` passam a receber `PreVisitIdentifier`/`PostVisitIdentifier` para os nomes do comando `TABLE t`, porque `Table` agora guarda `Ident`, como no Rust. `GetReferencedTables` não muda: esses nomes não são relações no Rust.
- `GetStatementKind` classifica `CREATE FOREIGN TABLE` como `Create` sem código novo (decisão 6).

## Correções da revisão R4 (F1, 2026-10-15)

A revisão R4 (`docs/port/onda-d/R4.md`) testou a API pública e o pacote de contexto limpo. A unidade F1 mudou o comportamento de `SqlParser` nestes pontos (todos registrados no `CHANGELOG.md`, em "Não lançado"):

| Item da R4 | Antes | Depois |
|---|---|---|
| 3 | `BEGIN ... END` (bloco) dava `Transaction` | `SqlStatementKind.Block` (membro novo, no fim do enum) |
| 4 | O alvo de `SELECT ... INTO t` saía nas colunas e sumia das tabelas | É tabela e não é coluna; o mesmo vale para `OUTPUT ... INTO t` |
| 5 | A autorreferência numa CTE sem `RECURSIVE` contava como CTE (a tabela real sumia) | Só conta com `RECURSIVE` ou com a definição numa operação de conjunto |
| 6 | `LATERAL fn(x)`/`LATERAL FLATTEN(...)` não apareciam em nenhuma lista; a documentação dizia que funções de tabela eram relações | Todas as funções de tabela aparecem em `GetReferencedFunctions`; `GetReferencedTables` não mudou (só a chamada direta no `FROM` é relação) |
| 9 | Item `null` em `ToSql(IEnumerable<Statement>)` lançava `ArgumentNullException` com `ParamName = "node"` | `ParamName = "statements"`, com a posição do item na mensagem |

Também na F1: a documentação de `SqlVisitor`, `SqlRewriter` e `SqlVisitorExtensions` avisa que os ganchos podem rodar em outra thread (DIV-009, item 8); a de `SqlQueryParser(int recursionLimit)` avisa o que um limite alto implica (item 2); a regra de comparação de nomes de CTE ganhou a tabela da decisão 5 (item 10). O `ToSql` de consultas aninhadas, de junções entre parênteses e de cadeias de `UNION` deixou de estourar a pilha, por desvios no Core registrados na DIV-005; a igualdade dos records, que continua recursiva, ficou registrada como DIV-010 ([ADR-0008](0008-api-publica-fase-1.md), decisão 3).
