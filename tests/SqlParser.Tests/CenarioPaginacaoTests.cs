using System.Globalization;
using Microsoft.Data.Sqlite;
using SqlParser.Core;
using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser.Tests;

/// <summary>
/// Cenário de uso, sem funcionalidade própria na biblioteca: a paginação de um grid. O sistema tem um SQL base
/// (com joins, filtros fixos, às vezes GROUP BY) e, <b>por cima dele</b>, o grid aplica o que o usuário escolheu na
/// tela: filtros, ordenação por coluna, agrupamento, contagem de linhas e a página. O teste mostra como fazer isso só
/// com a API existente (ler o SQL, montar a consulta externa na árvore, gerar o SQL) e confere os resultados
/// executando tudo num SQLite em memória, com parâmetros (nenhum valor digitado pelo usuário entra no texto do SQL).
/// </summary>
public class CenarioPaginacaoTests
{
    // --- SQLs base do sistema ---------------------------------------------------------------------------------------

    private const string PedidosPagos =
        "SELECT c.id, c.nome, p.id AS pedido_id, p.valor " +
        "FROM clientes AS c INNER JOIN pedidos AS p ON p.cliente_id = c.id " +
        "WHERE p.status = 'PAGO' " +
        "ORDER BY c.nome, p.id";

    private const string PedidosPagosSemOrdem =
        "SELECT c.id, c.nome, p.id AS pedido_id, p.valor " +
        "FROM clientes AS c INNER JOIN pedidos AS p ON p.cliente_id = c.id " +
        "WHERE p.status = 'PAGO'";

    // SQL base que já é agregado: um total por cliente.
    private const string TotaisPorCliente =
        "SELECT c.id, c.nome, COUNT(p.id) AS pedidos, SUM(p.valor) AS total " +
        "FROM clientes AS c INNER JOIN pedidos AS p ON p.cliente_id = c.id " +
        "WHERE p.status = 'PAGO' " +
        "GROUP BY c.id, c.nome";

    // --- O que a tela do grid envia -----------------------------------------------------------------------------------

    private sealed record Filtro(string Coluna, string Operador, string Parametro);

    private sealed record Ordem(string Coluna, bool Decrescente = false);

    private sealed record Agrupamento(string Coluna, string? Somar = null);

    private sealed record PedidoGrid(int Pagina, int Tamanho)
    {
        public Filtro[] Filtros { get; init; } = new Filtro[0];
        public Ordem[] Ordenacao { get; init; } = new Ordem[0];
        public Agrupamento? Agrupar { get; init; }
    }

    private sealed record SqlGrid(string Pagina, string Contagem);

    // --- Paginação simples (SQL base + página) ------------------------------------------------------------------------

    [Theory]
    [InlineData("postgres", " LIMIT 10 OFFSET 20")]
    [InlineData("mysql", " LIMIT 10 OFFSET 20")]
    [InlineData("sqlite", " LIMIT 10 OFFSET 20")]
    [InlineData("mssql", " OFFSET 20 ROWS FETCH FIRST 10 ROWS ONLY")]
    [InlineData("oracle", " OFFSET 20 ROWS FETCH FIRST 10 ROWS ONLY")]
    public void Pagina_3_com_10_linhas_em_cada_dialeto(string nomeDialeto, string clausulaEsperada)
    {
        var dialeto = Dialect.DialectFromStr(nomeDialeto)!;

        var sql = ToSql(AplicarPagina(LerConsulta(PedidosPagos, dialeto), dialeto, pagina: 3, tamanho: 10));

        Assert.Equal(PedidosPagos + clausulaEsperada, sql);
        Assert.Equal(sql, new SqlQueryParser().Parse(sql, dialeto).ToSql()); // válido no próprio dialeto
    }

