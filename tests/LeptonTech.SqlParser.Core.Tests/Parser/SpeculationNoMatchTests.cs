// Testes próprios do port (não existem no sqlparser-rs): fixam o contrato das variantes sem exceção usadas nas
// especulações quentes (`// PORT-PERF`, convencoes.md §13, caso 140; docs/port/onda-d/P2.md).
//   - Cada `XNoMatch` devolve null exatamente onde `X` lança um erro que não é RecursionLimitExceeded e, nos
//     demais casos, faz o mesmo que `X`: mesmo resultado, mesma posição final, mesmo erro.
//   - As expressões comuns não lançam nenhuma exceção durante o parse.
// Se uma sincronização com o upstream mudar `X` (por exemplo, um braço novo para outro tipo de token) sem
// atualizar a variante, os testes de contrato devem falhar.

using System.Runtime.ExceptionServices;
using LeptonTech.SqlParser.Core.Ast;
using LeptonTech.SqlParser.Core.Dialects;

namespace LeptonTech.SqlParser.Core.Tests;

public class SpeculationNoMatchTests
{
    // Os 16 dialetos embutidos.
    private static readonly Func<Dialect>[] Dialetos =
    {
        () => new GenericDialect(),
        () => new AnsiDialect(),
        () => new BigQueryDialect(),
        () => new ClickHouseDialect(),
        () => new DatabricksDialect(),
        () => new DuckDbDialect(),
        () => new HiveDialect(),
        () => new MsSqlDialect(),
        () => new MySqlDialect(),
        () => new OracleDialect(),
        () => new PostgreSqlDialect(),
        () => new RedshiftSqlDialect(),
        () => new SQLiteDialect(),
        () => new SnowflakeDialect(),
        () => new SparkSqlDialect(),
        () => new TeradataDialect(),
    };

    // Entradas que começam com tipos de token variados (palavras, keywords de tipo, strings de cada tipo de
    // aspas, números, operadores, pontuação e o fim da entrada), sozinhas e depois de um identificador.
    private static readonly string[] Entradas =
    {
        "", "a", "a.b", "a b", "INT", "INT(10)", "INT[]", "INT[3]", "INT ARRAY", "INT ARRAY[2]", "VARCHAR(10)",
        "DECIMAL(10, 2)", "TIMESTAMP WITH TIME ZONE", "INTERVAL '1' DAY", "DATE '2020-01-01'", "DATE", "VALUE 1",
        "VALUE", "xml '<a/>'", "BINARY 'x'", "BINARY(4)", "count(*)", "sum(a * b)", "coalesce(a, 0)",
        "STRUCT<a INT>", "ARRAY<INT>", "MAP(INT, TEXT)", "MAP<INT, TEXT>", "Nullable(Int32)", "NOT a", "CASE",
        "NULL", "TRUE", "ROW(1)", "TABLE", "ANY TYPE", "SIGNED INTEGER", "'s'", "\"q\"", "`q`", "[q]", "E'x'",
        "N'x'", "X'01'", "B'01'", "$$x$$", "r'x'", "1", "1.5", "0x1F", "=> 1", "= 1", ":= 1", ": 1", "-> 1",
        "(", ")", ",", "*", "-1", "+1", "~1", "!1", "@x", "$1", "?", ":x", "[1]", "{a: 1}", "::INT", ";", "#x",
        "||", "<", ">", "%", "/", "^", "->>", "@>", "a => 1", "a = 1", "a := 1", "a : 1", "a -> 1", "a VALUE 1",
        "a)", "a,", "a * b", "'s' => 1", "\"q\" = 1", "1 => 2",
    };

    private sealed class Resultado
    {
        public string Texto = "";
        public bool SemCorrespondencia;
        public bool ErroComum;
    }

    private static Resultado Rodar<T>(Dialect dialect, string sql, int? limite, Func<Parser, T?> f)
        where T : class
    {
        var parser = new Parser(dialect);
        if (limite is { } l)
        {
            parser = parser.WithRecursionLimit(l);
        }
        parser = parser.TryWithSql(sql);
        try
        {
            var valor = f(parser);
            return valor is null
                ? new Resultado { SemCorrespondencia = true, Texto = "sem correspondência" }
                : new Resultado { Texto = $"OK {valor} @{parser.Index()}" };
        }
        catch (ParserException e)
        {
            return new Resultado
            {
                Texto = $"{e.Error.GetType().Name}: {e.Message} @{parser.Index()}",
                ErroComum = e.Error is not ParserError.RecursionLimitExceeded,
            };
        }
    }

