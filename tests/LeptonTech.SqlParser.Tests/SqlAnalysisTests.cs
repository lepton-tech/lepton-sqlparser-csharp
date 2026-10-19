// Testes próprios da API pública, fase 2: utilitários de análise (GetReferencedTables, GetReferencedColumns,
// GetReferencedFunctions, GetStatementKind), em vários dialetos. A semântica testada é a documentada em
// SqlAnalysisExtensions e no ADR-0009.

using LeptonTech.SqlParser.Core.Ast;

namespace LeptonTech.SqlParser.Tests;

public class SqlAnalysisTests
{
    private static readonly SqlQueryParser Parser = new SqlQueryParser();

    private static string[] Names(IReadOnlyList<ObjectName> names) => names.Select(n => n.ToSql()).ToArray();

    private static string[] Tables(string sql, string dialect = "generic") => Names(Parser.Parse(sql, dialect).GetReferencedTables());

    private static string[] Columns(string sql, string dialect = "generic") => Names(Parser.Parse(sql, dialect).GetReferencedColumns());

    private static string[] Functions(string sql, string dialect = "generic") => Names(Parser.Parse(sql, dialect).GetReferencedFunctions());

    // ---------------------------------------------------------------------------------------------
    // GetReferencedTables

    [Theory]
    [InlineData("generic", "SELECT * FROM a JOIN b ON a.id = b.id JOIN a AS a2 ON a2.id = b.id", new[] { "a", "b" })]
    [InlineData("generic", "SELECT * FROM t WHERE x IN (SELECT y FROM u) AND EXISTS (SELECT 1 FROM v)", new[] { "t", "u", "v" })]
    [InlineData("generic", "SELECT (SELECT MAX(b) FROM u) FROM (SELECT * FROM t) AS d", new[] { "u", "t" })]
    [InlineData("generic", "SELECT * FROM t; SELECT * FROM u; SELECT * FROM t", new[] { "t", "u" })]
    [InlineData("generic", "SELECT 1", new string[0])]
    [InlineData("mssql", "SELECT * FROM [dbo].[clientes] AS c JOIN dbo.pedidos AS p ON c.id = p.cliente_id", new[] { "[dbo].[clientes]", "dbo.pedidos" })]
    [InlineData("mysql", "SELECT * FROM `loja`.`produtos` WHERE preco > 10", new[] { "`loja`.`produtos`" })]
    [InlineData("postgres", "SELECT * FROM public.t, \"Outra\" AS o", new[] { "public.t", "\"Outra\"" })]
    [InlineData("snowflake", "SELECT * FROM db.s.t1 NATURAL JOIN db.s.t2", new[] { "db.s.t1", "db.s.t2" })]
    [InlineData("generic", "SELECT * FROM T JOIN t ON T.a = t.a", new[] { "T", "t" })]
    public void Tabelas_de_consultas(string dialect, string sql, string[] expected)
    {
        Assert.Equal(expected, Tables(sql, dialect));
    }

    [Theory]
    [InlineData("generic", "INSERT INTO t1 (a) SELECT a FROM t2", new[] { "t1", "t2" })]
    [InlineData("generic", "UPDATE t SET a = (SELECT MAX(b) FROM u)", new[] { "t", "u" })]
    [InlineData("generic", "DELETE FROM t WHERE id IN (SELECT id FROM u)", new[] { "t", "u" })]
    [InlineData("generic", "MERGE INTO target USING source ON target.id = source.id WHEN MATCHED THEN DELETE", new[] { "target", "source" })]
    [InlineData("mssql", "UPDATE t SET t.preco = s.preco FROM produtos AS t JOIN precos AS s ON t.id = s.id", new[] { "t", "produtos", "precos" })]
    [InlineData("generic", "CREATE TABLE t2 AS SELECT * FROM t1", new[] { "t2", "t1" })]
    [InlineData("generic", "CREATE VIEW v AS SELECT * FROM t", new[] { "v", "t" })]
    [InlineData("generic", "CREATE INDEX i ON t (a)", new[] { "t" })]
    [InlineData("generic", "ALTER TABLE t ADD COLUMN c INT", new[] { "t" })]
    [InlineData("generic", "TRUNCATE TABLE t", new[] { "t" })]
    [InlineData("generic", "DROP TABLE t", new string[0])]
    [InlineData("postgres", "SELECT * FROM generate_series(1, 3) AS g", new[] { "generate_series" })]
    [InlineData("postgres", "LOCK TABLE t IN ACCESS EXCLUSIVE MODE", new[] { "t" })]
    [InlineData("mysql", "SHOW COLUMNS FROM t", new[] { "t" })]
    [InlineData("mysql", "SHOW TABLES FROM db", new[] { "db" })]
    // Funções de tabela: a chamada direta no FROM é relação no Rust; LATERAL fn(..) e TABLE(fn(..)) não são.
    [InlineData("mssql", "SELECT * FROM t CROSS APPLY dbo.fn(t.id) AS f", new[] { "t", "dbo.fn" })]
    [InlineData("postgres", "SELECT * FROM t1 LEFT JOIN LATERAL fn(t1.x) AS f ON true", new[] { "t1" })]
    [InlineData("snowflake", "SELECT * FROM TABLE(udtf(1)) AS x", new string[0])]
    public void Tabelas_de_DML_e_DDL(string dialect, string sql, string[] expected)
    {
        Assert.Equal(expected, Tables(sql, dialect));
    }

