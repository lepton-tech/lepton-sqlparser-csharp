// Código próprio da camada pública (sem origem no sqlparser-rs). Imita o formato de uso do
// SqlParser-cs (new SqlQueryParser().Parse(sql, dialect, options)) sem copiar código dele.
// Ver docs/adr/0002-arquitetura-em-camadas-e-api.md e docs/adr/0008-api-publica-fase-1.md.

using System.Diagnostics.CodeAnalysis;
using SqlParser.Core;
using SqlParser.Core.Ast;
using SqlParser.Core.Dialects;

namespace SqlParser;

/// <summary>
/// Ponto de entrada da API pública: interpreta texto SQL e devolve a árvore sintática (AST) do
/// <c>SqlParser.Core</c>.
/// </summary>
/// <remarks>
/// <para>
/// A classe não guarda estado entre chamadas: a mesma instância pode ser reutilizada e usada por
/// várias threads ao mesmo tempo. Cada chamada cria um <see cref="Parser"/> novo do Core.
/// </para>
/// <para>
/// O dialeto padrão é <see cref="GenericDialect"/>, como no SqlParser-cs. Quando nenhuma opção é
/// informada, valem as opções padrão do Core para o dialeto (por exemplo, vírgulas finais conforme
/// <see cref="Dialect.SupportsTrailingCommas"/>). Quando <see cref="ParserOptions"/> é informado, ele
/// substitui essas opções por inteiro, exatamente como <c>Parser::with_options</c> no Rust.
/// </para>
/// <para>
/// Erros de sintaxe e de tokenização são lançados como <see cref="SqlParseException"/>, com a mesma
/// mensagem do Core (<c>"sql parser error: ..."</c>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var statements = new SqlQueryParser().Parse("SELECT a FROM t WHERE b > 1");
/// string sql = statements[0].ToSql();
/// string pretty = statements[0].ToSql(pretty: true);
/// </code>
/// </example>
public sealed class SqlQueryParser
{
    /// <summary>
    /// Cria um parser com o limite de recursão padrão do Core (50 em sqlparser-rs v0.63.0).
    /// </summary>
    public SqlQueryParser()
    {
    }