    [Fact]
    public void SQL_Server_sem_ORDER_BY_precisa_de_um()
    {
        // No SQL Server, OFFSET/FETCH exige ORDER BY; sem ordem definida, usa-se ORDER BY (SELECT NULL).
        var dialeto = new MsSqlDialect();

        var sql = ToSql(AplicarPagina(LerConsulta(PedidosPagosSemOrdem, dialeto), dialeto, pagina: 2, tamanho: 5));

        Assert.Equal(PedidosPagosSemOrdem + " ORDER BY (SELECT NULL) OFFSET 5 ROWS FETCH FIRST 5 ROWS ONLY", sql);
    }

    // --- Grid: SQL por cima do SQL base -------------------------------------------------------------------------------

    private static readonly PedidoGrid FiltrosEOrdem = new(Pagina: 2, Tamanho: 5)
    {
        Filtros = new[] { new Filtro("nome", "LIKE", "nome"), new Filtro("valor", ">=", "minimo") },
        Ordenacao = new[] { new Ordem("valor", Decrescente: true), new Ordem("pedido_id") },
    };

    [Fact]
    public void Grid_SQL_Server()
    {
        var sql = MontarGrid(PedidosPagos, new MsSqlDialect(), FiltrosEOrdem);

        // O SQL base vira subconsulta (sem o ORDER BY dele, que o SQL Server proíbe numa tabela derivada); os
        // filtros, a ordem e a página do grid ficam na consulta externa, sobre as colunas de saída do SQL base.
        Assert.Equal(
            "SELECT * FROM (" + PedidosPagosSemOrdem + ") grid " +
            "WHERE grid.nome LIKE @nome AND grid.valor >= @minimo " +
            "ORDER BY grid.valor DESC, grid.pedido_id " +
            "OFFSET 5 ROWS FETCH FIRST 5 ROWS ONLY",
            sql.Pagina);
        Assert.Equal(
            "SELECT COUNT(*) AS total FROM (" + PedidosPagosSemOrdem + ") grid " +
            "WHERE grid.nome LIKE @nome AND grid.valor >= @minimo",
            sql.Contagem);
    }

    [Fact]
    public void Grid_PostgreSQL_usa_parametros_posicionais()
    {
        var sql = MontarGrid(PedidosPagos, new PostgreSqlDialect(), FiltrosEOrdem);

        Assert.Equal(
            "SELECT * FROM (" + PedidosPagosSemOrdem + ") grid " +
            "WHERE grid.nome LIKE $1 AND grid.valor >= $2 " +
            "ORDER BY grid.valor DESC, grid.pedido_id " +
            "LIMIT 5 OFFSET 5",
            sql.Pagina);
    }

    [Fact]
    public void Grid_sem_ordem_escolhida_herda_o_ORDER_BY_do_SQL_base()
    {
        // ORDER BY c.nome, p.id do SQL base vira ORDER BY grid.nome, grid.pedido_id (as colunas de saída).
        var sql = MontarGrid(PedidosPagos, new SQLiteDialect(), new PedidoGrid(Pagina: 1, Tamanho: 10));

        Assert.Equal(
            "SELECT * FROM (" + PedidosPagosSemOrdem + ") grid ORDER BY grid.nome, grid.pedido_id LIMIT 10",
            sql.Pagina);
    }

    [Fact]
    public void Grid_com_agrupamento_por_cima()
    {
        var pedido = new PedidoGrid(Pagina: 1, Tamanho: 3)
        {
            Filtros = new[] { new Filtro("valor", ">=", "minimo") },
            Agrupar = new Agrupamento("nome", Somar: "valor"),
            Ordenacao = new[] { new Ordem("soma", Decrescente: true) },
        };

        var sql = MontarGrid(PedidosPagos, new SQLiteDialect(), pedido);

        Assert.Equal(
            "SELECT grid.nome, COUNT(*) AS quantidade, SUM(grid.valor) AS soma " +
            "FROM (" + PedidosPagosSemOrdem + ") grid " +
            "WHERE grid.valor >= @minimo GROUP BY grid.nome ORDER BY soma DESC LIMIT 3",
            sql.Pagina);
        // Com agrupamento, a contagem é de grupos.
        Assert.Equal(
            "SELECT COUNT(*) AS total FROM (" +
            "SELECT grid.nome, COUNT(*) AS quantidade, SUM(grid.valor) AS soma " +
            "FROM (" + PedidosPagosSemOrdem + ") grid " +
            "WHERE grid.valor >= @minimo GROUP BY grid.nome) contagem",
            sql.Contagem);
    }