    /// <summary>
    /// Confere o contrato: null só onde o original lança um erro comum (que o maybe_parse descarta); fora
    /// isso, resultado, posição e erro iguais. Devolve quantas entradas deram "sem correspondência".
    /// </summary>
    private static int ConferirContrato<T>(Func<Parser, T> original, Func<Parser, T?> semExcecao, int? limite = null)
        where T : class
    {
        var semCorrespondencia = 0;
        foreach (var novoDialeto in Dialetos)
        {
            foreach (var sql in Entradas)
            {
                var dialect = novoDialeto();
                try
                {
                    new Tokenizer(dialect, sql).Tokenize();
                }
                catch (TokenizerException)
                {
                    continue;
                }
                var esperado = Rodar(dialect, sql, limite, p => original(p));
                var obtido = Rodar(dialect, sql, limite, semExcecao);
                var contexto = $"{dialect.GetType().Name}, entrada \"{sql}\"";
                if (obtido.SemCorrespondencia)
                {
                    semCorrespondencia++;
                    Assert.True(esperado.ErroComum, $"{contexto}: a variante devolveu null, mas o original deu {esperado.Texto}");
                }
                else
                {
                    Assert.True(esperado.Texto == obtido.Texto, $"{contexto}: original {esperado.Texto}; variante {obtido.Texto}");
                }
            }
        }
        return semCorrespondencia;
    }

    [Fact]
    public void ParseDataTypeNoMatch_cumpre_o_contrato_de_ParseDataType()
    {
        var n = ConferirContrato(p => p.ParseDataType(), p => p.ParseDataTypeNoMatch());
        Assert.True(n > 0);
    }

    [Fact]
    public void ParseDataTypeNoMatch_mantem_o_RecursionLimitExceeded()
    {
        // Com o contador esgotado, parse_data_type_helper falha no try_decrease, antes de olhar o token, e o
        // maybe_parse repassa esse erro: a variante não pode devolver null.
        ConferirContrato(p => p.ParseDataType(), p => p.ParseDataTypeNoMatch(), limite: 0);
        var ex = Assert.Throws<ParserException>(
            () => new Parser(new GenericDialect()).WithRecursionLimit(0).TryWithSql("1").ParseDataTypeNoMatch());
        Assert.IsType<ParserError.RecursionLimitExceeded>(ex.Error);
    }

    [Fact]
    public void ParseIdentifierNoMatch_cumpre_o_contrato_de_ParseIdentifier()
    {
        var n = ConferirContrato(p => p.ParseIdentifier(), p => p.ParseIdentifierNoMatch());
        Assert.True(n > 0);
    }

    [Fact]
    public void ParseFunctionNamedArgOperatorNoMatch_cumpre_o_contrato_de_ParseFunctionNamedArgOperator()
    {
        var n = ConferirContrato(p => p.ParseFunctionNamedArgOperator(), p => p.ParseFunctionNamedArgOperatorNoMatch());
        Assert.True(n > 0);
    }

    [Fact]
    public void MaybeParseNoMatch_volta_a_posicao_quando_a_closure_devolve_null()
    {
        var parser = new Parser(new GenericDialect()).TryWithSql("a b c");
        var r = parser.MaybeParseNoMatch<Ident>(p =>
        {
            p.ParseIdentifier();
            p.ParseIdentifier();
            return null;
        });
        Assert.Null(r);
        Assert.Equal(0, parser.Index());
        Assert.Equal("a", parser.ParseIdentifier().Value);
    }

