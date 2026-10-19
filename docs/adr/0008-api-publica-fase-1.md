# ADR-0008: API pública, fase 1 (parse, erros e geração de SQL)

- **Status:** Aceita para a fase 1, com itens a confirmar pelo responsável (seção "A confirmar")
- **Data:** 2026-10-14

## Contexto

- O [ADR-0002](0002-arquitetura-em-camadas-e-api.md) separou o `Lepton.SqlParser.Core` (port fiel) da camada pública `Lepton.SqlParser` (código nosso, no estilo do SqlParser-cs). Até o [ADR-0011](0011-renome-lepton-sqlparser.md), os namespaces eram `SqlParser.Core` e `SqlParser`.
- O parser do Core compila e interpreta SQL de vários dialetos, com o pretty-print igual ao do Rust. O visitor (`ast/visitor.rs`) ainda está sendo portado.
- Esta fase cria o mínimo para uso real: ponto de entrada, erros e geração de SQL. Visitors e utilitários de análise ficam para a fase 2.
- Referência de uso: SqlParser-cs (`new SqlQueryParser().Parse(sql, dialect, options)`, `ToSql()`, `ParserException` com `Line`/`Column`).

## Decisões

### 1. Namespaces e reexposição do Core

- Tipos da camada pública no namespace `Lepton.SqlParser`: `SqlQueryParser`, `SqlParseException`, `SqlParseResult` e `SqlWritingExtensions`.
- A AST, os dialetos, `ParserOptions`, `ParserError`, `Location` e `Comments` são usados **diretamente do Core** (`Lepton.SqlParser.Core`, `Lepton.SqlParser.Core.Ast`, `Lepton.SqlParser.Core.Dialects`), como decidido no ADR-0002. Não há tipos-espelho nem wrappers.
- C# não tem reexportação de namespaces. O consumidor declara os `using` do Core (ou `global using` no próprio projeto).

### 2. Ponto de entrada: `SqlQueryParser`

```csharp
public sealed class SqlQueryParser
{
    public SqlQueryParser();
    public SqlQueryParser(int recursionLimit);
    public int? RecursionLimit { get; }

    public Sequence<Statement> Parse(string sql, Dialect? dialect = null, ParserOptions? options = null);
    public Sequence<Statement> Parse(string sql, string dialectName, ParserOptions? options = null);
    public bool TryParse(string sql, out Sequence<Statement> statements, [NotNullWhen(false)] out SqlParseException? error);
    public bool TryParse(string sql, Dialect? dialect, out Sequence<Statement> statements, [NotNullWhen(false)] out SqlParseException? error);
    public bool TryParse(string sql, Dialect? dialect, ParserOptions? options, out Sequence<Statement> statements, [NotNullWhen(false)] out SqlParseException? error);
    public SqlParseResult ParseWithComments(string sql, Dialect? dialect = null, ParserOptions? options = null);
}
```

- **Classe de instância, como no SqlParser-cs.** Sem estado entre chamadas: cada chamada cria um `Parser` do Core. A mesma instância pode ser usada por várias threads. `sealed`, porque não foi desenhada para herança.
- **Dialeto padrão:** `GenericDialect`, como no SqlParser-cs.
- **Opções:** `null` mantém as opções que o `Parser` do Core cria para o dialeto (vírgula final conforme `SupportsTrailingCommas`). Opções informadas substituem essas por inteiro, como `Parser::with_options` no Rust e como no SqlParser-cs.
- **Dialeto por nome:** `Parse(sql, "postgres")` usa `Dialect.DialectFromStr` (o `dialect_from_str` do Rust). Nome desconhecido lança `ArgumentException`; nome `null` lança `ArgumentNullException`.
  - **Custo conhecido:** a chamada `Parse(sql, null, options)` é ambígua (CS0121) entre as duas sobrecargas. A forma recomendada é o argumento nomeado `Parse(sql, options: options)`, registrada na documentação XML e no README.
  - Não há `TryParse` por nome, para não multiplicar sobrecargas. Use `Dialect.DialectFromStr(nome)`.
- **Limite de recursão:** parâmetro do construtor, porque no Rust ele não faz parte de `ParserOptions` (`Parser::with_recursion_limit`). `null` significa "padrão do Core" (50 na v0.63.0). A constante não é copiada, para que uma mudança do upstream chegue sozinha. Valor negativo lança `ArgumentOutOfRangeException`.
  - Um limite alto aceita árvores mais profundas, e tudo o que as percorre desce o mesmo tanto. A documentação XML do construtor avisa (revisão R4, item 2): o parse, o `ToSql` e os visitors continuam numa thread nova quando falta pilha (DIV-005 e DIV-009), mas a igualdade dos nós (DIV-010) e o `Span()` de consultas e junções aninhadas não têm essa proteção.

