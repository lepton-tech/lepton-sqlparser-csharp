// Código próprio da camada pública (sem origem no sqlparser-rs). O nome ToSql imita o SqlParser-cs,
// sem copiar código dele: a geração de SQL é o Display do Core (Support/Formatter.cs).
// Ver docs/adr/0008-api-publica-fase-1.md.

using System.Text;
using SqlParser.Core;
using SqlParser.Core.Ast;

namespace SqlParser;

/// <summary>Geração de SQL a partir de nós da AST.</summary>
public static class SqlWritingExtensions
{
    /// <summary>Separador entre instruções em <see cref="ToSql(IEnumerable{Statement}, bool)"/>, no modo compacto.</summary>
    public const string StatementSeparator = "; ";

    /// <summary>Separador entre instruções em <see cref="ToSql(IEnumerable{Statement}, bool)"/>, no modo pretty.</summary>
    public const string PrettyStatementSeparator = ";\n";

    /// <summary>
    /// Gera o SQL de um nó da AST (ou de qualquer valor com <c>Display</c> no Core, como <see cref="Token"/>).
    /// </summary>
    /// <param name="node">O nó.</param>
    /// <param name="pretty">
    /// <see langword="false"/> (padrão): SQL em uma linha, igual a <c>node.ToString()</c> (Rust: <c>format!("{}", node)</c>).
    /// <see langword="true"/>: SQL indentado em várias linhas, com quebras <c>"\n"</c> (Rust: <c>format!("{:#}", node)</c>).
    /// </param>
    /// <returns>O texto SQL.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> é <see langword="null"/>.</exception>
    public static string ToSql(this IDisplay node, bool pretty = false)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }
        return pretty ? Formatter.FormatAlternate(node) : Formatter.Format(node);
    }

    /// <summary>
    /// Gera o SQL de várias instruções, como as devolvidas por <c>SqlQueryParser.Parse</c>.
    /// </summary>
    /// <param name="statements">As instruções.</param>
    /// <param name="pretty">
    /// <see langword="false"/> (padrão): cada instrução em uma linha, separadas por <see cref="StatementSeparator"/> (<c>"; "</c>).
    /// <see langword="true"/>: cada instrução no formato pretty, separadas por <see cref="PrettyStatementSeparator"/> (<c>";\n"</c>).
    /// </param>
    /// <returns>O texto SQL, sem <c>;</c> depois da última instrução.</returns>
    /// <remarks>O Rust não tem <c>Display</c> para <c>Vec&lt;Statement&gt;</c>; o separador é uma escolha desta camada.</remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="statements"/> ou um de seus itens é <see langword="null"/> (nos dois casos,
    /// <see cref="ArgumentException.ParamName"/> é <c>"statements"</c>; a mensagem diz a posição do item).
    /// </exception>
    public static string ToSql(this IEnumerable<Statement> statements, bool pretty = false)
    {
        if (statements is null)
        {
            throw new ArgumentNullException(nameof(statements));
        }
        var separator = pretty ? PrettyStatementSeparator : StatementSeparator;
        var sb = new StringBuilder();
        var index = 0;
        foreach (var statement in statements)
        {
            if (statement is null)
            {
                throw new ArgumentNullException(nameof(statements), $"O item {index} da coleção de instruções é null.");
            }
            if (index > 0)
            {
                sb.Append(separator);
            }
            sb.Append(statement.ToSql(pretty));
            index++;
        }
        return sb.ToString();
    }
}