    [Fact]
    public void MaybeParseNoMatch_repassa_RecursionLimitExceeded_e_volta_a_posicao()
    {
        var parser = new Parser(new GenericDialect()).TryWithSql("a b c");
        var ex = Assert.Throws<ParserException>(() => parser.MaybeParseNoMatch<Ident>(p =>
        {
            p.ParseIdentifier();
            throw new ParserException(new ParserError.RecursionLimitExceeded());
        }));
        Assert.IsType<ParserError.RecursionLimitExceeded>(ex.Error);
        Assert.Equal(0, parser.Index());
    }

    [Fact]
    public void MaybeParseNoMatch_descarta_outros_erros_e_volta_a_posicao()
    {
        var parser = new Parser(new GenericDialect()).TryWithSql("a b c");
        var r = parser.MaybeParseNoMatch<Ident>(p =>
        {
            p.ParseIdentifier();
            return p.ExpectedRef<Ident>("x", p.PeekTokenRef());
        });
        Assert.Null(r);
        Assert.Equal(0, parser.Index());
    }

    /// <summary>
    /// Exceções de primeira chance lançadas nesta thread durante o parse (null se o dialeto não aceita a
    /// instrução).
    /// </summary>
    private static List<string>? ExcecoesNoParse(Dialect dialect, string sql)
    {
        var thread = Environment.CurrentManagedThreadId;
        var excecoes = new List<string>();
        void Contar(object? sender, FirstChanceExceptionEventArgs e)
        {
            if (Environment.CurrentManagedThreadId == thread)
            {
                excecoes.Add(e.Exception.Message);
            }
        }

        AppDomain.CurrentDomain.FirstChanceException += Contar;
        try
        {
            Parser.ParseSql(dialect, sql);
        }
        catch (ParserException)
        {
            return null;
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= Contar;
        }
        return excecoes;
    }

    [Fact]
    public void Expressoes_comuns_nao_lancam_excecao()
    {
        // Antes da P2, cada nó de expressão lançava e capturava ao menos uma ParserException na especulação de
        // literal tipado, e cada argumento de função sem nome, outra. Agora o número de exceções não cresce
        // com o número de expressões (1 × 20 repetições da parte entre chaves), em nenhum dialeto, e é zero no
        // GenericDialect. Sobram, em alguns dialetos, especulações de uma vez por instrução (por exemplo, o
        // alias de tabela do INSERT no Oracle).
        var modelos = new (string Antes, string Repetida, string Separador, string Depois)[]
        {
            ("SELECT ", "a, b, t.c AS x, 1.5, 'abc', NULL, -d", ", ", " FROM t WHERE id = 1"),
            ("INSERT INTO t (a, b, c) VALUES ", "(1, 'x', NULL), (2, 'y', -3.25)", ", ", ""),
            ("SELECT ", "COALESCE(a, 0), ROUND(b, 2), UPPER(c), MAX(d), SUBSTRING(e, 1, 3)", ", ", " FROM t GROUP BY f"),
            ("SELECT * FROM t WHERE ", "a > 1.5 AND b LIKE 'x%' AND c IS NOT NULL", " AND ", " ORDER BY a DESC"),
            ("SELECT ", "CAST(a AS INT), DATE '2020-01-01', INTERVAL '1' DAY", ", ", " FROM t"),
            ("UPDATE t SET ", "a = a + 1, b = 'z'", ", ", " WHERE c = 2"),
        };

        var aceitas = 0;
        foreach (var novoDialeto in Dialetos)
        {
            foreach (var (antes, repetida, separador, depois) in modelos)
            {
                var dialect = novoDialeto();
                var uma = antes + repetida + depois;
                var vinte = antes + string.Join(separador, Enumerable.Repeat(repetida, 20)) + depois;
                var excecoesUma = ExcecoesNoParse(dialect, uma);
                var excecoesVinte = ExcecoesNoParse(dialect, vinte);
                if (excecoesUma is null || excecoesVinte is null)
                {
                    continue;
                }
                aceitas++;
                var contexto = $"{dialect.GetType().Name}, \"{uma}\": {string.Join(" | ", excecoesVinte)}";
                Assert.True(excecoesUma.Count == excecoesVinte.Count, contexto);
                if (dialect is GenericDialect)
                {
                    Assert.True(excecoesVinte.Count == 0, contexto);
                }
            }
        }
        Assert.True(aceitas >= modelos.Length * 10);
    }
}