    [Fact]
    public void Grid_sobre_SQL_base_com_CTE_mantem_o_WITH_no_topo()
    {
        const string comCte =
            "WITH pagos AS (SELECT * FROM pedidos WHERE status = 'PAGO') " +
            "SELECT c.nome, p.valor FROM clientes AS c INNER JOIN pagos AS p ON p.cliente_id = c.id";
        var pedido = new PedidoGrid(Pagina: 1, Tamanho: 10) { Ordenacao = new[] { new Ordem("valor") } };

        var sql = MontarGrid(comCte, new MsSqlDialect(), pedido);

        Assert.Equal(
            "WITH pagos AS (SELECT * FROM pedidos WHERE status = 'PAGO') " +
            "SELECT * FROM (SELECT c.nome, p.valor FROM clientes AS c INNER JOIN pagos AS p ON p.cliente_id = c.id) grid " +
            "ORDER BY grid.valor OFFSET 0 ROWS FETCH FIRST 10 ROWS ONLY",
            sql.Pagina);
    }

    [Fact]
    public void Coluna_que_nao_existe_no_SQL_base_e_recusada()
    {
        // As colunas do grid são validadas contra as colunas de saída do SQL base.
        var pedido = new PedidoGrid(1, 10) { Ordenacao = new[] { new Ordem("valor; DROP TABLE pedidos --") } };

        var ex = Assert.Throws<ArgumentException>(() => MontarGrid(PedidosPagos, new SQLiteDialect(), pedido));
        Assert.Contains("id, nome, pedido_id, valor", ex.Message);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData("mysql")]
    [InlineData("mssql")]
    [InlineData("oracle")]
    [InlineData("sqlite")]
    public void SQL_do_grid_e_valido_no_proprio_dialeto(string nomeDialeto)
    {
        var dialeto = Dialect.DialectFromStr(nomeDialeto)!;
        var pedido = FiltrosEOrdem with { Agrupar = new Agrupamento("nome", "valor"), Ordenacao = new[] { new Ordem("soma") } };

        foreach (var grid in new[] { MontarGrid(PedidosPagos, dialeto, FiltrosEOrdem), MontarGrid(PedidosPagos, dialeto, pedido) })
        {
            Assert.Equal(grid.Pagina, new SqlQueryParser().Parse(grid.Pagina, dialeto).ToSql());
            Assert.Equal(grid.Contagem, new SqlQueryParser().Parse(grid.Contagem, dialeto).ToSql());
        }
    }

    // --- Execução no banco (SQLite em memória) ------------------------------------------------------------------------

    [Fact]
    public void Paginacao_simples_no_banco_reproduz_o_resultado_completo()
    {
        using var conexao = CriarBanco();
        var dialeto = new SQLiteDialect();

        var completo = Linhas(conexao, PedidosPagos);
        Assert.Equal(23, completo.Count);

        var juntas = new List<string>();
        for (var pagina = 1; pagina <= 3; pagina++)
        {
            juntas.AddRange(Linhas(conexao, ToSql(AplicarPagina(LerConsulta(PedidosPagos, dialeto), dialeto, pagina, 10))));
        }
        Assert.Equal(completo, juntas); // 10 + 10 + 3, sem repetir nem pular
    }