    [Theory]
    // R4, item 4: o alvo de SELECT ... INTO é tabela (antes saía como coluna e sumia das tabelas).
    [InlineData("mssql", "SELECT * INTO #tmp FROM t", new[] { "#tmp", "t" }, new string[0])]
    [InlineData("mssql", "SELECT a, b INTO dbo.novo FROM t WHERE c = 1", new[] { "dbo.novo", "t" }, new[] { "a", "b", "c" })]
    [InlineData("postgres", "SELECT * INTO TEMPORARY TABLE novo FROM t", new[] { "novo", "t" }, new string[0])]
    // O mesmo texto como coluna em outro ponto continua coluna.
    [InlineData("mssql", "SELECT novo.a, novo INTO novo FROM velho AS novo", new[] { "novo", "velho" }, new[] { "novo.a", "novo" })]
    // OUTPUT ... INTO do SQL Server guarda o alvo no mesmo nó (SelectInto).
    [InlineData("mssql", "INSERT INTO t (a) OUTPUT inserted.a INTO @ids VALUES (1)", new[] { "t", "@ids" }, new[] { "inserted.a" })]
    [InlineData("mssql", "DELETE FROM t OUTPUT deleted.a INTO arquivo WHERE a = 1", new[] { "t", "arquivo" }, new[] { "a", "deleted.a" })]
    // Sintático: uma variável como alvo (MySQL) também conta como tabela.
    [InlineData("mysql", "SELECT a INTO @x FROM t", new[] { "@x", "t" }, new[] { "a" })]
    // O alvo de INTO nunca é CTE.
    [InlineData("mssql", "WITH c AS (SELECT 1 AS x) SELECT x INTO c FROM c", new[] { "c" }, new[] { "x" })]
    public void Alvos_de_INTO_sao_tabelas_e_nao_colunas(string dialect, string sql, string[] tables, string[] columns)
    {
        Assert.Equal(tables, Tables(sql, dialect));
        Assert.Equal(columns, Columns(sql, dialect));
    }

