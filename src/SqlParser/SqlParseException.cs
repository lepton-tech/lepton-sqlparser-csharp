// Código próprio da camada pública (sem origem no sqlparser-rs).
// Ver docs/adr/0008-api-publica-fase-1.md (decisão sobre erros).

using System.Globalization;
using System.Text.RegularExpressions;
using SqlParser.Core;

namespace SqlParser;

/// <summary>
/// Erro de interpretação de SQL lançado pela API pública (<see cref="SqlQueryParser"/>).
/// </summary>
/// <remarks>
/// <para>
/// A mensagem é exatamente a do Core (o <c>Display</c> de <see cref="ParserError"/> no Rust), por exemplo
/// <c>"sql parser error: Expected: an expression, found: = at Line: 2, Column: 22"</c>. Erros do
/// tokenizador chegam como <see cref="ParserError.TokenizerError"/>, como no Rust
/// (<c>impl From&lt;TokenizerError&gt; for ParserError</c>).
/// </para>
/// <para>
/// O Rust não guarda a posição do erro em um campo: ela faz parte do texto (sufixo
/// <c>" at Line: L, Column: C"</c>, o <c>Display</c> de <see cref="Location"/>). Esta classe extrai a posição
/// desse sufixo; quando a mensagem não tem posição (por exemplo, fim inesperado do texto ou limite de
/// recursão), <see cref="Location"/> é <see langword="null"/> e <see cref="Line"/>/<see cref="Column"/> valem 0.
/// </para>
/// </remarks>
public sealed class SqlParseException : Exception
{
    // Display de Location no Core (Tokenizer/Location.cs): " at Line: {line}, Column: {column}",
    // sempre no fim da mensagem (parser_err! e TokenizerError acrescentam a posição por último).
    private static readonly Regex LocationSuffix = new Regex(
        @" at Line: (\d+), Column: (\d+)$",
        RegexOptions.CultureInvariant);

    /// <summary>Cria a exceção para o erro <paramref name="error"/> do Core.</summary>
    /// <param name="error">O erro do parser.</param>
    /// <param name="innerException">A exceção original, normalmente a <see cref="ParserException"/> do Core.</param>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> é <see langword="null"/>.</exception>
    public SqlParseException(ParserError error, Exception? innerException = null)
        : base((error ?? throw new ArgumentNullException(nameof(error))).ToString(), innerException)
    {
        Error = error;
        Location = ExtractLocation(Message);
    }

    /// <summary>O erro do Core (Rust: o valor de <c>Err(ParserError)</c>).</summary>
    public ParserError Error { get; }

    /// <summary>Posição do erro no texto SQL (linha e coluna a partir de 1), ou <see langword="null"/> se desconhecida.</summary>
    public Location? Location { get; }

    /// <summary>Linha do erro, a partir de 1; 0 quando a posição é desconhecida.</summary>
    public long Line => Location?.Line ?? 0;

    /// <summary>Coluna do erro, a partir de 1; 0 quando a posição é desconhecida.</summary>
    public long Column => Location?.Column ?? 0;

    private static Location? ExtractLocation(string message)
    {
        var match = LocationSuffix.Match(message);
        if (!match.Success)
        {
            return null;
        }
        if (!long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var line)
            || !long.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var column))
        {
            return null;
        }
        return new Location(line, column);
    }
}