    [Fact]
    public void Grid_com_filtros_e_ordem_no_banco()
    {
        using var conexao = CriarBanco();
        var dialeto = new SQLiteDialect();
        var parametros = new Dictionary<string, object> { ["@nome"] = "%", ["@minimo"] = 100 };

        // Pedidos com valor >= 100 (16), do maior para o menor; página 2 com 5 linhas.
        var grid = MontarGrid(PedidosPagos, dialeto, FiltrosEOrdem);
        Assert.Equal(16L, Escalar(conexao, grid.Contagem, parametros));
        Assert.Equal(
            new[] { "3|Carla|23|241.5", "2|Bruno|22|231", "1|Ana|21|220.5", "4|Diego|19|199.5", "3|Carla|18|189" },
            Linhas(conexao, grid.Pagina, parametros));

        // O filtro do usuário muda só o parâmetro: clientes que começam com "B".
        parametros["@nome"] = "B%";
        var primeira = MontarGrid(PedidosPagos, dialeto, FiltrosEOrdem with { Pagina = 1 });
        Assert.Equal(3L, Escalar(conexao, primeira.Contagem, parametros));
        Assert.Equal(
            new[] { "2|Bruno|27|283.5", "2|Bruno|22|231", "2|Bruno|17|178.5" },
            Linhas(conexao, primeira.Pagina, parametros));

        // Todas as páginas juntas = a mesma consulta do grid sem página.
        parametros["@nome"] = "%";
        var semPagina = MontarGrid(PedidosPagos, dialeto, FiltrosEOrdem with { Pagina = 1, Tamanho = 1000 });
        var juntas = new List<string>();
        for (var pagina = 1; pagina <= 4; pagina++)
        {
            juntas.AddRange(Linhas(conexao, MontarGrid(PedidosPagos, dialeto, FiltrosEOrdem with { Pagina = pagina }).Pagina, parametros));
        }
        Assert.Equal(Linhas(conexao, semPagina.Pagina, parametros), juntas);
    }

    [Fact]
    public void Grid_com_agrupamento_no_banco()
    {
        using var conexao = CriarBanco();
        var parametros = new Dictionary<string, object> { ["@minimo"] = 100 };
        var pedido = new PedidoGrid(Pagina: 1, Tamanho: 3)
        {
            Filtros = new[] { new Filtro("valor", ">=", "minimo") },
            Agrupar = new Agrupamento("nome", Somar: "valor"),
            Ordenacao = new[] { new Ordem("soma", Decrescente: true) },
        };

        var grid = MontarGrid(PedidosPagos, new SQLiteDialect(), pedido);

        Assert.Equal(5L, Escalar(conexao, grid.Contagem, parametros)); // 5 clientes (grupos)
        Assert.Equal(new[] { "Elisa|4|840", "Bruno|3|693", "Diego|3|651" }, Linhas(conexao, grid.Pagina, parametros));
    }

    [Fact]
    public void Grid_sobre_SQL_base_agregado_no_banco()
    {
        // O SQL base já tem GROUP BY; o filtro do grid sobre o total vira um WHERE simples na consulta externa
        // (sem precisar mexer no HAVING do SQL base).
        using var conexao = CriarBanco();
        var parametros = new Dictionary<string, object> { ["@minimo"] = 700 };
        var pedido = new PedidoGrid(Pagina: 1, Tamanho: 2)
        {
            Filtros = new[] { new Filtro("total", ">=", "minimo") },
            Ordenacao = new[] { new Ordem("total", Decrescente: true) },
        };

        var grid = MontarGrid(TotaisPorCliente, new SQLiteDialect(), pedido);

        Assert.Equal(3L, Escalar(conexao, grid.Contagem, parametros)); // Elisa, Bruno e Diego
        Assert.Equal(new[] { "5|Elisa|5|892.5", "2|Bruno|5|787.5" }, Linhas(conexao, grid.Pagina, parametros));
        Assert.Equal(
            new[] { "4|Diego|4|745.5" },
            Linhas(conexao, MontarGrid(TotaisPorCliente, new SQLiteDialect(), pedido with { Pagina = 2 }).Pagina, parametros));
    }

    // --- Como um sistema montaria o SQL do grid com a biblioteca --------------------------------------------------------

