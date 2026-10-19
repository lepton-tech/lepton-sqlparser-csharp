// Testes próprios da API pública (SqlQueryParser). Ver docs/adr/0008-api-publica-fase-1.md.

using SqlParser.Core;
using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Tests;

public class SqlQueryParserTests
{
    private readonly SqlQueryParser _parser = new SqlQueryParser();

    [Fact]
    public void Parse_sem_dialeto_usa_GenericDialect()
    {
        const string sql = "SELECT a, b FROM t WHERE c = 1";

        var statements = _parser.Parse(sql);

        Assert.Equal(Parser.ParseSql(new GenericDialect(), sql), statements);
        Assert.IsType<Statement.Query>(Assert.Single(statements));
    }

    [Fact]
    public void Parse_com_dialeto_nulo_explicito_usa_GenericDialect()
    {
        var statements = _parser.Parse("SELECT 1", dialect: null);

        Assert.Equal("SELECT 1", Assert.Single(statements).ToSql());
    }

    [Fact]
    public void Parse_devolve_o_mesmo_resultado_que_o_Core()
    {
        const string sql = "SELECT a::INT FROM t WHERE b ILIKE '%x%'";

        Assert.Equal(
            Parser.ParseSql(new PostgreSqlDialect(), sql),
            _parser.Parse(sql, new PostgreSqlDialect()));
    }

    [Fact]
    public void Parse_varias_instrucoes()
    {
        var statements = _parser.Parse("SELECT 1; SELECT 2;; SELECT 3");

        Assert.Equal(3, statements.Count);
        Assert.Equal(new[] { "SELECT 1", "SELECT 2", "SELECT 3" }, statements.Select(s => s.ToSql()));
        // Sequence<T> tem a propriedade First, mas o LINQ First() continua disponível (README).
        Assert.Same(statements.First, statements.First());
        Assert.Same(statements.Last, statements.Last());
    }

    [Fact]
    public void Parse_texto_vazio_devolve_sequencia_vazia()
    {
        Assert.True(_parser.Parse("").IsEmpty);
        Assert.True(_parser.Parse("  ;  -- só comentário").IsEmpty);
    }

    [Theory]
    [InlineData("postgres", typeof(PostgreSqlDialect))]
    [InlineData("PostgreSQL", typeof(PostgreSqlDialect))]
    [InlineData("MsSql", typeof(MsSqlDialect))]
    [InlineData("mysql", typeof(MySqlDialect))]
    [InlineData("bigquery", typeof(BigQueryDialect))]
    [InlineData("sparksql", typeof(SparkSqlDialect))]
    public void Parse_por_nome_de_dialeto_equivale_a_instancia(string dialectName, Type dialectType)
    {
        const string sql = "SELECT a FROM t WHERE b = 1";
        var dialect = (Dialect)Activator.CreateInstance(dialectType)!;

        Assert.Equal(_parser.Parse(sql, dialect), _parser.Parse(sql, dialectName));
    }

    [Fact]
    public void Parse_por_nome_usa_as_regras_do_dialeto()
    {
        // `::` é cast no PostgreSQL; [ ] delimita identificadores no MS SQL.
        Assert.Equal("SELECT a::INT FROM t", Assert.Single(_parser.Parse("SELECT a::INT FROM t", "postgres")).ToSql());
        Assert.Equal(
            "SELECT TOP 5 [nome] FROM [dbo].[clientes]",
            Assert.Single(_parser.Parse("SELECT TOP 5 [nome] FROM [dbo].[clientes]", "mssql")).ToSql());
    }

    [Fact]
    public void Parse_com_nome_de_dialeto_desconhecido_lanca_ArgumentException()
    {
        var e = Assert.Throws<ArgumentException>(() => _parser.Parse("SELECT 1", "nao-existe"));

        Assert.Equal("dialectName", e.ParamName);
    }