    [Theory]
    // CTE usada no corpo não é tabela.
    [InlineData("generic", "WITH c AS (SELECT * FROM t) SELECT * FROM c JOIN u ON c.id = u.id", new[] { "t", "u" })]
    // Uma CTE vê as anteriores do mesmo WITH...
    [InlineData("generic", "WITH a AS (SELECT * FROM t), b AS (SELECT * FROM a) SELECT * FROM b", new[] { "t" })]
    // ...mas não as posteriores: dentro da definição de a, "b" é tabela.
    [InlineData("generic", "WITH a AS (SELECT * FROM b), b AS (SELECT * FROM t) SELECT * FROM a", new[] { "b", "t" })]
    // Autorreferência: CTE recursiva, com RECURSIVE...
    [InlineData("generic", "WITH RECURSIVE r AS (SELECT 1 AS n UNION ALL SELECT n + 1 FROM r WHERE n < 10) SELECT * FROM r", new string[0])]
    [InlineData("postgres", "WITH RECURSIVE r AS (SELECT * FROM r) SELECT * FROM r", new string[0])]
    // ...ou sem a palavra, com a definição numa operação de conjunto (SQL Server, Oracle), com ou sem parênteses.
    [InlineData("mssql", "WITH r AS (SELECT id FROM emp WHERE chefe IS NULL UNION ALL SELECT e.id FROM emp AS e JOIN r ON e.chefe = r.id) SELECT * FROM r", new[] { "emp" })]
    [InlineData("mssql", "WITH r AS ((SELECT id FROM emp UNION ALL SELECT e.id FROM emp AS e JOIN r ON e.chefe = r.id)) SELECT * FROM r", new[] { "emp" })]
    // Sem RECURSIVE e sem operação de conjunto, o próprio nome dentro da definição é a tabela de verdade (R4, item 5).
    [InlineData("postgres", "WITH orders AS (SELECT * FROM orders WHERE x = 1) SELECT * FROM orders", new[] { "orders" })]
    [InlineData("mssql", "WITH c AS (SELECT * FROM c JOIN u ON c.id = u.id) SELECT * FROM c", new[] { "c", "u" })]
    // A CTE anterior continua visível na definição de uma CTE não recursiva.
    [InlineData("generic", "WITH a AS (SELECT * FROM t), b AS (SELECT * FROM a JOIN b ON a.id = b.id) SELECT * FROM b", new[] { "t", "b" })]
    // Fora do escopo do WITH, o mesmo nome é tabela.
    [InlineData("generic", "SELECT * FROM (WITH c AS (SELECT 1 AS x) SELECT * FROM c) AS d JOIN c ON 1 = 1", new[] { "c" })]
    [InlineData("generic", "WITH c AS (SELECT 1) SELECT * FROM c; SELECT * FROM c", new[] { "c" })]
    // O WITH externo vale dentro das subconsultas.
    [InlineData("generic", "WITH c AS (SELECT * FROM t) SELECT * FROM u WHERE id IN (SELECT id FROM c)", new[] { "t", "u" })]
    // Sem aspas, sem diferenciar maiúsculas; com aspas, diferenciando.
    [InlineData("generic", "WITH Recentes AS (SELECT * FROM t) SELECT * FROM RECENTES", new[] { "t" })]
    [InlineData("postgres", "WITH \"Recentes\" AS (SELECT * FROM t) SELECT * FROM recentes", new[] { "t", "recentes" })]
    [InlineData("postgres", "WITH recentes AS (SELECT * FROM t) SELECT * FROM \"recentes\"", new[] { "t" })]
    // Regra documentada com aspas de um lado só (ADR-0009): diferencia maiúsculas, mesmo onde o banco dobraria o
    // nome sem aspas (no PostgreSQL, C seria a CTE "c"; no Oracle, c seria uma tabela C).
    [InlineData("postgres", "WITH \"c\" AS (SELECT 1) SELECT * FROM C", new[] { "C" })]
    [InlineData("oracle", "WITH \"c\" AS (SELECT 1 FROM dual) SELECT * FROM c", new[] { "dual" })]
    [InlineData("mssql", "WITH [C] AS (SELECT 1 AS x) SELECT * FROM c", new[] { "c" })]
    // Nome qualificado nunca é CTE.
    [InlineData("generic", "WITH c AS (SELECT 1) SELECT * FROM s.c", new[] { "s.c" })]
    // WITH ... DELETE (PostgreSQL).
    [InlineData("postgres", "WITH velhos AS (SELECT id FROM pedidos WHERE ano < 2000) DELETE FROM pedidos WHERE id IN (SELECT id FROM velhos)", new[] { "pedidos" })]
    public void CTEs_nao_sao_tabelas(string dialect, string sql, string[] expected)
    {
        Assert.Equal(expected, Tables(sql, dialect));
    }

    [Fact]
    public void Tabelas_de_uma_subarvore()
    {
        var query = ((Statement.Query)Parser.Parse("WITH c AS (SELECT * FROM t) SELECT * FROM c WHERE x IN (SELECT y FROM c)")[0]).Value;
        var where = ((SetExpr.Select)query.Body).Value.Selection!;

        // Analisada sozinha, a subconsulta não vê o WITH de fora: "c" é tabela.
        Assert.Equal(new[] { "c" }, Names(where.GetReferencedTables()));
        Assert.Equal(new[] { "t" }, Names(query.GetReferencedTables()));
    }

    // ---------------------------------------------------------------------------------------------
    // GetReferencedColumns