    /// <summary>
    /// <c>SELECT * FROM (&lt;SQL base&gt;) grid WHERE &lt;filtros&gt; [GROUP BY] ORDER BY &lt;ordem&gt; &lt;página&gt;</c> e a contagem
    /// correspondente, montados na árvore. Valores sempre como parâmetros; colunas validadas contra o SQL base.
    /// </summary>
    private static SqlGrid MontarGrid(string sqlBase, Dialect dialeto, PedidoGrid pedido)
    {
        var baseCompleta = LerConsulta(sqlBase, dialeto);
        var colunas = ColunasDeSaida(baseCompleta);
        // O WITH sobe para a consulta externa; o ORDER BY sai (inútil numa subconsulta e proibido no SQL Server).
        var subconsulta = baseCompleta with { With = null, OrderBy = null };

        Expr? filtro = null;
        for (var i = 0; i < pedido.Filtros.Length; i++)
        {
            var f = pedido.Filtros[i];
            var condicao = Condicao(Coluna(colunas, f.Coluna), f.Operador, Parametro(dialeto, f.Parametro, i));
            filtro = filtro is null ? condicao : new Expr.BinaryOp(filtro, new BinaryOperator.And(), condicao);
        }

        // Consulta externa: SELECT * ou, com agrupamento, SELECT grid.<coluna>, COUNT(*), SUM(...) ... GROUP BY.
        // Os nomes de coluna usados no modelo já foram validados contra o SQL base.
        string modelo;
        string[] colunasExternas;
        if (pedido.Agrupar is { } agrupar)
        {
            var coluna = Validar(colunas, agrupar.Coluna);
            var soma = agrupar.Somar is null ? "" : $", SUM(grid.{Validar(colunas, agrupar.Somar)}) AS soma";
            modelo = $"SELECT grid.{coluna}, COUNT(*) AS quantidade{soma} FROM (SELECT 1) grid GROUP BY grid.{coluna}";
            colunasExternas = agrupar.Somar is null ? new[] { coluna, "quantidade" } : new[] { coluna, "quantidade", "soma" };
        }
        else
        {
            modelo = "SELECT * FROM (SELECT 1) grid";
            colunasExternas = colunas;
        }
        var externa = TrocarSubconsulta(LerConsulta(modelo, dialeto), subconsulta, filtro) with { With = baseCompleta.With };

        // Ordem: a escolhida no grid; sem escolha (e sem agrupamento), a do SQL base, traduzida para as colunas de saída.
        var ordem = pedido.Ordenacao.Length > 0
            ? OrderBy(pedido.Ordenacao.Select(o => (Expr: pedido.Agrupar is null
                    ? Coluna(colunas, o.Coluna)
                    : new Expr.Identifier(new Ident(Validar(colunasExternas, o.Coluna))), o.Decrescente)))
            : pedido.Agrupar is null ? OrdemDoSqlBase(baseCompleta, colunas) : null;
        var pagina = AplicarPagina(externa with { OrderBy = ordem }, dialeto, pedido.Pagina, pedido.Tamanho);

        // Contagem: sem agrupamento, COUNT(*) sobre o SQL base com os filtros; com agrupamento, conta os grupos.
        var contagem = pedido.Agrupar is null
            ? TrocarSubconsulta(LerConsulta("SELECT COUNT(*) AS total FROM (SELECT 1) grid", dialeto), subconsulta, filtro)
            : TrocarSubconsulta(LerConsulta("SELECT COUNT(*) AS total FROM (SELECT 1) contagem", dialeto), externa with { With = null }, null);

        return new SqlGrid(ToSql(pagina), ToSql(contagem with { With = baseCompleta.With }));
    }

    /// <summary>Acrescenta a cláusula de página na sintaxe do dialeto.</summary>
    private static Query AplicarPagina(Query consulta, Dialect dialeto, int pagina, int tamanho)
    {
        var pular = (pagina - 1) * tamanho;
        if (dialeto is MsSqlDialect or OracleDialect)
        {
            // OFFSET n ROWS FETCH FIRST m ROWS ONLY; no SQL Server o ORDER BY é obrigatório.
            var ordem = consulta.OrderBy ?? (dialeto is MsSqlDialect ? LerConsulta("SELECT 1 ORDER BY (SELECT NULL)", dialeto).OrderBy : null);
            return consulta with
            {
                OrderBy = ordem,
                LimitClause = new LimitClause.LimitOffset(null, new Offset(Numero(pular), new OffsetRows.Rows()), Sequence<Expr>.Empty),
                Fetch = new Fetch(WithTies: false, Percent: false, Quantity: Numero(tamanho)),
            };
        }
        // LIMIT m OFFSET n (sem OFFSET na primeira página).
        var offset = pular == 0 ? null : new Offset(Numero(pular), new OffsetRows.None());
        return consulta with { LimitClause = new LimitClause.LimitOffset(Numero(tamanho), offset, Sequence<Expr>.Empty) };
    }

