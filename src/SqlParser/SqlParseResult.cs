// Código próprio da camada pública (sem origem no sqlparser-rs).
// Ver docs/adr/0008-api-publica-fase-1.md.

using SqlParser.Core;
using SqlParser.Core.Ast;

namespace SqlParser;

/// <summary>
/// Resultado de <see cref="SqlQueryParser.ParseWithComments"/>: as instruções e os comentários do texto.
/// </summary>
/// <remarks>
/// Equivale à tupla <c>(Vec&lt;Statement&gt;, Comments)</c> de <c>Parser::parse_sql_with_comments</c> no Rust.
/// Pode ser desconstruído: <c>var (statements, comments) = parser.ParseWithComments(sql);</c>.
/// </remarks>
public sealed class SqlParseResult
{
    /// <summary>Cria o resultado.</summary>
    /// <param name="statements">As instruções, na ordem do texto.</param>
    /// <param name="comments">Os comentários encontrados no texto.</param>
    /// <exception cref="ArgumentNullException"><paramref name="comments"/> é <see langword="null"/>.</exception>
    public SqlParseResult(Sequence<Statement> statements, Comments comments)
    {
        Statements = statements;
        Comments = comments ?? throw new ArgumentNullException(nameof(comments));
    }

    /// <summary>As instruções, na ordem do texto.</summary>
    public Sequence<Statement> Statements { get; }

    /// <summary>Os comentários, na ordem do texto, com a posição de cada um (<see cref="CommentWithSpan.Span"/>).</summary>
    public Comments Comments { get; }

    /// <summary>Desconstrói o resultado em instruções e comentários.</summary>
    /// <param name="statements">As instruções.</param>
    /// <param name="comments">Os comentários.</param>
    public void Deconstruct(out Sequence<Statement> statements, out Comments comments)
    {
        statements = Statements;
        comments = Comments;
    }
}