    [Theory]
    [InlineData("generic", "SELECT a, t.b, s.t.c, a FROM t WHERE d > 1 ORDER BY e", new[] { "a", "t.b", "s.t.c", "d", "e" })]
    [InlineData("generic", "SELECT *, t.* FROM t", new string[0])]
    [InlineData("generic", "SELECT COUNT(*), SUM(x) AS total FROM t GROUP BY y HAVING SUM(x) > 0", new[] { "x", "y" })]
    [InlineData("generic", "INSERT INTO t (a, b) VALUES (1, c)", new[] { "c" })]
    [InlineData("generic", "UPDATE t SET a = b + 1 WHERE id = 2", new[] { "b", "id" })]
    [InlineData("generic", "SELECT a AS x FROM t ORDER BY x", new[] { "a", "x" })]
    [InlineData("mssql", "SELECT [Nome], c.[Idade] FROM clientes AS c", new[] { "[Nome]", "c.[Idade]" })]
    [InlineData("postgres", "SELECT \"Nome\", nome FROM t", new[] { "\"Nome\"", "nome" })]
    public void Colunas(string dialect, string sql, string[] expected)
    {
        Assert.Equal(expected, Columns(sql, dialect));
    }

    // ---------------------------------------------------------------------------------------------
    // GetReferencedFunctions

    [Theory]
    [InlineData(
        "generic",
        "SELECT COUNT(*), count(x), UPPER(nome), ROW_NUMBER() OVER (ORDER BY id) FROM t WHERE LENGTH(TRIM(nome)) > 0 AND CAST(x AS INT) > 1",
        new[] { "COUNT", "count", "UPPER", "ROW_NUMBER", "LENGTH" })]
    [InlineData("generic", "SELECT COALESCE(MAX(a), 0), COALESCE(b, 1) FROM t", new[] { "COALESCE", "MAX" })]
    [InlineData("mssql", "SELECT dbo.fn_calculo(a), GETDATE() FROM t", new[] { "dbo.fn_calculo", "GETDATE" })]
    [InlineData("postgres", "SELECT * FROM t WHERE created_at > now() - INTERVAL '1 day'", new[] { "now" })]
    [InlineData("generic", "SELECT a FROM t", new string[0])]
    // R4, item 6: funções de tabela nas três formas da AST.
    [InlineData("postgres", "SELECT * FROM t1 LEFT JOIN LATERAL fn(t1.x) AS f ON true", new[] { "fn" })]
    [InlineData("snowflake", "SELECT * FROM t, LATERAL FLATTEN(input => t.v) AS f", new[] { "FLATTEN" })]
    [InlineData("snowflake", "SELECT * FROM TABLE(udtf(1)) AS x", new[] { "udtf" })]
    [InlineData("postgres", "SELECT * FROM generate_series(1, 3) AS g", new[] { "generate_series" })]
    [InlineData("mssql", "SELECT * FROM t CROSS APPLY dbo.fn(UPPER(t.id)) AS f", new[] { "dbo.fn", "UPPER" })]
    // A dica antiga do SQL Server fica nos argumentos de função de tabela no Rust (documentado).
    [InlineData("mssql", "SELECT * FROM t (NOLOCK)", new[] { "t" })]
    [InlineData("mssql", "SELECT * FROM t WITH (NOLOCK)", new string[0])]
    // Construções com nós próprios não entram.
    [InlineData("bigquery", "SELECT * FROM UNNEST([1, 2]) AS x", new string[0])]
    public void Funcoes(string dialect, string sql, string[] expected)
    {
        Assert.Equal(expected, Functions(sql, dialect));
    }

    // ---------------------------------------------------------------------------------------------
    // GetStatementKind