    /// <summary>Coloca a consulta no lugar do <c>(SELECT 1)</c> do modelo e, se houver, o filtro no WHERE.</summary>
    private static Query TrocarSubconsulta(Query modelo, Query subconsulta, Expr? filtro)
    {
        var select = ((SetExpr.Select)modelo.Body).Value;
        var tabela = select.From[0];
        var derivada = (TableFactor.Derived)tabela.Relation;
        return modelo with
        {
            Body = new SetExpr.Select(select with
            {
                From = Sequence.Of(tabela with { Relation = derivada with { Subquery = subconsulta } }),
                Selection = filtro,
            }),
        };
    }

    /// <summary>Nomes das colunas de saída do SQL base (alias ou nome da coluna).</summary>
    private static string[] ColunasDeSaida(Query consulta) =>
        ((SetExpr.Select)consulta.Body).Value.Projection.Select(item => item switch
        {
            SelectItem.ExprWithAlias comAlias => comAlias.Alias.Value,
            SelectItem.UnnamedExpr { Value: Expr.Identifier id } => id.Value.Value,
            SelectItem.UnnamedExpr { Value: Expr.CompoundIdentifier composto } => composto.Value[composto.Value.Count - 1].Value,
            _ => throw new ArgumentException($"A coluna '{item}' do SQL base precisa de um alias para ser usada no grid."),
        }).ToArray();

    /// <summary>O ORDER BY do SQL base reescrito com as colunas de saída (c.nome → grid.nome, p.id → grid.pedido_id).</summary>
    private static OrderBy? OrdemDoSqlBase(Query consulta, string[] colunas)
    {
        if (consulta.OrderBy?.Kind is not OrderByKind.Expressions { Value: var itens })
        {
            return null;
        }
        var projecao = ((SetExpr.Select)consulta.Body).Value.Projection;
        var ordem = new List<(Expr, bool)>();
        foreach (var item in itens)
        {
            var indice = projecao.ToList().FindIndex(p => p switch
            {
                SelectItem.ExprWithAlias a => a.Expr.ToString() == item.Expr.ToString(),
                SelectItem.UnnamedExpr u => u.Value.ToString() == item.Expr.ToString(),
                _ => false,
            });
            if (indice < 0)
            {
                return null; // ordena por algo que não está na saída: não dá para levar para fora
            }
            ordem.Add((Coluna(colunas, colunas[indice]), item.Options.Sort is OrderBySort.Desc));
        }
        return OrderBy(ordem);
    }

    private static OrderBy OrderBy(IEnumerable<(Expr Expr, bool Decrescente)> itens) =>
        new(new OrderByKind.Expressions(itens
                .Select(i => new OrderByExpr(i.Expr, new OrderByOptions(i.Decrescente ? new OrderBySort.Desc() : null, null), null))
                .ToSequence()),
            null);

    private static Expr Condicao(Expr coluna, string operador, Expr valor) => operador switch
    {
        "=" => new Expr.BinaryOp(coluna, new BinaryOperator.Eq(), valor),
        "<>" => new Expr.BinaryOp(coluna, new BinaryOperator.NotEq(), valor),
        ">" => new Expr.BinaryOp(coluna, new BinaryOperator.Gt(), valor),
        ">=" => new Expr.BinaryOp(coluna, new BinaryOperator.GtEq(), valor),
        "<" => new Expr.BinaryOp(coluna, new BinaryOperator.Lt(), valor),
        "<=" => new Expr.BinaryOp(coluna, new BinaryOperator.LtEq(), valor),
        "LIKE" => new Expr.Like(Negated: false, Any: false, Expr: coluna, Pattern: valor, EscapeChar: null),
        _ => throw new ArgumentException($"Operador não suportado: {operador}"),
    };