### 3. Tipo de retorno: `Sequence<Statement>` do Core

- É o tipo que o Core já devolve: sem cópia e sem conversão.
- Implementa `IReadOnlyList<Statement>`, então o consumidor pode tratá-lo como tal.
- Tem igualdade estrutural, útil para comparar ASTs.
  - **Limite (DIV-010, revisão R4, item 1):** a igualdade e o `GetHashCode` são os que o compilador gera para os records do Core, recursivos e sem `StackGuard`. Numa AST com milhares de níveis, `==`, `Equals` e `GetHashCode` estouram a pilha e derrubam o processo: `WHERE id = 0 OR id = 1 ...` com 8.000 termos estoura a thread principal no net8.0 (6.000 no net48; 1.000 numa thread de 256 KB). Os records não mudam, por serem port fiel do `derive(PartialEq)`.
  - **Uso seguro:** para ASTs de origem desconhecida, comparar o SQL gerado (`a.ToSql() == b.ToSql()`, protegido contra estouro pela DIV-005); se a igualdade estrutural for necessária, compará-las numa thread com pilha grande ou limitar antes o tamanho do SQL aceito; não usar nós profundos como chave de `Dictionary`/`HashSet`. O parse, o `ToSql`, os visitors e a análise não usam a igualdade dos nós.
- É `struct`: nunca é `null`. Em `TryParse` com falha, `statements` é a sequência vazia.
- Tem o mesmo nome do tipo do SqlParser-cs (lá, uma `List<T>` mutável).
- **Alternativa rejeitada:** `IReadOnlyList<Statement>`. Faria boxing da `struct`, perderia a igualdade estrutural e não isolaria nada, porque os elementos já são tipos do Core.

### 4. Erros: `SqlParseException`

```csharp
public sealed class SqlParseException : Exception
{
    public SqlParseException(ParserError error, Exception? innerException = null);
    public ParserError Error { get; }
    public Location? Location { get; }
    public long Line { get; }    // 0 se desconhecida
    public long Column { get; }  // 0 se desconhecida
}
```

- **Um único tipo** para erros de parser e de tokenizador. O Core já converte `TokenizerError` em `ParserError.TokenizerError` em `Parser.TryWithSql`, como o `impl From<TokenizerError> for ParserError` do Rust.
- **Mensagem idêntica à do Core** (`"sql parser error: ..."`), que por sua vez é idêntica à do Rust. `InnerException` é a `ParserException` do Core.
- **Posição:** o Rust não guarda a posição do erro em um campo; ela faz parte do texto, como sufixo `" at Line: L, Column: C"` (o `Display` de `Location`), acrescentado por último por `parser_err!` e por `TokenizerError`. A exceção extrai a posição desse sufixo, ancorado no fim da mensagem.
  - Esse formato é contrato: os testes do Rust comparam mensagens exatas.
  - Erros sem posição (fim do texto, `found: EOF`, limite de recursão) têm `Location == null` e `Line`/`Column` iguais a 0, como no SqlParser-cs.
  - Se o upstream passar a guardar a posição de forma estruturada, a extração por texto é trocada pelo campo.
- **Por que não reutilizar a `ParserException` do Core:** ela é `sealed`, não expõe a posição e segue os nomes do Rust. A camada pública é o lugar para amortecer mudanças do Core (ADR-0002).
- **Por que não se chama `ParserException`:** um consumidor com `using Lepton.SqlParser;` e `using Lepton.SqlParser.Core;` teria conflito de nomes (CS0104).
- **Falhas internas não são convertidas:** `InvalidOperationException` (bug ou trecho não portado do Core) e `ArgumentException` propagam como estão, também em `TryParse`.

### 5. `TryParse`

- Padrão Try do .NET: `bool` + `out` no fim. `error` tem `[NotNullWhen(false)]`, então o compilador sabe que ele não é `null` quando o retorno é `false`.
- `sql` nulo lança `ArgumentNullException`: é erro de programação, não falha de parse.

### 6. Geração de SQL: `ToSql`

```csharp
public static class SqlWritingExtensions
{
    public const string StatementSeparator = "; ";
    public const string PrettyStatementSeparator = ";\n";
    public static string ToSql(this IDisplay node, bool pretty = false);
    public static string ToSql(this IEnumerable<Statement> statements, bool pretty = false);
}
```