    [Theory]
    [InlineData("generic", "SELECT 1", SqlStatementKind.Query)]
    [InlineData("generic", "VALUES (1), (2)", SqlStatementKind.Query)]
    [InlineData("generic", "WITH x AS (SELECT 1) SELECT * FROM x", SqlStatementKind.Query)]
    [InlineData("generic", "SELECT 1 UNION SELECT 2", SqlStatementKind.Query)]
    [InlineData("mssql", "SELECT a INTO t2 FROM t", SqlStatementKind.Query)]
    [InlineData("generic", "INSERT INTO t VALUES (1)", SqlStatementKind.Insert)]
    [InlineData("mysql", "REPLACE INTO t VALUES (1)", SqlStatementKind.Insert)]
    [InlineData("postgres", "WITH x AS (SELECT 1) INSERT INTO t SELECT * FROM x", SqlStatementKind.Insert)]
    [InlineData("generic", "UPDATE t SET a = 1", SqlStatementKind.Update)]
    [InlineData("postgres", "WITH x AS (SELECT 1) UPDATE t SET a = 1", SqlStatementKind.Update)]
    [InlineData("generic", "DELETE FROM t WHERE a = 1", SqlStatementKind.Delete)]
    [InlineData("postgres", "WITH x AS (SELECT 1) DELETE FROM t", SqlStatementKind.Delete)]
    [InlineData("generic", "MERGE INTO t USING s ON t.id = s.id WHEN MATCHED THEN DELETE", SqlStatementKind.Merge)]
    [InlineData("generic", "CREATE TABLE t (a INT)", SqlStatementKind.Create)]
    [InlineData("generic", "CREATE VIEW v AS SELECT 1", SqlStatementKind.Create)]
    [InlineData("generic", "CREATE INDEX i ON t (a)", SqlStatementKind.Create)]
    [InlineData("postgres", "CREATE FUNCTION f() RETURNS INT AS 'select 1' LANGUAGE SQL", SqlStatementKind.Create)]
    [InlineData("generic", "ALTER TABLE t ADD COLUMN b INT", SqlStatementKind.Alter)]
    [InlineData("generic", "ALTER VIEW v AS SELECT 2", SqlStatementKind.Alter)]
    [InlineData("generic", "DROP TABLE t", SqlStatementKind.Drop)]
    [InlineData("postgres", "DROP FUNCTION f", SqlStatementKind.Drop)]
    [InlineData("generic", "TRUNCATE TABLE t", SqlStatementKind.Truncate)]
    [InlineData("generic", "GRANT SELECT ON t TO u", SqlStatementKind.Grant)]
    [InlineData("generic", "REVOKE SELECT ON t FROM u", SqlStatementKind.Revoke)]
    [InlineData("mssql", "DENY SELECT ON t TO u", SqlStatementKind.Deny)]
    [InlineData("generic", "START TRANSACTION", SqlStatementKind.Transaction)]
    [InlineData("postgres", "BEGIN", SqlStatementKind.Transaction)]
    [InlineData("mssql", "BEGIN TRANSACTION", SqlStatementKind.Transaction)]
    [InlineData("snowflake", "BEGIN TRANSACTION", SqlStatementKind.Transaction)]
    // R4, item 3: bloco BEGIN ... END (no Rust, StartTransaction com HasEndKeyword) não é transação.
    [InlineData("mssql", "BEGIN SELECT 1; UPDATE t SET a = 1; END", SqlStatementKind.Block)]
    [InlineData("bigquery", "BEGIN SELECT 1; END", SqlStatementKind.Block)]
    [InlineData("snowflake", "BEGIN SELECT 1; EXCEPTION WHEN OTHER THEN SELECT 2; END", SqlStatementKind.Block)]
    [InlineData("generic", "COMMIT", SqlStatementKind.Transaction)]
    [InlineData("generic", "ROLLBACK", SqlStatementKind.Transaction)]
    [InlineData("generic", "SAVEPOINT s", SqlStatementKind.Transaction)]
    [InlineData("generic", "RELEASE SAVEPOINT s", SqlStatementKind.Transaction)]
    [InlineData("mysql", "SHOW TABLES", SqlStatementKind.Other)]
    [InlineData("generic", "SET x = 1", SqlStatementKind.Other)]
    [InlineData("generic", "EXPLAIN SELECT 1", SqlStatementKind.Other)]
    [InlineData("generic", "USE db", SqlStatementKind.Other)]
    [InlineData("generic", "CALL p()", SqlStatementKind.Other)]
    public void Tipo_da_instrucao(string dialect, string sql, SqlStatementKind expected)
    {
        Assert.Equal(expected, Assert.Single(Parser.Parse(sql, dialect)).GetStatementKind());
    }

    [Fact]
    public void Analise_valida_argumentos()
    {
        Assert.Throws<ArgumentNullException>(() => ((Statement)null!).GetReferencedTables());
        Assert.Throws<ArgumentNullException>(() => ((Statement)null!).GetReferencedColumns());
        Assert.Throws<ArgumentNullException>(() => ((Statement)null!).GetReferencedFunctions());
        Assert.Throws<ArgumentNullException>(() => ((Statement)null!).GetStatementKind());
    }
}