    // grid.<coluna>, com a coluna validada contra o SQL base.
    private static Expr Coluna(string[] colunas, string nome) =>
        new Expr.CompoundIdentifier(Sequence.Of(new Ident("grid"), new Ident(Validar(colunas, nome))));

    private static string Validar(string[] colunas, string nome) =>
        colunas.FirstOrDefault(c => string.Equals(c, nome, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Coluna '{nome}' não existe no SQL base. Colunas: {string.Join(", ", colunas)}.");

    // Parâmetro no formato do banco: $1, $2... no PostgreSQL; @nome nos demais.
    private static Expr Parametro(Dialect dialeto, string nome, int indice) =>
        Expr.MakeValue(new Value.Placeholder(dialeto is PostgreSqlDialect ? "$" + (indice + 1) : "@" + nome));

    private static Query LerConsulta(string sql, Dialect dialeto) =>
        Assert.IsType<Statement.Query>(Assert.Single(new SqlQueryParser().Parse(sql, dialeto))).Value;

    private static string ToSql(Query consulta) => new Statement.Query(consulta).ToSql();

    private static Expr Numero(int valor) =>
        Expr.MakeValue(new Value.Number(valor.ToString(CultureInfo.InvariantCulture), false));

    // --- Banco de teste ---------------------------------------------------------------------------------------------

    // 5 clientes e 30 pedidos (pedido N do cliente ((N - 1) % 5) + 1, valor N × 10,5); múltiplos de 4 cancelados.
    private const string Esquema =
        "CREATE TABLE clientes (id INTEGER PRIMARY KEY, nome TEXT NOT NULL);" +
        "CREATE TABLE pedidos (id INTEGER PRIMARY KEY, cliente_id INTEGER NOT NULL REFERENCES clientes (id)," +
        " status TEXT NOT NULL, valor REAL NOT NULL);";

    private static SqliteConnection CriarBanco()
    {
        var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        Executar(conexao, Esquema);
        var nomes = new[] { "Ana", "Bruno", "Carla", "Diego", "Elisa" };
        for (var i = 0; i < nomes.Length; i++)
        {
            Executar(conexao, $"INSERT INTO clientes (id, nome) VALUES ({i + 1}, '{nomes[i]}')");
        }
        for (var id = 1; id <= 30; id++)
        {
            var status = id % 4 == 0 ? "CANCELADO" : "PAGO";
            var valor = (id * 10.5).ToString(CultureInfo.InvariantCulture);
            Executar(conexao, $"INSERT INTO pedidos VALUES ({id}, {(id - 1) % nomes.Length + 1}, '{status}', {valor})");
        }
        return conexao;
    }

    private static void Executar(SqliteConnection conexao, string sql)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        comando.ExecuteNonQuery();
    }

    private static SqliteCommand Comando(SqliteConnection conexao, string sql, IDictionary<string, object>? parametros)
    {
        var comando = conexao.CreateCommand();
        comando.CommandText = sql;
        foreach (var p in parametros ?? new Dictionary<string, object>())
        {
            comando.Parameters.AddWithValue(p.Key, p.Value);
        }
        return comando;
    }

    private static object Escalar(SqliteConnection conexao, string sql, IDictionary<string, object>? parametros = null)
    {
        using var comando = Comando(conexao, sql, parametros);
        return comando.ExecuteScalar()!;
    }

    private static List<string> Linhas(SqliteConnection conexao, string sql, IDictionary<string, object>? parametros = null)
    {
        using var comando = Comando(conexao, sql, parametros);
        using var leitor = comando.ExecuteReader();
        var linhas = new List<string>();
        while (leitor.Read())
        {
            var valores = new string[leitor.FieldCount];
            for (var i = 0; i < leitor.FieldCount; i++)
            {
                valores[i] = Convert.ToString(leitor.GetValue(i), CultureInfo.InvariantCulture)!;
            }
            linhas.Add(string.Join("|", valores));
        }
        return linhas;
    }
}