    [Fact]
    public void Parse_com_argumentos_nulos_lanca_ArgumentNullException()
    {
        Assert.Equal("sql", Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!)).ParamName);
        Assert.Equal("dialectName", Assert.Throws<ArgumentNullException>(() => _parser.Parse("SELECT 1", (string)null!)).ParamName);
        Assert.Equal("sql", Assert.Throws<ArgumentNullException>(() => _parser.TryParse(null!, out _, out _)).ParamName);
        Assert.Equal("sql", Assert.Throws<ArgumentNullException>(() => _parser.ParseWithComments(null!)).ParamName);
    }

    [Fact]
    public void Parse_com_SQL_invalido_lanca_SqlParseException()
    {
        var e = Assert.Throws<SqlParseException>(() => _parser.Parse("SELEC 1"));

        Assert.Equal("sql parser error: Expected: an SQL statement, found: SELEC at Line: 1, Column: 1", e.Message);
    }

    [Fact]
    public void Opcoes_informadas_sao_repassadas_ao_parser()
    {
        const string sql = "SELECT a, b, FROM t";

        // PostgreSQL não aceita vírgula final por padrão; a opção liga o recurso.
        Assert.Throws<SqlParseException>(() => _parser.Parse(sql, new PostgreSqlDialect()));
        var statements = _parser.Parse(sql, new PostgreSqlDialect(), new ParserOptions().WithTrailingCommas(true));

        Assert.Equal("SELECT a, b FROM t", Assert.Single(statements).ToSql());
    }

    [Fact]
    public void Opcoes_informadas_substituem_as_opcoes_padrao_do_dialeto()
    {
        // DuckDB aceita vírgula final (SupportsTrailingCommas). Opções explícitas substituem o padrão do
        // dialeto por inteiro, como Parser::with_options no Rust.
        const string sql = "SELECT a FROM t WHERE a IN (1, 2,)";

        Assert.Single(_parser.Parse(sql, new DuckDbDialect()));
        Assert.Throws<SqlParseException>(() => _parser.Parse(sql, new DuckDbDialect(), new ParserOptions()));
    }

    [Fact]
    public void Opcao_sem_ponto_e_virgula_entre_instrucoes()
    {
        const string sql = "SELECT 1 SELECT 2";
        var options = new ParserOptions(TrailingCommas: false, Unescape: true, RequireSemicolonStmtDelimiter: false);

        Assert.Throws<SqlParseException>(() => _parser.Parse(sql));
        // Parse(sql, null, options) seria ambíguo com a sobrecarga por nome: use o argumento nomeado.
        Assert.Equal(2, _parser.Parse(sql, options: options).Count);
    }

    [Fact]
    public void TryParse_com_SQL_valido_devolve_true()
    {
        var ok = _parser.TryParse("SELECT 1; SELECT 2", out var statements, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(2, statements.Count);
    }

    [Fact]
    public void TryParse_com_SQL_invalido_devolve_false_e_o_erro()
    {
        var ok = _parser.TryParse("SELECT * FROM t WHERE x = = 1", out var statements, out var error);

        Assert.False(ok);
        Assert.True(statements.IsEmpty);
        Assert.NotNull(error);
        Assert.Equal("sql parser error: Expected: an expression, found: = at Line: 1, Column: 27", error.Message);
        Assert.Equal(1, error.Line);
        Assert.Equal(27, error.Column);
    }

    [Fact]
    public void TryParse_com_dialeto_e_opcoes()
    {
        Assert.True(_parser.TryParse("SELECT `a` FROM `t` LIMIT 5", new MySqlDialect(), out var statements, out _));
        Assert.Equal("SELECT `a` FROM `t` LIMIT 5", statements.ToSql());

        Assert.False(_parser.TryParse("SELECT a, FROM t", new PostgreSqlDialect(), null, out _, out var error));
        Assert.IsType<ParserError.ParserErrorValue>(error.Error);

        Assert.True(_parser.TryParse(
            "SELECT a, FROM t",
            new PostgreSqlDialect(),
            new ParserOptions().WithTrailingCommas(true),
            out statements,
            out _));
        Assert.Equal("SELECT a FROM t", statements.ToSql());
    }

    [Fact]
    public void Limite_de_recursao_configurado()
    {
        const string sql = "SELECT ((((((1))))))";
        Assert.Single(_parser.Parse(sql));

        var limited = new SqlQueryParser(recursionLimit: 3);
        Assert.Equal(3, limited.RecursionLimit);
        Assert.False(limited.TryParse(sql, out _, out var error));
        Assert.IsType<ParserError.RecursionLimitExceeded>(error.Error);
        Assert.Equal("sql parser error: recursion limit exceeded", error.Message);
        Assert.Null(error.Location);
    }

    [Fact]
    public void Limite_de_recursao_padrao_e_o_do_Core()
    {
        Assert.Null(new SqlQueryParser().RecursionLimit);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlQueryParser(-1));
    }

    [Fact]
    public void ParseWithComments_devolve_instrucoes_e_comentarios()
    {
        const string sql = "SELECT a FROM t -- comentário\n/* bloco */ WHERE b = 1";

        var result = _parser.ParseWithComments(sql);
        var (statements, comments) = result;

        Assert.Equal(_parser.Parse(sql), statements);
        Assert.Equal(result.Statements, statements);
        Assert.Same(result.Comments, comments);
        var list = comments.ToVec();
        Assert.Equal(2, list.Count);
        Assert.Equal(new Comment.SingleLine(" comentário", "--"), list[0].Comment);
        Assert.Equal(new Location(1, 17), list[0].Span.Start);
        Assert.Equal(new Comment.MultiLine(" bloco "), list[1].Comment);
        Assert.Equal(new Location(2, 1), list[1].Span.Start);
    }

    [Fact]
    public void ParseWithComments_com_SQL_invalido_lanca_SqlParseException()
    {
        var e = Assert.Throws<SqlParseException>(() => _parser.ParseWithComments("SELECT 'abc -- sem fim"));

        Assert.IsType<ParserError.TokenizerError>(e.Error);
    }

    [Fact]
    public void A_mesma_instancia_pode_ser_usada_em_paralelo()
    {
        var results = Enumerable.Range(0, 64)
            .AsParallel()
            .Select(i => _parser.Parse($"SELECT c{i} FROM t{i} WHERE x = {i}", new PostgreSqlDialect()).ToSql())
            .ToList();

        Assert.All(Enumerable.Range(0, 64), i => Assert.Contains($"SELECT c{i} FROM t{i} WHERE x = {i}", results));
    }
}