    /// <summary>
    /// Cria um parser com um limite de recursão próprio (Rust: <c>Parser::with_recursion_limit</c>).
    /// </summary>
    /// <param name="recursionLimit">
    /// Profundidade máxima de aninhamento aceita. Ao excedê-la, o parse falha com
    /// <see cref="ParserError.RecursionLimitExceeded"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Um limite acima do padrão aceita árvores mais profundas (subconsultas, parênteses e junções aninhadas), e
    /// todo código que percorre a árvore recursivamente desce o mesmo tanto. O parse, o <c>ToSql</c> e os visitors
    /// continuam numa thread nova quando falta pilha (DIV-005 e DIV-009 em <c>docs/port/divergencias.md</c>), mas
    /// nem tudo tem essa proteção: a igualdade dos nós (<c>==</c>, <c>Equals</c>, <c>GetHashCode</c>; DIV-010) e
    /// o <c>Span()</c> de consultas e junções aninhadas podem estourar a pilha e derrubar o processo.
    /// </para>
    /// <para>
    /// Use um limite alto só quando as entradas precisarem dele e, para texto de terceiros, limite também o
    /// tamanho do SQL aceito.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="recursionLimit"/> é negativo.</exception>
    public SqlQueryParser(int recursionLimit)
    {
        if (recursionLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(recursionLimit), recursionLimit, "O limite de recursão não pode ser negativo.");
        }
        RecursionLimit = recursionLimit;
    }

    /// <summary>
    /// Limite de recursão configurado, ou <see langword="null"/> quando vale o padrão do Core.
    /// </summary>
    public int? RecursionLimit { get; }

    /// <summary>Interpreta <paramref name="sql"/>, que pode conter várias instruções separadas por <c>;</c>.</summary>
    /// <param name="sql">Texto SQL.</param>
    /// <param name="dialect">Dialeto; <see langword="null"/> usa <see cref="GenericDialect"/>.</param>
    /// <param name="options">Opções do parser; <see langword="null"/> usa as opções padrão do dialeto.</param>
    /// <returns>As instruções, na ordem do texto.</returns>
    /// <remarks>
    /// Para usar o dialeto padrão com opções, use o argumento nomeado: <c>Parse(sql, options: opcoes)</c>.
    /// A chamada <c>Parse(sql, null, opcoes)</c> é ambígua com a sobrecarga que recebe o nome do dialeto.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> é <see langword="null"/>.</exception>
    /// <exception cref="SqlParseException">O texto não é SQL válido para o dialeto.</exception>
    public Sequence<Statement> Parse(string sql, Dialect? dialect = null, ParserOptions? options = null)
    {
        if (sql is null)
        {
            throw new ArgumentNullException(nameof(sql));
        }
        try
        {
            return CreateParser(dialect, options).TryWithSql(sql).ParseStatements();
        }
        catch (ParserException e)
        {
            throw new SqlParseException(e.Error, e);
        }
    }

    /// <summary>Interpreta <paramref name="sql"/> com o dialeto de nome <paramref name="dialectName"/>.</summary>
    /// <param name="sql">Texto SQL.</param>
    /// <param name="dialectName">
    /// Nome do dialeto, sem diferenciar maiúsculas de minúsculas, como em <see cref="Dialect.DialectFromStr"/>
    /// (Rust: <c>dialect_from_str</c>): <c>generic</c>, <c>mysql</c>, <c>postgresql</c>/<c>postgres</c>,
    /// <c>hive</c>, <c>sqlite</c>, <c>snowflake</c>, <c>redshift</c>, <c>mssql</c>, <c>clickhouse</c>,
    /// <c>bigquery</c>, <c>ansi</c>, <c>duckdb</c>, <c>databricks</c>, <c>spark</c>/<c>sparksql</c>,
    /// <c>oracle</c>, <c>teradata</c>.
    /// </param>
    /// <param name="options">Opções do parser; <see langword="null"/> usa as opções padrão do dialeto.</param>
    /// <returns>As instruções, na ordem do texto.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> ou <paramref name="dialectName"/> é <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="dialectName"/> não corresponde a nenhum dialeto.</exception>
    /// <exception cref="SqlParseException">O texto não é SQL válido para o dialeto.</exception>
    public Sequence<Statement> Parse(string sql, string dialectName, ParserOptions? options = null) =>
        Parse(sql, DialectFromName(dialectName), options);

    /// <summary>
    /// Tenta interpretar <paramref name="sql"/> com <see cref="GenericDialect"/>. Não lança exceção para SQL inválido.
    /// </summary>
    /// <param name="sql">Texto SQL.</param>
    /// <param name="statements">As instruções, se o parse deu certo; senão, uma sequência vazia.</param>
    /// <param name="error">O erro, se o parse falhou; senão, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> se o parse deu certo.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> é <see langword="null"/>.</exception>
    public bool TryParse(string sql, out Sequence<Statement> statements, [NotNullWhen(false)] out SqlParseException? error) =>
        TryParse(sql, null, null, out statements, out error);

    /// <summary>
    /// Tenta interpretar <paramref name="sql"/> com <paramref name="dialect"/>. Não lança exceção para SQL inválido.
    /// </summary>
    /// <param name="sql">Texto SQL.</param>
    /// <param name="dialect">Dialeto; <see langword="null"/> usa <see cref="GenericDialect"/>.</param>
    /// <param name="statements">As instruções, se o parse deu certo; senão, uma sequência vazia.</param>
    /// <param name="error">O erro, se o parse falhou; senão, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> se o parse deu certo.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> é <see langword="null"/>.</exception>
    public bool TryParse(string sql, Dialect? dialect, out Sequence<Statement> statements, [NotNullWhen(false)] out SqlParseException? error) =>
        TryParse(sql, dialect, null, out statements, out error);

    /// <summary>
    /// Tenta interpretar <paramref name="sql"/> com <paramref name="dialect"/> e <paramref name="options"/>.
    /// Não lança exceção para SQL inválido.
    /// </summary>
    /// <param name="sql">Texto SQL.</param>
    /// <param name="dialect">Dialeto; <see langword="null"/> usa <see cref="GenericDialect"/>.</param>
    /// <param name="options">Opções do parser; <see langword="null"/> usa as opções padrão do dialeto.</param>
    /// <param name="statements">As instruções, se o parse deu certo; senão, uma sequência vazia.</param>
    /// <param name="error">O erro, se o parse falhou; senão, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> se o parse deu certo.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> é <see langword="null"/>.</exception>
    public bool TryParse(
        string sql,
        Dialect? dialect,
        ParserOptions? options,
        out Sequence<Statement> statements,
        [NotNullWhen(false)] out SqlParseException? error)
    {
        try
        {
            statements = Parse(sql, dialect, options);
            error = null;
            return true;
        }
        catch (SqlParseException e)
        {
            statements = Sequence<Statement>.Empty;
            error = e;
            return false;
        }
    }

    /// <summary>
    /// Interpreta <paramref name="sql"/> e devolve também os comentários encontrados no texto
    /// (Rust: <c>Parser::parse_sql_with_comments</c>, aqui com opções).
    /// </summary>
    /// <param name="sql">Texto SQL.</param>
    /// <param name="dialect">Dialeto; <see langword="null"/> usa <see cref="GenericDialect"/>.</param>
    /// <param name="options">Opções do parser; <see langword="null"/> usa as opções padrão do dialeto.</param>
    /// <returns>As instruções e os comentários.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> é <see langword="null"/>.</exception>
    /// <exception cref="SqlParseException">O texto não é SQL válido para o dialeto.</exception>
    public SqlParseResult ParseWithComments(string sql, Dialect? dialect = null, ParserOptions? options = null)
    {
        if (sql is null)
        {
            throw new ArgumentNullException(nameof(sql));
        }
        try
        {
            var parser = CreateParser(dialect, options).TryWithSql(sql);
            var statements = parser.ParseStatements();
            return new SqlParseResult(statements, parser.IntoComments());
        }
        catch (ParserException e)
        {
            throw new SqlParseException(e.Error, e);
        }
    }

    private Parser CreateParser(Dialect? dialect, ParserOptions? options)
    {
        var parser = new Parser(dialect ?? new GenericDialect());
        if (options is not null)
        {
            parser = parser.WithOptions(options);
        }
        if (RecursionLimit is { } limit)
        {
            parser = parser.WithRecursionLimit(limit);
        }
        return parser;
    }

    private static Dialect DialectFromName(string dialectName)
    {
        if (dialectName is null)
        {
            throw new ArgumentNullException(nameof(dialectName));
        }
        return Dialect.DialectFromStr(dialectName)
            ?? throw new ArgumentException($"Dialeto desconhecido: '{dialectName}'.", nameof(dialectName));
    }
}