- **Sobre `IDisplay`:** vale para qualquer nó da AST (`Statement`, `Query`, `Expr`, `SelectItem`...) e para outros valores com `Display` no Core (`Token`, `Location`).
- `pretty: false` = `Formatter.Format` = `ToString()` (Rust `{}`); `pretty: true` = `Formatter.FormatAlternate` (Rust `{:#}`).
- Um método com parâmetro opcional (e não dois métodos), para a chamada `ToSql(pretty: true)` do esboço do plano (§7.4).
- **Quebras de linha do pretty:** sempre `"\n"`, como no Rust. Não usa `Environment.NewLine`, para a saída ser a mesma em qualquer sistema.
- **Coleção de instruções:** o Rust não tem `Display` para `Vec<Statement>`; o SqlParser-cs separa com `", "`, o que não gera SQL válido. Aqui: `"; "` no modo compacto e `";\n"` no pretty, sem `;` depois da última instrução. Um item `null` lança `ArgumentNullException` com `ParamName = "statements"` e a posição do item na mensagem (revisão R4, item 9).
- O nome da classe é o mesmo do SqlParser-cs (`SqlWritingExtensions`). Não há conflito com a extensão `Keyword.ToSql()` do Core, que é sobre um `enum`.

### 7. Comentários: `ParseWithComments`

- Equivale a `Parser::parse_sql_with_comments`, mas aceita dialeto e opções. A combinação é direta no Core: `new Parser(d).WithOptions(o).TryWithSql(sql)`, `ParseStatements()` e `IntoComments()`.
- Retorna `SqlParseResult` (classe com `Statements`, `Comments` e `Deconstruct`) em vez da tupla do Core. Um tipo nomeado é mais estável em API pública e ainda permite `var (statements, comments) = ...`.

### 8. Testes e licença

- Testes em `tests/Lepton.SqlParser.Tests` (net48, net8.0 e net10.0), com nomes em pt-BR, como os testes próprios do Core.
- `PrettyPrintTests.cs` usa casos de `tests/pretty_print.rs` (SQL e saída esperada). Por ser derivado, leva o cabeçalho ASF (ADR-0003). Não substitui o port fiel desse arquivo, que pertence ao `Lepton.SqlParser.Core.Tests`.
- `ReadmeExamplesTests.cs` verifica cada exemplo do `src/Lepton.SqlParser/README.md`.
- Nenhum código foi copiado do SqlParser-cs. Os nomes e o formato de uso (`SqlQueryParser`, `Parse(sql, dialect, options)`, `ToSql`, `SqlWritingExtensions`, `Line`/`Column`) são imitados como referência de desenho, o que está registrado em `THIRD-PARTY-NOTICES.md`.

## A confirmar

Decisões tomadas para esta fase, que o responsável deve confirmar ou trocar:

1. **Nome da exceção:** `SqlParseException`. Alternativa: `SqlParserException`. `ParserException` está descartado (conflito com o Core).
2. ~~**Pacote e assembly:** o SqlParser-cs também usa o assembly e o namespace `SqlParser`. As duas bibliotecas não convivem no mesmo projeto, o que impede uma migração lado a lado. Decidir na fase de empacotamento:~~
   - ~~o ID do pacote (não pode ser `SqlParserCS`, que já existe, nem usar "Apache" ou "DataFusion", conforme o ADR-0003);~~
   - ~~um pacote com as duas DLLs ou dois pacotes;~~
   - ~~se o assembly e o namespace raiz continuam `SqlParser`.~~
   **Decidido:** um pacote só, com as duas DLLs ([ADR-0010](0010-empacotamento.md)); pacote, assemblies e namespaces `Lepton.SqlParser` e `Lepton.SqlParser.Core` ([ADR-0011](0011-renome-lepton-sqlparser.md)), distintos dos do SqlParser-cs, que pode ser referenciado no mesmo projeto.
3. **`using` do Core para o consumidor:** manter `using` explícitos (atual) ou incluir no pacote um `build/*.props` com `<Using Include="Lepton.SqlParser.Core.Ast" />` etc., que funciona como `global using` em projetos SDK. Não há como reexportar namespaces em C#.
4. **Sobrecarga por nome de dialeto:** manter `Parse(sql, string dialectName)`, com a ambiguidade de `Parse(sql, null, options)`, ou trocar por um método de nome próprio (por exemplo, `ParseWithDialectName`).
5. **Separadores de `ToSql` em coleções de instruções:** `"; "` e `";\n"`. Alternativas: incluir `;` final, ou uma linha em branco entre instruções no pretty.
